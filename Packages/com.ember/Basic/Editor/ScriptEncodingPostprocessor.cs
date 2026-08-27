// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

#if UNITY_EDITOR

using UnityEditor;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 脚本导入时自动检测并转换编码为 UTF-8 BOM。
    ///
    /// Unity 脚本文件要求 UTF-8 BOM 编码。从外部导入或旧项目迁移的 .cs 文件
    /// 可能是 ANSI/GBK 编码，导致中文注释乱码。此 Processor 在每次脚本
    /// 重新导入时自动检测并修复。
    /// </summary>
    public class ScriptEncodingPostprocessor : AssetPostprocessor
    {
        /// <summary>
        /// 在任意资源被导入、删除、移动后，Unity 按文件类型回调此方法。
        /// 只处理 .cs 文件。
        /// </summary>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                if (!path.EndsWith(".cs")) continue;

                try
                {
                    string fullPath = System.IO.Path.GetFullPath(path);
                    if (!FileEncodingUtility.HasBOM(fullPath))
                    {
                        FileEncodingUtility.ConvertToUTF8BOM(fullPath);
                        // 转换后重新导入，让 Unity 用正确的编码解析文件
                        AssetDatabase.ImportAsset(path);
                    }
                }
                catch
                {
                    // 文件被锁定或不可写（如 PackageCache）→ 静默跳过
                }
            }
        }
    }
}

#endif
