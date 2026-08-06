// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember.basic

namespace Ember.Basic
{
    /// <summary>
    /// 设备性能档位 —— 框架统一的性能分级标准。
    ///
    /// 画质分级、LOD 策略、特效密度、帧率目标等都可以基于此枚举做判断。
    /// 五档覆盖从入门机到旗舰机的完整范围。
    ///
    /// 用法：
    /// <code>
    /// var level = GraphicLevelUtils.GetCurrentLevel();
    /// if (level >= PerformanceLevel.High) { EnableHighQualityEffects(); }
    /// </code>
    /// </summary>
    public enum PerformanceLevel
    {
        /// <summary>旗舰设备（如 iPhone 15 Pro、Adreno 750 + 12GB）</summary>
        VeryHigh,

        /// <summary>高端设备（如 iPhone 13、Adreno 660 + 8GB）</summary>
        High,

        /// <summary>中端设备（如 iPhone 10、Adreno 620 + 6GB）</summary>
        Mid,

        /// <summary>低端设备（如 iPhone 8、Adreno 512 + 4GB）</summary>
        Low,

        /// <summary>入门设备（如 Mali-T、3GB 以下）</summary>
        VeryLow,
    }
}
