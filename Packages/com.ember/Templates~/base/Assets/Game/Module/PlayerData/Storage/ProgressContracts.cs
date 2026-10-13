using System;
using System.Threading.Tasks;

namespace Game.PlayerData
{
    public enum SaveServiceState { Uninitialized, Preparing, Ready, Faulted, Draining, Stopped }
    public enum PrepareStatus { Ready, ConfigInvalid, IndexCorrupt, StorageVersionUnsupported, IoFailed, Cancelled, Stopped, NotInitialized }
    public enum SaveStatus
    {
        Committed, InvalidSlot, WrongDataType, EncodeFailed, TooLarge, NotReady,
        QueueFull, Draining, WriteFailed, Cancelled, CommitUncertain
    }
    public enum ReadStatus
    {
        Loaded, NotFound, InvalidSlot, WrongDataType, IntegrityFailed, StorageVersionUnsupported,
        DataVersionUnsupported, DecodeFailed, ReadFailed, NotReady, QueueFull, Draining, Cancelled
    }
    public enum DecodeStatus { Decoded, DataVersionUnsupported, DecodeFailed }

    /// <summary>每个请求的关联信息；准备等操作的 SlotId 为 null。ID 由上层服务分配。</summary>
    public readonly struct ProgressRequestContext
    {
        #region 内部参数
        public long RequestId { get; }
        public int? SlotId { get; }
        public long ServiceEpoch { get; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public ProgressRequestContext(long requestId, int? slotId, long serviceEpoch)
        {
            if (requestId <= 0 || serviceEpoch <= 0) throw new ArgumentOutOfRangeException(nameof(requestId));
            RequestId = requestId;
            SlotId = slotId;
            ServiceEpoch = serviceEpoch;
        }
        #endregion
    }

    public sealed class SaveOperation<TResult>
    {
        #region 内部参数
        public ProgressRequestContext Context { get; }
        public Task<TResult> Completion { get; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal SaveOperation(ProgressRequestContext context, Task<TResult> completion)
        {
            Context = context;
            Completion = completion ?? throw new ArgumentNullException(nameof(completion));
        }
        #endregion
    }

    public sealed class PrepareResult
    {
        #region 内部参数
        public ProgressRequestContext Context { get; }
        public PrepareStatus Status { get; }
        public string Error { get; }
        public bool Succeeded => Status == PrepareStatus.Ready;
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal PrepareResult(ProgressRequestContext context, PrepareStatus status, string error = null)
        { Context = context; Status = status; Error = error; }
        #endregion
    }

    public sealed class SaveResult
    {
        #region 内部参数
        public ProgressRequestContext Context { get; }
        public SaveStatus Status { get; }
        public string Error { get; }
        public long? Revision { get; }
        public string CommitId { get; }
        public bool Succeeded => Status == SaveStatus.Committed;
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal SaveResult(ProgressRequestContext context, SaveStatus status, string error = null,
            long? revision = null, string commitId = null)
        { Context = context; Status = status; Error = error; Revision = revision; CommitId = commitId; }
        #endregion
    }

    public sealed class ReadResult<TProgress>
    {
        #region 内部参数
        public ProgressRequestContext Context { get; }
        public ReadStatus Status { get; }
        public string Error { get; }
        public TProgress Progress { get; }
        public SlotMetadata Slot { get; }
        public bool Succeeded => Status == ReadStatus.Loaded;
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal ReadResult(ProgressRequestContext context, ReadStatus status, string error = null,
            TProgress progress = default, SlotMetadata slot = null)
        { Context = context; Status = status; Error = error; Progress = progress; Slot = slot; }
        #endregion
    }

    /// <summary>项目显式提供完整进度的 codec。调用线程执行，不由后台反射或扫描创建。</summary>
    public interface IProgressCodec<TProgress>
    {
        string DataTypeId { get; }
        EncodedProgress Encode(TProgress snapshot);
        DecodeResult<TProgress> Decode(ReadOnlyMemory<byte> payload, int dataVersion);
    }

    /// <summary>codec 输出可由调用方持有；接受保存时仍须复制，不能把此数组直接交给后台。</summary>
    public sealed class EncodedProgress
    {
        #region 内部参数
        public byte[] Payload { get; }
        public int DataVersion { get; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public EncodedProgress(byte[] payload, int dataVersion)
        { Payload = payload; DataVersion = dataVersion; }
        #endregion
    }

    public sealed class DecodeResult<TProgress>
    {
        #region 内部参数
        public DecodeStatus Status { get; }
        public TProgress Progress { get; }
        public string Error { get; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public DecodeResult(DecodeStatus status, TProgress progress = default, string error = null)
        { Status = status; Progress = progress; Error = error; }
        #endregion
    }
}
