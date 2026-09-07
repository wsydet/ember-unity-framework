// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System;

using Ember.Basic;

using Unity.Cinemachine;

using UnityEngine;

namespace Ember.SceneUI.Integration
{
    /// <summary>
    /// 在指定 CinemachineBrain 完成 Scene Camera 更新后通知 SceneUI Context。
    /// </summary>
    public sealed class CinemachineSceneUICameraUpdateSource : ISceneUICameraUpdateSource, IDisposable
    {
        #region 内部参数

        private readonly Camera _sceneCamera;
        private event Action CameraUpdated;
        private bool _isListening;

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public CinemachineSceneUICameraUpdateSource(Camera sceneCamera)
        {
            _sceneCamera = sceneCamera;
        }

        public void Subscribe(Action onCameraUpdated)
        {
            if (onCameraUpdated == null) return;

            CameraUpdated += onCameraUpdated;
            if (_isListening) return;

            CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
            _isListening = true;
        }

        public void Unsubscribe(Action onCameraUpdated)
        {
            CameraUpdated -= onCameraUpdated;
            if (CameraUpdated != null || !_isListening) return;

            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
            _isListening = false;
        }

        public void Dispose()
        {
            CameraUpdated = null;
            if (!_isListening) return;

            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
            _isListening = false;
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        [NoGC]
        private void OnCameraUpdated(CinemachineBrain brain)
        {
            if (!brain || !_sceneCamera || brain.OutputCamera != _sceneCamera)
                return;

            CameraUpdated?.Invoke();
        }

        #endregion
    }
}
