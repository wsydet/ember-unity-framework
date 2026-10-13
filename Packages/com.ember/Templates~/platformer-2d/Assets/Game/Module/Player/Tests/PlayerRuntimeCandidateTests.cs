using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Ember.Core.Fsm;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests
    {
        private async UniTask<(PlayerModule module, PreparedPlayerSession session)> CandidateHost()
        {
            var module = Module(); var session = await Prepare(module, Character());
            ((IEmberModule)module).OnInit(); module.StartSession(session);
            return (module, session);
        }

        [UnityTest] public IEnumerator SnapshotCandidatePreparationAndDiscardLeaveOldHostUntouched()
            => CandidateIsolation().ToCoroutine();
        private async UniTask CandidateIsolation()
        {
            InstallSessionPowers(); InstallEffects();
            var (module, session) = await CandidateHost(); var player = module.Controller;
            var old = player.Context; var fsm = player.FsmInstanceId;
            var sprite = player.gameObject.AddComponent<SpriteRenderer>();
            var effect = old.Effects.ApplyIndependent(SessionEffect(), "old");
            old.Skills.TryGet("test.skill", out var skill); skill.TryCommit(skill.CreateAttempt());
            var snapshot = PlatformerSnapshot.Capture(_config.Scene, old);
            var body = player.GetComponent<Rigidbody2D>(); body.position = new Vector2(6, 7);
            body.linearVelocity = new Vector2(2, 3); player.Tick(.5f);
            Keys(Key.Space);
            using var candidate = player.PrepareSnapshot(new object(), _config, snapshot, _input.Service);
            Assert.That(player.Context, Is.SameAs(old)); Assert.That(player.FsmInstanceId, Is.EqualTo(fsm));
            Assert.That(candidate.Fsm.InstanceId, Is.Not.EqualTo(fsm));
            Assert.That(candidate.Fsm.Status, Is.EqualTo(EmberFsmStatus.Stopped));
            Assert.That(candidate.Context.Powers, Is.Not.SameAs(old.Powers));
            Assert.That(candidate.Context.Effects, Is.Not.SameAs(old.Effects));
            Assert.That(candidate.Context.Input, Is.Not.SameAs(old.Input));
            Assert.That(body.position, Is.EqualTo(new Vector2(6, 7))); Assert.That(body.linearVelocity, Is.EqualTo(new Vector2(2, 3)));
            Assert.That(body.simulated, Is.True); Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(old.Input.ConsumeJump(), Is.True, "准备候选不能消费旧输入");
            candidate.Dispose(); candidate.Dispose();
            Assert.That(candidate.Context.Effects.IsDisposed, Is.True); Assert.That(effect.IsActive, Is.True);
            Assert.That(skill.RemainingCooldown, Is.EqualTo(9.5f)); Assert.That(sprite.color.a, Is.EqualTo(1));
            Assert.That(player.IsRunning, Is.True); Assert.That(module.PowerProgress, Is.SameAs(old.PowerProgress));
            Assert.That(_data.LastSuccessfulSaveSlot, Is.Null);
        }

        [UnityTest] public IEnumerator InvalidSnapshotCandidateKeepsCurrentResourcesAndAllowsRetry()
            => CandidateInvalid().ToCoroutine();
        private async UniTask CandidateInvalid()
        {
            InstallSessionPowers(); var (module, session) = await CandidateHost(); var player = module.Controller;
            var old = player.Context; old.Abilities.TryGet("test.air", out var ability); ability.TryCommit(ability.CreateAttempt());
            Assert.Throws<ArgumentException>(() => player.PrepareSnapshot(new object(), _config, EmptySnapshot(), _input.Service));
            var valid = PlatformerSnapshot.Capture(_config.Scene, old);
            var wrongScene = new PlatformerSnapshot("Other", valid.Position, valid.Velocity, valid.FacesRight,
                valid.Powers, valid.EffectsInstalled, Array.Empty<EntityTimedEffectSnapshot>());
            Assert.Throws<InvalidOperationException>(() => player.PrepareSnapshot(new object(), _config, wrongScene, _input.Service));
            Assert.Throws<InvalidOperationException>(() => player.PrepareSnapshot(session, _config, valid, _input.Service));
            Assert.That(player.Context, Is.SameAs(old)); Assert.That(ability.RemainingUses, Is.EqualTo(1));
            using var candidate = player.PrepareSnapshot(new object(), _config, valid, _input.Service);
            Assert.Throws<InvalidOperationException>(() => player.PrepareSnapshot(new object(), _config, valid, _input.Service));
            Assert.That(player.IsRunning, Is.True);
        }

        [UnityTest] public IEnumerator SnapshotHandoffRequiresWholePauseAndRestoresIndependentResources()
            => CandidateHandoff().ToCoroutine();
        private async UniTask CandidateHandoff()
        {
            InstallSessionPowers(); InstallEffects();
            var module = Module(); var player = Character();
            var sprite = player.gameObject.AddComponent<SpriteRenderer>(); sprite.color = new Color(1, 1, 1, .8f);
            var collider = player.GetComponent<Collider2D>(); collider.excludeLayers = 1 << 7;
            var session = await Prepare(module, player); ((IEmberModule)module).OnInit(); module.StartSession(session);
            var old = player.Context; old.Abilities.TryGet("test.air", out var ability); ability.TryCommit(ability.CreateAttempt());
            old.Skills.TryGet("test.skill", out var skill); skill.TryCommit(skill.CreateAttempt());
            var staleAttempt = ability.CreateAttempt();
            var handle = old.Effects.ApplyIndependent(SessionEffect(), "first"); player.Tick(.4f);
            old.Effects.ApplyIndependent(SessionEffect(), "second");
            var snapshot = PlatformerSnapshot.Capture(_config.Scene, old);
            player.Tick(1); ability.RefillUses(); var owner = new object();
            using var candidate = player.PrepareSnapshot(owner, _config, snapshot, _input.Service);
            Assert.Throws<InvalidOperationException>(() => player.CommitSnapshot(candidate, session));
            using (module.Pause())
            {
                Assert.Throws<InvalidOperationException>(() => player.CommitSnapshot(candidate, new object()));
                player.CommitSnapshot(candidate, session);
                Assert.That(player.Context, Is.SameAs(candidate.Context));
                Assert.That(player.FsmStatus, Is.EqualTo(EmberFsmStatus.Paused));
                Assert.That(Time.timeScale, Is.Zero); Assert.That(AudioListener.pause, Is.False);
                Assert.That(player.GetComponent<Rigidbody2D>().simulated, Is.False);
                Assert.That(old.Effects.IsDisposed, Is.True); Assert.That(handle.IsActive, Is.False);
                Assert.That(sprite.color.a, Is.EqualTo(.2f).Within(.0001), "新绑定须读取原始 .8 基线，不能继承旧 .2 结果");
                Assert.That(collider.excludeLayers.value, Is.EqualTo((1 << 7) | (1 << 8)));
                candidate.Context.Abilities.TryGet("test.air", out var restored);
                Assert.That(restored.RemainingUses, Is.EqualTo(1));
                Assert.That(restored.TryCommit(staleAttempt), Is.EqualTo(EntityUseStatus.InvalidAttempt));
                Assert.That(SessionSkill(player).RemainingCooldown, Is.EqualTo(9.6f).Within(.0001));
                Assert.That(candidate.Context.Effects.GetAt(0).RemainingSeconds, Is.EqualTo(9.6f).Within(.0001));
                Assert.That(candidate.Context.Effects.GetAt(1).RemainingSeconds, Is.EqualTo(10));
                candidate.Dispose(); player.StopPrepared(session);
                Assert.That(candidate.IsDisposed, Is.False, "交接后的候选清理及旧所有者不能结束新运行资源");
            }
            Assert.That(player.IsRunning, Is.True); player.Tick(.1f);
            Assert.That(SessionSkill(player).RemainingCooldown, Is.EqualTo(9.5f).Within(.0001));
            player.StopPrepared(owner);
            Assert.That(candidate.IsDisposed, Is.True); Assert.That(sprite.color.a, Is.EqualTo(.8f));
            Assert.That(collider.excludeLayers.value, Is.EqualTo(1 << 7));
        }

        [UnityTest] public IEnumerator CandidateStateFailureRestoresOldHostAndEffectBaseline()
            => CandidateFailure().ToCoroutine();
        private async UniTask CandidateFailure()
        {
            InstallSessionPowers(); InstallEffects();
            var module = Module(); var player = Character();
            var sprite = player.gameObject.AddComponent<SpriteRenderer>(); sprite.color = new Color(1, 1, 1, .8f);
            var session = await Prepare(module, player); ((IEmberModule)module).OnInit(); module.StartSession(session);
            var old = player.Context; var oldFsm = player.FsmInstanceId;
            var effect = old.Effects.ApplyIndependent(SessionEffect(), "old");
            var saved = PlatformerSnapshot.Capture(_config.Scene, old);
            var definition = Keep(ScriptableObject.CreateInstance<CandidateProbeDefinition>());
            var graph = Keep(ScriptableObject.CreateInstance<EmberFsmGraph>());
            graph.Configure("probe", new EmberFsmNode("probe", "probe", definition));
            _config.Configure(graph, _config.Spawn);
            foreach (var phase in new[] { 1, 2, 3 })
            {
                definition.Failure = phase;
                using var pause = module.Pause();
                using var candidate = player.PrepareSnapshot(new object(), _config, saved, _input.Service);
                Assert.That(definition.Created, Is.EqualTo(phase - 1), "准备和图检查不能执行状态工厂或 Guard");
                Assert.Throws<InvalidOperationException>(() => player.CommitSnapshot(candidate, session));
                Assert.That(candidate.IsDisposed, Is.True); Assert.That(candidate.Context.Effects.IsDisposed, Is.True);
                Assert.That(player.Context, Is.SameAs(old)); Assert.That(player.FsmInstanceId, Is.EqualTo(oldFsm));
                Assert.That(player.FsmStatus, Is.EqualTo(EmberFsmStatus.Paused)); Assert.That(Time.timeScale, Is.Zero);
                Assert.That(old.Motor.Position, Is.EqualTo(saved.Position)); Assert.That(old.Motor.Velocity, Is.EqualTo(saved.Velocity));
                Assert.That(old.Facing.FacesRight, Is.EqualTo(saved.FacesRight));
                Assert.That(sprite.color.a, Is.EqualTo(.2f).Within(.0001)); Assert.That(effect.IsActive, Is.True);
            }
            Assert.That(player.IsRunning, Is.True); Assert.That(Time.timeScale, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator DestroyingHostReleasesBothCurrentAndUncommittedCandidate()
            => CandidateDestroyed().ToCoroutine();
        private async UniTask CandidateDestroyed()
        {
            InstallEffects(); var (module, session) = await CandidateHost(); var player = module.Controller;
            var old = player.Context; using var pause = module.Pause();
            var candidate = player.PrepareSnapshot(new object(), _config, PlatformerSnapshot.Capture(_config.Scene, old), _input.Service);
            UnityEngine.Object.DestroyImmediate(player.gameObject);
            Assert.That(old.Effects.IsDisposed, Is.True); Assert.That(candidate.IsDisposed, Is.True);
            Assert.That(candidate.Context.Effects.IsDisposed, Is.True); Assert.That(module.HasSession, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1)); candidate.Dispose();
        }

        [UnityTest] public IEnumerator DisablingControllerDuringCandidateEnterDoesNotResurrectOldHost()
            => CandidateCallbackDisable().ToCoroutine();
        private async UniTask CandidateCallbackDisable()
        {
            InstallEffects(); var (module, session) = await CandidateHost(); var player = module.Controller;
            var old = player.Context;
            var definition = Keep(ScriptableObject.CreateInstance<CandidateProbeDefinition>()); definition.Failure = 4;
            var graph = Keep(ScriptableObject.CreateInstance<EmberFsmGraph>());
            graph.Configure("probe", new EmberFsmNode("probe", "probe", definition)); _config.Configure(graph, _config.Spawn);
            using var pause = module.Pause();
            var candidate = player.PrepareSnapshot(new object(), _config, PlatformerSnapshot.Capture(_config.Scene, old), _input.Service);
            Assert.Throws<InvalidOperationException>(() => player.CommitSnapshot(candidate, session));
            Assert.That(player.Context, Is.Null); Assert.That(module.HasSession, Is.False);
            Assert.That(old.Effects.IsDisposed, Is.True); Assert.That(candidate.Context.Effects.IsDisposed, Is.True);
            Assert.That(candidate.Fsm.Status, Is.EqualTo(EmberFsmStatus.Disposed));
            Assert.That(Time.timeScale, Is.EqualTo(1)); Assert.That(player.GetComponent<Rigidbody2D>().simulated, Is.False);
        }

        [UnityTest] public IEnumerator RestoredGroundBaselinePreservesUsesThenNewLandingRefills()
            => CandidateGround().ToCoroutine();
        private async UniTask CandidateGround()
        {
            InstallSessionPowers();
            _config.Configure(Resources.Load<EmberFsmGraph>("Config/Player/PlayerGraph"), _config.Spawn);
            var (module, session) = await CandidateHost(); var player = module.Controller;
            var floor = Keep(new GameObject("RestoreFloor")); SceneManager.MoveGameObjectToScene(floor, _scene);
            floor.transform.position = new Vector3(0, -.5f); floor.AddComponent<BoxCollider2D>().size = new Vector2(10, 1);
            player.Context.Abilities.TryGet("test.air", out var ability); ability.TryCommit(ability.CreateAttempt());
            var snapshot = new PlatformerSnapshot(_config.Scene, new Vector2(0, .9f), Vector2.zero, true,
                player.Context.Powers.Capture(), false, Array.Empty<EntityTimedEffectSnapshot>());
            Physics2D.SyncTransforms();
            using (module.Pause())
            {
                var candidate = player.PrepareSnapshot(new object(), _config, snapshot, _input.Service);
                player.CommitSnapshot(candidate, session);
                Assert.That(player.Context.JumpWindow.HasGroundSupport, Is.True);
                Assert.That(player.CurrentStateId, Is.EqualTo("fall"), "正式图初始仍从 Air/Fall 进入");
            }
            player.Context.Abilities.TryGet("test.air", out var restored);
            player.FixedTick(.02f); Assert.That(restored.RemainingUses, Is.EqualTo(1));
            Assert.That(player.CurrentStateId, Is.EqualTo("idle"));
            var body = player.GetComponent<Rigidbody2D>(); body.position = new Vector2(0, 4);
            player.transform.position = body.position; Physics2D.SyncTransforms(); player.FixedTick(.02f);
            Assert.That(player.Context.JumpWindow.HasGroundSupport, Is.False);
            body.position = new Vector2(0, .9f); player.transform.position = body.position;
            body.linearVelocity = Vector2.zero; Physics2D.SyncTransforms(); player.FixedTick(.02f);
            Assert.That(restored.RemainingUses, Is.EqualTo(2));
        }

        [UnityTest] public IEnumerator SnapshotEntryGuardUsesRestoredPositionInsteadOfOldGroundCache()
            => CandidateGroundToAir().ToCoroutine();
        private async UniTask CandidateGroundToAir()
        {
            var graph = Keep(UnityEngine.Object.Instantiate(Resources.Load<EmberFsmGraph>("Config/Player/PlayerGraph")));
            graph.Configure("ground", graph.Nodes.ToArray());
            _config.Configure(graph, _config.Spawn);
            var (module, session) = await CandidateHost(); var player = module.Controller;
            var floor = Keep(new GameObject("OldFloor")); SceneManager.MoveGameObjectToScene(floor, _scene);
            floor.transform.position = new Vector3(0, -.5f, 0); floor.AddComponent<BoxCollider2D>().size = new Vector2(10, 1);
            var body = player.GetComponent<Rigidbody2D>(); body.position = new Vector2(0, .9f);
            player.transform.position = body.position; body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms(); player.FixedTick(.02f);
            Assert.That(player.Context.Sensors.IsGrounded, Is.True);
            var snapshot = new PlatformerSnapshot(_config.Scene, new Vector2(0, 5), Vector2.zero, true,
                player.Context.Powers.Capture(), false, Array.Empty<EntityTimedEffectSnapshot>());
            using (module.Pause())
            {
                var candidate = player.PrepareSnapshot(new object(), _config, snapshot, _input.Service);
                player.CommitSnapshot(candidate, session);
                Assert.That(player.Context.Sensors.IsGrounded, Is.False);
                Assert.That(player.CurrentStateId, Is.EqualTo("fall"));
            }
        }

        [UnityTest] public IEnumerator SnapshotCanStartFreshControllerWithFormalGraphAndNoStaleInput()
            => CandidateFresh().ToCoroutine();
        private async UniTask CandidateFresh()
        {
            await _data.WhenReady;
            _config.Configure(Resources.Load<EmberFsmGraph>("Config/Player/PlayerGraph"), _config.Spawn);
#if UNITY_EDITOR
            var player = FormalCharacter(); // 从实际 Prefab 实例验证相同控制器、组件和 1×1.75 尺寸。
            _config.SetCapturedAssembly(_config, _config.Graph, _config.Powers, _config.Feel,
                Keep(UnityEngine.Object.Instantiate(Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH).Animation)));
#else
            var player = Character();
#endif
            var snapshot = EmptySnapshot(); var owner = new object();
            var candidate = player.PrepareSnapshot(owner, _config, snapshot, _input.Service);
            Assert.That(player.Context, Is.Null); Assert.That(player.IsPrepared, Is.False);
            Assert.That(player.GetComponent<Rigidbody2D>().simulated, Is.False);
            player.CommitSnapshot(candidate);
            Assert.That(player.IsRunning, Is.True); Assert.That(player.Context.Motor.Position, Is.EqualTo(snapshot.Position));
            Assert.That(player.Context.Motor.Velocity, Is.EqualTo(snapshot.Velocity));
            Assert.That(player.CurrentStateId, Is.EqualTo("jump"));
            Assert.That(player.Context.Input.JumpRequested, Is.False); Assert.That(player.Context.Effects, Is.Null);
            Assert.That(_input.Service.GetChannel("gameplay").IsAvailable, Is.False, "Controller 不拥有输入开放权");
            candidate.Dispose(); Assert.That(player.IsRunning, Is.True); player.StopPrepared(owner);
            Assert.That(candidate.IsDisposed, Is.True);
        }
    }

    public sealed class CandidateProbeDefinition : EmberFsmStateDefinition<PlayerContext, CandidateProbeState>
    {
        [NonSerialized] public int Failure, Created;
        public override CandidateProbeState CreateState()
        {
            Created++;
            if (Failure == 1) throw new InvalidOperationException("candidate factory failure");
            return new CandidateProbeState(Failure);
        }
        public override IReadOnlyList<EmberFsmTransition> GetTransitions() => Array.Empty<EmberFsmTransition>();
    }

    public sealed class CandidateProbeState : EmberFsmState<PlayerContext>
    {
        private readonly int _failure;
        public CandidateProbeState(int failure) => _failure = failure;
        public override void Enter(EmberFsmActivation activation)
        {
            base.Enter(activation);
            Context.Motor.Apply(new Vector2(90, 90), new Vector2(10, 10), 1);
            Context.Facing.SetFacing(false);
            if (_failure == 2) throw new InvalidOperationException("candidate enter failure");
            if (_failure == 4) Context.Entity.GetComponent<PlayerController>().enabled = false;
        }
        public override void Pause()
        { if (_failure == 3) throw new InvalidOperationException("candidate pause failure"); }
    }
}
