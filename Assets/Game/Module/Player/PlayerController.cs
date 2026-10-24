using System;
using Ember.Core.Fsm;
using System.Linq;
using Game.Entity;
using Game.Input;
using UnityEngine;

namespace Game.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(Entity2D), typeof(EntityMotor2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        #region 内部参数
        private Entity2D _entity;
        private EntityMotor2D _motor;
        private EntitySensors2D _sensors;
        private EntityFacing2D _facing;
        private PlayerRuntimeCandidate _active, _candidate, _candidatePrevious;
        private PlayerRuntimeCandidate _committingPrevious;
        private EmberFsm<PlayerContext> _fsm => _active?.Fsm;
        private PlayerPowerSession _powers => Context?.Powers;
        private EntityEffectContainer _effects => Context?.Effects;
        private EntityEffectBinding _effectBinding;
        private bool _interactionHeld;
        public PlayerContext Context => _active?.Context;
        internal PlatformerSnapshot EntrySnapshot => _active?.Snapshot;
        public bool IsRunning => _fsm?.Status == EmberFsmStatus.Running;
        public bool IsPrepared => _fsm != null;
        public EmberFsmStatus FsmStatus => _fsm?.Status ?? EmberFsmStatus.Stopped;
        public Guid? FsmInstanceId => _fsm?.InstanceId;
        public string CurrentStateId => _fsm?.CurrentLeafId;
        public System.Collections.Generic.IReadOnlyList<string> ActiveStatePath => _fsm?.GetActivePath() ?? Array.Empty<string>();
        public EmberFsmGraph ObservationGraph => _active?.Graph;
#if UNITY_EDITOR
        internal PlayerSchemeObservation SchemeObservation => _active?.SchemeObservation;
#endif
        public EmberFsmObservation CaptureFsmObservation() => EmberFsmObservation.Capture(_fsm, _active?.ObservationOrigin);
        public event Action<PlayerController> Invalidated;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void Awake()
        {
            _motor = GetComponent<EntityMotor2D>();
            _motor.Freeze(); // 只冻结候选，绝不从 Awake 启动会话。
        }
        private void OnDisable() { Context?.Actions.MarkEnd(PlayerActionEndReason.Disabled); Stop(); Invalidated?.Invoke(this); }
        private void OnDestroy() { Context?.Actions.MarkEnd(PlayerActionEndReason.Destroyed); Stop(); Invalidated = null; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void Check(EmberFsmResult result)
        {
            if (result != EmberFsmResult.Changed && result != EmberFsmResult.NoChange)
            {
                var fault = _fsm?.LastFault;
                throw new InvalidOperationException($"玩家 FSM：{result}; 节点={fault?.FaultNodeId}; 阶段={fault?.Phase}", fault?.Exception);
            }
        }

        private void ValidateConfiguration(PlayerConfig config, string scene, string trialScene = null)
        {
            if (!isActiveAndEnabled) throw new InvalidOperationException("角色未激活。");
            _entity = GetComponent<Entity2D>(); _motor = GetComponent<EntityMotor2D>();
            _sensors = GetComponent<EntitySensors2D>(); _facing = GetComponent<EntityFacing2D>();
            if (_entity == null || _motor == null || _sensors == null || _facing == null || config == null || config.Graph == null)
                throw new InvalidOperationException("角色能力或 PlayerConfig/图缺失。");
            if (_motor.Body.bodyType != RigidbodyType2D.Dynamic || !_motor.Body.simulated && !_motor.IsFrozen)
                throw new InvalidOperationException("玩家要求可模拟的 Dynamic Rigidbody2D。");
            if (config.Feel == null)
                throw new InvalidOperationException("玩家手感预设缺失。");
            if (_sensors.Shape == null || !_entity.Owns(_sensors.Shape))
                throw new InvalidOperationException("Sensors 必须配置属于本实体的碰撞体。");
            string target = trialScene ?? config.Scene;
            if (scene != target || gameObject.scene.name != target)
                throw new InvalidOperationException("进度、配置与角色场景不一致。");
        }

        private void BindEffects()
        {
            if (_effects == null) return;
            var explicitTargets = GetComponent<EntityEffectTargets>();
            if (explicitTargets != null) { _effectBinding = explicitTargets.Bind(_effects); return; }
            var view = GetComponentInChildren<PlayerAnimationView>(true);
            _effectBinding = new EntityEffectBinding(_effects,
                GetComponentsInChildren<Collider2D>(true).Where(item => _entity.Owns(item) && !item.isTrigger).ToArray(),
                view != null ? new[] { view.Sprite } : GetComponents<SpriteRenderer>());
        }

        private void ApplyCandidate(PlayerRuntimeCandidate candidate)
        {
            _interactionHeld = false;
            _active = candidate;
            candidate.Animation?.Bind();
            _sensors.Bind(_entity);
            _motor.Freeze();
            _motor.Apply(candidate.Snapshot.Position, candidate.Snapshot.Velocity, candidate.GravityScale);
            _facing.SetFacing(candidate.Snapshot.FacesRight);
            BindEffects();
            if (candidate.Restoring)
            {
                // 同步提交区内没有物理步。让查询使用新位置，不能让进入 Guard 看到旧会话的地面缓存。
                _motor.Resume();
                try { _sensors.Sample(); }
                finally { _motor.Freeze(); }
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal void PrepareNewGame(object owner, PlayerConfig config, GameInputService input,
            Vector2? initialPosition = null, Vector2? initialVelocity = null, PlayerTrialStart trial = null)
        {
            if (_fsm != null || _candidate != null && !_candidate.IsDisposed || !isActiveAndEnabled)
                throw new InvalidOperationException("角色已占用或未激活。");
            ValidateConfiguration(config, trial?.Scene ?? config.Scene, trial?.Scene);
            var candidate = new PlayerRuntimeCandidate(this, owner, config, null, input,
                _entity, _motor, _sensors, _facing, initialPosition, initialVelocity, trial);
            try
            {
                ApplyCandidate(candidate);
                candidate.Transferred = true;
            }
            catch { Stop(); throw; }
        }

        /// <summary>完整快照候选；只装配独立资源和声明，不运行状态、不改变当前角色。</summary>
        internal PlayerRuntimeCandidate PrepareSnapshot(object owner, PlayerConfig config, PlatformerSnapshot snapshot,
            GameInputService input)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (_candidate != null && !_candidate.IsDisposed)
                throw new InvalidOperationException("已有未交接的玩家候选。");
            if (_active != null && ReferenceEquals(owner, _active.Owner))
                throw new InvalidOperationException("新旧会话必须使用独立所有者。");
            ValidateConfiguration(config, snapshot.Scene);
            _candidate = new PlayerRuntimeCandidate(this, owner, config, snapshot, input,
                _entity, _motor, _sensors, _facing);
            _candidatePrevious = _active;
            return _candidate;
        }

        /// <summary>同一主线程提交；替换须由模块整体暂停。失败候选释放，旧资源和效果绑定恢复。</summary>
        internal void CommitSnapshot(PlayerRuntimeCandidate candidate, object previousOwner = null)
        {
            if (candidate == null || candidate != _candidate || candidate.Controller != this || candidate.IsDisposed ||
                candidate.Transferred || !isActiveAndEnabled || !ReferenceEquals(_active, _candidatePrevious))
                throw new InvalidOperationException("完整快照候选已失效或不属于此角色。");
            var previous = _active;
            if (previous != null && (!ReferenceEquals(previous.Owner, previousOwner) ||
                previous.Fsm.Status != EmberFsmStatus.Paused || Time.timeScale != 0))
                throw new InvalidOperationException("替换玩家前必须由原会话整体暂停。");
            if (previous == null && previousOwner != null)
                throw new InvalidOperationException("原玩家会话已经结束。");
            var position = _motor.Position; var velocity = _motor.Velocity;
            float gravity = _motor.Body.gravityScale;
            bool facing = _facing.FacesRight;
            _effectBinding?.Dispose(); _effectBinding = null;
            _committingPrevious = previous;
            try
            {
                ApplyCandidate(candidate);
                StartPrepared(candidate.Owner);
                if (previous != null) Pause();
                if (!isActiveAndEnabled || _active != candidate || candidate.IsDisposed)
                    throw new InvalidOperationException("玩家在交接回调中失效。");
            }
            catch
            {
                // 旧 FSM 未退出；候选状态回调的实体运动/表现写入在本同步提交区还原。
                if (_motor != null) _motor.Freeze();
                try { candidate.Release(); }
                finally
                {
                    _effectBinding?.Dispose(); _effectBinding = null;
                    bool restore = this != null && isActiveAndEnabled && (previous == null || !previous.IsDisposed);
                    _active = restore ? previous : null; _candidate = null; _candidatePrevious = null;
                    if (restore)
                    {
                        _motor.Apply(position, velocity, gravity); _facing.SetFacing(facing);
                        if (previous != null)
                        {
                            previous.Animation?.Bind();
                            previous.Animation?.Sync(previous.Fsm);
                            previous.Animation?.Pause(true);
                            BindEffects();
                            _motor.Resume(); Context.DropThrough.Reapply(); _sensors.Sample(); _motor.Freeze();
                        }
                    }
                    else previous?.Release();
                }
                throw;
            }
            finally { _committingPrevious = null; }
            candidate.Transferred = true; _candidate = null; _candidatePrevious = null;
            // 从此候选归 Controller。即使旧清理失败，也不能用旧票据停止新会话。
            previous?.Release();
        }

        public void StartPrepared(object owner)
        {
            if (!ReferenceEquals(owner, _active?.Owner) || _fsm == null || !isActiveAndEnabled)
                throw new InvalidOperationException("准备结果已失效或不属于此角色。");
            bool starting = _fsm.Status == EmberFsmStatus.Stopped;
            if (starting && _active.Restoring) Context.Input.Clear();
            var startingRuntime = _active;
            try { Check(startingRuntime.Fsm.Start()); }
            finally { if (startingRuntime.IsDisposed) startingRuntime.Release(); }
            if (_active == null || _active.IsDisposed || !isActiveAndEnabled)
                throw new InvalidOperationException("玩家在状态启动回调中失效。");
            _motor.Resume();
            _active.Animation?.Sync(_fsm);
            if (starting && _active.Restoring)
            {
                // 建立恢复位置的支撑基线，不把加载在地面当作一次新落地补满资源。
                _sensors.Sample();
                if (_sensors.IsGrounded && _motor.Velocity.y <= 0)
                    Context.JumpWindow.PreserveRestoredGroundBaseline();
                Context.JumpWindow.Observe(Context.Support.ResolveContact(_sensors.GroundContact), 0, _sensors.IsGrounded);
                Context.VerticalMotion.Observe(Context.JumpWindow.HasGroundSupport, _motor.Velocity.y, 0);
            }
        }

        public void Tick(float dt)
        {
            if (!IsRunning) return;
            if (!Context.Actions.Prune()) { Stop(); return; }
            if (!IsRunning) return;
            MarkLifeActionEnd();
            _powers.Tick(dt);
            _effects?.Tick(dt);
            Context.ExternalMotion.Prune();
            Context.VerticalMotion.PruneFlight();
            if (Context.Life?.IsDead == true || Context.Life?.Reaction?.IsActive == true) Context.ExternalMotion.Clear();
            if (UpdateInteractionHold()) return;
            Context.Input.Sample();
            var active = _active;
            try { Check(active.Fsm.Update(dt)); }
            finally { if (active.IsDisposed) active.Release(); }
            if (ReferenceEquals(active, _active) && !active.IsDisposed) active.Animation?.Sync(active.Fsm);
        }

        public void FixedTick(float dt)
        {
            if (!IsRunning) return;
            if (!Context.Actions.Prune()) { Stop(); return; }
            if (!IsRunning) return;
            MarkLifeActionEnd();
            Context.ExternalMotion.BeginStep();
            if (Context.Life?.IsDead == true || Context.Life?.Reaction?.IsActive == true) Context.ExternalMotion.Clear();
            if (UpdateInteractionHold()) { Context.ExternalMotion.EndStep(dt); return; }
#if UNITY_EDITOR
            var observedPosition = _motor.Position;
#endif
            _sensors.Sample();
            if (Context.JumpWindow.Observe(Context.Support.ResolveContact(_sensors.GroundContact), dt,
                _sensors.IsGrounded, Context.Ledge.IsActive || Context.DropThrough.IsActive))
                _powers.RefillGroundUses(); // 统一支撑边沿，仅真实接触取得时补充一次。
            if (Context.Life?.IsDead != true && Context.Life?.Reaction?.IsActive != true) Context.Support.Observe(dt);
            Context.Landing.Observe();
            Context.HeadJump.Observe();
            Context.Wall.Observe(dt);
            Context.Ledge.ObserveInput();
            _motor.BeginStep();
            _motor.SetHorizontalVelocity(Context.ExternalMotion.SelfVelocity.x);
            _motor.SetVerticalVelocity(Context.ExternalMotion.SelfVelocity.y);
            Context.Correction.BeginStep();
            Context.VerticalMotion.Observe(Context.JumpWindow.HasGroundSupport, _motor.Velocity.y, dt);
            Context.Combat?.Dash?.ObserveEnvironment();
            var active = _active;
            try
            {
                Check(_fsm.FixedUpdate(dt));
                if (ReferenceEquals(active, _active) && !active.IsDisposed)
                {
                    Context.Actions.Apply();
                    if (Context.ExternalMotion.HasVerticalOverride)
                        Context.VerticalMotion.CancelJumpHold();
                    if (!Context.MotionControl.Owns(PlayerMotionChannels.Wall)) Context.Wall.Apply();
                    if (!Context.MotionControl.Owns(PlayerMotionChannels.Ledge)) Context.Ledge.Apply(dt);
                    if (!Context.MotionControl.Owns(PlayerMotionChannels.Vertical)) Context.VerticalMotion.Apply(_motor, UnityEngine.Physics2D.gravity.y, dt);
                    else Context.VerticalMotion.ApplyOwnedFlight(_motor, UnityEngine.Physics2D.gravity.y, dt);
                    if (!Context.MotionControl.Owns(PlayerMotionChannels.Correction)) Context.Correction.Apply(dt, CurrentStateId);
                }
                if (ReferenceEquals(active, _active) && !active.IsDisposed) Context.ExternalMotion.Compose(dt);
                _motor.ExecuteStep();
                if (ReferenceEquals(active, _active) && !active.IsDisposed) active.Animation?.Sync(active.Fsm);
                if (ReferenceEquals(active, _active) && !active.IsDisposed) Context.Landing.RecordStep();
#if UNITY_EDITOR
                if (ReferenceEquals(active, _active) && !active.IsDisposed)
                    Context.MotionTrace.Record(dt, observedPosition, _motor.Velocity, _motor.Body.gravityScale,
                        Context.Input.BufferRemaining, Context.JumpWindow.CoyoteRemaining, CurrentStateId);
#endif
            }
            finally
            {
                Context?.Input.EndPhysicsStep(dt);
                Context?.ExternalMotion.EndStep(dt);
                if (active.IsDisposed) active.Release();
            }
        }

        // 仅由 PlayerModule 的整体暂停协调调用，不向业务暴露角色单独暂停入口。
        internal void Pause()
        {
            if (!IsRunning) return;
            Context.VerticalMotion.PauseOrdinary();
            _motor.Freeze(); Context.Input.Clear(); Check(_fsm.Pause());
            _active.Animation?.Pause(true);
        }
        internal void Resume()
        {
            if (_fsm?.Status != EmberFsmStatus.Paused) return;
            Context.Input.Clear(); Check(_fsm.Resume());
            if (!_interactionHeld) _motor.Resume();
            Context.DropThrough.Reapply();
            _active.Animation?.Pause(false);
        }

        private bool UpdateInteractionHold()
        {
            if (Context.ExternalMotion.IsInteractionLocked)
            {
                if (!_interactionHeld)
                {
                    _motor.Freeze();
                    _motor.Apply(_motor.Position, Vector2.zero, Context.Feel.GravityScale);
                    _interactionHeld = true;
                }
                Context.Input.Clear();
                return true;
            }
            if (_interactionHeld)
            {
                _interactionHeld = false;
                _motor.Resume();
                Context.Input.Clear();
            }
            return false;
        }

        private void MarkLifeActionEnd()
        {
            if (Context.Life?.IsDead == true || Context.Life?.Reaction?.IsActive == true || Context.Life?.Reaction?.HasPending == true)
                Context.VerticalMotion.CancelOrdinary();
            if (Context.Life?.IsDead == true) Context.Actions.MarkEnd(PlayerActionEndReason.Death);
            else if (Context.Life?.Reaction?.IsActive == true || Context.Life?.Reaction?.HasPending == true)
                Context.Actions.MarkEnd(PlayerActionEndReason.Reaction);
        }

        public PlatformerSnapshot Capture(string scene)
        {
            if (_fsm == null) throw new InvalidOperationException("角色没有准备好的会话。");
            return PlatformerSnapshot.Capture(scene, Context);
        }

        public void StopPrepared(object owner)
        {
            if (ReferenceEquals(owner, _active?.Owner)) Stop();
            else if (ReferenceEquals(owner, _candidate?.Owner))
            { _candidate.Dispose(); _candidate = null; _candidatePrevious = null; }
        }

        public void Stop()
        {
            _motor?.Freeze();
            var active = _active; var candidate = _candidate;
            var committingPrevious = _committingPrevious; _committingPrevious = null;
            _active = null; _candidate = null; _candidatePrevious = null;
            try { candidate?.Dispose(); }
            finally
            {
                try { active?.Release(); }
                finally
                {
                    try { committingPrevious?.Release(); }
                    finally { _effectBinding?.Dispose(); _effectBinding = null; }
                }
            }
        }
        #endregion
    }
}
