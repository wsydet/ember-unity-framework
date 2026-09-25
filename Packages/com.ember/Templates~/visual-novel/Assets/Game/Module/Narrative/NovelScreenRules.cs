using System;
using Ember.Basic;
using UnityEngine;

namespace Game.Narrative
{
    public enum NovelEffectPreference { Normal, Reduced, Off }

    /// <summary>Presentation only; the session owns clocks, identities and resource lifetimes.</summary>
    public interface INovelScreenView
    {
        void SetShake(NovelTargetKind kind, NovelPortraitSlot slot, Vector2 offset);
        void SetCover(Color color, bool wholeReader);
        void BeginCrossFade(NovelTargetKind kind, NovelPortraitSlot slot, Sprite next);
        void SetCrossFade(NovelTargetKind kind, NovelPortraitSlot slot, float progress);
        void EndCrossFade(NovelTargetKind kind, NovelPortraitSlot slot);
    }

    public static class NovelScreenRules
    {
        #region 外部方法
        [NoGC] public static bool IsAction(NovelCommandKind kind) => kind >= NovelCommandKind.Shake && kind <= NovelCommandKind.CrossFade || kind == NovelCommandKind.Wipe;
        [NoGC] public static NovelCommandKind ResourceKind(NovelCommand c) => c.Kind == NovelCommandKind.Wipe ? NovelCommandKind.Background : c.Kind == NovelCommandKind.CrossFade ?
            c.TargetKind == NovelTargetKind.Background ? NovelCommandKind.Background : NovelCommandKind.Character : c.Kind;
        [NoGC] public static bool ValidColor(Color c) => Unit(c.r) && Unit(c.g) && Unit(c.b) && Unit(c.a);
        [NoGC] public static float PreferenceScale(NovelEffectPreference value) => value == NovelEffectPreference.Off ? 0 : value == NovelEffectPreference.Reduced ? .25f : 1;
        [HasGC] public static string Validate(NovelCommand c)
        {
            if (!IsAction(c.Kind)) return null;
            if (!NovelActionHandle.ValidTime(c.Delay) ||
                !Enum.IsDefined(typeof(NovelEase), c.Ease)) return "画面动作需要非负延迟与有效缓动";
            if (c.Kind == NovelCommandKind.Wipe && (c.TargetKind != NovelTargetKind.Background || !Enum.IsDefined(typeof(NovelWipeDirection), c.WipeDirection)))
                return "擦除转场仅支持已有背景，方向必须为左到右/右到左/下到上/上到下";
            if (c.Kind == NovelCommandKind.Shake)
            {
                if (c.TargetKind != NovelTargetKind.Stage && c.TargetKind != NovelTargetKind.Character ||
                    c.TargetKind == NovelTargetKind.Character && string.IsNullOrWhiteSpace(c.InstanceId)) return "震动目标仅支持舞台或已显示的人物实例";
                if (!NovelActionHandle.ValidTime(c.Strength) || c.Strength > 5 ||
                    !NovelActorRules.Finite(c.Frequency) || c.Frequency < .1f || c.Frequency > 60 ||
                    !NovelActorRules.Finite(c.Direction.x) || !NovelActorRules.Finite(c.Direction.y) ||
                    c.Direction.sqrMagnitude < .0001f || c.Direction.sqrMagnitude > 10000) return "震动强度 0–5、频率 0.1–60 Hz，方向需为有限非零向量";
            }
            if ((c.Kind == NovelCommandKind.Cover || c.Kind == NovelCommandKind.Flash) &&
                (!ValidColor(c.Color) || !Unit(c.Opacity) || !NovelActionHandle.ValidTime(c.Hold) ||
                !NovelActionHandle.ValidTime(c.Duration + c.Hold))) return "遮罩需要 0–1 颜色和透明度、有限非负保持时间";
            if (c.Kind == NovelCommandKind.CrossFade &&
                (c.TargetKind != NovelTargetKind.Background && c.TargetKind != NovelTargetKind.Character ||
                 c.TargetKind == NovelTargetKind.Character && string.IsNullOrWhiteSpace(c.InstanceId))) return "交叉淡化需要背景或已显示的人物实例";
            return null;
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private static bool Unit(float v) => NovelActionHandle.ValidTime(v) && v <= 1;
        #endregion
    }
}
