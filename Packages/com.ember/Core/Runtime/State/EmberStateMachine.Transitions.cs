using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Ember.Basic;

namespace Ember.Core
{
    public partial class EmberStateMachine
    {
        #region 内部参数
        private long _transitionId;
        private EmberTransitionOperation _activeTransition;
        private EmberTransitionOperation _enteredTransition;
        private bool _stopped;
        private bool _evaluatingGuard;
        private Task<EmberTransitionResult> _transitionStop;
        /// <summary>正式桥接：必须等待准备、提交和场景清理，异常由请求结果承接。</summary>
        public Func<SceneTransitionContext, UniTask> OnSceneTransitionAsync;
        public bool IsTransitioning => _activeTransition != null;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private EmberTransitionOperation Reject(EmberTransitionStatus status, string error)
        {
            var operation = new EmberTransitionOperation(++_transitionId, default);
            operation.Finish(status, error);
            return operation;
        }

        private EmberTransitionOperation Request<T>(object args, bool skipSceneLoad, bool push,
            CancellationToken cancellation) where T : EmberGameState
        {
            if (_stopped) return Reject(EmberTransitionStatus.Stopped, "状态机已停止。");
            if (_activeTransition != null || _evaluatingGuard) return Reject(EmberTransitionStatus.Busy, "上一转场仍在准备、提交或清理。");
            if (!TryGetState<T>(out var next)) return Reject(EmberTransitionStatus.Rejected, "目标状态未注册。");
            if (!push && _current == next && !next.AllowReEnter)
                return Reject(EmberTransitionStatus.Rejected, "当前状态不允许重入。");
            try
            {
                _evaluatingGuard = true;
                if (_current != null && !ValidateGuard(_current, typeof(T), push ? "Push" : "TransitionTo"))
                    return Reject(EmberTransitionStatus.Rejected, "Guard 拒绝进入。");
            }
            catch (Exception failure) { return Reject(EmberTransitionStatus.Rejected, failure.ToString()); }
            finally { _evaluatingGuard = false; }
            if (_stopped) return Reject(EmberTransitionStatus.Stopped, "Guard 期间状态机已停止。");
            string fromScene = _currentScenePath ?? _current?.ScenePath ?? "";
            string toScene = next.ScenePath ?? "";
            var type = push || (_current != null && (string.IsNullOrEmpty(toScene) || toScene == fromScene))
                ? TransitionType.Push : TransitionType.TransitionTo;
            QuickSceneLoad = type == TransitionType.TransitionTo && !skipSceneLoad
                && (FindTransitionDescriptor(_current, typeof(T))?.QuickSceneLoad ?? false);
            return Begin(next, type, fromScene, toScene, args, skipSceneLoad, cancellation);
        }

        private EmberTransitionOperation Begin(EmberGameState next, TransitionType type,
            string fromScene, string toScene, object args, bool skipSceneLoad, CancellationToken cancellation)
        {
            var operation = new EmberTransitionOperation(++_transitionId, cancellation);
            _activeTransition = operation;
            var scenesToRelease = new System.Collections.Generic.List<string>();
            if (type != TransitionType.Push && !string.IsNullOrEmpty(fromScene) && fromScene != toScene)
                scenesToRelease.Add(fromScene);
            if (type == TransitionType.TransitionTo)
                foreach (var scene in _popScenePathStack)
                    if (!string.IsNullOrEmpty(scene) && scene != toScene && !scenesToRelease.Contains(scene))
                        scenesToRelease.Add(scene);
            var context = new SceneTransitionContext
            {
                FromState = _current, ToState = next, FromScene = fromScene, ToScene = toScene,
                Type = type, Args = args, PreparedArgs = args, SkipSceneLoad = skipSceneLoad, Operation = operation,
                ScenesToRelease = scenesToRelease.AsReadOnly()
            };
            context.IsCurrent = () => !_stopped && _activeTransition == operation && _current == context.FromState;
            context.Proceed = () => Commit(context);
            RunTransitionAsync(context).Forget();
            return operation;
        }

        private void Commit(SceneTransitionContext context)
        {
            // Captured delegates remain harmless after completion, cancellation or shutdown.
            if (context.CommitStarted || !context.IsCurrent() || context.Cancellation.IsCancellationRequested) return;
            if (context.Type != TransitionType.Pop && !context.PreparationCompleted)
                throw new InvalidOperationException("目标状态尚未完成本请求的准备。");
            if (!context.Operation.BeginCommit()) return;
            _enteredTransition?.InvalidateEntry();
            _enteredTransition = context.Operation;
            switch (context.Type)
            {
                case TransitionType.InPlace:
                    ((IEmberInPlaceState)_current).CommitInPlace(context);
                    break;
                case TransitionType.TransitionTo:
                    _current?.OnExit();
                    ThrowIfStoppedDuringCommit();
                    // Leaving while an overlay is active also releases its paused owners.
                    while (_overlayStack.Count > 0)
                    {
                        _overlayStack.Pop()?.OnExit();
                        ThrowIfStoppedDuringCommit();
                    }
                    _popScenePathStack.Clear();
                    Previous = _current;
                    _current = context.ToState;
                    _currentScenePath = context.ToScene;
                    _current.OnEnter(context.PreparedArgs);
                    break;
                case TransitionType.Push:
                    _current?.OnPause();
                    ThrowIfStoppedDuringCommit();
                    _overlayStack.Push(_current);
                    _popScenePathStack.Push(_currentScenePath);
                    Previous = _current;
                    _current = context.ToState;
                    // A scene-less overlay must retain the scene that backs its paused state.
                    if (!string.IsNullOrEmpty(context.ToScene)) _currentScenePath = context.ToScene;
                    _current.OnEnter(context.PreparedArgs);
                    break;
                case TransitionType.Pop:
                    _current?.OnExit();
                    ThrowIfStoppedDuringCommit();
                    Previous = _current;
                    _current = _overlayStack.Pop();
                    _currentScenePath = _popScenePathStack.Pop();
                    _current?.OnResume();
                    break;
            }
            context.IsCommitted = true;
            ThrowIfStoppedDuringCommit();
            EmberEventBus.OnNext(EmberBroadcastEvent.GameStateChanged);
            // Shutdown from a lifecycle callback must never publish an actionable entry.
            if (_stopped) throw new OperationCanceledException("状态机在提交期间停止。");
            context.Operation.MarkRunning();
        }

        private void ThrowIfStoppedDuringCommit()
        {
            if (_stopped) throw new OperationCanceledException("状态机在提交期间停止。");
        }

        private async UniTask RunTransitionAsync(SceneTransitionContext context)
        {
            var status = EmberTransitionStatus.Completed;
            string error = null;
            try
            {
                context.ThrowIfObsolete();
                if (OnSceneTransitionAsync != null)
                    await OnSceneTransitionAsync(context);
                else if (!context.SkipSceneLoad && OnSceneTransition != null && context.FromScene != context.ToScene)
                {
                    // Legacy custom bridges own their scene cleanup. New integrations use the awaited bridge.
                    await context.PrepareAsync();
                    var completed = new UniTaskCompletionSource();
                    context.Proceed = () =>
                    {
                        try { Commit(context); completed.TrySetResult(); }
                        catch (Exception failure) { completed.TrySetException(failure); }
                    };
                    OnSceneTransition(context);
                    await completed.Task.AttachExternalCancellation(context.Cancellation);
                }
                else
                {
                    await context.PrepareAsync();
                    context.Proceed();
                }
                if (!context.IsCommitted)
                {
                    context.ThrowIfObsolete();
                    throw new InvalidOperationException("转场桥接结束但没有完成提交。");
                }
            }
            catch (OperationCanceledException failure)
            {
                status = context.CommitStarted
                    ? (context.IsCommitted ? EmberTransitionStatus.CleanupFailed : EmberTransitionStatus.CommitFailed)
                    : EmberTransitionStatus.Cancelled;
                error = failure.ToString();
            }
            catch (Exception failure)
            {
                status = context.CommitStarted
                    ? (context.IsCommitted ? EmberTransitionStatus.CleanupFailed : EmberTransitionStatus.CommitFailed)
                    : EmberTransitionStatus.PreparationFailed;
                error = failure.ToString();
            }
            finally
            {
                try { await context.AbortPreparationAsync(); }
                catch (Exception failure)
                { status = EmberTransitionStatus.CleanupFailed; error += "\n准备清理失败：" + failure; }
                if (context.CleanupFailure != null)
                { status = EmberTransitionStatus.CleanupFailed; error += "\n清理失败：" + context.CleanupFailure; }
                if (_activeTransition == context.Operation) _activeTransition = null;
                context.Operation.Finish(status, error);
            }
        }

        private async UniTask ObserveTransition(EmberTransitionOperation operation)
        {
            var result = await operation.Completion;
            if (result.Status != EmberTransitionStatus.Completed && result.Status != EmberTransitionStatus.Cancelled)
                EmberDebug.LogWarning(TAG, $"转场 #{result.RequestId}：{result.Status}；{result.Error}");
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        /// <summary>整机一个在途请求；并发返回 Busy。Completion 包括目标准备、进入和场景清理。</summary>
        public EmberTransitionOperation RequestTransitionTo<T>(object args = null, bool skipSceneLoad = false,
            CancellationToken cancellation = default) where T : EmberGameState
            => Request<T>(args, skipSceneLoad, false, cancellation);

        public EmberTransitionOperation RequestPush<T>(object args = null, CancellationToken cancellation = default)
            where T : EmberGameState => Request<T>(args, false, true, cancellation);

        /// <summary>显式原位资源交接。当前状态须实现协议并声明自边；不受 AllowReEnter 控制。</summary>
        public EmberTransitionOperation RequestInPlace<T>(object args = null, CancellationToken cancellation = default)
            where T : EmberGameState
        {
            if (_stopped) return Reject(EmberTransitionStatus.Stopped, "状态机已停止。");
            if (_activeTransition != null || _evaluatingGuard)
                return Reject(EmberTransitionStatus.Busy, "上一转场仍在准备、提交或清理。");
            if (!TryGetState<T>(out var target) || _current != target || target is not IEmberInPlaceState)
                return Reject(EmberTransitionStatus.Rejected, "当前状态未实现原位交接协议。");
            try
            {
                _evaluatingGuard = true;
                if (FindTransitionDescriptor(target, typeof(T)) == null || !ValidateGuard(target, typeof(T), "InPlace"))
                    return Reject(EmberTransitionStatus.Rejected, "原位交接必须声明自边且通过 Guard。");
            }
            catch (Exception failure) { return Reject(EmberTransitionStatus.Rejected, failure.ToString()); }
            finally { _evaluatingGuard = false; }
            if (_stopped) return Reject(EmberTransitionStatus.Stopped, "Guard 期间状态机已停止。");
            QuickSceneLoad = false;
            string scene = _currentScenePath ?? target.ScenePath ?? "";
            return Begin(target, TransitionType.InPlace, scene, scene, args, true, cancellation);
        }

        public EmberTransitionOperation RequestPop(CancellationToken cancellation = default)
        {
            if (_stopped) return Reject(EmberTransitionStatus.Stopped, "状态机已停止。");
            if (_activeTransition != null || _evaluatingGuard) return Reject(EmberTransitionStatus.Busy, "上一转场尚未结束。");
            if (_overlayStack.Count == 0) return Reject(EmberTransitionStatus.Rejected, "覆盖栈为空。");
            return Begin(_overlayStack.Peek(), TransitionType.Pop, _currentScenePath,
                _popScenePathStack.Peek(), null, false, cancellation);
        }

        /// <summary>正常取消仍须等待活动请求完成清理；不会强制中断 Unity 的场景加载。</summary>
        public void CancelTransition() => _activeTransition?.Cancel();

        /// <summary>框架销毁前封闭入口并使迟到请求失效。正常离开应先等待自己的转场/保存结果。</summary>
        public void StopTransitions()
        {
            _transitionStop ??= _activeTransition?.Completion ?? Task.FromResult<EmberTransitionResult>(null);
            _stopped = true;
            _enteredTransition?.InvalidateEntry();
            _activeTransition?.Cancel();
        }

        /// <summary>封闭入口并等待本次事务清理。无活动请求时结果为 null；不得在 Unity 主线程同步等待。</summary>
        public Task<EmberTransitionResult> StopTransitionsAsync()
        {
            StopTransitions();
            return _transitionStop;
        }
        #endregion
    }
}
