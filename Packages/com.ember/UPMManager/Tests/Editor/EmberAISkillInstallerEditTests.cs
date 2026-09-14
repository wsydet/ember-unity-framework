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
    }
}
