using System;
using System.Linq;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private EntityHeadTarget2D HeadTarget(float x = 0)
        {
            var go = Keep(new GameObject("HeadTarget")); SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = new Vector2(x, 2);
            var shape = go.AddComponent<BoxCollider2D>(); shape.isTrigger = true; shape.size = new Vector2(3, .3f);
            var target = go.AddComponent<EntityHeadTarget2D>(); target.Configure(shape); Physics2D.SyncTransforms(); return target;
        }
        private void InstallHead(bool enabled = true, bool open = true)
        {
            if (_config.Powers == null) SetAirField(_config, "_powers", Keep(ScriptableObject.CreateInstance<PlayerPowerConfig>()));
            var definition = Keep(ScriptableObject.CreateInstance<PlayerAbilityDefinition>());
            SetAirField(definition, "_id", "head-jump"); SetAirField(definition, "_defaultOpen", open);
            SetAirField(definition, "_limitedUses", true); SetAirField(definition, "_maximumUses", 2);
            SetAirField(_config.Powers, "_abilities", _config.Powers.AbilitySlots.Concat(new[] { new EntityAbilitySlot(definition, enabled) }).ToArray());
        }
        private void CrossHead(PlayerController p, bool press = true)
        {
            Keys(); Decide(p);
            p.Context.Motor.Body.position = new Vector2(0, 2.8f); p.Context.Motor.Body.linearVelocity = new Vector2(2, -8);
            Physics2D.SyncTransforms(); if (press) Keys(Key.Space); Decide(p);
        }
        [Test]
        public void HeadJumpConsumesOncePreservesHorizontalAndNoShortJumpOrRefill()
        {
            InstallAirJump(); InstallHead(); HeadTarget(); var p = Character(new Vector2(0, 4), new Vector2(2, -8));
            var air = AirResource(p); air.TryCommit(air.CreateAttempt()); CrossHead(p);
            Assert.That(p.CurrentStateId, Is.EqualTo("head-jump")); Assert.That(p.Context.Motor.Velocity.x, Is.EqualTo(2));
            Assert.That(p.Context.Motor.Velocity.y, Is.EqualTo(12.528f));
            p.Context.Abilities.TryGet("head-jump", out var head); Assert.That(head.RemainingUses, Is.EqualTo(1));
            Assert.That(air.RemainingUses, Is.Zero); Assert.That(p.Context.Input.JumpRequested, Is.False);
            Assert.Throws<InvalidOperationException>(() => p.Capture("GameplayScene"));
            Keys(); Simulate(p); Assert.That(p.Context.Motor.Velocity.y, Is.GreaterThan(10));
            p.Context.Motor.Body.linearVelocity = Vector2.zero; Decide(p);
            Assert.That(p.CurrentStateId, Is.EqualTo("fall")); Assert.That(p.Context.HeadJump.IsActive, Is.False);
            Assert.DoesNotThrow(() => p.Capture("GameplayScene"));
        }
        [TestCase(false, true)] [TestCase(true, false)]
        public void HeadJumpDisabledOrLockedDoesNotConsume(bool enabled, bool open)
        {
            InstallHead(enabled, open); HeadTarget(); var p = Character(new Vector2(0, 4), new Vector2(0, -8)); CrossHead(p);
            Assert.That(p.CurrentStateId, Is.Not.EqualTo("head-jump"));
            p.Context.Abilities.TryGet("head-jump", out var head); Assert.That(head.RemainingUses, Is.EqualTo(2));
        }
        [Test]
        public void HeadJumpRequiresInputAndDoesNotTriggerLateInsideRegion()
        {
            InstallHead(); HeadTarget(); var p = Character(new Vector2(0, 4), new Vector2(0, -8)); CrossHead(p, false);
            Assert.That(p.Context.HeadJump.IsActive, Is.False); Keys(Key.Space); Decide(p);
            Assert.That(p.Context.HeadJump.IsActive, Is.False);
        }
        [Test]
        public void HeadProbeRejectsSideBelowAndInitialOverlapButSweepsFastDescent()
        {
            var target = HeadTarget(); var p = Character(new Vector2(0, 2));
            var probe = new EntityHeadProbe2D(p.Context.Entity, p.Context.Sensors.Shape);
            probe.Sample(true); Assert.That(probe.Candidate, Is.Null);
            p.Context.Motor.Body.position = new Vector2(0, 4); Physics2D.SyncTransforms(); probe.Sample(true);
            Assert.That(probe.Candidate, Is.Null);
            p.Context.Motor.Body.position = new Vector2(0, 1); Physics2D.SyncTransforms(); probe.Sample(true);
            Assert.That(probe.Candidate, Is.SameAs(target)); probe.Sample(true); Assert.That(probe.Candidate, Is.Null);
            probe.Clear(); p.Context.Motor.Body.position = new Vector2(-4, 2.8f); Physics2D.SyncTransforms(); probe.Sample(true);
            p.Context.Motor.Body.position = new Vector2(0, 2.8f); Physics2D.SyncTransforms(); probe.Sample(true);
            Assert.That(probe.Candidate, Is.Null);
        }
        [Test]
        public void HeadTargetMovementOrDynamicBodyInvalidatesCandidate()
        {
            var target = HeadTarget(); target.transform.position += Vector3.right; Physics2D.SyncTransforms();
            Assert.That(target.IsAvailable, Is.False); target.Configure(target.Head);
            Assert.That(target.IsAvailable, Is.True); target.gameObject.AddComponent<Rigidbody2D>();
            Assert.That(target.IsAvailable, Is.False);
        }
        [Test]
        public void HeadJumpTakesPriorityOverAirJumpWithoutConsumingItsResource()
        {
            InstallAirJump(); InstallHead(); HeadTarget(); var p = Character(new Vector2(0, 4), new Vector2(0, -8));
            CrossHead(p); Assert.That(p.CurrentStateId, Is.EqualTo("head-jump"));
            Assert.That(AirResource(p).RemainingUses, Is.EqualTo(1));
        }
        [Test]
        public void HeadProbeRearmsOnlyAfterLeavingAndIsIndependentPerHost()
        {
            var target = HeadTarget(); var a = Character(new Vector2(0, 4)); var b = Character(new Vector2(0, 4));
            var probes = new[] { new EntityHeadProbe2D(a.Context.Entity, a.Context.Sensors.Shape),
                new EntityHeadProbe2D(b.Context.Entity, b.Context.Sensors.Shape) };
            foreach (var probe in probes) probe.Sample(true);
            a.Context.Motor.Body.position = b.Context.Motor.Body.position = new Vector2(0, 2.8f); Physics2D.SyncTransforms();
            foreach (var probe in probes) { probe.Sample(true); Assert.That(probe.Candidate, Is.SameAs(target)); }
            probes[0].Sample(true); Assert.That(probes[0].Candidate, Is.Null);
            a.Context.Motor.Body.position = new Vector2(0, 4); Physics2D.SyncTransforms(); probes[0].Sample(true);
            a.Context.Motor.Body.position = new Vector2(0, 2.8f); Physics2D.SyncTransforms(); probes[0].Sample(true);
            Assert.That(probes[0].Candidate, Is.SameAs(target));
        }
        [Test]
        public void HeadJumpPauseAndStopClearPerPlayerState()
        {
            InstallHead(); HeadTarget(); var p = Character(new Vector2(0, 4), new Vector2(0, -8)); CrossHead(p);
            var old = p.Context; p.Pause(); p.Tick(3); p.FixedTick(3); Assert.That(old.HeadJump.IsActive, Is.True);
            p.Resume(); p.Stop(); Assert.That(old.HeadJump.IsActive, Is.False); Assert.That(old.HeadJump.Target, Is.Null);
        }
    }
}
