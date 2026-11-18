// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using NUnit.Framework;
using UnityEngine;

namespace Ember.UI.Tests
{
    public sealed class EUIFullscreenBackdropEditTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void FullScreenCulling_RestoresOriginalPhysicalDistance_AndOrdinaryResumeDoesNotMoveCanvas(bool lowerVisible)
        {
            var root = new GameObject("Fullscreen lower", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            var camera = new GameObject("Fullscreen fixture camera", typeof(Camera));
            var fullscreen = new GameObject("Fullscreen", typeof(RectTransform));
            try
            {
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera.GetComponent<Camera>();
                canvas.planeDistance = 137;
                var page = new EUIPage(root) { EUIPageDef = new EUIPageDef("lower", UILayer.Normal) };
                var cover = new EUIPage(fullscreen)
                {
                    EUIPageDef = new EUIPageDef("fullscreen", UILayer.Popup, PageType.FullScreenPopup),
                };
                var context = new EUIPageContext(null);
                context.PushMainPage(page);
                page.Init(null);
                page.PlayShow();
                page.CanvasGroup.alpha = lowerVisible ? 1f : 0f;
                page.CanvasGroup.interactable = lowerVisible;
                page.CanvasGroup.blocksRaycasts = lowerVisible;

                page.OnPause();
                page.OnResume();
                Assert.That(canvas.planeDistance, Is.EqualTo(137).Within(.01));

                context.AddPopup(cover);
                context.CommitFullScreenPopup(cover);
                context.CommitFullScreenPopup(cover);
                Assert.That(canvas.planeDistance, Is.GreaterThan(camera.GetComponent<Camera>().farClipPlane));
                Assert.That(page.CanvasGroup.alpha, Is.Zero);
                Assert.That(page.CanvasGroup.interactable, Is.False);
                Assert.That(page.CanvasGroup.blocksRaycasts, Is.False);

                context.RemovePopup(cover);
                context.FinishPopupClose(cover);
                context.FinishPopupClose(cover);
                Assert.That(canvas.planeDistance, Is.EqualTo(137).Within(.01));
                Assert.That(page.CanvasGroup.alpha, Is.EqualTo(lowerVisible ? 1f : 0f));
                Assert.That(page.CanvasGroup.interactable, Is.EqualTo(lowerVisible));
                Assert.That(page.CanvasGroup.blocksRaycasts, Is.EqualTo(lowerVisible));
            }
            finally
            {
                Object.DestroyImmediate(fullscreen);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(camera);
            }
        }
    }
}
