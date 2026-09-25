using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Ember.Basic;

namespace Game.Narrative
{
    [Serializable]
    public sealed class NovelCheckpoint
    {
        /// <summary>当前存档格式版本。新增字段时递增，并在 NovelSession / NarrativeRunner 的恢复路径按版本迁移。</summary>
        public const int CurrentSchemaVersion = 7;

        public int SchemaVersion = CurrentSchemaVersion;
        public uint RandomState;
        public float CameraZoom = 1;
        public UnityEngine.Vector2 CameraOffset;
        public string StoryPath, StoryId, Semantics, ChapterId, NodeId, CommandId, LineId;
        public int StoryRevision, TextRevision;
        public NarrativeState Stop;
        public List<NovelVariable> Globals = new(), Locals = new();
        public List<string> Options = new();
        public List<NovelVisualState> Visuals = new();
        public float StageOpacity = 1;
        public UnityEngine.Color CoverColor = UnityEngine.Color.clear;
        public bool CoverWholeReader;
        public NovelEmphasisMode EmphasisMode;
        public string EmphasisInstance;
        public float DimFactor = .55f;
        public List<NovelSavedAction> Actions = new();
        public List<string> Effects = new(); // Legacy reserved field stays empty; Schema 5 uses PersistentEffects.
        public List<NovelEffectState> PersistentEffects = new();
        public List<NovelLoopState> Loops = new();
        public string BgmKey;
        public bool BgmLoop = true;
        public List<NovelHistoryEntry> History = new();
    }

    [Serializable]
    public sealed class NovelVisualState
    {
        public NovelCommandKind Kind;
        public NovelPortraitSlot Slot;
        public string Key;
        public string InstanceId;
        public string CharacterId;
        public int NamedSlot = -1;
        public bool Mirror;
        public int Layer;
        public float Brightness = 1;
        [NonSerialized] internal UnityEngine.Vector2 GestureOffset;
        [NonSerialized] internal float GestureRotation;
        internal NovelVisualState Copy() => (NovelVisualState)MemberwiseClone();
        public float Opacity = 1;
        public UnityEngine.Vector3 Scale = UnityEngine.Vector3.one;
        public float Rotation;
        public UnityEngine.Vector2 Offset; // Normalized displacement from the physical render slot authored in the EUI prefab.
    }

    [Serializable]
    public sealed class NovelHistoryEntry
    {
        public string ChapterId, NodeId, CommandId, LineId, Text, Speaker;
        /// <summary>说话人称呼的多语言 Key（如 speaker.unknown）。旧档没有这个字段，反序列化后为空，
        /// 解析退回角色名回退链，所以不需要迁移、也不推进 SchemaVersion。</summary>
        public string SpeakerNameKey;
        public int TextRevision;
    }

    /// <summary>Only semantic fields participate. Unordered registries and choice display order are canonicalized.</summary>
    public static class NovelCompatibility
    {
        #region 内部方法
        private static void Value(BinaryWriter w, NovelValue v) { w.Write((int)v.Type); w.Write(v.ToString()); }
        private static void Variables(BinaryWriter w, IReadOnlyList<NovelVariable> values)
        {
            w.Write(values.Count);
            foreach (var v in values.OrderBy(v => v.Id, StringComparer.Ordinal)) { w.Write(v.Id); Value(w, v.Value); }
        }
        private static void Routes(BinaryWriter w, IEnumerable<NovelRoute> routes)
        {
            var list = routes.ToArray(); w.Write(list.Length);
            foreach (var r in list)
            {
                w.Write(r.Id); w.Write(r.TargetId ?? ""); w.Write((int)r.Condition.Junction);
                var predicates = r.Condition.Predicates.Select(p =>
                    ((int)p.Scope) + ":" + p.VariableId.Length + ":" + p.VariableId + ":" + (int)p.Comparison + ":" + (int)p.Value.Type + ":" + p.Value)
                    .OrderBy(p => p, StringComparer.Ordinal).ToArray();
                w.Write(predicates.Length); foreach (var p in predicates) w.Write(p);
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [HasGC]
        public static string Fingerprint(NovelStory story)
        {
            using var bytes = new MemoryStream(); using var w = new BinaryWriter(bytes, Encoding.UTF8, true);
            w.Write(story.Id); w.Write(story.EntryChapterId); Variables(w, story.Globals);
            w.Write(story.Chapters.Count);
            foreach (var c in story.Chapters.OrderBy(c => c.Id, StringComparer.Ordinal))
            {
                w.Write(c.Id); w.Write(c.EntryId); Variables(w, c.Variables); w.Write(c.Nodes.Count);
                foreach (var n in c.Nodes.OrderBy(n => n.Id, StringComparer.Ordinal))
                {
                    w.Write(n.Id); w.Write((int)n.Kind); w.Write(n.NextId ?? ""); w.Write(n.EndingId ?? "");
                    Routes(w, n.Kind == NovelNodeKind.Choice ? n.Routes.OrderBy(r => r.Id, StringComparer.Ordinal) : n.Routes);
                    // Command order and priority branch order ARE execution semantics.
                    w.Write(n.Commands.Count);
                    foreach (var cmd in n.Commands)
                    {
                        w.Write(cmd.CommandId); w.Write((int)cmd.Kind); w.Write(cmd.LineId ?? "");
                        w.Write(cmd.CharacterId ?? ""); w.Write(cmd.ResourceKey ?? ""); w.Write(cmd.VariableId ?? "");
                        w.Write((int)cmd.Scope); Value(w, cmd.Value); w.Write(cmd.Duration);
                        w.Write((int)cmd.Slot); w.Write((int)cmd.VisualAction);
                        if (cmd.TextBindings.Count > 0)
                        {
                            w.Write("TextBindings1"); w.Write(cmd.TextBindings.Count);
                            foreach (var binding in cmd.TextBindings)
                            { w.Write(binding.Token); w.Write(binding.VariableId); w.Write((int)binding.Scope); }
                        }
                        if (cmd.Kind == NovelCommandKind.CalculateVariable)
                        {
                            w.Write("Integer1"); w.Write((int)cmd.IntegerOperation); w.Write(cmd.IntegerOperand);
                            w.Write(cmd.OperandVariableId ?? ""); w.Write((int)cmd.OperandScope);
                        }
                        if (cmd.Kind == NovelCommandKind.RandomVariable)
                        { w.Write("Random1"); w.Write(cmd.RandomMin); w.Write(cmd.RandomMax); }
                        // Keep the exact legacy byte stream for stories without E0 fields.
                        if (!string.IsNullOrEmpty(cmd.InstanceId) || cmd.Kind == NovelCommandKind.Opacity || cmd.Kind == NovelCommandKind.WaitActions)
                        {
                            w.Write("E0"); w.Write(cmd.InstanceId ?? ""); w.Write((int)cmd.TargetKind);
                            w.Write(cmd.ActionId ?? ""); w.Write(cmd.Parallel); w.Write(cmd.Delay); w.Write(cmd.Opacity);
                            w.Write(cmd.WaitActions.Count); foreach (var id in cmd.WaitActions) w.Write(id ?? "");
                        }
                        if (cmd.Kind >= NovelCommandKind.Move && cmd.Kind <= NovelCommandKind.Emphasis || cmd.Kind == NovelCommandKind.Character && cmd.PositionMode != NovelPositionMode.Named)
                        {
                            w.Write("E1"); w.Write((int)cmd.PositionMode); w.Write(cmd.Position.x); w.Write(cmd.Position.y);
                            w.Write(cmd.Scale.x); w.Write(cmd.Scale.y); w.Write(cmd.Rotation); w.Write(cmd.Mirror); w.Write(cmd.Layer);
                            w.Write((int)cmd.Ease); w.Write((int)cmd.Gesture); w.Write(cmd.Strength); w.Write(cmd.ExitAfterMove);
                            w.Write((int)cmd.EmphasisMode); w.Write(cmd.DimFactor);
                        }
                        if (NovelMediaRules.IsMedia(cmd.Kind) && (cmd.Kind != NovelCommandKind.BGM || cmd.Volume != 1 || cmd.Duration != 0 || cmd.Delay != 0 || cmd.Parallel || !string.IsNullOrEmpty(cmd.ActionId)))
                        {
                            w.Write("E3"); w.Write(cmd.Persistent); w.Write(cmd.KeepOnSceneChange); w.Write(cmd.BindingId ?? "");
                            w.Write(cmd.Volume); w.Write(cmd.Position.x); w.Write(cmd.Position.y); w.Write(cmd.Scale.x); w.Write(cmd.Scale.y);
                            w.Write(cmd.Layer); w.Write(cmd.ActionId ?? ""); w.Write(cmd.Parallel); w.Write(cmd.Delay); w.Write((int)cmd.Ease);
                        }
                        if (cmd.Kind == NovelCommandKind.DialogueVisibility || cmd.Kind == NovelCommandKind.Say && (cmd.TextMode != NovelTextMode.Dialogue || cmd.TextBeats.Count > 0))
                        {
                            w.Write("E4"); w.Write((int)cmd.TextMode); w.Write(cmd.Kind != NovelCommandKind.DialogueVisibility || cmd.DialogueVisible); w.Write(cmd.TextBeats.Count);
                            foreach (var beat in cmd.TextBeats)
                            { w.Write(beat.At); w.Write(beat.Pause); w.Write(beat.Speed); w.Write(beat.Instant); }
                        }
                        if (cmd.Kind == NovelCommandKind.Say && (cmd.TextMode == NovelTextMode.Title || cmd.TextReveal != NovelTextReveal.Typewriter || cmd.TextSpeedMultiplier != 1))
                        {
                            w.Write("TextEffects"); w.Write((int)cmd.TextReveal); w.Write(cmd.TextFadeDuration);
                            w.Write(cmd.TitleExitDuration); w.Write(cmd.TextSpeedMultiplier); w.Write((int)cmd.TextEase);
                        }
                        if (cmd.Kind == NovelCommandKind.HideAllCharacters) w.Write((int)cmd.Ease);
                        if (cmd.Kind == NovelCommandKind.Camera || cmd.Kind == NovelCommandKind.Wipe)
                        {
                            w.Write("E5"); w.Write(cmd.ActionId ?? ""); w.Write(cmd.Parallel); w.Write(cmd.Delay); w.Write((int)cmd.Ease);
                            w.Write(cmd.CameraZoom); w.Write(cmd.Position.x); w.Write(cmd.Position.y); w.Write((int)cmd.WipeDirection);
                        }
                        if (NovelScreenRules.IsAction(cmd.Kind))
                        {
                            w.Write("E2"); w.Write((int)cmd.TargetKind); w.Write(cmd.InstanceId ?? "");
                            w.Write(cmd.ActionId ?? ""); w.Write(cmd.Parallel); w.Write(cmd.Delay); w.Write((int)cmd.Ease);
                            w.Write(cmd.Strength); w.Write(cmd.Direction.x); w.Write(cmd.Direction.y); w.Write(cmd.Frequency); w.Write(cmd.Decay);
                            w.Write(cmd.Color.r); w.Write(cmd.Color.g); w.Write(cmd.Color.b); w.Write(cmd.Color.a);
                            w.Write(cmd.Opacity); w.Write(cmd.Hold); w.Write(cmd.WholeReader);
                        }
                    }
                }
            }
            w.Write(story.Exits.Count);
            foreach (var e in story.Exits.OrderBy(e => e.ChapterId, StringComparer.Ordinal).ThenBy(e => e.NodeId, StringComparer.Ordinal))
            { w.Write(e.ChapterId); w.Write(e.NodeId); w.Write(e.FallbackChapterId); Routes(w, e.Routes); }
            w.Flush(); using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes.ToArray())).Replace("-", "");
        }
        #endregion
    }
}
