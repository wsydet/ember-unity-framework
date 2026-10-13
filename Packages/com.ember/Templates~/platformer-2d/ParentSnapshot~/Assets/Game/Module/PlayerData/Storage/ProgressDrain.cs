using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Game.PlayerData
{
    public enum ProgressOperationKind { Save, Read }
    public enum DrainStatus { Drained, Failed, WaitCancelled, InvalidTicket }

    /// <summary>用于退出报告的终态，不持有解码后的业务进度。</summary>
    public sealed class ProgressOperationResult
    {
        #region 内部参数
        public ProgressRequestContext Context { get; }
        public ProgressOperationKind Kind { get; }
        public SaveStatus? SaveStatus { get; }
        public ReadStatus? ReadStatus { get; }
        public string Error { get; }
        public long? Revision { get; }
        public string CommitId { get; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal ProgressOperationResult(SaveResult result)
        {
            Context = result.Context; Kind = ProgressOperationKind.Save; SaveStatus = result.Status;
            Error = result.Error; Revision = result.Revision; CommitId = result.CommitId;
        }

        internal ProgressOperationResult(ProgressRequestContext context, ReadStatus status, string error, SlotMetadata slot)
        {
            Context = context; Kind = ProgressOperationKind.Read; ReadStatus = status; Error = error;
            Revision = slot?.Revision; CommitId = slot?.CommitId;
        }
        #endregion
    }

    /// <summary>关闭入口时的固定水位与结果集合，只能由创建它的服务使用。</summary>
    public sealed class DrainTicket
    {
        #region 内部参数
        public ProgressRequestContext Context { get; }
        public long Watermark { get; }
        internal Task<ProgressOperationResult>[] Operations { get; }
        internal TaskCompletionSource<bool> Invalidated { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal Task<ProgressOperationResult[]> Completion { get; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal DrainTicket(ProgressRequestContext context, long watermark, Task<ProgressOperationResult>[] operations)
        {
            Context = context; Watermark = watermark; Operations = operations;
            Completion = Task.WhenAll(operations);
        }
        #endregion
    }

    public sealed class DrainResult
    {
        #region 内部参数
        public ProgressRequestContext Context { get; }
        public DrainStatus Status { get; }
        public long Watermark { get; }
        public IReadOnlyList<ProgressOperationResult> Operations { get; }
        public IReadOnlyList<ProgressOperationResult> FailedWrites { get; }
        public IReadOnlyList<long> PendingRequestIds { get; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal DrainResult(ProgressRequestContext context, DrainStatus status, long watermark,
            IEnumerable<ProgressOperationResult> operations, IEnumerable<long> pending)
        {
            Context = context; Status = status; Watermark = watermark;
            var ordered = operations.OrderBy(item => item.Context.RequestId).ToList();
            Operations = new ReadOnlyCollection<ProgressOperationResult>(ordered);
            FailedWrites = new ReadOnlyCollection<ProgressOperationResult>(ordered.Where(item =>
                item.Kind == ProgressOperationKind.Save && item.SaveStatus != Game.PlayerData.SaveStatus.Committed).ToList());
            PendingRequestIds = new ReadOnlyCollection<long>(pending.OrderBy(id => id).ToList());
        }
        #endregion
    }
}
