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
        /// <summary>已经报过诊断的空实例 ID 立绘隐藏指令，避免同一会话里反复刷同一条警告。</summary>
        private readonly System.Collections.Generic.HashSet<string> _diagnosedEmptyHides = new(StringComparer.Ordinal);
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private Vector2 NamedPosition(NovelPortraitSlot slot) => _view is INovelActorView actorView ? actorView.NamedPosition(slot) :
            new Vector2(.26f + (int)slot * .24f, slot == NovelPortraitSlot.Center ? .395f : .335f);
        private NovelVisualState Occupant(NovelPortraitSlot slot) => _visualStates.FirstOrDefault(v =>
            v.Kind == NovelCommandKind.Character && v.NamedSlot == (int)slot);
        /// <summary>
        /// 空实例 ID 的退化匹配：渲染槽上还没有认领任何命名位置的实例。
        ///
        /// 归一化入场（<c>PositionMode = Position</c>）的实例 <c>NamedSlot</c> 恒为 -1，
        /// 按命名位置找永远找不到它；而它的渲染槽就是入场时选的那个槽。
        /// 只在这个实例**没有**登记到别的命名位置时才退化匹配：已经 Move 到别处的实例
        /// 仍然按旧的「过期槽位引用」处理，不会因为一条陈旧的空 ID 指令被误删。
        /// </summary>
        private NovelVisualState UnnamedSlotOccupant(NovelPortraitSlot slot) => _visualStates.FirstOrDefault(v =>
            v.Kind == NovelCommandKind.Character && v.Slot == slot && v.NamedSlot < 0);
        /// <summary>空实例 ID 的立绘隐藏只在每条指令上报一次诊断，不随节点重访反复刷屏。</summary>
        private void DiagnoseEmptyHide(NovelCommand command, string detail)
        {
            string id = command.CommandId ?? command.Slot.ToString();
            if (string.IsNullOrEmpty(id) || !_diagnosedEmptyHides.Add(id)) return;
            Ember.Basic.EmberDebug.LogWarning("Game.Narrative",
                "空实例 ID 的立绘隐藏（指令 " + id + "）：" + detail);
        }
        private NovelCommand ResolveActorVisual(NovelCommand command)
        {
            var existing = string.IsNullOrEmpty(command.InstanceId)
                ? Occupant(command.Slot) ?? UnnamedSlotOccupant(command.Slot) : Target(NovelTargetKind.Character, command.InstanceId);
            // 空实例 ID 的 Hide 落在归一化入场的实例上时，语义从「按命名位置操作」退化为
            // 「隐藏该渲染槽上的实例」。动作本身照常生效，这里只补一条一次性诊断，
            // 让作者知道该把实例 ID 写明确，而不是让它继续依赖退化规则。
            if (existing != null && string.IsNullOrEmpty(command.InstanceId) &&
                command.VisualAction == NovelVisualAction.Hide && existing.NamedSlot < 0)
                DiagnoseEmptyHide(command, "已按退化规则隐藏渲染槽 " + command.Slot + " 上的实例「" + existing.InstanceId +
                    "」——该实例是归一化入场的，没有认领命名位置。建议把这条隐藏的实例 ID 改写成「" + existing.InstanceId + "」，不要依赖槽位退化。");
            bool preserve = command.VisualAction != NovelVisualAction.Hide && existing != null &&
                command.VisualAction == NovelVisualAction.Replace && !string.IsNullOrEmpty(command.InstanceId);
            if (!string.IsNullOrEmpty(command.InstanceId) && command.VisualAction != NovelVisualAction.Show && existing == null)
                throw new InvalidOperationException("人物实例不存在：" + command.InstanceId);
            if (existing == null && command.VisualAction == NovelVisualAction.Hide)
            {
                DiagnoseEmptyHide(command, "没有隐藏任何实例：" + command.Slot +
                    " 槽位上既没有认领该命名位置的人物，也没有归一化入场的人物。这条指令不会产生任何画面变化。");
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
            // 句柄身份统一取解析值；留空 ActionId 时回退到步骤 ID。Camera / Screen 也在这里就完成
            // 冲突守卫和上限检查，再委派给各自的 Start*，所以它们内部写入 _actions 时不会再覆盖活动句柄。
            string handleId = NovelActionHandle.ResolveId(command);
            if (_actions.TryGetValue(handleId, out var previous) && !previous.IsFinished)
                throw new InvalidOperationException("动作 ID 正在使用：" + handleId);
            if (!_actions.ContainsKey(handleId) && _actions.Count >= 4096) throw new InvalidOperationException("动作 ID 超过会话上限 4096");
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
                Id = handleId, Kind = command.Kind, Property = command.Kind.ToString(), TargetKind = kind, TargetId = id,
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
            _actions[handleId] = action; _runningActions.Add(action); return action;
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
