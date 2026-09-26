// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

using Ember.Basic;
using Ember.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Ember.UI
{
    /// <summary>
    /// 退出封屏遮罩（框架内部）—— 订阅 <see cref="EmberQuitCurtain.CoverRequested"/>，
    /// 在退出前盖一张不透明的全屏黑幕，作为 3D 主相机封屏之外的第二道保险。
    ///
    /// <para><b>为什么用独立 Overlay 画布：</b></para>
    /// <list type="bullet">
    ///   <item><c>RenderMode.ScreenSpaceOverlay</c> 在所有相机之后渲染，不依赖 UI 相机，也不受场景里相机数量影响。</item>
    ///   <item>不挂在 UIRoot 下、也不注册为 EUI 页面，因此 <c>EUIViewEngine.Shutdown</c> 销毁页面时不会把它一起销毁；
    ///   这正是「清理完成 → 进程退出」这段区间需要它继续遮住画面的原因。</item>
    ///   <item>UI 引擎初始化时就预先创建并保持隐藏，避免退出帧临时创建画布带来的首帧抖动。</item>
    /// </list>
    /// </summary>
    internal static class EUIQuitCurtainCover
    {
        #region 内部参数

        private const string TAG = LogTags.UIManager;
        private const string COVER_NAME = "EmberQuitCurtainCover";

        /// <summary>遮罩排序值：必须高于所有页面层级（FreePage 30000 起）与业务启动黑幕（32500）。</summary>
        private const int COVER_SORTING_ORDER = 32767;

        private static GameObject _cover;
        private static bool _installed;

        /// <summary>当前遮罩对象（可能为 null；测试与诊断使用）。</summary>
        internal static GameObject Cover => _cover;

        /// <summary>当前遮罩的黑图（可能为 null；测试与诊断使用）。</summary>
        internal static Image CoverImage => _cover != null ? _cover.GetComponentInChildren<Image>(true) : null;

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static GameObject CreateCover()
        {
            // 根节点只放 Canvas / CanvasGroup：根 Canvas 的 RectTransform 由 Canvas 自己管理（尺寸=屏幕），
            // 满屏黑图挂在子节点上，与业务启动黑幕 BootSplash 的 Background 做法一致
            var go = new GameObject(COVER_NAME, typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = COVER_SORTING_ORDER;

            var group = go.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var picture = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            picture.transform.SetParent(go.transform, false);

            var rect = picture.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = picture.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;   // 封屏只负责画面，不参与输入

            go.SetActive(false);
            return go;
        }

        private static void OnCoverRequested()
        {
            if (_cover == null) _cover = CreateCover();   // 场景卸载等意外销毁时兜底重建
            _cover.SetActive(true);
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>订阅退出封屏请求并预创建遮罩。由 <c>EUIViewEngine.Init</c> 调用。</summary>
        internal static void Install()
        {
            if (_cover == null) _cover = CreateCover();

            if (_installed) return;
            _installed = true;
            EmberQuitCurtain.CoverRequested += OnCoverRequested;
        }

        /// <summary>
        /// 退订退出封屏请求。由 <c>EUIViewEngine.Shutdown</c> 调用。
        /// <b>不销毁遮罩</b>：它必须活过「框架清理完成 → 进程退出」这段区间，
        /// 否则那几帧会重新露出 3D 场景。
        /// </summary>
        internal static void Uninstall()
        {
            if (!_installed) return;
            _installed = false;
            EmberQuitCurtain.CoverRequested -= OnCoverRequested;
        }

        #endregion
    }
}
