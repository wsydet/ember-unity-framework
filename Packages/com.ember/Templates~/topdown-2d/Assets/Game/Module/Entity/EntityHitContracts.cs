using System;
using UnityEngine;

namespace Game.Entity
{
    public enum EntityHitOutcome { Rejected, Damage, Reaction, DamageAndReaction, Lethal }
    /// <summary>收集时冻结的单击值；不包含来源角色运行状态。</summary>
    public readonly struct EntityHit
    {
        public string Category { get; }
        public float Damage { get; }
        public Vector2 Velocity { get; }
        public float Braking { get; }
        public float StunSeconds { get; }
        public int Priority { get; }
        public EntityHit(EntityHitPayload payload, Vector2 direction)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload)); payload.Validate();
            Category = payload.Category; Damage = payload.Damage;
            if(!float.IsFinite(direction.x)||!float.IsFinite(direction.y)||direction.sqrMagnitude<.000001f)throw new ArgumentException("命中需要有效平面方向。");
            Velocity = EntityPlaneFrame.Transform(payload.KnockbackVelocity,direction);
            Braking = payload.Braking; StunSeconds = payload.StunSeconds; Priority = payload.Priority;
        }
        public EntityHit(string category,float damage,Vector2 velocity,float braking,float stunSeconds,int priority=0)
        {
            if(string.IsNullOrWhiteSpace(category)||!float.IsFinite(damage)||damage<0||!float.IsFinite(velocity.x)||
                !float.IsFinite(velocity.y)||!float.IsFinite(braking)||braking<0||!float.IsFinite(stunSeconds)||stunSeconds<0)
                throw new ArgumentException("平面命中值无效。");
            Category=category;Damage=damage;Velocity=velocity;Braking=braking;StunSeconds=stunSeconds;Priority=priority;
        }
    }
    public readonly struct EntityHitResult
    {
        public EntityHitOutcome Outcome { get; }
        public float Damage { get; }
        public bool ReactionAccepted { get; }
        public string Rejection { get; }
        public bool Accepted => Outcome != EntityHitOutcome.Rejected;
        public EntityHitResult(EntityHitOutcome outcome, float damage = 0, bool reaction = false, string rejection = null)
        { Outcome = outcome; Damage = damage; ReactionAccepted = reaction; Rejection = rejection; }
    }
    public interface IEntityHitReceiver
    {
        bool IsDead { get; }
        EntityHitResult Receive(in EntityHit hit);
    }
    /// <summary>只登记反应请求；接收器调用期间不得重入 FSM。宿主在统一提交阶段处理。</summary>
    public interface IEntityReactionSink { bool Request(in EntityHit hit); }
    /// <summary>所有生命、硬直及新保护写入前的单次规则；null 明确拒绝。</summary>
    public interface IEntityHitRule { EntityHit? Filter(in EntityHit incoming); }

    public sealed class EntityHitReceiver : IEntityHitReceiver
    {
        private readonly EntityHealth _health;
        private readonly EntityEffectContainer _effects;
        private readonly IEntityReactionSink _reactions;
        private readonly float _multiplier, _reduction, _fixedDamage;
        private readonly bool _fixed, _nonLethal;
        private readonly EntityEffectSpec _protection;
        private readonly IEntityHitRule _rule;
        private readonly Func<bool> _isValid;
        public bool IsDead => _health != null && _health.Current <= 0 && !_nonLethal;
        public event Action<EntityHitResult> Resolved;
        public EntityHitReceiver(EntityHealth health, EntityEffectContainer effects, IEntityReactionSink reactions,
            float multiplier = 1, float reduction = 0, bool fixedDamage = false, float fixedValue = 1,
            bool nonLethal = false, EntityEffectSpec protection = null,IEntityHitRule rule=null,Func<bool> isValid=null)
        {
            if (!float.IsFinite(multiplier) || multiplier < 0 || !float.IsFinite(reduction) || reduction < 0 ||
                !float.IsFinite(fixedValue) || fixedValue < 0) throw new ArgumentOutOfRangeException(nameof(multiplier));
            if (protection != null && (effects == null || protection.Lifetime != EntityEffectLifetime.Timed))
                throw new ArgumentException("受击保护需要效果容器及独立 Timed 生命周期。");
            _health = health; _effects = effects; _reactions = reactions; _multiplier = multiplier;
            _reduction = reduction; _fixed = fixedDamage; _fixedDamage = fixedValue; _nonLethal = nonLethal; _protection = protection;_rule=rule;_isValid=isValid;
        }
        public EntityHitResult Receive(in EntityHit incoming)
        {
            if(_isValid?.Invoke()==false)return new EntityHitResult(EntityHitOutcome.Rejected,rejection:"inactive");
            if (IsDead) return new EntityHitResult(EntityHitOutcome.Rejected, rejection: "dead");
            var hit=incoming;
            if(_rule!=null)
            {
                var filtered=_rule.Filter(incoming);if(!filtered.HasValue)return new EntityHitResult(EntityHitOutcome.Rejected,rejection:"project-rule");
                var value=filtered.Value;hit=new EntityHit(value.Category,value.Damage,value.Velocity,value.Braking,value.StunSeconds,value.Priority);
            }
            // 本击许可在任何血量变化、新保护之前一次性确定。
            bool damageAllowed = _effects?.ProtectsDamage(hit.Category) != true;
            bool reactionAllowed = _effects?.PreventsStagger != true;
            float damage = damageAllowed && _health != null
                ? _health.Damage(_fixed ? _fixedDamage : Math.Max(0, hit.Damage * _multiplier - _reduction), _nonLethal) : 0;
            bool dead = IsDead;
            bool reaction = !dead && reactionAllowed && _reactions != null &&
                (hit.StunSeconds > 0 || hit.Velocity != Vector2.zero) && _reactions.Request(hit);
            if (damage > 0 && !dead && _protection != null) _effects.ApplyCaptured(_protection, "damage-protection");
            var outcome = dead ? EntityHitOutcome.Lethal : damage > 0 ? reaction ? EntityHitOutcome.DamageAndReaction : EntityHitOutcome.Damage :
                reaction ? EntityHitOutcome.Reaction : EntityHitOutcome.Rejected;
            var result = new EntityHitResult(outcome, damage, reaction, outcome == EntityHitOutcome.Rejected ? "protected-or-no-effect" : null);
            if (Resolved != null)
                foreach (Action<EntityHitResult> handler in Resolved.GetInvocationList())
                    try { handler(result); }
                    catch (Exception error) { Ember.Basic.EmberDebug.LogError("Game.Entity", "命中观察者失败：" + error); }
            return result;
        }
    }
}
