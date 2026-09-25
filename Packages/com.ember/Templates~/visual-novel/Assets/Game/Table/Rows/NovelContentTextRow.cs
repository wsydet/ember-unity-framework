using Ember.Table;

namespace Game.Table
{
    /// <summary>
    /// 内容多语言表：随剧情内容增长，由导入 skill 按 Key 写入。
    /// Key 取剧情指令的 _textKey；角色名约定使用 character.《角色键》这样的形式。
    /// </summary>
    [EmberTable("novel_content_text")]
    public sealed class NovelContentTextRow
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
        public NovelContentTextRow(string key, string zhHans, string zhHant, string ja, string en)
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
