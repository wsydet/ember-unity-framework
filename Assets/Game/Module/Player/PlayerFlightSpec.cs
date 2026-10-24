using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>单次飞行下降配置。值复制冻结，不更改上升段或其他来源；default 关闭。</summary>
    [Serializable]
    public struct PlayerFlightSpec
    {
        #region 编辑器面板参数
        [SerializeField] private bool _enabled;
        [SerializeField] private float _fallAcceleration;
        [SerializeField] private float _fallLimit;
        [SerializeField] private bool _heldSlowFall;
        [SerializeField] private float _heldSpeed;
        [SerializeField] private float _heldAcceleration;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public bool Enabled => _enabled;
        public float FallAcceleration => _fallAcceleration;
        public float FallLimit => _fallLimit;
        public bool HeldSlowFall => _heldSlowFall;
        public float HeldSpeed => _heldSpeed;
        public float HeldAcceleration => _heldAcceleration;
        public PlayerFlightSpec(float fallAcceleration, float fallLimit, bool heldSlowFall = false,
            float heldSpeed = 0, float heldAcceleration = 0)
        {
            _enabled = true; _fallAcceleration = fallAcceleration; _fallLimit = fallLimit;
            _heldSlowFall = heldSlowFall; _heldSpeed = heldSpeed; _heldAcceleration = heldAcceleration;
            Validate();
        }
        public void Validate()
        {
            if (!_enabled) return;
            if (!float.IsFinite(_fallAcceleration) || _fallAcceleration <= 0 || !float.IsFinite(_fallLimit) || _fallLimit <= 0 ||
                _heldSlowFall && (!float.IsFinite(_heldSpeed) || _heldSpeed < 0 || !float.IsFinite(_heldAcceleration) ||
                    _heldAcceleration <= 0 || _heldAcceleration > _fallAcceleration))
                throw new InvalidOperationException("飞行下降策略需要有限正加速度/终速，低速按住加速度不得大于常规下降加速度。");
        }
        internal void ValidateWorld(Vector2 gravity)
        {
            Validate();
            if (_enabled && (gravity.x != 0 || !float.IsFinite(gravity.y) || gravity.y >= 0 ||
                !float.IsFinite(_fallAcceleration / -gravity.y)))
                throw new InvalidOperationException("飞行下降策略需要纯竖直向下重力及有限倍率。");
        }
        #endregion
    }

    /// <summary>只代表这次飞行；释放旧凭证不会清除新飞行，动作结束不自动释放。</summary>
    public sealed class PlayerFlightLease : IDisposable
    {
        #region 内部参数
        internal PlayerVerticalMotion Session;
        internal readonly Behaviour Owner;
        internal readonly PlayerInputAdapter Input;
        internal readonly ulong ClearVersion;
        internal readonly PlayerFlightSpec Spec;
        public string SourceId { get; }
        public ulong Generation { get; }
        public bool IsActive => Session != null && Owner != null && Owner.isActiveAndEnabled;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerFlightLease(PlayerVerticalMotion session, Behaviour owner, PlayerInputAdapter input,
            string sourceId, ulong generation, PlayerFlightSpec spec)
        {
            Session = session; Owner = owner; Input = input; ClearVersion = input.ClearVersion;
            SourceId = sourceId; Generation = generation; Spec = spec;
        }
        public void Dispose()
        {
            var session = Session; Session = null; session?.ReleaseFlight(this);
        }
        #endregion
    }
}
