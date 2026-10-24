using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        [TestCase("HollowKnightStyle", false)]
        [TestCase("CelesteStyle", false)]
        [TestCase("CelesteStyle", true)]
        public void CompleteSchemeWorkflowRunsRealMoveJumpFallAndKeepsCapturedHostsIndependent(string style, bool blank)
        {
#if UNITY_EDITOR
            string folder = "Assets/SchemeBehaviorTest_" + Guid.NewGuid().ToString("N");
            UnityEditor.AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(folder));
            var original = _config;
            PlayerSchemeCapture first = null, second = null;
            PlayerController player = null, other = null;
            try
            {
                var workflow = Type.GetType("Game.Player.Editor.PlayerSchemeWorkflow, Game.Player.Editor", true);
                var builtin = Resources.Load<PlayerScheme>("Config/Player/BuiltinSchemes/" +
                    (style == "HollowKnightStyle" ? "空洞骑士风格" : "蔚蓝风格"));
                Assert.That(builtin, Is.Not.Null);
                var scheme = (PlayerScheme)workflow.GetMethod("Copy").Invoke(null, new object[] { builtin, folder + "/Custom.asset", blank });
                var source = Keep(UnityEngine.Object.Instantiate(Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH)));
                PlayerDashEffectTestAssets.Set(source, "_scheme", scheme);
                first = new PlayerSchemeCapture(source); _config = first.Config;
                typeof(PlayerConfig).GetProperty("CapturedAnimation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_config, null);
                player = Character(new Vector2(-3, .9f)); Keys(); Land(player);
                float speed = player.Context.Feel.MoveSpeed;
                Keys(Key.D); for (int n = 0; n < 10; n++) Simulate(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("move"));
                Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(speed).Within(.02f));
                Keys(); for (int n = 0; n < 30; n++) Simulate(player);
                Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
                float baseline = player.Context.Motor.Position.y; float peak = baseline; bool fell = false;
                Keys(Key.Space);
                for (int n = 0; n < 150; n++)
                {
                    Simulate(player); peak = Mathf.Max(peak, player.Context.Motor.Position.y); fell |= player.CurrentStateId == "fall";
                    if (n > 1 && player.Context.JumpWindow.HasGroundSupport) break;
                }
                Assert.That(peak - baseline, Is.GreaterThan(.5f)); Assert.That(fell, Is.True);
                Assert.That(player.Context.JumpWindow.HasGroundSupport, Is.True);
                Keys(); Simulate(player); Keys(Key.LeftShift); Decide(player);
                Assert.That(player.CurrentStateId == "ground-dash", Is.EqualTo(!blank));
                Assert.That(player.Context.Effects, Is.Null, "现有内置与空白均明确无附加效果");
                PlayerDashEffectTestAssets.Set(scheme.Feel.Movement, "_speed", speed + 2);
                second = new PlayerSchemeCapture(source); _config = second.Config;
                typeof(PlayerConfig).GetProperty("CapturedAnimation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_config, null);
                Keys(); other = Character(new Vector2(3, .9f)); Land(other);
                Assert.That(player.Context.Feel.MoveSpeed, Is.EqualTo(speed));
                Assert.That(other.Context.Feel.MoveSpeed, Is.EqualTo(speed + 2));
                Assert.That(player.Context.Powers, Is.Not.SameAs(other.Context.Powers));
                var observation = player.CaptureFsmObservation(); var otherObservation = other.CaptureFsmObservation();
                Assert.That(observation.Origin, Is.Not.Null);
                Assert.That(observation.MatchesSource(scheme.Graph, scheme.Graph.Inspect()), Is.True);
                Assert.That(otherObservation.MatchesSource(scheme.Graph, scheme.Graph.Inspect()), Is.True);
                Assert.That(observation.Origin.Matches(scheme.Graph, scheme.Graph.Inspect(), otherObservation.Description), Is.False);
                Assert.That(builtin.Feel.Capture(Physics2D.gravity).MoveSpeed, Is.EqualTo(speed));
                TestContext.WriteLine($"COMPLETE {style} blank={blank}: move={speed}, jumpHeight={peak-baseline:F4}, fall={fell}; frozen hosts isolated");
            }
            finally
            {
                player?.Stop(); other?.Stop(); first?.Dispose(); second?.Dispose(); _config = original;
                UnityEditor.AssetDatabase.DeleteAsset(folder);
            }
#else
            Assert.Ignore("完整方案资产工作流需要 Unity Editor。");
#endif
        }
    }
}
