#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Ember.Core.Fsm;
using Game.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests
    {
        [UnityTest]
        public IEnumerator CombatRuntimeFaultReleasesActiveAttackAndModuleSession()
            => CombatRuntimeFault().ToCoroutine();

        private async UniTask CombatRuntimeFault()
        {
            var actions = Keep(InputActionAsset.FromJson(_input.Service.GetChannel("gameplay").GetAction("Primary").actionMap.asset.ToJson()));
            actions.devices = new InputDevice[] { _keyboard };
            actions.FindActionMap("Gameplay").AddAction("Attack", InputActionType.Button, "<Keyboard>/j");
            var inputConfig = Keep(ScriptableObject.CreateInstance<GameInputConfig>());
            inputConfig.Configure(actions, new GameInputChannelConfig("gameplay", "Gameplay"));
            ((IEmberModule)_input).OnDestroy(); _input = new GameInputModule();
            _input.SetConfigurationForNextInit(inputConfig, actions); ((IEmberModule)_input).OnInit();
            PlayerDashEffectTestAssets.Set(_config, "_scheme", Resources.Load<PlayerScheme>("Config/Player/BuiltinSchemes/通用攻击模板1"));
            var module = Module(); var player = FormalCharacter();
            var floor = Keep(new GameObject("CombatFaultFloor")); SceneManager.MoveGameObjectToScene(floor, _scene);
            floor.transform.position = new Vector3(0, -.5f); floor.AddComponent<BoxCollider2D>().size = new Vector2(30, 1);
            var session = await Prepare(module, player);
            ((IEmberModule)module).OnInit(); module.StartSession(session);
            try
            {
                for (int i = 0; i < 150 && player.CurrentStateId != "idle"; i++)
                { ((IEmberFixedUpdate)module).FixedUpdate(); await UniTask.Yield(PlayerLoopTiming.FixedUpdate); }
                Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
                Keys(Key.J); player.Tick(.02f); ((IEmberFixedUpdate)module).FixedUpdate(); Keys();
                Assert.That(player.CurrentStateId, Is.EqualTo("combat.attack.first"));
                for (int i = 0; i < 7; i++) { player.Tick(.02f); ((IEmberFixedUpdate)module).FixedUpdate(); }
                var slash = player.GetComponentsInChildren<SpriteRenderer>().Single(r => r.name == "Attack visual (session)");
                Assert.That(slash.enabled, Is.True);
                var context = player.Context;
                var animator = player.GetComponentInChildren<Animator>(); var animation = animator.runtimeAnimatorController;
                var runtime = (PlayerRuntimeCandidate)typeof(PlayerController).GetField("_active", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player);
                // 只替换当前实例的回调；保留原状态的退出/释放，不修改源图或冻结配置。
                var nodes = (IDictionary)runtime.Fsm.GetType().GetField("_nodes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(runtime.Fsm);
                var node = nodes[player.CurrentStateId];
                var field = node.GetType().GetField("State", BindingFlags.Instance | BindingFlags.NonPublic);
                var fault = new CombatFaultState((IEmberFsmState<PlayerContext>)field.GetValue(node));
                field.SetValue(node, fault);
                LogAssert.Expect(LogType.Error, new Regex("S03-runtime-combat-fault"));
                ((IEmberFixedUpdate)module).FixedUpdate();
                Assert.That(module.State, Is.EqualTo(PlayerSessionState.Faulted));
                Assert.That(module.LastError, Does.Contain("S03-runtime-combat-fault"));
                Assert.That(module.HasSession, Is.False); Assert.That(player.Context, Is.Null);
                Assert.That(runtime.IsDisposed, Is.True); Assert.That(context.Effects.IsDisposed, Is.True);
                Assert.That(player.GetComponent<Rigidbody2D>().simulated, Is.False);
                Assert.That(_input.Service.GetChannel("gameplay").IsAvailable, Is.False);
                Assert.That(slash == null, Is.True); Assert.That(animation == null, Is.True);
                Assert.That(fault.Exits, Is.EqualTo(1)); Assert.That(fault.Disposals, Is.EqualTo(1));
                module.StopSession(); module.StopSession(); player.Stop();
                Assert.That(fault.Exits, Is.EqualTo(1)); Assert.That(fault.Disposals, Is.EqualTo(1));
            }
            finally { module.StopSession(); }
        }

        private sealed class CombatFaultState : IEmberFsmState<PlayerContext>
        {
            private readonly IEmberFsmState<PlayerContext> _inner;
            internal int Exits, Disposals;
            internal CombatFaultState(IEmberFsmState<PlayerContext> inner) => _inner = inner;
            public void Initialize(PlayerContext context, string nodeId) => _inner.Initialize(context, nodeId);
            public EmberFsmEntryDecision CanEnter() => _inner.CanEnter();
            public void Enter(EmberFsmActivation activation) => _inner.Enter(activation);
            public void Update(float dt) => _inner.Update(dt);
            public void FixedUpdate(float dt) => throw new InvalidOperationException("S03-runtime-combat-fault");
            public void Exit() { Exits++; _inner.Exit(); }
            public void Dispose() { Disposals++; _inner.Dispose(); }
            public void Pause() => _inner.Pause();
            public void Resume() => _inner.Resume();
        }
    }
}
#endif
