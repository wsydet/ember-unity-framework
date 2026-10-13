using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Ember.Core.Fsm.Tests
{
    public sealed class EmberFsmRuleContext
    {
        public readonly List<string> Log = new();
        public readonly Dictionary<string, EmberFsmRuleState> States = new();
        public Func<string, EmberFsmEntryDecision> Entry;
        public Action<EmberFsmRuleState> Tick;
        public Action<string> Callback;
        public bool First = true;
        public int Queries, Commits;
        public EmberFsm<EmberFsmRuleContext> Fsm;
    }
    public sealed class EmberFsmRuleDefinition : EmberFsmStateDefinition<EmberFsmRuleContext, EmberFsmRuleState>
    {
        public EmberFsmTransition[] Edges = Array.Empty<EmberFsmTransition>();
        public override EmberFsmRuleState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => Edges;
        public override IReadOnlyList<string> GetRedirectSlots() => new[] { "redirect" };
    }
    public sealed class EmberFsmRuleState : EmberFsmState<EmberFsmRuleContext>
    {
        public string Id => NodeId;
        public EmberFsmActivation Token => Activation;
        private void Notify(string phase) { Context.Log.Add(phase + ":" + NodeId); Context.Callback?.Invoke(phase + ":" + NodeId); }
        public override void Initialize(EmberFsmRuleContext context, string id) { base.Initialize(context, id); context.States.Add(id, this); }
        public override EmberFsmEntryDecision CanEnter() { Notify("check"); return Context.Entry?.Invoke(NodeId) ?? EmberFsmEntryDecision.Allow; }
        public override void Enter(EmberFsmActivation token) { base.Enter(token); Notify("enter"); }
        public override void Exit() => Notify("exit");
        public override void Dispose() => Notify("dispose");
        public override void FixedUpdate(float dt) { Notify("fixed"); Context.Tick?.Invoke(this); }
        public override void Update(float dt) { Notify("update"); Context.Tick?.Invoke(this); }
    }

    public sealed class EmberFsmRuleTests
    {
        private readonly List<UnityEngine.Object> _assets = new();
        private EmberFsmRuleContext _ctx;
        private EmberFsm<EmberFsmRuleContext> _fsm;
        private EmberFsmTransition<EmberFsmRuleContext, EmberFsmRuleState> _first, _second;
        private EmberFsmGraph _graph;
        private T Keep<T>() where T : ScriptableObject { var value = ScriptableObject.CreateInstance<T>(); _assets.Add(value); return value; }
        private EmberFsmRequestSite<EmberFsmRuleContext, EmberFsmRuleState> Site(string id, int order,
            bool first = false, EmberFsmContinuation continuation = EmberFsmContinuation.UntilChanged)
            => new(id, EmberFsmTriggerPhase.FixedUpdate, "decision", order,
                (s, c) => { c.Queries++; c.Log.Add("query:" + id); c.Callback?.Invoke("query:" + id); return !first || c.First; },
                id, continuation, (s, c) => { c.Commits++; c.Log.Add("commit:" + id); c.Callback?.Invoke("commit:" + id); });
        private void Build(EmberFsmContinuation continuation = EmberFsmContinuation.UntilChanged, bool multi = false)
        {
            _ctx = new();
            _first = new("first", "b", sites: multi ? new[] { Site("wall", 10, true, continuation), Site("elapsed", 15, true) }
                : new[] { Site("first", 10, true, continuation) });
            _second = new("second", "c", sites: new[] { Site("second", 20) });
            var root = Keep<EmberFsmRuleDefinition>(); var source = Keep<EmberFsmRuleDefinition>();
            var target = Keep<EmberFsmRuleDefinition>(); source.Edges = new EmberFsmTransition[] { _second, _first };
            _graph = Keep<EmberFsmGraph>();
            _graph.Configure("root", new EmberFsmNode("root", "root", root, defaultChildId: "a", bindings: new[] { new EmberFsmTargetBinding("redirect", "c") }),
                new EmberFsmNode("a", "a", source, "root", bindings: new[] { new EmberFsmTargetBinding("b", "b"), new EmberFsmTargetBinding("c", "c"), new EmberFsmTargetBinding("redirect", "c") }),
                new EmberFsmNode("b", "b", target, "root", bindings: new[] { new EmberFsmTargetBinding("redirect", "c") }),
                new EmberFsmNode("c", "c", target, "root", bindings: new[] { new EmberFsmTargetBinding("redirect", "c") }));
            _fsm = new(_graph, _ctx, 16); _ctx.Fsm = _fsm;
            Assert.That(_fsm.Start(), Is.EqualTo(EmberFsmResult.Changed)); _ctx.Log.Clear();
        }
        [TearDown] public void Cleanup()
        {
            if (_ctx != null) _ctx.Callback = null;
            _fsm?.Dispose(); foreach (var asset in _assets) UnityEngine.Object.DestroyImmediate(asset);
            _assets.Clear(); _fsm = null;
        }
        [TestCase(true)] [TestCase(false)]
        public void GroupUsesDeclaredOrderAndQueriesEachReachedSiteOnce(bool allowed)
        {
            Build(); _ctx.First = allowed;
            _ctx.Tick = s => { if (s.Id == "a" && !s.Token.AttemptGroup("decision").ShouldStop) _ctx.Log.Add("after"); };
            Assert.That(_fsm.FixedUpdate(.02f), Is.EqualTo(EmberFsmResult.Changed));
            Assert.That(_fsm.CurrentLeafId, Is.EqualTo(allowed ? "b" : "c"));
            Assert.That(_ctx.Queries, Is.EqualTo(allowed ? 1 : 2)); Assert.That(_ctx.Commits, Is.EqualTo(1));
            Assert.That(_ctx.Log, Does.Not.Contain("after")); Assert.That(_ctx.Log, Does.Not.Contain("fixed:b"));
            if (allowed) Assert.That(_fsm.GetHistory().Any(x => x.Attempt?.SkipReason != null && x.TransitionId == "second"), Is.True);
        }
        [TestCase(EmberFsmContinuation.UntilChanged, "c", 2)]
        [TestCase(EmberFsmContinuation.UntilMatched, "a", 1)]
        public void EntryRejectionRespectsSiteContinuation(EmberFsmContinuation policy, string leaf, int queries)
        {
            Build(policy); _ctx.Entry = id => id == "b" ? EmberFsmEntryDecision.Reject : EmberFsmEntryDecision.Allow;
            _ctx.States["a"].Token.AttemptGroup("decision");
            Assert.That(_fsm.CurrentLeafId, Is.EqualTo(leaf)); Assert.That(_ctx.Queries, Is.EqualTo(queries));
            Assert.That(_ctx.Log, Does.Not.Contain("commit:first"));
        }
        [Test] public void RedirectPrechecksFinishBeforeCommitAndGuardDoesNotRepeat()
        {
            Build(); _ctx.Entry = id => id == "b" ? EmberFsmEntryDecision.Redirect("redirect") : EmberFsmEntryDecision.Allow;
            _fsm.Attempt(_ctx.States["a"].Token, _first);
            CollectionAssert.AreEqual(new[] { "query:first", "check:b", "check:c", "commit:first", "exit:a", "enter:c" }, _ctx.Log);
            var record = _fsm.LastOperation.Attempt;
            Assert.That(record.Entries.Count, Is.EqualTo(2)); Assert.That(record.RequestedTarget, Is.EqualTo("b"));
            Assert.That(record.CommitCompleted, Is.True); Assert.That(_fsm.LastOperation.TargetId, Is.EqualTo("c"));
        }
        [Test] public void RedirectToCurrentPathIsNoChangeWithoutCommit()
        {
            var same = new EmberFsmTransition<EmberFsmRuleContext, EmberFsmRuleState>("same", "same", sites: new[] { Site("same", 1) });
            var definition = Keep<EmberFsmRuleDefinition>(); definition.Edges = new[] { same };
            var target = Keep<EmberFsmRuleDefinition>();
            var graph = Keep<EmberFsmGraph>(); graph.Configure("a",
                new EmberFsmNode("a", "a", definition, bindings: new[] { new EmberFsmTargetBinding("same", "b"), new EmberFsmTargetBinding("redirect", "a") }),
                new EmberFsmNode("b", "b", target, bindings: new[] { new EmberFsmTargetBinding("redirect", "a") }));
            var ctx = new EmberFsmRuleContext(); using var fsm = new EmberFsm<EmberFsmRuleContext>(graph, ctx); fsm.Start();
            ctx.Entry = id => id == "b" ? EmberFsmEntryDecision.Redirect("redirect") : EmberFsmEntryDecision.Allow;
            Assert.That(fsm.Attempt(ctx.States["a"].Token, same).Result, Is.EqualTo(EmberFsmResult.NoChange));
            Assert.That(ctx.Queries, Is.EqualTo(1)); Assert.That(ctx.Commits, Is.Zero);
            Assert.That(fsm.LastOperation.Attempt.Entries.Single().RedirectSlot, Is.EqualTo("redirect"));
        }
        [Test] public void MultiSiteRequiresExplicitPositionAndStaleTokensDoNotQuery()
        {
            Build(multi: true); var token = _ctx.States["a"].Token;
            Assert.That(token.Request(_first), Is.EqualTo(EmberFsmResult.RequestSiteRequired));
            Assert.That(token.Attempt(_first, "wrong").Result, Is.EqualTo(EmberFsmResult.InvalidRequestSite));
            Assert.That(_ctx.Queries, Is.Zero);
            Assert.That(token.Attempt(_first, "elapsed").Result, Is.EqualTo(EmberFsmResult.Changed));
            Assert.That(token.Attempt(_first, "wall").Result, Is.EqualTo(EmberFsmResult.StaleActivation));
            Assert.That(_ctx.Queries, Is.EqualTo(1));
        }
        [Test] public void ProtocolErrorStopsDispatchWithoutFaultingAndIndependentRequestCanRetry()
        {
            Build(multi: true);
            _ctx.Tick = s => { if (s.Id == "root") _fsm.Attempt(_ctx.States["a"].Token, _first); };
            Assert.That(_fsm.FixedUpdate(.02f), Is.EqualTo(EmberFsmResult.RequestSiteRequired));
            Assert.That(_ctx.Log, Does.Not.Contain("fixed:a")); Assert.That(_ctx.Queries, Is.Zero);
            Assert.That(_fsm.Status, Is.EqualTo(EmberFsmStatus.Running));
            Assert.That(_fsm.Attempt(_ctx.States["a"].Token, _first, "wall").Result, Is.EqualTo(EmberFsmResult.Changed));
        }
        [TestCase("query:first", "Guard", 0)] [TestCase("commit:first", "Commit", 1)]
        [TestCase("exit:a", "Exit", 1)] [TestCase("enter:b", "Enter", 1)]
        public void FailureRetainsFirstPhaseCleansOnceAndDoesNotRefund(string at, string phase, int commits)
        {
            Build(); _ctx.Callback = p => { if (p == at) throw new InvalidOperationException("expected rule failure"); };
            var result = _ctx.States["a"].Token.AttemptGroup("decision");
            Assert.That(result.Result, Is.EqualTo(EmberFsmResult.Faulted)); Assert.That(result.ShouldStop, Is.True);
            Assert.That(_fsm.LastFault.Phase, Is.EqualTo(phase)); Assert.That(_ctx.Commits, Is.EqualTo(commits));
            Assert.That(_ctx.Log.Count(x => x == "exit:a"), Is.EqualTo(1));
            Assert.That(_ctx.Log.Count(x => x == "dispose:a"), Is.EqualTo(1));
            Assert.That(_ctx.Log, Does.Not.Contain("query:second")); Assert.That(_fsm.LastFault.Attempt.SiteId, Is.EqualTo("first"));
        }
        [Test] public void NestedAttemptFromGuardCommitAndObserverDoesNotPoisonOuterTransaction()
        {
            Build(); var rejected = new List<EmberFsmResult>();
            _ctx.Callback = p => { if (p.StartsWith("query:") || p.StartsWith("commit:")) rejected.Add(_fsm.Attempt(_ctx.States["a"].Token, _second).Result); };
            _fsm.Diagnostic += _ => rejected.Add(_fsm.Attempt(_ctx.States["a"].Token, _second).Result);
            Assert.That(_fsm.Attempt(_ctx.States["a"].Token, _first).Result, Is.EqualTo(EmberFsmResult.Changed));
            Assert.That(rejected, Is.All.EqualTo(EmberFsmResult.Reentrant)); Assert.That(_ctx.Commits, Is.EqualTo(1));
        }
        [Test] public void ObservationAndStopNeverEvaluateSitesAndRecordsAreIndependentSnapshots()
        {
            Build(); _ctx.First = false; _fsm.Attempt(_ctx.States["a"].Token, _first);
            var before = EmberFsmObservation.Capture(_fsm); var record = before.LastOperation.Attempt;
            _graph.Inspect(); EmberFsmObservation.Capture(_fsm); _fsm.Stop(); _fsm.Dispose();
            Assert.That(_ctx.Queries, Is.EqualTo(1)); Assert.That(_ctx.Commits, Is.Zero);
            Assert.That(record.Allowed, Is.False); Assert.That(record.PreviousPath, Is.EqualTo(new[] { "root", "a" }));
        }
        [Test] public void EqualOrderIsValidationError()
        {
            Build(); var definition = (EmberFsmRuleDefinition)_graph.Nodes.First(x => x.Id == "a").Definition;
            var snapshot = _graph.Inspect();
            definition.Edges = new EmberFsmTransition[] { _first, new EmberFsmTransition<EmberFsmRuleContext, EmberFsmRuleState>("second", "c", sites: new[] { Site("second", 10) }) };
            Assert.That(_graph.Inspect().Errors.Any(x => x.Contains("duplicate request site order")), Is.True);
            Assert.That(_ctx.Queries, Is.Zero); Assert.That(snapshot.IsValid, Is.True);
        }
        [Test] public void ChangedSiteDescriptionDoesNotMatchFrozenOrigin()
        {
            Build(); var origin = EmberFsmObservationOrigin.Capture(_graph, _graph);
            var frozen = EmberFsmObservation.Capture(_fsm, origin);
            var definition = (EmberFsmRuleDefinition)_graph.Nodes.First(x => x.Id == "a").Definition;
            Assert.That(frozen.MatchesSource(_graph, _graph.Inspect()), Is.True);
            definition.Edges = new EmberFsmTransition[] { _second,
                new EmberFsmTransition<EmberFsmRuleContext, EmberFsmRuleState>("first", "b", sites: new[] { Site("changed-position", 10, true) }) };
            Assert.That(_graph.Inspect().IsValid, Is.True);
            Assert.That(frozen.MatchesSource(_graph, _graph.Inspect()), Is.False);
            Assert.That(_ctx.Queries, Is.Zero);
            Assert.That(_fsm.Attempt(_ctx.States["a"].Token, _first).Result, Is.EqualTo(EmberFsmResult.Changed));
            Assert.That(_fsm.LastOperation.Attempt.SiteId, Is.EqualTo("first"));
        }
        [TestCase(true)] [TestCase(false)]
        public void EventInsideUpdateUsesActualPhaseAndDiagnosticsDoNotChangeExecution(bool diagnostics)
        {
            Build(); _fsm.DiagnosticsEnabled = diagnostics;
            _ctx.Tick = s => { if (s.Id == "root") _fsm.Attempt(_ctx.States["a"].Token, _first); };
            Assert.That(_fsm.Update(.1f), Is.EqualTo(EmberFsmResult.Changed));
            Assert.That(_ctx.Log, Does.Not.Contain("update:a")); Assert.That(_ctx.Log, Does.Not.Contain("update:b"));
            Assert.That(_fsm.LastOperation.Attempt.ActualPhase, Is.EqualTo(EmberFsmTriggerPhase.Update));
            Assert.That(_fsm.LastOperation.Origin, Is.EqualTo(EmberFsmRequestOrigin.External));
            Assert.That(_ctx.Queries, Is.EqualTo(1)); Assert.That(_ctx.Commits, Is.EqualTo(1));
        }
    }
}
