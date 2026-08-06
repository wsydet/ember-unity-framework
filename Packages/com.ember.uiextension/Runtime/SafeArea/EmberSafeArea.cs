// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System;
using System.Collections;

using Sirenix.OdinInspector;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// 安全区域适配组件。
    /// 自动根据设备的安全区域（刘海屏、底部横条等）调整 RectTransform 的 padding，
    /// 确保 UI 内容不被遮挡。支持横竖屏独立配置、四边独立控制。
    /// </summary>
    [AddComponentMenu("UI/Ember/Safe Area")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    [Preserve]
    public class EmberSafeArea : UIBehaviour, ILayoutSelfController
    {
        #region 编辑器面板参数

        [FoldoutGroup("方向")]
        [SerializeField]
        [LabelText("方向模式")]
        [Tooltip("Single = 只使用默认配置；Dual = 横竖屏各自独立配置")]
        private SupportedOrientations _orientationType;

        [FoldoutGroup("默认/竖屏")]
        [SerializeField]
        [LabelText("四边模式")]
        private PerEdgeEvaluationModes _portraitOrDefaultPaddings = new PerEdgeEvaluationModes();

        [FoldoutGroup("横屏")]
        [SerializeField]
        [ShowIf("@_orientationType == SupportedOrientations.Dual")]
        [LabelText("四边模式")]
        private PerEdgeEvaluationModes _landscapePaddings = new PerEdgeEvaluationModes();

        [FoldoutGroup("设置")]
        [SerializeField]
        [Range(0f, 1f)]
        [LabelText("影响系数")]
        [Tooltip("安全区域实际应用的百分比，0 = 不应用，1 = 完全应用")]
        private float _influence = 1f;

        [FoldoutGroup("设置")]
        [SerializeField]
        [LabelText("翻转 Padding")]
        [Tooltip("启用后将左右/上下 padding 互换")]
        private bool _flipPadding;

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private readonly WaitForEndOfFrame _endOfFrame = new WaitForEndOfFrame();
        private RectTransform _rectTransform;
        private DrivenRectTransformTracker _tracker;
        private Vector4 _lastPadding;
        private bool _hasSafeArea;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        protected override void OnEnable()
        {
            base.OnEnable();
            DelayedRefresh();
        }

        protected override void OnDisable()
        {
            _tracker.Clear();
            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
            base.OnDisable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            Refresh();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (gameObject.activeInHierarchy)
                DelayedRefresh();
        }
#endif

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>是否存在有效的安全区域</summary>
        public bool HasSafeArea => _hasSafeArea;

        /// <summary>安全区域作用的 RectTransform</summary>
        public RectTransform SafeAreaRoot => RectTransform;

        /// <summary>上次计算的 Padding（left, bottom, right, top）</summary>
        public Vector4 LastPadding => _lastPadding;

        /// <summary>安全区域变化事件</summary>
        public event Action SafeAreaChanged;

        /// <summary>手动刷新安全区域</summary>
        public void Refresh()
        {
            UpdateRect();
        }

        void ILayoutController.SetLayoutHorizontal()
        {
            Refresh();
        }

        void ILayoutController.SetLayoutVertical()
        {
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private RectTransform RectTransform
        {
            get
            {
                if (!_rectTransform)
                    _rectTransform = GetComponent<RectTransform>();
                return _rectTransform;
            }
        }

        private void DelayedRefresh()
        {
            if (isActiveAndEnabled)
                StartCoroutine(DelayedRefreshRoutine());
        }

        private IEnumerator DelayedRefreshRoutine()
        {
            yield return _endOfFrame;
            Refresh();
        }

        private void UpdateRect()
        {
            if (!(enabled && gameObject.activeInHierarchy))
                return;

            var selectedOrientation = _orientationType == SupportedOrientations.Dual && IsLandscape()
                ? _landscapePaddings
                : _portraitOrDefaultPaddings;

            _tracker.Clear();
            _tracker.Add(this, RectTransform, GetDrivenProperties(selectedOrientation));

            RectTransform.anchorMin = Vector2.zero;
            RectTransform.anchorMax = Vector2.one;

            var canvasRect = GetCanvasRect();
            var relative = GetSafeAreaRelative();
            var relativePadding = new Vector4(
                relative.xMin,
                relative.yMin,
                1f - (relative.yMin + relative.height),
                1f - (relative.xMin + relative.width));

            Sanitize(ref relativePadding);

            var finalPadding = CalculatePadding(selectedOrientation, relativePadding, canvasRect);
            finalPadding *= Mathf.Clamp01(_influence);

            if (_flipPadding)
                finalPadding = new Vector4(finalPadding.w, finalPadding.z, finalPadding.y, finalPadding.x);

            Sanitize(ref finalPadding);
            ApplyPadding(canvasRect, finalPadding);

            _hasSafeArea = finalPadding.sqrMagnitude > 0.001f;
            _lastPadding = finalPadding;
            SafeAreaChanged?.Invoke();
        }

        private static DrivenTransformProperties GetDrivenProperties(PerEdgeEvaluationModes mode)
        {
            return
                (LockSide(mode.left) ? DrivenTransformProperties.AnchorMinX : DrivenTransformProperties.None) |
                (LockSide(mode.right) ? DrivenTransformProperties.AnchorMaxX : DrivenTransformProperties.None) |
                (LockSide(mode.bottom) ? DrivenTransformProperties.AnchorMinY : DrivenTransformProperties.None) |
                (LockSide(mode.top) ? DrivenTransformProperties.AnchorMaxY : DrivenTransformProperties.None) |
                (LockSide(mode.left) && LockSide(mode.right)
                    ? DrivenTransformProperties.SizeDeltaX | DrivenTransformProperties.AnchoredPositionX
                    : DrivenTransformProperties.None) |
                (LockSide(mode.top) && LockSide(mode.bottom)
                    ? DrivenTransformProperties.SizeDeltaY | DrivenTransformProperties.AnchoredPositionY
                    : DrivenTransformProperties.None);
        }

        private static bool LockSide(EdgeEvaluationMode mode)
        {
            return mode == EdgeEvaluationMode.On || mode == EdgeEvaluationMode.Balanced || mode == EdgeEvaluationMode.Off;
        }

        private static bool IsLandscape()
        {
            return Screen.width > Screen.height;
        }

        private Rect GetCanvasRect()
        {
            var canvas = GetComponentInParent<Canvas>();
            var rootCanvas = canvas ? canvas.rootCanvas : null;
            var rootRect = rootCanvas ? rootCanvas.transform as RectTransform : null;
            if (rootRect)
            {
                var rect = rootRect.rect;
                if (rect.width > 0f && rect.height > 0f)
                    return rect;

                var size = rootRect.sizeDelta;
                if (size.x > 0f && size.y > 0f)
                    return new Rect(Vector2.zero, size);
            }

            return new Rect(Vector2.zero, new Vector2(Screen.width, Screen.height));
        }

        private static Rect GetSafeAreaRelative()
        {
            var safeArea = Screen.safeArea;
            var width = Mathf.Max(Screen.width, 1);
            var height = Mathf.Max(Screen.height, 1);
            return Rect.MinMaxRect(
                safeArea.xMin / width,
                safeArea.yMin / height,
                safeArea.xMax / width,
                safeArea.yMax / height);
        }

        private static Vector4 CalculatePadding(PerEdgeEvaluationModes mode, Vector4 relativePadding, Rect canvasRect)
        {
            var padding = Vector4.zero;
            padding.x = EvaluateHorizontal(mode.left, relativePadding.x, relativePadding.w, canvasRect.width);
            padding.w = EvaluateHorizontal(mode.right, relativePadding.w, relativePadding.x, canvasRect.width);
            padding.y = EvaluateVertical(mode.bottom, relativePadding.y, relativePadding.z, canvasRect.height);
            padding.z = EvaluateVertical(mode.top, relativePadding.z, relativePadding.y, canvasRect.height);
            return padding;
        }

        private static float EvaluateHorizontal(EdgeEvaluationMode mode, float current, float opposite, float width)
        {
            switch (mode)
            {
                case EdgeEvaluationMode.On:
                    return width * current;
                case EdgeEvaluationMode.Balanced:
                    return width * Mathf.Max(current, opposite);
                default:
                    return 0f;
            }
        }

        private static float EvaluateVertical(EdgeEvaluationMode mode, float current, float opposite, float height)
        {
            switch (mode)
            {
                case EdgeEvaluationMode.On:
                    return height * current;
                case EdgeEvaluationMode.Balanced:
                    return height * Mathf.Max(current, opposite);
                default:
                    return 0f;
            }
        }

        private void ApplyPadding(Rect canvasRect, Vector4 padding)
        {
            var sizeDelta = RectTransform.sizeDelta;
            sizeDelta.x = -(padding.x + padding.w);
            sizeDelta.y = -(padding.y + padding.z);
            RectTransform.sizeDelta = sizeDelta;

            var rectWidthHeight = new Vector2(canvasRect.width + sizeDelta.x, canvasRect.height + sizeDelta.y);
            var zeroPosition = new Vector2(
                RectTransform.pivot.x * canvasRect.width,
                RectTransform.pivot.y * canvasRect.height);
            var pivotInRect = new Vector2(
                RectTransform.pivot.x * rectWidthHeight.x,
                RectTransform.pivot.y * rectWidthHeight.y);

            RectTransform.anchoredPosition3D = new Vector3(
                padding.x + pivotInRect.x - zeroPosition.x,
                padding.y + pivotInRect.y - zeroPosition.y,
                RectTransform.anchoredPosition3D.z);
        }

        private static void Sanitize(ref Vector4 value)
        {
            value.x = Sanitize(value.x);
            value.y = Sanitize(value.y);
            value.z = Sanitize(value.z);
            value.w = Sanitize(value.w);
        }

        private static float Sanitize(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
        }

        #endregion

        // --------------------------------------------------------

        #region 嵌套类型

        private enum SupportedOrientations
        {
            Single,
            Dual,
        }

        private enum EdgeEvaluationMode
        {
            On,
            Balanced,
            Off,
        }

        [Serializable]
        [Preserve]
        private class PerEdgeEvaluationModes
        {
            [LabelText("左")]
            public EdgeEvaluationMode left;
            [LabelText("下")]
            public EdgeEvaluationMode bottom;
            [LabelText("上")]
            public EdgeEvaluationMode top;
            [LabelText("右")]
            public EdgeEvaluationMode right;
        }

        #endregion
    }
}
