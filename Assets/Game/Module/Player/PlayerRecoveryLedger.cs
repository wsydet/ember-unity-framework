using System;
using Game.PlayerData;

namespace Game.Player
{
    /// <summary>单次玩家会话的恢复目标。由 PlayerModule 在主线程持有，不能跨会话复用。</summary>
    internal sealed class PlayerRecoveryLedger : IDisposable
    {
        #region 内部参数
        private readonly long _serviceEpoch;
        private long _lastAcceptedRequest;
        private long _lastCommittedRequest;
        private bool _disposed;
        public PlatformerSnapshot EntrySnapshot { get; }
        public PlatformerSnapshot Target { get; private set; }
        public int? TargetSlot { get; private set; }
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        internal sealed class SaveTicket
        {
            internal readonly PlayerRecoveryLedger Owner;
            internal readonly ProgressRequestContext Context;
            internal readonly PlatformerSnapshot Snapshot;
            internal bool Completed;

            internal SaveTicket(PlayerRecoveryLedger owner, ProgressRequestContext context, PlatformerSnapshot snapshot)
            { Owner = owner; Context = context; Snapshot = snapshot; }
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        /// <summary>进入成功时传入实际使用的初始或读档快照，不以槽位查询或当前配置重新生成。</summary>
        internal PlayerRecoveryLedger(PlatformerSnapshot entrySnapshot, long serviceEpoch, int? sourceSlot = null)
        {
            if (entrySnapshot == null) throw new ArgumentNullException(nameof(entrySnapshot));
            if (serviceEpoch <= 0) throw new ArgumentOutOfRangeException(nameof(serviceEpoch));
            if (sourceSlot < 0) throw new ArgumentOutOfRangeException(nameof(sourceSlot));
            EntrySnapshot = Target = entrySnapshot;
            TargetSlot = sourceSlot;
            _serviceEpoch = serviceEpoch;
        }

        /// <summary>保存入口同步采集并提交存储请求后关联，不等待结果，也不改变当前恢复目标。</summary>
        internal SaveTicket Track(ProgressRequestContext request, PlatformerSnapshot acceptedSnapshot)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PlayerRecoveryLedger));
            if (acceptedSnapshot == null) throw new ArgumentNullException(nameof(acceptedSnapshot));
            if (request.ServiceEpoch != _serviceEpoch || !request.SlotId.HasValue || request.SlotId.Value < 0
                || request.RequestId <= _lastAcceptedRequest)
                throw new ArgumentException("保存请求不属于当前服务，或请求顺序/槽位无效。", nameof(request));
            _lastAcceptedRequest = request.RequestId;
            return new SaveTicket(this, request, acceptedSnapshot);
        }

        /// <summary>只接受票据对应的结果；调用方在向外完成保存任务前执行。不重新采集运行状态。</summary>
        internal bool RecordResult(SaveTicket ticket, ProgressRequestContext result, SaveStatus status)
        {
            if (_disposed || ticket == null || !ReferenceEquals(ticket.Owner, this) || ticket.Completed
                || ticket.Context.RequestId != result.RequestId || ticket.Context.ServiceEpoch != result.ServiceEpoch
                || ticket.Context.SlotId != result.SlotId) return false;
            ticket.Completed = true;
            if (status != SaveStatus.Committed || result.RequestId <= _lastCommittedRequest) return false;
            _lastCommittedRequest = result.RequestId;
            Target = ticket.Snapshot;
            TargetSlot = result.SlotId;
            return true;
        }

        /// <summary>会话替换/退出时封闭旧票据，迟到提交不能再改变目标。</summary>
        public void Dispose() => _disposed = true;
        #endregion
    }
}
