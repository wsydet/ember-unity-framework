using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.Module;
using Game.PlayerData;

namespace Game.Player
{
    public sealed partial class PlayerModule
    {
        #region 内部参数
        private TopdownSnapshot _checkpoint;
        private readonly HashSet<long> _pendingSaves=new();
        private ProgressReadLease _progressRead;
        public bool ProgressBusy {get;private set;}
        public bool CanLeaveGameplay=>!ProgressBusy&&_pendingSaves.Count==0;
        public string LastProgressMessage {get;private set;}="";
        private sealed class ProgressReadLease:IDisposable
        {
            private PlayerModule _module;
            private readonly IDisposable _pause;
            internal ProgressReadLease(PlayerModule module,object owner){_module=module;_pause=module.Pause(owner);}
            public void Dispose()
            {
                var module=_module;_module=null;
                if(module==null)return;
                if(ReferenceEquals(module._progressRead,this)){module._progressRead=null;module.ProgressBusy=false;}
                _pause.Dispose();
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>读档准备期间阻止其他退出/恢复请求；准备结束后同步释放，再交给正式转场接管。</summary>
        public IDisposable BeginProgressRead(object owner)
        {
            if(owner==null||_session==null||!CanLeaveGameplay)throw new InvalidOperationException("没有可读取的会话或其他进度操作未完成。");
            var lease=new ProgressReadLease(this,owner);_progressRead=lease;ProgressBusy=true;return lease;
        }
        public bool TryCaptureSnapshot(out TopdownSnapshot snapshot,out string reason)
        {
            snapshot=null;reason=null;
            try
            {
                if(_session==null||_session.Disposed||!_session.Started)throw new InvalidOperationException("没有活动玩家会话。");
                _session.Validate();var c=_session.Runtime.Context;
                if(!_session.World.Persistable)throw new InvalidOperationException("关卡未登记 TopdownLevelIdentity，不能保存。");
                if(Controller.CurrentState!="idle"&&Controller.CurrentState!="move"||c.IsDead||c.HasPendingHit)
                    throw new InvalidOperationException("当前动作不允许保存："+Controller.CurrentState+"；请等待稳定移动状态。");
                var effects=c.Effects.CaptureRestorableTimed();
                snapshot=new TopdownSnapshot(_session.World.Scene,_session.World.LevelId,_session.World.SpawnId,c.Scheme.Id,
                    Controller.Body.position,c.Facing,_session.World.Checkpoint,c.Scheme.CombatEnabled,c.Health?.Current??0,
                    c.Powers.Capture(),effects,_session.Combat.CaptureProgress(),_session.World.Capture(),c.Weapons.Capture());return true;
            }
            catch(Exception e){reason=e.Message;return false;}
        }

        /// <summary>同步捕获玩家和世界后才接受异步写入；后续移动不改变已接受的字节。</summary>
        public async UniTask<bool> SaveProgressAsync(int slot,CancellationToken cancellation=default)
        {
            if(ProgressBusy){LastProgressMessage="进度操作尚未完成。";return false;}
            if(!TryCaptureSnapshot(out var snapshot,out var reason)){LastProgressMessage=reason;return false;}
            var data=_session.Data;var version=SessionVersion;
            var operation=data.SaveAsync(slot,snapshot,new SaveSummary(snapshot.LevelId,"俯视检查点 · 玩家与世界"),new TopdownSnapshotCodec(),cancellation);
            _pendingSaves.Add(operation.Context.RequestId);
            try
            {
                var result=await operation.Completion;await UniTask.SwitchToMainThread();
                if(SessionVersion!=version)return false;
                LastProgressMessage=result.Succeeded?"已保存到槽位 "+slot:"保存失败："+result.Status+" "+result.Error;
                if(result.Succeeded)data.AcknowledgeResults(new[]{result.Context.RequestId});
                return result.Succeeded;
            }
            finally{_pendingSaves.Remove(operation.Context.RequestId);}
        }

        /// <summary>退出关闭存储入口并等待已接受写入；失败恢复入口并留在当前会话。</summary>
        public async UniTask<bool> ExitAsync(EmberStateMachine flow,bool saveFirst,int slot=0,CancellationToken cancellation=default)
        {
            if(flow==null||_session==null||ProgressBusy)return false;
            if(saveFirst&&!await SaveProgressAsync(slot,cancellation))return false;
            var current=_session;var data=current.Data;var version=SessionVersion;DrainTicket ticket=null;IDisposable pause=null;
            ProgressBusy=true;
            try
            {
                pause=Pause(this);ticket=data.BeginDrain();var drained=await data.DrainAsync(ticket,cancellation);await UniTask.SwitchToMainThread();
                if(SessionVersion!=version||_session!=current)return false;
                if(drained.Status!=DrainStatus.Drained){LastProgressMessage="退出未完成："+drained.Status+"；写入失败或等待被取消，当前会话保留。";return false;}
                ProgressBusy=false;
                var result=await flow.RequestTransitionTo<MainState>(cancellation:cancellation).WhenInteractive;await UniTask.SwitchToMainThread();
                LastProgressMessage=result.Status==EmberTransitionStatus.Completed?"已返回主菜单。":"返回主菜单失败："+result.Status;
                return result.Status==EmberTransitionStatus.Completed;
            }
            finally{if(ticket!=null)data.ResumeRequests(ticket);pause?.Dispose();ProgressBusy=false;}
        }

        public TopdownEntryRequest CreateRetryRequest()
        {
            if(_session==null||_checkpoint==null||ProgressBusy)throw new InvalidOperationException("没有可用检查点或进度操作未完成。");
            return new TopdownEntryRequest(_checkpoint.ForRetry(_session.Runtime.Scheme.Combat.MaximumHealth),TopdownRecoveryReason.Retry);
        }
        /// <summary>显示失败明细后由用户明确确认；不在失败路径静默清掉存储错误。</summary>
        public void AcknowledgeFailedSaves()
        {
            if(_session==null)return;var results=_session.Data.GetUnacknowledgedSaveResults();var ids=new List<long>();
            foreach(var item in results)ids.Add(item.Context.RequestId);_session.Data.AcknowledgeResults(ids);LastProgressMessage="已确认历史写入结果；可重试保存或退出。";
        }
        #endregion
    }
}
