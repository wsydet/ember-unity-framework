using System;
using Ember.Core;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 默认启动动画 —— 立即完成，无动画效果。
    ///
    /// 挂在 MainScene 的 GameObject 上，替代抽象基类 <see cref="EmberInitAnimationStarter"/>。
    /// <see cref="PlayStartupAnimation"/> 中立即调用 <c>onComplete()</c>，
    /// InitState 直接过渡到 MainState，无等待。
    ///
    /// <b>自定义动画：</b>
    /// 继承 <see cref="EmberInitAnimationStarter"/>，override <see cref="PlayStartupAnimation"/>，
    /// 在动画结束时调用 <c>onComplete()</c>。然后替换场景中的本组件。
    ///
    /// 使用方式：
    /// <code>
    /// public class MyLogoAnimation : EmberInitAnimationStarter
    /// {
    ///     [SerializeField] private CanvasGroup _logoGroup;
    ///
    ///     protected override async void PlayStartupAnimation(Action onComplete)
    ///     {
    ///         // 淡入 logo（1 秒）
    ///         await FadeIn(_logoGroup, 1f);
    ///         // 停留 0.5 秒
    ///         await Task.Delay(500);
    ///         // 淡出
    ///         await FadeOut(_logoGroup, 1f);
    ///         onComplete();  // 必须调用！
    ///     }
    /// }
    /// </code>
    /// </summary>
    public sealed class EmberDefaultInitAnimation : EmberInitAnimationStarter
    {
        protected override void PlayStartupAnimation(Action onComplete)
        {
            // 默认：无开场动画，立即完成
            onComplete();
        }
    }
}
