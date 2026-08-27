// Copyright (c) 2026 Ember Unity Framework. All rights reserved.
// Package: com.ember

using UnityEngine;

namespace Ember.UIExtension
{
    /// <summary>
    /// Transform 属性复制器。
    /// 在编辑模式和运行模式下将自身 Transform 同步到目标 Transform 的 position/rotation/scale。
    /// 常用于 UI 调试时将一个 GameObject 对齐到另一个。
    /// </summary>
    [ExecuteAlways]
    public class TransformCopier : MonoBehaviour
    {
        #region 编辑器面板参数

        [SerializeField]
        [Tooltip("要复制 Transform 属性的目标对象")]
        private Transform _copied;

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private Transform _selfTransform;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void Update()
        {
            if (_copied == null)
                return;

            if (_selfTransform == null)
                _selfTransform = transform;

            if (_selfTransform.position != _copied.position)
                _selfTransform.position = _copied.position;

            if (_selfTransform.rotation != _copied.rotation)
                _selfTransform.rotation = _copied.rotation;

            if (_selfTransform.parent == null)
            {
                if (_selfTransform.localScale != _copied.lossyScale)
                    _selfTransform.localScale = _copied.lossyScale;
            }
            else
            {
                var parentLossyScale = _selfTransform.parent.lossyScale;
                var targetScale = Vector3.zero;

                if (parentLossyScale.x != 0)
                    targetScale.x = _copied.lossyScale.x / parentLossyScale.x;
                if (parentLossyScale.y != 0)
                    targetScale.y = _copied.lossyScale.y / parentLossyScale.y;
                if (parentLossyScale.z != 0)
                    targetScale.z = _copied.lossyScale.z / parentLossyScale.z;

                if (targetScale != _selfTransform.localScale)
                    _selfTransform.localScale = targetScale;
            }
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        /// <summary>
        /// 要复制 Transform 属性的目标对象。
        /// </summary>
        public Transform Copied
        {
            get => _copied;
            set => _copied = value;
        }

        #endregion
    }
}
