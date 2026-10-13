using System;
using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    /// <summary>规则资产的显式编辑事务；绘制不创建或迁移资产。</summary>
    public static class PlayerSchemeRuleWorkflow
    {
        internal static void Assign(PlayerScheme scheme, PlayerSchemeRules rules, bool undo)
        {
            using var data = new SerializedObject(scheme);
            data.FindProperty("_rules").objectReferenceValue = rules;
            data.FindProperty("_ruleMode").enumValueIndex = rules == null ? 0 : 1;
            if (undo) data.ApplyModifiedProperties(); else data.ApplyModifiedPropertiesWithoutUndo();
        }
        internal static void ValidateOwnership(PlayerScheme scheme)
        {
            PlayerSchemeRules.Capture(scheme);
            if (scheme.Rules != null && AssetDatabase.Contains(scheme)
                && AssetDatabase.GetAssetPath(scheme.Rules) != AssetDatabase.GetAssetPath(scheme))
                throw new InvalidOperationException("规则必须是当前方案的独占子资产。");
        }
        public static void Enable(PlayerScheme scheme)
        {
            PlayerEditorAccess.Require(); PlayerSchemeOwnership.RequireWritable(scheme);
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请在非Play模式执行规则迁移。");
            if (scheme == null || !AssetDatabase.Contains(scheme) || scheme.RuleMode != PlayerRuleMode.LegacyCode || scheme.Rules != null)
                throw new InvalidOperationException("请选择已保存的旧版自定义方案。");
            PlayerSchemeWorkflow.Validate(scheme);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            PlayerSchemeRules created = null;
            try
            {
                Undo.RegisterCompleteObjectUndo(scheme, "启用方案规则编辑");
                created = PlayerSchemeRules.CreateNeutral(scheme);
                AssetDatabase.AddObjectToAsset(created, scheme);
                Undo.RegisterCreatedObjectUndo(created, "创建独占规则配置");
                Assign(scheme, created, true); ValidateOwnership(scheme);
                EditorUtility.SetDirty(scheme); EditorUtility.SetDirty(created);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                if (created != null) { AssetDatabase.RemoveObjectFromAsset(created); UnityEngine.Object.DestroyImmediate(created); }
                throw;
            }
        }
    }
}
