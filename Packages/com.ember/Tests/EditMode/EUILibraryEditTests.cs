// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.IO;
using System.Linq;
using Ember.UIExtension;
using Ember.UIExtension.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ember.UI.Tests
{
    public class EUILibraryEditTests
    {
        private string _path;
        private EUILibrary _library;
        private GameObject _root;
        [SetUp] public void Setup()
        {
            _path="Assets/EUILibraryTest_"+Guid.NewGuid().ToString("N")+".prefab";
            _root=new GameObject("TestItem",typeof(RectTransform),typeof(Image),typeof(EUIBinding));
            var prefab=PrefabUtility.SaveAsPrefabAsset(_root,_path);
            _library=ScriptableObject.CreateInstance<EUILibrary>();
            _library.Entries.Add(new EUILibraryEntry { Id="test",Family="Button",Prefab=prefab,RequiredRoles=new[]{"background"},
                Slots={new EUILibrarySlot { Id="background",Role="background",Target=prefab.GetComponent<Image>(),ComponentType=typeof(Image).FullName,Properties=new[]{"m_Color"} }} });
        }
        [TearDown] public void TearDown()
        { UnityEngine.Object.DestroyImmediate(_root); UnityEngine.Object.DestroyImmediate(_library); AssetDatabase.DeleteAsset(_path); AssetDatabase.DeleteAsset(_path+".json"); }
        [Test] public void DuplicateIdentity_StopsExactLookup()
        { _library.Entries.Add(_library.Entries[0]); Assert.Throws<InvalidOperationException>(()=>EUILibraryService.ResolveExact(_library,"test")); }
        [Test] public void MissingSlot_StopsCatalog()
        { _library.Entries[0].Slots[0].Target=null; Assert.Throws<InvalidOperationException>(()=>EUILibraryService.GenerateCatalog(_library,_path+".json")); }
        [Test] public void WrongType_IsDiagnosed()
        { _library.Entries[0].Slots[0].ComponentType=typeof(Button).FullName; Assert.IsTrue(EUILibraryService.Validate(_library).Any(e=>e.Contains("类型不兼容"))); }
        [Test] public void ExternalSlot_IsRejected()
        { _library.Entries[0].Slots[0].Target=_root.GetComponent<Image>(); Assert.IsTrue(EUILibraryService.Validate(_library).Any(e=>e.Contains("越过组件边界"))); }
        [Test] public void Catalog_ExpiresWhenDefinitionChanges()
        { EUILibraryService.GenerateCatalog(_library,_path+".json"); Assert.IsTrue(EUILibraryService.IsCatalogCurrent(_library,_path+".json")); _library.Entries[0].Family="Changed"; Assert.IsFalse(EUILibraryService.IsCatalogCurrent(_library,_path+".json")); }
        [Test] public void TwoInstances_KeepOverridesAndSourceIndependent()
        {
            var prefab=_library.Entries[0].Prefab; var bytes=File.ReadAllBytes(_path);
            var a=(GameObject)PrefabUtility.InstantiatePrefab(prefab); var b=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try {
                ((Image)EUILibraryService.ResolveInstanceSlot(_library,"test",a,"background","m_Color")).color=Color.red;
                ((Image)EUILibraryService.ResolveInstanceSlot(_library,"test",b,"background","m_Color")).color=Color.blue;
                Assert.AreEqual(Color.red,a.GetComponent<Image>().color); Assert.AreEqual(Color.blue,b.GetComponent<Image>().color);
                Assert.AreEqual(Color.white,prefab.GetComponent<Image>().color); CollectionAssert.AreEqual(bytes,File.ReadAllBytes(_path));
                Assert.Throws<InvalidOperationException>(()=>EUILibraryService.ResolveInstanceSlot(_library,"test",prefab,"background","m_Color"));
                Assert.Throws<InvalidOperationException>(()=>EUILibraryService.ResolveInstanceSlot(_library,"test",a,"background","m_Sprite"));
            } finally { UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); }
        }
        [Test] public void MissingInheritedField_AndBindingCycle_AreErrors()
        {
            var parent = _library.Entries[0].Prefab.GetComponent<EUIBinding>();
            EUIBindingEditorUtility.SetBindings(parent,new[]{new EUIBindingEntrySnapshot {Name="Image",GameObjectPath="",WidgetType=EUIBinding.WidgetTypes.Image}});
            var child = _root.GetComponent<EUIBinding>();
            EUIBindingEditorUtility.SetBaseBinding(child,_library.Entries[0].Prefab);
            Assert.IsTrue(EUIBindingEditorUtility.ValidateBinding(child).Issues.Any(i=>i.Message.Contains("缺失继承控件")));
            EUIBindingEditorUtility.SetBaseBinding(parent,_library.Entries[0].Prefab);
            Assert.IsTrue(EUIBindingEditorUtility.ValidateBinding(parent).Issues.Any(i=>i.Message.Contains("继承循环")));
        }

        [Test] public void NestedSourceInsidePageVariant_ResolvesPublicSlotWithoutChangingSource()
        {
            var pagePath=_path+".page.prefab";var variantPath=_path+".variant.prefab";
            var page=new GameObject("Page",typeof(RectTransform));GameObject variant=null,loaded=null;
            var before=File.ReadAllBytes(_path);
            try
            {
                var nested=(GameObject)PrefabUtility.InstantiatePrefab(_library.Entries[0].Prefab,page.transform);
                nested.name="Nested";
                var source=PrefabUtility.SaveAsPrefabAsset(page,pagePath);
                variant=(GameObject)PrefabUtility.InstantiatePrefab(source);
                PrefabUtility.SaveAsPrefabAsset(variant,variantPath);
                loaded=PrefabUtility.LoadPrefabContents(variantPath);
                var image=(Image)EUILibraryService.ResolveInstanceSlot(_library,"test",loaded.transform.Find("Nested").gameObject,"background","m_Color");
                image.color=Color.red;
                Assert.AreEqual(Color.red,image.color);
                CollectionAssert.AreEqual(before,File.ReadAllBytes(_path));
            }
            finally
            {
                if(loaded)PrefabUtility.UnloadPrefabContents(loaded);
                if(variant)UnityEngine.Object.DestroyImmediate(variant);
                UnityEngine.Object.DestroyImmediate(page);
                AssetDatabase.DeleteAsset(variantPath);AssetDatabase.DeleteAsset(pagePath);
            }
        }
        [Test] public void StructureOnlySource_CannotBecomeBindingBase()
        {
            var binding=_library.Entries[0].Prefab.GetComponent<EUIBinding>();
            var snapshot=EUIBindingEditorUtility.GetBindingSnapshot(binding); snapshot.NoCodeGen=true;
            EUIBindingEditorUtility.ApplyBindingSnapshot(binding,snapshot);
            var request=new EUICreationRequest { SourceSkeleton=binding.gameObject,InheritBinding=true,Role=binding.Role };
            Assert.IsFalse(EUILibraryService.ValidateCreationSource(request,out var error)); Assert.That(error,Does.Contain("逻辑类"));
        }
    }
}

