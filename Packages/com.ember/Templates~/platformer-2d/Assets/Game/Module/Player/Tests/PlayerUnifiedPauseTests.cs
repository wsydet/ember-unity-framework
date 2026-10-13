using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.PlayerData;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests
    {
        private EmberStateMachine ExitFlow(PlayerModule module)
        {
            var flow = new EmberStateMachine();
            flow.Register(new ExitProbe(module, _data)); flow.Register(new MainState());
            flow.Start<ExitProbe>();
            return flow;
        }

        [UnityTest] public IEnumerator WholePause_LastOwnerRestoresOriginalScale_AndStaleOwnerIsHarmless()
            => WholePauseOwners().ToCoroutine();
        private async UniTask WholePauseOwners()
        {
            var module = await Running();
            Time.timeScale = .4f;
            var first = module.Pause(); var second = module.Pause();
            first.Dispose(); first.Dispose();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(module.Controller.IsRunning, Is.False);
            Assert.That(module.Controller.GetComponent<Rigidbody2D>().simulated, Is.False);
            second.Dispose(); Assert.That(Time.timeScale, Is.EqualTo(.4f));
            Assert.That(module.Controller.IsRunning, Is.True);
            var stale = module.Pause(); module.StopSession();
            Assert.That(Time.timeScale, Is.EqualTo(.4f));
            var candidate = await Prepare(module, Character()); module.StartSession(candidate);
            using (module.Pause())
            {
                stale.Dispose();
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(module.State, Is.EqualTo(PlayerSessionState.Paused));
            }
            Assert.That(Time.timeScale, Is.EqualTo(.4f));
        }

        [UnityTest] public IEnumerator ExitSave_PausesBeforeAsyncCommit_AndPreservesCapturedVelocity()
            => ExitPauseBeforeCommit().ToCoroutine();
        private async UniTask ExitPauseBeforeCommit()
        {
            var module = await Running(); var flow = ExitFlow(module);
            var controller = module.Controller; var body = controller.GetComponent<Rigidbody2D>();
            body.position = new Vector2(7, 9); body.linearVelocity = new Vector2(3, 4);
            var expected = controller.Capture(_config.Scene);
            var exit = module.ExitToMenuAsync(flow, 0).AsTask();
            Assert.That(exit.IsCompleted, Is.False, "退出必须经过异步提交/Drain");
            Assert.That(Time.timeScale, Is.Zero); Assert.That(AudioListener.pause, Is.False);
            Assert.That(module.State, Is.EqualTo(PlayerSessionState.Paused));
            Assert.That(body.simulated, Is.False);
            Assert.That(_input.Service.GetChannel("gameplay").IsAvailable, Is.False);
            Assert.Throws<InvalidOperationException>(() => module.Save(1));
            Assert.That((await module.ExitToMenuAsync(flow, null)).Status, Is.EqualTo(PlayerExitStatus.Busy));
            ((IEmberUpdate)module).Update(); ((IEmberFixedUpdate)module).FixedUpdate();
            Assert.That(controller.Context.Motor.Velocity, Is.EqualTo(expected.Velocity));
            Assert.That((await exit).Status, Is.EqualTo(PlayerExitStatus.Completed));
            Assert.That(Time.timeScale, Is.EqualTo(1));
            var read = await _data.ReadAsync(0, new PlatformerSnapshotCodec()).Completion;
            Assert.That(read.Succeeded, Is.True);
            Assert.That(read.Progress.Position, Is.EqualTo(expected.Position));
            Assert.That(read.Progress.Velocity, Is.EqualTo(expected.Velocity));
        }

        [UnityTest] public IEnumerator ExitFailures_ReleaseOnlyTheirOwnPause()
            => ExitFailureOwners().ToCoroutine();
        private async UniTask ExitFailureOwners()
        {
            var module = await Running(); var flow = ExitFlow(module);
            using (module.Pause())
            {
                var saveFailed = await module.ExitToMenuAsync(flow, 999);
                Assert.That(saveFailed.Status, Is.EqualTo(PlayerExitStatus.SaveFailed));
                Assert.That(Time.timeScale, Is.Zero);
                // 未确认的失败保存必须让 Drain 报告失败，不能提前卸载场景。
                var drainFailed = await module.ExitToMenuAsync(flow, null);
                Assert.That(drainFailed.Status, Is.EqualTo(PlayerExitStatus.DrainFailed));
                Assert.That(Time.timeScale, Is.Zero);
                using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
                var cancelled = await module.ExitToMenuAsync(flow, null, cancellation.Token);
                Assert.That(cancelled.Status, Is.EqualTo(PlayerExitStatus.Cancelled));
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(module.State, Is.EqualTo(PlayerSessionState.Paused));
                Assert.That(module.Controller.GetComponent<Rigidbody2D>().simulated, Is.False);
                Assert.That(_input.Service.GetChannel("gameplay").IsAvailable, Is.False);
                Assert.That(flow.Is<GameplayState>(), Is.True);
            }
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(module.State, Is.EqualTo(PlayerSessionState.Running));
            Assert.That(_data.State, Is.EqualTo(SaveServiceState.Ready));
            flow.StopTransitions();
        }

        [UnityTest] public IEnumerator ExitWithoutNewSave_PausesDuringPendingDrain_AndCancellationRestoresPlay()
            => PendingDrainCancellation().ToCoroutine();
        private async UniTask PendingDrainCancellation()
        {
            var module = await Running(); var flow = ExitFlow(module);
            var save = module.Save(0);
            Assert.That(save.Completion.IsCompleted, Is.False);
            // 普通安全区保存请求接受后，玩家、世界和输入都继续。
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(module.State, Is.EqualTo(PlayerSessionState.Running));
            Assert.That(module.Controller.GetComponent<Rigidbody2D>().simulated, Is.True);
            Assert.That(_input.Service.GetChannel("gameplay").IsAvailable, Is.True);
            using var cancellation = new CancellationTokenSource();
            var exit = module.ExitToMenuAsync(flow, null, cancellation.Token).AsTask();
            Assert.That(exit.IsCompleted, Is.False);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(_data.State, Is.EqualTo(SaveServiceState.Draining));
            cancellation.Cancel();
            var result = await exit;
            // 已接受的写入可能在取消的同一帧完成；两条路径都不得转场或留下暂停。
            Assert.That(result.Status, Is.EqualTo(PlayerExitStatus.DrainFailed).Or.EqualTo(PlayerExitStatus.Cancelled));
            if (result.Status == PlayerExitStatus.DrainFailed)
                Assert.That(result.Drain.Status, Is.EqualTo(DrainStatus.WaitCancelled));
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(module.State, Is.EqualTo(PlayerSessionState.Running));
            Assert.That(_input.Service.GetChannel("gameplay").IsAvailable, Is.True);
            Assert.That((await save.Completion).Succeeded, Is.True, "取消退出不取消已经接受的独立保存");
            flow.StopTransitions();
        }
    }
}
