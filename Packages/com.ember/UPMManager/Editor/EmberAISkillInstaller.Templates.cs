// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Ember.UPMManager.Editor
{
    public static partial class EmberAISkillInstaller
    {
        #region 内部参数
        /// <summary>相对于模板 Assets；属于模板内容 hash，不是 AI 客户端的发现目录。</summary>
        public const string TemplateSourceDirectory = "Game/Documentation/TemplateSkills";

        /// <summary>由项目中心正式编辑/部署记录提供的上下文，不通过技能目录推断模板身份。</summary>
        public sealed class TemplateContext
        {
            public string TemplateId { get; }
            public string Version { get; }
            public string ContentHash { get; }
            public string PreviousTemplateId { get; }
            public string FrameworkVersion { get; }
            public bool HasEuiApi { get; }
            public bool Editing { get; }
            public string BusinessVersion { get; }
            public string BusinessContentHash { get; }
            public bool UpdateSource { get; }
            public TemplateContext(string templateId, string version, string contentHash,
                string previousTemplateId, string frameworkVersion, bool hasEuiApi, bool editing = false,
                string businessVersion = null, string businessContentHash = null, bool updateSource = false)
            {
                TemplateId = templateId; Version = version; ContentHash = contentHash;
                PreviousTemplateId = previousTemplateId; FrameworkVersion = frameworkVersion; HasEuiApi = hasEuiApi;
                Editing = editing; BusinessVersion = businessVersion ?? version;
                BusinessContentHash = businessContentHash ?? contentHash; UpdateSource = updateSource;
            }
        }

        /// <summary>只读预览；应用必须复核来源、发现副本和安装记录。</summary>
        public sealed class TemplatePreview
        {
            internal string ProjectRoot, SourceRoot, CatalogHash, StateHash;
            internal TemplateContext Context;
            internal List<Package> Packages;
            internal List<TemplateEntry> Entries;
            internal TemplateSkillBaseline DesiredBaseline;
            internal Fingerprint[] LocalSourceFiles;
            internal string[] LocalSourceDirectories;
            internal bool LocalSourceExists;
            internal string LocalSourceMetaHash;
            internal readonly Dictionary<string, string> IdentityRecords = new Dictionary<string, string>();
            public IReadOnlyList<string> Differences { get; internal set; }
            public IReadOnlyList<string> Errors { get; internal set; }
            public bool NeedsBackupConfirmation { get; internal set; }
            public bool HasChanges { get; internal set; }
            public string ProjectRootPath => ProjectRoot;
            public string SourceAssetsPath { get; internal set; }
            public string TemplateId => Context.TemplateId;
            public string TemplateVersion => Context.Version;
            public string TemplateContentHash => Context.ContentHash;
            public string BusinessContentHash => Context.BusinessContentHash;
            public bool IsEditing => Context.Editing;
            public bool IsIndependentUpdate => Context.UpdateSource;
            public string BusinessTemplateVersion => Context.BusinessVersion;
        }

        internal sealed class TemplateEntry
        {
            internal string Id;
            internal Package Package;
            internal Fingerprint[] Local;
            internal bool Existed;
            internal string[] LocalDirectories, SourceDirectories;
        }

        /// <summary>安装器只准备内容；项目中心将这些目标加入同一模板文件事务。</summary>
        public sealed class PreparedSkillTarget
        {
            public string StagedPath { get; }
            public string DestinationPath { get; }
            public bool Remove { get; }
            internal PreparedSkillTarget(string staged, string destination, bool remove = false)
            { StagedPath = staged; DestinationPath = destination; Remove = remove; }
        }
        #endregion

        #region 内部方法
        private static Fingerprint[] SnapshotTemplateSource(string root) => Snapshot(root, MaxFiles * 64, MaxBytes * 64L);

        private static TemplateSkillBaseline CreateBaseline(TemplateContext context, string source) => new TemplateSkillBaseline
        {
            templateId = context.TemplateId, businessVersion = context.BusinessVersion,
            businessContentHash = context.BusinessContentHash, sourceVersion = context.Version,
            sourceContentHash = context.ContentHash, sourceExists = Directory.Exists(source),
            sourceFiles = SnapshotTemplateSource(source), sourceDirectories = SnapshotDirectories(source),
            sourceMetaHash = Hash(source + ".meta")
        };

        private static bool MatchesBusiness(TemplateSkillBaseline baseline, string id, string version, string hash) =>
            baseline != null && baseline.templateId == id && baseline.businessVersion == version && baseline.businessContentHash == hash;

        private static bool SameBaseline(TemplateSkillBaseline left, TemplateSkillBaseline right) =>
            (left == null && right == null) || (left != null && right != null && MatchesBusiness(left, right.templateId, right.businessVersion, right.businessContentHash)
            && left.sourceVersion == right.sourceVersion && left.sourceContentHash == right.sourceContentHash
            && left.sourceExists == right.sourceExists && Same(left.sourceFiles, right.sourceFiles)
            && left.sourceDirectories.SequenceEqual(right.sourceDirectories) && left.sourceMetaHash == right.sourceMetaHash);

        private static List<Package> ReadTemplatePackages(string sourceRoot)
        {
            Within(sourceRoot, CatalogFile);
            if (File.Exists(sourceRoot)) throw new IOException("模板技能源路径是文件而非目录。");
            if (!Directory.Exists(sourceRoot)) return new List<Package>();
            // An existing source directory without a catalog is an authoring error, not an empty template.
            return ReadCatalog(sourceRoot, true);
        }

        private static IEnumerable<string> FileDifferences(string id, Fingerprint[] before, Fingerprint[] after)
        {
            var left = before.ToDictionary(f => f.path, f => f.sha256, StringComparer.Ordinal);
            var right = after.ToDictionary(f => f.path, f => f.sha256, StringComparer.Ordinal);
            foreach (string path in left.Keys.Union(right.Keys).OrderBy(p => p, StringComparer.Ordinal))
                if (!left.ContainsKey(path)) yield return "+ " + id + "/" + path;
                else if (!right.ContainsKey(path)) yield return "- " + id + "/" + path;
                else if (left[path] != right[path]) yield return "~ " + id + "/" + path;
        }

        private static void CopySkillFiles(string source, string destination, Fingerprint[] files)
        {
            Directory.CreateDirectory(destination);
            foreach (string directory in SnapshotDirectories(source)) Directory.CreateDirectory(Within(destination, directory));
            foreach (var file in files)
            {
                string target = Within(destination, file.path);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(Within(source, file.path), target);
            }
            if (!Same(Snapshot(destination, MaxFiles * 64, MaxBytes * 64L), files)) throw new IOException("技能暂存校验失败。");
        }

        private static string[] SnapshotDirectories(string root)
        {
            var result = new List<string>();
            CollectDirectories(root, root, result);
            return result.OrderBy(p => p, StringComparer.Ordinal).ToArray();
        }

        private static void CollectDirectories(string root, string current, List<string> result)
        {
            if (!Directory.Exists(current)) return;
            foreach (string directory in Directory.GetDirectories(current))
            {
                string relative = directory.Substring(root.Length + 1).Replace('\\', '/');
                Within(root, relative);
                result.Add(relative);
                CollectDirectories(root, directory, result);
            }
        }
        #endregion

        #region 外部方法
        internal static IEnumerable<string> DescribeFileDifferences(Preview preview) =>
            FileDifferences(preview.Package.Definition.id, preview.LocalFiles, preview.Package.Files);

        /// <summary>保存/继承时验证清单与文件结构，不改变发现副本，也不要求下一封存版本已生效。</summary>
        public static void ValidateTemplateSkillSource(string assetsPath)
        {
            ReadTemplatePackages(Within(assetsPath, TemplateSourceDirectory));
        }

        /// <summary>只查询有明确模板所有权的安装记录。</summary>
        public static bool HasTemplateSkillOwnership(string projectRoot, string templateId = null) =>
            ReadState(projectRoot).skills.Any(s => s.ownerKind == "template" && (templateId == null || s.templateId == templateId));

        /// <summary>技能执行前只读检查归属，不回收已加载会话内容。</summary>
        public static string GetTemplateSkillExecutionBlockReason(string projectRoot, string id,
            string templateId, string version, string contentHash, bool editing = false)
        {
            CheckId(id);
            var state = ReadState(projectRoot);
            var installed = state.skills.FirstOrDefault(s => s.id == id && s.ownerKind == "template");
            if (installed == null) return "此技能没有模板所有权记录。";
            if (!editing && state.templateBaseline != null)
            {
                if (!MatchesBusiness(state.templateBaseline, templateId, version, contentHash))
                    return "技能独立基线与正式业务部署记录不一致，请在项目中心检查。";
                version = state.templateBaseline.sourceVersion;
                contentHash = state.templateBaseline.sourceContentHash;
            }
            if (string.IsNullOrEmpty(templateId) || installed.templateId != templateId
                || installed.templateVersion != version || installed.templateContentHash != contentHash)
                return "技能归属与正式编辑/部署模板不一致，请停止执行并在项目中心重新预览。";
            if (installed.templateMode != (editing ? "editing" : "deployed")) return "技能的开发/部署身份模式不一致。";
            if (!Same(Snapshot(Within(projectRoot, ".agents/skills/" + id)), installed.files))
                return "技能发现副本有本地修改，请先检查并重新同步。";
            return null;
        }

        /// <summary>绑定正式记录的预览指纹；编辑/部署记录变化会使计划失效。</summary>
        public static void BindTemplateIdentityRecord(TemplatePreview preview, string projectRelativePath)
        {
            string path = Within(preview.ProjectRoot, projectRelativePath);
            if (Directory.Exists(path)) throw new IOException("模板身份记录不是文件。");
            preview.IdentityRecords.Add(projectRelativePath, Hash(path));
        }

        /// <summary>生命周期提交入口必须确认计划仍绑定同一项目与模板快照。</summary>
        public static void ValidateTemplateBinding(TemplatePreview preview, string projectRoot, string sourceAssets,
            string templateId, string version, string contentHash)
        {
            if (preview == null || Path.GetFullPath(preview.ProjectRoot) != Path.GetFullPath(projectRoot)
                || Path.GetFullPath(preview.SourceRoot) != Within(sourceAssets, TemplateSourceDirectory)
                || preview.Context.TemplateId != templateId || preview.Context.Version != version
                || preview.Context.ContentHash != contentHash)
                throw new InvalidOperationException("技能计划与模板生命周期目标不一致。");
            ValidateTemplatePreview(preview);
        }

        /// <summary>只读检查；context 必须由模板生命周期调用方根据正式记录构建。</summary>
        public static TemplatePreview PreviewTemplateSkills(string projectRoot, string sourceAssets, TemplateContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            CheckId(context.TemplateId);
            if (!Version.TryParse(context.Version, out _) || string.IsNullOrEmpty(context.ContentHash))
                throw new InvalidDataException("模板技能缺少固定的模板版本/contentHash。");
            if (!Version.TryParse(context.BusinessVersion, out _) || string.IsNullOrEmpty(context.BusinessContentHash))
                throw new InvalidDataException("缺少正式业务部署基线，不能独立升级技能。");
            if (context.UpdateSource && (context.Editing || context.PreviousTemplateId != context.TemplateId))
                throw new InvalidOperationException("独立技能更新只能用于当前正式部署的同一个模板。");
            string source = Within(sourceAssets, TemplateSourceDirectory);
            Within(sourceAssets, TemplateSourceDirectory + ".meta");
            var desiredBaseline = context.Editing ? null : CreateBaseline(context, source);
            var packages = ReadTemplatePackages(source);
            if (Directory.Exists(Within(projectRoot, StateFile))) throw new IOException("AI Skill 安装记录路径是目录，请先修复。");
            var state = ReadState(projectRoot);
            var errors = new List<string>();
            var differences = new List<string>();
            var entries = new List<TemplateEntry>();
            bool confirm = false, changed = false;
            var desired = packages.ToDictionary(p => p.Definition.id, StringComparer.OrdinalIgnoreCase);
            var owned = state.skills.Where(s => s.ownerKind == "template"
                && s.templateId == context.PreviousTemplateId).ToList();
            if (!context.UpdateSource && packages.Count == 0 && owned.Count == 0 && state.templateBaseline == null)
                desiredBaseline = null; // Legacy templates without skills retain their no-op lifecycle.

            foreach (var foreign in state.skills.Where(s => s.ownerKind == "template" && !owned.Contains(s)))
                errors.Add(foreign.id + ": 所有权模板 " + foreign.templateId
                    + " 与正式当前模板记录不一致。请先恢复匹配的记录与备份，不能删除或接管其他模板技能。");
            // A conflicting owner is never silently adopted, even if bytes happen to match.
            foreach (var package in packages)
            {
                var definition = package.Definition;
                string incompatible = Incompatibility(definition, context.FrameworkVersion, context.HasEuiApi);
                if (incompatible != null) errors.Add(definition.id + ": " + incompatible);
                if (Version.Parse(context.BusinessVersion) < Version.Parse(definition.minimumTemplateVersion))
                    errors.Add(definition.id + ": 需要实际业务模板版本 >= " + definition.minimumTemplateVersion);
                var installed = state.skills.FirstOrDefault(s => s.id == definition.id);
                string target = Within(projectRoot, ".agents/skills/" + definition.id);
                if (File.Exists(target)) errors.Add(definition.id + ": 目标是文件。");
                if ((installed != null && !owned.Contains(installed))
                    || (installed == null && Directory.Exists(target)))
                    errors.Add(definition.id + ": 同名目录属于通用/其他模板/未管理技能。请先自行移走或改 ID，不能接管。");
                // Development repository generic source is protected even if it has no installation record.
                string genericCatalog = Within(projectRoot, ".agents/skills/catalog.json");
                if (File.Exists(genericCatalog))
                {
                    var generic = JsonUtility.FromJson<Catalog>(File.ReadAllText(genericCatalog, Utf8));
                    if (generic?.skills != null && generic.skills.Any(s => s.id == definition.id))
                        errors.Add(definition.id + ": 与通用技能维护源 ID 冲突。");
                }
            }
            foreach (string id in desired.Keys.Union(owned.Select(s => s.id), StringComparer.OrdinalIgnoreCase).OrderBy(s => s))
            {
                string target = Within(projectRoot, ".agents/skills/" + id);
                if (File.Exists(target)) throw new IOException("技能目标是文件：" + id);
                var local = Snapshot(target);
                var old = owned.FirstOrDefault(s => s.id == id);
                desired.TryGetValue(id, out var package);
                bool exists = Directory.Exists(target);
                var localDirectories = SnapshotDirectories(target);
                var sourceDirectories = package == null ? Array.Empty<string>() : SnapshotDirectories(package.Directory);
                bool modified = old != null && (!exists || !Same(local, old.files)
                    || (old.directories != null && !localDirectories.SequenceEqual(old.directories)));
                if (modified) { confirm = true; differences.Add("本地修改，需确认备份：" + id); }
                bool entryChanged = package == null || old == null || !Same(local, package.Files)
                    || !Same(old.files, package.Files) || old.originTemplateId != package.Definition.templateId
                    || !localDirectories.SequenceEqual(sourceDirectories)
                    || old.templateId != context.TemplateId || old.templateVersion != context.Version
                    || old.templateContentHash != context.ContentHash || old.templateMode != (context.Editing ? "editing" : "deployed");
                changed |= entryChanged;
                differences.Add((package == null ? "移出发现目录" : old == null ? "安装" : entryChanged ? "更新" : "已一致")
                    + " · 模板专属 · " + id + " · 来源模板 " + (package?.Definition.templateId ?? old.originTemplateId)
                    + " · " + context.TemplateId + " " + context.Version + (context.Editing ? "（编辑基线，允许草稿）" : "（部署版本）"));
                differences.AddRange(FileDifferences(id, local, package?.Files ?? Array.Empty<Fingerprint>()));
                entries.Add(new TemplateEntry { Id = id, Package = package, Local = local, Existed = exists,
                    LocalDirectories = localDirectories, SourceDirectories = sourceDirectories });
            }
            string localSource = Within(projectRoot, "Assets/" + TemplateSourceDirectory);
            Within(projectRoot, "Assets/" + TemplateSourceDirectory + ".meta");
            Fingerprint[] localSourceFiles = null;
            string[] localSourceDirectories = null;
            bool localSourceExists = false;
            string localSourceMetaHash = null;
            if (context.UpdateSource)
            {
                if (File.Exists(localSource) || Directory.Exists(localSource + ".meta"))
                    throw new IOException("项目技能源路径类型错误。");
                if (state.templateBaseline != null && !MatchesBusiness(state.templateBaseline,
                        context.TemplateId, context.BusinessVersion, context.BusinessContentHash))
                    errors.Add("技能独立基线与业务部署身份不一致，请先检查记录。");
                foreach (var old in owned)
                {
                    string expectedVersion = state.templateBaseline?.sourceVersion ?? context.BusinessVersion;
                    string expectedHash = state.templateBaseline?.sourceContentHash ?? context.BusinessContentHash;
                    if (old.templateMode != "deployed" || old.templateVersion != expectedVersion || old.templateContentHash != expectedHash)
                        errors.Add(old.id + ": 旧技能归属与正式业务/技能源基线不一致，不能自动接管。");
                }
                localSourceFiles = SnapshotTemplateSource(localSource);
                localSourceDirectories = SnapshotDirectories(localSource);
                localSourceExists = Directory.Exists(localSource);
                localSourceMetaHash = Hash(localSource + ".meta");
                bool sourceChanged = localSourceExists != desiredBaseline.sourceExists
                    || !Same(localSourceFiles, desiredBaseline.sourceFiles)
                    || !localSourceDirectories.SequenceEqual(desiredBaseline.sourceDirectories)
                    || localSourceMetaHash != desiredBaseline.sourceMetaHash;
                var baseline = state.templateBaseline;
                bool localModified = baseline == null ? sourceChanged && (localSourceExists || localSourceMetaHash != null)
                    : localSourceExists != baseline.sourceExists || !Same(localSourceFiles, baseline.sourceFiles)
                        || !localSourceDirectories.SequenceEqual(baseline.sourceDirectories) || localSourceMetaHash != baseline.sourceMetaHash;
                if (localModified)
                {
                    confirm = true;
                    differences.Add("技能源有本地修改或旧版未记录其指纹；须先备份整个 TemplateSkills 目录及 .meta。");
                }
                changed |= sourceChanged;
                differences.Insert(0, "仅更新模板技能：业务部署保持 " + context.BusinessVersion + "；技能源 -> " + context.Version
                    + "。不修改剧情、配表、图片、Prefab、场景或业务代码。");
                differences.AddRange(FileDifferences("Assets/" + TemplateSourceDirectory, localSourceFiles, desiredBaseline.sourceFiles));
            }
            bool baselineChanged = context.Editing ? state.templateBaseline != null : !SameBaseline(state.templateBaseline, desiredBaseline);
            changed |= baselineChanged;
            if (baselineChanged) differences.Add("更新独立技能基线记录；业务部署记录保持原有生命周期。");
            return new TemplatePreview
            {
                ProjectRoot = projectRoot, SourceRoot = source, SourceAssetsPath = sourceAssets, Context = context, Packages = packages, Entries = entries,
                CatalogHash = Hash(Within(source, CatalogFile)), StateHash = Hash(Within(projectRoot, StateFile)),
                DesiredBaseline = desiredBaseline, LocalSourceFiles = localSourceFiles, LocalSourceDirectories = localSourceDirectories,
                LocalSourceExists = localSourceExists, LocalSourceMetaHash = localSourceMetaHash,
                Differences = differences.AsReadOnly(), Errors = errors.AsReadOnly(),
                NeedsBackupConfirmation = confirm, HasChanges = changed
            };
        }

        /// <summary>提交前再次调用；任何预览后变化均拒绝，不自动扩大授权范围。</summary>
        public static void ValidateTemplatePreview(TemplatePreview preview)
        {
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            if (preview.Errors.Count > 0) throw new InvalidOperationException(string.Join("\n", preview.Errors));
            foreach (var record in preview.IdentityRecords)
                if (Directory.Exists(Within(preview.ProjectRoot, record.Key))
                    || Hash(Within(preview.ProjectRoot, record.Key)) != record.Value)
                    throw new IOException("正式模板身份记录在预览后变化，请重新预览。");
            if (Directory.Exists(Within(preview.ProjectRoot, StateFile))
                || Hash(Within(preview.ProjectRoot, StateFile)) != preview.StateHash
                || Hash(Within(preview.SourceRoot, CatalogFile)) != preview.CatalogHash)
                throw new IOException("技能清单或安装记录在预览后变化，请重新预览。");
            Within(Path.GetDirectoryName(preview.SourceRoot), Path.GetFileName(preview.SourceRoot) + ".meta");
            if (preview.DesiredBaseline != null && !SameBaseline(preview.DesiredBaseline, CreateBaseline(preview.Context, preview.SourceRoot)))
                throw new IOException("完整技能源或根目录 .meta 在预览后变化，请重新预览。");
            if (preview.IsIndependentUpdate)
            {
                string source = Within(preview.ProjectRoot, "Assets/" + TemplateSourceDirectory);
                Within(preview.ProjectRoot, "Assets/" + TemplateSourceDirectory + ".meta");
                if (File.Exists(source) || Directory.Exists(source + ".meta") || Directory.Exists(source) != preview.LocalSourceExists
                    || !Same(SnapshotTemplateSource(source), preview.LocalSourceFiles)
                    || !SnapshotDirectories(source).SequenceEqual(preview.LocalSourceDirectories)
                    || Hash(source + ".meta") != preview.LocalSourceMetaHash)
                    throw new IOException("项目技能源在预览后变化，请重新预览。");
            }
            foreach (var entry in preview.Entries)
            {
                string target = Within(preview.ProjectRoot, ".agents/skills/" + entry.Id);
                if (File.Exists(target) || Directory.Exists(target) != entry.Existed || !Same(Snapshot(target), entry.Local))
                    throw new IOException("项目技能在预览后变化：" + entry.Id);
                if (!SnapshotDirectories(target).SequenceEqual(entry.LocalDirectories)
                    || (entry.Package != null && !SnapshotDirectories(entry.Package.Directory).SequenceEqual(entry.SourceDirectories)))
                    throw new IOException("技能目录结构在预览后变化：" + entry.Id);
                if (entry.Package != null && !Same(Snapshot(entry.Package.Directory), entry.Package.Files))
                    throw new IOException("模板技能源文件在预览后变化：" + entry.Id);
            }
        }

        /// <summary>准备增删目标及 schema v3 记录；备份始终留在 .utmp，不在发现目录内。</summary>
        public static IReadOnlyList<PreparedSkillTarget> PrepareTemplateSkills(TemplatePreview preview,
            bool allowBackup, out string backupRoot)
        {
            ValidateTemplatePreview(preview);
            if (preview.NeedsBackupConfirmation && !allowBackup)
                throw new InvalidOperationException("模板技能有本地修改；取消操作或明确确认备份后重试。");
            var targets = new List<PreparedSkillTarget>();
            backupRoot = null;
            if (!preview.HasChanges) return targets.AsReadOnly();
            string transaction = Within(preview.ProjectRoot, ".utmp/ember-ai-skills/" + Guid.NewGuid().ToString("N"));
            backupRoot = Path.Combine(transaction, "backup");
            var state = ReadState(preview.ProjectRoot);
            string statePath = Within(preview.ProjectRoot, StateFile);
            Directory.CreateDirectory(backupRoot);
            if (File.Exists(statePath)) File.Copy(statePath, Path.Combine(backupRoot, "ember-ai-skills.json"));
            foreach (var entry in preview.Entries)
            {
                string target = Within(preview.ProjectRoot, ".agents/skills/" + entry.Id);
                if (entry.Existed) CopySkillFiles(target, Path.Combine(backupRoot, entry.Id), entry.Local);
                string staged = Path.Combine(transaction, "incoming", entry.Id);
                if (entry.Package != null) CopySkillFiles(entry.Package.Directory, staged, entry.Package.Files);
                targets.Add(new PreparedSkillTarget(staged, target, entry.Package == null));
                state.skills.RemoveAll(s => s.id == entry.Id && s.ownerKind == "template"
                    && s.templateId == preview.Context.PreviousTemplateId);
                if (entry.Package != null)
                    state.skills.Add(new Installation
                    {
                        id = entry.Id, ownerKind = "template", templateId = preview.Context.TemplateId,
                        templateVersion = preview.Context.Version, templateContentHash = preview.Context.ContentHash,
                        originTemplateId = entry.Package.Definition.templateId, revision = preview.Context.Version,
                        templateMode = preview.Context.Editing ? "editing" : "deployed",
                        directories = entry.SourceDirectories,
                        repository = "package:com.ember", files = entry.Package.Files, installedAtUtc = DateTime.UtcNow.ToString("o")
                    });
            }
            if (preview.IsIndependentUpdate)
            {
                string destination = Within(preview.ProjectRoot, "Assets/" + TemplateSourceDirectory);
                string stagedSource = Path.Combine(transaction, "template-source");
                if (preview.LocalSourceExists) CopySkillFiles(destination, Path.Combine(backupRoot, ".template-source"), preview.LocalSourceFiles);
                if (preview.LocalSourceMetaHash != null) File.Copy(destination + ".meta", Path.Combine(backupRoot, ".template-source.meta"));
                if (preview.DesiredBaseline.sourceExists) CopySkillFiles(preview.SourceRoot, stagedSource, preview.DesiredBaseline.sourceFiles);
                targets.Add(new PreparedSkillTarget(stagedSource, destination, !preview.DesiredBaseline.sourceExists));
                string stagedMeta = stagedSource + ".meta";
                if (preview.DesiredBaseline.sourceMetaHash != null) File.Copy(preview.SourceRoot + ".meta", stagedMeta);
                targets.Add(new PreparedSkillTarget(stagedMeta, destination + ".meta", preview.DesiredBaseline.sourceMetaHash == null));
            }
            state.schemaVersion = 3;
            state.templateBaseline = preview.DesiredBaseline;
            string pending = Path.Combine(transaction, "state.json");
            File.WriteAllText(pending, JsonUtility.ToJson(state, true) + "\n", Utf8);
            targets.Add(new PreparedSkillTarget(pending, statePath));
            ValidateTemplatePreview(preview);
            return targets.AsReadOnly();
        }
        #endregion
    }
}
