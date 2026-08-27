// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;

using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>
    /// UI 组件生命周期接口。
    /// 简化的 IUIBehaviour，定义组件初始化、显示/隐藏、销毁的回调。
    /// </summary>
    public interface IEUIComponent
    {
        /// <summary>组件是否可见</summary>
        bool Visible { get; set; }
        /// <summary>组件是否已销毁</summary>
        bool IsDisposed { get; }
        /// <summary>初始化（由框架在创建后调用）</summary>
        void OnInit();
        /// <summary>显示时回调</summary>
        void OnShow();
        /// <summary>隐藏时回调</summary>
        void OnHide();
        /// <summary>销毁时回调</summary>
        void OnDispose();
        /// <summary>每帧更新</summary>
        void OnUpdate();
    }
}
