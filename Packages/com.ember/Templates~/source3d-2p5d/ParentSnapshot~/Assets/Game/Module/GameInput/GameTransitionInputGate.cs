using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Ember.Core;

namespace Game.Input
{
    /// <summary>一个输入服务世代的转场接线。仅释放自己持有的禁止句柄，不强制 Allow。</summary>
    public sealed class GameTransitionInputGate : IDisposable
    {
        #region 内部参数
        private readonly EmberStateMachine _fsm;
        private readonly GameInputService _service;
        private readonly string _channel;
        private readonly Func<SceneTransitionContext, UniTask> _previous;
        private readonly List<Entry> _entries = new();
        private bool _disposed;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private sealed class Entry
        {
            internal EmberTransitionOperation Operation;
            internal GameInputRequest Request;
        }

        private async UniTask RunAsync(SceneTransitionContext context)
        {
            if (_disposed || _service.IsStopped)
                throw new ObjectDisposedException(nameof(GameTransitionInputGate));
            var entry = new Entry { Operation = context.Operation };
            entry.Request = _service.Request(entry, int.MaxValue,
                new GameInputRule(_channel, GameInputPermission.Deny));
            _entries.Add(entry);
            ReleaseWhenSettledAsync(entry).Forget();
            if (_previous != null) await _previous(context);
            else
            {
                await context.PrepareAsync();
                context.Proceed();
            }
        }

        private async UniTask ReleaseWhenSettledAsync(Entry entry)
        {
            var result = await entry.Operation.WhenInteractive;
            await UniTask.SwitchToMainThread();
            if (_disposed || _service.IsStopped) return;
            if (result.Status == EmberTransitionStatus.Completed && entry.Operation.CanInteract)
            {
                // 新进入完整成功后，才回收本世代更早的失败/失效请求；迟到旧结果不碰新句柄。
                for (int i = _entries.Count - 1; i >= 0; i--)
                    if (_entries[i].Operation.RequestId <= entry.Operation.RequestId) Release(_entries[i]);
            }
            else if (!result.CommitStarted) Release(entry); // 未提交失败仍保留旧会话。
            // 提交/外层显露失败不交还操作；保留到后续成功进入或服务停止。
        }

        private void Release(Entry entry)
        {
            _entries.Remove(entry);
            entry.Request.Dispose();
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public GameTransitionInputGate(EmberStateMachine fsm, GameInputService service, string channel)
        {
            _fsm = fsm ?? throw new ArgumentNullException(nameof(fsm));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            service.GetChannel(channel); // 接线时验证配置，不等到转场中才暴露缺失。
            _channel = channel;
            _previous = fsm.OnSceneTransitionAsync;
            fsm.OnSceneTransitionAsync = RunAsync;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_fsm.OnSceneTransitionAsync == RunAsync) _fsm.OnSceneTransitionAsync = _previous;
            foreach (var entry in _entries) entry.Request.Dispose();
            _entries.Clear();
        }
        #endregion
    }
}
