using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Ember.UIExtension
{
    /// <summary>单指/鼠标拖动入口；位置按安全区左右边和归一化高度保存。</summary>
    public sealed class EUIFloatingEntry : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IEndDragHandler, ICancelHandler
    {
        #region 编辑器面板参数
        [SerializeField] private string _positionKey = "Ember.GM.Entry";
        [SerializeField] private bool _reducedMotion;
        #endregion
        #region 内部参数
        private RectTransform _rect, _parent;
        private int _pointer = int.MinValue;
        private Vector2 _startScreen, _startPosition, _snapFrom, _snapTo;
        private bool _dragged, _snapping, _right = true;
        private float _y = .72f, _snapTime;
        private Rect _safeArea;
        private Vector2Int _screen;
        private Camera _camera;
        private Canvas _canvas;
        private Vector2 _parentSize;
        private float _pressedAt;
        private bool _longPressed;
        public event Action Activated;
        public event Action LongPressed;
        public event Action DragStarted;
        public event Action PositionChanged;
        /// <summary>默认关闭，既有使用者保持原激活行为。</summary>
        public bool LongPressEnabled { get; set; }
        public Func<Vector2, Vector2> DockConstraint { get; set; }
        public bool WasDragged => _dragged;
        #endregion
        #region 生命周期
        private void OnEnable()
        {
            _rect = (RectTransform)transform; _parent = _rect.parent as RectTransform;
            _canvas = GetComponentInParent<Canvas>(); _camera = _canvas ? _canvas.worldCamera : null;
            _right = PlayerPrefs.GetInt(_positionKey + ".Right", 1) != 0;
            _y = Mathf.Clamp01(PlayerPrefs.GetFloat(_positionKey + ".Y", .72f));
            _safeArea = default; Reposition();
        }
        private void OnDisable() { _pointer = int.MinValue; _snapping = false; }
        private void Update()
        {
            if (_pointer != int.MinValue && !_dragged && !_longPressed && LongPressEnabled && Time.unscaledTime - _pressedAt >= .55f)
            { _longPressed = true; LongPressed?.Invoke(); }
            if (_pointer == int.MinValue && (_safeArea != Screen.safeArea || _screen.x != Screen.width || _screen.y != Screen.height ||
                (_parent && _parent.rect.size != _parentSize) || (_canvas && _canvas.worldCamera != _camera))) Reposition();
            if (!_snapping) return;
            _snapTime += Time.unscaledDeltaTime / .18f;
            _rect.anchoredPosition = Vector2.Lerp(_snapFrom, _snapTo, 1 - Mathf.Pow(1 - Mathf.Clamp01(_snapTime), 3));
            PositionChanged?.Invoke();
            if (_snapTime >= 1) _snapping = false;
        }
        #endregion
        #region 内部方法
        private Rect Bounds()
        {
            if (!_parent) return default;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_parent, Screen.safeArea.min, _camera, out var min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_parent, Screen.safeArea.max, _camera, out var max);
            var margin = _rect.rect.size * .5f + Vector2.one * 12;
            min += margin; max -= margin;
            max = Vector2.Max(min, max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        private void Reposition()
        {
            _safeArea = Screen.safeArea; _screen = new Vector2Int(Screen.width, Screen.height);
            if (_parent) _parentSize = _parent.rect.size;
            if (_canvas) _camera = _canvas.worldCamera;
            var bounds = Bounds(); _rect.anchoredPosition = new Vector2(_right ? bounds.xMax : bounds.xMin, Mathf.Lerp(bounds.yMin, bounds.yMax, _y));
            _snapping = false;
            PositionChanged?.Invoke();
        }
        private void Snap()
        {
            var bounds = Bounds(); var p = _rect.anchoredPosition;
            _right = p.x >= bounds.center.x;
            _y = Mathf.InverseLerp(bounds.yMin, bounds.yMax, Mathf.Clamp(p.y, bounds.yMin, bounds.yMax));
            _snapFrom = p; _snapTo = new Vector2(_right ? bounds.xMax : bounds.xMin, Mathf.Lerp(bounds.yMin, bounds.yMax, _y));
            if (DockConstraint != null)
            {
                var constrained = DockConstraint(_snapTo);
                _snapTo.y = Mathf.Clamp(constrained.y, bounds.yMin, bounds.yMax);
                _y = Mathf.InverseLerp(bounds.yMin, bounds.yMax, _snapTo.y);
            }
            _snapTime = 0; _snapping = !_reducedMotion;
            if (_reducedMotion) _rect.anchoredPosition = _snapTo;
            PlayerPrefs.SetInt(_positionKey + ".Right", _right ? 1 : 0); PlayerPrefs.SetFloat(_positionKey + ".Y", _y);
        }
        #endregion
        #region 外部方法
        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            if (_pointer != int.MinValue) { if(LongPressEnabled&&e.pointerId!=_pointer)OnCancel(null);return; }
            _pointer = e.pointerId; _startScreen = e.position; _startPosition = _rect.anchoredPosition;
            _dragged = false; _snapping = false; _camera = e.pressEventCamera;
            _pressedAt = Time.unscaledTime; _longPressed = false;
        }
        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _pointer) return;
            if (_longPressed) return;
            var canvas = GetComponentInParent<Canvas>();
            if (!_dragged && (e.position - _startScreen).magnitude < 8 * (canvas ? canvas.scaleFactor : 1)) return;
            if (!_dragged) { _dragged = true; DragStarted?.Invoke(); }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_parent, _startScreen, _camera, out var start);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_parent, e.position, _camera, out var end);
            var bounds = Bounds(); var p = _startPosition + end - start;
            _rect.anchoredPosition = new Vector2(Mathf.Clamp(p.x, bounds.xMin, bounds.xMax), Mathf.Clamp(p.y, bounds.yMin, bounds.yMax));
            PositionChanged?.Invoke();
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != _pointer) return;
            _pointer = int.MinValue;
            if (_dragged) Snap(); else if (!_longPressed) Activated?.Invoke();
        }
        public void OnEndDrag(PointerEventData e)
        {
            if (e.pointerId != _pointer) return;
            _pointer = int.MinValue;
            if (_dragged) Snap();
        }
        public void OnCancel(BaseEventData e) { _pointer = int.MinValue; _longPressed = false; }
        private void OnApplicationFocus(bool focused) { if (!focused) OnCancel(null); }
        #endregion
    }
}
