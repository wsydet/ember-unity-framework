using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Game.Narrative
{
    /// <summary>Gameplay 会话所有者。准备期间不启动运行器；异步结果只由当前会话主动消费。</summary>
    public sealed partial class NovelSession : INarrativeDiagnostics, IDisposable
    {
        #region 内部参数
        private static long _nextPreparingGeneration;
        private readonly long _preparingGeneration = -Interlocked.Increment(ref _nextPreparingGeneration);
        private readonly INovelResources _resources;
        private readonly string _entryChapterId, _entryNodeId;
        private readonly Func<NarrativeTableCatalog> _catalogProvider;
        private readonly INovelAudio _audio;
        private readonly List<IDisposable> _leases = new();
        private readonly Dictionary<(Type, string), IDisposable> _resourceCache = new();
        private readonly HashSet<string> _pauses = new(StringComparer.Ordinal);
        private readonly NarrativeRunner _runner = new();
        private INovelAssetLease<NarrativeStorySO> _story;
        private INovelAssetLease<Sprite> _sprite;
        private INovelAssetLease<AudioClip> _clip;
        private NarrativeTableCatalog _catalog;
        private INovelView _view;
        private NovelCommand _presenting;
        private NovelCommand _revealing;
        private bool _started, _disposed, _visualStarted;
        private float _visible, _transition;
        private long _presentationVersion;
        private NarrativeError _preparationError;
        public event Action Changed;
        public bool IsReady => _started && !_disposed && _view != null && !_restoring;
        public bool IsDisposed => _disposed;
        public int OwnedResourceCount => _leases.Count + MediaResourceCount;
        public NarrativeSnapshot Snapshot => _restoring ? RestoreSnapshot : _started ? ReadingSnapshot : new NarrativeSnapshot(
            _preparingGeneration, 0, null, null, null,
            _disposed ? NarrativeState.Cancelled : _preparationError != null ? NarrativeState.Faulted : NarrativeState.Preparing,
            _disposed || _preparationError != null ? NarrativeWait.None :
                (_catalog?.IsReady == true ? NarrativeWait.None : NarrativeWait.Table) |
                (_view == null ? NarrativeWait.Page : NarrativeWait.None) |
                (_story?.IsDone == true ? NarrativeWait.None : NarrativeWait.Resource),
            _pauses, new Dictionary<string, NovelValue>(), Array.Empty<NovelRoute>(), _preparationError, null);
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Notify()
        {
            if (Changed == null) return;
            foreach (Action observer in Changed.GetInvocationList())
                try { observer(); } catch (Exception ex) { Ember.Basic.EmberDebug.LogWarning("Game.Narrative", ex.Message); }
        }
        private TLease Own<TLease>(TLease lease) where TLease : IDisposable { _leases.Add(lease); return lease; }
        private INovelAssetLease<T> Load<T>(string path) where T : UnityEngine.Object
        {
            var key = (typeof(T), path);
            if (_resourceCache.TryGetValue(key, out var existing)) return (INovelAssetLease<T>)existing;
            var lease = Own(_resources.Load<T>(path)); _resourceCache.Add(key, lease); return lease;
        }
        private void Fail(string message)
        {
            CancelActions();
            ClearMedia();
            StopVoice();
            if (_started)
            {
                var s = _runner.Snapshot;
                _runner.ReportFailure(s.SessionGeneration, message);
            }
            else { _preparationError = new NarrativeError("PreparationFailed", message, null); Notify(); }
        }
        private void Render()
        {
            if (_view == null) return;
            var snapshot = Snapshot;
            var command = _runner.CurrentCommand;
            if (command?.Kind == NovelCommandKind.Say && !ReferenceEquals(_revealing, command))
            { _revealing = command; _visible = 0; }
            string speaker = "";
            if (command != null && _catalog != null && _catalog.TryGetCharacter(command.CharacterId, out var row)) speaker = NovelLocalization.CharacterName(command.CharacterId, row.DisplayName);
            string status = snapshot.Error?.ToString() ?? (snapshot.State == NarrativeState.Ended ? "结局 · " + snapshot.EndingId
                : !_started ? "正在准备配表、页面与剧情…" : snapshot.PauseReasons.Count > 0 ? "已暂停" : "");
            int visible = snapshot.State == NarrativeState.AwaitingAdvance ? int.MaxValue : (int)_visible;
            PrepareText(command, snapshot.State == NarrativeState.AwaitingAdvance);
            (_view as INovelTextView)?.SetStoryDialogueVisible(StoryDialogueVisible);
            _view.Render(snapshot, command, speaker, visible, status);
            RenderTextEffects();
        }
        private void Prepare()
        {
            _catalog = _catalogProvider();
            if (_pauses.Count > 0) return;
            if (_catalog?.IsReady != true || _view == null || _story?.IsDone != true) return;
            if (!_story.Asset) { Fail(_story.Error ?? "剧情资源加载失败"); return; }
            if (!_story.Asset.TryReadDefinition(_catalog, out var definition, out var errors))
            { _preparationError = errors[0]; Notify(); return; }
            _started = true;
            _runner.StartStory(definition, _catalog, _entryChapterId, _entryNodeId);
            foreach (string pause in _pauses) _runner.Pause(pause);
            Notify();
        }
        private void Present(float delta)
        {
            var snapshot = _runner.Snapshot;
            var command = _runner.CurrentCommand;
            if ((_runner.Snapshot.Wait & NarrativeWait.Presentation) == 0) return;
            if (command.Kind == NovelCommandKind.HideAllCharacters) { PresentHideAll(command, snapshot, delta); return; }
            if (command.Kind == NovelCommandKind.DialogueVisibility)
            { StoryDialogueVisible = command.DialogueVisible; _runner.CompletePresentation(snapshot.SessionGeneration, snapshot.PositionVersion); return; }
            if (NovelMediaRules.IsMedia(command.Kind) && (command.Kind != NovelCommandKind.BGM || _audio is INovelLoopAudio))
            { PresentMedia(command, snapshot); return; }
            if (NovelActorRules.IsAction(command.Kind) || command.Kind == NovelCommandKind.WaitActions)
            { PresentAction(command, snapshot); return; }
            if (command.Kind == NovelCommandKind.Emphasis)
            { SetEmphasis(command); _runner.CompletePresentation(snapshot.SessionGeneration, snapshot.PositionVersion); return; }
            if (_readMode == NarrativeReadMode.Skip && (command.Kind == NovelCommandKind.Voice || command.Kind == NovelCommandKind.SFX))
            { StopVoice(); _runner.CompletePresentation(snapshot.SessionGeneration, snapshot.PositionVersion); _presenting = null; return; }
            if (_presenting != command)
            {
                if (command.Kind == NovelCommandKind.Background) SceneChangeMedia();
                _presenting = command; _resolvedVisual = ResolveVisualCommand(command); _presentationVersion = snapshot.PositionVersion;
                _sprite = null; _clip = null; _transition = 0; _visualStarted = false;
                if (command.VisualAction == NovelVisualAction.Hide && (command.Kind == NovelCommandKind.Character || command.Kind == NovelCommandKind.Background))
                { _visualStarted = true; }
                else
                {
                    if (!_catalog.TryResolve(command.Kind, command.ResourceKey, out var path)) { Fail("配表资源键无法解析：" + command.ResourceKey); return; }
                    _runner.SetPresentationWait(snapshot.SessionGeneration, snapshot.PositionVersion, NarrativeWait.Resource);
                    if (command.Kind == NovelCommandKind.Character || command.Kind == NovelCommandKind.Background)
                        _sprite = Load<Sprite>(path);
                    else _clip = Load<AudioClip>(path);
                    // 即使 Provider 同步完成，也在下一帧消费结果，保持可取消的准备边界。
                    return;
                }
            }
            if (_sprite != null && !_sprite.IsDone || _clip != null && !_clip.IsDone) return;
            if (_sprite != null && !_sprite.Asset || _clip != null && !_clip.Asset)
            { Fail(_sprite?.Error ?? _clip?.Error ?? "演出资源不存在或类型错误"); return; }
            if (_clip != null)
            {
                if (!_visualStarted) { _audio.Play(command.Kind, _clip.Asset); _visualStarted = true; }
                if (command.Kind == NovelCommandKind.Voice && _audio.VoicePlaying && _readMode != NarrativeReadMode.Skip)
                { _runner.SetPresentationWait(snapshot.SessionGeneration, _presentationVersion, NarrativeWait.Voice); return; }
                if (command.Kind == NovelCommandKind.Voice && _readMode == NarrativeReadMode.Skip) StopVoice();
                if (command.Kind == NovelCommandKind.BGM) _bgmKey = command.ResourceKey;
                _runner.CompletePresentation(snapshot.SessionGeneration, _presentationVersion);
                _presenting = null; return;
            }
            if (!_visualStarted)
            { _visualStarted = true; _runner.SetPresentationWait(snapshot.SessionGeneration, _presentationVersion, NarrativeWait.Transition); }
            else if ((snapshot.Wait & NarrativeWait.Transition) == 0)
                _runner.SetPresentationWait(snapshot.SessionGeneration, _presentationVersion, NarrativeWait.Transition);
            _transition += delta;
            float progress = command.Duration <= 0 || _readMode == NarrativeReadMode.Skip ? 1 : Mathf.Clamp01(_transition / command.Duration);
            float visualProgress = command.VisualAction == NovelVisualAction.Hide
                ? 1 - _visualFromOpacity * (1 - progress) : Mathf.Lerp(_visualFromOpacity, 1, progress);
            PrepareActorVisual();
            // An empty logical slot can share a physical root with an actor that moved away.
            // Preserve the old Hide duration, but never render that Hide over the moved actor.
            if (!_emptyActorHide || !_visualStates.Exists(v => v.Kind == NovelCommandKind.Character && v.Slot == _resolvedVisual.Slot))
                _view.Visual(_resolvedVisual, _sprite?.Asset, visualProgress);
            if (_incomingActor != null) ApplyActor(_incomingActor);
            if (progress >= 1)
            {
                RememberVisual(_resolvedVisual); _pendingOldVisual = null; PruneVisualResources();
                _runner.CompletePresentation(snapshot.SessionGeneration, _presentationVersion);
                _presenting = null;
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelSession(NovelNewGameRequest request, Func<NarrativeTableCatalog> catalogProvider, INovelResources resources, INovelAudio audio = null)
        {
            _audio = audio ?? new NovelAudio();
            _storyPath = request.StoryPath;
            _entryChapterId = request.ChapterId; _entryNodeId = request.NodeId;
            _resources = resources; _catalogProvider = catalogProvider;
            _runner.Changed += RunnerChanged;
            _audio.VoiceCompleted += Notify;
            try { _story = Own(resources is INovelStoryResources stories ? stories.LoadStory(request.StoryPath, request.StoryId) : resources.Load<NarrativeStorySO>(request.StoryPath)); }
            catch (Exception ex) { Fail(ex.Message); }
        }
        public void AttachView(INovelView view) { if (_disposed) return; _view = view; Resume("PageUnavailable"); Render(); }
        public void ReportFailure(string message) { if (!_disposed) Fail(message); }
        public void DetachView(INovelView view) { if (ReferenceEquals(_view, view)) { _view.ClearVisuals(); _view = null; Pause("PageUnavailable"); } }
        public void Pause(string reason) { if (_disposed || !_pauses.Add(reason)) return; if (_started) _runner.Pause(reason); _audio.SetPaused(true); PauseMedia(true); Notify(); }
        public void Resume(string reason) { if (_disposed || !_pauses.Remove(reason)) return; if (_started) _runner.Resume(reason); _audio.SetPaused(_pauses.Count > 0); PauseMedia(_pauses.Count > 0); Notify(); }
        public void Advance(long frame)
        {
            if (DialogueHidden) { SetDialogueHidden(false); _restoredUIFrame = frame; return; }
            if (frame == _restoredUIFrame) return;
            if (!IsReady || _pauses.Count > 0) return;
            SetReadMode(NarrativeReadMode.Manual); AdvanceCore(frame);
        }
        public void Choose(long generation, long position, string option, long frame)
        {
            if (!IsReady || _pauses.Count > 0 || frame == _restoredUIFrame) return;
            _runner.Choose(generation, position, option, frame); Render();
        }
        public void Tick(float delta, long frame)
        {
            if (_disposed || _preparationError != null) { Render(); return; }
            try
            {
                if (_restoring) { PrepareRestore(); return; }
                if (!_started) { Prepare(); Render(); return; }
                if (_view == null || _pauses.Count > 0) { Render(); return; }
                if (_suppressReadingTick) { _suppressReadingTick = false; Render(); return; }
                TickMedia(Mathf.Max(0, delta));
                if (!_runner.Snapshot.HasActiveSession) { Render(); return; }
                TickActions(Mathf.Max(0, delta) * ReadingMultiplier);
                _audio.SetPaused(false);
                _audio.Tick();
                if (TickTextExit(Mathf.Max(0, delta), frame)) return;
                var snapshot = _runner.Snapshot;
                if (snapshot.State == NarrativeState.Revealing || snapshot.State == NarrativeState.AwaitingAdvance) TickLineVoice();
                if (snapshot.State == NarrativeState.Revealing)
                {
                    if (_revealing != _runner.CurrentCommand) { _revealing = _runner.CurrentCommand; _visible = 0; Render(); }
                    TickText(Mathf.Max(0, delta), frame);
                }
                else if ((snapshot.Wait & NarrativeWait.Presentation) != 0) Present(Mathf.Max(0, delta) * ReadingMultiplier);
                else if (snapshot.Wait == NarrativeWait.Timer) _runner.Tick(snapshot.SessionGeneration, _readMode == NarrativeReadMode.Skip ? float.MaxValue : Mathf.Max(0, delta) * ReadingMultiplier, frame);
                Render();
                RecordStableLine();
                // 完整显示和推进分属不同帧，保留首个稳定点供自动保存消费。
                if (snapshot.State == NarrativeState.AwaitingAdvance) TickReading(Mathf.Max(0, delta), frame);
            }
            catch (Exception ex) { Fail(ex.Message); }
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CancelActions();
            ClearMedia();
            StopVoice();
            void Release(Action action)
            {
                try { action(); }
                catch (Exception ex) { Ember.Basic.EmberDebug.LogWarning("Game.Narrative", "会话释放：" + ex.Message); }
            }
            Release(() => _runner.Cancel()); _audio.VoiceCompleted -= Notify; Release(_audio.Dispose);
            var view = _view; _view = null;
            if (view != null) Release(view.ClearVisuals);
            foreach (var lease in _leases) Release(lease.Dispose);
            _leases.Clear(); _resourceCache.Clear(); _story = null; _sprite = null; _clip = null;
            _runner.Changed -= RunnerChanged; Notify(); Changed = null; LineRead = null;
        }
        #endregion
    }
}
