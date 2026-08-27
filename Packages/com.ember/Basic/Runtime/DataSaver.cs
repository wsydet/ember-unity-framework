// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

using System;
using System.IO;
using UnityEngine;

namespace Ember.Basic
{
    /// <summary>
    /// 基于 JsonUtility 的存档工具 —— JSON 序列化到 Application.persistentDataPath。
    ///
    /// 使用方式：
    /// <code>
    /// DataSaver.Save("settings.json", mySettings);
    /// if (DataSaver.TryLoad《MySettings》("settings.json", out var data))
    ///     ApplySettings(data);
    /// DataSaver.Delete("settings.json");
    /// </code>
    ///
    /// 异步版本（UniTask）待迁移（同包内 Extensions 程序集）。
    /// </summary>
    public static class DataSaver
    {
        /// <summary>
        /// 保存对象为 JSON 文件（同步，会阻塞主线程直到写入完毕）。
        /// 小文件（几 KB）可以直接用，大文件或频繁写入用异步版。
        /// </summary>
        [HasGC]
        public static void Save<T>(string fileName, T data) where T : class
        {
            string fullPath = GetPath(fileName);
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(fullPath, json);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[Ember] DataSaver.Save failed: {fullPath}\n{e}");
            }
        }

        /// <summary>
        /// 从 JSON 文件加载对象。文件不存在或格式错误返回 false。
        /// </summary>
        [HasGC]
        public static bool TryLoad<T>(string fileName, out T data) where T : class
        {
            data = null;
            string fullPath = GetPath(fileName);

            if (!File.Exists(fullPath)) return false;

            try
            {
                string json = File.ReadAllText(fullPath);
                data = JsonUtility.FromJson<T>(json);
                return data != null;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[Ember] DataSaver.TryLoad failed: {fullPath}\n{e}");
                return false;
            }
        }

        /// <summary>
        /// 删除存档文件。文件不存在不报错。
        /// </summary>
        public static void Delete(string fileName)
        {
            string fullPath = GetPath(fileName);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }

        /// <summary>
        /// 存档文件是否存在。
        /// </summary>
        [NoGC]
        public static bool Exists(string fileName) => File.Exists(GetPath(fileName));

        [NoGC]
        private static string GetPath(string fileName)
            => Path.Combine(Application.persistentDataPath, fileName);
    }
}
