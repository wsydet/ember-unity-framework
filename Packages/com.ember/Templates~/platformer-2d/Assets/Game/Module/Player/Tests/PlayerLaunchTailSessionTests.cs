using System;
using System.Collections;
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
        [UnityTest]
        public IEnumerator LaunchTailSession_CheckpointRecoveryReleasesOldGeneration()
            => LaunchTailRecovery().ToCoroutine();

        private async UniTask LaunchTailRecovery()
        {
            var (module, host, flow) = await RecoveryFlow();
            try
            {
                var player = module.Controller;
                var target = player.gameObject.AddComponent<PlayerLaunchTarget2D>();
                var request = new LaunchRequest2D("recovery-tail", Vector2.up * 20, LaunchMode2D.ConstantRise,
                    .001f, tail: new LaunchTailSpec2D(120, 120, 20, true, 5, 60, true));
                Assert.That(target.TryLaunch(target, request), Is.EqualTo(LaunchResult2D.Accepted));
                ((IEmberFixedUpdate)module).FixedUpdate();
                var tail = player.Context.VerticalMotion.LaunchTail;
                Assert.That(tail.IsActive, Is.True);
                using (module.Pause())
                {
                    Assert.Throws<InvalidOperationException>(() => module.Save(0));
                    for (int i = 0; i < 5; i++) ((IEmberFixedUpdate)module).FixedUpdate();
                    Assert.That(tail.IsActive, Is.True);
                }
                Assert.That((await module.Recover(flow).WhenInteractive).Status, Is.EqualTo(EmberTransitionStatus.Completed));
                Assert.That(tail.IsActive, Is.False);
                var currentTarget = module.Controller.GetComponent<PlayerLaunchTarget2D>() ??
                    module.Controller.gameObject.AddComponent<PlayerLaunchTarget2D>();
                Assert.That(currentTarget.TryLaunch(currentTarget, request), Is.EqualTo(LaunchResult2D.Accepted));
                ((IEmberFixedUpdate)module).FixedUpdate();
                var current = module.Controller.Context.VerticalMotion.LaunchTail;
                tail.Dispose();
                Assert.That(current.IsActive, Is.True);
                module.StopSession();
                Assert.That(current.IsActive, Is.False);
            }
            finally { host.Session.Dispose(); }
        }
    }
}
