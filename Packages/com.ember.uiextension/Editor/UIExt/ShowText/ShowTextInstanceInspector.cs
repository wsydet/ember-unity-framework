////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using UnityEngine;
////using UnityEditor;
////
////namespace Burner.UIExtension
////{
////	[CustomEditor(typeof(ShowTextInstance))]
////	public class ShowTextInstanceInspector : UnityEditor.Editor
////	{
////		private ShowTextInstance showText;
////		public override void OnInspectorGUI()
////		{
////			base.OnInspectorGUI();
////
////			showText = target as ShowTextInstance;
////			if (GUILayout.Button("test play"))
////			{
////				showText.Play(() =>
////				{
////					// Debug.Log("finish");
////				});
////			}
////			if (GUILayout.Button("test stop"))
////			{
////				showText.Stop(true);
////			}
////		}
////	}
////}
