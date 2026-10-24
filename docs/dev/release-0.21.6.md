# Ember 0.21.6 — Play 脚本重载会话终止

发布版本：v0.21.6。仅发布框架包，不自动升级消费项目；异步业务所有权适配限制见下文。

## 契约

不支持保持当前业务会话的 Play 热重载。旧域中撤销就绪、停止转场入口、广播 CoreShutdown、取消会话并同步中止注册所有者，清理 Module/Manager。新域发出一次退出 Play 请求。用户再次进入 Play 正常启动；不自动保存、清档、续接或开始轮次。

IsInitialized 派生于 GameLauncher.Lifetime.IsReady 和 FSM。Lifetime 不跨重载序列化；CoreReady 播报前已经 Ready。State 区分 Starting/Ready/Stopping/Stopped/Faulted；StopReason 区分 Quit/ScriptReload/InitializationFailed。停止通知先于资源销毁，收到时应拒绝新输入。

捕获当前 Lifetime.Cancellation；迟到回调还须检查所捕获 lifetime.IsReady。使用 RegisterAbort 注册同步、幂等所有权释放；返回的 IDisposable 用于正常释放后注销。不能注册 async void。纯后台任务可使用 RegisterBackgroundOwner：任务必须响应会话取消且不依赖 Unity 主线程，确认后台完成后才释放资源；超时报告 Faulted，绝不假报已释放。不能把普通 Unity-context StopAsync 传入等待，以免主线程死锁。

这不是对任意既有业务任务的自动回收。现有 PlayerData.StopAsync 依赖主线程续体，需要消费端后续显式适配其资源所有权；本次不修改模板或 Windward，也不宣称 Windward 存储锁已经验收。未注册的业务后台任务不在所有权完成保证内。

## 验证

见 evidence/play-reload/validation.txt、tests.json、final-console.json、final-after.json、reentry.json 和 no-domain-*.json。首次候选重复退出引起 Unity EditModeScope 警告，修正后只剩预期的一条 Ember 退出诊断。四项新增测试通过，包括临时文件独占所有权和原保存字节保持。

未覆盖：Windward 消费适配、正在执行的业务转场/保存、所有输入/UI业务组合、关闭 Scene Reload、Player 构建。不能把本框架专项结果作为这些路径的签收。
