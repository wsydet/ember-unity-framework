using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ember.Core.Editor
{
    public sealed partial class EmberFsmBlueprintWindow
    {
        #region 编辑器面板参数
        [SerializeField] private bool _snapEnabled=true;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private const float GRID_STEP=16;
        private readonly List<GraphElement> _alignmentGuides=new();
        private Label _overlapNotice;
        private sealed class AlignmentGuide : GraphElement { }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private IEnumerable<string[]> LayoutSiblingGroups() => _nodes.Keys
            .GroupBy(id => _parents.TryGetValue(id, out var parent) ? parent : "")
            .OrderByDescending(group => group.Max(Depth)).Select(group => group.ToArray());

        private void UpdateOverlapNotice()
        {
            if (_overlapNotice == null) return;
            var overlaps = new List<string>();
            foreach (var group in LayoutSiblingGroups())
                for (int i = 0; i < group.Length; i++)
                    for (int j = i + 1; j < group.Length; j++)
                        if (Envelope(group[i]).Overlaps(Envelope(group[j])))
                            overlaps.Add(Display(group[i]) + " / " + Display(group[j]));
            _overlapNotice.text = overlaps.Count == 0 ? "" : "布局重叠（含来源/目标引用）："
                + string.Join("、", overlaps) + "。已保留坐标；可点“仅修复重叠”，不会在刷新时自动重排。";
            _overlapNotice.tooltip = _overlapNotice.text;
        }

        private void PlaceNewNodes(HashSet<string> missing)
        {
            FitFrames();
            foreach (var id in missing.OrderByDescending(Depth).ThenBy(x => x, StringComparer.Ordinal))
            {
                _parents.TryGetValue(id, out var parent);
                var siblings = _map.Keys.Where(x => x != id && _positions.ContainsKey(x)
                    && (_parents.TryGetValue(x, out var p) ? p : null) == parent).ToArray();
                var bounds = Envelope(id);
                // 只安置新增状态；已有坐标是用户布局，不允许刷新时整体重排。
                if (!siblings.Any(x => Envelope(x).Overlaps(bounds))) continue;
                float bottom = siblings.Max(x => Envelope(x).yMax) + 40;
                var delta = new Vector2(0, bottom - bounds.yMin);
                _positions[id] += delta;
                foreach (var child in _map.Keys.Where(x => Descendant(x, id))) _positions[child] += delta;
                FitFrames();
            }
        }

        private void ClearAlignmentGuides()
        { foreach(var line in _alignmentGuides) _canvas?.RemoveElement(line); _alignmentGuides.Clear(); }
        private Node[] MovingNodes(IEnumerable<GraphElement> elements)
            => elements.OfType<Node>().Where(n=>_nodes.TryGetValue(n.viewDataKey??"",out var real)&&real==n)
                .OrderBy(n=>Depth(n.viewDataKey)).ThenBy(n=>n.viewDataKey,StringComparer.Ordinal).ToArray();
        private Vector2 SnapCorrection(Node[] moving,bool draw)
        {
            if(moving.Length==0||!_snapEnabled)return Vector2.zero;
            var anchor=moving[0]; var rect=NodeRect(anchor); var position=rect.position;
            var correction=new Vector2(Mathf.Round(position.x/GRID_STEP)*GRID_STEP-position.x,
                Mathf.Round(position.y/GRID_STEP)*GRID_STEP-position.y);
            float zoom=_canvas.contentViewContainer.resolvedStyle.scale.value.x;
            float threshold=6/Math.Max(.1f,zoom), bestX=threshold,bestY=threshold;
            float? guideX=null,guideY=null;
            _parents.TryGetValue(anchor.viewDataKey,out var parent);
            foreach(var pair in _nodes)
            {
                if(moving.Contains(pair.Value)||moving.Any(n=>Descendant(pair.Key,n.viewDataKey)))continue;
                _parents.TryGetValue(pair.Key,out var otherParent);
                if(parent!=otherParent)continue; // 不吸到容纳它的父框或另一层节点。
                var other=NodeRect(pair.Value);
                foreach(float target in new[]{other.xMin,other.center.x,other.xMax})
                    foreach(float source in new[]{rect.xMin,rect.center.x,rect.xMax})
                        if(Math.Abs(target-source)<bestX){bestX=Math.Abs(target-source);correction.x=target-source;guideX=target;}
                foreach(float target in new[]{other.yMin,other.center.y,other.yMax})
                    foreach(float source in new[]{rect.yMin,rect.center.y,rect.yMax})
                        if(Math.Abs(target-source)<bestY){bestY=Math.Abs(target-source);correction.y=target-source;guideY=target;}
            }
            if(draw)
            {
                if(guideX.HasValue) Guide(new Rect(guideX.Value,rect.yMin-150,1/Math.Max(.1f,zoom),rect.height+300));
                if(guideY.HasValue) Guide(new Rect(rect.xMin-200,guideY.Value,rect.width+400,1/Math.Max(.1f,zoom)));
            }
            return correction;
        }
        private void Guide(Rect rect)
        {
            var line=new AlignmentGuide { capabilities=(Capabilities)0,pickingMode=PickingMode.Ignore,layer=5 };
            line.style.position=Position.Absolute;line.style.left=rect.x;line.style.top=rect.y;
            line.style.width=rect.width;line.style.height=rect.height;line.style.backgroundColor=new Color(.3f,.85f,.8f,.8f);
            _canvas.AddElement(line);_alignmentGuides.Add(line);
        }
        private void PreviewDragGuides()
        {
            ClearAlignmentGuides();
            if(_canvas==null)return;
            var selected = _canvas.selection.OfType<GraphElement>().ToList();
            if (!MovingNodes(selected).Any(n => _positions.TryGetValue(n.viewDataKey, out var old)
                && InlinePosition(n) != old)) return;
            CancelLayoutPreview();
            // SelectionDragger 已写入 inline 坐标；逐帧带动后代和外围引用，不能等松手才接回连线。
            SnapMovedNodes(selected);
            CapturePositions(); FitFrames(); ApplyPositions(); RenderPaths();
        }
        private void SnapMovedNodes(List<GraphElement> elements)
        {
            ClearAlignmentGuides();var moving=MovingNodes(elements);var delta=SnapCorrection(moving,true);
            foreach(var node in moving)node.SetPosition(new Rect(InlinePosition(node)+delta,NodeRect(node).size));
            // 选中父/子统一修正，再由 CapturePositions 只传播一次。引用位置不单独存储。
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>显式局部修复：按当前纵向顺序，只向下移动发生冲突的同级节点及其后代。</summary>
        public string[] RepairLayoutOverlaps()
        {
            var before = new Dictionary<string, Vector2>(_positions);
            PreviewLayout("overlaps");
            if (!ApplyLayoutPreview()) return Array.Empty<string>();
            return _positions.Keys.Where(id => before[id] != _positions[id]).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        }
        #endregion
    }
}
