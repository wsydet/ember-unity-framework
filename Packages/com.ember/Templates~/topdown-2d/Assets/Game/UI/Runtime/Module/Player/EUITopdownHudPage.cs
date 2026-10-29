/*=============================================================
 * author       : Bingo
 * prefab name  : EUITopdownHudPage
 * ui role      : Page (EUITopdownHudPage)
 * create date  : 2026/10/28 22:14:53
==============================================================*/
using System;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.Player;

namespace Game.UI
{
    public partial class EUITopdownHudPage
    {
        private IDisposable _pause;
        private bool _busy;
        private int _generation;
        private string _message;
        private long _sessionVersion=-1;
        private float _nextRefresh;
        // ── UI 配置 ──

        // [EmberOptional:begin UIUpdate]
        /// <summary>是否需要每帧 Update；由 EUIBinding「使用 UIUpdate」生成。</summary>
        public override bool NeedUpdate => true;
        // [EmberOptional:end UIUpdate]

        // [EmberOptional:begin OnUpdate]
        public override void OnUpdate()
        {
            base.OnUpdate();
            if(UnityEngine.Time.unscaledTime>=_nextRefresh){_nextRefresh=UnityEngine.Time.unscaledTime+.1f;Refresh();}
        }
        // [EmberOptional:end OnUpdate]



        // ── 生命周期钩子（在此文件中填充业务逻辑） ──

        public override void OnInit()
        {
            // 在此处初始化业务数据和事件绑定
            base.OnInit();
            Btn_Save.onClick.AddListener(()=>Run(()=>PlayerModule.Instance.SaveProgressAsync(0)).Forget());
            Btn_SaveExit.onClick.AddListener(()=>Run(()=>PlayerModule.Instance.ExitAsync(GameLauncher.Instance.Fsm,true)).Forget());
            Btn_Load.onClick.AddListener(()=>Run(()=>Restore(false)).Forget());
            Btn_Retry.onClick.AddListener(()=>Run(()=>Restore(true)).Forget());
            Btn_Pause.onClick.AddListener(()=>{if(_pause==null)_pause=PlayerModule.Instance.Pause(this);else{_pause.Dispose();_pause=null;}Refresh();});
            Btn_Acknowledge.onClick.AddListener(()=>{PlayerModule.Instance.AcknowledgeFailedSaves();Refresh();});
        }

        public override void OnOpen(object param)
        {
            // UI 开始一次使用，处理传入参数
            base.OnOpen(param);
        }

        public override void OnShow()
        {
            // UI 变为可见
            base.OnShow();
            _generation++;_busy=false;_message=null;Refresh();
        }

        public override void OnHide()
        {
            // UI 被隐藏
            base.OnHide();
            _generation++;_pause?.Dispose();_pause=null;
        }

        public override void OnClose()
        {
            // UI 结束本次使用
            base.OnClose();
        }

        public override void OnDispose()
        {
            // 清理：注销事件、释放引用
            base.OnDispose();
            _generation++;_pause?.Dispose();_pause=null;
            foreach(var button in new[]{Btn_Save,Btn_SaveExit,Btn_Load,Btn_Retry,Btn_Pause,Btn_Acknowledge})button.onClick.RemoveAllListeners();
        }

        private async UniTask Run(Func<UniTask<bool>> action)
        {
            if(_busy)return;var generation=_generation;_busy=true;_message="正在处理…";Refresh();
            try{var success=await action();if(generation==_generation)_message=success?"操作完成。":PlayerModule.Instance.LastProgressMessage;}
            catch(Exception e){if(generation==_generation)_message=e.Message;}
            finally{if(generation==_generation){_busy=false;Refresh();}}
        }
        private async UniTask<bool> Restore(bool retry)
        {
            var player=PlayerModule.Instance;var flow=GameLauncher.Instance.Fsm;
            var generation=_generation;var sessionVersion=player.SessionVersion;
            TopdownEntryRequest entry;
            if(retry)entry=player.CreateRetryRequest();
            else using(player.BeginProgressRead(this))entry=await TopdownEntryRequest.ReadAsync(Game.Module.PlayerDataModule.Instance,0,TopdownRecoveryReason.Load);
            if(generation!=_generation||sessionVersion!=player.SessionVersion||!player.HasSession||flow.Current is not GameplayState)
                return false;
            var result=await ((Game.State.GameGameplayState)flow.GetState<GameplayState>()).EnterLevel(flow,entry).WhenInteractive;
            if(result.Status!=EmberTransitionStatus.Completed)throw new InvalidOperationException("恢复失败："+result.Status+" "+result.Error);
            return true;
        }
        private void Refresh()
        {
            var p=PlayerModule.Instance;var c=p.Controller;
            if(p.SessionVersion!=_sessionVersion){_sessionVersion=p.SessionVersion;_pause?.Dispose();_pause=null;}
            if(c==null){Txt_Stats.text="正在交接玩家…";return;}
            var context=c.Context;var state=c.CurrentState;
            Txt_Stats.text=$"HP {context.Health?.Current.ToString("0")??"—"} / {context.Health?.Maximum.ToString("0")??"—"}    {state}    {(p.IsPaused?"已暂停":"运行中")}\n{context.Weapons.Current?.Spec.DisplayName??"未装备"} · {context.Weapons.Current?.CurrentState}  [Q 切换武器]";
            Txt_Status.text=context.IsDead?"已死亡：可读取槽位 0，或重试检查点。重试恢复检查点记录的世界。":_message??p.LastProgressMessage;
            var enabled=!_busy&&!p.ProgressBusy&&!GameLauncher.Instance.Fsm.IsTransitioning&&GameLauncher.Instance.Fsm.Current is GameplayState;
            Btn_Save.interactable=Btn_SaveExit.interactable=enabled&&(state=="idle"||state=="move")&&context.Weapons.Stable;
            Btn_Load.interactable=Btn_Retry.interactable=enabled;Btn_Pause.interactable=enabled;
            Btn_Pause.GetComponentInChildren<TMPro.TMP_Text>().text=p.IsPaused?"继续 / 暂停":"暂停 / 继续";
            Btn_Acknowledge.gameObject.SetActive(Game.Module.PlayerDataModule.Instance.GetUnacknowledgedSaveResults().Count>0);
        }
    }
}
