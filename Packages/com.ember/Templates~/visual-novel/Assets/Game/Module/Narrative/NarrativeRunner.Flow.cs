using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Narrative
{
    [Serializable]
    public sealed class NovelCallFrame
    {
        #region 编辑器面板参数
        public string CallId, FlowId;
        public List<NovelVariable> Variables = new();
        public List<NovelReturnAddress> Returns = new();
        #endregion
    }

    [Serializable]
    public sealed class NovelReturnAddress
    {
        #region 编辑器面板参数
        public string Result, NodeId;
        #endregion
    }

    public sealed partial class NarrativeRunner
    {
        #region 内部参数
        public const int MAX_FLOW_DEPTH = 16;
        private sealed class FlowFrame
        {
            internal NovelNode Call, Flow;
            internal Dictionary<string, NovelValue> Variables;
        }
        private readonly List<FlowFrame> _frames = new();
        private readonly Dictionary<string, NovelValue> _emptyFlow = new(StringComparer.Ordinal);
        private Dictionary<string, NovelValue> FlowValues => _frames.Count == 0 ? _emptyFlow : _frames[_frames.Count - 1].Variables;
        private string ActiveScope => _frames.Count == 0 ? "" : _frames[_frames.Count - 1].Flow.Id;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private Dictionary<string, NovelValue> Values(NovelVariableScope scope)
            => scope == NovelVariableScope.Flow ? FlowValues : scope == NovelVariableScope.Global ? _globals : _variables;

        private void CallFlow()
        {
            if (_frames.Count >= MAX_FLOW_DEPTH) { Fault("FlowDepth", "流程嵌套超过上限 16"); return; }
            if (!_nodes.TryGetValue(_node.LinkId ?? "", out var flow) || flow.Kind != NovelNodeKind.FlowStart)
            { Fault("MissingFlow", "调用流程不存在：" + _node.LinkId); return; }
            if (_frames.Any(f => f.Flow.Id == flow.Id)) { Fault("RecursiveFlow", "禁止递归调用：" + flow.Id); return; }
            var frame = new FlowFrame { Call = _node, Flow = flow, Variables = new(StringComparer.Ordinal) };
            foreach (var v in flow.FlowVariables) frame.Variables.Add(v.Id, v.Value);
            _frames.Add(frame);
            Enter(flow.Id);
        }

        private void ReturnFlow()
        {
            if (_frames.Count == 0) { Fault("UnexpectedReturn", "主线不能执行流程返回"); return; }
            var frame = _frames[_frames.Count - 1];
            var route = frame.Call.Routes.FirstOrDefault(r => r.Id == _node.Result);
            if (route == null) { Fault("MissingResult", "调用点未配置结果：" + _node.Result + "，调用点：" + frame.Call.Id); return; }
            _frames.RemoveAt(_frames.Count - 1);
            Enter(route.TargetId);
        }

        private List<NovelCallFrame> CaptureFrames() => _frames.Select(f => new NovelCallFrame
        {
            CallId = f.Call.Id, FlowId = f.Flow.Id,
            Variables = f.Variables.Select(v => new NovelVariable(v.Key, v.Value)).ToList(),
            Returns = f.Call.Routes.Select(r => new NovelReturnAddress { Result = r.Id, NodeId = r.TargetId }).ToList()
        }).ToList();

        private List<FlowFrame> ReadFrames(NovelCheckpoint checkpoint, NovelChapter chapter, NovelNode current)
        {
            var frames = new List<FlowFrame>();
            var saved = checkpoint.CallStack ?? new List<NovelCallFrame>();
            if (saved.Count > MAX_FLOW_DEPTH || checkpoint.SchemaVersion < 9 && saved.Count != 0)
                throw new InvalidOperationException("存档调用栈版本或深度无效");
            string scope = "";
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var f in saved)
            {
                var call = chapter.Nodes.FirstOrDefault(n => n.Id == f?.CallId);
                var flow = chapter.Nodes.FirstOrDefault(n => n.Id == f?.FlowId);
                if (call == null || flow == null || call.Kind != NovelNodeKind.FlowCall || flow.Kind != NovelNodeKind.FlowStart ||
                    call.ScopeId != scope || call.LinkId != flow.Id || !seen.Add(flow.Id))
                    throw new InvalidOperationException("存档调用链无效：" + f?.CallId);
                if (f.Returns == null || f.Returns.Count != call.Routes.Count ||
                    f.Returns.Any(r => r == null || !call.Routes.Any(x => x.Id == r.Result && x.TargetId == r.NodeId)) ||
                    f.Returns.Select(r => r.Result).Distinct().Count() != f.Returns.Count)
                    throw new InvalidOperationException("存档返回位置失效，调用点：" + call.Id);
                frames.Add(new FlowFrame { Call = call, Flow = flow, Variables = ReadVariables(f.Variables, flow.FlowVariables) });
                scope = flow.Id;
            }
            if (current.ScopeId != scope) throw new InvalidOperationException("存档内部节点与调用栈不一致：" + current.Id);
            return frames;
        }
        #endregion
    }
}
