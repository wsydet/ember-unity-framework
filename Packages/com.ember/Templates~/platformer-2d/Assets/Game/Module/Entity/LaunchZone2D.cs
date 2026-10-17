using System;
using System.Collections.Generic;
using Ember.Basic;
using UnityEngine;

namespace Game.Entity
{
    /// <summary>按目标而非碰撞体去重；冷却按游戏秒数。禁用后不保留旧触发记录。</summary>
    public sealed class LaunchZone2D : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private string _sourceId = "launch";
        [SerializeField] private Vector2 _velocity = new(0, 10);
        [SerializeField] private LaunchMode2D _mode;
        [SerializeField, Min(0)] private float _duration;
        [SerializeField, Min(0)] private float _cooldown = .2f;
        [SerializeField] private LaunchEnd2D _end;
        [SerializeField] private LaunchInterrupt2D _interrupt = LaunchInterrupt2D.ReplaceExternalMotion;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private readonly Dictionary<MonoBehaviour, double> _accepted = new();
        private readonly List<MonoBehaviour> _expired = new();
        public event Action<ILaunchTarget2D> Launched;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void OnDisable() => _accepted.Clear();
        private void OnTriggerEnter2D(Collider2D other) => TryTrigger(other);
        private void OnTriggerStay2D(Collider2D other) => TryTrigger(other);
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [HasGC] public bool TryTrigger(Collider2D other)
        {
            if (!isActiveAndEnabled || other == null || !float.IsFinite(_cooldown) || _cooldown < 0) return false;
            foreach (var pair in _accepted) if (pair.Key == null) _expired.Add(pair.Key);
            foreach (var key in _expired) _accepted.Remove(key);
            _expired.Clear();
            foreach (var component in other.GetComponentsInParent<MonoBehaviour>())
            {
                if (component is not ILaunchTarget2D target) continue;
                double now = Time.timeAsDouble;
                if (_accepted.TryGetValue(component, out var previous) && now - previous <= Math.Max(_cooldown, .000001f)) return false;
                var request = new LaunchRequest2D(_sourceId, _velocity, _mode, _duration, _end, _interrupt);
                if (target.TryLaunch(this, request) != LaunchResult2D.Accepted) return false;
                _accepted[component] = now;
                if (Launched != null)
                    foreach (Action<ILaunchTarget2D> callback in Launched.GetInvocationList())
                        try { callback(target); }
                        catch (Exception error) { EmberDebug.LogError("Game.Entity", "弹射机关表现通知失败：" + error); }
                return true;
            }
            return false;
        }
        #endregion
    }
}
