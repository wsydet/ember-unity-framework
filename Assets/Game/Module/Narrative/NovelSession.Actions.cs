using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Narrative
{
    public sealed partial class NovelSession
    {
        #region 内部参数
        private readonly Dictionary<string, NovelActionHandle> _actions = new(StringComparer.Ordinal);
        private readonly List<NovelActionHandle> _runningActions = new();
        private NovelActionHandle[] _actionWait;
        private long _actionSequence;
        private string _waitingActions;
        private NovelCommand _resolvedVisual;
        private NovelVisualState _pendingOldVisual;
        private float _stageOpacity = 1, _visualFromOpacity;
        public IReadOnlyList<NovelActionHandle> Actions => _actions.Values.ToList().AsReadOnly();
        private IReadOnlyList<string> ActionObservation => _runningActions.Select(a =>
            $"{a.Id} #{a.Sequence} · {a.TargetKind}/{a.TargetId} · {a.Property} · {a.Progress:P0}")
            .Concat(_effects.Values.Select(e => $"效果 {e.State.Id} · {e.State.Key} · {(e.State.Persistent ? "持续" : "一次性")}"))
            .Concat(_loops.Values.Select(l => $"声音 {(l.State.Bgm ? "BGM" : l.State.Id)} · {l.State.Key} · 剧情音量 {l.Gain:0.00}"))
            .ToList().AsReadOnly();
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private string VisualId(NovelCommand command) => command.Kind == NovelCommandKind.Background ? "background" :
            string.IsNullOrEmpty(command.InstanceId) ? "legacy-" + command.Slot : command.InstanceId;

        private NovelCommand ResolveVisualCommand(NovelCommand command)
        {
            _incomingActor = null; _actorVisualPrepared = false; _emptyActorHide = false;
            _pendingOldVisual = command.Kind == NovelCommandKind.Character ?
                (string.IsNullOrEmpty(command.InstanceId) ? Occupant(command.Slot) : Target(NovelTargetKind.Character, command.InstanceId))?.Copy() :
                command.Kind == NovelCommandKind.Background ? Target(NovelTargetKind.Background, "background")?.Copy() : null;
            if (command.Kind == NovelCommandKind.Character) return ResolveActorVisual(command);
            if (command.Kind != NovelCommandKind.Background) return command;
            var old = Target(NovelTargetKind.Background, "background");
            _visualFromOpacity = command.VisualAction == NovelVisualAction.Hide ? old?.Opacity ?? 1 : 0;
            CancelTarget(NovelTargetKind.Background, "background");
            return new NovelCommand(command.CommandId, command.Kind, resourceKey: command.ResourceKey,
                visualAction: command.VisualAction, instanceId: "background");
        }

        private NovelVisualState Target(NovelTargetKind kind, string id)
        {
            var visualKind = kind == NovelTargetKind.Background ? NovelCommandKind.Background : NovelCommandKind.Character;
            return _visualStates.FirstOrDefault(v => v.Kind == visualKind &&
                (kind == NovelTargetKind.Background || v.InstanceId == id));
        }
        private void WriteOpacity(NovelActionHandle action, float value)
        {
            if (_disposed || _view == null) return;
            if (_view is not INovelOpacityView view) throw new InvalidOperationException("页面未提供透明度演出适配");
            if (action.TargetKind == NovelTargetKind.Stage)
            { _stageOpacity = value; view.SetOpacity(action.TargetKind, default, value); return; }
            var target = Target(action.TargetKind, action.TargetId);
            if (target == null) { action.Status = NovelActionStatus.Cancelled; return; }
            target.Opacity = value; view.SetOpacity(action.TargetKind, target.Slot, value);
        }
        private void CancelTarget(NovelTargetKind kind, string id, string property = null)
        {
            foreach (var action in _runningActions)
                if (action.TargetKind == kind && action.TargetId == id && (property == null || action.Property == property))
                { ResetGesture(action); action.Status = NovelActionStatus.Cancelled; }
            _runningActions.RemoveAll(a => a.IsFinished);
            PruneVisualResources();
        }
        private void CancelActions()
        {
            foreach (var action in _runningActions) { ResetGesture(action); action.Status = NovelActionStatus.Cancelled; }
            _runningActions.Clear(); _actionWait = null; _waitingActions = null;
        }
        private void FinishActions()
        {
            foreach (var action in _runningActions)
            {
                if (action.IsFinished) continue;
                WriteAction(action, 1);
                if (!action.IsFinished) { action.Progress = 1; action.Status = NovelActionStatus.Completed; }
            }
            _runningActions.Clear(); PruneVisualResources();
        }
        private void TickActions(float delta)
        {
            if (_runningActions.Count == 0) return;
            if (_readMode == NarrativeReadMode.Skip) { FinishActions(); Notify(); return; }
            foreach (var action in _runningActions)
            {
                if (action.IsFinished) continue;
                action.Elapsed += delta;
                if (action.Elapsed < action.Delay) continue;
                action.Progress = action.Duration <= 0 ? 1 : Mathf.Clamp01((action.Elapsed - action.Delay) / action.Duration);
                WriteAction(action, action.Progress);
                if (!action.IsFinished && action.Progress >= 1) action.Status = NovelActionStatus.Completed;
            }
            _runningActions.RemoveAll(a => a.IsFinished);
            PruneVisualResources();
            Notify();
        }
        private void PresentAction(NovelCommand command, NarrativeSnapshot snapshot)
        {
            if (!ReferenceEquals(_presenting, command))
            {
                if ((command.Kind == NovelCommandKind.CrossFade || command.Kind == NovelCommandKind.Wipe) && !PrepareCrossFade(command, snapshot)) return;
                _presenting = command; _presentationVersion = snapshot.PositionVersion;
                if (NovelActorRules.IsAction(command.Kind))
                {
                    var action = StartAction(command, snapshot);
                    _actionWait = command.Parallel ? Array.Empty<NovelActionHandle>() : new[] { action };
                    TickActions(0);
                }
                else
                {
                    _actionWait = command.WaitActions.Select(id => _actions.TryGetValue(id, out var action) ? action :
                        throw new InvalidOperationException("等待的动作尚未启动：" + id)).ToArray();
                }
                _waitingActions = string.Join(", ", _actionWait.Select(a => a.Id + "#" + a.Sequence));
            }
            if (_actionWait.All(a => a.IsFinished))
            {
                _presenting = null; _actionWait = null; _waitingActions = null;
                _runner.CompletePresentation(snapshot.SessionGeneration, _presentationVersion);
            }
            else _runner.SetPresentationWait(snapshot.SessionGeneration, _presentationVersion, NarrativeWait.Actions);
        }
        #endregion
    }
}
