using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.Module;
using Game.PlayerData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests
    {
        [UnityTest] public IEnumerator RecoveryRebuildsRuntimeAndClearsOldTriggerInput() => RecoveryInput().ToCoroutine();
        private async UniTask RecoveryInput()
        {
            var (module, host, flow) = await RecoveryFlow();
            try
            {
                var controller = module.Controller; var context = controller.Context;
                var instance = controller.FsmInstanceId;
                controller.GetComponent<Rigidbody2D>().position = new Vector2(7, 8);
                Keys(Key.Space); ((IEmberUpdate)module).Update();
                Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(controller.Context, Is.Not.SameAs(context));
                Assert.That(controller.FsmInstanceId, Is.Not.EqualTo(instance));
                Assert.That(controller.Context.Motor.Position, Is.EqualTo(module.RecoveryTarget.Position));
                ((IEmberUpdate)module).Update();
                Assert.That(controller.Context.Input.JumpRequested, Is.False);
                Assert.That(_data.LastSuccessfulSaveSlot, Is.Null);
                Keys(); Keys(Key.Space); ((IEmberUpdate)module).Update();
                Assert.That(controller.Context.Input.JumpRequested, Is.True);
            }
            finally { host.Session.Dispose(); }
        }

        [UnityTest] public IEnumerator RecoveryPreservesAnExistingWholePause() => RecoveryPaused().ToCoroutine();
        private async UniTask RecoveryPaused()
        {
            var (module, host, flow) = await RecoveryFlow();
            try
            {
                using (module.Pause())
                {
                    Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                    Assert.That(module.State, Is.EqualTo(PlayerSessionState.Paused));
                    Assert.That(Time.timeScale, Is.Zero);
                }
                Assert.That(module.State, Is.EqualTo(PlayerSessionState.Running));
                Assert.That(Time.timeScale, Is.EqualTo(1));
            }
            finally { host.Session.Dispose(); }
        }

        [UnityTest] public IEnumerator RecoveryRejectsStoppedSessionWithoutRestartingIt() => RecoveryStopped().ToCoroutine();
        private async UniTask RecoveryStopped()
        {
            var (module, host, flow) = await RecoveryFlow();
            try
            {
                module.StopSession();
                Assert.Throws<InvalidOperationException>(() => module.Recover(flow));
                Assert.That(module.HasSession, Is.False);
            }
            finally { host.Session.Dispose(); }
        }
#if UNITY_EDITOR
        private PlayerController FormalCharacter()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameResource/Player/PlayerBlock.prefab");
            Assert.That(prefab, Is.Not.Null);
            var go = Keep(UnityEngine.Object.Instantiate(prefab)); SceneManager.MoveGameObjectToScene(go, _scene);
            var player = go.GetComponent<PlayerController>();
            Assert.That(player.IsPrepared, Is.False);
            Assert.That(go.GetComponent<Rigidbody2D>().simulated, Is.False);
            Assert.That(go.GetComponent<BoxCollider2D>().size, Is.EqualTo(new Vector2(1, 1.75f)));
            return player;
        }

        [UnityTest] public IEnumerator FormalPrefab_SaveExitReenter_AndSeparateInstances() => FormalLifecycle().ToCoroutine();
        private async UniTask FormalLifecycle()
        {
            _config = Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH);
            var module = Module(); var controller = FormalCharacter();
            var candidate = await Prepare(module, controller);
            ((IEmberModule)module).ResetModuleData(); ((IEmberModule)module).OnInit(); module.StartSession(candidate);
            Assert.That(controller.CurrentStateId, Is.EqualTo("fall"));
            controller.GetComponent<Rigidbody2D>().position = new Vector2(4, 5);
            controller.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(-2, 3);
            controller.Context.Facing.SetFacing(false);
            var snapshot = controller.Capture(_config.Scene);
            var oldInstance = controller.FsmInstanceId;
            var flow = new EmberStateMachine(); flow.Register(new ExitProbe(module, _data));
            flow.Register(new MainState()); flow.Start<ExitProbe>();
            Assert.That((await module.ExitToMenuAsync(flow, 0)).Status, Is.EqualTo(PlayerExitStatus.Completed));
            Assert.That(controller.IsPrepared, Is.False);
            UnityEngine.Object.DestroyImmediate(controller.gameObject);
            // 关闭并重开磁盘服务，确保恢复并非命中运行时对象缓存。
            await _data.StopAsync(); ((IEmberModule)_data).OnInit();
            controller = FormalCharacter(); candidate = await Prepare(module, controller, PlayerEntryRequest.FromSlot(0));
            ((IEmberModule)module).ResetModuleData(); ((IEmberModule)module).OnInit(); module.StartSession(candidate);
            Assert.That(controller.Context.Motor.Position, Is.EqualTo(snapshot.Position));
            Assert.That(controller.Context.Motor.Velocity, Is.EqualTo(snapshot.Velocity));
            Assert.That(controller.Context.Facing.FacesRight, Is.False);
            Assert.That(controller.CurrentStateId, Is.EqualTo("jump"));
            Assert.That(controller.FsmInstanceId, Is.Not.EqualTo(oldInstance));
            var second = Module(); var other = FormalCharacter();
            var otherCandidate = await Prepare(second, other); ((IEmberModule)second).OnInit(); second.StartSession(otherCandidate);
            Assert.That(other.Context, Is.Not.SameAs(controller.Context));
            Assert.That(other.FsmInstanceId, Is.Not.EqualTo(controller.FsmInstanceId));
            using (module.Pause())
            {
                Assert.That(other.IsRunning, Is.True);
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(controller.GetComponent<Rigidbody2D>().simulated, Is.False);
            }
            second.StopSession();
        }
#endif
    }
}
