using Cysharp.Threading.Tasks;
using Ember.Core;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 默认开屏动画 —— 无动画，立即完成。基类会先加载背景页再播 OpeningAnimationEnd。
    ///
    /// 挂在 MainScene 的 GameObject 上，替代抽象基类 <see cref="EUIMainAnimationStarter"/>。
    /// <see cref="PlayOpeningAnimation"/> 直接返回 <see cref="UniTask.CompletedTask"/>。
    ///
    /// <b>自定义动画：</b>
    /// 继承 <see cref="EUIMainAnimationStarter"/>，override <see cref="PlayOpeningAnimation"/>，
    /// 返回动画 UniTask 即可（无需手动触发完成）。然后替换场景中的本组件。
    ///
    /// 使用方式：
    /// <code>
    /// public class MyLogoAnimation : EUIMainAnimationStarter
    /// {
    ///     [SerializeField] private CanvasGroup _logoGroup;
    ///
    ///     protected override async UniTask PlayOpeningAnimation()
    ///     {
    ///         // 淡入 logo（1 秒）
    ///         await FadeIn(_logoGroup, 1f);
    ///         // 停留 0.5 秒
    ///         await UniTask.Delay(500);
    ///         // 淡出
    ///         await FadeOut(_logoGroup, 1f);
    ///         // 无需调用 onComplete，框架会自动等待背景页并广播 OpeningAnimationEnd
    ///     }
    /// }
    /// </code>
    /// </summary>
    public sealed class EUIDefaultMainAnimation : EUIMainAnimationStarter
    {
        protected override UniTask PlayOpeningAnimation()
        {
            // 默认：无开场动画，立即完成
            return UniTask.CompletedTask;
        }
    }
}
