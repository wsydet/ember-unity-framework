// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using Ember.Basic;

using UnityEngine;

namespace Ember.SceneUI
{
    /// <summary>跟随 Unity Transform 的场景 UI Anchor。</summary>
    public sealed class TransformSceneUIAnchor : ISceneUIAnchor
    {
        #region 内部参数

        private Transform _target;

        /// <summary>当前跟随目标。</summary>
        public Transform Target => _target;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public TransformSceneUIAnchor(Transform target)
        {
            _target = target;
        }

        /// <summary>替换跟随目标。调用方随后应标记对应 Entry 空间脏。</summary>
        [NoGC]
        public void SetTarget(Transform target)
        {
            _target = target;
        }

        [NoGC]
        public bool TryGetWorldPosition(out Vector3 worldPosition)
        {
            if (!_target)
            {
                worldPosition = default;
                return false;
            }

            worldPosition = _target.position;
            return SceneUIMath.IsFinite(worldPosition);
        }

        #endregion
    }

    /// <summary>由逻辑系统直接推送世界坐标的场景 UI Anchor。</summary>
    public sealed class WorldPositionSceneUIAnchor : ISceneUIAnchor
    {
        #region 内部参数

        private Vector3 _worldPosition;
        private bool _hasPosition;

        /// <summary>是否已经收到过有效世界坐标。</summary>
        public bool HasPosition => _hasPosition;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public WorldPositionSceneUIAnchor()
        {
        }

        public WorldPositionSceneUIAnchor(Vector3 worldPosition)
        {
            SetWorldPosition(worldPosition);
        }

        /// <summary>保存最新世界坐标。调用方随后应标记对应 Entry 空间脏。</summary>
        [NoGC]
        public bool SetWorldPosition(Vector3 worldPosition)
        {
            if (!SceneUIMath.IsFinite(worldPosition))
                return false;

            _worldPosition = worldPosition;
            _hasPosition = true;
            return true;
        }

        /// <summary>使当前坐标失效。</summary>
        [NoGC]
        public void Clear()
        {
            _hasPosition = false;
        }

        [NoGC]
        public bool TryGetWorldPosition(out Vector3 worldPosition)
        {
            worldPosition = _worldPosition;
            return _hasPosition && SceneUIMath.IsFinite(worldPosition);
        }

        #endregion
    }
}
