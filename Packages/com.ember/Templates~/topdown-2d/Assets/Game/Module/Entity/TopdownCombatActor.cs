using UnityEngine;

namespace Game.Entity
{
    /// <summary>俯视沙袋/敌人场景声明。无 Update；行为、生命与计时归本次战斗会话。</summary>
    [DisallowMultipleComponent,RequireComponent(typeof(Entity2D),typeof(Rigidbody2D),typeof(BoxCollider2D))]
    public sealed class TopdownCombatActor:MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private string _stableId;
        [SerializeField] private int _team=2;
        [SerializeField,Min(1)] private float _health=8;
        [SerializeField] private bool _chase;
        [SerializeField,Min(0)] private float _speed=2.5f,_detectionRange=8,_damage=1;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string StableId=>_stableId;
        public int Team=>_team;
        public float Health=>_health;
        public bool Chase=>_chase;
        public float Speed=>_speed;
        public float DetectionRange=>_detectionRange;
        public float Damage=>_damage;
        public Color InitialColor {get;private set;}=Color.white;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void Awake(){var sprite=GetComponentInChildren<SpriteRenderer>();if(sprite!=null)InitialColor=sprite.color;var body=GetComponent<Rigidbody2D>();body.linearVelocity=Vector2.zero;body.simulated=false;}
        #endregion
    }
}
