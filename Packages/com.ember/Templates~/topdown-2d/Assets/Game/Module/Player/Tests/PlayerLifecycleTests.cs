using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Ember.Core.Fsm;
using Game.Input;
using Game.Entity;
using Game.Module;
using Game.SceneFlow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerLifecycleTests : InputTestFixture
    {
        private readonly List<UnityEngine.Object> _objects = new();
        private GameInputModule _input;
        private PlayerDataModule _data;
        private PlayerModule _player;
        private Scene _scene;
        private string _root;
        private PlayerScheme _scheme;
        private EmberFsmGraph _graph;
        private Gamepad _gamepad;
        private Keyboard _keyboard;
        private Vector2 _testMove,_testAim;

        public override void Setup()
        {
            base.Setup();
            _scene=SceneManager.CreateScene("TopdownLifecycle-"+Guid.NewGuid().ToString("N"));
            var actions=Keep(ScriptableObject.CreateInstance<InputActionAsset>());
            _gamepad=InputSystem.AddDevice<Gamepad>();_keyboard=InputSystem.AddDevice<Keyboard>();
            actions.devices=new InputDevice[]{_gamepad,_keyboard};_testMove=Vector2.zero;_testAim=Vector2.zero;
            var map=actions.AddActionMap("Gameplay");
            map.AddAction("Move",InputActionType.Value,"<Gamepad>/leftStick",expectedControlLayout:"Vector2");
            map.AddAction("Aim",InputActionType.Value,"<Gamepad>/rightStick",expectedControlLayout:"Vector2");
            map.AddAction("Point",InputActionType.Value,expectedControlLayout:"Vector2");
            map.AddAction("Attack",InputActionType.Button,"<Keyboard>/j");
            map.AddAction("Dash",InputActionType.Button,"<Keyboard>/leftShift");
            map.AddAction("Interact",InputActionType.Button,"<Keyboard>/e");
            map.AddAction("Focus",InputActionType.Button,"<Keyboard>/f");
            map.AddAction("NextWeapon",InputActionType.Button,"<Keyboard>/q");
            actions.AddActionMap("UI").AddAction("Pause",InputActionType.Button,"<Keyboard>/escape");
            var input=Keep(ScriptableObject.CreateInstance<GameInputConfig>());
            input.Configure(actions,new GameInputChannelConfig("gameplay","Gameplay"),new GameInputChannelConfig("ui","UI",true));
            _input=new GameInputModule(); _input.SetConfigurationForNextInit(input,actions); ((IEmberModule)_input).OnInit();
            var config=Keep(ScriptableObject.CreateInstance<PlayerDataConfig>());
            config.Configure("Test","ember.topdown2d.progress",new[]{0});
            _root=Path.Combine(Path.GetTempPath(),"TopdownLifecycle-"+Guid.NewGuid().ToString("N"));
            _data=new PlayerDataModule(); _data.SetConfigurationForNextInit(config,_root); ((IEmberModule)_data).OnInit();
            _player=new PlayerModule();
            _graph=Keep(ScriptableObject.CreateInstance<EmberFsmGraph>());
            PlayerStateDefinition Definition(PlayerStateKind kind){var d=Keep(ScriptableObject.CreateInstance<PlayerStateDefinition>());d.Configure(kind);return d;}
            _graph.Configure("alive",new EmberFsmNode("alive","Alive",Definition(PlayerStateKind.Alive),null,"locomotion",new EmberFsmTargetBinding("dead","dead"),new EmberFsmTargetBinding("hit","hit")),
                new EmberFsmNode("locomotion","Locomotion",Definition(PlayerStateKind.Locomotion),"alive","idle",new EmberFsmTargetBinding("dash","dash")),
                new EmberFsmNode("idle","Idle",Definition(PlayerStateKind.Idle),"locomotion",null,new EmberFsmTargetBinding("move","move")),
                new EmberFsmNode("move","Move",Definition(PlayerStateKind.Move),"locomotion",null,new EmberFsmTargetBinding("idle","idle")),
                new EmberFsmNode("action","Action",Definition(PlayerStateKind.Action),"alive","dash"),
                new EmberFsmNode("dash","Dash",Definition(PlayerStateKind.Dash),"action",null,new EmberFsmTargetBinding("locomotion","locomotion")),
                new EmberFsmNode("hit","Hit",Definition(PlayerStateKind.Hit),"alive",null,new EmberFsmTargetBinding("locomotion","locomotion")),
                new EmberFsmNode("dead","Dead",Definition(PlayerStateKind.Dead)));
            _scheme=Keep(ScriptableObject.CreateInstance<PlayerScheme>());_scheme.Configure(_graph);
            _player.SetSchemeForNextSession(_scheme);
        }

        private T Keep<T>(T value) where T:UnityEngine.Object { _objects.Add(value); return value; }
        private void Sticks(Vector2? move=null,Vector2? aim=null)
        {
            if(move.HasValue)_testMove=move.Value;if(aim.HasValue)_testAim=aim.Value;
            InputSystem.QueueStateEvent(_gamepad,new UnityEngine.InputSystem.LowLevel.GamepadState{leftStick=_testMove,rightStick=_testAim});
        }
        private PlayerController Character()
        {
            var root=Keep(new GameObject("Test Player")); SceneManager.MoveGameObjectToScene(root,_scene);
            root.AddComponent<Entity2D>();
            var body=root.AddComponent<Rigidbody2D>(); body.bodyType=RigidbodyType2D.Kinematic; body.gravityScale=0;
            root.AddComponent<BoxCollider2D>(); return root.AddComponent<PlayerController>();
        }

        // NUnit 的同步 TearDown 先于 UnityTearDown；设备必须活到会话及输入订阅释放之后。
        public override void TearDown() { }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            // Unity Test Runner 恢复清理进度需要真实迭代器的程序计数字段。
            try { yield return CleanupAsync().ToCoroutine(); }
            finally { base.TearDown(); }
        }
        private async UniTask CleanupAsync()
        {
            if(_player!=null)((IEmberModule)_player).OnDestroy();if(_input!=null)((IEmberModule)_input).OnDestroy();if(_data!=null)await _data.StopAsync();
            foreach(var value in _objects) if(value!=null) UnityEngine.Object.DestroyImmediate(value);
            _objects.Clear();if(_scene.IsValid()&&_scene.isLoaded)await SceneManager.UnloadSceneAsync(_scene);
            if(!string.IsNullOrEmpty(_root)&&Directory.Exists(_root)) Directory.Delete(_root,true);
        }

        [UnityTest] public IEnumerator PrepareDoesNotActivateAndCandidateSurvivesModuleReset() => Preparation().ToCoroutine();
        private async UniTask Preparation()
        {
            var controller=Character(); var candidate=await _player.PrepareAsync(controller,_input,_data);
            Assert.IsTrue(controller.IsPrepared); Assert.IsFalse(controller.IsRunning || controller.Body.simulated);
            Assert.IsFalse(_input.Service.GetChannel("gameplay").IsAvailable);
            ((IEmberModule)_player).ResetModuleData(); ((IEmberModule)_player).OnInit(); _player.StartSession(candidate);
            Assert.IsTrue(controller.IsRunning && controller.Body.simulated);
            Assert.Throws<InvalidOperationException>(()=>_player.StartSession(candidate));
            candidate.Dispose(); candidate.Dispose(); Assert.IsFalse(_player.HasSession || controller.IsPrepared || controller.Body.simulated);
        }

        [UnityTest] public IEnumerator CancelledPreparationAndOldResultCannotReleaseLaterSession() => CancellationAndOldResult().ToCoroutine();
        private async UniTask CancellationAndOldResult()
        {
            var first=Character(); using var cancel=new CancellationTokenSource(); cancel.Cancel();
            var cancelled=false; try{await _player.PrepareAsync(first,_input,_data,cancel.Token);}catch(OperationCanceledException){cancelled=true;}
            Assert.IsTrue(cancelled); Assert.IsFalse(first.IsPrepared);
            var old=await _player.PrepareAsync(first,_input,_data); ((IEmberModule)_player).OnInit(); _player.StartSession(old); _player.StopSession();
            var next=await _player.PrepareAsync(Character(),_input,_data); _player.StartSession(next); old.Dispose();
            Assert.AreSame(next.Controller,_player.Controller); Assert.IsTrue(next.Controller.IsRunning);
        }

        [UnityTest] public IEnumerator LaterScopeFailureCleansTransferredPlayer() => Failure().ToCoroutine();
        private async UniTask Failure()
        {
            var controller=Character(); var scopeRoot=Keep(new GameObject("Scope")); SceneManager.MoveGameObjectToScene(scopeRoot,_scene);
            var scope=scopeRoot.AddComponent<GameSceneScope>(); var probe=scopeRoot.AddComponent<LifecycleParticipant>();
            probe.Controller=controller; probe.Module=_player; probe.Input=_input; probe.Data=_data;
            var fail=scopeRoot.AddComponent<LifecycleFailure>();
            typeof(GameSceneScope).GetField("_participants",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(scope,new GameSceneParticipant[]{probe,fail});
            var session=await scope.PrepareAsync(1,null,default); ((IEmberModule)_player).OnInit();
            Assert.Throws<InvalidOperationException>(()=>session.Start());
            Assert.IsTrue(session.IsDisposed); Assert.IsFalse(_player.HasSession || controller.IsPrepared || controller.Body.simulated);
            Assert.IsFalse(_input.Service.GetChannel("gameplay").IsAvailable);
        }
    }

    public sealed class LifecycleParticipant:GameSceneParticipant
    {
        public PlayerController Controller;
        [NonSerialized] public PlayerModule Module;
        [NonSerialized] public GameInputModule Input;
        [NonSerialized] public PlayerDataModule Data;
        public override async UniTask<IGameScenePreparedParticipant> PrepareAsync(GameScenePreparationContext context)
            => new Result(Module,await Module.PrepareAsync(Controller,Input,Data,context.Cancellation));
        private sealed class Result:IGameScenePreparedParticipant
        {
            private readonly PlayerModule _module; private readonly PreparedPlayerSession _candidate;
            internal Result(PlayerModule module,PreparedPlayerSession candidate){_module=module;_candidate=candidate;}
            public void Start()=>_module.StartSession(_candidate);
            public void Dispose()=>_candidate.Dispose();
        }
    }
    public sealed class LifecycleFailure:GameSceneParticipant
    {
        public override UniTask<IGameScenePreparedParticipant> PrepareAsync(GameScenePreparationContext context)=>UniTask.FromResult<IGameScenePreparedParticipant>(new Result());
        private sealed class Result:IGameScenePreparedParticipant {public void Start()=>throw new InvalidOperationException("expected start failure");public void Dispose(){} }
    }
}
