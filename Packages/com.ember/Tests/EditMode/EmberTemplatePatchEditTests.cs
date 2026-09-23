// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ember.Core.Editor;
using Ember.UPMManager.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Ember.UI.Tests
{
    /// <summary>仅操作临时项目，验证消费端增量更新与本地文件保护。</summary>
    public sealed class EmberTemplatePatchEditTests
    {
        #region 内部参数
        private string _root, _project, _source;
        private const string RecordPath = "Assets/Editor/EmberDeployedTemplates.json";
        #endregion

        #region 外部方法
        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "EmberPatchTests-" + Guid.NewGuid().ToString("N"));
            _project = Path.Combine(_root, "Project");
            _source = Path.Combine(_root, "Source");
            Directory.CreateDirectory(_project);
            Asset(_source, "Game/Shared.txt", "old");
            Asset(_source, "Game/Local.txt", "local base");
            Deploy(Info("1.2.0"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        [TestCase("1.2.0", "1.2.3", true)]
        [TestCase("1.2.3", "1.2.3", false)]
        [TestCase("1.2.3", "1.2.2", false)]
        [TestCase("1.2.0", "1.3.0", false)]
        [TestCase("1.2.0", "2.2.1", false)]
        [TestCase("1.2", "1.2.1", false)]
        [TestCase("bad", "1.2.1", false)]
        [TestCase("1.2.0", "1.2.1.0", false)]
        public void VersionGate_OnlyForwardPatch(string from, string to, bool expected)
        {
            Assert.AreEqual(expected, EmberProjectSetup.IsForwardTemplatePatch(from, to));
        }

        [Test]
        public void Patch_UpdatesChangedFiles_PreservesLocalAdditionsEditsAndDeletions()
        {
            Asset(_project, "Assets/Game/Local.txt", "my game");
            Asset(_project, "Assets/Game/User.txt", "user only");
            Asset(_source, "Game/Shared.txt", "new");
            Asset(_source, "Game/New.txt", "new file");
            var next = Info("1.2.2");
            var plan = Plan(next);
            Assert.IsFalse(plan.Changes.Any(c => c.IsConflict));
            EmberProjectSetup.CommitTemplatePatch(plan, next, null);
            Assert.AreEqual("new", Read("Assets/Game/Shared.txt"));
            Assert.AreEqual("my game", Read("Assets/Game/Local.txt"));
            Assert.AreEqual("user only", Read("Assets/Game/User.txt"));
            Assert.AreEqual("new file", Read("Assets/Game/New.txt"));
            Assert.AreEqual(next.contentHash, Record().contentHash);
            Assert.AreEqual("1.2.2", Record().version);
            Assert.AreEqual(next.contentHash, EmberProjectSetup.ComputeTemplateContentHash(
                Path.Combine(_project, EmberProjectSetup.DeploymentBaselinePath, "Assets")));
            File.Delete(Path.Combine(_project, "Assets/Game/Local.txt"));
            File.Delete(Path.Combine(_project, "Assets/Game/Local.txt.meta"));
            var again = Info("1.2.3");
            EmberProjectSetup.CommitTemplatePatch(Plan(again), again, null);
            Assert.IsFalse(File.Exists(Path.Combine(_project, "Assets/Game/Local.txt")));
        }

        [Test]
        public void Conflict_NoWritesUntilResolved_AndLogHasThreeWayIdentity()
        {
            Asset(_project, "Assets/Game/Shared.txt", "game edit");
            Asset(_source, "Game/Shared.txt", "template edit");
            var next = Info("1.2.1");
            var plan = Plan(next);
            string before = Fingerprint();
            Assert.Throws<InvalidOperationException>(() => EmberProjectSetup.CommitTemplatePatch(plan, next, null));
            Assert.AreEqual(before, Fingerprint());
            string log = EmberProjectSetup.BuildTemplatePatchConflictLog(plan);
            StringAssert.Contains("Game/Shared.txt", log);
            StringAssert.Contains("1.2.0 -> 1.2.1", log);
            StringAssert.Contains("ConflictConcurrentModification", log);
            StringAssert.Contains(plan.SourceHash, log);
            StringAssert.Contains(plan.BaselineHash, log);
            StringAssert.Contains("<不存在>", EmberProjectSetup.BuildTemplatePatchConflictLog(ConcurrentAddition(next)));
            // 重新预览恢复本测试的冲突集合。
            plan = Plan(next);
            var choices = plan.Changes.Where(c => c.IsConflict).ToDictionary(c => c.UnitPath, c => TemplateConflictChoice.KeepChild);
            EmberProjectSetup.CommitTemplatePatch(plan, next, choices);
            Assert.AreEqual("game edit", Read("Assets/Game/Shared.txt"));
            Assert.AreEqual("1.2.1", Record().version);
        }

        [Test]
        public void StalePreview_RejectsLocalChange()
        {
            Asset(_source, "Game/Shared.txt", "new");
            var next = Info("1.2.1");
            var plan = Plan(next);
            Asset(_project, "Assets/Game/Shared.txt", "after preview");
            string before = Fingerprint();
            Assert.Throws<IOException>(() => EmberProjectSetup.CommitTemplatePatch(plan, next, null));
            Assert.AreEqual(before, Fingerprint());
        }

        [TestCase("source")]
        [TestCase("baseline")]
        [TestCase("record")]
        public void StalePreview_RejectsChangedInputs(string input)
        {
            Asset(_source, "Game/Shared.txt", "new");
            var next = Info("1.2.1");
            var plan = Plan(next);
            if (input == "source") Asset(_source, "Game/Shared.txt", "changed source");
            else if (input == "baseline") Asset(Path.Combine(_project, EmberProjectSetup.DeploymentBaselinePath, "Assets"),
                "Game/Shared.txt", "damaged baseline");
            else File.WriteAllText(Path.Combine(_project, RecordPath), "{broken");
            string before = Fingerprint();
            Assert.That(() => EmberProjectSetup.CommitTemplatePatch(plan, next, null), Throws.Exception);
            Assert.AreEqual(before, Fingerprint());
        }

        [Test]
        public void GuidCollisionOutsideManagedDirectories_RejectsBeforeWrite()
        {
            Asset(_source, "Game/Added.txt", "new");
            Asset(_project, "Assets/Art/Occupied.txt", "art");
            File.Copy(Path.Combine(_source, "Game/Added.txt.meta"), Path.Combine(_project, "Assets/Art/Occupied.txt.meta"), true);
            var next = Info("1.2.1");
            var plan = Plan(next);
            string before = Fingerprint();
            Assert.Throws<InvalidOperationException>(() => EmberProjectSetup.CommitTemplatePatch(plan, next, null));
            Assert.AreEqual(before, Fingerprint());
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void InterruptedCommit_RestoresFilesBaselineAndRecord(int index)
        {
            Asset(_source, "Game/Shared.txt", "new");
            var next = Info("1.2.1");
            var plan = Plan(next);
            string before = Fingerprint();
            Assert.Throws<IOException>(() => EmberProjectSetup.CommitTemplatePatch(plan, next, null,
                faultInjector: step => { if (step == index) throw new IOException("injected"); }));
            Assert.AreEqual(before, Fingerprint());
        }

        [Test]
        public void MissingBaseline_CanRestoreOnlyFromMatchingOldOriginal()
        {
            string old = Path.Combine(_root, "Old");
            EmberTemplateTransaction.CopyDirectory(_source, old);
            Directory.Delete(Path.Combine(_project, EmberProjectSetup.DeploymentBaselinePath), true);
            Asset(_source, "Game/Shared.txt", "new");
            var next = Info("1.2.1");
            Assert.Throws<InvalidDataException>(() => Plan(next));
            Assert.Throws<InvalidDataException>(() => EmberProjectSetup.RestoreTemplateDeploymentBaseline(_project, _source));
            string business = Read("Assets/Game/Shared.txt");
            EmberProjectSetup.RestoreTemplateDeploymentBaseline(_project, old);
            Assert.AreEqual(business, Read("Assets/Game/Shared.txt"));
            Assert.DoesNotThrow(() => Plan(next));
        }

        [Test]
        public void Repair_DoesNotAdvanceBaseline_AndCannotUpgrade()
        {
            string before = Read(RecordPath);
            Deploy(Info("1.2.0"), false);
            Assert.AreEqual(before, Read(RecordPath));
            Asset(_source, "Game/Shared.txt", "new");
            Assert.Throws<InvalidOperationException>(() => Deploy(Info("1.2.1"), false));
            Assert.AreEqual("old", Read("Assets/Game/Shared.txt"));
        }

        [Test]
        public void FolderDeletionAndEmptyAddition_CommitAsDirectoryUnits()
        {
            Asset(_source, "Game/Removed/Old.txt", "old");
            Write(_source, "Game/Removed.meta", Meta(true));
            Deploy(Info("1.2.0"));
            Directory.Delete(Path.Combine(_source, "Game/Removed"), true);
            File.Delete(Path.Combine(_source, "Game/Removed.meta"));
            Directory.CreateDirectory(Path.Combine(_source, "Game/Empty"));
            Write(_source, "Game/Empty.meta", Meta(true));
            var next = Info("1.2.1");
            EmberProjectSetup.CommitTemplatePatch(Plan(next), next, null);
            Assert.IsFalse(Directory.Exists(Path.Combine(_project, "Assets/Game/Removed")));
            Assert.IsTrue(Directory.Exists(Path.Combine(_project, "Assets/Game/Empty")));
            Assert.IsTrue(File.Exists(Path.Combine(_project, "Assets/Game/Empty.meta")));
        }

        [Test]
        public void DeletedFolderWithLocalAddition_CannotDropFolderGuidWhileKeepingItsContent()
        {
            Asset(_source, "Game/Removed/Old.txt", "old");
            Write(_source, "Game/Removed.meta", Meta(true));
            Deploy(Info("1.2.0"));
            Asset(_project, "Assets/Game/Removed/User.txt", "user");
            Directory.Delete(Path.Combine(_source, "Game/Removed"), true);
            File.Delete(Path.Combine(_source, "Game/Removed.meta"));
            var next = Info("1.2.1");
            var plan = Plan(next);
            var choices = plan.Changes.Where(c => c.IsConflict)
                .ToDictionary(c => c.UnitPath, c => TemplateConflictChoice.AcceptParent);
            string before = Fingerprint();
            Assert.Throws<InvalidDataException>(() => EmberProjectSetup.CommitTemplatePatch(plan, next, choices));
            Assert.AreEqual(before, Fingerprint());
            choices["Game/Removed"] = TemplateConflictChoice.KeepChild;
            EmberProjectSetup.CommitTemplatePatch(plan, next, choices);
            Assert.AreEqual("user", Read("Assets/Game/Removed/User.txt"));
            Assert.IsTrue(File.Exists(Path.Combine(_project, "Assets/Game/Removed.meta")));
        }

        [Test]
        public void GeneratedVersionHeader_IsNotAConcurrentEdit()
        {
            Asset(_source, "Game/Generated.txt", "// Generated by Ember Setup v0.1.0\nold");
            Deploy(Info("1.2.0"));
            Asset(_source, "Game/Generated.txt", "// Generated by Ember Setup v0.1.0\nfixed");
            var next = Info("1.2.1");
            var plan = Plan(next);
            Assert.IsFalse(plan.Changes.Any(c => c.IsConflict));
            EmberProjectSetup.CommitTemplatePatch(plan, next, null);
            StringAssert.Contains("fixed", Read("Assets/Game/Generated.txt"));
            StringAssert.Contains("v1.2.1", Read("Assets/Game/Generated.txt"));
        }
        #endregion

        #region 内部方法
        private TemplatePatchPlan ConcurrentAddition(TemplateInfo next)
        {
            Asset(_source, "Game/Added.txt", "incoming");
            Asset(_project, "Assets/Game/Added.txt", "local");
            var updated = Info(next.version);
            next.contentHash = updated.contentHash;
            next.versionedContentHash = updated.versionedContentHash;
            return Plan(next);
        }

        private TemplatePatchPlan Plan(TemplateInfo next) => EmberProjectSetup.BuildTemplatePatchPlan(_project, _source, next);
        private TemplateInfo Info(string version)
        {
            string hash = EmberProjectSetup.ComputeTemplateContentHash(_source);
            return new TemplateInfo { schemaVersion = 2, id = "fixture", version = version,
                frameworkVersion = "0.14.4", contentHash = hash, versionedContentHash = hash };
        }

        private void Deploy(TemplateInfo info, bool replace = true)
        {
            var skills = EmberAISkillInstaller.PreviewTemplateSkills(_project, _source,
                new EmberAISkillInstaller.TemplateContext(info.id, info.version, info.contentHash, "fixture", "0.14.4", true));
            EmberProjectSetup.CommitTemplateDeployment(_project, _source, info, replace, skills);
        }

        private string Read(string path) => File.ReadAllText(Path.Combine(_project, path));
        private DeployedTemplateRecord Record() => EmberProjectSetup.ResolveActiveDeployment(
            JsonUtility.FromJson<DeployedTemplatesData>(Read(RecordPath)));
        private string Fingerprint() => string.Join("\n", Directory.GetFiles(_project, "*", SearchOption.AllDirectories)
            .Where(p => !p.StartsWith(Path.Combine(_project, "Temp") + Path.DirectorySeparatorChar)
                && !p.StartsWith(Path.Combine(_project, ".utmp") + Path.DirectorySeparatorChar))
            .OrderBy(p => p, StringComparer.Ordinal).Select(p => p.Substring(_project.Length) + ":" + Convert.ToBase64String(File.ReadAllBytes(p))));
        private static string Meta(bool folder = false) => "fileFormatVersion: 2\nguid: " + Guid.NewGuid().ToString("N") + "\n"
            + (folder ? "folderAsset: yes\n" : "");
        private static void Asset(string root, string relative, string text)
        {
            Write(root, relative, text);
            if (!File.Exists(Path.Combine(root, relative + ".meta"))) Write(root, relative + ".meta", Meta());
        }

        private static void Write(string root, string relative, string text)
        {
            string path = Path.Combine(root, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text);
        }
        #endregion
    }
}
