using Ember.Table;

namespace Game.Table
{
    /// <summary>
    /// 皮肤素材覆盖表：一条记录把某个页面上的一个图片节点指向另一张图。
    /// 本期只覆盖图片，不覆盖位置与大小——那些仍由各小说自己在布局窗口里调。
    /// <para>定位方式：<c>control</c> 取 EUI 绑定控件名（留空表示页面根），<c>node</c> 是该锚点下的相对节点路径
    /// （留空表示锚点自身）。因此 SkinIcon 这类同名节点可以按所属控件分别覆盖。</para>
    /// </summary>
    [EmberTable("novel_skin_sprites")]
    public sealed class NovelSkinSpriteRow
    {
        #region 内部参数
        /// <summary>合成主键，约定写成 skinId.page.control.node，便于人工核对。</summary>
        [EmberTableKey, EmberTableColumn("id")]
        public string Id { get; }
        [EmberTableColumn("skinId"), EmberTableReference("novel_skins")]
        public string SkinId { get; }
        [EmberTableColumn("page")]
        public string Page { get; }
        [EmberTableColumn("control")]
        public string Control { get; }
        [EmberTableColumn("node")]
        public string Node { get; }
        /// <summary>Resources 下不含扩展名的图片路径，与立绘/背景的寻址方式一致。</summary>
        [EmberTableColumn("spritePath")]
        public string SpritePath { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [EmberTableConstructor]
        public NovelSkinSpriteRow(string id, string skinId, string page, string control, string node, string spritePath)
        {
            Id = id;
            SkinId = skinId;
            Page = page;
            Control = control;
            Node = node;
            SpritePath = spritePath;
        }
        #endregion
    }
}
