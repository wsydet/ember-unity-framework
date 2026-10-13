using System;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/Feel/Movement", fileName = "PlayerMoveFeel")]
    public sealed class PlayerMoveFeel : ScriptableObject
    {
        #region 编辑器面板参数
        [Header("自动落地运动规则；不含伤害或能力消耗")]
        [SerializeField] private PlayerLandingSettings _landing = new();
        [SerializeField] private PlayerRollingSettings _rolling = new();
        [Header("挂边／翻越；项目启用和解锁由独立 Ability 控制")]
        [SerializeField] private PlayerLedgeSettings _ledge = new();
        [Header("单向平台下穿；启用和解锁由 Ability 控制")]
        [SerializeField, Min(.01f), Tooltip("进入时的向下速度，世界单位/秒；之后使用常规重力。")] private float _dropThroughSpeed = 4;
        [Header("地面水平控制")]
        [SerializeField, Min(0), Tooltip("地面速度，世界单位/秒；关闭独立空中控制时空中也使用此值。")] private float _speed = 5;
        [SerializeField, Tooltip("关闭时即时达到输入目标速度；开启时使用以下起步、刹车、转向加速度。")] private bool _accelerated;
        [SerializeField, Min(0), Tooltip("起步/同向加速，世界单位/秒²。零表示不加速。")] private float _acceleration = 40;
        [SerializeField, Min(0), Tooltip("松键或超过目标速度时的刹车，世界单位/秒²。")] private float _braking = 50;
        [SerializeField, Min(0), Tooltip("输入与当前速度反向时的加速度，世界单位/秒²。")] private float _turning = 60;
        [Header("独立空中水平控制")]
        [SerializeField, Tooltip("关闭时沿用地面水平配置；不影响跳跃或土狼时间。")] private bool _independentAir;
        [SerializeField, Min(0), Tooltip("空中速度，世界单位/秒。")] private float _airSpeed = 5;
        [SerializeField, Tooltip("关闭时空中即时响应；仅在独立空中控制开启时生效。")] private bool _airAccelerated;
        [SerializeField, Min(0), Tooltip("空中起步/同向加速，世界单位/秒²。")] private float _airAcceleration = 30;
        [SerializeField, Min(0), Tooltip("空中松键或超过目标速度时刹车，世界单位/秒²。")] private float _airBraking = 20;
        [SerializeField, Min(0), Tooltip("空中反向输入加速度，世界单位/秒²。")] private float _airTurning = 40;
        [Header("起跳早期响应")]
        [SerializeField, Tooltip("本次有效起跳后，在窗口内有水平输入时即时响应；无输入仍使用常规刹车。不延续到下落或恢复的旧动作。")] private bool _enableJumpResponse;
        [SerializeField, Min(0), Tooltip("起跳早期水平直接响应窗口，游戏秒；独立于输入缓冲和土狼时间。")] private float _jumpResponseDuration = .1f;
        [Header("冲刺运动；项目启用与 CD 由 Skill 装配控制")]
        [SerializeField] private PlayerDashSettings _groundDash = new();
        [SerializeField] private PlayerDashSettings _airDash = new();
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static void ValidateValue(float value)
        {
            if (!float.IsFinite(value) || value < 0)
                throw new InvalidOperationException("启用的水平速度及加速度必须为有限非负值。");
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public float Speed => _speed;
        internal PlayerLandingSpec Landing => (_landing ?? throw new InvalidOperationException("缺少落地配置。")).Capture();
        internal PlayerRollingSpec Rolling => (_rolling ?? throw new InvalidOperationException("缺少翻滚配置。")).Capture();
        internal PlayerLedgeSpec Ledge => _ledge?.Capture() ?? default;
        public float DropThroughSpeed => _dropThroughSpeed;
        public bool Accelerated => _accelerated;
        public float Acceleration => _acceleration;
        public float Braking => _braking;
        public float Turning => _turning;
        public bool IndependentAir => _independentAir;
        public float AirSpeed => _airSpeed;
        public bool AirAccelerated => _airAccelerated;
        public float AirAcceleration => _airAcceleration;
        public float AirBraking => _airBraking;
        public float AirTurning => _airTurning;
        public float JumpResponseDuration => _enableJumpResponse ? _jumpResponseDuration : 0;
        public PlayerDashSettings GroundDash => _groundDash;
        public PlayerDashSettings AirDash => _airDash;
        internal void Validate()
        {
            _ = Landing;
            _ = Rolling;
            if (_groundDash == null || _airDash == null) throw new InvalidOperationException("缺少地面或空中冲刺运动分组。");
            _ = _groundDash.Capture(); _ = _airDash.Capture();
            ValidateValue(_speed);
            if (_enableJumpResponse) ValidateValue(_jumpResponseDuration);
            if (_accelerated) { ValidateValue(_acceleration); ValidateValue(_braking); ValidateValue(_turning); }
            if (!_independentAir) return;
            ValidateValue(_airSpeed);
            if (_airAccelerated) { ValidateValue(_airAcceleration); ValidateValue(_airBraking); ValidateValue(_airTurning); }
        }
        #endregion
    }
}

