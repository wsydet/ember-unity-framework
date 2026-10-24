using System;
using Game.Entity;

namespace Game.Player
{
    /// <summary>由单个冲刺状态拥有；尾随移交宿主计时，普通退出不撤销其他施加。</summary>
    internal sealed class PlayerDashEffects
    {
        #region 内部参数
        private readonly EntityEffectSpec _action, _tail;
        private EntityEffectHandle _handle;
        private bool _entered;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerDashEffects(EntityEffectDefinition action, EntityEffectDefinition tail)
        {
            _action = action != null ? action.Capture() : null;
            _tail = tail != null ? tail.Capture() : null;
            if (_action != null && _action.Lifetime != EntityEffectLifetime.Owned)
                throw new ArgumentException("冲刺动作效果必须为 Owned。");
            if (_tail != null && _tail.Lifetime != EntityEffectLifetime.Timed)
                throw new ArgumentException("冲刺尾随必须为独立 Timed，不能使用 OwnedThenTimed。");
        }
        internal void ValidateContainer(EntityEffectContainer container)
        {
            if ((_action != null || _tail != null) && container == null)
                throw new InvalidOperationException("冲刺配置了效果，但玩家未装配效果容器。");
        }
        internal void Enter(PlayerContext context, string source)
        {
            ValidateContainer(context.Effects);
            _entered = true;
            if (_action != null) _handle = context.Effects.ApplyCaptured(_action, source + "/action");
        }
        internal void Exit(PlayerContext context, string source)
        {
            var handle = _handle; _handle = null;
            bool completed = _entered && context.Dash.EndReason == PlayerDashEndReason.Completed;
            _entered = false;
            handle?.Dispose();
            if (completed && _tail != null && context.Effects != null && !context.Effects.IsDisposed)
                context.Effects.ApplyCaptured(_tail, source + "/tail");
        }
        internal void Dispose()
        { _entered = false; var handle = _handle; _handle = null; handle?.Dispose(); }
        #endregion
    }
}
