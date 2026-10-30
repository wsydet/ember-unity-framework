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
        [Test]
        public void ProjectSkills_IndependentOwnershipVersionBackupAndTemplateUpdate()
        {
            ProjectSource(); var info = Info(); ApplyProject(ProjectPlan(info));
            Assert.IsNull(ProjectBlock(info));
            string statePath = Path.Combine(_project, ".agents/ember-ai-skills.json");
            var state = JsonUtility.FromJson<EmberAISkillInstaller.State>(File.ReadAllText(statePath));
            var installed = state.skills.Single(s => s.id == "project-fixture");
            Assert.AreEqual(4, state.schemaVersion); Assert.AreEqual("project", installed.ownerKind);
            Assert.AreEqual("fixture-project", installed.projectId); Assert.AreEqual("1.0.0", installed.revision);
            File.AppendAllText(ProjectTarget(), "local draft");
            ProjectSource("1.0.1", "updated");
            Assert.IsNotNull(ProjectBlock(info));
            var preview = ProjectPlan(info);
            Assert.IsTrue(preview.NeedsBackupConfirmation);
            Assert.Throws<InvalidOperationException>(() => EmberAISkillInstaller.PrepareTemplateSkills(preview, false, out _));
            ApplyProject(preview); Assert.IsNull(ProjectBlock(info));
            Assert.IsTrue(Directory.GetFiles(Path.Combine(_project, ".utmp"), "SKILL.md", SearchOption.AllDirectories).Any(p => File.ReadAllText(p).Contains("local draft")));
            string source = File.ReadAllText(Path.Combine(_project, EmberAISkillInstaller.ProjectSourceDirectory, "catalog.json"));
            // Full template deployment uses the same transaction and cannot replace the project source.
            Commit(info, ProjectPlan(info));
            Assert.AreEqual(source, File.ReadAllText(Path.Combine(_project, EmberAISkillInstaller.ProjectSourceDirectory, "catalog.json")));
            Assert.IsNull(ProjectBlock(info));
        }

        [TestCase("extension")]
        [TestCase("module")]
        [TestCase("template")]
        [TestCase("version")]
        [TestCase("source")]
        [TestCase("hash")]
        public void ProjectSkills_InvalidConditionsPruneWithBackupAndRejectCachedExecution(string change)
        {
            ProjectSource(); var info = Info(); ApplyProject(ProjectPlan(info));
            File.AppendAllText(ProjectTarget(), "preserved discovery draft");
            var extensions = ProjectExtensions(); var scopes = new[] { "fixture.module" };
            if (change == "extension") extensions = Array.Empty<EmberAISkillInstaller.ExtensionIdentity>();
            if (change == "module") scopes = Array.Empty<string>();
            if (change == "template") info = Info(id: "other");
            if (change == "version") info = Info(version: "2.0.0");
            if (change == "hash") extensions[0].contentHash = "changed";
            if (change == "source") ProjectSource(body: "changed without version bump");
            Assert.IsNotNull(ProjectBlock(info, extensions, scopes));
            var plan = ProjectPlan(info, extensions, scopes);
            EmberAISkillInstaller.LimitToUnavailableExtensions(plan); ApplyProject(plan);
            Assert.IsFalse(File.Exists(ProjectTarget()));
            Assert.IsTrue(File.Exists(Path.Combine(_project, EmberAISkillInstaller.ProjectSourceDirectory, "project-fixture/SKILL.md")));
            Assert.IsTrue(Directory.GetFiles(Path.Combine(_project, ".utmp"), "SKILL.md", SearchOption.AllDirectories).Any(p => File.ReadAllText(p).Contains("preserved discovery draft")));
        }

        [Test]
        public void ProjectSkills_IndependentTemplateSourceUpdateKeepsBusinessBindingAndBecomesNoOp()
        {
            ProjectSource(); EnsureMetadata(_assets); var business = Info(); Commit(business, ProjectPlan(business));
            Source(body: "new template source"); EnsureMetadata(_assets); var next = Info(version: "1.1.0");
            EmberAISkillInstaller.TemplatePreview Plan() => EmberAISkillInstaller.PreviewTemplateSkills(_project, _assets,
                new EmberAISkillInstaller.TemplateContext(next.id, next.version, next.contentHash, business.id, "0.25.0", true,
                    businessVersion: business.version, businessContentHash: business.contentHash, updateSource: true,
                    extensions: ProjectExtensions(), moduleScopes: new[] { "fixture.module" }));
            EmberProjectSetup.CommitTemplateSkillUpdate(Plan(), true);
            Assert.IsNull(ProjectBlock(business));
            Assert.IsFalse(Plan().HasChanges);
        }

        [Test]
        public void ProjectSkills_PreviewMutationConflictAndRollbackFailClosed()
        {
            ProjectSource(); var info = Info(); var plan = ProjectPlan(info);
            ProjectSource(body: "changed after preview");
            Assert.Throws<IOException>(() => ApplyProject(plan));
            ApplyProject(ProjectPlan(info));
            string before = File.ReadAllText(ProjectTarget());
            ProjectSource("1.0.1", "new");
            var targets = EmberAISkillInstaller.PrepareTemplateSkills(ProjectPlan(info), true, out _)
                .Select(t => new TemplateTransactionTarget(t.StagedPath, t.DestinationPath, t.Remove)).ToList();
            Assert.Throws<IOException>(() => EmberTemplateTransaction.CommitPreparedTargets(targets, i => { if (i == 1) throw new IOException("injected"); }));
            Assert.AreEqual(before, File.ReadAllText(ProjectTarget()));
            string catalog = Path.Combine(_project, EmberAISkillInstaller.ProjectSourceDirectory, "catalog.json");
            File.WriteAllText(catalog, File.ReadAllText(catalog).Replace("fixture-project", "another-owner"));
            Assert.IsTrue(ProjectPlan(info).Errors.Any(e => e.Contains("所有权")));
            var prune = EmberAISkillInstaller.PreviewUnavailableProjectSkills(_project); ApplyProject(prune);
            Assert.IsFalse(File.Exists(ProjectTarget()));
        }

        [Test]
        public void ProjectSkills_UnmanagedAndTemplateIdsCannotBeAdopted()
        {
            ProjectSource(); var info = Info(); Write(_project, ".agents/skills/project-fixture/SKILL.md", "unmanaged");
            Assert.IsTrue(ProjectPlan(info).Errors.Count > 0);
            string source = Path.Combine(_project, EmberAISkillInstaller.ProjectSourceDirectory);
            string catalog = Path.Combine(source, "catalog.json");
            File.WriteAllText(catalog, File.ReadAllText(catalog).Replace("project-fixture", Skill));
            Directory.Move(Path.Combine(source, "project-fixture"), Path.Combine(source, Skill));
            File.WriteAllText(Path.Combine(source, Skill, "SKILL.md"), "---\nname: " + Skill + "\ndescription: fixture\n---\nfixture");
            Assert.Throws<InvalidDataException>(() => ProjectPlan(info));
        }

        [Test]
        public void ExtensionSkill_DiscoveryExecutionHashAndModuleFailClosed_KeepSourceAndBackup()
        {
            string catalog = Path.Combine(SourceRoot(), "catalog.json");
            File.WriteAllText(catalog, File.ReadAllText(catalog).Replace("\"minimumTemplateVersion\":", "\"requiredExtensions\":[\"ember.fixture\"],\"requiredModuleScope\":\"fixture.module\",\"minimumTemplateVersion\":"));
            var info = Info();
            var identity = new EmberAISkillInstaller.ExtensionIdentity { id = "ember.fixture", contractVersion = 1, contentHash = "source-a" };
            EmberAISkillInstaller.TemplatePreview Plan(EmberAISkillInstaller.ExtensionIdentity[] enabled, string[] scopes) =>
                EmberAISkillInstaller.PreviewTemplateSkills(_project, _assets, new EmberAISkillInstaller.TemplateContext(info.id, info.version,
                    info.contentHash, info.id, "0.24.3", true, extensions: enabled, moduleScopes: scopes));
            Commit(info, Plan(Array.Empty<EmberAISkillInstaller.ExtensionIdentity>(), new[] { "fixture.module" }));
            Assert.IsFalse(File.Exists(Target()));
            Commit(info, Plan(new[] { identity }, new[] { "fixture.module" }));
            Assert.IsTrue(File.Exists(Target()));
            Assert.IsNull(EmberAISkillInstaller.GetTemplateSkillExecutionBlockReason(_project, Skill, info.id, info.version, info.contentHash,
                extensions: new[] { identity }, moduleScopes: new[] { "fixture.module" }));
            Assert.IsNotNull(EmberAISkillInstaller.GetTemplateSkillExecutionBlockReason(_project, Skill, info.id, info.version, info.contentHash));
            Assert.IsNotNull(EmberAISkillInstaller.GetTemplateSkillExecutionBlockReason(_project, Skill, info.id, info.version, info.contentHash,
                extensions: new[] { new EmberAISkillInstaller.ExtensionIdentity { id = identity.id, contractVersion = 1, contentHash = "source-b" } }, moduleScopes: new[] { "fixture.module" }));
            Assert.IsNotNull(EmberAISkillInstaller.GetTemplateSkillExecutionBlockReason(_project, Skill, info.id, info.version, info.contentHash,
                extensions: new[] { identity }));
            string sourceBefore = File.ReadAllText(Path.Combine(SourceRoot(), Skill, "SKILL.md"));
            File.AppendAllText(Target(), "local draft");
            var off = Plan(Array.Empty<EmberAISkillInstaller.ExtensionIdentity>(), new[] { "fixture.module" });
            Assert.IsTrue(off.NeedsBackupConfirmation);
            Commit(info, off);
            Assert.IsFalse(File.Exists(Target()));
            Assert.AreEqual(sourceBefore, File.ReadAllText(Path.Combine(SourceRoot(), Skill, "SKILL.md")));
            Assert.IsTrue(Directory.GetFiles(Path.Combine(_project, ".utmp/ember-ai-skills"), "SKILL.md", SearchOption.AllDirectories).Any(p => File.ReadAllText(p).Contains("local draft")));
        }

        [Test]
        public void ExtensionBindings_RejectOldSchemaTemplateVersionAndHash_WithoutChangingDraft()
        {
            var config = EmberExtensionProject.CreateConfiguration(new[] { EmberExtensionProject.FsmBlueprint });
            string id = config.ownerTemplateId, version = config.ownerTemplateVersion;
            Assert.IsTrue(EmberExtensionProject.ResolveConfiguration(config, id, version).IsEnabled(EmberExtensionProject.FsmBlueprint));
            Assert.IsFalse(EmberExtensionProject.ResolveConfiguration(config, id, "999.0.0").IsEnabled(EmberExtensionProject.FsmBlueprint));
            config.bindings.Single(x => x.id == EmberExtensionProject.Core).contentHash = "old-core";
            Assert.IsFalse(EmberExtensionProject.ResolveConfiguration(config, id, version).IsEnabled(EmberExtensionProject.FsmBlueprint));
            config = EmberExtensionProject.CreateConfiguration(new[] { EmberExtensionProject.FsmBlueprint });
            config.bindings.Single(x => x.id == EmberExtensionProject.FsmBlueprint).contentHash = "different";
            Assert.IsFalse(EmberExtensionProject.ResolveConfiguration(config, id, version).IsEnabled(EmberExtensionProject.FsmBlueprint));
            config = EmberExtensionProject.CreateConfiguration(new[] { EmberExtensionProject.FsmBlueprint });
            config.schemaVersion = 1;
            string before = JsonUtility.ToJson(config);
            Assert.IsFalse(EmberExtensionProject.ResolveConfiguration(config, id, version).IsEnabled(EmberExtensionProject.FsmBlueprint));
            Assert.AreEqual(before, JsonUtility.ToJson(config));
        }

        [Test] public void ExtensionDeclarationChangedAfterPreview_RejectsSkillCommit()
        {
            var info = Info(); var preview = Preview(info);
            string declaration = Path.Combine(_root, "extensions.json"); File.WriteAllText(declaration, "first");
            EmberAISkillInstaller.BindReadOnlyInput(preview, declaration);
            File.WriteAllText(declaration, "changed");
            Assert.Throws<IOException>(() => EmberAISkillInstaller.PrepareTemplateSkills(preview, true, out _));
            Assert.IsFalse(File.Exists(Target()));
        }
        [Test] public void ExtensionTextFingerprintIgnoresCheckoutNewlineAndBomOnly()
        {
            var first = System.Text.Encoding.UTF8.GetBytes("line1\nline2\n");
            var second = System.Text.Encoding.UTF8.GetBytes("\uFEFFline1\r\nline2\r\n");
            CollectionAssert.AreEqual(EmberExtensionProject.CanonicalSource(first), EmberExtensionProject.CanonicalSource(second));
            CollectionAssert.AreNotEqual(EmberExtensionProject.CanonicalSource(first), EmberExtensionProject.CanonicalSource(System.Text.Encoding.UTF8.GetBytes("changed\n")));
        }
        [Test] public void MalformedExtensionArrayCannotSilentlyRemoveSkillGate()
        {
            string catalog = Path.Combine(SourceRoot(), "catalog.json");
            File.WriteAllText(catalog, File.ReadAllText(catalog).Replace("\"minimumTemplateVersion\":", "\"requiredExtensions\":\"ember.fixture\",\"minimumTemplateVersion\":"));
            Assert.Throws<InvalidDataException>(() => EmberAISkillInstaller.ValidateTemplateSkillSource(_assets));
        }

        [Test]
        public void ExtensionCompositionRollback_RestoresConfigurationAndDiscoveryTogether()
        {
            var info = Info(); Commit(info, Preview(info));
            string configuration = Path.Combine(_project, "ProjectSettings/EmberExtensions.json");
            Write(_project, "ProjectSettings/EmberExtensions.json", "before");
            File.AppendAllText(Target(), "preserved draft");
            string before = File.ReadAllText(Target());
            string incoming = Path.Combine(_root, "new-config.json"); File.WriteAllText(incoming, "after");
            string empty = Path.Combine(_root, "Empty", "Assets"); Directory.CreateDirectory(empty);
            var off = EmberAISkillInstaller.PreviewTemplateSkills(_project, empty, new EmberAISkillInstaller.TemplateContext(info.id, info.version,
                info.contentHash, info.id, "0.24.3", true, true));
            var prepared = EmberAISkillInstaller.PrepareTemplateSkills(off, true, out _).Select(x => new TemplateTransactionTarget(x.StagedPath, x.DestinationPath, x.Remove)).ToList();
            prepared.Insert(0, new TemplateTransactionTarget(incoming, configuration));
            Assert.Throws<IOException>(() => EmberTemplateTransaction.CommitPreparedTargets(prepared, i => { if (i == 1) throw new IOException("injected"); }));
            Assert.AreEqual("before", File.ReadAllText(configuration)); Assert.AreEqual(before, File.ReadAllText(Target()));
        }

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
        public void AllTemplateSkillSources_HaveValidEncodingAndFrontmatter()
        {
            EmberAISkillInstaller.ValidateTemplateSkillSource(Application.dataPath);
            string templates = Path.Combine(Application.dataPath, "../Packages/com.ember/Templates~");
            foreach (string directory in Directory.GetDirectories(templates))
            {
                if (File.Exists(Path.Combine(directory, "template.json")))
                    EmberAISkillInstaller.ValidateTemplateSkillSource(Path.Combine(directory, "Assets"));
            }
        }

        [TestCase("\n")]
        [TestCase("\r\n")]
        public void DeploymentAndIndependentUpdate_PreserveUtf8BytesAndLocalBackup(string newline)
        {
            string path = Path.Combine(SourceRoot(), Skill, "SKILL.md");
            string text = "---" + newline + "name: " + Skill + newline
                + "description: 中文编码测试" + newline + "---" + newline + "正文" + newline;
            byte[] original = new System.Text.UTF8Encoding(false, true).GetBytes(text);
            File.WriteAllBytes(path, original);
            var old = Info(); Commit(old, Preview(old));
            string local = Path.Combine(_project, "Assets", EmberAISkillInstaller.TemplateSourceDirectory, Skill, "SKILL.md");
            CollectionAssert.AreEqual(original, File.ReadAllBytes(local));
            CollectionAssert.AreEqual(original, File.ReadAllBytes(Target()));
            // A legacy BOM/local edit must still require explicit backup, never silent normalization.
            File.WriteAllText(Target(), text + "local edit", new System.Text.UTF8Encoding(true));
            byte[] edited = File.ReadAllBytes(Target());
            var next = Info(version: "1.0.1");
            var plan = Independent(next, old);
            Assert.IsTrue(plan.NeedsBackupConfirmation);
            Assert.Throws<InvalidOperationException>(() => EmberProjectSetup.CommitTemplateSkillUpdate(plan, false));
            CollectionAssert.AreEqual(edited, File.ReadAllBytes(Target()));
            EmberProjectSetup.CommitTemplateSkillUpdate(plan, true);
            CollectionAssert.AreEqual(original, File.ReadAllBytes(Target()));
            CollectionAssert.AreEqual(original, File.ReadAllBytes(local));
            Assert.IsTrue(Directory.GetFiles(Path.Combine(_project, ".utmp/ember-ai-skills"), "SKILL.md", SearchOption.AllDirectories)
                .Any(p => File.ReadAllBytes(p).SequenceEqual(edited)));
        }

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
        [Test]
        public void SkillSources_KeepLiteralVersionExamplesDuringFullDeployment()
        {
            Source(body: "Generated by Ember Setup v0.1.0");
            var info = Info(); Commit(info, Preview(info));
            Assert.IsFalse(Independent(info, info).NeedsBackupConfirmation);
            StringAssert.Contains("v0.1.0", File.ReadAllText(Path.Combine(_project, "Assets",
                EmberAISkillInstaller.TemplateSourceDirectory, Skill, "SKILL.md")));
        }

        [Test]
        public void IndependentUpdate_GuidCollisionOutsideSkillSourceBlocksBeforeMutation()
        {
            EnsureMetadata(_assets);
            var old = Info(); Commit(old, Preview(old));
            Source(body: "next"); var next = Info(version: "1.1.0");
            var plan = Independent(next, old);
            Write(_project, "Assets/Game/Module/user.txt.meta", File.ReadAllText(Path.Combine(SourceRoot(), Skill, "SKILL.md.meta")));
            string before = TreeFingerprint(_project);
            StringAssert.Contains("GUID", Assert.Throws<InvalidOperationException>(() => Independent(next, old)).Message);
            Assert.Throws<InvalidOperationException>(() => EmberProjectSetup.CommitTemplateSkillUpdate(plan, true));
            Assert.AreEqual(before, TreeFingerprint(_project));
        }

        [Test]
        public void IndependentUpdate_PreservesBusinessAndIdentity_AndRemainsExecutable()
        {
            EnsureMetadata(_assets);
            var old = Info(); Commit(old, Preview(old));
            string[] business = { "Assets/Game/Module/Story.cs", "Assets/Game/Config/story.csv",
                "Assets/GameResource/Resources/Novel/image.png", "Assets/Scenes/Novel.unity", "Assets/Game/UI/page.prefab" };
            foreach (string file in business) Write(_project, file, "user content " + file);
            string record = File.ReadAllText(Path.Combine(_project, Record));
            Source(body: "new skill"); var next = Info(version: "1.1.0");
            var plan = Independent(next, old);
            Assert.IsTrue(plan.IsIndependentUpdate);
            Assert.IsFalse(plan.NeedsBackupConfirmation);
            Assert.AreEqual("1.0.0", plan.BusinessTemplateVersion);
            Assert.IsFalse(File.ReadAllText(Target()).Contains("new skill"));
            EmberProjectSetup.CommitTemplateSkillUpdate(plan, false);
            foreach (string file in business) Assert.AreEqual("user content " + file, File.ReadAllText(Path.Combine(_project, file)));
            Assert.AreEqual(record, File.ReadAllText(Path.Combine(_project, Record)));
            StringAssert.Contains("new skill", File.ReadAllText(Target()));
            Assert.AreEqual(File.ReadAllText(Path.Combine(SourceRoot(), Skill, "SKILL.md")),
                File.ReadAllText(Path.Combine(_project, "Assets", EmberAISkillInstaller.TemplateSourceDirectory, Skill, "SKILL.md")));
            Assert.IsNull(EmberAISkillInstaller.GetTemplateSkillExecutionBlockReason(_project, Skill, old.id, old.version, old.contentHash));
            Assert.IsFalse(Independent(next, old).HasChanges);
            Source(body: "future package only");
            Assert.IsNull(EmberAISkillInstaller.GetTemplateSkillExecutionBlockReason(_project, Skill, old.id, old.version, old.contentHash));
            Assert.IsNotNull(EmberAISkillInstaller.GetTemplateSkillExecutionBlockReason(_project, Skill, old.id, "9.0.0", old.contentHash));
        }

        [Test]
        public void IndependentUpdate_MinimumUsesBusinessVersion_AndRequiresSealedSource()
        {
            var old = Info(); Commit(old, Preview(old));
            Source(minTemplate: "1.1.0"); var next = Info(version: "1.2.0");
            Assert.IsNotEmpty(Independent(next, old).Errors);
            Assert.Throws<InvalidOperationException>(() => EmberProjectSetup.CommitTemplateSkillUpdate(Independent(next, old), true));
            next.versionedContentHash = "unsealed";
            Assert.Throws<InvalidOperationException>(() => Independent(next, old));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void IndependentUpdate_FailureRollsBackEveryTarget(int fault)
        {
            EnsureMetadata(_assets);
            var old = Info(); Commit(old, Preview(old));
            string before = TreeFingerprint(_project);
            Source(body: "next"); var next = Info(version: "1.1.0");
            Assert.Throws<IOException>(() => EmberProjectSetup.CommitTemplateSkillUpdate(Independent(next, old), false,
                i => { if (i == fault) throw new IOException("injected"); }));
            Assert.AreEqual(before, TreeFingerprint(_project));
        }

        [TestCase("source")]
        [TestCase("source-meta")]
        [TestCase("catalog")]
        [TestCase("incoming-meta")]
        [TestCase("identity")]
        public void IndependentUpdate_ChangedPreviewCannotWrite(string change)
        {
            EnsureMetadata(_assets);
            var old = Info(); Commit(old, Preview(old));
            Source(body: "next"); var next = Info(version: "1.1.0");
            var plan = Independent(next, old);
            string local = Path.Combine(_project, "Assets", EmberAISkillInstaller.TemplateSourceDirectory);
            string path = change == "source" ? Path.Combine(local, Skill, "SKILL.md")
                : change == "source-meta" ? local + ".meta"
                : change == "catalog" ? Path.Combine(local, "catalog.json")
                : change == "incoming-meta" ? SourceRoot() + ".meta" : Path.Combine(_project, Record);
            File.AppendAllText(path, "changed");
            string before = TreeFingerprint(_project);
            Assert.Throws<IOException>(() => EmberProjectSetup.CommitTemplateSkillUpdate(plan, true));
            Assert.AreEqual(before, TreeFingerprint(_project));
        }

        [Test]
        public void IndependentUpdate_LocalSourceAndDiscoveryRequireBackup_CancelWritesNothing()
        {
            var old = Info(); Commit(old, Preview(old));
            string local = Path.Combine(_project, "Assets", EmberAISkillInstaller.TemplateSourceDirectory, Skill, "SKILL.md");
            File.AppendAllText(local, "source edit"); File.AppendAllText(Target(), "discovery edit");
            Source(body: "next"); var plan = Independent(Info(version: "1.1.0"), old);
            Assert.IsTrue(plan.NeedsBackupConfirmation);
            string before = TreeFingerprint(_project);
            Assert.Throws<InvalidOperationException>(() => EmberProjectSetup.CommitTemplateSkillUpdate(plan, false));
            Assert.AreEqual(before, TreeFingerprint(_project));
            EmberProjectSetup.CommitTemplateSkillUpdate(plan, true);
            string[] backups = Directory.GetFiles(Path.Combine(_project, ".utmp/ember-ai-skills"), "SKILL.md", SearchOption.AllDirectories);
            Assert.IsTrue(backups.Any(p => p.Contains(".template-source") && File.ReadAllText(p).Contains("source edit")));
            Assert.IsTrue(backups.Any(p => File.ReadAllText(p).Contains("discovery edit")));
        }

        [Test]
        public void IndependentUpdate_LegacyStateMigrates_AndFullDeploymentResetsBaseline()
        {
            var old = Info(); Commit(old, Preview(old));
            string path = Path.Combine(_project, ".agents/ember-ai-skills.json");
            var state = JsonUtility.FromJson<EmberAISkillInstaller.State>(File.ReadAllText(path));
            state.schemaVersion = 2; state.templateBaseline = null;
            File.WriteAllText(path, JsonUtility.ToJson(state));
            Source(body: "next"); var next = Info(version: "1.1.0");
            var plan = Independent(next, old);
            Assert.IsTrue(plan.NeedsBackupConfirmation);
            EmberProjectSetup.CommitTemplateSkillUpdate(plan, true);
            Assert.AreEqual(3, JsonUtility.FromJson<EmberAISkillInstaller.State>(File.ReadAllText(path)).schemaVersion);
            Assert.Throws<InvalidOperationException>(() => Commit(next, Independent(next, old)));
            Commit(next, Preview(next, old.id));
            Assert.IsNull(EmberAISkillInstaller.GetTemplateSkillExecutionBlockReason(_project, Skill, next.id, next.version, next.contentHash));
            Assert.IsFalse(Independent(next, next).HasChanges);
        }
        #endregion

        #region 内部方法
        private void ProjectSource(string version = "1.0.0", string body = "project source")
        {
            string root = Path.Combine(_project, EmberAISkillInstaller.ProjectSourceDirectory);
            Write(root, "project-fixture/SKILL.md", "---\nname: project-fixture\ndescription: project fixture\n---\n" + body);
            Write(root, "catalog.json", "{\"schemaVersion\":3,\"skills\":[{\"id\":\"project-fixture\",\"projectId\":\"fixture-project\",\"version\":\"" + version
                + "\",\"templateId\":\"fixture\",\"minimumFrameworkVersion\":\"0.1.0\",\"minimumTemplateVersion\":\"1.0.0\",\"maximumTemplateVersion\":\"1.9.9\",\"requiredExtensions\":[\"ember.fixture\"],\"requiredModuleScope\":\"fixture.module\"}]}");
        }
        private static EmberAISkillInstaller.ExtensionIdentity[] ProjectExtensions() => new[] {
            new EmberAISkillInstaller.ExtensionIdentity { id = "ember.fixture", contractVersion = 1, contentHash = "source-a" } };
        private string ProjectTarget() => Path.Combine(_project, ".agents/skills/project-fixture/SKILL.md");
        private EmberAISkillInstaller.TemplatePreview ProjectPlan(TemplateInfo info, EmberAISkillInstaller.ExtensionIdentity[] extensions = null, string[] scopes = null) =>
            EmberAISkillInstaller.PreviewTemplateSkills(_project, _assets, new EmberAISkillInstaller.TemplateContext(info.id, info.version, info.contentHash,
                "fixture", "0.25.0", true, extensions: extensions ?? ProjectExtensions(), moduleScopes: scopes ?? new[] { "fixture.module" }));
        private string ProjectBlock(TemplateInfo info, EmberAISkillInstaller.ExtensionIdentity[] extensions = null, string[] scopes = null) =>
            EmberAISkillInstaller.GetTemplateSkillExecutionBlockReason(_project, "project-fixture", info.id, info.version, info.contentHash,
                editing: false, extensions: extensions ?? ProjectExtensions(), moduleScopes: scopes ?? new[] { "fixture.module" }, frameworkVersion: "0.25.0", hasEuiApi: true);
        private static void ApplyProject(EmberAISkillInstaller.TemplatePreview plan)
        {
            var targets = EmberAISkillInstaller.PrepareTemplateSkills(plan, true, out _)
                .Select(t => new TemplateTransactionTarget(t.StagedPath, t.DestinationPath, t.Remove)).ToList();
            EmberAISkillInstaller.ValidateTemplatePreview(plan);
            EmberTemplateTransaction.CommitPreparedTargets(targets);
        }
        private EmberAISkillInstaller.TemplatePreview Independent(TemplateInfo next, TemplateInfo business) =>
            EmberProjectSetup.PreviewDeployedTemplateSkillUpdate(_project, _assets, next,
                new DeployedTemplateRecord { templateId = business.id, version = business.version, contentHash = business.contentHash }, "0.14.4", true);
        private static string TreeFingerprint(string root) => string.Join("\n", Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Where(p => !p.Contains(Path.DirectorySeparatorChar + ".utmp" + Path.DirectorySeparatorChar)
                && !p.Contains(Path.DirectorySeparatorChar + "Temp" + Path.DirectorySeparatorChar))
            .OrderBy(p => p, StringComparer.Ordinal).Select(p => p.Substring(root.Length) + ":" + Convert.ToBase64String(File.ReadAllBytes(p))));

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
