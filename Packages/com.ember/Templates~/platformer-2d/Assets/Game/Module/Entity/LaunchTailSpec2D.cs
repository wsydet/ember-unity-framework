using System;
using UnityEngine;

namespace Game.Entity
{
    /// <summary>恒速保持后的单次自由飞行策略。default 关闭；全部字段值复制，不引用共享手感。</summary>
    [Serializable]
    public struct LaunchTailSpec2D
    {
        #region 编辑器面板参数
        [SerializeField] private bool _enabled;
        [SerializeField] private float _riseAcceleration;
        [SerializeField] private float _fallAcceleration;
        [SerializeField] private float _fallLimit;
        [SerializeField] private bool _slowBand;
        [SerializeField] private float _slowSpeed;
        [SerializeField] private float _slowAcceleration;
        [SerializeField] private bool _latchSlowUntilDescending;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public bool Enabled => _enabled;
        public float RiseAcceleration => _riseAcceleration;
        public float FallAcceleration => _fallAcceleration;
        public float FallLimit => _fallLimit;
        public bool SlowBand => _slowBand;
        public float SlowSpeed => _slowSpeed;
        public float SlowAcceleration => _slowAcceleration;
        public bool LatchSlowUntilDescending => _latchSlowUntilDescending;

        public LaunchTailSpec2D(float riseAcceleration, float fallAcceleration, float fallLimit,
            bool slowBand = false, float slowSpeed = 0, float slowAcceleration = 0,
            bool latchSlowUntilDescending = false)
        {
            _enabled = true; _riseAcceleration = riseAcceleration; _fallAcceleration = fallAcceleration;
            _fallLimit = fallLimit; _slowBand = slowBand; _slowSpeed = slowSpeed;
            _slowAcceleration = slowAcceleration; _latchSlowUntilDescending = latchSlowUntilDescending;
            Validate();
        }

        public void Validate()
        {
            if (!_enabled) return;
            if (!float.IsFinite(_riseAcceleration) || _riseAcceleration <= 0 ||
                !float.IsFinite(_fallAcceleration) || _fallAcceleration <= 0 ||
                !float.IsFinite(_fallLimit) || _fallLimit <= 0 ||
                _latchSlowUntilDescending && !_slowBand ||
                _slowBand && (!float.IsFinite(_slowSpeed) || _slowSpeed < 0 || _slowSpeed >= _fallLimit ||
                    !float.IsFinite(_slowAcceleration) || _slowAcceleration <= 0 ||
                    _slowAcceleration > Mathf.Min(_riseAcceleration, _fallAcceleration)))
                throw new InvalidOperationException("弹射尾段需要有限正加速度/终速；低速范围须小于终速，低速加速度不得大于上升或下降加速度。");
        }

        public void ValidateWorld(Vector2 gravity)
        {
            Validate();
            if (_enabled && (gravity.x != 0 || !float.IsFinite(gravity.y) || gravity.y >= 0 ||
                !float.IsFinite(_riseAcceleration / -gravity.y) || !float.IsFinite(_fallAcceleration / -gravity.y)))
                throw new InvalidOperationException("弹射尾段需要纯竖直向下重力及有限倍率。");
        }
        #endregion
    }
}
