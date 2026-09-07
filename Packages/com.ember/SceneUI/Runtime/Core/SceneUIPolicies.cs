// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using Ember.Basic;

using UnityEngine;

namespace Ember.SceneUI
{
    /// <summary>Anchor 的更新检测方式。</summary>
    public enum SceneUIUpdatePolicy
    {
        /// <summary>仅在调用方显式标记空间脏时读取 Anchor。</summary>
        Manual = 0,

        /// <summary>每次 Context 更新时读取 Anchor，位置变化后才重新投影。</summary>
        PollPosition = 1,
    }

    /// <summary>目标离开可见区域后的处理方式。</summary>
    public enum SceneUIOutOfBoundsMode
    {
        Hide = 0,
        Clamp = 1,
        Keep = 2,
    }

    /// <summary>View 实例在不可见时的生命周期策略。</summary>
    public enum SceneUIViewLifetimePolicy
    {
        KeepWhileRegistered = 0,
        RecycleWhenInvisible = 1,
        RecycleAfterDelay = 2,
    }

    /// <summary>判断 View 与可见区域关系时使用的边界精度。</summary>
    public enum SceneUIBoundsMode
    {
        /// <summary>仅检查锚点。</summary>
        Point = 0,

        /// <summary>View 矩形只要与可见区域相交就视为在屏幕内。</summary>
        PartialRect = 1,

        /// <summary>View 矩形必须完整位于可见区域内。</summary>
        FullRect = 2,
    }

    /// <summary>同一 Context 内 View 的层级排序方式。</summary>
    public enum SceneUISortingMode
    {
        /// <summary>保持 ViewHost 的获取顺序。</summary>
        None = 0,

        /// <summary>按固定优先级排序；更高优先级显示在更上层。</summary>
        Priority = 1,

        /// <summary>先按固定优先级，再让更近的目标显示在更上层。</summary>
        PriorityThenDepth = 2,
    }

    /// <summary>遮挡结果尚未建立时的临时显示策略。</summary>
    public enum SceneUIOcclusionUnknownMode
    {
        Show = 0,
        Hide = 1,
    }

    /// <summary>Entry 当前缓存的遮挡状态。</summary>
    public enum SceneUIOcclusionState
    {
        Disabled = 0,
        Unknown = 1,
        Visible = 2,
        Occluded = 3,
    }

    /// <summary>缩放计算模式。</summary>
    public enum SceneUIScaleMode
    {
        FixedScreenSize = 0,
        SceneRelative = 1,
    }

    /// <summary>Entry 当前不可见或投影失败的原因。</summary>
    public enum SceneUIInvisibleReason
    {
        None = 0,
        ContextHidden = 1,
        BusinessHidden = 2,
        AnchorInvalid = 3,
        ProjectionInvalid = 4,
        BehindCamera = 5,
        BeforeNearClip = 6,
        BeyondFarClip = 7,
        OutOfBounds = 8,
        ViewUnavailable = 9,
        Occluded = 10,
        OcclusionUnknown = 11,
    }

    /// <summary>空间可见性策略。</summary>
    public struct SceneUIVisibilityPolicy
    {
        #region 编辑器面板参数

        /// <summary>屏幕外处理方式。</summary>
        public SceneUIOutOfBoundsMode OutOfBoundsMode;

        /// <summary>从可见区域四边向内收缩的屏幕像素。</summary>
        public float ScreenPadding;

        /// <summary>是否检查相机远裁剪面。</summary>
        public bool CheckFarClip;

        /// <summary>锚点、部分矩形或完整矩形边界判定。</summary>
        public SceneUIBoundsMode BoundsMode;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>默认策略：屏幕外隐藏，并检查远裁剪面。</summary>
        public static SceneUIVisibilityPolicy DefaultHide
        {
            [NoGC]
            get
            {
                return new SceneUIVisibilityPolicy
                {
                    OutOfBoundsMode = SceneUIOutOfBoundsMode.Hide,
                    ScreenPadding = 0f,
                    CheckFarClip = true,
                    BoundsMode = SceneUIBoundsMode.Point,
                };
            }
        }

        #endregion
    }

    /// <summary>单个 Entry 的预算化遮挡检测策略。</summary>
    public struct SceneUIOcclusionPolicy
    {
        #region 编辑器面板参数

        /// <summary>是否为该 Entry 启用遮挡检测。</summary>
        public bool Enabled;

        /// <summary>同一 Entry 两次检测的最小间隔，单位为秒。</summary>
        public float RetestIntervalSeconds;

        /// <summary>目标中心附近不视作遮挡的半径，用于忽略目标自身碰撞体。</summary>
        public float TargetRadius;

        /// <summary>第一次检测完成前的显示策略。</summary>
        public SceneUIOcclusionUnknownMode UnknownMode;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public static SceneUIOcclusionPolicy Disabled => default;

        public static SceneUIOcclusionPolicy Default
        {
            [NoGC]
            get
            {
                return new SceneUIOcclusionPolicy
                {
                    Enabled = true,
                    RetestIntervalSeconds = 0.1f,
                    TargetRadius = 0.5f,
                    UnknownMode = SceneUIOcclusionUnknownMode.Show,
                };
            }
        }

        #endregion
    }

    /// <summary>SceneUI 根节点缩放策略。</summary>
    public struct SceneUIScalePolicy
    {
        #region 编辑器面板参数

        public SceneUIScaleMode Mode;
        public float ReferenceDistance;
        public float Factor;
        public float MinScale;
        public float MaxScale;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>固定屏幕尺寸策略。</summary>
        public static SceneUIScalePolicy Fixed
        {
            [NoGC]
            get
            {
                return new SceneUIScalePolicy
                {
                    Mode = SceneUIScaleMode.FixedScreenSize,
                    ReferenceDistance = 1f,
                    Factor = 0f,
                    MinScale = 1f,
                    MaxScale = 1f,
                };
            }
        }

        /// <summary>创建随相机深度变化的缩放策略。</summary>
        [NoGC]
        public static SceneUIScalePolicy Relative(
            float referenceDistance,
            float factor = 1f,
            float minScale = 0.25f,
            float maxScale = 2f)
        {
            float safeMin = Mathf.Max(0.0001f, Mathf.Min(minScale, maxScale));
            float safeMax = Mathf.Max(safeMin, Mathf.Max(minScale, maxScale));
            return new SceneUIScalePolicy
            {
                Mode = SceneUIScaleMode.SceneRelative,
                ReferenceDistance = Mathf.Max(referenceDistance, 0.0001f),
                Factor = factor,
                MinScale = safeMin,
                MaxScale = safeMax,
            };
        }

        #endregion
    }
}
