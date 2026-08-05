// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember.basic

#if UNITY_EDITOR

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace Ember.Basic.Editor
{
    /// <summary>
    /// 自定义 Unity 编辑器主窗口标题。
    /// 格式：项目名 [| 子路径] | 分支 | 构建目标 | Git根目录
    /// 子路径仅在 Unity 工程非 Git 仓库根目录时显示。
    /// 通过反射 Hook Unity 内部标题 API，不可用时降级到原生窗口 API。
    /// </summary>
    [InitializeOnLoad]
    public static class EditorMainWindowTitle
    {
        #region 内部参数

        private const string TAG = LogTags.EmberBasic + "." + nameof(EditorMainWindowTitle);

        private const string DescriptorTypeName = "UnityEditor.ApplicationTitleDescriptor";
        private const string UpdateMainWindowTitleEventName = "updateMainWindowTitle";
        private const string UpdateMainWindowTitleMethodName = "UpdateMainWindowTitle";
        private const string TitleFieldName = "title";
        private const double RefreshIntervalSeconds = 5d;

        private static readonly BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Type EditorApplicationType = typeof(EditorApplication);
        private static readonly Type DescriptorType = EditorApplicationType.Assembly.GetType(DescriptorTypeName);
        private static readonly EventInfo UpdateMainWindowTitleEvent =
            EditorApplicationType.GetEvent(UpdateMainWindowTitleEventName, StaticFlags);
        private static readonly MethodInfo UpdateMainWindowTitleMethod =
            EditorApplicationType.GetMethod(UpdateMainWindowTitleMethodName, StaticFlags);

        private static Delegate _updateMainWindowTitleDelegate;
        private static bool _internalCallbackRegistered;
        private static bool _reflectionWarningLogged;
        private static double _nextRefreshTime;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        static EditorMainWindowTitle()
        {
            RegisterInternalTitleCallback();
            EditorApplication.delayCall += ApplyTitle;
            EditorApplication.update += RefreshTitlePeriodically;
            EditorApplication.playModeStateChanged += _ => ApplyTitle();
            EditorApplication.projectChanged += ApplyTitle;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static void RefreshTitlePeriodically()
        {
            if (EditorApplication.timeSinceStartup < _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime = EditorApplication.timeSinceStartup + RefreshIntervalSeconds;
            ApplyTitle();
        }

        private static void ApplyTitle()
        {
            var title = BuildCurrentTitle();

            if (RegisterInternalTitleCallback() && TryUpdateMainWindowTitle())
            {
                return;
            }

            TryApplyNativeWindowTitle(title);
        }

        private static bool RegisterInternalTitleCallback()
        {
            if (_internalCallbackRegistered)
            {
                return true;
            }

            if (DescriptorType == null || UpdateMainWindowTitleEvent == null)
            {
                LogReflectionWarning();
                return false;
            }

            try
            {
                var delegateType = typeof(Action<>).MakeGenericType(DescriptorType);
                var handler = ((Action<object>)UpdateTitleDescriptor).Method;
                _updateMainWindowTitleDelegate = Delegate.CreateDelegate(delegateType, null, handler);
                UpdateMainWindowTitleEvent.GetAddMethod(true)?.Invoke(null, new object[] { _updateMainWindowTitleDelegate });
                _internalCallbackRegistered = true;
                return true;
            }
            catch (Exception e)
            {
                LogReflectionWarning(e);
                return false;
            }
        }

        private static bool TryUpdateMainWindowTitle()
        {
            if (UpdateMainWindowTitleMethod == null)
            {
                LogReflectionWarning();
                return false;
            }

            try
            {
                UpdateMainWindowTitleMethod.Invoke(null, Array.Empty<object>());
                return true;
            }
            catch (Exception e)
            {
                LogReflectionWarning(e);
                return false;
            }
        }

        private static void UpdateTitleDescriptor(object descriptor)
        {
            descriptor.GetType()
                .GetField(TitleFieldName, InstanceFlags)
                ?.SetValue(descriptor, BuildCurrentTitle());
        }

        private static string BuildCurrentTitle()
        {
            var projectPath = GetProjectPath();
            var branch = GetGitBranch(projectPath);
            return BuildProjectTitle(PlayerSettings.productName, projectPath, branch);
        }

        private static string GetProjectPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string NormalizeProjectPath(string projectPath)
        {
            var fullPath = Path.GetFullPath(projectPath);
            return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static string FindGitRoot(string projectPath)
        {
            var directory = new DirectoryInfo(projectPath);
            while (directory != null)
            {
                var gitPath = Path.Combine(directory.FullName, ".git");
                if (Directory.Exists(gitPath) || File.Exists(gitPath))
                {
                    return NormalizeProjectPath(directory.FullName);
                }

                directory = directory.Parent;
            }

            return projectPath;
        }

        private static string GetGitBranch(string projectPath)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "branch --show-current",
                        WorkingDirectory = projectPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit(2000);

                // detached HEAD 时 --show-current 输出为空
                if (string.IsNullOrEmpty(output))
                {
                    return "HEAD";
                }

                return output;
            }
            catch
            {
                return "?";
            }
        }

        private static string GetProjectSubPath(string gitRoot, string projectPath)
        {
            if (string.IsNullOrEmpty(gitRoot))
            {
                return projectPath;
            }

            if (string.Equals(gitRoot, projectPath, GetPathComparison()))
            {
                return ".";
            }

            var gitRootPrefix = gitRoot + Path.DirectorySeparatorChar;
            return projectPath.StartsWith(gitRootPrefix, GetPathComparison())
                ? projectPath.Substring(gitRootPrefix.Length)
                : projectPath;
        }

        private static string GetBuildTargetLabel()
        {
            return EditorUserBuildSettings.activeBuildTarget.ToString();
        }

        private static string ToDisplayPath(string path)
        {
            return path?.Replace('\\', '/') ?? string.Empty;
        }

        private static StringComparison GetPathComparison()
        {
#if UNITY_EDITOR_WIN
            return StringComparison.OrdinalIgnoreCase;
#else
            return StringComparison.Ordinal;
#endif
        }

        private static void LogReflectionWarning(Exception exception = null)
        {
            if (_reflectionWarningLogged)
            {
                return;
            }

            _reflectionWarningLogged = true;
            var message = exception == null
                ? "[EditorMainWindowTitle] Unity internal title API is unavailable, fallback to native window title."
                : $"[EditorMainWindowTitle] Unity internal title API failed, fallback to native window title. {exception.Message}";
            EmberDebug.LogWarning(TAG, message);
        }

        private static void TryApplyNativeWindowTitle(string title)
        {
#if UNITY_EDITOR_WIN
            TryApplyNativeWindowTitleWin(title);
#elif UNITY_EDITOR_OSX
            TryApplyNativeWindowTitleMac(title);
#else
            // Linux: 无可靠的跨 DE 设置窗口标题方案，静默跳过
#endif
        }

#if UNITY_EDITOR_WIN
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetWindowText(IntPtr hWnd, string lpString);

        private static void TryApplyNativeWindowTitleWin(string title)
        {
            using var process = Process.GetCurrentProcess();
            process.Refresh();
            var handle = process.MainWindowHandle;
            if (handle != IntPtr.Zero)
            {
                SetWindowText(handle, title);
            }
        }
#endif

#if UNITY_EDITOR_OSX
        /// <summary>
        /// macOS 降级路径：通过 osascript 设置 Unity 窗口标题。
        /// 注意：此方法依赖 AppleScript 权限，且 Unity 窗口标题设置后
        /// 可能被 Unity 自身刷新覆盖（效果不如 Windows SetWindowText 稳定）。
        /// 尚未在 macOS 上实测验证。
        /// </summary>
        private static void TryApplyNativeWindowTitleMac(string title)
        {
            try
            {
                // 转义标题中的双引号和反斜杠，防止 osascript 语法错误
                var escapedTitle = title.Replace("\\", "\\\\").Replace("\"", "\\\"");
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "osascript",
                        Arguments = $"-e 'tell application \"Unity\" to set title of front window to \"{escapedTitle}\"'",
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit(1000);
            }
            catch
            {
                // osascript 不可用时静默降级（首次已通过 LogReflectionWarning 通知）
            }
        }
#endif

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>
        /// 手动刷新编辑器标题。
        /// </summary>
        [MenuItem("Ember/Tool/刷新编辑器标题", priority = 390)]
        private static void RefreshTitleByMenu()
        {
            ApplyTitle();
        }

        /// <summary>
        /// 根据项目名和路径构建标题字符串。
        /// </summary>
        internal static string BuildProjectTitle(string projectName, string projectPath)
        {
            var branch = GetGitBranch(projectPath);
            return BuildProjectTitle(projectName, projectPath, branch, GetBuildTargetLabel());
        }

        internal static string BuildProjectTitle(string projectName, string projectPath, string branch)
        {
            return BuildProjectTitle(projectName, projectPath, branch, GetBuildTargetLabel());
        }

        /// <summary>
        /// 根据项目名、路径、Git 分支和构建目标构建标题字符串。
        /// 格式：项目名 [| 子路径] | 分支 | 构建目标 | Git根目录
        /// 当 Unity 工程位于 Git 仓库根目录（子路径为 "."）时省略子路径。
        /// </summary>
        internal static string BuildProjectTitle(string projectName, string projectPath, string branch, string buildTarget)
        {
            var normalizedPath = NormalizeProjectPath(projectPath);
            var normalizedProjectName = string.IsNullOrWhiteSpace(projectName)
                ? new DirectoryInfo(normalizedPath).Name
                : projectName.Trim();
            var gitRoot = FindGitRoot(normalizedPath);
            var projectSubPath = GetProjectSubPath(gitRoot, normalizedPath);
            var target = string.IsNullOrWhiteSpace(buildTarget) ? "Unknown" : buildTarget.Trim();
            var branchDisplay = string.IsNullOrWhiteSpace(branch) ? "?" : branch.Trim();

            if (projectSubPath == ".")
            {
                return $"{normalizedProjectName} | {branchDisplay} | {target} | {ToDisplayPath(gitRoot)}";
            }

            return $"{normalizedProjectName} | {ToDisplayPath(projectSubPath)} | {branchDisplay} | {target} | {ToDisplayPath(gitRoot)}";
        }

        #endregion
    }
}

#endif
