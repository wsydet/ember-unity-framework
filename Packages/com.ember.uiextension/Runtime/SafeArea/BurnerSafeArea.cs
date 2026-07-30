//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System;
//using System.Collections;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.Scripting;
//using UnityEngine.UI;
//
//namespace Burner.UIExtension
//{
//    [DisallowMultipleComponent]
//    [RequireComponent(typeof(RectTransform))]
//    [ExecuteAlways]
//    [Preserve]
//    public class BurnerSafeArea : UIBehaviour, ILayoutSelfController
//    {
//        private readonly WaitForEndOfFrame _endOfFrame = new WaitForEndOfFrame();
//        private RectTransform _rectTransform;
//        private DrivenRectTransformTracker _tracker;
//        private Vector4 _lastPadding;
//        private bool _hasSafeArea;
//
//        [SerializeField]
//        private SupportedOrientations orientationType;
//
//        [SerializeField]
//        private PerEdgeEvaluationModes portraitOrDefaultPaddings = new PerEdgeEvaluationModes();
//
//        [SerializeField]
//        private PerEdgeEvaluationModes landscapePaddings = new PerEdgeEvaluationModes();
//
//        [SerializeField]
//        [Range(0f, 1f)]
//        private float influence = 1f;
//
//        [SerializeField]
//        private bool flipPadding;
//
//        public bool HasSafeArea => _hasSafeArea;
//        public RectTransform SafeAreaRoot => RectTransform;
//        public Vector4 LastPadding => _lastPadding;
//        public event Action SafeAreaChanged;
//
//        private RectTransform RectTransform
//        {
//            get
//            {
//                if (!_rectTransform)
//                {
//                    _rectTransform = GetComponent<RectTransform>();
//                }
//
//                return _rectTransform;
//            }
//        }
//
//        public void Refresh()
//        {
//            UpdateRect();
//        }
//
//        protected override void OnEnable()
//        {
//            base.OnEnable();
//            DelayedRefresh();
//        }
//
//        protected override void OnDisable()
//        {
//            _tracker.Clear();
//            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
//            base.OnDisable();
//        }
//
//        protected override void OnRectTransformDimensionsChange()
//        {
//            Refresh();
//        }
//
//        void ILayoutController.SetLayoutHorizontal()
//        {
//            Refresh();
//        }
//
//        void ILayoutController.SetLayoutVertical()
//        {
//        }
//
//#if UNITY_EDITOR
//        protected override void OnValidate()
//        {
//            if (gameObject.activeInHierarchy)
//            {
//                DelayedRefresh();
//            }
//        }
//#endif
//
//        private void DelayedRefresh()
//        {
//            if (isActiveAndEnabled)
//            {
//                StartCoroutine(DelayedRefreshRoutine());
//            }
//        }
//
//        private IEnumerator DelayedRefreshRoutine()
//        {
//            yield return _endOfFrame;
//            Refresh();
//        }
//
//        private void UpdateRect()
//        {
//            if (!(enabled && gameObject.activeInHierarchy))
//            {
//                return;
//            }
//
//            var selectedOrientation = orientationType == SupportedOrientations.Dual && IsLandscape()
//                ? landscapePaddings
//                : portraitOrDefaultPaddings;
//
//            _tracker.Clear();
//            _tracker.Add(
//                this,
//                RectTransform,
//                GetDrivenProperties(selectedOrientation));
//
//            RectTransform.anchorMin = Vector2.zero;
//            RectTransform.anchorMax = Vector2.one;
//
//            var canvasRect = GetCanvasRect();
//            var relative = GetSafeAreaRelative();
//            var relativePadding = new Vector4(
//                relative.xMin,
//                relative.yMin,
//                1f - (relative.yMin + relative.height),
//                1f - (relative.xMin + relative.width));
//
//            Sanitize(ref relativePadding);
//
//            var finalPadding = CalculatePadding(selectedOrientation, relativePadding, canvasRect);
//            finalPadding *= Mathf.Clamp01(influence);
//            if (flipPadding)
//            {
//                finalPadding = new Vector4(finalPadding.w, finalPadding.z, finalPadding.y, finalPadding.x);
//            }
//
//            Sanitize(ref finalPadding);
//            ApplyPadding(canvasRect, finalPadding);
//
//            _hasSafeArea = finalPadding.sqrMagnitude > 0.001f;
//            _lastPadding = finalPadding;
//            SafeAreaChanged?.Invoke();
//        }
//
//        private DrivenTransformProperties GetDrivenProperties(PerEdgeEvaluationModes mode)
//        {
//            return
//                (LockSide(mode.left) ? DrivenTransformProperties.AnchorMinX : DrivenTransformProperties.None) |
//                (LockSide(mode.right) ? DrivenTransformProperties.AnchorMaxX : DrivenTransformProperties.None) |
//                (LockSide(mode.bottom) ? DrivenTransformProperties.AnchorMinY : DrivenTransformProperties.None) |
//                (LockSide(mode.top) ? DrivenTransformProperties.AnchorMaxY : DrivenTransformProperties.None) |
//                (LockSide(mode.left) && LockSide(mode.right)
//                    ? DrivenTransformProperties.SizeDeltaX | DrivenTransformProperties.AnchoredPositionX
//                    : DrivenTransformProperties.None) |
//                (LockSide(mode.top) && LockSide(mode.bottom)
//                    ? DrivenTransformProperties.SizeDeltaY | DrivenTransformProperties.AnchoredPositionY
//                    : DrivenTransformProperties.None);
//        }
//
//        private static bool LockSide(EdgeEvaluationMode mode)
//        {
//            return mode == EdgeEvaluationMode.On || mode == EdgeEvaluationMode.Balanced || mode == EdgeEvaluationMode.Off;
//        }
//
//        private static bool IsLandscape()
//        {
//            return Screen.width > Screen.height;
//        }
//
//        private Rect GetCanvasRect()
//        {
//            var canvas = GetComponentInParent<Canvas>();
//            var rootCanvas = canvas ? canvas.rootCanvas : null;
//            var rootRect = rootCanvas ? rootCanvas.transform as RectTransform : null;
//            if (rootRect)
//            {
//                var rect = rootRect.rect;
//                if (rect.width > 0f && rect.height > 0f)
//                {
//                    return rect;
//                }
//
//                var size = rootRect.sizeDelta;
//                if (size.x > 0f && size.y > 0f)
//                {
//                    return new Rect(Vector2.zero, size);
//                }
//            }
//
//            return new Rect(Vector2.zero, new Vector2(Screen.width, Screen.height));
//        }
//
//        private static Rect GetSafeAreaRelative()
//        {
//            var safeArea = Screen.safeArea;
//            var width = Mathf.Max(Screen.width, 1);
//            var height = Mathf.Max(Screen.height, 1);
//            return Rect.MinMaxRect(
//                safeArea.xMin / width,
//                safeArea.yMin / height,
//                safeArea.xMax / width,
//                safeArea.yMax / height);
//        }
//
//        private static Vector4 CalculatePadding(PerEdgeEvaluationModes mode, Vector4 relativePadding, Rect canvasRect)
//        {
//            var padding = Vector4.zero;
//            padding.x = EvaluateHorizontal(mode.left, relativePadding.x, relativePadding.w, canvasRect.width);
//            padding.w = EvaluateHorizontal(mode.right, relativePadding.w, relativePadding.x, canvasRect.width);
//            padding.y = EvaluateVertical(mode.bottom, relativePadding.y, relativePadding.z, canvasRect.height);
//            padding.z = EvaluateVertical(mode.top, relativePadding.z, relativePadding.y, canvasRect.height);
//            return padding;
//        }
//
//        private static float EvaluateHorizontal(EdgeEvaluationMode mode, float current, float opposite, float width)
//        {
//            switch (mode)
//            {
//                case EdgeEvaluationMode.On:
//                    return width * current;
//                case EdgeEvaluationMode.Balanced:
//                    return width * Mathf.Max(current, opposite);
//                default:
//                    return 0f;
//            }
//        }
//
//        private static float EvaluateVertical(EdgeEvaluationMode mode, float current, float opposite, float height)
//        {
//            switch (mode)
//            {
//                case EdgeEvaluationMode.On:
//                    return height * current;
//                case EdgeEvaluationMode.Balanced:
//                    return height * Mathf.Max(current, opposite);
//                default:
//                    return 0f;
//            }
//        }
//
//        private void ApplyPadding(Rect canvasRect, Vector4 padding)
//        {
//            var sizeDelta = RectTransform.sizeDelta;
//            sizeDelta.x = -(padding.x + padding.w);
//            sizeDelta.y = -(padding.y + padding.z);
//            RectTransform.sizeDelta = sizeDelta;
//
//            var rectWidthHeight = new Vector2(canvasRect.width + sizeDelta.x, canvasRect.height + sizeDelta.y);
//            var zeroPosition = new Vector2(
//                RectTransform.pivot.x * canvasRect.width,
//                RectTransform.pivot.y * canvasRect.height);
//            var pivotInRect = new Vector2(
//                RectTransform.pivot.x * rectWidthHeight.x,
//                RectTransform.pivot.y * rectWidthHeight.y);
//
//            RectTransform.anchoredPosition3D = new Vector3(
//                padding.x + pivotInRect.x - zeroPosition.x,
//                padding.y + pivotInRect.y - zeroPosition.y,
//                RectTransform.anchoredPosition3D.z);
//        }
//
//        private static void Sanitize(ref Vector4 value)
//        {
//            value.x = Sanitize(value.x);
//            value.y = Sanitize(value.y);
//            value.z = Sanitize(value.z);
//            value.w = Sanitize(value.w);
//        }
//
//        private static float Sanitize(float value)
//        {
//            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
//        }
//
//        private enum SupportedOrientations
//        {
//            Single,
//            Dual,
//        }
//
//        private enum EdgeEvaluationMode
//        {
//            On,
//            Balanced,
//            Off,
//        }
//
//        [Serializable]
//        [Preserve]
//        private class PerEdgeEvaluationModes
//        {
//            public EdgeEvaluationMode left;
//            public EdgeEvaluationMode bottom;
//            public EdgeEvaluationMode top;
//            public EdgeEvaluationMode right;
//        }
//    }
//}
