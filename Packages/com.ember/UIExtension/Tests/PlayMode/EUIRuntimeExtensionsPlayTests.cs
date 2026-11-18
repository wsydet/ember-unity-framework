using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ember.Core;
using Ember.Resource;
using Ember.UI;
using Ember.UIExtension;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Ember.UIExtension.Tests
{
    public sealed class EUIRuntimeExtensionsPlayTests
    {
        private readonly FieldInfo _instance=typeof(EmberSingleton<EmberResourceManager>).GetField("_instance",BindingFlags.Static|BindingFlags.NonPublic);
        private object _previous;private IResourceProvider _service;private Provider _provider;private float _timeScale;
        private readonly List<Object> _objects=new();
        private string _prefabPath;
        [UnitySetUp] public IEnumerator SetUp()
        {
            yield return null;_timeScale=Time.timeScale;
            _previous=_instance.GetValue(null);_service=EmberServiceLocator.TryResolve<IResourceProvider>();
            _instance.SetValue(null,new EmberResourceManager());EmberServiceLocator.Unregister<IResourceProvider>();_provider=new Provider();EmberResourceManager.Instance.Initialize(_provider);
            var camera=Go("TestClearCamera",typeof(Camera)).GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0.03f,0.04f,0.06f);camera.cullingMask=0;camera.depth=1000;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            Time.timeScale=_timeScale;
            foreach(var obj in _objects)if(obj is GameObject go && go){go.GetComponent<EUIImageLoader>()?.Clear();go.GetComponent<EUIRawImageLoader>()?.Clear();}
            // Text and submeshes must release their references before their fixture fonts own cleanup runs.
            foreach(var obj in _objects)if(obj is GameObject && obj)Object.Destroy(obj);yield return null;
            for(int i=_objects.Count-1;i>=0;i--)if(_objects[i])Object.Destroy(_objects[i]);_objects.Clear();yield return null;
            _instance.SetValue(null,_previous);EmberServiceLocator.Unregister<IResourceProvider>();if(_service!=null)EmberServiceLocator.Register(_service);
#if UNITY_EDITOR
            if(_prefabPath!=null)AssetDatabase.DeleteAsset(_prefabPath);_prefabPath=null;
#endif
        }
        private GameObject Go(string name,params Type[] components){var go=new GameObject(name,components);_objects.Add(go);return go;}
        private Texture2D Texture(){var t=new Texture2D(8,8);t.SetPixels(Enumerable.Repeat(Color.cyan,64).ToArray());t.Apply();_objects.Add(t);return t;}
        private Sprite Sprite(){var t=Texture();var s=UnityEngine.Sprite.Create(t,new Rect(0,0,8,8),Vector2.one*0.5f);_objects.Add(s);return s;}
        private RectTransform Rect(string name,Transform parent,Vector2 size)
        {var go=Go(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;rect.sizeDelta=size;return rect;}

        [UnityTest] public IEnumerator NeverActiveDestroyOrComponentRemoval_ReleasesPendingAndSynchronousTokens()
        {
            var observations=new List<RemovalObservation>();
            foreach(bool raw in new[]{false,true})foreach(bool sync in new[]{false,true})foreach(bool removeComponent in new[]{false,true})
            {
                var go=Go("NeverActive",typeof(RectTransform));go.SetActive(false);
                Object asset=raw?(Object)Texture():Sprite();_provider.Synchronous=sync?asset:null;
                MonoBehaviour loader;var results=new List<EUIImageLoadResult>();int index=_provider.Requests.Count;
                if(raw){var l=go.AddComponent<EUIRawImageLoader>();loader=l;l.Load("never",results.Add);}else{var l=go.AddComponent<EUIImageLoader>();loader=l;l.Load("never",results.Add);}
                Assert.AreEqual(index+1,_provider.Requests.Count);
                var image=go.GetComponent<Image>();var rawImage=go.GetComponent<RawImage>();
                Func<bool> mounted=()=>raw ? rawImage && rawImage.texture==asset : image && (image.sprite==asset || image.overrideSprite==asset);
                var observation=new RemovalObservation{Raw=raw,Synchronous=sync,ComponentOnly=removeComponent,MountedBeforeDestroy=mounted()};
                Assert.AreEqual(sync,observation.MountedBeforeDestroy);
                _provider.Requests[index].Released=()=>{observation.ReleaseObserved=true;observation.MountedAtRelease=mounted();};
                if(removeComponent)Object.Destroy(loader);else Object.Destroy(go);
                yield return null;yield return null;yield return null;
                observation.GraphicAlive=raw ? rawImage : image;observation.MountedAfterDestroy=mounted();observations.Add(observation);
                System.IO.File.WriteAllText(EvidencePath("loader-removal.json"),JsonUtility.ToJson(new RemovalObservations{Cases=observations.ToArray()},true));
                Assert.IsFalse(loader);Assert.AreEqual(removeComponent,observation.GraphicAlive);
                Assert.IsTrue(observation.ReleaseObserved);Assert.IsFalse(observation.MountedAtRelease,"Graphic still owns the resource at release");
                Assert.IsFalse(observation.MountedAfterDestroy,"Graphic retains the released resource");
                Assert.AreEqual(1,_provider.Requests[index].Releases,$"raw={raw}, sync={sync}, component={removeComponent}");
                Assert.AreEqual(sync?0:1,_provider.Requests[index].Cancels);Assert.AreEqual(1,results.Count);
                Assert.AreEqual(sync?EUIImageLoadStatus.Success:EUIImageLoadStatus.Destroyed,results[0].Status);
                Assert.IsFalse(_provider.Requests[index].Finish(asset));
            }
        }

        [UnityTest] public IEnumerator NeverActiveRemoval_PreservesBorrowedOverrideAndDestroyedGraphicIsSafe()
        {
            foreach(bool raw in new[]{false,true})foreach(bool destroyGraphicFirst in new[]{false,true})
            {
                var go=Go("BorrowedOrDestroyedGraphic",typeof(RectTransform));go.SetActive(false);
                Object owned=raw?(Object)Texture():Sprite(),borrowed=raw?(Object)Texture():Sprite();_provider.Synchronous=owned;
                MonoBehaviour loader;Image image=null;RawImage rawImage=null;int index=_provider.Requests.Count;
                if(raw){var l=go.AddComponent<EUIRawImageLoader>();loader=l;rawImage=go.GetComponent<RawImage>();l.Load("owned");}
                else{var l=go.AddComponent<EUIImageLoader>();loader=l;image=go.GetComponent<Image>();l.Load("owned");}
                Func<bool> mounted=()=>raw ? rawImage && rawImage.texture==owned : image && (image.sprite==owned || image.overrideSprite==owned);
                bool released=false,mountedAtRelease=false;_provider.Requests[index].Released=()=>{released=true;mountedAtRelease=mounted();};
                if(destroyGraphicFirst)
                {
                    // Remove the required companion first, then lose the Graphic before the monitor's next tick.
                    Object.DestroyImmediate(loader);
                    Object.DestroyImmediate(raw?(Object)rawImage:image);
                }
                else if(raw)rawImage.texture=(Texture)borrowed;else image.overrideSprite=(Sprite)borrowed;
                Object.Destroy(loader);yield return null;yield return null;yield return null;
                Assert.IsTrue(released);Assert.IsFalse(mountedAtRelease);Assert.IsFalse(mounted());Assert.AreEqual(1,_provider.Requests[index].Releases);
                if(!destroyGraphicFirst){Assert.AreSame(borrowed,raw?(Object)rawImage.texture:image.overrideSprite);Assert.IsTrue(borrowed);}
            }
        }

        [UnityTest] public IEnumerator HideEndsExistingRequest_AlphaAndGraphicDisableDoNot_AndShowDoesNotReplay()
        {
            var go=Go("Lifecycle",typeof(RectTransform),typeof(Image),typeof(CanvasGroup),typeof(EUIImageLoader));var loader=go.GetComponent<EUIImageLoader>();var image=go.GetComponent<Image>();yield return null;
            loader.Load("A");go.GetComponent<CanvasGroup>().alpha=0;image.enabled=false;yield return null;Assert.AreEqual(0,_provider.Requests[0].Releases);
            go.SetActive(false);Assert.AreEqual(1,_provider.Requests[0].Releases);go.SetActive(true);yield return null;Assert.AreEqual(1,_provider.Requests.Count);
            loader.Load("A");_provider.Requests[1].Finish(Sprite());Assert.IsNotNull(image.sprite);go.SetActive(false);Assert.AreEqual(1,_provider.Requests[1].Releases);Assert.IsNull(image.sprite);
        }

        [UnityTest] public IEnumerator RealItemList_InactiveBindHideShowAndNeverShownPoolReturnUseLeaseClear()
        {
            var root=Go("PoolHost",typeof(RectTransform),typeof(ScrollRect),typeof(EUIDynamicListHost));var host=root.GetComponent<EUIDynamicListHost>();
            var template=Go("Template",typeof(RectTransform),typeof(EUIBinding));template.SetActive(false);
            var viewport=Rect("Viewport",root.transform,new Vector2(100,200));viewport.gameObject.AddComponent<RectMask2D>();
            var content=Rect("Content",viewport,new Vector2(100,200));var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.childControlHeight=true;layout.childForceExpandHeight=false;
            var fitter=content.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=root.GetComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;
#if UNITY_EDITOR
            using(var so=new SerializedObject(template.GetComponent<EUIBinding>())){so.FindProperty("isPage").boolValue=false;so.FindProperty("noCodeGen").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();}
            _prefabPath="Assets/__EmberRuntimePool_"+Guid.NewGuid().ToString("N")+".prefab";var prefab=PrefabUtility.SaveAsPrefabAsset(template,_prefabPath);
            using(var so=new SerializedObject(host)){so.FindProperty("_scroll").objectReferenceValue=scroll;so.FindProperty("_itemTemplate").objectReferenceValue=prefab;so.FindProperty("_dataBinding").stringValue="test:image-path";so.ApplyModifiedPropertiesWithoutUndo();}
#endif
            bool boundInactive=false;EUIImageLoader current=null;
            using(var list=host.Create<string>(x=>x,lease=>
            {
                var item=lease.Item.GameObject;boundInactive=!item.activeInHierarchy;current=item.GetComponent<EUIImageLoader>()??item.AddComponent<EUIImageLoader>();
                lease.Track(current.Clear);current.Load(lease.Data);
                if(lease.Data=="never")throw new InvalidOperationException("bind abort probe");
            }))
            {
                list.Replace(new[]{"A"});Assert.IsTrue(boundInactive);var lease=list[0];yield return null;
                list.Hide();Assert.IsTrue(lease.IsCurrent);Assert.AreEqual(1,_provider.Requests[0].Releases);list.Show();Assert.AreEqual(1,_provider.Requests.Count);
                current.Load("show");_provider.Requests[1].Finish(Sprite());list.Clear();Assert.AreEqual(1,_provider.Requests[1].Releases);
                Assert.Throws<InvalidOperationException>(()=>list.Replace(new[]{"never"}));Assert.AreEqual(1,_provider.Requests[2].Releases);
            }
            yield return null;
        }

        [UnityTest] public IEnumerator ScrollAnimation_InputInterruptionSnapAndReentrantReplacement_UseRealEvents()
        {
            var canvas=Go("MotionCanvas",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var view=Rect("View",canvas.transform,new Vector2(200,160));var scroll=view.gameObject.AddComponent<ScrollRect>();scroll.viewport=view;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var content=Rect("Content",view,new Vector2(200,800));scroll.content=content;
            var a=Rect("First",content,new Vector2(180,80));a.anchoredPosition=new Vector2(0,320);var b=Rect("Middle",content,new Vector2(180,80));var c=Rect("Last",content,new Vector2(180,80));c.anchoredPosition=new Vector2(0,-320);
            view.gameObject.AddComponent<Image>().color=new Color(0.1f,0.1f,0.1f);view.gameObject.AddComponent<RectMask2D>();
            a.gameObject.AddComponent<Image>().color=Color.cyan;b.gameObject.AddComponent<Image>().color=Color.yellow;c.gameObject.AddComponent<Image>().color=Color.magenta;
            var controller=view.gameObject.AddComponent<EUIScrollController>();controller.SnapDuration=0.03f;controller.SettleDelay=0.01f;controller.SetSnapTargets(new[]{a,b,c},EUIScrollAxis.Vertical);
            var events=Go("TestEventSystem",typeof(EventSystem)).GetComponent<EventSystem>();yield return null;
            Time.timeScale=0;var results=new List<EUIScrollResult>();controller.ScrollToBegin();controller.ScrollToEnd(0.08f,results.Add);
            yield return new WaitForSecondsRealtime(0.13f);Assert.AreEqual(1,results.Count);Assert.AreEqual(EUIScrollStatus.Completed,results[0].Status);Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(0).Within(0.01));
            controller.ScrollToBegin(0.2f,results.Add);var pointer=new PointerEventData(events){button=PointerEventData.InputButton.Left,position=Vector2.zero};
            ExecuteEvents.Execute(view.gameObject,pointer,ExecuteEvents.beginDragHandler);Assert.AreEqual(EUIScrollStatus.UserInterrupted,results.Last().Status);
            int snaps=0,focus=0;controller.SnapCompleted+=_=>snaps++;controller.FocusChanged+=_=>focus++;
            ExecuteEvents.Execute(view.gameObject,pointer,ExecuteEvents.endDragHandler);yield return new WaitForSecondsRealtime(0.12f);Assert.AreEqual(1,snaps);Assert.AreEqual(1,focus);Assert.AreSame(c,controller.FocusedTarget);
            controller.ScrollToNormalized(new Vector2(0,0.5f));a.anchoredPosition=new Vector2(0,40);c.anchoredPosition=new Vector2(0,-40);controller.SetSnapTargets(new[]{a,c},EUIScrollAxis.Vertical);controller.SnapDuration=0;
            Assert.IsTrue(controller.SnapToNearest());Assert.AreSame(a,controller.FocusedTarget,"Equal distance must preserve candidate order.");
            a.anchoredPosition=new Vector2(0,320);c.anchoredPosition=new Vector2(0,-320);controller.SetSnapTargets(new[]{a,b,c},EUIScrollAxis.Vertical);controller.SnapDuration=0.03f;controller.ScrollToNormalized(new Vector2(0,0.5f));
            int beforeInertia=snaps;scroll.decelerationRate=0.001f;ExecuteEvents.Execute(view.gameObject,pointer,ExecuteEvents.beginDragHandler);scroll.velocity=new Vector2(0,200);ExecuteEvents.Execute(view.gameObject,pointer,ExecuteEvents.endDragHandler);
            yield return new WaitForSecondsRealtime(0.03f);Assert.AreEqual(beforeInertia,snaps,"Snap must not interrupt native inertia.");
            float settleDeadline=Time.unscaledTime+1.8f;while(snaps==beforeInertia && Time.unscaledTime<settleDeadline)yield return null;
            Assert.AreEqual(beforeInertia+1,snaps);Assert.AreSame(b,controller.FocusedTarget);
            controller.ScrollToBegin(0.1f,r=>{results.Add(r);if(r.Status==EUIScrollStatus.Replaced)controller.ScrollTo(b,completed:results.Add);});controller.ScrollToEnd(0.1f,results.Add);
            yield return new WaitForSecondsRealtime(0.15f);Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(0.5f).Within(0.02));
            controller.ScrollToBegin(0.2f,results.Add);ExecuteEvents.Execute(view.gameObject,new PointerEventData(events){scrollDelta=Vector2.up},ExecuteEvents.scrollHandler);Assert.AreEqual(EUIScrollStatus.UserInterrupted,results.Last().Status);
            controller.Stop();controller.ScrollTo(b,duration:0.1f,completed:results.Add);Object.Destroy(b.gameObject);yield return null;yield return null;Assert.AreEqual(EUIScrollStatus.TargetLost,results.Last().Status);
            controller.ScrollToBegin();yield return new WaitForEndOfFrame();Capture("scroll-vertical-begin.png");controller.ScrollToEnd();yield return new WaitForEndOfFrame();Capture("scroll-vertical-end.png");
            scroll.horizontal=true;scroll.vertical=false;content.sizeDelta=new Vector2(800,160);content.anchoredPosition=Vector2.zero;a.anchoredPosition=new Vector2(-320,0);c.anchoredPosition=new Vector2(320,0);
            controller.ScrollToBegin();yield return new WaitForEndOfFrame();Assert.IsTrue(controller.IsVisible(a));Capture("scroll-horizontal-begin.png");controller.ScrollToEnd();yield return new WaitForEndOfFrame();Assert.IsTrue(controller.IsVisible(c));Capture("scroll-horizontal-end.png");
            scroll.vertical=true;scroll.horizontal=false;content.sizeDelta=new Vector2(200,800);content.anchoredPosition=Vector2.zero;controller.ScrollToEnd();
            var bar=Go("Scrollbar",typeof(RectTransform),typeof(Scrollbar)).GetComponent<Scrollbar>();scroll.verticalScrollbar=bar;
            controller.ScrollToBegin(0.2f,results.Add);bar.value=0.3f;yield return null;yield return null;Assert.AreEqual(EUIScrollStatus.UserInterrupted,results.Last().Status);
            System.IO.File.WriteAllLines(EvidencePath("scroll-events.txt"),results.Select(r=>$"{r.OperationId}\t{r.Status}\t{r.NormalizedPosition}"));
        }

        [UnityTest] public IEnumerator MarqueeRealTime_AllMaterialMeshesMoveTogetherAndMasksRender()
        {
            var canvas=Go("MarqueeCanvas",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var clip=Rect("Clip",canvas.transform,new Vector2(240,72));clip.gameObject.AddComponent<RectMask2D>();
            var rect=Rect("Text",clip,new Vector2(240,72));var text=rect.gameObject.AddComponent<TMPEx>();
#if UNITY_EDITOR
            // Create fresh tables, atlas and material from immutable TTFs; never render through a shared dynamic SDF.
            var font=FixtureFont("Packages/com.ember/SharedAssets/Fonts/钉钉进步体/DingTalk-JinBuTi.ttf");
            var symbols=FixtureFont("Packages/com.ember/SharedAssets/Fonts/NotoSansSymbols2/NotoSansSymbols2-Regular.ttf");
            Assert.IsTrue(font.TryAddCharacters("中文 "+new string(Enumerable.Range(32,95).Select(x=>(char)x).ToArray())));
            font.atlasPopulationMode=AtlasPopulationMode.Static;
            font.fallbackFontAssetTable=new List<TMP_FontAsset>{symbols};text.SetFontSkin(-1,1);text.font=font;
            const uint symbol=0x25B2;Assert.IsFalse(font.HasCharacter((int)symbol));Assert.IsTrue(symbols.TryAddCharacters(char.ConvertFromUtf32((int)symbol)));
            text.text="中文 "+char.ConvertFromUtf32((int)symbol)+" Long text for visible clipped movement 0123456789";
#endif
            var spriteAsset=ScriptableObject.CreateInstance<TMP_SpriteAsset>();_objects.Add(spriteAsset);spriteAsset.spriteSheet=Texture();
#if UNITY_EDITOR
            using(var so=new SerializedObject(spriteAsset)){so.FindProperty("m_Version").stringValue="1.1.0";so.ApplyModifiedPropertiesWithoutUndo();}
#endif
            var spriteMaterial=new Material(Shader.Find("TextMeshPro/Sprite"));_objects.Add(spriteMaterial);spriteMaterial.mainTexture=spriteAsset.spriteSheet;spriteAsset.material=spriteMaterial;
            var glyph=new TMP_SpriteGlyph(0,new UnityEngine.TextCore.GlyphMetrics(8,8,0,8,8),new UnityEngine.TextCore.GlyphRect(0,0,8,8),1,0);
            spriteAsset.spriteGlyphTable.Add(glyph);spriteAsset.spriteCharacterTable.Add(new TMP_SpriteCharacter(0xE000,spriteAsset,glyph){name="fixture"});spriteAsset.UpdateLookupTables();text.spriteAsset=spriteAsset;text.text=text.text.Replace(" Long"," <sprite=0> Long");
            text.enableAutoSizing=false;text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Overflow;text.fontSize=34;
            var marquee=rect.gameObject.AddComponent<TMPMarquee>();marquee.StartDelay=0;marquee.EndDelay=0;marquee.SetMarqueeEnabled(true,40);marquee.Refresh();yield return null;yield return new WaitForEndOfFrame();
            Assert.GreaterOrEqual(text.textInfo.materialCount,3);var material=text.fontSharedMaterial;var shader=material.shader;var size=rect.sizeDelta;int count=text.textInfo.materialCount;var before=new Vector3[count];
            for(int m=0;m<count;m++)before[m]=text.textInfo.meshInfo[m].vertices[0];float time=Time.unscaledTime;yield return new WaitForSecondsRealtime(0.12f);float elapsed=Time.unscaledTime-time;
            for(int m=0;m<count;m++)Assert.That(before[m].x-text.textInfo.meshInfo[m].vertices[0].x,Is.EqualTo(40*elapsed).Within(2.5));
            var after=new Vector3[count];for(int m=0;m<count;m++)after[m]=text.textInfo.meshInfo[m].vertices[0];
            System.IO.File.WriteAllText(EvidencePath("marquee-mesh.json"),JsonUtility.ToJson(new MarqueeEvidence{Materials=count,Elapsed=elapsed,Before=before,After=after,Font=text.font.name,Shader=shader.name,RectSize=size},true));
            Assert.AreSame(material,text.fontSharedMaterial);Assert.AreSame(shader,material.shader);Assert.AreEqual(size,rect.sizeDelta);
            marquee.SetMarqueeEnabled(true,0);var paused=text.textInfo.meshInfo[0].vertices[0];yield return null;Assert.AreEqual(paused,text.textInfo.meshInfo[0].vertices[0]);
#if UNITY_EDITOR
            using(var so=new SerializedObject(marquee)){so.FindProperty("_speed").floatValue=float.NaN;so.ApplyModifiedPropertiesWithoutUndo();}
            yield return null;Assert.IsFalse(marquee.IsRunning);Assert.IsTrue(float.IsFinite(text.textInfo.meshInfo[0].vertices[0].x));marquee.Speed=0;
#endif
            yield return new WaitForEndOfFrame();Assert.IsFalse(text.canvasRenderer.cull,"RectMask2D must render the visible text region.");
#if UNITY_EDITOR
            Capture("marquee-rectmask.png");
#endif
            yield return new WaitForEndOfFrame();Object.Destroy(clip.GetComponent<RectMask2D>());yield return null;var image=clip.gameObject.AddComponent<Image>();image.color=new Color(0,0,0,0.7f);clip.gameObject.AddComponent<Mask>();marquee.Refresh();marquee.SetMarqueeEnabled(true,60);yield return null;yield return new WaitForEndOfFrame();
            Assert.IsTrue(marquee.IsRunning);Assert.GreaterOrEqual(text.textInfo.materialCount,2);
#if UNITY_EDITOR
            Capture("marquee-mask.png");
#endif
            yield return new WaitForEndOfFrame();
            marquee.StartDelay=0.08f;marquee.EndDelay=0.08f;marquee.SetMarqueeEnabled(true,1000000000);marquee.ResetPosition();
            float startX=text.textInfo.meshInfo[0].vertices[0].x;
            yield return new WaitForSecondsRealtime(0.03f);Assert.That(text.textInfo.meshInfo[0].vertices[0].x,Is.EqualTo(startX).Within(0.01),"Start delay must hold geometry.");
            float deadline=Time.unscaledTime+0.5f;while(Mathf.Abs(text.textInfo.meshInfo[0].vertices[0].x-startX)<0.1f && Time.unscaledTime<deadline)yield return null;
            float endX=text.textInfo.meshInfo[0].vertices[0].x;Assert.Less(endX,startX-1);
            yield return new WaitForSecondsRealtime(0.03f);Assert.That(text.textInfo.meshInfo[0].vertices[0].x,Is.EqualTo(endX).Within(0.01),"End delay must hold geometry.");
            deadline=Time.unscaledTime+0.5f;while(Mathf.Abs(text.textInfo.meshInfo[0].vertices[0].x-startX)>0.1f && Time.unscaledTime<deadline)yield return null;
            Assert.That(text.textInfo.meshInfo[0].vertices[0].x,Is.EqualTo(startX).Within(0.1),"Marquee must return to its start.");
            // Global SetSkin/RefreshAll can mutate unrelated live texts. That integration remains suspended under UIRT-A02.
            var alternate=FixtureFont("Packages/com.ember/SharedAssets/Fonts/钉钉进步体/DingTalk-JinBuTi.ttf");text.font=alternate;text.fontSharedMaterial=alternate.material;
            text.text="Changed width and font skin with a long single line";text.margin=new Vector4(8,0,8,0);rect.sizeDelta=new Vector2(160,72);marquee.Refresh();yield return null;Assert.IsTrue(marquee.IsRunning);
            marquee.SetMarqueeEnabled(false);var original=text.textInfo.meshInfo[0].vertices[0];rect.gameObject.SetActive(false);rect.gameObject.SetActive(true);marquee.SetMarqueeEnabled(true,30);marquee.Refresh();marquee.SetMarqueeEnabled(false);Assert.AreEqual(original,text.textInfo.meshInfo[0].vertices[0]);
        }

        [UnityTest] public IEnumerator ImageLoaderAndImageExRoundedCombinationRenderWithoutOwnedReferences()
        {
            var canvas=Go("ImageCanvas",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var texture=new Texture2D(80,40);_objects.Add(texture);var pixels=new Color[3200];for(int y=0;y<40;y++)for(int x=0;x<80;x++)pixels[y*80+x]=x<40?Color.cyan:Color.magenta;texture.SetPixels(pixels);texture.Apply();
            var first=UnityEngine.Sprite.Create(texture,new Rect(0,0,40,40),Vector2.one*0.5f);var second=UnityEngine.Sprite.Create(texture,new Rect(40,0,40,40),Vector2.one*0.5f);_objects.Add(first);_objects.Add(second);
            var rect=Rect("RoundedImage",canvas.transform,new Vector2(240,160));var image=rect.gameObject.AddComponent<EUIImageEx>();image.Animated=false;var effect=rect.gameObject.AddComponent<EUIRoundedImageModifier>();effect.Radius=36;
            var loader=rect.gameObject.AddComponent<EUIImageLoader>();loader.Load("image");_provider.Requests[0].Finish(first);yield return new WaitForEndOfFrame();Capture("image-async-rounded.png");
            image.SpriteArray=new[]{first,second};image.SpriteIndex=1;Assert.AreSame(second,image.overrideSprite);Assert.AreEqual(1,_provider.Requests[0].Releases);Assert.AreNotSame(first,image.sprite);
            effect.Radius=64;yield return new WaitForEndOfFrame();Capture("image-index-rounded.png");
        }

        private void Capture(string name)
        {
#if UNITY_EDITOR
            var shot=ScreenCapture.CaptureScreenshotAsTexture();System.IO.File.WriteAllBytes(EvidencePath(name),shot.EncodeToPNG());Object.Destroy(shot);
#endif
        }
#if UNITY_EDITOR
        private TMP_FontAsset FixtureFont(string path)
        {
            var font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(path));Assert.IsNotNull(font);_objects.Add(font);
            font.fallbackFontAssetTable=new List<TMP_FontAsset>();return font;
        }
#endif
        private string EvidencePath(string name)
        {var directory=System.IO.Path.GetFullPath("Temp/EmberUIRuntimeExtensionsTests");System.IO.Directory.CreateDirectory(directory);return System.IO.Path.Combine(directory,name);}
        [Serializable] private sealed class MarqueeEvidence
        {public int Materials;public float Elapsed;public Vector3[] Before,After;public string Font,Shader;public Vector2 RectSize;}
        [Serializable] private sealed class RemovalObservations {public RemovalObservation[] Cases;}
        [Serializable] private sealed class RemovalObservation
        {public bool Raw,Synchronous,ComponentOnly,MountedBeforeDestroy,ReleaseObserved,GraphicAlive,MountedAtRelease,MountedAfterDestroy;}

        private sealed class Provider:IResourceProvider
        {
            internal sealed class Request{internal Func<Object,bool> Finish;internal int Releases,Cancels;internal Action Released;}
            internal readonly List<Request> Requests=new();internal Object Synchronous;
            public float Progress=>1;public void Initialize(Action<bool> complete)=>complete(true);
            public EmberAssetHandle<T> LoadAssetHandle<T>(string p)where T:Object
            {var r=new Request();var h=EmberAssetHandle<T>.Create(p,()=>r.Cancels++,()=>{r.Releases++;r.Released?.Invoke();});r.Finish=x=>h.TryComplete(x as T,x!=null,"probe failure");Requests.Add(r);if(Synchronous)r.Finish(Synchronous);return h;}
            public void LoadAssetAsync<T>(string p,Action<T> c)where T:Object=>c(null);public AsyncOperation LoadSceneAsync(string p,LoadSceneMode m=LoadSceneMode.Additive)=>null;
            public void UnloadAsset(string p){}public void UnloadUnusedAssets(){}public EmberFileHandle LoadFileAsync(string p)=>null;public byte[] LoadFileSync(string p)=>null;
        }
    }
}
