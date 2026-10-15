using System;
using UnityEngine;

namespace Game.Player
{
    public enum PlayerCorrectionDirection
    {
        [InspectorName("优先右／上")] PositiveFirst,
        [InspectorName("优先左／下")] NegativeFirst,
        [InspectorName("仅右／上")] PositiveOnly,
        [InspectorName("仅左／下")] NegativeOnly,
        [InspectorName("优先输入方向")] InputFirst
    }

    [Serializable]
    public sealed class PlayerCorrectionSettings
    {
        #region 编辑器面板参数
        [SerializeField] private bool _enabled;
        [SerializeField, Min(.001f), Tooltip("与运动方向垂直的最大挪动距离；跳跃为左右，冲刺为上下，跨台阶仅向上。")] private float _maximumDistance = .2f;
        [SerializeField, Min(.001f), Tooltip("由近到远搜索的步长；最大距离/步长不能超过64，避免无界查询。")] private float _increment = .025f;
        [SerializeField, Min(0), Tooltip("至少检查前方此距离；实际检查范围不会小于本步运动距离。")] private float _lookAhead = .06f;
        [SerializeField, Min(.001f), Tooltip("补正路径和目的位置的安全间隙，世界单位。")] private float _skin = .01f;
        [SerializeField, Tooltip("每个距离先试哪个方向。正方向为右/上；输入优先用对应轴输入，无输入回退右/上。跨台阶固定向上。")] private PlayerCorrectionDirection _direction = PlayerCorrectionDirection.PositiveFirst;
        [SerializeField, Tooltip("允许触发补正的静止实体障碍层；安全检查仍检查其他物理碰撞层。")] private LayerMask _obstacleLayers = 1;
        [SerializeField, Min(.001f), Tooltip("地面冲刺和跨台阶需要在本步前进后的脚下找到支撑；该值为向下确认距离。")] private float _supportDistance = .08f;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerCorrectionSpec Capture()
        {
            if (!_enabled) return default;
            if (!float.IsFinite(_maximumDistance * _maximumDistance) || _maximumDistance <= 0 || !float.IsFinite(_increment) || _increment <= 0 ||
                _maximumDistance / _increment > 64 || !float.IsFinite(_lookAhead) || _lookAhead < 0 ||
                !float.IsFinite(_skin) || _skin <= 0 || _skin >= _maximumDistance ||
                !float.IsFinite(_supportDistance) || _supportDistance <= 0 || _obstacleLayers.value == 0 ||
                !Enum.IsDefined(typeof(PlayerCorrectionDirection), _direction))
                throw new InvalidOperationException("碰撞补正需要有效距离、步长、间隙、方向和障碍层；搜索最多64级，安全间隙须小于最大补正距离。");
            return new PlayerCorrectionSpec(true, _maximumDistance, _increment, _lookAhead, _skin, _direction, _obstacleLayers, _supportDistance);
        }
        #endregion
    }

    public readonly struct PlayerCorrectionSpec
    {
        public readonly bool Enabled;
        public readonly float MaximumDistance, Increment, LookAhead, Skin, SupportDistance;
        public readonly PlayerCorrectionDirection Direction;
        public readonly LayerMask ObstacleLayers;
        internal PlayerCorrectionSpec(bool enabled, float maximum, float increment, float lookAhead, float skin,
            PlayerCorrectionDirection direction, LayerMask layers, float supportDistance)
        {
            Enabled = enabled; MaximumDistance = maximum; Increment = increment; LookAhead = lookAhead;
            Skin = skin; Direction = direction; ObstacleLayers = layers; SupportDistance = supportDistance;
        }
    }
}
