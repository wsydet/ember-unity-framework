// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

using System;
using UnityEngine;

namespace Ember.Basic
{
    /// <summary>
    /// 设备性能分级配置（ScriptableObject）。
    ///
    /// 存储 GPU 型号阈值、iOS 设备代数阈值、Fallback RAM/CPU 阈值。
    /// 在 Inspector 中可编辑，无需改代码即可调整分级规则。
    ///
    /// 资源路径：Assets/Resources/EmberPerformanceConfig.asset
    /// 加载方式：Resources.Load《EmberPerformanceConfigSO》("EmberPerformanceConfig")
    /// </summary>
    [CreateAssetMenu(menuName = "Ember/Performance Config", fileName = "EmberPerformanceConfig", order = 100)]
    public class EmberPerformanceConfigSO : ScriptableObject
    {
        [Header("Adreno（高通）")]
        [Tooltip("VeryHigh: model >= X && RAM >= Y MB | High: model >= X | Mid: model >= X | VeryLow: model in [1, X]")]
        public GpuModelThresholds adreno = new()
        {
            veryHighModel = 740,
            veryHighRam = 10000,
            highModel = 661,
            midModel = 621,
            veryLowMaxModel = 500,
        };

        [Header("Mali-G / Immortalis-G（ARM）")]
        public GpuModelThresholds maliG = new()
        {
            veryHighModel = 715,
            veryHighRam = 10000,
            highModel = 701,
            midModel = 77,
            veryLowMaxModel = 52,
        };

        [Header("Maleoon（华为）")]
        [Tooltip("VeryHigh: model >= X && RAM >= Y | High: model >= X（RAM 不足 Y）| 其他: Mid")]
        public GpuModelThresholds maleoon = new()
        {
            veryHighModel = 910,
            veryHighRam = 10000,
            highModel = 910,
        };

        [Header("Mali-T（ARM 老款，仅 RAM 判定）")]
        public int maliTVeryLowMaxRam = 3500;

        [Header("PowerVR")]
        public int powerVRMidMinRam = 6000;
        public int powerVRVeryLowMaxRam = 3500;

        [Header("iOS — iPhone")]
        public IosDeviceThresholds iphone = new()
        {
            veryHighMajor = 15,
            veryHighRam = 7000,
            highMajor = 13,
            midMajor = 10,
            veryLowMaxRam = 2500,
        };

        [Header("iOS — iPad")]
        public IosDeviceThresholds ipad = new()
        {
            veryHighMajor = 13,
            veryHighRam = 7000,
            highMajor = 12,
            midMajor = 10,
            veryLowMaxRam = 2500,
        };

        [Header("Fallback（纯 RAM + CPU）")]
        public FallbackThresholds fallback = new()
        {
            veryHighRam = 12000,
            veryHighCpu = 2500,
            highRam = 8000,
            highCpu = 2200,
            midRam = 6000,
            midCpu = 2000,
            veryLowMaxRam = 3000,
        };

        [Header("Editor / 内存兜底")]
        [Tooltip("Editor 中和 Fallback 时纯按内存判定画质")]
        public int memoryHighMinRam = 12000;
        public int memoryMidMinRam = 6000;
        public int iosMemoryHighMinRam = 5000;
        public int iosMemoryMidMinRam = 3000;
    }

    /// <summary>
    /// GPU 型号阈值组。用于 Adreno、Mali-G、Maleoon 等以数字型号命名的 GPU 系列。
    /// 规则从上到下优先匹配：VeryHigh → High → Mid → Low（默认）→ VeryLow（仅型号 <= veryLowMaxModel 时）。
    /// </summary>
    [Serializable]
    public class GpuModelThresholds
    {
        [Tooltip("VeryHigh 最低型号")]
        public int veryHighModel;
        [Tooltip("VeryHigh 最低 RAM（MB）")]
        public int veryHighRam;

        [Tooltip("High 最低型号（model >= X 且不满足 VeryHigh 时）")]
        public int highModel;

        [Tooltip("Mid 最低型号（model >= X 且不满足 High 时）")]
        public int midModel;

        [Tooltip("VeryLow 最高型号（model in [1, X] 且不满足 Mid 时）")]
        public int veryLowMaxModel;
    }

    /// <summary>
    /// iOS 设备代数阈值组。
    /// </summary>
    [Serializable]
    public class IosDeviceThresholds
    {
        [Tooltip("VeryHigh 最低代数")]
        public int veryHighMajor;
        [Tooltip("VeryHigh 最低 RAM（MB）")]
        public int veryHighRam;

        [Tooltip("High 最低代数")]
        public int highMajor;

        [Tooltip("Mid 最低代数")]
        public int midMajor;

        [Tooltip("VeryLow 最高 RAM（MB），<= 此值为 VeryLow")]
        public int veryLowMaxRam;
    }

    /// <summary>
    /// Fallback RAM + CPU 阈值组。
    /// </summary>
    [Serializable]
    public class FallbackThresholds
    {
        [Tooltip("VeryHigh: RAM >= X && CPU >= Y")]
        public int veryHighRam;
        public int veryHighCpu;

        [Tooltip("High: RAM >= X || (RAM >= Y && CPU >= Z)")]
        public int highRam;
        public int highCpu;

        [Tooltip("Mid: RAM >= X || CPU >= Y")]
        public int midRam;
        public int midCpu;

        [Tooltip("VeryLow: RAM <= X")]
        public int veryLowMaxRam;
    }
}
