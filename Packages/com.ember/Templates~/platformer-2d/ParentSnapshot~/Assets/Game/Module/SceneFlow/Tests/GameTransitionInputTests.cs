using System;
using System.Collections;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Game.SceneFlow.Tests
{
    public sealed class GameTransitionInputTests
    {
        private GameInputConfig _config;
        private InputActionAsset _actions;
        private GameInputService _service;
        private GameTransitionInputGate _gate;
        private EmberStateMachine _fsm;
        private Destination _target;
        private EmberTransitionPresentation _curtain;
        private GameInputChannel Channel => _service.GetChannel("play");

        [SetUp]
        public void Setup()
        {
            _actions = ScriptableObject.CreateInstance<InputActionAsset>();
            _actions.AddActionMap("Play").AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            _config = ScriptableObject.CreateInstance<GameInputConfig>();
            _config.Configure(_actions, new GameInputChannelConfig("play", "Play", true));
            _service = new GameInputService(_config, _actions);
            _fsm = new EmberStateMachine();
            _target = new Destination();
            _fsm.Register(new Origin()); _fsm.Register(_target); _fsm.Start<Origin>();
            _fsm.OnSceneTransitionAsync = async context =>
            {
                _curtain = context.HoldPresentation();
                await context.PrepareAsync();
                context.Proceed();
            };
            _gate = new GameTransitionInputGate(_fsm, _service, "play");
        }

        [TearDown]
        public void Teardown()
        {
            _fsm.StopTransitions(); _curtain?.Dispose(); _service.Dispose(); _gate.Dispose();
            Object.DestroyImmediate(_actions); Object.DestroyImmediate(_config);
        }

        [UnityTest]
        public IEnumerator OuterCompletionWaitsForCurtainAndPreservesOtherOwner()
        {
            var operation = _fsm.RequestTransitionTo<Destination>();
            yield return Wait(operation.Completion);
            Assert.That(operation.IsRunning, Is.True);
            Assert.That(Channel.IsAvailable, Is.False);
            using var other = _service.Request(this, 1, new GameInputRule("play", GameInputPermission.Deny));
            _curtain.Complete();
            yield return Wait(operation.WhenInteractive); yield return null;
            Assert.That(Channel.IsAvailable, Is.False);
            other.Dispose();
            Assert.That(Channel.IsAvailable, Is.True);
        }

        [UnityTest]
        public IEnumerator SlowPreparationCannotOperateOrStart()
        {
            var ready = new UniTaskCompletionSource();
            _target.Prepare = () => ready.Task;
            var operation = _fsm.RequestTransitionTo<Destination>();
            Assert.That(Channel.IsAvailable || operation.IsPrepared || operation.IsRunning, Is.False);
            ready.TrySetResult(); yield return Wait(operation.Completion);
            Assert.That(Channel.IsAvailable, Is.False);
            _curtain.Complete(); yield return Wait(operation.WhenInteractive); yield return null;
            Assert.That(Channel.IsAvailable, Is.True);
        }

        [UnityTest]
        public IEnumerator PreparationFailureRestoresOldSessionOnly()
        {
            _target.Prepare = () => throw new InvalidOperationException("prepare");
            var operation = _fsm.RequestTransitionTo<Destination>();
            yield return Wait(operation.WhenInteractive); yield return null;
            Assert.That(operation.WhenInteractive.Result.Status, Is.EqualTo(EmberTransitionStatus.PreparationFailed));
            Assert.That(Channel.IsAvailable, Is.True);
        }

        [UnityTest]
        public IEnumerator CommitFailureStaysClosedUntilLaterSuccess()
        {
            _target.FailEnter = true;
            var failed = _fsm.RequestTransitionTo<Destination>();
            yield return Wait(failed.WhenInteractive); yield return null;
            Assert.That(Channel.IsAvailable, Is.False);
            _curtain.Dispose(); _target.FailEnter = false;
            var next = _fsm.RequestTransitionTo<Destination>();
            yield return Wait(next.Completion);
            Assert.That(Channel.IsAvailable, Is.False);
            _curtain.Complete(); yield return Wait(next.WhenInteractive); yield return null;
            Assert.That(Channel.IsAvailable, Is.True);
        }

        [UnityTest]
        public IEnumerator LateOldCurtainDoesNotUnlockNewRequest()
        {
            var old = _fsm.RequestTransitionTo<Destination>();
            yield return Wait(old.Completion);
            var oldCurtain = _curtain;
            var next = _fsm.RequestTransitionTo<Destination>();
            yield return Wait(next.Completion);
            oldCurtain.Complete(); yield return null;
            Assert.That(Channel.IsAvailable, Is.False);
            _curtain.Complete(); yield return Wait(next.WhenInteractive); yield return null;
            Assert.That(Channel.IsAvailable, Is.True);
        }

        [UnityTest]
        public IEnumerator OuterFailureDoesNotUnlockAndStoppedServiceStaysStopped()
        {
            var operation = _fsm.RequestTransitionTo<Destination>();
            yield return Wait(operation.Completion);
            _curtain.Fail("outer failure"); yield return Wait(operation.WhenInteractive); yield return null;
            Assert.That(Channel.IsAvailable, Is.False);
            var channel = Channel;
            _service.Dispose(); _gate.Dispose();
            Assert.That(channel.IsStopped, Is.True);
            Assert.That(channel.IsAvailable, Is.False);
        }

        private IEnumerator Wait(Task task)
        {
            for (int i = 0; i < 120 && !task.IsCompleted; i++) yield return null;
            Assert.That(task.IsCompleted, Is.True);
        }

        private sealed class Origin : EmberGameState
        {
            public override string ScenePath => "origin";
            public override TransitionDescriptor[] GetEdges() => new[] { new TransitionDescriptor(typeof(Destination), "enter") };
        }
        private sealed class Destination : EmberGameState
        {
            public Func<UniTask> Prepare = () => UniTask.CompletedTask;
            public bool FailEnter;
            public override string ScenePath => "destination";
            public override bool AllowReEnter => true;
            public override TransitionDescriptor[] GetEdges() => new[] { new TransitionDescriptor(typeof(Destination), "again") };
            public override UniTask PrepareEnterAsync(SceneTransitionContext context) => Prepare();
            public override void OnEnter(object args) { if (FailEnter) throw new InvalidOperationException("enter"); }
        }
    }
}
