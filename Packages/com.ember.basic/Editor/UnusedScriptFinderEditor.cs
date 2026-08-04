// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 未引用脚本查找器 —— 扫描项目中所有 .cs 文件，找出未被任何 Scene/Prefab/ScriptableObject 引用的脚本。
    ///
    /// ⚠️ 注意：通过 AddComponent〈T〉、Resources.Load、反射等方式动态加载的脚本不会出现在引用链中，
    /// 会被误判为"未引用"。结果仅供参考，删除前请人工确认。
    /// </summary>
    public class UnusedScriptFinderEditor : EmberEditorWindow
    {
        protected override string MenuPath => "Tools/Ember/未引用脚本查找";
        protected override string WindowTitle => "未引用脚本查找";
        protected override Vector2 WindowSize => new(550, 650);

        private List<MonoScript> _unreferenced = new();
        private bool _ignoreEditor = true;
        private bool _ignorePlugins = true;
        private bool _scanning;
        private Vector2 _scrollPos;

        // ======== 菜单 ========

        [MenuItem("Tools/Ember/未引用脚本查找")]
        public static void ShowWindow()
        {
            var win = GetWindow<UnusedScriptFinderEditor>();
            win.minSize = win.WindowSize;
            win.Show();
        }

        [MenuItem("Assets/Ember/查找此脚本的引用", false, 2300)]
        public static void CheckSingleScript()
        {
            var script = Selection.activeObject as MonoScript;
            if (!script) return;

            bool hasRef = false;
            foreach (var path in GetAllScenesAndPrefabs())
            {
                var deps = AssetDatabase.GetDependencies(path, false);
                var scriptPath = AssetDatabase.GetAssetPath(script);
                if (deps.Contains(scriptPath)) { hasRef = true; break; }
            }

            EditorUtility.DisplayDialog("Ember",
                hasRef
                    ? $"'{script.name}' 被至少一个 Scene/Prefab/SO 引用。"
                    : $"'{script.name}' 未找到任何 Scene/Prefab/SO 引用。\n请人工确认是否可安全删除。", "OK");
        }

        [MenuItem("Assets/Ember/查找此脚本的引用", true)]
        public static bool CheckSingleValidate() => Selection.activeObject is MonoScript;

        [MenuItem("Assets/Ember/扫描未引用脚本", false, 2301)]
        public static void QuickScanFromFolder()
        {
            var win = GetWindow<UnusedScriptFinderEditor>();
            win.minSize = win.WindowSize;
            win.Scan();
            win.Show();
        }

        // ======== UI ========

        protected override void DrawContent()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(L10n("1. Settings", "1. 设置"), EditorStyles.boldLabel);
            _ignoreEditor = EditorGUILayout.Toggle(L10n("Ignore Editor folder", "忽略 Editor 文件夹"), _ignoreEditor);
            _ignorePlugins = EditorGUILayout.Toggle(L10n("Ignore Plugins folder", "忽略 Plugins 文件夹"), _ignorePlugins);

            EditorGUILayout.HelpBox(L10n(
                "Note: Scripts loaded via AddComponent, Resources.Load, or reflection won't show in dependency chains and may be flagged as unreferenced. Always manually verify before deleting.",
                "注意：通过 AddComponent、Resources.Load、反射等方式动态加载的脚本不在依赖链中，可能被误判。删除前请人工确认。"), MessageType.Warning);

            EditorGUI.BeginDisabledGroup(_scanning);
            if (GUILayout.Button(_scanning
                ? L10n("Scanning...", "扫描中...")
                : L10n("Scan Project", "扫描项目"), GUILayout.Height(40)))
                Scan();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();

            if (_unreferenced.Count == 0) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(string.Format(L10n("Unreferenced Scripts ({0})", "未引用脚本 ({0})"), _unreferenced.Count), EditorStyles.boldLabel);

            if (GUILayout.Button(L10n("Select All in Project", "在 Project 中全选")))
                Selection.objects = _unreferenced.Cast<UnityEngine.Object>().ToArray();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, "box", GUILayout.ExpandHeight(true));
            foreach (var script in _unreferenced)
            {
                if (!script) continue;
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(script.name, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(L10n("Select", "选中"), GUILayout.Width(50)))
                { Selection.activeObject = script; EditorGUIUtility.PingObject(script); }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        // ======== 核心逻辑 ========

        private void Scan()
        {
            _scanning = true;
            _unreferenced.Clear();
            try
            {
                var allScripts = AssetDatabase.FindAssets("t:MonoScript")
                    .Select(g => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(s => s && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(s)))
                    .ToList();

                // 过滤
                allScripts = allScripts.Where(s =>
                {
                    var path = AssetDatabase.GetAssetPath(s);
                    if (_ignoreEditor && path.Contains("/Editor/")) return false;
                    if (_ignorePlugins && path.Contains("/Plugins/")) return false;
                    return path.EndsWith(".cs");
                }).ToList();

                var referencedPaths = new HashSet<string>();

                var allAssets = GetAllScenesAndPrefabs();
                for (int i = 0; i < allAssets.Length; i++)
                {
                    if (i % 20 == 0)
                        EditorUtility.DisplayProgressBar("扫描中...", allAssets[i], (float)i / allAssets.Length);

                    foreach (var dep in AssetDatabase.GetDependencies(allAssets[i], false))
                        referencedPaths.Add(dep);
                }

                _unreferenced = allScripts.Where(s => !referencedPaths.Contains(AssetDatabase.GetAssetPath(s))).ToList();
            }
            catch (Exception ex) { Debug.LogError($"[Ember] Scan failed: {ex.Message}"); }
            finally { _scanning = false; EditorUtility.ClearProgressBar(); }

            EditorUtility.DisplayDialog("完成",
                string.Format(L10n($"Found {_unreferenced.Count} unreferenced scripts.", $"找到 {_unreferenced.Count} 个未被引用的脚本。"), _unreferenced.Count), "OK");
        }

        private static string[] GetAllScenesAndPrefabs()
        {
            var scenes = AssetDatabase.FindAssets("t:Scene").Select(g => AssetDatabase.GUIDToAssetPath(g));
            var prefabs = AssetDatabase.FindAssets("t:Prefab").Select(g => AssetDatabase.GUIDToAssetPath(g));
            var sos = AssetDatabase.FindAssets("t:ScriptableObject").Select(g => AssetDatabase.GUIDToAssetPath(g));
            return scenes.Concat(prefabs).Concat(sos).ToArray();
        }
    }
}
#endif
