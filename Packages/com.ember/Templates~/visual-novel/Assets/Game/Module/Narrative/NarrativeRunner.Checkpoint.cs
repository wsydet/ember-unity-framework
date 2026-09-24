using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Game.Narrative
{
    public sealed partial class NarrativeRunner
    {
        #region 内部方法
        private Dictionary<string, NovelValue> ReadVariables(List<NovelVariable> saved, IReadOnlyList<NovelVariable> definitions)
        {
            if (saved == null || saved.Count != definitions.Count) throw new InvalidOperationException("存档变量集合不完整");
            var values = new Dictionary<string, NovelValue>(StringComparer.Ordinal);
            foreach (var v in saved)
            {
                if (v == null || string.IsNullOrEmpty(v.Id) || !definitions.Any(d => d.Id == v.Id && d.Value.Type == v.Value.Type))
                    throw new InvalidOperationException("存档变量不存在或类型不兼容");
                values.Add(v.Id, v.Value);
            }
            return values;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public bool TryCapture(out NovelCheckpoint checkpoint, out string error)
        {
            checkpoint = null; error = null;
            if (_busy || _story == null || (_state != NarrativeState.AwaitingAdvance && _state != NarrativeState.AwaitingChoice))
            { error = "请等待当前句完整显示或选项准备完成后保存"; return false; }
            checkpoint = new NovelCheckpoint
            {
                RandomState = _randomState,
                StoryId = _story.Id, StoryRevision = _story.Revision, Semantics = NovelCompatibility.Fingerprint(_story),
                ChapterId = _chapter.Id, NodeId = _node.Id, CommandId = CurrentCommand?.CommandId,
                LineId = CurrentCommand?.LineId, TextRevision = CurrentCommand?.TextRevision ?? 0, Stop = _state,
                Globals = _globals.Select(v => new NovelVariable(v.Key, v.Value)).ToList(),
                Locals = _variables.Select(v => new NovelVariable(v.Key, v.Value)).ToList(),
                Options = _options.Select(o => o.Id).ToList()
            };
            return true;
        }

        /// <summary>Restore directly at the stop, never Start/EnterChapter/Drain. Candidate runners only.</summary>
        public bool TryRestore(NovelStory story, INarrativeCatalog catalog, NovelCheckpoint checkpoint, out string error)
        {
            error = null;
            if (_busy || _state != NarrativeState.Idle) { error = "恢复必须使用独立的新运行器"; return false; }
            try
            {
                if (checkpoint == null || (checkpoint.SchemaVersion < 1 || checkpoint.SchemaVersion > 7)) throw new InvalidOperationException("未知存档格式版本");
                var issues = NarrativeStoryValidator.Validate(story, catalog);
                if (issues.Count > 0) throw new InvalidOperationException(issues[0].ToString());
                if (checkpoint.StoryId != story.Id || checkpoint.Semantics != NovelCompatibility.Fingerprint(story))
                    throw new InvalidOperationException("剧情语义已变化，存档不兼容；文件已保留");
                if (checkpoint.SchemaVersion >= 7 && checkpoint.RandomState == 0)
                    throw new InvalidOperationException("存档随机状态无效");
                if (checkpoint.SchemaVersion < 7 && story.Chapters.Any(c => c.Nodes.Any(n => n.Commands.Any(x => x.Kind == NovelCommandKind.RandomVariable))))
                    throw new InvalidOperationException("旧存档缺少随机状态，不能恢复随机剧情");
                var chapter = story.Chapters.Single(c => c.Id == checkpoint.ChapterId);
                var node = chapter.Nodes.Single(n => n.Id == checkpoint.NodeId);
                var globals = ReadVariables(checkpoint.Globals, story.Globals);
                var locals = ReadVariables(checkpoint.Locals, chapter.Variables);
                int index = 0; var options = new List<NovelRoute>();
                if (checkpoint.Stop == NarrativeState.AwaitingAdvance && node.Kind == NovelNodeKind.Dialogue)
                {
                    index = node.Commands.ToList().FindIndex(c => c.CommandId == checkpoint.CommandId);
                    if (index < 0 || node.Commands[index].Kind != NovelCommandKind.Say || node.Commands[index].LineId != checkpoint.LineId)
                        throw new InvalidOperationException("存档台词标识不存在");
                }
                else if (checkpoint.Stop == NarrativeState.AwaitingChoice && node.Kind == NovelNodeKind.Choice)
                {
                    options.AddRange(node.Routes.Where(r => r.Condition.Evaluate(locals, globals)));
                    if (options.Count == 0 || checkpoint.Options == null || checkpoint.Options.Count != options.Count ||
                        !new HashSet<string>(checkpoint.Options).SetEquals(options.Select(o => o.Id)))
                        throw new InvalidOperationException("存档合法选项不一致");
                }
                else throw new InvalidOperationException("存档不是合法稳定点");
                return Mutate(() =>
                {
                    _generation = Interlocked.Increment(ref _nextGeneration); _story = story; _chapter = chapter; _node = node;
                    foreach (var c in story.Chapters) _chapters.Add(c.Id, c);
                    foreach (var e in story.Exits) _exits.Add((e.ChapterId, e.NodeId), e);
                    foreach (var n in chapter.Nodes) _nodes.Add(n.Id, n);
                    foreach (var v in globals) _globals.Add(v.Key, v.Value);
                    foreach (var v in locals) _variables.Add(v.Key, v.Value);
                    _randomState = checkpoint.SchemaVersion >= 7 ? checkpoint.RandomState : _initialRandomState;
                    _commandIndex = index; _options.AddRange(options); _state = checkpoint.Stop;
                    _wait = _state == NarrativeState.AwaitingAdvance ? NarrativeWait.Advance : NarrativeWait.Choice;
                });
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }
        #endregion
    }
}
