using System;
using System.Collections;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Ember.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.SceneFlow.Tests
{
    /// <summary>读取模板真实 Loading 类型，避免测试程序集反向依赖 Assembly-CSharp。</summary>
    public sealed class GameLoadingProgressTests
    {
        private EUILogic _logic;
        private object _settings;
        private GameObject _root;
        private Image _bar;
        private float _timeScale;
        private bool _quickSceneLoad;

        [SetUp]
        public void Setup()
        {
            _timeScale = Time.timeScale;
            _quickSceneLoad = EmberStateMachine.QuickSceneLoad;
            EmberStateMachine.QuickSceneLoad = false;
            _logic = (EUILogic)Activator.CreateInstance(Type.GetType("Game.UI.EUILoadingPage, Assembly-CSharp", true));
            _settings = Activator.CreateInstance(Type.GetType("Game.UI.EUILoadingPageSettings, Assembly-CSharp", true));
            SetSetting("fastFillDuration", 0f); SetSetting("tailDuration", 0f); SetSetting("customEnterDuration", 0f);
            _root = new GameObject("LoadingProgressTest", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _bar = _root.GetComponent<Image>();
            SetField("Img_ProgressBar", _bar); SetField("Cg_Progress", _root.GetComponent<CanvasGroup>());
            _logic.CustomSettings = _settings;
            _logic.OnInit(); _logic.OnShow();
            _logic.OnCustomEnter().GetAwaiter().GetResult();
        }

        [TearDown]
        public void Teardown()
        {
            Time.timeScale = _timeScale;
            EmberStateMachine.QuickSceneLoad = _quickSceneLoad;
            Object.DestroyImmediate(_root);
        }

        [UnityTest]
        public IEnumerator SlowBusinessPreparationHoldsBelow100EvenWhenSceneIsIdle()
        {
            var ready = new UniTaskCompletionSource();
            var fsm = new EmberStateMachine();
            fsm.Register(new Destination { Ready = ready });
            var operation = fsm.RequestTransitionTo<Destination>();
            SetProperty("TransitionOperation", operation);
            for (int i = 0; i < 5; i++) { _logic.OnUpdate(); yield return null; }
            Assert.That(_bar.fillAmount, Is.LessThan(1f));
            Assert.That(_logic.IsTransitionReady || operation.IsRunning, Is.False);
            ready.TrySetResult();
            for (int i = 0; i < 100 && !operation.Completion.IsCompleted; i++) yield return null;
            _logic.OnUpdate();
            Assert.That(operation.IsPrepared, Is.True);
            Assert.That(_bar.fillAmount, Is.EqualTo(1f));
            Assert.That(_logic.IsTransitionReady, Is.True);
            fsm.StopTransitions();
        }

        [Test]
        public void QuickPathStillRequiresSuccessfulWorkAndHidesProgress()
        {
            _logic.SkipFakeProgress = true; _logic.OnUpdate();
            Assert.That(_bar.gameObject.activeSelf, Is.False);
            Assert.That(_logic.IsTransitionReady, Is.False);
            SetProperty("LoadingWorkCompleted", true);
            Assert.That(_logic.IsTransitionReady, Is.True);
        }

        [Test]
        public void GenericWorkAndManualProgressCannotReach100Early()
        {
            _logic.OnUpdate();
            _logic.GetType().GetMethod("SetProgress").Invoke(_logic, new object[] { 1f });
            Assert.That(_bar.fillAmount, Is.LessThan(1f));
            Assert.That(_logic.IsTransitionReady, Is.False);
            SetProperty("LoadingWorkCompleted", true); _logic.OnUpdate();
            Assert.That(_logic.IsTransitionReady, Is.True);
        }

        [UnityTest]
        public IEnumerator ProgressAndCustomFadeCompleteAtZeroTimeScale()
        {
            Time.timeScale = 0f;
            SetSetting("fastFillDuration", 0.03f); SetSetting("tailDuration", 0.03f);
            SetSetting("customEnterDuration", 0.03f); SetSetting("customExitDuration", 0.03f);
            SetProperty("LoadingWorkCompleted", true);
            var enter = _logic.OnCustomEnter().AsTask();
            for (int i = 0; i < 120 && !enter.IsCompleted; i++) yield return null;
            Assert.That(enter.IsCompleted, Is.True);
            for (int i = 0; i < 120 && !_logic.IsTransitionReady; i++) { _logic.OnUpdate(); yield return null; }
            Assert.That(_logic.IsTransitionReady, Is.True);
            var exit = _logic.OnCustomExit().AsTask();
            for (int i = 0; i < 120 && !exit.IsCompleted; i++) yield return null;
            Assert.That(exit.IsCompleted, Is.True);
            Assert.That(_root.GetComponent<CanvasGroup>().alpha, Is.Zero);
        }

        private void SetField(string name, object value)
            => _logic.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_logic, value);
        private void SetSetting(string name, object value) => _settings.GetType().GetField(name).SetValue(_settings, value);
        private void SetProperty(string name, object value) => typeof(EUILogic).GetProperty(name).SetValue(_logic, value);

        private sealed class Destination : EmberGameState
        {
            public UniTaskCompletionSource Ready;
            public override UniTask PrepareEnterAsync(SceneTransitionContext context) => Ready.Task;
        }
    }
}
