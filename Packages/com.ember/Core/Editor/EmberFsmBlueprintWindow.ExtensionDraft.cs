using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ember.Core.Editor
{
    public sealed partial class EmberFsmBlueprintWindow
    {
        #region 编辑器面板参数
        [SerializeField] private string _extensionLayoutDraft;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        [Serializable] private sealed class ExtensionLayoutEntry { public string id; public Vector2 before, position, size, baseSize; public bool collapsed, pinned; }
        [Serializable] private sealed class ExtensionLayoutDraft
        { public string signature, graphData, notice, details; public bool redirects; public string[] conflicts; public ExtensionLayoutEntry[] entries; }
        private bool _layoutSuspended;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void CaptureExtensionLayoutDraft()
        {
            if (_layoutCandidate == null || _previewBefore == null) return;
            _extensionLayoutDraft = JsonUtility.ToJson(new ExtensionLayoutDraft
            {
                signature = _previewSignature, notice = _candidateNotice?.text, details = _candidateDetails?.text,
                graphData = UnityEditor.EditorJsonUtility.ToJson(_graph), redirects = _showRedirects,
                conflicts = _layoutCandidate.Conflicts.ToArray(),
                entries = _layoutCandidate.Positions.Select(p => new ExtensionLayoutEntry
                { id = p.Key, position = p.Value, before = _previewBefore[p.Key], size = _layoutCandidate.Sizes[p.Key], baseSize = _sizes[p.Key], collapsed = _collapsed.Contains(p.Key), pinned = _pinnedLayout.Contains(p.Key) }).ToArray()
            });
        }
        private void RestoreExtensionLayoutDraft()
        {
            if (string.IsNullOrEmpty(_extensionLayoutDraft) || !IsTemplateAvailable || _map.Count == 0) return;
            var draft = JsonUtility.FromJson<ExtensionLayoutDraft>(_extensionLayoutDraft);
            if (draft?.entries == null || draft.graphData != UnityEditor.EditorJsonUtility.ToJson(_graph) || draft.entries.Length != _map.Count || draft.entries.Any(e => !_map.ContainsKey(e.id)))
            { if (_candidateNotice != null) _candidateNotice.text = "原布局已变化；旧候选草稿保留，请明确重新预览。"; return; }
            _collapsed.Clear(); _pinnedLayout.Clear(); _showRedirects = draft.redirects;
            foreach (var entry in draft.entries)
            {
                _positions[entry.id] = entry.before; _sizes[entry.id] = entry.baseSize;
                if (entry.collapsed) _collapsed.Add(entry.id); if (entry.pinned) _pinnedLayout.Add(entry.id);
            }
            _layoutCandidate = new EmberFsmLayoutPlanner(_map.Keys.Select(id => new EmberFsmLayoutPlanner.Item
            {
                Id = id, Parent = _parents.TryGetValue(id, out var parent) ? parent : null, Position = _positions[id],
                Header = ExteriorHeader(id), Collapsed = _collapsed.Contains(id), Left = Sources(id).Length > 0 ? EXTENT : 20, Right = Exits(id).Length > 0 ? EXTENT : 20
            }), new HashSet<string>(_map.Keys), _pinnedLayout);
            _previewBefore = new Dictionary<string, Vector2>(); _previewSignature = LayoutSignature();
            foreach (var entry in draft.entries)
            { _previewBefore[entry.id] = entry.before; _layoutCandidate.Positions[entry.id] = entry.position; _layoutCandidate.Sizes[entry.id] = entry.size; }
            _layoutCandidate.Conflicts.AddRange(draft.conflicts ?? Array.Empty<string>());
            _candidateNotice.text = draft.notice; _candidateDetails.text = draft.details; _layoutSuspended = true;
            ApplyPositions(); RenderPaths();
            ResumeExtensionLayoutDraft();
        }
        private void SuspendExtensionLayoutDraft()
        {
            CaptureExtensionLayoutDraft();
            foreach (var ghost in _layoutGhosts) _canvas?.RemoveElement(ghost);
            _layoutGhosts.Clear(); _layoutSuspended = true;
        }
        private void ResumeExtensionLayoutDraft()
        {
            if (!_layoutSuspended) return;
            if (_layoutCandidate == null) { RestoreExtensionLayoutDraft(); return; }
            if (_layoutCandidate == null) return;
            _layoutSuspended = false;
            DrawLayoutGhosts();
            _applyLayoutButton?.SetEnabled(_layoutCandidate.Conflicts.Count == 0 && _layoutCandidate.Positions.Any(p => _positions[p.Key] != p.Value));
            _cancelLayoutButton?.SetEnabled(true);
        }
        #endregion
    }
}
