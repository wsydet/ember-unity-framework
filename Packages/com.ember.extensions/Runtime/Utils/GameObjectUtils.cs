//// Copyright (c) 2026 Burner Games. All rights reserved.
////
//// This file is part of Burner Unity Packages.
//// Package: com.burner.extensions
//// Primary author: qinho
//
//using Burner.Basic;
//using Burner.Basic.Tasks;
//using Burner.Basic.LitJson;
//using UnityEngine;
//
//namespace Burner.Extensions
//{
//    public static class GameObjectUtils
//    {
//        /// <summary>
//        ///
//        /// </summary>
//        /// <param name="obj"></param>
//        /// <param name="name"></param>
//        /// <returns></returns>
//        public static GameObject GetGameObjectByName(GameObject obj, string name)
//        {
//            Transform root = obj.transform;
//
//            return GetGameObjectByNameSub(root, name);
//        }
//
//        static GameObject GetGameObjectByNameSub(Transform t, string name)
//        {
//            if (t.name == name)
//                return t.gameObject;
//            int cnt = t.childCount;
//            for (int i = 0; i < cnt; i++)
//            {
//                var res = GetGameObjectByNameSub(t.GetChild(i), name);
//                if (res)
//                    return res;
//            }
//            return null;
//        }
//
//        /// <summary>
//        ///
//        /// </summary>
//        /// <param name="go"></param>
//        /// <param name="layer"></param>
//        /// <param name="enforceSet"></param>
//        static public void SetLayer(GameObject go, int layer)
//        {
//            SetLayerSub(go.transform, layer);
//        }
//
//        static void SetLayerSub(Transform tran, int layer)
//        {
//            impGameObjectlayer(tran.gameObject, layer);
//            int cnt = tran.childCount;
//            for (int i = 0; i < cnt; i++)
//            {
//                SetLayerSub(tran.GetChild(i), layer);
//            }
//        }
//
//        private static void impGameObjectlayer(GameObject obj, int layer)
//        {
//            obj.layer = layer;
//        }
//
//        /// <summary>
//        ///
//        /// </summary>
//        /// <param name="obj"></param>
//        /// <param name="name"></param>
//        /// <returns></returns>
//        public static Transform GetBindPoint(GameObject obj, string name)
//        {
//            if (name == "self")
//            {
//                return obj.transform;
//            }
//
//            var res = GetGameObjectByName(obj, name);
//            if (res)
//                return res.transform;
//            return null;
//        }
//    }
//}
