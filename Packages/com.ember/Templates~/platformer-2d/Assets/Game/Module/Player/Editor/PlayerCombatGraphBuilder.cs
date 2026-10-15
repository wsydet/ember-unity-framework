using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player.Editor
{
    /// <summary>只构造候选；调用方须联合动画/方案预检后提交。不会编辑源图或删除用户节点。</summary>
    internal static class PlayerCombatGraphBuilder
    {
        #region 外部方法
        internal static EmberFsmGraph Prepare(EmberFsmGraph source, PlayerCombatGraphMap previous,
            PlayerAttackProfile attack, PlayerCombo combo, EmberFsmStateDefinition attackDefinition,
            EmberFsmStateDefinition recoveryDefinition, out PlayerCombatGraphMap map,
            PlayerCombatLocomotionStateDefinition locomotionDefinition = null,
            PlayerDelayedAttackStateDefinition delayedAttackDefinition = null, PlayerDelayedRecoveryStateDefinition delayedRecoveryDefinition = null,
            PlayerDashAttackRouteDefinition dashRouteDefinition = null, PlayerDashRecoveryStateDefinition dashRecoveryDefinition = null)
        {
            if (source == null || previous == null) throw new ArgumentException("战斗图候选缺少源图或归属清单。");
            attack.Validate(); combo.Validate(attack);
            if (attackDefinition is not IPlayerCombatNodeDefinition attackRole || attackRole.Role != PlayerCombatNodeRole.Attack ||
                recoveryDefinition is not IPlayerCombatNodeDefinition recoveryRole || recoveryRole.Role != PlayerCombatNodeRole.Recovery)
                throw new InvalidOperationException("战斗节点定义必须显式声明攻击/收刀用途。");
            var nodes = source.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
            var entries = previous.Nodes.ToDictionary(n => n.SegmentId, StringComparer.Ordinal);
            var owned = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pair in entries.Values)
            {
                if (!attack.Segments.Any(s => s.Id == pair.SegmentId))
                    throw new InvalidOperationException("删除攻击段必须走显式结构删除事务：" + pair.SegmentId);
                foreach (string id in new[] { pair.AttackNodeId, pair.RecoveryNodeId, pair.DelayedRecoveryNodeId }.Where(id => !string.IsNullOrEmpty(id)))
                    if (!owned.Add(id) || !nodes.TryGetValue(id, out var node) || node.Definition is not IPlayerCombatNodeDefinition)
                        throw new InvalidOperationException("拒绝覆盖未拥有的节点：" + id);
            }
            foreach (var segment in attack.Segments)
            {
                if (entries.ContainsKey(segment.Id)) continue;
                string attackId = "combat.attack." + segment.Id, recoveryId = "combat.recovery." + segment.Id;
                if (nodes.ContainsKey(attackId) || nodes.ContainsKey(recoveryId))
                    throw new InvalidOperationException("新战斗节点 ID 与用户节点冲突：" + segment.Id);
                entries.Add(segment.Id, new PlayerCombatNodePair(segment.Id, attackId, recoveryId));
            }
            foreach (var segment in attack.Segments)
            {
                var pair = entries[segment.Id];
                bool delayed = combo.Connections.Any(c => c.Source == segment.Id && c.DelayedEnabled);
                // 关闭玩法开关不等同于删除拓扑；保留原ID、归属和布局，运行选择仍检查开关。
                if (!delayed && string.IsNullOrEmpty(pair.DelayedRecoveryNodeId)) continue;
                if (delayedAttackDefinition == null || delayedRecoveryDefinition == null)
                    throw new InvalidOperationException("停顿连接需要显式提供特殊攻击/收刀声明。");
                if (string.IsNullOrEmpty(pair.DelayedRecoveryNodeId))
                {
                    string id = "combat.delayed-recovery." + segment.Id;
                    if (nodes.ContainsKey(id)) throw new InvalidOperationException("特殊收刀 ID 与用户节点冲突：" + id);
                    entries[segment.Id] = new PlayerCombatNodePair(pair.SegmentId, pair.AttackNodeId, pair.RecoveryNodeId, id);
                }
            }
            bool dash = dashRouteDefinition != null || dashRecoveryDefinition != null || !string.IsNullOrEmpty(previous.DashRouteNodeId);
            string routeId = null, tailId = null;
            if (dash)
            {
                if (dashRouteDefinition == null || dashRecoveryDefinition == null || locomotionDefinition is not PlayerDashCombatLocomotionStateDefinition)
                    throw new InvalidOperationException("冲刺集成必须同时提供路由、收尾和根声明。");
                routeId = string.IsNullOrEmpty(previous.DashRouteNodeId) ? "combat.dash-route" : previous.DashRouteNodeId;
                tailId = string.IsNullOrEmpty(previous.DashRecoveryNodeId) ? "combat.dash-recovery" : previous.DashRecoveryNodeId;
                foreach (string id in new[] { routeId, tailId })
                    if (nodes.ContainsKey(id))
                    {
                        if (id != previous.DashRouteNodeId && id != previous.DashRecoveryNodeId) throw new InvalidOperationException("冲刺节点与用户节点冲突：" + id);
                        owned.Add(id);
                    }
                if (!dashRouteDefinition.GetRedirectSlots().SequenceEqual(attack.Segments.Select(s => "attack:" + s.Id)))
                    throw new InvalidOperationException("路由声明必须事先配置为候选攻击目标，预检不修改传入定义。");
            }
            map = new PlayerCombatGraphMap(attack.Segments.Select(s => entries[s.Id]).ToArray(), routeId, tailId);
            var result = ScriptableObject.CreateInstance<EmberFsmGraph>();
            result.hideFlags = HideFlags.DontSave;
            try
            {
                var candidate = source.Nodes.Where(n => !owned.Contains(n.Id)).Select(n => new EmberFsmNode(n.Id, n.DisplayName,
                    n.Definition, n.ParentId, n.DefaultChildId, n.Bindings.Select(b => new EmberFsmTargetBinding(b.Slot, b.TargetId)).ToArray())).ToList();
                if (locomotionDefinition != null)
                {
                    int index = candidate.FindIndex(n => n.Id == source.EntryId);
                    var root = candidate[index];
                    if (root.Definition is not PlayerLocomotionStateDefinition && root.Definition is not PlayerCombatLocomotionStateDefinition &&
                        root.Definition is not PlayerLifeLocomotionStateDefinition)
                        throw new InvalidOperationException("战斗入口不能覆盖自定义根定义。");
                    if (root.Definition is PlayerLocomotionStateDefinition && root.Bindings.Any(b => b.Slot.StartsWith("combat-", StringComparison.Ordinal)))
                        throw new InvalidOperationException("战斗根绑定与现有用户绑定冲突。");
                    candidate[index] = new EmberFsmNode(root.Id, root.DisplayName, locomotionDefinition, root.ParentId, root.DefaultChildId,
                        root.Bindings.Where(b => !b.Slot.StartsWith("combat-", StringComparison.Ordinal)).Concat(map.EntryBindings(combo)).ToArray());
                }
                foreach (var pair in map.Nodes)
                {
                    foreach (var role in new[] { PlayerCombatNodeRole.Attack, PlayerCombatNodeRole.Recovery })
                    {
                        string id = role == PlayerCombatNodeRole.Attack ? pair.AttackNodeId : pair.RecoveryNodeId;
                        nodes.TryGetValue(id, out var old);
                        candidate.Add(new EmberFsmNode(id, old?.DisplayName ?? pair.SegmentId + " " + role,
                            role == PlayerCombatNodeRole.Attack ? !string.IsNullOrEmpty(pair.DelayedRecoveryNodeId) ? delayedAttackDefinition : attackDefinition : recoveryDefinition,
                            old?.ParentId ?? source.EntryId, null, map.Bindings(pair, combo, role)));
                    }
                    if (!string.IsNullOrEmpty(pair.DelayedRecoveryNodeId))
                    {
                        nodes.TryGetValue(pair.DelayedRecoveryNodeId, out var old);
                        candidate.Add(new EmberFsmNode(pair.DelayedRecoveryNodeId, old?.DisplayName ?? pair.SegmentId + " 特殊收刀",
                            delayedRecoveryDefinition, old?.ParentId ?? source.EntryId, null, map.Bindings(pair, combo, PlayerCombatNodeRole.Recovery)));
                    }
                }
                if (dash)
                {
                    candidate.Add(new EmberFsmNode(routeId, "冲刺攻击路由", dashRouteDefinition, source.EntryId, null, map.RouteBindings()));
                    candidate.Add(new EmberFsmNode(tailId, "冲刺收尾", dashRecoveryDefinition, source.EntryId, null,
                        new[] { new EmberFsmTargetBinding("ground", "ground"), new EmberFsmTargetBinding("fall", "fall") }));
                    foreach (string id in new[] { "ground-dash", "air-dash" })
                    {
                        int index = candidate.FindIndex(n => n.Id == id); var node = candidate[index];
                        if (node.Definition is not PlayerGroundDashStateDefinition && node.Definition is not PlayerAirDashStateDefinition)
                            throw new InvalidOperationException("冲刺接线不能覆盖自定义状态声明：" + id);
                        candidate[index] = new EmberFsmNode(node.Id, node.DisplayName, node.Definition, node.ParentId, node.DefaultChildId,
                            node.Bindings.Select(b => new EmberFsmTargetBinding(b.Slot, b.Slot is "ground" or "fall" ? tailId : b.TargetId)).ToArray());
                    }
                }
                result.Configure(source.EntryId, candidate.ToArray());
                map.Validate(result, attack, combo);
                if (!result.Inspect(typeof(PlayerContext)).IsValid) throw new InvalidOperationException("生成战斗节点后 FSM 声明校验失败。");
                return result;
            }
            catch { UnityEngine.Object.DestroyImmediate(result); throw; }
        }
        #endregion
    }
}
