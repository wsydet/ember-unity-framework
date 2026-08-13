// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using DG.Tweening;

using Ember.UI;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// 方向滑动动画：进入从屏幕外滑入，退出滑回屏幕外（原路返回）。
    /// </summary>
    public sealed class EUIBlockAnimationSlideFromDirection : EUIBlockAnimation
    {
        public override EUIBlockAnimationType Type => EUIBlockAnimationType.SlideFromDirection;

        public override void PlayEnter(EUIBlockAnimationContext ctx)
        {
            UpdateType update = ResolveUpdateType(ctx);
            ctx.Rect.localScale = Vector3.one;
            ctx.Rect.anchoredPosition = ctx.Position + ctx.SlideOffset;
            ctx.Rect.DOAnchorPos(ctx.Position, ctx.Duration).SetEase(ctx.Ease).SetDelay(ctx.Delay).SetUpdate(update);
        }

        public override void PlayExit(EUIBlockAnimationContext ctx)
        {
            UpdateType update = ResolveUpdateType(ctx);
            ctx.Rect.localScale = Vector3.one;
            var c = ctx.Raw.color; c.a = 1f; ctx.Raw.color = c;
            ctx.Rect.DOAnchorPos(ctx.Position + ctx.SlideOffset, ctx.Duration).SetEase(ctx.Ease).SetDelay(ctx.Delay).SetUpdate(update);
        }
    }
}
