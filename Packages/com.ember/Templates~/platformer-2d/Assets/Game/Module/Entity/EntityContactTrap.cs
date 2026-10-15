using UnityEngine;

namespace Game.Entity
{
    /// <summary>周期接触伤害配置；由场景拥有者收集，与玩家攻击共用批次和接收协议。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Entity2D))]
    public sealed class EntityContactTrap : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private string _stableId;
        [SerializeField] private int _team = 2;
        [SerializeField] private Collider2D _shape;
        [SerializeField] private LayerMask _layers = -1;
        [SerializeField, Min(.001f)] private float _interval = .5f;
        [SerializeField] private EntityHitPayload _payload = new();
        #endregion
        #region 外部方法
        public string StableId => _stableId;
        public int Team => _team;
        public Collider2D Shape => _shape;
        public LayerMask Layers => _layers;
        public float Interval => _interval;
        public EntityHitPayload Payload => _payload;
        #endregion
    }
}
