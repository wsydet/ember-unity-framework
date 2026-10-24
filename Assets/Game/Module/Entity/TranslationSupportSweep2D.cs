using System.Collections.Generic;
using UnityEngine;

namespace Game.Entity
{
    /// <summary>承载刚体的直线步速度约束。逐次投影阻挡法向，每次从真实起点重扫；不移动 Transform。</summary>
    internal sealed class TranslationSupportSweep2D
    {
        #region 内部参数
        private readonly List<Collider2D> _shapes = new(4);
        private readonly List<RaycastHit2D> _hits = new(16);
        private const int MAX_PASSES = 12;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        internal bool Blocks(Collider2D shape, RaycastHit2D hit, Vector2 direction, float skin)
        {
            var other = hit.collider;
            if (other == null || other.isTrigger || other.attachedRigidbody == shape.attachedRigidbody ||
                Physics2D.GetIgnoreLayerCollision(shape.gameObject.layer, other.gameObject.layer) ||
                EntityCollisionIgnoreLease2D.IsIgnored(shape, other) || Vector2.Dot(hit.normal, direction) >= -.00001f) return false;
            if (!other.usedByEffector) return true;
            var effector = other.GetComponent<PlatformEffector2D>();
            if (effector == null && other.attachedRigidbody != null)
                effector = other.attachedRigidbody.GetComponent<PlatformEffector2D>();
            if (effector == null || !effector.enabled || !effector.useOneWay) return true;
            if (effector.useColliderMask && (effector.colliderMask & (1 << shape.gameObject.layer)) == 0) return false;
            var up = (Vector2)(effector.transform.rotation * Quaternion.Euler(0, 0, effector.rotationalOffset) * Vector3.up);
            if (Vector2.Dot(direction, up) >= -.00001f ||
                Vector2.Dot(hit.normal, up) < Mathf.Cos(effector.surfaceArc * .5f * Mathf.Deg2Rad)) return false;
            // Cast 不运行 Effector 规则。起点已穿入时不能在盒内被反向假法线捕获。
            var separation = shape.Distance(other);
            return !separation.isOverlapped && Vector2.Dot(separation.pointA - separation.pointB, up) >= -skin;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal Vector2 Constrain(Rigidbody2D body, Vector2 velocity, float dt, LayerMask layers, float skin, List<Collider2D> riders)
        {
            body.GetAttachedColliders(_shapes);
            var displacement = velocity * dt;
            var filter = new ContactFilter2D { useTriggers = false }; filter.SetLayerMask(layers);
            for (int pass = 0; pass < MAX_PASSES; pass++)
            {
                float length = displacement.magnitude;
                if (length < .0000001f) return Vector2.zero;
                var direction = displacement / length;
                float greatest = 0; Vector2 normal = Vector2.zero;
                foreach (var shape in _shapes)
                {
                    if (shape == null || !shape.enabled || shape.isTrigger || !shape.gameObject.activeInHierarchy) continue;
                    shape.Cast(direction, filter, _hits, length + skin, true);
                    foreach (var hit in _hits)
                    {
                        if (riders.Contains(hit.collider) || !Blocks(shape, hit, direction, skin)) continue;
                        float approach = -Vector2.Dot(direction, hit.normal);
                        float correction = (length - Mathf.Max(0, hit.distance - skin)) * approach;
                        if (correction <= greatest + .0000001f) continue;
                        greatest = correction; normal = hit.normal;
                    }
                }
                if (greatest <= .0000001f) return displacement / dt;
                displacement += normal * greatest;
            }
            // 无法收敛的复杂夹角保守停止，不能提交未验证路径。
            return Vector2.zero;
        }
        #endregion
    }
}
