using System;
using Ember.Basic;
using UnityEngine;

namespace Game.Narrative
{
    public enum NovelWipeDirection { LeftToRight, RightToLeft, BottomToTop, TopToBottom }
    public interface INovelCameraView { void SetCamera(Vector2 offset, float zoom); }
    public interface INovelWipeView { void SetWipe(float progress, NovelWipeDirection direction); }

    public static class NovelCameraRules
    {
        #region 外部方法
        [NoGC]
        public static bool Valid(Vector2 offset, float zoom) => NovelActorRules.Finite(zoom) && zoom >= 1 && zoom <= 3 &&
            NovelActorRules.Finite(offset.x) && NovelActorRules.Finite(offset.y) &&
            Mathf.Abs(offset.x) <= (zoom - 1) * .5f + .00001f && Mathf.Abs(offset.y) <= (zoom - 1) * .5f + .00001f;
        [HasGC]
        public static string Validate(NovelCommand c)
        {
            if (c.Kind != NovelCommandKind.Camera) return null;
            if (!NovelActionHandle.ValidTime(c.Delay) ||
                !Enum.IsDefined(typeof(NovelEase), c.Ease) || !Valid(c.Position, c.CameraZoom))
                return "舞台镜头需要非负延迟和有效缓动；缩放 1–3，平移各轴绝对值不得超过 (缩放−1)/2，复位为 1 倍及 (0,0)";
            return null;
        }
        #endregion
    }

    public sealed partial class NovelSession
    {
        #region 内部参数
        private Vector2 _cameraOffset;
        private float _cameraZoom = 1;
        public Vector2 CameraOffset => _cameraOffset;
        public float CameraZoom => _cameraZoom;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private NovelActionHandle StartCamera(NovelCommand command, NarrativeSnapshot snapshot)
        {
            if (_view is not INovelCameraView) throw new InvalidOperationException("页面未提供舞台镜头适配");
            CancelTarget(NovelTargetKind.Stage, "stage", "Camera");
            var action = new NovelActionHandle { Generation = snapshot.SessionGeneration, Sequence = ++_actionSequence,
                Id = NovelActionHandle.ResolveId(command), Kind = NovelCommandKind.Camera, Property = "Camera", TargetKind = NovelTargetKind.Stage, TargetId = "stage",
                VectorFrom = _cameraOffset, VectorTo = command.Position, From = _cameraZoom, To = command.CameraZoom,
                Duration = command.Duration, Delay = command.Delay, Ease = command.Ease };
            _actions[action.Id] = action; _runningActions.Add(action); return action;
        }
        private void WriteCamera(NovelActionHandle action, float progress)
        {
            float p = NovelActorRules.Ease(action.Ease, progress);
            _cameraOffset = Vector2.Lerp(action.VectorFrom, action.VectorTo, p); _cameraZoom = Mathf.Lerp(action.From, action.To, p);
            (_view as INovelCameraView)?.SetCamera(_cameraOffset, _cameraZoom);
        }
        #endregion
    }
}
