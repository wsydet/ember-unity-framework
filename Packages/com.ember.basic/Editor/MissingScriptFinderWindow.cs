// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 丢失脚本查找与清理工具 —— 指定一个 Prefab/场景物体，扫出所有 Missing Script 并一键移除。
    ///
    /// 与 SystemCleanerEditor 的区别：SystemCleaner 扫描整个场景；本工具针对单个目标层级精准操作。
    /// 适用场景：预制体保存时报 "有丢失脚本"，拖进来修。
    /// </summary>
    public class MissingScriptFinderWindow : EmberEditorWindow
    {
        protected override string MenuPath => "Tools/Ember/丢失脚本清理工具";
        protected override string WindowTitle => "Missing Script Finder";
        protected override Vector2 WindowSize => new(600, 700);

        [BoxGroup("目标"), LabelText("$TargetLabel")]
        public GameObject TargetObject;

        private string TargetLabel => L10n("Target Object (Prefab/Scene)", "目标物体 (预制体/场景节点)");

        [Serializable]
        public class ScriptInfo
        {
            [HideInInspector] public GameObject Obj;
            [HideInInspector] public string Name;

            [Button("$PingLabel")]
            public void Ping()
            {
                if (Obj) { EditorGUIUtility.PingObject(Obj); Selection.activeGameObject = Obj; }
            }
            private string PingLabel => "定位";
        }

        [BoxGroup("结果"), LabelText("$ResultLabel")]
        [ListDrawerSettings(IsReadOnly = true, ShowPaging = false)]
        public List<ScriptInfo> MissingList = new();

        private string ResultLabel => L10n("Missing Scripts", "丢失脚本列表");

        // ======== 菜单 ========

        [MenuItem("Tools/Ember/丢失脚本清理工具")]
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
            int count = ScanHierarchy(go);
            if (count > 0)
            {
                // 打开窗口并填充
                var win = GetWindow<MissingScriptFinderWindow>();
                win.TargetObject = go;
                win.Scan();
                win.Show();
            }
            else
                EditorUtility.DisplayDialog("Ember", "未发现丢失脚本。", "OK");
        }

        [MenuItem("GameObject/Ember/查找丢失脚本", true)]
        public static bool QuickScanValidate() => Selection.activeGameObject;

        // ======== UI ========

        protected override void DrawContent()
        {
            // Odin 自动绘制 BoxGroup 字段
            DrawSeparatorLine();
            DrawActionButtons();
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
            int count = ScanHierarchy(TargetObject);
            Debug.Log($"[Ember] Found {count} missing script(s) in '{TargetObject.name}'.");
        }

        private static int ScanHierarchy(GameObject root)
        {
            int count = 0;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                foreach (var c in t.GetComponents<Component>())
                {
                    if (c == null) { count++; break; }
                }
            }
            return count;
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
            Debug.Log($"[Ember] Removed {removed} missing script(s).");
            Scan(); // 刷新列表
        }
    }
}
#endif
