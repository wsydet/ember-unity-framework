using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using Ember.UI;
using Game.Narrative;
using UnityEngine;

namespace Game.UI
{
    public partial class EUINovelHistoryPage
    {
        #region 内部参数
        private IDisposable _pause;
        private float _baseFontSize;
        private Vector4 _baseMargin;
        private NovelHistoryAppearance _appearance;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void ClosePage() => EUIManager.Instance.ClosePage(Page);
        private void OpenSaves() => NovelSaveUI.Open();
        private void ShowHistory(IReadOnlyList<NovelHistoryEntry> history)
        {
            Entries.fontSize = _baseFontSize * NovelReadingUI.FontScale; Entries.parseCtrlCharacters = false;
            var text = new StringBuilder();
            for (int i = 0; i < history.Count; i++)
            {
                var entry = history[i];
                string speaker = string.IsNullOrEmpty(entry.Speaker)
                    ? NovelLocalization.Runtime("ui.history.Narrator", "旁白") : entry.Speaker;
                // Fixed columns in font units keep wrapped dialogue aligned below its body.
                float nameWidth = Entries.GetPreferredValues(speaker).x;
                float nameScale = Mathf.Min(1, Entries.fontSize * _appearance.SpeakerWidth / Mathf.Max(1, nameWidth));
                text.Append("<pos=0><color=#").Append(ColorUtility.ToHtmlStringRGBA(_appearance.SpeakerColor)).Append("><size=")
                    .Append((nameScale * 100).ToString("0.##", CultureInfo.InvariantCulture))
                    .Append("%><noparse>").Append(speaker.Replace("<", "＜").Replace(">", "＞"))
                    .Append("</noparse></size></color>");
                if (i == history.Count - 1) text.Append("<pos=").Append(_appearance.MarkerPosition.ToString(CultureInfo.InvariantCulture)).Append("em><color=#").Append(ColorUtility.ToHtmlStringRGBA(_appearance.LatestMarkerColor)).Append(">▶</color>");
                // 正文走 HistoryText：没有文字变量绑定的句子按 Key 用当前语言重新解析，
                // 所以回看旧句时会跟着当前语言走，而不是固定成读到那句时的语言。
                text.Append("<pos=").Append(_appearance.TextIndent.ToString(CultureInfo.InvariantCulture)).Append("em><indent=").Append(_appearance.TextIndent.ToString(CultureInfo.InvariantCulture)).Append("em>").Append((NovelLocalization.HistoryText(entry) ?? string.Empty).Replace("<", "<noparse><</noparse>")).Append("</indent>");
                if (i < history.Count - 1) text.Append("\n\n");
            }
            Entries.text = text.Length == 0 ? NovelLocalization.Runtime("ui.history.Empty", "尚无已完整显示的对白。") : text.ToString();
            Canvas.ForceUpdateCanvases();
            // Short histories sit in the middle; long histories open at the latest line.
            Entries.margin = _baseMargin;
            float height = Entries.GetPreferredValues(Entries.text, HistoryScroll.viewport.rect.width, Mathf.Infinity).y;
            float padding = _appearance.CenterShortHistory ? Mathf.Max(0, (HistoryScroll.viewport.rect.height - height) * .5f) : 0;
            Entries.margin = _baseMargin + new Vector4(0, padding, 0, padding);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(Entries.rectTransform);
            Canvas.ForceUpdateCanvases();
            HistoryScroll.StopMovement();
            HistoryScroll.verticalNormalizedPosition = 0;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override void OnInit()
        {
            base.OnInit(); _baseFontSize = Entries.fontSize; _baseMargin = Entries.margin; _appearance = Entries.GetComponent<NovelHistoryAppearance>(); Close.onClick.AddListener(ClosePage); Saves.onClick.AddListener(OpenSaves);
            NovelSkin.Apply(this, "EUINovelHistoryPage");
        }
        public override void OnOpen(object param)
        {
            base.OnOpen(param); _pause?.Dispose();
            var session = param as NovelSession;
            if (session == null || session.IsDisposed) { ClosePage(); return; }
            _pause = session.AcquirePause("History");
            ShowHistory(session.History);
        }
        public override void OnClose() { _pause?.Dispose(); _pause = null; base.OnClose(); }
        public override void OnDispose()
        {
            _pause?.Dispose(); _pause = null; Close.onClick.RemoveListener(ClosePage); Saves.onClick.RemoveListener(OpenSaves); base.OnDispose();
        }
        #endregion
    }
}
