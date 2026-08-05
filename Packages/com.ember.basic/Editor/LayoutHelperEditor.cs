// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 布局助手 —— 复制+偏移 / 快速打组。
    /// 右键 Hierarchy → Ember → 布局助手 → 一键操作。
    /// </summary>
    public class LayoutHelperEditor : EmberEditorWindow
    {
        protected override string MenuPath => "Ember/Tool/布局助手";
        protected override string WindowTitle => "布局助手";
        protected override Vector2 WindowSize => new(450, 500);

        private Vector3 _offset = Vector3.zero;
        private static Vector3 s_lastOffset = Vector3.zero;

        // ======== 菜单 ========

        [MenuItem("Ember/Tool/布局助手", false, 120)]
        public static void ShowWindow()
        {
            var win = GetWindow<LayoutHelperEditor>();
            win.minSize = win.WindowSize;
            if (win._offset == Vector3.zero) win._offset = s_lastOffset;
            win.Show();
        }

        [MenuItem("GameObject/Ember/布局助手/打开面板", false, 1500)]
        public static void ShowFromContext() => ShowWindow();

        [MenuItem("GameObject/Ember/布局助手/复制并偏移 (沿用上次偏移)", false, 1570)]
        public static void QuickDuplicateAndMove()
        {
            var sel = Selection.activeGameObject;
            if (!sel) return;
            DuplicateAndMove(sel, s_lastOffset);
        }

        [MenuItem("GameObject/Ember/布局助手/复制并偏移 (沿用上次偏移)", true, 1570)]
        public static bool QuickDuplicateAndMoveValidate() => Selection.activeGameObject;

        [MenuItem("GameObject/Ember/布局助手/快速打组 Ctrl+G", false, 1571)]
        public static void QuickGroup()
        {
            var gos = Selection.gameObjects;
            if (gos.Length <= 1) return;
            GroupObjects(gos);
        }

        [MenuItem("GameObject/Ember/布局助手/快速打组 Ctrl+G", true, 1571)]
        public static bool QuickGroupValidate() => Selection.gameObjects.Length > 1;

        // ======== UI ========

        protected override void DrawContent()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(L10n("1. Offset Settings", "1. 偏移量设置"), EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _offset = EditorGUILayout.Vector3Field(L10n("XYZ Offset", "XYZ 偏移量"), _offset);
            if (EditorGUI.EndChangeCheck()) s_lastOffset = _offset;

            if (GUILayout.Button(L10n("Reset Offset", "重置偏移"), GUILayout.Width(100)))
                _offset = s_lastOffset = Vector3.zero;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);

            var gos = Selection.gameObjects;
            int count = gos.Length;

            // 按钮 1: 复制并偏移
            bool canDup = count == 1;
            EditorGUI.BeginDisabledGroup(!canDup);
            GUI.backgroundColor = canDup ? new Color(0.2f, 0.6f, 0.9f) : Color.grey;
            string dupLabel = canDup
                ? string.Format(L10n("Duplicate & Move: {0}", "复制偏移: {0}"), gos[0].name)
                : L10n("Select 1 object to duplicate", "选中 1 个物体以激活复制偏移");
            if (GUILayout.Button(new GUIContent(dupLabel, EditorGUIUtility.IconContent("d_ToolHandleLocal").image), GUILayout.Height(50)))
            {
                s_lastOffset = _offset;
                DuplicateAndMove(gos[0], _offset);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(15);

            // 按钮 2: 快速打组
            bool canGroup = count > 1;
            EditorGUI.BeginDisabledGroup(!canGroup);
            GUI.backgroundColor = canGroup ? new Color(0.3f, 0.8f, 0.5f) : Color.grey;
            string groupLabel = canGroup
                ? string.Format(L10n("Group {0} Objects", "将 {0} 个物体快速打组"), count)
                : L10n("Select multiple objects to group", "选中多个物体以激活快速打组");
            if (GUILayout.Button(new GUIContent(groupLabel, EditorGUIUtility.IconContent("d_VerticalLayoutGroup Icon").image), GUILayout.Height(50)))
                GroupObjects(gos);
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();
        }

        // ======== 核心逻辑 ========

        private static void DuplicateAndMove(GameObject target, Vector3 offset)
        {
            Undo.IncrementCurrentGroup();
            var newObj = Object.Instantiate(target, target.transform.parent);
            if (target.scene.IsValid()) SceneManager.MoveGameObjectToScene(newObj, target.scene);
            newObj.name = target.name;
            Undo.RegisterCreatedObjectUndo(newObj, "Duplicate & Move");

            if (newObj.TryGetComponent<RectTransform>(out var rt))
                rt.anchoredPosition3D += offset;
            else
                newObj.transform.localPosition += offset;

            Selection.activeGameObject = newObj;
            Undo.SetCurrentGroupName("Layout Helper: Duplicate & Move");
        }

        private static void GroupObjects(GameObject[] targets)
        {
            if (targets == null || targets.Length == 0) return;

            Undo.IncrementCurrentGroup();
            var parent = new GameObject(targets[0].name + "_Group");
            if (targets[0].GetComponent<RectTransform>())
                parent.AddComponent<RectTransform>();
            Undo.RegisterCreatedObjectUndo(parent, "Create Group");

            if (targets[0].scene.IsValid()) SceneManager.MoveGameObjectToScene(parent, targets[0].scene);

            parent.transform.SetParent(targets[0].transform.parent);
            parent.transform.localPosition = targets[0].transform.localPosition;
            parent.transform.localRotation = targets[0].transform.localRotation;
            parent.transform.localScale = Vector3.one;

            foreach (var obj in targets)
                Undo.SetTransformParent(obj.transform, parent.transform, "Move into Group");

            Selection.activeGameObject = parent;
            Undo.SetCurrentGroupName("Layout Helper: Quick Group");
        }
    }
}
#endif
