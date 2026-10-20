using System;
using Game.Entity;

namespace Game.Player
{
    /// <summary>冻结装配下的生命与受击会话；接收回调只排队，状态接管通过已声明的 FSM 节点。</summary>
    internal sealed class PlayerLifeSession
    {
        #region 内部参数
        private readonly PlayerContext _context;
        internal PlayerCombatConfiguration Configuration { get; }
        internal EntityHealth Health { get; }
        internal EntityReactionMotion Reaction { get; }
        internal EntityHitReceiver Receiver { get; }
        internal bool IsDead => Receiver.IsDead;
        internal bool DeathCommitted { get; private set; }
        internal IPlayerDeathHandler CustomDeath { get; }
        internal IPlayerRecoveryPositionProvider CustomPosition { get; }
        #endregion
        #region 外部方法
        internal PlayerLifeSession(PlayerContext context, PlayerCombatConfiguration config, float? savedHealth = null)
        {
            _context = context; Configuration = config;
            var life = config.Life;
            foreach (var component in context.Entity.GetComponents<UnityEngine.MonoBehaviour>())
            {
                if (component is IPlayerDeathHandler handler && handler.HandlerId == life.CustomDeathHandler)
                { if (CustomDeath != null) throw new InvalidOperationException("自定义死亡处理ID重复。"); CustomDeath = handler; }
                if (component is IPlayerRecoveryPositionProvider position && position.ProviderId == config.Recovery.CustomPositionProvider)
                { if (CustomPosition != null) throw new InvalidOperationException("恢复位置提供方ID重复。"); CustomPosition = position; }
            }
            if (life.Enabled && life.Death == PlayerDeathMode.Custom && CustomDeath == null)
                throw new InvalidOperationException("未装配自定义死亡处理：" + life.CustomDeathHandler);
            if (config.Recovery.Position == PlayerRecoveryPositionMode.Custom && CustomPosition == null)
                throw new InvalidOperationException("未装配自定义恢复位置：" + config.Recovery.CustomPositionProvider);
            Health = life.Enabled ? new EntityHealth(life.Maximum, savedHealth ?? life.Initial) : null;
            Reaction = config.Reactions.Enabled ? new EntityReactionMotion() : null;
            Receiver = new EntityHitReceiver(Health, context.Effects, Reaction, life.Multiplier, life.Reduction,
                life.DamageMode == PlayerDamageMode.Fixed, life.FixedDamage, life.Death == PlayerDeathMode.NonLethal,
                config.DamageProtection);
        }
        private void ClearAction()
        {
            _context.Combat?.Leave(); _context.Combat?.Dash?.Cancel(); _context.Input.Clear();
            _context.Dash.End(); _context.Wall.Cancel(); _context.Ledge.End(); _context.DropThrough.Dispose();
            _context.Landing.End(); _context.Rolling.End(); _context.VerticalMotion.NotifyJump();
        }
        internal void BeginReaction()
        {
            // 在受击提交的同一步释放弹射凭证，不能等下一个 Tick 才清理输入锁和来源。
            _context.ExternalMotion.Clear();
            ClearAction(); Reaction.Begin(_context.Motor);
            _context.Input.BeginReactionBuffer();
            _context.Motor.SetGravityScale(_context.Feel.GravityScale);
            _context.MotionControl.Take(this, PlayerMotionChannels.Horizontal | PlayerMotionChannels.Facing |
                PlayerMotionChannels.AutomaticTransitions | PlayerMotionChannels.Wall | PlayerMotionChannels.Ledge | PlayerMotionChannels.Vertical | PlayerMotionChannels.Correction);
        }
        internal void EndReaction() { Reaction.End(); _context.Landing.End(); _context.MotionControl.Release(this); _context.Input.FinishReactionBuffer(); }
        internal void BeginDeath()
        {
            if (DeathCommitted || !IsDead) throw new InvalidOperationException("死亡状态只能提交一次。");
            DeathCommitted = true; _context.ExternalMotion.Clear(); ClearAction(); Reaction?.Clear();
            _context.MotionControl.Take(this, PlayerMotionChannels.Horizontal | PlayerMotionChannels.Facing |
                PlayerMotionChannels.AutomaticTransitions | PlayerMotionChannels.Wall | PlayerMotionChannels.Ledge |
                PlayerMotionChannels.Vertical | PlayerMotionChannels.Correction);
            HoldDeath();
        }
        internal void HoldDeath()
        {
            _context.Input.Clear(); _context.Motor.SetHorizontalVelocity(0); _context.Motor.SetVerticalVelocity(0);
            _context.Motor.SetGravityScale(0);
        }
        internal PlatformerSnapshot RecoverySnapshot(PlatformerSnapshot checkpoint, UnityEngine.Vector2 spawn)
        {
            var policy = Configuration.Recovery;
            var position = policy.Position == PlayerRecoveryPositionMode.Checkpoint ? checkpoint.Position :
                policy.Position == PlayerRecoveryPositionMode.Spawn ? spawn : CustomPosition.ResolvePosition(checkpoint);
            float hp = Health == null ? 0 : policy.Health switch
            {
                PlayerRecoveryHealthMode.Full => Health.Maximum,
                PlayerRecoveryHealthMode.Fixed => UnityEngine.Mathf.Min(Health.Maximum, policy.FixedHealth),
                PlayerRecoveryHealthMode.Ratio => Health.Maximum * policy.HealthRatio,
                _ => checkpoint.Life.Health
            };
            if (Health != null && hp <= 0) throw new InvalidOperationException("死亡恢复策略必须产生大于0的HP；原死亡会话保留。");
            return checkpoint.WithRecovery(position, hp);
        }
        #endregion
    }
}
