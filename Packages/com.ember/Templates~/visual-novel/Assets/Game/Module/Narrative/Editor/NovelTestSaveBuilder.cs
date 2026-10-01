using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Narrative.Editor
{
    /// <summary>只生成正式剧情的普通停留点，不执行剧情、自定义步骤或修改剧情资产。</summary>
    public static class NovelTestSaveBuilder
    {
        #region 内部参数
        public sealed class Target
        {
            public NovelChapter Chapter { get; }
            public NovelNode Node { get; }
            public int Distance { get; }
            internal Target(NovelChapter chapter, NovelNode node, int distance)
            { Chapter = chapter; Node = node; Distance = distance; }
        }
        private const int MAX_CALL_CHAINS = 128;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static List<NovelVariable> Copy(IReadOnlyList<NovelVariable> variables)
            => variables.Select(v => new NovelVariable(v.Id, v.Value)).ToList();

        private static IEnumerable<(string chapter, string node)> Successors(NovelStory story, NovelChapter chapter, NovelNode node)
        {
            switch (node.Kind)
            {
                case NovelNodeKind.Dialogue:
                case NovelNodeKind.Receiver:
                case NovelNodeKind.FlowStart:
                    yield return (chapter.Id, node.NextId); break;
                case NovelNodeKind.Choice:
                case NovelNodeKind.Branch:
                    foreach (var route in node.Routes) yield return (chapter.Id, route.TargetId);
                    if (node.Kind == NovelNodeKind.Branch) yield return (chapter.Id, node.NextId);
                    break;
                case NovelNodeKind.Jump:
                case NovelNodeKind.FlowCall:
                    yield return (chapter.Id, node.LinkId); break;
                case NovelNodeKind.FlowReturn:
                    foreach (var call in chapter.Nodes.Where(n => n.Kind == NovelNodeKind.FlowCall && n.LinkId == node.ScopeId))
                        foreach (var route in call.Routes.Where(r => r.Id == node.Result))
                            yield return (chapter.Id, route.TargetId);
                    break;
                case NovelNodeKind.ChapterExit:
                    var exit = story.Exits.Single(e => e.ChapterId == chapter.Id && e.NodeId == node.Id);
                    foreach (var id in exit.Routes.Select(r => r.TargetId).Append(exit.FallbackChapterId).Distinct())
                    {
                        var next = story.Chapters.Single(c => c.Id == id);
                        yield return (next.Id, next.EntryId);
                    }
                    break;
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public static bool IsOrdinary(NovelNode node) => node != null &&
            (node.Kind == NovelNodeKind.Choice || node.Kind == NovelNodeKind.Dialogue &&
                !node.Commands.Any(c => c.Kind == NovelCommandKind.CustomStep) &&
                node.Commands.Any(c => c.Kind == NovelCommandKind.Say));

        /// <summary>沿每条前驱路径寻找最近普通节点；循环有界，多来源全部交给用户选择。</summary>
        public static IReadOnlyList<Target> FindTargets(NovelStory story, string chapterId, string nodeId)
        {
            var nodes = new Dictionary<(string, string), Target>();
            foreach (var chapter in story.Chapters)
                foreach (var node in chapter.Nodes) nodes.Add((chapter.Id, node.Id), new Target(chapter, node, 0));
            if (!nodes.ContainsKey((chapterId, nodeId))) throw new InvalidOperationException("所选节点不属于当前剧情。");
            var incoming = nodes.Keys.ToDictionary(k => k, _ => new List<(string, string)>());
            foreach (var pair in nodes)
                foreach (var next in Successors(story, pair.Value.Chapter, pair.Value.Node))
                    if (next.node != null && incoming.TryGetValue(next, out var list)) list.Add(pair.Key);
            var queue = new Queue<((string, string) key, int distance)>();
            var visited = new HashSet<(string, string)>();
            var result = new List<Target>();
            queue.Enqueue(((chapterId, nodeId), 0));
            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                if (!visited.Add(item.key)) continue;
                var target = nodes[item.key];
                if (IsOrdinary(target.Node)) result.Add(new Target(target.Chapter, target.Node, item.distance));
                else foreach (var previous in incoming[item.key]) queue.Enqueue((previous, item.distance + 1));
            }
            return result;
        }

        /// <summary>从主线到当前作用域的真实调用链；不创建虚构调用点或返回地址。</summary>
        public static IReadOnlyList<string[]> CallChains(NovelChapter chapter, NovelNode node)
        {
            var result = new List<string[]>();
            void Visit(string scope, List<string> innerCalls, HashSet<string> seen)
            {
                if (string.IsNullOrEmpty(scope))
                {
                    if (result.Count >= MAX_CALL_CHAINS) throw new InvalidOperationException("调用链超过 128 条，请简化调用关系后生成。");
                    result.Add(innerCalls.AsEnumerable().Reverse().ToArray()); return;
                }
                if (innerCalls.Count >= NarrativeRunner.MAX_FLOW_DEPTH || !seen.Add(scope)) return;
                foreach (var call in chapter.Nodes.Where(n => n.Kind == NovelNodeKind.FlowCall && n.LinkId == scope))
                {
                    innerCalls.Add(call.Id); Visit(call.ScopeId, innerCalls, seen); innerCalls.RemoveAt(innerCalls.Count - 1);
                }
                seen.Remove(scope);
            }
            Visit(node.ScopeId, new List<string>(), new HashSet<string>(StringComparer.Ordinal));
            return result;
        }

        public static NovelCheckpoint Defaults(NovelStory story, Target target, string storyPath, IReadOnlyList<string> calls)
        {
            if (!IsOrdinary(target.Node)) throw new InvalidOperationException("特殊节点不能直接生成存档，请选择前置普通节点。");
            if (string.IsNullOrWhiteSpace(storyPath)) throw new InvalidOperationException("剧情加载路径为空。");
            var checkpoint = new NovelCheckpoint
            {
                StoryPath = storyPath, StoryId = story.Id, StoryRevision = story.Revision,
                Semantics = NovelCompatibility.Fingerprint(story), ChapterId = target.Chapter.Id, NodeId = target.Node.Id,
                RandomState = 0x6D2B79F5u, Globals = Copy(story.Globals), Locals = Copy(target.Chapter.Variables)
            };
            string scope = "";
            foreach (string id in calls ?? Array.Empty<string>())
            {
                var call = target.Chapter.Nodes.Single(n => n.Id == id && n.Kind == NovelNodeKind.FlowCall);
                if (call.ScopeId != scope) throw new InvalidOperationException("调用链作用域不匹配。");
                var flow = target.Chapter.Nodes.Single(n => n.Id == call.LinkId && n.Kind == NovelNodeKind.FlowStart);
                checkpoint.CallStack.Add(new NovelCallFrame { CallId = call.Id, FlowId = flow.Id,
                    Variables = Copy(flow.FlowVariables), Returns = call.Routes.Select(r => new NovelReturnAddress { Result = r.Id, NodeId = r.TargetId }).ToList() });
                scope = flow.Id;
            }
            if (scope != target.Node.ScopeId) throw new InvalidOperationException("请选择目标节点的完整调用链。");
            return checkpoint;
        }

        /// <summary>变量以用户配置为准；不重放所选台词之前的指令，最终由正式读档校验器验收。</summary>
        public static NovelCheckpoint Build(NovelStory story, INarrativeCatalog catalog, NovelCheckpoint settings, string commandId = null)
        {
            if (settings == null || settings.StoryId != story.Id || settings.Semantics != NovelCompatibility.Fingerprint(story))
                throw new InvalidOperationException("剧情已变化，请重新读取节点并配置测试变量。");
            var chapter = story.Chapters.Single(c => c.Id == settings.ChapterId);
            var node = chapter.Nodes.Single(n => n.Id == settings.NodeId);
            if (!IsOrdinary(node)) throw new InvalidOperationException("特殊节点不能直接生成存档，请选择前置普通节点。");
            var checkpoint = JsonUtility.FromJson<NovelCheckpoint>(JsonUtility.ToJson(settings));
            checkpoint.History.Clear(); checkpoint.Options.Clear();
            if (node.Kind == NovelNodeKind.Choice)
            {
                checkpoint.Stop = NarrativeState.AwaitingChoice; checkpoint.CommandId = checkpoint.LineId = null;
                var locals = checkpoint.Locals.ToDictionary(v => v.Id, v => v.Value);
                var globals = checkpoint.Globals.ToDictionary(v => v.Id, v => v.Value);
                var flow = checkpoint.CallStack.LastOrDefault()?.Variables.ToDictionary(v => v.Id, v => v.Value);
                checkpoint.Options = node.Routes.Where(r => r.Condition.Evaluate(locals, globals, flow)).Select(r => r.Id).ToList();
            }
            else
            {
                var command = node.Commands.FirstOrDefault(c => c.Kind == NovelCommandKind.Say && (string.IsNullOrEmpty(commandId) || c.CommandId == commandId));
                if (command == null) throw new InvalidOperationException("停留台词不存在，请重新选择。");
                checkpoint.Stop = NarrativeState.AwaitingAdvance; checkpoint.CommandId = command.CommandId;
                checkpoint.LineId = command.LineId; checkpoint.TextRevision = command.TextRevision;
            }
            var runner = new NarrativeRunner();
            if (!runner.TryRestore(story, catalog, checkpoint, out string error)) throw new InvalidOperationException(error);
            if (checkpoint.Stop == NarrativeState.AwaitingAdvance)
            {
                var command = runner.CurrentCommand;
                checkpoint.History.Add(new NovelHistoryEntry { ChapterId = chapter.Id, NodeId = node.Id,
                    CommandId = command.CommandId, LineId = command.LineId, TextRevision = command.TextRevision,
                    Text = command.Text, Speaker = command.CharacterId, SpeakerNameKey = command.SpeakerNameKey,
                    SpeakerVariableId = command.SpeakerVariableId, SpeakerVariableScope = command.SpeakerVariableScope,
                    FlowSpeaker = command.SpeakerVariableScope == NovelVariableScope.Flow &&
                        runner.TryGetVariable(NovelVariableScope.Flow, command.SpeakerVariableId, out var speaker) ? speaker.ToString() : null,
                    TextKey = command.TextKey, TextHasBindings = command.TextBindings.Count > 0 });
            }
            return checkpoint;
        }
        #endregion
    }
}
