using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    public enum PlayerCombatNodeRole { Attack, Recovery, Route, DashRecovery }

    /// <summary>仅声明节点用途；具体运行状态仍由 FSM 的正常工厂创建。</summary>
    public interface IPlayerCombatNodeDefinition
    {
        PlayerCombatNodeRole Role { get; }
    }

    [Serializable]
    public sealed class PlayerCombatNodePair
    {
        #region 编辑器面板参数
        [SerializeField] private string _segmentId, _attackNodeId, _recoveryNodeId;
        [SerializeField] private string _delayedRecoveryNodeId;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public string SegmentId => _segmentId;
        public string AttackNodeId => _attackNodeId;
        public string RecoveryNodeId => _recoveryNodeId;
        public string DelayedRecoveryNodeId => _delayedRecoveryNodeId;
        internal PlayerCombatNodePair(string segment, string attack, string recovery, string delayed = null)
        { _segmentId = segment; _attackNodeId = attack; _recoveryNodeId = recovery; _delayedRecoveryNodeId = delayed; }
        #endregion
    }

    /// <summary>方案拥有的节点身份清单，不存储连招目标；目标唯一来源为 Combo。</summary>
    [Serializable]
    public sealed class PlayerCombatGraphMap
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerCombatNodePair[] _nodes = Array.Empty<PlayerCombatNodePair>();
        [SerializeField] private string _dashRouteNodeId, _dashRecoveryNodeId;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public IReadOnlyList<PlayerCombatNodePair> Nodes => Array.AsReadOnly(_nodes);
        public string DashRouteNodeId => _dashRouteNodeId;
        public string DashRecoveryNodeId => _dashRecoveryNodeId;
        internal PlayerCombatGraphMap() { }
        internal PlayerCombatGraphMap(PlayerCombatNodePair[] nodes, string route = null, string recovery = null)
        { _nodes = nodes; _dashRouteNodeId = route; _dashRecoveryNodeId = recovery; }

        internal EmberFsmTargetBinding[] Bindings(PlayerCombatNodePair pair, PlayerCombo combo, PlayerCombatNodeRole role)
        {
            var connection = combo.Connections.FirstOrDefault(c => c.Source == pair.SegmentId);
            string Resolve(string id, string fallback) => string.IsNullOrEmpty(id) ? fallback :
                _nodes.First(n => n.SegmentId == id).AttackNodeId;
            var bindings = new[]
            {
                new EmberFsmTargetBinding("finish", role == PlayerCombatNodeRole.Attack ? pair.RecoveryNodeId : "ground"),
                new EmberFsmTargetBinding("normal", Resolve(connection?.Normal, "ground")),
                new EmberFsmTargetBinding("delayed", Resolve(connection?.Delayed, "ground")),
                new EmberFsmTargetBinding("ground", "ground"), new EmberFsmTargetBinding("fall", "fall"),
                new EmberFsmTargetBinding("jump", "jump"), new EmberFsmTargetBinding("ground-dash", "ground-dash"),
                new EmberFsmTargetBinding("air-dash", "air-dash"), new EmberFsmTargetBinding("air-jump", "air-jump"),
                new EmberFsmTargetBinding("restart-ground", Resolve(combo.GroundEntry, "ground")),
                new EmberFsmTargetBinding("restart-air", Resolve(combo.AirEntry, "fall"))
            };
            return role == PlayerCombatNodeRole.Attack && !string.IsNullOrEmpty(pair.DelayedRecoveryNodeId)
                ? bindings.Concat(new[] { new EmberFsmTargetBinding("finish-special", pair.DelayedRecoveryNodeId) }).ToArray() : bindings;
        }

        internal EmberFsmTargetBinding[] EntryBindings(PlayerCombo combo)
        {
            string Resolve(string id, string fallback) => string.IsNullOrEmpty(id) ? fallback : _nodes.First(n => n.SegmentId == id).AttackNodeId;
            var bindings = new[]
            {
                new EmberFsmTargetBinding("combat-ground", Resolve(combo.GroundEntry, "ground")),
                new EmberFsmTargetBinding("combat-air", Resolve(combo.AirEntry, "fall")),
                new EmberFsmTargetBinding("combat-dash-ground", "ground-dash"), new EmberFsmTargetBinding("combat-dash-air", "air-dash"),
                new EmberFsmTargetBinding("combat-jump", "jump"), new EmberFsmTargetBinding("combat-air-jump", "air-jump")
            };
            return string.IsNullOrEmpty(_dashRouteNodeId) ? bindings : bindings.Concat(new[] { new EmberFsmTargetBinding("combat-route", _dashRouteNodeId) }).ToArray();
        }
        internal EmberFsmTargetBinding[] RouteBindings() => _nodes.Select(n => new EmberFsmTargetBinding("attack:" + n.SegmentId, n.AttackNodeId)).ToArray();

        internal void Validate(EmberFsmGraph graph, PlayerAttackProfile attack, PlayerCombo combo)
        {
            if (_nodes == null) throw new InvalidOperationException("CombatGraph.Nodes：集合为空。");
            if (_nodes.Length == 0 && !attack.Enabled)
            {
                if (graph != null && graph.Nodes.Any(n => n.Definition is IPlayerCombatNodeDefinition))
                    throw new InvalidOperationException("CombatGraph：存在未登记归属的战斗节点，关闭攻击也不能跳过归属校验。");
                return;
            }
            if (graph == null || _nodes.Length != attack.Segments.Count)
                throw new InvalidOperationException("CombatGraph.Nodes：每个攻击段必须对应一组攻击/收刀节点。");
            var segments = new HashSet<string>(attack.Segments.Select(s => s.Id), StringComparer.Ordinal);
            var owned = new HashSet<string>(StringComparer.Ordinal);
            var nodes = graph.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
            if (nodes[graph.EntryId].Definition is PlayerCombatLocomotionStateDefinition)
                foreach (var binding in EntryBindings(combo))
                    if (!nodes[graph.EntryId].Bindings.Any(b => b.Slot == binding.Slot && b.TargetId == binding.TargetId))
                        throw new InvalidOperationException("CombatGraph.Entry：入口绑定与 Combo 不一致：" + binding.Slot);
            foreach (var pair in _nodes)
            {
                if (pair == null || !segments.Remove(pair.SegmentId)) throw new InvalidOperationException("CombatGraph.Nodes：攻击段为空、重复或不存在。");
                bool delayed = combo.Connections.Any(c => c.Source == pair.SegmentId && c.DelayedEnabled);
                if (delayed && string.IsNullOrEmpty(pair.DelayedRecoveryNodeId))
                    throw new InvalidOperationException("CombatGraph：特殊收刀归属必须与启用的停顿连接一致：" + pair.SegmentId);
                foreach (var role in new[] { PlayerCombatNodeRole.Attack, PlayerCombatNodeRole.Recovery })
                {
                    string id = role == PlayerCombatNodeRole.Attack ? pair.AttackNodeId : pair.RecoveryNodeId;
                    if (string.IsNullOrWhiteSpace(id) || !owned.Add(id) || !nodes.TryGetValue(id, out var node) ||
                        node.Definition is not IPlayerCombatNodeDefinition definition || definition.Role != role)
                        throw new InvalidOperationException("CombatGraph[" + pair.SegmentId + "]." + role + "：节点身份、归属或定义无效。");
                    var expected = Bindings(pair, combo, role).ToDictionary(b => b.Slot, b => b.TargetId, StringComparer.Ordinal);
                    if (node.Bindings.Count != expected.Count || node.Bindings.Any(b => !expected.TryGetValue(b.Slot, out var target) || target != b.TargetId))
                        throw new InvalidOperationException("CombatGraph[" + id + "]：图绑定与 Combo 不一致，请显式更新生成区。");
                }
                if (!string.IsNullOrEmpty(pair.DelayedRecoveryNodeId))
                {
                    string id = pair.DelayedRecoveryNodeId;
                    if (!owned.Add(id) || !nodes.TryGetValue(id, out var node) || node.Definition is not PlayerDelayedRecoveryStateDefinition)
                        throw new InvalidOperationException("CombatGraph：特殊收刀节点身份无效：" + id);
                    var expected = Bindings(pair, combo, PlayerCombatNodeRole.Recovery).ToDictionary(b => b.Slot, b => b.TargetId);
                    if (node.Bindings.Count != expected.Count || node.Bindings.Any(b => !expected.TryGetValue(b.Slot, out var target) || target != b.TargetId))
                        throw new InvalidOperationException("CombatGraph：特殊收刀绑定与 Combo 不一致：" + id);
                }
            }
            if (!string.IsNullOrEmpty(_dashRouteNodeId) || !string.IsNullOrEmpty(_dashRecoveryNodeId))
            {
                if (!owned.Add(_dashRouteNodeId) || !owned.Add(_dashRecoveryNodeId) ||
                    !nodes.TryGetValue(_dashRouteNodeId, out var route) || route.Definition is not PlayerDashAttackRouteDefinition ||
                    !nodes.TryGetValue(_dashRecoveryNodeId, out var tail) || tail.Definition is not PlayerDashRecoveryStateDefinition ||
                    nodes[graph.EntryId].Definition is not PlayerDashCombatLocomotionStateDefinition)
                    throw new InvalidOperationException("CombatGraph：冲刺路由、收尾和根声明归属不完整。");
                var expected = RouteBindings().ToDictionary(b => b.Slot, b => b.TargetId);
                if (route.Bindings.Count != expected.Count || route.Bindings.Any(b => !expected.TryGetValue(b.Slot, out var target) || target != b.TargetId))
                    throw new InvalidOperationException("CombatGraph：冲刺路由目标与攻击归属不一致。");
                foreach (string dashId in new[] { "ground-dash", "air-dash" })
                    if (nodes[dashId].Bindings.Any(b => (b.Slot == "ground" || b.Slot == "fall") && b.TargetId != _dashRecoveryNodeId))
                        throw new InvalidOperationException("CombatGraph：冲刺退出未接独立收尾。");
            }
            foreach (var node in graph.Nodes)
                if (node.Definition is IPlayerCombatNodeDefinition && !owned.Contains(node.Id))
                    throw new InvalidOperationException("CombatGraph：未登记归属的战斗节点：" + node.Id);
        }
        #endregion
    }
}
