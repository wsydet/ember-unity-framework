using System;
using UnityEngine;

namespace Ember.Resource
{
    /// <summary>
    /// 资源提供者接口 —— 定义资源加载/卸载的基本操作。
    ///
    /// 框架不绑定具体资源后端（Resources / Addressables / YooAsset），
    /// 而是通过此接口隔离。使用者实现此接口并在启动时注册，
    /// 所有上层模块通过 <see cref="EmberResourceManager"/> 消费资源，不感知后端。
    ///
    /// 用法：
    /// <code>
    /// // 实现一个 Addressables 后端
    /// public class AddressablesProvider : IResourceProvider
    /// {
    ///     public void Initialize(Action&lt;bool&gt; onComplete) { ... }
    ///     public void LoadAssetAsync&lt;T&gt;(string path, Action&lt;T&gt; onComplete) where T : Object { ... }
    ///     ...
    /// }
    ///
    /// // 启动时注册
    /// EmberResourceManager.Instance.Initialize(new AddressablesProvider(), onComplete: ...);
    /// </code>
    /// </summary>
    public interface IResourceProvider
    {
        #region 参数

        /// <summary>
        /// 当前整体加载/下载进度，范围 0.0 ~ 1.0。
        /// 初始化阶段表示资源包下载进度，运行阶段通常返回 1.0。
        /// </summary>
        float Progress { get; }

        #endregion

        // ============================================================

        #region 外部方法

        /// <summary>
        /// 初始化资源后端。完成后调用 onComplete，参数 true 表示成功。
        /// 可能涉及版本检查、资源包下载、索引构建等耗时操作。
        /// </summary>
        void Initialize(Action<bool> onComplete);

        /// <summary>
        /// 异步加载指定路径的资源。完成后调用 onComplete 回调。
        /// 加载失败时回调参数为 null。
        /// </summary>
        /// <typeparam name="T">资源类型，必须是 UnityEngine.Object 的子类</typeparam>
        /// <param name="path">资源路径（格式由后端定义，如 "ui/icons/coin"）</param>
        /// <param name="onComplete">加载完成回调</param>
        void LoadAssetAsync<T>(string path, Action<T> onComplete) where T : UnityEngine.Object;

        /// <summary>
        /// 异步加载场景。完成后调用 onComplete 回调。
        /// </summary>
        /// <param name="sceneName">场景名（Build Settings 中的名称或路径）</param>
        /// <param name="onComplete">场景加载完成回调</param>
        void LoadSceneAsync(string sceneName, Action onComplete);

        /// <summary>
        /// 释放指定路径的资源引用。实际释放时机由后端决定（引用计数归零、GC 等）。
        /// </summary>
        void UnloadAsset(string path);

        /// <summary>
        /// 释放所有未使用的资源（对应 Resources.UnloadUnusedAssets）。
        /// 通常在场景切换后调用以清理旧场景残留。
        /// </summary>
        void UnloadUnusedAssets();

        #endregion
    }
}
