// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Ember.UPMManager.Editor
{
    /// <summary>浅克隆与稀疏检出技能目录；不检出框架、模板或项目资产。</summary>
    internal sealed class EmberAISkillDownload : IDisposable
    {
        #region 内部参数
        private readonly string _directory;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly TimeSpan _timeout;
        private Process _process;
        private Task<string> _stdout;
        private Task<string> _stderr;
        private int _step;
        private bool _disposed;
        internal bool IsCompleted { get; private set; }
        internal string Error { get; private set; }
        internal string Commit { get; private set; }
        internal string SkillsRoot => Path.Combine(_directory, ".agents", "skills");
        internal TimeSpan Elapsed => _clock.Elapsed;
        internal string Progress => _step == 0 ? "正在连接技能仓库" : "正在读取技能目录";
        #endregion

        #region 内部方法
        private static string Quote(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.IndexOfAny(new[] { '"', '\r', '\n', '\0' }) >= 0
                || value.EndsWith("\\", StringComparison.Ordinal))
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
            info.EnvironmentVariables["GIT_LFS_SKIP_SMUDGE"] = "1";
            _process = new Process { StartInfo = info };
            if (!_process.Start()) throw new InvalidOperationException("无法启动 Git，请确认已安装并加入 PATH。");
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
                Observe(_stdout); Observe(_stderr);
                _process.Dispose(); _process = null;
            }
        }

        private static void Observe(Task task) => task?.ContinueWith(t => { _ = t.Exception; },
            CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        #endregion

        #region 外部方法
        internal EmberAISkillDownload(string repository, string revision, string cacheRoot, TimeSpan? timeout = null)
        {
            if (string.IsNullOrWhiteSpace(revision) || !Regex.IsMatch(revision, @"\A[A-Za-z0-9][A-Za-z0-9._/-]*\z")
                || revision.Contains("..")) throw new ArgumentException("请填写有效的分支或标签名称。");
            _timeout = timeout ?? TimeSpan.FromSeconds(120);
            _directory = Path.Combine(Path.GetFullPath(cacheRoot), "skills-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(cacheRoot);
                // The server may ignore partial filtering; sparse checkout still limits installed files.
                Start("-c core.autocrlf=false clone --no-checkout --filter=blob:none --depth=1 --single-branch --branch "
                    + Quote(revision) + " -- " + Quote(repository) + " " + Quote(_directory));
            }
            catch { Dispose(); throw; }
        }

        internal void Poll()
        {
            if (_disposed || IsCompleted) return;
            try
            {
                if (Elapsed >= _timeout) throw new TimeoutException("技能下载超时，请检查网络与 Git 凭据后重试。");
                if (!_process.HasExited || !_stdout.IsCompleted || !_stderr.IsCompleted) return;
                string output = _stdout.GetAwaiter().GetResult();
                string error = _stderr.GetAwaiter().GetResult();
                if (_process.ExitCode != 0) throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(error) ? "Git 未能读取技能目录。" : error.Trim());
                ReleaseProcess();
                string prefix = "-c core.autocrlf=false -c core.hooksPath=" + Quote(Path.Combine(_directory, ".git", "disabled-hooks"))
                    + " -C " + Quote(_directory) + " ";
                switch (_step++)
                {
                    case 0: Start(prefix + "ls-tree -r HEAD -- .agents/skills"); break;
                    case 1:
                        if (string.IsNullOrWhiteSpace(output) || Regex.IsMatch(output, @"(?m)^(120000|160000) "))
                            throw new InvalidDataException("技能目录为空或包含符号链接/子模块，停止下载。");
                        Start(prefix + "sparse-checkout set --no-cone \"/.agents/skills/\""); break;
                    case 2: Start(prefix + "checkout --force HEAD"); break;
                    case 3: Start(prefix + "rev-parse HEAD"); break;
                    default:
                        Commit = output.Trim();
                        if (!Regex.IsMatch(Commit, @"\A[0-9a-f]{40,64}\z") || !Directory.Exists(SkillsRoot))
                            throw new InvalidOperationException("所选版本没有 AI Skill 目录，请选择包含技能的分支或标签。");
                        IsCompleted = true;
                        break;
                }
            }
            catch (Exception exception) { Error = exception.Message; IsCompleted = true; ReleaseProcess(); }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _clock.Stop(); ReleaseProcess();
            // Only this request's uniquely owned cache; never delete a project or installed skill.
            Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(_directory)) return;
                    foreach (string file in Directory.GetFiles(_directory, "*", SearchOption.AllDirectories))
                        File.SetAttributes(file, FileAttributes.Normal);
                    Directory.Delete(_directory, true);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            });
        }
        #endregion
    }
}
