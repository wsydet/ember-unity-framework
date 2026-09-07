// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using Ember.Basic;
using Ember.Core;

using UnityEngine;

namespace Ember.SceneUI
{
    /// <summary>
    /// 通用场景锚点 UI 引擎。
    /// 统一管理 Context、版本句柄、脏状态、世界坐标投影和 View 生命周期。
    /// 由业务 SceneUIModule 持有，不参与 IEmberManager 全局启动生命周期。
    /// </summary>
    public sealed class EmberSceneUIEngine : IDisposable
    {
        [Flags]
        private enum DirtyFlags
        {
            None = 0,
            Spatial = 1 << 0,
            Content = 1 << 1,
            Visibility = 1 << 2,
            All = Spatial | Content | Visibility,
        }

        private sealed class EntrySlot
        {
            public int Version = 1;
            public bool Active;
            public int ContextIndex = -1;
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
            public SceneUIOcclusionState OcclusionState;
            public bool BusinessVisible;
            public DirtyFlags Dirty;
            public bool HasWorldPosition;
            public Vector3 LastWorldPosition;
            public bool HasSpatialState;
            public bool SpatialVisible;
            public SceneUISpatialState SpatialState;
            public SceneUIInvisibleReason InvisibleReason;
            public ISceneUIView View;
            public bool ViewBound;
            public bool ViewVisible;
            public bool QueuedDirty;
            public bool QueuedDelayedRecycle;
            public bool DelayedRecycleListed;
            public double RecycleAtTime;
            public bool OcclusionTestScheduled;
            public double NextOcclusionTestTime;
            public int LastSpatialFrame = -1;
            public int LastProcessedFrame = -1;
        }

        private sealed class ContextSlot
        {
            public int Version = 1;
            public bool Active;
            public SceneUIContextDescriptor Descriptor;
            public readonly List<int> EntryIndices = new List<int>();
            public readonly List<int> DirtyEntryIndices = new List<int>();
            public readonly List<int> PolledEntryIndices = new List<int>();
            public readonly List<int> DelayedRecycleEntryIndices = new List<int>();
            public readonly List<int> SortableEntryIndices = new List<int>();
            public readonly List<int> OcclusionEntryIndices = new List<int>();
            public readonly List<int> DeferredFreeEntries = new List<int>();
            public Action UpdateCallback;
            public bool IsFlushing;
            public bool PendingRelease;
            public bool ForceRefresh = true;
            public int LastFlushFrame = -1;
            public bool HasSnapshot;
            public ContextSnapshot Snapshot;
            public bool HasVisibleRect;
            public Rect LastVisibleRect;
            public bool SortingDirty;
            public int OcclusionCursor;
        }

        private readonly struct ContextSnapshot : IEquatable<ContextSnapshot>
        {
            private readonly Matrix4x4 _worldToCamera;
            private readonly Matrix4x4 _projection;
            private readonly Rect _pixelRect;
            private readonly Rect _layerRect;
            private readonly Rect _safeArea;
            private readonly float _nearClip;
            private readonly float _farClip;
            private readonly float _canvasScale;
            private readonly int _screenWidth;
            private readonly int _screenHeight;
            private readonly int _sceneDisplay;
            private readonly int _canvasDisplay;
            private readonly Camera _uiCamera;
            private readonly RenderMode _renderMode;

            public ContextSnapshot(in SceneUIContextDescriptor descriptor)
            {
                Camera sceneCamera = descriptor.SceneCamera;
                Canvas canvas = descriptor.Canvas;
                RectTransform layerRoot = descriptor.LayerRoot;

                _worldToCamera = sceneCamera.worldToCameraMatrix;
                _projection = sceneCamera.projectionMatrix;
                _pixelRect = sceneCamera.pixelRect;
                _nearClip = sceneCamera.nearClipPlane;
                _farClip = sceneCamera.farClipPlane;
                _sceneDisplay = sceneCamera.targetDisplay;

                _renderMode = canvas.renderMode;
                _canvasDisplay = canvas.targetDisplay;
                _canvasScale = canvas.scaleFactor;
                _uiCamera = descriptor.UICamera;

                _layerRect = layerRoot.rect;
                _safeArea = Screen.safeArea;
                _screenWidth = Screen.width;
                _screenHeight = Screen.height;
            }

            public bool Equals(ContextSnapshot other)
            {
                return _worldToCamera == other._worldToCamera
                    && _projection == other._projection
                    && _pixelRect == other._pixelRect
                    && _layerRect == other._layerRect
                    && _safeArea == other._safeArea
                    && Mathf.Approximately(_nearClip, other._nearClip)
                    && Mathf.Approximately(_farClip, other._farClip)
                    && Mathf.Approximately(_canvasScale, other._canvasScale)
                    && _screenWidth == other._screenWidth
                    && _screenHeight == other._screenHeight
                    && _sceneDisplay == other._sceneDisplay
                    && _canvasDisplay == other._canvasDisplay
                    && _uiCamera == other._uiCamera
                    && _renderMode == other._renderMode;
            }
        }

        #region 内部参数

        private const string TAG = "SceneUI";

        private readonly List<ContextSlot> _contexts = new List<ContextSlot>();
        private readonly Stack<int> _freeContextIndices = new Stack<int>();
        private readonly List<EntrySlot> _entries = new List<EntrySlot>();
        private readonly Stack<int> _freeEntryIndices = new Stack<int>();

        private int _activeContextCount;
        private int _activeEntryCount;
        private int _activeViewCount;
        private long _projectedCount;
        private long _culledCount;
        private long _invalidAnchorCount;
        private long _behindCameraCount;
        private long _projectionInvalidCount;
        private long _beforeNearClipCount;
        private long _beyondFarClipCount;
        private long _outOfBoundsCount;
        private long _viewUnavailableCount;
        private long _recycledAfterDelayCount;
        private long _sortingPassCount;
        private long _occlusionTestCount;
        private long _occlusionHitCount;
        private long _occlusionFailureCount;
        private int _lastFlushFrame = -1;
        private double _lastFlushDurationMs;
        private long _lastFlushAllocatedBytes;
        private bool _isDisposed;

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static bool ValidateContextDescriptor(
            in SceneUIContextDescriptor descriptor,
            out string error)
        {
            if (!descriptor.SceneCamera)
            {
                error = "SceneCamera is missing.";
                return false;
            }

            if (!descriptor.Canvas)
            {
                error = "Canvas is missing.";
                return false;
            }

            if (!descriptor.LayerRoot)
            {
                error = "LayerRoot is missing.";
                return false;
            }

            if (descriptor.ViewHost == null)
            {
                error = "ViewHost is missing.";
                return false;
            }

            if (descriptor.ViewHost is ISceneUIViewHostReadiness readiness && !readiness.IsReady)
            {
                error = "ViewHost has not completed asynchronous preparation.";
                return false;
            }

            if (descriptor.VisibleRegionProvider == null)
            {
                error = "VisibleRegionProvider is missing.";
                return false;
            }

            if (descriptor.CameraUpdateSource == null)
            {
                error = "CameraUpdateSource is missing. Use ManualSceneUICameraUpdateSource for manual cameras.";
                return false;
            }

            if (descriptor.Canvas.renderMode != RenderMode.ScreenSpaceOverlay && !descriptor.UICamera)
            {
                error = "UICamera is required by a non-overlay Canvas.";
                return false;
            }

            if (descriptor.Canvas.renderMode != RenderMode.ScreenSpaceOverlay
                && descriptor.Canvas.worldCamera != descriptor.UICamera)
            {
                error = "Canvas.worldCamera does not match the explicit UICamera.";
                return false;
            }

            Canvas owningCanvas = descriptor.LayerRoot.GetComponentInParent<Canvas>();
            if (owningCanvas != descriptor.Canvas)
            {
                error = "LayerRoot is not owned by the provided Canvas.";
                return false;
            }

            if (descriptor.SceneCamera.targetDisplay != descriptor.Canvas.targetDisplay)
            {
                error = "SceneCamera and Canvas targetDisplay do not match.";
                return false;
            }

            if (descriptor.UICamera
                && descriptor.UICamera.targetDisplay != descriptor.Canvas.targetDisplay)
            {
                error = "UICamera and Canvas targetDisplay do not match.";
                return false;
            }

            if (descriptor.SortingMode < SceneUISortingMode.None
                || descriptor.SortingMode > SceneUISortingMode.PriorityThenDepth)
            {
                error = "SortingMode is invalid.";
                return false;
            }

            if (descriptor.PrewarmInstancesPerFlush < 0
                || (descriptor.PrewarmInstancesPerFlush > 0
                    && !(descriptor.ViewHost is ISceneUIViewHostPrewarm)))
            {
                error = "A positive PrewarmInstancesPerFlush requires an incremental-prewarm ViewHost.";
                return false;
            }

            bool hasOcclusionTester = descriptor.OcclusionTester != null;
            if (descriptor.OcclusionTestsPerFlush < 0
                || (hasOcclusionTester && descriptor.OcclusionTestsPerFlush <= 0)
                || (!hasOcclusionTester && descriptor.OcclusionTestsPerFlush != 0))
            {
                error = "OcclusionTester and a positive OcclusionTestsPerFlush budget must be configured together.";
                return false;
            }

            error = null;
            return true;
        }

        private bool TryGetContext(SceneUIContextHandle handle, out ContextSlot context)
        {
            if (!handle.IsValid || handle.Index >= _contexts.Count)
            {
                context = null;
                return false;
            }

            context = _contexts[handle.Index];
            return context.Active && context.Version == handle.Version;
        }

        private bool TryGetEntry(SceneUIHandle handle, out EntrySlot entry)
        {
            if (!handle.IsValid || handle.Index >= _entries.Count)
            {
                entry = null;
                return false;
            }

            entry = _entries[handle.Index];
            return entry.Active && entry.Version == handle.Version;
        }

        private bool TryCaptureSnapshot(ContextSlot context, out bool changed)
        {
            SceneUIContextDescriptor descriptor = context.Descriptor;
            if (!descriptor.SceneCamera || !descriptor.Canvas || !descriptor.LayerRoot)
            {
                changed = false;
                return false;
            }

            var current = new ContextSnapshot(descriptor);
            changed = !context.HasSnapshot || !context.Snapshot.Equals(current);
            context.Snapshot = current;
            context.HasSnapshot = true;
            return true;
        }

        private void QueueEntryForUpdate(int entryIndex, EntrySlot entry)
        {
            if (entry.QueuedDirty
                || entry.ContextIndex < 0
                || entry.ContextIndex >= _contexts.Count)
                return;

            ContextSlot context = _contexts[entry.ContextIndex];
            if (!context.Active || context.PendingRelease)
                return;

            entry.QueuedDirty = true;
            context.DirtyEntryIndices.Add(entryIndex);
        }

        private static void ProcessViewHostPrewarm(ContextSlot context)
        {
            int budget = context.Descriptor.PrewarmInstancesPerFlush;
            if (budget <= 0
                || !(context.Descriptor.ViewHost is ISceneUIViewHostPrewarm prewarmHost)
                || prewarmHost.PendingPrewarmCount <= 0)
                return;

            try
            {
                prewarmHost.ProcessPrewarm(budget);
            }
            catch (Exception ex)
            {
                EmberDebug.LogError(TAG, $"ViewHost incremental prewarm failed: {ex}");
            }
        }

        private void MarkContextOcclusionDirty(ContextSlot context)
        {
            for (int i = 0; i < context.OcclusionEntryIndices.Count; i++)
            {
                int entryIndex = context.OcclusionEntryIndices[i];
                if (entryIndex < 0 || entryIndex >= _entries.Count)
                    continue;

                EntrySlot entry = _entries[entryIndex];
                if (entry.Active && entry.OcclusionPolicy.Enabled)
                    entry.NextOcclusionTestTime = 0d;
            }
        }

        private void ScheduleOcclusionTests(int contextIndex, ContextSlot context)
        {
            int count = context.OcclusionEntryIndices.Count;
            int budget = context.Descriptor.OcclusionTestsPerFlush;
            if (count == 0 || budget <= 0 || context.Descriptor.OcclusionTester == null)
                return;

            if (context.OcclusionCursor < 0 || context.OcclusionCursor >= count)
                context.OcclusionCursor = 0;

            double now = Time.realtimeSinceStartupAsDouble;
            int scanned = 0;
            int scheduled = 0;
            while (scanned < count && scheduled < budget)
            {
                if (context.OcclusionCursor >= count)
                    context.OcclusionCursor = 0;

                int entryIndex = context.OcclusionEntryIndices[context.OcclusionCursor];
                context.OcclusionCursor++;
                scanned++;

                if (entryIndex < 0 || entryIndex >= _entries.Count)
                    continue;

                EntrySlot entry = _entries[entryIndex];
                if (!entry.Active
                    || entry.ContextIndex != contextIndex
                    || !entry.OcclusionPolicy.Enabled
                    || !entry.BusinessVisible
                    || entry.OcclusionTestScheduled
                    || now < entry.NextOcclusionTestTime)
                    continue;

                entry.OcclusionTestScheduled = true;
                entry.Dirty |= DirtyFlags.Visibility;
                QueueEntryForUpdate(entryIndex, entry);
                scheduled++;
            }
        }

        private void FinishOcclusionScheduleWithoutTest(EntrySlot entry)
        {
            if (!entry.OcclusionTestScheduled)
                return;

            entry.OcclusionTestScheduled = false;
            entry.NextOcclusionTestTime = Time.realtimeSinceStartupAsDouble
                + entry.OcclusionPolicy.RetestIntervalSeconds;
        }

        private void EvaluateScheduledOcclusion(
            EntrySlot entry,
            ContextSlot context,
            Vector3 targetWorldPosition)
        {
            if (!entry.OcclusionTestScheduled)
                return;

            entry.OcclusionTestScheduled = false;
            entry.NextOcclusionTestTime = Time.realtimeSinceStartupAsDouble
                + entry.OcclusionPolicy.RetestIntervalSeconds;
            if (!entry.HasSpatialState || !entry.SpatialVisible)
                return;

            bool tested;
            bool occluded;
            try
            {
                tested = context.Descriptor.OcclusionTester.TryTestOcclusion(
                    context.Descriptor.SceneCamera,
                    targetWorldPosition,
                    entry.OcclusionPolicy,
                    out occluded);
            }
            catch (Exception ex)
            {
                _occlusionFailureCount++;
                EmberDebug.LogError(TAG, $"Occlusion tester failed: {ex}");
                return;
            }

            _occlusionTestCount++;
            if (!tested)
            {
                _occlusionFailureCount++;
                return;
            }

            if (occluded)
                _occlusionHitCount++;

            entry.OcclusionState = occluded
                ? SceneUIOcclusionState.Occluded
                : SceneUIOcclusionState.Visible;
        }

        private static bool IsOcclusionVisible(EntrySlot entry)
        {
            if (!entry.OcclusionPolicy.Enabled
                || entry.OcclusionState == SceneUIOcclusionState.Disabled
                || entry.OcclusionState == SceneUIOcclusionState.Visible)
                return true;

            return entry.OcclusionState == SceneUIOcclusionState.Unknown
                && entry.OcclusionPolicy.UnknownMode == SceneUIOcclusionUnknownMode.Show;
        }

        private void ProcessEntry(
            int entryIndex,
            int contextIndex,
            EntrySlot entry,
            ContextSlot context,
            Rect visibleRect,
            int frame,
            bool refreshAll)
        {
            if (!context.Active
                || context.PendingRelease
                || !entry.Active
                || entry.ContextIndex != contextIndex
                || entry.LastProcessedFrame == frame)
                return;

            entry.LastProcessedFrame = frame;
            bool spatialDirty = (entry.Dirty & DirtyFlags.Spatial) != 0;
            bool pollPosition = entry.UpdatePolicy == SceneUIUpdatePolicy.PollPosition;
            bool mustReadAnchor = refreshAll || spatialDirty || pollPosition || !entry.HasWorldPosition;
            bool spatialChanged = false;

            if (mustReadAnchor)
            {
                if (!entry.Anchor.TryGetWorldPosition(out Vector3 anchorPosition))
                {
                    FinishOcclusionScheduleWithoutTest(entry);
                    SetSpatialInvisible(entry, SceneUIInvisibleReason.AnchorInvalid);
                    entry.Dirty &= ~DirtyFlags.Spatial;
                    ApplyEntryState(entryIndex, entry, context, false);
                    return;
                }

                bool positionChanged = !entry.HasWorldPosition || entry.LastWorldPosition != anchorPosition;
                entry.LastWorldPosition = anchorPosition;
                entry.HasWorldPosition = true;

                if (refreshAll || spatialDirty || positionChanged || !entry.HasSpatialState)
                {
                    spatialChanged = true;
                    RefreshSpatialState(entry, context, anchorPosition, visibleRect, frame);
                    entry.Dirty &= ~DirtyFlags.Spatial;
                }

                if (positionChanged && entry.OcclusionPolicy.Enabled && !entry.OcclusionTestScheduled)
                    entry.NextOcclusionTestTime = 0d;
            }


            if (entry.OcclusionTestScheduled)
            {
                if (entry.HasWorldPosition)
                    EvaluateScheduledOcclusion(
                        entry,
                        context,
                        entry.LastWorldPosition + entry.WorldOffset);
                else
                    FinishOcclusionScheduleWithoutTest(entry);
            }

            ApplyEntryState(entryIndex, entry, context, spatialChanged);
        }

        private bool RefreshSpatialState(
            EntrySlot entry,
            ContextSlot context,
            Vector3 anchorPosition,
            Rect visibleRect,
            int frame)
        {
            if (entry.LastSpatialFrame == frame)
                return entry.HasSpatialState;

            entry.LastSpatialFrame = frame;
            Vector3 worldPosition = anchorPosition + entry.WorldOffset;
            if (!SceneUIWorldProjector.TryProject(
                    context.Descriptor.SceneCamera,
                    worldPosition,
                    out SceneUIProjectionResult projection))
            {
                SetSpatialInvisible(entry, SceneUIInvisibleReason.ProjectionInvalid);
                return false;
            }

            _projectedCount++;
            float scale = SceneUIScaleEvaluator.Evaluate(entry.ScalePolicy, projection.Depth);
            bool useViewBounds = entry.VisibilityPolicy.BoundsMode != SceneUIBoundsMode.Point;
            Vector2 initialAnchoredPosition = default;
            SceneUIVisibilityResult visibility;
            if (useViewBounds)
            {
                if (!SceneUICanvasMapper.TryMap(
                        context.Descriptor.Canvas,
                        context.Descriptor.LayerRoot,
                        context.Descriptor.UICamera,
                        projection.ScreenPosition,
                        entry.UiOffset,
                        out initialAnchoredPosition)
                    || !SceneUICanvasMapper.TryGetScreenRect(
                        context.Descriptor.Canvas,
                        context.Descriptor.LayerRoot,
                        context.Descriptor.UICamera,
                        initialAnchoredPosition,
                        entry.ViewMetrics,
                        scale,
                        out Vector2 boundsAnchorScreenPosition,
                        out Rect viewScreenRect))
                {
                    SetSpatialInvisible(entry, SceneUIInvisibleReason.ProjectionInvalid);
                    return false;
                }

                visibility = SceneUIVisibilityEvaluator.Evaluate(
                    projection,
                    context.Descriptor.SceneCamera,
                    visibleRect,
                    entry.VisibilityPolicy,
                    boundsAnchorScreenPosition,
                    viewScreenRect);
            }
            else
            {
                visibility = SceneUIVisibilityEvaluator.Evaluate(
                    projection,
                    context.Descriptor.SceneCamera,
                    visibleRect,
                    entry.VisibilityPolicy);
            }

            if (!visibility.Visible)
            {
                _culledCount++;
                SetSpatialInvisible(entry, visibility.InvisibleReason);
                return false;
            }

            Vector2 anchoredPosition;
            if (useViewBounds && visibility.ScreenPosition == projection.ScreenPosition)
            {
                anchoredPosition = initialAnchoredPosition;
            }
            else if (!SceneUICanvasMapper.TryMap(
                         context.Descriptor.Canvas,
                         context.Descriptor.LayerRoot,
                         context.Descriptor.UICamera,
                         visibility.ScreenPosition,
                         entry.UiOffset,
                         out anchoredPosition))
            {
                SetSpatialInvisible(entry, SceneUIInvisibleReason.ProjectionInvalid);
                return false;
            }

            bool depthChanged = !entry.HasSpatialState
                || !Mathf.Approximately(entry.SpatialState.Depth, projection.Depth);
            entry.SpatialState = new SceneUISpatialState(
                anchoredPosition,
                scale,
                projection.Depth,
                visibility.IsOnScreen,
                visibility.OffscreenDirection);
            entry.HasSpatialState = true;
            entry.SpatialVisible = true;
            entry.InvisibleReason = SceneUIInvisibleReason.None;
            if (depthChanged
                && context.Descriptor.SortingMode == SceneUISortingMode.PriorityThenDepth)
                context.SortingDirty = true;
            return true;
        }

        private void SetSpatialInvisible(EntrySlot entry, SceneUIInvisibleReason reason)
        {
            entry.HasSpatialState = false;
            entry.SpatialVisible = false;
            entry.InvisibleReason = reason;

            switch (reason)
            {
                case SceneUIInvisibleReason.AnchorInvalid:
                    _invalidAnchorCount++;
                    break;
                case SceneUIInvisibleReason.ProjectionInvalid:
                    _projectionInvalidCount++;
                    break;
                case SceneUIInvisibleReason.BehindCamera:
                    _behindCameraCount++;
                    break;
                case SceneUIInvisibleReason.BeforeNearClip:
                    _beforeNearClipCount++;
                    break;
                case SceneUIInvisibleReason.BeyondFarClip:
                    _beyondFarClipCount++;
                    break;
                case SceneUIInvisibleReason.OutOfBounds:
                    _outOfBoundsCount++;
                    break;
            }
        }

        private void ApplyEntryState(int entryIndex, EntrySlot entry, ContextSlot context, bool spatialChanged)
        {
            if (!entry.Active) return;

            bool occlusionVisible = IsOcclusionVisible(entry);
            bool shouldRender = context.Descriptor.Visible
                && entry.BusinessVisible
                && entry.HasSpatialState
                && entry.SpatialVisible
                && occlusionVisible;

            if (!shouldRender)
            {
                if (!context.Descriptor.Visible)
                    entry.InvisibleReason = SceneUIInvisibleReason.ContextHidden;
                else if (!entry.BusinessVisible)
                    entry.InvisibleReason = SceneUIInvisibleReason.BusinessHidden;
                else if (entry.HasSpatialState && entry.SpatialVisible && !occlusionVisible)
                {
                    entry.InvisibleReason = entry.OcclusionState == SceneUIOcclusionState.Occluded
                        ? SceneUIInvisibleReason.Occluded
                        : SceneUIInvisibleReason.OcclusionUnknown;
                }

                TransitionToInvisible(entryIndex, entry, context);
                entry.Dirty &= ~DirtyFlags.Visibility;
                return;
            }

            if (!EnsureView(entryIndex, entry, context))
            {
                entry.InvisibleReason = SceneUIInvisibleReason.ViewUnavailable;
                _viewUnavailableCount++;
                return;
            }

            CancelDelayedRecycle(entry);

            if (spatialChanged)
                entry.View.ApplySpatialState(entry.SpatialState);

            if (!entry.Active || context.PendingRelease || entry.View == null)
                return;

            if ((entry.Dirty & DirtyFlags.Content) != 0)
            {
                entry.Binder?.RefreshContent(entry.View);
                if (!entry.Active || context.PendingRelease || entry.View == null)
                    return;

                entry.Dirty &= ~DirtyFlags.Content;
            }

            if (!entry.ViewVisible)
            {
                ISceneUIView view = entry.View;
                view.SetVisible(true);
                if (!entry.Active || context.PendingRelease || entry.View != view)
                    return;

                entry.ViewVisible = true;
                context.SortingDirty = true;
            }

            entry.InvisibleReason = SceneUIInvisibleReason.None;
            entry.Dirty &= ~DirtyFlags.Visibility;
        }

        private bool EnsureView(int entryIndex, EntrySlot entry, ContextSlot context)
        {
            if (entry.View != null)
                return true;

            if (!context.Descriptor.ViewHost.TryAcquire(entry.ViewKey, out ISceneUIView view)
                || view == null
                || !view.RectTransform)
                return false;

            entry.View = view;
            _activeViewCount++;
            context.SortingDirty = true;

            RectTransform rectTransform = view.RectTransform;
            Vector2 expectedAnchor = context.Descriptor.LayerRoot.pivot;
            rectTransform.anchorMin = expectedAnchor;
            rectTransform.anchorMax = expectedAnchor;

            try
            {
                entry.ViewBound = true;
                entry.Binder?.Bind(view, new SceneUIHandle(entryIndex, entry.Version));
                if (!entry.Active || context.PendingRelease || entry.View != view)
                    return false;

                view.ApplySpatialState(entry.SpatialState);
                if (!entry.Active || context.PendingRelease || entry.View != view)
                    return false;

                entry.Binder?.RefreshContent(view);
                if (!entry.Active || context.PendingRelease || entry.View != view)
                    return false;

                entry.Dirty &= ~DirtyFlags.Content;
                return true;
            }
            catch (Exception ex)
            {
                EmberDebug.LogError(TAG, $"View bind failed for {entry.ViewKey}: {ex}");
                ReleaseView(entry, context);
                return false;
            }
        }

        private void TransitionToInvisible(int entryIndex, EntrySlot entry, ContextSlot context)
        {
            if (entry.View == null)
            {
                CancelDelayedRecycle(entry);
                return;
            }

            if (entry.ViewVisible)
            {
                entry.View.SetVisible(false);
                entry.ViewVisible = false;
                context.SortingDirty = true;
            }

            switch (entry.ViewLifetimePolicy)
            {
                case SceneUIViewLifetimePolicy.RecycleWhenInvisible:
                    CancelDelayedRecycle(entry);
                    ReleaseView(entry, context);
                    break;

                case SceneUIViewLifetimePolicy.RecycleAfterDelay:
                    QueueDelayedRecycle(entryIndex, entry, context);
                    break;

                default:
                    CancelDelayedRecycle(entry);
                    break;
            }
        }

        private static void CancelDelayedRecycle(EntrySlot entry)
        {
            entry.QueuedDelayedRecycle = false;
            entry.RecycleAtTime = 0d;
        }

        private void QueueDelayedRecycle(int entryIndex, EntrySlot entry, ContextSlot context)
        {
            if (entry.RecycleDelaySeconds <= 0f)
            {
                CancelDelayedRecycle(entry);
                ReleaseView(entry, context);
                _recycledAfterDelayCount++;
                return;
            }

            entry.QueuedDelayedRecycle = true;
            entry.RecycleAtTime = Time.realtimeSinceStartupAsDouble + entry.RecycleDelaySeconds;
            if (!entry.DelayedRecycleListed)
            {
                entry.DelayedRecycleListed = true;
                context.DelayedRecycleEntryIndices.Add(entryIndex);
            }
        }

        private void ProcessDelayedRecycles(ContextSlot context)
        {
            if (context.DelayedRecycleEntryIndices.Count == 0)
                return;

            double now = Time.realtimeSinceStartupAsDouble;
            for (int i = context.DelayedRecycleEntryIndices.Count - 1; i >= 0; i--)
            {
                int entryIndex = context.DelayedRecycleEntryIndices[i];
                if (entryIndex < 0 || entryIndex >= _entries.Count)
                {
                    context.DelayedRecycleEntryIndices.RemoveAt(i);
                    continue;
                }

                EntrySlot entry = _entries[entryIndex];
                if (!entry.Active
                    || entry.ContextIndex < 0
                    || !entry.QueuedDelayedRecycle
                    || entry.View == null)
                {
                    CancelDelayedRecycle(entry);
                    entry.DelayedRecycleListed = false;
                    context.DelayedRecycleEntryIndices.RemoveAt(i);
                    continue;
                }

                if (now < entry.RecycleAtTime)
                    continue;

                CancelDelayedRecycle(entry);
                ReleaseView(entry, context);
                entry.DelayedRecycleListed = false;
                context.DelayedRecycleEntryIndices.RemoveAt(i);
                _recycledAfterDelayCount++;
            }
        }

        private void ApplyViewSorting(ContextSlot context)
        {
            if (!context.SortingDirty)
                return;

            context.SortingDirty = false;
            if (context.Descriptor.SortingMode == SceneUISortingMode.None)
                return;

            List<int> sortable = context.SortableEntryIndices;
            sortable.Clear();
            for (int i = 0; i < context.EntryIndices.Count; i++)
            {
                int entryIndex = context.EntryIndices[i];
                if (entryIndex < 0 || entryIndex >= _entries.Count)
                    continue;

                EntrySlot entry = _entries[entryIndex];
                if (entry.Active
                    && entry.ViewVisible
                    && entry.View != null
                    && entry.View.RectTransform)
                    sortable.Add(entryIndex);
            }

            HeapSort(sortable, context.Descriptor.SortingMode);

            for (int i = 0; i < sortable.Count; i++)
            {
                RectTransform rectTransform = _entries[sortable[i]].View.RectTransform;
                if (rectTransform)
                    rectTransform.SetAsLastSibling();
            }

            _sortingPassCount++;
        }

        private void HeapSort(List<int> entries, SceneUISortingMode sortingMode)
        {
            int count = entries.Count;
            for (int root = count / 2 - 1; root >= 0; root--)
                SiftDown(entries, root, count, sortingMode);

            for (int end = count - 1; end > 0; end--)
            {
                int value = entries[0];
                entries[0] = entries[end];
                entries[end] = value;
                SiftDown(entries, 0, end, sortingMode);
            }
        }

        private void SiftDown(
            List<int> entries,
            int root,
            int count,
            SceneUISortingMode sortingMode)
        {
            while (true)
            {
                int child = root * 2 + 1;
                if (child >= count)
                    return;

                int larger = child;
                int right = child + 1;
                if (right < count
                    && CompareForSorting(entries[right], entries[child], sortingMode) > 0)
                    larger = right;

                if (CompareForSorting(entries[larger], entries[root], sortingMode) <= 0)
                    return;

                int value = entries[root];
                entries[root] = entries[larger];
                entries[larger] = value;
                root = larger;
            }
        }

        private int CompareForSorting(int leftIndex, int rightIndex, SceneUISortingMode sortingMode)
        {
            EntrySlot left = _entries[leftIndex];
            EntrySlot right = _entries[rightIndex];
            int priorityComparison = left.SortingPriority.CompareTo(right.SortingPriority);
            if (priorityComparison != 0)
                return priorityComparison;

            if (sortingMode == SceneUISortingMode.PriorityThenDepth)
            {
                // 远处先绘制，近处后绘制，从而让近处目标位于更上层。
                int depthComparison = right.SpatialState.Depth.CompareTo(left.SpatialState.Depth);
                if (depthComparison != 0)
                    return depthComparison;
            }

            return leftIndex.CompareTo(rightIndex);
        }

        private void ReleaseView(EntrySlot entry, ContextSlot context)
        {
            ISceneUIView view = entry.View;
            if (view == null) return;

            ISceneUIViewHost viewHost = context.Descriptor.ViewHost;
            ISceneUIBinder binder = entry.Binder;
            bool wasBound = entry.ViewBound;
            bool wasVisible = entry.ViewVisible;

            entry.View = null;
            entry.ViewBound = false;
            entry.ViewVisible = false;
            CancelDelayedRecycle(entry);
            context.SortingDirty = true;
            _activeViewCount = Mathf.Max(0, _activeViewCount - 1);

            try
            {
                if (wasVisible)
                    view.SetVisible(false);
            }
            catch (Exception ex)
            {
                EmberDebug.LogError(TAG, $"View hide failed for {entry.ViewKey}: {ex}");
            }

            if (wasBound)
            {
                try
                {
                    binder?.Unbind(view);
                }
                catch (Exception ex)
                {
                    EmberDebug.LogError(TAG, $"View unbind failed for {entry.ViewKey}: {ex}");
                }
            }

            try
            {
                viewHost?.Release(entry.ViewKey, view);
            }
            catch (Exception ex)
            {
                EmberDebug.LogError(TAG, $"View host release failed for {entry.ViewKey}: {ex}");
            }
        }

        private void InvalidateEntry(int entryIndex, EntrySlot entry, ContextSlot context, bool deferFree)
        {
            entry.Active = false;
            entry.Version = NextVersion(entry.Version);
            _activeEntryCount = Mathf.Max(0, _activeEntryCount - 1);

            if (deferFree)
            {
                context.DeferredFreeEntries.Add(entryIndex);
                return;
            }

            FinalizeEntryRelease(entryIndex, entry, context);
        }

        private void FinalizeEntryRelease(int entryIndex, EntrySlot entry, ContextSlot context)
        {
            ReleaseView(entry, context);
            entry.ContextIndex = -1;
            entry.Anchor = null;
            entry.Binder = null;
            entry.Dirty = DirtyFlags.None;
            entry.HasWorldPosition = false;
            entry.HasSpatialState = false;
            entry.SpatialVisible = false;
            entry.QueuedDirty = false;
            CancelDelayedRecycle(entry);
            entry.DelayedRecycleListed = false;
            entry.OcclusionPolicy = SceneUIOcclusionPolicy.Disabled;
            entry.OcclusionState = SceneUIOcclusionState.Disabled;
            entry.OcclusionTestScheduled = false;
            entry.NextOcclusionTestTime = 0d;
            entry.LastProcessedFrame = -1;
            _freeEntryIndices.Push(entryIndex);
        }

        private void RemoveInactiveEntryIndices(List<int> indices)
        {
            for (int i = indices.Count - 1; i >= 0; i--)
            {
                int entryIndex = indices[i];
                if (entryIndex < 0 || entryIndex >= _entries.Count || !_entries[entryIndex].Active)
                    indices.RemoveAt(i);
            }
        }

        private void CompactContextEntryLists(ContextSlot context)
        {
            RemoveInactiveEntryIndices(context.EntryIndices);
            RemoveInactiveEntryIndices(context.DirtyEntryIndices);
            RemoveInactiveEntryIndices(context.PolledEntryIndices);
            RemoveInactiveEntryIndices(context.DelayedRecycleEntryIndices);
            RemoveInactiveEntryIndices(context.OcclusionEntryIndices);
        }

        private void ReserveBatchCapacity(ContextSlot context, int additionalCount)
        {
            if (additionalCount <= 0)
                return;

            int contextCapacity = context.EntryIndices.Count + additionalCount;
            context.EntryIndices.Capacity = Mathf.Max(context.EntryIndices.Capacity, contextCapacity);
            context.DirtyEntryIndices.Capacity = Mathf.Max(
                context.DirtyEntryIndices.Capacity,
                context.DirtyEntryIndices.Count + additionalCount);
            context.PolledEntryIndices.Capacity = Mathf.Max(
                context.PolledEntryIndices.Capacity,
                contextCapacity);
            context.SortableEntryIndices.Capacity = Mathf.Max(
                context.SortableEntryIndices.Capacity,
                contextCapacity);
            context.OcclusionEntryIndices.Capacity = Mathf.Max(
                context.OcclusionEntryIndices.Capacity,
                contextCapacity);
            context.DeferredFreeEntries.Capacity = Mathf.Max(
                context.DeferredFreeEntries.Capacity,
                contextCapacity);

            int newSlotCount = Mathf.Max(0, additionalCount - _freeEntryIndices.Count);
            _entries.Capacity = Mathf.Max(_entries.Capacity, _entries.Count + newSlotCount);
        }

        private void FinishContextFlush(int contextIndex, ContextSlot context)
        {
            context.IsFlushing = false;

            if (context.PendingRelease)
            {
                FinalizeContextRelease(contextIndex, context);
                return;
            }

            if (context.DeferredFreeEntries.Count == 0)
                return;

            for (int i = 0; i < context.DeferredFreeEntries.Count; i++)
            {
                int entryIndex = context.DeferredFreeEntries[i];
                FinalizeEntryRelease(entryIndex, _entries[entryIndex], context);
            }

            context.DeferredFreeEntries.Clear();
            CompactContextEntryLists(context);
        }

        private void FinalizeContextRelease(int contextIndex, ContextSlot context)
        {
            if (context.UpdateCallback != null && context.Descriptor.CameraUpdateSource != null)
            {
                try
                {
                    context.Descriptor.CameraUpdateSource.Unsubscribe(context.UpdateCallback);
                }
                catch (Exception ex)
                {
                    EmberDebug.LogError(TAG, $"Camera update source unsubscribe failed: {ex}");
                }

                context.UpdateCallback = null;
            }

            for (int i = 0; i < context.EntryIndices.Count; i++)
            {
                int entryIndex = context.EntryIndices[i];
                if (entryIndex < 0 || entryIndex >= _entries.Count)
                    continue;

                EntrySlot entry = _entries[entryIndex];
                if (entry.Active)
                    InvalidateEntry(entryIndex, entry, context, false);
            }

            for (int i = 0; i < context.DeferredFreeEntries.Count; i++)
            {
                int entryIndex = context.DeferredFreeEntries[i];
                FinalizeEntryRelease(entryIndex, _entries[entryIndex], context);
            }

            context.DeferredFreeEntries.Clear();
            context.EntryIndices.Clear();
            context.DirtyEntryIndices.Clear();
            context.PolledEntryIndices.Clear();
            context.DelayedRecycleEntryIndices.Clear();
            context.SortableEntryIndices.Clear();
            context.OcclusionEntryIndices.Clear();

            if (context.Descriptor.OwnsViewHost && context.Descriptor.ViewHost is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    EmberDebug.LogError(TAG, $"Owned ViewHost dispose failed: {ex}");
                }
            }

            context.Descriptor = default;
            context.Active = false;
            context.PendingRelease = false;
            context.ForceRefresh = true;
            context.HasSnapshot = false;
            context.HasVisibleRect = false;
            context.SortingDirty = false;
            context.OcclusionCursor = 0;
            context.LastFlushFrame = -1;
            context.Version = NextVersion(context.Version);
            _freeContextIndices.Push(contextIndex);
            _activeContextCount = Mathf.Max(0, _activeContextCount - 1);
        }

        private void ClearAll()
        {
            for (int i = 0; i < _contexts.Count; i++)
            {
                ContextSlot context = _contexts[i];
                if (!context.Active) continue;

                context.Active = false;
                FinalizeContextRelease(i, context);
            }

            _contexts.Clear();
            _entries.Clear();
            _freeContextIndices.Clear();
            _freeEntryIndices.Clear();
            _activeContextCount = 0;
            _activeEntryCount = 0;
            _activeViewCount = 0;
            _projectedCount = 0;
            _culledCount = 0;
            _invalidAnchorCount = 0;
            _behindCameraCount = 0;
            _projectionInvalidCount = 0;
            _beforeNearClipCount = 0;
            _beyondFarClipCount = 0;
            _outOfBoundsCount = 0;
            _viewUnavailableCount = 0;
            _recycledAfterDelayCount = 0;
            _sortingPassCount = 0;
            _occlusionTestCount = 0;
            _occlusionHitCount = 0;
            _occlusionFailureCount = 0;
            _lastFlushFrame = -1;
            _lastFlushDurationMs = 0d;
            _lastFlushAllocatedBytes = 0L;
        }

        private static int NextVersion(int current)
        {
            return current == int.MaxValue ? 1 : current + 1;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>当前累计诊断统计。</summary>
        public SceneUIDiagnostics Diagnostics
        {
            get
            {
                int pooledViewCount = 0;
                int poolHitCount = 0;
                int poolMissCount = 0;
                int pendingPrewarmCount = 0;
                int prewarmedViewCount = 0;
                int prewarmFailureCount = 0;
                int pendingDelayedRecycleCount = 0;
                for (int i = 0; i < _contexts.Count; i++)
                {
                    ContextSlot context = _contexts[i];
                    if (!context.Active)
                        continue;

                    if (context.Descriptor.ViewHost is ISceneUIViewHostDiagnostics diagnostics)
                    {
                        pooledViewCount += diagnostics.PooledViewCount;
                        poolHitCount += diagnostics.PoolHitCount;
                        poolMissCount += diagnostics.PoolMissCount;
                    }

                    if (context.Descriptor.ViewHost is ISceneUIViewHostPrewarm prewarm)
                    {
                        pendingPrewarmCount += prewarm.PendingPrewarmCount;
                        prewarmedViewCount += prewarm.PrewarmedViewCount;
                        prewarmFailureCount += prewarm.PrewarmFailureCount;
                    }
                }

                for (int i = 0; i < _entries.Count; i++)
                {
                    EntrySlot entry = _entries[i];
                    if (entry.Active && entry.QueuedDelayedRecycle)
                        pendingDelayedRecycleCount++;
                }

                return new SceneUIDiagnostics(
                    _activeContextCount,
                    _activeEntryCount,
                    _activeViewCount,
                    pooledViewCount,
                    poolHitCount,
                    poolMissCount,
                    pendingPrewarmCount,
                    prewarmedViewCount,
                    prewarmFailureCount,
                    _projectedCount,
                    _culledCount,
                    _invalidAnchorCount,
                    _behindCameraCount,
                    _lastFlushFrame,
                    _lastFlushDurationMs,
                    _lastFlushAllocatedBytes,
                    pendingDelayedRecycleCount,
                    _recycledAfterDelayCount,
                    _sortingPassCount,
                    _projectionInvalidCount,
                    _beforeNearClipCount,
                    _beyondFarClipCount,
                    _outOfBoundsCount,
                    _viewUnavailableCount,
                    _occlusionTestCount,
                    _occlusionHitCount,
                    _occlusionFailureCount);
            }
        }

        /// <summary>引擎是否已经释放。释放后的句柄和注册请求全部无效。</summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>释放全部 Context、Entry、ViewHost 与相机更新订阅。可安全重复调用。</summary>
        [NoGC]
        public void Dispose()
        {
            if (_isDisposed) return;

            _isDisposed = true;
            ClearAll();
            EmberDebug.LogCleanup(TAG, "EmberSceneUIEngine disposed.");
        }

        /// <summary>注册一组显式投影环境。</summary>
        [HasGC]
        public SceneUIContextHandle RegisterContext(in SceneUIContextDescriptor descriptor)
        {
            if (_isDisposed)
            {
                EmberDebug.LogError(TAG, "RegisterContext failed: engine is disposed.");
                return default;
            }

            if (!ValidateContextDescriptor(descriptor, out string error))
            {
                EmberDebug.LogError(TAG, $"RegisterContext failed: {error}");
                return default;
            }

            int index;
            ContextSlot context;
            if (_freeContextIndices.Count > 0)
            {
                index = _freeContextIndices.Pop();
                context = _contexts[index];
            }
            else
            {
                index = _contexts.Count;
                context = new ContextSlot();
                _contexts.Add(context);
            }

            context.Active = true;
            context.Descriptor = descriptor;
            context.EntryIndices.Clear();
            context.DirtyEntryIndices.Clear();
            context.PolledEntryIndices.Clear();
            context.DelayedRecycleEntryIndices.Clear();
            context.SortableEntryIndices.Clear();
            context.OcclusionEntryIndices.Clear();
            context.DeferredFreeEntries.Clear();
            context.PendingRelease = false;
            context.ForceRefresh = true;
            context.LastFlushFrame = -1;
            context.HasSnapshot = false;
            context.HasVisibleRect = false;
            context.SortingDirty = false;
            context.OcclusionCursor = 0;

            var handle = new SceneUIContextHandle(index, context.Version);
            context.UpdateCallback = () => FlushContext(handle);
            try
            {
                descriptor.CameraUpdateSource.Subscribe(context.UpdateCallback);
            }
            catch (Exception ex)
            {
                try
                {
                    descriptor.CameraUpdateSource.Unsubscribe(context.UpdateCallback);
                }
                catch
                {
                    // 回滚时递增句柄版本，残留回调也无法命中新 Context。
                }

                context.UpdateCallback = null;
                context.Descriptor = default;
                context.Active = false;
                context.Version = NextVersion(context.Version);
                _freeContextIndices.Push(index);
                EmberDebug.LogError(TAG, $"RegisterContext failed while subscribing update source: {ex}");
                return default;
            }

            _activeContextCount++;
            return handle;
        }

        /// <summary>注册一个 SceneUI Entry；实际 View 在首次可渲染时获取。</summary>
        [HasGC]
        public SceneUIHandle Register(in SceneUIRequest request)
        {
            if (!TryGetContext(request.Context, out ContextSlot context))
            {
                EmberDebug.LogError(TAG, "Register failed: invalid Context handle.");
                return default;
            }

            if (!request.ViewKey.IsValid || request.Anchor == null)
            {
                EmberDebug.LogError(TAG, "Register failed: ViewKey or Anchor is invalid.");
                return default;
            }

            if (!SceneUIMath.IsFinite(request.WorldOffset) || !SceneUIMath.IsFinite(request.UiOffset))
            {
                EmberDebug.LogError(TAG, "Register failed: WorldOffset or UiOffset is not finite.");
                return default;
            }

            if (request.ViewLifetimePolicy < SceneUIViewLifetimePolicy.KeepWhileRegistered
                || request.ViewLifetimePolicy > SceneUIViewLifetimePolicy.RecycleAfterDelay
                || !SceneUIMath.IsFinite(request.RecycleDelaySeconds))
            {
                EmberDebug.LogError(TAG, "Register failed: View lifetime policy or recycle delay is invalid.");
                return default;
            }

            if (request.VisibilityPolicy.BoundsMode < SceneUIBoundsMode.Point
                || request.VisibilityPolicy.BoundsMode > SceneUIBoundsMode.FullRect)
            {
                EmberDebug.LogError(TAG, "Register failed: BoundsMode is invalid.");
                return default;
            }

            if (request.OcclusionPolicy.Enabled
                && (request.OcclusionPolicy.UnknownMode < SceneUIOcclusionUnknownMode.Show
                    || request.OcclusionPolicy.UnknownMode > SceneUIOcclusionUnknownMode.Hide
                    || !SceneUIMath.IsFinite(request.OcclusionPolicy.RetestIntervalSeconds)
                    || !SceneUIMath.IsFinite(request.OcclusionPolicy.TargetRadius)
                    || context.Descriptor.OcclusionTester == null
                    || context.Descriptor.OcclusionTestsPerFlush <= 0))
            {
                EmberDebug.LogError(
                    TAG,
                    "Register failed: enabled occlusion requires a valid policy and Context occlusion tester/budget.");
                return default;
            }

            if (request.ScalePolicy.Mode == SceneUIScaleMode.SceneRelative
                && (!SceneUIMath.IsFinite(request.ScalePolicy.ReferenceDistance)
                    || !SceneUIMath.IsFinite(request.ScalePolicy.Factor)
                    || !SceneUIMath.IsFinite(request.ScalePolicy.MinScale)
                    || !SceneUIMath.IsFinite(request.ScalePolicy.MaxScale)))
            {
                EmberDebug.LogError(TAG, "Register failed: ScalePolicy contains a non-finite value.");
                return default;
            }

            SceneUIViewMetrics viewMetrics = request.ViewMetrics;
            if (request.VisibilityPolicy.BoundsMode != SceneUIBoundsMode.Point
                && !viewMetrics.IsValid)
            {
                if (!(context.Descriptor.ViewHost is ISceneUIViewMetricsProvider metricsProvider)
                    || !metricsProvider.TryGetMetrics(request.ViewKey, out viewMetrics)
                    || !viewMetrics.IsValid)
                {
                    EmberDebug.LogError(
                        TAG,
                        "Register failed: PartialRect/FullRect requires valid ViewMetrics from the request or ViewHost.");
                    return default;
                }
            }

            int index;
            EntrySlot entry;
            if (_freeEntryIndices.Count > 0)
            {
                index = _freeEntryIndices.Pop();
                entry = _entries[index];
            }
            else
            {
                index = _entries.Count;
                entry = new EntrySlot();
                _entries.Add(entry);
            }

            entry.Active = true;
            entry.ContextIndex = request.Context.Index;
            entry.ViewKey = request.ViewKey;
            entry.Anchor = request.Anchor;
            entry.Binder = request.Binder;
            entry.WorldOffset = request.WorldOffset;
            entry.UiOffset = request.UiOffset;
            entry.UpdatePolicy = request.UpdatePolicy;
            entry.VisibilityPolicy = request.VisibilityPolicy;
            entry.ScalePolicy = request.ScalePolicy.Mode == SceneUIScaleMode.SceneRelative
                ? SceneUIScalePolicy.Relative(
                    request.ScalePolicy.ReferenceDistance,
                    request.ScalePolicy.Factor,
                    request.ScalePolicy.MinScale,
                    request.ScalePolicy.MaxScale)
                : SceneUIScalePolicy.Fixed;
            entry.ViewLifetimePolicy = request.ViewLifetimePolicy;
            entry.RecycleDelaySeconds = Mathf.Max(0f, request.RecycleDelaySeconds);
            entry.SortingPriority = request.SortingPriority;
            entry.ViewMetrics = viewMetrics;
            entry.OcclusionPolicy = request.OcclusionPolicy.Enabled
                ? new SceneUIOcclusionPolicy
                {
                    Enabled = true,
                    RetestIntervalSeconds = Mathf.Max(0f, request.OcclusionPolicy.RetestIntervalSeconds),
                    TargetRadius = Mathf.Max(0f, request.OcclusionPolicy.TargetRadius),
                    UnknownMode = request.OcclusionPolicy.UnknownMode,
                }
                : SceneUIOcclusionPolicy.Disabled;
            entry.OcclusionState = request.OcclusionPolicy.Enabled
                ? SceneUIOcclusionState.Unknown
                : SceneUIOcclusionState.Disabled;
            entry.BusinessVisible = request.BusinessVisible;
            entry.Dirty = DirtyFlags.All;
            entry.HasWorldPosition = false;
            entry.HasSpatialState = false;
            entry.SpatialVisible = false;
            entry.InvisibleReason = SceneUIInvisibleReason.None;
            entry.View = null;
            entry.ViewBound = false;
            entry.ViewVisible = false;
            entry.QueuedDirty = false;
            entry.QueuedDelayedRecycle = false;
            entry.DelayedRecycleListed = false;
            entry.RecycleAtTime = 0d;
            entry.OcclusionTestScheduled = false;
            entry.NextOcclusionTestTime = 0d;
            entry.LastSpatialFrame = -1;
            entry.LastProcessedFrame = -1;

            context.EntryIndices.Add(index);
            if (entry.UpdatePolicy == SceneUIUpdatePolicy.PollPosition)
                context.PolledEntryIndices.Add(index);
            if (entry.OcclusionPolicy.Enabled)
                context.OcclusionEntryIndices.Add(index);

            QueueEntryForUpdate(index, entry);
            _activeEntryCount++;
            return new SceneUIHandle(index, entry.Version);
        }

        /// <summary>原子批量注册；任一请求失败时回滚本批次已经创建的 Entry。</summary>
        [HasGC]
        public bool TryRegisterBatch(
            SceneUIRequest[] requests,
            SceneUIHandle[] results,
            int count)
        {
            if (requests == null
                || results == null
                || count < 0
                || count > requests.Length
                || count > results.Length)
                return false;

            if (count > 0)
            {
                SceneUIContextHandle sharedContextHandle = requests[0].Context;
                bool sharesContext = true;
                for (int i = 1; i < count; i++)
                {
                    if (requests[i].Context == sharedContextHandle)
                        continue;

                    sharesContext = false;
                    break;
                }

                if (sharesContext && TryGetContext(sharedContextHandle, out ContextSlot sharedContext))
                    ReserveBatchCapacity(sharedContext, count);
            }

            int registeredCount = 0;
            for (int i = 0; i < count; i++)
            {
                SceneUIHandle handle = Register(requests[i]);
                results[i] = handle;
                if (!handle.IsValid)
                {
                    UnregisterBatch(results, registeredCount);
                    for (int clearIndex = 0; clearIndex <= registeredCount; clearIndex++)
                        results[clearIndex] = default;
                    return false;
                }

                registeredCount++;
            }

            return true;
        }

        /// <summary>批量注销并按 Context 一次性压缩索引表；返回成功注销数量。</summary>
        [HasGC]
        public int UnregisterBatch(SceneUIHandle[] handles, int count)
        {
            if (handles == null || count <= 0 || count > handles.Length)
                return 0;

            var contextsToCompact = new HashSet<int>();
            int unregisteredCount = 0;
            for (int i = 0; i < count; i++)
            {
                SceneUIHandle handle = handles[i];
                if (!TryGetEntry(handle, out EntrySlot entry)
                    || entry.ContextIndex < 0
                    || entry.ContextIndex >= _contexts.Count)
                    continue;

                int contextIndex = entry.ContextIndex;
                ContextSlot context = _contexts[contextIndex];
                InvalidateEntry(handle.Index, entry, context, context.IsFlushing);
                if (!context.IsFlushing)
                    contextsToCompact.Add(contextIndex);
                unregisteredCount++;
            }

            foreach (int contextIndex in contextsToCompact)
            {
                ContextSlot context = _contexts[contextIndex];
                if (context.Active && !context.PendingRelease)
                    CompactContextEntryLists(context);
            }

            return unregisteredCount;
        }

        /// <summary>检查 Context 句柄当前是否仍有效。</summary>
        [NoGC]
        public bool IsContextValid(SceneUIContextHandle handle)
        {
            return TryGetContext(handle, out _);
        }

        /// <summary>检查 Entry 句柄当前是否仍有效。</summary>
        [NoGC]
        public bool IsEntryValid(SceneUIHandle handle)
        {
            return TryGetEntry(handle, out _);
        }

        /// <summary>读取单个 Entry 的当前状态，不暴露内部可变对象。</summary>
        [NoGC]
        public bool TryGetEntryDiagnostics(
            SceneUIHandle handle,
            out SceneUIEntryDiagnostics diagnostics)
        {
            if (!TryGetEntry(handle, out EntrySlot entry))
            {
                diagnostics = default;
                return false;
            }

            diagnostics = new SceneUIEntryDiagnostics(
                entry.BusinessVisible,
                entry.SpatialVisible,
                entry.View != null,
                entry.ViewVisible,
                entry.QueuedDelayedRecycle,
                entry.InvisibleReason,
                entry.LastWorldPosition,
                entry.SpatialState,
                entry.SortingPriority,
                entry.OcclusionState);
            return true;
        }

        /// <summary>标记单个 Entry 的空间输入已变化。</summary>
        [NoGC]
        public bool MarkSpatialDirty(SceneUIHandle handle)
        {
            if (!TryGetEntry(handle, out EntrySlot entry)) return false;
            entry.Dirty |= DirtyFlags.Spatial;
            if (entry.OcclusionPolicy.Enabled)
                entry.NextOcclusionTestTime = 0d;
            QueueEntryForUpdate(handle.Index, entry);
            return true;
        }

        /// <summary>让 Entry 在下一次预算轮转中尽快重新执行遮挡检测。</summary>
        [NoGC]
        public bool MarkOcclusionDirty(SceneUIHandle handle)
        {
            if (!TryGetEntry(handle, out EntrySlot entry) || !entry.OcclusionPolicy.Enabled)
                return false;

            entry.NextOcclusionTestTime = 0d;
            return true;
        }

        /// <summary>标记单个 Entry 的业务内容已变化。</summary>
        [NoGC]
        public bool MarkContentDirty(SceneUIHandle handle)
        {
            if (!TryGetEntry(handle, out EntrySlot entry)) return false;
            entry.Dirty |= DirtyFlags.Content;
            QueueEntryForUpdate(handle.Index, entry);
            return true;
        }

        /// <summary>强制 Context 下全部 Entry 在下一次 Flush 重新投影。</summary>
        [NoGC]
        public bool MarkContextSpatialDirty(SceneUIContextHandle handle)
        {
            if (!TryGetContext(handle, out ContextSlot context)) return false;
            context.ForceRefresh = true;
            MarkContextOcclusionDirty(context);
            return true;
        }

        /// <summary>修改业务显隐；隐藏时立即关闭射线和视觉。</summary>
        [NoGC]
        public bool SetBusinessVisible(SceneUIHandle handle, bool visible)
        {
            if (!TryGetEntry(handle, out EntrySlot entry)) return false;
            if (entry.BusinessVisible == visible) return true;

            entry.BusinessVisible = visible;
            entry.Dirty |= DirtyFlags.Visibility;
            if (visible && entry.OcclusionPolicy.Enabled)
                entry.NextOcclusionTestTime = 0d;
            QueueEntryForUpdate(handle.Index, entry);
            if (!visible && entry.ContextIndex >= 0 && entry.ContextIndex < _contexts.Count)
                TransitionToInvisible(handle.Index, entry, _contexts[entry.ContextIndex]);
            return true;
        }

        /// <summary>替换 Anchor 并标记空间脏。</summary>
        [NoGC]
        public bool SetAnchor(SceneUIHandle handle, ISceneUIAnchor anchor)
        {
            if (anchor == null || !TryGetEntry(handle, out EntrySlot entry)) return false;
            entry.Anchor = anchor;
            entry.HasWorldPosition = false;
            entry.Dirty |= DirtyFlags.Spatial;
            if (entry.OcclusionPolicy.Enabled)
                entry.NextOcclusionTestTime = 0d;
            QueueEntryForUpdate(handle.Index, entry);
            return true;
        }

        /// <summary>同时修改世界偏移与 UI 偏移。</summary>
        [NoGC]
        public bool SetOffsets(SceneUIHandle handle, Vector3 worldOffset, Vector2 uiOffset)
        {
            if (!SceneUIMath.IsFinite(worldOffset)
                || !SceneUIMath.IsFinite(uiOffset)
                || !TryGetEntry(handle, out EntrySlot entry))
                return false;

            entry.WorldOffset = worldOffset;
            entry.UiOffset = uiOffset;
            entry.Dirty |= DirtyFlags.Spatial;
            if (entry.OcclusionPolicy.Enabled)
                entry.NextOcclusionTestTime = 0d;
            QueueEntryForUpdate(handle.Index, entry);
            return true;
        }

        /// <summary>更新精确矩形边界元数据，并在下一次 Flush 重新计算。</summary>
        [NoGC]
        public bool SetViewMetrics(SceneUIHandle handle, in SceneUIViewMetrics metrics)
        {
            if (!metrics.IsValid || !TryGetEntry(handle, out EntrySlot entry))
                return false;

            entry.ViewMetrics = metrics;
            entry.Dirty |= DirtyFlags.Spatial;
            QueueEntryForUpdate(handle.Index, entry);
            return true;
        }

        /// <summary>更新同一 Context 内的固定排序优先级。</summary>
        [NoGC]
        public bool SetSortingPriority(SceneUIHandle handle, int sortingPriority)
        {
            if (!TryGetEntry(handle, out EntrySlot entry)) return false;
            if (entry.SortingPriority == sortingPriority) return true;

            entry.SortingPriority = sortingPriority;
            if (entry.ContextIndex >= 0 && entry.ContextIndex < _contexts.Count)
                _contexts[entry.ContextIndex].SortingDirty = true;
            return true;
        }

        /// <summary>更新 View 的不可见生命周期策略。</summary>
        [NoGC]
        public bool SetViewLifetimePolicy(
            SceneUIHandle handle,
            SceneUIViewLifetimePolicy policy,
            float recycleDelaySeconds = 0f)
        {
            if (policy < SceneUIViewLifetimePolicy.KeepWhileRegistered
                || policy > SceneUIViewLifetimePolicy.RecycleAfterDelay
                || !SceneUIMath.IsFinite(recycleDelaySeconds)
                || !TryGetEntry(handle, out EntrySlot entry))
                return false;

            entry.ViewLifetimePolicy = policy;
            entry.RecycleDelaySeconds = Mathf.Max(0f, recycleDelaySeconds);
            if (entry.ContextIndex < 0 || entry.ContextIndex >= _contexts.Count)
                return true;

            ContextSlot context = _contexts[entry.ContextIndex];
            bool shouldRender = context.Descriptor.Visible
                && entry.BusinessVisible
                && entry.HasSpatialState
                && entry.SpatialVisible
                && IsOcclusionVisible(entry);
            if (shouldRender)
                CancelDelayedRecycle(entry);
            else
                TransitionToInvisible(handle.Index, entry, context);
            return true;
        }

        /// <summary>动态启用、关闭或更新单个 Entry 的预算化遮挡策略。</summary>
        [NoGC]
        public bool SetOcclusionPolicy(
            SceneUIHandle handle,
            in SceneUIOcclusionPolicy policy)
        {
            if (!TryGetEntry(handle, out EntrySlot entry)
                || entry.ContextIndex < 0
                || entry.ContextIndex >= _contexts.Count)
                return false;

            ContextSlot context = _contexts[entry.ContextIndex];
            if (policy.Enabled
                && (policy.UnknownMode < SceneUIOcclusionUnknownMode.Show
                    || policy.UnknownMode > SceneUIOcclusionUnknownMode.Hide
                    || !SceneUIMath.IsFinite(policy.RetestIntervalSeconds)
                    || !SceneUIMath.IsFinite(policy.TargetRadius)
                    || context.Descriptor.OcclusionTester == null
                    || context.Descriptor.OcclusionTestsPerFlush <= 0))
                return false;

            bool wasEnabled = entry.OcclusionPolicy.Enabled;
            entry.OcclusionPolicy = policy.Enabled
                ? new SceneUIOcclusionPolicy
                {
                    Enabled = true,
                    RetestIntervalSeconds = Mathf.Max(0f, policy.RetestIntervalSeconds),
                    TargetRadius = Mathf.Max(0f, policy.TargetRadius),
                    UnknownMode = policy.UnknownMode,
                }
                : SceneUIOcclusionPolicy.Disabled;
            entry.OcclusionState = policy.Enabled
                ? SceneUIOcclusionState.Unknown
                : SceneUIOcclusionState.Disabled;
            entry.OcclusionTestScheduled = false;
            entry.NextOcclusionTestTime = 0d;

            if (policy.Enabled && !wasEnabled)
                context.OcclusionEntryIndices.Add(handle.Index);
            else if (!policy.Enabled && wasEnabled)
                context.OcclusionEntryIndices.Remove(handle.Index);

            entry.Dirty |= DirtyFlags.Visibility;
            QueueEntryForUpdate(handle.Index, entry);
            return true;
        }

        /// <summary>修改整个 Context 的可见状态。</summary>
        [NoGC]
        public bool SetContextVisible(SceneUIContextHandle handle, bool visible)
        {
            if (!TryGetContext(handle, out ContextSlot context)) return false;
            if (context.Descriptor.Visible == visible) return true;

            context.Descriptor.Visible = visible;
            if (visible)
            {
                context.ForceRefresh = true;
                context.LastFlushFrame = -1;
                MarkContextOcclusionDirty(context);
                return true;
            }

            for (int i = 0; i < context.EntryIndices.Count; i++)
            {
                int entryIndex = context.EntryIndices[i];
                if (entryIndex < 0 || entryIndex >= _entries.Count) continue;
                EntrySlot entry = _entries[entryIndex];
                if (!entry.Active) continue;
                entry.InvisibleReason = SceneUIInvisibleReason.ContextHidden;
                TransitionToInvisible(entryIndex, entry, context);
            }

            return true;
        }

        /// <summary>注销单个 Entry。重复注销安全返回 false。</summary>
        [NoGC]
        public bool Unregister(SceneUIHandle handle)
        {
            if (!TryGetEntry(handle, out EntrySlot entry)) return false;
            if (entry.ContextIndex < 0 || entry.ContextIndex >= _contexts.Count) return false;

            ContextSlot context = _contexts[entry.ContextIndex];
            int entryIndex = handle.Index;
            if (context.IsFlushing)
            {
                InvalidateEntry(entryIndex, entry, context, true);
            }
            else
            {
                context.EntryIndices.Remove(entryIndex);
                context.DirtyEntryIndices.Remove(entryIndex);
                context.PolledEntryIndices.Remove(entryIndex);
                context.DelayedRecycleEntryIndices.Remove(entryIndex);
                context.OcclusionEntryIndices.Remove(entryIndex);
                entry.DelayedRecycleListed = false;
                entry.QueuedDirty = false;
                InvalidateEntry(entryIndex, entry, context, false);
            }

            return true;
        }

        /// <summary>注销 Context 及其全部 Entry、View 和更新订阅。</summary>
        [HasGC]
        public bool UnregisterContext(SceneUIContextHandle handle)
        {
            if (!TryGetContext(handle, out ContextSlot context)) return false;

            context.Active = false;
            if (context.IsFlushing)
            {
                context.PendingRelease = true;
                return true;
            }

            FinalizeContextRelease(handle.Index, context);
            return true;
        }

        /// <summary>
        /// 在对应相机完成更新后批量刷新一个 Context。同一 Context 每帧最多 Flush 一次。
        /// </summary>
        [HasGC("View pool misses may instantiate a prefab.")]
        public bool FlushContext(SceneUIContextHandle handle)
        {
            if (!TryGetContext(handle, out ContextSlot context) || context.IsFlushing)
                return false;

            int frame = Time.frameCount;
            if (context.LastFlushFrame == frame)
                return true;

            context.LastFlushFrame = frame;
            context.IsFlushing = true;
            double flushStartedAt = Time.realtimeSinceStartupAsDouble;
            long allocatedBytesBefore = GC.GetAllocatedBytesForCurrentThread();
            try
            {
                if (!TryCaptureSnapshot(context, out bool snapshotChanged))
                {
                    int invalidCount = context.EntryIndices.Count;
                    for (int i = 0; i < invalidCount; i++)
                    {
                        int entryIndex = context.EntryIndices[i];
                        if (entryIndex < 0 || entryIndex >= _entries.Count) continue;
                        EntrySlot entry = _entries[entryIndex];
                        if (!entry.Active) continue;
                        SetSpatialInvisible(entry, SceneUIInvisibleReason.ProjectionInvalid);
                        TransitionToInvisible(entryIndex, entry, context);
                    }
                    ProcessDelayedRecycles(context);
                    ApplyViewSorting(context);
                    return false;
                }

                ProcessViewHostPrewarm(context);

                if (!context.Descriptor.Visible)
                {
                    ProcessDelayedRecycles(context);
                    ApplyViewSorting(context);
                    return true;
                }

                if (!context.Descriptor.VisibleRegionProvider.TryGetVisibleRect(out Rect visibleRect))
                {
                    int invalidCount = context.EntryIndices.Count;
                    for (int i = 0; i < invalidCount; i++)
                    {
                        int entryIndex = context.EntryIndices[i];
                        if (entryIndex < 0 || entryIndex >= _entries.Count) continue;
                        EntrySlot entry = _entries[entryIndex];
                        if (!entry.Active) continue;
                        SetSpatialInvisible(entry, SceneUIInvisibleReason.ProjectionInvalid);
                        TransitionToInvisible(entryIndex, entry, context);
                    }
                    ProcessDelayedRecycles(context);
                    ApplyViewSorting(context);
                    return false;
                }

                bool visibleRectChanged = !context.HasVisibleRect || context.LastVisibleRect != visibleRect;
                context.LastVisibleRect = visibleRect;
                context.HasVisibleRect = true;

                if (snapshotChanged)
                    MarkContextOcclusionDirty(context);
                ScheduleOcclusionTests(handle.Index, context);

                bool refreshAll = context.ForceRefresh || snapshotChanged || visibleRectChanged;
                context.ForceRefresh = false;

                int dirtyCount = context.DirtyEntryIndices.Count;
                int polledCount = context.PolledEntryIndices.Count;
                for (int i = 0; i < dirtyCount; i++)
                {
                    int entryIndex = context.DirtyEntryIndices[i];
                    if (entryIndex >= 0 && entryIndex < _entries.Count)
                        _entries[entryIndex].QueuedDirty = false;
                }

                if (refreshAll)
                {
                    int entryCount = context.EntryIndices.Count;
                    for (int i = 0; i < entryCount; i++)
                    {
                        int entryIndex = context.EntryIndices[i];
                        if (entryIndex < 0 || entryIndex >= _entries.Count) continue;
                        ProcessEntry(
                            entryIndex,
                            handle.Index,
                            _entries[entryIndex],
                            context,
                            visibleRect,
                            frame,
                            true);
                    }
                }
                else
                {
                    for (int i = 0; i < dirtyCount; i++)
                    {
                        int entryIndex = context.DirtyEntryIndices[i];
                        if (entryIndex < 0 || entryIndex >= _entries.Count) continue;
                        ProcessEntry(
                            entryIndex,
                            handle.Index,
                            _entries[entryIndex],
                            context,
                            visibleRect,
                            frame,
                            false);
                    }

                    for (int i = 0; i < polledCount; i++)
                    {
                        int entryIndex = context.PolledEntryIndices[i];
                        if (entryIndex < 0 || entryIndex >= _entries.Count) continue;
                        ProcessEntry(
                            entryIndex,
                            handle.Index,
                            _entries[entryIndex],
                            context,
                            visibleRect,
                            frame,
                            false);
                    }
                }

                if (dirtyCount > 0)
                    context.DirtyEntryIndices.RemoveRange(0, dirtyCount);

                ProcessDelayedRecycles(context);
                ApplyViewSorting(context);

                return true;
            }
            finally
            {
                _lastFlushFrame = frame;
                _lastFlushDurationMs = (Time.realtimeSinceStartupAsDouble - flushStartedAt) * 1000d;
                _lastFlushAllocatedBytes = Math.Max(
                    0L,
                    GC.GetAllocatedBytesForCurrentThread() - allocatedBytesBefore);
                FinishContextFlush(handle.Index, context);
            }
        }

        #endregion
    }
}
