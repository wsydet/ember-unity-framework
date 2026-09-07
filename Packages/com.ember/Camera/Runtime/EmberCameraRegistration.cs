using Ember.Basic;
using Unity.Cinemachine;
using UnityEngine;

namespace Ember.Camera
{
    /// <summary>
    /// 场景虚拟相机注册器。
    /// 挂到 CinemachineCamera 所在对象后，由相机模块负责注册、切换和注销，
    /// 业务模块无需持有 EmberCameraManager 的注册职责。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineCamera))]
    public sealed class EmberCameraRegistration : MonoBehaviour
    {
        private const string TAG = LogTags.CoreCameraManager;

        [SerializeField] private string _cameraKey = "Camera";
        [SerializeField] private bool _activateOnAwake;
        [SerializeField] private bool _forceActivation;

        private CinemachineCamera _camera;
        private bool _registered;

        /// <summary>注册到 EmberCameraManager 使用的唯一键。</summary>
        public string CameraKey => _cameraKey;

        private void Awake()
        {
            _camera = GetComponent<CinemachineCamera>();
            RegisterCamera();
        }

        private void OnDestroy()
        {
            UnregisterCamera();
        }

        /// <summary>注册当前对象上的 CinemachineCamera，并按配置决定是否立即切换。</summary>
        public void RegisterCamera()
        {
            if (_registered) return;
            if (_camera == null)
                _camera = GetComponent<CinemachineCamera>();

            if (_camera == null || string.IsNullOrWhiteSpace(_cameraKey))
            {
                EmberDebug.LogError(TAG, $"Camera registration invalid on '{name}'.");
                return;
            }

            var manager = EmberCameraManager.Instance;
            manager.Register(_cameraKey, _camera);
            _registered = true;

            if (_activateOnAwake)
                manager.Switch(_cameraKey, _forceActivation);
        }

        /// <summary>仅当该键仍指向当前相机时注销，避免误删后来注册的替代相机。</summary>
        public void UnregisterCamera()
        {
            if (!_registered || !EmberCameraManager.IsValid) return;

            var manager = EmberCameraManager.Instance;
            if (manager.GetCamera(_cameraKey) == _camera)
                manager.Unregister(_cameraKey);

            _registered = false;
        }
    }
}
