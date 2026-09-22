using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Ember.UPMManager.Editor.Tests
{
    public class EmberAISkillInstallerEditTests
    {
        private string _root;
        private string _source;
        private string _project;
        private const string Id = "ember-test-skill";
        private const string Commit = "0123456789012345678901234567890123456789";

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "EmberSkillTest-" + Guid.NewGuid().ToString("N"));
            _source = Path.Combine(_root, "source"); _project = Path.Combine(_root, "project");
            Write(Path.Combine(_source, Id, "SKILL.md"), "---\nname: " + Id + "\ndescription: test\n---\nVersion one\n");
            Write(Path.Combine(_source, "catalog.json"), JsonUtility.ToJson(new EmberAISkillInstaller.Catalog
            {
                skills = new[] { new EmberAISkillInstaller.Definition { id = Id, displayName = "测试技能" } }
            }));
            Directory.CreateDirectory(_project);
        }

        [TearDown]
        public void TearDown() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }

        private static void Write(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, text);
        }

        private string Target(string file) => Path.Combine(_project, ".agents/skills", Id, file);
        private EmberAISkillInstaller.Preview Preview() =>
            EmberAISkillInstaller.Inspect(_project, EmberAISkillInstaller.ReadCatalog(_source)[0]);
        private string Install(EmberAISkillInstaller.Preview preview, bool overwrite = false) =>
            EmberAISkillInstaller.Install(_project, preview, "https://example.invalid/framework.git", "main", Commit, overwrite);

        [Test]
        public void Install_RecordsSourceAndPreservesOtherSkillsAndProjectConfiguration()
        {
            string personal = Path.Combine(_project, ".agents/skills/personal/SKILL.md");
            string manifest = Path.Combine(_project, "Packages/manifest.json");
            Write(personal, "personal"); Write(manifest, "unchanged");
            Assert.AreEqual(EmberAISkillInstaller.Status.Missing, Preview().Status);
            Assert.IsNull(Install(Preview()));
            Assert.AreEqual(EmberAISkillInstaller.Status.Current, Preview().Status);
            Assert.AreEqual(Commit, Preview().InstalledCommit);
            Assert.AreEqual("personal", File.ReadAllText(personal));
            Assert.AreEqual("unchanged", File.ReadAllText(manifest));
        }

        [Test]
        public void Update_RemovesObsoleteManagedFilesAndKeepsCompleteBackup()
        {
            Write(Path.Combine(_source, Id, "obsolete.txt"), "old");
            Install(Preview());
            File.Delete(Path.Combine(_source, Id, "obsolete.txt"));
            File.AppendAllText(Path.Combine(_source, Id, "SKILL.md"), "new version");
            Assert.AreEqual(EmberAISkillInstaller.Status.UpdateAvailable, Preview().Status);
            string backup = Install(Preview());
            Assert.AreEqual("old", File.ReadAllText(Path.Combine(backup, "obsolete.txt")));
            Assert.IsFalse(File.Exists(Target("obsolete.txt")));
            StringAssert.EndsWith("new version", File.ReadAllText(Target("SKILL.md")));
        }

        [TestCase("edit")]
        [TestCase("add")]
        [TestCase("delete")]
        public void LocalChanges_RequireExplicitOverwriteAndAreBackedUp(string kind)
        {
            Install(Preview());
            if (kind == "edit") File.AppendAllText(Target("SKILL.md"), "my changes");
            if (kind == "add") Write(Target("my-notes.txt"), "personal notes");
            if (kind == "delete") File.Delete(Target("SKILL.md"));
            var preview = Preview();
            Assert.AreEqual(EmberAISkillInstaller.Status.LocalChanges, preview.Status);
            Assert.Throws<InvalidOperationException>(() => Install(preview));
            string backup = Install(preview, true);
            Assert.IsTrue(Directory.Exists(backup));
            if (kind == "edit") StringAssert.EndsWith("my changes", File.ReadAllText(Path.Combine(backup, "SKILL.md")));
            if (kind == "add") Assert.AreEqual("personal notes", File.ReadAllText(Path.Combine(backup, "my-notes.txt")));
            if (kind == "delete") Assert.IsFalse(File.Exists(Path.Combine(backup, "SKILL.md")));
        }

        [Test]
        public void UnmanagedOrStalePreview_CannotSilentlyReplaceLocalWork()
        {
            Write(Target("SKILL.md"), "existing skill");
            var preview = Preview();
            Assert.AreEqual(EmberAISkillInstaller.Status.Unmanaged, preview.Status);
            Assert.Throws<InvalidOperationException>(() => Install(preview));
            File.AppendAllText(Target("SKILL.md"), " changed after preview");
            Assert.Throws<IOException>(() => Install(preview, true));
            StringAssert.EndsWith("changed after preview", File.ReadAllText(Target("SKILL.md")));
        }

        [Test]
        public void StateWriteFailure_RestoresOriginalSkillDirectory()
        {
            Write(Target("SKILL.md"), "original");
            // A directory at the state-file path makes the final rename fail after skill replacement.
            Directory.CreateDirectory(Path.Combine(_project, EmberAISkillInstaller.StateFile));
            Assert.Throws<IOException>(() => Install(Preview(), true));
            Assert.AreEqual("original", File.ReadAllText(Target("SKILL.md")));
        }

        [Test]
        public void CorruptedStateAndChangedDownload_AreRejectedWithoutInstalling()
        {
            var preview = Preview();
            File.AppendAllText(Path.Combine(_source, Id, "SKILL.md"), "changed download");
            Assert.Throws<IOException>(() => Install(preview));
            Assert.IsFalse(File.Exists(Target("SKILL.md")));
            Write(Path.Combine(_project, EmberAISkillInstaller.StateFile), "{\"schemaVersion\":99}");
            Assert.Throws<InvalidDataException>(() => Preview());
        }

        [TestCase("../outside")]
        [TestCase("Bad-Name")]
        [TestCase("skill/child")]
        public void Catalog_RejectsInvalidDirectories(string id)
        {
            Write(Path.Combine(_source, "catalog.json"), JsonUtility.ToJson(new EmberAISkillInstaller.Catalog
            { skills = new[] { new EmberAISkillInstaller.Definition { id = id } } }));
            Assert.Throws<InvalidDataException>(() => EmberAISkillInstaller.ReadCatalog(_source));
        }

        [Test]
        public void CapabilitiesAndSourceProject_ProtectOldFrameworksAndCanonicalSource()
        {
            var definition = new EmberAISkillInstaller.Definition
            { minimumFrameworkVersion = "0.12.5", requiredCapability = "eui-regenerate-v1" };
            Assert.IsNotNull(EmberAISkillInstaller.Incompatibility(definition, "0.12.11", false));
            Assert.IsNull(EmberAISkillInstaller.Incompatibility(definition, "0.12.11", true));
            Assert.IsNotNull(EmberAISkillInstaller.Incompatibility(definition, "0.12.4", true));
            Write(Path.Combine(_project, "Packages/com.ember/package.json"), "{}");
            Write(Path.Combine(_project, ".agents/skills/catalog.json"), "{}");
            Assert.Throws<InvalidOperationException>(() => Install(Preview()));
        }

        [Test]
        public void TemplateCatalog_CannotEnterGenericDownloadOrInstallPath()
        {
            Write(Path.Combine(_source, "catalog.json"), JsonUtility.ToJson(new EmberAISkillInstaller.Catalog
            { schemaVersion = 2, skills = new[] { new EmberAISkillInstaller.Definition
                { id = Id, templateId = "fixture", minimumTemplateVersion = "0.1.0", minimumFrameworkVersion = "0.13.2" } } }));
            Assert.Throws<InvalidDataException>(() => EmberAISkillInstaller.ReadCatalog(_source));
            var package = EmberAISkillInstaller.ReadCatalog(_source, true)[0];
            Assert.Throws<InvalidOperationException>(() => Install(EmberAISkillInstaller.Inspect(_project, package)));
        }

        [Test]
        public void V1State_RemainsGeneric_AndTemplateCollisionDoesNotClaimIt()
        {
            Install(Preview());
            string before = File.ReadAllText(Path.Combine(_project, EmberAISkillInstaller.StateFile));
            var assets = Path.Combine(_root, "assets");
            var source = Path.Combine(assets, EmberAISkillInstaller.TemplateSourceDirectory);
            Write(Path.Combine(source, Id, "SKILL.md"), File.ReadAllText(Target("SKILL.md")));
            Write(Path.Combine(source, "catalog.json"), JsonUtility.ToJson(new EmberAISkillInstaller.Catalog
            { schemaVersion = 2, skills = new[] { new EmberAISkillInstaller.Definition
                { id = Id, templateId = "fixture", minimumTemplateVersion = "0.1.0", minimumFrameworkVersion = "0.13.2" } } }));
            var plan = EmberAISkillInstaller.PreviewTemplateSkills(_project, assets,
                new EmberAISkillInstaller.TemplateContext("fixture", "1.0.0", "fixture-hash", null, "0.13.2", true));
            Assert.IsNotEmpty(plan.Errors);
            Assert.Throws<InvalidOperationException>(() => EmberAISkillInstaller.PrepareTemplateSkills(plan, true, out _));
            Assert.AreEqual(before, File.ReadAllText(Path.Combine(_project, EmberAISkillInstaller.StateFile)));
        }

        [TestCase(1)]
        [TestCase(99)]
        public void TemplateCatalog_RequiresExplicitSupportedSchema(int schema)
        {
            Write(Path.Combine(_source, "catalog.json"), "{\"schemaVersion\":" + schema + ",\"skills\":[]}");
            Assert.Throws<InvalidDataException>(() => EmberAISkillInstaller.ReadCatalog(_source, true));
        }

        [Test]
        public void TemplateCatalog_RejectsDuplicateIdsAndInvalidOwner()
        {
            var definition = new EmberAISkillInstaller.Definition
            { id = Id, templateId = "fixture", minimumTemplateVersion = "0.1.0", minimumFrameworkVersion = "0.13.2" };
            Write(Path.Combine(_source, "catalog.json"), JsonUtility.ToJson(new EmberAISkillInstaller.Catalog
            { schemaVersion = 2, skills = new[] { definition, definition } }));
            Assert.Throws<InvalidDataException>(() => EmberAISkillInstaller.ReadCatalog(_source, true));
            definition.templateId = "../outside";
            Write(Path.Combine(_source, "catalog.json"), JsonUtility.ToJson(new EmberAISkillInstaller.Catalog
            { schemaVersion = 2, skills = new[] { definition } }));
            Assert.Throws<InvalidDataException>(() => EmberAISkillInstaller.ReadCatalog(_source, true));
        }

        [Test]
        public void BundledFrameworkSkills_AutomaticFirstInstall_PreservesEditsAndExplicitRemoval()
        {
            string bundle = BuildBundle();
            EmberAISkillInstaller.InstallBundledMissing(_project, bundle, "0.13.2", true);
            Assert.IsTrue(File.Exists(Target("SKILL.md")));
            Assert.AreEqual(Commit, Preview().InstalledCommit);
            File.AppendAllText(Target("SKILL.md"), "my local edit");
            string record = File.ReadAllText(Path.Combine(_project, EmberAISkillInstaller.StateFile));
            EmberAISkillInstaller.InstallBundledMissing(_project, bundle, "0.13.2", true);
            StringAssert.Contains("my local edit", File.ReadAllText(Target("SKILL.md")));
            Assert.AreEqual(record, File.ReadAllText(Path.Combine(_project, EmberAISkillInstaller.StateFile)));
            Directory.Delete(Path.GetDirectoryName(Target("SKILL.md")), true);
            EmberAISkillInstaller.InstallBundledMissing(_project, bundle, "0.13.2", true);
            Assert.IsFalse(File.Exists(Target("SKILL.md"))); // Prior ownership makes deletion an explicit state, not first install.
        }

        [Test]
        public void BundledFrameworkSkills_ProtectUnmanagedDirectory_AndRejectChangedBundle()
        {
            string bundle = BuildBundle();
            Write(Target("SKILL.md"), "manual skill");
            EmberAISkillInstaller.InstallBundledMissing(_project, bundle, "0.13.2", true);
            Assert.AreEqual("manual skill", File.ReadAllText(Target("SKILL.md")));
            File.AppendAllText(Path.Combine(bundle, "skills", Id, "SKILL.md"), "tampered");
            Assert.Throws<InvalidDataException>(() => EmberAISkillInstaller.InstallBundledMissing(_project, bundle, "0.13.2", true));
        }

        private string BuildBundle()
        {
            string bundle = Path.Combine(_root, "bundle");
            var files = new System.Collections.Generic.List<EmberAISkillInstaller.Fingerprint>();
            foreach (string source in Directory.GetFiles(_source, "*", SearchOption.AllDirectories))
            {
                string relative = source.Substring(_source.Length + 1).Replace('\\', '/');
                Write(Path.Combine(bundle, "skills", relative), File.ReadAllText(source));
                using var algorithm = System.Security.Cryptography.SHA256.Create();
                files.Add(new EmberAISkillInstaller.Fingerprint { path = relative,
                    sha256 = BitConverter.ToString(algorithm.ComputeHash(File.ReadAllBytes(source))).Replace("-", "").ToLowerInvariant() });
            }
            Write(Path.Combine(bundle, "bundle.json"), JsonUtility.ToJson(new EmberAISkillInstaller.BundleManifest
            { schemaVersion = 1, generatedBy = "Ember AI Skill Bundle", sourceCommit = Commit, files = files.ToArray() }));
            return bundle;
        }
    }
}
