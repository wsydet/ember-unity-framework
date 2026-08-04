// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

using System;

namespace Ember.Basic
{
    /// <summary>
    /// 可池化对象接口。
    /// 实现此接口的对象可以被对象池回收和重用。
    /// </summary>
    public interface IPool : IDisposable
    {
        /// <summary>
        /// 从池中取出后调用，用于重置/初始化对象状态。
        /// </summary>
        void Revive();
    }
}
