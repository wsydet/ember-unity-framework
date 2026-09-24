using Game.Narrative;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Editor
{
    public sealed partial class NovelGameplayLayoutWindow
    {
        #region 内部参数
        private NovelTextMode _previewTextMode;
        #endregion

        // --------------------------------------------------------
        #region 内部方法

        private void ApplyTextLayoutPreview()
        {
            string selected = Keys[_selected];
            var mode = selected.StartsWith("FullScreen") ? NovelTextMode.FullScreen
                : selected.StartsWith("Title") ? NovelTextMode.Title
                : _nodePreview ? _previewTextMode : NovelTextMode.Dialogue;
            if (mode == NovelTextMode.Dialogue) return;
            string prefix = mode == NovelTextMode.Title ? "Title" : "FullScreen";
            var frame = _previewTargets[prefix + "Layout"];
            var sourceText = _previewTargets[prefix + "Body"];
            var dialogue = _previewTargets["Dialogue"];
            var body = _previewTargets["Body"].GetComponent<TMP_Text>();
            if (mode == NovelTextMode.Title)
            {
                dialogue.SetParent(frame.parent, false);
                dialogue.SetSiblingIndex(_previewTargets["Shading"].GetSiblingIndex() + 1);
                new LayoutRecord("", _previewTargets["TitleAdvance"]).Apply(_previewTargets["Advance"]);
            }
            new LayoutRecord("", frame).Apply(dialogue);
            new LayoutRecord("", sourceText).Apply(body.rectTransform);
            string content = body.text;
            UnityEditor.EditorUtility.CopySerialized(sourceText.GetComponent<TMP_Text>(), body);
            body.text = content;
            var source = frame.GetComponent<Image>();
            var target = dialogue.GetComponent<Image>();
            target.sprite = source.sprite; target.color = source.color; target.type = source.type;
            target.preserveAspect = source.preserveAspect; target.fillCenter = source.fillCenter;
            target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            if (!_nodePreview) body.text = sourceText.GetComponent<TMP_Text>().text;
            _previewTargets["Speaker"].gameObject.SetActive(false);
            var divider = dialogue.Find("SpeakerDivider");
            if (divider) divider.gameObject.SetActive(false);
            _previewTargets["ReadingControls"].gameObject.SetActive(mode != NovelTextMode.Title);
            // Layout/style source nodes remain hidden; only the real reader controls render.
            frame.gameObject.SetActive(false);
        }
        #endregion
    }
}
