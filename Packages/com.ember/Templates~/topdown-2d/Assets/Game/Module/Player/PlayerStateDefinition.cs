using System.Collections.Generic;
using Ember.Core.Fsm;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    public enum PlayerStateKind { Alive=0, Locomotion=1, Idle=2, Move=3, Action=4, Dash=6, Hit=7, Dead=8 }

    [CreateAssetMenu(menuName="Game/Player/Topdown State",fileName="PlayerState")]
    public class PlayerStateDefinition:EmberFsmStateDefinition<PlayerContext,PlayerState>
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerStateKind _kind;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void Configure(PlayerStateKind kind)=>_kind=kind;
        public override PlayerState CreateState()=>new(_kind);
        public override IReadOnlyList<EmberFsmTransition> GetTransitions()=>DeclarationsFor(_kind);
        [Ember.Basic.HasGC]
        public static IReadOnlyList<EmberFsmTransition> DeclarationsFor(PlayerStateKind kind)=>kind switch
        {
            PlayerStateKind.Alive=>new EmberFsmTransition[]{PlayerState.ToDead,PlayerState.ToHit},
            PlayerStateKind.Locomotion=>new EmberFsmTransition[]{PlayerState.ToDash},
            PlayerStateKind.Idle=>new EmberFsmTransition[]{PlayerState.ToMove},PlayerStateKind.Move=>new EmberFsmTransition[]{PlayerState.ToIdle},
            PlayerStateKind.Dash or PlayerStateKind.Hit=>new EmberFsmTransition[]{PlayerState.ToLocomotion},
            _=>System.Array.Empty<EmberFsmTransition>()
        };
        #endregion
    }

    public class PlayerState:EmberFsmState<PlayerContext>
    {
        #region 内部参数
        private readonly PlayerStateKind _kind;
        private float _elapsed;
        private EntityEffectHandle _actionEffect;
        internal static readonly EmberFsmTransition<PlayerContext,PlayerState> ToMove=new("start-move","move",description:"输入向量非零，进入移动表现",
            sites:new[]{Site("start-move","locomotion",10,(s,c)=>c.Move.sqrMagnitude>.0001f,"Move.sqrMagnitude > 0.0001")});
        internal static readonly EmberFsmTransition<PlayerContext,PlayerState> ToIdle=new("stop-move","idle",description:"输入归零，进入待机表现",
            sites:new[]{Site("stop-move","locomotion",10,(s,c)=>c.Move.sqrMagnitude<=.0001f,"Move.sqrMagnitude ≤ 0.0001")});
        internal static readonly EmberFsmTransition<PlayerContext,PlayerState> ToDash=new("dash","dash",description:"开始配置方向的冲刺；先于同时攻击请求",
            sites:new[]{Site("dash","action",10,(s,c)=>c.DashPending&&c.CanUse("dash"),"新冲刺请求 && 战斗启用 && 存活 && 输入许可 && dash资源可用")});
        internal static readonly EmberFsmTransition<PlayerContext,PlayerState> ToHit=new("hit","hit",description:"已结算的受击请求在统一物理步提交",
            mode:EmberFsmTransitionMode.ReenterTargetBranch,sites:new[]{Site("hit","life",20,(s,c)=>c.HasPendingHit&&!c.IsDead,"存活 && 已接受待提交受击；重入 Hit 更新本次反应")});
        internal static readonly EmberFsmTransition<PlayerContext,PlayerState> ToDead=new("dead","dead",description:"死亡优先于受击与动作",
            sites:new[]{Site("dead","life",10,(s,c)=>c.IsDead,"生命已装配 && HP ≤ 0")});
        internal static readonly EmberFsmTransition<PlayerContext,PlayerState> ToLocomotion=new("finish","locomotion",description:"动作或硬直结束后交还移动",
            sites:new[]{Site("finish","finish",10,(s,c)=>s._kind switch{PlayerStateKind.Dash=>s._elapsed>=c.Scheme.Combat.DashDuration,PlayerStateKind.Hit=>c.HitRemaining<=0,_=>false},"冲刺达到配置时长，或受击剩余时间归零")});
        private static EmberFsmRequestSite<PlayerContext,PlayerState> Site(string id,string segment,int order,
            System.Func<PlayerState,PlayerContext,bool> condition,string description)
            =>new(id,EmberFsmTriggerPhase.FixedUpdate,segment,order,condition,description);
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public PlayerState(PlayerStateKind kind)=>_kind=kind;
        public override EmberFsmEntryDecision CanEnter()
        {
            if(_kind==PlayerStateKind.Dash&&!Context.CanUse("dash"))
                return EmberFsmEntryDecision.Reject;
            return EmberFsmEntryDecision.Allow;
        }
        public override void Enter(EmberFsmActivation activation)
        {
            base.Enter(activation);_elapsed=0;
            if(_kind==PlayerStateKind.Dash)
            {
                Context.CommitSkill("dash");
                Context.ActionDirection=Context.Scheme.Combat.DashDirection==PlayerDashDirection.MoveElseAim&&Context.Move.sqrMagnitude>.0001f?Context.Move.normalized:Context.Facing;
                Context.Facing=Context.ActionDirection;Context.FacingLocked=true;
                if(Context.Scheme.Combat.DashInvulnerable)_actionEffect=Context.Effects.ApplyCaptured(Context.Scheme.Combat.DashEffect(),"dash-action");
            }
            else if(_kind==PlayerStateKind.Hit)Context.BeginReaction();
            else if(_kind==PlayerStateKind.Dead){Context.ClearInput();Context.FacingLocked=false;}
        }
        public override void Exit()
        {
            _actionEffect?.Dispose();_actionEffect=null;
            if(_kind==PlayerStateKind.Dash)Context.FacingLocked=false;
        }
        public override void FixedUpdate(float dt)
        {
            if(_kind==PlayerStateKind.Alive)
            {
                if(Activation.AttemptGroup("life").ShouldStop)return;
            }
            else if(_kind==PlayerStateKind.Locomotion)
            {
                if(Activation.AttemptGroup("action").ShouldStop)return;
                Context.MoveStep(dt);
            }
            else if(_kind==PlayerStateKind.Idle||_kind==PlayerStateKind.Move)Activation.AttemptGroup("locomotion");
            else if(_kind==PlayerStateKind.Dash)
            {
                _elapsed+=dt;Context.Velocity=Context.ActionDirection*Context.Scheme.Combat.DashSpeed;
                if(_elapsed>=Context.Scheme.Combat.DashDuration)Context.Velocity=Vector2.zero;
                Activation.Attempt(ToLocomotion);
            }
            else if(_kind==PlayerStateKind.Hit)
            {Context.ReactionStep(dt);Activation.Attempt(ToLocomotion);}
            else if(_kind==PlayerStateKind.Dead)Context.Velocity=Vector2.zero;
        }
        #endregion
    }
}
