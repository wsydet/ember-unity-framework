// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System.Collections.Generic;

using UnityEditor;

using UnityEditor.SceneManagement;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>
    /// 逻辑代码实现数据的抽象基类。
    /// 子类化后提供具体的代码生成逻辑（如 CSharpLogicImplementationData）。
    /// </summary>
    public abstract class LogicImplementationData : ScriptableObject
    {
        private const string GenerateAuthorSettingKey = "EmberUIGenerateAuthorSetting";

        /// <summary>代码作者名（存储在 EditorPrefs 中）</summary>
        public static string GenerateAuthorName
        {
            get => EditorPrefs.GetString(GenerateAuthorSettingKey, System.Net.Dns.GetHostName());
            set => EditorPrefs.SetString(GenerateAuthorSettingKey, value);
        }

        #region 内部参数

        [SerializeField]
        protected string codePath;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>代码文件后缀名（如 ".cs"）</summary>
        public abstract string CodeFileExtension { get; }

        /// <summary>获取代码文件完整路径</summary>
        public virtual string GetCodeFilePath(string path)
        {
            return $"{codePath}/{path}{CodeFileExtension}";
        }

        /// <summary>从路径提取类名</summary>
        public virtual string GetCodeClassName(string path)
        {
            int idx = path.LastIndexOfAny(new[] { '\\', '/' });
            string name = idx >= 0 ? path.Substring(idx + 1) : path;
            int dot = name.LastIndexOf('.');
            return dot >= 0 ? name.Substring(0, dot) : name;
        }

        /// <summary>是否需要重新生成</summary>
        public virtual bool IsNeedRegenerate(EmberUIBinding binding)
        {
            return true;
        }

        /// <summary>是否可以正常生成代码</summary>
        public virtual bool CanGenerate(EmberUIBinding binding)
        {
            return !string.IsNullOrEmpty(binding.ClassName)
                && (!binding.IsPage || !string.IsNullOrEmpty(binding.PageName));
        }

        /// <summary>是否可以生成到剪贴板</summary>
        public virtual bool CanGenerateForNoGen(EmberUIBinding binding)
        {
            return true;
        }

        /// <summary>执行代码生成</summary>
        public abstract void GenerateCode(EmberUIBinding binding, string baseClsName, EmberUIBinding.BindingEntry[] declaredFields);

        /// <summary>执行剪贴板代码生成（noCodeGen 模式）</summary>
        public abstract void GenerateCodeForNoGen(EmberUIBinding binding, string className);

        /// <summary>绘制 noCodeGen 模式的设置 UI</summary>
        public virtual void DrawNoCodeGenerationSettings(EmberUIBinding binding)
        {
            DrawAutoCollect(binding);
        }

        /// <summary>绘制代码生成设置 UI</summary>
        public virtual void DrawGenerateSettings(EmberUIBinding binding)
        {
            DrawAutoCollect(binding);

            if (string.IsNullOrEmpty(binding.ClassName))
            {
                EditorGUILayout.HelpBox("请输入逻辑类名", MessageType.Error);
            }
            else
            {
                if (binding.IsPage && string.IsNullOrEmpty(binding.PageName))
                {
                    EditorGUILayout.HelpBox("请输入页面名", MessageType.Error);
                }
            }
        }

        /// <summary>将 UI 节点的原始名称转为代码用的变量名</summary>
        public virtual string GetNameForCode(string name)
        {
            name = name.Replace(" ", "_");
            if (name.StartsWith("m_"))
                return $"m{name.Substring(2)}";
            return name;
        }

        /// <summary>生成唯一的变量名</summary>
        public virtual string GetNameForCode(string name, HashSet<string> defined)
        {
            int idx = 1;
            string cur = GetNameForCode(name);
            while (defined.Contains(cur))
                cur = GetNameForCode($"{name}_{idx++}");
            defined.Add(cur);
            return cur;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        /// <summary>判断节点名是否适合作为绑定变量名（以 m_ 或 mXxx 开头）</summary>
        protected virtual bool IsNameSuitable(string name)
        {
            return true;
        }

        /// <summary>绘制自动收集按钮</summary>
        protected virtual void DrawAutoCollect(EmberUIBinding binding)
        {
            EditorGUILayout.Separator();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("自动收集绑定", GUILayout.Width(100)))
            {
                AutoCollect(binding);
            }
            if (GUILayout.Button("清除并重新收集绑定", GUILayout.Width(130)))
            {
                ClearAll(binding);
                AutoCollect(binding);
            }
            if (GUILayout.Button("清除所有绑定", GUILayout.Width(100)))
            {
                ClearAll(binding);
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Separator();
        }

        /// <summary>自动收集子节点绑定</summary>
        protected void AutoCollect(EmberUIBinding binding)
        {
            Dictionary<GameObject, GameObject> defined = new Dictionary<GameObject, GameObject>();
            HashSet<string> definedNames = new HashSet<string>();
            EmberUIBindingEditorUtility.GatherBindingDefinitions(binding, defined);
            SerializedObject so = new SerializedObject(binding);
            SerializedProperty sp = so.FindProperty("bindings");
            var curTrans = binding.transform;
            GatherBindings(binding, sp, defined, definedNames, curTrans);
            if (so.hasModifiedProperties)
                so.ApplyModifiedProperties();
            so.Dispose();
        }

        /// <summary>清除所有绑定</summary>
        protected void ClearAll(EmberUIBinding binding)
        {
            SerializedObject so = new SerializedObject(binding);
            SerializedProperty sp = so.FindProperty("bindings");
            sp.ClearArray();
            if (so.hasModifiedProperties)
                so.ApplyModifiedProperties();
            so.Dispose();
        }

        /// <summary>递归收集子节点</summary>
        protected void GatherBindings(
            EmberUIBinding binding,
            SerializedProperty sp,
            Dictionary<GameObject, GameObject> defined,
            HashSet<string> definedNames,
            Transform trans)
        {
            var cnt = trans.childCount;
            for (int i = 0; i < cnt; i++)
            {
                var child = trans.GetChild(i);
                var childGO = child.gameObject;
                bool hasChildBinding = childGO.GetComponent<EmberUIBinding>() != null;
                var childName = child.name;

                if (!defined.ContainsKey(childGO) && IsNameSuitable(childName))
                {
                    var idx = sp.arraySize;
                    sp.InsertArrayElementAtIndex(idx);
                    var childSP = sp.GetArrayElementAtIndex(idx);
                    var nameSP = childSP.FindPropertyRelative("Name");
                    var goSP = childSP.FindPropertyRelative("GameObject");
                    var typeSP = childSP.FindPropertyRelative("Type");
                    var cnSP = childSP.FindPropertyRelative("ClassName");

                    nameSP.stringValue = GetNameForCode(childName, definedNames);
                    goSP.objectReferenceValue = childGO;
                    EmberUIBindingEditorUtility.AutoSelectByObject(childGO, typeSP, cnSP);
                }
                if (!hasChildBinding)
                {
                    GatherBindings(binding, sp, defined, definedNames, child);
                }
            }
        }

        /// <summary>获取 binding 所在的 prefab 名称</summary>
        public static bool TryGetPrefabName(EmberUIBinding binding, out string prefabName)
        {
            prefabName = System.IO.Path.GetFileName(
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(binding));
            var go = PrefabUtility.GetNearestPrefabInstanceRoot(binding);
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (go && stage && go == stage.prefabContentsRoot)
            {
                if (stage != null && stage.IsPartOfPrefabContents(binding.gameObject))
                {
                    prefabName = System.IO.Path.GetFileName(stage.assetPath);
                }
            }
            else if (go != binding.gameObject)
            {
                if (stage != null && stage.IsPartOfPrefabContents(binding.gameObject))
                {
                    prefabName = System.IO.Path.GetFileName(stage.assetPath);
                }
                else if (string.IsNullOrEmpty(prefabName))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion
    }

    // --------------------------------------------------------

    /// <summary>
    /// LogicImplementationData 的自定义 Inspector。
    /// </summary>
    [CustomEditor(typeof(LogicImplementationData), true)]
    public class LogicImplementationDataEditor : UnityEditor.Editor
    {
        SerializedProperty codePath;

        protected virtual void OnEnable()
        {
            codePath = serializedObject.FindProperty("codePath");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(codePath, new GUIContent("逻辑代码路径"));
            if (string.IsNullOrEmpty(codePath.stringValue) || !System.IO.Directory.Exists(codePath.stringValue))
            {
                EditorGUILayout.HelpBox("请输入正确的代码路径", MessageType.Error);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
