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
                string speaker = string.IsNullOrEmpty(entry.Speaker) ? "旁白" : entry.Speaker;
                // Fixed columns in font units keep wrapped dialogue aligned below its body.
                float nameWidth = Entries.GetPreferredValues(speaker).x;
                float nameScale = Mathf.Min(1, Entries.fontSize * 5.5f / Mathf.Max(1, nameWidth));
                text.Append("<pos=0><color=#999999><size=")
                    .Append((nameScale * 100).ToString("0.##", CultureInfo.InvariantCulture))
                    .Append("%><noparse>").Append(speaker.Replace("<", "＜").Replace(">", "＞"))
                    .Append("</noparse></size></color>");
                if (i == history.Count - 1) text.Append("<pos=6em><color=#FFD52A>▶</color>");
                text.Append("<pos=7em><indent=7em>").Append((entry.Text ?? string.Empty).Replace("<", "<noparse><</noparse>")).Append("</indent>");
                if (i < history.Count - 1) text.Append("\n\n");
            }
            Entries.text = text.Length == 0 ? "尚无已完整显示的对白。" : text.ToString();
            Canvas.ForceUpdateCanvases();
            // Short histories sit in the middle; long histories open at the latest line.
            Entries.margin = Vector4.zero;
            float height = Entries.GetPreferredValues(Entries.text, HistoryScroll.viewport.rect.width, Mathf.Infinity).y;
            float padding = Mathf.Max(0, (HistoryScroll.viewport.rect.height - height) * .5f);
            Entries.margin = new Vector4(0, padding, 0, padding);
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
            base.OnInit(); _baseFontSize = Entries.fontSize; Close.onClick.AddListener(ClosePage); Saves.onClick.AddListener(OpenSaves);
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
