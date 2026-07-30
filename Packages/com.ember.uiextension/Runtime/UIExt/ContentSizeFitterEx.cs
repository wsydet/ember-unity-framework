//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using UnityEngine.EventSystems;
//
//namespace UnityEngine.UI
//{
//    [ExecuteAlways]
//    [RequireComponent(typeof(RectTransform))]
//    /// <summary>
//    /// Resizes a RectTransform to fit the size of its content.
//    /// </summary>
//    /// <remarks>
//    /// The ContentSizeFitter can be used on GameObjects that have one or more ILayoutElement components, such as Text, Image, HorizontalLayoutGroup, VerticalLayoutGroup, and GridLayoutGroup.
//    /// </remarks>
//    public class ContentSizeFitterEx : UIBehaviour, ILayoutSelfController
//    {
//        [SerializeField] protected ContentSizeFitter.FitMode m_HorizontalFit = ContentSizeFitter.FitMode.Unconstrained;
//        [SerializeField] float m_maxWidth;
//
//        /// <summary>
//        /// The fit mode to use to determine the width.
//        /// </summary>
//        public ContentSizeFitter.FitMode horizontalFit
//        {
//            get { return m_HorizontalFit; }
//            set
//            {
//                if(m_HorizontalFit != value)
//                {
//                    m_HorizontalFit = value;
//                    SetDirty();
//                }
//            }
//        }
//
//        [SerializeField] protected ContentSizeFitter.FitMode m_VerticalFit = ContentSizeFitter.FitMode.Unconstrained;
//        [SerializeField] float m_maxHeight;
//        /// <summary>
//        /// The fit mode to use to determine the height.
//        /// </summary>
//        public ContentSizeFitter.FitMode verticalFit
//        {
//            get { return m_VerticalFit; }
//            set
//            {
//                if(m_VerticalFit != value)
//                {
//                    m_VerticalFit = value;
//                    SetDirty();
//                }
//            }
//        }
//
//        [System.NonSerialized] private RectTransform m_Rect;
//        private RectTransform rectTransform
//        {
//            get
//            {
//                if (m_Rect == null)
//                    m_Rect = GetComponent<RectTransform>();
//                return m_Rect;
//            }
//        }
//
//        // field is never assigned warning
//#pragma warning disable 649
//        private DrivenRectTransformTracker m_Tracker;
//#pragma warning restore 649
//
//        protected ContentSizeFitterEx()
//        { }
//
//        protected override void OnEnable()
//        {
//            base.OnEnable();
//            SetDirty();
//        }
//
//        protected override void OnDisable()
//        {
//            m_Tracker.Clear();
//            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
//            base.OnDisable();
//        }
//        protected override void Awake()
//        {
//            LayoutRebuilder.RegisterLayoutController(rectTransform, this);
//        }
//
//        protected override void OnDestroy()
//        {
//            LayoutRebuilder.UnregisterLayoutController(rectTransform, this);
//        }
//        protected override void OnRectTransformDimensionsChange()
//        {
//            SetDirty();
//        }
//
//        private void HandleSelfFittingAlongAxis(int axis)
//        {
//            ContentSizeFitter.FitMode fitting = (axis == 0 ? horizontalFit : verticalFit);
//            if (fitting == ContentSizeFitter.FitMode.Unconstrained)
//            {
//                // Keep a reference to the tracked transform, but don't control its properties:
//                m_Tracker.Add(this, rectTransform, DrivenTransformProperties.None);
//                return;
//            }
//
//            m_Tracker.Add(this, rectTransform, (axis == 0 ? DrivenTransformProperties.SizeDeltaX : DrivenTransformProperties.SizeDeltaY));
//
//            // Set size to min or preferred size
//            if (fitting == ContentSizeFitter.FitMode.MinSize)
//                rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, LayoutUtility.GetMinSize(m_Rect, axis));
//            else
//            {
//                rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, GetClampedValueByAxis(LayoutUtility.GetPreferredSize(m_Rect, axis), axis));
//            }
//        }
//
//        float GetClampedValueByAxis(float value, int axis)
//        {
//            if(axis == 0)
//            {
//                if (m_maxWidth > 0)
//                    return Mathf.Min(m_maxWidth, value);
//                else
//                    return value;
//            }
//            else
//            {
//                if (m_maxHeight > 0)
//                    return Mathf.Min(m_maxHeight, value);
//                else
//                    return value;
//            }
//        }
//
//        /// <summary>
//        /// Calculate and apply the horizontal component of the size to the RectTransform
//        /// </summary>
//        public virtual void SetLayoutHorizontal()
//        {
//            m_Tracker.Clear();
//            HandleSelfFittingAlongAxis(0);
//        }
//
//        /// <summary>
//        /// Calculate and apply the vertical component of the size to the RectTransform
//        /// </summary>
//        public virtual void SetLayoutVertical()
//        {
//            HandleSelfFittingAlongAxis(1);
//        }
//
//        protected void SetDirty()
//        {
//            if (!IsActive())
//                return;
//
//            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
//        }
//
//#if UNITY_EDITOR
//        protected override void OnValidate()
//        {
//            SetDirty();
//        }
//
//#endif
//    }
//}
