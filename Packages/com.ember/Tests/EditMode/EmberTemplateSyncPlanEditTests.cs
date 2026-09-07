// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.IO;

using Ember.Core.Editor;

using NUnit.Framework;

namespace Ember.UI.Tests
{
    /// <summary>模板 O/N/C 三方计划、原子分组与同步闸门的纯 Edit Mode 测试。</summary>
    public class EmberTemplateSyncPlanEditTests
    {
        #region 内部参数

        private const string DEFAULT_GUID = "11111111111111111111111111111111";

        private string _testRoot;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        [SetUp]
        public void SetUp()
        {
            EmberTemplateSceneMergePlanner.ClearPreviewCache();
            _testRoot = Path.Combine(
                Path.GetTempPath(),
                "EmberTemplateSyncPlanTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testRoot);
        }

        [TearDown]
        public void TearDown()
        {
            EmberTemplateSceneMergePlanner.ClearPreviewCache();
            if (!string.IsNullOrEmpty(_testRoot) && Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, true);
        }

        [TestCase("old", "parent", "old",
            TemplateChangeKind.AdoptParentModification, TemplateConflictChoice.AcceptParent)]
        [TestCase("old", "old", "child",
            TemplateChangeKind.KeepChildModification, TemplateConflictChoice.KeepChild)]
        [TestCase("old", "parent", "parent",
            TemplateChangeKind.AlreadyMatchesParent, TemplateConflictChoice.KeepChild)]
        [TestCase("old", "parent", "child",
            TemplateChangeKind.ConflictConcurrentModification, TemplateConflictChoice.Unresolved)]
        [TestCase(null, "parent", null,
            TemplateChangeKind.AdoptParentAddition, TemplateConflictChoice.AcceptParent)]
        [TestCase(null, null, "child",
            TemplateChangeKind.KeepChildAddition, TemplateConflictChoice.KeepChild)]
        [TestCase(null, "same", "same",
            TemplateChangeKind.AlreadyMatchesParent, TemplateConflictChoice.KeepChild)]
        [TestCase(null, "parent", "child",
            TemplateChangeKind.ConflictConcurrentAddition, TemplateConflictChoice.Unresolved)]
        [TestCase("old", null, "old",
            TemplateChangeKind.AdoptParentDeletion, TemplateConflictChoice.AcceptParent)]
        [TestCase("old", null, "child",
            TemplateChangeKind.ConflictParentDeletionChildModification, TemplateConflictChoice.Unresolved)]
        [TestCase("old", "old", null,
            TemplateChangeKind.KeepChildDeletion, TemplateConflictChoice.KeepChild)]
        [TestCase("old", "parent", null,
            TemplateChangeKind.ConflictParentModificationChildDeletion, TemplateConflictChoice.Unresolved)]
        public void ThreeWayClassification_ShouldCoverSafeAndConflictBranches(
            string oldContent,
            string parentContent,
            string childContent,
            TemplateChangeKind expectedKind,
            TemplateConflictChoice expectedChoice)
        {
            var paths = CreateThreeWayDirectories();
            WriteOptionalAsset(paths.OldParent, oldContent);
            WriteOptionalAsset(paths.CurrentParent, parentContent);
            WriteOptionalAsset(paths.CurrentChild, childContent);

            var changes = EmberTemplateInheritanceEngine.BuildThreeWayChanges(
                paths.OldParent,
                paths.CurrentParent,
                paths.CurrentChild);

            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual("Example.prefab", changes[0].UnitPath);
            Assert.AreEqual(TemplateChangeUnitKind.Asset, changes[0].UnitKind);
            Assert.AreEqual(expectedKind, changes[0].Kind);
            Assert.AreEqual(expectedChoice, changes[0].RecommendedChoice);
            Assert.AreEqual(2, changes[0].Files.Count,
                "资源文件与同名 .meta 必须始终组成一个原子单元。");
        }

        [Test]
        public void ThreeWayClassification_AllSidesEqualShouldProduceNoChange()
        {
            var paths = CreateThreeWayDirectories();
            WriteOptionalAsset(paths.OldParent, "same");
            WriteOptionalAsset(paths.CurrentParent, "same");
            WriteOptionalAsset(paths.CurrentChild, "same");

            var changes = EmberTemplateInheritanceEngine.BuildThreeWayChanges(
                paths.OldParent,
                paths.CurrentParent,
                paths.CurrentChild);

            Assert.AreEqual(0, changes.Count);
        }

        [Test]
        public void ThreeWayClassification_GuidMismatchShouldBeRedConflict()
        {
            var paths = CreateThreeWayDirectories();
            WriteOptionalAsset(paths.OldParent, "same", DEFAULT_GUID);
            WriteOptionalAsset(paths.CurrentParent, "same", DEFAULT_GUID);
            WriteOptionalAsset(paths.CurrentChild, "same", "22222222222222222222222222222222");

            var changes = EmberTemplateInheritanceEngine.BuildThreeWayChanges(
                paths.OldParent,
                paths.CurrentParent,
                paths.CurrentChild);

            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(TemplateChangeKind.ConflictGuidMismatch, changes[0].Kind);
            Assert.IsTrue(changes[0].IsConflict);
        }

        [Test]
        public void ThreeWayClassification_DirectoryMetaShouldBeSeparateUnit()
        {
            var paths = CreateThreeWayDirectories();
            WriteDirectoryMeta(paths.OldParent, "Config", DEFAULT_GUID, "old");
            WriteDirectoryMeta(paths.CurrentParent, "Config", DEFAULT_GUID, "parent");
            WriteDirectoryMeta(paths.CurrentChild, "Config", DEFAULT_GUID, "old");

            var changes = EmberTemplateInheritanceEngine.BuildThreeWayChanges(
                paths.OldParent,
                paths.CurrentParent,
                paths.CurrentChild);

            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual("Config", changes[0].UnitPath);
            Assert.AreEqual(TemplateChangeUnitKind.DirectoryMetadata, changes[0].UnitKind);
            Assert.AreEqual(TemplateChangeKind.AdoptParentModification, changes[0].Kind);
            Assert.AreEqual(1, changes[0].Files.Count);
            Assert.AreEqual("Config.meta", changes[0].Files[0].RelativePath);
        }

        [Test]
        public void ThreeWayClassification_CurrentFileDirectoryMismatchShouldConflict()
        {
            var paths = CreateThreeWayDirectories();
            WriteOptionalAsset(paths.OldParent, "old");
            WriteDirectoryMeta(paths.CurrentParent, "Example.prefab", DEFAULT_GUID, "directory");
            WriteOptionalAsset(paths.CurrentChild, "old");

            var changes = EmberTemplateInheritanceEngine.BuildThreeWayChanges(
                paths.OldParent,
                paths.CurrentParent,
                paths.CurrentChild);

            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(TemplateChangeUnitKind.PathTypeConflict, changes[0].UnitKind);
            Assert.AreEqual(TemplateChangeKind.ConflictPathTypeChanged, changes[0].Kind);
        }

        [Test]
        public void TemplateGraph_MissingParentShouldBeReported()
        {
            var child = CreateTemplate("child", "missing");

            var issues = EmberTemplateInheritanceEngine.ValidateTemplateGraph(
                new[] { child });

            Assert.AreEqual(1, issues.Count);
            Assert.AreEqual(TemplateSyncStatus.ParentMissing, issues[0].Status);
        }

        [Test]
        public void TemplateGraph_CycleShouldBeReportedForAffectedTemplates()
        {
            var first = CreateTemplate("first", "second");
            var second = CreateTemplate("second", "first");

            var issues = EmberTemplateInheritanceEngine.ValidateTemplateGraph(
                new[] { first, second });

            Assert.IsNotNull(issues.Find(issue =>
                issue.TemplateId == "first" && issue.Status == TemplateSyncStatus.CycleDetected));
            Assert.IsNotNull(issues.Find(issue =>
                issue.TemplateId == "second" && issue.Status == TemplateSyncStatus.CycleDetected));
        }

        [Test]
        public void ParentSyncPlan_ParentChangeShouldProduceApplicableDryRun()
        {
            var setup = CreateValidPlanSetup("old", "parent", "old");

            var plan = BuildPlan(setup);

            Assert.AreEqual(TemplateSyncStatus.ParentChanged, plan.Status);
            Assert.IsTrue(plan.CanApply);
            Assert.IsFalse(plan.HasConflicts);
            Assert.AreEqual(1, plan.Changes.Count);
            Assert.AreEqual(TemplateChangeKind.AdoptParentModification, plan.Changes[0].Kind);
        }

        [Test]
        public void ParentSyncPlan_ParentVersionOnlyChangeShouldHaveNoContentChanges()
        {
            var setup = CreateValidPlanSetup("same", "same", "same");

            var plan = BuildPlan(setup);

            Assert.AreEqual(TemplateSyncStatus.ParentChanged, plan.Status);
            Assert.IsTrue(plan.CanApply);
            Assert.AreEqual(0, plan.Changes.Count);
        }

        [Test]
        public void ParentSyncPlan_ParentFrameworkOnlyChangeShouldRequirePointerSync()
        {
            var setup = CreateValidPlanSetup("same", "same", "same");
            setup.Child.parentVersion = setup.Parent.version;
            setup.Parent.frameworkVersion = "0.11.0";
            setup.Child.frameworkVersion = "0.10.0";

            var plan = BuildPlan(setup);

            Assert.AreEqual(TemplateSyncStatus.ParentChanged, plan.Status);
            Assert.IsTrue(plan.CanApply);
            Assert.AreEqual(0, plan.Changes.Count);
        }

        [Test]
        public void ParentSyncPlan_UnresolvedConflictShouldPreventApply()
        {
            var setup = CreateValidPlanSetup("old", "parent", "child");

            var plan = BuildPlan(setup);

            Assert.AreEqual(TemplateSyncStatus.ParentChanged, plan.Status);
            Assert.IsTrue(plan.IsEligible);
            Assert.IsTrue(plan.HasUnresolvedConflicts);
            Assert.IsFalse(plan.CanApply);
            Assert.AreEqual(TemplateChangeKind.ConflictConcurrentModification,
                plan.Changes[0].Kind);
        }

        [Test]
        public void ScenePreview_MergeableConcurrentSceneShouldBecomeSemanticAutoMerge()
        {
            var setup = CreateScenePlanSetup("Old", "Parent", "Child");
            var runner = new FakeSceneMergeRunner
            {
                OnRun = request =>
                {
                    WriteAbsoluteFile(
                        request.Arguments[request.Arguments.Count - 1],
                        CreateSceneYaml("Merged"));
                    return SuccessfulMergeProcess();
                }
            };
            var adapter = CreateSceneMergeAdapter(runner, true);

            var previewPlan = EmberTemplateSceneMergePlanner.EnhancePlan(
                BuildPlan(setup),
                setup.Paths.OldParent,
                setup.Paths.CurrentParent,
                setup.Paths.CurrentChild,
                adapter);

            Assert.AreEqual(1, runner.CallCount);
            Assert.AreEqual(1, previewPlan.Changes.Count);
            Assert.IsTrue(previewPlan.CanApply);
            Assert.IsTrue(previewPlan.HasSemanticMerges);
            Assert.IsTrue(previewPlan.WillChangeChildContent);
            var change = previewPlan.Changes[0];
            Assert.AreEqual(TemplateChangeUnitKind.SceneSemantic, change.UnitKind);
            Assert.AreEqual(TemplateChangeKind.AutoMergeScene, change.Kind);
            Assert.AreEqual(TemplateChangeApplyMode.SemanticMerge, change.ApplyMode);
            Assert.IsFalse(change.IsConflict);
            Assert.IsTrue(change.WillChangeChildContent);
            Assert.AreEqual(SceneSemanticMergeStatus.Succeeded,
                change.SceneMergePreview.Status);
            Assert.AreEqual(change.Files[0].OldHash, change.SceneMergePreview.OldHash);
            Assert.AreEqual(change.Files[0].ParentHash, change.SceneMergePreview.ParentHash);
            Assert.AreEqual(change.Files[0].ChildHash, change.SceneMergePreview.ChildHash);
            Assert.IsTrue(
                EmberTemplateDevelopmentPanel.WillSyncChoiceChangeContent(
                    change,
                    TemplateConflictChoice.KeepChild),
                "自动语义合并改变内容时必须触发版本 bump 闸门。");
        }

        [Test]
        public void ScenePreview_PrefabConflictShouldRemainFileLevelWithoutRunner()
        {
            var setup = CreateValidPlanSetup("old", "parent", "child");
            var runner = new FakeSceneMergeRunner();

            var previewPlan = EmberTemplateSceneMergePlanner.EnhancePlan(
                BuildPlan(setup),
                setup.Paths.OldParent,
                setup.Paths.CurrentParent,
                setup.Paths.CurrentChild,
                CreateSceneMergeAdapter(runner, true));

            Assert.AreEqual(0, runner.CallCount);
            Assert.IsTrue(previewPlan.HasConflicts);
            Assert.AreEqual(TemplateChangeUnitKind.Asset, previewPlan.Changes[0].UnitKind);
            Assert.AreEqual(
                TemplateChangeKind.ConflictConcurrentModification,
                previewPlan.Changes[0].Kind);
            Assert.IsNull(previewPlan.Changes[0].SceneMergePreview);
        }

        [Test]
        public void ScenePreview_GuidMismatchShouldRemainFileLevelWithoutRunner()
        {
            var setup = CreateScenePlanSetup("Old", "Parent", "Child");
            WriteAsset(
                setup.Paths.CurrentChild,
                "Game/Scenes/FrameworkScene.unity",
                CreateSceneYaml("Child"),
                "22222222222222222222222222222222");
            setup.Child.contentHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(
                setup.Paths.CurrentChild);
            setup.Child.versionedContentHash = setup.Child.contentHash;
            var runner = new FakeSceneMergeRunner();

            var previewPlan = EmberTemplateSceneMergePlanner.EnhancePlan(
                BuildPlan(setup),
                setup.Paths.OldParent,
                setup.Paths.CurrentParent,
                setup.Paths.CurrentChild,
                CreateSceneMergeAdapter(runner, true));

            Assert.AreEqual(0, runner.CallCount);
            Assert.AreEqual(TemplateChangeKind.ConflictGuidMismatch,
                previewPlan.Changes[0].Kind);
            Assert.IsTrue(previewPlan.Changes[0].IsConflict);
        }

        [Test]
        public void ScenePreview_ToolFailureShouldPreserveWholeSceneConflictAndReason()
        {
            var setup = CreateScenePlanSetup("Old", "Parent", "Child");
            var runner = new FakeSceneMergeRunner
            {
                OnRun = _ => new UnityYamlMergeProcessResult(
                    true,
                    1,
                    false,
                    string.Empty,
                    "semantic conflict",
                    null)
            };

            var previewPlan = EmberTemplateSceneMergePlanner.EnhancePlan(
                BuildPlan(setup),
                setup.Paths.OldParent,
                setup.Paths.CurrentParent,
                setup.Paths.CurrentChild,
                CreateSceneMergeAdapter(runner, true));

            Assert.AreEqual(1, runner.CallCount);
            Assert.IsTrue(previewPlan.HasConflicts);
            var change = previewPlan.Changes[0];
            Assert.AreEqual(TemplateChangeKind.ConflictConcurrentModification, change.Kind);
            Assert.AreEqual(SceneSemanticMergeStatus.Fallback,
                change.SceneMergePreview.Status);
            StringAssert.Contains("退出码 1", change.SceneMergePreview.FailureReason);
            Assert.IsFalse(
                EmberTemplateDevelopmentPanel.WillSyncChoiceChangeContent(
                    change,
                    TemplateConflictChoice.KeepChild));
            Assert.IsTrue(
                EmberTemplateDevelopmentPanel.WillSyncChoiceChangeContent(
                    change,
                    TemplateConflictChoice.AcceptParent));
        }

        [Test]
        public void ScenePreview_NonForceTextShouldFallbackWithoutCallingRunner()
        {
            var setup = CreateScenePlanSetup("Old", "Parent", "Child");
            var runner = new FakeSceneMergeRunner();

            var previewPlan = EmberTemplateSceneMergePlanner.EnhancePlan(
                BuildPlan(setup),
                setup.Paths.OldParent,
                setup.Paths.CurrentParent,
                setup.Paths.CurrentChild,
                CreateSceneMergeAdapter(runner, false));

            Assert.AreEqual(0, runner.CallCount);
            Assert.IsTrue(previewPlan.HasConflicts);
            Assert.AreEqual(SceneSemanticMergeStatus.Fallback,
                previewPlan.Changes[0].SceneMergePreview.Status);
            StringAssert.Contains("Force Text",
                previewPlan.Changes[0].SceneMergePreview.FailureReason);
        }

        [Test]
        public void ScenePreview_CacheShouldInvalidateForInputToolAndRulesFingerprints()
        {
            var setup = CreateScenePlanSetup("Old", "Parent", "Child");
            var runner = new FakeSceneMergeRunner();
            runner.OnRun = request =>
            {
                WriteAbsoluteFile(
                    request.Arguments[request.Arguments.Count - 1],
                    CreateSceneYaml("Merged-" + runner.CallCount));
                return SuccessfulMergeProcess();
            };
            var adapter = CreateSceneMergeAdapter(runner, true);

            EnhanceScenePlan(setup, adapter);
            EnhanceScenePlan(setup, adapter);
            Assert.AreEqual(1, runner.CallCount, "相同指纹应复用 dry-run 缓存。");

            WriteAsset(
                setup.Paths.CurrentChild,
                "Game/Scenes/FrameworkScene.unity",
                CreateSceneYaml("Child-Changed"),
                DEFAULT_GUID);
            setup.Child.contentHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(
                setup.Paths.CurrentChild);
            setup.Child.versionedContentHash = setup.Child.contentHash;
            EnhanceScenePlan(setup, adapter);
            Assert.AreEqual(2, runner.CallCount, "C hash 变化必须使缓存失效。");

            WriteAbsoluteFile(GetFakeRulesPath(), "rules changed");
            EnhanceScenePlan(setup, adapter);
            Assert.AreEqual(3, runner.CallCount, "规则 hash 变化必须使缓存失效。");

            WriteAbsoluteFile(GetFakeToolPath(), "tool changed");
            EnhanceScenePlan(setup, adapter);
            Assert.AreEqual(4, runner.CallCount, "工具版本指纹变化必须使缓存失效。");
        }

        [Test]
        public void ParentSyncPlan_UnversionedParentShouldBeBlocked()
        {
            var setup = CreateValidPlanSetup("old", "parent", "old");
            setup.Parent.versionedContentHash = "previous-content";

            var plan = BuildPlan(setup);

            Assert.AreEqual(TemplateSyncStatus.ParentUnversioned, plan.Status);
            Assert.IsFalse(plan.CanApply);
        }

        [Test]
        public void ParentSyncPlan_CorruptedSnapshotShouldBeBlocked()
        {
            var setup = CreateValidPlanSetup("old", "parent", "old");
            setup.Child.parentContentHash = "not-the-snapshot-hash";

            var plan = BuildPlan(setup);

            Assert.AreEqual(TemplateSyncStatus.SnapshotMissingOrCorrupted, plan.Status);
            Assert.IsFalse(plan.CanApply);
        }

        [Test]
        public void ParentSyncPlan_DirtyParentStorageShouldBeBlocked()
        {
            var setup = CreateValidPlanSetup("old", "parent", "old");
            setup.Parent.contentHash = "stale-parent-hash";
            setup.Parent.versionedContentHash = "stale-parent-hash";

            var plan = BuildPlan(setup);

            Assert.AreEqual(TemplateSyncStatus.TemplateContentDirty, plan.Status);
            StringAssert.Contains("父模板", plan.Error);
        }

        [Test]
        public void ParentSyncPlan_DirtyChildStorageShouldBeBlocked()
        {
            var setup = CreateValidPlanSetup("old", "parent", "old");
            setup.Child.contentHash = "stale-child-hash";

            var plan = BuildPlan(setup);

            Assert.AreEqual(TemplateSyncStatus.TemplateContentDirty, plan.Status);
            StringAssert.Contains("派生模板", plan.Error);
        }

        [Test]
        public void ParentSyncPlan_DuplicateGuidInChildShouldBeBlocked()
        {
            var setup = CreateValidPlanSetup("old", "parent", "old");
            WriteAsset(setup.Paths.CurrentChild, "Second.prefab", "second", DEFAULT_GUID);
            setup.Child.contentHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(
                setup.Paths.CurrentChild);

            var plan = BuildPlan(setup);

            Assert.AreEqual(TemplateSyncStatus.InvalidAssetMetadata, plan.Status);
            StringAssert.Contains("GUID 重复", plan.Error);
            Assert.IsFalse(plan.CanApply);
        }

        [Test]
        public void RootSyncStatus_LegacyMetadataShouldRequireInitialization()
        {
            var assets = Path.Combine(_testRoot, "LegacyRoot");
            Directory.CreateDirectory(assets);
            var root = CreateTemplate("base", null);
            root.schemaVersion = 0;
            root.contentHash = null;
            root.versionedContentHash = null;

            var plan = EmberTemplateInheritanceEngine.BuildParentSyncPlan(
                root,
                new[] { root },
                null,
                null,
                assets);

            Assert.AreEqual(TemplateSyncStatus.MetadataNotInitialized, plan.Status);
        }

        [Test]
        public void RootSyncStatus_DirtyStorageShouldBeBlocked()
        {
            var assets = Path.Combine(_testRoot, "DirtyRoot");
            Directory.CreateDirectory(assets);
            WriteOptionalAsset(assets, "live");
            var root = CreateTemplate("base", null);
            root.contentHash = "stale";
            root.versionedContentHash = "stale";

            var plan = EmberTemplateInheritanceEngine.BuildParentSyncPlan(
                root,
                new[] { root },
                null,
                null,
                assets);

            Assert.AreEqual(TemplateSyncStatus.TemplateContentDirty, plan.Status);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private PlanSetup CreateScenePlanSetup(
            string oldName,
            string parentName,
            string childName)
        {
            var paths = CreateThreeWayDirectories();
            const string scenePath = "Game/Scenes/FrameworkScene.unity";
            WriteAsset(paths.OldParent, scenePath, CreateSceneYaml(oldName), DEFAULT_GUID);
            WriteAsset(paths.CurrentParent, scenePath, CreateSceneYaml(parentName), DEFAULT_GUID);
            WriteAsset(paths.CurrentChild, scenePath, CreateSceneYaml(childName), DEFAULT_GUID);

            var oldHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(paths.OldParent);
            var parentHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(
                paths.CurrentParent);
            var childHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(
                paths.CurrentChild);
            var parent = CreateTemplate("base", null);
            parent.version = "0.6.0";
            parent.contentHash = parentHash;
            parent.versionedContentHash = parentHash;

            var child = CreateTemplate("child", "base");
            child.parentVersion = "0.5.0";
            child.parentContentHash = oldHash;
            child.contentHash = childHash;
            child.versionedContentHash = childHash;
            return new PlanSetup
            {
                Paths = paths,
                Parent = parent,
                Child = child
            };
        }

        private TemplateSyncPlan EnhanceScenePlan(
            PlanSetup setup,
            EmberUnityYamlMerge adapter)
        {
            return EmberTemplateSceneMergePlanner.EnhancePlan(
                BuildPlan(setup),
                setup.Paths.OldParent,
                setup.Paths.CurrentParent,
                setup.Paths.CurrentChild,
                adapter);
        }

        private EmberUnityYamlMerge CreateSceneMergeAdapter(
            FakeSceneMergeRunner runner,
            bool isForceText)
        {
            WriteAbsoluteFile(GetFakeToolPath(), "fake tool");
            WriteAbsoluteFile(GetFakeRulesPath(), "fake rules");
            return new EmberUnityYamlMerge(
                runner,
                new FakeSceneMergeEnvironment(
                    Path.Combine(_testRoot, "Fake Unity", "Data"),
                    Path.Combine(_testRoot, "Fake Project"),
                    isForceText));
        }

        private string GetFakeToolPath()
        {
            return Path.Combine(
                _testRoot,
                "Fake Unity",
                "Data",
                "Tools",
                "UnityYAMLMerge.exe");
        }

        private string GetFakeRulesPath()
        {
            return Path.Combine(
                _testRoot,
                "Fake Unity",
                "Data",
                "Tools",
                "mergerules.txt");
        }

        private static string CreateSceneYaml(string name)
        {
            return "%YAML 1.1\n"
                + "%TAG !u! tag:unity3d.com,2011:\n"
                + "--- !u!1 &1\n"
                + "GameObject:\n"
                + "  m_Name: " + name + "\n";
        }

        private static UnityYamlMergeProcessResult SuccessfulMergeProcess()
        {
            return new UnityYamlMergeProcessResult(
                true,
                0,
                false,
                "success",
                string.Empty,
                null);
        }

        private static void WriteAbsoluteFile(string path, string content)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(path, content);
        }

        private ThreeWayPaths CreateThreeWayDirectories()
        {
            var root = Path.Combine(_testRoot, Guid.NewGuid().ToString("N"));
            var paths = new ThreeWayPaths
            {
                OldParent = Path.Combine(root, "O"),
                CurrentParent = Path.Combine(root, "N"),
                CurrentChild = Path.Combine(root, "C")
            };
            Directory.CreateDirectory(paths.OldParent);
            Directory.CreateDirectory(paths.CurrentParent);
            Directory.CreateDirectory(paths.CurrentChild);
            return paths;
        }

        private PlanSetup CreateValidPlanSetup(
            string oldContent,
            string parentContent,
            string childContent)
        {
            var paths = CreateThreeWayDirectories();
            WriteOptionalAsset(paths.OldParent, oldContent);
            WriteOptionalAsset(paths.CurrentParent, parentContent);
            WriteOptionalAsset(paths.CurrentChild, childContent);

            var oldHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(paths.OldParent);
            var parentHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(paths.CurrentParent);
            var childHash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(paths.CurrentChild);
            var parent = CreateTemplate("base", null);
            parent.version = "0.6.0";
            parent.contentHash = parentHash;
            parent.versionedContentHash = parentHash;

            var child = CreateTemplate("child", "base");
            child.parentVersion = "0.5.0";
            child.parentContentHash = oldHash;
            child.contentHash = childHash;
            child.versionedContentHash = childHash;

            return new PlanSetup
            {
                Paths = paths,
                Parent = parent,
                Child = child
            };
        }

        private static TemplateSyncPlan BuildPlan(PlanSetup setup)
        {
            return EmberTemplateInheritanceEngine.BuildParentSyncPlan(
                setup.Child,
                new[] { setup.Parent, setup.Child },
                setup.Paths.CurrentParent,
                setup.Paths.OldParent,
                setup.Paths.CurrentChild);
        }

        private static TemplateInfo CreateTemplate(string id, string parentId)
        {
            return new TemplateInfo
            {
                schemaVersion = EmberTemplateInheritanceEngine.CurrentSchemaVersion,
                id = id,
                displayName = id,
                description = string.Empty,
                version = "0.1.0",
                frameworkVersion = "0.10.0",
                channel = string.IsNullOrEmpty(parentId) ? "stable" : "preview",
                order = 1,
                parentId = parentId ?? string.Empty,
                parentVersion = string.Empty,
                parentContentHash = string.Empty,
                contentHash = "content",
                versionedContentHash = "content"
            };
        }

        private static void WriteOptionalAsset(
            string root,
            string content,
            string guid = DEFAULT_GUID)
        {
            if (content == null) return;
            WriteAsset(root, "Example.prefab", content, guid);
        }

        private static void WriteAsset(
            string root,
            string relativePath,
            string content,
            string guid)
        {
            WriteFile(root, relativePath, content);
            WriteFile(root, relativePath + ".meta",
                "fileFormatVersion: 2\n" +
                "guid: " + guid + "\n");
        }

        private static void WriteDirectoryMeta(
            string root,
            string relativePath,
            string guid,
            string marker)
        {
            Directory.CreateDirectory(Path.Combine(
                root,
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
            WriteFile(root, relativePath + ".meta",
                "fileFormatVersion: 2\n" +
                "guid: " + guid + "\n" +
                "folderAsset: yes\n" +
                "marker: " + marker + "\n");
        }

        private static void WriteFile(string root, string relativePath, string content)
        {
            var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, content);
        }

        private sealed class ThreeWayPaths
        {
            public string OldParent;
            public string CurrentParent;
            public string CurrentChild;
        }

        private sealed class PlanSetup
        {
            public ThreeWayPaths Paths;
            public TemplateInfo Parent;
            public TemplateInfo Child;
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

        #endregion
    }
}
