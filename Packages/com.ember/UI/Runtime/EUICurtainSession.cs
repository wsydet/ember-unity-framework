using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Ember.UI
{
    public enum EUICurtainState { Covering, Covered, Revealing, Revealed, Released }

    /// <summary>
    /// 唯一遮幕凭证。所有方法在 Unity 主线程调用；Dispose/Cancel 立即放弃表现且不报告揭幕成功。
    /// 不拥有业务取消、timeScale 或玩家输入锁。业务失败后由业务决定恢复路径，再释放本凭证。
    /// </summary>
    public sealed class EUICurtainSession : IDisposable
    {
        #region 内部参数
        private readonly UniTaskCompletionSource _covered = new();
        private readonly UniTaskCompletionSource _revealed = new();
        private readonly CancellationTokenSource _abort = new();
        private readonly Func<EUICurtainSession, UniTask> _reveal;
        private readonly Action<EUICurtainSession, bool> _release;
        private bool _released;
        private Exception _failure;
        internal readonly float EnterDuration, ExitDuration;
        internal readonly bool SkipFakeProgress, SkipCustomTransition;
        internal EUITransitionPlayback Playback { get; }
        internal Action<EUIPage> PageAttached;
        internal event Action<bool> Released;
        internal Exception AnimationError { get; set; }
        public EUICurtainState State { get; private set; } = EUICurtainState.Covering;
        public UniTask Covered => _covered.Task;
        public UniTask RevealFinished => _revealed.Task;
        public bool IsPaused => Playback.IsPaused;
        internal bool IsReleased => _released;
        #endregion

        #region 内部方法
        internal EUICurtainSession(EUICurtainOptions options, Func<EUICurtainSession, UniTask> reveal,
            Action<EUICurtainSession, bool> release)
        {
            options ??= new EUICurtainOptions();
            if (float.IsNaN(options.EnterDuration) || float.IsInfinity(options.EnterDuration)
                || float.IsNaN(options.ExitDuration) || float.IsInfinity(options.ExitDuration))
                throw new ArgumentOutOfRangeException(nameof(options), "Durations must be finite.");
            EnterDuration = options.EnterDuration;
            ExitDuration = options.ExitDuration;
            SkipFakeProgress = options.SkipFakeProgress;
            SkipCustomTransition = options.SkipCustomTransition;
            Playback = new EUITransitionPlayback(options.UseScaledTime, _abort.Token) { IsPaused = options.InitiallyPaused };
            _reveal = reveal;
            _release = release;
        }

        internal void MarkCovered()
        {
            if (_released) return;
            State = EUICurtainState.Covered;
            _covered.TrySetResult();
        }

        internal void Fail(Exception error)
        {
            if (_released) return;
            _failure = error;
            Dispose();
        }

        private void NotifyReleased(bool success)
        {
            var released = Released;
            Released = null;
            if (released == null) return;
            foreach (Action<bool> observer in released.GetInvocationList())
                TryCleanup(() => observer(success));
        }

        private void TryCleanup(Action cleanup)
        {
            try { cleanup(); }
            catch (Exception error) { _failure ??= error; }
        }

        private void SetFailure(UniTaskCompletionSource completion)
        {
            if (!completion.TrySetException(_failure)) return;
            // These sources support multiple awaits. Observe the sibling phase as well: a caller
            // failing at Covered will never await RevealFinished, but can still read its same error.
            try { completion.Task.GetAwaiter().GetResult(); }
            catch (Exception) { }
        }

        private async UniTask RevealCoreAsync(CancellationToken cancellation)
        {
            try
            {
                await _reveal(this).AttachExternalCancellation(cancellation);
                if (_released) return;
                if (AnimationError != null) throw AnimationError;
                State = EUICurtainState.Revealed;
                _release(this, true);
                _released = true;
                _abort.Dispose();
                NotifyReleased(true);
                if (_failure == null) _revealed.TrySetResult();
                else SetFailure(_revealed);
            }
            catch (Exception error) { Fail(error); }
        }
        #endregion

        #region 外部方法
        /// <summary>仅冻结本会话动画；不读取业务内部暂停原因，也不修改 timeScale。</summary>
        public void SetPaused(bool paused)
        {
            if (!_released) Playback.IsPaused = paused;
        }

        /// <summary>必须先等待 Covered。重复调用共享第一次揭幕；首次调用的令牌取消会放弃本会话。</summary>
        public UniTask RevealAsync(CancellationToken cancellation = default)
        {
            if (State == EUICurtainState.Covering)
                throw new InvalidOperationException("Wait for Covered before revealing.");
            if (State == EUICurtainState.Covered)
            {
                State = EUICurtainState.Revealing;
                RevealCoreAsync(cancellation).Forget();
            }
            return _revealed.Task;
        }

        public void Cancel() => Dispose();

        /// <summary>立即取消动画并清除本会话视图/展示持有。不会回滚已经提交的业务。</summary>
        public void Dispose()
        {
            if (_released) return;
            _released = true;
            State = EUICurtainState.Released;
            TryCleanup(() => _abort.Cancel());
            TryCleanup(() => _release(this, false));
            NotifyReleased(false);
            if (_failure != null && _failure is not OperationCanceledException)
            {
                SetFailure(_covered);
                SetFailure(_revealed);
            }
            else
            {
                _covered.TrySetCanceled();
                _revealed.TrySetCanceled();
            }
            _abort.Dispose();
        }

        public UniTask DisposeAsync() { Dispose(); return UniTask.CompletedTask; }
        #endregion
    }

    internal sealed class EUICurtainOpenArgs
    {
        internal EUICurtainSession Session;
        internal object Args;
    }
}
