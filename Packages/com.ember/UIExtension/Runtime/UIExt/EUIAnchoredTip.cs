// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Ember.UIExtension
{
    /// <summary>显式点击打开的提示；在所属 Canvas 内避让边缘，不依赖 hover。</summary>
    public sealed class EUIAnchoredTip : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Button _trigger, _close;
        #endregion
        #region 内部参数
        private readonly Vector3[] _corners = new Vector3[4];
        #endregion
        #region 生命周期
        private void OnEnable() { if (_trigger) _trigger.onClick.AddListener(Toggle); if (_close) _close.onClick.AddListener(Hide); }
        private void OnDisable() { if (_trigger) _trigger.onClick.RemoveListener(Toggle); if (_close) _close.onClick.RemoveListener(Hide); Hide(); }
        #endregion
        #region 内部方法
        private void Toggle() { if (_panel.gameObject.activeSelf) Hide(); else ShowAt((RectTransform)_trigger.transform, _label.text); }
        #endregion
        #region 外部方法
        public void Hide() { if (_panel) _panel.gameObject.SetActive(false); }
        public void ShowAt(RectTransform anchor, string message)
        {
            if (!anchor) throw new System.ArgumentNullException(nameof(anchor));
            _label.text = message ?? string.Empty; _panel.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            var parent = (RectTransform)_panel.parent;
            ((RectTransform)canvas.transform).GetWorldCorners(_corners);
            var min = parent.InverseTransformPoint(_corners[0]); var max = parent.InverseTransformPoint(_corners[2]);
            anchor.GetWorldCorners(_corners);
            var point = parent.InverseTransformPoint((_corners[0] + _corners[3]) * .5f);
            var half = _panel.rect.size * .5f;
            var y = point.y - half.y - 8;
            if (y - half.y < min.y + 8) y = parent.InverseTransformPoint(_corners[1]).y + half.y + 8;
            _panel.localPosition = new Vector3(Mathf.Clamp(point.x, min.x + half.x + 8, max.x - half.x - 8), Mathf.Clamp(y, min.y + half.y + 8, max.y - half.y - 8), 0);
            _panel.SetAsLastSibling();
        }
        #endregion
    }
}
