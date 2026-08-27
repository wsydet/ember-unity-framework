// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using Cysharp.Threading.Tasks;

using UnityEngine;

namespace Ember.UI
{
    /// <summary>
    /// UI 全局过渡动画接口。
    /// EUIPage 在 Preset 模式下调用此接口的 PlayShowAsync / PlayHideAsync。
    /// 默认实现为 CanvasGroup alpha 渐入渐出，项目可替换为 DOTween、侧滑、缩放等。
    /// </summary>
    public interface IEUITransitionHandler
    {
        /// <summary>
        /// 播放打开动画。
        /// </summary>
        /// <param name="page">页面根 GameObject（可从中获取 CanvasGroup 等组件）</param>
        /// <param name="duration">动画时长（秒），来自 EUIBinding Inspector 中的"进入时长"</param>
        UniTask PlayShowAsync(GameObject page, float duration);

        /// <summary>
        /// 播放关闭动画。
        /// </summary>
        /// <param name="page">页面根 GameObject</param>
        /// <param name="duration">动画时长（秒），来自 EUIBinding Inspector 中的"退出时长"</param>
        UniTask PlayHideAsync(GameObject page, float duration);
    }

    /// <summary>
    /// 默认全局过渡动画 —— CanvasGroup alpha 渐入渐出。
    /// 由 EUIViewEngine 自动注册，项目可通过 <see cref="EUIViewEngine.TransitionHandler"/> 替换。
    /// </summary>
    public class DefaultUITransitionHandler : IEUITransitionHandler
    {
        public async UniTask PlayShowAsync(GameObject page, float duration)
        {
            if (duration <= 0f) return;
            var cg = page != null ? page.GetComponent<CanvasGroup>() : null;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (cg != null)
                    cg.alpha = Mathf.Clamp01(elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }

        public async UniTask PlayHideAsync(GameObject page, float duration)
        {
            if (duration <= 0f) return;
            var cg = page != null ? page.GetComponent<CanvasGroup>() : null;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (cg != null)
                    cg.alpha = Mathf.Clamp01(1f - (elapsed / duration));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }
    }
}
