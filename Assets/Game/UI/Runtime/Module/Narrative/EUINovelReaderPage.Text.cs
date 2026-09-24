using Game.Narrative;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    // Reuses the EUI-center-authored Dialogue/Body/Speaker. No alternate page, generated binding or global owner.
    public partial class EUINovelReaderPage : INovelTextView, INovelTextEffectsView
    {
        #region 内部参数
        private bool _textLayoutCached, _storyDialogueVisible = true;
        private TextRect _dialogueTextRect, _bodyTextRect, _advanceTextRect;
        private NovelTextAppearance _normalAppearance;
        private Transform _dialogueParent;
        private int _dialogueSibling;
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
        private UnityEngine.UI.Graphic[] _advanceGraphics;
        private Sprite _textBackdropSprite;
        private Color _textBackdropColor;
        private UnityEngine.UI.Image.Type _textBackdropType;
        private bool _textBackdropPreserve, _textBackdropFill;
        private float _textBackdropPixels;
        private Transform _textDivider;
        private bool _textDividerVisible;
        private struct TextRect
        {
            private Vector2 _min, _max, _pivot, _position, _size;
            private Vector3 _scale;
            public TextRect(RectTransform rect)
            { _min = rect.anchorMin; _max = rect.anchorMax; _pivot = rect.pivot; _position = rect.anchoredPosition; _size = rect.sizeDelta; _scale = rect.localScale; }
            public void Apply(RectTransform rect)
            { rect.anchorMin = _min; rect.anchorMax = _max; rect.pivot = _pivot; rect.anchoredPosition = _position; rect.sizeDelta = _size; rect.localScale = _scale; }
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void CacheTextLayout()
        {
            if (_textLayoutCached) return;
            _advanceGraphics = Advance.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            _textBackdrop = Dialogue.GetComponent<UnityEngine.UI.Image>();
            if (_textBackdrop) { _textBackdropSprite = _textBackdrop.sprite; _textBackdropColor = _textBackdrop.color; _textBackdropType = _textBackdrop.type; _textBackdropPreserve = _textBackdrop.preserveAspect; _textBackdropFill = _textBackdrop.fillCenter; _textBackdropPixels = _textBackdrop.pixelsPerUnitMultiplier; }
            _textDivider = Dialogue.Find("SpeakerDivider"); _textDividerVisible = _textDivider && _textDivider.gameObject.activeSelf;
            _dialogueParent = Dialogue.parent; _dialogueSibling = Dialogue.GetSiblingIndex(); _advanceTextRect = new TextRect((RectTransform)Advance.transform);
            _textLayoutCached = true; _dialogueTextRect = new TextRect(Dialogue); _bodyTextRect = new TextRect(Body.rectTransform);
            _normalAppearance = new NovelTextAppearance(Body);
            _normalTextAlignment = Body.alignment; _normalTextOverflow = Body.overflowMode; _normalRichText = Body.richText; _normalParseControls = Body.parseCtrlCharacters;
        }
        private void ApplyTextMode(NovelTextMode mode)
        {
            CacheTextLayout();
            if (_activeTextMode == mode) return;
            _activeTextMode = mode; RestoreTextHierarchy(); _dialogueTextRect.Apply(Dialogue); _bodyTextRect.Apply(Body.rectTransform);
            Body.alignment = _normalTextAlignment;
            if (mode == NovelTextMode.Dialogue)
            {
                _normalAppearance.Apply(Body);
                if (_textBackdrop) { _textBackdrop.sprite = _textBackdropSprite; _textBackdrop.color = _textBackdropColor; _textBackdrop.type = _textBackdropType; _textBackdrop.preserveAspect = _textBackdropPreserve; _textBackdrop.fillCenter = _textBackdropFill; _textBackdrop.pixelsPerUnitMultiplier = _textBackdropPixels; }
            }
            else
            {
                var frame = mode == NovelTextMode.Title ? TitleLayout : FullScreenLayout;
                var text = mode == NovelTextMode.Title ? TitleBody : FullScreenBody;
                if (mode == NovelTextMode.Title)
                {
                    Dialogue.SetParent(TitleLayout.parent, false);
                    Dialogue.SetSiblingIndex(ReadingShading.GetSiblingIndex() + 1);
                    new TextRect(TitleAdvance).Apply((RectTransform)Advance.transform);
                }
                new TextRect(frame).Apply(Dialogue);
                new TextRect(text).Apply(Body.rectTransform);
                var backdrop = frame.GetComponent<UnityEngine.UI.Image>();
                if (_textBackdrop && backdrop)
                {
                    _textBackdrop.sprite = backdrop.sprite; _textBackdrop.color = backdrop.color;
                    _textBackdrop.type = backdrop.type; _textBackdrop.preserveAspect = backdrop.preserveAspect;
                    _textBackdrop.fillCenter = backdrop.fillCenter; _textBackdrop.pixelsPerUnitMultiplier = backdrop.pixelsPerUnitMultiplier;
                }
            }
            if (_textDivider) _textDivider.gameObject.SetActive(mode == NovelTextMode.Dialogue && _textDividerVisible);
        }

        private void RestoreTextHierarchy()
        {
            Dialogue.SetParent(_dialogueParent, false); Dialogue.SetSiblingIndex(_dialogueSibling);
            _advanceTextRect.Apply((RectTransform)Advance.transform);
        }
        private void ResetTextLayout()
        {
            if (!_textLayoutCached) return;
            RestoreTextHierarchy();
            _dialogueTextRect.Apply(Dialogue); _bodyTextRect.Apply(Body.rectTransform);
            _normalAppearance.Apply(Body); Body.alignment = _normalTextAlignment; Body.overflowMode = _normalTextOverflow; Body.richText = _normalRichText; Body.parseCtrlCharacters = _normalParseControls;
            Body.pageToDisplay = 1; Body.maxVisibleCharacters = int.MaxValue;
            _textMeasurementValid = false; _activeTextMode = NovelTextMode.Dialogue; _textPresentationCommand = null; _renderTextStart = 0;
            _storyDialogueVisible = true; Speaker.gameObject.SetActive(true); _text = null;
            if (_textBackdrop) { _textBackdrop.sprite = _textBackdropSprite; _textBackdrop.color = _textBackdropColor; _textBackdrop.type = _textBackdropType; _textBackdrop.preserveAspect = _textBackdropPreserve; _textBackdrop.fillCenter = _textBackdropFill; _textBackdrop.pixelsPerUnitMultiplier = _textBackdropPixels; }
            if (_textDivider) _textDivider.gameObject.SetActive(_textDividerVisible);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void SetTextEffects(float textOpacity, float cardOpacity)
        {
            if (_advanceGraphics != null) foreach (var graphic in _advanceGraphics) graphic.canvasRenderer.SetAlpha(Mathf.Clamp01(cardOpacity));
            var color = Body.color; color.a *= Mathf.Clamp01(textOpacity) * Mathf.Clamp01(cardOpacity); Body.color = color;
            if (_textBackdrop)
            {
                var backdrop = _activeTextMode == NovelTextMode.Title ? TitleLayout.GetComponent<UnityEngine.UI.Image>().color
                    : _activeTextMode == NovelTextMode.FullScreen ? FullScreenLayout.GetComponent<UnityEngine.UI.Image>().color : _textBackdropColor;
                backdrop.a *= Mathf.Clamp01(cardOpacity); _textBackdrop.color = backdrop;
            }
        }
        public int PrepareText(NovelCommand command, int start)
        {
            ApplyTextMode(command.TextMode);
            if (command.TextMode == NovelTextMode.Dialogue) _normalAppearance.Apply(Body, NovelReadingUI.FontScale);
            else new NovelTextAppearance((command.TextMode == NovelTextMode.Title ? TitleBody : FullScreenBody).GetComponent<TMP_Text>()).Apply(Body, NovelReadingUI.FontScale);
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
