using System.Threading;

namespace Game.PlayerData
{
    internal static class ProgressIdentity
    {
        #region 内部参数
        private static long _epoch;
        private static long _request;
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal static long NextEpoch() => Interlocked.Increment(ref _epoch);
        internal static ProgressRequestContext Request(long epoch, int? slot = null) =>
            new(Interlocked.Increment(ref _request), slot, epoch);
        #endregion
    }
}
