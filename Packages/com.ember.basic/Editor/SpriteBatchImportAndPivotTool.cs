// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 批量修改 Sprite 导入参数和锚点 —— 选文件夹，一键设 Pivot、压缩格式、MaxSize 等。
    /// 按尺寸分组，可以只改某个尺寸组的 Sprite。
    /// </summary>
    public class SpriteBatchImportAndPivotTool : EmberEditorWindow
    {
        protected override string MenuPath => "Ember/Tool/批量修改 Sprite 锚点";
        protected override string WindowTitle => "Sprite Batch Import & Pivot";
        protected override Vector2 WindowSize => new(580, 750);

        // ---- 文件夹 ----
        private DefaultAsset _folderAsset;
        private string _folderPath = "";
        private List<string> _previewPaths = new();
        private List<Vector2Int> _sizeGroups = new();
        private Vector2Int _selectedSize;
        private bool _onlySelectedSize = true;

        // ---- 参考 Sprite ----
        private Sprite _referenceSprite;

        // ---- 导入参数 ----
        private bool _overrideSize = true;
        private int _maxSize = 2048;
        private bool _overrideFormat = true;
        private int _formatIdx;
        private bool _overrideAlpha = true;
        private int _alphaSrcIdx = 1;
        private bool _overrideResize = true;
        private int _resizeAlgoIdx;
        private bool _overrideMipmaps = true;
        private bool _mipmaps;

        // ---- 锚点 ----
        private bool _applyPivot = true;
        private SpriteAlignment _pivotAlign = SpriteAlignment.Center;
        private Vector2 _customPivot = new(0.5f, 0.5f);

        // ======== 菜单 ========

        [MenuItem("Ember/Tool/批量修改 Sprite 锚点", false, 230)]
        public static void ShowWindow()
        {
            var win = GetWindow<SpriteBatchImportAndPivotTool>();
            win.minSize = win.WindowSize;
            win.Show();
        }

        [MenuItem("Assets/Ember/批量修改 Sprite 锚点", false, 2100)]
        public static void QuickOpenFromFolder()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!AssetDatabase.IsValidFolder(path)) return;
            var win = GetWindow<SpriteBatchImportAndPivotTool>();
            win.minSize = win.WindowSize;
            win._folderPath = path;
            win._folderAsset = Selection.activeObject as DefaultAsset;
            win.RefreshPreview();
            win.Show();
        }

        [MenuItem("Assets/Ember/批量修改 Sprite 锚点", true)]
        public static bool QuickOpenValidate()
        {
            var obj = Selection.activeObject;
            return obj && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(obj));
        }

        // ======== Lifecycle ========

        protected override void OnEnable()
        {
            var saved = EditorPrefs.GetString("Ember_SpritePivot_Folder", "");
            if (!string.IsNullOrEmpty(saved)) { _folderPath = saved; _folderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(saved); RefreshPreview(); }
        }

        // ======== UI ========

        protected override void DrawContent()
        {
            // 1. 文件夹
            EditorGUI.BeginChangeCheck();
            _folderAsset = (DefaultAsset)EditorGUILayout.ObjectField(L10n("Sprite Folder", "Sprite 文件夹"), _folderAsset, typeof(DefaultAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                _folderPath = _folderAsset ? AssetDatabase.GetAssetPath(_folderAsset) : "";
                if (!string.IsNullOrEmpty(_folderPath)) { EditorPrefs.SetString("Ember_SpritePivot_Folder", _folderPath); RefreshPreview(); }
            }

            if (string.IsNullOrEmpty(_folderPath)) { EditorGUILayout.HelpBox("选择一个 Assets 内的 Sprite 文件夹。", MessageType.Info); return; }

            // 2. 参考 Sprite（可选）
            EditorGUILayout.Space(5);
            _referenceSprite = (Sprite)EditorGUILayout.ObjectField(L10n("Reference Sprite (optional)", "参考 Sprite（可选）"), _referenceSprite, typeof(Sprite), false);
            if (_referenceSprite && GUILayout.Button(L10n("Read Settings from Reference", "从参考 Sprite 读取设置")))
                ReadFromReference();

            DrawSeparatorLine();

            // 3. 尺寸组
            string sizeLabel = _onlySelectedSize
                ? string.Format(L10n("Target Size: {0}x{1} ({2} sprites)", "目标尺寸: {0}x{1} ({2} 个 Sprite)"), _selectedSize.x, _selectedSize.y, _previewPaths.Count)
                : string.Format(L10n("All Sizes ({0} sprites)", "所有尺寸 ({0} 个 Sprite)"), _previewPaths.Count);
            EditorGUILayout.LabelField(sizeLabel, EditorStyles.boldLabel);

            if (_sizeGroups.Count > 1)
            {
                _onlySelectedSize = EditorGUILayout.ToggleLeft(L10n("Only modify selected size group", "只修改选中的尺寸组"), _onlySelectedSize);
                if (_onlySelectedSize)
                {
                    int idx = _sizeGroups.IndexOf(_selectedSize);
                    if (idx < 0) idx = 0;
                    var names = _sizeGroups.Select(s => $"{s.x}x{s.y}").ToArray();
                    idx = EditorGUILayout.Popup("尺寸组", idx, names);
                    if (idx >= 0 && idx < _sizeGroups.Count) { _selectedSize = _sizeGroups[idx]; RefreshPreview(); }
                }
            }

            DrawSeparatorLine();

            // 4. 导入参数
            EditorGUILayout.LabelField(L10n("Import Settings Override", "导入参数覆写"), EditorStyles.boldLabel);

            _overrideSize = EditorGUILayout.ToggleLeft("Max Size", _overrideSize);
            if (_overrideSize) _maxSize = EditorGUILayout.IntPopup("", _maxSize, SpriteImportUtility.MaxSizeOptions.Select(o => o.ToString()).ToArray(), SpriteImportUtility.MaxSizeOptions);

            _overrideFormat = EditorGUILayout.ToggleLeft("Format", _overrideFormat);
            if (_overrideFormat) _formatIdx = EditorGUILayout.Popup("", _formatIdx, SpriteImportUtility.FormatLabels);

            _overrideAlpha = EditorGUILayout.ToggleLeft("Alpha Source", _overrideAlpha);
            if (_overrideAlpha) _alphaSrcIdx = EditorGUILayout.Popup("", _alphaSrcIdx, new[] { "None", "From Input", "From GrayScale" });

            _overrideResize = EditorGUILayout.ToggleLeft("Resize Algorithm", _overrideResize);
            if (_overrideResize) _resizeAlgoIdx = EditorGUILayout.Popup("", _resizeAlgoIdx, SpriteImportUtility.ResizeAlgorithmLabels);

            _overrideMipmaps = EditorGUILayout.ToggleLeft("Mipmaps", _overrideMipmaps);
            if (_overrideMipmaps) _mipmaps = EditorGUILayout.Toggle("", _mipmaps);

            DrawSeparatorLine();

            // 5. 锚点
            _applyPivot = EditorGUILayout.ToggleLeft(L10n("Apply Pivot", "修改锚点"), _applyPivot);
            if (_applyPivot)
            {
                int pivotIdx = Array.IndexOf(SpriteImportUtility.PivotAlignments, _pivotAlign);
                if (pivotIdx < 0) pivotIdx = SpriteImportUtility.PivotAlignments.Length - 1;
                pivotIdx = EditorGUILayout.Popup(L10n("Pivot Mode", "锚点模式"), pivotIdx, SpriteImportUtility.PivotLabels);
                _pivotAlign = SpriteImportUtility.PivotAlignments[Mathf.Clamp(pivotIdx, 0, SpriteImportUtility.PivotAlignments.Length - 1)];
                if (_pivotAlign == SpriteAlignment.Custom)
                    _customPivot = EditorGUILayout.Vector2Field(L10n("Custom", "自定义坐标"), _customPivot);
            }

            DrawSeparatorLine();

            // 6. 执行
            if (GUILayout.Button(L10n("Apply Settings to All Sprites", "批量应用导入参数和锚点"), BigButtonStyle))
                Execute();
        }

        // ======== 核心逻辑 ========

        private void RefreshPreview()
        {
            _previewPaths.Clear();
            _sizeGroups.Clear();
            if (string.IsNullOrEmpty(_folderPath)) return;

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { _folderPath });
            var sizeMap = new Dictionary<Vector2Int, List<string>>();

            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                var ext = Path.GetExtension(p);
                if (!SpriteImportUtility.ImageExtensions.Contains(ext)) continue;

                foreach (var sz in SpriteImportUtility.GetSpriteSizes(p))
                {
                    if (!sizeMap.ContainsKey(sz)) sizeMap[sz] = new List<string>();
                    sizeMap[sz].Add(p);
                }
            }

            _sizeGroups = sizeMap.Keys.OrderByDescending(k => k.x * k.y).ToList();
            if (_sizeGroups.Count > 0 && (_selectedSize == default || !_sizeGroups.Contains(_selectedSize)))
                _selectedSize = _sizeGroups[0];

            if (_onlySelectedSize && sizeMap.TryGetValue(_selectedSize, out var paths))
                _previewPaths = paths;
            else
                _previewPaths = sizeMap.Values.SelectMany(v => v).Distinct().ToList();
        }

        private void ReadFromReference()
        {
            if (!_referenceSprite) return;
            var path = AssetDatabase.GetAssetPath(_referenceSprite.texture);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) return;

            _maxSize = SpriteImportUtility.GetTextureSize(path).x;
            _formatIdx = Mathf.Clamp(Array.IndexOf(SpriteImportUtility.FormatOptions, importer.GetPlatformTextureSettings("Default").format), 0, SpriteImportUtility.FormatOptions.Length - 1);
            _alphaSrcIdx = SpriteImportUtility.FromAlphaSource(importer.alphaSource);
            _mipmaps = importer.mipmapEnabled;
            _overrideSize = _overrideFormat = _overrideAlpha = _overrideMipmaps = true;

            if (_applyPivot)
            {
                var rect = _referenceSprite.rect;
                Vector2 norm = new(0.5f, 0.5f);
                if (rect.width > 0 && rect.height > 0)
                {
                    var texSize = SpriteImportUtility.GetTextureSize(path);
                    if (texSize.x > 0 && texSize.y > 0)
                    {
                        var piv = _referenceSprite.pivot;
                        norm = new Vector2(piv.x / rect.width, piv.y / rect.height);
                    }
                }
                _pivotAlign = SpriteAlignment.Custom;
                _customPivot = norm;
            }
        }

        private void Execute()
        {
            string targetSizeDesc = _applyPivot && _onlySelectedSize ? $"\n目标锚点尺寸：{_selectedSize.x} x {_selectedSize.y}" : "";
            if (!EditorUtility.DisplayDialog("确认批量修改",
                $"将修改 {_previewPaths.Count} 个图片文件。{targetSizeDesc}\n\n注意：会直接覆盖现有导入参数和锚点。是否继续？", "继续", "取消"))
                return;

            int touched = 0, pivotCount = 0;
            try
            {
                for (int i = 0; i < _previewPaths.Count; i++)
                {
                    var p = _previewPaths[i];
                    EditorUtility.DisplayProgressBar("批量修改 Sprite", Path.GetFileName(p), (float)i / _previewPaths.Count);
                    var importer = AssetImporter.GetAtPath(p) as TextureImporter;
                    if (!importer) continue;

                    // 导入参数
                    if (_overrideSize) importer.maxTextureSize = _maxSize;
                    if (_overrideFormat)
                    {
                        var plat = importer.GetPlatformTextureSettings("Default");
                        plat.format = SpriteImportUtility.FormatOptions[_formatIdx];
                        importer.SetPlatformTextureSettings(plat);
                    }
                    if (_overrideAlpha) importer.alphaSource = SpriteImportUtility.ToAlphaSource(_alphaSrcIdx);
                    if (_overrideResize) importer.textureCompression = _resizeAlgoIdx == 1 ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Compressed;
                    if (_overrideMipmaps) importer.mipmapEnabled = _mipmaps;

                    // 锚点
                    if (_applyPivot)
                    {
                        var sizes = SpriteImportUtility.GetSpriteSizes(p).ToList();
                        var targetSizes = _onlySelectedSize ? sizes.Where(s => SpriteImportUtility.IsSameSize(new Rect(0, 0, s.x, s.y), _selectedSize)).ToList() : sizes;
                        if (targetSizes.Count > 0)
                        {
                            SpriteImportUtility.ClearSpriteSheetSetting(importer);
                            var piv = SpriteImportUtility.GetPivotValue(_pivotAlign, _customPivot);
                            var sheet = new SpriteMetaData[targetSizes.Count];
                            var allSprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(p).OfType<Sprite>().ToList();
                            for (int j = 0; j < targetSizes.Count; j++)
                            {
                                var sprite = allSprites.FirstOrDefault(s => SpriteImportUtility.IsSameSize(s.rect, targetSizes[j]));
                                sheet[j] = new SpriteMetaData
                                {
                                    name = sprite ? sprite.name : Path.GetFileNameWithoutExtension(p),
                                    rect = sprite ? sprite.rect : new Rect(0, 0, targetSizes[j].x, targetSizes[j].y),
                                    alignment = (int)_pivotAlign,
                                    pivot = piv,
                                };
                            }
#pragma warning disable CS0618 // spritesheet is obsolete; ISpriteEditorDataProvider migration deferred
                            importer.spritesheet = sheet;
#pragma warning restore CS0618
                            pivotCount += targetSizes.Count;
                        }
                    }

                    importer.SaveAndReimport();
                    touched++;
                }
            }
            catch (Exception ex) { EditorUtility.DisplayDialog("失败", ex.Message, "确定"); }
            finally { EditorUtility.ClearProgressBar(); AssetDatabase.Refresh(); }

            EditorUtility.DisplayDialog("完成", $"处理 {touched} 个文件。锚点已设置 {pivotCount} 个 Sprite。", "确定");
        }
    }
}
#endif
