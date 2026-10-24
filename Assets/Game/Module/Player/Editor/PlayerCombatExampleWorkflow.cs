using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entity;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    /// <summary>P3 三连击配置入口。数值为可编辑示例；Clip 由调用方显式选择，伤害与美术在后续阶段完善。</summary>
    public static class PlayerCombatExampleWorkflow
    {
        /// <summary>完整候选提交入口；新增/连接攻击共用状态实现，保留生命与冲刺装配，不自动保存。</summary>
        public static void ApplyConfiguration(PlayerScheme scheme, PlayerAttackProfile attack, PlayerCombo combo,
            IReadOnlyDictionary<string, AnimationClip> newClips)
        {
            PlayerEditorAccess.Require(); PlayerSchemeWorkflow.Validate(scheme);
            PlayerSchemeOwnership.RequireWritable(scheme); PlayerSchemeOwnership.RequireWritable(scheme.Feel);
            bool parametersOnly = newClips == null || newClips.Count == 0;
            if (parametersOnly)
                try { scheme.CombatGraph.Validate(scheme.Graph, attack, combo); }
                catch (InvalidOperationException) { parametersOnly = false; }
            if (parametersOnly)
            {
                var candidate = UnityEngine.Object.Instantiate(scheme);
                var feel = UnityEngine.Object.Instantiate(scheme.Feel);
                try
                {
                    using (var data = new SerializedObject(candidate))
                    { data.FindProperty("_feel").objectReferenceValue = feel; data.ApplyModifiedPropertiesWithoutUndo(); }
                    feel.SetAttackConfiguration(attack); candidate.SetCombatConfiguration(combo, scheme.CombatGraph);
                    PlayerSchemeWorkflow.Validate(candidate);
                    Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
                    Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { scheme, scheme.Feel }, "应用攻击参数");
                    scheme.Feel.SetAttackConfiguration(attack); scheme.SetCombatConfiguration(combo, scheme.CombatGraph);
                    EditorUtility.SetDirty(scheme.Feel); EditorUtility.SetDirty(scheme);
                    Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
                }
                finally { UnityEngine.Object.DestroyImmediate(candidate); UnityEngine.Object.DestroyImmediate(feel); }
                return;
            }
            bool life = scheme.Graph.Nodes.First(n => n.Id == scheme.Graph.EntryId).Definition is IPlayerLifeRootDefinition;
            bool dash = !string.IsNullOrEmpty(scheme.CombatGraph.DashRouteNodeId);
            var body = ScriptableObject.CreateInstance<PlayerAttackStateDefinition>();
            var recovery = ScriptableObject.CreateInstance<PlayerAttackRecoveryStateDefinition>();
            PlayerCombatLocomotionStateDefinition root = life
                ? dash ? ScriptableObject.CreateInstance<PlayerLifeDashLocomotionStateDefinition>() : ScriptableObject.CreateInstance<PlayerLifeCombatLocomotionStateDefinition>()
                : dash ? ScriptableObject.CreateInstance<PlayerDashCombatLocomotionStateDefinition>() : ScriptableObject.CreateInstance<PlayerCombatLocomotionStateDefinition>();
            var delayedBody = ScriptableObject.CreateInstance<PlayerDelayedAttackStateDefinition>();
            var delayedRecovery = ScriptableObject.CreateInstance<PlayerDelayedRecoveryStateDefinition>();
            var route = dash ? ScriptableObject.CreateInstance<PlayerDashAttackRouteDefinition>() : null;
            var tail = dash ? ScriptableObject.CreateInstance<PlayerDashRecoveryStateDefinition>() : null;
            try
            {
                route?.Configure(attack.Segments.Select(s => s.Id));
                PlayerCombatStructureTransaction.Apply(scheme, attack, combo, body, recovery,
                    newClips ?? new Dictionary<string, AnimationClip>(), root, delayedBody, delayedRecovery, route, tail);
            }
            finally
            {
                foreach (var item in new UnityEngine.Object[] { body, recovery, root, delayedBody, delayedRecovery, route, tail })
                    if (item != null) UnityEngine.Object.DestroyImmediate(item);
            }
        }
        public static void InstallLife(PlayerScheme scheme, PlayerLifeConfiguration life, PlayerReactionConfiguration reactions,
            AnimationClip hitClip = null, AnimationClip deathClip = null)
            => PlayerCombatStructureTransaction.ApplyLife(scheme, life, reactions, hitClip, deathClip);
        public static void InstallThreeHit(PlayerScheme scheme, AnimationClip first, AnimationClip second, AnimationClip third)
        {
            PlayerEditorAccess.Require(); PlayerSchemeWorkflow.Validate(scheme);
            PlayerSchemeOwnership.RequireWritable(scheme);
            if (scheme.Feel.Attack.Segments.Count != 0 || scheme.Powers.AbilitySlots.Any(a => a.Definition.Id == "basic-attack"))
                throw new InvalidOperationException("目标已含攻击配置；示例安装不覆盖已有招式。");
            if (first == null || second == null || third == null) throw new ArgumentException("三段攻击必须显式指定 Clip。");
            var attack = JsonUtility.FromJson<PlayerAttackProfile>("{\"_enabled\":true,\"_segments\":[" +
                Segment("first", .45f, .28f) + "," + Segment("second", .5f, .3f) + "," + Segment("third", .6f, .4f) +
                "],\"_recoveries\":[{\"_id\":\"ordinary\",\"_normalDuration\":0.3,\"_delayedNormalDuration\":0.3,\"_delayedTotalDuration\":1.3}]," +
                "\"_dash\":{\"_mode\":1,\"_opensAt\":0,\"_duration\":0.2,\"_airVelocityRatio\":1,\"_airBraking\":30,\"_airTurning\":50}}");
            var combo = JsonUtility.FromJson<PlayerCombo>("{\"_groundEntry\":\"first\",\"_connections\":[{\"_source\":\"first\",\"_normal\":\"second\"},{\"_source\":\"second\",\"_normal\":\"third\"}]}");
            var body = ScriptableObject.CreateInstance<PlayerAttackStateDefinition>();
            var recovery = ScriptableObject.CreateInstance<PlayerAttackRecoveryStateDefinition>();
            var root = ScriptableObject.CreateInstance<PlayerCombatLocomotionStateDefinition>();
            var ability = ScriptableObject.CreateInstance<PlayerAbilityDefinition>(); ability.name = "BasicAttackAbility";
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                using (var data = new SerializedObject(ability))
                {
                    data.FindProperty("_id").stringValue = "basic-attack"; data.FindProperty("_defaultOpen").boolValue = true;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                AssetDatabase.AddObjectToAsset(ability, scheme); Undo.RegisterCreatedObjectUndo(ability, "新增基础攻击能力");
                Undo.RegisterCompleteObjectUndo(scheme.Powers, "装配基础攻击能力");
                using (var data = new SerializedObject(scheme.Powers))
                {
                    var slots = data.FindProperty("_abilities"); var slot = slots.GetArrayElementAtIndex(slots.arraySize++);
                    slot.FindPropertyRelative("_definition").objectReferenceValue = ability;
                    slot.FindPropertyRelative("_enabled").boolValue = true; data.ApplyModifiedPropertiesWithoutUndo();
                }
                PlayerCombatStructureTransaction.Apply(scheme, attack, combo, body, recovery,
                    new Dictionary<string, AnimationClip> { ["combat.attack.first"] = first, ["combat.attack.second"] = second, ["combat.attack.third"] = third }, root);
                Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                if (ability != null) { if (AssetDatabase.Contains(ability)) AssetDatabase.RemoveObjectFromAsset(ability); UnityEngine.Object.DestroyImmediate(ability); }
                throw;
            }
            finally { UnityEngine.Object.DestroyImmediate(body); UnityEngine.Object.DestroyImmediate(recovery); UnityEngine.Object.DestroyImmediate(root); }
        }
        /// <summary>为已有独立战斗方案接入冲刺路由和收尾；不改招式参数、不自动保存。</summary>
        public static void EnableDashFollowup(PlayerScheme scheme)
        {
            PlayerEditorAccess.Require(); PlayerSchemeWorkflow.Validate(scheme);
            if (!scheme.Feel.Attack.Enabled || !string.IsNullOrEmpty(scheme.CombatGraph.DashRouteNodeId))
                throw new InvalidOperationException("需要尚未接入冲刺出招的有效独立战斗方案。");
            var body = ScriptableObject.CreateInstance<PlayerAttackStateDefinition>();
            var recovery = ScriptableObject.CreateInstance<PlayerAttackRecoveryStateDefinition>();
            var root = ScriptableObject.CreateInstance<PlayerDashCombatLocomotionStateDefinition>();
            var route = ScriptableObject.CreateInstance<PlayerDashAttackRouteDefinition>();
            var tail = ScriptableObject.CreateInstance<PlayerDashRecoveryStateDefinition>();
            var delayedBody = ScriptableObject.CreateInstance<PlayerDelayedAttackStateDefinition>();
            var delayedRecovery = ScriptableObject.CreateInstance<PlayerDelayedRecoveryStateDefinition>();
            try
            {
                route.Configure(scheme.Feel.Attack.Segments.Select(s => s.Id));
                PlayerCombatStructureTransaction.Apply(scheme, PlayerCombatData.Copy(scheme.Feel.Attack), PlayerCombatData.Copy(scheme.Combo),
                    body, recovery, new Dictionary<string, AnimationClip>(), root, delayedBody, delayedRecovery, route, tail);
            }
            finally
            {
                foreach (var definition in new UnityEngine.Object[] { body, recovery, root, route, tail, delayedBody, delayedRecovery })
                    UnityEngine.Object.DestroyImmediate(definition);
            }
        }
        private static string Segment(string id, float duration, float next)
        {
            string Number(float value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return "{\"_id\":\"" + id + "\",\"_enabled\":true,\"_abilityId\":\"basic-attack\",\"_environment\":0,\"_duration\":" + Number(duration) +
                ",\"_windupEnd\":0.1,\"_activeEnd\":0.25,\"_speed\":1,\"_dash\":{\"_enabled\":true,\"_opensAt\":0.25}," +
                "\"_jump\":{\"_enabled\":true,\"_opensAt\":0.25},\"_attack\":{\"_enabled\":true,\"_opensAt\":" + Number(next) +
                "},\"_move\":{\"_enabled\":true,\"_opensAt\":0.3},\"_motion\":1,\"_entryVelocity\":0,\"_velocity\":{\"x\":1,\"y\":1}," +
                "\"_moveSpeed\":3,\"_acceleration\":50,\"_recoveryId\":\"ordinary\",\"_windows\":[]}";
        }
    }
}
