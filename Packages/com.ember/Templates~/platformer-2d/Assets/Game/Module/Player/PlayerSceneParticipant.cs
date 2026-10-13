using System;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.Input;
using Game.Module;
using Game.SceneFlow;
using UnityEngine;

namespace Game.Player
{
    /// <summary>正式玩家 Prefab 的场景准备适配。只交接资源，Tick 仍唯一归 PlayerModule。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerController))]
    public sealed class PlayerSceneParticipant : GameSceneParticipant, IGameSceneReplaceableParticipant
    {
        #region 内部参数
        private PreparedPlayer _prepared;
        /// <summary>仅反映当前仍由 Scope 持有的实际结果，不触发准备或启动。</summary>
        public long? PreparedRequestId => _prepared?.RequestId;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private async UniTask<IGameScenePreparedParticipant> PrepareWithDependencies(GameScenePreparationContext context,
            PlayerModule player, GameInputModule input, PlayerDataModule data)
        {
            if (_prepared != null) throw new InvalidOperationException("玩家参与者已有准备结果。");
            if (context.Args != null && context.Args is not PlayerEntryRequest)
                throw new ArgumentException("进入参数必须明确为新游戏或指定槽位。");
            var controller = GetComponent<PlayerController>();
            if (controller == null || !controller.isActiveAndEnabled || gameObject.scene != context.Scene)
                throw new InvalidOperationException("玩家必须启用并属于本次候选场景。");
            var candidate = await player.PrepareAsync(controller,
                context.Args as PlayerEntryRequest ?? PlayerEntryRequest.NewGame(), input, data, context.Cancellation);
            return _prepared = new PreparedPlayer(this, player, candidate, context.RequestId);
        }

        private sealed class PreparedPlayer : IGameScenePreparedParticipant
        {
            private readonly PlayerModule _player;
            private readonly PlayerSceneParticipant _participant;
            private readonly PreparedPlayerSession _candidate;
            private PreparedPlayer _previous;
            private bool _started, _disposed;
            internal long RequestId { get; }

            internal PreparedPlayer(PlayerSceneParticipant participant, PlayerModule player, PreparedPlayerSession candidate,
                long requestId, PreparedPlayer previous = null)
            { _participant = participant; _player = player; _candidate = candidate; RequestId = requestId; _previous = previous; }

            internal async UniTask<IGameScenePreparedParticipant> PrepareReplacement(GameScenePreparationContext context)
            {
                if (_disposed || !_started || !ReferenceEquals(_participant._prepared, this))
                    throw new InvalidOperationException("玩家场景结果已失效，不能替换。");
                if (context.Scene != _participant.gameObject.scene || context.Args is not PlayerEntryRequest entry)
                    throw new ArgumentException("替换需要当前场景与明确的玩家进入请求。");
                var candidate = await _player.PrepareReplacementAsync(_candidate, entry, context.Cancellation);
                return new PreparedPlayer(_participant, _player, candidate, context.RequestId, this);
            }

            public void Start()
            {
                if (_disposed || _started) throw new InvalidOperationException("玩家场景结果不能重复启动或在释放后启动。");
                if (_previous != null && !ReferenceEquals(_participant._prepared, _previous))
                    throw new InvalidOperationException("原玩家场景结果已经改变。");
                _player.StartSession(_candidate);
                _participant._prepared = this;
                _previous = null;
                _started = true;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                try { _player.StopSession(_candidate); }
                finally
                {
                    _candidate.Dispose();
                    if (ReferenceEquals(_participant._prepared, this)) _participant._prepared = null;
                }
            }
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public override UniTask<IGameScenePreparedParticipant> PrepareAsync(GameScenePreparationContext context)
        {
            var modules = EmberModuleCollector.Instance;
            if (!modules.TryGetModule<PlayerModule>(out var player) || !modules.TryGetModule<GameInputModule>(out var input)
                || !modules.TryGetModule<PlayerDataModule>(out var data))
                throw new InvalidOperationException("平台玩家及输入/存档依赖未装配。");
            return PrepareWithDependencies(context, player, input, data);
        }

        public UniTask<IGameScenePreparedParticipant> PrepareReplacementAsync(GameScenePreparationContext context,
            IGameScenePreparedParticipant current)
        {
            if (current is not PreparedPlayer prepared || !ReferenceEquals(_prepared, prepared))
                throw new InvalidOperationException("替换只能使用本玩家当前的场景结果。");
            return prepared.PrepareReplacement(context);
        }
        #endregion
    }
}
