// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

using System;
using UnityEngine;

namespace Ember.Basic
{
    /// <summary>
    /// 二维 AnimationCurve 组合 —— 分别控制 X 和 Y 随时间的变化。
    ///
    /// 典型场景：一个物体沿自定义路径移动，X 轴位移和 Y 轴位移各自用一条曲线描述。
    /// 比用两个独立 AnimationCurve 变量更清晰——调用方只需传一个参数。
    ///
    /// <code>
    /// var path = new FloatCurve2D { x = curveX, y = curveY };
    /// Vector2 pos = path.Evaluate(t);
    /// </code>
    /// </summary>
    [Serializable]
    public class FloatCurve2D
    {
        public AnimationCurve x;
        public AnimationCurve y;

        /// <summary>
        /// 在时间点 t 同时采样 X 和 Y 曲线，返回组合后的 Vector2。
        /// </summary>
        [NoGC]
        public Vector2 Evaluate(float t) => new(x.Evaluate(t), y.Evaluate(t));
    }
}
