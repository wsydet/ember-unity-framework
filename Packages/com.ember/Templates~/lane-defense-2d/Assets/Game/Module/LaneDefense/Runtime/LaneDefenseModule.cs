using System;
using Ember.Basic;
using Ember.Core;
using Game.Input;
using UnityEngine;

namespace Game.LaneDefense
{
    [EmberModule(ModulePhase.Gameplay,Enabled=true)]
    public sealed class LaneDefenseModule : EmberSingleton<LaneDefenseModule>,IEmberModule,IEmberUpdate
    {
        #region 内部参数
        private LaneDefenseSceneLease _lease;
        private bool _active,_suspended,_userPaused;
        public BattleSession Current=>_lease?.Battle;
        public string SelectedCard {get;private set;}
        public bool DemolishMode {get;private set;}
        public string Feedback {get;private set;}="Select a card, then an empty cell.";
        public bool CanCommand=>_active&&Current!=null&&!_suspended&&Application.isFocused
            &&!GameLauncher.Instance.Fsm.IsTransitioning
            &&GameInputModule.Instance.Service?.GetChannel("gameplay").IsAvailable==true;
        public event Action<BattleSession> SessionChanged;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Changed()
        {
            var handlers=SessionChanged;if(handlers==null)return;
            foreach(Action<BattleSession> callback in handlers.GetInvocationList())
                try{callback(Current);}catch(Exception e){EmberDebug.LogError("Game.LaneDefense",e.ToString());}
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void OnInit(){_active=true;_suspended=false;_userPaused=false;}
        void IEmberModule.OnDestroy()=>Stop();
        protected override void OnDestroy()=>Stop();
        private void Stop(){_lease?.Dispose();_lease=null;_active=false;SessionChanged=null;}
        public void ResetModuleData()=>Stop();
        void IEmberUpdate.Update()
        {
            if(Current==null)return;
            Current.SetPaused(_userPaused||!CanCommand);
            _lease.Tick(Math.Min(.25f,Time.deltaTime),CanCommand&&!_userPaused);
        }
        internal void Attach(LaneDefenseSceneLease lease)
        {
            if(!_active||_lease!=null)throw new InvalidOperationException("Gameplay module not ready or already owns a scene.");
            _lease=lease;SelectedCard=null;DemolishMode=false;_userPaused=false;Changed();
        }
        internal void Detach(LaneDefenseSceneLease lease)
        {if(!ReferenceEquals(_lease,lease))return;_lease=null;SelectedCard=null;Changed();}
        public void SelectCard(string id)
        {
            if(!CanCommand||Current.Paused)return;
            foreach(var card in Current.Level.Units)if(card.Id==id){SelectedCard=id;DemolishMode=false;Feedback="Selected "+card.Label;return;}
        }
        public void CancelSelection(){SelectedCard=null;DemolishMode=false;Feedback="Selection cancelled.";}
        public void SelectDemolish(){if(!CanCommand||Current.Paused)return;SelectedCard=null;DemolishMode=true;Feedback=$"Select a unit to dismantle ({Current.Level.DemolishRefund:P0} refund).";}
        internal void SetFeedback(string message)=>Feedback=message;
        public bool StartWave(){if(!CanCommand||!Current.StartWave())return false;Feedback="Wave started.";return true;}
        public void SetUserPaused(bool paused)
        {if(Current==null||_suspended||GameLauncher.Instance.Fsm.IsTransitioning)return;_userPaused=paused;Current.SetPaused(paused);CancelSelection();}
        public void SetSuspended(bool suspended){_suspended=suspended;if(suspended)CancelSelection();}
        public bool Restart()
        {
            if(!CanCommand||_lease==null)return false;
            _lease.Restart();_userPaused=false;CancelSelection();Feedback="New battle ready.";Changed();return true;
        }
        #endregion
    }
}
