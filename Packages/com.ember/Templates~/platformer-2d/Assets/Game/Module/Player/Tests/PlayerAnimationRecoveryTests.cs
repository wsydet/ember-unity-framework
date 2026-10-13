#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ember.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Player.Tests
{
    public sealed partial class PlayerFoundationTests
    {
        [UnityTest] public IEnumerator AnimationSaveLoadAndRecoveryUseFrozenMapsAndReleaseOldOverrides()
            => AnimationRecovery().ToCoroutine();
        private async UniTask AnimationRecovery()
        {
            var original = Resources.Load<PlayerConfig>(PlayerModule.CONFIG_PATH);
            var profile = Keep(UnityEngine.Object.Instantiate(original.Animation));
            _config.Configure(original.Graph, _config.Spawn);
            _config.SetCapturedAssembly(_config, _config.Graph, _config.Powers, _config.Feel, profile);
            var module = Module(); var controller = FormalCharacter(); var scope = Scope(module, controller);
            var session = await scope.PrepareAsync(301, PlayerEntryRequest.NewGame(), default);
            ((IEmberModule)module).OnInit(); session.Start();
            var host = new RecoveryFlowHost(module, session, scope.GetComponent<PlayerScopeDependencyProbe>());
            var flow = new EmberStateMachine(); flow.Register(host); flow.Start<RecoveryFlowHost>();
            try
            {
                var animator = controller.GetComponentInChildren<Animator>();
                var first = animator.runtimeAnimatorController;
                var save = module.Save(0);
                Assert.That(Time.timeScale, Is.EqualTo(1));
                animator.Update(.1f);
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.GreaterThan(0));
                Assert.That((await save.Completion).Succeeded, Is.True);
                foreach (var slot in profile.Slots) PlayerDashEffectTestAssets.Set(slot, "_speed", 3f);
                using (module.Pause())
                {
                    float time = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                    animator.Update(.1f);
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(time));
                }
                Assert.That((await module.LoadCheckpoint(flow, 0).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(animator.speed, Is.EqualTo(1), "读档沿用 A 捕获");
                Assert.That(first == null, Is.True, "旧覆盖已释放");
                var second = animator.runtimeAnimatorController;
                Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(animator.speed, Is.EqualTo(1), "恢复沿用 A 捕获");
                Assert.That(second == null, Is.True);
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer." + controller.CurrentStateId), Is.True);
                var last = animator.runtimeAnimatorController;
                module.StopSession();
                Assert.That(last == null, Is.True);
                Assert.That(animator.runtimeAnimatorController == null, Is.True);
            }
            finally { host.Session.Dispose(); }
        }
    }
}
#endif
