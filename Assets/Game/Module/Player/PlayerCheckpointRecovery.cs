using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.Entity;
using UnityEngine;

namespace Game.Player
{
    public sealed class PlayerCheckpointRecord
    {
        #region 内部参数

        private readonly WorldStateRecord2D[] _world;
        public string CheckpointId { get; }
        public PlatformerSnapshot Player { get; }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public WorldStateRecord2D[] CopyWorld() => (WorldStateRecord2D[])_world.Clone();
        public PlayerCheckpointRecord(string checkpointId, PlatformerSnapshot player, WorldStateRecord2D[] world)
        {
            if (string.IsNullOrWhiteSpace(checkpointId)) throw new ArgumentException("检查点需要稳定ID。");
            CheckpointId = checkpointId; Player = player ?? throw new ArgumentNullException(nameof(player));
            _world = (WorldStateRecord2D[])(world ?? throw new ArgumentNullException(nameof(world))).Clone();
        }

        #endregion
    }

    /// <summary>显式世界恢复事务。失败保留暂停和错误，直到重试成功或宿主退出。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerController))]
    public sealed class PlayerCheckpointRecovery : MonoBehaviour, IPlayerDeathHandler
    {
        #region 编辑器面板参数

        [SerializeField] private WorldResetScope2D _world;
        [SerializeField] private string _entryCheckpointId = "entry";

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        public string HandlerId => "checkpoint-world";
        private PlayerPauseRequest _pause;
        private PlayerModule _module;
        private CancellationTokenSource _recoveryCancellation;
        private bool _recovering;
        public PlayerCheckpointRecord Checkpoint { get; private set; }
        public string LastError { get; private set; }
        public bool IsBlocked => _pause != null;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void OnDisable()
        {
            _recoveryCancellation?.Cancel();
            // 部分世界已经复位时不能因适配器禁用而重新驱动旧玩家。
            if (_pause != null) _module?.StopSession();
            _pause?.Dispose(); _pause = null; _module = null;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public void InitializeEntryBaseline()
        {
            if (Checkpoint != null) return;
            var player = GetComponent<PlayerController>();
            if (_world == null || _world.gameObject.scene != gameObject.scene || player.EntrySnapshot == null)
                throw new InvalidOperationException("入口检查点缺少世界Scope或已准备的玩家基线。");
            Checkpoint = new PlayerCheckpointRecord(_entryCheckpointId, player.EntrySnapshot, _world.Capture());
        }
        public void OnDeath(PlayerModule module, PlayerController player)
            => RecoverAsync(module, GameLauncher.Instance.Fsm, WorldResetReason2D.Death, this.GetCancellationTokenOnDestroy()).Forget();
        public bool TryCapture(string checkpointId)
        {
            try { Capture(checkpointId); LastError = null; return true; }
            catch (InvalidOperationException error) { LastError = error.Message; return false; }
        }
        public PlayerCheckpointRecord Capture(string checkpointId)
        {
            var player = GetComponent<PlayerController>();
            if (!player.IsRunning || !player.Context.JumpWindow.HasGroundSupport || _recovering || IsBlocked)
                throw new InvalidOperationException("检查点采集需要稳定地面上的运行玩家。");
            var snapshot = player.Capture(gameObject.scene.name);
            if (_world == null || _world.gameObject.scene != gameObject.scene) throw new InvalidOperationException("检查点缺少同场景世界复位Scope。");
            return Checkpoint = new PlayerCheckpointRecord(checkpointId, snapshot, _world.Capture());
        }
        public void SetBaseline(PlayerCheckpointRecord checkpoint)
        {
            if (_recovering || checkpoint == null || checkpoint.Player.Scene != gameObject.scene.name)
                throw new InvalidOperationException("检查点基线与场景不匹配。");
            Checkpoint = checkpoint;
        }
        public async UniTask<bool> RecoverAsync(PlayerModule module, EmberStateMachine flow, WorldResetReason2D reason,
            CancellationToken cancellation = default, Action restoreProject = null)
        {
            if (_recovering) return false;
            if (module == null || module.Controller != GetComponent<PlayerController>() || Checkpoint == null || _world == null)
                throw new InvalidOperationException("世界恢复需要当前玩家、合法检查点和显式世界Scope。");
            _recovering = true; LastError = null;
            _module = module;
            _recoveryCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            cancellation = _recoveryCancellation.Token;
            try
            {
                _pause ??= module.Pause();
                module.Controller.Context.ExternalMotion.Clear();
                cancellation.ThrowIfCancellationRequested();
                _world.Restore(Checkpoint.CopyWorld(), reason);
                restoreProject?.Invoke();
                var result = await module.RestoreCheckpoint(flow, Checkpoint.Player, cancellation).Completion;
                if (result.Status != EmberTransitionStatus.Completed) throw new InvalidOperationException("玩家检查点交接失败：" + result.Status);
                cancellation.ThrowIfCancellationRequested();
                GetComponent<PlayerRegionTracker2D>()?.Reevaluate();
                _pause.Dispose(); _pause = null;
                _module = null;
                return true;
            }
            catch (Exception error) { LastError = error.ToString(); return false; }
            finally { _recoveryCancellation.Dispose(); _recoveryCancellation = null; _recovering = false; }
        }

        #endregion
    }
}
