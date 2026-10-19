using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ember.Core.Fsm;
using Game.Entity;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Player.Editor
{
    /// <summary>完整配置的显式编辑事务；只复制配置，不复制基础图、状态声明或美术。</summary>
    public static class PlayerSchemeWorkflow
    {
        #region 内部方法
        private static bool Mutable(Object value) => value is ScriptableObject && value is not EmberFsmGraph
            && value is not EmberFsmStateDefinition && value is not MonoScript;
        private static void Remap(Object target, IReadOnlyDictionary<Object, Object> map)
        {
            using var data = new SerializedObject(target);
            var p = data.GetIterator();
            while (p.Next(true))
                if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue != null
                    && map.TryGetValue(p.objectReferenceValue, out var replacement)) p.objectReferenceValue = replacement;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void NewPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("..")
                || !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                || !AssetDatabase.IsValidFolder(Path.GetDirectoryName(path).Replace('\\', '/')))
                throw new ArgumentException("请选择 Assets 下有效的新 .asset 路径。");
            if (File.Exists(path) || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
                throw new InvalidOperationException("目标已存在，不覆盖：" + path);
        }
        private static void Bool(Object asset, params string[] names)
        {
            using var data = new SerializedObject(asset);
            foreach (var name in names) data.FindProperty(name).boolValue = false;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static string Value(SerializedProperty p, Dictionary<Object, string> labels)
        {
            if (p.propertyType == SerializedPropertyType.ObjectReference)
            {
                var value = p.objectReferenceValue;
                if (value == null) return "null";
                if (labels.TryGetValue(value, out string label)) return "配置:" + label;
                return GlobalObjectId.GetGlobalObjectIdSlow(value).ToString();
            }
            return p.propertyType switch
            {
                SerializedPropertyType.Float => p.doubleValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                SerializedPropertyType.Boolean => p.boolValue.ToString(),
                SerializedPropertyType.String => p.stringValue,
                SerializedPropertyType.Enum or SerializedPropertyType.Integer or SerializedPropertyType.LayerMask or SerializedPropertyType.ArraySize => p.longValue.ToString(),
                _ => p.propertyType.ToString()
            };
        }
        private static SortedDictionary<string, string> Values(PlayerScheme scheme)
        {
            var closure = Configuration(scheme); var labels = closure.ToDictionary(p => (Object)p.Value, p => p.Key);
            var values = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in closure)
            {
                values[pair.Key + "/类型"] = pair.Value.GetType().FullName;
                using var data = new SerializedObject(pair.Value); var p = data.GetIterator(); bool enter = true;
                while (p.Next(enter))
                {
                    // Unity 6 的 PPtr 暴露内部 m_FileID；引用已按配置路径归一，不能再比较实例指针。
                    enter = p.propertyType != SerializedPropertyType.ObjectReference;
                    if (p.propertyPath.StartsWith("_", StringComparison.Ordinal) && p.propertyType != SerializedPropertyType.Generic)
                        values[pair.Key + "/" + p.propertyPath] = Value(p, labels);
                }
            }
            return values;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static Dictionary<string, ScriptableObject> Configuration(PlayerScheme scheme)
        {
            if (scheme == null) throw new ArgumentNullException(nameof(scheme));
            var result = new Dictionary<string, ScriptableObject>(); var seen = new HashSet<Object>();
            void Visit(string key, ScriptableObject item)
            {
                if (!seen.Add(item)) return;
                result.Add(key, item);
                using var data = new SerializedObject(item); var p = data.GetIterator();
                while (p.Next(true))
                    if (p.propertyType == SerializedPropertyType.ObjectReference && Mutable(p.objectReferenceValue))
                        Visit(key + "/" + p.propertyPath, (ScriptableObject)p.objectReferenceValue);
            }
            Visit("方案", scheme); return result;
        }
        public static void Validate(PlayerScheme scheme)
        {
            if (scheme == null || scheme.Graph == null || scheme.Feel == null || scheme.Powers == null || scheme.Animation == null || scheme.ActionEffects == null)
                throw new InvalidOperationException("完整方案引用缺失：" + (scheme == null ? "Scheme" :
                    string.Join(", ", new[] { scheme.Graph == null ? "Graph" : null, scheme.Feel == null ? "Feel" : null,
                        scheme.Powers == null ? "Powers" : null, scheme.Animation == null ? "Animation" : null,
                        scheme.ActionEffects == null ? "ActionEffects" : null }.Where(s => s != null))));
            if (!scheme.Graph.Inspect(typeof(PlayerContext)).IsValid) throw new InvalidOperationException("基础图声明无效。");
            PlayerSchemeRuleWorkflow.ValidateOwnership(scheme);
            var feel = scheme.Feel.Capture(Physics2D.gravity);
            _ = new PlayerCombatConfiguration(scheme, feel.Attack);
            if (scheme.CombatGraph == null) throw new InvalidOperationException("Scheme.CombatGraph：节点归属清单为空。");
            scheme.CombatGraph.Validate(scheme.Graph, feel.Attack, scheme.Combo);
            if (feel.Attack.Enabled && string.IsNullOrWhiteSpace(scheme.AttackAction))
                throw new InvalidOperationException("Scheme.AttackAction：攻击开启时必须指定输入。");
            scheme.Animation.Validate(scheme.Graph); scheme.ActionEffects.Validate(scheme.Graph);
            using var powers = PlayerPowerSession.CreateNewGame(scheme.Powers);
            var config = ScriptableObject.CreateInstance<PlayerConfig>();
            try
            {
                PlayerSchemeConversion.SelectScheme(config, scheme, false);
                PlayerSchemeValidation.Validate(config, feel, powers);
            }
            finally { Object.DestroyImmediate(config); }
            foreach (var slot in scheme.ActionEffects.Slots)
                if (!scheme.EnableEffects && (slot.ActionEffect != null || slot.TailEffect != null)
                    && scheme.Powers.SkillSlots.Any(s => s.Enabled && s.Definition.Id ==
                        (scheme.Graph.Nodes.First(n => n.Id == slot.NodeId).Definition is PlayerGroundDashStateDefinition
                            ? PlayerGroundDashState.SKILL_ID : PlayerAirDashState.SKILL_ID)))
                    throw new InvalidOperationException("动作已配置效果但容器未启用；请显式修正装配。");
            if (string.IsNullOrWhiteSpace(scheme.Channel) || string.IsNullOrWhiteSpace(scheme.MoveAction) || string.IsNullOrWhiteSpace(scheme.JumpAction)
                || scheme.Powers.SkillSlots.Any(s => s.Enabled) && string.IsNullOrWhiteSpace(scheme.DashAction))
                throw new InvalidOperationException("方案输入接线缺失。");
        }
        public static PlayerScheme Copy(PlayerScheme source, string path, bool blank = false)
        {
            PlayerEditorAccess.Require();
            Validate(source); NewPath(path);
            var map = new Dictionary<Object, Object>(); bool created = false;
            try
            {
                foreach (var item in Configuration(source).Values)
                { var copy = Object.Instantiate(item); copy.name = item.name; copy.hideFlags = HideFlags.None; map.Add(item, copy); }
                foreach (var copy in map.Values) Remap(copy, map);
                var result = (PlayerScheme)map[source]; result.name = Path.GetFileNameWithoutExtension(path);
                if (blank)
                {
                    var neutral = PlayerSchemeRules.CreateNeutral(result);
                    if (result.Rules != null)
                    { EditorUtility.CopySerialized(neutral, result.Rules); Object.DestroyImmediate(neutral); }
                    else
                    { PlayerSchemeRuleWorkflow.Assign(result, neutral, false); map.Add(neutral, neutral); }
                    Bool(result, "_enableEffects");
                    Bool(result.Feel, "_attack._enabled");
                    Bool(result.Feel.Movement, "_accelerated", "_independentAir", "_airAccelerated", "_enableJumpResponse", "_landing._normalEnabled", "_landing._hardEnabled", "_rolling._enabled");
                    Bool(result.Feel.Jump, "_ordinaryJump._enabled", "_enableShortJump", "_enableApexGravity", "_enableFallGravity", "_enableFallLimit", "_enableEdgeHover", "_enableWallFastSlide", "_wallReturnEnabled");
                    Bool(result.Feel.Input, "_enableJumpBuffer", "_enableCoyote");
                    Bool(result.Feel.Movement, "_jumpCorrection._enabled", "_groundDashCorrection._enabled", "_airDashCorrection._enabled", "_stepCorrection._enabled");
                    using var powers = new SerializedObject(result.Powers);
                    foreach (var name in new[] { "_abilities", "_skills" })
                    { var slots = powers.FindProperty(name); for (int i = 0; i < slots.arraySize; i++) slots.GetArrayElementAtIndex(i).FindPropertyRelative("_enabled").boolValue = false; }
                    powers.ApplyModifiedPropertiesWithoutUndo();
                    result.ActionEffects.Configure(result.ActionEffects.Slots.Select(s => new PlayerActionEffectSlot(s.NodeId, null, null)).ToArray());
                    var basic = result.Feel.Capture(Physics2D.gravity);
                    if (basic.MoveSpeed <= 0 || basic.GravityScale <= 0 || Physics2D.gravity.y >= 0 || Physics2D.gravity.x != 0)
                        throw new InvalidOperationException("空白方案需要正的基础速度、重力倍率和竖直向下的世界重力；请选择有效基础参考。");
                }
                Validate(result);
                AssetDatabase.CreateAsset(result, path); created = true;
                foreach (var copy in map.Values) if (copy != result) AssetDatabase.AddObjectToAsset(copy, result);
                AssetDatabase.SaveAssetIfDirty(result); return result;
            }
            catch
            {
                if (created) AssetDatabase.DeleteAsset(path);
                foreach (var copy in map.Values) if (copy != null && !AssetDatabase.Contains(copy)) Object.DestroyImmediate(copy);
                throw;
            }
        }
        public static void Save(PlayerScheme scheme)
        {
            PlayerEditorAccess.Require();
            var assets = Configuration(scheme).Values;
            foreach (var item in assets) { PlayerSchemeOwnership.RequireWritable(item); if (!AssetDatabase.Contains(item)) throw new InvalidOperationException("配置尚未保存为资产。"); }
            Validate(scheme);
            foreach (var item in assets) AssetDatabase.SaveAssetIfDirty(item);
        }
        public static Object CopyArt(Object source, string path)
        {
            PlayerEditorAccess.Require();
            string original = AssetDatabase.GetAssetPath(source);
            if (source is not AnimationClip && source is not RuntimeAnimatorController && source is not Texture2D && source is not Sprite)
                throw new ArgumentException("请选择动画片段、Controller 或图片资源。");
            if (string.IsNullOrEmpty(original) || !path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("..")
                || Path.GetExtension(path) != Path.GetExtension(original) || File.Exists(path)
                || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path))
                || !AssetDatabase.IsValidFolder(Path.GetDirectoryName(path).Replace('\\', '/')))
                throw new ArgumentException("请选择 Assets 内同格式的新素材路径，不覆盖现有文件。");
            if (!AssetDatabase.CopyAsset(original, path)) throw new InvalidOperationException("素材复制失败。");
            foreach (var item in AssetDatabase.LoadAllAssetsAtPath(path))
                if (item != null) item.hideFlags &= ~HideFlags.NotEditable;
            return AssetDatabase.LoadMainAssetAtPath(path);
        }
        public static List<string> Compare(PlayerScheme current, PlayerScheme reference)
        {
            var left = Values(current); var right = Values(reference); var result = new List<string>();
            foreach (string key in left.Keys.Union(right.Keys).OrderBy(k => k, StringComparer.Ordinal))
            { left.TryGetValue(key, out string a); right.TryGetValue(key, out string b); if (a != b) result.Add(key + "\n" + (a ?? "缺失") + " → " + (b ?? "缺失")); }
            return result;
        }
        public static void Restore(PlayerScheme target, PlayerScheme reference)
        {
            PlayerEditorAccess.Require();
            if (target == null || !AssetDatabase.Contains(target)) throw new InvalidOperationException("请选择已保存的自定义方案。");
            Validate(reference); PlayerSchemeOwnership.RequireWritable(target);
            PlayerSchemeStructureWorkflow.RequireCompatible(target, reference);
            string path = AssetDatabase.GetAssetPath(target);
            var destination = Configuration(target); var source = Configuration(reference);
            var previousRules = target.Rules;
            foreach (var item in destination.Values)
            {
                PlayerSchemeOwnership.RequireWritable(item);
                if (AssetDatabase.GetAssetPath(item) != path) throw new InvalidOperationException("当前方案仍共享外部可变配置，请先独立另存后恢复。");
            }
            var map = new Dictionary<Object, Object>(); var claimed = new HashSet<Object>(); var added = new List<Object>();
            int group = Undo.GetCurrentGroup(); Undo.IncrementCurrentGroup(); group = Undo.GetCurrentGroup();
            try
            {
                foreach (var pair in source)
                {
                    if (destination.TryGetValue(pair.Key, out var value) && value.GetType() == pair.Value.GetType() && claimed.Add(value)) map.Add(pair.Value, value);
                    else { var copy = Object.Instantiate(pair.Value); copy.hideFlags = HideFlags.None; copy.name = pair.Value.name; added.Add(copy); map.Add(pair.Value, copy); }
                }
                foreach (var item in added)
                {
                    AssetDatabase.AddObjectToAsset(item, target);
                    if (item is PlayerSchemeRules) Undo.RegisterCreatedObjectUndo(item, "恢复规则配置子资产");
                }
                Undo.RegisterCompleteObjectUndo(destination.Values.Cast<Object>().ToArray(), "恢复完整方案参考参数");
                var references = new Dictionary<Object, Object>(map)
                {
                    [reference.Graph] = target.Graph,
                    [reference.Animation.Controller] = target.Animation.Controller
                };
                foreach (var slot in reference.Animation.Slots)
                    references[slot.Key] = target.Animation.Slots.First(s => s.NodeId == slot.NodeId).Key;
                foreach (var pair in map)
                { string name = pair.Value.name; EditorUtility.CopySerialized(pair.Key, pair.Value); pair.Value.name = name; pair.Value.hideFlags = HideFlags.None; Remap(pair.Value, references); EditorUtility.SetDirty(pair.Value); }
                Validate(target);
                if (previousRules != null && target.Rules != previousRules) Undo.DestroyObjectImmediate(previousRules);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                foreach (var item in added) if (item != null) { AssetDatabase.RemoveObjectFromAsset(item); Object.DestroyImmediate(item); }
                throw;
            }
        }
        public static void SetDefault(PlayerScheme scheme)
        {
            PlayerEditorAccess.Require();
            Validate(scheme); var config = Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH);
            PlayerSchemeConversion.SelectScheme(config, scheme);
        }
        public static void Use(PlayerScheme scheme)
        {
            PlayerEditorAccess.Require(); Validate(scheme); PlayerDevelopmentEnvironment.SetSchemeParameters(scheme, PlayerDevelopmentEnvironment.SelectedPosition, PlayerDevelopmentEnvironment.SelectedVelocity); }
        #endregion
    }
}
