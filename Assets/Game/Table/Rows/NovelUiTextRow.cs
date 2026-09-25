using Ember.Table;

namespace Game.Table
{
    /// <summary>
    /// UI 文案多语言表。列名即语言标识，与 novel_languages 的 id 一一对应；
    /// 只有新增或改动 UI 时才需要动它，因此把 Key 写成「ui.页面.控件[.节点]」。
    /// </summary>
    [EmberTable("novel_ui_text")]
    public sealed class NovelUiTextRow
    {
        #region 内部参数
        [EmberTableKey, EmberTableColumn("key")]
        public string Key { get; }
        [EmberTableColumn("zh_Hans")]
        public string ZhHans { get; }
        [EmberTableColumn("zh_Hant")]
        public string ZhHant { get; }
        [EmberTableColumn("ja")]
        public string Ja { get; }
        [EmberTableColumn("en")]
        public string En { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [EmberTableConstructor]
        public NovelUiTextRow(string key, string zhHans, string zhHant, string ja, string en)
        {
            Key = key;
            ZhHans = zhHans;
            ZhHant = zhHant;
            Ja = ja;
            En = en;
        }
        #endregion
    }
}
