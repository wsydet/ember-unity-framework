// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// Ember UI Binding 的项目级设置（ScriptableObject 单例）。
    /// 存储逻辑实现数据、默认模板路径等。
    /// </summary>
    public class UIBindingSettingData : ScriptableObject
    {
        public const string k_SettingsPath = "Assets/Ember/UI/Editor/EmberUIBindingSettings.asset";

        [SerializeField]
        private LogicImplementationData[] logicImplementations;

        [SerializeField]
        private DefaultAsset defaultBindingTemplatePath;

        /// <summary>已注册的逻辑实现列表</summary>
        public LogicImplementationData[] LogicImplementations => logicImplementations;

        /// <summary>默认模板保存路径</summary>
        public DefaultAsset DefaultBindingTemplatePath => defaultBindingTemplatePath;

        /// <summary>获取或创建设置实例</summary>
        public static UIBindingSettingData GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<UIBindingSettingData>(k_SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<UIBindingSettingData>();
                AssetDatabase.CreateAsset(settings, k_SettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        /// <summary>获取 SerializedObject 形式的设置</summary>
        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }

    // --------------------------------------------------------

    /// <summary>
    /// Project Settings 窗口注册。
    /// </summary>
    static class UIBindingSettingDataIMGUIRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateUIBindingSettingDataProvider()
        {
            var provider = new SettingsProvider("Project/Ember UI Binding", SettingsScope.Project)
            {
                label = "Ember UI Binding",
                guiHandler = (searchContext) =>
                {
                    var authorName = LogicImplementationData.GenerateAuthorName;
                    var newName = EditorGUILayout.TextField("代码作者名", authorName);
                    if (newName != authorName)
                        LogicImplementationData.GenerateAuthorName = newName;

                    var settings = UIBindingSettingData.GetSerializedSettings();
                    var logicImplementations = settings.FindProperty("logicImplementations");
                    var templatePath = settings.FindProperty("defaultBindingTemplatePath");

                    EditorGUILayout.PropertyField(templatePath, new GUIContent("默认模板保存路径"));
                    EditorGUILayout.PropertyField(logicImplementations, new GUIContent("逻辑实现数据"), true);

                    if (logicImplementations.arraySize <= 0)
                    {
                        EditorGUILayout.HelpBox("请添加至少一种逻辑代码实现数据", MessageType.Error);
                    }
                    settings.ApplyModifiedProperties();
                },
                keywords = new HashSet<string>(new[] { "UI", "Binding", "Ember", "Code Gen" })
            };
            return provider;
        }
    }
}
