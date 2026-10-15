using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Player
{
    public enum PlayerAttackEnvironment { Ground, Air, Both }
    public enum PlayerAttackMotion { Stationary, Moving }
    public enum PlayerAttackEntryVelocity { Preserve, Scale, Set }
    public enum PlayerDashAttackMode { Dedicated, ContinueNormal }
    public enum PlayerHitShapeKind { Box, Circle, Polygon }
    public enum PlayerHitRetry { RecordAttempt, RecordAccepted }

    /// <summary>Feel 内的攻击参数；连接只属于 Combo，能力资源只属于 Powers。</summary>
    [Serializable]
    public sealed class PlayerAttackProfile
    {
        #region 编辑器面板参数
        [SerializeField] private bool _enabled;
        [SerializeField] private PlayerAttackSegment[] _segments = Array.Empty<PlayerAttackSegment>();
        [SerializeField] private PlayerAttackRecovery[] _recoveries = Array.Empty<PlayerAttackRecovery>();
        [SerializeField] private PlayerDashFollowup _dash = new();
        #endregion
        #region 外部方法
        public bool Enabled => _enabled;
        public IReadOnlyList<PlayerAttackSegment> Segments => Array.AsReadOnly(_segments);
        public IReadOnlyList<PlayerAttackRecovery> Recoveries => Array.AsReadOnly(_recoveries);
        public PlayerDashFollowup Dash => _dash;
        internal void Validate()
        {
            if (_segments == null || _recoveries == null || _dash == null) throw new InvalidOperationException("Feel.Attack：集合/冲刺配置为空。");
            var recoveries = new HashSet<string>(StringComparer.Ordinal);
            foreach (var recovery in _recoveries)
            {
                if (recovery == null) throw new InvalidOperationException("Feel.Attack.Recoveries：空项。");
                recovery.Validate();
                if (!recoveries.Add(recovery.Id)) throw new InvalidOperationException("Feel.Attack.Recoveries：重复 ID " + recovery.Id);
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var segment in _segments)
            {
                if (segment == null) throw new InvalidOperationException("Feel.Attack.Segments：空项。");
                segment.Validate();
                if (!ids.Add(segment.Id)) throw new InvalidOperationException("Feel.Attack.Segments：重复 ID " + segment.Id);
                if (!recoveries.Contains(segment.RecoveryId)) throw new InvalidOperationException("Attack[" + segment.Id + "].RecoveryId：目标不存在。");
            }
            if (_enabled && _segments.Length == 0) throw new InvalidOperationException("Feel.Attack：开启时至少需要一个攻击段。");
            _dash.Validate();
        }
        #endregion
    }

    [Serializable]
    public sealed class PlayerAttackSegment
    {
        #region 编辑器面板参数
        [SerializeField] private string _id;
        [SerializeField] private bool _enabled = true;
        [SerializeField] private string _abilityId;
        [SerializeField] private PlayerAttackEnvironment _environment;
        [SerializeField] private float _duration = .5f, _windupEnd = .1f, _activeEnd = .3f, _speed = 1;
        [SerializeField] private PlayerCancelGate _dash = new(), _jump = new(), _attack = new(), _move = new();
        [SerializeField] private PlayerAttackMotion _motion;
        [SerializeField] private PlayerAttackEntryVelocity _entryVelocity;
        [SerializeField] private Vector2 _velocity = Vector2.one;
        [SerializeField] private float _moveSpeed = 5, _acceleration = 50;
        [SerializeField] private string _recoveryId;
        [SerializeField] private PlayerHitWindow[] _windows = Array.Empty<PlayerHitWindow>();
        [SerializeField] private PlayerAttackVisual _visual = new();
        #endregion
        #region 外部方法
        public string Id => _id;
        public bool Enabled => _enabled;
        public string AbilityId => _abilityId;
        public PlayerAttackEnvironment Environment => _environment;
        public float Duration => _duration;
        public float WindupEnd => _windupEnd;
        public float ActiveEnd => _activeEnd;
        public float Speed => _speed;
        public PlayerCancelGate Dash => _dash;
        public PlayerCancelGate Jump => _jump;
        public PlayerCancelGate Attack => _attack;
        public PlayerCancelGate Move => _move;
        public PlayerAttackMotion Motion => _motion;
        public PlayerAttackEntryVelocity EntryVelocity => _entryVelocity;
        public Vector2 Velocity => _velocity;
        public float MoveSpeed => _moveSpeed;
        public float Acceleration => _acceleration;
        public string RecoveryId => _recoveryId;
        public IReadOnlyList<PlayerHitWindow> Windows => Array.AsReadOnly(_windows);
        public PlayerAttackVisual Visual => _visual;
        internal void Validate()
        {
            string p = "Attack[" + _id + "]";
            if (_visual == null) throw new InvalidOperationException(p + ".Visual：配置为空。");
            _visual.Validate(_duration, p + ".Visual");
            PlayerCombatData.Id(_id, p + ".Id"); PlayerCombatData.Id(_abilityId, p + ".AbilityId");
            PlayerCombatData.Id(_recoveryId, p + ".RecoveryId");
            PlayerCombatData.Enum(_environment, p + ".Environment"); PlayerCombatData.Enum(_motion, p + ".Motion");
            PlayerCombatData.Enum(_entryVelocity, p + ".EntryVelocity");
            PlayerCombatData.Positive(_duration, p + ".Duration"); PlayerCombatData.Positive(_speed, p + ".Speed");
            PlayerCombatData.Range(_windupEnd, 0, _duration, p + ".WindupEnd");
            PlayerCombatData.Range(_activeEnd, _windupEnd, _duration, p + ".ActiveEnd");
            PlayerCombatData.Vector(_velocity, p + ".Velocity");
            PlayerCombatData.NonNegative(_moveSpeed, p + ".MoveSpeed"); PlayerCombatData.Positive(_acceleration, p + ".Acceleration");
            if (_dash == null || _jump == null || _attack == null || _move == null || _windows == null)
                throw new InvalidOperationException(p + "：取消节点或命中集合为空。");
            _dash.Validate(_duration, p + ".Dash"); _jump.Validate(_duration, p + ".Jump");
            _attack.Validate(_duration, p + ".Attack"); _move.Validate(_duration, p + ".Move");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var window in _windows)
            {
                if (window == null) throw new InvalidOperationException(p + ".Windows：空项。");
                window.Validate(_duration, p);
                if (!ids.Add(window.Id)) throw new InvalidOperationException(p + ".Windows：重复 ID " + window.Id);
            }
        }
        #endregion
    }

    [Serializable]
    public sealed class PlayerCancelGate
    {
        [SerializeField] private bool _enabled;
        [SerializeField] private float _opensAt;
        public bool Enabled => _enabled;
        public float OpensAt => _opensAt;
        internal void Validate(float duration, string path) => PlayerCombatData.Range(_opensAt, 0, duration, path + ".OpensAt");
    }

    [Serializable]
    public sealed class PlayerAttackRecovery
    {
        [SerializeField] private string _id;
        [SerializeField] private float _normalDuration = .3f, _delayedNormalDuration = .3f, _delayedTotalDuration = 1.3f;
        public string Id => _id;
        public float NormalDuration => _normalDuration;
        public float DelayedNormalDuration => _delayedNormalDuration;
        public float DelayedTotalDuration => _delayedTotalDuration;
        internal void Validate()
        {
            string p = "Attack.Recovery[" + _id + "]";
            PlayerCombatData.Id(_id, p + ".Id");
            PlayerCombatData.NonNegative(_normalDuration, p + ".NormalDuration");
            PlayerCombatData.NonNegative(_delayedTotalDuration, p + ".DelayedTotalDuration");
            PlayerCombatData.Range(_delayedNormalDuration, 0, _delayedTotalDuration, p + ".DelayedNormalDuration");
            if (_delayedNormalDuration < _normalDuration)
                throw new InvalidOperationException(p + ".DelayedNormalDuration：特殊收刀不能压缩普通接续窗口。");
        }
    }

    [Serializable]
    public sealed class PlayerDashFollowup
    {
        [SerializeField] private PlayerDashAttackMode _mode = PlayerDashAttackMode.ContinueNormal;
        [SerializeField] private float _opensAt, _duration = .2f, _airVelocityRatio = 1, _airBraking = 30, _airTurning = 50;
        public PlayerDashAttackMode Mode => _mode;
        public float OpensAt => _opensAt;
        public float Duration => _duration;
        public float AirVelocityRatio => _airVelocityRatio;
        public float AirBraking => _airBraking;
        public float AirTurning => _airTurning;
        internal void Validate()
        {
            PlayerCombatData.Enum(_mode, "Attack.Dash.Mode");
            PlayerCombatData.NonNegative(_opensAt, "Attack.Dash.OpensAt"); PlayerCombatData.NonNegative(_duration, "Attack.Dash.Duration");
            PlayerCombatData.Range(_airVelocityRatio, 0, 1, "Attack.Dash.AirVelocityRatio");
            PlayerCombatData.Positive(_airBraking, "Attack.Dash.AirBraking"); PlayerCombatData.Positive(_airTurning, "Attack.Dash.AirTurning");
        }
    }

    /// <summary>纯值配置的复制与校验，不保存 SO、委托或运行状态。</summary>
    internal static class PlayerCombatData
    {
        internal static T Copy<T>(T value) where T : class => value == null ? null : JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        internal static void Id(string value, string path)
        { if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException(path + "：必需的稳定 ID 为空。"); }
        internal static void NonNegative(float value, string path)
        { if (!float.IsFinite(value) || value < 0) throw new InvalidOperationException(path + "：必须为有限非负数。"); }
        internal static void Positive(float value, string path)
        { if (!float.IsFinite(value) || value <= 0) throw new InvalidOperationException(path + "：必须为有限正数。"); }
        internal static void Range(float value, float min, float max, string path)
        { if (!float.IsFinite(value) || value < min || value > max) throw new InvalidOperationException(path + "：超出允许区间。"); }
        internal static void Vector(Vector2 value, string path)
        { if (!float.IsFinite(value.x) || !float.IsFinite(value.y)) throw new InvalidOperationException(path + "：必须为有限向量。"); }
        internal static void Enum<T>(T value, string path) where T : struct
        { if (!System.Enum.IsDefined(typeof(T), value)) throw new InvalidOperationException(path + "：未知枚举值。"); }
    }
}
