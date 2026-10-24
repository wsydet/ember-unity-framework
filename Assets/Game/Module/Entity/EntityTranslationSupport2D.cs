using System;
using UnityEngine;
using System.Collections.Generic;

namespace Game.Entity
{
    public enum SupportDepartureVelocity { None, Horizontal, Full }

    /// <summary>二维平移支撑适配。平台运动由调用方设置；玩家不写平台刚体。</summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D))]
    public sealed class EntityTranslationSupport2D : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private SupportDepartureVelocity _departure = SupportDepartureVelocity.Horizontal;
        [SerializeField, Min(.01f)] private float _teleportDistance = .5f;
        [SerializeField] private bool _constrainMotion;
        [SerializeField] private LayerMask _motionLayers = ~0;
        [SerializeField, Min(.001f)] private float _motionSkin = .025f;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private Rigidbody2D _body;
        private readonly List<RaycastHit2D> _riderHits = new(8);
        private readonly TranslationSupportSweep2D _sweep = new();
        private readonly List<Collider2D> _riders = new(2);
        private TranslationSupportLaunchTarget2D _launch;
        public Vector2 TargetVelocity { get; private set; }
        public bool ConstrainsMotion => _constrainMotion;
        public Rigidbody2D Body => _body != null ? _body : (_body = GetComponent<Rigidbody2D>());
        public Vector2 Velocity => Body.linearVelocity;
        public SupportDepartureVelocity Departure => _departure;
        public float TeleportDistance => _teleportDistance;
        public uint Revision { get; private set; }
        public bool IsAvailable => isActiveAndEnabled && Body.simulated && Body.bodyType == RigidbodyType2D.Kinematic;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void OnEnable() => Revision++;
        private void OnDisable()
        {
            Revision++;
            _riders.Clear();
            GetComponent<TranslationSupportLaunchTarget2D>()?.CancelLaunch();
            Body.linearVelocity = TargetVelocity = Vector2.zero;
        }
        private void FixedUpdate() => PrepareStep(Time.fixedDeltaTime);
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>显式启用载具自身扫掠；默认关闭，保留旧支撑的直接速度语义。</summary>
        public void ConfigureMotion(bool enabled, LayerMask layers, float skin = .025f)
        {
            if (!float.IsFinite(skin) || skin <= 0) throw new ArgumentOutOfRangeException(nameof(skin));
            _constrainMotion = enabled; _motionLayers = layers; _motionSkin = skin;
        }
        /// <summary>正式固定步阶段：宿主先发令，再结算弹射与自身碰撞。自动调用；隔离物理测试可显式驱动一次。</summary>
        public void PrepareStep(float dt)
        {
            if (!float.IsFinite(dt) || dt <= 0) throw new ArgumentOutOfRangeException(nameof(dt));
            if (!IsAvailable) return;
            _launch = GetComponent<TranslationSupportLaunchTarget2D>();
            if (_launch != null && _launch.isActiveAndEnabled) _launch.AdvanceStep(dt);
            TargetVelocity = Velocity;
            if (_constrainMotion) Body.linearVelocity = _sweep.Constrain(Body, Velocity, dt, _motionLayers, _motionSkin, _riders);
        }
        public bool Owns(Collider2D collider) => collider != null && collider.attachedRigidbody == Body;
        public void SetVelocity(Vector2 velocity)
        {
            if (!IsAvailable || !float.IsFinite(velocity.x) || !float.IsFinite(velocity.y))
                throw new InvalidOperationException("平移支撑要求启用的 Kinematic 刚体及有限速度。");
            Body.linearVelocity = velocity;
        }
        /// <summary>在本步物理前按乘员头顶净空收窄上升速度，避免挤压乘员穿过天花板。</summary>
        public void ConstrainForRider(Collider2D rider, float dt)
        {
            if (rider != null && !_riders.Contains(rider)) _riders.Add(rider);
            if (!IsAvailable || rider == null || !float.IsFinite(dt) || dt <= 0 || Velocity.y <= 0) return;
            const float skin = .02f;
            float distance = Velocity.y * dt;
            rider.Cast(Vector2.up, new ContactFilter2D { useTriggers = false }, _riderHits, distance + skin, true);
            foreach (var hit in _riderHits)
            {
                if (hit.collider == null || Owns(hit.collider) || hit.normal.y >= -.01f ||
                    !_sweep.Blocks(rider, hit, Vector2.up, skin)) continue;
                distance = Mathf.Min(distance, Mathf.Max(0, hit.distance - skin));
            }
            var velocity = Velocity; velocity.y = Mathf.Min(velocity.y, distance / dt);
            // 净空改变方向后重新扫掠，防止收窄 Y 后的直线路径撞到原斜向路径未经过的侧障碍。
            Body.linearVelocity = _constrainMotion ? _sweep.Constrain(Body, velocity, dt, _motionLayers, _motionSkin, _riders) : velocity;
        }
        /// <summary>乘员离台或会话退出时移除其自身碰撞排除，不修改 Unity 碰撞对。</summary>
        public void ReleaseRider(Collider2D rider) => _riders.Remove(rider);
        /// <summary>瞬移/复位使旧承载立即失效，不把位置差作为运动速度继承。</summary>
        public void Teleport(Vector2 position)
        {
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y)) throw new ArgumentException("支撑位置必须有限。");
            GetComponent<TranslationSupportLaunchTarget2D>()?.CancelLaunch();
            _riders.Clear();
            Revision++; Body.linearVelocity = TargetVelocity = Vector2.zero; Body.position = position;
        }
        #endregion
    }
}
