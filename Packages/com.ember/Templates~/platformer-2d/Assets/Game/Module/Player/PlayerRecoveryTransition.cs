using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.SceneFlow;

namespace Game.Player
{
    /// <summary>公共原位请求的玩家适配。准备阶段整体暂停，提交后恢复运行，输入仍由公共过渡持有。</summary>
    public sealed class PlayerRecoveryTransition : IDisposable
    {
        #region 内部参数
        private readonly PlayerPauseRequest _pause;
        private GameSceneReplacement _replacement;
        private bool _disposed;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public PlayerRecoveryTransition(PlayerModule player)
        { _pause = (player ?? throw new ArgumentNullException(nameof(player))).Pause(); }

        public async UniTask PrepareAsync(GameSceneSession session, GameSceneParticipant participant,
            long requestId, PlayerEntryRequest entry, CancellationToken cancellation)
        {
            if (_disposed || _replacement != null) throw new InvalidOperationException("恢复事务不能重复准备。");
            try
            {
                var candidate = await session.PrepareReplacementAsync(participant, requestId, entry, cancellation);
                if (_disposed) { candidate.Dispose(); throw new OperationCanceledException("恢复事务已结束。"); }
                _replacement = candidate;
            }
            catch { Dispose(); throw; }
        }

        public void Commit()
        {
            if (_disposed || _replacement == null) throw new InvalidOperationException("恢复事务尚未准备。");
            try { _replacement.Commit(); }
            finally { Dispose(); }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _replacement?.Dispose(); }
            finally { _pause.Dispose(); }
        }
        #endregion
    }
}
