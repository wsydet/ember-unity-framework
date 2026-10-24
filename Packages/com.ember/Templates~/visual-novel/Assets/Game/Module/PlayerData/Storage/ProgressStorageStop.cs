using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game.PlayerData
{
    /// <summary>单代存储停止凭证。完成不依赖控制线程；保存终态和目录释放是不同事实。</summary>
    public sealed class ProgressStorageStop
    {
        private readonly TaskCompletionSource<bool> _io = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<IReadOnlyList<ProgressOperationResult>> _results = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task WhenIoTerminated => _io.Task;
        public Task<IReadOnlyList<ProgressOperationResult>> WhenResultsSettled => _results.Task;
        public Task WhenOwnershipReleased => _released.Task;
        public bool IsOwnershipReleased => _released.Task.Status == TaskStatus.RanToCompletion;
        internal void IoTerminated() => _io.TrySetResult(true);
        internal void ResultsSettled(ProgressOperationResult[] results) => _results.TrySetResult(Array.AsReadOnly(results));
        internal void OwnershipReleased() => _released.TrySetResult(true);
        internal void Fail(Exception error)
        {
            _results.TrySetException(error);
            _released.TrySetException(error);
        }
    }
}
