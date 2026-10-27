// 用户页面注册区 —— 本文件属于用户，框架升级永不覆盖。
//
// 与框架的 GamePages.cs 为同名 partial 类，编译时自动拼接，
// 运行时用法完全一致：EUIManager.Instance.ShowMainPage(GamePages.XXX)。
//
// 添加方式（二选一）：
// ① 手写注册：
//      public static readonly EUIPageDef InventoryPage = new("Assets/GameResource/Resources/UI/Module/Inventory/Prefabs/InventoryPage.prefab", UILayer.Popup, PageType.Popup);
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

        /// <summary>EUIOwnershipExamplePage 页面</summary>
        public static readonly EUIPageDef EUIOwnershipExamplePage = new("Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUIOwnershipExamplePage.prefab", UILayer.TopMost, PageType.FreePage, freePageSortingOrder: 30000);

        /// <summary>EUILibraryPreviewPage 页面</summary>
        public static readonly EUIPageDef EUILibraryPreviewPage = new("Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUILibraryPreviewPage.prefab", UILayer.Normal, PageType.MainPage);

        /// <summary>EUILibraryPopupPage 页面</summary>
        public static readonly EUIPageDef EUILibraryPopupPage = new("Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUILibraryPopupPage.prefab", UILayer.Popup, PageType.Popup);

        /// <summary>EUIAlertPage 页面</summary>
        public static readonly EUIPageDef EUIAlertPage = new("Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUIAlertPage.prefab", UILayer.Popup, PageType.Popup);

        /// <summary>EUIConfirmPage 页面</summary>
        public static readonly EUIPageDef EUIConfirmPage = new("Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUIConfirmPage.prefab", UILayer.Popup, PageType.Popup);

        /// <summary>EUIExpandedLibraryPage 页面</summary>
        public static readonly EUIPageDef EUIExpandedLibraryPage = new("Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUIExpandedLibraryPage.prefab", UILayer.Normal, PageType.MainPage);

        /// <summary>EUIDynamicListPage 页面</summary>
        public static readonly EUIPageDef EUIDynamicListPage = new("Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUIDynamicListPage.prefab", UILayer.Normal, PageType.MainPage);

        /// <summary>EUIBuiCandidateDynamicListPage 页面</summary>
        public static readonly EUIPageDef EUIBuiCandidateDynamicListPage = new("Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUIBuiCandidateDynamicListPage.prefab", UILayer.Normal, PageType.MainPage);

        /// <summary>EUIBuiCandidateMainPage 页面</summary>
        public static readonly EUIPageDef EUIBuiCandidateMainPage = new("Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/EUIBuiCandidateMainPage.prefab", UILayer.Normal, PageType.MainPage);

    }
}