// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ember.UPMManager.Editor
{
    /// <summary>项目级技能安装：校验目录、检测本地修改、保留备份并事务替换单个技能。</summary>
    public static partial class EmberAISkillInstaller
    {
        #region 内部参数
        internal const string CatalogFile = "catalog.json";
        internal const string StateFile = ".agents/ember-ai-skills.json";
        private const int MaxFiles = 512;
        private const long MaxBytes = 16 * 1024 * 1024;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        [Serializable] internal sealed class Catalog
        {
            public int schemaVersion = 1;
            public Definition[] skills;
        }

        [Serializable] internal sealed class Definition
        {
            public string id;
            public string displayName;
            public string description;
            public string minimumFrameworkVersion;
            public string requiredCapability;
            public string templateId;
            public string minimumTemplateVersion;
        }

        [Serializable] internal sealed class Fingerprint
        {
            public string path;
            public string sha256;
        }

        [Serializable] internal sealed class Installation
        {
            public string id;
            public string repository;
            public string revision;
            public string commit;
            public string installedAtUtc;
            public Fingerprint[] files;
            public string ownerKind;
            public string templateId;
            public string templateVersion;
            public string templateContentHash;
            public string originTemplateId;
            public string templateMode;
            public string[] directories;
        }

        [Serializable] internal sealed class State
        {
            public int schemaVersion = 1;
            public List<Installation> skills = new List<Installation>();
        }

        internal sealed class Package
        {
            internal Definition Definition;
            internal string Directory;
            internal Fingerprint[] Files;
        }

        internal enum Status { Missing, Current, UpdateAvailable, LocalChanges, Unmanaged }

        internal sealed class Preview
        {
            internal Package Package;
            internal Status Status;
            internal string InstalledCommit;
            internal string InstalledTemplateId;
            internal Fingerprint[] LocalFiles;
            internal string StateHash;
            internal bool NeedsOverwrite => Status == EmberAISkillInstaller.Status.LocalChanges
                || Status == EmberAISkillInstaller.Status.Unmanaged;
        }
        #endregion

        #region 内部方法
        private static void CheckId(string id)
        {
            if (id == null || !Regex.IsMatch(id, @"\A[a-z0-9][a-z0-9-]{0,63}\z"))
                throw new InvalidDataException("技能目录名无效：" + id);
            if (Regex.IsMatch(id, @"\A(con|prn|aux|nul|com[1-9]|lpt[1-9])\z"))
                throw new InvalidDataException("技能目录不能使用系统设备名：" + id);
        }

        private static string Within(string root, string relative)
        {
            root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.IsNullOrEmpty(relative) || Path.IsPathRooted(relative)
                || relative.Split('/', '\\').Any(p => p == ".." || p == "." || p.Length == 0)
                || relative.Contains(":")) throw new InvalidDataException("技能路径无效。");
            string full = Path.GetFullPath(Path.Combine(root, relative));
            if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("技能路径超出项目目录。");
            // Reject junctions/symlinks in every existing ancestor, including the project root.
            for (string current = full; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
            {
                FileAttributes attributes;
                try { attributes = File.GetAttributes(current); }
                catch (FileNotFoundException) { continue; }
                catch (DirectoryNotFoundException) { continue; }
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("技能目录不能通过符号链接或 junction 写入：" + current);
            }
            return full;
        }

        private static IEnumerable<string> EnumerateFiles(string directory)
        {
            foreach (string entry in Directory.GetFileSystemEntries(directory).OrderBy(p => p, StringComparer.Ordinal))
            {
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("技能中不支持符号链接：" + entry);
                if ((attributes & FileAttributes.Directory) != 0)
                {
                    if (Path.GetFileName(entry) == ".git") throw new InvalidDataException("技能中不能包含嵌套 Git 仓库。");
                    foreach (string file in EnumerateFiles(entry)) yield return file;
                }
                else yield return entry;
            }
        }

        private static string Hash(string file)
        {
            if (!File.Exists(file)) return null;
            using var stream = File.OpenRead(file);
            using var algorithm = SHA256.Create();
            return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        private static Fingerprint[] Snapshot(string directory)
        {
            if (!Directory.Exists(directory)) return Array.Empty<Fingerprint>();
            var result = new List<Fingerprint>();
            long size = 0;
            foreach (string file in EnumerateFiles(directory))
            {
                size += new FileInfo(file).Length;
                if (result.Count >= MaxFiles || size > MaxBytes)
                    throw new InvalidDataException("单个技能超过 512 个文件或 16 MB，停止安装。");
                result.Add(new Fingerprint
                {
                    path = file.Substring(directory.TrimEnd(Path.DirectorySeparatorChar).Length + 1).Replace('\\', '/'),
                    sha256 = Hash(file)
                });
            }
            return result.OrderBy(f => f.path, StringComparer.Ordinal).ToArray();
        }

        private static bool Same(Fingerprint[] left, Fingerprint[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            var sortedLeft = left.OrderBy(f => f.path, StringComparer.Ordinal).ToArray();
            var sortedRight = right.OrderBy(f => f.path, StringComparer.Ordinal).ToArray();
            return sortedLeft.Zip(sortedRight, (a, b) => a.path == b.path && a.sha256 == b.sha256).All(equal => equal);
        }

        private static State ReadState(string projectRoot)
        {
            string path = Within(projectRoot, StateFile);
            if (!File.Exists(path)) return new State();
            var state = JsonUtility.FromJson<State>(File.ReadAllText(path, Utf8));
            if (state == null || (state.schemaVersion != 1 && state.schemaVersion != 2) || state.skills == null)
                throw new InvalidDataException("AI Skill 安装记录无效，请先检查 " + StateFile);
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in state.skills)
            {
                if (item == null || item.files == null) throw new InvalidDataException("AI Skill 安装记录不完整。");
                CheckId(item.id);
                if (!string.IsNullOrEmpty(item.ownerKind) && item.ownerKind != "template")
                    throw new InvalidDataException("未知技能所有权类型，请升级管理器。");
                if (item.ownerKind == "template")
                {
                    if (state.schemaVersion != 2) throw new InvalidDataException("模板所有权需要记录 schema v2。");
                    CheckId(item.templateId);
                    if (!Version.TryParse(item.templateVersion, out _) || string.IsNullOrEmpty(item.templateContentHash))
                        throw new InvalidDataException("模板技能所有权记录不完整。");
                    if (item.templateMode != "editing" && item.templateMode != "deployed")
                        throw new InvalidDataException("模板技能身份模式无效。");
                }
                if (!ids.Add(item.id) || item.files.Any(f => f == null || string.IsNullOrEmpty(f.path)
                        || !Regex.IsMatch(f.sha256 ?? "", @"\A[0-9a-f]{64}\z")))
                    throw new InvalidDataException("AI Skill 安装记录包含重复或无效项。");
                var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var file in item.files)
                {
                    Within(Path.Combine(projectRoot, ".agents/skills", item.id), file.path);
                    if (!paths.Add(file.path)) throw new InvalidDataException("技能记录包含重复文件路径。");
                }
            }
            return state;
        }
        #endregion

        #region 外部方法
        internal static List<Package> ReadCatalog(string skillsRoot, bool templateCatalog = false)
        {
            string catalogPath = Within(skillsRoot, CatalogFile);
            if (!File.Exists(catalogPath) || new FileInfo(catalogPath).Length > 128 * 1024)
                throw new InvalidDataException("所选版本缺少有效的 AI Skill 发布目录，请选择包含 catalog.json 的版本。");
            var catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(catalogPath, Utf8));
            if (catalog == null || catalog.schemaVersion != (templateCatalog ? 2 : 1) || catalog.skills == null
                || (!templateCatalog && catalog.skills.Length == 0) || catalog.skills.Length > 64)
                throw new InvalidDataException("AI Skill 发布目录为空或版本不受支持。");
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var packages = new List<Package>();
            foreach (var definition in catalog.skills)
            {
                if (definition == null) throw new InvalidDataException("技能定义为空。");
                CheckId(definition.id);
                if (templateCatalog)
                {
                    CheckId(definition.templateId);
                    if (!Version.TryParse(definition.minimumTemplateVersion, out _)
                        || !Version.TryParse(definition.minimumFrameworkVersion, out _))
                        throw new InvalidDataException("模板技能必须声明最低框架和模板版本：" + definition.id);
                }
                else if (!string.IsNullOrEmpty(definition.templateId) || !string.IsNullOrEmpty(definition.minimumTemplateVersion))
                    throw new InvalidDataException("通用目录不能分发模板技能，请通过项目中心部署模板。");
                if (!ids.Add(definition.id)) throw new InvalidDataException("技能目录重复：" + definition.id);
                string directory = Within(skillsRoot, definition.id);
                var files = Snapshot(directory);
                if (!files.Any(f => f.path == "SKILL.md")) throw new InvalidDataException("技能缺少 SKILL.md：" + definition.id);
                string markdown = File.ReadAllText(Path.Combine(directory, "SKILL.md"), Utf8).TrimStart('\uFEFF');
                var frontmatter = Regex.Match(markdown, @"\A---\r?\n([\s\S]*?)\r?\n---(?:\r?\n|$)");
                if (!frontmatter.Success || !Regex.IsMatch(frontmatter.Groups[1].Value,
                        @"(?m)^name:\s*" + Regex.Escape(definition.id) + @"\s*$"))
                    throw new InvalidDataException("技能名称与 SKILL.md 不一致：" + definition.id);
                packages.Add(new Package { Definition = definition, Directory = directory, Files = files });
            }
            return packages;
        }

        internal static string Incompatibility(Definition definition, string frameworkVersion, bool hasEuiApi)
        {
            if (!string.IsNullOrEmpty(definition.minimumFrameworkVersion)
                && (!Version.TryParse(definition.minimumFrameworkVersion, out var minimum)
                    || !Version.TryParse(frameworkVersion, out var current) || current < minimum))
                return "需要 Ember " + definition.minimumFrameworkVersion + " 或更新版本。";
            if (definition.requiredCapability == "eui-regenerate-v1" && !hasEuiApi)
                return "当前框架尚未提供 EUI 自动生成公共 API，请先升级框架并完成编译。";
            if (!string.IsNullOrEmpty(definition.requiredCapability) && definition.requiredCapability != "eui-regenerate-v1")
                return "此技能需要新版 AI Skill 管理器，请先升级框架。";
            return null;
        }

        internal static bool IsSourceProject(string projectRoot) =>
            File.Exists(Path.Combine(projectRoot, "Packages/com.ember/package.json"))
            && File.Exists(Path.Combine(projectRoot, ".agents/skills", CatalogFile));

        internal static Preview Inspect(string projectRoot, Package package)
        {
            CheckId(package.Definition.id);
            string directory = Within(projectRoot, ".agents/skills/" + package.Definition.id);
            if (File.Exists(directory)) throw new IOException("技能目标是文件而非目录：" + directory);
            var local = Snapshot(directory);
            var state = ReadState(projectRoot);
            var installation = state.skills.FirstOrDefault(s => s.id == package.Definition.id);
            Status status = !Directory.Exists(directory) ? Status.Missing
                : Same(local, package.Files) ? Status.Current
                : installation == null ? Status.Unmanaged
                : Same(local, installation.files) ? Status.UpdateAvailable : Status.LocalChanges;
            return new Preview
            {
                Package = package, Status = status, LocalFiles = local,
                InstalledCommit = installation?.commit, StateHash = Hash(Within(projectRoot, StateFile)),
                InstalledTemplateId = installation?.ownerKind == "template" ? installation.templateId : null
            };
        }

        /// <summary>返回旧版本备份目录；预览后发生变化时拒绝安装，不自动覆盖。</summary>
        internal static string Install(string projectRoot, Preview preview, string repository, string revision,
            string commit, bool allowOverwrite)
        {
            if (IsSourceProject(projectRoot)) throw new InvalidOperationException("框架开发仓库直接维护技能源文件，不通过更新器覆盖。");
            if (!string.IsNullOrEmpty(preview.Package.Definition.templateId)
                || ReadState(projectRoot).skills.Any(s => s.id == preview.Package.Definition.id && s.ownerKind == "template"))
                throw new InvalidOperationException("模板技能只能通过项目中心同步，通用安装不能覆盖模板所有权。");
            if (preview.NeedsOverwrite && !allowOverwrite) throw new InvalidOperationException("技能存在本地内容，请先确认备份并覆盖。");
            if (!Regex.IsMatch(commit ?? "", @"\A[0-9a-f]{40,64}\z")) throw new InvalidDataException("缺少有效的技能来源提交。");
            var fresh = Inspect(projectRoot, preview.Package);
            if (!Same(fresh.LocalFiles, preview.LocalFiles) || fresh.Status != preview.Status || fresh.StateHash != preview.StateHash)
                throw new IOException("技能或安装记录在预览后发生变化，请重新检查。");
            if (!Same(Snapshot(preview.Package.Directory), preview.Package.Files))
                throw new IOException("下载缓存已变化，请重新检查远程技能。");

            string id = preview.Package.Definition.id;
            string target = Within(projectRoot, ".agents/skills/" + id);
            string transaction = Within(projectRoot, ".utmp/ember-ai-skills/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
            string incoming = Path.Combine(transaction, "incoming", id);
            string backup = Path.Combine(transaction, "backup", id);
            string statePath = Within(projectRoot, StateFile);
            Directory.CreateDirectory(incoming);
            foreach (var file in preview.Package.Files)
            {
                string destination = Within(incoming, file.path);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(Within(preview.Package.Directory, file.path), destination);
            }
            if (!Same(Snapshot(incoming), preview.Package.Files)) throw new IOException("技能复制校验失败，项目内容未替换。");
            var state = ReadState(projectRoot);
            state.skills.RemoveAll(s => s.id == id);
            state.skills.Add(new Installation
            {
                id = id, repository = repository, revision = revision, commit = commit,
                installedAtUtc = DateTime.UtcNow.ToString("o"), files = preview.Package.Files
            });
            string pendingState = Path.Combine(transaction, "state.json");
            File.WriteAllText(pendingState, JsonUtility.ToJson(state, true) + "\n", Utf8);
            // Recheck after staging too, so edits made during copying are never silently replaced.
            fresh = Inspect(projectRoot, preview.Package);
            if (!Same(fresh.LocalFiles, preview.LocalFiles) || fresh.StateHash != preview.StateHash || fresh.Status != preview.Status)
                throw new IOException("暂存过程中技能发生变化，未替换，请重新检查。");
            bool movedOld = false, movedNew = false;
            try
            {
                Within(projectRoot, ".agents/skills/" + id);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                if (Directory.Exists(target))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    Directory.Move(target, backup); movedOld = true;
                }
                Directory.Move(incoming, target); movedNew = true;
                if (File.Exists(statePath)) File.Replace(pendingState, statePath, null);
                else File.Move(pendingState, statePath);
            }
            catch
            {
                // Preserve the failed incoming copy for diagnosis; restore the entire original directory.
                if (movedNew) Directory.Move(target, Path.Combine(transaction, "failed"));
                if (movedOld) Directory.Move(backup, target);
                throw;
            }
            return movedOld ? backup : null;
        }
        #endregion
    }
}
