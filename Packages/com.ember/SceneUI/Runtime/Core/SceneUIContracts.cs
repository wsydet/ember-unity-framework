// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;

using UnityEngine;

namespace Ember.SceneUI
{
    /// <summary>向 SceneUI 提供当前世界坐标的最小契约。</summary>
    public interface ISceneUIAnchor
    {
        bool TryGetWorldPosition(out Vector3 worldPosition);
    }

    /// <summary>为 Context 提供当前屏幕像素可见区域。</summary>
    public interface ISceneUIVisibleRegionProvider
    {
        bool TryGetVisibleRect(out Rect visibleRect);
    }

    /// <summary>通知 Context 当前相机已经完成本帧更新。</summary>
    public interface ISceneUICameraUpdateSource
    {
        void Subscribe(Action onCameraUpdated);
        void Unsubscribe(Action onCameraUpdated);
    }

    /// <summary>执行一次后端无关的场景遮挡检测。</summary>
    public interface ISceneUIOcclusionTester
    {
        bool TryTestOcclusion(
            Camera sceneCamera,
            Vector3 targetWorldPosition,
            in SceneUIOcclusionPolicy policy,
            out bool occluded);
    }

    /// <summary>场景锚点 UI View 的空间与生命周期契约。</summary>
    public interface ISceneUIView
    {
        RectTransform RectTransform { get; }
        void ApplySpatialState(in SceneUISpatialState state);
        void SetVisible(bool visible);
        void ResetView();
    }

    /// <summary>由业务实现，负责具体 View 的内容绑定与解绑。</summary>
    public interface ISceneUIBinder
    {
        void Bind(ISceneUIView view, SceneUIHandle handle);
        void RefreshContent(ISceneUIView view);
        void Unbind(ISceneUIView view);
    }

    /// <summary>按 ViewKey 获取和回收 View 实例。</summary>
    public interface ISceneUIViewHost
    {
        bool TryAcquire(SceneUIViewKey viewKey, out ISceneUIView view);
        void Release(SceneUIViewKey viewKey, ISceneUIView view);
    }

    /// <summary>需要异步准备的 ViewHost 可实现此契约，Engine 只接受已经就绪的实例。</summary>
    public interface ISceneUIViewHostReadiness
    {
        bool IsReady { get; }
    }

    /// <summary>ViewHost 可选的无分配池诊断契约。</summary>
    public interface ISceneUIViewHostDiagnostics
    {
        int PooledViewCount { get; }
        int PoolHitCount { get; }
        int PoolMissCount { get; }
    }

    /// <summary>ViewHost 可选的分帧预热契约，由 Context 的统一刷新入口按硬预算驱动。</summary>
    public interface ISceneUIViewHostPrewarm
    {
        int PendingPrewarmCount { get; }
        int PrewarmedViewCount { get; }
        int PrewarmFailureCount { get; }
        int ProcessPrewarm(int maxInstantiateCount);
    }

    /// <summary>ViewHost 可选的 View 矩形元数据契约。</summary>
    public interface ISceneUIViewMetricsProvider
    {
        bool TryGetMetrics(SceneUIViewKey viewKey, out SceneUIViewMetrics metrics);
    }

    /// <summary>手动相机完成通知源，适用于非 Cinemachine 相机。</summary>
    public sealed class ManualSceneUICameraUpdateSource : ISceneUICameraUpdateSource
    {
        #region 内部参数

        private event Action CameraUpdated;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public void Subscribe(Action onCameraUpdated)
        {
            CameraUpdated += onCameraUpdated;
        }

        public void Unsubscribe(Action onCameraUpdated)
        {
            CameraUpdated -= onCameraUpdated;
        }

        /// <summary>在相机本帧最终变换确定后调用。</summary>
        public void NotifyCameraUpdated()
        {
            CameraUpdated?.Invoke();
        }

        #endregion
    }
}
