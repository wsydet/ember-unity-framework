// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using TMPro;
using UnityEngine;
namespace Ember.UIExtension
{
    /// <summary>徽标仅消费展示状态，不查询业务红点。</summary>
    public sealed class EUIBadgeView : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private GameObject _dot, _tag;
        [SerializeField] private TMP_Text _label;
        #endregion
        #region 外部方法
        public void Hide() { _dot.SetActive(false); _tag.SetActive(false); }
        public void ShowDot() { _dot.SetActive(true); _tag.SetActive(false); }
        public void ShowCount(int count, int maximum = 99)
        {
            if (maximum < 1) throw new System.ArgumentOutOfRangeException(nameof(maximum));
            if (count <= 0) { Hide(); return; }
            ShowLabel(count > maximum ? maximum + "+" : count.ToString());
        }
        public void ShowLabel(string text) { _dot.SetActive(false); _tag.SetActive(!string.IsNullOrEmpty(text)); _label.text = text ?? string.Empty; }
        #endregion
    }
}
