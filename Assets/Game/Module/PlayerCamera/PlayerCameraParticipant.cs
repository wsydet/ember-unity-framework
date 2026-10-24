using System;
using Cysharp.Threading.Tasks;
using Ember.Camera;
using Ember.Core;
using Game.Player;
using Game.SceneFlow;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace Game.PlayerCamera
{
    /// <summary>平台玩家镜头适配。由场景准备拥有，玩家交接后绑定；不驱动玩家或相机逐帧更新。</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCameraParticipant : GameSceneParticipant
    {
        #region 编辑器面板参数
        [SerializeField, Tooltip("同一 SceneScope 内的玩家根，禁止指定 Sprite 子对象。")]
        private PlayerSceneParticipant _player;
        [SerializeField, Tooltip("已有 CinemachineCamera；Follow 的偏移和阻尼仍在相机组件编辑。")]
        private CinemachineCamera _camera;
        [SerializeField, Tooltip("现有注册器。关闭 Activate On Awake，由成功的玩家会话激活镜头。")]
        private EmberCameraRegistration _registration;
        #endregion

        // --------------------------------------------------------
        #region 内部参数
        private PreparedCamera _prepared;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private void Validate(EmberCameraManager manager)
        {
            if (_player == null || !_player.isActiveAndEnabled || _player.gameObject.scene != gameObject.scene ||
                _player.GetComponent<PlayerController>() == null)
                throw new InvalidOperationException("玩家镜头缺少同场景的启用玩家参与者。");
            if (_camera == null || !_camera.enabled || _camera.gameObject.scene != gameObject.scene ||
                _registration == null || !_registration.enabled || _registration.gameObject != _camera.gameObject ||
                string.IsNullOrWhiteSpace(_registration.CameraKey))
                throw new InvalidOperationException("玩家镜头的 Camera / 注册器接线无效。");
            if (transform == _camera.transform || transform.IsChildOf(_camera.transform))
                throw new InvalidOperationException("相机参与者必须位于持续启用的 Scope 对象，不能随虚拟相机禁用。");
            var follow = _camera.GetComponent<CinemachineFollow>();
            if (follow == null || !follow.enabled || follow.TrackerSettings.BindingMode != BindingMode.WorldSpace ||
                _camera.GetCinemachineComponent(CinemachineCore.Stage.Aim) != null ||
                Quaternion.Angle(_camera.transform.rotation, Quaternion.identity) > 0.01f)
                throw new InvalidOperationException("2D 玩家镜头需要 WorldSpace Follow 和固定朝向，不允许 Aim 旋转组件。");
            if (manager == null || manager.MainCamera == null || manager.Brain == null || !manager.Brain.isActiveAndEnabled ||
                (manager.Brain.ChannelMask & _camera.OutputChannel) == 0 || !manager.MainCamera.orthographic ||
                _camera.Lens.OrthographicSize <= 0)
                throw new InvalidOperationException("玩家镜头缺少有效的正交主相机、Brain 或匹配输出通道。");
            var registered = manager.GetCamera(_registration.CameraKey);
            if (registered != null && registered != _camera)
                throw new InvalidOperationException("玩家镜头注册键已由其他相机占用。");
        }

        private sealed class PreparedCamera : IGameScenePreparedParticipant
        {
            private readonly PlayerCameraParticipant _owner;
            private readonly PlayerModule _playerModule;
            private readonly EmberCameraManager _manager;
            private PlayerController _bound;
            private Transform _target;
            private bool _started, _disposed;

            internal PreparedCamera(PlayerCameraParticipant owner, PlayerModule player, EmberCameraManager manager)
            { _owner = owner; _playerModule = player; _manager = manager; }

            public void Start()
            {
                if (_disposed || _started) throw new InvalidOperationException("玩家镜头候选不能重复启动。");
                _owner.Validate(_manager);
                if (_playerModule.HasSession)
                    throw new InvalidOperationException("SceneScope 必须将相机参与者排在玩家参与者之前。");
                if (_manager.IsLocked || _manager.IsOverrideMode)
                    throw new InvalidOperationException("提交前相机控制权已改变，不能激活玩家镜头。");
                _owner._registration.RegisterCamera();
                _playerModule.SessionCommitted += OnCommitted;
                _playerModule.SessionReleased += OnReleased;
                _started = true;
            }

            private void OnCommitted(PlayerController controller)
            {
                if (_disposed || controller == null || controller.gameObject != _owner._player.gameObject) return;
                _owner.Validate(_manager);
                var camera = _owner._camera;
                _bound = controller;
                _target = controller.transform;
                camera.Follow = _target;
                camera.LookAt = _target;
                // 同一 Transform 的读档/重生也必须丢弃阻尼历史；普通移动不进入这条通知。
                camera.PreviousStateIsValid = false;
                camera.UpdateCameraState(Vector3.up, -1f);
                _manager.Switch(_owner._registration.CameraKey);
                if (_manager.ActiveCamera == camera)
                    _manager.Brain.ResetState(); // 下次正常 Brain 更新直接使用已准备的构图，不混入旧镜头。
            }

            private void OnReleased(PlayerController controller)
            {
                if (!ReferenceEquals(controller, _bound)) return;
                ClearBinding();
            }

            private void ClearBinding()
            {
                if (ReferenceEquals(_bound, null)) return;
                var camera = _owner._camera;
                if (camera != null)
                {
                    if (camera.Follow == _target) camera.Follow = null;
                    if (camera.LookAt == _target) camera.LookAt = null;
                    camera.PreviousStateIsValid = false;
                }
                if (_owner._registration != null) _owner._registration.UnregisterCamera();
                _bound = null;
                _target = null;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _playerModule.SessionCommitted -= OnCommitted;
                _playerModule.SessionReleased -= OnReleased;
                ClearBinding();
                if (_owner._prepared == this) _owner._prepared = null;
            }
        }
        private UniTask<IGameScenePreparedParticipant> PrepareWithDependencies(GameScenePreparationContext context,
            PlayerModule player, EmberCameraManager manager)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            if (_prepared != null || context.Scene != gameObject.scene)
                throw new InvalidOperationException("玩家镜头准备上下文无效或已被占用。");
            Validate(manager);
            if (_player.PreparedRequestId.HasValue)
                throw new InvalidOperationException("SceneScope 必须先准备相机，再准备玩家。");
            if (manager.ActiveCamera == _camera)
                throw new InvalidOperationException("玩家镜头不能在交接前激活；请关闭注册器 Activate On Awake。");
            if (manager.IsLocked || manager.IsOverrideMode)
                throw new InvalidOperationException("相机控制权被锁定或占用，无法准备玩家镜头。");
            IGameScenePreparedParticipant result = _prepared = new PreparedCamera(this, player, manager);
            return UniTask.FromResult(result);
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public override UniTask<IGameScenePreparedParticipant> PrepareAsync(GameScenePreparationContext context)
        {
            if (!EmberModuleCollector.Instance.TryGetModule<PlayerModule>(out var player))
                throw new InvalidOperationException("玩家镜头缺少 Player 模块。");
            return PrepareWithDependencies(context, player, EmberCameraManager.Instance);
        }
        #endregion
    }
}
