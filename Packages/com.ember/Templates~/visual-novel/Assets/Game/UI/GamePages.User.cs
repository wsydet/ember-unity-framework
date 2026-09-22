// 用户页面注册区 —— 本文件属于用户，框架升级永不覆盖。
//
// 与框架的 GamePages.cs 为同名 partial 类，编译时自动拼接，
// 运行时用法完全一致：EUIManager.Instance.ShowMainPage(GamePages.XXX)。
//
// 添加方式（二选一）：
// ① 手写注册：
//      public static readonly EUIPageDef InventoryPage = new("Assets/GameResource/Resources/UI/Module/Inventory/Prefabs/InventoryPanel.prefab", UILayer.Popup, PageType.Popup);
// ② 通过 Ember 代码生成器（EmberCSharpImplementation 已配置指向本文件）自动写入。
using Ember.UI;

namespace Game.UI
{
    /// <summary>
    /// 用户页面注册区（partial 类，与框架的 GamePages.cs 拼接）。
    /// 框架升级绝不触碰本文件；新增页面注册写在下面。
    /// </summary>
    public static partial class GamePages
    {
        // TODO: 在此处继续添加（用户页面注册区）

        /// <summary>视觉小说阅读主页面</summary>
        public static readonly EUIPageDef EUINovelReaderPage = new("Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelReaderPage.prefab", UILayer.Normal, PageType.MainPage);

        /// <summary>EUINovelSavePage 页面</summary>
        public static readonly EUIPageDef EUINovelSavePage = new("Assets/GameResource/Resources/UI/Module/NovelSave/Prefabs/EUINovelSavePage.prefab", UILayer.Popup, PageType.Popup);

        /// <summary>EUINovelHistoryPage 页面</summary>
        public static readonly EUIPageDef EUINovelHistoryPage = new("Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelHistoryPage.prefab", UILayer.Popup, PageType.Popup);

        /// <summary>EUINovelFontPage 页面</summary>
        public static readonly EUIPageDef EUINovelFontPage = new("Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelFontPage.prefab", UILayer.Popup, PageType.Popup);

        /// <summary>EUINovelReadingMenuPage 页面</summary>
        public static readonly EUIPageDef EUINovelReadingMenuPage = new("Assets/GameResource/Resources/UI/Module/Narrative/Prefabs/EUINovelReadingMenuPage.prefab", UILayer.Popup, PageType.Popup);

    }
}
