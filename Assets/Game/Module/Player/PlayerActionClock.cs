using System;

namespace Game.Player
{
    /// <summary>动作逻辑时钟；只由实际固定步推进，攻击倍率不影响外部输入/收刀游戏秒。</summary>
    internal sealed class PlayerActionClock
    {
        #region 内部参数
        internal double Previous { get; private set; }
        internal double Current { get; private set; }
        internal float Speed { get; private set; } = 1;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal void Reset(float speed = 1)
        {
            PlayerCombatData.Positive(speed, "ActionClock.Speed");
            Previous = Current = 0; Speed = speed;
        }
        internal void Advance(float dt)
        {
            if (!float.IsFinite(dt) || dt < 0) throw new ArgumentOutOfRangeException(nameof(dt));
            Previous = Current; Current += (double)dt * Speed;
        }
        internal bool Crosses(double start, double end) => start < end && Previous < end && Current >= start;
        internal bool Reached(double time) => Current >= time;
        #endregion
    }
}
