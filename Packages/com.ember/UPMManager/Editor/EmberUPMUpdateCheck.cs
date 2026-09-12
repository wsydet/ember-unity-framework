// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Ember.UPMManager.Editor
{
    /// <summary>非阻塞 Git 版本查询；由编辑器主线程轮询并持有生命周期。</summary>
    internal sealed class EmberUPMUpdateCheck : IDisposable
    {
        #region 内部参数

        private readonly Process _process;
        private readonly Task<string> _stdout;
        private readonly Task<string> _stderr;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly TimeSpan _timeout;
        private bool _disposed;

        internal TimeSpan Elapsed => _clock.Elapsed;
        internal bool IsCompleted { get; private set; }
        internal string Error { get; private set; }
        internal List<Version> Versions { get; private set; }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private static ProcessStartInfo CreateStartInfo(string repositoryUrl)
        {
            var info = new ProcessStartInfo("git", $"ls-remote --tags \"{repositoryUrl}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            // 后台检查不能挂在不可见的凭据输入上；已保存的凭据仍可使用。
            info.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
            info.EnvironmentVariables["GCM_INTERACTIVE"] = "Never";
            return info;
        }

        private static void ObserveCancelledRead(Task task)
        {
            // 关闭管道可能使尚未结束的读取失败；取消后没有 Poll 再观察这些异常。
            task?.ContinueWith(completed => { _ = completed.Exception; },
                System.Threading.CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        internal EmberUPMUpdateCheck(string repositoryUrl, TimeSpan? timeout = null)
        {
            _timeout = timeout ?? TimeSpan.FromSeconds(60);
            _process = new Process { StartInfo = CreateStartInfo(repositoryUrl) };
            try
            {
                if (!_process.Start())
                    throw new InvalidOperationException("无法启动 git，请确认已安装并加入 PATH。");

                // 同时排空两个管道，避免某个管道写满后阻塞 Git。
                _stdout = _process.StandardOutput.ReadToEndAsync();
                _stderr = _process.StandardError.ReadToEndAsync();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal void Poll()
        {
            if (IsCompleted || _disposed) return;
            try
            {
                if (Elapsed >= _timeout)
                {
                    Error = $"查询超时（{_timeout.TotalSeconds:0} 秒），请检查网络或 Git 凭据后重试。";
                    IsCompleted = true;
                }
                else if (_process.HasExited && _stdout.IsCompleted && _stderr.IsCompleted)
                {
                    var stdout = _stdout.GetAwaiter().GetResult();
                    var stderr = _stderr.GetAwaiter().GetResult();
                    if (_process.ExitCode != 0)
                        throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr)
                            ? $"git ls-remote 失败（exit {_process.ExitCode}）"
                            : stderr.Trim());

                    Versions = ParseVersions(stdout);
                    if (Versions.Count == 0)
                        throw new InvalidOperationException("远程未返回可识别的版本 tag，请检查仓库与版本命名。");
                    IsCompleted = true;
                }
            }
            catch (Exception exception)
            {
                Error = exception.Message;
                IsCompleted = true;
            }

            if (IsCompleted) Dispose();
        }

        internal static List<Version> ParseVersions(string stdout)
        {
            var versions = new List<Version>();
            foreach (var line in stdout.Split('\n'))
            {
                var idx = line.IndexOf("refs/tags/", StringComparison.Ordinal);
                if (idx < 0) continue;
                var tag = line.Substring(idx + "refs/tags/".Length).Trim();
                tag = tag.Replace("^{}", "").TrimEnd('^', '{', '}');
                if (tag.StartsWith("v", StringComparison.Ordinal)) tag = tag.Substring(1);
                if (Version.TryParse(tag, out var version)) versions.Add(version);
            }
            return versions.Distinct().ToList();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _clock.Stop();
            try
            {
                if (!_process.HasExited) _process.Kill();
            }
            catch (InvalidOperationException) { } // 尚未启动或已退出。
            catch (System.ComponentModel.Win32Exception) { } // 退出竞争或系统拒绝终止。
            finally
            {
                ObserveCancelledRead(_stdout);
                ObserveCancelledRead(_stderr);
                _process.Dispose();
            }
        }

        #endregion
    }
}
