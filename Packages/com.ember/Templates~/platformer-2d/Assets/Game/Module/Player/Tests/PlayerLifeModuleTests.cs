#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Ember.Scene;
using Game.Entity;
using Game.Input;
using Game.SceneFlow;
using System.Threading;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests
    {
        [UnityTest] public IEnumerator DeathModuleClosesOnlyGameplayAndScopeRecoveryKeepsWorldAndSavedHealth()
            => LifeModule(false).ToCoroutine();
        [UnityTest] public IEnumerator CustomDeathHandlerRunsOnceOutsideFsmAndUsesSameRecoveryTransaction()
            => LifeModule(true).ToCoroutine();
        [UnityTest] public IEnumerator DeathRecoverEntryRejectsRepeatedRequestAndCommitsThroughFsm()
            => LifeModule(false, true).ToCoroutine();

        private async UniTask LifeModule(bool custom, bool useFlow = false)
        {
            string folder = "Assets/LifeModuleTest_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder.Substring(7));
            PlayerSchemeCapture capture = null; PlayerModule module = null; Game.SceneFlow.GameSceneSession sceneSession = null;
            var original = _config;
            try
            {
                var actions = Keep(InputActionAsset.FromJson(_input.Service.GetChannel("gameplay").GetAction("Primary").actionMap.asset.ToJson()));
                actions.devices = new InputDevice[] { _keyboard };
                var confirm = actions.AddActionMap("UI").AddAction("Confirm", InputActionType.Button, "<Keyboard>/enter");
                var inputConfig = Keep(ScriptableObject.CreateInstance<GameInputConfig>());
                inputConfig.Configure(actions, new GameInputChannelConfig("gameplay", "Gameplay"), new GameInputChannelConfig("ui", "UI", true));
                ((IEmberModule)_input).OnDestroy(); _input = new GameInputModule();
                _input.SetConfigurationForNextInit(inputConfig, actions); ((IEmberModule)_input).OnInit();
                var copy = Type.GetType("Game.Player.Editor.PlayerSchemeStructureWorkflow, Game.Player.Editor", true);
                var source = Resources.Load<PlayerScheme>("Config/Player/BuiltinSchemes/空洞骑士风格");
                var scheme = (PlayerScheme)copy.GetMethod("CopyIndependent").Invoke(null,
                    new object[] { source, folder + "/Scheme.asset", folder + "/Graph.asset", folder + "/Visual.controller" });
                var life = JsonUtility.FromJson<PlayerLifeConfiguration>("{\"_enabled\":true,\"_maximum\":10,\"_initial\":10,\"_multiplier\":1,\"_death\":" + (custom ? "2" : "1") + ",\"_customDeathHandler\":\"test\"}");
                var reactions = JsonUtility.FromJson<PlayerReactionConfiguration>("{\"_enabled\":false,\"_invulnerability\":false}");
                Type.GetType("Game.Player.Editor.PlayerCombatExampleWorkflow, Game.Player.Editor", true).GetMethod("InstallLife").Invoke(null,
                    new object[] { scheme, life, reactions, null, null });
                if (custom)
                {
                    PlayerDashEffectTestAssets.Set(scheme.Recovery, "_position", PlayerRecoveryPositionMode.Custom);
                    PlayerDashEffectTestAssets.Set(scheme.Recovery, "_customPositionProvider", "test");
                }
                PlayerDashEffectTestAssets.Set(_config, "_scheme", scheme);
                capture = new PlayerSchemeCapture(_config); _config = capture.Config;
                typeof(PlayerConfig).GetProperty("CapturedAnimation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_config, null);
                module = Module(); var player = Character();
                var handler = custom ? player.gameObject.AddComponent<LifeDeathProbe>() : null;
                var scope = Scope(module, player); var participant = scope.GetComponent<PlayerScopeDependencyProbe>();
                sceneSession = await scope.PrepareAsync(501, PlayerEntryRequest.NewGame(), default);
                ((IEmberModule)module).OnInit(); sceneSession.Start();
                int notices = 0; module.DeathAwaitingConfirmation += _ => notices++;
                var payload = new EntityHitPayload(); PlayerDashEffectTestAssets.Set(payload, "_damage", 3f);
                player.Context.HitReceiver.Receive(new EntityHit(payload, 1));
                Assert.That((await module.Save(0).Completion).Succeeded, Is.True);
                var saved = module.RecoveryTarget; Assert.That(saved.Life.Health, Is.EqualTo(7));
                PlayerDashEffectTestAssets.Set(payload, "_damage", 20f);
                player.Context.HitReceiver.Receive(new EntityHit(payload, 1));
                ((IEmberFixedUpdate)module).FixedUpdate(); ((IEmberFixedUpdate)module).FixedUpdate();
                Assert.That(player.CurrentStateId, Is.EqualTo("life.dead"));
                Assert.That(_input.Service.GetChannel("gameplay").IsAvailable, Is.False);
                int confirms = 0; confirm.performed += _ => confirms++;
                Keys(Key.Enter); Keys();
                Assert.That(_input.Service.GetChannel("ui").IsAvailable, Is.True);
                Assert.That(confirms, Is.EqualTo(1), "死亡后独立UI通道仍接收真实虚拟键盘确认");
                Assert.That(Time.timeScale, Is.EqualTo(1), "死亡不暂停世界或屏蔽独立UI确认");
                Assert.That(custom ? handler.Count : notices, Is.EqualTo(1));
                if (custom) Assert.That(handler.State, Is.EqualTo("life.dead"));
                var dead = player.Context;
                var hitScene = (PlayerHitScene)typeof(PlayerModule).GetField("_hits", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(module);
                hitScene.Batch.TryGet(dead.Entity, out var oldTarget);
                var sourceEntity = Character().GetComponent<Entity2D>();
                using var hitSource = hitScene.Batch.Register(sourceEntity, "late-source", 2, new EntityHitReceiver(null, null, null));
                Assert.That(hitScene.Batch.Collect(hitSource, 1, "late", oldTarget, new EntityHit(payload, 1)), Is.True);
                using (module.Pause())
                {
                    await AssertThrowsAsync<ArgumentOutOfRangeException>(async () =>
                        await sceneSession.PrepareReplacementAsync(participant, 502,
                            PlayerEntryRequest.FromSnapshot(saved.WithRecovery(saved.Position, 11)), default));
                    Assert.That(player.Context, Is.SameAs(dead));
                    var restored = dead.Life.RecoverySnapshot(saved, _config.Spawn);
                    if (useFlow)
                    {
                        var collector = EmberModuleCollector.Instance;
                        bool wasActive = collector.TryGetModule<PlayerModule>(out var registered) && registered.IsActive;
                        var flow = new EmberStateMachine();
                        var host = new LifeRecoveryFlowProbe(module, sceneSession, participant);
                        flow.Register(host); flow.OnSceneTransitionAsync = new EmberSceneTransitionRunner(new LifeRecoveryScenes()).RunAsync;
                        try
                        {
                            flow.Start<LifeRecoveryFlowProbe>();
                            var operation = module.Recover(flow);
                            Assert.That(flow.IsTransitioning, Is.True);
                            Assert.Throws<InvalidOperationException>(() => module.Recover(flow));
                            host.Ready.TrySetResult();
                            Assert.That((await operation.Completion).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                            Assert.That(host.Commits, Is.EqualTo(1));
                        }
                        finally
                        {
                            flow.StopTransitions();
                            if (!wasActive) collector.DestroyPhase(ModulePhase.Gameplay);
                        }
                    }
                    else
                    {
                        var replacement = await sceneSession.PrepareReplacementAsync(participant, 503, PlayerEntryRequest.FromSnapshot(restored), default);
                        replacement.Commit();
                    }
                    hitScene.Batch.Resolve();
                    Assert.That(oldTarget.IsValid, Is.False, "旧玩家目标必须注销，排队致死命中不得进入新玩家");
                    Assert.That(player.Context.Health.Current, Is.EqualTo(10)); Assert.That(player.Context, Is.Not.SameAs(dead));
                    Assert.That(player.Context.Motor.Position, Is.EqualTo(custom ? new Vector2(12, 8) : saved.Position));
                    Assert.That(sceneSession.IsRunning, Is.True); Assert.That(module.State, Is.EqualTo(PlayerSessionState.Paused));
                }
                Assert.That(_input.Service.GetChannel("gameplay").IsAvailable, Is.True);
                using (module.Pause())
                {
                    var load = await sceneSession.PrepareReplacementAsync(participant, 504, PlayerEntryRequest.FromSlot(0), default);
                    load.Commit(); Assert.That(player.Context.Health.Current, Is.EqualTo(7), "普通读档不套死亡回血策略");
                }
                Assert.That(module.State, Is.EqualTo(PlayerSessionState.Running));
            }
            finally
            {
                sceneSession?.Dispose(); module?.StopSession(); capture?.Dispose(); _config = original;
                PlayerDashEffectTestAssets.Set(original, "_scheme", null);
                foreach (string suffix in new[] { "/Scheme.asset", "/Graph.asset", "/Visual.controller" })
                    foreach (var item in AssetDatabase.LoadAllAssetsAtPath(folder + suffix)) if (item != null) Undo.ClearUndo(item);
                AssetDatabase.DeleteAsset(folder);
            }
        }
        private async UniTask AssertThrowsAsync<T>(Func<UniTask> operation) where T : Exception
        {
            try { await operation(); }
            catch (T) { return; }
            Assert.Fail("Expected " + typeof(T).Name);
        }
    }
    public sealed class LifeDeathProbe : MonoBehaviour, IPlayerDeathHandler, IPlayerRecoveryPositionProvider
    {
        public string HandlerId => "test";
        public string ProviderId => "test";
        public Vector2 ResolvePosition(PlatformerSnapshot checkpoint) => new Vector2(12, 8);
        public int Count; public string State;
        public void OnDeath(PlayerModule module, PlayerController player) { Count++; State = player.CurrentStateId; }
    }
    // 隔离展示与模块查找；FSM声明/仲裁、公共原位Runner、玩家恢复适配及Scope替换均走生产实现。
    internal sealed class LifeRecoveryFlowProbe : GameplayState, IEmberInPlaceState
    {
        private readonly PlayerModule _player;
        private readonly GameSceneSession _session;
        private readonly GameSceneParticipant _participant;
        internal readonly UniTaskCompletionSource Ready = new();
        internal int Commits;
        internal LifeRecoveryFlowProbe(PlayerModule player, GameSceneSession session, GameSceneParticipant participant)
        { _player = player; _session = session; _participant = participant; }
        public override TransitionDescriptor[] GetEdges() => new[] { new TransitionDescriptor(typeof(GameplayState), "恢复") };
        public async UniTask PrepareInPlaceAsync(SceneTransitionContext request)
        {
            await Ready.Task;
            var recovery = new PlayerRecoveryTransition(_player); request.PreparedArgs = recovery;
            await recovery.PrepareAsync(_session, _participant, request.Operation.RequestId, (PlayerEntryRequest)request.Args, request.Cancellation);
        }
        public void CommitInPlace(SceneTransitionContext request) { ((PlayerRecoveryTransition)request.PreparedArgs).Commit(); Commits++; }
        public UniTask AbortInPlaceAsync(SceneTransitionContext request)
        { (request.PreparedArgs as PlayerRecoveryTransition)?.Dispose(); return UniTask.CompletedTask; }
    }
    internal sealed class LifeRecoveryScenes : IEmberTransitionScenes
    {
        public bool IsSceneLoaded(string scene) => UnityEngine.SceneManagement.SceneManager.GetSceneByName(scene).isLoaded;
        public UniTask<EmberTransitionSceneLoad> LoadForTransitionAsync(string scene, CancellationToken cancellation)
            => throw new InvalidOperationException("原位恢复不应加载场景。");
        public UniTask UnloadForTransitionAsync(string scene) => throw new InvalidOperationException("原位恢复不应卸载世界。");
        public void SetCurrentScene(string scene) => throw new InvalidOperationException("原位恢复不应切场景。");
    }
}
#endif
