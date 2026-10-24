using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Ember.Core.Fsm.Tests
{
    public sealed class EmberLifetimeTests
    {
        [Test]
        public void StopRevokesReadinessBeforeCancellationAndAbortsInReverseOrderOnce()
        {
            var lifetime = new EmberLifetime();
            var calls = new List<int>();
            lifetime.MarkReady();
            lifetime.Cancellation.Register(() => Assert.That(lifetime.IsReady, Is.False));
            lifetime.RegisterAbort(() => calls.Add(1));
            lifetime.RegisterAbort(() => calls.Add(2));
            Assert.That(lifetime.BeginStop(EmberStopReason.ScriptReload), Is.True);
            Assert.That(lifetime.AbortOwners(), Is.True);
            lifetime.FinishStop(true);
            Assert.That(calls, Is.EqualTo(new[] { 2, 1 }));
            Assert.That(lifetime.BeginStop(EmberStopReason.Quit), Is.False);
            Assert.That(lifetime.State, Is.EqualTo(EmberLifetimeState.Stopped));
            Assert.That(lifetime.StopReason, Is.EqualTo(EmberStopReason.ScriptReload));
        }

        [Test]
        public void UnregisteredOwnerIsNotAbortedAndStoppedSessionRejectsNewOwners()
        {
            var lifetime = new EmberLifetime();
            bool aborted = false;
            lifetime.RegisterAbort(() => aborted = true).Dispose();
            lifetime.BeginStop(EmberStopReason.Quit);
            lifetime.AbortOwners();
            lifetime.FinishStop(true);
            Assert.That(aborted, Is.False);
            Assert.Throws<InvalidOperationException>(() => lifetime.RegisterAbort(() => { }));
        }

        [Test]
        public void BackgroundOwnerEndsBeforeExclusiveFileIsReleasedWithoutChangingSavedBytes()
        {
            string path = Path.Combine(Path.GetTempPath(), "EmberLifetime-" + Guid.NewGuid() + ".test");
            File.WriteAllText(path, "committed");
            try
            {
                var lifetime = new EmberLifetime();
                using var owner = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                var worker = Task.Run(() => lifetime.Cancellation.WaitHandle.WaitOne());
                bool released = false;
                lifetime.RegisterBackgroundOwner(worker, () =>
                {
                    Assert.That(worker.IsCompleted, Is.True);
                    owner.Dispose();
                    released = true;
                });
                lifetime.BeginStop(EmberStopReason.ScriptReload);
                Assert.That(lifetime.AbortOwners(), Is.True);
                lifetime.FinishStop(true);
                Assert.That(released, Is.True);
                Assert.That(File.ReadAllText(path), Is.EqualTo("committed"));
            }
            finally { File.Delete(path); }
        }

        [Test]
        public void InitializationCanStopBeforeReady()
        {
            var lifetime = new EmberLifetime();
            lifetime.BeginStop(EmberStopReason.InitializationFailed);
            lifetime.MarkReady();
            Assert.That(lifetime.IsReady, Is.False);
            lifetime.AbortOwners();
            lifetime.FinishStop(false);
            Assert.That(lifetime.State, Is.EqualTo(EmberLifetimeState.Faulted));
        }
    }
}
