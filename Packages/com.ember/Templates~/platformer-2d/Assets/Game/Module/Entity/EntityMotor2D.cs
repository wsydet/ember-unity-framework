using System;
using UnityEngine;
using System.Collections.Generic;

namespace Game.Entity
{
    /// <summary>只执行物理指令；由角色宿主统一调用，不自行 Tick。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D))]
    public sealed class EntityMotor2D : MonoBehaviour
    {
        #region 内部参数
        private Rigidbody2D _body;
        private bool _frozen;
        private bool _restoreSimulation;
        private Vector2 _savedVelocity;
        private float _savedAngularVelocity;
        private Vector2 _command;
        private float _gravityCommand;
        private readonly List<RaycastHit2D> _motionHits = new(8);
        public Vector2 CommandVelocity => _command;
        public Rigidbody2D Body => _body != null ? _body : (_body = GetComponent<Rigidbody2D>());
        public bool IsFrozen => _frozen;
        public Vector2 Velocity => _frozen ? _savedVelocity : Body.linearVelocity;
        public Vector2 Position => Body.position;
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public void Freeze()
        {
            if (_frozen) return;
            _savedVelocity = Body.linearVelocity;
            _savedAngularVelocity = Body.angularVelocity;
            _restoreSimulation = Body.simulated;
            _frozen = true;
            Body.simulated = false;
        }

        public void Resume()
        {
            if (!_frozen) return;
            Body.simulated = _restoreSimulation;
            Body.linearVelocity = _savedVelocity;
            Body.angularVelocity = _savedAngularVelocity;
            _frozen = false;
        }

        public void Apply(Vector2 position, Vector2 velocity, float gravityScale)
        {
            if (!_frozen) throw new InvalidOperationException("应用进度前必须冻结角色物理。");
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y) ||
                !float.IsFinite(velocity.x) || !float.IsFinite(velocity.y) || !float.IsFinite(gravityScale))
                throw new ArgumentException("物理数据必须为有限值。");
            Body.position = position;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            Body.gravityScale = gravityScale;
            _savedVelocity = velocity;
            _savedAngularVelocity = 0;
            _command = velocity;
        }

        public void BeginStep() { _command = Velocity; _gravityCommand = Body.gravityScale; }
        public void SetHorizontalVelocity(float value) => _command.x = value;
        public void SetVerticalVelocity(float value) => _command.y = value;
        /// <summary>动作位移转换为本物理步速度；按刚体全部碰撞体扫掠并保留安全间距，不写 Transform。</summary>
        public bool SetSweptDisplacement(Vector2 displacement, float deltaTime, LayerMask layers, float skin, out Vector2 allowed)
        {
            if (!float.IsFinite(displacement.x) || !float.IsFinite(displacement.y) ||
                !float.IsFinite(deltaTime) || deltaTime <= 0 || !float.IsFinite(skin) || skin <= 0)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            allowed = Vector2.zero;
            if (_frozen || !Body.simulated) return false;
            float distance = displacement.magnitude;
            var direction = distance > 0 ? displacement / distance : Vector2.zero;
            float travel = distance;
            var filter = new ContactFilter2D { useTriggers = false }; filter.SetLayerMask(layers);
            if (distance > 0)
            {
                Body.Cast(direction, filter, _motionHits, distance + skin);
                foreach (var hit in _motionHits)
                {
                    if (hit.rigidbody == Body || Vector2.Dot(hit.normal, direction) >= -.001f) continue;
                    travel = Mathf.Min(travel, Mathf.Max(0, hit.distance - skin));
                }
            }
            allowed = direction * travel;
            _command = allowed / deltaTime;
            _gravityCommand = 0;
            return travel >= distance - .00001f;
        }
        public void SetGravityScale(float value)
        {
            if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _gravityCommand = value;
        }
        public void ExecuteStep()
        {
            if (!_frozen) { Body.linearVelocity = _command; Body.gravityScale = _gravityCommand; }
        }
        #endregion
    }
}
