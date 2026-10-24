using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Ember.Core.Fsm;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests
    {
        [UnityTest] public IEnumerator DevelopmentGameplay_UsesNormalSaveReadAndRecoveryWithActualScene()
            => DevelopmentNormalFlow().ToCoroutine();
        private async UniTask DevelopmentNormalFlow()
        {
            var originalScene = _scene;
            _scene = SceneManager.CreateScene("PlayerFeelLab");
            typeof(PlayerConfig).GetField("_scene", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(_config, _scene.name);
            try
            {
                var (module, host, flow) = await RecoveryFlow();
                try
                {
                    Assert.That(module.IsTrial, Is.False);
                    var initial = module.RecoveryTarget;
                    Assert.That(initial.Scene, Is.EqualTo("PlayerFeelLab"));
                    module.Controller.GetComponent<Rigidbody2D>().position = new Vector2(9, 10);
                    Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                    Assert.That(module.Controller.Context.Motor.Position, Is.EqualTo(initial.Position));
                    module.Controller.GetComponent<Rigidbody2D>().position = new Vector2(4, 5);
                    var save = module.Save(0);
                    Assert.That(Time.timeScale, Is.EqualTo(1));
                    Assert.That(module.Controller.IsRunning, Is.True);
                    Assert.That((await save.Completion).Succeeded, Is.True);
                    var stored = await _data.ReadAsync(0, new PlatformerSnapshotCodec()).Completion;
                    Assert.That(stored.Succeeded, Is.True);
                    Assert.That(stored.Progress.Scene, Is.EqualTo("PlayerFeelLab"));
                    module.Controller.GetComponent<Rigidbody2D>().position = new Vector2(15, 16);
                    // 查询恢复瞬间的位置时显式持有暂停，避免等待帧内的真实重力移动混入断言。
                    using (module.Pause())
                    {
                        Assert.That((await module.LoadCheckpoint(flow, 0).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                        Assert.That(module.Controller.Context.Motor.Position, Is.EqualTo(stored.Progress.Position));
                    }
                    Assert.That(module.RecoveryTarget.Scene, Is.EqualTo("PlayerFeelLab"));
                    Assert.That(_data.LastSuccessfulSaveSlot, Is.EqualTo(0));
                }
                finally { host.Session.Dispose(); }
            }
            finally { await SceneManager.UnloadSceneAsync(_scene); _scene = originalScene; }
        }

        [UnityTest] public IEnumerator NewGameVelocityOverride_DoesNotReplaceLoadedVelocity()
            => DevelopmentVelocity().ToCoroutine();
        private async UniTask DevelopmentVelocity()
        {
            var module = Module();
            module.SetConfigurationForNextInit(_config, new Vector2(2, 3));
            var controller = Character();
            var candidate = await Prepare(module, controller);
            ((IEmberModule)module).OnInit(); module.StartSession(candidate);
            Assert.That(controller.Context.Motor.Velocity, Is.EqualTo(new Vector2(2, 3)));
            controller.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(4, 5);
            Assert.That((await module.Save(0).Completion).Succeeded, Is.True);
            module.StopSession();
            var loaded = await Prepare(module, controller, PlayerEntryRequest.FromSlot(0));
            module.StartSession(loaded);
            Assert.That(controller.Context.Motor.Velocity, Is.EqualTo(new Vector2(4, 5)));
        }

        [UnityTest] public IEnumerator TrialUsesExplicitSceneAndFrozenInitialConditionsWithoutChangingConfig() => TrialStart().ToCoroutine();
        private async UniTask TrialStart()
        {
            var lab = SceneManager.CreateScene("PlayerFeelLab");
            var module = Module(); var controller = Character();
            SceneManager.MoveGameObjectToScene(controller.gameObject, lab);
            try
            {
                _config.Configure(Resources.Load<EmberFsmGraph>("Config/Player/PlayerGraph"), _config.Spawn);
                var spawn = new Vector2(2, 4); var velocity = new Vector2(1, 3);
                var request = PlayerEntryRequest.ForTrial(new PlayerTrialStart(lab.name, spawn, velocity, _config.Feel));
                typeof(PlayerMoveFeel).GetField("_speed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(_config.Feel.Movement, 99f);
                var candidate = await Prepare(module, controller, request);
                Assert.That(controller.IsRunning, Is.False);
                Assert.That(candidate.IsTrial, Is.True);
                ((IEmberModule)module).OnInit(); module.StartSession(candidate);
                Assert.That(module.IsTrial, Is.True); Assert.That(controller.IsRunning, Is.True);
                Assert.That(controller.Context.Motor.Position, Is.EqualTo(spawn));
                Assert.That(controller.Context.Motor.Velocity, Is.EqualTo(velocity));
                Assert.That(controller.Context.Feel.MoveSpeed, Is.EqualTo(5));
                Assert.That(controller.EntrySnapshot.Scene, Is.EqualTo(lab.name));
                Assert.That(_config.Scene, Is.EqualTo("GameplayScene")); Assert.That(_config.Spawn, Is.EqualTo(new Vector2(0, 3)));
                Assert.That(module.SourceSlot, Is.Null); Assert.That(module.RecoveryTarget, Is.Null);
                Assert.That(_data.LastSuccessfulSaveSlot, Is.Null);
            }
            finally { module.StopSession(); await SceneManager.UnloadSceneAsync(lab); }
        }

        [UnityTest] public IEnumerator TrialRejectsFormalSaveLoadRecoverAndExitWithoutPausing() => TrialSaveGuards().ToCoroutine();
        private async UniTask TrialSaveGuards()
        {
            var module = Module(); var controller = Character();
            var candidate = await Prepare(module, controller, PlayerEntryRequest.ForTrial(
                new PlayerTrialStart(_scene.name, _config.Spawn, Vector2.zero, _config.Feel)));
            ((IEmberModule)module).OnInit(); module.StartSession(candidate);
            Assert.Throws<InvalidOperationException>(() => module.Save(0));
            Assert.Throws<InvalidOperationException>(() => module.LoadCheckpoint(null, 0));
            Assert.Throws<InvalidOperationException>(() => module.Recover(null));
            Assert.That((await module.ExitToMenuAsync(null, 0)).Status, Is.EqualTo(PlayerExitStatus.InvalidState));
            Assert.That((await module.ExitToMenuAsync(null, null)).Status, Is.EqualTo(PlayerExitStatus.InvalidState));
            Assert.That(module.State, Is.EqualTo(PlayerSessionState.Running)); Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(_data.LastSuccessfulSaveSlot, Is.Null);
            module.StopSession(); Assert.That(module.IsTrial, Is.False);
            var formal = await Prepare(module, controller); module.StartSession(formal);
            Assert.That(module.IsTrial, Is.False); Assert.That(module.RecoveryTarget, Is.Not.Null);
        }

        [UnityTest] public IEnumerator TrialRejectsWrongSceneAndCancelledPreparationWithoutStarting() => TrialInvalid().ToCoroutine();
        private async UniTask TrialInvalid()
        {
            var module = Module(); var controller = Character();
            bool rejected = false;
            try { await Prepare(module, controller, PlayerEntryRequest.ForTrial(new PlayerTrialStart("WrongScene", Vector2.zero, Vector2.zero, _config.Feel))); }
            catch (InvalidOperationException) { rejected = true; }
            Assert.That(rejected, Is.True); Assert.That(controller.IsPrepared, Is.False);
            var cancelled = new CancellationToken(true); bool aborted = false;
            try { await Prepare(module, controller, PlayerEntryRequest.ForTrial(new PlayerTrialStart(_scene.name, Vector2.zero, Vector2.zero, _config.Feel)), cancelled); }
            catch (OperationCanceledException) { aborted = true; }
            Assert.That(aborted, Is.True); Assert.That(module.HasSession, Is.False);
            Assert.That(controller.IsRunning, Is.False);
        }

        [Test]
        public void TrialInitialConditionsRejectMissingSceneOrInvalidVectors()
        {
            Assert.Throws<ArgumentException>(() => new PlayerTrialStart(" ", Vector2.zero, Vector2.zero, _config.Feel));
            Assert.Throws<ArgumentException>(() => new PlayerTrialStart("Lab", new Vector2(float.NaN, 0), Vector2.zero, _config.Feel));
            Assert.Throws<ArgumentNullException>(() => PlayerEntryRequest.ForTrial(null));
        }
    }
}
