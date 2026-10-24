using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Ember.Basic;
using Ember.Core;
using Ember.Resource;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ember.Scene
{
    public partial class EmberSceneManager
    {
        private bool _transitionRetired;
        private long _transitionGeneration;
        private AsyncOperation _transitionLoad;

        private bool IsRetired(long generation) => _transitionRetired || generation != _transitionGeneration;

        private void RetireTransitions()
        {
            _transitionRetired = true;
            OnBeforeActivate = null;
            // Unity cannot cancel a load. Never leave activation blocked across Play exit.
            if (_transitionLoad != null) _transitionLoad.allowSceneActivation = true;
        }

        #region 外部方法
        /// <summary>
        /// 可等待的 Additive 加载。Unity 加载不能强制终止；取消后仍等待激活完成并报告所有权，
        /// 由转场 Runner 卸载候选。不会把忙碌或空 AsyncOperation 当作成功回调。
        /// </summary>
        public async UniTask<EmberTransitionSceneLoad> LoadForTransitionAsync(string sceneName, CancellationToken cancellation)
        {
            long generation = _transitionGeneration;
            if (_transitionRetired) throw new OperationCanceledException("场景服务已停止。");
            if (IsLoading) return new EmberTransitionSceneLoad(false, "已有场景正在加载。");
            if (IsSceneLoaded(sceneName)) return new EmberTransitionSceneLoad(false);
            cancellation.ThrowIfCancellationRequested();
            IsLoading = true;
            Progress = 0;
            AsyncOperation operation = null;
            string error = null;
            try
            {
                operation = EmberResourceManager.Instance.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                _transitionLoad = operation;
                if (operation == null) return new EmberTransitionSceneLoad(false, "找不到场景：" + sceneName);
                operation.allowSceneActivation = false;
                EmberEventBus.OnNext(EmberBroadcastEvent.SceneLoadStart);
                while (operation.progress < 0.9f && !IsRetired(generation) && !cancellation.IsCancellationRequested)
                {
                    Progress = operation.progress;
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
                if (!IsRetired(generation) && !cancellation.IsCancellationRequested && OnBeforeActivate != null)
                {
                    var activate = new UniTaskCompletionSource();
                    // A delayed external callback only completes the local wait, never touches a retired operation.
                    OnBeforeActivate.Invoke(ResolveScene(sceneName), () => activate.TrySetResult());
                    await activate.Task.AttachExternalCancellation(cancellation);
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception failure) { error = failure.ToString(); }
            finally
            {
                try
                {
                    if (operation != null)
                    {
                        operation.allowSceneActivation = true;
                        while (!operation.isDone) await UniTask.Yield(PlayerLoopTiming.Update);
                    }
                }
                finally
                {
                    if (generation == _transitionGeneration) { IsLoading = false; Progress = 1; _transitionLoad = null; }
                }
            }
            bool acquired = operation != null && IsSceneLoaded(sceneName);
            if (acquired && !IsRetired(generation) && !cancellation.IsCancellationRequested && error == null)
            {
                try
                {
                    EmberEventBus.OnNext(EmberBroadcastEvent.SceneLoaded);
                    EmberEventBus.OnNext(EmberBroadcastEvent.SceneLoadDone);
                }
                catch (Exception failure) { error = failure.ToString(); }
            }
            return new EmberTransitionSceneLoad(acquired, error ?? (acquired ? null : "场景未完成加载：" + sceneName));
        }

        /// <summary>不使用已取消的转场令牌中断清理；完成后才允许下一次转场。</summary>
        public async UniTask UnloadForTransitionAsync(string sceneName)
        {
            long generation = _transitionGeneration;
            if (_transitionRetired) throw new OperationCanceledException("场景资源已交由退出 Play 接管，不能报告异步卸载完成。");
            if (!IsSceneLoaded(sceneName)) return;
            EmberEventBus.OnNext(EmberBroadcastEvent.SceneUnloading);
            var operation = SceneManager.UnloadSceneAsync(sceneName);
            if (operation == null) throw new InvalidOperationException("无法卸载场景：" + sceneName);
            while (!operation.isDone) await UniTask.Yield(PlayerLoopTiming.Update);
            if (IsRetired(generation)) throw new OperationCanceledException("退出期间的场景卸载未由本请求确认。");
            if (IsSceneLoaded(sceneName)) throw new InvalidOperationException("场景卸载未完成：" + sceneName);
            if (CurrentScene == sceneName) CurrentScene = null;
        }

        /// <summary>候选加载不发布当前场景；仅在状态提交成功后调用。</summary>
        public void SetCurrentScene(string sceneName)
        {
            if (_transitionRetired) throw new OperationCanceledException("旧场景服务不能发布场景。");
            if (!IsSceneLoaded(sceneName)) throw new InvalidOperationException("场景尚未加载：" + sceneName);
            CurrentScene = sceneName;
        }
        #endregion
    }
}
