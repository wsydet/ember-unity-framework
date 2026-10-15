using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Core.Fsm;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    /// <summary>一次 Gameplay 准备的私有配置快照。恢复共享只读装配，运行资源仍每候选独立。</summary>
    internal sealed class PlayerSchemeCapture : IDisposable
    {
        #region 内部参数
        private readonly Dictionary<ScriptableObject, ScriptableObject> _copies = new();
        private readonly List<ScriptableObject> _nodeCopies = new();
        private int _references = 1;
        internal PlayerConfig Config { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private ScriptableObject Copy(ScriptableObject source)
        {
            if (source == null) return null;
            if (_copies.TryGetValue(source, out var existing)) return existing;
            var copy = UnityEngine.Object.Instantiate(source);
            copy.hideFlags = HideFlags.DontSave;
            _copies.Add(source, copy);
            return copy;
        }
        private EmberFsmStateDefinition CaptureDefinition(EmberFsmNode node,
            Dictionary<string, PlayerActionEffectSlot> slots)
        {
            var source = node.Definition;
            if (source is PlayerDashAttackRouteDefinition) return (EmberFsmStateDefinition)Copy(source);
            // 仅历史纯逻辑夹具允许显式注入运行字段；绝不读取旧序列化字段。
            if (source is PlayerGroundDashStateDefinition ground)
            {
                var copy = UnityEngine.Object.Instantiate(ground);
                copy.hideFlags = HideFlags.DontSave; _nodeCopies.Add(copy);
                var slot = slots == null ? null : slots[node.Id];
                copy.SetCapturedEffects((EntityEffectDefinition)Copy(slots == null ? ground.ActionEffect : slot.ActionEffect),
                    (EntityEffectDefinition)Copy(slots == null ? ground.TailEffect : slot.TailEffect));
                return copy;
            }
            if (source is PlayerAirDashStateDefinition air)
            {
                var copy = UnityEngine.Object.Instantiate(air);
                copy.hideFlags = HideFlags.DontSave; _nodeCopies.Add(copy);
                var slot = slots == null ? null : slots[node.Id];
                copy.SetCapturedEffects((EntityEffectDefinition)Copy(slots == null ? air.ActionEffect : slot.ActionEffect),
                    (EntityEffectDefinition)Copy(slots == null ? air.TailEffect : slot.TailEffect));
                return copy;
            }
            // 其余玩家 Definition 仅声明代码与转换，无可变配置字段。
            return source;
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        internal PlayerSchemeCapture(PlayerConfig source)
        {
            try
            {
                if (source == null || source.Graph == null || source.Feel == null)
                    throw new InvalidOperationException("完整玩家方案缺少图或手感配置。");
                Dictionary<string, PlayerActionEffectSlot> slots = null;
                if (source.Scheme != null)
                {
                    if (source.Scheme.ActionEffects == null)
                        throw new InvalidOperationException("完整玩家方案尚未迁移动作效果；请执行显式等价迁移。");
                    slots = source.Scheme.ActionEffects.Validate(source.Graph);
                }
                var graph = (EmberFsmGraph)Copy(source.Graph);
                graph.Configure(source.Graph.EntryId, source.Graph.Nodes.Select(node => new EmberFsmNode(
                    node.Id, node.DisplayName, CaptureDefinition(node, slots), node.ParentId, node.DefaultChildId,
                    node.Bindings.Select(binding => new EmberFsmTargetBinding(binding.Slot, binding.TargetId)).ToArray())).ToArray());
                var powers = (PlayerPowerConfig)Copy(source.Powers);
                if (powers != null) powers.CaptureSlots(source.Powers, Copy);
                var feel = (PlayerFeelProfile)Copy(source.Feel);
                feel.Configure((PlayerMoveFeel)Copy(source.Feel.Movement), (PlayerJumpFeel)Copy(source.Feel.Jump),
                    (PlayerInputFeel)Copy(source.Feel.Input));
                Config = (PlayerConfig)Copy(source);
                var animation = (PlayerAnimationProfile)Copy(source.Animation);
                if (source.Scheme != null && animation == null)
                    throw new InvalidOperationException("完整玩家方案缺少动画配置。");
                if (animation != null) animation.Validate(graph);
                var rules = source.Scheme != null ? PlayerSchemeRules.Capture(source.Scheme) : source.CapturedRules;
                Config.SetCapturedAssembly(source, graph, powers, feel, animation, rules);
                Config.CapturedCombat.Graph.Validate(graph, Config.CapturedFeel.Attack, Config.CapturedCombat.Combo);
            }
            catch { Dispose(); throw; }
        }
        internal PlayerSchemeCapture Retain()
        {
            if (_references == 0) throw new ObjectDisposedException(nameof(PlayerSchemeCapture));
            _references++; return this;
        }
        public void Dispose()
        {
            if (_references == 0 || --_references != 0) return;
            foreach (var copy in _nodeCopies) if (copy != null) UnityEngine.Object.DestroyImmediate(copy);
            _nodeCopies.Clear();
            foreach (var copy in _copies.Values)
                if (copy != null) UnityEngine.Object.DestroyImmediate(copy);
            _copies.Clear(); Config = null;
        }
        #endregion
    }
}
