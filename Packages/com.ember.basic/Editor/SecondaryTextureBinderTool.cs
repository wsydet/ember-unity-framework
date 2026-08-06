// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Ember.Basic;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 次要纹理批量绑定工具 —— 给主图 Sprite 批量绑定发光图/法线图/遮罩等。
    /// 例如：Fire_01.png 在主文件夹，Fire_01_Emission.png 在次要文件夹。
    /// </summary>
    public class SecondaryTextureBinderTool : EmberEditorWindow
    {
        private const string TAG = LogTags.EmberBasic + "." + nameof(SecondaryTextureBinderTool);

        protected override string MenuPath => "Ember/Tool/次要纹理批量绑定";
        protected override string WindowTitle => "Secondary Texture Binder";
        protected override Vector2 WindowSize => new(550, 650);
        protected override string WindowVersion => "v2.0";

        // ---- 文件夹 ----
        public string MainFolder = "Assets";
        public string SecondaryFolder = "Assets";

        // ---- 绑定规则 ----
        public string NameSuffix = "_Emission";
        public string ShaderProperty = "_EmissionTex";
        public bool DryRun;

        private bool _showFolderConfig = true;
        private bool _showRules = true;

        // ======== 菜单 ========

        [MenuItem("Ember/Tool/次要纹理批量绑定", false, 250)]
        public static void ShowWindow()
        {
            var win = GetWindow<SecondaryTextureBinderTool>();
            win.minSize = win.WindowSize;
            win.Show();
        }

        // ======== UI ========

        protected override void DrawContent()
        {
            // 1. 文件夹配置
            _showFolderConfig = EditorGUILayout.BeginFoldoutHeaderGroup(_showFolderConfig,
                L10n("1. Folder Configuration", "1. 文件夹配置"));
            if (_showFolderConfig)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(L10n("Main Sprite Folder", "主图文件夹"), EditorStyles.miniBoldLabel);
                MainFolder = DrawFolderPathField(MainFolder);
                EditorGUILayout.HelpBox(L10n("Folder containing main sprites (e.g. Fire_01, Fire_02...)", "存放普通 Sprite 的文件夹（例如 Fire_01, Fire_02...）"), MessageType.Info);

                EditorGUILayout.Space(4);

                EditorGUILayout.LabelField(L10n("Secondary Texture Folder", "次要纹理文件夹"), EditorStyles.miniBoldLabel);
                SecondaryFolder = DrawFolderPathField(SecondaryFolder);
                EditorGUILayout.HelpBox(L10n("Folder containing secondary textures (emission/normal maps etc.)", "存放发光图/法线图等次要纹理的文件夹"), MessageType.Info);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(5);

            // 2. 绑定规则
            _showRules = EditorGUILayout.BeginFoldoutHeaderGroup(_showRules,
                L10n("2. Binding Rules", "2. 绑定规则"));
            if (_showRules)
            {
                EditorGUILayout.BeginVertical("box");
                NameSuffix = EditorGUILayout.TextField(
                    new GUIContent(L10n("Secondary Texture Name Suffix", "次要纹理名称后缀"),
                        L10n("Main image Attack_01 → searches for Attack_01_Emission", "主图 Attack_01 → 找 Attack_01_Emission")),
                    NameSuffix);
                ShaderProperty = EditorGUILayout.TextField(
                    new GUIContent(L10n("Shader Property Name", "Shader 属性名"),
                        L10n("Shader sampling variable name, e.g. _EmissionTex", "Shader 中的采样变量名，如 _EmissionTex")),
                    ShaderProperty);
                DryRun = EditorGUILayout.Toggle(L10n("Dry Run (preview only, no write)", "仅预览不写入 (Dry Run)"), DryRun);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            DrawSeparatorLine();

            // 3. 执行
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(L10n("3. Execute", "3. 执行"), EditorStyles.boldLabel);

            GUI.backgroundColor = DryRun ? new Color(1f, 0.8f, 0.3f) : new Color(0.3f, 0.8f, 0.5f);
            string label = DryRun
                ? L10n("Preview Bindings (Dry Run)", "预览绑定结果 (Dry Run)")
                : L10n("Execute Binding", "一键智能绑定");
            if (GUILayout.Button(new GUIContent(label, EditorGUIUtility.IconContent(DryRun ? "ViewToolOrbit" : "SaveActive").image), GUILayout.Height(50)))
                ExecuteBinding();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }

        private string DrawFolderPathField(string currentPath)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField(currentPath);
            if (GUILayout.Button(L10n("Browse", "浏览"), GUILayout.Width(60)))
            {
                string picked = EditorUtility.OpenFolderPanel(L10n("Select Folder", "选择文件夹"), "Assets", "");
                if (!string.IsNullOrEmpty(picked))
                {
                    picked = picked.Replace("\\", "/");
                    if (picked.Contains("Assets"))
                        currentPath = picked.Substring(picked.IndexOf("Assets"));
                }
            }
            EditorGUILayout.EndHorizontal();
            return currentPath;
        }

        // ======== 核心逻辑 ========

        private void ExecuteBinding()
        {
            string mainRel = ToRelativePath(MainFolder);
            string secRel = ToRelativePath(SecondaryFolder);

            if (string.IsNullOrEmpty(mainRel) || string.IsNullOrEmpty(secRel))
            {
                EditorUtility.DisplayDialog("Ember", L10n("Please select valid folders.", "请选择有效的文件夹。"), "OK");
                return;
            }

            string[] mainGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { mainRel });
            if (mainGuids.Length == 0)
            {
                EditorUtility.DisplayDialog("Ember", L10n("No textures found in main folder.", "主文件夹中未找到图片。"), "OK");
                return;
            }

            int success = 0, skipped = 0;
            try
            {
                for (int i = 0; i < mainGuids.Length; i++)
                {
                    string mainPath = AssetDatabase.GUIDToAssetPath(mainGuids[i]);
                    string mainName = Path.GetFileNameWithoutExtension(mainPath);

                    if (EditorUtility.DisplayCancelableProgressBar(
                        L10n("Binding...", "正在绑定..."),
                        $"{mainName} ({i + 1}/{mainGuids.Length})",
                        (float)i / mainGuids.Length))
                    { EmberDebug.LogWarning(TAG, "[Ember] User cancelled."); break; }

                    var importer = AssetImporter.GetAtPath(mainPath) as TextureImporter;
                    if (importer == null || importer.textureType != TextureImporterType.Sprite) { skipped++; continue; }

                    string expectedName = mainName + NameSuffix;
                    string[] secGuids = AssetDatabase.FindAssets($"{expectedName} t:Texture2D", new[] { secRel });
                    Texture2D secTex = null;
                    foreach (var sg in secGuids)
                    {
                        string sp = AssetDatabase.GUIDToAssetPath(sg);
                        if (Path.GetFileNameWithoutExtension(sp) == expectedName) { secTex = AssetDatabase.LoadAssetAtPath<Texture2D>(sp); break; }
                    }

                    if (!secTex) { EmberDebug.Log(TAG, $"[Ember] No match: {mainName} → looking for {expectedName}"); continue; }

                    if (DryRun)
                    {
                        EmberDebug.Log(TAG, $"<color=yellow>[DryRun] Would bind: {mainName} → {expectedName}</color>");
                        success++;
                        continue;
                    }

                    var list = importer.secondarySpriteTextures?.ToList() ?? new List<SecondarySpriteTexture>();
                    int idx = list.FindIndex(x => x.name == ShaderProperty);
                    if (idx >= 0)
                        list[idx] = new SecondarySpriteTexture { name = ShaderProperty, texture = secTex };
                    else
                        list.Add(new SecondarySpriteTexture { name = ShaderProperty, texture = secTex });

                    importer.secondarySpriteTextures = list.ToArray();
                    importer.SaveAndReimport();
                    EmberDebug.Log(TAG, $"[Ember] Bound: {mainName} → {expectedName}");
                    success++;
                }
            }
            catch (Exception ex) { EmberDebug.LogError(TAG, $"[Ember] Error: {ex.Message}"); }
            finally { EditorUtility.ClearProgressBar(); AssetDatabase.Refresh(); }

            string msg = DryRun
                ? string.Format(L10n("Dry Run: {0} would be bound, {1} skipped.", "预览结果: {0} 可绑定, {1} 跳过。"), success, skipped)
                : string.Format(L10n("Bound {0} textures, {1} skipped.", "成功绑定 {0} 张, 跳过 {1} 张。"), success, skipped);
            EditorUtility.DisplayDialog("Ember", msg, "OK");
        }

        private static string ToRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            path = path.Replace("\\", "/");
            if (path.StartsWith("Assets")) return path;
            int i = path.IndexOf("Assets");
            return i >= 0 ? path.Substring(i) : path;
        }
    }
}
#endif
