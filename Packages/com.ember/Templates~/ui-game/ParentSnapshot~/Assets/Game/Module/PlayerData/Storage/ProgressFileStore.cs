using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Game.PlayerData
{
    /// <summary>
    /// 模块内部的同步字节后端。调用方串行调度 I/O；本类不启动任务、不调用 codec、不加载游戏会话。
    /// 目录锁从 Prepare 成功一直持有至 Dispose，禁止旧 I/O 尚未结束就取得第二个文件所有者。
    /// </summary>
    internal sealed class ProgressFileStore : IDisposable
    {
        #region 内部参数
        private readonly object _gate = new();
        private readonly ProgressStoreOptions _options;
        private readonly ProgressFileIO _files;
        private readonly string _indexPath;
        private readonly string _backupPath;
        private IDisposable _ownership;
        private ProgressIndex _index = new();
        private byte[] _indexBytes;
        private volatile SlotCatalog _catalog;
        private bool _disposed;
        internal SlotCatalog Catalog => _catalog;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private void Publish(SaveServiceState state)
        {
            var slots = new List<SlotMetadata>();
            foreach (int id in _options.SlotIds)
                slots.Add(_index.Entries.TryGetValue(id, out var entry) ? entry.Metadata : new SlotMetadata(id));
            _catalog = new SlotCatalog(state, _index.Revision, _index.LatestSlot, slots);
        }

        private SaveResult Committed(ProgressRequestContext context, ProgressIndex candidate, byte[] bytes)
        {
            _index = candidate;
            _indexBytes = bytes;
            Publish(SaveServiceState.Ready);
            return new SaveResult(context, SaveStatus.Committed, revision: candidate.Revision, commitId: candidate.CommitId);
        }

        private SaveResult ResolveCommit(ProgressRequestContext context, ProgressIndex candidate, byte[] bytes, Exception error)
        {
            try
            {
                var actual = _files.ReadBounded(_indexPath, _options.MaxIndexBytes);
                if (actual.SequenceEqual(bytes)) return Committed(context, candidate, bytes);
                if (_indexBytes != null && actual.SequenceEqual(_indexBytes))
                    return new SaveResult(context, SaveStatus.WriteFailed, error.ToString());
            }
            catch (FileNotFoundException)
            {
                if (_indexBytes == null) return new SaveResult(context, SaveStatus.WriteFailed, error.ToString());
            }
            catch (Exception verificationError)
            {
                error = new AggregateException("索引提交异常且回读无法确认。", error, verificationError);
            }
            // 不发布候选，不自动重试，不允许后续写覆盖未知磁盘状态。保留所有负载和备份待核对。
            Publish(SaveServiceState.Faulted);
            return new SaveResult(context, SaveStatus.CommitUncertain, error.ToString(), commitId: candidate.CommitId);
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal ProgressFileStore(ProgressStoreOptions options, ProgressFileIO files = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _files = files ?? new ProgressFileIO();
            _indexPath = Path.Combine(options.RootDirectory, "index.json");
            _backupPath = Path.Combine(options.RootDirectory, "index.json.bak");
            Publish(SaveServiceState.Uninitialized);
        }

        internal PrepareResult Prepare(ProgressRequestContext context, CancellationToken cancellation = default)
        {
            lock (_gate)
            {
                if (_disposed) return new PrepareResult(context, PrepareStatus.Stopped);
                if (_catalog.State == SaveServiceState.Ready) return new PrepareResult(context, PrepareStatus.Ready);
                // Faulted 实例不可自行重新准备；先释放旧文件所有权，再由新服务显式核对。
                if (_catalog.State == SaveServiceState.Faulted)
                    return new PrepareResult(context, PrepareStatus.IoFailed, "存储已故障，请释放本实例后重新核对。");
                Publish(SaveServiceState.Preparing);
                PrepareStatus status;
                string error;
                try
                {
                    cancellation.ThrowIfCancellationRequested();
                    _ownership = _files.AcquireOwnership(_options.RootDirectory);
                    byte[] bytes;
                    try { bytes = _files.ReadBounded(_indexPath, _options.MaxIndexBytes); }
                    catch (FileNotFoundException)
                    {
                        if (_files.Exists(_backupPath)) throw new InvalidDataException("主索引缺失但备份存在，需要显式恢复，不能创建空库。");
                        bytes = null;
                    }
                    ProgressIndex loaded = bytes == null ? new ProgressIndex() : ProgressFileFormat.DecodeIndex(bytes, _options);
                    cancellation.ThrowIfCancellationRequested();
                    _index = loaded;
                    _indexBytes = bytes;
                    Publish(SaveServiceState.Ready);
                    return new PrepareResult(context, PrepareStatus.Ready);
                }
                catch (OperationCanceledException exception) { status = PrepareStatus.Cancelled; error = exception.Message; }
                catch (ProgressConfigurationException exception) { status = PrepareStatus.ConfigInvalid; error = exception.Message; }
                catch (UnsupportedProgressFormatException exception) { status = PrepareStatus.StorageVersionUnsupported; error = exception.Message; }
                catch (InvalidDataException exception) { status = PrepareStatus.IndexCorrupt; error = exception.Message; }
                catch (IOException exception) { status = PrepareStatus.IoFailed; error = exception.ToString(); }
                catch (UnauthorizedAccessException exception) { status = PrepareStatus.IoFailed; error = exception.ToString(); }
                catch (Exception exception) { status = PrepareStatus.IndexCorrupt; error = exception.ToString(); }
                _ownership?.Dispose();
                _ownership = null;
                Publish(SaveServiceState.Faulted);
                return new PrepareResult(context, status, error);
            }
        }

        internal SaveResult Save(ProgressRequestContext context, FrozenProgress snapshot, CancellationToken cancellation = default)
        {
            lock (_gate)
            {
                if (_catalog.State != SaveServiceState.Ready) return new SaveResult(context, SaveStatus.NotReady);
                if (!context.SlotId.HasValue || !_options.ContainsSlot(context.SlotId.Value))
                    return new SaveResult(context, SaveStatus.InvalidSlot);
                if (snapshot == null) return new SaveResult(context, SaveStatus.EncodeFailed);
                if (snapshot.DataTypeId != _options.DataTypeId) return new SaveResult(context, SaveStatus.WrongDataType);
                if (snapshot.Payload.Length > _options.MaxPayloadBytes) return new SaveResult(context, SaveStatus.TooLarge);
                using var commitGate = new ProgressCommitGate(cancellation);
                string id = Guid.NewGuid().ToString("N");
                string payloadName = id + ".progress";
                string payloadPath = Path.Combine(_options.RootDirectory, payloadName);
                string payloadTemp = payloadPath + ".tmp";
                string indexTemp = Path.Combine(_options.RootDirectory, id + ".index.tmp");
                ProgressIndex candidate = null;
                byte[] candidateBytes = null;
                bool commitAttempted = false;
                try
                {
                    commitGate.CheckCancellation();
                    byte[] payload = ProgressFileFormat.EncodePayload(snapshot);
                    _files.WriteNewAndFlush(payloadTemp, payload);
                    commitGate.CheckCancellation();
                    _files.MoveNew(payloadTemp, payloadPath);
                    commitGate.CheckCancellation();
                    // 必须在执行时从最新索引构造，不能在接受请求/冻结快照时构造。
                    long revision = checked(_index.Revision + 1);
                    var metadata = new SlotMetadata(context.SlotId.Value, snapshot.Summary, DateTime.UtcNow,
                        snapshot.DataVersion, revision, id);
                    var entries = new Dictionary<int, ProgressEntry>(_index.Entries)
                    {
                        [metadata.SlotId] = new ProgressEntry(metadata, payloadName, payload.Length, ProgressFileFormat.Hash(payload))
                    };
                    candidate = new ProgressIndex(revision, metadata.SlotId, id, entries);
                    candidateBytes = ProgressFileFormat.EncodeIndex(candidate, _options.DataTypeId);
                    if (candidateBytes.Length > _options.MaxIndexBytes) return new SaveResult(context, SaveStatus.TooLarge, "索引超过大小限制。");
                    _files.WriteNewAndFlush(indexTemp, candidateBytes);
                    commitGate.EnterCommit();
                    commitAttempted = true;
                    if (_indexBytes == null) _files.MoveNew(indexTemp, _indexPath);
                    else _files.ReplaceWithBackup(indexTemp, _indexPath, _backupPath);
                    return Committed(context, candidate, candidateBytes);
                }
                catch (Exception exception) when (commitAttempted)
                {
                    return ResolveCommit(context, candidate, candidateBytes, exception);
                }
                catch (OperationCanceledException) { return new SaveResult(context, SaveStatus.Cancelled); }
                catch (Exception exception) { return new SaveResult(context, SaveStatus.WriteFailed, exception.ToString()); }
                finally
                {
                    _files.RemoveTemporary(payloadTemp);
                    _files.RemoveTemporary(indexTemp);
                }
            }
        }

        internal StoredPayload Read(ProgressRequestContext context, CancellationToken cancellation = default)
        {
            lock (_gate)
            {
                if (_catalog.State != SaveServiceState.Ready) return new StoredPayload(context, ReadStatus.NotReady);
                if (!context.SlotId.HasValue || !_options.ContainsSlot(context.SlotId.Value))
                    return new StoredPayload(context, ReadStatus.InvalidSlot);
                if (cancellation.IsCancellationRequested) return new StoredPayload(context, ReadStatus.Cancelled);
                if (!_index.Entries.TryGetValue(context.SlotId.Value, out var entry))
                    return new StoredPayload(context, ReadStatus.NotFound);
                try
                {
                    // FileName 已经通过严格的 GUID 文件名校验；所有路径还会检查符号链接/重解析点。
                    string path = Path.Combine(_options.RootDirectory, entry.FileName);
                    byte[] bytes = _files.ReadBounded(path, _options.MaxPayloadBytes + ProgressFileFormat.MAX_ENVELOPE_OVERHEAD);
                    cancellation.ThrowIfCancellationRequested();
                    if (bytes.Length != entry.FileLength || ProgressFileFormat.Hash(bytes) != entry.Sha256)
                        return new StoredPayload(context, ReadStatus.IntegrityFailed, error: "负载长度或 SHA-256 不匹配。");
                    byte[] payload = ProgressFileFormat.DecodePayload(bytes, _options, entry.Metadata.DataVersion.Value);
                    cancellation.ThrowIfCancellationRequested();
                    return new StoredPayload(context, ReadStatus.Loaded, payload, entry.Metadata);
                }
                catch (OperationCanceledException) { return new StoredPayload(context, ReadStatus.Cancelled); }
                catch (UnsupportedProgressFormatException exception)
                { return new StoredPayload(context, ReadStatus.StorageVersionUnsupported, error: exception.Message); }
                catch (Exception exception) when (exception is InvalidDataException || exception is FileNotFoundException
                    || exception is EndOfStreamException || exception is System.Text.DecoderFallbackException)
                { return new StoredPayload(context, ReadStatus.IntegrityFailed, error: exception.ToString()); }
                catch (Exception exception) { return new StoredPayload(context, ReadStatus.ReadFailed, error: exception.ToString()); }
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed) return;
                _ownership?.Dispose();
                _ownership = null;
                _disposed = true;
                Publish(SaveServiceState.Stopped);
            }
        }
        #endregion
    }
}
