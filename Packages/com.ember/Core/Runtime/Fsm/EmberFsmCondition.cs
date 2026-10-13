using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Basic;
using UnityEngine;

namespace Ember.Core.Fsm
{
    public enum EmberConditionValueKind { Number, Boolean, Text, Choice }

    [Serializable]
    public sealed class EmberConditionParameter
    {
        [SerializeField] private string _name;
        [SerializeField] private EmberConditionValueKind _type;
        [SerializeField] private float _number;
        [SerializeField] private bool _boolean;
        [SerializeField] private string _text;
        [SerializeField] private int _choice;
        public string Name => _name;
        public EmberConditionValueKind Type => _type;
        public float Number => _number;
        public bool Boolean => _boolean;
        public string Text => _text;
        public int Choice => _choice;
        public EmberConditionParameter(string name, EmberConditionValueKind type, float number = 0,
            bool boolean = false, string text = null, int choice = 0)
        { _name = name; _type = type; _number = number; _boolean = boolean; _text = text; _choice = choice; }
        internal EmberConditionParameter Copy() => new(Name, Type, Number, Boolean, Text, Choice);
    }

    [Serializable]
    public sealed class EmberConditionNode
    {
        [SerializeField] private string _id;
        [SerializeField] private string _kind;
        [SerializeField] private EmberConditionParameter[] _parameters = Array.Empty<EmberConditionParameter>();
        [SerializeField] private string[] _children = Array.Empty<string>();
        public string Id => _id;
        public string Kind => _kind;
        public IReadOnlyList<EmberConditionParameter> Parameters => Array.AsReadOnly(_parameters);
        public IReadOnlyList<string> Children => Array.AsReadOnly(_children);
        public EmberConditionNode(string id, string kind, EmberConditionParameter[] parameters = null, params string[] children)
        { _id = id; _kind = kind; _parameters = parameters ?? Array.Empty<EmberConditionParameter>(); _children = children ?? Array.Empty<string>(); }
    }

    /// <summary>原生序列化的有限表达式树；节点表只含值和稳定 ID，不保存类型名或宿主。</summary>
    [Serializable]
    public sealed class EmberConditionExpression
    {
        [SerializeField] private string _root;
        [SerializeField] private EmberConditionNode[] _nodes = Array.Empty<EmberConditionNode>();
        public string Root => _root;
        public IReadOnlyList<EmberConditionNode> Nodes => Array.AsReadOnly(_nodes);
        public EmberConditionExpression(string root, params EmberConditionNode[] nodes) { _root = root; _nodes = nodes; }
        [HasGC]
        public static EmberConditionExpression True() => new("root", new EmberConditionNode("root", "true"));
    }

    public readonly struct EmberConditionSample
    {
        public bool Result { get; }
        public string Fact { get; }
        public EmberConditionSample(bool result, string fact = null) { Result = result; Fact = fact; }
    }

    public sealed class EmberConditionTrace
    {
        public string NodeId { get; }
        public string Description { get; }
        public bool? Result { get; }
        public string Fact { get; }
        internal EmberConditionTrace(string id, string description, bool? result, string fact)
        { NodeId = id; Description = description; Result = result; Fact = fact; }
    }

    /// <summary>叶积木的静态参数协议；用于统一校验与编辑显示。</summary>
    public sealed class EmberConditionParameterSpec
    {
        public string Name { get; }
        public string Label { get; }
        public EmberConditionValueKind Type { get; }
        public IReadOnlyList<string> Choices { get; }
        public EmberConditionParameterSpec(string name, string label, EmberConditionValueKind type, params string[] choices)
        { Name = name; Label = label; Type = type; Choices = Array.AsReadOnly((string[])choices.Clone()); }
    }

    public sealed class EmberConditionLeaf<T> where T : class
    {
        public string Id { get; }
        public string Label { get; }
        public IReadOnlyList<EmberConditionParameterSpec> Parameters { get; }
        internal Func<IReadOnlyList<EmberConditionParameter>, Func<T, bool, EmberConditionSample>> Compile { get; }
        internal Func<IReadOnlyList<EmberConditionParameter>, string> Describe { get; }
        public EmberConditionLeaf(string id, string label, EmberConditionParameterSpec[] parameters,
            Func<IReadOnlyList<EmberConditionParameter>, Func<T, bool, EmberConditionSample>> compile,
            Func<IReadOnlyList<EmberConditionParameter>, string> describe)
        { Id = id; Label = label; Parameters = Array.AsReadOnly((EmberConditionParameterSpec[])parameters.Clone());
            Compile = compile ?? throw new ArgumentNullException(nameof(compile)); Describe = describe ?? throw new ArgumentNullException(nameof(describe)); }
    }

    /// <summary>按强类型上下文登记叶积木；编译仅校验参数，不访问运行上下文。</summary>
    public sealed class EmberConditionCatalog<T> where T : class
    {
        private readonly Dictionary<string, EmberConditionLeaf<T>> _leaves;
        public IReadOnlyList<EmberConditionLeaf<T>> Leaves { get; }
        public EmberConditionCatalog(params EmberConditionLeaf<T>[] leaves)
        {
            _leaves = new Dictionary<string, EmberConditionLeaf<T>>(StringComparer.Ordinal);
            foreach (var leaf in leaves)
                if (leaf == null || string.IsNullOrWhiteSpace(leaf.Id) || leaf.Id is "true" or "and" or "or" or "not" || !_leaves.TryAdd(leaf.Id, leaf))
                    throw new ArgumentException("Duplicate or reserved condition kind.");
            Leaves = Array.AsReadOnly((EmberConditionLeaf<T>[])leaves.Clone());
        }
        public EmberConditionPlan<T> Compile(EmberConditionExpression expression)
        {
            if (expression == null || string.IsNullOrWhiteSpace(expression.Root)) throw new ArgumentException("条件根缺失。");
            var map = new Dictionary<string, EmberConditionNode>(StringComparer.Ordinal);
            foreach (var node in expression.Nodes)
                if (node == null || string.IsNullOrWhiteSpace(node.Id) || !map.TryAdd(node.Id, node)) throw new ArgumentException("条件节点 ID 缺失或重复。");
            var visited = new HashSet<string>(StringComparer.Ordinal);
            EmberConditionPlan<T>.Node Build(string id, int depth)
            {
                if (id == null || !map.TryGetValue(id, out var data)) throw new ArgumentException("条件子节点缺失：" + id);
                if (depth > 32 || map.Count > 256) throw new ArgumentException("条件树最多32层、256节点。");
                if (!visited.Add(id)) throw new ArgumentException("条件树有环或重复引用：" + id);
                try
                {
                    var parameters = data.Parameters.Select(p => p?.Copy() ?? throw new ArgumentException("条件参数为空。")).ToArray();
                    var kind = data.Kind;
                    bool composite = kind is "and" or "or" or "not";
                    if (kind == "true" || composite)
                    {
                        if (parameters.Length != 0 || (kind == "true" && data.Children.Count != 0)
                            || (kind == "not" && data.Children.Count != 1)
                            || ((kind == "and" || kind == "or") && data.Children.Count < 2)) throw new ArgumentException("组合参数或子节点数量无效。");
                        return new EmberConditionPlan<T>.Node(id, kind, kind switch { "and" => "全部满足 AND", "or" => "任一满足 OR", "not" => "不满足 NOT", _ => "无额外限制" },
                            data.Children.Select(c => Build(c, depth + 1)).ToArray(), null, "");
                    }
                    if (kind == null || !_leaves.TryGetValue(kind, out var leaf)) throw new ArgumentException("未知积木：" + kind);
                    if (data.Children.Count != 0 || parameters.Length != leaf.Parameters.Count) throw new ArgumentException("叶积木参数数量或子节点无效。");
                    var ordered = new EmberConditionParameter[parameters.Length];
                    for (int i = 0; i < ordered.Length; i++)
                    {
                        var spec = leaf.Parameters[i]; var matches = parameters.Where(p => p.Name == spec.Name).ToArray();
                        if (matches.Length != 1 || matches[0].Type != spec.Type) throw new ArgumentException("参数缺失、重复或类型错误：" + spec.Name);
                        var p = matches[0];
                        if (p.Type == EmberConditionValueKind.Number && !float.IsFinite(p.Number)
                            || p.Type == EmberConditionValueKind.Choice && (p.Choice < 0 || p.Choice >= spec.Choices.Count)) throw new ArgumentException("参数超出范围：" + spec.Name);
                        ordered[i] = p;
                    }
                    var frozen = Array.AsReadOnly(ordered);
                    string identity = string.Join(";", ordered.Select(p => p.Name + ":" + (int)p.Type + ":" + p.Number.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
                        + ":" + p.Boolean + ":" + p.Choice + ":" + p.Text?.Length + ":" + p.Text));
                    return new EmberConditionPlan<T>.Node(id, kind, leaf.Describe(frozen), Array.Empty<EmberConditionPlan<T>.Node>(), leaf.Compile(frozen), identity);
                }
                catch (Exception ex) { throw new ArgumentException("条件节点 " + id + "：" + ex.Message, ex); }
            }
            var root = Build(expression.Root, 1);
            if (visited.Count != map.Count) throw new ArgumentException("条件树存在未连接节点。");
            return new EmberConditionPlan<T>(root);
        }
    }

    /// <summary>与编辑资产分离的不可变条件计划；每次查询只读调用方上下文。</summary>
    public sealed class EmberConditionPlan<T> where T : class
    {
        internal sealed class Node
        {
            internal readonly string Id, Kind, Description, Parameters;
            internal readonly Node[] Children;
            internal readonly Func<T, bool, EmberConditionSample> Query;
            internal Node(string id, string kind, string description, Node[] children, Func<T, bool, EmberConditionSample> query, string parameters)
            { Id = id; Kind = kind; Description = description; Children = children; Query = query; Parameters = parameters; }
            internal string Identity => Id.Length + ":" + Id + "/" + Kind + "/" + Parameters.Length + ":" + Parameters + "[" + string.Join(",", Children.Select(c => c.Identity)) + "]";
            internal string Describe(int depth) => new string(' ', depth * 2) + Description + " [" + Id + "]\n" + string.Concat(Children.Select(c => c.Describe(depth + 1)));
            internal void Skip(List<EmberConditionTrace> trace, string reason)
            { if (trace == null) return; trace.Add(new EmberConditionTrace(Id, Description, null, reason)); foreach (var child in Children) child.Skip(trace, reason); }
            internal bool Evaluate(T context, List<EmberConditionTrace> trace)
            {
                int index = trace?.Count ?? 0;
                trace?.Add(null);
                bool value = Kind != "or"; string fact = null;
                if (Query != null) { var sample = Query(context, trace != null); value = sample.Result; fact = sample.Fact; }
                else if (Kind == "not") value = !Children[0].Evaluate(context, trace);
                else if (Kind != "true")
                {
                    bool stopped = false;
                    foreach (var child in Children)
                    {
                        if (stopped) { child.Skip(trace, "前序子项短路"); continue; }
                        value = child.Evaluate(context, trace);
                        stopped = Kind == "and" ? !value : value;
                    }
                }
                if (trace != null) trace[index] = new EmberConditionTrace(Id, Description, value, fact);
                return value;
            }
        }
        private readonly Node _root;
        public string Identity { get; }
        public string Description { get; }
        internal EmberConditionPlan(Node root) { _root = root; Identity = root.Identity; Description = root.Describe(0); }
        public bool Evaluate(T context, List<EmberConditionTrace> trace = null) => _root.Evaluate(context, trace);
        internal void Skip(List<EmberConditionTrace> trace, string reason) => _root.Skip(trace, reason);
    }
}
