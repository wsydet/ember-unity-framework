using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ember.Core.Editor
{
    public sealed partial class EmberFsmBlueprintWindow
    {
        #region 内部参数
        private readonly HashSet<string> _pinnedLayout = new();
        private readonly List<GraphElement> _layoutGhosts = new();
        private EmberFsmLayoutPlanner _layoutCandidate;
        private Dictionary<string, Vector2> _previewBefore;
        private string _previewSignature;
        private Label _candidateNotice;
        private Label _candidateDetails;
        private Button _applyLayoutButton, _cancelLayoutButton;
        private EmberFsmLayoutUndoState _layoutUndo;
        private string _layoutUndoStamp;
        private sealed class LayoutGhost : GraphElement { }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void AddLayoutToolbar()
        {
            var toolbar = new Toolbar { name = "layout-actions" };
            toolbar.style.flexWrap = Wrap.Wrap;
            toolbar.style.height = StyleKeyword.Auto; toolbar.style.minHeight = 30;
            toolbar.Add(new Label("布局预览"));
            toolbar.Add(new ToolbarButton(() => PreviewLayout("overlaps")) { text = "仅修复重叠" });
            toolbar.Add(new ToolbarButton(() => PreviewLayout("branch")) { text = "整理选中分支" });
            toolbar.Add(new ToolbarButton(() => PreviewLayout("all")) { text = "整理全图" });
            toolbar.Add(new ToolbarButton(() => PinSelection(true)) { text = "固定所选", tooltip = "固定复合节点同时保护全部后代；不改变图或状态行为。" });
            toolbar.Add(new ToolbarButton(() => PinSelection(false)) { text = "取消所选固定" });
            toolbar.Add(new ToolbarButton(FitLayoutPreview) { text = "适应预览" });
            _applyLayoutButton = new ToolbarButton(() => ApplyLayoutPreview()) { text = "应用预览", name = "apply-layout-preview" };
            _cancelLayoutButton = new ToolbarButton(CancelLayoutPreview) { text = "取消预览" };
            toolbar.Add(_applyLayoutButton); toolbar.Add(_cancelLayoutButton);
            rootVisualElement.Add(toolbar);
            _candidateNotice = new Label("选择范围生成候选；固定复合节点会保护其全部后代。") { name = "layout-preview-notice" };
            _candidateNotice.style.whiteSpace = WhiteSpace.Normal;
            rootVisualElement.Add(_candidateNotice);
            var details = new Foldout { text = "移动清单 / 约束冲突", value = false };
            var scroll = new ScrollView(); scroll.style.maxHeight = 140;
            _candidateDetails = new Label(); _candidateDetails.style.whiteSpace = WhiteSpace.Normal;
            scroll.Add(_candidateDetails); details.Add(scroll); rootVisualElement.Add(details);
            _applyLayoutButton.SetEnabled(false); _cancelLayoutButton.SetEnabled(false);
        }
        private string[] SelectedLayoutIds()
        {
            var ids = _canvas.selection.OfType<Node>().Select(n => n.viewDataKey).Where(_map.ContainsKey).ToArray();
            return ids.Length > 0 ? ids : _selected != null ? new[] { _selected.Id } : Array.Empty<string>();
        }
        private void FitLayoutPreview()
        {
            if (_layoutCandidate == null) { FitVisible(); return; }
            var bounds = _nodes.Keys.SelectMany(id => new[] { Envelope(id), _layoutCandidate.Bounds(id) }).ToArray();
            if (bounds.Length == 0) return;
            float left = bounds.Min(r => r.xMin) - 35, top = bounds.Min(r => r.yMin) - 35;
            float right = bounds.Max(r => r.xMax) + 35, bottom = bounds.Max(r => r.yMax) + 35;
            var size = _canvas.contentRect.size;
            float zoom = Mathf.Clamp(Math.Min(size.x / Math.Max(1, right - left), size.y / Math.Max(1, bottom - top)), .1f, 1f);
            _canvas.UpdateViewTransform(new Vector3(size.x / 2 - (left + right) * zoom / 2,
                size.y / 2 - (top + bottom) * zoom / 2, 0), new Vector3(zoom, zoom, 1));
        }
        private void PinSelection(bool pinned)
        {
            var ids = SelectedLayoutIds();
            if (ids.Length == 0) { _candidateNotice.text = "请先选择要固定的状态。"; return; }
            SetLayoutPinned(ids, pinned);
        }
        private bool LayoutLocked(string id) => _pinnedLayout.Any(p => p == id || Descendant(id, p));
        private string LayoutSignature() => string.Join("|", _map.Keys.OrderBy(k => k, StringComparer.Ordinal).Select(id =>
            id + ":" + _positions[id].x.ToString("R") + "," + _positions[id].y.ToString("R") + "," + _sizes[id]
            + ":" + _collapsed.Contains(id) + ":" + _pinnedLayout.Contains(id) + ":" + ExteriorHeader(id))) + ":" + _showRedirects;
        private void SyncLayoutUndo()
        {
            if (_layoutUndo == null)
            { _layoutUndo = CreateInstance<EmberFsmLayoutUndoState>(); _layoutUndo.hideFlags = HideFlags.HideAndDontSave; }
            _layoutUndo.Entries = _positions.Select(p => new EmberFsmLayoutUndoState.Entry
                { Id = p.Key, Position = p.Value, Pinned = _pinnedLayout.Contains(p.Key) }).ToArray();
            _layoutUndoStamp = JsonUtility.ToJson(_layoutUndo);
        }
        private void BeginLayoutUndo(string name)
        {
            SyncLayoutUndo(); Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName(name);
            Undo.RegisterCompleteObjectUndo(_layoutUndo, name);
        }
        private void LayoutUndoRedo()
        {
            if (_layoutUndo == null || _canvas == null) return;
            var stamp = JsonUtility.ToJson(_layoutUndo);
            if (_layoutUndoStamp == stamp) return;
            _layoutUndoStamp = stamp;
            CancelLayoutPreview();
            _pinnedLayout.Clear();
            foreach (var entry in _layoutUndo.Entries)
            {
                if (!_map.ContainsKey(entry.Id)) continue;
                _positions[entry.Id] = entry.Position;
                if (entry.Pinned) _pinnedLayout.Add(entry.Id);
            }
            ApplyPositions(); FitFrames(); ApplyPositions(); RenderPaths(); UpdatePinStyles(); PersistLayout();
        }
        private void DisposeLayoutUndo()
        {
            if (_layoutUndo == null) return;
            Undo.ClearUndo(_layoutUndo); DestroyImmediate(_layoutUndo); _layoutUndo = null;
        }
        private void UpdatePinStyles()
        {
            foreach (var pair in _nodes)
            {
                bool locked = LayoutLocked(pair.Key);
                bool branchLocked = locked || _pinnedLayout.Any(p => Descendant(p, pair.Key));
                pair.Value.capabilities = Capabilities.Selectable | (branchLocked ? (Capabilities)0 : Capabilities.Movable);
                pair.Value.title = Display(pair.Key) + (Children(pair.Key).Any() ? " · 复合状态" : "")
                    + (pair.Key == _description.EntryId ? " ◆ 图入口" : "") + (locked ? " · 固定" : "");
                pair.Value.titleContainer.tooltip = Display(pair.Key) + " [" + pair.Key + "]"
                    + (locked ? "\n已固定（复合节点的全部后代同时受保护）。" : branchLocked ? "\n分支内含固定节点，不能整体拖动。" : "");
            }
        }
        private void DrawLayoutGhosts()
        {
            foreach (var pair in _layoutCandidate.Positions)
            {
                if (!_nodes.ContainsKey(pair.Key)) continue;
                var bounds = _layoutCandidate.Bounds(pair.Key);
                if (pair.Value == _previewBefore[pair.Key] && bounds == Envelope(pair.Key)) continue;
                var ghost = new LayoutGhost { pickingMode = PickingMode.Ignore, capabilities = (Capabilities)0, layer = 10 };
                ghost.style.position = Position.Absolute;
                ghost.style.left = bounds.x; ghost.style.top = bounds.y; ghost.style.width = bounds.width; ghost.style.height = bounds.height;
                ghost.style.borderLeftWidth = ghost.style.borderTopWidth = ghost.style.borderRightWidth = ghost.style.borderBottomWidth = 2;
                ghost.style.borderLeftColor = ghost.style.borderTopColor = ghost.style.borderRightColor = ghost.style.borderBottomColor = new Color(1, .72f, .2f);
                var label = new Label("候选 · " + Display(pair.Key)); label.style.color = new Color(1, .8f, .3f); ghost.Add(label);
                _canvas.AddElement(ghost); _layoutGhosts.Add(ghost);
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>只计算候选并叠加显示边界，不修改本机坐标。scope 为 overlaps、branch 或 all。</summary>
        public string[] PreviewLayout(string scope)
        {
            CancelLayoutPreview();
            if (_map.Count == 0) return new[] { "请先选择有效图。" };
            if (scope != "overlaps" && scope != "branch" && scope != "all") throw new ArgumentException("未知布局范围", nameof(scope));
            var selected = SelectedLayoutIds();
            if (scope == "branch" && selected.Length == 0)
            { _candidateNotice.text = "请先选择分支根节点。"; return new[] { _candidateNotice.text }; }
            var allowed = new HashSet<string>(_map.Keys.Where(id => scope != "branch" || selected.Any(root => id == root || Descendant(id, root))));
            _previewBefore = new Dictionary<string, Vector2>(_positions);
            _previewSignature = LayoutSignature();
            _layoutCandidate = new EmberFsmLayoutPlanner(_map.Keys.Select(id => new EmberFsmLayoutPlanner.Item
            {
                Id = id, Parent = _parents.TryGetValue(id, out var parent) ? parent : null,
                Position = _positions[id], Header = ExteriorHeader(id), Collapsed = _collapsed.Contains(id),
                Left = Sources(id).Length > 0 ? EXTENT : 20, Right = Exits(id).Length > 0 ? EXTENT : 20
            }), allowed, _pinnedLayout);
            _layoutCandidate.Calculate(scope != "overlaps", scope == "branch");
            var moved = _map.Keys.Where(id => _layoutCandidate.Positions[id] != _positions[id]).OrderBy(id => id, StringComparer.Ordinal).ToArray();
            var conflicts = _layoutCandidate.Conflicts.ToArray();
            _candidateNotice.text = "预览：移动 " + moved.Length + " 个节点（含隐藏后代）；"
                + (conflicts.Length == 0 ? moved.Length == 0 ? "无需移动。" : "可应用。金框为候选，原图尚未移动。" : "存在 " + conflicts.Length + " 项冲突，不能应用。展开下方查看原因。");
            _candidateDetails.text = string.Join("\n", conflicts)
                + (conflicts.Length == 0 ? "" : "\n\n")
                + string.Join("\n", moved.Select(id => Display(id) + " [" + id + "]：" + _positions[id] + " → " + _layoutCandidate.Positions[id]));
            _candidateNotice.tooltip = _candidateDetails.text;
            _applyLayoutButton.SetEnabled(conflicts.Length == 0 && moved.Length > 0); _cancelLayoutButton.SetEnabled(true);
            DrawLayoutGhosts(); return conflicts;
        }
        public bool ApplyLayoutPreview()
        {
            if (_layoutCandidate == null || _layoutCandidate.Conflicts.Count != 0) return false;
            if (_previewSignature != LayoutSignature())
            { CancelLayoutPreview(); _candidateNotice.text = "布局或显示范围已变化，请重新预览。"; return false; }
            if (_layoutCandidate.Positions.All(p => p.Value == _positions[p.Key])) return false;
            BeginLayoutUndo("应用 FSM 布局");
            foreach (var pair in _layoutCandidate.Positions) _positions[pair.Key] = pair.Value;
            foreach (var pair in _layoutCandidate.Sizes) _sizes[pair.Key] = pair.Value;
            CancelLayoutPreview(); ApplyPositions(); RenderPaths(); PersistLayout(); SyncLayoutUndo();
            _candidateNotice.text = "布局已应用并保存到本机；可用 Undo/Redo 撤销或重做。";
            return true;
        }
        public void CancelLayoutPreview()
        {
            foreach (var ghost in _layoutGhosts) _canvas?.RemoveElement(ghost);
            _layoutGhosts.Clear(); _layoutCandidate = null; _previewBefore = null;
            _applyLayoutButton?.SetEnabled(false); _cancelLayoutButton?.SetEnabled(false);
            if (_candidateNotice != null) _candidateNotice.text = "选择范围生成候选；固定复合节点会保护其全部后代。";
            if (_candidateDetails != null) _candidateDetails.text = "尚未生成预览。";
        }
        public void SetLayoutPinned(string[] ids, bool pinned)
        {
            var valid = ids.Where(_map.ContainsKey).Distinct().ToArray();
            if (!valid.Any(id => _pinnedLayout.Contains(id) != pinned)) return;
            CancelLayoutPreview(); BeginLayoutUndo(pinned ? "固定 FSM 节点" : "取消 FSM 节点固定");
            foreach (var id in valid) { if (pinned) _pinnedLayout.Add(id); else _pinnedLayout.Remove(id); }
            UpdatePinStyles(); PersistLayout(); SyncLayoutUndo();
        }
        #endregion
    }
}
