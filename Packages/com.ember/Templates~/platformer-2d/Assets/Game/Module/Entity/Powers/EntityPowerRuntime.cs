using System;
using UnityEngine;

namespace Game.Entity
{
    /// <summary>两个业务入口共用的少量资源规则，不执行动作、不查询输入、环境或全局玩家。</summary>
    public sealed class EntityPowerRuntime : IDisposable
    {
        #region 内部参数
        private readonly EntityPowerSpec _spec;
        private readonly EntityPowerAvailability _availability;
        private readonly bool _skill;
        private int _remainingUses;
        private bool _disposed;
        public string Id => _spec.Id;
        public bool IsEnabled => _spec.Enabled;
        public bool IsOpen => _skill ? _availability.IsSkillOpen(Id) : _availability.IsAbilityOpen(Id);
        public int? RemainingUses => _spec.LimitedUses ? _remainingUses : null;
        public float RemainingCooldown { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        internal EntityPowerRuntime(EntityPowerSpec spec, EntityPowerAvailability availability, bool skill)
        {
            _spec = spec; _availability = availability; _skill = skill; _remainingUses = spec.MaximumUses;
            availability.Set(skill, spec.Id, spec.DefaultOpen);
        }

        internal void Tick(float gameSeconds)
        {
            if (_disposed) return;
            RemainingCooldown = Mathf.Max(0, RemainingCooldown - gameSeconds);
            // 与玩家机会计时相同的微秒边界容差，避免 float 扣减残差多阻塞一帧。
            if (gameSeconds > 0 && RemainingCooldown <= .000001f) RemainingCooldown = 0;
        }

        internal EntityPowerRecord Capture()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(EntityPowerRuntime));
            return new EntityPowerRecord(Id, IsOpen, RemainingUses, RemainingCooldown);
        }
        internal void ValidateRestore(EntityPowerRecord record)
        {
            record.Validate();
            if (record.Id != Id || record.RemainingUses.HasValue != _spec.LimitedUses ||
                record.RemainingUses > _spec.MaximumUses || record.RemainingCooldown > _spec.CooldownSeconds)
                throw new ArgumentException("能力快照与当前装配不兼容：" + record.Id);
        }
        internal void ApplyRestore(EntityPowerRecord record)
        {
            _availability.Set(_skill, Id, record.IsOpen);
            _remainingUses = record.RemainingUses ?? 0;
            RemainingCooldown = record.RemainingCooldown;
        }

        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>只读查询，返回全部禁止原因。环境/临时限制由角色本次事实提供，不保存成可用性总开关。</summary>
        public EntityUseStatus Query(bool environmentAllowed = true, bool prohibited = false)
        {
            var status = EntityUseStatus.Available;
            if (_disposed) status |= EntityUseStatus.Disposed;
            if (!IsEnabled) status |= EntityUseStatus.ProjectDisabled;
            if (!IsOpen) status |= EntityUseStatus.Closed;
            if (_spec.LimitedUses && _remainingUses == 0) status |= EntityUseStatus.NoUses;
            if (RemainingCooldown > 0) status |= EntityUseStatus.CoolingDown;
            if (!environmentAllowed) status |= EntityUseStatus.EnvironmentDenied;
            if (prohibited) status |= EntityUseStatus.Prohibited;
            return status;
        }

        public void SetOpen(bool open)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(EntityPowerRuntime));
            _availability.Set(_skill, Id, open);
        }

        public EntityUseAttempt CreateAttempt()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(EntityPowerRuntime));
            return new EntityUseAttempt(this);
        }

        /// <summary>调用者在动作确定提交点显式调用。同一尝试最多成功一次；不定义冲刺起手/结束、返还或回滚策略。</summary>
        public EntityUseStatus TryCommit(EntityUseAttempt attempt, bool environmentAllowed = true, bool prohibited = false)
        {
            if (attempt == null || !ReferenceEquals(attempt.Owner, this)) return EntityUseStatus.InvalidAttempt;
            if (attempt.IsCommitted) return EntityUseStatus.AlreadyCommitted;
            var status = Query(environmentAllowed, prohibited);
            if (status != EntityUseStatus.Available) return status;
            if (_spec.LimitedUses) _remainingUses--;
            RemainingCooldown = _spec.CooldownSeconds;
            attempt.IsCommitted = true;
            return EntityUseStatus.Available;
        }

        public void RefillUses()
        {
            if (!_disposed && _spec.LimitedUses)
                _remainingUses = _spec.MaximumUses;
        }
        public void Dispose() => _disposed = true;
        #endregion
    }
}


