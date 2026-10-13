using System;

namespace Game.Input
{
    public enum GameInputServiceState { Uninitialized, Preparing, Ready, Faulted, Stopped }
    public enum GameInputPrepareStatus { NotInitialized, Ready, ConfigInvalid, InputUnavailable, Stopped }
    public enum GameInputPermission { Unchanged, Allow, Deny }

    public sealed class GameInputPrepareResult
    {
        #region 内部参数
        public long Epoch { get; }
        public GameInputPrepareStatus Status { get; }
        public string Error { get; }
        public bool Succeeded => Status == GameInputPrepareStatus.Ready;
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal GameInputPrepareResult(long epoch, GameInputPrepareStatus status, string error = null)
        {
            Epoch = epoch;
            Status = status;
            Error = error;
        }
        #endregion
    }

    /// <summary>未列出的通道与 Unchanged 一样不参与本请求的仲裁。</summary>
    public readonly struct GameInputRule
    {
        public string ChannelId { get; }
        public GameInputPermission Permission { get; }

        public GameInputRule(string channelId, GameInputPermission permission)
        {
            ChannelId = channelId;
            Permission = permission;
        }
    }

    /// <summary>请求的寿命由请求者拥有；Dispose 幂等，不恢复历史模式。</summary>
    public sealed class GameInputRequest : IDisposable
    {
        #region 内部参数
        private GameInputService _service;
        internal GameInputRule[] Rules { get; private set; }
        public object Owner { get; private set; }
        public int Priority { get; private set; }
        public bool IsReleased => _service == null;
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal GameInputRequest(GameInputService service, object owner, int priority, GameInputRule[] rules)
        {
            _service = service;
            Owner = owner;
            Priority = priority;
            Rules = rules;
        }

        public void Update(int priority, params GameInputRule[] rules)
        {
            if (_service == null) throw new ObjectDisposedException(nameof(GameInputRequest));
            var copy = _service.ValidateRules(rules);
            Priority = priority;
            Rules = copy;
            _service.Recalculate();
        }

        public void Dispose()
        {
            var service = _service;
            if (service == null) return;
            Invalidate();
            service.Release(this);
        }

        internal void Invalidate()
        {
            _service = null;
            Owner = null;
            Rules = Array.Empty<GameInputRule>();
        }
        #endregion
    }
}
