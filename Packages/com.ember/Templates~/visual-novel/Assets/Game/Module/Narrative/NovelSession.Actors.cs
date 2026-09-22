using System;
using System.Linq;
using UnityEngine;

namespace Game.Narrative
{
    public sealed partial class NovelSession
    {
        #region 内部参数
        private NovelVisualState _incomingActor;
        private bool _actorVisualPrepared, _emptyActorHide;
        private NovelEmphasisMode _emphasisMode;
        private string _emphasisInstance;
        private float _dimFactor = .55f;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private Vector2 NamedPosition(NovelPortraitSlot slot) => _view is INovelActorView actorView ? actorView.NamedPosition(slot) :
            new Vector2(.26f + (int)slot * .24f, slot == NovelPortraitSlot.Center ? .395f : .335f);
        private NovelVisualState Occupant(NovelPortraitSlot slot) => _visualStates.FirstOrDefault(v =>
            v.Kind == NovelCommandKind.Character && v.NamedSlot == (int)slot);
        private NovelCommand ResolveActorVisual(NovelCommand command)
        {
            var existing = string.IsNullOrEmpty(command.InstanceId) ? Occupant(command.Slot) : Target(NovelTargetKind.Character, command.InstanceId);
            bool preserve = command.VisualAction != NovelVisualAction.Hide && existing != null &&
                command.VisualAction == NovelVisualAction.Replace && !string.IsNullOrEmpty(command.InstanceId);
            if (!string.IsNullOrEmpty(command.InstanceId) && command.VisualAction != NovelVisualAction.Show && existing == null)
                throw new InvalidOperationException("人物实例不存在：" + command.InstanceId);
            if (existing == null && command.VisualAction == NovelVisualAction.Hide)
            {
                _emptyActorHide = true; _visualFromOpacity = 1;
                return new NovelCommand(command.CommandId, NovelCommandKind.Character, slot: command.Slot,
                    visualAction: NovelVisualAction.Hide);
            }
            if (command.VisualAction == NovelVisualAction.Show && !string.IsNullOrEmpty(command.InstanceId))
            {
                var occupant = command.PositionMode == NovelPositionMode.Named ? Occupant(command.Slot) : null;
                if (occupant != null && occupant != existing) throw new InvalidOperationException("命名位置已占用：" + command.Slot);
                if (existing != null && (command.PositionMode != NovelPositionMode.Named || existing.NamedSlot != (int)command.Slot))
                    throw new InvalidOperationException("请用 Move 移动已有实例：" + command.InstanceId);
            }
            var slot = existing?.Slot ?? command.Slot;
            if (existing == null && _visualStates.Any(v => v.Kind == NovelCommandKind.Character && v.Slot == slot))
            {
                var free = Enumerable.Range(0, 3).Where(i => !_visualStates.Any(v => v.Kind == NovelCommandKind.Character && (int)v.Slot == i)).ToArray();
                if (free.Length == 0) throw new InvalidOperationException("人物渲染槽已满（最多三个实例）");
                slot = (NovelPortraitSlot)free[0];
            }
            string id = string.IsNullOrEmpty(command.InstanceId) ? existing?.InstanceId ?? VisualId(command) : command.InstanceId;
            if (existing == null && string.IsNullOrEmpty(command.InstanceId))
                while (Target(NovelTargetKind.Character, id) != null) id += "-new";
            _visualFromOpacity = command.VisualAction == NovelVisualAction.Hide ? existing?.Opacity ?? 1 : preserve ? existing.Opacity : 0;
            if (existing != null) CancelTarget(NovelTargetKind.Character, existing.InstanceId, preserve ? "Opacity" : null);
            if (preserve) CancelTarget(NovelTargetKind.Character, existing.InstanceId, "CrossFade");
            if (command.VisualAction != NovelVisualAction.Hide)
            {
                _incomingActor = preserve ? existing : new NovelVisualState { Kind = NovelCommandKind.Character, Slot = slot,
                    InstanceId = id, NamedSlot = command.PositionMode == NovelPositionMode.Named ? (int)command.Slot : -1,
                    Offset = (command.PositionMode == NovelPositionMode.Named ? NamedPosition(command.Slot) : command.Position) - NamedPosition(slot) };
                _incomingActor.Key = command.ResourceKey;
                _incomingActor.CharacterId = _catalog.Portraits.FirstOrDefault(p => p.Id == command.ResourceKey)?.CharacterId;
            }
            return new NovelCommand(command.CommandId, NovelCommandKind.Character, resourceKey: command.ResourceKey,
                slot: slot, visualAction: command.VisualAction, instanceId: id);
        }
        private void PrepareActorVisual()
        {
            if (_incomingActor == null || _actorVisualPrepared) return;
            _actorVisualPrepared = true;
            if (!_visualStates.Contains(_incomingActor)) (_view as INovelActorView)?.ResetActor(_incomingActor.Slot);
            ApplyActor(_incomingActor);
        }
        private void ApplyActor(NovelVisualState actor)
        {
            if (!_disposed && _view is INovelActorView view)
                view.ApplyActor(actor.Copy(), actor.GestureOffset, actor.GestureRotation);
        }
        private void RemoveActor(NovelVisualState actor, NovelActionHandle completing = null)
        {
            StopBoundEffects(actor.InstanceId);
            foreach (var action in _runningActions)
                if (action != completing && action.TargetKind == NovelTargetKind.Character && action.TargetId == actor.InstanceId)
                { ResetGesture(action); action.Status = NovelActionStatus.Cancelled; }
            _visualStates.Remove(actor);
            _view?.Visual(new NovelCommand("exit", NovelCommandKind.Character, slot: actor.Slot, visualAction: NovelVisualAction.Hide), null, 1);
            (_view as INovelActorView)?.ResetActor(actor.Slot);
            PruneVisualResources(); RefreshEmphasis();
        }
        private NovelActionHandle StartAction(NovelCommand command, NarrativeSnapshot snapshot)
        {
            if (_actions.TryGetValue(command.ActionId, out var previous) && !previous.IsFinished)
                throw new InvalidOperationException("动作 ID 正在使用：" + command.ActionId);
            if (!_actions.ContainsKey(command.ActionId) && _actions.Count >= 4096) throw new InvalidOperationException("动作 ID 超过会话上限 4096");
            if (command.Kind == NovelCommandKind.Camera) return StartCamera(command, snapshot);
            if (NovelScreenRules.IsAction(command.Kind)) return StartScreenAction(command, snapshot);
            bool opacity = command.Kind == NovelCommandKind.Opacity;
            if (opacity ? _view is not INovelOpacityView : _view is not INovelActorView)
                throw new InvalidOperationException("页面未提供所需演出适配：" + command.Kind);
            var kind = opacity ? command.TargetKind : NovelTargetKind.Character;
            string id = kind == NovelTargetKind.Stage ? "stage" : kind == NovelTargetKind.Background ? "background" : command.InstanceId;
            var target = kind == NovelTargetKind.Stage ? null : Target(kind, id);
            if (kind != NovelTargetKind.Stage && target == null) throw new InvalidOperationException("演出目标不存在：" + kind + "/" + id);
            if (command.Kind == NovelCommandKind.Move && command.PositionMode == NovelPositionMode.Named)
            {
                var occupied = Occupant(command.Slot);
                if (occupied != null && occupied != target) throw new InvalidOperationException("命名位置已占用：" + command.Slot);
            }
            CancelTarget(kind, id, command.Kind.ToString());
            var action = new NovelActionHandle { Generation = snapshot.SessionGeneration, Sequence = ++_actionSequence,
                Id = command.ActionId, Kind = command.Kind, Property = command.Kind.ToString(), TargetKind = kind, TargetId = id,
                From = target?.Opacity ?? _stageOpacity, To = command.Opacity, Duration = command.Duration, Delay = command.Delay,
                Ease = opacity ? NovelEase.Linear : command.Ease, Gesture = command.Gesture, Strength = command.Strength, ExitAfterMove = command.ExitAfterMove };
            switch (command.Kind)
            {
                case NovelCommandKind.Move:
                    action.VectorFrom = target.Offset;
                    action.VectorTo = (command.PositionMode == NovelPositionMode.Named ? NamedPosition(command.Slot) : command.Position) - NamedPosition(target.Slot);
                    target.NamedSlot = command.PositionMode == NovelPositionMode.Named ? (int)command.Slot : -1; break;
                case NovelCommandKind.Scale: action.VectorFrom = target.Scale; action.VectorTo = command.Scale; break;
                case NovelCommandKind.Rotate: action.From = target.Rotation; action.To = command.Rotation; break;
                case NovelCommandKind.Mirror: action.To = command.Mirror ? 1 : 0; break;
                case NovelCommandKind.Layer: action.To = command.Layer; break;
            }
            _actions[command.ActionId] = action; _runningActions.Add(action); return action;
        }
        private void WriteAction(NovelActionHandle action, float progress)
        {
            if (action.Kind == NovelCommandKind.Camera) { WriteCamera(action, progress); return; }
            if (action.Property == "Audio") { WriteAudioAction(action, progress); return; }
            if (NovelScreenRules.IsAction(action.Kind)) { WriteScreenAction(action, progress); return; }
            float p = NovelActorRules.Ease(action.Ease, progress);
            if (action.Kind == NovelCommandKind.Opacity) { WriteOpacity(action, Mathf.Lerp(action.From, action.To, p)); return; }
            var target = Target(NovelTargetKind.Character, action.TargetId);
            if (target == null) { action.Status = NovelActionStatus.Cancelled; return; }
            switch (action.Kind)
            {
                case NovelCommandKind.Move: target.Offset = Vector2.Lerp(action.VectorFrom, action.VectorTo, p); break;
                case NovelCommandKind.Scale: var scale = Vector2.Lerp(action.VectorFrom, action.VectorTo, p); target.Scale = new Vector3(scale.x, scale.y, 1); break;
                case NovelCommandKind.Rotate: target.Rotation = Mathf.Lerp(action.From, action.To, p); break;
                case NovelCommandKind.Mirror: if (progress >= 1) target.Mirror = action.To != 0; break;
                case NovelCommandKind.Layer: if (progress >= 1) target.Layer = (int)action.To; break;
                case NovelCommandKind.Gesture:
                    float pulse = progress >= 1 ? 0 : Mathf.Sin(Mathf.PI * p) * action.Strength;
                    target.GestureOffset = action.Gesture == NovelGesture.Jump ? new Vector2(0, .04f * pulse) : Vector2.zero;
                    target.GestureRotation = action.Gesture == NovelGesture.Nod ? -8 * pulse : 0; break;
            }
            ApplyActor(target);
            if (progress >= 1 && action.Kind == NovelCommandKind.Move && action.ExitAfterMove) RemoveActor(target, action);
        }
        private void ResetGesture(NovelActionHandle action)
        {
            if (NovelScreenRules.IsAction(action.Kind)) { CancelScreenAction(action); return; }
            if (action.TargetKind != NovelTargetKind.Character) return;
            var target = Target(NovelTargetKind.Character, action.TargetId); if (target == null) return;
            if (action.Kind == NovelCommandKind.Move && !action.IsFinished) target.NamedSlot = -1;
            if (action.Kind != NovelCommandKind.Gesture) return;
            target.GestureOffset = Vector2.zero; target.GestureRotation = 0; ApplyActor(target);
        }
        private void SetEmphasis(NovelCommand command)
        {
            if (_view is not INovelActorView) throw new InvalidOperationException("页面未提供人物强调适配");
            if (command.EmphasisMode == NovelEmphasisMode.Manual && Target(NovelTargetKind.Character, command.InstanceId) == null)
                throw new InvalidOperationException("强调目标不存在：" + command.InstanceId);
            _emphasisMode = command.EmphasisMode; _emphasisInstance = command.InstanceId; _dimFactor = command.DimFactor;
            RefreshEmphasis();
        }
        private void RefreshEmphasis()
        {
            var command = _runner.CurrentCommand;
            string speaker = command?.Kind == NovelCommandKind.Say ? command.CharacterId : null;
            bool found = _visualStates.Any(v => v.Kind == NovelCommandKind.Character && (_emphasisMode == NovelEmphasisMode.Manual ?
                v.InstanceId == _emphasisInstance : !string.IsNullOrEmpty(speaker) && v.CharacterId == speaker));
            foreach (var actor in _visualStates.Where(v => v.Kind == NovelCommandKind.Character))
            {
                bool highlighted = _emphasisMode == NovelEmphasisMode.Manual ? actor.InstanceId == _emphasisInstance : actor.CharacterId == speaker;
                actor.Brightness = _emphasisMode == NovelEmphasisMode.Off || !found || highlighted ? 1 : _dimFactor;
                ApplyActor(actor);
            }
        }
        #endregion
    }
}
