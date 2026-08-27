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
////	[CustomEditor(typeof(Plaque))]
////	public class PlaqueInspector : UnityEditor.Editor
////	{
////		public override void OnInspectorGUI()
////		{
////			var plaque = target as Plaque;
////
////			base.OnInspectorGUI();
////
////			EditorGUILayout.BeginHorizontal();
////			GUILayout.Label("Sprite:");
////			GUI.SetNextControlName("PackedTextureSpritePopup");
////			var rect = EditorGUILayout.GetControlRect(GUILayout.MinWidth(50));
////			if (EditorGUI.DropdownButton(rect, new GUIContent(plaque.SpriteName), FocusType.Passive))
////			{
////				GUI.FocusControl("PackedTextureSpritePopup");
////				var sourceData = plaque.sourceData;
////				if (sourceData != null)
////				{
////					var spriteNames = sourceData.ListSpriteName;
////					var menu = new GenericMenu();
////
////					foreach (var spriteName in spriteNames)
////					{
////						var targetName = spriteName;
////						menu.AddItem(new GUIContent(targetName), false, () =>
////					   {
////						   plaque.SpriteName = targetName;
////					   });
////					}
////					menu.DropDown(rect);
////				}
////			}
////			EditorGUILayout.EndHorizontal();
////
////		}
////	}
////}
