using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ember.Core;
using Ember.Resource;
using Ember.SceneUI.Integration;
using Ember.UIExtension;
using Ember.UIExtension.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace Ember.UI.Tests
{
    public abstract class EUIResourceFixture
    {
        private readonly FieldInfo _instance=typeof(EmberSingleton<EmberResourceManager>).GetField("_instance",BindingFlags.Static|BindingFlags.NonPublic);
        private object _previous;private IResourceProvider _service;
        protected TestProvider Provider;
        protected readonly List<Object> Objects=new();
        [SetUp] public void ResourceSetUp()
        {
            _previous=_instance.GetValue(null);_service=EmberServiceLocator.TryResolve<IResourceProvider>();
            _instance.SetValue(null,new EmberResourceManager());EmberServiceLocator.Unregister<IResourceProvider>();
            Provider=new TestProvider();EmberResourceManager.Instance.Initialize(Provider);
        }
        [TearDown] public void ResourceTearDown()
        {
            foreach(var go in Objects.OfType<GameObject>()){if(go){go.GetComponent<EUIImageLoader>()?.Clear();go.GetComponent<EUIRawImageLoader>()?.Clear();}}
            for(int i=Objects.Count-1;i>=0;i--)if(Objects[i])Object.DestroyImmediate(Objects[i]);Objects.Clear();
            _instance.SetValue(null,_previous);EmberServiceLocator.Unregister<IResourceProvider>();if(_service!=null)EmberServiceLocator.Register(_service);
        }
        protected Texture2D Texture(int width=16,int height=8){var t=new Texture2D(width,height);Objects.Add(t);return t;}
        protected Sprite Sprite(){var t=Texture();var s=UnityEngine.Sprite.Create(t,new Rect(0,0,t.width,t.height),Vector2.one*0.5f,100);Objects.Add(s);return s;}
        protected void UseProvider(IResourceProvider provider)
        {
            _instance.SetValue(null,new EmberResourceManager());EmberServiceLocator.Unregister<IResourceProvider>();
            if(provider!=null)EmberResourceManager.Instance.Initialize(provider);
        }
        protected sealed class TestProvider:IResourceProvider
        {
            public sealed class Request{public string Path;public Func<Object,bool> Complete;public int Releases,Cancels;public Object Asset;}
            public readonly List<Request> Requests=new();public Object Synchronous;public bool Throw;public Action<Request> OnRelease;
            public float Progress=>1;public void Initialize(Action<bool> complete)=>complete(true);
            public EmberAssetHandle<T> LoadAssetHandle<T>(string path)where T:Object
            {
                if(Throw)throw new InvalidOperationException("provider probe");
                var r=new Request{Path=path};var h=EmberAssetHandle<T>.Create(path,()=>r.Cancels++,()=>{r.Releases++;OnRelease?.Invoke(r);});
                r.Complete=x=>{r.Asset=x;return h.TryComplete(x as T,x!=null,"not found");};Requests.Add(r);if(Synchronous)r.Complete(Synchronous);return h;
            }
            public bool Finish(int index,Object asset)=>Requests[index].Complete(asset);
            public void LoadAssetAsync<T>(string p,Action<T> c)where T:Object=>c(null);
            public AsyncOperation LoadSceneAsync(string p,LoadSceneMode m=LoadSceneMode.Additive)=>null;
            public void UnloadAsset(string p){}public void UnloadUnusedAssets(){}public EmberFileHandle LoadFileAsync(string p)=>null;public byte[] LoadFileSync(string p)=>null;
        }
    }

    public sealed class EUIAsyncImageEditTests:EUIResourceFixture
    {
        private sealed class View
        {
            internal GameObject Go;internal Image Image;internal RawImage Raw;internal EUIImageLoader SpriteLoader;internal EUIRawImageLoader TextureLoader;
            internal Object Read=>Image?(Object)Image.overrideSprite:Raw.texture;
            internal long Load(string p,Action<EUIImageLoadResult> c=null)=>Image?SpriteLoader.Load(p,c):TextureLoader.Load(p,c);
            internal void Clear(){if(Image)SpriteLoader.Clear();else TextureLoader.Clear();}
            internal void Cancel(){if(Image)SpriteLoader.CancelLoading();else TextureLoader.CancelLoading();}
            internal void Set(Object value){if(Image)SpriteLoader.SetSprite((Sprite)value);else TextureLoader.SetTexture((Texture)value);}
            internal void Direct(Object value){if(Image)Image.sprite=(Sprite)value;else Raw.texture=(Texture)value;}
            internal void KeepPrevious(){if(Image)SpriteLoader.LoadingDisplay=EUIImageLoadingDisplay.KeepPrevious;else TextureLoader.LoadingDisplay=EUIImageLoadingDisplay.KeepPrevious;}
            internal string Path=>Image?SpriteLoader.CurrentPath:TextureLoader.CurrentPath;
        }
        private View Create(bool raw,bool inactive=false)
        {
            var v=new View{Go=new GameObject("ImageFixture",typeof(RectTransform))};Objects.Add(v.Go);v.Go.SetActive(!inactive);
            if(raw){v.Raw=v.Go.AddComponent<RawImage>();v.TextureLoader=v.Go.AddComponent<EUIRawImageLoader>();}
            else{v.Image=v.Go.AddComponent<Image>();v.SpriteLoader=v.Go.AddComponent<EUIImageLoader>();}return v;
        }
        private Object Asset(bool raw)=>raw?(Object)Texture():Sprite();
        [TestCase(false)][TestCase(true)] public void SamePendingAndSuccessDeduplicate_ButEveryUILoadHasTerminalResult(bool raw)
        {
            var view=Create(raw);var asset=Asset(raw);var results=new List<EUIImageLoadResult>();
            long a=view.Load("A",results.Add),b=view.Load("A",results.Add);Assert.AreEqual(1,Provider.Requests.Count);Assert.AreEqual(a,results[0].OperationId);Assert.AreEqual(EUIImageLoadStatus.Replaced,results[0].Status);
            Provider.Finish(0,asset);Assert.AreSame(asset,view.Read);Assert.AreEqual(b,results[1].OperationId);Assert.AreEqual(EUIImageLoadStatus.Success,results[1].Status);
            view.Load("A",results.Add);Assert.AreEqual(1,Provider.Requests.Count);Assert.AreEqual(3,results.Count);
            bool mounted=false;Provider.OnRelease=r=>mounted|=r.Asset && view.Read==r.Asset;view.Clear();Assert.IsFalse(mounted);Assert.AreEqual(1,Provider.Requests[0].Releases);Assert.IsTrue(asset);
        }
        [TestCase(false)][TestCase(true)] public void ReverseCompletion_ABA_AndFailureRetry(bool raw)
        {
            var view=Create(raw);var a=Asset(raw);var b=Asset(raw);view.Load("A");Provider.Finish(0,a);view.KeepPrevious();
            var results=new List<EUIImageLoadResult>();view.Load("B",results.Add);view.Load("A",results.Add);Assert.AreSame(a,view.Read);Assert.IsFalse(Provider.Finish(1,b));Assert.AreEqual(EUIImageLoadStatus.Replaced,results[0].Status);
            view.Load("B",results.Add);Provider.Finish(2,null);Assert.AreEqual(EUIImageLoadStatus.Failed,results.Last().Status);Assert.IsNull(view.Path);
            view.Load("B",results.Add);Provider.Finish(3,b);Assert.AreSame(b,view.Read);Assert.AreEqual(4,Provider.Requests.Count);
        }
        [TestCase(false)][TestCase(true)] public void ManualDirectAssignmentAndExplicitSameValueRetireRequests(bool raw)
        {
            var view=Create(raw);var external=Asset(raw);var incoming=Asset(raw);var results=new List<EUIImageLoadResult>();
            view.Load("A",results.Add);view.Direct(external);Assert.AreEqual(EUIImageLoadStatus.ManualOverride,results.Single().Status);Provider.Finish(0,incoming);Assert.AreSame(external,view.Read);
            view.Load("B",results.Add);view.Set(view.Read);Assert.AreEqual(EUIImageLoadStatus.ManualOverride,results.Last().Status);Assert.AreEqual(1,Provider.Requests[1].Releases);
        }
        [TestCase(false)][TestCase(true)] public void SynchronousInactiveBind_NativeSize_ClearAndCallbackReentry(bool raw)
        {
            var view=Create(raw,true);var asset=Asset(raw);Provider.Synchronous=asset;
            if(raw)view.TextureLoader.KeepNativeSize=true;else view.SpriteLoader.KeepNativeSize=true;
            EUIImageLoadResult result=default;view.Load("sync",x=>{result=x;view.Clear();throw new Exception("image callback probe");});
            Assert.AreEqual(EUIImageLoadStatus.Success,result.Status);Assert.IsNull(view.Path);Assert.AreEqual(1,Provider.Requests[0].Releases);
            Assert.That(((RectTransform)view.Go.transform).rect.width,Is.EqualTo(16).Within(0.01));
            view.Load("sync");Assert.AreEqual(2,Provider.Requests.Count);view.Load(null,x=>result=x);Assert.AreEqual(EUIImageLoadStatus.Cleared,result.Status);
        }
        [TestCase(false)][TestCase(true)] public void CancellationRestoresOwnedPrevious_AndProviderExceptionsFail(bool raw)
        {
            var view=Create(raw);var asset=Asset(raw);view.Load("A");Provider.Finish(0,asset);
            var results=new List<EUIImageLoadResult>();view.Load("B",results.Add);view.Cancel();Assert.AreSame(asset,view.Read);Assert.AreEqual("A",view.Path);Assert.AreEqual(EUIImageLoadStatus.Cancelled,results.Last().Status);
            Provider.Throw=true;view.Load("throw",results.Add);Assert.AreEqual(EUIImageLoadStatus.Failed,results.Last().Status);StringAssert.Contains("provider probe",results.Last().Error);
        }
        [Test] public void ImageOverrideAndImageExAnimationHaveExplicitOwnership()
        {
            var go=new GameObject("ImageEx",typeof(RectTransform),typeof(EUIImageEx),typeof(EUIImageLoader));Objects.Add(go);
            var image=go.GetComponent<EUIImageEx>();var loader=go.GetComponent<EUIImageLoader>();var manual=Sprite();var results=new List<EUIImageLoadResult>();
            loader.Load("A",results.Add);image.overrideSprite=manual;Assert.AreEqual(EUIImageLoadStatus.ManualOverride,results.Last().Status);
            image.Animated=true;loader.Load("B",results.Add);Assert.AreEqual(EUIImageLoadStatus.Failed,results.Last().Status);Assert.AreEqual(1,Provider.Requests.Count);
            image.Animated=false;loader.Load("B",results.Add);var loaded=Sprite();Provider.Finish(1,loaded);bool mountedAtRelease=false;
            Provider.OnRelease=r=>mountedAtRelease|=image.sprite==r.Asset || image.overrideSprite==r.Asset;
            image.SpriteArray=new[]{manual};image.SpriteIndex=0;Assert.AreSame(manual,image.overrideSprite);Assert.IsNull(loader.CurrentPath);Assert.IsFalse(mountedAtRelease);Assert.AreNotSame(loaded,image.sprite);
        }
        [Test] public void RealResourcesProviderLoadsSpriteAndTextureSynchronously_AndKeepsSharedAssetAlive()
        {
            string folder="Assets/__EmberImageResources_"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(folder+"/Resources");
            string spritePath=folder+"/Resources/RuntimeSprite.png",texturePath=folder+"/Resources/RuntimeTexture.png";
            var tex=Texture();File.WriteAllBytes(spritePath,tex.EncodeToPNG());File.WriteAllBytes(texturePath,tex.EncodeToPNG());
            try
            {
                AssetDatabase.ImportAsset(spritePath,ImportAssetOptions.ForceSynchronousImport);AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(spritePath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.SaveAndReimport();
                var provider=new ResourcesProvider();var sh=provider.LoadAssetHandle<Sprite>("RuntimeSprite");var th=provider.LoadAssetHandle<Texture>("RuntimeTexture");
                Assert.IsTrue(sh.IsDone && sh.Succeeded);Assert.IsTrue(th.IsDone && th.Succeeded);var s=sh.Asset;var t=th.Asset;sh.Dispose();th.Dispose();Assert.IsTrue(s);Assert.IsTrue(t);
                UseProvider(provider);var spriteView=Create(false);var textureView=Create(true);var results=new List<EUIImageLoadResult>();
                spriteView.Load("RuntimeSprite",results.Add);textureView.Load("RuntimeTexture",results.Add);Assert.AreEqual(2,results.Count);Assert.IsTrue(results.All(x=>x.Status==EUIImageLoadStatus.Success));Assert.AreSame(s,spriteView.Read);Assert.AreSame(t,textureView.Read);
                spriteView.Clear();textureView.Clear();Assert.IsTrue(s);Assert.IsTrue(t);
            }
            finally{AssetDatabase.DeleteAsset(folder);}
        }
        [Test] public void UninitializedManagerAndBorrowedDisplayPoliciesHaveConcreteResults()
        {
            var view=Create(false);var loader=view.SpriteLoader;var placeholder=Sprite();var failed=Sprite();loader.Placeholder=placeholder;loader.FailureImage=failed;
            UseProvider(null);EUIImageLoadResult result=default;view.Load("A",x=>result=x);Assert.AreEqual(EUIImageLoadStatus.Failed,result.Status);StringAssert.Contains("not initialized",result.Error);Assert.AreSame(failed,view.Read);
            UseProvider(Provider);view.Load("B");Assert.AreSame(placeholder,view.Read);var good=Sprite();Provider.Finish(0,good);loader.KeepPreviousOnFailure=true;loader.LoadingDisplay=EUIImageLoadingDisplay.KeepPrevious;view.Load("C");Assert.AreSame(good,view.Read);Provider.Finish(1,null);Assert.AreSame(good,view.Read);Assert.AreEqual("B",view.Path);
            view.Clear();Assert.IsTrue(placeholder);Assert.IsTrue(failed);
        }
        [Test] public void RealSceneUIHandleAdapterPropagatesCancelAndDisposeToAssetHandle()
        {
            var loader=new EmberSceneUIViewResourceLoader();var handle=loader.LoadPrefab("Prefab");int calls=0;handle.Completed+=()=>calls++;
            handle.Cancel();Assert.IsTrue(handle.IsDone);Assert.IsFalse(handle.Succeeded);Assert.AreEqual(1,calls);Assert.AreEqual(1,Provider.Requests[0].Cancels);
            handle.Dispose();handle.Dispose();Assert.AreEqual(1,Provider.Requests[0].Releases);Assert.IsFalse(Provider.Finish(0,null));
        }
    }

    public sealed class EUIImageExAndRoundedEditTests
    {
        [Test] public void SerializedMultiSpriteIndexUndoAndInvalidValuesFallBackToBase()
        {
            var a=new GameObject("A",typeof(RectTransform),typeof(EUIImageEx));var b=new GameObject("B",typeof(RectTransform),typeof(EUIImageEx));var tex=new Texture2D(4,4);var s0=Sprite.Create(tex,new Rect(0,0,2,4),Vector2.one*0.5f);var s1=Sprite.Create(tex,new Rect(2,0,2,4),Vector2.one*0.5f);
            try
            {
                var images=new[]{a.GetComponent<EUIImageEx>(),b.GetComponent<EUIImageEx>()};foreach(var image in images){image.sprite=s0;image.SpriteArray=new[]{s0,s1};}
                using(var so=new SerializedObject(images)){so.FindProperty("_animated").boolValue=false;so.FindProperty("_spriteIndex").intValue=1;so.ApplyModifiedProperties();}
                foreach(var image in images)Assert.AreSame(s1,image.overrideSprite);
                Undo.PerformUndo();foreach(var image in images)Assert.AreSame(s0,image.overrideSprite);
                foreach(var image in images){image.SpriteIndex=99;Assert.AreSame(s0,image.overrideSprite);image.SpriteIndex=0;image.SpriteArray=new Sprite[]{null};Assert.AreSame(s0,image.overrideSprite);image.SpriteArray=Array.Empty<Sprite>();Assert.AreSame(s0,image.overrideSprite);}
                var editor=UnityEditor.Editor.CreateEditor(images);Assert.IsInstanceOf<EUIImageExEditor>(editor);Object.DestroyImmediate(editor);
            }
            finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);Object.DestroyImmediate(s0);Object.DestroyImmediate(s1);Object.DestroyImmediate(tex);}
        }
        [TestCase(0f,0)][TestCase(float.NaN,100)][TestCase(1000f,-20)]
        public void RoundedMeshIsFinite_UsesAspectAndActiveState(float radius,int triangles)
        {
            var go=new GameObject("Rounded",typeof(RectTransform),typeof(Image),typeof(EUIRoundedImageModifier));var tex=new Texture2D(40,20);var sprite=Sprite.Create(tex,new Rect(0,0,40,20),Vector2.one*0.5f);
            try
            {
                var image=go.GetComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.rectTransform.sizeDelta=new Vector2(100,100);
                var effect=go.GetComponent<EUIRoundedImageModifier>();effect.Radius=radius;effect.TriangleNum=triangles;
                using var vh=new VertexHelper();effect.ModifyMesh(vh);Assert.Greater(vh.currentVertCount,0);var v=new UIVertex();
                for(int i=0;i<vh.currentVertCount;i++){vh.PopulateUIVertex(ref v,i);Assert.IsTrue(float.IsFinite(v.position.x)&&float.IsFinite(v.uv0.x));Assert.That(Mathf.Abs(v.position.y),Is.LessThanOrEqualTo(25.01));}
                int count=vh.currentVertCount;effect.enabled=false;effect.ModifyMesh(vh);Assert.AreEqual(count,vh.currentVertCount);
                effect.enabled=true;image.rectTransform.sizeDelta=Vector2.zero;effect.ModifyMesh(vh);Assert.AreEqual(0,vh.currentVertCount);
                foreach(var type in new[]{Image.Type.Sliced,Image.Type.Tiled,Image.Type.Filled}){image.type=type;vh.AddVert(Vector3.zero,Color.white,Vector2.zero);count=vh.currentVertCount;effect.ModifyMesh(vh);Assert.AreEqual(count,vh.currentVertCount);}
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(sprite);Object.DestroyImmediate(tex);}
        }
    }
}
