using System;
using System.Linq;
using System.Reflection;
using Ember.Core.Fsm;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private static EmberConditionExpression E02Velocity(float threshold) => new("velocity", new EmberConditionNode("velocity", "player.velocity", new[] {
            new EmberConditionParameter("axis", EmberConditionValueKind.Choice, choice: 1),
            new EmberConditionParameter("absolute", EmberConditionValueKind.Boolean),
            new EmberConditionParameter("compare", EmberConditionValueKind.Choice, choice: 1),
            new EmberConditionParameter("threshold", EmberConditionValueKind.Number, number: threshold) }));
        private void E02Install(EmberFsmTransition handle, EmberConditionExpression expression)
        {
            var plan = new EmberFsmRulePlan<PlayerContext>(new EmberFsmRuleBinding<PlayerContext>("air", handle, "fixed", PlayerRuleConditions.CreateCatalog(null).Compile(expression)));
            typeof(PlayerConfig).GetProperty("CapturedRules", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_config, plan);
        }
        [Test] public void E02WallSlideThresholdOnlyRestrictsEntryAndDifferentHostsKeepFrozenPlans()
        {
            InstallWalls(); E02Install(PlayerAirState.WallSlide, E02Velocity(-5));
            var first = AtRightWall(vy: -1); Keys(Key.D); Decide(first);
            Assert.That(first.CurrentStateId, Is.EqualTo("fall"));
            first.Context.Motor.Body.linearVelocity = new Vector2(0, -5); Decide(first);
            Assert.That(first.CurrentStateId, Is.EqualTo("wall-slide"));
            Decide(first); Assert.That(first.CurrentStateId, Is.EqualTo("wall-slide"));
            Assert.That(first.Context.Motor.Velocity.y, Is.EqualTo(-.5f));
            E02Install(PlayerAirState.WallSlide, E02Velocity(-20));
            var second = Character(new Vector2(0, 8), new Vector2(0, -5)); Keys(Key.D); Decide(second);
            Assert.That(second.CurrentStateId, Is.EqualTo("fall"));
            Assert.That(first.CaptureFsmObservation().Rules[0].Identity, Is.Not.EqualTo(second.CaptureFsmObservation().Rules[0].Identity));
            Decide(first); Assert.That(first.CurrentStateId, Is.EqualTo("wall-slide"));
        }
        [Test] public void E02AirJumpRejectionKeepsInputAndUsesThenCommitsExactlyOnce()
        {
            InstallAirJump();
            E02Install(PlayerAirState.AirJump, new EmberConditionExpression("up", new EmberConditionNode("up", "player.input-axis", new[] {
                new EmberConditionParameter("axis", EmberConditionValueKind.Choice, choice: 1), new EmberConditionParameter("absolute", EmberConditionValueKind.Boolean),
                new EmberConditionParameter("compare", EmberConditionValueKind.Choice, choice: 2), new EmberConditionParameter("threshold", EmberConditionValueKind.Number, number: .5f) })));
            var player = Character(new Vector2(0, 8)); Keys(Key.Space); player.Tick(Step);
            var fsm = Fsm(player); Assert.That(fsm.Request(fsm.GetActivation("air"), PlayerAirState.AirJump), Is.EqualTo(EmberFsmResult.GuardRejected));
            Assert.That(player.Context.Input.JumpRequested, Is.True); Assert.That(AirResource(player).RemainingUses, Is.EqualTo(1));
            Keys(Key.W, Key.Space); player.Tick(Step);
            Assert.That(fsm.Request(fsm.GetActivation("air"), PlayerAirState.AirJump), Is.EqualTo(EmberFsmResult.Changed));
            Assert.That(player.Context.Input.JumpRequested, Is.False); Assert.That(AirResource(player).RemainingUses, Is.Zero);
        }
        [Test] public void E02HangRefusalContinuesToWallSlideWithoutHangConsumption()
        {
            InstallFormalLedge(); E02Install(PlayerAirState.Hang, new EmberConditionExpression("not", new EmberConditionNode("not", "not", null, "yes"), new EmberConditionNode("yes", "true")));
            LedgePlatform(); var player = Character(); Keys(Key.D);
            for (int i = 0; i < 20 && player.CurrentStateId != "wall-slide"; i++) Simulate(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("wall-slide"));
            Assert.That(LedgeResource(player, "ledge-hang").RemainingUses, Is.EqualTo(2));
            Assert.That(player.CaptureFsmObservation().History.Any(h => h.TransitionId == "ledge-hang" && h.Attempt?.BaseAllowed == true && h.Attempt.Allowed == false), Is.True);
        }
        [Test] public void E02SchemeSaveReloadCaptureAndRetainPreserveSessionRules()
        {
#if UNITY_EDITOR
            string folder = "Assets/E02Behavior_" + Guid.NewGuid().ToString("N");
            UnityEditor.AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(folder));
            var original = _config; PlayerSchemeCapture first = null, next = null; PlayerController a = null, b = null;
            try
            {
                var workflow = Type.GetType("Game.Player.Editor.PlayerSchemeWorkflow, Game.Player.Editor", true);
                var builtin = Resources.Load<PlayerScheme>("Config/Player/BuiltinSchemes/空洞骑士风格");
                string path = folder + "/Custom.asset";
                var scheme = (PlayerScheme)workflow.GetMethod("Copy").Invoke(null, new object[] { builtin, path, false });
                // PlayMode fixture assembles its own asset; explicit editor migration is covered in EditMode.
                var rules = PlayerSchemeRules.CreateNeutral(scheme);
                UnityEditor.AssetDatabase.AddObjectToAsset(rules, scheme);
                SetAirField(scheme, "_rules", rules); SetAirField(scheme, "_ruleMode", PlayerRuleMode.Configured);
                UnityEditor.EditorUtility.SetDirty(scheme);
                var rule = scheme.Rules.Rules.Single(r => r.TransitionId == "wall-slide");
                SetAirField(rule, "_condition", E02Velocity(-5)); UnityEditor.EditorUtility.SetDirty(scheme.Rules);
                workflow.GetMethod("Save").Invoke(null, new object[] { scheme });
                UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport | UnityEditor.ImportAssetOptions.ForceUpdate);
                scheme = UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerScheme>(path);
                var source = Keep(UnityEngine.Object.Instantiate(Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH))); SetAirField(source, "_scheme", scheme);
                first = new PlayerSchemeCapture(source); _config = first.Config;
                typeof(PlayerConfig).GetProperty("CapturedAnimation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_config, null);
                a = AtRightWall(vy: -1); Keys(Key.D); Decide(a); Assert.That(a.CurrentStateId, Is.EqualTo("fall"));
                a.Context.Motor.Body.linearVelocity = new Vector2(0, -5); Decide(a); Assert.That(a.CurrentStateId, Is.EqualTo("wall-slide"));
                SetAirField(scheme.Rules.Rules.Single(r => r.TransitionId == "wall-slide"), "_condition", E02Velocity(-20));
                using (var retained = first.Retain()) Assert.That(retained.Config.CapturedRules, Is.SameAs(first.Config.CapturedRules));
                next = new PlayerSchemeCapture(source); _config = next.Config;
                typeof(PlayerConfig).GetProperty("CapturedAnimation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_config, null);
                b = Character(new Vector2(0, 8), new Vector2(0, -5)); Keys(Key.D); Decide(b);
                Assert.That(b.CurrentStateId, Is.EqualTo("fall")); Decide(a); Assert.That(a.CurrentStateId, Is.EqualTo("wall-slide"));
                Assert.That(a.CaptureFsmObservation().Rules.Single(r => r.TransitionId == "wall-slide").Identity,
                    Is.Not.EqualTo(b.CaptureFsmObservation().Rules.Single(r => r.TransitionId == "wall-slide").Identity));
            }
            finally { a?.Stop(); b?.Stop(); first?.Dispose(); next?.Dispose(); _config = original; UnityEditor.AssetDatabase.DeleteAsset(folder); }
#else
            Assert.Ignore("完整方案资产链需要Unity Editor。");
#endif
        }
    }
}
