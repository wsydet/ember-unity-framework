using System;

namespace Game.Player
{
    [Flags]
    internal enum PlayerMotionChannels
    {
        None = 0, Horizontal = 1, Facing = 2, AutomaticTransitions = 4,
        Wall = 8, Ledge = 16, Vertical = 32, Correction = 64
    }

    /// <summary>互斥动作显式取得运动职责；旧动作释放不能解除后来动作的控制权。</summary>
    internal sealed class PlayerMotionControl
    {
        #region 内部参数
        private object _owner;
        private PlayerMotionChannels _channels;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal bool Owns(PlayerMotionChannels channel) => (_channels & channel) != 0;
        internal string Observation => $"{_owner?.GetType().Name ?? "普通移动"} · {_channels}";
        internal void Take(object owner, PlayerMotionChannels channels)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner)); _channels = channels;
        }
        internal void Release(object owner)
        {
            if (!ReferenceEquals(_owner, owner)) return;
            _owner = null; _channels = PlayerMotionChannels.None;
        }
        #endregion
    }
}
