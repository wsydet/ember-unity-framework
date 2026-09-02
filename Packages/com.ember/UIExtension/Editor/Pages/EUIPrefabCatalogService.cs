// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Ember.UI;

using UnityEditor;

using UnityEngine;

namespace Ember.UIExtension.Editor
{
    /// <summary>UI 开发中心的一条只读目录记录。</summary>
    public sealed class EUIPrefabCatalogEntry
    {
        public string PrefabPath;
        public bool IsPage;
        public bool NoCodeGeneration;
        public PageType PageType;
        public string PageName;
        public string PageDesc;
        public int BindingCount;
        public int FrameworkBindingCount;
        public bool GenerateCustomSettings;
        public string LogicScriptPath;
        public bool LogicScriptExists;
        public string BindingScriptPath;
        public bool BindingScriptExists;
        public string SettingsScriptPath;
        public bool SettingsScriptExists;
        public string PageDefFile;
        public string PageDefLine;
        public bool PageDefOk;
        public int PageDefMatchCount;
        public int MissingScriptCount;
        public int NullBindingCount;
        public int EmptyLeafCount;

        public bool IsHealthy =>
            MissingScriptCount == 0
            && NullBindingCount == 0
            && (NoCodeGeneration || (LogicScriptExists && BindingScriptExists
                && (!GenerateCustomSettings || SettingsScriptExists)))
            && (!IsPage || NoCodeGeneration || PageDefOk);
    }

    /// <summary>一次只读目录扫描的完整上下文。</summary>
    public sealed class EUIPrefabCatalogSnapshot
    {
        public readonly List<EUIPrefabCatalogEntry> Entries = new List<EUIPrefabCatalogEntry>();
        public string UIResourceRoot;
        public string BusinessCodeRoot;
        public string UserPageDefFile;
        public string FrameworkPageDefFile;
        public string Error;

        public bool IsConfigured => string.IsNullOrEmpty(Error);
    }

    /// <summary>
    /// UI 预制体只读目录服务。扫描过程只加载现有设置，不创建/自愈资产，也不保存任何内容。
    /// </summary>
    public static class EUIPrefabCatalogService
    {
        internal sealed class PageDefSourceMatch
        {
            public string Name;
            public string PrefabPath;
            public int DeclarationIndex;
            public int DeclarationLine;
            public int DeclarationEndLine;
            public bool IsStandardField;
        }

        private static readonly Regex PageDefDeclarationRegex = new Regex(
            @"(?<![_\p{L}\p{Nd}])EUIPageDef\s+(?<name>[_\p{L}][_\p{L}\p{Nd}]*)\s*=\s*new\s*\(",
            RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>扫描配置 UI 根目录中的根 EUIBinding 预制体。</summary>
        public static EUIPrefabCatalogSnapshot Scan()
        {
            var snapshot = new EUIPrefabCatalogSnapshot();
            var settings = EUIBindingSettingData.LoadExistingSettings();
            if (!settings)
            {
                snapshot.Error = $"未找到 EUIBinding 设置：{EUIBindingSettingData.k_SettingsPath}";
                return snapshot;
            }

            var implementation = settings.LogicImplementations?
                .OfType<CSharpLogicImplementationData>()
                .FirstOrDefault(i => i);
            if (!implementation)
            {
                snapshot.Error = "EUIBinding 设置中未配置 C# 逻辑实现。";
                return snapshot;
            }

            snapshot.UIResourceRoot = NormalizeAssetPath(implementation.UIResourceRoot);
            snapshot.BusinessCodeRoot = NormalizeAssetPath(settings.BusinessCodeRoot);
            snapshot.UserPageDefFile = NormalizeAssetPath(implementation.PageDefFile);
            snapshot.FrameworkPageDefFile = ResolveFrameworkPageDefFile(snapshot.UserPageDefFile);

            if (!ValidateConfiguredPaths(snapshot, out snapshot.Error))
                return snapshot;

            string[] guids;
            try
            {
                guids = AssetDatabase.FindAssets("t:Prefab", new[] { snapshot.UIResourceRoot });
            }
            catch (Exception exception)
            {
                snapshot.Error = $"扫描 UI 预制体失败：{exception.Message}";
                return snapshot;
            }
            foreach (var guid in guids)
            {
                var path = NormalizeAssetPath(AssetDatabase.GUIDToAssetPath(guid));
                if (string.IsNullOrEmpty(path)
                    || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var binding = prefab ? prefab.GetComponent<EUIBinding>() : null;
                    if (!binding) continue;
                    snapshot.Entries.Add(BuildEntry(snapshot, path, prefab, binding));
                }
                catch (Exception exception)
                {
                    snapshot.Entries.Clear();
                    snapshot.Error = $"读取 UI 预制体失败：{path}\n{exception.Message}";
                    return snapshot;
                }
            }

            snapshot.Entries.Sort((a, b) => string.Compare(
                a.PrefabPath, b.PrefabPath, StringComparison.Ordinal));
            return snapshot;
        }

        /// <summary>列出预制体中可疑的空叶子；嵌套预制体与 SafeArea 语义节点永不进入结果。</summary>
        public static List<string> FindEmptyLeafPaths(string prefabPath)
        {
            var result = new List<string>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (!prefab) return result;

            foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (!IsDeletableEmptyLeaf(prefab, transform)) continue;
                result.Add(GetNodePath(prefab.transform, transform));
            }
            return result;
        }

        /// <summary>判断节点是否为允许进入清理候选的普通空叶子。</summary>
        public static bool IsDeletableEmptyLeaf(GameObject prefabRoot, Transform transform)
        {
            if (!prefabRoot || !transform || transform == prefabRoot.transform || transform.childCount > 0)
                return false;

            var components = transform.GetComponents<Component>();
            if (components.Length != 1 || !(components[0] is Transform)) return false;

            // EUISafeArea 的九个定位点是有语义的空节点，绝不能作为垃圾节点。
            if (transform.GetComponentInParent<EUISafeArea>(true) != null) return false;

            // 任意 nested prefab 都由其源资产负责，管理器不能跨边界删除内部节点。
            var nearestInstanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(transform.gameObject);
            if (nearestInstanceRoot && nearestInstanceRoot != prefabRoot) return false;

            return true;
        }

        public static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? null
                : path.Trim().Replace('\\', '/').TrimEnd('/');
        }

        public static string ToFullPath(string assetPath)
        {
            assetPath = NormalizeAssetPath(assetPath);
            if (!IsSafeAssetPath(assetPath, true)) return null;

            try
            {
                var dataRoot = Path.GetFullPath(Application.dataPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (string.Equals(assetPath, "Assets", StringComparison.Ordinal)) return dataRoot;

                var fullPath = Path.GetFullPath(Path.Combine(dataRoot,
                    assetPath.Substring("Assets/".Length)
                        .Replace('/', Path.DirectorySeparatorChar)));
                return fullPath.StartsWith(dataRoot + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase) ? fullPath : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>判断路径是否为规范、无穿越且可被 Unity 导入的 Assets 路径。</summary>
        public static bool IsSafeAssetPath(string assetPath, bool allowAssetsRoot = false)
        {
            assetPath = NormalizeAssetPath(assetPath);
            if (string.IsNullOrEmpty(assetPath)) return false;
            if (string.Equals(assetPath, "Assets", StringComparison.Ordinal))
                return allowAssetsRoot;
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal)
                || assetPath.StartsWith("/", StringComparison.Ordinal)
                || assetPath.Contains(":")
                || assetPath.Contains("//"))
                return false;

            var segments = assetPath.Split('/');
            return segments.Length > 1 && segments.All(segment =>
                !string.IsNullOrEmpty(segment)
                && string.Equals(segment, segment.Trim(), StringComparison.Ordinal)
                && segment != "."
                && segment != ".."
                && segment.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                && !segment.EndsWith("~", StringComparison.Ordinal));
        }

        public static string ResolveFrameworkPageDefFile(string userFile)
        {
            const string userName = "GamePages.User.cs";
            userFile = NormalizeAssetPath(userFile);
            return !string.IsNullOrEmpty(userFile)
                && userFile.EndsWith(userName, StringComparison.Ordinal)
                    ? userFile.Substring(0, userFile.Length - userName.Length) + "GamePages.cs"
                    : null;
        }

        private static EUIPrefabCatalogEntry BuildEntry(EUIPrefabCatalogSnapshot snapshot,
            string prefabPath, GameObject prefab, EUIBinding binding)
        {
            var subDirectory = string.IsNullOrWhiteSpace(binding.ClassPath)
                ? string.Empty
                : binding.ClassPath.Trim().Replace('\\', '/').Trim('/') + "/";
            var className = binding.ClassName ?? string.Empty;

            var entry = new EUIPrefabCatalogEntry
            {
                PrefabPath = prefabPath,
                IsPage = binding.IsPage,
                NoCodeGeneration = binding.NoCodeGeneration,
                PageType = binding.PageType,
                PageName = binding.IsPage ? binding.PageName ?? string.Empty : string.Empty,
                PageDesc = GetPageDesc(binding.PageType),
                BindingCount = binding.Bindings?.Length ?? 0,
                FrameworkBindingCount = binding.Bindings?.Count(item => item.IsFramework) ?? 0,
                GenerateCustomSettings = binding.GenerateCustomSettings,
                LogicScriptPath = $"{snapshot.BusinessCodeRoot}/{subDirectory}{className}.cs",
                BindingScriptPath = $"{snapshot.BusinessCodeRoot}/{subDirectory}{className}.Binding.cs",
                SettingsScriptPath = $"{snapshot.BusinessCodeRoot}/{subDirectory}{className}Settings.cs",
                MissingScriptCount = CountMissingScripts(prefab),
                NullBindingCount = binding.Bindings?.Count(item => item.GameObject == null) ?? 0,
                EmptyLeafCount = FindEmptyLeafPaths(prefabPath).Count,
            };

            entry.LogicScriptExists = File.Exists(ToFullPath(entry.LogicScriptPath));
            entry.BindingScriptExists = File.Exists(ToFullPath(entry.BindingScriptPath));
            entry.SettingsScriptExists = File.Exists(ToFullPath(entry.SettingsScriptPath));

            if (entry.IsPage && !string.IsNullOrEmpty(entry.PageName))
                FillPageDefInfo(entry, snapshot);
            return entry;
        }

        private static void FillPageDefInfo(EUIPrefabCatalogEntry entry,
            EUIPrefabCatalogSnapshot snapshot)
        {
            var allMatches = new List<(string File, string Content, PageDefSourceMatch Match)>();
            foreach (var file in new[] { snapshot.UserPageDefFile, snapshot.FrameworkPageDefFile })
            {
                var fullPath = ToFullPath(file);
                if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath)) continue;

                var content = File.ReadAllText(fullPath, Encoding.UTF8);
                var matches = FindPageDefinitions(content)
                    .Where(match => string.Equals(match.Name,
                        entry.PageName, StringComparison.Ordinal))
                    .ToList();
                allMatches.AddRange(matches.Select(match => (file, content, match)));
            }

            entry.PageDefMatchCount = allMatches.Count;
            if (allMatches.Count == 0) return;

            var first = allMatches[0];
            entry.PageDefFile = first.File;
            var lineStart = first.Content.LastIndexOf('\n',
                Math.Max(0, first.Match.DeclarationIndex - 1));
            lineStart = lineStart < 0 ? 0 : lineStart + 1;
            var lineEnd = first.Content.IndexOf('\n', first.Match.DeclarationIndex);
            lineEnd = lineEnd < 0 ? first.Content.Length : lineEnd;
            entry.PageDefLine = first.Content.Substring(lineStart, lineEnd - lineStart).Trim();
            if (entry.PageDefLine.Length > 110)
                entry.PageDefLine = entry.PageDefLine.Substring(0, 110) + "…";

            entry.PageDefOk = allMatches.Count == 1
                && first.Match.IsStandardField
                && first.Match.DeclarationEndLine >= first.Match.DeclarationLine
                && string.Equals(first.Match.PrefabPath, entry.PrefabPath,
                    StringComparison.Ordinal);
        }

        /// <summary>
        /// 解析真实 C# 代码中的 EUIPageDef 声明。注释与所有字符串/字符字面量先被等长屏蔽，
        /// 因此示例文本不会被当成声明；声明首参数仅从已确认的 new(...) 代码位置读取。
        /// </summary>
        internal static List<PageDefSourceMatch> FindPageDefinitions(string content)
        {
            var result = new List<PageDefSourceMatch>();
            if (string.IsNullOrEmpty(content)) return result;

            var code = MaskCommentsAndLiterals(content);
            foreach (Match match in PageDefDeclarationRegex.Matches(code))
            {
                var statementEnd = code.IndexOf(';', match.Index + match.Length);
                var tailEnd = statementEnd >= 0 ? statementEnd : content.Length;
                var tailLength = Math.Max(0, tailEnd - (match.Index + match.Length));
                var tail = content.Substring(match.Index + match.Length, tailLength);
                var pathMatch = Regex.Match(tail, @"^\s*""(?<path>[^""\r\n]+)""",
                    RegexOptions.CultureInvariant);
                var lineStart = content.LastIndexOf('\n', Math.Max(0, match.Index - 1));
                lineStart = lineStart < 0 ? 0 : lineStart + 1;
                var lineEnd = content.IndexOf('\n', match.Index);
                lineEnd = lineEnd < 0 ? content.Length : lineEnd;
                var declarationLine = content.Substring(lineStart, lineEnd - lineStart);
                var isStandardField = Regex.IsMatch(declarationLine,
                    @"^\s*public\s+static\s+readonly\s+EUIPageDef\s+"
                    + Regex.Escape(match.Groups["name"].Value) + @"\b",
                    RegexOptions.CultureInvariant);

                result.Add(new PageDefSourceMatch
                {
                    Name = match.Groups["name"].Value,
                    PrefabPath = pathMatch.Success ? pathMatch.Groups["path"].Value : null,
                    DeclarationIndex = match.Index,
                    DeclarationLine = CountLinesBefore(content, match.Index),
                    DeclarationEndLine = statementEnd >= 0
                        ? CountLinesBefore(content, statementEnd)
                        : -1,
                    IsStandardField = isStandardField,
                });
            }
            return result;
        }

        private static int CountLinesBefore(string content, int index)
        {
            var line = 0;
            for (var current = 0; current < index && current < content.Length; current++)
                if (content[current] == '\n') line++;
            return line;
        }

        /// <summary>将注释、字符串与字符字面量替换为等长空白，保留换行和源码索引。</summary>
        internal static string MaskCommentsAndLiterals(string content)
        {
            if (string.IsNullOrEmpty(content)) return content ?? string.Empty;

            var chars = content.ToCharArray();
            var state = 0; // 0=code, 1=line comment, 2=block comment, 3=string, 4=verbatim string, 5=char, 6=raw string
            var rawDelimiterLength = 0;
            for (var index = 0; index < chars.Length; index++)
            {
                var current = chars[index];
                var next = index + 1 < chars.Length ? chars[index + 1] : '\0';
                if (state == 0)
                {
                    if (current == '/' && next == '/')
                    {
                        chars[index] = chars[++index] = ' ';
                        state = 1;
                    }
                    else if (current == '/' && next == '*')
                    {
                        chars[index] = chars[++index] = ' ';
                        state = 2;
                    }
                    else if (current == '"')
                    {
                        var quoteCount = CountRepeated(chars, index, '"');
                        if (quoteCount >= 3)
                        {
                            rawDelimiterLength = quoteCount;
                            for (var quote = 0; quote < quoteCount; quote++)
                                chars[index + quote] = ' ';
                            index += quoteCount - 1;
                            state = 6;
                            continue;
                        }

                        chars[index] = ' ';
                        state = index > 0 && chars[index - 1] == '@'
                            || index > 1 && chars[index - 2] == '@' && chars[index - 1] == '$'
                                ? 4
                                : 3;
                    }
                    else if (current == '\'')
                    {
                        chars[index] = ' ';
                        state = 5;
                    }
                    continue;
                }

                if (current == '\r' || current == '\n')
                {
                    if (state == 1) state = 0;
                    continue;
                }

                chars[index] = ' ';
                if (state == 1) continue;
                if (state == 2)
                {
                    if (current == '*' && next == '/')
                    {
                        chars[++index] = ' ';
                        state = 0;
                    }
                    continue;
                }
                if (state == 6)
                {
                    var quoteCount = current == '"' ? CountRepeated(content, index, '"') : 0;
                    if (quoteCount >= rawDelimiterLength)
                    {
                        for (var quote = 1; quote < rawDelimiterLength; quote++)
                            chars[index + quote] = ' ';
                        index += rawDelimiterLength - 1;
                        state = 0;
                    }
                    continue;
                }
                if ((state == 3 || state == 5) && current == '\\' && next != '\0')
                {
                    chars[++index] = ' ';
                }
                else if ((state == 3 && current == '"') || (state == 5 && current == '\''))
                {
                    state = 0;
                }
                else if (state == 4 && current == '"')
                {
                    if (next == '"') chars[++index] = ' ';
                    else state = 0;
                }
            }
            return new string(chars);
        }

        private static int CountRepeated(IReadOnlyList<char> chars, int start, char value)
        {
            var count = 0;
            while (start + count < chars.Count && chars[start + count] == value) count++;
            return count;
        }

        private static int CountRepeated(string text, int start, char value)
        {
            var count = 0;
            while (start + count < text.Length && text[start + count] == value) count++;
            return count;
        }

        private static bool ValidateConfiguredPaths(EUIPrefabCatalogSnapshot snapshot,
            out string error)
        {
            error = null;
            if (!ValidateFolder(snapshot.UIResourceRoot, "UI 资源根目录", out error))
                return false;
            if (!ValidateFolder(snapshot.BusinessCodeRoot, "业务代码根目录", out error))
                return false;
            if (!ValidatePageDefFile(snapshot.UserPageDefFile, "用户 PageDef 文件", out error))
                return false;
            return string.IsNullOrEmpty(snapshot.FrameworkPageDefFile)
                || ValidatePageDefFile(snapshot.FrameworkPageDefFile, "框架 PageDef 文件", out error);
        }

        private static bool ValidateFolder(string path, string label, out string error)
        {
            error = null;
            if (!IsSafeAssetPath(path) || !AssetDatabase.IsValidFolder(path))
            {
                error = $"{label}必须是 Assets/ 下已存在的规范目录：{path ?? "（空）"}";
                return false;
            }
            return true;
        }

        private static bool ValidatePageDefFile(string path, string label, out string error)
        {
            error = null;
            var fullPath = ToFullPath(path);
            if (!IsSafeAssetPath(path)
                || !path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(fullPath)
                || !File.Exists(fullPath))
            {
                error = $"{label}必须是 Assets/ 下已存在的规范 C# 文件：{path ?? "（空）"}";
                return false;
            }
            return true;
        }

        private static int CountMissingScripts(GameObject prefab)
        {
            var count = 0;
            foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
                count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
            return count;
        }

        private static string GetPageDesc(PageType pageType)
        {
            var layer = pageType switch
            {
                PageType.Background => "Background",
                PageType.Popup => "Popup",
                PageType.FullScreenPopup => "Popup",
                PageType.TopMost => "TopMost",
                PageType.FreePage => "FreePage",
                _ => "Normal",
            };
            return $"{layer} · {pageType}";
        }

        private static string GetNodePath(Transform root, Transform target)
        {
            var names = new List<string>();
            for (var current = target; current && current != root; current = current.parent)
                names.Add(current.name);
            names.Reverse();
            return string.Join("/", names);
        }
    }
}
