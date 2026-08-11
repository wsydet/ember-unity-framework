////// Copyright (c) 2026 Burner Games. All rights reserved.
//////
////// This file is part of Burner Unity Packages.
////// Package: com.burner.uiextension
////// Primary author: qinho
////
////using UnityEngine;
////using UnityEditor;
////using System.IO;
////
////namespace Burner.UIExtension
////{
////	[CustomEditor(typeof(ShowText))]
////	public class ShowTextInspector : UnityEditor.Editor
////	{
////		private ShowText showText;
////		public override void OnInspectorGUI()
////		{
////			base.OnInspectorGUI();
////
////			showText = target as ShowText;
////			if (GUILayout.Button("test play"))
////			{
////				string st = showText.text;
////				showText.Reset();
////				showText.text = st;
////			}
////			if (GUILayout.Button("Create Child node"))
////			{
////				Create();
////			}
////			if (GUILayout.Button("Create Text Source Assets"))
////			{
////				CreateTextSource();
////			}
////		}
////
////		private void Create()
////		{
////			if (showText.transform.childCount != 0)
////			{
////				for (int i = showText.transform.childCount - 1; i >= 0; i--)
////				{
////					GameObject.DestroyImmediate(showText.transform.GetChild(i).gameObject);
////				}
////			}
////			for (int i = 0; i < showText.maxCharCount; i++)
////			{
////				var go = new GameObject("" + i);
////				go.transform.SetParent(showText.transform, false);
////				var cgo = new GameObject("sprite", typeof(SpriteRenderer));
////				cgo.transform.SetParent(go.transform, false);
////			}
////			showText.Clear();
////		}
////
////		void CreateTextSource()
////		{
////			ScriptableObject bullet = ScriptableObject.CreateInstance<ShowTextSource>();
////			if (!bullet)
////			{
////				Debug.LogWarning("Bullet not found");
////				return;
////			}
////			string path = Application.dataPath + "/_debug";
////			if (!Directory.Exists(path))
////			{
////				Directory.CreateDirectory(path);
////
////			}
////			path = string.Format("Assets/_debug/{0}.asset", (typeof(ShowTextSource).ToString()));
////			AssetDatabase.CreateAsset(bullet, path);
////			showText.showTextSource = bullet as ShowTextSource;
////		}
////	}
////}
