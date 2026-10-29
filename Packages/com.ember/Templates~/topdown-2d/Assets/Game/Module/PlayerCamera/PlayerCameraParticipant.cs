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
    /// <summary>场景拥有镜头绑定；玩家程序集不依赖 Cinemachine。</summary>
    public sealed class PlayerCameraParticipant : GameSceneParticipant
    {
        #region 编辑器面板参数
        [SerializeField] private PlayerController _player;
        [SerializeField] private CinemachineCamera _camera;
        [SerializeField] private EmberCameraRegistration _registration;
        #endregion

        // --------------------------------------------------------
        #region 内部方法
        private void Validate(EmberCameraManager manager)
        {
            if (_player == null || _player.gameObject.scene != gameObject.scene || _camera == null || _registration == null ||
                _registration.gameObject != _camera.gameObject || string.IsNullOrWhiteSpace(_registration.CameraKey))
                throw new InvalidOperationException("俯视镜头的玩家、相机或注册器接线缺失。");
            var follow = _camera.GetComponent<CinemachineFollow>();
            if (follow == null || follow.TrackerSettings.BindingMode != BindingMode.WorldSpace ||
                _camera.GetCinemachineComponent(CinemachineCore.Stage.Aim) != null ||
                Quaternion.Angle(_camera.transform.rotation, Quaternion.identity) > .01f ||
                manager.MainCamera == null || !manager.MainCamera.orthographic || manager.Brain == null ||
                (manager.Brain.ChannelMask & _camera.OutputChannel) == 0 || _camera.Lens.OrthographicSize <= 0)
                throw new InvalidOperationException("俯视镜头需要正交主相机、匹配 Brain 和固定方向 WorldSpace Follow。");
            if (manager.IsLocked || manager.IsOverrideMode)
                throw new InvalidOperationException("镜头控制权已被占用。");
        }

        private sealed class Result : IGameScenePreparedParticipant
        {
            private readonly PlayerCameraParticipant _owner;
            private readonly PlayerModule _module;
            private readonly EmberCameraManager _manager;
            private bool _started, _disposed;
            internal Result(PlayerCameraParticipant owner, PlayerModule module, EmberCameraManager manager)
            { _owner = owner; _module = module; _manager = manager; }
            public void Start()
            {
                if (_started || _disposed || _module.HasSession) throw new InvalidOperationException("镜头必须先于玩家交接。");
                _owner.Validate(_manager);
                _owner._registration.RegisterCamera();
                _module.SessionCommitted += Committed; _module.SessionReleased += Released;
                _started = true;
            }
            private void Committed(PlayerController player)
            {
                if (_disposed || player != _owner._player) return;
                _owner._camera.Follow = player.transform;
                player.SetAimCamera(_manager.MainCamera);
                _owner._camera.PreviousStateIsValid = false;
                _manager.Switch(_owner._registration.CameraKey);
                _manager.Brain.ResetState();
            }
            private void Released(PlayerController player)
            {
                if (player != _owner._player || _owner._camera == null) return;
                _owner._camera.Follow = null; _owner._camera.PreviousStateIsValid = false;
            }
            public void Dispose()
            {
                if (_disposed) return; _disposed = true;
                _module.SessionCommitted -= Committed; _module.SessionReleased -= Released;
                Released(_owner._player);
                if (_started && _owner._registration != null) _owner._registration.UnregisterCamera();
            }
        }
        #endregion

        // --------------------------------------------------------
        #region 外部方法
        public override UniTask<IGameScenePreparedParticipant> PrepareAsync(GameScenePreparationContext context)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            if (context.Scene != gameObject.scene || !EmberModuleCollector.Instance.TryGetModule<PlayerModule>(out var player))
                throw new InvalidOperationException("镜头场景或玩家模块无效。");
            var manager = EmberCameraManager.Instance; Validate(manager);
            return UniTask.FromResult<IGameScenePreparedParticipant>(new Result(this, player, manager));
        }
        #endregion
    }
}
