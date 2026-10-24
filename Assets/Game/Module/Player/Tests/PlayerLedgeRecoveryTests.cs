using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Ember.Core.Fsm;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests
    {
        [UnityTest] public IEnumerator LedgePublicSavePauseAndRecoveryDoNotResumeAction()
            => LedgePublicRecovery().ToCoroutine();

        private async UniTask LedgePublicRecovery()
        {
            _config.Configure(Resources.Load<EmberFsmGraph>("Config/Player/PlayerGraph"), new Vector2(0, 3.2f));
            var powers = Keep(ScriptableObject.CreateInstance<PlayerPowerConfig>());
            var slots = new EntityAbilitySlot[2];
            for (int i = 0; i < 2; i++)
            {
                var ability = Keep(ScriptableObject.CreateInstance<PlayerAbilityDefinition>());
                PlayerDashEffectTestAssets.Set(ability, "_id", i == 0 ? "ledge-hang" : "ledge-climb");
                PlayerDashEffectTestAssets.Set(ability, "_defaultOpen", true);
                PlayerDashEffectTestAssets.Set(ability, "_limitedUses", true);
                PlayerDashEffectTestAssets.Set(ability, "_maximumUses", 2);
                slots[i] = new EntityAbilitySlot(ability);
            }
            PlayerDashEffectTestAssets.Set(powers, "_abilities", slots);
            PlayerDashEffectTestAssets.Set(_config, "_powers", powers);
            var platform = Keep(new GameObject("RecoveryLedge")); SceneManager.MoveGameObjectToScene(platform, _scene);
            platform.transform.position = new Vector2(2.6f, 3);
            platform.AddComponent<BoxCollider2D>().size = new Vector2(4, 2);
            Physics2D.SyncTransforms();
            var (module, host, flow) = await RecoveryFlow();
            try
            {
                var player = module.Controller;
                using var allow = _input.Service.Request(new object(), 100, new Game.Input.GameInputRule("gameplay", Game.Input.GameInputPermission.Allow));
                var initial = module.RecoveryTarget;
                for (int i = 0; i < 4; i++)
                {
                    // 虚拟设备由夹具手动更新；每个取样步重新提交，避免跨自动 FixedUpdate 读取旧缓存。
                    Keys(Key.D); player.Tick(.02f); player.FixedTick(.02f); await UniTask.WaitForFixedUpdate();
                }
                Assert.That(player.CurrentStateId, Is.EqualTo("ledge-hang"));
                Assert.Throws<InvalidOperationException>(() => module.Save(0));
                Assert.That(module.RecoveryTarget, Is.SameAs(initial)); Assert.That(_data.LastSuccessfulSaveSlot, Is.Null);
                Keys(Key.Space); player.Tick(.02f); player.FixedTick(.02f);
                Assert.That(player.CurrentStateId, Is.EqualTo("ledge-climb"));
                Assert.Throws<InvalidOperationException>(() => module.Save(0));
                var old = player.Context; var elapsed = old.Ledge.Elapsed; var position = old.Motor.Position;
                using (module.Pause())
                {
                    player.Tick(3); player.FixedTick(3);
                    Assert.That(Time.timeScale, Is.Zero);
                    Assert.That(old.Ledge.Elapsed, Is.EqualTo(elapsed));
                    Assert.That(old.Motor.Position, Is.EqualTo(position));
                }
                Keys();
                Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(old.Ledge.IsActive, Is.False); Assert.That(old.Ledge.Target, Is.Null);
                Assert.That(player.Context.Ledge.IsActive, Is.False);
                Assert.That(module.RecoveryTarget, Is.SameAs(initial));
                player.Context.Abilities.TryGet("ledge-hang", out var hang); hang.TryCommit(hang.CreateAttempt());
                player.Context.Abilities.TryGet("ledge-climb", out var climb); climb.TryCommit(climb.CreateAttempt());
                Assert.That((await module.Save(0).Completion).Succeeded, Is.True);
                Assert.That((await module.LoadCheckpoint(flow, 0).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                player.Context.Abilities.TryGet("ledge-hang", out hang); player.Context.Abilities.TryGet("ledge-climb", out climb);
                Assert.That(hang.RemainingUses, Is.EqualTo(1)); Assert.That(climb.RemainingUses, Is.EqualTo(1));
                Assert.That(player.Context.Ledge.IsActive, Is.False);
                Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
            }
            finally { host.Session.Dispose(); }
        }
    }
}
