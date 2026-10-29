using System;
using Game.Input;
using Game.Module;
using Game.PlayerData;

namespace Game.Player
{
    /// <summary>本次准备的独立所有权。旧结果只释放自己，不能结束后来会话。</summary>
    public sealed class PreparedPlayerSession : IDisposable
    {
        #region 内部参数
        internal readonly PlayerModule Owner;
        internal readonly GameInputModule Input;
        internal readonly PlayerDataModule Data;
        internal readonly long InputEpoch, DataEpoch;
        internal GameInputRequest InputRequest;
        internal PlayerRuntime Runtime {get;}
        internal PlayerCombatScene Combat {get;}
        internal PlayerWorldSession World {get;}
        internal TopdownSnapshot Restore {get;}
        internal bool Replacement {get;}
        internal IPlayerInteractionResolver Interactions {get;}
        internal UnityEngine.Vector2? StartPosition {get;}
        public PlayerController Controller { get; }
        public bool Started { get; internal set; }
        public bool Disposed { get; private set; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal PreparedPlayerSession(PlayerModule owner, PlayerController controller, GameInputModule input, PlayerDataModule data,PlayerScheme scheme,
            TopdownSnapshot restore=null,bool replacement=false,IPlayerCombatRules rules=null,IPlayerInteractionResolver interactions=null)
        {
            Owner = owner; Controller = controller; Input = input; Data = data;
            InputEpoch = input.Epoch; DataEpoch = data.ServiceEpoch;
            Restore=restore;Replacement=replacement;Interactions=interactions;
            StartPosition=restore?.Position;
#if UNITY_EDITOR
            if(restore==null&&PlayerTrialBridge.Active)StartPosition=PlayerTrialBridge.Position;
#endif
            Runtime=new PlayerRuntime(controller,scheme,input.Service.GetChannel("gameplay"),restore,rules);
            try
            {
                if(StartPosition.HasValue&&!Runtime.Context.Motor.CanOccupy(StartPosition.Value))throw new InvalidOperationException("试玩或恢复出生点与实体墙重叠。");
                World=new PlayerWorldSession(Runtime.Context,restore);Combat=new PlayerCombatScene(Runtime.Context,restore,rules);
                if(!replacement)controller.Reserve(this);
            }
            catch{try{Combat?.Dispose();World?.Dispose();}finally{Runtime.Dispose();}throw;}
        }

        internal void Validate()
        {
            if (Disposed || Controller == null || !Controller.isActiveAndEnabled || Input.State != GameInputServiceState.Ready ||
                Input.Epoch != InputEpoch || Data.State != SaveServiceState.Ready && !(Started&&Data.State==SaveServiceState.Draining) || Data.ServiceEpoch != DataEpoch)
                throw new InvalidOperationException("俯视候选或输入/存储服务已失效。");
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Owner.StopSession(this);
            InputRequest?.Dispose(); InputRequest = null;
            try{try{Combat.Dispose();World.Dispose();}finally{Runtime.Dispose();}}finally{if (Controller != null) Controller.Release(this);}
        }
        #endregion
    }
}
