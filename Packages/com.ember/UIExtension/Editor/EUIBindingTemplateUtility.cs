// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using UnityEditor;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// EUIBinding 模板操作工具（保存 / 加载 / 复制 / 粘贴）。
    /// 在静态构造函数中注册到 EUIBinding 的静态委托上，
    /// 使得 Runtime 程序集中的 [Button] 方法可以委托到 Editor 程序集执行。
    /// </summary>
    [InitializeOnLoad]
    public static class EUIBindingTemplateUtility
    {
        #region 内部参数

        private const string TAG = Ember.Basic.LogTags.EmberUI;

        /// <summary>内存剪贴板 —— 复制的模板快照（不保存到磁盘）</summary>
        private static EUIBindingTemplate _savedTemplate;

        #endregion

        // --------------------------------------------------------

        #region 生命周期（初始化）

        static EUIBindingTemplateUtility()
        {
            EUIBinding.OnSaveAsTemplate = HandleSaveAsTemplate;
            EUIBinding.OnLoadTemplate = HandleLoadTemplate;
            EUIBinding.OnCopyTemplate = HandleCopyTemplate;
            EUIBinding.OnPasteTemplate = HandlePasteTemplate;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 模板操作

        /// <summary>保存当前 binding 配置为 ScriptableObject 模板</summary>
        private static void HandleSaveAsTemplate(EUIBinding binding)
        {
            if (!binding) return;

            var defaultDir = "Assets";
            var settings = EUIBindingSettingData.GetOrCreateSettings();
            if (settings.DefaultBindingTemplatePath != null)
                defaultDir = AssetDatabase.GetAssetPath(settings.DefaultBindingTemplatePath);

            var fileName = string.IsNullOrEmpty(binding.ClassName)
                ? binding.gameObject.name
                : binding.ClassName;
            var path = EditorUtility.SaveFilePanel("保存模板", defaultDir, fileName, "asset");
            if (string.IsNullOrEmpty(path)) return;

            // 转为 Assets-relative 路径
            path = "Assets" + path.Replace(Application.dataPath, "");

            var template = ScriptableObject.CreateInstance<EUIBindingTemplate>();
            template.CopyFromUIBinding(binding);

            AssetDatabase.CreateAsset(template, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Ember.Basic.EmberDebug.Log(TAG, $"模板已保存至：{path}");
        }

        /// <summary>通过文件对话框选择模板并加载</summary>
        private static void HandleLoadTemplate(EUIBinding binding)
        {
            if (!binding) return;

            var path = EditorUtility.OpenFilePanel("加载模板", "Assets", "asset");
            if (string.IsNullOrEmpty(path)) return;

            // 转为 Assets-relative 路径
            path = "Assets" + path.Replace(Application.dataPath, "");

            var template = AssetDatabase.LoadAssetAtPath<EUIBindingTemplate>(path);
            if (template == null)
            {
                Ember.Basic.EmberDebug.LogWarning(TAG, $"未找到有效的模板文件：{path}");
                return;
            }

            if (!EditorUtility.DisplayDialog("加载模板",
                $"确认加载模板 \"{template.name}\"（{template.Bindings?.Length ?? 0} 个绑定）？\n当前配置将被覆盖。",
                "确认加载", "取消"))
                return;

            ApplyTemplate(binding, template);
            Ember.Basic.EmberDebug.Log(TAG, $"已从模板 \"{template.name}\" 加载配置");
        }

        /// <summary>复制当前 binding 配置到内存剪贴板</summary>
        private static void HandleCopyTemplate(EUIBinding binding)
        {
            if (!binding) return;

            if (_savedTemplate != null)
                Object.DestroyImmediate(_savedTemplate);

            _savedTemplate = ScriptableObject.CreateInstance<EUIBindingTemplate>();
            _savedTemplate.CopyFromUIBinding(binding);
            EUIBinding.HasCopiedTemplate = true;

            Ember.Basic.EmberDebug.Log(TAG,
                $"已复制模板（{_savedTemplate.Bindings?.Length ?? 0} 个绑定）");
        }

        /// <summary>粘贴内存剪贴板中的模板配置到当前 binding</summary>
        private static void HandlePasteTemplate(EUIBinding binding)
        {
            if (!binding) return;

            if (_savedTemplate == null)
            {
                Ember.Basic.EmberDebug.LogWarning(TAG, "没有已复制的模板数据");
                return;
            }

            var count = _savedTemplate.Bindings?.Length ?? 0;
            var className = string.IsNullOrEmpty(_savedTemplate.ClassName)
                ? "(未命名)" : _savedTemplate.ClassName;

            if (!EditorUtility.DisplayDialog("粘贴模板",
                $"确认将模板 \"{className}\"（{count} 个绑定）粘贴到当前配置？\n当前配置将被覆盖。",
                "确认粘贴", "取消"))
                return;

            ApplyTemplate(binding, _savedTemplate);
            Ember.Basic.EmberDebug.Log(TAG,
                $"已粘贴模板 \"{className}\"（{count} 个绑定）");
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 模板应用

        /// <summary>
        /// 将模板数据应用到目标 binding。
        /// 使用 SerializedObject 直接设置序列化字段，支持 Undo。
        /// </summary>
        private static void ApplyTemplate(EUIBinding binding, EUIBindingTemplate template)
        {
            if (!binding || !template) return;

            Undo.RecordObject(binding, "应用 UIBinding 模板");

            using (var so = new SerializedObject(binding))
            {
                // ── 页面配置 ──
                so.FindProperty("isPage").boolValue = template.IsPage;
                so.FindProperty("pageName").stringValue = template.PageName ?? string.Empty;
                so.FindProperty("noCodeGen").boolValue = template.NoCodeGeneration;
                so.FindProperty("useUIUpdate").boolValue = template.UseUIUpdate;
                so.FindProperty("generateAutoCreateClickableMaskOverride").boolValue =
                    template.GenerateAutoCreateClickableMaskOverride;
                so.FindProperty("generateOnClickMaskOverride").boolValue = template.GenerateOnClickMaskOverride;

                // ── 输出设置 ──
                so.FindProperty("classPath").stringValue = template.ClassPath ?? string.Empty;
                so.FindProperty("className").stringValue = template.ClassName ?? string.Empty;

                // ── 自身控件 ──
                so.FindProperty("selfWidgetType").enumValueIndex =
                    template.SelfWidgetType > EUIBinding.WidgetTypes.End
                        ? (int)EUIBinding.WidgetTypes.End + 1
                        : (int)template.SelfWidgetType;
                so.FindProperty("selfWidgetClassName").stringValue =
                    template.SelfWidgetClassName ?? string.Empty;

                // ── 绑定列表 — 通过路径恢复 GameObject 引用 ──
                var templateBindings = template.Bindings;
                var bindingsProp = so.FindProperty("bindings");
                bindingsProp.ClearArray();

                if (templateBindings != null && templateBindings.Length > 0)
                {
                    for (int i = 0; i < templateBindings.Length; i++)
                    {
                        var tpl = templateBindings[i];
                        bindingsProp.InsertArrayElementAtIndex(i);
                        var elem = bindingsProp.GetArrayElementAtIndex(i);

                        elem.FindPropertyRelative("Name").stringValue = tpl.Name ?? string.Empty;
                        elem.FindPropertyRelative("Type").enumValueIndex =
                            tpl.Type > EUIBinding.WidgetTypes.End
                                ? (int)EUIBinding.WidgetTypes.End + 1
                                : (int)tpl.Type;
                        elem.FindPropertyRelative("ClassName").stringValue = tpl.ClassName ?? string.Empty;

                        // 通过相对路径恢复 GameObject 引用
                        if (!string.IsNullOrEmpty(tpl.GameObjectPath))
                        {
                            var target = binding.transform.Find(tpl.GameObjectPath);
                            elem.FindPropertyRelative("GameObject").objectReferenceValue =
                                target != null ? target.gameObject : null;
                        }
                        else
                        {
                            elem.FindPropertyRelative("GameObject").objectReferenceValue = null;
                        }
                    }
                }

                so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(binding);
        }

        #endregion
    }
}
