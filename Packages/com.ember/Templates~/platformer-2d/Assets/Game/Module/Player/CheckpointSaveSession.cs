using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.Module;
using Game.PlayerData;

namespace Game.Player
{
    /// <summary>一个项目轮次的写入入口。所有该轮次写入须经此实例；新轮次先排空旧队列。</summary>
    public sealed class CheckpointSaveSession<T>
    {
        #region 内部参数

        private readonly PlayerDataModule _data;
        private readonly CheckpointProgressCodec<T> _codec;
        private readonly long _epoch;
        private bool _changing;
        public string RunId { get; private set; }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void Check(string expectedRun)
        {
            if (_changing || expectedRun != RunId || _epoch != _data.ServiceEpoch)
                throw new InvalidOperationException("保存请求属于旧轮次、旧服务或轮次正在切换。");
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public CheckpointSaveSession(PlayerDataModule data, CheckpointProgressCodec<T> codec, string runId)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data)); _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("轮次ID不能为空。");
            _epoch = data.ServiceEpoch; RunId = runId;
        }
        public SaveOperation<SaveResult> Save(string expectedRun, int slot, string levelId,
            PlayerCheckpointRecord checkpoint, T latestProject, SaveSummary summary, CancellationToken cancellation = default)
        {
            Check(expectedRun);
            return _data.SaveAsync(slot, new CheckpointProgress<T>(RunId, levelId, checkpoint, latestProject), summary, _codec, cancellation);
        }
        public async UniTask BeginRunAsync(string newRunId, CancellationToken cancellation = default)
        {
            Check(RunId);
            if (string.IsNullOrWhiteSpace(newRunId) || newRunId == RunId) throw new ArgumentException("新轮次需要新的稳定ID。");
            _changing = true; DrainTicket ticket = null;
            try
            {
                ticket = _data.BeginDrain();
                var result = await _data.DrainAsync(ticket, cancellation);
                if (result.Status != DrainStatus.Drained || _epoch != _data.ServiceEpoch)
                    throw new InvalidOperationException("旧轮次队列未成功排空。");
                cancellation.ThrowIfCancellationRequested();
                _data.AcknowledgeResults(result.Operations.Select(item => item.Context.RequestId).ToArray());
                RunId = newRunId;
            }
            finally
            {
                try { if (ticket != null) _data.ResumeRequests(ticket); }
                finally { _changing = false; }
            }
        }
        public UniTask<bool> RestoreAsync(CheckpointProgress<T> saved, PlayerCheckpointRecovery recovery,
            PlayerModule player, EmberStateMachine flow, Action<T> restoreProject, CancellationToken cancellation = default)
        {
            if (saved == null || restoreProject == null) throw new ArgumentNullException(nameof(saved));
            Check(saved.RunId);
            recovery.SetBaseline(saved.Checkpoint);
            return recovery.RecoverAsync(player, flow, Game.Entity.WorldResetReason2D.Load, cancellation, () => restoreProject(saved.Project));
        }
        public async UniTask<PlayerExitResult> SaveAndExitAsync(string expectedRun, int slot, string levelId,
            PlayerCheckpointRecord checkpoint, T latestProject, SaveSummary summary, PlayerModule player,
            EmberStateMachine flow, CancellationToken cancellation = default)
        {
            Check(expectedRun);
            using var pause = player.Pause();
            var save = Save(expectedRun, slot, levelId, checkpoint, latestProject, summary, cancellation);
            _changing = true;
            try
            {
                var result = await save.Completion;
                if (!result.Succeeded) return new PlayerExitResult(PlayerExitStatus.SaveFailed, result);
                return await player.ExitToMenuAsync(flow, null, cancellation);
            }
            finally { _changing = false; }
        }

        #endregion
    }
}
