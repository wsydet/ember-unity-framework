// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
//
// This file is part of Ember Unity Packages.
// Package: com.ember

using System;
using System.Collections;
using UnityEngine;

namespace Ember.Basic
{
    /// <summary>
    /// 设备性能分级检测工具。
    ///
    /// 自动检测手机 GPU / CPU / RAM，映射到 <see cref="PerformanceLevel"/> 五档。
    /// 检测结果缓存在 PlayerPrefs 中，后续启动直接读取。
    /// GPU 型号阈值等参数从 <see cref="EmberPerformanceConfigSO"/> 加载（可在 Inspector 中编辑）。
    ///
    /// 检测策略（按平台）：
    /// <list type="bullet">
    ///   <item>iOS：按 iPhone/iPad 代数 + RAM 判定</item>
    ///   <item>Android：按 GPU 型号（Adreno / Mali / Maleoon / PowerVR）+ RAM 判定</item>
    ///   <item>其他：按 RAM + CPU 频率判定</item>
    /// </list>
    ///
    /// 用法：
    /// <code>
    /// GraphicLevelUtils.EnsurePhoneLevelInitialized();
    /// var level = GraphicLevelUtils.GetCurrentLevel();
    /// if (level >= PerformanceLevel.High) { EnableHighQualityEffects(); }
    /// </code>
    /// </summary>
    public static class GraphicLevelUtils
    {
        #region 内部参数

        private const string TAG = LogTags.BasicPerformance;
        private const string ConfigResourcePath = "EmberPerformanceConfig";

        // PlayerPrefs keys
        private const string PhoneLevelKey = "Ember.PhoneLevel";
        private const string GraphicLevelKey = "Ember.GraphicLevel";
        private const string FpsKey = "Ember.Fps";

        // 未初始化的哨兵值
        private const int NotSet = -1;

        private static EmberPerformanceConfigSO _config;
        private static bool _configLoaded;

        #endregion

        // ============================================================

        #region 外部方法

        /// <summary>
        /// 设置目标帧率并持久化。
        /// </summary>
        public static void SetFrameRatePrefs(int fps)
        {
            PlayerPrefs.SetInt(FpsKey, fps);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 获取已持久化的目标帧率，未设置时返回默认值。
        /// </summary>
        public static int GetFrameRatePrefs(int defaultValue = 60)
        {
            return PlayerPrefs.GetInt(FpsKey, defaultValue);
        }

        /// <summary>
        /// 确保手机档位已初始化。如果已有缓存则直接返回，否则执行检测并保存。
        /// </summary>
        public static void EnsurePhoneLevelInitialized()
        {
            var savedPhoneLevel = PlayerPrefs.GetInt(PhoneLevelKey, NotSet);
            if (IsValidPhoneLevel(savedPhoneLevel))
            {
                EmberDebug.Log(TAG, $"使用已缓存的手机档位: {(PerformanceLevel)savedPhoneLevel}");
                return;
            }

            var phoneLevel = DetectPhoneLevel(out var source);
            SavePhoneLevel(phoneLevel, keepCurrentGraphicLevel: true);

            EmberDebug.Log(TAG,
                $"手机档位检测完成 | source={source} | level={(PerformanceLevel)phoneLevel} | "
                + $"device={SystemInfo.deviceModel} | gpu={SystemInfo.graphicsDeviceName} | "
                + $"ram={SystemInfo.systemMemorySize}MB | cpu={SystemInfo.processorFrequency}MHz");
        }

        /// <summary>
        /// 获取当前设备性能档位（需先调用 EnsurePhoneLevelInitialized）。
        /// </summary>
        public static PerformanceLevel GetCurrentLevel()
        {
            var phoneLevel = PlayerPrefs.GetInt(PhoneLevelKey, NotSet);
            if (phoneLevel == NotSet) return PerformanceLevel.Mid;
            return (PerformanceLevel)phoneLevel;
        }

        /// <summary>
        /// 判断是否为高端或旗舰设备。
        /// </summary>
        public static bool IsHighOrHighestPhone()
        {
            var level = GetCurrentLevel();
            return level == PerformanceLevel.High || level == PerformanceLevel.VeryHigh;
        }

        /// <summary>
        /// 判断是否为旗舰设备。
        /// </summary>
        public static bool IsHighestPhone()
        {
            return GetCurrentLevel() == PerformanceLevel.VeryHigh;
        }

        /// <summary>
        /// 判断是否为入门设备。
        /// </summary>
        public static bool IsLowestPhone()
        {
            return GetCurrentLevel() == PerformanceLevel.VeryLow;
        }

        /// <summary>
        /// 获取综合性能档位（结合手机硬件档位和用户画质设置）。
        /// 通过协程确保已初始化后回调。
        /// </summary>
        public static IEnumerator GetGraphicLevel(Action<int, int> callback)
        {
            EnsurePhoneLevelInitialized();

            var curLevel = GetPerformanceLevel();
            if (curLevel != NotSet)
            {
                EmberDebug.Log(TAG, $"GetPerformanceLevel: {(PerformanceLevel)curLevel}");
                callback?.Invoke(curLevel, PlayerPrefs.GetInt(FpsKey, 60));
                yield break;
            }

            curLevel = GetGraphicLevelByMemorySize();
            callback?.Invoke(curLevel, 0);
        }

        #endregion

        // ============================================================

        #region 内部方法 —— 配置加载

        /// <summary>
        /// 延迟加载 SO 配置。首次调用时从 Resources 加载，
        /// 加载失败则使用默认值（SO 中定义的字段初始值）。
        /// </summary>
        private static EmberPerformanceConfigSO LoadConfig()
        {
            if (_configLoaded) return _config;
            _configLoaded = true;

            _config = Resources.Load<EmberPerformanceConfigSO>(ConfigResourcePath);
            if (_config == null)
            {
                EmberDebug.LogWarning(TAG,
                    "EmberPerformanceConfig.asset 未找到，使用默认阈值。"
                    + "可通过菜单 Ember/Performance Config 创建配置资产。");
                // 创建一个临时实例以使用其默认字段值
                _config = ScriptableObject.CreateInstance<EmberPerformanceConfigSO>();
            }

            return _config;
        }

        #endregion

        // ============================================================

        #region 内部方法 —— PlayerPrefs

        private static void SavePhoneLevel(int phoneLevel, bool keepCurrentGraphicLevel = false)
        {
            if (!IsValidPhoneLevel(phoneLevel))
                phoneLevel = (int)PerformanceLevel.Low;

            PlayerPrefs.SetInt(PhoneLevelKey, phoneLevel);

            if (keepCurrentGraphicLevel && IsValidGraphicLevel(PlayerPrefs.GetInt(GraphicLevelKey, NotSet)))
            {
                EmberDebug.Log(TAG, $"保持当前画质档位: {PlayerPrefs.GetInt(GraphicLevelKey, 0)}");
                return;
            }

            int graphicLevel;
            switch ((PerformanceLevel)phoneLevel)
            {
                case PerformanceLevel.VeryHigh:
                case PerformanceLevel.High:
                    graphicLevel = 2;
                    break;
                case PerformanceLevel.Mid:
                    graphicLevel = 1;
                    break;
                case PerformanceLevel.Low:
                case PerformanceLevel.VeryLow:
                default:
                    graphicLevel = 0;
                    break;
            }

            PlayerPrefs.SetInt(GraphicLevelKey, graphicLevel);
            PlayerPrefs.Save();

            EmberDebug.Log(TAG, $"保存手机档位: {(PerformanceLevel)phoneLevel}, 画质档位: {graphicLevel}");
        }

        private static int GetPerformanceLevel()
        {
            var phoneLevel = PlayerPrefs.GetInt(PhoneLevelKey, NotSet);
            if (phoneLevel == NotSet) return NotSet;

            var graphicLevel = PlayerPrefs.GetInt(GraphicLevelKey, NotSet);
            if (graphicLevel == NotSet) return NotSet;

            switch (graphicLevel)
            {
                case 0:
                    return phoneLevel == (int)PerformanceLevel.VeryLow
                        ? (int)PerformanceLevel.VeryLow
                        : (int)PerformanceLevel.Low;
                case 1:
                    return (int)PerformanceLevel.Mid;
                case 2:
                    return phoneLevel == (int)PerformanceLevel.VeryHigh
                        ? (int)PerformanceLevel.VeryHigh
                        : (int)PerformanceLevel.High;
                default:
                    return (int)PerformanceLevel.Mid;
            }
        }

        #endregion

        // ============================================================

        #region 内部方法 —— 设备检测

        private static int DetectPhoneLevel(out string source)
        {
#if UNITY_EDITOR
            source = "EditorMemory";
            return GetGraphicLevelByMemorySize();
#else
            var ram = SystemInfo.systemMemorySize;
            var cpu = SystemInfo.processorFrequency;
            var deviceModel = SystemInfo.deviceModel;
            var gpuName = SystemInfo.graphicsDeviceName;

            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                source = "IOSModel";
                return GetIosPhoneLevel(deviceModel, ram, cpu);
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                var gpuLevel = GetAndroidGpuPhoneLevel(gpuName, ram);
                if (IsValidPhoneLevel(gpuLevel))
                {
                    source = "AndroidGPU";
                    return gpuLevel;
                }

                source = "AndroidFallback";
                return GetFallbackPhoneLevel(ram, cpu);
            }

            source = "Memory";
            return GetFallbackPhoneLevel(ram, cpu);
#endif
        }

        /// <summary>
        /// 纯内存判定（Editor 和 Fallback 使用）。阈值从 SO 读取。
        /// </summary>
        private static int GetGraphicLevelByMemorySize()
        {
            var cfg = LoadConfig();
            var ram = SystemInfo.systemMemorySize;
#if !UNITY_EDITOR && UNITY_IOS
            if (ram > cfg.iosMemoryHighMinRam) return (int)PerformanceLevel.High;
            if (ram > cfg.iosMemoryMidMinRam) return (int)PerformanceLevel.Mid;
#else
            if (ram > cfg.memoryHighMinRam) return (int)PerformanceLevel.High;
            if (ram > cfg.memoryMidMinRam) return (int)PerformanceLevel.Mid;
#endif
            return (int)PerformanceLevel.Low;
        }

        /// <summary>
        /// RAM + CPU 兜底判定。阈值从 SO 读取。
        /// </summary>
        private static int GetFallbackPhoneLevel(int ram, int cpu)
        {
            var cfg = LoadConfig();
            var f = cfg.fallback;

            if (ram >= f.veryHighRam && cpu >= f.veryHighCpu)
                return (int)PerformanceLevel.VeryHigh;
            if (ram >= f.veryHighRam || (ram >= f.highRam && cpu >= f.highCpu))
                return (int)PerformanceLevel.High;
            if (ram >= f.midRam || cpu >= f.midCpu)
                return (int)PerformanceLevel.Mid;
            if (ram <= f.veryLowMaxRam)
                return (int)PerformanceLevel.VeryLow;
            return (int)PerformanceLevel.Low;
        }

        /// <summary>
        /// iOS 设备检测：按设备代数 + RAM 综合判定。阈值从 SO 读取。
        /// </summary>
        private static int GetIosPhoneLevel(string deviceModel, int ram, int cpu)
        {
            var cfg = LoadConfig();

            if (TryParseAppleDeviceMajor(deviceModel, "iPhone", out var major))
                return EvaluateIosRule(major, ram, cfg.iphone);

            if (TryParseAppleDeviceMajor(deviceModel, "iPad", out major))
                return EvaluateIosRule(major, ram, cfg.ipad);

            return GetFallbackPhoneLevel(ram, cpu);
        }

        private static int EvaluateIosRule(int major, int ram, IosDeviceThresholds t)
        {
            if (major >= t.veryHighMajor && ram >= t.veryHighRam)
                return (int)PerformanceLevel.VeryHigh;
            if (major >= t.highMajor)
                return (int)PerformanceLevel.High;
            if (major >= t.midMajor)
                return (int)PerformanceLevel.Mid;
            return ram <= t.veryLowMaxRam
                ? (int)PerformanceLevel.VeryLow
                : (int)PerformanceLevel.Low;
        }

        /// <summary>
        /// Android GPU 检测。识别 Adreno / Mali / Maleoon / PowerVR 系列。
        /// 阈值从 SO 读取，无法识别时返回 NotSet。
        /// </summary>
        private static int GetAndroidGpuPhoneLevel(string gpuName, int ram)
        {
            if (string.IsNullOrEmpty(gpuName))
                return NotSet;

            var cfg = LoadConfig();
            var gpu = gpuName.ToUpperInvariant();

            if (gpu.Contains("ADRENO"))
                return EvaluateGpuModelRule(gpu, ram, cfg.adreno);

            if (gpu.Contains("MALI-G") || gpu.Contains("IMMORTALIS-G"))
                return EvaluateGpuModelRule(gpu, ram, cfg.maliG);

            if (gpu.Contains("MALI-T"))
                return ram <= cfg.maliTVeryLowMaxRam
                    ? (int)PerformanceLevel.VeryLow
                    : (int)PerformanceLevel.Low;

            if (gpu.Contains("MALEOON"))
                return EvaluateMaleoonRule(gpu, ram, cfg.maleoon);

            if (gpu.Contains("POWERVR"))
                return EvaluatePowerVRRule(gpu, ram, cfg);

            return NotSet;
        }

        /// <summary>
        /// 通用 GPU 型号规则评估。适用于 Adreno、Mali-G 等型号数字递增的 GPU 系列。
        /// 从上到下检查：VeryHigh → High → Mid → VeryLow → Low（默认）。
        /// </summary>
        private static int EvaluateGpuModelRule(string gpuName, int ram, GpuModelThresholds t)
        {
            var model = GetFirstNumber(gpuName);
            if (model >= t.veryHighModel && ram >= t.veryHighRam)
                return (int)PerformanceLevel.VeryHigh;
            if (model >= t.highModel)
                return (int)PerformanceLevel.High;
            if (model >= t.midModel)
                return (int)PerformanceLevel.Mid;
            if (model >= 1 && model <= t.veryLowMaxModel)
                return (int)PerformanceLevel.VeryLow;
            return (int)PerformanceLevel.Low;
        }

        /// <summary>
        /// Maleoon GPU 特殊规则：VeryHigh 和 High 共用同一型号阈值，仅 RAM 区分。
        /// </summary>
        private static int EvaluateMaleoonRule(string gpuName, int ram, GpuModelThresholds t)
        {
            var model = GetFirstNumber(gpuName);
            if (model >= t.veryHighModel)
                return ram >= t.veryHighRam
                    ? (int)PerformanceLevel.VeryHigh
                    : (int)PerformanceLevel.High;
            return (int)PerformanceLevel.Mid;
        }

        /// <summary>
        /// PowerVR GPU 特殊规则：区分高端子系列（B-Series/XT/GM9）和普通系列。
        /// </summary>
        private static int EvaluatePowerVRRule(string gpuName, int ram, EmberPerformanceConfigSO cfg)
        {
            var isHighEnd = gpuName.Contains("B-SERIES")
                || gpuName.Contains("XT")
                || gpuName.Contains("GM9");

            if (isHighEnd)
                return ram >= cfg.powerVRMidMinRam
                    ? (int)PerformanceLevel.Mid
                    : (int)PerformanceLevel.Low;

            return ram <= cfg.powerVRVeryLowMaxRam
                ? (int)PerformanceLevel.VeryLow
                : (int)PerformanceLevel.Low;
        }

        #endregion

        // ============================================================

        #region 内部方法 —— 辅助

        private static int GetFirstNumber(string value)
        {
            var result = 0;
            var hasNumber = false;
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c < '0' || c > '9')
                {
                    if (hasNumber) break;
                    continue;
                }
                hasNumber = true;
                result = result * 10 + c - '0';
            }
            return hasNumber ? result : 0;
        }

        private static bool TryParseAppleDeviceMajor(string deviceModel, string prefix, out int major)
        {
            major = 0;
            if (string.IsNullOrEmpty(deviceModel) || !deviceModel.StartsWith(prefix, StringComparison.Ordinal))
                return false;

            for (var i = prefix.Length; i < deviceModel.Length; i++)
            {
                var c = deviceModel[i];
                if (c < '0' || c > '9') break;
                major = major * 10 + c - '0';
            }
            return major > 0;
        }

        private static bool IsValidPhoneLevel(int phoneLevel)
        {
            return phoneLevel >= (int)PerformanceLevel.VeryHigh
                && phoneLevel <= (int)PerformanceLevel.VeryLow;
        }

        private static bool IsValidGraphicLevel(int graphicLevel)
        {
            return graphicLevel >= 0 && graphicLevel <= 2;
        }

        #endregion
    }
}
