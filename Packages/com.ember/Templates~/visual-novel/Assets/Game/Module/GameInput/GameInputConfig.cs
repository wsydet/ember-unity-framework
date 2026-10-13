using System;
using System.Collections.Generic;
using Ember.Basic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Input
{
    /// <summary>一个配置通道独占一个 Action Map；默认关闭，开放由会话持有的请求决定。</summary>
    [Serializable]
    public sealed class GameInputChannelConfig
    {
        #region 编辑器面板参数
        [SerializeField] private string _id;
        [SerializeField] private string _actionMap;
        [SerializeField] private bool _defaultEnabled;
        #endregion

        // --------------------------------------------------------
        #region 内部参数
        public string Id => _id;
        public string ActionMap => _actionMap;
        public bool DefaultEnabled => _defaultEnabled;
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public GameInputChannelConfig(string id, string actionMap, bool defaultEnabled = false)
        {
            _id = id;
            _actionMap = actionMap;
            _defaultEnabled = defaultEnabled;
        }
        #endregion
    }

    [CreateAssetMenu(menuName = "Game/Input/Module Config", fileName = "GameInputConfig")]
    public sealed class GameInputConfig : EmberBaseSO
    {
        #region 编辑器面板参数
        [SerializeField] private InputActionAsset _actions;
        [SerializeField] private GameInputChannelConfig[] _channels = Array.Empty<GameInputChannelConfig>();
        #endregion

        // --------------------------------------------------------
        #region 内部参数
        public InputActionAsset Actions => _actions;
        public IReadOnlyList<GameInputChannelConfig> Channels => _channels;
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        /// <summary>供显式装配与测试使用；已运行服务持有独立通道设置，不跟随配置变化。</summary>
        public void Configure(InputActionAsset actions, params GameInputChannelConfig[] channels)
        {
            _actions = actions;
            _channels = channels == null ? Array.Empty<GameInputChannelConfig>() : (GameInputChannelConfig[])channels.Clone();
        }
        #endregion
    }
}
