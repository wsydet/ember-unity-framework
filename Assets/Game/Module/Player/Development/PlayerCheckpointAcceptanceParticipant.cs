#if UNITY_EDITOR || EMBER_PLAYER_ACCEPTANCE
using System;
using Cysharp.Threading.Tasks;
using Game.Entity;
using Game.SceneFlow;
using UnityEngine;

namespace Game.Player
{
    /// <summary>编辑器或专用验收构建白盒：示范项目数据在目标场景准备阶段恢复，不定义产品存档含义。</summary>
    public sealed class PlayerCheckpointAcceptanceParticipant : GameSceneParticipant
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerCheckpointRecovery _recovery;
        [SerializeField] private WorldResetScope2D _world;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public static CheckpointProgress<string> Pending { get; set; }
        public string Applied { get; private set; }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private sealed class Prepared : IGameScenePreparedParticipant
        {
            public void Start() { }
            public void Dispose() { }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public override UniTask<IGameScenePreparedParticipant> PrepareAsync(GameScenePreparationContext context)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            if (Pending != null)
            {
                if (context.Args is not PlayerEntryRequest entry || entry.LevelId != Pending.LevelId ||
                    Pending.Checkpoint.Player.Scene != gameObject.scene.name)
                    throw new InvalidOperationException("白盒进度与本次关卡不匹配。");
                if (_recovery.GetComponent<PlayerController>().IsRunning)
                    throw new InvalidOperationException("项目进度必须在玩家启动前恢复。");
                _world.Restore(Pending.Checkpoint.CopyWorld(), WorldResetReason2D.Load);
                _recovery.SetBaseline(Pending.Checkpoint);
                Applied = Pending.Project;
            }
            return UniTask.FromResult<IGameScenePreparedParticipant>(new Prepared());
        }
        #endregion
    }
}
#endif
