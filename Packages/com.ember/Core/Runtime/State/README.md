# State — 游戏状态机

## 概述

管理游戏全局状态的切换。基于 EmberGameState 抽象基类，支持 TransitionTo（替换式切换）、
Push/Pop（栈式覆盖）、TransitionDescriptor（可视化编辑器数据源）。
InitState → MainState → GameplayState 构成核心状态管线。状态机同时负责驱动可选业务 Module 的
Phase 生命周期；框架必要 Manager 只在 Init 阶段统一启动，不随业务状态切换。

请求级准备、取消与失败清理、场景所有权及 Loading 接口迁移见[游戏流程转场](../../../Documentation~/core/game-transitions.md)。

## 文件清单

| 角色 | 路径 |
|------|------|
| 状态基类 + 状态机 | `EmberStateMachine.cs` |
| 转场请求与执行 | `EmberTransitionOperation.cs`、`EmberStateMachine.Transitions.cs` |
| 系统初始化状态 | `InitState.cs` |
| 主界面/大厅状态 | `MainState.cs` |
| 核心玩法状态 | `GameplayState.cs` |
| 设置状态（覆盖式） | `SettingsState.cs` |
| 流转描述符 | `TransitionDescriptor.cs` |

## 公开 API

### EmberStateMachine — 状态机

| 方法 | 说明 |
|------|------|
| `Register(EmberGameState)` | 注册状态，同类型重复忽略 |
| `Unregister<T>() → bool` | 注销状态。IsRequired 或当前活跃状态拒绝注销 |
| `Start<T>(object args)` | 直接进入状态（不走 Exit 流程），仅用于首次启动 |
| `TransitionTo<T>(object args)` | 切换：Exit 当前 → Enter 目标。AllowReEnter=false 同状态忽略 |
| `Push<T>(object args)` | 暂停当前(OnPause)，在上方覆盖新状态 |
| `Pop()` | 弹出覆盖状态(OnExit)，恢复下层(OnResume) |
| `Is<T>() → bool` | 当前是否处于指定状态 |
| `GetState<T>() → T` | 获取已注册的状态实例 |
| `ValidateRequiredStates() → bool` | 检查必需状态是否都已注册 |
| `Current` (属性) | 当前活跃状态 |
| `Previous` (属性) | 上一个状态 |
| `RegisteredStates` (属性) | 所有已注册状态的只读集合 |
| `RequestTransitionTo<T>(args, skipSceneLoad, cancellation)` | 返回单次请求，Completion 包括准备、提交和清理 |
| `RequestPush<T>(args, cancellation)` / `RequestPop(cancellation)` | 可等待的覆盖与恢复 |
| `RequestInPlace<T>(args, cancellation)` | 当前状态内的资源交接；须实现 IEmberInPlaceState 并声明自身边，不退出/重入状态或重新激活 Module |
| `IsTransitioning` / `CancelTransition()` / `StopTransitions()` | 在途查询、取消、永久封闭转场入口 |

状态变更使用 `EmberBroadcastEvent.GameStateChanged` 广播。

请求的 `WhenPrepared`、`WhenRunning`、`WhenInteractive` 分别表示准备、交接及完整遮罩退出的阶段结果；必须检查结果状态与请求有效性。`Completion` 不一定包含业务外层仍持有的遮罩。阶段、原位请求和失败清理契约以[游戏流程转场](../../../Documentation~/core/game-transitions.md)为准。

### EmberGameState — 状态基类

| 虚成员 | 说明 | 默认值 |
|--------|------|--------|
| `Name` | 状态名称 | 类名 |
| `Description` | 状态描述（可视化编辑器展示） | "" |
| `ScenePath` | 对应场景名 | null |
| `IsRequired` | 系统必需（不可删除） | false |
| `AllowReEnter` | 允许切换到自身 | false |
| `OnEnter(object args)` | 进入状态 | 空 |
| `OnExit()` | 离开状态 | 空 |
| `OnUpdate()` | 每帧调用 | 空 |
| `OnPause()` | 被覆盖时 | 空 |
| `OnResume()` | 覆盖恢复时 | 空 |
| `PrepareEnterAsync(SceneTransitionContext)` | 本请求进入准备，可提供 PreparedArgs | 调用无参 PrepareEnterAsync |
| `AbortPrepareAsync(SceneTransitionContext)` | 准备已开始、提交未开始时清理本请求候选 | 空 |
| `GetEdges() → TransitionDescriptor[]` | 当前声明入口；注册时初始化实例 Edges | 合并两个兼容声明 |
| `GetTransitions() → TransitionDescriptor[]` | 旧式目标声明，兼容保留 | 空数组 |
| `GetPushTargets() → TransitionDescriptor[]` | 旧式覆盖目标声明，兼容保留 | 空数组 |

### 核心状态一览

| 状态 | IsRequired | ScenePath | 说明 |
|------|------------|-----------|------|
| `InitState` | ✅ | — | 发现启用 Module → 启动全部 Manager → 激活 Global Module → TransitionTo<MainState> |
| `MainState` | ✅ | MainScene | 主界面/大厅。Init → Main → Gameplay。支持 Push Settings |
| `GameplayState` | ✅ | GameplayScene | 核心玩法。只需 override OnGameplayXxx 系列方法 |
| `SettingsState` | ❌ | — | 通用设置。以 Push 模式弹出，通过 SettingsContext 区分上下文 |

### Manager 与 Module 的状态关系

- Manager 是框架必要组件：`InitState` 调用 `EmberManagerCollector.InitializeAll()` 后持续存活到框架退出。
- Module 是可选业务积木：`Enabled` 决定启动扫描时是否装配，`Phase` 决定已装配模块何时调用
  `OnInit` / `OnDestroy`。
- `DiscoverModules()` 只构造和登记，不代表模块已经激活；Module 只有 `OnInit` 成功后才接收 Update。
- 内置状态当前自动驱动 `Global` 和 `Gameplay`。自定义阶段或 `ModulePhase.Main` 需要在对应状态的
  Enter/Exit 中显式接入 `InitPhase` / `DestroyPhase`。

### GameplayState 子类化钩子

生命周期方法已 sealed，子类应 override：

| 钩子 | 说明 |
|------|------|
| `OnGameplayEnter(object args)` | Gameplay Module 激活后执行玩法入口逻辑 |
| `OnGameplayExit()` | Gameplay Module 销毁前执行玩法退出逻辑 |
| `OnGameplayUpdate()` | 每帧驱动玩法主循环 |
| `OnGameplayPause()` | 被弹窗覆盖时暂停 |
| `OnGameplayResume()` | 弹窗关闭后恢复 |

### MainState 子类化钩子

| 钩子 | 说明 |
|------|------|
| `OnMainEnter(object args)` | 进入主界面：显示 UI、播放 BGM |
| `OnMainExit()` | 离开主界面：隐藏 UI |

### TransitionDescriptor — 流转描述符

| 成员 | 说明 |
|------|------|
| `TargetState` (Type, init) | 目标状态类型 |
| `Label` (string, init) | 可视化编辑器中连线上的标签 |
| `Condition` (string, init) | 条件文字描述（可视化展示用） |
| `Guard` (Func<bool>, init) | 运行时准入条件，null 无条件 |
| `QuickSceneLoad` (bool, set) | 发生场景切换时使用快速加载；不改变切换/覆盖语义 |
| `ReadOnly` (bool, init) | 编辑器禁止修改内置边结构；允许调整 QuickSceneLoad |

```csharp
public override TransitionDescriptor[] GetEdges() => new[] {
    new(typeof(MainState), "返回大厅"),
    new(typeof(RaidState), "突袭副本") { Condition = "Lv≥10", Guard = () => level >= 10 },
};
```

## 主流程

**启动：** `Start<T>(args)` → 查找状态 → `_current.OnEnter(args)` → Dispatch(GameStateChanged)

**切换：** `RequestTransitionTo<T>(args)` → 并发/AllowReEnter/Guard 检查 → Loading 遮挡 → 候选场景加载 → 请求级 PrepareEnterAsync → 旧状态及覆盖栈退出 → 新状态 OnEnter(PreparedArgs) → 状态广播 → 旧场景卸载 → 遮挡关闭 → Completion。无场景或同场景目标沿用覆盖语义。

**Push：** `Push<T>(args)` → `_current.OnPause()` → 压入栈 → `新状态.OnEnter(args)` → 事件广播

**Pop：** `Pop()` → 空栈检查 → `_current.OnExit()` → 栈恢复 → `_current.OnResume()` → 事件广播

Push 同样执行请求级准备；Pop 恢复已有状态，不重新准备。旧 void 入口委托给请求 API 并记录失败，
不能以调用返回表示切换完成。并发返回 Busy，取消须等待清理；提交开始后不自动回滚业务生命周期。

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| 状态注册 | InitState 必须手动注册到状态机。GameLauncher.ConfigureStateMachine() 是注册的唯一入口 |
| Push/Pop 配对 | Push 后必须有对应 Pop，否则底层状态永远不会 Resume |
| IsRequired 保护 | IsRequired=true 的状态不可注销。当前活跃状态不可注销 |
| Start vs TransitionTo | Start 不走 Exit 流程，仅首次启动使用。正常切换用 TransitionTo |
| ScenePath | 状态上的 ScenePath 由 SceneCoordinator 读取并自动加载/卸载对应场景 |
