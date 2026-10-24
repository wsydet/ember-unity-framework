using System;
using UnityEngine;

namespace Game.Entity
{
    [DisallowMultipleComponent]
    public sealed class EntityFacing2D : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private Transform _visual;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        public bool FacesRight { get; private set; } = true;
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void Configure(Transform visual)
        {
            if (visual != null && (visual == transform || !visual.IsChildOf(transform)))
                throw new ArgumentException("朝向只翻转独立表现子节点，不翻转实体根及物理。");
            _visual = visual;
        }
        public void SetFacing(bool right)
        {
            FacesRight = right;
            if (_visual == null) return;
            var scale = _visual.localScale;
            scale.x = Mathf.Abs(scale.x) * (right ? 1 : -1);
            _visual.localScale = scale;
        }
        #endregion
    }
}
