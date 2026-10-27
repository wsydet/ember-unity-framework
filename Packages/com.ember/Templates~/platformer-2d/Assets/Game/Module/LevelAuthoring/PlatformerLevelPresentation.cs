using System;
using Cysharp.Threading.Tasks;
using Ember.Camera;
using Ember.LevelAuthoring;
using Game.Player;
using Game.SceneFlow;
using UnityEngine;

namespace Game.LevelAuthoring
{
    /// <summary>平台样例的正式参与者：在玩家启动后建立检查点世界基线并绑定已有主相机。</summary>
    public sealed class PlatformerLevelPresentation : GameSceneParticipant
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerCheckpointRecovery _recovery;
        [SerializeField] private EmberParallaxLayer[] _layers = Array.Empty<EmberParallaxLayer>();
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private sealed class Prepared : IGameScenePreparedParticipant
        {
            private readonly PlatformerLevelPresentation _owner;
            public Prepared(PlatformerLevelPresentation owner) { _owner = owner; }
            public void Start()
            {
                _owner._recovery?.InitializeEntryBaseline();
                foreach (var layer in _owner._layers) if (layer != null) layer.BindRuntimeCamera(EmberCameraManager.Instance.MainCamera);
            }
            public void Dispose() { foreach (var layer in _owner._layers) if (layer != null) layer.BindRuntimeCamera(null); }
        }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void Configure(PlayerCheckpointRecovery recovery, EmberParallaxLayer[] layers) { _recovery = recovery; _layers = layers; }
        public override UniTask<IGameScenePreparedParticipant> PrepareAsync(GameScenePreparationContext context)
        { context.Cancellation.ThrowIfCancellationRequested(); return UniTask.FromResult<IGameScenePreparedParticipant>(new Prepared(this)); }
        #endregion
    }
}
