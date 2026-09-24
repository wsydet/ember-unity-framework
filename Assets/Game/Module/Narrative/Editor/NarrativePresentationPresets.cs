using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Narrative.Editor
{
    public enum NarrativePresetKind { Entrance, Impact, Memory, Reset, HideAll, RestoreStage, SceneCleanup }
    /// <summary>Author-time expansion into ordinary commands. No runtime macro interpreter or shared mutable preset state.</summary>
    public static class NarrativePresentationPresets
    {
        #region 外部方法
        public static IReadOnlyList<NovelCommand> Build(NarrativePresetKind kind, string prefix, string actor,
            string portrait, NovelPortraitSlot slot, float duration, float strength, string sfx = null)
        {
            if (!Enum.IsDefined(typeof(NarrativePresetKind), kind) || string.IsNullOrWhiteSpace(prefix) ||
                !NovelActionHandle.ValidTime(duration) || duration <= 0 || duration > 30 ||
                !NovelActorRules.Finite(strength) || strength < .1f || strength > 3 || !Enum.IsDefined(typeof(NovelPortraitSlot), slot))
                throw new ArgumentException("预设需要有效名称，时长 (0,30] 秒、强度 0.1–3 与有效位置");
            if ((kind == NarrativePresetKind.Entrance || kind == NarrativePresetKind.Impact) && string.IsNullOrWhiteSpace(actor))
                throw new ArgumentException("预设需要人物实例 ID");
            if (kind == NarrativePresetKind.Entrance && string.IsNullOrWhiteSpace(portrait)) throw new ArgumentException("进场需要立绘资源键");
            var result = new List<NovelCommand>(); var waits = new List<string>();
            void Add(NovelCommand c) { result.Add(c); if (!string.IsNullOrEmpty(c.ActionId)) waits.Add(c.ActionId); }
            if (kind == NarrativePresetKind.HideAll)
                return new[] { new NovelCommand(prefix + "-hide-all", NovelCommandKind.HideAllCharacters, duration: duration, ease: NovelEase.SmoothStep) };
            if (kind == NarrativePresetKind.SceneCleanup)
                result.Add(new NovelCommand(prefix + "-hide-all", NovelCommandKind.HideAllCharacters, duration: duration, ease: NovelEase.SmoothStep));
            if (kind == NarrativePresetKind.Entrance)
            {
                Add(new NovelCommand(prefix + "-show", NovelCommandKind.Character, resourceKey: portrait, instanceId: actor, slot: slot,
                    positionMode: NovelPositionMode.Normalized, position: new Vector2(slot == NovelPortraitSlot.Right ? 1.2f : -.2f, .335f)));
                Add(new NovelCommand(prefix + "-move", NovelCommandKind.Move, instanceId: actor, slot: slot, actionId: prefix + "-move",
                    duration: duration, parallel: true, ease: NovelEase.SmoothStep));
            }
            else if (kind == NarrativePresetKind.Impact)
            {
                Add(new NovelCommand(prefix + "-shake", NovelCommandKind.Shake, instanceId: actor, targetKind: NovelTargetKind.Character,
                    actionId: prefix + "-shake", duration: duration, strength: strength, frequency: 12, parallel: true));
                Add(new NovelCommand(prefix + "-flash", NovelCommandKind.Flash, actionId: prefix + "-flash", duration: duration,
                    color: Color.white, opacity: .12f * strength, parallel: true));
                if (!string.IsNullOrWhiteSpace(sfx)) Add(new NovelCommand(prefix + "-sfx", NovelCommandKind.SFX, resourceKey: sfx));
            }
            else
            {
                bool reset = kind != NarrativePresetKind.Memory;
                Add(new NovelCommand(prefix + "-camera", NovelCommandKind.Camera, actionId: prefix + "-camera", cameraZoom: reset ? 1 : 1 + .1f * strength,
                    duration: duration, ease: NovelEase.SmoothStep, parallel: true));
                Add(new NovelCommand(prefix + "-cover", NovelCommandKind.Cover, actionId: prefix + "-cover", duration: duration,
                    color: new Color(.6f, .43f, .25f, 1), opacity: reset ? 0 : .08f * strength, parallel: true));
            }
            if (kind == NarrativePresetKind.RestoreStage || kind == NarrativePresetKind.SceneCleanup)
            {
                Add(new NovelCommand(prefix + "-opacity", NovelCommandKind.Opacity, targetKind: NovelTargetKind.Stage, opacity: 1, actionId: prefix + "-opacity", duration: duration, parallel: true));
                result.Add(new NovelCommand(prefix + "-emphasis", NovelCommandKind.Emphasis, emphasisMode: NovelEmphasisMode.Off));
                result.Add(new NovelCommand(prefix + "-dialogue", NovelCommandKind.DialogueVisibility, dialogueVisible: true));
            }
            result.Add(new NovelCommand(prefix + "-wait", NovelCommandKind.WaitActions, waitActions: waits.ToArray()));
            return result.AsReadOnly();
        }
        #endregion
    }
}
