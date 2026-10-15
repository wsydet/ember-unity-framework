using System;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/Feel/Profile", fileName = "PlayerFeelProfile")]
    public sealed class PlayerFeelProfile : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerMoveFeel _movement;
        [SerializeField] private PlayerJumpFeel _jump;
        [SerializeField] private PlayerInputFeel _input;
        [SerializeField] private PlayerAttackProfile _attack = new();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public PlayerMoveFeel Movement => _movement;
        public PlayerJumpFeel Jump => _jump;
        public PlayerInputFeel Input => _input;
        public PlayerAttackProfile Attack => _attack;
        internal void SetAttackConfiguration(PlayerAttackProfile attack) => _attack = PlayerCombatData.Copy(attack);
        public void Configure(PlayerMoveFeel movement, PlayerJumpFeel jump, PlayerInputFeel input)
        { _movement = movement; _jump = jump; _input = input; }
        public PlayerFeelRuntime Capture(Vector2 worldGravity)
        {
            if (_movement == null || _jump == null || _input == null)
                throw new InvalidOperationException("手感预设缺少移动、跳跃或输入窗口配置。");
            _movement.Validate(); _input.Validate();
            if (_attack == null) throw new InvalidOperationException("Feel.Attack：配置为空。");
            _attack.Validate();
            _jump.Resolve(worldGravity, out var launch, out var gravity);
            return new PlayerFeelRuntime(_movement, launch, gravity, _jump.ReleaseSpeed,
                _jump.EnableShortJump, _input.BufferDuration, _input.CoyoteDuration, worldGravity,
                _jump.ShortJumpMode, _jump.MinimumHeight, _jump.ReleaseMultiplier, _jump, _attack, _input);
        }
        /// <summary>创建未保存的独立预设及三份配置；调用方负责保存资产或销毁全部四个对象。</summary>
        public PlayerFeelProfile CreateIndependentCopy()
        {
            if (_movement == null || _jump == null || _input == null)
                throw new InvalidOperationException("不能复制缺少分组配置的预设。");
            var result = Instantiate(this);
            result._movement = Instantiate(_movement); result._jump = Instantiate(_jump); result._input = Instantiate(_input);
            result._attack = PlayerCombatData.Copy(_attack);
            return result;
        }
        #endregion
    }

    /// <summary>准备时一次采集的只读有效参数，不持有配置 SO；估算不包含碰撞、短跳修正和物理离散误差。</summary>
    public sealed class PlayerFeelRuntime
    {
        #region 内部参数
        public float MoveSpeed { get; }
        public PlayerAttackProfile Attack { get; }
        public PlayerCorrectionSpec JumpCorrection { get; }
        public PlayerCorrectionSpec GroundDashCorrection { get; }
        public PlayerCorrectionSpec AirDashCorrection { get; }
        public PlayerCorrectionSpec StepCorrection { get; }
        public float DropThroughSpeed { get; }
        public PlayerLedgeSpec Ledge { get; }
        public PlayerLandingSpec Landing { get; }
        public PlayerRollingSpec Rolling { get; }
        public float HeadJumpSpeed { get; }
        public PlayerDashMotionSpec GroundDash { get; }
        public PlayerDashMotionSpec AirDash { get; }
        public bool Accelerated { get; }
        public float Acceleration { get; }
        public float Braking { get; }
        public float Turning { get; }
        public bool IndependentAir { get; }
        public float AirSpeed { get; }
        public bool AirAccelerated { get; }
        public float AirAcceleration { get; }
        public float AirBraking { get; }
        public float AirTurning { get; }
        public float JumpResponseDuration { get; }
        public float EffectiveAirSpeed => IndependentAir ? AirSpeed : MoveSpeed;
        public float LaunchSpeed { get; }
        public float GravityScale { get; }
        public float ReleaseSpeed { get; }
        public bool ShortJump { get; }
        public PlayerShortJumpMode ShortJumpMode { get; }
        public float MinimumHeight { get; }
        public float ReleaseMultiplier { get; }
        public float BufferDuration { get; }
        public float AttackBufferDuration { get; }
        public float CombatDashBufferDuration { get; }
        public float CombatJumpBufferDuration { get; }
        public float CoyoteDuration { get; }
        public float EstimatedHeight { get; }
        public float EstimatedApexTime { get; }
        public bool ApexGravity { get; }
        public float ApexSpeed { get; }
        public float ApexMultiplier { get; }
        public bool FallGravity { get; }
        public float FallMultiplier { get; }
        public bool FallLimitEnabled { get; }
        public float FallLimit { get; }
        public float EdgeHoverDuration { get; }
        public float WallSlideSpeed { get; }
        public float WallFastSlideSpeed { get; }
        public float WallJumpX { get; }
        public float WallJumpY { get; }
        public float WallProtection { get; }
        public bool WallReturnEnabled { get; }
        public float WallReturnDuration { get; }
        public float WallReturnMultiplier { get; }
        public float AirJumpSpeed { get; }
        public PlayerAirJumpHorizontalMode AirJumpHorizontalMode { get; }
        public float AirJumpHorizontalSpeed { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerFeelRuntime(PlayerMoveFeel movement, float launch, float gravity, float release, bool shortJump,
            float buffer, float coyote, Vector2 worldGravity, PlayerShortJumpMode shortJumpMode,
            float minimumHeight, float releaseMultiplier, PlayerJumpFeel jump, PlayerAttackProfile attack, PlayerInputFeel input)
        {
            Attack = PlayerCombatData.Copy(attack);
            AttackBufferDuration = input.AttackBufferDuration;
            CombatDashBufferDuration = input.CombatDashBufferDuration;
            CombatJumpBufferDuration = input.CombatJumpBufferDuration;
            WallSlideSpeed = jump.WallSlideSpeed; DropThroughSpeed = movement.DropThroughSpeed;
            JumpCorrection = movement.JumpCorrection; GroundDashCorrection = movement.GroundDashCorrection;
            AirDashCorrection = movement.AirDashCorrection; StepCorrection = movement.StepCorrection;
            Ledge = movement.Ledge;
            Landing = movement.Landing;
            Rolling = movement.Rolling; HeadJumpSpeed = jump.HeadJumpSpeed;
            GroundDash = movement.GroundDash.Capture(); AirDash = movement.AirDash.Capture();
            WallFastSlideSpeed = jump.WallFastSlideSpeed;
            WallJumpX = jump.WallJumpX;
            WallJumpY = jump.WallJumpY;
            WallProtection = jump.WallProtection;
            WallReturnEnabled = jump.WallReturnEnabled;
            WallReturnDuration = jump.WallReturnDuration;
            WallReturnMultiplier = jump.WallReturnMultiplier;
            AirJumpSpeed = jump.AirJumpSpeed; AirJumpHorizontalMode = jump.AirJumpHorizontalMode;
            AirJumpHorizontalSpeed = jump.AirJumpHorizontalSpeed;
            MoveSpeed = movement.Speed; Accelerated = movement.Accelerated;
            Acceleration = movement.Acceleration; Braking = movement.Braking; Turning = movement.Turning;
            IndependentAir = movement.IndependentAir; AirSpeed = movement.AirSpeed;
            AirAccelerated = movement.AirAccelerated; AirAcceleration = movement.AirAcceleration;
            AirBraking = movement.AirBraking; AirTurning = movement.AirTurning;
            JumpResponseDuration = movement.JumpResponseDuration;
            LaunchSpeed = launch; GravityScale = gravity; ReleaseSpeed = release;
            ShortJump = shortJump; BufferDuration = buffer; CoyoteDuration = coyote;
            ShortJumpMode = shortJumpMode; MinimumHeight = minimumHeight; ReleaseMultiplier = releaseMultiplier;
            ApexGravity = jump.ApexGravity; ApexSpeed = jump.ApexSpeed; ApexMultiplier = jump.ApexMultiplier;
            FallGravity = jump.FallGravity; FallMultiplier = jump.FallMultiplier;
            FallLimitEnabled = jump.FallLimitEnabled; FallLimit = jump.FallLimit; EdgeHoverDuration = jump.EdgeHoverDuration;
            float acceleration = -worldGravity.y * gravity;
            EstimatedApexTime = acceleration > 0 ? launch / acceleration : float.PositiveInfinity;
            EstimatedHeight = acceleration > 0 ? launch * launch / (2 * acceleration) : float.PositiveInfinity;
        }

        /// <summary>常规水平规则的统一计算；由当前支撑事实选择配置，不读取旧状态类型或修改计时。</summary>
        internal float CalculateHorizontalSpeed(float current, float direction, bool grounded, float deltaTime)
        {
            bool air = !grounded && IndependentAir;
            float target = Mathf.Clamp(direction, -1, 1) * (air ? AirSpeed : MoveSpeed);
            if (!(air ? AirAccelerated : Accelerated)) return target;
            bool turning = current * target < 0;
            bool braking = target == 0 || (!turning && Mathf.Abs(current) > Mathf.Abs(target));
            float rate = turning ? (air ? AirTurning : Turning)
                : braking ? (air ? AirBraking : Braking) : (air ? AirAcceleration : Acceleration);
            return Mathf.MoveTowards(current, target, rate * deltaTime);
        }
        #endregion
    }
}

