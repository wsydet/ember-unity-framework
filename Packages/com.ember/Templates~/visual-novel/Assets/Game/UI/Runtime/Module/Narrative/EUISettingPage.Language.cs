using System;
using System.Collections.Generic;
using Ember.Core;
using Ember.UI;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 设置面板的「语言」选择：按钮文案各自用自己的语言，不挂多语言 Key
    /// （切语言时不该把「日本語」翻译成别的），高亮按当前语言刷新。
    ///
    /// <para>切换走 <c>NovelLocalization.SetLanguage</c>：它会持久化到 PlayerPrefs、
    /// 刷新所有 TMPEx 文本并广播 Changed，所以切完当场就能看到界面文案变化。</para>
    ///
    /// <para>默认中文：语言偏好为空时解析器回退源语言列（<c>zh_Hans</c>），
    /// 因此未做过任何选择时高亮落在列表第一个（<c>novel_languages.order = 0</c>，即源语言）。</para>
    /// </summary>
    public partial class EUISettingPage
    {
        #region 内部参数

        /// <summary>绑定控件名 → novel_languages 的语言标识。</summary>
        private static readonly (string Control, string Language)[] LanguageControls =
        {
            ("LanguageZhHans", "zh_Hans"),
            ("LanguageZhHant", "zh_Hant"),
            ("LanguageJa", "ja"),
            ("LanguageEn", "en")
        };

        private readonly Dictionary<string, Button> _languageButtons = new();
        private IDisposable _languageSubscription;

        /// <summary>novel_languages 里 <c>isSource = true</c> 的那一行；解析器没装配时用它兜底。</summary>
        private const string SourceLanguage = "zh_Hans";

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void InitLanguagePreferences()
        {
            // ControlMap 可能滞后于一次重新生成，取不到就静默跳过，不影响其它设置项。
            foreach (var pair in LanguageControls)
            {
                if (!ControlMap.TryGetValue(pair.Control, out var control) || control is not Button button) continue;
                string language = pair.Language;
                _languageButtons[language] = button;
                button.onClick.AddListener(() => SelectLanguage(language));
            }

            if (_languageSubscription == null)
            {
                // 订阅框架的全局语言事件，而不是业务层的静态事件：
                // 设置页只关心「语言变了，重刷高亮与单位文案」，不需要认识 Game.Narrative。
                _languageSubscription = EmberEventBus.Subscribe<string>(EmberBroadcastEvent.LanguageChanged, OnLanguageChanged);
            }
            RefreshLanguagePreferences();
        }

        /// <summary>语言变更：重刷选中高亮，并让「字/秒」「秒」「正常/减弱/关闭」这些运行期文案跟着换语言。</summary>
        private void OnLanguageChanged(string language)
        {
            RefreshLanguagePreferences();
            RefreshNovelPreferences();
        }

        private void SelectLanguage(string language)
        {
            Game.Narrative.NovelLocalization.SetLanguage(language);
            RefreshLanguagePreferences();
        }

        private void RefreshLanguagePreferences()
        {
            if (_languageButtons.Count == 0) return;

            string current = Game.Narrative.NovelLanguageSettings.Current;
            if (string.IsNullOrEmpty(current))
            {
                // 语言偏好为空 = 跟随源语言列，也就是默认中文。列表按 order 排序、源语言在最前，
                // 所以优先取解析器的第一个语言；解析器未装配（例如编辑期没开试播窗口）时
                // 退回 novel_languages 里 isSource = true 的那一行。
                var languages = Game.Narrative.NovelLocalization.Localizer?.Languages;
                current = languages != null && languages.Count > 0 ? languages[0] : SourceLanguage;
            }

            foreach (var pair in _languageButtons)
                ApplySelected(pair.Value, pair.Key == current);
        }

        private static void ApplySelected(Button button, bool selected)
        {
            if (!button || button.targetGraphic == null) return;
            button.targetGraphic.color = selected ? button.colors.selectedColor : button.colors.normalColor;
        }

        private void ReleaseLanguagePreferences()
        {
            foreach (var button in _languageButtons.Values)
                if (button) button.onClick.RemoveAllListeners();
            _languageButtons.Clear();

            _languageSubscription?.Dispose();
            _languageSubscription = null;
        }

        #endregion
    }
}
