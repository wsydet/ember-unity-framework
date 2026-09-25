using System;
using Ember.Basic;
using UnityEngine;

namespace Game.Narrative
{
    /// <summary>
    /// 全局语言偏好。它不属于剧情状态：只写 PlayerPrefs，不进存档、不影响剧情指纹，
    /// 因此切语言不会让任何既有存档失效。空字符串表示"跟随源语言"。
    /// </summary>
    public static class NovelLanguageSettings
    {
        #region 内部参数
        private const string PREFERENCE_KEY = "Ember.Novel.Language";
        private static string _current;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>当前语言标识；从未设置过时返回空字符串（由解析器回退源语言）。</summary>
        public static string Current
        {
            get
            {
                if (_current == null) _current = PlayerPrefs.GetString(PREFERENCE_KEY, string.Empty) ?? string.Empty;
                return _current;
            }
        }

        /// <summary>写入并持久化语言；值没有变化时什么都不做。</summary>
        [HasGC]
        public static void SetLanguage(string language)
        {
            string next = language ?? string.Empty;
            if (Current == next) return;
            _current = next;
            PlayerPrefs.SetString(PREFERENCE_KEY, next);
            PlayerPrefs.Save();
        }

        /// <summary>只改内存值、不写盘；供编辑器预览与测试隔离使用。</summary>
        [NoGC]
        public static void SetPreview(string language) => _current = language ?? string.Empty;

        /// <summary>清除内存缓存，让下次读取重新走 PlayerPrefs；供测试隔离使用。</summary>
        [NoGC]
        public static void ResetCache() => _current = null;
        #endregion
    }
}
