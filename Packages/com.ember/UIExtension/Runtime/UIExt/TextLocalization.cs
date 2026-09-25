// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System.Collections.Generic;
using Ember.Basic;
using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>
    /// <see cref="TMPEx"/> 的解析入口与活动实例注册表。
    /// 未注入 <see cref="ITextLocalizer"/> 时所有 TMPEx 保持原文，行为与普通 TMP 完全一致，
    /// 因此未接入多语言的项目不需要做任何改动。
    /// </summary>
    public static class TextLocalization
    {
        #region 内部参数

        private static readonly List<TMPEx> Live = new List<TMPEx>(32);
        private static ITextLocalizer _localizer;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>当前解析器；未注入时为 null。</summary>
        public static ITextLocalizer Localizer => _localizer;

        /// <summary>当前语言标识；未注入解析器时返回空字符串。</summary>
        public static string CurrentLanguage => _localizer == null ? string.Empty : _localizer.CurrentLanguage;

        /// <summary>注入解析器并立即重刷活动文本；传 null 表示取消本地化（全部回退原文）。</summary>
        [HasGC]
        public static void SetLocalizer(ITextLocalizer localizer)
        {
            _localizer = localizer;
            RefreshAll();
        }

        /// <summary>按当前语言解析 key；解析器缺失、key 为空或查不到条目时返回 false。</summary>
        [HasGC]
        public static bool TryResolve(string key, out string text)
        {
            text = null;
            return _localizer != null && !string.IsNullOrEmpty(key) && _localizer.TryGet(key, out text);
        }

        /// <summary>按指定语言解析 key，供编辑器预览使用。</summary>
        [HasGC]
        public static bool TryResolve(string key, string language, out string text)
        {
            text = null;
            return _localizer != null && !string.IsNullOrEmpty(key) && _localizer.TryGet(key, language, out text);
        }

        /// <summary>语言切换后重刷所有活动 TMPEx。</summary>
        [HasGC]
        public static void RefreshAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                var target = Live[i];
                if (!target) { Live.RemoveAt(i); continue; }
                target.ApplyLocalizedText();
            }
        }

        /// <summary>活动实例数量，供测试与诊断使用。</summary>
        public static int LiveCount => Live.Count;

        [NoGC]
        internal static void Register(TMPEx target)
        {
            if (target && !Live.Contains(target)) Live.Add(target);
        }

        [NoGC]
        internal static void Unregister(TMPEx target) => Live.Remove(target);

        #endregion
    }
}
