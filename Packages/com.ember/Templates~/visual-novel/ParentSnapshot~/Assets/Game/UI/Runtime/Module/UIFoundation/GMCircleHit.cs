using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>快捷钮的同心透明命中圆；几何求解使用相同直径。</summary>
    public sealed class GMCircleHit : MonoBehaviour, ICanvasRaycastFilter, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerExitHandler, IBeginDragHandler, ICancelHandler
    {
        #region 内部参数
        private Button _button;
        private float _diameter;
        private int _pointer = int.MinValue;
        private bool _shortcut;
        #endregion
        #region 生命周期
        private void OnDisable() { _pointer = int.MinValue; }
        private void OnApplicationFocus(bool focused) { if (!focused) _pointer = int.MinValue; }
        #endregion
        #region 外部方法
        public void Configure(Button button, float hit, float visible)
        { _shortcut = true; _button = button; _diameter = hit; GetComponent<Image>().raycastTarget = true; }
        public bool IsRaycastLocationValid(Vector2 screen, Camera camera)
        {
            if (!_shortcut) return true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screen, camera, out var local);
            return local.sqrMagnitude <= _diameter * _diameter * .25f;
        }
        public void OnPointerDown(PointerEventData e) { if (_shortcut && e.button == PointerEventData.InputButton.Left && _pointer == int.MinValue) _pointer = e.pointerId; }
        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != _pointer) return;
            _pointer = int.MinValue;
            if (_button && _button.interactable && IsRaycastLocationValid(e.position, e.pressEventCamera)) GetComponentInChildren<GMActionGesture>(true)?.ShortPressed?.Invoke();
        }
        public void OnPointerClick(PointerEventData e) { }
        public void OnPointerExit(PointerEventData e) { _pointer=int.MinValue; }
        public void OnBeginDrag(PointerEventData e) { _pointer=int.MinValue; }
        public void OnCancel(BaseEventData e) { _pointer=int.MinValue; }
        #endregion
    }
}
