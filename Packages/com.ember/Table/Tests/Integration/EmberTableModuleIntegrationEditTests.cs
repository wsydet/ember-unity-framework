// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;

using Ember.Core;
using Ember.Resource;
using Ember.Table.Integration;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ember.Table.Integration.Tests
{
    public sealed class EmberTableModuleIntegrationEditTests
    {
        [SetUp]
        public void SetUp()
        {
            TestTableModule.Destroy();
            EmberResourceManager.Destroy();
        }

        [TearDown]
        public void TearDown()
        {
            TestTableModule.Destroy();
            EmberResourceManager.Destroy();
        }

        [Test]
        public void ModuleOwnsEngineAndLoadsAfterResourceManager()
        {
            var binding = new TestBinding("settings");
            TestTableModule.Catalog = new EmberTableCatalog(
                new[] { new EmberTableCatalogEntry(binding) });
            var provider = new FakeProvider
            {
                Files = { [binding.ResourcePath] = BuildFile(binding, "ready") },
            };
            EmberResourceManager.Instance.Initialize(provider);

            var module = (IEmberModule)TestTableModule.Instance;
            module.OnInit();

            Assert.That(TestTableModule.Instance.IsReady, Is.True);
            Assert.That(TestTableModule.Instance.LastLoadResult.Succeeded, Is.True);
            Assert.That(TestTableModule.Instance.Database.TryGetTable("settings", out EmberTable<TestRow> table), Is.True);
            Assert.That(table.TryGet("ready", out _), Is.True);

            module.OnDestroy();
            Assert.That(TestTableModule.Instance.IsReady, Is.False);
            Assert.That(TestTableModule.Instance.Database.Count, Is.Zero);
            module.OnDestroy();
        }

        [Test]
        public void RequiredFailurePreservesResultAndNeverReportsReady()
        {
            var binding = new TestBinding("missing");
            TestTableModule.Catalog = new EmberTableCatalog(
                new[] { new EmberTableCatalogEntry(binding) });
            EmberResourceManager.Instance.Initialize(new FakeProvider());

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => ((IEmberModule)TestTableModule.Instance).OnInit());

            Assert.That(exception.Message, Does.Contain("Required"));
            Assert.That(TestTableModule.Instance.IsReady, Is.False);
            Assert.That(TestTableModule.Instance.Database.Count, Is.Zero);
            Assert.That(TestTableModule.Instance.LastLoadResult, Is.Not.Null);
            Assert.That(TestTableModule.Instance.LastLoadResult.Succeeded, Is.False);
        }

        [Test]
        public void ResetAndHotRestartUseOneIdempotentShutdownPath()
        {
            var binding = new TestBinding("settings");
            TestTableModule.Catalog = new EmberTableCatalog(
                new[] { new EmberTableCatalogEntry(binding) });
            var provider = new FakeProvider
            {
                Files = { [binding.ResourcePath] = BuildFile(binding, "first") },
            };
            EmberResourceManager.Instance.Initialize(provider);
            var module = (IEmberModule)TestTableModule.Instance;
            module.OnInit();
            module.OnDestroy();
            module.ResetModuleData();
            provider.Files[binding.ResourcePath] = BuildFile(binding, "second");

            module.OnInit();

            Assert.That(TestTableModule.Instance.IsReady, Is.True);
            Assert.That(TestTableModule.Instance.Database.TryGetTable("settings", out EmberTable<TestRow> table), Is.True);
            Assert.That(table.ContainsKey("first"), Is.False);
            Assert.That(table.ContainsKey("second"), Is.True);
        }

        private static byte[] BuildFile(TestBinding binding, string key)
        {
            var writer = new EmberTableBinaryWriter();
            writer.WriteString(key);
            return EmberTableBinaryFormat.BuildFile(
                binding.TableId,
                binding.RowTypeId,
                new byte[32],
                new byte[32],
                1,
                writer.ToArray());
        }

        [EmberModule(ModulePhase.Global, Enabled = false)]
        private sealed class TestTableModule : EmberTableModuleBase<TestTableModule>
        {
            public static IEmberTableCatalog Catalog { private get; set; } = EmberTableCatalog.Empty;

            public TestTableModule()
            {
            }

            protected override IEmberTableCatalog CreateTableCatalog()
            {
                return Catalog;
            }
        }

        private sealed class TestBinding : EmberTableBinding<TestRow>
        {
            public TestBinding(string tableId)
                : base(tableId, "Ember.Table.Integration.Tests:TestRow", "Config/Tables/" + tableId, new string('0', 64))
            {
            }

            protected override TestRow ReadRow(EmberTableBinaryReader reader)
            {
                return new TestRow(reader.ReadString());
            }

            protected override string GetPrimaryKey(TestRow row)
            {
                return row.Id;
            }
        }

        private sealed class TestRow
        {
            public string Id { get; }

            public TestRow(string id)
            {
                Id = id;
            }
        }

        private sealed class FakeProvider : IResourceProvider
        {
            public Dictionary<string, byte[]> Files { get; } = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            public float Progress => 1f;

            public void Initialize(Action<bool> onComplete) { onComplete?.Invoke(true); }
            public void LoadAssetAsync<T>(string path, Action<T> onComplete) where T : UnityEngine.Object { onComplete?.Invoke(null); }
            public AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Additive) { return null; }
            public void UnloadAsset(string path) { }
            public void UnloadUnusedAssets() { }
            public EmberAssetHandle<T> LoadAssetHandle<T>(string path) where T : UnityEngine.Object { throw new NotSupportedException(); }
            public EmberFileHandle LoadFileAsync(string path) { throw new NotSupportedException(); }
            public byte[] LoadFileSync(string path) { return Files.TryGetValue(path, out byte[] bytes) ? bytes : null; }
        }
    }
}
