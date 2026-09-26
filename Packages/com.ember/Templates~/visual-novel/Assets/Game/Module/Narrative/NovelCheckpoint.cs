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
        public const int CurrentSchemaVersion = 8;

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
        /// <summary>当前曲目键：分段曲目取自 novel_bgm，旧单文件曲目取自 novel_audio。</summary>
        public string BgmKey;
        public bool BgmLoop = true;
        /// <summary>
        /// 曲目当前所在段落（Schema 8 起）。
        /// 旧档没有该字段，反序列化后是 None，恢复时按「旧曲目没有前奏、停在循环段」迁移成 Loop。
        /// </summary>
        public NovelBgmSegment BgmSegment;
        /// <summary>曲目的剧情音量（Schema 8 起）；玩家音量不随存档，始终来自偏好设置。</summary>
        public float BgmVolume = 1;
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
        /// <summary>说话人显示名的变量 ID（表现层字段）。旧档没有这个字段，反序列化后为空，
        /// 解析退回称呼 Key / 角色名回退链，所以不需要迁移、也不推进 SchemaVersion。</summary>
        public string SpeakerVariableId;
        /// <summary>说话人变量的作用域；SpeakerVariableId 为空时无意义。</summary>
        public NovelVariableScope SpeakerVariableScope;
        /// <summary>该句正文的多语言 Key。旧档没有这个字段，反序列化后为空 → 用存下来的 Text，
        /// 所以不需要迁移、也不推进 SchemaVersion（与 SpeakerNameKey 同口径）。</summary>
        public string TextKey;
        /// <summary>该句是否含文字变量绑定。带绑定的句子存的是已替换过变量的文本，
        /// 历史里没有可复用的变量值，因此不再按 Key 重解析。</summary>
        public bool TextHasBindings;
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
                        // 高潮段的语义参数是切入淡入时长；它不在 E3 段里，漏写就会让两种编排指纹相同。
                        if (cmd.Kind == NovelCommandKind.BGMClimax) { w.Write("Bgm1"); w.Write(cmd.Duration); }
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
                        // 自定义节点：只在真的用了才写入，所以既有故事的指纹逐字节不变。
                        // 语义参数住在脚本资产里，因此把「登记键 + 脚本内容」一起写进指纹：
                        // 改脚本参数会让旧档判为不兼容，与「语义变化拒绝旧档、不静默重置」的既有口径一致。
                        // 说话人变量绑定属于表现层，与称呼 Key 同口径，不参与指纹。
                        if (!string.IsNullOrEmpty(cmd.CustomStepId))
                        {
                            w.Write("CustomStep1"); w.Write(cmd.CustomStepId);
                            if (story.CustomSteps.TryGetValue(cmd.CustomStepId, out NovelCustomStepSO script) &&
                                script != null && script.IncludeInFingerprint)
                                w.Write(UnityEngine.JsonUtility.ToJson(script));
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
