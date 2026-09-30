using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Narrative
{
    internal static class NovelFlowValidation
    {
        #region 外部方法
        internal static void Validate(NovelChapter chapter, List<NarrativeError> errors)
        {
            var nodes = chapter.Nodes.Where(n => n != null && !string.IsNullOrWhiteSpace(n.Id))
                .GroupBy(n => n.Id).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            void Error(string code, string text, NovelNode n) => errors.Add(new NarrativeError(code, text, chapter.Id, n?.Id));
            if (nodes.TryGetValue(chapter.EntryId ?? "", out var entry) && entry.ScopeId != "")
                Error("IllegalEntry", "章节入口必须在主线，流程必须通过调用节点进入", entry);
            foreach (var n in nodes.Values)
            {
                if (n.ScopeId != "" && (!nodes.TryGetValue(n.ScopeId, out var owner) || owner.Kind != NovelNodeKind.FlowStart))
                    Error("MissingScope", "所属流程不存在：" + n.ScopeId, n);
                if (n.Kind == NovelNodeKind.FlowStart && n.ScopeId != n.Id)
                    Error("BadFlowEntry", "流程开始的作用域必须是自身 ID", n);
                if (n.Kind == NovelNodeKind.FlowReturn && (n.ScopeId == "" || string.IsNullOrWhiteSpace(n.Result)))
                    Error("BadReturn", "流程结束必须属于流程且具有结果名", n);
                if (n.ScopeId != "" && (n.Kind == NovelNodeKind.Ending || n.Kind == NovelNodeKind.ChapterExit))
                    Error("IllegalExit", "流程内部必须通过流程结束返回，不能切章或结束剧情", n);
                foreach (var id in DirectTargets(n))
                {
                    if (!nodes.TryGetValue(id ?? "", out var target)) { Error("BadTarget", "目标缺失：" + id, n); continue; }
                    if (target.ScopeId != n.ScopeId || target.Kind == NovelNodeKind.FlowStart || target.Kind == NovelNodeKind.Receiver && n.Kind != NovelNodeKind.Jump)
                        Error("IllegalScope", "非法直连或跨作用域连接：" + id, n);
                    if (n.Kind == NovelNodeKind.Jump && target.Kind != NovelNodeKind.Receiver)
                        Error("BadJump", "跳转目标必须是同作用域的接收点：" + id, n);
                }
                if (n.Kind == NovelNodeKind.FlowCall)
                {
                    if (!nodes.TryGetValue(n.LinkId ?? "", out var flow) || flow.Kind != NovelNodeKind.FlowStart)
                        Error("MissingFlow", "调用目标必须是本章流程开始：" + n.LinkId, n);
                    else
                    {
                        var results = nodes.Values.Where(x => x.Kind == NovelNodeKind.FlowReturn && x.ScopeId == flow.Id).Select(x => x.Result).ToHashSet();
                        if (results.Count == 0) Error("MissingReturn", "流程没有结束节点：" + flow.Id, n);
                        foreach (var result in results)
                            if (!n.Routes.Any(r => r != null && r.Id == result)) Error("MissingResult", "调用点缺少结果出口：" + result, n);
                        foreach (var r in n.Routes.Where(r => r != null))
                            if (!results.Contains(r.Id) || r.Condition.Predicates.Count != 0) Error("BadResult", "无效结果或结果附带条件：" + r.Id, n);
                    }
                    if (n.Routes.Any(r => r == null || string.IsNullOrWhiteSpace(r.Id)) || n.Routes.Where(r => r != null).GroupBy(r => r.Id).Any(g => g.Count() > 1))
                        Error("DuplicateResult", "结果名为空或在调用点重复", n);
                }
                if (n.Kind == NovelNodeKind.FlowStart)
                {
                    var ids = new HashSet<string>();
                    foreach (var v in n.FlowVariables)
                        if (v == null || string.IsNullOrWhiteSpace(v.Id) || !ids.Add(v.Id) || !Enum.IsDefined(typeof(NovelValueType), v.Value.Type))
                            Error("BadFlowVariable", "流程临时变量为空、重复或类型无效", n);
                }
            }
            // Detect recursion on the call graph, including branches never selected in the current run.
            var active = new HashSet<string>(); var done = new HashSet<string>();
            void Visit(string scope, int depth)
            {
                if (depth > NarrativeRunner.MAX_FLOW_DEPTH) { Error("FlowDepth", "静态调用深度超过 16", nodes.GetValueOrDefault(scope)); return; }
                if (!active.Add(scope)) { Error("RecursiveFlow", "禁止递归调用：" + scope, nodes.GetValueOrDefault(scope)); return; }
                foreach (var call in nodes.Values.Where(n => n.ScopeId == scope && n.Kind == NovelNodeKind.FlowCall))
                    if (nodes.ContainsKey(call.LinkId ?? "")) Visit(call.LinkId, depth + 1);
                active.Remove(scope);
            }
            Visit("", 0);
            foreach (var start in nodes.Values.Where(n => n.Kind == NovelNodeKind.FlowStart)) Visit(start.Id, 1);
            // Legacy graphs retain their established runtime StepLimit diagnostic.
            if (errors.Count != 0 || !nodes.Values.Any(n => n.Kind >= NovelNodeKind.Jump)) return;
            // A conservative graph of only guaranteed-immediate nodes. Conditional loops remain guarded at runtime.
            IEnumerable<string> Immediate(NovelNode n)
            {
                if (n.Kind == NovelNodeKind.Choice || n.Kind == NovelNodeKind.FlowCall || n.Kind == NovelNodeKind.FlowReturn ||
                    n.Kind == NovelNodeKind.Branch || n.Kind == NovelNodeKind.Dialogue && n.Commands.Any(c => c == null ||
                        c.Kind != NovelCommandKind.SetVariable && c.Kind != NovelCommandKind.CalculateVariable && c.Kind != NovelCommandKind.RandomVariable && !(c.Kind == NovelCommandKind.Wait && c.Duration == 0)))
                    return Array.Empty<string>();
                return DirectTargets(n);
            }
            active.Clear();
            void Cycle(string id)
            {
                if (!nodes.ContainsKey(id) || done.Contains(id)) return;
                if (!active.Add(id)) { Error("ImmediateCycle", "没有等待的即时执行闭环", nodes[id]); return; }
                foreach (var next in Immediate(nodes[id])) if (next != null) Cycle(next);
                active.Remove(id); done.Add(id);
            }
            foreach (var id in nodes.Keys) Cycle(id);
        }

        internal static IEnumerable<string> DirectTargets(NovelNode n)
        {
            if (n.Kind == NovelNodeKind.Jump) yield return n.LinkId;
            if (n.Kind == NovelNodeKind.Dialogue || n.Kind == NovelNodeKind.Branch || n.Kind == NovelNodeKind.Receiver || n.Kind == NovelNodeKind.FlowStart) yield return n.NextId;
            foreach (var r in n.Routes) if (r != null) yield return r.TargetId;
        }
        #endregion
    }
}
