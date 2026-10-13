using System;
using System.Threading;
using Ember.Basic;
using Game.Input;
using Game.Module;

namespace Game.Player
{
    public enum PlayerSessionState { Inactive, Ready, Running, Paused, Faulted }

    /// <summary>显式新游戏/选定槽位。没有隐式当前槽位，也不把读失败变成新游戏。</summary>
    public sealed class PlayerEntryRequest
    {
        public int? Slot { get; }
        internal PlatformerSnapshot Snapshot { get; }
        public PlayerTrialStart Trial { get; }
        private PlayerEntryRequest(int? slot) => Slot = slot;
        private PlayerEntryRequest(PlatformerSnapshot snapshot, int? slot)
        { Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot)); Slot = slot; }
        private PlayerEntryRequest(PlayerTrialStart trial)
        { Trial = trial ?? throw new ArgumentNullException(nameof(trial)); }
        internal static PlayerEntryRequest FromSnapshot(PlatformerSnapshot snapshot, int? slot = null) => new(snapshot, slot);
        [HasGC] public static PlayerEntryRequest NewGame() => new((int?)null);
        [HasGC] public static PlayerEntryRequest FromSlot(int slot) => new(slot);
        [HasGC] public static PlayerEntryRequest ForTrial(PlayerTrialStart start) => new(start);
    }

    /// <summary>转场拥有的候选，独立于模块运行字段，Reset 不清除它。成功交接后由模块接管。</summary>
    public sealed class PreparedPlayerSession : IDisposable
    {
        internal PlayerModule Owner;
        internal long Request;
        internal GameInputModule Input;
        internal PlayerDataModule Data;
        internal long InputEpoch, DataEpoch;
        internal CancellationToken Cancellation;
        internal PlayerConfig Config;
        internal PlayerSchemeCapture SchemeCapture;
        internal string InputChannel;
        internal PreparedPlayerSession Previous;
        internal PlayerRuntimeCandidate Runtime;
        internal PlayerRecoveryLedger Recovery;
        internal bool Transferred, Disposed;
        public PlayerController Controller { get; internal set; }
        public int? SourceSlot { get; internal set; }
        public PlayerPowerProgress PowerProgress { get; internal set; }
        public bool IsTrial { get; internal set; }
        public void Dispose()
        {
            if (Disposed || Transferred) return;
            Disposed = true;
            try { if (Controller != null) Controller.StopPrepared(this); }
            finally
            {
                try { Runtime?.Dispose(); }
                finally { try { Recovery?.Dispose(); } finally { ReleaseScheme(); } }
            }
        }
        internal void ReleaseScheme()
        { var capture = SchemeCapture; SchemeCapture = null; capture?.Dispose(); }
    }

    public sealed class PlayerPauseRequest : IDisposable
    {
        internal PlayerModule Module;
        internal PlayerPauseRequest(PlayerModule module) { Module = module; }
        public void Dispose()
        {
            var module = Module; Module = null;
            module?.ReleasePause(this);
        }
    }
}
