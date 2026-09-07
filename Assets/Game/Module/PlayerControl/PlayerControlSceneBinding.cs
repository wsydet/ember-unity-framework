using Sirenix.OdinInspector;
using Ember.Basic;
using Ember.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Module
{
    /// <summary>GameplayScene 对 PlayerControlModule 的场景引用入口。</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerControlSceneBinding : MonoBehaviour
    {
        private const string TAG = LogTags.Game + "." + nameof(PlayerControlSceneBinding);
        private const string INSPECTOR_GROUP = "Player Control Scene Binding";

        private PlayerControlModule _boundModule;

        #region 编辑器面板参数

        [FoldoutGroup("$INSPECTOR_GROUP", Expanded = true)]
        [BoxGroup("$INSPECTOR_GROUP/输入与配置", ShowLabel = false)]
        [Title("输入与配置", "运行时直接读取配置资产，播放状态修改后立即生效。")]
        [Required, LabelText("输入动作资产")]
        [SerializeField] private InputActionAsset _inputActions;

        [FoldoutGroup("$INSPECTOR_GROUP", Expanded = true)]
        [BoxGroup("$INSPECTOR_GROUP/输入与配置")]
        [Required, LabelText("控制参数")]
        [SerializeField] private PlayerControlSettings _settings;

        [FoldoutGroup("$INSPECTOR_GROUP", Expanded = true)]
        [BoxGroup("$INSPECTOR_GROUP/场景引用", ShowLabel = false)]
        [Title("场景引用", "玩家控制只消费这些引用，不负责相机注册。")]
        [Required, LabelText("LookAt 目标")]
        [SerializeField] private Transform _lookAt;

        [FoldoutGroup("$INSPECTOR_GROUP", Expanded = true)]
        [BoxGroup("$INSPECTOR_GROUP/场景引用")]
        [Required, LabelText("玩法正交相机")]
        [SerializeField] private CinemachineCamera _gameplayCamera;

        [FoldoutGroup("$INSPECTOR_GROUP", Expanded = true)]
        [BoxGroup("$INSPECTOR_GROUP/场景引用")]
        [Required, LabelText("移动边界")]
        [SerializeField] private PlayerControlBounds _movementBounds;

        [FoldoutGroup("$INSPECTOR_GROUP", Expanded = true)]
        [BoxGroup("$INSPECTOR_GROUP/鼠标拖动", ShowLabel = false)]
        [Title("鼠标拖动", "射线优先抓取场景碰撞体，没有命中时使用 LookAt 水平面。")]
        [LabelText("射线检测层")]
        [SerializeField] private LayerMask _dragRaycastLayers = ~0;

        [FoldoutGroup("$INSPECTOR_GROUP", Expanded = true)]
        [BoxGroup("$INSPECTOR_GROUP/鼠标拖动")]
        [LabelText("阻止从 UI 开始拖动")]
        [SerializeField] private bool _blockPointerOverUI = true;

        #endregion

        // --------------------------------------------------------

        #region 生命周期

        private void Awake()
        {
            if (!EmberModuleCollector.TryGetInstance(out EmberModuleCollector collector)
                || !collector.IsDiscovered)
            {
                EmberDebug.LogError(
                    TAG,
                    "Business modules were not discovered before GameplayScene Awake. "
                    + "Start the game through the framework bootstrap flow.");
                return;
            }

            if (!collector.IsModuleEnabled<PlayerControlModule>())
                return;

            if (!collector.TryGetModule(out PlayerControlModule module))
            {
                EmberDebug.LogError(
                    TAG,
                    "PlayerControlModule is enabled but has no discovered instance.");
                return;
            }

            _boundModule = module;
            _boundModule.Bind(
                _lookAt,
                _gameplayCamera,
                _movementBounds,
                _settings,
                _inputActions,
                _dragRaycastLayers,
                _blockPointerOverUI);
        }

        private void OnDestroy()
        {
            _boundModule?.Unbind(_lookAt);
            _boundModule = null;
        }

        #endregion
    }
}
