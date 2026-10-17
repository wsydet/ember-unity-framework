using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Game/Player/Level", fileName = "PlayerLevel")]
    public sealed class PlayerLevelDefinition : ScriptableObject
    {
        #region 编辑器面板参数

        [SerializeField] private string _levelId;
        [SerializeField] private string _scenePath;
        [SerializeField] private PlayerConfig _config;

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        public string LevelId => _levelId;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public PlayerEntryRequest CreateEntry(PlatformerSnapshot checkpoint = null)
            => PlayerEntryRequest.ForLevel(_levelId, _scenePath, _config, checkpoint);

        #endregion
    }
}
