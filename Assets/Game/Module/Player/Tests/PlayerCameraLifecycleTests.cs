using System;
using System.Collections;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Ember.Camera;
using Ember.Core;
using Game.PlayerCamera;
using Game.SceneFlow;
using NUnit.Framework;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests
    {
        private sealed class CameraTestRig : IDisposable
        {
            internal EmberCameraManager Manager;
            internal Camera PreviousOutput;
            internal CinemachineBrain PreviousBrain;
            internal CinemachineCamera Camera;
            internal EmberCameraRegistration Registration;
            internal GameSceneScope Scope;
            internal PlayerCameraParticipant Adapter;
            internal PlayerScopeDependencyProbe Player;
            internal GameSceneSession Session;
            public void Dispose()
            {
                Session?.Dispose();
                Registration.UnregisterCamera();
                typeof(EmberCameraManager).GetProperty("MainCamera").SetValue(Manager, PreviousOutput);
                typeof(EmberCameraManager).GetProperty("Brain").SetValue(Manager, PreviousBrain);
            }
        }

        private CameraTestRig CameraRig(PlayerModule module, PlayerController controller)
        {
            var manager = EmberCameraManager.Instance;
            var rig = new CameraTestRig { Manager = manager, PreviousOutput = manager.MainCamera, PreviousBrain = manager.Brain };
            var output = Keep(new GameObject("CameraTestOutput")); SceneManager.MoveGameObjectToScene(output, _scene);
            var camera = output.AddComponent<Camera>(); camera.orthographic = true;
            var brain = output.AddComponent<CinemachineBrain>();
            typeof(EmberCameraManager).GetProperty("MainCamera").SetValue(manager, camera);
            typeof(EmberCameraManager).GetProperty("Brain").SetValue(manager, brain);
            var go = Keep(new GameObject("CameraTestFollow")); go.SetActive(false); SceneManager.MoveGameObjectToScene(go, _scene);
            rig.Camera = go.AddComponent<CinemachineCamera>();
            var follow = go.AddComponent<CinemachineFollow>();
            follow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
            follow.TrackerSettings.PositionDamping = new Vector3(2, 2, 0); // 恢复必须清除非零历史，不能只依赖默认零阻尼。
            follow.FollowOffset = new Vector3(0, 1, -10);
            rig.Registration = go.AddComponent<EmberCameraRegistration>();
            typeof(EmberCameraRegistration).GetField("_cameraKey", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(rig.Registration, "CameraTest-" + Guid.NewGuid());
            go.SetActive(true);
            rig.Scope = Scope(module, controller);
            rig.Player = rig.Scope.GetComponent<PlayerScopeDependencyProbe>();
            rig.Adapter = rig.Scope.gameObject.AddComponent<PlayerCameraParticipant>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(PlayerCameraParticipant).GetField("_player", flags).SetValue(rig.Adapter, rig.Player.Player);
            typeof(PlayerCameraParticipant).GetField("_camera", flags).SetValue(rig.Adapter, rig.Camera);
            typeof(PlayerCameraParticipant).GetField("_registration", flags).SetValue(rig.Adapter, rig.Registration);
            var proxy = rig.Scope.gameObject.AddComponent<PlayerCameraDependencyProbe>();
            proxy.Adapter = rig.Adapter; proxy.Player = module; proxy.Manager = manager;
            typeof(GameSceneScope).GetField("_participants", flags).SetValue(rig.Scope,
                new GameSceneParticipant[] { proxy, rig.Player });
            return rig;
        }

        [UnityTest] public IEnumerator Camera_PrepareCommitRecoveryAndRelease() => CameraLifecycle().ToCoroutine();
        private async UniTask CameraLifecycle()
        {
            var module = Module(); var controller = Character();
            using var rig = CameraRig(module, controller);
            int committed = 0, released = 0;
            module.SessionCommitted += _ => committed++;
            module.SessionReleased += _ => released++;
            rig.Session = await rig.Scope.PrepareAsync(201, PlayerEntryRequest.NewGame(), default);
            Assert.That(committed, Is.Zero); Assert.That(rig.Camera.Follow, Is.Null);
            Assert.That(rig.Manager.ActiveCamera, Is.Not.SameAs(rig.Camera));
            ((IEmberModule)module).OnInit(); rig.Session.Start();
            Assert.That(committed, Is.EqualTo(1));
            Assert.That(rig.Camera.Follow, Is.SameAs(controller.transform));
            Assert.That(rig.Camera.LookAt, Is.SameAs(controller.transform));
            Assert.That(rig.Manager.ActiveCamera, Is.SameAs(rig.Camera));
            var saved = module.RecoveryTarget;
            controller.transform.position = new Vector3(150, 40, 0);
            rig.Camera.UpdateCameraState(Vector3.up, -1);
            using (module.Pause())
            {
                using var replacement = await rig.Session.PrepareReplacementAsync(rig.Player, 202,
                    PlayerEntryRequest.FromSnapshot(saved), default);
                Assert.That(committed, Is.EqualTo(1), "准备阶段不能通知或切镜头");
                Assert.That(rig.Camera.State.RawPosition.x, Is.GreaterThan(100));
                replacement.Commit();
                Assert.That(committed, Is.EqualTo(2));
                Assert.That(Vector3.Distance(rig.Camera.State.RawPosition,
                    controller.transform.position + new Vector3(0, 1, -10)), Is.LessThan(0.001f));
            }
            rig.Session.Dispose();
            Assert.That(released, Is.EqualTo(1));
            Assert.That(rig.Camera.Follow == null && rig.Camera.LookAt == null, Is.True);
            Assert.That(rig.Manager.ActiveCamera, Is.Not.SameAs(rig.Camera));
        }

        [UnityTest] public IEnumerator Camera_InvalidWiringRejectsBeforePlayerPreparation() => CameraInvalid().ToCoroutine();
        private async UniTask CameraInvalid()
        {
            var module = Module(); var controller = Character(); using var rig = CameraRig(module, controller);
            rig.Camera.GetComponent<CinemachineFollow>().enabled = false;
            bool rejected = false;
            try { await rig.Scope.PrepareAsync(203, PlayerEntryRequest.NewGame(), default); }
            catch (InvalidOperationException) { rejected = true; }
            Assert.That(rejected, Is.True); Assert.That(controller.IsPrepared || module.HasSession, Is.False);
            Assert.That(rig.Camera.Follow, Is.Null); Assert.That(rig.Manager.ActiveCamera, Is.Not.SameAs(rig.Camera));
        }

        [UnityTest] public IEnumerator Camera_FailedPlayerCandidateDoesNotBind() => CameraRejectedPlayer().ToCoroutine();
        private async UniTask CameraRejectedPlayer()
        {
            var module = Module(); var controller = Character(); using var rig = CameraRig(module, controller);
            bool rejected = false;
            try { await rig.Scope.PrepareAsync(204, PlayerEntryRequest.FromSlot(0), default); }
            catch (InvalidOperationException) { rejected = true; }
            Assert.That(rejected, Is.True); Assert.That(rig.Camera.Follow, Is.Null);
            Assert.That(rig.Manager.ActiveCamera, Is.Not.SameAs(rig.Camera));
            rig.Session = await rig.Scope.PrepareAsync(205, PlayerEntryRequest.NewGame(), default);
            ((IEmberModule)module).OnInit(); rig.Session.Start();
            Assert.That(rig.Camera.Follow, Is.SameAs(controller.transform));
        }

        [UnityTest] public IEnumerator Camera_DestroyedPlayerClearsReferences() => CameraDestroyedPlayer().ToCoroutine();
        private async UniTask CameraDestroyedPlayer()
        {
            var module = Module(); var controller = Character(); using var rig = CameraRig(module, controller);
            rig.Session = await rig.Scope.PrepareAsync(206, PlayerEntryRequest.NewGame(), default);
            ((IEmberModule)module).OnInit(); rig.Session.Start();
            UnityEngine.Object.DestroyImmediate(controller.gameObject);
            Assert.That(rig.Session.IsDisposed, Is.True);
            Assert.That(rig.Camera.Target.TrackingTarget == null && rig.Camera.Target.LookAtTarget == null, Is.True);
            Assert.That(rig.Manager.ActiveCamera, Is.Not.SameAs(rig.Camera));
        }

        [UnityTest] public IEnumerator Camera_WrongOrderRejectedDuringPreparation() => CameraWrongOrder().ToCoroutine();
        private async UniTask CameraWrongOrder()
        {
            var module = Module(); var controller = Character(); using var rig = CameraRig(module, controller);
            typeof(GameSceneScope).GetField("_participants", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(rig.Scope, new GameSceneParticipant[] { rig.Player, rig.Scope.GetComponent<PlayerCameraDependencyProbe>() });
            bool rejected = false;
            try { await rig.Scope.PrepareAsync(207, PlayerEntryRequest.NewGame(), default); }
            catch (InvalidOperationException) { rejected = true; }
            Assert.That(rejected, Is.True);
            Assert.That(controller.IsPrepared || module.HasSession, Is.False);
            Assert.That(rig.Camera.Follow, Is.Null);
            Assert.That(rig.Manager.ActiveCamera, Is.Not.SameAs(rig.Camera));
        }
    }

    // 只注入现有测试模块，准备/绑定/恢复/释放全部调用生产实现。
    public sealed class PlayerCameraDependencyProbe : GameSceneParticipant
    {
        [NonSerialized] public PlayerCameraParticipant Adapter;
        [NonSerialized] public PlayerModule Player;
        [NonSerialized] public EmberCameraManager Manager;
        public override System.Collections.Generic.IReadOnlyList<GameSceneParticipant> OwnedParticipants
            => new GameSceneParticipant[] { Adapter };
        public override UniTask<IGameScenePreparedParticipant> PrepareAsync(GameScenePreparationContext context)
        {
            try
            {
                return (UniTask<IGameScenePreparedParticipant>)typeof(PlayerCameraParticipant)
                    .GetMethod("PrepareWithDependencies", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(Adapter, new object[] { context, Player, Manager });
            }
            catch (TargetInvocationException failure) { throw failure.InnerException; }
        }
    }
}
