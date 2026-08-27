// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// Sprite 导入工具共享方法 —— 供 SpriteBatchImportAndPivotTool / SpriteFrameFolderReplacerTool 共用。
    /// </summary>
    public static class SpriteImportUtility
    {
        // ---- 常量 ----

        public static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".bmp", ".tif", ".tiff", ".webp" };

        public static readonly int[] MaxSizeOptions = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };

        public static readonly TextureResizeAlgorithm[] ResizeAlgorithms = { TextureResizeAlgorithm.Mitchell, TextureResizeAlgorithm.Bilinear };

        public static readonly string[] ResizeAlgorithmLabels = { "Mitchell", "Bilinear" };

        public static readonly TextureImporterFormat[] FormatOptions =
            ((TextureImporterFormat[])Enum.GetValues(typeof(TextureImporterFormat))).ToArray();

        public static readonly string[] FormatLabels = FormatOptions.Select(GetTextureFormatLabel).ToArray();

        public static readonly SpriteAlignment[] PivotAlignments =
        {
            SpriteAlignment.Center, SpriteAlignment.TopLeft, SpriteAlignment.TopCenter, SpriteAlignment.TopRight,
            SpriteAlignment.LeftCenter, SpriteAlignment.RightCenter, SpriteAlignment.BottomLeft, SpriteAlignment.BottomCenter, SpriteAlignment.BottomRight, SpriteAlignment.Custom,
        };

        public static readonly string[] PivotLabels = PivotAlignments.Select(a => a == SpriteAlignment.Custom ? "Custom" : a.ToString()).ToArray();

        // ---- 纹理信息 ----

        public static IEnumerable<Vector2Int> GetSpriteSizes(string assetPath)
        {
            var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath).OfType<Sprite>();
            return sprites.Select(s => new Vector2Int((int)s.rect.width, (int)s.rect.height));
        }

        public static Vector2Int GetTextureSize(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (!importer) return Vector2Int.zero;

            // 优先读 platform maxSize，失败则用 importer.maxTextureSize
            int maxSize;
            try
            {
                var so = new SerializedObject(importer);
                var platformProp = so.FindProperty("m_PlatformSettings");
                if (platformProp?.arraySize > 0)
                {
                    var first = platformProp.GetArrayElementAtIndex(0);
                    maxSize = first.FindPropertyRelative("m_MaxTextureSize")?.intValue ?? importer.maxTextureSize;
                }
                else maxSize = importer.maxTextureSize;
            }
            catch { maxSize = importer.maxTextureSize; }
            return new Vector2Int(maxSize, maxSize);
        }

        public static bool IsSameSize(Rect rect, Vector2Int size) =>
            Mathf.Approximately(rect.width, size.x) && Mathf.Approximately(rect.height, size.y);

        // ---- 导入参数读写 ----

        public static void ClearSpriteSheetSetting(TextureImporter importer)
        {
            using var so = new SerializedObject(importer);
            so.FindProperty("m_SpriteSheet")?.ClearArray();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetImportBool(TextureImporter importer, string prop, bool val)
        {
            using var so = new SerializedObject(importer);
            var p = so.FindProperty(prop);
            if (p != null) { p.boolValue = val; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        public static bool GetImportBool(TextureImporter importer, string prop)
        {
            using var so = new SerializedObject(importer);
            return so.FindProperty(prop)?.boolValue ?? false;
        }

        // ---- Pivot ----

        public static Vector2 GetPivotValue(SpriteAlignment alignment, Vector2 custom)
        {
            return alignment switch
            {
                SpriteAlignment.Center => new(0.5f, 0.5f),
                SpriteAlignment.TopLeft => new(0f, 1f),
                SpriteAlignment.TopCenter => new(0.5f, 1f),
                SpriteAlignment.TopRight => new(1f, 1f),
                SpriteAlignment.LeftCenter => new(0f, 0.5f),
                SpriteAlignment.RightCenter => new(1f, 0.5f),
                SpriteAlignment.BottomLeft => new(0f, 0f),
                SpriteAlignment.BottomCenter => new(0.5f, 0f),
                SpriteAlignment.BottomRight => new(1f, 0f),
                _ => custom,
            };
        }

        // ---- 路径 ----

        public static string ToAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            path = path.Replace("\\", "/");
            return path.StartsWith("Assets/") ? path : path.Contains("Assets/") ? path.Substring(path.IndexOf("Assets/")) : path;
        }

        public static bool IsSameOrChildPath(string child, string parent)
        {
            child = child.Replace('\\', '/').TrimEnd('/');
            parent = parent.Replace('\\', '/').TrimEnd('/');
            return child.Equals(parent, StringComparison.OrdinalIgnoreCase)
                || child.StartsWith(parent + "/", StringComparison.OrdinalIgnoreCase);
        }

        // ---- 格式标签 ----

        public static string GetTextureFormatLabel(TextureImporterFormat fmt)
        {
            // 简化常用格式名称
            return fmt switch
            {
                TextureImporterFormat.Automatic => "Auto",
                TextureImporterFormat.RGBA32 => "RGBA32",
                TextureImporterFormat.RGBA16 => "RGBA16",
                TextureImporterFormat.RGB24 => "RGB24",
                TextureImporterFormat.RGB16 => "RGB16",
                TextureImporterFormat.ASTC_4x4 => "ASTC 4x4",
                TextureImporterFormat.ASTC_6x6 => "ASTC 6x6",
                TextureImporterFormat.ASTC_8x8 => "ASTC 8x8",
                TextureImporterFormat.ETC2_RGBA8 => "ETC2 RGBA8",
                TextureImporterFormat.ETC_RGB4 => "ETC RGB4",
                TextureImporterFormat.DXT1 => "DXT1",
                TextureImporterFormat.DXT5 => "DXT5",
                TextureImporterFormat.BC7 => "BC7",
                _ => fmt.ToString(),
            };
        }

        public static TextureImporterAlphaSource ToAlphaSource(int option) => option switch
        {
            1 => TextureImporterAlphaSource.FromInput,
            2 => TextureImporterAlphaSource.FromGrayScale,
            _ => TextureImporterAlphaSource.None,
        };

        public static int FromAlphaSource(TextureImporterAlphaSource src) => src switch
        {
            TextureImporterAlphaSource.FromInput => 1,
            TextureImporterAlphaSource.FromGrayScale => 2,
            _ => 0,
        };
    }
}
#endif
