using System;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.SceneFlow;

namespace Game.Player
{
    /// <summary>原位恢复的暂停与候选所有权。准备失败只清理候选，旧身体和世界保持原值。</summary>
    public sealed class TopdownRecoveryTransition:IDisposable
    {
        #region 内部参数
        private readonly PlayerModule _player;
        private IDisposable _pause;
        private GameSceneReplacement _replacement;
        private bool _disposed;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public TopdownRecoveryTransition(PlayerModule player,EmberTransitionOperation operation)
        {_player=player;_pause=player.Pause(this);ReleaseWhenFinished(operation).Forget();}
        public async UniTask PrepareAsync(GameSceneSession scene,PlayerSceneParticipant participant,long requestId,TopdownEntryRequest entry,System.Threading.CancellationToken cancellation)
        {_replacement=await scene.PrepareReplacementAsync(participant,requestId,entry,cancellation);if(_disposed){_replacement.Dispose();throw new OperationCanceledException("恢复已取消。");}}
        public void Commit()
        {
            if(_disposed||_replacement==null)throw new InvalidOperationException("恢复未准备完成。");
            _replacement.Commit();_pause?.Dispose();_pause=_player.Pause(this);
        }
        private async UniTask ReleaseWhenFinished(EmberTransitionOperation operation)
        {await operation.Completion;await UniTask.SwitchToMainThread();Dispose();}
        public void Dispose(){if(_disposed)return;_disposed=true;try{_replacement?.Dispose();}finally{_pause?.Dispose();_pause=null;}}
        #endregion
    }
}
