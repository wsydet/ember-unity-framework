using System;
using System.Collections;
using System.Reflection;
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
        [UnityTest] public IEnumerator RollingPublicSavePauseAndRecoveryDoNotResumeAction()
            => RollingPublicRecovery().ToCoroutine();

        private async UniTask RollingPublicRecovery()
        {
            _config.Configure(Resources.Load<EmberFsmGraph>("Config/Player/PlayerGraph"), new Vector2(0, 4));
            var settings = typeof(PlayerMoveFeel).GetField("_rolling", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_config.Feel.Movement);
            PlayerDashEffectTestAssets.Set(settings, "_enabled", true);
            PlayerDashEffectTestAssets.Set(settings, "_speed", 5f);
            PlayerDashEffectTestAssets.Set(settings, "_duration", .5f);
            var floor = Keep(new GameObject("RollingRecoveryFloor")); SceneManager.MoveGameObjectToScene(floor, _scene);
            floor.transform.position = new Vector2(0, -.5f); floor.AddComponent<BoxCollider2D>().size = new Vector2(20, 1);
            Physics2D.SyncTransforms();
            var (module, host, flow) = await RecoveryFlow();
            try
            {
                var player = module.Controller; var initial = module.RecoveryTarget;
                using var allow = _input.Service.Request(new object(), 100, new Game.Input.GameInputRule("gameplay", Game.Input.GameInputPermission.Allow));
                for (int i = 0; i < 150 && !player.Context.Rolling.IsActive; i++)
                {
                    Keys(UnityEngine.InputSystem.Key.D); player.Tick(.02f); player.FixedTick(.02f); await UniTask.WaitForFixedUpdate();
                }
                Assert.That(player.CurrentStateId, Is.EqualTo("rolling"));
                Assert.Throws<InvalidOperationException>(() => module.Save(0));
                Assert.That(module.RecoveryTarget, Is.SameAs(initial)); Assert.That(_data.LastSuccessfulSaveSlot, Is.Null);
                var old = player.Context; float remaining = old.Rolling.Remaining;
                using (module.Pause())
                {
                    player.Tick(3); player.FixedTick(3);
                    Assert.That(Time.timeScale, Is.Zero); Assert.That(old.Rolling.Remaining, Is.EqualTo(remaining));
                }
                Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(old.Rolling.IsActive, Is.False); Assert.That(player.Context.Rolling.IsActive, Is.False);
                Assert.That(module.RecoveryTarget, Is.SameAs(initial));
                player.Context.Motor.Body.position = new Vector2(0, .89f); player.Context.Motor.Body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms(); Keys(); player.Tick(.02f); player.FixedTick(.02f);
                Assert.That((await module.Save(0).Completion).Succeeded, Is.True);
                Assert.That((await module.LoadCheckpoint(flow, 0).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                for (int i = 0; i < 3; i++)
                {
                    Keys(UnityEngine.InputSystem.Key.D); player.Tick(.02f); player.FixedTick(.02f); await UniTask.WaitForFixedUpdate();
                    Assert.That(player.Context.Rolling.IsActive, Is.False, "站地恢复不能生成新的落地事件");
                }
            }
            finally { host.Session.Dispose(); }
        }
    }
}
