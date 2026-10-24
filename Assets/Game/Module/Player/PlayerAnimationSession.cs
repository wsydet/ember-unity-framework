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
        private readonly PlayerController _owner;
        private readonly PlayerAttackVisualSession _attackVisual;
        private readonly Dictionary<string, PlayerAnimationSlot> _slots = new(StringComparer.Ordinal);
        private readonly List<KeyValuePair<AnimationClip, AnimationClip>> _overrides = new();
        private AnimatorOverrideController _override;
        private RuntimeAnimatorController _previousController;
        private string _node;
        private string _visualNode;
        private bool _recoveryClipFinished;
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
            _owner = owner;
            _attackVisual = new PlayerAttackVisualSession(owner, view.Sprite);
            profile.Validate(graph);
            try
            {
                _override = new AnimatorOverrideController(profile.Controller) { hideFlags = HideFlags.DontSave };
                foreach (var slot in profile.Slots)
                {
                    _slots.Add(slot.NodeId, slot);
                    _overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(slot.Key, profile.ResolveClip(slot, graph)));
                }
                _override.ApplyOverrides(_overrides);
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
            _attackVisual.Sync();
            var id = fsm.CurrentLeafId;
            var activation = fsm.GetActivation(id);
            var visual = _owner.Context?.Combat?.VisualNode(id) ?? id;
            if (_owner.Context?.Life is { } life && life.Reaction?.IsActive == true && !life.Configuration.Reactions.Animation) visual = "idle";
            bool sameActivation = _node == id && _generation == activation.Generation;
            bool recovery = _owner.Context?.Combat is { IsActive: true, IsRecovery: true };
            var logicalSlot = _slots[id];
            if (!sameActivation) _recoveryClipFinished = false;
            else if (recovery && _visualNode == id && logicalSlot.Clip != null &&
                !_animator.IsInTransition(0) && _animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1)
                _recoveryClipFinished = true;
            bool dedicatedMovement = recovery && visual == "move" && logicalSlot.MovingRecoveryClip != null;
            if (dedicatedMovement) visual = id + ":moving";
            else if (recovery && visual == id && _recoveryClipFinished) visual = "idle";
            if (_node == id && _generation == activation.Generation && _visualNode == visual) return;
            if (!_slots.TryGetValue(dedicatedMovement ? id : visual, out var slot)) throw new InvalidOperationException("活动节点或显式移动变体没有动画映射：" + visual);
            if (recovery)
            {
                // Unity 序列化空引用可能是托管占位对象，不能使用 ?? 判断 Clip 是否存在。
                var clip = dedicatedMovement ? logicalSlot.MovingRecoveryClip :
                    logicalSlot.Clip != null ? logicalSlot.Clip : _slots["idle"].Clip;
                int index = _overrides.FindIndex(pair => pair.Key == logicalSlot.Key);
                if (_overrides[index].Value != clip)
                {
                    _overrides[index] = new KeyValuePair<AnimationClip, AnimationClip>(logicalSlot.Key, clip);
                    _override.ApplyOverrides(_overrides);
                }
            }
            bool initial = _node == null;
            _node = id; _generation = activation.Generation; _speed = slot.Speed * (_owner.Context?.Combat?.AnimationSpeed ?? 1);
            _visualNode = visual; // 变体从头播放，仅改变表现；不会重置逻辑收刀计时。
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
            _attackVisual?.Dispose();
            if (_bound && _animator != null && _animator.runtimeAnimatorController == _override)
            {
                _animator.runtimeAnimatorController = _previousController;
                _animator.speed = 1;
            }
            _bound = false;
            if (_override != null) UnityEngine.Object.DestroyImmediate(_override);
            _override = null;
            _slots.Clear();
            _overrides.Clear();
        }
        #endregion
    }
}
