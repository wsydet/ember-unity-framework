using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Game.Narrative
{
    public enum NarrativeState { Idle, Preparing, Executing, Revealing, AwaitingAdvance, AwaitingChoice, Ended, Faulted, Cancelled, Restoring }
    [Flags]
    public enum NarrativeWait { None = 0, Text = 1, Advance = 2, Choice = 4, Timer = 8, Presentation = 16, Resource = 32, Transition = 64, Voice = 128, Table = 256, Page = 512, Actions = 1024 }
    public enum NarrativeReadMode { Manual, Auto, Skip }

    public sealed class NarrativeError
    {
        #region 内部参数
        public string Code { get; }
        public string Message { get; }
        public string ChapterId { get; }
        public string NodeId { get; }
        public string CommandId { get; }
        public string OptionId { get; }
        public string ResourceKey { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public NarrativeError(string code, string message, string chapterId, string nodeId = null,
            string commandId = null, string optionId = null, string resourceKey = null)
        {
            Code = code; Message = message; ChapterId = chapterId; NodeId = nodeId;
            CommandId = commandId; OptionId = optionId; ResourceKey = resourceKey;
        }
        public override string ToString() => $"{Code}: {Message} [{ChapterId}/{NodeId}/{CommandId ?? OptionId}] {ResourceKey}";
        #endregion
    }

    /// <summary>通知只是状态变更信号。窗口读取 Snapshot；不可通过快照修改会话。</summary>
    public interface INarrativeDiagnostics
    {
        NarrativeSnapshot Snapshot { get; }
        event Action Changed;
    }

    public sealed class NarrativeSnapshot
    {
        #region 内部参数
        public long SessionGeneration { get; }
        public long PositionVersion { get; }
        public string ChapterId { get; }
        public string NodeId { get; }
        public string CommandId { get; }
        public NarrativeState State { get; }
        public NarrativeWait Wait { get; }
        public NarrativeReadMode ReadMode { get; }
        public IReadOnlyList<string> PauseReasons { get; }
        public IReadOnlyDictionary<string, NovelValue> Variables { get; }
        public IReadOnlyDictionary<string, NovelValue> GlobalVariables { get; }
        public string StoryId { get; }
        public IReadOnlyList<NovelRoute> Options { get; }
        public NarrativeError Error { get; }
        public string EndingId { get; }
        public IReadOnlyList<string> Actions { get; } = Array.Empty<string>();
        public string WaitingActions { get; }
        public bool HasActiveSession => State != NarrativeState.Idle && State != NarrativeState.Cancelled
            && State != NarrativeState.Ended && State != NarrativeState.Faulted;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal NarrativeSnapshot(long generation, long positionVersion, string chapterId, string nodeId,
            string commandId, NarrativeState state, NarrativeWait wait, IEnumerable<string> pauseReasons,
            IDictionary<string, NovelValue> variables, IList<NovelRoute> options, NarrativeError error, string endingId,
            IDictionary<string, NovelValue> globals = null, string storyId = null)
        {
            SessionGeneration = generation; PositionVersion = positionVersion; ChapterId = chapterId;
            NodeId = nodeId; CommandId = commandId; State = state; Wait = wait;
            var reasons = new List<string>(pauseReasons); reasons.Sort(StringComparer.Ordinal);
            PauseReasons = reasons.AsReadOnly();
            Variables = new ReadOnlyDictionary<string, NovelValue>(new Dictionary<string, NovelValue>(variables, StringComparer.Ordinal));
            GlobalVariables = new ReadOnlyDictionary<string, NovelValue>(globals == null ? new Dictionary<string, NovelValue>() : new Dictionary<string, NovelValue>(globals, StringComparer.Ordinal));
            StoryId = storyId;
            Options = new List<NovelRoute>(options).AsReadOnly(); Error = error; EndingId = endingId;
        }
        internal NarrativeSnapshot(NarrativeSnapshot source, NarrativeReadMode mode, NarrativeWait wait, IReadOnlyList<string> actions = null, string waitingActions = null)
        {
            SessionGeneration = source.SessionGeneration; PositionVersion = source.PositionVersion;
            ChapterId = source.ChapterId; NodeId = source.NodeId; CommandId = source.CommandId;
            Actions = actions ?? Array.Empty<string>(); WaitingActions = waitingActions;
            State = source.State; Wait = wait; ReadMode = mode; PauseReasons = source.PauseReasons;
            Variables = source.Variables; GlobalVariables = source.GlobalVariables; StoryId = source.StoryId;
            Options = source.Options; Error = source.Error; EndingId = source.EndingId;
        }
        #endregion
    }
}
