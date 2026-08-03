# State — 游戏状态机

## 概述

管理游戏全局状态的切换。基于 EmberGameState 抽象基类，支持 TransitionTo（替换式切换）、
Push/Pop（栈式覆盖）、TransitionDescriptor（可视化编辑器数据源）。
InitState → MainState → GameplayState 构成核心状态管线。

## 文件清单

| 角色 | 路径 |
|------|------|
| 状态基类 + 状态机 | `EmberStateMachine.cs` |
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
| `OnStateChanged` (事件) | 状态变更事件 `(oldState, newState)` |

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
| `GetTransitions() → TransitionDescriptor[]` | 可流转的目标状态（可视化编辑器边列表） | 空数组 |
| `GetPushTargets() → TransitionDescriptor[]` | 可 Push 的目标状态 | 空数组 |

### 核心状态一览

| 状态 | IsRequired | ScenePath | 说明 |
|------|------------|-----------|------|
| `InitState` | ✅ | — | 系统初始化：Manager 启动、资源就绪。完成后 TransitionTo<MainState> |
| `MainState` | ✅ | MainScene | 主界面/大厅。Init → Main → Gameplay。支持 Push Settings |
| `GameplayState` | ✅ | GameplayScene | 核心玩法。只需 override OnGameplayXxx 系列方法 |
| `SettingsState` | ❌ | — | 通用设置。以 Push 模式弹出，通过 SettingsContext 区分上下文 |

### GameplayState 子类化钩子

生命周期方法已 sealed，子类应 override：

| 钩子 | 说明 |
|------|------|
| `OnGameplayEnter(object args)` | 进入玩法：加载战斗场景、初始化模块 |
| `OnGameplayExit()` | 退出玩法：卸载场景、清理模块 |
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

```csharp
public override TransitionDescriptor[] GetTransitions() => new[] {
    new(typeof(MainState), "返回大厅"),
    new(typeof(RaidState), "突袭副本") { Condition = "Lv≥10", Guard = () => level >= 10 },
};
```

## 主流程

**启动：** `Start<T>(args)` → 查找状态 → `_current.OnEnter(args)` → Dispatch(GameStateChanged)

**切换：** `TransitionTo<T>(args)` → AllowReEnter检查 → `_current.OnExit()` → 保存Previous → `_current.OnEnter(args)` → Dispatch+OnStateChanged事件

**Push：** `Push<T>(args)` → `_current.OnPause()` → 压入栈 → `新状态.OnEnter(args)` → 事件广播

**Pop：** `Pop()` → 空栈检查 → `_current.OnExit()` → 栈恢复 → `_current.OnResume()` → 事件广播

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| 状态注册 | InitState 必须手动注册到状态机。GameLauncher.ConfigureStateMachine() 是注册的唯一入口 |
| Push/Pop 配对 | Push 后必须有对应 Pop，否则底层状态永远不会 Resume |
| IsRequired 保护 | IsRequired=true 的状态不可注销。当前活跃状态不可注销 |
| Start vs TransitionTo | Start 不走 Exit 流程，仅首次启动使用。正常切换用 TransitionTo |
| ScenePath | 状态上的 ScenePath 由 SceneCoordinator 读取并自动加载/卸载对应场景 |
