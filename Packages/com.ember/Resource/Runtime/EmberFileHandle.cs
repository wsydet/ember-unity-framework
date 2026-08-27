using System;
using System.Text;
using Ember.Basic;

namespace Ember.Resource
{
    /// <summary>
    /// 文件加载句柄 —— 统一管理 Raw File / Bytes / Text 的异步加载。
    ///
    /// 设计参考了 burner 的 ResFileHandle，去掉 YooAsset 依赖，改为后端无关的设计：
    /// - 统一 IsDone / Succeeded / Error 状态查询
    /// - 支持 GetBytes（防御性拷贝）、GetText（懒解析+缓存）、GetFilePath
    /// - 支持 Cancel 取消和 Dispose 释放
    ///
    /// 通常不直接创建，而是通过 EmberResourceManager.LoadFileAsync 获取。
    ///
    /// 用法：
    /// <code>
    /// var handle = EmberResourceManager.Instance.LoadFileAsync("config/game_data.json");
    /// handle.Completed += (h) => {
    ///     if (h.Succeeded) {
    ///         string text = h.GetText();
    ///         byte[] bytes = h.GetBytes();
    ///     }
    /// };
    /// </code>
    /// </summary>
    public sealed class EmberFileHandle : IDisposable
    {
        #region 内部参数

        private readonly string _assetPath;
        private byte[] _bytes;
        private string _text;
        private string _filePath;
        private bool _isDone;
        private bool _succeeded;
        private string _error;
        private Action _cancelAction;
        private event Action<EmberFileHandle> _completed;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>文件路径。</summary>
        public string AssetPath => _assetPath;

        /// <summary>加载是否已完成。</summary>
        public bool IsDone => _isDone;

        /// <summary>加载是否成功。</summary>
        public bool Succeeded => _succeeded;

        /// <summary>错误信息（仅失败时非空）。</summary>
        public string Error => _error;

        /// <summary>
        /// 加载完成事件。成功后可通过 GetBytes / GetText 获取内容。
        /// 如果 Handle 在事件注册前已加载完毕，注册时会立即回调。
        /// </summary>
        public event Action<EmberFileHandle> Completed
        {
            add
            {
                if (_isDone)
                {
                    value?.Invoke(this);
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
        /// 取消加载请求。已完成则忽略。
        /// </summary>
        public void Cancel()
        {
            if (_isDone) return;
            _cancelAction?.Invoke();
            Complete(null, null, false, "Cancelled");
        }

        /// <summary>
        /// 获取文件原始字节（防御性拷贝，安全修改不影响缓存）。
        /// </summary>
        [HasGC]
        public byte[] GetBytes()
        {
            if (_bytes == null || _bytes.Length == 0)
                return Array.Empty<byte>();

            var copy = new byte[_bytes.Length];
            Buffer.BlockCopy(_bytes, 0, copy, 0, _bytes.Length);
            return copy;
        }

        /// <summary>
        /// 获取文件文本内容（UTF-8 解码，首次调用后缓存，后续零 GC）。
        /// </summary>
        [HasGC]
        public string GetText()
        {
            if (_bytes == null || _bytes.Length == 0)
                return string.Empty;

            return _text ??= Encoding.UTF8.GetString(_bytes);
        }

        /// <summary>
        /// 获取文件在磁盘上的路径。
        /// 仅在 Provider 支持文件路径时有效，否则返回 string.Empty。
        /// </summary>
        [NoGC]
        public string GetFilePath()
        {
            return _filePath ?? string.Empty;
        }

        /// <summary>
        /// 释放句柄：取消加载（如果进行中）+ 清空缓存数据。
        /// </summary>
        public void Dispose()
        {
            Cancel();
            _bytes = null;
            _text = null;
            _completed = null;
            _cancelAction = null;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法（由 Provider 调用）

        internal EmberFileHandle(string path)
        {
            _assetPath = path;
        }

        /// <summary>Provider 注入取消委托。</summary>
        internal void SetCancellation(Action cancel)
        {
            _cancelAction = cancel;
        }

        /// <summary>Provider 在加载完成时调用此方法通知 Handle。</summary>
        internal void Complete(byte[] bytes, string filePath, bool succeeded, string error)
        {
            if (_isDone) return;
            _isDone = true;
            _succeeded = succeeded;
            _bytes = bytes;
            _filePath = filePath;
            _error = error;
            _completed?.Invoke(this);
        }

        /// <summary>创建已失败的句柄（工厂方法，供 Provider 内部使用）。</summary>
        internal static EmberFileHandle Failed(string path, string error)
        {
            var handle = new EmberFileHandle(path);
            handle.Complete(null, null, false, error);
            return handle;
        }

        #endregion
    }
}
