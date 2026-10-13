using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Ember.Core.Fsm;
using Game.Entity;
using Game.Input;
using Game.Module;
using Game.PlayerData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests : InputTestFixture
    {
        private readonly List<UnityEngine.Object> _objects = new();
        private readonly List<PlayerModule> _players = new();
        private Scene _scene;
        private string _directory;
        private GameInputModule _input;
        private PlayerDataModule _data;
        private PlayerConfig _config;
        private Keyboard _keyboard;
        private float _timeScale;

        public override void Setup()
        {
            base.Setup();
            _timeScale = Time.timeScale; Time.timeScale = 1;
            _scene = SceneManager.CreateScene("GameplayScene");
            _keyboard = InputSystem.AddDevice<Keyboard>();
            var actions = Keep(ScriptableObject.CreateInstance<InputActionAsset>());
            actions.devices = new InputDevice[] { _keyboard };
            var map = actions.AddActionMap("Gameplay");
            map.AddAction("Direction", InputActionType.Value).AddCompositeBinding("2DVector")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d")
                .With("Down", "<Keyboard>/s");
            map.AddAction("Primary", InputActionType.Button, "<Keyboard>/space");
            map.AddAction("Dash", InputActionType.Button, "<Keyboard>/leftShift");
            var inputConfig = Keep(ScriptableObject.CreateInstance<GameInputConfig>());
            inputConfig.Configure(actions, new GameInputChannelConfig("gameplay", "Gameplay"));
            _input = new GameInputModule(); _input.SetConfigurationForNextInit(inputConfig, actions);
            ((IEmberModule)_input).OnInit();

            _directory = Path.Combine(Path.GetTempPath(), "EmberPlayerTests-" + Guid.NewGuid().ToString("N"));
            var dataConfig = Keep(ScriptableObject.CreateInstance<PlayerDataConfig>());
            dataConfig.Configure("Test", "ember.platformer2d.progress", new[] { 0, 1 });
            _data = new PlayerDataModule(); _data.SetConfigurationForNextInit(dataConfig, _directory);
            ((IEmberModule)_data).OnInit();

            var definition = Keep(ScriptableObject.CreateInstance<PlayerBasicStateDefinition>());
            var graph = Keep(ScriptableObject.CreateInstance<EmberFsmGraph>());
            graph.Configure("drive", new EmberFsmNode("drive", "drive", definition));
            _config = Keep(ScriptableObject.CreateInstance<PlayerConfig>());
            var feel = Keep(ScriptableObject.CreateInstance<PlayerFeelProfile>());
            feel.Configure(Keep(ScriptableObject.CreateInstance<PlayerMoveFeel>()),
                Keep(ScriptableObject.CreateInstance<PlayerJumpFeel>()), Keep(ScriptableObject.CreateInstance<PlayerInputFeel>()));
            _config.SetFeelProfile(feel);
            _config.Configure(graph, new Vector2(0, 3));
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            // Test Runner 恢复清理入口时需要编译器生成的迭代器状态字段。
            yield return CleanupAsync().ToCoroutine();
        }
        private async UniTask CleanupAsync()
        {
            foreach (var player in _players) ((IEmberModule)player).OnDestroy();
            _players.Clear();
            ((IEmberModule)_input).OnDestroy();
            await _data.StopAsync();
            foreach (var item in _objects) if (item != null) UnityEngine.Object.DestroyImmediate(item);
            _objects.Clear();
            await SceneManager.UnloadSceneAsync(_scene);
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
            Time.timeScale = _timeScale;
        }

        private T Keep<T>(T item) where T : UnityEngine.Object { _objects.Add(item); return item; }
        private void Keys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }
        private PlayerModule Module()
        {
            var result = new PlayerModule(); result.SetConfigurationForNextInit(_config); _players.Add(result); return result;
        }
        private PlayerController Character()
        {
            var go = Keep(new GameObject("TestPlayer")); SceneManager.MoveGameObjectToScene(go, _scene);
            go.AddComponent<Entity2D>();
            var body = go.AddComponent<Rigidbody2D>(); body.constraints = RigidbodyConstraints2D.FreezeRotation;
            var shape = go.AddComponent<BoxCollider2D>(); shape.size = new Vector2(1, 1.75f);
            go.AddComponent<EntityMotor2D>();
            go.AddComponent<EntitySensors2D>().Configure(shape, 1);
            go.AddComponent<EntityFacing2D>();
            return go.AddComponent<PlayerController>();
        }
        private async UniTask<PreparedPlayerSession> Prepare(PlayerModule module, PlayerController character,
            PlayerEntryRequest entry = null, CancellationToken cancellation = default)
            => await module.PrepareAsync(character, entry ?? PlayerEntryRequest.NewGame(), _input, _data, cancellation);

        private async UniTask<PlayerModule> Running()
        {
            var module = Module(); var candidate = await Prepare(module, Character());
            ((IEmberModule)module).OnInit(); module.StartSession(candidate); return module;
        }

        [UnityTest] public IEnumerator Preparation_DoesNotRunOrOpenInput_AndSurvivesReset() => Verify().ToCoroutine();
        private async UniTask Verify()
        {
            var module = Module(); var controller = Character();
            Assert.That(controller.GetComponent<Rigidbody2D>().simulated, Is.False);
            var candidate = await Prepare(module, controller);
            Assert.That(module.IsActive, Is.False); Assert.That(controller.IsRunning, Is.False);
            Assert.That(_input.Service.GetChannel("gameplay").IsAvailable, Is.False);
            Assert.Throws<InvalidOperationException>(() => module.StartSession(candidate));
            ((IEmberModule)module).ResetModuleData(); ((IEmberModule)module).OnInit();
            module.StartSession(candidate);
            Assert.That(controller.IsRunning, Is.True);
            Assert.That(_input.Service.GetChannel("gameplay").IsAvailable, Is.True);
            Assert.That(_data.GetSlots().LastSuccessfulSaveSlot, Is.Null);
        }

        [UnityTest] public IEnumerator ModuleIsTheOnlyDriver_InputThenPhysics() => Drive().ToCoroutine();
        private async UniTask Drive()
        {
            var module = await Running();
            Keys(Key.D);
            Assert.That(_keyboard.dKey.isPressed, Is.True, "虚拟键盘未接收输入");
            Assert.That(_input.Service.GetChannel("gameplay").ReadContinuous<Vector2>("Direction").x, Is.EqualTo(1), "通道未读取按键");
            ((IEmberUpdate)module).Update();
            Assert.That(module.Controller.Context.Motor.Velocity.x, Is.Zero);
            ((IEmberFixedUpdate)module).FixedUpdate();
            Assert.That(module.Controller.Context.Motor.Velocity.x, Is.EqualTo(5));
            Assert.That(module.Controller.Context.Facing.FacesRight, Is.True);
        }

        [UnityTest] public IEnumerator NestedPause_RestoresVelocity_AndRequiresFreshJump() => Pauses().ToCoroutine();
        private async UniTask Pauses()
        {
            var module = await Running(); var body = module.Controller.GetComponent<Rigidbody2D>();
            body.linearVelocity = new Vector2(2, 4);
            var local = module.Pause();
            Assert.That(Time.timeScale, Is.Zero, "第一个拥有者也必须暂停整个世界");
            Keys(Key.Space);
            var world = module.Pause();
            Assert.That(Time.timeScale, Is.Zero); Assert.That(AudioListener.pause, Is.False);
            local.Dispose(); Assert.That(module.State, Is.EqualTo(PlayerSessionState.Paused));
            Assert.That(Time.timeScale, Is.Zero);
            world.Dispose();
            Assert.That(Time.timeScale, Is.EqualTo(1)); Assert.That(body.linearVelocity, Is.EqualTo(new Vector2(2, 4)));
            Assert.That(module.Controller.Context.Input.ConsumeJump(), Is.False);
            Keys(); Keys(Key.Space);
            Assert.That(module.Controller.Context.Input.ConsumeJump(), Is.True);
            Assert.That(module.Controller.Context.Input.ConsumeJump(), Is.False);
        }

        [UnityTest] public IEnumerator CancelledCandidate_CannotStart() => CancelCandidate().ToCoroutine();
        private async UniTask CancelCandidate()
        {
            var module = Module(); using var cancellation = new CancellationTokenSource();
            var candidate = await Prepare(module, Character(), cancellation: cancellation.Token);
            cancellation.Cancel(); ((IEmberModule)module).OnInit();
            Assert.Throws<OperationCanceledException>(() => module.StartSession(candidate));
            candidate.Dispose(); Assert.That(candidate.Controller.IsPrepared, Is.False);
            Assert.That(_input.Service.GetChannel("gameplay").IsAvailable, Is.False);
        }

        [UnityTest] public IEnumerator MissingSlot_DoesNotStartDefaultOrChangeLastSaved() => Missing().ToCoroutine();
        private async UniTask Missing()
        {
            var module = Module(); bool failed = false;
            try { await Prepare(module, Character(), PlayerEntryRequest.FromSlot(0)); }
            catch (InvalidOperationException e) { failed = e.Message.Contains("NotFound"); }
            Assert.That(failed, Is.True); Assert.That(module.HasSession, Is.False);
            Assert.That(_data.LastSuccessfulSaveSlot, Is.Null);
        }

        [UnityTest] public IEnumerator ExplicitSave_DoesNotPauseWorld_AndRestoresIndependentData() => SaveRestore().ToCoroutine();
        private async UniTask SaveRestore()
        {
            var module = await Running(); var controller = module.Controller;
            controller.GetComponent<Rigidbody2D>().position = new Vector2(4, 5);
            var saved = await module.Save(0).Completion;
            Assert.That(saved.Succeeded, Is.True); Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(module.State, Is.EqualTo(PlayerSessionState.Running));
            module.StopSession(); ((IEmberModule)module).OnDestroy();
            var candidate = await Prepare(module, Character(), PlayerEntryRequest.FromSlot(0));
            ((IEmberModule)module).ResetModuleData(); ((IEmberModule)module).OnInit(); module.StartSession(candidate);
            Assert.That(module.Controller.Context.Motor.Position, Is.EqualTo(new Vector2(4, 5)));
            Assert.That(module.SourceSlot, Is.EqualTo(0));
            Assert.That(controller.IsPrepared, Is.False);
        }

        [UnityTest] public IEnumerator DestroyedCharacter_ClosesSessionAndRestoresWorldTime() => Destroyed().ToCoroutine();
        private async UniTask Destroyed()
        {
            var module = await Running(); var pause = module.Pause();
            UnityEngine.Object.DestroyImmediate(module.Controller.gameObject);
            Assert.That(module.HasSession, Is.False); Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(_input.Service.GetChannel("gameplay").IsAvailable, Is.False); pause.Dispose();
        }

        [UnityTest] public IEnumerator NewInputGeneration_RejectsPreparedCandidate() => Generation().ToCoroutine();
        private async UniTask Generation()
        {
            var module = Module(); var candidate = await Prepare(module, Character());
            ((IEmberModule)_input).OnDestroy(); ((IEmberModule)_input).OnInit();
            ((IEmberModule)module).OnInit();
            Assert.Throws<InvalidOperationException>(() => module.StartSession(candidate)); candidate.Dispose();
        }

        [UnityTest] public IEnumerator SameGraph_SeparateContextsAndFsmInstances() => Independent().ToCoroutine();
        private async UniTask Independent()
        {
            var first = await Running(); var second = await Running();
            Assert.That(first.Controller.Context, Is.Not.SameAs(second.Controller.Context));
            Assert.That(first.Controller.FsmInstanceId, Is.Not.EqualTo(second.Controller.FsmInstanceId));
            first.Controller.Pause();
            Assert.That(second.Controller.IsRunning, Is.True);
            Assert.That(second.Controller.GetComponent<Rigidbody2D>().simulated, Is.True);
        }

        [UnityTest] public IEnumerator Sensors_ExcludeOwnChild_AndFindRealFloor() => Ground().ToCoroutine();
        private async UniTask Ground()
        {
            var module = await Running(); var controller = module.Controller;
            var body = controller.GetComponent<Rigidbody2D>(); body.position = new Vector2(0, .9f);
            controller.transform.position = body.position;
            var own = new GameObject("OwnedCollider"); own.transform.SetParent(controller.transform, false);
            own.transform.localPosition = new Vector3(0, -0.9f, 0); own.AddComponent<BoxCollider2D>().size = new Vector2(.3f, .05f);
            Physics2D.SyncTransforms(); controller.Context.Sensors.Sample();
            Assert.That(Entity2D.Resolve(own.GetComponent<Collider2D>()), Is.SameAs(controller.Context.Entity));
            Assert.That(controller.Context.Sensors.IsGrounded, Is.False);
            var floor = Keep(new GameObject("Floor")); SceneManager.MoveGameObjectToScene(floor, _scene);
            floor.transform.position = new Vector3(0, -.5f, 0); floor.AddComponent<BoxCollider2D>().size = new Vector2(10, 1);
            Physics2D.SyncTransforms(); controller.Context.Sensors.Sample();
            Assert.That(controller.Context.Sensors.IsGrounded, Is.True);
            Assert.That(controller.Context.Sensors.Ground.gameObject, Is.SameAs(floor));
        }

        [UnityTest] public IEnumerator SaveAndExit_WaitsForCommitBeforeStateExit() => ExitSave().ToCoroutine();
        private async UniTask ExitSave()
        {
            var module = await Running();
            var flow = new EmberStateMachine(); var gameplay = new ExitProbe(module, _data);
            flow.Register(gameplay); flow.Register(new MainState()); flow.Start<ExitProbe>();
            // ExitProbe derives GameplayState so the business exit entrance recognizes it.
            var result = await module.ExitToMenuAsync(flow, 0);
            Assert.That(result.Status, Is.EqualTo(PlayerExitStatus.Completed));
            Assert.That(gameplay.SavedBeforeExit, Is.True);
            Assert.That(flow.Is<MainState>(), Is.True);
        }

        private sealed class ExitProbe : GameplayState
        {
            private readonly PlayerModule _module;
            private readonly PlayerDataModule _data;
            public bool SavedBeforeExit;
            public ExitProbe(PlayerModule module, PlayerDataModule data) { _module = module; _data = data; }
            protected override void OnGameplayExit()
            { SavedBeforeExit = _data.LastSuccessfulSaveSlot == 0; _module.StopSession(); }
        }

        [UnityTest] public IEnumerator FailedExitSave_KeepsPlayableSession() => FailedExit().ToCoroutine();
        private async UniTask FailedExit()
        {
            var module = await Running();
            var flow = new EmberStateMachine(); flow.Register(new ExitProbe(module, _data));
            flow.Register(new MainState()); flow.Start<ExitProbe>();
            var result = await module.ExitToMenuAsync(flow, 999);
            Assert.That(result.Status, Is.EqualTo(PlayerExitStatus.SaveFailed));
            Assert.That(module.State, Is.EqualTo(PlayerSessionState.Running));
            Assert.That(flow.Is<GameplayState>(), Is.True);
            Assert.That(_input.Service.GetChannel("gameplay").IsAvailable, Is.True);
            await flow.RequestTransitionTo<MainState>().Completion;
        }

        [UnityTest] public IEnumerator CancelledExit_KeepsSessionAndRestoresTime() => CancelExit().ToCoroutine();
        private async UniTask CancelExit()
        {
            var module = await Running();
            var flow = new EmberStateMachine(); flow.Register(new ExitProbe(module, _data));
            flow.Register(new MainState()); flow.Start<ExitProbe>();
            using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            var result = await module.ExitToMenuAsync(flow, null, cancellation.Token);
            Assert.That(result.Status, Is.EqualTo(PlayerExitStatus.Cancelled));
            Assert.That(module.State, Is.EqualTo(PlayerSessionState.Running));
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(_data.State, Is.EqualTo(SaveServiceState.Ready));
            await flow.RequestTransitionTo<MainState>().Completion;
        }

        [UnityTest] public IEnumerator SupersededPreparation_CannotStartOrStopNewSession() => Superseded().ToCoroutine();
        private async UniTask Superseded()
        {
            var module = Module(); var old = await Prepare(module, Character());
            var next = await Prepare(module, Character());
            ((IEmberModule)module).OnInit();
            Assert.Throws<OperationCanceledException>(() => module.StartSession(old));
            Assert.That(old.Controller.IsPrepared, Is.False);
            module.StartSession(next); old.Dispose();
            Assert.That(module.Controller, Is.SameAs(next.Controller));
            Assert.That(module.Controller.IsRunning, Is.True);
        }

        [Test] public void Codec_RejectsTruncatedUnknownAndNonFinite()
        {
            var codec = new PlatformerProgressCodec();
            Assert.That(codec.Decode(new byte[] { 1, 2 }, 1).Status, Is.EqualTo(DecodeStatus.DecodeFailed));
            Assert.That(codec.Decode(Array.Empty<byte>(), 2).Status, Is.EqualTo(DecodeStatus.DataVersionUnsupported));
            Assert.Throws<ArgumentException>(() => codec.Encode(new PlatformerProgress { Scene = "GameplayScene", X = float.NaN }));
        }
    }
}

