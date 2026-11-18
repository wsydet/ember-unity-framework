using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>宽度变化时重排滚动内容，保留每个控件的首选高度。</summary>
    public sealed class EUIResponsiveFlowLayout : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private float _minimumCellWidth = 280;
        [SerializeField] private float _spacing = 16;
        [SerializeField, Range(1,6)] private int _maximumColumns = 3;
        #endregion
        #region 内部参数
        private float _width = -1;
        private int _count = -1;
        #endregion
        #region 生命周期
        private void OnEnable() { _width = -1; }
        private void OnTransformChildrenChanged() { _width = -1; }
        private void LateUpdate()
        {
            var root = (RectTransform)transform;
            if (Mathf.Abs(_width - root.rect.width) < .1f && _count == root.childCount) return;
            _width = root.rect.width; _count = root.childCount;
            int columns = Mathf.Clamp(Mathf.FloorToInt((_width + _spacing) / (_minimumCellWidth + _spacing)), 1, Mathf.Max(1,_maximumColumns));
            float cellWidth = Mathf.Max(1, (_width - (columns - 1) * _spacing) / columns);
            float y = 0, rowHeight = 0; int index = 0;
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i) as RectTransform; if (!child || !child.gameObject.activeSelf) continue;
                int column = index % columns;
                if (column == 0 && index > 0) { y += rowHeight + _spacing; rowHeight = 0; }
                var layout = child.GetComponent<LayoutElement>();
                float height = layout && layout.preferredHeight > 0 ? layout.preferredHeight : Mathf.Max(64, child.sizeDelta.y);
                child.anchorMin = child.anchorMax = new Vector2(0, 1); child.pivot = new Vector2(0, 1);
                float preferredWidth = layout && layout.preferredWidth > 0 ? Mathf.Min(cellWidth, layout.preferredWidth) : cellWidth;
                child.sizeDelta = new Vector2(preferredWidth, height); child.anchoredPosition = new Vector2(column * (cellWidth + _spacing) + (cellWidth - preferredWidth) * .5f, -y);
                rowHeight = Mathf.Max(rowHeight, height); index++;
            }
            root.sizeDelta = new Vector2(root.sizeDelta.x, y + rowHeight + _spacing);
        }
        #endregion
    }
}
