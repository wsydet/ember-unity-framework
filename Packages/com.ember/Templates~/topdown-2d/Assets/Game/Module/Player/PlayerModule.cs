using System;
using System.Threading;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Ember.Basic;
using Ember.Core;
using Game.Input;
using Game.Module;
using UnityEngine;

namespace Game.Player
{
    [EmberModule(ModulePhase.Gameplay, Enabled = true)]
    public sealed partial class PlayerModule : EmberSingleton<PlayerModule>, IEmberModule,IEmberUpdate,IEmberFixedUpdate
    {
        #region 内部参数
        private PreparedPlayerSession _session;
        private PlayerScheme _scheme;
        private IPlayerCombatRules _combatRules;
        private IPlayerInteractionResolver _interactions;
        private GameInputButton _pauseButton;
        private readonly HashSet<PauseLease> _pauses=new();
        private GameInputRequest _pauseInput;
        private GameInputRequest _deathInput;
        private IDisposable _userPause;
        private float _previousTimeScale;
        public bool IsActive { get; private set; }
        public bool HasSession => _session != null;
        public PlayerController Controller => _session?.Controller;
        public long SessionVersion { get; private set; }
        public bool IsPaused=>_pauses.Count>0;
        public PlayerCombatActorView[] CaptureCombatActors()=>_session?.Combat.Capture()??Array.Empty<PlayerCombatActorView>();
        public event Action<PlayerController> SessionCommitted;
        public event Action<PlayerController> SessionReleased;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private void Invalidated() => StopSession();
        private sealed class PauseLease:IDisposable
        {
            internal PlayerModule Module;
            internal PauseLease(PlayerModule module)=>Module=module;
            public void Dispose(){var module=Module;Module=null;module?.ReleasePause(this);}
        }
        private void ReleasePause(PauseLease lease)
        {
            if(!_pauses.Remove(lease)||_pauses.Count!=0)return;
            Time.timeScale=_previousTimeScale;_pauseInput?.Dispose();_pauseInput=null;
            _session?.Controller.SetPaused(false);
        }
        private void Notify(Action<PlayerController> callbacks, PlayerController controller)
        {
            if (callbacks == null) return;
            foreach (Action<PlayerController> callback in callbacks.GetInvocationList())
                try { callback(controller); }
                catch (Exception error) { EmberDebug.LogError("Game.Player", "会话观察者失败：" + error); }
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        void IEmberModule.OnInit() => IsActive = true;
        void IEmberModule.ResetModuleData() { StopSession(); IsActive = false; }
        void IEmberModule.OnDestroy() { StopSession(); IsActive = false; }
        protected override void OnDestroy() { StopSession(); IsActive = false; }

        public void SetSchemeForNextSession(PlayerScheme scheme)
        {if(HasSession)throw new InvalidOperationException("不能替换活动会话配置。");_scheme=scheme;}
        public void SetCombatRulesForNextSession(IPlayerCombatRules rules)
        {if(HasSession)throw new InvalidOperationException("活动会话不能替换结算规则。");_combatRules=rules;}
        public PlayerActionRequest RequestAction(object owner,PlayerActionIntent intent)
            =>_session==null?throw new InvalidOperationException("没有活动玩家。"):_session.Runtime.Context.RequestAction(owner,intent);
        public PlayerStatusView CaptureState()=>Controller==null?default:new PlayerStatusView(Controller,IsPaused);
        public bool TryEquipWeapon(string id)=>_session!=null&&!IsPaused&&!ProgressBusy&&Controller.Context.ControlsAvailable&&!Controller.Context.IsDead&&
            (Controller.CurrentState=="idle"||Controller.CurrentState=="move")&&Controller.Context.Weapons.Equip(id);
        public void SetInteractionResolverForNextSession(IPlayerInteractionResolver resolver)
        {if(HasSession)throw new InvalidOperationException("活动会话不能替换交互解析器。");_interactions=resolver;}

        public IDisposable Pause(object owner)
        {
            if(owner==null||_session==null)throw new InvalidOperationException("暂停需要所有者与活动会话。");
            if(_pauses.Count==0)
            {
                _previousTimeScale=Time.timeScale;
                _pauseInput=_session.Input.Service.Request(this,100,new GameInputRule("gameplay",GameInputPermission.Deny));
                _session.Controller.SetPaused(true);Time.timeScale=0;
            }
            var lease=new PauseLease(this);_pauses.Add(lease);return lease;
        }

        void IEmberUpdate.Update()
        {
            if(!IsActive||_session==null)return;
            try
            {
                _session.Validate();
                if(_pauseButton.ConsumePress())
                {if(_userPause==null)_userPause=Pause(this);else{_userPause.Dispose();_userPause=null;}}
                if(!IsPaused)
                {
                    _session.Runtime.Tick(Time.deltaTime);_session.Combat.Tick(Time.deltaTime);
                    if(_session.Runtime.Context.ConsumeInteraction()&&
                        _session.Interactions?.TryInteract(new PlayerInteractionIntent(Controller.Body.position,Controller.Context.Facing))!=true&&_session.World.Interact())
                    {
                        if(TryCaptureSnapshot(out var checkpoint,out var reason)){_checkpoint=checkpoint;LastProgressMessage="检查点已更新，世界进度将一同保存。";}
                        else LastProgressMessage=reason;
                    }
                }
                if(_session.Runtime.Context.IsDead&&_deathInput==null)
                    _deathInput=_session.Input.Service.Request(_session,60,new GameInputRule("gameplay",GameInputPermission.Deny));
            }
            catch(Exception error){StopSession();EmberDebug.LogError("Game.Player","运行会话失败："+error);}
        }
        void IEmberFixedUpdate.FixedUpdate()
        {
            if(!IsActive||_session==null||IsPaused)return;
            try{_session.Validate();_session.Runtime.FixedTick(Time.fixedDeltaTime);_session.Combat.Step(Time.fixedDeltaTime);}
            catch(Exception error){StopSession();EmberDebug.LogError("Game.Player","物理会话失败："+error);}
        }

        public async UniTask<PreparedPlayerSession> PrepareAsync(PlayerController controller,
            GameInputModule input, PlayerDataModule data, CancellationToken cancellation = default,TopdownEntryRequest entry=null,bool replacement=false)
        {
            if (controller == null || input == null || data == null) throw new ArgumentNullException(nameof(controller));
            cancellation.ThrowIfCancellationRequested();
            var inputResult = await input.WhenReady.AsUniTask().AttachExternalCancellation(cancellation);
            var dataResult = await data.WhenReady.AsUniTask().AttachExternalCancellation(cancellation);
            await UniTask.SwitchToMainThread(cancellation);
            cancellation.ThrowIfCancellationRequested();
            if (!inputResult.Succeeded || inputResult.Epoch != input.Epoch || !dataResult.Succeeded || dataResult.Context.ServiceEpoch!=data.ServiceEpoch)
                throw new InvalidOperationException("俯视输入或存储准备失败。");
            if (input.Service.GetChannel("gameplay").IsStopped) throw new InvalidOperationException("玩法输入通道已停止。");
            var scheme=_scheme;
#if UNITY_EDITOR
            if(scheme==null&&PlayerTrialBridge.Active)scheme=PlayerTrialBridge.Scheme;
#endif
            if(scheme==null)
                foreach(var root in controller.gameObject.scene.GetRootGameObjects())
                    foreach(var identity in root.GetComponentsInChildren<TopdownLevelIdentity>(true))
                        if(identity.Scheme!=null){if(scheme!=null&&scheme!=identity.Scheme)throw new InvalidOperationException("关卡存在不同角色方案声明。");scheme=identity.Scheme;}
            if(scheme==null)scheme=Resources.Load<PlayerScheme>("Config/Player/PlayerScheme");
            if(scheme==null)throw new InvalidOperationException("缺少完整俯视玩家方案。");
            if(replacement&&(_session==null||_session.Controller!=controller||entry==null))throw new InvalidOperationException("原位恢复需要当前玩家和明确候选。");
            return new PreparedPlayerSession(this, controller, input, data,scheme,entry?.Snapshot,replacement,_combatRules,_interactions);
        }

        public void StartSession(PreparedPlayerSession candidate)
        {
            if (!IsActive || candidate == null || candidate.Owner != this || candidate.Started ||
                (_session!=null&&(!candidate.Replacement||candidate.Controller!=_session.Controller)))
                throw new InvalidOperationException("玩家模块未激活、已有会话或候选无效。");
            candidate.Validate();
            GameInputButton pauseButton=null;
            try
            {
                // 所有可预检资源及 FSM 先检查，尚未写旧身体/世界。提交之后的意外异常进入故障退出，不宣称回滚。
                candidate.Combat.ValidateSources();candidate.World.ValidateSources();candidate.Runtime.Start();
                pauseButton=new GameInputButton(candidate.Input.Service.GetChannel("ui"),"Pause");
                if(candidate.Replacement){StopSession();candidate.Controller.Reserve(candidate);}
                candidate.InputRequest = candidate.Input.Service.Request(candidate, 0,
                    new GameInputRule("gameplay", GameInputPermission.Allow));
                candidate.Runtime.BindPresentation();
                candidate.Controller.Begin(candidate);
                if(candidate.StartPosition.HasValue)candidate.Controller.Body.position=candidate.StartPosition.Value;
                candidate.Combat.Start();candidate.World.Start();
                _pauseButton=pauseButton;pauseButton=null;
                candidate.Started = true; _session = candidate; SessionVersion++;
                candidate.Controller.Invalidated += Invalidated;
                _checkpoint=candidate.Restore;
                if(_checkpoint==null&&TryCaptureSnapshot(out var initial,out _))_checkpoint=initial;
            }
            catch {pauseButton?.Dispose(); candidate.Dispose(); throw; }
            Notify(SessionCommitted, candidate.Controller);
        }

        public void StopSession()
        {
            var previous = _session; _session = null;
            if (previous == null) return;
            _progressRead?.Dispose();
            _pauseButton?.Dispose();_pauseButton=null;
            _deathInput?.Dispose();_deathInput=null;
            _pauseInput?.Dispose();_pauseInput=null;_userPause=null;
            if(_pauses.Count>0)Time.timeScale=_previousTimeScale;
            foreach(var lease in _pauses)lease.Module=null;
            _pauses.Clear();
            if (previous.Controller != null) previous.Controller.Invalidated -= Invalidated;
            SessionVersion++;
            previous.Dispose();
            Notify(SessionReleased, previous.Controller);
        }

        internal void StopSession(PreparedPlayerSession owner)
        {
            if (ReferenceEquals(_session, owner)) StopSession();
        }
        #endregion
    }
}
