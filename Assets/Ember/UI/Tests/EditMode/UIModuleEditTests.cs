// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using Ember.UI;

using NUnit.Framework;

using UniRx;

namespace Ember.UI.Tests
{
    /// <summary>
    /// 纯 C# 逻辑的 Edit Mode 测试（不需要 Play Mode）。
    /// </summary>
    public class UIModuleEditTests
    {
        #region PageDef

        [Test]
        public void PageDef_ShouldStorePrefabPath()
        {
            var def = new PageDef("ui/test_page", 100);
            Assert.AreEqual("ui/test_page", def.PrefabPath);
        }

        [Test]
        public void PageDef_ShouldStoreLayer()
        {
            var def = new PageDef("ui/test_page", UILayer.Popup);
            Assert.AreEqual((int)UILayer.Popup, def.Layer);
        }

        [Test]
        public void PageDef_ShouldStorePageType()
        {
            var def = new PageDef("ui/test_page", UILayer.Normal, PageType.MainPage);
            Assert.AreEqual(PageType.MainPage, def.PageType);
        }

        [Test]
        public void PageDef_DefaultPageType_ShouldBeMainPage()
        {
            var def = new PageDef("ui/test_page", 100);
            Assert.AreEqual(PageType.MainPage, def.PageType);
        }

        #endregion

        // --------------------------------------------------------

        #region UILayer Enum

        [Test]
        public void UILayer_Order_Background_Lowest()
        {
            Assert.Less((int)UILayer.Background, (int)UILayer.Normal);
            Assert.Less((int)UILayer.Normal, (int)UILayer.Popup);
            Assert.Less((int)UILayer.Popup, (int)UILayer.TopMost);
        }

        #endregion

        // --------------------------------------------------------

        #region EmberUIObserver

        [Test]
        public void Observer_OnPageOpened_ShouldNotify()
        {
            var pageDef = new PageDef("ui/test", UILayer.Normal);
            PageLifecycleEvent? received = null;

            var sub = EmberUIObserver.OnPageOpened.Subscribe(e => received = e);

            EmberUIObserver.NotifyOpened(pageDef, "hello");

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(pageDef, received.Value.Page);
            Assert.AreEqual("hello", received.Value.Args);

            sub.Dispose();
        }

        [Test]
        public void Observer_OnPageClosed_ShouldNotify()
        {
            var pageDef = new PageDef("ui/test", UILayer.Popup);
            PageLifecycleEvent? received = null;

            var sub = EmberUIObserver.OnPageClosed.Subscribe(e => received = e);

            EmberUIObserver.NotifyClosed(pageDef, null);

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(pageDef, received.Value.Page);

            sub.Dispose();
        }

        [Test]
        public void Observer_OnPagePaused_ShouldNotify()
        {
            var pageDef = new PageDef("ui/test", UILayer.Popup);
            PageLifecycleEvent? received = null;

            var sub = EmberUIObserver.OnPagePaused.Subscribe(e => received = e);

            EmberUIObserver.NotifyPaused(pageDef);

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(pageDef, received.Value.Page);
            Assert.IsNull(received.Value.Args); // Pause 不传 args

            sub.Dispose();
        }

        [Test]
        public void Observer_OnPageResumed_ShouldNotify()
        {
            var pageDef = new PageDef("ui/test", UILayer.Popup);
            PageLifecycleEvent? received = null;

            var sub = EmberUIObserver.OnPageResumed.Subscribe(e => received = e);

            EmberUIObserver.NotifyResumed(pageDef);

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(pageDef, received.Value.Page);

            sub.Dispose();
        }

        [Test]
        public void Observer_OnAllClosed_ShouldNotify()
        {
            bool received = false;
            var sub = EmberUIObserver.OnAllClosed.Subscribe(_ => received = true);

            EmberUIObserver.NotifyAllClosed();

            Assert.IsTrue(received);
            sub.Dispose();
        }

        [Test]
        public void Observer_Unsubscribed_ShouldNotReceive()
        {
            int count = 0;
            var sub = EmberUIObserver.OnPageOpened.Subscribe(_ => count++);

            EmberUIObserver.NotifyOpened(new PageDef("a", 1), null);
            Assert.AreEqual(1, count);

            sub.Dispose();

            EmberUIObserver.NotifyOpened(new PageDef("b", 2), null);
            Assert.AreEqual(1, count); // 不再增长
        }

        [Test]
        public void Observer_WhereFilter_ShouldWork()
        {
            var settingsDef = new PageDef("ui/settings", UILayer.Popup);
            var bagDef = new PageDef("ui/bag", UILayer.Popup);

            PageLifecycleEvent? received = null;

            var sub = EmberUIObserver.OnPageOpened
                .Where(e => e.Page.PrefabPath == "ui/settings")
                .Subscribe(e => received = e);

            // bag 不应触发
            EmberUIObserver.NotifyOpened(bagDef, null);
            Assert.IsFalse(received.HasValue);

            // settings 应触发
            EmberUIObserver.NotifyOpened(settingsDef, null);
            Assert.IsTrue(received.HasValue);

            sub.Dispose();
        }

        #endregion

        // --------------------------------------------------------

        #region EmberUIEvents

        [Test]
        public void UIEvents_Keys_InCorrectRange()
        {
            Assert.AreEqual(5000, EmberUIEvents.UIManagerReady);
            Assert.AreEqual(5001, EmberUIEvents.UIManagerShutdown);
            Assert.AreEqual(5002, EmberUIEvents.UIPageRouterReady);
        }

        #endregion
    }
}
