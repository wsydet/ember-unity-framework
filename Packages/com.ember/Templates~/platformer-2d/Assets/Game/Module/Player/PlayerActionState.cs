using System;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    [Flags]
    public enum PlayerActionChannels
    {
        None = 0, Horizontal = 1, Facing = 2, AutomaticTransitions = 4,
        Wall = 8, Ledge = 16, Vertical = 32, Correction = 64, All = 127
    }

    public enum PlayerActionEndReason
    {
        Completed, Transition, Launch, Death, Reaction, OwnerDisabled,
        SessionEnded, Disabled, Destroyed, PreparationFailed
    }

    /// <summary>一次真实 FSM 激活的动作凭证；命令经宿主唯一固定步提交。释放通道不可重新取得。</summary>
    public sealed class PlayerActionLease : IDisposable
    {
        #region 内部参数
        internal readonly PlayerActionSession Session;
        internal readonly EmberFsmActivation Activation;
        internal readonly EmberFsmTransition ExitTransition;
        internal Behaviour Owner;
        internal bool HasOwner;
        internal float? Horizontal, Vertical, Gravity;
        internal bool HoldVertical = true;
        internal bool NewVertical;
        internal PlayerFlightSpec PendingFlight;
        internal string PendingSource;
        internal Behaviour FlightOwner;
        public PlayerFlightLease Flight { get; internal set; }
        internal bool? Facing;
        public PlayerActionChannels Channels { get; internal set; }
        public PlayerActionEndReason EndReason { get; internal set; } = PlayerActionEndReason.Transition;
        public bool IsActive => Session.IsCurrent(this);
        public ulong Generation => Activation.Generation;
        public string NodeId => Activation.NodeId;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerActionLease(PlayerActionSession session, EmberFsmActivation activation,
            EmberFsmTransition exit, PlayerActionChannels channels)
        { Session = session; Activation = activation; ExitTransition = exit; Channels = channels; }

        /// <summary>可选外部组件寿命；禁用/销毁后由宿主终止动作，不依赖触发器退出。</summary>
        public void BindOwner(Behaviour owner)
        {
            if (!IsActive) return;
            if (owner == null || !owner.isActiveAndEnabled) throw new ArgumentException("动作所有者必须启用。", nameof(owner));
            Owner = owner; HasOwner = true;
        }
        public bool SetHorizontal(float velocity)
        {
            if (!float.IsFinite(velocity)) throw new ArgumentOutOfRangeException(nameof(velocity));
            if (!IsActive || (Channels & PlayerActionChannels.Horizontal) == 0) return false;
            Horizontal = velocity; return true;
        }
        /// <summary>默认保持速度；holdVelocity=false 只提交一次速度，随后由真实重力积分。重力持续到释放竖直职责。</summary>
        public bool SetVertical(float velocity, float gravityScale, bool holdVelocity = true)
        {
            if (!float.IsFinite(velocity)) throw new ArgumentOutOfRangeException(nameof(velocity));
            if (!float.IsFinite(gravityScale) || gravityScale < 0) throw new ArgumentOutOfRangeException(nameof(gravityScale));
            if (!IsActive || (Channels & PlayerActionChannels.Vertical) == 0) return false;
            Vertical = velocity; Gravity = gravityScale; HoldVertical = holdVelocity;
            NewVertical = true; PendingFlight = default; PendingSource = null; FlightOwner = null; return true;
        }
        /// <summary>提交一次纵向发射并绑定下降策略；owner 寿命独立于动作，释放 Vertical 或结束动作后策略继续。</summary>
        public bool LaunchVertical(float velocity, float gravityScale, Behaviour owner, string sourceId, PlayerFlightSpec flight)
        {
            flight.ValidateWorld(Physics2D.gravity);
            if (owner == null || !owner.isActiveAndEnabled || string.IsNullOrWhiteSpace(sourceId))
                throw new ArgumentException("飞行必须指定启用的所有者和来源 ID。");
            if (!SetVertical(velocity, gravityScale, false)) return false;
            PendingFlight = flight; PendingSource = sourceId; FlightOwner = owner; return true;
        }
        public bool SetFacing(bool facesRight)
        {
            if (!IsActive || (Channels & PlayerActionChannels.Facing) == 0) return false;
            Facing = facesRight; return true;
        }
        public bool Release(PlayerActionChannels channels) => Session.ReleaseChannels(this, channels);
        /// <summary>请求声明的退出转换；拒绝时保留动作，返回真实 FSM 结果。</summary>
        public EmberFsmResult End() => Session.TryEnd(this, PlayerActionEndReason.Completed);
        public void Dispose() => End();
        #endregion
    }

    /// <summary>每运行候选独立的动作接合点；只能由 PlayerActionState 的真实 Enter 取得。</summary>
    public sealed class PlayerActionSession
    {
        #region 内部参数
        private readonly PlayerContext _context;
        private PlayerActionLease _current;
        private EmberFsm<PlayerContext> _fsm;
        private bool _disposed, _ending;
        public bool IsActive => _current != null;
        public string NodeId => _current?.NodeId;
        public bool CanEnter => !_disposed && !_ending && !IsActive &&
            _context.Life?.IsDead != true && _context.Life?.Reaction?.IsActive != true &&
            !_context.ExternalMotion.IsInteractionLocked && !_context.MotionControl.Owns((PlayerMotionChannels)127) &&
            !_context.Dash.IsActive && !_context.Ledge.IsActive && !_context.Rolling.IsActive &&
            !_context.Landing.IsActive && !_context.DropThrough.IsActive && !_context.HeadJump.IsActive &&
            _context.Combat?.IsActive != true;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerActionSession(PlayerContext context) => _context = context;
        internal void Bind(EmberFsm<PlayerContext> fsm) => _fsm = fsm;
        internal bool IsCurrent(PlayerActionLease lease) => !_disposed && ReferenceEquals(_current, lease);
        internal PlayerActionLease Begin(EmberFsmActivation activation, EmberFsmTransition exit, PlayerActionChannels channels)
        {
            if (!CanEnter || activation.Generation == 0 || _fsm == null ||
                !_fsm.GetActivation(activation.NodeId).Equals(activation) ||
                exit == null || (channels & ~PlayerActionChannels.All) != 0)
                throw new InvalidOperationException("动作进入必须来自可用宿主的真实激活及声明的退出转换。");
            _current = new PlayerActionLease(this, activation, exit, channels);
            _context.Wall.Cancel(); _context.Support.Clear();
            _context.MotionControl.Take(_current, (PlayerMotionChannels)channels);
            return _current;
        }
        internal bool ReleaseChannels(PlayerActionLease lease, PlayerActionChannels channels)
        {
            if (!IsCurrent(lease)) return false;
            if ((channels & ~PlayerActionChannels.All) != 0) throw new ArgumentOutOfRangeException(nameof(channels));
            lease.Channels &= ~channels;
            if ((channels & PlayerActionChannels.Vertical) != 0)
            { lease.NewVertical = false; lease.Vertical = null; lease.PendingFlight = default; }
            _context.MotionControl.Take(lease, (PlayerMotionChannels)lease.Channels);
            return true;
        }
        internal EmberFsmResult TryEnd(PlayerActionLease lease, PlayerActionEndReason reason)
        {
            if (!IsCurrent(lease)) return EmberFsmResult.StaleActivation;
            if (_ending) return EmberFsmResult.Reentrant;
            _ending = true;
            var previous = lease.EndReason;
            lease.EndReason = reason;
            try
            {
                var result = lease.Activation.Request(lease.ExitTransition);
                if (IsCurrent(lease)) lease.EndReason = previous;
                return result;
            }
            finally { _ending = false; }
        }
        internal bool InterruptLaunch()
            => _current == null || TryEnd(_current, PlayerActionEndReason.Launch) == EmberFsmResult.Changed && !IsActive;
        internal bool Prune()
        {
            if (_current == null || !_current.HasOwner || _current.Owner != null && _current.Owner.isActiveAndEnabled) return true;
            return TryEnd(_current, PlayerActionEndReason.OwnerDisabled) == EmberFsmResult.Changed && !IsActive;
        }
        internal void MarkEnd(PlayerActionEndReason reason)
        { if (_current != null) _current.EndReason = reason; }
        internal void Leave(PlayerActionLease lease)
        {
            if (!ReferenceEquals(_current, lease)) return;
            _context.MotionControl.Release(lease);
            _current = null;
        }
        internal void Apply()
        {
            var lease = _current;
            if (lease == null || _disposed) return;
            if ((lease.Channels & PlayerActionChannels.Horizontal) != 0 && lease.Horizontal.HasValue)
                _context.Motor.SetHorizontalVelocity(lease.Horizontal.Value);
            if ((lease.Channels & PlayerActionChannels.Vertical) != 0)
            {
                if (lease.NewVertical && lease.Vertical.HasValue)
                {
                    if (lease.PendingFlight.Enabled && (lease.FlightOwner == null || !lease.FlightOwner.isActiveAndEnabled))
                    { lease.Vertical = null; lease.NewVertical = false; return; }
                    _context.Wall.Cancel();
                    lease.Flight = _context.VerticalMotion.BeginFlight(lease.FlightOwner, _context.Input,
                        lease.PendingSource, lease.PendingFlight, PlayerFlightOrigin.Action);
                    lease.NewVertical = false;
                }
                if (lease.Vertical.HasValue)
                {
                    _context.Motor.SetVerticalVelocity(lease.Vertical.Value);
                    if (lease.Vertical.Value > 0) _context.JumpWindow.Consume();
                }
                if (!lease.HoldVertical) lease.Vertical = null;
                if (lease.Gravity.HasValue) _context.Motor.SetGravityScale(lease.Gravity.Value);
            }
            if ((lease.Channels & PlayerActionChannels.Facing) != 0 && lease.Facing.HasValue)
                _context.Facing.SetFacing(lease.Facing.Value);
        }
        internal void Close(PlayerActionEndReason reason)
        {
            if (_current?.EndReason == PlayerActionEndReason.Transition) MarkEnd(reason);
            _disposed = true;
        }
        #endregion
    }

    /// <summary>独立消费程序集的互斥动作基类。配置必须冻结；退出、故障及候选释放均归还本代凭证。</summary>
    public abstract class PlayerActionState : EmberFsmState<PlayerContext>
    {
        #region 内部参数
        private readonly EmberFsmTransition _exit;
        private readonly PlayerActionChannels _channels;
        protected PlayerActionLease Action { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        protected PlayerActionState(EmberFsmTransition exit, PlayerActionChannels channels = PlayerActionChannels.All)
        { _exit = exit ?? throw new ArgumentNullException(nameof(exit)); _channels = channels; }
        public sealed override EmberFsmEntryDecision CanEnter() => Context.Actions.CanEnter
            ? CanEnterAction() : EmberFsmEntryDecision.Reject;
        protected virtual EmberFsmEntryDecision CanEnterAction() => EmberFsmEntryDecision.Allow;
        public sealed override void Enter(EmberFsmActivation activation)
        {
            base.Enter(activation);
            Action = Context.Actions.Begin(activation, _exit, _channels);
            try { EnterAction(Action); }
            catch { Action.EndReason = PlayerActionEndReason.PreparationFailed; Exit(); throw; }
        }
        protected abstract void EnterAction(PlayerActionLease action);
        protected virtual void ExitAction(PlayerActionEndReason reason) { }
        public sealed override void Exit()
        {
            var action = Action;
            if (action == null) return;
            Action = null;
            Context.Actions.Leave(action);
            ExitAction(action.EndReason);
        }
        public sealed override void Dispose() => Exit();
        #endregion
    }
}
