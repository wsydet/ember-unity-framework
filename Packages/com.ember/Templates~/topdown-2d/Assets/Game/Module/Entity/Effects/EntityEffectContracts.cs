using System;

namespace Game.Entity
{
    /// <summary>一次施加的只读普通数据。SourceKey 表示来源，不作为撤销身份。</summary>
    public readonly struct EntityEffectView
    {
        #region 内部参数
        public Guid ApplicationId { get; }
        public string EffectId { get; }
        public string SourceKey { get; }
        public EntityEffectKind Kind { get; }
        public EntityEffectLifetime Lifetime { get; }
        public bool IsTiming { get; }
        public float RemainingSeconds { get; }
        public double ElapsedSeconds { get; }
        public EntityEffectRules Rules { get; }
        public EntityEffectAppearance Appearance { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal EntityEffectView(Guid applicationId, string effectId, string sourceKey, EntityEffectKind kind,
            EntityEffectLifetime lifetime, bool isTiming, float remaining, double elapsed,
            EntityEffectRules rules, EntityEffectAppearance appearance)
        {
            ApplicationId=applicationId; EffectId=effectId; SourceKey=sourceKey; Kind=kind;
            Lifetime=lifetime; IsTiming=isTiming; RemainingSeconds=remaining;
            ElapsedSeconds=elapsed; Rules=rules; Appearance=appearance;
        }
        #endregion
    }

    /// <summary>独立计时效果的冻结记录；不包含组件、句柄或时钟。不是项目存档格式。</summary>
    public readonly struct EntityTimedEffectSnapshot
    {
        #region 内部参数
        public string EffectId { get; }
        public string SourceKey { get; }
        public float RemainingSeconds { get; }
        public EntityEffectRules Rules { get; }
        public EntityEffectAppearance Appearance { get; }
        public EntityEffectKind Kind { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EntityTimedEffectSnapshot(string effectId, string sourceKey, float remaining,
            EntityEffectRules rules, EntityEffectAppearance appearance, EntityEffectKind kind = EntityEffectKind.Buff)
        {
            EffectId=effectId; SourceKey=sourceKey; RemainingSeconds=remaining; Rules=rules; Appearance=appearance; Kind=kind;
            Validate();
        }
        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(EffectId) || EffectId != EffectId.Trim() ||
                string.IsNullOrWhiteSpace(SourceKey) || SourceKey != SourceKey.Trim() ||
                !float.IsFinite(RemainingSeconds) || RemainingSeconds <= 0 || Rules == null ||
                !Enum.IsDefined(typeof(EntityEffectKind), Kind)) throw new ArgumentException("独立计时效果快照无效。");
        }
        #endregion
    }

    /// <summary>只拥有本次施加；Dispose 立即撤销，EndAction 显式请求按配置结束/开始尾随。</summary>
    public sealed class EntityEffectHandle : IDisposable
    {
        #region 内部参数
        private readonly EntityEffectContainer _owner;
        private bool _owns = true;
        public Guid ApplicationId { get; }
        public bool IsActive => _owner != null && _owner.TryGet(ApplicationId, out _);
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal EntityEffectHandle(EntityEffectContainer owner, Guid applicationId)
        { _owner=owner; ApplicationId=applicationId; }
        public bool EndAction()
        {
            if (!_owns) return false;
            if (!_owner.EndAction(ApplicationId)) return false;
            // 只切换计时阶段；句柄仍可显式撤销，具体动作决定何时释放或交接拥有权。
            return true;
        }
        public void Dispose()
        {
            if (!_owns) return;
            _owner.Remove(ApplicationId);
            _owns=false;
        }
        #endregion
    }
}
