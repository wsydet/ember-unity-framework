using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.PlayerData;
using Game.SceneFlow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests
    {
        private async UniTask<(PlayerModule module, RecoveryFlowHost host, EmberStateMachine flow)> RecoveryFlow()
        {
            var module = Module(); var controller = Character(); var scope = Scope(module, controller);
            var session = await scope.PrepareAsync(301, PlayerEntryRequest.NewGame(), default);
            ((IEmberModule)module).OnInit(); session.Start();
            var host = new RecoveryFlowHost(module, session, scope.GetComponent<PlayerScopeDependencyProbe>());
            var flow = new EmberStateMachine(); flow.Register(host); flow.Start<RecoveryFlowHost>();
            return (module, host, flow);
        }

        [UnityTest] public IEnumerator FullPowersAndTimedEffectsRoundTripThroughPublicRecovery()
            => FormalFullRecovery().ToCoroutine();
        private async UniTask FormalFullRecovery()
        {
            InstallSessionPowers(); InstallEffects(); var (module, host, flow) = await RecoveryFlow();
            try
            {
                var controller = module.Controller; var context = controller.Context;
                context.Abilities.TryGet("test.air", out var ability);
                ability.TryCommit(ability.CreateAttempt());
                var skill = SessionSkill(controller); skill.TryCommit(skill.CreateAttempt()); skill.SetOpen(false);
                var definition = SessionEffect(); context.Effects.ApplyIndependent(definition, "first");
                controller.Tick(.2f); context.Effects.ApplyIndependent(definition, "second");
                var save = module.Save(0); var frozen = module.RecoveryTarget;
                controller.Tick(1); ability.RefillUses(); skill.SetOpen(true); context.Effects.RemoveAll(definition.Id);
                Assert.That((await save.Completion).Succeeded, Is.True);
                Assert.That(module.RecoveryTarget, Is.Not.SameAs(frozen));
                Assert.That(Time.timeScale, Is.EqualTo(1));
                Assert.That((await module.LoadCheckpoint(flow, 0).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                controller.Context.Abilities.TryGet("test.air", out var restoredAbility);
                Assert.That(restoredAbility.RemainingUses, Is.EqualTo(1));
                Assert.That(SessionSkill(controller).IsOpen, Is.False);
                Assert.That(SessionSkill(controller).RemainingCooldown, Is.EqualTo(9.8f).Within(.0001));
                var effects = controller.Context.Effects;
                Assert.That(effects.Count, Is.EqualTo(2));
                Assert.That(effects.GetAt(0).RemainingSeconds, Is.EqualTo(9.8f).Within(.0001));
                Assert.That(effects.GetAt(1).RemainingSeconds, Is.EqualTo(10));
                Assert.That(effects.GetAt(0).SourceKey, Is.EqualTo("first"));
                controller.Tick(2); effects.RemoveAll(definition.Id);
                Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(SessionSkill(controller).RemainingCooldown, Is.EqualTo(9.8f).Within(.0001));
                Assert.That(controller.Context.Effects.Count, Is.EqualTo(2));
                Assert.That(host.Enters, Is.EqualTo(1)); Assert.That(host.Exits, Is.Zero);
            }
            finally { host.Session.Dispose(); }
        }

        [UnityTest] public IEnumerator FormalSaveFreezesSnapshotAndUpdatesTargetBeforeCompletion()
            => FormalSaveTarget().ToCoroutine();
        private async UniTask FormalSaveTarget()
        {
            var module = await Running(); var initial = module.RecoveryTarget;
            var body = module.Controller.GetComponent<Rigidbody2D>(); body.position = new Vector2(7, 8); body.linearVelocity = new Vector2(1, 2);
            var save = module.Save(0); body.position = new Vector2(9, 10);
            if (!save.Completion.IsCompleted) Assert.That(module.RecoveryTarget, Is.SameAs(initial));
            Assert.That(Time.timeScale, Is.EqualTo(1)); Assert.That(module.Controller.IsRunning, Is.True);
            Assert.That((await save.Completion).Succeeded, Is.True);
            Assert.That(module.RecoveryTarget.Position, Is.EqualTo(new Vector2(7, 8)));
            Assert.That(module.RecoveryTarget.Velocity, Is.EqualTo(new Vector2(1, 2))); Assert.That(module.RecoverySlot, Is.EqualTo(0));
            var read = await _data.ReadAsync(0, new PlatformerSnapshotCodec()).Completion;
            Assert.That(read.Succeeded, Is.True); Assert.That(read.Progress.Position, Is.EqualTo(module.RecoveryTarget.Position));
            var target = module.RecoveryTarget;
            Assert.That((await module.Save(999).Completion).Status, Is.EqualTo(SaveStatus.InvalidSlot));
            Assert.That((await module.Save(-1).Completion).Status, Is.EqualTo(SaveStatus.InvalidSlot));
            Assert.That(module.RecoveryTarget, Is.SameAs(target));
        }

        [UnityTest] public IEnumerator OldSessionSaveCompletionCannotReplaceNewSessionTarget()
            => FormalLateSave().ToCoroutine();
        private async UniTask FormalLateSave()
        {
            var module = await Running(); module.Controller.GetComponent<Rigidbody2D>().position = new Vector2(7, 8);
            var save = module.Save(0); module.StopSession();
            var candidate = await Prepare(module, Character()); module.StartSession(candidate);
            var initial = module.RecoveryTarget; await save.Completion;
            Assert.That(module.RecoveryTarget, Is.SameAs(initial)); Assert.That(module.RecoverySlot, Is.Null);
            Assert.That(initial.Position, Is.EqualTo(_config.Spawn));
        }

        [UnityTest] public IEnumerator PublicRecoveryUsesFrozenInitialSnapshotAndPreservesGameplay()
            => FormalInitialRecovery().ToCoroutine();
        private async UniTask FormalInitialRecovery()
        {
            InstallSessionPowers(); var (module, host, flow) = await RecoveryFlow();
            try
            {
                var initial = module.RecoveryTarget; var controller = module.Controller;
                controller.Context.Abilities.TryGet("test.air", out var ability); ability.TryCommit(ability.CreateAttempt());
                _config.Configure(_config.Graph, new Vector2(70, 80)); controller.GetComponent<Rigidbody2D>().position = new Vector2(4, 5);
                var operation = module.Recover(flow);
                Assert.That((await operation.WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(module.RecoveryTarget, Is.SameAs(initial)); Assert.That(module.Controller, Is.SameAs(controller));
                Assert.That(controller.Context.Motor.Position, Is.EqualTo(initial.Position));
                controller.Context.Abilities.TryGet("test.air", out var restored); Assert.That(restored.RemainingUses, Is.EqualTo(2));
                Assert.That(host.Enters, Is.EqualTo(1)); Assert.That(host.Exits, Is.Zero); Assert.That(module.IsActive, Is.True);
                Assert.That(_data.LastSuccessfulSaveSlot, Is.Null); Assert.That(Time.timeScale, Is.EqualTo(1));
            }
            finally { host.Session.Dispose(); }
        }

        [UnityTest] public IEnumerator PublicReadAndRecoveryShareChainAndKeepActualLoadedSnapshot()
            => FormalReadRecovery().ToCoroutine();
        private async UniTask FormalReadRecovery()
        {
            var (module, host, flow) = await RecoveryFlow();
            try
            {
                module.Controller.GetComponent<Rigidbody2D>().position = new Vector2(4, 5);
                Assert.That((await module.Save(0).Completion).Succeeded, Is.True);
                Assert.That((await module.LoadCheckpoint(flow, 0).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                var loaded = module.RecoveryTarget;
                Assert.That(loaded.Position, Is.EqualTo(new Vector2(4, 5))); Assert.That(module.RecoverySlot, Is.EqualTo(0));
                // 通过存储服务覆盖槽位不改变本会话已读取的业务目标。
                Assert.That((await _data.SaveAsync(0, EmptySnapshot(), new SaveSummary("external overwrite"),
                    new PlatformerSnapshotCodec()).Completion).Succeeded, Is.True);
                module.Controller.GetComponent<Rigidbody2D>().position = new Vector2(9, 10);
                Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(module.RecoveryTarget, Is.SameAs(loaded));
                Assert.That(module.Controller.Context.Motor.Position, Is.EqualTo(loaded.Position));
                Assert.That(host.Enters, Is.EqualTo(1));
            }
            finally { host.Session.Dispose(); }
        }

        [UnityTest] public IEnumerator PublicV1ReadFailsWithoutReplacingSessionOrTarget()
            => FormalV1Rejected().ToCoroutine();
        private async UniTask FormalV1Rejected()
        {
            var (module, host, flow) = await RecoveryFlow();
            try
            {
                await _data.SaveAsync(0, new PlatformerProgress { Scene = _config.Scene }, new SaveSummary("legacy"),
                    new PlatformerProgressCodec()).Completion;
                var context = module.Controller.Context; var target = module.RecoveryTarget;
                var result = await module.LoadCheckpoint(flow, 0).Completion;
                Assert.That(result.Status, Is.EqualTo(EmberTransitionStatus.PreparationFailed));
                StringAssert.Contains("DataVersionUnsupported", result.Error);
                Assert.That(module.Controller.Context, Is.SameAs(context)); Assert.That(module.RecoveryTarget, Is.SameAs(target));
                Assert.That(module.Controller.IsRunning, Is.True); Assert.That(Time.timeScale, Is.EqualTo(1));
                Assert.That((await _data.ReadAsync(0, new PlatformerProgressCodec()).Completion).Succeeded, Is.True);
            }
            finally { host.Session.Dispose(); }
        }

        [UnityTest] public IEnumerator PublicRecoveryWaitsForPreparationAndPresentationAndCancelsCleanly()
            => FormalRecoveryBoundaries().ToCoroutine();
        private async UniTask FormalRecoveryBoundaries()
        {
            var (module, host, flow) = await RecoveryFlow();
            try
            {
                host.Ready = new UniTaskCompletionSource(); var old = module.Controller.Context;
                var operation = module.Recover(flow);
                Assert.That(operation.IsPrepared, Is.False); Assert.That(Time.timeScale, Is.Zero);
                Assert.Throws<InvalidOperationException>(() => module.Recover(flow));
                operation.Cancel(); host.Ready.TrySetResult();
                Assert.That((await operation.Completion).Status, Is.EqualTo(EmberTransitionStatus.Cancelled));
                Assert.That(module.Controller.Context, Is.SameAs(old)); Assert.That(Time.timeScale, Is.EqualTo(1));
                host.Ready = null; EmberTransitionPresentation presentation = null;
                flow.OnSceneTransitionAsync = async request =>
                { presentation = request.HoldPresentation(); await request.PrepareAsync(); request.Proceed(); };
                operation = module.Recover(flow); await operation.Completion;
                Assert.That(operation.IsPrepared && operation.IsRunning, Is.True);
                Assert.That(operation.WhenInteractive.IsCompleted, Is.False); Assert.That(module.Controller.IsRunning, Is.True);
                presentation.Complete(); Assert.That((await operation.WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
            }
            finally { host.Session.Dispose(); }
        }

        private sealed class RecoveryFlowHost : GameplayState, IEmberInPlaceState
        {
            private readonly PlayerModule _player;
            private readonly GameSceneParticipant _participant;
            internal GameSceneSession Session { get; }
            internal int Enters, Exits;
            internal UniTaskCompletionSource Ready;
            internal RecoveryFlowHost(PlayerModule player, GameSceneSession session, GameSceneParticipant participant)
            { _player = player; Session = session; _participant = participant; }
            public override TransitionDescriptor[] GetEdges() => new[] { new TransitionDescriptor(typeof(GameplayState), "restore") };
            protected override void OnGameplayEnter(object args) => Enters++;
            protected override void OnGameplayExit() => Exits++;
            public async UniTask PrepareInPlaceAsync(SceneTransitionContext request)
            {
                var recovery = new PlayerRecoveryTransition(_player); request.PreparedArgs = recovery;
                if (Ready != null) await Ready.Task;
                await recovery.PrepareAsync(Session, _participant, request.Operation.RequestId,
                    (PlayerEntryRequest)request.Args, request.Cancellation);
            }
            public void CommitInPlace(SceneTransitionContext request) => ((PlayerRecoveryTransition)request.PreparedArgs).Commit();
            public UniTask AbortInPlaceAsync(SceneTransitionContext request)
            { (request.PreparedArgs as PlayerRecoveryTransition)?.Dispose(); return UniTask.CompletedTask; }
        }
    }
}
