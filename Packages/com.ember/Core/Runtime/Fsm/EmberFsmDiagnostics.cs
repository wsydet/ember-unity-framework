using System;
using System.Collections.Generic;

namespace Ember.Core.Fsm
{
    /// <summary>不可变操作快照。SourceNodeId 是声明来源，LeafId 是操作完成后的活动叶子。</summary>
    public sealed class EmberFsmDiagnostic
    {
        #region 内部参数
        public Guid InstanceId { get; }
        public long Sequence { get; }
        public EmberFsmRequestOrigin Origin { get; }
        public string SourceNodeId { get; }
        public string TransitionId { get; }
        public string TargetId { get; }
        public string LeafId { get; }
        public IReadOnlyList<string> ActivePath { get; }
        public EmberFsmResult Result { get; }
        public string Phase { get; }
        public string FaultNodeId { get; }
        public Exception Exception { get; }
        public EmberFsmAttemptRecord Attempt { get; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal EmberFsmDiagnostic(Guid instanceId, long sequence, EmberFsmRequestOrigin origin,
            string source, string transition, string target, string[] path, EmberFsmResult result,
            string phase, string faultNode = null, Exception exception = null, EmberFsmAttemptRecord attempt = null)
        {
            InstanceId = instanceId; Sequence = sequence; Origin = origin; SourceNodeId = source;
            Attempt = attempt;
            TransitionId = transition; TargetId = target; ActivePath = Array.AsReadOnly(path);
            LeafId = path.Length == 0 ? null : path[path.Length - 1];
            Result = result; Phase = phase; FaultNodeId = faultNode; Exception = exception;
        }
        #endregion
    }
}
