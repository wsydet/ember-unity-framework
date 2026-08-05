// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Ember.Basic;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// Sprite 帧文件夹替换工具 —— 将旧文件夹的 Sprite 帧替换为新文件夹中的对应帧，
    /// 保留 GUID 以维持 AnimationClip 等资源的引用不丢失。
    /// </summary>
    public class SpriteFrameFolderReplacerTool : EmberEditorWindow
    {
        private const string TAG = LogTags.EmberBasic + "." + nameof(SpriteFrameFolderReplacerTool);
        protected override string MenuPath => "Ember/Tool/Sprite 帧替换";
        protected override string WindowTitle => "Sprite Frame Replacer";
        protected override Vector2 WindowSize => new(580, 850);

        // ---- 文件夹 ----
        private DefaultAsset _oldFolderAsset;
        private DefaultAsset _newFolderAsset;
        private string _oldFolderPath = "";
        private string _newFolderPath = "";

        // ---- 选项 ----
        private enum ImportMode { ReferenceSafeSingle, CopyNewMetaKeepOldGuid, KeepOldMetaApplyCustom }
        private int _importMode;
        private bool _rewriteAnimClips;
        private bool _createBackup = true;
        private string _previewMsg = "选好新旧文件夹后点刷新。";
        private Vector2 _scrollPos;

        // ---- 自定义导入参数 ----
        private bool _useCustomImport;
        private int _maxSize = 2048;
        private int _formatIdx;
        private int _alphaSrcIdx = 1;

        // ======== 菜单 ========

        [MenuItem("Ember/Tool/Sprite 帧替换", false, 220)]
        public static void ShowWindow()
        {
            var win = GetWindow<SpriteFrameFolderReplacerTool>();
            win.minSize = win.WindowSize;
            win.Show();
        }

        [MenuItem("Assets/Ember/Sprite 帧替换", false, 2200)]
        public static void QuickOpen()
        {
            var win = GetWindow<SpriteFrameFolderReplacerTool>();
            win.minSize = win.WindowSize;
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (AssetDatabase.IsValidFolder(path))
            {
                if (string.IsNullOrEmpty(win._oldFolderPath)) { win._oldFolderPath = path; win._oldFolderAsset = Selection.activeObject as DefaultAsset; }
                else { win._newFolderPath = path; win._newFolderAsset = Selection.activeObject as DefaultAsset; }
            }
            win.Show();
        }

        [MenuItem("Assets/Ember/Sprite 帧替换", true)]
        public static bool QuickOpenValidate() => Selection.activeObject && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));

        // ======== UI ========

        protected override void DrawContent()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(L10n("1. Folders", "1. 文件夹"), EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _oldFolderAsset = (DefaultAsset)EditorGUILayout.ObjectField(L10n("Old Folder (current)", "旧文件夹 (当前)"), _oldFolderAsset, typeof(DefaultAsset), false);
            if (EditorGUI.EndChangeCheck()) _oldFolderPath = _oldFolderAsset ? AssetDatabase.GetAssetPath(_oldFolderAsset) : "";

            EditorGUI.BeginChangeCheck();
            _newFolderAsset = (DefaultAsset)EditorGUILayout.ObjectField(L10n("New Folder (replacement)", "新文件夹 (替换)"), _newFolderAsset, typeof(DefaultAsset), false);
            if (EditorGUI.EndChangeCheck()) _newFolderPath = _newFolderAsset ? AssetDatabase.GetAssetPath(_newFolderAsset) : "";
            EditorGUILayout.EndVertical();

            if (string.IsNullOrEmpty(_oldFolderPath) || string.IsNullOrEmpty(_newFolderPath))
            { EditorGUILayout.HelpBox("请选择新旧两个 Sprite 帧文件夹。", MessageType.Info); return; }

            DrawSeparatorLine();

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(L10n("2. Options", "2. 选项"), EditorStyles.boldLabel);
            _importMode = EditorGUILayout.Popup(L10n("Import Mode", "导入模式"), _importMode,
                new[] { "Safe Single Reimport", "Copy Meta + Keep Old GUID", "Keep Old Meta + Custom" });
            _rewriteAnimClips = EditorGUILayout.Toggle(L10n("Rewrite AnimationClip references", "重写 AnimationClip 引用"), _rewriteAnimClips);
            _createBackup = EditorGUILayout.Toggle(L10n("Create backup", "创建备份"), _createBackup);

            _useCustomImport = EditorGUILayout.Toggle(L10n("Custom Import Settings", "自定义导入参数"), _useCustomImport);
            if (_useCustomImport)
            {
                _maxSize = EditorGUILayout.IntPopup("Max Size", _maxSize, SpriteImportUtility.MaxSizeOptions.Select(o => o.ToString()).ToArray(), SpriteImportUtility.MaxSizeOptions);
                _formatIdx = EditorGUILayout.Popup("Format", _formatIdx, SpriteImportUtility.FormatLabels);
                _alphaSrcIdx = EditorGUILayout.Popup("Alpha", _alphaSrcIdx, new[] { "None", "From Input", "From GrayScale" });
            }
            EditorGUILayout.EndVertical();

            DrawSeparatorLine();

            EditorGUILayout.HelpBox(_previewMsg, MessageType.Info);

            if (GUILayout.Button(L10n("Refresh Preview", "刷新预览"))) RefreshPreview();

            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.5f);
            if (GUILayout.Button(L10n("Execute Replacement", "执行替换"), GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog(L10n("Confirm", "确认"),
                    L10n($"Replace sprites in '{Path.GetFileName(_oldFolderPath)}' with '{Path.GetFileName(_newFolderPath)}'?", $"将 '{Path.GetFileName(_oldFolderPath)}' 中的 Sprite 替换为 '{Path.GetFileName(_newFolderPath)}' 中的对应帧？"),
                    L10n("Yes", "确定"), L10n("Cancel", "取消")))
                    Execute();
            }
            GUI.backgroundColor = Color.white;
        }

        // ======== 核心逻辑 ========

        private void RefreshPreview()
        {
            if (string.IsNullOrEmpty(_oldFolderPath) || string.IsNullOrEmpty(_newFolderPath)) return;

            var oldFrames = ScanFrames(_oldFolderPath);
            var newFrames = ScanFrames(_newFolderPath);

            if (oldFrames.Count == 0 || newFrames.Count == 0)
            { _previewMsg = L10n("No image frames found in one or both folders.", "一个或两个文件夹中没有找到图片帧。"); return; }

            int frameDiff = newFrames.Count - oldFrames.Count;
            _previewMsg = string.Format(L10n(
                $"Old: {oldFrames.Count} frames, New: {newFrames.Count} frames. Frame diff: {frameDiff:+0;-0;0}.\nReady to replace.",
                $"旧: {oldFrames.Count} 帧, 新: {newFrames.Count} 帧。帧数差: {frameDiff:+0;-0;0}。\n准备就绪。"));
        }

        private static List<string> ScanFrames(string folder)
        {
            var result = new List<string>();
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetDirectoryName(p).Replace("\\", "/") == folder)
                    result.Add(p);
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private void Execute()
        {
            var oldFrames = ScanFrames(_oldFolderPath);
            var newFrames = ScanFrames(_newFolderPath);
            int count = Mathf.Min(oldFrames.Count, newFrames.Count);

            // Backup
            if (_createBackup)
            {
                var backupRoot = Path.Combine(Application.dataPath, "../SpriteFrameBackup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                Directory.CreateDirectory(backupRoot);
                foreach (var f in oldFrames)
                {
                    var rel = SpriteImportUtility.ToAssetPath(f).Substring("Assets/".Length);
                    var dest = Path.Combine(backupRoot, rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    File.Copy(f, dest, true);
                    var meta = f + ".meta";
                    if (File.Exists(meta)) File.Copy(meta, dest + ".meta", true);
                }
                EmberDebug.Log(TAG, $"[Ember] Backup created: {backupRoot}");
            }

            // Replace
            var rewrittenClips = new HashSet<string>();
            try
            {
                for (int i = 0; i < count; i++)
                {
                    EditorUtility.DisplayProgressBar("Replacing Sprites", Path.GetFileName(oldFrames[i]), (float)i / count);
                    ReplaceFrame(oldFrames[i], newFrames[i], _importMode, _useCustomImport, _maxSize, _formatIdx, _alphaSrcIdx);
                    if (_rewriteAnimClips)
                        RewriteAnimClipRefs(Path.GetFileNameWithoutExtension(oldFrames[i]), rewrittenClips);
                }
            }
            finally { EditorUtility.ClearProgressBar(); AssetDatabase.Refresh(); }

            EditorUtility.DisplayDialog("完成",
                string.Format(L10n($"Replaced {count} frames.", $"已替换 {count} 帧。")), "OK");
        }

        private static void ReplaceFrame(string oldPath, string newPath, int mode, bool customImport, int maxSize, int fmt, int alpha)
        {
            var oldGuid = AssetDatabase.AssetPathToGUID(oldPath);
            var newGuid = AssetDatabase.AssetPathToGUID(newPath);

            if (mode == 0) // ReferenceSafeSingle
            {
                // Read new sprite data, apply to old via secondary texture hack — simplified: just reimport old with new texture
                var oldBytes = File.ReadAllBytes(newPath);
                File.WriteAllBytes(oldPath, oldBytes);
                AssetDatabase.ImportAsset(oldPath);
            }
            else if (mode == 1) // CopyNewMetaKeepOldGuid
            {
                var oldMeta = File.ReadAllText(oldPath + ".meta");
                var newMeta = File.ReadAllText(newPath + ".meta");
                // Replace GUID in new meta with old GUID
                var oldGuidMatch = Regex.Match(oldMeta, @"guid:\s*([0-9a-fA-F]{32})");
                if (oldGuidMatch.Success)
                {
                    newMeta = Regex.Replace(newMeta, @"guid:\s*[0-9a-fA-F]{32}", $"guid: {oldGuidMatch.Groups[1].Value}");
                    File.WriteAllText(newPath + ".meta", newMeta);
                }
                File.Copy(newPath, oldPath, true);
                File.Copy(newPath + ".meta", oldPath + ".meta", true);
                AssetDatabase.ImportAsset(oldPath);
            }
            else // KeepOldMetaApplyCustom
            {
                File.Copy(newPath, oldPath, true);
                AssetDatabase.ImportAsset(oldPath);
            }

            if (customImport)
            {
                var importer = AssetImporter.GetAtPath(oldPath) as TextureImporter;
                if (importer)
                {
                    importer.maxTextureSize = maxSize;
                    importer.alphaSource = SpriteImportUtility.ToAlphaSource(alpha);
                    var plat = importer.GetPlatformTextureSettings("Default");
                    plat.format = SpriteImportUtility.FormatOptions[fmt];
                    importer.SetPlatformTextureSettings(plat);
                    importer.SaveAndReimport();
                }
            }
        }

        private static void RewriteAnimClipRefs(string frameName, HashSet<string> rewritten)
        {
            var guids = AssetDatabase.FindAssets("t:AnimationClip");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (!rewritten.Add(path)) continue;
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (!clip) continue;
                var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                bool changed = false;
                foreach (var b in bindings)
                {
                    var curve = AnimationUtility.GetObjectReferenceCurve(clip, b);
                    if (curve == null) continue;
                    for (int i = 0; i < curve.Length; i++)
                    {
                        if (curve[i].value is Sprite s && s.name.StartsWith(frameName))
                        { changed = true; break; }
                    }
                }
                if (changed) { EditorUtility.SetDirty(clip); EmberDebug.Log(TAG, $"[Ember] Marked for rewrite: {path}"); }
            }
        }
    }
}
#endif
