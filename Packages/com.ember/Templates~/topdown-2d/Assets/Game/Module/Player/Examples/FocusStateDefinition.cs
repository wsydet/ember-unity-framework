using System;
using System.Collections.Generic;
using Ember.Core.Fsm;
using Game.Entity;
using UnityEngine;

namespace Game.Player.Examples
{
    [CreateAssetMenu(menuName="Game/Player/Examples/Focus State")]
    public sealed class FocusStateDefinition:EmberFsmStateDefinition<PlayerContext,FocusState>
    {
        #region 编辑器面板参数
        [SerializeField,Min(.05f)] private float _duration=.5f;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override FocusState CreateState(){if(!float.IsFinite(_duration)||_duration<=0)throw new InvalidOperationException("Focus 时长必须正有限。");return new FocusState(_duration);}
        public override IReadOnlyList<EmberFsmTransition> GetTransitions()=>new EmberFsmTransition[]{FocusState.Finish};
        #endregion
    }
    public sealed class FocusState:EmberFsmState<PlayerContext>
    {
        #region 内部参数
        private readonly float _duration;
        private float _elapsed;
        private PlayerActionControl _control;
        private EntityEffectHandle _effect;
        internal static readonly EmberFsmTransition<PlayerContext,FocusState> Finish=new("finish-focus","locomotion",description:"定时结束，Exit 释放运动/朝向与 Owned 外观",
            sites:new[]{new EmberFsmRequestSite<PlayerContext,FocusState>("finish",EmberFsmTriggerPhase.FixedUpdate,"finish",10,(s,c)=>s._elapsed>=s._duration,"Focus 已完成配置时长")});
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public FocusState(float duration)=>_duration=duration;
        public override EmberFsmEntryDecision CanEnter()=>Context.CanUseSkill("focus")?EmberFsmEntryDecision.Allow:EmberFsmEntryDecision.Reject;
        public override void Enter(EmberFsmActivation activation)
        {
            base.Enter(activation);if(!Context.TryCommitSkill("focus"))throw new InvalidOperationException("Focus 资源提交失败。");
            _elapsed=0;_control=Context.AcquireActionControl(this);
            _effect=Context.Effects.ApplyCaptured(new EntityEffectSpec("focus-glow",EntityEffectKind.Buff,EntityEffectLifetime.Owned,0,
                new EntityEffectRules(Array.Empty<string>(),false,0),new EntityEffectAppearance(true,3,.45f,2)),"example.focus");
        }
        public override void FixedUpdate(float dt){_control.SetVelocity(Vector2.zero);_elapsed+=dt;Activation.Attempt(Finish);}
        public override void Exit(){_effect?.Dispose();_effect=null;_control?.Dispose();_control=null;}
        public override void Dispose()=>Exit();
        #endregion
    }
}
