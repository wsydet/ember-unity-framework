using UnityEditor;
using UnityEngine;

namespace Game.Module.Guide.Editor
{
    /// <summary>
    /// <see cref="GuideDefine"/> 自定义 Inspector —— 提供「添加步骤」按钮与开始 / 结束阶段分组。
    ///
    /// 条件 / 事件 / 执行器字段均为 <c>[SerializeReference]</c>，Unity 默认的托管引用
    /// 绘制器会自动提供「类型下拉」，无需为每种子类手写编辑器。
    /// </summary>
    [CustomEditor(typeof(GuideDefine))]
    public class GuideDefineEditor : UnityEditor.Editor
    {
        private bool _stepsFoldout = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var baseSkipAll = serializedObject.FindProperty("baseSkipAll");
            var guideSteps = serializedObject.FindProperty("guideSteps");

            EditorGUILayout.PropertyField(baseSkipAll, true);
            EditorGUILayout.Space();

            _stepsFoldout = EditorGUILayout.Foldout(_stepsFoldout, $"步骤列表 ({guideSteps.arraySize})", true);
            if (_stepsFoldout)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < guideSteps.arraySize; i++)
                    DrawStep(guideSteps.GetArrayElementAtIndex(i), i);
                EditorGUI.indentLevel--;

                EditorGUILayout.Space();
                if (GUILayout.Button("添加步骤"))
                    guideSteps.arraySize++;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawStep(SerializedProperty step, int index)
        {
            var name = step.FindPropertyRelative("name");
            var needUpdate = step.FindPropertyRelative("needUpdate");

            var title = string.IsNullOrEmpty(name.stringValue)
                ? $"步骤 {index}"
                : $"步骤 {index} - {name.stringValue}";

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(name);
            EditorGUILayout.PropertyField(needUpdate);

            DrawSection(step, "开始阶段", new[]
            {
                "startEvents",
                "startConditionsToSkipAll",
                "startConditionsToSkip",
                "startConditionsToSuccess",
                "startExecutors",
            });

            DrawSection(step, "结束阶段", new[]
            {
                "endEvents",
                "endConditionsToFinishAll",
                "endConditionsToCancelAll",
                "endConditionsToCancel",
                "endConditionsToSuccess",
                "endExecutors",
            });

            EditorGUILayout.EndVertical();
        }

        private static void DrawSection(SerializedProperty step, string title, string[] fieldNames)
        {
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            foreach (var fieldName in fieldNames)
            {
                var prop = step.FindPropertyRelative(fieldName);
                EditorGUILayout.PropertyField(prop, true);
            }
            EditorGUI.indentLevel--;
        }
    }
}
