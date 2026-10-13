using System;
using UnityEngine;

namespace Game.Player
{
    public enum PlayerAirJumpHorizontalMode { PreserveVelocity, FacingSpeed }
    public enum PlayerJumpParameterMode { HeightAndApexTime, Direct }
    public enum PlayerShortJumpMode { ReleaseSpeedLimit, MinimumHeightVelocityReduction }

    [CreateAssetMenu(menuName = "Game/Player/Feel/Jump", fileName = "PlayerJumpFeel")]
    public sealed class PlayerJumpFeel : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerJumpParameterMode _mode = PlayerJumpParameterMode.Direct;
        [SerializeField, Min(.01f), Tooltip("基础高度，世界单位；未计入短跳等修正和离散物理。")] private float _height = 1.699f;
        [SerializeField, Min(.01f), Tooltip("基础到顶时间，游戏秒。")] private float _apexTime = .34f;
        [SerializeField, Min(.01f), Tooltip("仅直接模式生效，世界单位/秒。")] private float _launchSpeed = 10;
        [SerializeField, Min(0), Tooltip("仅直接模式生效，Unity 2D 重力倍率。")] private float _gravityScale = 3;
        [SerializeField] private bool _enableShortJump = true;
        [SerializeField, Tooltip("短跳策略互斥；仅在本次有效起跳的松键触发时修改速度。")] private PlayerShortJumpMode _shortJumpMode;
        [SerializeField, Min(.01f), Tooltip("松键上升限速，世界单位/秒。")] private float _releaseSpeed = 4;
        [SerializeField, Min(0), Tooltip("最低高度策略：相对起跳点的上升距离，世界单位；碰顶或上升结束不会强求达到此高度。")] private float _minimumHeight = .5f;
        [SerializeField, Range(0, 1), Tooltip("最低高度后松键削速：触发时将当前上升速度乘此倍率，只执行一次。")] private float _releaseMultiplier = .6666667f;
        [Header("竖直运动组合")]
        [SerializeField] private bool _enableApexGravity;
        [SerializeField, Min(0), Tooltip("空中竖直速度绝对值不超过此值时使用顶点重力，世界单位/秒。")] private float _apexSpeed = 4;
        [SerializeField, Range(.01f, 1), Tooltip("顶点区间优先使用此基础重力倍率，不与下落倍率相乘。")] private float _apexMultiplier = .75f;
        [SerializeField] private bool _enableFallGravity;
        [SerializeField, Min(.01f), Tooltip("空中下降且不处于有效顶点区间时的基础重力倍率。")] private float _fallMultiplier = 1.5f;
        [SerializeField] private bool _enableFallLimit;
        [SerializeField, Min(.01f), Tooltip("最大向下速度，世界单位/秒；结算时包含本物理步重力增量。")] private float _fallLimit = 35;
        [SerializeField] private bool _enableEdgeHover;
        [SerializeField, Min(0), Tooltip("自然离地悬停游戏秒数；独立于土狼资格，松方向不取消，起跳/落地取消。")] private float _edgeHoverDuration = .0625f;
        [Header("踩头反弹：启用和次数由 Powers 管理")]
        [SerializeField, Min(.01f)] private float _headJumpSpeed = 12.528f;
        [Header("二段跳：启用和次数由 Player Powers 唯一管理")]
        [SerializeField, Min(.01f), Tooltip("二段跳起跳速度，世界单位/秒；独立于普通 Jump，不应用短跳。")] private float _airJumpSpeed = 10;
        [SerializeField, Tooltip("只在进入动作的本物理步处理水平速度，之后恢复常规空中控制。")] private PlayerAirJumpHorizontalMode _airJumpHorizontalMode;
        [SerializeField, Min(0), Tooltip("按朝向设置速度模式，世界单位/秒。")] private float _airJumpHorizontalSpeed = 6.4f;
        [Header("墙面动作：启用和解锁由 Powers 管理；速度 u/s，时长游戏秒")]
        [SerializeField] private float _wallSlideSpeed = 0.5f;
        [SerializeField, Tooltip("关闭时向下输入仍使用普通墙滑速度。保持已有方案默认行为。")] private bool _enableWallFastSlide = true;
        [SerializeField] private float _wallFastSlideSpeed = 8f;
        [SerializeField] private float _wallJumpX = 5.44f;
        [SerializeField] private float _wallJumpY = 11.22f;
        [SerializeField] private float _wallProtection = 0.1f;
        [SerializeField] private bool _wallReturnEnabled = true;
        [SerializeField] private float _wallReturnDuration = 0.1f;
        [SerializeField] private float _wallReturnMultiplier = 0.8f;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public float WallSlideSpeed => _wallSlideSpeed;
        public bool EnableWallFastSlide => _enableWallFastSlide;
        public float WallFastSlideSpeed => _enableWallFastSlide ? _wallFastSlideSpeed : _wallSlideSpeed;
        public float WallJumpX => _wallJumpX;
        public float WallJumpY => _wallJumpY;
        public float WallProtection => _wallProtection;
        public bool WallReturnEnabled => _wallReturnEnabled;
        public float WallReturnDuration => _wallReturnDuration;
        public float WallReturnMultiplier => _wallReturnMultiplier;
        public float HeadJumpSpeed => _headJumpSpeed;
        public float AirJumpSpeed => _airJumpSpeed;
        public PlayerAirJumpHorizontalMode AirJumpHorizontalMode => _airJumpHorizontalMode;
        public float AirJumpHorizontalSpeed => _airJumpHorizontalSpeed;
        public PlayerJumpParameterMode Mode => _mode;
        public bool EnableShortJump => _enableShortJump;
        public PlayerShortJumpMode ShortJumpMode => _shortJumpMode;
        public float ReleaseSpeed => _releaseSpeed;
        public float MinimumHeight => _minimumHeight;
        public float ReleaseMultiplier => _releaseMultiplier;
        public bool ApexGravity => _enableApexGravity;
        public float ApexSpeed => _apexSpeed;
        public float ApexMultiplier => _apexMultiplier;
        public bool FallGravity => _enableFallGravity;
        public float FallMultiplier => _fallMultiplier;
        public bool FallLimitEnabled => _enableFallLimit;
        public float FallLimit => _fallLimit;
        public float EdgeHoverDuration => _enableEdgeHover ? _edgeHoverDuration : 0;
        internal void Resolve(Vector2 worldGravity, out float launchSpeed, out float gravityScale)
        {
            if (_mode == PlayerJumpParameterMode.HeightAndApexTime)
            {
                if (!float.IsFinite(_height) || _height <= 0 || !float.IsFinite(_apexTime) || _apexTime <= 0 ||
                    !float.IsFinite(worldGravity.y) || worldGravity.y >= 0 || worldGravity.x != 0)
                    throw new InvalidOperationException("高度/到顶模式需要正的有限高度与时间，以及纯竖直向下的世界重力。");
                launchSpeed = 2 * _height / _apexTime;
                gravityScale = (2 * _height / (_apexTime * _apexTime)) / -worldGravity.y;
            }
            else if (_mode == PlayerJumpParameterMode.Direct)
            { launchSpeed = _launchSpeed; gravityScale = _gravityScale; }
            else throw new InvalidOperationException("未知跳跃参数模式。");
            if (!float.IsFinite(launchSpeed) || launchSpeed <= 0 || !float.IsFinite(gravityScale) || gravityScale < 0)
                throw new InvalidOperationException("跳跃速度/重力计算结果无效。");
            if (_enableApexGravity || _enableFallGravity || _enableFallLimit || _enableEdgeHover)
            {
                if (!float.IsFinite(worldGravity.y) || worldGravity.y >= 0 || worldGravity.x != 0)
                    throw new InvalidOperationException("竖直运动组合需要纯竖直向下的有限世界重力。");
                if (_enableApexGravity && (!float.IsFinite(_apexSpeed) || _apexSpeed < 0 ||
                    !float.IsFinite(_apexMultiplier) || _apexMultiplier <= 0 || _apexMultiplier > 1))
                    throw new InvalidOperationException("顶点速度阈值必须有限非负，倍率在 (0, 1]。");
                if (_enableFallGravity && (!float.IsFinite(_fallMultiplier) || _fallMultiplier <= 0 ||
                    !float.IsFinite(gravityScale * _fallMultiplier)))
                    throw new InvalidOperationException("下落倍率及有效重力必须为有效有限值。");
                if (_enableFallLimit && (!float.IsFinite(_fallLimit) || _fallLimit <= 0))
                    throw new InvalidOperationException("下落限速必须为有限正数。");
                if (_enableEdgeHover && (!float.IsFinite(_edgeHoverDuration) || _edgeHoverDuration < 0))
                    throw new InvalidOperationException("悬停时长必须为有限非负值。");
            }
            if (!_enableShortJump) return;
            if (_shortJumpMode == PlayerShortJumpMode.ReleaseSpeedLimit)
            {
                if (!float.IsFinite(_releaseSpeed) || _releaseSpeed <= 0 || _releaseSpeed > launchSpeed)
                    throw new InvalidOperationException("短跳限速必须为正数，启用时不能超过起跳速度。");
            }
            else if (_shortJumpMode == PlayerShortJumpMode.MinimumHeightVelocityReduction)
            {
                if (!float.IsFinite(_minimumHeight) || _minimumHeight < 0 ||
                    !float.IsFinite(_releaseMultiplier) || _releaseMultiplier < 0 || _releaseMultiplier > 1)
                    throw new InvalidOperationException("最低高度必须有限非负，削速倍率必须在 0 到 1 之间。");
            }
            else throw new InvalidOperationException("未知短跳策略。");
        }
        #endregion
    }
}

