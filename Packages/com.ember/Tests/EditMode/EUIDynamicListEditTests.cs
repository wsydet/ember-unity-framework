using System;
using System.Linq;
using Ember.UIExtension;
using Ember.UIExtension.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ember.UI.Tests
{
    public sealed class EUIDynamicListProbe : EUILogic
    {
        public static int Created, Disposed;
        public override void OnBind() { Created++; }
        public override void OnDispose() { Disposed++; }
    }

    public sealed class EUIDynamicListEditTests
    {
        private GameObject _root;
        private string _path;
        private EUIDynamicListHost _host;

        [SetUp]
        public void SetUp()
        {
            EUIDynamicListProbe.Created = EUIDynamicListProbe.Disposed = 0;
            _path = "Assets/DynamicListTest_" + Guid.NewGuid().ToString("N") + ".prefab";
            var item = new GameObject("Item", typeof(RectTransform), typeof(EUIBinding));
            EUIBindingEditorUtility.SetPageInfo(item.GetComponent<EUIBinding>(), "", "Tests", typeof(EUIDynamicListProbe).FullName, false, PageType.MainPage);
            var prefab = PrefabUtility.SaveAsPrefabAsset(item, _path);
            UnityEngine.Object.DestroyImmediate(item);
            _root = new GameObject("Host", typeof(RectTransform), typeof(ScrollRect), typeof(EUIDynamicListHost));
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)); viewport.transform.SetParent(_root.transform,false);
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)); content.transform.SetParent(viewport.transform,false);
            content.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;
            content.GetComponent<VerticalLayoutGroup>().childControlHeight = true;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = _root.GetComponent<ScrollRect>(); scroll.viewport=(RectTransform)viewport.transform; scroll.content=(RectTransform)content.transform; scroll.horizontal=false;
            _host = _root.GetComponent<EUIDynamicListHost>();
            using var so = new SerializedObject(_host);
            so.FindProperty("_scroll").objectReferenceValue=scroll;
            so.FindProperty("_itemTemplate").objectReferenceValue=prefab;
            so.FindProperty("_dataBinding").stringValue="test:string-key";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown() { UnityEngine.Object.DestroyImmediate(_root); AssetDatabase.DeleteAsset(_path); }

        [Test]
        public void Counts_Reorder_DuplicateKeys_AndClearKeepOneLogicPerInstance()
        {
            using var list = _host.Create<string>(x=>x, _=>{});
            foreach (var count in new[]{0,1,3,20,3,1,0})
            {
                list.Replace(Enumerable.Range(0,count).Select(x=>x.ToString()).ToArray());
                Assert.AreEqual(count,list.Count);
                Assert.AreEqual(count,_host.Content.Cast<Transform>().Count(x=>x.gameObject.activeSelf));
            }
            Assert.AreEqual(20,list.CreatedCount); Assert.AreEqual(20,list.LogicCreatedCount);
            list.Replace(new[]{"a","b","c"}); var old = list[0];
            list.Replace(new[]{"c","a","b"});
            Assert.AreEqual("c",list[0].Key); Assert.AreEqual(0,list[0].Index); Assert.IsFalse(old.IsCurrent);
            Assert.Throws<ArgumentException>(()=>list.Replace(new[]{"x","x"}));
            Assert.AreEqual("c",list[0].Data);
            list.Clear(); Assert.AreEqual(0,list.Count); Assert.AreEqual(20,list.CachedCount);
            list.Dispose(); Assert.AreEqual(20,list.DestroyedCount); Assert.AreEqual(20,EUIDynamicListProbe.Disposed);
        }

        [Test]
        public void LeaseCancelsAndUnsubscribes_RejectsLateResult_ButHideRetainsLease()
        {
            var subscriptions=0; var fakeHandles=0;
            using var list=_host.Create<string>(x=>x,lease=>{ subscriptions++; fakeHandles++; lease.Track(()=>subscriptions--); lease.Track(()=>fakeHandles--); });
            list.Replace(new[]{"old"}); var old=list[0]; var item=old.Item;
            list.Hide(); Assert.IsTrue(old.IsCurrent); Assert.IsFalse(item.IsVisible); list.Show();
            list.Clear(); Assert.IsTrue(old.Cancellation.IsCancellationRequested); Assert.AreEqual(0,subscriptions); Assert.AreEqual(0,fakeHandles);
            list.Replace(new[]{"new"}); Assert.AreSame(item,list[0].Item);
            var value="new"; Assert.IsFalse(old.TryApply(_=>value="stale")); Assert.AreEqual("new",value);
            list.Dispose(); Assert.AreEqual(0,subscriptions); Assert.AreEqual(0,fakeHandles);
        }

        [Test]
        public void BindingExceptionAndHostDisposalReleaseEveryLease()
        {
            var active=0;
            using var list=_host.Create<string>(x=>x,lease=>{ active++; lease.Track(()=>active--); if(lease.Data=="bad") throw new InvalidOperationException("probe"); });
            Assert.Throws<InvalidOperationException>(()=>list.Replace(new[]{"ok","bad"}));
            Assert.AreEqual(0,list.Count); Assert.AreEqual(0,active);
            list.Replace(new[]{"ok"});
            // 普通 MonoBehaviour EditMode 生命周期不保证自动派发；Play 另验真实 Destroy。
            typeof(EUIDynamicListHost).GetMethod("OnDestroy",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(_host,null);
            Assert.IsTrue(list.IsDisposed); Assert.AreEqual(0,active); Assert.AreEqual(list.CreatedCount,list.DestroyedCount);
        }

        [Test]
        public void CancellationReentrancyDisposesWithoutDoubleRelease()
        {
            using var list=_host.Create<string>(x=>x,_=>{});
            list.Replace(new[]{"one","two"});
            list[1].Cancellation.Register(list.Dispose);
            Assert.Throws<ObjectDisposedException>(()=>list.Clear());
            Assert.AreEqual(list.CreatedCount,list.DestroyedCount); Assert.AreEqual(2,EUIDynamicListProbe.Disposed);
        }

        [Test]
        public void CacheCapacityAndSingleOwnerAreEnforced()
        {
            using (var so = new SerializedObject(_host))
            { so.FindProperty("_cacheCapacity").intValue = 1; so.ApplyModifiedPropertiesWithoutUndo(); }
            using var list = _host.Create<string>(x=>x,_=>{});
            Assert.Throws<InvalidOperationException>(()=>_host.Create<string>(x=>x,_=>{}));
            list.Replace(new[]{"a","b","c"}); list.Clear();
            Assert.AreEqual(1,list.CachedCount); Assert.AreEqual(2,list.DestroyedCount);
            list.Dispose(); Assert.AreEqual(3,list.DestroyedCount);
            var fixedChild=new GameObject("FixedChild",typeof(RectTransform)); fixedChild.transform.SetParent(_host.Content,false);
            Assert.Throws<InvalidOperationException>(()=>_host.Create<string>(x=>x,_=>{}));
        }

        [Test]
        public void LayoutConflictsAreActionable_AndListContractChangesBlock()
        {
            Assert.IsEmpty(_host.ValidateConfiguration());
            _host.Content.GetComponent<ContentSizeFitter>().horizontalFit=ContentSizeFitter.FitMode.PreferredSize;
            Assert.IsTrue(_host.ValidateConfiguration().Any(x=>x.Contains("循环")));
            var before=new EUIContract(); var after=new EUIContract();
            after.lists.Add(new EUIContractList { itemGuid="different" });
            Assert.IsTrue(EUIContractService.Compare(before,after).Any(x=>x.category=="list-contract" && x.blocksApply));
        }
    }
}
