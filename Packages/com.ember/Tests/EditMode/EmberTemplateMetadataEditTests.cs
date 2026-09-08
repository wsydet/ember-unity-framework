// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.IO;

using Ember.Core.Editor;

using NUnit.Framework;

using UnityEngine;

namespace Ember.UI.Tests
{
    /// <summary>模板 metadata v2 与确定性目录 hash 的纯 Edit Mode 测试。</summary>
    public class EmberTemplateMetadataEditTests
    {
        #region 内部参数

        private string _testRoot;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        [SetUp]
        public void SetUp()
        {
            _testRoot = Path.Combine(
                Path.GetTempPath(),
                "EmberTemplateMetadataTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_testRoot) && Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, true);
        }

        [Test]
        public void ContentHash_FileCreationOrderShouldNotMatter()
        {
            var first = CreateAssetsDirectory("First");
            var second = CreateAssetsDirectory("Second");

            WriteFile(first, "Zeta/Z.txt", "z");
            WriteFile(first, "Alpha/A.txt", "a");
            WriteFile(second, "Alpha/A.txt", "a");
            WriteFile(second, "Zeta/Z.txt", "z");

            Assert.AreEqual(
                EmberTemplateInheritanceEngine.ComputeTemplateContentHash(first),
                EmberTemplateInheritanceEngine.ComputeTemplateContentHash(second));
        }

        [Test]
        public void ContentHash_MetaChangeShouldChangeTreeHash()
        {
            var assets = CreateAssetsDirectory("MetaIncluded");
            WriteFile(assets, "Example.prefab", "prefab");
            WriteFile(assets, "Example.prefab.meta", "guid: first");
            var before = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(assets);

            WriteFile(assets, "Example.prefab.meta", "guid: second");
            var after = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(assets);

            Assert.AreNotEqual(before, after);
        }

        [Test]
        public void ContentHash_TextLineEndingStyleShouldNotMatter()
        {
            var first = CreateAssetsDirectory("LfText");
            var second = CreateAssetsDirectory("CrLfText");

            WriteFile(first, "Game/State.cs", "first\nsecond\n");
            WriteFile(second, "Game/State.cs", "first\r\nsecond\r\n");

            Assert.AreEqual(
                EmberTemplateInheritanceEngine.ComputeTemplateContentHash(first),
                EmberTemplateInheritanceEngine.ComputeTemplateContentHash(second));
        }

        [Test]
        public void ContentHash_BinaryLineEndingBytesShouldRemainSignificant()
        {
            var first = CreateAssetsDirectory("LfBinary");
            var second = CreateAssetsDirectory("CrLfBinary");

            File.WriteAllBytes(Path.Combine(first, "Image.png"), new byte[] { 1, 13, 10, 2 });
            File.WriteAllBytes(Path.Combine(second, "Image.png"), new byte[] { 1, 10, 2 });

            Assert.AreNotEqual(
                EmberTemplateInheritanceEngine.ComputeTemplateContentHash(first),
                EmberTemplateInheritanceEngine.ComputeTemplateContentHash(second));
        }

        [Test]
        public void ContentHash_EmptyDirectoryShouldBeDeterministic()
        {
            var first = CreateAssetsDirectory("EmptyFirst");
            var second = CreateAssetsDirectory("EmptySecond");

            Assert.AreEqual(
                EmberTemplateInheritanceEngine.ComputeTemplateContentHash(first),
                EmberTemplateInheritanceEngine.ComputeTemplateContentHash(second));
        }

        [Test]
        public void PathValidation_CaseCollidingDirectoryShouldBeRejected()
        {
            var paths = new[] { "Framework/Page.cs", "framework/Other.cs" };

            var error = Assert.Throws<InvalidDataException>(() =>
                EmberTemplateInheritanceEngine.ValidateCaseInsensitivePathUniqueness(paths));
            StringAssert.Contains("大小写路径碰撞", error.Message);
        }

        [Test]
        public void AssetValidation_OptionalDirectoryMetaAndVirtualEmptyFolderShouldBeAccepted()
        {
            var assets = CreateAssetsDirectory("LegacyDirectoryLayout");
            WriteFile(assets, "Game/State.cs", "content");
            WriteFile(assets, "Game/State.cs.meta", "guid: state-guid");
            WriteFile(
                assets,
                "Game/Empty.meta",
                "fileFormatVersion: 2\nguid: empty-folder-guid\nfolderAsset: yes\n");

            Assert.IsFalse(Directory.Exists(Path.Combine(assets, "Game", "Empty")));
            Assert.IsTrue(EmberTemplateInheritanceEngine.TryValidateTemplateAssets(
                assets,
                out var contentHash,
                out var error), error);
            Assert.IsNotEmpty(contentHash);
            Assert.IsNull(error);
        }

        [Test]
        public void AssetValidation_OrdinaryOrphanMetaShouldBeRejected()
        {
            var assets = CreateAssetsDirectory("OrphanMetadata");
            WriteFile(assets, "Missing.asset.meta", "guid: orphan-guid");

            Assert.IsFalse(EmberTemplateInheritanceEngine.TryValidateTemplateAssets(
                assets,
                out _,
                out var error));
            StringAssert.Contains("孤立 .meta", error);
        }

        [Test]
        public void MetadataMigration_SchemaV1ShouldNotMutateSourceContentOrVersion()
        {
            var assets = CreateAssetsDirectory("Legacy");
            var file = WriteFile(assets, "Game/State.cs", "legacy-content");
            WriteFile(assets, "Game/State.cs.meta", "guid: legacy-state-guid");
            var source = new TemplateInfo
            {
                id = "base",
                displayName = "基础模板",
                version = "0.5.0",
                frameworkVersion = "0.10.0",
                channel = "stable",
                order = 1
            };

            var fileBefore = File.ReadAllText(file);
            var plan = EmberTemplateInheritanceEngine.BuildMetadataMigrationPlan(source, assets);

            Assert.IsTrue(plan.IsRequired);
            Assert.IsTrue(plan.CanApply);
            Assert.IsNull(plan.Error);
            Assert.AreEqual(0, source.schemaVersion);
            Assert.AreEqual("0.5.0", source.version);
            Assert.IsNull(source.contentHash);
            Assert.AreEqual(fileBefore, File.ReadAllText(file));

            Assert.AreEqual(EmberTemplateInheritanceEngine.CurrentSchemaVersion,
                plan.Migrated.schemaVersion);
            Assert.AreEqual("0.5.0", plan.Migrated.version);
            Assert.AreEqual("0.10.0", plan.Migrated.frameworkVersion);
            Assert.AreEqual(string.Empty, plan.Migrated.parentId);
            Assert.AreEqual(string.Empty, plan.Migrated.parentVersion);
            Assert.AreEqual(string.Empty, plan.Migrated.parentContentHash);
            Assert.AreEqual(plan.ComputedContentHash, plan.Migrated.contentHash);
            Assert.AreEqual(plan.ComputedContentHash, plan.Migrated.versionedContentHash);
        }

        [Test]
        public void MetadataMigration_InvalidAssetsShouldNotProduceWritablePlan()
        {
            var assets = CreateAssetsDirectory("InvalidLegacy");
            WriteFile(assets, "Game/State.cs", "missing-meta");
            var source = new TemplateInfo
            {
                id = "base",
                version = "0.5.0",
                frameworkVersion = "0.10.0"
            };

            var plan = EmberTemplateInheritanceEngine.BuildMetadataMigrationPlan(source, assets);

            Assert.IsTrue(plan.IsRequired);
            Assert.IsFalse(plan.CanApply);
            Assert.IsNull(plan.Migrated);
            StringAssert.Contains("资源缺少配套 .meta", plan.Error);
        }

        [Test]
        public void MetadataMigration_SchemaV2ShouldNotRequestWrite()
        {
            var assets = CreateAssetsDirectory("Current");
            var hash = EmberTemplateInheritanceEngine.ComputeTemplateContentHash(assets);
            var source = CreateSchemaV2Template(hash, hash);

            var plan = EmberTemplateInheritanceEngine.BuildMetadataMigrationPlan(source, assets);

            Assert.IsFalse(plan.IsRequired);
            Assert.IsFalse(plan.CanApply);
            Assert.IsNull(plan.Error);
            Assert.AreEqual(hash, plan.Migrated.contentHash);
            Assert.AreNotSame(source, plan.Migrated);
        }

        [Test]
        public void MetadataJson_ShouldReadLegacyDefaultsAndRoundTripSchemaV2Fields()
        {
            const string legacyJson =
                "{\"id\":\"base\",\"version\":\"0.5.0\",\"frameworkVersion\":\"0.10.0\"}";
            var legacy = JsonUtility.FromJson<TemplateInfo>(legacyJson);

            Assert.AreEqual(0, legacy.schemaVersion);
            Assert.IsNull(legacy.parentId);
            Assert.IsNull(legacy.contentHash);

            var source = CreateSchemaV2Template("content", "versioned");
            source.parentId = "base";
            source.parentVersion = "0.5.0";
            source.parentContentHash = "parent";
            var roundTrip = JsonUtility.FromJson<TemplateInfo>(JsonUtility.ToJson(source));

            Assert.AreEqual(2, roundTrip.schemaVersion);
            Assert.AreEqual("base", roundTrip.parentId);
            Assert.AreEqual("0.5.0", roundTrip.parentVersion);
            Assert.AreEqual("parent", roundTrip.parentContentHash);
            Assert.AreEqual("content", roundTrip.contentHash);
            Assert.AreEqual("versioned", roundTrip.versionedContentHash);
        }

        [Test]
        public void SavedMetadata_ShouldMarkContentDirtyWithoutChangingVersionOrFramework()
        {
            var source = CreateSchemaV2Template("old-content", "old-versioned");
            source.parentId = "base";
            source.parentVersion = "0.5.0";
            source.parentContentHash = "parent-hash";

            var saved = EmberTemplateInheritanceEngine.BuildSavedMetadata(
                source,
                "平台模板",
                "更新描述",
                "new-content");

            Assert.AreEqual("new-content", saved.contentHash);
            Assert.AreEqual("old-versioned", saved.versionedContentHash);
            Assert.AreEqual("0.1.0", saved.version);
            Assert.AreEqual("0.10.0", saved.frameworkVersion);
            Assert.AreEqual("base", saved.parentId);
            Assert.AreEqual("old-content", source.contentHash);
        }

        [Test]
        public void BumpedMetadata_ShouldSealCurrentContentHash()
        {
            var source = CreateSchemaV2Template("saved-content", "old-versioned");

            var bumped = EmberTemplateInheritanceEngine.BuildBumpedMetadata(source, 1);

            Assert.AreEqual("0.2.0", bumped.version);
            Assert.AreEqual("saved-content", bumped.contentHash);
            Assert.AreEqual("saved-content", bumped.versionedContentHash);
            Assert.AreEqual("0.1.0", source.version);
            Assert.AreEqual("old-versioned", source.versionedContentHash);
        }

        [Test]
        public void FrameworkDeclaration_DerivedTemplateShouldBeRejected()
        {
            var source = CreateSchemaV2Template("content", "content");
            source.parentId = "base";

            var error = Assert.Throws<InvalidOperationException>(() =>
                EmberTemplateInheritanceEngine.BuildFrameworkDeclaration(source, "0.11.0"));
            StringAssert.Contains("不能单独声明框架版本", error.Message);
        }

        [Test]
        public void Versioning_DirtyContentCannotBeSealedWithSameVersion()
        {
            var source = CreateSchemaV2Template("saved-content", "old-versioned");

            var error = Assert.Throws<InvalidOperationException>(() =>
                EmberTemplateInheritanceEngine.BuildVersionedMetadata(source, source.version));
            StringAssert.Contains("请选择主/次/补丁 bump", error.Message);
        }

        [Test]
        public void FrameworkDeclaration_UnversionedRootShouldBeRejected()
        {
            var source = CreateSchemaV2Template("saved-content", "old-versioned");

            var error = Assert.Throws<InvalidOperationException>(() =>
                EmberTemplateInheritanceEngine.BuildFrameworkDeclaration(source, "0.11.0"));
            StringAssert.Contains("先 bump 模板版本", error.Message);
        }

        [Test]
        public void EditingRecord_MatchingVersionAndHashShouldNotBeStale()
        {
            var template = CreateSchemaV2Template("content", "content");
            var record = new EditingTemplateRecord
            {
                templateId = template.id,
                templateVersion = template.version,
                contentHash = template.contentHash
            };

            Assert.IsFalse(EmberTemplateInheritanceEngine.IsEditingRecordStale(record, template));
        }

        [Test]
        public void EditingRecord_LegacyOrChangedStorageShouldBeStale()
        {
            var template = CreateSchemaV2Template("content", "content");
            var legacy = new EditingTemplateRecord { templateId = template.id };
            var oldVersion = new EditingTemplateRecord
            {
                templateId = template.id,
                templateVersion = "0.0.9",
                contentHash = template.contentHash
            };
            var oldContent = new EditingTemplateRecord
            {
                templateId = template.id,
                templateVersion = template.version,
                contentHash = "old-content"
            };

            Assert.IsTrue(EmberTemplateInheritanceEngine.IsEditingRecordStale(legacy, template));
            Assert.IsTrue(EmberTemplateInheritanceEngine.IsEditingRecordStale(oldVersion, template));
            Assert.IsTrue(EmberTemplateInheritanceEngine.IsEditingRecordStale(oldContent, template));
            Assert.IsTrue(EmberTemplateInheritanceEngine.IsEditingRecordStale(null, template));
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private string CreateAssetsDirectory(string name)
        {
            var path = Path.Combine(_testRoot, name, "Assets");
            Directory.CreateDirectory(path);
            return path;
        }

        private static string WriteFile(string root, string relativePath, string content)
        {
            var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, content);
            return path;
        }

        private static TemplateInfo CreateSchemaV2Template(
            string contentHash,
            string versionedContentHash)
        {
            return new TemplateInfo
            {
                schemaVersion = EmberTemplateInheritanceEngine.CurrentSchemaVersion,
                id = "platformer2d",
                displayName = "平台模板",
                description = "",
                version = "0.1.0",
                frameworkVersion = "0.10.0",
                channel = "preview",
                order = 2,
                parentId = string.Empty,
                parentVersion = string.Empty,
                parentContentHash = string.Empty,
                contentHash = contentHash,
                versionedContentHash = versionedContentHash
            };
        }

        #endregion
    }
}
