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
        [UnityTest] public IEnumerator HeadJumpPublicSavePauseAndRecoveryDoNotResumeAction()
            => HeadJumpPublicRecovery().ToCoroutine();

        private async UniTask HeadJumpPublicRecovery()
        {
            _config.Configure(Resources.Load<EmberFsmGraph>("Config/Player/PlayerGraph"), new Vector2(0, 4));
            var powers = Keep(ScriptableObject.CreateInstance<PlayerPowerConfig>());
            var ability = Keep(ScriptableObject.CreateInstance<PlayerAbilityDefinition>());
            PlayerDashEffectTestAssets.Set(ability, "_id", "head-jump");
            PlayerDashEffectTestAssets.Set(ability, "_defaultOpen", true);
            PlayerDashEffectTestAssets.Set(ability, "_limitedUses", true);
            PlayerDashEffectTestAssets.Set(ability, "_maximumUses", 2);
            PlayerDashEffectTestAssets.Set(powers, "_abilities", new[] { new EntityAbilitySlot(ability) });
            PlayerDashEffectTestAssets.Set(_config, "_powers", powers);
            var target = Keep(new GameObject("RecoveryHead")); SceneManager.MoveGameObjectToScene(target, _scene);
            target.transform.position = new Vector2(0, 2);
            var shape = target.AddComponent<BoxCollider2D>(); shape.isTrigger = true; shape.size = new Vector2(3, .3f);
            target.AddComponent<EntityHeadTarget2D>().Configure(shape); Physics2D.SyncTransforms();
            var (module, host, flow) = await RecoveryFlow();
            try
            {
                var player = module.Controller; var initial = module.RecoveryTarget;
                using var allow = _input.Service.Request(new object(), 100, new Game.Input.GameInputRule("gameplay", Game.Input.GameInputPermission.Allow));
                player.Context.Motor.Body.position = new Vector2(0, 4);
                player.transform.position = new Vector2(0, 4); Physics2D.SyncTransforms();
                Keys(); player.Tick(.02f); player.FixedTick(.02f);
                player.Context.Motor.Body.position = new Vector2(0, 2.8f);
                player.Context.Motor.Body.linearVelocity = new Vector2(0, -8); Physics2D.SyncTransforms();
                Keys(Key.Space); player.Tick(.02f); player.FixedTick(.02f);
                Assert.That(player.CurrentStateId, Is.EqualTo("head-jump"));
                Assert.Throws<InvalidOperationException>(() => module.Save(0));
                Assert.That(module.RecoveryTarget, Is.SameAs(initial)); Assert.That(_data.LastSuccessfulSaveSlot, Is.Null);
                var old = player.Context; var position = old.Motor.Position;
                using (module.Pause())
                {
                    player.Tick(3); player.FixedTick(3);
                    Assert.That(Time.timeScale, Is.Zero); Assert.That(old.Motor.Position, Is.EqualTo(position));
                    Assert.That(old.HeadJump.IsActive, Is.True);
                }
                Keys();
                Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(old.HeadJump.IsActive, Is.False); Assert.That(old.HeadJump.Target, Is.Null);
                player.Context.Abilities.TryGet("head-jump", out var head);
                Assert.That(head.RemainingUses, Is.EqualTo(2));
                head.TryCommit(head.CreateAttempt());
                Assert.That((await module.Save(0).Completion).Succeeded, Is.True);
                Assert.That((await module.LoadCheckpoint(flow, 0).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                player.Context.Abilities.TryGet("head-jump", out head);
                Assert.That(head.RemainingUses, Is.EqualTo(1));
                Assert.That(player.Context.HeadJump.IsActive, Is.False); Assert.That(player.Context.HeadJump.Target, Is.Null);
            }
            finally { host.Session.Dispose(); }
        }
    }
}
