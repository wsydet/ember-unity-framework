using System;
using System.Collections.Generic;
using Ember.Basic;
using UnityEngine;

namespace Game.Entity
{
    public enum CollapsingPlatformPhase { Ready, Counting, Hidden, WaitingForClearance }

    /// <summary>可选塌陷平台。两个独立触发探头分别定义站立范围和恢复净空，不依赖 Exit 必然到来。</summary>
    public sealed class CollapsingPlatform2D : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private string _stableId;
        [SerializeField] private Collider2D _solid;
        [SerializeField] private Collider2D _standingProbe;
        [SerializeField] private Collider2D _clearanceProbe;
        [SerializeField] private LayerMask _occupants = 1;
        [SerializeField, Min(0)] private float _collapseDelay = .6f;
        [SerializeField, Min(0)] private float _recoveryDelay = 2f;
        [SerializeField] private bool _collapseOnLeave = true;
        [SerializeField] private bool _canRecover = true;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private readonly List<Collider2D> _overlaps = new(8);
        private bool _paused;
        private bool _configured;
        private float _remaining;
        public string StableId => _stableId;
        public CollapsingPlatformPhase Phase { get; private set; }
        public float RemainingSeconds => _remaining;
        public event Action<CollapsingPlatformPhase> PhaseChanged;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void OnEnable()
        {
            _configured = !string.IsNullOrWhiteSpace(_stableId) && _solid != null && !_solid.isTrigger &&
                _standingProbe != null && _clearanceProbe != null && _standingProbe != _solid &&
                _clearanceProbe != _solid && _standingProbe != _clearanceProbe && _standingProbe.isTrigger && _clearanceProbe.isTrigger &&
                float.IsFinite(_collapseDelay) && _collapseDelay >= 0 && float.IsFinite(_recoveryDelay) && _recoveryDelay >= 0;
            if (!_configured) { EmberDebug.LogError("Game.Entity", "塌陷平台需要稳定ID、实体碰撞体和两个独立触发探头，以及有效秒数。", this); return; }
            RequestReset();
        }
        private void FixedUpdate()
        {
            if (!_configured || _paused) return;
            // 探头必须持续存在；失效时保持不可恢复，避免在玩家内部重新打开碰撞。
            if (_solid == null) return;
            if (_standingProbe == null || _clearanceProbe == null ||
                !_standingProbe.isActiveAndEnabled || !_clearanceProbe.isActiveAndEnabled) return;
            Advance(Time.fixedDeltaTime, Occupied(_standingProbe), Occupied(_clearanceProbe));
        }
        private void OnDisable() { if (_configured && _solid != null) _solid.enabled = false; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private bool Occupied(Collider2D probe)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(_occupants);
            _overlaps.Clear(); probe.Overlap(filter, _overlaps);
            foreach (var collider in _overlaps)
                if (collider != _solid && collider != _standingProbe && collider != _clearanceProbe &&
                    !collider.transform.IsChildOf(transform)) return true;
            return false;
        }
        private void Change(CollapsingPlatformPhase phase, float remaining)
        {
            bool changed = Phase != phase;
            Phase = phase; _remaining = remaining;
            _solid.enabled = phase == CollapsingPlatformPhase.Ready || phase == CollapsingPlatformPhase.Counting;
            if (!changed || PhaseChanged == null) return;
            foreach (Action<CollapsingPlatformPhase> callback in PhaseChanged.GetInvocationList())
                try { callback(phase); }
                catch (Exception error) { EmberDebug.LogError("Game.Entity", "塌陷平台表现回调失败：" + error); }
        }
        private void Advance(float dt, bool standing, bool occupied)
        {
            switch (Phase)
            {
                case CollapsingPlatformPhase.Ready:
                    if (standing) Change(CollapsingPlatformPhase.Counting, _collapseDelay);
                    break;
                case CollapsingPlatformPhase.Counting:
                    _remaining = Mathf.Max(0, _remaining - dt);
                    if (_remaining <= 0 || _collapseOnLeave && !standing)
                        Change(CollapsingPlatformPhase.Hidden, _recoveryDelay);
                    break;
                case CollapsingPlatformPhase.Hidden:
                    if (!_canRecover) break;
                    _remaining = Mathf.Max(0, _remaining - dt);
                    if (_remaining <= 0) Change(CollapsingPlatformPhase.WaitingForClearance, 0);
                    break;
                case CollapsingPlatformPhase.WaitingForClearance:
                    if (!occupied) Change(CollapsingPlatformPhase.Ready, 0);
                    break;
            }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void SetPaused(bool paused) => _paused = paused;
        /// <summary>显式复位同样等待净空；不会把碰撞直接恢复到占位玩家内部。</summary>
        public void RequestReset()
        {
            if (!_configured || _solid == null) return;
            Change(CollapsingPlatformPhase.WaitingForClearance, 0);
        }
        #endregion
    }
}
