// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections;

using UnityEngine;

namespace Ember.UI
{
    /// <summary>
    /// UI 过渡动画接口。
    /// 框架层调用此接口的 PlayShowAsync / PlayHideAsync，业务层注入具体实现（如 DOTween）。
    /// 框架不绑定任何特定 Tween 引擎。
    ///
    /// <para>默认实现为无动画（立即完成），项目可通过实现此接口替换为 DOTween 等动画效果。</para>
    /// </summary>
    public interface IUITransitionHandler
    {
        /// <summary>播放打开动画。返回的 IEnumerator 在动画完成后 yield break。</summary>
        IEnumerator PlayShowAsync(GameObject page);

        /// <summary>播放关闭动画。返回的 IEnumerator 在动画完成后 yield break。</summary>
        IEnumerator PlayHideAsync(GameObject page);
    }

    /// <summary>
    /// 默认过渡动画处理器 —— 无动画，立即返回。
    /// </summary>
    public class DefaultUITransitionHandler : IUITransitionHandler
    {
        public IEnumerator PlayShowAsync(GameObject page) { yield break; }
        public IEnumerator PlayHideAsync(GameObject page) { yield break; }
    }
}
