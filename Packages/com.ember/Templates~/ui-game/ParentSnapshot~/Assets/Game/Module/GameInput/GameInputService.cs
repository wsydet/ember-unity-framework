using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Game.Input
{
    /// <summary>主线程使用。只管理配置通道的权限，不驱动物理、FSM 或游戏时间。</summary>
    public sealed class GameInputService : IDisposable
    {
        #region 内部参数
        private readonly Dictionary<string, GameInputChannel> _channels = new(StringComparer.Ordinal);
        private readonly List<GameInputRequest> _requests = new();
        private readonly List<GameInputChannel> _changed = new();
        private bool _recalculating;
        private bool _dirty;
        public bool IsStopped { get; private set; }
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private bool Evaluate(GameInputChannel channel)
        {
            bool found = false;
            int priority = int.MinValue;
            bool allowed = channel.DefaultEnabled;
            foreach (var request in _requests)
            {
                foreach (var rule in request.Rules)
                {
                    if (rule.ChannelId != channel.Id || rule.Permission == GameInputPermission.Unchanged) continue;
                    if (!found || request.Priority > priority)
                    {
                        found = true;
                        priority = request.Priority;
                        allowed = rule.Permission == GameInputPermission.Allow;
                    }
                    else if (request.Priority == priority && rule.Permission == GameInputPermission.Deny)
                        allowed = false;
                }
            }
            return allowed;
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        /// <summary>先验证全部通道再接管 Map；runtimeActions 由调用方持有并管理资产寿命。</summary>
        public GameInputService(GameInputConfig config, InputActionAsset runtimeActions)
        {
            if (config == null || runtimeActions == null)
                throw new ArgumentException("配置和运行输入资产不能为空。");
            if (config.Channels == null || config.Channels.Count == 0)
                throw new ArgumentException("至少需要一个输入通道。");
            var maps = new HashSet<InputActionMap>();
            foreach (var item in config.Channels)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.ActionMap))
                    throw new ArgumentException("通道 ID 和 Action Map 必须非空。");
                var map = runtimeActions.FindActionMap(item.ActionMap);
                if (map == null || !maps.Add(map) || _channels.ContainsKey(item.Id))
                    throw new ArgumentException("通道重复、Map 缺失或一个 Map 被多个通道占用：" + item.Id);
                _channels.Add(item.Id, new GameInputChannel(item, map));
            }
            try
            {
                foreach (var channel in _channels.Values) channel.Map.Disable();
                Recalculate();
            }
            catch { Dispose(); throw; }
        }

        public GameInputChannel GetChannel(string channelId)
        {
            if (IsStopped) throw new ObjectDisposedException(nameof(GameInputService));
            if (channelId == null || !_channels.TryGetValue(channelId, out var channel))
                throw new ArgumentException("未配置的输入通道：" + channelId, nameof(channelId));
            return channel;
        }

        public GameInputRequest Request(object owner, int priority, params GameInputRule[] rules)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            var request = new GameInputRequest(this, owner, priority, ValidateRules(rules));
            _requests.Add(request);
            Recalculate();
            return request;
        }

        /// <summary>按引用相等释放同一所有者的请求；不依赖名称或 Equals 的业务语义。</summary>
        public void ReleaseOwner(object owner)
        {
            for (int i = _requests.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_requests[i].Owner, owner)) continue;
                _requests[i].Invalidate();
                _requests.RemoveAt(i);
            }
            Recalculate();
        }

        internal GameInputRule[] ValidateRules(GameInputRule[] rules)
        {
            if (IsStopped) throw new ObjectDisposedException(nameof(GameInputService));
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rule in rules)
            {
                GetChannel(rule.ChannelId);
                if (!ids.Add(rule.ChannelId) || !Enum.IsDefined(typeof(GameInputPermission), rule.Permission))
                    throw new ArgumentException("请求中的通道重复或权限无效。", nameof(rules));
            }
            return (GameInputRule[])rules.Clone();
        }

        internal void Release(GameInputRequest request)
        {
            _requests.Remove(request);
            Recalculate();
        }

        internal void Recalculate()
        {
            if (IsStopped) return;
            _dirty = true;
            if (_recalculating) return;
            _recalculating = true;
            try
            {
                // 通知期间可释放其他请求；本轮通知结束后根据最新所有权重新计算。
                while (_dirty && !IsStopped)
                {
                    _dirty = false;
                    _changed.Clear();
                    foreach (var channel in _channels.Values)
                    {
                        bool available = Evaluate(channel);
                        if (available == channel.IsAvailable) continue;
                        channel.IsAvailable = available;
                        _changed.Add(channel);
                    }
                    foreach (var channel in _changed)
                    {
                        if (IsStopped) break;
                        if (channel.IsAvailable) channel.Map.Enable();
                        else channel.Map.Disable();
                    }
                    foreach (var channel in _changed)
                    {
                        if (IsStopped) break;
                        channel.Notify();
                    }
                }
            }
            finally { _recalculating = false; }
        }

        public void Dispose()
        {
            if (IsStopped) return;
            IsStopped = true;
            foreach (var request in _requests) request.Invalidate();
            _requests.Clear();
            // 先全部关闭查询入口，再触发 Disable 的 canceled 回调。
            foreach (var channel in _channels.Values)
            {
                channel.IsAvailable = false;
                channel.IsStopped = true;
            }
            foreach (var channel in _channels.Values) channel.Map.Disable();
            foreach (var channel in _channels.Values)
            {
                channel.Notify();
                channel.ClearSubscribers();
            }
        }
        #endregion
    }
}
