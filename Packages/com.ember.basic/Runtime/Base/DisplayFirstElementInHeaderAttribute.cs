// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

using UnityEngine;

namespace Ember.Basic
{
    /// <summary>
    /// Inspector 中把数组/列表的第一个元素的值作为 Foldout 标题显示。
    ///
    /// <code>
    /// [DisplayFirstElementInHeader]
    /// public MyData[] items;
    /// </code>
    ///
    /// 不用这个 Attribute 时，Inspector 中数组的 foldout 标题是 "Element 0", "Element 1"...。
    /// 加上后自动取第一个子字段的值当标题。
    /// </summary>
    public class DisplayFirstElementInHeaderAttribute : PropertyAttribute
    {
    }
}
