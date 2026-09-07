// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;

using Ember.UI;

using UnityEngine;

namespace Ember.SceneUI.Integration
{
    /// <summary>SceneUI Channel 的页面加载与运行状态。</summary>
    public enum SceneUIChannelState
    {
        Unregistered = 0,
        Loading = 1,
        Ready = 2,
        Unavailable = 3,
    }

    /// <summary>注册一个业务 SceneUI Channel 所需的显式依赖。</summary>
    public struct SceneUIChannelDescriptor
    {
        #region 编辑器面板参数

        public EUIPageDef HostPage;
        public Camera SceneCamera;
        public SceneUIViewCatalog ViewCatalog;
        public ISceneUICameraUpdateSource CameraUpdateSource;
        public bool OwnsCameraUpdateSource;
        public HostedSceneUIContextOptions ContextOptions;

        #endregion
    }

    /// <summary>不含 Engine Context 的业务 SceneUI 注册描述。</summary>
    public struct SceneUIModuleRequest
    {
        #region 编辑器面板参数

        public SceneUIViewKey ViewKey;
        public ISceneUIAnchor Anchor;
        public ISceneUIBinder Binder;
        public Vector3 WorldOffset;
        public Vector2 UiOffset;
        public SceneUIUpdatePolicy UpdatePolicy;
        public SceneUIVisibilityPolicy VisibilityPolicy;
        public SceneUIScalePolicy ScalePolicy;
        public SceneUIViewLifetimePolicy ViewLifetimePolicy;
        public float RecycleDelaySeconds;
        public int SortingPriority;
        public SceneUIViewMetrics ViewMetrics;
        public SceneUIOcclusionPolicy OcclusionPolicy;
        public bool BusinessVisible;

        #endregion
    }

    /// <summary>
    /// 业务模块级 SceneUI 句柄。ModuleGeneration 与 ChannelKey 防止旧生命周期或跨 Channel 误用。
    /// </summary>
    public readonly struct SceneUIModuleHandle : IEquatable<SceneUIModuleHandle>
    {
        #region 内部参数

        internal readonly int ModuleGeneration;
        internal readonly SceneUIHandle EngineHandle;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public SceneUIChannelKey ChannelKey { get; }
        public bool IsValid => ModuleGeneration > 0 && ChannelKey.IsValid && EngineHandle.IsValid;

        internal SceneUIModuleHandle(
            int moduleGeneration,
            SceneUIChannelKey channelKey,
            SceneUIHandle engineHandle)
        {
            ModuleGeneration = moduleGeneration;
            ChannelKey = channelKey;
            EngineHandle = engineHandle;
        }

        public bool Equals(SceneUIModuleHandle other)
        {
            return ModuleGeneration == other.ModuleGeneration
                   && ChannelKey == other.ChannelKey
                   && EngineHandle == other.EngineHandle;
        }

        public override bool Equals(object obj)
        {
            return obj is SceneUIModuleHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ModuleGeneration;
                hashCode = (hashCode * 397) ^ ChannelKey.GetHashCode();
                hashCode = (hashCode * 397) ^ EngineHandle.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(SceneUIModuleHandle left, SceneUIModuleHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SceneUIModuleHandle left, SceneUIModuleHandle right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"SceneUIModuleHandle({ModuleGeneration}:{ChannelKey.Value}:{EngineHandle.Index}:{EngineHandle.Version})";
        }

        #endregion
    }
}
