using System;
using System.Collections;
using System.Reflection;
using System.Text;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.Entity;
using Game.PlayerData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed class CheckpointWorldProbe : WorldResetParticipant2D
    {
        public int Value;
        public bool Fail;
        public override string CaptureState() => Value.ToString();
        public override void RestoreState(string state, WorldResetReason2D reason)
        { if (Fail) throw new InvalidOperationException("probe reset failure"); Value = int.Parse(state); }
    }

    public sealed partial class PlayerFoundationTests
    {
        private sealed class ProjectRecord { public string Value; }
        private sealed class ProjectCodec : IProgressCodec<ProjectRecord>
        {
            public string DataTypeId => "test.project";
            public EncodedProgress Encode(ProjectRecord value) => new(Encoding.UTF8.GetBytes(value.Value), 1);
            public DecodeResult<ProjectRecord> Decode(ReadOnlyMemory<byte> bytes, int version)
                => new(DecodeStatus.Decoded, new ProjectRecord { Value = Encoding.UTF8.GetString(bytes.Span) });
        }

        [UnityTest] public IEnumerator Checkpoint_CombinedSaveFreezesLatestDataAndDrainsOldRun() => CombinedSave().ToCoroutine();
        private async UniTask CombinedSave()
        {
            var module = await Running();
            var baseline = new PlayerCheckpointRecord("entry", module.RecoveryTarget, Array.Empty<WorldStateRecord2D>());
            var owner = module.Controller.gameObject.AddComponent<PlayerConstantVelocitySource>();
            module.Controller.Context.ExternalMotion.Register(owner,
                new PlayerExternalMotionRequest("air action", PlayerExternalMotionMode.ForcedRise, Vector2.up * 8, 1));
            Assert.Throws<InvalidOperationException>(() => module.Save(0));
            var codec = new CheckpointProgressCodec<ProjectRecord>("ember.platformer2d.progress", new ProjectCodec());
            var session = new CheckpointSaveSession<ProjectRecord>(_data, codec, "run-a");
            var project = new ProjectRecord { Value = "collected-and-confirmed" };
            var saved = session.Save("run-a", 0, "level-a", baseline, project, new SaveSummary());
            project.Value = "later-uncommitted";
            await session.BeginRunAsync("run-b");
            Assert.That((await saved.Completion).Succeeded, Is.True);
            var read = await _data.ReadAsync(0, codec).Completion;
            Assert.That(read.Succeeded, Is.True);
            Assert.That(read.Progress.Project.Value, Is.EqualTo("collected-and-confirmed"));
            Assert.That(read.Progress.Checkpoint.Player.Position, Is.EqualTo(baseline.Player.Position));
            Assert.That(read.Progress.RunId, Is.EqualTo("run-a"));
            Assert.Throws<InvalidOperationException>(() => session.Save("run-a", 0, "level-a", baseline, project, new SaveSummary()));
            Assert.That((await session.Save("run-b", 0, "level-a", baseline, project, new SaveSummary()).Completion).Succeeded, Is.True);
            Assert.That((await _data.ReadAsync(0, codec).Completion).Progress.RunId, Is.EqualTo("run-b"));
        }

        [UnityTest] public IEnumerator Checkpoint_SaveFailureKeepsSceneAndSuccessExitsAfterCommit() => CombinedExit().ToCoroutine();
        private async UniTask CombinedExit()
        {
            var module = await Running();
            var flow = new EmberStateMachine(); var gameplay = new ExitProbe(module, _data);
            flow.Register(gameplay); flow.Register(new MainState()); flow.Start<ExitProbe>();
            var codec = new CheckpointProgressCodec<ProjectRecord>("ember.platformer2d.progress", new ProjectCodec());
            var session = new CheckpointSaveSession<ProjectRecord>(_data, codec, "run");
            var baseline = new PlayerCheckpointRecord("entry", module.RecoveryTarget, Array.Empty<WorldStateRecord2D>());
            var failed = await session.SaveAndExitAsync("run", 999, "level", baseline, new ProjectRecord { Value = "latest" }, new SaveSummary(), module, flow);
            Assert.That(failed.Status, Is.EqualTo(PlayerExitStatus.SaveFailed));
            Assert.That(flow.Is<GameplayState>() && module.Controller.IsRunning, Is.True);
            _data.AcknowledgeResults(new[] { failed.Save.Context.RequestId });
            var done = await session.SaveAndExitAsync("run", 0, "level", baseline, new ProjectRecord { Value = "latest" }, new SaveSummary(), module, flow);
            Assert.That(done.Status, Is.EqualTo(PlayerExitStatus.Completed));
            Assert.That(gameplay.SavedBeforeExit, Is.True);
        }

        [UnityTest] public IEnumerator Checkpoint_WorldFailureHoldsInputAndRetryCommitsOnce() => WorldRecovery().ToCoroutine();
        [UnityTest] public IEnumerator Checkpoint_DisablingFailedRecoveryStopsPartialWorldSession() => WorldRecovery(true).ToCoroutine();
        private async UniTask WorldRecovery(bool disableAfterFailure = false)
        {
            var (module, host, flow) = await RecoveryFlow();
            try
            {
                var go = Keep(new GameObject("World reset")); go.SetActive(false);
                SceneManager.MoveGameObjectToScene(go, _scene);
                var probe = go.AddComponent<CheckpointWorldProbe>();
                typeof(WorldResetParticipant2D).GetField("_stableId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(probe, "mechanism");
                var world = go.AddComponent<WorldResetScope2D>();
                typeof(WorldResetScope2D).GetField("_participants", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(world, new WorldResetParticipant2D[] { probe });
                go.SetActive(true);
                var recovery = module.Controller.gameObject.AddComponent<PlayerCheckpointRecovery>();
                typeof(PlayerCheckpointRecovery).GetField("_world", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(recovery, world);
                recovery.SetBaseline(new PlayerCheckpointRecord("entry", module.RecoveryTarget, world.Capture()));
                probe.Value = 10; probe.Fail = true;
                Assert.That(await recovery.RecoverAsync(module, flow, WorldResetReason2D.Death), Is.False);
                Assert.That(recovery.IsBlocked, Is.True);
                Assert.That(module.State, Is.EqualTo(PlayerSessionState.Paused));
                Assert.That(recovery.LastError, Does.Contain("probe reset failure"));
                if (disableAfterFailure)
                {
                    recovery.enabled = false;
                    Assert.That(module.Controller, Is.Null);
                    Assert.That(recovery.IsBlocked, Is.False);
                    return;
                }
                int commits = 0; module.SessionCommitted += _ => { commits++; Assert.That(probe.Value, Is.Zero); };
                probe.Fail = false;
                Assert.That(await recovery.RecoverAsync(module, flow, WorldResetReason2D.Death), Is.True);
                Assert.That(commits, Is.EqualTo(1)); Assert.That(recovery.IsBlocked, Is.False);
                Assert.That(module.State, Is.EqualTo(PlayerSessionState.Running));
            }
            finally { host.Session.Dispose(); }
        }

        [UnityTest] public IEnumerator LevelEntry_FrozenCrossSceneCandidateSurvivesOldModuleExit() => CrossLevel().ToCoroutine();
        private async UniTask CrossLevel()
        {
            var module = await Running(); var old = module.Controller;
            var scene = SceneManager.CreateScene("LevelTwo");
            try
            {
                var config = Keep(UnityEngine.Object.Instantiate(_config));
                typeof(PlayerConfig).GetField("_scene", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(config, "LevelTwo");
                var controller = Character(); SceneManager.MoveGameObjectToScene(controller.gameObject, scene);
                using var request = PlayerEntryRequest.ForLevel("two", "LevelTwo", config);
                config.Configure(config.Graph, new Vector2(80, 80));
                var candidate = await Prepare(module, controller, request);
                Assert.That(module.Controller, Is.SameAs(old)); Assert.That(old.IsRunning, Is.True);
                Assert.That(controller.IsRunning, Is.False);
                ((IEmberModule)module).OnDestroy(); ((IEmberModule)module).OnInit(); module.StartSession(candidate);
                Assert.That(module.Controller, Is.SameAs(controller));
                Assert.That(controller.Context.Motor.Position, Is.EqualTo(_config.Spawn));
                module.StopSession();
            }
            finally { await SceneManager.UnloadSceneAsync(scene); }
        }
    }
}
