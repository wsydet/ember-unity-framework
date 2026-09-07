// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;

using Ember.Core.Editor;

using NUnit.Framework;

namespace Ember.UI.Tests
{
    /// <summary>模板多目标事务、三方同步应用与故障回滚测试。</summary>
    public class EmberTemplateTransactionEditTests
    {
        #region 内部参数

        private const string ASSET_GUID = "11111111111111111111111111111111";
        private const string SCENE_PATH = "Game/Scenes/FrameworkScene.unity";

        private string _testRoot;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        [SetUp]
        public void SetUp()
        {
            _testRoot = Path.Combine(
                Path.GetTempPath(),
                "EmberTemplateTransactionTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testRoot);
            EmberTemplateSceneMergePlanner.ClearPreviewCache();
        }

        [TearDown]
        public void TearDown()
        {
            EmberTemplateSceneMergePlanner.ClearPreviewCache();
            if (!string.IsNullOrEmpty(_testRoot) && Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, true);
        }

        [Test]
        public void CommitPreparedTargets_AllTargetsShouldReplaceTogether()
        {
            var formalRoot = Path.Combine(_testRoot, "Formal");
            var stageRoot = Path.Combine(_testRoot, "Stage");
            var formalAssets = Path.Combine(formalRoot, "Assets");
            var formalSnapshot = Path.Combine(formalRoot, "ParentSnapshot", "Assets");
            var formalMetadata = Path.Combine(formalRoot, "template.json");
            var stagedAssets = Path.Combine(stageRoot, "Assets");
            var stagedSnapshot = Path.Combine(stageRoot, "ParentSnapshot", "Assets");
            var stagedMetadata = Path.Combine(stageRoot, "template.json");
            WriteFile(formalAssets, "value.txt", "old-assets");
            WriteFile(formalSnapshot, "value.txt", "old-snapshot");
            WriteFile(formalMetadata, "old-metadata");
            WriteFile(stagedAssets, "value.txt", "new-assets");
            WriteFile(stagedSnapshot, "value.txt", "new-snapshot");
            WriteFile(stagedMetadata, "new-metadata");

            EmberTemplateTransaction.CommitPreparedTargets(
                new[]
                {
                    new TemplateTransactionTarget(stagedAssets, formalAssets),
                    new TemplateTransactionTarget(stagedSnapshot, formalSnapshot),
                    new TemplateTransactionTarget(stagedMetadata, formalMetadata)
                });

            Assert.AreEqual("new-assets", File.ReadAllText(Path.Combine(formalAssets, "value.txt")));
            Assert.AreEqual("new-snapshot", File.ReadAllText(Path.Combine(formalSnapshot, "value.txt")));
            Assert.AreEqual("new-metadata", File.ReadAllText(formalMetadata));
            AssertNoBackups(formalAssets, formalSnapshot, formalMetadata);
        }

        [Test]
        public void CopyDirectory_FolderMetadataShouldMaterializeVirtualEmptyDirectory()
        {
            var source = Path.Combine(_testRoot, "VirtualFolderSource");
            var destination = Path.Combine(_testRoot, "VirtualFolderDestination");
            Directory.CreateDirectory(source);
            WriteFile(
                Path.Combine(source, "Empty.meta"),
                "fileFormatVersion: 2\nguid: virtual-folder-guid\nfolderAsset: yes\n");

            EmberTemplateTransaction.CopyDirectory(source, destination);

            Assert.IsTrue(File.Exists(Path.Combine(destination, "Empty.meta")));
            Assert.IsTrue(Directory.Exists(Path.Combine(destination, "Empty")));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ParentSync_FaultAtAnyCommitStepShouldRestoreAllFormalTargets(int faultIndex)
        {
            var setup = CreateSyncSetup("old", "parent", "old");
            var metadataBefore = File.ReadAllText(setup.ChildMetadataPath);
            var childHashBefore = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(
                setup.ChildAssetsPath);
            var snapshotHashBefore = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(
                setup.SnapshotAssetsPath);

            Assert.Throws<InjectedTransactionException>(() =>
                EmberTemplateTransaction.ApplyParentSync(
                    CreateRequest(
                        setup,
                        null,
                        2,
                        index =>
                        {
                            if (index == faultIndex) throw new InjectedTransactionException();
                        })));

            Assert.AreEqual(
                childHashBefore,
                EmberTemplateInheritanceEngine.ComputeTemplateContentHash(setup.ChildAssetsPath));
            Assert.AreEqual(
                snapshotHashBefore,
                EmberTemplateInheritanceEngine.ComputeTemplateContentHash(setup.SnapshotAssetsPath));
            Assert.AreEqual(metadataBefore, File.ReadAllText(setup.ChildMetadataPath));
            AssertNoBackups(
                setup.ChildAssetsPath,
                setup.SnapshotAssetsPath,
                setup.ChildMetadataPath);
        }

        [Test]
        public void ParentSync_SafeParentChangeShouldUpdateAssetsSnapshotAndMetadata()
        {
            var setup = CreateSyncSetup("old", "parent", "old");

            var updated = EmberTemplateTransaction.ApplyParentSync(
                CreateRequest(setup, null, 2));

            Assert.AreEqual("parent", ReadAsset(setup.ChildAssetsPath));
            Assert.AreEqual("parent", ReadAsset(setup.SnapshotAssetsPath));
            Assert.AreEqual("0.1.1", updated.version);
            Assert.AreEqual("0.2.0", updated.parentVersion);
            Assert.AreEqual(setup.Parent.contentHash, updated.parentContentHash);
            Assert.AreEqual("0.11.0", updated.frameworkVersion);
            Assert.AreEqual(updated.contentHash, updated.versionedContentHash);
            StringAssert.Contains("\"version\": \"0.1.1\"", File.ReadAllText(setup.ChildMetadataPath));
        }

        [Test]
        public void ParentSync_KeepChildConflictShouldAdvanceSnapshotWithoutVersionBump()
        {
            var setup = CreateSyncSetup("old", "parent", "child");
            Assert.IsTrue(setup.Plan.HasConflicts);
            var resolutions = new Dictionary<string, TemplateConflictChoice>(StringComparer.Ordinal)
            {
                ["Example.prefab"] = TemplateConflictChoice.KeepChild
            };

            var updated = EmberTemplateTransaction.ApplyParentSync(
                CreateRequest(setup, resolutions, null));

            Assert.AreEqual("child", ReadAsset(setup.ChildAssetsPath));
            Assert.AreEqual("parent", ReadAsset(setup.SnapshotAssetsPath));
            Assert.AreEqual("0.1.0", updated.version);
            Assert.AreEqual(setup.Child.contentHash, updated.contentHash);
            Assert.AreEqual("0.2.0", updated.parentVersion);
            Assert.AreEqual(setup.Parent.contentHash, updated.parentContentHash);
        }

        [Test]
        public void ParentSync_UnresolvedConflictShouldWriteNothing()
        {
            var setup = CreateSyncSetup("old", "parent", "child");
            var metadataBefore = File.ReadAllText(setup.ChildMetadataPath);
            var childHashBefore = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(
                setup.ChildAssetsPath);
            var snapshotHashBefore = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(
                setup.SnapshotAssetsPath);

            var error = Assert.Throws<InvalidOperationException>(() =>
                EmberTemplateTransaction.ApplyParentSync(
                    CreateRequest(setup, null, 2)));

            StringAssert.Contains("未解决冲突", error.Message);
            Assert.AreEqual(
                childHashBefore,
                EmberTemplateInheritanceEngine.ComputeTemplateContentHash(setup.ChildAssetsPath));
            Assert.AreEqual(
                snapshotHashBefore,
                EmberTemplateInheritanceEngine.ComputeTemplateContentHash(setup.SnapshotAssetsPath));
            Assert.AreEqual(metadataBefore, File.ReadAllText(setup.ChildMetadataPath));
            AssertNoBackups(
                setup.ChildAssetsPath,
                setup.SnapshotAssetsPath,
                setup.ChildMetadataPath);
        }

        [Test]
        public void ParentSync_ContentChangeWithoutBumpShouldWriteNothing()
        {
            var setup = CreateSyncSetup("old", "parent", "old");
            var metadataBefore = File.ReadAllText(setup.ChildMetadataPath);

            var error = Assert.Throws<InvalidOperationException>(() =>
                EmberTemplateTransaction.ApplyParentSync(
                    CreateRequest(setup, null, null)));

            StringAssert.Contains("必须同时选择", error.Message);
            Assert.AreEqual("old", ReadAsset(setup.ChildAssetsPath));
            Assert.AreEqual("old", ReadAsset(setup.SnapshotAssetsPath));
            Assert.AreEqual(metadataBefore, File.ReadAllText(setup.ChildMetadataPath));
        }

        [Test]
        public void ParentSync_SemanticSceneShouldRerunInStageAndPreserveChildMeta()
        {
            var mergedScene = CreateSceneYaml("MergedGameBootAndCamera");
            var setup = CreateSceneSyncSetup(mergedScene, out var previewRunner);
            var childMetaPath = CombineRelativePath(
                setup.ChildAssetsPath,
                SCENE_PATH + ".meta");
            var childMetaBefore = File.ReadAllText(childMetaPath);
            var applyRunner = CreateSuccessfulSceneRunner(mergedScene);
            var applyAdapter = CreateSceneMergeAdapter(applyRunner, "SharedTool");

            var updated = EmberTemplateTransaction.ApplyParentSync(
                CreateRequest(setup, null, 2, null, applyAdapter));

            Assert.AreEqual(1, previewRunner.CallCount, "dry-run 应只执行一次。 ");
            Assert.AreEqual(1, applyRunner.CallCount, "事务 stage 必须重新执行语义合并。 ");
            Assert.AreEqual(
                mergedScene,
                File.ReadAllText(CombineRelativePath(setup.ChildAssetsPath, SCENE_PATH)));
            Assert.AreEqual(childMetaBefore, File.ReadAllText(childMetaPath));
            Assert.AreEqual(
                CreateSceneYaml("ParentGameBoot"),
                File.ReadAllText(CombineRelativePath(setup.SnapshotAssetsPath, SCENE_PATH)));
            Assert.AreEqual(
                EmberTemplateInheritanceEngine.ComputeTemplateContentHash(setup.ChildAssetsPath),
                updated.contentHash);
            Assert.AreEqual(updated.contentHash, updated.versionedContentHash);
            Assert.AreEqual("0.1.1", updated.version);
        }

        [Test]
        public void ParentSync_SemanticResultHashMismatchShouldWriteNothing()
        {
            var setup = CreateSceneSyncSetup(
                CreateSceneYaml("PreviewMerged"),
                out _);
            var metadataBefore = File.ReadAllText(setup.ChildMetadataPath);
            var childHashBefore = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(
                setup.ChildAssetsPath);
            var snapshotHashBefore = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(
                setup.SnapshotAssetsPath);
            var applyRunner = CreateSuccessfulSceneRunner(CreateSceneYaml("DifferentStageMerged"));
            var applyAdapter = CreateSceneMergeAdapter(applyRunner, "SharedTool");

            var error = Assert.Throws<InvalidOperationException>(() =>
                EmberTemplateTransaction.ApplyParentSync(
                    CreateRequest(setup, null, 2, null, applyAdapter)));

            StringAssert.Contains("结果 hash", error.Message);
            Assert.AreEqual(1, applyRunner.CallCount);
            AssertFormalTargetsUnchanged(
                setup,
                metadataBefore,
                childHashBefore,
                snapshotHashBefore);
        }

        [Test]
        public void ParentSync_SemanticInputChangedAfterPreviewShouldWriteNothing()
        {
            var mergedScene = CreateSceneYaml("Merged");
            var setup = CreateSceneSyncSetup(mergedScene, out _);
            var metadataBefore = File.ReadAllText(setup.ChildMetadataPath);
            var snapshotHashBefore = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(
                setup.SnapshotAssetsPath);
            WriteFile(
                CombineRelativePath(setup.ChildAssetsPath, SCENE_PATH),
                CreateSceneYaml("ChildChangedAfterPreview"));
            var changedChildHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(
                setup.ChildAssetsPath);
            var applyRunner = CreateSuccessfulSceneRunner(mergedScene);
            var applyAdapter = CreateSceneMergeAdapter(applyRunner, "SharedTool");

            var error = Assert.Throws<InvalidOperationException>(() =>
                EmberTemplateTransaction.ApplyParentSync(
                    CreateRequest(setup, null, 2, null, applyAdapter)));

            StringAssert.Contains("内容已变化", error.Message);
            Assert.AreEqual(0, applyRunner.CallCount, "输入 hash 失败后不得启动外部进程。 ");
            AssertFormalTargetsUnchanged(
                setup,
                metadataBefore,
                changedChildHash,
                snapshotHashBefore);
        }

        [Test]
        public void ParentSync_SemanticContentChangeWithoutBumpShouldWriteNothing()
        {
            var mergedScene = CreateSceneYaml("Merged");
            var setup = CreateSceneSyncSetup(mergedScene, out _);
            var metadataBefore = File.ReadAllText(setup.ChildMetadataPath);
            var childHashBefore = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(
                setup.ChildAssetsPath);
            var snapshotHashBefore = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(
                setup.SnapshotAssetsPath);
            var applyRunner = CreateSuccessfulSceneRunner(mergedScene);
            var applyAdapter = CreateSceneMergeAdapter(applyRunner, "SharedTool");

            var error = Assert.Throws<InvalidOperationException>(() =>
                EmberTemplateTransaction.ApplyParentSync(
                    CreateRequest(setup, null, null, null, applyAdapter)));

            StringAssert.Contains("必须同时选择", error.Message);
            Assert.AreEqual(1, applyRunner.CallCount);
            AssertFormalTargetsUnchanged(
                setup,
                metadataBefore,
                childHashBefore,
                snapshotHashBefore);
        }

        [Test]
        public void ParentSync_SemanticToolFingerprintMismatchShouldWriteNothing()
        {
            var mergedScene = CreateSceneYaml("Merged");
            var setup = CreateSceneSyncSetup(mergedScene, out _);
            var metadataBefore = File.ReadAllText(setup.ChildMetadataPath);
            var childHashBefore = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(
                setup.ChildAssetsPath);
            var snapshotHashBefore = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(
                setup.SnapshotAssetsPath);
            var applyRunner = CreateSuccessfulSceneRunner(mergedScene);
            var applyAdapter = CreateSceneMergeAdapter(applyRunner, "ChangedTool");
            WriteFile(
                Path.Combine(
                    _testRoot,
                    "Fake Unity",
                    "ChangedTool",
                    "Data",
                    "Tools",
                    "mergerules.txt"),
                "changed fake rules");

            var error = Assert.Throws<InvalidOperationException>(() =>
                EmberTemplateTransaction.ApplyParentSync(
                    CreateRequest(setup, null, 2, null, applyAdapter)));

            StringAssert.Contains("工具或规则已变化", error.Message);
            Assert.AreEqual(1, applyRunner.CallCount);
            AssertFormalTargetsUnchanged(
                setup,
                metadataBefore,
                childHashBefore,
                snapshotHashBefore);
        }

        [Test]
        public void ParentSync_AcceptParentDirectoryTypeShouldKeepDirectoryAndItsMetadata()
        {
            var parentRoot = Path.Combine(_testRoot, "Templates", "base");
            var childRoot = Path.Combine(_testRoot, "Templates", "child");
            var parentAssets = Path.Combine(parentRoot, "Assets");
            var childAssets = Path.Combine(childRoot, "Assets");
            var snapshotAssets = Path.Combine(childRoot, "ParentSnapshot~", "Assets");
            WriteAsset(childAssets, "old");
            WriteAsset(snapshotAssets, "old");
            WriteFile(
                parentAssets,
                "Example.prefab.meta",
                $"fileFormatVersion: 2\nguid: {ASSET_GUID}\nfolderAsset: yes\n");
            WriteFile(parentAssets, "Example.prefab/Inner.asset", "inner");
            WriteFile(
                parentAssets,
                "Example.prefab/Inner.asset.meta",
                "fileFormatVersion: 2\nguid: 22222222222222222222222222222222\n");

            var parentHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(parentAssets);
            var childHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(childAssets);
            var snapshotHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(snapshotAssets);
            var parent = CreateTemplate("base", null, "0.2.0", "0.11.0", parentHash);
            var child = CreateTemplate("child", "base", "0.1.0", "0.10.0", childHash);
            child.parentVersion = "0.1.0";
            child.parentContentHash = snapshotHash;
            var childMetadataPath = Path.Combine(childRoot, "template.json");
            EmberTemplateTransaction.WriteTemplateJson(childMetadataPath, child);
            var plan = EmberTemplateInheritanceEngine.BuildParentSyncPlan(
                child,
                new[] { parent, child },
                parentAssets,
                snapshotAssets,
                childAssets);
            TemplateChange typeConflict = null;
            foreach (var change in plan.Changes)
            {
                if (change.UnitKind != TemplateChangeUnitKind.PathTypeConflict) continue;
                typeConflict = change;
                break;
            }
            Assert.IsNotNull(typeConflict);
            var resolutions = new Dictionary<string, TemplateConflictChoice>(StringComparer.Ordinal)
            {
                [typeConflict.UnitPath] = TemplateConflictChoice.AcceptParent
            };
            var setup = new SyncSetup(
                parent,
                child,
                plan,
                parentAssets,
                childRoot,
                childAssets,
                snapshotAssets,
                childMetadataPath);

            EmberTemplateTransaction.ApplyParentSync(
                CreateRequest(setup, resolutions, 2));

            Assert.IsTrue(Directory.Exists(Path.Combine(childAssets, "Example.prefab")));
            Assert.IsTrue(File.Exists(Path.Combine(childAssets, "Example.prefab.meta")));
            Assert.AreEqual(
                "inner",
                File.ReadAllText(Path.Combine(childAssets, "Example.prefab", "Inner.asset")));
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private SyncSetup CreateSyncSetup(
            string oldContent,
            string parentContent,
            string childContent)
        {
            var parentRoot = Path.Combine(_testRoot, "Templates", "base");
            var childRoot = Path.Combine(_testRoot, "Templates", "child");
            var parentAssets = Path.Combine(parentRoot, "Assets");
            var childAssets = Path.Combine(childRoot, "Assets");
            var snapshotAssets = Path.Combine(childRoot, "ParentSnapshot~", "Assets");
            WriteAsset(parentAssets, parentContent);
            WriteAsset(childAssets, childContent);
            WriteAsset(snapshotAssets, oldContent);

            var parentHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(parentAssets);
            var childHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(childAssets);
            var snapshotHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(snapshotAssets);
            var parent = CreateTemplate("base", null, "0.2.0", "0.11.0", parentHash);
            var child = CreateTemplate("child", "base", "0.1.0", "0.10.0", childHash);
            child.parentVersion = "0.1.0";
            child.parentContentHash = snapshotHash;
            var childMetadataPath = Path.Combine(childRoot, "template.json");
            EmberTemplateTransaction.WriteTemplateJson(childMetadataPath, child);

            var plan = EmberTemplateInheritanceEngine.BuildParentSyncPlan(
                child,
                new[] { parent, child },
                parentAssets,
                snapshotAssets,
                childAssets);
            Assert.AreEqual(TemplateSyncStatus.ParentChanged, plan.Status);
            Assert.IsTrue(plan.IsEligible);
            return new SyncSetup(
                parent,
                child,
                plan,
                parentAssets,
                childRoot,
                childAssets,
                snapshotAssets,
                childMetadataPath);
        }

        private SyncSetup CreateSceneSyncSetup(
            string previewMergedScene,
            out FakeSceneMergeRunner previewRunner)
        {
            var parentRoot = Path.Combine(_testRoot, "Templates", "base");
            var childRoot = Path.Combine(_testRoot, "Templates", "child");
            var parentAssets = Path.Combine(parentRoot, "Assets");
            var childAssets = Path.Combine(childRoot, "Assets");
            var snapshotAssets = Path.Combine(childRoot, "ParentSnapshot~", "Assets");
            WriteSceneAsset(parentAssets, CreateSceneYaml("ParentGameBoot"));
            WriteSceneAsset(childAssets, CreateSceneYaml("ChildCamera"));
            WriteSceneAsset(snapshotAssets, CreateSceneYaml("OldGameBootAndCamera"));

            var parentHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(parentAssets);
            var childHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(childAssets);
            var snapshotHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(snapshotAssets);
            var parent = CreateTemplate("base", null, "0.2.0", "0.11.0", parentHash);
            var child = CreateTemplate("child", "base", "0.1.0", "0.10.0", childHash);
            child.parentVersion = "0.1.0";
            child.parentContentHash = snapshotHash;
            var childMetadataPath = Path.Combine(childRoot, "template.json");
            EmberTemplateTransaction.WriteTemplateJson(childMetadataPath, child);

            var filePlan = EmberTemplateInheritanceEngine.BuildParentSyncPlan(
                child,
                new[] { parent, child },
                parentAssets,
                snapshotAssets,
                childAssets);
            Assert.IsTrue(filePlan.HasConflicts);
            previewRunner = CreateSuccessfulSceneRunner(previewMergedScene);
            var previewAdapter = CreateSceneMergeAdapter(previewRunner, "SharedTool");
            var semanticPlan = EmberTemplateSceneMergePlanner.EnhancePlan(
                filePlan,
                snapshotAssets,
                parentAssets,
                childAssets,
                previewAdapter);
            Assert.IsTrue(semanticPlan.HasSemanticMerges);
            Assert.IsFalse(semanticPlan.HasConflicts);
            return new SyncSetup(
                parent,
                child,
                semanticPlan,
                parentAssets,
                childRoot,
                childAssets,
                snapshotAssets,
                childMetadataPath);
        }

        private static ParentSyncTransactionRequest CreateRequest(
            SyncSetup setup,
            IReadOnlyDictionary<string, TemplateConflictChoice> resolutions,
            int? versionBump,
            Action<int> faultInjector = null,
            EmberUnityYamlMerge yamlMerge = null)
        {
            return new ParentSyncTransactionRequest(
                setup.Plan,
                setup.Child,
                setup.Parent,
                setup.ChildRoot,
                setup.ChildAssetsPath,
                setup.ParentAssetsPath,
                setup.SnapshotAssetsPath,
                resolutions,
                versionBump,
                faultInjector,
                yamlMerge);
        }

        private EmberUnityYamlMerge CreateSceneMergeAdapter(
            FakeSceneMergeRunner runner,
            string toolFolder)
        {
            var applicationContentsPath = Path.Combine(
                _testRoot,
                "Fake Unity",
                toolFolder,
                "Data");
            WriteFile(
                Path.Combine(applicationContentsPath, "Tools", "UnityYAMLMerge.exe"),
                "fake tool");
            WriteFile(
                Path.Combine(applicationContentsPath, "Tools", "mergerules.txt"),
                "fake rules");
            return new EmberUnityYamlMerge(
                runner,
                new FakeSceneMergeEnvironment(
                    applicationContentsPath,
                    Path.Combine(_testRoot, "Fake Project"),
                    true));
        }

        private static FakeSceneMergeRunner CreateSuccessfulSceneRunner(string mergedScene)
        {
            return new FakeSceneMergeRunner
            {
                OnRun = request =>
                {
                    WriteFile(request.Arguments[request.Arguments.Count - 1], mergedScene);
                    return new UnityYamlMergeProcessResult(
                        true,
                        0,
                        false,
                        "success",
                        string.Empty,
                        null);
                }
            };
        }

        private static TemplateInfo CreateTemplate(
            string id,
            string parentId,
            string version,
            string frameworkVersion,
            string contentHash)
        {
            return new TemplateInfo
            {
                schemaVersion = EmberTemplateInheritanceEngine.CurrentSchemaVersion,
                id = id,
                displayName = id,
                description = string.Empty,
                version = version,
                frameworkVersion = frameworkVersion,
                channel = "preview",
                order = 1,
                parentId = parentId ?? string.Empty,
                parentVersion = string.Empty,
                parentContentHash = string.Empty,
                contentHash = contentHash,
                versionedContentHash = contentHash
            };
        }

        private static void WriteAsset(string assetsPath, string content)
        {
            WriteFile(assetsPath, "Example.prefab", content);
            WriteFile(
                assetsPath,
                "Example.prefab.meta",
                $"fileFormatVersion: 2\nguid: {ASSET_GUID}\n");
        }

        private static void WriteSceneAsset(string assetsPath, string content)
        {
            WriteFile(assetsPath, SCENE_PATH, content);
            WriteFile(
                assetsPath,
                SCENE_PATH + ".meta",
                $"fileFormatVersion: 2\nguid: {ASSET_GUID}\n");
        }

        private static string CreateSceneYaml(string name)
        {
            return "%YAML 1.1\n"
                + "%TAG !u! tag:unity3d.com,2011:\n"
                + "--- !u!1 &1\n"
                + "GameObject:\n"
                + "  m_Name: " + name + "\n";
        }

        private static string ReadAsset(string assetsPath)
        {
            return File.ReadAllText(Path.Combine(assetsPath, "Example.prefab"));
        }

        private static string WriteFile(string root, string relativePath, string content)
        {
            return WriteFile(
                Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)),
                content);
        }

        private static string WriteFile(string path, string content)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, content);
            return path;
        }

        private static string CombineRelativePath(string root, string relativePath)
        {
            return Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void AssertFormalTargetsUnchanged(
            SyncSetup setup,
            string metadataBefore,
            string childHashBefore,
            string snapshotHashBefore)
        {
            Assert.AreEqual(
                childHashBefore,
                EmberTemplateInheritanceEngine.ComputeTemplateContentHash(setup.ChildAssetsPath));
            Assert.AreEqual(
                snapshotHashBefore,
                EmberTemplateInheritanceEngine.ComputeTemplateContentHash(setup.SnapshotAssetsPath));
            Assert.AreEqual(metadataBefore, File.ReadAllText(setup.ChildMetadataPath));
            AssertNoBackups(
                setup.ChildAssetsPath,
                setup.SnapshotAssetsPath,
                setup.ChildMetadataPath);
        }

        private static void AssertNoBackups(params string[] paths)
        {
            foreach (var path in paths)
                Assert.IsFalse(File.Exists(path + ".ember-backup~")
                    || Directory.Exists(path + ".ember-backup~"));
        }

        private sealed class SyncSetup
        {
            internal TemplateInfo Parent { get; }
            internal TemplateInfo Child { get; }
            internal TemplateSyncPlan Plan { get; }
            internal string ParentAssetsPath { get; }
            internal string ChildRoot { get; }
            internal string ChildAssetsPath { get; }
            internal string SnapshotAssetsPath { get; }
            internal string ChildMetadataPath { get; }

            internal SyncSetup(
                TemplateInfo parent,
                TemplateInfo child,
                TemplateSyncPlan plan,
                string parentAssetsPath,
                string childRoot,
                string childAssetsPath,
                string snapshotAssetsPath,
                string childMetadataPath)
            {
                Parent = parent;
                Child = child;
                Plan = plan;
                ParentAssetsPath = parentAssetsPath;
                ChildRoot = childRoot;
                ChildAssetsPath = childAssetsPath;
                SnapshotAssetsPath = snapshotAssetsPath;
                ChildMetadataPath = childMetadataPath;
            }
        }

        private sealed class FakeSceneMergeEnvironment : IUnityYamlMergeEnvironment
        {
            internal FakeSceneMergeEnvironment(
                string applicationContentsPath,
                string projectRoot,
                bool isForceText)
            {
                ApplicationContentsPath = applicationContentsPath;
                ProjectRoot = projectRoot;
                IsForceText = isForceText;
            }

            public string ApplicationContentsPath { get; }
            public string ProjectRoot { get; }
            public bool IsForceText { get; }
            public bool IsWindowsEditor => true;
        }

        private sealed class FakeSceneMergeRunner : IUnityYamlMergeProcessRunner
        {
            internal int CallCount { get; private set; }
            internal Func<UnityYamlMergeProcessRequest, UnityYamlMergeProcessResult> OnRun { get; set; }

            public UnityYamlMergeProcessResult Run(UnityYamlMergeProcessRequest request)
            {
                CallCount++;
                return OnRun?.Invoke(request);
            }
        }

        private sealed class InjectedTransactionException : Exception
        {
        }

        #endregion
    }
}
