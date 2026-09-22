using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Basic;

namespace Game.Narrative
{
    public sealed class NovelChapterExit
    {
        #region 内部参数
        public string ChapterId { get; }
        public string NodeId { get; }
        public string FallbackChapterId { get; }
        public IReadOnlyList<NovelRoute> Routes { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelChapterExit(string chapterId, string nodeId, string fallbackChapterId, IList<NovelRoute> routes = null)
        {
            ChapterId = chapterId; NodeId = nodeId; FallbackChapterId = fallbackChapterId;
            Routes = new List<NovelRoute>(routes ?? Array.Empty<NovelRoute>()).AsReadOnly();
        }
        #endregion
    }

    public sealed class NovelStory
    {
        #region 内部参数
        public string Id { get; }
        public int Revision { get; }
        public string EntryChapterId { get; }
        public IReadOnlyList<NovelChapter> Chapters { get; }
        public IReadOnlyList<NovelVariable> Globals { get; }
        public IReadOnlyList<NovelChapterExit> Exits { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NovelStory(string id, int revision, string entryChapterId, IList<NovelChapter> chapters,
            IList<NovelVariable> globals = null, IList<NovelChapterExit> exits = null)
        {
            Id = id; Revision = revision; EntryChapterId = entryChapterId;
            Chapters = new List<NovelChapter>(chapters ?? Array.Empty<NovelChapter>()).AsReadOnly();
            Globals = new List<NovelVariable>(globals ?? Array.Empty<NovelVariable>()).AsReadOnly();
            Exits = new List<NovelChapterExit>(exits ?? Array.Empty<NovelChapterExit>()).AsReadOnly();
        }
        #endregion
    }

    public static class NarrativeStoryValidator
    {
        #region 外部方法
        [HasGC]
        public static IReadOnlyList<NarrativeError> Validate(NovelStory story, INarrativeCatalog catalog)
        {
            var errors = new List<NarrativeError>();
            void Error(string text, string chapter = null, string node = null) =>
                errors.Add(new NarrativeError("BadStory", text, chapter, node));
            if (story == null) { Error("剧情为空"); return errors; }
            if (string.IsNullOrWhiteSpace(story.Id) || story.Revision < 1) Error("剧情 ID / 修订无效");
            var globals = new Dictionary<string, NovelValue>(StringComparer.Ordinal);
            foreach (var v in story.Globals)
                if (v == null || string.IsNullOrWhiteSpace(v.Id) || globals.ContainsKey(v.Id) || !Enum.IsDefined(typeof(NovelValueType), v.Value.Type))
                    Error("全局变量为空、重复或类型无效");
                else globals.Add(v.Id, v.Value);
            var chapters = new Dictionary<string, NovelChapter>(StringComparer.Ordinal);
            foreach (var c in story.Chapters)
            {
                if (c == null || string.IsNullOrWhiteSpace(c.Id) || chapters.ContainsKey(c.Id)) { Error("章节为空或 ID 重复", c?.Id); continue; }
                chapters.Add(c.Id, c); errors.AddRange(NarrativeValidator.Validate(c, catalog, globals, true));
            }
            if (story.EntryChapterId == null || !chapters.ContainsKey(story.EntryChapterId)) Error("剧情入口章节未登记");
            var exits = new HashSet<(string, string)>();
            foreach (var exit in story.Exits)
            {
                if (exit == null) { Error("章节出口配置为空"); continue; }
                if (exit.ChapterId == null || !chapters.TryGetValue(exit.ChapterId, out var chapter) ||
                    !chapter.Nodes.Any(n => n != null && n.Id == exit.NodeId && n.Kind == NovelNodeKind.ChapterExit))
                { Error("出口不属于已登记章节", exit.ChapterId, exit.NodeId); continue; }
                if (!exits.Add((exit.ChapterId, exit.NodeId))) Error("出口配置重复", exit.ChapterId, exit.NodeId);
                if (exit.FallbackChapterId == null || !chapters.ContainsKey(exit.FallbackChapterId)) Error("出口必须连接兜底章节", exit.ChapterId, exit.NodeId);
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var route in exit.Routes)
                {
                    if (route == null || string.IsNullOrWhiteSpace(route.Id) || !ids.Add(route.Id) ||
                        route.TargetId == null || !chapters.ContainsKey(route.TargetId))
                    { Error("章节路线 ID / 目标无效或重复", exit.ChapterId, exit.NodeId); continue; }
                    if (!Enum.IsDefined(typeof(NovelJunction), route.Condition.Junction)) Error("条件组合无效", exit.ChapterId, exit.NodeId);
                    foreach (var p in route.Condition.Predicates)
                        if (p == null || p.Scope != NovelVariableScope.Global || string.IsNullOrWhiteSpace(p.VariableId) ||
                            !globals.TryGetValue(p.VariableId, out var value) || value.Type != p.Value.Type ||
                            !Enum.IsDefined(typeof(NovelComparison), p.Comparison) ||
                            (value.Type != NovelValueType.Int && p.Comparison != NovelComparison.Equal && p.Comparison != NovelComparison.NotEqual))
                            Error("章节连线只能使用已声明的全局变量与合法比较", exit.ChapterId, exit.NodeId);
                }
            }
            foreach (var c in chapters.Values)
                foreach (var n in c.Nodes)
                    if (n?.Kind == NovelNodeKind.ChapterExit && !exits.Contains((c.Id, n.Id))) Error("章节出口尚未在总览配置", c.Id, n.Id);
            return errors.AsReadOnly();
        }
        #endregion
    }
}
