using System.Reflection;
using Game.Input;
using System.Linq;
using Ember.Core.Fsm;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private void SetJumpFeel(string field, object value) => typeof(PlayerJumpFeel)
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_config.Feel.Jump, value);

        [TestCase(false)] [TestCase(true)]
        public void MinimumHeightShortJump_WaitsForHeightAndCutsOnlyOnce(bool releasedBuffer)
        {
            SetJumpFeel("_shortJumpMode", PlayerShortJumpMode.MinimumHeightVelocityReduction);
            SetJumpFeel("_minimumHeight", .5f); SetJumpFeel("_releaseMultiplier", .5f);
            SetJumpFeel("_releaseSpeed", float.NaN); // 不选中的限速参数不应参与此策略。
            var player = Character(); Land(player); float launchHeight = player.Context.Motor.Position.y;
            Keys(Key.Space); player.Tick(Step); if (releasedBuffer) Keys(); Decide(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(10));
            Keys(); Decide(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(10), "尚未达到最低高度");
            bool cut = false;
            for (int i = 0; i < 30; i++)
            {
                _scene.GetPhysicsScene2D().Simulate(Step);
                float before = player.Context.Motor.Velocity.y;
                Decide(player);
                if (player.Context.Motor.Velocity.y < before)
                {
                    Assert.That(player.Context.Motor.Position.y - launchHeight, Is.GreaterThanOrEqualTo(.5f));
                    Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(before * .5f).Within(.0001));
                    cut = true; break;
                }
            }
            Assert.That(cut, Is.True); Assert.That(player.Context.Input.ConsumeJump(), Is.False);
            player.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(0, 6); Decide(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(6));
        }

        [Test]
        public void MinimumHeightShortJump_LatchesReleaseAndDoesNotCutOnPauseOrApex()
        {
            SetJumpFeel("_shortJumpMode", PlayerShortJumpMode.MinimumHeightVelocityReduction);
            SetJumpFeel("_minimumHeight", 1f); SetJumpFeel("_releaseMultiplier", .5f);
            var player = Character(); Land(player); Keys(Key.Space); Decide(player);
            float start = player.Context.Motor.Position.y;
            Keys(); Decide(player); Keys(Key.Space);
            Place(player, new Vector2(0, start + 1.1f), 8); Decide(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(4), "重新按键不能撤销已发生的松键");
            player.Stop(); Keys();
            var paused = Character(); Land(paused); Keys(Key.Space); Decide(paused); Keys(); Decide(paused);
            paused.Pause(); paused.Resume(); Place(paused, new Vector2(0, 3), 8); Decide(paused);
            Assert.That(paused.Context.Motor.Velocity.y, Is.EqualTo(8), "暂停清除旧高度控制");
            paused.Stop(); Keys(); SetJumpFeel("_minimumHeight", 100f);
            var apex = Character(); Land(apex); Keys(Key.Space); Decide(apex); Keys();
            Place(apex, new Vector2(0, 2), -2); Decide(apex);
            Assert.That(apex.CurrentStateId, Is.EqualTo("fall"));
            Assert.That(apex.Context.Motor.Velocity.y, Is.EqualTo(-2), "普通退出不施加削速");
        }

        [Test]
        public void MinimumHeightZero_ReleasedBufferCutsAtLaunchAndInvalidActiveValuesReject()
        {
            SetJumpFeel("_shortJumpMode", PlayerShortJumpMode.MinimumHeightVelocityReduction);
            SetJumpFeel("_minimumHeight", 0f); SetJumpFeel("_releaseMultiplier", .5f);
            var player = Character(); Land(player); Keys(Key.Space); player.Tick(Step); Keys(); Decide(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(5));
            SetJumpFeel("_releaseMultiplier", 2f);
            Assert.Throws<System.InvalidOperationException>(() => _config.Feel.Capture(Physics2D.gravity));
            SetJumpFeel("_enableShortJump", false);
            Assert.DoesNotThrow(() => _config.Feel.Capture(Physics2D.gravity));
        }

        [Test]
        public void JumpResponseWindow_OverridesOnlyWithInputAndEndsAtBoundary()
        {
            SetMoveFeel("_independentAir", true); SetMoveFeel("_airSpeed", 2f);
            SetMoveFeel("_airAccelerated", true); SetMoveFeel("_airTurning", 10f);
            SetMoveFeel("_airBraking", 5f); SetMoveFeel("_enableJumpResponse", true);
            SetMoveFeel("_jumpResponseDuration", .06f);
            var player = Character(); Land(player); Keys(Key.D, Key.Space); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(2), "起跳步直接取得空中水平目标");
            Keys(Key.Space); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(1.9f).Within(.0001), "无输入仍按空中刹车");
            Keys(Key.A, Key.Space); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(-2));
            Keys(Key.D, Key.Space); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(-1.8f).Within(.0001), "到期使用常规转向速率");
        }

        [Test]
        public void JumpResponseWindow_FreezesDuringPauseAndDoesNotStartForRestoredAscent()
        {
            SetMoveFeel("_independentAir", true); SetMoveFeel("_airSpeed", 2f);
            SetMoveFeel("_airAccelerated", true); SetMoveFeel("_airTurning", 10f);
            SetMoveFeel("_airAcceleration", 10f); SetMoveFeel("_enableJumpResponse", true);
            SetMoveFeel("_jumpResponseDuration", .06f);
            var player = Character(); Land(player); Keys(Key.D, Key.Space); Decide(player);
            player.Pause(); for (int i = 0; i < 100; i++) Decide(player);
            player.Resume(); Keys(Key.A); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(-2));
            player.Stop(); Keys();
            var restored = Character(new Vector2(3, 5), new Vector2(0, 8));
            Keys(Key.D); Decide(restored);
            Assert.That(restored.Context.Motor.Velocity.x, Is.EqualTo(.2f).Within(.0001));
            Assert.That(restored.Context.Motor.Velocity.y, Is.EqualTo(8));
        }

        private void SetMoveFeel(string field, object value) => typeof(PlayerMoveFeel)
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_config.Feel.Movement, value);

        [Test]
        public void GroundAccelerationBrakingAndTurning_UseDistinctRatesAndReachTarget()
        {
            SetMoveFeel("_accelerated", true); SetMoveFeel("_acceleration", 20f);
            SetMoveFeel("_braking", 10f); SetMoveFeel("_turning", 60f);
            var player = Character(); Land(player);
            Keys(Key.D); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(.4f).Within(.0001));
            for (int i = 0; i < 20; i++) Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(5));
            Keys(); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(4.8f).Within(.0001));
            Keys(Key.A); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(3.6f).Within(.0001));
            Assert.That(player.Context.Facing.FacesRight, Is.False);
            for (int i = 0; i < 50; i++) Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(-5));
            Keys(); for (int i = 0; i < 50; i++) Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.Zero);
        }

        [Test]
        public void IndependentAirControl_UsesOwnRatesAndCapturedConfiguration()
        {
            SetMoveFeel("_independentAir", true); SetMoveFeel("_airSpeed", 2f);
            SetMoveFeel("_airAccelerated", true); SetMoveFeel("_airAcceleration", 10f);
            SetMoveFeel("_airBraking", 5f); SetMoveFeel("_airTurning", 30f);
            var player = Character(new Vector2(0, 10));
            SetMoveFeel("_airSpeed", 99f); SetMoveFeel("_airAcceleration", 999f);
            Keys(Key.D); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(.2f).Within(.0001));
            Keys(Key.A); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(-.4f).Within(.0001));
            Keys(); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(-.3f).Within(.0001));
            Keys(Key.D); for (int i = 0; i < 30; i++) Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(2));
        }

        [TestCase(false)] [TestCase(true)]
        public void AirSelectionAndLanding_UseCurrentSupportWithActualPhysics(bool independent)
        {
            SetMoveFeel("_independentAir", independent); SetMoveFeel("_airSpeed", 2f);
            var player = Character(); Land(player);
            Keys(Key.D, Key.Space); Simulate(player);
            Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(independent ? 2 : 5));
            Assert.That(player.Context.Input.ConsumeJump(), Is.False);
            bool sawAir = false, landed = false;
            for (int i = 0; i < 150; i++)
            {
                Simulate(player);
                if (!player.Context.JumpWindow.HasGroundSupport) sawAir = true;
                else if (sawAir) { landed = true; break; }
            }
            Assert.That(landed, Is.True);
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(5));
        }

        [Test]
        public void HorizontalConfiguration_ValidatesActiveValuesAndSupportsZeroAcceleration()
        {
            SetMoveFeel("_airSpeed", float.NaN); SetMoveFeel("_acceleration", float.NaN);
            Assert.DoesNotThrow(() => _config.Feel.Capture(Physics2D.gravity));
            SetMoveFeel("_accelerated", true);
            Assert.Throws<System.InvalidOperationException>(() => _config.Feel.Capture(Physics2D.gravity));
            SetMoveFeel("_acceleration", 0f);
            var player = Character(); Land(player); Keys(Key.D); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.Zero);
            SetMoveFeel("_independentAir", true);
            Assert.Throws<System.InvalidOperationException>(() => _config.Feel.Capture(Physics2D.gravity));
            SetMoveFeel("_airSpeed", 2f); SetMoveFeel("_airAccelerated", true); SetMoveFeel("_airTurning", -1f);
            Assert.Throws<System.InvalidOperationException>(() => _config.Feel.Capture(Physics2D.gravity));
        }

        [Test]
        public void HeightAndTimeResolveBaseMotionAndIgnoreInactiveDirectFields()
        {
            var jump = _config.Feel.Jump;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(PlayerJumpFeel).GetField("_mode", flags).SetValue(jump, PlayerJumpParameterMode.HeightAndApexTime);
            typeof(PlayerJumpFeel).GetField("_height", flags).SetValue(jump, 3f);
            typeof(PlayerJumpFeel).GetField("_apexTime", flags).SetValue(jump, .5f);
            typeof(PlayerJumpFeel).GetField("_launchSpeed", flags).SetValue(jump, float.NaN);
            var feel = _config.Feel.Capture(new Vector2(0, -10));
            Assert.That(feel.LaunchSpeed, Is.EqualTo(12)); Assert.That(feel.GravityScale, Is.EqualTo(2.4f));
            Assert.That(feel.EstimatedHeight, Is.EqualTo(3).Within(.0001));
            Assert.That(feel.EstimatedApexTime, Is.EqualTo(.5f).Within(.0001));
            Assert.Throws<System.InvalidOperationException>(() => _config.Feel.Capture(Vector2.zero));
        }

        [Test]
        public void DirectModeIgnoresInactiveHeightAndKeepsExistingMotion()
        {
            typeof(PlayerJumpFeel).GetField("_height", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_config.Feel.Jump, float.NaN);
            var feel = _config.Feel.Capture(new Vector2(0, -9.81f));
            Assert.That(feel.MoveSpeed, Is.EqualTo(5)); Assert.That(feel.LaunchSpeed, Is.EqualTo(10));
            Assert.That(feel.GravityScale, Is.EqualTo(3)); Assert.That(feel.BufferDuration, Is.EqualTo(.12f));
        }

        [Test]
        public void ProfileCopyOwnsAllMutableGroupsAndDoesNotChangeCapturedRuntime()
        {
            var source = _config.Feel; var runtime = source.Capture(new Vector2(0, -10));
            var copy = Keep(source.CreateIndependentCopy()); Keep(copy.Movement); Keep(copy.Jump); Keep(copy.Input);
            Assert.That(copy.Movement, Is.Not.SameAs(source.Movement));
            Assert.That(copy.Jump, Is.Not.SameAs(source.Jump)); Assert.That(copy.Input, Is.Not.SameAs(source.Input));
            typeof(PlayerMoveFeel).GetField("_speed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(copy.Movement, 12f);
            Assert.That(copy.Capture(new Vector2(0, -10)).MoveSpeed, Is.EqualTo(12));
            Assert.That(source.Movement.Speed, Is.EqualTo(5)); Assert.That(runtime.MoveSpeed, Is.EqualTo(5));
        }

        [Test]
        public void MissingGroupAndInvalidActiveValuesRejectPreparation()
        {
            var source = _config.Feel;
            source.Configure(null, source.Jump, source.Input);
            Assert.Throws<System.InvalidOperationException>(() => source.Capture(Physics2D.gravity));
            source.Configure(Keep(ScriptableObject.CreateInstance<PlayerMoveFeel>()), source.Jump, source.Input);
            typeof(PlayerMoveFeel).GetField("_speed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(source.Movement, float.PositiveInfinity);
            Assert.Throws<System.InvalidOperationException>(() => source.Capture(Physics2D.gravity));
        }

        [Test]
        public void DisabledBufferKeepsImmediatePressButDoesNotQueueLanding()
        {
            typeof(PlayerInputFeel).GetField("_enableJumpBuffer", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_config.Feel.Input, false);
            var ground = Character(); Land(ground); Keys(Key.Space); Decide(ground);
            Assert.That(ground.CurrentStateId, Is.EqualTo("jump")); ground.Stop(); Keys();
            var air = Character(new Vector2(2, 5)); Keys(Key.Space); Decide(air);
            Assert.That(air.Context.Input.BufferRemaining, Is.Zero);
            Assert.That(air.Context.Input.JumpRequested, Is.False);
            SettleGroundContact(air, 2); Decide(air);
            Assert.That(air.CurrentStateId, Is.EqualTo("idle"));
        }

        [Test]
        public void DisabledCoyoteDoesNotDisableGroundJumpOrInputBuffer()
        {
            typeof(PlayerInputFeel).GetField("_enableCoyote", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_config.Feel.Input, false);
            var player = Character(); Land(player); Place(player, new Vector2(14, 1), -1);
            Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
            Assert.That(player.Context.JumpWindow.CoyoteRemaining, Is.Zero);
            Assert.That(player.Context.Input.BufferRemaining, Is.GreaterThan(0));
            SettleGroundContact(player); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
            Assert.That(player.Context.Input.ConsumeJump(), Is.False);
        }

        [TestCase(false)] [TestCase(true)]
        public void DisabledShortJumpLaunchesFullSpeedForHeldAndReleasedPress(bool held)
        {
            var definition = _config.Feel.Jump;
            var enabled = typeof(PlayerJumpFeel).GetField("_enableShortJump", BindingFlags.Instance | BindingFlags.NonPublic);
            enabled.SetValue(definition, false);
            var player = Character(); Land(player);
            enabled.SetValue(definition, true); // 已创建的状态实例继续使用创建时参数。
            Keys(Key.Space); player.Tick(Step); if (!held) Keys(); Decide(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(10));
            Keys(); Decide(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(10));
            Assert.That(player.Context.Input.ConsumeJump(), Is.False);
        }

        [Test]
        public void TimingDiagnosticsAreReadOnlyAndConfigurationChangesRequireNewRuntime()
        {
            var player = Character(); Land(player);
            typeof(PlayerInputFeel).GetField("_enableCoyote", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_config.Feel.Input, false);
            typeof(PlayerInputFeel).GetField("_enableJumpBuffer", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_config.Feel.Input, false);
            Place(player, new Vector2(14, 1), -1); Decide(player); Keys(Key.Space); player.Tick(Step);
            float coyote = player.Context.JumpWindow.CoyoteRemaining, buffer = player.Context.Input.BufferRemaining;
            Assert.That(coyote, Is.GreaterThan(0)); Assert.That(buffer, Is.GreaterThan(0));
            for (int i = 0; i < 20; i++)
            {
                Assert.That(player.Context.JumpWindow.CoyoteRemaining, Is.EqualTo(coyote));
                Assert.That(player.Context.Input.BufferRemaining, Is.EqualTo(buffer));
            }
            Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
            Assert.That(player.Context.Input.BufferRemaining, Is.Zero);
            Assert.That(player.Context.JumpWindow.CoyoteRemaining, Is.Zero);
        }

        private void Place(PlayerController player, Vector2 position, float verticalSpeed)
        {
            var body = player.GetComponent<Rigidbody2D>();
            body.position = position; player.transform.position = position;
            body.linearVelocity = new Vector2(0, verticalSpeed);
            Physics2D.SyncTransforms();
        }
        // 只推进隔离物理，不推进输入窗口；用于测试“下一次决策时已真实落地”的时间边界。
        private void SettleGroundContact(PlayerController player, float x = 0)
        {
            Place(player, new Vector2(x, .9f), -1);
            // 传送后的 broadphase/contact cache 可能要到下一拍才建立；不把位置已写入当作接触完成。
            for (int i = 0; i < 5; i++)
            {
                _scene.GetPhysicsScene2D().Simulate(Step);
                if (player.Context.Sensors.Shape.IsTouchingLayers(1)) break;
            }
            Assert.That(player.Context.Sensors.Shape.IsTouchingLayers(1), Is.True);
        }

        [TestCase(false, 4)]
        [TestCase(true, 10)]
        public void BufferedLanding_CommitsJumpInTheDetectionStep(bool held, float expectedSpeed)
        {
            var player = Character(new Vector2(0, 1.05f), new Vector2(0, -2));
            Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
            if (!held) Keys();
            for (int i = 0; i < 5; i++)
            {
                _scene.GetPhysicsScene2D().Simulate(Step);
                Decide(player);
                Assert.That(player.CurrentStateId, Is.Not.EqualTo("idle"), "缓冲不应等待一帧 Ground/Idle 再起跳");
                if (player.CurrentStateId != "jump") continue;
                Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(expectedSpeed));
                Assert.That(player.Context.Input.JumpRequested, Is.False);
                Assert.That(player.Context.JumpWindow.CanJump, Is.False);
                return;
            }
            Assert.Fail("有效缓冲应在本次落地检测时起跳");
        }

        [TestCase(5, true)]
        [TestCase(6, false)]
        public void BufferExpiry_UsesElapsedPhysicsTime_AndHasAnExactBoundary(int elapsedSteps, bool expectedJump)
        {
            var player = Character(new Vector2(0, 5)); Keys(Key.Space); player.Tick(Step); Keys();
            for (int i = 0; i < elapsedSteps; i++) player.FixedTick(Step);
            Assert.That(player.Context.Input.JumpRequested, Is.EqualTo(expectedJump));
            SettleGroundContact(player); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo(expectedJump ? "jump" : "idle"));
        }

        [TestCase(0, true)]
        [TestCase(3, true)]
        [TestCase(4, false)]
        public void NaturalEdgeDeparture_AllowsOnlyTheCoyoteWindow(int priorAirSteps, bool expectedJump)
        {
            var player = Character(); Land(player);
            Place(player, new Vector2(14, 1), -1);
            for (int i = 0; i < priorAirSteps; i++) Decide(player);
            Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo(expectedJump ? "jump" : "fall"));
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(expectedJump ? 10 : -1));
        }

        [Test]
        public void ZeroWindows_AllowImmediateGroundPress_ButNoAirBufferOrCoyote()
        {
            typeof(PlayerInputFeel).GetField("_jumpBufferDuration", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_config.Feel.Input, 0f);
            typeof(PlayerInputFeel).GetField("_coyoteDuration", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_config.Feel.Input, 0f);
            var ground = Character(new Vector2(-2, 0.9f)); Land(ground); Keys(Key.Space); Decide(ground);
            Assert.That(ground.CurrentStateId, Is.EqualTo("jump")); ground.Stop();
            Keys();
            var edge = Character(new Vector2(2, 0.9f)); Land(edge);
            Place(edge, new Vector2(14, 1), -1); Keys(Key.Space); Decide(edge);
            Assert.That(edge.CurrentStateId, Is.EqualTo("fall"));
            Assert.That(edge.Context.Input.JumpRequested, Is.False);
            SettleGroundContact(edge, 2); Decide(edge);
            Assert.That(edge.CurrentStateId, Is.EqualTo("idle"));
        }

        [Test]
        public void NewPress_RefreshesOneBuffer_WithoutQueuingMultipleJumps()
        {
            var player = Character(new Vector2(0, 5)); Keys(Key.Space); Decide(player);
            for (int i = 0; i < 4; i++) Decide(player);
            Keys(); Keys(Key.Space); Decide(player);
            for (int i = 0; i < 4; i++) Decide(player);
            Assert.That(player.Context.Input.JumpRequested, Is.True);
            SettleGroundContact(player); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
            Land(player); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
        }

        [Test]
        public void ShortTapWithinOneInputUpdate_SurvivesMultipleRenderSamples_AndLaunchesShort()
        {
            var player = Character(); Land(player);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            InputSystem.Update();
            for (int i = 0; i < 20; i++) player.Tick(0.001f);
            player.FixedTick(Step);
            Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(4));
            Assert.That(player.Context.Input.ConsumeJump(), Is.False);
        }

        [Test]
        public void ConsumedGroundOpportunity_CannotBecomeADoubleJump_WhenStillNearFloor()
        {
            var player = Character(); Land(player); Keys(Key.Space); Decide(player);
            // 此时尚未物理模拟，地面 Cast 仍为 true，但起跳机会已经消耗。
            Keys(); Keys(Key.Space); Decide(player);
            Assert.That(player.Context.Sensors.IsGrounded, Is.True);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(4), "新按下表明旧跳跃已释放，只能截速，不能再次施加 10");
            Assert.That(player.Context.JumpWindow.CanJump, Is.False);
        }

        [Test]
        public void RealPhysics_HeldJumpReachesHigherThanReleasedJump()
        {
            float Peak(PlayerController player, bool release)
            {
                Land(player); Keys(Key.Space); Decide(player);
                if (release) Keys();
                float peak = player.Context.Motor.Position.y;
                for (int i = 0; i < 100; i++)
                {
                    Simulate(player); peak = Mathf.Max(peak, player.Context.Motor.Position.y);
                    if (player.CurrentStateId == "idle") break;
                }
                Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
                player.Stop(); Keys(); return peak;
            }
            float full = Peak(Character(new Vector2(-2, 0.9f)), false);
            float shortJump = Peak(Character(new Vector2(2, 0.9f)), true);
            Assert.That(full, Is.GreaterThan(2.2f));
            Assert.That(shortJump, Is.LessThan(1.4f));
            Assert.That(full - shortJump, Is.GreaterThan(1));
        }

        [Test]
        public void ReleaseCut_NeverAddsUpwardSpeed_AndRunsOnlyOnce()
        {
            var player = Character(); Land(player); Keys(Key.Space); Decide(player);
            for (int i = 0; i < 20 && player.Context.Motor.Velocity.y > 3; i++) Simulate(player);
            float before = player.Context.Motor.Velocity.y; Assert.That(before, Is.GreaterThan(0).And.LessThan(4));
            Keys(); Decide(player); Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(before));
            // 外部竖直运动不应被已经消费的释放反复截断。
            player.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(0, 6); Decide(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(6));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InputDisableOrPause_EndsOldHeightControl_WithoutClippingRestoredVelocity(bool pauseFsm)
        {
            var player = Character(); Land(player); Keys(Key.Space); Decide(player);
            var deny = _input.Request(this, 100, new GameInputRule("gameplay", GameInputPermission.Deny));
            if (pauseFsm) player.Pause();
            Keys();
            if (pauseFsm) player.Resume();
            deny.Dispose(); Decide(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(10));
            Keys(Key.Space); Decide(player); Keys(); Decide(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(10), "新的空中按键不能接管旧跳跃的高度控制");
        }

        [Test]
        public void InternalControllerPause_FreezesCoyoteTime_AndClearsBufferedInput()
        {
            var player = Character(); Land(player); Place(player, new Vector2(14, 1), -1); Decide(player);
            Keys(Key.Space); player.Tick(Step);
            player.Pause();
            for (int i = 0; i < 100; i++) { player.Tick(Step); player.FixedTick(Step); }
            Assert.That(player.Context.Input.JumpRequested, Is.False);
            player.Resume(); Keys(); Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
        }

        [Test]
        public void RestoredAirborneCharacter_DoesNotInheritAnotherCharactersCoyoteTime()
        {
            var first = Character(new Vector2(-2, 0.9f)); Land(first);
            var second = Character(new Vector2(16, 3));
            Place(first, new Vector2(14, 1), -1);
            Keys(Key.Space); Decide(first); Decide(second);
            Assert.That(first.CurrentStateId, Is.EqualTo("jump"));
            Assert.That(second.CurrentStateId, Is.EqualTo("fall"));
            Assert.That(second.Context.Motor.Velocity.y, Is.Zero);
        }

        [Test]
        public void LandingBuffer_ReentersJumpEvenIfPhysicsSkippedTheFallState()
        {
            var player = Character(); Land(player); Keys(Key.Space); Decide(player);
            var oldJump = Fsm(player).GetActivation("jump");
            var air = Fsm(player).GetActivation("air");
            // 先确认起跳已离开真实接触，再模拟决策间落地；叶状态仍未经过 Fall。
            _scene.GetPhysicsScene2D().Simulate(Step); Decide(player);
            SettleGroundContact(player); Keys(); Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
            Assert.That(Fsm(player).GetActivation("jump"), Is.Not.EqualTo(oldJump));
            Assert.That(Fsm(player).GetActivation("air"), Is.EqualTo(air));
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(10));
        }
    }
}
