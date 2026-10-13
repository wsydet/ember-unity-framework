using System;
using System.Threading;

namespace Ember.Core
{
    /// <summary>本请求的过渡完成凭证。真正的遮罩拥有者在完整退出后完成，放弃或失败不能报告成功。</summary>
    public sealed class EmberTransitionPresentation : IDisposable
    {
        #region 内部参数
        private EmberTransitionOperation _operation;
        public long RequestId { get; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal EmberTransitionPresentation(EmberTransitionOperation operation)
        { _operation = operation; RequestId = operation.RequestId; }

        /// <summary>只能在完整过渡已消失时调用；重复调用和迟到凭证均不会再次解锁。</summary>
        public void Complete() => Interlocked.Exchange(ref _operation, null)?.EndPresentation(null);

        public void Fail(string error)
            => Interlocked.Exchange(ref _operation, null)?.EndPresentation(error ?? "过渡未成功结束。");

        public void Dispose() => Fail("过渡拥有者放弃了本请求。");
        #endregion
    }
}
