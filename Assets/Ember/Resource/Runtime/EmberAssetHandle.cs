using System;
using Object = UnityEngine.Object;

namespace Ember.Resource
{
    /// <summary>
    /// 异步资源加载句柄 —— 封装一次资源加载请求的完整生命周期。
    ///
    /// 与直接使用回调（Action《T》）不同，Handle 提供了：
    /// - 状态查询（IsDone / Succeeded / Error）
    /// - 取消支持（Cancel）
    /// - 引用释放（Dispose）
    ///
    /// 设计参考了 burner 的 GameResourceHandle，去掉了对 YooAsset HandleBase 的依赖，
    /// 改为后端无关的纯回调模式。
    ///
    /// 通常不直接创建，而是通过 EmberResourceManager.LoadAssetHandle 获取。
    ///
    /// 用法：
    /// <code>
    /// var handle = EmberResourceManager.Instance.LoadAssetHandle&lt;Sprite&gt;("ui/icon");
    /// handle.Completed += (sprite) => { image.sprite = sprite; };
    /// handle.Cancel();   // 取消加载
    /// handle.Dispose();  // 释放资源引用
    /// </code>
    /// </summary>
    public sealed class EmberAssetHandle<T> : IDisposable where T : Object
    {
        #region 内部参数

        private readonly string _assetPath;
        private T _asset;
        private bool _isDone;
        private bool _isCancelled;
        private bool _succeeded;
        private string _error;
        private Action _cancelAction;
        private event Action<T> _completed;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>资源路径（与 LoadAssetHandle 传入的 path 一致）。</summary>
        public string AssetPath => _assetPath;

        /// <summary>加载是否已完成（成功、失败或取消）。</summary>
        public bool IsDone => _isDone;

        /// <summary>加载是否成功（IsDone && 无错误且未被取消）。</summary>
        public bool Succeeded => _succeeded;

        /// <summary>已加载的资源（仅 Succeeded 时非 null）。</summary>
        public T Asset => _asset;

        /// <summary>错误信息（仅失败时非空）。</summary>
        public string Error => _error;

        /// <summary>
        /// 加载完成事件。成功时参数为已加载的资源，失败时为 null。
        /// 如果 Handle 在事件注册前已加载完毕，注册时会立即回调。
        /// </summary>
        public event Action<T> Completed
        {
            add
            {
                if (_isDone)
                {
                    // 已加载完毕，立即回调（模拟同步完成）
                    value?.Invoke(_succeeded ? _asset : null);
                }
                else
                {
                    _completed += value;
                }
            }
            remove
            {
                _completed -= value;
            }
        }

        /// <summary>
        /// 取消加载请求。已完成的请求忽略。
        /// 取消后会触发 Completed(null)。
        /// </summary>
        public void Cancel()
        {
            if (_isDone || _isCancelled) return;
            _isCancelled = true;
            _cancelAction?.Invoke();
            Complete(null, false, "Cancelled");
        }

        /// <summary>
        /// 释放资源引用。如果加载中则先取消。
        /// </summary>
        public void Dispose()
        {
            Cancel();
            _asset = null;
            _completed = null;
            _cancelAction = null;
        }

        /// <summary>
        /// 隐式转换为 T（语法糖）。仅 Succeeded 时有效，否则返回 null。
        /// </summary>
        public static implicit operator T(EmberAssetHandle<T> handle)
        {
            return handle?._asset;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法（由 Provider 调用）

        internal EmberAssetHandle(string path)
        {
            _assetPath = path;
        }

        /// <summary>Provider 注入取消委托（例如 YooAsset Handle.Release）。</summary>
        internal void SetCancellation(Action cancel)
        {
            _cancelAction = cancel;
        }

        /// <summary>Provider 在加载完成时调用此方法通知 Handle。</summary>
        internal void Complete(T asset, bool succeeded, string error)
        {
            if (_isDone || _isCancelled) return;
            _isDone = true;
            _succeeded = succeeded;
            _asset = asset;
            _error = error;
            _completed?.Invoke(succeeded ? asset : null);
        }

        #endregion
    }
}
