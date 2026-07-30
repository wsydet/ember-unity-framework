# 模块名称：核心基础设施（Core）

---

## 1. 快速上手

```csharp
// 方式一：自动初始化（推荐）
// 在游戏入口调用一次，自动发现并初始化所有 IEmberManager
EmberManagerCollector.Instance.InitializeAll();

// 方式二：手动使用单个组件
EmberEventBus.Subscribe(EmberBroadcastEvent.CoreReady, () =>
{
    Debug.Log("Core 模块已就绪");
});
EmberEventBus.Dispatch(EmberBroadcastEvent.CoreReady);
```

---

## 2. 模块概述

Core 是 ember-unity-framework 的基础设施层，提供以下通用能力：
- **事件总线**（EventBus）—— 全局发布/订阅，广播型生命周期事件
- **服务定位器**（ServiceLocator）—— 接口驱动的服务注册与查找
- **单例模式**（Singleton / MonoSingleton）—— 线程安全的纯 C# 和 MonoBehaviour 单例
- **对象池**（ObjectPool）—— class 实例复用，减少 GC
- **管理器自动收集**（ManagerCollector）—— 反射扫描 IEmberManager，按优先级自动初始化
- **状态机**（StateMachine）—— 游戏全局状态管理，支持 TransitionTo / Push / Pop
- **统一 Update 循环**（UpdateManager）—— 一处驱动所有模块的 Update/LateUpdate/FixedUpdate

所有上层模块（Resource、UI、Scene 等）均依赖 Core。

---

## 3. 依赖关系

| 依赖 | 类型 | 说明 |
|------|------|------|
| `UnityEngine` | 引擎 | MonoBehaviour、GameObject、DontDestroyOnLoad、Debug 等 |
| `System` / `System.Collections.Generic` | 标准库 | Action、Delegate、Dictionary、Stack、List 等 |
| `System.Reflection` | 标准库 | 运行时类型扫描（ManagerCollector、UpdateManager） |

> Core 模块不依赖任何外部包或其他框架模块。它是整个框架的**最底层**。

---

## 4. 文件清单

| 角色 | 路径 |
|------|------|
| 事件总线 | `Assets/Ember/Core/Runtime/EmberEventBus.cs` |
| 广播事件常量表 | `Assets/Ember/Core/Runtime/EmberBroadcastEvent.cs` |
| 服务定位器 | `Assets/Ember/Core/Runtime/EmberServiceLocator.cs` |
| 单例基类（纯 C# + MonoBehaviour） | `Assets/Ember/Core/Runtime/EmberSingleton.cs` |
| 对象池 + 池化接口 | `Assets/Ember/Core/Runtime/EmberObjectPool.cs` |
| 管理器接口 | `Assets/Ember/Core/Runtime/IEmberManager.cs` |
| 初始化优先级特性 | `Assets/Ember/Core/Runtime/EmberInitOrderAttribute.cs` |
| 管理器自动收集器 | `Assets/Ember/Core/Runtime/EmberManagerCollector.cs` |
| 统一 Update 循环 | `Assets/Ember/Core/Runtime/EmberUpdateManager.cs` |
| 帧更新接口 | `Assets/Ember/Core/Runtime/IEmberUpdate.cs` |
| 游戏状态基类 + 状态机 | `Assets/Ember/Core/Runtime/EmberStateMachine.cs` |
| 内置 Init 状态 | `Assets/Ember/Core/Runtime/InitState.cs` |
| 视图/表现层 | 无 |
| 编辑器扩展 | 无 |

---

## 5. 公开 API

### 5.1 入口类型

| 类型 | 职责 | 获取方式 |
|------|------|----------|
| `EmberEventBus` | 全局事件发布/订阅系统 | 静态方法调用 |
| `EmberServiceLocator` | 轻量级服务注册与查找 | 静态方法调用 |
| `EmberSingleton<T>` | 纯 C# 单例基类 | `MyManager.Instance` |
| `EmberMonoSingleton<T>` | MonoBehaviour 单例基类 | `MyManager.Instance` |
| `EmberObjectPool<T>` | 通用对象池，复用 class 实例 | `new EmberObjectPool<MyClass>(maxCapacity: 100)` |
| `EmberManagerCollector` | 反射扫描并自动初始化所有 IEmberManager | `EmberManagerCollector.Instance` |
| `EmberUpdateManager` | 统一驱动 Update/LateUpdate/FixedUpdate | `EmberUpdateManager.Instance`（自动创建） |
| `EmberStateMachine` | 游戏状态机，管理全局状态切换 | `new EmberStateMachine()` |
| `EmberGameState` | 游戏状态抽象基类 | 继承并 override 钩子 |
| `EmberInitOrderAttribute` | 管理器初始化优先级特性 | `[EmberInitOrder(100)]` 标注在类上 |
| `EmberBroadcastEvent` | 广播事件 int-key 常量表 | 直接引用常量 |
| `IEmberManager` | 管理器接口（Init/Destroy） | 由管理器类实现 |
| `IEmberUpdate` / `IEmberLateUpdate` / `IEmberFixedUpdate` | 帧更新接口 | 由需要帧回调的类实现 |
| `IPoolable` | 池化对象接口 | 由业务类实现 |
| `InitState` | 系统初始化必需状态 | `new InitState()` 后注册到 StateMachine |

### 5.2 核心方法

#### EmberManagerCollector — 管理器自动收集

| 方法签名 | 说明 |
|----------|------|
| `InitializeAll()` | 反射扫描所有 IEmberManager 实现，按 InitOrder 排序后依次调用 Init()。可安全重复调用 |
| `DestroyAll()` | 按 InitOrder 逆序调用所有管理器的 Destroy()，异常不中断 |
| `ManagerCount` (属性) | 已发现的管理器数量 |

#### EmberUpdateManager — 统一 Update 循环

| 方法签名 | 说明 |
|----------|------|
| `CurrentPhase` (属性) | 当前激活的模块阶段。只 Tick 该阶段及之前的接收者（默认 int.MaxValue 全量 Tick） |

#### EmberStateMachine — 游戏状态机

| 方法签名 | 说明 |
|----------|------|
| `Register(EmberGameState state)` | 注册状态，同类型重复注册忽略 |
| `Unregister<T>()` | 注销状态。必需状态（IsRequired）或当前活跃状态拒绝注销 |
| `Start<T>(object args)` | 直接进入状态（不走 Exit/Enter 流程），用于首次启动 |
| `TransitionTo<T>(object args)` | 切换状态：Exit 当前 → Enter 目标。同状态且 AllowReEnter=false 时忽略 |
| `Push<T>(object args)` | 暂停当前状态，在其上方覆盖新状态（弹窗式） |
| `Pop()` | 弹出最上层覆盖状态，恢复下层 |
| `Is<T>()` | 当前是否处于指定状态 |
| `GetState<T>()` | 获取已注册的状态实例 |
| `ValidateRequiredStates()` | 检查必需状态是否都已注册 |
| `Current` (属性) | 当前活跃状态 |
| `Previous` (属性) | 上一个状态 |
| `RegisteredStates` (属性) | 所有已注册状态的只读集合 |
| `OnStateChanged` (事件) | 状态变更事件（oldState, newState） |

#### EmberGameState — 状态基类

| 虚成员 | 说明 |
|--------|------|
| `Name` | 状态名称（默认类名） |
| `Description` | 状态描述（图形化编辑器展示） |
| `IsRequired` | 是否系统必需状态（不可删除），默认 false |
| `AllowReEnter` | 是否允许切换到自身，默认 false |
| `OnEnter(object args)` | 进入状态时调用 |
| `OnExit()` | 离开状态时调用 |
| `OnUpdate()` | 每帧调用（需在 StateMachine 中驱动） |
| `OnPause()` | 被其他状态覆盖时调用 |
| `OnResume()` | 覆盖状态退出后，此状态重新可见时调用 |

#### EmberInitOrderAttribute — 初始化优先级

| 成员 | 说明 |
|------|------|
| `Order` (属性) | 初始化顺序值，越小越先初始化 |
| 常量 `Core = 100` | 基础设施（EventBus、ServiceLocator） |
| 常量 `Resource = 200` | 资源系统 |
| 常量 `Audio = 300` | 音频 |
| 常量 `Input = 400` | 输入 |
| 常量 `UI = 500` | UI |
| 常量 `Scene = 600` | 场景 |
| 常量 `Game = 700` | 业务层 |
| 常量 `Default = 1000` | 未标注时的 fallback |

#### IEmberManager — 管理器接口

| 方法签名 | 说明 |
|----------|------|
| `Init()` | 初始化，由 Collector 按顺序调用 |
| `Destroy()` | 销毁，由 Collector 逆序调用 |

#### IEmberUpdate / IEmberLateUpdate / IEmberFixedUpdate

| 方法签名 | 说明 |
|----------|------|
| `Update()` | 每帧由 EmberUpdateManager 调用 |
| `LateUpdate()` | 每帧 LateUpdate 阶段调用 |
| `FixedUpdate()` | 每物理帧调用 |

#### EmberEventBus — 事件总线

| 方法签名 | 说明 |
|----------|------|
| `Subscribe(int eventKey, Action handler)` | 订阅无参事件（支持 0～4 参） |
| `Unsubscribe(...)` | 取消订阅（对应 0～4 参） |
| `Dispatch(int eventKey)` | 派发无参事件（支持 0～4 参） |
| `HasSubscribers(int eventKey)` | 检查是否有订阅者 |
| `Clear(int eventKey)` | 清除指定事件所有订阅者 |
| `ClearAll()` | 清除所有事件订阅 |

#### EmberServiceLocator — 服务定位器

| 方法签名 | 说明 |
|----------|------|
| `Register<T>(T instance)` | 注册服务实例 |
| `RegisterLazy<T>(Func<T> factory)` | 延迟注册 |
| `TryRegister<T>(T instance)` | 尝试注册，已存在返回 false |
| `Resolve<T>()` | 解析服务，未注册抛异常 |
| `TryResolve<T>()` | 尝试解析，未注册返回 null |
| `IsRegistered<T>()` | 检查是否已注册 |
| `Unregister<T>()` | 注销服务 |
| `ClearAll()` | 清除所有服务（自动 Dispose） |

#### EmberObjectPool<T> — 对象池

| 方法签名 | 说明 |
|----------|------|
| 构造 `(int initialCapacity, int maxCapacity, bool trackStats)` | 创建对象池 |
| `Get()` | 获取对象 |
| `Return(T obj)` | 归还对象 |
| `Prewarm(int count)` | 预分配 |
| `Clear()` | 清空（自动 Dispose） |

#### EmberSingleton<T> / EmberMonoSingleton<T> — 单例

| 方法签名 | 说明 |
|----------|------|
| `Instance` (静态属性) | 获取单例（线程安全） |
| `IsValid` (静态属性) | 是否已创建 |
| `Destroy()` | 销毁（EmberSingleton） |
| `OnDestroy()` | 清理钩子（纯 C#） |
| `OnSingletonAwake()` | 初始化钩子（Mono） |
| `OnSingletonDestroy()` | 清理钩子（Mono） |

---

## 6. 主流程

**流程一：管理器自动收集与初始化**
`[入口] InitializeAll()` → `[重复初始化检查]` → `ScanAndCollect()` → `[反射遍历所有程序集]` → `[过滤系统程序集]` → `[筛选 IEmberManager 实现]` → `GetSingletonInstance(type)` → `[反射获取 Instance 属性]` → `[读取 EmberInitOrderAttribute]` → `[按 Order 排序]` → `[依次调用 mgr.Init()]` → `[标记 _initialized]`
→ 销毁时：`DestroyAll()` → `[逆序遍历 _managers]` → `[逐个 mgr.Destroy()]` → `[异常不中断]` → `Clear`

**流程二：状态机切换**
`[注册] Register(state)` → `[去重检查]` → `存入 _states`
`[启动] Start<T>(args)` → `[查找状态]` → `_current.OnEnter(args)` → `Dispatch(GameStateChanged)`
`[切换] TransitionTo<T>(args)` → `[AllowReEnter 检查]` → `_current.OnExit()` → `保存 Previous` → `_current.OnEnter(args)` → `Dispatch(GameStateChanged)` → `OnStateChanged?.Invoke()`

**流程三：栈式覆盖（Push/Pop）**
`Push<T>(args)` → `_current.OnPause()` → `压入 _overlayStack` → `新状态.OnEnter(args)` → `事件广播`
`Pop()` → `[空栈检查]` → `_current.OnExit()` → `从栈中恢复` → `_current.OnResume()` → `事件广播`

**流程四：统一 Update 循环**
`Update()` → `[同帧防重]` → `[遍历 _updaters 按阶段过滤]` → `[逐个 updater.Update()，异常不中断]`
`LateUpdate()` / `FixedUpdate()` 同理，分别遍历 _lateUpdaters / _fixedUpdaters

**流程五：事件订阅与派发**
`Subscribe(key, handler)` → `[判空 + InDispatch 检查]` → `CombineInto() 存入字典`
`Dispatch(key)` → `[查找 handler]` → `EnterDispatch()` → `handler.Invoke()` → `ExitDispatch()` → `ExecutePendingOps()`

---

## 7. 修改影响范围

- **新增模块事件 Key** → 在 `EmberBroadcastEvent` 添加常量（基址已预留，偏移 1～99）
- **新增全局管理器** → 实现 `IEmberManager` + 标注 `[EmberInitOrder]` + 继承单例基类，ManagerCollector 自动发现
- **新增帧更新逻辑** → 实现 `IEmberUpdate` / `IEmberLateUpdate` / `IEmberFixedUpdate` + 继承单例基类，UpdateManager 自动发现
- **新增游戏状态** → 继承 `EmberGameState`，override 钩子，注册到 `EmberStateMachine`
- **调整管理器初始化顺序** → 改 `EmberInitOrderAttribute` 的 Order 值或其预定义常量
- **调整状态机行为（如增加过渡动画）** → 改 `EmberStateMachine.TransitionTo()` 或 `Push()`
- **调整反射扫描白名单** → 改 `EmberManagerCollector.IsSystemAssembly()` 或 `EmberUpdateManager.IsSystemAssembly()`

---

## 8. 约束与已知陷阱

| 类别 | 说明 |
|------|------|
| 初始化顺序 | 推荐使用 `EmberManagerCollector.InitializeAll()` 自动初始化所有 IEmberManager。手动初始化时注意 EventBus → ServiceLocator → Resource → ... → UI → Scene 的顺序 |
| 生命周期 | `EmberMonoSingleton` 挂载 `DontDestroyOnLoad`，场景切换不销毁。`EmberSingleton.Destroy()` 需手动调用。ManagerCollector.DestroyAll() 在程序退出时逆序销毁 |
| 反射扫描 | ManagerCollector 和 UpdateManager 依赖反射扫描程序集。只扫描以 `Ember` 或 `Game` 开头的程序集，跳过 System/Unity/Sirenix/UniTask 等第三方和系统程序集。新增第三方包时若命名空间特殊，需更新 `IsSystemAssembly()` 过滤逻辑 |
| 状态机 | Push 后**必须**有对应的 Pop，否则底层状态永远不会 Resume。`Start<T>()` 直接进入不走 TransitionTo 流程，仅用于首次启动。必需状态（IsRequired=true）不可注销 |
| 数据边界 | `EmberBroadcastEvent` 模块基址间隔 100，每个模块偏移 1～99。EventBus 支持 0～4 个泛型参数。`EmberUpdateManager.CurrentPhase` 可控制只 Tick 特定阶段，默认 int.MaxValue 全量 |
| 线程安全 | EventBus、ServiceLocator 均**线程不安全**，仅限主线程。EmberSingleton 双检锁线程安全。ManagerCollector 初始化应在主线程完成 |
| 事件泄漏 | Subscribe 后必须在对象销毁前 Unsubscribe。`OnStateChanged` 事件订阅者需手动取消订阅。UpdateManager 通过反射持有单例引用——不会泄漏，但销毁时需调用 Destroy() 清空列表 |
| 已知问题 | ManagerCollector 和 UpdateManager 各自维护 `IsSystemAssembly()` 副本，逻辑略有不同，未来应统一。InitState 未被 EmberStateMachine 自动注册，需手动 `Register(new InitState())` |
