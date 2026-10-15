using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Core.Fsm;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Player.Editor
{
    /// <summary>战斗结构联合预检与 Undo 事务。不保存已有资产，不修改源布局，不隐式删除节点。</summary>
    internal static class PlayerCombatStructureTransaction
    {
        #region 内部方法
        private static void SetCombat(PlayerScheme scheme, PlayerAttackProfile attack, PlayerCombo combo, PlayerCombatGraphMap map)
        {
            scheme.Feel.SetAttackConfiguration(attack);
            scheme.SetCombatConfiguration(combo, map);
        }

        private static void SetReference(Object target, string name, Object value)
        {
            using var data = new SerializedObject(target);
            data.FindProperty(name).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ExtendAnimation(PlayerAnimationProfile profile, AnimatorController controller, EmberFsmGraph graph,
            IReadOnlyDictionary<string, AnimationClip> clips, bool undo, List<Object> created)
        {
            var machine = controller.layers[0].stateMachine;
            using var data = new SerializedObject(profile);
            var slots = data.FindProperty("_slots");
            var existing = new HashSet<string>(profile.Slots.Select(s => s.NodeId), StringComparer.Ordinal);
            foreach (var node in graph.Nodes)
            {
                if (existing.Contains(node.Id)) continue;
                var role = node.Definition as IPlayerCombatNodeDefinition;
                if (role == null && node.Definition is not IPlayerLifeNodeDefinition) throw new InvalidOperationException("动画生成器不拥有节点：" + node.Id);
                clips.TryGetValue(node.Id, out var clip);
                if (clip == null && role?.Role == PlayerCombatNodeRole.Attack)
                    throw new InvalidOperationException("必需攻击 Clip 缺失：" + node.Id);
                var fallback = clip ?? profile.Slots.First(s => s.NodeId == "idle").Clip;
                if (fallback == null) throw new InvalidOperationException("缺少收刀 Idle 回退。");
                string stateName = node.Id.Replace('.', '_');
                if (machine.states.Any(s => s.state.name == stateName)) throw new InvalidOperationException("Animator 已有同名用户状态：" + stateName);
                var key = Object.Instantiate(fallback); key.name = node.Id + " Key"; key.hideFlags = HideFlags.None;
                AssetDatabase.AddObjectToAsset(key, controller); created.Add(key);
                if (undo) Undo.RegisterCreatedObjectUndo(key, "新增战斗动画 Key");
                var state = machine.AddState(stateName); created.Add(state); state.motion = key;
                if (undo) Undo.RegisterCreatedObjectUndo(state, "新增战斗动画状态");
                int index = slots.arraySize++; var slot = slots.GetArrayElementAtIndex(index);
                slot.FindPropertyRelative("_nodeId").stringValue = node.Id;
                slot.FindPropertyRelative("_state").stringValue = controller.layers[0].name + "." + state.name;
                slot.FindPropertyRelative("_key").objectReferenceValue = key;
                slot.FindPropertyRelative("_clip").objectReferenceValue = clip;
                slot.FindPropertyRelative("_movingRecoveryClip").objectReferenceValue = null;
                slot.FindPropertyRelative("_speed").floatValue = 1;
                slot.FindPropertyRelative("_blendSeconds").floatValue = 0;
            }
            // Controller 资产复制会复制内嵌 Key，按已有状态目标恢复对应引用。
            for (int i = 0; i < slots.arraySize; i++)
            {
                var slot = slots.GetArrayElementAtIndex(i);
                string path = slot.FindPropertyRelative("_state").stringValue;
                var state = machine.states.First(s => controller.layers[0].name + "." + s.state.name == path).state;
                slot.FindPropertyRelative("_key").objectReferenceValue = state.motion;
            }
            if (undo) Undo.RegisterCompleteObjectUndo(profile, "更新战斗动画映射");
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RequireIndependent(PlayerScheme scheme)
        {
            foreach (var item in PlayerSchemeWorkflow.Configuration(scheme).Values.Append<ScriptableObject>(scheme.Graph))
            {
                PlayerSchemeOwnership.RequireWritable(item);
                if (!AssetDatabase.Contains(item)) throw new InvalidOperationException("结构编辑要求已保存的独立方案。");
            }
            PlayerSchemeOwnership.RequireWritable(scheme.Animation.Controller);
            foreach (string guid in AssetDatabase.FindAssets("t:PlayerScheme"))
            {
                var other = AssetDatabase.LoadAssetAtPath<PlayerScheme>(AssetDatabase.GUIDToAssetPath(guid));
                if (other == null || other == scheme) continue;
                if (other.Graph == scheme.Graph || other.Animation != null && other.Animation.Controller == scheme.Animation.Controller ||
                    PlayerSchemeWorkflow.Configuration(other).Values.Intersect(PlayerSchemeWorkflow.Configuration(scheme).Values).Any())
                    throw new InvalidOperationException("结构或可变配置仍被另一方案共享，请先独立复制：" + other.name);
            }
        }
        private static void DisposeCandidate(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return;
            foreach (string name in new[] { "/Candidate.asset", "/Candidate.controller" })
                foreach (var item in AssetDatabase.LoadAllAssetsAtPath(folder + name)) if (item != null) Undo.ClearUndo(item);
            AssetDatabase.DeleteAsset(folder);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal static void Apply(PlayerScheme scheme, PlayerAttackProfile attack, PlayerCombo combo,
            EmberFsmStateDefinition attackDefinition, EmberFsmStateDefinition recoveryDefinition,
            IReadOnlyDictionary<string, AnimationClip> clips, PlayerCombatLocomotionStateDefinition locomotionDefinition = null,
            PlayerDelayedAttackStateDefinition delayedAttackDefinition = null, PlayerDelayedRecoveryStateDefinition delayedRecoveryDefinition = null,
            PlayerDashAttackRouteDefinition dashRouteDefinition = null, PlayerDashRecoveryStateDefinition dashRecoveryDefinition = null)
        {
            PlayerEditorAccess.Require(); PlayerSchemeWorkflow.Validate(scheme); RequireIndependent(scheme);
            if (clips == null) throw new ArgumentNullException(nameof(clips));
            var controller = scheme.Animation.Controller as AnimatorController;
            string controllerPath = AssetDatabase.GetAssetPath(controller);
            if (controller == null || string.IsNullOrEmpty(controllerPath) || AssetDatabase.LoadAllAssetsAtPath(controllerPath).Any(EditorUtility.IsDirty))
                throw new InvalidOperationException("请先保存 Controller 草稿，联合预检不会以磁盘旧值替代内存修改。");
            var graph = PlayerCombatGraphBuilder.Prepare(scheme.Graph, scheme.CombatGraph, attack, combo,
                attackDefinition, recoveryDefinition, out var map, locomotionDefinition, delayedAttackDefinition, delayedRecoveryDefinition,
                dashRouteDefinition, dashRecoveryDefinition);
            CommitPrepared(scheme, attack, combo, map, graph, clips, controller, controllerPath,
                new EmberFsmStateDefinition[] { attackDefinition, recoveryDefinition, locomotionDefinition, delayedAttackDefinition,
                    delayedRecoveryDefinition, dashRouteDefinition, dashRecoveryDefinition });
        }

        private static void CommitPrepared(PlayerScheme scheme, PlayerAttackProfile attack, PlayerCombo combo, PlayerCombatGraphMap map,
            EmberFsmGraph graph, IReadOnlyDictionary<string, AnimationClip> clips, AnimatorController controller, string controllerPath,
            EmberFsmStateDefinition[] prototypes, PlayerLifeConfiguration life = null, PlayerReactionConfiguration reactions = null)
        {
            string folder = "Assets/CombatTransaction_" + Guid.NewGuid().ToString("N");
            var created = new List<Object>(); int group = -1;
            // 回滚只恢复本次修改引入的脏标记，绝不清除用户已有草稿。
            var cleanSnapshots = PlayerSchemeWorkflow.Configuration(scheme).Values.Cast<Object>()
                .Append(scheme).Append(scheme.Graph).Concat(AssetDatabase.LoadAllAssetsAtPath(controllerPath))
                .Where(item => item != null && !EditorUtility.IsDirty(item)).Distinct()
                .ToDictionary(item => item, item => EditorJsonUtility.ToJson(item));
            string phase = "候选准备";
            try
            {
                AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
                var candidate = PlayerSchemeWorkflow.Copy(scheme, folder + "/Candidate.asset");
                if (!AssetDatabase.CopyAsset(controllerPath, folder + "/Candidate.controller")) throw new InvalidOperationException("候选 Controller 复制失败。");
                var candidateController = AssetDatabase.LoadAssetAtPath<AnimatorController>(folder + "/Candidate.controller");
                candidate.SetPreparedGraph(graph);
                SetReference(candidate.Animation, "_controller", candidateController);
                SetCombat(candidate, attack, combo, map);
                if (life != null) candidate.SetLifeConfiguration(life, reactions);
                ExtendAnimation(candidate.Animation, candidateController, graph, clips, false, new List<Object>());
                PlayerSchemeWorkflow.Validate(candidate);
                DisposeCandidate(folder);

                phase = "目标提交";
                Undo.IncrementCurrentGroup(); group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("更新战斗方案结构");
                Undo.RegisterCompleteObjectUndo(new Object[] { controller, controller.layers[0].stateMachine }, "更新战斗方案结构");
                var definitions = new Dictionary<EmberFsmStateDefinition, EmberFsmStateDefinition>();
                var closure = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(scheme.Graph))
                    .Where(item => item is ScriptableObject).ToDictionary(item => item, item => item);
                foreach (var definition in prototypes.Where(d => d != null))
                {
                    definitions[definition] = (EmberFsmStateDefinition)PlayerSchemeStructureWorkflow.CopyDependency(definition, closure, created);
                }
                foreach (var copy in created)
                {
                    AssetDatabase.AddObjectToAsset(copy, scheme.Graph);
                    Undo.RegisterCreatedObjectUndo(copy, "新增战斗定义与配置");
                }
                ExtendAnimation(scheme.Animation, controller, graph, clips, true, created);
                Undo.RegisterCompleteObjectUndo(new Object[] { scheme, scheme.Feel, scheme.Graph }, "更新战斗配置与图");
                scheme.Graph.Configure(graph.EntryId, graph.Nodes.Select(n => new EmberFsmNode(n.Id, n.DisplayName,
                    definitions.TryGetValue(n.Definition, out var definition) ? definition : n.Definition,
                    n.ParentId, n.DefaultChildId, n.Bindings.ToArray())).ToArray());
                SetCombat(scheme, attack, combo, map);
                if (life != null) scheme.SetLifeConfiguration(life, reactions);
                PlayerSchemeWorkflow.Validate(scheme);
                foreach (var item in new Object[] { scheme, scheme.Feel, scheme.Graph, scheme.Animation, controller, controller.layers[0].stateMachine }) EditorUtility.SetDirty(item);
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(group);
            }
            catch (Exception error)
            {
                if (group >= 0) Undo.RevertAllDownToGroup(group);
                foreach (var item in created) if (item != null) { AssetDatabase.RemoveObjectFromAsset(item); Object.DestroyImmediate(item); }
                foreach (var snapshot in cleanSnapshots)
                    if (snapshot.Key != null && EditorJsonUtility.ToJson(snapshot.Key) == snapshot.Value)
                        EditorUtility.ClearDirty(snapshot.Key);
                throw new InvalidOperationException("战斗联合事务失败（" + phase + "）：" + error.Message, error);
            }
            finally
            {
                Object.DestroyImmediate(graph);
                DisposeCandidate(folder);
            }
        }
        internal static void ApplyLife(PlayerScheme scheme, PlayerLifeConfiguration life, PlayerReactionConfiguration reactions,
            AnimationClip hitClip, AnimationClip deathClip)
        {
            PlayerEditorAccess.Require(); PlayerSchemeWorkflow.Validate(scheme); RequireIndependent(scheme);
            life.Validate(); reactions.Validate();
            var controller = scheme.Animation.Controller as AnimatorController;
            string controllerPath = AssetDatabase.GetAssetPath(controller);
            if (controller == null || string.IsNullOrEmpty(controllerPath) || AssetDatabase.LoadAllAssetsAtPath(controllerPath).Any(EditorUtility.IsDirty))
                throw new InvalidOperationException("生命结构事务要求先保存 Controller 草稿。");
            var root = scheme.Graph.Nodes.First(n => n.Id == scheme.Graph.EntryId);
            if (root.Definition is IPlayerLifeRootDefinition || scheme.Graph.Nodes.Any(n => n.Id is "life.hit" or "life.dead") ||
                root.Bindings.Any(b => b.Slot.StartsWith("life-", StringComparison.Ordinal)))
                throw new InvalidOperationException("已存在生命结构或用户ID冲突；不会覆盖。");
            EmberFsmStateDefinition rootDefinition;
            if (root.Definition is PlayerDashCombatLocomotionStateDefinition) rootDefinition = ScriptableObject.CreateInstance<PlayerLifeDashLocomotionStateDefinition>();
            else if (root.Definition is PlayerCombatLocomotionStateDefinition) rootDefinition = ScriptableObject.CreateInstance<PlayerLifeCombatLocomotionStateDefinition>();
            else if (root.Definition is PlayerLocomotionStateDefinition) rootDefinition = ScriptableObject.CreateInstance<PlayerLifeLocomotionStateDefinition>();
            else throw new InvalidOperationException("生命事务不能覆盖自定义根声明。");
            var hit = ScriptableObject.CreateInstance<PlayerHitStateDefinition>();
            var death = ScriptableObject.CreateInstance<PlayerDeathStateDefinition>();
            var graph = ScriptableObject.CreateInstance<EmberFsmGraph>();
            try
            {
                var nodes = scheme.Graph.Nodes.Select(n => n.Id == root.Id ? new EmberFsmNode(n.Id, n.DisplayName, rootDefinition,
                    n.ParentId, n.DefaultChildId, n.Bindings.Concat(new[] { new EmberFsmTargetBinding("life-hit", "life.hit"), new EmberFsmTargetBinding("life-death", "life.dead") }).ToArray()) : n).ToList();
                nodes.Add(new EmberFsmNode("life.hit", "受击", hit, root.Id, null,
                    new[] { new EmberFsmTargetBinding("ground", "ground"), new EmberFsmTargetBinding("fall", "fall") }));
                nodes.Add(new EmberFsmNode("life.dead", "死亡确认", death, root.Id, null, Array.Empty<EmberFsmTargetBinding>()));
                graph.Configure(scheme.Graph.EntryId, nodes.ToArray());
                if (!graph.Inspect(typeof(PlayerContext)).IsValid) throw new InvalidOperationException("生命图候选声明无效。");
                CommitPrepared(scheme, scheme.Feel.Attack, scheme.Combo, scheme.CombatGraph, graph,
                    new Dictionary<string, AnimationClip> { ["life.hit"] = hitClip, ["life.dead"] = deathClip }, controller, controllerPath,
                    new[] { rootDefinition, hit, death }, life, reactions);
            }
            finally
            {
                if (graph != null) Object.DestroyImmediate(graph);
                Object.DestroyImmediate(rootDefinition); Object.DestroyImmediate(hit); Object.DestroyImmediate(death);
            }
        }
        #endregion
    }
}
