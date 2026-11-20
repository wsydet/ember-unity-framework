using System;
using System.Collections.Generic;
using Ember.Basic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>只保存稳定 key；PlayerPrefs 已按应用/系统用户隔离，可在登录时切换业务用户命名空间。</summary>
    public static class GMFavorites
    {
        #region 内部参数
        [Serializable] private sealed class Data { public int version = 1; public List<string> keys = new(); }
        private static readonly List<string> Keys = new();
        private static string _user = "local";
        private static bool _loaded;
        private static string StorageKey => "Ember.GM.Favorites.v1." + Application.identifier + "." + _user;
        public static int Version { get; private set; }
        public static string Notice { get; private set; }
        public static event Action Changed;
        #endregion
        #region 内部方法
        private static void Load()
        {
            if (_loaded) return;
            _loaded = true; Keys.Clear(); Notice = null;
            string text = PlayerPrefs.GetString(StorageKey, ""); if (text.Length == 0) return;
            try
            {
                var data = JsonUtility.FromJson<Data>(text);
                if (data == null || data.version != 1 || data.keys == null || data.keys.Count > 1024) throw new FormatException();
                foreach (var key in data.keys) if (Valid(key) && !Keys.Contains(key) && Keys.Count < 5) Keys.Add(key);
                if (data.keys.Count != Keys.Count) { Notice = "收藏已整理，最多保留前 5 项"; Save(); }
            }
            catch (Exception) { Notice = "收藏记录损坏，已恢复为空"; Keys.Clear(); Save(); }
        }
        private static bool Valid(string key) => !string.IsNullOrWhiteSpace(key) && key.Length <= 256 && key.IndexOfAny(new[] { '\n', '\r', '\0' }) < 0;
        private static void Save()
        {
            try { PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(new Data { keys = new List<string>(Keys) })); PlayerPrefs.Save(); }
            catch (Exception) { Notice = "收藏暂仅在本次运行保留"; }
        }
        #endregion
        #region 外部方法
        [HasGC]
        public static void SetUserNamespace(string user) { _user = string.IsNullOrWhiteSpace(user) ? "local" : user; _loaded = false; Load(); Version++; Changed?.Invoke(); }
        [HasGC]
        public static string[] Snapshot() { Load(); return Keys.ToArray(); }
        [HasGC]
        public static bool Contains(string key) { Load(); return Keys.Contains(key); }
        [HasGC]
        public static bool Toggle(string key)
        {
            Load(); if (!Valid(key)) return false;
            if (Keys.Remove(key)) Notice = "已取消收藏";
            else
            {
                if (Keys.Count >= 5) { Notice = "最多收藏 5 项，请先取消一项"; Changed?.Invoke(); return false; }
                Keys.Add(key); Notice = "已收藏";
            }
            Save(); Version++; Changed?.Invoke(); return true;
        }
        #endregion
    }
}
