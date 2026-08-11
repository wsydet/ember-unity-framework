////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using UnityEditor;
////using UnityEngine;
////
////namespace Burner.UIExtension
////{
////	[CustomEditor(typeof(PackedTextureScript))]
////	public class PackedTextureScriptInspector : UnityEditor.Editor
////	{
////		private PackedTextureScript _script;
////
////		public override void OnInspectorGUI()
////		{
////			_script = target as PackedTextureScript;
////
////			base.OnInspectorGUI();
////
////			EditorGUILayout.BeginHorizontal();
////			var size = _script.Size;
////			size.x = EditorGUILayout.FloatField(new GUIContent("width"), size.x);
////			size.y = EditorGUILayout.FloatField(new GUIContent("height"), size.y);
////			_script.Size = size;
////			EditorGUILayout.EndHorizontal();
////
////			EditorGUILayout.BeginHorizontal();
////			var anchor = _script.Anchor;
////			anchor.x = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("pivot x"), anchor.x), 0.0f, 1.0f);
////			anchor.y = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("pivot y"), anchor.y), 0.0f, 1.0f);
////			_script.Anchor = anchor;
////			EditorGUILayout.EndHorizontal();
////
////			_script.FillVertical = GUILayout.Toggle(_script.FillVertical, new GUIContent("Fill In Vertical"));
////			_script.FillAmount = EditorGUILayout.Slider(new GUIContent("Fill Amount"), _script.FillAmount, 0, 1.0f);
////			_script.TrisectionEnabled = GUILayout.Toggle(_script.TrisectionEnabled, new GUIContent("Trisection Enabled"));
////			if (_script.TrisectionEnabled)
////			{
////				_script.TrisectionValue = EditorGUILayout.Vector2Field("Trisection Value", _script.TrisectionValue);
////			}
////
////			EditorGUILayout.BeginHorizontal();
////			GUILayout.Label("Sprite:");
////			GUI.SetNextControlName("PackedTextureSpritePopup");
////			var rect = EditorGUILayout.GetControlRect(GUILayout.MinWidth(50));
////			if (EditorGUI.DropdownButton(rect, new GUIContent(_script.SpriteName), FocusType.Passive))
////			{
////				GUI.FocusControl("PackedTextureSpritePopup");
////				var sourceData = _script.sourceData;
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
////						   _script.SpriteName = targetName;
////					   });
////					}
////					menu.DropDown(rect);
////				}
////			}
////			EditorGUILayout.EndHorizontal();
////
////			if (GUILayout.Button(new GUIContent("Refresh")))
////			{
////				_script.EditorRefresh();
////			}
////
////			if (_script.Dirty)
////			{
////				_script.Dirty = false;
////				EditorUtility.SetDirty(_script);
////			}
////		}
////	}
////}
