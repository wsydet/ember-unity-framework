// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;

namespace Ember.UI
{
    /// <summary>
    /// 页面定义 —— 描述一个 UI 页面的完整元数据。
    ///
    /// 静态注册表示例：
    /// <code>
    /// public static class GamePages
    /// {
    ///     public static readonly PageDef MainMenu  = new("ui/main_menu", UILayer.Normal,   PageType.MainPage);
    ///     public static readonly PageDef Settings  = new("ui/settings",  UILayer.Popup,    PageType.Popup);
    ///     public static readonly PageDef Loading   = new("ui/loading",   UILayer.TopMost,  PageType.TopMost);
    ///     public static readonly PageDef HeroTab   = new("ui/hero_tab",  UILayer.Normal,   PageType.SubPage);
    /// }
    /// </code>
    /// </summary>
    public class PageDef
    {
        #region 内部参数

        private readonly string _prefabPath;
        private readonly int _layer;
        private readonly PageType _pageType;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>预制体资源路径</summary>
        public string PrefabPath => _prefabPath;

        /// <summary>渲染层级（值越大越靠前）</summary>
        public int Layer => _layer;

        /// <summary>页面行为模式</summary>
        public PageType PageType => _pageType;

        /// <summary>完整定义页面</summary>
        public PageDef(string prefabPath, int layer, PageType pageType = PageType.MainPage)
        {
            _prefabPath = prefabPath ?? throw new ArgumentNullException(nameof(prefabPath));
            _layer = layer;
            _pageType = pageType;
        }

        /// <summary>使用 UILayer 枚举的便捷构造</summary>
        public PageDef(string prefabPath, UILayer layer, PageType pageType = PageType.MainPage)
            : this(prefabPath, (int)layer, pageType)
        {
        }

        public override string ToString()
        {
            return $"PageDef({_prefabPath}, layer={_layer}, type={_pageType})";
        }

        #endregion
    }
}
