using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Basic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.SceneFlow
{
    /// <summary>base 场景装配组件。只组织显式参与者，不加载场景、不读取存档、不驱动 Tick。</summary>
    [DisallowMultipleComponent]
    public sealed class GameSceneScope : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private GameSceneParticipant[] _participants = Array.Empty<GameSceneParticipant>();
        #endregion

        // --------------------------------------------------------
        #region 内部参数
        private GameSceneSession _session;
        #endregion

        // --------------------------------------------------------
        #region 生命周期
        private void OnDisable() => _session?.DisposeSafely();
        private void OnDestroy() => _session?.DisposeSafely();
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private void Collect(GameSceneParticipant participant, HashSet<GameSceneParticipant> seen,
            List<GameSceneParticipant> owned)
        {
            if (participant == null || !participant.isActiveAndEnabled || participant.gameObject.scene != gameObject.scene)
                throw new InvalidOperationException("必要参与者缺失、未启用或不属于本场景。");
            if (!seen.Add(participant)) throw new InvalidOperationException("参与者重复或组所有权存在循环：" + participant.name);
            owned.Add(participant);
            var children = participant.OwnedParticipants;
            if (children == null) throw new InvalidOperationException("组的子参与者声明缺失。");
            foreach (var child in children) Collect(child, seen, owned);
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        /// <summary>单次准备串行汇总。任何失败均清理已返回结果；不把取消后迟到的结果交接给新会话。</summary>
        public async UniTask<GameSceneSession> PrepareAsync(long requestId, object args, CancellationToken cancellation)
        {
            if (!isActiveAndEnabled) throw new InvalidOperationException("SceneScope 必须启用。");
            if (_session != null) throw new InvalidOperationException("SceneScope 已有准备或运行会话。");
            var participants = _participants == null ? throw new InvalidOperationException("参与者集合缺失。")
                : (GameSceneParticipant[])_participants.Clone();
            var seen = new HashSet<GameSceneParticipant>();
            var owned = new List<GameSceneParticipant>();
            foreach (var participant in participants) Collect(participant, seen, owned);
            var session = new GameSceneSession(this, participants, owned.ToArray(), requestId, args, cancellation);
            _session = session;
            await session.PrepareAsync();
            return session;
        }

        internal void Release(GameSceneSession session)
        { if (ReferenceEquals(_session, session)) _session = null; }

        /// <summary>限定在目标场景内查找唯一显式 Scope；静态场景使用空参与者列表。</summary>
        [HasGC]
        public static GameSceneScope Require(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("目标场景尚未加载。");
            GameSceneScope result = null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var scope in root.GetComponentsInChildren<GameSceneScope>(true))
                {
                    if (result != null) throw new InvalidOperationException("目标场景存在多个 SceneScope。");
                    result = scope;
                }
            if (result == null || !result.isActiveAndEnabled) throw new InvalidOperationException("目标场景缺少启用的 SceneScope。");
            return result;
        }
        #endregion
    }
}
