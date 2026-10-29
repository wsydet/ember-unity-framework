using System;
using Cysharp.Threading.Tasks;
using Ember.Core;
using Game.Input;
using Game.Module;
using Game.SceneFlow;
using UnityEngine;

namespace Game.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerController))]
    public sealed class PlayerSceneParticipant : GameSceneParticipant,IGameSceneReplaceableParticipant
    {
        #region 内部方法
        private async UniTask<IGameScenePreparedParticipant> PrepareWithDependencies(GameScenePreparationContext context,
            PlayerModule module, GameInputModule input, PlayerDataModule data,bool replacement=false)
        {
            if (context.Scene != gameObject.scene || context.Args!=null&&context.Args is not TopdownEntryRequest)
                throw new InvalidOperationException("玩家只接受本场景新游戏或明确的俯视恢复请求。");
            var candidate = await module.PrepareAsync(GetComponent<PlayerController>(), input, data, context.Cancellation,context.Args as TopdownEntryRequest,replacement);
            return new Result(module, candidate);
        }

        private sealed class Result : IGameScenePreparedParticipant
        {
            private readonly PlayerModule _module;
            private readonly PreparedPlayerSession _candidate;
            private bool _disposed;
            internal Result(PlayerModule module, PreparedPlayerSession candidate) { _module = module; _candidate = candidate; }
            public void Start()
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Result));
                _module.StartSession(_candidate);
            }
            public void Dispose() { if (_disposed) return; _disposed = true; _candidate.Dispose(); }
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public override UniTask<IGameScenePreparedParticipant> PrepareAsync(GameScenePreparationContext context)
            =>Prepare(context,false);
        public UniTask<IGameScenePreparedParticipant> PrepareReplacementAsync(GameScenePreparationContext context,IGameScenePreparedParticipant current)
            =>Prepare(context,true);
        private UniTask<IGameScenePreparedParticipant> Prepare(GameScenePreparationContext context,bool replacement)
        {
            var modules = EmberModuleCollector.Instance;
            if (!modules.TryGetModule<PlayerModule>(out var player) || !modules.TryGetModule<GameInputModule>(out var input) ||
                !modules.TryGetModule<PlayerDataModule>(out var data))
                throw new InvalidOperationException("俯视 Player/Input/PlayerData 未装配。");
            return PrepareWithDependencies(context, player, input, data,replacement);
        }
        #endregion
    }
}
