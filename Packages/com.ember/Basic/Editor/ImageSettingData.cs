// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 纹理导入配置单元 —— 一个文件夹 + 一组导入参数。
    /// </summary>
    [Serializable]
    public class ImageSettingUnit
    {
        public string unitName = "New Unit";
        public string folderPath = "";
        public bool isEnabled = true;

        // 导入参数开关 + 值
        public bool enableTextureType; public TextureImporterType textureType = TextureImporterType.Sprite;
        public bool enableTextureShape; public TextureImporterShape textureShape;
        public bool enableSpriteImportMode; public SpriteImportMode spriteImportMode;
        public bool enableSpritePixelsPerUnit; public float spritePixelsPerUnit = 100f;
        public bool enableGeneratePhysicsShape; public bool generatePhysicsShape;
        public bool enableIsReadable; public bool isReadable;
        public bool enableAlphaSource; public TextureImporterAlphaSource alphaSource;
        public bool enableAlphaIsTransparency; public bool alphaIsTransparency = true;
        public bool enableMipmapEnabled; public bool mipmapEnabled;
        public bool enableWrapMode; public TextureWrapMode wrapMode;
        public bool enableFilterMode; public FilterMode filterMode;
        public bool enableAnisoLevel; public int anisoLevel = 1;
        public bool enableMaxTextureSize; public int maxTextureSize = 2048;
        public bool enableCompression; public TextureImporterCompression textureCompression;

        // 平台覆写
        public bool enableStandaloneSettings; public int standaloneMaxSize = 4096; public TextureImporterFormat standaloneFormat; public int standaloneCompressionQuality = 100;
        public bool enableAndroidSettings; public int androidMaxSize = 2048; public TextureImporterFormat androidFormat; public int androidCompressionQuality = 100;
        public bool enableiOSSettings; public int iOsMaxSize = 2048; public TextureImporterFormat iOsFormat; public int iOsCompressionQuality = 100;
    }

    /// <summary>
    /// 纹理导入配置数据 —— ScriptableObject，存储多个 ImageSettingUnit。
    /// </summary>
    public class ImageSettingData : ScriptableObject
    {
        public List<ImageSettingUnit> units = new();

        public string ToJson() => JsonUtility.ToJson(this, true);
        public void FromJson(string json) => JsonUtility.FromJsonOverwrite(json, this);
    }
}
#endif
