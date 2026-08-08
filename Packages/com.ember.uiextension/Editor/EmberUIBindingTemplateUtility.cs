// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using UnityEditor;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// EmberUIBinding 模板操作工具（保存 / 加载 / 复制 / 粘贴）。
    /// 在静态构造函数中注册到 EmberUIBinding 的静态委托上，
    /// 使得 Runtime 程序集中的 [Button] 方法可以委托到 Editor 程序集执行。
    /// </summary>
    [InitializeOnLoad]
    public static class EmberUIBindingTemplateUtility
    {
        #region 内部参数

        /// <summary>内存剪贴板 —— 复制的模板快照（不保存到磁盘）</summary>
        private static EmberUIBindingTemplate _savedTemplate;

        /// <summary>ObjectPicker 控制 ID，用于检测选择器关闭</summary>
        private static int _templateSelectorControlID;

        /// <summary>当前正在等待模板加载的 binding</summary>
        private static EmberUIBinding _currentBindingForLoad;

        #endregion

        // --------------------------------------------------------

        #region 生命周期（初始化）

        static EmberUIBindingTemplateUtility()
        {
            EmberUIBinding.OnSaveAsTemplate = HandleSaveAsTemplate;
            EmberUIBinding.OnLoadTemplate = HandleLoadTemplate;
            EmberUIBinding.OnCopyTemplate = HandleCopyTemplate;
            EmberUIBinding.OnPasteTemplate = HandlePasteTemplate;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 模板操作

        /// <summary>保存当前 binding 配置为 ScriptableObject 模板</summary>
        private static void HandleSaveAsTemplate(EmberUIBinding binding)
        {
            if (!binding) return;

            var defaultDir = "Assets";
            var settings = UIBindingSettingData.GetOrCreateSettings();
            if (settings.DefaultBindingTemplatePath != null)
                defaultDir = AssetDatabase.GetAssetPath(settings.DefaultBindingTemplatePath);

            var fileName = string.IsNullOrEmpty(binding.ClassName)
                ? binding.gameObject.name
                : binding.ClassName;
            var path = EditorUtility.SaveFilePanel("保存模板", defaultDir, fileName, "asset");
            if (string.IsNullOrEmpty(path)) return;

            // 转为 Assets-relative 路径
            path = "Assets" + path.Replace(Application.dataPath, "");

            var template = ScriptableObject.CreateInstance<EmberUIBindingTemplate>();
            template.CopyFromUIBinding(binding);

            AssetDatabase.CreateAsset(template, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Ember.Basic.EmberDebug.Log("EmberUI", $"模板已保存至：{path}");
        }

        /// <summary>打开 ObjectPicker 选择模板并加载</summary>
        private static void HandleLoadTemplate(EmberUIBinding binding)
        {
            if (!binding) return;

            _currentBindingForLoad = binding;
            _templateSelectorControlID = EditorGUIUtility.GetControlID(FocusType.Passive);
            EditorGUIUtility.ShowObjectPicker<EmberUIBindingTemplate>(
                null, allowSceneObjects: false, searchFilter: string.Empty,
                controlID: _templateSelectorControlID);
            EditorApplication.update += CheckTemplatePicker;
        }

        /// <summary>轮询 ObjectPicker 是否关闭，关闭后处理选中结果</summary>
        private static void CheckTemplatePicker()
        {
            // 选择器关闭后 GetObjectPickerControlID 返回不同的值
            if (EditorGUIUtility.GetObjectPickerControlID() == _templateSelectorControlID) return;

            EditorApplication.update -= CheckTemplatePicker;

            if (!_currentBindingForLoad) return;

            var template = EditorGUIUtility.GetObjectPickerObject() as EmberUIBindingTemplate;
            if (template != null
                && EditorUtility.DisplayDialog("加载模板",
                    $"确认加载模板 \"{template.name}\"？\n当前配置将被覆盖。", "确认", "取消"))
            {
                ApplyTemplate(_currentBindingForLoad, template);
                Ember.Basic.EmberDebug.Log("EmberUI", $"已从模板 \"{template.name}\" 加载配置");
            }

            _currentBindingForLoad = null;
        }

        /// <summary>复制当前 binding 配置到内存剪贴板</summary>
        private static void HandleCopyTemplate(EmberUIBinding binding)
        {
            if (!binding) return;

            if (_savedTemplate != null)
                Object.DestroyImmediate(_savedTemplate);

            _savedTemplate = ScriptableObject.CreateInstance<EmberUIBindingTemplate>();
            _savedTemplate.CopyFromUIBinding(binding);
            EmberUIBinding.HasCopiedTemplate = true;

            Ember.Basic.EmberDebug.Log("EmberUI",
                $"已复制模板（{_savedTemplate.Bindings?.Length ?? 0} 个绑定）");
        }

        /// <summary>粘贴内存剪贴板中的模板配置到当前 binding</summary>
        private static void HandlePasteTemplate(EmberUIBinding binding)
        {
            if (!binding) return;

            if (_savedTemplate == null)
            {
                Ember.Basic.EmberDebug.LogWarning("EmberUI", "没有已复制的模板数据");
                return;
            }

            ApplyTemplate(binding, _savedTemplate);
            Ember.Basic.EmberDebug.Log("EmberUI", "已粘贴模板");
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法 —— 模板应用

        /// <summary>
        /// 将模板数据应用到目标 binding。
        /// 使用 SerializedObject 直接设置序列化字段，支持 Undo。
        /// </summary>
        private static void ApplyTemplate(EmberUIBinding binding, EmberUIBindingTemplate template)
        {
            if (!binding || !template) return;

            Undo.RecordObject(binding, "应用 UIBinding 模板");

            using (var so = new SerializedObject(binding))
            {
                // ── 页面配置 ──
                so.FindProperty("isPage").boolValue = template.IsPage;
                so.FindProperty("pageName").stringValue = template.PageName ?? string.Empty;
                so.FindProperty("noCodeGen").boolValue = template.NoCodeGeneration;

                // ── 输出设置 ──
                so.FindProperty("classPath").stringValue = template.ClassPath ?? string.Empty;
                so.FindProperty("className").stringValue = template.ClassName ?? string.Empty;

                // ── 自身控件 ──
                so.FindProperty("selfWidgetType").enumValueIndex =
                    template.SelfWidgetType > EmberUIBinding.WidgetTypes.End
                        ? (int)EmberUIBinding.WidgetTypes.End + 1
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
                            tpl.Type > EmberUIBinding.WidgetTypes.End
                                ? (int)EmberUIBinding.WidgetTypes.End + 1
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
