////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using System.Collections;
////using System.Collections.Generic;
////using System.IO;
////using UnityEngine;
////using UnityEditor;
////using UnityEditor.Experimental.SceneManagement;
////
////using Cottle;
////namespace Burner.UIExtension
////{
////    public class CSharpLogicImplementationData : LogicImplementationData
////    {
////        [SerializeField]
////        private string namespaceName;
////        [SerializeField]
////        private string baseClassName = "GameUILogic";
////        [SerializeField]
////        private string pageDefFile;
////        [SerializeField]
////        private DefaultAsset codeTemplate;
////        [SerializeField]
////        private DefaultAsset codeTemplateForNoGen;
////        [SerializeField]
////        private DefaultAsset bindingCodeTemplate;
////        [SerializeField]
////        private DefaultAsset pageDefTemplate;
////        public override string CodeFileExtension => ".cs";
////        public string PageDefFile => pageDefFile;
////
////        [MenuItem("Assets/Create/Burner/UI/C#实现数据")]
////        public static void CreateAsset()
////        {
////            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
////            if (!string.IsNullOrEmpty(path))
////            {
////                if (System.IO.File.Exists(path))
////                    path = System.IO.Path.GetDirectoryName(path);
////                var instance = ScriptableObject.CreateInstance<CSharpLogicImplementationData>();
////                instance.codeTemplate = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Packages/com.burner.uiextension/Editor/Settings/CSharpCodeTemplate.tpl");
////                instance.codeTemplateForNoGen = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Packages/com.burner.uiextension/Editor/Settings/CSharpCodeForNoGenTemplate.tpl");
////                instance.bindingCodeTemplate = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Packages/com.burner.uiextension/Editor/Settings/CSharpBindingTemplate.tpl");
////                instance.pageDefTemplate = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Packages/com.burner.uiextension/Editor/Settings/CSharpPageDefTemplate.tpl");
////                AssetDatabase.CreateAsset(instance, AssetDatabase.GenerateUniqueAssetPath(path + "/C#实现.asset"));
////            }
////        }
////        public override bool CanGenerate(GameUIBinding binding)
////        {
////            return base.CanGenerate(binding) && !string.IsNullOrEmpty(pageDefFile) && codeTemplate && pageDefTemplate && bindingCodeTemplate && !string.IsNullOrEmpty(namespaceName);
////        }
////
////        public override bool CanGenerateForNoGen(GameUIBinding binding)
////        {
////            return codeTemplateForNoGen;
////        }
////
////        public override string GetNameForCode(string name)
////        {
////            name = name.Replace(" ", "_");
////            if (name.StartsWith("m_"))
////                return $"{name.Substring(2)}";
////            else
////                return name;
////        }
////
////        public override void GenerateCodeForNoGen(GameUIBinding binding, string className)
////        {
////            using (var sr = new System.IO.StreamReader(AssetDatabase.GetAssetPath(codeTemplateForNoGen), new System.Text.UTF8Encoding(false)))
////            {
////                var doc = Document.CreateDefault(sr).DocumentOrThrow;
////                var ctx = Context.CreateBuiltin(new Dictionary<Value, Value>
////                {
////                    ["fields"] = GetFields(binding, binding.Bindings),
////                    ["class_name"] = className,
////                    ["author_name"] = LogicImplementationData.GenerateAuthorName,
////                });
////                GUIUtility.systemCopyBuffer = doc.Render(ctx);
////            }
////            EditorUtility.DisplayDialog("OK", $"代码已复制至剪贴板", "确认");
////        }
////
////        public override void GenerateCode(GameUIBinding binding, string baseClsName, GameUIBinding.BindingEntry[] declearedFields)
////        {
////            if (!GeneratePageDefinition(binding))
////                return;
////            TryGetPrefabName(binding, out var prefabName);
////            prefabName = Path.GetFileNameWithoutExtension(prefabName);
////            baseClsName = !string.IsNullOrEmpty(baseClsName) ? baseClsName : this.baseClassName;
////            declearedFields = declearedFields != null ? declearedFields : binding.Bindings;
////            string path = GetCodeFilePath(binding.ClassPath);
////            var now = System.DateTime.Now;
////            if (!System.IO.File.Exists(path))
////            {
////                string folder = System.IO.Path.GetDirectoryName(path);
////                if (!System.IO.Directory.Exists(folder))
////                    System.IO.Directory.CreateDirectory(folder);
////                using (var sr = new System.IO.StreamReader(AssetDatabase.GetAssetPath(codeTemplate), new System.Text.UTF8Encoding(false)))
////                {
////                    var doc = Document.CreateDefault(sr).DocumentOrThrow;
////                    var ctx = Context.CreateBuiltin(new Dictionary<Value, Value>
////                    {
////                        ["fields"] = GetFields(binding, declearedFields),
////                        ["isPage"] = binding.IsPage ? 1 : 0,
////                        ["namespace_name"] = namespaceName,
////                        ["page_name"] = binding.PageName,
////                        ["prefab_name"] = prefabName,
////                        ["class_name"] = binding.ClassName,
////                        ["base_class_name"] = baseClsName,
////                        ["author_name"] = LogicImplementationData.GenerateAuthorName,
////                        ["create_date"] = $"{now.ToString()}",
////                    });
////                    using (var sw = new System.IO.StreamWriter(path, false, new System.Text.UTF8Encoding(false)))
////                        doc.Render(ctx, sw);
////                }
////            }
////
////            path = GetBindingCodeFilePath(binding.ClassPath);
////            using (var sr = new System.IO.StreamReader(AssetDatabase.GetAssetPath(bindingCodeTemplate), new System.Text.UTF8Encoding(false)))
////            {
////                var doc = Document.CreateDefault(sr).DocumentOrThrow;
////                var ctx = Context.CreateBuiltin(new Dictionary<Value, Value>
////                {
////                    ["fields"] = GetFields(binding, declearedFields),
////                    ["isPage"] = binding.IsPage ? 1 : 0,
////                    ["namespace_name"] = namespaceName,
////                    ["page_name"] = binding.PageName,
////                    ["prefab_name"] = prefabName,
////                    ["class_name"] = binding.ClassName,
////                    ["base_class_name"] = baseClsName,
////                    ["author_name"] = LogicImplementationData.GenerateAuthorName,
////                    ["create_date"] = $"{now.ToString()}",
////                });
////                using (var sw = new System.IO.StreamWriter(path, false, new System.Text.UTF8Encoding(false)))
////                    doc.Render(ctx, sw);
////            }
////            EditorUtility.DisplayDialog("OK", $"代码生成成功", "确认");
////            AssetDatabase.Refresh();
////            //Cottle.Document.CreateDefault()
////        }
////
////        public string GetBindingCodeFilePath(string path)
////        {
////            return $"{codePath}/{path}.Binding{CodeFileExtension}";
////        }
////
////        public bool GenerateOrUpdatePageDefinition(GameUIBinding binding)
////        {
////            return GeneratePageDefinition(binding);
////        }
////
////        Value[] GetFields(GameUIBinding binding, GameUIBinding.BindingEntry[] entries)
////        {
////            var cnt = entries.Length;
////            Value[] fields = new Value[cnt];
////            for (int i = 0; i < cnt; i++)
////            {
////                var entry = entries[i];
////                fields[i] = new Dictionary<Value, Value> {
////                    ["name"] = entry.Name,
////                    ["type"] = GetTypeName(entry),
////                    ["comment"] = entry.GameObject.transform.GetFullPathName(binding.transform)
////                };
////            }
////            return fields;
////        }
////
////        string GetTypeName(GameUIBinding.BindingEntry entry)
////        {
////            switch (entry.Type)
////            {
////                case GameUIBinding.WidgetTypes.Button:
////                    return "GameButton";
////                case GameUIBinding.WidgetTypes.Component:
////                    return "GameUIComponent";
////                case GameUIBinding.WidgetTypes.Image:
////                    return "GameImage";
////                case GameUIBinding.WidgetTypes.InputField:
////                    return "GameInputField";
////                case GameUIBinding.WidgetTypes.ProgressBar:
////                    return "GameProgressBar";
////                case GameUIBinding.WidgetTypes.Text:
////                    return "GameText";
////                case GameUIBinding.WidgetTypes.Toggle:
////                    return "GameToggle";
////                case GameUIBinding.WidgetTypes.UIContainer:
////                    {
////                        /*UIContainer container = entry.GameObject.GetComponent<UIContainer>();
////                        if (container.TemplateType == GameUIBinding.WidgetTypes.UILogic)
////                        {
////                            return container ? $"{container.TemplateClassName}" : "GameUIContainer";
////                        }
////                        else*/
////                        return "GameUIContainer";
////                    }
////                case GameUIBinding.WidgetTypes.ToggleGroup:
////                    return "GameToggleGroup";
////                case GameUIBinding.WidgetTypes.ScrollRect:
////                    return "GameScrollRect";
////                case GameUIBinding.WidgetTypes.RawImage:
////                    return "GameRawImage";
////                case GameUIBinding.WidgetTypes.Canvas:
////                    return "GameCanvas";
////                case GameUIBinding.WidgetTypes.TabLoader:
////                    return "GameTabLoader";
////                case GameUIBinding.WidgetTypes.UILogic:
////                    return !string.IsNullOrEmpty(entry.ClassName) ? entry.ClassName : "GameUIComponent";
////                case GameUIBinding.WidgetTypes.Extension:
////                    {
////                        var mapping = GameUIBindingEditor.GetExtensionTypeMapping();
////                        if (mapping.TryGetValue(entry.ClassName, out var info))
////                        {
////                            return $"{info.Value.Value.FullName}";
////                        }
////                        else
////                            return "UnknownType";
////                    }
////                default:
////                    return "unknown";
////            }
////        }
////
////        void ClearInvalidDef(Dictionary<string, Dictionary<Value, Value>> defs)
////        {
////            DirectoryInfo dir = new DirectoryInfo("Assets/GameResource/Editor/Resources/Runtime/UI/");
////            var allPrefabFiles = dir.GetFiles("*.prefab", SearchOption.AllDirectories);
////            var assetsIndex = dir.FullName.IndexOf("Assets");
////
////            Dictionary<string, FileInfo> files = new Dictionary<string, FileInfo>();
////            foreach (var file in allPrefabFiles)
////            {
////                files[Path.GetFileNameWithoutExtension(file.Name).ToLower()] = file;
////            }
////
////            List<string> invalidPages = new List<string>();
////            foreach (var def in defs)
////            {
////                var defPrefabName = def.Value["info"];
////                if (!files.TryGetValue(defPrefabName.AsString, out var fileInfo))
////                {
////                    invalidPages.Add(def.Key);
////                    continue;
////                }
////
////                var assetPath = fileInfo.FullName.Substring(assetsIndex);
////                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
////                var binding = prefab.GetComponent<GameUIBinding>();
////                if (binding.PageName != def.Key)
////                {
////                    invalidPages.Add(def.Key);
////                    continue;
////                }
////            }
////
////            if (invalidPages.Count > 0)
////            {
////                foreach (var invalidPage in invalidPages)
////                {
////                    defs.Remove(invalidPage);
////                    // Debug.LogError(invalidPage);
////                }
////            }
////        }
////
////        bool GeneratePageDefinition(GameUIBinding binding)
////        {
////            if (binding.IsPage)
////            {
////                if (!TryGetPrefabName(binding, out var prefabName))
////                {
////                    EditorUtility.DisplayDialog("生成代码失败", "只有Prefab才可以被定义为Page", "确定");
////                    return false;
////                }
////
////                if (binding.PageFlags == PageFlags.None)
////                {
////                    EditorUtility.DisplayDialog("生成代码失败", "界面类型page flags定义错误!", "确定");
////                    return false;
////                }
////
////                Dictionary<string, Dictionary<Value, Value>> curDef = new Dictionary<string, Dictionary<Value, Value>>();
////
////                string folder = System.IO.Path.GetDirectoryName(pageDefFile);
////                if (!System.IO.Directory.Exists(folder))
////                    System.IO.Directory.CreateDirectory(folder);
////                int idx = 0;
////                string date = null;
////                if (System.IO.File.Exists(pageDefFile))
////                {
////                    using (var sr = new System.IO.StreamReader(pageDefFile, new System.Text.UTF8Encoding(false)))
////                    {
////                        while (!sr.EndOfStream)
////                        {
////                            var line = sr.ReadLine().Trim(' ', '\t');
////                            if (line.StartsWith("public const string"))
////                            {
////                                line = line.Substring(19);
////                                idx = line.IndexOf('=');
////                                if (idx > 0)
////                                {
////                                    string[] token = new string[2];
////                                    token[0] = line.Substring(0, idx - 1).Trim(' ', '\t');
////                                    token[1] = line.Substring(idx + 1).Trim(' ', '\t', '\"', '\"', ';');
////
////                                    if (!string.IsNullOrEmpty(token[0]))
////                                        curDef[token[0]] = (new Dictionary<Value, Value> { ["name"] = token[0], ["info"] = token[1] });
////                                }
////                            }
////                            else if (line.Contains(", PageFlags."))
////                            {
////                                line = line.Substring(2, line.Length - 5);
////                                var array = line.Split(", PageFlags.");
////                                var pageName = array[0].Trim();
////                                var flag = array[1].Trim();
////                                var pageDef = curDef[pageName];
////                                pageDef.Add("flag", flag);
////                            }
////                        }
////
////
////                        ClearInvalidDef(curDef);
////                    }
////                }
////
////                // return false;
////
////                prefabName = Path.GetFileNameWithoutExtension(prefabName);
////                curDef[binding.PageName] = (new Dictionary<Value, Value> { ["name"] = binding.PageName, ["info"] = prefabName.ToLowerInvariant(), ["flag"] = binding.PageFlags.ToString() });
////                Value[] values = new Value[curDef.Count];
////                idx = 0;
////                var keys = new List<string>(curDef.Keys);
////                keys.Sort(System.StringComparer.Ordinal);
////                foreach (var key in keys)
////                {
////                    values[idx++] = curDef[key];
////                }
////                using (var sr = new System.IO.StreamReader(AssetDatabase.GetAssetPath(pageDefTemplate), new System.Text.UTF8Encoding(false)))
////                {
////                    var doc = Document.CreateDefault(sr).DocumentOrThrow;
////                    var now = System.DateTime.Now;
////                    var ctx = Context.CreateBuiltin(new Dictionary<Value, Value>
////                    {
////                        ["pages"] = values,
////                        ["namespace_name"] = namespaceName,
////                        ["create_date"] = string.IsNullOrEmpty(date) ? $"{now.ToString()}" : date,
////                    });
////                    using (var sw = new System.IO.StreamWriter(pageDefFile, false, new System.Text.UTF8Encoding(false)))
////                        doc.Render(ctx, sw);
////                }
////                return true;
////            }
////            else
////                return true;
////        }
////
////        public override void DrawNoCodeGenerationSettings(GameUIBinding binding)
////        {
////            if (!codeTemplateForNoGen)
////            {
////                EditorGUILayout.HelpBox("请在设置界面配置剪贴板代码生成模板", MessageType.Error);
////                return;
////            }
////        }
////        public override void DrawGenerateSettings(GameUIBinding binding)
////        {
////            base.DrawGenerateSettings(binding);
////            if (string.IsNullOrEmpty(pageDefFile))
////            {
////                EditorGUILayout.HelpBox("请在设置界面配置PageDef源码路径", MessageType.Error);
////                return;
////            }
////            if (string.IsNullOrEmpty(namespaceName))
////            {
////                EditorGUILayout.HelpBox("请在设置界面配置代码生成命名空间", MessageType.Error);
////                return;
////            }
////            if (!pageDefTemplate)
////            {
////                EditorGUILayout.HelpBox("请在设置界面配置PageDef源码生成模板", MessageType.Error);
////                return;
////            }
////            if (!bindingCodeTemplate)
////            {
////                EditorGUILayout.HelpBox("请在设置界面配置绑定代码生成模板", MessageType.Error);
////                return;
////            }
////            if (!codeTemplate)
////            {
////                EditorGUILayout.HelpBox("请在设置界面配置逻辑代码生成模板", MessageType.Error);
////                return;
////            }
////        }
////    }
////
////    [CustomEditor(typeof(CSharpLogicImplementationData), true)]
////    public class CSharpLogicImplementationDataEditor : LogicImplementationDataEditor
////    {
////        SerializedProperty pageDefFile, baseClassName, codeTemplate, pageDefTemplate, bindingCodeTemplate, namespaceName, codeTemplateForNoGen;
////
////        protected override void OnEnable()
////        {
////            base.OnEnable();
////            pageDefFile = serializedObject.FindProperty("pageDefFile");
////            codeTemplate = serializedObject.FindProperty("codeTemplate");
////            pageDefTemplate = serializedObject.FindProperty("pageDefTemplate");
////            bindingCodeTemplate = serializedObject.FindProperty("bindingCodeTemplate");
////            namespaceName = serializedObject.FindProperty("namespaceName");
////            baseClassName = serializedObject.FindProperty("baseClassName");
////            codeTemplateForNoGen = serializedObject.FindProperty("codeTemplateForNoGen");
////        }
////        public override void OnInspectorGUI()
////        {
////            base.OnInspectorGUI();
////            EditorGUILayout.PropertyField(namespaceName, new GUIContent("代码生成命名空间"));
////            if (string.IsNullOrEmpty(namespaceName.stringValue))
////            {
////                EditorGUILayout.HelpBox("请输入正确的命名空间", MessageType.Error);
////            }
////            EditorGUILayout.PropertyField(baseClassName, new GUIContent("基类名"));
////            if (string.IsNullOrEmpty(baseClassName.stringValue))
////            {
////                EditorGUILayout.HelpBox("请输入正确的基类名", MessageType.Error);
////            }
////            EditorGUILayout.PropertyField(codeTemplate, new GUIContent("逻辑代码生成模板"));
////            EditorGUILayout.PropertyField(bindingCodeTemplate, new GUIContent("逻辑代码绑定文件生成模板"));
////            EditorGUILayout.PropertyField(codeTemplateForNoGen, new GUIContent("剪贴板代码生成模板"));
////            EditorGUILayout.PropertyField(pageDefFile, new GUIContent("PageDef文件路径"));
////            if (string.IsNullOrEmpty(pageDefFile.stringValue))
////            {
////                EditorGUILayout.HelpBox("请输入正确的PageDef源码文件路径", MessageType.Error);
////            }
////            EditorGUILayout.PropertyField(pageDefTemplate, new GUIContent("PageDef生成模板"));
////        }
////    }
////}
