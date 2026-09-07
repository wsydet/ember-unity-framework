// Copyright (c) 2026 Ember Unity Framework. All rights reserved.

using System.Collections.Generic;

using Ember.SceneUI;
using Ember.SceneUI.Integration;

using Sirenix.OdinInspector;

using UnityEngine;

namespace Game.Module
{
    /// <summary>场景物体的空间更新方式。</summary>
    public enum SceneUIObjectUpdateMode
    {
        /// <summary>物体位置运行时不变；只响应首次注册、手动标脏和相机变化。</summary>
        Static = 0,

        /// <summary>每次 World Channel 刷新时检查物体位置。</summary>
        Dynamic = 1,
    }

    /// <summary>
    /// 挂载在场景物体上的 SceneUI 业务入口。
    /// 组件只声明目标和气泡语义；实际 View 仍是 BubbleRoot 下的 EUI Item。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneUIObject : MonoBehaviour
    {
        private const string INSPECTOR_GROUP = "Scene UI Object";

        #region 编辑器面板参数

        [PropertyOrder(-100)]
        [FoldoutGroup("$INSPECTOR_GROUP", Expanded = true)]
        [BoxGroup("$INSPECTOR_GROUP/气泡配置", ShowLabel = false)]
        [Title("气泡配置", "声明气泡类型、空间更新方式和初始状态。")]
        [LabelText("气泡类型")]
        [SerializeField, Tooltip("该物体使用的业务气泡类型。")]
        private SceneUIViewId _bubbleType = SceneUIViewId.Example;

        [PropertyOrder(-99)]
        [FoldoutGroup("$INSPECTOR_GROUP", Expanded = true)]
        [BoxGroup("$INSPECTOR_GROUP/气泡配置")]
        [LabelText("位置更新方式")]
        [SerializeField, Tooltip("静态物体不轮询位置；动态物体会在 Channel 刷新时检查位置。")]
        private SceneUIObjectUpdateMode _updateMode = SceneUIObjectUpdateMode.Static;

        [PropertyOrder(-98)]
        [FoldoutGroup("$INSPECTOR_GROUP", Expanded = true)]
        [BoxGroup("$INSPECTOR_GROUP/气泡配置")]
        [LabelText("世界坐标偏移")]
        [SerializeField, Tooltip("相对于物体 Transform.position 的世界坐标偏移。")]
        private Vector3 _worldOffset = new Vector3(0f, 2f, 0f);

        [PropertyOrder(-97)]
        [FoldoutGroup("$INSPECTOR_GROUP", Expanded = true)]
        [BoxGroup("$INSPECTOR_GROUP/气泡配置")]
        [LabelText("初始可见")]
        [SerializeField, Tooltip("组件注册后气泡是否默认可见。")]
        private bool _initiallyVisible = true;

        [PropertyOrder(-50)]
        [FoldoutGroup("$INSPECTOR_GROUP", Expanded = true)]
        [BoxGroup("$INSPECTOR_GROUP/运行时状态", ShowLabel = false)]
        [Title("运行时状态", "SceneUI Channel 就绪后会自动完成注册。")]
        [ShowInInspector, ReadOnly, LabelText("注册状态")]
        [GUIColor("$RegistrationStatusColor")]
        private string RegistrationStatus => IsRegistered ? "已注册" : "未注册";

        [PropertyOrder(0)]
        [FoldoutGroup("$INSPECTOR_GROUP", Expanded = true)]
        [BoxGroup("$INSPECTOR_GROUP/运行时操作", ShowLabel = false)]
        [Title("运行时操作", "调整物体 Transform 或世界坐标偏移后，可手动同步气泡位置。")]
        [Button("手动刷新气泡位置", ButtonSizes.Large)]
        [GUIColor(0.3f, 0.7f, 1f)]
        [EnableIf("@UnityEngine.Application.isPlaying && IsRegistered")]
        private void RefreshPositionFromInspector()
        {
            RefreshPosition();
        }

        #endregion

        // --------------------------------------------------------

        #region 内部参数

        private static readonly HashSet<SceneUIObject> ActiveObjects =
            new HashSet<SceneUIObject>();

        private static SceneUIModule _module;

        private SceneUIModuleHandle _handle;

        private Color RegistrationStatusColor => IsRegistered
            ? new Color(0.35f, 0.9f, 0.45f)
            : new Color(0.95f, 0.45f, 0.35f);

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            ActiveObjects.Clear();
            _module = null;
        }

        private void OnEnable()
        {
            ActiveObjects.Add(this);
            TryRegister();
        }

        private void OnDisable()
        {
            Unregister();
            ActiveObjects.Remove(this);
        }

        #endregion

        // --------------------------------------------------------

        #region 内部方法

        internal static void AttachModule(SceneUIModule module)
        {
            if (module == null)
                return;

            _module = module;
            TryRegisterAll();
        }

        internal static void NotifyChannelReady(SceneUIChannelKey channelKey)
        {
            if (channelKey == SceneUIModule.WorldChannel)
                TryRegisterAll();
        }

        internal static void DetachModule(SceneUIModule module)
        {
            if (_module != module)
                return;

            foreach (SceneUIObject sceneObject in ActiveObjects)
            {
                if (sceneObject)
                    sceneObject._handle = default;
            }

            _module = null;
        }

        private static void TryRegisterAll()
        {
            foreach (SceneUIObject sceneObject in ActiveObjects)
            {
                if (sceneObject && sceneObject.isActiveAndEnabled)
                    sceneObject.TryRegister();
            }
        }

        private bool TryRegister()
        {
            if (!isActiveAndEnabled || _module == null || !_module.IsActive)
                return false;

            if (_module.IsSceneUIValid(_handle))
                return true;

            _handle = default;
            return _module.TryShowBubble(
                transform,
                _bubbleType,
                _updateMode,
                _worldOffset,
                _initiallyVisible,
                out _handle);
        }

        private void Unregister()
        {
            if (_module != null)
                _module.UnregisterSceneUI(_handle);

            _handle = default;
        }

        #endregion

        // --------------------------------------------------------

        #region 外部方法

        public SceneUIViewId BubbleType => _bubbleType;
        public SceneUIObjectUpdateMode UpdateMode => _updateMode;
        public Vector3 WorldOffset => _worldOffset;
        public bool IsRegistered => _module != null && _module.IsSceneUIValid(_handle);

        /// <summary>静态物体被业务移动后，显式通知 SceneUI 重新读取位置。</summary>
        public bool NotifyPositionChanged()
        {
            return RefreshPosition();
        }

        /// <summary>应用当前偏移，并立即重新投影该物体的 SceneUI。</summary>
        public bool RefreshPosition()
        {
            return _module != null
                   && _module.RefreshSceneUIPosition(_handle, _worldOffset, Vector2.zero);
        }

        /// <summary>修改气泡业务显隐。</summary>
        public bool SetVisible(bool visible)
        {
            _initiallyVisible = visible;
            return _module != null
                   && _module.SetSceneUIVisible(_handle, visible);
        }

        /// <summary>运行时修改配置后重新注册；Channel 尚未 Ready 时会等待 Ready 回调。</summary>
        public bool RefreshRegistration()
        {
            Unregister();
            return TryRegister();
        }

        #endregion
    }
}
