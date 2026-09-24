using System;
using System.Collections.Generic;
using System.Threading;

namespace Game.Narrative
{
    /// <summary>单线程、显式驱动的运行器。不访问 Unity、页面、资源或 SO，不持有全局 Module。</summary>
    public sealed partial class NarrativeRunner : INarrativeDiagnostics
    {
        #region 内部参数
        private static long _nextGeneration;
        private readonly int _maxImmediateSteps;
        private readonly Dictionary<string, NovelNode> _nodes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, NovelValue> _variables = new(StringComparer.Ordinal);
        private readonly Dictionary<string, NovelValue> _globals = new(StringComparer.Ordinal);
        private readonly Dictionary<string, NovelChapter> _chapters = new(StringComparer.Ordinal);
        private readonly Dictionary<(string, string), NovelChapterExit> _exits = new();
        private NovelStory _story;
        private readonly uint _initialRandomState;
        private uint _randomState;
        private readonly HashSet<string> _pauses = new(StringComparer.Ordinal);
        private readonly List<NovelRoute> _options = new();
        private NovelChapter _chapter;
        private NovelNode _node;
        private int _commandIndex;
        private long _generation;
        private long _positionVersion;
        private long _lastInputFrame = -1;
        private long _lastTickFrame = -1;
        private float _remainingWait;
        private bool _busy;
        private NarrativeState _state = NarrativeState.Idle;
        private NarrativeWait _wait;
        private NarrativeError _error;
        private string _endingId;
        public string LastObserverError { get; private set; }
        public long AutoSaveRevision { get; private set; }
        public event Action Changed;
        private NovelCommand _resolvedSource, _resolvedCommand;
        public NovelCommand CurrentCommand
        {
            get
            {
                var raw = _node?.Kind == NovelNodeKind.Dialogue && _commandIndex >= 0 && _commandIndex < _node.Commands.Count ? _node.Commands[_commandIndex] : null;
                if (raw == null || raw.Kind != NovelCommandKind.Say || raw.TextBindings.Count == 0) return raw;
                if (_resolvedSource != raw)
                {
                    _resolvedCommand = raw.WithResolvedText(NovelTextBindings.Resolve(raw, _variables, _globals));
                    _resolvedSource = raw;
                }
                return _resolvedCommand;
            }
        }
        public NarrativeSnapshot Snapshot => new(_generation, _positionVersion, _chapter?.Id, _node?.Id,
            CurrentCommand?.CommandId, _state, _wait, _pauses, _variables, _options, _error, _endingId, _globals, _story?.Id);
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private bool Matches(long generation, long positionVersion)
            => !_busy && generation == _generation && positionVersion == _positionVersion &&
               _pauses.Count == 0 && _state != NarrativeState.Cancelled && _state != NarrativeState.Faulted && _state != NarrativeState.Ended;

        private void Publish(bool positionChanged)
        {
            if (positionChanged) _positionVersion++;
            // 观察者抛错或重入不得改变剧情结果。记录错误供宿主诊断。
            if (Changed == null) return;
            foreach (Action observer in Changed.GetInvocationList())
                try { observer(); }
                catch (Exception ex) { LastObserverError = ex.ToString(); }
        }

        private void Fault(string code, string message, string optionId = null, string resourceKey = null)
        {
            _error = new NarrativeError(code, message, _chapter?.Id, _node?.Id,
                CurrentCommand?.CommandId, optionId, resourceKey);
            _state = NarrativeState.Faulted; _wait = NarrativeWait.None; _options.Clear();
        }

        private void Enter(string nodeId)
        {
            if (nodeId == null || !_nodes.TryGetValue(nodeId, out NovelNode next))
            { Fault("BadTarget", "目标节点不存在"); return; }
            _resolvedSource = _resolvedCommand = null;
            _node = next; _commandIndex = 0; _options.Clear();
            _state = NarrativeState.Executing; _wait = NarrativeWait.None;
        }

        private void Drain()
        {
            int steps = 0;
            while (_state == NarrativeState.Executing && _wait == NarrativeWait.None)
            {
                if (++steps > _maxImmediateSteps) { Fault("StepLimit", "连续即时指令与节点跳转超过上限"); return; }
                switch (_node.Kind)
                {
                    case NovelNodeKind.ChapterExit:
                        if (!_exits.TryGetValue((_chapter.Id, _node.Id), out var exit)) { Fault("BadExit", "章节出口未配置"); return; }
                        string nextChapter = exit.FallbackChapterId;
                        foreach (var route in exit.Routes)
                            if (route.Condition.Evaluate(_variables, _globals)) { nextChapter = route.TargetId; break; }
                        EnterChapter(_chapters[nextChapter]); break;
                    case NovelNodeKind.Ending:
                        _endingId = _node.EndingId; _state = NarrativeState.Ended; return;
                    case NovelNodeKind.Choice:
                        foreach (NovelRoute route in _node.Routes)
                            if (route.Condition.Evaluate(_variables, _globals)) _options.Add(route);
                        if (_options.Count == 0) { Fault("ZeroOptions", "筛选后没有合法选项"); return; }
                        _state = NarrativeState.AwaitingChoice; _wait = NarrativeWait.Choice; return;
                    case NovelNodeKind.Branch:
                        string target = _node.NextId;
                        foreach (NovelRoute route in _node.Routes)
                            if (route.Condition.Evaluate(_variables, _globals)) { target = route.TargetId; break; }
                        Enter(target); break;
                    case NovelNodeKind.Dialogue:
                        NovelCommand command = CurrentCommand;
                        if (command == null) { Enter(_node.NextId); break; }
                        switch (command.Kind)
                        {
                            case NovelCommandKind.CalculateVariable:
                            case NovelCommandKind.RandomVariable:
                                if (!ExecuteVariable(command)) return;
                                _commandIndex++; break;
                            case NovelCommandKind.SetVariable:
                                (command.Scope == NovelVariableScope.Global ? _globals : _variables)[command.VariableId] = command.Value; _commandIndex++; break;
                            case NovelCommandKind.Say:
                                _state = NarrativeState.Revealing; _wait = NarrativeWait.Text; return;
                            case NovelCommandKind.Wait:
                                if (command.Duration == 0) { _commandIndex++; break; }
                                _remainingWait = command.Duration; _wait = NarrativeWait.Timer; return;
                            default:
                                _wait = NarrativeWait.Presentation; return;
                        }
                        break;
                    default: Fault("UnknownNode", "未知节点类型"); return;
                }
            }
        }

        private bool Mutate(Action action, bool positionChanged = true)
        {
            if (_busy) return false;
            _busy = true;
            try { action(); Publish(positionChanged); return true; }
            finally { _busy = false; }
        }
        private void EnterChapter(NovelChapter chapter, string entryNodeId = null)
        {
            AutoSaveRevision++;
            _chapter = chapter; _nodes.Clear(); _variables.Clear();
            foreach (var node in chapter.Nodes) _nodes.Add(node.Id, node);
            foreach (var variable in chapter.Variables) _variables.Add(variable.Id, variable.Value);
            Enter(entryNodeId ?? chapter.EntryId);
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NarrativeRunner(int maxImmediateSteps = 1024, uint randomSeed = 0)
        {
            if (maxImmediateSteps < 1) throw new ArgumentOutOfRangeException(nameof(maxImmediateSteps));
            _maxImmediateSteps = maxImmediateSteps;
            _initialRandomState = randomSeed == 0 ? unchecked((uint)Guid.NewGuid().GetHashCode()) : randomSeed;
            if (_initialRandomState == 0) _initialRandomState = 1;
            _randomState = _initialRandomState;
        }

        public bool Start(NovelChapter chapter, INarrativeCatalog catalog)
            => StartCore(chapter, null, catalog);

        public bool StartStory(NovelStory story, INarrativeCatalog catalog, string chapterId = null, string nodeId = null)
            => StartCore(null, story, catalog, chapterId, nodeId);

        private bool StartCore(NovelChapter chapter, NovelStory story, INarrativeCatalog catalog, string chapterId = null, string nodeId = null)
        {
            if (_busy) return false;
            IReadOnlyList<NarrativeError> errors = story == null ? NarrativeValidator.Validate(chapter, catalog) : NarrativeStoryValidator.Validate(story, catalog);
            Mutate(() =>
            {
                _generation = Interlocked.Increment(ref _nextGeneration); _chapter = chapter; _node = null;
                _randomState = _initialRandomState;
                _story = story; _globals.Clear(); _chapters.Clear(); _exits.Clear();
                _nodes.Clear(); _variables.Clear(); _options.Clear(); _pauses.Clear();
                _error = null; _endingId = null; _commandIndex = 0; _remainingWait = 0;
                _lastInputFrame = -1; _lastTickFrame = -1; LastObserverError = null;
                _state = NarrativeState.Preparing; _wait = NarrativeWait.None;
                if (errors.Count > 0) { _error = errors[0]; _state = NarrativeState.Faulted; return; }
                if (story != null)
                {
                    foreach (var item in story.Chapters) _chapters.Add(item.Id, item);
                    foreach (var item in story.Exits) _exits.Add((item.ChapterId, item.NodeId), item);
                    foreach (var variable in story.Globals) _globals.Add(variable.Id, variable.Value);
                    if (!_chapters.TryGetValue(chapterId ?? story.EntryChapterId, out chapter))
                    { _error = new NarrativeError("BadEntry", "指定章节未登记", chapterId, nodeId); _state = NarrativeState.Faulted; return; }
                    if (nodeId != null)
                    {
                        bool found = false;
                        foreach (var node in chapter.Nodes) if (node.Id == nodeId) { found = true; break; }
                        if (!found) { _error = new NarrativeError("BadEntry", "指定入口不属于目标章节", chapter.Id, nodeId); _state = NarrativeState.Faulted; return; }
                    }
                }
                EnterChapter(chapter, nodeId); Drain();
            });
            return _state != NarrativeState.Faulted;
        }

        /// <summary>inputFrame 由宿主传入帧号；位置版本与代次拒绝双击、重复选择和旧回调。</summary>
        public bool Advance(long generation, long positionVersion, long inputFrame)
        {
            if (!Matches(generation, positionVersion) || inputFrame < 0 || inputFrame <= _lastInputFrame ||
                (_state != NarrativeState.Revealing && _state != NarrativeState.AwaitingAdvance)) return false;
            return Mutate(() =>
            {
                _lastInputFrame = inputFrame;
                if (_state == NarrativeState.Revealing) { _state = NarrativeState.AwaitingAdvance; _wait = NarrativeWait.Advance; }
                else { _commandIndex++; _state = NarrativeState.Executing; _wait = NarrativeWait.None; Drain(); }
            });
        }

        public bool CompleteReveal(long generation, long positionVersion)
        {
            if (!Matches(generation, positionVersion) || _state != NarrativeState.Revealing) return false;
            return Mutate(() => { _state = NarrativeState.AwaitingAdvance; _wait = NarrativeWait.Advance; });
        }

        public bool Choose(long generation, long positionVersion, string optionId, long inputFrame)
        {
            if (!Matches(generation, positionVersion) || _state != NarrativeState.AwaitingChoice ||
                inputFrame < 0 || inputFrame <= _lastInputFrame) return false;
            NovelRoute selected = _options.Find(option => option.Id == optionId);
            if (selected == null) return false;
            return Mutate(() => { _lastInputFrame = inputFrame; AutoSaveRevision++; Enter(selected.TargetId); Drain(); });
        }

        public bool Tick(long generation, float deltaSeconds, long frame)
        {
            if (_busy || generation != _generation || _pauses.Count != 0 || _wait != NarrativeWait.Timer ||
                frame < 0 || frame <= _lastTickFrame || deltaSeconds < 0 || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds)) return false;
            return Mutate(() =>
            {
                _lastTickFrame = frame; _remainingWait -= deltaSeconds;
                if (_remainingWait <= 0) { _commandIndex++; _wait = NarrativeWait.None; Drain(); }
            });
        }

        /// <summary>M2 演出层调用；M1 不把尚未执行的资源/音频操作视作已经完成。</summary>
        public bool CompletePresentation(long generation, long positionVersion, string failure = null)
        {
            if (!Matches(generation, positionVersion) || (_wait & NarrativeWait.Presentation) == 0) return false;
            return Mutate(() =>
            {
                if (failure != null) Fault("PresentationFailed", failure, resourceKey: CurrentCommand?.ResourceKey);
                else { _commandIndex++; _wait = NarrativeWait.None; Drain(); }
            });
        }

        public bool SetPresentationWait(long generation, long positionVersion, NarrativeWait reasons)
        {
            const NarrativeWait allowed = NarrativeWait.Resource | NarrativeWait.Transition | NarrativeWait.Voice | NarrativeWait.Actions;
            if (!Matches(generation, positionVersion) || (_wait & NarrativeWait.Presentation) == 0 || (reasons & ~allowed) != 0) return false;
            return Mutate(() => _wait = NarrativeWait.Presentation | reasons, false);
        }

        /// <summary>宿主的页面、资源或音频失败；允许在暂停中记录故障，不伪装为正常结局。</summary>
        public bool ReportFailure(long generation, string message)
        {
            if (_busy || generation != _generation || !Snapshot.HasActiveSession) return false;
            return Mutate(() => Fault("SessionFailed", message, resourceKey: CurrentCommand?.ResourceKey));
        }

        /// <summary>每个暂停拥有者使用独立 token；重复添加同一 token 幂等。</summary>
        public bool Pause(string token)
        {
            if (_busy || string.IsNullOrWhiteSpace(token) || _pauses.Contains(token) || !Snapshot.HasActiveSession) return false;
            return Mutate(() => _pauses.Add(token), false);
        }

        public bool Resume(string token)
        {
            if (_busy || token == null || !_pauses.Contains(token)) return false;
            return Mutate(() => _pauses.Remove(token), false);
        }

        public bool Cancel()
        {
            if (_busy || _state == NarrativeState.Cancelled || _state == NarrativeState.Idle) return false;
            return Mutate(() => { _state = NarrativeState.Cancelled; _wait = NarrativeWait.None; _pauses.Clear(); _options.Clear(); });
        }
        #endregion
    }
}
