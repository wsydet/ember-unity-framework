// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ember.Basic;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 2D 阴影生成器 —— 给选中的 SpriteRenderer 物体生成子物体阴影。
    /// 只适用于 2D（SpriteRenderer），3D 物体的阴影由光照系统处理。
    /// </summary>
    public class ShadowGeneratorTool : EmberEditorWindow
    {
        private const string TAG = LogTags.EmberBasic + "." + nameof(ShadowGeneratorTool);
        protected override string MenuPath => "Ember/Tool/2D 阴影生成器";
        protected override string WindowTitle => "Shadow Generator";
        protected override Vector2 WindowSize => new(500, 650);

        private Color _shadowColor = new(0, 0, 0, 0.5f);
        private Vector2 _offset = new(0.2f, -0.2f);
        private int _orderDiff = -1;

        [Serializable]
        public class TargetNode { public GameObject Obj; public SpriteRenderer Sr; public string Path; }
        private List<TargetNode> _targets = new();
        private Vector2 _scrollPos;

        private static Color s_lastColor = new(0, 0, 0, 0.5f);
        private static Vector2 s_lastOffset = new(0.2f, -0.2f);
        private static int s_lastOrder = -1;

        // ======== 菜单 ========

        [MenuItem("Ember/Tool/2D 阴影生成器", false, 170)]
        public static void ShowWindow()
        {
            var win = GetWindow<ShadowGeneratorTool>();
            win.minSize = win.WindowSize;
            win.Show();
        }

        [MenuItem("GameObject/Ember/2D 阴影/打开面板", false, 1900)]
        public static void ShowFromContext() => ShowWindow();

        [MenuItem("GameObject/Ember/2D 阴影/生成阴影 (用上次参数)", false, 1970)]
        public static void QuickGenerate()
        {
            int count = GenerateShadowsFor(Selection.gameObjects, s_lastColor, s_lastOffset, s_lastOrder);
            if (count > 0) EmberDebug.Log(TAG, $"[Ember] Generated {count} shadow(s).");
        }

        [MenuItem("GameObject/Ember/2D 阴影/生成阴影 (用上次参数)", true, 1970)]
        public static bool QuickGenerateValidate() => Selection.gameObjects.Length > 0;

        [MenuItem("GameObject/Ember/2D 阴影/移除选中物体下的阴影子物体", false, 1971)]
        public static void QuickRemove()
        {
            int count = RemoveShadowsFrom(Selection.gameObjects);
            EmberDebug.Log(TAG, $"[Ember] Removed {count} shadow(s).");
        }

        [MenuItem("GameObject/Ember/2D 阴影/移除选中物体下的阴影子物体", true, 1971)]
        public static bool QuickRemoveValidate() => Selection.gameObjects.Length > 0;

        // ======== Lifecycle ========

        protected override void OnEnable() { base.OnEnable(); LoadSettings(); UpdateTargets(); }
        protected override void OnDisable() => SaveSettings();
        private void OnSelectionChange() => UpdateTargets();

        private void LoadSettings()
        {
            if (ColorUtility.TryParseHtmlString("#" + EditorPrefs.GetString("Ember_ShadowColor", ""), out Color c)) _shadowColor = c;
            _offset.x = EditorPrefs.GetFloat("Ember_ShadowOffX", 0.2f);
            _offset.y = EditorPrefs.GetFloat("Ember_ShadowOffY", -0.2f);
            _orderDiff = EditorPrefs.GetInt("Ember_ShadowOrder", -1);
        }

        private void SaveSettings()
        {
            EditorPrefs.SetString("Ember_ShadowColor", ColorUtility.ToHtmlStringRGBA(_shadowColor));
            EditorPrefs.SetFloat("Ember_ShadowOffX", _offset.x);
            EditorPrefs.SetFloat("Ember_ShadowOffY", _offset.y);
            EditorPrefs.SetInt("Ember_ShadowOrder", _orderDiff);
        }

        // ======== UI ========

        protected override void DrawContent()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(L10n("1. Shadow Settings", "1. 阴影参数"), EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _shadowColor = EditorGUILayout.ColorField(L10n("Color", "颜色"), _shadowColor);
            _offset = EditorGUILayout.Vector2Field(L10n("Offset", "位移"), _offset);
            _orderDiff = EditorGUILayout.IntField(L10n("Order Diff", "层级差值"), _orderDiff);
            if (EditorGUI.EndChangeCheck()) SaveSettings();
            EditorGUILayout.HelpBox(string.Format(L10n("Shadow Order = Sprite Order + ({0})", "阴影层级 = 原物体层级 + ({0})"), _orderDiff), MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(string.Format(L10n("2. Targets ({0})", "2. 目标 ({0})"), _targets.Count), EditorStyles.boldLabel);

            bool canGen = _targets.Count > 0;
            EditorGUI.BeginDisabledGroup(!canGen);
            GUI.backgroundColor = canGen ? new Color(0.3f, 0.8f, 0.5f) : Color.white;
            if (GUILayout.Button(L10n("Generate Shadows", "生成阴影"), GUILayout.Height(40)))
            {
                s_lastColor = _shadowColor; s_lastOffset = _offset; s_lastOrder = _orderDiff;
                GenerateShadowsFor(Selection.gameObjects, _shadowColor, _offset, _orderDiff);
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            foreach (var n in _targets)
            {
                if (!n.Obj) continue;
                EditorGUILayout.BeginHorizontal("HelpBox");
                GUILayout.Label(EditorGUIUtility.IconContent("GameObject Icon"), GUILayout.Width(20));
                EditorGUILayout.LabelField(n.Obj.name, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(EditorGUIUtility.IconContent("d_ViewToolOrbit"), GUILayout.Width(25)))
                { EditorGUIUtility.PingObject(n.Obj); }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        // ======== 核心 ========

        private void UpdateTargets()
        {
            _targets.Clear();
            foreach (var go in Selection.gameObjects)
            {
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr) _targets.Add(new TargetNode { Obj = go, Sr = sr, Path = go.name });
            }
            Repaint();
        }

        private static int GenerateShadowsFor(GameObject[] gos, Color color, Vector2 offset, int orderDiff)
        {
            int count = 0;
            foreach (var go in gos)
            {
                var sr = go.GetComponent<SpriteRenderer>();
                if (!sr) continue;
                var shadow = new GameObject(go.name + "_Shadow");
                Undo.RegisterCreatedObjectUndo(shadow, "Create Shadow");
                shadow.transform.SetParent(go.transform, false);
                shadow.transform.localPosition = new Vector3(offset.x, offset.y, 0);
                shadow.transform.localRotation = Quaternion.identity;
                shadow.transform.localScale = Vector3.one;

                var shadowSR = shadow.AddComponent<SpriteRenderer>();
                shadowSR.sprite = sr.sprite;
                shadowSR.flipX = sr.flipX; shadowSR.flipY = sr.flipY;
                shadowSR.drawMode = sr.drawMode; shadowSR.size = sr.size; shadowSR.tileMode = sr.tileMode;
                shadowSR.color = color;
                shadowSR.sortingLayerID = sr.sortingLayerID;
                shadowSR.sortingOrder = sr.sortingOrder + orderDiff;
                count++;
            }
            return count;
        }

        private static int RemoveShadowsFrom(GameObject[] gos)
        {
            int count = 0;
            foreach (var go in gos)
            {
                for (int i = go.transform.childCount - 1; i >= 0; i--)
                {
                    var child = go.transform.GetChild(i);
                    if (child.name.EndsWith("_Shadow") && child.GetComponent<SpriteRenderer>())
                    { Undo.DestroyObjectImmediate(child.gameObject); count++; }
                }
            }
            return count;
        }
    }
}
#endif
