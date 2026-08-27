// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Ember.Basic;
using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    public class BatchRenamerEditor : EmberEditorWindow
    {
        private const string TAG = LogTags.EmberBasic + "." + nameof(BatchRenamerEditor);

        protected override string MenuPath => "Ember/Tool/批量重命名";
        protected override string WindowTitle => "批量重命名";
        protected override Vector2 WindowSize => new(500, 700);
        protected override string WindowVersion => "v2.0";

        // ---- 重命名参数 ----
        public string BaseName = "NewName";
        public string Prefix = "";
        public string Suffix = "";
        public int StartNumber;
        public int DigitCount = 2;

        // ---- 目标 ----
        [HideInInspector]
        public DefaultAsset FolderAsset;

        private string FolderPath
        {
            get
            {
                if (_cachedFolderPath == null && FolderAsset != null)
                    _cachedFolderPath = AssetDatabase.GetAssetPath(FolderAsset);
                return _cachedFolderPath;
            }
        }
        private string _cachedFolderPath;

        // ---- 内部 ----
        [HideInInspector] public List<UnityEngine.Object> PendingTargets;

        private static List<UnityEngine.Object> s_queuedSelection;

        // ======== 菜单入口 ========

        [MenuItem("Ember/Tool/批量重命名", false, 100)]
        public static void ShowWindow() => OpenWithTargets(null);

        [MenuItem("Assets/Ember/批量重命名", false, 1000)]
        public static void ShowWindowFromAssets()
        {
            if (Selection.objects.Length == 1)
            {
                string path = AssetDatabase.GetAssetPath(Selection.objects[0]);
                if (AssetDatabase.IsValidFolder(path))
                {
                    var win = GetWindow<BatchRenamerEditor>();
                    win.minSize = win.WindowSize;
                    win.FolderAsset = (DefaultAsset)Selection.objects[0];
                    win.Show();
                    return;
                }
            }
            s_queuedSelection = new List<UnityEngine.Object>(Selection.objects);
            OpenWithTargets(s_queuedSelection);
        }

        [MenuItem("Assets/Ember/批量重命名", true)]
        public static bool ShowWindowFromAssetsValidate() => Selection.objects.Length > 0;

        [MenuItem("GameObject/Ember/批量重命名", false, 1000)]
        public static void ShowWindowFromHierarchy()
        {
            s_queuedSelection = new List<UnityEngine.Object>(Selection.gameObjects);
            OpenWithTargets(s_queuedSelection);
        }

        [MenuItem("GameObject/Ember/批量重命名", true)]
        public static bool ShowWindowFromHierarchyValidate() => Selection.gameObjects.Length > 0;

        private static void OpenWithTargets(List<UnityEngine.Object> targets)
        {
            var win = GetWindow<BatchRenamerEditor>();
            win.minSize = win.WindowSize;
            if (targets != null && targets.Count > 0)
                win.PendingTargets = targets;
            win.Show();
        }

        // ======== Lifecycle ========

        protected override void OnEnable()
        {
            base.OnEnable();
            if (s_queuedSelection != null && s_queuedSelection.Count > 0)
            {
                PendingTargets = new List<UnityEngine.Object>(s_queuedSelection);
                s_queuedSelection = null;
            }
        }

        // ======== UI ========

        protected override void DrawContent()
        {
            DrawNamingRules();
            EditorGUILayout.Space(5);
            DrawFolderPicker();
            EditorGUILayout.Space(5);
            DrawPreview();
            EditorGUILayout.Space(5);
            DrawRenameButton();
        }

        private void DrawNamingRules()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(L10n("Naming Rules", "命名规则"), EditorStyles.miniBoldLabel);

            BaseName = EditorGUILayout.TextField(L10n("Base Name", "基础名称"), BaseName);

            EditorGUILayout.BeginHorizontal();
            Prefix = EditorGUILayout.TextField(L10n("Prefix", "前缀"), Prefix);
            Suffix = EditorGUILayout.TextField(L10n("Suffix", "后缀"), Suffix);
            EditorGUILayout.EndHorizontal();

            StartNumber = EditorGUILayout.IntField(L10n("Start Number", "起始编号"), StartNumber);
            DigitCount = EditorGUILayout.IntSlider(L10n("Digit Count", "编号位数"), DigitCount, 1, 10);

            EditorGUILayout.EndVertical();
        }

        private void DrawFolderPicker()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(L10n("Target Folder (Drag & Drop)", "目标文件夹 (可拖拽)"), EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            FolderAsset = (DefaultAsset)EditorGUILayout.ObjectField(FolderAsset, typeof(DefaultAsset), false, GUILayout.Height(20));
            if (EditorGUI.EndChangeCheck())
            {
                _cachedFolderPath = null;
                if (FolderAsset != null && !AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(FolderAsset)))
                { FolderAsset = null; EmberDebug.LogWarning(TAG, "Not a folder!"); }
            }

            if (GUILayout.Button(L10n("Select", "选择"), GUILayout.Width(50), GUILayout.Height(20)))
            {
                string p = EditorUtility.OpenFolderPanel(L10n("Select Folder", "选择文件夹"), "Assets", "");
                if (!string.IsNullOrEmpty(p))
                {
                    _cachedFolderPath = GetRelativePath(p);
                    FolderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(_cachedFolderPath);
                }
            }
            if (FolderAsset != null && GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
            { FolderAsset = null; _cachedFolderPath = null; }

            EditorGUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(FolderPath))
            {
                EditorGUILayout.LabelField(FolderPath, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.HelpBox(L10n("Folder rename mode enabled.", "已启用文件夹重命名模式，选中物体将被忽略。"), MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPreview()
        {
            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.LabelField(L10n("Preview:", "预览:"), EditorStyles.miniBoldLabel);
            string fmt = "D" + DigitCount;
            string name = "";
            if (!string.IsNullOrEmpty(Prefix)) name += Prefix + "_";
            name += BaseName + "_" + StartNumber.ToString(fmt);
            if (!string.IsNullOrEmpty(Suffix)) name += "_" + Suffix;
            EditorGUILayout.LabelField(name, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawRenameButton()
        {
            bool hasFolder = !string.IsNullOrEmpty(FolderPath);
            bool hasPending = PendingTargets != null && PendingTargets.Count > 0;
            bool canRename = hasFolder || hasPending;

            EditorGUI.BeginDisabledGroup(!canRename);
            string label = hasFolder
                ? L10n("RENAME FOLDER CONTENT", "重命名文件夹内容")
                : hasPending
                    ? string.Format(L10n("RENAME {0} OBJECTS", "重命名 {0} 个物体"), PendingTargets.Count)
                    : L10n("SELECT OBJECTS OR FOLDER", "请选中物体或文件夹");

            if (GUILayout.Button(label, BigButtonStyle))
                ExecuteRename();

            EditorGUI.EndDisabledGroup();
        }

        // ======== 核心逻辑 ========

        private void ExecuteRename()
        {
            string fmt = "D" + DigitCount;
            var targets = new List<UnityEngine.Object>();

            if (!string.IsNullOrEmpty(FolderPath))
            {
                foreach (var g in AssetDatabase.FindAssets("", new[] { FolderPath }))
                {
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    if (Path.GetDirectoryName(p).Replace("\\", "/") == FolderPath)
                    {
                        var a = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p);
                        if (a) targets.Add(a);
                    }
                }
            }
            else if (PendingTargets != null)
            {
                targets.AddRange(PendingTargets);
            }

            if (targets.Count == 0) return;
            targets.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            int need = (StartNumber + targets.Count - 1).ToString().Length;
            if (need > DigitCount)
            {
                int choice = EditorUtility.DisplayDialogComplex(
                    L10n("Warning", "位数警告"),
                    L10n(
                        $"You are renaming {targets.Count} objects but the digit count ({DigitCount}) is too small. Needs {need} digits. Fix automatically?",
                        $"正在重命名 {targets.Count} 个物体，但编号位数 ({DigitCount}) 不足，需要 {need} 位编号。是否自动修正？"),
                    L10n("Auto-Fix", "自动修正"),
                    L10n("Cancel", "取消"),
                    L10n("Keep", "保持"));
                if (choice == 1) return;
                if (choice == 0) { DigitCount = need; fmt = "D" + DigitCount; }
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                int i = 0;
                foreach (var obj in targets)
                {
                    string n = GenerateName(i++, fmt);
                    if (obj is GameObject go && !AssetDatabase.Contains(go))
                    { Undo.RecordObject(go, "Rename"); go.name = n; }
                    else
                    { string p = AssetDatabase.GetAssetPath(obj); if (!string.IsNullOrEmpty(p)) AssetDatabase.RenameAsset(p, n); }
                }
                if (!string.IsNullOrEmpty(FolderPath))
                {
                    string fn = "";
                    if (!string.IsNullOrEmpty(Prefix)) fn += Prefix + "_";
                    fn += BaseName;
                    if (!string.IsNullOrEmpty(Suffix)) fn += "_" + Suffix;
                    AssetDatabase.RenameAsset(FolderPath, fn);
                    FolderAsset = null; _cachedFolderPath = null;
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            PendingTargets = null;
            EditorUtility.DisplayDialog(L10n("Success", "成功"), L10n("Rename Task Done", "重命名完成"), "OK");
        }

        private string GenerateName(int idx, string fmt)
        {
            string r = "";
            if (!string.IsNullOrEmpty(Prefix)) r += Prefix + "_";
            r += BaseName + "_" + (StartNumber + idx).ToString(fmt);
            if (!string.IsNullOrEmpty(Suffix)) r += "_" + Suffix;
            return r;
        }

        private static string GetRelativePath(string p)
        {
            string n = p.Replace("\\", "/");
            return n.Contains("Assets/") ? n.Substring(n.IndexOf("Assets/")) : "Assets";
        }
    }
}
#endif
