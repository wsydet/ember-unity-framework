using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Ember.Core;
using NUnit.Framework;

namespace Ember.Scene.Tests
{
    public sealed class EmberSceneTransitionTests
    {
        private readonly List<string> _trace = new();
        private EmberStateMachine _fsm;
        private TestScenes _scenes;
        private OldState _old;
        private NextState _next;
        private OverlayState _overlay;

        [SetUp]
        public void SetUp()
        {
            _trace.Clear();
            _fsm = new EmberStateMachine();
            _old = new OldState(_trace);
            _next = new NextState(_trace);
            _overlay = new OverlayState(_trace);
            _fsm.Register(_old); _fsm.Register(_next); _fsm.Register(_overlay);
            _scenes = new TestScenes(_trace);
            _scenes.Loaded.Add("old");
            _fsm.OnSceneTransitionAsync = new EmberSceneTransitionRunner(_scenes).RunAsync;
            _fsm.Start<OldState>();
            _trace.Clear();
        }

        [Test]
        public void SceneManagerReinitializationRetiresOldGenerationOnly()
        {
            var scenes = new EmberSceneManager();
            var manager = (IEmberManager)scenes;
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var generation = typeof(EmberSceneManager).GetField("_transitionGeneration", flags);
            var retired = typeof(EmberSceneManager).GetMethod("IsRetired", flags);
            manager.Init();
            long previous = (long)generation.GetValue(scenes);
            manager.Destroy();
            manager.Init();
            Assert.That(retired.Invoke(scenes, new object[] { previous }), Is.True);
            Assert.That(retired.Invoke(scenes, new object[] { generation.GetValue(scenes) }), Is.False);
            Assert.DoesNotThrow(() => scenes.UnloadForTransitionAsync("missing-reentry-test-scene").GetAwaiter().GetResult());
            manager.Destroy();
        }

        [Test]
        public async Task StopWaitsForLoadAndOnlyReleasesAcquiredScene()
        {
            _scenes.LoadGate = new UniTaskCompletionSource();
            _scenes.UnloadGate = new UniTaskCompletionSource();
            var request = _fsm.RequestTransitionTo<NextState>();
            var stop = _fsm.StopTransitionsAsync();
            Assert.That(_fsm.StopTransitionsAsync(), Is.SameAs(stop));
            Assert.That(stop.IsCompleted, Is.False);
            _scenes.LoadGate.TrySetResult();
            Assert.That(stop.IsCompleted, Is.False);
            Assert.That(_trace, Does.Not.Contain("enter:next"));
            _scenes.UnloadGate.TrySetResult();
            Assert.That((await stop).Status, Is.EqualTo(EmberTransitionStatus.Cancelled));
            Assert.That(stop, Is.SameAs(request.Completion));
            CollectionAssert.AreEquivalent(new[] { "old" }, _scenes.Loaded);
        }

        [Test]
        public async Task StopAfterCommitWaitsForUnloadWithoutRewritingCommit()
        {
            _scenes.UnloadGate = new UniTaskCompletionSource();
            var request = _fsm.RequestTransitionTo<NextState>();
            var stop = _fsm.StopTransitionsAsync();
            Assert.That(stop.IsCompleted, Is.False);
            Assert.That(request.CanInteract, Is.False);
            _scenes.UnloadGate.TrySetResult();
            Assert.That((await stop).Status, Is.EqualTo(EmberTransitionStatus.Completed));
            Assert.That((await stop).CommitStarted, Is.True);
            CollectionAssert.AreEquivalent(new[] { "next" }, _scenes.Loaded);
        }

        [Test]
        public async Task StopInsideExit_DoesNotEnterOrPublishNextState()
        {
            _old.Exit = () => _fsm.StopTransitions();
            var result = await _fsm.RequestTransitionTo<NextState>().Completion;
            Assert.That(result.CommitStarted, Is.True);
            Assert.That(result.Status, Is.EqualTo(EmberTransitionStatus.CommitFailed));
            Assert.That(_trace, Does.Not.Contain("enter:next"));
            CollectionAssert.AreEquivalent(new[] { "old", "next" }, _scenes.Loaded);
        }

        [Test]
        public async Task Success_PreservesOldSceneThroughPrepareAndEnter_ThenUnloads()
        {
            _next.Prepare = context =>
            {
                Assert.That(_scenes.Loaded.Contains("old"), Is.True);
                Assert.That(_fsm.Current, Is.SameAs(_old));
                context.PreparedArgs = "candidate";
                return UniTask.CompletedTask;
            };
            _next.Enter = args => Assert.That(_scenes.Loaded.Contains("old"), Is.True);
            var result = await _fsm.RequestTransitionTo<NextState>("selection").Completion;
            Assert.That(result.Status, Is.EqualTo(EmberTransitionStatus.Completed));
            Assert.That(_next.Args, Is.EqualTo("candidate"));
            CollectionAssert.AreEqual(new[] { "load:next", "prepare:next", "exit:old", "enter:next", "unload:old" }, _trace);
        }

        [Test]
        public async Task PreparationFailure_AbortsBeforeUnloadingCandidate_LeavesOldState()
        {
            _next.Prepare = context => throw new InvalidOperationException("invalid progress");
            var result = await _fsm.RequestTransitionTo<NextState>().Completion;
            Assert.That(result.Status, Is.EqualTo(EmberTransitionStatus.PreparationFailed));
            Assert.That(_fsm.Current, Is.SameAs(_old));
            CollectionAssert.AreEqual(new[] { "load:next", "prepare:next", "abort:next", "unload:next" }, _trace);
            CollectionAssert.AreEquivalent(new[] { "old" }, _scenes.Loaded);
        }

        [Test]
        public async Task CancelDuringPrepare_WaitsForLateResult_RejectsConcurrentRequest_ThenAllowsRetry()
        {
            var ready = new UniTaskCompletionSource();
            SceneTransitionContext captured = null;
            _next.Prepare = context => { captured = context; return ready.Task; };
            var first = _fsm.RequestTransitionTo<NextState>();
            first.Cancel();
            Assert.That(first.Completion.IsCompleted, Is.False);
            Assert.That((await _fsm.RequestTransitionTo<NextState>().Completion).Status, Is.EqualTo(EmberTransitionStatus.Busy));
            ready.TrySetResult();
            Assert.That((await first.Completion).Status, Is.EqualTo(EmberTransitionStatus.Cancelled));
            captured.Proceed();
            Assert.That(_fsm.Current, Is.SameAs(_old));
            _next.Prepare = null;
            var retry = _fsm.RequestTransitionTo<NextState>();
            Assert.That((await retry.Completion).Status, Is.EqualTo(EmberTransitionStatus.Completed));
            Assert.That(retry.RequestId, Is.GreaterThan(first.RequestId));
            captured.Proceed();
            Assert.That(_trace.FindAll(item => item == "enter:next").Count, Is.EqualTo(1));
        }

        [Test]
        public async Task CancelDuringLoad_WaitsForUncancellableSceneThenReleasesIt()
        {
            _scenes.LoadGate = new UniTaskCompletionSource();
            var operation = _fsm.RequestTransitionTo<NextState>();
            operation.Cancel();
            Assert.That(operation.Completion.IsCompleted, Is.False);
            _scenes.LoadGate.TrySetResult();
            Assert.That((await operation.Completion).Status, Is.EqualTo(EmberTransitionStatus.Cancelled));
            CollectionAssert.AreEqual(new[] { "load:next", "unload:next" }, _trace);
        }

        [Test]
        public async Task PreCancelledRequest_DoesNotLoadOrPrepare()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var result = await _fsm.RequestTransitionTo<NextState>(cancellation: cancellation.Token).Completion;
            Assert.That(result.Status, Is.EqualTo(EmberTransitionStatus.Cancelled));
            Assert.That(_trace, Is.Empty);
        }

        [Test]
        public async Task Shutdown_InvalidatesDelayedProceed()
        {
            var ready = new UniTaskCompletionSource();
            _next.Prepare = context => ready.Task;
            var operation = _fsm.RequestTransitionTo<NextState>();
            _fsm.StopTransitions();
            ready.TrySetResult();
            Assert.That((await operation.Completion).Status, Is.EqualTo(EmberTransitionStatus.Cancelled));
            Assert.That((await _fsm.RequestTransitionTo<NextState>().Completion).Status, Is.EqualTo(EmberTransitionStatus.Stopped));
            Assert.That(_fsm.Current, Is.SameAs(_old));
        }

        [Test]
        public async Task ExistingTargetScene_IsNotOwnedOrUnloadedOnFailure()
        {
            _scenes.Loaded.Add("next");
            _next.Prepare = context => throw new InvalidOperationException("reject");
            await _fsm.RequestTransitionTo<NextState>().Completion;
            Assert.That(_scenes.Loaded.Contains("next"), Is.True);
            Assert.That(_trace, Does.Not.Contain("unload:next"));
        }

        [Test]
        public async Task MissingScene_ReportsFailureWithoutCallingPrepareOrExit()
        {
            _scenes.LoadError = "missing scene";
            var result = await _fsm.RequestTransitionTo<NextState>().Completion;
            Assert.That(result.Status, Is.EqualTo(EmberTransitionStatus.PreparationFailed));
            CollectionAssert.AreEqual(new[] { "load:next" }, _trace);
        }

        [Test]
        public async Task SkipSceneLoad_RequiresLoadedScene_AndStillPrepares()
        {
            var rejected = await _fsm.RequestTransitionTo<NextState>(skipSceneLoad: true).Completion;
            Assert.That(rejected.Status, Is.EqualTo(EmberTransitionStatus.PreparationFailed));
            _scenes.Loaded.Add("next");
            Assert.That((await _fsm.RequestTransitionTo<NextState>(skipSceneLoad: true).Completion).Status,
                Is.EqualTo(EmberTransitionStatus.Completed));
            Assert.That(_trace, Does.Contain("prepare:next"));
            Assert.That(_trace, Does.Not.Contain("load:next"));
        }

        [Test]
        public async Task EnterFailure_IsCommitFailure_NotRollback_AndRetainsBothScenes()
        {
            _next.Enter = args => throw new InvalidOperationException("enter failed");
            var result = await _fsm.RequestTransitionTo<NextState>().Completion;
            Assert.That(result.Status, Is.EqualTo(EmberTransitionStatus.CommitFailed));
            Assert.That(result.CommitStarted, Is.True);
            Assert.That(_fsm.Current, Is.SameAs(_next));
            CollectionAssert.AreEquivalent(new[] { "old", "next" }, _scenes.Loaded);
            Assert.That(_trace, Does.Not.Contain("abort:next"));
        }

        [Test]
        public async Task CancelInsideEnter_ReportsActualCommit()
        {
            using var cancellation = new CancellationTokenSource();
            _next.Enter = args => cancellation.Cancel();
            var result = await _fsm.RequestTransitionTo<NextState>(cancellation: cancellation.Token).Completion;
            Assert.That(result.Status, Is.EqualTo(EmberTransitionStatus.Completed));
            Assert.That(_scenes.Loaded.Contains("old"), Is.False);
        }

        [Test]
        public async Task DuplicateProceed_DoesNotDoubleEnter()
        {
            SceneTransitionContext captured = null;
            var runner = new EmberSceneTransitionRunner(_scenes);
            _fsm.OnSceneTransitionAsync = async context =>
            { captured = context; await runner.RunAsync(context); context.Proceed(); };
            await _fsm.RequestTransitionTo<NextState>().Completion;
            captured.Proceed();
            Assert.That(_trace.FindAll(item => item == "enter:next").Count, Is.EqualTo(1));
        }

        [Test]
        public async Task Completion_WaitsForOldSceneCleanup()
        {
            _scenes.UnloadGate = new UniTaskCompletionSource();
            var operation = _fsm.RequestTransitionTo<NextState>();
            Assert.That(_fsm.Current, Is.SameAs(_next));
            Assert.That(operation.Completion.IsCompleted, Is.False);
            Assert.That((await _fsm.RequestPop().Completion).Status, Is.EqualTo(EmberTransitionStatus.Busy));
            _scenes.UnloadGate.TrySetResult();
            Assert.That((await operation.Completion).Status, Is.EqualTo(EmberTransitionStatus.Completed));
        }

        [Test]
        public async Task CleanupFailure_IsReportedAndDoesNotClaimCancellationSuccess()
        {
            _next.Prepare = context => throw new OperationCanceledException();
            _scenes.UnloadError = true;
            var result = await _fsm.RequestTransitionTo<NextState>().Completion;
            Assert.That(result.Status, Is.EqualTo(EmberTransitionStatus.CleanupFailed));
            Assert.That(_scenes.Loaded.Contains("next"), Is.True);
        }

        [Test]
        public async Task OverlayPauseAndResume_PreserveBackingScene()
        {
            Assert.That((await _fsm.RequestPush<OverlayState>().Completion).Status, Is.EqualTo(EmberTransitionStatus.Completed));
            Assert.That(_fsm.CurrentScenePath, Is.EqualTo("old"));
            Assert.That((await _fsm.RequestPop().Completion).Status, Is.EqualTo(EmberTransitionStatus.Completed));
            CollectionAssert.AreEqual(new[] { "prepare:overlay", "pause:old", "enter:overlay", "exit:overlay", "resume:old" }, _trace);
            Assert.That(_scenes.Loaded.Contains("old"), Is.True);
        }

        [Test]
        public async Task ExitFromOverlay_ReleasesUnderlyingState()
        {
            await _fsm.RequestPush<OverlayState>().Completion;
            _trace.Clear();
            var result = await _fsm.RequestTransitionTo<NextState>().Completion;
            Assert.That(result.Status, Is.EqualTo(EmberTransitionStatus.Completed));
            CollectionAssert.AreEqual(new[] { "load:next", "prepare:next", "exit:overlay", "exit:old", "enter:next", "unload:old" }, _trace);
        }

        [Test]
        public async Task SynchronousLegacyEntry_UsesExistingPreparationOverride()
        {
            _fsm.OnSceneTransitionAsync = null;
            _fsm.TransitionTo<NextState>();
            Assert.That(_fsm.Current, Is.SameAs(_next));
            await Task.CompletedTask;
            CollectionAssert.AreEqual(new[] { "prepare:next", "exit:old", "enter:next" }, _trace);
        }

        [Test]
        public async Task SceneOverlayReplacement_UnloadsBothRetiredScenes()
        {
            _overlay.Scene = "overlay";
            await _fsm.RequestPush<OverlayState>().Completion;
            Assert.That(_scenes.Loaded.Contains("old"), Is.True);
            Assert.That(_scenes.Loaded.Contains("overlay"), Is.True);
            await _fsm.RequestTransitionTo<NextState>().Completion;
            CollectionAssert.AreEquivalent(new[] { "next" }, _scenes.Loaded);
        }

        [Test]
        public async Task AbortFailure_StillUnloadsOwnedCandidate_AndReportsCleanupFailure()
        {
            _next.Prepare = context => throw new InvalidOperationException("prepare");
            _next.AbortError = true;
            var result = await _fsm.RequestTransitionTo<NextState>().Completion;
            Assert.That(result.Status, Is.EqualTo(EmberTransitionStatus.CleanupFailed));
            Assert.That(_scenes.Loaded.Contains("next"), Is.False);
            Assert.That(_fsm.Current, Is.SameAs(_old));
        }

        [Test]
        public async Task EntryBoundaries_SlowPreparationAndCurtainRemainSeparate()
        {
            var ready = new UniTaskCompletionSource();
            EmberTransitionPresentation curtain = null;
            SceneTransitionContext captured = null;
            _next.Prepare = context => { captured = context; curtain = context.HoldPresentation(); return ready.Task; };
            _next.Enter = args =>
            {
                Assert.That(captured.Operation.IsPrepared, Is.True);
                Assert.That(captured.Operation.WhenRunning.IsCompleted, Is.False);
            };
            var operation = _fsm.RequestTransitionTo<NextState>();
            Assert.That(_scenes.Loaded.Contains("next"), Is.True);
            Assert.That(operation.WhenPrepared.IsCompleted, Is.False);
            Assert.That(operation.WhenRunning.IsCompleted, Is.False);
            Assert.That(operation.WhenInteractive.IsCompleted, Is.False);
            ready.TrySetResult();
            Assert.That((await operation.Completion).Status, Is.EqualTo(EmberTransitionStatus.Completed));
            Assert.That((await operation.WhenPrepared).RequestId, Is.EqualTo(operation.RequestId));
            Assert.That((await operation.WhenRunning).Status, Is.EqualTo(EmberTransitionStatus.Completed));
            Assert.That(operation.IsRunning, Is.True);
            Assert.That(operation.WhenInteractive.IsCompleted, Is.False);
            curtain.Complete(); curtain.Dispose();
            Assert.That((await operation.WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
            Assert.That(operation.CanInteract, Is.True);
        }

        [Test]
        public async Task EntryBoundaries_PreparationFailureSettlesAllWaiters()
        {
            _next.Prepare = context => throw new InvalidOperationException("resource failed");
            var operation = _fsm.RequestTransitionTo<NextState>();
            await operation.Completion;
            Assert.That((await operation.WhenPrepared).Status, Is.EqualTo(EmberTransitionStatus.PreparationFailed));
            Assert.That((await operation.WhenRunning).Status, Is.EqualTo(EmberTransitionStatus.PreparationFailed));
            Assert.That((await operation.WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.PreparationFailed));
            Assert.That(operation.IsPrepared || operation.IsRunning || operation.CanInteract, Is.False);
        }

        [Test]
        public async Task EntryBoundaries_CancelledLatePreparationNeverReportsReady()
        {
            var ready = new UniTaskCompletionSource();
            _next.Prepare = context => ready.Task;
            var operation = _fsm.RequestTransitionTo<NextState>();
            operation.Cancel(); ready.TrySetResult();
            await operation.Completion;
            Assert.That((await operation.WhenPrepared).Status, Is.EqualTo(EmberTransitionStatus.Cancelled));
            Assert.That((await operation.WhenRunning).Status, Is.EqualTo(EmberTransitionStatus.Cancelled));
            Assert.That((await operation.WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Cancelled));
        }

        [Test]
        public async Task EntryBoundaries_StartFailureDoesNotReportRunningOrInteractive()
        {
            _next.Enter = args => throw new InvalidOperationException("start failed");
            var operation = _fsm.RequestTransitionTo<NextState>();
            await operation.Completion;
            Assert.That((await operation.WhenPrepared).Status, Is.EqualTo(EmberTransitionStatus.Completed));
            Assert.That((await operation.WhenRunning).Status, Is.EqualTo(EmberTransitionStatus.CommitFailed));
            Assert.That((await operation.WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.CommitFailed));
            Assert.That(operation.CanInteract, Is.False);
        }

        [Test]
        public async Task EntryBoundaries_CleanupFailureDoesNotReportInteractive()
        {
            _scenes.UnloadError = true;
            var operation = _fsm.RequestTransitionTo<NextState>();
            await operation.Completion;
            Assert.That((await operation.WhenRunning).Status, Is.EqualTo(EmberTransitionStatus.Completed));
            Assert.That((await operation.WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.CleanupFailed));
            Assert.That(operation.CanInteract, Is.False);
        }

        [Test]
        public async Task EntryBoundaries_ExternalCurtainFailurePreservesTransactionResult()
        {
            EmberTransitionPresentation curtain = null;
            _next.Prepare = context => { curtain = context.HoldPresentation(); return UniTask.CompletedTask; };
            var operation = _fsm.RequestTransitionTo<NextState>();
            Assert.That((await operation.Completion).Status, Is.EqualTo(EmberTransitionStatus.Completed));
            curtain.Fail("outer operation failed"); curtain.Complete();
            Assert.That((await operation.WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.CleanupFailed));
            Assert.That(operation.Completion.Result.Status, Is.EqualTo(EmberTransitionStatus.Completed));
            Assert.That(operation.CanInteract, Is.False);
        }

        [Test]
        public async Task EntryBoundaries_AbandonedCurtainCannotUnlock()
        {
            EmberTransitionPresentation curtain = null;
            _next.Prepare = context => { curtain = context.HoldPresentation(); return UniTask.CompletedTask; };
            var operation = _fsm.RequestTransitionTo<NextState>();
            await operation.Completion;
            curtain.Dispose(); curtain.Complete();
            Assert.That((await operation.WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.CleanupFailed));
        }

        [Test]
        public async Task EntryBoundaries_NewCommitInvalidatesOldUnfinishedCurtain()
        {
            EmberTransitionPresentation oldCurtain = null;
            _next.Prepare = context => { oldCurtain = context.HoldPresentation(); return UniTask.CompletedTask; };
            var first = _fsm.RequestTransitionTo<NextState>();
            await first.Completion;
            var second = _fsm.RequestPush<OverlayState>();
            await second.Completion;
            oldCurtain.Complete();
            Assert.That((await first.WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Cancelled));
            Assert.That(first.CanInteract, Is.False);
            Assert.That(second.CanInteract, Is.True);
        }

        [Test]
        public async Task EntryBoundaries_ShutdownInvalidatesPendingAndCompletedEntries()
        {
            var operation = _fsm.RequestTransitionTo<NextState>();
            await operation.Completion;
            Assert.That(operation.CanInteract, Is.True);
            _fsm.StopTransitions();
            Assert.That(operation.CanInteract, Is.False);
            // Historical milestones stay immutable; current validity is a separate query.
            Assert.That((await operation.WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
        }

        [Test]
        public async Task EntryBoundaries_ShutdownSettlesPendingCurtain()
        {
            EmberTransitionPresentation curtain = null;
            _next.Prepare = context => { curtain = context.HoldPresentation(); return UniTask.CompletedTask; };
            var operation = _fsm.RequestTransitionTo<NextState>();
            await operation.Completion;
            _fsm.StopTransitions(); curtain.Complete();
            Assert.That((await operation.WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Cancelled));
            Assert.That(operation.CanInteract, Is.False);
        }

        [Test]
        public async Task EntryBoundaries_QuickAndPopStillPublishAllBoundaries()
        {
            _scenes.Loaded.Add("next");
            var quick = _fsm.RequestTransitionTo<NextState>(skipSceneLoad: true);
            await quick.Completion;
            Assert.That(quick.IsPrepared && quick.IsRunning && quick.CanInteract, Is.True);
            await _fsm.RequestPush<OverlayState>().Completion;
            var pop = _fsm.RequestPop();
            await pop.Completion;
            Assert.That(pop.IsPrepared && pop.IsRunning && pop.CanInteract, Is.True);
        }

        [Test]
        public async Task EntryBoundaries_RejectDuplicateOrLatePresentationOwner()
        {
            SceneTransitionContext captured = null;
            EmberTransitionPresentation curtain = null;
            _next.Prepare = context =>
            {
                captured = context; curtain = context.HoldPresentation();
                Assert.Throws<InvalidOperationException>(() => context.HoldPresentation());
                return UniTask.CompletedTask;
            };
            var operation = _fsm.RequestTransitionTo<NextState>();
            await operation.Completion;
            Assert.Throws<InvalidOperationException>(() => captured.HoldPresentation());
            curtain.Complete();
        }

        [Test]
        public async Task EntryBoundaries_BusyRequestSettlesWithoutAffectingAcceptedRequest()
        {
            var ready = new UniTaskCompletionSource();
            _next.Prepare = context => ready.Task;
            var first = _fsm.RequestTransitionTo<NextState>();
            var busy = _fsm.RequestTransitionTo<NextState>();
            Assert.That((await busy.WhenPrepared).Status, Is.EqualTo(EmberTransitionStatus.Busy));
            Assert.That((await busy.WhenRunning).Status, Is.EqualTo(EmberTransitionStatus.Busy));
            Assert.That((await busy.WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Busy));
            ready.TrySetResult();
            await first.Completion;
            Assert.That(first.CanInteract, Is.True);
        }

        private abstract class TestState : EmberGameState
        {
            protected readonly List<string> Trace;
            protected abstract string Key { get; }
            public Func<SceneTransitionContext, UniTask> Prepare;
            public Action<object> Enter;
            public Action Exit;
            public object Args;
            public bool AbortError;
            protected TestState(List<string> trace) { Trace = trace; }
            public override TransitionDescriptor[] GetEdges() => new[]
            { new TransitionDescriptor(typeof(NextState), "next"), new TransitionDescriptor(typeof(OverlayState), "overlay") };
            public override async UniTask PrepareEnterAsync(SceneTransitionContext request)
            { Trace.Add("prepare:" + Key); if (Prepare != null) await Prepare(request); }
            public override UniTask AbortPrepareAsync(SceneTransitionContext request)
            { Trace.Add("abort:" + Key); if (AbortError) throw new InvalidOperationException("abort"); return UniTask.CompletedTask; }
            public override void OnEnter(object args) { Args = args; Trace.Add("enter:" + Key); Enter?.Invoke(args); }
            public override void OnExit() { Trace.Add("exit:" + Key); Exit?.Invoke(); }
            public override void OnPause() => Trace.Add("pause:" + Key);
            public override void OnResume() => Trace.Add("resume:" + Key);
        }
        private sealed class OldState : TestState
        { public OldState(List<string> trace) : base(trace) { } protected override string Key => "old"; public override string ScenePath => "old"; }
        private sealed class NextState : TestState
        { public NextState(List<string> trace) : base(trace) { } protected override string Key => "next"; public override string ScenePath => "next"; }
        private sealed class OverlayState : TestState
        { public string Scene = ""; public OverlayState(List<string> trace) : base(trace) { } protected override string Key => "overlay"; public override string ScenePath => Scene; }

        private sealed class TestScenes : IEmberTransitionScenes
        {
            public readonly HashSet<string> Loaded = new();
            private readonly List<string> _trace;
            public UniTaskCompletionSource LoadGate, UnloadGate;
            public string LoadError;
            public bool UnloadError;
            public TestScenes(List<string> trace) { _trace = trace; }
            public bool IsSceneLoaded(string scene) => Loaded.Contains(scene);
            public void SetCurrentScene(string scene) { Assert.That(Loaded.Contains(scene), Is.True); }
            public async UniTask<EmberTransitionSceneLoad> LoadForTransitionAsync(string scene, CancellationToken cancellation)
            {
                if (Loaded.Contains(scene)) return new EmberTransitionSceneLoad(false);
                _trace.Add("load:" + scene);
                if (LoadGate != null) await LoadGate.Task;
                if (LoadError != null) return new EmberTransitionSceneLoad(false, LoadError);
                Loaded.Add(scene);
                return new EmberTransitionSceneLoad(true);
            }
            public async UniTask UnloadForTransitionAsync(string scene)
            {
                _trace.Add("unload:" + scene);
                if (UnloadGate != null) await UnloadGate.Task;
                if (UnloadError) throw new InvalidOperationException("unload failed");
                Loaded.Remove(scene);
            }
        }
    }
}
