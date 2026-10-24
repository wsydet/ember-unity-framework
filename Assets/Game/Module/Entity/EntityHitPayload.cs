using System;
using UnityEngine;

namespace Game.Entity
{
    /// <summary>命中效果的纯值配置；玩家、陷阱和自定义接收器共用，不含目标或运行句柄。</summary>
    [Serializable]
    public sealed class EntityHitPayload
    {
        #region 编辑器面板参数
        [SerializeField] private string _category = "physical";
        [SerializeField] private float _damage = 1;
        [SerializeField] private Vector2 _knockbackVelocity;
        [SerializeField] private float _braking = 30, _stunSeconds = .15f;
        [SerializeField] private int _priority;
        #endregion
        #region 外部方法
        public string Category => _category;
        public float Damage => _damage;
        public Vector2 KnockbackVelocity => _knockbackVelocity;
        public float Braking => _braking;
        public float StunSeconds => _stunSeconds;
        public int Priority => _priority;
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(_category) || !float.IsFinite(_damage) || _damage < 0 ||
                !float.IsFinite(_knockbackVelocity.x) || !float.IsFinite(_knockbackVelocity.y) ||
                !float.IsFinite(_braking) || _braking < 0 || !float.IsFinite(_stunSeconds) || _stunSeconds < 0)
                throw new InvalidOperationException("HitPayload：类别、伤害、击退/击飞速度、刹车或硬直参数无效。");
        }
        #endregion
    }
}
