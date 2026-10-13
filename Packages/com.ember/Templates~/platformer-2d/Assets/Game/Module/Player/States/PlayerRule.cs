using System;
using Ember.Basic;
using Ember.Core.Fsm;

namespace Game.Player
{
    /// <summary>玩家代码规则的固定步位置声明；不保存角色数据、不驱动状态机。</summary>
    internal static class PlayerRule
    {
        #region 外部方法
        [HasGC]
        internal static EmberFsmRequestSite<PlayerContext, TState> Fixed<TState>(int order,
            Func<TState, PlayerContext, bool> condition, string description, string segment = "decision",
            bool stopOnMatch = false, string id = "fixed", Action<TState, PlayerContext> commit = null,
            string commitDescription = "目标 Enter/Begin 保留原有提交；本规则不重复消费输入、能力或效果")
            where TState : class, IEmberFsmState<PlayerContext>
            => new(id, EmberFsmTriggerPhase.FixedUpdate, segment, order, condition, description,
                stopOnMatch ? EmberFsmContinuation.UntilMatched : EmberFsmContinuation.UntilChanged,
                commit, commitDescription);
        #endregion
    }
}
