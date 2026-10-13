using System;
using Ember.Basic;

namespace Ember.Core.Fsm
{
    public sealed partial class EmberFsm<TContext>
    {
        #region 内部参数
        private sealed class SiteBinding
        {
            internal readonly EmberFsmTransition Transition;
            internal readonly EmberFsmRequestSite Site;
            internal SiteBinding(EmberFsmTransition transition, EmberFsmRequestSite site)
            { Transition = transition; Site = site; }
        }
        private EmberFsmAttemptTrace _attempt;
        private string _attemptSource;
        private EmberFsmTransition _attemptTransition;
        private long _dispatchId;
        private EmberFsmTriggerPhase _dispatchPhase;
        private EmberFsmResult? _dispatchFailure;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private EmberFsmAttemptTrace NewAttempt(EmberFsmActivation activation, string site) => new EmberFsmAttemptTrace
        {
            Generation = activation.Generation, SiteId = site, PreviousPath = CopyPath(),
            DispatchId = _dispatching ? _dispatchId : 0,
            ActualPhase = _dispatching ? _dispatchPhase : EmberFsmTriggerPhase.External
        };

        private EmberFsmResult? ValidateActivation(EmberFsmActivation activation, out RuntimeNode node)
        {
            node = null;
            if (Status == EmberFsmStatus.Paused) return EmberFsmResult.Paused;
            if (Status != EmberFsmStatus.Running) return EmberFsmResult.NotRunning;
            if (activation.Owner != this || activation.NodeId == null || !_nodes.TryGetValue(activation.NodeId, out node) ||
                !node.Active || node.Generation != activation.Generation) return EmberFsmResult.StaleActivation;
            return null;
        }

        private void EndDispatchOnError(EmberFsmResult result, bool strict)
        {
            if (strict && _dispatching && result != EmberFsmResult.Changed && !EmberFsmAttemptResult.CanContinue(result))
                _dispatchFailure = _dispatchFailure ?? result;
        }

        private EmberFsmAttemptResult AttemptInternal(EmberFsmActivation activation, EmberFsmTransition transition,
            string siteId, EmberFsmRequestOrigin origin, bool strict)
        {
            var blocked = CheckControl();
            // 嵌套请求不能写外层评估记录、终止标记或事务锁。
            if (blocked.HasValue)
            {
                if (blocked != EmberFsmResult.Reentrant) EndDispatchOnError(blocked.Value, strict);
                return new EmberFsmAttemptResult(blocked.Value);
            }
            _busy = true;
            _attempt = NewAttempt(activation, siteId); _attemptSource = activation.NodeId; _attemptTransition = transition;
            EmberFsmRequestSite site = null;
            string target = null;
            try
            {
                var invalid = ValidateActivation(activation, out var node);
                var result = invalid ?? EmberFsmResult.NoChange;
                if (!invalid.HasValue)
                {
                    bool declared = false;
                    foreach (var edge in node.Description.Transitions)
                        if (ReferenceEquals(edge, transition)) { declared = true; break; }
                    if (!declared || transition == null) result = EmberFsmResult.UndeclaredTransition;
                    else
                    {
                        if (transition.Sites.Count > 0)
                        {
                            if (siteId == null && transition.Sites.Count == 1) site = transition.Sites[0];
                            else if (siteId != null)
                                foreach (var item in transition.Sites) if (item.Id == siteId) { site = item; break; }
                            if (site == null) result = siteId == null
                                ? EmberFsmResult.RequestSiteRequired : EmberFsmResult.InvalidRequestSite;
                            else _attempt.SiteId = site.Id;
                        }
                        else if (siteId != null) result = EmberFsmResult.InvalidRequestSite;
                        if (result == EmberFsmResult.NoChange)
                        {
                            if (!node.Description.Targets.TryGetValue(transition.TargetSlot, out target)) result = EmberFsmResult.InvalidTarget;
                            else
                            {
                                _attempt.RequestedTarget = target;
                                bool allowed;
                                try
                                {
                                    _attempt.Evaluated = true;
                                    allowed = site == null ? transition.Evaluate(node.State, _context) : site.Evaluate(node.State, _context);
                                    _attempt.BaseAllowed = allowed;
                                    var extra = _rules?.Find(activation.NodeId, transition, site?.Id);
                                    if (extra != null)
                                    {
                                        _attempt.RuleIdentity = extra.Identity;
                                        _attempt.RuleDescription = extra.Description;
                                        var trace = DiagnosticsEnabled ? _attempt.Conditions : null;
                                        if (allowed) allowed = extra.Evaluate(_context, trace);
                                        else extra.Skip(trace, "基础许可未通过");
                                    }
                                    _attempt.Allowed = allowed;
                                }
                                catch (Exception ex)
                                {
                                    Fault(node, "Guard", ex);
                                    EndDispatchOnError(EmberFsmResult.Faulted, strict);
                                    return new EmberFsmAttemptResult(Complete(EmberFsmResult.Faulted, "Request", origin,
                                        activation.NodeId, transition, target));
                                }
                                result = allowed ? Change(target, transition.Mode, out target, node, site) : EmberFsmResult.GuardRejected;
                            }
                        }
                    }
                }
                EndDispatchOnError(result, strict);
                return new EmberFsmAttemptResult(Complete(result, "Request", origin, activation.NodeId, transition, target),
                    _attempt.Allowed == true, site?.Continuation ?? EmberFsmContinuation.UntilChanged);
            }
            finally { _attempt = null; _attemptSource = null; _attemptTransition = null; _busy = false; }
        }

        private void RecordSkipped(EmberFsmActivation activation, SiteBinding binding, string reason)
        {
            if (!DiagnosticsEnabled || _historyCapacity == 0 && Diagnostic == null) return;
            var trace = NewAttempt(activation, binding.Site.Id); trace.SkipReason = reason;
            if (_nodes.TryGetValue(activation.NodeId, out var node))
                node.Description.Targets.TryGetValue(binding.Transition.TargetSlot, out trace.RequestedTarget);
            // 不覆盖最后一次真实操作，也不将短路伪装成一次 GuardRejected。
            Publish(new EmberFsmDiagnostic(InstanceId, ++_sequence, EmberFsmRequestOrigin.State,
                activation.NodeId, binding.Transition.Id, trace.RequestedTarget, CopyPath(), EmberFsmResult.NoChange,
                "Skipped", attempt: new EmberFsmAttemptRecord(trace)));
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [HasGC]
        public EmberFsmAttemptResult Attempt(EmberFsmActivation activation, EmberFsmTransition transition, string site = null)
            => AttemptInternal(activation, transition, site, EmberFsmRequestOrigin.External, true);

        EmberFsmAttemptResult IEmberFsmRequests.Attempt(EmberFsmActivation activation, EmberFsmTransition transition,
            string site, EmberFsmRequestOrigin origin) => AttemptInternal(activation, transition, site, origin, true);

        EmberFsmAttemptResult IEmberFsmRequests.AttemptGroup(EmberFsmActivation activation, string segment, EmberFsmTriggerPhase phase)
        {
            var blocked = CheckControl();
            if (blocked.HasValue)
            {
                if (blocked != EmberFsmResult.Reentrant) EndDispatchOnError(blocked.Value, true);
                return new EmberFsmAttemptResult(blocked.Value);
            }
            var invalid = ValidateActivation(activation, out var node);
            bool found = false, stopped = false;
            var outcome = new EmberFsmAttemptResult(EmberFsmResult.NoChange);
            if (!invalid.HasValue)
            {
                foreach (var binding in node.Sites)
                {
                    if (binding.Site.Phase != phase || binding.Site.Segment != segment) continue;
                    found = true;
                    if (stopped) { RecordSkipped(activation, binding, "前序位置停止：" + outcome.Result); continue; }
                    outcome = AttemptInternal(activation, binding.Transition, binding.Site.Id, EmberFsmRequestOrigin.State, true);
                    stopped = outcome.ShouldStop;
                }
            }
            if (found) return outcome;
            _busy = true;
            _attempt = NewAttempt(activation, null);
            try
            {
                var result = invalid ?? EmberFsmResult.InvalidRequestSite;
                EndDispatchOnError(result, true);
                return new EmberFsmAttemptResult(Complete(result, "RequestGroup:" + phase + "/" + segment,
                    EmberFsmRequestOrigin.State, activation.NodeId));
            }
            finally { _attempt = null; _busy = false; }
        }
        #endregion
    }
}
