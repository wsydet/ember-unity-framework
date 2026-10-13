using System;
using Ember.Basic;
using System.Collections.Generic;

namespace Ember.Core.Fsm
{
    public enum EmberFsmStatus { Stopped, Running, Paused, Faulted, Disposed }
    public enum EmberFsmResult
    {
        Changed, NoChange, GuardRejected, EntryRejected, InvalidTarget, UndeclaredTransition,
        StaleActivation, RedirectCycle, RedirectLimit, Reentrant, NotRunning, Paused, Faulted, Disposed,
        RequestSiteRequired, InvalidRequestSite
    }
    public enum EmberFsmTransitionMode { PreserveAncestors, ReenterTargetBranch }
    public enum EmberFsmRequestOrigin { State, External, Start, Host }
    public enum EmberFsmEntryKind { Allow, Reject, Redirect }

    /// <summary>进入预检的决定。重定向只能使用当前预检节点声明并绑定的槽。</summary>
    public readonly struct EmberFsmEntryDecision
    {
        #region 内部参数
        public EmberFsmEntryKind Kind { get; }
        public string TargetSlot { get; }
        public static EmberFsmEntryDecision Allow => default;
        public static EmberFsmEntryDecision Reject => new EmberFsmEntryDecision(EmberFsmEntryKind.Reject, null);
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private EmberFsmEntryDecision(EmberFsmEntryKind kind, string slot) { Kind = kind; TargetSlot = slot; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        [NoGC]
        public static EmberFsmEntryDecision Redirect(string slot) => new EmberFsmEntryDecision(EmberFsmEntryKind.Redirect, slot);
        #endregion
    }

    /// <summary>不可变声明，也是运行时请求的句柄。应由行为类持有 static readonly 实例。</summary>
    public abstract class EmberFsmTransition
    {
        #region 内部参数
        public string Id { get; }
        public string TargetSlot { get; }
        public string Description { get; }
        public EmberFsmTransitionMode Mode { get; }
        public IReadOnlyList<EmberFsmRequestSite> Sites { get; protected set; } = Array.Empty<EmberFsmRequestSite>();
        internal virtual bool HasLegacyGuard => false;
        internal abstract Type ContextType { get; }
        internal abstract Type StateType { get; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        protected EmberFsmTransition(string id, string targetSlot, string description, EmberFsmTransitionMode mode)
        { Id = id; TargetSlot = targetSlot; Description = description; Mode = mode; }
        internal abstract bool Evaluate(object state, object context);
        #endregion
    }

    public sealed class EmberFsmTransition<TContext, TState> : EmberFsmTransition
        where TContext : class where TState : class, IEmberFsmState<TContext>
    {
        #region 内部参数
        private readonly Func<TState, TContext, bool> _guard;
        internal override bool HasLegacyGuard => _guard != null;
        internal override Type ContextType => typeof(TContext);
        internal override Type StateType => typeof(TState);
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public EmberFsmTransition(string id, string targetSlot, Func<TState, TContext, bool> guard = null,
            string description = "", EmberFsmTransitionMode mode = EmberFsmTransitionMode.PreserveAncestors,
            params EmberFsmRequestSite<TContext, TState>[] sites)
            : base(id, targetSlot, description, mode)
        { _guard = guard; Sites = Array.AsReadOnly<EmberFsmRequestSite>(
            sites == null ? Array.Empty<EmberFsmRequestSite>() : (EmberFsmRequestSite[])sites.Clone()); }
        internal override bool Evaluate(object state, object context) => _guard == null || _guard((TState)state, (TContext)context);
        #endregion
    }

    internal interface IEmberFsmRequests
    {
        EmberFsmResult Request(EmberFsmActivation activation, EmberFsmTransition transition, EmberFsmRequestOrigin origin);
        EmberFsmAttemptResult Attempt(EmberFsmActivation activation, EmberFsmTransition transition, string site,
            EmberFsmRequestOrigin origin);
        EmberFsmAttemptResult AttemptGroup(EmberFsmActivation activation, string segment, EmberFsmTriggerPhase phase);
    }

    /// <summary>一次激活的能力凭证。异步/事件订阅应捕获此值，不能在延迟回调中重新获取。</summary>
    public readonly struct EmberFsmActivation
    {
        #region 内部参数
        internal IEmberFsmRequests Owner { get; }
        public string NodeId { get; }
        public ulong Generation { get; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal EmberFsmActivation(IEmberFsmRequests owner, string nodeId, ulong generation)
        { Owner = owner; NodeId = nodeId; Generation = generation; }
        [HasGC]
        public EmberFsmResult Request(EmberFsmTransition transition) => Owner == null
            ? EmberFsmResult.StaleActivation : Owner.Request(this, transition, EmberFsmRequestOrigin.State);
        [HasGC]
        public EmberFsmAttemptResult Attempt(EmberFsmTransition transition, string site = null) => Owner == null
            ? new EmberFsmAttemptResult(EmberFsmResult.StaleActivation)
            : Owner.Attempt(this, transition, site, EmberFsmRequestOrigin.State);
        [HasGC]
        public EmberFsmAttemptResult AttemptGroup(string segment, EmberFsmTriggerPhase phase = EmberFsmTriggerPhase.FixedUpdate)
            => Owner == null ? new EmberFsmAttemptResult(EmberFsmResult.StaleActivation)
                : Owner.AttemptGroup(this, segment, phase);
        #endregion
    }

    /// <summary>每宿主每节点独立对象。Exit 清理激活资源，Dispose 清理对象寿命资源。</summary>
    public interface IEmberFsmState<TContext> : IDisposable where TContext : class
    {
        void Initialize(TContext context, string nodeId);
        EmberFsmEntryDecision CanEnter();
        void Enter(EmberFsmActivation activation);
        void Exit();
        void Update(float deltaTime);
        void FixedUpdate(float fixedDeltaTime);
        void Pause();
        void Resume();
    }

    public abstract class EmberFsmState<TContext> : IEmberFsmState<TContext> where TContext : class
    {
        #region 内部参数
        protected TContext Context { get; private set; }
        protected string NodeId { get; private set; }
        protected EmberFsmActivation Activation { get; private set; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public virtual void Initialize(TContext context, string nodeId) { Context = context; NodeId = nodeId; }
        public virtual EmberFsmEntryDecision CanEnter() => EmberFsmEntryDecision.Allow;
        public virtual void Enter(EmberFsmActivation activation) { Activation = activation; }
        public virtual void Exit() { }
        public virtual void Update(float deltaTime) { }
        public virtual void FixedUpdate(float fixedDeltaTime) { }
        public virtual void Pause() { }
        public virtual void Resume() { }
        public virtual void Dispose() { }
        #endregion
    }
}
