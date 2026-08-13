// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using DG.Tweening;

using Ember.UI;

using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>
    /// 缩放动画：进入从 0 放大到 1，退出从 1 缩小到 0。
    /// </summary>
    public sealed class EUIBlockAnimationScaleUp : EUIBlockAnimation
    {
        public override EUIBlockAnimationType Type => EUIBlockAnimationType.ScaleUp;

        public override void PlayEnter(EUIBlockAnimationContext ctx)
        {
            UpdateType update = ResolveUpdateType(ctx);
            ctx.Rect.localScale = Vector3.zero;
            ctx.Rect.DOScale(Vector3.one, ctx.Duration).SetEase(ctx.Ease).SetDelay(ctx.Delay).SetUpdate(update);
        }

        public override void PlayExit(EUIBlockAnimationContext ctx)
        {
            UpdateType update = ResolveUpdateType(ctx);
            ctx.Rect.localScale = Vector3.one;
            ctx.Rect.DOScale(Vector3.zero, ctx.Duration).SetEase(ctx.Ease).SetDelay(ctx.Delay).SetUpdate(update);
        }
    }
}
