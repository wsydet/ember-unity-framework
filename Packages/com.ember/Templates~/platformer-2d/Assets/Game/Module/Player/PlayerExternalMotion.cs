using System;
using System.Collections.Generic;
using System.Text;
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

        public PlayerExternalMotionRequest(string sourceId, PlayerExternalMotionMode mode, Vector2 velocity,
            float duration = 0, int priority = 0, bool lockInput = false, bool stopVerticalOnEnd = false)
        {
            if (string.IsNullOrWhiteSpace(sourceId) || !Enum.IsDefined(typeof(PlayerExternalMotionMode), mode) ||
                !float.IsFinite(velocity.x) || !float.IsFinite(velocity.y) || !float.IsFinite(duration) || duration < 0 ||
                mode == PlayerExternalMotionMode.ForcedRise && (duration <= 0 || velocity.y <= 0))
                throw new ArgumentException("外部运动需要来源ID、有限速度和合法时长；强制上升必须有正速度和有限正时长。");
            SourceId = sourceId; Mode = mode; Velocity = velocity; Duration = duration;
            Priority = priority; LockInput = lockInput || mode == PlayerExternalMotionMode.InteractionLock;
            StopVerticalOnEnd = stopVerticalOnEnd;
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
        public bool HasTransientMotion => _sources.Count != 0 || _lastContribution != Vector2.zero || _context.Support.HasTransientMotion;
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
            bool locked = false;
            IsInteractionLocked = false;
            foreach (var source in _sources)
            {
                locked |= source.Request.LockInput;
                IsInteractionLocked |= source.Request.Mode == PlayerExternalMotionMode.InteractionLock;
            }
            _context.Input.SetExternalSuppression(locked);
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
            if (request.Mode == PlayerExternalMotionMode.InteractionLock && !IsInteractionLocked)
            {
                // 不把任意动作冻结后再复活；编辑只能从合法地面姿态进入。
                if (!_context.JumpWindow.HasGroundSupport || _context.Support.HasTransientMotion || _context.MotionControl.Owns((PlayerMotionChannels)127))
                    throw new InvalidOperationException("交互锁需要稳定地面姿态且没有内部动作接管。");
                PlatformerSnapshot.CaptureForInteraction(_context.Entity.gameObject.scene.name, _context);
            }
            var lease = new PlayerExternalMotionLease(this, owner, request);
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
            if (_lastRise?.IsActive == true && _context.Motor.Velocity.y <= 0) _lastRise.Dispose();
            _lastRise = null;
        }

        internal void Compose()
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
            bool internalAction = _context.MotionControl.Owns((PlayerMotionChannels)127) ||
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
                if (stopVertical) velocity.y = 0;
                OverrideReason = replacement?.Request.SourceId ?? "自身速度 + 附加来源";
                if (replacement != null) velocity = replacement.Request.Velocity;
                if (rise != null)
                {
                    _lastRise = rise;
                    velocity = rise.Request.Velocity;
                    _context.Motor.SetGravityScale(0);
                    OverrideReason = rise.Request.SourceId;
                }
                // 替换速度成为新的基础；只有环境项在下步扣除，不反复累积。
                _lastContribution = EnvironmentVelocity + _context.Support.Velocity;
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

        internal void Clear()
        {
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
