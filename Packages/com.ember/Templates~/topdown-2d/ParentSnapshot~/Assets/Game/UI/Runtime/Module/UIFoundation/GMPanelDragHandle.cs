using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class GMPanelDragHandle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, ICancelHandler
    {
        #region 编辑器面板参数
        [SerializeField] private RectTransform _panel;
        [SerializeField] private Graphic _surface;
        #endregion
        #region 内部参数
        private int _pointer = int.MinValue;
        private Vector2 _startScreen, _startPosition;
        private Color _normal;
        #endregion
        #region 生命周期
        private void OnDisable() => Release();
        private void OnApplicationFocus(bool focused) { if (!focused) Release(); }
        #endregion
        #region 内部方法
        private void Release() { if (_pointer != int.MinValue && _surface) _surface.color = _normal; _pointer = int.MinValue; }
        #endregion
        #region 外部方法
        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || _pointer != int.MinValue) return;
            _pointer = e.pointerId; _startScreen = e.position; _startPosition = _panel.anchoredPosition;
            _normal = _surface.color; _surface.color = new Color(1, .74f, .15f, .9f);
        }
        public void OnPointerUp(PointerEventData e) { if (e.pointerId == _pointer) Release(); }
        public void OnCancel(BaseEventData e) => Release();
        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _pointer) return;
            var parent = (RectTransform)_panel.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, _startScreen, e.pressEventCamera, out var start);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, e.position, e.pressEventCamera, out var end);
            var p = _startPosition + end - start; var half = (parent.rect.size - _panel.rect.size) * .5f - Vector2.one * 12;
            half = Vector2.Max(Vector2.zero, half); _panel.anchoredPosition = new Vector2(Mathf.Clamp(p.x, -half.x, half.x), Mathf.Clamp(p.y, -half.y, half.y));
        }
        #endregion
    }
}
