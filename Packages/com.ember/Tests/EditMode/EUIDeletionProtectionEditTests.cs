// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.IO;
using System.Linq;
using Ember.UIExtension;
using Ember.UIExtension.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Ember.UI.Tests
{
    public class EUIDeletionProtectionEditTests
    {
        private string _folder;
        private string _prefabPath;

        [SetUp]
        public void SetUp()
        {
            var catalog = EUIPrefabCatalogService.Scan();
            Assert.IsTrue(catalog.IsConfigured, catalog.Error);
            var folderName = "DeletionProtectionTests" + Guid.NewGuid().ToString("N");
            _folder = catalog.UIResourceRoot + "/" + folderName;
            AssetDatabase.CreateFolder(catalog.UIResourceRoot, folderName);
            _prefabPath = _folder + "/Probe.prefab";
            var root = new GameObject("Probe", typeof(RectTransform));
            try
            {
                var binding = root.AddComponent<EUIBinding>();
                using (var so = new SerializedObject(binding))
                {
                    so.FindProperty("noCodeGen").boolValue = true;
                    so.FindProperty("className").stringValue = "DeletionProtectionProbe";
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                Assert.IsNotNull(PrefabUtility.SaveAsPrefabAsset(root, _prefabPath));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_folder)) AssetDatabase.DeleteAsset(_folder);
        }

        [Test]
        public void ProtectedPrefab_PreviewRejectsEvenWhenCatalogFlagIsCleared()
        {
            ProtectPrefab();
            var catalog = EUIPrefabCatalogService.Scan();
            var entry = catalog.Entries.Single(item => item.PrefabPath == _prefabPath);
            Assert.IsTrue(entry.IsDeletionProtected);
            entry.IsDeletionProtected = false;

            var plan = EUIPrefabMaintenanceService.BuildDeletePlan(catalog, entry);

            Assert.IsFalse(plan.CanExecute);
            Assert.IsEmpty(plan.AssetPaths);
            StringAssert.Contains("禁止删除", string.Join("\n", plan.Errors));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Execution_RechecksProtectionForStaleOrForgedPlans(bool forgePlan)
        {
            var catalog = EUIPrefabCatalogService.Scan();
            var entry = catalog.Entries.Single(item => item.PrefabPath == _prefabPath);
            var plan = forgePlan ? new EUIDeletePlan { Entry = entry }
                : EUIPrefabMaintenanceService.BuildDeletePlan(catalog, entry);
            if (forgePlan) plan.AssetPaths.Add(_prefabPath);
            Assert.IsTrue(plan.CanExecute);
            ProtectPrefab();
            var fullPath = EUIPrefabCatalogService.ToFullPath(_prefabPath);
            var before = File.ReadAllBytes(fullPath);
            var metaBefore = File.ReadAllBytes(fullPath + ".meta");

            var result = EUIPrefabMaintenanceService.ExecuteDelete(plan, catalog);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(0, result.ChangedCount);
            StringAssert.Contains("禁止删除", result.Message);
            CollectionAssert.AreEqual(before, File.ReadAllBytes(fullPath));
            CollectionAssert.AreEqual(metaBefore, File.ReadAllBytes(fullPath + ".meta"));
        }

        [Test]
        public void UnprotectedPrefab_CanStillBeDeleted()
        {
            var catalog = EUIPrefabCatalogService.Scan();
            var entry = catalog.Entries.Single(item => item.PrefabPath == _prefabPath);
            var plan = EUIPrefabMaintenanceService.BuildDeletePlan(catalog, entry);
            Assert.IsTrue(plan.CanExecute, plan.BuildSummary());

            var result = EUIPrefabMaintenanceService.ExecuteDelete(plan, catalog);

            Assert.IsTrue(result.Success, result.Message);
            Assert.AreEqual(1, result.ChangedCount);
            Assert.IsFalse(File.Exists(EUIPrefabCatalogService.ToFullPath(_prefabPath)));
        }

        private void ProtectPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
            using (var so = new SerializedObject(prefab.GetComponent<EUIBinding>()))
            {
                so.FindProperty("deletionProtected").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SavePrefabAsset(prefab);
        }
    }
}
