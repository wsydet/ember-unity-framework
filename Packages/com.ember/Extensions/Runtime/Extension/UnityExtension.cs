// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember
// Migrated from Burner extensions with cleanup: removed legacy methods, WX game code, CJK utilities.

using System.Text;
using Ember.Basic;
using UnityEngine;

namespace Ember.Extensions
{
    /// <summary>
    /// Fundamental Unity extension methods for Transform, GameObject, and RectTransform.
    ///
    /// Design principle: extension methods on Unity Objects do NOT null-check "this".
    /// If "this" is null or destroyed, let the NullReferenceException propagate naturally —
    /// silently swallowing it only hides bugs and shifts the crash downstream.
    /// </summary>
    public static class UnityExtension
    {
        #region 外部方法

        /// <summary>
        /// Checks whether a Unity Object is truly null (either destroyed or reference-equals null).
        /// Uses Equals(obj, null) short-circuit for performance before the managed null check.
        /// </summary>
        [NoGC]
        public static bool IsNull(this Object obj)
        {
            return Equals(obj, null) || obj == null;
        }

        /// <summary>
        /// Checks whether a Unity Object is NOT null (neither destroyed nor reference-equals null).
        /// </summary>
        [NoGC]
        public static bool IsNotNull(this Object obj)
        {
            return !Equals(obj, null) && obj != null;
        }

        /// <summary>
        /// Adds a child GameObject to a parent. Throws if parent is null or destroyed.
        /// </summary>
        public static void AddChild(this GameObject parent, GameObject child)
        {
            if (parent.IsNull())
            {
                throw new System.Exception("[Ember]: GameObject is null or destroyed, cannot add child");
            }

            if (child.IsNotNull())
            {
                child.transform.SetParent(parent.transform, false);
            }
        }

        /// <summary>
        /// Recursively counts all children (including nested grandchildren) of a GameObject.
        /// </summary>
        [NoGC]
        public static int GetAllChildrenCount(this GameObject go)
        {
            static void Dfs(Transform trans, ref int c)
            {
                c += trans.childCount;
                for (int i = 0; i < trans.childCount; i++)
                {
                    Dfs(trans.GetChild(i), ref c);
                }
            }

            var count = 0;
            Dfs(go.transform, ref count);
            return count;
        }

        /// <summary>
        /// Sets the parent of a child GameObject. If parent is null, child becomes a root transform.
        /// </summary>
        public static void SetParent(this GameObject child, GameObject parent)
        {
            child.transform.SetParent(parent.IsNotNull() ? parent.transform : null, false);
        }

        /// <summary>
        /// Finds a child Transform by name using BFS.
        /// </summary>
        public static Transform GetChildByName(this GameObject gameObject, string name)
        {
            return gameObject.transform.GetChildByName(name);
        }

        /// <summary>
        /// Builds a hierarchy path string (e.g. "/Root/Parent/Child").
        /// </summary>
        [HasGC]
        public static string GetHierachyPath(this GameObject go, bool noRoot = false)
        {
            if (go.IsNull())
            {
                throw new System.Exception("[Ember]: GameObject is null or destroyed, cannot GetHierachyPath");
            }

            var sb = new StringBuilder();
            sb.Append("/").Append(go.name);

            while (go.transform.parent.IsNotNull())
            {
                if (noRoot
                    && (go.transform.parent.parent.IsNull()
                        || go.transform.parent.parent.name == "Canvas (Environment)"))
                {
                    sb.Insert(0, ".");
                    break;
                }
                go = go.transform.parent.gameObject;
                sb.Insert(0, go.name).Insert(0, "/");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Resets Transform to world origin: position (0,0,0), rotation identity, scale (1,1,1).
        /// </summary>
        public static void Reset(this Transform transform)
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Resets Transform to local origin: localPosition (0,0,0), localRotation identity, localScale (1,1,1).
        /// </summary>
        public static void ResetLocal(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Resets RectTransform: anchoredPosition3D (0,0,0), sizeDelta (0,0), localScale (1,1,1).
        /// </summary>
        public static void ResetRectTransform(this RectTransform rt)
        {
            rt.anchoredPosition3D = Vector3.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        /// <summary>
        /// Sets world position of a Transform.
        /// </summary>
        [HasGC]
        public static void SetXYZ(this Transform transform, float x = 0, float y = 0, float z = 0)
        {
            transform.position = new Vector3(x, y, z);
        }

        /// <summary>
        /// Sets local position of a Transform.
        /// </summary>
        [HasGC]
        public static void SetLocalXYZ(this Transform transform, float x = 0, float y = 0, float z = 0)
        {
            transform.localPosition = new Vector3(x, y, z);
        }

        /// <summary>
        /// Finds a child Transform by name using BFS (breadth-first search).
        /// </summary>
        public static Transform GetChildByName(this Transform tr, string name)
        {
            // BFS
            foreach (Transform child in tr)
            {
                if (child.name == name) return child;
            }

            foreach (Transform child in tr)
            {
                Transform c = GetChildByName(child, name);
                if (c.IsNotNull()) return c;
            }

            return null;
        }

        /// <summary>
        /// Sets the local scale of a Transform.
        /// </summary>
        [HasGC]
        public static void SetLocalScale(this Transform transform, float x, float y, float z)
        {
            transform.localScale = new Vector3(x, y, z);
        }

        /// <summary>
        /// Sets a uniform local scale on all three axes.
        /// </summary>
        [HasGC]
        public static void SetLocalScale(this Transform transform, float value)
        {
            transform.localScale = new Vector3(value, value, value);
        }

        /// <summary>
        /// Sets only the width of a RectTransform's sizeDelta.
        /// </summary>
        [HasGC]
        public static void SetSizeDeltaWidth(this RectTransform transform, float w)
        {
            Vector2 size = transform.sizeDelta;
            size.x = w;
            transform.sizeDelta = size;
        }

        /// <summary>
        /// Sets only the height of a RectTransform's sizeDelta.
        /// </summary>
        [HasGC]
        public static void SetSizeDeltaHeight(this RectTransform transform, float h)
        {
            Vector2 size = transform.sizeDelta;
            size.y = h;
            transform.sizeDelta = size;
        }

        #endregion
    }
}
