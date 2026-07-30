# 模块名称：核心基础设施（Core）

---

## 1. 快速上手

```csharp
// 订阅框架生命周期事件，在模块就绪后执行初始化
EmberEventBus.Subscribe(EmberBroadcastEvent.CoreReady, () =>
{
    Debug.Log("Core 模块已就绪，可以安全访问其他模块");
});

// 触发事件
EmberEventBus.Dispatch(EmberBroadcastEvent.CoreReady);
```

---

## 2. 模块概述

Core 是 ember-unity-framework 的基础设施层，提供事件总线（EventBus）、服务定位器（ServiceLocator）、
单例模式（Singleton/MonoSingleton）和对象池（ObjectPool）等通用能力。
所有上层模块（Resource、UI、Scene 等）均依赖 Core 进行通信和服务管理。

---

## 3. 依赖关系

| 依赖 | 类型 | 说明 |
|------|------|------|
| `UnityEngine` | 引擎 | MonoBehaviour、GameObject、DontDestroyOnLoad 等 Unity API |
| `System` / `System.Collections.Generic` | 标准库 | Action、Delegate、Dictionary、Stack 等基础类型 |

> Core 模块不依赖任何外部包或其他框架模块。它是整个框架的**最底层**，所有其他模块依赖 Core。

---

## 4. 文件清单

| 角色 | 路径 |
|------|------|
| 事件总线 | `Assets/Ember/Core/Runtime/EmberEventBus.cs` |
| 广播事件常量表 | `Assets/Ember/Core/Runtime/EmberBroadcastEvent.cs` |
| 服务定位器 | `Assets/Ember/Core/Runtime/EmberServiceLocator.cs` |
| 单例基类（纯 C# + MonoBehaviour） | `Assets/Ember/Core/Runtime/EmberSingleton.cs` |
| 对象池 + 池化接口 | `Assets/Ember/Core/Runtime/EmberObjectPool.cs` |
| 视图/表现层 | 无 |
| 编辑器扩展 | 无 |

---

## 5. 公开 API

### 5.1 入口类型

| 类型 | 职责 | 获取方式 |
|------|------|----------|
| `EmberEventBus` | 全局事件发布/订阅系统，广播型生命周期事件 | `EmberEventBus.Subscribe(...)` 静态方法 |
| `EmberServiceLocator` | 轻量级服务注册与查找，接口→实现映射 | `EmberServiceLocator.Resolve<T>()` 静态方法 |
| `EmberSingleton<T>` | 纯 C# 单例基类，用于不需要 GameObject 的逻辑管理器 | `MyManager.Instance` |
| `EmberMonoSingleton<T>` | MonoBehaviour 单例基类，用于需要挂载到 GameObject 的组件 | `MyUIManager.Instance` |
| `EmberObjectPool<T>` | 通用对象池，复用 class 实例减少 GC | `new EmberObjectPool<MyClass>(maxCapacity: 100)` |
| `EmberBroadcastEvent` | 广播事件 int-key 常量表，按模块区间分配 | 直接引用常量：`EmberBroadcastEvent.CoreReady` |
| `IPoolable` | 池化对象接口，取出/归还时自动回调 | 由业务类实现 |

### 5.2 核心方法

#### EmberEventBus — 事件总线

| 方法签名 | 说明 |
|----------|------|
| `Subscribe(int eventKey, Action handler)` | 订阅无参事件 |
| `Subscribe<T>(int eventKey, Action<T> handler)` | 订阅 1 参事件 |
| `Subscribe<T1, T2>(int eventKey, Action<T1, T2> handler)` | 订阅 2 参事件 |
| `Subscribe<T1, T2, T3>(int eventKey, Action<T1, T2, T3> handler)` | 订阅 3 参事件 |
| `Subscribe<T1, T2, T3, T4>(int eventKey, Action<T1, T2, T3, T4> handler)` | 订阅 4 参事件 |
| `Unsubscribe(...)` | 取消订阅（支持 0～4 参，签名与 Subscribe 对应） |
| `Dispatch(int eventKey)` | 派发无参事件 |
| `Dispatch<T>(int eventKey, T arg)` | 派发 1 参事件（支持 1～4 参） |
| `HasSubscribers(int eventKey)` | 检查指定事件是否有订阅者（诊断用） |
| `Clear(int eventKey)` | 清除指定事件的所有订阅者 |
| `ClearAll()` | 清除所有事件订阅（仅程序退出/重置时使用） |

#### EmberServiceLocator — 服务定位器

| 方法签名 | 说明 |
|----------|------|
| `Register<T>(T instance)` | 注册服务实例，重复注册抛出异常 |
| `RegisterLazy<T>(Func<T> factory)` | 延迟注册，首次 Resolve 时才创建 |
| `TryRegister<T>(T instance)` | 尝试注册，已存在返回 false 不抛异常 |
| `Resolve<T>()` | 解析服务，未注册抛出异常 |
| `TryResolve<T>()` | 尝试解析服务，未注册返回 null |
| `IsRegistered<T>()` | 检查服务是否已注册（含延迟注册） |
| `Unregister<T>()` | 注销指定服务，返回是否成功 |
| `ClearAll()` | 清除所有服务（自动释放 IDisposable） |
| `RegisteredCount` (属性) | 获取已注册服务总数 |

#### EmberObjectPool<T> — 对象池

| 方法签名 | 说明 |
|----------|------|
| `EmberObjectPool(int initialCapacity, int maxCapacity, bool trackStats)` | 构造，可指定初始容量、最大容量、统计开关 |
| `Get()` | 从池中获取对象，池空则创建新对象 |
| `Return(T obj)` | 归还对象，池满则丢弃（自动 Dispose） |
| `Prewarm(int count)` | 预分配对象到池中 |
| `Clear()` | 清空池，释放所有空闲对象（自动 Dispose） |
| `FreeCount` (属性) | 当前空闲对象数 |
| `TotalCreated` (属性) | 累计创建数 |
| `TotalRetrieved` (属性) | 累计取出次数 |
| `TotalReturned` (属性) | 累计归还次数 |

#### EmberSingleton<T> / EmberMonoSingleton<T> — 单例

| 方法签名 | 说明 |
|----------|------|
| `Instance` (静态属性) | 获取单例实例，首次访问自动创建（线程安全） |
| `IsValid` (静态属性) | 检查单例是否已创建，不触发创建 |
| `Destroy()` (静态方法, EmberSingleton) | 销毁单例实例，调用 OnDestroy 钩子 |
| `OnDestroy()` (受保护虚方法) | 子类可重写以做清理（纯 C# 版本） |
| `OnSingletonAwake()` (受保护虚方法) | 替代 Awake，首次创建时调用一次（Mono 版本） |
| `OnSingletonDestroy()` (受保护虚方法) | 替代 OnDestroy 的清理钩子（Mono 版本） |

---

## 6. 主流程

**流程一：事件订阅与派发**
`[外部] Subscribe(key, handler)` → `[判空 + InDispatch 检查]` → `CombineInto() 存入字典` → `[返回]`
`[外部] Dispatch(key)` → `[查找 handler]` → `EnterDispatch()` → `handler.Invoke()` → `ExitDispatch()` → `ExecutePendingOps() 处理延迟操作`

**流程二：服务注册与解析**
`[框架初始化] Register<T>(instance)` → `[判空 + 重复检查]` → `存入 _services 字典`
`[业务层] Resolve<T>()` → `[查 _services]` → `[未命中则查 _lazyFactories]` → `[调用工厂 + 缓存]` → `返回实例`

**流程三：对象池获取与归还**
`[业务层] Get()` → `[_free 栈 Pop 或 new T()]` → `IPoolable.OnTakeFromPool()` → `返回对象`
`[业务层] Return(obj)` → `[判空 + 容量检查]` → `IPoolable.OnReturnToPool()` → `Push 回 _free 栈`

**流程四：MonoSingleton 自动创建**
`[首次访问] Instance (get)` → `[双检锁]` → `FindAnyObjectByType<T>()` → `[未找到则 new GameObject + AddComponent]` → `DontDestroyOnLoad()` → `返回实例`

---

## 7. 修改影响范围

- **新增模块事件 Key** → 在 `EmberBroadcastEvent` 中为对应模块添加常量（基址已预留，偏移 1～99）
- **调整事件派发行为** → 改 `EmberEventBus.Dispatch()` 及其内部的 `EnterDispatch` / `ExitDispatch`
- **新增服务注册方式** → 改 `EmberServiceLocator`，添加新方法
- **调整单例生命周期钩子** → 改 `EmberSingleton.OnDestroy()` 或 `EmberMonoSingleton.OnSingletonAwake()`
- **调整对象池策略（如 LRU 驱逐）** → 改 `EmberObjectPool.Return()` 和构造函数
- **增加池化统计指标** → 改 `EmberObjectPool` 的参数区，添加新属性和统计逻辑

---

## 8. 约束与已知陷阱

| 类别 | 说明 |
|------|------|
| 初始化顺序 | 所有框架模块应在订阅 `CoreReady` 事件后初始化；Core 自身无初始化依赖 |
| 生命周期 | `EmberMonoSingleton` 挂载 `DontDestroyOnLoad`，场景切换不销毁。`EmberSingleton.Destroy()` 需手动调用，不自动释放 |
| 数据边界 | `EmberBroadcastEvent` 模块基址间隔 100，每个模块偏移 1～99；超出偏移会与下一模块冲突。EventBus 支持 0～4 个泛型参数，超过 4 个需自行封装 data class |
| 线程安全 | EventBus、ServiceLocator 均**线程不安全**，仅限 Unity 主线程使用。EmberSingleton 双检锁保证线程安全 |
| 事件泄漏 | Subscribe 后必须在对象销毁前调用 Unsubscribe，否则 handler 持有的引用会导致 GC 无法回收（事件泄漏） |
| 空引用 | `Subscribe` / `Unsubscribe` 对 null handler 静默返回（不抛异常）；`Dispatch` 在无订阅者时静默跳过 |
| 已知问题 | 无 `[Obsolete]`、`TODO`、`FIXME` 标记。`EmberObjectPool` 在池满时丢弃对象可能触发 GC（如果对象实现了 IDisposable 且 Dispose 中有重量操作） |
