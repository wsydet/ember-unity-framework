using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private BoxCollider2D WallBox(string name, Vector2 position, Vector2 size)
        {
            var go = Keep(new GameObject(name));
            SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = position;
            var shape = go.AddComponent<BoxCollider2D>(); shape.size = size;
            return shape;
        }

        [Test]
        public void WallSensors_BothSidesHaveRealNormals_AndClearAfterLeaving()
        {
            var player = Character(new Vector2(0, 3));
            var left = WallBox("Left", new Vector2(-1.02f, 3), new Vector2(1, 4));
            var right = WallBox("Right", new Vector2(1.02f, 3), new Vector2(1, 4));
            Physics2D.SyncTransforms();
            var sensors = player.Context.Sensors; sensors.Sample();
            Assert.That(sensors.LeftWall, Is.EqualTo(left));
            Assert.That(sensors.RightWall, Is.EqualTo(right));
            Assert.That(sensors.LeftWallNormal.x, Is.GreaterThan(.99f));
            Assert.That(sensors.RightWallNormal.x, Is.LessThan(-.99f));
            right.transform.position += Vector3.right;
            left.enabled = false;
            Physics2D.SyncTransforms(); sensors.Sample();
            Assert.That(sensors.LeftWall, Is.Null);
            Assert.That(sensors.RightWall, Is.Null);
            Assert.That(sensors.LeftWallNormal, Is.EqualTo(Vector2.zero));
            Assert.That(sensors.RightWallNormal, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void WallSensors_FloorCeilingAndOverlapAreNotWalls()
        {
            var player = Character();
            WallBox("Ceiling", new Vector2(0, 2.28f), new Vector2(4, 1));
            Physics2D.SyncTransforms();
            var sensors = player.Context.Sensors; sensors.Sample();
            Assert.That(sensors.IsGrounded, Is.True);
            Assert.That(sensors.LeftWall, Is.Null);
            Assert.That(sensors.RightWall, Is.Null);
            WallBox("Overlapping", new Vector2(0, .9f), new Vector2(3, 3));
            Physics2D.SyncTransforms(); sensors.Sample();
            Assert.That(sensors.LeftWall, Is.Null);
            Assert.That(sensors.RightWall, Is.Null);
        }

        [Test]
        public void WallSensors_IgnoreOwnShapesTriggersAndExcludedLayers()
        {
            var player = Character(new Vector2(0, 3));
            var own = WallBox("Own", new Vector2(1.02f, 3), new Vector2(1, 3));
            own.transform.SetParent(player.transform, true);
            var trigger = WallBox("Trigger", new Vector2(-1.02f, 3), new Vector2(1, 3));
            trigger.isTrigger = true;
            Physics2D.SyncTransforms();
            var sensors = player.Context.Sensors; sensors.Sample();
            Assert.That(sensors.LeftWall, Is.Null); Assert.That(sensors.RightWall, Is.Null);
            trigger.isTrigger = false; trigger.gameObject.layer = 2;
            Physics2D.SyncTransforms(); sensors.Sample();
            Assert.That(sensors.LeftWall, Is.Null);
            sensors.ConfigureWalls(1 << 2); sensors.Sample();
            Assert.That(sensors.LeftWall, Is.EqualTo(trigger));
        }

        [Test]
        public void WallSensors_SelectNearest_AndClearWhenShapeDisabled()
        {
            var player = Character(new Vector2(0, 3));
            WallBox("Far", new Vector2(1.05f, 3), new Vector2(1, 3));
            var near = WallBox("Near", new Vector2(1.02f, 3), new Vector2(1, 3));
            Physics2D.SyncTransforms();
            var sensors = player.Context.Sensors; sensors.Sample();
            Assert.That(sensors.RightWall, Is.EqualTo(near));
            sensors.Shape.enabled = false; sensors.Sample();
            Assert.That(sensors.RightWall, Is.Null);
            Assert.That(sensors.RightWallNormal, Is.EqualTo(Vector2.zero));
            Assert.That(sensors.IsGrounded, Is.False);
        }

        [Test]
        public void WallSensors_RejectInvalidProbeConfiguration()
        {
            var sensors = Character().Context.Sensors;
            Assert.Throws<ArgumentOutOfRangeException>(() => sensors.ConfigureWalls(1, float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => sensors.ConfigureWalls(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => sensors.ConfigureWalls(1, .06f, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => sensors.ConfigureWalls(1, .06f, 1.1f));
        }

        [Test]
        public void VerticalInput_SamplesDirection_AndClearsWithoutConsumingJump()
        {
            var player = Character();
            Keys(Key.S, Key.Space); player.Tick(Step);
            Assert.That(player.Context.Input.Vertical, Is.EqualTo(-1));
            Assert.That(player.Context.Input.JumpRequested, Is.True);
            player.Context.Input.Sample();
            Assert.That(player.Context.Input.JumpRequested, Is.True);
            Keys(Key.W); player.Tick(Step);
            Assert.That(player.Context.Input.Vertical, Is.EqualTo(1));
            player.Context.Input.Clear();
            Assert.That(player.Context.Input.Vertical, Is.Zero);
            Assert.That(player.Context.Input.JumpRequested, Is.False);
            Keys(); player.Tick(Step);
            Assert.That(player.Context.Input.Vertical, Is.Zero);
        }
    }
}
