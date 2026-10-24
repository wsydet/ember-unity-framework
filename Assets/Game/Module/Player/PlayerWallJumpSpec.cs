using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>可选墙跳分支与独立运动配置；默认关闭，所有时间为游戏秒。</summary>
    [Serializable]
    public struct PlayerWallJumpSpec
    {
        #region 编辑器面板参数
        [SerializeField] private bool _enabled;
        [SerializeField] private bool _firstTowardVertical;
        [SerializeField] private bool _restoreOnConversion;
        [SerializeField] private float _conversionWindow;
        [SerializeField] private float _outwardSpeed;
        [SerializeField] private float _horizontalLimit;
        [SerializeField] private float _overspeedDeceleration;
        [SerializeField] private PlayerOrdinaryJumpSpec _vertical;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public bool Enabled => _enabled;
        public bool FirstTowardVertical => _firstTowardVertical;
        public bool RestoreOnConversion => _restoreOnConversion;
        public float ConversionWindow => _conversionWindow;
        public float OutwardSpeed => _outwardSpeed;
        public float HorizontalLimit => _horizontalLimit;
        public float OverspeedDeceleration => _overspeedDeceleration;
        public PlayerOrdinaryJumpSpec Vertical => _vertical;
        public PlayerWallJumpSpec(PlayerOrdinaryJumpSpec vertical, float outwardSpeed, float horizontalLimit,
            float overspeedDeceleration, bool firstTowardVertical = false, float conversionWindow = 0,
            bool restoreOnConversion = false)
        {
            _enabled = true; _vertical = vertical; _outwardSpeed = outwardSpeed;
            _horizontalLimit = horizontalLimit; _overspeedDeceleration = overspeedDeceleration;
            _firstTowardVertical = firstTowardVertical; _conversionWindow = conversionWindow;
            _restoreOnConversion = restoreOnConversion; Validate();
        }
        internal void Validate()
        {
            if (!_enabled) return;
            _vertical.Validate();
            if (!_vertical.Enabled || !float.IsFinite(_outwardSpeed) || _outwardSpeed < 0 ||
                !float.IsFinite(_horizontalLimit) || _horizontalLimit < 0 ||
                !float.IsFinite(_overspeedDeceleration) || _overspeedDeceleration <= 0 ||
                !float.IsFinite(_conversionWindow) || _conversionWindow < 0)
                throw new InvalidOperationException("墙跳需要独立竖直策略、有限非负速度/窗口和正超速减速度。");
        }
        #endregion
    }
}
