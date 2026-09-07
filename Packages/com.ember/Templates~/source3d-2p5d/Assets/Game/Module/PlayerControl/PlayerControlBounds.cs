using Ember.Basic;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Module
{
    /// <summary>把 LookAt 限制在一组 BoxCollider 共同定义的可移动区域内。</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerControlBounds : MonoBehaviour
    {
        private const string INSPECTOR_GROUP = "Player Control Bounds";
        private const float REGION_CONTACT_WORLD_EPSILON = 0.0001f;

        #region 编辑器面板参数

        [FoldoutGroup("$INSPECTOR_GROUP", Expanded = true)]
        [BoxGroup("$INSPECTOR_GROUP/边界", ShowLabel = false)]
        [Title("移动边界", "启用的 BoxCollider 共同组成允许区域；场景围栏只负责可视化。")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        [Required, LabelText("允许区域")]
        [SerializeField] private List<BoxCollider> _allowedRegions = new();

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void Awake()
        {
            ResolveDefaultRegion();
        }

        private void Reset()
        {
            _allowedRegions = new List<BoxCollider>();
            ResolveDefaultRegion();
        }

        private void OnValidate()
        {
            if (_allowedRegions == null)
                _allowedRegions = new List<BoxCollider>();
            else
                _allowedRegions.RemoveAll(region => region == null);

            ResolveDefaultRegion();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void ResolveDefaultRegion()
        {
            if (_allowedRegions == null)
                _allowedRegions = new List<BoxCollider>();

            if (_allowedRegions.Count > 0)
                return;

            BoxCollider defaultRegion = GetComponent<BoxCollider>();
            if (defaultRegion != null)
                _allowedRegions.Add(defaultRegion);
        }

        private static bool IsRegionActive(BoxCollider region)
        {
            return region != null
                   && region.enabled
                   && region.gameObject.activeInHierarchy;
        }

        private static bool ContainsHorizontalPosition(
            BoxCollider region,
            Vector3 worldPosition,
            float worldTolerance)
        {
            Transform regionTransform = region.transform;
            Vector3 localPosition = regionTransform.InverseTransformPoint(worldPosition);
            Vector3 center = region.center;
            Vector3 halfSize = region.size * 0.5f;
            float localToleranceX = worldTolerance / Mathf.Max(
                regionTransform.TransformVector(Vector3.right).magnitude,
                Mathf.Epsilon);
            float localToleranceZ = worldTolerance / Mathf.Max(
                regionTransform.TransformVector(Vector3.forward).magnitude,
                Mathf.Epsilon);

            return localPosition.x >= center.x - halfSize.x - localToleranceX
                   && localPosition.x <= center.x + halfSize.x + localToleranceX
                   && localPosition.z >= center.z - halfSize.z - localToleranceZ
                   && localPosition.z <= center.z + halfSize.z + localToleranceZ;
        }

        private static void GetRegionCandidate(
            BoxCollider region,
            Vector3 worldPosition,
            out Vector3 candidate,
            out bool containsPosition)
        {
            Transform regionTransform = region.transform;
            Vector3 localPosition = regionTransform.InverseTransformPoint(worldPosition);
            Vector3 center = region.center;
            Vector3 halfSize = region.size * 0.5f;

            float minX = center.x - halfSize.x;
            float maxX = center.x + halfSize.x;
            float minZ = center.z - halfSize.z;
            float maxZ = center.z + halfSize.z;
            containsPosition = localPosition.x >= minX
                               && localPosition.x <= maxX
                               && localPosition.z >= minZ
                               && localPosition.z <= maxZ;

            localPosition.x = Mathf.Clamp(localPosition.x, minX, maxX);
            localPosition.z = Mathf.Clamp(localPosition.z, minZ, maxZ);
            candidate = regionTransform.TransformPoint(localPosition);
            candidate.y = worldPosition.y;
        }

        private bool TryGetMovementStep(
            Vector3 fromWorldPosition,
            Vector3 targetWorldPosition,
            out Vector3 stepPosition,
            out bool reachedTarget)
        {
            stepPosition = fromWorldPosition;
            reachedTarget = false;
            float nearestSqrDistance = float.PositiveInfinity;
            bool foundReachableRegion = false;

            for (int i = 0; i < _allowedRegions.Count; i++)
            {
                BoxCollider region = _allowedRegions[i];
                if (!IsRegionActive(region)
                    || !ContainsHorizontalPosition(
                        region,
                        fromWorldPosition,
                        REGION_CONTACT_WORLD_EPSILON))
                {
                    continue;
                }

                foundReachableRegion = true;
                GetRegionCandidate(
                    region,
                    targetWorldPosition,
                    out Vector3 candidate,
                    out bool containsTarget);
                if (containsTarget)
                {
                    stepPosition = targetWorldPosition;
                    reachedTarget = true;
                    return true;
                }

                Vector2 horizontalDelta = new(
                    candidate.x - targetWorldPosition.x,
                    candidate.z - targetWorldPosition.z);
                float sqrDistance = horizontalDelta.sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance)
                    continue;

                nearestSqrDistance = sqrDistance;
                stepPosition = candidate;
            }

            return foundReachableRegion;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public IReadOnlyList<BoxCollider> AllowedRegions => _allowedRegions;

        /// <summary>
        /// 将世界坐标限制到所有已启用区域的并集。坐标位于任一区域内时保持不变；
        /// 位于区域外时选择水平距离最近的区域边缘，并保留调用方传入的世界 Y。
        /// 没有启用区域时返回 false，由调用方保持当前位置。
        /// </summary>
        [NoGC]
        public bool TryClampPosition(Vector3 worldPosition, out Vector3 clampedPosition)
        {
            clampedPosition = worldPosition;
            float nearestSqrDistance = float.PositiveInfinity;
            bool foundActiveRegion = false;

            for (int i = 0; i < _allowedRegions.Count; i++)
            {
                BoxCollider region = _allowedRegions[i];
                if (!IsRegionActive(region))
                    continue;

                foundActiveRegion = true;
                GetRegionCandidate(
                    region,
                    worldPosition,
                    out Vector3 candidate,
                    out bool containsPosition);
                if (containsPosition)
                {
                    clampedPosition = worldPosition;
                    return true;
                }

                Vector2 horizontalDelta = new(
                    candidate.x - worldPosition.x,
                    candidate.z - worldPosition.z);
                float sqrDistance = horizontalDelta.sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance)
                    continue;

                nearestSqrDistance = sqrDistance;
                clampedPosition = candidate;
            }

            return foundActiveRegion;
        }

        /// <summary>
        /// 把一次移动限制在当前位置能够连续进入的区域内。
        /// 每一步只使用包含当前中间点的启用区域生成目标候选，因此不会跨越不连通区域之间的空隙；
        /// 候选进入重叠或接触区域后会继续约束剩余移动，使大步长也能穿过合法接缝。
        /// 起点不在任何启用区域时返回 false，由调用方单独执行位置恢复。
        /// </summary>
        [NoGC]
        public bool TryConstrainMovement(
            Vector3 fromWorldPosition,
            Vector3 targetWorldPosition,
            out Vector3 constrainedPosition)
        {
            constrainedPosition = fromWorldPosition;
            bool foundReachableRegion = false;

            // 每次产生有效进展都至少会抵达一个区域对目标点的投影位置；
            // 区域数量就是穿过重叠链所需步骤的安全上限。
            for (int step = 0; step < _allowedRegions.Count; step++)
            {
                Vector3 previousPosition = constrainedPosition;
                if (!TryGetMovementStep(
                        previousPosition,
                        targetWorldPosition,
                        out constrainedPosition,
                        out bool reachedTarget))
                {
                    return foundReachableRegion;
                }

                foundReachableRegion = true;
                if (reachedTarget)
                    return true;

                Vector2 stepDelta = new(
                    constrainedPosition.x - previousPosition.x,
                    constrainedPosition.z - previousPosition.z);
                if (stepDelta.sqrMagnitude
                    <= REGION_CONTACT_WORLD_EPSILON * REGION_CONTACT_WORLD_EPSILON)
                {
                    return true;
                }
            }

            return foundReachableRegion;
        }

        /// <summary>把区域加入允许列表，并设置它当前是否解锁。</summary>
        public bool AddRegion(BoxCollider region, bool unlocked = true)
        {
            if (region == null || _allowedRegions.Contains(region))
                return false;

            _allowedRegions.Add(region);
            region.enabled = unlocked;
            return true;
        }

        /// <summary>从允许列表移除区域，不销毁区域对象。</summary>
        public bool RemoveRegion(BoxCollider region)
        {
            return region != null && _allowedRegions.Remove(region);
        }

        /// <summary>通过 Collider.enabled 切换已登记区域的解锁状态。</summary>
        public bool SetRegionUnlocked(BoxCollider region, bool unlocked)
        {
            if (region == null || !_allowedRegions.Contains(region))
                return false;

            region.enabled = unlocked;
            return true;
        }

        #endregion
    }
}
