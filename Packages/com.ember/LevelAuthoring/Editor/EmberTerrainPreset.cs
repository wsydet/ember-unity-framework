using UnityEngine;

namespace Ember.LevelAuthoring.Editor
{
    /// <summary>矩形印章；放置后为普通 Tile 格子，不形成第二份占用地图。</summary>
    public sealed class EmberTerrainPreset : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField] private string _id;
        [SerializeField] private Vector2 _size = Vector2.one;
        [SerializeField] private Vector2Int _anchor;
        [SerializeField] private EmberTerrainTile _terrain;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public string Id => _id;
        public Vector2 Size => _size;
        public Vector2Int Anchor => _anchor;
        public EmberTerrainTile Terrain => _terrain;
        public void Configure(string id, Vector2 size, Vector2Int anchor, EmberTerrainTile terrain)
        {
            if (string.IsNullOrWhiteSpace(id) || terrain == null) throw new System.ArgumentException("需要稳定ID与地形样式。");
            // 宽高为世界单位；是否整除取决于目标Grid的实际缩放，放置时统一验证。
            if (!float.IsFinite(size.x) || !float.IsFinite(size.y) || size.x <= 0 || size.y <= 0)
                throw new System.ArgumentException("尺寸必须为正有限世界宽高。");
            _id = id; _size = size; _anchor = anchor; _terrain = terrain;
        }
        #endregion
    }
}
