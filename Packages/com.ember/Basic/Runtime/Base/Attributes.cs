// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

using System;

namespace Ember.Basic
{
    /// <summary>
    /// 标记一个方法或类型会产生 GC 分配。
    /// 用于代码审查和性能分析时快速定位分配点。
    /// </summary>
    public class HasGCAttribute : Attribute
    {
        public HasGCAttribute(string _ = "") { }
    }

    /// <summary>
    /// 标记一个方法或类型是零 GC 分配的。
    /// </summary>
    public class NoGCAttribute : Attribute { }

    /// <summary>
    /// 标记一个方法或类型仅供测试使用，不应在正式代码中调用。
    /// </summary>
    public class ForTestAttribute : Attribute
    {
        public ForTestAttribute(string _ = "") { }
    }

    /// <summary>
    /// 标记一个方法或类型仅供调试使用，不应在正式代码中调用。
    /// </summary>
    public class ForDebugAttribute : Attribute
    {
        public ForDebugAttribute(string _ = "") { }
    }

    /// <summary>
    /// 标记一个方法或类型为遗留代码，计划在未来版本中移除。
    /// </summary>
    public class LegacyAttribute : Attribute
    {
        public LegacyAttribute(string _ = "") { }
    }
}
