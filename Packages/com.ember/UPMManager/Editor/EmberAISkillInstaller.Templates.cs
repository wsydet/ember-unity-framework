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
            public TemplateContext(string templateId, string version, string contentHash,
                string previousTemplateId, string frameworkVersion, bool hasEuiApi, bool editing = false)
            {
                TemplateId = templateId; Version = version; ContentHash = contentHash;
                PreviousTemplateId = previousTemplateId; FrameworkVersion = frameworkVersion; HasEuiApi = hasEuiApi;
                Editing = editing;
            }
        }

        /// <summary>只读预览；应用必须复核来源、发现副本和安装记录。</summary>
        public sealed class TemplatePreview
        {
            internal string ProjectRoot, SourceRoot, CatalogHash, StateHash;
            internal TemplateContext Context;
            internal List<Package> Packages;
            internal List<TemplateEntry> Entries;
            internal readonly Dictionary<string, string> IdentityRecords = new Dictionary<string, string>();
            public IReadOnlyList<string> Differences { get; internal set; }
            public IReadOnlyList<string> Errors { get; internal set; }
            public bool NeedsBackupConfirmation { get; internal set; }
            public bool HasChanges { get; internal set; }
            public string TemplateId => Context.TemplateId;
            public string TemplateVersion => Context.Version;
            public bool IsEditing => Context.Editing;
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
            if (!Same(Snapshot(destination), files)) throw new IOException("技能暂存校验失败。");
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
            var installed = ReadState(projectRoot).skills.FirstOrDefault(s => s.id == id && s.ownerKind == "template");
            if (installed == null) return "此技能没有模板所有权记录。";
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
            string source = Within(sourceAssets, TemplateSourceDirectory);
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
            foreach (var foreign in state.skills.Where(s => s.ownerKind == "template" && !owned.Contains(s)))
                errors.Add(foreign.id + ": 所有权模板 " + foreign.templateId
                    + " 与正式当前模板记录不一致。请先恢复匹配的记录与备份，不能删除或接管其他模板技能。");
            // A conflicting owner is never silently adopted, even if bytes happen to match.
            foreach (var package in packages)
            {
                var definition = package.Definition;
                string incompatible = Incompatibility(definition, context.FrameworkVersion, context.HasEuiApi);
                if (incompatible != null) errors.Add(definition.id + ": " + incompatible);
                if (Version.Parse(context.Version) < Version.Parse(definition.minimumTemplateVersion))
                    errors.Add(definition.id + ": 需要包含此技能的模板版本 >= " + definition.minimumTemplateVersion);
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
            return new TemplatePreview
            {
                ProjectRoot = projectRoot, SourceRoot = source, Context = context, Packages = packages, Entries = entries,
                CatalogHash = Hash(Within(source, CatalogFile)), StateHash = Hash(Within(projectRoot, StateFile)),
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

        /// <summary>准备增删目标及 schema v2 记录；备份始终留在 .utmp，不在发现目录内。</summary>
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
            state.schemaVersion = 2;
            string pending = Path.Combine(transaction, "state.json");
            File.WriteAllText(pending, JsonUtility.ToJson(state, true) + "\n", Utf8);
            targets.Add(new PreparedSkillTarget(pending, statePath));
            ValidateTemplatePreview(preview);
            return targets.AsReadOnly();
        }
        #endregion
    }
}
