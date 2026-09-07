// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using Ember.Basic;
using Ember.Core;
using Ember.UI;

using UnityEngine;

namespace Ember.SceneUI.Integration
{
    /// <summary>
    /// SceneUI 业务模块基类。负责 Engine 生命周期、宿主页加载、Channel 隔离和模块级句柄校验。
    /// 具体项目只需要继承为 SceneUIModule，并在 RegisterSceneUIChannels 中声明业务 Channel。
    /// </summary>
    public abstract class EmberSceneUIModuleBase<TModule> : EmberSingleton<TModule>, IEmberModule
        where TModule : EmberSceneUIModuleBase<TModule>, new()
    {
        private sealed class ChannelRuntime
        {
            public SceneUIChannelDescriptor Descriptor;
            public SceneUIChannelState State;
            public EUIPage HostPage;
            public HostedSceneUIContext Context;
            public bool OwnsUpdateSource;
        }

        #region 内部参数

        private const string TAG = "SceneUI.Module";

        private readonly Dictionary<SceneUIChannelKey, ChannelRuntime> _channels =
            new Dictionary<SceneUIChannelKey, ChannelRuntime>();

        private EmberSceneUIEngine _engine;
        private int _moduleGeneration;
        private bool _isActive;
        private bool _acceptingRegistrations;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>模块是否已经进入对应 Phase。</summary>
        public bool IsActive => _isActive;

        /// <summary>业务可以监听 Channel 宿主页完成加载并成功绑定。</summary>
        public event Action<SceneUIChannelKey> ChannelReady;

        /// <summary>业务可以监听 Channel 创建失败及具体原因。</summary>
        public event Action<SceneUIChannelKey, string> ChannelUnavailable;

        /// <summary>读取 Channel 当前状态。</summary>
        [NoGC]
        public SceneUIChannelState GetChannelState(SceneUIChannelKey channelKey)
        {
            return _channels.TryGetValue(channelKey, out ChannelRuntime runtime)
                ? runtime.State
                : SceneUIChannelState.Unregistered;
        }

        /// <summary>读取当前模块持有 Engine 的累计诊断。</summary>
        [NoGC]
        public bool TryGetDiagnostics(out SceneUIDiagnostics diagnostics)
        {
            if (_engine != null && !_engine.IsDisposed)
            {
                diagnostics = _engine.Diagnostics;
                return true;
            }

            diagnostics = default;
            return false;
        }

        /// <summary>向指定 Ready Channel 注册一个 SceneUI。</summary>
        [HasGC]
        public bool TryRegisterSceneUI(
            SceneUIChannelKey channelKey,
            in SceneUIModuleRequest request,
            out SceneUIModuleHandle handle)
        {
            handle = default;
            if (!_acceptingRegistrations
                || _engine == null
                || _engine.IsDisposed
                || !_channels.TryGetValue(channelKey, out ChannelRuntime runtime)
                || runtime.State != SceneUIChannelState.Ready
                || runtime.Context == null
                || runtime.Context.IsDisposed)
                return false;

            SceneUIHandle engineHandle = _engine.Register(
                new SceneUIRequest
                {
                    Context = runtime.Context.ContextHandle,
                    ViewKey = request.ViewKey,
                    Anchor = request.Anchor,
                    Binder = request.Binder,
                    WorldOffset = request.WorldOffset,
                    UiOffset = request.UiOffset,
                    UpdatePolicy = request.UpdatePolicy,
                    VisibilityPolicy = request.VisibilityPolicy,
                    ScalePolicy = request.ScalePolicy,
                    ViewLifetimePolicy = request.ViewLifetimePolicy,
                    RecycleDelaySeconds = request.RecycleDelaySeconds,
                    SortingPriority = request.SortingPriority,
                    ViewMetrics = request.ViewMetrics,
                    OcclusionPolicy = request.OcclusionPolicy,
                    BusinessVisible = request.BusinessVisible,
                });

            if (!engineHandle.IsValid)
                return false;

            handle = new SceneUIModuleHandle(_moduleGeneration, channelKey, engineHandle);
            return true;
        }

        /// <summary>注销一个业务 SceneUI。旧生命周期或跨 Channel 句柄会安全失败。</summary>
        [NoGC]
        public bool UnregisterSceneUI(SceneUIModuleHandle handle)
        {
            return TryResolveHandle(handle, out _, out EmberSceneUIEngine engine)
                   && engine.Unregister(handle.EngineHandle);
        }

        /// <summary>检查业务模块级句柄是否仍然有效。</summary>
        [NoGC]
        public bool IsSceneUIValid(SceneUIModuleHandle handle)
        {
            return TryResolveHandle(handle, out _, out _);
        }

        /// <summary>通知 Engine 对应场景位置已经变化。</summary>
        [NoGC]
        public bool MarkSceneUIPositionDirty(SceneUIModuleHandle handle)
        {
            return TryResolveHandle(handle, out _, out EmberSceneUIEngine engine)
                   && engine.MarkSpatialDirty(handle.EngineHandle);
        }

        /// <summary>应用最新偏移并立即刷新对应 SceneUI 所属的 Channel。</summary>
        [HasGC("View pool misses may instantiate a prefab.")]
        public bool RefreshSceneUIPosition(
            SceneUIModuleHandle handle,
            Vector3 worldOffset,
            Vector2 uiOffset)
        {
            if (!TryResolveHandle(handle, out ChannelRuntime runtime, out EmberSceneUIEngine engine)
                || !engine.SetOffsets(handle.EngineHandle, worldOffset, uiOffset))
                return false;

            return engine.FlushContext(runtime.Context.ContextHandle);
        }

        /// <summary>通知 Engine 对应业务内容已经变化。</summary>
        [NoGC]
        public bool MarkSceneUIContentDirty(SceneUIModuleHandle handle)
        {
            return TryResolveHandle(handle, out _, out EmberSceneUIEngine engine)
                   && engine.MarkContentDirty(handle.EngineHandle);
        }

        /// <summary>设置单个业务 SceneUI 的显隐。</summary>
        [NoGC]
        public bool SetSceneUIVisible(SceneUIModuleHandle handle, bool visible)
        {
            return TryResolveHandle(handle, out _, out EmberSceneUIEngine engine)
                   && engine.SetBusinessVisible(handle.EngineHandle, visible);
        }

        /// <summary>设置整个 Channel 的显隐。</summary>
        [NoGC]
        public bool SetChannelVisible(SceneUIChannelKey channelKey, bool visible)
        {
            return TryGetReadyChannel(channelKey, out ChannelRuntime runtime)
                   && _engine.SetContextVisible(runtime.Context.ContextHandle, visible);
        }

        /// <summary>通知指定 Channel 相机已完成本帧最终更新并立即尝试刷新。</summary>
        [HasGC("View pool misses may instantiate a prefab.")]
        public bool NotifyCameraUpdated(SceneUIChannelKey channelKey)
        {
            if (!TryGetReadyChannel(channelKey, out ChannelRuntime runtime))
                return false;

            SceneUIContextHandle contextHandle = runtime.Context.ContextHandle;
            return _engine.MarkContextSpatialDirty(contextHandle)
                   && _engine.FlushContext(contextHandle);
        }

        /// <summary>注销 Channel、释放 Context，并关闭其业务宿主页。</summary>
        [HasGC]
        public bool UnregisterChannel(SceneUIChannelKey channelKey)
        {
            if (!_channels.TryGetValue(channelKey, out ChannelRuntime runtime))
                return false;

            _channels.Remove(channelKey);
            DisposeChannelRuntime(runtime, true);
            return true;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        /// <summary>具体业务模块在此声明全部 Channel。</summary>
        protected abstract void RegisterSceneUIChannels();

        /// <summary>模块完成 Channel 声明后的业务钩子。</summary>
        protected virtual void OnSceneUIModuleInitialized()
        {
        }

        /// <summary>模块开始释放前的业务钩子。</summary>
        protected virtual void OnSceneUIModuleDestroying()
        {
        }

        /// <summary>热重启时清理业务模块自身数据的钩子。</summary>
        protected virtual void ResetSceneUIModuleData()
        {
        }

        /// <summary>注册使用显式更新源的 Channel。宿主页只支持 Overlay 或 FreePage。</summary>
        [HasGC]
        protected bool RegisterChannel(
            SceneUIChannelKey channelKey,
            in SceneUIChannelDescriptor descriptor)
        {
            if (!_isActive || !channelKey.IsValid || _channels.ContainsKey(channelKey))
                return false;

            if (!ValidateChannelDescriptor(descriptor, out string error))
            {
                ReleaseOwnedUpdateSource(descriptor.CameraUpdateSource, descriptor.OwnsCameraUpdateSource);
                NotifyChannelUnavailable(channelKey, error);
                return false;
            }

            var runtime = new ChannelRuntime
            {
                Descriptor = descriptor,
                State = SceneUIChannelState.Loading,
                HostPage = null,
                Context = null,
                OwnsUpdateSource = descriptor.OwnsCameraUpdateSource,
            };
            _channels.Add(channelKey, runtime);

            int generation = _moduleGeneration;
            Action<EUIPage> onComplete = page => OnHostPageLoaded(
                generation,
                channelKey,
                runtime,
                page);

            if (descriptor.HostPage.PageType == PageType.Overlay)
                EUIManager.Instance.ShowOverlay(descriptor.HostPage, onComplete: onComplete);
            else
                EUIManager.Instance.ShowFreePage(descriptor.HostPage, onComplete: onComplete);

            return true;
        }

        /// <summary>创建由 Cinemachine 完成事件驱动的 Channel。</summary>
        [HasGC]
        protected bool RegisterCinemachineChannel(
            SceneUIChannelKey channelKey,
            EUIPageDef hostPage,
            Camera sceneCamera,
            SceneUIViewCatalog viewCatalog,
            in HostedSceneUIContextOptions options)
        {
            var updateSource = new CinemachineSceneUICameraUpdateSource(sceneCamera);
            return RegisterChannel(
                channelKey,
                new SceneUIChannelDescriptor
                {
                    HostPage = hostPage,
                    SceneCamera = sceneCamera,
                    ViewCatalog = viewCatalog,
                    CameraUpdateSource = updateSource,
                    OwnsCameraUpdateSource = true,
                    ContextOptions = options,
                });
        }

        private static bool ValidateChannelDescriptor(
            in SceneUIChannelDescriptor descriptor,
            out string error)
        {
            if (descriptor.HostPage == null)
            {
                error = "SceneUI Channel has no host EUIPageDef.";
                return false;
            }

            if (descriptor.HostPage.PageType != PageType.Overlay
                && descriptor.HostPage.PageType != PageType.FreePage)
            {
                error = "SceneUI host page must use PageType.Overlay or PageType.FreePage.";
                return false;
            }

            if (!descriptor.SceneCamera)
            {
                error = "SceneUI Channel has no SceneCamera.";
                return false;
            }

            if (!descriptor.ViewCatalog)
            {
                error = "SceneUI Channel has no SceneUIViewCatalog.";
                return false;
            }

            if (descriptor.CameraUpdateSource == null)
            {
                error = "SceneUI Channel has no CameraUpdateSource.";
                return false;
            }

            if (descriptor.OwnsCameraUpdateSource
                && !(descriptor.CameraUpdateSource is IDisposable))
            {
                error = "Owned CameraUpdateSource must implement IDisposable.";
                return false;
            }

            error = null;
            return true;
        }

        private void OnHostPageLoaded(
            int generation,
            SceneUIChannelKey channelKey,
            ChannelRuntime expectedRuntime,
            EUIPage page)
        {
            if (!_isActive
                || generation != _moduleGeneration
                || !_channels.TryGetValue(channelKey, out ChannelRuntime runtime)
                || runtime != expectedRuntime)
            {
                if (page != null && EUIManager.IsValid)
                    EUIManager.Instance.ClosePage(page);
                return;
            }

            if (page == null || !page.GameObject)
            {
                SetChannelUnavailable(channelKey, runtime, "SceneUI host page failed to load.", false);
                return;
            }

            runtime.HostPage = page;
            SceneUIPageHost pageHost = page.GameObject.GetComponentInChildren<SceneUIPageHost>(true);
            if (!pageHost)
            {
                SetChannelUnavailable(
                    channelKey,
                    runtime,
                    "SceneUI host page prefab has no SceneUIPageHost component.",
                    true);
                return;
            }

            IDisposable ownedUpdateSource = runtime.OwnsUpdateSource
                ? runtime.Descriptor.CameraUpdateSource as IDisposable
                : null;
            if (!HostedSceneUIContextFactory.TryCreate(
                    _engine,
                    pageHost,
                    runtime.Descriptor.SceneCamera,
                    runtime.Descriptor.CameraUpdateSource,
                    ownedUpdateSource,
                    runtime.Descriptor.ViewCatalog,
                    runtime.Descriptor.ContextOptions,
                    out HostedSceneUIContext context,
                    out string error))
            {
                SetChannelUnavailable(channelKey, runtime, error, true);
                return;
            }

            runtime.Context = context;
            runtime.OwnsUpdateSource = false;
            runtime.State = SceneUIChannelState.Ready;
            ChannelReady?.Invoke(channelKey);
        }

        private void SetChannelUnavailable(
            SceneUIChannelKey channelKey,
            ChannelRuntime runtime,
            string error,
            bool closePage)
        {
            runtime.State = SceneUIChannelState.Unavailable;
            DisposeChannelRuntime(runtime, closePage);
            NotifyChannelUnavailable(channelKey, error);
        }

        private static void ReleaseOwnedUpdateSource(
            ISceneUICameraUpdateSource updateSource,
            bool owned)
        {
            if (owned && updateSource is IDisposable disposable)
                disposable.Dispose();
        }

        private static int NextGeneration(int generation)
        {
            return generation == int.MaxValue ? 1 : generation + 1;
        }

        private bool TryGetReadyChannel(
            SceneUIChannelKey channelKey,
            out ChannelRuntime runtime)
        {
            if (_isActive
                && _engine != null
                && !_engine.IsDisposed
                && _channels.TryGetValue(channelKey, out runtime)
                && runtime.State == SceneUIChannelState.Ready
                && runtime.Context != null
                && !runtime.Context.IsDisposed
                && _engine.IsContextValid(runtime.Context.ContextHandle))
                return true;

            runtime = null;
            return false;
        }

        [NoGC]
        private bool TryResolveHandle(
            SceneUIModuleHandle handle,
            out ChannelRuntime runtime,
            out EmberSceneUIEngine engine)
        {
            engine = _engine;
            if (!handle.IsValid
                || handle.ModuleGeneration != _moduleGeneration
                || !TryGetReadyChannel(handle.ChannelKey, out runtime)
                || !engine.IsEntryValid(handle.EngineHandle))
            {
                runtime = null;
                return false;
            }

            return true;
        }

        private void NotifyChannelUnavailable(SceneUIChannelKey channelKey, string error)
        {
            EmberDebug.LogError(TAG, $"Channel {channelKey.Value} unavailable: {error}");
            ChannelUnavailable?.Invoke(channelKey, error);
        }

        private static void CloseHostPage(EUIPage page)
        {
            if (page != null && EUIManager.IsValid)
                EUIManager.Instance.ClosePage(page);
        }

        private static void DisposeChannelRuntime(ChannelRuntime runtime, bool closePage)
        {
            if (runtime == null) return;

            if (runtime.Context != null)
            {
                runtime.Context.Dispose();
                runtime.Context = null;
            }
            else
            {
                ReleaseOwnedUpdateSource(
                    runtime.Descriptor.CameraUpdateSource,
                    runtime.OwnsUpdateSource);
            }

            runtime.OwnsUpdateSource = false;
            if (closePage)
            {
                CloseHostPage(runtime.HostPage);
                runtime.HostPage = null;
            }
        }

        private void ShutdownModule()
        {
            if (!_isActive && _engine == null)
                return;

            _acceptingRegistrations = false;
            try
            {
                OnSceneUIModuleDestroying();
            }
            catch (Exception ex)
            {
                EmberDebug.LogError(TAG, $"{typeof(TModule).Name} destroy hook failed: {ex}");
            }

            foreach (KeyValuePair<SceneUIChannelKey, ChannelRuntime> pair in _channels)
                DisposeChannelRuntime(pair.Value, true);
            _channels.Clear();

            _engine?.Dispose();
            _engine = null;
            _isActive = false;
            _moduleGeneration = NextGeneration(_moduleGeneration);
            ChannelReady = null;
            ChannelUnavailable = null;
        }

        void IEmberModule.OnInit()
        {
            if (_isActive) return;

            _moduleGeneration = NextGeneration(_moduleGeneration);
            _engine = new EmberSceneUIEngine();
            _isActive = true;
            _acceptingRegistrations = true;

            try
            {
                RegisterSceneUIChannels();
                OnSceneUIModuleInitialized();
            }
            catch
            {
                ShutdownModule();
                throw;
            }
            EmberDebug.LogInit(TAG, $"{typeof(TModule).Name} initialized with {_channels.Count} channel(s).");
        }

        void IEmberModule.OnDestroy()
        {
            ShutdownModule();
            EmberDebug.LogCleanup(TAG, $"{typeof(TModule).Name} destroyed.");
        }

        void IEmberModule.ResetModuleData()
        {
            ResetSceneUIModuleData();
        }

        protected override void OnDestroy()
        {
            ShutdownModule();
            base.OnDestroy();
        }

        #endregion
    }
}
