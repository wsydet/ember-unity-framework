using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.SceneFlow.Tests
{
    public sealed class ScopeProbe : GameSceneParticipant, IGameSceneReplaceableParticipant
    {
        public Func<GameScenePreparationContext, UniTask<IGameScenePreparedParticipant>> Prepare;
        public int Calls;
        public Func<GameScenePreparationContext, IGameScenePreparedParticipant, UniTask<IGameScenePreparedParticipant>> Replace;
        public GameSceneParticipant[] Children = Array.Empty<GameSceneParticipant>();
        public override IReadOnlyList<GameSceneParticipant> OwnedParticipants => Children;
        public override UniTask<IGameScenePreparedParticipant> PrepareAsync(GameScenePreparationContext context)
        { Calls++; return Prepare(context); }
        public UniTask<IGameScenePreparedParticipant> PrepareReplacementAsync(
            GameScenePreparationContext context, IGameScenePreparedParticipant current) => Replace(context, current);
    }

    public sealed partial class GameSceneScopeTests
    {
        private Scene _scene;
        private GameSceneScope _scope;
        private readonly List<string> _trace = new();
        private readonly List<GameObject> _objects = new();

        private sealed class Result : IGameScenePreparedParticipant
        {
            private readonly List<string> _trace;
            private readonly string _name;
            public Action OnStart;
            public bool FailDispose;
            public Result(List<string> trace, string name) { _trace = trace; _name = name; }
            public void Start() { _trace.Add("start:" + _name); OnStart?.Invoke(); }
            public void Dispose()
            { _trace.Add("dispose:" + _name); if (FailDispose) throw new InvalidOperationException("cleanup"); }
        }

        [SetUp]
        public void Setup()
        {
            _trace.Clear();
            _scene = SceneManager.GetActiveScene();
            _scope = ObjectInScene("Scope").AddComponent<GameSceneScope>();
        }

        [TearDown]
        public void Teardown()
        {
            foreach (var go in _objects) if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
        }

        private GameObject ObjectInScene(string name)
        {
            var go = new GameObject(name);
            _objects.Add(go);
            SceneManager.MoveGameObjectToScene(go, _scene);
            return go;
        }

        private ScopeProbe Probe(string name, Result result = null)
        {
            var probe = ObjectInScene(name).AddComponent<ScopeProbe>();
            result ??= new Result(_trace, name);
            probe.Prepare = _ => UniTask.FromResult<IGameScenePreparedParticipant>(result);
            return probe;
        }

        private void Configure(GameSceneScope scope, params GameSceneParticipant[] participants)
            => typeof(GameSceneScope).GetField("_participants", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(scope, participants);

        [Test]
        public async Task StopWaitsForLatePreparationAndDoesNotStartIt()
        {
            var gate = new UniTaskCompletionSource<IGameScenePreparedParticipant>();
            var probe = Probe("late"); probe.Prepare = _ => gate.Task;
            Configure(_scope, probe);
            var pending = _scope.PrepareAsync(1, null, CancellationToken.None).AsTask();
            var stop = _scope.StopAsync();
            Assert.That(_scope.StopAsync(), Is.SameAs(stop));
            Assert.That(stop.IsCompleted, Is.False);
            gate.TrySetResult(new Result(_trace, "late"));
            try { await pending; Assert.Fail("Disposed candidate returned"); } catch (OperationCanceledException) { }
            await stop;
            CollectionAssert.AreEqual(new[] { "dispose:late" }, _trace);
        }

        [Test]
        public async Task FailedLateCleanupFaultsStopCompletion()
        {
            var gate = new UniTaskCompletionSource<IGameScenePreparedParticipant>();
            var probe = Probe("late"); probe.Prepare = _ => gate.Task;
            Configure(_scope, probe);
            var pending = _scope.PrepareAsync(1, null, CancellationToken.None).AsTask();
            var stop = _scope.StopAsync();
            gate.TrySetResult(new Result(_trace, "late") { FailDispose = true });
            try { await pending; Assert.Fail("Cleanup failure lost"); } catch (InvalidOperationException) { }
            try { await stop; Assert.Fail("Stop falsely succeeded"); } catch (InvalidOperationException) { }
            Assert.That(stop.IsFaulted, Is.True);
        }

        [Test]
        public async Task PrepareDoesNotStart_ExplicitStartOnce_DisposeInReverse()
        {
            Configure(_scope, Probe("a"), Probe("b"));
            var session = await _scope.PrepareAsync(7, "entry", CancellationToken.None);
            Assert.That(session.IsPrepared, Is.True);
            Assert.That(session.Args, Is.EqualTo("entry"));
            Assert.That(session.RequestId, Is.EqualTo(7));
            Assert.That(_trace, Is.Empty);
            session.Start();
            Assert.Throws<InvalidOperationException>(() => session.Start());
            session.Dispose(); session.Dispose();
            CollectionAssert.AreEqual(new[] { "start:a", "start:b", "dispose:b", "dispose:a" }, _trace);
        }

        [Test]
        public async Task SlowPreparationBlocksReady_AndRejectsOverlap()
        {
            var gate = new UniTaskCompletionSource<IGameScenePreparedParticipant>();
            var probe = Probe("slow"); probe.Prepare = _ => gate.Task;
            Configure(_scope, probe);
            var pending = _scope.PrepareAsync(1, null, CancellationToken.None).AsTask();
            Assert.That(pending.IsCompleted, Is.False);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await _scope.PrepareAsync(2, null, CancellationToken.None));
            gate.TrySetResult(new Result(_trace, "slow"));
            var session = await pending;
            Assert.That(session.IsPrepared, Is.True);
            Assert.That(session.IsRunning, Is.False);
            session.Dispose();
        }

        [Test]
        public void InvalidReferencesFailBeforePreparingAnyone()
        {
            var probe = Probe("a");
            Configure(_scope, probe, probe);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await _scope.PrepareAsync(1, null, CancellationToken.None));
            Configure(_scope, probe, null);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await _scope.PrepareAsync(2, null, CancellationToken.None));
            Assert.That(probe.Calls, Is.Zero);
        }

        [Test]
        public async Task PartialFailureDisposesEarlierResults_AllowsRetry()
        {
            var second = Probe("b"); second.Prepare = _ => throw new InvalidOperationException("prepare");
            Configure(_scope, Probe("a"), second);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await _scope.PrepareAsync(1, null, CancellationToken.None));
            CollectionAssert.AreEqual(new[] { "dispose:a" }, _trace);
            second.Prepare = _ => UniTask.FromResult<IGameScenePreparedParticipant>(new Result(_trace, "b"));
            var retry = await _scope.PrepareAsync(2, null, CancellationToken.None);
            retry.Dispose();
        }

        [Test]
        public async Task CancellationDisposesLateResult_WithoutStarting()
        {
            using var cancellation = new CancellationTokenSource();
            var gate = new UniTaskCompletionSource<IGameScenePreparedParticipant>();
            var probe = Probe("late"); probe.Prepare = _ => gate.Task;
            Configure(_scope, probe);
            var pending = _scope.PrepareAsync(1, null, cancellation.Token).AsTask();
            cancellation.Cancel(); gate.TrySetResult(new Result(_trace, "late"));
            try { await pending; Assert.Fail("Cancellation was ignored"); } catch (OperationCanceledException) { }
            CollectionAssert.AreEqual(new[] { "dispose:late" }, _trace);
        }

        [Test]
        public async Task ScopeDisabledDuringPrepare_HoldsOwnershipUntilLateCleanup()
        {
            var gate = new UniTaskCompletionSource<IGameScenePreparedParticipant>();
            var probe = Probe("late"); probe.Prepare = _ => gate.Task;
            Configure(_scope, probe);
            var pending = _scope.PrepareAsync(1, null, CancellationToken.None).AsTask();
            _scope.enabled = false; _scope.enabled = true;
            Assert.ThrowsAsync<InvalidOperationException>(async () => await _scope.PrepareAsync(2, null, CancellationToken.None));
            gate.TrySetResult(new Result(_trace, "late"));
            try { await pending; Assert.Fail("Disposed candidate returned"); } catch (OperationCanceledException) { }
            CollectionAssert.AreEqual(new[] { "dispose:late" }, _trace);
        }

        [Test]
        public async Task TwoScopesCannotOwnSameParticipant()
        {
            var probe = Probe("shared");
            var other = ObjectInScene("Other").AddComponent<GameSceneScope>();
            Configure(_scope, probe); Configure(other, probe);
            var first = await _scope.PrepareAsync(1, null, CancellationToken.None);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await other.PrepareAsync(2, null, CancellationToken.None));
            first.Start(); first.Dispose();
            var second = await other.PrepareAsync(3, null, CancellationToken.None); second.Dispose();
            Assert.That(probe.Calls, Is.EqualTo(2));
        }

        [Test]
        public async Task StartFailureCleansStartedAndUnstartedResults()
        {
            var bad = new Result(_trace, "b") { OnStart = () => throw new InvalidOperationException("start") };
            Configure(_scope, Probe("a"), Probe("b", bad), Probe("c"));
            var session = await _scope.PrepareAsync(1, null, CancellationToken.None);
            Assert.Throws<InvalidOperationException>(() => session.Start());
            Assert.That(session.IsRunning, Is.False);
            CollectionAssert.AreEqual(new[] { "start:a", "start:b", "dispose:c", "dispose:b", "dispose:a" }, _trace);
        }

        [Test]
        public async Task CleanupFailureDoesNotSkipOtherResultsOrKeepScopeBusy()
        {
            Configure(_scope, Probe("a"), Probe("b", new Result(_trace, "b") { FailDispose = true }));
            var session = await _scope.PrepareAsync(1, null, CancellationToken.None);
            Assert.Throws<AggregateException>(() => session.Dispose()); session.Dispose();
            CollectionAssert.AreEqual(new[] { "dispose:b", "dispose:a" }, _trace);
            Configure(_scope);
            (await _scope.PrepareAsync(2, null, CancellationToken.None)).Dispose();
        }

        [Test]
        public async Task DestroyParticipantStopsOwnedSession()
        {
            var probe = Probe("a"); Configure(_scope, probe);
            var session = await _scope.PrepareAsync(1, null, CancellationToken.None);
            session.Start(); Object.DestroyImmediate(probe.gameObject);
            Assert.That(session.IsDisposed, Is.True);
            Assert.That(session.IsRunning, Is.False);
            CollectionAssert.AreEqual(new[] { "start:a", "dispose:a" }, _trace);
        }

        [Test]
        public void RequireRejectsMissingOrMultipleScopes()
        {
            Assert.That(GameSceneScope.Require(_scene), Is.SameAs(_scope));
            var extra = ObjectInScene("Other").AddComponent<GameSceneScope>();
            Assert.Throws<InvalidOperationException>(() => GameSceneScope.Require(_scene));
            Object.DestroyImmediate(extra.gameObject); Object.DestroyImmediate(_scope.gameObject);
            Assert.Throws<InvalidOperationException>(() => GameSceneScope.Require(_scene));
        }

        [Test]
        public async Task EmptyScopeWorksWithoutPlayerOrInputModule()
        {
            var session = await _scope.PrepareAsync(1, null, CancellationToken.None);
            session.Start(); Assert.That(session.IsRunning, Is.True); session.Dispose();
        }

        [Test]
        public void GroupAliasAndCyclesFailBeforeAnyPrepare()
        {
            var parent = Probe("parent"); var child = Probe("child");
            parent.Children = new GameSceneParticipant[] { child };
            Configure(_scope, parent, child);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await _scope.PrepareAsync(1, null, CancellationToken.None));
            Configure(_scope, parent); child.Children = new GameSceneParticipant[] { parent };
            Assert.ThrowsAsync<InvalidOperationException>(async () => await _scope.PrepareAsync(2, null, CancellationToken.None));
            Assert.That(parent.Calls + child.Calls, Is.Zero);
        }

        [Test]
        public async Task GroupOwnsChildWithoutScopeDrivingItTwice()
        {
            var parent = Probe("parent"); var child = Probe("child");
            parent.Children = new GameSceneParticipant[] { child };
            parent.Prepare = context => child.PrepareAsync(context);
            Configure(_scope, parent);
            var session = await _scope.PrepareAsync(1, null, CancellationToken.None);
            session.Start(); session.Dispose();
            Assert.That(parent.Calls, Is.EqualTo(1)); Assert.That(child.Calls, Is.EqualTo(1));
            CollectionAssert.AreEqual(new[] { "start:child", "dispose:child" }, _trace);
        }
    }
}
