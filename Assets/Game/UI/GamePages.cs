using Ember.UI;

namespace Game.UI
{
    /// <summary>
    /// 游戏页面注册表 —— 所有 UI 页面的统一定义处。
    ///
    /// 每个页面在此声明一个 public static readonly PageDef，
    /// 运行时通过 EmberUIManager.Instance.Push(GamePages.XXX) 打开。
    ///
    /// 后期可由图形化编辑器自动生成此类，无需手写。
    /// </summary>
    public static class GamePages
    {
        // ============================================================
        // Normal 层 —— 全屏主页面
        // ============================================================

        /// <summary>主菜单页面</summary>
        public static readonly PageDef MainMenu = new("ui/main_menu", UILayer.Normal);

        // ============================================================
        // Popup 层 —— 弹窗
        // ============================================================

        /// <summary>设置面板</summary>
        public static readonly PageDef Settings = new("ui/settings", UILayer.Popup);

        // ============================================================
        // TopMost 层 —— 顶层（引导、加载遮罩等）
        // ============================================================

        /// <summary>Loading 遮罩</summary>
        public static readonly PageDef Loading = new("ui/loading", UILayer.TopMost);

        // TODO: 在此处继续添加页面定义
        // public static readonly PageDef NewPage = new("ui/new_page", UILayer.Popup);
    }
}
