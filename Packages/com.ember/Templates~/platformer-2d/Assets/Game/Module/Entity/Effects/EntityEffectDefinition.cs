using System;
using UnityEngine;

namespace Game.Entity
{
    public enum EntityEffectKind { Buff, Debuff }
    public enum EntityEffectLifetime { Timed, Owned, OwnedThenTimed }

    /// <summary>效果身份与生命周期配置；规则/表现适配另行组合，不保存宿主运行状态。</summary>
    [CreateAssetMenu(menuName = "Game/Entity/Effect", fileName = "EntityEffect")]
    public class EntityEffectDefinition : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField] private string _id;
        [SerializeField] private EntityEffectKind _kind;
        [SerializeField] private EntityEffectLifetime _lifetime;
        [SerializeField, Min(0)] private float _duration = 1;
        [SerializeField] private string[] _damageCategories = Array.Empty<string>();
        [SerializeField] private bool _preventsStagger;
        [SerializeField] private LayerMask _excludedLayers;
        [SerializeField] private bool _hasAppearance;
        [SerializeField] private int _appearancePriority;
        [SerializeField, Range(0, 1)] private float _alpha = 1;
        [SerializeField, Min(0)] private float _blinkHz;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public string Id => _id;
        public EntityEffectKind Kind => _kind;
        public EntityEffectLifetime Lifetime => _lifetime;
        public float Duration => _duration;
        /// <summary>准备时冻结配置；不施加效果、不创建句柄或推进时间。</summary>
        public EntityEffectSpec Capture()
        { Validate(); return new EntityEffectSpec(Id, Kind, Lifetime, Duration, CaptureRules(), CaptureAppearance()); }
        internal EntityEffectRules CaptureRules() => new(_damageCategories, _preventsStagger, _excludedLayers.value);
        internal EntityEffectAppearance CaptureAppearance() => new(_hasAppearance, _appearancePriority, _alpha, _blinkHz);
        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(_id) || _id != _id.Trim()) throw new ArgumentException("效果需要稳定的非空 ID，不能带首尾空白。");
            if (!Enum.IsDefined(typeof(EntityEffectKind), _kind) || !Enum.IsDefined(typeof(EntityEffectLifetime), _lifetime))
                throw new ArgumentException("未知效果分类或生命周期。");
            if (!float.IsFinite(_duration) || _duration < 0) throw new ArgumentOutOfRangeException(nameof(Duration));
        }
        #endregion
    }

    /// <summary>可复用的不可变施加配置，不包含宿主或施加身份。</summary>
    public sealed class EntityEffectSpec
    {
        #region 内部参数
        public string Id { get; }
        public EntityEffectKind Kind { get; }
        public EntityEffectLifetime Lifetime { get; }
        public float Duration { get; }
        public EntityEffectRules Rules { get; }
        public EntityEffectAppearance Appearance { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EntityEffectSpec(string id, EntityEffectKind kind, EntityEffectLifetime lifetime,
            float duration, EntityEffectRules rules, EntityEffectAppearance appearance)
        {
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim() || rules == null || !float.IsFinite(duration) || duration < 0 ||
                !Enum.IsDefined(typeof(EntityEffectKind), kind) || !Enum.IsDefined(typeof(EntityEffectLifetime), lifetime))
                throw new ArgumentException("效果捕获值无效。");
            Id = id; Kind = kind; Lifetime = lifetime; Duration = duration; Rules = rules; Appearance = appearance;
        }
        #endregion
    }
}
