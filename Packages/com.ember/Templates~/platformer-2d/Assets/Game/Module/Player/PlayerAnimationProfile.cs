using System;
using System.Collections.Generic;
using Ember.Core.Fsm;
using UnityEngine;

namespace Game.Player
{
    /// <summary>方案的视觉配置。循环/末帧保持由片段导入设置控制，不参与玩法退出。</summary>
    [CreateAssetMenu(menuName = "Game/Player/Animation Profile", fileName = "PlayerAnimationProfile")]
    public sealed class PlayerAnimationProfile : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField] private RuntimeAnimatorController _controller;
        [SerializeField] private PlayerAnimationSlot[] _slots = Array.Empty<PlayerAnimationSlot>();
        #endregion
        #region 外部方法
        public RuntimeAnimatorController Controller => _controller;
        public IReadOnlyList<PlayerAnimationSlot> Slots => _slots;
        internal void Validate(EmberFsmGraph graph)
        {
            if (_controller == null || graph == null || _slots == null)
                throw new InvalidOperationException("动画方案缺少 Controller、图或映射。");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var keys = new HashSet<AnimationClip>();
            var states = new HashSet<string>(StringComparer.Ordinal);
            var available = new HashSet<AnimationClip>(_controller.animationClips);
            foreach (var slot in _slots)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.NodeId) || !ids.Add(slot.NodeId))
                    throw new InvalidOperationException("动画映射存在空或重复节点 ID。");
                if (slot.Key == null || slot.Clip == null || !keys.Add(slot.Key) || !available.Contains(slot.Key))
                    throw new InvalidOperationException("动画映射缺少片段、覆盖键重复或 Controller 不包含该键：" + slot.NodeId);
                if (string.IsNullOrWhiteSpace(slot.State) || !states.Add(slot.State) ||
                    !float.IsFinite(slot.Speed) || slot.Speed <= 0 || !float.IsFinite(slot.BlendSeconds) || slot.BlendSeconds < 0)
                    throw new InvalidOperationException("动画状态目标或播放参数无效：" + slot.NodeId);
                if (slot.Clip.legacy || slot.Clip.events.Length != 0 || slot.Key.legacy || slot.Key.events.Length != 0)
                    throw new InvalidOperationException("玩家视觉片段不能使用 Legacy 或 AnimationEvent：" + slot.NodeId);
            }
            if (ids.Count != graph.Nodes.Count) throw new InvalidOperationException("动画映射必须与全部图节点一一对应。");
            foreach (var node in graph.Nodes)
                if (!ids.Contains(node.Id)) throw new InvalidOperationException("动画映射缺少节点：" + node.Id);
#if UNITY_EDITOR
            ValidateEditorContent();
#endif
        }
#if UNITY_EDITOR
        private void ValidateEditorContent()
        {
            var controller = _controller as UnityEditor.Animations.AnimatorController;
            if (controller == null || controller.layers.Length != 1 || controller.parameters.Length != 0)
                throw new InvalidOperationException("玩家动画需要单层、无参数的基础 AnimatorController。");
            var machine = controller.layers[0].stateMachine;
            if (machine.stateMachines.Length != 0 || machine.anyStateTransitions.Length != 0 || machine.entryTransitions.Length != 0 ||
                machine.behaviours.Length != 0 || machine.states.Length != _slots.Length)
                throw new InvalidOperationException("玩家 Animator 不允许嵌套、自动转换或状态回调。");
            foreach (var slot in _slots)
            {
                bool found = false;
                foreach (var child in machine.states)
                {
                    if (controller.layers[0].name + "." + child.state.name != slot.State) continue;
                    found = child.state.motion == slot.Key && child.state.transitions.Length == 0 && child.state.behaviours.Length == 0 &&
                        child.state.speed == 1 && !child.state.speedParameterActive && !child.state.timeParameterActive;
                }
                if (!found) throw new InvalidOperationException("动画目标必须直接引用独占键且无自动转换/回调：" + slot.NodeId);
                foreach (var clip in new[] { slot.Key, slot.Clip })
                {
                    foreach (var curve in UnityEditor.AnimationUtility.GetCurveBindings(clip))
                        if (!ValidVisualPath(curve.path) || curve.type != typeof(Transform) ||
                            !(curve.propertyName.StartsWith("m_LocalPosition.", StringComparison.Ordinal) ||
                              curve.propertyName.StartsWith("m_LocalScale.", StringComparison.Ordinal) ||
                              curve.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal) ||
                              curve.propertyName.StartsWith("localEulerAnglesRaw.", StringComparison.Ordinal)))
                            throw new InvalidOperationException("片段只允许 Sprite 子树的局部姿态曲线：" + clip.name);
                    foreach (var curve in UnityEditor.AnimationUtility.GetObjectReferenceCurveBindings(clip))
                        if (!ValidVisualPath(curve.path) || curve.type != typeof(SpriteRenderer) || curve.propertyName != "m_Sprite")
                            throw new InvalidOperationException("片段只允许 Sprite 子树的 Sprite 引用曲线：" + clip.name);
                }
            }
        }
        private static bool ValidVisualPath(string path) => !path.Contains("..") &&
            (path == "Sprite" || path.StartsWith("Sprite/", StringComparison.Ordinal));
#endif
        #endregion
    }

    [Serializable]
    public sealed class PlayerAnimationSlot
    {
        #region 编辑器面板参数
        [SerializeField] private string _nodeId;
        [Tooltip("完整 Animator 状态路径；仅作视觉输出目标。")]
        [SerializeField] private string _state;
        [Tooltip("Controller 中该节点独占的原始片段引用；不是按名称查找。")]
        [SerializeField] private AnimationClip _key;
        [SerializeField] private AnimationClip _clip;
        [SerializeField, Min(.01f)] private float _speed = 1;
        [SerializeField, Min(0)] private float _blendSeconds;
        #endregion
        #region 外部方法
        public string NodeId => _nodeId;
        public string State => _state;
        public AnimationClip Key => _key;
        public AnimationClip Clip => _clip;
        public float Speed => _speed;
        public float BlendSeconds => _blendSeconds;
        #endregion
    }
}
