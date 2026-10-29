using System.Collections.Generic;
using System.Linq;
using Ember.Core.Fsm;
using Game.Input;

namespace Game.Player.Examples
{
    /// <summary>用继承扩展角色动作入口，保留基础移动/冲刺规则；武器攻击由独立宿主驱动。</summary>
    [UnityEngine.CreateAssetMenu(menuName="Game/Player/Examples/Focus Locomotion")]
    public sealed class ExampleLocomotionDefinition:EmberFsmStateDefinition<PlayerContext,ExampleLocomotionState>
    {
        #region 外部方法
        public override ExampleLocomotionState CreateState()=>new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions()=>PlayerStateDefinition.DeclarationsFor(PlayerStateKind.Locomotion).Concat(new[]{ExampleLocomotionState.Focus}).ToArray();
        #endregion
    }
    public sealed class ExampleLocomotionState:PlayerState
    {
        #region 内部参数
        private GameInputButton _button;
        private bool _pending;
        internal static readonly EmberFsmTransition<PlayerContext,ExampleLocomotionState> Focus=new("example-focus","focus",description:"F 新按下；预检只查询，Focus.Enter 提交资源",
            sites:new[]{new EmberFsmRequestSite<PlayerContext,ExampleLocomotionState>("focus",EmberFsmTriggerPhase.FixedUpdate,"example",10,(s,c)=>s._pending&&c.CanUseSkill("focus"),"新 Focus 请求且资源可用；先于基础动作段")});
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public ExampleLocomotionState():base(PlayerStateKind.Locomotion){}
        public override void Initialize(PlayerContext context,string nodeId){base.Initialize(context,nodeId);_button=context.CreateInputButton("Focus");}
        public override void FixedUpdate(float dt)
        {_pending=_button.ConsumePress();if(_pending&&Activation.Attempt(Focus).ShouldStop)return;base.FixedUpdate(dt);}
        public override void Dispose(){_button?.Dispose();base.Dispose();}
        #endregion
    }
}
