using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ember.Basic;
using Ember.Core;
using Game.PlayerData;
using UnityEngine;

namespace Game.Module
{
    /// <summary>base 可选 Global 存档模块。初始化只准备服务，读取、快照保存和正常退出由业务显式调用。</summary>
    [EmberModule(ModulePhase.Global, Enabled = false)]
    public sealed class PlayerDataModule : EmberSingleton<PlayerDataModule>, IEmberModule
    {
        #region 内部参数
        public const string CONFIG_PATH = "Config/PlayerData/PlayerDataConfig";
        private readonly List<ProgressStorageService> _generations = new();
        private readonly Dictionary<long, ProgressOperationResult> _rejections = new();
        private ProgressStorageService _service;
        private PlayerDataConfig _override;
        private string _persistentRootOverride;
        private bool _hasOverride;
        private SaveServiceState _state;
        private TaskCompletionSource<PrepareResult> _preparing;
        private Task _stopped = Task.CompletedTask;
        private IDisposable _lifetimeRegistration;
        internal ProgressFileIO FilesForNextInit { get; set; }
        public long ServiceEpoch { get; private set; } = ProgressIdentity.NextEpoch();
        public SaveServiceState State => _state == SaveServiceState.Ready ? _service.State : _state;
        public Task<PrepareResult> WhenReady { get; private set; }
        public Task WhenStopped => _stopped;
        public int? LastSuccessfulSaveSlot => GetSlots().LastSuccessfulSaveSlot;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private async Task PrepareGenerationAsync(long epoch, ProgressStoreOptions options, int capacity,
            ProgressFileIO files, Task predecessor, TaskCompletionSource<PrepareResult> completion)
        {
            try
            {
                await predecessor;
                if (epoch != ServiceEpoch) return;
                var service = new ProgressStorageService(options, capacity, files, epoch);
                _service = service;
                _generations.Add(service);
                var result = await service.PrepareAsync();
                if (epoch != ServiceEpoch) return;
                _state = result.Succeeded ? SaveServiceState.Ready : SaveServiceState.Faulted;
                completion.TrySetResult(result);
            }
            catch (Exception exception)
            {
                if (epoch != ServiceEpoch) return;
                _state = SaveServiceState.Faulted;
                completion.TrySetResult(new PrepareResult(ProgressIdentity.Request(epoch), PrepareStatus.IoFailed, exception.ToString()));
            }
        }

        private void CheckConfigurationChange()
        {
            if (_state != SaveServiceState.Uninitialized && _state != SaveServiceState.Stopped)
                throw new InvalidOperationException("请先停止当前存档服务，再设置下一次初始化配置。");
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public PlayerDataModule()
        {
            WhenReady = Task.FromResult(new PrepareResult(ProgressIdentity.Request(ServiceEpoch), PrepareStatus.NotInitialized));
        }

        /// <summary>注入不初始化服务；override 跨 Reset 保留，null 配置明确产生 ConfigInvalid。</summary>
        public void SetConfigurationForNextInit(PlayerDataConfig config, string persistentRoot = null)
        {
            CheckConfigurationChange();
            _override = config; _persistentRootOverride = persistentRoot; _hasOverride = true;
        }

        public void UseDefaultConfigurationForNextInit(string persistentRoot = null)
        {
            CheckConfigurationChange();
            _override = null; _persistentRootOverride = persistentRoot; _hasOverride = false;
        }

        void IEmberModule.OnInit()
        {
            if (_state != SaveServiceState.Uninitialized && _state != SaveServiceState.Stopped) return;
            _lifetimeRegistration?.Dispose();
            _lifetimeRegistration = GameLauncher.CurrentLifetime?.RegisterStopOwner(StopAsync, backgroundIndependent: true);
            ServiceEpoch = ProgressIdentity.NextEpoch();
            _state = SaveServiceState.Preparing;
            _preparing = new TaskCompletionSource<PrepareResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            WhenReady = _preparing.Task;
            try
            {
                var config = _hasOverride ? _override : Resources.Load<PlayerDataConfig>(CONFIG_PATH);
                if (config == null) throw new ArgumentException("缺少 PlayerDataConfig：" + CONFIG_PATH);
                // 在等待旧 I/O 前冻结 SO 设置，Inspector 后续修改不影响本世代。
                var options = config.CreateOptions(_persistentRootOverride ?? Application.persistentDataPath);
                _ = PrepareGenerationAsync(ServiceEpoch, options, config.QueueCapacity, FilesForNextInit, _stopped, _preparing);
            }
            catch (Exception exception)
            {
                _state = SaveServiceState.Faulted;
                _preparing.TrySetResult(new PrepareResult(ProgressIdentity.Request(ServiceEpoch), PrepareStatus.ConfigInvalid, exception.ToString()));
            }
        }

        void IEmberModule.OnDestroy() { _ = StopAsync(); }
        void IEmberModule.ResetModuleData() { _ = StopAsync(); }
        protected override void OnDestroy() { _ = StopAsync(); }

        public SlotCatalog GetSlots()
        {
            if (_service != null)
            {
                var slots = _service.GetSlots();
                return _state == SaveServiceState.Ready ? slots : new SlotCatalog(_state, slots.Revision,
                    slots.LastSuccessfulSaveSlot, slots.Slots.ToArray());
            }
            return new SlotCatalog(_state, 0, null, Array.Empty<SlotMetadata>());
        }

        public SaveOperation<SaveResult> SaveAsync<T>(int slot, T snapshot, SaveSummary summary,
            IProgressCodec<T> codec, CancellationToken cancellation = default)
        {
            if (_state == SaveServiceState.Ready) return _service.SaveAsync(slot, snapshot, summary, codec, cancellation);
            var context = ProgressIdentity.Request(ServiceEpoch, slot);
            var result = new SaveResult(context, SaveStatus.NotReady);
            _rejections.Add(context.RequestId, new ProgressOperationResult(result));
            return new SaveOperation<SaveResult>(context, Task.FromResult(result));
        }

        public SaveOperation<ReadResult<T>> ReadAsync<T>(int slot, IProgressCodec<T> codec, CancellationToken cancellation = default)
        {
            if (_state == SaveServiceState.Ready) return _service.ReadAsync(slot, codec, cancellation);
            var context = ProgressIdentity.Request(ServiceEpoch, slot);
            return new SaveOperation<ReadResult<T>>(context, Task.FromResult(new ReadResult<T>(context, ReadStatus.NotReady)));
        }

        public DrainTicket BeginDrain()
        {
            if (_service == null || _state == SaveServiceState.Preparing || _state == SaveServiceState.Stopped)
                throw new InvalidOperationException("请先等待当前存档服务准备完成。");
            var previous = _generations.Where(item => item != _service).SelectMany(item => item.GetUnacknowledgedSaveResults())
                .Concat(_rejections.Values).ToArray();
            return _service.BeginDrain(previous);
        }

        public Task<DrainResult> DrainAsync(DrainTicket ticket, CancellationToken waitCancellation = default)
        {
            if (_service != null) return _service.DrainAsync(ticket, waitCancellation);
            return Task.FromResult(new DrainResult(ProgressIdentity.Request(ServiceEpoch), DrainStatus.InvalidTicket, 0,
                Array.Empty<ProgressOperationResult>(), Array.Empty<long>()));
        }

        public bool ResumeRequests(DrainTicket ticket) => _service != null && _service.ResumeRequests(ticket);

        public IReadOnlyList<ProgressOperationResult> GetUnacknowledgedSaveResults() => Array.AsReadOnly(
            _generations.SelectMany(item => item.GetUnacknowledgedSaveResults()).Concat(_rejections.Values)
                .OrderBy(item => item.Context.RequestId).ToArray());

        public void AcknowledgeResults(IReadOnlyList<long> requestIds)
        {
            if (requestIds == null) throw new ArgumentNullException(nameof(requestIds));
            foreach (var service in _generations) service.AcknowledgeResults(requestIds);
            foreach (long id in requestIds) _rejections.Remove(id);
            _generations.RemoveAll(item => item != _service && item.State == SaveServiceState.Stopped
                && item.OutstandingCount == 0 && item.UnacknowledgedSaveCount == 0);
        }

        /// <summary>不采集、不自动保存；完成后才可确认旧 I/O 已结束。正常退出先处理 Drain 的结果。</summary>
        public Task StopAsync()
        {
            if (_state == SaveServiceState.Stopped) return _stopped;
            // 先让服务校验主线程及 codec 重入；若调用不合法，不能先破坏模块的世代/引用。
            var old = _service;
            var stop = old?.StopAsync();
            long previousEpoch = ServiceEpoch;
            ServiceEpoch = ProgressIdentity.NextEpoch();
            _state = SaveServiceState.Stopped;
            _preparing?.TrySetResult(new PrepareResult(ProgressIdentity.Request(previousEpoch), PrepareStatus.Stopped));
            WhenReady = Task.FromResult(new PrepareResult(ProgressIdentity.Request(ServiceEpoch), PrepareStatus.Stopped));
            _service = null;
            if (stop != null) _stopped = Task.WhenAll(_stopped, stop);
            return _stopped;
        }
        #endregion
    }
}
