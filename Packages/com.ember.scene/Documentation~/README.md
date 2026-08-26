# Scene — 场景管理

## 概述

Scene 模块负责场景的异步加载/卸载。EmberSceneManager 封装 Unity SceneManager，
SceneCoordinator 桥接状态机与场景管理器，实现状态切换自动加载/卸载对应场景。

## 文件清单

| 角色 | 路径 |
|------|------|
| 场景管理器 | `Runtime/EmberSceneManager.cs` |
| 状态机↔场景桥接器 | `Runtime/SceneCoordinator.cs` |

## 依赖

| 依赖 | 类型 | 说明 |
|------|------|------|
| `Ember.Core` | 框架模块 | EmberSingleton、IEmberManager、EmberEventBus、EmberDebug、GameLauncher |
| `UniTask` (Cysharp) | 第三方 | 异步加载驱动 |

## 公开 API

### EmberSceneManager — 场景管理器

继承 EmberSingleton，实现 IEmberManager。[EmberInitOrder(Scene)]。

| 方法 | 说明 |
|------|------|
| `LoadSceneAsync(string sceneName, Action onComplete)` | Additive 异步加载。加载中再次调用会被忽略 |
| `LoadSceneSingleAsync(string sceneName, Action onComplete)` | Single 模式加载（卸载所有已加载场景） |
| `UnloadSceneAsync(string sceneName, Action onComplete)` | 异步卸载场景 |
| `TransitionTo(string newScene, string oldScene, Action onComplete)` | 过渡：加载新场景 → 卸载旧场景 |
| `IsSceneLoaded(string sceneName) → bool` | 检查场景是否已加载 |

**属性：**

| 属性 | 说明 |
|------|------|
| `CurrentScene` | 当前活跃场景名 |
| `IsLoading` | 是否正在加载 |
| `Progress` | 真实加载进度（0.0～1.0），供 UI 层（如 EUILoading）读取并自行做假进度平滑 |
| `OnBeforeActivate` (事件) | 加载到 90% 时触发 `(Scene scene, Action activate)`，必须调用 activate |

### SceneCoordinator — 状态机桥接器

继承 EmberSingleton，实现 IEmberManager。[EmberInitOrder(Scene)]。

在 Init 阶段注入 `EmberStateMachine.OnSceneTransition` 钩子，使状态机切换时自动：
- TransitionTo：加载新场景 → 执行状态生命周期 → 卸载旧场景
- Push：加载覆盖场景 → 执行状态生命周期（底层场景保留）
- Pop：执行状态生命周期 → 卸载覆盖场景

## 主流程

**场景加载：** `LoadSceneAsync(name, onComplete)` → [IsLoading 防重入] → `StartLoad` → `SceneManager.LoadSceneAsync` → UniTask 异步驱动 → 等待 progress 达 0.9 → OnBeforeActivate 触发 → 等待 activate → 等待 isDone → Dispatch(SceneLoaded + SceneLoadDone) → onComplete

**状态切换联动：** TransitionTo → SceneCoordinator.HandleTransitionTo → LoadSceneAsync → ctx.Proceed() → UnloadIfDifferent

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| 初始化 | 继承 EmberSingleton（非 MonoSingleton），由 ManagerCollector 自动初始化 |
| 单加载锁 | IsLoading=true 时拒绝新的加载请求（LogWarning + 直接回调） |
| OnBeforeActivate | 必须调用 activate 参数，否则场景永远停留在 0.9 |
| 假进度 | EmberSceneManager 只提供真实 Progress，假进度平滑逻辑已移至 EUILoading（displayMaxRatio + smoothDuration） |
| UniTask | 使用 UniTask 替代协程驱动异步流程 |
