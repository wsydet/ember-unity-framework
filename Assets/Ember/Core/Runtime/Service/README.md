# Service — 服务基础设施

## 概述

提供服务定位器（接口驱动注册/解析）、单例基类（纯 C# + MonoBehaviour）、对象池（class 复用）、
SO 基类（Odin 继承溯源面板）。

## 文件清单

| 角色 | 路径 |
|------|------|
| 服务定位器 | `EmberServiceLocator.cs` |
| 纯 C# 单例 | `EmberSingleton.cs` |
| 对象池 + IPoolable | `EmberObjectPool.cs` |
| SO 基类 | `EmberBaseSO.cs` |

## 公开 API

### EmberServiceLocator — 服务定位器

静态类，线程不安全，仅限主线程。接口→实现映射，支持立即注册和延迟工厂注册。

| 方法 | 说明 |
|------|------|
| `Register<T>(T instance)` | 注册服务实例，重复注册抛 InvalidOperationException |
| `RegisterLazy<T>(Func<T> factory)` | 延迟注册，首次 Resolve 时调用工厂并缓存 |
| `TryRegister<T>(T instance) → bool` | 尝试注册，已存在返回 false 不抛异常 |
| `Resolve<T>() → T` | 解析服务，未注册抛 InvalidOperationException |
| `TryResolve<T>() → T` | 尝试解析，未注册返回 null |
| `IsRegistered<T>() → bool` | 检查是否已注册（含延迟） |
| `Unregister<T>() → bool` | 注销服务 |
| `ClearAll()` | 清除所有服务（自动释放 IDisposable 实例） |
| `RegisteredCount` | 已注册服务总数 |

### EmberSingleton<T> — 纯 C# 单例

线程安全（双检锁+volatile），懒初始化。用于不需要 GameObject 的纯逻辑管理器。

| 成员 | 说明 |
|------|------|
| `Instance` (静态属性) | 获取单例，首次访问自动创建 |
| `IsValid` (静态属性) | 是否已创建，不触发创建 |
| `Destroy()` (静态) | 销毁实例，调用 OnDestroy 钩子 |
| `OnDestroy()` (protected virtual) | 子类可重写清理逻辑 |

### EmberMonoSingleton<T> — MonoBehaviour 单例

用于需要挂载 GameObject 的组件单例。DontDestroyOnLoad。

| 成员 | 说明 |
|------|------|
| `Instance` (静态属性) | 获取单例，场景中查找或自动创建 |
| `IsValid` (静态属性) | 是否已创建 |
| `OnSingletonAwake()` (protected virtual) | 替代 Awake，首次创建时调用一次 |
| `OnSingletonDestroy()` (protected virtual) | 替代 OnDestroy 的清理钩子 |

### EmberObjectPool<T> — 对象池

复用 class 实例，减少 GC。支持 IPoolable 回调、IDisposable 清理、统计信息。

| 方法 | 说明 |
|------|------|
| 构造 `(initialCapacity, maxCapacity, trackStats)` | 创建池 |
| `Get() → T` | 获取对象，池空则 new |
| `Return(T obj)` | 归还对象，池满则丢弃（自动 Dispose） |
| `Prewarm(int count)` | 预分配 |
| `Clear()` | 清空（自动 Dispose） |

### IPoolable — 池化对象接口

| 方法 | 说明 |
|------|------|
| `OnTakeFromPool()` | 从池中取出时调用 |
| `OnReturnToPool()` | 归还到池中时调用，在此重置对象状态 |

### EmberBaseSO — SO 基类

继承 ScriptableObject，在 Inspector 顶部显示类型继承链。

## 主流程

**服务解析：** `Resolve<T>()` → 查 _services → 未命中查 _lazyFactories → 调用工厂+缓存 → 返回

**对象池：** `Get()` → _free.Pop() 或 new → IPoolable.OnTakeFromPool → 返回
`Return(obj)` → 判空+容量检查 → IPoolable.OnReturnToPool → Push

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| 线程安全 | ServiceLocator 线程不安全。EmberSingleton 双检锁线程安全 |
| 单例生命周期 | MonoSingleton 挂 DontDestroyOnLoad；EmberSingleton.Destroy() 需手动调用 |
| 对象池满 | 池满时归还对象会被丢弃（若实现 IDisposable 则自动 Dispose） |
