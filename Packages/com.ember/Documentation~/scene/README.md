# Scene — 场景管理

## 概述

Scene 模块负责场景的异步加载/卸载。EmberSceneManager 封装 Unity SceneManager，
SceneCoordinator 桥接状态机与场景管理器，实现状态切换自动加载/卸载对应场景。

正式玩法进入使用可等待的流程请求，完整准备、提交、取消及遮罩协议见[游戏流程转场](../core/game-transitions.md)。旧回调式场景 API 不提供同等结果/所有权保证，不能以 onComplete 被调用证明玩家准备成功。

## 文件清单

| 角色 | 路径 |
|------|------|
| 场景管理器 | [EmberSceneManager.cs](../../Scene/Runtime/EmberSceneManager.cs) |
| 请求级场景端口 | [EmberSceneManager.Transitions.cs](../../Scene/Runtime/EmberSceneManager.Transitions.cs) |
| 转场执行器 | [EmberSceneTransitionRunner.cs](../../Scene/Runtime/EmberSceneTransitionRunner.cs) |
| 状态机↔场景桥接器 | [SceneCoordinator.cs](../../Scene/Runtime/SceneCoordinator.cs) |

## 依赖

| 依赖 | 类型 | 说明 |
|------|------|------|
| `Ember.Core` | 框架模块 | EmberSingleton、IEmberManager、EmberEventBus、GameLauncher（日志 EmberDebug 来自 Basic） |
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
| `LoadForTransitionAsync(sceneName, cancellation)` | 正式转场 Additive 加载；返回本请求是否取得场景及错误，不提前发布 CurrentScene |
| `UnloadForTransitionAsync(sceneName)` | 可等待的清理，完成后检查卸载结果 |
| `SetCurrentScene(sceneName)` | 验证场景已加载；由转场成功提交后发布 |
| `LoadSceneSilentlyAsync(sceneName)` / `UnloadSceneSilentlyAsync(sceneName)` | 静默场景块操作；不代表流程准备/提交完成 |
| `GetSceneRoots(sceneName)` | 已加载场景根对象，未加载返回空数组 |

**属性：**

| 属性 | 说明 |
|------|------|
| `CurrentScene` | Manager 记录的场景名；正式转场仅提交后发布，旧 StartLoad 在开始时赋值；不等同 Unity ActiveScene |
| `IsLoading` | 是否正在加载 |
| `Progress` | 真实场景加载进度（0.0～1.0）；不含业务准备，UI 不能仅凭它到1就宣布全部就绪 |
| `OnBeforeActivate` (事件) | 激活前通知 `(Scene scene, Action activate)`；正常继续必须调用 activate，正式请求取消时仍等待底层激活后清理 |

### SceneCoordinator — 状态机桥接器

继承 EmberSingleton，实现 IEmberManager。[EmberInitOrder(Scene)]。

在 Init 阶段注入 `EmberStateMachine.OnSceneTransitionAsync`，交由 `EmberSceneTransitionRunner` 管理场景所有权；同步 `LoadSceneAsync` 接线保留给启动预加载。UI 通过 `WrapTransitionAsync` 包裹并等待完整操作，不自行重复加载/卸载。

- 替换：加载候选 → 等待本请求准备 → 旧状态退出及目标进入 → 发布新场景 → 卸载旧场景。
- 覆盖：无场景或同场景目标沿覆盖语义准备/暂停/压栈，保留承载场景。
- Pop：检查下层场景并恢复已有状态，不重新执行进入准备。
- 原位请求：在当前状态内准备/交接资源，不装卸场景或重新进入Gameplay。

## 主流程

**场景加载：** `LoadSceneAsync(name, onComplete)` → [IsLoading 防重入] → `StartLoad` → `SceneManager.LoadSceneAsync` → UniTask 异步驱动 → 等待 progress 达 0.9 → OnBeforeActivate 触发 → 等待 activate → 等待 isDone → Dispatch(SceneLoaded + SceneLoadDone) → onComplete

**正式转场联动：** RequestTransitionTo → OnSceneTransitionAsync → SceneCoordinator 的遮罩包装 → Runner 准备/提交/清理 → 等待 Completion，并按需要检查 WhenInteractive。准备失败只清理本请求取得的候选，提交后异常不承诺业务自动回滚。

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| 初始化 | 继承 EmberSingleton（非 MonoSingleton），由 ManagerCollector 自动初始化 |
| 单加载锁 | 旧回调 API 忙碌时告警并直接回调；正式 LoadForTransitionAsync 返回明确失败，不能将两者混用为成功信号 |
| OnBeforeActivate | 正常路径须完成 activate；正式取消不能强停 Unity 加载，Runner 在其激活后按所有权清理 |
| 假进度 | UI 平滑与业务准备分离；必要准备未通过不得显示完成，完整退出遮罩后才交还操作 |
| UniTask | 使用 UniTask 替代协程驱动异步流程 |
