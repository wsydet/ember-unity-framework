// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 纹理批量设置工具 —— 创建多个配置单元，每个指定一个文件夹 + 一组导入参数，一键应用到文件夹内所有纹理。
    /// 数据类 <see cref="ImageSettingData"/> / <see cref="ImageSettingUnit"/> 位于 ImageSettingData.cs（Unity 要求与类同名文件）。
    /// </summary>
    public class ImageBatchSettingsEditor : EmberEditorWindow
    {
        protected override string MenuPath => "Ember/Tool/批量修改图片设置";
        protected override string WindowTitle => "Image Batch Settings";
        protected override Vector2 WindowSize => new(1000, 800);

        private ImageSettingData _data;
        private Vector2 _scrollPos;
        private const string DataPath = "Assets/Editor/EmberImageBatchSettings.asset";

        // ======== 菜单 ========

        [MenuItem("Ember/Tool/批量修改图片设置", false, 240)]
        public static void ShowWindow()
        {
            var win = GetWindow<ImageBatchSettingsEditor>();
            win.minSize = win.WindowSize;
            win.Show();
        }

        [MenuItem("Assets/Ember/添加到图片批量设置", false, 2400)]
        public static void QuickAddFolder()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!AssetDatabase.IsValidFolder(path)) return;

            var data = LoadOrCreateData();
            data.units.Add(new ImageSettingUnit { unitName = Path.GetFileName(path), folderPath = path });
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            var win = GetWindow<ImageBatchSettingsEditor>();
            win._data = data;
            win.Show();
        }

        [MenuItem("Assets/Ember/添加到图片批量设置", true)]
        public static bool QuickAddValidate() => Selection.activeObject && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));

        // ======== Lifecycle ========

        protected override void OnEnable() { base.OnEnable(); _data = LoadOrCreateData(); }

        // ======== UI ========

        protected override void DrawContent()
        {
            if (!_data) { EditorGUILayout.HelpBox("Failed to load config.", MessageType.Error); return; }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            for (int i = 0; i < _data.units.Count; i++)
            {
                DrawUnit(_data.units[i], i);
                EditorGUILayout.Space(8);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            DrawBottomBar();
        }

        // ======== 单个配置单元 ========

        private void DrawUnit(ImageSettingUnit unit, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 标题行
            EditorGUILayout.BeginHorizontal();
            unit.isEnabled = EditorGUILayout.Foldout(unit.isEnabled, unit.unitName, true, EditorStyles.boldLabel);

            // 路径
            var pathStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = unit.folderPath.StartsWith("Assets") ? new Color(0.4f, 0.7f, 1f, 0.8f) : Color.red }
            };
            if (GUILayout.Button(new GUIContent(Truncate(unit.folderPath, 60), unit.folderPath), pathStyle, GUILayout.ExpandWidth(true)))
                LocateFolder(unit.folderPath);

            // 排序/删除
            if (_data.units.Count > 1)
            {
                if (index > 0 && GUILayout.Button("▲", EditorStyles.miniButton, GUILayout.Width(22)))
                    MoveUnit(index, -1);
                if (index < _data.units.Count - 1 && GUILayout.Button("▼", EditorStyles.miniButton, GUILayout.Width(22)))
                    MoveUnit(index, 1);
            }
            GUI.backgroundColor = new Color(1f, 0.35f, 0.35f);
            if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(22)))
            {
                if (EditorUtility.DisplayDialog(L10n("Confirm", "确认"), L10n("Delete?", "确定删除？"), "OK", "Cancel"))
                { _data.units.RemoveAt(index); EditorUtility.SetDirty(_data); }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // 应用按钮
            GUI.backgroundColor = Color.HSVToRGB((0.6f + index * 0.1f) % 1f, 0.6f, 0.8f);
            if (GUILayout.Button(string.Format(L10n("APPLY: {0}", "应用: {0}"), unit.unitName), GUILayout.Height(28)))
                Apply(unit);
            GUI.backgroundColor = Color.white;

            if (!unit.isEnabled) { EditorGUILayout.EndVertical(); return; }

            EditorGUI.indentLevel++;

            unit.unitName = EditorGUILayout.TextField(L10n("Name", "名称"), unit.unitName);

            EditorGUILayout.BeginHorizontal();
            unit.folderPath = EditorGUILayout.TextField(L10n("Path", "路径"), unit.folderPath);
            if (GUILayout.Button(L10n("Select", "选择"), GUILayout.Width(60)))
            {
                string p = EditorUtility.OpenFolderPanel(L10n("Select Folder", "选择文件夹"), "Assets", "");
                if (!string.IsNullOrEmpty(p)) unit.folderPath = SpriteImportUtility.ToAssetPath(p);
            }
            EditorGUILayout.EndHorizontal();

            if (!unit.folderPath.StartsWith("Assets") && !string.IsNullOrEmpty(unit.folderPath))
                EditorGUILayout.HelpBox("路径必须在 Assets 内。", MessageType.Error);

            EditorGUILayout.Space(3);

            ToggleProp("Texture Type", ref unit.enableTextureType, () => unit.textureType = (TextureImporterType)EditorGUILayout.EnumPopup(unit.textureType));
            ToggleProp("Texture Shape", ref unit.enableTextureShape, () => unit.textureShape = (TextureImporterShape)EditorGUILayout.EnumPopup(unit.textureShape));

            if (unit.textureType == TextureImporterType.Sprite)
            {
                ToggleProp("Sprite Mode", ref unit.enableSpriteImportMode, () => unit.spriteImportMode = (SpriteImportMode)EditorGUILayout.EnumPopup(unit.spriteImportMode));
                ToggleProp("Pixels Per Unit", ref unit.enableSpritePixelsPerUnit, () => unit.spritePixelsPerUnit = EditorGUILayout.FloatField(unit.spritePixelsPerUnit));
                ToggleProp("Generate Physics Shape", ref unit.enableGeneratePhysicsShape, () => unit.generatePhysicsShape = EditorGUILayout.Toggle(unit.generatePhysicsShape));
            }

            ToggleProp("Read/Write", ref unit.enableIsReadable, () => unit.isReadable = EditorGUILayout.Toggle(unit.isReadable));
            ToggleProp("Alpha Source", ref unit.enableAlphaSource, () => unit.alphaSource = (TextureImporterAlphaSource)EditorGUILayout.EnumPopup(unit.alphaSource));
            ToggleProp("Alpha Is Transparency", ref unit.enableAlphaIsTransparency, () => unit.alphaIsTransparency = EditorGUILayout.Toggle(unit.alphaIsTransparency));
            ToggleProp("Mip Maps", ref unit.enableMipmapEnabled, () => unit.mipmapEnabled = EditorGUILayout.Toggle(unit.mipmapEnabled));
            ToggleProp("Wrap Mode", ref unit.enableWrapMode, () => unit.wrapMode = (TextureWrapMode)EditorGUILayout.EnumPopup(unit.wrapMode));
            ToggleProp("Filter Mode", ref unit.enableFilterMode, () => unit.filterMode = (FilterMode)EditorGUILayout.EnumPopup(unit.filterMode));
            ToggleProp("Aniso Level", ref unit.enableAnisoLevel, () => unit.anisoLevel = EditorGUILayout.IntSlider(unit.anisoLevel, 0, 16));

            ToggleProp("Max Size", ref unit.enableMaxTextureSize, () =>
            {
                var names = new[] { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" };
                var vals = new[] { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
                unit.maxTextureSize = EditorGUILayout.IntPopup(unit.maxTextureSize, names, vals);
            });

            ToggleProp("Compression", ref unit.enableCompression, () => unit.textureCompression = (TextureImporterCompression)EditorGUILayout.EnumPopup(unit.textureCompression));

            // 平台覆写
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(L10n("Platform Overrides", "平台覆写"), EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("HelpBox");

            ToggleProp("PC/Mac", ref unit.enableStandaloneSettings, () =>
            {
                unit.standaloneMaxSize = EditorGUILayout.IntPopup("Max Size", unit.standaloneMaxSize, new[] { "32","64","128","256","512","1024","2048","4096","8192" }, new[] { 32,64,128,256,512,1024,2048,4096,8192 });
                unit.standaloneFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("Format", unit.standaloneFormat);
                unit.standaloneCompressionQuality = EditorGUILayout.IntSlider("Quality", unit.standaloneCompressionQuality, 0, 100);
            });

            ToggleProp("Android", ref unit.enableAndroidSettings, () =>
            {
                unit.androidMaxSize = EditorGUILayout.IntPopup("Max Size", unit.androidMaxSize, new[] { "32","64","128","256","512","1024","2048","4096" }, new[] { 32,64,128,256,512,1024,2048,4096 });
                unit.androidFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("Format", unit.androidFormat);
                unit.androidCompressionQuality = EditorGUILayout.IntSlider("Quality", unit.androidCompressionQuality, 0, 100);
            });

            ToggleProp("iOS", ref unit.enableiOSSettings, () =>
            {
                unit.iOsMaxSize = EditorGUILayout.IntPopup("Max Size", unit.iOsMaxSize, new[] { "32","64","128","256","512","1024","2048","4096" }, new[] { 32,64,128,256,512,1024,2048,4096 });
                unit.iOsFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("Format", unit.iOsFormat);
                unit.iOsCompressionQuality = EditorGUILayout.IntSlider("Quality", unit.iOsCompressionQuality, 0, 100);
            });

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private void DrawBottomBar()
        {
            DrawSeparatorLine();

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button(L10n("+ Add Unit", "+ 添加单元"), GUILayout.Height(36)))
            { Undo.RecordObject(_data, "Add"); _data.units.Add(new ImageSettingUnit()); }

            GUI.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
            if (GUILayout.Button(L10n("Export JSON", "导出 JSON"), GUILayout.Height(36)))
            {
                string p = EditorUtility.SaveFilePanel("Export", "", "ImageSettings.json", "json");
                if (!string.IsNullOrEmpty(p)) File.WriteAllText(p, _data.ToJson());
            }

            GUI.backgroundColor = new Color(0.9f, 0.6f, 0.4f);
            if (GUILayout.Button(L10n("Import JSON", "导入 JSON"), GUILayout.Height(36)))
            {
                string p = EditorUtility.OpenFilePanel("Import", "", "json");
                if (!string.IsNullOrEmpty(p)) { Undo.RecordObject(_data, "Import"); _data.FromJson(File.ReadAllText(p)); }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        // ======== 核心逻辑 ========

        private static void ToggleProp(string label, ref bool toggle, Action draw)
        {
            EditorGUILayout.BeginHorizontal();
            toggle = EditorGUILayout.ToggleLeft(label, toggle, GUILayout.Width(140));
            EditorGUI.BeginDisabledGroup(!toggle);
            draw?.Invoke();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        private static void Apply(ImageSettingUnit unit)
        {
            unit.folderPath = SpriteImportUtility.ToAssetPath(unit.folderPath);
            if (!unit.folderPath.StartsWith("Assets")) { EditorUtility.DisplayDialog("Error", "Path must be inside Assets.", "OK"); return; }

            var guids = AssetDatabase.FindAssets("t:Texture", new[] { unit.folderPath });
            if (guids.Length == 0) { EditorUtility.DisplayDialog("提示", "文件夹中无纹理。", "OK"); return; }

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guids[i])) as TextureImporter;
                    if (!importer) continue;

                    if (unit.enableTextureType) importer.textureType = unit.textureType;
                    if (unit.enableTextureShape) importer.textureShape = unit.textureShape;
                    if (unit.enableSpriteImportMode) importer.spriteImportMode = unit.spriteImportMode;
                    if (unit.enableSpritePixelsPerUnit) importer.spritePixelsPerUnit = unit.spritePixelsPerUnit;
                    if (unit.enableIsReadable) importer.isReadable = unit.isReadable;
                    if (unit.enableAlphaSource) importer.alphaSource = unit.alphaSource;
                    if (unit.enableAlphaIsTransparency) importer.alphaIsTransparency = unit.alphaIsTransparency;
                    if (unit.enableMipmapEnabled) importer.mipmapEnabled = unit.mipmapEnabled;
                    if (unit.enableWrapMode) importer.wrapMode = unit.wrapMode;
                    if (unit.enableFilterMode) importer.filterMode = unit.filterMode;
                    if (unit.enableAnisoLevel) importer.anisoLevel = unit.anisoLevel;
                    if (unit.enableMaxTextureSize) importer.maxTextureSize = unit.maxTextureSize;
                    if (unit.enableCompression) importer.textureCompression = unit.textureCompression;

                    if (unit.enableGeneratePhysicsShape && unit.textureType == TextureImporterType.Sprite)
                    {
                        var s = new TextureImporterSettings(); importer.ReadTextureSettings(s);
                        s.spriteGenerateFallbackPhysicsShape = unit.generatePhysicsShape;
                        importer.SetTextureSettings(s);
                    }

                    void ApplyPlatform(string platform, bool enabled, int maxSize, TextureImporterFormat fmt, int quality)
                    {
                        if (!enabled) return;
                        var ps = importer.GetPlatformTextureSettings(platform);
                        ps.overridden = true; ps.maxTextureSize = maxSize; ps.format = fmt; ps.compressionQuality = quality;
                        importer.SetPlatformTextureSettings(ps);
                    }

                    ApplyPlatform("Standalone", unit.enableStandaloneSettings, unit.standaloneMaxSize, unit.standaloneFormat, unit.standaloneCompressionQuality);
                    ApplyPlatform("Android", unit.enableAndroidSettings, unit.androidMaxSize, unit.androidFormat, unit.androidCompressionQuality);
                    ApplyPlatform("iPhone", unit.enableiOSSettings, unit.iOsMaxSize, unit.iOsFormat, unit.iOsCompressionQuality);

                    EditorUtility.SetDirty(importer);
                }
            }
            finally { AssetDatabase.StopAssetEditing(); AssetDatabase.Refresh(); }
            EditorUtility.DisplayDialog("完成", "处理完成。", "OK");
        }

        private void MoveUnit(int index, int dir) { Undo.RecordObject(_data, "Move"); var u = _data.units[index]; _data.units.RemoveAt(index); _data.units.Insert(index + dir, u); }
        private static void LocateFolder(string path) { var o = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path); if (o) { Selection.activeObject = o; EditorGUIUtility.PingObject(o); } }
        private static string Truncate(string s, int max) => string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max / 2 - 2) + "..." + s.Substring(s.Length - max / 2 + 2);

        private static ImageSettingData LoadOrCreateData()
        {
            var data = AssetDatabase.LoadAssetAtPath<ImageSettingData>(DataPath);
            if (data) return data;

            // 历史遗留：asset 存在但脚本引用丢失（m_Script: fileID 0）时 LoadAssetAtPath 返回 null，
            // 直接 CreateAsset 会因路径已存在而报错——先删除坏资产再重建
            if (File.Exists(DataPath))
            {
                EmberDebug.LogWarning("EmberBasic", $"EmberImageBatchSettings.asset 脚本引用丢失，删除并重建: {DataPath}");
                AssetDatabase.DeleteAsset(DataPath);
            }

            data = CreateInstance<ImageSettingData>();
            var dir = Path.GetDirectoryName(DataPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            AssetDatabase.CreateAsset(data, DataPath);
            AssetDatabase.SaveAssets();
            return data;
        }
    }
}
#endif
