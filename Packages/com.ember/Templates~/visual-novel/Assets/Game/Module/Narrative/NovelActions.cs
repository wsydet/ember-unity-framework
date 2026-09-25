using System;
using Ember.Basic;

namespace Game.Narrative
{
    // Mask is a singleton; E3 effects have independent instance IDs and lifetimes.
    public enum NovelTargetKind { Stage, Background, Character, Mask, Effect }
    public enum NovelActionStatus { Running, Completed, Cancelled }

    /// <summary>Session-owned identity. No completion callbacks may outlive a page.</summary>
    public sealed class NovelActionHandle
    {
        #region 内部参数
        public long Generation { get; internal set; }
        public long Sequence { get; internal set; }
        public string Id { get; internal set; }
        public NovelTargetKind TargetKind { get; internal set; }
        public string TargetId { get; internal set; }
        public string Property { get; internal set; } = "Opacity";
        internal NovelCommandKind Kind = NovelCommandKind.Opacity;
        internal UnityEngine.Vector2 VectorFrom, VectorTo;
        internal NovelEase Ease;
        internal NovelWipeDirection WipeDirection;
        internal NovelGesture Gesture;
        internal float Strength;
        internal bool ExitAfterMove;
        internal UnityEngine.Color ColorFrom, ColorTo;
        internal float Frequency, Hold, FadeDuration;
        internal bool Decay, WholeReader, PreviousWholeReader;
        internal string OldKey, NewKey;
        internal NovelPortraitSlot Slot;
        public NovelActionStatus Status { get; internal set; }
        public float Progress { get; internal set; }
        internal float From, To, Elapsed, Duration, Delay;
        public bool IsFinished => Status != NovelActionStatus.Running;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [NoGC]
        public static bool ValidTime(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;

        /// <summary>
        /// 句柄身份的唯一真源：填充 ActionId 时用它，留空时回退到步骤 ID。
        /// 步骤 ID 已由校验器保证章节内唯一，因此回退不会破坏唯一性。
        /// 只在运行期解析句柄；存档指纹继续写原始字段，老剧情指纹逐字节不变。
        /// </summary>
        [NoGC]
        public static string ResolveId(NovelCommand command) =>
            string.IsNullOrWhiteSpace(command.ActionId) ? command.CommandId : command.ActionId;
        #endregion
    }

    public interface INovelOpacityView
    {
        void SetOpacity(NovelTargetKind kind, NovelPortraitSlot slot, float opacity);
    }

    [Serializable]
    public sealed class NovelSavedAction
    {
        public string Id;
        public NovelActionStatus Status;
    }
}
