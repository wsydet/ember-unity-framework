# 游戏流程转场

本页对应 `Ember.Core.EmberStateMachine` 的本地未发布实现。它协调全局游戏状态、场景与 Loading；
角色行为使用独立的[通用层级 FSM](hierarchical-fsm.md)。本页定义公共协议；玩家会话、暂停与恢复的实际接线由所属模板负责，不能仅因引用公共协议就视为完成。

## 请求与结果

正式 GameLauncher 启动时由 SceneCoordinator 装配异步场景桥接，EUIManager 提供默认 Loading 遮挡。
业务在 Unity 主线程发起请求并等待结果：

```csharp
var request = GameLauncher.Instance.Fsm.RequestTransitionTo<GameplayState>(
    args: selectedSession, cancellation: lifetimeToken);
var result = await request.Completion;
if (result.Status != EmberTransitionStatus.Completed)
{
    // 按 Status、CommitStarted 和 Error 显示失败、重试或进入项目定义的恢复流程。
}
```

`selectedSession` 和 `lifetimeToken` 是项目提供的进入参数及生命周期令牌。请求 ID 在每台流程机内递增。
`RequestPush<T>` 和 `RequestPop` 同样返回 `EmberTransitionOperation`。旧 `TransitionTo`、`Push`、`Pop`
仍可调用，但只发起请求并记录失败；调用返回不表示完成。每台流程机从准备直到清理结束只允许一个在途请求，
后续请求立即返回 Busy，不排队，也不抢占当前请求。正常重试应先等待旧请求 Completion。

| 结果 | 含义 |
| --- | --- |
| Completed | 状态已提交，场景释放及默认 Loading 关闭已完成 |
| Busy | 另一个请求或 Guard 仍在执行，本请求未接受 |
| Rejected | 状态未注册、禁止重入、Guard 拒绝/异常或空栈 Pop |
| Cancelled | 提交前取消或请求失效，已完成可执行的候选清理 |
| PreparationFailed | 加载或准备失败，未开始业务生命周期提交 |
| CommitFailed | 退出/暂停/进入/恢复等提交生命周期中发生异常，不保证旧会话完整 |
| CleanupFailed | 候选清理、已提交后的场景释放或遮挡清理失败；检查 CommitStarted 区分阶段 |
| Stopped | 框架已封闭该流程机的转场入口 |

结果保留 Error；失败不转换为默认成功。清理失败可能保留场景或资源，不能直接假定重试前环境已复原。

## 准备与交接

普通替换转场按以下顺序执行：

1. 打开默认 Loading（若已配置）。
2. Additive 加载候选场景，保留旧场景；候选的 Awake/OnEnable 只做实例绑定。
3. 等待目标状态 `PrepareEnterAsync(SceneTransitionContext request)`。
4. 检查取消及请求有效性，进入不可逆提交区。
5. 执行旧状态及覆盖栈的 OnExit，然后目标 OnEnter(request.PreparedArgs)。
6. 发布当前场景并卸载旧场景；最后关闭 Loading，完成请求。

request 提供 Args、PreparedArgs、Operation、Cancellation、CommitStarted、IsCommitted。
PreparedArgs 初始为 Args，准备钩子可替换为本请求独立持有的候选结果。默认请求级准备调用既有无参重载，
旧状态无需为了兼容而空改代码。新玩家接线应使用请求级钩子，在 Gameplay Module 的 Reset/OnInit 后，
通过 OnGameplayEnter 接收候选，避免把准备结果放入会被 Reset 擦除的活动运行字段。

准备已开始且未开始提交时，失败或取消会调用一次 `AbortPrepareAsync(request)`。
清理可能面对只完成一部分的候选；它在候选场景卸载之前执行。旧无参准备若取得资源，也应补充该清理钩子。
只有本请求加载取得的候选场景会在失败时卸载，原先已经加载的场景不会被误删。

目标无场景或与当前承载场景相同，沿用覆盖语义：准备后暂停旧状态并压栈；场景为空的覆盖状态保留承载场景。
Pop 验证下层场景仍存在，退出上层并恢复下层，不重新执行进入准备。从覆盖状态替换到其他场景时，
其被暂停的状态也会退出，关联旧场景去重释放。

`skipSceneLoad: true` 只跳过加载，仍验证目标已加载并等待准备。`Start<T>` 是同步启动专用入口，
不能作为正式会话准备的替代路径。InitState 的首次场景预加载保留原启动通道。

## 取消、迟到结果与提交边界

`request.Cancel()` 或外部令牌只请求取消，调用者仍须等待 Completion。
Unity 场景加载无法强行终止，已经发起的加载会先完成激活，再清理本请求取得的场景。
准备钩子应响应令牌；如果其底层任务不能取消，框架会等待它返回，检查请求有效性后清理候选，不让迟到结果进入。

Proceed 带单次提交及当前请求检查，重复、过期、取消或框架停止后的调用不会启动旧会话。
一旦进入退出/暂停等业务生命周期，取消不再撤销提交；结果报告真实提交或失败。
若 OnEnter 等在中途抛异常，框架保留场景并返回 CommitFailed，由业务明确处理，不能宣称自动回滚。

GameLauncher 销毁前调用 StopTransitions，封闭入口并取消未提交的活动请求。
该同步销毁入口不保证等待异步清理，更不自动保存。正常保存后退出必须由业务先等待保存及转场结果，
再释放业务对象或退出程序。

## 场景与 Loading 扩展

`EmberSceneTransitionRunner` 持有 `IEmberTransitionScenes`；默认端口是 EmberSceneManager。
加载结果 `EmberTransitionSceneLoad` 显式区分是否由本请求取得场景及错误，忙碌/缺失场景不能伪装成功。
候选加载不发布 CurrentScene，只有状态成功提交才更新。

旧 `SceneCoordinator.InterceptSceneLoad` 已由以下接口替代，自定义 Loading 集成需要迁移：

```csharp
SceneCoordinator.WrapTransitionAsync = async (request, operation) =>
{
    // 自定义遮挡打开并就绪。
    try { await operation(); }
    finally { /* 等待遮挡关闭，不能被已取消的请求令牌中断清理。 */ }
};
```

包装器必须等待 operation，不得自行重复加载/卸载场景，不得直接调用 Proceed。
默认 EUI 实现使用 RunWithLoadingAsync 的共同流程，并在正常完成后等待页面 IsTransitionReady。
自定义宿主应设置 `OnSceneTransitionAsync = runner.RunAsync`；框架已连接时不要覆盖该桥接。
旧 `OnSceneTransition` 委托仅在没有异步桥接时兼容执行；其自定义加载和清理仍由原集成负责，
没有自动获得默认 SceneCoordinator 的场景所有权保障。

外层业务 `RunWithLoadingAsync` 的遮挡可被转场复用；外层应等待转场 Completion 并检查结果，
再结束自己的操作。转场 Completion 在这种情况下不包含外层业务随后才关闭的遮挡。

## 请求级进入边界

`EmberTransitionOperation` 现在提供三个可多次等待的阶段结果，均携带本请求 RequestId：

| 信号 | Completed 的含义 |
| --- | --- |
| `WhenPrepared` | 目标的请求级准备全部返回成功，并通过取消/陈旧检查；Pop 已完成场景检查，无须重新准备 |
| `WhenRunning` | OnEnter 或 OnResume 及当前提交通知成功；Gameplay 的 Module 激活和候选交接仍在原生命周期内 |
| `WhenInteractive` | 事务成功、场景清理成功，且真正的遮罩拥有者已报告完整退出；不会仅因内部 Completion 完成而成功 |

阶段结果不是整个请求的终态。已经到达的阶段保留历史成功；未到达的阶段在失败、取消或拒绝时取得失败结果，
等待结束不等于成功，必须检查 Status。`IsPrepared` 供 Loading 查询；`IsRunning`、`CanInteract` 还检查进入是否仍有效。
后续提交（含 Push/Pop）、显式 Start 或 StopTransitions 使先前进入失效；旧遮罩迟到完成不能让旧请求重新有效。
业务续体仍要检查自己的角色/场景/会话世代和其他输入禁止，只释放本次输入句柄，不强行打开通道。

默认 EUI 为每个请求登记独立 `EmberTransitionPresentation`，并在完整关闭回调之后完成它。
复用外层 Loading 时，外层负责完成凭证；内部请求可先完成 Completion，WhenInteractive 保持等待。
**外层 operation 不能等待 WhenInteractive**，否则它在等待自己稍后才会关闭的遮罩。
外层业务失败、UI 销毁或凭证被放弃时 WhenInteractive 返回失败；已经完成的内部 Completion 保留真实提交结果。
快速、skipSceneLoad 和覆盖入口也经过包装器以识别外层遮罩；没有外层遮罩时仍不额外创建默认 Loading。
遮罩正在打开或关闭时不接受新的场景请求，避免将新请求挂到正在结束的拥有者上。

自定义遮罩在执行 `operation` 前调用 `request.HoldPresentation()`，只有一个拥有者可登记：

```csharp
using var presentation = request.HoldPresentation();
try
{
    await operation();
    await CloseFullCurtainAsync(); // 项目提供：等待所有退出动画真正结束。
    presentation.Complete();
}
catch (Exception error)
{
    presentation.Fail(error.ToString());
    throw;
}
```

上例仅展示凭证协议，失败路径仍须由实际遮罩拥有者执行可等待的清理。
未登记可见过渡的入口默认按无额外遮罩处理；自定义桥接不得省略真实外层拥有者的登记。
`EUILogic.TransitionOperation` 暴露当前 Loading 承载的请求，结束后清除；base Loading 将据此限制进度上限。
这只是通用协议，不能替代业务显式汇总必要资源，也不自动暂停世界或驱动角色。

历史首轮记录（2026-10-09 R01.1）：新增 13 项 EditMode 和 5 项 PlayMode 测试源码后，Unity MCP 只读比较首次调用失败，停止自动验证。该轮未完成编译、测试或真实场景验收；续验结果见下方历史记录。当时尚未修改 base 假进度、实现 SceneScope 或接入玩家输入句柄，不表示这些业务接线目前仍未完成。

Loading 不负责暂停世界、物理、玩家 FSM 或音频。若项目冻结游戏时间，页面动画及进度应使用合适的
非缩放时间；暂停所有权与恢复由业务层显式协调。存档也不隐式冻结时间。

需要业务显式决定揭幕时，使用 [Loading 会话公开契约](../ui/loading-sessions.md)。外层 Covered 后再发起请求，
内层 Completion 允许恢复事务释放内部暂停，外层 Reveal 完成后才结算 WhenInteractive；不能在揭幕许可逻辑中先等 WhenInteractive。

## 验证范围

原转场实现的历史验证，Unity 6000.5.4f1，通过 Unity MCP 执行：

- `Ember.Scene.Transitions.Tests`：19 项 EditMode 测试，通过；覆盖准备/取消/迟到回调、提交与清理失败、
  场景所有权、覆盖栈、skipSceneLoad、关闭入口及旧入口兼容。
- `Ember.Scene.Transitions.PlayModeTests`：3 项通过；真实 EUI 页面生命周期验证成功、准备失败、取消后的遮挡关闭。
- 既有 `Ember.Core.Fsm.Tests` 52 项和 `Ember.UI.Tests.UIModuleEditTests` 50 项回归通过。
- 真实 platformer 编辑副本完成 FrameworkScene 启动、Main → Gameplay → Settings → Pop → Main；
  各请求 Completed，替换后的旧场景已卸载，覆盖状态保持 GameplayScene；运行 Console 无错误或警告。

场景端口的故障与迟到完成使用可控替身；不等同于设备上的真实加载故障/系统退出验收。
该批验证当时未覆盖玩家、新游戏/恢复进度、世界/局部暂停或存档后退出；业务模板的后续结果应独立核对。

2026-10-09 R01.1 续验：Unity MCP 确认编译完成，Console error 为 0；扩充后的
`Ember.Scene.Transitions.Tests` 32/32、`Ember.Scene.Transitions.PlayModeTests` 8/8 通过。
补充真实入口回归时临时查询误用 `Game.GameLauncher` 类型名，查询编译失败后按仓库规则停止自动验证并退出 Play。
本轮未完成真实游戏入口全链路回归；上述历史场景记录不作为 R01.1 新增行为的通过证据。

2026-10-09 再次续验：从实际 `Ember.Core.GameLauncher` 正常启动，Main → Gameplay → Settings → Pop → Main
四个请求均 Completed，准备、运行和可交互阶段均成功；覆盖保留 GameplayScene，Pop 恢复玩家与 timeScale，
返回 Main 后 GameplayScene 卸载且玩家会话清空。旧 Gameplay 请求在后续提交后 CanInteract 保持 false。
流程结束及退出 Play 后 Console error/warning 均为 0，R01.1 验收完成。
该回归使用正式 FSM 请求 API，不包含人工输入、保存退出、死亡恢复或当时尚未实现的 SceneScope/统一暂停验收。

## 当前状态内的资源交接

`RequestInPlace<T>(args, cancellation)` 显式发起原位事务。T 必须是当前注册状态，须实现
`IEmberInPlaceState` 并在 GetEdges 声明到自身的边；Guard 只在发起时执行，未声明或 Guard 拒绝均拒绝请求。
此接口与 AllowReEnter 独立，普通 TransitionTo/Push 的同场景覆盖语义保持原样。

调用链为默认 Loading → 验证当前场景仍已加载 → PrepareInPlaceAsync → Prepared →
CommitInPlace → Running → 完整遮罩退出 → Interactive。复用外层遮罩时，仍须等待实际拥有者完成凭证。
原位请求不加载、卸载或重新发布当前场景，不改变 Current、Previous 或覆盖栈，也不调用
OnExit/OnEnter/OnPause/OnResume；Gameplay Phase 的持续激活由此得到保留。

准备须产生隔离候选；准备失败、取消或迟到失效时调用 AbortInPlaceAsync 一次，只清理候选。
提交区开始后异常报告 CommitFailed，不宣称框架能够回滚业务资源；业务实现仍须先充分验证、再进行交接，
并负责提交清理。并发 Busy、停止、取消、旧进入失效及 Prepared/Running/Interactive 沿用同一请求协议。

框架不因该请求自动暂停世界。整体暂停、SceneScope 候选归属、玩家快照与恢复目标属于业务接线，
公共协议测试不能代替这些业务入口的验收。R04.2.2a 当时新增 10 项 EditMode 协议测试及 1 项真实 EUI PlayMode 测试，结果如下。

R04.2.2a 验收（2026-10-09）：Unity 编译完成无错误，另有 3 条未改动 UIExtension 脚本警告。
EditMode 94/94、域重载后 PlayMode 180/180 通过，分别包含新增 10 项原位协议和 1 项真实 EUI 遮罩测试。
首次跨模式组合测试因 Test Framework EditModePcHelper 清理异常导致连锁失败，未改代码/断言，域重载后同组全过。该结果验证公共协议，不代表玩家正式恢复链完成。
