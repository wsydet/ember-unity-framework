using System;
using System.Globalization;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        // 本夹具只使用0.20.4已有API，同一份源码在实现前后运行并逐样本比较输出。
        [TestCase("ground-inertia")] [TestCase("air-inertia")]
        [TestCase("ordinary-short")] [TestCase("ordinary-long")]
        [TestCase("wall-slide")] [TestCase("wall-first")] [TestCase("wall-outward")]
        [TestCase("wall-convert")] [TestCase("wall-repeat")]
        [TestCase("ground-flag-window")] [TestCase("ground-auto")] [TestCase("air-auto-wind")]
        [TestCase("legacy-up-continue")] [TestCase("legacy-up-stop")] [TestCase("legacy-side")]
        public void LaunchTailCompatibility_ProtectedFullTrace(string scenario)
        {
            LaunchInertia();
            typeof(PlayerMoveFeel).GetField("_accelerated", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(_config.Feel.Movement, true);
            _config.Feel.Jump.ConfigureOrdinaryJump(RhythmSpec());
            bool wall = scenario.StartsWith("wall", StringComparison.Ordinal);
            bool action = scenario.Contains("flag") || scenario.Contains("auto");
            if (wall) WallContract();
            if (action) InstallActionGraph(10);
            var p = wall ? AtRightWall(5) : Character(new Vector2(0,
                scenario == "air-inertia" ? 80 : scenario == "air-auto-wind" || scenario.StartsWith("legacy") ? 8 : .9f));
            if (!wall && !scenario.StartsWith("legacy") && scenario != "air-auto-wind" && scenario != "air-inertia") Land(p);
            ActionContractConsumer.ActionContractProbe probe = null;
            if (action) probe = StartAction(p);
            if (scenario.StartsWith("legacy"))
            {
                var target = p.gameObject.AddComponent<PlayerLaunchTarget2D>();
                target.TryLaunch(ExternalOwner(), new LaunchRequest2D("legacy-fixture",
                    new Vector2(scenario == "legacy-side" ? 20 : 0, scenario == "legacy-side" ? 10 : 20),
                    LaunchMode2D.ConstantRise, 13f / 60,
                    scenario == "legacy-up-stop" ? LaunchEnd2D.StopVertical : LaunchEnd2D.ContinueVelocity,
                    horizontal: scenario == "legacy-side" ? LaunchHorizontal2D.HoldRequestVelocity : LaunchHorizontal2D.PreserveControl));
            }
            for (int i = 0; i < 200; i++)
            {
                Keys();
                if (scenario.Contains("inertia"))
                {
                    if (i < 20 || i >= 60 && i < 80) Keys(Key.D);
                    else if (i >= 40 && i < 60) Keys(Key.A);
                }
                else if (scenario.StartsWith("ordinary") && i < (scenario == "ordinary-short" ? 2 : 30)) Keys(Key.Space);
                else if (wall && i < 20)
                {
                    if (scenario == "wall-slide") Keys(Key.D);
                    else if (scenario == "wall-outward") Keys(Key.Space);
                    else if (scenario == "wall-convert" && i >= 1) Keys(Key.Space, Key.A);
                    else Keys(Key.Space, Key.D);
                    if (scenario == "wall-repeat" && i == 8)
                    { Keys(); Place(p, new Vector2(0, 5), -1); }
                }
                if (action && i == (scenario == "ground-flag-window" ? 2 : 10))
                    Assert.That(probe.Lease.LaunchVertical(14, 3, probe, scenario,
                        scenario == "ground-flag-window" ? default : new PlayerFlightSpec(120, 20)), Is.True);
                if (action && i == (scenario == "ground-flag-window" ? 3 : 11))
                    probe.Lease.Release(PlayerActionChannels.Vertical);
                if (action && i == (scenario == "ground-flag-window" ? 4 : 12)) probe.Lease.End();
                if (scenario == "air-auto-wind" && i == 18)
                    p.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest("own-wind",
                        PlayerExternalMotionMode.AddVelocity, new Vector2(3, 2), .3f));
                Simulate(p);
                var pos = p.Context.Motor.Position; var v = p.Context.Motor.Velocity;
                TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "PROTECTED\t{0}\t{1}\t{2:R}\t{3:R}\t{4:R}\t{5:R}\t{6:R}\t{7}\t{8}\t{9}",
                    scenario, i, pos.x, pos.y, v.x, v.y, p.Context.Motor.Body.gravityScale,
                    p.CurrentStateId, p.Context.Wall.VerticalAvailable, p.Context.VerticalMotion.Origin));
            }
        }
    }
}
