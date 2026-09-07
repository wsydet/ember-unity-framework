// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using Ember.Core;
using Ember.SceneUI.Integration;
using Ember.UIExtension;

using NUnit.Framework;

using Unity.Cinemachine;

using UnityEditor;

using UnityEngine;

namespace Ember.SceneUI.Tests
{
    public class SceneUICoreEditTests
    {
        [EmberModule(ModulePhase.Gameplay, Enabled = false)]
        private sealed class TestSceneUIModule : EmberSceneUIModuleBase<TestSceneUIModule>
        {
            public TestSceneUIModule()
            {
            }

            protected override void RegisterSceneUIChannels()
            {
            }
        }

        private sealed class FakeView : ISceneUIView
        {
            private readonly GameObject _gameObject;

            public RectTransform RectTransform { get; }
            public SceneUISpatialState SpatialState { get; private set; }
            public bool Visible { get; private set; }

            public FakeView(RectTransform parent)
            {
                _gameObject = new GameObject("FakeSceneUIView", typeof(RectTransform));
                RectTransform = _gameObject.GetComponent<RectTransform>();
                RectTransform.SetParent(parent, false);
                _gameObject.SetActive(false);
            }

            public void ApplySpatialState(in SceneUISpatialState state)
            {
                SpatialState = state;
                RectTransform.anchoredPosition = state.AnchoredPosition;
            }

            public void SetVisible(bool visible)
            {
                Visible = visible;
                _gameObject.SetActive(visible);
            }

            public void ResetView()
            {
            }

            public void Destroy()
            {
                if (_gameObject)
                    UnityEngine.Object.DestroyImmediate(_gameObject);
            }
        }

        private sealed class FakeViewHost : ISceneUIViewHost
        {
            private readonly RectTransform _parent;

            public int AcquireCount { get; private set; }
            public int ReleaseCount { get; private set; }
            public FakeView LastView { get; private set; }

            public FakeViewHost(RectTransform parent)
            {
                _parent = parent;
            }

            public bool TryAcquire(SceneUIViewKey viewKey, out ISceneUIView view)
            {
                AcquireCount++;
                LastView = new FakeView(_parent);
                view = LastView;
                return true;
            }

            public void Release(SceneUIViewKey viewKey, ISceneUIView view)
            {
                ReleaseCount++;
                (view as FakeView)?.Destroy();
            }
        }

        private sealed class CountingBinder : ISceneUIBinder
        {
            public int BindCount { get; private set; }
            public int RefreshCount { get; private set; }
            public int UnbindCount { get; private set; }
            public ISceneUIView View { get; private set; }

            public void Bind(ISceneUIView view, SceneUIHandle handle)
            {
                BindCount++;
                View = view;
            }

            public void RefreshContent(ISceneUIView view)
            {
                RefreshCount++;
            }

            public void Unbind(ISceneUIView view)
            {
                UnbindCount++;
            }
        }

        private sealed class SelfUnregisterBinder : ISceneUIBinder
        {
            private readonly EmberSceneUIEngine _engine;
            private SceneUIHandle _handle;

            public SelfUnregisterBinder(EmberSceneUIEngine engine)
            {
                _engine = engine;
            }

            public void Bind(ISceneUIView view, SceneUIHandle handle)
            {
                _handle = handle;
            }

            public void RefreshContent(ISceneUIView view)
            {
                _engine.Unregister(_handle);
            }

            public void Unbind(ISceneUIView view)
            {
            }
        }

        private sealed class FixedOcclusionTester : ISceneUIOcclusionTester
        {
            private readonly bool _occluded;

            public int TestCount { get; private set; }

            public FixedOcclusionTester(bool occluded)
            {
                _occluded = occluded;
            }

            public bool TryTestOcclusion(
                Camera sceneCamera,
                Vector3 targetWorldPosition,
                in SceneUIOcclusionPolicy policy,
                out bool occluded)
            {
                TestCount++;
                occluded = _occluded;
                return true;
            }
        }

        private sealed class FakeResourceHandle : ISceneUIViewResourceHandle
        {
            private event Action CompletionCallbacks;
            private bool _isDisposed;

            public bool IsDone { get; private set; }
            public bool Succeeded { get; private set; }
            public GameObject Asset { get; private set; }
            public string Error { get; private set; }
            public int CancelCount { get; private set; }
            public int DisposeCount { get; private set; }

            public event Action Completed
            {
                add
                {
                    if (IsDone)
                        value?.Invoke();
                    else
                        CompletionCallbacks += value;
                }
                remove => CompletionCallbacks -= value;
            }

            public void Complete(GameObject asset, bool succeeded, string error = null)
            {
                if (IsDone || _isDisposed)
                    return;

                IsDone = true;
                Succeeded = succeeded;
                Asset = asset;
                Error = error;
                Action callbacks = CompletionCallbacks;
                CompletionCallbacks = null;
                callbacks?.Invoke();
            }

            public void Cancel()
            {
                if (IsDone || _isDisposed)
                    return;

                CancelCount++;
                Complete(null, false, "Cancelled");
            }

            public void Dispose()
            {
                if (_isDisposed)
                    return;

                if (!IsDone)
                    Cancel();
                _isDisposed = true;
                DisposeCount++;
                Asset = null;
                CompletionCallbacks = null;
            }
        }

        private sealed class FakeResourceLoader : ISceneUIViewResourceLoader
        {
            private readonly Queue<ISceneUIViewResourceHandle> _handles =
                new Queue<ISceneUIViewResourceHandle>();

            public int LoadCount { get; private set; }

            public void Enqueue(ISceneUIViewResourceHandle handle)
            {
                _handles.Enqueue(handle);
            }

            public ISceneUIViewResourceHandle LoadPrefab(string assetPath)
            {
                LoadCount++;
                return _handles.Count > 0 ? _handles.Dequeue() : null;
            }
        }

        private readonly List<GameObject> _objects = new List<GameObject>();
        private EmberSceneUIEngine _engine;

        [SetUp]
        public void SetUp()
        {
            _engine = new EmberSceneUIEngine();
        }

        [TearDown]
        public void TearDown()
        {
            TestSceneUIModule.Destroy();
            _engine?.Dispose();
            _engine = null;
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i])
                    UnityEngine.Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
        }

        [Test]
        public void HandleReuse_ShouldInvalidateOldVersion()
        {
            CreateContext(out SceneUIContextHandle context, out _, out _, out _);
            SceneUIHandle first = RegisterEntry(context, new CountingBinder());

            Assert.IsTrue(_engine.Unregister(first));
            SceneUIHandle second = RegisterEntry(context, new CountingBinder());

            Assert.AreEqual(first.Index, second.Index);
            Assert.AreNotEqual(first.Version, second.Version);
            Assert.IsFalse(_engine.IsEntryValid(first));
            Assert.IsTrue(_engine.IsEntryValid(second));
        }

        [Test]
        public void EngineDispose_ShouldInvalidateContextAndRemainIdempotent()
        {
            CreateContext(out SceneUIContextHandle context, out _, out _, out _);

            Assert.IsTrue(_engine.IsContextValid(context));
            Assert.DoesNotThrow(_engine.Dispose);
            Assert.DoesNotThrow(_engine.Dispose);
            Assert.IsTrue(_engine.IsDisposed);
            Assert.IsFalse(_engine.IsContextValid(context));
        }

        [Test]
        public void ModuleBase_ShouldExposeInheritedTypedSingletonInstance()
        {
            var instanceProperty = typeof(TestSceneUIModule).GetProperty(
                "Instance",
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static
                | System.Reflection.BindingFlags.FlattenHierarchy);

            Assert.IsNotNull(instanceProperty);
            Assert.IsInstanceOf<TestSceneUIModule>(instanceProperty.GetValue(null));
        }

        [Test]
        public void ModuleBase_ShouldCreateAndDisposeOwnedEngineOnlyDuringModuleLifecycle()
        {
            var module = TestSceneUIModule.Instance;
            Assert.IsFalse(module.IsActive);
            Assert.IsFalse(module.TryGetDiagnostics(out _));

            ((IEmberModule)module).OnInit();

            Assert.IsTrue(module.IsActive);
            Assert.IsTrue(module.TryGetDiagnostics(out SceneUIDiagnostics diagnostics));
            Assert.AreEqual(0, diagnostics.ActiveContextCount);

            ((IEmberModule)module).OnDestroy();

            Assert.IsFalse(module.IsActive);
            Assert.IsFalse(module.TryGetDiagnostics(out _));
        }

        [Test]
        public void ContextRelease_ShouldReleaseViewAndInvalidateEntry()
        {
            CreateContext(out SceneUIContextHandle context, out ManualSceneUICameraUpdateSource source,
                out FakeViewHost host, out _);
            var binder = new CountingBinder();
            SceneUIHandle entry = RegisterEntry(context, binder);

            source.NotifyCameraUpdated();
            Assert.AreEqual(1, host.AcquireCount);
            Assert.IsTrue(host.LastView.Visible);

            Assert.IsTrue(_engine.UnregisterContext(context));
            Assert.AreEqual(1, host.ReleaseCount);
            Assert.AreEqual(1, binder.UnbindCount);
            Assert.IsFalse(_engine.IsContextValid(context));
            Assert.IsFalse(_engine.IsEntryValid(entry));
        }

        [Test]
        public void Flush_WhenBinderUnregistersItself_ShouldDeferReleaseSafely()
        {
            CreateContext(out SceneUIContextHandle context, out ManualSceneUICameraUpdateSource source,
                out FakeViewHost host, out _);
            SceneUIHandle entry = RegisterEntry(context, new SelfUnregisterBinder(_engine));

            Assert.DoesNotThrow(source.NotifyCameraUpdated);
            Assert.IsFalse(_engine.IsEntryValid(entry));
            Assert.AreEqual(1, host.AcquireCount);
            Assert.AreEqual(1, host.ReleaseCount);
        }

        [Test]
        public void VisibilityClamp_ShouldClampInScreenSpaceAndKeepDirection()
        {
            Camera camera = CreateCamera();
            var projection = new SceneUIProjectionResult(
                true,
                new Vector3(1.2f, 0.5f, 10f),
                new Vector2(1200f, 500f),
                10f);
            var policy = SceneUIVisibilityPolicy.DefaultHide;
            policy.OutOfBoundsMode = SceneUIOutOfBoundsMode.Clamp;

            SceneUIVisibilityResult result = SceneUIVisibilityEvaluator.Evaluate(
                projection,
                camera,
                new Rect(0f, 0f, 1000f, 1000f),
                policy);

            Assert.IsTrue(result.Visible);
            Assert.IsFalse(result.IsOnScreen);
            Assert.AreEqual(new Vector2(1000f, 500f), result.ScreenPosition);
            Assert.Greater(result.OffscreenDirection.x, 0f);
        }

        [Test]
        public void VisibilityBehindCamera_ShouldNotClamp()
        {
            Camera camera = CreateCamera();
            var projection = new SceneUIProjectionResult(
                true,
                new Vector3(0.5f, 0.5f, -1f),
                new Vector2(500f, 500f),
                -1f);
            var policy = SceneUIVisibilityPolicy.DefaultHide;
            policy.OutOfBoundsMode = SceneUIOutOfBoundsMode.Clamp;

            SceneUIVisibilityResult result = SceneUIVisibilityEvaluator.Evaluate(
                projection,
                camera,
                new Rect(0f, 0f, 1000f, 1000f),
                policy);

            Assert.IsFalse(result.Visible);
            Assert.AreEqual(SceneUIInvisibleReason.BehindCamera, result.InvisibleReason);
        }

        [Test]
        public void VisibilityBounds_PartialRectAndFullRect_ShouldUseDifferentThresholds()
        {
            Camera camera = CreateCamera();
            var projection = new SceneUIProjectionResult(
                true,
                new Vector3(0.95f, 0.5f, 10f),
                new Vector2(95f, 50f),
                10f);
            var policy = SceneUIVisibilityPolicy.DefaultHide;
            var viewRect = Rect.MinMaxRect(85f, 40f, 105f, 60f);

            policy.BoundsMode = SceneUIBoundsMode.PartialRect;
            SceneUIVisibilityResult partial = SceneUIVisibilityEvaluator.Evaluate(
                projection,
                camera,
                new Rect(0f, 0f, 100f, 100f),
                policy,
                new Vector2(95f, 50f),
                viewRect);

            policy.BoundsMode = SceneUIBoundsMode.FullRect;
            SceneUIVisibilityResult full = SceneUIVisibilityEvaluator.Evaluate(
                projection,
                camera,
                new Rect(0f, 0f, 100f, 100f),
                policy,
                new Vector2(95f, 50f),
                viewRect);

            Assert.IsTrue(partial.Visible);
            Assert.IsTrue(partial.IsOnScreen);
            Assert.IsFalse(full.Visible);
            Assert.AreEqual(SceneUIInvisibleReason.OutOfBounds, full.InvisibleReason);
        }

        [Test]
        public void VisibilityBounds_FullRectClamp_ShouldKeepWholeViewInside()
        {
            Camera camera = CreateCamera();
            var projection = new SceneUIProjectionResult(
                true,
                new Vector3(0.95f, 0.5f, 10f),
                new Vector2(95f, 50f),
                10f);
            var policy = SceneUIVisibilityPolicy.DefaultHide;
            policy.BoundsMode = SceneUIBoundsMode.FullRect;
            policy.OutOfBoundsMode = SceneUIOutOfBoundsMode.Clamp;

            SceneUIVisibilityResult result = SceneUIVisibilityEvaluator.Evaluate(
                projection,
                camera,
                new Rect(0f, 0f, 100f, 100f),
                policy,
                new Vector2(95f, 50f),
                Rect.MinMaxRect(85f, 40f, 105f, 60f));

            Assert.IsTrue(result.Visible);
            Assert.IsFalse(result.IsOnScreen);
            Assert.AreEqual(new Vector2(90f, 50f), result.ScreenPosition);
        }

        [Test]
        public void RelativeScale_ShouldRespectReferenceDistanceAndLimits()
        {
            SceneUIScalePolicy policy = SceneUIScalePolicy.Relative(10f, 1f, 0.5f, 2f);

            Assert.AreEqual(1f, SceneUIScaleEvaluator.Evaluate(policy, 10f), 0.0001f);
            Assert.AreEqual(2f, SceneUIScaleEvaluator.Evaluate(policy, 1f), 0.0001f);
            Assert.AreEqual(0.5f, SceneUIScaleEvaluator.Evaluate(policy, 100f), 0.0001f);
        }

        [Test]
        public void WorldPositionAnchor_ShouldRejectNonFinitePosition()
        {
            var anchor = new WorldPositionSceneUIAnchor(new Vector3(1f, 2f, 3f));
            Assert.IsTrue(anchor.TryGetWorldPosition(out Vector3 position));
            Assert.AreEqual(new Vector3(1f, 2f, 3f), position);

            Assert.IsFalse(anchor.SetWorldPosition(new Vector3(float.NaN, 0f, 0f)));
            Assert.IsTrue(anchor.TryGetWorldPosition(out position));
            Assert.AreEqual(new Vector3(1f, 2f, 3f), position);

            anchor.Clear();
            Assert.IsFalse(anchor.TryGetWorldPosition(out _));
        }

        [Test]
        public void PrefabHost_ShouldPrewarmReuseAndResetEUIItem()
        {
            RectTransform root = CreateRectTransform("PoolRoot");
            GameObject prefabObject = CreateItemPrefab("SceneUIViewPrefab");
            var host = new PrefabSceneUIViewHost(root);
            var key = new SceneUIViewKey(7);

            Assert.IsTrue(host.Register(key, prefabObject, prewarmCount: 2, maxPooledCount: 2));
            Assert.AreEqual(2, host.PooledViewCount);

            Assert.IsTrue(host.TryAcquire(key, out ISceneUIView acquired));
            Assert.AreEqual(1, host.PoolHitCount);
            Assert.AreEqual(1, host.PooledViewCount);

            host.Release(key, acquired);
            Assert.AreEqual(2, host.PooledViewCount);
            Assert.IsInstanceOf<EUIItemSceneUIView>(acquired);

            host.Dispose();
            Assert.AreEqual(0, host.PooledViewCount);
        }

        [Test]
        public void PrefabHost_EUIItemShouldUsePureCSharpSceneUIViewAdapter()
        {
            RectTransform root = CreateRectTransform("EUIItemPoolRoot");
            GameObject prefabObject = CreateItemPrefab("EUIItemPrefab");
            var host = new PrefabSceneUIViewHost(root);
            var key = new SceneUIViewKey(73);

            Assert.IsTrue(host.Register(key, prefabObject, prewarmCount: 1, maxPooledCount: 1));
            Assert.IsTrue(host.TryAcquire(key, out ISceneUIView acquired));
            Assert.IsInstanceOf<EUIItemSceneUIView>(acquired);

            acquired.SetVisible(true);
            Assert.IsTrue(acquired.RectTransform.gameObject.activeSelf);

            host.Release(key, acquired);
            Assert.IsFalse(acquired.RectTransform.gameObject.activeSelf);
            Assert.AreEqual(1, host.PooledViewCount);
            host.Dispose();
        }

        [Test]
        public void PrefabHost_IncrementalPrewarmShouldRoundRobinAcrossViewKeys()
        {
            RectTransform root = CreateRectTransform("IncrementalPoolRoot");
            GameObject prefabObject = CreateItemPrefab("IncrementalSceneUIViewPrefab");
            var host = new PrefabSceneUIViewHost(root);
            var firstKey = new SceneUIViewKey(71);
            var secondKey = new SceneUIViewKey(72);

            Assert.IsTrue(host.Register(firstKey, prefabObject, 3, 3, true));
            Assert.IsTrue(host.Register(secondKey, prefabObject, 3, 3, true));
            Assert.AreEqual(6, host.PendingPrewarmCount);
            Assert.AreEqual(0, host.PooledViewCount);

            Assert.AreEqual(2, host.ProcessPrewarm(2));
            Assert.AreEqual(4, host.PendingPrewarmCount);
            Assert.AreEqual(2, host.PrewarmedViewCount);
            Assert.AreEqual(2, host.PooledViewCount);

            Assert.IsTrue(host.TryAcquire(firstKey, out ISceneUIView firstView));
            Assert.IsTrue(host.TryAcquire(secondKey, out ISceneUIView secondView));
            Assert.AreEqual(2, host.PoolHitCount, "Each ViewKey should receive one instance in the first round.");

            host.Release(firstKey, firstView);
            host.Release(secondKey, secondView);
            host.Dispose();
        }

        [Test]
        public void BatchRegistration_ShouldHandleOneThousandEntriesAndBatchUnregister()
        {
            CreateContext(
                out SceneUIContextHandle context,
                out _,
                out _,
                out _);
            const int count = 1000;
            var requests = new SceneUIRequest[count];
            var handles = new SceneUIHandle[count];
            for (int i = 0; i < count; i++)
            {
                requests[i] = new SceneUIRequest
                {
                    Context = context,
                    ViewKey = new SceneUIViewKey(1),
                    Anchor = new WorldPositionSceneUIAnchor(new Vector3(i, 0f, 10f)),
                    UpdatePolicy = SceneUIUpdatePolicy.Manual,
                    VisibilityPolicy = SceneUIVisibilityPolicy.DefaultHide,
                    ScalePolicy = SceneUIScalePolicy.Fixed,
                    ViewLifetimePolicy = SceneUIViewLifetimePolicy.KeepWhileRegistered,
                    BusinessVisible = false,
                };
            }

            Assert.IsTrue(_engine.TryRegisterBatch(requests, handles, count));
            Assert.AreEqual(count, _engine.Diagnostics.ActiveEntryCount);
            Assert.AreEqual(count, _engine.UnregisterBatch(handles, count));
            Assert.AreEqual(0, _engine.Diagnostics.ActiveEntryCount);
        }

        [Test]
        public void ResourceHost_ShouldPrepareBeforeSynchronousAcquireAndReleaseHandleOnDispose()
        {
            RectTransform root = CreateRectTransform("ResourcePoolRoot");
            GameObject prefabObject = CreateItemPrefab("ResourceSceneUIViewPrefab");
            var handle = new FakeResourceHandle();
            var loader = new FakeResourceLoader();
            loader.Enqueue(handle);
            var host = new ResourceSceneUIViewHost(loader);
            var key = new SceneUIViewKey(81);

            Assert.IsTrue(host.Register(key, "scene-ui/test", prewarmCount: 1, maxPooledCount: 2));
            bool? prepareSucceeded = null;
            Assert.IsTrue(host.PrepareAsync((success, _) => prepareSucceeded = success));
            Assert.AreEqual(ResourceSceneUIViewHostState.Preparing, host.State);
            Assert.IsFalse(host.IsReady);

            handle.Complete(prefabObject, true);

            Assert.AreEqual(true, prepareSucceeded);
            Assert.AreEqual(ResourceSceneUIViewHostState.Prepared, host.State);
            Assert.IsFalse(host.IsReady);
            Assert.AreEqual(0, host.PendingResourceCount);
            Assert.IsTrue(host.Attach(root, -1, true, out string attachError), attachError);
            Assert.AreEqual(ResourceSceneUIViewHostState.Ready, host.State);
            Assert.AreEqual(1, host.PendingPrewarmCount);
            Assert.AreEqual(1, host.ProcessPrewarm(1));
            Assert.IsTrue(host.TryAcquire(key, out ISceneUIView view));
            Assert.AreEqual(1, host.PoolHitCount);
            host.Release(key, view);

            host.Dispose();
            Assert.AreEqual(ResourceSceneUIViewHostState.Disposed, host.State);
            Assert.AreEqual(1, handle.DisposeCount);
            Assert.AreEqual(0, handle.CancelCount);
        }

        [Test]
        public void ResourceHost_DisposeDuringPreparationShouldCancelEveryHandleAndCompleteOnce()
        {
            var firstHandle = new FakeResourceHandle();
            var secondHandle = new FakeResourceHandle();
            var loader = new FakeResourceLoader();
            loader.Enqueue(firstHandle);
            loader.Enqueue(secondHandle);
            var host = new ResourceSceneUIViewHost(loader);
            Assert.IsTrue(host.Register(new SceneUIViewKey(91), "scene-ui/first"));
            Assert.IsTrue(host.Register(new SceneUIViewKey(92), "scene-ui/second"));
            int callbackCount = 0;
            bool? prepareSucceeded = null;

            Assert.IsTrue(host.PrepareAsync((success, _) =>
            {
                callbackCount++;
                prepareSucceeded = success;
            }));
            Assert.AreEqual(2, loader.LoadCount);

            host.Dispose();

            Assert.AreEqual(ResourceSceneUIViewHostState.Disposed, host.State);
            Assert.AreEqual(false, prepareSucceeded);
            Assert.AreEqual(1, callbackCount);
            Assert.AreEqual(1, firstHandle.CancelCount);
            Assert.AreEqual(1, secondHandle.CancelCount);
            Assert.AreEqual(1, firstHandle.DisposeCount);
            Assert.AreEqual(1, secondHandle.DisposeCount);
        }

        [Test]
        public void ResourceHost_LoadFailureShouldCancelRemainingHandles()
        {
            var failedHandle = new FakeResourceHandle();
            var pendingHandle = new FakeResourceHandle();
            var loader = new FakeResourceLoader();
            loader.Enqueue(failedHandle);
            loader.Enqueue(pendingHandle);
            var host = new ResourceSceneUIViewHost(loader);
            Assert.IsTrue(host.Register(new SceneUIViewKey(101), "scene-ui/failure"));
            Assert.IsTrue(host.Register(new SceneUIViewKey(102), "scene-ui/pending"));
            bool? prepareSucceeded = null;
            Assert.IsTrue(host.PrepareAsync((success, _) => prepareSucceeded = success));

            failedHandle.Complete(null, false, "Expected failure");

            Assert.AreEqual(false, prepareSucceeded);
            Assert.AreEqual(ResourceSceneUIViewHostState.Failed, host.State);
            Assert.AreEqual(1, host.ResourceFailureCount);
            Assert.AreEqual(1, failedHandle.DisposeCount);
            Assert.AreEqual(1, pendingHandle.CancelCount);
            Assert.AreEqual(1, pendingHandle.DisposeCount);
            host.Dispose();
        }

        [Test]
        public void CinemachineUpdateSource_ShouldOnlyForwardMatchingOutputCamera()
        {
            GameObject matchingObject = CreateObject(
                "MatchingBrain",
                typeof(Camera),
                typeof(CinemachineBrain));
            GameObject otherObject = CreateObject(
                "OtherBrain",
                typeof(Camera),
                typeof(CinemachineBrain));
            Camera matchingCamera = matchingObject.GetComponent<Camera>();
            CinemachineBrain matchingBrain = matchingObject.GetComponent<CinemachineBrain>();
            CinemachineBrain otherBrain = otherObject.GetComponent<CinemachineBrain>();
            var source = new CinemachineSceneUICameraUpdateSource(matchingCamera);
            int updateCount = 0;
            Action callback = () => updateCount++;

            try
            {
                source.Subscribe(callback);
                CinemachineCore.CameraUpdatedEvent.Invoke(otherBrain);
                CinemachineCore.CameraUpdatedEvent.Invoke(matchingBrain);
                Assert.AreEqual(1, updateCount);

                source.Unsubscribe(callback);
                CinemachineCore.CameraUpdatedEvent.Invoke(matchingBrain);
                Assert.AreEqual(1, updateCount);
            }
            finally
            {
                source.Dispose();
            }
        }

        [Test]
        public void PrioritySorting_HigherPriorityViewShouldBeLaterSibling()
        {
            CreateContext(
                out SceneUIContextHandle context,
                out ManualSceneUICameraUpdateSource source,
                out _,
                out _,
                SceneUISortingMode.Priority);
            var low = new CountingBinder();
            var high = new CountingBinder();

            RegisterEntry(context, low, sortingPriority: 10);
            RegisterEntry(context, high, sortingPriority: 100);
            source.NotifyCameraUpdated();

            Assert.NotNull(low.View);
            Assert.NotNull(high.View);
            Assert.Less(
                low.View.RectTransform.GetSiblingIndex(),
                high.View.RectTransform.GetSiblingIndex());
            Assert.GreaterOrEqual(_engine.Diagnostics.SortingPassCount, 1);
        }

        [Test]
        public void DepthSorting_NearerViewShouldBeLaterSiblingAtSamePriority()
        {
            CreateContext(
                out SceneUIContextHandle context,
                out ManualSceneUICameraUpdateSource source,
                out _,
                out _,
                SceneUISortingMode.PriorityThenDepth);
            var near = new CountingBinder();
            var far = new CountingBinder();

            RegisterEntry(context, near, sortingPriority: 0, depth: 5f);
            RegisterEntry(context, far, sortingPriority: 0, depth: 20f);
            source.NotifyCameraUpdated();

            Assert.NotNull(near.View);
            Assert.NotNull(far.View);
            Assert.Less(
                far.View.RectTransform.GetSiblingIndex(),
                near.View.RectTransform.GetSiblingIndex());
        }

        [Test]
        public void BudgetedOcclusion_ShouldTestOnlyBudgetedEntriesAndCacheResult()
        {
            var tester = new FixedOcclusionTester(true);
            CreateContext(
                out SceneUIContextHandle context,
                out ManualSceneUICameraUpdateSource source,
                out FakeViewHost host,
                out _,
                occlusionTester: tester,
                occlusionTestsPerFlush: 1);
            var firstBinder = new CountingBinder();
            var secondBinder = new CountingBinder();
            SceneUIOcclusionPolicy policy = SceneUIOcclusionPolicy.Default;

            SceneUIHandle first = RegisterEntry(context, firstBinder, occlusionPolicy: policy);
            SceneUIHandle second = RegisterEntry(context, secondBinder, occlusionPolicy: policy);
            source.NotifyCameraUpdated();

            Assert.AreEqual(1, tester.TestCount);
            Assert.AreEqual(1, host.AcquireCount, "Unknown defaults to visible, while the tested entry is occluded.");
            Assert.IsTrue(_engine.TryGetEntryDiagnostics(first, out var firstState));
            Assert.IsTrue(_engine.TryGetEntryDiagnostics(second, out var secondState));
            Assert.AreEqual(SceneUIOcclusionState.Occluded, firstState.OcclusionState);
            Assert.AreEqual(SceneUIInvisibleReason.Occluded, firstState.InvisibleReason);
            Assert.AreEqual(SceneUIOcclusionState.Unknown, secondState.OcclusionState);
            Assert.AreEqual(1, _engine.Diagnostics.OcclusionTestCount);
            Assert.AreEqual(1, _engine.Diagnostics.OcclusionHitCount);
        }

        [Test]
        public void PhysicsOcclusionTester_OrthographicRayShouldDetectObstacle()
        {
            Camera camera = CreateCamera();
            camera.orthographic = true;
            camera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "SceneUIOccluder";
            obstacle.transform.position = new Vector3(0f, 0f, 5f);
            _objects.Add(obstacle);
            Physics.SyncTransforms();

            var tester = new PhysicsSceneUIOcclusionTester(~0);
            SceneUIOcclusionPolicy policy = SceneUIOcclusionPolicy.Default;
            Assert.IsTrue(tester.TryTestOcclusion(
                camera,
                new Vector3(0f, 0f, 10f),
                policy,
                out bool occluded));
            Assert.IsTrue(occluded);

            obstacle.GetComponent<Collider>().enabled = false;
            Physics.SyncTransforms();
            Assert.IsTrue(tester.TryTestOcclusion(
                camera,
                new Vector3(0f, 0f, 10f),
                policy,
                out occluded));
            Assert.IsFalse(occluded);
        }

        private SceneUIHandle RegisterEntry(
            SceneUIContextHandle context,
            ISceneUIBinder binder,
            int sortingPriority = 0,
            float depth = 10f,
            SceneUIOcclusionPolicy? occlusionPolicy = null)
        {
            return _engine.Register(new SceneUIRequest
            {
                Context = context,
                ViewKey = new SceneUIViewKey(1),
                Anchor = new WorldPositionSceneUIAnchor(new Vector3(0f, 0f, depth)),
                Binder = binder,
                UpdatePolicy = SceneUIUpdatePolicy.Manual,
                VisibilityPolicy = SceneUIVisibilityPolicy.DefaultHide,
                ScalePolicy = SceneUIScalePolicy.Fixed,
                ViewLifetimePolicy = SceneUIViewLifetimePolicy.KeepWhileRegistered,
                SortingPriority = sortingPriority,
                OcclusionPolicy = occlusionPolicy ?? SceneUIOcclusionPolicy.Disabled,
                BusinessVisible = true,
            });
        }

        private void CreateContext(
            out SceneUIContextHandle context,
            out ManualSceneUICameraUpdateSource source,
            out FakeViewHost host,
            out RectTransform root,
            SceneUISortingMode sortingMode = SceneUISortingMode.None,
            ISceneUIOcclusionTester occlusionTester = null,
            int occlusionTestsPerFlush = 0)
        {
            Camera sceneCamera = CreateCamera();
            root = CreateRectTransform("SceneUICanvas");
            Canvas canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            root.sizeDelta = new Vector2(1920f, 1080f);

            source = new ManualSceneUICameraUpdateSource();
            host = new FakeViewHost(root);
            context = _engine.RegisterContext(new SceneUIContextDescriptor
            {
                SceneCamera = sceneCamera,
                Canvas = canvas,
                UICamera = null,
                LayerRoot = root,
                VisibleRegionProvider = new FixedSceneUIVisibleRegionProvider(
                    new Rect(0f, 0f, 1920f, 1080f)),
                ViewHost = host,
                CameraUpdateSource = source,
                Visible = true,
                SortingMode = sortingMode,
                OcclusionTester = occlusionTester,
                OcclusionTestsPerFlush = occlusionTestsPerFlush,
            });

            Assert.IsTrue(context.IsValid);
        }

        private Camera CreateCamera()
        {
            GameObject cameraObject = CreateObject("SceneCamera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;
            camera.pixelRect = new Rect(0f, 0f, 1920f, 1080f);
            return camera;
        }

        private RectTransform CreateRectTransform(string name)
        {
            return CreateObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        }

        private GameObject CreateItemPrefab(string name)
        {
            GameObject gameObject = CreateObject(name, typeof(RectTransform));
            EUIBinding binding = gameObject.AddComponent<EUIBinding>();
            var serializedBinding = new SerializedObject(binding);
            serializedBinding.FindProperty("isPage").boolValue = false;
            serializedBinding.FindProperty("className").stringValue = string.Empty;
            serializedBinding.ApplyModifiedPropertiesWithoutUndo();
            Assert.AreEqual(EUIBindingRole.Item, binding.Role);
            return gameObject;
        }

        private GameObject CreateObject(string name, params Type[] components)
        {
            var gameObject = new GameObject(name, components);
            _objects.Add(gameObject);
            return gameObject;
        }
    }
}
