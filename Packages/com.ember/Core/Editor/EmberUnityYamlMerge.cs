// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using Ember.Basic;

using UnityEditor;
using UnityEngine;

namespace Ember.Core.Editor
{
    /// <summary>UnityYAMLMerge 场景三方合并适配器；所有输入和输出都隔离在项目 Temp 目录。</summary>
    internal sealed class EmberUnityYamlMerge
    {
        #region 内部参数

        internal const int DefaultTimeoutMilliseconds = 30000;

        private const string MergeExecutableName = "UnityYAMLMerge";
        private const string RulesFileName = "mergerules.txt";

        private readonly IUnityYamlMergeProcessRunner _processRunner;
        private readonly IUnityYamlMergeEnvironment _environment;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal EmberUnityYamlMerge()
            : this(new UnityYamlMergeProcessRunner(), new UnityEditorYamlMergeEnvironment())
        {
        }

        internal EmberUnityYamlMerge(
            IUnityYamlMergeProcessRunner processRunner,
            IUnityYamlMergeEnvironment environment)
        {
            _processRunner = processRunner
                ?? throw new ArgumentNullException(nameof(processRunner));
            _environment = environment
                ?? throw new ArgumentNullException(nameof(environment));
        }

        /// <summary>定位 Unity 随附的 Smart Merge 工具和规则文件，不启动外部进程。</summary>
        internal bool TryGetToolInfo(out UnityYamlMergeToolInfo toolInfo, out string error)
        {
            toolInfo = null;
            error = null;
            if (string.IsNullOrWhiteSpace(_environment.ApplicationContentsPath))
            {
                error = "EditorApplication.applicationContentsPath 为空，无法定位 UnityYAMLMerge。";
                return false;
            }

            var toolsPath = Path.Combine(_environment.ApplicationContentsPath, "Tools");
            var executableName = _environment.IsWindowsEditor
                ? MergeExecutableName + ".exe"
                : MergeExecutableName;
            var executablePath = Path.Combine(toolsPath, executableName);
            var rulesPath = Path.Combine(toolsPath, RulesFileName);
            if (!File.Exists(executablePath))
            {
                error = $"UnityYAMLMerge 不存在：{executablePath}";
                return false;
            }
            if (!File.Exists(rulesPath))
            {
                error = $"UnityYAMLMerge 规则文件不存在：{rulesPath}";
                return false;
            }

            try
            {
                toolInfo = new UnityYamlMergeToolInfo(
                    executablePath,
                    rulesPath,
                    GetToolVersion(executablePath),
                    CryptographyUtils.GetMD5File(rulesPath));
                return true;
            }
            catch (Exception ex)
            {
                error = "读取 UnityYAMLMerge 工具信息失败：" + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 对 O/N/C 三份文本场景执行无 GUI、无外部 fallback 的语义三方合并。
        /// 正式输入只读；合并结果以字节返回，调用结束后隔离目录立即清理。
        /// </summary>
        internal UnityYamlMergeResult Merge(
            string oldScenePath,
            string parentScenePath,
            string childScenePath,
            int timeoutMilliseconds = DefaultTimeoutMilliseconds)
        {
            if (!_environment.IsForceText)
            {
                return UnityYamlMergeResult.Failed(
                    UnityYamlMergeStatus.ForceTextRequired,
                    "项目未启用 Force Text，场景语义合并已安全回退。",
                    null);
            }

            if (!TryValidateInputScene(oldScenePath, "O（旧父基线）", out var inputError)
                || !TryValidateInputScene(parentScenePath, "N（当前父模板）", out inputError)
                || !TryValidateInputScene(childScenePath, "C（当前派生模板）", out inputError))
            {
                return UnityYamlMergeResult.Failed(
                    UnityYamlMergeStatus.InvalidInput,
                    inputError,
                    null);
            }

            if (timeoutMilliseconds <= 0)
            {
                return UnityYamlMergeResult.Failed(
                    UnityYamlMergeStatus.InvalidInput,
                    "UnityYAMLMerge 超时必须大于 0。",
                    null);
            }

            if (!TryGetToolInfo(out var toolInfo, out var toolError))
            {
                return UnityYamlMergeResult.Failed(
                    UnityYamlMergeStatus.ToolUnavailable,
                    toolError,
                    null);
            }

            if (string.IsNullOrWhiteSpace(_environment.ProjectRoot))
            {
                return UnityYamlMergeResult.Failed(
                    UnityYamlMergeStatus.InvalidInput,
                    "无法解析 Unity 项目根目录，不能创建场景合并隔离区。",
                    toolInfo);
            }

            var mergeRoot = Path.Combine(
                _environment.ProjectRoot,
                "Temp",
                $"EmberSceneMerge-{Guid.NewGuid():N}");
            try
            {
                Directory.CreateDirectory(mergeRoot);
                var isolatedOld = Path.Combine(mergeRoot, "base.unity");
                var isolatedParent = Path.Combine(mergeRoot, "theirs.unity");
                var isolatedChild = Path.Combine(mergeRoot, "mine.unity");
                var mergedPath = Path.Combine(mergeRoot, "merged.unity");
                var reportPath = Path.Combine(mergeRoot, "report.txt");
                File.Copy(oldScenePath, isolatedOld, true);
                File.Copy(parentScenePath, isolatedParent, true);
                File.Copy(childScenePath, isolatedChild, true);

                var arguments = new List<string>
                {
                    "merge",
                    "-p",
                    "-h",
                    "--fallback",
                    "none",
                    "--rules",
                    toolInfo.RulesPath,
                    "--describe",
                    "-o",
                    reportPath,
                    isolatedOld,
                    isolatedParent,
                    isolatedChild,
                    mergedPath
                };
                var processResult = _processRunner.Run(
                    new UnityYamlMergeProcessRequest(
                        toolInfo.ExecutablePath,
                        arguments,
                        mergeRoot,
                        timeoutMilliseconds));
                if (processResult == null)
                {
                    return UnityYamlMergeResult.Failed(
                        UnityYamlMergeStatus.ProcessFailed,
                        "UnityYAMLMerge runner 未返回执行结果。",
                        toolInfo);
                }

                var report = ReadReport(reportPath, processResult);
                if (processResult.TimedOut)
                {
                    return UnityYamlMergeResult.Failed(
                        UnityYamlMergeStatus.TimedOut,
                        $"UnityYAMLMerge 超过 {timeoutMilliseconds} ms，进程已终止。",
                        toolInfo,
                        processResult,
                        report);
                }
                if (!processResult.Started)
                {
                    return UnityYamlMergeResult.Failed(
                        UnityYamlMergeStatus.ProcessFailed,
                        processResult.FailureReason ?? "UnityYAMLMerge 启动失败。",
                        toolInfo,
                        processResult,
                        report);
                }
                if (processResult.ExitCode != 0)
                {
                    return UnityYamlMergeResult.Failed(
                        UnityYamlMergeStatus.Conflict,
                        $"UnityYAMLMerge 返回退出码 {processResult.ExitCode}，场景仍需整场景人工选择。",
                        toolInfo,
                        processResult,
                        report);
                }
                if (!File.Exists(mergedPath))
                {
                    return UnityYamlMergeResult.Failed(
                        UnityYamlMergeStatus.InvalidOutput,
                        "UnityYAMLMerge 未生成结果文件。",
                        toolInfo,
                        processResult,
                        report);
                }

                var mergedBytes = File.ReadAllBytes(mergedPath);
                if (!HasUnityYamlHeader(mergedBytes))
                {
                    return UnityYamlMergeResult.Failed(
                        UnityYamlMergeStatus.InvalidOutput,
                        "UnityYAMLMerge 结果为空或不是有效的 Unity YAML。",
                        toolInfo,
                        processResult,
                        report);
                }

                var mergedText = Encoding.UTF8.GetString(mergedBytes);
                if (ContainsConflictMarkers(mergedText))
                {
                    return UnityYamlMergeResult.Failed(
                        UnityYamlMergeStatus.Conflict,
                        "UnityYAMLMerge 结果仍含冲突标记，已安全回退整场景选择。",
                        toolInfo,
                        processResult,
                        report);
                }

                return UnityYamlMergeResult.Succeeded(
                    mergedBytes,
                    CryptographyUtils.GetMD5File(mergedPath),
                    toolInfo,
                    processResult,
                    report);
            }
            catch (Exception ex)
            {
                return UnityYamlMergeResult.Failed(
                    UnityYamlMergeStatus.ProcessFailed,
                    "UnityYAMLMerge 适配器执行失败：" + ex.Message,
                    toolInfo);
            }
            finally
            {
                try
                {
                    if (Directory.Exists(mergeRoot))
                        Directory.Delete(mergeRoot, true);
                }
                catch
                {
                    // Temp 清理失败不能改变合并成功/失败语义，也不会触碰正式模板。
                }
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static bool TryValidateInputScene(
            string scenePath,
            string label,
            out string error)
        {
            if (string.IsNullOrWhiteSpace(scenePath)
                || !string.Equals(
                    Path.GetExtension(scenePath),
                    ".unity",
                    StringComparison.OrdinalIgnoreCase))
            {
                error = $"{label}不是 .unity 场景：{scenePath}";
                return false;
            }
            if (!File.Exists(scenePath))
            {
                error = $"{label}场景不存在：{scenePath}";
                return false;
            }

            try
            {
                if (!HasUnityYamlHeader(File.ReadAllBytes(scenePath)))
                {
                    error = $"{label}不是有效的文本 Unity YAML：{scenePath}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = $"读取{label}场景失败：{ex.Message}";
                return false;
            }

            error = null;
            return true;
        }

        private static bool HasUnityYamlHeader(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return false;
            var text = Encoding.UTF8.GetString(bytes);
            if (text.Length > 0 && text[0] == '\uFEFF')
                text = text.Substring(1);
            using var reader = new StringReader(text);
            return string.Equals(reader.ReadLine(), "%YAML 1.1", StringComparison.Ordinal)
                && string.Equals(
                    reader.ReadLine(),
                    "%TAG !u! tag:unity3d.com,2011:",
                    StringComparison.Ordinal);
        }

        private static bool ContainsConflictMarkers(string text)
        {
            return text.Contains("<<<<<<<", StringComparison.Ordinal)
                || text.Contains(">>>>>>>", StringComparison.Ordinal);
        }

        private static UnityYamlMergeReport ReadReport(
            string reportPath,
            UnityYamlMergeProcessResult processResult)
        {
            string fullReport;
            try
            {
                fullReport = File.Exists(reportPath)
                    ? File.ReadAllText(reportPath, Encoding.UTF8)
                    : string.Empty;
            }
            catch (Exception ex)
            {
                fullReport = "读取 UnityYAMLMerge report 失败：" + ex.Message;
            }

            if (string.IsNullOrEmpty(fullReport))
            {
                fullReport = (processResult?.StandardOutput ?? string.Empty)
                    + "\n"
                    + (processResult?.StandardError ?? string.Empty);
            }

            const int summaryLimit = 2048;
            var summary = fullReport.Length <= summaryLimit
                ? fullReport
                : fullReport.Substring(0, summaryLimit);
            return new UnityYamlMergeReport(
                summary.Trim(),
                CryptographyUtils.GetMD5(fullReport));
        }

        private static string GetToolVersion(string executablePath)
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
            if (!string.IsNullOrWhiteSpace(versionInfo.ProductVersion))
                return versionInfo.ProductVersion;
            if (!string.IsNullOrWhiteSpace(versionInfo.FileVersion))
                return versionInfo.FileVersion;
            return "md5:" + CryptographyUtils.GetMD5File(executablePath);
        }

        #endregion
    }

    internal enum UnityYamlMergeStatus
    {
        Success,
        ToolUnavailable,
        ForceTextRequired,
        InvalidInput,
        Conflict,
        TimedOut,
        ProcessFailed,
        InvalidOutput
    }

    /// <summary>一次场景语义合并的结构化结果；失败结果不携带可应用内容。</summary>
    internal sealed class UnityYamlMergeResult
    {
        #region 内部参数

        internal UnityYamlMergeStatus Status { get; }
        internal bool IsSuccess => Status == UnityYamlMergeStatus.Success;
        internal string FailureReason { get; }
        internal byte[] MergedBytes { get; }
        internal string MergedHash { get; }
        internal string ToolVersion { get; }
        internal string RulesHash { get; }
        internal string ReportSummary { get; }
        internal string ReportDigest { get; }
        internal bool ProcessStarted { get; }
        internal bool TimedOut { get; }
        internal int ExitCode { get; }
        internal string StandardOutput { get; }
        internal string StandardError { get; }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        private UnityYamlMergeResult(
            UnityYamlMergeStatus status,
            string failureReason,
            byte[] mergedBytes,
            string mergedHash,
            UnityYamlMergeToolInfo toolInfo,
            UnityYamlMergeProcessResult processResult,
            UnityYamlMergeReport report)
        {
            Status = status;
            FailureReason = failureReason;
            MergedBytes = mergedBytes;
            MergedHash = mergedHash;
            ToolVersion = toolInfo?.ToolVersion;
            RulesHash = toolInfo?.RulesHash;
            ReportSummary = report?.Summary;
            ReportDigest = report?.Digest;
            ProcessStarted = processResult?.Started ?? false;
            TimedOut = processResult?.TimedOut ?? false;
            ExitCode = processResult?.ExitCode ?? -1;
            StandardOutput = processResult?.StandardOutput;
            StandardError = processResult?.StandardError;
        }

        internal static UnityYamlMergeResult Succeeded(
            byte[] mergedBytes,
            string mergedHash,
            UnityYamlMergeToolInfo toolInfo,
            UnityYamlMergeProcessResult processResult,
            UnityYamlMergeReport report)
        {
            return new UnityYamlMergeResult(
                UnityYamlMergeStatus.Success,
                null,
                mergedBytes,
                mergedHash,
                toolInfo,
                processResult,
                report);
        }

        internal static UnityYamlMergeResult Failed(
            UnityYamlMergeStatus status,
            string failureReason,
            UnityYamlMergeToolInfo toolInfo,
            UnityYamlMergeProcessResult processResult = null,
            UnityYamlMergeReport report = null)
        {
            return new UnityYamlMergeResult(
                status,
                failureReason,
                null,
                null,
                toolInfo,
                processResult,
                report);
        }

        #endregion
    }

    /// <summary>从 EditorApplication.applicationContentsPath 推导出的工具指纹。</summary>
    internal sealed class UnityYamlMergeToolInfo
    {
        #region 内部参数

        internal string ExecutablePath { get; }
        internal string RulesPath { get; }
        internal string ToolVersion { get; }
        internal string RulesHash { get; }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal UnityYamlMergeToolInfo(
            string executablePath,
            string rulesPath,
            string toolVersion,
            string rulesHash)
        {
            ExecutablePath = executablePath;
            RulesPath = rulesPath;
            ToolVersion = toolVersion;
            RulesHash = rulesHash;
        }

        #endregion
    }

    internal sealed class UnityYamlMergeReport
    {
        #region 内部参数

        internal string Summary { get; }
        internal string Digest { get; }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal UnityYamlMergeReport(string summary, string digest)
        {
            Summary = summary;
            Digest = digest;
        }

        #endregion
    }

    internal interface IUnityYamlMergeEnvironment
    {
        string ApplicationContentsPath { get; }
        string ProjectRoot { get; }
        bool IsForceText { get; }
        bool IsWindowsEditor { get; }
    }

    internal sealed class UnityEditorYamlMergeEnvironment : IUnityYamlMergeEnvironment
    {
        public string ApplicationContentsPath => EditorApplication.applicationContentsPath;

        public string ProjectRoot => Directory.GetParent(Application.dataPath)?.FullName;

        public bool IsForceText => EditorSettings.serializationMode == SerializationMode.ForceText;

        public bool IsWindowsEditor => Application.platform == RuntimePlatform.WindowsEditor;
    }

    internal interface IUnityYamlMergeProcessRunner
    {
        UnityYamlMergeProcessResult Run(UnityYamlMergeProcessRequest request);
    }

    internal sealed class UnityYamlMergeProcessRequest
    {
        #region 内部参数

        internal string ExecutablePath { get; }
        internal IReadOnlyList<string> Arguments { get; }
        internal string WorkingDirectory { get; }
        internal int TimeoutMilliseconds { get; }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal UnityYamlMergeProcessRequest(
            string executablePath,
            IReadOnlyList<string> arguments,
            string workingDirectory,
            int timeoutMilliseconds)
        {
            ExecutablePath = executablePath;
            Arguments = arguments;
            WorkingDirectory = workingDirectory;
            TimeoutMilliseconds = timeoutMilliseconds;
        }

        #endregion
    }

    internal sealed class UnityYamlMergeProcessResult
    {
        #region 内部参数

        internal bool Started { get; }
        internal int ExitCode { get; }
        internal bool TimedOut { get; }
        internal string StandardOutput { get; }
        internal string StandardError { get; }
        internal string FailureReason { get; }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal UnityYamlMergeProcessResult(
            bool started,
            int exitCode,
            bool timedOut,
            string standardOutput,
            string standardError,
            string failureReason)
        {
            Started = started;
            ExitCode = exitCode;
            TimedOut = timedOut;
            StandardOutput = standardOutput;
            StandardError = standardError;
            FailureReason = failureReason;
        }

        #endregion
    }

    /// <summary>无 shell 的外部进程 runner；参数逐项加入 ArgumentList，超时后终止进程。</summary>
    internal sealed class UnityYamlMergeProcessRunner : IUnityYamlMergeProcessRunner
    {
        #region 外部方法

        public UnityYamlMergeProcessResult Run(UnityYamlMergeProcessRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var standardOutput = new StringBuilder();
            var standardError = new StringBuilder();
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = request.ExecutablePath,
                    WorkingDirectory = request.WorkingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                foreach (var argument in request.Arguments)
                    startInfo.ArgumentList.Add(argument ?? string.Empty);

                using var process = new Process { StartInfo = startInfo };
                process.OutputDataReceived += (_, args) =>
                {
                    if (args.Data != null) standardOutput.AppendLine(args.Data);
                };
                process.ErrorDataReceived += (_, args) =>
                {
                    if (args.Data != null) standardError.AppendLine(args.Data);
                };

                if (!process.Start())
                {
                    return new UnityYamlMergeProcessResult(
                        false,
                        -1,
                        false,
                        standardOutput.ToString(),
                        standardError.ToString(),
                        "UnityYAMLMerge 进程未启动。");
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                if (!process.WaitForExit(request.TimeoutMilliseconds))
                {
                    try { process.Kill(); }
                    catch { /* 后续仍按超时失败处理。 */ }
                    try { process.WaitForExit(5000); }
                    catch { /* 仅等待输出管道收尾。 */ }
                    return new UnityYamlMergeProcessResult(
                        true,
                        -1,
                        true,
                        standardOutput.ToString(),
                        standardError.ToString(),
                        null);
                }

                // 无超时版本的 WaitForExit 用于等待异步 stdout/stderr 事件完全排空。
                process.WaitForExit();
                return new UnityYamlMergeProcessResult(
                    true,
                    process.ExitCode,
                    false,
                    standardOutput.ToString(),
                    standardError.ToString(),
                    null);
            }
            catch (Exception ex)
            {
                return new UnityYamlMergeProcessResult(
                    false,
                    -1,
                    false,
                    standardOutput.ToString(),
                    standardError.ToString(),
                    ex.Message);
            }
        }

        #endregion
    }
}
