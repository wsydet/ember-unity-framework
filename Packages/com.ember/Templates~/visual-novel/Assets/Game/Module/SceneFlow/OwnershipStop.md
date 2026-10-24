# 会话停止与异步所有权

框架 0.22 的正常退出与编译重载先撤销 Lifetime 就绪资格，再等待停止任务，最后销毁 Module/Manager。不自动保存、续玩或启动下一轮。`GameLauncher.CurrentLifetime` 只查询现有会话，不创建单例。

## 正式模板接线

`PlayerDataModule.OnInit` 登记 `RegisterStopOwner(StopAsync, backgroundIndependent: true)`。停止函数在控制线程同步封闭世代；返回的目录释放任务在后台完成，不需要 Unity 续体。不要把旧版本 StopAsync 放进 Task.Run，也不要用 WhenReady 代替释放凭证。

`GameSceneSession` 创建时登记 `RegisterStopOwner(StopAsync)`；`GameSceneScope.StopAsync()` 可用于停止尚未返回 session 的准备过程。`IsDisposed` 只代表入口已关闭，`WhenReleased` 才代表准备、替换、迟到结果和占用清理全部结束；清理异常使任务失败。主线程只能 await，不能 Wait/GetResult。

```csharp
var lifetime = GameLauncher.CurrentLifetime; // 操作开始前捕获
var operation = fsm.RequestTransitionTo<GameplayState>(cancellation: lifetime.Cancellation);
var result = await operation.Completion;
if (!lifetime.IsReady || !operation.CanInteract) return;
// 此处才发布本次操作的业务/UI 结果。
```

消费者自己的 await 后发布、UI 回调及 finally 同样必须检查捕获的 Lifetime。释放只能作用于本请求取得的句柄；不能通过当前单例释放新会话的资源。参与者返回前取得的资源仍由参与者负责，取消后必须结束准备并释放或返回可释放结果。

## 存储凭证

在创建线程调用 `service.BeginStopOwnership()`，同步拒绝新请求，封闭旧代 codec/状态发布。重复调用返回同一个 `ProgressStorageStop`：

1. `WhenIoTerminated`：原后台准备/IO 已结束。
2. `WhenResultsSettled`：停止时仍在途的请求终态已确定，不包含此前已发布的历史请求。
3. `WhenOwnershipReleased`：目录资源 Dispose 成功；`IsOwnershipReleased` 才为 true。

这三个任务的完成不依赖 Unity 主线程；可以在不可异步的中止边界进行有界等待。等待超时不取消停止任务，也不释放锁。保存失败与释放成功是独立事实：IO 异常可产生 WriteFailed 并正常释放目录；目录释放失败使释放任务失败，不能报告成功。

原子索引提交前取消保留旧档；提交点之后取消仍返回 Committed。无法验证提交状态时保留 CommitUncertain，不能伪报 Cancelled、回滚或继续覆盖未知提交。重新准备按磁盘恢复协议裁决。停止不发起额外保存。

## 转场边界

`fsm.StopTransitionsAsync()` 同步撤销入口与交互资格，再返回本请求真实 Completion（无在途请求时结果为 null）。加载不可取消，须等待实际加载、释放本请求取得的候选；已有场景不属于本请求。准备期间取消须等待参与者清理；卸载期间停止须等待卸载。提交开始后不承诺回滚；OnExit 中停止不得迟到进入目标，已完成提交不得改写为取消。

正常异步停止期间 Manager 保持存活。Loading 在同步中止阶段释放本会话遮罩，不依赖停止后的业务 Update 驱动淡出；正常业务 Loading 表现与计时不变。

编译重载在 compilationStarted 建立临时重载屏障，收束后退出 Play，再允许域重载。10 秒未完成会报错并保留屏障和所有权，直到实际完成；停止任务失败也保留屏障并报告故障，不会假报成功或恢复旧业务。不要以强制重载来解除未完成 IO。

没有 compilationStarted 的直接域重载、编辑器强制退出 Play 属于同步兜底：只等待声明为纯后台的所有者，绝不同步等待 Unity 异步任务。Unity 原生场景随退出 Play 接管；未完成的托管清理报告 Faulted，不能当作 WhenReleased 成功。自定义异步参与者不能假设旧域 finally 会执行。要获得托管清理完成保证，使用 ShutdownFrameworkAsync 或正常编译停止路径。

## Windward 后续最小步骤

当前不升级或部署 Windward。后续另行升级框架与合并正式模板差异，保留产品实现；为每个异步操作捕获所属 Lifetime、传取消令牌、门控 await 后业务发布/UI/finally；在隔离目录重复保存及转场分段验收。框架发布不代表 Windward 已适配或已验收。
