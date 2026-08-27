// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

using System;

namespace Ember.Basic
{
    /// <summary>
    /// 更新器接口 —— 需要每帧驱动的对象实现此接口。
    /// 由 ResourceManager 或其他更新循环统一调用。
    /// </summary>
    public interface IUpdater
    {
        /// <summary>每渲染帧调用一次。</summary>
        /// <returns>true = 已失效，应从更新列表中移除，不再需要更新。</returns>
        bool Update();

        /// <summary>此 Updater 在异步加载列表之前还是之后调用。</summary>
        bool PreOrPostAsyncList { get; }

        /// <summary>资源加载优先级（用于排序）。值越小优先级越高。</summary>
        int Priority { get; }
    }

    /// <summary>
    /// 延迟释放接口 —— 实现此接口的对象不会立即被 Dispose，
    /// 而是等待 <see cref="NotYet"/> 返回 false 后才执行。
    /// </summary>
    public interface IDelayDisposable : IDisposable
    {
        /// <summary>返回 true 表示还不能释放。</summary>
        bool NotYet();
    }
}
