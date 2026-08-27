// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 框架代码规范校验器 —— 编译完成后扫描所有 .cs 文件，检测禁止的 API 调用。
    ///
    /// 检测规则：
    /// - Debug.Log / Debug.LogWarning / Debug.LogError / Debug.LogFormat → 请使用 EmberDebug
    /// - GameObject.Find → 请使用 EmberServiceLocator 或注册机制
    /// - FindObjectOfType → 请使用 EmberServiceLocator 或注册机制
    ///
    /// 白名单文件：
    /// - EmberDebug.cs（日志类自身）
    /// - EmberCodeValidator.cs（本文件）
    /// 排除目录：见 ExcludedFolders.json（默认排除 Plugins/、ThirdParty/）
    /// </summary>
    [InitializeOnLoad]
    public class EmberCodeValidator
    {
        private const string TAG = LogTags.EmberBasic + "." + nameof(EmberCodeValidator);
        private const string EmberDebugFile = "EmberDebug.cs";
        private const string ValidatorFile = "EmberCodeValidator.cs";

        private static readonly Regex DebugLogPattern = new(
            @"(?<!Ember)(?<!\.)Debug\.(Log|LogWarning|LogError|LogFormat|LogException|LogAssertion)\s*\(",
            RegexOptions.Compiled);

        private static readonly Regex GameObjectFindPattern = new(
            @"GameObject\.Find\s*\(",
            RegexOptions.Compiled);

        private static readonly Regex FindObjectOfTypePattern = new(
            @"FindObjectOfType\s*[<\(]",
            RegexOptions.Compiled);

        static EmberCodeValidator()
        {
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        [MenuItem("Ember/Tool/校验代码规范", false, 350)]
        public static void ManualValidate()
        {
            ValidateAllScripts(interactive: true);
        }

        private static void OnCompilationFinished(object obj)
        {
            ValidateAllScripts(interactive: false);
        }

        private static void ValidateAllScripts(bool interactive)
        {
            var violations = new List<Violation>();
            var csFiles = AssetDatabase.FindAssets("t:MonoScript")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".cs") && File.Exists(p))
                .ToList();

            foreach (var path in csFiles)
            {
                // 排除用户配置的文件夹（默认包含 Plugins/、ThirdParty/）
                if (EmberExcludedFolders.IsExcluded(path))
                    continue;

                // 检查是否为框架包路径（从 FrameworkPackageRoots 动态读取）
                bool isFrameworkPackage = false;
                foreach (var root in EmberExcludedFolders.FrameworkPackageRoots)
                {
                    if (path.StartsWith(root, System.StringComparison.OrdinalIgnoreCase))
                    {
                        isFrameworkPackage = true;
                        break;
                    }
                }

                // 只扫描 ember 框架包目录 + 业务层
                if (!path.StartsWith("Assets/Game/")
                    && !isFrameworkPackage)
                    continue;

                string fileName = Path.GetFileName(path);
                if (fileName == EmberDebugFile || fileName == ValidatorFile) continue;

                string content;
                try { content = File.ReadAllText(path); }
                catch { continue; }

                CheckLineByLine(path, content, DebugLogPattern, "禁止使用 Debug.Log*，请用 EmberDebug", violations);
                CheckLineByLine(path, content, GameObjectFindPattern, "禁止使用 GameObject.Find，请用 EmberServiceLocator", violations);
                CheckLineByLine(path, content, FindObjectOfTypePattern, "禁止使用 FindObjectOfType，请用 EmberServiceLocator", violations);
            }

            if (violations.Count > 0)
            {
                foreach (var v in violations)
                {
                    EmberDebug.LogWarning(TAG, $"{v.Message}\n<b>{v.Path}:{v.Line}</b>\n{v.Content}");
                }

                EmberDebug.LogWarning(TAG, $"代码规范校验发现 {violations.Count} 处违规。");
            }
            else if (interactive)
            {
                var lang = EmberEditorWindow.GlobalLang;
                EditorUtility.DisplayDialog(
                    EditorToolUtility.L10n(lang, "Ember Code Validator", "Ember 代码校验器"),
                    EditorToolUtility.L10n(lang,
                        "All code standards checks passed.\n\nChecks:\n  • Debug.Log* → EmberDebug\n  • GameObject.Find → EmberServiceLocator\n  • FindObjectOfType → EmberServiceLocator",
                        "代码规范校验通过，未发现违规。\n\n检查项：\n  • Debug.Log* → EmberDebug\n  • GameObject.Find → EmberServiceLocator\n  • FindObjectOfType → EmberServiceLocator"),
                    "OK");
                EmberDebug.Log(TAG, EditorToolUtility.L10n(lang,
                    "Code standards validation passed — 0 violations.",
                    "代码规范校验通过，0 处违规。"));
            }
        }

        private static void CheckLineByLine(string path, string content, Regex pattern, string message, List<Violation> violations)
        {
            var lines = content.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("///")) continue;
                if (pattern.IsMatch(lines[i]))
                {
                    violations.Add(new Violation { Path = path, Line = i + 1, Message = $"{message}  →  ({Path.GetFileName(path)}:{i + 1})", Content = lines[i].Trim() });
                }
            }
        }

        private struct Violation
        {
            public string Path;
            public int Line;
            public string Message;
            public string Content;
        }
    }
}
#endif
