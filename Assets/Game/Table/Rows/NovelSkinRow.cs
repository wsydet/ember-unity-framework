using Ember.Table;

namespace Game.Table
{
    /// <summary>皮肤清单。一套皮肤是若干覆盖素材的集合；没有任何覆盖项的皮肤等价于沿用 Prefab 外观。</summary>
    [EmberTable("novel_skins")]
    public sealed class NovelSkinRow
    {
        #region 内部参数
        [EmberTableKey, EmberTableColumn("skinId")]
        public string SkinId { get; }
        [EmberTableColumn("displayName")]
        public string DisplayName { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [EmberTableConstructor]
        public NovelSkinRow(string skinId, string displayName)
        {
            SkinId = skinId;
            DisplayName = displayName;
        }
        #endregion
    }
}
