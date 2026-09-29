using System;
using System.Linq;
using Ember.UIExtension;
using Ember.UIExtension.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Automation
{
    /// <summary>技能使用的 Editor 调用适配器；调用现有生成器，不自行渲染或写 Binding。</summary>
    internal static class EmberEuiSkillAdapter
    {
        #region 外部方法
        internal static void Validate(EUIBinding binding)
        {
            var result = EUIBindingEditorUtility.ValidateBinding(binding);
            if (result.HasError)
                throw new InvalidOperationException(string.Join("\n", result.Issues
                    .Where(issue => issue.Severity == EUIBindingIssueSeverity.Error).Select(issue => issue.Message)));
            foreach (var entry in binding.Bindings)
            {
                if (!entry.GameObject || !entry.GameObject.transform.IsChildOf(binding.transform))
                    throw new InvalidOperationException("绑定不在当前根内：" + entry.Name);
                Type type = null;
                switch (entry.Type)
                {
                    case EUIBinding.WidgetTypes.Text: type = typeof(TMPro.TMP_Text); break;
                    case EUIBinding.WidgetTypes.Button: type = typeof(UnityEngine.UI.Button); break;
                    case EUIBinding.WidgetTypes.CanvasGroup: type = typeof(CanvasGroup); break;
                    case EUIBinding.WidgetTypes.ScrollRect: type = typeof(UnityEngine.UI.ScrollRect); break;
                    case EUIBinding.WidgetTypes.Extension:
                        type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(entry.ClassName)).FirstOrDefault(t => t != null);
                        if (type == null) throw new InvalidOperationException("Extension 类型不存在：" + entry.ClassName);
                        break;
                }
                if (type != null && !entry.GameObject.GetComponent(type))
                    throw new InvalidOperationException("绑定组件不存在：" + entry.Name + " / " + type.FullName);
            }
        }

        internal static EUIBindingSnapshot Regenerate(GameObject savedPrefab)
        {
            var binding = savedPrefab.GetComponent<EUIBinding>();
            Validate(binding);
            if (!EUIBindingCodeGenUtility.TryRegenerateCode(binding, out string error))
                throw new InvalidOperationException("UI 中心生成失败：" + error);
            return EUIBindingEditorUtility.GetBindingSnapshot(binding);
        }

        internal static void AppendRootBinding(EUIBinding binding, string name, RectTransform target)
        {
            if (!target || !target.IsChildOf(binding.transform)) throw new InvalidOperationException("宿主引用必须在当前 Page 内。");
            foreach (var entry in binding.Bindings)
                if (entry.Name == name) throw new InvalidOperationException("宿主引用已存在：" + name);
            using (var serialized = new SerializedObject(binding))
            {
                var entries = serialized.FindProperty("bindings");
                int index = entries.arraySize;
                entries.InsertArrayElementAtIndex(index);
                var entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("Name").stringValue = name;
                entry.FindPropertyRelative("GameObject").objectReferenceValue = target.gameObject;
                entry.FindPropertyRelative("Type").intValue = (int)EUIBinding.WidgetTypes.Extension;
                entry.FindPropertyRelative("ClassName").stringValue = typeof(RectTransform).FullName;
                entry.FindPropertyRelative("IsFramework").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(binding);
        }
        #endregion
    }
}
