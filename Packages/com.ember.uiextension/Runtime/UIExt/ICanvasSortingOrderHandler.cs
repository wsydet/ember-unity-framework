// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

namespace Ember.UIExtension
{
    /// <summary>
    /// Canvas 排序变化回调接口。
    /// 当组件的父 Canvas sortingOrder 发生变化时，实现此接口的组件将收到 <see cref="UpdateSortingOrder"/> 调用。
    /// </summary>
    public interface ICanvasSortingOrderHandler
    {
        /// <summary>
        /// 当父 Canvas 的 sortingOrder 变化时调用，实现者应同步更新自身的渲染排序。
        /// </summary>
        void UpdateSortingOrder();
    }
}
