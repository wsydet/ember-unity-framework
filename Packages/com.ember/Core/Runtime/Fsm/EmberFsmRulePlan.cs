using System;
using System.Collections.Generic;
using System.Linq;

namespace Ember.Core.Fsm
{
    public sealed class EmberFsmRuleDescription
    {
        public string NodeId { get; }
        public string TransitionId { get; }
        public string SiteId { get; }
        public string Identity { get; }
        public string Condition { get; }
        internal EmberFsmRuleDescription(string node, string transition, string site, string identity, string condition)
        { NodeId = node; TransitionId = transition; SiteId = site; Identity = identity; Condition = condition; }
    }

    /// <summary>附加条件只能绑定已声明句柄与请求位置，不替代原代码许可。</summary>
    public sealed class EmberFsmRuleBinding<T> where T : class
    {
        internal EmberFsmTransition Transition { get; }
        internal EmberConditionPlan<T> Condition { get; }
        public EmberFsmRuleDescription Description { get; }
        public EmberFsmRuleBinding(string nodeId, EmberFsmTransition transition, string siteId, EmberConditionPlan<T> condition)
        {
            Transition = transition ?? throw new ArgumentNullException(nameof(transition));
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            Description = new EmberFsmRuleDescription(nodeId, transition.Id, siteId, condition.Identity, condition.Description);
        }
    }

    public sealed class EmberFsmRulePlan<T> where T : class
    {
        private readonly EmberFsmRuleBinding<T>[] _bindings;
        public IReadOnlyList<EmberFsmRuleDescription> Descriptions { get; }
        public EmberFsmRulePlan(params EmberFsmRuleBinding<T>[] bindings)
        {
            _bindings = (EmberFsmRuleBinding<T>[])bindings.Clone();
            if (_bindings.Any(b => b == null)) throw new ArgumentException("Rule binding is null.");
            Descriptions = Array.AsReadOnly(_bindings.Select(b => b.Description).ToArray());
        }
        public void Validate(EmberFsmGraphDescription graph)
        {
            var keys = new HashSet<(string, string, string)>();
            foreach (var binding in _bindings)
            {
                var d = binding.Description;
                var node = graph.Nodes.FirstOrDefault(n => n.Id == d.NodeId);
                if (!keys.Add((d.NodeId, d.TransitionId, d.SiteId)) || node == null
                    || !node.Transitions.Any(t => ReferenceEquals(t, binding.Transition))
                    || binding.Transition.ContextType != typeof(T)
                    || !binding.Transition.Sites.Any(s => s.Id == d.SiteId))
                    throw new ArgumentException("附加规则绑定无效：" + d.NodeId + "/" + d.TransitionId + "/" + d.SiteId);
            }
        }
        internal EmberConditionPlan<T> Find(string nodeId, EmberFsmTransition transition, string siteId)
        {
            foreach (var binding in _bindings)
                if (binding.Description.NodeId == nodeId && ReferenceEquals(binding.Transition, transition) && binding.Description.SiteId == siteId)
                    return binding.Condition;
            return null;
        }
    }
}
