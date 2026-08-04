// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 资产替换工具 —— 选中场景中的物体，一键替换为指定 Prefab（保留位置/旋转/缩放/名称）。
    /// </summary>
    public class PrefabReplacerEditor : EmberEditorWindow
    {
        protected override string MenuPath => "Tools/Ember/资产替换工具";
        protected override string WindowTitle => "Prefab Replacer";
        protected override Vector2 WindowSize => new(500, 600);

        private GameObject _targetPrefab;
        private bool _keepPosition = true;
        private bool _keepRotation = true;
        private bool _keepScale = true;
        private bool _keepName;

        private static GameObject s_lastPrefab;

        // ======== 菜单 ========

        [MenuItem("Tools/Ember/资产替换工具")]
        public static void ShowWindow()
        {
            var win = GetWindow<PrefabReplacerEditor>();
            win.minSize = win.WindowSize;
            if (s_lastPrefab) win._targetPrefab = s_lastPrefab;
            win.Show();
        }

        [MenuItem("GameObject/Ember/资产替换/打开面板", false, 1800)]
        public static void ShowFromContext() => ShowWindow();

        [MenuItem("GameObject/Ember/资产替换/替换选中为上次预制体", false, 1820)]
        public static void QuickReplace()
        {
            if (!s_lastPrefab) return;
            int count = ReplaceSelected(s_lastPrefab, true, true, true, false);
            Debug.Log($"[Ember] Replaced {count} objects with '{s_lastPrefab.name}'.");
        }

        [MenuItem("GameObject/Ember/资产替换/替换选中为上次预制体", true)]
        public static bool QuickReplaceValidate() => s_lastPrefab && Selection.gameObjects.Length > 0;

        // ======== UI ========

        protected override void DrawContent()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(L10n("1. Target Prefab", "1. 目标预制体"), EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _targetPrefab = (GameObject)EditorGUILayout.ObjectField(
                L10n("Prefab to use", "用于替换的预制体"), _targetPrefab, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck() && _targetPrefab) s_lastPrefab = _targetPrefab;

            if (_targetPrefab)
            {
                var preview = AssetPreview.GetAssetPreview(_targetPrefab);
                if (preview)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(preview, GUILayout.Width(100), GUILayout.Height(100));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
                EditorGUILayout.HelpBox(L10n("Drag a prefab here.", "请将预制体拖入此处。"), MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(L10n("2. Options", "2. 替换选项"), EditorStyles.boldLabel);
            _keepPosition = EditorGUILayout.Toggle(L10n("Keep Position", "保留坐标"), _keepPosition);
            _keepRotation = EditorGUILayout.Toggle(L10n("Keep Rotation", "保留旋转"), _keepRotation);
            _keepScale = EditorGUILayout.Toggle(L10n("Keep Scale", "保留缩放"), _keepScale);
            _keepName = EditorGUILayout.Toggle(L10n("Keep Original Name", "保留原名称"), _keepName);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(20);

            var sel = Selection.gameObjects;
            bool can = sel.Length > 0 && _targetPrefab;
            EditorGUI.BeginDisabledGroup(!can);
            GUI.backgroundColor = can ? new Color(0.6f, 0.4f, 0.9f) : new Color(0.7f, 0.7f, 0.7f, 0.5f);
            string btnLabel = can
                ? string.Format(L10n("Replace {0} Selected", "替换选中的 {0} 个物体"), sel.Length)
                : L10n("Select objects in scene", "请在场景中选择物体");
            if (GUILayout.Button(new GUIContent(btnLabel, EditorGUIUtility.IconContent("Prefab Icon").image), GUILayout.Height(60)))
            {
                if (EditorUtility.DisplayDialog(L10n("Confirm", "确认替换"),
                    string.Format(L10n("Replace {0} objects with '{1}'?", "将 {0} 个物体替换为 '{1}' 吗？"), sel.Length, _targetPrefab.name),
                    L10n("Yes", "确定"), L10n("Cancel", "取消")))
                {
                    s_lastPrefab = _targetPrefab;
                    ReplaceSelected(_targetPrefab, _keepPosition, _keepRotation, _keepScale, _keepName);
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();
        }

        // ======== 核心逻辑 ========

        private static int ReplaceSelected(GameObject prefab, bool keepPos, bool keepRot, bool keepScale, bool keepName)
        {
            var targets = Selection.gameObjects;
            if (targets.Length == 0 || !prefab) return 0;

            List<GameObject> created = new();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();

            foreach (var old in targets)
            {
                if (!old) continue;
                var newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(newObj, "Replace Prefab");
                newObj.transform.SetParent(old.transform.parent);
                if (keepPos) newObj.transform.position = old.transform.position;
                if (keepRot) newObj.transform.rotation = old.transform.rotation;
                if (keepScale) newObj.transform.localScale = old.transform.localScale;
                if (keepName) newObj.name = old.name;
                Undo.DestroyObjectImmediate(old);
                created.Add(newObj);
            }

            Undo.SetCurrentGroupName("Batch Replace Prefabs");
            Undo.CollapseUndoOperations(group);
            Selection.objects = created.ToArray();
            return created.Count;
        }
    }
}
#endif
