using System;
using System.Collections.Generic;
using System.IO;
using Game.Entity;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    /// <summary>显式迁移；先完整构造及校验，再创建单个配置文件，最后可撤销地接入方案。</summary>
    public static class PlayerActionEffectMigration
    {
        #region 外部方法
        public static PlayerActionEffects Migrate(PlayerScheme scheme, string path)
        {
            if (scheme == null || scheme.Graph == null) throw new ArgumentException("方案或基础图缺失。");
            PlayerSchemeOwnership.RequireWritable(scheme);
            if (scheme.ActionEffects != null)
            { scheme.ActionEffects.Validate(scheme.Graph); return scheme.ActionEffects; }
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal)
                || path.Contains("..") || !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                || !AssetDatabase.IsValidFolder(Path.GetDirectoryName(path).Replace('\\', '/')))
                throw new ArgumentException("请选择 Assets 下有效的新 .asset 路径。");
            if (File.Exists(path) || !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
                throw new InvalidOperationException("目标已经存在，不覆盖。撤销迁移后可重做以恢复原接线：" + path);
            var result = ScriptableObject.CreateInstance<PlayerActionEffects>();
            var copies = new Dictionary<EntityEffectDefinition, EntityEffectDefinition>();
            bool created = false;
            try
            {
                EntityEffectDefinition Copy(EntityEffectDefinition source)
                {
                    if (source == null) return null;
                    if (copies.TryGetValue(source, out var existing)) return existing;
                    var copy = UnityEngine.Object.Instantiate(source); copy.name = source.name;
                    copies.Add(source, copy); return copy;
                }
                var slots = new List<PlayerActionEffectSlot>();
                foreach (var node in scheme.Graph.Nodes)
                {
                    if (node.Definition is PlayerGroundDashStateDefinition ground)
                        slots.Add(new PlayerActionEffectSlot(node.Id, Copy(ground.LegacyActionEffect), Copy(ground.LegacyTailEffect)));
                    else if (node.Definition is PlayerAirDashStateDefinition air)
                        slots.Add(new PlayerActionEffectSlot(node.Id, Copy(air.LegacyActionEffect), Copy(air.LegacyTailEffect)));
                }
                result.Configure(slots.ToArray()); result.Validate(scheme.Graph);
                AssetDatabase.CreateAsset(result, path); created = true;
                foreach (var copy in copies.Values) AssetDatabase.AddObjectToAsset(copy, result);
                AssetDatabase.SaveAssetIfDirty(result);
                Undo.RecordObject(scheme, "迁移动作效果接线");
                using var data = new SerializedObject(scheme);
                data.FindProperty("_actionEffects").objectReferenceValue = result;
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(scheme);
                // 文件显式创建；Undo 只解除方案引用，保留内容供 Redo，不删除可能已被用户编辑的配置。
                return result;
            }
            catch
            {
                if (created) AssetDatabase.DeleteAsset(path);
                foreach (var copy in copies.Values) if (copy != null && !AssetDatabase.Contains(copy)) UnityEngine.Object.DestroyImmediate(copy);
                if (result != null && !AssetDatabase.Contains(result)) UnityEngine.Object.DestroyImmediate(result);
                throw;
            }
        }

        public static void Save(PlayerScheme scheme)
        {
            if (scheme == null || scheme.ActionEffects == null) throw new InvalidOperationException("方案尚未迁移动作效果。");
            scheme.ActionEffects.Validate(scheme.Graph);
            var assets = new HashSet<UnityEngine.Object> { scheme, scheme.ActionEffects };
            foreach (var slot in scheme.ActionEffects.Slots)
            { if (slot.ActionEffect != null) assets.Add(slot.ActionEffect); if (slot.TailEffect != null) assets.Add(slot.TailEffect); }
            foreach (var asset in assets)
            { PlayerSchemeOwnership.RequireWritable(asset); if (!AssetDatabase.Contains(asset)) throw new InvalidOperationException("请先创建实际配置资产。"); }
            foreach (var asset in assets) AssetDatabase.SaveAssetIfDirty(asset);
        }
        #endregion
    }
}
