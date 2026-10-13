using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Core;

namespace Ember.Scene
{
    /// <summary>加载结果区分“本请求取得的场景”与此前已存在的场景；失败也必须报告所有权。</summary>
    public readonly struct EmberTransitionSceneLoad
    {
        public bool Acquired { get; }
        public string Error { get; }
        public EmberTransitionSceneLoad(bool acquired, string error = null) { Acquired = acquired; Error = error; }
    }

    /// <summary>场景事务的最小端口；默认实现由 EmberSceneManager 提供。</summary>
    public interface IEmberTransitionScenes
    {
        bool IsSceneLoaded(string scene);
        UniTask<EmberTransitionSceneLoad> LoadForTransitionAsync(string scene, CancellationToken cancellation);
        UniTask UnloadForTransitionAsync(string scene);
        void SetCurrentScene(string scene);
    }

    /// <summary>加载候选 → 准备 → 提交 → 释放旧场景。准备失败只释放本请求取得的场景。</summary>
    public sealed class EmberSceneTransitionRunner
    {
        #region 内部参数
        private readonly IEmberTransitionScenes _scenes;
        private bool _running;
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public EmberSceneTransitionRunner(IEmberTransitionScenes scenes)
            => _scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));

        public async UniTask RunAsync(SceneTransitionContext request)
        {
            if (_running) throw new InvalidOperationException("已有场景转场仍在执行或清理。");
            _running = true;
            bool acquired = false;
            try
            {
                request.ThrowIfObsolete();
                if (!string.IsNullOrEmpty(request.ToScene))
                {
                    if (request.SkipSceneLoad || request.Type == TransitionType.Pop)
                    {
                        if (!_scenes.IsSceneLoaded(request.ToScene))
                            throw new InvalidOperationException("目标场景尚未就绪：" + request.ToScene);
                    }
                    else
                    {
                        var loaded = await _scenes.LoadForTransitionAsync(request.ToScene, request.Cancellation);
                        acquired = loaded.Acquired;
                        if (loaded.Error != null) throw new InvalidOperationException(loaded.Error);
                        if (!_scenes.IsSceneLoaded(request.ToScene))
                            throw new InvalidOperationException("场景加载未成功：" + request.ToScene);
                    }
                }
                request.ThrowIfObsolete();
                await request.PrepareAsync();
                request.ThrowIfObsolete();
                request.Proceed();
                if (request.IsCommitted && request.Type != TransitionType.InPlace && !string.IsNullOrEmpty(request.ToScene))
                    _scenes.SetCurrentScene(request.ToScene);
                if (request.IsCommitted)
                    foreach (var scene in request.ScenesToRelease)
                        await _scenes.UnloadForTransitionAsync(scene);
            }
            finally
            {
                try
                {
                    if (!request.CommitStarted)
                    {
                        // Candidate disposal may refer to components: do it before unloading its scene.
                        try { await request.AbortPreparationAsync(); }
                        finally
                        {
                            if (acquired)
                            {
                                try { await _scenes.UnloadForTransitionAsync(request.ToScene); }
                                catch (Exception failure) { request.ReportCleanupFailure(failure); throw; }
                            }
                        }
                    }
                    // OnExit/OnEnter may already have changed business state. Retain scenes on commit failure.
                }
                finally { _running = false; }
            }
        }
        #endregion
    }
}
