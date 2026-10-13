#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        [Test]
        public void MotionTraceRecordsOncePerPhysicsStepAndReadsHaveNoEffects()
        {
            var p = Character(); Keys(); Land(p); var trace = p.Context.MotionTrace;
            int count = trace.Count; p.Tick(.02f); Assert.That(trace.Count, Is.EqualTo(count));
            Keys(Key.Space); var position = p.Context.Motor.Position; Simulate(p);
            Assert.That(trace.Count, Is.EqualTo(count + 1));
            Assert.That(trace[trace.Count - 1].Position, Is.EqualTo(position));
            Assert.That(trace[trace.Count - 1].State, Is.EqualTo("jump"));
            var velocity = p.Context.Motor.Velocity; var state = p.CurrentStateId; var time = trace.Time;
            for (int i = 0; i < 100; i++) { var sample = trace[trace.Count - 1]; Assert.That(sample.State, Is.EqualTo(state)); }
            Assert.That(trace.Time, Is.EqualTo(time)); Assert.That(p.Context.Motor.Velocity, Is.EqualTo(velocity));
            p.Pause(); p.Tick(2); p.FixedTick(2); Assert.That(trace.Time, Is.EqualTo(time));
            p.Resume(); Keys(); Land(p);
            Assert.That(trace.HasJump && trace.JumpComplete, Is.True); Assert.That(trace.JumpRise, Is.GreaterThan(0));
            Assert.That(trace.JumpEndState, Is.EqualTo("fall"));
            p.Stop(); Assert.That(trace.Count, Is.Zero); Assert.That(trace.HasJump, Is.False);
        }
        [Test]
        public void MotionTraceRingIsBoundedAndIndependentAcrossHosts()
        {
            var a = Character(new Vector2(-3, .9f)); var b = Character(new Vector2(3, .9f));
            Keys(); for (int n = 0; n < 520; n++) Decide(a);
            Assert.That(a.Context.MotionTrace.Count, Is.EqualTo(PlayerMotionTrace.CAPACITY));
            Assert.That(b.Context.MotionTrace.Count, Is.Zero);
            var trace = a.Context.MotionTrace;
            Assert.That(trace[0].Time, Is.EqualTo(.16).Within(.00001));
            Assert.That(trace[511].Time, Is.EqualTo(10.38).Within(.0001));
            a.Stop(); Assert.That(trace.Count, Is.Zero);
        }
    }
}
#endif
