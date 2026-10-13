using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Ember.Core.Fsm;
using Game.SceneFlow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests
    {
        [UnityTest] public IEnumerator ModuleReplacementTransfersProgressAndPreservesNestedPause()
            => ModuleReplacement().ToCoroutine();
        private async UniTask ModuleReplacement()
        {
            InstallSessionPowers(); InstallEffects();
            var (module, previous) = await CandidateHost(); var controller = module.Controller;
            var old = controller.Context; long version = module.SessionVersion;
            old.Skills.TryGet("test.skill", out var skill); skill.TryCommit(skill.CreateAttempt()); controller.Tick(.4f);
            var snapshot = PlatformerSnapshot.Capture(_config.Scene, old);
            var outer = module.Pause(); var inner = module.Pause();
            var candidate = await module.PrepareReplacementAsync(previous, PlayerEntryRequest.FromSnapshot(snapshot, 1), default);
            Assert.That(module.PowerProgress, Is.SameAs(old.PowerProgress));
            module.StartSession(candidate);
            Assert.That(module.Controller, Is.SameAs(controller)); Assert.That(module.SourceSlot, Is.EqualTo(1));
            Assert.That(module.PowerProgress, Is.SameAs(controller.Context.PowerProgress));
            Assert.That(module.PowerProgress, Is.Not.SameAs(old.PowerProgress));
            Assert.That(module.SessionVersion, Is.EqualTo(version + 1)); Assert.That(module.IsActive, Is.True);
            Assert.That(module.State, Is.EqualTo(PlayerSessionState.Paused)); Assert.That(Time.timeScale, Is.Zero);
            module.StopSession(previous); previous.Dispose(); candidate.Dispose();
            Assert.That(module.HasSession, Is.True); Assert.That(candidate.Runtime.IsDisposed, Is.False);
            inner.Dispose(); Assert.That(module.State, Is.EqualTo(PlayerSessionState.Paused));
            Assert.That(_input.Service.GetChannel("gameplay").IsAvailable, Is.False);
            outer.Dispose(); Assert.That(module.State, Is.EqualTo(PlayerSessionState.Running));
            Assert.That(Time.timeScale, Is.EqualTo(1)); Assert.That(_input.Service.GetChannel("gameplay").IsAvailable, Is.True);
            controller.Tick(.1f); Assert.That(SessionSkill(controller).RemainingCooldown, Is.EqualTo(9.5f).Within(.0001));
            module.StopSession(); Assert.That(candidate.Runtime.IsDisposed, Is.True);
            Assert.That(_input.Service.GetChannel("gameplay").IsAvailable, Is.False);
        }

        [UnityTest] public IEnumerator ModuleReplacementCancellationKeepsOldSessionAndAllowsRetry()
            => ModuleReplacementCancel().ToCoroutine();
        private async UniTask ModuleReplacementCancel()
        {
            var (module, previous) = await CandidateHost(); var old = module.Controller.Context; long version = module.SessionVersion;
            var entry = PlayerEntryRequest.FromSnapshot(PlatformerSnapshot.Capture(_config.Scene, old));
            using var cancel = new CancellationTokenSource();
            using (module.Pause())
            {
                var candidate = await module.PrepareReplacementAsync(previous, entry, cancel.Token); cancel.Cancel();
                Assert.Throws<OperationCanceledException>(() => module.StartSession(candidate));
                Assert.That(candidate.Runtime.IsDisposed, Is.True); Assert.That(module.Controller.Context, Is.SameAs(old));
                Assert.That(module.SessionVersion, Is.EqualTo(version));
                var retry = await module.PrepareReplacementAsync(previous, entry, default);
                module.StartSession(retry); Assert.That(module.Controller.Context, Is.Not.SameAs(old));
            }
            Assert.That(module.State, Is.EqualTo(PlayerSessionState.Running));
        }

        [UnityTest] public IEnumerator ModuleReplacementCannotCommitWithoutPauseOrAfterReset()
            => ModuleReplacementReset().ToCoroutine();
        private async UniTask ModuleReplacementReset()
        {
            var (module, previous) = await CandidateHost(); var old = module.Controller.Context;
            var entry = PlayerEntryRequest.FromSnapshot(PlatformerSnapshot.Capture(_config.Scene, old));
            var candidate = await module.PrepareReplacementAsync(previous, entry, default);
            Assert.Throws<InvalidOperationException>(() => module.StartSession(candidate));
            Assert.That(module.Controller.Context, Is.SameAs(old));
            ((IEmberModule)module).ResetModuleData();
            Assert.That(candidate.Runtime.IsDisposed, Is.True); Assert.That(module.HasSession, Is.False);
            ((IEmberModule)module).OnInit();
            Assert.Throws<InvalidOperationException>(() => module.StartSession(candidate)); candidate.Dispose();
            Assert.That(module.State, Is.EqualTo(PlayerSessionState.Ready));
        }

        [UnityTest] public IEnumerator ModuleReplacementRejectsOldSessionAfterAnotherHandoff()
            => ModuleReplacementStale().ToCoroutine();
        private async UniTask ModuleReplacementStale()
        {
            var (module, previous) = await CandidateHost();
            var entry = PlayerEntryRequest.FromSnapshot(PlatformerSnapshot.Capture(_config.Scene, module.Controller.Context));
            using (module.Pause())
            {
                var candidate = await module.PrepareReplacementAsync(previous, entry, default); module.StartSession(candidate);
                try { await module.PrepareReplacementAsync(previous, entry, default); Assert.Fail("旧会话不能继续替换"); }
                catch (InvalidOperationException) { }
                Assert.Throws<InvalidOperationException>(() => module.StartSession(candidate));
                Assert.That(candidate.Runtime.IsDisposed, Is.False);
            }
            Assert.That(module.Controller.IsRunning, Is.True);
        }

        [UnityTest] public IEnumerator PlayerScopeReplacementPreservesWorldAndReleasesLatestSession()
            => ScopeReplacementWorld().ToCoroutine();
        private async UniTask ScopeReplacementWorld()
        {
            InstallSessionPowers(); var module = Module(); var controller = Character(); var scope = Scope(module, controller);
            var world = scope.gameObject.AddComponent<PlayerReplacementWorldProbe>();
            var participants = typeof(GameSceneScope).GetField("_participants", BindingFlags.Instance | BindingFlags.NonPublic);
            var oldList = (GameSceneParticipant[])participants.GetValue(scope);
            participants.SetValue(scope, oldList.Concat(new GameSceneParticipant[] { world }).ToArray());
            var session = await scope.PrepareAsync(201, PlayerEntryRequest.NewGame(), default);
            ((IEmberModule)module).OnInit(); session.Start(); var old = controller.Context;
            using (module.Pause())
            {
                var entry = PlayerEntryRequest.FromSnapshot(PlatformerSnapshot.Capture(_config.Scene, old));
                var replacement = await session.PrepareReplacementAsync(oldList[0], 202, entry, default);
                Assert.That(controller.Context, Is.SameAs(old)); Assert.That(world.Prepared, Is.EqualTo(1));
                replacement.Commit(); replacement.Dispose();
                Assert.That(controller.Context, Is.Not.SameAs(old)); Assert.That(module.HasSession, Is.True);
                Assert.That(controller.GetComponent<PlayerSceneParticipant>().PreparedRequestId, Is.EqualTo(202));
                Assert.That(session.IsRunning, Is.True); Assert.That(world.Started, Is.EqualTo(1)); Assert.That(world.Disposed, Is.Zero);
            }
            Assert.That(controller.IsRunning, Is.True); session.Dispose();
            Assert.That(module.HasSession, Is.False); Assert.That(controller.IsPrepared, Is.False);
            Assert.That(world.Disposed, Is.EqualTo(1));
            Assert.That(controller.GetComponent<PlayerSceneParticipant>().PreparedRequestId, Is.Null);
        }

        [UnityTest] public IEnumerator PlayerScopeReplacementDiscardKeepsCurrentResult()
            => ScopeReplacementDiscard().ToCoroutine();
        private async UniTask ScopeReplacementDiscard()
        {
            var module = Module(); var controller = Character(); var scope = Scope(module, controller);
            var participant = scope.GetComponent<PlayerScopeDependencyProbe>();
            var session = await scope.PrepareAsync(211, PlayerEntryRequest.NewGame(), default);
            ((IEmberModule)module).OnInit(); session.Start(); var old = controller.Context;
            using (module.Pause())
            {
                var entry = PlayerEntryRequest.FromSnapshot(PlatformerSnapshot.Capture(_config.Scene, old));
                var candidate = await session.PrepareReplacementAsync(participant, 212, entry, default); candidate.Dispose();
                Assert.That(controller.Context, Is.SameAs(old)); Assert.That(module.HasSession, Is.True);
                Assert.That(participant.Player.PreparedRequestId, Is.EqualTo(211));
                var retry = await session.PrepareReplacementAsync(participant, 213, entry, default); retry.Commit();
                Assert.That(participant.Player.PreparedRequestId, Is.EqualTo(213));
            }
            session.Dispose();
        }

        [UnityTest] public IEnumerator PlayerScopeReplacementStartFailureKeepsCurrentPlayableSession()
            => ScopeReplacementFailure().ToCoroutine();
        private async UniTask ScopeReplacementFailure()
        {
            // 图属于本会话冻结装配；只切换测试探针的外部故障注入，不通过运行后换图触发恢复。
            var definition = Keep(ScriptableObject.CreateInstance<CandidateProbeDefinition>());
            var graph = Keep(ScriptableObject.CreateInstance<EmberFsmGraph>());
            graph.Configure("probe", new EmberFsmNode("probe", "probe", definition)); _config.Configure(graph, _config.Spawn);
            var module = Module(); var controller = Character(); var scope = Scope(module, controller);
            var participant = scope.GetComponent<PlayerScopeDependencyProbe>();
            var session = await scope.PrepareAsync(221, PlayerEntryRequest.NewGame(), default);
            ((IEmberModule)module).OnInit(); session.Start(); var old = controller.Context; long version = module.SessionVersion;
            definition.Failure = 1;
            using (module.Pause())
            {
                var entry = PlayerEntryRequest.FromSnapshot(PlatformerSnapshot.Capture(_config.Scene, old));
                var candidate = await session.PrepareReplacementAsync(participant, 222, entry, default);
                Assert.Throws<InvalidOperationException>(() => candidate.Commit());
                Assert.That(controller.Context, Is.SameAs(old)); Assert.That(module.SessionVersion, Is.EqualTo(version));
                Assert.That(participant.Player.PreparedRequestId, Is.EqualTo(221)); Assert.That(session.IsRunning, Is.True);
            }
            Assert.That(controller.IsRunning, Is.True); Assert.That(Time.timeScale, Is.EqualTo(1)); session.Dispose();
        }
    }

    public sealed class PlayerReplacementWorldProbe : GameSceneParticipant
    {
        [NonSerialized] public int Prepared, Started, Disposed;
        public override UniTask<IGameScenePreparedParticipant> PrepareAsync(GameScenePreparationContext context)
        { Prepared++; return UniTask.FromResult<IGameScenePreparedParticipant>(new Result(this)); }
        private sealed class Result : IGameScenePreparedParticipant
        {
            private readonly PlayerReplacementWorldProbe _owner;
            internal Result(PlayerReplacementWorldProbe owner) => _owner = owner;
            public void Start() => _owner.Started++;
            public void Dispose() => _owner.Disposed++;
        }
    }
}
