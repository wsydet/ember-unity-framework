using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Ember.Core.Fsm.Tests
{
    public sealed class ConditionTestContext
    {
        public int Queries, Entries, Commits;
        public bool BaseAllowed = true, ExtraAllowed = true, EntryAllowed = true, Throw;
        public ConditionTestState Source;
    }
    public sealed class ConditionTestDefinition : EmberFsmStateDefinition<ConditionTestContext, ConditionTestState>
    {
        public EmberFsmTransition[] Edges = Array.Empty<EmberFsmTransition>();
        public override ConditionTestState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => Edges;
    }
    public sealed class ConditionTestState : EmberFsmState<ConditionTestContext>
    {
        public EmberFsmActivation Token => Activation;
        public override void Initialize(ConditionTestContext context, string id)
        { base.Initialize(context, id); if (id == "a") context.Source = this; }
        public override EmberFsmEntryDecision CanEnter() => NodeId == "b" && !Context.EntryAllowed ? EmberFsmEntryDecision.Reject : EmberFsmEntryDecision.Allow;
        public override void Enter(EmberFsmActivation activation) { base.Enter(activation); Context.Entries++; }
        public override void FixedUpdate(float dt) { if (NodeId == "a") Activation.AttemptGroup("decision"); }
    }
    public sealed class EmberFsmConditionTests
    {
        private readonly List<UnityEngine.Object> _assets = new();
        private EmberFsm<ConditionTestContext> _fsm;
        private ConditionTestContext _context;
        private EmberFsmGraph _graph;
        private EmberFsmTransition<ConditionTestContext, ConditionTestState> _edge;
        private EmberConditionCatalog<ConditionTestContext> Catalog() => new(new EmberConditionLeaf<ConditionTestContext>("fact", "查询", Array.Empty<EmberConditionParameterSpec>(),
            p => (c, trace) => { c.Queries++; if (c.Throw) throw new InvalidOperationException("query fault"); return new EmberConditionSample(c.ExtraAllowed); }, p => "查询"));
        private T Keep<T>() where T : ScriptableObject { var a = ScriptableObject.CreateInstance<T>(); _assets.Add(a); return a; }
        private void Build(EmberConditionExpression expression = null)
        {
            _context = new(); var a = Keep<ConditionTestDefinition>(); var b = Keep<ConditionTestDefinition>(); _graph = Keep<EmberFsmGraph>();
            _edge = new("go", "to", sites: new[] { new EmberFsmRequestSite<ConditionTestContext, ConditionTestState>("fixed", EmberFsmTriggerPhase.FixedUpdate, "decision", 1,
                (s, c) => c.BaseAllowed, "基础", commit: (s, c) => c.Commits++) });
            a.Edges = new EmberFsmTransition[] { _edge };
            _graph.Configure("a", new EmberFsmNode("a", "a", a, bindings: new[] { new EmberFsmTargetBinding("to", "b") }), new EmberFsmNode("b", "b", b));
            var rule = new EmberFsmRulePlan<ConditionTestContext>(new EmberFsmRuleBinding<ConditionTestContext>("a", _edge, "fixed",
                Catalog().Compile(expression ?? new EmberConditionExpression("x", new EmberConditionNode("x", "fact")))));
            _fsm = new(_graph, _context, rules: rule); Assert.That(_fsm.Start(), Is.EqualTo(EmberFsmResult.Changed));
        }
        [TearDown] public void Cleanup() { _fsm?.Dispose(); foreach (var asset in _assets) UnityEngine.Object.DestroyImmediate(asset); _assets.Clear(); }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void EveryRequestPathUsesExtraExactlyOnceWithoutConsumptionOnRejection(int path)
        {
            Build(); _context.ExtraAllowed = false;
            if (path == 0) Assert.That(_fsm.Request(_context.Source.Token, _edge), Is.EqualTo(EmberFsmResult.GuardRejected));
            else if (path == 1) Assert.That(_fsm.Attempt(_context.Source.Token, _edge).Result, Is.EqualTo(EmberFsmResult.GuardRejected));
            else _fsm.FixedUpdate(.02f);
            Assert.That(_context.Queries, Is.EqualTo(1)); Assert.That(_context.Commits, Is.Zero); Assert.That(_fsm.CurrentLeafId, Is.EqualTo("a"));
        }
        [Test] public void BaseRejectionSkipsWholeTreeAndObservationNeverQueries()
        {
            Build(); _context.BaseAllowed = false; _fsm.Request(_context.Source.Token, _edge);
            var snapshot = EmberFsmObservation.Capture(_fsm);
            Assert.That(_context.Queries, Is.Zero); Assert.That(snapshot.LastOperation.Attempt.BaseAllowed, Is.False);
            Assert.That(snapshot.LastOperation.Attempt.Conditions.Single().Result, Is.Null); Assert.That(snapshot.Rules.Count, Is.EqualTo(1));
        }
        [TestCase(false)] [TestCase(true)]
        public void TargetPrecheckDoesNotReevaluateExpressionAndCommitsOnce(bool entry)
        {
            Build(); _context.EntryAllowed = entry;
            Assert.That(_fsm.Request(_context.Source.Token, _edge), Is.EqualTo(entry ? EmberFsmResult.Changed : EmberFsmResult.EntryRejected));
            Assert.That(_context.Queries, Is.EqualTo(1)); Assert.That(_context.Commits, Is.EqualTo(entry ? 1 : 0));
        }
        [Test] public void QueryExceptionFaultsAndDiagnosticsDisabledStillEnforcesRule()
        {
            Build(); _fsm.DiagnosticsEnabled = false; _context.ExtraAllowed = false;
            Assert.That(_fsm.Request(_context.Source.Token, _edge), Is.EqualTo(EmberFsmResult.GuardRejected));
            _context.Throw = true; Assert.That(_fsm.Request(_context.Source.Token, _edge), Is.EqualTo(EmberFsmResult.Faulted));
            Assert.That(_context.Commits, Is.Zero); Assert.That(_fsm.Status, Is.EqualTo(EmberFsmStatus.Faulted));
        }
        [TestCase("and", false, 1)] [TestCase("or", true, 1)] [TestCase("and", true, 2)] [TestCase("or", false, 2)]
        public void CompositionVisitsChildrenInOrderAndRecordsSkipped(string kind, bool fact, int calls)
        {
            var plan = Catalog().Compile(new EmberConditionExpression("root", new EmberConditionNode("root", kind, null, "first", "second"),
                new EmberConditionNode("first", "fact"), new EmberConditionNode("second", "fact")));
            var ctx = new ConditionTestContext { ExtraAllowed = fact }; var trace = new List<EmberConditionTrace>();
            Assert.That(plan.Evaluate(ctx, trace), Is.EqualTo(fact)); Assert.That(ctx.Queries, Is.EqualTo(calls));
            Assert.That(trace.Select(t => t.NodeId), Is.EqualTo(new[] { "root", "first", "second" }));
            Assert.That(trace[2].Result, Is.EqualTo(calls == 1 ? (bool?)null : fact));
        }
        [Test] public void InvalidTreesAreRejectedAndNotNegatesOnlyItsChild()
        {
            foreach (var invalid in new[] {
                new EmberConditionExpression("x", new EmberConditionNode("x", "and")),
                new EmberConditionExpression("x", new EmberConditionNode("x", "not", null, "x")),
                new EmberConditionExpression("x", new EmberConditionNode("x", "missing")),
                new EmberConditionExpression("x", new EmberConditionNode("x", "true"), new EmberConditionNode("unused", "true")) })
                Assert.Throws<ArgumentException>(() => Catalog().Compile(invalid));
            var plan = Catalog().Compile(new EmberConditionExpression("x", new EmberConditionNode("x", "not", null, "y"), new EmberConditionNode("y", "true")));
            Assert.That(plan.Evaluate(new ConditionTestContext()), Is.False);
        }
        [Test] public void BindingNeverAuthorizesForgedHandleOrUnknownSite()
        {
            Build(); var condition = Catalog().Compile(EmberConditionExpression.True());
            var fake = new EmberFsmTransition<ConditionTestContext, ConditionTestState>("go", "to");
            Assert.Throws<ArgumentException>(() => new EmberFsm<ConditionTestContext>(_graph, new(), rules:
                new EmberFsmRulePlan<ConditionTestContext>(new EmberFsmRuleBinding<ConditionTestContext>("a", fake, "fixed", condition))));
            Assert.Throws<ArgumentException>(() => new EmberFsm<ConditionTestContext>(_graph, new(), rules:
                new EmberFsmRulePlan<ConditionTestContext>(new EmberFsmRuleBinding<ConditionTestContext>("a", _edge, "other", condition))));
            Assert.That(_fsm.Request(_context.Source.Token, fake), Is.EqualTo(EmberFsmResult.UndeclaredTransition));
            Assert.That(_context.Queries, Is.Zero);
        }
    }
}
