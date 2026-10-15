using System;
using Ember.Core.Fsm;
using Game.Entity;
using Game.Input;
using UnityEngine;

namespace Game.Player
{
    /// <summary>未交接的玩家运行资源。准备不执行状态回调或绑定共享物理/表现，交接后由 Controller 独占释放。</summary>
    internal sealed class PlayerRuntimeCandidate : IDisposable
    {
        #region 内部参数
        internal PlayerController Controller { get; }
        internal object Owner { get; }
        internal EmberFsmGraph Graph { get; }
        internal EmberFsmObservationOrigin ObservationOrigin { get; }
#if UNITY_EDITOR
        internal PlayerSchemeObservation SchemeObservation { get; }
#endif
        internal PlayerContext Context { get; private set; }
        internal EmberFsm<PlayerContext> Fsm { get; private set; }
        internal PlayerAnimationSession Animation { get; private set; }
        internal PlatformerSnapshot Snapshot { get; }
        internal float GravityScale { get; }
        internal bool Restoring { get; }
        internal bool Transferred { get; set; }
        internal bool IsDisposed { get; private set; }
        private bool _released;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerRuntimeCandidate(PlayerController controller, object owner, PlayerConfig config,
            PlatformerSnapshot snapshot, GameInputService input, Entity2D entity, EntityMotor2D motor,
            EntitySensors2D sensors, EntityFacing2D facing, Vector2? initialPosition = null, Vector2? initialVelocity = null,
            PlayerTrialStart trial = null)
        {
            if (config.Scheme != null)
                throw new InvalidOperationException("完整方案必须先经 PlayerSchemeCapture 捕获，再准备运行候选。");
            Controller = controller; Graph = config.SourceGraph != null ? config.SourceGraph : config.Graph;
            ObservationOrigin = config.ObservationOrigin;
#if UNITY_EDITOR
            SchemeObservation = config.SchemeObservation;
#endif
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            var feel = trial?.Feel ?? config.CapturedFeel ?? config.Feel.Capture(Physics2D.gravity);
            GravityScale = feel.GravityScale;
            Restoring = snapshot != null;
            PlayerPowerSession powers = null;
            EntityEffectContainer effects = null;
            PlayerInputAdapter adapter = null;
            try
            {
                powers = snapshot != null ? snapshot.RestorePowers(config.Powers)
                    : config.Powers == null ? PlayerPowerSession.CreateEmpty() : PlayerPowerSession.CreateNewGame(config.Powers);
                bool bindDash = PlayerSchemeValidation.Validate(config, feel, powers);
                effects = snapshot != null ? snapshot.RestoreEffects()
                    : config.EnableEffects ? new EntityEffectContainer() : null;
                foreach (var node in config.Graph.Nodes)
                {
                    if (node.Definition is PlayerGroundDashStateDefinition groundEffect &&
                        powers.Skills.TryGet(PlayerGroundDashState.SKILL_ID, out var groundSkill) && groundSkill.IsEnabled)
                        groundEffect.ValidateEffects(effects);
                    if (node.Definition is PlayerAirDashStateDefinition airEffect &&
                        powers.Skills.TryGet(PlayerAirDashState.SKILL_ID, out var airSkill) && airSkill.IsEnabled)
                        airEffect.ValidateEffects(effects);
                }
                Snapshot = snapshot ?? new PlatformerSnapshot(trial?.Scene ?? config.Scene, trial?.Position ?? initialPosition ?? config.Spawn,
                    trial?.Velocity ?? initialVelocity ?? Vector2.zero, true, powers.Capture(), effects != null, Array.Empty<EntityTimedEffectSnapshot>(),
                    new PlatformerLifeRecord(config.CapturedCombat?.Life.Enabled == true,
                        config.CapturedCombat?.Life.Enabled == true ? config.CapturedCombat.Life.Initial : 0));
                if (Snapshot.Life.Installed != (config.CapturedCombat?.Life.Enabled == true))
                    throw new InvalidOperationException("快照生命装配与冻结方案不一致，请重新开始或显式迁移。");
                var jumpWindow = new PlayerJumpWindow(feel.CoyoteDuration);
                adapter = new PlayerInputAdapter(input.GetChannel(config.Channel), config.MoveAction,
                    config.JumpAction, feel.BufferDuration, bindDash ? config.DashAction : null,
                    feel.Attack.Enabled ? config.CapturedAttackAction ?? throw new InvalidOperationException("攻击输入未捕获。") : null, feel,
                    config.CapturedCombat?.Reactions.Enabled == true);
                Context = new PlayerContext(entity, motor, sensors, facing, adapter, jumpWindow, powers, effects, feel);
                if (config.CapturedCombat is { } combat && (combat.Life.Enabled || combat.Reactions.Enabled))
                {
                    var root = System.Linq.Enumerable.First(config.Graph.Nodes, n => n.Id == config.Graph.EntryId);
                    if (root.Definition is not IPlayerLifeRootDefinition)
                        throw new InvalidOperationException("生命/默认反应启用时必须显式接入生命根声明及受击/死亡节点。");
                    foreach (var slot in new[] { "life-hit", "life-death" })
                    {
                        var binding = System.Linq.Enumerable.FirstOrDefault(root.Bindings, b => b.Slot == slot);
                        if (binding == null || !System.Linq.Enumerable.Any(config.Graph.Nodes, n => n.Id == binding.TargetId &&
                            (slot == "life-hit" ? n.Definition is PlayerHitStateDefinition : n.Definition is PlayerDeathStateDefinition)))
                            throw new InvalidOperationException("生命根绑定缺失或类型错误：" + slot);
                    }
                    Context.Life = new PlayerLifeSession(Context, combat, Snapshot.Life.Installed ? Snapshot.Life.Health : null);
                    Context.HitReceiver = Context.Life.Receiver;
                }
                if (feel.Attack.Enabled)
                {
                    if (!System.Linq.Enumerable.Any(config.Graph.Nodes, n => n.Id == config.Graph.EntryId && n.Definition is PlayerCombatLocomotionStateDefinition))
                        throw new InvalidOperationException("战斗方案缺少独立战斗根声明。");
                    Context.Combat = new PlayerCombatMotion(Context, config.CapturedCombat);
                }
                var effectDescription = new System.Text.StringBuilder("实例冻结动作效果（不是静态编辑方案）");
                foreach (var node in config.Graph.Nodes)
                {
                    var ground = node.Definition as PlayerGroundDashStateDefinition;
                    var air = node.Definition as PlayerAirDashStateDefinition;
                    if (ground == null && air == null) continue;
                    var action = ground != null ? ground.ActionEffect : air.ActionEffect;
                    var tail = ground != null ? ground.TailEffect : air.TailEffect;
                    effectDescription.Append($"\n{node.Id} · Action {action?.Id ?? "无"} · Tail {tail?.Id ?? "无"} / {tail?.Duration ?? 0}s");
                }
                Context.FrozenActionEffects = effectDescription.ToString();
                Fsm = new EmberFsm<PlayerContext>(config.Graph, Context, rules: config.CapturedRules);
                var view = controller.GetComponentInChildren<PlayerAnimationView>(true);
                if (view != null || config.Animation != null)
                {
                    if (view == null || config.Animation == null)
                        throw new InvalidOperationException("玩家动画组件与方案配置必须同时装配。");
                    Animation = new PlayerAnimationSession(view, controller, config.Animation, config.Graph);
                }
            }
            catch
            {
                Animation?.Dispose();
                Fsm?.TryDispose();
                try { adapter?.Dispose(); }
                finally { try { effects?.Dispose(); } finally { powers?.Dispose(); } }
                throw;
            }
        }

        public void Dispose()
        {
            if (!Transferred) Release();
        }

        internal void Release()
        {
            IsDisposed = true;
            if (_released) return;
            // Controller 在状态回调返回后再次释放；生命周期回调不能重入 FSM 的事务。
            if (Fsm != null && Fsm.TryDispose() == EmberFsmResult.Reentrant) return;
            _released = true;
            Animation?.Dispose();
#if UNITY_EDITOR
            Context?.MotionTrace.Clear();
#endif
            try { Context?.HeadJump.Clear(); Context?.Rolling.End(); Context?.Landing.End(); Context?.Ledge.Dispose(); Context?.DropThrough.Dispose(); Context?.Input.Dispose(); }
            finally
            {
                try { Context?.Combat?.Hits?.Dispose(); Context?.Effects?.Dispose(); }
                finally { Context?.Powers.Dispose(); }
            }
        }
        #endregion
    }
}
