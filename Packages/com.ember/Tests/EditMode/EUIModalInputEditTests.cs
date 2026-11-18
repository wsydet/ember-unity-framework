using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ember.UI.Tests
{
    public sealed class EUIModalInputEditTests
    {
        private readonly List<GameObject> _objects = new();
        private readonly List<EUIPage> _pages = new();
        private readonly Dictionary<EUIPage, GameObject> _masks = new();
        private Component _scope;
        private EventSystem _events;

        private GameObject Create(string name, params System.Type[] types)
        { var go = new GameObject(name, types); _objects.Add(go); return go; }
        private EUIPage Page(string name, int order)
        {
            var go = Create(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            var page = new EUIPage(go); go.GetComponent<Canvas>().sortingOrder = order;
            typeof(EUIPage).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(page, PageState.Opened);
            page.CanvasGroup.alpha = 1; page.CanvasGroup.interactable = true;
            _pages.Add(page); return page;
        }
        private Button Button(EUIPage page)
        {
            var go = Create(page.Name + " button", typeof(RectTransform), typeof(Button));
            go.transform.SetParent(page.Transform, false); return go.GetComponent<Button>();
        }
        private void Refresh() { _scope.GetType().GetMethod("Refresh", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_scope, null); }

        [SetUp]
        public void SetUp()
        {
            _events = Create("Modal test events", typeof(EventSystem)).GetComponent<EventSystem>();
            // EventSystem is not ExecuteAlways; EditMode fixtures must enter its lifecycle explicitly.
            typeof(EventSystem).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_events, null);
            _scope = Create("Modal input owner").AddComponent(typeof(EUIPage).Assembly.GetType("Ember.UI.EUIModalInputScope"));
            _scope.GetType().GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_scope, new object[] { _pages, _masks, null });
        }
        [TearDown]
        public void TearDown()
        {
            // Dispose the input owner while its dependency lists still exist.
            Object.DestroyImmediate(_scope);
            typeof(EventSystem).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_events, null);
            for (int i = _objects.Count - 1; i >= 0; i--) if (_objects[i]) Object.DestroyImmediate(_objects[i]);
            _objects.Clear(); _pages.Clear(); _masks.Clear();
        }

        [Test]
        public void Modal_BlocksRealSubmitAndNavigation_WithoutHidingLower_ThenRestoresSelection()
        {
            var main = Page("Main", 1000); var lower = Button(main); int calls = 0; lower.onClick.AddListener(() => calls++);
            _events.SetSelectedGameObject(lower.gameObject);
            var popup = Page("Popup", 1500); var upper = Button(popup);
            _masks[popup] = Create("Mask"); Refresh();
            Assert.That(_events.currentSelectedGameObject, Is.EqualTo(upper.gameObject));
            Assert.That(lower.IsInteractable(), Is.False);
            ExecuteEvents.Execute(lower.gameObject, new BaseEventData(_events), ExecuteEvents.submitHandler);
            Assert.That(calls, Is.Zero, "Submit must be blocked by interactability, not just a mouse mask");
            var axis = new AxisEventData(_events) { moveDir = MoveDirection.Down };
            upper.OnMove(axis); Assert.That(_events.currentSelectedGameObject, Is.Not.EqualTo(lower.gameObject));
            Assert.That(main.CanvasGroup.alpha, Is.EqualTo(1)); Assert.That(main.Canvas.enabled, Is.True);
            var context = new EUIPageContext(null); context.PushMainPage(main); context.AddPopup(popup);
            bool visitedLower = false;
            context.ForEachVisiblePage(page => { if (page == main) visitedLower = true; return false; });
            Assert.That(visitedLower, Is.False, "Unhandled Escape must not bubble into the lower modal-blocked page");
            _masks.Clear(); Refresh();
            Assert.That(_events.currentSelectedGameObject, Is.EqualTo(lower.gameObject));
            ExecuteEvents.Execute(lower.gameObject, new BaseEventData(_events), ExecuteEvents.submitHandler);
            Assert.That(calls, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TwoModals_OutOfOrderRemovalOrDestroy_PreservesReturnChain(bool destroyLower)
        {
            var main = Page("Main", 1000); var initial = Button(main); _events.SetSelectedGameObject(initial.gameObject);
            var lower = Page("Lower", 1500); Button(lower); _masks[lower] = Create("Mask1"); Refresh();
            var upper = Page("Upper", 2000); var top = Button(upper); _masks[upper] = Create("Mask2"); Refresh();
            if (destroyLower) Object.DestroyImmediate(lower.GameObject);
            _masks.Remove(lower); Refresh();
            Assert.That(_events.currentSelectedGameObject, Is.EqualTo(top.gameObject));
            Assert.That(initial.IsInteractable(), Is.False);
            _masks.Remove(upper); Refresh();
            Assert.That(_events.currentSelectedGameObject, Is.EqualTo(initial.gameObject));
        }

        [Test]
        public void HideAndRestoreModal_AndNoMask_KeepFocusOwnedByVisiblePages()
        {
            var main = Page("Main", 1000); var initial = Button(main); _events.SetSelectedGameObject(initial.gameObject);
            var popup = Page("Popup", 1500); var upper = Button(popup);
            Refresh(); Assert.That(_events.currentSelectedGameObject, Is.EqualTo(initial.gameObject), "No mask preserves business-managed input");
            _masks[popup] = Create("Mask"); Refresh();
            popup.HideViewOnly(); Refresh();
            Assert.That(_events.currentSelectedGameObject, Is.EqualTo(initial.gameObject));
            popup.RestoreViewOnly(); Refresh();
            Assert.That(_events.currentSelectedGameObject, Is.EqualTo(upper.gameObject));
        }

        [Test]
        public void ClosingModal_PreservesFocusMovedToHigherIndependentUI()
        {
            var main = Page("Main", 1000); var initial = Button(main); _events.SetSelectedGameObject(initial.gameObject);
            var popup = Page("Popup", 1500); Button(popup); _masks[popup] = Create("Mask"); Refresh();
            var gm = Page("Higher independent UI", 25000); var focused = Button(gm); _events.SetSelectedGameObject(focused.gameObject);
            _masks.Clear(); Refresh();
            Assert.That(_events.currentSelectedGameObject, Is.EqualTo(focused.gameObject));
        }

        [Test]
        public void CrossHide_PausedPopups_KeepOriginalReturnChain()
        {
            var main = Page("Main", 1000); var initial = Button(main);
            _events.SetSelectedGameObject(initial.gameObject);
            var lower = Page("Lower", 1500); var lowerButton = Button(lower);
            _masks[lower] = Create("Mask1"); Refresh();
            var upper = Page("Upper", 2000); var upperButton = Button(upper);
            lower.OnPause();
            // Actual Manager + engine queue integration is covered separately in Play runtime evidence.
            _masks[upper] = Create("Mask2"); Refresh();
            upper.HideViewOnly(); lower.OnResume(); Refresh();
            Assert.That(lowerButton.IsInteractable(), Is.True);
            Assert.That(_events.currentSelectedGameObject, Is.EqualTo(lowerButton.gameObject));
            upper.RestoreViewOnly(); lower.OnPause(); Refresh();
            lower.HideViewOnly(); Refresh(); lower.RestoreViewOnly(); Refresh();
            Assert.That(_events.currentSelectedGameObject, Is.EqualTo(upperButton.gameObject));
            _masks.Remove(upper); lower.OnResume(); Refresh();
            Assert.That(_events.currentSelectedGameObject, Is.EqualTo(lowerButton.gameObject));
            _masks.Remove(lower); Refresh();
            Assert.That(_events.currentSelectedGameObject, Is.EqualTo(initial.gameObject));
        }

        [Test]
        public void PauseAndViewHide_PreserveBusinessDisabledInteraction()
        {
            var page = Page("Business disabled", 1000); page.CanvasGroup.interactable = false;
            page.CanvasGroup.blocksRaycasts = false;
            page.OnPause(); page.HideViewOnly(); page.RestoreViewOnly(); page.OnResume();
            Assert.That(page.CanvasGroup.interactable, Is.False);
            Assert.That(page.CanvasGroup.blocksRaycasts, Is.False);
        }

        [Test]
        public void ModalRelease_PreservesAnAlreadyDisabledPageRoot()
        {
            var main = Page("Business disabled root", 1000); Button(main); main.CanvasGroup.interactable = false;
            var popup = Page("Popup", 1500); Button(popup); _masks[popup] = Create("Mask"); Refresh();
            _masks.Clear(); Refresh();
            Assert.That(main.CanvasGroup.interactable, Is.False);
        }
    }
}
