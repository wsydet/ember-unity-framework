// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using Ember.Basic;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ember.SceneUI.Integration
{
    /// <summary>
    /// 使用相机所在 PhysicsScene 的单次 Raycast 实现遮挡检测。
    /// 正交相机使用穿过目标屏幕位置的平行射线，透视相机使用相机位置射线。
    /// </summary>
    public sealed class PhysicsSceneUIOcclusionTester : ISceneUIOcclusionTester
    {
        #region 内部参数

        private readonly int _layerMask;
        private readonly QueryTriggerInteraction _queryTriggerInteraction;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public PhysicsSceneUIOcclusionTester(
            int layerMask,
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            _layerMask = layerMask;
            _queryTriggerInteraction = queryTriggerInteraction;
        }

        [NoGC]
        public bool TryTestOcclusion(
            Camera sceneCamera,
            Vector3 targetWorldPosition,
            in SceneUIOcclusionPolicy policy,
            out bool occluded)
        {
            occluded = false;
            if (!sceneCamera
                || !SceneUIMath.IsFinite(targetWorldPosition))
                return false;

            PhysicsScene physicsScene = sceneCamera.gameObject.scene.GetPhysicsScene();
            if (!physicsScene.IsValid())
                return false;

            float targetRadius = Mathf.Max(0f, policy.TargetRadius);
            Vector3 origin;
            Vector3 direction;
            float distance;

            if (sceneCamera.orthographic)
            {
                direction = sceneCamera.transform.forward;
                float depth = Vector3.Dot(
                    targetWorldPosition - sceneCamera.transform.position,
                    direction);
                if (!SceneUIMath.IsFinite(depth) || depth <= sceneCamera.nearClipPlane)
                    return true;

                origin = targetWorldPosition
                    - direction * (depth - sceneCamera.nearClipPlane);
                distance = depth - sceneCamera.nearClipPlane - targetRadius;
            }
            else
            {
                origin = sceneCamera.transform.position;
                Vector3 offset = targetWorldPosition - origin;
                float targetDistance = offset.magnitude;
                if (!SceneUIMath.IsFinite(targetDistance) || targetDistance <= 0.0001f)
                    return true;

                direction = offset / targetDistance;
                distance = targetDistance - targetRadius;
            }

            if (distance <= 0f)
                return true;

            occluded = physicsScene.Raycast(
                origin,
                direction,
                out _,
                distance,
                _layerMask,
                _queryTriggerInteraction);
            return true;
        }

        #endregion
    }
}
