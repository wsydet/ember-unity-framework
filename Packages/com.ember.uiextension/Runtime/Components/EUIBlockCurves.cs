// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.uiextension

using System;

using Sirenix.OdinInspector;

using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>
    /// 方块动画的曲线预设。选一个自动填充 6 条曲线；选「自定义」后手动微调曲线。
    /// </summary>
    public enum EUIBlockCurvePreset
    {
        Custom,
        GrowSmooth,
        GrowInSteps,
        GrowHorizontally,
        GrowVertically,
        Fade,
        GrowAndFade,
        SlideFromBottom,
        SlideFromLeft,
        SlideFromRight,
        SlideFromTop,
        GrowAndRotateClockwise,
        GrowAndRotateCounterClockwise,
        GrowAndBounce,
        GrowAndWobble,
        NoAnimation,
    }

    /// <summary>
    /// 方块动画的 6 条曲线集合：x/y 缩放、x/y 位移、旋转、透明度。
    /// 曲线横轴为进度 0→1（0=隐藏初始态，1=完全显现的最终态），纵轴为各属性目标值。
    /// 位移单位为「方块尺寸」，旋转单位为「整圈」（1.0=360°）。
    /// 模型移植自 TransitionBlocks 插件的 TransitionBlock（6 条 AnimationCurve）。
    /// </summary>
    [Serializable]
    public sealed class EUIBlockCurves
    {
        /// <summary>滑入/滑出的默认屏外偏移（单位=方块尺寸），覆盖常规网格分辨率。</summary>
        private const float SLIDE_BLOCKS = 20f;

        [Tooltip("X 缩放乘数，终点应为 1.0。")]
        public AnimationCurve XScale = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Y 缩放乘数，终点应为 1.0。")]
        public AnimationCurve YScale = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("X 位移（单位=方块尺寸），终点应为 0.0。")]
        public AnimationCurve XPosition = AnimationCurve.Constant(0f, 1f, 0f);

        [Tooltip("Y 位移（单位=方块尺寸），终点应为 0.0。")]
        public AnimationCurve YPosition = AnimationCurve.Constant(0f, 1f, 0f);

        [Tooltip("旋转（整圈，1.0=360°），终点应为整数。")]
        public AnimationCurve Rotation = AnimationCurve.Constant(0f, 1f, 0f);

        [Tooltip("透明度 0→1，终点应为 1.0。")]
        public AnimationCurve Alpha = AnimationCurve.Constant(0f, 1f, 1f);

        /// <summary>曲线预设下拉项（中文名），供 Inspector 的「曲线预设」下拉统一使用。</summary>
        public static readonly ValueDropdownList<EUIBlockCurvePreset> PresetItems = new()
        {
            { "自定义", EUIBlockCurvePreset.Custom },
            { "平滑放大", EUIBlockCurvePreset.GrowSmooth },
            { "分步放大", EUIBlockCurvePreset.GrowInSteps },
            { "水平生长", EUIBlockCurvePreset.GrowHorizontally },
            { "垂直生长", EUIBlockCurvePreset.GrowVertically },
            { "淡入", EUIBlockCurvePreset.Fade },
            { "缩放淡入", EUIBlockCurvePreset.GrowAndFade },
            { "从下滑入", EUIBlockCurvePreset.SlideFromBottom },
            { "从左滑入", EUIBlockCurvePreset.SlideFromLeft },
            { "从右滑入", EUIBlockCurvePreset.SlideFromRight },
            { "从上滑入", EUIBlockCurvePreset.SlideFromTop },
            { "旋转放大（顺时针）", EUIBlockCurvePreset.GrowAndRotateClockwise },
            { "旋转放大（逆时针）", EUIBlockCurvePreset.GrowAndRotateCounterClockwise },
            { "弹跳放大", EUIBlockCurvePreset.GrowAndBounce },
            { "摆动放大", EUIBlockCurvePreset.GrowAndWobble },
            { "无动画", EUIBlockCurvePreset.NoAnimation },
        };

        /// <summary>按曲线预设生成一套曲线；Custom 返回默认 GrowSmooth。</summary>
        public static EUIBlockCurves Create(EUIBlockCurvePreset preset)
        {
            return preset switch
            {
                EUIBlockCurvePreset.GrowInSteps => GrowInSteps(),
                EUIBlockCurvePreset.GrowHorizontally => GrowHorizontally(),
                EUIBlockCurvePreset.GrowVertically => GrowVertically(),
                EUIBlockCurvePreset.Fade => Fade(),
                EUIBlockCurvePreset.GrowAndFade => GrowAndFade(),
                EUIBlockCurvePreset.SlideFromBottom => Slide(false),
                EUIBlockCurvePreset.SlideFromLeft => Slide(true),
                EUIBlockCurvePreset.SlideFromRight => Slide(true, true),
                EUIBlockCurvePreset.SlideFromTop => Slide(false, true),
                EUIBlockCurvePreset.GrowAndRotateClockwise => GrowAndRotate(true),
                EUIBlockCurvePreset.GrowAndRotateCounterClockwise => GrowAndRotate(false),
                EUIBlockCurvePreset.GrowAndBounce => GrowAndBounce(),
                EUIBlockCurvePreset.GrowAndWobble => GrowAndWobble(),
                EUIBlockCurvePreset.NoAnimation => NoAnimation(),
                _ => new EUIBlockCurves(),
            };
        }

        // --------------------------------------------------------

        #region 曲线预设工厂

        private static EUIBlockCurves GrowInSteps()
        {
            var c = new EUIBlockCurves();
            c.XScale = c.YScale = Stepped(0f, 0.33f, 0.66f, 1f);
            return c;
        }

        private static EUIBlockCurves GrowHorizontally()
        {
            var c = new EUIBlockCurves();
            c.YScale = AnimationCurve.Constant(0f, 1f, 1f);
            return c;
        }

        private static EUIBlockCurves GrowVertically()
        {
            var c = new EUIBlockCurves();
            c.XScale = AnimationCurve.Constant(0f, 1f, 1f);
            return c;
        }

        private static EUIBlockCurves Fade()
        {
            var c = new EUIBlockCurves();
            c.XScale = c.YScale = AnimationCurve.Constant(0f, 1f, 1f);
            c.Alpha = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            return c;
        }

        private static EUIBlockCurves GrowAndFade()
        {
            var c = new EUIBlockCurves();
            c.Alpha = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            return c;
        }

        /// <summary>四向滑入。horizontal 决定滑入方向是水平还是垂直，flip 决定从负半轴还是正半轴滑入。</summary>
        private static EUIBlockCurves Slide(bool horizontal, bool flip = false)
        {
            var c = new EUIBlockCurves();
            c.XScale = c.YScale = AnimationCurve.Constant(0f, 1f, 1f);
            float from = flip ? SLIDE_BLOCKS : -SLIDE_BLOCKS;
            if (horizontal)
                c.XPosition = AnimationCurve.EaseInOut(0f, from, 1f, 0f);
            else
                c.YPosition = AnimationCurve.EaseInOut(0f, from, 1f, 0f);
            return c;
        }

        private static EUIBlockCurves GrowAndRotate(bool clockwise)
        {
            var c = new EUIBlockCurves();
            c.Rotation = AnimationCurve.Linear(0f, 0f, 1f, clockwise ? 1f : -1f);
            return c;
        }

        private static EUIBlockCurves GrowAndBounce()
        {
            var c = new EUIBlockCurves();
            c.XScale = c.YScale = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.7f, 1.2f),
                new Keyframe(1f, 1f));
            return c;
        }

        private static EUIBlockCurves GrowAndWobble()
        {
            var c = new EUIBlockCurves();
            c.XScale = c.YScale = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.4f, 1.15f),
                new Keyframe(0.7f, 0.9f),
                new Keyframe(1f, 1f));
            return c;
        }

        private static EUIBlockCurves NoAnimation()
        {
            var c = new EUIBlockCurves();
            c.XScale = c.YScale = AnimationCurve.Constant(0f, 1f, 1f);
            return c;
        }

        /// <summary>阶梯状曲线：在相邻节点间做平滑过渡的「分步」近似（真阶梯需手动调关键帧切线）。</summary>
        private static AnimationCurve Stepped(params float[] values)
        {
            var keys = new Keyframe[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                float t = values.Length == 1 ? 0f : (float)i / (values.Length - 1);
                keys[i] = new Keyframe(t, values[i]);
            }
            return new AnimationCurve(keys);
        }

        #endregion
    }
}
