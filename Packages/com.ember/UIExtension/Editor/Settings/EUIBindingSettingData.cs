// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// EUI Binding 的项目级全局设置（ScriptableObject 单例）。
    /// 预设好框架/业务两条生成路径，一次配置，所有 EUIBinding 共用。
    /// </summary>
    public class EUIBindingSettingData : ScriptableObject
    {
        public const string k_SettingsPath = "Assets/Editor/Ember/EUIBindingSettings.asset";

        [SerializeField]
        private LogicImplementationData[] logicImplementations;

        [SerializeField]
        private DefaultAsset defaultBindingTemplatePath;

        [SerializeField]
        [Tooltip("框架代码根目录（只读展示，框架代码已随包发布，不可生成）")]
        private string frameworkCodeRoot = "Packages/com.ember/UI/Runtime";

        [SerializeField]
        [Tooltip("业务代码生成根目录")]
        private string businessCodeRoot = "Assets/Game/UI/Runtime";

        /// <summary>已注册的逻辑实现列表</summary>
        public LogicImplementationData[] LogicImplementations => logicImplementations;

        /// <summary>默认模板保存路径</summary>
        public DefaultAsset DefaultBindingTemplatePath => defaultBindingTemplatePath;

        /// <summary>框架代码根目录</summary>
        public string FrameworkCodeRoot => frameworkCodeRoot;

        /// <summary>业务代码根目录</summary>
        public string BusinessCodeRoot => businessCodeRoot;

        /// <summary>获取或创建设置实例</summary>
        public static EUIBindingSettingData GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<EUIBindingSettingData>(k_SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<EUIBindingSettingData>();
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
    static class EUIBindingSettingDataIMGUIRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateEUIBindingSettingDataProvider()
        {
            var provider = new SettingsProvider("Project/EUI Binding", SettingsScope.Project)
            {
                label = "EUI Binding",
                guiHandler = (searchContext) =>
                {
                    var authorName = LogicImplementationData.GenerateAuthorName;
                    var newName = EditorGUILayout.TextField("代码作者名", authorName);
                    if (newName != authorName)
                        LogicImplementationData.GenerateAuthorName = newName;

                    var settings = EUIBindingSettingData.GetSerializedSettings();
                    var frameworkCodeRoot = settings.FindProperty("frameworkCodeRoot");
                    var businessCodeRoot = settings.FindProperty("businessCodeRoot");
                    var logicImplementations = settings.FindProperty("logicImplementations");
                    var templatePath = settings.FindProperty("defaultBindingTemplatePath");

                    EditorGUILayout.PropertyField(templatePath, new GUIContent("默认模板保存路径"));
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("代码生成路径", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(frameworkCodeRoot, new GUIContent("框架根目录"));
                    EditorGUILayout.PropertyField(businessCodeRoot, new GUIContent("业务根目录"));
                    EditorGUILayout.Space();
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
