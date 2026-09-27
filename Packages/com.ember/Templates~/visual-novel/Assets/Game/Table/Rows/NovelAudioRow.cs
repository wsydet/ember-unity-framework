using Ember.Table;

namespace Game.Table
{
    [EmberTable("novel_audio")]
    public sealed class NovelAudioRow
    {
        #region 内部参数
        [EmberTableKey, EmberTableColumn("id")]
        public string Id { get; }
        [EmberTableColumn("category")]
        public string Category { get; }
        [EmberTableColumn("resourcePath")]
        public string ResourcePath { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [EmberTableConstructor]
        public NovelAudioRow(string id, string category, string resourcePath)
        {
            Id = id;
            Category = category;
            ResourcePath = resourcePath;
        }
        #endregion
    }
}

