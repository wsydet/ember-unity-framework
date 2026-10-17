using System;
using System.Linq;
using ActionContractConsumer;
using Ember.Core.Fsm;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Player.Tests
{
    public sealed partial class PlayerStateTests
    {
        private ActionProbeSettings InstallActionGraph(float duration = 1)
        {
            var source = _config.Graph;
            var root = Keep(ScriptableObject.CreateInstance<ActionProbeRoot>());
            root.BaseDefinition = source.Nodes.First(n => n.Id == source.EntryId).Definition;
            var definition = Keep(ScriptableObject.CreateInstance<ActionProbeDefinition>());
            var settings = Keep(ScriptableObject.CreateInstance<ActionProbeSettings>()); settings.Duration = duration;
            definition.Settings = settings;
            var graph = Keep(ScriptableObject.CreateInstance<EmberFsmGraph>());
            graph.Configure(source.EntryId, source.Nodes.Select(n => n.Id == source.EntryId
                ? new EmberFsmNode(n.Id, n.DisplayName, root, n.ParentId, n.DefaultChildId,
                    n.Bindings.Append(new EmberFsmTargetBinding("project-probe", "project-probe")).ToArray()) : n)
                .Append(new EmberFsmNode("project-probe", "项目动作", definition, source.EntryId, null,
                    new EmberFsmTargetBinding("return", "air"))).ToArray());
            Assert.That(graph.Inspect(typeof(PlayerContext)).IsValid, Is.True);
            _config.Configure(graph, _config.Spawn);
            return settings;
        }
        private ActionContractProbe StartAction(PlayerController player)
        {
            var probe = player.GetComponent<ActionContractProbe>() ?? player.gameObject.AddComponent<ActionContractProbe>();
            probe.Requested = true; Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("project-probe"));
            Assert.That(probe.Lease.IsActive, Is.True);
            return probe;
        }

        [TestCase(.01f)] [TestCase(.02f)] [TestCase(.03333333f)]
        public void ActionContract_RealJumpHoverZeroGravityAndRestore(float step)
        {
            InstallActionGraph(); var player = Character(); Land(player);
            Keys(Key.Space); Simulate(player); Keys(); Simulate(player);
            Assert.That(player.Context.Motor.Position.y, Is.GreaterThan(1));
            var probe = StartAction(player); var position = player.Context.Motor.Position;
            for (float time = 0; time < .2f; time += step)
            { player.Tick(step); player.FixedTick(step); _scene.GetPhysicsScene2D().Simulate(step); }
            Assert.That(player.Context.Motor.Position.y, Is.EqualTo(position.y).Within(.0001));
            Assert.That(player.Context.Motor.Body.gravityScale, Is.Zero);
            Assert.That(player.Context.ExternalMotion.ComposedVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(player.CurrentStateId, Is.EqualTo("project-probe"));
            TestContext.WriteLine($"Hover step={step}: y={position.y} -> {player.Context.Motor.Position.y}; gravity={player.Context.Motor.Body.gravityScale}; state={player.CurrentStateId}");
            Assert.That(probe.Lease.End(), Is.EqualTo(EmberFsmResult.Changed));
            Simulate(player);
            Assert.That(player.Context.Motor.Body.gravityScale, Is.GreaterThan(0));
            Assert.That(probe.LastEnd, Is.EqualTo(PlayerActionEndReason.Completed));
            Land(player);
        }

        [Test]
        public void ActionContract_PartialReleaseRestoresHorizontalFacingAndGravity()
        {
            InstallActionGraph(); var player = Character(new Vector2(0, 5));
            var probe = StartAction(player); var lease = probe.Lease;
            Keys(Key.A); Simulate(player);
            Assert.That(player.Context.Motor.Velocity, Is.EqualTo(Vector2.zero));
            lease.Release(PlayerActionChannels.Horizontal | PlayerActionChannels.Facing);
            Simulate(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.LessThan(0));
            Assert.That(player.Context.Facing.FacesRight, Is.False);
            Assert.That(player.Context.Motor.Body.gravityScale, Is.Zero);
            Assert.That(lease.SetHorizontal(9), Is.False);
            lease.SetVertical(8, 0); Simulate(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(8));
            lease.Release(PlayerActionChannels.Vertical); Simulate(player);
            Assert.That(player.Context.Motor.Body.gravityScale, Is.GreaterThan(0));
            Assert.That(player.CurrentStateId, Is.EqualTo("project-probe"));
            Assert.Throws<InvalidOperationException>(() => player.Capture("GameplayScene"));
        }

        [TestCase(8f)] [TestCase(-8f)]
        public void ActionContract_VerticalImpulseUsesRealGravityWithoutProjectCompensation(float initial)
        {
            InstallActionGraph(); var player = Character(new Vector2(0, 10)); var probe = StartAction(player);
            probe.Lease.SetVertical(initial, 2, holdVelocity: false);
            for (int i = 0; i < 4; i++) Simulate(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(initial + Physics2D.gravity.y * 2 * Step * 4).Within(.001f));
            Assert.That(probe.Lease.IsActive, Is.True);
            probe.Lease.Release(PlayerActionChannels.Vertical); Simulate(player);
            Assert.That(player.Context.Motor.Body.gravityScale, Is.Not.EqualTo(2));
        }

        [TestCase(PlayerExternalMotionMode.AddVelocity)]
        [TestCase(PlayerExternalMotionMode.ReplaceVelocity)]
        [TestCase(PlayerExternalMotionMode.ForcedRise)]
        public void ActionContract_ExternalMotionReturnsOnlyOnReleasedAxes(PlayerExternalMotionMode mode)
        {
            InstallActionGraph(); var player = Character(new Vector2(0, 5)); var probe = StartAction(player);
            using var source = player.Context.ExternalMotion.Register(ExternalOwner(),
                new PlayerExternalMotionRequest("wind", mode, new Vector2(3, 4), 1));
            Decide(player); Assert.That(player.Context.Motor.Velocity, Is.EqualTo(Vector2.zero));
            probe.Lease.Release(PlayerActionChannels.Horizontal); Decide(player);
            Assert.That(player.Context.Motor.Velocity, Is.EqualTo(new Vector2(3, 0)));
            probe.Lease.Release(PlayerActionChannels.Vertical); Decide(player);
            Assert.That(player.Context.Motor.Velocity, Is.EqualTo(new Vector2(3, 4)));
            Assert.That(player.CurrentStateId, Is.EqualTo("project-probe"));
            Assert.That(probe.Lease.Channels.HasFlag(PlayerActionChannels.AutomaticTransitions), Is.True);
        }

        [Test]
        public void ActionContract_OriginalReplacementRemainsVelocityOnly()
        {
            var player = Character(); Land(player); Keys(Key.Space); Simulate(player); Keys(); Simulate(player);
            using var source = player.Context.ExternalMotion.Register(ExternalOwner(), new PlayerExternalMotionRequest(
                "baseline", PlayerExternalMotionMode.ReplaceVelocity, Vector2.zero, priority: 100, lockInput: true));
            float before = player.Context.Motor.Position.y;
            for (int i = 0; i < 10; i++) Simulate(player);
            Assert.That(player.Context.Motor.Position.y, Is.LessThan(before));
            Assert.That(player.Context.Motor.Body.gravityScale, Is.GreaterThan(0));
            Assert.That(player.Context.ExternalMotion.ComposedVelocity, Is.EqualTo(Vector2.zero));
            Assert.Throws<ArgumentException>(() => new PlayerExternalMotionRequest("zero", PlayerExternalMotionMode.ForcedRise, Vector2.zero, .2f));
            TestContext.WriteLine($"Original ReplaceVelocity: y={before} -> {player.Context.Motor.Position.y}; gravity={player.Context.Motor.Body.gravityScale}; state={player.CurrentStateId}");
        }

        [Test]
        public void ActionContract_OldLeaseCannotEndOrWriteNewActivation()
        {
            InstallActionGraph(); var player = Character(new Vector2(0, 5));
            var probe = StartAction(player); var old = probe.Lease;
            old.End(); StartAction(player); var current = probe.Lease;
            Assert.That(current.Generation, Is.Not.EqualTo(old.Generation));
            Assert.That(old.Release(PlayerActionChannels.All), Is.False);
            Assert.That(old.SetVertical(99, 0), Is.False);
            Assert.That(old.End(), Is.EqualTo(EmberFsmResult.StaleActivation)); old.Dispose();
            Simulate(player); Assert.That(current.IsActive, Is.True);
            Assert.That(player.Context.Motor.Velocity, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ActionContract_LaunchRejectionPreservesActionAndAcceptanceExitsBeforeNotification()
        {
            InstallActionGraph(); var player = Character(new Vector2(0, 5)); var probe = StartAction(player);
            var target = player.gameObject.AddComponent<PlayerLaunchTarget2D>(); var owner = ExternalOwner();
            int notifications = 0;
            target.Accepted += _ => { notifications++; Assert.That(probe.Lease.IsActive, Is.False); Assert.That(player.CurrentStateId, Is.Not.EqualTo("project-probe")); };
            Assert.That(target.TryLaunch(owner, new LaunchRequest2D("reject", Vector2.up * 8,
                interrupt: LaunchInterrupt2D.RejectWhileBusy)), Is.EqualTo(LaunchResult2D.Busy));
            Assert.That(probe.Lease.IsActive, Is.True); Assert.That(probe.Exits, Is.Zero);
            probe.AllowExit = false;
            Assert.That(target.TryLaunch(owner, new LaunchRequest2D("guard", Vector2.up * 8)), Is.EqualTo(LaunchResult2D.Busy));
            Assert.That(probe.Lease.IsActive, Is.True); Assert.That(probe.Exits, Is.Zero);
            probe.AllowExit = true;
            Assert.That(target.TryLaunch(owner, new LaunchRequest2D("accept", new Vector2(3, 8))), Is.EqualTo(LaunchResult2D.Accepted));
            Assert.That(probe.LastEnd, Is.EqualTo(PlayerActionEndReason.Launch)); Assert.That(notifications, Is.EqualTo(1));
            Decide(player); Assert.That(player.Context.Motor.Velocity, Is.EqualTo(new Vector2(3, 8)));
        }

        [Test]
        public void ActionContract_LaunchExitDisablingHostCannotRegisterOnOldSession()
        {
            InstallActionGraph(); var player = Character(new Vector2(0, 5)); var probe = StartAction(player);
            var target = player.gameObject.AddComponent<PlayerLaunchTarget2D>();
            probe.OnExit = () => player.enabled = false;
            Assert.That(target.TryLaunch(ExternalOwner(), new LaunchRequest2D("launch", Vector2.up * 8)), Is.EqualTo(LaunchResult2D.Inactive));
            Assert.That(probe.Lease.IsActive, Is.False); Assert.That(player.Context, Is.Null);
        }

        [TestCase(false)] [TestCase(true)]
        public void ActionContract_OwnerDisableOrDestroyEndsExactlyOnce(bool destroy)
        {
            InstallActionGraph(); var player = Character(new Vector2(0, 5)); var probe = StartAction(player);
            var owner = ExternalOwner(); probe.Lease.BindOwner(owner);
            if (destroy) UnityEngine.Object.DestroyImmediate(owner.gameObject); else owner.enabled = false;
            Simulate(player); Assert.That(probe.Lease.IsActive, Is.False);
            Assert.That(probe.LastEnd, Is.EqualTo(PlayerActionEndReason.OwnerDisabled)); Assert.That(probe.Exits, Is.EqualTo(1));
        }

        [TestCase(false)] [TestCase(true)]
        public void ActionContract_HostDisableOrDestroyInvalidatesLease(bool destroy)
        {
            InstallActionGraph(); var player = Character(new Vector2(0, 5)); var probe = StartAction(player); var lease = probe.Lease;
            if (destroy) UnityEngine.Object.DestroyImmediate(player.gameObject); else player.enabled = false;
            Assert.That(lease.IsActive, Is.False); lease.Dispose();
        }

        [Test]
        public void ActionContract_PauseAndTwoHostsKeepTimersAndOwnersIndependent()
        {
            InstallActionGraph(.12f); var first = Character(new Vector2(-3, 5)); var second = Character(new Vector2(3, 5));
            var a = StartAction(first); var b = StartAction(second); first.Pause();
            for (int i = 0; i < 10; i++) { Decide(first); Simulate(second); }
            Assert.That(a.Lease.IsActive, Is.True); Assert.That(b.Lease.IsActive, Is.False);
            first.Resume();
            for (int i = 0; i < 10; i++) Simulate(first);
            Assert.That(a.Lease.IsActive, Is.False); Assert.That(a.Exits, Is.EqualTo(1));
        }

        [TestCase(true)] [TestCase(false)]
        public void ActionContract_WallAndCeilingRemainPhysical(bool ceiling)
        {
            InstallActionGraph(); var obstacle = Keep(new GameObject("Action obstacle"));
            SceneManager.MoveGameObjectToScene(obstacle, _scene);
            obstacle.transform.position = ceiling ? new Vector3(0, 7) : new Vector3(2, 5);
            obstacle.AddComponent<BoxCollider2D>().size = ceiling ? new Vector2(8, .2f) : new Vector2(.2f, 8);
            var player = Character(new Vector2(0, 5)); var probe = StartAction(player);
            probe.Lease.SetHorizontal(ceiling ? 0 : 8); probe.Lease.SetVertical(ceiling ? 8 : 0, 0);
            for (int i = 0; i < 20; i++) Simulate(player);
            Assert.That(ceiling ? player.Context.Motor.Position.y : player.Context.Motor.Position.x,
                Is.LessThan(ceiling ? 6.1f : 1.5f));
            Assert.That(probe.Lease.IsActive, Is.True);
            probe.Lease.End(); Land(player);
        }

        [Test]
        public void ActionContract_EnterFailureReleasesOwnership()
        {
            InstallActionGraph(); var player = Character(new Vector2(0, 5));
            var probe = player.gameObject.AddComponent<ActionContractProbe>(); probe.FailEnter = true; probe.Requested = true;
            Assert.Throws<InvalidOperationException>(() => Decide(player));
            Assert.That(probe.Lease.IsActive, Is.False); Assert.That(probe.Exits, Is.EqualTo(1));
            Assert.That(probe.LastEnd, Is.EqualTo(PlayerActionEndReason.PreparationFailed));
        }

        [Test]
        public void ActionContract_WallJumpHandoffCancelsOldWallWindowAndAllowsFreshJump()
        {
            InstallWalls(); InstallActionGraph(); var player = AtRightWall();
            Keys(Key.Space); Decide(player); Assert.That(player.CurrentStateId, Is.EqualTo("wall-jump"));
            Keys(); var probe = StartAction(player);
            Assert.That(player.Context.Wall.ProtectionRemaining, Is.Zero);
            probe.Lease.End(); Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("wall-jump"));
            Assert.That(probe.Exits, Is.EqualTo(1));
        }

        [TestCase(false)] [TestCase(true)]
        public void ActionContract_LifeInterruptHasPriorityOverOwnedChannels(bool lethal)
        {
            var lifeRoot = Keep(ScriptableObject.CreateInstance<PlayerLifeLocomotionStateDefinition>());
            var source = _config.Graph; var graph = Keep(ScriptableObject.CreateInstance<EmberFsmGraph>());
            graph.Configure(source.EntryId, source.Nodes.Select(n => n.Id == source.EntryId
                ? new EmberFsmNode(n.Id, n.DisplayName, lifeRoot, n.ParentId, n.DefaultChildId, n.Bindings.Concat(new[] {
                    new EmberFsmTargetBinding("life-death", "life.dead"), new EmberFsmTargetBinding("life-hit", "life.hit") }).ToArray()) : n)
                .Concat(new[] {
                    new EmberFsmNode("life.dead", "死亡", Keep(ScriptableObject.CreateInstance<PlayerDeathStateDefinition>()), source.EntryId),
                    new EmberFsmNode("life.hit", "受击", Keep(ScriptableObject.CreateInstance<PlayerHitStateDefinition>()), source.EntryId, null,
                        new EmberFsmTargetBinding("ground", "ground"), new EmberFsmTargetBinding("fall", "fall")) }).ToArray());
            _config.Configure(graph, _config.Spawn); InstallActionGraph();
            var scheme = Keep(ScriptableObject.CreateInstance<PlayerScheme>());
            PlayerDashEffectTestAssets.Set(scheme, "_life", JsonUtility.FromJson<PlayerLifeConfiguration>("{\"_enabled\":true,\"_maximum\":10,\"_initial\":10,\"_multiplier\":1,\"_death\":1}"));
            PlayerDashEffectTestAssets.Set(scheme, "_reactions", JsonUtility.FromJson<PlayerReactionConfiguration>("{\"_enabled\":true,\"_invulnerability\":false}"));
            var combat = new PlayerCombatConfiguration(scheme, _config.Feel.Attack);
            typeof(PlayerConfig).GetProperty("CapturedCombat", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(_config, combat);
            var player = Character(new Vector2(0, 5)); var probe = StartAction(player);
            var payload = new EntityHitPayload(); PlayerDashEffectTestAssets.Set(payload, "_damage", lethal ? 20f : 1f);
            PlayerDashEffectTestAssets.Set(payload, "_stunSeconds", .3f);
            player.Context.HitReceiver.Receive(new EntityHit(payload, 1)); Decide(player);
            Assert.That(probe.Lease.IsActive, Is.False); Assert.That(probe.Exits, Is.EqualTo(1));
            Assert.That(probe.LastEnd, Is.EqualTo(lethal ? PlayerActionEndReason.Death : PlayerActionEndReason.Reaction));
            Assert.That(player.CurrentStateId, Is.EqualTo(lethal ? "life.dead" : "life.hit"));
        }
    }
}
