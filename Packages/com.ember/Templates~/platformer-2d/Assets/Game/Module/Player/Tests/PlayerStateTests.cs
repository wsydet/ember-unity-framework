using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ember.Core.Fsm;
using Game.Entity;
using Game.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    /// <summary>用正式图资产、虚拟键盘和隔离物理场景验证玩家行为。</summary>
    public sealed partial class PlayerStateTests : InputTestFixture
    {
        private readonly List<UnityEngine.Object> _objects = new();
        private readonly List<PlayerController> _characters = new();
        private Scene _scene;
        private GameInputService _input;
        private Keyboard _keyboard;
        private PlayerConfig _config;
        private const float Step = 0.02f;

        public override void Setup()
        {
            base.Setup();
            _scene = SceneManager.CreateScene("GameplayScene", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            _keyboard = InputSystem.AddDevice<Keyboard>();
            var actions = Keep(ScriptableObject.CreateInstance<InputActionAsset>());
            actions.devices = new InputDevice[] { _keyboard };
            var map = actions.AddActionMap("Gameplay");
            map.AddAction("Direction", InputActionType.Value).AddCompositeBinding("2DVector")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s");
            map.AddAction("Primary", InputActionType.Button, "<Keyboard>/space");
            map.AddAction("Dash", InputActionType.Button, "<Keyboard>/leftShift");
            map.AddAction("Attack", InputActionType.Button, "<Keyboard>/j");
            var inputConfig = Keep(ScriptableObject.CreateInstance<GameInputConfig>());
            inputConfig.Configure(actions, new GameInputChannelConfig("gameplay", "Gameplay"));
            _input = new GameInputService(inputConfig, actions);
            _config = Keep(ScriptableObject.CreateInstance<PlayerConfig>());
            var feel = Keep(ScriptableObject.CreateInstance<PlayerFeelProfile>());
            feel.Configure(Keep(ScriptableObject.CreateInstance<PlayerMoveFeel>()),
                Keep(ScriptableObject.CreateInstance<PlayerJumpFeel>()), Keep(ScriptableObject.CreateInstance<PlayerInputFeel>()));
            _config.SetFeelProfile(feel);
            var graph = Resources.Load<EmberFsmGraph>("Config/Player/PlayerGraph");
            Assert.That(graph, Is.Not.Null, "正式玩家图资产必须安装后运行这些测试");
            Assert.That(graph.Inspect(typeof(PlayerContext)).IsValid, Is.True);
            _config.Configure(graph, new Vector2(0, 0.9f));
            var floor = Keep(new GameObject("Floor"));
            SceneManager.MoveGameObjectToScene(floor, _scene);
            floor.transform.position = new Vector3(0, -0.5f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(24, 1);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (var character in _characters) if (character != null) character.Stop();
            _characters.Clear();
            _input?.Dispose();
            foreach (var item in _objects) if (item != null) UnityEngine.Object.DestroyImmediate(item);
            _objects.Clear();
            yield return SceneManager.UnloadSceneAsync(_scene);
        }

        private T Keep<T>(T value) where T : UnityEngine.Object { _objects.Add(value); return value; }
        private void Keys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }
        private PlayerController Character(Vector2? position = null, Vector2? velocity = null)
        {
            var go = Keep(new GameObject("StateTestPlayer"));
            SceneManager.MoveGameObjectToScene(go, _scene);
            go.AddComponent<Entity2D>();
            var body = go.AddComponent<Rigidbody2D>();
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            var collider = go.AddComponent<BoxCollider2D>(); collider.size = new Vector2(1, 1.75f);
            var material = Keep(new PhysicsMaterial2D { friction = 0 }); collider.sharedMaterial = material;
            go.AddComponent<EntityMotor2D>();
            go.AddComponent<EntitySensors2D>().Configure(collider, 1);
            go.AddComponent<EntityFacing2D>();
            var player = go.AddComponent<PlayerController>(); _characters.Add(player);
            var p = position ?? _config.Spawn; var v = velocity ?? Vector2.zero;
            var owner = new object();
            player.PrepareNewGame(owner, _config, _input, p, v);
            player.StartPrepared(owner);
            _input.Request(owner, 0, new GameInputRule("gameplay", GameInputPermission.Allow));
            Physics2D.SyncTransforms();
            return player;
        }
        private void Decide(PlayerController player) { player.Tick(Step); player.FixedTick(Step); }
        private void Simulate(PlayerController player)
        {
            Decide(player);
            Assert.That(_scene.GetPhysicsScene2D().Simulate(Step), Is.True);
        }
        private void Land(PlayerController player)
        {
            for (int i = 0; i < 150; i++)
            {
                Simulate(player);
                if (player.CurrentStateId == "idle" || player.CurrentStateId == "move") return;
            }
            Assert.Fail("玩家没有在 3 秒内落地，当前状态：" + player.CurrentStateId);
        }
        // 只读取内核公开诊断，避免为了测试把运行状态实例公开给业务。
        private EmberFsm<PlayerContext> Fsm(PlayerController player) =>
            (EmberFsm<PlayerContext>)typeof(PlayerController).GetProperty("_fsm", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(player);

        [Test]
        public void IdleMove_PreserveParents_AndApplyFacingAndSpeed()
        {
            var player = Character(); Land(player);
            CollectionAssert.AreEqual(new[] { "basic-drive", "ground", "idle" }, player.ActiveStatePath);
            var root = Fsm(player).GetActivation("basic-drive");
            var ground = Fsm(player).GetActivation("ground");
            Keys(Key.A); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("move"));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(-5));
            Assert.That(player.Context.Facing.FacesRight, Is.False);
            Assert.That(Fsm(player).GetActivation("basic-drive"), Is.EqualTo(root));
            Assert.That(Fsm(player).GetActivation("ground"), Is.EqualTo(ground));
            Keys(); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
            Assert.That(player.Context.Motor.Velocity.x, Is.Zero);
        }

        [Test]
        public void GroundJump_ShortCircuitsOldAndNewChildren_AndLaunchesOnce()
        {
            var player = Character(); Land(player);
            Keys(Key.D, Key.Space); Decide(player);
            CollectionAssert.AreEqual(new[] { "basic-drive", "air", "jump" }, player.ActiveStatePath);
            // Jump.FixedUpdate 若在新进入的同一轮运行，会看到执行指令前的 y<=0 并错误转 Fall。
            Assert.That(player.Context.Motor.Velocity, Is.EqualTo(new Vector2(5, 10)));
            Assert.That(player.Context.Input.ConsumeJump(), Is.False);
            _scene.GetPhysicsScene2D().Simulate(Step);
            Decide(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.LessThan(10).And.GreaterThan(0));
            Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
        }

        [Test]
        public void Gravity_DrivesApexFallAndLanding_WithoutHeldRepeat()
        {
            var player = Character(); Land(player); Keys(Key.Space); Decide(player);
            bool sawFall = false;
            for (int i = 0; i < 100; i++)
            {
                Simulate(player); sawFall |= player.CurrentStateId == "fall";
                if (sawFall && player.CurrentStateId == "idle") break;
            }
            Assert.That(sawFall, Is.True); Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
            for (int i = 0; i < 4; i++) Simulate(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
            Keys(); Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
        }

        [Test]
        public void AirPress_DoesNotDoubleJumpOrRemainBufferedAfterLanding()
        {
            var player = Character(new Vector2(0, 2), new Vector2(0, -1));
            Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(-1));
            Keys(); Land(player); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
        }

        [Test]
        public void LeavingEdge_AfterCoyoteExpires_RejectsJump()
        {
            var player = Character(); Land(player);
            var body = player.GetComponent<Rigidbody2D>();
            body.position = new Vector2(14, 1); player.transform.position = body.position;
            Physics2D.SyncTransforms();
            for (int i = 0; i < 5; i++) Decide(player);
            Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
            Assert.That(player.Context.Motor.Velocity.y, Is.LessThanOrEqualTo(0));
        }

        [TestCase(4, "jump")]
        [TestCase(8, "jump")]
        [TestCase(-4, "fall")]
        public void RestoreAirborneVelocity_SelectsBranchWithoutNewImpulse(float verticalSpeed, string state)
        {
            var player = Character(new Vector2(0, 5), new Vector2(0, verticalSpeed));
            Assert.That(player.CurrentStateId, Is.EqualTo(state));
            Decide(player);
            Assert.That(player.Context.Motor.Velocity.y, Is.EqualTo(verticalSpeed));
        }

        [Test]
        public void LandingWithDirection_EntersMoveWithoutAnIdleFrame()
        {
            var player = Character(new Vector2(0, 2)); Keys(Key.D); Land(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("move"));
            Assert.That(player.Context.Motor.Velocity.x, Is.EqualTo(5).Within(0.01f));
        }

        [Test]
        public void CeilingContact_EndsAscentAndAllowsLanding()
        {
            var ceiling = Keep(new GameObject("Ceiling")); SceneManager.MoveGameObjectToScene(ceiling, _scene);
            ceiling.transform.position = new Vector3(0, 3);
            ceiling.AddComponent<BoxCollider2D>().size = new Vector2(8, 0.2f);
            var player = Character(); Land(player); Keys(Key.Space); Decide(player);
            bool stoppedEarly = false;
            for (int i = 0; i < 15; i++)
            {
                Simulate(player);
                if (player.CurrentStateId == "fall") { stoppedEarly = true; break; }
            }
            Assert.That(stoppedEarly, Is.True, "应由顶头结束上升，早于自然最高点");
            Assert.That(player.Context.Motor.Position.y, Is.LessThan(2.1f));
            Land(player); Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
        }

        [Test]
        public void SharedGraph_CreatesIndependentStateObjectsAndActivationPaths()
        {
            var first = Character(new Vector2(-2, 0.9f)); var second = Character(new Vector2(2, 0.9f));
            Land(first); Land(second);
            var field = typeof(EmberFsm<PlayerContext>).GetField("_nodes", BindingFlags.Instance | BindingFlags.NonPublic);
            var a = (IDictionary)field.GetValue(Fsm(first)); var b = (IDictionary)field.GetValue(Fsm(second));
            foreach (string id in a.Keys)
            {
                var stateField = a[id].GetType().GetField("State", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(stateField.GetValue(a[id]), Is.Not.SameAs(stateField.GetValue(b[id])), id);
            }
            Keys(Key.Space); Decide(first);
            Assert.That(first.CurrentStateId, Is.EqualTo("jump"));
            Assert.That(second.CurrentStateId, Is.EqualTo("idle"));
            Decide(second); Assert.That(second.CurrentStateId, Is.EqualTo("jump"));
        }

        [Test]
        public void DifferentProfiles_CopyParameters_AndSharedDefinitionCreatesOneStatePerNode()
        {
            var source = _config.Graph;
            PlayerLocomotionStateDefinition Definition(float speed)
            {
                var result = Keep(ScriptableObject.CreateInstance<PlayerLocomotionStateDefinition>());
                typeof(PlayerMoveFeel).GetField("_speed", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(_config.Feel.Movement, speed);
                return result;
            }
            EmberFsmGraph Graph(PlayerLocomotionStateDefinition definition)
            {
                var graph = Keep(ScriptableObject.CreateInstance<EmberFsmGraph>());
                graph.Configure(source.EntryId, source.Nodes.Select(node => node.Id == "basic-drive"
                    ? new EmberFsmNode(node.Id, node.DisplayName, definition, defaultChildId: node.DefaultChildId, bindings: node.Bindings.ToArray()) : node)
                    .Append(new EmberFsmNode("another-locomotion", "另一个节点", definition, bindings: source.Nodes.First(n => n.Id == "basic-drive").Bindings.ToArray())).ToArray());
                return graph;
            }
            var slow = Definition(2); _config.Configure(Graph(slow), _config.Spawn);
            var first = Character(new Vector2(-3, 0.9f));
            _config.Configure(Graph(Definition(7)), _config.Spawn);
            var second = Character(new Vector2(3, 0.9f));
            // 运行实例复制配置；之后编辑共享 SO 不能改变已创建实例的参数。
            typeof(PlayerMoveFeel).GetField("_speed", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_config.Feel.Movement, 99f);
            Keys(Key.D); Decide(first); Decide(second);
            Assert.That(first.Context.Motor.Velocity.x, Is.EqualTo(2));
            Assert.That(second.Context.Motor.Velocity.x, Is.EqualTo(7));
            var nodes = (IDictionary)typeof(EmberFsm<PlayerContext>).GetField("_nodes", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(Fsm(first));
            var stateField = nodes["basic-drive"].GetType().GetField("State", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(stateField.GetValue(nodes["basic-drive"]), Is.Not.SameAs(stateField.GetValue(nodes["another-locomotion"])));
        }

        [Test]
        public void EntryRejection_DoesNotConsumeJumpOrApplyImpulse()
        {
            var rejected = Keep(ScriptableObject.CreateInstance<RejectedJumpDefinition>());
            var graph = Keep(ScriptableObject.CreateInstance<EmberFsmGraph>());
            graph.Configure(_config.Graph.EntryId, _config.Graph.Nodes.Select(node => node.Id == "jump"
                ? new EmberFsmNode(node.Id, node.DisplayName, rejected, node.ParentId) : node).ToArray());
            _config.Configure(graph, _config.Spawn);
            var player = Character(); Land(player); Keys(Key.Space); player.Tick(Step);
            var before = player.Context.Motor.Velocity;
            Assert.That(Fsm(player).GetActivation("ground").Request(PlayerGroundState.Jump), Is.EqualTo(EmberFsmResult.EntryRejected));
            Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
            Assert.That(player.Context.Input.JumpRequested, Is.True);
            Assert.That(player.Context.Motor.Velocity, Is.EqualTo(before));
        }

        [Test]
        public void PauseAndInputDeny_ClearPendingJump_RequireFreshPressAfterResume()
        {
            var player = Character(); Land(player); Keys(Key.Space); player.Tick(Step);
            var deny = _input.Request(this, 100, new GameInputRule("gameplay", GameInputPermission.Deny));
            player.Pause(); Assert.That(player.Context.Input.JumpRequested, Is.False);
            player.Resume(); deny.Dispose(); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
            Keys(); Keys(Key.Space); Decide(player);
            Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
        }
    }

    public sealed class RejectedJumpDefinition : EmberFsmStateDefinition<PlayerContext, RejectedJumpState>
    {
        public override RejectedJumpState CreateState() => new();
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => Array.Empty<EmberFsmTransition>();
    }
    public sealed class RejectedJumpState : EmberFsmState<PlayerContext>
    {
        public override EmberFsmEntryDecision CanEnter() => EmberFsmEntryDecision.Reject;
    }
}
