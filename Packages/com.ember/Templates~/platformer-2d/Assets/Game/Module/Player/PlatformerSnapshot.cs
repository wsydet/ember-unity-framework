using System;
using Ember.Basic;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    /// <summary>v2 完整不可变快照。R04.1 先独立验收，R04.2 替换正式入口的旧 PlatformerProgress。</summary>
    public sealed class PlatformerSnapshot
    {
        #region 内部参数
        public const int DATA_VERSION = 2;
        private readonly EntityTimedEffectSnapshot[] _effects;
        public string Scene { get; }
        public Vector2 Position { get; }
        public Vector2 Velocity { get; }
        public bool FacesRight { get; }
        public EntityPowerSnapshot Powers { get; }
        public bool EffectsInstalled { get; }
        public int EffectCount => _effects.Length;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void ValidatePowers()
        {
            if (Powers.AbilityCount > 256 || Powers.SkillCount > 256) throw new ArgumentException("能力快照条目过多。");
            for (int i = 0; i < Powers.AbilityCount; i++) ValidateId(Powers.GetAbility(i).Id);
            for (int i = 0; i < Powers.SkillCount; i++) ValidateId(Powers.GetSkill(i).Id);
        }
        private void ValidateId(string id)
        { if (id == null || id.Length > 256) throw new ArgumentException("快照 ID 长度无效。"); }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public PlatformerSnapshot(string scene, Vector2 position, Vector2 velocity, bool facesRight,
            EntityPowerSnapshot powers, bool effectsInstalled, EntityTimedEffectSnapshot[] effects)
        {
            if (string.IsNullOrWhiteSpace(scene) || scene.Length > 128 ||
                !float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(velocity.x) || !float.IsFinite(velocity.y))
                throw new ArgumentException("快照场景或运动数据无效。");
            if (powers == null || effects == null) throw new ArgumentNullException("完整快照不能缺少资源/效果数据。");
            if (effects.Length > 4096 || !effectsInstalled && effects.Length != 0)
                throw new ArgumentException("效果装配或条目数量无效。");
            Scene = scene; Position = position; Velocity = velocity; FacesRight = facesRight;
            Powers = powers; EffectsInstalled = effectsInstalled;
            _effects = (EntityTimedEffectSnapshot[])effects.Clone();
            ValidatePowers();
            foreach (var record in _effects)
            {
                // 同时拒绝默认 struct、缺失规则和非法计时；规则对象本身不可变。
                _ = new EntityTimedEffectSnapshot(record.EffectId, record.SourceKey, record.RemainingSeconds,
                    record.Rules, record.Appearance, record.Kind);
                ValidateId(record.EffectId); ValidateId(record.SourceKey);
                if (record.Rules.DamageCategoryCount > 64) throw new ArgumentException("伤害类别过多。");
                for (int i = 0; i < record.Rules.DamageCategoryCount; i++) ValidateId(record.Rules.GetDamageCategory(i));
            }
        }
        public EntityTimedEffectSnapshot GetEffect(int index) => _effects[index];
        public EntityEffectContainer RestoreEffects()
            => EffectsInstalled ? EntityEffectContainer.RestoreTimed(_effects) : null;
        public PlayerPowerSession RestorePowers(PlayerPowerConfig config) => PlayerPowerSession.Restore(config, Powers);

        /// <summary>主线程同一逻辑时点采集，无 await；先检查全部有效效果，拒绝时不改变任何运行对象。</summary>
        [HasGC]
        public static PlatformerSnapshot Capture(string scene, PlayerContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (context.Landing.IsActive) throw new InvalidOperationException("落地停顿期间不能保存；请在动作结束后重试。");
            if (context.HeadJump.IsActive) throw new InvalidOperationException("踩头反弹上升动作期间不能保存；请在动作结束后重试。");
            if (context.Rolling.IsActive) throw new InvalidOperationException("翻滚期间不能保存；请在动作结束后重试。");
            if (context.Dash.IsActive) throw new InvalidOperationException("冲刺动作期间不能保存；请在动作结束后重试。");
            if (context.Ledge.IsActive) throw new InvalidOperationException("挂边／翻越期间不能保存；请离开动作后重试。");
            if (context.DropThrough.IsActive) throw new InvalidOperationException("下穿碰撞豁免期间不能保存；请完全离开平台后重试。");
            var effects = context.Effects?.CaptureRestorableTimed() ?? Array.Empty<EntityTimedEffectSnapshot>();
            return new PlatformerSnapshot(scene, context.Motor.Position, context.Motor.Velocity, context.Facing.FacesRight,
                context.Powers.Capture(), context.Effects != null, effects);
        }
        #endregion
    }
}
