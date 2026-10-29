using System;
using Ember.Basic;
using Game.Player;
using UnityEngine;

namespace Game.LevelAuthoring
{
    [CreateAssetMenu(menuName="Game/Level/Topdown Level",fileName="TopdownLevel")]
    public sealed class TopdownLevelDefinition:EmberBaseSO,IPlayerLevelConfiguration
    {
        #region 编辑器面板参数
        [SerializeField] private string _id,_displayName,_scenePath;
        [SerializeField] private PlayerScheme _scheme;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string Id=>_id;
        public string DisplayName=>_displayName;
        public string ScenePath=>_scenePath;
        public PlayerScheme Scheme=>_scheme;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void Configure(string id,string displayName,string scene,PlayerScheme scheme)
        {
            if(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(displayName)||string.IsNullOrWhiteSpace(scene)||scheme==null)throw new ArgumentException("关卡身份、路径及角色方案必须明确。");
            _id=id;_displayName=displayName;_scenePath=scene;_scheme=scheme;
        }
        #endregion
    }
}
