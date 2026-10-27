using System;
using Ember.Basic;
using UnityEngine.InputSystem;

namespace Game.Input
{
    /// <summary>可用性通知和读取入口。消费者不得自行 Enable/Disable 归属通道的 Map。</summary>
    public sealed class GameInputChannel
    {
        #region 内部参数
        internal InputActionMap Map { get; }
        internal bool DefaultEnabled { get; }
        public string Id { get; }
        public bool IsAvailable { get; internal set; }
        public bool IsStopped { get; internal set; }
        public event Action<GameInputChannel> AvailabilityChanged;
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        internal GameInputChannel(GameInputChannelConfig config, InputActionMap map)
        {
            Id = config.Id;
            DefaultEnabled = config.DefaultEnabled;
            Map = map;
        }

        public InputAction GetAction(string actionName)
        {
            if (IsStopped) throw new ObjectDisposedException(nameof(GameInputChannel));
            return Map.FindAction(actionName, true);
        }

        /// <summary>连续输入语义由调用方选择；关闭时返回零，恢复后可恢复当前轴值。</summary>
        public T ReadContinuous<T>(string actionName) where T : struct
            => IsAvailable ? GetAction(actionName).ReadValue<T>() : default;

        internal void Notify()
        {
            var handlers = AvailabilityChanged;
            if (handlers == null) return;
            foreach (Action<GameInputChannel> handler in handlers.GetInvocationList())
            {
                try { handler(this); }
                catch (Exception exception)
                {
                    EmberDebug.LogError("Game.Input", "输入可用性订阅失败：" + exception);
                }
            }
        }

        internal void ClearSubscribers() => AvailabilityChanged = null;
        #endregion
    }
}
