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
    public sealed class EUIContractProbeLogic : EUILogic
    {
        public static int Bound, Disposed;
        public override void OnBind() { Bound++; }
        public override void OnDispose() { Disposed++; }
    }

    public class EUIContractEditTests
    {
        private string _path;
        private GameObject _root;
        [SetUp]
        public void SetUp()
        {
            _path = "Assets/EUIContractTest_" + Guid.NewGuid().ToString("N") + ".prefab";
            _root = new GameObject("ContractItem", typeof(RectTransform), typeof(EUIBinding));
            EUIBindingEditorUtility.SetPageInfo(_root.GetComponent<EUIBinding>(), "", "Tests", typeof(EUIContractProbeLogic).FullName, false, PageType.MainPage);
            var button = new GameObject("m_Btn_Action", typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(_root.transform, false);
            EUIBindingEditorUtility.SetBindings(_root.GetComponent<EUIBinding>(), new[] {
                new EUIBindingEntrySnapshot { Name = "Btn_Action", GameObjectPath = "m_Btn_Action", WidgetType = EUIBinding.WidgetTypes.Button, IsFramework = true }
            });
        }
        [TearDown]
        public void TearDown() { UnityEngine.Object.DestroyImmediate(_root); AssetDatabase.DeleteAsset(_path); }
        private EUIContract SaveExport() { PrefabUtility.SaveAsPrefabAsset(_root, _path); return EUIContractService.Export(_path); }

        [Test]
        public void Export_IsReadOnlyStable_AndPreservesFrameworkMetadata()
        {
            var first = SaveExport(); var bytes = File.ReadAllBytes(_path);
            var second = EUIContractService.Export(_path);
            first.exportedUtc = second.exportedUtc;
            Assert.AreEqual(JsonUtility.ToJson(first), JsonUtility.ToJson(second));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(_path));
            Assert.IsTrue(first.bindings.Single().isFramework);
            Assert.IsNotEmpty(first.bindings.Single().objectId);
            var binding = _root.GetComponent<EUIBinding>();
            var snapshot = EUIBindingEditorUtility.GetBindingSnapshot(binding);
            EUIBindingEditorUtility.ApplyBindingSnapshot(binding, snapshot);
            Assert.IsTrue(binding.Bindings.Single().IsFramework);
            Assert.AreEqual(snapshot.CodePathMode, binding.PathMode);
        }
        [Test]
        public void Diff_ColorIsAppearance_ButRemovalAndTypeChangesBlock()
        {
            var before = SaveExport();
            _root.GetComponentInChildren<Image>().color = Color.magenta;
            var appearance = EUIContractService.Compare(before, SaveExport());
            Assert.IsTrue(appearance.Any(x => x.category == "appearance"));
            Assert.IsFalse(appearance.Any(x => x.blocksApply), string.Join("\n", appearance.Select(x => x.category + ":" + x.target)));
            var binding = _root.GetComponent<EUIBinding>();
            var snapshot = EUIBindingEditorUtility.GetBindingSnapshot(binding);
            snapshot.Entries[0].WidgetType = EUIBinding.WidgetTypes.Image;
            EUIBindingEditorUtility.ApplyBindingSnapshot(binding, snapshot);
            Assert.IsTrue(EUIContractService.Compare(before, SaveExport()).Any(x => x.category == "binding-type" && x.blocksApply));
            EUIBindingEditorUtility.SetBindings(binding, Array.Empty<EUIBindingEntrySnapshot>());
            Assert.IsTrue(EUIContractService.Compare(before, SaveExport()).Any(x => x.category == "binding-remove" && x.blocksApply));
        }
        [Test]
        public void Export_RejectsMissingReferenceAndDuplicateSemanticIds()
        {
            var contract = SaveExport();
            var owner = contract.bindings[0].ownerId;
            var notes = new[] { new EUIContractAnnotation { ownerId = owner, bindingName = "Btn_Action", semanticId = "same" }, new EUIContractAnnotation { ownerId = owner, bindingName = "missing", semanticId = "same" } };
            Assert.IsTrue(EUIContractService.Export(_path, notes).blockers.Any(x => x.Contains("重复语义")));
            UnityEngine.Object.DestroyImmediate(_root.GetComponentInChildren<Button>().gameObject);
            Assert.IsTrue(SaveExport().blockers.Any(x => x.Contains("空引用")));
        }
        [Test]
        public void DynamicOwnership_RejectsSecondFactoryBeforeOnBind_AndReleasesOnDispose()
        {
            EUIContractProbeLogic.Bound = EUIContractProbeLogic.Disposed = 0;
            Assert.IsTrue(EUIItemFactory.TryCreate(_root, out var item, out var error), error);
            try
            {
                Assert.IsFalse(EUIItemFactory.TryCreate(_root, out _, out error));
                Assert.AreEqual(1, EUIContractProbeLogic.Bound);
                item.Show(); item.Hide(); item.Show(); item.ResetForPool();
                Assert.AreEqual(1, EUIContractProbeLogic.Bound);
            }
            finally { item.Dispose(); item.Dispose(); }
            Assert.AreEqual(1, EUIContractProbeLogic.Disposed);
            Assert.IsTrue(EUIItemFactory.TryCreate(_root, out var next, out error), error);
            next.Dispose();
            Assert.AreEqual(2, EUIContractProbeLogic.Bound);
            Assert.AreEqual(2, EUIContractProbeLogic.Disposed);
        }
        [Test]
        public void ParentOwnership_BlocksFactoryOnNestedLogic_AndReleasesWithParent()
        {
            var child = new GameObject("ChildItem", typeof(RectTransform), typeof(EUIBinding));
            child.transform.SetParent(_root.transform, false);
            EUIBindingEditorUtility.SetPageInfo(child.GetComponent<EUIBinding>(), "", "Tests", typeof(EUIContractProbeLogic).FullName, false, PageType.MainPage);
            EUIBindingEditorUtility.SetBindings(_root.GetComponent<EUIBinding>(), new[] { new EUIBindingEntrySnapshot { Name = "Child", GameObjectPath = "ChildItem", WidgetType = EUIBinding.WidgetTypes.UILogic, ClassName = typeof(EUIContractProbeLogic).FullName } });
            EUIContractProbeLogic.Bound = EUIContractProbeLogic.Disposed = 0;
            Assert.IsTrue(EUIItemFactory.TryCreate(_root, out var parent, out var error), error);
            try
            {
                Assert.AreEqual(2, EUIContractProbeLogic.Bound);
                Assert.IsFalse(EUIItemFactory.TryCreate(child, out _, out _));
                Assert.AreEqual(2, EUIContractProbeLogic.Bound);
            }
            finally { parent.Dispose(); }
            Assert.AreEqual(2, EUIContractProbeLogic.Disposed);
            Assert.IsFalse(EUIInstanceOwnership.IsOwned(child));
        }
        [Test]
        public void ButtonLabelSlot_IsNotCollectedAgain_AndEnableStateDoesNotChangeInteractable()
        {
            var button = new GameObject("m_Btn_Slotted", typeof(RectTransform), typeof(EUIButtonEx));
            button.transform.SetParent(_root.transform, false);
            var label = new GameObject("m_Txt_Internal", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            label.transform.SetParent(button.transform, false);
            var component = button.GetComponent<EUIButtonEx>();
            component.Label = label.GetComponent<TMPro.TMP_Text>();
            component.interactable = false; component.EnableState = true;
            Assert.IsFalse(component.interactable);
            EUIBindingEditorUtility.CollectBindings(_root.GetComponent<EUIBinding>());
            Assert.IsFalse(_root.GetComponent<EUIBinding>().Bindings.Any(x => x.GameObject == label));
            Assert.IsTrue(_root.GetComponent<EUIBinding>().Bindings.Any(x => x.GameObject == button));
        }

        [Test]
        public void PageCreation_DefaultUsesPage_AndRejectsOldPanelSuffix()
        {
            Assert.AreEqual("NewPage", new EUICreationRequest().PrefabName);
            Assert.IsFalse(EUICreationService.TryBuildPlan(new EUICreationRequest { PrefabName = "OldPanel" }, out _, out var result));
            StringAssert.Contains("Page 后缀", result.Error);
        }
    }
}
