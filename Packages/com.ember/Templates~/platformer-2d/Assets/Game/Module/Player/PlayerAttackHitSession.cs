using System;
using System.Collections.Generic;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    /// <summary>每攻击实例/窗口的目标账本。只收集，不在查询过程中结算伤害。</summary>
    internal sealed class PlayerAttackHitSession : IDisposable
    {
        private sealed class Window
        {
            internal readonly PlayerHitWindow Config;
            internal readonly HashSet<EntityHitBatch.Target> Recorded = new();
            internal bool Sampled;
            internal Window(PlayerHitWindow config) => Config = config;
        }
        private readonly EntityHitBatch _batch;
        private readonly EntityHitBatch.Target _source;
        private readonly PlayerHitQuery _query;
        private readonly List<Window> _windows = new();
        private Vector2 _previousPosition;
        private ulong _attack;
        private int _direction;
        private EntityHitResult? _lastResult;
        internal string Observation
        {
            get
            {
                var text = new System.Text.StringBuilder($"攻击实例 {_attack} · 方向 {_direction}");
                foreach (var window in _windows)
                    text.Append($"\n窗口 {window.Config.Id} [{window.Config.Start:F3}, {window.Config.End:F3}) · 关键帧 {window.Config.Keyframes.Count} · 已采样 {window.Sampled} · 账本 {window.Recorded.Count}");
                if (_lastResult.HasValue)
                {
                    var result = _lastResult.Value;
                    text.Append($"\n最近结算 {result.Outcome} · 伤害 {result.Damage} · 反应 {result.ReactionAccepted} · {result.Rejection}");
                }
                return text.ToString();
            }
        }
        internal PlayerAttackHitSession(EntityHitBatch batch, EntityHitBatch.Target source)
        {
            _batch = batch; _source = source;
            _query = new PlayerHitQuery(source.Entity.gameObject.scene);
        }
        internal void Begin(PlayerAttackSegment segment, int direction, Vector2 position)
        {
            _attack++; _direction = direction; _previousPosition = position; _windows.Clear(); _lastResult = null;
            foreach (var window in segment.Windows) _windows.Add(new Window(window));
        }
        internal void Sample(double previous, double current, Vector2 position)
        {
            if (current < previous) throw new ArgumentOutOfRangeException(nameof(current));
            if (!_source.IsValid || _source.Receiver.IsDead) return;
            foreach (var window in _windows)
            {
                var config = window.Config;
                if (config.SingleSample ? window.Sampled || previous > config.Start || current < config.Start : previous >= config.End || current < config.Start) continue;
                window.Sampled = true;
                double start = Math.Max(previous, config.Start), end = config.SingleSample ? config.Start : Math.Min(current, config.End);
                for (int i = 0; i < config.Keyframes.Count; i++)
                {
                    var frame = config.Keyframes[i];
                    double frameEnd = i + 1 < config.Keyframes.Count ? config.Keyframes[i + 1].Time : config.End;
                    double left = Math.Max(start, frame.Time), right = Math.Min(end, frameEnd);
                    if (left > right || !config.SingleSample && left >= frameEnd) continue;
                    Vector2 Pose(double time) => Vector2.Lerp(_previousPosition, position,
                        current == previous ? 1 : (float)((time - previous) / (current - previous)));
                    foreach (var shape in frame.Shapes)
                        _query.Query(shape, _direction, Pose(left), Pose(right), config.Layers, collider =>
                        {
                            if (!_batch.TryGet(Entity2D.Resolve(collider), out var target) || target == _source ||
                                !config.AllowFriendly && target.Team == _source.Team || window.Recorded.Contains(target)) return;
                            var hit = new EntityHit(config.Payload, _direction);
                            if (_batch.Collect(_source, _attack, config.Id, target, hit, result =>
                                { _lastResult = result; if (result.Accepted) window.Recorded.Add(target); }) && config.Retry == PlayerHitRetry.RecordAttempt)
                                window.Recorded.Add(target);
                        });
                }
            }
            _previousPosition = position;
        }
        public void Dispose() { _windows.Clear(); _query.Dispose(); }
    }
}
