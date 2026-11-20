using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>短按、右键、长按互斥；移动后将拖动继续交给扩展 ScrollRect。</summary>
    public sealed class GMActionGesture : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, ICancelHandler, ISubmitHandler
    {
        #region 内部参数
        private int _pointer = int.MinValue;
        private Vector2 _start;
        private float _pressedAt, _lastFavorite = -1;
        private bool _consumed;
        private ScrollRect _scroll;
        public Action ShortPressed, FavoriteRequested;
        #endregion
        #region 生命周期
        private void Update()
        {
            if (_pointer == int.MinValue || _consumed || Time.unscaledTime - _pressedAt < .55f) return;
            _consumed = true; Favorite();
        }
        private void OnDisable() => Cancel();
        private void OnApplicationFocus(bool focus) { if (!focus) Cancel(); }
        #endregion
        #region 内部方法
        private void Cancel() { _pointer = int.MinValue; _consumed = true; }
        private void Favorite() { if (Time.unscaledTime - _lastFavorite < .4f) return; _lastFavorite = Time.unscaledTime; FavoriteRequested?.Invoke(); }
        #endregion
        #region 外部方法
        public void OnPointerDown(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Right) { Cancel(); Favorite(); return; }
            if (e.button != PointerEventData.InputButton.Left) return;
            if(_pointer!=int.MinValue){if(e.pointerId!=_pointer)Cancel();return;}
            _pointer = e.pointerId; _start = e.position; _pressedAt = Time.unscaledTime; _consumed = false;
            _scroll = GetComponentInParent<ScrollRect>();
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != _pointer) return;
            bool execute = !_consumed; Cancel(); if (execute) ShortPressed?.Invoke();
        }
        public void OnPointerClick(PointerEventData e) { }
        public void OnPointerExit(PointerEventData e) { if (e.pointerId == _pointer) Cancel(); }
        public void OnBeginDrag(PointerEventData e) { _consumed = true; if (_scroll) _scroll.OnBeginDrag(e); }
        public void OnDrag(PointerEventData e)
        {
            if ((e.position - _start).magnitude >= 8 * (GetComponentInParent<Canvas>()?.scaleFactor ?? 1)) _consumed = true;
            if (_scroll) _scroll.OnDrag(e);
        }
        public void OnEndDrag(PointerEventData e) { Cancel(); if (_scroll) _scroll.OnEndDrag(e); }
        public void OnCancel(BaseEventData e) => Cancel();
        public void OnSubmit(BaseEventData e) => ShortPressed?.Invoke();
        #endregion
    }
}
