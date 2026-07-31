using System;
using Ember.Core;
using UnityEngine;

namespace Ember.Resource
{
    /// <summary>
    /// 资源管理器 —— 框架资源加载的统一入口。
    ///
    /// 设计参考了 burner 项目的 <c>ResManager</c>，核心职责：
    /// - 接收一个 <see cref="IResourceProvider"/> 实现并初始化它
    /// - 将所有资源操作委托给 Provider，自身不做加载逻辑
    /// - 管理资源生命周期（初始化/销毁广播事件）
    ///
    /// 使用方式：
    /// <code>
    /// // 启动时
    /// EmberResourceManager.Instance.Initialize(new AddressablesProvider(), onComplete: success => {
    ///     if (success) EmberDebug.Log(TAG, "资源系统就绪");
    /// });
    ///
    /// // 运行时
    /// EmberResourceManager.Instance.LoadAssetAsync&lt;Sprite&gt;("ui/icons/coin", sprite => {
    ///     image.sprite = sprite;
    /// });
    /// </code>
    /// </summary>
    public class EmberResourceManager : EmberMonoSingleton<EmberResourceManager>
    {
        private const string TAG = LogTags.ResourceManager;
        #region 参数

        private IResourceProvider _provider;


        /// <summary>
        /// 资源系统是否已完成初始化。
        /// </summary>
        private bool _initialized;

        /// <summary>
        /// 资源系统是否已完成初始化。
        /// </summary>
        public bool IsInitialized => _initialized;

        /// <summary>
        /// 当前加载/下载进度（0.0 ~ 1.0）。
        /// 未初始化时返回 0，未设置 Provider 时返回 -1。
        /// </summary>
        public float Progress => _provider?.Progress ?? 0f;

        #endregion

        // ============================================================

        #region 外部方法

        // ======== 初始化 ========

        /// <summary>
        /// 初始化资源管理器，传入一个具体的 <see cref="IResourceProvider"/> 实现。
        ///
        /// 初始化过程中会：
        /// 1. 调用 Provider 的 Initialize 方法（可能耗时，涉及版本检查/下载）
        /// 2. 派发 <see cref="EmberBroadcastEvent.ResourceReady"/> 事件
        /// </summary>
        /// <param name="provider">资源后端实现</param>
        /// <param name="onComplete">初始化完成回调，true 表示成功</param>
        public void Initialize(IResourceProvider provider, Action<bool> onComplete = null)
        {
            if (_initialized)
            {
                EmberDebug.LogWarning(TAG, "EmberResourceManager is already initialized.");
                onComplete?.Invoke(true);
                return;
            }

            if (provider == null)
            {
                EmberDebug.LogError(TAG, "EmberResourceManager.Initialize: provider is null.");
                onComplete?.Invoke(false);
                return;
            }

            _provider = provider;

            _provider.Initialize(success =>
            {
                if (success)
                {
                    _initialized = true;
                    EmberEventBus.Dispatch(EmberBroadcastEvent.ResourceReady);
                    EmberDebug.Log(TAG, "Resource system initialized successfully.");
                }
                else
                {
                    EmberDebug.LogError(TAG, "Resource system initialization failed.");
                }

                onComplete?.Invoke(success);
            });
        }

        // ======== 资源加载 ========

        /// <summary>
        /// 异步加载资源。完成后回调在主线程执行。
        /// 若未初始化或 Provider 为空，回调返回 null。
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        /// <param name="onComplete">完成回调，加载失败时参数为 null</param>
        public void LoadAssetAsync<T>(string path, Action<T> onComplete) where T : UnityEngine.Object
        {
            if (!IsProviderReady(onComplete)) return;

            _provider.LoadAssetAsync(path, onComplete);
        }

        /// <summary>
        /// 异步加载场景。
        /// </summary>
        /// <param name="sceneName">场景名</param>
        /// <param name="onComplete">场景加载完成回调</param>
        public void LoadSceneAsync(string sceneName, Action onComplete = null)
        {
            if (!IsProviderReady())
            {
                onComplete?.Invoke();
                return;
            }

            _provider.LoadSceneAsync(sceneName, onComplete);
        }

        // ======== 资源卸载 ========

        /// <summary>
        /// 释放指定路径的资源引用。
        /// </summary>
        public void UnloadAsset(string path)
        {
            if (_provider == null) return;
            _provider.UnloadAsset(path);
        }

        /// <summary>
        /// 释放所有未使用的资源。通常在场景切换后调用。
        /// </summary>
        public void UnloadUnusedAssets()
        {
            _provider?.UnloadUnusedAssets();
        }

        #endregion

        // ============================================================

        #region 生命周期

        protected override void OnSingletonDestroy()
        {
            EmberEventBus.Dispatch(EmberBroadcastEvent.ResourceShutdown);

            _provider?.UnloadUnusedAssets();
            _provider = null;

            _initialized = false;
        }

        #endregion

        // ============================================================

        #region 内部方法

        /// <summary>
        /// 检查 Provider 是否就绪。就绪返回 true；
        /// 未就绪时自动回调 null 并打 Warning，返回 false。
        /// </summary>
        private bool IsProviderReady<T>(Action<T> onComplete) where T : UnityEngine.Object
        {
            if (!_initialized || _provider == null)
            {
                EmberDebug.LogWarning(TAG, "EmberResourceManager is not initialized. Call Initialize() first.");
                onComplete?.Invoke(null);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 无回调版本的 Provider 就绪检查（用于 LoadScene 等不泛型的场景）。
        /// </summary>
        private bool IsProviderReady()
        {
            if (!_initialized || _provider == null)
            {
                EmberDebug.LogWarning(TAG, "EmberResourceManager is not initialized. Call Initialize() first.");
                return false;
            }

            return true;
        }

        #endregion
    }
}
