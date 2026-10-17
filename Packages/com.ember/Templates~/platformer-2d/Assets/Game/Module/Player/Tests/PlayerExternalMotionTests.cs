using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private PlayerConstantVelocitySource ExternalOwner()
            => Keep(new GameObject("External source owner")).AddComponent<PlayerConstantVelocitySource>();

        [Test]
        public void ExternalMotion_OverlapReleaseAndDisableDoNotAccumulate()
        {
            var player = Character(); Land(player);
            var owner = ExternalOwner();
            var a = player.Context.ExternalMotion.Register(owner,
                new PlayerExternalMotionRequest("a", PlayerExternalMotionMode.AddVelocity, new Vector2(2, 0)));
            var b = player.Context.ExternalMotion.Register(ExternalOwner(),
                new PlayerExternalMotionRequest("b", PlayerExternalMotionMode.AddVelocity, new Vector2(3, 0)));
            for (int i = 0; i < 5; i++)
            { Simulate(player); Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(5).Within(.001f)); }
            b.Dispose(); Simulate(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(2).Within(.001f));
            owner.enabled = false; Simulate(player);
            Assert.That(a.IsActive, Is.False);
            Assert.That(player.Context.Motor.Velocity.x, Is.Zero.Within(.001f));
        }

        [Test]
        public void ExternalMotion_ReplacementPriorityAndStaleReleaseAreDeterministic()
        {
            var player = Character(); Land(player);
            var owner = ExternalOwner();
            var old = player.Context.ExternalMotion.Register(owner,
                new PlayerExternalMotionRequest("low", PlayerExternalMotionMode.ReplaceVelocity, new Vector2(2, 0), priority: 1));
            var current = player.Context.ExternalMotion.Register(owner,
                new PlayerExternalMotionRequest("high", PlayerExternalMotionMode.ReplaceVelocity, new Vector2(7, 0), priority: 2));
            old.Dispose(); old.Dispose(); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(7));
            Assert.That(current.IsActive, Is.True);
            Assert.That(player.Context.ExternalMotion.Describe(), Does.Contain("high"));
        }

        [Test]
        public void ExternalMotion_PauseDoesNotConsumeDurationAndStopInvalidatesLeases()
        {
            var player = Character(); Land(player);
            var lease = player.Context.ExternalMotion.Register(ExternalOwner(),
                new PlayerExternalMotionRequest("timed", PlayerExternalMotionMode.AddVelocity, Vector2.right, .1f));
            player.Pause();
            for (int i = 0; i < 20; i++) Decide(player);
            Assert.That(lease.RemainingSeconds, Is.EqualTo(.1f));
            player.Resume();
            for (int i = 0; i < 5; i++) Decide(player);
            Assert.That(lease.IsActive, Is.False);
            var other = player.Context.ExternalMotion.Register(ExternalOwner(),
                new PlayerExternalMotionRequest("stop", PlayerExternalMotionMode.AddVelocity, Vector2.right));
            player.Stop(); Assert.That(other.IsActive, Is.False);
        }

        [Test]
        public void ExternalMotion_InteractionLocksAreOwnedAndDoNotChangeWorldTime()
        {
            var player = Character(); Land(player);
            float scale = Time.timeScale;
            var motion = player.Context.ExternalMotion;
            motion.Register(ExternalOwner(), new PlayerExternalMotionRequest("environment", PlayerExternalMotionMode.AddVelocity, Vector2.right, .04f));
            var first = motion.Register(ExternalOwner(), new PlayerExternalMotionRequest("editor-a", PlayerExternalMotionMode.InteractionLock, Vector2.zero));
            var second = motion.Register(ExternalOwner(), new PlayerExternalMotionRequest("editor-b", PlayerExternalMotionMode.InteractionLock, Vector2.zero));
            var position = player.Context.Motor.Position;
            Keys(Key.D, Key.Space);
            for (int i = 0; i < 4; i++) Simulate(player);
            Assert.That(Time.timeScale, Is.EqualTo(scale));
            Assert.That(player.Context.Motor.Position, Is.EqualTo(position));
            Assert.That(motion.SourceCount, Is.EqualTo(2), "世界时间中的限时来源应自然到期");
            first.Dispose(); Decide(player);
            Assert.That(motion.IsInteractionLocked, Is.True);
            player.Pause(); player.Resume(); Decide(player);
            Assert.That(player.Context.Motor.IsFrozen, Is.True, "全局恢复不能释放局部锁");
            second.Dispose(); Keys(); Simulate(player);
            Assert.That(player.Context.Motor.IsFrozen, Is.False);
            Assert.That(player.Context.Input.JumpRequested, Is.False);
        }

        [Test]
        public void ExternalMotion_ActiveSourcesRejectInstantSnapshot()
        {
            var player = Character(); Land(player);
            var lease = player.Context.ExternalMotion.Register(ExternalOwner(),
                new PlayerExternalMotionRequest("source", PlayerExternalMotionMode.AddVelocity, Vector2.right));
            Assert.Throws<InvalidOperationException>(() => player.Capture("GameplayScene"));
            lease.Dispose(); Simulate(player);
            Assert.DoesNotThrow(() => player.Capture("GameplayScene"));
        }

        [Test]
        public void ExternalMotion_ForcedRiseExpiresAndUsesSeconds()
        {
            var player = Character(); Land(player);
            var lease = player.Context.ExternalMotion.Register(ExternalOwner(),
                new PlayerExternalMotionRequest("rise", PlayerExternalMotionMode.ForcedRise, new Vector2(0, 8), .04f));
            Simulate(player); Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(8).Within(.001f));
            Simulate(player); Assert.That(lease.IsActive, Is.False);
            Simulate(player); Assert.That(player.Context.Motor.Body.gravityScale, Is.GreaterThan(0));
        }
    }
}
