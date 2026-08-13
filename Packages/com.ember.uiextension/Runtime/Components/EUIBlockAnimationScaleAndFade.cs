// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using DG.Tweening;

using Ember.UI;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// 缩放 + 淡入淡出动画：进入从 0 放大到 1 同时淡入，退出缩小到 0 同时淡出。
    /// </summary>
    public sealed class EUIBlockAnimationScaleAndFade : EUIBlockAnimation
    {
        public override EUIBlockAnimationType Type => EUIBlockAnimationType.ScaleAndFade;

        public override void PlayEnter(EUIBlockAnimationContext ctx)
        {
            UpdateType update = ResolveUpdateType(ctx);
            ctx.Rect.localScale = Vector3.zero;
            var c = ctx.Raw.color; c.a = 0f; ctx.Raw.color = c;
            ctx.Rect.DOScale(Vector3.one, ctx.Duration).SetEase(ctx.Ease).SetDelay(ctx.Delay).SetUpdate(update);
            ctx.Raw.DOFade(1f, ctx.Duration).SetEase(ctx.Ease).SetDelay(ctx.Delay).SetUpdate(update);
        }

        public override void PlayExit(EUIBlockAnimationContext ctx)
        {
            UpdateType update = ResolveUpdateType(ctx);
            ctx.Rect.localScale = Vector3.one;
            var c = ctx.Raw.color; c.a = 1f; ctx.Raw.color = c;
            ctx.Rect.DOScale(Vector3.zero, ctx.Duration).SetEase(ctx.Ease).SetDelay(ctx.Delay).SetUpdate(update);
            ctx.Raw.DOFade(0f, ctx.Duration).SetEase(ctx.Ease).SetDelay(ctx.Delay).SetUpdate(update);
        }
    }
}
