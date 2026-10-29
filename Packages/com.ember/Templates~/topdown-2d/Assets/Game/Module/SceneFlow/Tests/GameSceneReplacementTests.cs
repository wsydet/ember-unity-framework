using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Game.SceneFlow.Tests
{
    public sealed partial class GameSceneScopeTests
    {
        [Test] public async Task ReplacementPreservesWorldAndDisposesOldOnlyAfterNewStarts()
        {
            var old = new Result(_trace, "old"); var fresh = new Result(_trace, "new");
            var player = Probe("player", old); var world = Probe("world");
            player.Replace = (context, current) =>
            {
                Assert.That(current, Is.SameAs(old)); Assert.That(context.Args, Is.EqualTo("snapshot"));
                Assert.That(context.RequestId, Is.EqualTo(2)); Assert.That(context.Scene, Is.EqualTo(_scene));
                return UniTask.FromResult<IGameScenePreparedParticipant>(fresh);
            };
            Configure(_scope, player, world);
            var session = await _scope.PrepareAsync(1, null, CancellationToken.None); session.Start(); _trace.Clear();
            var replacement = await session.PrepareReplacementAsync(player, 2, "snapshot", CancellationToken.None);
            Assert.That(replacement.IsPrepared, Is.True); Assert.That(session.IsRunning, Is.True); Assert.That(_trace, Is.Empty);
            replacement.Commit(); replacement.Dispose(); replacement.Dispose();
            Assert.Throws<InvalidOperationException>(() => replacement.Commit());
            CollectionAssert.AreEqual(new[] { "start:new", "dispose:old" }, _trace);
            Assert.That(world.Calls, Is.EqualTo(1)); Assert.That(player.Calls, Is.EqualTo(1));
            session.Dispose();
            CollectionAssert.AreEqual(new[] { "start:new", "dispose:old", "dispose:world", "dispose:new" }, _trace);
        }

        [Test] public async Task ReplacementFailureOrAbandonKeepsOldResultAndAllowsRetry()
        {
            var player = Probe("old"); Configure(_scope, player);
            var session = await _scope.PrepareAsync(1, null, CancellationToken.None); session.Start(); _trace.Clear();
            player.Replace = (context, current) => throw new InvalidOperationException("prepare");
            Assert.ThrowsAsync<InvalidOperationException>(async () => await session.PrepareReplacementAsync(player, 2, null, CancellationToken.None));
            Assert.That(_trace, Is.Empty); Assert.That(session.IsRunning, Is.True);
            player.Replace = (context, current) => UniTask.FromResult<IGameScenePreparedParticipant>(new Result(_trace, "candidate"));
            var retry = await session.PrepareReplacementAsync(player, 3, null, CancellationToken.None);
            retry.Dispose(); retry.Dispose();
            CollectionAssert.AreEqual(new[] { "dispose:candidate" }, _trace);
            session.Dispose(); Assert.That(_trace, Does.Contain("dispose:old"));
        }

        [Test] public async Task ReplacementCancellationRetainsOwnershipUntilLateCandidateIsCleaned()
        {
            var player = Probe("old"); Configure(_scope, player);
            var session = await _scope.PrepareAsync(1, null, CancellationToken.None); session.Start(); _trace.Clear();
            var gate = new UniTaskCompletionSource<IGameScenePreparedParticipant>();
            player.Replace = (context, current) => gate.Task;
            using var cancellation = new CancellationTokenSource();
            var pending = session.PrepareReplacementAsync(player, 2, null, cancellation.Token).AsTask();
            cancellation.Cancel();
            Assert.That(pending.IsCompleted, Is.False);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await session.PrepareReplacementAsync(player, 3, null, CancellationToken.None));
            gate.TrySetResult(new Result(_trace, "late"));
            try { await pending; Assert.Fail("cancel ignored"); } catch (OperationCanceledException) { }
            CollectionAssert.AreEqual(new[] { "dispose:late" }, _trace); Assert.That(session.IsRunning, Is.True);
            session.Dispose();
        }

        [Test] public async Task ScopeDisableDuringReplacementWaitsForLateCleanupBeforeReclaim()
        {
            var player = Probe("old"); Configure(_scope, player);
            var session = await _scope.PrepareAsync(1, null, CancellationToken.None); session.Start(); _trace.Clear();
            var gate = new UniTaskCompletionSource<IGameScenePreparedParticipant>(); player.Replace = (context, current) => gate.Task;
            var pending = session.PrepareReplacementAsync(player, 2, null, CancellationToken.None).AsTask();
            _scope.enabled = false; _scope.enabled = true;
            Assert.That(session.IsDisposed, Is.True); CollectionAssert.AreEqual(new[] { "dispose:old" }, _trace);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await _scope.PrepareAsync(3, null, CancellationToken.None));
            gate.TrySetResult(new Result(_trace, "late"));
            try { await pending; Assert.Fail("late candidate returned"); } catch (OperationCanceledException) { }
            CollectionAssert.AreEqual(new[] { "dispose:old", "dispose:late" }, _trace);
            (await _scope.PrepareAsync(4, null, CancellationToken.None)).Dispose();
        }

        [Test] public async Task ReplacementCannotReuseAnyOwnedResult()
        {
            var old = new Result(_trace, "old"); var world = new Result(_trace, "world");
            var player = Probe("player", old); Configure(_scope, player, Probe("world", world));
            var session = await _scope.PrepareAsync(1, null, CancellationToken.None); session.Start(); _trace.Clear();
            foreach (var invalid in new IGameScenePreparedParticipant[] { null, old, world })
            {
                player.Replace = (context, current) => UniTask.FromResult(invalid);
                Assert.ThrowsAsync<InvalidOperationException>(async () => await session.PrepareReplacementAsync(player, 2, null, CancellationToken.None));
                Assert.That(_trace, Is.Empty);
            }
            session.Dispose(); CollectionAssert.AreEqual(new[] { "dispose:world", "dispose:old" }, _trace);
        }

        [Test] public async Task ReplacementRejectsNestedAndForeignParticipantsAndNonRunningSession()
        {
            var group = Probe("group"); var child = Probe("child"); var foreign = Probe("foreign");
            group.Children = new GameSceneParticipant[] { child }; Configure(_scope, group);
            var session = await _scope.PrepareAsync(1, null, CancellationToken.None);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await session.PrepareReplacementAsync(group, 2, null, CancellationToken.None));
            session.Start();
            foreach (var invalid in new GameSceneParticipant[] { child, foreign, null })
                Assert.ThrowsAsync<InvalidOperationException>(async () => await session.PrepareReplacementAsync(invalid, 2, null, CancellationToken.None));
            session.Dispose();
        }

        [Test] public async Task ReplacementStartFailureRetainsOldAndCleansCandidateOnce()
        {
            var player = Probe("old"); Configure(_scope, player);
            var session = await _scope.PrepareAsync(1, null, CancellationToken.None); session.Start(); _trace.Clear();
            player.Replace = (context, current) => UniTask.FromResult<IGameScenePreparedParticipant>(
                new Result(_trace, "new") { OnStart = () => throw new InvalidOperationException("start") });
            var replacement = await session.PrepareReplacementAsync(player, 2, null, CancellationToken.None);
            Assert.Throws<InvalidOperationException>(() => replacement.Commit()); replacement.Dispose();
            CollectionAssert.AreEqual(new[] { "start:new", "dispose:new" }, _trace);
            Assert.That(session.IsRunning, Is.True); session.Dispose(); Assert.That(_trace, Does.Contain("dispose:old"));
        }

        [Test] public async Task OldCleanupFailureKeepsNewResultOwnedAndDoesNotDisposeOldAgain()
        {
            var player = Probe("old", new Result(_trace, "old") { FailDispose = true }); Configure(_scope, player);
            var session = await _scope.PrepareAsync(1, null, CancellationToken.None); session.Start(); _trace.Clear();
            player.Replace = (context, current) => UniTask.FromResult<IGameScenePreparedParticipant>(new Result(_trace, "new"));
            var replacement = await session.PrepareReplacementAsync(player, 2, null, CancellationToken.None);
            Assert.Throws<InvalidOperationException>(() => replacement.Commit()); Assert.That(replacement.IsCommitted, Is.True);
            replacement.Dispose(); session.Dispose();
            CollectionAssert.AreEqual(new[] { "start:new", "dispose:old", "dispose:new" }, _trace);
        }

        [Test] public async Task ReplacementDisposeFailureStillReleasesRequestSlot()
        {
            var player = Probe("old"); Configure(_scope, player);
            var session = await _scope.PrepareAsync(1, null, CancellationToken.None); session.Start();
            player.Replace = (context, current) => UniTask.FromResult<IGameScenePreparedParticipant>(new Result(_trace, "bad") { FailDispose = true });
            var replacement = await session.PrepareReplacementAsync(player, 2, null, CancellationToken.None);
            Assert.Throws<InvalidOperationException>(() => replacement.Dispose());
            player.Replace = (context, current) => UniTask.FromResult<IGameScenePreparedParticipant>(new Result(_trace, "retry"));
            (await session.PrepareReplacementAsync(player, 3, null, CancellationToken.None)).Dispose(); session.Dispose();
        }

        [Test] public async Task ScopeLossInsideCandidateStartDoesNotInstallOrLeakCandidate()
        {
            var player = Probe("old"); Configure(_scope, player);
            var session = await _scope.PrepareAsync(1, null, CancellationToken.None); session.Start(); _trace.Clear();
            player.Replace = (context, current) => UniTask.FromResult<IGameScenePreparedParticipant>(
                new Result(_trace, "new") { OnStart = () => _scope.enabled = false });
            var replacement = await session.PrepareReplacementAsync(player, 2, null, CancellationToken.None);
            Assert.Throws<OperationCanceledException>(() => replacement.Commit());
            Assert.That(replacement.IsCommitted, Is.False); Assert.That(session.IsDisposed, Is.True);
            CollectionAssert.AreEqual(new[] { "start:new", "dispose:new", "dispose:old" }, _trace);
        }
    }
}
