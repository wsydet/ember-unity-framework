using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Ember.Core;
using NUnit.Framework;

namespace Ember.Scene.Tests
{
    public sealed class EmberInPlaceTransitionTests
    {
        private EmberStateMachine _fsm;
        private Host _host;
        private Scenes _scenes;
        private EmberSceneTransitionRunner _runner;

        [SetUp] public void Setup()
        {
            _fsm = new EmberStateMachine(); _host = new Host(); _scenes = new Scenes();
            _fsm.Register(_host); _fsm.Register(new Other()); _fsm.Start<Host>();
            _runner = new EmberSceneTransitionRunner(_scenes); _fsm.OnSceneTransitionAsync = _runner.RunAsync;
        }

        [Test] public async Task SuccessPreservesStateSceneStackAndLifecycle()
        {
            _host.Prepare = request => { request.PreparedArgs = "candidate"; return UniTask.CompletedTask; };
            var operation = _fsm.RequestInPlace<Host>("source");
            Assert.That((await operation.Completion).Status, Is.EqualTo(EmberTransitionStatus.Completed));
            Assert.That(operation.CanInteract, Is.True); Assert.That(_host.Committed, Is.EqualTo("candidate"));
            Assert.That(_host.Enters, Is.EqualTo(1)); Assert.That(_host.Exits + _host.Pauses + _host.Resumes, Is.Zero);
            Assert.That(_host.NormalPrepares, Is.Zero); Assert.That(_fsm.Current, Is.SameAs(_host));
            Assert.That(_fsm.Previous, Is.Null); Assert.That(_fsm.CurrentScenePath, Is.EqualTo("world"));
            Assert.That((await _fsm.RequestPop().Completion).Status, Is.EqualTo(EmberTransitionStatus.Rejected));
            Assert.That(_scenes.Mutations, Is.Zero);
        }

        [Test] public async Task PreparationFailureCleansCandidateAndRetainsRunningState()
        {
            _host.Prepare = request => throw new InvalidOperationException("bad snapshot");
            var result = await _fsm.RequestInPlace<Host>().Completion;
            Assert.That(result.Status, Is.EqualTo(EmberTransitionStatus.PreparationFailed));
            Assert.That(result.CommitStarted, Is.False); Assert.That(_host.Aborts, Is.EqualTo(1));
            Assert.That(_host.Commits, Is.Zero); Assert.That(_host.Exits, Is.Zero);
            Assert.That(_fsm.Current, Is.SameAs(_host)); Assert.That(_scenes.Mutations, Is.Zero);
        }

        [Test] public async Task CancellationWaitsForCandidateAndRejectsLateProceed()
        {
            var ready = new UniTaskCompletionSource(); SceneTransitionContext captured = null;
            _host.Prepare = request => { captured = request; return ready.Task; };
            var operation = _fsm.RequestInPlace<Host>(); operation.Cancel();
            Assert.That(operation.Completion.IsCompleted, Is.False);
            Assert.That((await _fsm.RequestInPlace<Host>().Completion).Status, Is.EqualTo(EmberTransitionStatus.Busy));
            ready.TrySetResult();
            Assert.That((await operation.Completion).Status, Is.EqualTo(EmberTransitionStatus.Cancelled));
            captured.Proceed(); Assert.That(_host.Commits, Is.Zero); Assert.That(_host.Aborts, Is.EqualTo(1));
            _host.Prepare = null;
            Assert.That((await _fsm.RequestInPlace<Host>().Completion).Status, Is.EqualTo(EmberTransitionStatus.Completed));
            captured.Proceed(); Assert.That(_host.Commits, Is.EqualTo(1));
        }

        [Test] public async Task PreparationAndRunningPrecedeRealPresentationRelease()
        {
            var ready = new UniTaskCompletionSource(); EmberTransitionPresentation presentation = null;
            _host.Prepare = request => ready.Task;
            _fsm.OnSceneTransitionAsync = request => { presentation = request.HoldPresentation(); return _runner.RunAsync(request); };
            var operation = _fsm.RequestInPlace<Host>();
            Assert.That(operation.IsPrepared, Is.False); Assert.That(operation.IsRunning, Is.False);
            ready.TrySetResult(); await operation.Completion;
            Assert.That(operation.IsPrepared, Is.True); Assert.That(operation.IsRunning, Is.True);
            Assert.That(operation.WhenInteractive.IsCompleted, Is.False);
            presentation.Complete(); Assert.That((await operation.WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
            Assert.That(operation.CanInteract, Is.True);
        }

        [Test] public async Task SubsequentCommitInvalidatesPriorEntryButFailedPreparationDoesNot()
        {
            var first = _fsm.RequestInPlace<Host>(); await first.Completion;
            _host.Prepare = request => throw new InvalidOperationException("reject");
            await _fsm.RequestInPlace<Host>().Completion; Assert.That(first.CanInteract, Is.True);
            _host.Prepare = null; var second = _fsm.RequestInPlace<Host>(); await second.Completion;
            Assert.That(first.CanInteract, Is.False); Assert.That(second.CanInteract, Is.True);
        }

        [Test] public async Task MissingSceneNeverLoadsOrPrepares()
        {
            _scenes.Loaded = false;
            var operation = _fsm.RequestInPlace<Host>();
            Assert.That((await operation.Completion).Status, Is.EqualTo(EmberTransitionStatus.PreparationFailed));
            Assert.That(_host.Prepares + _host.Commits + _host.Aborts + _scenes.Mutations, Is.Zero);
        }

        [Test] public async Task ExplicitProtocolSelfEdgeAndCurrentStateAreRequired()
        {
            Assert.That((await _fsm.RequestInPlace<Other>().Completion).Status, Is.EqualTo(EmberTransitionStatus.Rejected));
            _host.DeclareEdge = false; _fsm = new EmberStateMachine(); _fsm.Register(_host); _fsm.Start<Host>();
            Assert.That((await _fsm.RequestInPlace<Host>().Completion).Status, Is.EqualTo(EmberTransitionStatus.Rejected));
            _fsm.Start<Host>();
            Assert.That(_host.Commits, Is.Zero);
        }

        [Test] public async Task GuardRunsOnceAndCannotStartNestedTransaction()
        {
            int calls = 0; EmberTransitionOperation nested = null;
            _host.Guard = () => { calls++; nested = _fsm.RequestInPlace<Host>(); return true; };
            await _fsm.RequestInPlace<Host>().Completion;
            Assert.That(calls, Is.EqualTo(1)); Assert.That((await nested.Completion).Status, Is.EqualTo(EmberTransitionStatus.Busy));
            _host.Guard = () => throw new InvalidOperationException("guard failure");
            Assert.That((await _fsm.RequestInPlace<Host>().Completion).Status, Is.EqualTo(EmberTransitionStatus.Rejected));
            Assert.That(_host.Commits, Is.EqualTo(1));
        }

        [Test] public async Task StopDuringPrepareInvalidatesCandidateAndClosesRequests()
        {
            var ready = new UniTaskCompletionSource(); _host.Prepare = request => ready.Task;
            var operation = _fsm.RequestInPlace<Host>(); _fsm.StopTransitions(); ready.TrySetResult();
            Assert.That((await operation.Completion).Status, Is.EqualTo(EmberTransitionStatus.Cancelled));
            Assert.That(_host.Aborts, Is.EqualTo(1)); Assert.That(_host.Commits, Is.Zero);
            Assert.That((await _fsm.RequestInPlace<Host>().Completion).Status, Is.EqualTo(EmberTransitionStatus.Stopped));
        }

        [Test] public async Task CommitFailureIsExplicitAndDoesNotClaimRollback()
        {
            _host.FailCommit = true; var operation = _fsm.RequestInPlace<Host>();
            var result = await operation.Completion;
            Assert.That(result.Status, Is.EqualTo(EmberTransitionStatus.CommitFailed));
            Assert.That(result.CommitStarted, Is.True); Assert.That(_host.Aborts, Is.Zero);
            Assert.That(operation.CanInteract, Is.False); Assert.That(_scenes.Mutations, Is.Zero);
        }

        private sealed class Host : EmberGameState, IEmberInPlaceState
        {
            public int Enters, Exits, Pauses, Resumes, Prepares, NormalPrepares, Commits, Aborts;
            public object Committed;
            public bool DeclareEdge = true, FailCommit;
            public Func<bool> Guard;
            public Func<SceneTransitionContext, UniTask> Prepare;
            public override string ScenePath => "world";
            public override TransitionDescriptor[] GetEdges() => DeclareEdge
                ? new[] { new TransitionDescriptor(typeof(Host), "replace", "允许原位交接", () => Guard?.Invoke() ?? true) }
                : Array.Empty<TransitionDescriptor>();
            public override void OnEnter(object args) => Enters++;
            public override void OnExit() => Exits++;
            public override void OnPause() => Pauses++;
            public override void OnResume() => Resumes++;
            public override UniTask PrepareEnterAsync(SceneTransitionContext request) { NormalPrepares++; return UniTask.CompletedTask; }
            public UniTask PrepareInPlaceAsync(SceneTransitionContext request) { Prepares++; return Prepare?.Invoke(request) ?? UniTask.CompletedTask; }
            public void CommitInPlace(SceneTransitionContext request)
            { Commits++; if (FailCommit) throw new InvalidOperationException("commit failed"); Committed = request.PreparedArgs; }
            public UniTask AbortInPlaceAsync(SceneTransitionContext request) { Aborts++; return UniTask.CompletedTask; }
        }
        private sealed class Other : EmberGameState { }
        private sealed class Scenes : IEmberTransitionScenes
        {
            public bool Loaded = true;
            public int Mutations;
            public bool IsSceneLoaded(string scene) => Loaded;
            public UniTask<EmberTransitionSceneLoad> LoadForTransitionAsync(string scene, CancellationToken cancellation)
            { Mutations++; throw new InvalidOperationException("In-place must not load."); }
            public UniTask UnloadForTransitionAsync(string scene) { Mutations++; throw new InvalidOperationException("In-place must not unload."); }
            public void SetCurrentScene(string scene) => Mutations++;
        }
    }
}
