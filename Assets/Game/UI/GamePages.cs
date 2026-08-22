using Ember.UI;

namespace Game.UI
{
    /// <summary>
    /// 游戏页面注册表 —— 所有 UI 页面的统一定义处。
    ///
    /// 每个页面在此声明一个 public static readonly EUIPageDef，
    /// 运行时通过 EUIManager.Instance.ShowMainPage(GamePages.XXX) 打开。
    ///
    /// <para>路径为预制体在项目中的完整 Asset 路径（Assets/ 开头），
    /// Editor 下直接走 AssetDatabase.LoadAssetAtPath，无需 Resources 目录。</para>
    ///
    /// 后期可由图形化编辑器自动生成此类，无需手写。
    /// </summary>
    public static class GamePages
    {
        // ============================================================
        // Background 层 —— 兜底背景（sortingOrder=0，单例）
        // ============================================================
        /// <summary>EUIBackgroundPage 页面</summary>
        public static readonly EUIPageDef EUIBackgroundPage = new("Assets/Ember/UI/Runtime/Prefabs/EUIBackgroundPage.prefab", UILayer.Background, PageType.Background);



        // ============================================================
        // Normal 层 —— 全屏主页面
        // ============================================================

        public static readonly EUIPageDef MainMenu = new("Assets/Game/UI/Runtime/Prefabs/MainMenu.prefab", UILayer.Normal, PageType.MainPage);

        public static readonly EUIPageDef InGameUI = new("Assets/Game/UI/Runtime/Prefabs/InGameUI.prefab", UILayer.Normal, PageType.MainPage);

        // ============================================================
        // Popup 层 —— 弹窗
        // ============================================================

        public static readonly EUIPageDef Settings = new("Assets/Game/UI/Runtime/Prefabs/Settings.prefab", UILayer.Popup, PageType.Popup);

        // ============================================================
        // TopMost 层 —— 顶层（引导、加载遮罩等）
        // ============================================================

        public static readonly EUIPageDef EUILoadingPage = new("Assets/Ember/UI/Runtime/Prefabs/EUILoadingPage.prefab", UILayer.TopMost, PageType.TopMost);



        /// <summary>GMPage 页面（FreePage 需显式指定固定 sortingOrder）</summary>
        public static readonly EUIPageDef GMPage = new("Assets/Game/UI/Runtime/Prefabs/GMPage.prefab", UILayer.TopMost, PageType.FreePage, freePageSortingOrder: 30000);

    }
}