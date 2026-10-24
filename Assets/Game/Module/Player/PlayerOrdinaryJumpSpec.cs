using System;
using UnityEngine;

namespace Game.Player
{
    public enum PlayerOrdinaryJumpRelease { EndHold, LimitSpeed, MultiplySpeed }
    public enum PlayerFlightOrigin { Natural, OrdinaryJump, External, WallJump, AirJump, HeadJump, Action }

    /// <summary>普通起跳独立竖直配置。值复制进入运行会话；默认值关闭，不改变旧方案。</summary>
    [Serializable]
    public struct PlayerOrdinaryJumpSpec
    {
        #region 编辑器面板参数
        [SerializeField] private bool _enabled;
        [SerializeField, Min(.01f)] private float _launchSpeed;
        [SerializeField, Min(0), Tooltip("最短恒速上升游戏秒数，松键仍完成；碰顶立即终止。")] private float _minimumHold;
        [SerializeField, Min(0), Tooltip("按住时最长恒速上升游戏秒数，包含起跳物理步。")] private float _maximumHold;
        [SerializeField, Min(.01f), Tooltip("上升重力，世界单位/秒²。")] private float _riseGravity;
        [SerializeField, Min(.01f), Tooltip("下降重力，世界单位/秒²。")] private float _fallGravity;
        [SerializeField, Min(.01f)] private float _fallLimit;
        [SerializeField] private PlayerOrdinaryJumpRelease _release;
        [SerializeField, Min(0), Tooltip("LimitSpeed 为 u/s；MultiplySpeed 为 0..1；EndHold 不使用此值。")] private float _releaseValue;
        [SerializeField] private bool _apexEnabled;
        [SerializeField] private bool _apexRequiresHeld;
        [SerializeField, Min(0)] private float _apexSpeed;
        [SerializeField, Range(.01f, 1)] private float _apexMultiplier;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public bool Enabled => _enabled;
        public float LaunchSpeed => _launchSpeed;
        public float MinimumHold => _minimumHold;
        public float MaximumHold => _maximumHold;
        public float RiseGravity => _riseGravity;
        public float FallGravity => _fallGravity;
        public float FallLimit => _fallLimit;
        public PlayerOrdinaryJumpRelease Release => _release;
        public float ReleaseValue => _releaseValue;
        public bool ApexEnabled => _apexEnabled;
        public bool ApexRequiresHeld => _apexRequiresHeld;
        public float ApexSpeed => _apexSpeed;
        public float ApexMultiplier => _apexMultiplier;

        public PlayerOrdinaryJumpSpec(float launchSpeed, float minimumHold, float maximumHold,
            float riseGravity, float fallGravity, float fallLimit,
            PlayerOrdinaryJumpRelease release = PlayerOrdinaryJumpRelease.EndHold, float releaseValue = 0,
            bool apexEnabled = false, bool apexRequiresHeld = true, float apexSpeed = 5, float apexMultiplier = .5f)
        {
            _enabled = true; _launchSpeed = launchSpeed; _minimumHold = minimumHold; _maximumHold = maximumHold;
            _riseGravity = riseGravity; _fallGravity = fallGravity; _fallLimit = fallLimit;
            _release = release; _releaseValue = releaseValue; _apexEnabled = apexEnabled;
            _apexRequiresHeld = apexRequiresHeld; _apexSpeed = apexSpeed; _apexMultiplier = apexMultiplier;
            Validate();
        }

        internal void Validate()
        {
            if (!_enabled) return;
            if (!float.IsFinite(_launchSpeed) || _launchSpeed <= 0 || !float.IsFinite(_minimumHold) || _minimumHold < 0 ||
                !float.IsFinite(_maximumHold) || _maximumHold < _minimumHold ||
                !float.IsFinite(_riseGravity) || _riseGravity <= 0 || !float.IsFinite(_fallGravity) || _fallGravity <= 0 ||
                !float.IsFinite(_fallLimit) || _fallLimit <= 0 || !Enum.IsDefined(typeof(PlayerOrdinaryJumpRelease), _release))
                throw new InvalidOperationException("普通跳独立策略需要有限正速度/重力/限速，且 0 ≤ 最短保持 ≤ 最长保持。");
            if (_release != PlayerOrdinaryJumpRelease.EndHold && (!float.IsFinite(_releaseValue) || _releaseValue < 0 ||
                _releaseValue > (_release == PlayerOrdinaryJumpRelease.MultiplySpeed ? 1 : _launchSpeed)))
                throw new InvalidOperationException("普通跳松键值无效：削速倍率为 0..1，限速为 0..起跳速度。");
            if (_apexEnabled && (!float.IsFinite(_apexSpeed) || _apexSpeed < 0 || !float.IsFinite(_apexMultiplier) ||
                _apexMultiplier <= 0 || _apexMultiplier > 1))
                throw new InvalidOperationException("普通跳顶点阈值须有限非负，倍率为 (0,1]。");
        }
        #endregion
    }
}
