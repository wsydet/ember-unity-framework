// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

namespace Ember.UI
{
    /// <summary>
    /// 方块排布模式 —— 决定方块在屏幕上的出现顺序。
    /// </summary>
    public enum EUIBlockOrderPattern
    {
        AllAtOnce = 0,
        Random = 1,
        Diamond = 2,
        CornerWipe = 3,
        SideWipe = 4,
        Checkerboard = 5,
        Spiral = 6,
        Lines = 7,
        Teeth = 8,
    }

    public enum EUIBlockDirection
    {
        Top = 0, Bottom = 1, Left = 2, Right = 3,
        TopLeft = 4, TopRight = 5, BottomLeft = 6, BottomRight = 7,
        Random = 8,
    }

    /// <summary>
    /// 自动网格下每个方块的目标像素大小。限定为 2 的次方，覆盖常见分辨率。
    /// </summary>
    public enum EUIBlockSize
    {
        Size32 = 32,
        Size64 = 64,
        Size128 = 128,
        Size256 = 256,
        Size512 = 512,
    }

    public enum EUIDiamondMode { Outward = 0, Inward = 1, Random = 2 }
    public enum EUISpiralMode { Clockwise = 0, CounterClockwise = 1, Random = 2 }

    public enum EUILoadingTransitionMode
    {
        SimpleFade = 0,
        BlockSweep = 1,
    }

    /// <summary>
    /// 方块过渡预设。选一个自动填充所有动画参数，之后仍可手动微调。
    /// </summary>
    public enum EUIBlockPreset
    {
        Custom = 0,
        SideWipe = 1,
        Diamond = 5,
        Spiral = 7,
        Checkerboard = 9,
        CornerWipe = 10,
        Random = 11,
        Lines = 12,
        Teeth = 13,
    }

    /// <summary>
    /// EUIBlockPreset 每个预设的默认错开间隔。
    /// 与枚举定义放在一起，方便查阅和修改每个预设的默认值。
    /// </summary>
    public static class EUIBlockPresetDefaults
    {
        /// <summary>获取指定预设的默认错开间隔（秒）。</summary>
        public static float GetStagger(EUIBlockPreset preset)
        {
            return preset switch
            {
                EUIBlockPreset.Spiral => 0.008f,
                EUIBlockPreset.Random => 0.012f,
                EUIBlockPreset.CornerWipe => 0.02f,
                _ => 0.03f,
            };
        }
    }
}
