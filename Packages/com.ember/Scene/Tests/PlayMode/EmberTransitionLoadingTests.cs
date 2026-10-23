using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ember.Core;
using Ember.UI;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Ember.Scene.Tests
{
    /// <summary>真实 EUI 页面生命周期与请求 Runner 集成，场景端口由受控测试替身提供。</summary>
    public sealed class EmberTransitionLoadingTests
    {
        private EUIViewEngine _engine;
        private EUIManager _ui;
        private GameObject _root, _pageObject;
        private EUIPage _page;
        private EUIPageDef _def, _previousDefault;
        private Func<SceneTransitionContext, Func<UniTask>, UniTask> _previousWrapper;

        [SetUp]
        public void Setup()
        {
            _previousWrapper = SceneCoordinator.WrapTransitionAsync;
            _previousDefault = EUIManager.DefaultLoadingPageDef;
            _root = new GameObject("TransitionLoadingTest", typeof(RectTransform));
            _pageObject = new GameObject("TestCurtain", typeof(RectTransform), typeof(CanvasGroup), typeof(Canvas));
            _page = new EUIPage(_pageObject);
            _engine = new EUIViewEngine();
            Set(_engine, "_uiRoot", _root.transform);
            Set(_engine, "_pageContext", new EUIPageContext(_engine));
            Set(_engine, "_initialized", true);
            _engine.AutoAdjustCanvasScaler = false;
            _ui = new EUIManager();
            ((IEmberManager)_ui).Init();
            Set(_ui, "_viewEngine", _engine);
            _def = new EUIPageDef("test/curtain", UILayer.TopMost, PageType.TopMost);
            EUIManager.DefaultLoadingPageDef = _def;
            var preloaded = (System.Collections.Generic.Dictionary<string, EUIPage>)
                typeof(EUIManager).GetField("_preloadedPages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_ui);
            preloaded.Add(_def.PrefabPath, _page);
        }

        [TearDown]
        public void Teardown()
        {
            ((IEmberManager)_ui).Destroy();
            SceneCoordinator.WrapTransitionAsync = _previousWrapper;
            EUIManager.DefaultLoadingPageDef = _previousDefault;
            typeof(EUIPage).GetMethod("MarkDisposed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_page, null);
            Object.DestroyImmediate(_pageObject);
            Object.DestroyImmediate(_root);
        }

        [UnityTest]
        public IEnumerator PreparationFailure_ClosesRealCurtainAndRetainsOldScene()
        {
            var scenes = new Scenes();
            var fsm = Build(scenes, context => throw new InvalidOperationException("prepare failed"));
            var operation = fsm.RequestTransitionTo<Destination>();
            yield return PumpUntil(operation.Completion);
            Assert.That(operation.Completion.Result.Status, Is.EqualTo(EmberTransitionStatus.PreparationFailed));
            Assert.That(_page.IsOpened, Is.False);
            Assert.That(_engine.PageContext.FindOpenedPage(_def), Is.Null);
            Assert.That(scenes.OldLoaded, Is.True);
            Assert.That(scenes.NewLoaded, Is.False);
        }

        [UnityTest]
        public IEnumerator CancelledPreparation_ClosesCurtainDespiteCancelledToken()
        {
            var scenes = new Scenes();
            var ready = new UniTaskCompletionSource();
            bool preparing = false;
            var fsm = Build(scenes, context => { preparing = true; return ready.Task; });
            var operation = fsm.RequestTransitionTo<Destination>();
            for (int i = 0; i < 100 && !preparing; i++) { Pump(); yield return null; }
            Assert.That(preparing, Is.True);
            Assert.That(_page.IsOpened, Is.True);
            operation.Cancel(); ready.TrySetResult();
            yield return PumpUntil(operation.Completion);
            Assert.That(operation.Completion.Result.Status, Is.EqualTo(EmberTransitionStatus.Cancelled));
            Assert.That(_page.IsOpened, Is.False);
            Assert.That(scenes.OldLoaded, Is.True);
        }

        [UnityTest]
        public IEnumerator Success_CompletionIncludesCurtainClose()
        {
            var scenes = new Scenes();
            var fsm = Build(scenes, context =>
            {
                Assert.That(_page.IsOpened, Is.True);
                Assert.That(scenes.OldLoaded, Is.True);
                return UniTask.CompletedTask;
            });
            var operation = fsm.RequestTransitionTo<Destination>();
            yield return PumpUntil(operation.Completion);
            Assert.That(operation.Completion.Result.Status, Is.EqualTo(EmberTransitionStatus.Completed));
            Assert.That(_page.IsOpened, Is.False);
            Assert.That(scenes.NewLoaded, Is.True);
            Assert.That(scenes.OldLoaded, Is.False);
            Assert.That(operation.IsPrepared, Is.True);
            Assert.That(operation.IsRunning, Is.True);
            Assert.That(operation.CanInteract, Is.True);
        }

        [UnityTest]
        public IEnumerator OuterCurtain_InnerCompletionDoesNotGrantInteraction()
            => OuterCurtain(false, false);

        [UnityTest]
        public IEnumerator OuterCurtain_SkipSceneLoadStillWaitsForOwner()
            => OuterCurtain(true, false);

        [UnityTest]
        public IEnumerator OuterCurtain_OuterFailureDoesNotGrantInteraction()
            => OuterCurtain(false, true);

        private IEnumerator OuterCurtain(bool skipSceneLoad, bool failOuter)
        {
            var scenes = new Scenes { NewLoaded = skipSceneLoad };
            var fsm = Build(scenes, context => UniTask.CompletedTask);
            var release = new UniTaskCompletionSource();
            EmberTransitionOperation operation = null;
            var outer = _ui.RunWithLoadingAsync(_def, null, async () =>
            {
                operation = fsm.RequestTransitionTo<Destination>(skipSceneLoad: skipSceneLoad);
                Assert.That((await operation.Completion).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                await release.Task;
                if (failOuter) throw new InvalidOperationException("outer failed after commit");
            }).AsTask();
            for (int i = 0; i < 150 && (operation == null || !operation.Completion.IsCompleted); i++)
            { Pump(); yield return null; }
            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.Completion.IsCompleted, Is.True);
            Assert.That(operation.IsRunning, Is.True);
            Assert.That(operation.WhenInteractive.IsCompleted, Is.False);
            Assert.That(_page.IsOpened, Is.True);
            release.TrySetResult();
            yield return PumpUntil(outer);
            Assert.That(_page.IsOpened, Is.False);
            Assert.That(outer.IsFaulted, Is.EqualTo(failOuter));
            if (failOuter) Assert.That(outer.Exception.ToString(), Does.Contain("outer failed"));
            Assert.That(operation.WhenInteractive.IsCompleted, Is.True);
            Assert.That(operation.WhenInteractive.Result.Status, Is.EqualTo(failOuter
                ? EmberTransitionStatus.CleanupFailed : EmberTransitionStatus.Completed));
            Assert.That(operation.CanInteract, Is.EqualTo(!failOuter));
        }

        [UnityTest]
        public IEnumerator SlowPreparation_ExposesPendingBoundaryToLoading()
        {
            var scenes = new Scenes();
            var ready = new UniTaskCompletionSource();
            bool preparing = false;
            var fsm = Build(scenes, context => { preparing = true; return ready.Task; });
            var operation = fsm.RequestTransitionTo<Destination>();
            for (int i = 0; i < 150 && !preparing; i++) { Pump(); yield return null; }
            Assert.That(preparing && scenes.NewLoaded, Is.True);
            Assert.That(operation.IsPrepared, Is.False);
            Assert.That(operation.IsRunning, Is.False);
            Assert.That(operation.CanInteract, Is.False);
            Assert.That(_page.IsOpened, Is.True);
            ready.TrySetResult();
            yield return PumpUntil(operation.Completion);
            Assert.That(operation.CanInteract, Is.True);
        }

        [UnityTest]
        public IEnumerator ShutdownUnderOuterCurtain_FailsPendingInteraction()
        {
            var scenes = new Scenes();
            var fsm = Build(scenes, context => UniTask.CompletedTask);
            var release = new UniTaskCompletionSource();
            EmberTransitionOperation operation = null;
            var outer = _ui.RunWithLoadingAsync(_def, null, async () =>
            {
                operation = fsm.RequestTransitionTo<Destination>();
                await operation.Completion;
                await release.Task;
            }).AsTask();
            for (int i = 0; i < 150 && (operation == null || !operation.Completion.IsCompleted); i++)
            { Pump(); yield return null; }
            Assert.That(operation?.Completion.IsCompleted, Is.True);
            ((IEmberManager)_ui).Destroy();
            Assert.That(operation.WhenInteractive.IsCompleted, Is.True);
            Assert.That(operation.WhenInteractive.Result.Status, Is.EqualTo(EmberTransitionStatus.CleanupFailed));
            release.TrySetResult();
            yield return PumpUntil(outer);
            Assert.That(outer.IsFaulted, Is.True);
            Assert.That(outer.Exception, Is.Not.Null);
            Assert.That(operation.CanInteract, Is.False);
        }

        [UnityTest]
        public IEnumerator InPlace_UsesDefaultCurtainAndRetainsWorld()
        {
            var scenes = new Scenes(); var ready = new UniTaskCompletionSource();
            var state = new InPlaceHost { Prepare = request => ready.Task };
            var fsm = new EmberStateMachine(); fsm.Register(state); fsm.Start<InPlaceHost>();
            var runner = new EmberSceneTransitionRunner(scenes);
            fsm.OnSceneTransitionAsync = request => SceneCoordinator.WrapTransitionAsync(request, () => runner.RunAsync(request));
            var operation = fsm.RequestInPlace<InPlaceHost>();
            for (int i = 0; i < 100 && !state.Preparing; i++) { Pump(); yield return null; }
            Assert.That(state.Preparing, Is.True); Assert.That(_page.IsOpened, Is.True);
            Assert.That(operation.IsPrepared, Is.False); Assert.That(operation.CanInteract, Is.False);
            ready.TrySetResult(); yield return PumpUntil(operation.Completion);
            Assert.That(operation.Completion.Result.Status, Is.EqualTo(EmberTransitionStatus.Completed));
            Assert.That(operation.CanInteract, Is.True); Assert.That(_page.IsOpened, Is.False);
            Assert.That(state.Enters, Is.EqualTo(1)); Assert.That(state.Commits, Is.EqualTo(1));
            Assert.That(scenes.OldLoaded, Is.True); Assert.That(scenes.NewLoaded, Is.False);
        }

        [UnityTest]
        public IEnumerator ManualCurtain_HoldsWithoutSceneUntilExplicitReveal()
        {
            var session = _ui.BeginCurtain(_def);
            var covered = session.Covered.AsTask();
            yield return PumpUntil(covered);
            Assert.That(covered.IsCompletedSuccessfully, Is.True);
            for (int i = 0; i < 5; i++) { Pump(); yield return null; }
            Assert.That(session.State, Is.EqualTo(EUICurtainState.Covered));
            Assert.That(_page.IsOpened, Is.True);
            Assert.Throws<InvalidOperationException>(() => _ui.BeginCurtain(_def));
            var reveal = session.RevealAsync().AsTask();
            var duplicate = session.RevealAsync().AsTask();
            yield return PumpUntil(reveal);
            yield return PumpUntil(duplicate);
            Assert.That(reveal.IsCompletedSuccessfully && duplicate.IsCompletedSuccessfully, Is.True);
            Assert.That(_page.State, Is.EqualTo(PageState.Unloaded));
            Assert.That(_page.Logic, Is.Null);
            Assert.That(_engine.ActivePages.Count, Is.Zero);
            session.Dispose(); session.Cancel();
            Assert.That(_page.IsOpened, Is.False);
        }

        [UnityTest]
        public IEnumerator ManualCurtain_InPlaceInternalPauseFinishesBeforeReveal()
        {
            float previous = Time.timeScale;
            EUICurtainSession session = null;
            try
            {
                Time.timeScale = 0f;
                session = _ui.BeginCurtain(_def);
                yield return PumpUntil(session.Covered.AsTask());
                var state = new InPlaceHost { Prepare = request => UniTask.CompletedTask };
                var fsm = new EmberStateMachine(); fsm.Register(state); fsm.Start<InPlaceHost>();
                var scenes = new Scenes();
                var runner = new EmberSceneTransitionRunner(scenes);
                fsm.OnSceneTransitionAsync = request => SceneCoordinator.WrapTransitionAsync(request, () => runner.RunAsync(request));
                var operation = fsm.RequestInPlace<InPlaceHost>();
                yield return PumpUntil(operation.Completion);
                Assert.That(operation.WhenPrepared.Result.Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(operation.WhenRunning.Result.Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(operation.WhenInteractive.IsCompleted, Is.False);
                Assert.That(session.State, Is.EqualTo(EUICurtainState.Covered));
                Assert.That(scenes.OldLoaded && !scenes.NewLoaded, Is.True);
                Time.timeScale = previous;
                yield return PumpUntil(session.RevealAsync().AsTask());
                Assert.That(operation.CanInteract, Is.True);
            }
            finally { session?.Dispose(); Time.timeScale = previous; }
        }

        [UnityTest]
        public IEnumerator ManualCurtain_DisposeDuringOpenSettlesWaitAndReleasesOwnership()
        {
            var session = _ui.BeginCurtain(_def);
            var covered = session.Covered.AsTask();
            session.Dispose();
            yield return PumpUntil(covered);
            Assert.That(session.Covered.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(covered.Exception?.InnerException, Is.InstanceOf<OperationCanceledException>());
            for (int i = 0; i < 3; i++) { Pump(); yield return null; }
            Assert.That(_engine.PageContext.FindOpenedPage(_def), Is.Null);
            Assert.That(typeof(EUIManager).GetField("_curtainSession", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_ui), Is.Null);
        }

        [UnityTest]
        public IEnumerator ManualCurtain_DestroyedViewSettlesRevealAndOwner()
        {
            var session = _ui.BeginCurtain(_def);
            yield return PumpUntil(session.Covered.AsTask());
            Object.DestroyImmediate(_pageObject);
            yield return null;
            Assert.That(session.State, Is.EqualTo(EUICurtainState.Released));
            Assert.That(session.RevealFinished.Status.IsCompleted(), Is.True);
            session.Dispose();
        }

        [UnityTest]
        public IEnumerator RebirthTiming_CommitThenReveal_ControlsRemainOwnedByBusiness()
        {
            AddRealBlocks();
            float previous = Time.timeScale;
            var session = _ui.BeginCurtain(_def, options: new EUICurtainOptions {
                EnterDuration = 0.75f, ExitDuration = 0.6666667f,
                SkipFakeProgress = true, SkipCustomTransition = true
            });
            try
            {
                yield return PumpUntil(session.Covered.AsTask());
                Time.timeScale = 0f; // recovery owns its internal pause
                var state = new InPlaceHost { Prepare = request => UniTask.CompletedTask };
                var fsm = new EmberStateMachine(); fsm.Register(state); fsm.Start<InPlaceHost>();
                var scenes = new Scenes();
                var runner = new EmberSceneTransitionRunner(scenes);
                fsm.OnSceneTransitionAsync = context => SceneCoordinator.WrapTransitionAsync(context, () => runner.RunAsync(context));
                var operation = fsm.RequestInPlace<InPlaceHost>();
                yield return PumpUntil(operation.Completion);
                Assert.That(operation.Completion.Result.Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(operation.WhenPrepared.IsCompleted && operation.WhenRunning.IsCompleted, Is.True);
                Assert.That(operation.WhenInteractive.IsCompleted, Is.False);
                Assert.That(session.State, Is.EqualTo(EUICurtainState.Covered));
                Time.timeScale = 1f; // recovery can release before presentation finishes
                float elapsed = 0f;
                while (elapsed < 0.2f) { Pump(); yield return null; elapsed += Time.deltaTime; }
                var reveal = session.RevealAsync().AsTask();
                bool actionStarted = false, controlsAllowed = false, revealBeforeControl = false;
                while (elapsed < 0.5f + 0.53333336f)
                {
                    Pump(); yield return null; elapsed += Time.deltaTime;
                    if (elapsed >= 0.5f) actionStarted = true;
                    if (reveal.IsCompleted && elapsed < 1f)
                    {
                        revealBeforeControl = true;
                        Assert.That(controlsAllowed, Is.False);
                        Assert.That(actionStarted, Is.True);
                    }
                }
                controlsAllowed = true;
                Assert.That(actionStarted && controlsAllowed, Is.True);
                Assert.That(revealBeforeControl, Is.True, "揭幕已完成时，复生动作仍须独立持有控制。");
                Assert.That(reveal.IsCompletedSuccessfully, Is.True);
                yield return PumpUntil(operation.WhenInteractive);
                Assert.That(operation.CanInteract, Is.True);
                Assert.That(scenes.OldLoaded, Is.True); Assert.That(scenes.NewLoaded, Is.False);
            }
            finally { session.Dispose(); Time.timeScale = previous; }
        }

        [UnityTest]
        public IEnumerator SessionOptions_AppliedBeforeInit_AndSkipCustomTimers()
        {
            AddRealBlocks();
            _page.SetTransition(false, true, false, true, 0f, 0f);
            var logic = new OptionsProbe();
            Set(_page, "_logic", logic);
            var session = _ui.BeginCurtain(_def, options: new EUICurtainOptions {
                EnterDuration = 0f, ExitDuration = 0f, SkipFakeProgress = true, SkipCustomTransition = true
            });
            yield return PumpUntil(session.Covered.AsTask());
            Assert.That(logic.SkippedAtInit, Is.True);
            yield return PumpUntil(session.RevealAsync().AsTask());
            Assert.That(logic.CustomCalls, Is.Zero);
            Assert.That(logic.Disposed, Is.True);
        }

        private sealed class OptionsProbe : EUILogic
        {
            public bool SkippedAtInit, Disposed;
            public int CustomCalls;
            public override void OnResetDefault() { base.OnResetDefault(); SkipFakeProgress = false; }
            public override void OnInit() { SkippedAtInit = SkipFakeProgress; }
            public override UniTask OnCustomEnter() { CustomCalls++; return UniTask.CompletedTask; }
            public override UniTask OnCustomExit() { CustomCalls++; return UniTask.CompletedTask; }
            public override void OnDispose() { Disposed = true; }
        }

        [UnityTest]
        public IEnumerator RealBlocks_UserPauseFreezesBothDirections_DespiteInternalPause()
        {
            var block = AddRealBlocks();
            float previous = Time.timeScale;
            var session = _ui.BeginCurtain(_def, options: new EUICurtainOptions { EnterDuration = 0.3f, ExitDuration = 0.2f });
            try
            {
                Time.timeScale = 0f;
                for (int i = 0; i < 3; i++) { Pump(); yield return null; }
                Assert.That(block.HasActiveBlocks, Is.True);
                var rect = (RectTransform)block.transform.GetChild(0);
                session.SetPaused(true);
                var position = rect.anchoredPosition;
                var scale = rect.localScale;
                float until = Time.realtimeSinceStartup + 0.15f;
                while (Time.realtimeSinceStartup < until) { Pump(); yield return null; }
                Assert.That(rect.anchoredPosition, Is.EqualTo(position));
                Assert.That(rect.localScale, Is.EqualTo(scale));
                Assert.That(session.State, Is.EqualTo(EUICurtainState.Covering));
                session.SetPaused(false);
                yield return PumpUntil(session.Covered.AsTask());
                Assert.That(session.State, Is.EqualTo(EUICurtainState.Covered));
                // Slow business never triggers automatic reveal.
                for (int i = 0; i < 5; i++) { Pump(); yield return null; }
                Assert.That(session.State, Is.EqualTo(EUICurtainState.Covered));
                var reveal = session.RevealAsync().AsTask();
                for (int i = 0; i < 2; i++) { Pump(); yield return null; }
                session.SetPaused(true); position = rect.anchoredPosition; scale = rect.localScale;
                until = Time.realtimeSinceStartup + 0.15f;
                while (Time.realtimeSinceStartup < until) { Pump(); yield return null; }
                Assert.That(rect.anchoredPosition, Is.EqualTo(position));
                Assert.That(rect.localScale, Is.EqualTo(scale));
                Assert.That(reveal.IsCompleted, Is.False);
                session.SetPaused(false);
                yield return PumpUntil(reveal);
                Assert.That(reveal.IsCompletedSuccessfully, Is.True);
            }
            finally { session.Dispose(); Time.timeScale = previous; }
        }

        [UnityTest]
        public IEnumerator RealBlocks_CancelDuringPausedEnterSettlesAndCleans()
        {
            var block = AddRealBlocks();
            var session = _ui.BeginCurtain(_def, options: new EUICurtainOptions { InitiallyPaused = true });
            for (int i = 0; i < 3; i++) { Pump(); yield return null; }
            Assert.That(block.HasActiveBlocks, Is.True);
            session.Cancel();
            Assert.That(session.Covered.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(block.HasActiveBlocks, Is.False);
            Assert.That(_engine.PageContext.FindOpenedPage(_def), Is.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RealBlocks_DestroyDuringEnterSettlesCovered()
        {
            AddRealBlocks();
            var session = _ui.BeginCurtain(_def, options: new EUICurtainOptions { InitiallyPaused = true });
            for (int i = 0; i < 3; i++) { Pump(); yield return null; }
            Object.DestroyImmediate(_pageObject);
            for (int i = 0; i < 5 && session.State != EUICurtainState.Released; i++) { Pump(); yield return null; }
            Assert.That(session.Covered.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(_engine.PageContext.FindOpenedPage(_def), Is.Null);
        }

        [UnityTest]
        public IEnumerator RealBlocks_CancelRevealIgnoresPauseAndReleases()
        {
            var block = AddRealBlocks();
            var session = _ui.BeginCurtain(_def, options: new EUICurtainOptions { EnterDuration = 0.05f });
            yield return PumpUntil(session.Covered.AsTask());
            session.SetPaused(true);
            using var cancellation = new CancellationTokenSource();
            var reveal = session.RevealAsync(cancellation.Token).AsTask();
            Pump(); yield return null;
            cancellation.Cancel();
            yield return PumpUntil(reveal);
            Assert.That(session.State, Is.EqualTo(EUICurtainState.Released));
            Assert.That(block == null || !block.HasActiveBlocks, Is.True);
            Assert.That(_engine.PageContext.FindOpenedPage(_def), Is.Null);
        }

        [UnityTest]
        public IEnumerator OldOwnerLateCompletionCannotCloseReplacementCurtain()
        {
            var oldRelease = new UniTaskCompletionSource();
            bool entered = false;
            var oldTask = _ui.RunWithLoadingAsync(_def, null, async () => { entered = true; await oldRelease.Task; }).AsTask();
            for (int i = 0; i < 100 && !entered; i++) { Pump(); yield return null; }
            Assert.That(entered, Is.True);
            var old = (EUICurtainSession)typeof(EUIManager).GetField("_curtainSession", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_ui);
            old.Dispose();
            yield return null;
            _pageObject = new GameObject("Replacement", typeof(RectTransform), typeof(CanvasGroup), typeof(Canvas));
            _page = new EUIPage(_pageObject);
            var preloaded = (System.Collections.Generic.Dictionary<string, EUIPage>)typeof(EUIManager).GetField("_preloadedPages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_ui);
            preloaded[_def.PrefabPath] = _page;
            var replacement = _ui.BeginCurtain(_def);
            yield return PumpUntil(replacement.Covered.AsTask());
            oldRelease.TrySetResult();
            yield return PumpUntil(oldTask);
            Assert.That(oldTask.IsFaulted, Is.True);
            Assert.That(oldTask.Exception, Is.Not.Null);
            old.Cancel(); old.SetPaused(true);
            Assert.That(replacement.State, Is.EqualTo(EUICurtainState.Covered));
            Assert.That(replacement.IsPaused, Is.False);
            Assert.That(_page.IsOpened, Is.True);
            yield return PumpUntil(replacement.RevealAsync().AsTask());
        }

        [UnityTest]
        public IEnumerator RealBlocks_ScaledClockIsExplicitOptIn()
        {
            AddRealBlocks();
            float previous = Time.timeScale;
            Time.timeScale = 0f;
            var session = _ui.BeginCurtain(_def, options: new EUICurtainOptions { UseScaledTime = true, EnterDuration = 0.08f, ExitDuration = 0.05f });
            try
            {
                for (int i = 0; i < 5; i++) { Pump(); yield return null; }
                Assert.That(session.Covered.Status, Is.EqualTo(UniTaskStatus.Pending));
                Time.timeScale = 1f;
                yield return PumpUntil(session.Covered.AsTask());
                yield return PumpUntil(session.RevealAsync().AsTask());
            }
            finally { session.Dispose(); Time.timeScale = previous; }
        }

        private Ember.UIExtension.EUITransitionBlock AddRealBlocks()
        {
            var go = new GameObject("Blocks", typeof(RectTransform));
            go.transform.SetParent(_pageObject.transform, false);
            var block = go.AddComponent<Ember.UIExtension.EUITransitionBlock>();
            JsonUtility.FromJsonOverwrite("{\"_autoGridSize\":false,\"_fixedColumns\":3}", block);
            _page.SetTransition(false, true, false, false, 0f, 0f);
            return block;
        }

        [UnityTest]
        public IEnumerator ResourceException_SettlesCoverAndDoesNotPoisonQueue()
        {
            ClearPreloaded();
            var provider = new DeferredProvider { ThrowOnLoad = true };
            _engine.ResourceProvider = provider;
            var failed = _ui.BeginCurtain(_def);
            Pump(); yield return null;
            Assert.That(failed.Covered.Status, Is.EqualTo(UniTaskStatus.Faulted));
            provider.ThrowOnLoad = false;
            var next = _ui.BeginCurtain(_def);
            Pump(); provider.Callbacks[0](_pageObject);
            yield return PumpUntil(next.Covered.AsTask());
            yield return PumpUntil(next.RevealAsync().AsTask());
        }

        [UnityTest]
        public IEnumerator CancelInPageCreated_DoesNotRouteOrLeakUninitializedLogic()
        {
            var session = _ui.BeginCurtain(_def);
            var logic = new OptionsProbe();
            Action<EUIPage> created = page => { Set(page, "_logic", logic); session.Cancel(); };
            EUIManager.OnPageCreated += created;
            try
            {
                Pump(); yield return null; Pump();
                Assert.That(session.Covered.Status, Is.EqualTo(UniTaskStatus.Canceled));
                Assert.That(_engine.ActivePages.Count, Is.Zero);
                Assert.That(_engine.PageContext.FindOpenedPage(_def), Is.Null);
                Assert.That(logic.Disposed, Is.True);
            }
            finally { EUIManager.OnPageCreated -= created; }
        }

        [UnityTest]
        public IEnumerator DuplicateAndLateResourceCallbacks_CannotRouteSecondPage()
        {
            ClearPreloaded();
            var provider = new DeferredProvider(); _engine.ResourceProvider = provider;
            var old = _ui.BeginCurtain(_def); Pump(); old.Cancel();
            var current = _ui.BeginCurtain(_def); Pump();
            provider.Callbacks[1](_pageObject);
            yield return PumpUntil(current.Covered.AsTask());
            provider.Callbacks[0](_pageObject); // late cancelled acquisition
            provider.Callbacks[1](_pageObject); // duplicate notification, not a second acquisition
            Pump(); yield return null; Pump();
            Assert.That(_engine.ActivePages.Count, Is.EqualTo(1));
            Assert.That(current.State, Is.EqualTo(EUICurtainState.Covered));
            Assert.That(provider.Releases, Is.EqualTo(1));
            yield return PumpUntil(current.RevealAsync().AsTask());
        }

        private void ClearPreloaded() => ((System.Collections.IDictionary)typeof(EUIManager)
            .GetField("_preloadedPages", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_ui)).Clear();

        [UnityTest]
        public IEnumerator ExternalClose_SettlesOwnerInsteadOfLeavingCoveredHandle()
        {
            var session = _ui.BeginCurtain(_def);
            yield return PumpUntil(session.Covered.AsTask());
            _ui.ClosePage(_page);
            Assert.That(session.RevealFinished.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(_engine.ActivePages.Count, Is.Zero);
            Assert.That(_engine.PageContext.FindOpenedPage(_def), Is.Null);
        }

        [UnityTest]
        public IEnumerator ThrowingCancellationAndEffectCleanup_StillReleasesPageAndTasks()
        {
            _pageObject.AddComponent<ThrowingCleanupEffect>();
            _page.SetTransition(false, true, false, false, 0f, 0f);
            var session = _ui.BeginCurtain(_def);
            yield return PumpUntil(session.Covered.AsTask());
            Assert.DoesNotThrow(session.Cancel);
            Assert.That(session.RevealFinished.Status, Is.EqualTo(UniTaskStatus.Faulted));
            Assert.That(_engine.ActivePages.Count, Is.Zero);
            Assert.That(_engine.PageContext.FindOpenedPage(_def), Is.Null);
            Assert.That(_pageObject == null || !_pageObject.activeSelf, Is.True);
            Assert.DoesNotThrow(session.Dispose);
        }

        [UnityTest]
        public IEnumerator ExternalViewEngineClose_SettlesOwnerWithoutWaitingForDelayedDestroy()
        {
            var session = _ui.BeginCurtain(_def);
            yield return PumpUntil(session.Covered.AsTask());
            _engine.ClosePage(_page);
            Pump(); yield return null; Pump(); yield return null;
            Assert.That(session.RevealFinished.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(_engine.ActivePages.Count, Is.Zero);
        }

        public sealed class ThrowingCleanupEffect : MonoBehaviour, IEUIControlledTransitionEffect
        {
            public bool HasActiveBlocks => true;
            public UniTask PlayEnterAsync(EUITransitionPlayback playback, float duration = -1f)
            {
                playback.Cancellation.Register(() => throw new InvalidOperationException("cancel cleanup failed"));
                return UniTask.CompletedTask;
            }
            public UniTask PlayExitAsync(EUITransitionPlayback playback, float duration = -1f) => UniTask.CompletedTask;
            public UniTask PlayEnterAsync(float duration = -1f) => UniTask.CompletedTask;
            public UniTask PlayExitAsync(float duration = -1f) => UniTask.CompletedTask;
            public void HideAllImmediate() => throw new InvalidOperationException("effect cleanup failed");
        }

        private sealed class DeferredProvider : IEUIResourceProvider
        {
            public bool ThrowOnLoad;
            public int Releases;
            public readonly System.Collections.Generic.List<Action<GameObject>> Callbacks = new();
            public void LoadPrefabAsync(string path, Action<GameObject> callback)
            {
                if (ThrowOnLoad) throw new InvalidOperationException("provider failure");
                Callbacks.Add(callback);
            }
            public void Release(string path) { Releases++; }
        }

        private sealed class InPlaceHost : EmberGameState, IEmberInPlaceState
        {
            public Func<SceneTransitionContext, UniTask> Prepare;
            public bool Preparing;
            public int Enters, Commits;
            public override string ScenePath => "old";
            public override TransitionDescriptor[] GetEdges() => new[] { new TransitionDescriptor(typeof(InPlaceHost), "replace") };
            public override void OnEnter(object args) => Enters++;
            public UniTask PrepareInPlaceAsync(SceneTransitionContext request) { Preparing = true; return Prepare(request); }
            public void CommitInPlace(SceneTransitionContext request) => Commits++;
            public UniTask AbortInPlaceAsync(SceneTransitionContext request) => UniTask.CompletedTask;
        }

        private EmberStateMachine Build(Scenes scenes, Func<SceneTransitionContext, UniTask> prepare)
        {
            var fsm = new EmberStateMachine();
            fsm.Register(new Origin()); fsm.Register(new Destination { Prepare = prepare });
            fsm.Start<Origin>();
            var runner = new EmberSceneTransitionRunner(scenes);
            fsm.OnSceneTransitionAsync = context => SceneCoordinator.WrapTransitionAsync(context, () => runner.RunAsync(context));
            return fsm;
        }

        private IEnumerator PumpUntil(Task task)
        {
            float deadline = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < deadline && !task.IsCompleted) { Pump(); yield return null; }
            Assert.That(task.IsCompleted, Is.True, "转场/遮罩清理未在 4 秒内完成。");
        }

        private void Pump() { ((IEmberUpdate)_ui).Update(); ((IEmberUpdate)_engine).Update(); }
        private static void Set(object target, string name, object value)
            => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private sealed class Origin : EmberGameState
        {
            public override string ScenePath => "old";
            public override TransitionDescriptor[] GetEdges() => new[] { new TransitionDescriptor(typeof(Destination), "next") };
        }
        private sealed class Destination : EmberGameState
        {
            public Func<SceneTransitionContext, UniTask> Prepare;
            public override string ScenePath => "next";
            public override UniTask PrepareEnterAsync(SceneTransitionContext context) => Prepare(context);
        }
        private sealed class Scenes : IEmberTransitionScenes
        {
            public bool OldLoaded = true, NewLoaded;
            public bool IsSceneLoaded(string scene) => scene == "old" ? OldLoaded : NewLoaded;
            public UniTask<EmberTransitionSceneLoad> LoadForTransitionAsync(string scene, CancellationToken cancellation)
            { NewLoaded = true; return UniTask.FromResult(new EmberTransitionSceneLoad(true)); }
            public UniTask UnloadForTransitionAsync(string scene)
            { if (scene == "old") OldLoaded = false; else NewLoaded = false; return UniTask.CompletedTask; }
            public void SetCurrentScene(string scene) { }
        }
    }
}
