using UnityEditor;

namespace Game.Player.Editor
{
    using UnityEngine;
    [CustomEditor(typeof(PlayerConfig))]
    internal sealed class PlayerConfigInspector : UnityEditor.Editor
    {
        private PlayerScheme _selection;
        public override void OnInspectorGUI()
        {
            if (PlayerEditorAccess.DrawUnavailable()) return;
            serializedObject.Update();
            var scheme = serializedObject.FindProperty("_scheme");
            if (scheme.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("旧装配兼容模式。完整方案转换需显式执行；不会自动改变项目默认。", MessageType.Info);
                DrawPropertiesExcluding(serializedObject, "m_Script", "_scheme");
            }
            else
            {
                // 第一段只展示绑定结果，避免随意清空引用后启用已清除的历史字段。
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(scheme);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_scene"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_spawn"));
                EditorGUILayout.HelpBox("玩家装配仅由引用方案提供；Scene/Spawn 是环境接线。", MessageType.Info);
            }
            serializedObject.ApplyModifiedProperties();
            if (GUILayout.Button("完整玩家方案 / 内置与自定义")) PlayerSchemeWindow.Open(((PlayerConfig)target).Scheme);
            _selection = (PlayerScheme)EditorGUILayout.ObjectField("待选择完整方案", _selection, typeof(PlayerScheme), false);
            using (new EditorGUI.DisabledScope(_selection == null || EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("将此方案设为该配置默认（不自动保存）"))
                {
                    PlayerSchemeWorkflow.Validate(_selection);
                    PlayerSchemeConversion.SelectScheme((PlayerConfig)target, _selection);
                }
        }
    }
}
