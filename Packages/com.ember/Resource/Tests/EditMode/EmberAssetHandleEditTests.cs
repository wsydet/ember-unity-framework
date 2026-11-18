using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Ember.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Ember.Resource.Tests
{
    public sealed class EmberAssetHandleEditTests
    {
        [Test]
        public void CancelIsTerminalBeforeBackendReentry_AndReleaseIsIndependent()
        {
            int cancel=0, release=0, notified=0;
            bool terminalBeforeBackend=false,acceptedReentry=true;
            EmberAssetHandle<Texture> handle=null;
            handle=EmberAssetHandle<Texture>.Create("A",()=> { cancel++;terminalBeforeBackend=handle.IsDone && handle.IsCancelled;acceptedReentry=handle.TryComplete(null,false); },()=>release++);
            handle.Completed += x=>{ Assert.IsNull(x); notified++; };
            handle.Cancel(); handle.Cancel();
            Assert.IsTrue(terminalBeforeBackend);Assert.IsFalse(acceptedReentry);
            Assert.AreEqual(1,cancel); Assert.AreEqual(1,notified); Assert.AreEqual(0,release);
            handle.Completed += x=>notified++;
            Assert.AreEqual(2,notified);
            handle.Dispose(); handle.Dispose(); handle.Completed += x=>notified++;
            Assert.AreEqual(1,release); Assert.AreEqual(2,notified); Assert.IsTrue(handle.IsDisposed);
        }

        [Test]
        public void SuccessDispose_ReleasesEachRequestOnce_AndDoesNotDestroySharedAsset()
        {
            var texture=new Texture2D(2,2); int releases=0, cancels=0;
            try
            {
                var a=EmberAssetHandle<Texture>.Create("same",()=>cancels++,()=>releases++);
                var b=EmberAssetHandle<Texture>.Create("same",()=>cancels++,()=>releases++);
                Assert.IsTrue(a.TryComplete(texture,true)); Assert.IsTrue(b.TryComplete(texture,true));
                int calls=0; a.Completed += x=>{ Assert.AreSame(texture,x); calls++; };
                a.Dispose(); a.Dispose(); b.Dispose();
                Assert.AreEqual(2,releases); Assert.AreEqual(0,cancels); Assert.AreEqual(1,calls);
                Assert.IsFalse(a.Succeeded); Assert.IsNull(a.Asset); Assert.IsTrue(texture);
            }
            finally { Object.DestroyImmediate(texture); }
        }

        [Test]
        public void ThrowingCallbacksAndHooks_DoNotPreventOtherNotificationsOrRelease()
        {
            int calls=0, releases=0;
            var h=EmberAssetHandle<Texture>.Create("A",()=>throw new Exception("cancel probe"),()=>{releases++;throw new Exception("release probe");});
            h.Completed += x=>throw new Exception("callback probe"); h.Completed += x=>calls++;
            Assert.DoesNotThrow(()=>h.Dispose()); h.Dispose();
            Assert.AreEqual(1,calls); Assert.AreEqual(1,releases); Assert.IsTrue(h.IsDone);
        }

        [Test]
        public void ProviderRejectedLateCompletion_RetainsBackendCleanupResponsibility()
        {
            int release=0, backendTokens=1;
            bool tokenReleased=false;
            var h=EmberAssetHandle<Texture>.Create("late",release:()=>{release++;tokenReleased=true;});
            h.Dispose();
            var texture=new Texture2D(1,1);
            try { if (!h.TryComplete(texture,true) && tokenReleased) backendTokens--; Assert.AreEqual(0,backendTokens); Assert.AreEqual(1,release); }
            finally { Object.DestroyImmediate(texture); }
        }
    }

    public sealed class EmberAssetHandleSlotEditTests
    {
        private readonly FieldInfo _instance=typeof(EmberSingleton<EmberResourceManager>).GetField("_instance",BindingFlags.Static|BindingFlags.NonPublic);
        private object _previous;
        private IResourceProvider _service;
        private ProbeProvider _provider;
        private Texture2D _a, _b;
        private EmberAssetHandleSlot<Texture> _slot;

        [SetUp] public void SetUp()
        {
            _previous=_instance.GetValue(null); _service=EmberServiceLocator.TryResolve<IResourceProvider>();
            _instance.SetValue(null,new EmberResourceManager()); EmberServiceLocator.Unregister<IResourceProvider>();
            _provider=new ProbeProvider(); EmberResourceManager.Instance.Initialize(_provider);
            _a=new Texture2D(1,1); _b=new Texture2D(2,2); _slot=new EmberAssetHandleSlot<Texture>();
        }
        [TearDown] public void TearDown()
        {
            _slot.Dispose(); _instance.SetValue(null,_previous);
            EmberServiceLocator.Unregister<IResourceProvider>(); if(_service!=null) EmberServiceLocator.Register(_service);
            Object.DestroyImmediate(_a); Object.DestroyImmediate(_b);
        }

        [Test] public void SamePending_DeduplicatesAndLatestCallbackWins_FailureRetries()
        {
            int old=0, latest=0;
            _slot.LoadAsync("A",x=>old++); _slot.LoadAsync("A",x=>latest++);
            Assert.AreEqual(1,_provider.Requests.Count);
            _provider.Finish(0,null); Assert.AreEqual(0,old); Assert.AreEqual(1,latest); Assert.IsNull(_slot.CurrentAssetPath);
            Assert.AreEqual("probe failure",_slot.LastError); Assert.AreEqual(1,_provider.Releases);
            Texture received=null;_slot.LoadAsync("A",x=>received=x); _provider.Finish(1,_a);Assert.AreSame(_a,received);
            Assert.AreEqual("A",_slot.CurrentAssetPath); Assert.AreSame(_a,_slot.CurrentAsset);
        }

        [Test] public void ReverseCompletion_AndABA_KeepCurrentUntilDisplaySwitch()
        {
            _slot.LoadAsync("A",x=>{}); _provider.Finish(0,_a);
            int stale=0;_slot.LoadAsync("B",x=>stale++);
            _slot.LoadAsync("A",x=>Assert.AreSame(_a,x));
            Assert.AreEqual(2,_provider.Requests.Count); Assert.AreEqual(1,_provider.Cancels); Assert.AreEqual(1,_provider.Releases);
            Assert.IsFalse(_provider.Finish(1,_b)); Assert.AreSame(_a,_slot.CurrentAsset);
            Assert.AreEqual(0,stale);
            _slot.LoadAsync("B",x=>{Assert.AreEqual(1,_provider.Releases);Assert.AreSame(_b,x);}); _provider.Finish(2,_b);
            Assert.AreEqual(2,_provider.Releases); _slot.Clear(); Assert.AreEqual(3,_provider.Releases);
        }

        [Test] public void SynchronousCompletion_ReentrantClear_AndCallbackThrow_ReleaseAll()
        {
            _provider.SyncAsset=_a;
            _slot.LoadAsync("A",x=>{Assert.AreSame(_a,x);_slot.Clear();throw new Exception("slot probe");});
            Assert.IsFalse(_slot.IsLoading); Assert.IsNull(_slot.CurrentAsset); Assert.AreEqual(1,_provider.Releases);
            _slot.LoadAsync("A",x=>{}); Assert.AreEqual(2,_provider.Requests.Count);
        }

        [TestCase("en-US")][TestCase("tr-TR")]
        public void AddressesAreOrdinalAndUnmodified(string culture)
        {
            var old=CultureInfo.CurrentCulture;
            try { CultureInfo.CurrentCulture=new CultureInfo(culture); _slot.LoadAsync("ICON",x=>{}); _slot.LoadAsync("icon",x=>{}); _slot.LoadAsync(" icon ",x=>{}); Assert.AreEqual(3,_provider.Requests.Count); Assert.AreEqual(" icon ",_provider.Requests[2].path); }
            finally { CultureInfo.CurrentCulture=old; }
        }

        [Test] public void CancelAndClearAreReusable_DisposeIsPermanent()
        {
            _slot.LoadAsync("A",x=>Assert.Fail()); _slot.CancelLoading();
            Assert.AreEqual(1,_provider.Releases); Assert.AreEqual(1,_provider.Cancels);
            _slot.Clear(); _slot.LoadAsync("B",x=>{}); _provider.Finish(1,_b); _slot.Dispose();
            int calls=0;_slot.LoadAsync("C",x=>calls++);Assert.AreEqual(1,calls);Assert.AreEqual(2,_provider.Requests.Count);
        }

        private sealed class ProbeProvider : IResourceProvider
        {
            public readonly List<(string path, Func<Object,bool> finish)> Requests=new();
            public int Cancels,Releases;
            public Object SyncAsset;
            public float Progress=>1;
            public void Initialize(Action<bool> complete)=>complete(true);
            public EmberAssetHandle<T> LoadAssetHandle<T>(string path) where T:Object
            {
                var h=EmberAssetHandle<T>.Create(path,()=>Cancels++,()=>Releases++);
                Requests.Add((path, x=>h.TryComplete(x as T,x!=null,"probe failure")));
                if(SyncAsset)h.TryComplete(SyncAsset as T,true);
                return h;
            }
            public bool Finish(int index,Object asset)=>Requests[index].finish(asset);
            public void LoadAssetAsync<T>(string p,Action<T> c) where T:Object=>c(null);
            public AsyncOperation LoadSceneAsync(string p,LoadSceneMode m=LoadSceneMode.Additive)=>null;
            public void UnloadAsset(string p){} public void UnloadUnusedAssets(){}
            public EmberFileHandle LoadFileAsync(string p)=>null;public byte[] LoadFileSync(string p)=>null;
        }
    }
}
