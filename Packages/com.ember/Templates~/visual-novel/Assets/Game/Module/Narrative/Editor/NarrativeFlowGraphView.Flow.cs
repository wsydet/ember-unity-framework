using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Narrative.Editor
{
    public sealed partial class NarrativeFlowGraphView
    {
        #region 内部参数
        private readonly HashSet<string> _collapsedFlows = new();
        private NarrativeTableCatalog _catalog;
        private bool _showLinks;
        private IMGUIContainer _linkOverlay;
        public NarrativeFlowStartSO ViewFlow { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private IEnumerable<FlowNode> ExpandFlowSelection(IEnumerable<FlowNode> selected)
        {
            var list = selected.ToList();
            var scopes = list.Where(n => n.Model is NarrativeFlowStartSO).Select(n => n.Model.NodeId).ToHashSet();
            return list.Concat(_nodes.Values.Where(n => scopes.Contains(n.Model.ScopeId))).Distinct();
        }

        private void BuildFlowMenu(ContextualMenuPopulateEvent e)
        {
            if (!_chapter) return;
            e.menu.AppendAction("流程视图/显示主线与独立区域", _ => ShowFlow(null));
            e.menu.AppendAction(_showLinks ? "隐藏跳转关联线" : "显示跳转关联线", _ => { _showLinks = !_showLinks; Rebuild(_chapter, _catalog); });
            var model = selection.OfType<FlowNode>().FirstOrDefault()?.Model;
            var flow = model as NarrativeFlowStartSO ?? (model as NarrativeFlowCallSO)?.Callee ?? model?.Flow;
            if (flow)
            {
                e.menu.AppendAction("流程视图/进入 " + flow.name, _ => ShowFlow(flow));
                e.menu.AppendAction("流程视图/折叠或展开 " + flow.name, _ => { NarrativeGraphModel.SetFlowCollapsed(_chapter, flow, !_collapsedFlows.Contains(flow.NodeId)); Rebuild(_chapter, _catalog); });
                e.menu.AppendAction("流程视图/选择整个流程（移动或复制）", _ =>
                {
                    _building = true; ClearSelection();
                    foreach (var n in _nodes.Values.Where(n => n.Model.ScopeId == flow.NodeId)) AddToSelection(n);
                    _building = false;
                });
            }
            if (model is NarrativeJumpSO jump)
            {
                var receiver = _chapter.Nodes.FirstOrDefault(n => n && n.NodeId == jump.ReceiverId);
                if (receiver) e.menu.AppendAction("定位接收点/" + receiver.name, _ => LocateFlowNode(receiver));
                if (_editable) foreach (var target in _chapter.Nodes.OfType<NarrativeReceiverSO>().Where(n => n.ScopeId == jump.ScopeId))
                    e.menu.AppendAction("关联接收点/" + target.name + " · " + target.NodeId, _ => _edit(() => NarrativeGraphModel.SetReceiver(_chapter, jump, target)));
            }
            if (model is NarrativeReceiverSO)
                foreach (var source in _chapter.Nodes.OfType<NarrativeJumpSO>().Where(n => n.ReceiverId == model.NodeId))
                    e.menu.AppendAction("全部跳转来源/" + source.name + " · " + source.NodeId, _ => LocateFlowNode(source));
            if (model is NarrativeFlowCallSO call && call.Callee)
                e.menu.AppendAction("定位流程开始", _ => LocateFlowNode(call.Callee));
        }

        private void LocateFlowNode(NarrativeNodeSO model)
        {
            ViewFlow = model as NarrativeFlowStartSO ?? model.Flow;
            if (model.Flow) NarrativeGraphModel.SetFlowCollapsed(_chapter, model.Flow, false); Rebuild(_chapter, _catalog); SelectModel(model, true); _selected(model);
        }

        private void ApplyFlowVisibility()
        {
            foreach (var n in _nodes.Values)
            {
                bool visible = ViewFlow ? n.Model.ScopeId == ViewFlow.NodeId :
                    !_collapsedFlows.Contains(n.Model.ScopeId) || n.Model is NarrativeFlowStartSO;
                n.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (n.Model is NarrativeFlowStartSO && _collapsedFlows.Contains(n.Model.NodeId)) n.title += "（已折叠）";
            }
            foreach (var edge in edges.ToList())
                edge.style.display = edge.input.node.style.display == DisplayStyle.None || edge.output.node.style.display == DisplayStyle.None ? DisplayStyle.None : DisplayStyle.Flex;
            _linkOverlay?.MarkDirtyRepaint();
        }

        private void DrawJumpLinks()
        {
            if (!_showLinks || Event.current.type != EventType.Repaint || panel == null) return;
            foreach (var n in _nodes.Values.Where(n => n.Model is NarrativeJumpSO && n.style.display != DisplayStyle.None))
            {
                var jump = (NarrativeJumpSO)n.Model;
                var target = _nodes.Values.FirstOrDefault(x => x.Model.NodeId == jump.ReceiverId);
                if (target == null || target.style.display == DisplayStyle.None) continue;
                // Pure overlay: associations never create hidden input/output ports or editable graph edges.
                Vector2 from = _linkOverlay.WorldToLocal(new Vector2(n.worldBound.xMax, n.worldBound.center.y));
                Vector2 to = _linkOverlay.WorldToLocal(new Vector2(target.worldBound.xMin, target.worldBound.center.y));
                Handles.DrawBezier(from, to, from + Vector2.right * 60, to + Vector2.left * 60,
                    new Color(.95f, .7f, .3f, .8f), null, 2);
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void ShowFlow(NarrativeFlowStartSO flow)
        {
            if (flow && (!_chapter || !_chapter.Nodes.Contains(flow))) throw new System.ArgumentException("流程不属于当前章节");
            ViewFlow = flow; Rebuild(_chapter, _catalog); FrameAll();
        }
        #endregion
    }
}
