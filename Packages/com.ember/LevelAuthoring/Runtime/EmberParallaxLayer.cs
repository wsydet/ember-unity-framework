using UnityEngine;

namespace Ember.LevelAuthoring
{
    /// <summary>0为世界固定，1为随镜头位移。显式绑定实际相机，不创建或控制相机。</summary>
    public sealed class EmberParallaxLayer : MonoBehaviour
    {
        #region 编辑器面板参数
        [SerializeField] private Vector2 _factor;
        [SerializeField] private Vector3 _baseline;
        [SerializeField] private Camera _camera;
        #endregion
        // --------------------------------------------------------
        #region 内部参数
        private Camera _bound;
        private Camera _runtimeCamera;
        private Vector3 _reference;
        public Vector2 Factor => _factor;
        public Vector3 Baseline => _baseline;
        public Camera ReferenceCamera => _camera;
        #endregion
        // --------------------------------------------------------
        #region 生命周期
        private void OnEnable() { _bound = null; }
        private void LateUpdate()
        {
            var camera = _runtimeCamera != null ? _runtimeCamera : _camera;
            if (camera == null) return;
            if (_bound != camera) { _bound = camera; _reference = camera.transform.position; }
            transform.position = Evaluate(camera.transform.position, _reference);
        }
        private void OnDisable() { transform.position = _baseline; _bound = null; }
        #endregion
        // --------------------------------------------------------
        #region 外部方法
        public void Configure(Camera camera, Vector2 factor, Vector3 baseline)
        { _camera = camera; _factor = factor; _baseline = baseline; _bound = null; }
        public Vector3 Evaluate(Vector3 cameraPosition, Vector3 reference)
        { var delta = cameraPosition - reference; return _baseline + new Vector3(delta.x * _factor.x, delta.y * _factor.y, 0); }
        public void RebaseCamera() { _bound = null; }
        public void BindRuntimeCamera(Camera camera) { _runtimeCamera = camera; _bound = null; transform.position = _baseline; }
        #endregion
    }
}
