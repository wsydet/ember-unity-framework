using System;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    /// <summary>显式等价转换；沿用资源和图身份，不深复制配置，不设置项目默认。</summary>
    public static class PlayerSchemeConversion
    {
        #region 内部参数
        private static readonly string[] Fields =
        { "_graph", "_feel", "_powers", "_enableEffects", "_inputChannel", "_moveAction", "_jumpAction", "_dashAction" };
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>显式选择整套方案；环境保持，旧装配清空。不保存资产、不应用活动会话。</summary>
        public static void SelectScheme(PlayerConfig target, PlayerScheme scheme, bool recordUndo = true)
        {
            if (target == null || scheme == null || scheme.Graph == null || scheme.Feel == null)
                throw new InvalidOperationException("完整方案缺少图或手感配置。");
            _ = scheme.Feel.Capture(Physics2D.gravity);
            if (scheme.ActionEffects == null) throw new InvalidOperationException("请先显式迁移方案的动作效果。");
            scheme.ActionEffects.Validate(scheme.Graph);
            if (recordUndo) Undo.RecordObject(target, "选择完整玩家方案");
            using var data = new SerializedObject(target);
            data.FindProperty("_scheme").objectReferenceValue = scheme;
            foreach (string name in Fields)
            {
                var property = data.FindProperty(name);
                if (property.propertyType == SerializedPropertyType.ObjectReference) property.objectReferenceValue = null;
                else if (property.propertyType == SerializedPropertyType.Boolean) property.boolValue = false;
                else property.stringValue = "";
            }
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        public static PlayerScheme CreateFromLegacy(PlayerConfig source, string path)
        {
            if (source == null || source.Scheme != null)
                throw new InvalidOperationException("转换要求尚未绑定方案的 PlayerConfig。");
            if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("Assets/", StringComparison.Ordinal)
                || !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                || System.IO.File.Exists(path) || AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new ArgumentException("请选择 Assets 下未占用的 .asset 路径。", nameof(path));
            if (source.Graph == null || source.Feel == null)
                throw new InvalidOperationException("旧配置缺少图或手感，不能转换。");
            _ = source.Feel.Capture(Physics2D.gravity);
            var result = ScriptableObject.CreateInstance<PlayerScheme>();
            string effectsPath = path.Substring(0, path.Length - 6) + "Effects.asset";
            bool effectsCreated = false;
            try
            {
                using var from = new SerializedObject(source);
                using var to = new SerializedObject(result);
                foreach (string name in Fields) to.CopyFromSerializedProperty(from.FindProperty(name));
                to.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(result, path);
                PlayerActionEffectMigration.Migrate(result, effectsPath); effectsCreated = true;
                AssetDatabase.SaveAssetIfDirty(result);
                return result;
            }
            catch
            {
                if (effectsCreated) AssetDatabase.DeleteAsset(effectsPath);
                if (AssetDatabase.GetAssetPath(result) == path) AssetDatabase.DeleteAsset(path);
                else UnityEngine.Object.DestroyImmediate(result);
                throw;
            }
        }

        /// <summary>只接受相同引用与值的转换结果；原配置 GUID、Scene、Spawn 保持，旧装配清空。</summary>
        public static void BindEquivalent(PlayerConfig target, PlayerScheme scheme)
        {
            if (target == null || target.Scheme != null || scheme == null
                || target.Graph != scheme.Graph || target.Feel != scheme.Feel || target.Powers != scheme.Powers
                || target.EnableEffects != scheme.EnableEffects || target.Channel != scheme.Channel
                || target.MoveAction != scheme.MoveAction || target.JumpAction != scheme.JumpAction
                || target.DashAction != scheme.DashAction)
                throw new InvalidOperationException("仅允许把等价转换结果显式绑定到原配置。");
            Undo.RecordObject(target, "绑定等价玩家方案");
            using var data = new SerializedObject(target);
            data.FindProperty("_scheme").objectReferenceValue = scheme;
            foreach (string name in Fields)
            {
                var property = data.FindProperty(name);
                if (property.propertyType == SerializedPropertyType.ObjectReference) property.objectReferenceValue = null;
                else if (property.propertyType == SerializedPropertyType.Boolean) property.boolValue = false;
                else property.stringValue = "";
            }
            data.ApplyModifiedProperties();
        }
        #endregion
    }
}
