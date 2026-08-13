// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using Ember.Basic;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// UI 相关的 C# 扩展方法集合。
    /// </summary>
    public static class EUIBasicUIExtensions
    {
        /// <summary>
        /// 按名称查找子节点并获取其 Image 组件。
        /// </summary>
        [NoGC]
        public static Image GetChildImage(this Transform root, string childName)
        {
            var child = root.Find(childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        /// <summary>
        /// 按名称查找子节点并获取其 Text 组件。
        /// </summary>
        [NoGC]
        public static Text GetChildText(this Transform root, string childName)
        {
            var child = root.Find(childName);
            return child != null ? child.GetComponent<Text>() : null;
        }

        /// <summary>
        /// 快速设置 Graphic 的颜色（RGBA 分量）。
        /// </summary>
        [NoGC]
        public static void SetTextColor(this Graphic graphic, float r, float g, float b, float a)
        {
            graphic.color = new Color(r, g, b, a);
        }
    }
}
