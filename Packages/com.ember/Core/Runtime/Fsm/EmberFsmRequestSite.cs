using System;
using System.Collections.Generic;
using Ember.Basic;

namespace Ember.Core.Fsm
{
    public enum EmberFsmTriggerPhase { External, Update, FixedUpdate, Event }
    public enum EmberFsmContinuation { UntilChanged, UntilMatched }

    /// <summary>转换的一个主动请求位置；只存不可变代码声明，不安排自动调度。</summary>
    public abstract class EmberFsmRequestSite
    {
        #region 内部参数
        public string Id { get; }
        public EmberFsmTriggerPhase Phase { get; }
        public string Segment { get; }
        public int Order { get; }
        public EmberFsmContinuation Continuation { get; }
        public string Condition { get; }
        public string CommitDescription { get; }
        internal abstract Type ContextType { get; }
        internal abstract Type StateType { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        protected EmberFsmRequestSite(string id, EmberFsmTriggerPhase phase, string segment, int order,
            EmberFsmContinuation continuation, string condition, string commitDescription)
        { Id = id; Phase = phase; Segment = segment; Order = order; Continuation = continuation;
            Condition = condition; CommitDescription = commitDescription; }
        internal abstract bool Evaluate(object state, object context);
        internal abstract void Commit(object state, object context);
        #endregion
    }

    public sealed class EmberFsmRequestSite<TContext, TState> : EmberFsmRequestSite
        where TContext : class where TState : class, IEmberFsmState<TContext>
    {
        #region 内部参数
        private readonly Func<TState, TContext, bool> _condition;
        private readonly Action<TState, TContext> _commit;
        internal override Type ContextType => typeof(TContext);
        internal override Type StateType => typeof(TState);
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EmberFsmRequestSite(string id, EmberFsmTriggerPhase phase, string segment, int order,
            Func<TState, TContext, bool> condition, string description,
            EmberFsmContinuation continuation = EmberFsmContinuation.UntilChanged,
            Action<TState, TContext> commit = null, string commitDescription = "进入目标状态时执行其原有提交")
            : base(id, phase, segment, order, continuation, description, commitDescription)
        { _condition = condition ?? throw new ArgumentNullException(nameof(condition)); _commit = commit; }
        internal override bool Evaluate(object state, object context) => _condition((TState)state, (TContext)context);
        internal override void Commit(object state, object context) => _commit?.Invoke((TState)state, (TContext)context);
        #endregion
    }

    /// <summary>条件匹配与状态改变分开；调用者必须在 ShouldStop 时结束当前位置的业务执行。</summary>
    public readonly struct EmberFsmAttemptResult
    {
        #region 内部参数
        public EmberFsmResult Result { get; }
        public bool Matched { get; }
        public bool ShouldStop { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal EmberFsmAttemptResult(EmberFsmResult result, bool matched = false,
            EmberFsmContinuation continuation = EmberFsmContinuation.UntilChanged)
        { Result = result; Matched = matched;
            ShouldStop = !CanContinue(result) || matched && continuation == EmberFsmContinuation.UntilMatched; }
        [NoGC]
        internal static bool CanContinue(EmberFsmResult result) => result == EmberFsmResult.GuardRejected ||
            result == EmberFsmResult.EntryRejected || result == EmberFsmResult.NoChange;
        #endregion
    }

    public sealed class EmberFsmEntryTrace
    {
        public string NodeId { get; }
        public EmberFsmEntryKind Decision { get; }
        public string RedirectSlot { get; }
        internal EmberFsmEntryTrace(string node, EmberFsmEntryDecision decision)
        { NodeId = node; Decision = decision.Kind; RedirectSlot = decision.TargetSlot; }
    }

    /// <summary>一次真实尝试的值快照；未评估和短路不能伪装成条件为 false。</summary>
    public sealed class EmberFsmAttemptRecord
    {
        #region 内部参数
        public ulong Generation { get; }
        public long DispatchId { get; }
        public EmberFsmTriggerPhase ActualPhase { get; }
        public string SiteId { get; }
        public bool Evaluated { get; }
        public bool? Allowed { get; }
        public bool? BaseAllowed { get; }
        public string RuleIdentity { get; }
        public string RuleDescription { get; }
        public IReadOnlyList<EmberConditionTrace> Conditions { get; }
        public bool CommitStarted { get; }
        public bool CommitCompleted { get; }
        public string RequestedTarget { get; }
        public string SkipReason { get; }
        public IReadOnlyList<string> PreviousPath { get; }
        public IReadOnlyList<EmberFsmEntryTrace> Entries { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal EmberFsmAttemptRecord(EmberFsmAttemptTrace trace)
        { Generation = trace.Generation; DispatchId = trace.DispatchId; ActualPhase = trace.ActualPhase;
            SiteId = trace.SiteId; Evaluated = trace.Evaluated; Allowed = trace.Allowed;
            BaseAllowed = trace.BaseAllowed; RuleIdentity = trace.RuleIdentity; RuleDescription = trace.RuleDescription;
            Conditions = Array.AsReadOnly(trace.Conditions.FindAll(item => item != null).ToArray());
            CommitStarted = trace.CommitStarted; CommitCompleted = trace.CommitCompleted;
            RequestedTarget = trace.RequestedTarget; SkipReason = trace.SkipReason;
            PreviousPath = Array.AsReadOnly((string[])trace.PreviousPath.Clone());
            Entries = Array.AsReadOnly(trace.Entries.ToArray()); }
        #endregion
    }

    internal sealed class EmberFsmAttemptTrace
    {
        internal ulong Generation;
        internal long DispatchId;
        internal EmberFsmTriggerPhase ActualPhase;
        internal string SiteId, RequestedTarget, SkipReason;
        internal bool Evaluated, CommitStarted, CommitCompleted;
        internal bool? Allowed;
        internal bool? BaseAllowed;
        internal string RuleIdentity, RuleDescription;
        internal readonly List<EmberConditionTrace> Conditions = new List<EmberConditionTrace>();
        internal string[] PreviousPath;
        internal readonly List<EmberFsmEntryTrace> Entries = new List<EmberFsmEntryTrace>();
    }
}
