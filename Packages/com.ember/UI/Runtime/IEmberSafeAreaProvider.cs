// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;

using UnityEngine;

namespace Ember.UI
{
    /// <summary>
    /// 安全区提供者接口 —— 页面预制体内挂载的安全区组件实现本接口，
    /// <see cref="EUILogic"/> 通过 <c>GetComponentInChildren《IEmberSafeAreaProvider》</c> 懒加载发现。
    /// </summary>
    /// <para><b>为什么放 UI.Runtime：</b>依赖方向是 UIExtension → UI（单向），
    /// EUILogic（UI.Runtime）不能反向引用 UIExtension 的 EUISafeArea 类型；
    /// 低层定义接口、高层实现，方向合法。</para>
    public interface IEmberSafeAreaProvider
    {
        /// <summary>是否存在有效安全区域</summary>
        bool HasSafeArea { get; }

        /// <summary>安全区作用的 RectTransform（页面内容容器）</summary>
        RectTransform SafeAreaRoot { get; }

        /// <summary>安全区变化事件（设备旋转/窗口变化后触发）</summary>
        event Action SafeAreaChanged;
    }
}
