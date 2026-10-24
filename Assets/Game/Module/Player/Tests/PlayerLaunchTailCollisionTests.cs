using Game.Entity;
using NUnit.Framework;
using UnityEngine;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        [TestCase(false, -3)] [TestCase(false, 3)]
        [TestCase(true, -3)] [TestCase(true, 3)]
        public void LaunchTailCollision_CeilingWithWindHandsOffActualResult(bool side, float wind)
        {
            WallBox("Wind ceiling", new Vector2(0, 8), new Vector2(100, .5f));
            var p = Character(new Vector2(0, 5));
            p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest("ceiling-wind",
                PlayerExternalMotionMode.AddVelocity, Vector2.up * wind));
            var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
            target.TryLaunch(ExternalOwner(), TailRequest(side, duration: 2));
            for (int i = 0; i < 40 && target.IsLaunching; i++)
            {
                Simulate(p);
                TestContext.WriteLine($"P06 ceiling side={side} wind={wind} i={i} hold={target.IsLaunching} self={p.Context.ExternalMotion.SelfVelocity.y:R} world={p.Context.Motor.Velocity.y:R}");
            }
            Assert.That(target.IsLaunching, Is.False, "反向风扣除后的正自身速度不能掩盖真实碰顶");
            Assert.That(p.Context.VerticalMotion.LaunchTail?.IsActive, Is.True);
            for (int i = 0; i < 5; i++)
            {
                float actualSelf = p.Context.ExternalMotion.SelfVelocity.y;
                Decide(p);
                Assert.That(p.Context.Motor.Velocity.y - wind,
                    Is.EqualTo(Mathf.Max(actualSelf, -20)).Within(.001), "不重发保持速度；环境只合成一次");
                Assert.That(p.Context.Motor.Body.gravityScale, Is.GreaterThan(0));
                _scene.GetPhysicsScene2D().Simulate(Step);
                Assert.That(p.Context.Motor.Position.y, Is.LessThan(7));
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void LaunchTailCollision_OpposingWindWithoutCeilingDoesNotEndHold(bool side)
        {
            var p = Character(new Vector2(0, 100));
            p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest("opposing",
                PlayerExternalMotionMode.AddVelocity, Vector2.down * 30));
            var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
            target.TryLaunch(ExternalOwner(), TailRequest(side, duration: .1f));
            for (int i = 0; i < 5; i++)
            {
                Assert.That(target.IsLaunching, Is.True);
                Simulate(p);
                Assert.That(p.Context.ExternalMotion.SelfVelocity.y, Is.EqualTo(side ? 10 : 20).Within(.001));
                Assert.That(p.Context.Motor.Velocity.y, Is.LessThan(0), "仅世界合速度下降不是碰顶");
            }
            Assert.That(target.IsLaunching, Is.False);
            Assert.That(p.Context.VerticalMotion.LaunchTail?.IsActive, Is.True);
        }
    }
}
