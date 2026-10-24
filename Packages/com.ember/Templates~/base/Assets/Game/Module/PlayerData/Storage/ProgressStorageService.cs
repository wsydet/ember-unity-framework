using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Game.PlayerData
{
    /// <summary>
    /// 单个槽位库的异步服务，由模块持有。请求及 codec 在创建线程执行，后台只执行文件后端。
    /// 常规入口需要创建线程的 SynchronizationContext。BeginStopOwnership 在创建线程封闭请求，
    /// 返回的停止凭证由后台独立完成；仅此凭证允许有界同步等待。
    /// </summary>
    public sealed class ProgressStorageService
    {
        #region 内部参数
        private readonly int _ownerThread;
        private readonly ProgressStoreOptions _options;
        private readonly ProgressFileStore _store;
        private readonly int _capacity;
        private readonly Queue<Work> _queue = new();
        private readonly Dictionary<long, Work> _pending = new();
        private readonly Dictionary<long, Task<ProgressOperationResult>> _writes = new();
        private readonly CancellationTokenSource _lifetime = new();
        private readonly TaskCompletionSource<PrepareResult> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task _pump = Task.CompletedTask;
        private Task _activeIo = Task.CompletedTask;
        private Task<PrepareResult> _prepareIo;
        private Work _activeWork;
        private ProgressStorageStop _ownershipStop;
        private Task _stop;
        private bool _pumping;
        private bool _insideCodec;
        private long _lastRequest;
        private SaveServiceState _state = SaveServiceState.Uninitialized;
        private DrainTicket _drain;
        public long ServiceEpoch { get; }
        public Task<PrepareResult> WhenReady => _ready.Task;
        public SaveServiceState State { get { CheckOwner(); return _state; } }
        public int OutstandingCount { get { CheckOwner(); return _ownershipStop?.IsOwnershipReleased == true ? 0 : _pending.Count; } }
        public int UnacknowledgedSaveCount { get { CheckOwner(); return _writes.Count; } }
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private void CheckOwner()
        {
            if (Thread.CurrentThread.ManagedThreadId != _ownerThread)
                throw new InvalidOperationException("存储服务入口必须在创建线程调用。");
        }

        private void CheckControl()
        {
            CheckOwner();
            if (_insideCodec) throw new InvalidOperationException("codec 执行期间不能重入存储服务控制入口。");
            if (SynchronizationContext.Current == null || SynchronizationContext.Current.GetType() == typeof(SynchronizationContext))
                throw new InvalidOperationException("存储服务需要可返回创建线程的 SynchronizationContext。");
        }

        private ProgressRequestContext Request(int? slot = null)
        {
            var context = ProgressIdentity.Request(ServiceEpoch, slot);
            _lastRequest = context.RequestId;
            return context;
        }

        private SaveOperation<SaveResult> Reject(SaveResult result)
        {
            _writes.Add(result.Context.RequestId, Task.FromResult(new ProgressOperationResult(result)));
            return new SaveOperation<SaveResult>(result.Context, Task.FromResult(result));
        }

        private SaveStatus? Rejection(CancellationToken cancellation)
        {
            if (_state == SaveServiceState.Draining) return SaveStatus.Draining;
            if (_state != SaveServiceState.Ready) return SaveStatus.NotReady;
            if (cancellation.IsCancellationRequested) return SaveStatus.Cancelled;
            if (_pending.Count >= _capacity) return SaveStatus.QueueFull;
            return null;
        }

        private async Task PrepareCore(ProgressRequestContext context, CancellationToken cancellation)
        {
            var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _lifetime.Token);
            _prepareIo = Task.Run(() =>
            {
                using (linked)
                {
                    try { return _store.Prepare(context, linked.Token); }
                    catch (Exception error) { return new PrepareResult(context, PrepareStatus.IoFailed, error.ToString()); }
                }
            });
            PrepareResult result;
            try { result = await _prepareIo; }
            catch (Exception error) { result = new PrepareResult(context, PrepareStatus.IoFailed, error.ToString()); }
            if (_state != SaveServiceState.Stopped) _state = result.Succeeded ? SaveServiceState.Ready : SaveServiceState.Faulted;
            else result = new PrepareResult(context, PrepareStatus.Stopped, "服务在准备期间停止。");
            _ready.TrySetResult(result);
        }

        private void Enqueue(Work work)
        {
            _pending.Add(work.Context.RequestId, work);
            _queue.Enqueue(work);
            if (!_pumping) _pump = PumpAsync();
        }

        private async Task PumpAsync()
        {
            _pumping = true;
            try
            {
                while (_queue.Count != 0)
                {
                    var work = _queue.Dequeue();
                    _activeWork = work;
                    try
                    {
                        _activeIo = Task.Run(() => work.ExecuteIO(_store));
                        await _activeIo;
                        if (_state == SaveServiceState.Stopped) return;
                        // 解码始终回到创建线程；停止/取消时丢弃迟到字节，不触发业务 codec。
                        _insideCodec = true;
                        try { work.Complete(); }
                        finally { _insideCodec = false; }
                    }
                    catch (Exception error) { if (_state != SaveServiceState.Stopped) work.Fail(error); }
                    finally
                    {
                        if (_state != SaveServiceState.Stopped)
                        {
                            _pending.Remove(work.Context.RequestId);
                            work.Dispose();
                            _activeWork = null;
                        }
                    }
                    if (_state == SaveServiceState.Stopped) return;
                    if (_state != SaveServiceState.Stopped && _store.Catalog.State == SaveServiceState.Faulted)
                        _state = SaveServiceState.Faulted;
                }
            }
            finally { _pumping = false; }
        }

        private DrainResult DrainSnapshot(DrainTicket ticket, DrainStatus? forced = null)
        {
            var completed = ticket.Operations.Where(task => task.IsCompleted).Select(task => task.Result).ToArray();
            var pending = _pending.Keys.Where(id => id <= ticket.Watermark).ToArray();
            bool failed = completed.Any(item => item.Kind == ProgressOperationKind.Save && item.SaveStatus != SaveStatus.Committed);
            return new DrainResult(ticket.Context, forced ?? (failed ? DrainStatus.Failed : DrainStatus.Drained),
                ticket.Watermark, completed, pending);
        }

        private void StopCore()
        {
            // Publish the immutable stop handle before cancellation can reenter this service.
            _ownershipStop = new ProgressStorageStop();
            _stop = _ownershipStop.WhenOwnershipReleased;
            _state = SaveServiceState.Stopped;
            _drain?.Invalidated.TrySetResult(true);
            _drain = null;
            var pending = _pending.Values.ToArray();
            var active = _activeWork;
            var activeIo = _activeIo;
            var prepareIo = _prepareIo;
            _queue.Clear();
            _lifetime.Cancel();
            if (!_ready.Task.IsCompleted)
                _ready.TrySetResult(new PrepareResult(Request(), PrepareStatus.Stopped));
            _ = Task.Run(async () =>
            {
                Exception ioFailure = null;
                if (prepareIo != null) await prepareIo.ConfigureAwait(false);
                try { await activeIo.ConfigureAwait(false); }
                catch (Exception error) { ioFailure = error; }
                _ownershipStop.IoTerminated();
                try
                {
                    foreach (var work in pending)
                    {
                        try
                        {
                            if (ReferenceEquals(work, active) && ioFailure != null) work.Fail(ioFailure);
                            else
                            {
                                if (!ReferenceEquals(work, active)) work.ExecuteIO(_store);
                                // Reads are cancelled: Complete cannot invoke their codec.
                                work.Complete();
                            }
                        }
                        finally { work.Dispose(); }
                    }
                    _ownershipStop.ResultsSettled(pending.Select(work => work.Report.Task.Result).ToArray());
                    _store.Dispose();
                    _ownershipStop.OwnershipReleased();
                }
                catch (Exception error) { _ownershipStop.Fail(error); }
                finally { _lifetime.Dispose(); }
            });
        }

        private abstract class Work : IDisposable
        {
            internal readonly ProgressRequestContext Context;
            internal readonly CancellationTokenSource Cancellation;
            internal readonly TaskCompletionSource<ProgressOperationResult> Report = new(TaskCreationOptions.RunContinuationsAsynchronously);
            protected Work(ProgressRequestContext context, CancellationToken caller, CancellationToken lifetime)
            { Context = context; Cancellation = CancellationTokenSource.CreateLinkedTokenSource(caller, lifetime); }
            internal abstract void ExecuteIO(ProgressFileStore store);
            internal abstract void Complete();
            internal abstract void Fail(Exception error);
            public void Dispose() => Cancellation.Dispose();
        }

        private sealed class SaveWork : Work
        {
            private readonly FrozenProgress _snapshot;
            private SaveResult _result;
            internal readonly TaskCompletionSource<SaveResult> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            internal SaveWork(ProgressRequestContext context, FrozenProgress snapshot, CancellationToken caller, CancellationToken lifetime)
                : base(context, caller, lifetime) { _snapshot = snapshot; }
            internal override void ExecuteIO(ProgressFileStore store) => _result = Cancellation.IsCancellationRequested
                ? new SaveResult(Context, SaveStatus.Cancelled) : store.Save(Context, _snapshot, Cancellation.Token);
            internal override void Complete()
            {
                Report.TrySetResult(new ProgressOperationResult(_result));
                Completion.TrySetResult(_result);
            }
            internal override void Fail(Exception error)
            { _result = new SaveResult(Context, SaveStatus.WriteFailed, error.ToString()); Complete(); }
        }

        private sealed class ReadWork<T> : Work
        {
            private readonly IProgressCodec<T> _codec;
            private readonly string _family;
            private StoredPayload _stored;
            internal readonly TaskCompletionSource<ReadResult<T>> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            internal ReadWork(ProgressRequestContext context, string family, IProgressCodec<T> codec,
                CancellationToken caller, CancellationToken lifetime) : base(context, caller, lifetime)
            { _family = family; _codec = codec; }
            internal override void ExecuteIO(ProgressFileStore store) => _stored = Cancellation.IsCancellationRequested
                ? new StoredPayload(Context, ReadStatus.Cancelled) : store.Read(Context, Cancellation.Token);
            internal override void Complete()
            {
                var result = Cancellation.IsCancellationRequested ? new ReadResult<T>(Context, ReadStatus.Cancelled)
                    : ProgressSnapshot.Decode(_stored, _family, _codec);
                // codec 运行中外部取消也不交付成功的迟到对象。
                if (Cancellation.IsCancellationRequested) result = new ReadResult<T>(Context, ReadStatus.Cancelled);
                Finish(result);
            }
            private void Finish(ReadResult<T> result)
            {
                Report.TrySetResult(new ProgressOperationResult(result.Context, result.Status, result.Error, result.Slot));
                Completion.TrySetResult(result);
            }
            internal override void Fail(Exception error) => Finish(new ReadResult<T>(Context, ReadStatus.ReadFailed, error.ToString()));
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public ProgressStorageService(ProgressStoreOptions options, int queueCapacity = 16)
            : this(options, queueCapacity, null) { }

        internal ProgressStorageService(ProgressStoreOptions options, int queueCapacity, ProgressFileIO files, long? serviceEpoch = null)
        {
            _ownerThread = Thread.CurrentThread.ManagedThreadId;
            CheckControl();
            if (queueCapacity < 1 || queueCapacity > 1024) throw new ArgumentOutOfRangeException(nameof(queueCapacity));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _capacity = queueCapacity;
            _store = new ProgressFileStore(options, files);
            ServiceEpoch = serviceEpoch ?? ProgressIdentity.NextEpoch();
        }

        public Task<PrepareResult> PrepareAsync(CancellationToken cancellation = default)
        {
            CheckControl();
            if (_state != SaveServiceState.Uninitialized) return _ready.Task;
            _state = SaveServiceState.Preparing;
            _ = PrepareCore(Request(), cancellation);
            return _ready.Task;
        }

        public SlotCatalog GetSlots()
        {
            CheckOwner();
            var current = _store.Catalog;
            return new SlotCatalog(_state, current.Revision, current.LastSuccessfulSaveSlot, current.Slots.ToArray());
        }

        public SaveOperation<SaveResult> SaveAsync<T>(int slot, T snapshot, SaveSummary summary,
            IProgressCodec<T> codec, CancellationToken cancellation = default)
        {
            CheckControl();
            var context = Request(slot);
            var rejection = Rejection(cancellation);
            if (rejection.HasValue) return Reject(new SaveResult(context, rejection.Value));
            SaveResult failure;
            FrozenProgress frozen;
            _insideCodec = true;
            try { failure = ProgressSnapshot.TryFreeze(context, _options, snapshot, summary, codec, out frozen); }
            finally { _insideCodec = false; }
            if (failure != null) return Reject(failure);
            if (cancellation.IsCancellationRequested) return Reject(new SaveResult(context, SaveStatus.Cancelled));
            var work = new SaveWork(context, frozen, cancellation, _lifetime.Token);
            _writes.Add(context.RequestId, work.Report.Task);
            Enqueue(work);
            return new SaveOperation<SaveResult>(context, work.Completion.Task);
        }

        public SaveOperation<ReadResult<T>> ReadAsync<T>(int slot, IProgressCodec<T> codec, CancellationToken cancellation = default)
        {
            CheckControl();
            var context = Request(slot);
            var rejection = Rejection(cancellation);
            ReadStatus? status = rejection switch
            {
                SaveStatus.Draining => ReadStatus.Draining,
                SaveStatus.NotReady => ReadStatus.NotReady,
                SaveStatus.Cancelled => ReadStatus.Cancelled,
                SaveStatus.QueueFull => ReadStatus.QueueFull,
                _ => null
            };
            string error = null;
            if (!status.HasValue && !_options.ContainsSlot(slot)) status = ReadStatus.InvalidSlot;
            if (!status.HasValue)
            {
                _insideCodec = true;
                try { if (codec == null || codec.DataTypeId != _options.DataTypeId) status = ReadStatus.WrongDataType; }
                catch (Exception exception) { status = ReadStatus.DecodeFailed; error = exception.ToString(); }
                finally { _insideCodec = false; }
            }
            if (status.HasValue) return new SaveOperation<ReadResult<T>>(context, Task.FromResult(new ReadResult<T>(context, status.Value, error)));
            var work = new ReadWork<T>(context, _options.DataTypeId, codec, cancellation, _lifetime.Token);
            Enqueue(work);
            return new SaveOperation<ReadResult<T>>(context, work.Completion.Task);
        }

        public DrainTicket BeginDrain() => BeginDrain(Array.Empty<ProgressOperationResult>());

        internal DrainTicket BeginDrain(IReadOnlyList<ProgressOperationResult> earlierResults)
        {
            CheckControl();
            if (_drain != null) return _drain;
            if (_state != SaveServiceState.Ready && _state != SaveServiceState.Faulted)
                throw new InvalidOperationException("必须等待准备完成后才能开始 Drain。");
            if (_state == SaveServiceState.Ready) _state = SaveServiceState.Draining;
            long watermark = Math.Max(_lastRequest, earlierResults.Count == 0 ? 0 : earlierResults.Max(item => item.Context.RequestId));
            var records = _writes.Values.Concat(_pending.Values.Select(work => work.Report.Task))
                .Concat(earlierResults.Select(Task.FromResult)).Distinct().ToArray();
            _drain = new DrainTicket(Request(), watermark, records);
            return _drain;
        }

        public async Task<DrainResult> DrainAsync(DrainTicket ticket, CancellationToken waitCancellation = default)
        {
            CheckControl();
            if (ticket == null || !ReferenceEquals(ticket, _drain))
                return new DrainResult(Request(), DrainStatus.InvalidTicket, 0, Array.Empty<ProgressOperationResult>(), Array.Empty<long>());
            var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = waitCancellation.Register(() => cancelled.TrySetResult(true));
            await Task.WhenAny(ticket.Completion, cancelled.Task, ticket.Invalidated.Task);
            if (!ReferenceEquals(ticket, _drain)) return DrainSnapshot(ticket, DrainStatus.InvalidTicket);
            if (!ticket.Completion.IsCompleted) return DrainSnapshot(ticket, DrainStatus.WaitCancelled);
            return DrainSnapshot(ticket);
        }

        public bool ResumeRequests(DrainTicket ticket)
        {
            CheckControl();
            if (ticket == null || !ReferenceEquals(ticket, _drain) || _state != SaveServiceState.Draining) return false;
            _drain.Invalidated.TrySetResult(true);
            _drain = null;
            _state = SaveServiceState.Ready;
            return true;
        }

        /// <summary>只确认已完成的保存结果；不确认未完成操作，不改写既有句柄/Drain 报告。</summary>
        public void AcknowledgeResults(IReadOnlyList<long> requestIds)
        {
            CheckControl();
            if (requestIds == null) throw new ArgumentNullException(nameof(requestIds));
            foreach (long id in requestIds)
                if (_writes.TryGetValue(id, out var result) && result.IsCompleted) _writes.Remove(id);
        }

        /// <summary>关闭入口并取消尚可取消的工作；等待完成后才释放目录，不发起任何额外保存。</summary>
        public Task StopAsync()
        {
            return BeginStopOwnership().WhenOwnershipReleased;
        }

        /// <summary>控制线程同步封闭入口；返回的三个后台完成信号均不依赖 Unity 续体。等待超时不释放目录。</summary>
        public ProgressStorageStop BeginStopOwnership()
        {
            CheckOwner();
            if (_insideCodec) throw new InvalidOperationException("codec 执行期间不能停止存储服务。");
            if (_ownershipStop == null) StopCore();
            return _ownershipStop;
        }

        /// <summary>返回已完成但未确认的保存结果，用于模块跨世代保留，不持有业务进度。</summary>
        public IReadOnlyList<ProgressOperationResult> GetUnacknowledgedSaveResults()
        {
            CheckOwner();
            return Array.AsReadOnly(_writes.Values.Where(task => task.IsCompleted).Select(task => task.Result)
                .OrderBy(result => result.Context.RequestId).ToArray());
        }
        #endregion
    }
}
