using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Ember.Basic;

namespace Ember.Core.Fsm
{
    /// <summary>宿主捕获配置时显式建立的来源关联；按节点保存两侧结构，不替换真实运行描述。</summary>
    public sealed class EmberFsmObservationOrigin
    {
        #region 内部参数
        public EmberFsmGraph SourceGraph { get; }
        public EmberFsmGraphDescription SourceDescription { get; }
        public EmberFsmGraphDescription RuntimeDescription { get; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private EmberFsmObservationOrigin(EmberFsmGraph source, EmberFsmGraphDescription original,
            EmberFsmGraphDescription runtime)
        { SourceGraph = source; SourceDescription = original; RuntimeDescription = runtime; }

        internal static bool SameStructure(EmberFsmGraphDescription left, EmberFsmGraphDescription right, bool identity)
        {
            if (left == null || right == null || !left.IsValid || !right.IsValid
                || left.EntryId != right.EntryId || left.Nodes.Count != right.Nodes.Count) return false;
            var map = right.Nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
            foreach (var node in left.Nodes)
            {
                if (!map.TryGetValue(node.Id, out var other) || node.Definition == null || other.Definition == null
                    || identity && node.Definition != other.Definition
                    || node.Definition.GetType() != other.Definition.GetType()
                    || node.ParentId != other.ParentId || node.DefaultChildId != other.DefaultChildId
                    || !node.RedirectSlots.SequenceEqual(other.RedirectSlots)
                    || node.Targets.Count != other.Targets.Count
                    || node.Targets.Any(p => !other.Targets.TryGetValue(p.Key, out var target) || target != p.Value)
                    || node.Transitions.Count != other.Transitions.Count) return false;
                for (int i = 0; i < node.Transitions.Count; i++)
                {
                    var a = node.Transitions[i]; var b = other.Transitions[i];
                    if (a.Id != b.Id || a.TargetSlot != b.TargetSlot || a.Mode != b.Mode
                        || a.ContextType != b.ContextType || a.StateType != b.StateType) return false;
                    if (a.Sites.Count != b.Sites.Count) return false;
                    for (int j = 0; j < a.Sites.Count; j++)
                    {
                        var x = a.Sites[j]; var y = b.Sites[j];
                        if (x.Id != y.Id || x.Phase != y.Phase || x.Segment != y.Segment || x.Order != y.Order ||
                            x.Continuation != y.Continuation || x.Condition != y.Condition ||
                            x.CommitDescription != y.CommitDescription || x.ContextType != y.ContextType || x.StateType != y.StateType)
                            return false;
                    }
                }
            }
            return true;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>按稳定节点 ID 显式关联源与捕获图。只允许配置副本，不接受结构或声明变化。</summary>
        [HasGC]
        public static EmberFsmObservationOrigin Capture(EmberFsmGraph source, EmberFsmGraph runtime)
        {
            if (source == null || runtime == null) throw new ArgumentNullException();
            var original = source.Inspect(); var captured = runtime.Inspect();
            if (!SameStructure(original, captured, false))
                throw new ArgumentException("Observation origin requires equivalent node structure and declarations.");
            return new EmberFsmObservationOrigin(source, original, captured);
        }
        /// <summary>同时验证原图身份、当前源结构与实际运行结构；不重新评估 Guard。</summary>
        public bool Matches(EmberFsmGraph source, EmberFsmGraphDescription current, EmberFsmGraphDescription runtime)
            => SourceGraph != null && SourceGraph == source
                && SameStructure(SourceDescription, current, true)
                && SameStructure(RuntimeDescription, runtime, true);
        #endregion
    }

    /// <summary>按需捕获的只读观察数据；不持有运行器，不驱动或评估任何状态。</summary>
    public sealed class EmberFsmObservation
    {
        #region 内部参数
        public Guid InstanceId { get; private set; }
        public EmberFsmStatus Status { get; private set; }
        public EmberFsmGraphDescription Description { get; private set; }
        public IReadOnlyList<string> ActivePath { get; private set; }
        public IReadOnlyDictionary<string,double> Elapsed { get; private set; }
        public IReadOnlyList<EmberFsmDiagnostic> History { get; private set; }
        public EmberFsmDiagnostic LastOperation { get; private set; }
        public EmberFsmDiagnostic LastFault { get; private set; }
        public bool DiagnosticsEnabled { get; private set; }
        public EmberFsmObservationOrigin Origin { get; private set; }
        public IReadOnlyList<EmberFsmRuleDescription> Rules { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [HasGC]
        public static EmberFsmObservation Capture<T>(EmberFsm<T> fsm, EmberFsmObservationOrigin origin = null) where T:class
        {
            if(fsm==null)return null;
            var times=new Dictionary<string,double>(StringComparer.Ordinal);
            foreach(var n in fsm.Description.Nodes) times.Add(n.Id,fsm.GetElapsedTime(n.Id));
            return new EmberFsmObservation { InstanceId=fsm.InstanceId,Status=fsm.Status,
                Description=fsm.Description,Origin=origin,Rules=fsm.Rules,ActivePath=fsm.GetActivePath(),
                Elapsed=new ReadOnlyDictionary<string,double>(times),History=fsm.GetHistory(),
                LastOperation=fsm.LastOperation,LastFault=fsm.LastFault,DiagnosticsEnabled=fsm.DiagnosticsEnabled };
        }
        public bool MatchesSource(EmberFsmGraph source, EmberFsmGraphDescription current)
            => Origin != null ? Origin.Matches(source, current, Description)
                : EmberFsmObservationOrigin.SameStructure(current, Description, true);
        #endregion
    }
}
