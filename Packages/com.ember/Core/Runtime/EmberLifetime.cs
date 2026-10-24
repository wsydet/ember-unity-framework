using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ember.Basic;

namespace Ember.Core
{
    public enum EmberLifetimeState { Stopped, Starting, Ready, Stopping, Faulted }
    public enum EmberStopReason { Quit, ScriptReload, InitializationFailed }

    /// <summary>
    /// 单次框架会话。停止先撤销就绪资格，再取消工作并收束注册的所有权。
    /// RegisterAbort 必须同步释放；异步清理由 RegisterStopOwner 登记，主线程不得同步等待 Unity 续体。
    /// </summary>
    public sealed class EmberLifetime
    {
        #region 内部参数
        private readonly CancellationTokenSource _cancellation = new();
        private readonly List<Registration> _owners = new();
        private readonly List<StopOwner> _stopOwners = new();
        private Task _whenStopped;
        /// <summary>已登记异步所有者的真实终止任务；失败或未完成不代表资源已释放。</summary>
        public Task WhenOwnersStopped => _whenStopped ?? Task.CompletedTask;
        public EmberLifetimeState State { get; private set; } = EmberLifetimeState.Starting;
        public EmberStopReason? StopReason { get; private set; }
        public CancellationToken Cancellation { get; }
        public bool IsReady => State == EmberLifetimeState.Ready;
        #endregion

        #region 外部方法
        public EmberLifetime() => Cancellation = _cancellation.Token;

        /// <summary>登记主线程开始的停止协议。backgroundIndependent 仅适用于完成完全不依赖 Unity 的任务。</summary>
        public IDisposable RegisterStopOwner(Func<Task> beginStop, bool backgroundIndependent = false)
        {
            if (beginStop == null) throw new ArgumentNullException(nameof(beginStop));
            if (State != EmberLifetimeState.Starting && State != EmberLifetimeState.Ready)
                throw new InvalidOperationException("会话已经停止接收资源所有权。");
            var owner = new StopOwner(beginStop, backgroundIndependent);
            _stopOwners.Add(owner);
            return owner;
        }

        /// <summary>注册本会话资源的同步中止操作。Dispose 仅注销，不执行中止。</summary>
        public IDisposable RegisterAbort(Action abort)
        {
            if (abort == null) throw new ArgumentNullException(nameof(abort));
            if (abort.Method.IsDefined(typeof(System.Runtime.CompilerServices.AsyncStateMachineAttribute), false))
                throw new ArgumentException("中止操作必须同步完成，不接受 async void。", nameof(abort));
            if (State != EmberLifetimeState.Starting && State != EmberLifetimeState.Ready)
                throw new InvalidOperationException("会话已经停止接收资源所有权。");
            var registration = new Registration(abort);
            _owners.Add(registration);
            return registration;
        }

        /// <summary>
        /// 登记纯后台工作的所有权。后台任务必须响应会话 Cancellation，且不得等待 Unity 主线程。
        /// 停止时等待后台真正结束后释放资源；超时保留所有权并报告故障，绝不提前释放。
        /// </summary>
        public IDisposable RegisterBackgroundOwner(Task completion, Action release, int timeoutMilliseconds = 2000)
        {
            if (completion == null) throw new ArgumentNullException(nameof(completion));
            if (release == null) throw new ArgumentNullException(nameof(release));
            if (timeoutMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));
            return RegisterAbort(() =>
            {
                try
                {
                    if (!completion.Wait(timeoutMilliseconds))
                        throw new TimeoutException("后台所有者未终止，不能确认资源已释放。");
                }
                catch (AggregateException) when (completion.IsCompleted) { }
                release();
            });
        }

        internal void MarkReady()
        {
            if (State == EmberLifetimeState.Starting) State = EmberLifetimeState.Ready;
        }

        internal bool BeginStop(EmberStopReason reason)
        {
            if (State != EmberLifetimeState.Starting && State != EmberLifetimeState.Ready) return false;
            State = EmberLifetimeState.Stopping;
            StopReason = reason;
            return true;
        }

        internal bool AbortOwners(bool waitForBackground = true)
        {
            bool success = true;
            StartStopOwners();
            try { _cancellation.Cancel(); }
            catch (Exception error) { success = false; Report(error); }
            for (int i = _owners.Count - 1; i >= 0; i--)
            {
                try { _owners[i].Abort(); }
                catch (Exception error) { success = false; Report(error); }
            }
            _owners.Clear();
            foreach (var owner in _stopOwners)
            {
                if (!waitForBackground || !owner.BackgroundIndependent) continue;
                try
                {
                    if (!owner.Completion.Wait(2000))
                        throw new TimeoutException("后台停止超时；所有权仍由停止任务持有，不能确认已释放。");
                    owner.Completion.GetAwaiter().GetResult();
                }
                catch (Exception error) { success = false; Report(error); }
            }
            return success;
        }

        internal void StartStopOwners()
        {
            if (_whenStopped != null) return;
            // Snapshot first: synchronous cancellation may unregister owners.
            var owners = _stopOwners.ToArray();
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _whenStopped = completion.Task;
            foreach (var owner in owners) owner.Stop();
            _ = CompleteOwnersAsync(owners, completion);
        }

        private static async Task CompleteOwnersAsync(StopOwner[] owners, TaskCompletionSource<bool> completion)
        {
            try
            {
                var tasks = new Task[owners.Length];
                for (int i = 0; i < owners.Length; i++) tasks[i] = owners[i].Completion;
                await Task.WhenAll(tasks).ConfigureAwait(false);
                completion.TrySetResult(true);
            }
            catch (Exception error) { completion.TrySetException(error); }
        }

        internal void FinishStop(bool success)
        {
            State = success ? EmberLifetimeState.Stopped : EmberLifetimeState.Faulted;
            if (WhenOwnersStopped.IsCompleted) _cancellation.Dispose();
            else _ = DisposeCancellationAsync();
        }

        private async Task DisposeCancellationAsync()
        {
            try { await WhenOwnersStopped.ConfigureAwait(false); }
            catch { /* The stop task retains the failure for its caller. */ }
            finally { _cancellation.Dispose(); }
        }
        #endregion

        #region 内部方法
        private static void Report(Exception error)
            => EmberDebug.LogError(LogTags.CoreGameLauncher, "会话所有权中止失败：" + error);

        private sealed class Registration : IDisposable
        {
            private Action _abort;
            internal Registration(Action abort) => _abort = abort;
            public void Dispose() => _abort = null;
            internal void Abort()
            {
                var abort = _abort;
                _abort = null;
                abort?.Invoke();
            }
        }

        private sealed class StopOwner : IDisposable
        {
            private Func<Task> _beginStop;
            private bool _started;
            internal readonly bool BackgroundIndependent;
            internal Task Completion { get; private set; } = Task.CompletedTask;
            internal StopOwner(Func<Task> beginStop, bool backgroundIndependent)
            { _beginStop = beginStop; BackgroundIndependent = backgroundIndependent; }
            public void Dispose() => _beginStop = null;
            internal void Stop()
            {
                if (_started) return;
                _started = true;
                // Keep the owner's closure alive while termination is pending or failed.
                var stop = _beginStop;
                try { Completion = stop?.Invoke() ?? Task.CompletedTask; }
                catch (Exception error) { Completion = Task.FromException(error); }
            }
        }
        #endregion
    }
}
