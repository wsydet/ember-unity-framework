// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

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
    /// 格式：项目名 [| 子路径] | 分支 | 当前模板 | 构建目标 | Git根目录
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
        private static readonly EmberTitleSnapshot Snapshot = new EmberTitleSnapshot();
        private static string _currentTitle;
        private static bool _disposed;
#if UNITY_EDITOR_OSX
        private static System.Threading.Tasks.Task<EmberTitleProcess.Result> _nativeTask;
        private static System.Threading.CancellationTokenSource _nativeCancellation = new System.Threading.CancellationTokenSource();
#endif
        internal static string LastQueryError => Snapshot.Current?.Error;
        internal static int QueryRequests => Snapshot.Requests;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        static EditorMainWindowTitle()
        {
            RegisterInternalTitleCallback();
            EditorApplication.delayCall += ApplyTitle;
            EditorApplication.update += RefreshTitlePeriodically;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.projectChanged += OnProjectChanged;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            EditorApplication.quitting += Dispose;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static void RefreshTitlePeriodically()
        {
            if (_disposed) return;
            bool due = EditorApplication.timeSinceStartup >= _nextRefreshTime;
            if (due)
            {
                _nextRefreshTime = EditorApplication.timeSinceStartup + RefreshIntervalSeconds;
                UpdateContext();
            }
            if (Snapshot.Tick(due) || due) ApplyTitle();
        }

        private static void ApplyTitle()
        {
            if (_disposed) return;
            var data = Snapshot.Current;
            _currentTitle = BuildProjectTitle(PlayerSettings.productName, data?.Root ?? GetProjectPath(),
                data?.Branch, EmberTitleSnapshot.TemplateId(data), GetBuildTargetLabel(), data?.GitRoot);

            if (RegisterInternalTitleCallback() && TryUpdateMainWindowTitle())
            {
                return;
            }

            TryApplyNativeWindowTitle(_currentTitle);
        }

        private static void UpdateContext()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(EditorMainWindowTitle).Assembly);
            Snapshot.SetContext(GetProjectPath(), package?.source == UnityEditor.PackageManager.PackageSource.Embedded);
        }

        private static void OnProjectChanged()
        {
#if UNITY_EDITOR_OSX
            _nativeCancellation.Cancel();
            var old = _nativeCancellation;
            if (_nativeTask != null) _nativeTask.ContinueWith(_ => old.Dispose());
            else old.Dispose();
            _nativeCancellation = new System.Threading.CancellationTokenSource();
#endif
            UpdateContext(); Snapshot.Invalidate(); Snapshot.Tick(true); ApplyTitle();
        }

        private static void OnPlayModeChanged(PlayModeStateChange _) => RefreshTitleByMenu();

        private static void Dispose()
        {
            _disposed = true;
            EditorApplication.delayCall -= ApplyTitle;
            EditorApplication.update -= RefreshTitlePeriodically;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.projectChanged -= OnProjectChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
            EditorApplication.quitting -= Dispose;
            try
            {
                if (_internalCallbackRegistered)
                    UpdateMainWindowTitleEvent.GetRemoveMethod(true)?.Invoke(null, new object[] { _updateMainWindowTitleDelegate });
            }
            catch (Exception e) { LogReflectionWarning(e); }
            finally { Snapshot.Dispose(); }
#if UNITY_EDITOR_OSX
            _nativeCancellation.Cancel();
            if (_nativeTask != null) _nativeTask.ContinueWith(_ => _nativeCancellation.Dispose());
            else _nativeCancellation.Dispose();
#endif
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
                if (DescriptorType.GetField(TitleFieldName, InstanceFlags) == null)
                    throw new MissingFieldException(DescriptorTypeName, TitleFieldName);
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
            if (_disposed || _currentTitle == null) return;
            descriptor.GetType()
                .GetField(TitleFieldName, InstanceFlags)
                ?.SetValue(descriptor, _currentTitle);
        }

        private static string GetProjectPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string NormalizeProjectPath(string projectPath)
        {
            var fullPath = Path.GetFullPath(projectPath);
            var root = Path.GetPathRoot(fullPath);
            return fullPath.Length > root.Length ? fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : root;
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

            var gitRootPrefix = gitRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
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
            if (_nativeTask != null && !_nativeTask.IsCompleted) return;
            var escapedTitle = title.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var script = $"tell application \"Unity\" to set title of front window to \"{escapedTitle}\"";
            var token = _nativeCancellation.Token;
            _nativeTask = System.Threading.Tasks.Task.Run(() => EmberTitleProcess.RunAsync(
                new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = "-e \"" + script.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""
                }, 1000, token));
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
            UpdateContext(); Snapshot.Tick(true);
            ApplyTitle();
        }

        /// <summary>
        /// 根据项目名和路径构建标题字符串。
        /// </summary>
        internal static string BuildProjectTitle(string projectName, string projectPath)
        {
            return BuildProjectTitle(
                projectName,
                projectPath,
                Snapshot.Current?.Root == NormalizeProjectPath(projectPath) ? Snapshot.Current.Branch : "?",
                Snapshot.Current?.Root == NormalizeProjectPath(projectPath) ? EmberTitleSnapshot.TemplateId(Snapshot.Current) : "?",
                GetBuildTargetLabel());
        }

        internal static string BuildProjectTitle(string projectName, string projectPath, string branch)
        {
            return BuildProjectTitle(
                projectName,
                projectPath,
                branch,
                Snapshot.Current?.Root == NormalizeProjectPath(projectPath) ? EmberTitleSnapshot.TemplateId(Snapshot.Current) : "?",
                GetBuildTargetLabel());
        }

        /// <summary>
        /// 根据项目名、路径、Git 分支和构建目标构建标题字符串。
        /// 格式：项目名 [| 子路径] | 分支 | 当前模板 | 构建目标 | Git根目录
        /// 当 Unity 工程位于 Git 仓库根目录（子路径为 "."）时省略子路径。
        /// </summary>
        internal static string BuildProjectTitle(string projectName, string projectPath, string branch, string buildTarget)
        {
            return BuildProjectTitle(
                projectName,
                projectPath,
                branch,
                Snapshot.Current?.Root == NormalizeProjectPath(projectPath) ? EmberTitleSnapshot.TemplateId(Snapshot.Current) : "?",
                buildTarget);
        }

        /// <summary>
        /// 根据项目名、路径、Git 分支、当前模板和构建目标构建标题字符串。
        /// 格式：项目名 [| 子路径] | 分支 | 当前模板 | 构建目标 | Git根目录
        /// 当 Unity 工程位于 Git 仓库根目录（子路径为 "."）时省略子路径。
        /// </summary>
        internal static string BuildProjectTitle(
            string projectName,
            string projectPath,
            string branch,
            string template,
            string buildTarget)
        {
            return BuildProjectTitle(projectName, projectPath, branch, template, buildTarget,
                EmberTitleSnapshot.FindGitRoot(NormalizeProjectPath(projectPath)));
        }

        internal static string BuildProjectTitle(string projectName, string projectPath, string branch,
            string template, string buildTarget, string gitRoot)
        {
            var normalizedPath = NormalizeProjectPath(projectPath);
            var normalizedProjectName = string.IsNullOrWhiteSpace(projectName)
                ? new DirectoryInfo(normalizedPath).Name
                : projectName.Trim();
            gitRoot = NormalizeProjectPath(gitRoot ?? normalizedPath);
            var projectSubPath = GetProjectSubPath(gitRoot, normalizedPath);
            var target = string.IsNullOrWhiteSpace(buildTarget) ? "Unknown" : buildTarget.Trim();
            var branchDisplay = string.IsNullOrWhiteSpace(branch) ? "?" : branch.Trim();
            var templateDisplay = string.IsNullOrWhiteSpace(template) ? "?" : template.Trim();

            if (projectSubPath == ".")
            {
                return $"{normalizedProjectName} | {branchDisplay} | {templateDisplay} | {target} | {ToDisplayPath(gitRoot)}";
            }

            return $"{normalizedProjectName} | {ToDisplayPath(projectSubPath)} | {branchDisplay} | {templateDisplay} | {target} | {ToDisplayPath(gitRoot)}";
        }

        #endregion
    }
}

#endif
