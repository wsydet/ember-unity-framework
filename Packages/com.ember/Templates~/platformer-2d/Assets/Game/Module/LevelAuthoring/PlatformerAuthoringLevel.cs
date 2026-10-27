using System;
using Game.Player;
using UnityEngine;

namespace Game.LevelAuthoring
{
    /// <summary>模板关卡描述；消费项目可以用自己的关卡类型替换提供者。</summary>
    public sealed class PlatformerAuthoringLevel : ScriptableObject
    {
        #region 编辑器面板参数
        [SerializeField] private string _id;
        [SerializeField] private string _sceneGuid;
        [SerializeField] private PlayerConfig _config;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public string Id => _id;
        public string SceneGuid => _sceneGuid;
        public PlayerConfig Config => _config;
        public void Configure(string id, string sceneGuid, PlayerConfig config)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(sceneGuid) || config == null) throw new ArgumentException("关卡身份/场景/配置缺失。");
            _id = id; _sceneGuid = sceneGuid; _config = config;
        }
        #endregion
    }
}
