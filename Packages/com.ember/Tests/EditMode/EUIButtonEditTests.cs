// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System.Reflection;
using Ember.UIExtension;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ember.UI.Tests
{
    public class EUIButtonEditTests
    {
        private GameObject _root;
        private GameObject _events;
        private EUIButton _control;
        private Button _button;
        private GameObject _enabledNode;
        private GameObject _disabledNode;
        private int _clicks;

        [SetUp]
        public void SetUp()
        {
            _clicks = 0;
            _control = null;
            _button = null;
            _enabledNode = null;
            _disabledNode = null;
            _root = new GameObject("ButtonFixture", typeof(RectTransform));
            _events = new GameObject("ButtonEvents", typeof(EventSystem));
        }

        [TearDown]
        public void TearDown()
        {
            _control?.DoDispose();
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_events);
        }

        private void Create(bool enhanced = true, bool stateNodes = true)
        {
            _button = enhanced ? _root.AddComponent<EUIButtonEx>() : _root.AddComponent<Button>();
            if (enhanced && stateNodes)
            {
                _enabledNode = new GameObject("Enabled");
                _disabledNode = new GameObject("Disabled");
                _enabledNode.transform.SetParent(_root.transform);
                _disabledNode.transform.SetParent(_root.transform);
                SetField(_button, "_enableNode", _enabledNode);
                SetField(_button, "_disableNode", _disabledNode);
                ((EUIButtonEx)_button).EnableState = true;
            }
            _control = new EUIButton();
            typeof(EUIComponent).GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_control, new object[] { _root });
            _control.OnClick = _ => _clicks++;
        }

        private void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private void Click(PointerEventData.InputButton button = PointerEventData.InputButton.Left)
        {
            var data = new PointerEventData(_events.GetComponent<EventSystem>()) { button = button };
            ExecuteEvents.Execute(_root, data, ExecuteEvents.pointerClickHandler);
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void GrayClick_InvokesConditionHint_RegardlessOfConfigurationOrder(bool flagFirst, bool stateNodes)
        {
            Create(stateNodes: stateNodes);
            if (flagFirst) _control.CanClickWhenDisable = true;
            _control.Enable = false;
            if (!flagFirst) _control.CanClickWhenDisable = true;
            var hints = 0;
            var actions = 0;
            _control.OnClick = c => { if (!c.Enable) hints++; else actions++; };
            Click();
            Assert.IsFalse(_control.Enable);
            Assert.IsTrue(_button.interactable);
            Assert.AreEqual(1, hints);
            Assert.AreEqual(0, actions);
            if (stateNodes)
            {
                Assert.IsFalse(_enabledNode.activeSelf);
                Assert.IsTrue(_disabledNode.activeSelf);
            }
            _control.Enable = true;
            Click();
            Assert.AreEqual(1, hints);
            Assert.AreEqual(1, actions);
        }

        [Test]
        public void GrayClick_TurningPermissionOff_BlocksImmediately()
        {
            Create();
            _control.Enable = false;
            Click();
            Assert.AreEqual(0, _clicks);
            _control.CanClickWhenDisable = true;
            Click();
            Assert.AreEqual(1, _clicks);
            _control.CanClickWhenDisable = false;
            Click();
            Assert.IsFalse(_button.interactable);
            Assert.AreEqual(1, _clicks);
        }

        [Test]
        public void GrayClick_RespectsCanvasGroupAndExplicitInputBlocking()
        {
            Create();
            _control.CanClickWhenDisable = true;
            _control.Enable = false;
            var group = _root.AddComponent<CanvasGroup>();
            group.interactable = false;
            Click();
            _button.onClick.Invoke();
            Assert.AreEqual(0, _clicks);
            group.interactable = true;
            Click();
            Assert.AreEqual(1, _clicks);
            _button.interactable = false;
            _button.onClick.Invoke();
            Assert.AreEqual(1, _clicks);
        }

        [Test]
        public void GrayClick_SubmitUsesSameCallback_AndRightClickIsIgnored()
        {
            Create();
            _control.Enable = false;
            _control.CanClickWhenDisable = true;
            Click(PointerEventData.InputButton.Right);
            Assert.AreEqual(0, _clicks);
            ExecuteEvents.Execute(_root, new BaseEventData(_events.GetComponent<EventSystem>()), ExecuteEvents.submitHandler);
            Assert.AreEqual(1, _clicks);
        }

        [Test]
        public void PlainButton_DisabledStateCannotBeBypassed()
        {
            Create(enhanced: false);
            _control.Enable = false;
            _control.CanClickWhenDisable = true;
            Click();
            _button.onClick.Invoke();
            Assert.IsFalse(_control.Enable);
            Assert.AreEqual(0, _clicks);
        }

        [Test]
        public void GrayClick_HiddenOrDisabledComponentCannotInvoke()
        {
            Create();
            _control.Enable = false;
            _control.CanClickWhenDisable = true;
            _root.SetActive(false);
            _button.onClick.Invoke();
            _root.SetActive(true);
            _button.enabled = false;
            _button.onClick.Invoke();
            Assert.AreEqual(0, _clicks);
        }

        [Test]
        public void GrayClick_LongPressReleaseDoesNotAlsoClick()
        {
            Create();
            _control.Enable = false;
            _control.CanClickWhenDisable = true;
            _control.OnLongPress = (_, __) => { };
            EUIEventTriggerListener.Get(_root).onLongPressTime.Invoke(_root, false);
            Click();
            Assert.AreEqual(0, _clicks);
        }

        [Test]
        public void DirectButtonBinding_SetEnabledSupportsHintsWithoutWrapper()
        {
            var button = _root.AddComponent<EUIButtonEx>();
            Assert.IsTrue(button.EnableState);
            var hints = 0;
            var actions = 0;
            button.onClick.AddListener(() => { if (!button.EnableState) hints++; else actions++; });
            button.SetEnabled(false, canClickWhenDisable: true);
            Click();
            Assert.IsFalse(button.EnableState);
            Assert.AreEqual(1, hints);
            Assert.AreEqual(0, actions);
            button.SetEnabled(false);
            Click();
            Assert.AreEqual(1, hints);
            button.SetEnabled(true);
            Click();
            Assert.AreEqual(1, actions);
        }

        [Test]
        public void VisualStateOnly_DoesNotChangeInputPermission_AndWrapperObservesChanges()
        {
            Create(stateNodes: false);
            var button = (EUIButtonEx)_button;
            button.EnableState = false;
            Assert.IsFalse(_control.Enable);
            Assert.IsTrue(button.interactable);
            Click();
            Assert.AreEqual(0, _clicks);
            button.EnableState = true;
            Click();
            Assert.AreEqual(1, _clicks);
        }

        [Test]
        public void GrayClick_GlobalPointerCallbackStillRunsOnce()
        {
            Create();
            _control.Enable = false;
            _control.CanClickWhenDisable = true;
            var previous = EUIEventTriggerListener.GlobalClickCallback;
            var globalClicks = 0;
            EUIEventTriggerListener.GlobalClickCallback = _ => globalClicks++;
            try
            {
                Click();
                Assert.AreEqual(1, _clicks);
                Assert.AreEqual(1, globalClicks);
            }
            finally { EUIEventTriggerListener.GlobalClickCallback = previous; }
        }

        [Test]
        public void GenericComponent_ClickAndDisposalKeepIndependentListener()
        {
            var component = new EUIComponent();
            typeof(EUIComponent).GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(component, new object[] { _root });
            var listener = EUIEventTriggerListener.Get(_root);
            var otherClicks = 0;
            listener.onClick += _ => otherClicks++;
            component.OnClick = _ => _clicks++;
            try
            {
                Click();
                Assert.AreEqual(1, _clicks);
                component.DoDispose();
                Click();
                Assert.AreEqual(1, _clicks);
                Assert.AreEqual(2, otherClicks);
            }
            finally { component.DoDispose(); }
        }

        [Test]
        public void CallbackReplacementAndDisposal_PreserveOtherButtonListeners()
        {
            Create();
            var otherClicks = 0;
            _button.onClick.AddListener(() => otherClicks++);
            _control.OnClick = _ => _clicks += 2;
            Click();
            Assert.AreEqual(2, _clicks);
            Assert.AreEqual(1, otherClicks);
            _control.DoDispose();
            Click();
            Assert.AreEqual(2, _clicks);
            Assert.AreEqual(2, otherClicks);
        }
    }
}
