using System;
using System.IO;
using System.Threading;

namespace Game.PlayerData
{
    /// <summary>桌面文件后端。失败抛出原异常；禁止以先删目标再 Move 冒充原子替换。</summary>
    internal class ProgressFileIO
    {
        #region 外部方法
        internal virtual byte[] ReadBounded(string path, int maximumBytes)
        {
            EnsureNoLinks(path);
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > maximumBytes) throw new InvalidDataException("文件超过大小限制。");
            var bytes = new byte[checked((int)stream.Length)];
            int position = 0;
            while (position < bytes.Length)
            {
                int read = stream.Read(bytes, position, bytes.Length - position);
                if (read == 0) throw new EndOfStreamException();
                position += read;
            }
            if (stream.ReadByte() != -1) throw new InvalidDataException("读取时文件长度变化。");
            return bytes;
        }

        internal virtual void WriteNewAndFlush(string path, byte[] bytes)
        {
            EnsureNoLinks(path);
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }

        internal virtual void MoveNew(string source, string destination)
        {
            EnsureNoLinks(source); EnsureNoLinks(destination);
            File.Move(source, destination);
        }

        internal virtual void ReplaceWithBackup(string source, string destination, string backup)
        {
            EnsureNoLinks(source); EnsureNoLinks(destination); EnsureNoLinks(backup);
            File.Replace(source, destination, backup);
        }

        internal IDisposable AcquireOwnership(string root)
        {
            EnsureNoLinks(root);
            Directory.CreateDirectory(root);
            string lockPath = Path.Combine(root, ".writer.lock");
            EnsureNoLinks(lockPath);
            // 锁文件始终保留，避免 Unix 删除已锁文件后另一写者创建同名文件形成双所有者。
            return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        internal bool Exists(string path)
        {
            EnsureNoLinks(path);
            try { File.GetAttributes(path); return true; }
            catch (FileNotFoundException) { return false; }
            catch (DirectoryNotFoundException) { return false; }
        }

        internal void RemoveTemporary(string path)
        {
            if (path == null) return;
            // 只用于本次请求创建的临时文件；负载、主索引和备份永不由此清理。
            try { EnsureNoLinks(path); File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        internal void EnsureNoLinks(string path)
        {
            string current = Path.GetFullPath(path);
            while (!string.IsNullOrEmpty(current))
            {
                try
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                        throw new IOException("存储路径不能经过符号链接/重解析点：" + current);
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
                string parent = Path.GetDirectoryName(current);
                if (parent == current) break;
                current = parent;
            }
        }
        #endregion
    }

    /// <summary>取消和进入提交区共享同一把锁；提交区内取消只取消后续等待，不能改写真实提交结果。</summary>
    internal sealed class ProgressCommitGate : IDisposable
    {
        #region 内部参数
        private readonly object _gate = new();
        private readonly CancellationToken _token;
        private readonly CancellationTokenRegistration _registration;
        private bool _cancelled;
        private bool _committing;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private void Cancel()
        {
            lock (_gate) { if (!_committing) _cancelled = true; }
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal ProgressCommitGate(CancellationToken token)
        {
            _token = token;
            _registration = token.Register(Cancel);
        }

        internal void CheckCancellation()
        {
            lock (_gate)
                if (!_committing && (_cancelled || _token.IsCancellationRequested))
                    throw new OperationCanceledException(_token);
        }

        internal void EnterCommit()
        {
            lock (_gate)
            {
                if (_cancelled || _token.IsCancellationRequested) throw new OperationCanceledException(_token);
                _committing = true;
            }
        }

        public void Dispose() => _registration.Dispose();
        #endregion
    }
}
