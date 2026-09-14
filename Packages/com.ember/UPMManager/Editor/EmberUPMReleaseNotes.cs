// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Ember.UPMManager.Editor
{
    /// <summary>通过现有 Git 凭据异步读取固定版本的 CHANGELOG，不检出项目资产。</summary>
    internal sealed class EmberUPMReleaseNotes : IDisposable
    {
        #region 内部参数

        private readonly string _directory;
        private readonly TimeSpan _timeout;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private Process _process;
        private Task<string> _stdout;
        private Task<string> _stderr;
        private bool _reading;
        private bool _disposed;
        internal bool IsCompleted { get; private set; }
        internal string Error { get; private set; }
        internal Dictionary<Version, string> Notes { get; private set; }

        #endregion

        #region 内部方法

        private static string Quote(string value)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOfAny(new[] { '"', '\r', '\n', '\0' }) >= 0)
                throw new ArgumentException("Git 参数无效。");
            return "\"" + value + "\"";
        }

        private void Start(string arguments)
        {
            var info = new ProcessStartInfo("git", arguments)
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
            };
            info.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
            info.EnvironmentVariables["GCM_INTERACTIVE"] = "Never";
            _process = new Process { StartInfo = info };
            if (!_process.Start()) throw new InvalidOperationException("无法启动 Git。");
            _stdout = _process.StandardOutput.ReadToEndAsync();
            _stderr = _process.StandardError.ReadToEndAsync();
        }

        private void ReleaseProcess()
        {
            if (_process == null) return;
            try { if (!_process.HasExited) _process.Kill(); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
            finally
            {
                ObserveRead(_stdout);
                ObserveRead(_stderr);
                _process.Dispose();
                _process = null;
            }
        }

        private static void ObserveRead(Task task)
        {
            task?.ContinueWith(t => { _ = t.Exception; }, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        #endregion

        #region 外部方法

        internal EmberUPMReleaseNotes(string repository, Version version, string cacheRoot, TimeSpan? timeout = null)
        {
            _timeout = timeout ?? TimeSpan.FromSeconds(60);
            _directory = Path.Combine(Path.GetFullPath(cacheRoot), "notes-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(cacheRoot);
                // Bare + partial clone: no worktree, only the selected tag's tree and requested text blob.
                Start("clone --bare --filter=blob:none --depth=1 --single-branch --branch " +
                    Quote("v" + version) + " -- " + Quote(repository) + " " + Quote(_directory));
            }
            catch { Dispose(); throw; }
        }

        internal void Poll()
        {
            if (_disposed || IsCompleted) return;
            try
            {
                if (_clock.Elapsed >= _timeout)
                    throw new TimeoutException("更新内容加载超时，请检查网络或 Git 凭据后重试。");
                if (!_process.HasExited || !_stdout.IsCompleted || !_stderr.IsCompleted) return;
                string output = _stdout.GetAwaiter().GetResult();
                string error = _stderr.GetAwaiter().GetResult();
                if (_process.ExitCode != 0)
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? "Git 未能读取更新内容。" : error.Trim());
                ReleaseProcess();
                if (!_reading)
                {
                    _reading = true;
                    Start("-C " + Quote(_directory) + " show HEAD:Packages/com.ember/CHANGELOG.md");
                    return;
                }
                Notes = Parse(output);
                if (Notes.Count == 0) throw new InvalidOperationException("发布日志中没有可识别的版本说明。");
                IsCompleted = true;
            }
            catch (Exception ex) { Error = ex.Message; IsCompleted = true; }
            if (IsCompleted) Dispose();
        }

        internal static Dictionary<Version, string> Parse(string markdown)
        {
            var result = new Dictionary<Version, string>();
            Version version = null;
            var body = new StringBuilder();
            char fence = '\0';
            foreach (string line in (markdown ?? "").TrimStart('\uFEFF').Replace("\r\n", "\n").Split('\n'))
            {
                string trimmed = line.TrimStart();
                if (trimmed.StartsWith("```") || trimmed.StartsWith("~~~"))
                    fence = fence == '\0' ? trimmed[0] : fence == trimmed[0] ? '\0' : fence;
                if (fence == '\0' && Regex.IsMatch(line, @"^##\s+"))
                {
                    if (version != null && !result.ContainsKey(version)) result.Add(version, body.ToString().Trim());
                    body.Clear();
                    version = null;
                    var match = Regex.Match(line, @"^##\s+\[?v?(\d+\.\d+\.\d+(?:\.\d+)?)\]?(?:\s|$)");
                    if (match.Success) Version.TryParse(match.Groups[1].Value, out version);
                }
                else if (version != null) body.AppendLine(line);
            }
            if (version != null && !result.ContainsKey(version)) result.Add(version, body.ToString().Trim());
            return result;
        }

        internal static string ToDisplayText(string markdown)
        {
            string text = Regex.Replace(markdown ?? "", @"(?m)^#{1,6}\s+", "");
            text = Regex.Replace(text, @"\[([^\]]+)\]\(([^)]+)\)", "$1（$2）");
            return text.Replace("**", "").Replace("`", "");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _clock.Stop();
            ReleaseProcess();
            // Only delete this request's uniquely owned bare repository; cleanup must not stall IMGUI.
            Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(_directory)) return;
                    foreach (string file in Directory.GetFiles(_directory, "*", SearchOption.AllDirectories))
                        File.SetAttributes(file, FileAttributes.Normal);
                    Directory.Delete(_directory, true);
                }
                catch (IOException) { } // Cancelled Git helpers may still hold a file; Library is disposable.
                catch (UnauthorizedAccessException) { }
            });
        }

        #endregion
    }
}
