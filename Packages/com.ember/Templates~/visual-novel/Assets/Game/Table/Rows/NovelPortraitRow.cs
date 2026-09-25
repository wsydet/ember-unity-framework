using Ember.Table;

namespace Game.Table
{
    [EmberTable("novel_portraits")]
    public sealed class NovelPortraitRow
    {
        #region 内部参数
        [EmberTableKey, EmberTableColumn("id")]
        public string Id { get; }
        [EmberTableColumn("characterId"), EmberTableReference("novel_characters")]
        public string CharacterId { get; }
        [EmberTableColumn("expression")]
        public string Expression { get; }
        [EmberTableColumn("resourcePath")]
        public string ResourcePath { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [EmberTableConstructor]
        public NovelPortraitRow(string id, string characterId, string expression, string resourcePath)
        {
            Id = id;
            CharacterId = characterId;
            Expression = expression;
            ResourcePath = resourcePath;
        }
        #endregion
    }
}

