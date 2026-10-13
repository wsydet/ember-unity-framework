using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>每角色的地面跳跃机会及离地宽限计时；不读取输入，不请求转换，不执行运动。</summary>
    public sealed class PlayerJumpWindow
    {
        #region 内部参数
        private readonly float _coyoteDuration;
        private float _remaining;
        /// <summary>当前离地宽限剩余秒数；在地面上显示零，读取不会推进或消费机会。</summary>
        public float CoyoteRemaining => HasGroundSupport ? 0 : _remaining;
        public bool CanJump { get; private set; }
        public bool HasGroundSupport { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public PlayerJumpWindow(float coyoteDuration)
        {
            if (!float.IsFinite(coyoteDuration) || coyoteDuration < 0)
                throw new ArgumentOutOfRangeException(nameof(coyoteDuration));
            _coyoteDuration = coyoteDuration;
        }
        /// <summary>统一确认支撑事实，返回本步是否首次取得有效支撑；连续支撑不重复补充资源。</summary>
        public bool Observe(bool grounded, float verticalSpeed, float dt)
        {
            // 起跳后仍在地面探测范围内时，不能重发地面机会。
            bool supported = grounded && verticalSpeed <= 0;
            bool acquired = supported && !HasGroundSupport;
            HasGroundSupport = supported;
            if (supported)
            { _remaining = _coyoteDuration; CanJump = true; }
            else
            {
                _remaining = Mathf.Max(0, _remaining - dt);
                CanJump = _remaining > 0.000001f; // 避免多次 float 扣减让边界多活一整步。
            }
            return acquired;
        }
        internal void ClearSupport() { Consume(); HasGroundSupport = false; }
        public void Consume() { _remaining = 0; CanJump = false; }
        #endregion
    }
}
