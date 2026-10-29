using System;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    public enum PlayerDashDirection { MoveElseAim, Aim }

    [Serializable]
    public sealed class PlayerCombatSettings
    {
        #region 编辑器面板参数
        [SerializeField,Min(1)] private float _maximumHealth=6;
        [SerializeField,Min(0)] private float _damageProtection=.45f;
        [SerializeField,Min(.01f)] private float _dashDuration=.16f,_dashSpeed=14;
        [SerializeField] private bool _dashInvulnerable;
        [SerializeField] private PlayerDashDirection _dashDirection;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerCombatSpec Capture()
        {
            foreach(var number in new[]{_maximumHealth,_damageProtection,_dashDuration,_dashSpeed})
                if(!float.IsFinite(number))throw new InvalidOperationException("战斗配置必须为有限数值。");
            if(_maximumHealth<=0||_damageProtection<0||_dashDuration<=0||_dashSpeed<=0||
                !Enum.IsDefined(typeof(PlayerDashDirection),_dashDirection))throw new InvalidOperationException("战斗时长、命中窗口、形状或生命配置无效。");
            return new PlayerCombatSpec(_maximumHealth,_damageProtection,_dashDuration,_dashSpeed,_dashInvulnerable,_dashDirection);
        }
        #endregion
    }

    public sealed class PlayerCombatSpec
    {
        #region 内部参数
        public float MaximumHealth {get;}
        public float DamageProtection {get;}
        public float DashDuration {get;}
        public float DashSpeed {get;}
        public bool DashInvulnerable {get;}
        public PlayerDashDirection DashDirection {get;}
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerCombatSpec(float health,float protection,float dashDuration,float dashSpeed,bool dashInvulnerable,PlayerDashDirection direction)
        {MaximumHealth=health;DamageProtection=protection;DashDuration=dashDuration;DashSpeed=dashSpeed;DashInvulnerable=dashInvulnerable;DashDirection=direction;}
        internal EntityEffectSpec DamageEffect()=>new("damage-protection",EntityEffectKind.Buff,EntityEffectLifetime.Timed,DamageProtection,
            new EntityEffectRules(new[]{"physical"},false,0),new EntityEffectAppearance(true,10,.35f,8));
        internal EntityEffectSpec DashEffect()=>new("dash-protection",EntityEffectKind.Buff,EntityEffectLifetime.Owned,0,
            new EntityEffectRules(new[]{"physical"},true,0),new EntityEffectAppearance(true,20,.5f,0));
        #endregion
    }
}
