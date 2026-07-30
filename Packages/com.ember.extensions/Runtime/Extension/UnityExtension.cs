//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.extensions
//// Primary author: qinho
//
//using Burner.Basic;
//using Burner.Basic.Tasks;
//using Burner.Basic.LitJson;
//using Burner.Basic;
//using Burner.Basic.Tasks;
//using Burner.Basic.LitJson;
//using Burner.Extensions;
//using System.Text;
//using System.Text.RegularExpressions;
//using UnityEngine;
//
//namespace Burner.Extensions
//{
//    /// <summary>
//    /// Fundamental unity extension.
//    ///
//    /// we have a simple principle:
//    ///
//    ///    DO NOT use "if(transform != null)" for this parameter "transform"
//    ///
//    /// If a API's this decorated parameter is null or destroyed, we should let exceptions throw rather than getting through it
//    /// Let me explain by some client using code:
//    ///
//    /// Transform transform == null;
//    /// ...
//    /// transform.SetParent(xxxx); <-- there is no exception if we add "if(transform != null)" in SetParent API
//    /// transform.localposition = new Vector();  <-- NullReference Exception will throw here
//    ///
//    ///
//    /// </summary>
//    public static class UnityExtension
//    {
//        public static bool IsNull(this Object obj)
//        {
//            // "obj == null" has a little bit cost compared to "Equals(obj, null)"
//            // so that we need put "obj.Equals(null)" in the front of "obj == null"
//            return Equals(obj, null) || obj == null;
//        }
//
//        public static bool IsNotNull(this Object obj)
//        {
//            // "obj != null" has a little bit cost compared to "!Equals(obj, null)"
//            // so that we need put "!obj.Equals(null)" in the front of "obj != null"
//            return !Equals(obj, null) && obj != null;
//        }
//
//        public static void AddChild(this GameObject parent, GameObject child)
//        {
//            if(parent.IsNull())
//            {
//                throw new System.Exception("[Burner]: GameObject is null or destoryed, cannot add child");
//            }
//
//            if(child.IsNotNull())
//            {
//                child.transform.SetParent(parent.transform, false);
//            }
//        }
//
//        public static int GetAllChildrenCount(this GameObject go)
//        {
//            static void dfs(Transform trans, ref int c)
//            {
//                c += trans.childCount;
//                for(int i = 0 ; i < trans.childCount;i++)
//                {
//                    dfs(trans.GetChild(i), ref c);
//                }
//            }
//
//            var count = 0;
//            dfs(go.transform,ref count);
//
//            return count;
//        }
//
//        public static void SetParent(this GameObject child, GameObject parent)
//        {
//            child.transform.SetParent(parent.IsNotNull() ? parent.transform : null, false);
//        }
//
//        public static Transform GetChildByName(this GameObject gameObject, string name)
//        {
//            return gameObject.transform.GetChildByName(name);
//        }
//
//        [HasGC]
//        public static string GetHierachyPath(this GameObject go, bool noRoot = false)
//        {
//            if(go.IsNull())
//            {
//                throw new System.Exception("[Burner]: GameObject is null or destoryed, cannot GetHierachyPath");
//            }
//
//            var sb = new StringBuilder();
//            sb.Append("/").Append(go.name);
//
//            while(go.transform.parent.IsNotNull())
//            {
//                if(noRoot
//                   && (go.transform.parent.parent.IsNull()
//                       || go.transform.parent.parent.name == "Canvas (Environment)")) // it's fake canvas in Prefab EditingMode
//                {
//                    sb.Insert(0, ".");
//                    break;
//                }
//                go = go.transform.parent.gameObject;
//                sb.Insert(0, go.name).Insert(0, "/");
//            }
//
//            return sb.ToString();
//        }
//
//        public static void Reset(this Transform transform)
//        {
//            transform.position = Vector3.zero;
//            transform.rotation = Quaternion.identity;
//            transform.localScale = Vector3.one;
//        }
//
//        public static void ResetLocal(this Transform transform)
//        {
//            transform.localPosition = Vector3.zero;
//            transform.localRotation = Quaternion.identity;
//            transform.localScale = Vector3.one;
//        }
//
//        public static void ResetRectTransform(this RectTransform rt)
//        {
//            rt.anchoredPosition3D = Vector3.zero;
//            rt.sizeDelta = Vector2.zero;
//            rt.localScale = Vector3.one;
//        }
//
//        public static void SetXYZ(this Transform transform, float x = 0, float y = 0, float z = 0)
//        {
//            transform.position = new Vector3(x, y, z);
//        }
//
//        public static void SetLocalXYZ(this Transform transform, float x = 0, float y = 0, float z = 0)
//        {
//            transform.localPosition = new Vector3(x, y, z);
//        }
//
//        public static Transform GetChildByName(this Transform tr, string name)
//        {
//            // BFS
//            foreach(Transform child in tr)
//            {
//                if(child.name == name) return child;
//            }
//
//            foreach(Transform child in tr)
//            {
//                Transform c = GetChildByName(child, name);
//                if(c.IsNotNull()) return c;
//            }
//
//            return null;
//        }
//
//        public static void SetLocalScale(this Transform transform, float x, float y, float z)
//        {
//            transform.localScale = new Vector3(x, y, z);
//        }
//
//        public static void SetLocalScale(this Transform transform, float value)
//        {
//            transform.localScale = new Vector3(value, value, value);
//        }
//
//        public static void SetSizeDeltaWidth(this RectTransform transform, float w)
//        {
//            Vector2 size = transform.sizeDelta;
//            size.x = w;
//            transform.sizeDelta = size;
//        }
//
//        public static void SetSizeDeltaHeight(this RectTransform transform, float h)
//        {
//            Vector2 size = transform.sizeDelta;
//            size.y = h;
//            transform.sizeDelta = size;
//        }
//
//        [HasGC, Legacy("Used by Assets/Burner/Editor/Resources/lua/extensions/stringEx.lua")]
//        public static string StringReplaceEx(string value, params object[] param)
//        {
//            if(param.IsNullOrEmpty())
//                return value;
//
//            for(int i = 0; i < param.Length; i++)
//            {
//                string key = $"#v{i + 1}#";
//                value = value.Replace(key, param[i].ToString());
//            }
//            return value;
//        }
//
//        [Legacy]
//        private static bool IsCJK(this char c)
//        {
//            //return new Regex("^[\u2E80-\u9FFF]$").IsMatch(c.ToString());
//            //中日韩 unicode 区间 /u2E80-/u9FFF
//            return '\u2E80' <= c && c <= '\u9FFF';
//        }
//
//        [Legacy]
//        private static bool IsUnicodeFull(this char c)
//        {
//            //return new Regex("^[\uFF00-\uFFFF]$").IsMatch(c.ToString());
//            //全角字符 unicode 区间
//            return '\uFF00' <= c && c <= '\uFFEF';
//        }
//
//        [Legacy("Used by Assets/Burner/Editor/Resources/lua/extensions/stringEx.lua")]
//        public static int GetStringRealLength(string str)
//        {
//            int strLength = 0;
//
//            foreach(var c in str)
//            {
//                if(IsCJK(c) || IsUnicodeFull(c))
//                {
//                    //如果为中文字符或全角字符，字符串长度加2
//                    strLength += 2;
//                }
//                else
//                {
//                    //否则加1
//                    strLength += 1;
//                }
//            }
//            return strLength;
//        }
//
//        public static void UniUnload(this AssetBundle ab, bool unloadAllLoadedObjects)
//        {
//#if UNITY_WEBGL && !UNITY_EDITOR && WX_GAME
//            // https://github.com/wechat-miniprogram/minigame-unity-webgl-transform/blob/main/Design/UsingAssetBundle.md#%E4%B8%89%E6%9B%B4%E8%8A%82%E7%9C%81%E5%86%85%E5%AD%98%E7%9A%84wxassetbundle
//            WeChatWASM.AssetBundleExtensions.WXUnload(ab, unloadAllLoadedObjects);
//#else
//            ab.Unload(unloadAllLoadedObjects);
//#endif
//        }
//    }
//}
