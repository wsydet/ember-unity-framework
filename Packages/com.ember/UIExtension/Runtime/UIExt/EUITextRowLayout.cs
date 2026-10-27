using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>按真实 TMP 换行计算行高；尺寸由父 LayoutGroup 写入，不使用第二个 fitter。</summary>
    public sealed class EUITextRowLayout : UIBehaviour, ILayoutElement
    {
        #region 编辑器面板参数
        [SerializeField] private TMP_Text _text;
        [SerializeField] private float _horizontalInset = 128;
        [SerializeField] private float _verticalInset = 32;
        [SerializeField] private float _minimumHeight = 72;
        #endregion
        #region 内部参数
        public float minWidth => 0;
        public float preferredWidth => -1;
        public float flexibleWidth => 1;
        public float minHeight => _minimumHeight;
        public float preferredHeight => !_text ? _minimumHeight : Mathf.Max(_minimumHeight,
            _text.GetPreferredValues(_text.text, Mathf.Max(1, ((RectTransform)transform).rect.width - _horizontalInset), Mathf.Infinity).y + _verticalInset);
        public float flexibleHeight => 0;
        public int layoutPriority => 1;
        #endregion
        #region 外部方法
        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
        #endregion
    }
}
