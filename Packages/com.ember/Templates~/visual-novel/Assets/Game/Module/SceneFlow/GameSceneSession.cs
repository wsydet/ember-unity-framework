using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Ember.Basic;
using Ember.Core;

namespace Game.SceneFlow
{
    /// <summary>请求持有的独立候选；启动后仍由 Scope/状态持有。Module Reset 不改变此对象。</summary>
    public sealed class GameSceneSession : IDisposable
    {
        #region 内部参数
        private readonly GameSceneScope _scope;
        private readonly GameSceneParticipant[] _participants;
        private readonly GameSceneParticipant[] _drivers;
        private readonly List<IGameScenePreparedParticipant> _results = new();
        private readonly CancellationTokenSource _cancellation;
        private readonly GameScenePreparationContext _context;
        private int _claimed;
        private bool _preparing = true;
        private bool _released;
        private bool _disposing;
        private GameSceneReplacement _replacement;
        private readonly TaskCompletionSource<bool> _releaseCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private IDisposable _lifetimeRegistration;
        private Exception _releaseFailure;
        /// <summary>实际释放完准备、替换及所有占用后完成；清理异常通过此任务报告。不能在主线程同步等待。</summary>
        public Task WhenReleased => _releaseCompletion.Task;
        public bool IsPrepared { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsDisposed { get; private set; }
        public long RequestId => _context.RequestId;
        public object Args => _context.Args;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        internal GameSceneSession(GameSceneScope scope, GameSceneParticipant[] drivers, GameSceneParticipant[] participants,
            long requestId, object args, CancellationToken cancellation)
        {
            _scope = scope; _drivers = drivers; _participants = participants;
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            _context = new GameScenePreparationContext(requestId, scope.gameObject.scene, args, _cancellation.Token);
            _lifetimeRegistration = GameLauncher.CurrentLifetime?.RegisterStopOwner(StopAsync);
        }

        private void Validate()
        {
            if (IsDisposed) throw new OperationCanceledException("场景候选已释放。");
            _context.Cancellation.ThrowIfCancellationRequested();
            if (_scope == null || !_scope.isActiveAndEnabled) throw new OperationCanceledException("SceneScope 已失效。");
            foreach (var participant in _participants)
                if (participant == null || !participant.isActiveAndEnabled || participant.gameObject.scene != _context.Scene)
                    throw new OperationCanceledException("必要参与者已失效。");
        }

        private void ReleaseClaims()
        {
            if (_released || _preparing || _disposing || _replacement != null) return;
            _released = true;
            for (int i = 0; i < _claimed; i++)
                if (!ReferenceEquals(_participants[i], null)) _participants[i].Release(this);
            if (_scope != null) _scope.Release(this);
            _cancellation.Dispose();
            _lifetimeRegistration?.Dispose();
            if (_releaseFailure == null) _releaseCompletion.TrySetResult(true);
            else _releaseCompletion.TrySetException(_releaseFailure);
        }

        internal async UniTask PrepareAsync()
        {
            try
            {
                Validate();
                foreach (var participant in _participants) { participant.Claim(this); _claimed++; }
                foreach (var participant in _drivers)
                {
                    Validate();
                    var prepared = await participant.PrepareAsync(_context);
                    if (prepared == null) throw new InvalidOperationException("参与者未返回准备结果：" + participant.name);
                    // Dispose 可在等待期间发生；迟到结果必须释放，不得遗漏或启动。
                    if (IsDisposed)
                    {
                        try { prepared.Dispose(); }
                        catch (Exception error) { _releaseFailure = error; throw; }
                        throw new OperationCanceledException("准备结果迟到。");
                    }
                    _results.Add(prepared);
                    Validate();
                }
                IsPrepared = true;
            }
            catch (Exception failure)
            {
                try { Dispose(); }
                catch (Exception cleanup) { throw new AggregateException(failure, cleanup); }
                throw;
            }
            finally
            {
                _preparing = false;
                if (IsDisposed) ReleaseClaims();
            }
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        /// <summary>保留世界会话，只准备一个顶层参与者的新结果。调用者通过公共原位协议提交或释放。</summary>
        public async UniTask<GameSceneReplacement> PrepareReplacementAsync(GameSceneParticipant participant,
            long requestId, object args, CancellationToken cancellation)
        {
            Validate();
            if (!IsRunning || _replacement != null) throw new InvalidOperationException("场景未运行或已有替换请求。");
            int index = Array.IndexOf(_drivers, participant);
            if (index < 0 || participant is not IGameSceneReplaceableParticipant replaceable)
                throw new InvalidOperationException("只能替换显式支持协议的顶层参与者；组内对象由组负责。");
            if (requestId <= 0) throw new ArgumentOutOfRangeException(nameof(requestId));
            var replacement = new GameSceneReplacement(this, index, _results[index], replaceable,
                requestId, _context.Scene, args, _context.Cancellation, cancellation);
            _replacement = replacement;
            await replacement.PrepareAsync();
            return replacement;
        }

        internal void ValidateReplacement(GameSceneReplacement replacement)
        {
            Validate();
            if (!IsRunning || !ReferenceEquals(_replacement, replacement))
                throw new OperationCanceledException("场景替换请求已失效。");
        }

        internal bool OwnsResult(IGameScenePreparedParticipant result)
        {
            foreach (var owned in _results) if (ReferenceEquals(owned, result)) return true;
            return false;
        }

        internal void CommitReplacement(GameSceneReplacement replacement, int index,
            IGameScenePreparedParticipant previous, IGameScenePreparedParticipant candidate)
        {
            ValidateReplacement(replacement);
            if (!ReferenceEquals(_results[index], previous)) throw new InvalidOperationException("被替换结果已改变。");
            candidate.Start(); // 实现须在破坏旧资源前完成可能失败的检查；SceneScope 不声称回滚业务副作用。
            ValidateReplacement(replacement);
            _results[index] = candidate;
            replacement.Transfer(); // 先交接所有权，再清理旧结果；旧清理异常不能丢失新结果。
            previous.Dispose();
        }

        internal void ReleaseReplacement(GameSceneReplacement replacement)
        {
            if (!ReferenceEquals(_replacement, replacement)) return;
            _replacement = null;
            if (IsDisposed) ReleaseClaims();
        }

        internal void RecordReleaseFailure(Exception error)
            => _releaseFailure = _releaseFailure == null ? error : new AggregateException(_releaseFailure, error);

        /// <summary>由 Gameplay Module 激活后的状态钩子调用，只允许启动一次。</summary>
        public void Start()
        {
            if (!IsPrepared || IsDisposed || IsRunning) throw new InvalidOperationException("场景会话不允许启动。");
            try
            {
                Validate();
                foreach (var result in _results) { result.Start(); Validate(); }
                IsRunning = true;
            }
            catch (Exception failure)
            {
                try { Dispose(); }
                catch (Exception cleanup) { throw new AggregateException(failure, cleanup); }
                throw;
            }
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true; IsRunning = false;
            _disposing = true; // 取消可能同步完成候选续体；旧结果全部清理前不能提前释放场景占用。
            var errors = new List<Exception>();
            try { _cancellation.Cancel(); } catch (Exception error) { errors.Add(error); }
            try { _replacement?.Dispose(); } catch (Exception error) { errors.Add(error); }
            for (int i = _results.Count - 1; i >= 0; i--)
                try { _results[i].Dispose(); } catch (Exception error) { errors.Add(error); }
            _results.Clear();
            _disposing = false;
            if (errors.Count != 0) _releaseFailure = new AggregateException("场景结果清理失败。", errors);
            ReleaseClaims();
            if (errors.Count != 0) throw new AggregateException("场景结果清理失败。", errors);
        }

        /// <summary>取消准备并等待迟到结果与占用释放；重复调用共享同一完成任务。</summary>
        public Task StopAsync()
        {
            DisposeSafely();
            return WhenReleased;
        }

        internal void DisposeSafely()
        {
            try { Dispose(); }
            catch (Exception failure) { EmberDebug.LogError("Game.SceneFlow", failure.ToString()); }
        }

        /// <summary>Module 激活或提交失败时，OnGameplayEnter 可能尚未调用，仍须回收本候选。</summary>
        public async UniTask ReleaseOnFailedTransitionAsync(EmberTransitionOperation operation)
        {
            var result = await operation.Completion;
            if (result.Status != EmberTransitionStatus.Completed) DisposeSafely();
        }
        #endregion
    }
}
