using System;
using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    /// <summary>每宿主独立覆盖与播放状态；只观察已提交激活，不调用 Guard 或推进 FSM。</summary>
    internal sealed class PlayerAnimationSession : IDisposable
    {
        #region 内部参数
        private readonly Animator _animator;
        private readonly Dictionary<string, PlayerAnimationSlot> _slots = new(StringComparer.Ordinal);
        private AnimatorOverrideController _override;
        private RuntimeAnimatorController _previousController;
        private string _node;
        private ulong _generation;
        private float _speed = 1;
        private bool _bound, _paused;
        internal int PlayCount { get; private set; }
        internal string NodeId => _node;
        internal AnimatorOverrideController Override => _override;
        #endregion
        #region 外部方法
        internal PlayerAnimationSession(PlayerAnimationView view, PlayerController owner, PlayerAnimationProfile profile, EmberFsmGraph graph)
        {
            _animator = view.Validate(owner);
            profile.Validate(graph);
            try
            {
                _override = new AnimatorOverrideController(profile.Controller) { hideFlags = HideFlags.DontSave };
                var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                foreach (var slot in profile.Slots)
                {
                    _slots.Add(slot.NodeId, slot);
                    pairs.Add(new KeyValuePair<AnimationClip, AnimationClip>(slot.Key, slot.Clip));
                }
                _override.ApplyOverrides(pairs);
                // 准备阶段不触碰正在运行的 Animator；无 Renderer 的临时探针只校验 Controller 目标。
                var probe = new GameObject("Player animation validation") { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    var animator = probe.AddComponent<Animator>();
                    animator.runtimeAnimatorController = _override;
                    animator.Rebind();
                    foreach (var slot in profile.Slots)
                        if (!animator.HasState(0, Animator.StringToHash(slot.State)))
                            throw new InvalidOperationException("Controller 缺少动画目标：" + slot.State);
                }
                finally { UnityEngine.Object.DestroyImmediate(probe); }
            }
            catch { Dispose(); throw; }
        }
        internal void Bind()
        {
            if (_override == null) throw new ObjectDisposedException(nameof(PlayerAnimationSession));
            if (!_bound) _previousController = _animator.runtimeAnimatorController;
            _bound = true;
            _animator.runtimeAnimatorController = _override;
            _animator.applyRootMotion = false;
            _animator.updateMode = AnimatorUpdateMode.Normal;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _animator.Rebind();
            _node = null;
        }
        internal void Sync(EmberFsm<PlayerContext> fsm)
        {
            if (!_bound || _override == null || _animator == null || string.IsNullOrEmpty(fsm.CurrentLeafId)) return;
            var id = fsm.CurrentLeafId;
            var activation = fsm.GetActivation(id);
            if (_node == id && _generation == activation.Generation) return;
            if (!_slots.TryGetValue(id, out var slot)) throw new InvalidOperationException("活动节点没有动画映射：" + id);
            bool initial = _node == null;
            _node = id; _generation = activation.Generation; _speed = slot.Speed;
            _animator.speed = _paused ? 0 : _speed;
            if (!initial && slot.BlendSeconds > 0) _animator.CrossFadeInFixedTime(slot.State, slot.BlendSeconds, 0, 0);
            else _animator.Play(slot.State, 0, 0);
            _animator.Update(0); // 首次显示前建立姿态，不推进动画时间或玩法。
            PlayCount++;
        }
        internal void Pause(bool value)
        {
            _paused = value;
            if (_bound && _animator != null) _animator.speed = value ? 0 : _speed;
        }
        public void Dispose()
        {
            if (_bound && _animator != null && _animator.runtimeAnimatorController == _override)
            {
                _animator.runtimeAnimatorController = _previousController;
                _animator.speed = 1;
            }
            _bound = false;
            if (_override != null) UnityEngine.Object.DestroyImmediate(_override);
            _override = null;
            _slots.Clear();
        }
        #endregion
    }
}
