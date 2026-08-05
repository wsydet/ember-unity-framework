using System;

namespace Ember.UI
{
    /// <summary>
    /// 页面定义 —— 描述一个 UI 页面的元数据。
    ///
    /// 所有页面通过静态类集中注册（类似 burner 的 PageDef），
    /// 未来可由图形化编辑器自动生成，无需手写。
    ///
    /// 预定义的层级枚举值（Background=0, Normal=100, Popup=200, TopMost=300），
    /// 也可使用自定义 int 值实现更细粒度的层级控制。
    ///
    /// 用法：
    /// <code>
    /// // 静态注册表（手写或工具生成）
    /// public static class GamePages
    /// {
    ///     public static readonly PageDef MainMenu  = new("ui/main_menu",  UILayer.Normal);
    ///     public static readonly PageDef Settings  = new("ui/settings",   UILayer.Popup);
    ///     public static readonly PageDef Loading   = new("ui/loading",    UILayer.TopMost);
    /// }
    ///
    /// // 使用时
    /// EmberUIManager.Instance.Push(GamePages.Settings, args: null);
    /// </code>
    /// </summary>
    public class PageDef
    {
        #region 参数

        /// <summary>
        /// 预制体资源路径（不含 PrefabPathPrefix，由 EmberUIManager 统一拼接）。
        /// </summary>
        public string PrefabPath { get; }

        /// <summary>
        /// 所属层级。值越大，渲染越靠前。
        /// </summary>
        public int Layer { get; }

        #endregion

        // ============================================================

        #region 外部方法

        /// <summary>
        /// 定义一个页面。
        /// </summary>
        /// <param name="prefabPath">预制体路径（相对于 Resources/Addressables 的 key）</param>
        /// <param name="layer">所属层级，可直接用 <see cref="UILayer"/> 枚举值或自定义 int</param>
        public PageDef(string prefabPath, int layer)
        {
            PrefabPath = prefabPath ?? throw new ArgumentNullException(nameof(prefabPath));
            Layer = layer;
        }

        /// <summary>
        /// 定义一个页面（重载，接受 UILayer 枚举）。
        /// </summary>
        public PageDef(string prefabPath, UILayer layer)
            : this(prefabPath, (int)layer)
        {
        }

        #endregion
    }
}
