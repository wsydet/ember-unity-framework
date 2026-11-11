// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using File = Ember.UI.Tests.PatchObservation.File;
using Directory = Ember.UI.Tests.PatchObservation.Directory;
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

        [TestCase(false)]
        [TestCase(true)]
        public void LongPaths_FullPreviewPreservesInputs_AndApplyCanCommitOrRollback(bool interrupt)
        {
            string relative = LongAssetPath();
            Asset(_source, relative, "old asset");
            Deploy(Info("1.2.0"));
            string live = Path.Combine(_project, "Assets", relative);
            Assert.AreEqual(235, (live + ".meta").Length);
            Assert.AreEqual(RuntimePlatform.WindowsEditor, Application.platform);
            Assert.AreEqual(0, (int)Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem", "LongPathsEnabled", -1));
            foreach (string input in new[] { live, Path.Combine(_source, relative) })
            {
                Assert.LessOrEqual((input + ".meta").Length, 235);
                Assert.AreEqual("old asset", System.IO.File.ReadAllText(input));
                Assert.IsNotEmpty(System.IO.File.ReadAllBytes(input + ".meta"));
                TestContext.Out.WriteLine("ordinary input=" + input + " meta length=" + (input + ".meta").Length);
            }
            TestContext.Out.WriteLine("Unity=" + Application.unityVersion + " platform=" + Application.platform
                + " CLR=" + Environment.Version + " OS=" + Environment.OSVersion + " volume=" + new DriveInfo(Path.GetPathRoot(_root)).DriveFormat);
            // 源与 live 必须能普通读取。
            Assert.AreEqual("old asset", System.IO.File.ReadAllText(live));
            string meta = System.IO.File.ReadAllText(live + ".meta");
            Asset(_source, relative, "new asset");
            var next = Info("1.2.1");
            string before = Fingerprint(), sourceBefore = RawTree(_source);
            string stage = null;
            var plan = EmberProjectSetup.BuildTemplatePatchPlan(_project, _source, next, path =>
            {
                stage = path;
                foreach (string side in new[] { "Old", "Incoming", "Local" })
                {
                    string staged = Path.Combine(path, side, relative);
                    Assert.Greater((staged + ".meta").Length, 260);
                    TestContext.Out.WriteLine(side + " meta length=" + (staged + ".meta").Length);
                    Assert.AreEqual(meta, File.ReadAllText(staged + ".meta"));
                }
                TestContext.Out.WriteLine("live meta length=235, source meta length=" + (Path.Combine(_source, relative) + ".meta").Length
                    + ", Local meta length=" + Path.Combine(path, "Local", relative + ".meta").Length);
            });
            Assert.IsFalse(Directory.Exists(stage));
            Assert.AreEqual(before, Fingerprint());
            Assert.AreEqual(sourceBefore, RawTree(_source));
            var change = plan.Changes.Single(c => c.UnitPath == relative);
            Assert.IsFalse(change.IsConflict);
            Assert.AreEqual(TemplateConflictChoice.AcceptParent, change.RecommendedChoice);
            Assert.IsTrue(change.Files.Any(f => f.RelativePath == relative + ".meta"));
            Assert.IsFalse(plan.Changes.SelectMany(c => c.Files).Any(f => f.RelativePath.Contains(@"\\?\")));
            if (interrupt)
            {
                var primary = new IOException("injected long-path commit");
                Assert.AreSame(primary, Assert.Throws<IOException>(() => EmberProjectSetup.CommitTemplatePatch(plan, next, null,
                    faultInjector: _ => throw primary)));
                Assert.AreEqual(before, Fingerprint());
            }
            else
            {
                EmberProjectSetup.CommitTemplatePatch(plan, next, null);
                Assert.AreEqual("new asset", System.IO.File.ReadAllText(live));
                Assert.AreEqual(next.contentHash, Record().contentHash);
            }
            Assert.AreEqual(meta, System.IO.File.ReadAllText(live + ".meta"));
            Assert.AreEqual(sourceBefore, RawTree(_source));
            Assert.IsEmpty(Directory.GetDirectories(Path.Combine(_project, "Temp"), "EmberPatch*"));
        }

        [Test]
        public void LongPaths_ConcurrentEditRemainsConflict_WithOriginalGuidAndHashes()
        {
            string relative = LongAssetPath();
            Asset(_source, relative, "old");
            Deploy(Info("1.2.0"));
            Asset(_source, relative, "parent");
            Asset(_project, "Assets/" + relative, "child");
            string before = Fingerprint();
            var plan = Plan(Info("1.2.1"));
            var change = plan.Changes.Single(c => c.UnitPath == relative);
            Assert.IsTrue(change.IsConflict);
            var file = change.Files.Single(f => f.RelativePath == relative);
            Assert.AreNotEqual(file.OldHash, file.ParentHash);
            Assert.AreNotEqual(file.ParentHash, file.ChildHash);
            var meta = change.Files.Single(f => f.RelativePath == relative + ".meta");
            Assert.AreEqual(meta.OldGuid, meta.ParentGuid);
            Assert.AreEqual(meta.OldGuid, meta.ChildGuid);
            Assert.AreEqual(before, Fingerprint());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LongPaths_PreviewFailurePreservesPrimary_AndReportsCleanupFailure(bool lockFile)
        {
            if (lockFile && Application.platform != RuntimePlatform.WindowsEditor) Assert.Ignore("Windows 文件锁语义");
            string relative = LongAssetPath();
            Asset(_source, relative, "old");
            Deploy(Info("1.2.0"));
            Asset(_source, relative, "incoming");
            var next = Info("1.2.1");
            string before = Fingerprint(), stage = null;
            string sibling = Path.Combine(_project, "Temp", "EmberPatchPreview-other~");
            Write(sibling, "keep.txt", "another operation");
            FileStream held = null;
            var primary = new IOException("primary preview fault: " + relative);
            try
            {
                var actual = Assert.Throws<IOException>(() => EmberProjectSetup.BuildTemplatePatchPlan(_project, _source, next, path =>
                {
                    stage = path;
                    if (lockFile) held = new FileStream(PatchObservation.Native(Path.Combine(path, "Local", relative)),
                        FileMode.Open, FileAccess.Read, FileShare.None);
                    throw primary;
                }));
                Assert.AreSame(primary, actual);
                Assert.AreEqual(stage, actual.Data["EmberTemporaryDirectory"]);
                Assert.AreEqual(lockFile, Directory.Exists(stage));
                if (lockFile)
                {
                    var cleanup = actual.Data["EmberStageCleanupException"] as IOException;
                    Assert.IsNotNull(cleanup);
                    StringAssert.Contains(stage, cleanup.Message);
                    Assert.IsNotNull(cleanup.InnerException);
                }
                Assert.AreEqual(before, Fingerprint());
                Assert.AreEqual("another operation", File.ReadAllText(Path.Combine(sibling, "keep.txt")));
            }
            finally
            {
                held?.Dispose();
                if (stage != null) EmberTemplateTransaction.CleanPath(stage);
            }
        }

        [Test]
        public void Preview_NestedOperationsHaveSeparateOwnedDirectories()
        {
            Asset(_source, "Game/Shared.txt", "new");
            var next = Info("1.2.1");
            string outer = null, inner = null;
            EmberProjectSetup.BuildTemplatePatchPlan(_project, _source, next, path =>
            {
                outer = path;
                EmberProjectSetup.BuildTemplatePatchPlan(_project, _source, next, nested => inner = nested);
                Assert.AreNotEqual(outer, inner);
                Assert.IsTrue(Directory.Exists(outer));
                Assert.IsFalse(Directory.Exists(inner));
            });
            Assert.IsFalse(Directory.Exists(outer));
        }

        [TestCase("copy")] [TestCase("normalize")] [TestCase("meta")]
        public void Preview_CopyAndReadFailuresPreservePrimaryPathAndInputs(string phase)
        {
            Asset(_source, "Game/Shared.txt", "new"); var next = Info("1.2.1");
            string before = Fingerprint(), sourceBefore = RawTree(_source), stage = null, failedPath = null;
            var primary = new IOException("copy interrupted"); FileStream held = null;
            try
            {
                var error = Assert.Throws<IOException>(() => EmberProjectSetup.BuildTemplatePatchPlan(_project, _source, next,
                    previewStaged: p => { stage = p; if (phase == "meta") { failedPath = Path.Combine(p, "Local/Game/Shared.txt.meta"); held = new FileStream(PatchObservation.Native(failedPath), FileMode.Open, FileAccess.Read, FileShare.None); } },
                    beforeNormalizeForTest: p => { if (phase == "normalize" && held == null) { failedPath = p; held = new FileStream(PatchObservation.Native(p), FileMode.Open, FileAccess.Read, FileShare.None); } },
                    copiedForTest: (source, dest) => { if (phase == "copy") { failedPath = dest; primary.Data["copyPath"] = dest; throw primary; } }));
                stage ??= (string)error.Data["EmberTemporaryDirectory"];
                Assert.IsNotNull(stage);
                if (phase == "copy") { Assert.AreSame(primary, error); StringAssert.Contains("CopyPatchAssets", error.StackTrace); Assert.IsNotNull(error.Data["copyPath"]); }
                else { Assert.AreEqual(Path.GetFullPath(failedPath), Path.GetFullPath((string)error.Data["EmberIOPath"])); Assert.IsNotNull(error.Data["EmberStageCleanupException"]); }
                Assert.AreEqual(before, Fingerprint()); Assert.AreEqual(sourceBefore, RawTree(_source));
                if (phase == "copy") Assert.IsFalse(Directory.Exists(stage));
            }
            finally { held?.Dispose(); if (stage != null) EmberTemplateTransaction.CleanPath(stage); }
        }

        [Test] public void Preview_NoChangesPreservesRawTreesAndDirectoryGuids()
        {
            var next = Info("1.2.1"); string before = Fingerprint(), sourceBefore = RawTree(_source);
            var plan = Plan(next); Assert.IsFalse(plan.Changes.Any(c => c.IsConflict));
            Assert.IsTrue(plan.Changes.All(c => c.RecommendedChoice == TemplateConflictChoice.KeepChild));
            Assert.AreEqual(before, Fingerprint()); Assert.AreEqual(sourceBefore, RawTree(_source));
        }

        [Test] public void Apply_CommittedCleanupFailureReturnsCountAndWarnsWithoutRetryAmbiguity()
        {
            Asset(_source, "Game/Shared.txt", "committed"); var next = Info("1.2.1"); var plan = Plan(next);
            string stage = null; FileStream held = null; var warnings = new List<string>();
            void Observe(string message, string stack, LogType type) { if (type == LogType.Warning) warnings.Add(message); }
            Application.logMessageReceived += Observe;
            try
            {
                var hooks = new EmberTemplateTemporaryDirectory.TestHooks { BeforeCleanup = p =>
                {
                    stage = p; string path = Path.Combine(p, "cleanup-lock"); File.WriteAllText(path, "lock");
                    held = new FileStream(PatchObservation.Native(path), FileMode.Open, FileAccess.Read, FileShare.None);
                }};
                Assert.Greater(EmberProjectSetup.CommitTemplatePatch(plan, next, null, scopeHooksForTest: hooks), 0);
                Assert.AreEqual("committed", Read("Assets/Game/Shared.txt")); Assert.AreEqual("1.2.1", Record().version);
                Assert.IsTrue(Directory.Exists(stage)); Assert.IsTrue(warnings.Any(w => w.Contains("已提交") && w.Contains(stage)));
            }
            finally { Application.logMessageReceived -= Observe; held?.Dispose(); if (stage != null) EmberTemplateTransaction.CleanPath(stage); }
        }

        [Test] public void LongPaths_ShortEquivalentPlanAndHashesMatch()
        {
            string relative = LongAssetPath(); Asset(_source, relative, "old\r\nasset"); Deploy(Info("1.2.0"));
            Asset(_source, relative, "new\r\nasset"); var next = Info("1.2.1");
            string sandbox = Path.Combine(Path.GetTempPath(), "EP" + Guid.NewGuid().ToString("N").Substring(0, 12));
            Assert.IsFalse(Directory.Exists(sandbox)); Directory.CreateDirectory(sandbox);
            try
            {
            string shortRoot = Path.Combine(sandbox, "P");
            EmberTemplateTransaction.CopyDirectory(_project, shortRoot);
            string shortSource = Path.Combine(sandbox, "N"); EmberTemplateTransaction.CopyDirectory(_source, shortSource);
            var longPlan = Plan(next); var shortPlan = EmberProjectSetup.BuildTemplatePatchPlan(shortRoot, shortSource, next, stage =>
            {
                foreach (string side in new[] { "Old", "Incoming", "Local" })
                {
                    string meta = Path.Combine(stage, side, relative + ".meta"); Assert.Less(meta.Length, 260);
                    Assert.IsNotEmpty(System.IO.File.ReadAllText(meta)); TestContext.Out.WriteLine("short " + side + " meta=" + meta.Length);
                }
            });
            Assert.AreEqual(longPlan.ProjectHash, shortPlan.ProjectHash); Assert.AreEqual(longPlan.BaselineHash, shortPlan.BaselineHash);
            CollectionAssert.AreEqual(longPlan.Changes.Select(c => c.UnitPath + ":" + c.Kind + ":" + c.RecommendedChoice), shortPlan.Changes.Select(c => c.UnitPath + ":" + c.Kind + ":" + c.RecommendedChoice));
            CollectionAssert.AreEqual(longPlan.Changes.SelectMany(c => c.Files).Select(f => f.RelativePath + ":" + f.OldHash + ":" + f.ParentHash + ":" + f.ChildHash),
                shortPlan.Changes.SelectMany(c => c.Files).Select(f => f.RelativePath + ":" + f.OldHash + ":" + f.ParentHash + ":" + f.ChildHash));
            Assert.IsFalse(Read(RecordPath).Contains(@"\\?\"));
            }
            finally { Directory.Delete(sandbox, true); }
        }

        [Test] public void LongPaths_DeepSkillApplyCoversUpmSnapshotsAndDefaults()
        {
            string skillRoot = EmberAISkillInstaller.TemplateSourceDirectory;
            Write(_source, skillRoot + "/ember-fixture/SKILL.md", "---\nname: ember-fixture\ndescription: isolated fixture\n---\nfixture");
            Write(_source, skillRoot + "/catalog.json", "{\"schemaVersion\":2,\"skills\":[{\"id\":\"ember-fixture\",\"templateId\":\"fixture\",\"minimumFrameworkVersion\":\"0.1.0\",\"minimumTemplateVersion\":\"0.1.0\"}]}");
            string deep = skillRoot + "/ember-fixture/" + string.Join("/", Enumerable.Repeat("nested-legal-component", 8)) + "/reference.md";
            Write(_source, deep, "old reference");
            Write(_source, "Game/Config/EmberExtensionDefaults.json", "{\"schemaVersion\":2,\"ownerTemplateId\":\"fixture\",\"ownerTemplateVersion\":\"1.2.0\",\"enabled\":[]}");
            foreach (string file in Directory.GetFiles(_source, "*", SearchOption.AllDirectories).Where(p => !p.EndsWith(".meta")))
                if (!File.Exists(file + ".meta")) File.WriteAllText(file + ".meta", Meta());
            foreach (string dir in Directory.GetDirectories(_source, "*", SearchOption.AllDirectories))
                if (!File.Exists(dir + ".meta")) File.WriteAllText(dir + ".meta", Meta(true));
            Deploy(Info("1.2.0")); Write(_source, deep, "new reference");
            var next = Info("1.2.1"); var plan = Plan(next); string sourceBefore = RawTree(_source);
            EmberProjectSetup.CommitTemplatePatch(plan, next, null);
            Assert.AreEqual("new reference", Read("Assets/" + deep));
            string discovery = Path.Combine(_project, ".agents/skills/ember-fixture", deep.Substring((skillRoot + "/ember-fixture/").Length));
            Assert.Greater(discovery.Length, 260); Assert.AreEqual("new reference", File.ReadAllText(discovery));
            Assert.AreEqual(sourceBefore, RawTree(_source)); Assert.IsFalse(Read(".agents/ember-ai-skills.json").Contains(@"\\?\"));
        }
        #endregion

        #region 内部方法
        private string LongAssetPath()
        {
            string prefix = "GameResource/LongPaths/";
            int length = 235 - Path.Combine(_project, "Assets", prefix).Length - ".asset.meta".Length;
            Assert.Greater(length, 0);
            while (length > 40) { prefix += new string('a', 30) + "/"; length -= 31; }
            return prefix + new string('b', length) + ".asset";
        }

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
        private string Fingerprint() => RawTree(_project, true);
        private static string RawTree(string root, bool excludeTemp = false)
        {
            bool Include(string p) => !excludeTemp || (!p.StartsWith(Path.Combine(root, "Temp") + Path.DirectorySeparatorChar)
                && !p.StartsWith(Path.Combine(root, ".utmp") + Path.DirectorySeparatorChar));
            var dirs = Directory.GetDirectories(root, "*", SearchOption.AllDirectories).Where(Include)
                .Where(p => !excludeTemp || (p != Path.Combine(root, "Temp") && p != Path.Combine(root, ".utmp")))
                .Select(p => "D:" + p.Substring(root.Length));
            var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories).Where(Include)
                .Select(p => "F:" + p.Substring(root.Length) + ":" + Convert.ToBase64String(File.ReadAllBytes(p)));
            return string.Join("\n", dirs.Concat(files).OrderBy(p => p, StringComparer.Ordinal));
        }
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

namespace Ember.UI.Tests
{
    internal static class PatchObservation
    {
        internal static string Native(string p) => Path.DirectorySeparatorChar != '\\' ? p
            : p.StartsWith(@"\\", StringComparison.Ordinal) ? @"\\?\UNC\" + p.Substring(2) : @"\\?\" + Path.GetFullPath(p);
        internal static class File
        {
            internal static bool Exists(string p) => System.IO.File.Exists(Native(p));
            internal static string ReadAllText(string p) => System.IO.File.ReadAllText(Native(p));
            internal static byte[] ReadAllBytes(string p) => System.IO.File.ReadAllBytes(Native(p));
            internal static void WriteAllText(string p, string t) => System.IO.File.WriteAllText(Native(p), t);
            internal static void Copy(string a, string b, bool overwrite = false) => System.IO.File.Copy(Native(a), Native(b), overwrite);
            internal static void Delete(string p) => System.IO.File.Delete(Native(p));
        }
        internal static class Directory
        {
            internal static bool Exists(string p) => System.IO.Directory.Exists(Native(p));
            internal static void CreateDirectory(string p) => System.IO.Directory.CreateDirectory(Native(p));
            internal static void Delete(string p, bool recursive = false) => System.IO.Directory.Delete(Native(p), recursive);
            private static string[] Restore(string root, string[] entries) => entries.Select(p => Path.Combine(root, p.Substring(Native(root).TrimEnd('\\').Length).TrimStart('\\', '/'))).ToArray();
            internal static string[] GetFiles(string p, string pattern = "*", SearchOption option = SearchOption.TopDirectoryOnly) => Restore(p, System.IO.Directory.GetFiles(Native(p), pattern, option));
            internal static string[] GetDirectories(string p, string pattern = "*", SearchOption option = SearchOption.TopDirectoryOnly) => Restore(p, System.IO.Directory.GetDirectories(Native(p), pattern, option));
        }
    }
}
