using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Game.PlayerData;

namespace Game.Player
{
    /// <summary>业务保存结果在主线程恢复目标记账之后才完成；不改变通用存储协议。</summary>
    public sealed class PlayerSaveOperation
    {
        #region 内部参数
        public ProgressRequestContext Context { get; }
        public Task<SaveResult> Completion { get; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private async UniTask<SaveResult> Complete(SaveOperation<SaveResult> operation, PlayerRecoveryLedger ledger,
            PlayerRecoveryLedger.SaveTicket ticket)
        {
            var result = await operation.Completion;
            await UniTask.SwitchToMainThread();
            if (ticket != null) ledger.RecordResult(ticket, result.Context, result.Status);
            return result;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerSaveOperation(SaveOperation<SaveResult> operation, PlayerRecoveryLedger ledger, PlatformerSnapshot snapshot)
        {
            Context = operation.Context;
            // 非法负槽位仍返回存储服务的 InvalidSlot，不生成恢复票据。
            var ticket = Context.SlotId >= 0 ? ledger.Track(Context, snapshot) : null;
            Completion = Complete(operation, ledger, ticket).AsTask();
        }
        #endregion
    }
}
