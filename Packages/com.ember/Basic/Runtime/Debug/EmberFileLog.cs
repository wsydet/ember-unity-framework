// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Ember.Basic
{
    /// <summary>
    /// 文件日志持久化 —— 异步将 EmberDebug 日志写入 .log 文件。
    ///
    /// 特性：
    /// - <b>异步非阻塞</b>：入队仅 ConcurrentQueue.Enqueue，零 I/O 等待
    /// - <b>批量写入</b>：攒够一批或循环间隔后一次性写入磁盘
    /// - <b>日志轮转</b>：单个文件达到最大大小后自动切换到下一个，按环形覆盖
    /// - <b>过期清理</b>：启动时删除超过保留天数的旧日志
    /// - <b>纯文本格式</b>：不含 Rich Text 标签，可用任何文本工具查看和分析
    ///
    /// 用法：
    /// <code>
    /// // 配置（由 EmberDebug 在加载配置时自动调用）
    /// EmberFileLog.ApplyConfig(configSO);
    ///
    /// // 启动 / 停止（由 GameLauncher 调用）
    /// EmberFileLog.Start();
    /// EmberFileLog.Stop();
    ///
    /// // 上传（业务层订阅，文件关闭/轮转时触发）
    /// EmberFileLog.OnLogFileReady += path => UploadToServer(path);
    /// </code>
    /// </summary>
    public static class EmberFileLog
    {
        #region 内部参数

        // 配置（从 SO 读入）
        private static bool _enableFileLog = true;
        private static string _logDirectory = "";
        private static int _maxFileSizeMB = 10;
        private static int _maxFileCount = 5;
        private static int _retentionDays = 30;

        // 运行时状态
        private static ConcurrentQueue<string> _queue;
        private static Thread _writeThread;
        private static CancellationTokenSource _cts;
        private static StreamWriter _writer;
        private static FileStream _fileStream;
        private static string _currentFilePath;
        private static long _currentFileSize;
        private static int _currentFileIndex;
        private static volatile bool _isRunning;
        private static readonly object _fileLock = new();

        // 常量
        private const string LOG_TAG = nameof(EmberFileLog);
        private const string LOG_FILE_PREFIX = "ember";
        private const string LOG_FILE_EXT = ".log";
        private const int FLUSH_BATCH_SIZE = 100;
        private const int WRITE_LOOP_SLEEP_MS = 100;
        private const long BYTES_PER_MB = 1024 * 1024;

        #endregion

        // ============================================================

        #region 外部方法

        /// <summary>日志文件关闭时触发（轮转或停止时）。参数为文件完整路径。</summary>
        public static event Action<string> OnLogFileReady;

        /// <summary>
        /// 从 SO 应用文件日志配置。由 EmberDebug 在加载配置 LoadConfig 时自动调用。
        /// </summary>
        [NoGC]
        public static void ApplyConfig(EmberDebugConfigSO config)
        {
            if (config == null) return;

            _enableFileLog = config.enableFileLog;
            _logDirectory = config.logDirectory;
            _maxFileSizeMB = Mathf.Max(1, config.maxFileSizeMB);
            _maxFileCount = Mathf.Max(1, config.maxFileCount);
            _retentionDays = Mathf.Max(1, config.retentionDays);
        }

        /// <summary>
        /// 启动文件日志系统。应在游戏启动早期调用（如 GameLauncher.Awake）。
        /// 如果 enableFileLog 为 false，此方法无操作。
        /// </summary>
        [HasGC]
        public static void Start()
        {
            if (!_enableFileLog) return;
            if (_isRunning) return;

#if UNITY_EDITOR
            // Editor 下自动放宽限制（防磁盘写满，但也避免开发时频繁轮转）
            _maxFileSizeMB = Mathf.Max(_maxFileSizeMB, 50);
            _maxFileCount = Mathf.Max(_maxFileCount, 10);
#endif

            _queue = new ConcurrentQueue<string>();
            _cts = new CancellationTokenSource();

            // 解析日志目录
            string dir = ResolveLogDirectory();
            try
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            catch (Exception ex)
            {
                EmberDebug.LogError(LOG_TAG, $"Failed to create log directory: {dir}. {ex.Message}");
                return;
            }

            // 清理过期日志
            CleanupExpiredLogs(dir);

            // 找到下一个可用文件索引（优先追加到最近未满的文件）
            _currentFileIndex = FindNextFileIndex(dir);

            // 打开文件
            if (!OpenFile(dir, _currentFileIndex))
                return;

            // 启动后台写线程
            _writeThread = new Thread(WriteLoop)
            {
                Name = "EmberFileLog",
                IsBackground = true,
                Priority = System.Threading.ThreadPriority.BelowNormal
            };
            _isRunning = true;
            _writeThread.Start();

            EmberDebug.LogInit(LOG_TAG, $"File logging started. Dir: {dir}, Max: {_maxFileSizeMB}MB × {_maxFileCount}, Retention: {_retentionDays}d");
        }

        /// <summary>
        /// 停止文件日志系统。会等待所有待写条目刷入磁盘后关闭文件。
        /// 应在游戏退出时调用（如 GameLauncher.OnDestroy）。
        /// 线程安全：可被多次调用，第二次及之后为无操作。
        /// </summary>
        [HasGC]
        public static void Stop()
        {
            if (!_isRunning) return;

            EmberDebug.LogShutdown(LOG_TAG, "Stopping file logging...");
            _isRunning = false;

            // 取消写线程
            _cts?.Cancel();

            if (_writeThread != null)
            {
                if (_writeThread.IsAlive)
                {
                    if (!_writeThread.Join(5000))
                    {
                        EmberDebug.LogWarning(LOG_TAG, "Write thread did not exit in time (5s), forcing close.");
                    }
                }
                _writeThread = null;
            }

            // 消费队列中剩余的所有条目
            DrainRemaining();

            // 最终关闭文件
            CloseFile();

            _cts?.Dispose();
            _cts = null;
            _queue = null;

            EmberDebug.LogCleanup(LOG_TAG, "File logging stopped.");
        }

        /// <summary>
        /// 入队一条纯文本日志行。由 EmberDebug 在每次输出日志时调用。
        /// 仅 ConcurrentQueue.Enqueue，零阻塞，不触发 I/O。
        /// </summary>
        [NoGC]
        public static void Enqueue(string plainTextLine)
        {
            if (!_isRunning) return;
            _queue?.Enqueue(plainTextLine);
        }

        /// <summary>
        /// 当前是否正在运行文件日志。
        /// </summary>
        public static bool IsRunning => _isRunning;

        #endregion

        // ============================================================

        #region 内部方法

        /// <summary>
        /// 后台写循环。批量消费队列，攒够 FLUSH_BATCH_SIZE 条或等待一轮后写入。
        /// </summary>
        private static void WriteLoop()
        {
            var sb = new StringBuilder();

            while (!_cts.Token.IsCancellationRequested)
            {
                int count = 0;
                sb.Clear();

                // 批量消费队列
                while (count < FLUSH_BATCH_SIZE && _queue.TryDequeue(out string line))
                {
                    sb.AppendLine(line);
                    count++;
                }

                if (count > 0)
                {
                    AppendToFile(sb.ToString());
                }

                Thread.Sleep(WRITE_LOOP_SLEEP_MS);
            }
        }

        /// <summary>
        /// 消费队列中剩余的所有条目（Stop 时在主线程调用）。
        /// </summary>
        [HasGC]
        private static void DrainRemaining()
        {
            if (_queue == null) return;

            var sb = new StringBuilder();
            int count = 0;

            while (_queue.TryDequeue(out string line))
            {
                sb.AppendLine(line);
                count++;
            }

            if (count > 0)
            {
                AppendToFile(sb.ToString());
            }
        }

        /// <summary>
        /// 线程安全地将内容追加到当前文件。自动检查轮转条件。
        /// </summary>
        [HasGC]
        private static void AppendToFile(string content)
        {
            lock (_fileLock)
            {
                if (_writer == null) return;

                int byteCount = Encoding.UTF8.GetByteCount(content);

                // 如果本次写入会超出文件大小上限，先轮转
                if (_currentFileSize + byteCount > (long)_maxFileSizeMB * BYTES_PER_MB
                    && _currentFileSize > 0)
                {
                    RotateFile();
                }

                try
                {
                    _writer.Write(content);
                    _writer.Flush();
                    _currentFileSize += byteCount;
                }
                catch (Exception ex)
                {
                    EmberDebug.LogError(LOG_TAG, $"Failed to write log file: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 轮转日志文件：关闭当前文件，切换到下一个索引（环形覆盖）。
        /// </summary>
        [HasGC]
        private static void RotateFile()
        {
            CloseFile();

            _currentFileIndex = (_currentFileIndex + 1) % _maxFileCount;
            string dir = ResolveLogDirectory();

            // 删除即将覆盖的旧文件
            string nextPath = GetFilePath(dir, _currentFileIndex);
            try
            {
                if (File.Exists(nextPath))
                    File.Delete(nextPath);
            }
            catch (Exception ex)
            {
                EmberDebug.LogWarning(LOG_TAG, $"Failed to delete old log file: {nextPath}. {ex.Message}");
            }

            // 打开新文件
            if (!OpenFile(dir, _currentFileIndex))
            {
                EmberDebug.LogError(LOG_TAG, "Log file rotation failed. File logging is now disabled.");
            }
            else
            {
                EmberDebug.LogEvent(LOG_TAG, $"Rotated to: {Path.GetFileName(_currentFilePath)}");
            }
        }

        /// <summary>
        /// 打开指定索引的日志文件（Append 模式，UTF-8）。
        /// </summary>
        [HasGC]
        private static bool OpenFile(string dir, int index)
        {
            string path = GetFilePath(dir, index);

            try
            {
                _fileStream = new FileStream(path, FileMode.Append, FileAccess.Write,
                    FileShare.Read, 4096, FileOptions.WriteThrough);
                _writer = new StreamWriter(_fileStream, Encoding.UTF8, 4096) { AutoFlush = false };
                _currentFilePath = path;
                _currentFileSize = _fileStream.Length;

                // 新文件写入头部标记
                if (_currentFileSize == 0)
                {
                    string header = $"=== Ember Log [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ==={Environment.NewLine}";
                    _writer.Write(header);
                    _writer.Flush();
                    _currentFileSize = _fileStream.Length;
                }

                return true;
            }
            catch (Exception ex)
            {
                EmberDebug.LogError(LOG_TAG, $"Failed to open log file: {path}. {ex.Message}");
                _writer?.Dispose();
                _writer = null;
                _fileStream?.Dispose();
                _fileStream = null;
                return false;
            }
        }

        /// <summary>
        /// 安全关闭当前文件，写入尾部标记，触发 OnLogFileReady 事件。
        /// </summary>
        [HasGC]
        private static void CloseFile()
        {
            string closedPath = _currentFilePath;

            // 写入尾部标记
            if (_writer != null && !string.IsNullOrEmpty(closedPath))
            {
                try
                {
                    _writer.WriteLine();
                    _writer.WriteLine($"=== Ember Log End [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ===");
                    _writer.Flush();
                }
                catch { /* 忽略关闭时的写入错误 */ }
            }

            // 关闭 writer
            if (_writer != null)
            {
                try { _writer.Close(); _writer.Dispose(); }
                catch { /* 忽略关闭时的异常 */ }
                _writer = null;
            }

            // 关闭 filestream
            if (_fileStream != null)
            {
                try { _fileStream.Close(); _fileStream.Dispose(); }
                catch { /* 忽略关闭时的异常 */ }
                _fileStream = null;
            }

            _currentFilePath = null;
            _currentFileSize = 0;

            // 通知上传
            if (!string.IsNullOrEmpty(closedPath))
            {
                OnLogFileReady?.Invoke(closedPath);
            }
        }

        /// <summary>
        /// 解析日志目录。SO 中 logDirectory 为空则自动选择：
        /// Editor 下为 {项目}/Logs/ember/，Build 下为 persistentDataPath/logs/。
        /// </summary>
        [NoGC]
        private static string ResolveLogDirectory()
        {
            if (!string.IsNullOrEmpty(_logDirectory))
                return _logDirectory;

#if UNITY_EDITOR
            return Path.Combine(Application.dataPath, "..", "Logs", "ember");
#else
            return Path.Combine(Application.persistentDataPath, "logs");
#endif
        }

        /// <summary>
        /// 获取指定索引的完整文件路径。
        /// </summary>
        [NoGC]
        private static string GetFilePath(string dir, int index)
        {
            return Path.Combine(dir, $"{LOG_FILE_PREFIX}_{index}{LOG_FILE_EXT}");
        }

        /// <summary>
        /// 找到目录中可写入的下一个文件索引。
        /// 优先追加到已存在且未满的最新文件；若无可用文件则从索引 0 开始。
        /// </summary>
        [HasGC]
        private static int FindNextFileIndex(string dir)
        {
            long maxBytes = (long)_maxFileSizeMB * BYTES_PER_MB;
            int bestIndex = 0;
            DateTime bestTime = DateTime.MinValue;

            for (int i = 0; i < _maxFileCount; i++)
            {
                string path = GetFilePath(dir, i);
                if (File.Exists(path))
                {
                    var info = new FileInfo(path);
                    if (info.Length < maxBytes && info.LastWriteTime > bestTime)
                    {
                        bestIndex = i;
                        bestTime = info.LastWriteTime;
                    }
                }
            }

            return bestIndex;
        }

        /// <summary>
        /// 清理超过保留天数的旧日志文件。
        /// </summary>
        [HasGC]
        private static void CleanupExpiredLogs(string dir)
        {
            if (!Directory.Exists(dir)) return;

            DateTime cutoff = DateTime.Now.AddDays(-_retentionDays);

            for (int i = 0; i < _maxFileCount; i++)
            {
                string path = GetFilePath(dir, i);
                try
                {
                    if (File.Exists(path))
                    {
                        var info = new FileInfo(path);
                        if (info.LastWriteTime < cutoff)
                        {
                            File.Delete(path);
                            EmberDebug.LogCleanup(LOG_TAG, $"Deleted expired log: {Path.GetFileName(path)}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    EmberDebug.LogWarning(LOG_TAG, $"Failed to clean up log file: {path}. {ex.Message}");
                }
            }
        }

        #endregion
    }
}
