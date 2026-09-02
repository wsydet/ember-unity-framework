// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;

namespace Ember.UI
{
    /// <summary>
    /// 页面定义 —— 描述一个 UI 页面的完整元数据。
    ///
    /// 静态注册表示例（框架页面与用户页面分文件、partial 拼接）：
    /// <code>
    /// // GamePages.cs —— 框架预写页面（全文件框架所有，升级可刷新）
    /// public static partial class GamePages
    /// {
    ///     public static readonly EUIPageDef MainMenu  = new("ui/main_menu", UILayer.Normal,   PageType.MainPage);
    /// }
    ///
    /// // GamePages.User.cs —— 用户页面注册区（用户所有，框架永不覆盖）
    /// public static partial class GamePages
    /// {
    ///     public static readonly EUIPageDef Settings  = new("ui/settings",  UILayer.Popup,    PageType.Popup);
    ///     public static readonly EUIPageDef HeroTab   = new("ui/hero_tab",  UILayer.Normal,   PageType.SubPage);
    ///     public static readonly EUIPageDef GM        = new("ui/gm",        UILayer.TopMost,  PageType.FreePage, freePageSortingOrder: 30000);
    /// }
    /// </code>
    /// </summary>
    public class EUIPageDef
    {
        #region 内部参数

        private readonly string _prefabPath;
        private readonly int _layer;
        private readonly PageType _pageType;
        private readonly int? _overlaySortingOrder;
        private readonly int? _freePageSortingOrder;
        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>预制体资源路径</summary>
        public string PrefabPath => _prefabPath;

        /// <summary>渲染层级（值越大越靠前）</summary>
        public int Layer => _layer;

        /// <summary>页面行为模式</summary>
        public PageType PageType => _pageType;

        /// <summary>Overlay 页面固定排序值（仅 PageType.Overlay 时有效）。null 则使用 Layer 值。</summary>
        public int? OverlaySortingOrder => _overlaySortingOrder;

        /// <summary>FreePage 页面固定排序值（仅 PageType.FreePage 时有效）。null 则回退到 FreePageBaseOrder 并警告。</summary>
        public int? FreePageSortingOrder => _freePageSortingOrder;

        /// <summary>
        /// 是否为全屏弹窗。由 <see cref="PageType.FullScreenPopup"/> 唯一决定，
        /// 打开时下层被推裁剪面远端；普通 Popup 不隐藏下层，下层保持渲染、靠遮罩拦截交互。
        /// </summary>
        public bool IsFullScreen => _pageType == PageType.FullScreenPopup;

        /// <summary>
        /// 完整定义页面。<paramref name="isFullScreen"/> 仅用于兼容旧注册代码；
        /// 新代码直接使用 <see cref="PageType.FullScreenPopup"/>。
        /// </summary>
        public EUIPageDef(string prefabPath, int layer, PageType pageType = PageType.MainPage, int? overlaySortingOrder = null, int? freePageSortingOrder = null, bool isFullScreen = false)
        {
            _prefabPath = prefabPath ?? throw new ArgumentNullException(nameof(prefabPath));
            _layer = layer;
            if (isFullScreen && pageType != PageType.Popup && pageType != PageType.FullScreenPopup)
                throw new ArgumentException("isFullScreen 仅可与 Popup 页面类型组合使用。", nameof(isFullScreen));

            _pageType = isFullScreen ? PageType.FullScreenPopup : pageType;
            _overlaySortingOrder = overlaySortingOrder;
            _freePageSortingOrder = freePageSortingOrder;
        }

        /// <summary>使用 UILayer 枚举的便捷构造</summary>
        public EUIPageDef(string prefabPath, UILayer layer, PageType pageType = PageType.MainPage, int? overlaySortingOrder = null, int? freePageSortingOrder = null, bool isFullScreen = false)
            : this(prefabPath, (int)layer, pageType, overlaySortingOrder, freePageSortingOrder, isFullScreen)
        {
        }

        public override string ToString()
        {
            return $"EUIPageDef({_prefabPath}, layer={_layer}, type={_pageType})";
        }

        #endregion
    }
}
