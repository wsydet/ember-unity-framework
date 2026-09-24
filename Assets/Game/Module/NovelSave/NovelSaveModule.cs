using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ember.Basic;
using Ember.Core;
using Game.Narrative;
using UnityEngine;

namespace Game.NovelSave
{
    [EmberModule(ModulePhase.Global, Enabled = true)]
    public sealed class NovelSaveModule : EmberSingleton<NovelSaveModule>, IEmberModule, IEmberUpdate
    {
        #region 内部参数
        private NovelSession _tracked, _candidate, _original, _handoff;
        private IDisposable _restoreObservation;
        private Action<NovelSession> _prepared;
        private Action<string> _loadFailed;
        private Action _loadCancelled;
        private long _autoRevision;
        private bool _busy, _accountWritable;
        private Task<string> _saveTask, _accountTask;
        private Task<NovelCheckpoint> _loadTask;
        private Func<NarrativeTableCatalog> _loadCatalog;
        private int _savingSlot;
        private bool _accountDirty, _preferencesPending;
        private readonly Queue<NovelCheckpoint> _autoSaves = new();
        private readonly HashSet<string> _read = new(StringComparer.Ordinal);
        private readonly int[] _lastWriteFrames = { -1, -1, -1, -1, -1, -1, -1, -1 };
        public NovelSaveStore Store { get; private set; }
        public NovelAccountData Account { get; private set; }
        public bool IsBusy => _busy || _saveTask != null || _autoSaves.Count > 0 || _loadTask != null || _candidate != null || (_handoff != null && !_handoff.IsDisposed && !_handoff.IsReady);
        public string Message { get; private set; }
        public event Action Changed;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Feedback(string message) { Message = message; Notify(); }
        private void Notify()
        {
            if (Changed == null) return;
            foreach (Action observer in Changed.GetInvocationList())
                try { observer(); } catch (Exception ex) { EmberDebug.LogWarning("Game.NovelSave", ex.Message); }
        }
        private void MarkRead(NovelHistoryEntry line)
        {
            string key = ReadKey(_tracked.Snapshot.StoryId, line.LineId, line.TextRevision);
            if (!_read.Add(key)) return;
            Account.ReadLines.Add(key);
            if (_accountWritable) _accountDirty = true;
        }
        private string ReadKey(string storyId, string lineId, int revision) => storyId.Length + ":" + storyId + lineId.Length + ":" + lineId + ":" + revision;
        private void ReleaseCandidate()
        {
            _candidate?.Dispose(); _candidate = null; _restoreObservation?.Dispose(); _restoreObservation = null;
            _original?.Resume("RestoreTransaction");
            if (EmberModuleCollector.Instance.TryGetModule(out NarrativeModule narrative)) narrative.RefreshObservation();
            _original = null; _prepared = null; _loadFailed = null; _loadCancelled = null;
        }
        private void FailLoad(string error)
        {
            var failed = _loadFailed; ReleaseCandidate(); Feedback(error); failed?.Invoke(error);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void OnInit()
        {
            Store = new NovelSaveStore(Path.Combine(Application.persistentDataPath, "VisualNovelSaves"));
            _accountWritable = Store.ReadAccount(out var account, out var error); Account = account;
            foreach (var line in account.ReadLines) _read.Add(line);
            Message = Store.IndexError ?? error;
        }
        public void Track(NovelSession session)
        {
            if (_tracked != null) _tracked.LineRead -= MarkRead;
            _tracked = session; _autoRevision = 0;
            if (_tracked != null) { _tracked.LineRead += MarkRead; _autoRevision = _tracked.RestoreReady ? _tracked.AutoSaveRevision : 0; ApplyPreferences(); }
        }
        private void ApplyPreferences()
        {
            var a = Account ?? new NovelAccountData();
            _tracked?.ConfigureReading(IsRead, a.TextSpeed, a.AutoInterval, a.BgmVolume, a.SfxVolume, a.VoiceVolume);
            _tracked?.ConfigureScreenEffects(a.ShakePreference, a.FlashPreference);
            _tracked?.SetReadingMultiplier(Mathf.Clamp(a.ReadingMultiplier, 1, 3));
            if (Ember.Audio.EmberAudioManager.TryGetInstance(out var audio) && audio.IsInitialized)
            { audio.SetBGMVolume(a.BgmVolume); audio.SetSFXVolume(a.SfxVolume); }
        }
        /// <summary>Returns whether the request was accepted; Changed/Message report the eventual write result.</summary>
        public bool Save(int slot)
        {
            if (IsBusy) { Feedback("正在读写存档，请稍候"); return false; }
            if (slot < 0 || slot > 7) { Feedback("无效槽位"); return false; }
            if (_lastWriteFrames[slot] == Time.frameCount) return false;
            _busy = true;
            try
            {
                if (_tracked == null) { Feedback("当前没有可保存的会话"); return false; }
                if (!_tracked.TryCapture(out var checkpoint, out var error)) { Feedback(error); return false; }
                _lastWriteFrames[slot] = Time.frameCount;
                _savingSlot = slot; _saveTask = Store.SaveAsync(slot, checkpoint);
                Feedback("正在保存…"); return true;
            }
            finally { _busy = false; Notify(); }
        }
        public bool BeginLoad(int slot, Func<NarrativeTableCatalog> catalog, Action<NovelSession> prepared, Action<string> failed = null, Action cancelled = null)
        {
            if (IsBusy) { Feedback("正在恢复，请先等待或取消"); return false; }
            _original = _tracked?.IsDisposed == false ? _tracked : null; _original?.Pause("RestoreTransaction");
            _loadCatalog = catalog; _prepared = prepared; _loadFailed = failed; _loadCancelled = cancelled; _loadTask = Store.ReadAsync(slot);
            Feedback("正在读取存档…"); return true;
        }
        public void CancelLoad()
        {
            if (_candidate == null && _loadTask == null) return;
            // File reads may finish, but a cancelled request can never construct or commit a session.
            if (_loadTask != null) _loadTask.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
            var cancelled = _loadCancelled;
            _loadTask = null; _loadCatalog = null; ReleaseCandidate(); Feedback("已取消读档，原进度保留");
            cancelled?.Invoke();
        }
        public void ReportMessage(string message) { Feedback(message); }
        public bool IsRead(string storyId, string lineId, int textRevision) => storyId != null && lineId != null && _read.Contains(ReadKey(storyId, lineId, textRevision));
        /// <summary>Accepts an account update; the eventual durable result is reported through Changed/Message.</summary>
        public bool SavePreferences(float textSpeed, float autoInterval, float bgmVolume, float sfxVolume, float voiceVolume)
        {
            if (!_accountWritable) { Feedback("已读/偏好文件不可写，原文件已保留"); return false; }
            if (float.IsNaN(textSpeed) || float.IsNaN(autoInterval) || float.IsNaN(bgmVolume) || float.IsNaN(sfxVolume) || float.IsNaN(voiceVolume) ||
                textSpeed < 1 || textSpeed > 200 || autoInterval < 0 || autoInterval > 60 || bgmVolume < 0 || bgmVolume > 1 ||
                sfxVolume < 0 || sfxVolume > 1 || voiceVolume < 0 || voiceVolume > 1)
            { Feedback("偏好值超出范围"); return false; }
            var next = JsonUtility.FromJson<NovelAccountData>(JsonUtility.ToJson(Account));
            next.TextSpeed = textSpeed; next.AutoInterval = autoInterval; next.BgmVolume = bgmVolume; next.SfxVolume = sfxVolume; next.VoiceVolume = voiceVolume;
            Account = next; ApplyPreferences(); _accountDirty = true; _preferencesPending = true; Feedback("正在保存偏好…"); return true;
        }
        public bool SaveReadingDisplay(int fontSize, int multiplier)
        {
            if (!_accountWritable) { Feedback("已读/偏好文件不可写，原文件已保留"); return false; }
            if (fontSize < 0 || fontSize > 2 || multiplier < 1 || multiplier > 3) return false;
            var next = JsonUtility.FromJson<NovelAccountData>(JsonUtility.ToJson(Account));
            next.FontSize = fontSize; next.ReadingMultiplier = multiplier;
            Account = next; ApplyPreferences(); _accountDirty = true; _preferencesPending = true;
            Feedback("正在保存偏好…"); return true;
        }
        public bool SaveScreenPreferences(NovelEffectPreference shake, NovelEffectPreference flash)
        {
            if (!_accountWritable) { Feedback("已读/偏好文件不可写，原文件已保留"); return false; }
            if (!Enum.IsDefined(typeof(NovelEffectPreference), shake) || !Enum.IsDefined(typeof(NovelEffectPreference), flash)) return false;
            var next = JsonUtility.FromJson<NovelAccountData>(JsonUtility.ToJson(Account));
            next.ShakePreference = shake; next.FlashPreference = flash;
            Account = next; ApplyPreferences(); _accountDirty = true; _preferencesPending = true;
            Feedback("正在保存偏好…"); return true;
        }
        public void Update()
        {
            if (_accountTask?.IsCompleted == true)
            {
                string error = _accountTask.GetAwaiter().GetResult(); _accountTask = null;
                if (error != null) { _preferencesPending = false; Feedback(error); }
                else if (_preferencesPending && !_accountDirty) { _preferencesPending = false; Feedback("偏好已保存"); }
            }
            if (_accountDirty && _accountTask == null)
            { _accountDirty = false; _accountTask = Store.SaveAccountAsync(Account); }
            if (_saveTask?.IsCompleted == true)
            {
                string error = _saveTask.GetAwaiter().GetResult(); _saveTask = null;
                Feedback(error ?? "保存成功 · " + (_savingSlot < 6 ? "手动槽 " + (_savingSlot + 1) : _savingSlot == 6 ? "快速槽" : "自动槽"));
            }
            if (_loadTask?.IsCompleted == true)
            {
                var loading = _loadTask; _loadTask = null;
                try
                {
                    _candidate = new NovelSession(loading.GetAwaiter().GetResult(), _loadCatalog, new NovelResources());
                    _restoreObservation = NarrativeObservation.Register(_candidate); Feedback("正在校验并准备恢复资源…");
                }
                catch (Exception ex) { FailLoad(ex.Message); }
                _loadCatalog = null;
            }
            if (_candidate != null)
            {
                _candidate.Tick(0, Time.frameCount);
                if (_candidate.Snapshot.State == NarrativeState.Faulted)
                { string error = _candidate.Snapshot.Error.ToString(); FailLoad(error); return; }
                if (_candidate.RestoreReady)
                {
                    var ready = _candidate; var callback = _prepared; var original = _original; var failed = _loadFailed;
                    _candidate = null; _prepared = null; _original = null; _loadFailed = null; _loadCancelled = null;
                    _restoreObservation?.Dispose(); _restoreObservation = null;
                    _busy = true;
                    try { _handoff = ready; callback(ready); Feedback(ready.IsReady ? "读档成功" : "正在打开阅读页面…"); }
                    catch (Exception ex)
                    {
                        ready.Dispose(); original?.Resume("RestoreTransaction");
                        if (EmberModuleCollector.Instance.TryGetModule(out NarrativeModule narrative)) narrative.RefreshObservation();
                        Feedback("恢复提交失败：" + ex.Message);
                        failed?.Invoke(Message);
                    }
                    finally { _busy = false; Notify(); }
                }
                return;
            }
            if (_loadTask == null && _tracked != null && !_tracked.IsDisposed && _tracked.AutoSaveRevision != _autoRevision &&
                (_tracked.Snapshot.State == NarrativeState.AwaitingAdvance || _tracked.Snapshot.State == NarrativeState.AwaitingChoice))
            {
                // Capture the FIRST stable point even when a manual write is still in flight.
                _autoRevision = _tracked.AutoSaveRevision;
                if (_tracked.TryCapture(out var checkpoint, out var error)) _autoSaves.Enqueue(checkpoint);
                else Feedback(error);
            }
            if (!_busy && _saveTask == null && _loadTask == null && _autoSaves.Count > 0)
            {
                _savingSlot = 7; _saveTask = Store.SaveAsync(7, _autoSaves.Dequeue()); Feedback("正在保存自动槽…");
            }
        }
        void IEmberModule.OnDestroy()
        {
            Changed = null; CancelLoad(); ReleaseCandidate(); _handoff?.Dispose(); _handoff = null; Track(null); _read.Clear();
            // Drain the newest immutable account snapshot through the same ordered queue without blocking a frame.
            if (_accountDirty && _accountWritable) Store.SaveAccountAsync(Account);
            _accountDirty = false; _accountTask = null; _saveTask = null; _autoSaves.Clear(); _preferencesPending = false;
        }
        public void ResetModuleData() { ((IEmberModule)this).OnDestroy(); }
        #endregion
    }
}
