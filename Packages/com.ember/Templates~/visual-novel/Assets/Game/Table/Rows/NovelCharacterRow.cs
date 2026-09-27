using Ember.Table;

namespace Game.Table
{
    [EmberTable("novel_characters")]
    public sealed class NovelCharacterRow
    {
        #region 内部参数
        [EmberTableKey, EmberTableColumn("id")]
        public string Id { get; }
        [EmberTableColumn("displayName")]
        public string DisplayName { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [EmberTableConstructor]
        public NovelCharacterRow(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }
        #endregion
    }
}

