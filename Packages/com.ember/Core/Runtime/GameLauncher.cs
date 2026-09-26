using System;
using Sirenix.OdinInspector;
using UnityEngine;
using Ember.Basic;

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
    /// 2. Start:  InitState.OnEnter → DiscoverModules → InitializeAll → CoreReady → TransitionTo&lt;MainState&gt;
    /// 3. Update / LateUpdate / FixedUpdate: 驱动 EmberUpdateManager + 状态机 Tick
    ///
    /// 使用方式：
    /// - 创建 GameBoot GameObject，挂载此脚本
    /// - 在 GameBoot 下创建 UIRoot / AudioHost / InputHost 子节点
    /// - 拖入 Inspector 对应字段（不要用字符串查找）
    ///
    /// 参考 burner 的入口模式。
    /// </summary>
    public class GameLauncher : EmberBootBase<GameLauncher>
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

        /// <summary>是否已经开始退出流程（防止退出按钮连点重复封屏与重复清理）。</summary>
        private bool _isQuitting;

        #endregion

        // ============================================================

        #region 生命周期

        // ======== Awake / Start / Destroy ========

        /// <inheritdoc />
        protected override void OnBootAwake()
        {
            EmberQuitCurtain.Reset();  // 同域重进 Play Mode 时清掉上一次的封屏标记
            EmberDebug.LoadConfig();  // 自动同步 SO 配置到 EmberFileLog
            EmberFileLog.Start();     // 启动后台写线程

            EmberDebug.LogInit(TAG, "GameLauncher: initializing framework...");

            // 创建状态机并注册所有游戏状态
            Fsm = new EmberStateMachine();
            ConfigureStateMachine(Fsm);

            // Manager 初始化推迟到 InitState.OnEnter() 中执行（对齐 burner InitProcedure 模式）
            EmberDebug.LogInit(TAG, "GameLauncher: state machine ready. Entering InitState...");
        }

        /// <inheritdoc />
        protected override void OnBootStart()
        {
            // InitState.OnEnter → InitializeAll → CoreReady → TransitionTo<MainState>
            Fsm.Start<InitState>(args: Fsm);
            IsInitialized = true;
            EmberDebug.LogInit(TAG, "GameLauncher: InitState complete, ticking...");
        }

        /// <inheritdoc />
        protected override void OnBootDestroy()
        {
            ShutdownFramework();
        }

        // ======== Update / LateUpdate / FixedUpdate ========

        /// <inheritdoc />
        protected override void OnBootUpdate()
        {
            if (!IsInitialized) return;

            EmberEventBus.FlushPostQueue();  // 消费上一帧 PostNext 的延迟事件
            EmberUpdateManager.Instance.DoUpdate();
            Fsm.Current?.OnUpdate();
        }

        /// <inheritdoc />
        protected override void OnBootLateUpdate()
        {
            if (!IsInitialized) return;

            EmberUpdateManager.Instance.DoLateUpdate();
        }

        /// <inheritdoc />
        protected override void OnBootFixedUpdate()
        {
            if (!IsInitialized) return;

            EmberUpdateManager.Instance.DoFixedUpdate();
        }

        // ======== Application 生命周期 ========

        /// <summary>应用获得/失去焦点。当前无需处理。</summary>
        protected override void OnBootApplicationFocus(bool hasFocus) { }

        /// <summary>应用暂停/恢复。当前无需处理。</summary>
        protected override void OnBootApplicationPause(bool pauseStatus) { }

        /// <summary>
        /// 应用退出。与 OnBootDestroy 形成双保险：部分平台 OnDestroy 不保证调用。
        /// 这里同样先封屏，覆盖「系统/窗口关闭」等不经过 <see cref="Quit"/> 的退出路径：
        /// 清理之后 Unity 仍可能渲染若干帧，没有封屏就会露出 3D 场景。
        /// </summary>
        protected override void OnBootApplicationQuit()
        {
            EmberQuitCurtain.Show();
            ShutdownFramework();
        }

        #endregion

        // ============================================================

        #region 内部方法

        /// <summary>
        /// 框架清理：先销毁可选业务 Module，再逆序销毁所有必要 Manager，
        /// 最后停止文件日志并重置初始化标志。
        /// 编辑器退出 Play Mode 和游戏代码调用 Quit() 共用此方法。
        /// </summary>
        private void ShutdownFramework()
        {
            if (!IsInitialized) return;

            EmberDebug.LogShutdown(TAG, "GameLauncher: shutting down framework...");
            EmberModuleCollector.Instance.DestroyAll();   // 业务模块（高层）先销毁
            EmberManagerCollector.Instance.DestroyAll();   // 框架管道（底层）后销毁
            IsInitialized = false;
            EmberFileLog.Stop();
            EmberDebug.LogShutdown(TAG, "GameLauncher: framework shutdown complete.");
        }

        #endregion

        // ============================================================

        #region 外部方法

        /// <summary>
        /// 退出应用。顺序为：先执行退出封屏（3D 主相机转纯黑 + UI 不透明遮罩），
        /// 再执行框架清理（逆序销毁 Manager、刷写文件日志），
        /// 最后调用 <see cref="ApplicationQuitUtil.Quit"/> 退出应用或停止编辑器 Play Mode。
        ///
        /// 业务层应通过此方法退出，而不是直接调用 Application.Quit()。
        ///
        /// <para><b>为什么封屏必须在清理之前：</b>Unity 的 <c>Object.Destroy</c> 在
        /// 「当前 Update 之后、渲染之前」生效，而 PC 等平台的 <c>Application.Quit()</c> 之后仍会继续出帧。
        /// 若先清理再退出，就会出现「UI 已销毁、只剩 3D 场景」的穿帮帧
        /// （视觉小说等以 3D 场景承载 2D 画面的项目尤其明显）。
        /// 封屏与相机黑屏同帧生效，因此之后渲染的任何一帧都是全黑。</para>
        ///
        /// 编辑器中调用此方法会停止 Play Mode；手动停止 Play Mode 时，
        /// 由 <see cref="OnSingletonDestroy"/> 执行相同的框架清理。
        /// </summary>
        public void Quit()
        {
            if (_isQuitting) return;
            _isQuitting = true;

            EmberDebug.LogShutdown(TAG, $"GameLauncher: quit requested (exit branch: {ApplicationQuitUtil.DescribeBranch()}).");

            EmberQuitCurtain.Show();       // ① 封屏：3D 主相机纯黑 + UI 不透明遮罩（同帧生效）
            ShutdownFramework();           // ② 框架清理：日志落盘 + 逆序销毁（此时销毁页面已无穿帮风险）
            ApplicationQuitUtil.Quit();    // ③ 退出应用 / 停止编辑器 Play Mode
        }

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
        /// <summary>
        /// 创建 InitState 实例。自动发现游戏层的 GameInitState 子类；
        /// 如果不存在则返回框架默认 InitState。用户只需在游戏层创建
        /// <c>GameInitState : InitState</c>，无需修改任何框架代码。
        /// </summary>
        protected virtual InitState CreateInitState()
        {
            var found = FindSubclass<InitState>();
            return found != null ? (InitState)Activator.CreateInstance(found) : new InitState();
        }

        /// <summary>
        /// 创建 MainState 实例。自动发现游戏层的 GameMainState 子类；
        /// 如果不存在则返回框架默认 MainState。用户只需在游戏层创建
        /// <c>GameMainState : MainState</c>，无需修改任何框架代码。
        /// </summary>
        protected virtual MainState CreateMainState()
        {
            var found = FindSubclass<MainState>();
            return found != null ? (MainState)Activator.CreateInstance(found) : new MainState();
        }

        /// <summary>
        /// 创建 GameplayState 实例。自动发现游戏层的 GameGameplayState 子类；
        /// 如果不存在则返回框架默认 GameplayState。用户只需在游戏层创建
        /// <c>GameGameplayState : GameplayState</c>，无需修改任何框架代码。
        /// </summary>
        protected virtual GameplayState CreateGameplayState()
        {
            var found = FindSubclass<GameplayState>();
            return found != null ? (GameplayState)Activator.CreateInstance(found) : new GameplayState();
        }

        /// <summary>
        /// 创建 SettingsState 实例。自动发现游戏层的 GameSettingsState 子类；
        /// 如果不存在则返回框架默认 SettingsState。
        /// </summary>
        protected virtual SettingsState CreateSettingsState()
        {
            var found = FindSubclass<SettingsState>();
            return found != null ? (SettingsState)Activator.CreateInstance(found) : new SettingsState();
        }

        /// <summary>
        /// 在所有已加载的程序集中查找指定基类的非抽象子类。
        /// 安全处理 ReflectionTypeLoadException（缺失引用导致 GetTypes 抛出异常）。
        /// </summary>
        /// <typeparam name="T">基类类型</typeparam>
        /// <returns>找到的 Type，未找到返回 null</returns>
        private static Type FindSubclass<T>() where T : EmberGameState
        {
            var baseType = typeof(T);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }  // 跳过因缺失引用导致 GetTypes() 失败的程序集

                foreach (var t in types)
                {
                    if (t != null && t.IsSubclassOf(baseType) && !t.IsAbstract)
                        return t;
                }
            }
            return null;
        }

        protected virtual void ConfigureStateMachine(EmberStateMachine fsm)
        {
            // ============================================================
            // 框架核心三状态（始终注册，不可删除）
            // Init → Main → Gameplay，单机/网游通用
            // ============================================================
            fsm.Register(CreateInitState());
            fsm.Register(CreateMainState());
            fsm.Register(CreateGameplayState());
            fsm.Register(CreateSettingsState());

            // --- 业务状态注册 ---
            // 在下方注册自定义状态，例如：
            // fsm.Register(new LoginState());    // 网游：Init → Login → Main
            // fsm.Register(new BattleState());   // 玩法：Main → Battle → Main
            // --- 可视化编辑器生成区域 ---
        }

        #endregion
    }
}
