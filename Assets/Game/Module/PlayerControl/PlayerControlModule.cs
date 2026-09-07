using Ember.Basic;
using Ember.Camera;
using Ember.Core;
using Ember.Input;
using Ember.UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game.Module
{
    /// <summary>
    /// 玩家操作模块：消费 EmberInputManager 的玩家输入，并将其转换为场景操作。
    /// 当前负责移动 GameplayScene 的 LookAt 目标，以及调整玩法正交相机的视野大小。
    /// </summary>
    [EmberModule(ModulePhase.Gameplay)]
    public sealed class PlayerControlModule : EmberSingleton<PlayerControlModule>, IEmberModule, IEmberUpdate
    {
        #region 内部参数

        private const string TAG = LogTags.Game + "." + nameof(PlayerControlModule);
        private const string PLAYER_MAP = "Player";
        private const string MOVE_ACTION = "Move";
        private const string POINTER_POSITION_ACTION = "PointerPosition";
        private const string CAMERA_DRAG_ACTION = "CameraDrag";
        private const string ZOOM_ACTION = "Zoom";
        private const float INPUT_EPSILON = 0.0001f;
        private const float DRAG_RAYCAST_DISTANCE = 10000f;

        private Transform _lookAt;
        private CinemachineCamera _gameplayCamera;
        private PlayerControlBounds _movementBounds;
        private PlayerControlSettings _settings;
        private InputActionAsset _inputActions;
        private InputAction _moveAction;
        private InputAction _pointerPositionAction;
        private InputAction _cameraDragAction;
        private InputAction _zoomAction;
        private LayerMask _dragRaycastLayers;
        private Plane _dragPlane;
        private Vector3 _dragAnchorWorld;
        private bool _blockPointerOverUI;
        private bool _moduleActive;
        private bool _inputReady;
        private bool _acceptingInput;
        private bool _isPointerDragging;
        private System.IDisposable _loadingFadeInSub;
        private System.IDisposable _loadingFadeOutSub;

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        private void TryPrepareInput()
        {
            _inputReady = false;
            EndPointerDrag();
            ClearActions();

            if (!ValidateSceneBinding())
                return;

            var inputManager = EmberInputManager.Instance;
            if (!inputManager.IsInitialized)
                inputManager.Init(_inputActions, PLAYER_MAP);
            else if (inputManager.ActionAsset != _inputActions)
            {
                EmberDebug.LogError(TAG, "EmberInputManager is already using a different InputActionAsset.");
                return;
            }
            else
                inputManager.SwitchMap(PLAYER_MAP);

            if (!inputManager.IsInitialized) return;

            _moveAction = inputManager.GetAction(PLAYER_MAP, MOVE_ACTION);
            _pointerPositionAction = inputManager.GetAction(PLAYER_MAP, POINTER_POSITION_ACTION);
            _cameraDragAction = inputManager.GetAction(PLAYER_MAP, CAMERA_DRAG_ACTION);
            _zoomAction = inputManager.GetAction(PLAYER_MAP, ZOOM_ACTION);
            _inputReady = _moveAction != null
                          && _pointerPositionAction != null
                          && _cameraDragAction != null
                          && _zoomAction != null;

            if (!_inputReady)
                EmberDebug.LogError(
                    TAG,
                    "Player map requires Move, PointerPosition, CameraDrag and Zoom actions.");
        }

        private bool ValidateSceneBinding()
        {
            bool valid = true;

            if (_lookAt == null)
            {
                EmberDebug.LogError(TAG, "PlayerControlSceneBinding is missing LookAt.");
                valid = false;
            }

            if (_gameplayCamera == null)
            {
                EmberDebug.LogError(TAG, "PlayerControlSceneBinding is missing CinemachineCamera.");
                valid = false;
            }

            if (_movementBounds == null)
            {
                EmberDebug.LogError(TAG, "PlayerControlSceneBinding is missing movement bounds.");
                valid = false;
            }

            if (_settings == null)
            {
                EmberDebug.LogError(TAG, "PlayerControlSceneBinding is missing PlayerControlSettings.");
                valid = false;
            }

            if (_inputActions == null)
            {
                EmberDebug.LogError(TAG, "PlayerControlSceneBinding is missing InputActionAsset.");
                valid = false;
            }

            return valid;
        }

        private void OnLoadingFadeInStart()
        {
            if (!_moduleActive) return;

            _acceptingInput = false;
            EndPointerDrag();
        }

        private void OnLoadingFadeOutComplete()
        {
            if (!_moduleActive) return;

            _acceptingInput = true;
            EmberDebug.Log(TAG, "Loading fully exited; player input enabled.");
        }

        private void MoveByKeyboard(Vector2 input)
        {
            GetPlanarAxes(out Vector3 right, out Vector3 forward);
            Vector3 direction = right * input.x + forward * input.y;
            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            MoveLookAtTo(
                _lookAt.position
                + direction * (_settings.KeyboardMoveSpeed * Time.deltaTime));
        }

        private void UpdatePointerDrag(bool pointerBlocked)
        {
            if (_isPointerDragging)
            {
                if (!_cameraDragAction.IsPressed())
                {
                    EndPointerDrag();
                    return;
                }

                Vector2 pointerPosition = _pointerPositionAction.ReadValue<Vector2>();
                MoveByPointerDrag(pointerPosition);
                return;
            }

            if (pointerBlocked || !_cameraDragAction.WasPressedThisFrame())
                return;

            Vector2 pressPosition = _pointerPositionAction.ReadValue<Vector2>();
            BeginPointerDrag(pressPosition);
        }

        private void BeginPointerDrag(Vector2 pointerPosition)
        {
            UnityEngine.Camera sceneCamera = ResolveSceneCamera();
            if (sceneCamera == null)
            {
                EmberDebug.LogError(TAG, "Cannot begin camera drag: EmberCameraManager.MainCamera is missing.");
                return;
            }

            Ray pointerRay = sceneCamera.ScreenPointToRay(
                new Vector3(pointerPosition.x, pointerPosition.y, 0f));

            if (Physics.Raycast(
                    pointerRay,
                    out RaycastHit hit,
                    DRAG_RAYCAST_DISTANCE,
                    _dragRaycastLayers,
                    QueryTriggerInteraction.Ignore))
            {
                _dragPlane = new Plane(Vector3.up, hit.point);
            }
            else
            {
                _dragPlane = new Plane(Vector3.up, _lookAt.position);
            }

            if (!_dragPlane.Raycast(pointerRay, out float enter))
                return;

            _dragAnchorWorld = pointerRay.GetPoint(enter);
            _isPointerDragging = true;
        }

        private void MoveByPointerDrag(Vector2 pointerPosition)
        {
            UnityEngine.Camera sceneCamera = ResolveSceneCamera();
            if (sceneCamera == null)
            {
                EndPointerDrag();
                return;
            }

            Ray pointerRay = sceneCamera.ScreenPointToRay(
                new Vector3(pointerPosition.x, pointerPosition.y, 0f));
            if (!_dragPlane.Raycast(pointerRay, out float enter))
                return;

            Vector3 pointerWorld = pointerRay.GetPoint(enter);
            Vector3 translation = _dragAnchorWorld - pointerWorld;
            translation.y = 0f;

            if (translation.sqrMagnitude > INPUT_EPSILON)
            {
                Vector3 targetPosition = _lookAt.position + translation;
                Vector3 actualPosition = MoveLookAtTo(targetPosition);

                // 丢弃边界拒绝的拖动残差，避免反向拖动时出现空行程。
                _dragAnchorWorld += actualPosition - targetPosition;
            }
        }

        private void EndPointerDrag()
        {
            _isPointerDragging = false;
            _dragAnchorWorld = default;
            _dragPlane = default;
        }

        private void UpdateZoom(bool pointerBlocked)
        {
            ClampOrthographicSize();

            if (pointerBlocked || _isPointerDragging)
                return;

            float zoomInput = _zoomAction.ReadValue<float>();
            if (Mathf.Abs(zoomInput) <= INPUT_EPSILON)
                return;

            float targetSize = Mathf.Clamp(
                _gameplayCamera.Lens.OrthographicSize
                - zoomInput * _settings.OrthographicSizePerScroll,
                _settings.MinOrthographicSize,
                _settings.MaxOrthographicSize);
            SetOrthographicSize(targetSize);
        }

        private void ClampOrthographicSize()
        {
            float currentSize = _gameplayCamera.Lens.OrthographicSize;
            float clampedSize = Mathf.Clamp(
                currentSize,
                _settings.MinOrthographicSize,
                _settings.MaxOrthographicSize);
            if (Mathf.Approximately(currentSize, clampedSize))
                return;

            SetOrthographicSize(clampedSize);
        }

        private void SetOrthographicSize(float size)
        {
            LensSettings lens = _gameplayCamera.Lens;
            lens.OrthographicSize = size;
            _gameplayCamera.Lens = lens;
        }

        private Vector3 MoveLookAtTo(Vector3 targetPosition)
        {
            Vector3 currentPosition = _lookAt.position;
            if (_movementBounds.TryConstrainMovement(
                    currentPosition,
                    targetPosition,
                    out Vector3 constrainedPosition))
            {
                _lookAt.position = constrainedPosition;
            }

            return _lookAt.position;
        }

        private bool CorrectInvalidLookAtPosition()
        {
            Vector3 currentPosition = _lookAt.position;
            if (!_movementBounds.TryClampPosition(currentPosition, out Vector3 clampedPosition))
                return false;

            _lookAt.position = clampedPosition;
            return (clampedPosition - currentPosition).sqrMagnitude
                   > INPUT_EPSILON * INPUT_EPSILON;
        }

        private void GetPlanarAxes(out Vector3 right, out Vector3 forward)
        {
            UnityEngine.Camera sceneCamera = ResolveSceneCamera();
            if (sceneCamera == null)
            {
                right = Vector3.right;
                forward = Vector3.forward;
                return;
            }

            forward = Vector3.ProjectOnPlane(sceneCamera.transform.forward, Vector3.up);
            if (forward.sqrMagnitude < INPUT_EPSILON)
                forward = Vector3.forward;
            else
                forward.Normalize();

            right = Vector3.Cross(Vector3.up, forward).normalized;
        }

        private static UnityEngine.Camera ResolveSceneCamera()
        {
            return EmberCameraManager.IsValid
                ? EmberCameraManager.Instance.MainCamera
                : null;
        }

        private void ClearActions()
        {
            _moveAction = null;
            _pointerPositionAction = null;
            _cameraDragAction = null;
            _zoomAction = null;
        }

        private void DisposeLoadingSubscriptions()
        {
            _loadingFadeInSub?.Dispose();
            _loadingFadeInSub = null;
            _loadingFadeOutSub?.Dispose();
            _loadingFadeOutSub = null;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        void IEmberModule.OnInit()
        {
            _moduleActive = true;
            _acceptingInput = false;
            DisposeLoadingSubscriptions();
            _loadingFadeInSub = EmberEventBus.Subscribe(
                EUIEvents.LoadingFadeInStart,
                OnLoadingFadeInStart);
            _loadingFadeOutSub = EmberEventBus.Subscribe(
                EUIEvents.LoadingFadeOutComplete,
                OnLoadingFadeOutComplete);
            TryPrepareInput();
            EmberDebug.LogInit(TAG, "PlayerControlModule initialized; awaiting LoadingFadeOutComplete.");
        }

        void IEmberModule.OnDestroy()
        {
            _moduleActive = false;
            _acceptingInput = false;
            _inputReady = false;
            EndPointerDrag();
            DisposeLoadingSubscriptions();
            ClearActions();

            if (EmberInputManager.IsValid && EmberInputManager.Instance.IsInitialized)
                EmberInputManager.Instance.SwitchMap("UI");

            EmberDebug.LogCleanup(TAG, "PlayerControlModule destroyed.");
        }

        void IEmberModule.ResetModuleData()
        {
            _acceptingInput = false;
            _inputReady = false;
            EndPointerDrag();
            DisposeLoadingSubscriptions();
            ClearActions();
        }

        void IEmberUpdate.Update()
        {
            if (!_moduleActive || !_acceptingInput || !_inputReady || _lookAt == null)
            {
                EndPointerDrag();
                return;
            }

            // 运行时修改区域尺寸、位置或解锁状态后，即使没有玩家输入也立即纠正 LookAt。
            // 管理性纠偏可能改变当前区域，因此同时结束旧的鼠标捕获。
            if (CorrectInvalidLookAtPosition())
                EndPointerDrag();

            bool pointerBlocked = _blockPointerOverUI
                                  && EventSystem.current != null
                                  && EventSystem.current.IsPointerOverGameObject();

            UpdatePointerDrag(pointerBlocked);

            if (!_isPointerDragging)
            {
                Vector2 move = _moveAction.ReadValue<Vector2>();
                if (move.sqrMagnitude > 0f)
                    MoveByKeyboard(move);
            }

            UpdateZoom(pointerBlocked);
        }

        /// <summary>
        /// 由场景绑定组件注入操作目标与配置。只配置玩家操作，不负责相机注册。
        /// </summary>
        public void Bind(
            Transform lookAt,
            CinemachineCamera gameplayCamera,
            PlayerControlBounds movementBounds,
            PlayerControlSettings settings,
            InputActionAsset inputActions,
            LayerMask dragRaycastLayers,
            bool blockPointerOverUI)
        {
            _lookAt = lookAt;
            _gameplayCamera = gameplayCamera;
            _movementBounds = movementBounds;
            _settings = settings;
            _inputActions = inputActions;
            _dragRaycastLayers = dragRaycastLayers;
            _blockPointerOverUI = blockPointerOverUI;

            if (_moduleActive)
                TryPrepareInput();
        }

        /// <summary>当前场景提供的可移动区域；运行时区域解锁逻辑可以通过此入口访问。</summary>
        public PlayerControlBounds MovementBounds => _movementBounds;

        /// <summary>场景卸载时解除当前 LookAt 绑定。</summary>
        public void Unbind(Transform lookAt)
        {
            if (_lookAt != lookAt) return;

            EndPointerDrag();
            _lookAt = null;
            _gameplayCamera = null;
            _movementBounds = null;
            _settings = null;
            _inputActions = null;
            _inputReady = false;
            ClearActions();
        }

        #endregion
    }
}
