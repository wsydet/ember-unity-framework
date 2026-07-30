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
////#if UNITY_2022_1_OR_NEWER
////using UnityEditor.SceneManagement;
////#else
////using UnityEditor.Experimental.SceneManagement;
////#endif
////namespace Burner.UIExtension
////{
////    public abstract class LogicImplementationData : ScriptableObject
////    {
////        const string GenerateAuthorSettingKey = "BurnerUIGenerateAuthorSetting";
////
////        public static string GenerateAuthorName
////        {
////            get
////            {
////                return EditorPrefs.GetString(GenerateAuthorSettingKey, System.Net.Dns.GetHostName());
////            }
////            set
////            {
////                EditorPrefs.SetString(GenerateAuthorSettingKey, value);
////            }
////        }
////        [SerializeField]
////        protected string codePath;
////
////        /// <summary>
////        /// 代码文件的后缀名，例如'.cs'
////        /// </summary>
////        public abstract string CodeFileExtension { get; }
////
////        public virtual string GetCodeFilePath(string path)
////        {
////            return $"{codePath}/{path}{CodeFileExtension}";
////        }
////
////        public virtual string GetCodeClassName(string path)
////        {
////            int idx = path.LastIndexOfAny(new[] { '\\', '/' });
////            string name = idx >= 0 ? path.Substring(idx + 1) : path;
////            int dot = name.LastIndexOf('.');
////            return dot >= 0 ? name.Substring(0, dot) : name;
////        }
////
////        public virtual bool IsNeedRegenerate(GameUIBinding binding)
////        {
////            return true;
////        }
////
////        public virtual bool CanGenerate(GameUIBinding binding)
////        {
////            return !string.IsNullOrEmpty(binding.ClassName) && (!binding.IsPage || !string.IsNullOrEmpty(binding.PageName));
////        }
////        public virtual bool CanGenerateForNoGen(GameUIBinding binding)
////        {
////            return true;
////        }
////        public abstract void GenerateCode(GameUIBinding binding, string baseClsName, GameUIBinding.BindingEntry[] declearedFields);
////
////        public abstract void GenerateCodeForNoGen(GameUIBinding binding, string className);
////
////        public virtual void DrawNoCodeGenerationSettings(GameUIBinding binding)
////        {
////            DrawAutoCollect(binding);
////        }
////
////        public virtual void DrawGenerateSettings(GameUIBinding binding)
////        {
////            DrawAutoCollect(binding);
////
////            if (string.IsNullOrEmpty(binding.ClassName))
////            {
////                EditorGUILayout.HelpBox("请输入逻辑类名", MessageType.Error);
////            }
////            else
////            {
////                if(binding.IsPage && string.IsNullOrEmpty(binding.PageName))
////                {
////                    EditorGUILayout.HelpBox("请输入页面名", MessageType.Error);
////                }
////            }
////        }
////
////
////
////        protected void AutoCollect(GameUIBinding binding)
////        {
////            Dictionary<GameObject, GameObject> defined = new Dictionary<GameObject, GameObject>();
////            HashSet<string> definedNames = new HashSet<string>();
////            GameUIBindingEditor.GatherBindingDefinitions(binding, defined);
////            SerializedObject so = new SerializedObject(binding);
////            SerializedProperty sp = so.FindProperty("bindings");
////            var curTrans = binding.transform;
////            GatherBinding(binding, sp, defined, definedNames, curTrans);
////            if (so.hasModifiedProperties)
////                so.ApplyModifiedProperties();
////            so.Dispose();
////        }
////
////        protected void ClearAll(GameUIBinding binding)
////        {
////            SerializedObject so = new SerializedObject(binding);
////            SerializedProperty sp = so.FindProperty("bindings");
////            sp.ClearArray();
////            if (so.hasModifiedProperties)
////                so.ApplyModifiedProperties();
////            so.Dispose();
////        }
////
////        protected virtual void DrawAutoCollect(GameUIBinding binding)
////        {
////            EditorGUILayout.Separator();
////            GUILayout.BeginHorizontal();
////            if (GUILayout.Button("自动收集绑定", GUILayout.Width(100)))
////            {
////                AutoCollect(binding);
////            }
////
////            if (GUILayout.Button("清除并重新收集绑定", GUILayout.Width(120)))
////            {
////                ClearAll(binding);
////                AutoCollect(binding);
////            }
////
////            if (GUILayout.Button("清除所有绑定", GUILayout.Width(100)))
////            {
////                ClearAll(binding);
////            }
////
////            GUILayout.EndHorizontal();
////            EditorGUILayout.Separator();
////        }
////
////        protected void GatherBinding(GameUIBinding binding, SerializedProperty sp, Dictionary<GameObject, GameObject> defined, HashSet<string> definedNames, Transform trans)
////        {
////            var cnt = trans.childCount;
////            for (int i = 0; i < cnt; i++)
////            {
////                var child = trans.GetChild(i);
////                var childGO = child.gameObject;
////                var childBinding = childGO.GetComponent<GameUIBinding>() || childGO.GetComponent<UIContainer>();
////                var childName = child.name;
////
////                if (!defined.ContainsKey(childGO) && IsNameSuitable(childName))
////                {
////                    var idx = sp.arraySize;
////                    sp.InsertArrayElementAtIndex(idx);
////                    var childSP = sp.GetArrayElementAtIndex(idx);
////                    var nameSP = childSP.FindPropertyRelative("Name");
////                    var goSP = childSP.FindPropertyRelative("GameObject");
////                    var typeSP = childSP.FindPropertyRelative("Type");
////                    var cnSP = childSP.FindPropertyRelative("ClassName");
////
////                    nameSP.stringValue = GetNameForCode(childName, definedNames);
////                    goSP.objectReferenceValue = childGO;
////                    GameUIBindingEditor.AutoSelectByObject(childGO, typeSP, cnSP);
////
////                }
////                if (!childBinding)
////                {
////                    GatherBinding(binding, sp, defined, definedNames, child);
////                }
////            }
////        }
////
////        public virtual string GetNameForCode(string name)
////        {
////            name = name.Replace(" ", "_");
////            if (name.StartsWith("m_"))
////                return $"m{name.Substring(2)}";
////            else
////                return name;
////        }
////
////        public virtual string GetNameForCode(string name, HashSet<string> defined)
////        {
////            int idx = 1;
////            string cur = GetNameForCode(name);
////            while (defined.Contains(cur))
////                cur = GetNameForCode($"{name}_{idx++}");
////            defined.Add(cur);
////            return cur;
////        }
////
////        protected virtual bool IsNameSuitable(string name)
////        {
////            if (name.StartsWith("m_") || (name.StartsWith("m") && name.Length > 1 && char.IsUpper(name[1])))
////            {
////                return true;
////            }
////            else
////                return false;
////        }
////
////        public static bool TryGetPrefabName(GameUIBinding binding, out string prefabName)
////        {
////            prefabName = System.IO.Path.GetFileName(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(binding));
////            var go = PrefabUtility.GetNearestPrefabInstanceRoot(binding);
////            var stage = PrefabStageUtility.GetCurrentPrefabStage();
////            if (go && stage && go == stage.prefabContentsRoot)
////            {
////                if (stage != null && stage.IsPartOfPrefabContents(binding.gameObject))
////                {
////                    prefabName = System.IO.Path.GetFileName(stage.assetPath);
////                }
////            }
////            else if (go != binding.gameObject)
////            {
////                if (stage != null && stage.IsPartOfPrefabContents(binding.gameObject))
////                {
////                    prefabName = System.IO.Path.GetFileName(stage.assetPath);
////                }
////                else if (string.IsNullOrEmpty(prefabName))
////                {
////                    return false;
////                }
////            }
////            return true;
////        }
////    }
////
////    [CustomEditor(typeof(LogicImplementationData), true)]
////    public class LogicImplementationDataEditor : UnityEditor.Editor
////    {
////        SerializedProperty codePath;
////        protected virtual void OnEnable()
////        {
////            codePath = serializedObject.FindProperty("codePath");
////        }
////
////        public override void OnInspectorGUI()
////        {
////            EditorGUILayout.PropertyField(codePath, new GUIContent("逻辑代码路径"));
////            if(string.IsNullOrEmpty(codePath.stringValue) || !System.IO.Directory.Exists(codePath.stringValue))
////            {
////                EditorGUILayout.HelpBox("请输入正确的代码路径", MessageType.Error);
////            }
////
////            serializedObject.ApplyModifiedProperties();
////        }
////    }
////}
