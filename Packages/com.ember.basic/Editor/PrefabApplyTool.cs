// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Ember.Basic;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 预制体应用工具 —— 扫描场景中有 Override 的 Prefab 实例，批量/单独 Apply 回源预制体。
    /// </summary>
    public class PrefabApplyTool : EmberEditorWindow
    {
        private const string TAG = LogTags.EmberBasic + "." + nameof(PrefabApplyTool);
        protected override string MenuPath => "Ember/Tool/预制体应用工具";
        protected override string WindowTitle => "Prefab Apply";
        protected override Vector2 WindowSize => new(600, 700);

        [Serializable]
        public class PrefabNode
        {
            public GameObject Obj;
            public string Path;
            public GameObject Asset;
        }

        [HideInInspector] public List<PrefabNode> Modified = new();
        private bool _includeNested;
        private Vector2 _scrollPos;

        // ======== 菜单 ========

        [MenuItem("Ember/Tool/预制体应用工具", false, 190)]
        private static void OpenWindow()
        {
            var win = GetWindow<PrefabApplyTool>();
            win.minSize = win.WindowSize;
            win.Scan();
            win.Show();
        }

        [MenuItem("GameObject/Ember/预制体改动/应用选中到预制体", false, 1700)]
        public static void QuickApplySelected()
        {
            var go = Selection.activeGameObject;
            if (!go || !PrefabUtility.IsAnyPrefabInstanceRoot(go))
            { EditorUtility.DisplayDialog("Ember", "请选中一个预制体实例的根节点。", "OK"); return; }
            ApplyPrefabNode(go);
            EmberDebug.Log(TAG, $"[Ember] Applied: {go.name}");
        }

        [MenuItem("GameObject/Ember/预制体改动/应用选中到预制体", true, 1700)]
        public static bool QuickApplyValidate() =>
            Selection.activeGameObject && PrefabUtility.IsAnyPrefabInstanceRoot(Selection.activeGameObject);

        [MenuItem("GameObject/Ember/预制体改动/扫描所有改动", false, 1701)]
        public static void QuickScanOpen()
        {
            var win = GetWindow<PrefabApplyTool>();
            win.minSize = win.WindowSize;
            win.Scan();
            win.Show();
        }

        // ======== UI ========

        protected override void DrawContent()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(L10n("1. Actions", "1. 操作"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(L10n(
                "Scans for Prefab instances with overrides. Apply changes back to the source Prefab.",
                "扫描场景中所有有改动(Overrides)的预制体实例，可批量应用回源预制体。"), MessageType.Info);

            _includeNested = EditorGUILayout.Toggle(L10n("Include nested prefabs", "包含嵌套预制体"), _includeNested);
            if (GUILayout.Button(L10n("Scan Scene", "扫描场景"), GUILayout.Height(30))) Scan();

            if (Modified.Count > 0)
            {
                EditorGUILayout.Space(5);
                GUI.backgroundColor = new Color(0.3f, 0.8f, 0.5f);
                string label = string.Format(L10n("Apply All Changes ({0})", "应用所有改动 ({0})"), Modified.Count);
                if (GUILayout.Button(new GUIContent(label, EditorGUIUtility.IconContent("SaveActive").image), GUILayout.Height(40)))
                {
                    if (EditorUtility.DisplayDialog(L10n("Confirm", "确认"),
                        string.Format(L10n("Apply changes to {0} prefabs?", "确定将改动应用到 {0} 个预制体吗？"), Modified.Count),
                        L10n("Yes", "确定"), L10n("Cancel", "取消")))
                        ApplyAll();
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndVertical();

            if (Modified.Count == 0) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(string.Format(L10n("Modified List ({0})", "改动列表 ({0})"), Modified.Count), EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, "box", GUILayout.ExpandHeight(true));
            for (int i = 0; i < Modified.Count; i++)
            {
                var n = Modified[i];
                if (!n.Obj) continue;
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label(EditorGUIUtility.IconContent("console.warnicon"), GUILayout.Width(25));
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(n.Obj.name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(n.Path, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(L10n("Apply", "应用"), GUILayout.Width(60)))
                { ApplyPrefabNode(n.Obj); Scan(); }
                if (GUILayout.Button(L10n("Revert", "还原"), GUILayout.Width(50)))
                { RevertPrefabNode(n.Obj); Scan(); }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        // ======== 核心逻辑 ========

        private void Scan()
        {
            Modified.Clear();
            var all = FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in all)
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(go)) continue;
                if (!PrefabUtility.HasPrefabInstanceAnyOverrides(go, false)) continue;

                // 默认只显示顶层预制体（父级不是另一个 Prefab 实例的），排除嵌套变体
                if (!_includeNested && PrefabUtility.IsPartOfAnyPrefab(go.transform.parent)) continue;

                var asset = PrefabUtility.GetCorrespondingObjectFromSource(go);
                Modified.Add(new PrefabNode { Obj = go, Path = AssetDatabase.GetAssetPath(asset), Asset = asset });
            }
        }

        private void ApplyAll()
        {
            for (int i = Modified.Count - 1; i >= 0; i--)
                if (Modified[i].Obj) ApplyPrefabNode(Modified[i].Obj);
            AssetDatabase.SaveAssets();
            Scan();
        }

        private static void ApplyPrefabNode(GameObject root)
        {
            try { PrefabUtility.ApplyPrefabInstance(root, InteractionMode.UserAction); }
            catch (Exception e) { EmberDebug.LogError(TAG, $"[Ember] Apply failed: {root.name}\n{e.Message}"); }
        }

        private static void RevertPrefabNode(GameObject root)
        {
            PrefabUtility.RevertPrefabInstance(root, InteractionMode.UserAction);
        }
    }
}
#endif
