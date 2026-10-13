using System;
using System.Collections.Generic;

namespace Ember.Core.Fsm.Tests
{
    public sealed class EmberFsmTestContext
    {
        public readonly List<string> Log = new List<string>();
        public readonly Dictionary<string, EmberFsmTestState> States = new Dictionary<string, EmberFsmTestState>();
        public EmberFsm<EmberFsmTestContext> Machine;
        public Action<string, string, EmberFsmTestState> Callback;
        public Func<string, EmberFsmEntryDecision> Entry;
        public Action GuardCallback;
        public int GuardCount;
        public bool Allow = true;
    }

    // 仅测试夹具，无正式业务状态。
    public sealed class EmberFsmTestDefinition : EmberFsmStateDefinition<EmberFsmTestContext, EmberFsmTestState>
    {
        #region 内部参数
        public int Parameter;
        public Action DeclarationCallback;
        public Func<EmberFsmTestState> Factory;
        public int CreateCount;
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public override EmberFsmTestState CreateState()
        { CreateCount++; return Factory == null ? new EmberFsmTestState(Parameter) : Factory(); }
        public override IReadOnlyList<EmberFsmTransition> GetTransitions()
        { DeclarationCallback?.Invoke(); return EmberFsmTestState.Edges; }
        public override IReadOnlyList<string> GetRedirectSlots() => new[] { "redirectJump", "redirectMove" };
        #endregion
    }

    public sealed class EmberFsmTestState : EmberFsmState<EmberFsmTestContext>
    {
        #region 内部参数
        public static readonly EmberFsmTransition<EmberFsmTestContext, EmberFsmTestState> Move = new EmberFsmTransition<EmberFsmTestContext, EmberFsmTestState>("move", "move", Guard);
        public static readonly EmberFsmTransition<EmberFsmTestContext, EmberFsmTestState> Jump = new EmberFsmTransition<EmberFsmTestContext, EmberFsmTestState>("jump", "jump", Guard);
        public static readonly EmberFsmTransition<EmberFsmTestContext, EmberFsmTestState> Fall = new EmberFsmTransition<EmberFsmTestContext, EmberFsmTestState>("fall", "fall", Guard);
        public static readonly EmberFsmTransition<EmberFsmTestContext, EmberFsmTestState> Dead = new EmberFsmTransition<EmberFsmTestContext, EmberFsmTestState>("dead", "dead", Guard);
        public static readonly EmberFsmTransition<EmberFsmTestContext, EmberFsmTestState> Ground = new EmberFsmTransition<EmberFsmTestContext, EmberFsmTestState>("ground", "ground", Guard);
        public static readonly EmberFsmTransition<EmberFsmTestContext, EmberFsmTestState> ReenterGround = new EmberFsmTransition<EmberFsmTestContext, EmberFsmTestState>("reenterGround", "ground", Guard, mode: EmberFsmTransitionMode.ReenterTargetBranch);
        public static readonly EmberFsmTransition[] Edges = { Move, Jump, Fall, Dead, Ground, ReenterGround };
        public int Parameter { get; }
        public int EnterCount;
        public int ExitCount;
        public int DisposeCount;
        public int InitializeCount;
        public int Ticks;
        public EmberFsmActivation Token => Activation;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private static bool Guard(EmberFsmTestState state, EmberFsmTestContext context)
        { context.GuardCount++; context.GuardCallback?.Invoke(); return context.Allow; }
        private void Notify(string phase)
        { Context.Log.Add(phase + ":" + NodeId); Context.Callback?.Invoke(phase, NodeId, this); }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public EmberFsmTestState(int parameter) { Parameter = parameter; }
        public override void Initialize(EmberFsmTestContext context, string nodeId)
        { base.Initialize(context, nodeId); InitializeCount++; context.States.Add(nodeId, this); Notify("init"); }
        public override EmberFsmEntryDecision CanEnter()
        { Notify("check"); return Context.Entry?.Invoke(NodeId) ?? EmberFsmEntryDecision.Allow; }
        public override void Enter(EmberFsmActivation activation)
        { base.Enter(activation); EnterCount++; Ticks = 0; Notify("enter"); }
        public override void Exit() { ExitCount++; Notify("exit"); }
        public override void Update(float deltaTime) { Ticks++; Notify("update"); }
        public override void FixedUpdate(float fixedDeltaTime) { Notify("fixed"); }
        public override void Pause() { Notify("pause"); }
        public override void Resume() { Notify("resume"); }
        public override void Dispose() { DisposeCount++; Notify("dispose"); }
        #endregion
    }
}
