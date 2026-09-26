using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Narrative
{
    public sealed partial class NovelSession
    {
        #region 内部参数
        private NarrativeReadMode _readMode;
        private Func<string, string, int, bool> _isRead;
        private float _textSpeed = 32, _autoInterval = 1, _autoElapsed;
        /// <summary>脚本化段落对自动间隔的临时覆盖；为 null 时用玩家设置里的间隔。</summary>
        private float? _autoIntervalOverride;
        private long _readingVersion = -1, _lastReadingFrame = -1, _pauseSerial;
        private INovelAssetLease<AudioClip> _lineVoice;
        private bool _lineVoicePending, _lineHasVoice, _suppressReadingTick;
        public NarrativeReadMode ReadMode => _readMode;
        public int ReadingMultiplier { get; private set; } = 1;
        private long _restoredUIFrame = -1;
        public bool DialogueHidden { get; private set; }
        public bool VoiceWaiting => _lineVoicePending || _audio.VoicePlaying;
        public IReadOnlyList<NovelHistoryEntry> History => _history.Select(h => new NovelHistoryEntry
        { ChapterId = h.ChapterId, NodeId = h.NodeId, CommandId = h.CommandId, LineId = h.LineId,
            TextRevision = h.TextRevision, Text = h.Text, SpeakerNameKey = h.SpeakerNameKey,
            SpeakerVariableId = h.SpeakerVariableId, SpeakerVariableScope = h.SpeakerVariableScope,
            // 正文的多语言 Key 与「是否含文字变量绑定」必须一起带出来，
            // 否则历史页拿到的条目永远按存档文本显示，切语言不会跟着变。
            TextKey = h.TextKey, TextHasBindings = h.TextHasBindings,
            Speaker = ResolveSpeaker(h) }).ToList().AsReadOnly();
        private NarrativeSnapshot ReadingSnapshot
        {
            get
            {
                var s = _runner.Snapshot;
                var wait = s.Wait;
                if (s.State == NarrativeState.Revealing && _pageEnd > 0 && TextPageComplete)
                    wait = (wait & ~NarrativeWait.Text) | (_readMode == NarrativeReadMode.Auto ? NarrativeWait.Timer : NarrativeWait.Advance);
                else if (s.State == NarrativeState.Revealing && _textClock.Pausing) wait |= NarrativeWait.Timer;
                if (VoiceWaiting) wait |= NarrativeWait.Voice;
                if (_lineVoicePending) wait |= NarrativeWait.Resource;
                if (_readMode == NarrativeReadMode.Auto && s.State == NarrativeState.AwaitingAdvance)
                    wait = (wait & ~NarrativeWait.Advance) | (VoiceWaiting ? NarrativeWait.None : NarrativeWait.Timer);
                if (_readMode == NarrativeReadMode.Skip && s.State == NarrativeState.AwaitingAdvance)
                    wait &= ~NarrativeWait.Advance;
                return new NarrativeSnapshot(s, _readMode, wait, ActionObservation, _waitingActions);
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        // 历史是读时解析：称呼 Key 与角色键都按当前语言重新解析，所以旧句不会因为后来揭晓真名而串味，
        // 也不会因为补译文而失效。旧档缺 SpeakerNameKey 字段时退化为原来的角色名回退链。
        private string ResolveSpeaker(NovelHistoryEntry entry) => ResolveSpeakerName(entry.Speaker, entry.SpeakerNameKey,
            entry.SpeakerVariableId, entry.SpeakerVariableScope,
            _catalog != null && _catalog.TryGetCharacter(entry.Speaker, out var row) ? row.DisplayName : entry.Speaker);

        // 说话人显示名的唯一解析链：称呼 Key → 角色名 → 传入回退名；填了说话人变量则由变量覆盖。
        // 变量与称呼 Key 同属表现层：不参与存档指纹，也不改变对白角色键与强调匹配。
        // 解析不到（未声明、类型不符、空串）时退回原回退链，不显示空白、不报错。
        private string ResolveSpeakerName(string characterId, string speakerNameKey, string variableId,
            NovelVariableScope scope, string displayName)
        {
            string fallback = NovelLocalization.SpeakerName(characterId, speakerNameKey, displayName);
            if (string.IsNullOrWhiteSpace(variableId)) return fallback;
            return _runner.TryGetVariable(scope, variableId, out NovelValue value) &&
                value.Type == NovelValueType.String && !string.IsNullOrWhiteSpace(value.String)
                ? value.String : fallback;
        }
        private bool CurrentIsRead()
        {
            var c = _runner.CurrentCommand;
            return c?.Kind == NovelCommandKind.Say && _isRead?.Invoke(_runner.Snapshot.StoryId, c.LineId, c.TextRevision) == true;
        }
        private void RunnerChanged()
        {
            var s = _runner.Snapshot;
            if (_started && !_restoring)
            {
                if (_mediaChapter != null && _mediaChapter != s.ChapterId) SceneChangeMedia();
                _mediaChapter = s.ChapterId;
                if (s.State == NarrativeState.Revealing && _readingVersion != s.PositionVersion)
                {
                    StopVoice(); _readingVersion = s.PositionVersion; _autoElapsed = 0;
                    _revealing = _runner.CurrentCommand; ResetText(_revealing);
                    RefreshEmphasis();
                    if (_readMode == NarrativeReadMode.Skip && !CurrentIsRead()) { FinishActions(); _readMode = NarrativeReadMode.Manual; }
                    _lineHasVoice = !string.IsNullOrEmpty(_revealing.ResourceKey);
                    _lineVoicePending = _lineHasVoice && _readMode != NarrativeReadMode.Skip;
                }
                if (s.State == NarrativeState.AwaitingChoice || !s.HasActiveSession)
                { StoryDialogueVisible = true; if (_readMode == NarrativeReadMode.Skip) FinishActions(); _readMode = NarrativeReadMode.Manual; StopVoice(); }
                if (!s.HasActiveSession)
                {
                    if (s.State == NarrativeState.Ended)
                        foreach (var a in _runningActions)
                            if (!a.IsFinished && a.Property == "Audio") WriteAudioAction(a, 1);
                    CancelActions();
                }
            }
            Notify();
        }
        private void TickLineVoice()
        {
            if (!_lineVoicePending) return;
            if (_lineVoice == null)
            {
                if (!_catalog.TryResolve(NovelCommandKind.Voice, _runner.CurrentCommand.ResourceKey, out var path))
                    throw new InvalidOperationException("配音资源键无法解析：" + _runner.CurrentCommand.ResourceKey);
                _lineVoice = Load<AudioClip>(path); Notify(); return;
            }
            if (!_lineVoice.IsDone) return;
            if (!_lineVoice.Asset) throw new InvalidOperationException(_lineVoice.Error ?? "配音资源加载失败");
            _audio.Play(NovelCommandKind.Voice, _lineVoice.Asset); _lineVoicePending = false; Notify();
        }
        private void TickReading(float delta, long frame)
        {
            if (frame <= _lastReadingFrame) return;
            _lastReadingFrame = frame;
            var s = _runner.Snapshot;
            if (s.State != NarrativeState.AwaitingAdvance) return;
            if (_readMode == NarrativeReadMode.Skip)
            {
                if (!CurrentIsRead()) { SetReadMode(NarrativeReadMode.Manual); return; }
                AdvanceCore(frame); return;
            }
            if (_readMode != NarrativeReadMode.Auto || VoiceWaiting) return;
            _autoElapsed += delta;
            // 无配音：全文显示后，每秒 20 字、至少 0.5 秒，再加用户间隔。
            float minimum = _lineHasVoice ? 0 : Mathf.Max(.5f, (_pageEnd - _pageStart) / 20f);
            if (_autoElapsed >= (minimum + (_autoIntervalOverride ?? _autoInterval)) / ReadingMultiplier) AdvanceCore(frame);
        }
        private void AdvanceCore(long frame)
        {
            if (_textExiting) return;
            if (AdvanceTextPage(frame)) { Render(); RecordStableLine(); return; }
            if (BeginTextExit()) return;
            AdvanceRunnerCore(frame);
        }
        private void AdvanceRunnerCore(long frame)
        {
            var s = _runner.Snapshot;
            if (_runner.Advance(s.SessionGeneration, s.PositionVersion, frame) && s.State == NarrativeState.AwaitingAdvance)
            {
                // RunnerChanged may already have armed the NEXT line. Only stop the departing playback here.
                _audio.StopVoice();
                if (_runner.Snapshot.State != NarrativeState.Revealing) { _lineVoicePending = false; _lineVoice = null; }
            }
            Render(); RecordStableLine();
        }
        private sealed class PauseLease : IDisposable
        {
            private NovelSession _owner;
            private readonly string _key;
            public PauseLease(NovelSession owner, string key) { _owner = owner; _key = key; owner.Pause(key); }
            public void Dispose() { var owner = _owner; _owner = null; owner?.Resume(_key); }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public IDisposable AcquirePause(string reason) => new PauseLease(this, reason + "#" + ++_pauseSerial);

        /// <summary>
        /// 临时覆盖自动播放间隔（秒）；传 null 恢复玩家在设置里的间隔。
        /// 脚本化段落用它固定节奏，不受玩家自动间隔偏好影响（玩家把它设成 60 秒也不会拖慢开场）。
        /// </summary>
        internal void SetStoryAutoIntervalOverride(float? seconds)
        {
            _autoIntervalOverride = seconds.HasValue ? Mathf.Clamp(seconds.Value, 0f, 60f) : (float?)null;
            _autoElapsed = 0f;
            Notify();
        }
        public void ConfigureReading(Func<string, string, int, bool> isRead, float textSpeed, float autoInterval,
            float bgmVolume, float sfxVolume, float voiceVolume)
        {
            _isRead = isRead; _textSpeed = Mathf.Clamp(textSpeed, 1, 200); _autoInterval = Mathf.Clamp(autoInterval, 0, 60);
            _playerBgm = Mathf.Clamp01(bgmVolume); _playerSfx = Mathf.Clamp01(sfxVolume);
            _audio.SetVolumes(bgmVolume, sfxVolume, voiceVolume); RefreshLoopVolumes(); Notify();
        }
        public void SetReadingMultiplier(int multiplier)
        {
            if (_disposed || multiplier < 1 || multiplier > 3) return;
            ReadingMultiplier = multiplier; Notify(); Render();
        }
        public void SetReadMode(NarrativeReadMode mode)
        {
            if (_disposed || !IsReady || _pauses.Count > 0 && mode != NarrativeReadMode.Manual || !Enum.IsDefined(typeof(NarrativeReadMode), mode)) return;
            var s = _runner.Snapshot;
            if (!s.HasActiveSession || s.State == NarrativeState.AwaitingChoice) mode = NarrativeReadMode.Manual;
            if (mode == NarrativeReadMode.Skip && _runner.CurrentCommand?.Kind == NovelCommandKind.Say && !CurrentIsRead()) mode = NarrativeReadMode.Manual;
            _readMode = mode; _autoElapsed = 0;
            if (mode == NarrativeReadMode.Skip) StopVoice();
            Notify(); Render();
        }
        public void SetDialogueHidden(bool hidden)
        {
            if (_disposed || !IsReady || hidden && _pauses.Count > 0) return;
            DialogueHidden = hidden;
            if (!hidden) { _suppressReadingTick = true; _restoredUIFrame = Time.frameCount; }
            if (hidden) Pause("DialogueHidden"); else Resume("DialogueHidden");
            Render();
        }
        public void StopVoice()
        {
            bool changed = VoiceWaiting;
            _lineVoicePending = false; _lineVoice = null; _audio.StopVoice();
            if (changed && !_disposed) Notify();
        }
        #endregion
    }
}
