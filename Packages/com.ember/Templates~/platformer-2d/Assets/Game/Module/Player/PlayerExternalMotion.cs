using System;
using System.Collections.Generic;
using System.Text;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    public enum PlayerExternalMotionMode { AddVelocity, ReplaceVelocity, ForcedRise, InteractionLock, InitialVelocity }

    /// <summary>按秒执行的外部运动声明；持续时间0表示由所有者显式释放。</summary>
    public readonly struct PlayerExternalMotionRequest
    {
        public PlayerExternalMotionMode Mode { get; }
        public Vector2 Velocity { get; }
        public float Duration { get; }
        public int Priority { get; }
        public bool LockInput { get; }
        public string SourceId { get; }
        public bool StopVerticalOnEnd { get; }
        public PlayerFlightSpec Flight { get; }
        public bool PreserveHorizontalControl { get; }
        public LaunchTailSpec2D LaunchTail { get; }

        public PlayerExternalMotionRequest(string sourceId, PlayerExternalMotionMode mode, Vector2 velocity,
            float duration = 0, int priority = 0, bool lockInput = false, bool stopVerticalOnEnd = false, PlayerFlightSpec flight = default,
            bool preserveHorizontalControl = false, LaunchTailSpec2D launchTail = default)
        {
            if (string.IsNullOrWhiteSpace(sourceId) || !Enum.IsDefined(typeof(PlayerExternalMotionMode), mode) ||
                !float.IsFinite(velocity.x) || !float.IsFinite(velocity.y) || !float.IsFinite(duration) || duration < 0 ||
                preserveHorizontalControl && mode != PlayerExternalMotionMode.ForcedRise ||
                mode == PlayerExternalMotionMode.ForcedRise && (duration <= 0 || velocity.y <= 0))
                throw new ArgumentException("外部运动需要来源ID、有限速度和合法时长；强制上升必须有正速度和有限正时长。");
            SourceId = sourceId; Mode = mode; Velocity = velocity; Duration = duration;
            Priority = priority; LockInput = lockInput || mode == PlayerExternalMotionMode.InteractionLock;
            StopVerticalOnEnd = stopVerticalOnEnd; Flight = flight;
            PreserveHorizontalControl = preserveHorizontalControl;
            flight.ValidateWorld(Physics2D.gravity);
            if (flight.Enabled && mode != PlayerExternalMotionMode.InitialVelocity)
                throw new ArgumentException("单次飞行策略仅适用于 InitialVelocity 发射。");
            launchTail.ValidateWorld(Physics2D.gravity);
            if (launchTail.Enabled && (mode != PlayerExternalMotionMode.ForcedRise || stopVerticalOnEnd))
                throw new ArgumentException("弹射尾段仅适用于不在结束时停速的 ForcedRise。");
            LaunchTail = launchTail;
        }
    }

    /// <summary>单次登记凭证。释放旧凭证不会删除后来登记的来源。</summary>
    public sealed class PlayerExternalMotionLease : IDisposable
    {
        #region 内部参数
        internal PlayerExternalMotion Session;
        internal readonly Behaviour Owner;
        internal readonly PlayerExternalMotionRequest Request;
        internal float Remaining;
        public PlayerFlightLease Flight { get; internal set; }
        public PlayerLaunchTailLease LaunchTail { get; internal set; }
        public bool IsActive => Session != null && Owner != null && Owner.isActiveAndEnabled;
        public float RemainingSeconds => Remaining;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerExternalMotionLease(PlayerExternalMotion session, Behaviour owner, PlayerExternalMotionRequest request)
        { Session = session; Owner = owner; Request = request; Remaining = request.Duration; }
        public void Dispose()
        {
            var session = Session;
            Session = null;
            session?.Remove(this);
            LaunchTail?.Dispose();
        }
        #endregion
    }

    /// <summary>每个玩家会话独立持有。只由 Controller 驱动；不向刚体增加第二个更新入口。</summary>
    public sealed class PlayerExternalMotion : IDisposable
    {
        #region 内部参数
        private readonly PlayerContext _context;
        private readonly List<PlayerExternalMotionLease> _sources = new();
        private Vector2 _lastContribution;
        private readonly List<ContactPoint2D> _contacts = new(8);
        private PlayerExternalMotionLease _lastRise;
        private bool _disposed;
        private bool _stopVertical;
        public int SourceCount => _sources.Count;
        internal bool PreservesHorizontalDuringRise
        {
            get
            {
                foreach (var source in _sources)
                    if (source.IsActive && source.Request.PreserveHorizontalControl) return true;
                return false;
            }
        }
        internal bool HasVerticalOverride
        {
            get
            {
                foreach (var source in _sources)
                    if (source.IsActive && source.Request.Mode != PlayerExternalMotionMode.AddVelocity) return true;
                return false;
            }
        }
        public bool HasTransientMotion => _sources.Count != 0 || _stopVertical ||
            _lastContribution != Vector2.zero || _context.Support.HasTransientMotion;
        public bool IsInteractionLocked { get; private set; }
        public Vector2 EnvironmentVelocity { get; private set; }
        public Vector2 SelfVelocity => RemoveContribution();
        public Vector2 ComposedVelocity { get; private set; }
        public string OverrideReason { get; private set; } = "无外部来源";
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private Vector2 RemoveContribution()
        {
            var actual = _context.Motor.Velocity;
            var contribution = _lastContribution;
            if (contribution == Vector2.zero) return actual;
            // 只有真实接触阻挡的法向贡献被物理解算移除。零合速度也可能是反向来源抵消。
            _context.Motor.Body.GetContacts(_contacts);
            foreach (var contact in _contacts)
            {
                var normal = contact.normal;
                var obstacle = _context.Entity.Owns(contact.collider) ? contact.otherCollider : contact.collider;
                var surfaceVelocity = obstacle != null && obstacle.attachedRigidbody != null
                    ? obstacle.attachedRigidbody.linearVelocity : Vector2.zero;
                float inward = Vector2.Dot(contribution - surfaceVelocity, normal);
                if (inward < 0 && Vector2.Dot(actual - surfaceVelocity, normal) <= .00001f)
                    contribution -= inward * normal;
            }
            return actual - contribution;
        }
        private void RefreshInput()
        {
            bool locked = false, horizontalLocked = false;
            IsInteractionLocked = false;
            foreach (var source in _sources)
            {
                locked |= source.Request.LockInput;
                horizontalLocked |= source.Request.LockInput && !source.Request.PreserveHorizontalControl;
                IsInteractionLocked |= source.Request.Mode == PlayerExternalMotionMode.InteractionLock;
            }
            _context.Input.SetExternalSuppression(locked, locked && !horizontalLocked);
        }

        private void HandoffLaunchTail(PlayerExternalMotionLease source)
        {
            if (!source.Request.LaunchTail.Enabled || !ReferenceEquals(source, _lastRise) ||
                source.Owner == null || !source.Owner.isActiveAndEnabled ||
                _context.Life?.IsDead == true || _context.Life?.Reaction?.IsActive == true ||
                _context.Life?.Reaction?.HasPending == true) return;
            source.LaunchTail = _context.VerticalMotion.BeginLaunchTail(source.Owner, _context.Input,
                source.Request.SourceId, source.Request.LaunchTail);
        }

        private bool LaunchTailRiseBlocked()
        {
            if (SelfVelocity.y <= 0) return true;
            // 向下风在碰顶后扣除时，自身Y可能仍为正；真实顶面接触也必须结束保持。
            // 仅世界合速度下降不等于碰顶，不能用反向风提前取消保持。
            var actual = _context.Motor.Velocity;
            _context.Motor.Body.GetContacts(_contacts);
            foreach (var contact in _contacts)
            {
                if (contact.normal.y >= -.00001f) continue;
                var obstacle = _context.Entity.Owns(contact.collider) ? contact.otherCollider : contact.collider;
                var surface = obstacle != null && obstacle.attachedRigidbody != null
                    ? obstacle.attachedRigidbody.linearVelocity : Vector2.zero;
                if (Vector2.Dot(actual - surface, contact.normal) <= .00001f) return true;
            }
            return false;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerExternalMotion(PlayerContext context) => _context = context;

        /// <summary>主线程登记。所有者须为启用组件；禁用/销毁后在下一次玩家更新前剔除。</summary>
        public PlayerExternalMotionLease Register(Behaviour owner, PlayerExternalMotionRequest request)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PlayerExternalMotion));
            if (owner == null || !owner.isActiveAndEnabled) throw new ArgumentException("外部运动所有者必须启用。", nameof(owner));
            if (string.IsNullOrWhiteSpace(request.SourceId)) throw new ArgumentException("不能登记默认请求。", nameof(request));
            if (_context.Life?.IsDead == true) throw new InvalidOperationException("死亡玩家不接受外部运动。");
            request.LaunchTail.ValidateWorld(Physics2D.gravity);
            if (request.Mode == PlayerExternalMotionMode.InteractionLock && !IsInteractionLocked)
            {
                // 不把任意动作冻结后再复活；编辑只能从合法地面姿态进入。
                if (!_context.JumpWindow.HasGroundSupport || _context.Support.HasTransientMotion || _context.MotionControl.Owns((PlayerMotionChannels)127))
                    throw new InvalidOperationException("交互锁需要稳定地面姿态且没有内部动作接管。");
                PlatformerSnapshot.CaptureForInteraction(_context.Entity.gameObject.scene.name, _context);
            }
            var lease = new PlayerExternalMotionLease(this, owner, request);
            if (request.Mode != PlayerExternalMotionMode.AddVelocity)
            { _context.VerticalMotion.CancelOrdinary(PlayerFlightOrigin.External); _context.Wall.Cancel(); }
            _sources.Add(lease);
            RefreshInput();
            return lease;
        }

        internal void Remove(PlayerExternalMotionLease lease)
        {
            if (_sources.Remove(lease) && lease.Request.StopVerticalOnEnd) _stopVertical = true;
            RefreshInput();
        }

        internal void Prune()
        {
            for (int i = _sources.Count - 1; i >= 0; i--)
                if (!_sources[i].IsActive)
                {
                    _stopVertical |= _sources[i].Request.StopVerticalOnEnd;
                    _sources[i].Session = null; _sources.RemoveAt(i);
                }
            RefreshInput();
        }

        internal void BeginStep()
        {
            Prune();
            if (_lastRise?.IsActive == true &&
                (_lastRise.Request.LaunchTail.Enabled ? LaunchTailRiseBlocked() : _context.Motor.Velocity.y <= 0))
            {
                var source = _lastRise;
                HandoffLaunchTail(source);
                // 碰顶只结束保持，不重发请求速度；尾段在本步使用物理解算后的自身速度。
                source.Session = null;
                Remove(source);
            }
            _lastRise = null;
        }

        internal void Compose(float dt)
        {
            EnvironmentVelocity = Vector2.zero;
            var velocity = _context.Motor.CommandVelocity;
            bool stopVertical = _stopVertical; _stopVertical = false;
            PlayerExternalMotionLease replacement = null, rise = null;
            foreach (var source in _sources)
            {
                var request = source.Request;
                if (request.Mode == PlayerExternalMotionMode.AddVelocity) EnvironmentVelocity += request.Velocity;
                if ((request.Mode == PlayerExternalMotionMode.ReplaceVelocity || request.Mode == PlayerExternalMotionMode.InitialVelocity) &&
                    (replacement == null || request.Priority >= replacement.Request.Priority)) replacement = source;
                if (request.Mode == PlayerExternalMotionMode.ForcedRise &&
                    (rise == null || request.Priority >= rise.Request.Priority)) rise = source;
            }
            bool projectAction = _context.Actions.IsActive;
            bool ownsHorizontal = projectAction && _context.MotionControl.Owns(PlayerMotionChannels.Horizontal);
            bool ownsVertical = projectAction && _context.MotionControl.Owns(PlayerMotionChannels.Vertical);
            bool internalAction = !projectAction && _context.MotionControl.Owns((PlayerMotionChannels)127) ||
                _context.Dash.IsActive || _context.Ledge.IsActive || _context.Rolling.IsActive ||
                _context.Landing.IsActive || _context.Wall.Sliding;
            if (_context.Life?.IsDead == true || _context.Life?.Reaction?.IsActive == true || internalAction)
            {
                OverrideReason = "内部动作/生命流程优先，外部速度本步不参与";
                EnvironmentVelocity = Vector2.zero;
                _lastContribution = Vector2.zero;
            }
            else
            {
                if (stopVertical && !ownsVertical) velocity.y = 0;
                OverrideReason = replacement?.Request.SourceId ?? "自身速度 + 附加来源";
                if (replacement != null)
                {
                    if (!ownsHorizontal) velocity.x = replacement.Request.Velocity.x;
                    if (!ownsVertical)
                    {
                        velocity.y = replacement.Request.Velocity.y;
                        if (replacement.Request.Mode != PlayerExternalMotionMode.InitialVelocity)
                            _context.VerticalMotion.CancelOrdinary(PlayerFlightOrigin.External);
                        if (replacement.Request.Mode == PlayerExternalMotionMode.InitialVelocity)
                            replacement.Flight = _context.VerticalMotion.BeginFlight(replacement.Owner, _context.Input,
                                replacement.Request.SourceId, replacement.Request.Flight, PlayerFlightOrigin.External);
                    }
                }
                if (rise != null)
                {
                    if (!ownsHorizontal && !rise.Request.PreserveHorizontalControl) velocity.x = rise.Request.Velocity.x;
                    if (!ownsVertical)
                    {
                        _context.VerticalMotion.CancelOrdinary(PlayerFlightOrigin.External);
                        _lastRise = rise;
                        velocity.y = rise.Request.Velocity.y;
                        _context.Motor.SetGravityScale(0);
                    }
                    OverrideReason = rise.Request.SourceId;
                }
                if (!ownsVertical && replacement?.Request.Mode == PlayerExternalMotionMode.InitialVelocity && replacement.Flight != null)
                {
                    _context.Motor.SetVerticalVelocity(velocity.y);
                    _context.VerticalMotion.ApplyOwnedFlight(_context.Motor, Physics2D.gravity.y, dt);
                    velocity.y = _context.Motor.CommandVelocity.y;
                }
                // 替换速度成为新的基础；只有环境项在下步扣除，不反复累积。
                _lastContribution = EnvironmentVelocity + _context.Support.Velocity;
                if (ownsHorizontal) { EnvironmentVelocity = new Vector2(0, EnvironmentVelocity.y); _lastContribution.x = 0; }
                if (ownsVertical) { EnvironmentVelocity = new Vector2(EnvironmentVelocity.x, 0); _lastContribution.y = 0; }
                if (ownsHorizontal || ownsVertical) OverrideReason += " · 项目动作按轴优先";
                velocity += _lastContribution;
            }
            if (!float.IsFinite(velocity.x) || !float.IsFinite(velocity.y))
                throw new InvalidOperationException("外部来源合成速度溢出，拒绝提交物理指令。");
            ComposedVelocity = velocity;
            _context.Motor.SetHorizontalVelocity(velocity.x);
            _context.Motor.SetVerticalVelocity(velocity.y);
        }

        internal void EndStep(float dt)
        {
            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                var source = _sources[i];
                if (source.Request.Mode == PlayerExternalMotionMode.InitialVelocity)
                { source.Session = null; _sources.RemoveAt(i); continue; }
                if (source.Request.Duration == 0) continue;
                source.Remaining = Mathf.Max(0, source.Remaining - dt);
                if (source.Remaining > .000001f) continue;
                HandoffLaunchTail(source);
                _stopVertical |= source.Request.StopVerticalOnEnd;
                source.Session = null; _sources.RemoveAt(i);
            }
            RefreshInput();
        }

        public string Describe()
        {
            var result = new StringBuilder($"自身={SelfVelocity} 环境={EnvironmentVelocity} 支撑={_context.Support.Velocity} 合成={ComposedVelocity} 原因={OverrideReason}");
            foreach (var source in _sources)
                result.Append($"\n{source.Request.SourceId} · {source.Request.Mode} · priority={source.Request.Priority} · remaining={source.Remaining} · owner={source.Owner}");
            return result.ToString();
        }

        /// <summary>竖直弹射只替换运动接管，保留环境凭证、上一合成贡献与承载离台语义。</summary>
        internal void ClearOverridesForLaunch()
        {
            _context.VerticalMotion.ClearLaunchTail();
            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                if (_sources[i].Request.Mode == PlayerExternalMotionMode.AddVelocity) continue;
                _sources[i].Session = null;
                _sources.RemoveAt(i);
            }
            _lastRise = null; _stopVertical = false;
            RefreshInput();
        }

        internal void Clear()
        {
            _context.VerticalMotion.ClearLaunchTail();
            foreach (var source in _sources) source.Session = null;
            _sources.Clear(); _lastContribution = Vector2.zero; _lastRise = null;
            _stopVertical = false;
            _context.Support.Clear();
            EnvironmentVelocity = Vector2.zero;
            RefreshInput();
        }
        public void Dispose() { Clear(); _disposed = true; }
        #endregion
    }
}
