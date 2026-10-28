using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>将共享字体的动态数据归属到项目资源；按源 GUID 复用，不按 UI 复制字库。</summary>
    public static class EmberWritableFonts
    {
        #region 内部参数
        public const string DefaultOutputFolder = "Assets/GameResource/Resources/UI/Common/Fonts/EmberGenerated";
        private const string SHARED_ROOT = "Packages/com.ember/SharedAssets/Fonts/";
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static TMP_FontAsset CopyFont(TMP_FontAsset source, string folder,
            Dictionary<TMP_FontAsset, TMP_FontAsset> copies)
        {
            if (!source || !AssetDatabase.GetAssetPath(source).StartsWith(SHARED_ROOT, StringComparison.Ordinal)) return source;
            if (copies.TryGetValue(source, out var known)) return known;
            string sourcePath = AssetDatabase.GetAssetPath(source);
            string sourceFolder = folder + "/" + AssetDatabase.AssetPathToGUID(sourcePath);
            if (!AssetDatabase.IsValidFolder(sourceFolder))
                AssetDatabase.CreateFolder(folder, AssetDatabase.AssetPathToGUID(sourcePath));
            string path = sourceFolder + "/" + Path.GetFileName(sourcePath);
            var copy = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            bool existing = copy;
            if (!copy)
            {
                if (File.Exists(path)) throw new InvalidOperationException("生成路径已被其他资源占用：" + path);
                if (!AssetDatabase.CopyAsset(sourcePath, path)) throw new IOException("无法生成项目字体：" + path);
                copy = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }
            copies.Add(source, copy); // 先登记，以支持循环 fallback。
            if (copy.material == source.material || copy.atlasTexture == source.atlasTexture)
                throw new InvalidOperationException("字体生成未隔离材质或 Atlas：" + path);
            if (existing) return copy;
            // CopyAsset 对字体数组与材质的子资源引用不保证同步重映射；显式绑定已复制的 Atlas。
            copy.material.mainTexture = copy.atlasTexture;
            EditorUtility.SetDirty(copy.material);
            AssetDatabase.SaveAssetIfDirty(copy.material);
            var fallbacks = new List<TMP_FontAsset>();
            foreach (var fallback in source.fallbackFontAssetTable) fallbacks.Add(CopyFont(fallback, folder, copies));
            copy.fallbackFontAssetTable = fallbacks;
            var weights = copy.fontWeightTable;
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i].regularTypeface = CopyFont(weights[i].regularTypeface, folder, copies);
                weights[i].italicTypeface = CopyFont(weights[i].italicTypeface, folder, copies);
            }
            var serialized = new SerializedObject(copy);
            serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(copy);
            AssetDatabase.SaveAssetIfDirty(copy);
            return copy;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static string LastBackupPath { get; private set; }

        /// <summary>固定字体和普通 TMP 的定向迁移；不更换组件或 Binding。修改前备份 Prefab 与 meta。</summary>
        public static int MakePrefabsWritable(string folder, string outputFolder = DefaultOutputFolder)
        {
            folder = folder.Replace('\\', '/').TrimEnd('/');
            if (!folder.StartsWith("Assets/", StringComparison.Ordinal) || !AssetDatabase.IsValidFolder(folder) ||
                !Path.GetFullPath(folder).StartsWith(Path.GetFullPath("Assets") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("仅允许迁移 Assets 下的指定 UI 目录。");
            ValidateOutput(outputFolder);
            Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
            LastBackupPath = Path.GetFullPath("Library/EmberFontSkinBackups/" + Guid.NewGuid().ToString("N"));
            var copies = new Dictionary<TMP_FontAsset, TMP_FontAsset>();
            int changed = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    bool dirty = false;
                    foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                    {
                        var original = text.font;
                        var font = CopyFont(original, outputFolder, copies);
                        if (font == original) continue;
                        var material = text.fontSharedMaterial;
                        if (material && material != original.material)
                        {
                            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(material, out string materialGuid, out long localId);
                            string materialPath = outputFolder + "/" + guid + "-" + materialGuid + "-" + localId + ".mat";
                            var owned = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                            if (!owned)
                            {
                                owned = new Material(material);
                                owned.mainTexture = font.atlasTexture;
                                AssetDatabase.CreateAsset(owned, materialPath);
                            }
                            material = owned;
                        }
                        else material = font.material;
                        text.font = font;
                        text.fontSharedMaterial = material;
                        dirty = true;
                    }
                    if (!dirty) continue;
                    string backup = Path.Combine(LastBackupPath, path);
                    Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    File.Copy(path, backup);
                    File.Copy(path + ".meta", backup + ".meta");
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed++;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            return changed;
        }

        private static void ValidateOutput(string outputFolder)
        {
            string full = Path.GetFullPath(outputFolder);
            if (!outputFolder.StartsWith("Assets/", StringComparison.Ordinal) ||
                !full.StartsWith(Path.GetFullPath("Assets") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("生成字体必须位于项目 Assets 中。");
        }

        /// <summary>保持皮肤/槽位 ID 与字体参数，仅替换项目库中的共享源引用。项目已有生成字体不会被覆盖。</summary>
        public static int EnsureProjectFonts(EmberFontSkinCatalog catalog, string outputFolder = DefaultOutputFolder)
        {
            string catalogPath = AssetDatabase.GetAssetPath(catalog);
            if (!catalog || !catalogPath.StartsWith("Assets/", StringComparison.Ordinal))
                throw new ArgumentException("只允许更新项目自有字体皮肤库。");
            outputFolder = outputFolder.Replace('\\', '/').TrimEnd('/');
            ValidateOutput(outputFolder);
            Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
            var copies = new Dictionary<TMP_FontAsset, TMP_FontAsset>();
            foreach (var skin in catalog.Skins)
                foreach (var entry in skin.Fonts)
                    entry.Font = CopyFont(entry.Font, outputFolder, copies);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            return copies.Count;
        }
        #endregion
    }
}
