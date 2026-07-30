////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using System.Collections;
////using System.Collections.Generic;
////using UnityEngine;
////using UnityEditor;
////
////namespace Burner.UIExtension
////{
////    public class UIBindingSettingData : ScriptableObject
////    {
////        public const string k_MyCustomSettingsPath = "Assets/Editor/BurnerUIBindingSettings.asset";
////
////        [SerializeField]
////        private LogicImplementationData[] logicImplementations;
////        [SerializeField]
////        private DefaultAsset defaultBindingTemplatePath;
////        public LogicImplementationData[] LogicImplementations => logicImplementations;
////
////        public DefaultAsset DefaultBindingTemplatePath => defaultBindingTemplatePath;
////        public static UIBindingSettingData GetOrCreateSettings()
////        {
////            var settings = AssetDatabase.LoadAssetAtPath<UIBindingSettingData>(k_MyCustomSettingsPath);
////            if (settings == null)
////            {
////                settings = ScriptableObject.CreateInstance<UIBindingSettingData>();
////                AssetDatabase.CreateAsset(settings, k_MyCustomSettingsPath);
////                AssetDatabase.SaveAssets();
////            }
////            return settings;
////        }
////
////        internal static SerializedObject GetSerializedSettings()
////        {
////            return new SerializedObject(GetOrCreateSettings());
////        }
////    }
////
////    // Register a SettingsProvider using IMGUI for the drawing framework:
////    static class UIBindingSettingDataIMGUIRegister
////    {
////        [SettingsProvider]
////        public static SettingsProvider CreateUIBindingSettingDataProvider()
////        {
////            // First parameter is the path in the Settings window.
////            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
////            var provider = new SettingsProvider("Project/BurnerUIBindingSetting", SettingsScope.Project)
////            {
////                // By default the last token of the path is used as display name if no label is provided.
////                label = "Burner UI",
////                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
////                guiHandler = (searchContext) =>
////                {
////                    var authorName = LogicImplementationData.GenerateAuthorName;
////                    var newName = EditorGUILayout.TextField("代码作者名", authorName);
////                    if (newName != authorName)
////                        LogicImplementationData.GenerateAuthorName = newName;
////                    var settings = UIBindingSettingData.GetSerializedSettings();
////
////                    var logicImplementations = settings.FindProperty("logicImplementations");
////                    var templatePath = settings.FindProperty("defaultBindingTemplatePath");
////
////                    EditorGUILayout.PropertyField(templatePath, new GUIContent("默认UI绑定模板保存路径"));
////
////                    EditorGUILayout.PropertyField(logicImplementations, new GUIContent("逻辑实现数据"), true);
////                    if (logicImplementations.arraySize <= 0)
////                    {
////                        EditorGUILayout.HelpBox("请添加至少一种逻辑代码实现数据", MessageType.Error);
////                    }
////                    settings.ApplyModifiedProperties();
////                },
////
////                // Populate the search keywords to enable smart search filtering and label highlighting:
////                keywords = new HashSet<string>(new[] { "lua", "c#", "path" })
////            };
////
////            return provider;
////        }
////    }
////}
