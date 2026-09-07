// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;

using Ember.Basic;

using UnityEngine;

namespace Ember.SceneUI.Integration
{
    /// <summary>绑定现有业务页面时使用的 Context 配置，不包含任何 Canvas 创建或排序参数。</summary>
    public struct HostedSceneUIContextOptions
    {
        #region 编辑器面板参数

        public bool InitiallyVisible;
        public SceneUISortingMode SortingMode;
        public int PrewarmInstancesPerFlush;
        public ISceneUIVisibleRegionProvider VisibleRegionProvider;
        public ISceneUIOcclusionTester OcclusionTester;
        public int OcclusionTestsPerFlush;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public static HostedSceneUIContextOptions Default
        {
            [NoGC]
            get
            {
                return new HostedSceneUIContextOptions
                {
                    InitiallyVisible = true,
                    SortingMode = SceneUISortingMode.None,
                    PrewarmInstancesPerFlush = 0,
                    VisibleRegionProvider = null,
                    OcclusionTester = null,
                    OcclusionTestsPerFlush = 0,
                };
            }
        }

        #endregion
    }

    /// <summary>
    /// 一个绑定到业务 EUI 页面 BubbleRoot 的运行时 Context。
    /// Dispose 只释放 Engine Context、ViewHost 和更新源，不会销毁或关闭业务页面。
    /// </summary>
    public sealed class HostedSceneUIContext : IDisposable
    {
        #region 内部参数

        private readonly EmberSceneUIEngine _engine;
        private readonly SceneUIPageHost _pageHost;
        private readonly IDisposable _ownedUpdateSource;
        private bool _isDisposed;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public SceneUIContextHandle ContextHandle { get; }
        public RectTransform BubbleRoot { get; }
        public PrefabSceneUIViewHost ViewHost { get; }
        public bool IsDisposed => _isDisposed;

        internal HostedSceneUIContext(
            EmberSceneUIEngine engine,
            SceneUIPageHost pageHost,
            SceneUIContextHandle contextHandle,
            RectTransform bubbleRoot,
            PrefabSceneUIViewHost viewHost,
            IDisposable ownedUpdateSource)
        {
            _engine = engine;
            _pageHost = pageHost;
            _ownedUpdateSource = ownedUpdateSource;
            ContextHandle = contextHandle;
            BubbleRoot = bubbleRoot;
            ViewHost = viewHost;

            _pageHost.Bind(_engine, ContextHandle);
        }

        [NoGC]
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (_pageHost)
                _pageHost.Unbind(_engine, ContextHandle);

            if (_engine != null && !_engine.IsDisposed)
                _engine.UnregisterContext(ContextHandle);

            _ownedUpdateSource?.Dispose();
        }

        #endregion
    }

    /// <summary>把 SceneUI Engine 绑定到现有业务页面宿主，不创建任何 UI 层级。</summary>
    public static class HostedSceneUIContextFactory
    {
        #region 外部方法

        /// <summary>使用显式相机更新源绑定宿主页。</summary>
        [HasGC]
        public static bool TryCreate(
            EmberSceneUIEngine engine,
            SceneUIPageHost pageHost,
            Camera sceneCamera,
            ISceneUICameraUpdateSource updateSource,
            IDisposable ownedUpdateSource,
            SceneUIViewCatalog catalog,
            in HostedSceneUIContextOptions options,
            out HostedSceneUIContext context,
            out string error)
        {
            context = null;
            if (engine == null || engine.IsDisposed)
            {
                error = "EmberSceneUIEngine is missing or disposed.";
                return false;
            }

            if (!pageHost)
            {
                error = "SceneUIPageHost is missing.";
                return false;
            }

            if (!sceneCamera || updateSource == null)
            {
                error = "SceneCamera and CameraUpdateSource are required.";
                return false;
            }

            if (!catalog)
            {
                error = "SceneUIViewCatalog is missing.";
                return false;
            }

            if (options.PrewarmInstancesPerFlush < 0 || options.OcclusionTestsPerFlush < 0)
            {
                error = "SceneUI budgets cannot be negative.";
                return false;
            }

            if (!pageHost.TryResolve(
                    out RectTransform bubbleRoot,
                    out Canvas canvas,
                    out Camera uiCamera,
                    out error))
                return false;

            var viewHost = new PrefabSceneUIViewHost(bubbleRoot, bubbleRoot.gameObject.layer);
            bool deferPrewarm = options.PrewarmInstancesPerFlush > 0;
            if (!catalog.TryRegisterAll(viewHost, deferPrewarm, out error))
            {
                viewHost.Dispose();
                return false;
            }

            var visibleRegionProvider = options.VisibleRegionProvider
                                        ?? new CameraSafeAreaVisibleRegionProvider(sceneCamera);
            SceneUIContextHandle contextHandle = engine.RegisterContext(
                new SceneUIContextDescriptor
                {
                    SceneCamera = sceneCamera,
                    Canvas = canvas,
                    UICamera = uiCamera,
                    LayerRoot = bubbleRoot,
                    VisibleRegionProvider = visibleRegionProvider,
                    ViewHost = viewHost,
                    CameraUpdateSource = updateSource,
                    Visible = options.InitiallyVisible,
                    OwnsViewHost = true,
                    SortingMode = options.SortingMode,
                    PrewarmInstancesPerFlush = options.PrewarmInstancesPerFlush,
                    OcclusionTester = options.OcclusionTester,
                    OcclusionTestsPerFlush = options.OcclusionTestsPerFlush,
                });

            if (!contextHandle.IsValid)
            {
                viewHost.Dispose();
                error = "EmberSceneUIEngine rejected the hosted Context descriptor.";
                return false;
            }

            context = new HostedSceneUIContext(
                engine,
                pageHost,
                contextHandle,
                bubbleRoot,
                viewHost,
                ownedUpdateSource);
            error = null;
            return true;
        }

        #endregion
    }
}
