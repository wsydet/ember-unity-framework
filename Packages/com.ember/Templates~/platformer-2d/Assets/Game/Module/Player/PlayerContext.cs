using Game.Entity;

namespace Game.Player
{
    /// <summary>每角色独立能力视图；不查询全局服务，不驱动状态机。</summary>
    public sealed class PlayerContext
    {
#if UNITY_EDITOR
        public PlayerMotionTrace MotionTrace { get; } = new();
#endif
        public Entity2D Entity { get; }
        public EntityMotor2D Motor { get; }
        public EntitySensors2D Sensors { get; }
        public EntityFacing2D Facing { get; }
        public PlayerInputAdapter Input { get; }
        public PlayerExternalMotion ExternalMotion { get; }
        public PlayerActionSession Actions { get; }
        public PlayerSupportMotion Support { get; }
        internal PlayerMotionControl MotionControl { get; } = new();
        internal PlayerCombatMotion Combat { get; set; }
        internal EntityHitReceiver HitReceiver { get; set; }
        internal PlayerLifeSession Life { get; set; }
        public EntityHealth Health => Life?.Health;
        public bool ShowHealth => Life?.Configuration.Life is { Enabled: true, ShowHealth: true };
        public PlayerJumpWindow JumpWindow { get; }
        public PlayerFeelRuntime Feel { get; }
        public PlayerWallMotion Wall { get; }
        public PlayerDashMotion Dash { get; }
        public PlayerDropThroughMotion DropThrough { get; }
        public PlayerLedgeMotion Ledge { get; }
        public PlayerLandingMotion Landing { get; }
        public PlayerRollingMotion Rolling { get; }
        public PlayerHeadJumpMotion HeadJump { get; }
        public PlayerCollisionCorrection Correction { get; }
        /// <summary>固定步中 Motor 尚未提交刚体速度；所有地面入口共用同一实际速度与输入事实。</summary>
        internal string GroundMotionState
        {
            get
            {
                float direction = Input.Horizontal;
                if (!Feel.Accelerated) return direction == 0 ? "idle" : "move";
                const float tolerance = .0001f;
                float target = UnityEngine.Mathf.Clamp(direction, -1, 1) * Feel.MoveSpeed;
                float speed = ExternalMotion.SelfVelocity.x;
                if (target == 0) return UnityEngine.Mathf.Abs(speed) <= tolerance ? "idle" : "decelerate";
                if (UnityEngine.Mathf.Abs(speed - target) <= tolerance) return "move";
                if (speed * target < 0 || UnityEngine.Mathf.Abs(speed) > UnityEngine.Mathf.Abs(target)) return "decelerate";
                return "accelerate";
            }
        }
        public int ContactWall => Sensors.LeftWall != null && Sensors.RightWall != null
            ? (Input.Horizontal != 0 ? (Input.Horizontal > 0 ? 1 : -1) : (Facing.FacesRight ? 1 : -1))
            : Sensors.RightWall != null ? 1 : Sensors.LeftWall != null ? -1 : 0;
        public bool CanStartWallJump => !Ledge.IsActive && !DropThrough.IsActive && Input.JumpRequested && !JumpWindow.CanJump && !JumpWindow.HasGroundSupport
            && Wall.ProtectionRemaining <= 0 && ContactWall != 0
            && Abilities.Query(PlayerWallJumpState.ABILITY_ID) == EntityUseStatus.Available;
        public bool CanContinueWallSlide => !Ledge.IsActive && !DropThrough.IsActive && !JumpWindow.HasGroundSupport && Motor.Velocity.y <= 0
            && Wall.ProtectionRemaining <= 0 && ContactWall != 0 && Input.Horizontal * ContactWall > 0
            && Abilities.TryGet(PlayerWallSlideState.ABILITY_ID, out var slide) && slide.IsEnabled && slide.IsOpen;
        public bool CanStartWallSlide => !Wall.Sliding && CanContinueWallSlide
            && Abilities.Query(PlayerWallSlideState.ABILITY_ID) == EntityUseStatus.Available;
        public PlayerVerticalMotion VerticalMotion { get; }
        internal PlayerPowerSession Powers { get; }
        public EntityEffectContainer Effects { get; }
        internal string FrozenActionEffects { get; set; }
        public EntityPowerSnapshot CapturePowerSnapshot() => Powers.Capture();
        public EntityAbilitySet Abilities => Powers.Abilities;
        public EntitySkillSet Skills => Powers.Skills;
        public PlayerPowerProgress PowerProgress => Powers.Progress;
        public bool CanStartAirJump => !Ledge.IsActive && !DropThrough.IsActive && Input.JumpRequested && !JumpWindow.CanJump && !JumpWindow.HasGroundSupport
            && Abilities.Query(PlayerAirJumpState.ABILITY_ID) == EntityUseStatus.Available;
        public bool CanStartJump => !Ledge.IsActive && !DropThrough.IsActive && Input.JumpRequested && JumpWindow.CanJump;
        internal PlayerContext(Entity2D entity, EntityMotor2D motor, EntitySensors2D sensors,
            EntityFacing2D facing, PlayerInputAdapter input, PlayerJumpWindow jumpWindow, PlayerPowerSession powers,
            EntityEffectContainer effects, PlayerFeelRuntime feel)
        { Entity = entity; Motor = motor; Sensors = sensors; Facing = facing; Input = input; JumpWindow = jumpWindow; Powers = powers; Effects = effects; Feel = feel; Support = new PlayerSupportMotion(this); ExternalMotion = new PlayerExternalMotion(this); Actions = new PlayerActionSession(this); VerticalMotion = new PlayerVerticalMotion(feel); Wall = new PlayerWallMotion(this); Dash = new PlayerDashMotion(this); DropThrough = new PlayerDropThroughMotion(this); Ledge = new PlayerLedgeMotion(this); Landing = new PlayerLandingMotion(this); Rolling = new PlayerRollingMotion(this); HeadJump = new PlayerHeadJumpMotion(this); Correction = new PlayerCollisionCorrection(this); }
    }
}
