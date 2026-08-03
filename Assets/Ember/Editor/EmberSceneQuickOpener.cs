using System.Collections.Generic;
using System.Linq;
using Ember.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>
    /// 快速场景打开器。主场景互斥选择 + 叠加场景多选 + 一键打开。
    /// 菜单 Ember → 快速打开场景 或 Toolbar 按钮。
    /// </summary>
    public class EmberSceneQuickOpener : EditorWindow
    {
        private EmberSceneMapping _mapping;
        private List<StateSceneEntry> _mainStates = new();     // 互斥主场景（非 Init、非 Settings）
        private List<StateSceneEntry> _overlayStates = new();  // 可叠加场景
        private int _mainIndex;
        private readonly List<bool> _overlayToggles = new();

        [MenuItem("Ember/快速打开场景", false, 1)]
        public static void Open()
        {
            var window = GetWindow<EmberSceneQuickOpener>(true, "快速打开场景");
            window.minSize = new Vector2(320, 160);
            window.maxSize = new Vector2(420, 260);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshMapping();
        }

        private void RefreshMapping()
        {
            var guids = AssetDatabase.FindAssets("t:EmberSceneMapping");
            _mapping = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<EmberSceneMapping>(
                    AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault();

            if (_mapping == null) return;

            _mainStates.Clear();
            _overlayStates.Clear();

            foreach (var e in _mapping.entries)
            {
                if (e.stateName == "InitState") continue;       // 隐藏，无用户场景
                if (e.stateName == "SettingsState")              // 可叠加
                    _overlayStates.Add(e);
                else
                    _mainStates.Add(e);                          // 主场景
            }

            // 同步 toggle 列表长度
            while (_overlayToggles.Count < _overlayStates.Count)
                _overlayToggles.Add(false);
            if (_overlayToggles.Count > _overlayStates.Count)
                _overlayToggles.RemoveRange(_overlayStates.Count, _overlayToggles.Count - _overlayStates.Count);
        }

        private void OnGUI()
        {
            if (_mapping == null)
            {
                EditorGUILayout.HelpBox("EmberSceneMapping.asset 未找到，下次编译自动生成。", MessageType.Info);
                return;
            }

            // 工具栏
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("刷新", GUILayout.Width(50))) RefreshMapping();
            if (GUILayout.Button("SO", GUILayout.Width(40))) Selection.activeObject = _mapping;
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            // --- 主场景（互斥） ---
            if (_mainStates.Count > 0)
            {
                GUILayout.Label("主场景", EditorStyles.boldLabel);
                var labels = _mainStates.Select(e => e.stateName.Replace("State", "")).ToArray();
                _mainIndex = Mathf.Clamp(_mainIndex, 0, labels.Length - 1);
                _mainIndex = GUILayout.Toolbar(_mainIndex, labels);

                var selected = _mainStates[_mainIndex];
                ShowSceneStatus(selected);
            }

            // --- 叠加场景（多选） ---
            if (_overlayStates.Count > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label("叠加场景（可多选）", EditorStyles.boldLabel);
                for (int i = 0; i < _overlayStates.Count; i++)
                {
                    _overlayToggles[i] = GUILayout.Toggle(_overlayToggles[i],
                        _overlayStates[i].stateName.Replace("State", ""));
                    ShowSceneStatus(_overlayStates[i]);
                }
            }

            GUILayout.Space(8);

            // --- 打开按钮 ---
            GUI.enabled = CanOpen();
            if (GUILayout.Button("打开场景", GUILayout.Height(28)))
                OpenScenes();
            GUI.enabled = true;
        }

        private static void ShowSceneStatus(StateSceneEntry entry)
        {
            if (entry.sceneField.HasValue)
                EditorGUILayout.LabelField($"  {entry.sceneField.SceneName}", EditorStyles.miniLabel);
            else
                EditorGUILayout.LabelField("  (未设置 — 请在 SO 中手动赋值)", EditorStyles.miniLabel);
        }

        private bool CanOpen()
        {
            if (_mapping.frameworkScene == null) return false;
            if (_mainStates.Count == 0) return false;
            return _mainStates[_mainIndex].sceneField.HasValue;
        }

        private void OpenScenes()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var frameworkPath = AssetDatabase.GetAssetPath(_mapping.frameworkScene);
            EditorSceneManager.OpenScene(frameworkPath, OpenSceneMode.Single);

            // 主场景
            var mainPath = FindScenePath(_mainStates[_mainIndex].sceneField.SceneName);
            EditorSceneManager.OpenScene(mainPath, OpenSceneMode.Additive);

            // 叠加场景
            var opened = new List<string> { frameworkPath, mainPath };
            for (int i = 0; i < _overlayToggles.Count; i++)
            {
                if (_overlayToggles[i] && _overlayStates[i].sceneField.HasValue)
                {
                    var path = FindScenePath(_overlayStates[i].sceneField.SceneName);
                    if (path != null)
                    {
                        EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                        opened.Add(path);
                    }
                }
            }

            EmberDebug.Log("EmberCore.Editor", $"场景已打开: {string.Join(" + ", opened.Select(System.IO.Path.GetFileNameWithoutExtension))}");
            Close();
        }

        private static string FindScenePath(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return null;
            var guids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == sceneName)
                    return p;
            }
            return null;
        }
    }
}
