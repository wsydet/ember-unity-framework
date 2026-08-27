//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.uiextension
//// Primary author: qinho
//
//using System.Collections.Generic;
//using UnityEngine;
//
//namespace Burner.UIExtension
//{
//	[ExecuteAlways]
//	public class ShowTextSource : ScriptableObject
//	{
//		[System.Serializable]
//		public class CharSource
//		{
//			public char code;
//			public Sprite sprite;
//		}
//
//		public string fontName;
//		public List<CharSource> chars;
//
//
//		public Sprite GetCharSprite(char c)
//		{
//			return chars.Find((cs) => { return cs.code == c; })?.sprite;
//		}
//
//		public CharSource GetCharSourceWithSpriteName(string name, out string language)
//		{
//			foreach (var cs in chars)
//			{
//				if (cs.sprite == null) continue;
//
//				if (cs.sprite.name == name)
//				{
//					language = "";
//					return cs;
//				}
//
//				if (!name.StartsWith(cs.sprite.name)) continue;
//
//				foreach (var lan in ShowTextInstanceSource.languageMap)
//				{
//					//检查多语言后缀是否满足
//					if ((cs.sprite.name + "_" + lan.Value) == name)
//					{
//						language = "_" + lan.Value;
//						return cs;
//					}
//				}
//			}
//			language = "";
//			return null;
//		}
//	}
//}
