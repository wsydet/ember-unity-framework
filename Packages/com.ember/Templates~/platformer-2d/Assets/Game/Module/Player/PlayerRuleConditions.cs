using System;
using System.Collections.Generic;
using System.Linq;
using Ember.Basic;
using Ember.Core.Fsm;
using Game.Entity;

namespace Game.Player
{
    /// <summary>玩家只读事实积木；不采样、不消费、不创建动作。</summary>
    public static class PlayerRuleConditions
    {
        #region 内部方法
        private static EmberConditionParameterSpec Choice(string name, string label, params string[] values)
            => new(name, label, EmberConditionValueKind.Choice, values);
        private static readonly EmberConditionParameterSpec[] Threshold =
        {
            Choice("axis", "方向", "水平 X", "竖直 Y"),
            new("absolute", "取绝对值", EmberConditionValueKind.Boolean),
            Choice("compare", "比较", "<", "<=", ">", ">="),
            new("threshold", "阈值", EmberConditionValueKind.Number)
        };
        private static string ThresholdDescription(IReadOnlyList<EmberConditionParameter> p, string label, string unit)
            => label + (p[0].Choice == 0 ? " X" : " Y") + (p[1].Boolean ? "绝对值" : "") + " "
                + new[] { "<", "<=", ">", ">=" }[p[2].Choice] + " " + p[3].Number.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + " " + unit;
        private static Func<PlayerContext, bool, EmberConditionSample> CompileThreshold(IReadOnlyList<EmberConditionParameter> p, bool input)
        {
            int axis = p[0].Choice, op = p[2].Choice; bool abs = p[1].Boolean; float limit = p[3].Number;
            if (abs && limit < 0 || input && (limit < -1 || limit > 1)) throw new ArgumentException("输入阈值应在[-1,1]内；绝对值阈值不可为负。");
            return (ctx, trace) =>
            {
                float value = input ? (axis == 0 ? ctx.Input.Horizontal : ctx.Input.Vertical) : (axis == 0 ? ctx.Motor.Velocity.x : ctx.Motor.Velocity.y);
                if (abs) value = Math.Abs(value);
                bool result = op switch { 0 => value < limit, 1 => value <= limit, 2 => value > limit, _ => value >= limit };
                return new EmberConditionSample(result, trace ? value.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + (input ? " 归一化输入" : " u/s") : null);
            };
        }
        private static Func<PlayerContext, bool, EmberConditionSample> CompilePower(IReadOnlyList<EmberConditionParameter> p, PlayerPowerConfig powers)
        {
            bool skill = p[0].Choice == 1; string id = p[1].Text;
            if (string.IsNullOrWhiteSpace(id) || powers == null || (skill
                ? !powers.SkillSlots.Any(s => s?.Definition != null && s.Definition.Id == id)
                : !powers.AbilitySlots.Any(s => s?.Definition != null && s.Definition.Id == id)))
                throw new ArgumentException("本方案未装配该" + (skill ? "Skill" : "Ability") + "：" + id);
            return (ctx, trace) =>
            {
                var status = skill ? ctx.Skills.Query(id) : ctx.Abilities.Query(id);
                return new EmberConditionSample(status == EntityUseStatus.Available, trace ? status.ToString() : null);
            };
        }
        private static Func<PlayerContext, bool, EmberConditionSample> CompileFact(IReadOnlyList<EmberConditionParameter> p, bool input)
        {
            int fact = p[0].Choice;
            return (ctx, trace) =>
            {
                bool value = input ? fact switch { 0 => ctx.Input.JumpRequested, 1 => ctx.Input.FreshJumpRequested, 2 => ctx.Input.JumpHeld, _ => ctx.Input.DashRequested }
                    : fact switch { 0 => ctx.Sensors.IsGrounded, 1 => ctx.JumpWindow.HasGroundSupport, _ => ctx.JumpWindow.CanJump };
                return new EmberConditionSample(value, trace ? value.ToString() : null);
            };
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        [HasGC]
        public static EmberConditionCatalog<PlayerContext> CreateCatalog(PlayerScheme scheme)
        {
            var input = Choice("fact", "输入事实", "跳跃请求（含缓冲）", "本步新按跳跃", "当前按住跳跃", "本步冲刺请求");
            var support = Choice("fact", "支撑事实", "地面探测", "有效地面支撑", "地面/宽限起跳机会");
            return new EmberConditionCatalog<PlayerContext>(
                new("player.input-axis", "输入轴阈值（归一化）", Threshold, p => CompileThreshold(p, true), p => ThresholdDescription(p, "输入", "归一化")),
                new("player.velocity", "速度阈值（u/s）", Threshold, p => CompileThreshold(p, false), p => ThresholdDescription(p, "速度", "u/s")),
                new("player.input-fact", "输入事实", new[] { input }, p => CompileFact(p, true), p => input.Choices[p[0].Choice]),
                new("player.support", "支撑事实", new[] { support }, p => CompileFact(p, false), p => support.Choices[p[0].Choice]),
                new("player.power", "能力资源可用", new[] { Choice("kind", "能力类别", "Ability", "Skill"), new EmberConditionParameterSpec("id", "稳定能力 ID", EmberConditionValueKind.Text) },
                    p => CompilePower(p, scheme?.Powers), p => (p[0].Choice == 0 ? "Ability" : "Skill") + " " + p[1].Text + " 资源可用"));
        }
        #endregion
    }
}
