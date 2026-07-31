using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    [CustomEditor(typeof(EmberDebugConfigSO))]
    public class EmberDebugConfigEditor : UnityEditor.Editor
    {
        private Vector2 _scroll;

        public override void OnInspectorGUI()
        {
            var config = (EmberDebugConfigSO)target;

            // ---- 全局 ----
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("全局设置", EditorStyles.boldLabel);
            config.globalOpen = EditorGUILayout.Toggle("全局开关", config.globalOpen);
            config.autoCollect = EditorGUILayout.Toggle("自动收集新类", config.autoCollect);

            EditorGUILayout.Space();

            // ---- 一键按钮 ----
            EditorGUILayout.LabelField("批量操作", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全部开启", GUILayout.Height(24)))
            {
                config.EnableAll();
                EditorUtility.SetDirty(config);
            }
            if (GUILayout.Button("全部关闭", GUILayout.Height(24)))
            {
                config.DisableAll();
                EditorUtility.SetDirty(config);
            }
            if (GUILayout.Button("清理空项", GUILayout.Height(24)))
            {
                config.classEntries.RemoveAll(e => string.IsNullOrEmpty(e.className));
                EditorUtility.SetDirty(config);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // ---- 类列表 ----
            EditorGUILayout.LabelField($"类配置 ({config.classEntries.Count})", EditorStyles.boldLabel);

            if (config.classEntries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "还没有任何类记录。运行时调用 EmberDebug.Log() 会自动收集（如果开启了\"自动收集新类\"）。",
                    MessageType.Info);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

            for (int i = config.classEntries.Count - 1; i >= 0; i--)
            {
                var entry = config.classEntries[i];

                if (entry == null || string.IsNullOrEmpty(entry.className))
                {
                    config.classEntries.RemoveAt(i);
                    continue;
                }

                bool isPredefined = LogTagColors.IsPredefined(entry.className);
                bool isChild = entry.className.Contains('.');
                string indent = isChild ? "  └─ " : "";

                EditorGUILayout.BeginHorizontal();

                // 缩进
                if (isChild)
                    GUILayout.Space(16);

                // 开关
                entry.enabled = EditorGUILayout.Toggle(entry.enabled, GUILayout.Width(20));

                // 颜色（预定义标签锁住不可编辑）
                var prevEnabled = GUI.enabled;
                GUI.enabled = !isPredefined;
                entry.color = EditorGUILayout.ColorField(entry.color, GUILayout.Width(36));
                GUI.enabled = prevEnabled;

                // 类名 + 层级标记
                EditorGUILayout.LabelField(isPredefined
                    ? $"{indent}{entry.className}  🔒"
                    : $"{indent}{entry.className}");

                // 删除按钮
                if (GUILayout.Button("×", GUILayout.Width(24)))
                {
                    config.classEntries.RemoveAt(i);
                    EditorUtility.SetDirty(config);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (GUI.changed)
                EditorUtility.SetDirty(config);
        }
    }
}
