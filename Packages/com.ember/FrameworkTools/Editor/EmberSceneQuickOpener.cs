using System.Collections.Generic;
using System.Linq;
using Ember.Basic;
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
        private const string TAG = LogTags.CoreEditor;

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
            var mapping = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<EmberSceneMapping>(
                    AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(m => m != null);
            UpdateMapping(mapping);
        }

        private void UpdateMapping(EmberSceneMapping mapping)
        {
            var selectedState = _mainIndex >= 0 && _mainIndex < _mainStates.Count
                ? _mainStates[_mainIndex].stateName : null;
            var selectedOverlays = new HashSet<string>();
            for (int i = 0; i < _overlayStates.Count && i < _overlayToggles.Count; i++)
                if (_overlayToggles[i]) selectedOverlays.Add(_overlayStates[i].stateName);

            _mapping = mapping;
            _mainStates.Clear();
            _overlayStates.Clear();
            _overlayToggles.Clear();
            _mainIndex = 0;
            if (_mapping == null || _mapping.entries == null) return;

            foreach (var e in _mapping.entries)
            {
                if (e == null || string.IsNullOrEmpty(e.stateName)) continue;
                if (e.stateName == "InitState") continue;       // 隐藏，无用户场景
                if (e.stateName == "SettingsState")              // 可叠加
                    _overlayStates.Add(e);
                else
                    _mainStates.Add(e);                          // 主场景
            }

            // 按状态身份保留选择，避免刷新后的顺序或数量变化选中另一个场景。
            int index = _mainStates.FindIndex(e => e.stateName == selectedState);
            if (index >= 0) _mainIndex = index;
            foreach (var entry in _overlayStates)
                _overlayToggles.Add(selectedOverlays.Contains(entry.stateName));
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            bool refresh = GUILayout.Button("刷新", GUILayout.Width(50));
            using (new EditorGUI.DisabledScope(_mapping == null))
                if (GUILayout.Button("SO", GUILayout.Width(40))) Selection.activeObject = _mapping;
            EditorGUILayout.EndHorizontal();
            if (refresh)
            {
                RefreshMapping();
                Repaint();
                GUIUtility.ExitGUI(); // 列表可能改变，下一次 Layout 重新构建控件。
            }

            if (_mapping == null)
            {
                EditorGUILayout.HelpBox("EmberSceneMapping.asset 未找到，下次编译自动生成。", MessageType.Info);
                return;
            }

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
            using (new EditorGUI.DisabledScope(!CanOpen()))
                if (GUILayout.Button("打开场景", GUILayout.Height(28)))
                    OpenScenes();
        }

        private static void ShowSceneStatus(StateSceneEntry entry)
        {
            var path = ResolveScenePath(entry.sceneField);
            if (!string.IsNullOrEmpty(path))
                EditorGUILayout.LabelField($"  {System.IO.Path.GetFileNameWithoutExtension(path)}", EditorStyles.miniLabel);
            else
                EditorGUILayout.LabelField("  (场景未设置、已丢失或名称不唯一 — 请在 SO 中指定)", EditorStyles.miniLabel);
        }

        private bool CanOpen()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
            if (_mapping == null || _mapping.frameworkScene == null) return false;
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_mapping.frameworkScene))) return false;
            if (_mainIndex < 0 || _mainIndex >= _mainStates.Count) return false;
            if (string.IsNullOrEmpty(ResolveScenePath(_mainStates[_mainIndex].sceneField))) return false;
            if (_overlayStates.Count != _overlayToggles.Count) return false;
            for (int i = 0; i < _overlayStates.Count; i++)
                if (_overlayToggles[i] && string.IsNullOrEmpty(ResolveScenePath(_overlayStates[i].sceneField)))
                    return false;
            return true;
        }

        private void OpenScenes()
        {
            if (!CanOpen()) return;
            // 先解析全部路径，再询问保存与切换，避免打开框架后才发现目标场景无效。
            var paths = new List<string> { AssetDatabase.GetAssetPath(_mapping.frameworkScene) };
            var mainPath = ResolveScenePath(_mainStates[_mainIndex].sceneField);
            if (!paths.Contains(mainPath)) paths.Add(mainPath);
            for (int i = 0; i < _overlayStates.Count; i++)
            {
                if (!_overlayToggles[i]) continue;
                var path = ResolveScenePath(_overlayStates[i].sceneField);
                if (!paths.Contains(path)) paths.Add(path);
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            for (int i = 0; i < paths.Count; i++)
                EditorSceneManager.OpenScene(paths[i], i == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);

            EmberDebug.Log(TAG, $"场景已打开: {string.Join(" + ", paths.Select(System.IO.Path.GetFileNameWithoutExtension))}");
            Close();
        }

        private static string ResolveScenePath(EmberSceneField field)
        {
            var assetPath = field.EditorScenePath;
            if (!string.IsNullOrEmpty(assetPath)) return assetPath;
            // 保留代码通过场景名构造引用的用法；重名时要求显式指定资产。
            var sceneName = field.SceneName;
            if (string.IsNullOrEmpty(sceneName)) return null;
            var guids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
            string match = null;
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(p) != sceneName) continue;
                if (match != null) return null;
                match = p;
            }
            return match;
        }
    }
}
