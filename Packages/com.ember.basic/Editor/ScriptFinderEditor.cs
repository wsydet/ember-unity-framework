// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ember.Basic;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 脚本查找工具 —— 拖入 .cs 文件或输入类名，找到场景中所有挂了该脚本的 GameObject。
    /// Unity Hierarchy 搜索栏支持 t:ClassName，但本工具提供更直观的交互。
    /// </summary>
    public class ScriptFinderEditor : EmberEditorWindow
    {
        private const string TAG = LogTags.EmberBasic + "." + nameof(ScriptFinderEditor);
        protected override string MenuPath => "Ember/Tool/脚本查找";
        protected override string WindowTitle => "Script Finder";
        protected override Vector2 WindowSize => new(500, 600);

        private MonoScript _targetScript;
        private string _scriptName = "";
        private List<GameObject> _results = new();
        private Vector2 _scrollPos;

        // ======== 菜单 ========

        [MenuItem("Ember/Tool/脚本查找", false, 130)]
        public static void ShowWindow()
        {
            var win = GetWindow<ScriptFinderEditor>();
            win.minSize = win.WindowSize;
            win.Show();
        }

        [MenuItem("Assets/Ember/查找场景中使用此脚本的物体", false, 2000)]
        public static void QuickFindFromAsset()
        {
            var script = Selection.activeObject as MonoScript;
            if (!script) return;
            var gos = FindByScript(script);
            if (gos.Count == 0) { EditorUtility.DisplayDialog("Ember", $"场景中未找到使用 '{script.name}' 的物体。", "OK"); return; }
            Selection.objects = gos.ToArray();
            EmberDebug.Log(TAG, $"[Ember] Found {gos.Count} object(s) with '{script.name}'.");
        }

        [MenuItem("Assets/Ember/查找场景中使用此脚本的物体", true)]
        public static bool QuickFindValidate() => Selection.activeObject is MonoScript;

        // ======== UI ========

        protected override void DrawContent()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(L10n("Search Criteria", "搜索条件"), EditorStyles.boldLabel);

            // 拖拽脚本
            EditorGUI.BeginChangeCheck();
            _targetScript = (MonoScript)EditorGUILayout.ObjectField(
                L10n("Script Asset", "脚本文件"), _targetScript, typeof(MonoScript), false);
            if (EditorGUI.EndChangeCheck() && _targetScript)
                _scriptName = _targetScript.name;

            // 手动输入名称
            EditorGUI.BeginChangeCheck();
            _scriptName = EditorGUILayout.TextField(L10n("Or type name", "或输入类名"), _scriptName);
            if (EditorGUI.EndChangeCheck() && _targetScript?.name != _scriptName)
                _targetScript = null;

            bool canSearch = _targetScript || !string.IsNullOrEmpty(_scriptName);
            EditorGUI.BeginDisabledGroup(!canSearch);
            if (GUILayout.Button(new GUIContent(L10n(" Find in Scene", " 在场景中查找"), EditorGUIUtility.IconContent("ViewToolZoom").image), GUILayout.Height(40)))
                Search();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();

            if (_results.Count == 0) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(string.Format(L10n("Found {0} objects:", "找到 {0} 个物体:"), _results.Count), EditorStyles.boldLabel);

            if (GUILayout.Button(L10n("Select All", "全选所有结果")))
                Selection.objects = _results.ToArray();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, "box", GUILayout.ExpandHeight(true));
            foreach (var go in _results)
            {
                if (!go) continue;
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(EditorGUIUtility.ObjectContent(go, typeof(GameObject)), GUILayout.Height(20));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(L10n("Locate", "定位"), GUILayout.Width(60)))
                { Selection.activeGameObject = go; EditorGUIUtility.PingObject(go); }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        // ======== 核心 ========

        private void Search()
        {
            _results.Clear();
            if (_targetScript)
            {
                _results = FindByScript(_targetScript);
            }
            else if (!string.IsNullOrEmpty(_scriptName))
            {
                // 尝试从名字反查 MonoScript
                var guids = AssetDatabase.FindAssets("t:MonoScript " + _scriptName);
                foreach (var g in guids)
                {
                    var s = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(g));
                    if (s && s.name.Equals(_scriptName, StringComparison.OrdinalIgnoreCase))
                    { _targetScript = s; break; }
                }
                if (_targetScript)
                    _results = FindByScript(_targetScript);
                else
                {
                    // 降级：字符串匹配组件名
                    foreach (var go in GetAllSceneObjects())
                        foreach (var c in go.GetComponents<Component>())
                            if (c && c.GetType().Name.Equals(_scriptName, StringComparison.OrdinalIgnoreCase))
                            { _results.Add(go); break; }
                }
            }
            if (_results.Count > 0) Selection.objects = _results.ToArray();
            EditorUtility.DisplayDialog("Ember",
                _results.Count > 0
                    ? string.Format(L10n("Found {0} objects.", "找到 {0} 个物体。"), _results.Count)
                    : L10n("No objects found.", "未找到匹配物体。"), "OK");
        }

        private static List<GameObject> FindByScript(MonoScript script)
        {
            var result = new List<GameObject>();
            var type = script.GetClass();
            if (type == null) return result;
            foreach (var go in GetAllSceneObjects())
                if (go.GetComponent(type)) result.Add(go);
            return result;
        }

        private static List<GameObject> GetAllSceneObjects()
        {
            var list = new List<GameObject>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var roots = SceneManager.GetSceneAt(i).GetRootGameObjects();
                foreach (var root in roots) AddRecursive(root.transform, list);
            }
            return list;
        }

        private static void AddRecursive(Transform t, List<GameObject> list)
        {
            list.Add(t.gameObject);
            for (int i = 0; i < t.childCount; i++) AddRecursive(t.GetChild(i), list);
        }
    }
}
#endif
