// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

using System.Collections.Generic;
using UnityEngine;

namespace Ember.Basic
{
    /// <summary>
    /// 数学与曲线扩展方法，补上 Unity 自带 Mathf/AnimationCurve 没给的工具。
    /// </summary>
    public static class MathExtension
    {
        // ======== 值域判断 ========

        /// <summary>
        /// x 是否在 min 和 max 之间（不包含边界）。
        /// </summary>
        [NoGC]
        public static bool IsBetween(this float x, float min, float max) => x > min && x < max;

        /// <summary>
        /// x 是否在 min 和 max 之间（包含边界）。
        /// </summary>
        [NoGC]
        public static bool IsBetweenInclusive(this float x, float min, float max) => x >= min && x <= max;

        // ======== 圆周 ========

        /// <summary>
        /// 计算圆心 + 半径 + 角度对应的圆上坐标。
        /// </summary>
        [NoGC]
        public static Vector2 PointOnCircle(Vector2 center, float radius, float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(
                center.x + radius * Mathf.Cos(radians),
                center.y + radius * Mathf.Sin(radians));
        }

        // ======== 二次方程 ========

        /// <summary>
        /// 解一元二次方程 ax² + bx + c = 0。
        /// </summary>
        /// <returns>true = 有实数解；false = 只有复数解</returns>
        [NoGC]
        public static bool TrySolveQuadratic(float a, float b, float c, out float x1, out float x2)
        {
            float discriminant = b * b - 4f * a * c;

            if (discriminant < 0f)
            {
                x1 = x2 = 0f;
                return false;
            }

            float sqrt = Mathf.Sqrt(discriminant);
            float denom = 2f * a;
            x1 = (-b + sqrt) / denom;
            x2 = (-b - sqrt) / denom;
            return true;
        }

        // ======== 时间格式化 ========

        /// <summary>
        /// 秒数 → "HH:MM:SS" 字符串。有 GC 分配，不要每帧调。
        /// </summary>
        [HasGC]
        public static string ToTimeString(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            int h = total / 3600;
            total %= 3600;
            int m = total / 60;
            int s = total % 60;
            return $"{h:D2}:{m:D2}:{s:D2}";
        }

        // ======== AnimationCurve 扩展 ========

        /// <summary>
        /// 数值微分计算曲线在指定时间点的导数。
        /// </summary>
        [NoGC]
        public static float EvaluateDerivative(this AnimationCurve curve, float time, float delta = 0.001f)
        {
            return (curve.Evaluate(time + delta) - curve.Evaluate(time - delta)) / (2f * delta);
        }

        /// <summary>
        /// 基于贝塞尔曲线数学创建导数曲线。
        /// 采样密度根据关键帧间距自动调整。有 GC 分配（new AnimationCurve）。
        /// </summary>
        [HasGC]
        public static AnimationCurve CreateDerivativeCurve(this AnimationCurve curve)
        {
            Keyframe[] keys = curve.keys;
            if (keys.Length <= 1) return new AnimationCurve();

            var derivativeKeys = new List<Keyframe>();

            for (int i = 0; i < keys.Length - 1; i++)
            {
                float dt = keys[i + 1].time - keys[i].time;
                if (dt <= 0f) continue;

                int samples = Mathf.Max(3, Mathf.CeilToInt(dt * 10f));
                for (int j = 0; j <= samples; j++)
                {
                    float t = keys[i].time + dt * j / samples;
                    derivativeKeys.Add(new Keyframe(t, curve.EvaluateDerivative(t)));
                }
            }

            return new AnimationCurve(derivativeKeys.ToArray());
        }

    }
}
