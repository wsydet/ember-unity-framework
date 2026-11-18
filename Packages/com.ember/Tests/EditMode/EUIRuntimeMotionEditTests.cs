using System;
using Ember.UIExtension;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace Ember.UI.Tests
{
    public sealed class EUIScrollControllerEditTests
    {
        private GameObject _root;
        private RectTransform _view,_content,_target;
        private ScrollRect _scroll;
        private EUIScrollController _controller;
        [SetUp] public void SetUp()
        {
            _root=new GameObject("ScrollFixture",typeof(RectTransform),typeof(Canvas));_root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var go=new GameObject("Scroll",typeof(RectTransform),typeof(ScrollRect));go.transform.SetParent(_root.transform,false);
            _view=(RectTransform)go.transform;_view.sizeDelta=new Vector2(200,160);_scroll=go.GetComponent<ScrollRect>();_scroll.viewport=_view;_scroll.movementType=ScrollRect.MovementType.Clamped;
            _content=Rect("Content",_view,new Vector2(800,960));_scroll.content=_content;
            var nested=Rect("Nested",_content,Vector2.zero);nested.anchoredPosition=new Vector2(30,-20);
            _target=Rect("Target",nested,new Vector2(60,40));_target.anchoredPosition=new Vector2(70,-80);
            _controller=go.AddComponent<EUIScrollController>();
        }
        private RectTransform Rect(string name,Transform parent,Vector2 size)
        {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.sizeDelta=size;return r;}
        private Bounds Bounds(RectTransform rect)
        {var c=new Vector3[4];rect.GetWorldCorners(c);var b=new Bounds(_view.InverseTransformPoint(c[0]),Vector3.zero);for(int i=1;i<4;i++)b.Encapsulate(_view.InverseTransformPoint(c[i]));return b;}
        [TearDown] public void TearDown()=>Object.DestroyImmediate(_root);
        [TestCase(0f,false)][TestCase(0.5f,false)][TestCase(1f,false)][TestCase(0f,true)][TestCase(0.5f,true)][TestCase(1f,true)]
        public void BeginEnd_ProjectActualContentEdges_ForPivotAndAnchors(float pivot,bool stretch)
        {
            _content.pivot=new Vector2(pivot,pivot);
            if(stretch){_content.anchorMin=Vector2.zero;_content.anchorMax=Vector2.one;_content.sizeDelta=new Vector2(600,800);}
            _controller.ScrollToBegin();var b=Bounds(_content);Assert.That(b.min.x,Is.EqualTo(_view.rect.xMin).Within(0.02));Assert.That(b.max.y,Is.EqualTo(_view.rect.yMax).Within(0.02));
            _controller.ScrollToEnd();b=Bounds(_content);Assert.That(b.max.x,Is.EqualTo(_view.rect.xMax).Within(0.02));Assert.That(b.min.y,Is.EqualTo(_view.rect.yMin).Within(0.02));
        }
        [TestCase(EUIScrollAlignment.Begin,12f)][TestCase(EUIScrollAlignment.Center,12f)][TestCase(EUIScrollAlignment.End,12f)]
        [TestCase(EUIScrollAlignment.Begin,-12f)][TestCase(EUIScrollAlignment.Center,-12f)][TestCase(EUIScrollAlignment.End,-12f)]
        public void TargetAlignmentUsesProjectedEdgesAndViewportOffset(EUIScrollAlignment alignment,float offset)
        {
            _controller.ScrollTo(_target,alignment,new Vector2(offset,offset));var b=Bounds(_target);var v=_view.rect;
            var actual=alignment==EUIScrollAlignment.Begin?new Vector2(b.min.x,b.max.y):alignment==EUIScrollAlignment.End?new Vector2(b.max.x,b.min.y):(Vector2)b.center;
            var expected=alignment==EUIScrollAlignment.Begin?new Vector2(v.xMin,v.yMax):alignment==EUIScrollAlignment.End?new Vector2(v.xMax,v.yMin):v.center;
            Assert.That(Vector2.Distance(actual,expected+new Vector2(offset,offset)),Is.LessThan(0.03));Assert.IsTrue(_controller.IsVisible(_target));
        }
        [TestCase(0f)][TestCase(50f)][TestCase(200f)]
        public void SmallOrZeroContentAndLayoutChangesStayFinite(float size)
        {
            _content.sizeDelta=new Vector2(size,size);_controller.ScrollToEnd();_controller.ScrollByPixels(new Vector2(500,500));
            Assert.IsTrue(float.IsFinite(_content.anchoredPosition.x));Assert.IsTrue(float.IsFinite(_content.anchoredPosition.y));
            _content.sizeDelta=new Vector2(800,960);_controller.ScrollToEnd();Assert.That(Bounds(_content).min.y,Is.EqualTo(_view.rect.yMin).Within(0.02));
        }
        [Test] public void PixelDirectionAndVisibilityUseAreaAndRejectForeignTargets()
        {
            _controller.ScrollToBegin();var before=Bounds(_content).center;_controller.ScrollByPixels(new Vector2(25,35));var after=Bounds(_content).center;
            Assert.That(after.x-before.x,Is.EqualTo(-25).Within(0.02));Assert.That(after.y-before.y,Is.EqualTo(35).Within(0.02));
            _controller.ScrollTo(_target);Assert.IsTrue(_controller.IsVisible(_target,true));_target.sizeDelta=Vector2.zero;Assert.IsFalse(_controller.IsVisible(_target));Assert.IsFalse(_controller.IsVisible(_view));
        }
        [Test] public void LayoutGroupPreferredHeightChangesAreAppliedBeforePositioning()
        {
            Object.DestroyImmediate(_target.parent.gameObject);_scroll.horizontal=false;
            var layout=_content.gameObject.AddComponent<VerticalLayoutGroup>();layout.childControlHeight=true;layout.childForceExpandHeight=false;
            var fitter=_content.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            LayoutElement last=null;for(int i=0;i<3;i++){var row=Rect("Row"+i,_content,new Vector2(100,100));last=row.gameObject.AddComponent<LayoutElement>();last.preferredHeight=100;}
            _controller.ScrollToEnd();Assert.That(_content.rect.height,Is.EqualTo(300).Within(0.02));Assert.That(Bounds(_content).min.y,Is.EqualTo(_view.rect.yMin).Within(0.02));
            last.preferredHeight=300;_controller.ScrollToEnd();Assert.That(_content.rect.height,Is.EqualTo(500).Within(0.02));Assert.That(Bounds(_content).min.y,Is.EqualTo(_view.rect.yMin).Within(0.02));
        }
    }

    public sealed class TMPMarqueeEditTests
    {
        private GameObject _root;
        private TextMeshProUGUI _text;
        private TMPMarquee _marquee;
        private TMP_FontAsset _font;
        [SetUp] public void SetUp()
        {
            _root=new GameObject("MarqueeFixture",typeof(RectTransform),typeof(Canvas));_root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var clip=new GameObject("Clip",typeof(RectTransform),typeof(RectMask2D));clip.transform.SetParent(_root.transform,false);((RectTransform)clip.transform).sizeDelta=new Vector2(150,60);
            var go=new GameObject("Text",typeof(RectTransform));go.transform.SetParent(clip.transform,false);_text=go.AddComponent<TextMeshProUGUI>();
            _font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Packages/com.ember/SharedAssets/Fonts/钉钉进步体/DingTalk-JinBuTi.ttf"));
            Assert.IsNotNull(_font);_font.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>();
            _text.rectTransform.sizeDelta=new Vector2(150,60);_text.font=_font;
            _text.fontSize=28;_text.enableAutoSizing=false;_text.textWrappingMode=TextWrappingModes.NoWrap;_text.overflowMode=TextOverflowModes.Overflow;
            _marquee=go.AddComponent<TMPMarquee>();
        }
        [TearDown] public void TearDown(){Object.DestroyImmediate(_root);if(_font)Object.DestroyImmediate(_font);}
        [Test] public void ShortTextStaysAtAuthorAlignment_AndConfigurationIsUnchanged()
        {
            _text.text="Hi";_text.alignment=TextAlignmentOptions.Center;_text.ForceMeshUpdate();var x=_text.textInfo.meshInfo[0].vertices[0];
            var font=_text.font;var mat=_text.fontSharedMaterial;var size=_text.rectTransform.sizeDelta;
            _marquee.Refresh();Assert.AreEqual(x,_text.textInfo.meshInfo[0].vertices[0]);Assert.IsFalse(_marquee.IsRunning);
            Assert.AreSame(font,_text.font);Assert.AreSame(mat,_text.fontSharedMaterial);Assert.AreEqual(size,_text.rectTransform.sizeDelta);
        }
        [Test] public void LongTextResetsWithoutAccumulation_AndDisableRestoresOriginalGeometry()
        {
            _marquee.SetMarqueeEnabled(false);_text.text="A long single line for marquee scrolling";_text.alignment=TextAlignmentOptions.Center;_text.ForceMeshUpdate();var original=_text.textInfo.meshInfo[0].vertices[0];
            _marquee.SetMarqueeEnabled(true,12);_marquee.Refresh();Assert.IsTrue(_marquee.IsRunning);var start=_text.textInfo.meshInfo[0].vertices[0];
            _marquee.Refresh();Assert.AreEqual(start,_text.textInfo.meshInfo[0].vertices[0]);_marquee.SetMarqueeEnabled(true,0);Assert.IsFalse(_marquee.IsRunning);_marquee.SetMarqueeEnabled(true,45);Assert.AreEqual(45,_marquee.Speed);
            _marquee.SetMarqueeEnabled(false);Assert.AreEqual(original,_text.textInfo.meshInfo[0].vertices[0]);
        }
        [TestCase(0)][TestCase(1)][TestCase(2)]
        public void InvalidConfigurationRemainsStaticAndDiagnoses(int invalid)
        {
            _marquee.SetMarqueeEnabled(false);_text.text="Long long long line for validation";
            if(invalid==0)_text.enableAutoSizing=true;if(invalid==1)_text.textWrappingMode=TextWrappingModes.Normal;if(invalid==2)Object.DestroyImmediate(_text.transform.parent.GetComponent<RectMask2D>());
            _text.ForceMeshUpdate();var original=_text.textInfo.meshInfo[0].vertices[0];_marquee.SetMarqueeEnabled(true);_marquee.Refresh();
            Assert.IsNotNull(_marquee.Diagnostic);Assert.IsFalse(_marquee.IsRunning);Assert.AreEqual(original,_text.textInfo.meshInfo[0].vertices[0]);
        }
    }
}
