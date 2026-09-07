using Ember.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using Ember.Basic;

namespace Ember.Input
{
    /// <summary>
    /// 输入管理器 —— Unity Input System 的框架封装。
    ///
    /// 核心设计：
    /// - 持有 InputActionAsset，支持运行时切换 Action Map
    /// - 将输入事件桥接到 EmberEventBus（可选）
    /// - 广播生命周期事件（InputReady / InputShutdown）
    ///
    /// 使用方式：
    /// <code>
    /// // 初始化
    /// EmberInputManager.Instance.Init(inputActions);
    ///
    /// // 切换操作模式
    /// EmberInputManager.Instance.SwitchMap("Gameplay");
    /// EmberInputManager.Instance.SwitchMap("UI");
    ///
    /// // 获取输入值
    /// var move = EmberInputManager.Instance.GetAxis("Move");
    /// </code>
    /// </summary>
    [EmberInitOrder(EmberInitOrderAttribute.Input)]
    public class EmberInputManager : EmberSingleton<EmberInputManager>, IEmberManager
    {
        private const string TAG = LogTags.InputManager;
        #region 参数

        private PlayerInput _playerInput;
        private InputActionAsset _actionAsset;
        private IEmberInputRebindingService _rebindingService;
        private string _currentMap;
        private bool _initialized;

        #endregion

        // ============================================================

        #region 外部方法

        // ======== 初始化 ========

        /// <summary>
        /// 初始化输入管理器。
        /// 如果 GameObject 上没有 PlayerInput 组件则自动添加。
        /// </summary>
        /// <param name="actionAsset">InputActionAsset 资源</param>
        /// <param name="defaultMap">默认启用的 Action Map（可选）</param>
        public void Init(InputActionAsset actionAsset, string defaultMap = null)
        {
            if (_initialized) return;

            if (actionAsset == null)
            {
                EmberDebug.LogError(TAG, "InputActionAsset 不能为空，InputManager 初始化失败。");
                return;
            }

            var host = GameLauncher.Instance.InputHost;
            if (host == null)
            {
                EmberDebug.LogError(TAG, "GameBoot 下缺少 InputHost 子节点，InputManager 无法初始化。");
                return;
            }

            _actionAsset = actionAsset;
            _playerInput = host.GetComponent<PlayerInput>();
            if (_playerInput == null)
                _playerInput = host.AddComponent<PlayerInput>();

            _playerInput.actions = _actionAsset;
            _playerInput.notificationBehavior = PlayerNotifications.InvokeUnityEvents;

            // SwitchMap 只接受已完成初始化的管理器，因此必须先更新状态。
            _initialized = true;

            if (!string.IsNullOrEmpty(defaultMap))
            {
                SwitchMap(defaultMap);
            }

            EmberEventBus.OnNext(EmberBroadcastEvent.InputReady);
        }

        // ======== Action Map 切换 ========

        /// <summary>
        /// 切换到指定 Action Map。先禁用当前 Map，再启用目标 Map。
        /// </summary>
        /// <param name="mapName">Action Map 名称（如 "Gameplay", "UI"）</param>
        public void SwitchMap(string mapName)
        {
            if (!_initialized || _actionAsset == null) return;

            // 禁用当前
            if (!string.IsNullOrEmpty(_currentMap))
            {
                var cur = _actionAsset.FindActionMap(_currentMap);
                if (cur != null) cur.Disable();
            }

            // 启用目标
            var target = _actionAsset.FindActionMap(mapName);
            if (target != null)
            {
                target.Enable();
                _currentMap = mapName;
                EmberDebug.Log(TAG, $"Input map switched to: {mapName}");
            }
            else
            {
                EmberDebug.LogWarning(TAG, $"Input map '{mapName}' not found.");
            }
        }

        // ======== 输入读取 ========

        /// <summary>
        /// 读取 Vector2 类型的输入值（如 Move、Look）。
        /// </summary>
        public Vector2 GetAxis(string actionName)
        {
            if (!_initialized || _actionAsset == null) return Vector2.zero;

            var action = _actionAsset.FindAction(actionName);
            return action?.ReadValue<Vector2>() ?? Vector2.zero;
        }

        /// <summary>
        /// 读取 float 类型的输入值（如水平轴、垂直轴）。
        /// </summary>
        public float GetFloat(string actionName)
        {
            if (!_initialized || _actionAsset == null) return 0f;

            var action = _actionAsset.FindAction(actionName);
            return action?.ReadValue<float>() ?? 0f;
        }

        /// <summary>
        /// 检查按钮是否被按下（本帧触发）。
        /// </summary>
        public bool IsPressed(string actionName)
        {
            if (!_initialized || _actionAsset == null) return false;

            var action = _actionAsset.FindAction(actionName);
            return action?.WasPressedThisFrame() ?? false;
        }

        /// <summary>
        /// 获取 InputAction 引用，用于手动订阅 performed/canceled 事件。
        /// </summary>
        public InputAction GetAction(string actionName)
        {
            return _actionAsset?.FindAction(actionName);
        }

        /// <summary>
        /// 获取指定 Action Map 下的 InputAction，避免不同 Map 中存在同名 Action 时产生歧义。
        /// </summary>
        public InputAction GetAction(string actionMapName, string actionName)
        {
            if (_actionAsset == null || string.IsNullOrEmpty(actionMapName)
                || string.IsNullOrEmpty(actionName))
                return null;

            return _actionAsset.FindActionMap(actionMapName)?.FindAction(actionName);
        }

        /// <summary>
        /// 注册玩家输入重绑定服务。Ember 当前不提供默认实现，服务生命周期由注册方管理。
        /// 传入 null 可以移除当前服务。
        /// </summary>
        public void SetRebindingService(IEmberInputRebindingService service)
        {
            _rebindingService = service;
        }

        /// <summary>
        /// 当前激活的 Action Map 名称。
        /// </summary>
        public string CurrentMap => _currentMap;

        /// <summary>当前使用的 InputActionAsset，未完成输入初始化时可能为 null。</summary>
        public InputActionAsset ActionAsset => _actionAsset;

        /// <summary>输入管理器是否已绑定 InputActionAsset 并完成初始化。</summary>
        public bool IsInitialized => _initialized;

        /// <summary>已注册的玩家输入重绑定服务；未接入实现时为 null。</summary>
        public IEmberInputRebindingService RebindingService => _rebindingService;

        // ======== IEmberManager ========

        /// <summary>
        /// 由 ManagerCollector 自动调用的无参初始化。
        /// InputManager 需要 InputActionAsset 才能完整工作，
        /// 在此之前仅做最小准备；完整初始化请调用 <see cref="Init(InputActionAsset, string)"/>。
        /// </summary>
        void IEmberManager.Init()
        {
            if (_initialized) return;

            if (GameLauncher.Instance.InputHost == null)
            {
                EmberDebug.LogError(TAG, "GameBoot 下缺少 InputHost 子节点，InputManager 无法初始化。");
                return;
            }

            EmberDebug.LogInit(TAG, "EmberInputManager basic init (awaiting InputActionAsset).");
        }

        /// <summary>
        /// 由 ManagerCollector 逆序调用的销毁逻辑。
        /// </summary>
        void IEmberManager.Destroy()
        {
            DestroyInternal();
        }

        #endregion

        // ============================================================

        #region 内部方法

        /// <summary>
        /// EmberSingleton 销毁钩子。
        /// </summary>
        protected override void OnDestroy()
        {
            DestroyInternal();
        }

        /// <summary>
        /// 共享清理逻辑：广播 InputShutdown、禁用 PlayerInput、销毁组件、重置状态。
        /// InputHost 由 GameBoot 预置，不在此销毁。
        /// </summary>
        private void DestroyInternal()
        {
            EmberEventBus.OnNext(EmberBroadcastEvent.InputShutdown);
            _playerInput?.actions?.Disable();
            if (_playerInput != null)
            {
                UnityEngine.Object.Destroy(_playerInput);
                _playerInput = null;
            }
            _rebindingService = null;
            _actionAsset = null;
            _currentMap = null;
            _initialized = false;
        }

        #endregion
    }
}
