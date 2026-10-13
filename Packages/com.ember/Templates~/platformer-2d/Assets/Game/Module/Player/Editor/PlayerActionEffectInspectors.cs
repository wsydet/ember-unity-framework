using UnityEditor;
using UnityEngine;

namespace Game.Player.Editor
{
    [CustomEditor(typeof(PlayerGroundDashStateDefinition))]
    internal sealed class PlayerGroundDashDefinitionInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (PlayerEditorAccess.DrawUnavailable()) return;
            EditorGUILayout.HelpBox("效果旧字段仅保留供显式迁移读取。请在蓝图中明确选择完整方案后编辑动作效果；此 Definition 不再提供运行效果来源。", MessageType.Info);
            using (new EditorGUI.DisabledScope(true))
            { serializedObject.Update(); EditorGUILayout.PropertyField(serializedObject.FindProperty("_actionEffect")); EditorGUILayout.PropertyField(serializedObject.FindProperty("_tailEffect")); }
        }
    }
    [CustomEditor(typeof(PlayerAirDashStateDefinition))]
    internal sealed class PlayerAirDashDefinitionInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (PlayerEditorAccess.DrawUnavailable()) return;
            EditorGUILayout.HelpBox("遗留效果仅供迁移读取；请明确选择完整方案编辑新接线。", MessageType.Info);
            using (new EditorGUI.DisabledScope(true))
            { serializedObject.Update(); EditorGUILayout.PropertyField(serializedObject.FindProperty("_actionEffect")); EditorGUILayout.PropertyField(serializedObject.FindProperty("_tailEffect")); }
        }
    }
    [CustomEditor(typeof(PlayerScheme))]
    internal sealed class PlayerSchemeInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (PlayerEditorAccess.DrawUnavailable()) return;
            PlayerSchemeWindow.DrawAsset((PlayerScheme)target);
            if (GUILayout.Button("完整方案工作流")) PlayerSchemeWindow.Open((PlayerScheme)target);
            var scheme = (PlayerScheme)target;
            if (GUILayout.Button("转换附加条件 · 蓝图编辑")) PlayerSchemeRuleEditor.Open(scheme);
            if (scheme.ActionEffects != null)
            {
                try { scheme.ActionEffects.Validate(scheme.Graph); }
                catch (System.Exception error) { EditorGUILayout.HelpBox(error.Message, MessageType.Error); }
                return;
            }
            EditorGUILayout.HelpBox("尚未迁移动作效果，完整方案运行将拒绝。显式迁移保留原空效果与业务 ID；创建配置文件后，方案引用待显式保存，支持 Undo/Redo。", MessageType.Warning);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("显式等价迁移原 Definition 效果…"))
                {
                    string path = EditorUtility.SaveFilePanelInProject("迁移动作效果", scheme.name + "Effects", "asset", "创建独立效果配置");
                    if (!string.IsNullOrEmpty(path))
                        try { PlayerActionEffectMigration.Migrate(scheme, path); }
                        catch (System.Exception error) { EditorUtility.DisplayDialog("迁移未完成", error.Message, "确定"); }
                }
        }
    }
}
