using System;
using UnityEngine;

namespace Game.Player
{
    [Serializable]
    public sealed class PlayerLandingSettings
    {
        #region 编辑器面板参数
        [SerializeField] private bool _normalEnabled;
        [SerializeField, Min(.001f), Tooltip("普通落地停顿，秒")] private float _normalDuration = .08f;
        [SerializeField] private bool _hardEnabled;
        [SerializeField, Min(.001f), Tooltip("接触前下降速度阈值，世界单位/秒")] private float _hardSpeed = 18;
        [SerializeField, Min(.001f), Tooltip("重落地不可取消停顿，秒")] private float _hardDuration = .35f;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerLandingSpec Capture() => new(_normalEnabled, _normalDuration, _hardEnabled, _hardSpeed, _hardDuration);
        #endregion
    }

    public readonly struct PlayerLandingSpec
    {
        #region 内部参数
        public readonly bool NormalEnabled, HardEnabled;
        public readonly float NormalDuration, HardSpeed, HardDuration;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerLandingSpec(bool normal, float normalDuration, bool hard, float hardSpeed, float hardDuration)
        {
            NormalEnabled = normal; NormalDuration = normalDuration; HardEnabled = hard; HardSpeed = hardSpeed; HardDuration = hardDuration;
            if ((normal && (!float.IsFinite(normalDuration) || normalDuration <= 0)) ||
                (hard && (!float.IsFinite(hardSpeed) || hardSpeed <= 0 || !float.IsFinite(hardDuration) || hardDuration <= 0)))
                throw new InvalidOperationException("启用落地停顿时，时长和重落地速度阈值必须为有限正数。");
        }
        #endregion
    }
}
