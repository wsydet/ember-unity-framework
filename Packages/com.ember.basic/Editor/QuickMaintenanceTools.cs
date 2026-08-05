// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 快捷维护工具 —— 独立菜单项，不需要打开窗口。
    /// </summary>
    public static class QuickMaintenanceTools
    {
        private const string TAG = LogTags.EmberBasic + "." + nameof(QuickMaintenanceTools);

        /// <summary>
        /// Roslyn 互操作包装 —— 通过运行时反射加载 Roslyn DLL，
        /// 绕过 Unity 编译期 System.Runtime 版本冲突。
        /// </summary>
        private static class Roslyn
        {
            private static Assembly _codeAnalysis;
            private static Assembly _codeAnalysisCSharp;
            private static Type _syntaxTreeType;
            private static Type _compilationType;
            private static Type _compilationOptsType;
            private static Type _parseOptsType;
            private static Type _metaRefType;
            private static Type _outputKindEnum;
            private static Type _diagSeverityEnum;
            private static Type _languageVersionEnum;

            public static bool IsAvailable { get; private set; }

            static Roslyn()
            {
                try
                {
                    // Roslyn~ 文件夹末尾的 ~ 让 Unity 跳过插件导入，
                    // 避免 ".NET 8 DLL 引用验证失败" 的问题。
                    // 我们在运行时通过 Assembly.LoadFrom 手动加载。
                    string roslynDir = Path.GetFullPath(
                        "Packages/com.ember.basic/Editor/Roslyn~");

                    // 处理 System.Runtime 版本重定向：
                    // Roslyn DLL 编译时引用 System.Runtime 8.0，
                    // 但 Unity 提供的是 4.1.2.0 门面。
                    // 运行时所有需要的类型都已存在，只是版本号不同。
                    AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
                    {
                        var reqName = new AssemblyName(args.Name);
                        if (reqName.Name == "System.Runtime" ||
                            reqName.Name == "System.Collections.Immutable" ||
                            reqName.Name == "System.Reflection.Metadata")
                        {
                            // 返回已加载的对应程序集（忽略版本号差异）
                            var loaded = AppDomain.CurrentDomain.GetAssemblies()
                                .FirstOrDefault(a => a.GetName().Name == reqName.Name);
                            if (loaded != null) return loaded;
                        }
                        return null;
                    };

                    _codeAnalysis = Assembly.LoadFrom(
                        Path.Combine(roslynDir, "Microsoft.CodeAnalysis.dll"));
                    _codeAnalysisCSharp = Assembly.LoadFrom(
                        Path.Combine(roslynDir, "Microsoft.CodeAnalysis.CSharp.dll"));

                    // 缓存所有需要的类型
                    _syntaxTreeType = _codeAnalysisCSharp.GetType(
                        "Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree");
                    _compilationType = _codeAnalysisCSharp.GetType(
                        "Microsoft.CodeAnalysis.CSharp.CSharpCompilation");
                    _compilationOptsType = _codeAnalysisCSharp.GetType(
                        "Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions");
                    _parseOptsType = _codeAnalysisCSharp.GetType(
                        "Microsoft.CodeAnalysis.CSharp.CSharpParseOptions");
                    _metaRefType = _codeAnalysis.GetType(
                        "Microsoft.CodeAnalysis.MetadataReference");
                    _outputKindEnum = _codeAnalysis.GetType(
                        "Microsoft.CodeAnalysis.OutputKind");
                    _diagSeverityEnum = _codeAnalysis.GetType(
                        "Microsoft.CodeAnalysis.DiagnosticSeverity");
                    _languageVersionEnum = _codeAnalysisCSharp.GetType(
                        "Microsoft.CodeAnalysis.CSharp.LanguageVersion");

                    IsAvailable = true;
                }
                catch (Exception ex)
                {
                    EmberDebug.LogError(TAG,
                        $"Roslyn failed to load — 'Clean Unused References' will be unavailable: {ex.Message}");
                    IsAvailable = false;
                }
            }

            /// <summary>
            /// 构建所有已加载程序集的 MetadataReference 数组。
            /// </summary>
            public static object[] BuildMetadataReferences()
            {
                var refs = new List<object>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var createMethod = _metaRefType.GetMethod("CreateFromFile",
                    new[] { typeof(string) });

                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.IsDynamic) continue;
                    string loc;
                    try { loc = asm.Location; }
                    catch { continue; }
                    if (string.IsNullOrEmpty(loc) || !File.Exists(loc)) continue;
                    if (!seen.Add(loc)) continue;
                    try { refs.Add(createMethod.Invoke(null, new object[] { loc })); }
                    catch { /* 跳过无法引用的程序集 */ }
                }
                return refs.ToArray();
            }

            /// <summary>
            /// 构建解析选项（含 Unity 预处理器符号）。
            /// </summary>
            public static object CreateParseOptions()
            {
                // LanguageVersion.Latest
                object latest = Enum.Parse(_languageVersionEnum, "Latest");

                // 预处理器符号
                var symbols = new[]
                {
                    "UNITY_EDITOR", "UNITY_6000", "UNITY_ENGINE",
                    "UNITY_STANDALONE", "UNITY_STANDALONE_WIN",
                    "PLATFORM_STANDALONE_WIN", "PLATFORM_STANDALONE",
                };

                // new CSharpParseOptions(latest, preprocessorSymbols: symbols)
                var ctor = _parseOptsType.GetConstructor(new[] { _languageVersionEnum, typeof(IEnumerable<string>) });
                return ctor.Invoke(new object[] { latest, symbols });
            }

            /// <summary>
            /// 构建编译选项。
            /// </summary>
            public static object CreateCompilationOptions()
            {
                // OutputKind.DynamicallyLinkedLibrary
                object dllKind = Enum.Parse(_outputKindEnum, "DynamicallyLinkedLibrary");
                var ctor = _compilationOptsType.GetConstructor(new[] { _outputKindEnum });
                return ctor.Invoke(new object[] { dllKind });
            }

            /// <summary>
            /// CSharpSyntaxTree.ParseText(code, options, path)
            /// </summary>
            public static object ParseText(string code, object parseOptions, string path)
            {
                var method = _syntaxTreeType.GetMethod("ParseText",
                    new[] { typeof(string), _parseOptsType, typeof(string) });
                return method.Invoke(null, new[] { code, parseOptions, path });
            }

            /// <summary>
            /// CSharpCompilation.Create(name, trees, refs, opts)
            /// </summary>
            public static object CreateCompilation(string name, object[] trees,
                object[] refs, object compOptions)
            {
                var treeArr = Array.CreateInstance(_syntaxTreeType, trees.Length);
                Array.Copy(trees, treeArr, trees.Length);

                var refArr = Array.CreateInstance(_metaRefType, refs.Length);
                Array.Copy(refs, refArr, refs.Length);

                var method = _compilationType.GetMethod("Create", new[]
                {
                    typeof(string),
                    treeArr.GetType(),
                    refArr.GetType(),
                    _compilationOptsType
                });
                return method.Invoke(null, new[] { name, treeArr, refArr, compOptions });
            }

            /// <summary>
            /// tree.GetDiagnostics() —— 语法层面诊断
            /// </summary>
            public static IReadOnlyList<DiagnosticInfo> GetDiagnostics(object syntaxTreeOrCompilation)
            {
                var method = syntaxTreeOrCompilation.GetType().GetMethod("GetDiagnostics", Type.EmptyTypes);
                var diags = (System.Collections.IEnumerable)method.Invoke(syntaxTreeOrCompilation, null);

                var result = new List<DiagnosticInfo>();
                foreach (var d in diags)
                {
                    result.Add(new DiagnosticInfo(d));
                }
                return result;
            }

            /// <summary>
            /// 一个轻量级的诊断信息提取器，避免在外部使用反射。
            /// </summary>
            public sealed class DiagnosticInfo
            {
                public string Id { get; }
                public int Severity { get; } // 0=Hidden, 1=Info, 2=Warning, 3=Error
                public int Line { get; }

                public DiagnosticInfo(object diag)
                {
                    // Id
                    Id = (string)diag.GetType().GetProperty("Id").GetValue(diag);

                    // Severity (enum → int)
                    var sevProp = diag.GetType().GetProperty("Severity");
                    var sevValue = sevProp.GetValue(diag);
                    Severity = (int)sevValue;

                    // Location → GetLineSpan → StartLinePosition.Line
                    var locProp = diag.GetType().GetProperty("Location");
                    var location = locProp.GetValue(diag);
                    var spanMethod = location.GetType().GetMethod("GetLineSpan");
                    var lineSpan = spanMethod.Invoke(location, null);
                    var startProp = lineSpan.GetType().GetProperty("StartLinePosition");
                    var startPos = startProp.GetValue(lineSpan);
                    var lineProp = startPos.GetType().GetProperty("Line");
                    Line = (int)lineProp.GetValue(startPos);
                }
            }

            public static object[] MetadataRefs { get; set; }
            public static object ParseOptions { get; set; }
            public static object CompilationOptions { get; set; }
        }

        /// <summary>
        /// Roslyn 元数据是否已初始化。
        /// </summary>
        private static bool _roslynInitialized;

        #region 外部方法

        [MenuItem("Ember/Tool/清空本地缓存 (PlayerPrefs + PersistentData)", false, 360)]
        public static void ClearLocalCache()
        {
            var lang = EmberEditorWindow.GlobalLang;
            if (!EditorUtility.DisplayDialog(
                EditorToolUtility.L10n(lang, "Confirm Cleanup", "确认清理"),
                EditorToolUtility.L10n(lang,
                    "This will delete all PlayerPrefs records and files under persistentDataPath.\nThis action cannot be undone!",
                    "将删除所有 PlayerPrefs 记录和 persistentDataPath 下的文件（存档等）。\n此操作不可撤销！"),
                EditorToolUtility.L10n(lang, "Confirm", "确认清理"),
                EditorToolUtility.L10n(lang, "Cancel", "取消")))
                return;

            PlayerPrefs.DeleteAll();
            string path = Application.persistentDataPath;
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                Directory.CreateDirectory(path);
            }
            EditorUtility.DisplayDialog(
                EditorToolUtility.L10n(lang, "Done", "完成"),
                EditorToolUtility.L10n(lang, "Local cache cleared.", "本地缓存已清空。"),
                "OK");
        }

        [MenuItem("Ember/Tool/删除项目空文件夹", false, 370)]
        public static void RemoveEmptyFolders()
        {
            var lang = EmberEditorWindow.GlobalLang;
            if (!EditorUtility.DisplayDialog(
                EditorToolUtility.L10n(lang, "Confirm", "确认操作"),
                EditorToolUtility.L10n(lang,
                    "Scan the Assets directory and delete all empty sub-folders (including .meta).\nThis action cannot be undone!",
                    "将扫描 Assets 目录，删除所有空的子文件夹（含 .meta）。\n此操作不可撤销！"),
                EditorToolUtility.L10n(lang, "Delete", "确认删除"),
                EditorToolUtility.L10n(lang, "Cancel", "取消")))
                return;

            int count = 0;
            var dirs = Directory.GetDirectories(Application.dataPath, "*", SearchOption.AllDirectories);
            for (int i = dirs.Length - 1; i >= 0; i--)
            {
                if (Directory.GetFiles(dirs[i]).Length == 0 && Directory.GetDirectories(dirs[i]).Length == 0)
                {
                    string meta = dirs[i] + ".meta";
                    Directory.Delete(dirs[i], true);
                    if (File.Exists(meta)) File.Delete(meta);
                    count++;
                }
            }
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                EditorToolUtility.L10n(lang, "Done", "完成"),
                EditorToolUtility.L10n(lang, $"Deleted {count} empty folders.", $"已删除 {count} 个空文件夹。"),
                "OK");
        }

        [MenuItem("Ember/Tool/批量清理脚本未使用引用", false, 380)]
        public static void CleanUnusedScriptReferences()
        {
            var lang = EmberEditorWindow.GlobalLang;

            // 首次调用时初始化 Roslyn
            if (!_roslynInitialized)
            {
                _roslynInitialized = true;
                if (Roslyn.IsAvailable)
                {
                    Roslyn.MetadataRefs = Roslyn.BuildMetadataReferences();
                    Roslyn.ParseOptions = Roslyn.CreateParseOptions();
                    Roslyn.CompilationOptions = Roslyn.CreateCompilationOptions();
                }
            }

            if (!Roslyn.IsAvailable)
            {
                EditorUtility.DisplayDialog(
                    EditorToolUtility.L10n(lang, "Unavailable", "不可用"),
                    EditorToolUtility.L10n(lang,
                        "Roslyn is not available. The 'Clean Unused References' tool requires Microsoft.CodeAnalysis DLLs.\nCheck the Console for details.",
                        "Roslyn 不可用。'批量清理脚本未使用引用' 工具需要 Microsoft.CodeAnalysis DLL。\n详见 Console。"),
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                EditorToolUtility.L10n(lang, "Clean Unused References", "批量清理未使用引用"),
                EditorToolUtility.L10n(lang,
                    "Scan all .cs files under Assets/ and remove unused using directives.\n\n" +
                    "Powered by Roslyn (same engine as VS Studio):\n" +
                    "  • Full semantic analysis — 100% accurate\n" +
                    "  • Same CS8019 diagnostic that VS Studio uses\n" +
                    "  • Skips files with parse errors (reported in Console)\n\n" +
                    "Modified files are listed in the Console.\n" +
                    "This operation can be undone via version control.",
                    "将扫描 Assets/ 下所有 .cs 文件，移除非必要的 using 引用。\n\n" +
                    "基于 Roslyn 语义分析（与 VS Studio 相同引擎）：\n" +
                    "  • 完整语义分析 —— 100% 准确\n" +
                    "  • 使用与 VS Studio 相同的 CS8019 诊断\n" +
                    "  • 跳过有语法错误的文件（在 Console 中报告）\n\n" +
                    "被修改的文件将列在 Console 中。\n" +
                    "此操作可通过版本控制撤销。"),
                EditorToolUtility.L10n(lang, "Start", "开始清理"),
                EditorToolUtility.L10n(lang, "Cancel", "取消")))
                return;

            int totalFiles = 0;
            int modifiedFiles = 0;
            int totalUsingsRemoved = 0;
            int skippedFiles = 0;

            var csFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

            try
            {
                int totalCount = csFiles.Length;
                for (int i = 0; i < csFiles.Length; i++)
                {
                    string filePath = csFiles[i];
                    totalFiles++;

                    if (i % 20 == 0)
                    {
                        float progress = (float)i / totalCount;
                        if (EditorUtility.DisplayCancelableProgressBar(
                            EditorToolUtility.L10n(lang, "Cleaning unused references...", "正在清理未使用引用..."),
                            EditorToolUtility.L10n(lang,
                                $"{Path.GetFileName(filePath)}  ({i}/{totalCount})",
                                $"{Path.GetFileName(filePath)}  ({i}/{totalCount})"),
                            progress))
                        {
                            EmberDebug.Log(TAG, EditorToolUtility.L10n(lang,
                                "Cleanup cancelled by user.", "清理已被用户取消。"));
                            EditorUtility.ClearProgressBar();
                            return;
                        }
                    }

                    int removed = RemoveUnusedUsingsWithRoslyn(filePath, ref skippedFiles);
                    if (removed > 0)
                    {
                        modifiedFiles++;
                        totalUsingsRemoved += removed;
                    }
                }

                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();

                string resultMsg = EditorToolUtility.L10n(lang,
                    $"Scanned {totalFiles} files.\nModified {modifiedFiles} files.\nRemoved {totalUsingsRemoved} unused using directive(s)." +
                    (skippedFiles > 0 ? $"\nSkipped {skippedFiles} file(s) with parse errors (see Console)." : ""),
                    $"已扫描 {totalFiles} 个文件。\n修改了 {modifiedFiles} 个文件。\n移除了 {totalUsingsRemoved} 个未使用的 using 引用。" +
                    (skippedFiles > 0 ? $"\n跳过了 {skippedFiles} 个有语法错误的文件（详见 Console）。" : ""));

                EditorUtility.DisplayDialog(
                    EditorToolUtility.L10n(lang, "Done", "完成"),
                    resultMsg,
                    "OK");

                EmberDebug.Log(TAG, EditorToolUtility.L10n(lang,
                    $"Unused reference cleanup done: scanned {totalFiles}, modified {modifiedFiles}, removed {totalUsingsRemoved}, skipped {skippedFiles}.",
                    $"未使用引用清理完成：扫描 {totalFiles}，修改 {modifiedFiles}，移除 {totalUsingsRemoved}，跳过 {skippedFiles}。"));
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EmberDebug.LogError(TAG, $"Cleanup error: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog(
                    EditorToolUtility.L10n(lang, "Error", "错误"),
                    EditorToolUtility.L10n(lang, $"Error: {ex.Message}", $"发生错误：{ex.Message}"),
                    "OK");
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        /// <summary>
        /// 使用 Roslyn 语义分析移除未使用的 using 指令（通过反射调用，绕过编译期版本冲突）。
        /// </summary>
        private static int RemoveUnusedUsingsWithRoslyn(string filePath, ref int skippedFiles)
        {
            string content;
            try { content = File.ReadAllText(filePath); }
            catch { return 0; }

            // 1. 解析语法树
            object syntaxTree;
            try
            {
                syntaxTree = Roslyn.ParseText(content, Roslyn.ParseOptions, filePath);
            }
            catch (Exception ex)
            {
                EmberDebug.LogWarning(TAG, $"Parse error in {Path.GetFileName(filePath)}: {ex.Message}");
                skippedFiles++;
                return 0;
            }

            // 2. 语法层面检查 —— 有语法错误的文件跳过
            var syntaxDiagnostics = Roslyn.GetDiagnostics(syntaxTree);
            if (syntaxDiagnostics.Any(d => d.Severity == 3)) // Error
            {
                skippedFiles++;
                return 0;
            }

            // 3. 创建编译
            object compilation;
            try
            {
                compilation = Roslyn.CreateCompilation(
                    $"Analysis_{Guid.NewGuid():N}",
                    new[] { syntaxTree },
                    Roslyn.MetadataRefs,
                    Roslyn.CompilationOptions);
            }
            catch (Exception ex)
            {
                EmberDebug.LogWarning(TAG, $"Compilation error in {Path.GetFileName(filePath)}: {ex.Message}");
                skippedFiles++;
                return 0;
            }

            // 4. 获取 CS8019 诊断
            var semanticDiagnostics = Roslyn.GetDiagnostics(compilation);
            var unusedLines = new HashSet<int>();
            foreach (var diag in semanticDiagnostics)
            {
                if (diag.Id == "CS8019")
                {
                    unusedLines.Add(diag.Line);
                }
            }

            if (unusedLines.Count == 0) return 0;

            // 5. 移除未使用的 using 行
            string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var newLines = new List<string>();
            var removedNamespaces = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (unusedLines.Contains(i))
                {
                    string trimmed = lines[i].TrimStart();
                    if (trimmed.StartsWith("using ") && trimmed.EndsWith(";"))
                    {
                        string ns = trimmed.Substring(6, trimmed.Length - 7).Trim();
                        if (!ns.StartsWith("static ") && !ns.Contains(" = "))
                        {
                            removedNamespaces.Add(ns);
                        }
                    }
                    continue;
                }
                newLines.Add(lines[i]);
            }

            if (removedNamespaces.Count == 0) return 0;

            string newline = content.Contains("\r\n") ? "\r\n" :
                             (content.Contains("\n") ? "\n" : "\r");
            string newContent = string.Join(newline, newLines);

            if (newContent != content)
            {
                File.WriteAllText(filePath, newContent);
                EmberDebug.Log(TAG, EditorToolUtility.L10n(
                    EmberEditorWindow.GlobalLang,
                    $"Removed {removedNamespaces.Count} unused using(s) from {Path.GetFileName(filePath)}: {string.Join(", ", removedNamespaces)}",
                    $"从 {Path.GetFileName(filePath)} 移除了 {removedNamespaces.Count} 个未使用的 using：{string.Join(", ", removedNamespaces)}"));
                return removedNamespaces.Count;
            }

            return 0;
        }

        #endregion
    }
}
#endif
