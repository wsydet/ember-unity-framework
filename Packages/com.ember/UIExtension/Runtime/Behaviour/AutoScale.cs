//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using UnityEngine;
//
//namespace Burner
//{
//	[DisallowMultipleComponent]
//	public class AutoScale : MonoBehaviour
//	{
//		public Camera m_Camera = null;
//
//		public void SetScale()
//		{
//			if (m_Camera == null) return;
//			if (!m_Camera.orthographic)
//			{
//				Debug.LogWarning("The Camera is not orthographic, this script will not take effect!", gameObject);
//				return;
//			}
//			float scaleY = m_Camera.orthographicSize * 2.0f;
//			float scaleX = scaleY * m_Camera.aspect;
//			transform.localScale = new Vector3(scaleX, scaleY, transform.localScale.z);
//		}
//
//		public void Awake() => SetScale();
//	}
//}
