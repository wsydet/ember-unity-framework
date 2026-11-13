using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ember.Core.Editor;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Ember.UI.Tests
{
    /// <summary>只读真实身份；可写输入放入独立 Temp，不切换模板或签收项目配置。</summary>
    public sealed class EmberEditorIdentityCacheEditTests
    {
        #region 内部参数
        private string _root;
        private readonly List<EmberExtensionDefinition> _definitions = new();
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [SetUp] public void Setup()
        {
            _root = Path.GetFullPath("Temp/EmberIdentityCache-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            EmberEditorMetadataCache.Invalidate();
        }

        [TearDown] public void Teardown()
        {
            foreach (var definition in _definitions) EmberExtensionProject.Unregister(definition);
            _definitions.Clear();
            EmberEditorMetadataCache.Invalidate();
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        [Test] public void WarmChecksReuseMetadataAndBindingsWhileReturningIndependentCopies()
        {
            EmberExtensionProject.Resolve();
            var templates = EmberProjectSetup.GetTemplates();
            var config = EmberExtensionProject.Load();
            int reads = EmberEditorMetadataCache.JsonReads, scans = EmberEditorMetadataCache.TemplateScans;
            int builds = EmberExtensionProject.ResolutionBuilds, hashes = EmberExtensionProject.ContentHashBuilds;
            var id = EmberFsmBlueprintAccess.CurrentTemplateId;
            for (int i = 0; i < 100; i++)
            {
                using var batch = EmberExtensionProject.BeginCheckBatch();
                EmberExtensionProject.IsEnabled(EmberExtensionProject.LevelTrial);
                EmberExtensionProject.IsEnabled(EmberExtensionProject.FsmBlueprint);
                Assert.AreEqual(id, EmberFsmBlueprintAccess.CurrentTemplateId);
            }
            Assert.AreEqual(reads, EmberEditorMetadataCache.JsonReads);
            Assert.AreEqual(scans, EmberEditorMetadataCache.TemplateScans);
            Assert.AreEqual(builds, EmberExtensionProject.ResolutionBuilds);
            Assert.AreEqual(hashes, EmberExtensionProject.ContentHashBuilds);
            string firstId = templates[0].id;
            templates[0].id = "tampered"; templates.Clear();
            Assert.AreEqual(firstId, EmberProjectSetup.GetTemplates()[0].id);
            var original = EmberExtensionProject.Load();
            config.ownerTemplateId = "tampered";
            if (config.enabled.Length > 0) config.enabled[0] = "tampered";
            if (config.bindings?.Length > 0) config.bindings[0].contentHash = "tampered";
            var after = EmberExtensionProject.Load();
            Assert.AreEqual(original.ownerTemplateId, after.ownerTemplateId);
            CollectionAssert.AreEqual(original.enabled, after.enabled);
            if (after.bindings?.Length > 0) Assert.AreEqual(original.bindings[0].contentHash, after.bindings[0].contentHash);
            var result = EmberExtensionProject.Resolve();
            Assert.Throws<NotSupportedException>(() => ((IList<string>)result.Enabled).Add("tampered"));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string>)result.Problems).Add("tampered", "true"));
        }

        [UnityTest] public IEnumerator ExternalJsonChangesAndCorruptionAreDetectedWithinOneProbe()
        {
            string path = Path.Combine(_root, "configuration.json");
            Assert.IsNull(EmberEditorMetadataCache.ReadJson<EmberExtensionProject.Configuration>(path));
            File.WriteAllText(path, "{\"schemaVersion\":2,\"enabled\":[\"first\"]}");
            EmberExtensionProject.ValidateCurrent();
            Assert.AreEqual("first", EmberEditorMetadataCache.ReadJson<EmberExtensionProject.Configuration>(path).enabled[0]);
            File.WriteAllText(path, "{\"schemaVersion\":2,\"enabled\":[\"second-longer\"]}");
            double deadline = UnityEditor.EditorApplication.timeSinceStartup + 1.1;
            while (UnityEditor.EditorApplication.timeSinceStartup < deadline) yield return null;
            Assert.AreEqual("second-longer", EmberEditorMetadataCache.ReadJson<EmberExtensionProject.Configuration>(path).enabled[0]);
            File.WriteAllText(path, "broken-json");
            EmberExtensionProject.ValidateCurrent();
            Assert.Throws<InvalidDataException>(() => EmberEditorMetadataCache.ReadJson<EmberExtensionProject.Configuration>(path));
            File.Delete(path); EmberExtensionProject.ValidateCurrent();
            Assert.IsNull(EmberEditorMetadataCache.ReadJson<EmberExtensionProject.Configuration>(path));
        }

        [Test] public void DynamicCompatibilityRegistrationAndBatchInvalidationNeverFreezeTrue()
        {
            bool available = true;
            int checks = 0;
            var definition = new EmberExtensionDefinition("test.identity-cache", "fixture", EmberExtensionKind.Infrastructure,
                checkCompatibility: () => { checks++; return available ? null : "disabled"; });
            _definitions.Add(definition); EmberExtensionProject.Register(definition);
            using (EmberExtensionProject.BeginCheckBatch())
            {
                Assert.IsTrue(EmberExtensionProject.IsEnabled(definition.Id));
                int once = checks;
                Assert.IsTrue(EmberExtensionProject.IsEnabled(definition.Id));
                Assert.AreEqual(once, checks);
                available = false;
                EmberExtensionProject.InvalidateChecks();
                Assert.IsFalse(EmberExtensionProject.IsEnabled(definition.Id));
                available = true;
                EmberExtensionProject.ValidateCurrent();
                Assert.IsTrue(EmberExtensionProject.IsEnabled(definition.Id));
                EmberExtensionProject.Unregister(definition);
                Assert.IsFalse(EmberExtensionProject.IsEnabled(definition.Id));
            }
        }

        [Test] public void ExplicitConfigurationRequiresMatchingIdentityBindingsAndCurrentConditions()
        {
            bool enabled = true;
            var definition = new EmberExtensionDefinition("test.bound-cache", "fixture", EmberExtensionKind.EditorExtension,
                checkCompatibility: () => enabled ? null : "disabled");
            _definitions.Add(definition); EmberExtensionProject.Register(definition);
            var config = EmberExtensionProject.CreateConfiguration(new[] { definition.Id });
            string id = EmberFsmBlueprintAccess.CurrentTemplateId, version = EmberExtensionProject.CurrentTemplateVersion;
            Assert.IsTrue(EmberExtensionProject.ResolveConfiguration(config, id, version).IsEnabled(definition.Id));
            enabled = false;
            Assert.IsFalse(EmberExtensionProject.ResolveConfiguration(config, id, version).IsEnabled(definition.Id));
            enabled = true;
            config.enabled = Array.Empty<string>();
            Assert.IsFalse(EmberExtensionProject.ResolveConfiguration(config, id, version).IsEnabled(definition.Id));
            config.enabled = new[] { definition.Id }; config.bindings.Single(x => x.id == definition.Id).contentHash = "unaccepted";
            Assert.IsFalse(EmberExtensionProject.ResolveConfiguration(config, id, version).IsEnabled(definition.Id));
            Assert.IsFalse(EmberExtensionProject.ResolveConfiguration(config, "other", version).IsEnabled(definition.Id));
            Assert.IsFalse(EmberExtensionProject.ResolveConfiguration(config, null, null).IsEnabled(definition.Id));
        }

        [Test] public void BrokenParentChainCannotAuthorizeEvenItsOwnIdentity()
        {
            var templates = new List<TemplateInfo> { new() { id = "child", parentId = "missing" } };
            Assert.IsFalse(EmberProjectSetup.IsTemplateActiveForIdentity("child", true, "child", "base", templates));
            templates.Add(new TemplateInfo { id = "missing", parentId = "child" });
            Assert.IsFalse(EmberProjectSetup.IsTemplateActiveForIdentity("child", false, "base", "child", templates));
            templates[1].parentId = null;
            Assert.IsTrue(EmberProjectSetup.IsTemplateActiveForIdentity("child", false, "base", "child", templates));
        }

        [Test] public void DisposingOrDisablingModuleScopeImmediatelyRevokesTheNextCheck()
        {
            bool enabled = true;
            string id = "test.cache-scope";
            using var batch = EmberExtensionProject.BeginCheckBatch();
            var scope = EmberExtensionDocuments.RegisterModuleScope(id, EmberFsmBlueprintAccess.CurrentTemplateId, () => enabled);
            try
            {
                Assert.IsTrue(EmberExtensionDocuments.IsScopeEnabled(id));
                enabled = false;
                Assert.IsFalse(EmberExtensionDocuments.IsScopeEnabled(id));
                enabled = true;
                Assert.IsTrue(EmberExtensionDocuments.IsScopeEnabled(id));
                scope.Dispose();
                Assert.IsFalse(EmberExtensionDocuments.IsScopeEnabled(id));
            }
            finally { scope.Dispose(); }
        }

        [Test] public void TransactionCommitInvalidatesCachedInputImmediately()
        {
            string destination = Path.Combine(_root, "record.json");
            string stage = Path.Combine(_root, "stage.json");
            File.WriteAllText(destination, "{\"templateId\":\"before\"}");
            Assert.AreEqual("before", EmberEditorMetadataCache.ReadJson<EditingTemplateRecord>(destination).templateId);
            File.WriteAllText(stage, "{\"templateId\":\"after\"}");
            EmberTemplateTransaction.CommitPreparedTargets(new[] { new TemplateTransactionTarget(stage, destination) });
            Assert.AreEqual("after", EmberEditorMetadataCache.ReadJson<EditingTemplateRecord>(destination).templateId);
        }

        [UnityTest] public IEnumerator PackageSourceNotificationsInvalidatePreviouslyAcceptedContentHash()
        {
            string relative = "Tests/.IdentityCacheFixture-" + Guid.NewGuid().ToString("N") + "~";
            string folder = Path.Combine(EmberEditorMetadataCache.Package.resolvedPath, relative);
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "source.txt");
            try
            {
                File.WriteAllText(path, "accepted source");
                double deadline = UnityEditor.EditorApplication.timeSinceStartup + 1.1;
                while (UnityEditor.EditorApplication.timeSinceStartup < deadline) yield return null;
                var definition = new EmberExtensionDefinition("test.source-cache", "fixture", EmberExtensionKind.EditorExtension,
                    sourceRoots: new[] { relative });
                _definitions.Add(definition); EmberExtensionProject.Register(definition);
                var config = EmberExtensionProject.CreateConfiguration(new[] { definition.Id });
                string id = EmberFsmBlueprintAccess.CurrentTemplateId, version = EmberExtensionProject.CurrentTemplateVersion;
                Assert.IsTrue(EmberExtensionProject.ResolveConfiguration(config, id, version).IsEnabled(definition.Id));
                int hashes = EmberExtensionProject.ContentHashBuilds;
                File.WriteAllText(path, "changed source after acceptance");
                deadline = UnityEditor.EditorApplication.timeSinceStartup + 1.1;
                while (UnityEditor.EditorApplication.timeSinceStartup < deadline) yield return null;
                Assert.IsFalse(EmberExtensionProject.ResolveConfiguration(config, id, version).IsEnabled(definition.Id));
                Assert.Greater(EmberExtensionProject.ContentHashBuilds, hashes);
            }
            finally { Directory.Delete(folder, true); }
        }
        #endregion
    }
}
