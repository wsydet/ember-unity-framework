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
    /// 单次框架会话。停止先撤销就绪资格，再取消工作并同步释放注册的所有权。
    /// 注册的中止操作必须在返回前释放资源；不得使用 async void 或等待 Unity 主线程续体。
    /// </summary>
    public sealed class EmberLifetime
    {
        #region 内部参数
        private readonly CancellationTokenSource _cancellation = new();
        private readonly List<Registration> _owners = new();
        public EmberLifetimeState State { get; private set; } = EmberLifetimeState.Starting;
        public EmberStopReason? StopReason { get; private set; }
        public CancellationToken Cancellation { get; }
        public bool IsReady => State == EmberLifetimeState.Ready;
        #endregion

        #region 外部方法
        public EmberLifetime() => Cancellation = _cancellation.Token;

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
                catch (AggregateException) when (completion.IsCompleted)
                {
                    // 任务失败或取消仍代表后台已结束；释放所有权不代表工作成功。
                }
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

        internal bool AbortOwners()
        {
            bool success = true;
            try { _cancellation.Cancel(); }
            catch (Exception error) { success = false; Report(error); }
            for (int i = _owners.Count - 1; i >= 0; i--)
            {
                try { _owners[i].Abort(); }
                catch (Exception error) { success = false; Report(error); }
            }
            _owners.Clear();
            return success;
        }

        internal void FinishStop(bool success)
        {
            State = success ? EmberLifetimeState.Stopped : EmberLifetimeState.Faulted;
            _cancellation.Dispose();
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
        #endregion
    }
}
