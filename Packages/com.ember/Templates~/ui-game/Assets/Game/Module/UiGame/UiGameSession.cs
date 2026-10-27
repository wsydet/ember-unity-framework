using System;

namespace Game.UiGame
{
    /// <summary>中性三步会话。旧会话释放后永远失效，暂停和导航锁独立。</summary>
    public sealed class UiGameSession : IDisposable
    {
        #region 内部参数
        public const int STEP_COUNT = 3;
        public long Generation { get; }
        public int CompletedSteps { get; private set; }
        public bool IsDisposed { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsNavigating { get; private set; }
        public bool IsComplete => CompletedSteps == STEP_COUNT;
        public bool CanAdvance => !IsDisposed && !IsPaused && !IsNavigating && !IsComplete;
        public event Action Changed;
        #endregion

        #region 外部方法
        public UiGameSession(long generation) { Generation = generation; }

        /// <summary>调用方携带当时看到的步骤，重复/迟到提交不能推进下一步。</summary>
        public bool TryAdvance(int expectedStep)
        {
            if (!CanAdvance || expectedStep != CompletedSteps) return false;
            CompletedSteps++;
            Changed?.Invoke();
            return true;
        }

        public void SetPaused(bool paused)
        {
            if (IsDisposed || IsPaused == paused) return;
            IsPaused = paused;
            Changed?.Invoke();
        }

        public bool TryBeginNavigation()
        {
            if (IsDisposed || IsPaused || IsNavigating) return false;
            IsNavigating = true;
            Changed?.Invoke();
            return true;
        }

        public void CancelNavigation()
        {
            if (IsDisposed || !IsNavigating) return;
            IsNavigating = false;
            Changed?.Invoke();
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            Changed = null;
        }
        #endregion
    }
}
