using System;
using System.Collections.Generic;

namespace Game.Entity
{
    /// <summary>会话拥有的结算批次。显式稳定键排序，来源死亡不撤销已收集候选，目标替换令旧句柄失效。</summary>
    public sealed class EntityHitBatch : IDisposable
    {
        public sealed class Target : IDisposable
        {
            internal readonly EntityHitBatch Owner;
            public Entity2D Entity { get; }
            public string Id { get; }
            public int Team { get; }
            public IEntityHitReceiver Receiver { get; }
            public bool IsValid { get; private set; } = true;
            internal Target(EntityHitBatch owner, Entity2D entity, string id, int team, IEntityHitReceiver receiver)
            { Owner = owner; Entity = entity; Id = id; Team = team; Receiver = receiver; }
            public void Dispose() { if (!IsValid) return; IsValid = false; Owner.Remove(this); }
        }
        private readonly struct Candidate
        {
            internal readonly string SourceId, Window;
            internal readonly ulong Attack;
            internal readonly Target Target;
            internal readonly EntityHit Hit;
            internal readonly Action<EntityHitResult> Complete;
            internal Candidate(string source, ulong attack, string window, Target target, EntityHit hit, Action<EntityHitResult> complete)
            { SourceId = source; Attack = attack; Window = window; Target = target; Hit = hit; Complete = complete; }
        }
        private readonly Dictionary<Entity2D, Target> _targets = new();
        private readonly HashSet<string> _ids = new(StringComparer.Ordinal);
        private readonly List<Candidate> _pending = new();
        private readonly HashSet<(string, ulong, string, Target)> _thisStep = new();
        private bool _resolving, _disposed;
        public Target Register(Entity2D entity, string stableId, int team, IEntityHitReceiver receiver)
        {
            if (_disposed || _resolving || entity == null || receiver == null || string.IsNullOrWhiteSpace(stableId) ||
                _targets.ContainsKey(entity) || !_ids.Add(stableId)) throw new InvalidOperationException("命中目标注册无效或稳定 ID 重复。");
            var target = new Target(this, entity, stableId, team, receiver); _targets.Add(entity, target); return target;
        }
        private void Remove(Target target) { _targets.Remove(target.Entity); _ids.Remove(target.Id); }
        public bool TryGet(Entity2D entity, out Target target)
        { target = null; return entity != null && _targets.TryGetValue(entity, out target) && target.IsValid; }
        public bool Collect(Target source, ulong attack, string window, Target target, in EntityHit hit, Action<EntityHitResult> complete = null)
        {
            if (_disposed || _resolving) throw new InvalidOperationException("不能在已释放或结算中的批次收集。");
            if (source == null || target == null || source.Owner != this || target.Owner != this || !source.IsValid || !target.IsValid ||
                source == target || attack == 0 || string.IsNullOrWhiteSpace(window)) return false;
            if (!_thisStep.Add((source.Id, attack, window, target))) return false;
            _pending.Add(new Candidate(source.Id, attack, window, target, hit, complete)); return true;
        }
        public void Resolve()
        {
            if (_disposed || _resolving) throw new InvalidOperationException("命中批次不能重入或在释放后结算。");
            _resolving = true;
            try
            {
                _pending.Sort((a,b) =>
                {
                    int order = string.CompareOrdinal(a.SourceId, b.SourceId);
                    if (order == 0) order = a.Attack.CompareTo(b.Attack);
                    if (order == 0) order = string.CompareOrdinal(a.Window, b.Window);
                    if (order == 0) order = string.CompareOrdinal(a.Target.Id, b.Target.Id);
                    return order;
                });
                foreach (var candidate in _pending)
                {
                    if (!candidate.Target.IsValid || candidate.Target.Entity == null) continue;
                    var result = candidate.Target.Receiver.Receive(candidate.Hit);
                    candidate.Complete?.Invoke(result);
                }
            }
            finally { _pending.Clear(); _thisStep.Clear(); _resolving = false; }
        }
        public void Clear() { if (_resolving) throw new InvalidOperationException("结算中不能清空批次。"); _pending.Clear(); _thisStep.Clear(); }
        public void Dispose()
        {
            if (_disposed) return; Clear(); _disposed = true;
            foreach (var target in new List<Target>(_targets.Values)) target.Dispose();
        }
    }
}
