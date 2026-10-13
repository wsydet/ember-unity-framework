using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Ember.Basic;

namespace Ember.Core.Fsm
{
    // 弱键只验证工厂对象的独占性，不持有或调度状态机，不延长运行对象寿命。
    internal static class EmberFsmOwnership
    {
        internal static readonly ConditionalWeakTable<object, object> Claims = new ConditionalWeakTable<object, object>();
    }

    /// <summary>资产装配的单路径层级状态机。只在创建线程使用；无自动更新、全局注册或请求队列。</summary>
    public sealed partial class EmberFsm<TContext> : IDisposable, IEmberFsmRequests where TContext : class
    {
        #region 内部参数
        private sealed class RuntimeNode
        {
            internal EmberFsmNodeDescription Description;
            internal IEmberFsmState<TContext> State;
            internal bool Active;
            internal bool Released;
            internal ulong Generation;
            internal double Elapsed;
            internal readonly List<SiteBinding> Sites = new List<SiteBinding>();
        }

        private readonly TContext _context;
        private readonly EmberFsmRulePlan<TContext> _rules;
        public IReadOnlyList<EmberFsmRuleDescription> Rules => _rules?.Descriptions ?? Array.Empty<EmberFsmRuleDescription>();
        private readonly Dictionary<string, RuntimeNode> _nodes = new Dictionary<string, RuntimeNode>(StringComparer.Ordinal);
        private readonly List<RuntimeNode> _ordered = new List<RuntimeNode>();
        private readonly List<RuntimeNode> _path = new List<RuntimeNode>();
        private readonly Queue<EmberFsmDiagnostic> _history = new Queue<EmberFsmDiagnostic>();
        private readonly int _threadId = Environment.CurrentManagedThreadId;
        private readonly int _historyCapacity;
        private readonly int _redirectLimit;
        private bool _busy;
        private bool _notifying;
        private bool _dispatching;
        private bool _initialized;
        private long _version;
        private long _sequence;
        public Guid InstanceId { get; } = Guid.NewGuid();
        public EmberFsmGraphDescription Description { get; }
        public EmberFsmStatus Status { get; private set; } = EmberFsmStatus.Stopped;
        public EmberFsmDiagnostic LastOperation { get; private set; }
        public EmberFsmDiagnostic LastFault { get; private set; }
        public int CleanupFaultCount { get; private set; }
        public int ObserverFaultCount { get; private set; }
        public bool DiagnosticsEnabled { get; set; } = true;
        public event Action<EmberFsmDiagnostic> Diagnostic;
        public string CurrentLeafId => _path.Count == 0 ? null : _path[_path.Count - 1].Description.Id;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private EmberFsmResult? CheckControl()
        {
            if (Environment.CurrentManagedThreadId != _threadId)
                throw new InvalidOperationException("FSM must be used on its creating thread");
            if (_busy || _notifying || EmberFsmGraphDescription.ReadingDeclarations) return EmberFsmResult.Reentrant;
            if (Status == EmberFsmStatus.Disposed) return EmberFsmResult.Disposed;
            if (Status == EmberFsmStatus.Faulted) return EmberFsmResult.Faulted;
            return null;
        }

        private string[] CopyPath()
        {
            var result = new string[_path.Count];
            for (var i = 0; i < result.Length; i++) result[i] = _path[i].Description.Id;
            return result;
        }

        private void Publish(EmberFsmDiagnostic item)
        {
            if (!DiagnosticsEnabled || _notifying) return;
            if (_historyCapacity > 0)
            {
                if (_history.Count == _historyCapacity) _history.Dequeue();
                _history.Enqueue(item);
            }
            _notifying = true;
            try
            {
                var observers = Diagnostic;
                if (observers == null) return;
                foreach (Action<EmberFsmDiagnostic> observer in observers.GetInvocationList())
                {
                    try { observer(item); }
                    catch (Exception) { ObserverFaultCount++; }
                }
            }
            finally { _notifying = false; }
        }

        private EmberFsmResult Complete(EmberFsmResult result, string phase, EmberFsmRequestOrigin origin,
            string source = null, EmberFsmTransition transition = null, string target = null)
        {
            LastOperation = new EmberFsmDiagnostic(InstanceId, ++_sequence, origin, source, transition?.Id,
                target, CopyPath(), result, phase, attempt: _attempt == null ? null : new EmberFsmAttemptRecord(_attempt));
            Publish(LastOperation);
            return result;
        }

        private void Fault(RuntimeNode node, string phase, Exception exception)
        {
            if (Status != EmberFsmStatus.Disposed) Status = EmberFsmStatus.Faulted;
            _version++;
            var fault = new EmberFsmDiagnostic(InstanceId, ++_sequence, EmberFsmRequestOrigin.Host,
                _attemptSource, _attemptTransition?.Id, _attempt?.RequestedTarget, CopyPath(), EmberFsmResult.Faulted,
                phase, node?.Description.Id, exception, _attempt == null ? null : new EmberFsmAttemptRecord(_attempt));
            if (LastFault == null) LastFault = fault;
            // 清理时保持事务锁；每节点 Exit/Dispose 至多尝试一次，不递归重试故障回调。
            var wasBusy = _busy;
            _busy = true;
            try { CleanupPath(); ReleaseStates(); Publish(fault); }
            finally { _busy = wasBusy; }
        }

        private void CleanupPath()
        {
            while (_path.Count > 0)
            {
                var node = _path[_path.Count - 1];
                _path.RemoveAt(_path.Count - 1);
                node.Active = false;
                try { node.State.Exit(); }
                catch (Exception ex) { CleanupFailure(node, "Exit cleanup", ex); }
            }
        }

        private void CleanupFailure(RuntimeNode node, string phase, Exception ex)
        {
            CleanupFaultCount++;
            var item = new EmberFsmDiagnostic(InstanceId, ++_sequence, EmberFsmRequestOrigin.Host, null,
                null, null, CopyPath(), EmberFsmResult.Faulted, phase, node.Description.Id, ex);
            if (LastFault == null) LastFault = item;
            Publish(item);
        }

        private void ReleaseStates()
        {
            for (var i = _ordered.Count - 1; i >= 0; i--)
            {
                var node = _ordered[i];
                if (node.State == null || node.Released) continue;
                node.Released = true;
                try { node.State.Dispose(); }
                catch (Exception ex) { CleanupFailure(node, "Dispose", ex); }
            }
        }

        private bool Initialize()
        {
            if (_initialized) return true;
            foreach (var node in _ordered)
            {
                var phase = "Create";
                try
                {
                    var state = node.Description.Definition.CreateRuntime() as IEmberFsmState<TContext>;
                    if (state == null) throw new InvalidOperationException("Factory returned null or incompatible state");
                    if (!node.Description.Definition.RuntimeStateType.IsInstanceOfType(state))
                        throw new InvalidOperationException("Factory returned incorrect runtime type");
                    if (EmberFsmOwnership.Claims.TryGetValue(state, out _))
                        throw new InvalidOperationException("Factory reused a runtime state; return a new object for every node");
                    EmberFsmOwnership.Claims.Add(state, new object());
                    node.State = state;
                    phase = "Initialize";
                    state.Initialize(_context, node.Description.Id);
                }
                catch (Exception ex) { Fault(node, phase, ex); return false; }
            }
            _initialized = true;
            return true;
        }

        private List<RuntimeNode> ResolvePath(string target)
        {
            if (target == null || !_nodes.TryGetValue(target, out var node)) return null;
            var path = new List<RuntimeNode>();
            var cursor = node;
            while (cursor != null)
            {
                path.Add(cursor);
                cursor = string.IsNullOrEmpty(cursor.Description.ParentId) ? null : _nodes[cursor.Description.ParentId];
            }
            path.Reverse();
            while (!string.IsNullOrEmpty(node.Description.DefaultChildId))
            {
                node = _nodes[node.Description.DefaultChildId];
                path.Add(node);
            }
            return path;
        }

        private int RetainedCount(List<RuntimeNode> next, string target, EmberFsmTransitionMode mode)
        {
            var count = 0;
            while (count < _path.Count && count < next.Count && _path[count] == next[count]) count++;
            if (mode == EmberFsmTransitionMode.ReenterTargetBranch)
            {
                var targetIndex = next.IndexOf(_nodes[target]);
                count = Math.Min(count, targetIndex);
            }
            return count;
        }

        private EmberFsmResult Change(string requestedTarget, EmberFsmTransitionMode mode, out string actualTarget,
            RuntimeNode source = null, EmberFsmRequestSite site = null)
        {
            actualTarget = requestedTarget;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            List<RuntimeNode> next;
            int retained;
            var redirects = 0;
            while (true)
            {
                next = ResolvePath(actualTarget);
                if (next == null) return EmberFsmResult.InvalidTarget;
                if (!visited.Add(actualTarget)) return EmberFsmResult.RedirectCycle;
                retained = RetainedCount(next, actualTarget, mode);
                if (retained == next.Count && retained == _path.Count) return EmberFsmResult.NoChange;
                var redirect = false;
                for (var i = retained; i < next.Count; i++)
                {
                    var node = next[i];
                    EmberFsmEntryDecision decision;
                    try { decision = node.State.CanEnter(); }
                    catch (Exception ex) { Fault(node, "CanEnter", ex); return EmberFsmResult.Faulted; }
                    _attempt?.Entries.Add(new EmberFsmEntryTrace(node.Description.Id, decision));
                    if (decision.Kind == EmberFsmEntryKind.Reject) return EmberFsmResult.EntryRejected;
                    if (decision.Kind == EmberFsmEntryKind.Allow) continue;
                    if (decision.Kind != EmberFsmEntryKind.Redirect || string.IsNullOrEmpty(decision.TargetSlot) ||
                        !Contains(node.Description.RedirectSlots, decision.TargetSlot) ||
                        !node.Description.Targets.TryGetValue(decision.TargetSlot, out actualTarget)) return EmberFsmResult.InvalidTarget;
                    if (++redirects > _redirectLimit) return EmberFsmResult.RedirectLimit;
                    redirect = true;
                    break;
                }
                if (!redirect) break;
            }

            _version++;
            if (_attempt != null) _attempt.CommitStarted = true;
            if (site != null)
            {
                try { site.Commit(source.State, _context); }
                catch (Exception ex) { Fault(source, "Commit", ex); return EmberFsmResult.Faulted; }
            }
            while (_path.Count > retained)
            {
                var node = _path[_path.Count - 1];
                _path.RemoveAt(_path.Count - 1);
                node.Active = false;
                try { node.State.Exit(); }
                catch (Exception ex) { Fault(node, "Exit", ex); return EmberFsmResult.Faulted; }
            }
            for (var i = retained; i < next.Count; i++)
            {
                var node = next[i];
                node.Generation++;
                node.Elapsed = 0;
                node.Active = true;
                _path.Add(node);
                try { node.State.Enter(new EmberFsmActivation(this, node.Description.Id, node.Generation)); }
                catch (Exception ex) { Fault(node, "Enter", ex); return EmberFsmResult.Faulted; }
            }
            if (_attempt != null) _attempt.CommitCompleted = true;
            return EmberFsmResult.Changed;
        }

        private static bool Contains(IReadOnlyList<string> values, string target)
        {
            for (var i = 0; i < values.Count; i++) if (values[i] == target) return true;
            return false;
        }

        private EmberFsmResult Dispatch(float delta, bool physics)
        {
            var blocked = CheckControl();
            if (blocked.HasValue) return blocked.Value;
            if (_dispatching) return EmberFsmResult.Reentrant;
            if (Status == EmberFsmStatus.Paused) return EmberFsmResult.Paused;
            if (Status != EmberFsmStatus.Running) return EmberFsmResult.NotRunning;
            if (float.IsNaN(delta) || float.IsInfinity(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
            _dispatching = true;
            _dispatchFailure = null;
            _dispatchId++;
            _dispatchPhase = physics ? EmberFsmTriggerPhase.FixedUpdate : EmberFsmTriggerPhase.Update;
            var version = _version;
            try
            {
                // 时间只使用逻辑 Update 域；先为整条旧路径累计，父级切换不遗漏旧子状态已经经过的时间。
                if (!physics) for (var i = 0; i < _path.Count; i++) _path[i].Elapsed += delta;
                for (var i = 0; i < _path.Count; i++)
                {
                    var node = _path[i];
                    try
                    {
                        if (physics) node.State.FixedUpdate(delta);
                        else node.State.Update(delta);
                    }
                    catch (Exception ex) { Fault(node, physics ? "FixedUpdate" : "Update", ex); return EmberFsmResult.Faulted; }
                    if (Status == EmberFsmStatus.Faulted) return EmberFsmResult.Faulted;
                    if (_dispatchFailure.HasValue) return _dispatchFailure.Value;
                    if (_version != version || Status != EmberFsmStatus.Running) break;
                }
                return _version == version ? EmberFsmResult.NoChange : EmberFsmResult.Changed;
            }
            finally { _dispatching = false; _dispatchPhase = EmberFsmTriggerPhase.External; _dispatchFailure = null; }
        }

        private EmberFsmResult SetPaused(bool paused)
        {
            var blocked = CheckControl();
            if (blocked.HasValue) return blocked.Value;
            if (Status == EmberFsmStatus.Stopped) return EmberFsmResult.NotRunning;
            if ((Status == EmberFsmStatus.Paused) == paused) return EmberFsmResult.NoChange;
            _busy = true;
            try
            {
                Status = paused ? EmberFsmStatus.Paused : EmberFsmStatus.Running;
                _version++;
                foreach (var node in _path)
                {
                    try { if (paused) node.State.Pause(); else node.State.Resume(); }
                    catch (Exception ex) { Fault(node, paused ? "Pause" : "Resume", ex); return EmberFsmResult.Faulted; }
                }
                return Complete(EmberFsmResult.Changed, paused ? "Pause" : "Resume", EmberFsmRequestOrigin.Host);
            }
            finally { _busy = false; }
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        /// <summary>立即校验和快照配置；首次 Start 创建并初始化状态。context 类型必须与定义的泛型上下文一致。</summary>
        public EmberFsm(EmberFsmGraph graph, TContext context, int historyCapacity = 64, int redirectLimit = 16,
            EmberFsmRulePlan<TContext> rules = null)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            if (historyCapacity < 0) throw new ArgumentOutOfRangeException(nameof(historyCapacity));
            if (redirectLimit < 1) throw new ArgumentOutOfRangeException(nameof(redirectLimit));
            _historyCapacity = historyCapacity; _redirectLimit = redirectLimit;
            Description = graph.Inspect(typeof(TContext));
            if (!Description.IsValid) throw new ArgumentException(string.Join("\n", Description.Errors), nameof(graph));
            rules?.Validate(Description); _rules = rules;
            foreach (var description in Description.Nodes)
            {
                var node = new RuntimeNode { Description = description };
                foreach (var transition in description.Transitions)
                    foreach (var site in transition.Sites) node.Sites.Add(new SiteBinding(transition, site));
                node.Sites.Sort((a, b) => a.Site.Order.CompareTo(b.Site.Order));
                _nodes.Add(description.Id, node); _ordered.Add(node);
            }
        }

        public EmberFsmResult Start()
        {
            var blocked = CheckControl();
            if (blocked.HasValue) return blocked.Value;
            if (_dispatching) return EmberFsmResult.Reentrant;
            if (Status != EmberFsmStatus.Stopped) return EmberFsmResult.NoChange;
            _busy = true;
            try
            {
                if (!Initialize()) return EmberFsmResult.Faulted;
                Status = EmberFsmStatus.Running;
                var result = Change(Description.EntryId, EmberFsmTransitionMode.PreserveAncestors, out var actual);
                if (result != EmberFsmResult.Changed && Status != EmberFsmStatus.Faulted) Status = EmberFsmStatus.Stopped;
                return Complete(result, "Start", EmberFsmRequestOrigin.Start, target: actual);
            }
            finally { _busy = false; }
        }

        /// <summary>宿主请求必须提交有效激活凭证和该节点原始声明句柄；没有按任意节点 ID 强制跳转入口。</summary>
        [HasGC]
        public EmberFsmResult Request(EmberFsmActivation activation, EmberFsmTransition transition) =>
            ((IEmberFsmRequests)this).Request(activation, transition, EmberFsmRequestOrigin.External);

        EmberFsmResult IEmberFsmRequests.Request(EmberFsmActivation activation, EmberFsmTransition transition, EmberFsmRequestOrigin origin)
            => AttemptInternal(activation, transition, null, origin, transition?.Sites.Count > 0).Result;

        [HasGC]
        public EmberFsmResult Update(float deltaTime) => Dispatch(deltaTime, false);
        [HasGC]
        public EmberFsmResult FixedUpdate(float fixedDeltaTime) => Dispatch(fixedDeltaTime, true);
        public EmberFsmResult Pause() => SetPaused(true);
        public EmberFsmResult Resume() => SetPaused(false);

        public EmberFsmResult Stop()
        {
            var blocked = CheckControl();
            if (blocked.HasValue) return blocked.Value;
            if (Status == EmberFsmStatus.Stopped) return EmberFsmResult.NoChange;
            _busy = true;
            try
            {
                _version++;
                while (_path.Count > 0)
                {
                    var node = _path[_path.Count - 1];
                    _path.RemoveAt(_path.Count - 1); node.Active = false;
                    try { node.State.Exit(); }
                    catch (Exception ex) { Fault(node, "Exit", ex); return EmberFsmResult.Faulted; }
                }
                Status = EmberFsmStatus.Stopped;
                return Complete(EmberFsmResult.Changed, "Stop", EmberFsmRequestOrigin.Host);
            }
            finally { _busy = false; }
        }

        /// <summary>提供可检查的最终释放结果；事务内拒绝释放。</summary>
        public EmberFsmResult TryDispose()
        {
            var blocked = CheckControl();
            if (blocked.HasValue && blocked != EmberFsmResult.Faulted) return blocked.Value;
            _busy = true;
            try
            {
                _version++;
                var before = CleanupFaultCount;
                CleanupPath(); ReleaseStates();
                Status = EmberFsmStatus.Disposed;
                return Complete(CleanupFaultCount == before ? EmberFsmResult.Changed : EmberFsmResult.Faulted,
                    "Dispose", EmberFsmRequestOrigin.Host);
            }
            finally { _busy = false; }
        }

        public void Dispose()
        {
            if (TryDispose() == EmberFsmResult.Reentrant)
                throw new InvalidOperationException("Cannot Dispose during FSM transaction; use TryDispose for an explicit result");
        }

        public EmberFsmActivation GetActivation(string nodeId) => nodeId != null && _nodes.TryGetValue(nodeId, out var node) && node.Active
            ? new EmberFsmActivation(this, nodeId, node.Generation) : default;
        public double GetElapsedTime(string nodeId) => _nodes[nodeId].Elapsed;
        public IReadOnlyList<string> GetActivePath() => Array.AsReadOnly(CopyPath());
        public IReadOnlyList<EmberFsmDiagnostic> GetHistory() => Array.AsReadOnly(_history.ToArray());
        #endregion
    }
}
