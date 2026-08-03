using Sirenix.OdinInspector;
using UnityEngine;

namespace Ember.Core
{
    /// <summary>
    /// 游戏启动器 —— 框架的集中入口点。
    ///
    /// 挂在初始场景的 GameBoot GameObject 上。在 Inspector 中拖入宿主节点：
    ///
    /// <b>Inspector 赋值（手动拖拽，零运行时开销）：</b>
    /// <code>
    /// GameBoot (挂 GameLauncher)
    /// ├── UIRoot      → 拖到 UIRoot 字段   (UI 宿主，RectTransform)
    /// ├── AudioHost   → 拖到 AudioHost 字段 (音频宿主)
    /// ├── InputHost   → 拖到 InputHost 字段 (输入宿主)
    /// ├── EventSystem → Unity EventSystem（可选）
    /// └── MainCamera  → 主摄像机（可选）
    /// </code>
    ///
    /// <b>启动流程：</b>
    /// 1. Awake:  ConfigureStateMachine() 注册 Init / Main / Gameplay 三个核心状态
    /// 2. Start:  InitState.OnEnter → InitializeAll → CoreReady → TransitionTo&lt;MainState&gt;
    /// 3. Update / LateUpdate / FixedUpdate: 驱动 EmberUpdateManager + 状态机 Tick
    ///
    /// 使用方式：
    /// - 创建 GameBoot GameObject，挂载此脚本
    /// - 在 GameBoot 下创建 UIRoot / AudioHost / InputHost 子节点
    /// - 拖入 Inspector 对应字段（不要用字符串查找）
    ///
    /// 参考 burner 的入口模式。
    /// </summary>
    public class GameLauncher : EmberMonoSingleton<GameLauncher>
    {
        private const string ODIN_GROUP = "Game Launcher";

        #region 编辑器面板参数

        [FoldoutGroup(ODIN_GROUP, Expanded = true)]
        [BoxGroup(ODIN_GROUP + "/配置", ShowLabel = false)]
        [Title("宿主节点")]
        [Required("请拖入 UI 根节点")]
        [SerializeField] private GameObject _uiRoot;

        [BoxGroup(ODIN_GROUP + "/配置")]
        [Required("请拖入音频宿主")]
        [SerializeField] private GameObject _audioHost;

        [BoxGroup(ODIN_GROUP + "/配置")]
        [Required("请拖入输入宿主")]
        [SerializeField] private GameObject _inputHost;

        [BoxGroup(ODIN_GROUP + "/配置")]
        [Title("相机")]
        [Required("请拖入 UI 相机")]
        [SerializeField] private Camera _uiCamera;

        [BoxGroup(ODIN_GROUP + "/配置")]
        [Required("请拖入主相机")]
        [SerializeField] private Camera _mainCamera;

        #endregion

        // ============================================================

        #region 内部参数

        private const string TAG = LogTags.CoreGameLauncher;

        [FoldoutGroup(ODIN_GROUP)]
        [BoxGroup(ODIN_GROUP + "/运行时", ShowLabel = false)]
        [Title("运行时状态")]
        /// <summary>游戏状态机实例。</summary>
        [ShowInInspector, ReadOnly]
        public EmberStateMachine Fsm { get; private set; }

        [BoxGroup(ODIN_GROUP + "/运行时")]
        /// <summary>启动器是否已完成初始化。</summary>
        [ShowInInspector, ReadOnly, LabelText("已初始化")]
        public bool IsInitialized { get; private set; }

        [BoxGroup(ODIN_GROUP + "/运行时")]
        /// <summary>当前活跃的状态名。</summary>
        [ShowInInspector, ReadOnly, LabelText("当前状态")]
        private string CurrentState => Fsm?.Current?.Name ?? "—";

        public GameObject UIRoot => _uiRoot;
        public GameObject AudioHost => _audioHost;
        public GameObject InputHost => _inputHost;
        public Camera UICamera => _uiCamera;
        public Camera MainCamera => _mainCamera;

        #endregion

        // ============================================================

        #region 生命周期

        protected override void OnSingletonAwake()
        {
            EmberDebug.LogInit(TAG, "GameLauncher: initializing framework...");

            // 创建状态机并注册所有游戏状态
            Fsm = new EmberStateMachine();
            ConfigureStateMachine(Fsm);

            // Manager 初始化推迟到 InitState.OnEnter() 中执行（对齐 burner InitProcedure 模式）
            EmberDebug.LogInit(TAG, "GameLauncher: state machine ready. Entering InitState...");
        }

        private void Start()
        {
            // InitState.OnEnter → InitializeAll → CoreReady → TransitionTo<MainState>
            Fsm.Start<InitState>(args: Fsm);
            IsInitialized = true;
            EmberDebug.LogInit(TAG, "GameLauncher: InitState complete, ticking...");
        }

        private void Update()
        {
            if (!IsInitialized) return;

            EmberUpdateManager.Instance.DoUpdate();
            Fsm.Current?.OnUpdate();
        }

        private void LateUpdate()
        {
            if (!IsInitialized) return;

            EmberUpdateManager.Instance.DoLateUpdate();
        }

        private void FixedUpdate()
        {
            if (!IsInitialized) return;

            EmberUpdateManager.Instance.DoFixedUpdate();
        }

        protected override void OnSingletonDestroy()
        {
            EmberDebug.LogShutdown(TAG, "GameLauncher: shutting down framework...");

            // 逆序销毁所有 Manager
            EmberManagerCollector.Instance.DestroyAll();

            IsInitialized = false;
            EmberDebug.LogShutdown(TAG, "GameLauncher: framework shutdown complete.");
        }

        #endregion

        // ============================================================

        #region 外部方法

        /// <summary>
        /// 配置状态机 —— 注册所有游戏状态。
        ///
        /// 这是状态注册的<b>唯一入口</b>。未来可视化编辑器只需修改此方法内部的代码，
        /// 无需触碰 GameLauncher 的核心驱动逻辑（初始化顺序、Update 循环、销毁流程）。
        ///
        /// 扩展方式：
        /// <list type="bullet">
        ///   <item>手写：直接在此方法中调用 <c>fsm.Register(...)</c></item>
        ///   <item>可视化编辑器：代码生成器修改此方法体</item>
        ///   <item>继承：子类 override 此方法，调用 <c>base.ConfigureStateMachine(fsm)</c> 后追加自定义状态</item>
        /// </list>
        /// </summary>
        /// <param name="fsm">已创建的状态机实例</param>
        protected virtual void ConfigureStateMachine(EmberStateMachine fsm)
        {
            // ============================================================
            // 框架核心三状态（始终注册，不可删除）
            // Init → Main → Gameplay，单机/网游通用
            // ============================================================
            fsm.Register(new InitState());
            fsm.Register(new MainState());
            fsm.Register(new GameplayState());

            // 框架通用覆盖状态（可删除，用户可用自定义设置替代）
            fsm.Register(new SettingsState());

            // --- 业务状态注册 ---
            // 在下方注册自定义状态，例如：
            // fsm.Register(new LoginState());    // 网游：Init → Login → Main
            // fsm.Register(new BattleState());   // 玩法：Main → Battle → Main
            // --- 可视化编辑器生成区域 ---
        }

        #endregion
    }
}
