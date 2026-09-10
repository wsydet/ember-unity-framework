// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEditor;
using UnityEngine;

namespace Ember.Table.Editor
{
    public enum EmberTableArtifactActionKind
    {
        Create,
        Replace,
        Unchanged,
        DeleteOwnedOrphan,
    }

    public sealed class EmberTableArtifactAction
    {
        public EmberTableArtifactActionKind Kind { get; }
        public string AssetPath { get; }

        public EmberTableArtifactAction(EmberTableArtifactActionKind kind, string assetPath)
        {
            Kind = kind;
            AssetPath = assetPath;
        }
    }

    [Serializable]
    internal sealed class EmberTableArtifactManifest
    {
        public string generator = "Ember.Table.Editor";
        public int formatVersion = 1;
        public List<string> paths = new List<string>();
    }

    /// <summary>生成器所有权清单、零写入预检和可回滚整批提交。</summary>
    public static class EmberTableArtifactBatch
    {
        public const string MANIFEST_PATH =
            EmberTableCodeGenerator.GENERATED_ROOT + "/EmberTableArtifacts.manifest.json";

        internal static string ProjectRootOverrideForTests { get; set; }
        internal static Func<string, bool> CommitFailureForTests { get; set; }

        public static bool TryPreview(
            IList<EmberTableArtifactContent> contents,
            out IReadOnlyList<EmberTableArtifactAction> actions,
            out EmberTableDiagnostic diagnostic)
        {
            actions = Array.Empty<EmberTableArtifactAction>();
            diagnostic = null;
            if (!TryPrepare(contents, out List<EmberTableArtifactContent> prepared, out HashSet<string> oldOwned, out diagnostic))
                return false;

            var current = new HashSet<string>(prepared.Select(item => item.AssetPath), StringComparer.Ordinal);
            List<string> orphans = oldOwned
                .Where(item => !current.Contains(item))
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToList();
            actions = BuildActions(prepared, orphans);
            return true;
        }

        public static bool TryCommit(
            IList<EmberTableArtifactContent> contents,
            out IReadOnlyList<EmberTableArtifactAction> actions,
            out EmberTableDiagnostic diagnostic)
        {
            actions = Array.Empty<EmberTableArtifactAction>();
            diagnostic = null;
            if (!TryPreview(contents, out actions, out diagnostic)
                || !TryPrepare(contents, out List<EmberTableArtifactContent> prepared, out HashSet<string> oldOwned, out diagnostic))
                return false;

            var newPaths = new HashSet<string>(prepared.Select(item => item.AssetPath), StringComparer.Ordinal);
            var orphans = oldOwned.Where(item => !newPaths.Contains(item)).OrderBy(item => item, StringComparer.Ordinal).ToList();
            return TryCommitPrepared(prepared, orphans, out diagnostic);
        }

        /// <summary>确认一组生成物仍由 Manifest 持有且内容与期望完全一致。</summary>
        public static bool TryCheckOwnedArtifactsCurrent(
            IList<EmberTableArtifactContent> contents,
            out EmberTableDiagnostic diagnostic)
        {
            if (!TryPrepareOwnedUpdates(contents, out List<EmberTableArtifactContent> prepared, out diagnostic))
                return false;
            for (int i = 0; i < prepared.Count; i++)
            {
                EmberTableArtifactContent content = prepared[i];
                string fullPath = ToFullPath(content.AssetPath);
                if (!File.Exists(fullPath) || !BytesEqual(File.ReadAllBytes(fullPath), content.Bytes))
                    return Fail(
                        EmberTableErrorCode.ArtifactStale,
                        "Generated Binding or Catalog changed. Export all tables before exporting one table.",
                        content.AssetPath,
                        out diagnostic);
            }
            return true;
        }

        /// <summary>只替换 Manifest 已持有的指定产物；不改 Manifest，也不清理其他产物。</summary>
        public static bool TryCommitOwned(
            IList<EmberTableArtifactContent> contents,
            out IReadOnlyList<EmberTableArtifactAction> actions,
            out EmberTableDiagnostic diagnostic)
        {
            actions = Array.Empty<EmberTableArtifactAction>();
            if (!TryPrepareOwnedUpdates(contents, out List<EmberTableArtifactContent> prepared, out diagnostic))
                return false;
            actions = BuildActions(prepared, Array.Empty<string>());
            return TryCommitPrepared(prepared, Array.Empty<string>(), out diagnostic);
        }

        private static bool TryCommitPrepared(
            IList<EmberTableArtifactContent> prepared,
            IList<string> orphans,
            out EmberTableDiagnostic diagnostic)
        {
            diagnostic = null;
            string projectRoot = ProjectRoot;
            string transactionId = Guid.NewGuid().ToString("N");
            string stageRoot = Path.Combine(projectRoot, "Library", "EmberTableStage", transactionId);
            string contentRoot = Path.Combine(stageRoot, "content");
            string backupRoot = Path.Combine(stageRoot, "backup");
            var originals = new HashSet<string>(StringComparer.Ordinal);
            var created = new List<string>();
            var temporaryPaths = new List<string>();

            try
            {
                var changed = prepared.Where(content =>
                {
                    string fullPath = ToFullPath(content.AssetPath);
                    return !File.Exists(fullPath) || !BytesEqual(File.ReadAllBytes(fullPath), content.Bytes);
                }).ToList();
                if (changed.Count == 0 && orphans.Count == 0) return true;

                for (int i = 0; i < changed.Count; i++)
                {
                    string staged = Path.Combine(contentRoot, changed[i].AssetPath.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(staged));
                    File.WriteAllBytes(staged, changed[i].Bytes);
                }

                var originalCandidates = changed.Select(item => item.AssetPath).Concat(orphans).ToList();
                for (int i = 0; i < orphans.Count; i++)
                    if (File.Exists(ToFullPath(orphans[i]) + ".meta")) originalCandidates.Add(orphans[i] + ".meta");
                foreach (string assetPath in originalCandidates.Distinct(StringComparer.Ordinal))
                {
                    string fullPath = ToFullPath(assetPath);
                    if (!File.Exists(fullPath)) continue;
                    originals.Add(assetPath);
                    string backup = Path.Combine(backupRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    File.Copy(fullPath, backup, true);
                }

                AssetDatabase.DisallowAutoRefresh();
                try
                {
                    foreach (EmberTableArtifactContent content in changed
                                 .OrderBy(item => item.AssetPath == MANIFEST_PATH ? 1 : 0)
                                 .ThenBy(item => item.AssetPath, StringComparer.Ordinal))
                    {
                        if (CommitFailureForTests?.Invoke(content.AssetPath) == true)
                            throw new IOException("Injected artifact commit failure.");
                        string target = ToFullPath(content.AssetPath);
                        string staged = Path.Combine(contentRoot, content.AssetPath.Replace('/', Path.DirectorySeparatorChar));
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        string temporary = target + ".embertable." + transactionId + ".tmp";
                        temporaryPaths.Add(temporary);
                        File.Copy(staged, temporary, true);
                        if (File.Exists(target)) File.Replace(temporary, target, null);
                        else
                        {
                            File.Move(temporary, target);
                            created.Add(content.AssetPath);
                        }
                    }

                    for (int i = 0; i < orphans.Count; i++)
                    {
                        string orphan = ToFullPath(orphans[i]);
                        if (File.Exists(orphan)) File.Delete(orphan);
                        if (File.Exists(orphan + ".meta")) File.Delete(orphan + ".meta");
                    }
                }
                catch
                {
                    for (int i = 0; i < temporaryPaths.Count; i++)
                        if (File.Exists(temporaryPaths[i])) File.Delete(temporaryPaths[i]);
                    for (int i = 0; i < created.Count; i++)
                    {
                        string fullPath = ToFullPath(created[i]);
                        if (File.Exists(fullPath)) File.Delete(fullPath);
                    }
                    foreach (string assetPath in originals)
                    {
                        string backup = Path.Combine(backupRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
                        string target = ToFullPath(assetPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        File.Copy(backup, target, true);
                    }
                    throw;
                }
                finally
                {
                    AssetDatabase.AllowAutoRefresh();
                }

                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception ex)
            {
                diagnostic = new EmberTableDiagnostic(
                    EmberTableDiagnosticSeverity.Error,
                    EmberTableErrorCode.ArtifactCommitFailed,
                    "Table artifact batch was rolled back: " + ex.Message);
                return false;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(stageRoot)) Directory.Delete(stageRoot, true);
                }
                catch
                {
                    // 正式产物已提交或回滚；暂存区清理失败不覆盖事务结果。
                }
            }
        }

        private static IReadOnlyList<EmberTableArtifactAction> BuildActions(
            IList<EmberTableArtifactContent> contents,
            IList<string> orphans)
        {
            var result = new List<EmberTableArtifactAction>();
            for (int i = 0; i < contents.Count; i++)
            {
                EmberTableArtifactContent content = contents[i];
                string fullPath = ToFullPath(content.AssetPath);
                EmberTableArtifactActionKind kind = !File.Exists(fullPath)
                    ? EmberTableArtifactActionKind.Create
                    : BytesEqual(File.ReadAllBytes(fullPath), content.Bytes)
                        ? EmberTableArtifactActionKind.Unchanged
                        : EmberTableArtifactActionKind.Replace;
                result.Add(new EmberTableArtifactAction(kind, content.AssetPath));
            }
            for (int i = 0; i < orphans.Count; i++)
                result.Add(new EmberTableArtifactAction(EmberTableArtifactActionKind.DeleteOwnedOrphan, orphans[i]));
            return result.AsReadOnly();
        }

        private static bool TryPrepareOwnedUpdates(
            IList<EmberTableArtifactContent> contents,
            out List<EmberTableArtifactContent> prepared,
            out EmberTableDiagnostic diagnostic)
        {
            prepared = new List<EmberTableArtifactContent>();
            diagnostic = null;
            HashSet<string> oldOwned = LoadOwnedPaths();
            if (!oldOwned.Contains(MANIFEST_PATH))
                return Fail(
                    EmberTableErrorCode.ArtifactStale,
                    "The generated artifact Manifest is missing or invalid. Export all tables first.",
                    MANIFEST_PATH,
                    out diagnostic);

            var exactPaths = new HashSet<string>(StringComparer.Ordinal);
            var platformPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            contents ??= Array.Empty<EmberTableArtifactContent>();
            for (int i = 0; i < contents.Count; i++)
            {
                EmberTableArtifactContent content = contents[i];
                string path = content?.AssetPath;
                if (!IsAllowedPath(path) || string.Equals(path, MANIFEST_PATH, StringComparison.Ordinal))
                    return Fail(
                        EmberTableErrorCode.OutputPathInvalid,
                        $"Partial output path is invalid or reserved: {path}",
                        path,
                        out diagnostic);
                if (!exactPaths.Add(path) || !platformPaths.Add(path))
                    return Fail(
                        EmberTableErrorCode.OutputPathConflict,
                        $"Partial output path conflicts with another artifact: {path}",
                        path,
                        out diagnostic);
                if (!oldOwned.Contains(path))
                    return Fail(
                        EmberTableErrorCode.ArtifactStale,
                        "The selected artifact is not owned by the current Manifest. Export all tables first.",
                        path,
                        out diagnostic);
                prepared.Add(content);
            }
            return true;
        }

        private static bool TryPrepare(
            IList<EmberTableArtifactContent> contents,
            out List<EmberTableArtifactContent> prepared,
            out HashSet<string> oldOwned,
            out EmberTableDiagnostic diagnostic)
        {
            prepared = new List<EmberTableArtifactContent>();
            oldOwned = LoadOwnedPaths();
            diagnostic = null;
            var exactPaths = new HashSet<string>(StringComparer.Ordinal);
            var platformPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            contents ??= Array.Empty<EmberTableArtifactContent>();

            for (int i = 0; i < contents.Count; i++)
            {
                EmberTableArtifactContent content = contents[i];
                string path = content?.AssetPath;
                if (!IsAllowedPath(path))
                    return Fail(EmberTableErrorCode.OutputPathInvalid, $"Generated output path is outside an allowed root: {path}", path, out diagnostic);
                if (string.Equals(path, MANIFEST_PATH, StringComparison.Ordinal))
                    return Fail(EmberTableErrorCode.OutputPathConflict, "The ownership manifest path is reserved by the artifact batch.", path, out diagnostic);
                if (!exactPaths.Add(path) || !platformPaths.Add(path))
                    return Fail(EmberTableErrorCode.OutputPathConflict, $"Generated output path conflicts with another artifact: {path}", path, out diagnostic);

                string fullPath = ToFullPath(path);
                if (File.Exists(fullPath)
                    && !oldOwned.Contains(path)
                    && !BytesEqual(File.ReadAllBytes(fullPath), content.Bytes))
                    return Fail(EmberTableErrorCode.UserFileProtected, "Refusing to replace a file not owned by the previous manifest.", path, out diagnostic);
                prepared.Add(content);
            }

            var manifest = new EmberTableArtifactManifest
            {
                paths = prepared.Select(item => item.AssetPath).OrderBy(item => item, StringComparer.Ordinal).ToList(),
            };
            string json = JsonUtility.ToJson(manifest, true) + "\n";
            var manifestContent = EmberTableArtifactContent.Text(MANIFEST_PATH, json);
            string manifestFullPath = ToFullPath(MANIFEST_PATH);
            if (File.Exists(manifestFullPath)
                && !oldOwned.Contains(MANIFEST_PATH)
                && !IsManifest(File.ReadAllText(manifestFullPath)))
                return Fail(EmberTableErrorCode.UserFileProtected, "Manifest path is occupied by a non-Ember file.", MANIFEST_PATH, out diagnostic);
            prepared.Add(manifestContent);
            return true;
        }

        private static HashSet<string> LoadOwnedPaths()
        {
            var owned = new HashSet<string>(StringComparer.Ordinal);
            string fullPath = ToFullPath(MANIFEST_PATH);
            if (!File.Exists(fullPath)) return owned;
            try
            {
                EmberTableArtifactManifest manifest = JsonUtility.FromJson<EmberTableArtifactManifest>(File.ReadAllText(fullPath));
                if (manifest?.generator != "Ember.Table.Editor" || manifest.formatVersion != 1) return owned;
                owned.Add(MANIFEST_PATH);
                if (manifest.paths == null) return owned;
                for (int i = 0; i < manifest.paths.Count; i++)
                    if (IsAllowedPath(manifest.paths[i])) owned.Add(manifest.paths[i]);
            }
            catch
            {
                return owned;
            }
            return owned;
        }

        private static bool IsAllowedPath(string path)
        {
            if (string.IsNullOrEmpty(path) || path.Contains("/../") || path.Contains("/./")) return false;
            return path == MANIFEST_PATH
                   || path.StartsWith(EmberTableCodeGenerator.GENERATED_ROOT + "/", StringComparison.Ordinal)
                   || path.StartsWith("Assets/GameResource/Resources/", StringComparison.Ordinal)
                   && path.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsManifest(string text)
        {
            try
            {
                EmberTableArtifactManifest manifest = JsonUtility.FromJson<EmberTableArtifactManifest>(text);
                return manifest?.generator == "Ember.Table.Editor" && manifest.formatVersion == 1;
            }
            catch { return false; }
        }

        private static bool Fail(
            EmberTableErrorCode code,
            string message,
            string path,
            out EmberTableDiagnostic diagnostic)
        {
            diagnostic = new EmberTableDiagnostic(
                EmberTableDiagnosticSeverity.Error,
                code,
                message,
                filePath: path);
            return false;
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
            return true;
        }

        private static string ToFullPath(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(ProjectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
            string root = ProjectRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Generated path escaped the project root.");
            return fullPath;
        }

        private static string ProjectRoot
        {
            get
            {
                if (!string.IsNullOrEmpty(ProjectRootOverrideForTests))
                    return Path.GetFullPath(ProjectRootOverrideForTests);
                return Directory.GetParent(Application.dataPath).FullName;
            }
        }
    }
}
