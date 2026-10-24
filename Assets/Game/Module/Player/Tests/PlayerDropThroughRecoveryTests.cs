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
        [UnityTest] public IEnumerator DropPublicSaveRejectsAndRecoveryReleasesLease()
            => DropPublicRecovery().ToCoroutine();
        private async UniTask DropPublicRecovery()
        {
            _config.Configure(Resources.Load<EmberFsmGraph>("Config/Player/PlayerGraph"), new Vector2(0, 3));
            var powers = Keep(ScriptableObject.CreateInstance<PlayerPowerConfig>());
            var ability = Keep(ScriptableObject.CreateInstance<PlayerAbilityDefinition>());
            PlayerDashEffectTestAssets.Set(ability, "_id", "drop-through");
            PlayerDashEffectTestAssets.Set(ability, "_defaultOpen", true);
            PlayerDashEffectTestAssets.Set(ability, "_limitedUses", true);
            PlayerDashEffectTestAssets.Set(ability, "_maximumUses", 2);
            PlayerDashEffectTestAssets.Set(powers, "_abilities", new[] { new EntityAbilitySlot(ability) });
            PlayerDashEffectTestAssets.Set(_config, "_powers", powers);
            var platform = Keep(new GameObject("RecoveryOneWay")); SceneManager.MoveGameObjectToScene(platform, _scene);
            platform.transform.position = new Vector2(0, 1);
            var shape = platform.AddComponent<BoxCollider2D>(); shape.size = new Vector2(8, .2f);
            var effector = platform.AddComponent<PlatformEffector2D>(); effector.useOneWay = true;
            effector.useColliderMask = false; shape.usedByEffector = true;
            var (module, host, flow) = await RecoveryFlow();
            try
            {
                var player = module.Controller;
                using var allow = _input.Service.Request(new object(), 100, new Game.Input.GameInputRule("gameplay", Game.Input.GameInputPermission.Allow));
                for (int i = 0; i < 120 && player.Context.Sensors.OneWayGround == null; i++)
                { player.Tick(.02f); player.FixedTick(.02f); await UniTask.WaitForFixedUpdate(); }
                Assert.That(player.Context.Sensors.OneWayGround, Is.SameAs(shape));
                var initial = module.RecoveryTarget;
                Keys(Key.S, Key.Space); player.Tick(.02f); player.FixedTick(.02f);
                Assert.That(player.Context.DropThrough.IsActive, Is.True);
                Assert.Throws<InvalidOperationException>(() => module.Save(0));
                Assert.That(module.RecoveryTarget, Is.SameAs(initial));
                Assert.That(_data.LastSuccessfulSaveSlot, Is.Null);
                var context = player.Context;
                using (module.Pause())
                {
                    player.Tick(3); player.FixedTick(3);
                    Assert.That(context.DropThrough.IsActive, Is.True);
                    Assert.That(Time.timeScale, Is.Zero);
                }
                Assert.That(Physics2D.GetIgnoreCollision(context.Sensors.Shape, shape), Is.True);
                Keys();
                Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(context.DropThrough.IsActive, Is.False);
                Assert.That(player.Context.DropThrough.IsActive, Is.False);
                Assert.That(Physics2D.GetIgnoreCollision(player.Context.Sensors.Shape, shape), Is.False);
                Assert.That(module.RecoveryTarget, Is.SameAs(initial));
                // 成功保存一个已消费资源的普通下落状态，再经读档和死亡共链恢复。
                player.Context.Abilities.TryGet("drop-through", out var resource); resource.TryCommit(resource.CreateAttempt());
                var save = module.Save(0); Assert.That((await save.Completion).Succeeded, Is.True);
                Assert.That((await module.LoadCheckpoint(flow, 0).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                player.Context.Abilities.TryGet("drop-through", out resource);
                Assert.That(resource.RemainingUses, Is.EqualTo(1)); Assert.That(player.Context.DropThrough.IsActive, Is.False);
                Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                player.Context.Abilities.TryGet("drop-through", out resource);
                Assert.That(resource.RemainingUses, Is.EqualTo(1)); Assert.That(player.Context.DropThrough.IsActive, Is.False);
            }
            finally { host.Session.Dispose(); }
        }
    }
}
