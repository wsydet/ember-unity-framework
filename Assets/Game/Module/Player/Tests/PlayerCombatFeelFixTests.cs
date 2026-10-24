#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private PlayerScheme FeelFixScheme() => Resources.Load<PlayerScheme>("Config/Player/BuiltinSchemes/通用攻击模板1");

        private void WithFeelFix(Action<PlayerController> verify)
        {
            var original = _config;
            var source = Keep(UnityEngine.Object.Instantiate(Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH)));
            PlayerDashEffectTestAssets.Set(source, "_scheme", FeelFixScheme());
            using var capture = new PlayerSchemeCapture(source);
            PlayerController player = null;
            try
            {
                _config = capture.Config;
                var animation = _config.CapturedAnimation;
                typeof(PlayerConfig).GetProperty("CapturedAnimation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_config, null);
                player = AnimatedCharacter(animation); Land(player); verify(player);
            }
            finally { player?.Stop(); _config = original; }
        }

        [TestCase(0, -1)] [TestCase(0, 1)]
        [TestCase(1, -1)] [TestCase(1, 1)]
        [TestCase(2, -1)] [TestCase(2, 1)]
        public void FeelFix_AllThreeAttacksReachExtendedTargetButNotOutside(int index, int direction)
        {
            using var batch = new EntityHitBatch();
            var source = batch.Register(HitEntity("source", new Vector2(0, 3), false), "source", 1, new EntityHitReceiver(null, null, null));
            var near = new EntityHealth(30, 30); var far = new EntityHealth(30, 30);
            batch.Register(HitEntity("extended", new Vector2(direction * 5.4f, 3), true), "extended", 2, new EntityHitReceiver(near, null, null));
            batch.Register(HitEntity("outside", new Vector2(direction * 6.6f, 3), true), "outside", 2, new EntityHitReceiver(far, null, null));
            Physics2D.SyncTransforms();
            var segment = FeelFixScheme().Feel.Attack.Segments[index];
            using var hit = new PlayerAttackHitSession(batch, source);
            hit.Begin(segment, direction, new Vector2(0, 3)); hit.Sample(0, .2, new Vector2(0, 3)); batch.Resolve();
            Assert.That(near.Current, Is.EqualTo(30 - segment.Windows[0].Payload.Damage));
            Assert.That(far.Current, Is.EqualTo(30));
            Assert.That(segment.Visual.Offset.x, Is.EqualTo(2.4f));
        }

        [Test]
        public void FeelFix_AirDashExitRetainsQuarterSpeed()
        {
            WithFeelFix(player =>
            {
                Place(player, new Vector2(0, 10), 0); Keys(Key.D, Key.LeftShift); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("air-dash"));
                float speed = player.Context.Motor.Velocity.x;
                Keys(); UntilCombat(player, "combat.dash-recovery", 30);
                Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(speed * .25f).Within(.001f));
            });
        }

        [Test]
        public void FeelFix_InterruptedHitReturnsToIdleWithoutPositionResidue()
        {
            WithFeelFix(player =>
            {
                player.Context.HitReceiver.Receive(TestHit(stun: .15f, x: 0, y: 0)); Decide(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("life.hit"));
                var animator = player.GetComponentInChildren<Animator>();
                var sprite = player.GetComponentInChildren<PlayerAnimationView>().Sprite.transform;
                animator.Update(0); animator.Update(.1f);
                Assert.That(sprite.localPosition.y, Is.GreaterThan(.1f));
                for (int i = 0; i < 20 && player.CurrentStateId == "life.hit"; i++) Simulate(player);
                player.Tick(0);
                Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
                Assert.That(sprite.localPosition.y, Is.Zero.Within(.0001f));
            });
        }
    }
}
#endif
