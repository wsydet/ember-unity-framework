using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Player
{
    /// <summary>当前玩家场景的命中资源，由 PlayerModule 唯一物理步驱动。原位换玩家不重置世界。</summary>
    internal sealed class PlayerHitScene : IDisposable
    {
        #region 内部参数
        internal sealed class Actor : IEntityReactionSink, IDisposable
        {
            internal EntityHitActor Source { get; }
            internal EntityHealth Health { get; }
            internal EntityHitBatch.Target Target { get; private set; }
            internal EntityHit? PendingReaction { get; private set; }
            internal EntityReactionMotion Reaction { get; } = new();
            internal EntityEffectContainer Effects { get; private set; } = new();
            private EntityEffectBinding _binding;
            private readonly EntityHitBatch _batch;
            private readonly EntityMotor2D _motor;
            private readonly Vector2 _spawn;
            private readonly float _gravity;
            private readonly string _id;
            private readonly int _team;
            internal Actor(EntityHitBatch batch, EntityHitActor source)
            {
                Source = source; _batch = batch; _id = source.StableId; _team = source.Team;
                _motor = source.GetComponent<EntityMotor2D>();
                if (_motor == null || _motor.Body.bodyType != RigidbodyType2D.Dynamic)
                    throw new InvalidOperationException("沙袋需要 Dynamic EntityMotor2D。");
                _spawn = _motor.Position; _gravity = _motor.Body.gravityScale;
                Health = source.HasHealth ? new EntityHealth(source.Maximum, source.Maximum) : null;
                Register(); BindEffects(); _motor.Resume();
            }
            private void BindEffects()
            {
                var targets = Source.GetComponent<EntityEffectTargets>(); var entity = Source.GetComponent<Entity2D>();
                _binding = targets != null ? targets.Bind(Effects) : new EntityEffectBinding(Effects,
                    Source.GetComponentsInChildren<Collider2D>().Where(c => entity.Owns(c) && !c.isTrigger).ToArray(), Source.GetComponents<SpriteRenderer>());
            }
            private void Register() => Target = _batch.Register(Source.GetComponent<Entity2D>(), _id, _team,
                new EntityHitReceiver(Health, Effects, this, nonLethal: true));
            public bool Request(in EntityHit hit)
            { if (!Reaction.Request(hit)) return false; PendingReaction = hit; return true; }
            internal void Step(float dt)
            {
                Effects.Tick(dt);
                _motor.BeginStep(); _motor.SetGravityScale(_gravity);
                if (Reaction.HasPending) { Reaction.Begin(_motor); PendingReaction = null; }
                else if (Reaction.IsActive)
                { Reaction.Advance(_motor, dt); if (Reaction.Remaining <= 0) Reaction.End(); }
                else _motor.SetHorizontalVelocity(0);
                _motor.ExecuteStep();
            }
            internal void Reset()
            {
                Target.Dispose(); Reaction.Clear(); PendingReaction = null; Health?.Restore(Health.Maximum);
                _binding?.Dispose(); Effects.Dispose(); Effects = new EntityEffectContainer(); BindEffects();
                _motor.Freeze(); _motor.Apply(_spawn, Vector2.zero, _gravity); Register(); _motor.Resume();
            }
            public void Dispose() { Target?.Dispose(); PendingReaction = null; Reaction.Clear(); _binding?.Dispose(); Effects.Dispose(); if (_motor != null) _motor.Freeze(); }
        }
        private sealed class Trap : IDisposable
        {
            internal readonly EntityContactTrap Source;
            private readonly EntityHitBatch _batch;
            private readonly EntityHitBatch.Target _target;
            private readonly Collider2D _shape;
            private readonly ContactFilter2D _filter;
            private readonly EntityHit _right, _left;
            private readonly float _interval;
            private readonly List<Collider2D> _overlaps = new();
            private readonly Dictionary<EntityHitBatch.Target, double> _next = new();
            private readonly List<EntityHitBatch.Target> _expired = new();
            private double _time;
            private ulong _step;
            internal Trap(EntityHitBatch batch, EntityContactTrap source)
            {
                Source = source; _batch = batch; _shape = source.Shape; _interval = source.Interval;
                if (_shape == null || !_shape.isTrigger || !source.GetComponent<Entity2D>().Owns(_shape) || !float.IsFinite(_interval) || _interval <= 0)
                    throw new InvalidOperationException("陷阱需要所属Trigger形状及有限正间隔。");
                _right = new EntityHit(source.Payload, 1); _left = new EntityHit(source.Payload, -1);
                _filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = source.Layers };
                _target = batch.Register(source.GetComponent<Entity2D>(), source.StableId, source.Team, new EntityHitReceiver(null, null, null));
            }
            internal void Collect(float dt)
            {
                _time += dt; _step++;
                _expired.Clear(); foreach (var target in _next.Keys) if (!target.IsValid) _expired.Add(target);
                foreach (var target in _expired) _next.Remove(target);
                if (!_target.IsValid || _shape == null || !_shape.enabled) return;
                _overlaps.Clear(); _shape.Overlap(_filter, _overlaps);
                foreach (var collider in _overlaps)
                {
                    if (!_batch.TryGet(Entity2D.Resolve(collider), out var target) || target == _target || target.Team == _target.Team ||
                        _next.TryGetValue(target, out var deadline) && _time < deadline) continue;
                    _next[target] = _time + _interval;
                    _batch.Collect(_target, _step, "contact", target, target.Entity.transform.position.x >= Source.transform.position.x ? _right : _left);
                }
            }
            internal void Reset() { _next.Clear(); _time = 0; }
            public void Dispose() { _target.Dispose(); _next.Clear(); }
        }
        private readonly EntityHitBatch _batch = new();
        private readonly List<Actor> _actors = new();
        private readonly List<Trap> _traps = new();
        private EntityHitBatch.Target _player;
        private PlayerContext _context;
        private bool _disposed;
        internal IReadOnlyList<Actor> Actors => _actors;
        internal EntityHitBatch Batch => _batch;
        #endregion
        #region 外部方法
        internal PlayerHitScene(Scene scene, PlayerContext context)
        {
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var actor in root.GetComponentsInChildren<EntityHitActor>(false))
                        if (actor.isActiveAndEnabled) _actors.Add(new Actor(_batch, actor));
                    foreach (var trap in root.GetComponentsInChildren<EntityContactTrap>(false))
                        if (trap.isActiveAndEnabled) _traps.Add(new Trap(_batch, trap));
                }
                Bind(context);
            }
            catch { Dispose(); throw; }
        }
        internal void Bind(PlayerContext context)
        {
            if (_disposed || context == null) throw new InvalidOperationException("命中场景或玩家失效。");
            _batch.Clear();
            _context?.Combat?.Hits?.Dispose();
            if (_context?.Combat != null) _context.Combat.Hits = null;
            _player?.Dispose(); _context = context;
            _player = _batch.Register(context.Entity, "player", 1, context.HitReceiver ?? new EntityHitReceiver(null, context.Effects, null));
            if (context.Combat != null) context.Combat.Hits = new PlayerAttackHitSession(_batch, _player);
        }
        internal void Step(Action collectPlayer, float dt = .02f)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PlayerHitScene));
            try
            {
                foreach (var actor in _actors)
                    if (actor.Source == null || !actor.Source.isActiveAndEnabled) actor.Dispose();
                    else actor.Step(dt);
                collectPlayer();
                foreach (var trap in _traps)
                    if (trap.Source == null || !trap.Source.isActiveAndEnabled) trap.Dispose(); else trap.Collect(dt);
                // 所有查询在此处完成后才结算；受击请求由后续统一动作提交处理。
                _batch.Resolve();
            }
            catch { _batch.Clear(); throw; }
        }
        internal void ResetActors()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PlayerHitScene));
            _batch.Clear();
            foreach (var actor in _actors) if (actor.Source != null && actor.Source.isActiveAndEnabled) actor.Reset();
            foreach (var trap in _traps) trap.Reset();
        }
        public void Dispose()
        {
            if (_disposed) return; _disposed = true;
            _context?.Combat?.Hits?.Dispose();
            if (_context?.Combat != null) _context.Combat.Hits = null;
            foreach (var actor in _actors) actor.Dispose();
            foreach (var trap in _traps) trap.Dispose(); _traps.Clear();
            _actors.Clear(); _batch.Dispose(); _context = null;
        }
        #endregion
    }
}
