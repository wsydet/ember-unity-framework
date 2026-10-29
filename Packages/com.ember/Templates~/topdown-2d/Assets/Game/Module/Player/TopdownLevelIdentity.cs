using UnityEngine;

namespace Game.Player
{
    public interface IPlayerLevelConfiguration
    {
        string Id {get;}
        string ScenePath {get;}
        PlayerScheme Scheme {get;}
    }
    /// <summary>场景与出生身份；显式登记，不由场景名字猜测存档目的地。</summary>
    [DisallowMultipleComponent]
    public sealed class TopdownLevelIdentity:MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private string _levelId="starter",_spawnId="start",_scenePath="Assets/Game/Scenes/GameplayScene.unity";
        [SerializeField] private ScriptableObject _configuration;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string LevelId=>_levelId;
        public string SpawnId=>_spawnId;
        public string ScenePath=>_scenePath;
        public ScriptableObject Configuration=>_configuration;
        public PlayerScheme Scheme=>(_configuration as IPlayerLevelConfiguration)?.Scheme;
        #endregion
    }
}
