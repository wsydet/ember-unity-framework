using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ember.Core
{
    public enum EmberTransitionStatus
    {
        Completed, Busy, Rejected, Cancelled, PreparationFailed, CommitFailed, CleanupFailed, Stopped
    }

    /// <summary>终态包含不可逆生命周期是否已开始；失败不能被解释成旧会话已经回滚。</summary>
    public sealed class EmberTransitionResult
    {
        public long RequestId { get; }
        public EmberTransitionStatus Status { get; }
        public bool CommitStarted { get; }
        public string Error { get; }

        internal EmberTransitionResult(long id, EmberTransitionStatus status, bool commitStarted, string error)
        { RequestId = id; Status = status; CommitStarted = commitStarted; Error = error; }
    }

    /// <summary>单次游戏转场。Cancel 只请求取消；Completion 在准备及场景清理结束后完成。</summary>
    public sealed class EmberTransitionOperation
    {
        #region 内部参数
        private readonly CancellationTokenSource _cancellation;
        private readonly TaskCompletionSource<EmberTransitionResult> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object _gate = new();
        private bool _finished;
        private readonly TaskCompletionSource<EmberTransitionResult> _prepared = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<EmberTransitionResult> _running = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<EmberTransitionResult> _interactive = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private EmberTransitionPresentation _presentation;
        private EmberTransitionResult _result;
        private bool _presentationEnded;
        private string _presentationError;
        private bool _entryValid;
        public long RequestId { get; }
        public CancellationToken Cancellation { get; }
        public Task<EmberTransitionResult> Completion => _completion.Task;
        public bool CommitStarted { get; private set; }
        /// <summary>本请求全部必要准备成功；失败时返回未到达边界的终态，不会永久等待。</summary>
        public Task<EmberTransitionResult> WhenPrepared => _prepared.Task;
        /// <summary>进入/恢复生命周期成功。此时场景可运行，但不表示遮罩已退出。</summary>
        public Task<EmberTransitionResult> WhenRunning => _running.Task;
        /// <summary>提交、清理及真正拥有者的完整过渡均成功。复用外层遮罩时晚于 Completion。</summary>
        public Task<EmberTransitionResult> WhenInteractive => _interactive.Task;
        public bool IsPrepared => _prepared.Task.IsCompleted && _prepared.Task.Result.Status == EmberTransitionStatus.Completed;
        public bool IsRunning => _entryValid && _running.Task.IsCompleted && _running.Task.Result.Status == EmberTransitionStatus.Completed;
        /// <summary>历史成功信号不代表当前会话仍有效；异步接线还应检查自己的会话世代。</summary>
        public bool CanInteract => IsRunning && _interactive.Task.IsCompleted && _interactive.Task.Result.Status == EmberTransitionStatus.Completed;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private EmberTransitionResult Result(EmberTransitionStatus status, string error = null)
            => new(RequestId, status, CommitStarted, error);

        private void TryCompleteInteraction()
        {
            if (_result == null) return;
            if (_result.Status != EmberTransitionStatus.Completed)
                _interactive.TrySetResult(_result);
            else if (_presentation != null && !_presentationEnded) return;
            else if (_presentationError != null)
                _interactive.TrySetResult(Result(EmberTransitionStatus.CleanupFailed, _presentationError));
            else if (_entryValid)
                _interactive.TrySetResult(Result(EmberTransitionStatus.Completed));
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal EmberTransitionOperation(long id, CancellationToken cancellation)
        {
            RequestId = id;
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            Cancellation = _cancellation.Token;
        }

        internal EmberTransitionPresentation HoldPresentation()
        {
            lock (_gate)
            {
                if (_finished || CommitStarted || IsPrepared || _presentation != null)
                    throw new InvalidOperationException("过渡拥有者必须在准备完成前唯一登记。");
                return _presentation = new EmberTransitionPresentation(this);
            }
        }

        internal void EndPresentation(string error)
        {
            lock (_gate)
            {
                if (_presentationEnded) return;
                _presentationEnded = true;
                _presentationError = error;
                TryCompleteInteraction();
            }
        }

        internal void MarkPrepared()
        {
            lock (_gate) { if (!_finished) _prepared.TrySetResult(Result(EmberTransitionStatus.Completed)); }
        }

        internal void MarkRunning()
        {
            lock (_gate)
            {
                if (_finished) return;
                _entryValid = true;
                _running.TrySetResult(Result(EmberTransitionStatus.Completed));
            }
        }

        internal void InvalidateEntry()
        {
            lock (_gate)
            {
                _entryValid = false;
                _interactive.TrySetResult(Result(EmberTransitionStatus.Cancelled, "进入已失效，不能交还本请求操作权。"));
            }
        }

        /// <summary>提交区开始后不再取消；最终结果仍报告实际提交或失败。</summary>
        public void Cancel()
        {
            lock (_gate)
            {
                if (!_finished && !CommitStarted) _cancellation.Cancel();
            }
        }

        internal bool BeginCommit()
        {
            lock (_gate)
            {
                if (_finished || CommitStarted || Cancellation.IsCancellationRequested) return false;
                CommitStarted = true;
                return true;
            }
        }

        internal void Finish(EmberTransitionStatus status, string error = null)
        {
            lock (_gate)
            {
                if (_finished) return;
                _finished = true;
                _result = Result(status, error);
                if (status != EmberTransitionStatus.Completed)
                {
                    _entryValid = false;
                    _prepared.TrySetResult(_result);
                    _running.TrySetResult(_result);
                }
                TryCompleteInteraction();
                _completion.TrySetResult(_result);
                _cancellation.Dispose();
            }
        }
        #endregion
    }
}
