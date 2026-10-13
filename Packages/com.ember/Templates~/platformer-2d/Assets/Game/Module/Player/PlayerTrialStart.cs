using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>请求接受时冻结的试跑条件；不修改正式配置，不包含存档槽位或旧动作进度。</summary>
    public sealed class PlayerTrialStart
    {
        #region 内部参数
        public string Scene { get; }
        public Vector2 Position { get; }
        public Vector2 Velocity { get; }
        public PlayerFeelRuntime Feel { get; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public PlayerTrialStart(string scene, Vector2 position, Vector2 velocity, PlayerFeelProfile feel)
        {
            if (string.IsNullOrWhiteSpace(scene)) throw new ArgumentException("试跑必须指定目标场景名称。", nameof(scene));
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y) ||
                !float.IsFinite(velocity.x) || !float.IsFinite(velocity.y))
                throw new ArgumentException("试跑初始位置/速度必须为有限值。");
            if (feel == null) throw new ArgumentNullException(nameof(feel));
            Scene = scene; Position = position; Velocity = velocity;
            Feel = feel.Capture(Physics2D.gravity);
        }
        #endregion
    }
}
