// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Ember.Basic;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 碰撞体可视化助手 —— 一键切换 2D 碰撞体填充透明度 / 3D Gizmos 线框显示。
    /// </summary>
    public class ColliderHelperEditor : EmberEditorWindow
    {
        private const string TAG = LogTags.EmberBasic + "." + nameof(ColliderHelperEditor);
        protected override string MenuPath => "Ember/Tool/碰撞体可视化助手";
        protected override string WindowTitle => "碰撞体可视化";
        protected override Vector2 WindowSize => new(400, 350);

        private Vector2 _scrollPos;

        // ======== 菜单入口 ========

        [MenuItem("Ember/Tool/碰撞体可视化助手", false, 160)]
        public static void ShowWindow()
        {
            var win = GetWindow<ColliderHelperEditor>();
            win.minSize = win.maxSize = win.WindowSize;
            win.Show();
        }

        // ---- 右键菜单：GameObject/碰撞体显示 ----

        [MenuItem("GameObject/Ember/碰撞体显示/打开面板", false, 1000)]
        public static void ShowWindowFromContext() => ShowWindow();

        [MenuItem("GameObject/Ember/碰撞体显示/切换 2D 碰撞体填充", false, 1050)]
        public static void Toggle2DFromContext() => Toggle2DFill();

        [MenuItem("GameObject/Ember/碰撞体显示/切换 2D 碰撞体填充", true, 1050)]
        public static bool Toggle2DValidate() => Selection.activeGameObject != null && (Selection.activeGameObject.GetComponent<Collider2D>() != null || Selection.activeGameObject.GetComponentInChildren<Collider2D>() != null);

        [MenuItem("GameObject/Ember/碰撞体显示/切换 3D Gizmos 显示", true, 1051)]
        public static bool Toggle3DValidate() => Selection.activeGameObject != null && (Selection.activeGameObject.GetComponent<Collider>() != null || Selection.activeGameObject.GetComponentInChildren<Collider>() != null);

        [MenuItem("GameObject/Ember/碰撞体显示/切换 3D Gizmos 显示", false, 1051)]
        public static void Toggle3DFromContext() => ToggleGizmos();

        protected override void DrawContent()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            // ---- 2D ----
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(L10n("1. 2D Physics Fill", "1. 2D 碰撞体填充"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(L10n(
                "Toggle 2D collider fill transparent/visible to see through overlapping colliders.",
                "切换 2D 碰撞体填充颜色（透明/半透明），方便查看被遮挡的物体。"), MessageType.Info);

            float alpha = Get2DFillAlpha();
            bool isOn = alpha > 0.05f;
            GUI.backgroundColor = isOn ? new Color(0.3f, 0.8f, 0.5f) : Color.grey;
            string label = isOn
                ? L10n("2D Fill: ON (Click to Hide)", "2D 填充: 开启 (点击隐藏)")
                : L10n("2D Fill: OFF (Click to Show)", "2D 填充: 关闭 (点击显示)");
            if (GUILayout.Button(new GUIContent(label, EditorGUIUtility.IconContent("BoxCollider2D Icon").image), GUILayout.Height(40)))
                Toggle2DFill();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(20);

            // ---- 3D ----
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(L10n("2. 3D Gizmos", "2. 3D 线框显示"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(L10n(
                "Quick toggle Scene Gizmos to show/hide 3D collider wireframes.",
                "快速切换场景 Gizmos 开关以显示/隐藏 3D 碰撞体线框。"), MessageType.Info);

            bool gizmosOn = SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.drawGizmos;
            GUI.backgroundColor = gizmosOn ? new Color(0.2f, 0.6f, 0.9f) : Color.grey;
            string gizmosLabel = gizmosOn
                ? L10n("Gizmos: ON (Click to Hide)", "Gizmos: 开启 (点击隐藏)")
                : L10n("Gizmos: OFF (Click to Show)", "Gizmos: 关闭 (点击显示)");
            if (GUILayout.Button(new GUIContent(gizmosLabel, EditorGUIUtility.IconContent("BoxCollider Icon").image), GUILayout.Height(40)))
                ToggleGizmos();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private static float Get2DFillAlpha()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset");
            if (assets.Length == 0) return 0f;
            using var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("m_GizmoColor");
            return prop?.colorValue.a ?? 0f;
        }

        private static void Toggle2DFill()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset");
            if (assets.Length == 0) return;
            using var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("m_GizmoColor");
            if (prop == null) return;

            var c = prop.colorValue;
            if (c.a > 0.01f) { EditorPrefs.SetFloat("Ember_LastPhysics2DAlpha", c.a); c.a = 0f; }
            else { c.a = EditorPrefs.GetFloat("Ember_LastPhysics2DAlpha", 0.5f); if (c.a < 0.1f) c.a = 0.5f; }
            prop.colorValue = c;
            so.ApplyModifiedProperties();
            InternalEditorUtility.RepaintAllViews();
        }

        private static void ToggleGizmos()
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv != null) { sv.drawGizmos = !sv.drawGizmos; sv.Repaint(); }
            else EmberDebug.LogWarning(TAG, "No active Scene View found.");
        }
    }
}
#endif
