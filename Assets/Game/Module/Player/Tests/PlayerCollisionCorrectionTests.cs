using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private PlayerController CorrectionCharacter(Vector2? position = null)
        {
            var player = Character(position);
            // 正式 PlayerBlock 使用连续检测；离散检测会在首次高速碰撞步产生可观的穿入。
            player.Context.Motor.Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            return player;
        }
        private PlayerCorrectionSettings EnableCorrection(string field, float maximum = .2f)
        {
            var settings = new PlayerCorrectionSettings();
            SetAirField(settings, "_enabled", true); SetAirField(settings, "_maximumDistance", maximum);
            SetAirField(_config.Feel.Movement, field, settings);
            return settings;
        }
        private BoxCollider2D CorrectionObstacle(Vector2 center, Vector2 size)
        {
            var go = Keep(new GameObject("Correction obstacle"));
            SceneManager.MoveGameObjectToScene(go, _scene); go.transform.position = center;
            var box = go.AddComponent<BoxCollider2D>(); box.size = size;
            Physics2D.SyncTransforms(); return box;
        }

        [TestCase(false)] [TestCase(true)]
        public void CornerCorrection_JumpClearsSmallCeilingEdgeWithoutChangingLaunch(bool enabled)
        {
            if (enabled) EnableCorrection("_jumpCorrection");
            var player = CorrectionCharacter(); Land(player); Keys(Key.Space); Decide(player);
            Place(player, new Vector2(0, 3), 10);
            var ceiling = CorrectionObstacle(new Vector2(.9f, 4.1f), new Vector2(1, .2f));
            Decide(player);
            Assert.That(player.Context.Motor.Position.x, enabled ? Is.LessThan(-.1f) : Is.EqualTo(0));
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(10));
            Assert.That(player.Context.Sensors.Shape.Distance(ceiling).isOverlapped, Is.False, "补正提交后不能产生重叠");
            _scene.GetPhysicsScene2D().Simulate(Step);
            Assert.That(player.Context.Sensors.Shape.Distance(ceiling).distance, Is.GreaterThanOrEqualTo(-Physics2D.defaultContactOffset), "连续碰撞接触误差必须限制在物理接触容差内");
        }

        [TestCase(false)] [TestCase(true)]
        public void CornerCorrection_AirDashBypassesEdgeButNotTallWall(bool tall)
        {
            InstallDash(); EnableCorrection("_airDashCorrection");
            var player = CorrectionCharacter(new Vector2(0, 4));
            var obstacle = CorrectionObstacle(new Vector2(1, tall ? 4 : 2.7f), new Vector2(.8f, tall ? 4 : 1.1f));
            Keys(Key.D, Key.LeftShift); Decide(player);
            Assert.That(player.Context.Correction.LastOffset.y, tall ? Is.EqualTo(0) : Is.GreaterThan(.1f));
            Assert.That(DashResource(player, false).RemainingUses, Is.EqualTo(1));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(16));
            Assert.That(player.Context.Sensors.Shape.Distance(obstacle).isOverlapped, Is.False, "补正提交后不能产生重叠");
            _scene.GetPhysicsScene2D().Simulate(Step);
            Assert.That(player.Context.Sensors.Shape.Distance(obstacle).distance, Is.GreaterThanOrEqualTo(-Physics2D.defaultContactOffset), "连续碰撞接触误差必须限制在物理接触容差内");
        }

        [TestCase(false)] [TestCase(true)]
        public void CornerCorrection_GroundMovementAndDashClimbSmallStep(bool dash)
        {
            if (dash) InstallDash();
            EnableCorrection(dash ? "_groundDashCorrection" : "_stepCorrection");
            var player = CorrectionCharacter(); Land(player);
            var step = CorrectionObstacle(new Vector2(1.04f, .075f), new Vector2(1, .15f));
            float before = player.Context.Motor.Position.y;
            Keys(dash ? new[] { Key.D, Key.LeftShift } : new[] { Key.D });
            Decide(player);
            Assert.That(player.Context.Motor.Position.y, Is.GreaterThan(before + .1f));
            Assert.That(player.Context.Correction.LastOffset.y, Is.LessThanOrEqualTo(.2f));
            Assert.That(player.Context.Sensors.Shape.Distance(step).isOverlapped, Is.False, "补正提交后不能产生重叠");
            _scene.GetPhysicsScene2D().Simulate(Step);
            Assert.That(player.Context.Sensors.Shape.Distance(step).distance, Is.GreaterThanOrEqualTo(-Physics2D.defaultContactOffset), "连续碰撞接触误差必须限制在物理接触容差内");
            Decide(player);
            if (dash) Assert.That(player.Context.Dash.IsActive, Is.True, "补正后不能被旧墙面事实中断");
        }

        [Test]
        public void CornerCorrection_RejectsBlockedSideAndDoesNotRunWhilePaused()
        {
            EnableCorrection("_jumpCorrection");
            var player = CorrectionCharacter(); Land(player); Keys(Key.Space); Decide(player);
            Place(player, new Vector2(0, 3), 10);
            CorrectionObstacle(new Vector2(.9f, 4.1f), new Vector2(1, .2f));
            CorrectionObstacle(new Vector2(-.7f, 3), new Vector2(.3f, 2));
            Decide(player); Assert.That(player.Context.Correction.LastOffset, Is.EqualTo(Vector2.zero));
            player.Pause(); var position = player.Context.Motor.Position;
            Decide(player); Assert.That(player.Context.Motor.Position, Is.EqualTo(position));
        }

        [Test]
        public void CornerCorrection_ConfigurationIsValidatedAndFrozen()
        {
            var settings = EnableCorrection("_jumpCorrection");
            var player = CorrectionCharacter();
            SetAirField(settings, "_maximumDistance", .5f);
            Assert.That(player.Context.Feel.JumpCorrection.MaximumDistance, Is.EqualTo(.2f));
            SetAirField(settings, "_increment", .0001f);
            Assert.Throws<InvalidOperationException>(() => _config.Feel.Capture(Physics2D.gravity));
            SetAirField(settings, "_enabled", false);
            Assert.DoesNotThrow(() => _config.Feel.Capture(Physics2D.gravity));
        }

        [TestCase(PlayerCorrectionDirection.PositiveOnly, false)]
        [TestCase(PlayerCorrectionDirection.NegativeOnly, true)]
        public void CornerCorrection_AirDashRespectsDirectionRestriction(PlayerCorrectionDirection direction, bool corrected)
        {
            InstallDash(); var settings = EnableCorrection("_airDashCorrection");
            SetAirField(settings, "_direction", direction);
            var player = CorrectionCharacter(new Vector2(0, 4));
            CorrectionObstacle(new Vector2(1, 5.3f), new Vector2(.8f, 1.1f));
            Keys(Key.D, Key.LeftShift); Decide(player);
            Assert.That(player.Context.Correction.LastOffset.y, corrected ? Is.LessThan(-.1f) : Is.EqualTo(0));
        }

        [TestCase(true)] [TestCase(false)]
        public void CornerCorrection_AirDashRequiresEnabledObstacleLayerAndSolidTarget(bool oneWay)
        {
            InstallDash(); EnableCorrection("_airDashCorrection");
            var player = CorrectionCharacter(new Vector2(0, 4));
            var obstacle = CorrectionObstacle(new Vector2(1, 2.7f), new Vector2(.8f, 1.1f));
            if (oneWay)
            {
                obstacle.gameObject.AddComponent<PlatformEffector2D>().useOneWay = true;
                obstacle.usedByEffector = true;
            }
            else obstacle.gameObject.layer = 2;
            Physics2D.SyncTransforms();
            Keys(Key.D, Key.LeftShift); Decide(player);
            Assert.That(player.Context.Correction.LastOffset, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void CornerCorrection_ObstacleLayerDoesNotHideBlockedCorrectionPath()
        {
            InstallDash(); EnableCorrection("_airDashCorrection");
            var player = CorrectionCharacter(new Vector2(0, 4));
            CorrectionObstacle(new Vector2(1, 2.7f), new Vector2(.8f, 1.1f));
            var roof = CorrectionObstacle(new Vector2(0, 5.025f), new Vector2(1, .1f));
            roof.gameObject.layer = 2; Physics2D.SyncTransforms();
            Keys(Key.D, Key.LeftShift); Decide(player);
            Assert.That(player.Context.Correction.LastOffset, Is.EqualTo(Vector2.zero));
        }
    }
}
