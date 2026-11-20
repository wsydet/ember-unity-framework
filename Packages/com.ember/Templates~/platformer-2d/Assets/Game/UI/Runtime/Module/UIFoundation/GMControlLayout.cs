using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>GM 专用响应布局，只定位正式制作节点，不影响其它 ConstrainedPanel。</summary>
    public sealed class GMControlLayout : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private RectTransform _title, _drag, _close, _state, _time, _frame, _viewport, _actions, _notice;
        #endregion
        #region 内部参数
        private RectTransform _rect, _parent;
        private Vector2 _parentSize;
        private bool _placed;
        #endregion
        #region 生命周期
        private void OnEnable() { _rect = (RectTransform)transform; _parent = _rect.parent as RectTransform; Refresh(); }
        private void LateUpdate() { if (_parent && _parent.rect.size != _parentSize) Refresh(); }
        #endregion
        #region 内部方法
        private static void Box(RectTransform r, float x, float y, float w, float h)
        { r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); }
        #endregion
        #region 外部方法
        public void Refresh()
        {
            if (!_parent) return;
            _parentSize = _parent.rect.size; bool portrait = _parentSize.y > _parentSize.x;
            float width = Mathf.Min(_parentSize.x - 32, portrait ? _parentSize.x * .88f : Mathf.Max(420, _parentSize.x * .36f));
            float height = Mathf.Min(_parentSize.y - 32, portrait ? _parentSize.y * .62f : _parentSize.y * .86f);
            _rect.anchorMin = _rect.anchorMax = _rect.pivot = Vector2.one * .5f; _rect.sizeDelta = new Vector2(width, height);
            if (!_placed) { _rect.anchoredPosition = new Vector2((_parentSize.x - width) * .5f - 24, (portrait ? -1 : 1) * ((_parentSize.y - height) * .5f - 24)); _placed = true; }
            else { var limit = Vector2.Max(Vector2.zero, (_parentSize - _rect.sizeDelta) * .5f - Vector2.one * 12); _rect.anchoredPosition = new Vector2(Mathf.Clamp(_rect.anchoredPosition.x, -limit.x, limit.x), Mathf.Clamp(_rect.anchoredPosition.y, -limit.y, limit.y)); }
            float inner = width - 40, unit = (inner - 36) / 4, square = unit * 2 + 12, circle = Mathf.Min(64, unit), header = circle + 20;
            Box(_title, 20, 16, square, circle); Box(_drag, 20 + 2 * (unit + 12) + (unit - circle) * .5f, 16, circle, circle); Box(_close, width - 20 - (unit + circle) * .5f, 16, circle, circle);
            Box(_state, 20, header + 12, square, square); Box(_time, 20 + 2 * (unit + 12), header + 12, unit * 2 + 12, square);
            var enable = (RectTransform)_time.Find("Enable"); var reset = (RectTransform)_time.Find("Reset"); var slider = _time.GetComponentInChildren<Slider>(true);
            Box(enable, (unit - circle) * .5f, (unit - circle) * .5f, circle, circle); Box(reset, (unit - circle) * .5f, unit + 12 + (unit - circle) * .5f, circle, circle);
            Box((RectTransform)slider.transform, unit + 12 + (unit - circle) * .5f, 0, circle, square);
            float frameTop = header + square + 24; Box(_frame, 20, frameTop, inner, 100);
            _viewport.anchorMin = Vector2.zero; _viewport.anchorMax = Vector2.one; _viewport.offsetMin = new Vector2(20, 42); _viewport.offsetMax = new Vector2(-20, -frameTop - 112);
            Box(_notice, 20, height - 34, inner, 26); RefreshActions();
        }
        public void RefreshActions()
        {
            if (!_actions) return;
            float width = _viewport.rect.width; var grid = _actions.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(Mathf.Max(100, (width - 12) * .5f), 82); _actions.sizeDelta = new Vector2(0, Mathf.Ceil(_actions.childCount / 2f) * 94);
        }
        #endregion
    }
}
