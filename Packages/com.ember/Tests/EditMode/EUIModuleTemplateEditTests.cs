// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.IO;

using Ember.UIExtension.Editor;

using NUnit.Framework;

using UnityEditor;

namespace Ember.UI.Tests
{
    /// <summary>UI 模块空目录模板的计划与安全边界测试。</summary>
    public class EUIModuleTemplateEditTests
    {
        private string _testRoot;
        private string _uiRoot;
        private string _modulesRoot;
        private string _templateRoot;

        [SetUp]
        public void SetUp()
        {
            _testRoot = $"Assets/__EmberUIModuleTemplateTests_{Guid.NewGuid():N}";
            _uiRoot = $"{_testRoot}/UI";
            _modulesRoot = $"{_uiRoot}/Module";
            _templateRoot = $"{_modulesRoot}/{EUIModuleTemplateService.TemplateDirectoryName}";
            EnsureFolder(_templateRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_testRoot)) AssetDatabase.DeleteAsset(_testRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [Test]
        public void Initialization_ShouldMirrorNestedDirectoryNamesWithIndependentGuids()
        {
            EnsureFolder($"{_templateRoot}/Animator");
            EnsureFolder($"{_templateRoot}/Atlas/Icons");
            var snapshot = EUIModuleTemplateService.Scan(_uiRoot);
            var module = $"{_modulesRoot}/NewModule";

            var plan = EUIModuleTemplateService.BuildInitializationPlan(snapshot, module);
            var result = EUIModuleTemplateService.ExecuteInitialization(plan);

            Assert.IsTrue(plan.CanExecute, string.Join("\n", plan.Errors));
            Assert.IsTrue(result.Success, result.Message);
            Assert.IsTrue(AssetDatabase.IsValidFolder($"{module}/Animator"));
            Assert.IsTrue(AssetDatabase.IsValidFolder($"{module}/Atlas/Icons"));
            Assert.AreNotEqual(
                AssetDatabase.AssetPathToGUID($"{_templateRoot}/Animator"),
                AssetDatabase.AssetPathToGUID($"{module}/Animator"),
                "模块目录必须由 AssetDatabase 新建，不能复制模板 .meta GUID。");
        }

        [Test]
        public void Sync_ShouldOnlyCreateMissingDirectoriesAndExcludeTemplateItself()
        {
            EnsureFolder($"{_templateRoot}/Prefabs");
            EnsureFolder($"{_templateRoot}/Shader");
            EnsureFolder($"{_modulesRoot}/ModuleA/Prefabs");
            EnsureFolder($"{_modulesRoot}/ModuleB");
            var snapshot = EUIModuleTemplateService.Scan(_uiRoot);

            var plan = EUIModuleTemplateService.BuildSyncPlan(snapshot);
            var result = EUIModuleTemplateService.ExecuteSync(plan);

            Assert.IsTrue(plan.CanExecute, string.Join("\n", plan.Errors));
            Assert.AreEqual(3, plan.DirectoriesToCreate.Count);
            Assert.IsFalse(snapshot.ModuleDirectories.Contains(_templateRoot));
            Assert.IsTrue(result.Success, result.Message);
            Assert.IsTrue(AssetDatabase.IsValidFolder($"{_modulesRoot}/ModuleA/Shader"));
            Assert.IsTrue(AssetDatabase.IsValidFolder($"{_modulesRoot}/ModuleB/Prefabs"));
            Assert.IsTrue(AssetDatabase.IsValidFolder($"{_modulesRoot}/ModuleB/Shader"));
        }

        [Test]
        public void Rename_ShouldMoveExistingModuleFolderAndCreateMissingOne()
        {
            EnsureFolder($"{_templateRoot}/Atlas/Icons");
            EnsureFolder($"{_modulesRoot}/ModuleA/Atlas/Content");
            EnsureFolder($"{_modulesRoot}/ModuleB");
            var templateGuid = AssetDatabase.AssetPathToGUID($"{_templateRoot}/Atlas");
            var moduleGuid = AssetDatabase.AssetPathToGUID($"{_modulesRoot}/ModuleA/Atlas");
            var snapshot = EUIModuleTemplateService.Scan(_uiRoot);

            var plan = EUIModuleTemplateService.BuildRenamePlan(
                snapshot, $"{_templateRoot}/Atlas", "Art");
            var result = EUIModuleTemplateService.ExecuteRename(plan);

            Assert.IsTrue(plan.CanExecute, string.Join("\n", plan.Errors));
            Assert.IsTrue(result.Success, result.Message);
            Assert.IsFalse(AssetDatabase.IsValidFolder($"{_templateRoot}/Atlas"));
            Assert.AreEqual(templateGuid, AssetDatabase.AssetPathToGUID($"{_templateRoot}/Art"));
            Assert.AreEqual(moduleGuid,
                AssetDatabase.AssetPathToGUID($"{_modulesRoot}/ModuleA/Art"));
            Assert.IsTrue(AssetDatabase.IsValidFolder(
                $"{_modulesRoot}/ModuleA/Art/Content"));
            Assert.IsTrue(AssetDatabase.IsValidFolder($"{_modulesRoot}/ModuleB/Art"));
        }

        [Test]
        public void Rename_ShouldRejectOldAndNewFolderCollision()
        {
            EnsureFolder($"{_templateRoot}/Material");
            EnsureFolder($"{_modulesRoot}/ModuleA/Material");
            EnsureFolder($"{_modulesRoot}/ModuleA/Surface");

            var plan = EUIModuleTemplateService.BuildRenamePlan(
                EUIModuleTemplateService.Scan(_uiRoot),
                $"{_templateRoot}/Material", "Surface");

            Assert.IsFalse(plan.CanExecute);
            StringAssert.Contains("拒绝自动合并", string.Join("\n", plan.Errors));
        }

        [Test]
        public void DeletePlan_ShouldProtectPrefabsAndRejectDirectoriesContainingAssets()
        {
            EnsureFolder($"{_templateRoot}/Prefabs");
            EnsureFolder($"{_templateRoot}/Material");
            var assetPath = $"{_templateRoot}/Material/Keep.txt";
            File.WriteAllText(EUIPrefabCatalogService.ToFullPath(assetPath), "keep");
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var snapshot = EUIModuleTemplateService.Scan(_uiRoot);

            var prefabsPlan = EUIModuleTemplateService.BuildDeletePlan(
                snapshot, $"{_templateRoot}/Prefabs");
            var materialPlan = EUIModuleTemplateService.BuildDeletePlan(
                snapshot, $"{_templateRoot}/Material");

            Assert.IsFalse(prefabsPlan.CanExecute);
            StringAssert.Contains("禁止删除", string.Join("\n", prefabsPlan.Errors));
            Assert.IsFalse(materialPlan.CanExecute);
            CollectionAssert.Contains(materialPlan.AssetFiles, assetPath);
        }

        private static void EnsureFolder(string assetPath)
        {
            var segments = assetPath.Replace('\\', '/').Split('/');
            Assert.AreEqual("Assets", segments[0]);
            var current = "Assets";
            for (var i = 1; i < segments.Length; i++)
            {
                var next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    var guid = AssetDatabase.CreateFolder(current, segments[i]);
                    Assert.IsNotEmpty(guid, $"创建测试目录失败：{next}");
                }
                current = next;
            }
        }
    }
}
