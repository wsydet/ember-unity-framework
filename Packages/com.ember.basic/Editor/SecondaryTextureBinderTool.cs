// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 次要纹理批量绑定工具 —— 给主图 Sprite 批量绑定发光图/法线图/遮罩等。
    ///
    /// 例如：Fire_01.png 在主文件夹，Fire_01_Emission.png 在次要文件夹。
    /// 设置后缀 _Emission 和 Shader 属性名 _EmissionTex，一键全部绑定。
    ///
    /// Unity 自带的 SecondaryTexture 只能逐张手动绑定，这个工具批量搞定。
    /// </summary>
    public class SecondaryTextureBinderTool : EmberEditorWindow
    {
        protected override string MenuPath => "Tools/Ember/次要纹理批量绑定";
        protected override string WindowTitle => "Secondary Texture Binder";
        protected override Vector2 WindowSize => new(550, 600);
        protected override string WindowVersion => "v2.0";

        private const string GroupFolder = "1. 文件夹配置";
        private const string GroupRules = "2. 绑定规则";
        private const string GroupExec = "3. 执行";

        [FoldoutGroup(GroupFolder, Expanded = true)]
        [LabelText("主图文件夹"), FolderPath(RequireExistingPath = true)]
        [InfoBox("存放普通 Sprite 的文件夹（例如 Fire_01, Fire_02...）")]
        public string MainFolder = "Assets";

        [FoldoutGroup(GroupFolder)]
        [LabelText("次要纹理文件夹"), FolderPath(RequireExistingPath = true)]
        [InfoBox("存放发光图/法线图等次要纹理的文件夹")]
        public string SecondaryFolder = "Assets";

        [FoldoutGroup(GroupRules, Expanded = true)]
        [LabelText("次要纹理名称后缀"), Tooltip("主图 Attack_01 → 找 Attack_01_Emission")]
        public string NameSuffix = "_Emission";

        [FoldoutGroup(GroupRules)]
        [LabelText("Shader 属性名"), Tooltip("Shader 中的采样变量名，如 _EmissionTex")]
        public string ShaderProperty = "_EmissionTex";

        [FoldoutGroup(GroupRules)]
        [LabelText("仅预览不写入 (Dry Run)")]
        public bool DryRun;

        // ======== 菜单 ========

        [MenuItem("Tools/Ember/次要纹理批量绑定")]
        public static void ShowWindow()
        {
            var win = GetWindow<SecondaryTextureBinderTool>();
            win.minSize = win.WindowSize;
            win.Show();
        }

        // ======== UI ========

        protected override void DrawContent()
        {
            // Odin 自动绘制 FoldoutGroup 字段
            DrawSeparatorLine();

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(L10n("Actions", "操作"), EditorStyles.boldLabel);

            DryRun = EditorGUILayout.Toggle(L10n("Dry Run (preview only)", "仅预览不写入"), DryRun);

            GUI.backgroundColor = DryRun ? new Color(1f, 0.8f, 0.3f) : new Color(0.3f, 0.8f, 0.5f);
            string label = DryRun
                ? L10n("Preview Bindings (Dry Run)", "预览绑定结果 (Dry Run)")
                : L10n("Execute Binding", "一键智能绑定");
            if (GUILayout.Button(new GUIContent(label, EditorGUIUtility.IconContent(DryRun ? "ViewToolOrbit" : "SaveActive").image), GUILayout.Height(50)))
                ExecuteBinding();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
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
                    { Debug.LogWarning("[Ember] User cancelled."); break; }

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

                    if (!secTex) { Debug.Log($"[Ember] No match: {mainName} → looking for {expectedName}"); continue; }

                    if (DryRun)
                    {
                        Debug.Log($"<color=yellow>[DryRun] Would bind: {mainName} → {expectedName}</color>");
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
                    Debug.Log($"[Ember] Bound: {mainName} → {expectedName}");
                    success++;
                }
            }
            catch (Exception ex) { Debug.LogError($"[Ember] Error: {ex.Message}"); }
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
