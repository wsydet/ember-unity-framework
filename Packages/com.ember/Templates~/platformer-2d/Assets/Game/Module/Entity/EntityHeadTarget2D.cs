using System;
using UnityEngine;

namespace Game.Entity
{
    /// <summary>首版静止、轴对齐的头部触发区域；标记不包含敌人、伤害或奖励业务。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
    public sealed class EntityHeadTarget2D : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private BoxCollider2D _head;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private Matrix4x4 _matrix;
        private uint _shapeHash;
        public BoxCollider2D Head => _head;
        public bool IsAvailable => isActiveAndEnabled && _head != null && _head.enabled && _head.isTrigger &&
            _head.gameObject.activeInHierarchy && Mathf.Abs(Vector2.Dot(_head.transform.up, Vector2.up)) > .99999f &&
            (_head.attachedRigidbody == null || _head.attachedRigidbody.simulated && _head.attachedRigidbody.bodyType == RigidbodyType2D.Static) &&
            _head.transform.localToWorldMatrix == _matrix && _head.GetShapeHash() == _shapeHash;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void OnEnable()
        {
            if (_head == null) _head = GetComponent<BoxCollider2D>();
            CaptureGeometry();
        }
        #endregion
        // --------------------------------------------------------
        #region 内部方法
        private void CaptureGeometry()
        { if (_head != null) { _matrix = _head.transform.localToWorldMatrix; _shapeHash = _head.GetShapeHash(); } }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void Configure(BoxCollider2D head)
        {
            if (head == null || head.gameObject != gameObject || !head.isTrigger)
                throw new ArgumentException("头部需要同对象独立 BoxCollider2D Trigger。");
            _head = head; CaptureGeometry();
        }
        #endregion
    }
}
