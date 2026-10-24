using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Game.SceneFlow
{
    /// <summary>同一场景会话内的单个替换候选。准备、提交及清理都不重新启动其他参与者。</summary>
    public sealed class GameSceneReplacement : IDisposable
    {
        #region 内部参数
        private readonly GameSceneSession _session;
        private readonly int _index;
        private readonly IGameScenePreparedParticipant _previous;
        private readonly IGameSceneReplaceableParticipant _participant;
        private readonly CancellationTokenSource _cancellation;
        private readonly GameScenePreparationContext _context;
        private IGameScenePreparedParticipant _candidate;
        private bool _preparing = true, _released, _committing;
        public long RequestId => _context.RequestId;
        public bool IsPrepared { get; private set; }
        public bool IsCommitted { get; private set; }
        public bool IsDisposed { get; private set; }
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        internal GameSceneReplacement(GameSceneSession session, int index, IGameScenePreparedParticipant previous,
            IGameSceneReplaceableParticipant participant, long requestId, Scene scene, object args,
            CancellationToken sessionCancellation, CancellationToken requestCancellation)
        {
            _session = session; _index = index; _previous = previous; _participant = participant;
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(sessionCancellation, requestCancellation);
            _context = new GameScenePreparationContext(requestId, scene, args, _cancellation.Token);
        }

        private void Validate()
        {
            if (IsDisposed) throw new OperationCanceledException("替换候选已释放。");
            _context.Cancellation.ThrowIfCancellationRequested();
            _session.ValidateReplacement(this);
        }

        private void Release()
        {
            if (_released || _preparing || _committing) return;
            _released = true;
            _cancellation.Dispose();
            _session.ReleaseReplacement(this);
        }

        internal async UniTask PrepareAsync()
        {
            try
            {
                Validate();
                var candidate = await _participant.PrepareReplacementAsync(_context, _previous);
                if (candidate == null || ReferenceEquals(candidate, _previous) || _session.OwnsResult(candidate))
                    throw new InvalidOperationException("替换必须返回独立的新结果，不能复用旧结果。");
                _candidate = candidate;
                Validate();
                IsPrepared = true;
            }
            catch (Exception failure)
            {
                // 即使 Dispose 已在等待期间发生，也必须清理刚返回的迟到结果。
                IsDisposed = true;
                var candidate = _candidate; _candidate = null;
                try { candidate?.Dispose(); }
                catch (Exception cleanup) { _session.RecordReleaseFailure(cleanup); throw new AggregateException(failure, cleanup); }
                throw;
            }
            finally { _preparing = false; if (IsDisposed) Release(); }
        }

        internal void Transfer()
        { IsCommitted = true; _candidate = null; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        /// <summary>由公共原位协议的提交钩子调用。Start 异常只清理候选；业务副作用不自动回滚。</summary>
        public void Commit()
        {
            if (!IsPrepared || IsCommitted || _committing || IsDisposed)
                throw new InvalidOperationException("替换候选不能提交。");
            try
            {
                Validate(); _committing = true;
                _session.CommitReplacement(this, _index, _previous, _candidate);
            }
            catch (Exception failure)
            {
                if (!IsCommitted)
                {
                    try { Dispose(); }
                    catch (Exception cleanup) { throw new AggregateException(failure, cleanup); }
                }
                throw;
            }
            finally
            {
                _committing = false;
                if (IsCommitted || IsDisposed) Release();
            }
        }

        public void Dispose()
        {
            if (IsDisposed || IsCommitted) return;
            IsDisposed = true;
            var candidate = _candidate; _candidate = null;
            try { _cancellation.Cancel(); }
            finally
            {
                try { candidate?.Dispose(); }
                catch (Exception cleanup) { _session.RecordReleaseFailure(cleanup); throw; }
                finally { Release(); }
            }
        }
        #endregion
    }
}
