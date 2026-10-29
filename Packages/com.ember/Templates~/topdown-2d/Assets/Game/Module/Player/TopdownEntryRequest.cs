using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Module;

namespace Game.Player
{
    public enum TopdownRecoveryReason { Continue, Load, Retry }
    /// <summary>读取与校验完成的明确恢复请求；SceneFlow 不隐式读档或将坏档当新游戏。</summary>
    public sealed class TopdownEntryRequest
    {
        #region 内部参数
        public TopdownSnapshot Snapshot {get;}
        public TopdownRecoveryReason Reason {get;}
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public TopdownEntryRequest(TopdownSnapshot snapshot,TopdownRecoveryReason reason)
        {Snapshot=snapshot??throw new ArgumentNullException(nameof(snapshot));if(!Enum.IsDefined(typeof(TopdownRecoveryReason),reason))throw new ArgumentException("恢复原因无效。");Reason=reason;}
        public static async UniTask<TopdownEntryRequest> ReadAsync(PlayerDataModule data,int slot,TopdownRecoveryReason reason,CancellationToken cancellation=default)
        {
            if(data==null)throw new ArgumentNullException(nameof(data));var epoch=data.ServiceEpoch;
            var result=await data.ReadAsync(slot,new TopdownSnapshotCodec(),cancellation).Completion;
            await UniTask.SwitchToMainThread(cancellation);cancellation.ThrowIfCancellationRequested();
            if(epoch!=data.ServiceEpoch||!result.Succeeded)throw new InvalidOperationException("读取俯视进度失败："+result.Status+" "+result.Error);
            return new TopdownEntryRequest(result.Progress,reason);
        }
        #endregion
    }
}
