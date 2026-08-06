// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

using Ember.Basic;
using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// Unity 启动时自动检测并创建 EmberPerformanceConfig.asset（如果不存在）。
    ///
    /// 工作原理：
    /// <list type="bullet">
    ///   <item>Unity 启动时，静态构造函数注册 delayCall</item>
    ///   <item>delayCall 检查目标路径是否已有 SO 资产</item>
    ///   <item>不存在则自动创建，填入默认阈值</item>
    /// </list>
    ///
    /// 用户也可手动创建：右键 → Create → Ember → Performance Config。
    /// </summary>
    [InitializeOnLoad]
    public class EmberPerformanceConfigCreator
    {
        private const string TAG = LogTags.EmberBasic + "." + nameof(EmberPerformanceConfigCreator);
        private const string AssetPath = "Assets/Ember/Core/Runtime/Resources/EmberPerformanceConfig.asset";

        static EmberPerformanceConfigCreator()
        {
            EditorApplication.delayCall += EnsureConfigExists;
        }

        private static void EnsureConfigExists()
        {
            // 已存在则跳过
            if (AssetDatabase.LoadAssetAtPath<EmberPerformanceConfigSO>(AssetPath) != null)
                return;

            // 确保 Resources 目录存在（与 EmberDebugConfigCreator 逻辑一致）
            var dir = System.IO.Path.GetDirectoryName(AssetPath);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                var parent = System.IO.Path.GetDirectoryName(dir);
                var folderName = System.IO.Path.GetFileName(dir);
                AssetDatabase.CreateFolder(parent, folderName);
            }

            // 创建 SO 并保存
            var config = ScriptableObject.CreateInstance<EmberPerformanceConfigSO>();
            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();

            EmberDebug.Log(TAG,
                $"EmberPerformanceConfig.asset 已自动创建于: {AssetPath}\n"
                + "可在 Inspector 中编辑 GPU 型号阈值，无需改代码。");
        }
    }
}
