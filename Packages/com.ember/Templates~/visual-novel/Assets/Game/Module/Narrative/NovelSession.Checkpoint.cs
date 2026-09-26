using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Narrative
{
    public sealed partial class NovelSession
    {
        #region 内部参数
        private string _storyPath, _bgmKey;
        private readonly List<NovelVisualState> _visualStates = new();
        private readonly List<NovelHistoryEntry> _history = new();
        private readonly List<(NovelVisualState state, INovelAssetLease<Sprite> lease)> _restoreSprites = new();
        private INovelAssetLease<AudioClip> _restoreBgm;
        private NovelCheckpoint _restore;
        private bool _restoring, _restoreLoaded;
        private long _recordedPosition = -1;
        public bool RestoreReady { get; private set; }
        public long AutoSaveRevision => _runner.AutoSaveRevision;
        public event Action<NovelHistoryEntry> LineRead;
        public INovelView View => _view;
        private NarrativeSnapshot RestoreSnapshot => new(_preparingGeneration, 0, _restore?.ChapterId, _restore?.NodeId,
            _restore?.CommandId, _disposed ? NarrativeState.Cancelled : _preparationError == null ? NarrativeState.Restoring : NarrativeState.Faulted,
            _disposed || RestoreReady || _preparationError != null ? NarrativeWait.None : NarrativeWait.Resource | NarrativeWait.Table,
            _pauses, new Dictionary<string, NovelValue>(), Array.Empty<NovelRoute>(), _preparationError, null,
            storyId: _restore?.StoryId);
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void RememberVisual(NovelCommand command)
        {
            if (_emptyActorHide && command.Kind == NovelCommandKind.Character) return;
            if (command.Kind == NovelCommandKind.Character)
            {
                var prior = _visualStates.FirstOrDefault(v => v.Kind == command.Kind && v.Slot == command.Slot);
                if (command.VisualAction == NovelVisualAction.Hide)
                {
                    if (prior != null) { StopBoundEffects(prior.InstanceId); _visualStates.Remove(prior); }
                    (_view as INovelActorView)?.ResetActor(command.Slot);
                }
                else
                {
                    if (prior != null && prior != _incomingActor) _visualStates.Remove(prior);
                    if (!_visualStates.Contains(_incomingActor)) _visualStates.Add(_incomingActor);
                    _incomingActor.Opacity = 1; ApplyActor(_incomingActor);
                }
                RefreshEmphasis(); return;
            }
            _visualStates.RemoveAll(v => v.Kind == command.Kind && (v.Kind == NovelCommandKind.Background || v.Slot == command.Slot));
            if (command.VisualAction != NovelVisualAction.Hide)
                _visualStates.Add(new NovelVisualState { Kind = command.Kind, Slot = command.Slot, Key = command.ResourceKey, InstanceId = command.InstanceId, Opacity = 1 });
        }
        private void RecordStableLine()
        {
            var s = _runner.Snapshot;
            if (s.State != NarrativeState.AwaitingAdvance || s.PositionVersion == _recordedPosition) return;
            _recordedPosition = s.PositionVersion;
            var c = _runner.CurrentCommand;
            var entry = new NovelHistoryEntry { ChapterId = s.ChapterId, NodeId = s.NodeId, CommandId = c.CommandId,
                LineId = c.LineId, TextRevision = c.TextRevision, Text = c.Text, Speaker = c.CharacterId,
                SpeakerNameKey = c.SpeakerNameKey, SpeakerVariableId = c.SpeakerVariableId,
                SpeakerVariableScope = c.SpeakerVariableScope,
                TextKey = c.TextKey, TextHasBindings = c.TextBindings.Count > 0 };
            _history.Add(entry); if (_history.Count > 200) _history.RemoveAt(0);
            LineRead?.Invoke(entry);
        }
        private void PrepareRestore()
        {
            if (RestoreReady || _preparationError != null) return;
            _catalog = _catalogProvider();
            if (_catalog?.IsReady != true || _story?.IsDone != true) return;
            try
            {
                if (!_restoreLoaded)
                {
                    if (!_story.Asset) throw new InvalidOperationException(_story.Error ?? "剧情资源缺失");
                    if (!_story.Asset.TryReadDefinition(_catalog, out var definition, out var issues)) throw new InvalidOperationException(issues[0].ToString());
                    if (!_runner.TryRestore(definition, _catalog, _restore, out var error)) throw new InvalidOperationException(error);
                    _definition = definition;
                    // 历史只做结构与规模检查：条目里的 Speaker 与 SpeakerNameKey 都是表现层字段，
                    // 由读时解析（ResolveSpeaker）处理。旧档缺 SpeakerNameKey 即为空，行为与接入前一致，
                    // 所以不推进 SchemaVersion、也不加版本迁移分支。
                    if (_restore.Visuals == null || _restore.Visuals.Count > 4 || _restore.History == null || _restore.History.Count > 200 || _restore.History.Any(h => h == null) || !_restore.BgmLoop)
                        throw new InvalidOperationException("存档演出或历史格式不支持");
                    if (_restore.SchemaVersion == 1)
                    {
                        _restore.StageOpacity = 1; _restore.Actions = new(); _restore.Effects = new();
                        foreach (var v in _restore.Visuals)
                        {
                            if (v == null) continue;
                            v.InstanceId = v.Kind == NovelCommandKind.Background ? "background" : "legacy-" + v.Slot;
                            v.Opacity = 1; v.Scale = Vector3.one; v.Offset = Vector2.zero; v.Rotation = 0;
                        }
                    }
                    if (!NovelActionHandle.ValidTime(_restore.StageOpacity) || _restore.StageOpacity > 1 ||
                        _restore.Actions == null || _restore.Actions.Count > 4096 || _restore.Effects == null || _restore.Effects.Count != 0)
                        throw new InvalidOperationException("存档演出扩展格式不支持");
                    var actionIds = new HashSet<string>();
                    foreach (var a in _restore.Actions)
                        if (a == null || string.IsNullOrWhiteSpace(a.Id) || !actionIds.Add(a.Id) ||
                            (a.Status != NovelActionStatus.Completed && a.Status != NovelActionStatus.Cancelled))
                            throw new InvalidOperationException("存档动作状态无效");
                    if (_restore.SchemaVersion < 3)
                    {
                        if (_restore.SchemaVersion == 2 && _restore.Visuals.Any(v => v != null &&
                            (v.Offset != Vector2.zero || v.Scale != Vector3.one || v.Rotation != 0)))
                            throw new InvalidOperationException("旧版存档包含不支持的人物变换");
                        _restore.EmphasisMode = NovelEmphasisMode.Off; _restore.EmphasisInstance = null; _restore.DimFactor = .55f;
                        foreach (var v in _restore.Visuals)
                        {
                            if (v == null) continue;
                            v.NamedSlot = (int)v.Slot; v.Brightness = 1; v.Mirror = false; v.Layer = 0;
                            v.Offset = Vector2.zero; v.Scale = Vector3.one; v.Rotation = 0;
                        }
                    }
                    if (_restore.SchemaVersion < 6) { _restore.CameraZoom = 1; _restore.CameraOffset = Vector2.zero; }
                    if (!NovelCameraRules.Valid(_restore.CameraOffset, _restore.CameraZoom)) throw new InvalidOperationException("存档舞台镜头参数无效");
                    if (_restore.SchemaVersion < 4) { _restore.CoverColor = Color.clear; _restore.CoverWholeReader = false; }
                    if (!NovelScreenRules.ValidColor(_restore.CoverColor)) throw new InvalidOperationException("存档遮罩颜色无效");
                    if (!Enum.IsDefined(typeof(NovelEmphasisMode), _restore.EmphasisMode) ||
                        !NovelActionHandle.ValidTime(_restore.DimFactor) || _restore.DimFactor > 1)
                        throw new InvalidOperationException("存档强调参数无效");
                    var namedSlots = new HashSet<int>();
                    var instances = new HashSet<string>();
                    var slots = new HashSet<string>();
                    foreach (var visual in _restore.Visuals)
                    {
                        if (visual == null || (visual.Kind != NovelCommandKind.Background && visual.Kind != NovelCommandKind.Character) ||
                            !Enum.IsDefined(typeof(NovelPortraitSlot), visual.Slot) || !slots.Add(visual.Kind + ":" + (visual.Kind == NovelCommandKind.Background ? 0 : (int)visual.Slot)))
                            throw new InvalidOperationException("存档演出槽位无效或重复");
                        if (string.IsNullOrWhiteSpace(visual.InstanceId) || !instances.Add(visual.Kind + ":" + visual.InstanceId) ||
                            visual.Kind == NovelCommandKind.Background && visual.InstanceId != "background" ||
                            !NovelActionHandle.ValidTime(visual.Opacity) || visual.Opacity > 1 || !NovelActorRules.Scaling(visual.Scale) || visual.Scale.z != 1 ||
                            !NovelActorRules.Finite(visual.Offset.x) || !NovelActorRules.Finite(visual.Offset.y) ||
                            Mathf.Abs(visual.Offset.x) > 3 || Mathf.Abs(visual.Offset.y) > 3 ||
                            !NovelActorRules.Finite(visual.Rotation) || Mathf.Abs(visual.Rotation) > 3600 ||
                            visual.Layer < -100 || visual.Layer > 100 || !NovelActionHandle.ValidTime(visual.Brightness) || visual.Brightness > 1 ||
                            visual.NamedSlot < -1 || visual.NamedSlot > 2 ||
                            visual.Kind == NovelCommandKind.Character && visual.NamedSlot >= 0 && !namedSlots.Add(visual.NamedSlot))
                            throw new InvalidOperationException("存档实例、透明度或变换不支持");
                        if (!_catalog.TryResolve(visual.Kind, visual.Key, out var path)) throw new InvalidOperationException("存档演出资源键缺失：" + visual.Key);
                        if (visual.Kind == NovelCommandKind.Character) visual.CharacterId = _catalog.Portraits.FirstOrDefault(p => p.Id == visual.Key)?.CharacterId;
                        _restoreSprites.Add((visual, Load<Sprite>(path)));
                    }
                    PrepareMediaRestore();
                    // 有常驻 BGM 通道时曲目资源由通道自己加载与持有（尾段要活过会话销毁），
                    // 这里只保留没有通道的旧路径：把单文件 BGM 当成一条循环音恢复。
                    if (_bgm == null && !string.IsNullOrEmpty(_restore.BgmKey) && !_restore.Loops.Exists(l => l != null && l.Bgm))
                    {
                        if (!_catalog.TryResolve(NovelCommandKind.BGM, _restore.BgmKey, out var path)) throw new InvalidOperationException("存档 BGM 键缺失");
                        _restoreBgm = Load<AudioClip>(path);
                    }
                    _restoreLoaded = true; return;
                }
                if (_restoreSprites.Any(s => !s.lease.IsDone) || (_restoreBgm != null && !_restoreBgm.IsDone)) return;
                foreach (var item in _restoreSprites) if (!item.lease.Asset) throw new InvalidOperationException(item.lease.Error ?? "恢复立绘/背景失败");
                if (_restoreBgm != null && !_restoreBgm.Asset) throw new InvalidOperationException(_restoreBgm.Error ?? "恢复 BGM 失败");
                if (!MediaRestoreReady()) return;
                RestoreReady = true; Notify();
            }
            catch (Exception ex) { ClearMedia(); _preparationError = new NarrativeError("RestoreFailed", ex.Message, _restore?.ChapterId); Notify(); }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelSession(NovelCheckpoint checkpoint, Func<NarrativeTableCatalog> catalogProvider, INovelResources resources, INovelAudio audio = null, INovelBgmChannel bgm = null)
            : this(new NovelNewGameRequest(checkpoint.StoryPath, storyId: checkpoint.StoryId), catalogProvider, resources, audio, bgm)
        {
            // Deep copy prevents callers mutating an in-flight restore.
            _restore = JsonUtility.FromJson<NovelCheckpoint>(JsonUtility.ToJson(checkpoint)); _restoring = true;
        }
        public bool TryCapture(out NovelCheckpoint checkpoint, out string error)
        {
            checkpoint = null; error = "会话尚未就绪或正在恢复";
            if (_textExiting) { error = "章节卡正在渐隐，请等待过渡完成"; return false; }
            if (!IsReady || !_runner.TryCapture(out checkpoint, out error)) return false;
            RecordStableLine();
            checkpoint.StoryPath = _storyPath; checkpoint.BgmKey = _bgmKey;
            checkpoint.Visuals = _visualStates.Select(v => v.Copy()).ToList();
            foreach (var v in checkpoint.Visuals) { v.GestureOffset = Vector2.zero; v.GestureRotation = 0; }
            checkpoint.EmphasisMode = _emphasisMode; checkpoint.EmphasisInstance = _emphasisInstance; checkpoint.DimFactor = _dimFactor;
            checkpoint.StageOpacity = _stageOpacity;
            checkpoint.CameraZoom = _cameraZoom; checkpoint.CameraOffset = _cameraOffset;
            checkpoint.CoverColor = _coverColor; checkpoint.CoverWholeReader = _coverWholeReader;
            foreach (var action in _runningActions)
            {
                if (action.IsFinished) continue;
                if (action.Kind == NovelCommandKind.Camera)
                { checkpoint.CameraZoom = action.To; checkpoint.CameraOffset = action.VectorTo; }
                else if (action.Kind == NovelCommandKind.Cover || action.Kind == NovelCommandKind.Flash)
                {
                    checkpoint.CoverColor = action.Kind == NovelCommandKind.Flash ? action.ColorFrom : action.ColorTo;
                    checkpoint.CoverWholeReader = action.Kind == NovelCommandKind.Flash ? action.PreviousWholeReader : action.WholeReader;
                }
                else if (action.TargetKind == NovelTargetKind.Stage && action.Kind == NovelCommandKind.Opacity) checkpoint.StageOpacity = action.To;
                else
                {
                    var target = checkpoint.Visuals.FirstOrDefault(v => v.Kind == (action.TargetKind == NovelTargetKind.Background ?
                        NovelCommandKind.Background : NovelCommandKind.Character) && v.InstanceId == action.TargetId);
                    if (target != null)
                    {
                        switch (action.Kind)
                        {
                            case NovelCommandKind.Opacity: target.Opacity = action.To; break;
                            case NovelCommandKind.Move:
                                target.Offset = action.VectorTo;
                                if (action.ExitAfterMove) checkpoint.Visuals.Remove(target); break;
                            case NovelCommandKind.Scale: target.Scale = new Vector3(action.VectorTo.x, action.VectorTo.y, 1); break;
                            case NovelCommandKind.Rotate: target.Rotation = action.To; break;
                            case NovelCommandKind.Mirror: target.Mirror = action.To != 0; break;
                            case NovelCommandKind.Layer: target.Layer = (int)action.To; break;
                        }
                    }
                }
            }
            CaptureMedia(checkpoint);
            checkpoint.Actions = _actions.Values.Select(a => new NovelSavedAction { Id = a.Id,
                Status = a.Status == NovelActionStatus.Running ? NovelActionStatus.Completed : a.Status }).ToList();
            checkpoint.History = _history.Select(h => new NovelHistoryEntry { ChapterId = h.ChapterId, NodeId = h.NodeId,
                CommandId = h.CommandId, LineId = h.LineId, TextRevision = h.TextRevision, Text = h.Text, Speaker = h.Speaker,
                SpeakerNameKey = h.SpeakerNameKey, SpeakerVariableId = h.SpeakerVariableId,
                SpeakerVariableScope = h.SpeakerVariableScope }).ToList();
            return true;
        }
        /// <summary>Owner releases old session BEFORE this boundary. Failure after it returns to menu.</summary>
        public void CommitRestore(INovelView view)
        {
            if (_disposed || !RestoreReady || !_restoring || view == null) throw new InvalidOperationException("恢复尚未准备完成");
            if (view is not INovelActorView && _restore.Visuals.Any(v => v.Kind == NovelCommandKind.Character &&
                (v.Offset != Vector2.zero || v.Scale != Vector3.one || v.Rotation != 0 || v.Mirror || v.Layer != 0 || v.Brightness != 1)))
                throw new InvalidOperationException("恢复页面不支持人物变换");
            _emphasisMode = _restore.EmphasisMode; _emphasisInstance = _restore.EmphasisInstance; _dimFactor = _restore.DimFactor;
            if (view is not INovelCameraView && (_restore.CameraZoom != 1 || _restore.CameraOffset != Vector2.zero))
                throw new InvalidOperationException("恢复页面不支持舞台镜头");
            _view = view; _view.ClearVisuals();
            _cameraZoom = _restore.CameraZoom; _cameraOffset = _restore.CameraOffset;
            (view as INovelCameraView)?.SetCamera(_cameraOffset, _cameraZoom);
            foreach (var item in _restoreSprites)
            {
                _view.Visual(new NovelCommand("restore", item.state.Kind, resourceKey: item.state.Key, slot: item.state.Slot, instanceId: item.state.InstanceId), item.lease.Asset, 1);
                _visualStates.Add(item.state);
                if (item.state.Kind == NovelCommandKind.Character) ApplyActor(item.state);
                if (_view is INovelOpacityView opacityView)
                    opacityView.SetOpacity(item.state.Kind == NovelCommandKind.Background ? NovelTargetKind.Background : NovelTargetKind.Character,
                        item.state.Slot, item.state.Opacity);
                else if (item.state.Opacity != 1) throw new InvalidOperationException("恢复页面不支持透明度");
            }
            _stageOpacity = _restore.StageOpacity;
            if (_restore.CoverColor.a > 0 && view is not INovelScreenView) throw new InvalidOperationException("恢复页面不支持遮罩");
            SetCover(_restore.CoverColor, _restore.CoverWholeReader);
            RefreshEmphasis();
            if (_view is INovelOpacityView stageView) stageView.SetOpacity(NovelTargetKind.Stage, default, _stageOpacity);
            else if (_stageOpacity != 1) throw new InvalidOperationException("恢复页面不支持舞台透明度");
            foreach (var a in _restore.Actions) _actions.Add(a.Id, new NovelActionHandle { Id = a.Id, Status = a.Status,
                Generation = _runner.Snapshot.SessionGeneration, Sequence = ++_actionSequence, Progress = 1 });
            CommitMediaRestore();
            // 没有常驻通道时（编辑器试播、单测）BGM 仍按单文件循环音恢复；有通道的情况已在
            // CommitBgmRestore 里处理，并遵守「同曲同段不动、否则从新曲目前奏开始」。
            if (_restoreBgm != null) _audio.Play(NovelCommandKind.BGM, _restoreBgm.Asset);
            _restoreSprites.Clear();
            _bgmKey = _restore.BgmKey; _bgmVolume = Mathf.Clamp01(_restore.BgmVolume); _history.AddRange(_restore.History);
            _recordedPosition = _runner.Snapshot.PositionVersion; // repaint/restore must not append history or mark revised text read
            _started = true; _restoring = false;
            foreach (var pause in _pauses) _runner.Pause(pause);
            _audio.SetPaused(_pauses.Count > 0); Render(); Notify();
        }
        #endregion
    }
}
