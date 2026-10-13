using System;
using System.Linq;
using Ember.Core.Editor;
using NUnit.Framework;

namespace Ember.Core.Fsm.Tests
{
    public sealed partial class EmberFsmTests
    {
        private EmberFsmGraph CapturedObservationGraph(int parameter)
        {
            var graph = Asset<EmberFsmGraph>();
            graph.Configure(_graph.EntryId, _graph.Nodes.Select(n =>
            {
                var definition = Asset<EmberFsmTestDefinition>(); definition.Parameter = parameter;
                return new EmberFsmNode(n.Id, n.DisplayName, definition, n.ParentId, n.DefaultChildId, n.Bindings.ToArray());
            }).ToArray());
            return graph;
        }

        [Test]
        public void CapturedOriginMapsSharedSourcePerNodeAndKeepsTwoHostsIndependent()
        {
            var a = CapturedObservationGraph(11); var b = CapturedObservationGraph(22);
            var originA = EmberFsmObservationOrigin.Capture(_graph, a);
            var originB = EmberFsmObservationOrigin.Capture(_graph, b);
            var ca = new EmberFsmTestContext(); var cb = new EmberFsmTestContext();
            using var fa = new EmberFsm<EmberFsmTestContext>(a, ca);
            using var fb = new EmberFsm<EmberFsmTestContext>(b, cb);
            fa.Start(); fb.Start(); fa.Update(2);
            var sa = EmberFsmObservation.Capture(fa, originA); var sb = EmberFsmObservation.Capture(fb, originB);
            Assert.That(sa.MatchesSource(_graph, _graph.Inspect()), Is.True);
            Assert.That(sb.MatchesSource(_graph, _graph.Inspect()), Is.True);
            Assert.That(EmberFsmObservation.Capture(fa, originB).MatchesSource(_graph, _graph.Inspect()), Is.False);
            Assert.That(EmberFsmObservation.Capture(fa).MatchesSource(_graph, _graph.Inspect()), Is.False);
            Assert.That(ca.States["Idle"].Parameter, Is.EqualTo(11)); Assert.That(cb.States["Idle"].Parameter, Is.EqualTo(22));
            Assert.That(ca.States["Idle"], Is.Not.SameAs(ca.States["Ground"]));
            Assert.That(sb.Elapsed["Idle"], Is.Zero);
            EmberFsmObservationSources.Register("captured-origin", () => new[] {
                new EmberFsmObservedInstance(a, _graph, EmberFsmObservation.Capture(fa, originA)),
                new EmberFsmObservedInstance(b, _graph, EmberFsmObservation.Capture(fb, originB)) });
            var window = IsolatedLayoutWindow(_graph);
            try
            {
                window.ObserveInstance(fa.InstanceId); Assert.That(window.ObservedSnapshot.Elapsed["Idle"], Is.EqualTo(2));
                window.ObserveInstance(fb.InstanceId); Assert.That(window.ObservedSnapshot.Elapsed["Idle"], Is.Zero);
                Assert.That(window.ObservedSnapshot.Description.Nodes.First(n => n.Id == "Idle").Definition,
                    Is.SameAs(b.Nodes.First(n => n.Id == "Idle").Definition));
                Assert.That(ca.GuardCount + cb.GuardCount, Is.Zero);
            }
            finally { window.Close(); EmberFsmObservationSources.Remove("captured-origin"); }
        }

        [Test]
        public void OriginRejectsDifferentSourceIdentityAndReplacementDefinitionOfSameType()
        {
            var runtime = CapturedObservationGraph(11); var origin = EmberFsmObservationOrigin.Capture(_graph, runtime);
            var other = Asset<EmberFsmGraph>(); other.Configure(_graph.EntryId, _graph.Nodes.ToArray());
            Assert.That(origin.Matches(other, other.Inspect(), runtime.Inspect()), Is.False);
            var replacement = Asset<EmberFsmTestDefinition>();
            _graph.Configure(_graph.EntryId, _graph.Nodes.Select(n => new EmberFsmNode(n.Id, n.DisplayName,
                n.Id == "Idle" ? replacement : n.Definition, n.ParentId, n.DefaultChildId, n.Bindings.ToArray())).ToArray());
            Assert.That(origin.Matches(_graph, _graph.Inspect(), runtime.Inspect()), Is.False);
        }

        [Test]
        public void OriginFreezesStructureAndRejectsReboundTargetsOrChangedRuntime()
        {
            var runtime = CapturedObservationGraph(11); var origin = EmberFsmObservationOrigin.Capture(_graph, runtime);
            var original = _graph.Nodes.ToArray();
            _graph.Configure(_graph.EntryId, original.Select(n => new EmberFsmNode(n.Id, n.DisplayName,
                n.Definition, n.ParentId, n.DefaultChildId, n.Bindings.Select(b => new EmberFsmTargetBinding(b.Slot,
                    n.Id == "Idle" && b.Slot == "move" ? "Jump" : b.TargetId)).ToArray())).ToArray());
            Assert.That(origin.Matches(_graph, _graph.Inspect(), runtime.Inspect()), Is.False);
            Assert.Throws<ArgumentException>(() => EmberFsmObservationOrigin.Capture(_graph, runtime));
            _graph.Configure(_graph.EntryId, original);
            runtime.Configure("Dead", runtime.Nodes.ToArray());
            Assert.That(origin.Matches(_graph, _graph.Inspect(), runtime.Inspect()), Is.False);
            Assert.That(origin.SourceDescription.EntryId, Is.EqualTo("Alive"));
        }

        [Test]
        public void ObservationShowsRejectionFaultAndDisabledDiagnosticsWithoutReevaluation()
        {
            Start();_context.Allow=false;Request("Idle",EmberFsmTestState.Move);
            EmberFsmObservationSources.Register("test-errors",()=>new[]{new EmberFsmObservedInstance(_graph,_graph,EmberFsmObservation.Capture(_fsm))});
            var window=IsolatedLayoutWindow(_graph);
            try
            {
                window.ObserveInstance(_fsm.InstanceId);
                Assert.That(window.ObservedSnapshot.LastOperation.Result,Is.EqualTo(EmberFsmResult.GuardRejected));
                Assert.That(window.RecentRuntimeDeclarationKey,Is.Null);
                int guards=_context.GuardCount;window.RefreshRuntimeObservation();Assert.That(_context.GuardCount,Is.EqualTo(guards));
                _fsm.DiagnosticsEnabled=false;
                _context.Callback=(phase,id,state)=>{if(phase=="update")throw new InvalidOperationException("observation test fault");};
                _fsm.Update(1);window.RefreshRuntimeObservation();
                Assert.That(window.ObservedSnapshot.Status,Is.EqualTo(EmberFsmStatus.Faulted));
                Assert.That(window.ObservedSnapshot.LastFault.Exception.Message,Is.EqualTo("observation test fault"));
                Assert.That(window.ObservedSnapshot.ActivePath,Is.Empty);
                Assert.That(window.ObservedSnapshot.DiagnosticsEnabled,Is.False);
            }
            finally{window.Close();EmberFsmObservationSources.Remove("test-errors");}
        }
        [Test]
        public void ObservationFreezesReadOnlyFactsWithoutDrivingOrEvaluating()
        {
            Start();_fsm.Update(2);
            var before=EmberFsmObservation.Capture(_fsm);var log=_context.Log.ToArray();var guard=_context.GuardCount;
            for(int i=0;i<10;i++)EmberFsmObservation.Capture(_fsm);
            CollectionAssert.AreEqual(log,_context.Log);Assert.That(_context.GuardCount,Is.EqualTo(guard));
            Request("Idle",EmberFsmTestState.Move);_fsm.Update(1);
            var after=EmberFsmObservation.Capture(_fsm);
            Assert.That(before.ActivePath.Last(),Is.EqualTo("Idle"));Assert.That(before.Elapsed["Alive"],Is.EqualTo(2));
            Assert.That(after.ActivePath.Last(),Is.EqualTo("Move"));Assert.That(after.Elapsed["Alive"],Is.EqualTo(3));
            Assert.That(after.Elapsed["Move"],Is.EqualTo(1));
            _fsm.Pause();_fsm.Update(3);_fsm.FixedUpdate(3);
            Assert.That(EmberFsmObservation.Capture(_fsm).Elapsed["Move"],Is.EqualTo(1));
        }
        [Test]
        public void BlueprintObservationSelectsExactInstanceAndClearsEndedSource()
        {
            Start();_fsm.Update(2);var other=Machine(new EmberFsmTestContext());other.Start();
            bool present=true;
            EmberFsmObservationSources.Register("test-observer",()=>present?new[]{
                new EmberFsmObservedInstance(_graph,_graph,EmberFsmObservation.Capture(_fsm)),
                new EmberFsmObservedInstance(_definition,_graph,EmberFsmObservation.Capture(other))}:Array.Empty<EmberFsmObservedInstance>());
            var window=IsolatedLayoutWindow(_graph);
            try
            {
                window.ObserveInstance(_fsm.InstanceId);Assert.That(window.ObservedSnapshot.Elapsed["Idle"],Is.EqualTo(2));
                Request("Idle",EmberFsmTestState.Move);window.RefreshRuntimeObservation();
                Assert.That(window.RecentRuntimeDeclarationKey,Is.EqualTo("4:Idle:T:move"));
                window.SelectDeclaration("Ground",false,"jump");window.RefreshRuntimeObservation();
                Assert.That(window.RecentRuntimeDeclarationKey,Is.EqualTo("4:Idle:T:move"));
                window.ObserveInstance(other.InstanceId);Assert.That(window.ObservedSnapshot.Elapsed["Idle"],Is.Zero);
                window.SetCollapsed("Alive",true);window.RefreshRuntimeObservation();
                CollectionAssert.AreEqual(new[]{"Alive","Ground","Idle"},window.ObservedSnapshot.ActivePath);
                var log=_context.Log.ToArray();window.RefreshRuntimeObservation();CollectionAssert.AreEqual(log,_context.Log);
                present=false;window.RefreshRuntimeObservation();Assert.That(window.ObservedSnapshot,Is.Null);
                present=true;window.RefreshRuntimeObservation();Assert.That(window.ObservedSnapshot.InstanceId,Is.EqualTo(other.InstanceId));
            }
            finally{window.Close();EmberFsmObservationSources.Remove("test-observer");}
        }
    }
}
