using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.Input;
using Game.Module;
using Game.SceneFlow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests
    {
        private GameSceneScope Scope(PlayerModule module, PlayerController controller, bool failStart = false)
        {
            var root = Keep(new GameObject("PlayerScope")); SceneManager.MoveGameObjectToScene(root, _scene);
            var scope = root.AddComponent<GameSceneScope>();
            var proxy = root.AddComponent<PlayerScopeDependencyProbe>();
            proxy.Player = controller.GetComponent<PlayerSceneParticipant>() ?? controller.gameObject.AddComponent<PlayerSceneParticipant>();
            proxy.Module = module; proxy.Input = _input; proxy.Data = _data;
            var participants = new List<GameSceneParticipant> { proxy };
            if (failStart) participants.Add(root.AddComponent<PlayerScopeFailStartProbe>());
            typeof(GameSceneScope).GetField("_participants", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(scope, participants.ToArray());
            return scope;
        }

        [UnityTest] public IEnumerator ScopePlayer_PreparesBeforeModuleActivation_AndSurvivesReset() => ScopeLifecycle().ToCoroutine();
        private async UniTask ScopeLifecycle()
        {
            var module = Module(); var controller = Character(); var scope = Scope(module, controller);
            var session = await scope.PrepareAsync(11, PlayerEntryRequest.NewGame(), default);
            Assert.That(controller.IsPrepared && session.IsPrepared, Is.True);
            Assert.That(module.IsActive || controller.IsRunning || controller.GetComponent<Rigidbody2D>().simulated, Is.False);
            Assert.That(controller.GetComponent<PlayerSceneParticipant>().PreparedRequestId, Is.EqualTo(11));
            ((IEmberModule)module).ResetModuleData(); ((IEmberModule)module).OnInit(); session.Start();
            Assert.That(module.Controller, Is.SameAs(controller)); Assert.That(session.IsRunning, Is.True);
            Assert.Throws<InvalidOperationException>(() => session.Start());
            session.Dispose(); session.Dispose();
            Assert.That(module.HasSession || controller.IsPrepared, Is.False);
            Assert.That(_input.Service.GetChannel("gameplay").IsAvailable, Is.False);
            Assert.That(controller.GetComponent<PlayerSceneParticipant>().PreparedRequestId, Is.Null);
        }

        [UnityTest] public IEnumerator ScopePlayer_LaterParticipantStartFailureReleasesTransferredPlayer() => ScopeStartFailure().ToCoroutine();
        private async UniTask ScopeStartFailure()
        {
            var module = Module(); var controller = Character(); var scope = Scope(module, controller, true);
            var session = await scope.PrepareAsync(12, PlayerEntryRequest.NewGame(), default);
            ((IEmberModule)module).OnInit();
            Assert.Throws<InvalidOperationException>(() => session.Start());
            Assert.That(session.IsDisposed, Is.True);
            Assert.That(module.HasSession || controller.IsPrepared || _input.Service.GetChannel("gameplay").IsAvailable, Is.False);
        }

        [UnityTest] public IEnumerator ScopePlayer_OldResultCannotReleaseReplacementSession() => ScopeOldResult().ToCoroutine();
        private async UniTask ScopeOldResult()
        {
            var module = Module(); var first = Character();
            var old = await Scope(module, first).PrepareAsync(13, PlayerEntryRequest.NewGame(), default);
            ((IEmberModule)module).OnInit(); old.Start(); module.StopSession();
            var replacement = Character();
            var current = await Scope(module, replacement).PrepareAsync(14, PlayerEntryRequest.NewGame(), default);
            current.Start(); old.Dispose();
            Assert.That(module.Controller, Is.SameAs(replacement));
            Assert.That(replacement.IsRunning, Is.True);
            current.Dispose();
        }

        [UnityTest] public IEnumerator ScopePlayer_DestroyedPlayerReleasesWholeScope() => ScopeDestroyed().ToCoroutine();
        private async UniTask ScopeDestroyed()
        {
            var module = Module(); var controller = Character();
            var session = await Scope(module, controller).PrepareAsync(15, PlayerEntryRequest.NewGame(), default);
            ((IEmberModule)module).OnInit(); session.Start();
            UnityEngine.Object.DestroyImmediate(controller.gameObject);
            Assert.That(session.IsDisposed, Is.True);
            Assert.That(module.HasSession, Is.False);
        }

        [UnityTest] public IEnumerator ScopePlayer_ReadFailureDoesNotLeavePreparedPlayer() => ScopeReadFailure().ToCoroutine();
        private async UniTask ScopeReadFailure()
        {
            var module = Module(); var controller = Character(); var scope = Scope(module, controller);
            bool failed = false;
            try { await scope.PrepareAsync(16, PlayerEntryRequest.FromSlot(0), default); }
            catch (InvalidOperationException) { failed = true; }
            Assert.That(failed, Is.True); Assert.That(controller.IsPrepared || module.HasSession, Is.False);
            var retry = await scope.PrepareAsync(17, PlayerEntryRequest.NewGame(), default);
            Assert.That(retry.IsPrepared, Is.True); retry.Dispose();
        }
    }

    // 测试只替换模块解析；实际玩家准备、返回结果及 Scope 生命周期均使用生产实现。
    public sealed class PlayerScopeDependencyProbe : GameSceneParticipant, IGameSceneReplaceableParticipant
    {
        [NonSerialized] public PlayerSceneParticipant Player;
        [NonSerialized] public PlayerModule Module;
        [NonSerialized] public GameInputModule Input;
        [NonSerialized] public PlayerDataModule Data;
        public override IReadOnlyList<GameSceneParticipant> OwnedParticipants => new GameSceneParticipant[] { Player };
        public override UniTask<IGameScenePreparedParticipant> PrepareAsync(GameScenePreparationContext context)
            => (UniTask<IGameScenePreparedParticipant>)typeof(PlayerSceneParticipant)
                .GetMethod("PrepareWithDependencies", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(Player, new object[] { context, Module, Input, Data });
        public UniTask<IGameScenePreparedParticipant> PrepareReplacementAsync(GameScenePreparationContext context,
            IGameScenePreparedParticipant current) => Player.PrepareReplacementAsync(context, current);
    }

    public sealed class PlayerScopeFailStartProbe : GameSceneParticipant
    {
        public override UniTask<IGameScenePreparedParticipant> PrepareAsync(GameScenePreparationContext context)
            => UniTask.FromResult<IGameScenePreparedParticipant>(new Result());
        private sealed class Result : IGameScenePreparedParticipant
        {
            public void Start() => throw new InvalidOperationException("expected later participant failure");
            public void Dispose() { }
        }
    }
}
