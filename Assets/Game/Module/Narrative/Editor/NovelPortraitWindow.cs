using System.Collections.Generic;
using Ember.Table.Editor;

namespace Game.Narrative.Editor
{
    /// <summary>小说图片来源适配；保留旧窗口类型和 Open 入口。</summary>
    public sealed class NovelPortraitWindow : EmberImageLibraryWindow
    {
        #region 内部参数
        protected override string FolderAssetPath => "Assets/Game/Module/Narrative/Editor/Layouts/ImageFolders.asset";
        protected override bool EditingEnabled => NarrativeEditorAvailability.Enabled;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        protected override IEnumerable<EmberImageSource> BuiltinSources()
        {
            const string root = "Assets/GameResource/Resources/UI/Module/Narrative/Atlas/";
            yield return new EmberImageSource
            {
                Label = "角色立绘", TableId = "novel_portraits", GroupColumns = new[] { "characterId" },
                ImportFolder = root + "Portraits"
            };
            yield return new EmberImageSource
            {
                Label = "场景背景", TableId = "novel_backgrounds", ImportFolder = root + "Backgrounds"
            };
            yield return new EmberImageSource
            {
                Label = "界面皮肤", TableId = "novel_skin_sprites", ImageColumn = "spritePath",
                GroupColumns = new[] { "skinId", "page" }, Sprite = true, ImportFolder = root + "Skins/Imported"
            };
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static void Open()
        {
            NarrativeEditorAvailability.RequireEnabled();
            GetWindow<NovelPortraitWindow>().Show();
        }

        #endregion
    }
}
