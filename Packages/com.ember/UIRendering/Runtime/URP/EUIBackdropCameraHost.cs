using System;
using System.Collections.Generic;
using Ember.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ember.UIRendering
{
    /// <summary>显式接入的 UI 相机段宿主。只管理指定 UI 树，释放后还原原相机和实例层。</summary>
    public sealed class EUIBackdropCameraHost : MonoBehaviour, IEUIBackdropProvider
    {
        #region 内部参数
        private Camera _baseCamera;
        private Camera _uiCamera;
        private Transform _uiRoot;
        private int _rendererIndex;
        private int _originalRendererIndex;
        private int _uiLayer;
        private int[] _layers;
        private bool _dirty;
        private readonly List<BackdropLease> _leases = new();
        private readonly List<Canvas> _canvases = new();
        private readonly List<Transform> _nodes = new();
        private readonly List<Camera> _segments = new();
        private readonly Dictionary<GameObject, LayerChange> _layerChanges = new();
        private readonly Dictionary<Canvas, CameraChange> _cameraChanges = new();
        private readonly Dictionary<Camera, BackdropLease> _captures = new();
        private readonly Dictionary<Camera, BackdropLease> _inputs = new();
        public static EUIBackdropCameraHost Active { get; private set; }
        public int BoundaryCount => _leases.Count;
        public int CaptureCount { get; private set; }
        public string Diagnostic { get; private set; }
        private struct LayerChange { public int Original, Assigned; }
        private struct CameraChange { public Camera Original, Assigned; }
        #endregion

        #region 生命周期
        private void LateUpdate()
        {
            if (_dirty) Rebuild();
            for (int i = 0; i < _segments.Count; i++)
            {
                var camera = _segments[i];
                camera.transform.SetPositionAndRotation(_uiCamera.transform.position, _uiCamera.transform.rotation);
                camera.orthographic = _uiCamera.orthographic;
                camera.orthographicSize = _uiCamera.orthographicSize;
                camera.fieldOfView = _uiCamera.fieldOfView;
                camera.rect = _uiCamera.rect;
                camera.nearClipPlane = _uiCamera.nearClipPlane;
                camera.farClipPlane = _uiCamera.farClipPlane;
            }
        }

        private void OnDestroy()
        {
            for (int i = _leases.Count - 1; i >= 0; i--) _leases[i].Dispose();
            Restore();
            RemoveSegments();
            if (_uiCamera) _uiCamera.GetUniversalAdditionalCameraData().SetRenderer(_originalRendererIndex);
            if (ReferenceEquals(EUIBackdrop.Provider, this)) EUIBackdrop.Provider = null;
            if (Active == this) Active = null;
        }
        #endregion

        #region 外部方法
        public void Configure(Camera baseCamera, Camera uiCamera, Transform uiRoot,
            int rendererIndex, int originalRendererIndex, int[] reservedLayers)
        {
            if (Active && Active != this) throw new InvalidOperationException("An EUI backdrop host is already installed");
            if (!baseCamera || !uiCamera || !uiRoot || reservedLayers == null || reservedLayers.Length == 0)
                throw new ArgumentException("Backdrop requires base/UI camera, UI root and reserved layer pool");
            if (!baseCamera.GetUniversalAdditionalCameraData().cameraStack.Contains(uiCamera))
                throw new ArgumentException("UI camera must be in the supplied Base camera stack");
            _baseCamera = baseCamera; _uiCamera = uiCamera; _uiRoot = uiRoot;
            _rendererIndex = rendererIndex; _originalRendererIndex = originalRendererIndex;
            _layers = (int[])reservedLayers.Clone();
            int uiMask = uiCamera.cullingMask;
            if (uiMask == 0 || (uiMask & (uiMask - 1)) != 0) throw new ArgumentException("Backdrop requires a dedicated single-layer UI camera");
            for (int i = 0; i < 32; i++) if ((uiMask & (1 << i)) != 0) _uiLayer = i;
            foreach (var layer in _layers)
                if (layer < 8 || layer > 31 || (baseCamera.cullingMask & (1 << layer)) != 0 || (uiCamera.cullingMask & (1 << layer)) != 0)
                    throw new ArgumentException("Reserved layer conflicts with camera culling: " + layer);
            Active = this; EUIBackdrop.Provider = this; Diagnostic = "Ready; no consumers";
        }

        public IEUIBackdropLease Acquire(in EUIBackdropRequest request)
        {
            if (!_uiCamera || !request.Foreground) return null;
            if (!request.Foreground.isRootCanvas)
            { Diagnostic = "Solid fallback: independent backdrop boundary requires a root Canvas"; return null; }
            if (request.Settings && request.Settings.UpdateMode == EUIBackdropUpdateMode.Off) return null;
            if (_uiCamera.rect != new Rect(0, 0, 1, 1) || _baseCamera.stereoEnabled || request.Foreground.targetDisplay != _uiCamera.targetDisplay)
            { Diagnostic = "Solid fallback: partial viewport, XR or different display is not supported"; return null; }
            for (int i = 0; i < _leases.Count; i++)
            {
                var shared = _leases[i];
                if (shared.Request.Foreground == request.Foreground &&
                    shared.Request.FollowsForegroundOrder == request.FollowsForegroundOrder &&
                    shared.Request.CurrentSortingOrder == request.CurrentSortingOrder &&
                    shared.Request.Settings == request.Settings)
                { shared.References++; return new SharedLease(shared); }
            }
            if (_leases.Count >= _layers.Length)
            { Diagnostic = "Solid fallback: reserved layer pool exhausted"; return null; }
            var lease = new BackdropLease(this, request);
            _leases.Add(lease); _dirty = true;
            return new SharedLease(lease);
        }

        public void MarkOrderDirty() { _dirty = true; }
        public bool HasCapture(Camera camera) => _captures.ContainsKey(camera);
        public bool HasInput(Camera camera) => _inputs.ContainsKey(camera);
        internal BackdropLease CaptureFor(Camera camera) => _captures.TryGetValue(camera, out var lease) ? lease : null;
        internal BackdropLease InputFor(Camera camera) => _inputs.TryGetValue(camera, out var lease) ? lease : null;
        #endregion

        #region 内部方法
        private void Release(BackdropLease lease)
        {
            if (--lease.References > 0) return;
            _leases.Remove(lease); lease.Dispose(); _dirty = true;
            if (_leases.Count == 0) Rebuild();
        }

        private void Restore()
        {
            foreach (var pair in _layerChanges)
                if (pair.Key && pair.Key.layer == pair.Value.Assigned) pair.Key.layer = pair.Value.Original;
            _layerChanges.Clear();
            foreach (var pair in _cameraChanges)
                if (pair.Key && pair.Key.worldCamera == pair.Value.Assigned) pair.Key.worldCamera = pair.Value.Original;
            _cameraChanges.Clear();
        }

        private void RemoveSegments()
        {
            var stack = _baseCamera ? _baseCamera.GetUniversalAdditionalCameraData().cameraStack : null;
            foreach (var segment in _segments)
            { stack?.Remove(segment); if (segment) { segment.enabled = false; Destroy(segment.gameObject); } }
            _segments.Clear(); _captures.Clear(); _inputs.Clear();
        }

        private void Rebuild()
        {
            _dirty = false; Restore(); RemoveSegments();
            if (!_uiCamera) return;
            _uiCamera.GetUniversalAdditionalCameraData().SetRenderer(_leases.Count == 0 ? _originalRendererIndex : _rendererIndex);
            if (_leases.Count == 0) { Diagnostic = "Idle; 0 capture passes, 0 owned RT"; return; }
            _leases.Sort((a,b) => CompareBoundary(a.Request, b.Request));
            var stack = _baseCamera.GetUniversalAdditionalCameraData().cameraStack;
            int insert = stack.IndexOf(_uiCamera) + 1;
            Camera previous = _uiCamera;
            for (int i = 0; i < _leases.Count; i++)
            {
                var lease = _leases[i];
                var go = new GameObject("EUI Foreground Segment " + i) { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                var camera = go.AddComponent<Camera>(); camera.CopyFrom(_uiCamera);
                camera.cullingMask = 1 << _layers[i]; camera.clearFlags = CameraClearFlags.Nothing;
                camera.depth = _uiCamera.depth; camera.useOcclusionCulling = false;
                var data = camera.GetUniversalAdditionalCameraData();
                data.renderType = CameraRenderType.Overlay;
                data.renderPostProcessing = false; data.SetRenderer(_rendererIndex);
                stack.Insert(insert++, camera); _segments.Add(camera);
                _captures[previous] = lease; _inputs[camera] = lease;
                previous = camera;
            }
            _canvases.Clear(); _uiRoot.GetComponentsInChildren(true, _canvases);
            // Unity routes nested canvases through their root camera; only roots form independent segments.
            foreach (var canvas in _canvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || !canvas.isRootCanvas) continue;
                int segmentIndex = -1;
                for (int i = 0; i < _leases.Count; i++)
                    if (AtOrAbove(canvas, _leases[i].Request)) segmentIndex = i;
                var assignedCamera = segmentIndex < 0 ? _uiCamera : _segments[segmentIndex];
                int assignedLayer = segmentIndex < 0 ? _uiLayer : _layers[segmentIndex];
                _cameraChanges[canvas] = new CameraChange { Original = canvas.worldCamera, Assigned = assignedCamera };
                canvas.worldCamera = assignedCamera;
                _nodes.Clear(); canvas.GetComponentsInChildren(true, _nodes);
                foreach (var node in _nodes)
                {
                    var go = node.gameObject;
                    if (_layerChanges.TryGetValue(go, out var old))
                    { old.Assigned = assignedLayer; _layerChanges[go] = old; }
                    else _layerChanges[go] = new LayerChange { Original = go.layer, Assigned = assignedLayer };
                    go.layer = assignedLayer;
                }
            }
            Diagnostic = "Active boundaries: " + _leases.Count;
        }

        private static int CompareBoundary(EUIBackdropRequest a, EUIBackdropRequest b)
        {
            int layer = SortingLayer.GetLayerValueFromID(a.Foreground.sortingLayerID).CompareTo(SortingLayer.GetLayerValueFromID(b.Foreground.sortingLayerID));
            if (layer != 0) return layer;
            int order = a.CurrentSortingOrder.CompareTo(b.CurrentSortingOrder);
            return order != 0 ? order : a.Foreground.transform.GetSiblingIndex().CompareTo(b.Foreground.transform.GetSiblingIndex());
        }

        private static bool AtOrAbove(Canvas canvas, EUIBackdropRequest boundary)
        {
            int layer = SortingLayer.GetLayerValueFromID(canvas.sortingLayerID).CompareTo(SortingLayer.GetLayerValueFromID(boundary.Foreground.sortingLayerID));
            return layer > 0 || (layer == 0 && canvas.sortingOrder >= boundary.CurrentSortingOrder);
        }

        private sealed class SharedLease : IEUIBackdropLease
        {
            private BackdropLease _source;
            public SharedLease(BackdropLease source) { _source = source; }
            public Texture Texture => _source?.Texture;
            public bool IsReady => _source != null && _source.IsReady;
            public bool IsDisposed => _source == null || _source.IsDisposed;
            public int Generation => _source?.Generation ?? -1;
            public int TextureVersion => _source?.TextureVersion ?? 0;
            public string Diagnostic => _source?.Diagnostic ?? "Disposed";
            public void Invalidate() => _source?.Invalidate();
            public void Freeze() => _source?.Freeze();
            public void Dispose() { var source = _source; _source = null; if (source != null && !source.IsDisposed) source.Owner.Release(source); }
        }

        internal sealed class BackdropLease : IEUIBackdropLease
        {
            public readonly EUIBackdropCameraHost Owner;
            public readonly EUIBackdropRequest Request;
            public RTHandle Output;
            public Material BlurMaterial;
            public int References = 1;
            public string DiagnosticMessage = "Preparing first capture";
            private bool _invalidated = true, _frozen;
            private double _nextUpdate;
            public Texture Texture => Output?.rt;
            public bool IsReady { get; private set; }
            public bool IsDisposed { get; private set; }
            public int Generation => Request.Generation;
            public int TextureVersion { get; private set; }
            public string Diagnostic => DiagnosticMessage;
            public int Iterations => Request.Settings ? Request.Settings.Iterations : 2;
            public bool ShouldCapture => !IsDisposed && (_invalidated || (!_frozen &&
                (!Request.Settings || Request.Settings.UpdateMode != EUIBackdropUpdateMode.Snapshot) && Time.unscaledTimeAsDouble >= _nextUpdate));

            public BackdropLease(EUIBackdropCameraHost owner, EUIBackdropRequest request)
            {
                Owner = owner; Request = request;
                var shader = Shader.Find("Hidden/Ember/BackdropBlur");
                if (shader) BlurMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            public void EnsureOutput(RenderTextureDescriptor descriptor)
            {
                int downsample = Request.Settings ? Request.Settings.Downsample : 4;
                descriptor.width = Mathf.Max(1, descriptor.width / downsample);
                descriptor.height = Mathf.Max(1, descriptor.height / downsample);
                descriptor.depthBufferBits = 0; descriptor.msaaSamples = 1;
                descriptor.useMipMap = false; descriptor.autoGenerateMips = false;
                descriptor.bindMS = false;
                RenderingUtils.ReAllocateHandleIfNeeded(ref Output, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "EUI Backdrop Output");
                if (BlurMaterial)
                {
                    float radius = Request.Settings ? Request.Settings.BlurRadiusDU : 16;
                    float scale = Request.Foreground ? Request.Foreground.scaleFactor : 1;
                    BlurMaterial.SetFloat("_Radius", radius * scale / downsample / (4 * Mathf.Sqrt(Iterations)));
                }
            }

            public void Captured()
            {
                _invalidated = false; IsReady = true; TextureVersion++; Owner.CaptureCount++;
                _nextUpdate = Time.unscaledTimeAsDouble + (Request.Settings && Request.Settings.UpdateMode == EUIBackdropUpdateMode.Live ? 0 : 1.0 / (Request.Settings ? Request.Settings.UpdatesPerSecond : 30));
                DiagnosticMessage = "Blur ready";
            }
            public void Invalidate() { _invalidated = true; }
            public void Freeze() { _frozen = true; }
            public void Dispose()
            {
                if (IsDisposed) return;
                IsDisposed = true; IsReady = false;
                Output?.Release(); Output = null;
                if (BlurMaterial) Destroy(BlurMaterial); BlurMaterial = null;
            }
        }
        #endregion
    }
}
