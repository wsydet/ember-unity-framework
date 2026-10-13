using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Basic;
using Ember.Core;
using Game.Input;
using Game.Module;
using Game.PlayerData;
using UnityEngine;

namespace Game.Player
{
    [EmberModule(ModulePhase.Gameplay, Enabled = true)]
    public sealed class PlayerModule : EmberSingleton<PlayerModule>, IEmberModule, IEmberUpdate, IEmberFixedUpdate
    {
        #region 内部参数
        public const string CONFIG_PATH = "Config/Player/PlayerConfig";
        private PlayerConfig _config;
        private Vector2 _initialVelocity;
        private PreparedPlayerSession _session;
        private GameInputRequest _inputRequest, _pauseInput;
        private CancellationTokenSource _preparation;
        private long _request;
        private readonly HashSet<PlayerPauseRequest> _pauses = new();
        private float _previousTimeScale;
        private bool _exiting;
        private bool _exitTransitionAllowed;
        public bool CanLeaveGameplay => !_exiting || _exitTransitionAllowed;
        public bool IsActive { get; private set; }
        public PlayerSessionState State { get; private set; }
        public string LastError { get; private set; }
        public PlayerController Controller => _session?.Controller;
        public int? SourceSlot => _session?.SourceSlot;
        public PlayerPowerProgress PowerProgress => _session?.PowerProgress;
        public bool HasSession => _session != null;
        public bool IsTrial => _session?.IsTrial ?? false;
        public long SessionVersion { get; private set; }
        public PlatformerSnapshot RecoveryTarget => _session?.Recovery?.Target;
        public int? RecoverySlot => _session?.Recovery?.TargetSlot;
        /// <summary>完整会话成功交接（含原位恢复）后通知。观察者异常仅报告，不回滚已交接资源。</summary>
        public event Action<PlayerController> SessionCommitted;
        /// <summary>会话所有权释放时通知；失败候选和旧候选清理不会发出此通知。</summary>
        public event Action<PlayerController> SessionReleased;
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private PlayerConfig LoadConfig()
        {
            if (_config == null) _config = Resources.Load<PlayerConfig>(CONFIG_PATH);
            if (_config == null || _config.Graph == null) throw new InvalidOperationException("缺少 PlayerConfig 或玩家图。");
            return _config;
        }

        private void CheckDependencies(PreparedPlayerSession candidate)
        {
            candidate.Cancellation.ThrowIfCancellationRequested();
            if (candidate.Request != _request || candidate.Disposed || candidate.Controller == null || !candidate.Controller.isActiveAndEnabled)
                throw new OperationCanceledException("玩家候选已失效。");
            if (candidate.Input.State != GameInputServiceState.Ready || candidate.Input.Epoch != candidate.InputEpoch ||
                candidate.Data.State != SaveServiceState.Ready || candidate.Data.ServiceEpoch != candidate.DataEpoch)
                throw new InvalidOperationException("输入或存档服务已停止、未就绪或世代改变。");
        }

        private void OnInvalidated(PlayerController controller)
        { if (controller == Controller) StopSession(); }

        private void NotifySession(Action<PlayerController> handlers, PlayerController controller)
        {
            if (handlers == null) return;
            foreach (Action<PlayerController> handler in handlers.GetInvocationList())
                try { handler(controller); }
                catch (Exception error) { EmberDebug.LogError("Game.Player", "会话观察者失败：" + error); }
        }

        private void Drive(bool physics)
        {
            if (!IsActive || _session == null) return;
            try
            {
                // 运行后请求令牌不再决定会话寿命，但服务失效仍必须关闭角色。
                if (_session.Input.State != GameInputServiceState.Ready || _session.Input.Epoch != _session.InputEpoch ||
                    _session.Data.ServiceEpoch != _session.DataEpoch || _session.Data.State == SaveServiceState.Stopped ||
                    _session.Data.State == SaveServiceState.Faulted)
                    throw new InvalidOperationException("玩家运行依赖失效。");
                if (State != PlayerSessionState.Running) return;
                if (physics) Controller.FixedTick(Time.fixedDeltaTime);
                else Controller.Tick(Time.deltaTime);
            }
            catch (Exception error)
            {
                StopSession(); LastError = error.ToString(); State = PlayerSessionState.Faulted;
                EmberDebug.LogError("Game.Player", LastError);
            }
        }
        private void ReplaceSession(PreparedPlayerSession candidate)
        {
            if (!IsActive || _exiting || candidate.Owner != this || candidate.Transferred || candidate.Previous != _session ||
                _pauses.Count == 0 || State != PlayerSessionState.Paused)
                throw new InvalidOperationException("替换需要当前会话、独立候选及整体暂停。");
            GameInputRequest nextInput = null;
            try
            {
                CheckDependencies(candidate);
                nextInput = candidate.Input.Service.Request(candidate, 0,
                    new GameInputRule(candidate.Config.Channel, GameInputPermission.Allow));
                candidate.Controller.CommitSnapshot(candidate.Runtime, candidate.Previous);
                candidate.Previous.Recovery?.Dispose();
                candidate.Previous.ReleaseScheme();
                _session = candidate; candidate.Transferred = true; candidate.Previous = null;
                _inputRequest?.Dispose(); _inputRequest = nextInput; nextInput = null;
                // 同一 Controller 的失效订阅与所有暂停句柄继续有效；恢复仍由最后一个暂停拥有者决定。
                SessionVersion++; State = PlayerSessionState.Paused;
            }
            catch
            {
                // 若 Controller 已完成交接但旧资源清理抛错，不能让模块继续驱动失效的旧资源。
                if (candidate.Runtime?.Transferred == true && !candidate.Runtime.IsDisposed)
                {
                    candidate.Previous?.Recovery?.Dispose();
                    candidate.Previous?.ReleaseScheme();
                    _session = candidate; candidate.Transferred = true; candidate.Previous = null;
                    _inputRequest?.Dispose(); _inputRequest = nextInput; nextInput = null;
                    SessionVersion++; State = PlayerSessionState.Paused;
                }
                candidate.Dispose();
                throw;
            }
            finally { nextInput?.Dispose(); }
            NotifySession(SessionCommitted, candidate.Controller);
        }

        private async UniTask<PlatformerSnapshot> ReadEntry(PreparedPlayerSession candidate, PlayerEntryRequest entry)
        {
            if (entry.Snapshot != null) return entry.Snapshot;
            if (!entry.Slot.HasValue) throw new ArgumentException("恢复必须指定槽位或完整快照。");
            var read = await candidate.Data.ReadAsync(entry.Slot.Value, new PlatformerSnapshotCodec(), candidate.Cancellation).Completion;
            await UniTask.SwitchToMainThread();
            CheckDependencies(candidate);
            if (!read.Succeeded) throw new InvalidOperationException("读取完整进度失败：" + read.Status + "; " + read.Error);
            return read.Progress;
        }

        private PlayerSaveOperation SaveSnapshot(PreparedPlayerSession session, int slot, PlatformerSnapshot snapshot,
            string label, CancellationToken cancellation)
        {
            var operation = session.Data.SaveAsync(slot, snapshot, new SaveSummary(label), new PlatformerSnapshotCodec(), cancellation);
            return new PlayerSaveOperation(operation, session.Recovery, snapshot);
        }

        #endregion
        // --------------------------------------------------------
        #region 外部方法
        /// <summary>仅测试/特殊装配可在活动前注入。通常模块自行加载 Resources 配置。</summary>
        public void SetConfigurationForNextInit(PlayerConfig config, Vector2 initialVelocity = default)
        {
            if (IsActive || HasSession) throw new InvalidOperationException("玩家模块正在运行。");
            if (!float.IsFinite(initialVelocity.x) || !float.IsFinite(initialVelocity.y))
                throw new ArgumentException("新游戏初始速度必须为有限值。", nameof(initialVelocity));
            _config = config;
            _initialVelocity = initialVelocity;
        }

        void IEmberModule.OnInit() { LoadConfig(); IsActive = true; State = PlayerSessionState.Ready; LastError = null; }
        void IEmberModule.ResetModuleData() { StopSession(); IsActive = false; State = PlayerSessionState.Inactive; }
        void IEmberModule.OnDestroy()
        {
            _request++; _preparation?.Cancel(); _preparation?.Dispose(); _preparation = null;
            StopSession(); IsActive = false; State = PlayerSessionState.Inactive;
        }
        void IEmberUpdate.Update() => Drive(false);
        void IEmberFixedUpdate.FixedUpdate() => Drive(true);

        public async UniTask<PreparedPlayerSession> PrepareAsync(PlayerController controller, PlayerEntryRequest entry,
            GameInputModule input, PlayerDataModule data, CancellationToken cancellation)
        {
            if (HasSession) throw new InvalidOperationException("已有玩家会话，不能覆盖活动角色。");
            if (input == null || data == null || controller == null || entry == null)
                throw new ArgumentException("角色、进入请求或依赖未登记。");
            _preparation?.Cancel(); _preparation?.Dispose();
            _preparation = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            var candidate = new PreparedPlayerSession { Owner = this, Request = ++_request, Controller = controller,
                Input = input, Data = data, InputEpoch = input.Epoch, DataEpoch = data.ServiceEpoch,
                Cancellation = _preparation.Token, Config = LoadConfig(), SourceSlot = entry.Slot, IsTrial = entry.Trial != null };
            try
            {
                candidate.SchemeCapture = new PlayerSchemeCapture(candidate.Config);
                candidate.Config = candidate.SchemeCapture.Config;
                candidate.InputChannel = candidate.Config.Channel;
                var inputReady = await input.WhenReady.AsUniTask().AttachExternalCancellation(candidate.Cancellation);
                var dataReady = await data.WhenReady.AsUniTask().AttachExternalCancellation(candidate.Cancellation);
                if (!inputReady.Succeeded || !dataReady.Succeeded)
                    throw new InvalidOperationException("玩家依赖准备失败：" + inputReady.Status + "/" + dataReady.Status);
                CheckDependencies(candidate);
                PlatformerSnapshot snapshot;
                if (entry.Slot.HasValue || entry.Snapshot != null)
                {
                    snapshot = await ReadEntry(candidate, entry);
                    candidate.Runtime = controller.PrepareSnapshot(candidate, candidate.Config, snapshot, input.Service);
                    candidate.PowerProgress = candidate.Runtime.Context.PowerProgress;
                }
                else
                {
                    controller.PrepareNewGame(candidate, candidate.Config, input.Service,
                        initialVelocity: _initialVelocity, trial: entry.Trial);
                    candidate.PowerProgress = controller.Context.PowerProgress;
                    snapshot = controller.EntrySnapshot;
                }
                if (!candidate.IsTrial) candidate.Recovery = new PlayerRecoveryLedger(snapshot, candidate.DataEpoch, entry.Slot);
                CheckDependencies(candidate);
                return candidate;
            }
            catch { candidate.Dispose(); throw; }
        }

        public void StartSession(PreparedPlayerSession candidate)
        {
            if (candidate?.Previous != null) { ReplaceSession(candidate); return; }
            if (!IsActive || HasSession || candidate == null || candidate.Owner != this || candidate.Transferred)
                throw new InvalidOperationException("玩家模块尚未激活或候选不能交接。");
            try
            {
                CheckDependencies(candidate);
                if (candidate.Runtime != null) candidate.Controller.CommitSnapshot(candidate.Runtime);
                else candidate.Controller.StartPrepared(candidate); // FSM 先启动。
                _inputRequest = candidate.Input.Service.Request(candidate, 0,
                    new GameInputRule(candidate.Config.Channel, GameInputPermission.Allow));
                _session = candidate; candidate.Transferred = true;
                candidate.Controller.Invalidated += OnInvalidated;
                SessionVersion++; State = PlayerSessionState.Running;
            }
            catch { _inputRequest?.Dispose(); _inputRequest = null; candidate.Dispose(); throw; }
            NotifySession(SessionCommitted, candidate.Controller);
        }

        /// <summary>只准备现有玩家的独立替换资源；暂停由公共请求拥有者持有，不由准备隐式取得。</summary>
        internal async UniTask<PreparedPlayerSession> PrepareReplacementAsync(PreparedPlayerSession previous,
            PlayerEntryRequest entry, CancellationToken cancellation)
        {
            if (!IsActive || previous == null || previous.IsTrial || previous != _session || _exiting || entry == null ||
                entry.Snapshot == null && !entry.Slot.HasValue)
                throw new InvalidOperationException("当前玩家会话或替换快照无效。");
            if (previous.Config.Channel != previous.InputChannel)
                throw new InvalidOperationException("活动会话期间不能通过恢复切换输入通道。");
            cancellation.ThrowIfCancellationRequested();
            var candidate = new PreparedPlayerSession
            {
                Owner = this, Previous = previous, Request = _request, Controller = previous.Controller,
                Input = previous.Input, Data = previous.Data, InputEpoch = previous.InputEpoch, DataEpoch = previous.DataEpoch,
                Cancellation = cancellation, Config = previous.Config, InputChannel = previous.InputChannel, SourceSlot = entry.Slot
            };
            try
            {
                candidate.SchemeCapture = previous.SchemeCapture.Retain();
                CheckDependencies(candidate);
                var snapshot = await ReadEntry(candidate, entry);
                if (previous != _session) throw new OperationCanceledException("读取期间原玩家会话已改变。");
                CheckDependencies(candidate);
                candidate.Runtime = candidate.Controller.PrepareSnapshot(candidate, candidate.Config, snapshot, candidate.Input.Service);
                candidate.PowerProgress = candidate.Runtime.Context.PowerProgress;
                candidate.Recovery = new PlayerRecoveryLedger(snapshot, candidate.DataEpoch, entry.Slot);
                CheckDependencies(candidate);
                return candidate;
            }
            catch { candidate.Dispose(); throw; }
        }

        /// <summary>整体暂停玩家 FSM、物理与世界游戏时间；音乐和非缩放 UI 继续。句柄可嵌套。</summary>
        public PlayerPauseRequest Pause()
        {
            if (_session == null) throw new InvalidOperationException("没有运行中的玩家会话。");
            var request = new PlayerPauseRequest(this);
            if (_pauses.Count == 0)
            {
                _pauseInput = _session.Input.Service.Request(_session, 100,
                    new GameInputRule(_session.Config.Channel, GameInputPermission.Deny));
                Controller.Pause(); State = PlayerSessionState.Paused;
                _previousTimeScale = Time.timeScale;
                Time.timeScale = 0;
            }
            _pauses.Add(request);
            return request;
        }

        internal void ReleasePause(PlayerPauseRequest request)
        {
            if (!_pauses.Remove(request)) return;
            if (_pauses.Count != 0 || _session == null) return;
            Controller.Resume();
            _pauseInput?.Dispose(); _pauseInput = null;
            Time.timeScale = _previousTimeScale;
            State = PlayerSessionState.Running;
        }

        public PlayerSaveOperation Save(int slot, CancellationToken cancellation = default)
        {
            if (IsTrial) throw new InvalidOperationException("试跑会话不能进入正式存档；请使用独立试跑重置入口。");
            if (_session == null) throw new InvalidOperationException("没有可采集的玩家会话。");
            if (_exiting) throw new InvalidOperationException("退出正在等待已接受写入，不能再采集保存。");
            var snapshot = PlatformerSnapshot.Capture(_session.Config.Scene, Controller.Context);
            return SaveSnapshot(_session, slot, snapshot, "平台跳跃进度", cancellation);
        }

        /// <summary>恢复本会话初始/已提交的实际快照，不通过槽位重读替代目标。</summary>
        public EmberTransitionOperation Recover(EmberStateMachine flow, CancellationToken cancellation = default)
        {
            if (!IsActive || _session == null || IsTrial || _exiting || !flow.Is<GameplayState>())
                throw new InvalidOperationException("当前会话不能恢复。");
            return flow.RequestInPlace<GameplayState>(PlayerEntryRequest.FromSnapshot(RecoveryTarget, RecoverySlot), cancellation);
        }

        /// <summary>在当前世界读取指定槽位；与内存恢复共用公共事务、候选校验和应用链。</summary>
        public EmberTransitionOperation LoadCheckpoint(EmberStateMachine flow, int slot, CancellationToken cancellation = default)
        {
            if (!IsActive || _session == null || IsTrial || _exiting || !flow.Is<GameplayState>())
                throw new InvalidOperationException("当前会话不能读档。");
            return flow.RequestInPlace<GameplayState>(PlayerEntryRequest.FromSlot(slot), cancellation);
        }

        /// <summary>整体暂停等待保存/Drain；saveSlot=null 不新增保存。失败或取消仅释放本次暂停，保留其他拥有者。</summary>
        public async UniTask<PlayerExitResult> ExitToMenuAsync(EmberStateMachine flow, int? saveSlot,
            CancellationToken cancellation = default)
        {
            if (_exiting) return new PlayerExitResult(PlayerExitStatus.Busy);
            if (!HasSession || IsTrial || !flow.Is<GameplayState>() || flow.IsTransitioning)
                return new PlayerExitResult(PlayerExitStatus.InvalidState);
            var session = _session;
            var snapshot = saveSlot.HasValue ? PlatformerSnapshot.Capture(session.Config.Scene, Controller.Context) : null;
            long version = SessionVersion;
            var pause = Pause();
            _exiting = true;
            SaveResult saved = null;
            DrainResult drained = null;
            DrainTicket ticket = null;
            try
            {
                cancellation.ThrowIfCancellationRequested();
                if (saveSlot.HasValue)
                {
                    saved = await SaveSnapshot(session, saveSlot.Value, snapshot, "退出前保存", cancellation).Completion;
                    if (!saved.Succeeded) return new PlayerExitResult(PlayerExitStatus.SaveFailed, saved);
                }
                if (version != SessionVersion || session != _session) return new PlayerExitResult(PlayerExitStatus.Obsolete, saved);
                cancellation.ThrowIfCancellationRequested();
                ticket = session.Data.BeginDrain();
                drained = await session.Data.DrainAsync(ticket, cancellation);
                if (drained.Status != DrainStatus.Drained)
                    return new PlayerExitResult(PlayerExitStatus.DrainFailed, saved, drained);
                if (version != SessionVersion || session != _session) return new PlayerExitResult(PlayerExitStatus.Obsolete, saved, drained);
                cancellation.ThrowIfCancellationRequested();
                session.Data.AcknowledgeResults(drained.Operations.Select(item => item.Context.RequestId).ToArray());
                session.Data.ResumeRequests(ticket); ticket = null; // Global 服务跨菜单保持。
                _exitTransitionAllowed = true;
                var transition = await flow.RequestTransitionTo<MainState>(cancellation: cancellation).Completion;
                return new PlayerExitResult(transition.Status == EmberTransitionStatus.Completed
                    ? PlayerExitStatus.Completed : PlayerExitStatus.TransitionFailed, saved, drained, transition);
            }
            catch (OperationCanceledException) { return new PlayerExitResult(PlayerExitStatus.Cancelled, saved, drained); }
            finally
            {
                if (ticket != null && session.Data.ServiceEpoch == session.DataEpoch) session.Data.ResumeRequests(ticket);
                _exiting = false; _exitTransitionAllowed = false; pause.Dispose();
            }
        }

        /// <summary>场景参与者只能释放自己交接的会话，旧结果不能结束后来的玩家。</summary>
        internal void StopSession(PreparedPlayerSession owner)
        {
            if (ReferenceEquals(_session, owner)) StopSession();
        }

        public void StopSession()
        {
            var session = _session; _session = null;
            if (session != null) NotifySession(SessionReleased, session.Controller);
            _inputRequest?.Dispose(); _inputRequest = null;
            _pauseInput?.Dispose(); _pauseInput = null;
            foreach (var pause in _pauses) pause.Module = null;
            if (_pauses.Count > 0) Time.timeScale = _previousTimeScale;
            _pauses.Clear();
            if (session != null)
            {
                session.Recovery?.Dispose();
                SessionVersion++;
                if (session.Controller != null)
                {
                    session.Controller.Invalidated -= OnInvalidated;
                    try { session.Controller.StopPrepared(session); } finally { session.ReleaseScheme(); }
                }
                else session.ReleaseScheme();
            }
            State = IsActive ? PlayerSessionState.Ready : PlayerSessionState.Inactive;
        }
        #endregion
    }
}
