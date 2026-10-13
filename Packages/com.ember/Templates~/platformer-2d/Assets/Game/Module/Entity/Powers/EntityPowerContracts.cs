using System;
using System.Collections.Generic;

namespace Game.Entity
{
    [Flags]
    public enum EntityUseStatus
    {
        Available = 0, NotInstalled = 1, ProjectDisabled = 2, Closed = 4, NoUses = 8,
        CoolingDown = 16, EnvironmentDenied = 32, Prohibited = 64, Disposed = 128,
        InvalidAttempt = 256, AlreadyCommitted = 512
    }

    /// <summary>一次动作尝试的身份；查询不创建它，实际动作选择自己的确定提交点。</summary>
    public sealed class EntityUseAttempt
    {
        #region 内部参数
        internal readonly EntityPowerRuntime Owner;
        public bool IsCommitted { get; internal set; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal EntityUseAttempt(EntityPowerRuntime owner) => Owner = owner;
        #endregion
    }

    /// <summary>宿主持有的独立开放数据；资源释放不清除数据，寿命和持久化由宿主决定。</summary>
    public sealed class EntityPowerAvailability
    {
        #region 内部参数
        private readonly Dictionary<string, bool> _abilities = new(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> _skills = new(StringComparer.Ordinal);
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public bool IsAbilityOpen(string id) => id != null && _abilities.TryGetValue(id, out var value) && value;
        public bool IsSkillOpen(string id) => id != null && _skills.TryGetValue(id, out var value) && value;
        internal void Set(bool skill, string id, bool open) => (skill ? _skills : _abilities)[id] = open;
        #endregion
    }

    /// <summary>准备时复制并校验，运行对象不保留可变 SO 引用。</summary>
    internal readonly struct EntityPowerSpec
    {
        #region 内部参数
        internal readonly string Id;
        internal readonly bool Enabled, DefaultOpen, LimitedUses;
        internal readonly int MaximumUses;
        internal readonly float CooldownSeconds;
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal EntityPowerSpec(string id, bool enabled, bool open, bool limited, int maximum, float cooldown)
        {
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim()) throw new ArgumentException("能力/技能需要稳定的非空 ID，不能带首尾空白。");
            if (maximum < 1) throw new ArgumentOutOfRangeException(nameof(maximum));
            if (!float.IsFinite(cooldown) || cooldown < 0) throw new ArgumentOutOfRangeException(nameof(cooldown));
            Id = id; Enabled = enabled; DefaultOpen = open; LimitedUses = limited;
            MaximumUses = maximum; CooldownSeconds = cooldown;
        }
        #endregion
    }
}


