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
        public async Task AsyncOwnersStartOnceAndCompletionWaitsForEveryOwnerEvenAfterFailure()
        {
            var lifetime = new EmberLifetime();
            var gate = new TaskCompletionSource<bool>();
            int starts = 0;
            lifetime.RegisterStopOwner(() => { starts++; return gate.Task; });
            lifetime.RegisterStopOwner(() => Task.FromException(new InvalidOperationException("owner failed")));
            lifetime.BeginStop(EmberStopReason.ScriptReload);
            lifetime.AbortOwners(false);
            var stop = lifetime.WhenOwnersStopped;
            lifetime.StartStopOwners();
            Assert.That(lifetime.WhenOwnersStopped, Is.SameAs(stop));
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(stop.IsCompleted, Is.False);
            gate.SetResult(true);
            try { await stop; Assert.Fail("Failure lost"); } catch (InvalidOperationException) { }
            Assert.That(stop.IsFaulted, Is.True);
            lifetime.FinishStop(false);
        }

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
