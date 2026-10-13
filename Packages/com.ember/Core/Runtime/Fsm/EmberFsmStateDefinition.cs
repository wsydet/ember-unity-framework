using System;
using System.Collections.Generic;
using Ember.Basic;

namespace Ember.Core.Fsm
{
    /// <summary>共享只读配置及声明入口；读取声明不创建角色，不执行生命周期或 Guard。</summary>
    public abstract class EmberFsmStateDefinition : EmberBaseSO
    {
        #region 外部方法
        public abstract Type ContextType { get; }
        public abstract Type RuntimeStateType { get; }
        public abstract IReadOnlyList<EmberFsmTransition> GetTransitions();
        public virtual IReadOnlyList<string> GetRedirectSlots() => Array.Empty<string>();
        internal abstract object CreateRuntime();
        #endregion
    }

    /// <summary>项目编写封闭的具体派生 SO，并添加 CreateAssetMenu；不直接创建开放泛型资产。</summary>
    public abstract class EmberFsmStateDefinition<TContext, TState> : EmberFsmStateDefinition
        where TContext : class where TState : class, IEmberFsmState<TContext>
    {
        #region 外部方法
        public sealed override Type ContextType => typeof(TContext);
        public sealed override Type RuntimeStateType => typeof(TState);
        public abstract TState CreateState();
        internal sealed override object CreateRuntime() => CreateState();
        #endregion
    }
}
