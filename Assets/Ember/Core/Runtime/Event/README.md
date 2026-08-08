# Event — 事件总线

## 概述

提供框架级广播事件系统。int-key + 常量表避免 Key 冲突，Subscribe 返回 IDisposable（对齐 UniRx），
OnNext 广播事件。支持 0～4 个泛型参数，派发中安全增删。

## 文件清单

| 角色 | 路径 |
|------|------|
| 事件总线 | `EmberEventBus.cs` |
| 广播事件常量表 | `EmberBroadcastEvent.cs` |

## 公开 API

### EmberEventBus — 事件总线

静态类，线程不安全，仅限主线程使用。

| 方法 | 说明 |
|------|------|
| `Subscribe(int eventKey, Action) → IDisposable` | 订阅无参事件。Dispose 返回值即可取消订阅 |
| `Subscribe<T>(int eventKey, Action<T>) → IDisposable` | 订阅 1 参事件 |
| `Subscribe<T1,T2>(int eventKey, Action<T1,T2>) → IDisposable` | 订阅 2 参事件 |
| `Subscribe<T1,T2,T3>(int eventKey, Action<T1,T2,T3>) → IDisposable` | 订阅 3 参事件 |
| `Subscribe<T1,T2,T3,T4>(int eventKey, Action<T1,T2,T3,T4>) → IDisposable` | 订阅 4 参事件 |
| `Unsubscribe(int eventKey, Action)` | 取消订阅无参事件 |
| `Unsubscribe<T>(int eventKey, Action<T>)` | 取消订阅泛型事件（1～4 参对应重载） |
| `OnNext(int eventKey)` | 播报无参事件 |
| `OnNext<T>(int eventKey, T arg)` | 播报 1 参事件（支持 1～4 参） |
| `HasSubscribers(int eventKey) → bool` | 检查是否有订阅者 |
| `ClearSubscribers(int eventKey)` | 清除指定事件所有订阅者 |
| `ClearAllSubscribers()` | 清除所有事件订阅（仅退出/重置时使用） |

### 使用示例

```csharp
// 订阅（返回 IDisposable，对齐 UniRx）
var sub = EmberEventBus.Subscribe(EmberBroadcastEvent.ResourceReady, OnResourceReady);
// 播报
EmberEventBus.OnNext(EmberBroadcastEvent.ResourceReady);
// 取消订阅
sub.Dispose();
```

### EmberBroadcastEvent — 事件常量表

静态类，每个模块分配一个基址（间隔 1000），偏移 1～999。

| 模块 | 基址 | 事件 |
|------|------|------|
| Core | 1000 | CoreReady(1001), CoreShutdown(1002), GameStateChanged(1003), MainSceneReady(1006), OpeningAnimationEnd(1007) |
| Resource | 2000 | ResourceReady(2001), ResourceShutdown(2002) |
| UI | 3000 | UIReady(3001), UIShutdown(3002) |
| Scene | 4000 | SceneLoaded(4001), SceneUnloading(4002), SceneLoadStart(4003), SceneLoadDone(4004) |
| Audio | 5000 | AudioReady(5001), AudioShutdown(5002) |
| Input | 6000 | InputReady(6001), InputShutdown(6002) |
| Game | 10000 | 业务层预留基址 |

## 主流程

**订阅：** `Subscribe(key, handler)` → 判空 → InDispatch检查 → 派发中则延迟 → CombineInto字典 → 返回 Subscription

**播报：** `OnNext(key)` → 查找handler → EnterDispatch → handler.Invoke() → ExitDispatch → 深度归零时执行延迟操作

**取消：** `sub.Dispose()` 或 `Unsubscribe(key, handler)` → InDispatch检查 → RemoveFrom字典

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| 线程安全 | 线程不安全，仅限主线程 |
| 事件泄漏 | Subscribe 返回的 IDisposable 需在对象销毁前 Dispose，否则 handler 持有引用导致 GC 无法回收 |
| 空引用 | null handler 静默返回 Subscription.Empty；OnNext 无订阅者时静默跳过 |
| 参数上限 | 支持 0～4 个泛型参数，超过需自行封装 data class |
| 基址间隔 | 模块基址间隔 1000，偏移 1～999 |
