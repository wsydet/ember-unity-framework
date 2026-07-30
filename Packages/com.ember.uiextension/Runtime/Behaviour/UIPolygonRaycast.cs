//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using UnityEngine;
//using UnityEngine.UI;
//
//#if UNITY_EDITOR
//using UnityEditor;
//#endif
//
//namespace Burner.UIExtension
//{
//    [AddComponentMenu("BurnerUI/UIPolygonRaycast")]
//    [RequireComponent(typeof(PolygonCollider2D), typeof(CanvasRenderer))]
//    public class UIPolygonRaycast : Graphic, ICanvasRaycastFilter
//    {
//        public PolygonCollider2D m_Collider;
//
//        protected override void Awake()
//        {
//            base.Awake();
//            if (m_Collider == null) m_Collider = GetComponent<PolygonCollider2D>();
//        }
//
//        public override void Rebuild(CanvasUpdate update) { }
//
//        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
//        {
//            return m_Collider ? m_Collider.OverlapPoint(eventCamera.ScreenToWorldPoint(screenPoint)) : false;
//        }
//
//#if UNITY_EDITOR
//        protected override void Reset()
//        {
//            base.Reset();
//            Awake();
//            if (m_Collider == null) return;
//
//            transform.localPosition = Vector3.zero;
//
//            var w = rectTransform.sizeDelta.x * 0.5f + 0.1f;
//            var h = rectTransform.sizeDelta.y * 0.5f + 0.1f;
//            m_Collider.points = new Vector2[]
//            {
//                new Vector2(-w, -h),
//                new Vector2(w, -h),
//                new Vector2(w, h),
//                new Vector2(-w, h)
//            };
//        }
//
//		[MenuItem("GameObject/Burner UI/PolygonRaycast")]
//		public static void CreatePolygonRaycast()
//		{
//			var btnObj = Selection.activeGameObject;
//			if (btnObj == null) return;
//
//			if (btnObj.GetComponent<Button>() == null)
//			{
//				Debug.LogError("Selected GameObject is not a button", btnObj);
//				return;
//			}
//
//			var graphics = btnObj.GetComponentsInChildren<Graphic>();
//			foreach (var g in graphics) g.raycastTarget = false;
//
//			var polygon = new GameObject("PolygonRaycast", typeof(PolygonCollider2D), typeof(CanvasRenderer), typeof(UIPolygonRaycast));
//			polygon.transform.SetParent(btnObj.transform, true);
//			polygon.transform.SetAsLastSibling();
//
//			Selection.activeGameObject = polygon;
//		}
//#endif
//    }
//}
