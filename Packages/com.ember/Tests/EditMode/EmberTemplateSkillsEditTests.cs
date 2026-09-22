// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.IO;
using System.Linq;
using Ember.Core.Editor;
using Ember.UPMManager.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Ember.UI.Tests
{
    /// <summary>临时项目夹具使用生产部署/加载事务，不切换当前 Unity 项目的模板。</summary>
    public sealed class EmberTemplateSkillsEditTests
    {
        #region 内部参数
        private string _root, _project, _assets;
        private const string Skill = "ember-fixture";
        private const string Record = "Assets/Editor/EmberDeployedTemplates.json";
        #endregion

        #region 外部方法
        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "EmberTemplateSkills-" + Guid.NewGuid().ToString("N"));
            _project = Path.Combine(_root, "Project"); _assets = Path.Combine(_root, "Source", "Assets");
            Directory.CreateDirectory(_project);
            Source();
        }

        [TearDown]
        public void TearDown() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }

        [Test]
        public void FirstDeployment_Repeat_Update_Switch_PreserveUserContent()
        {
            Write(_project, ".agents/skills/personal/SKILL.md", "personal");
            Write(_project, "Assets/Art/keep.txt", "art");
            var first = Info();
            var preview = Preview(first);
            Assert.IsFalse(File.Exists(Target())); // Read-only preview does not enable an undeployed template.
            Assert.IsNotNull(EmberAISkillInstaller.GetTemplateSkillExecutionBlockReason(_project, Skill, null, null, null));
            Commit(first, preview, false);
            Assert.IsTrue(File.Exists(Target()));
            Assert.IsNull(EmberAISkillInstaller.GetTemplateSkillExecutionBlockReason(_project, Skill, first.id, first.version, first.contentHash));
            var repeat = Preview(first, first.id);
            Assert.IsFalse(repeat.HasChanges);
            Commit(first, repeat, false);
            Source(body: "updated");
            var updated = Info(version: "1.0.1");
            Commit(updated, Preview(updated, first.id));
            StringAssert.Contains("updated", File.ReadAllText(Target()));
            string empty = Path.Combine(_root, "Empty", "Assets"); Directory.CreateDirectory(empty);
            var next = Info("other", "1.0.0", empty);
            var switching = Preview(next, updated.id, empty);
            EmberProjectSetup.CommitTemplateDeployment(_project, empty, next, true, switching);
            Assert.IsFalse(Directory.Exists(Path.GetDirectoryName(Target())));
            Assert.AreEqual("other", JsonUtility.FromJson<DeployedTemplatesData>(File.ReadAllText(Path.Combine(_project, Record))).activeTemplateId);
            Assert.AreEqual("personal", File.ReadAllText(Path.Combine(_project, ".agents/skills/personal/SKILL.md")));
            Assert.AreEqual("art", File.ReadAllText(Path.Combine(_project, "Assets/Art/keep.txt")));
        }

        [Test]
        public void LocalChanges_BlockUntilConfirmed_AndBackupOutsideDiscovery()
        {
            var info = Info(); Commit(info, Preview(info));
            File.AppendAllText(Target(), "user change");
            var preview = Preview(info, info.id);
            Assert.IsTrue(preview.NeedsBackupConfirmation);
            Assert.Throws<InvalidOperationException>(() => EmberAISkillInstaller.PrepareTemplateSkills(preview, false, out _));
            var targets = EmberAISkillInstaller.PrepareTemplateSkills(preview, true, out string backup);
            StringAssert.Contains("user change", File.ReadAllText(Path.Combine(backup, Skill, "SKILL.md")));
            StringAssert.Contains(Path.Combine(".utmp", "ember-ai-skills"), backup);
            EmberTemplateTransaction.CommitPreparedTargets(targets.Select(t => new TemplateTransactionTarget(t.StagedPath, t.DestinationPath, t.Remove)).ToArray());
            Assert.IsFalse(File.ReadAllText(Target()).Contains("user change"));
        }

        [TestCase("skill")]
        [TestCase("catalog")]
        [TestCase("state")]
        [TestCase("identity")]
        [TestCase("local")]
        public void ChangesAfterPreview_RejectBeforeDeployment(string change)
        {
            var info = Info(); Commit(info, Preview(info));
            var preview = Preview(info, info.id);
            EmberAISkillInstaller.BindTemplateIdentityRecord(preview, Record);
            string path = change == "skill" ? Path.Combine(SourceRoot(), Skill, "SKILL.md")
                : change == "catalog" ? Path.Combine(SourceRoot(), "catalog.json")
                : change == "state" ? Path.Combine(_project, ".agents/ember-ai-skills.json")
                : change == "identity" ? Path.Combine(_project, Record) : Target();
            File.AppendAllText(path, " ");
            Assert.Throws<IOException>(() => EmberAISkillInstaller.PrepareTemplateSkills(preview, true, out _));
        }

        [TestCase(0)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void DeploymentOrSkillOrRecordFailure_RestoresEverything(int fault)
        {
            var old = Info(); Commit(old, Preview(old));
            string beforeState = File.ReadAllText(Path.Combine(_project, ".agents/ember-ai-skills.json"));
            string beforeRecord = File.ReadAllText(Path.Combine(_project, Record));
            string beforeSkill = File.ReadAllText(Target());
            string beforeAssets = EmberProjectSetup.ComputeTemplateContentHash(Path.Combine(_project, "Assets/Game"));
            Source(body: "new content");
            var next = Info(version: "1.0.1");
            Assert.Throws<IOException>(() => EmberProjectSetup.CommitTemplateDeployment(_project, _assets, next, true,
                Preview(next, old.id), i => { if (i == fault) throw new IOException("injected"); }));
            Assert.AreEqual(beforeState, File.ReadAllText(Path.Combine(_project, ".agents/ember-ai-skills.json")));
            Assert.AreEqual(beforeRecord, File.ReadAllText(Path.Combine(_project, Record)));
            Assert.AreEqual(beforeSkill, File.ReadAllText(Target()));
            Assert.AreEqual(beforeAssets, EmberProjectSetup.ComputeTemplateContentHash(Path.Combine(_project, "Assets/Game")));
        }

        [Test]
        public void FirstDeploymentFailure_DoesNotLeaveSkillsOrDeploymentRecord()
        {
            var info = Info();
            Assert.Throws<IOException>(() => EmberProjectSetup.CommitTemplateDeployment(_project, _assets, info, false,
                Preview(info), i => { if (i == 7) throw new IOException("record failure"); }));
            Assert.IsFalse(File.Exists(Target()));
            Assert.IsFalse(File.Exists(Path.Combine(_project, Record)));
            Assert.IsFalse(File.Exists(Path.Combine(_project, ".agents/ember-ai-skills.json")));
        }

        [Test]
        public void Load_UsesEditingRecord_AndRollsItBackWithSkills()
        {
            var info = Info();
            EmberProjectSetup.CommitTemplateDeployment(_project, _assets, info, true, Preview(info, editing: true), editing: true);
            string path = Path.Combine(_project, "Assets/Editor/EmberEditingTemplate.json");
            var record = JsonUtility.FromJson<EditingTemplateRecord>(File.ReadAllText(path));
            Assert.AreEqual(info.id, record.templateId);
            Assert.IsFalse(File.Exists(Path.Combine(_project, Record)));
            string before = File.ReadAllText(path);
            Source(body: "changed"); var next = Info(version: "1.1.0");
            Assert.Throws<IOException>(() => EmberProjectSetup.CommitTemplateDeployment(_project, _assets, next, true,
                Preview(next, info.id, editing: true), i => { if (i == 7) throw new IOException("injected"); }, true));
            Assert.AreEqual(before, File.ReadAllText(path));
            Assert.IsFalse(File.ReadAllText(Target()).Contains("changed"));
        }

        [Test]
        public void UnmanagedIdenticalDirectory_IsNotAdopted()
        {
            Write(_project, ".agents/skills/" + Skill + "/SKILL.md", File.ReadAllText(Path.Combine(SourceRoot(), Skill, "SKILL.md")));
            var info = Info(); var plan = Preview(info);
            Assert.IsNotEmpty(plan.Errors);
            Assert.Throws<InvalidOperationException>(() => Commit(info, plan));
        }

        [TestCase("99.0.0", "0.1.0")]
        [TestCase("0.1.0", "99.0.0")]
        public void MinimumVersions_Block(string framework, string template)
        {
            Source(minFramework: framework, minTemplate: template);
            Assert.IsNotEmpty(Preview(Info()).Errors);
        }

        [Test]
        public void InheritedMaterializedSkill_UsesChildOwnership_NotLatestParent()
        {
            string child = Path.Combine(_root, "Child", "Assets");
            EmberTemplateTransaction.CopyDirectory(_assets, child);
            Source(body: "latest parent not synchronized");
            var info = Info("child", "1.0.0", child);
            EmberProjectSetup.CommitTemplateDeployment(_project, child, info, true, Preview(info, null, child));
            Assert.IsFalse(File.ReadAllText(Target()).Contains("latest parent"));
            Assert.IsNull(EmberAISkillInstaller.GetTemplateSkillExecutionBlockReason(_project, Skill, "child", info.version, info.contentHash));
            StringAssert.Contains("\"originTemplateId\": \"fixture\"", File.ReadAllText(Path.Combine(_project, ".agents/ember-ai-skills.json")));
        }

        [Test]
        public void TemplateContentHash_IncludesSkillSource_AndSaveDoesNotSeal()
        {
            var old = Info(); Source(body: "draft");
            string hash = EmberProjectSetup.ComputeTemplateContentHash(_assets);
            var saved = EmberTemplateInheritanceEngine.BuildSavedMetadata(old, "fixture", "", hash);
            Assert.AreNotEqual(old.contentHash, saved.contentHash);
            Assert.AreEqual(old.versionedContentHash, saved.versionedContentHash);
            var bumped = EmberTemplateInheritanceEngine.BuildBumpedMetadata(saved, 2);
            Assert.AreEqual(bumped.contentHash, bumped.versionedContentHash);
        }

        [Test]
        public void TwoDerivedTemplates_InheritTwoBaseSkills_AndKeepTheirOwnSkill()
        {
            AddFixtureSkill(_assets, "ember-base-second", "fixture");
            string a = Path.Combine(_root, "DerivedA", "Assets"), b = Path.Combine(_root, "DerivedB", "Assets");
            EmberTemplateTransaction.CopyDirectory(_assets, a); EmberTemplateTransaction.CopyDirectory(_assets, b);
            AddFixtureSkill(a, "ember-child-a", "child-a"); AddFixtureSkill(b, "ember-child-b", "child-b");
            var infoA = Info("child-a", assets: a);
            EmberProjectSetup.CommitTemplateDeployment(_project, a, infoA, true, Preview(infoA, assets: a));
            Assert.IsTrue(File.Exists(Target()));
            Assert.IsTrue(File.Exists(Path.Combine(_project, ".agents/skills/ember-base-second/SKILL.md")));
            Assert.IsTrue(File.Exists(Path.Combine(_project, ".agents/skills/ember-child-a/SKILL.md")));
            var infoB = Info("child-b", assets: b);
            EmberProjectSetup.CommitTemplateDeployment(_project, b, infoB, true, Preview(infoB, "child-a", b));
            Assert.IsTrue(File.Exists(Target()));
            Assert.IsTrue(File.Exists(Path.Combine(_project, ".agents/skills/ember-base-second/SKILL.md")));
            Assert.IsFalse(Directory.Exists(Path.Combine(_project, ".agents/skills/ember-child-a")));
            Assert.IsTrue(File.Exists(Path.Combine(_project, ".agents/skills/ember-child-b/SKILL.md")));
        }

        [Test]
        public void ParentSync_UsesExistingThreeWayTransaction_ForSkillSources()
        {
            EnsureMetadata(_assets);
            var oldParent = Info();
            string childRoot = Path.Combine(_root, "child");
            string childAssets = Path.Combine(childRoot, "Assets");
            string baseline = Path.Combine(childRoot, "ParentSnapshot~", "Assets");
            EmberTemplateTransaction.CopyDirectory(_assets, childAssets);
            EmberTemplateTransaction.CopyDirectory(_assets, baseline);
            var child = Info("child", "1.0.0", childAssets);
            child.parentId = oldParent.id; child.parentVersion = oldParent.version; child.parentContentHash = oldParent.contentHash;
            EmberTemplateTransaction.WriteTemplateJson(Path.Combine(childRoot, "template.json"), child);
            Source(body: "parent synchronized");
            var parent = Info(version: "1.0.1");
            var plan = EmberTemplateInheritanceEngine.BuildParentSyncPlan(child, new[] { parent, child }, _assets, baseline, childAssets);
            Assert.AreEqual(TemplateSyncStatus.ParentChanged, plan.Status);
            Assert.IsFalse(plan.HasConflicts);
            var updated = EmberTemplateTransaction.ApplyParentSync(new ParentSyncTransactionRequest(
                plan, child, parent, childRoot, childAssets, _assets, baseline, null, 2));
            StringAssert.Contains("parent synchronized", File.ReadAllText(Path.Combine(childAssets,
                EmberAISkillInstaller.TemplateSourceDirectory, Skill, "SKILL.md")));
            Assert.AreEqual(parent.contentHash, updated.parentContentHash);
            Assert.AreEqual(updated.contentHash, updated.versionedContentHash);
            Assert.IsFalse(File.Exists(Target())); // Storage sync alone does not enable skills.
        }

        [Test]
        public void ForeignOwner_BlocksWithoutDeletingOtherTemplateSkills()
        {
            var info = Info(); Commit(info, Preview(info));
            var preview = Preview(Info("other"), "unrelated");
            Assert.IsNotEmpty(preview.Errors);
            Assert.Throws<InvalidOperationException>(() => EmberAISkillInstaller.PrepareTemplateSkills(preview, true, out _));
            Assert.IsTrue(File.Exists(Target()));
        }

        [Test]
        public void MatchingEditedSourceAndDiscovery_StillUpdatesOwnershipFingerprint()
        {
            var info = Info();
            EmberProjectSetup.CommitTemplateDeployment(_project, _assets, info, true, Preview(info, editing: true), editing: true);
            Source(body: "draft matches discovery");
            File.Copy(Path.Combine(SourceRoot(), Skill, "SKILL.md"), Target(), true);
            var preview = Preview(info, info.id, editing: true);
            Assert.IsTrue(preview.HasChanges);
            Assert.IsTrue(preview.NeedsBackupConfirmation);
            var targets = EmberAISkillInstaller.PrepareTemplateSkills(preview, true, out _);
            EmberTemplateTransaction.CommitPreparedTargets(targets.Select(t => new TemplateTransactionTarget(t.StagedPath, t.DestinationPath, t.Remove)).ToArray());
            Assert.IsNull(EmberAISkillInstaller.GetTemplateSkillExecutionBlockReason(_project, Skill, info.id, info.version, info.contentHash, true));
        }

        [Test]
        public void PathSafety_JunctionDiscoveryRoot_IsRejected()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) Assert.Ignore("Windows junction fixture.");
            string outside = Path.Combine(_root, "Outside"); Directory.CreateDirectory(outside);
            string agents = Path.Combine(_project, ".agents"); Directory.CreateDirectory(agents);
            string link = Path.Combine(agents, "skills");
            var start = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c mklink /J \"" + link + "\" \"" + outside + "\"")
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            using (var process = System.Diagnostics.Process.Start(start))
            {
                if (!process.WaitForExit(5000)) { process.Kill(); Assert.Fail("Junction fixture timed out."); }
                if (process.ExitCode != 0) Assert.Ignore("Junction creation unavailable on this host.");
            }
            try
            {
                Assert.Throws<IOException>(() => Preview(Info()));
                Assert.IsEmpty(Directory.GetFileSystemEntries(outside));
            }
            finally { if (Directory.Exists(link)) Directory.Delete(link); }
        }
        #endregion

        #region 内部方法
        private string SourceRoot() => Path.Combine(_assets, EmberAISkillInstaller.TemplateSourceDirectory);
        private string Target() => Path.Combine(_project, ".agents/skills", Skill, "SKILL.md");
        private void Source(string body = "fixture", string minFramework = "0.1.0", string minTemplate = "0.1.0")
        {
            Write(SourceRoot(), Skill + "/SKILL.md", "---\nname: " + Skill + "\ndescription: isolated fixture\n---\n" + body);
            Write(SourceRoot(), "catalog.json", "{\"schemaVersion\":2,\"skills\":[{\"id\":\"" + Skill
                + "\",\"templateId\":\"fixture\",\"minimumFrameworkVersion\":\"" + minFramework
                + "\",\"minimumTemplateVersion\":\"" + minTemplate + "\"}]}");
        }
        private TemplateInfo Info(string id = "fixture", string version = "1.0.0", string assets = null)
        {
            string hash = EmberProjectSetup.ComputeTemplateContentHash(assets ?? _assets);
            return new TemplateInfo { schemaVersion = 2, id = id, version = version, frameworkVersion = "0.13.2",
                contentHash = hash, versionedContentHash = hash };
        }
        private EmberAISkillInstaller.TemplatePreview Preview(TemplateInfo info, string previous = null, string assets = null, bool editing = false) =>
            EmberAISkillInstaller.PreviewTemplateSkills(_project, assets ?? _assets,
                new EmberAISkillInstaller.TemplateContext(info.id, info.version, info.contentHash, previous, "0.13.2", true, editing));
        private void Commit(TemplateInfo info, EmberAISkillInstaller.TemplatePreview preview, bool replace = true) =>
            EmberProjectSetup.CommitTemplateDeployment(_project, _assets, info, replace, preview);
        private static void Write(string root, string relative, string text)
        {
            string path = Path.Combine(root, relative); Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, text);
        }
        private static void EnsureMetadata(string root)
        {
            foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories).Where(p => !p.EndsWith(".meta")))
                if (!File.Exists(file + ".meta")) File.WriteAllText(file + ".meta", "fileFormatVersion: 2\nguid: " + Guid.NewGuid().ToString("N") + "\n");
            foreach (string directory in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
                if (!File.Exists(directory + ".meta")) File.WriteAllText(directory + ".meta", "fileFormatVersion: 2\nguid: "
                    + Guid.NewGuid().ToString("N") + "\nfolderAsset: yes\n");
        }
        private static void AddFixtureSkill(string assets, string id, string origin)
        {
            string source = Path.Combine(assets, EmberAISkillInstaller.TemplateSourceDirectory);
            Write(source, id + "/SKILL.md", "---\nname: " + id + "\ndescription: fixture\n---\nfixture only");
            string catalog = Path.Combine(source, "catalog.json");
            string entry = ",{\"id\":\"" + id + "\",\"templateId\":\"" + origin
                + "\",\"minimumFrameworkVersion\":\"0.1.0\",\"minimumTemplateVersion\":\"0.1.0\"}]}";
            File.WriteAllText(catalog, File.ReadAllText(catalog).Replace("]}", entry));
        }
        #endregion
    }
}
