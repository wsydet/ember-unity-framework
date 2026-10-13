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
            for (int i = 0; i < 150 && !task.IsCompleted; i++) { Pump(); yield return null; }
            Assert.That(task.IsCompleted, Is.True, "转场/遮罩清理未在有界帧数内完成。");
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
