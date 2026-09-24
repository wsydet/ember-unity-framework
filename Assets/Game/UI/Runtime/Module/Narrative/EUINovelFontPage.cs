using System;
using Ember.UI;
using Game.Narrative;
using UnityEngine;

namespace Game.UI
{
    public partial class EUINovelFontPage
    {
        private IDisposable _pause;
        private float _previewFontSize;
        private Game.NovelSave.NovelSaveModule _save;
        private void ClosePage() => EUIManager.Instance.ClosePage(Page);
        private void SetSmall() => SelectSize(0);
        private void SetMedium() => SelectSize(1);
        private void SetLarge() => SelectSize(2);
        private void SelectSize(int size)
        {
            _save?.SaveReadingDisplay(size, Mathf.Clamp(_save.Account.ReadingMultiplier, 1, 3)); Refresh();
        }
        private void Refresh()
        {
            int selected = Mathf.Clamp(_save?.Account?.FontSize ?? 1, 0, 2);
            var buttons = new[] { Small, Medium, Large };
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].targetGraphic.color = i == selected ? buttons[i].colors.selectedColor : buttons[i].colors.normalColor;
            Preview.fontSize = _previewFontSize * NovelReadingUI.FontScale;
        }
        public override void OnInit()
        {
            base.OnInit(); _previewFontSize = Preview.fontSize; Close.onClick.AddListener(ClosePage);
            Small.onClick.AddListener(SetSmall); Medium.onClick.AddListener(SetMedium); Large.onClick.AddListener(SetLarge);
        }
        public override void OnOpen(object param)
        {
            base.OnOpen(param); var session = param as NovelSession;
            if (session == null || session.IsDisposed) { ClosePage(); return; }
            _pause = session.AcquirePause("FontPopup"); _save = NovelSaveUI.Module;
            if (_save != null) _save.Changed += Refresh; Refresh();
        }
        public override void OnClose()
        {
            if (_save != null) _save.Changed -= Refresh; _save = null;
            _pause?.Dispose(); _pause = null; base.OnClose();
        }
        public override void OnDispose()
        {
            OnClose(); Close.onClick.RemoveListener(ClosePage);
            Small.onClick.RemoveListener(SetSmall); Medium.onClick.RemoveListener(SetMedium); Large.onClick.RemoveListener(SetLarge); base.OnDispose();
        }
    }
}
