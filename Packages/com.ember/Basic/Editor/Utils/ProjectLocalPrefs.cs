// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.IO;
using Ember.Basic;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 工程本地偏好存储（Editor-only）。
    ///
    /// 将 Editor 工具的持久化配置以 JSON 文件形式保存在
    /// <c>{ProjectRoot}/Library/EmberLocalPrefs/</c> 下，不污染项目 Assets 目录。
    ///
    /// 与 <see cref="UnityEditor.EditorPrefs"/> 的对比：
    /// <list type="bullet">
    ///   <item>EditorPrefs 存 Windows 注册表，换机器/重装系统会丢失</item>
    ///   <item>ProjectLocalPrefs 存 JSON 文件，可随项目 Git 管理、手动编辑</item>
    ///   <item>支持 migrateValueProvider 回调做数据迁移（EditorPrefs 不支持）</item>
    /// </list>
    ///
    /// 用法：
    /// <code>
    /// // 读取（带默认值和迁移回调）
    /// var lastPath = ProjectLocalPrefs.GetString("LastExportPath", "Assets/");
    ///
    /// // 写入
    /// ProjectLocalPrefs.SetString("LastExportPath", "Assets/Game/");
    ///
    /// // 从旧 key 迁移
    /// var val = ProjectLocalPrefs.GetString("NewKey", "", () => EditorPrefs.GetString("OldKey"));
    /// </code>
    /// </summary>
    public static class ProjectLocalPrefs
    {
        #region 内部参数

        private const string TAG = LogTags.EmberBasic + "." + nameof(ProjectLocalPrefs);
        private const string FolderName = "EmberLocalPrefs";
        private const string FileName = "prefs.json";

        private static PrefData _cachedData;
        private static bool _loaded;

        [Serializable]
        private class PrefEntry
        {
            public string key;
            public string value;
        }

        [Serializable]
        private class PrefData
        {
            public List<PrefEntry> entries = new();
        }

        #endregion

        // ============================================================

        #region 外部方法

        // ======== String ========

        /// <summary>
        /// 读取字符串值。
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="defaultValue">默认值</param>
        /// <param name="migrateValueProvider">迁移回调：key 不存在时调用，返回值自动写入</param>
        public static string GetString(string key, string defaultValue = "", Func<string> migrateValueProvider = null)
        {
            if (string.IsNullOrEmpty(key))
                return defaultValue;

            var data = LoadData();
            var entry = data.entries.Find(item => item.key == key);
            if (entry != null)
                return entry.value;

            if (migrateValueProvider == null)
                return defaultValue;

            var migratedValue = migrateValueProvider() ?? defaultValue;
            SetString(key, migratedValue);
            return migratedValue;
        }

        /// <summary>
        /// 写入字符串值。
        /// </summary>
        public static void SetString(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            value ??= string.Empty;

            var data = LoadData();
            var entry = data.entries.Find(item => item.key == key);
            if (entry == null)
            {
                data.entries.Add(new PrefEntry { key = key, value = value });
                SaveData();
                return;
            }

            if (entry.value == value)
                return;

            entry.value = value;
            SaveData();
        }

        // ======== Int ========

        /// <summary>
        /// 读取整数值。
        /// </summary>
        public static int GetInt(string key, int defaultValue = 0, Func<int> migrateValueProvider = null)
        {
            var value = GetString(
                key,
                string.Empty,
                migrateValueProvider == null ? null : () => migrateValueProvider().ToString());
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 写入整数值。
        /// </summary>
        public static void SetInt(string key, int value)
        {
            SetString(key, value.ToString());
        }

        // ======== Float ========

        /// <summary>
        /// 读取浮点数值。
        /// </summary>
        public static float GetFloat(string key, float defaultValue = 0f, Func<float> migrateValueProvider = null)
        {
            var value = GetString(
                key,
                string.Empty,
                migrateValueProvider == null ? null : () => migrateValueProvider().ToString("R"));
            return float.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 写入浮点数值。
        /// </summary>
        public static void SetFloat(string key, float value)
        {
            SetString(key, value.ToString("R"));
        }

        // ======== Bool ========

        /// <summary>
        /// 读取布尔值。
        /// </summary>
        public static bool GetBool(string key, bool defaultValue = false, Func<bool> migrateValueProvider = null)
        {
            var value = GetString(
                key,
                string.Empty,
                migrateValueProvider == null ? null : () => migrateValueProvider().ToString());
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 写入布尔值。
        /// </summary>
        public static void SetBool(string key, bool value)
        {
            SetString(key, value.ToString());
        }

        /// <summary>
        /// 删除指定 key 的存储。
        /// </summary>
        public static void DeleteKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            var data = LoadData();
            var entry = data.entries.Find(item => item.key == key);
            if (entry != null)
            {
                data.entries.Remove(entry);
                SaveData();
            }
        }

        /// <summary>
        /// 清空所有存储。
        /// </summary>
        public static void DeleteAll()
        {
            _cachedData = new PrefData();
            SaveData();
        }

        #endregion

        // ============================================================

        #region 内部方法

        private static PrefData LoadData()
        {
            if (_loaded)
                return _cachedData;

            _loaded = true;
            _cachedData = new PrefData();
            var path = GetFilePath();
            if (!File.Exists(path))
                return _cachedData;

            try
            {
                var json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<PrefData>(json);
                if (data?.entries != null)
                    _cachedData = data;
            }
            catch (Exception ex)
            {
                EmberDebug.LogWarning(TAG, $"读取工程本地偏好失败: {ex.Message}");
            }

            return _cachedData;
        }

        private static void SaveData()
        {
            try
            {
                var path = GetFilePath();
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(path, JsonUtility.ToJson(_cachedData, true));
            }
            catch (Exception ex)
            {
                EmberDebug.LogWarning(TAG, $"保存工程本地偏好失败: {ex.Message}");
            }
        }

        private static string GetFilePath()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, "Library", FolderName, FileName);
        }

        #endregion
    }
}
