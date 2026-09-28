using System;
using Ember.Basic;
using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>各模板共用的语言偏好、启动装配与语言切换入口。</summary>
    public static class EmberLocalization
    {
        #region 内部参数
        private const string PREFERENCE_KEY = "Ember.Localization.Language";
        private static string _language;
        private static EmberTextLocalizer _default;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { _language = null; _default = null; TextLocalization.SetLocalizer(null); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap() => Reload();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static string Language => _language ??= PlayerPrefs.GetString(PREFERENCE_KEY, string.Empty);
        public static ITextLocalizer Default => _default;
        [HasGC]
        public static void Reload()
        {
            var tables = Resources.LoadAll<EmberLocalizationTable>("Config/Localization");
            Array.Sort(tables, (a, b) => string.CompareOrdinal(a.TableId, b.TableId));
            var next = new EmberTextLocalizer(tables.Length > 0 ? tables[0].SourceLanguage : "zh_Hans",
                Array.Empty<string>(), () => Language);
            foreach (var table in tables) next.AddTable(table);
            bool replace = TextLocalization.Localizer == null || ReferenceEquals(TextLocalization.Localizer, _default);
            _default = next;
            if (replace) TextLocalization.SetLocalizer(next);
        }
        [HasGC]
        public static void SetLanguage(string language)
        {
            string next = language ?? string.Empty;
            if (Language == next) return;
            SetPreference(next);
            TextLocalization.PublishLanguageChanged(next);
        }
        /// <summary>用于旧模块兼容桥接；调用方负责一次语言变更广播。</summary>
        [HasGC]
        public static void SetPreference(string language)
        {
            _language = language ?? string.Empty;
            PlayerPrefs.SetString(PREFERENCE_KEY, _language);
            PlayerPrefs.Save();
        }
        [NoGC]
        public static void SetPreview(string language) => _language = language ?? string.Empty;
        [NoGC]
        public static void ResetCache() => _language = null;
        [HasGC]
        public static void RestoreDefault() => TextLocalization.SetLocalizer(_default);
        #endregion
    }
}
