// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using DG.Tweening;

using Ember.UI;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UIExtension
{
    /// <summary>
    /// 方块动画上下文 —— 一次进入/退出动画所需的全部数据。
    /// 由 <see cref="EUITransitionBlock"/> 在播放前组装好，传给具体动画。
    /// </summary>
    public struct EUIBlockAnimationContext
    {
        /// <summary>方块矩形（位移/缩放目标）</summary>
        public RectTransform Rect;

        /// <summary>方块图形（颜色/淡入淡出目标）</summary>
        public RawImage Raw;

        /// <summary>方块当前网格锚点位置（进入=目标位置，退出=起始位置）</summary>
        public Vector2 Position;

        public float Delay;
        public float Duration;
        public Ease Ease;

        /// <summary>是否由编辑器手动推进（预览模式）</summary>
        public bool ManualUpdate;

        /// <summary>滑动动画的偏移量（SlideFromDirection 使用，由调用方预计算）</summary>
        public Vector2 SlideOffset;
    }

    /// <summary>
    /// 方块动画基类 —— 仿状态机的「一个状态一个类」。
    /// 每种动画（缩放 / 缩放淡入 / 滑动）继承此类，实现进入/退出两个动作，
    /// 由 <see cref="EUIBlockAnimationRegistry"/> 反射自动发现并注册。
    /// 新增动画只需在 <see cref="EUIBlockAnimationType"/> 加一个枚举值 + 新建一个子类文件。
    /// </summary>
    public abstract class EUIBlockAnimation
    {
        /// <summary>对应的动画类型枚举（注册表据此建立映射）</summary>
        public abstract EUIBlockAnimationType Type { get; }

        /// <summary>进入动画：方块出现</summary>
        public abstract void PlayEnter(EUIBlockAnimationContext ctx);

        /// <summary>退出动画：方块消失</summary>
        public abstract void PlayExit(EUIBlockAnimationContext ctx);

        /// <summary>根据上下文解析 DOTween 的更新类型（编辑器预览用 Manual，运行时用 Normal）</summary>
        protected static UpdateType ResolveUpdateType(EUIBlockAnimationContext ctx)
            => ctx.ManualUpdate ? UpdateType.Manual : UpdateType.Normal;
    }
}
