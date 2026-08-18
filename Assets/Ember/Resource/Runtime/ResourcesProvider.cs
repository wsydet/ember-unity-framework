using System;
using System.Collections;
using Ember.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Ember.Basic;

namespace Ember.Resource
{
    /// <summary>
    /// 基于 Unity Resources 目录的资源后端。
    ///
    /// 这是最简实现，适合小型项目、原型开发和编辑器工具。
    /// 资源必须放在 Assets/Resources/ 目录下，路径相对于 Resources 目录。
    ///
    /// 用法：
    /// <code>
    /// EmberResourceManager.Instance.Initialize(new ResourcesProvider(), success => {
    ///     if (success) EmberDebug.Log(TAG, "资源系统就绪（Resources 模式）");
    /// });
    ///
    /// // 加载 Resources/Config/PlayerStates/IdleState.asset
    /// EmberResourceManager.Instance.LoadAssetAsync&lt;PlayerStateSO&gt;(
    ///     "Config/PlayerStates/IdleState", state => { ... });
    /// </code>
    ///
    /// 注意：
    /// - Resources 目录会全部打进包体，不支持热更新
    /// - 同步 Load 在主线程执行，不会卡顿（Resources 本身是同步的）
    /// - 大项目建议换成 AddressablesProvider 或 YooAssetProvider
    /// </summary>
    public class ResourcesProvider : IResourceProvider
    {
        private const string TAG = LogTags.ResourceProvider;
        #region 参数

        private MonoBehaviour _coroutineRunner;

        #endregion

        // ============================================================

        #region 外部方法

        /// <summary>
        /// Resources 后端无需真正的初始化（没有下载/版本检查），直接回调成功。
        /// 如果传入的 runner 不为 null，异步加载会用协程执行（模拟异步行为）；
        /// 否则直接用同步 Resources.Load。
        /// </summary>
        public void Initialize(Action<bool> onComplete)
        {
            onComplete?.Invoke(true);
        }

        /// <summary>
        /// 异步加载资源。底层走 Resources.Load（同步），
        /// 包装成异步回调是为了保持接口统一。
        /// </summary>
        public void LoadAssetAsync<T>(string path, Action<T> onComplete) where T : Object
        {
            if (string.IsNullOrEmpty(path))
            {
                onComplete?.Invoke(null);
                return;
            }

            // Resources.Load 本身就是同步的，直接回调
            // 如果未来需要真正的异步（避免主线程卡顿），可以走协程
            T asset = Resources.Load<T>(path);
            onComplete?.Invoke(asset);
        }

        /// <summary>
        /// 异步加载场景。底层走 Unity SceneManager.LoadSceneAsync，返回句柄供上层控制激活/进度。
        /// </summary>
        public AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Additive)
        {
            if (string.IsNullOrEmpty(sceneName))
                return null;

            var op = SceneManager.LoadSceneAsync(sceneName, mode);
            if (op == null)
                EmberDebug.LogError(TAG, $"LoadSceneAsync failed: scene '{sceneName}' not found.");
            return op;
        }

        /// <summary>
        /// Resources 模式下 UnloadAsset 不释放单个资源（Resources 没有此 API）。
        /// 调用 Resources.UnloadUnusedAssets 统一释放。
        /// </summary>
        public void UnloadAsset(string path)
        {
            // Resources 不支持单个资源卸载，由 UnloadUnusedAssets 统一处理
        }

        /// <summary>
        /// 释放所有未使用的资源。
        /// </summary>
        public void UnloadUnusedAssets()
        {
            Resources.UnloadUnusedAssets();
        }

        // ======== Handle API ========

        /// <summary>
        /// Resources 模式下同步返回已完成句柄（底层 Resources.Load 是同步的）。
        /// </summary>
        public EmberAssetHandle<T> LoadAssetHandle<T>(string path) where T : Object
        {
            var handle = new EmberAssetHandle<T>(path);
            if (string.IsNullOrEmpty(path))
            {
                handle.Complete(null, false, "Asset path is null or empty.");
                return handle;
            }

            T asset = Resources.Load<T>(path);
            if (asset != null)
                handle.Complete(asset, true, null);
            else
                handle.Complete(null, false, $"Resource not found at path: {path}");

            return handle;
        }

        /// <summary>
        /// Resources 模式下通过 TextAsset 加载文件内容。
        /// </summary>
        public EmberFileHandle LoadFileAsync(string path)
        {
            var handle = new EmberFileHandle(path);
            if (string.IsNullOrEmpty(path))
            {
                handle.Complete(null, null, false, "File path is null or empty.");
                return handle;
            }

            var asset = Resources.Load<TextAsset>(path);
            if (asset != null)
                handle.Complete(asset.bytes, null, true, null);
            else
                handle.Complete(null, null, false, $"File not found at path: {path}");

            return handle;
        }

        /// <summary>
        /// Resources 模式下同步读取 TextAsset 的 bytes。
        /// </summary>
        public byte[] LoadFileSync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                EmberDebug.LogError(TAG, "LoadFileSync: path is null or empty.");
                return null;
            }

            var asset = Resources.Load<TextAsset>(path);
            return asset?.bytes;
        }

        /// <summary>
        /// Resources 后端没有下载进度，始终返回 1。
        /// </summary>
        public float Progress => 1f;

        #endregion
    }
}
