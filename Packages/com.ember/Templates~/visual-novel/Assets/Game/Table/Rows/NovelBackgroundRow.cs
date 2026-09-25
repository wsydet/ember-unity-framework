using Ember.Table;

namespace Game.Table
{
    [EmberTable("novel_backgrounds")]
    public sealed class NovelBackgroundRow
    {
        #region 内部参数
        [EmberTableKey, EmberTableColumn("id")]
        public string Id { get; }
        [EmberTableColumn("resourcePath")]
        public string ResourcePath { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [EmberTableConstructor]
        public NovelBackgroundRow(string id, string resourcePath)
        {
            Id = id;
            ResourcePath = resourcePath;
        }
        #endregion
    }
}

