using System.Collections.Generic;
using Ember.Core.Fsm;

namespace Game.Player
{
    internal static class PlayerCombatTransitions
    {
        internal static EmberFsmTransition[] Build<T>(bool recovery, bool delayed = false) where T : class, IEmberFsmState<PlayerContext>
        {
            var result = new List<EmberFsmTransition>
            {
                new EmberFsmTransition<PlayerContext,T>("environment-ground", "ground", sites: new[] { PlayerRule.Fixed<T>(0,
                    (s,c) => c.Combat.EnvironmentLost && c.JumpWindow.HasGroundSupport, "地面专用/空中专用段失去环境") }),
                new EmberFsmTransition<PlayerContext,T>("environment-air", "fall", sites: new[] { PlayerRule.Fixed<T>(1,
                    (s,c) => c.Combat.EnvironmentLost && !c.JumpWindow.HasGroundSupport, "地面专用/空中专用段失去环境") }),
                new EmberFsmTransition<PlayerContext,T>("cancel-ground-dash", "ground-dash", sites: new[] { PlayerRule.Fixed<T>(10,
                    (s,c) => c.Combat.Gate(PlayerCombatButton.Dash) && c.Dash.CanStart(true), "冲刺取消开放且资源可用", commit:(s,c) => c.Combat.PrepareDash()) }),
                new EmberFsmTransition<PlayerContext,T>("cancel-air-dash", "air-dash", sites: new[] { PlayerRule.Fixed<T>(11,
                    (s,c) => c.Combat.Gate(PlayerCombatButton.Dash) && c.Dash.CanStart(false), "冲刺取消开放且资源可用", commit:(s,c) => c.Combat.PrepareDash()) }),
                new EmberFsmTransition<PlayerContext,T>("cancel-jump", "jump", sites: new[] { PlayerRule.Fixed<T>(20,
                    (s,c) => c.Combat.Gate(PlayerCombatButton.Jump) && c.CanStartJump, "跳跃取消开放且可以起跳", commit:(s,c) => c.Combat.PreserveForTransition()) }),
                new EmberFsmTransition<PlayerContext,T>("cancel-air-jump", "air-jump", sites: new[] { PlayerRule.Fixed<T>(21,
                    (s,c) => c.Combat.Gate(PlayerCombatButton.Jump) && c.CanStartAirJump, "空跳取消开放且资源可用", commit:(s,c) => c.Combat.PreserveForTransition()) }),
                new EmberFsmTransition<PlayerContext,T>("continue-normal", "normal", mode: EmberFsmTransitionMode.ReenterTargetBranch, sites: new[] { PlayerRule.Fixed<T>(30,
                    (s,c) => c.Combat.Gate(PlayerCombatButton.Attack) && c.Combat.CanAttack(c.Combat.Next), "普通下一段开放且可提交", commit:(s,c) => c.Combat.CommitAttack(c.Combat.Next)) }),
                new EmberFsmTransition<PlayerContext,T>("cancel-move-ground", "ground", sites: new[] { PlayerRule.Fixed<T>(40,
                    (s,c) => c.Combat.MoveCancel && c.JumpWindow.HasGroundSupport, "站桩移动取消断连") }),
                new EmberFsmTransition<PlayerContext,T>("cancel-move-air", "fall", sites: new[] { PlayerRule.Fixed<T>(41,
                    (s,c) => c.Combat.MoveCancel && !c.JumpWindow.HasGroundSupport, "站桩移动取消断连") })
            };
            if (recovery)
            {
                if (delayed) result.Add(new EmberFsmTransition<PlayerContext,T>("continue-delayed", "delayed", mode: EmberFsmTransitionMode.ReenterTargetBranch,
                    sites: new[] { PlayerRule.Fixed<T>(29, (s,c) => c.Combat.CanAttack(c.Combat.Delayed), "停顿窗口和目标资格同时有效",
                        commit:(s,c) => c.Combat.CommitAttack(c.Combat.Delayed)) }));
                result.Add(new EmberFsmTransition<PlayerContext,T>("restart-ground", "restart-ground", sites: new[] { PlayerRule.Fixed<T>(31,
                    (s,c) => string.IsNullOrEmpty(c.Combat.Next) && c.JumpWindow.HasGroundSupport && c.Combat.CanAttack(c.Combat.Entry), "末段收刀的新按键起手", commit:(s,c) => c.Combat.CommitAttack(c.Combat.Entry)) }));
                result.Add(new EmberFsmTransition<PlayerContext,T>("restart-air", "restart-air", sites: new[] { PlayerRule.Fixed<T>(32,
                    (s,c) => string.IsNullOrEmpty(c.Combat.Next) && !c.JumpWindow.HasGroundSupport && c.Combat.CanAttack(c.Combat.Entry), "末段收刀的新按键起手", commit:(s,c) => c.Combat.CommitAttack(c.Combat.Entry)) }));
                result.Add(new EmberFsmTransition<PlayerContext,T>("finish-ground", "finish", sites: new[] { PlayerRule.Fixed<T>(90,
                    (s,c) => c.Combat.Finished && c.JumpWindow.HasGroundSupport, "收刀游戏秒结束") }));
                result.Add(new EmberFsmTransition<PlayerContext,T>("finish-air", "fall", sites: new[] { PlayerRule.Fixed<T>(91,
                    (s,c) => c.Combat.Finished && !c.JumpWindow.HasGroundSupport, "收刀游戏秒结束") }));
            }
            else
            {
                if (delayed) result.Add(new EmberFsmTransition<PlayerContext,T>("finish-special", "finish-special", sites: new[] { PlayerRule.Fixed<T>(89,
                    (s,c) => c.Combat.Finished && c.Combat.CanSelectDelayed, "本体结束且停顿能力开启", commit:(s,c) => c.Combat.PreserveForTransition()) }));
                result.Add(new EmberFsmTransition<PlayerContext,T>("finish", "finish", sites: new[] { PlayerRule.Fixed<T>(90,
                    (s,c) => c.Combat.Finished, "本体自然进入逻辑收刀", commit:(s,c) => c.Combat.PreserveForTransition()) }));
            }
            return result.ToArray();
        }
    }
}
