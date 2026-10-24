using System;
using Ember.Core.Fsm;
using Game.Player;
using UnityEngine;

namespace ActionContractConsumer
{
    // 独立消费程序集：没有 InternalsVisibleTo，没有反射或刚体写入，也没有 Update/FixedUpdate。
    public sealed class ActionContractProbe : MonoBehaviour
    {
        public bool Requested, AllowExit = true, FailEnter;
        public PlayerActionLease Lease;
        public int Entries, Exits;
        public float CapturedDuration;
        public PlayerActionEndReason LastEnd;
        public Action OnExit;
    }

    public sealed class ActionProbeState : PlayerActionState
    {
        private readonly float _duration;
        private float _elapsed;
        private ActionContractProbe _probe;
        public static readonly EmberFsmTransition<PlayerContext, ActionProbeState> Finish = new(
            "finish", "return", (s,c) => c.Entity.GetComponent<ActionContractProbe>().AllowExit);
        public ActionProbeState(float duration) : base(Finish) => _duration = duration;
        protected override void EnterAction(PlayerActionLease action)
        {
            _probe = Context.Entity.GetComponent<ActionContractProbe>();
            _probe.Requested = false; _probe.Lease = action; _probe.Entries++; _probe.CapturedDuration = _duration;
            action.BindOwner(_probe); action.SetHorizontal(0); action.SetVertical(0, 0);
            action.SetFacing(Context.Facing.FacesRight);
            _elapsed = 0;
            if (_probe.FailEnter) throw new InvalidOperationException("动作准备失败探针");
        }
        public override void FixedUpdate(float dt)
        { _elapsed += dt; if (_elapsed >= _duration) Action.End(); }
        protected override void ExitAction(PlayerActionEndReason reason)
        { _probe.Exits++; _probe.LastEnd = reason; _probe.OnExit?.Invoke(); }
    }
}
