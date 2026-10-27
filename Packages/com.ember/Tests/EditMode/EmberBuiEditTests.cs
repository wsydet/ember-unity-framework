// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System.Reflection;
using Ember.UIExtension;
using Ember.UIExtension.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Ember.UI.Tests
{
    public class EmberBuiEditTests
    {
        [Test]
        public void ZeroScaleCanvasAndDrivenSafeArea_KeepTopFrameAtHalfHeight()
        {
            var root=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas));
            try
            {
                var rect=(RectTransform)root.transform;rect.pivot=Vector2.zero;rect.localScale=Vector3.zero;
                var safe=new GameObject("SafeArea",typeof(RectTransform),typeof(EUISafeArea));safe.transform.SetParent(rect,false);
                var safeRect=(RectTransform)safe.transform;safeRect.anchorMin=safeRect.anchorMax=Vector2.zero;safeRect.sizeDelta=Vector2.zero;
                var top=new GameObject("Top",typeof(RectTransform));top.transform.SetParent(safeRect,false);
                var topRect=(RectTransform)top.transform;topRect.anchorMin=new Vector2(0,1);topRect.anchorMax=Vector2.one;topRect.sizeDelta=Vector2.zero;
                var args=new object[]{topRect,rect,new Vector2(1280,720),null,null};
                typeof(EmberBuiService).GetMethod("Frame",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);
                Assert.AreEqual(new Vector2(640,720),(Vector2)args[3]);
                Assert.AreEqual(new Vector2(1280,0),(Vector2)args[4]);
                Assert.AreEqual(Vector3.zero,rect.localScale,"Analysis must not normalize the formal root.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase("../outside.json")]
        [TestCase("Assets/../outside.json")]
        public void Analyze_RejectsEscapingRequestWithoutWriting(string path)
        {
            var plan=EmberBuiService.Analyze(path);
            Assert.IsFalse(plan.valid);Assert.IsEmpty(plan.writes);
            Assert.That(plan.issues[0],Does.Contain("PATH_INVALID"));
        }
    }
}
