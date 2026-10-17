using System;
using UnityEngine;
using System.Collections.Generic;

namespace Game.Entity
{
    public enum SupportDepartureVelocity { None, Horizontal, Full }

    /// <summary>二维平移支撑适配。平台运动由调用方设置；玩家不写平台刚体。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D))]
    public sealed class EntityTranslationSupport2D : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private SupportDepartureVelocity _departure = SupportDepartureVelocity.Horizontal;
        [SerializeField, Min(.01f)] private float _teleportDistance = .5f;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private Rigidbody2D _body;
        private readonly List<RaycastHit2D> _riderHits = new(8);
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
        private void OnDisable() => Revision++;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
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
            if (!IsAvailable || rider == null || !float.IsFinite(dt) || dt <= 0 || Velocity.y <= 0) return;
            const float skin = .02f;
            float distance = Velocity.y * dt;
            rider.Cast(Vector2.up, new ContactFilter2D { useTriggers = false }, _riderHits, distance + skin, true);
            foreach (var hit in _riderHits)
            {
                if (hit.collider == null || Owns(hit.collider) || hit.normal.y >= -.01f ||
                    EntityCollisionIgnoreLease2D.IsIgnored(rider, hit.collider) ||
                    Physics2D.GetIgnoreLayerCollision(rider.gameObject.layer, hit.collider.gameObject.layer)) continue;
                distance = Mathf.Min(distance, Mathf.Max(0, hit.distance - skin));
            }
            var velocity = Velocity; velocity.y = Mathf.Min(velocity.y, distance / dt);
            Body.linearVelocity = velocity;
        }
        /// <summary>瞬移/复位使旧承载立即失效，不把位置差作为运动速度继承。</summary>
        public void Teleport(Vector2 position)
        {
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y)) throw new ArgumentException("支撑位置必须有限。");
            Revision++; Body.linearVelocity = Vector2.zero; Body.position = position;
        }
        #endregion
    }
}
