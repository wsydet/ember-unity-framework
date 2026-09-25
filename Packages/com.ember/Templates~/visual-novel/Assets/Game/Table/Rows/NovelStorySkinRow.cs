using Ember.Table;

namespace Game.Table
{
    /// <summary>
    /// 小说（剧情）与皮肤的赋值表：一个 storyId 对应一套皮肤。
    /// 没有记录的剧情不套任何皮肤，完全沿用 Prefab 外观。
    /// </summary>
    [EmberTable("novel_story_skin")]
    public sealed class NovelStorySkinRow
    {
        #region 内部参数
        [EmberTableKey, EmberTableColumn("storyId")]
        public string StoryId { get; }
        [EmberTableColumn("skinId"), EmberTableReference("novel_skins")]
        public string SkinId { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [EmberTableConstructor]
        public NovelStorySkinRow(string storyId, string skinId)
        {
            StoryId = storyId;
            SkinId = skinId;
        }
        #endregion
    }
}
