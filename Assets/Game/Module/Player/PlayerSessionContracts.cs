using System;
using System.Threading;
using Ember.Basic;
using Game.Input;
using Game.Module;

namespace Game.Player
{
    public enum PlayerSessionState { Inactive, Ready, Running, Paused, Faulted }

    /// <summary>显式新游戏/选定槽位。没有隐式当前槽位，也不把读失败变成新游戏。</summary>
    public sealed class PlayerEntryRequest : IDisposable
    {
        internal PlayerSchemeCapture LevelCapture { get; private set; }
        public string LevelId { get; private set; }
        public string ScenePath { get; private set; }
        public bool IsDisposed { get; private set; }
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
        /// <summary>在转场发起前冻结完整关卡装配；调用方在转场完成后释放请求。</summary>
        [HasGC] public static PlayerEntryRequest ForLevel(string levelId, string scenePath, PlayerConfig config, PlatformerSnapshot checkpoint = null)
        {
            if (string.IsNullOrWhiteSpace(levelId) || string.IsNullOrWhiteSpace(scenePath) || config == null ||
                System.IO.Path.GetFileNameWithoutExtension(scenePath) != config.Scene || checkpoint != null && checkpoint.Scene != config.Scene)
                throw new ArgumentException("关卡ID、场景路径、配置场景和检查点必须一致。");
            var result = checkpoint == null ? NewGame() : FromSnapshot(checkpoint);
            result.LevelId = levelId; result.ScenePath = scenePath;
            result.LevelCapture = new PlayerSchemeCapture(config);
            return result;
        }
        public void Dispose() { LevelCapture?.Dispose(); LevelCapture = null; IsDisposed = true; }
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
        internal bool CrossScene;
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
