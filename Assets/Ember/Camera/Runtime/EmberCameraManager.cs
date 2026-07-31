using System;
using System.Collections.Generic;
using Ember.Core;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;

namespace Ember.Camera
{
    /// <summary>
    /// 相机管理器 —— 框架级相机基础设施。
    ///
    /// 功能：
    /// - 注册/切换 Cinemachine 虚拟相机
    /// - 强制霸占堆栈（Override Stack）：对话 → Timeline → QTE 多重嵌套接管
    /// - 锁定模式：禁止任何切换（Loading 等场景）
    /// - 自动配置 CinemachineBrain 的过渡曲线（CinemachineBlenderSettings）
    ///
    /// <b>相机堆栈的使用场景：</b>
    /// 玩家自由移动（Normal）→ 进入对话（Dialogue 压栈）→ 对话中播 Timeline（Cutscene 压栈）
    /// → Timeline 结束（弹栈回 Dialogue）→ 对话结束（弹栈回 Normal）。
    ///
    /// 参考旧项目 CameraManager 的强制霸占模式。
    /// </summary>
    [EmberInitOrder(EmberInitOrderAttribute.Default)]
    public class EmberCameraManager : EmberSingleton<EmberCameraManager>, IEmberManager
    {
        private const string TAG = LogTags.CoreCameraManager;
        private const string ODIN_GROUP = "Camera Manager";

        #region 参数

        // === 基础引用 ===

        /// <summary>UI 相机（渲染 UI 层）。由 GameLauncher 注入。</summary>
        public UnityEngine.Camera UICamera { get; internal set; }

        /// <summary>主相机（渲染游戏世界）。由 GameLauncher 注入。</summary>
        public UnityEngine.Camera MainCamera { get; internal set; }

        /// <summary>MainCamera 上的 CinemachineBrain。</summary>
        public CinemachineBrain Brain { get; private set; }

        // === 相机注册 ===

        private readonly Dictionary<string, CinemachineCamera> _registry = new();

        // === 切换事件 ===

        /// <summary>相机切换后触发。</summary>
        public event Action<CinemachineCamera> OnCameraSwitched;

        // === 内部状态 ===

        private CinemachineCamera _active;
        private bool _isLocked;
        private readonly List<OverrideEntry> _overrideStack = new();

        /// <summary>当前活跃的虚拟相机。</summary>
        public CinemachineCamera ActiveCamera => _active;

        /// <summary>是否处于强制霸占模式。</summary>
        public bool IsOverrideMode => _overrideStack.Count > 0;

        /// <summary>霸占栈层数。</summary>
        public int OverrideStackCount => _overrideStack.Count;

        /// <summary>是否已锁定（拒绝一切切换）。</summary>
        public bool IsLocked => _isLocked;

        /// <summary>
        /// 强制霸占模式下的栈条目。
        /// </summary>
        private struct OverrideEntry
        {
            public string Key;
            public CinemachineCamera Camera;
        }

        #endregion

        // ============================================================
        // Editor UI (Odin)
        // ============================================================

        #region Editor UI

        [FoldoutGroup(ODIN_GROUP, Expanded = true)]

        [BoxGroup(ODIN_GROUP + "/Config", ShowLabel = false)]
        [Title("Setup", "核心引用", titleAlignment: TitleAlignments.Centered, horizontalLine: true)]
        [LabelText("转场配置")]
        [Tooltip("Cinemachine 原生 BlenderSettings 资产。\n通过 Create > Cinemachine > Blender Settings 创建。")]
        public CinemachineBlenderSettings blenderSettings;

        [FoldoutGroup(ODIN_GROUP)]
        [BoxGroup(ODIN_GROUP + "/Runtime", ShowLabel = false)]
        [Title("Runtime Info", "实时状态", titleAlignment: TitleAlignments.Centered, horizontalLine: true)]
        [ShowInInspector, ReadOnly, LabelText("活跃相机")]
        [GUIColor("@_active != null ? UnityEngine.Color.green : UnityEngine.Color.red")]
        private string ActiveCameraName => _active != null ? _active.name : "None";

        [BoxGroup(ODIN_GROUP + "/Runtime")]
        [ShowInInspector, ReadOnly, LabelText("强制霸占模式")]
        [GUIColor("@IsOverrideMode ? UnityEngine.Color.red : UnityEngine.Color.gray")]
        private bool DebugIsOverride => IsOverrideMode;

        [BoxGroup(ODIN_GROUP + "/Runtime")]
        [ShowInInspector, ReadOnly, LabelText("霸占栈层数")]
        private int DebugStackCount => _overrideStack.Count;

        [BoxGroup(ODIN_GROUP + "/Runtime")]
        [ShowInInspector, ReadOnly, LabelText("锁定状态")]
        private bool DebugIsLocked => _isLocked;

        [BoxGroup(ODIN_GROUP + "/Runtime")]
        [ShowInInspector, ReadOnly, LabelText("已注册相机列表")]
        private Dictionary<string, CinemachineCamera> DebugRegistry => _registry;

        #endregion

        // ============================================================
        // IEmberManager
        // ============================================================

        #region IEmberManager

        void IEmberManager.Init()
        {
            UICamera   = GameLauncher.Instance.UICamera;
            MainCamera = GameLauncher.Instance.MainCamera;

            if (MainCamera != null)
            {
                Brain = MainCamera.GetComponent<CinemachineBrain>();
                if (Brain != null && blenderSettings != null)
                    Brain.CustomBlends = blenderSettings;
            }

            EmberDebug.LogInit(TAG, $"EmberCameraManager initialized. " +
                $"Brain={(Brain != null ? "OK" : "not found")}, " +
                $"Blender={(blenderSettings != null ? "OK" : "not set")}.");
        }

        void IEmberManager.Destroy()
        {
            _registry.Clear();
            _overrideStack.Clear();
            _active = null;
            Brain = null;
        }

        #endregion

        // ============================================================
        // 相机注册
        // ============================================================

        #region 相机注册

        /// <summary>注册一个虚拟相机（用字符串 key）。</summary>
        public void Register(string key, CinemachineCamera vcam)
        {
            if (vcam == null) return;
            _registry[key] = vcam;
            vcam.gameObject.SetActive(false);
            EmberDebug.Log(TAG, $"Camera registered: {key} → {vcam.name}");
        }

        /// <summary>注销。</summary>
        public void Unregister(string key)
        {
            if (_registry.Remove(key))
                EmberDebug.Log(TAG, $"Camera unregistered: {key}");
        }

        /// <summary>是否已注册。</summary>
        public bool IsRegistered(string key) => _registry.ContainsKey(key);

        /// <summary>获取已注册相机。</summary>
        public CinemachineCamera GetCamera(string key)
            => _registry.TryGetValue(key, out var cam) ? cam : null;

        #endregion

        // ============================================================
        // 普通切换
        // ============================================================

        #region 普通切换

        /// <summary>
        /// 切换到指定 key 的相机。
        /// 如果当前处于霸占模式或锁定模式，切换会被拦截（除非 force = true）。
        /// </summary>
        public void Switch(string key, bool force = false)
        {
            if (!force)
            {
                if (_isLocked)
                {
                    EmberDebug.LogWarning(TAG, $"Switch to '{key}' blocked: camera is locked.");
                    return;
                }
                if (IsOverrideMode)
                {
                    EmberDebug.LogWarning(TAG, $"Switch to '{key}' blocked: override mode active.");
                    return;
                }
            }

            if (!_registry.TryGetValue(key, out var vcam))
            {
                EmberDebug.LogWarning(TAG, $"Switch failed: camera '{key}' not registered.");
                return;
            }

            ActivateCamera(vcam);
        }

        #endregion

        // ============================================================
        // 强制霸占堆栈 (Override Stack)
        // ============================================================

        #region 强制霸占堆栈

        /// <summary>
        /// 压入霸占栈顶 —— 强制接管相机控制权，并阻止普通切换。
        /// 适用于对话、Timeline、QTE 等需要完全控制相机的场景。
        ///
        /// 支持多重嵌套：对话里播 Timeline → Timeline 结束弹回对话 → 对话结束弹回自由。
        /// </summary>
        public void PushOverride(string key, CinemachineCamera localCamera = null)
        {
            // 优先使用传入的本地相机，其次从注册表查找
            var vcam = localCamera ?? GetCamera(key);
            if (vcam == null)
            {
                EmberDebug.LogWarning(TAG, $"PushOverride failed: camera '{key}' not found.");
                return;
            }

            _overrideStack.Add(new OverrideEntry { Key = key, Camera = vcam });
            EmberDebug.LogWarning(TAG, $"Camera OVERRIDE pushed: {key} (stack: {_overrideStack.Count})");

            ActivateCamera(vcam);
        }

        /// <summary>
        /// 弹出栈顶霸占。如果没有剩余霸占者，恢复正常模式。
        /// </summary>
        /// <param name="fallbackKey">弹栈后恢复到的相机 key（可选，默认回到上一个霸占者或自由模式）</param>
        public void PopOverride(string fallbackKey = null)
        {
            if (_overrideStack.Count == 0)
            {
                EmberDebug.LogWarning(TAG, "PopOverride: stack is empty.");
                return;
            }

            var removed = _overrideStack[_overrideStack.Count - 1];
            _overrideStack.RemoveAt(_overrideStack.Count - 1);
            EmberDebug.Log(TAG, $"Camera OVERRIDE popped: {removed.Key} (remaining: {_overrideStack.Count})");

            // 如果还有霸占者，流转给栈顶
            if (_overrideStack.Count > 0)
            {
                var top = _overrideStack[_overrideStack.Count - 1];
                EmberDebug.Log(TAG, $"Camera override resumed: {top.Key}");
                ActivateCamera(top.Camera);
                return;
            }

            // 没有霸占者了，恢复正常
            if (!string.IsNullOrEmpty(fallbackKey) && _registry.ContainsKey(fallbackKey))
            {
                Switch(fallbackKey, force: true);
            }
            else
            {
                EmberDebug.Log(TAG, "Camera override fully released. Normal switching restored.");
            }
        }

        /// <summary>
        /// 精准移除某个霸占条目（不限栈顶，用于 FixedCameraArea 安全退出等场景）。
        /// 如果移除的是当前活跃的栈顶，会流转给下一个霸占者或恢复正常。
        /// </summary>
        public bool RemoveOverride(string key)
        {
            int index = _overrideStack.FindLastIndex(e => e.Key == key);
            if (index < 0) return false;

            bool wasTop = (index == _overrideStack.Count - 1);
            _overrideStack.RemoveAt(index);
            EmberDebug.Log(TAG, $"Camera OVERRIDE removed: {key} (remaining: {_overrideStack.Count})");

            if (wasTop && _overrideStack.Count > 0)
            {
                var top = _overrideStack[_overrideStack.Count - 1];
                ActivateCamera(top.Camera);
            }

            return true;
        }

        #endregion

        // ============================================================
        // 锁定
        // ============================================================

        #region 锁定

        /// <summary>锁定相机，拒绝一切切换（霸占不受影响）。</summary>
        public void Lock()
        {
            _isLocked = true;
            EmberDebug.Log(TAG, "Camera locked.");
        }

        /// <summary>解除锁定。</summary>
        public void Unlock()
        {
            _isLocked = false;
            EmberDebug.Log(TAG, "Camera unlocked.");
        }

        #endregion

        // ============================================================
        // 内部方法
        // ============================================================

        #region 内部方法

        /// <summary>激活相机：禁用旧相机 → 启用新相机 → 触发事件。</summary>
        private void ActivateCamera(CinemachineCamera vcam)
        {
            if (_active == vcam) return;

            if (_active != null)
                _active.gameObject.SetActive(false);

            vcam.gameObject.SetActive(true);
            _active = vcam;

            EmberDebug.Log(TAG, $"Camera activated: {vcam.name}");
            OnCameraSwitched?.Invoke(vcam);
        }

        #endregion
    }
}
