using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>Movement SO 内的唯一挂边几何与翻越时长配置；启用和解锁来自两个 Ability。</summary>
    [Serializable]
    public sealed class PlayerLedgeSettings
    {
        #region 编辑器面板参数
        [SerializeField] private LayerMask _layers = 1;
        [SerializeField, Min(.01f)] private float _reach = .2f;
        [SerializeField, Min(.01f)] private float _verticalReach = .4f;
        [SerializeField, Min(.01f)] private float _gripInset = .2f;
        [SerializeField, Min(.001f)] private float _skin = .02f;
        [SerializeField, Min(.01f)] private float _riseDuration = .2f;
        [SerializeField, Min(.01f)] private float _inwardDuration = .2f;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerLedgeSpec Capture() => new(_layers, _reach, _verticalReach, _gripInset, _skin, _riseDuration, _inwardDuration);
        #endregion
    }

    public readonly struct PlayerLedgeSpec
    {
        #region 内部参数
        public readonly LayerMask Layers;
        public readonly float Reach, VerticalReach, GripInset, Skin, RiseDuration, InwardDuration;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerLedgeSpec(LayerMask layers, float reach, float verticalReach, float gripInset,
            float skin, float riseDuration, float inwardDuration)
        { Layers = layers; Reach = reach; VerticalReach = verticalReach; GripInset = gripInset; Skin = skin; RiseDuration = riseDuration; InwardDuration = inwardDuration; }
        internal void Validate()
        {
            if (Layers.value == 0 || !float.IsFinite(Reach) || Reach <= 0 || !float.IsFinite(VerticalReach) || VerticalReach <= 0 ||
                !float.IsFinite(GripInset) || GripInset <= 0 || !float.IsFinite(Skin) || Skin <= 0 ||
                !float.IsFinite(RiseDuration) || RiseDuration <= 0 || !float.IsFinite(InwardDuration) || InwardDuration <= 0)
                throw new InvalidOperationException("挂边／翻越需要非空碰撞层及有限正数的探测距离、间距和时长。");
        }
        #endregion
    }
}
