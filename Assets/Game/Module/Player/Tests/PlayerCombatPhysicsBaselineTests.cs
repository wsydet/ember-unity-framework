using System.Collections;
using System.Collections.Generic;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    /// <summary>自动物理时序、跨步短窗口及同批换血；不替代正式入口验收。</summary>
    public sealed class PlayerCombatPhysicsBaselineTests
    {
        #region 内部方法
        private IEnumerator Observe(float step, int frameRate)
        {
            Assert.That(Physics2D.simulationMode, Is.EqualTo(SimulationMode2D.FixedUpdate));
            float previousStep = Time.fixedDeltaTime;
            int previousRate = Application.targetFrameRate;
            var scene = SceneManager.CreateScene("CombatPhysicsBaseline");
            var root = new GameObject("CombatPhysicsBaseline");
            SceneManager.MoveGameObjectToScene(root, scene);
            AsyncOperation unloading = null;
            try
            {
                Time.fixedDeltaTime = step;
                Application.targetFrameRate = frameRate;
                var motors = new EntityMotor2D[2];
                var shapes = new BoxCollider2D[2];
                for (int i = 0; i < motors.Length; i++)
                {
                    var actor = new GameObject("Probe " + i);
                    actor.transform.SetParent(root.transform);
                    actor.transform.position = new Vector3(1000 + i * 10, 1000);
                    var body = actor.AddComponent<Rigidbody2D>();
                    body.gravityScale = 0;
                    body.constraints = RigidbodyConstraints2D.FreezeRotation;
                    shapes[i] = actor.AddComponent<BoxCollider2D>();
                    motors[i] = actor.AddComponent<EntityMotor2D>();
                }
                var probe = root.AddComponent<PlayerCombatPhysicsProbe>();
                probe.Configure(motors, shapes);
                for (int frame = 0; frame < 120 && probe.Samples.Count < 6; frame++) yield return null;
                probe.enabled = false;
                Assert.That(probe.Samples.Count, Is.GreaterThanOrEqualTo(6));
                Assert.That(probe.Traded, Is.True, "短窗口跨完整固定步仍收集双方命中，再统一结算致死");
                for (int sample = 0; sample < probe.Samples.Count; sample++)
                {
                    var current = probe.Samples[sample];
                    for (int actor = 0; actor < 2; actor++)
                    {
                        Assert.That(current.AfterCommands[actor], Is.EqualTo(current.Before[actor]),
                            "Motor 写速度不能被误认为已完成物理积分");
                        Assert.That(current.ShapeContainsBody[actor], Is.True,
                            "统一同步后查询应观察本步收集使用的刚体姿态");
                        if (sample > 0)
                            Assert.That(current.Before[actor].x - probe.Samples[sample - 1].Before[actor].x,
                                Is.EqualTo(3 * step).Within(.0002f), "下一固定步应看见上一步自动积分");
                    }
                }
                Assert.That(Physics2D.simulationMode, Is.EqualTo(SimulationMode2D.FixedUpdate));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Time.fixedDeltaTime = previousStep;
                Application.targetFrameRate = previousRate;
                unloading = SceneManager.UnloadSceneAsync(scene);
            }
            yield return unloading;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [UnityTest] public IEnumerator AutomaticPhysicsAtTwentyMillisecondsThirtyFps() => Observe(.02f, 30);
        [UnityTest] public IEnumerator AutomaticPhysicsAtTenMillisecondsOneTwentyFps() => Observe(.01f, 120);
        #endregion
    }

    /// <summary>单一固定步驱动两个实体，采样后批量提交速度，再统一同步并收集查询。</summary>
    public sealed class PlayerCombatPhysicsProbe : MonoBehaviour
    {
        #region 内部参数
        private EntityMotor2D[] _motors;
        private BoxCollider2D[] _shapes;
        private EntityHitBatch _batch;
        private PlayerAttackHitSession[] _attacks;
        private EntityHealth[] _health;
        private double _clock;
        internal bool Traded;
        internal readonly List<Sample> Samples = new();
        internal sealed class Sample
        {
            internal readonly Vector2[] Before = new Vector2[2];
            internal readonly Vector2[] AfterCommands = new Vector2[2];
            internal readonly bool[] ShapeContainsBody = new bool[2];
        }
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void FixedUpdate()
        {
            if (_motors == null) return;
            var sample = new Sample();
            for (int i = 0; i < _motors.Length; i++)
            {
                sample.Before[i] = _motors[i].Position;
                _motors[i].BeginStep();
                _motors[i].SetHorizontalVelocity(3);
                _motors[i].SetVerticalVelocity(0);
                _motors[i].SetGravityScale(0);
                _motors[i].ExecuteStep();
            }
            Physics2D.SyncTransforms();
            for (int i = 0; i < _motors.Length; i++)
            {
                sample.AfterCommands[i] = _motors[i].Position;
                sample.ShapeContainsBody[i] = _shapes[i].OverlapPoint(sample.AfterCommands[i]);
            }
            Samples.Add(sample);
            for (int i = 0; i < _attacks.Length; i++)
                _attacks[i].Sample(_clock, _clock + Time.fixedDeltaTime, _motors[i].Position);
            if (Samples.Count == 1)
                Assert.That(_health[0].Current + _health[1].Current, Is.EqualTo(2), "查询不能提前结算一方致死");
            _batch.Resolve();
            Traded = _health[0].Current == 0 && _health[1].Current == 0;
            _clock += Time.fixedDeltaTime;
        }
        private void OnDestroy()
        {
            if (_attacks != null) foreach (var attack in _attacks) attack?.Dispose();
            _batch?.Dispose();
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal void Configure(EntityMotor2D[] motors, BoxCollider2D[] shapes)
        {
            _motors = motors; _shapes = shapes;
            _batch = new EntityHitBatch(); _attacks = new PlayerAttackHitSession[2]; _health = new EntityHealth[2];
            var segment = JsonUtility.FromJson<PlayerAttackSegment>("{\"_duration\":0.1,\"_windows\":[{\"_id\":\"short\",\"_start\":0.004,\"_end\":0.006,\"_layers\":-1,\"_payload\":{\"_category\":\"physical\",\"_damage\":1,\"_braking\":30},\"_keyframes\":[{\"_time\":0.004,\"_shapes\":[{\"_kind\":0,\"_size\":{\"x\":24,\"y\":2}}]}]}]}");
            segment.Windows[0].Validate(segment.Duration, "automatic timing");
            for (int i = 0; i < 2; i++)
            {
                _health[i] = new EntityHealth(1, 1);
                var entity = motors[i].gameObject.AddComponent<Entity2D>();
                var target = _batch.Register(entity, "probe-" + i, i + 1, new EntityHitReceiver(_health[i], null, null));
                _attacks[i] = new PlayerAttackHitSession(_batch, target);
                _attacks[i].Begin(segment, 1, motors[i].Position);
            }
        }
        #endregion
    }
}
