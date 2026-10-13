using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Game.PlayerData.Tests
{
    public sealed class ProgressStorageServiceTests
    {
        #region 内部参数
        private string _root;
        private ProgressStoreOptions _options;
        private ProgressStorageService _service;
        private BlockingFiles _files;
        private TestCodec _codec;
        private PumpContext _context;
        private SynchronizationContext _previousContext;
        private int _ownerThread;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private void WaitUntil(Func<bool> condition)
        {
            var timer = Stopwatch.StartNew();
            while (!condition())
            {
                if (timer.Elapsed.TotalSeconds > 10) Assert.Fail("Timed out waiting for controlled test operation.");
                Thread.Sleep(1);
            }
        }

        private void Await(Task task)
        {
            WaitUntil(() => { _context.Pump(); return task.IsCompleted; });
            task.GetAwaiter().GetResult();
            _context.Pump();
        }

        private T Await<T>(Task<T> task) { Await((Task)task); return task.GetAwaiter().GetResult(); }

        private SaveOperation<SaveResult> Save(string text, int slot = 0, CancellationToken cancellation = default) =>
            _service.SaveAsync(slot, new Progress { Text = text }, new SaveSummary(text), _codec, cancellation);

        private ReadResult<Progress> Read(int slot = 0) => Await(_service.ReadAsync(slot, _codec).Completion);

        private void Ready() => Assert.That(Await(_service.PrepareAsync()).Status, Is.EqualTo(PrepareStatus.Ready));

        private void BlockNextWrite()
        {
            _files.Release.Reset(); _files.Entered.Reset();
            _files.BlockNextPayload = 1;
        }

        private void WaitForWrite() => WaitUntil(() => _files.Entered.IsSet);

        private sealed class Progress { internal string Text; }

        private sealed class TestCodec : IProgressCodec<Progress>
        {
            internal readonly List<int> EncodeThreads = new();
            internal readonly List<int> DecodeThreads = new();
            internal byte[] Bytes;
            internal Action OnEncode;
            internal Action OnDecode;
            public string DataTypeId => "test.queue";
            public EncodedProgress Encode(Progress snapshot)
            {
                EncodeThreads.Add(Thread.CurrentThread.ManagedThreadId);
                OnEncode?.Invoke();
                Bytes = Encoding.UTF8.GetBytes(snapshot.Text);
                return new EncodedProgress(Bytes, 1);
            }
            public DecodeResult<Progress> Decode(ReadOnlyMemory<byte> bytes, int version)
            {
                DecodeThreads.Add(Thread.CurrentThread.ManagedThreadId);
                OnDecode?.Invoke();
                return new DecodeResult<Progress>(DecodeStatus.Decoded, new Progress { Text = Encoding.UTF8.GetString(bytes.ToArray()) });
            }
        }

        private sealed class PumpContext : SynchronizationContext
        {
            private readonly ConcurrentQueue<Action> _posted = new();
            internal int PendingCount => _posted.Count;
            public override void Post(SendOrPostCallback callback, object state) => _posted.Enqueue(() => callback(state));
            public override SynchronizationContext CreateCopy() => this;
            internal void Pump()
            {
                while (_posted.TryDequeue(out var callback)) callback();
            }
        }

        private sealed class BlockingFiles : ProgressFileIO
        {
            internal readonly ManualResetEventSlim Entered = new(false);
            internal readonly ManualResetEventSlim Release = new(true);
            internal readonly ConcurrentQueue<int> WriteThreads = new();
            internal int BlockNextPayload;
            internal volatile bool FailIndexWrite;
            internal volatile bool UncertainCommit;
            internal volatile bool CommitHappened;
            internal Action BeforeCommit;

            internal override void WriteNewAndFlush(string path, byte[] bytes)
            {
                WriteThreads.Enqueue(Thread.CurrentThread.ManagedThreadId);
                if (path.EndsWith(".progress.tmp", StringComparison.Ordinal) && Interlocked.Exchange(ref BlockNextPayload, 0) == 1)
                {
                    Entered.Set();
                    if (!Release.Wait(TimeSpan.FromSeconds(8))) throw new IOException("Test write barrier timed out.");
                }
                if (FailIndexWrite && path.EndsWith(".index.tmp", StringComparison.Ordinal)) throw new IOException("injected index failure");
                base.WriteNewAndFlush(path, bytes);
            }

            internal override void ReplaceWithBackup(string source, string destination, string backup)
            {
                BeforeCommit?.Invoke();
                base.ReplaceWithBackup(source, destination, backup);
                CommitHappened = true;
                if (UncertainCommit) throw new IOException("injected unknown commit");
            }

            internal override byte[] ReadBounded(string path, int maximumBytes)
            {
                if (UncertainCommit && CommitHappened && path.EndsWith("index.json", StringComparison.Ordinal))
                    throw new IOException("injected verification failure");
                return base.ReadBounded(path, maximumBytes);
            }
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        [SetUp]
        public void SetUp()
        {
            _ownerThread = Thread.CurrentThread.ManagedThreadId;
            _previousContext = SynchronizationContext.Current;
            _context = new PumpContext();
            SynchronizationContext.SetSynchronizationContext(_context);
            _root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "EmberProgressQueueTests", Guid.NewGuid().ToString("N")));
            _options = new ProgressStoreOptions(_root, "test.queue", new[] { 0, 1 });
            _files = new BlockingFiles();
            _codec = new TestCodec();
            _service = new ProgressStorageService(_options, 8, _files);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                _files.Release.Set();
                Await(_service.StopAsync());
                string boundary = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "EmberProgressQueueTests")) + Path.DirectorySeparatorChar;
                Assert.That(_root.StartsWith(boundary, StringComparison.OrdinalIgnoreCase), Is.True);
                if (Directory.Exists(_root)) Directory.Delete(_root, true);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(_previousContext);
                _files.Entered.Dispose(); _files.Release.Dispose();
            }
        }

        [Test]
        public void PreparationIsExplicitAndRejectedRequestsHaveAwaitableCorrelatedResults()
        {
            Assert.That(_service.State, Is.EqualTo(SaveServiceState.Uninitialized));
            Assert.That(Directory.Exists(_root), Is.False);
            var save = Save("not ready");
            var read = _service.ReadAsync(0, _codec);
            Assert.That(save.Completion.IsCompleted, Is.True);
            Assert.That(save.Completion.Result.Status, Is.EqualTo(SaveStatus.NotReady));
            Assert.That(read.Completion.Result.Status, Is.EqualTo(ReadStatus.NotReady));
            Assert.That(save.Context.ServiceEpoch, Is.EqualTo(_service.ServiceEpoch));
            Assert.That(save.Context.RequestId, Is.Not.EqualTo(read.Context.RequestId));
            Assert.That(_codec.EncodeThreads, Is.Empty);
            Ready();
            Assert.That(_service.PrepareAsync(), Is.SameAs(_service.WhenReady));
            Assert.That(Read().Status, Is.EqualTo(ReadStatus.NotFound));
        }

        [Test]
        public void SameAndCrossSlotRequestsExecuteInAcceptanceOrderWithoutLosingIndexEntries()
        {
            Ready(); BlockNextWrite();
            var first = Save("A", 0); WaitForWrite();
            var second = Save("B", 1);
            var readFirst = _service.ReadAsync(0, _codec);
            var third = Save("C", 0);
            var readLast = _service.ReadAsync(0, _codec);
            Assert.That(_service.OutstandingCount, Is.EqualTo(5));
            Assert.That(_service.GetSlots().Revision, Is.Zero);
            _files.Release.Set();
            Assert.That(Await(readFirst.Completion).Progress.Text, Is.EqualTo("A"));
            Assert.That(Await(readLast.Completion).Progress.Text, Is.EqualTo("C"));
            Assert.That(Read(1).Progress.Text, Is.EqualTo("B"));
            Assert.That(first.Completion.Result.Revision, Is.EqualTo(1));
            Assert.That(second.Completion.Result.Revision, Is.EqualTo(2));
            Assert.That(third.Completion.Result.Revision, Is.EqualTo(3));
            Assert.That(_service.GetSlots().LastSuccessfulSaveSlot, Is.EqualTo(0));
            Assert.That(_service.OutstandingCount, Is.Zero);
        }

        [Test]
        public void SnapshotIsFrozenBeforeReturningHandleAndCodecNeverRunsOnIoThread()
        {
            Ready(); BlockNextWrite();
            var source = new Progress { Text = "original" };
            var operation = _service.SaveAsync(0, source, new SaveSummary("original"), _codec);
            source.Text = "changed";
            Array.Clear(_codec.Bytes, 0, _codec.Bytes.Length);
            WaitForWrite(); _files.Release.Set();
            Assert.That(Await(operation.Completion).Succeeded, Is.True);
            Assert.That(Read().Progress.Text, Is.EqualTo("original"));
            Assert.That(_codec.EncodeThreads.Concat(_codec.DecodeThreads), Is.All.EqualTo(_ownerThread));
            Assert.That(_files.WriteThreads, Is.Not.Empty);
            Assert.That(_files.WriteThreads, Has.None.EqualTo(_ownerThread));
            Assert.That(Await(operation.Completion), Is.SameAs(Await(operation.Completion)));
        }

        [Test]
        public void FullQueueRejectsBeforeEncodingAndCapacityIncludesActiveRequest()
        {
            Await(_service.StopAsync());
            _service = new ProgressStorageService(_options, 1, _files);
            Ready(); BlockNextWrite();
            var first = Save("A"); WaitForWrite();
            var rejectedSave = Save("B", 1);
            var rejectedRead = _service.ReadAsync(0, _codec);
            Assert.That(rejectedSave.Completion.Result.Status, Is.EqualTo(SaveStatus.QueueFull));
            Assert.That(rejectedRead.Completion.Result.Status, Is.EqualTo(ReadStatus.QueueFull));
            Assert.That(_codec.EncodeThreads.Count, Is.EqualTo(1));
            Assert.That(_service.OutstandingCount, Is.EqualTo(1));
            _files.Release.Set(); Await(first.Completion);
            Assert.That(Await(Save("retry", 1).Completion).Succeeded, Is.True);
        }

        [Test]
        public void PreCancelledAndQueuedCancelledWritesDoNotChangeSlots()
        {
            Ready();
            var pre = Save("pre", cancellation: new CancellationToken(true));
            Assert.That(pre.Completion.Result.Status, Is.EqualTo(SaveStatus.Cancelled));
            Assert.That(_codec.EncodeThreads, Is.Empty);
            BlockNextWrite();
            var first = Save("A"); WaitForWrite();
            using var cancel = new CancellationTokenSource();
            var second = Save("B", 1, cancel.Token);
            cancel.Cancel(); _files.Release.Set();
            Await(first.Completion);
            Assert.That(Await(second.Completion).Status, Is.EqualTo(SaveStatus.Cancelled));
            Assert.That(_service.GetSlots().Revision, Is.EqualTo(1));
            Assert.That(Read(1).Status, Is.EqualTo(ReadStatus.NotFound));
        }

        [Test]
        public void ActiveWriteCancellationBeforeCommitPreservesOldProgress()
        {
            Ready(); Await(Save("old").Completion); BlockNextWrite();
            using var cancel = new CancellationTokenSource();
            var save = Save("new", cancellation: cancel.Token); WaitForWrite();
            cancel.Cancel(); _files.Release.Set();
            Assert.That(Await(save.Completion).Status, Is.EqualTo(SaveStatus.Cancelled));
            Assert.That(Read().Progress.Text, Is.EqualTo("old"));
        }

        [Test]
        public void CancellationInsideCommitKeepsCommittedTerminalAndDrainRecord()
        {
            Ready(); Await(Save("old").Completion);
            using var cancel = new CancellationTokenSource();
            _files.BeforeCommit = cancel.Cancel;
            var save = Save("new", 1, cancel.Token);
            var ticket = _service.BeginDrain();
            var report = Await(_service.DrainAsync(ticket));
            Assert.That(save.Completion.Result.Status, Is.EqualTo(SaveStatus.Committed));
            Assert.That(report.Status, Is.EqualTo(DrainStatus.Drained));
            Assert.That(report.Operations.Single(item => item.Context.RequestId == save.Context.RequestId).SaveStatus,
                Is.EqualTo(SaveStatus.Committed));
        }

        [Test]
        public void CancelledLateReadSkipsCodecAndDoesNotChangeLatestSave()
        {
            Ready(); Await(Save("A").Completion);
            using var cancel = new CancellationTokenSource();
            var read = _service.ReadAsync(0, _codec, cancel.Token);
            // 不泵主线程回调，等后台读完后才取消，覆盖“迟到字节”而不是仅队首取消。
            WaitUntil(() => _context.PendingCount != 0);
            cancel.Cancel();
            Assert.That(Await(read.Completion).Status, Is.EqualTo(ReadStatus.Cancelled));
            Assert.That(_codec.DecodeThreads, Is.Empty);
            Assert.That(_service.GetSlots().LastSuccessfulSaveSlot, Is.EqualTo(0));
        }

        [Test]
        public void DrainClosesEntranceAndWaitCancellationDoesNotCancelAcceptedSave()
        {
            Ready(); BlockNextWrite();
            var save = Save("A"); WaitForWrite();
            var ticket = _service.BeginDrain();
            Assert.That(_service.BeginDrain(), Is.SameAs(ticket));
            Assert.That(_service.GetSlots().State, Is.EqualTo(SaveServiceState.Draining));
            var rejected = Save("B", 1);
            Assert.That(rejected.Completion.Result.Status, Is.EqualTo(SaveStatus.Draining));
            Assert.That(_service.ReadAsync(0, _codec).Completion.Result.Status, Is.EqualTo(ReadStatus.Draining));
            var cancelledWait = Await(_service.DrainAsync(ticket, new CancellationToken(true)));
            Assert.That(cancelledWait.Status, Is.EqualTo(DrainStatus.WaitCancelled));
            Assert.That(cancelledWait.PendingRequestIds, Does.Contain(save.Context.RequestId));
            Assert.That(save.Completion.IsCompleted, Is.False);
            _files.Release.Set();
            var finished = Await(_service.DrainAsync(ticket));
            Assert.That(finished.Status, Is.EqualTo(DrainStatus.Drained));
            Assert.That(save.Completion.Result.Succeeded, Is.True);
            Assert.That(finished.Operations.Select(item => item.Context.RequestId), Has.No.Member(rejected.Context.RequestId));
        }

        [Test]
        public void DrainRetainsHistoricalFailuresUntilAcknowledgedAndDoesNotMutateOldResults()
        {
            Ready();
            _files.FailIndexWrite = true;
            var failed = Save("fail");
            Assert.That(Await(failed.Completion).Status, Is.EqualTo(SaveStatus.WriteFailed));
            _files.FailIndexWrite = false;
            var ticket = _service.BeginDrain();
            var report = Await(_service.DrainAsync(ticket));
            Assert.That(report.Status, Is.EqualTo(DrainStatus.Failed));
            Assert.That(report.FailedWrites.Single().Context.RequestId, Is.EqualTo(failed.Context.RequestId));
            Assert.That(_service.ResumeRequests(ticket), Is.True);
            var retry = Save("retry"); Await(retry.Completion);
            Assert.That(retry.Context.RequestId, Is.GreaterThan(failed.Context.RequestId));
            var again = _service.BeginDrain();
            Assert.That(Await(_service.DrainAsync(again)).Status, Is.EqualTo(DrainStatus.Failed));
            Assert.That(_service.ResumeRequests(again), Is.True);
            _service.AcknowledgeResults(new[] { failed.Context.RequestId, retry.Context.RequestId });
            var clean = _service.BeginDrain();
            Assert.That(Await(_service.DrainAsync(clean)).Status, Is.EqualTo(DrainStatus.Drained));
            Assert.That(report.Status, Is.EqualTo(DrainStatus.Failed));
            Assert.That(failed.Completion.Result.Status, Is.EqualTo(SaveStatus.WriteFailed));
        }

        [Test]
        public void PendingAcknowledgementCannotEraseFutureFailureAndRejectedSavesAreReported()
        {
            Ready(); BlockNextWrite(); _files.FailIndexWrite = true;
            var failed = Save("fail"); WaitForWrite();
            _service.AcknowledgeResults(new[] { failed.Context.RequestId });
            var rejected = Save("bad slot", 99);
            Assert.That(rejected.Completion.Result.Status, Is.EqualTo(SaveStatus.InvalidSlot));
            var ticket = _service.BeginDrain(); _files.Release.Set();
            var report = Await(_service.DrainAsync(ticket));
            Assert.That(report.FailedWrites.Select(item => item.Context.RequestId),
                Is.EquivalentTo(new[] { failed.Context.RequestId, rejected.Context.RequestId }));
        }

        [Test]
        public void ResumeInvalidatesOldTicketAndItsPendingWaitWithoutCancellingIo()
        {
            Ready(); BlockNextWrite();
            var save = Save("A"); WaitForWrite();
            var first = _service.BeginDrain();
            var waiting = _service.DrainAsync(first);
            Assert.That(_service.ResumeRequests(first), Is.True);
            Assert.That(Await(waiting).Status, Is.EqualTo(DrainStatus.InvalidTicket));
            var second = _service.BeginDrain();
            Assert.That(_service.ResumeRequests(first), Is.False);
            Assert.That(Await(_service.DrainAsync(first)).Status, Is.EqualTo(DrainStatus.InvalidTicket));
            _files.Release.Set();
            Assert.That(Await(_service.DrainAsync(second)).Status, Is.EqualTo(DrainStatus.Drained));
            Assert.That(save.Completion.Result.Succeeded, Is.True);
        }

        [Test]
        public void ReadAfterFailedWriteSeesOldDataAndDrainWaitsForReadCompletion()
        {
            Ready(); Await(Save("old").Completion);
            BlockNextWrite(); _files.FailIndexWrite = true;
            var failed = Save("new"); WaitForWrite();
            var read = _service.ReadAsync(0, _codec);
            var ticket = _service.BeginDrain(); _files.Release.Set();
            var result = Await(_service.DrainAsync(ticket));
            Assert.That(result.Status, Is.EqualTo(DrainStatus.Failed));
            Assert.That(read.Completion.Result.Progress.Text, Is.EqualTo("old"));
            Assert.That(result.Operations.Single(item => item.Context.RequestId == read.Context.RequestId).ReadStatus,
                Is.EqualTo(ReadStatus.Loaded));
            Assert.That(failed.Completion.Result.Succeeded, Is.False);
        }

        [Test]
        public void UncertainCommitFaultsServiceAndQueuedWriteCannotOverwriteUnknownIndex()
        {
            Ready(); Await(Save("old").Completion);
            _files.CommitHappened = false;
            _files.UncertainCommit = true;
            BlockNextWrite();
            var uncertain = Save("committed but unconfirmed", 1); WaitForWrite();
            var queued = Save("must not commit", 0);
            var ticket = _service.BeginDrain(); _files.Release.Set();
            Assert.That(Await(_service.DrainAsync(ticket)).Status, Is.EqualTo(DrainStatus.Failed));
            Assert.That(uncertain.Completion.Result.Status, Is.EqualTo(SaveStatus.CommitUncertain));
            Assert.That(queued.Completion.Result.Status, Is.EqualTo(SaveStatus.NotReady));
            Assert.That(_service.State, Is.EqualTo(SaveServiceState.Faulted));
            Assert.That(_service.ResumeRequests(ticket), Is.False);
            Await(_service.StopAsync());
            _service = new ProgressStorageService(_options);
            Ready();
            Assert.That(Read(0).Progress.Text, Is.EqualTo("old"));
            Assert.That(Read(1).Progress.Text, Is.EqualTo("committed but unconfirmed"));
        }

        [Test]
        public void StopWaitsForIoOwnershipAndCompletesQueuedRequestsWithoutDecoding()
        {
            Ready(); BlockNextWrite();
            var active = Save("active"); WaitForWrite();
            var queued = Save("queued", 1);
            var read = _service.ReadAsync(0, _codec);
            var stopping = _service.StopAsync();
            Assert.That(_service.StopAsync(), Is.SameAs(stopping));
            Assert.That(stopping.IsCompleted, Is.False);
            Assert.That(_service.State, Is.EqualTo(SaveServiceState.Stopped));
            using (var contender = new ProgressFileStore(_options))
                Assert.That(contender.Prepare(new ProgressRequestContext(1, null, 1)).Status, Is.EqualTo(PrepareStatus.IoFailed));
            _files.Release.Set(); Await(stopping);
            Assert.That(active.Completion.Result.Status, Is.EqualTo(SaveStatus.Cancelled));
            Assert.That(queued.Completion.Result.Status, Is.EqualTo(SaveStatus.Cancelled));
            Assert.That(read.Completion.Result.Status, Is.EqualTo(ReadStatus.Cancelled));
            Assert.That(_codec.DecodeThreads, Is.Empty);
            using var next = new ProgressFileStore(_options);
            Assert.That(next.Prepare(new ProgressRequestContext(2, null, 2)).Succeeded, Is.True);
        }

        [Test]
        public void StopAfterCommitBeforeOwnerContinuationPreservesActualSuccess()
        {
            Ready(); Await(Save("old").Completion);
            var operation = Save("new", 1);
            WaitUntil(() => _context.PendingCount != 0);
            var stop = _service.StopAsync(); Await(stop);
            Assert.That(operation.Completion.Result.Status, Is.EqualTo(SaveStatus.Committed));
            Assert.That(_service.GetSlots().LastSuccessfulSaveSlot, Is.EqualTo(1));
        }

        [Test]
        public void NewGenerationRejectsOldDrainTicketAndOldAcknowledgements()
        {
            Ready(); var oldSave = Save("old"); Await(oldSave.Completion);
            var ticket = _service.BeginDrain();
            long epoch = _service.ServiceEpoch;
            Await(_service.StopAsync());
            _service = new ProgressStorageService(_options); Ready();
            var current = Save("new"); Await(current.Completion);
            Assert.That(_service.ServiceEpoch, Is.GreaterThan(epoch));
            Assert.That(_service.ResumeRequests(ticket), Is.False);
            Assert.That(Await(_service.DrainAsync(ticket)).Status, Is.EqualTo(DrainStatus.InvalidTicket));
            _service.AcknowledgeResults(new[] { oldSave.Context.RequestId });
            Assert.That(_service.UnacknowledgedSaveCount, Is.EqualTo(1));
        }

        [Test]
        public void StopBeforePrepareCompletesReadinessWithoutTouchingDisk()
        {
            Await(_service.StopAsync());
            Assert.That(_service.WhenReady.Result.Status, Is.EqualTo(PrepareStatus.Stopped));
            Assert.That(Directory.Exists(_root), Is.False);
            Assert.That(Save("late").Completion.Result.Status, Is.EqualTo(SaveStatus.NotReady));
        }

        [Test]
        public void InvalidOwnerThreadAndCodecReentryCannotAdmitHiddenRequests()
        {
            Ready();
            var foreign = Task.Run(() => Assert.Throws<InvalidOperationException>(() => Save("foreign")));
            Await(foreign);
            _codec.OnEncode = () => _service.BeginDrain();
            var result = Await(Save("reentry").Completion);
            Assert.That(result.Status, Is.EqualTo(SaveStatus.EncodeFailed));
            Assert.That(_service.State, Is.EqualTo(SaveServiceState.Ready));
            Assert.That(_service.OutstandingCount, Is.Zero);
            Assert.That(_service.GetSlots().Revision, Is.Zero);
        }
        #endregion
    }
}
