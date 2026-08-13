using System;
using Ember.Core;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 默认开屏动画 —— 立即完成，无动画效果。
    ///
    /// 挂在 MainScene 的 GameObject 上，替代抽象基类 <see cref="EmberMainAnimationStarter"/>。
    /// <see cref="PlayOpeningAnimation"/> 中立即调用 <c>onComplete()</c>，
    /// MainState 直接收到 OpeningAnimationEnd。
    ///
    /// <b>自定义动画：</b>
    /// 继承 <see cref="EmberMainAnimationStarter"/>，override <see cref="PlayOpeningAnimation"/>，
    /// 在动画结束时调用 <c>onComplete()</c>。然后替换场景中的本组件。
    ///
    /// 使用方式：
    /// <code>
    /// public class MyLogoAnimation : EmberMainAnimationStarter
    /// {
    ///     [SerializeField] private CanvasGroup _logoGroup;
    ///
    ///     protected override async void PlayOpeningAnimation(Action onComplete)
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
    public sealed class EmberDefaultMainAnimation : EmberMainAnimationStarter
    {
        protected override void PlayOpeningAnimation(Action onComplete)
        {
            // 默认：无开场动画，立即完成
            onComplete();
        }
    }
}
