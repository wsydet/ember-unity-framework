using System;
using Ember.Basic;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    /// <summary>v3 完整不可变快照，显式记录生命装配与当前HP；不保存临时动作进度。</summary>
    public sealed class PlatformerSnapshot
    {
        #region 内部参数
        public const int DATA_VERSION = 3;
        public PlatformerLifeRecord Life { get; }
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
            EntityPowerSnapshot powers, bool effectsInstalled, EntityTimedEffectSnapshot[] effects, PlatformerLifeRecord life = default)
        {
            if (string.IsNullOrWhiteSpace(scene) || scene.Length > 128 ||
                !float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(velocity.x) || !float.IsFinite(velocity.y))
                throw new ArgumentException("快照场景或运动数据无效。");
            if (powers == null || effects == null) throw new ArgumentNullException("完整快照不能缺少资源/效果数据。");
            if (effects.Length > 4096 || !effectsInstalled && effects.Length != 0)
                throw new ArgumentException("效果装配或条目数量无效。");
            Scene = scene; Position = position; Velocity = velocity; FacesRight = facesRight;
            Powers = powers; EffectsInstalled = effectsInstalled;
            Life = life;
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
            => CaptureCore(scene, context, false);

        internal static PlatformerSnapshot CaptureForInteraction(string scene, PlayerContext context)
            => CaptureCore(scene, context, true);

        private static PlatformerSnapshot CaptureCore(string scene, PlayerContext context, bool interaction)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (context.VerticalMotion.OrdinaryActive)
                throw new InvalidOperationException("普通跳独立竖直策略期间不能采集瞬时快照；请使用合法检查点基线组合保存。");
            if (context.Actions.IsActive)
                throw new InvalidOperationException("项目动作期间不能采集瞬时快照；请使用合法检查点基线组合保存。");
            context.ExternalMotion.Prune();
            if (!interaction && context.ExternalMotion.HasTransientMotion)
                throw new InvalidOperationException("外部运动/交互期间不能采集瞬时玩家快照；请使用合法检查点基线组合保存。");
            if (context.Life?.IsDead == true || context.Life?.Reaction is { IsActive: true } || context.Life?.Reaction?.HasPending == true)
                throw new InvalidOperationException("受击或死亡期间不能保存；请完成恢复后重试。");
            if (context.Combat is { IsActive: true } combat && (!combat.IsRecovery || !combat.Finished))
                throw new InvalidOperationException("攻击本体或有效收刀机会不能保存；请等待逻辑动作结束。");
            if (context.Combat?.Dash?.HasResidualMotion == true || context.Combat?.Dash?.OpportunityActive == true)
                throw new InvalidOperationException("冲刺接续或剩余惯性期间不能保存。");
            if (context.Landing.IsActive) throw new InvalidOperationException("落地停顿期间不能保存；请在动作结束后重试。");
            if (context.HeadJump.IsActive) throw new InvalidOperationException("踩头反弹上升动作期间不能保存；请在动作结束后重试。");
            if (context.Rolling.IsActive) throw new InvalidOperationException("翻滚期间不能保存；请在动作结束后重试。");
            if (context.Dash.IsActive) throw new InvalidOperationException("冲刺动作期间不能保存；请在动作结束后重试。");
            if (context.Ledge.IsActive) throw new InvalidOperationException("挂边／翻越期间不能保存；请离开动作后重试。");
            if (context.DropThrough.IsActive) throw new InvalidOperationException("下穿碰撞豁免期间不能保存；请完全离开平台后重试。");
            var effects = context.Effects?.CaptureRestorableTimed() ?? Array.Empty<EntityTimedEffectSnapshot>();
            return new PlatformerSnapshot(scene, context.Motor.Position, context.Motor.Velocity, context.Facing.FacesRight,
                context.Powers.Capture(), context.Effects != null, effects,
                new PlatformerLifeRecord(context.Health != null, context.Health?.Current ?? 0));
        }
        public PlatformerSnapshot WithRecovery(Vector2 position, float health)
            => new(Scene, position, Velocity, FacesRight, Powers, EffectsInstalled, _effects, new PlatformerLifeRecord(Life.Installed, health));
        #endregion
    }

    public readonly struct PlatformerLifeRecord
    {
        public bool Installed { get; }
        public float Health { get; }
        public PlatformerLifeRecord(bool installed, float health)
        {
            if (!float.IsFinite(health) || health < 0 || !installed && health != 0) throw new ArgumentException("生命快照装配或HP无效。");
            Installed = installed; Health = health;
        }
    }
}
