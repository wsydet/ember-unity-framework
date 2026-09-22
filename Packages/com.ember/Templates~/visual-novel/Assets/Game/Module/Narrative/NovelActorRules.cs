using System;
using Ember.Basic;
using UnityEngine;

namespace Game.Narrative
{
    public enum NovelPositionMode { Named, Normalized }
    public enum NovelEase { Linear, SmoothStep, EaseIn, EaseOut }
    public enum NovelGesture { Jump, Nod }
    public enum NovelEmphasisMode { Off, Auto, Manual }

    /// <summary>UI adapters address physical render slots; story commands address actor instances.</summary>
    public interface INovelActorView
    {
        Vector2 NamedPosition(NovelPortraitSlot slot);
        void ApplyActor(NovelVisualState state, Vector2 gestureOffset, float gestureRotation);
        void ResetActor(NovelPortraitSlot slot);
    }

    public static class NovelActorRules
    {
        #region 外部方法
        [NoGC] public static bool IsAction(NovelCommandKind kind) => kind == NovelCommandKind.Opacity || kind == NovelCommandKind.Camera ||
            kind >= NovelCommandKind.Move && kind <= NovelCommandKind.Gesture || NovelScreenRules.IsAction(kind);
        [NoGC] public static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        [NoGC] public static bool Coordinates(Vector2 v) => Finite(v.x) && Finite(v.y) &&
            v.x >= -2 && v.x <= 3 && v.y >= -2 && v.y <= 3;
        [NoGC] public static bool Scaling(Vector2 v) => Finite(v.x) && Finite(v.y) && v.x >= .05f && v.x <= 5 && v.y >= .05f && v.y <= 5;
        [NoGC] public static float Ease(NovelEase ease, float p) => ease switch
        { NovelEase.SmoothStep => p * p * (3 - 2 * p), NovelEase.EaseIn => p * p, NovelEase.EaseOut => 1 - (1 - p) * (1 - p), _ => p };
        [HasGC] public static string Validate(NovelCommand c)
        {
            if (c.Kind == NovelCommandKind.Character)
            {
                if (!string.IsNullOrEmpty(c.InstanceId) && (string.IsNullOrWhiteSpace(c.InstanceId) || c.InstanceId.StartsWith("legacy-", StringComparison.Ordinal)))
                    return "显式人物 ID 不能为空白，也不能使用内部保留前缀 legacy-";
                if (c.VisualAction == NovelVisualAction.Show && (!Enum.IsDefined(typeof(NovelPositionMode), c.PositionMode) ||
                    c.PositionMode == NovelPositionMode.Normalized && (string.IsNullOrWhiteSpace(c.InstanceId) || !Coordinates(c.Position))))
                    return "坐标入场需要显式实例 ID 与有效归一化坐标";
            }
            if (c.Kind >= NovelCommandKind.Move && c.Kind <= NovelCommandKind.Gesture)
            {
                if (string.IsNullOrWhiteSpace(c.InstanceId) || string.IsNullOrWhiteSpace(c.ActionId) ||
                    !NovelActionHandle.ValidTime(c.Delay) || !Enum.IsDefined(typeof(NovelEase), c.Ease)) return "人物动作需要实例/动作 ID、有效延迟和缓动";
                if (c.Kind == NovelCommandKind.Move && (!Enum.IsDefined(typeof(NovelPositionMode), c.PositionMode) ||
                    !Enum.IsDefined(typeof(NovelPortraitSlot), c.Slot) || !Coordinates(c.Position) ||
                    c.ExitAfterMove && (c.PositionMode != NovelPositionMode.Normalized || c.Position.x >= 0 && c.Position.x <= 1 && c.Position.y >= 0 && c.Position.y <= 1)))
                    return "移动位置无效；退场必须使用画面外归一化坐标";
                if (c.Kind == NovelCommandKind.Scale && !Scaling(c.Scale)) return "缩放各轴必须在 0.05–5 内";
                if (c.Kind == NovelCommandKind.Rotate && (!Finite(c.Rotation) || Mathf.Abs(c.Rotation) > 3600)) return "旋转必须是有限角度，范围 ±3600";
                if (c.Kind == NovelCommandKind.Layer && (c.Layer < -100 || c.Layer > 100)) return "层级范围为 -100–100";
                if ((c.Kind == NovelCommandKind.Mirror || c.Kind == NovelCommandKind.Layer) && c.Duration != 0) return "镜像/层级是即时动作，时长必须为 0（允许开始延迟）";
                if (c.Kind == NovelCommandKind.Gesture && (!Enum.IsDefined(typeof(NovelGesture), c.Gesture) ||
                    !NovelActionHandle.ValidTime(c.Strength) || c.Strength > 5)) return "跳动/点头强度范围为 0–5";
            }
            if (c.Kind == NovelCommandKind.Emphasis && (!Enum.IsDefined(typeof(NovelEmphasisMode), c.EmphasisMode) ||
                !NovelActionHandle.ValidTime(c.DimFactor) || c.DimFactor > 1 ||
                c.EmphasisMode == NovelEmphasisMode.Manual && string.IsNullOrWhiteSpace(c.InstanceId))) return "强调模式、目标实例或暗化系数无效";
            return null;
        }
        #endregion
    }
}
