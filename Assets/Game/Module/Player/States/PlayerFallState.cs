using Ember.Core.Fsm;

namespace Game.Player
{
    public sealed class PlayerFallState : EmberFsmState<PlayerContext>
    {
        #region 外部方法
        // 默认从 Air/Fall 装配；恢复时依据运动数据选上升，不保存或重放 FSM 对象。
        public override EmberFsmEntryDecision CanEnter() => Context.Motor.Velocity.y > 0
            ? EmberFsmEntryDecision.Redirect("rising") : EmberFsmEntryDecision.Allow;
        #endregion
    }
}
