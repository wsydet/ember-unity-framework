using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Ember.Table.Editor.Tests
{
    public sealed class EmberExternalSourceTests
    {
        private const string ROOT = ".utmp/外部源表 测试";
        private const string FILE = ROOT + "/editor_test.etable.csv";
        private byte[] _config;
        private EmberTableDefinition _definition;

        [SetUp] public void SetUp()
        {
            _config = File.Exists(EmberTableSourceFiles.CONFIG_PATH) ? File.ReadAllBytes(EmberTableSourceFiles.CONFIG_PATH) : null;
            File.WriteAllText(EmberTableSourceFiles.CONFIG_PATH, JsonUtility.ToJson(new EmberTableSourceFiles.Configuration
            { allowedRoots = new[] { ROOT }, defaultRoot = ROOT }));
            Directory.CreateDirectory(ROOT);
            File.Copy("Packages/com.ember/Table/Tests/Editor/editor-test.etable.csv", FILE, true);
            _definition = ScriptableObject.CreateInstance<EmberTableDefinition>();
            var serialized = new SerializedObject(_definition);
            serialized.FindProperty("_tableId").stringValue = "editor_test";
            serialized.FindProperty("_source").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>("Packages/com.ember/Table/Tests/Editor/editor-test.etable.csv");
            serialized.FindProperty("_rowScript").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MonoScript>("Packages/com.ember/Table/Tests/Editor/EditorTableTestRow.cs");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        [TearDown] public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_definition);
            if (_config == null) File.Delete(EmberTableSourceFiles.CONFIG_PATH);
            else File.WriteAllBytes(EmberTableSourceFiles.CONFIG_PATH, _config);
            if (Directory.Exists(ROOT)) Directory.Delete(ROOT, true);
        }
        private void Bind(string path)
        {
            var serialized = new SerializedObject(_definition);
            serialized.FindProperty("_externalSourcePath").stringValue = path;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        [Test] public void LegacyAndExternalBakeIdenticalBytesAndResourceKey()
        {
            var legacy = EmberTableValidationService.Validate(_definition);
            Assert.That(legacy.Succeeded, Is.True, string.Join("\n", legacy.Diagnostics));
            Assert.That(EmberTableBaker.TryBake(legacy.Tables[0], out var before, out _), Is.True);
            string key = _definition.GetLogicalResourcePath();
            Bind(FILE);
            var external = EmberTableValidationService.Validate(_definition);
            Assert.That(external.Succeeded, Is.True, string.Join("\n", external.Diagnostics));
            Assert.That(EmberTableBaker.TryBake(external.Tables[0], out var after, out _), Is.True);
            Assert.That(after.FileBytes, Is.EqualTo(before.FileBytes));
            Assert.That(_definition.GetLogicalResourcePath(), Is.EqualTo(key));
        }
        [Test] public void ExternalMissingNeverFallsBackToLegacy()
        {
            Bind(ROOT + "/missing.csv");
            Assert.That(EmberTableValidationService.Validate(_definition).Succeeded, Is.False);
        }
        [TestCase("../escape.csv")]
        [TestCase(".utmp/外部源表 测试/../escape.csv")]
        [TestCase("C:/private.csv")]
        [TestCase(".utmp/外部源表 测试/file.txt")]
        public void BoundaryRejectsInvalidPaths(string path) => Assert.Throws<InvalidOperationException>(() => EmberTableSourceFiles.Resolve(path));
        [Test] public void ChineseSpaceWindowsSeparatorsAndRelocatedRelativeBinding()
        {
            string path = "../" + new DirectoryInfo(EmberTableSourceFiles.ProjectRoot).Name + "/" + FILE;
            Assert.That(EmberTableSourceFiles.ReadBytes(path.Replace('/', '\\')), Is.EqualTo(File.ReadAllBytes(FILE)));
        }
        [Test] public void DraftRejectsConcurrentChangesIncludingFormatting()
        {
            var draft = EmberTableSourceDocument.Load(FILE);
            draft.Rows[0].Values[0] = "changed";
            byte[] changed = Encoding.UTF8.GetBytes(File.ReadAllText(FILE).Replace("\n", "\r\n"));
            File.WriteAllBytes(FILE, changed);
            Assert.Throws<InvalidOperationException>(() => draft.Save());
            Assert.That(File.ReadAllBytes(FILE), Is.EqualTo(changed));
            Assert.That(draft.Dirty, Is.True);
        }
        [Test] public void ExternalEditSaveAndFailureRestore()
        {
            var draft = EmberTableSourceDocument.Load(FILE);
            draft.Rows[0].Values[0] = "changed";
            draft.Save();
            Assert.That(EmberTableSourceDocument.Load(FILE).Rows[0].Values[0], Is.EqualTo("changed"));
            byte[] before = File.ReadAllBytes(FILE);
            Bind(FILE);
            draft.Rows[0].Values[0] = "another";
            Assert.Throws<InvalidOperationException>(() => draft.Save(_definition)); // transient Definition is outside project catalog
            Assert.That(File.ReadAllBytes(FILE), Is.EqualTo(before));
            Assert.That(draft.Dirty, Is.True);
        }
        [TestCase("id,amount,enabled,optional,rank\nA,1,true,,Low\nA,2,true,,Low\n")]
        [TestCase("id,amount\nA,\"unterminated")]
        public void InvalidDataBlocksExport(string text)
        {
            Bind(FILE); File.WriteAllText(FILE, text);
            Assert.That(EmberTableValidationService.Validate(_definition).Succeeded, Is.False);
        }
        [Test] public void SharedMappingWinsLegacyAndRejectsConflictingOrDuplicateBindings()
        {
            var config = EmberTableSourceFiles.ReadConfiguration();
            config.bindings = new[] { new EmberTableSourceFiles.Binding { tableId = "editor_test", path = FILE } };
            File.WriteAllText(EmberTableSourceFiles.CONFIG_PATH, JsonUtility.ToJson(config));
            Assert.That(_definition.SourcePath, Is.EqualTo(FILE));
            Assert.That(EmberTableValidationService.Validate(_definition).Succeeded, Is.True);
            Bind(ROOT + "/other.csv");
            Assert.That(EmberTableValidationService.Validate(_definition).Succeeded, Is.False);
            Bind("");
            config.bindings = config.bindings.Concat(config.bindings).ToArray();
            File.WriteAllText(EmberTableSourceFiles.CONFIG_PATH, JsonUtility.ToJson(config));
            Assert.That(EmberTableValidationService.Validate(_definition).Succeeded, Is.False);
        }
        [Test] public void ReadOnlySaveLeavesOriginalAndDraftIntact()
        {
            var document = EmberTableSourceDocument.Load(FILE);
            byte[] original = File.ReadAllBytes(FILE);
            document.Rows[0].Values[0] = "42";
            File.SetAttributes(FILE, File.GetAttributes(FILE) | FileAttributes.ReadOnly);
            try
            {
                Assert.Throws<UnauthorizedAccessException>(() => document.Save());
                Assert.That(File.ReadAllBytes(FILE), Is.EqualTo(original));
                Assert.That(document.Dirty, Is.True);
                Assert.That(Directory.GetFiles(ROOT, "*.tmp"), Is.Empty);
            }
            finally { File.SetAttributes(FILE, FileAttributes.Normal); }
        }
        [Test] public void CachedValidationDoesNotHideExternalChanges()
        {
            Bind(FILE);
            var validation = EmberTableValidationService.Validate(_definition);
            Assert.That(validation.Succeeded, Is.True);
            File.AppendAllText(FILE, "\n");
            Assert.That(EmberTablePipeline.IsArtifactCurrent(validation.Tables[0], out string reason), Is.False);
            Assert.That(reason, Does.Contain("Source changed"));
        }
    }
}
