// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using UnityEngine;

namespace Ember.UI.Tests
{
    public sealed class EUITopMostOrderingEditTests
    {
        [Test]
        public void TopMostAndSubPages_CompactInPresentationOrderAndStopBelowGuide()
        {
            var context = new EUIPageContext(new EUIViewEngine());
            var instances = new List<EUIPage>();
            EUIPage CreatePage(string name, PageType type)
            {
                var page = new EUIPage(new GameObject(name, typeof(RectTransform)))
                {
                    EUIPageDef = new EUIPageDef(name, UILayer.TopMost, type),
                };
                instances.Add(page);
                return page;
            }

            try
            {
                var first = CreatePage("first", PageType.TopMost);
                var child = CreatePage("child", PageType.SubPage);
                var leaving = CreatePage("leaving", PageType.TopMost);
                Assert.That(context.TryAddTopMost(first), Is.True);
                Assert.That(context.TrySetSubPageOrder(child, first), Is.True);
                Assert.That(context.TryAddTopMost(leaving), Is.True);
                context.RemoveTopMost(leaving);

                for (int i = 0; i < 76; i++)
                    Assert.That(context.TryAddTopMost(CreatePage("root" + i, PageType.TopMost)), Is.True, "root " + i);

                var orders = instances.Select(p => p.Canvas.sortingOrder).ToArray();
                Assert.That(orders, Is.Ordered.Ascending);
                Assert.That(orders.Distinct().Count(), Is.EqualTo(79));
                Assert.That(orders.First(), Is.GreaterThan(25000));
                Assert.That(orders.Last(), Is.EqualTo(28950));

                var next = CreatePage("over-capacity", PageType.TopMost);
                Assert.That(context.TryAddTopMost(next), Is.False);
                Assert.That(context.GetTopTopMost(), Is.EqualTo(instances[78]));

                // The exiting root's order is reusable only after its Canvas is gone.
                Object.DestroyImmediate(leaving.GameObject);
                Assert.That(context.TryAddTopMost(next), Is.True);
                Assert.That(context.GetTopTopMost(), Is.SameAs(next));
                Assert.That(child.Canvas.sortingOrder, Is.GreaterThan(first.Canvas.sortingOrder));
                Assert.That(next.Canvas.sortingOrder, Is.EqualTo(28950));
            }
            finally
            {
                foreach (var page in instances)
                    if (page.GameObject) Object.DestroyImmediate(page.GameObject);
            }
        }

        [Test]
        public void BackdropBoundary_FollowsCompactedCanvasOnlyForAnOwnedLease()
        {
            var root = new GameObject("Backdrop foreground", typeof(RectTransform), typeof(Canvas));
            try
            {
                var canvas = root.GetComponent<Canvas>();
                canvas.sortingOrder = 25500;
                var owned = new EUIBackdropRequest(canvas, null, 1);
                var explicitOrder = new EUIBackdropRequest(canvas, 25500, null, 1);
                canvas.sortingOrder = 25050;
                Assert.That(owned.CurrentSortingOrder, Is.EqualTo(25050));
                Assert.That(explicitOrder.CurrentSortingOrder, Is.EqualTo(25500));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
