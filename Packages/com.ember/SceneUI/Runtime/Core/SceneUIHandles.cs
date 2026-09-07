// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;

namespace Ember.SceneUI
{
    /// <summary>
    /// SceneUI Channel 键。Channel 表示一套共享宿主页、相机、可见区域与刷新策略的运行环境。
    /// </summary>
    public readonly struct SceneUIChannelKey : IEquatable<SceneUIChannelKey>
    {
        #region 内部参数

        /// <summary>Channel 的稳定整数值。</summary>
        public readonly int Value;

        /// <summary>0 为保留值，不表示有效 Channel。</summary>
        public bool IsValid => Value != 0;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public SceneUIChannelKey(int value)
        {
            Value = value;
        }

        public bool Equals(SceneUIChannelKey other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is SceneUIChannelKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(SceneUIChannelKey left, SceneUIChannelKey right)
        {
            return left.Value == right.Value;
        }

        public static bool operator !=(SceneUIChannelKey left, SceneUIChannelKey right)
        {
            return left.Value != right.Value;
        }

        public override string ToString()
        {
            return $"SceneUIChannelKey({Value})";
        }

        #endregion
    }

    /// <summary>
    /// 场景 UI 注册句柄。Index 定位槽位，Version 防止已注销句柄命中新注册项。
    /// </summary>
    public readonly struct SceneUIHandle : IEquatable<SceneUIHandle>
    {
        #region 内部参数

        /// <summary>内部槽位索引。</summary>
        public readonly int Index;

        /// <summary>槽位版本；大于 0 时句柄才可能有效。</summary>
        public readonly int Version;

        /// <summary>句柄是否具有可校验的基本格式。</summary>
        public bool IsValid => Index >= 0 && Version > 0;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public SceneUIHandle(int index, int version)
        {
            Index = index;
            Version = version;
        }

        public bool Equals(SceneUIHandle other)
        {
            return Index == other.Index && Version == other.Version;
        }

        public override bool Equals(object obj)
        {
            return obj is SceneUIHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Index * 397) ^ Version;
            }
        }

        public static bool operator ==(SceneUIHandle left, SceneUIHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SceneUIHandle left, SceneUIHandle right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"SceneUIHandle({Index}:{Version})";
        }

        #endregion
    }

    /// <summary>
    /// 场景 UI Context 句柄。Context 注销后，其所属的全部 SceneUIHandle 同时失效。
    /// </summary>
    public readonly struct SceneUIContextHandle : IEquatable<SceneUIContextHandle>
    {
        #region 内部参数

        /// <summary>内部槽位索引。</summary>
        public readonly int Index;

        /// <summary>槽位版本。</summary>
        public readonly int Version;

        /// <summary>句柄是否具有可校验的基本格式。</summary>
        public bool IsValid => Index >= 0 && Version > 0;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public SceneUIContextHandle(int index, int version)
        {
            Index = index;
            Version = version;
        }

        public bool Equals(SceneUIContextHandle other)
        {
            return Index == other.Index && Version == other.Version;
        }

        public override bool Equals(object obj)
        {
            return obj is SceneUIContextHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Index * 397) ^ Version;
            }
        }

        public static bool operator ==(SceneUIContextHandle left, SceneUIContextHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SceneUIContextHandle left, SceneUIContextHandle right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"SceneUIContextHandle({Index}:{Version})";
        }

        #endregion
    }

    /// <summary>
    /// View 类型键。由业务定义稳定整数，不在帧循环中进行字符串查找。
    /// </summary>
    public readonly struct SceneUIViewKey : IEquatable<SceneUIViewKey>
    {
        #region 内部参数

        /// <summary>View 类型唯一整数值。</summary>
        public readonly int Value;

        /// <summary>0 为保留值，不表示有效 View。</summary>
        public bool IsValid => Value != 0;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public SceneUIViewKey(int value)
        {
            Value = value;
        }

        public bool Equals(SceneUIViewKey other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is SceneUIViewKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(SceneUIViewKey left, SceneUIViewKey right)
        {
            return left.Value == right.Value;
        }

        public static bool operator !=(SceneUIViewKey left, SceneUIViewKey right)
        {
            return left.Value != right.Value;
        }

        public override string ToString()
        {
            return $"SceneUIViewKey({Value})";
        }

        #endregion
    }
}
