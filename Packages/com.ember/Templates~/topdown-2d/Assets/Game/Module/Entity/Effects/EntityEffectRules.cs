using System;

namespace Game.Entity
{
    /// <summary>捕获后的普通规则参数。伤害类别为项目稳定 ID，与 Unity 物理层无关。</summary>
    public sealed class EntityEffectRules
    {
        #region 内部参数
        private readonly string[] _damageCategories;
        public bool PreventsStagger { get; }
        public int ExcludedLayers { get; }
        public int DamageCategoryCount => _damageCategories.Length;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EntityEffectRules(string[] damageCategories, bool preventsStagger, int excludedLayers)
        {
            if (damageCategories == null) throw new ArgumentNullException(nameof(damageCategories));
            foreach (var id in damageCategories)
                if (string.IsNullOrWhiteSpace(id) || id != id.Trim()) throw new ArgumentException("伤害类别需要稳定的非空 ID。");
            _damageCategories = (string[])damageCategories.Clone();
            PreventsStagger = preventsStagger; ExcludedLayers = excludedLayers;
        }
        public string GetDamageCategory(int index) => _damageCategories[index];
        public bool ProtectsDamage(string category)
        {
            if (string.IsNullOrEmpty(category)) return false;
            foreach (var id in _damageCategories) if (string.Equals(id, category, StringComparison.Ordinal)) return true;
            return false;
        }
        #endregion
    }

    /// <summary>外观请求的普通参数；不引用 Renderer，强度不叠加。</summary>
    public readonly struct EntityEffectAppearance
    {
        #region 内部参数
        public bool Enabled { get; }
        public int Priority { get; }
        public float Alpha { get; }
        public float BlinkHz { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public EntityEffectAppearance(bool enabled, int priority, float alpha, float blinkHz)
        {
            if (!float.IsFinite(alpha) || alpha < 0 || alpha > 1 || !float.IsFinite(blinkHz) || blinkHz < 0)
                throw new ArgumentOutOfRangeException(nameof(alpha), "透明度必须为 0—1，闪烁频率必须非负且有限。");
            Enabled = enabled; Priority = priority; Alpha = alpha; BlinkHz = blinkHz;
        }
        public float Evaluate(double elapsedSeconds) => BlinkHz == 0 ? Alpha
            : Alpha + (1 - Alpha) * (float)(.5 + .5 * Math.Cos(2 * Math.PI * BlinkHz * elapsedSeconds));
        #endregion
    }
}
