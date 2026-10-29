using System;
using Ember.Core.Fsm;
using Game.Input;
using Game.Entity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    /// <summary>每角色独立意图、运动和朝向。输入只产生意图，物理由宿主统一提交。</summary>
    public sealed class PlayerContext:IDisposable,IEntityReactionSink,Weapons.IWeaponActor
    {
        #region 内部参数
        private readonly GameInputChannel _channel;
        private readonly GameInputButton _attack,_dash,_interact,_nextWeapon;
        private Vector2 _lastPointer;
        private bool _pointerMode,_pointerInitialized;
        private EntityEffectBinding _effectBinding;
        private EntityHit? _pendingHit;
        private int _reactionPriority;
        private float _reactionBraking;
        private bool _disposed;
        private PlayerActionControl _actionControl;
        private readonly System.Collections.Generic.List<GameInputButton> _extensionButtons=new();
        private readonly System.Collections.Generic.List<PlayerActionRequest> _actionRequests=new();
        internal bool FacingLocked;
        internal Vector2 ActionDirection;
        public Weapons.WeaponLoadout Weapons {get;}
        public bool NextWeaponPending {get;private set;}
        Vector2 Weapons.IWeaponActor.AimDirection=>Facing;
        bool Weapons.IWeaponActor.CanOperateWeapon=>Scheme.CombatEnabled&&!IsDead&&!HasPendingHit&&ControlsAvailable&&HitRemaining<=0&&
            (_actionControl==null||ReferenceEquals(_actionControl.Owner,Weapons?.Current?.Context));
        IDisposable Weapons.IWeaponActor.AcquireWeaponControl(object owner,bool lockFacing)=>AcquireActionControl(owner,lockFacing);
        internal float HitRemaining;
        internal bool HasPendingHit=>_pendingHit.HasValue;
        internal Vector2 StepFrom,StepTo;
        public EntityPowerHost Powers {get;}
        public EntityEffectContainer Effects {get;}
        public EntityHealth Health {get;}
        public IEntityHitReceiver HitReceiver {get;}
        public bool IsDead=>Health!=null&&Health.Current<=0;
        internal EntityMotor2D Motor {get;}
        public PlayerSchemeSnapshot Scheme {get;}
        public PlayerController Controller {get;}
        public Vector2 Move {get;private set;}
        public Vector2 Facing {get;internal set;}
        public Vector2 Velocity {get;internal set;}
        public bool AttackPending {get;private set;}
        public bool DashPending {get;private set;}
        public bool InteractPending {get;private set;}
        public bool ControlsAvailable=>_channel.IsAvailable;
        internal Camera AimCamera;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void AvailabilityChanged(GameInputChannel channel){if(!channel.IsAvailable)ClearInput();}
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerContext(PlayerController controller,PlayerSchemeSnapshot scheme,GameInputChannel channel,TopdownSnapshot restore=null,IPlayerCombatRules rules=null)
        {
            Controller=controller;Scheme=scheme;Facing=scheme.DefaultFacing;_channel=channel;
            Motor=new EntityMotor2D(controller.Body,controller.GetComponent<BoxCollider2D>(),scheme.Obstacles,scheme.Skin);
            if(restore!=null&&!Motor.CanOccupy(restore.Position))throw new InvalidOperationException("快照玩家位置与实体墙重叠。");
            foreach(var action in new[]{"Attack","Dash","Interact","Move","Aim","Point","NextWeapon"})channel.GetAction(action);
            if(scheme.CombatEnabled&&controller.GetComponent<Entity2D>()==null)throw new InvalidOperationException("战斗玩家需要 Entity2D 归属根。");
            if(restore!=null&&(restore.SchemeId!=scheme.Id||restore.CombatInstalled!=scheme.CombatEnabled||
                restore.Health>(scheme.CombatEnabled?scheme.Combat.MaximumHealth:0)||scheme.CombatEnabled&&restore.Health<=0))throw new InvalidOperationException("快照角色方案或生命装配不兼容。");
            try
            {
                Powers=restore!=null?EntityPowerHost.Restore(scheme.Powers,restore.Powers):scheme.Powers==null?EntityPowerHost.CreateEmpty():EntityPowerHost.Create(scheme.Powers);
                Weapons=new Weapons.WeaponLoadout(scheme.Weapons,this,restore?.Weapons);
                Effects=restore?.RestoreEffects()??new EntityEffectContainer();
                if(scheme.CombatEnabled)
                {
                    Health=new EntityHealth(scheme.Combat.MaximumHealth,restore?.Health??scheme.Combat.MaximumHealth);
                    HitReceiver=new EntityHitReceiver(Health,Effects,this,protection:scheme.Combat.DamageProtection>0?scheme.Combat.DamageEffect():null,
                        rule:rules==null?null:new PlayerBoundHitRule(rules,"player",1,controller.Body),
                        isValid:()=>!_disposed&&Controller!=null&&Controller.IsRunning&&ReferenceEquals(Controller.Context,this));
                }
                Facing=restore?.Facing??scheme.DefaultFacing;
                _attack=new GameInputButton(channel,"Attack");_dash=new GameInputButton(channel,"Dash");_interact=new GameInputButton(channel,"Interact");
                _nextWeapon=new GameInputButton(channel,"NextWeapon");
                channel.AvailabilityChanged+=AvailabilityChanged;
            }
            catch{_attack?.Dispose();_dash?.Dispose();_interact?.Dispose();_nextWeapon?.Dispose();Weapons?.Dispose();Effects?.Dispose();Powers?.Dispose();throw;}
        }

        internal void BindPresentation()
        {
            if(!Scheme.CombatEnabled)return;
            var sprite=Controller.transform.Find("Body")?.GetComponent<SpriteRenderer>();
            _effectBinding=new EntityEffectBinding(Effects,new Collider2D[]{Controller.GetComponent<BoxCollider2D>()},sprite==null?Array.Empty<SpriteRenderer>():new[]{sprite});
        }
        internal bool ConsumeInteraction(){var pending=InteractPending;InteractPending=false;return pending;}

        internal void Sample()
        {
            if(!_channel.IsAvailable){ClearInput();return;}
            Move=Vector2.ClampMagnitude(_channel.ReadContinuous<Vector2>("Move"),1);
            var previousFacing=Facing;
            var aim=_channel.ReadContinuous<Vector2>("Aim");var point=_channel.ReadContinuous<Vector2>("Point");
            if(Scheme.AimMode==PlayerAimMode.Movement)
            {if(Move.sqrMagnitude>.0001f)Facing=Move.normalized;}
            else
            {
                var pointerChanged=Mouse.current!=null&&(!_pointerInitialized||(point-_lastPointer).sqrMagnitude>.1f);
                if(pointerChanged)_pointerMode=true;
                if(aim.sqrMagnitude>Scheme.AimDeadzone*Scheme.AimDeadzone){_pointerMode=false;Facing=aim.normalized;}
                else if(_pointerMode&&AimCamera!=null)
                {
                    var ray=AimCamera.ScreenPointToRay(point);var plane=new Plane(Vector3.forward,Controller.transform.position);
                    if(plane.Raycast(ray,out var distance))
                    {var delta=(Vector2)(ray.GetPoint(distance)-Controller.transform.position);if(delta.sqrMagnitude>.0001f)Facing=delta.normalized;}
                }
                _lastPointer=point;_pointerInitialized=Mouse.current!=null;
            }
            if(FacingLocked||_actionControl?.LocksFacing==true)Facing=previousFacing;
            AttackPending|=_attack.ConsumePress();DashPending|=_dash.ConsumePress();InteractPending|=_interact.ConsumePress();
            NextWeaponPending|=_nextWeapon.ConsumePress();
        }
        internal void MoveStep(float dt)
        {Velocity=_actionControl!=null?Vector2.zero:Vector2.MoveTowards(Velocity,Move*Scheme.Speed,(Move.sqrMagnitude>.0001f?Scheme.Acceleration:Scheme.Braking)*dt);}
        internal void ClearInput(){Move=Vector2.zero;Velocity=Vector2.zero;ClearRequests();foreach(var r in _actionRequests)r.End(false);_actionRequests.Clear();}
        internal void ClearRequests(bool includeInteraction=true){AttackPending=false;DashPending=false;NextWeaponPending=false;if(includeInteraction)InteractPending=false;}
        internal bool CanUse(string skill)=>Scheme.CombatEnabled&&!IsDead&&ControlsAvailable&&Powers.Skills.Query(skill)==EntityUseStatus.Available;
        internal void CommitSkill(string skill)
        {
            if(!Powers.Skills.TryGet(skill,out var resource)||resource.TryCommit(resource.CreateAttempt())!=EntityUseStatus.Available)
                throw new InvalidOperationException("动作资源提交失败："+skill);
        }
        /// <summary>扩展状态的输入适配；不公开 Map。状态释放后可 Dispose，Context 结束也会回收。</summary>
        public GameInputButton CreateInputButton(string action)
        {var button=new GameInputButton(_channel,action);_extensionButtons.Add(button);return button;}
        public bool CanUseSkill(string id)=>!IsDead&&ControlsAvailable&&Powers.Skills.Query(id)==EntityUseStatus.Available;
        public bool TryCommitSkill(string id)=>CanUseSkill(id)&&Powers.Skills.TryGet(id,out var resource)&&resource.TryCommit(resource.CreateAttempt())==EntityUseStatus.Available;
        public PlayerActionControl AcquireActionControl(object owner,bool lockFacing=true)
        {
            // 角色动作可抢占当前武器自己的控制；不能解除其他角色动作/项目所有者的租约。
            if(owner!=null&&_actionControl!=null&&!ReferenceEquals(_actionControl.Owner,owner)&&ReferenceEquals(_actionControl.Owner,Weapons?.Current?.Context))
                Weapons.Interrupt();
            if(owner==null||_actionControl!=null)throw new InvalidOperationException("扩展动作运动需要唯一显式所有者。");
            return _actionControl=new PlayerActionControl(this,owner,lockFacing);
        }
        public PlayerActionRequest RequestAction(object owner,PlayerActionIntent intent)
        {
            if(owner==null||!Enum.IsDefined(typeof(PlayerActionIntent),intent))throw new ArgumentException("动作请求需要所有者和合法意图。");
            if(!ControlsAvailable||IsDead)throw new InvalidOperationException("当前控制许可不接受动作请求。");
            var request=new PlayerActionRequest(this,owner,intent);_actionRequests.Add(request);return request;
        }
        internal void CancelActionRequest(PlayerActionRequest request)=>_actionRequests.Remove(request);
        internal void DispatchActionRequests()
        {
            foreach(var request in _actionRequests)
            {
                if(request.Intent==PlayerActionIntent.Attack)AttackPending=true;else if(request.Intent==PlayerActionIntent.Dash)DashPending=true;else InteractPending=true;
                request.End(true);
            }
            _actionRequests.Clear();
        }
        internal void ReleaseActionControl(PlayerActionControl control)
        {if(!ReferenceEquals(_actionControl,control))return;_actionControl=null;}
        public bool Request(in EntityHit hit)
        {
            if(IsDead||(_pendingHit.HasValue&&_pendingHit.Value.Priority>hit.Priority)||(HitRemaining>0&&_reactionPriority>hit.Priority))return false;
            _pendingHit=hit;return true;
        }
        internal void BeginReaction()
        {
            if(!_pendingHit.HasValue)throw new InvalidOperationException("受击缺少已接受请求。");
            var hit=_pendingHit.Value;_pendingHit=null;Velocity=hit.Velocity;
            HitRemaining=hit.Priority==_reactionPriority?Mathf.Max(HitRemaining,hit.StunSeconds):hit.StunSeconds;
            _reactionPriority=hit.Priority;_reactionBraking=hit.Braking;
        }
        internal void ReactionStep(float dt)
        {HitRemaining=Mathf.Max(0,HitRemaining-dt);Velocity=HitRemaining<=0?Vector2.zero:Vector2.MoveTowards(Velocity,Vector2.zero,_reactionBraking*dt);}
        public void Dispose()
        {if(_disposed)return;_disposed=true;_channel.AvailabilityChanged-=AvailabilityChanged;Weapons.Dispose();_actionControl?.Dispose();foreach(var button in _extensionButtons)button.Dispose();_extensionButtons.Clear();_attack.Dispose();_dash.Dispose();_interact.Dispose();_nextWeapon.Dispose();_effectBinding?.Dispose();Effects.Dispose();Powers.Dispose();ClearInput();}
        #endregion
    }
}
