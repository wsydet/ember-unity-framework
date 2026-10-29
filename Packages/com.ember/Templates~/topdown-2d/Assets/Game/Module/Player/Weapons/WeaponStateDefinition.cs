using System;
using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player.Weapons
{
    public enum WeaponStateKind { Ready, Windup, Active, Recovery }
    [CreateAssetMenu(menuName="Game/Player/Weapon State")]
    public sealed class WeaponStateDefinition:EmberFsmStateDefinition<WeaponContext,WeaponState>
    {
        #region 编辑器面板参数
        [SerializeField] private WeaponStateKind _kind;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void Configure(WeaponStateKind kind)=>_kind=kind;
        public override WeaponState CreateState()=>new(_kind);
        public override IReadOnlyList<EmberFsmTransition> GetTransitions()=>new EmberFsmTransition[]{WeaponState.Transition(_kind)};
        #endregion
    }
    public sealed class WeaponState:EmberFsmState<WeaponContext>
    {
        #region 内部参数
        private readonly WeaponStateKind _kind;
        private float _elapsed;
        private static readonly EmberFsmTransition<WeaponContext,WeaponState> Fire=new("fire","windup",description:"武器提交自己的 fire 资源并捕获本次方向",
            sites:new[]{new EmberFsmRequestSite<WeaponContext,WeaponState>("fire",EmberFsmTriggerPhase.FixedUpdate,"weapon",10,(s,c)=>c.FirePending&&c.CanFire,"当前武器接到攻击意图且自身资源可用")});
        private static readonly EmberFsmTransition<WeaponContext,WeaponState> Active=new("activate","active",description:"武器前摇结束，进入命中阶段",
            sites:new[]{new EmberFsmRequestSite<WeaponContext,WeaponState>("activate",EmberFsmTriggerPhase.FixedUpdate,"weapon",10,(s,c)=>s._elapsed>=c.Spec.Windup,"武器前摇达到配置时长")});
        private static readonly EmberFsmTransition<WeaponContext,WeaponState> Recover=new("recover","recovery",description:"命中阶段结束",
            sites:new[]{new EmberFsmRequestSite<WeaponContext,WeaponState>("recover",EmberFsmTriggerPhase.FixedUpdate,"weapon",10,(s,c)=>s._elapsed>=c.Spec.Active,"武器命中阶段达到配置时长")});
        private static readonly EmberFsmTransition<WeaponContext,WeaponState> Ready=new("ready","ready",description:"武器后摇结束，释放自己的控制租约",
            sites:new[]{new EmberFsmRequestSite<WeaponContext,WeaponState>("ready",EmberFsmTriggerPhase.FixedUpdate,"weapon",10,(s,c)=>s._elapsed>=c.Spec.Recovery,"武器后摇达到配置时长")});
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public WeaponState(WeaponStateKind kind)=>_kind=kind;
        internal static EmberFsmTransition Transition(WeaponStateKind kind)=>kind switch
        {WeaponStateKind.Ready=>Fire,WeaponStateKind.Windup=>Active,WeaponStateKind.Active=>Recover,_=>Ready};
        public override EmberFsmEntryDecision CanEnter()=>_kind==WeaponStateKind.Windup&&!Context.CanFire?EmberFsmEntryDecision.Reject:EmberFsmEntryDecision.Allow;
        public override void Enter(EmberFsmActivation activation)
        {base.Enter(activation);_elapsed=0;if(_kind==WeaponStateKind.Windup)Context.BeginAttack();if(_kind==WeaponStateKind.Ready)Context.EndAttack();}
        public override void FixedUpdate(float dt)
        {_elapsed+=dt;if(_kind==WeaponStateKind.Active)Context.Emit();Activation.Attempt(Transition(_kind));}
        #endregion
    }
}
