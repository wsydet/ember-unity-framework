// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ember.Basic;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 丢失脚本查找与清理工具 —— 指定一个 Prefab/场景物体，扫出所有 Missing Script 并一键移除。
    /// </summary>
    public class MissingScriptFinderWindow : EmberEditorWindow
    {
        private const string TAG = LogTags.EmberBasic + "." + nameof(MissingScriptFinderWindow);
        protected override string MenuPath => "Ember/Tool/丢失脚本清理工具";
        protected override string WindowTitle => "Missing Script Finder";
        protected override Vector2 WindowSize => new(500, 700);

        public GameObject TargetObject;

        [Serializable]
        public class ScriptInfo
        {
            public GameObject Obj;
            public string Name;
        }

        public List<ScriptInfo> MissingList = new();

        private Vector2 _scrollPos;

        // ======== 菜单 ========

        [MenuItem("Ember/Tool/丢失脚本清理工具", false, 180)]
        private static void OpenWindow()
        {
            var win = GetWindow<MissingScriptFinderWindow>();
            win.minSize = win.WindowSize;
            win.Show();
        }

        [MenuItem("GameObject/Ember/查找丢失脚本", false, 1600)]
        public static void QuickScan()
        {
            var go = Selection.activeGameObject;
            if (!go) return;
            int count = 0;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                foreach (var c in t.GetComponents<Component>())
                {
                    if (c == null) { count++; break; }
                }
            }
            if (count > 0)
            {
                var win = GetWindow<MissingScriptFinderWindow>();
                win.TargetObject = go;
                win.Scan();
                win.Show();
            }
            else
                EditorUtility.DisplayDialog("Ember", "未发现丢失脚本。", "OK");
        }

        [MenuItem("GameObject/Ember/查找丢失脚本", true, 1600)]
        public static bool QuickScanValidate() => Selection.activeGameObject;

        // ======== UI ========

        protected override void DrawContent()
        {
            // 目标区
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(L10n("Target Object (Prefab/Scene)", "目标物体 (预制体/场景节点)"), EditorStyles.miniBoldLabel);
            TargetObject = (GameObject)EditorGUILayout.ObjectField(TargetObject, typeof(GameObject), true);
            EditorGUILayout.EndVertical();

            DrawSeparatorLine();

            // 操作按钮
            DrawActionButtons();

            // 结果列表
            if (MissingList.Count > 0)
            {
                DrawSeparatorLine();
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(L10n("Missing Scripts", "丢失脚本列表") + $" ({MissingList.Count})", EditorStyles.boldLabel);

                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(300));
                foreach (var info in MissingList)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(info.Name, EditorStyles.wordWrappedLabel);
                    if (GUILayout.Button(L10n("Ping", "定位"), GUILayout.Width(50)))
                    {
                        if (info.Obj) { EditorGUIUtility.PingObject(info.Obj); Selection.activeGameObject = info.Obj; }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawActionButtons()
        {
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.5f);
            if (GUILayout.Button(new GUIContent(L10n(" Scan Missing Scripts", " 扫描丢失脚本"), EditorGUIUtility.IconContent("d_ViewToolOrbit").image), GUILayout.Height(40)))
                Scan();

            if (MissingList.Count > 0)
            {
                EditorGUILayout.Space(5);
                GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
                if (GUILayout.Button(new GUIContent(L10n(" Remove All Missing Scripts", " 移除所有丢失脚本"), EditorGUIUtility.IconContent("d_TreeEditor.Trash").image), GUILayout.Height(40)))
                    RemoveAll();
            }
            GUI.backgroundColor = Color.white;
        }

        // ======== 核心逻辑 ========

        private void Scan()
        {
            MissingList.Clear();
            if (!TargetObject) return;
            ScanHierarchy(TargetObject);
            EmberDebug.Log(TAG, $"[Ember] Found {MissingList.Count} missing script(s) in '{TargetObject.name}'.");
        }

        private void ScanHierarchy(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                foreach (var c in t.GetComponents<Component>())
                {
                    if (c == null) { MissingList.Add(new ScriptInfo { Obj = t.gameObject, Name = t.gameObject.name }); break; }
                }
            }
        }

        private void RemoveAll()
        {
            if (!TargetObject || MissingList.Count == 0) return;
            if (!EditorUtility.DisplayDialog(L10n("Confirm", "确认移除"),
                L10n("Remove all missing scripts? This can be undone.", "确定要移除所有丢失的脚本吗？此操作可以撤销。"),
                L10n("Yes", "确定"), L10n("Cancel", "取消")))
                return;

            Undo.RegisterFullObjectHierarchyUndo(TargetObject, "Remove Missing Scripts");
            int removed = 0;
            foreach (var info in MissingList)
            {
                if (info.Obj)
                    removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(info.Obj);
            }

            if (PrefabUtility.IsPartOfAnyPrefab(TargetObject))
                PrefabUtility.RecordPrefabInstancePropertyModifications(TargetObject);

            AssetDatabase.SaveAssets();
            EmberDebug.Log(TAG, $"[Ember] Removed {removed} missing script(s).");
            Scan();
        }
    }
}
#endif
