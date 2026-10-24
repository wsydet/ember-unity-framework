using Ember.Core;
using Game.PlayerData;

namespace Game.Player
{
    public enum PlayerExitStatus { Completed, Busy, InvalidState, SaveFailed, DrainFailed, Cancelled, Obsolete, TransitionFailed }
    public sealed class PlayerExitResult
    {
        public PlayerExitStatus Status { get; }
        public SaveResult Save { get; }
        public DrainResult Drain { get; }
        public EmberTransitionResult Transition { get; }
        internal PlayerExitResult(PlayerExitStatus status, SaveResult save = null, DrainResult drain = null,
            EmberTransitionResult transition = null)
        { Status = status; Save = save; Drain = drain; Transition = transition; }
    }
}
