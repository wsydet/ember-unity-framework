// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Ember.Basic;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 集中管理的排除文件夹配置。
    ///
    /// 所有文件/目录扫描工具统一引用此工具来决定是否跳过某个路径。
    /// 配置文件位于 Packages/com.ember/Basic/Editor/Resources/ExcludedFolders.json，
    /// 首次访问时自动创建（含默认排除列表）。
    ///
    /// 使用方式：
    /// <code>
    /// // 检查单个路径
    /// if (EmberExcludedFolders.IsExcluded(filePath)) continue;
    ///
    /// // 强制重新加载（手动修改 JSON 后调用）
    /// EmberExcludedFolders.Reload();
    /// </code>
    /// </summary>
    public static class EmberExcludedFolders
    {
        #region 内部参数

        private const string TAG = LogTags.EmberBasic + "." + nameof(EmberExcludedFolders);

        /// <summary>JSON 文件相对于项目根目录的路径。</summary>
        private const string CONFIG_PATH = "Packages/com.ember/Basic/Editor/Resources/ExcludedFolders.json";

        private static List<string> _excludedFolders;
        private static bool _loaded;

        private static readonly string[] DefaultExclusions =
        {
            "Assets/Plugins/",
            "Assets/ThirdParty/",
            "Assets/TextMesh Pro/",
            // 包内 vendor 的第三方源码（随包分发，上游代码不改动，不参与框架代码规范校验）
            "Packages/com.ember/UniTask/"
        };

        /// <summary>
        /// 框架自身的包根目录 —— 除了 Assets/ 之外额外扫描的目录。
        /// 新增 Ember 包时在此添加即可。
        /// </summary>
        public static readonly string[] FrameworkPackageRoots =
        {
            "Packages/com.ember/"
        };

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static void EnsureLoaded()
        {
            if (_loaded) return;

            string fullPath = Path.GetFullPath(CONFIG_PATH);

            if (File.Exists(fullPath))
            {
                try
                {
                    string json = File.ReadAllText(fullPath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        _excludedFolders = new List<string>(DefaultExclusions);
                    }
                    else
                    {
                        var data = JsonUtility.FromJson<ExcludedFoldersData>(json);
                        _excludedFolders = data?.excludedFolders ?? new List<string>();
                    }
                }
                catch (Exception ex)
                {
                    EmberDebug.LogWarning(TAG, $"Failed to parse {CONFIG_PATH}: {ex.Message}. Using default exclusions.");
                    _excludedFolders = new List<string>(DefaultExclusions);
                }
            }
            else
            {
                EmberDebug.Log(TAG, $"{CONFIG_PATH} not found. Creating with default exclusions.");
                _excludedFolders = new List<string>(DefaultExclusions);
                SaveDefault(fullPath);
            }

            _loaded = true;
        }

        private static void SaveDefault(string fullPath)
        {
            try
            {
                var data = new ExcludedFoldersData
                {
                    excludedFolders = new List<string>(DefaultExclusions)
                };
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                string dir = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(fullPath, json);
            }
            catch (Exception ex)
            {
                EmberDebug.LogWarning(TAG, $"Failed to create {CONFIG_PATH}: {ex.Message}");
            }
        }

        /// <summary>
        /// 将输入路径标准化：反斜杠转正斜杠，去除末尾斜杠。
        /// </summary>
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            return path.Replace('\\', '/').TrimEnd('/');
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>
        /// 判断给定的路径是否在排除文件夹中。
        /// 支持绝对路径（来自 Directory.GetFiles）和 Assets 相对路径（来自 AssetDatabase）。
        /// </summary>
        /// <param name="path">文件或文件夹路径</param>
        /// <returns>路径在排除列表中时返回 true</returns>
        public static bool IsExcluded(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            EnsureLoaded();
            if (_excludedFolders == null || _excludedFolders.Count == 0) return false;

            string normalized = NormalizePath(path);

            // 尝试提取 Assets 相对路径（统一为 "Assets/xxx" 格式）
            string relative;
            string normalizedDataPath = NormalizePath(Application.dataPath);
            if (normalized.StartsWith(normalizedDataPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                // 绝对路径 → 补齐 Assets/ 前缀
                relative = "Assets/" + normalized.Substring(normalizedDataPath.Length + 1);
            }
            else if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                relative = normalized;
            }
            else if (normalized.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                // UPM 包路径（如包内 vendor 的第三方源码），同样支持目录排除
                relative = normalized;
            }
            else
            {
                // 既不是绝对路径，也不是 Assets/Packages 相对路径，不做排除
                return false;
            }

            relative = relative + "/"; // 确保目录边界检查

            foreach (string folder in _excludedFolders)
            {
                if (string.IsNullOrWhiteSpace(folder)) continue;
                string normalizedFolder = NormalizePath(folder);
                if (string.IsNullOrEmpty(normalizedFolder)) continue;

                string prefix = normalizedFolder + "/";
                if (relative.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 获取当前所有排除的文件夹路径（相对于项目根目录）。
        /// </summary>
        public static IReadOnlyList<string> GetExcludedPaths()
        {
            EnsureLoaded();
            return _excludedFolders;
        }

        /// <summary>
        /// 强制重新从文件加载配置（用户在外部修改 JSON 后调用）。
        /// </summary>
        public static void Reload()
        {
            _loaded = false;
            _excludedFolders = null;
            EnsureLoaded();
        }

        #endregion

        // --------------------------------------------------------

        /// <summary>
        /// JSON 反序列化结构。
        /// </summary>
        [Serializable]
        private class ExcludedFoldersData
        {
            public List<string> excludedFolders = new();
        }
    }
}
#endif
