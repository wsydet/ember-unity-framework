using System;
using System.Collections.Generic;
using Ember.Basic;
using UnityEngine;

namespace Ember.Core.Fsm
{
    [Serializable]
    public sealed class EmberFsmTargetBinding
    {
        #region 编辑器面板参数
        [SerializeField] private string _slot;
        [SerializeField] private string _targetId;
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public string Slot => _slot;
        public string TargetId => _targetId;
        public EmberFsmTargetBinding(string slot, string targetId) { _slot = slot; _targetId = targetId; }
        #endregion
    }

    [Serializable]
    public sealed class EmberFsmNode
    {
        #region 编辑器面板参数
        [SerializeField] private string _id = Guid.NewGuid().ToString("N");
        [SerializeField] private string _displayName;
        [SerializeField] private EmberFsmStateDefinition _definition;
        [SerializeField] private string _parentId;
        [SerializeField] private string _defaultChildId;
        [SerializeField] private List<EmberFsmTargetBinding> _bindings = new List<EmberFsmTargetBinding>();
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public string Id => _id;
        public string DisplayName => _displayName;
        public EmberFsmStateDefinition Definition => _definition;
        public string ParentId => _parentId;
        public string DefaultChildId => _defaultChildId;
        public IReadOnlyList<EmberFsmTargetBinding> Bindings => _bindings == null
            ? (IReadOnlyList<EmberFsmTargetBinding>)Array.Empty<EmberFsmTargetBinding>() : _bindings.AsReadOnly();
        public EmberFsmNode() { }
        public EmberFsmNode(string id, string displayName, EmberFsmStateDefinition definition,
            string parentId = null, string defaultChildId = null, params EmberFsmTargetBinding[] bindings)
        {
            _id = id; _displayName = displayName; _definition = definition;
            _parentId = parentId; _defaultChildId = defaultChildId;
            _bindings = new List<EmberFsmTargetBinding>(bindings ?? Array.Empty<EmberFsmTargetBinding>());
        }
        #endregion
    }

    [CreateAssetMenu(menuName = "Ember/Core/FSM Graph", fileName = "FsmGraph")]
    public sealed class EmberFsmGraph : EmberBaseSO
    {
        #region 编辑器面板参数
        [SerializeField] private string _entryId;
        [SerializeField] private List<EmberFsmNode> _nodes = new List<EmberFsmNode>();
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public string EntryId => _entryId;
        public IReadOnlyList<EmberFsmNode> Nodes => _nodes == null
            ? (IReadOnlyList<EmberFsmNode>)Array.Empty<EmberFsmNode>() : _nodes.AsReadOnly();

        /// <summary>配置资产或测试夹具；已装配的状态机持有结构快照，不会被后续配置变更影响。</summary>
        public void Configure(string entryId, params EmberFsmNode[] nodes)
        { _entryId = entryId; _nodes = new List<EmberFsmNode>(nodes ?? Array.Empty<EmberFsmNode>()); }

        /// <summary>无需角色的声明读取与校验。不执行工厂、生命周期、Guard 或进入预检。</summary>
        public EmberFsmGraphDescription Inspect(Type contextType = null) => EmberFsmGraphDescription.Build(this, contextType);
        #endregion
    }

    public sealed class EmberFsmNodeDescription
    {
        #region 内部参数
        public string Id { get; }
        public string DisplayName { get; }
        public EmberFsmStateDefinition Definition { get; }
        public string ParentId { get; }
        public string DefaultChildId { get; }
        public IReadOnlyList<EmberFsmTransition> Transitions { get; internal set; }
        public IReadOnlyList<string> RedirectSlots { get; internal set; }
        public IReadOnlyDictionary<string, string> Targets { get; }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal EmberFsmNodeDescription(EmberFsmNode node, Dictionary<string, string> targets)
        {
            Id = node.Id; DisplayName = node.DisplayName; Definition = node.Definition;
            ParentId = node.ParentId; DefaultChildId = node.DefaultChildId;
            Targets = new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(targets);
            Transitions = Array.Empty<EmberFsmTransition>(); RedirectSlots = Array.Empty<string>();
        }
        #endregion
    }

    /// <summary>结构快照，错误以节点 ID 定位；声明读取期间拒绝状态机控制操作及嵌套声明读取。</summary>
    public sealed class EmberFsmGraphDescription
    {
        #region 内部参数
        [ThreadStatic] internal static bool ReadingDeclarations;
        public string EntryId { get; private set; }
        public IReadOnlyList<EmberFsmNodeDescription> Nodes { get; private set; }
        public IReadOnlyList<string> Errors { get; private set; }
        public bool IsValid => Errors.Count == 0;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private static void ReadDeclarations(EmberFsmNodeDescription node, Type contextType, List<string> errors)
        {
            if (node.Definition == null) { errors.Add(node.Id + ": missing definition"); return; }
            try
            {
                if (contextType != null && node.Definition.ContextType != contextType)
                    errors.Add(node.Id + ": context type mismatch");
                var transitions = node.Definition.GetTransitions();
                var redirects = node.Definition.GetRedirectSlots();
                if (transitions == null || redirects == null) throw new InvalidOperationException("Null declaration list");
                node.Transitions = new List<EmberFsmTransition>(transitions).AsReadOnly();
                node.RedirectSlots = new List<string>(redirects).AsReadOnly();
                var ids = new HashSet<string>(StringComparer.Ordinal);
                var slots = new HashSet<string>(StringComparer.Ordinal);
                var orders = new HashSet<string>(StringComparer.Ordinal);
                foreach (var edge in node.Transitions)
                {
                    if (edge == null) { errors.Add(node.Id + ": null transition"); continue; }
                    if (string.IsNullOrWhiteSpace(edge.Id) || !ids.Add(edge.Id)) errors.Add(node.Id + ": duplicate/empty transition ID");
                    if (string.IsNullOrWhiteSpace(edge.TargetSlot)) errors.Add(node.Id + ": empty target slot");
                    else slots.Add(edge.TargetSlot);
                    if (edge.ContextType != node.Definition.ContextType ||
                        !edge.StateType.IsAssignableFrom(node.Definition.RuntimeStateType)) errors.Add(node.Id + ": transition type mismatch");
                    if (edge.Mode != EmberFsmTransitionMode.PreserveAncestors && edge.Mode != EmberFsmTransitionMode.ReenterTargetBranch)
                        errors.Add(node.Id + ": invalid transition mode");
                    if (edge.HasLegacyGuard && edge.Sites.Count > 0)
                        errors.Add(node.Id + "/" + edge.Id + ": legacy Guard and request sites cannot both own permission");
                    var siteIds = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var site in edge.Sites)
                    {
                        if (site == null) { errors.Add(node.Id + ": null request site"); continue; }
                        if (string.IsNullOrWhiteSpace(site.Id) || !siteIds.Add(site.Id))
                            errors.Add(node.Id + "/" + edge.Id + ": duplicate/empty request site ID");
                        if (string.IsNullOrWhiteSpace(site.Segment) || !Enum.IsDefined(typeof(EmberFsmTriggerPhase), site.Phase) ||
                            !Enum.IsDefined(typeof(EmberFsmContinuation), site.Continuation))
                            errors.Add(node.Id + "/" + edge.Id + ": invalid request site scheduling");
                        if (!orders.Add(site.Phase + ":" + site.Segment + ":" + site.Order))
                            errors.Add(node.Id + ": duplicate request site order in segment " + site.Segment);
                        if (site.ContextType != node.Definition.ContextType ||
                            !site.StateType.IsAssignableFrom(node.Definition.RuntimeStateType))
                            errors.Add(node.Id + ": request site type mismatch");
                    }
                }
                var uniqueRedirects = new HashSet<string>(StringComparer.Ordinal);
                foreach (var slot in node.RedirectSlots)
                {
                    if (string.IsNullOrWhiteSpace(slot) || !uniqueRedirects.Add(slot)) errors.Add(node.Id + ": duplicate/empty redirect slot");
                    else slots.Add(slot);
                }
                foreach (var slot in slots)
                    if (!node.Targets.ContainsKey(slot)) errors.Add(node.Id + ": missing binding " + slot);
                foreach (var slot in node.Targets.Keys)
                    if (!slots.Contains(slot)) errors.Add(node.Id + ": undeclared binding " + slot);
            }
            catch (Exception ex) { errors.Add(node.Id + ": declaration failure: " + ex.Message); }
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal static EmberFsmGraphDescription Build(EmberFsmGraph graph, Type contextType)
        {
            if (ReadingDeclarations) throw new InvalidOperationException("Nested FSM declaration read");
            ReadingDeclarations = true;
            try
            {
                var errors = new List<string>();
                var nodes = new List<EmberFsmNodeDescription>();
                var map = new Dictionary<string, EmberFsmNodeDescription>(StringComparer.Ordinal);
                foreach (var node in graph.Nodes)
                {
                    if (node == null || string.IsNullOrWhiteSpace(node.Id)) { errors.Add("Null node or empty ID"); continue; }
                    if (map.ContainsKey(node.Id)) { errors.Add(node.Id + ": duplicate ID"); continue; }
                    var targets = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (var binding in node.Bindings)
                    {
                        if (binding == null || string.IsNullOrWhiteSpace(binding.Slot)) { errors.Add(node.Id + ": empty binding"); continue; }
                        if (targets.ContainsKey(binding.Slot)) errors.Add(node.Id + ": duplicate binding " + binding.Slot);
                        else targets.Add(binding.Slot, binding.TargetId);
                    }
                    var item = new EmberFsmNodeDescription(node, targets);
                    nodes.Add(item); map.Add(item.Id, item);
                    ReadDeclarations(item, contextType, errors);
                }
                if (string.IsNullOrEmpty(graph.EntryId) || !map.ContainsKey(graph.EntryId)) errors.Add("Invalid entry ID");
                var parents = new HashSet<string>(StringComparer.Ordinal);
                foreach (var node in nodes)
                {
                    if (!string.IsNullOrEmpty(node.ParentId))
                    {
                        parents.Add(node.ParentId);
                        if (!map.ContainsKey(node.ParentId)) errors.Add(node.Id + ": invalid parent");
                    }
                    var visited = new HashSet<string>(StringComparer.Ordinal);
                    var cursor = node;
                    while (cursor != null)
                    {
                        if (!visited.Add(cursor.Id)) { errors.Add(node.Id + ": hierarchy cycle"); break; }
                        if (string.IsNullOrEmpty(cursor.ParentId) || !map.TryGetValue(cursor.ParentId, out cursor)) break;
                    }
                    if (!string.IsNullOrEmpty(node.DefaultChildId) &&
                        (!map.TryGetValue(node.DefaultChildId, out var child) || child.ParentId != node.Id))
                        errors.Add(node.Id + ": default must be a direct child");
                    foreach (var target in node.Targets.Values)
                        if (string.IsNullOrEmpty(target) || !map.ContainsKey(target)) errors.Add(node.Id + ": invalid bound target");
                }
                foreach (var node in nodes)
                    if (parents.Contains(node.Id) && string.IsNullOrEmpty(node.DefaultChildId)) errors.Add(node.Id + ": missing default child");
                return new EmberFsmGraphDescription { EntryId = graph.EntryId, Nodes = nodes.AsReadOnly(), Errors = errors.AsReadOnly() };
            }
            finally { ReadingDeclarations = false; }
        }
        #endregion
    }
}
