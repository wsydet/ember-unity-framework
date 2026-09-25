using Ember.Table;

namespace Game.Table
{
    /// <summary>语言清单。新增语言 = 这里加一行 + 文案表加一列，不需要改代码。</summary>
    [EmberTable("novel_languages")]
    public sealed class NovelLanguageRow
    {
        #region 内部参数
        [EmberTableKey, EmberTableColumn("id")]
        public string Id { get; }
        [EmberTableColumn("displayName")]
        public string DisplayName { get; }
        [EmberTableColumn("order")]
        public int Order { get; }
        /// <summary>源语言：其它语言缺条目时回退到它的列；文案表里对应列名固定为 zh_Hans。</summary>
        [EmberTableColumn("isSource")]
        public bool IsSource { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [EmberTableConstructor]
        public NovelLanguageRow(string id, string displayName, int order, bool isSource)
        {
            Id = id;
            DisplayName = displayName;
            Order = order;
            IsSource = isSource;
        }
        #endregion
    }
}
