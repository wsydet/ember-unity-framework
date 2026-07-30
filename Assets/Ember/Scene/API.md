# 模块名称：场景管理（Scene）

---

## 1. 快速上手

```csharp
// 加载场景（Additive 模式）
EmberSceneManager.Instance.LoadSceneAsync("Battle", () =>
{
    Debug.Log("战斗场景就绪");
});

// 过渡切换：加载新场景 → 卸载旧场景
EmberSceneManager.Instance.TransitionTo("Battle", "MainMenu");

// 带激活前初始化的加载
EmberSceneManager.Instance.OnBeforeActivate += (scene, activate) =>
{
    // 场景加载到 90%，激活前执行初始化
    Debug.Log($"场景 {scene.name} 准备激活");
    activate(); // 必须调用，否则场景永远不会激活
};
```

---

## 2. 模块概述

Scene 模块封装了 Unity 场景的异步加载/卸载和过渡流程。基于协程实现进度轮询，
在场景加载到 90% 时通过 `OnBeforeActivate` 事件提供激活前初始化窗口，
并广播 `SceneLoaded` / `SceneUnloading` 生命周期事件供其他模块响应。

---

## 3. 依赖关系

| 依赖 | 类型 | 说明 |
|------|------|------|
| `Ember.Core` | 框架模块 | EmberMonoSingleton（单例基类）、EmberEventBus（派发 SceneLoaded/SceneUnloading）、EmberBroadcastEvent（事件 Key 常量） |
| `UnityEngine` / `UnityEngine.SceneManagement` | 引擎 | SceneManager、AsyncOperation、LoadSceneMode、Scene、MonoBehaviour |
| `System` / `System.Collections` | 标准库 | Action、IEnumerator |

---

## 4. 文件清单

| 角色 | 路径 |
|------|------|
| 主逻辑入口 | `Assets/Ember/Scene/Runtime/EmberSceneManager.cs` |
| 核心接口 | 无（当前仅为单一 Manager 类） |
| 核心数据类 | 无 |
| 视图/表现层 | 无 |
| 编辑器扩展 | 无 |

---

## 5. 公开 API

### 5.1 入口类型

| 类型 | 职责 | 获取方式 |
|------|------|----------|
| `EmberSceneManager` | 场景异步加载/卸载/过渡的统一入口 | `EmberSceneManager.Instance`（MonoSingleton） |

### 5.2 核心方法

| 方法签名 | 说明 |
|----------|------|
| `LoadSceneAsync(string sceneName, Action onComplete)` | 异步加载场景（Additive 模式），不卸载现有场景 |
| `LoadSceneSingleAsync(string sceneName, Action onComplete)` | 异步加载场景（Single 模式），卸载所有已加载场景 |
| `UnloadSceneAsync(string sceneName, Action onComplete)` | 异步卸载场景，派发 SceneUnloading 事件 |
| `TransitionTo(string newScene, string oldScene, Action onComplete)` | 过渡切换：加载新场景 → 卸载旧场景 |
| `CurrentScene` (属性) | 当前活跃场景名 |
| `IsLoading` (属性) | 是否正在加载场景中 |
| `Progress` (属性) | 当前加载进度（0.0 ~ 1.0），未加载时为 1.0 |
| `OnBeforeActivate` (事件) | 场景加载到 90% 时触发，回调参数为 `(Scene scene, Action activate)`，必须调用 activate 才会激活场景 |

---

## 6. 主流程

**流程一：异步加载场景（Additive）**
`[外部] LoadSceneAsync(name, onComplete)` → `[IsLoading 防重入检查]` → `StartLoad(name, Additive, onComplete)` → `[标记 IsLoading, Progress=0]` → `SceneManager.LoadSceneAsync(name, Additive)` → `[op null: LogError + 重置]` → `op.allowSceneActivation = false` → `StartCoroutine(LoadRoutine)` → `[协程每帧轮询 progress]` → `[达到 0.9: 触发 OnBeforeActivate(scene, activateCb)]` → `[等待 activate 回调]` → `op.allowSceneActivation = true` → `[等待 isDone]` → `Progress=1, IsLoading=false` → `Dispatch(SceneLoaded)` → `onComplete?.Invoke()`

**流程二：卸载场景**
`[外部] UnloadSceneAsync(name, onComplete)` → `[检查场景是否已加载]` → `Dispatch(SceneUnloading)` → `SceneManager.UnloadSceneAsync(name)` → `[completed: 清空 CurrentScene]` → `onComplete?.Invoke()`

**流程三：过渡切换**
`[外部] TransitionTo(newScene, oldScene, onComplete)` → `LoadSceneAsync(newScene, callback)` → `[回调: 若 oldScene 非空则 UnloadSceneAsync(oldScene, onComplete)]` → `[否则直接 onComplete]`

---

## 7. 修改影响范围

- **新增加载模式（如并行加载多个场景）** → 改 `StartLoad` 或新增方法，同步处理 IsLoading 锁逻辑
- **调整激活前初始化行为（如超时自动激活）** → 改 `LoadRoutine` 协程中 `OnBeforeActivate` 后的等待逻辑
- **新增场景预加载/缓存** → 新增方法，可能需要维护场景名→AsyncOperation 的缓存字典
- **调整进度上报方式（如改为事件推送）** → 在 `LoadRoutine` 中添加事件派发或 Progress 属性更新逻辑
- **新增过渡效果（Loading 界面）** → 改 `TransitionTo`，在加载前 Push Loading 页面

---

## 8. 约束与已知陷阱

| 类别 | 说明 |
|------|------|
| 初始化顺序 | 依赖 `EmberEventBus`（派发 SceneLoaded/SceneUnloading），无其他模块初始化依赖 |
| 生命周期 | `EmberSceneManager` 继承 `EmberMonoSingleton`，挂载 `DontDestroyOnLoad`。加载协程运行在此 GameObject 上，销毁时协程自动终止。同时只能有一个加载操作进行中（IsLoading 防重入） |
| 数据边界 | 场景名必须在 Build Settings 中注册，否则 `LoadSceneAsync` 返回 null + LogError。`OnBeforeActivate` 回调中**必须调用 activate 参数**，否则场景永远停留在 0.9 不会激活（协程会无限等待） |
| 线程安全 | 所有操作仅限主线程。协程轮询基于 `WaitForEndOfFrame` |
| 事件泄漏 | `OnBeforeActivate` 使用 `+=` 订阅，无自动清理机制。订阅者需在对象销毁前手动 `-=`，否则 EmberSceneManager 会持有已销毁对象的引用 |
