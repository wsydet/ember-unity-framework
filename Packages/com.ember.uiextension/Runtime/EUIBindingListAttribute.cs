// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System;

namespace Ember.UIExtension
{
    /// <summary>
    /// 标记 EUIBinding.BindingEntry[] 数组字段，
    /// 使其由 EUIBindingListDrawer（Editor 端）渲染。
    /// 仅作标记，无运行时逻辑。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class EUIBindingListAttribute : Attribute { }
}
