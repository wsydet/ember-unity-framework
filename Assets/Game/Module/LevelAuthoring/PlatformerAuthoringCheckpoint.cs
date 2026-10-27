using Game.Player;
using UnityEngine;

namespace Game.LevelAuthoring
{
    /// <summary>已有检查点机制的白盒入口：仅在稳定支撑时采集；测试恢复采用关卡初始世界基线。</summary>
    [RequireComponent(typeof(BoxCollider2D), typeof(PlayerCheckpointZone2D))]
    public sealed class PlatformerAuthoringCheckpoint : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private Vector2 _spawnOffset = new(0, 1);
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public string Id => GetComponent<PlayerCheckpointZone2D>()?.CheckpointId;
        public Vector2 Spawn => transform.TransformPoint(_spawnOffset);
        public void Configure(string id, Vector2 spawnOffset) { GetComponent<PlayerCheckpointZone2D>().Configure(id); _spawnOffset = spawnOffset; }
        #endregion
    }
}
