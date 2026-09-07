// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;

namespace Ember.SceneUI.PlayModeTests
{
    public class SceneUIPlayModeTests
    {
        private EmberSceneUIEngine _engine;

        private sealed class CountingAnchor : ISceneUIAnchor
        {
            public Vector3 Position;
            public int ReadCount;

            public bool TryGetWorldPosition(out Vector3 worldPosition)
            {
                ReadCount++;
                worldPosition = Position;
                return true;
            }
        }

        private sealed class CountingOcclusionTester : ISceneUIOcclusionTester
        {
            public int TestCount { get; private set; }

            public bool TryTestOcclusion(
                Camera sceneCamera,
                Vector3 targetWorldPosition,
                in SceneUIOcclusionPolicy policy,
                out bool occluded)
            {
                TestCount++;
                occluded = false;
                return true;
            }
        }

        private sealed class TestView : ISceneUIView
        {
            private readonly GameObject _gameObject;

            public RectTransform RectTransform { get; }
            public Vector2 LastPosition { get; private set; }
            public int ApplyCount { get; private set; }

            public TestView(RectTransform parent)
            {
                _gameObject = new GameObject("SceneUIPlayModeView", typeof(RectTransform));
                RectTransform = _gameObject.GetComponent<RectTransform>();
                RectTransform.SetParent(parent, false);
                _gameObject.SetActive(false);
            }

            public void ApplySpatialState(in SceneUISpatialState state)
            {
                LastPosition = state.AnchoredPosition;
                ApplyCount++;
            }

            public void SetVisible(bool visible)
            {
                _gameObject.SetActive(visible);
            }

            public void ResetView()
            {
            }

            public void Destroy()
            {
                if (_gameObject)
                    Object.Destroy(_gameObject);
            }
        }

        private sealed class TestViewHost : ISceneUIViewHost, ISceneUIViewHostPrewarm
        {
            private readonly RectTransform _parent;
            private readonly List<TestView> _views = new List<TestView>();

            public IReadOnlyList<TestView> Views => _views;
            public int ReleaseCount { get; private set; }
            public int PendingPrewarmCount { get; set; }
            public int PrewarmedViewCount { get; private set; }
            public int PrewarmFailureCount => 0;
            public int PrewarmProcessCount { get; private set; }
            public int LastPrewarmBudget { get; private set; }

            public TestViewHost(RectTransform parent)
            {
                _parent = parent;
            }

            public bool TryAcquire(SceneUIViewKey viewKey, out ISceneUIView view)
            {
                var created = new TestView(_parent);
                _views.Add(created);
                view = created;
                return true;
            }

            public void Release(SceneUIViewKey viewKey, ISceneUIView view)
            {
                ReleaseCount++;
                (view as TestView)?.Destroy();
            }

            public int ProcessPrewarm(int maxInstantiateCount)
            {
                PrewarmProcessCount++;
                LastPrewarmBudget = maxInstantiateCount;
                int processed = Mathf.Min(PendingPrewarmCount, Mathf.Max(0, maxInstantiateCount));
                PendingPrewarmCount -= processed;
                PrewarmedViewCount += processed;
                return processed;
            }
        }

        [UnityTest]
        public IEnumerator PollPosition_TargetAndCameraMove_ShouldApplyInNotificationFrame()
        {
            CreateEnvironment(
                out GameObject cameraObject,
                out GameObject canvasObject,
                out Camera sceneCamera,
                out RectTransform layerRoot,
                out ManualSceneUICameraUpdateSource source,
                out TestViewHost host,
                out SceneUIContextHandle context);
            var targetObject = new GameObject("SceneUITarget");
            targetObject.transform.position = new Vector3(0f, 0f, 10f);

            try
            {
                SceneUIHandle entry = RegisterEntry(
                    context,
                    new TransformSceneUIAnchor(targetObject.transform),
                    SceneUIUpdatePolicy.PollPosition,
                    1);

                source.NotifyCameraUpdated();
                Assert.IsTrue(entry.IsValid);
                Assert.AreEqual(1, host.Views.Count);
                Vector2 firstPosition = host.Views[0].LastPosition;

                yield return null;

                targetObject.transform.position = new Vector3(2f, 1f, 10f);
                sceneCamera.transform.position = new Vector3(0.5f, 0f, 0f);
                int notificationFrame = Time.frameCount;
                source.NotifyCameraUpdated();

                Assert.AreEqual(notificationFrame, Time.frameCount);
                Assert.AreEqual(2, host.Views[0].ApplyCount);
                Assert.AreNotEqual(firstPosition, host.Views[0].LastPosition);
            }
            finally
            {
                _engine.UnregisterContext(context);
                Object.Destroy(targetObject);
                Object.Destroy(canvasObject);
                Object.Destroy(cameraObject);
                _engine.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator ManualEntry_UnchangedCamera_ShouldNotReadStaticAnchorAgain()
        {
            CreateEnvironment(
                out GameObject cameraObject,
                out GameObject canvasObject,
                out _,
                out _,
                out ManualSceneUICameraUpdateSource source,
                out _,
                out SceneUIContextHandle context);
            var anchor = new CountingAnchor { Position = new Vector3(0f, 0f, 10f) };

            try
            {
                RegisterEntry(context, anchor, SceneUIUpdatePolicy.Manual, 2);
                source.NotifyCameraUpdated();
                Assert.AreEqual(1, anchor.ReadCount);

                yield return null;

                source.NotifyCameraUpdated();
                Assert.AreEqual(1, anchor.ReadCount);
            }
            finally
            {
                _engine.UnregisterContext(context);
                Object.Destroy(canvasObject);
                Object.Destroy(cameraObject);
                _engine.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator RecycleAfterDelay_ShouldReleaseOnlyAfterDeadline()
        {
            CreateEnvironment(
                out GameObject cameraObject,
                out GameObject canvasObject,
                out _,
                out _,
                out ManualSceneUICameraUpdateSource source,
                out TestViewHost host,
                out SceneUIContextHandle context);

            try
            {
                SceneUIHandle entry = _engine.Register(new SceneUIRequest
                {
                    Context = context,
                    ViewKey = new SceneUIViewKey(3),
                    Anchor = new WorldPositionSceneUIAnchor(new Vector3(0f, 0f, 10f)),
                    UpdatePolicy = SceneUIUpdatePolicy.Manual,
                    VisibilityPolicy = SceneUIVisibilityPolicy.DefaultHide,
                    ScalePolicy = SceneUIScalePolicy.Fixed,
                    ViewLifetimePolicy = SceneUIViewLifetimePolicy.RecycleAfterDelay,
                    RecycleDelaySeconds = 0.05f,
                    BusinessVisible = true,
                });

                source.NotifyCameraUpdated();
                Assert.AreEqual(1, host.Views.Count);
                Assert.AreEqual(0, host.ReleaseCount);

                Assert.IsTrue(_engine.SetBusinessVisible(entry, false));
                Assert.AreEqual(1, _engine.Diagnostics.PendingDelayedRecycleCount);
                Assert.AreEqual(0, host.ReleaseCount);

                yield return new WaitForSecondsRealtime(0.06f);
                source.NotifyCameraUpdated();

                Assert.AreEqual(1, host.ReleaseCount);
                Assert.AreEqual(0, _engine.Diagnostics.PendingDelayedRecycleCount);
                Assert.GreaterOrEqual(
                    _engine.Diagnostics.RecycledAfterDelayCount,
                    1);
            }
            finally
            {
                _engine.UnregisterContext(context);
                Object.Destroy(canvasObject);
                Object.Destroy(cameraObject);
                _engine.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator OcclusionBudget_ShouldRoundRobinAcrossFrames()
        {
            var tester = new CountingOcclusionTester();
            CreateEnvironment(
                out GameObject cameraObject,
                out GameObject canvasObject,
                out _,
                out _,
                out ManualSceneUICameraUpdateSource source,
                out _,
                out SceneUIContextHandle context,
                tester,
                1);

            try
            {
                SceneUIOcclusionPolicy policy = SceneUIOcclusionPolicy.Default;
                policy.RetestIntervalSeconds = 10f;
                SceneUIHandle first = RegisterEntry(
                    context,
                    new WorldPositionSceneUIAnchor(new Vector3(-1f, 0f, 10f)),
                    SceneUIUpdatePolicy.Manual,
                    4,
                    policy);
                SceneUIHandle second = RegisterEntry(
                    context,
                    new WorldPositionSceneUIAnchor(new Vector3(1f, 0f, 10f)),
                    SceneUIUpdatePolicy.Manual,
                    5,
                    policy);

                source.NotifyCameraUpdated();
                Assert.AreEqual(1, tester.TestCount);
                Assert.IsTrue(_engine.TryGetEntryDiagnostics(first, out var firstState));
                Assert.IsTrue(_engine.TryGetEntryDiagnostics(second, out var secondState));
                Assert.AreEqual(SceneUIOcclusionState.Visible, firstState.OcclusionState);
                Assert.AreEqual(SceneUIOcclusionState.Unknown, secondState.OcclusionState);

                yield return null;
                source.NotifyCameraUpdated();

                Assert.AreEqual(2, tester.TestCount);
                Assert.IsTrue(_engine.TryGetEntryDiagnostics(second, out secondState));
                Assert.AreEqual(SceneUIOcclusionState.Visible, secondState.OcclusionState);
            }
            finally
            {
                _engine.UnregisterContext(context);
                Object.Destroy(canvasObject);
                Object.Destroy(cameraObject);
                _engine.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator ContextFlush_ShouldApplyIncrementalPrewarmBudgetOncePerFrame()
        {
            CreateEnvironment(
                out GameObject cameraObject,
                out GameObject canvasObject,
                out _,
                out _,
                out ManualSceneUICameraUpdateSource source,
                out TestViewHost host,
                out SceneUIContextHandle context,
                prewarmInstancesPerFlush: 3);
            host.PendingPrewarmCount = 8;

            try
            {
                source.NotifyCameraUpdated();
                Assert.AreEqual(1, host.PrewarmProcessCount);
                Assert.AreEqual(3, host.LastPrewarmBudget);
                Assert.AreEqual(5, host.PendingPrewarmCount);
                Assert.AreEqual(5, _engine.Diagnostics.PendingPrewarmCount);
                Assert.AreEqual(3, _engine.Diagnostics.PrewarmedViewCount);

                source.NotifyCameraUpdated();
                Assert.AreEqual(1, host.PrewarmProcessCount, "A Context flushes at most once in the same frame.");

                yield return null;
                source.NotifyCameraUpdated();
                Assert.AreEqual(2, host.PrewarmProcessCount);
                Assert.AreEqual(2, host.PendingPrewarmCount);
            }
            finally
            {
                _engine.UnregisterContext(context);
                Object.Destroy(canvasObject);
                Object.Destroy(cameraObject);
                _engine.Dispose();
            }
        }

        private SceneUIHandle RegisterEntry(
            SceneUIContextHandle context,
            ISceneUIAnchor anchor,
            SceneUIUpdatePolicy updatePolicy,
            int viewKey,
            SceneUIOcclusionPolicy? occlusionPolicy = null)
        {
            return _engine.Register(new SceneUIRequest
            {
                Context = context,
                ViewKey = new SceneUIViewKey(viewKey),
                Anchor = anchor,
                UpdatePolicy = updatePolicy,
                VisibilityPolicy = SceneUIVisibilityPolicy.DefaultHide,
                ScalePolicy = SceneUIScalePolicy.Fixed,
                ViewLifetimePolicy = SceneUIViewLifetimePolicy.KeepWhileRegistered,
                OcclusionPolicy = occlusionPolicy ?? SceneUIOcclusionPolicy.Disabled,
                BusinessVisible = true,
            });
        }

        private void CreateEnvironment(
            out GameObject cameraObject,
            out GameObject canvasObject,
            out Camera sceneCamera,
            out RectTransform layerRoot,
            out ManualSceneUICameraUpdateSource source,
            out TestViewHost host,
            out SceneUIContextHandle context,
            ISceneUIOcclusionTester occlusionTester = null,
            int occlusionTestsPerFlush = 0,
            int prewarmInstancesPerFlush = 0)
        {
            _engine?.Dispose();
            _engine = new EmberSceneUIEngine();

            cameraObject = new GameObject("SceneUICamera", typeof(Camera));
            sceneCamera = cameraObject.GetComponent<Camera>();
            sceneCamera.nearClipPlane = 0.1f;
            sceneCamera.farClipPlane = 1000f;

            canvasObject = new GameObject("SceneUICanvas", typeof(RectTransform), typeof(Canvas));
            layerRoot = canvasObject.GetComponent<RectTransform>();
            layerRoot.sizeDelta = new Vector2(Screen.width, Screen.height);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            source = new ManualSceneUICameraUpdateSource();
            host = new TestViewHost(layerRoot);
            context = _engine.RegisterContext(new SceneUIContextDescriptor
            {
                SceneCamera = sceneCamera,
                Canvas = canvas,
                LayerRoot = layerRoot,
                VisibleRegionProvider = new FixedSceneUIVisibleRegionProvider(
                    new Rect(0f, 0f, Screen.width, Screen.height)),
                ViewHost = host,
                CameraUpdateSource = source,
                Visible = true,
                PrewarmInstancesPerFlush = prewarmInstancesPerFlush,
                OcclusionTester = occlusionTester,
                OcclusionTestsPerFlush = occlusionTestsPerFlush,
            });

            Assert.IsTrue(context.IsValid);
        }
    }
}
