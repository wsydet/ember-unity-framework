using UnityEngine;

namespace Game.Player
{
    /// <summary>持久交互样例声明。无 Update；会话按 E 的新按下解析最近对象，进度由同次快照拥有。</summary>
    public sealed class TopdownInteraction:MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private string _stableId="checkpoint.training";
        [SerializeField,Min(.1f)] private float _radius=1.5f;
        [SerializeField] private bool _checkpoint=true;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public string StableId=>_stableId;
        public float Radius=>_radius;
        public bool IsCheckpoint=>_checkpoint;
        internal Color InitialColor {get;private set;}=Color.white;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void Awake(){var sprite=GetComponentInChildren<SpriteRenderer>();if(sprite!=null)InitialColor=sprite.color;}
        #endregion
    }
}
