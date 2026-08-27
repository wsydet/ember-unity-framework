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
//	public class ShowTextInstanceSource : ScriptableObject
//	{
//		public static Dictionary<SystemLanguage, string> languageMap = new Dictionary<SystemLanguage, string>(){
//			{SystemLanguage.Chinese,"ch"},
//			{SystemLanguage.ChineseSimplified,"ch"},
//			{SystemLanguage.ChineseTraditional,"cht"},
//			{SystemLanguage.Japanese,"ja"},
//			{SystemLanguage.Korean,"kr"},
//			{SystemLanguage.French,"fr"},
//			{SystemLanguage.German,"de"},
//			{SystemLanguage.Spanish,"es"},
//			{SystemLanguage.Portuguese,"pt"},
//			{SystemLanguage.Russian,"ru"},
//			{SystemLanguage.English,"en"}
//		};
//
//		[System.Serializable]
//		public class CharElement
//		{
//			public string name;
//			public int index;
//			public int width;
//			public int height;
//			public float Width
//			{
//				get => width * 0.01f;
//			}
//			public float WidthScale
//			{
//				get => (width * 1.0f / height);
//			}
//		}
//
//		public List<CharElement> chars;
//		private Dictionary<string, CharElement> elementMap;
//
//		public CharElement GetCharIndex(string c, SystemLanguage language)
//		{
//			if (elementMap == null)
//			{
//				BuildMap();
//			}
//			//查找多语言图片 如果存在就返回多语言
//			if (languageMap.TryGetValue(language, out string key))
//			{
//				if (elementMap.TryGetValue(c + "_" + key, out var value1))
//				{
//					return value1;
//				}
//			}
//			if (elementMap.TryGetValue(c, out var value))
//			{
//				return value;
//			}
//			return null;
//		}
//		
//		private void BuildMap()
//		{
//			elementMap = new Dictionary<string, CharElement>();
//			foreach (var c in chars)
//			{
//				if (!elementMap.ContainsKey(c.name))
//				{
//					elementMap.Add(c.name, c);
//				}
//			}
//		}
//	}
//}
