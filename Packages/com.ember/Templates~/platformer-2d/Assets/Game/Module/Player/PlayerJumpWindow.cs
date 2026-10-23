using System;
using UnityEngine;
using Game.Entity;

namespace Game.Player
{
    public enum PlayerGroundSupportReason { NoContact, Separating, ConsumedUntilSeparation, Contact, LegacyProbe, ActionSuppressed }
    /// <summary>每角色的地面跳跃机会及离地宽限计时；不读取输入，不请求转换，不执行运动。</summary>
    public sealed class PlayerJumpWindow
    {
        #region 内部参数
        private readonly float _coyoteDuration;
        private float _remaining;
        private bool _awaitingSeparation;
        private bool _restoredGroundBaseline;
        /// <summary>仅用于真实接触法向相对速度的舍入噪声，单位 u/s；不是探地距离或宽限。</summary>
        public const float CONTACT_SEPARATION_TOLERANCE = .001f;
        public EntityGroundContact2D Contact { get; private set; }
        public PlayerGroundSupportReason SupportReason { get; private set; }
        public bool AwaitingSeparation => _awaitingSeparation;
        /// <summary>当前离地宽限剩余秒数；在地面上显示零，读取不会推进或消费机会。</summary>
        public float CoyoteRemaining => HasGroundSupport ? 0 : _remaining;
        public bool CanJump { get; private set; }
        public bool HasGroundSupport { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public PlayerJumpWindow(float coyoteDuration)
        {
            if (!float.IsFinite(coyoteDuration) || coyoteDuration < 0)
                throw new ArgumentOutOfRangeException(nameof(coyoteDuration));
            _coyoteDuration = coyoteDuration;
        }
        /// <summary>统一确认支撑事实，返回本步是否首次取得有效支撑；连续支撑不重复补充资源。</summary>
        public bool Observe(bool grounded, float verticalSpeed, float dt)
        {
            // 兼容旧调用；bool 不能证明真实接触，不对该入口扩大速度容差。
            bool supported = grounded && verticalSpeed <= 0;
            if (!grounded) _awaitingSeparation = false;
            supported &= !_awaitingSeparation;
            SupportReason = supported ? PlayerGroundSupportReason.LegacyProbe :
                _awaitingSeparation ? PlayerGroundSupportReason.ConsumedUntilSeparation : PlayerGroundSupportReason.NoContact;
            return UpdateWindow(supported, dt);
        }
        /// <summary>由 Controller 每物理步统一消费真实接触。消费后须先观察到接触脱离，再允许新落地。</summary>
        public bool Observe(EntityGroundContact2D contact, float dt, bool groundInRange = false, bool suppressSupport = false)
        {
            Contact = contact;
            if (!groundInRange && !contact.HasContact) _restoredGroundBaseline = false;
            if (!contact.HasContact) _awaitingSeparation = false;
            SupportReason = !contact.HasContact ? PlayerGroundSupportReason.NoContact :
                suppressSupport ? PlayerGroundSupportReason.ActionSuppressed :
                _awaitingSeparation ? PlayerGroundSupportReason.ConsumedUntilSeparation :
                !float.IsFinite(contact.SeparationSpeed) || contact.SeparationSpeed > CONTACT_SEPARATION_TOLERANCE
                    ? PlayerGroundSupportReason.Separating : PlayerGroundSupportReason.Contact;
            bool acquired = UpdateWindow(SupportReason == PlayerGroundSupportReason.Contact, dt);
            if (acquired && _restoredGroundBaseline) { _restoredGroundBaseline = false; return false; }
            return acquired;
        }
        // 恢复在探地范围内的静止存档只建立基线；等首次真实接触，不制造落地奖励。
        internal void PreserveRestoredGroundBaseline() => _restoredGroundBaseline = true;
        private bool UpdateWindow(bool supported, float dt)
        {
            if (!float.IsFinite(dt) || dt < 0) throw new ArgumentOutOfRangeException(nameof(dt));
            bool acquired = supported && !HasGroundSupport;
            HasGroundSupport = supported;
            if (supported)
            { _remaining = _coyoteDuration; CanJump = true; }
            else
            {
                _remaining = Mathf.Max(0, _remaining - dt);
                CanJump = _remaining > 0.000001f; // 避免多次 float 扣减让边界多活一整步。
            }
            return acquired;
        }
        // 挂边/下穿暂时禁用支撑；与主动起跳的一次性离地锁分开。
        internal void ClearSupport()
        {
            _remaining = 0; CanJump = false; HasGroundSupport = false;
            _restoredGroundBaseline = false;
            SupportReason = PlayerGroundSupportReason.ActionSuppressed;
        }
        public void Consume()
        {
            _awaitingSeparation |= Contact.HasContact || HasGroundSupport;
            _remaining = 0; CanJump = false; HasGroundSupport = false;
            _restoredGroundBaseline = false;
            SupportReason = PlayerGroundSupportReason.ConsumedUntilSeparation;
        }
        #endregion
    }
}
