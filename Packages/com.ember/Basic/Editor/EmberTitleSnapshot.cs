// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Ember.Basic.Editor.Tests")]

namespace Ember.Basic.Editor
{
    /// <summary>标题专用显示缓存，不参与模板或扩展资格判断。仅主线程访问状态。</summary>
    internal sealed class EmberTitleSnapshot : IDisposable
    {
        #region 内部参数
        internal sealed class Data
        {
            internal string Root, GitRoot, Branch = "?", TemplateJson, Error;
            internal bool Embedded;
        }

        private readonly Func<string, bool, CancellationToken, Task<Data>> _read;
        private Task<Data> _pending;
        private CancellationTokenSource _cancellation;
        private string _root;
        private bool _embedded, _disposed, _dirty = true;
        private long _generation, _pendingGeneration;
        internal Data Current { get; private set; }
        internal int Requests { get; private set; }
        internal bool InFlight => _pending != null;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal EmberTitleSnapshot(Func<string, bool, CancellationToken, Task<Data>> read = null)
        {
            _read = read ?? ReadAsync;
        }

        internal void SetContext(string root, bool embedded)
        {
            if (_disposed || (_root == root && _embedded == embedded)) return;
            _root = root; _embedded = embedded;
            Current = new Data { Root = root, GitRoot = root, Embedded = embedded };
            Invalidate();
        }

        internal void Invalidate()
        {
            if (_disposed) return;
            _generation++; _dirty = true;
            _cancellation?.Cancel();
        }

        // 返回 true 仅表示可发布的新快照。旧请求结束前不启动另一请求。
        internal bool Tick(bool request)
        {
            if (_disposed) return false;
            _dirty |= request;
            bool changed = false;
            if (_pending?.IsCompleted == true)
            {
                Data result;
                try { result = _pending.GetAwaiter().GetResult(); }
                catch (Exception e) { result = new Data { Root = _root, GitRoot = _root, Embedded = _embedded, Error = e.Message }; }
                _pending = null;
                _cancellation.Dispose(); _cancellation = null;
                if (_pendingGeneration == _generation)
                {
                    Current = result;
                    changed = true;
                }
            }
            if (_dirty && _pending == null && _root != null)
            {
                _dirty = false;
                _pendingGeneration = _generation;
                _cancellation = new CancellationTokenSource();
                string root = _root; bool embedded = _embedded; var token = _cancellation.Token;
                Requests++;
                // 即使替换 reader 也保证不在主线程执行文件/进程操作。
                _pending = Task.Run(() => _read(root, embedded, token));
            }
            return changed;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true; _generation++;
            var cancellation = _cancellation;
            cancellation?.Cancel();
            if (_pending != null)
                _pending.ContinueWith(t => { _ = t.Exception; cancellation.Dispose(); }, TaskScheduler.Default);
            else cancellation?.Dispose();
            Current = null;
        }

        internal static async Task<Data> ReadAsync(string root, bool embedded, CancellationToken token)
        {
            var data = new Data { Root = root, GitRoot = root, Embedded = embedded };
            try
            {
                data.GitRoot = FindGitRoot(root);
                string record = embedded ? "EmberEditingTemplate.json" : "EmberDeployedTemplates.json";
                string path = Path.Combine(root, "Assets/Editor", record);
                if (File.Exists(path)) data.TemplateJson = File.ReadAllText(path);
            }
            catch (Exception e) { data.Error = "metadata: " + e.Message; }
            var result = await EmberTitleProcess.RunAsync(new ProcessStartInfo
            {
                FileName = "git", Arguments = "branch --show-current", WorkingDirectory = root
            }, 2000, token).ConfigureAwait(false);
            data.Branch = result.Success ? (string.IsNullOrWhiteSpace(result.Output) ? "HEAD" : result.Output.Trim()) : "?";
            if (!result.Success) data.Error = (data.Error == null ? "" : data.Error + "; ") + result.Error;
            return data;
        }

        internal static string FindGitRoot(string root)
        {
            for (var directory = new DirectoryInfo(root); directory != null; directory = directory.Parent)
            {
                string path = Path.Combine(directory.FullName, ".git");
                if (Directory.Exists(path) || File.Exists(path)) return directory.FullName;
            }
            return root;
        }

        internal static string TemplateId(Data data)
        {
            try
            {
                var record = JsonUtility.FromJson<TitleRecord>(data?.TemplateJson ?? "");
                string id = data.Embedded ? record?.templateId : record?.activeTemplateId;
                return string.IsNullOrWhiteSpace(id) ? "?" : id.Trim();
            }
            catch { return "?"; }
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        [Serializable] private sealed class TitleRecord
        {
            public string templateId = "";
            public string activeTemplateId = "";
        }
        #endregion
    }

    /// <summary>只在后台调用。退出、两路 EOF 和超时共享一个期限；输出有界，错误不逐次写 Console。</summary>
    internal static class EmberTitleProcess
    {
        #region 内部参数
        internal sealed class Result
        {
            internal bool Success;
            internal string Output, Error;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal static async Task<Result> RunAsync(ProcessStartInfo start, int timeoutMs, CancellationToken token)
        {
            using var process = new Process { StartInfo = start, EnableRaisingEvents = true };
            var output = new StringBuilder(); var error = new StringBuilder();
            var exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var stdout = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var stderr = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            process.Exited += (_, _) => exited.TrySetResult(true);
            process.OutputDataReceived += (_, e) => Drain(e.Data, output, stdout);
            process.ErrorDataReceived += (_, e) => Drain(e.Data, error, stderr);
            start.UseShellExecute = false; start.CreateNoWindow = true;
            start.RedirectStandardOutput = true; start.RedirectStandardError = true;
            bool started = false;
            // 域重载前同步发出终止信号，不能依赖旧域稍后继续调度 Task；不等待进程。
            using var stop = token.Register(() => { if (started) TryKill(process); });
            using var deadlineCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
            var deadline = Task.Delay(timeoutMs, deadlineCancellation.Token);
            try
            {
                token.ThrowIfCancellationRequested();
                started = process.Start();
                if (token.IsCancellationRequested) TryKill(process);
                process.BeginOutputReadLine(); process.BeginErrorReadLine();
                var complete = Task.WhenAll(exited.Task, stdout.Task, stderr.Task);
                if (await Task.WhenAny(complete, deadline).ConfigureAwait(false) != complete)
                    return new Result { Error = token.IsCancellationRequested ? "git cancelled" : "git timed out" };
                await complete.ConfigureAwait(false);
                return new Result { Success = process.ExitCode == 0, Output = output.ToString(),
                    Error = process.ExitCode == 0 ? null : "git exit " + process.ExitCode + ": " + error };
            }
            catch (Exception e) { return new Result { Error = "git: " + e.Message }; }
            finally
            {
                deadlineCancellation.Cancel();
                // 有界回收仅在后台执行。
                if (started)
                {
                    TryKill(process);
                    try { process.WaitForExit(250); } catch (InvalidOperationException) { }
                }
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static void TryKill(Process process)
        {
            try { if (!process.HasExited) process.Kill(); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
        }

        private static void Drain(string line, StringBuilder buffer, TaskCompletionSource<bool> eof)
        {
            if (line == null) { eof.TrySetResult(true); return; }
            // 超出上限继续排空管道，仅丢弃显示内容。
            if (buffer.Length < 8192) buffer.AppendLine(line.Substring(0, Math.Min(line.Length, 8192 - buffer.Length)));
        }
        #endregion
    }
}
