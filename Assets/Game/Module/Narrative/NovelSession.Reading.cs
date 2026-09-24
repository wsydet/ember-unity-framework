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
            TextRevision = h.TextRevision, Text = h.Text, Speaker = ResolveSpeaker(h.Speaker) }).ToList().AsReadOnly();
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
        private string ResolveSpeaker(string key) => _catalog != null && _catalog.TryGetCharacter(key, out var row) ? row.DisplayName : key;
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
            if (_autoElapsed >= (minimum + _autoInterval) / ReadingMultiplier) AdvanceCore(frame);
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
