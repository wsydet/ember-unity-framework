using UnityEngine;

namespace Game.Entity
{
    /// <summary>场景沙袋的装配数据。运行生命与反应请求由会话持有，不自行 Update。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Entity2D), typeof(EntityMotor2D))]
    public sealed class EntityHitActor : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private string _stableId;
        [SerializeField] private int _team = 2;
        [SerializeField] private bool _health = true;
        [SerializeField, Min(.001f)] private float _maximum = 100;
        #endregion
        #region 外部方法
        public string StableId => _stableId;
        public int Team => _team;
        public bool HasHealth => _health;
        public float Maximum => _maximum;
        #endregion
        #region 生命周期
        private void Awake() => GetComponent<EntityMotor2D>().Freeze();
        private void OnDisable() => GetComponent<EntityMotor2D>().Freeze();
        #endregion
    }
}
