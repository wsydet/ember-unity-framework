using System;
using Game.Entity;
using Ember.Basic;
using UnityEngine;

namespace Game.Player
{
    /// <summary>以玩家根的物理位置选区；优先级高者优先，同优先级按稳定ID排序，传送后可立即重算。</summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerRegionTracker2D : MonoBehaviour
    {
        #region 编辑器面板参数

        [SerializeField] private EntityRegion2D[] _regions = Array.Empty<EntityRegion2D>();

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        public EntityRegion2D Current { get; private set; }
        public event Action<EntityRegion2D, EntityRegion2D> Changed;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void LateUpdate() => Reevaluate();
        private void OnDisable() => Set(null);

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void Set(EntityRegion2D region)
        {
            if (ReferenceEquals(Current, region)) return;
            var old = Current; Current = region;
            if (Changed != null)
                foreach (Action<EntityRegion2D, EntityRegion2D> callback in Changed.GetInvocationList())
                    try { callback(old, region); }
                    catch (Exception error) { EmberDebug.LogError("Game.Player", "区域变更通知失败：" + error); }
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public void Reevaluate()
        {
            var player = GetComponent<PlayerController>();
            if (!isActiveAndEnabled || player.Context == null) { Set(null); return; }
            EntityRegion2D selected = null;
            foreach (var region in _regions)
            {
                if (region == null || region.gameObject.scene != gameObject.scene || string.IsNullOrWhiteSpace(region.RegionId) ||
                    !region.Contains(player.Context.Motor.Position)) continue;
                if (selected == null || region.Priority > selected.Priority || region.Priority == selected.Priority &&
                    string.CompareOrdinal(region.RegionId, selected.RegionId) < 0) selected = region;
            }
            Set(selected);
        }

        #endregion
    }
}
