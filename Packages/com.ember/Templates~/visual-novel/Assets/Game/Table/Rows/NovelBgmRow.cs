using Ember.Table;

namespace Game.Table
{
    /// <summary>
    /// 一首分段 BGM 的四段资源路径。一行一曲，行键即剧情 BGM 节点填写的资源键。
    /// loopPath 必需，其余段留空时按既有降级规则运行（见 NovelBgmRules）。
    /// </summary>
    [EmberTable("novel_bgm")]
    public sealed class NovelBgmRow
    {
        #region 内部参数
        [EmberTableKey, EmberTableColumn("id")]
        public string Id { get; }
        [EmberTableColumn("introPath")]
        public string IntroPath { get; }
        [EmberTableColumn("loopPath")]
        public string LoopPath { get; }
        [EmberTableColumn("climaxPath")]
        public string ClimaxPath { get; }
        [EmberTableColumn("outroPath")]
        public string OutroPath { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [EmberTableConstructor]
        public NovelBgmRow(string id, string introPath, string loopPath, string climaxPath, string outroPath)
        {
            Id = id;
            IntroPath = introPath;
            LoopPath = loopPath;
            ClimaxPath = climaxPath;
            OutroPath = outroPath;
        }
        #endregion
    }
}
