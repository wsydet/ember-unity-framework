using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.Entity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests
    {
        private void InstallSessionPowers() => PlayerPowerTestAssets.Install(_config,item=>_objects.Add(item));
        private EntityPowerRuntime SessionSkill(PlayerController player)
        { Assert.That(player.Context.Skills.TryGet("test.skill",out var item),Is.True);return item; }

        [UnityTest] public IEnumerator PowersPreparedNotRunning_ResetHandoffAndWholePauseUseOneDriver()
            => PowerDrive().ToCoroutine();
        private async UniTask PowerDrive()
        {
            InstallSessionPowers();var module=Module();var controller=Character();var candidate=await Prepare(module,controller);
            var skill=SessionSkill(controller);skill.TryCommit(skill.CreateAttempt());
            var progress=candidate.PowerProgress;controller.Tick(2);controller.FixedTick(2);
            Assert.That(skill.RemainingCooldown,Is.EqualTo(10));
            ((IEmberModule)module).ResetModuleData();((IEmberModule)module).OnInit();module.StartSession(candidate);
            Assert.That(module.PowerProgress,Is.SameAs(progress));
            await UniTask.Yield(); // 仅让 Unity 时间前进，模块没有自动注册的第二驱动。
            Assert.That(skill.RemainingCooldown,Is.EqualTo(10));
            float dt=Time.deltaTime;((IEmberUpdate)module).Update();
            Assert.That(skill.RemainingCooldown,Is.EqualTo(10-dt).Within(.00001));
            float remaining=skill.RemainingCooldown;
            for(int i=0;i<5;i++)((IEmberFixedUpdate)module).FixedUpdate();
            Assert.That(skill.RemainingCooldown,Is.EqualTo(remaining));
            using(module.Pause())
            {
                await UniTask.Yield();((IEmberUpdate)module).Update();((IEmberFixedUpdate)module).FixedUpdate();
                controller.Tick(2);controller.FixedTick(2);
                Assert.That(skill.RemainingCooldown,Is.EqualTo(remaining));
                Assert.That(Time.timeScale,Is.Zero);
            }
            await UniTask.Yield();dt=Time.deltaTime;((IEmberUpdate)module).Update();
            Assert.That(skill.RemainingCooldown,Is.EqualTo(remaining-dt).Within(.00001));
            module.StopSession();Assert.That(skill.Query().HasFlag(EntityUseStatus.Disposed),Is.True);
            Assert.That(progress.IsSkillUnlocked("test.skill"),Is.True);
        }
        [UnityTest] public IEnumerator CancelledCandidateAndDisabledCharacterReleaseResources()
            => PowerCleanup().ToCoroutine();
        private async UniTask PowerCleanup()
        {
            InstallSessionPowers();var module=Module();var controller=Character();
            using var cancellation=new CancellationTokenSource();
            var candidate=await Prepare(module,controller,cancellation:cancellation.Token);
            var old=SessionSkill(controller);var attempt=old.CreateAttempt();cancellation.Cancel();
            ((IEmberModule)module).OnInit();
            Assert.Throws<OperationCanceledException>(()=>module.StartSession(candidate));
            Assert.That(controller.Context,Is.Null);Assert.That(old.Query().HasFlag(EntityUseStatus.Disposed),Is.True);
            candidate=await Prepare(module,controller);module.StartSession(candidate);
            var current=SessionSkill(controller);
            Assert.That(current.TryCommit(attempt),Is.EqualTo(EntityUseStatus.InvalidAttempt));
            controller.gameObject.SetActive(false);
            Assert.That(module.HasSession,Is.False);Assert.That(current.Query().HasFlag(EntityUseStatus.Disposed),Is.True);
        }
        [UnityTest] public IEnumerator NonemptyPowersSaveAndLoadV2()
            => PowerSaveBoundary().ToCoroutine();
        private async UniTask PowerSaveBoundary()
        {
            InstallSessionPowers();var module=await Running();var controller=module.Controller;
            var context=controller.Context;var skill=SessionSkill(controller);skill.TryCommit(skill.CreateAttempt());
            Assert.That((await module.Save(0).Completion).Succeeded, Is.True);
            Assert.That(Time.timeScale,Is.EqualTo(1));Assert.That(controller.Context,Is.SameAs(context));
            Assert.That(skill.RemainingCooldown,Is.EqualTo(10));Assert.That(_data.LastSuccessfulSaveSlot,Is.EqualTo(0));
            module.StopSession();
            var loaded = await Prepare(module,controller,PlayerEntryRequest.FromSlot(0));
            module.StartSession(loaded);
            Assert.That(SessionSkill(controller).RemainingCooldown, Is.EqualTo(10));
            Assert.That((await module.ExitToMenuAsync(ExitFlow(module),0)).Status, Is.EqualTo(PlayerExitStatus.Completed));
        }
        [UnityTest] public IEnumerator NonemptyPowersAllowExitWithoutNewSave() => PowerExitWithoutSave().ToCoroutine();
        private async UniTask PowerExitWithoutSave()
        {
            InstallSessionPowers();var module=await Running();var skill=SessionSkill(module.Controller);
            var flow=ExitFlow(module);
            Assert.That((await module.ExitToMenuAsync(flow,null)).Status,Is.EqualTo(PlayerExitStatus.Completed));
            Assert.That(module.HasSession,Is.False);Assert.That(Time.timeScale,Is.EqualTo(1));
            Assert.That(skill.Query().HasFlag(EntityUseStatus.Disposed),Is.True);
        }
    }
}
