using System;
using System.Collections;
using System.Linq;
using ActionContractConsumer;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Ember.Core.Fsm;
using Game.Entity;
using Game.PlayerData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests
    {
        private ActionProbeSettings ConfigureProjectAction()
        {
            var source = Resources.Load<EmberFsmGraph>("Config/Player/PlayerGraph");
            var root = Keep(ScriptableObject.CreateInstance<ActionProbeRoot>());
            root.BaseDefinition = source.Nodes.First(n => n.Id == source.EntryId).Definition;
            var settings = Keep(ScriptableObject.CreateInstance<ActionProbeSettings>()); settings.Duration = 10;
            var definition = Keep(ScriptableObject.CreateInstance<ActionProbeDefinition>()); definition.Settings = settings;
            var graph = Keep(ScriptableObject.CreateInstance<EmberFsmGraph>());
            graph.Configure(source.EntryId, source.Nodes.Select(n => n.Id == source.EntryId
                ? new EmberFsmNode(n.Id, n.DisplayName, root, n.ParentId, n.DefaultChildId,
                    n.Bindings.Append(new EmberFsmTargetBinding("project-probe", "project-probe")).ToArray()) : n)
                .Append(new EmberFsmNode("project-probe", "项目动作", definition, source.EntryId, null,
                    new EmberFsmTargetBinding("return", "air"))).ToArray());
            _config.Configure(graph, _config.Spawn); return settings;
        }
        private ActionContractProbe BeginProjectAction(PlayerModule module)
        {
            var player = module.Controller;
            var probe = player.GetComponent<ActionContractProbe>() ?? player.gameObject.AddComponent<ActionContractProbe>();
            probe.Requested = true;
            ((IEmberUpdate)module).Update(); ((IEmberFixedUpdate)module).FixedUpdate();
            Assert.That(player.CurrentStateId, Is.EqualTo("project-probe")); return probe;
        }

        [UnityTest] public IEnumerator ActionContract_FormalScopeFrozenRecoveryAndStaleSession()
            => ActionFrozenRecovery().ToCoroutine();
        private async UniTask ActionFrozenRecovery()
        {
            var source = ConfigureProjectAction(); var (module, host, flow) = await RecoveryFlow();
            try
            {
                var probe = BeginProjectAction(module); var old = probe.Lease;
                source.Duration = 77;
                Assert.That(probe.CapturedDuration, Is.EqualTo(10));
                using (module.Pause())
                {
                    for (int i = 0; i < 10; i++) ((IEmberFixedUpdate)module).FixedUpdate();
                    Assert.That(old.IsActive, Is.True);
                }
                Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(old.IsActive, Is.False);
                BeginProjectAction(module); var current = probe.Lease;
                Assert.That(probe.CapturedDuration, Is.EqualTo(10), "恢复沿用正式准备时的冻结副本");
                old.Dispose(); Assert.That(current.IsActive, Is.True);
                var second = await Running(); var secondProbe = BeginProjectAction(second);
                Assert.That(secondProbe.CapturedDuration, Is.EqualTo(77), "新会话捕获新源值");
                Assert.That(current.IsActive, Is.True); second.StopSession();
            }
            finally { host.Session.Dispose(); }
        }

        [UnityTest] public IEnumerator ActionContract_TransientRejectsCaptureButCommittedProgressSavesAndExits()
            => ActionCombinedSave().ToCoroutine();
        private async UniTask ActionCombinedSave()
        {
            ConfigureProjectAction(); var module = await Running();
            var baseline = new PlayerCheckpointRecord("entry", module.RecoveryTarget, Array.Empty<WorldStateRecord2D>());
            var probe = BeginProjectAction(module);
            Assert.Throws<InvalidOperationException>(() => module.Save(0));
            var codec = new CheckpointProgressCodec<ProjectRecord>("ember.platformer2d.progress", new ProjectCodec());
            var saves = new CheckpointSaveSession<ProjectRecord>(_data, codec, "action-run");
            var project = new ProjectRecord { Value = "committed" };
            var write = saves.Save("action-run", 0, "level", baseline, project, new SaveSummary()); project.Value = "later";
            Assert.That((await write.Completion).Succeeded, Is.True);
            Assert.That((await _data.ReadAsync(0, codec).Completion).Progress.Project.Value, Is.EqualTo("committed"));
            var flow = new EmberStateMachine(); flow.Register(new ExitProbe(module, _data)); flow.Register(new MainState()); flow.Start<ExitProbe>();
            var result = await saves.SaveAndExitAsync("action-run", 0, "level", baseline, project, new SaveSummary(), module, flow);
            Assert.That(result.Status, Is.EqualTo(PlayerExitStatus.Completed));
            Assert.That(probe.Lease.IsActive, Is.False); Assert.That(probe.Exits, Is.EqualTo(1));
            Assert.That(probe.LastEnd, Is.EqualTo(PlayerActionEndReason.SessionEnded));
        }

        [UnityTest] public IEnumerator ActionContract_PreparationCancellationAndInvalidConfigKeepCurrentAction()
            => ActionPreparationFailure().ToCoroutine();
        private async UniTask ActionPreparationFailure()
        {
            var settings = ConfigureProjectAction(); var module = await Running(); var probe = BeginProjectAction(module);
            var other = Module();
            var candidate = await Prepare(other, Character());
            var frozen = candidate.Config;
            var captured = (ActionProbeDefinition)frozen.Graph.Nodes.First(n => n.Id == "project-probe").Definition;
            var dependency = captured.Settings;
            Assert.That(dependency, Is.Not.SameAs(settings));
            candidate.Dispose();
            Assert.That(captured == null && dependency == null, Is.True);
            settings.Duration = float.NaN;
            bool rejected = false;
            try { using var invalid = await Prepare(other, Character()); }
            catch (InvalidOperationException) { rejected = true; }
            Assert.That(rejected, Is.True); Assert.That(probe.Lease.IsActive, Is.True);
        }
    }
}
