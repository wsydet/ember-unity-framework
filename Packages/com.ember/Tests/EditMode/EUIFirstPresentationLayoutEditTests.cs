// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using Ember.UIExtension;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.UI;

namespace Ember.UI.Tests
{
    public sealed class EUIFirstPresentationLayoutEditTests
    {
        [Test]
        public void PlayShow_ResolvesLayoutInsideSafeAreaBeforeOpenCompletes()
        {
            var root = new GameObject("Page", typeof(RectTransform));
            try
            {
                var page = new EUIPage(root) { LogicTypeName = typeof(ChangeLayoutOnShow).FullName };
                var safe = new GameObject("Safe", typeof(RectTransform), typeof(EUISafeArea));
                safe.transform.SetParent(root.transform, false);
                var dialogue = new GameObject("Dialogue", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                dialogue.transform.SetParent(safe.transform, false);
                dialogue.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 200f);
                var group = dialogue.GetComponent<HorizontalLayoutGroup>();
                group.childControlWidth = true;
                group.childForceExpandWidth = false;
                var child = new GameObject("Portrait", typeof(RectTransform), typeof(LayoutElement));
                child.transform.SetParent(dialogue.transform, false);
                var childRect = child.GetComponent<RectTransform>();
                childRect.sizeDelta = Vector2.zero;
                var layout = child.GetComponent<LayoutElement>();
                layout.preferredWidth = 0f;

                page.CreateLogic((_, logic) => ((ChangeLayoutOnShow)logic).Layout = layout);
                page.Init(null);
                float widthAtOpen = -1;
                page.SetShowCallback(() => widthAtOpen = childRect.rect.width, null);
                page.PlayShow();

                Assert.That(page.CanvasGroup.alpha, Is.EqualTo(1));
                Assert.That(widthAtOpen, Is.EqualTo(160f).Within(.01f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void RestoreViewOnly_KeepsOnShowOverridesThroughInputGates(bool paused, bool modal)
        {
            var root = new GameObject("Restore Page", typeof(RectTransform));
            try
            {
                var page = new EUIPage(root) { LogicTypeName = typeof(ChangeLayoutOnShow).FullName };
                var layout = root.AddComponent<LayoutElement>();
                page.CreateLogic((_, logic) => ((ChangeLayoutOnShow)logic).Layout = layout);
                var logic = (ChangeLayoutOnShow)page.Logic;
                page.Init(null);
                page.PlayShow();
                if (paused) page.OnPause();
                if (modal) page.SetModalInputBlocked(true);
                page.HideViewOnly();
                logic.OverrideOnShow = () =>
                {
                    page.CanvasGroup.alpha = .35f;
                    page.CanvasGroup.blocksRaycasts = false;
                    page.CanvasGroup.interactable = false;
                };
                page.RestoreViewOnly();

                Assert.That(page.CanvasGroup.alpha, Is.EqualTo(.35f));
                Assert.That(page.State, Is.EqualTo(paused ? PageState.Paused : PageState.Opened));
                if (paused) page.OnResume();
                if (modal) page.SetModalInputBlocked(false);
                Assert.That(page.CanvasGroup.blocksRaycasts, Is.False);
                Assert.That(page.CanvasGroup.interactable, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void RestoreViewOnly_ReappliesPauseBeforeFrameEvenIfOnShowEnablesInput()
        {
            var root = new GameObject("Paused Restore Page", typeof(RectTransform));
            try
            {
                var page = new EUIPage(root) { LogicTypeName = typeof(ChangeLayoutOnShow).FullName };
                var layout = root.AddComponent<LayoutElement>();
                page.CreateLogic((_, logic) => ((ChangeLayoutOnShow)logic).Layout = layout);
                var logic = (ChangeLayoutOnShow)page.Logic;
                page.Init(null);
                page.PlayShow();
                page.OnPause();
                page.HideViewOnly();
                logic.OverrideOnShow = () =>
                {
                    page.CanvasGroup.blocksRaycasts = true;
                    page.CanvasGroup.interactable = true;
                };

                page.RestoreViewOnly();
                Assert.That(page.State, Is.EqualTo(PageState.Paused));
                Assert.That(page.CanvasGroup.blocksRaycasts, Is.False);
                Assert.That(page.CanvasGroup.interactable, Is.False);
                page.OnResume();
                Assert.That(page.CanvasGroup.blocksRaycasts, Is.True);
                Assert.That(page.CanvasGroup.interactable, Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        public sealed class ChangeLayoutOnShow : EUILogic
        {
            public LayoutElement Layout;
            public System.Action OverrideOnShow;

            public override void OnShow()
            {
                Layout.preferredWidth = 160f;
                OverrideOnShow?.Invoke();
            }
        }
    }
}
