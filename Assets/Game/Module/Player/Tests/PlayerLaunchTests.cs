using Game.Entity;
using NUnit.Framework;
using UnityEngine;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        [TestCase(-5)] [TestCase(0)] [TestCase(5)]
        public void Launch_InitialVelocityIsAppliedOnce(int x)
        {
            var player = Character(); Land(player);
            var target = player.gameObject.AddComponent<PlayerLaunchTarget2D>();
            Assert.That(target.TryLaunch(ExternalOwner(), new LaunchRequest2D("impulse", new Vector2(x, 8))), Is.EqualTo(LaunchResult2D.Accepted));
            Decide(player);
            Assert.That(player.Context.Motor.Velocity, Is.EqualTo(new Vector2(x, 8)));
            Assert.That(target.IsLaunching, Is.False);
        }

        [Test]
        public void Launch_ReplaceExternalActionAndStopVerticalOnExpiry()
        {
            var player = Character(new Vector2(0, 8));
            var target = player.gameObject.AddComponent<PlayerLaunchTarget2D>();
            var action = player.Context.ExternalMotion.Register(ExternalOwner(),
                new PlayerExternalMotionRequest("custom action", PlayerExternalMotionMode.AddVelocity, Vector2.right));
            int interrupted = 0; target.Accepted += _ => interrupted++;
            var request = new LaunchRequest2D("rise", new Vector2(3, 8), LaunchMode2D.ConstantRise, .04f, LaunchEnd2D.StopVertical);
            Assert.That(target.TryLaunch(ExternalOwner(), request), Is.EqualTo(LaunchResult2D.Accepted));
            Assert.That(action.IsActive, Is.False); Assert.That(interrupted, Is.EqualTo(1));
            Simulate(player); Assert.That(player.Context.Motor.Velocity, Is.EqualTo(new Vector2(3, 8)));
            Simulate(player); Decide(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.Zero.Within(.001f));
            Assert.That(player.Context.Motor.Body.gravityScale, Is.GreaterThan(0));
        }

        [Test]
        public void Launch_CooldownDeduplicatesMultipleCollidersAndCarrierAccepts()
        {
            var support = MovingSupport();
            var target = support.gameObject.AddComponent<TranslationSupportLaunchTarget2D>();
            var zone = Keep(new GameObject("Launch zone")).AddComponent<LaunchZone2D>();
            var first = support.GetComponent<Collider2D>();
            var second = support.gameObject.AddComponent<BoxCollider2D>();
            Assert.That(zone.TryTrigger(first), Is.True);
            Assert.That(zone.TryTrigger(second), Is.False);
            Assert.That(support.Velocity.y, Is.EqualTo(10));
            Assert.That(target.TryLaunch(zone, new LaunchRequest2D("carrier", new Vector2(-3, 7), LaunchMode2D.ConstantRise, .04f)), Is.EqualTo(LaunchResult2D.Accepted));
            Assert.That(target.IsLaunching, Is.True);
            target.enabled = false; Assert.That(target.IsLaunching, Is.False);
        }
    }
}
