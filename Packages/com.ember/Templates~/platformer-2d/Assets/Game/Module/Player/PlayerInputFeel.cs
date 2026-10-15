using System;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/Feel/Input Windows", fileName = "PlayerInputFeel")]
    public sealed class PlayerInputFeel : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField] private bool _enableJumpBuffer = true;
        [SerializeField, Min(0), Tooltip("跳跃缓冲，游戏秒；关闭仍保留首次物理决策。")] private float _jumpBufferDuration = .12f;
        [SerializeField] private bool _enableCoyote = true;
        [SerializeField, Min(0), Tooltip("离地起跳宽限，游戏秒；不提供边缘悬停。")] private float _coyoteDuration = .1f;
        [SerializeField, Min(0), Tooltip("攻击按下请求的有效游戏秒数。")] private float _attackBufferDuration = .2f;
        [SerializeField, Min(0), Tooltip("战斗上下文中冲刺取消请求的有效游戏秒数。")] private float _combatDashBufferDuration = .15f;
        [SerializeField, Min(0), Tooltip("战斗上下文中跳跃取消请求的有效游戏秒数。")] private float _combatJumpBufferDuration = .15f;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public bool EnableJumpBuffer => _enableJumpBuffer;
        public bool EnableCoyote => _enableCoyote;
        public float BufferDuration => _enableJumpBuffer ? _jumpBufferDuration : 0;
        public float CoyoteDuration => _enableCoyote ? _coyoteDuration : 0;
        public float AttackBufferDuration => _attackBufferDuration;
        public float CombatDashBufferDuration => _combatDashBufferDuration;
        public float CombatJumpBufferDuration => _combatJumpBufferDuration;
        internal void Validate()
        {
            if (!float.IsFinite(_jumpBufferDuration) || _jumpBufferDuration < 0 ||
                !float.IsFinite(_coyoteDuration) || _coyoteDuration < 0)
                throw new InvalidOperationException("输入窗口必须为有限非负秒数。");
            PlayerCombatData.NonNegative(_attackBufferDuration, "Input.AttackBufferDuration");
            PlayerCombatData.NonNegative(_combatDashBufferDuration, "Input.CombatDashBufferDuration");
            PlayerCombatData.NonNegative(_combatJumpBufferDuration, "Input.CombatJumpBufferDuration");
        }
        #endregion
    }
}

