using System;
using System.Threading;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.SceneFlow
{
    /// <summary>本次场景准备的上下文。参与者必须等待必要子任务，失败时自行释放尚未返回的资源。</summary>
    public sealed class GameScenePreparationContext
    {
        public long RequestId { get; }
        public Scene Scene { get; }
        public object Args { get; }
        public CancellationToken Cancellation { get; }

        internal GameScenePreparationContext(long requestId, Scene scene, object args, CancellationToken cancellation)
        { RequestId = requestId; Scene = scene; Args = args; Cancellation = cancellation; }
    }

    /// <summary>每次准备返回独立结果。Start 只交接已准备资源；Dispose 同时处理未启动与已启动的资源。</summary>
    public interface IGameScenePreparedParticipant : IDisposable
    {
        void Start();
    }

    /// <summary>顶层参与者可选的原位替换协议。准备时保留 current，不得修改或释放旧运行资源。</summary>
    public interface IGameSceneReplaceableParticipant
    {
        UniTask<IGameScenePreparedParticipant> PrepareReplacementAsync(
            GameScenePreparationContext context, IGameScenePreparedParticipant current);
    }

    /// <summary>显式场景参与者。Awake/OnEnable 不得启动业务；不能同时由组和外层 Scope 拥有。</summary>
    public abstract class GameSceneParticipant : MonoBehaviour
    {
        #region 内部参数
        internal GameSceneSession Owner { get; private set; }
        #endregion

        // --------------------------------------------------------
        #region 生命周期
        private void OnDisable() => Owner?.DisposeSafely();
        private void OnDestroy() => Owner?.DisposeSafely();
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        /// <summary>组拥有的子参与者声明；由组自行准备/启动，Scope 只保留所有权，不再次驱动。</summary>
        public virtual IReadOnlyList<GameSceneParticipant> OwnedParticipants => Array.Empty<GameSceneParticipant>();

        public abstract UniTask<IGameScenePreparedParticipant> PrepareAsync(GameScenePreparationContext context);

        internal void Claim(GameSceneSession owner)
        {
            if (Owner != null) throw new InvalidOperationException("参与者已由其他场景准备拥有：" + name);
            Owner = owner;
        }

        internal void Release(GameSceneSession owner)
        { if (ReferenceEquals(Owner, owner)) Owner = null; }
        #endregion
    }
}
