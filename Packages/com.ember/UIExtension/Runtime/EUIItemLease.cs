using System;
using System.Collections.Generic;
using System.Threading;
using Ember.Basic;
using Ember.UI;

namespace Ember.UIExtension
{
    /// <summary>一次数据借用。仅在 Unity 主线程调用；异步结果回主线程后用 TryApply 检查有效性。</summary>
    public sealed class EUIItemLease<T> : IDisposable
    {
        #region 内部参数
        private readonly CancellationTokenSource _cancellation = new();
        private readonly List<Action> _cleanup = new();
        private bool _ended;
        public EUIItem Item { get; }
        public T Data { get; private set; }
        public string Key { get; }
        public int Index { get; }
        public long Generation { get; }
        public CancellationToken Cancellation { get; }
        public bool IsCurrent => !_ended && !Item.IsDisposed && Item.GameObject;
        public int CleanupCount => _cleanup.Count;
        #endregion

        #region 外部方法
        internal EUIItemLease(EUIItem item, T data, string key, int index, long generation)
        {
            Item = item; Data = data; Key = key; Index = index; Generation = generation;
            Cancellation = _cancellation.Token;
        }

        public void Track(Action cleanup)
        {
            if (cleanup == null) throw new ArgumentNullException(nameof(cleanup));
            if (_ended) cleanup(); else _cleanup.Add(cleanup);
        }

        public bool TryApply(Action<EUIItemLease<T>> apply)
        {
            if (!IsCurrent) return false;
            apply(this);
            return true;
        }

        public void Dispose()
        {
            if (_ended) return;
            _ended = true; // 在取消回调之前失效，防止迟到结果或取消回调重入。
            try { _cancellation.Cancel(); }
            catch (Exception error) { EmberDebug.LogError("EUIItemLease", error.ToString()); }
            for (var i = _cleanup.Count - 1; i >= 0; i--)
                try { _cleanup[i](); }
                catch (Exception error) { EmberDebug.LogError("EUIItemLease", error.ToString()); }
            _cleanup.Clear();
            _cancellation.Dispose();
            Data = default;
        }
        #endregion
    }
}
