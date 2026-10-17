using UnityEngine;

namespace Game.Entity
{
    /// <summary>区域仅描述几何归属与稳定ID；安全、挑战和计时含义由项目配置。</summary>
    public sealed class EntityRegion2D : MonoBehaviour
    {
        #region 编辑器面板参数

        [SerializeField] private string _regionId;
        [SerializeField] private Collider2D _area;
        [SerializeField] private int _priority;

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        public string RegionId => _regionId;
        public int Priority => _priority;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public bool Contains(Vector2 point) => isActiveAndEnabled && _area != null && _area.isActiveAndEnabled && _area.OverlapPoint(point);

        #endregion
    }
}
