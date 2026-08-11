// Copyright (c) 2026 Ember Unity Framework.

using System;

using UnityEngine;

namespace Ember.UI
{
    /// <summary>
    /// EUILoading 的自定义页面参数。
    /// 在 EUIBinding Inspector 的"EUILoading"折叠框中显示。
    /// </summary>
    [Serializable]
    public class EUILoadingSettings
    {
        [Header("进度显示")]
        [Tooltip("是否显示进度条")]
        public bool useProgressBar = true;

        [Tooltip("是否显示进度数字（百分比文本）")]
        public bool useProgressNumber = true;

        [Header("假进度")]
        [Tooltip("快充阶段时长（秒），进度从 0 匀速到阈值（fastFillThreshold）")]
        [Range(0.5f, 10f)]
        public float fastFillDuration = 1.5f;

        [Tooltip("快充阈值（0.0~1.0）。进度到达此值后：若真实加载完成则平滑收尾，否则在此值等待。")]
        [Range(0.3f, 0.9f)]
        public float fastFillThreshold = 0.6f;

        [Tooltip("收尾时长（秒），真实加载完成后从当前进度平滑过渡到 1.0")]
        [Range(0.3f, 3f)]
        public float tailDuration = 1f;

        [Header("自定义过渡动画")]
        [Tooltip("进入动画时长（秒），控制进度条组渐显速度")]
        [Range(0f, 3f)]
        public float customEnterDuration = 0.3f;

        [Tooltip("退出动画时长（秒），控制进度条组渐隐速度")]
        [Range(0f, 3f)]
        public float customExitDuration = 0.2f;
    }
}
