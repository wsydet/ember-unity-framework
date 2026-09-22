using Game.Narrative;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    // Reuses the EUI-center-authored Dialogue/Body/Speaker. No alternate page, generated binding or global owner.
    public partial class EUINovelReaderPage : INovelTextView
    {
        #region 内部参数
        private bool _textLayoutCached, _storyDialogueVisible = true;
        private TextRect _dialogueTextRect, _bodyTextRect;
        private TextAlignmentOptions _normalTextAlignment;
        private TextOverflowModes _normalTextOverflow;
        private bool _normalRichText, _normalParseControls;
        private NovelTextMode _activeTextMode;
        private NovelCommand _textPresentationCommand;
        private int _renderTextStart, _measuredPageEnd;
        private bool _textMeasurementValid;
        private Vector2 _measuredTextSize;
        private float _measuredFontSize;
        private UnityEngine.UI.Image _textBackdrop;
        private Sprite _textBackdropSprite;
        private Color _textBackdropColor;
        private Transform _textDivider;
        private bool _textDividerVisible;
        private struct TextRect
        {
            private Vector2 _min, _max, _pivot, _position, _size;
            public TextRect(RectTransform rect)
            { _min = rect.anchorMin; _max = rect.anchorMax; _pivot = rect.pivot; _position = rect.anchoredPosition; _size = rect.sizeDelta; }
            public void Apply(RectTransform rect)
            { rect.anchorMin = _min; rect.anchorMax = _max; rect.pivot = _pivot; rect.anchoredPosition = _position; rect.sizeDelta = _size; }
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void CacheTextLayout()
        {
            if (_textLayoutCached) return;
            _textBackdrop = Dialogue.GetComponent<UnityEngine.UI.Image>();
            if (_textBackdrop) { _textBackdropSprite = _textBackdrop.sprite; _textBackdropColor = _textBackdrop.color; }
            _textDivider = Dialogue.Find("SpeakerDivider"); _textDividerVisible = _textDivider && _textDivider.gameObject.activeSelf;
            _textLayoutCached = true; _dialogueTextRect = new TextRect(Dialogue); _bodyTextRect = new TextRect(Body.rectTransform);
            _normalTextAlignment = Body.alignment; _normalTextOverflow = Body.overflowMode; _normalRichText = Body.richText; _normalParseControls = Body.parseCtrlCharacters;
        }
        private void ApplyTextMode(NovelTextMode mode)
        {
            CacheTextLayout();
            if (_activeTextMode == mode) return;
            _activeTextMode = mode; _dialogueTextRect.Apply(Dialogue); _bodyTextRect.Apply(Body.rectTransform);
            Body.alignment = _normalTextAlignment;
            if (_textBackdrop)
            {
                _textBackdrop.sprite = mode == NovelTextMode.Dialogue ? _textBackdropSprite : null;
                _textBackdrop.color = mode == NovelTextMode.Dialogue ? _textBackdropColor : new Color(.02f, .025f, .04f, .85f);
            }
            if (_textDivider) _textDivider.gameObject.SetActive(mode == NovelTextMode.Dialogue && _textDividerVisible);
            if (mode != NovelTextMode.Dialogue)
            {
                // Safe-area-relative margins leave the existing bottom reading controls/advance surface accessible.
                Dialogue.anchorMin = new Vector2(.06f, .16f); Dialogue.anchorMax = new Vector2(.94f, .9f);
                Dialogue.offsetMin = Dialogue.offsetMax = Vector2.zero;
                Body.rectTransform.anchorMin = Vector2.zero; Body.rectTransform.anchorMax = Vector2.one;
                Body.rectTransform.offsetMin = new Vector2(36, 36); Body.rectTransform.offsetMax = new Vector2(-36, -36);
                Body.alignment = mode == NovelTextMode.Title ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft;
            }
        }
        private void ResetTextLayout()
        {
            if (!_textLayoutCached) return;
            _dialogueTextRect.Apply(Dialogue); _bodyTextRect.Apply(Body.rectTransform);
            Body.alignment = _normalTextAlignment; Body.overflowMode = _normalTextOverflow; Body.richText = _normalRichText; Body.parseCtrlCharacters = _normalParseControls;
            Body.pageToDisplay = 1; Body.maxVisibleCharacters = int.MaxValue;
            _textMeasurementValid = false; _activeTextMode = NovelTextMode.Dialogue; _textPresentationCommand = null; _renderTextStart = 0;
            _storyDialogueVisible = true; Speaker.gameObject.SetActive(true); _text = null;
            if (_textBackdrop) { _textBackdrop.sprite = _textBackdropSprite; _textBackdrop.color = _textBackdropColor; }
            if (_textDivider) _textDivider.gameObject.SetActive(_textDividerVisible);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public int PrepareText(NovelCommand command, int start)
        {
            ApplyTextMode(command.TextMode);
            Body.fontSize = _bodyFontSize * NovelReadingUI.FontScale;
            Body.richText = false; Body.parseCtrlCharacters = false; Body.overflowMode = TextOverflowModes.Page; Body.pageToDisplay = 1;
            Body.rectTransform.ForceUpdateRectTransforms();
            bool textChanged = _textPresentationCommand != command || _renderTextStart != start;
            var size = Body.rectTransform.rect.size;
            if (!textChanged && _textMeasurementValid && size == _measuredTextSize && Mathf.Approximately(Body.fontSize, _measuredFontSize))
                return _measuredPageEnd;
            if (textChanged)
            {
                _textPresentationCommand = command; _renderTextStart = start;
                Body.text = NovelTextRules.Slice(command.Text, start); _text = null;
            }
            Body.maxVisibleCharacters = int.MaxValue;
            Body.ForceMeshUpdate(true);
            _textMeasurementValid = true; _measuredTextSize = size; _measuredFontSize = Body.fontSize;
            int remaining = NovelTextRules.Length(command.Text) - start;
            if (Body.textInfo.pageCount <= 1 || Body.textInfo.characterCount == 0) return _measuredPageEnd = start + remaining;
            int nextFirst = Body.textInfo.pageInfo[1].firstCharacterIndex;
            if (nextFirst > 0 && nextFirst < Body.textInfo.characterCount)
                return _measuredPageEnd = start + Mathf.Clamp(NovelTextRules.Length(Body.text, Body.textInfo.characterInfo[nextFirst].index), 1, remaining);
            int lastIndex = Mathf.Clamp(Body.textInfo.pageInfo[0].lastCharacterIndex, 0, Body.textInfo.characterCount - 1);
            var last = Body.textInfo.characterInfo[lastIndex];
            return _measuredPageEnd = start + Mathf.Clamp(NovelTextRules.Length(Body.text, last.index + last.stringLength), 1, remaining);
        }
        public void ShowText(string speaker, int visibleCharacters)
        {
            Speaker.gameObject.SetActive(_activeTextMode == NovelTextMode.Dialogue);
            Speaker.text = speaker ?? string.Empty;
            if (visibleCharacters == int.MaxValue) { Body.maxVisibleCharacters = int.MaxValue; return; }
            int utf16End = NovelTextRules.Utf16Index(Body.text, Mathf.Max(0, visibleCharacters - _renderTextStart));
            int visible = 0;
            while (visible < Body.textInfo.characterCount)
            {
                var character = Body.textInfo.characterInfo[visible];
                if (character.index + character.stringLength > utf16End) break;
                visible++;
            }
            Body.maxVisibleCharacters = visible;
        }
        public void SetStoryDialogueVisible(bool visible)
        { _storyDialogueVisible = visible; Dialogue.gameObject.SetActive(visible && _session?.DialogueHidden != true); }
        #endregion
    }
}
