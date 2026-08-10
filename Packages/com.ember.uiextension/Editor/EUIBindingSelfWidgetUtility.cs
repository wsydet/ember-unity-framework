// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System.Collections.Generic;

using Ember.Basic;

using UnityEditor;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// EUIBinding 自身控件类型工具（可用类型提示、自动识别）。
    /// </summary>
    [InitializeOnLoad]
    public static class EUIBindingSelfWidgetUtility
    {
        static EUIBindingSelfWidgetUtility()
        {
            EUIBinding.OnAutoDetectSelfWidgetType = HandleAutoDetect;
            EUIBinding.OnGetAvailableSelfWidgetTypes = HandleGetAvailableTypes;
        }

        private static void HandleAutoDetect(EUIBinding binding)
        {
            if (!binding) return;

            using (var so = new SerializedObject(binding))
            {
                var typeSp = so.FindProperty("selfWidgetType");
                var cnSp = so.FindProperty("selfWidgetClassName");
                EUIBindingEditorUtility.AutoSelectByObject(
                    binding.gameObject, typeSp, cnSp);
                so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(binding);
            EmberDebug.Log("EmberUI", "自身控件类型已自动识别");
        }

        private static string[] HandleGetAvailableTypes(EUIBinding binding)
        {
            if (!binding) return null;

            var go = binding.gameObject;
            var names = EUIBindingEditorUtility.GetComponentTypeNames();
            var result = new List<string>();

            foreach (var rule in EUIBindingEditorUtility.BuiltInComponentTypeRules)
            {
                if (rule.WidgetType == EUIBinding.WidgetTypes.UILogic) continue;
                if (rule.Matches(go))
                    result.Add(rule.WidgetType.ToString());
            }

            var mapping = EUIBindingEditorUtility.GetExtensionTypeMapping();
            if (mapping != null)
            {
                foreach (var kv in mapping)
                {
                    if (go.GetComponent(kv.Value.Value.Key))
                        result.Add(kv.Key);
                }
            }

            return result.Count > 0
                ? result.ToArray()
                : new[] { "Component" };
        }
    }
}
