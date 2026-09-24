using System.Linq;
using UnityEngine;

namespace Game.Narrative
{
    public sealed partial class NovelSession
    {
        #region 内部参数
        private NovelVisualState[] _cleanupActors;
        private float[] _cleanupOpacity;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void PresentHideAll(NovelCommand command, NarrativeSnapshot snapshot, float delta)
        {
            if (_presenting != command)
            {
                _presenting = command; _transition = 0;
                _cleanupActors = _visualStates.Where(v => v.Kind == NovelCommandKind.Character).ToArray();
                _cleanupOpacity = _cleanupActors.Select(v => v.Opacity).ToArray();
                foreach (var actor in _cleanupActors) CancelTarget(NovelTargetKind.Character, actor.InstanceId);
            }
            _transition += delta;
            float progress = command.Duration <= 0 || _readMode == NarrativeReadMode.Skip ? 1 : Mathf.Clamp01(_transition / command.Duration);
            float eased = NovelActorRules.Ease(command.Ease, progress);
            for (int i = 0; i < _cleanupActors.Length; i++)
            {
                var actor = _cleanupActors[i]; actor.Opacity = _cleanupOpacity[i] * (1 - eased);
                (_view as INovelOpacityView)?.SetOpacity(NovelTargetKind.Character, actor.Slot, actor.Opacity);
            }
            if (progress < 1 && _cleanupActors.Length > 0)
            { _runner.SetPresentationWait(snapshot.SessionGeneration, snapshot.PositionVersion, NarrativeWait.Transition); return; }
            foreach (var actor in _cleanupActors) RemoveActor(actor);
            _cleanupActors = null; _cleanupOpacity = null; _presenting = null;
            _runner.CompletePresentation(snapshot.SessionGeneration, snapshot.PositionVersion);
        }
        #endregion
    }
}
