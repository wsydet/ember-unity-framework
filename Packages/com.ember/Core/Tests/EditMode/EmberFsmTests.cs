using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Ember.Core.Fsm.Tests
{
    public sealed partial class EmberFsmTests
    {
        #region 内部参数
        private readonly List<UnityEngine.Object> _assets = new List<UnityEngine.Object>();
        private readonly List<EmberFsm<EmberFsmTestContext>> _machines = new List<EmberFsm<EmberFsmTestContext>>();
        private EmberFsmGraph _graph;
        private EmberFsmTestDefinition _definition;
        private EmberFsmTestContext _context;
        private EmberFsm<EmberFsmTestContext> _fsm;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private T Asset<T>() where T : ScriptableObject
        { var asset = ScriptableObject.CreateInstance<T>(); _assets.Add(asset); return asset; }
        private EmberFsmNode Node(string id, string parent = null, string child = null, EmberFsmTestDefinition definition = null)
        {
            return new EmberFsmNode(id, "repeated display name", definition ?? _definition, parent, child,
                new EmberFsmTargetBinding("move", "Move"), new EmberFsmTargetBinding("jump", "Jump"),
                new EmberFsmTargetBinding("fall", "Fall"), new EmberFsmTargetBinding("dead", "Dead"),
                new EmberFsmTargetBinding("ground", "Ground"), new EmberFsmTargetBinding("redirectJump", "Jump"),
                new EmberFsmTargetBinding("redirectMove", "Move"));
        }
        private EmberFsm<EmberFsmTestContext> Machine(EmberFsmTestContext context, int capacity = 8, int redirectLimit = 16)
        {
            var machine = new EmberFsm<EmberFsmTestContext>(_graph, context, capacity, redirectLimit);
            context.Machine = machine; _machines.Add(machine); return machine;
        }
        private void Start()
        { Assert.That(_fsm.Start(), Is.EqualTo(EmberFsmResult.Changed)); _context.Log.Clear(); }
        private EmberFsmResult Request(string node, EmberFsmTransition transition) => _fsm.Request(_fsm.GetActivation(node), transition);
        private string[] Lifecycle() => _context.Log.Where(x => x.StartsWith("enter:") || x.StartsWith("exit:")).ToArray();
        private void AssertPath(params string[] path) => CollectionAssert.AreEqual(path, _fsm.GetActivePath());
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        [SetUp]
        public void SetUp()
        {
            _definition = Asset<EmberFsmTestDefinition>(); _definition.Parameter = 3;
            var move = Asset<EmberFsmTestDefinition>(); move.Parameter = 9;
            _graph = Asset<EmberFsmGraph>();
            _graph.Configure("Alive", Node("Alive", child: "Ground"), Node("Ground", "Alive", "Idle"),
                Node("Idle", "Ground"), Node("Move", "Ground", definition: move), Node("Air", "Alive", "Jump"),
                Node("Jump", "Air"), Node("Fall", "Air"), Node("Dead"));
            _context = new EmberFsmTestContext(); _fsm = Machine(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _blueprintScope?.Dispose(); _blueprintScope=null;
            foreach (var machine in _machines) machine.Dispose();
            foreach (var asset in _assets) UnityEngine.Object.DestroyImmediate(asset);
            _machines.Clear(); _assets.Clear();
        }

        [Test]
        public void SameBehaviorDifferentAssetsAndRepeatedNamesRemainAddressable()
        {
            Start();
            Assert.That(_context.States["Idle"].Parameter, Is.EqualTo(3));
            Assert.That(Request("Idle", EmberFsmTestState.Move), Is.EqualTo(EmberFsmResult.Changed));
            Assert.That(_context.States["Move"].Parameter, Is.EqualTo(9));
            AssertPath("Alive", "Ground", "Move");
        }

        [Test]
        public void SharedDefinitionCreatesIndependentObjectsPerNodeAndHost()
        {
            Start(); var secondContext = new EmberFsmTestContext(); var second = Machine(secondContext);
            second.Start(); _fsm.Update(2);
            Assert.AreNotSame(_context.States["Alive"], _context.States["Idle"]);
            Assert.AreNotSame(_context.States["Idle"], secondContext.States["Idle"]);
            Assert.That(secondContext.States["Idle"].Ticks, Is.Zero);
            Assert.That(_context.States["Idle"].Ticks, Is.EqualTo(1));
            Assert.That(second.GetElapsedTime("Idle"), Is.Zero);
        }

        [Test]
        public void InitialEntryAndCrossBranchOrderRetainCommonAncestor()
        {
            _fsm.Start(); CollectionAssert.AreEqual(new[] { "enter:Alive", "enter:Ground", "enter:Idle" }, Lifecycle());
            _context.Log.Clear(); _fsm.Update(2);
            Request("Idle", EmberFsmTestState.Move);
            CollectionAssert.AreEqual(new[] { "exit:Idle", "enter:Move" }, Lifecycle());
            Assert.That(_fsm.GetElapsedTime("Alive"), Is.EqualTo(2));
            Assert.That(_fsm.GetElapsedTime("Ground"), Is.EqualTo(2));
            Assert.That(_fsm.GetElapsedTime("Move"), Is.Zero);
            _context.Log.Clear(); Request("Move", EmberFsmTestState.Jump);
            CollectionAssert.AreEqual(new[] { "exit:Move", "exit:Ground", "enter:Air", "enter:Jump" }, Lifecycle());
            Assert.That(_context.States["Alive"].EnterCount, Is.EqualTo(1));
            _context.Log.Clear(); Request("Jump", EmberFsmTestState.Fall);
            CollectionAssert.AreEqual(new[] { "exit:Jump", "enter:Fall" }, Lifecycle());
            _context.Log.Clear(); Request("Fall", EmberFsmTestState.Dead);
            CollectionAssert.AreEqual(new[] { "exit:Fall", "exit:Air", "exit:Alive", "enter:Dead" }, Lifecycle());
        }

        [TestCase("update")]
        [TestCase("fixed")]
        public void ParentTransitionStopsOldAndNewBranchDispatch(string phase)
        {
            Start();
            _context.Callback = (p, id, state) =>
            {
                if (p == phase && id == "Alive") Assert.That(state.Token.Request(EmberFsmTestState.Dead), Is.EqualTo(EmberFsmResult.Changed));
            };
            if (phase == "update") _fsm.Update(1); else _fsm.FixedUpdate(1);
            CollectionAssert.AreEqual(new[] { phase + ":Alive" }, _context.Log.Where(x => x.StartsWith(phase + ":")));
            AssertPath("Dead");
            Assert.That(_fsm.LastOperation.SourceNodeId, Is.EqualTo("Alive"));
            Assert.That(_fsm.LastOperation.Origin, Is.EqualTo(EmberFsmRequestOrigin.State));
        }

        [Test]
        public void DirectLeafEntrySkipsDefaults()
        {
            _graph.Configure("Fall", _graph.Nodes.ToArray());
            _fsm = Machine(_context); _fsm.Start();
            CollectionAssert.AreEqual(new[] { "enter:Alive", "enter:Air", "enter:Fall" }, Lifecycle());
        }

        [Test]
        public void ActiveAncestorRestoresDefaultWithoutReenteringParentAndExplicitReentryRestartsBranch()
        {
            Start(); _fsm.Update(5); Request("Idle", EmberFsmTestState.Move); _context.Log.Clear();
            Request("Move", EmberFsmTestState.Ground);
            CollectionAssert.AreEqual(new[] { "exit:Move", "enter:Idle" }, Lifecycle());
            Assert.That(_fsm.GetElapsedTime("Ground"), Is.EqualTo(5));
            _context.Log.Clear(); Assert.That(Request("Idle", EmberFsmTestState.Ground), Is.EqualTo(EmberFsmResult.NoChange));
            Assert.IsEmpty(Lifecycle());
            Request("Ground", EmberFsmTestState.ReenterGround);
            CollectionAssert.AreEqual(new[] { "exit:Idle", "exit:Ground", "enter:Ground", "enter:Idle" }, Lifecycle());
            Assert.That(_fsm.GetElapsedTime("Alive"), Is.EqualTo(5));
            Assert.That(_fsm.GetElapsedTime("Ground"), Is.Zero);
        }

        [Test]
        public void GuardRejectionAndEntryRejectionLeaveOriginalPathUntouched()
        {
            Start(); _context.Allow = false;
            Assert.That(Request("Idle", EmberFsmTestState.Move), Is.EqualTo(EmberFsmResult.GuardRejected));
            Assert.IsEmpty(_context.Log); Assert.That(_context.GuardCount, Is.EqualTo(1));
            _context.Allow = true; _context.Entry = id => id == "Jump" ? EmberFsmEntryDecision.Reject : EmberFsmEntryDecision.Allow;
            Assert.That(Request("Idle", EmberFsmTestState.Jump), Is.EqualTo(EmberFsmResult.EntryRejected));
            Assert.IsEmpty(Lifecycle()); AssertPath("Alive", "Ground", "Idle");
        }

        [Test]
        public void RedirectSuccessReportsActualTargetAndDoesNotEnterIntermediateNode()
        {
            Start(); _context.Entry = id => id == "Move" ? EmberFsmEntryDecision.Redirect("redirectJump") : EmberFsmEntryDecision.Allow;
            Assert.That(Request("Idle", EmberFsmTestState.Move), Is.EqualTo(EmberFsmResult.Changed));
            AssertPath("Alive", "Air", "Jump");
            Assert.That(_fsm.LastOperation.TargetId, Is.EqualTo("Jump"));
            Assert.That(_context.States["Move"].EnterCount, Is.Zero);
            Assert.That(_context.GuardCount, Is.EqualTo(1));
        }

        [TestCase("invalid", EmberFsmResult.InvalidTarget)]
        [TestCase("reject", EmberFsmResult.EntryRejected)]
        [TestCase("cycle", EmberFsmResult.RedirectCycle)]
        public void RedirectFailureNeverExitsOldPath(string mode, EmberFsmResult expected)
        {
            Start();
            _context.Entry = id => id == "Move" ? EmberFsmEntryDecision.Redirect(mode == "invalid" ? "unknown" : "redirectJump") :
                id == "Jump" ? (mode == "cycle" ? EmberFsmEntryDecision.Redirect("redirectMove") : EmberFsmEntryDecision.Reject) : EmberFsmEntryDecision.Allow;
            Assert.That(Request("Idle", EmberFsmTestState.Move), Is.EqualTo(expected));
            Assert.IsEmpty(Lifecycle()); AssertPath("Alive", "Ground", "Idle");
        }

        [Test]
        public void RedirectLimitTerminatesBeforeMutation()
        {
            _fsm = Machine(_context, redirectLimit: 1); Start();
            _context.Entry = id => id == "Move" ? EmberFsmEntryDecision.Redirect("redirectJump") :
                id == "Jump" ? EmberFsmEntryDecision.Redirect("redirectMove") : EmberFsmEntryDecision.Allow;
            Assert.That(Request("Idle", EmberFsmTestState.Move), Is.EqualTo(EmberFsmResult.RedirectLimit));
            Assert.IsEmpty(Lifecycle());
        }

        [TestCase("enter")]
        [TestCase("exit")]
        [TestCase("check")]
        [TestCase("pause")]
        [TestCase("resume")]
        public void LifecycleTransactionRejectsNestedControl(string phase)
        {
            Start(); var results = new List<EmberFsmResult>();
            _context.Callback = (p, id, state) =>
            {
                if (p != phase) return;
                results.Add(Request("Alive", EmberFsmTestState.Dead));
                results.Add(_fsm.Stop()); results.Add(_fsm.TryDispose()); results.Add(_fsm.Update(0));
            };
            Request("Idle", EmberFsmTestState.Move); _fsm.Pause(); _fsm.Resume();
            Assert.IsNotEmpty(results); Assert.That(results, Is.All.EqualTo(EmberFsmResult.Reentrant));
            AssertPath("Alive", "Ground", "Move"); _context.Callback = null;
        }

        [Test]
        public void GuardAndObserversCannotNestTransitions()
        {
            Start(); EmberFsmResult guardResult = default, observerResult = default;
            _context.GuardCallback = () => guardResult = Request("Alive", EmberFsmTestState.Dead);
            _fsm.Diagnostic += _ => observerResult = Request("Alive", EmberFsmTestState.Dead);
            Assert.That(Request("Idle", EmberFsmTestState.Move), Is.EqualTo(EmberFsmResult.Changed));
            Assert.That(guardResult, Is.EqualTo(EmberFsmResult.Reentrant));
            Assert.That(observerResult, Is.EqualTo(EmberFsmResult.Reentrant));
        }

        [Test]
        public void OldActivationAndForeignHostTokensCannotImpersonateCurrentActivation()
        {
            Start(); var old = _fsm.GetActivation("Idle"); var parent = _fsm.GetActivation("Alive");
            Request("Idle", EmberFsmTestState.Move);
            Assert.That(old.Request(EmberFsmTestState.Dead), Is.EqualTo(EmberFsmResult.StaleActivation));
            Request("Move", EmberFsmTestState.Ground);
            Assert.That(old.Request(EmberFsmTestState.Dead), Is.EqualTo(EmberFsmResult.StaleActivation));
            var other = Machine(new EmberFsmTestContext()); other.Start();
            Assert.That(other.Request(parent, EmberFsmTestState.Dead), Is.EqualTo(EmberFsmResult.StaleActivation));
            Assert.That(parent.Request(EmberFsmTestState.Dead), Is.EqualTo(EmberFsmResult.Changed));
        }

        [Test]
        public void MatchingStringIdCannotForgeDeclaredHandle()
        {
            Start(); var forged = new EmberFsmTransition<EmberFsmTestContext, EmberFsmTestState>("move", "move");
            Assert.That(Request("Idle", forged), Is.EqualTo(EmberFsmResult.UndeclaredTransition));
            Assert.That(_context.GuardCount, Is.Zero);
        }

        [Test]
        public void PauseResumeTimingStopRestartAndFinalRelease()
        {
            Start(); var old = _fsm.GetActivation("Idle"); _fsm.Update(2); _fsm.FixedUpdate(100);
            Assert.That(_fsm.GetElapsedTime("Idle"), Is.EqualTo(2)); _context.Log.Clear();
            _fsm.Pause(); _fsm.Update(100); _fsm.FixedUpdate(100);
            Assert.That(Request("Idle", EmberFsmTestState.Move), Is.EqualTo(EmberFsmResult.Paused));
            Assert.That(_fsm.GetElapsedTime("Idle"), Is.EqualTo(2));
            _fsm.Resume();
            CollectionAssert.AreEqual(new[] { "pause:Alive", "pause:Ground", "pause:Idle", "resume:Alive", "resume:Ground", "resume:Idle" }, _context.Log);
            _fsm.Pause(); _fsm.Stop(); AssertPath();
            Assert.That(_context.States["Idle"].DisposeCount, Is.Zero);
            _fsm.Start(); Assert.That(_context.States["Idle"].InitializeCount, Is.EqualTo(1));
            Assert.That(_context.States["Idle"].EnterCount, Is.EqualTo(2));
            Assert.That(_fsm.GetElapsedTime("Idle"), Is.Zero);
            Assert.That(old.Request(EmberFsmTestState.Move), Is.EqualTo(EmberFsmResult.StaleActivation));
            _fsm.Dispose(); _fsm.Dispose();
            Assert.That(_context.States.Values.Select(x => x.DisposeCount), Is.All.EqualTo(1));
            Assert.That(_fsm.Start(), Is.EqualTo(EmberFsmResult.Disposed));
        }

        [TestCase("enter", "Move", "Enter")]
        [TestCase("exit", "Idle", "Exit")]
        [TestCase("check", "Move", "CanEnter")]
        [TestCase("update", "Ground", "Update")]
        [TestCase("fixed", "Ground", "FixedUpdate")]
        [TestCase("pause", "Ground", "Pause")]
        [TestCase("resume", "Ground", "Resume")]
        public void CallbackFaultStopsDispatchAndPerformsBoundedCleanup(string phase, string node, string diagnosticPhase)
        {
            Start();
            _context.Callback = (p, id, state) => { if (p == phase && id == node) throw new InvalidOperationException("test fault"); };
            if (phase == "update") _fsm.Update(1);
            else if (phase == "fixed") _fsm.FixedUpdate(1);
            else if (phase == "pause") _fsm.Pause();
            else if (phase == "resume") { _fsm.Pause(); _fsm.Resume(); }
            else Assert.That(Request("Idle", EmberFsmTestState.Move), Is.EqualTo(EmberFsmResult.Faulted));
            Assert.That(_fsm.Status, Is.EqualTo(EmberFsmStatus.Faulted));
            Assert.That(_fsm.LastFault.Phase, Is.EqualTo(diagnosticPhase)); Assert.That(_fsm.LastFault.FaultNodeId, Is.EqualTo(node));
            AssertPath(); var count = _context.Log.Count; _fsm.Update(1); _fsm.FixedUpdate(1); _fsm.Start();
            Assert.That(_context.Log.Count, Is.EqualTo(count));
            Assert.That(_context.States.Values.Select(x => x.DisposeCount), Is.All.EqualTo(1));
            Assert.That(_context.States["Idle"].ExitCount, Is.EqualTo(1));
        }

        [Test]
        public void InitializeFailureReleasesPartiallyInitializedObject()
        {
            _context.Callback = (phase, id, state) => { if (phase == "init" && id == "Ground") throw new Exception("init"); };
            Assert.That(_fsm.Start(), Is.EqualTo(EmberFsmResult.Faulted));
            Assert.That(_fsm.LastFault.Phase, Is.EqualTo("Initialize"));
            Assert.That(_context.States.Values.Select(x => x.DisposeCount), Is.All.EqualTo(1));
            Assert.IsEmpty(Lifecycle());
        }

        [Test]
        public void GuardFaultAndCleanupFaultsPreservePrimaryCause()
        {
            Start(); _context.GuardCallback = () => throw new Exception("guard");
            _context.Callback = (phase, id, state) => { if (phase == "exit" || phase == "dispose") throw new Exception("cleanup"); };
            Assert.That(Request("Idle", EmberFsmTestState.Move), Is.EqualTo(EmberFsmResult.Faulted));
            Assert.That(_fsm.LastFault.Phase, Is.EqualTo("Guard"));
            Assert.That(_fsm.CleanupFaultCount, Is.EqualTo(11)); AssertPath();
            _fsm.Dispose(); Assert.That(_context.States.Values.Select(x => x.DisposeCount), Is.All.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CallerThrowingAfterFaultedRequestDoesNotOverwriteFirstFault(bool diagnostics)
        {
            Start(); _fsm.DiagnosticsEnabled = diagnostics;
            _context.GuardCallback = () => throw new Exception("first");
            _context.Callback = (phase, id, state) =>
            {
                if (phase != "update" || id != "Alive") return;
                state.Token.Request(EmberFsmTestState.Dead);
                throw new Exception("second");
            };
            Assert.That(_fsm.Update(1), Is.EqualTo(EmberFsmResult.Faulted));
            Assert.That(_fsm.LastFault.Phase, Is.EqualTo("Guard"));
            Assert.That(_fsm.LastFault.Exception.Message, Is.EqualTo("first"));
            Assert.That(_context.States.Values.Select(x => x.DisposeCount), Is.All.EqualTo(1));
        }

        [Test]
        public void ReusedFactoryObjectIsRejectedWithoutDisposingOtherHostsObject()
        {
            var shared = new EmberFsmTestState(1); _definition.Factory = () => shared;
            Assert.That(_fsm.Start(), Is.EqualTo(EmberFsmResult.Faulted));
            Assert.That(_fsm.LastFault.Phase, Is.EqualTo("Create")); Assert.That(shared.DisposeCount, Is.EqualTo(1));
            var second = Machine(new EmberFsmTestContext());
            Assert.That(second.Start(), Is.EqualTo(EmberFsmResult.Faulted)); Assert.That(shared.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void DeclarationsAreReadableWithoutContextFactoryLifecycleOrConditions()
        {
            var snapshot = _graph.Inspect(); Assert.IsTrue(snapshot.IsValid);
            Assert.That(snapshot.Nodes.Count, Is.EqualTo(8)); Assert.That(_definition.CreateCount, Is.Zero);
            Assert.That(_context.GuardCount, Is.Zero); Assert.IsEmpty(_context.Log);
            Assert.AreSame(EmberFsmTestState.Move, snapshot.Nodes[0].Transitions[0]);
            Assert.That(snapshot.Nodes[0].Targets["move"], Is.EqualTo("Move"));
            Start(); EmberFsmResult result = default;
            _definition.DeclarationCallback = () => result = Request("Alive", EmberFsmTestState.Dead);
            Assert.IsTrue(_graph.Inspect().IsValid); Assert.That(result, Is.EqualTo(EmberFsmResult.Reentrant));
            Assert.That(_context.GuardCount, Is.Zero);
        }

        [TestCase("duplicate")]
        [TestCase("definition")]
        [TestCase("parent")]
        [TestCase("cycle")]
        [TestCase("default")]
        [TestCase("missingDefault")]
        [TestCase("target")]
        [TestCase("binding")]
        [TestCase("entry")]
        [TestCase("context")]
        public void InvalidConfigurationFailsBeforeFactoryOrLifecycle(string error)
        {
            var nodes = _graph.Nodes.ToList();
            if (error == "duplicate") nodes.Add(Node("Idle"));
            if (error == "definition") nodes.Add(new EmberFsmNode("Broken", "", null));
            if (error == "parent") nodes[2] = Node("Idle", "missing");
            if (error == "cycle") { nodes[0] = Node("Alive", "Ground", "Ground"); }
            if (error == "default") nodes[0] = Node("Alive", child: "Idle");
            if (error == "missingDefault") nodes[0] = Node("Alive");
            if (error == "target") nodes.RemoveAt(3);
            if (error == "binding") nodes[2] = new EmberFsmNode("Idle", "", _definition, "Ground");
            _graph.Configure(error == "entry" ? "missing" : "Alive", nodes.ToArray());
            var result = _graph.Inspect(error == "context" ? typeof(object) : typeof(EmberFsmTestContext));
            Assert.IsFalse(result.IsValid); Assert.IsNotEmpty(result.Errors);
            if (error != "context") Assert.Throws<ArgumentException>(() => Machine(new EmberFsmTestContext()));
            else Assert.Throws<ArgumentException>(() => new EmberFsm<object>(_graph, new object()));
            Assert.That(_definition.CreateCount, Is.Zero); Assert.IsEmpty(_context.Log);
        }

        [Test]
        public void StructureSnapshotSurvivesGraphReconfiguration()
        {
            _graph.Configure("missing"); Start();
            Assert.That(Request("Idle", EmberFsmTestState.Move), Is.EqualTo(EmberFsmResult.Changed));
        }

        [Test]
        public void RejectedStartCanRetryWithoutReinitializingOrEnteringPartialPath()
        {
            _context.Entry = id => id == "Idle" ? EmberFsmEntryDecision.Reject : EmberFsmEntryDecision.Allow;
            Assert.That(_fsm.Start(), Is.EqualTo(EmberFsmResult.EntryRejected));
            Assert.That(_fsm.Status, Is.EqualTo(EmberFsmStatus.Stopped)); Assert.IsEmpty(Lifecycle());
            _context.Entry = null; Start();
            Assert.That(_context.States.Values.Select(x => x.InitializeCount), Is.All.EqualTo(1));
            Assert.That(_fsm.Start(), Is.EqualTo(EmberFsmResult.NoChange));
        }

        [Test]
        public void NestedDispatchIsRejectedAndPausedParentStopsCurrentDispatch()
        {
            Start();
            _context.Callback = (phase, id, state) =>
            {
                if (phase != "update" || id != "Alive") return;
                Assert.That(_fsm.Update(1), Is.EqualTo(EmberFsmResult.Reentrant));
                Assert.That(_fsm.FixedUpdate(1), Is.EqualTo(EmberFsmResult.Reentrant));
                _fsm.Pause();
            };
            _fsm.Update(1);
            CollectionAssert.AreEqual(new[] { "update:Alive" }, _context.Log.Where(x => x.StartsWith("update:")));
            Assert.That(_fsm.GetElapsedTime("Idle"), Is.EqualTo(1));
            Assert.That(_fsm.Status, Is.EqualTo(EmberFsmStatus.Paused));
            Assert.IsNull(_fsm.LastFault);
        }

        [Test]
        public void DeclarationExceptionAndRecursiveInspectionAreValidationFailures()
        {
            _definition.DeclarationCallback = () => throw new Exception("declaration");
            Assert.IsFalse(_graph.Inspect().IsValid);
            _definition.DeclarationCallback = () => _graph.Inspect();
            Assert.IsFalse(_graph.Inspect().IsValid);
            Assert.That(_definition.CreateCount, Is.Zero);
            _definition.DeclarationCallback = null;
        }

        [Test]
        public void LiveFactoryObjectCannotBeSharedAcrossHosts()
        {
            // 第二个宿主的错误工厂返回第一个宿主正在使用的对象。
            Start(); var existing = _context.States["Alive"];
            _definition.Factory = () => existing;
            var second = Machine(new EmberFsmTestContext());
            Assert.That(second.Start(), Is.EqualTo(EmberFsmResult.Faulted));
            Assert.That(existing.DisposeCount, Is.Zero);
            Assert.That(_fsm.Status, Is.EqualTo(EmberFsmStatus.Running));
        }

        [Test]
        public void StopAndDisposeFailuresAreNeverReportedAsSuccess()
        {
            Start();
            _context.Callback = (phase, id, state) => { if (phase == "exit" && id == "Idle") throw new Exception("stop"); };
            Assert.That(_fsm.Stop(), Is.EqualTo(EmberFsmResult.Faulted));
            Assert.That(_context.States["Idle"].ExitCount, Is.EqualTo(1)); AssertPath();
            var secondContext = new EmberFsmTestContext(); var second = Machine(secondContext); second.Start();
            secondContext.Callback = (phase, id, state) => { if (phase == "dispose") throw new Exception("dispose"); };
            Assert.That(second.TryDispose(), Is.EqualTo(EmberFsmResult.Faulted));
            Assert.That(second.Status, Is.EqualTo(EmberFsmStatus.Disposed));
            Assert.That(second.LastFault.Phase, Is.EqualTo("Dispose"));
            Assert.That(secondContext.States.Values.Select(x => x.DisposeCount), Is.All.EqualTo(1));
        }

        [Test]
        public void DiagnosticsAreBoundedAndDisablingThemDoesNotChangeBehavior()
        {
            Start(); var secondContext = new EmberFsmTestContext(); var second = Machine(secondContext, 0);
            second.DiagnosticsEnabled = false; second.Start(); secondContext.Log.Clear();
            _fsm.Diagnostic += _ => throw new Exception("observer");
            for (var i = 0; i < 12; i++)
            {
                _fsm.Update(1); second.Update(1);
                var edge = i % 2 == 0 ? EmberFsmTestState.Move : EmberFsmTestState.Ground;
                Request(_fsm.CurrentLeafId, edge); second.Request(second.GetActivation(second.CurrentLeafId), edge);
            }
            CollectionAssert.AreEqual(_context.Log, secondContext.Log);
            Assert.That(_context.GuardCount, Is.EqualTo(secondContext.GuardCount));
            Assert.That(_fsm.GetHistory().Count, Is.EqualTo(8)); Assert.IsEmpty(second.GetHistory());
            Assert.That(_fsm.ObserverFaultCount, Is.EqualTo(12));
            var record = _fsm.GetHistory()[0];
            Assert.That(record.InstanceId, Is.EqualTo(_fsm.InstanceId));
            Assert.That(record.Origin, Is.EqualTo(EmberFsmRequestOrigin.External));
            Assert.That(record.ActivePath.Count, Is.EqualTo(3));
        }
        #endregion
    }
}
