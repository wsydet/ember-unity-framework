# Ember Framework 开发进度

> 最后更新：2026-08-31
> 参考项目：[burner](../../c:/Users/wuyu/Project/burner/client/game/) — 成熟的 SLG 游戏框架

---

## 架构总览

```
Assets/Ember/                     # 框架层（零业务逻辑）
├── Core/                         #   核心：EventBus、ServiceLocator、Singleton、StateMachine、UpdateManager、Debug
├── Resource/                     #   资源管理：加载/卸载抽象
├── UI/                           #   UI 管理：界面栈 + 生命周期
├── Scene/                        #   场景管理：加载/卸载/过渡 + 状态机桥接
├── Audio/                        #   音频管理：BGM/SFX + Mixer
├── Camera/                       #   相机管理：Cinemachine + 霸占堆栈
├── Input/                        #   输入抽象层
└── Editor/                       #   框架级编辑器工具
```

### 依赖方向

```
Core ← Resource ← Scene
  ← UI
  ← Audio
  ← Camera
  ← Input

Core 是叶子层，零依赖（除 Unity 引擎 + Odin + UniTask），所有上层模块只能依赖 Core。
```

---

## 开发策略：单机先行 → 网络补充 → 可视化收尾

```
┌──────────────────────────────────────────────────┐
│  Phase 1: 单机框架（当前）                          │
│  先按单机游戏的需求开发，所有模块默认本地运行。         │
│  网络相关接口预留但暂不实现。                         │
│                                                   │
│  Phase 2: 网络适配                                │
│  单机跑通后，补上网络层：RPC、状态同步、联网验证等。    │
│  利用 Phase 1 的接口预留位快速接入。                  │
│                                                   │
│  Phase 3: 可视化编辑器                             │
│  框架定型后，搭建蓝图/节点编辑器，面向策划和设计师。    │
└──────────────────────────────────────────────────┘
```

**提醒规则：** 开发过程中如果某个模块在联网场景下需要不同的行为
（例如 Scene 模块的加载方式、StateMachine 的同步校验），
文档中用 `🌐 [NET] 待适配` 标记，代码中用 `// TODO NET` 留空，
方便 Phase 2 全局搜索。

---

<!-- ═══════════════════════════════════════════════════════════════════════════ -->
<!-- >>> 📍 CURRENT PHASE — v0.10.0 UI 框架与基础模板 v0.5.0 已完成编译、Play 验证、手动保存及静态复检；待用户提交/tag/push，随后进入多模板 P-C 与消费端/Farm 联调 <<< -->
<!-- ═══════════════════════════════════════════════════════════════════════════ -->

## 当前进度与路线

```
已完成（2026-07-25 → 2026-08-22）
═══════════════════════════════
Phase 1: 单机框架核心 ──────── ✅
  8 个模块 + Manager/Update/StateMachine/Debug
  S1-S9 场景集成验证通过

Phase 2: Package 迁移 ──────── ✅
  com.ember.basic      (36 文件)
  com.ember.extensions  (6 文件)
  com.ember.uiextension (33 文件，L1-L3)

Phase 3: UIManager 重写 ───── ✅
  12 文件的两层架构 + UIBinding 系统
  Edit Mode 测试 12 项全部通过

Phase 4: 启动流程重构 ──────── ✅
  事件链重设计：BootSplash→Init / 开屏动画→Main
  GameMainState + OnOpeningAnimationEnd 扩展点
  PlayModePageDefGuard 修复 + Canvas sortingOrder

Phase 5: UI 集成测试 ──────── ✅
  MainMenu / Settings / InGameUI 全链路 Play Mode 验证通过
  方块过渡动画 + 开屏串行时序 + 背景页下沉转场

Phase 5.5: UI 增强组件集成测试 ── ✅
  GMPage 迁移业务层（破除循环依赖）
  4 个增强组件（EUIButtonEx/ToggleEx/ImageEx/CircleImage）
  绑定类型识别 + Label 槽位 + 组件替换 + 运行时逻辑
  详见 uiextension-test-plan.md §五/§六

Phase 6: 剩余 P1/P2 ───────── 📍 当前
  Module 系统 / Timer / Audio 升级
  架构债务 / 预制体对象池 / 本地化

Phase 7: 网络适配 ─────────── ⬜ 远期
Phase 8: 可视化编辑器 ─────── ⬜ 远期
```

### 已完成清单

- ✅ Core / Resource / UI / Scene / Audio / Camera / Input / Editor 模块（8 个）
- ✅ Manager 自动发现 / Update 循环 / GameState 状态机 / 日志系统
- ✅ S1-S9 场景集成验证
- ✅ com.ember.basic 包迁移（36 Runtime + 编辑器工具）
- ✅ com.ember.extensions 包迁移（17→6 文件，逐文件审计删除 12，迁移 5）
- ✅ API 速查手册（docs/dev/ember-api-reference.md）
- ✅ 编辑器工具测试清单（docs/dev/editor-tools-test-checklist.md）
- ✅ 编辑器工具多轮修复（全局语言同步 / 面板布局 / 菜单重组）
- ✅ 模块 README 文档（15 个，覆盖全部 8 个模块 + Core 子目录）
- ✅ UIManager 结构性重写 Phase A（12 文件：EUIPage / PageContext / UIManager / UIPageRouter / UIObserver）
- ✅ com.ember.uiextension 包 L1-L2 迁移（25 文件：Tweener 已删除，独立控件 + Behaviour + SafeArea）
- ✅ UIBinding 系统（完整 Burner 对齐：EmberUIBinding / Editor / EmberUIBindingTemplate / LogicImplementationData / CSharpLogicImplementationData / UIBindingSettingData / EmberUIBindingEditorUtility）
- ✅ L3 组件封装层（8 文件：EUIComponent + Button/Text/Image/Toggle/InputField/ProgressBar）
- ✅ UI 模块 Edit Mode 测试（12 项全部通过）
- ✅ 启动流程重构：BootSplash 归 Init / 开屏动画归 Main / MainSceneReady + OpeningAnimationEnd 事件链
- ✅ GameMainState 子类化扩展点（反射自动发现，零配置）+ OnOpeningAnimationEnd 钩子 → ShowMainPage
- ✅ 启动动画基类重命名为 EUIMainAnimationStarter
- ✅ IEUIPersistentUI marker 接口（UIManager 跳过 BootSplash）
- ✅ PlayModePageDefGuard 失效检测修复（PageName 匹配替代文件路径拼接）+ 自动清理弹窗
- ✅ FrameworkSceneBootstrapper 场景清理守卫（isPlayingOrWillChangePlaymode 检测）
- ✅ Canvas sortingOrder 编辑器预览（生成时自动写入预制体）
- ✅ EmberUIBindingBridge 自动注册（RuntimeInitializeOnLoadMethod）
- ✅ MainMenu + Settings 页面生成 + 业务逻辑填充
- ✅ GameMainState / GameGameplayState / GameSettingsState 三个业务状态子类
- ✅ GameplayScene / SettingsScene / MainScene / FrameworkScene 四场景 + 场景映射
- ✅ UI 框架类型统一 EUI\* 前缀（uiextension Runtime + UI 框架层 + UI 组件）
- ✅ EUIPageRouter 合并进 EUIViewEngine（视图引擎），EUIManager 转应用层入口
- ✅ 状态机子类自动发现（GameLauncher 反射）+ BootSplash 淡出与状态切换解耦
- ✅ 场景加载拦截器接口 InterceptSceneLoad（跨场景 Loading 拦截）
- ✅ 方块过渡动画 TransitionBlock（曲线驱动）+ Loading 接入 + 页面预设过渡槽
- ✅ 开屏动画串行时序 + 背景页加载下沉状态机转场（遮挡层渐出前就绪）
- ✅ 启动时序文档（docs/dev/ember-boot-sequence.md）
- ✅ Core 新增 EmberTimeManager / EmberEventGroup / EmberBootBase / EmberBootSplashBridge
- ✅ P0 UI 集成测试 Play Mode 全链路验证通过（Init→Main→Settings→Gameplay→Main）

### 2026-08-22 UI 增强组件集成测试完成

- ✅ 4 个增强组件自定义 Editor（EUIButtonEx/ToggleEx/ImageEx/CircleImageEditor）—— 解决内置 Editor 接管子类导致增强字段不显示
- ✅ 组件替换三点菜单闪退修复（EditorApplication.delayCall 延迟执行）
- ✅ GMPage 迁至业务层（Assets/Game/UI/Runtime）—— 破除框架层循环依赖
- ✅ GMPage 实例化移至 Init 阶段（GameInitState，FreePage 常驻只创建一次）+ GameLauncher.CreateInitState 工厂
- ✅ GMPage 业务逻辑：时间缩放（Time.timeScale + EmberTimeManager 双设置）/ 顶层状态名显示 / 增强组件测试
- ✅ 绑定列表命名校验高亮（EUIBindingListDrawer，前缀不匹配橙色提醒 + ✎ 一键重命名）
- ✅ uiextension 测试计划更新（uiextension-test-plan.md §五结果表 + §六问题记录 7 条）

### 接下来要做的事

| 优先级 | 事项 | 文件数 | 说明 |
|--------|------|--------|------|
| ✅ P0 | **框架转为 UPM 包** | — | ✅ **完成（2026-08-31）**：单一 `com.ember` 包（模块中心布局，22 asmdef）；模板体系（Templates~/base 全量演示镜像 + 初始化窗口 + 模板编辑器）；EmberUPMManager 一键升级；UniTask 内置；共享字体入包；模板升级协同 P-A + 两级标记 + GamePages 拆分；EUIBinding 框架/用户块标记；框架 UI 业务层化；状态机边模型。**已发布 v0.9.0（框架 0.9.0 + 模板 0.3.0）/ v0.9.1 / v0.9.2**；v0.10.0 UI 框架与基础模板 v0.5.0 已完成编译、Play 验证、手动保存及静态复检，待用户发布。详见 [upm-migration-plan.md](upm-migration-plan.md) §〇 当前状态快照 |
| 🟡 P1 | **Audio 多 Category + AudioAgent 池** | ~5 | 详见 [audio-upgrade-plan.md](../../docs/dev/audio-upgrade-plan.md) |
| 🟡 P1 | **ScrollRect 配置异常导致 Prefab 永远 dirty** | — | GMPage `m_Scr_Test` Viewport 尺寸 0（`[ExecuteAlways]` 布局实时重算产生浮点残渣）。已隐藏跳过；后续修正 Viewport 为标准布局（anchor `(0,0)-(1,1)`）即可根治，详见 uiextension-test-plan.md §6.4 |
| 🟢 P2 | **预制体对象池** | 1 | GameObject 预制体池化 |
| 🟢 P2 | **本地化** | — | |
| 🟢 P2 | **uiextension Editor 工具** | 35 | Previews / Settings / Validation / Bake 编辑器工具 |
| 🟢 P2 | **L3 虚拟列表/预加载** | 4 | GameUIContainer / GameTabLoader / GameUIAttachment / GamePagePreloader（依赖资源系统升级） |
| 🟢 P2 | **GM 系统增强** | — | GMPage 基础版已完成（时间缩放/状态名显示/增强组件测试 + FPSDisplay 帧率显示挂载）；待加：快捷键、常用调试操作 |


/*通用条件模块 CommonConditionModule
功能解锁进度 FunctionUnlockModule 
任务系统

输入系统
输入重绑定

GM系统+帧率显示+UI    ← GMPage 基础版已完成（2026-08-22）：时间缩放/状态名/增强组件测试 + FPSDisplay 帧率显示

包管理，不报错

修改为upm框架    ← 已列入待办 P0：框架转 UPM 包（支持独立升级，转包后到具体项目验证缺陷）

2d/3d快速切换
单机/网游快速切换模板
平台快速切换

测试编辑器脚本*/

### UI 集成测试步骤

> 详见 [docs/dev/ui-testing-plan.md](../../docs/dev/ui-testing-plan.md)

| 步骤 | 内容 | 状态 |
|------|------|:--:|
| 1 | 创建 MainMenu + Settings 预制体（Canvas + UI 子节点） | ✅ |
| 2 | 挂载 EmberUIBinding + 配置 pageName/classPath/pageFlags | ✅ |
| 3 | 配置 CSharpLogicImplementationData SO + Project Settings | ✅ |
| 4 | Inspector 中"自动收集绑定" → "生成代码" | ✅ |
| 5 | 手写页面逻辑（OnInit / OnPause / OnResume / OnDispose） | ✅ |
| 6 | EmberUIBindingBridge.Register() 自动注册（RuntimeInitializeOnLoadMethod） | ✅ |
| 7 | 启动流程：BootSplash→Init 退出关闭 / 开屏动画→Main.OnEnter / OnOpeningAnimationEnd→打开 MainMenu | ✅ |
| 8 | Play Mode 验证完整链路 | ✅ |
| 9 | Loading 预制体绑定（TopMost 层） | ✅ |

**验证矩阵：** EUIManager Push/Pop、EUIPageRouter 路由、EUIPage（纯 C# 包装）+ EUILogic（生成逻辑类）生命周期分离、EUIPageContext 栈管理、EUIBgMaskPool 遮罩、EUIObserver 事件、EmberUIBinding 代码生成。**生成的代码继承 `EUILogic`（非 MonoBehaviour），使用 `ControlMap["name"] as Type` 模式。**

---

## 实现顺序

按依赖关系排列，先底层后上层：

| 序号 | 模块 | 程序集 | 状态 | 参考 burner |
|------|------|--------|------|-------------|
| 1 | **Core** | `Ember.Core.Runtime` | ✅ 已完成 | `GameCore.Runtime` + `Burner.Basic` |
| 2 | **Resource** | `Ember.Resource.Runtime` | ✅ 已完成 | `ResManager` + `IResourceProxy` + YooAsset |
| 3 | **UI** | `Ember.UI.Runtime` | ✅ 已完成 | `GameUIManager` + `Burner.UIExtension` |
| 4 | **Scene** | `Ember.Scene.Runtime` | ✅ 已完成 | `GameSceneManager` |
| 5 | **Audio** | `Ember.Audio.Runtime` | ✅ 已完成 | `AudioMgr` |
| 6 | **Camera** | `Ember.Camera.Runtime` | ✅ 已完成 | Cinemachine + 霸占堆栈 |
| 7 | **Input** | `Ember.Input.Runtime` | ✅ 已完成 | Unity Input System 封装 |
| 8 | **Editor** | `Ember.Editor` | ✅ 已完成 | 框架级编辑器工具 |
| 9 | **Manager 自动发现** | `Ember.Core.Runtime` | ✅ 已完成 | `GameMgrCollector` + `IManager` |
| 10 | **Update 循环管理器** | `Ember.Core.Runtime` | ✅ 已完成 | `GameUpdateManager` |
| 11 | **Timer 定时器** | `Ember.Core.Runtime` | ✅ 已完成 | `TimerManage`（delta 累加，不依赖 UniTask） |
| 12 | **GameState 状态机** | `Ember.Core.Runtime` | ✅ 已完成 | `GameStateManager` |
| 13 | **日志系统** | `Ember.Core.Runtime` | ✅ 已完成 | `Debuger` |
| 14 | **Basic 包迁移** | `com.ember.basic` | ✅ 已完成 | 36 文件迁移 + 用户工具整合 + 编辑器工具 |
| 15 | **模块文档** | `Assets/Ember/*/` | ✅ 已完成 | 15 个 README.md，覆盖全部 8 个模块 |

---

## 模块详情

### 1. Core 模块 `Ember.Core.Runtime`

> 状态：✅ 已完成
> burner 参考：`Assets/Game/GameCore/Runtime/` + `com.burner.basic`

#### 文件清单

Core 按功能分子目录，详见各子目录的 README.md：

| 子目录 | 文件 | 职责 |
|--------|------|------|
| `Event/` | EmberEventBus.cs、EmberBroadcastEvent.cs | 事件总线（Subscribe→IDisposable、OnNext 播报）+ 事件常量表（基址间隔 1000） |
| `Service/` | EmberServiceLocator.cs、EmberSingleton.cs、EmberObjectPool.cs、EmberBaseSO.cs | 服务定位器 + 两种单例 + 对象池 + SO 基类 |
| `Manager/` | IEmberManager.cs、IEmberModule.cs、EmberInitOrderAttribute.cs、EmberManagerCollector.cs | 管理器接口 + 业务模块接口 + 初始化优先级 + 反射自动收集 |
| `State/` | EmberStateMachine.cs、InitState.cs、MainState.cs、GameplayState.cs、SettingsState.cs、TransitionDescriptor.cs | 状态机 + 核心三状态 + 覆盖式设置 + 流转描述符 |
| `Update/` | EmberUpdateManager.cs、IEmberUpdate.cs | 统一 Update/LateUpdate/FixedUpdate 驱动 |
| — | GameLauncher.cs | 框架集中入口：状态机 + Update 循环 + Manager 生命周期 |
| — | EmberSceneField.cs | 场景文件引用（Odin 面板拖拽选择，隐式 string 转换） |
| `Editor/` | FrameworkSceneBootstrapper.cs | Build Settings 场景同步 + Play Mode 管理 |

#### 与 burner 的设计差异

| 维度 | burner | ember | 理由 |
|------|--------|-------|------|
| 事件 Key | int + 区间分配（ModuleType + EventDefine） | int + 区间分配（EmberBroadcastEvent 常量表） | 同方案，ember 合并为一个常量文件（模块数少） |
| 遍历安全 | 索引指针调整 | 延迟操作队列 (pending ops) | 更清晰的语义，支持嵌套 dispatch |
| 服务定位 | 无（Singleton.Instance + 反射） | EmberServiceLocator | 解耦接口与实现，方便测试和替换 |
| 对象池 | 最小实现（仅 Stack） | 带容量/统计/IPoolable | 更完整的生产级实现 |

#### 事件通信分层策略

事件通信的选择标准不是"框架 vs 业务"，而是**事件的本质**——它是"广播型"还是"数据流型"：

| 维度 | 广播型事件（Broadcast） | 数据流型事件（Data Stream） |
|------|------------------------|---------------------------|
| **例子** | 模块初始化完成、模块销毁、场景加载完毕 | 玩家血量变化、获得物品、技能释放 |
| **发布者心态** | "这件事发生了，谁关心谁听" | "这个数据变了，有几个确定的消费者" |
| **消费方** | 不确定有几方，随项目增长动态变化 | 确定——UI、音效、数据记录各取所需 |
| **频率** | 低频（初始化/销毁各一次） | 高频（每次操作都可能触发） |
| **需要操作符？** | 不需要 | 需要（`Throttle` 防抖、`Where` 过滤、`Batch` 聚合） |
| **适合什么** | EmberEventBus | UniRx Subject / MessageBroker |

```
┌─────────────────────────────────────────────┐
│  UniRx Subject / MessageBroker              │
│  玩家血量、技能命中、物品变更、战斗结算...       │
│  "具体数据流，有确定的生产者和消费者"            │
│  优势：类型安全 + 操作符 + AddTo 生命周期      │
├─────────────────────────────────────────────┤
│  EmberEventBus                              │
│  Module.Ready / Module.Destroy / Scene.Loaded │
│  "框架级公告，谁关心谁听"                       │
│  优势：零引用耦合 + 零外部依赖 + 不确定消费方   │
└─────────────────────────────────────────────┘
```

#### 选择规则

| 场景 | 用什么 | 理由 |
|------|--------|------|
| 模块生命周期（Ready / Destroy） | EmberEventBus | 广播型，不确定消费方是谁 |
| 全局系统通知（网络断开、切后台） | EmberEventBus | 零依赖，任何模块都可以发 |
| 具体游戏数据变化（血量、物品、技能） | UniRx Subject | 数据流型，各消费方需要独立处理 |
| 需要操作符的 UI 响应 | UniRx Subject | `Throttle` / `Where` / `Delay` 开箱即用 |
| 框架事件需被业务层消费 | EmberEventBus → UniRx 桥接 | 适配器转成 `IObservable<T>` |

#### 演进路径

- **当前**：EmberEventBus 服务于广播型事件，UniRx Subject 服务于数据流型事件
- **底线**：框架 Core 零外部依赖，EmberEventBus 不会被 UniRx 替代——它覆盖的场景是 UniRx 也无法替代的（真正的 "谁关心谁听" 无耦合广播）

---

### 2. Resource 模块 `Ember.Resource.Runtime`

> 状态：✅ 已完成
> burner 参考：`Assets/Game/GameCore/Runtime/Common/Res/`

#### 设计思路

Resource 模块是框架资源加载的统一入口，核心思想是**接口隔离**——
框架只定义 `IResourceProvider` 接口，具体后端由使用者实现并注册。
上层模块通过 `EmberResourceManager` 消费资源，不感知底层是 Resources、Addressables 还是 YooAsset。

```
UI / Scene / Audio / ...
        │
        ▼
EmberResourceManager (Singleton Facade)
        │
        ▼
IResourceProvider (接口)
        │
        ├── ResourcesProvider (开发/小项目)
        ├── AddressablesProvider (正式项目)
        └── YooAssetProvider (热更新/大项目)
```

#### 文件清单

| 文件 | 职责 | 参考 |
|------|------|------|
| [Ember.Resource.Runtime.asmdef](../../Assets/Ember/Resource/Runtime/Ember.Resource.Runtime.asmdef) | 程序集定义，依赖 Ember.Core.Runtime | — |
| [IResourceProvider.cs](../../Assets/Ember/Resource/Runtime/IResourceProvider.cs) | 资源提供者接口：Init / LoadAssetAsync / LoadSceneAsync / Unload / Progress | burner `IResourceProxy` |
| [EmberResourceManager.cs](../../Assets/Ember/Resource/Runtime/EmberResourceManager.cs) | 资源管理器门面，EmberMonoSingleton，委托 Provider 执行加载，管理生命周期事件 | burner `ResManager` |

#### API 速览

```csharp
// 启动时
EmberResourceManager.Instance.Initialize(new AddressablesProvider(), success => { ... });

// 运行时
EmberResourceManager.Instance.LoadAssetAsync<Sprite>("ui/icons/coin", sprite => { ... });
EmberResourceManager.Instance.LoadSceneAsync("Battle");
EmberResourceManager.Instance.UnloadUnusedAssets();
```

#### 生命周期

```
Initialize(provider)
    │
    ├─→ Provider.Initialize()
    └─→ Dispatch(ResourceReady)

销毁时：
    └─→ Dispatch(ResourceShutdown) → UnloadUnusedAssets
```

#### 待扩展

- [x] ~~默认 `ResourcesProvider` 实现（零配置开发入门）~~ → ✅ 已完成
- [x] ~~资源加载句柄（Handle）支持取消和追踪~~ → 📋 [方案已定](../../docs/dev/res-migration-plan.md)（从 burner 提取 AssetHandleSlot + ResFileHandle 模式）
- [ ] **EmberAssetHandle + EmberAssetHandleSlot** — 异步加载句柄 + 加载槽（去重/取消/重入安全）→ [方案 §1](../../docs/dev/res-migration-plan.md#一节步骤-1emberassethandle--emberfilehandle)
- [ ] **EmberFileHandle** — Raw File/Bytes/Text 统一加载句柄 → [方案 §1](../../docs/dev/res-migration-plan.md#一节步骤-1emberassethandle--emberfilehandle)
- [ ] **EmberEventBus.PostNext** — 延迟到下一帧播报事件 + EmberEventGroup 辅助类 → [方案 §2](../../docs/dev/res-migration-plan.md#二步骤-2unievent-vs-embereventbus-对比)
- [ ] 引用计数与自动卸载策略
- [ ] **YooAssetProvider** — 基于 YooAsset 的热更新 Provider（未来 Phase 2） → [方案 §3](../../docs/dev/res-migration-plan.md#三步骤-3yooassetprovider未来待办)

---

### 3. UI 模块 `Ember.UI.Runtime`

> 状态：✅ 已完成
> burner 参考：`Assets/Game/GameLogic/GameManagers/UIFramework/` + `com.burner.uiextension`

#### 3.1 设计思路

UI 模块管理所有界面的**层级关系**和**显示/隐藏切换**。
核心是四个 Canvas 层（每层一个界面栈）+ IEUIView 生命周期的四个阶段。

```
层级：Background(0) → Normal(100) → Popup(200) → TopMost(300)

每层一个栈：
  Push → LoadAssetAsync → Instantiate → PauseTop → OnOpen → 压入栈
  Pop  → OnClose → Destroy → ResumeTop → 弹出栈
```

#### 基础文件清单

| 文件 | 职责 | 参考 |
|------|------|------|
| [Ember.UI.Runtime.asmdef](../../Assets/Ember/UI/Runtime/Ember.UI.Runtime.asmdef) | 程序集定义，依赖 Ember.Core.Runtime | — |
| [IEUIView.cs](../../Assets/Ember/UI/Runtime/IEUIView.cs) | 界面生命周期接口：OnOpen / OnClose / OnPause / OnResume | burner `GameUIBase` |
| [EUIPageDef.cs](../../Assets/Ember/UI/Runtime/EUIPageDef.cs) | 页面元数据定义：预制体路径 + 层级，支持静态注册表 | burner `EUIPageDef` |
| [EUIManager.cs](../../Assets/Ember/UI/Runtime/EUIManager.cs) | UI 管理器：层级 Canvas 按需创建、界面栈推送/Pop、生命周期分发 | burner `GameUIManager` |

#### API 速览

```csharp
// 静态注册表（手写或工具生成）
public static class GamePages
{
    public static readonly EUIPageDef MainMenu = new("ui/main_menu", UILayer.Normal);
    public static readonly EUIPageDef Settings = new("ui/settings",  UILayer.Popup);
    public static readonly EUIPageDef Loading  = new("ui/loading",   UILayer.TopMost);
}

// 打开页面
EUIManager.Instance.Push(GamePages.Settings, args: null);

// 返回键
EUIManager.Instance.Pop(UILayer.Popup);

// 检查有无弹窗
if (EUIManager.Instance.HasView((int)UILayer.Popup)) { ... }
```

#### 生命周期

```
Push(GamePages.Settings, args)
    │
    ├─→ main_menu.OnPause()          ← 被遮挡
    ├─→ EmberResourceManager.LoadAssetAsync(预制体)
    ├─→ Instantiate
    └─→ settings.OnOpen(args)

按返回键 → Pop(UILayer.Popup)
    ├─→ settings.OnClose()
    ├─→ Destroy(settings)
    └─→ main_menu.OnResume()         ← 重新可见
```

---

#### 3.2 两层架构重写 Phase A（2026-08-04 启动，已完成 ✅）

> 📌 **架构演进（2026-08-11）**：本节最初的「EUIManager 框架层 + EUIPageRouter 应用层」两层架构已演进为
> 「**EUIViewEngine（视图引擎/底层） + EUIManager（应用层入口）**」——`EUIPageRouter` 的路由职责已合并进
> `EUIViewEngine`。下方对应关系表中两行的 ember 侧类名已按当前状态更新，完整现状见 [ember-boot-sequence.md](../../docs/dev/ember-boot-sequence.md)。

对 UI 模块进行架构级重构，**采用与 burner 相同的两层架构**：

```
┌─────────────────────────────────────────────┐
│  EUIPageRouter (应用层)                   │
│  ─────────────────────────────               │
│  · PageType 路由分发                          │
│  · Show Queue 顺序打开                        │
│  · 父子页面追踪                               │
│  · BG Mask 模态遮罩管理                       │
│  · Prepare/Finalize 两阶段事件                │
│  · 返回键自动处理                              │
│  · Return Value 页面回传值                    │
├─────────────────────────────────────────────┤
│  EUIManager (框架层)                      │
│  ─────────────────────                       │
│  · Page 生命周期（加载/实例化/销毁）             │
│  · PageContext（MainPage ↔ Popups 关系）      │
│  · Canvas 层管理 + CanvasScaler 适配           │
│  · Update / LateUpdate 统一分发               │
│  · 安全遍历（pendingAdd / pendingDelete）      │
│  · Hide / Restore 机制                       │
│  · 过渡动画管道 (IEUITransitionHandler)        │
│  · Frame Time Budget                         │
├─────────────────────────────────────────────┤
│  IEUIResourceProvider (资源注入层)              │
│  ─────────────────────────                   │
│  · 解耦预制体加载，默认走 EmberResourceManager  │
│  · 支持注入 Mock 实现（测试用）                 │
└─────────────────────────────────────────────┘
```

##### 与 burner 的对应关系

| burner | ember | 定位 |
|--------|-------|------|
| `BurnerUIManager` (com.burner.uiextension) | `EUIViewEngine` (Ember.UI.Runtime) | 框架层/视图引擎：页面生命周期 + 路由 + 资源加载 |
| `GameUIManager` (Assets/Game/...) | `EUIManager` (Ember.UI.Runtime) | 应用层入口：ShowMainPage / SetBackgroundAsync |
| `GameUILogic` | `IEUIView`（扩展） | 页面基类 / 接口 |
| `GameUIBase` | 业务层自行实现 | 游戏特有的 UI 基类（不在框架中） |
| `EUIPageDef` (游戏常量) | `EUIPageDef` + 业务层 `GamePages` | 页面元数据 |

##### 职责边界

**EUIManager（框架层）—— 管"怎么打开/关闭"：** 页面生命周期、PageContext 关系维护、Canvas 管理、安全遍历、Hide/Restore、Update 分发、Frame Time Budget、过渡动画管道、资源加载。

**EUIPageRouter（应用层）—— 管"打开什么/何时打开"：** PageType 路由、Show Queue、父子追踪、BG Mask、两阶段事件、返回键处理、Return Value。

**不做的事（留给实际 Game 项目）：** 相机可见性切换、Volume/PostProcess 控制、页面与音频绑定、引导系统集成、具体页面常量。这些通过事件钩子让业务层自行处理。

##### 新增文件清单

```
Assets/Ember/UI/Runtime/
├── Ember.UI.Runtime.asmdef              ← 保持
├── IEUIView.cs                           ← 扩展：两阶段生命周期（Init→PlayShow→PlayHide→Cleanup）
├── EUIPageDef.cs                           ← 扩展：+PageType 字段
├── EUIEnums.cs                      ← 新增：PageType / PageState / UILayer 枚举
├── EUIManager.cs                    ← 重写：框架层核心引擎
├── EUIPageRouter.cs                 ← 新增：应用层路由
├── EUIPageContext.cs                  ← 新增：MainPage + Popups 关系管理
├── EUIPage.cs                         ← 新增：页面包装类（纯 C#，对标 Burner GamePage，非 MonoBehaviour）
├── EUIBgMaskPool.cs                   ← 新增：模态背景遮罩对象池
├── EUIObserver.cs                   ← 新增：UniRx 门面
├── IEUITransitionHandler.cs              ← 新增：过渡动画接口
├── IEUIResourceProvider.cs               ← 新增：资源加载解耦接口
└── EUIEvents.cs                     ← 新增：UI 事件常量表（Key 5xxx）
```

##### PageType 设计

```csharp
public enum PageType
{
    MainPage,   // 全屏页面，替换当前 MainPage，压入主栈（preserveContext 可选）
    Popup,      // 弹窗，叠加在当前 MainPage 之上，不替换，自动创建 BG Mask
    TopMost,    // 置顶弹窗，高于所有 Popup（如全局提示、Loading）
    SubPage,    // 子页面，嵌入父页面的指定区域（Tab 切换内容等），父关子关
    Overlay,    // 自由排序的覆盖层（如 Guide Mask、点击特效层），不受 MainPage/Popup 栈管理
}
```

`UILayer` 决定**渲染排序**（Canvas.sortingOrder），`PageType` 决定**行为模式**，两者正交。

##### PageContext 核心机制

| 机制 | 说明 |
|------|------|
| **MainPageList + Groups** | 支持多组独立的 MainPage 栈（ember 简化版：先做单组） |
| **PageContextEntry** | 每个页面条目携带：Page + Context（SortingOrder, PlaneDistance, Parameter）+ Popups 列表 |
| **Prepare/Finalize 两阶段** | Prepare = 注册到栈、计算 SortingOrder；Finalize = 资源加载完后真正设置 |
| **HideLowerPage/ShowLowerPage** | 处理 ShouldHideLowerPage 的级联逻辑 |
| **SortingOrder 自动计算** | MainPageOrder=1000, PageGrowStep=500, InitialTopMostOrder=25000 |
| **PlaneDistance** | 控制 Canvas 深度，配合 SortingOrder 保证渲染顺序 |

##### GamePage 核心机制（ember 对应：EUIPage）

| 机制 | 说明 |
|------|------|
| **PageTargetState** | 页面加载/预加载期间的操作挂起队列，加载完成后自动执行 |
| **LoadStages** | 分阶段加载：OnResLoad → OnInit → OnLoad → OnBecomeVisible → Loaded，配合 Frame Time Budget 分帧执行 |
| **RenderVisible** | 独立于 `Visible` 的渲染剔除开关，隐藏 = Canvas.planeDistance = 100000 |
| **ShouldHideLowerPage** | Popup 的模态能力：打开时遮挡下层页面（不销毁），关闭时恢复 |
| **SetActive** | 激活/休眠时统一处理 Canvas + Raycaster + Animator + ParticleSystem |
| **安全遍历** | `isUpdating` 标志 + `pendingDelete` 列表，防止 Update 中修改集合 |
| **延时销毁** | `DestroyValue` + `closeTime`，关闭后延迟 N 秒再真正销毁（用于关闭动画 + 快速重开） |
| **SubPage 管理** | 父页面持有子页面字典，父关子关，排序 order 自动递增 |
| **PageLoadTiming** | 性能分析：记录 AssetLoad / Init / Open 各阶段耗时 |

##### 关键设计决策（与 burner 的差异）

| 决策 | burner | ember | 理由 |
|------|--------|-------|------|
| Logic/View 分离 | `GamePage`（纯 C# 包装）+ `GameUILogic`（纯 C# 逻辑），预制体只有 `GameUIBinding` | `EUIPage`（纯 C# 包装）+ `EUILogic`（纯 C# 逻辑），预制体只有 `EmberUIBinding` | 对齐 Burner 架构，支持热更新 |
| 资源加载 | `CacheManager` + `IResourceHandle`（burner 自己的资源系统） | `IEUIResourceProvider` 注入，默认走 `EmberResourceManager` | ember 已有 Resource 模块，不重复造轮子 |
| 类名解析 | `ILogicResolver` 从 Assembly 扫描类名→类型 | `EmberUIBindingBridge.Attach()` 自动跨程序集查找 Logic 类型并通过 `OnPageCreated` 钩子注入 | 无需手动配置，自动发现 |
| 安全区域 | `BurnerSafeArea` 组件 | Phase C | 不是核心功能 |
| 节点截图/模糊 | `NodePostProcessManager` | 不做 | 属于渲染效果，超出框架范围 |

---

#### 3.3 UIBinding 系统 ✅ 已完成

完整对标 Burner 原版，7 个文件 + 4 个 `.tpl` 模板：

**Runtime：** EmberUIBinding（14 种 WidgetTypes + PageFlags + isPage/pageName/classPath + 继承支持 + Odin 面板）、EmberUIBindingTemplate（绑定配置模板 SO）、EmberUIBindingBridge（自动注册 `OnPageCreated` 钩子，跨程序集查找 Logic 类型并填充 ControlMap）

**Editor：** EmberUIBindingEditor（完整面板：模板加载/保存/复制/粘贴、基类继承、搜索过滤、类型自动检测 + 验证、代码生成入口）、EmberUIBindingEditorUtility（快照/验证/批量收集/EUIPageDef 更新）、LogicImplementationData（代码生成基类 + 自动收集/清除绑定）、CSharpLogicImplementationData（C# 代码生成 + 简易模板引擎支持 `{var}`/`{for}`/`{if}`）、UIBindingSettingData（Project Settings 集成）

**模板：** 4 个 `.tpl` 文件（绑定代码 / 逻辑骨架 / EUIPageDef / 剪贴板），生成 `EUILogic` 子类，使用 `ControlMap["name"] as Type` 模式。

**架构：** 预制体上只有 `EmberUIBinding`（对标 Burner `GameUIBinding`），无 MonoBehaviour 页面类，运行时 `EUIPage`（纯 C# 包装）+ `EUILogic`（生成的逻辑类）全部可热更。

---

#### 3.4 L3 组件封装层 ✅ 已完成

8 个文件：IEUIComponent（接口）、EUIComponent（基类，精简 1447→230 行，去掉 Tween/Attachment/Animator/资源加载）、EUIButton / EUIText / EUIImage / EUIToggle / EUIInputField / EUIProgressBar。

跳过：GameUIContainer（2018 行虚拟列表暂缓）、GameTabLoader/GameUIAttachment/GamePagePreloader（深度耦合资源系统暂缓）。

---

#### 3.5 burner uiextension 包结构参考

burner 的 `com.burner.uiextension@1.0.2` 是一个**完整的 UI 框架包**，以下是完整结构：

```
com.burner.uiextension/
├── Runtime/
│   ├── Manager/                        # 框架层核心
│   │   ├── BurnerUIManager.cs          #    UI 管理器入口
│   │   ├── PageContext.cs              #    MainPage + Popups 关系管理
│   │   ├── GlobalEvents.cs             #    全局事件转发
│   │   ├── ILogicResolver.cs           #    UI 逻辑类发现接口
│   │   └── CacheManager.cs             #    资源缓存
│   ├── Pages/                          # 页面生命周期
│   │   ├── GamePage.cs                 #    页面核心：加载/实例化/显示/隐藏/销毁
│   │   ├── GameUILogic.cs              #    UI 逻辑基类
│   │   ├── IUIBehaviour.cs             #    行为接口
│   │   ├── GameUIBinding.cs            #    代码生成的绑定数据
│   │   └── GameUIBindingTemplate.cs    #    绑定代码模板
│   ├── Components/                     # UI 组件封装层
│   │   ├── GameUIComponent.cs          #    组件基类
│   │   ├── GameButton/Text/Image.cs    #    基础控件封装
│   │   ├── GameScrollRect.cs           #    ScrollRect 封装
│   │   ├── GameTabLoader.cs            #    Tab 切换加载器
│   │   ├── GameProgressBar.cs          #    进度条
│   │   ├── GameToggle.cs / GameToggleGroup.cs
│   │   ├── GameInputField.cs
│   │   ├── GameCanvas.cs / GameUIContainer.cs
│   │   ├── GameUIAttachment.cs         #    动态挂件
│   │   └── GamePagePreloader.cs        #    页面预加载器
│   ├── UIExt/                          # UI 扩展组件
│   │   ├── Tweener/                    #    Tween 动画系统（已删除，改用 DOTween）
│   │   ├── ButtonEx/ImageEx/...        #    扩展控件
│   │   ├── Gradient/AdvancedText/...   #    视觉效果
│   │   └── EventTriggerListener.cs     #    事件触发器
│   ├── Behaviour/                      #    附加行为组件
│   ├── SafeArea/                       #    安全区域适配
│   ├── NodeScreenShot/                 #    节点截图/模糊
│   └── Utils/                          #    工具类
└── Editor/                             # 编辑器工具
    ├── Pages/GameUIBindingEditor.cs    #    绑定编辑器（核心）
    ├── UIExt/                          #    各组件 Inspector
    ├── Previews/                       #    UI 预览
    ├── Validation/                     #    Prefab 校验
    └── Bake/                           #    UI 烘焙
```

##### 分阶段计划总览

```
Phase A: 框架层核心 ✅ 已完成
  对应 burner Manager + Pages 目录
  EUIPage / PageContext / UIManager / UIPageRouter / UIEvents /
  IEUIView（扩展）/ EUIPageDef（扩展）/ BgMaskPool / UITransitionHandler / UIResourceProvider

Phase B: 组件封装层 ✅ 已完成
  对应 burner Components/ 目录（精简版）
  EUIComponent / EmberButton / EmberText / EmberImage / EmberScrollRect 等

Phase C: 扩展 + 编辑器 ⬜ 待开始
  对应 burner UIExt/ + Editor/ 目录
  EmberTweener（改用 DOTween）/ EUISafeArea / UIBinding 代码生成 / 各组件 Inspector
```

---

### 4. Scene 模块 `Ember.Scene.Runtime`

> 状态：✅ 已完成
> burner 参考：`Assets/Game/GameLogic/GameManagers/GameScene/`

#### 设计思路

Scene 模块封装 Unity SceneManager，提供异步加载/卸载、激活前回调、过渡切换。
核心是基于协程的进度轮询 + `allowSceneActivation` 机制，
在场景加载到 90% 时触发 `OnBeforeActivate`，允许模块在激活前做初始化。

#### 文件清单

| 文件 | 职责 | 参考 |
|------|------|------|
| [EmberSceneManager.cs](../../Assets/Ember/Scene/Runtime/EmberSceneManager.cs) | 场景管理器：UniTask 异步加载/卸载/过渡，Progress 真实进度，OnBeforeActivate 回调 | burner `GameSceneManager` |
| [SceneCoordinator.cs](../../Assets/Ember/Scene/Runtime/SceneCoordinator.cs) | 状态机↔场景桥接器：注入 OnSceneTransition 钩子，自动加载/卸载状态对应场景 | — |

#### API 速览

```csharp
// 叠加加载
EmberSceneManager.Instance.LoadSceneAsync("Battle", () => Debug.Log("就绪"));

// 切换（加载新 + 卸载旧）
EmberSceneManager.Instance.TransitionTo("Battle", "MainMenu");

// 激活前回调（初始化时机）
EmberSceneManager.Instance.OnBeforeActivate += (scene, activate) =>
{
    // 初始化操作...
    activate(); // 完成后激活场景
};
```

#### 生命周期

```
LoadSceneAsync("Battle")
    │
    ├─→ Progress: 0.0 → 0.9
    ├─→ OnBeforeActivate(scene, activate)
    ├─→ activate()  ← 由业务层调用
    ├─→ Progress: 1.0
    └─→ Dispatch(SceneLoaded)
```

---

### 5. Audio 模块 `Ember.Audio.Runtime`

> 状态：✅ 已完成（基础版） → 📋 [升级方案](../../docs/dev/audio-upgrade-plan.md) 待实施
> burner 参考：`Assets/Game/GameLogic/GameManagers/Audio/` + `Assets/Game/GameCore/Runtime/Common/Audio/`

#### 文件清单

| 文件 | 职责 | 参考 |
|------|------|------|
| [Ember.Audio.Runtime.asmdef](../../Assets/Ember/Audio/Runtime/Ember.Audio.Runtime.asmdef) | 程序集定义，依赖 Ember.Core.Runtime | — |
| [EmberAudioManager.cs](../../Assets/Ember/Audio/Runtime/EmberAudioManager.cs) | 音频管理器：BGM/SFX 分离、Mixer 音量控制 | burner `AudioMgr` |

#### API 速览

```csharp
EmberAudioManager.Instance.Init(mixer);
EmberAudioManager.Instance.PlayBGM(bgmClip, loop: true);
EmberAudioManager.Instance.PlaySFX(sfxClip);
EmberAudioManager.Instance.SetBGMVolume(0.8f);
```

#### 已知局限 & 升级计划

| 局限 | 升级方案 |
|------|---------|
| SFX 每次 `AddComponent` 产生 GC | AudioAgent 池化，预创建 + SetActive 复用 |
| 无法按 Sound/Music/Voice 分类控制 | `AudioType` 枚举 + `AudioCategory` 分类管理器 |
| 无法按 ID 停止单个播放实例 | `AudioAgent.InstanceId` + `StopAgent(id)` |
| fade 参数未实现 | AudioAgent 内置 fade in/out |
| 无并发数量上限控制 | `AudioGroupConfig.AgentHelperCount` |

详见 [docs/dev/audio-upgrade-plan.md](../../docs/dev/audio-upgrade-plan.md)。

---

### 6. Camera 模块 `Ember.Camera.Runtime`

> 状态：✅ 已完成

#### 文件清单

| 文件 | 职责 |
|------|------|
| [EmberCameraManager.cs](../../Assets/Ember/Camera/Runtime/EmberCameraManager.cs) | 相机管理器：Cinemachine 虚拟相机注册/切换、强制霸占堆栈多重嵌套、锁定模式 |

#### API 速览

```csharp
// 注册相机
EmberCameraManager.Instance.Register("main", mainVCam);
// 普通切换
EmberCameraManager.Instance.Switch("battle");
// 强制霸占（对话 → Timeline 嵌套接管）
EmberCameraManager.Instance.PushOverride("dialogue");
EmberCameraManager.Instance.PopOverride();
```

#### 设计要点

- **霸占堆栈**：支持多重嵌套（Normal → Dialogue → Cutscene），Push/Pop 配对
- **锁定模式**：Lock() 后拒绝普通 Switch，霸占不受影响
- **Cinemachine 集成**：自动配置 Brain + BlenderSettings

---

### 7. Input 模块 `Ember.Input.Runtime`

> 状态：✅ 已完成
> burner 参考：Unity Input System 封装

#### 文件清单

| 文件 | 职责 | 参考 |
|------|------|------|
| [Ember.Input.Runtime.asmdef](../../Assets/Ember/Input/Runtime/Ember.Input.Runtime.asmdef) | 程序集定义，依赖 Ember.Core.Runtime | — |
| [EmberInputManager.cs](../../Assets/Ember/Input/Runtime/EmberInputManager.cs) | 输入管理器：Action Map 切换、GetAxis/IsPressed | Unity Input System |

#### API 速览

```csharp
EmberInputManager.Instance.Init(inputActions, defaultMap: "Gameplay");
EmberInputManager.Instance.SwitchMap("UI");
var move = EmberInputManager.Instance.GetAxis("Move");
if (EmberInputManager.Instance.IsPressed("Jump")) { ... }
```

---

### 8. Editor 模块 `Ember.Editor`

> 状态：✅ 已完成

#### 文件清单

| 文件 | 职责 |
|------|------|
| [EmberSceneMapping.cs](../../Assets/Ember/Editor/EmberSceneMapping.cs) | 状态↔场景映射 SO：自动扫描 EmberGameState 子类 + 匹配同名场景 |
| [EmberSceneMappingCreator.cs](../../Assets/Ember/Editor/EmberSceneMappingCreator.cs) | 映射 SO 自动创建（InitializeOnLoad） |
| [EmberSceneQuickOpener.cs](../../Assets/Ember/Editor/EmberSceneQuickOpener.cs) | 快速场景打开器：Toolbar 窗口，主场景互斥 + 叠加场景多选 |
| [FrameworkSceneToolbarButton.cs](../../Assets/Ember/Editor/FrameworkSceneToolbarButton.cs) | Toolbar 按钮：FrameworkScene 跳转 + 快速打开场景 |
| [OdinIntegrationTest.cs](../../Assets/Ember/Editor/OdinIntegrationTest.cs) | Odin Inspector 集成检测 |

---

## 核心系统

### 9. Manager 自动发现 `Ember.Core.Runtime`

> 状态：✅ 已完成
> burner 参考：`GameMgrCollector` + `IManager` + `[InitOrder]`

| 文件 | 职责 | 参考 |
|------|------|------|
| [IEmberManager.cs](../../Assets/Ember/Core/Runtime/Manager/IEmberManager.cs) | 框架管道接口：Init() / Destroy()，启动时由 Collector 初始化 | burner `IManager` |
| [IEmberModule.cs](../../Assets/Ember/Core/Runtime/Manager/IEmberModule.cs) | 业务模块接口：OnInit() / OnDestroy() / ResetModuleData()，由状态机按 Phase 驱动 | — |
| [EmberInitOrderAttribute.cs](../../Assets/Ember/Core/Runtime/Manager/EmberInitOrderAttribute.cs) | 初始化顺序特性，预定义 Core=100 → Game=700 | burner `[InitOrder]` |
| [EmberManagerCollector.cs](../../Assets/Ember/Core/Runtime/Manager/EmberManagerCollector.cs) | 反射扫描 → 按 Order 排序 → 依次 Init / 逆序 Destroy | burner `GameMgrCollector` |

---

### 10. Update 循环管理器 `Ember.Core.Runtime`

> 状态：✅ 已完成
> burner 参考：`GameUpdateManager` + `IGameUpdate`

| 文件 | 职责 | 参考 |
|------|------|------|
| [IEmberUpdate.cs](../../Assets/Ember/Core/Runtime/Update/IEmberUpdate.cs) | IEmberUpdate / IEmberLateUpdate / IEmberFixedUpdate 接口 | burner `IGameUpdate` |
| [EmberUpdateManager.cs](../../Assets/Ember/Core/Runtime/Update/EmberUpdateManager.cs) | 反射扫描 + 每帧统一驱动所有 IEmberUpdate | burner `GameUpdateManager` |

---

### 11. GameState 状态机 `Ember.Core.Runtime`

> 状态：✅ 已完成
> burner 参考：`GameStateManager` + `GameStateBase`

#### 核心三状态体系

框架强制提供三个核心状态，覆盖单机/网游通用流程：

```
                 ┌──────────────────────────┐
                 │        Settings          │
                 │   (Push 覆盖，可删除)      │
                 └────┬─────────────────┬───┘
                    Push               Push
                      ↑                 ↑
Init ──→ [Login] ──→ Main ←──→ Gameplay ──→ Main ──→ ...
 框架     网游可选     大厅      核心玩法      回到大厅
```

| 状态 | 文件 | 职责 | IsRequired |
|------|------|------|------------|
| **Init** | [InitState.cs](../../Assets/Ember/Core/Runtime/State/InitState.cs) | 初始化所有 Manager，广播 CoreReady，自动 TransitionTo Main | ✅ |
| **Main** | [MainState.cs](../../Assets/Ember/Core/Runtime/State/MainState.cs) | 大厅/主界面枢纽。Init 完成后的着陆点，退出 Gameplay 后的归宿 | — |
| **Gameplay** | [GameplayState.cs](../../Assets/Ember/Core/Runtime/State/GameplayState.cs) | 核心玩法循环。OnEnter/OnExit/OnUpdate/OnPause/OnResume 完整生命周期 | — |

#### 子类化模式

三个状态都采用"密封外层 + 虚内层"模式，子类不需要关心日志和事件广播：

```csharp
// MainState 示例
public class MyMainState : MainState
{
    protected override void OnMainEnter(object args) { /* 显示主界面、播放 BGM */ }
    protected override void OnMainExit() { /* 隐藏主界面 */ }
}

// GameplayState 示例
public class BattleState : GameplayState
{
    protected override void OnGameplayEnter(object args) { /* 加载战斗场景 */ }
    protected override void OnGameplayUpdate() { /* 战斗主循环 */ }
    protected override void OnGameplayPause() { /* 暂停战斗 */ }
    protected override void OnGameplayResume() { /* 继续战斗 */ }
    protected override void OnGameplayExit() { /* 卸载战斗场景 */ }
}
```

#### 文件清单

| 文件 | 职责 | 参考 |
|------|------|------|
| [EmberStateMachine.cs](../../Assets/Ember/Core/Runtime/State/EmberStateMachine.cs) | 状态机引擎 + EmberGameState 抽象基类 | burner `GameStateManager` |
| [InitState.cs](../../Assets/Ember/Core/Runtime/State/InitState.cs) | 框架内置必需状态（IsRequired = true），自动过渡到 MainState | — |
| [MainState.cs](../../Assets/Ember/Core/Runtime/State/MainState.cs) | 大厅/主界面状态 | — |
| [GameplayState.cs](../../Assets/Ember/Core/Runtime/State/GameplayState.cs) | 核心玩法状态 | — |
| [SettingsState.cs](../../Assets/Ember/Core/Runtime/State/SettingsState.cs) | 通用覆盖式设置状态，通过 SettingsContext 区分 Main/Gameplay 上下文 | — |

#### 设计要点

**TransitionTo vs Push/Pop：**

```csharp
// TransitionTo：替换式切换（Init → Main, Main → Gameplay）
fsm.TransitionTo<GameplayState>();

// Push/Pop：覆盖式弹窗（暂停 Gameplay 打开设置）
fsm.Push<SettingsState>();
fsm.Pop();
```

**图形化编辑器预留：**
- `RegisteredStates` 返回所有已注册状态（反射枚举）
- `Name` / `Description` 给编辑器展示
- `IsRequired = true` 的状态不可注销
- `Unregister<T>()` 拒绝删除必需状态和当前活跃状态

---

### 12. 日志系统 `Ember.Core.Runtime`

> 状态：✅ 已完成
> burner 参考：`Debuger`
> 详细文档：[docs/dev/ember-debug.md](../../docs/dev/ember-debug.md)

#### 文件清单

| 文件 | 职责 |
|------|------|
| [EmberDebug.cs](../../Packages/com.ember.basic/Runtime/Debug/EmberDebug.cs) | 日志核心：消息分色 + 两级标签级联过滤 + 文件日志双通道 |
| [EmberLogPresets.cs](../../Packages/com.ember.basic/Runtime/Debug/EmberLogPresets.cs) | 集中定义：LogTags、LogTagColors、LogColors |
| [EmberDebugConfigSO.cs](../../Packages/com.ember.basic/Runtime/Debug/EmberDebugConfigSO.cs) | SO 配置容器：全局开关、按类过滤、颜色管理、文件日志参数 |
| [EmberFileLog.cs](../../Packages/com.ember.basic/Runtime/Debug/EmberFileLog.cs) | 文件日志持久化：异步后台线程 + 批量写入 + 日志轮转 + 过期清理 |
| [IEmberLogUploader.cs](../../Packages/com.ember.basic/Runtime/Debug/IEmberLogUploader.cs) | 日志上传接口：业务层实现以对接自有服务端 |
| [EmberDebugConfigEditor.cs](../../Packages/com.ember.basic/Editor/EmberDebugConfigEditor.cs) | SO 自定义 Inspector |
| [EmberDebugConfigCreator.cs](../../Packages/com.ember.basic/Editor/EmberDebugConfigCreator.cs) | 自动创建 SO |

> 以上文件已从 `Assets/Ember/Core/` 迁移到 `Packages/com.ember.basic/`。

#### API 速览

```csharp
private const string TAG = LogTags.CoreEventBus;

EmberDebug.Log(TAG, "普通消息");           // 白色
EmberDebug.LogInit(TAG, "初始化完成");      // 绿色
EmberDebug.LogEvent(TAG, "事件播报");       // 紫色
EmberDebug.LogCleanup(TAG, "清理资源");     // 灰色
EmberDebug.LogWarning(TAG, "异常");         // 白色+黄底
EmberDebug.LogError(TAG, "错误");           // 白色+红底，不受开关控制

// 过滤
EmberDebug.Disable(LogTags.Audio);          // 父标签关闭 → 所有子标签静默
EmberDebug.Disable(LogTags.CoreEventBus);   // 只关子标签
EmberDebug.GlobalOpen = false;              // 全关（Error 除外）
```

---

### 13. Timer 定时器 `Ember.Core.Runtime`

> 状态：✅ 已完成（2026-08-17）
> burner 参考：`TimerManage`
> 实现位置：[EmberTimerManager.cs](../../Assets/Ember/Core/Runtime/Time/EmberTimerManager.cs)

- int-ID API（Delay / Interval / Schedule / Cancel），0 视为无效 ID，Cancel 统一取消
- 时间源来自 EmberTimeManager：逻辑时间（受 TimeScale/Pause 影响）或真实时间，由 useLogicTime 选择
- 由 EmberUpdateManager 每帧自动驱动（IEmberUpdate），无需挂 MonoBehaviour、无需手动 Tick
- delta 累加驱动，不依赖 UniTask —— 能正确响应 EmberTimeManager 独立于 Unity timeScale 的 TimeScale/Pause

> 📌 **位置调整说明**：原计划放 com.ember.extensions（理由「避免 Core 依赖 UniTask」）。实现时发现定时器
> 依赖 EmberTimeManager / IEmberUpdate / EmberSingleton，均属 Core，且 delta 累加根本不需要 UniTask。
> 故下沉到 Core/Time/，与 EmberTimeManager 并列。原「避免 Core 依赖 UniTask」的理由已过时（Core 早已用 UniTask）。

---

## Module 系统（已完成基础版 ✅）

### 问题

`IEmberManager` 只覆盖"应用启动即初始化"的框架管道（7 个 Manager）。
业务模块（战斗、背包、网络）需要由状态机按需驱动 —— 进入 BattleState 才初始化，退出时销毁。

### 两层初始化模型

```
┌────────────────────────────────────────────┐
│  IEmberManager (框架管道)                    │
│  ────────────────────────                   │
│  启动 → InitializeAll() → 全局存活 → 退出销毁  │
│  EmberUpdateManager / Resource / Audio ...  │
│                                             │
│  IEmberModule (业务模块)                     │
│  ────────────────────────                   │
│  Phase 1 → Login 后初始化                    │
│  Phase 2 → 进 Battle/MainMenu 初始化          │
│  状态退出 → OnDestroy / ResetModuleData      │
└────────────────────────────────────────────┘
```

### 接口定义

| 接口 | 文件 | 定位 |
|------|------|------|
| `IEmberManager` | [IEmberManager.cs](../../Assets/Ember/Core/Runtime/Manager/IEmberManager.cs) | 框架管道，启动时初始化 |
| `IEmberModule` | [IEmberModule.cs](../../Assets/Ember/Core/Runtime/Manager/IEmberModule.cs) | 业务模块，状态机驱动 |

两者**不继承**——EmberManagerCollector 只扫 `IEmberManager`，EmberModuleCollector 只扫 `IEmberModule`，互不干扰。

### 初始化流程

```
GameLauncher.Awake()
│
└─ Fsm.Start<InitState>()
      │
      └─ InitState.OnEnter()
           ├─ EmberManagerCollector.InitializeAll()               ← 框架管道
           ├─ EmberModuleCollector.InitPhase(ModulePhase.Global)   ← 全局业务模块
           └─ TransitionTo<MainState>()                            ← 加载 MainScene

游戏退出（GameLauncher.ShutdownFramework）
      └─ EmberModuleCollector.DestroyAll()                         ← 销毁全部模块
```

### 已完成（2026-08-17）

- [x] `ModulePhase` 常量：Framework(0) / Global(1) / Main(2) / Gameplay(3)
- [x] `EmberModuleCollector`：反射扫描 IEmberModule，按 Phase 分组，InitPhase / DestroyPhase / DestroyAll
- [x] 热重启：再次 InitPhase 时先 ResetModuleData 再 OnInit（对象复用）
- [x] 首个业务模块 `PlayerPrefsModule`（Global 阶段，Init 启动 / 退出销毁）

### 文件清单

| 文件 | 职责 |
|------|------|
| [ModulePhase.cs](../../Assets/Ember/Core/Runtime/Manager/ModulePhase.cs) | 阶段常量 |
| [EmberModuleCollector.cs](../../Assets/Ember/Core/Runtime/Manager/EmberModuleCollector.cs) | 反射扫描 + 按 Phase 驱动生命周期 |
| [PlayerPrefsModule.cs](../../Assets/Game/Module/PlayerPrefsModule.cs) | 首个业务模块：封装 UnityEngine.PlayerPrefs |

> Main / Gameplay 阶段的接线（`InitPhase(ModulePhase.Main)` 等）待对应业务模块出现时接入。

---

## Package 迁移

> 启动：2026-08-04 | basic ✅ | extensions ✅ | uiextension ⬜ 待开始

### 背景

项目 `Packages/` 下已复制了 burner 的三个包，全部代码均已注释（`////`），等待适配：

| 包 | 文件数 | 来源 | 状态 |
|---|--------|------|------|
| `com.ember.basic` | 36 cs | `com.burner.basic` | ✅ 已完成（2026-08-04） |
| `com.ember.extensions` | 17 cs → **5 cs** | `com.burner.extensions` | ✅ 已完成（2026-08-06），删除 12 无用文件 |
| `com.ember.uiextension` | 80+ cs | `com.burner.uiextension` | ⬜ 全部注释，待适配 |

### 迁移策略

采用**逐文件审计**方式，而不是批量取消注释。每个脚本走以下流程：

```
1. 阅读 burner 原始代码 → 理解用途
2. 判断：是否需要？ → 否 → 删除文件
3. 判断：是否需要修改？ → 记录优化点
4. 取消注释 → 改命名空间 → 适配 ember 规范
5. 更新 asmdef 引用（如需要）
6. 标记完成
```

### 迁移规范

| 规则 | burner 原始 | ember 目标 |
|------|------------|-----------|
| 命名空间 | `Burner.Basic` / `Burner.Extensions` | `Ember.Basic` / `Ember.Extensions` |
| 日志 | `Burner.Logger` / `Debuger.Log` | `EmberDebug.Log`（框架代码）或 `UnityEngine.Debug`（基础工具类保留） |
| 类名前缀 | 无统一前缀 | 工具类保持原名，框架类加 `Ember` 前缀 |
| 版权头 | burner copyright | 替换为 ember copyright |
| 依赖 | `Burner.Basic.Tasks` 等 | 改为 ember 对应包 |
| 单例模式 | `Singleton<T>`（纯 C#） | 已有 `EmberSingleton<T>`，评估是否重复 |
| 对象池 | `ListPool<T>` 等 | 评估是否需要；ember Core 已有 `EmberObjectPool` |

---

### 1. com.ember.basic（36 个文件）✅ 已完成

#### 目录结构

```
com.ember.basic/Runtime/
├── Async/                          # STTask 异步系统（7 文件）
├── Base/                           # 基础数据结构（11 文件）
│   ├── Attributes.cs               #   HasGC / NoGC / ForTest / ForDebug 标记
│   ├── ListPool.cs / DictionaryPool.cs / HashSetPool.cs  # 集合池
│   ├── MemoryPool.cs               #   内存池
│   ├── PoolRefCount.cs             #   池引用计数（Editor 调试用）
│   ├── IPool.cs                    #   池接口
│   ├── QuickQueue.cs               #   快速队列
│   ├── StringView.cs               #   字符串视图（零分配子串）
│   ├── ValueTypeList.cs            #   值类型 List（unsafe）
│   └── CacheSortedList.cs          #   缓存排序列表
├── Extension/                      # 扩展方法（4 文件）
├── LitJson/                        # JSON 库（8 文件）
├── Resource/                       # 资源接口（1 文件）
├── Unsafe/                         # 不安全代码（2 文件）
└── Utils/                          # 工具（1 文件）
```

#### 迁移变更汇总

| 目录 | 文件数 | 主要变更 |
|------|--------|---------|
| `Base/` | 11 | 命名空间, API 重命名(Pop→Get, Push→Return), 移除 burner wiki 链接, 错误消息 ember 化, StringView 去除 StringExtension 依赖, CacheSortedList FixRemove 重构 |
| `Extension/` | 4 | 命名空间, Implode→JoinToString, 移除 ParallelForEach 中 burner 异常消息, Il2Cpp polyfill 保持原命名空间 |
| `Async/` | 7 | 命名空间, 字段名 `_camelCase`, 异常消息 ember 化 |
| `LitJson/` | 8 | 命名空间, 表达式体属性语法, 移除 NETSTANDARD1_5 条件编译 |
| `Resource/` | 1 | 命名空间, XML 文档, IUpdater+IDelayDisposable |
| `Unsafe/` | 2 | 命名空间, 字段 PascalCase, 方法 PascalCase, UnsafeString 放入 Ember.Basic |
| `Utils/` | 1 | 重命名 Const→SharedConst, 字段语义化命名 |

#### 迁移注意事项

- **命名空间统一**：`Burner.Basic` → `Ember.Basic`
- **asmdef 引用**：`com.ember.basic` 应零依赖（除 Unity 引擎），不依赖 `Ember.Core.Runtime`
- **日志处理**：basic 包是底层工具库，应避免依赖 `EmberDebug`。保留使用 `UnityEngine.Debug` 或提供可注入的 `LogAction`
- **与现有 ember 代码的冲突检查**：`Ember.Core.Runtime` 中已有 `EmberObjectPool`、`EmberSingleton`，确认 basic 包的工具类不与之重叠

---

### 2. com.ember.extensions（5 个文件）✅ 已完成

> 原 17 个注释文件 → 逐文件审计 → 删除 12 个 → 迁移 5 个 + 已有 1 个 = **6 个活跃源文件**

#### 删除清单（12 个）

| 文件 | 删除理由 |
|------|---------|
| Async/STTaskFactory.cs | STTask WhenAll/WhenAny，Ember 用 UniTask |
| Async/SingleThreadSynchronizationContext.cs | STTask 同步上下文，Ember 用 UniTask |
| Base/Singleton.cs | 与 `EmberSingleton<T>` 重复，且 ember 版更优 |
| Base/ThreadPool.cs | 自定义线程池，Unity 中不推荐 |
| Resource/CacheManager.cs | 深度耦合 Burner 资源系统（IResourceProxy + MemoryPool） |
| Resource/ILoaderHandle.cs | 2 方法接口，无独立价值 |
| Resource/IResourceLoader.cs | 依赖 IUpdater + 仅被一个类实现 |
| Resource/IResourceProxy.cs | Burner 资源系统核心，ember 已有 `IResourceProvider` |
| Resource/ResourceLoader.cs | 依赖不存在的 ResourceManager |
| Utils/CachedIntPtrStrings.cs | 极专用（原生内存字符串驻留），需要时可从 burner 恢复 |
| Utils/FieldsInitializer.cs | 被 Burner 团队标记 `[Obsolete]` |
| Utils/GameObjectUtils.cs | 寥寥数行（SetLayer/GetGameObjectByName），按需新写 |
| Utils/Utility.cs | 853 行大杂烩（MD5/URL/随机/文件/Shell），需要时拆分 |

#### 迁移清单（5 个）

| 文件 | 说明 | 适配变更 |
|------|------|---------|
| Base/CacheLRUList.cs | 零 GC LRU 数据结构（O(1) push/pop + 内部节点头池） | namespace, [Burner]→[Ember], 去 wiki 链接, 英文文档注释 |
| Extension/UnityExtension.cs | Transform/GameObject/RectTransform 扩展方法（14 个） | namespace, 移除 legacy 方法, 移除 WX 游戏 UniUnload, 加 `[NoGC]`/`[HasGC]` 标注, XML 文档注释 |
| Extension/GameObjectComponentExtensions.cs | GetOrAddComponent 扩展（2 个重载） | 已激活（basic 迁移时创建），无需变更 |
| Utils/JsonUtils.cs | LitJson 反射序列化（`[JsonProp]` + 序列化回调） | namespace, Burner.Basic→Ember.Basic, [Burner]→[Ember], 扩展方法独立为 JsonDataExtensions 类 |
| Utils/StreamHelper.cs | 二进制 I/O 框架（3 种后端: Memory/IntPtr/Bytes + 7-bit 编码） | namespace, [Burner]→[Ember], `str.IsNullOrEmpty()`→`string.IsNullOrEmpty()`, `Mathf.Min`→`Math.Min` |

#### 最终目录结构

```
com.ember.extensions/Runtime/
├── Base/
│   └── CacheLRUList.cs                   # 零 GC LRU 缓存列表
├── Extension/
│   ├── GameObjectComponentExtensions.cs  # GetOrAddComponent 扩展
│   └── UnityExtension.cs                 # Transform/GameObject 扩展方法
└── Utils/
    ├── JsonUtils.cs                      # LitJson 反射序列化
    └── StreamHelper.cs                   # 二进制 I/O 框架
```

---

### 3. com.ember.uiextension（80+ 个文件）

> 状态：⬜ 待开始（basic + extensions 包完成后启动）
> 注意：此包的迁移与 UI 模块重写密切相关，届时需要协调

---

## 程序集依赖图

```
Ember.Core.Runtime          (叶子，依赖: UnityEngine + Odin + UniTask)
    ↑
    ├── Ember.Resource.Runtime
    │       ↑
    │       └── Ember.Scene.Runtime
    ├── Ember.UI.Runtime
    │       ↑
    │       └── Ember.Resource.Runtime（预制体加载）
    ├── Ember.Audio.Runtime
    ├── Ember.Camera.Runtime（额外依赖: Unity.Cinemachine）
    └── Ember.Input.Runtime
```

---

## 场景集成验证 — S1-S9 全部完成 ✅

> 当前进度：S1-S9 全部通过，Init → Main 场景加载链路正常，LoadingPage + BootSplash 防护就绪。

### 阶段目标

| 序号 | 任务 | 状态 |
|------|------|------|
| **S1** | 搭建 GameBoot 场景 | ✅ |
| **S2** | 验证 Manager 初始化链路 | ✅ 8 个 Manager（含 SceneCoordinator） |
| **S3** | 验证 Update 循环 | ✅ |
| **S4** | 验证事件总线 | ✅ |
| **S5** | 验证/修复日志系统 | ✅ GlobalOpen + 实时生效 + LogShutdown |
| **S6** | 验证相机模块 | ✅ UICamera=OK, MainCamera=OK, Brain=OK |
| **S7** | 整理遗留问题 | ✅ |
| **S8** | LoadingPage + BootSplash | ✅ 双遮罩：BootSplash（启屏黑幕）+ LoadingView（场景切换进度） |
| **S9** | EUIManager 完善 | ✅ EnsureLayerRoot 自动挂载 Canvas 三件套 |

### 架构快照（2026-08-03）

```
FrameworkScene.unity（启动场景，index 0，永不卸载）
  └── GameBoot
      ├── GameLauncher（EmberMonoSingleton）
      ├── GameBootCoordinator（可选）
      ├── UIRoot
      │     ├── BootSplash（EUIBootSplash，Frame 0 黑幕）
      │     └── LoadingPage（EUILoading，进度条）
      ├── MainCamera（CinemachineBrain + DefaultCinemachineCamera）
      ├── UICamera（Overlay）
      └── EventSystem

启动流程：
  1. FrameworkScene 加载 → BootSplash Frame 0 黑幕
  2. GameLauncher.Start → Fsm.Start<InitState>
  3. InitState → InitializeAll → LoadSceneAsync("MainScene") → InitSceneReady
  4. EUIMainAnimationStarter（MainScene 上）→ InitAnimationDone
  5. TransitionTo<MainState>(skipSceneLoad) → BootSplash 销毁
  6. Main→Gameplay：LoadingView 进度条 → 场景切换

场景加载：
  Init ──(LoadSceneAsync预加载)──→ Main ──(TransitionTo正常加载)──→ Gameplay
  FrameworkScene 始终常驻，其余 Additive 叠加/卸载

关键文件：
  Assets/Ember/Core/Runtime/GameLauncher.cs              — 入口 + 状态机
  Assets/Ember/Core/Runtime/State/InitState.cs           — 预加载 MainScene + InitSceneReady/AnimationDone
  Assets/Ember/Core/Runtime/State/MainState.cs            — 大厅状态
  Assets/Ember/Core/Runtime/State/GameplayState.cs        — 玩法状态
  Assets/Ember/Core/Runtime/State/SettingsState.cs        — 覆盖式设置
  Assets/Ember/Core/Runtime/State/EmberStateMachine.cs    — 状态机 + TransitionTo(skipSceneLoad) + LoadSceneAsync
  Assets/Ember/Core/Runtime/State/TransitionDescriptor.cs — 流转描述符
  Assets/Ember/Core/Runtime/Event/EmberEventBus.cs        — 事件总线
  Assets/Ember/Scene/Runtime/SceneCoordinator.cs          — 场景加载桥接
  Assets/Ember/Scene/Runtime/EmberSceneManager.cs         — 场景异步加载（UniTask）
  Assets/Game/UI/EUIBootSplash.cs                       — 启屏黑幕
  Assets/Ember/UI/Runtime/Pages/EUILoading.cs               — 加载进度
  Assets/Game/UI/EUIMainAnimationStarter.cs                — 启动动画基类
```

---

## 技术债务 & 后续规划

> 最后更新：2026-08-17

### 🔴 待修改（影响架构）

> ✅ 已全部解决（2026-08-18）：
> 1. EmberSceneManager 场景加载走 EmberResourceManager.LoadSceneAsync（IResourceProvider.LoadSceneAsync 改为返回 AsyncOperation + 支持 LoadSceneMode）
> 2. IResourceProvider 注册进 EmberServiceLocator（框架内部用 Instance、外部后端用 ServiceLocator）
> 3. 移除 EmberStateMachine 冗余的 OnStateChanged C# 事件（与 GameStateChanged 广播重复）

### 🟡 待补完（功能完整度）

| # | 事项 | 说明 |
|---|------|------|
| 3 | **Module 系统** | ✅ 已实现：ModulePhase + EmberModuleCollector + PlayerPrefsModule |
| 4 | **ResourcesProvider 异步化** | `LoadAssetAsync` 实际同步，应加真正异步 |
| 5 | **Timer 定时器** | ✅ 已实现：EmberTimerManager（Core/Time） |
| 6 | **UI Pop 动画** | 淡入淡出、滑动过渡 |

### 🟢 待扩展（增强项）

| # | 事项 |
|---|------|
| 7 | Audio 多 Category + AudioAgent 池（[方案](../../docs/dev/audio-upgrade-plan.md)） |
| 8 | GameObject 预制体对象池 |
| 9 | 本地化 |
| 10 | **DataSaver 异步版** — `SaveAsync` / `TryLoadAsync` 需 UniTask，放入 `com.ember.extensions` |

### 📋 后续想法（待评估）

| # | 事项 |
|---|------|
| — | **状态机流转图可视化** — 读取 `GetTransitions()` / `GetPushTargets()` 构建节点图，在 EditorWindow 中可拖拽查看 |
| — | **必要状态视觉区分** — `IsRequired = true` 的节点以不同样式渲染（锁图标、加粗边框） |
| — | **LoadingPage 预制体化** — 当前为 FrameworkScene 中常驻 GameObject，未来改为预制体 + `EUIManager.Push/Pop` 动态加载 |
| — | **Init 启动动画** — `EUIMainAnimationStarter` 基类已创建，子类 override `PlayStartupAnimation` 即可 |
| — | **新建状态时自动关联场景** — `EmberSceneMappingCreator` 已自动创建 SO + 匹配同名场景。未来可视化编辑器创建新状态时需先创建场景后创建状态 |
| — | **Settings UI 集成** — `SettingsState` 状态已创建，待实现 UI 层：根据 `SettingsContext` 展示不同选项面板 |
| — | **开源图标资源已入库** — `Assets/Art/Icons/game-icon-pack-v1.4/` — 800+ 圆角图标，CC0 许可证。后续蓝图节点图标 / 编辑器工具栏图标优先从这里取 |
| — | **EUIPageDef 按模块拆分注册** — 已部分落地（2026-08-26）：框架页面/用户页面拆分为 `GamePages.cs` + `GamePages.User.cs`（partial 拼接，codegen 写 User 文件）；彻底按模块拆分 `*Pages.cs`（生成器按 `classPath` 自动定位）仍待 UI 预制体目录结构稳定后实施 |
| — | Wwise 适配 |
| — | 图片/纹理管理 |

### ✅ 已解决（S1-S9 验证期间修复）

| # | 事项 | 修复日期 |
|---|------|----------|
| 1 | 现有 Manager 实现 IEmberManager（5 个 Manager + EmberUpdateManager） | 2026-07-31 |
| 2 | EmberUpdateManager 去 MonoBehaviour → 纯 C# 类 | 2026-07-31 |
| 3 | GameLauncher 集中入口（驱动 Update/Manager/StateMachine） | 2026-07-31 |
| 4 | EmberDebug GlobalOpen 不抑制 LogInit | 2026-08-03 |
| 5 | EmberDebug 运行时 SO 修改不实时生效 | 2026-08-03 |
| 6 | EmberDebug Disable/Enable 只改缓存不同步 SO | 2026-08-03 |
| 7 | C# 9 `init` 访问器需要 IsExternalInit polyfill | 2026-08-03 |

---

## 编码规范速查

| 规则 | 示例 |
|------|------|
| 框架类前缀 | `EmberEventBus`、`EmberServiceLocator` |
| 接口 I 开头 | `IEmberService`、`IEUIView` |
| 命名空间 | `Ember.Core`、`Ember.UI`、`Ember.Resource` |
| 私有字段 `_camelCase` | `_eventDict`、`_isInitialized` |
| 优先 `internal` | 只暴露必要的 `public` API |
| 禁止 | `GameObject.Find`、`FindObjectOfType` |

---

## 变更日志

| 日期 | 变更 |
|------|------|
| 2026-07-25 | 创建框架目录结构，13 个目录 + 13 个 .asmdef |
| 2026-07-25 | 完成 burner 项目框架层全面分析 |
| 2026-07-29 | Core 模块完成（EventBus / ServiceLocator / Singleton / ObjectPool / BroadcastEvent） |
| 2026-07-29 | UI 模块完成（IEUIView / EUIPageDef / EUIManager） |
| 2026-07-29 | Resource 模块完成（IResourceProvider / EmberResourceManager / ResourcesProvider） |
| 2026-07-30 | Scene / Audio / Input / Editor 模块完成 |
| 2026-07-30 | Manager 自动发现系统完成（IEmberManager + EmberManagerCollector） |
| 2026-07-30 | Update 循环管理器完成（IEmberUpdate + EmberUpdateManager） |
| 2026-07-30 | burner 基础包迁移（basic / extensions / uiextension，全部注释待适配） |
| 2026-07-31 | GameState 状态机完成（EmberStateMachine + InitState） |
| 2026-07-31 | Timer 决定放入 com.ember.extensions，保持 Core 零外部依赖 |
| 2026-07-31 | EmberDebug 日志系统完成（两级标签 + 消息分色 + SO 面板 + 全框架统一） |
| 2026-07-31 | 🔴 债务清理：5 个 Manager 统一实现 IEmberManager + [EmberInitOrder]，EmberUpdateManager 去 MonoBehaviour 化为纯 C# 类，新建 GameLauncher 集中驱动入口 |
| 2026-07-31 | 架构固化：定义 IEmberModule 接口，明确两层初始化模型（框架管道 vs 业务模块），Manager vs Module 平行不继承 |
| 2026-07-31 | EmberEventBus API 对齐 UniRx：Subscribe 返回 IDisposable，Dispatch 改名 OnNext |
| 2026-07-31 | InitState 接管 Manager 初始化（对齐 burner InitProcedure 模式） |
| 2026-07-31 | Camera 独立模块：CinemachineBrain + BlenderSettings + 相机堆栈 + 强制霸占模式 |
| 2026-07-31 | 新建 EmberBaseSO（继承溯源面板），Core 按功能分子文件夹 |
| 2026-08-01 | 🧪 场景验证 S1-S2 通过：GameBoot 场景搭建完成，7 个 Manager 初始化链路全部正常 |
| 2026-08-01 | 🐛 修复退出 Play 时 Odin [ShowInInspector] 访问已销毁对象的报错 |
| 2026-08-01 | 🔧 **架构改造：多场景叠加替代 DontDestroyOnLoad**（避开 Unity 6 DDOL 销毁竞态 bug） |
| 2026-08-01 | 🏗️ EmberSingleton 拆分：`EmberMonoSingleton`（无 DDOL）+ `EmberMonoSingletonDontDestroy`（含 DDOL） |
| 2026-08-01 | 📐 GameLauncher 退化为普通 `EmberMonoSingleton`（无 DDOL），由 FrameworkScene 保活 |
| 2026-08-01 | 🔨 FrameworkSceneBootstrapper：编译 + 文件变更时自动同步 Build Settings 场景列表 |
| 2026-08-01 | 🔘 Toolbar 自定义按钮（Unity 6 MainToolbarElement API）+ MenuItem 兜底，一键跳转 FrameworkScene |
| 2026-08-01 | 🧪 **退出 Play Mode 无报错**：S1-S2 重新验证通过，7 个 Manager 初始化链路全部正常 |
| 2026-08-03 | 🧪 **S3 验证通过**：Update/LateUpdate/FixedUpdate 循环正常，EmberUpdateDiagnostics 帧计数递增 |
| 2026-08-03 | 🧪 **S4 验证通过**：EmberEventBus 事件订阅/播报链路正常，GameStateChanged 事件确认可达 |
| 2026-08-03 | 🐛 **S5 修复日志系统**：`GlobalOpen` 读 SO 实时值，`IsEnabled` 优先查 SO，`Disable/Enable` 双向同步 SO |
| 2026-08-03 | 🏗️ **核心三状态体系**：Init → Main → Gameplay，密封外层 + 虚内层模式，子类只 override 业务钩子 |
| 2026-08-03 | 🏗️ **TransitionDescriptor 流转描述符**：声明流转目标 + Label + Condition + Guard，可视化编辑器 + 运行时校验双用途 |
| 2026-08-03 | 🏗️ **SettingsState 通用覆盖状态**：通过 `SettingsContext` 枚举传入上下文（Main / Gameplay），`IsRequired = false` 可被替换 |
| 2026-08-03 | 🏗️ **Init 预加载 MainScene**：`fsm.LoadSceneAsync` 委托 + `TransitionTo(skipSceneLoad)`，Init 先加载场景再过渡 |
| 2026-08-03 | 🏗️ **BootSplash + LoadingView 双遮罩**：BootSplash（Frame 0 黑幕，首次 LoadDone 后销毁）+ LoadingView（后续切换进度条，首跳跳过） |
| 2026-08-03 | 🏗️ **Init 启动动画预留**：`EUIMainAnimationStarter` 基类 + `InitSceneReady`/`InitAnimationDone` 事件，用户继承 override 即可 |
| 2026-08-03 | 🏗️ **场景映射 SO + 快速打开场景**：`EmberSceneMapping` 自动扫描状态 + 匹配同名场景，Toolbar 窗口一键打开 Framework + 目标场景 |
| 2026-08-03 | 🔧 **Play Mode 场景清理 + 退出恢复**：`FrameworkSceneBootstrapper` 点 Play 自动关闭多余场景，退出后恢复 |
| 2026-08-03 | 🔧 **事件 Key 间隔改为 1000**：SceneLoadDone 从 404 改为 4004，避免 HTTP 404 混淆 |
| 2026-08-03 | 🔧 **LogShutdown 淡紫色日志**：对应 LogInit 绿色，框架退出专用 |
| 2026-08-03 | 🔧 **Odin 编码规范补充**：$GROUP 成员引用语法不能拼接字符串，写入 odin-usage-notes.md §2.8 |
| 2026-08-04 | 📐 **UIManager 结构性重写启动**：对 `EUIManager` 进行架构重构 |
| 2026-08-04 | 📦 **Package 迁移计划制定**：分析 burner uiextension 包结构（80+ 文件），制定 ember 三层 Package 逐文件迁移策略 |
| 2026-08-04 | ✅ **com.ember.basic 迁移完成**：36 文件全部适配。从用户旧项目整合 6 个工具。建立 API 速查手册 |
| 2026-08-04 | 🛠️ **编辑器工具全面优化**：从用户旧项目迁移 26 个编辑器工具，删除 5 个，保留 21 个并全部手动优化——统一继承 `EmberEditorWindow : OdinEditorWindow` 基类、提取共享模块、右键快捷菜单统一到 `GameObject/Ember/` 和 `Assets/Ember/` 路径、中英文双语支持 |
| 2026-08-05 | 🔧 **编辑器工具第一轮修复**：全局语言同步 + 双语 WindowTitle + Odin/DrawContent 布局分离 + BatchRenamerEditor 位数警告三按钮 + 删除 DuplicateFinderEditor + EmberCodeValidator 通过时显示反馈 + 菜单重组 + 右键分隔线优先级差增大 + ConsoleLogExporter/QuickMaintenanceTools 对话框全部本地化 |
| 2026-08-05 | 🔧 **编辑器工具第二轮修复**：移除 `HasOdinFields()` 反射（→ 固定 10px 间距，消除布局抖动/错位）+ Ember/Tool 优先级压缩为 10 间隔 + 快捷键 Ctrl+Shift+F → Ctrl+Shift+G + 3 级右键菜单优先级差增至 50 + 补齐 6 个工具 9 个 validate 方法缺失的优先级参数 |
| 2026-08-05 | 🛠️ **新增第 4 个维护工具**：批量清理脚本未使用引用（`CleanUnusedScriptReferences`），扫描 Assets/ 下 .cs 文件，安全移除未使用的 using 指令 |
| 2026-08-05 | 📝 **模块文档全面更新**：Core 拆分到 7 个子目录，全部 API.md→README.md 重命名，更新 Scene/Resource/UI 文档，新增 Audio/Camera/Input/Editor 文档，共 15 个 README.md 覆盖全部 8 个模块 |
| 2026-08-05 | 🆕 **文件日志持久化**：新增 `EmberFileLog`（异步后台线程 + 批量写入 + 日志轮转 + 过期清理）+ `IEmberLogUploader` 上传接口，`EmberDebug` 所有 Log 方法增加文件输出通道 |
| 2026-08-05 | 📋 **Audio 升级方案**：基于 burner 四层架构，制定完整移植方案 → [docs/dev/audio-upgrade-plan.md](../../docs/dev/audio-upgrade-plan.md) |
| 2026-08-06 | ✅ **com.ember.extensions 迁移完成**：逐文件审计 17 个注释文件 → 删除 12 → 迁移 5 + 保留 1 = 6 个活跃源文件。空目录 Async/Resource 已清理 |
| 2026-08-06 | ❌ **Tweener 系统删除**：Runtime 15 + Editor 11 = 26 文件。决策：DOTween 是业界标准，框架不绑定自研 Tween 引擎，改用 `IEUITransitionHandler` 动画钩子接口 |
| 2026-08-06 | 📋 **uiextension 迁移方案文档**：[docs/dev/uiextension-migration-plan.md](../../docs/dev/uiextension-migration-plan.md)，逐文件审计 148 .cs → 确定迁移 58、暂缓 32、删除 10、参考 10 |
| 2026-08-06 | 📋 **uiextension 学习路径文档**：[docs/dev/uiextension-learning-path.md](../../docs/dev/uiextension-learning-path.md)，按依赖深度四层排列 48 个文件的学习顺序 |
| 2026-08-06 | ✅ **uiextension L1-L2 迁移（25 文件）**：L1（18 独立控件/行为/安全区/工具）+ L2（7 内部依赖控件）。asmdef 更新（+Odin +Basic 引用） |
| 2026-08-06 | ✅ **UIManager 结构性重写 Phase A（12 文件）**：EUIEnums / 扩展 IEUIView / EUIPageDef 扩展 / IEUIResourceProvider / IEUITransitionHandler / EUIEvents / EUIObserver / EUIPage / EUIPageContext / EUIBgMaskPool / EUIManager / EUIPageRouter。asmdef 更新（+UniRx 引用） |
| 2026-08-07 | 🏗️ **UIBinding 系统完整还原**：从简化 3 文件 → 完整 Burner 对齐（7 文件 + 4 模板）。EUIPage 纯 C# 化（对标 GamePage），EUILogic 逻辑层分离（对标 GameUILogic），EmberUIBindingBridge 自动注册钩子。预制体只保留 EmberUIBinding |
| 2026-08-06 | ✅ **UIBinding 系统初版（3 文件）**：EmberUIBinding / EmberUIBindingEditor / EmberUIBindingGenerator |
| 2026-08-06 | ✅ **L3 组件封装层（8 文件）**：IEUIComponent / EUIComponent（精简 1447→230 行）/ EUIButton / EUIText / EUIImage / EUIToggle / EUIInputField / EUIProgressBar |
| 2026-08-06 | 🧹 **uiextension 清理**：删除重复/废弃文件 15（ObjectPool/ListPool/BetterList/Logger/StringUtils/BurnerButton/Mirror×2/NodeScreenShot×4/BurnerSafeArea/BurnerBasicUIExtensions 旧版） |
| 2026-08-06 | 🧪 **UI 模块 Edit Mode 测试（12 项）**：创建测试程序集。覆盖 EUIPageDef 构造（4）、UILayer 顺序（1）、EUIObserver 事件通知/取消订阅/Where 过滤（6）、EUIEvents 常量（1）。全部通过 ✅ |
| 2026-08-06 | 🔧 **编译错误批量修复**：UILayer 枚举恢复（移入 EUIEnums.cs）；EUIPage Debug 命名空间修正；TMPro 引用补全；ContentSizeFitterEx 移除 internal Unity API；RectTransformExtensions Vector3+Vector2 歧义修正；Gradient Color32 乘法运算符修正；5 文件补 `using Ember.Basic;`；EmberUIBindingGenerator Debug.Log→EmberDebug + 过时 API 替换 |
| 2026-08-06 | 📝 **进度文档更新**：framework-progress.md 整理重组，合并重复内容，修正编号，统一结构 |
| 2026-08-07 | 📐 **UI 架构对齐 Burner**：EUIPage 非 MonoBehaviour 化（纯 C# 包装类）、EUILogic 逻辑层分离（对标 GameUILogic）、预制体只保留 EmberUIBinding、EmberUIBindingBridge 自动注册钩子。完整 Burner 面板还原（模板/继承/搜索/自动收集/代码生成） |
| 2026-08-07 | 🏗️ **MainMenu + Settings 预制体创建**：通过 Unity MCP 自动搭建完整 UI 层级（Canvas + 子控件 + EmberUIBinding），配置 pageName/classPath/pageFlags，创建 CSharpLogicImplementationData SO + Project Settings 集成 |
| 2026-08-07 | 📝 **文档更新**：framework-progress.md + ui-testing-plan.md 更新架构描述、测试步骤、代码示例 |
| 2026-08-07 | 🔄 **启动流程重构**：事件链重设计 —— `EmberBroadcastEvent.MainSceneReady`(1006) + `OpeningAnimationEnd`(1007)；`InitState` 简化（仅管理 BootSplash + 加载 MainScene + TransitionTo）；`MainState.OnEnter` 加 Subscribe/Broadcast 事件链 + `protected virtual OnOpeningAnimationEnd()`；`EUIMainAnimationStarter`（监听 MainSceneReady 替代 InitSceneReady）；`GameLauncher.CreateMainState()` 工厂方法反射自动发现 `GameMainState` |
| 2026-08-07 | 🛡️ **PlayMode 守卫修复**：`PlayModePageDefGuard` 失效检测从文件路径拼接改为 `EmberUIBinding.PageName` 匹配；弹窗提供"清理并进入"按钮；`FrameworkSceneBootstrapper` 延迟订阅 + `isPlayingOrWillChangePlaymode` 守卫，防止 Guard 阻止 Play 时场景丢失 |
| 2026-08-07 | 🎨 **Canvas sortingOrder 编辑器预览**：`EmberUIBindingEditorUtility.ApplyCanvasSortingOrder`，生成时根据 PageFlags 自动写入预制体 Canvas.sortingOrder（MainPage=100, Popup=200, TopMost=300） |
| 2026-08-07 | 🔌 **EmberUIBindingBridge 自动注册**：加 `[RuntimeInitializeOnLoadMethod]`，引入包即生效，无需手动调用 Register() |
| 2026-08-07 | ✏️ **MainMenu + Settings 业务逻辑**：UIMainMenu（OnInit → BtnSettings ShowPopup / BtnStart Log / OnPause / OnResume / OnDispose）；Settings（OnInit → BtnClose ClosePage / BtnLogout Log / OnDispose） |
| 2026-08-07 | 🏷️ **IEUIPersistentUI marker 接口**：UIManager.Init 隐藏子节点时跳过 BootSplash |
| 2026-08-06 | 📋 **UI 集成测试计划**：[docs/dev/ui-testing-plan.md](../../docs/dev/ui-testing-plan.md)，MainMenu + Settings 两页面测试方案，6 个步骤，8 项验证点，覆盖 UIManager/PageRouter/Page/PageContext/BgMaskPool/UIObserver/UIBinding 全链路 |
| 2026-08-08 | 🎬 **EUIPage 全面文档化**：类级注释写入自研完整生命周期流程（6 大阶段，每步标注 override 层），8 个 virtual hook 全部补充触发时机 + 职责 + 业务层对应 + 示例代码 |
| 2026-08-08 | 🎬 **EUILogic 全面文档化**：12 个钩子全部补充"触发时机 / 在这里做 / 不要在这里做 / 去哪儿做"四段式注释；OnShow/OnHide 加 ⚠️ 警告：动画写在 EUIPage 子类不是这里 |
| 2026-08-08 | 🎬 **预设渐入渐出**：EmberUIBinding 新增 `usePresetFade` / `fadeInTime` / `fadeOutTime` 参数；EUIPage.PlayShow/PlayHide 分支判断；UniTask 线性 alpha 渐变；开启后跳过子类 OnShow/OnHide virtual |
| 2026-08-08 | 🎬 **NotifyOpened/NotifyClosed 时序修正**：从 `_nextFrameCallbacks`（动画未完成就播报） → `CompleteShow/CompleteHide`（动画真正结束时播报）；修复 ReopenPage 重复播报 bug |
| 2026-08-08 | 🗑️ **延迟销毁（对标 Burner）**：EUIPage 新增 closing 状态 + 30s 定时器；EUIManager 新增 `_closingPages` 字典 + 每帧到期检查 + 复用查询；EUIPageRouter 新增 FindReusablePage 复用路径 |
| 2026-08-08 | 🏗️ **GameMainState + 开屏动画**：新增 GameMainState 子类 + MainAnimation 开屏动画，适配新启动流程；Loading / MainMenu / Settings 页面预制体落地 |
| 2026-08-10 | 🏷️ **EUI\* 前缀重命名（uiextension）**：Runtime 核心类型 Ember\* → EUI\*；Editor 工具重构为委托注入模式 |
| 2026-08-11 | 🏷️ **UI 框架类型重命名 + 架构合并**：UI 框架类型统一 EUI\*；`EUIPageRouter` 合并进 `EUIViewEngine`（视图引擎），`EUIManager` 转应用层入口 |
| 2026-08-11 | 🏗️ **状态机子类自动发现**：BootSplash 淡出与状态切换解耦，GameLauncher 反射自动发现业务状态子类 |
| 2026-08-11 | 🛡️ **场景加载拦截器接口**：移除展示进度平滑，新增 `InterceptSceneLoad` 跨场景 Loading 拦截 |
| 2026-08-11 | 🏗️ **Gameplay/Settings 场景与状态**：新增 GameplayScene / SettingsScene + GameGameplayState / GameSettingsState，BootSplash 支持可配置淡出 |
| 2026-08-13 | 🎬 **方块过渡动画 TransitionBlock**：新增方块过渡组件，Loading 页面接入；UI 组件 Ember 前缀统一重命名为 EUI |
| 2026-08-13 | 🔧 **EUIViewEngine 生命周期改造**：改用 EmberSingleton + IEmberUpdate，去 MonoBehaviour |
| 2026-08-14 | 🎬 **方块过渡曲线驱动重构**：多态动画类 → 曲线驱动；接入页面预设过渡槽；第三方 TransitionBlocks 插件导入又移除（最终自研） |
| 2026-08-15 | 🔄 **开屏动画串行时序**：背景页加载迁移进状态机转场，遮挡层渐出前就绪 |
| 2026-08-17 | 📝 **启动时序文档**：ember-boot-sequence.md，五阶段启动链 + 两道串行门槛 |
| 2026-08-17 | ✅ **P0 UI 集成测试完成**：Init → Main(MainMenu) → Settings → Gameplay(InGameUI) → Main 全链路 Play Mode 验证通过 |
| 2026-08-17 | ⏱️ **Timer 定时器完成**：EmberTimerManager（Core/Time），int-ID API（Delay/Interval/Schedule/Cancel），时间源 EmberTimeManager，IEmberUpdate 自动驱动，delta 累加不依赖 UniTask |
| 2026-08-17 | 🧩 **Module 系统完成**：ModulePhase 常量 + EmberModuleCollector（按 Phase 驱动生命周期）+ 首个业务模块 PlayerPrefsModule（Global 阶段，Init 启动/退出销毁） |
| 2026-08-17 | 🌊 **无缝流送模块**：Game.Module 业务模块（Streaming/ 子目录，Phase=Gameplay），拓扑分块 + 方向感知触发器 + 分帧加载卸载；纯 C# 核心（StreamingModule）+ 3 个 Mono 触发器；走 EmberSceneManager 静默加载方法，不改动状态机切换链路 |
| 2026-08-17 | 💡 **全局灯光模块**：Game.Module 的 GlobalLightModule（Phase=Global，默认关闭），全局一套 Light2D 按场景名切换 + DOTween 平滑过渡，监听 SceneLoaded + EmberSceneManager.CurrentScene；配套 GlobalLightConfig 资产 |
| 2026-08-17 | 💾 **玩家存档模块**：Game.Module 的 PlayerDataModule（Phase=Global，默认关闭），管理 PlayerData 存档的自动加载/保存（init 加载、退出落盘），基于 DataSaver（JsonUtility） |
| 2026-08-18 | 🧹 **架构债务清理**：EmberSceneManager 场景加载走 EmberResourceManager（IResourceProvider.LoadSceneAsync 改为返回 AsyncOperation + LoadSceneMode）；IResourceProvider 注册进 ServiceLocator；移除冗余 OnStateChanged C# 事件 |

---

## 未来待办（参考 Burner uiextension）

2026-08-08 对比 Burner `com.burner.uiextension@1.0.2` 后梳理：

### 高价值、低改动

| 待办 | 说明 | 优先级 |
|------|------|--------|
| **PageAsyncOperations** | ShowPage 返回 `STTask《GameUILogic》` 而非 `Action` 回调 | 🟡 |
| **PageLoadTiming** | 页面加载耗时统计（AssetLoadTime / InitTime / OpenTime / TotalLoadTime） | 🟡 |

### 中等价值、中等改动

| 待办 | 说明 | 优先级 |
|------|------|--------|
| **PostponeSetVisible** | 异步加载完成后才 SetActive，防白屏闪烁 | 🟢 |
| **ShouldHideLowerPage** | Popup 支持完全遮盖下层（SetActive false 而非仅 Pause） | 🟢 |

### 高价值、大改动（远期）

| 待办 | 说明 | 优先级 |
|------|------|--------|
| **IResourceHandle + CacheManager** | 资源句柄引用计数 + 对象池，延迟销毁窗口期内真正跳过重复加载 | 🔵 |

### 不建议搬

| 组件 | 理由 |
|------|------|
| **GameUIComponent 三层组件模型** | Burner 每个控件都包一层，太重。Ember 用 `ControlMap` + `EUILogic` 已足够 |
