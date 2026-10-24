using Game.Input;
using NUnit.Framework;
using Game.Entity;
using Ember.Core.Fsm;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Game.Player.Tests
{
    public sealed class PlayerCombatInputTests : InputTestFixture
    {
        private Keyboard _keyboard;
        private InputActionAsset _actions;
        private GameInputConfig _config;
        private GameInputService _service;
        private PlayerInputAdapter _adapter;
        private PlayerFeelProfile _feel;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _actions = ScriptableObject.CreateInstance<InputActionAsset>();
            _actions.devices = new InputDevice[] { _keyboard };
            var map = _actions.AddActionMap("Play");
            map.AddAction("Move", InputActionType.Value).AddCompositeBinding("2DVector")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            map.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            map.AddAction("Dash", InputActionType.Button, "<Keyboard>/leftShift");
            map.AddAction("Attack", InputActionType.Button, "<Keyboard>/j");
            _config = ScriptableObject.CreateInstance<GameInputConfig>();
            _config.Configure(_actions, new GameInputChannelConfig("play", "Play"));
            _service = new GameInputService(_config, _actions);
            _feel = ScriptableObject.CreateInstance<PlayerFeelProfile>();
            _feel.Configure(ScriptableObject.CreateInstance<PlayerMoveFeel>(), ScriptableObject.CreateInstance<PlayerJumpFeel>(),
                ScriptableObject.CreateInstance<PlayerInputFeel>());
            _adapter = new PlayerInputAdapter(_service.GetChannel("play"), "Move", "Jump", .1f, "Dash", "Attack", _feel.Capture(Physics2D.gravity));
            _service.Request(this, 0, new GameInputRule("play", GameInputPermission.Allow));
        }

        public override void TearDown()
        {
            _adapter?.Dispose(); _service?.Dispose();
            if (_feel != null)
            {
                Object.DestroyImmediate(_feel.Movement); Object.DestroyImmediate(_feel.Jump);
                Object.DestroyImmediate(_feel.Input); Object.DestroyImmediate(_feel);
            }
            Object.DestroyImmediate(_actions); Object.DestroyImmediate(_config);
            base.TearDown();
        }
        private void Keys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); InputSystem.Update(); _adapter.Sample();
        }

        [Test]
        public void CombatDashBuffersButMovementDashDoesNotAndHoldNeverRefreshes()
        {
            Keys(Key.LeftShift); Assert.That(_adapter.DashRequested, Is.True);
            _adapter.EndPhysicsStep(.02f); Assert.That(_adapter.DashRequested, Is.False);
            Keys(); _adapter.Combat.Begin(PlayerAttackIntent.Normal, 1, 1);
            Keys(Key.LeftShift, Key.J);
            ulong version = _adapter.AttackPressVersion;
            _adapter.EndPhysicsStep(.1f); _adapter.Sample();
            Assert.That(_adapter.DashRequested, Is.True); Assert.That(_adapter.AttackRequested, Is.True);
            Assert.That(_adapter.AttackPressVersion, Is.EqualTo(version));
            _adapter.EndPhysicsStep(.11f); _adapter.Sample();
            Assert.That(_adapter.DashRequested, Is.False); Assert.That(_adapter.AttackRequested, Is.False);
        }

        [Test]
        public void CommittingOneActionClearsPeersAndUnavailableChannelClearsPending()
        {
            _adapter.Combat.Begin(PlayerAttackIntent.Normal, 1, 1);
            Keys(Key.LeftShift, Key.Space, Key.J);
            Assert.That(_adapter.JumpRequested && _adapter.DashRequested && _adapter.AttackRequested, Is.True);
            Assert.That(_adapter.ConsumeJump(), Is.True);
            Assert.That(_adapter.JumpRequested || _adapter.DashRequested || _adapter.AttackRequested, Is.False);
            Keys(); Keys(Key.J); Assert.That(_adapter.AttackRequested, Is.True);
            using var denial = _service.Request(new object(), 1, new GameInputRule("play", GameInputPermission.Deny));
            Assert.That(_adapter.AttackRequested, Is.False);
        }

        [TestCase(true, false, true, false, false, "dash")]
        [TestCase(false, false, true, false, false, "jump")]
        [TestCase(false, false, false, false, false, "attack")]
        [TestCase(true, true, false, false, false, "attack")]
        [TestCase(true, true, true, true, false, "attack")]
        [TestCase(true, true, true, true, true, "move")]
        public void ActualFsmContinuesAfterClosedGateOrEntryRejectionAndCommitsOnlyWinner(
            bool dashOpen, bool dashReject, bool jumpOpen, bool jumpReject, bool attackReject, string expected)
        {
            var body = new GameObject("CombatArbitrationTest");
            var graph = ScriptableObject.CreateInstance<EmberFsmGraph>();
            var definitions = Enumerable.Range(0, 5).Select(_ => ScriptableObject.CreateInstance<PlayerCombatArbitrationProbe>()).ToArray();
            using var powers = PlayerPowerSession.CreateEmpty();
            try
            {
                var entity = body.AddComponent<Entity2D>(); body.AddComponent<Rigidbody2D>();
                var shape = body.AddComponent<BoxCollider2D>(); var motor = body.AddComponent<EntityMotor2D>();
                var sensors = body.AddComponent<EntitySensors2D>(); sensors.Configure(shape, 1);
                var facing = body.AddComponent<EntityFacing2D>();
                var context = new PlayerContext(entity, motor, sensors, facing, _adapter, new PlayerJumpWindow(0), powers, null,
                    _feel.Capture(Physics2D.gravity));
                string[] ids = { "source", "dash", "jump", "attack", "move" };
                definitions[0].Gates[0] = dashOpen; definitions[0].Gates[1] = jumpOpen;
                definitions[1].Reject = dashReject; definitions[2].Reject = jumpReject; definitions[3].Reject = attackReject;
                for (int i = 1; i < definitions.Length; i++) definitions[i].Action = i - 1;
                graph.Configure("source", ids.Select((id, i) => new EmberFsmNode(id, id, definitions[i], bindings: i == 0
                    ? ids.Skip(1).Select(target => new EmberFsmTargetBinding(target, target)).ToArray() : System.Array.Empty<EmberFsmTargetBinding>())).ToArray());
                Assert.That(graph.Inspect(typeof(PlayerContext)).IsValid, Is.True);
                using var fsm = new EmberFsm<PlayerContext>(graph, context);
                Assert.That(fsm.Start(), Is.EqualTo(EmberFsmResult.Changed));
                _adapter.Combat.Begin(PlayerAttackIntent.Normal, 1, 1);
                Keys(Key.LeftShift, Key.Space, Key.J, Key.D);
                fsm.FixedUpdate(.02f);
                Assert.That(fsm.CurrentLeafId, Is.EqualTo(expected));
                Assert.That(definitions.Sum(d => d.Commits), Is.EqualTo(1));
                Assert.That(definitions[System.Array.IndexOf(ids, expected)].Commits, Is.EqualTo(1));
                Assert.That(_adapter.JumpRequested || _adapter.DashRequested || _adapter.AttackRequested, Is.False);
            }
            finally
            {
                foreach (var definition in definitions) Object.DestroyImmediate(definition);
                Object.DestroyImmediate(graph); Object.DestroyImmediate(body);
            }
        }
    }

    public sealed partial class PlayerStateTests
    {
        [Test]
        public void ExplicitMotionControlBlocksParentJumpHorizontalAndFacingUntilReleased()
        {
            var player = Character(); Land(player);
            var owner = new object(); var obsolete = new object();
            player.Context.MotionControl.Take(obsolete, PlayerMotionChannels.Horizontal);
            player.Context.MotionControl.Take(owner, PlayerMotionChannels.Horizontal | PlayerMotionChannels.Facing | PlayerMotionChannels.AutomaticTransitions);
            player.Context.MotionControl.Release(obsolete);
            Keys(Key.A, Key.Space); Decide(player);
            Assert.That(player.Context.Motor.Velocity.x, Is.Zero);
            Assert.That(player.Context.Facing.FacesRight, Is.True);
            Assert.That(player.CurrentStateId, Is.Not.EqualTo("jump"));
            Assert.That(player.Context.Input.JumpRequested, Is.True, "父级拒绝后原跳跃缓存仍有效且未消费");
            player.Context.MotionControl.Release(owner);
            Keys(); Decide(player); Keys(Key.A, Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
            Assert.That(player.Context.Facing.FacesRight, Is.False);
        }
    }
}
