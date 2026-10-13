using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Entity
{
    [DisallowMultipleComponent]
    public sealed class EntitySensors2D : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private Collider2D _shape;
        [SerializeField] private LayerMask _groundLayers = 1;
        [SerializeField, Min(0.001f)] private float _groundDistance = 0.06f;
        [SerializeField, Range(0, 1)] private float _minimumGroundNormal = 0.65f;
        [SerializeField] private LayerMask _wallLayers = 1;
        [SerializeField, Min(0.001f)] private float _wallDistance = 0.06f;
        [SerializeField, Range(0.01f, 1)] private float _minimumWallNormal = 0.9f;
        #endregion

        // --------------------------------------------------------
        #region 内部参数
        private Entity2D _entity;
        private readonly List<RaycastHit2D> _hits = new(8);
        public bool IsGrounded { get; private set; }
        public Collider2D Ground { get; private set; }
        /// <summary>实际接触并提供支撑的单向平台；不是单纯 Cast 穿过的表面。</summary>
        public Collider2D OneWayGround { get; private set; }
        public Collider2D Shape => _shape;
        public Collider2D LeftWall { get; private set; }
        public Collider2D RightWall { get; private set; }
        public Vector2 LeftWallNormal { get; private set; }
        public Vector2 RightWallNormal { get; private set; }
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private static PlatformEffector2D OneWayEffector(Collider2D collider)
        {
            if (collider == null || !collider.usedByEffector) return null;
            var effector = collider.GetComponent<PlatformEffector2D>();
            if (effector == null && collider.attachedRigidbody != null)
                effector = collider.attachedRigidbody.GetComponent<PlatformEffector2D>();
            return effector != null && effector.enabled && effector.useOneWay ? effector : null;
        }
        private bool Detectable(Collider2D collider)
        {
            if (collider == null || _entity.Owns(collider) || EntityCollisionIgnoreLease2D.IsIgnored(_shape, collider)) return false;
            // Cast 不执行 Effector 单向规则；穿过平台的几何命中不能冒充支撑/墙面。
            // 单向体采用上一次物理步的真实接触，普通实体地面仍保留原预测探测距离。
            return OneWayEffector(collider) == null || _shape.IsTouching(collider);
        }
        private Collider2D SampleWall(Vector2 direction, out Vector2 normal)
        {
            normal = Vector2.zero;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(_wallLayers);
            _shape.Cast(direction, filter, _hits, _wallDistance, true);
            Collider2D nearest = null;
            float distance = float.PositiveInfinity;
            foreach (var hit in _hits)
            {
                if (!Detectable(hit.collider) ||
                    Vector2.Dot(hit.normal, -direction) < _minimumWallNormal || hit.distance >= distance) continue;
                // 初始重叠时 Cast 的反向法线并非真实墙面法线；不把卡入几何体当作有效墙面。
                if (hit.distance == 0 && _shape.Distance(hit.collider).isOverlapped) continue;
                nearest = hit.collider; distance = hit.distance; normal = hit.normal;
            }
            return nearest;
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public void Configure(Collider2D shape, LayerMask groundLayers)
        { _shape = shape; _groundLayers = groundLayers; }

        public void ConfigureWalls(LayerMask layers, float distance = 0.06f, float minimumNormal = 0.9f)
        {
            if (!float.IsFinite(distance) || distance <= 0 || !float.IsFinite(minimumNormal) || minimumNormal <= 0 || minimumNormal > 1)
                throw new ArgumentOutOfRangeException(nameof(distance), "墙面检测距离须为正数，法线阈值须在 (0,1]。");
            _wallLayers = layers; _wallDistance = distance; _minimumWallNormal = minimumNormal;
        }

        public void Bind(Entity2D entity)
        {
            _entity = entity;
            if (_shape == null || entity == null || !entity.Owns(_shape))
                throw new InvalidOperationException("Sensors 必须配置属于本实体的碰撞体。");
        }

        public void Sample()
        {
            Ground = null;
            OneWayGround = null;
            IsGrounded = false;
            LeftWall = null; RightWall = null;
            LeftWallNormal = Vector2.zero; RightWallNormal = Vector2.zero;
            if (_shape == null || !_shape.enabled || !_shape.gameObject.activeInHierarchy) return;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(_groundLayers);
            _shape.Cast(Vector2.down, filter, _hits, _groundDistance, true);
            foreach (var hit in _hits)
            {
                if (!Detectable(hit.collider) || hit.normal.y < _minimumGroundNormal) continue;
                Ground = hit.collider;
                if (OneWayEffector(Ground) != null) OneWayGround = Ground;
                IsGrounded = true;
                break;
            }
            LeftWall = SampleWall(Vector2.left, out var leftNormal);
            RightWall = SampleWall(Vector2.right, out var rightNormal);
            LeftWallNormal = leftNormal; RightWallNormal = rightNormal;
        }
        #endregion
    }
}
