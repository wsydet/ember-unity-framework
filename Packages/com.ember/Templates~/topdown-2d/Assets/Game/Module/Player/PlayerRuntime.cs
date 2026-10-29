using System;
using Ember.Core.Fsm;
using Game.Input;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    internal sealed class PlayerRuntime:IDisposable
    {
        #region 内部参数
        internal PlayerSchemeSnapshot Scheme {get;}
        internal PlayerContext Context {get;}
        internal EmberFsm<PlayerContext> Fsm {get;}
        internal EmberFsmGraph SourceGraph {get;}
        internal EmberFsmObservationOrigin Origin {get;}
        private readonly Transform _facing;
        private readonly SpriteRenderer _bodySprite;
        private readonly SpriteRenderer _meleeSprite;
        private readonly Color _baseBodyColor;
        private string _animation;
        private float _animationSeconds;
        private bool _disposed,_paused,_started;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerRuntime(PlayerController controller,PlayerScheme source,GameInputChannel channel,TopdownSnapshot restore=null,IPlayerCombatRules rules=null)
        {
            Scheme=source.Capture();
            try
            {
                Context=new PlayerContext(controller,Scheme,channel,restore,rules);
                Fsm=new EmberFsm<PlayerContext>(Scheme.Graph,Context);
                SourceGraph=source.Graph;Origin=EmberFsmObservationOrigin.Capture(source.Graph,Scheme.Graph);
                _facing=controller.transform.Find("Facing");
                _bodySprite=controller.transform.Find("Body")?.GetComponent<SpriteRenderer>();
                _meleeSprite=controller.transform.Find("Weapon")?.GetComponent<SpriteRenderer>();
                _baseBodyColor=controller.InitialBodyColor;
            }
            catch {Context?.Dispose();Scheme.Dispose();throw;}
        }
        internal void Start()
        {if(Fsm.Start()!=EmberFsmResult.Changed)throw new InvalidOperationException("俯视 FSM 启动失败。");Context.Weapons.Start();}
        internal void BindPresentation(){Context.BindPresentation();_started=true;}
        internal void Tick(float dt)
        {
            if(_disposed||_paused)return;Context.Sample();Context.Powers.Tick(dt);Context.Weapons.Tick(dt);Context.Effects.Tick(dt);Fsm.Update(dt);CheckFsm();
            var action=Fsm.CurrentLeafId;
            if(_animation!=action){_animation=action;_animationSeconds=0;}else _animationSeconds+=dt;
            if(_bodySprite!=null&&Scheme.TryAnimation(action,out var mapping))_bodySprite.sprite=mapping.Evaluate(Context.Facing,_animationSeconds);
            if(_bodySprite!=null)
            {var color=Context.IsDead?new Color(.2f,.24f,.3f):_baseBodyColor;color.a=_bodySprite.color.a;_bodySprite.color=color;}
            if(_meleeSprite!=null)
            {
                if(Context.IsDead||Context.Weapons.Current==null)_meleeSprite.enabled=false;
                else Context.Weapons.Current.Present(_meleeSprite,Context.Facing);
            }
            if(_facing!=null)
            {_facing.localPosition=(Vector3)(Context.Facing*.5f);_facing.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(Context.Facing.y,Context.Facing.x)*Mathf.Rad2Deg);}
        }
        internal void FixedTick(float dt)
        {
            if(_disposed||_paused)return;
            Context.StepFrom=Context.Controller.Body.position;
            Context.DispatchActionRequests();
            if(Context.IsDead||Context.HasPendingHit||Context.DashPending&&Context.CanUse("dash"))Context.Weapons.Interrupt();
            if(Context.NextWeaponPending&&Context.ControlsAvailable&&!Context.IsDead&&!Context.HasPendingHit&&
                (Fsm.CurrentLeafId=="idle"||Fsm.CurrentLeafId=="move"))Context.Weapons.Next();
            Fsm.FixedUpdate(dt);CheckFsm();
            var available=Context.ControlsAvailable&&(Fsm.CurrentLeafId=="idle"||Fsm.CurrentLeafId=="move");
            Context.Weapons.Step(dt,Context.AttackPending,available);
            if(Context.Weapons.Current?.Spec.LockMovement==true&&!Context.Weapons.Current.IsReady&&available)Context.Velocity=Vector2.zero;
            Context.Velocity=Context.Motor.Step(Context.Velocity,dt);Context.StepTo=Context.Motor.Destination;Context.ClearRequests(false);
        }
        private void CheckFsm()
        {if(Fsm.Status==EmberFsmStatus.Faulted)throw new InvalidOperationException("俯视状态机已故障："+Fsm.LastFault);}
        internal void SetPaused(bool paused)
        {if(_disposed||_paused==paused)return;_paused=paused;Context.ClearInput();Context.Weapons.SetPaused(paused);if(paused)Fsm.Pause();else Fsm.Resume();}
        public void Dispose()
        {
            if(_disposed)return;_disposed=true;
            try{Fsm.Dispose();}finally{Context.Dispose();Scheme.Dispose();if(_started&&_bodySprite!=null)_bodySprite.color=_baseBodyColor;if(_started&&_meleeSprite!=null)_meleeSprite.enabled=false;}
        }
        #endregion
    }
}
