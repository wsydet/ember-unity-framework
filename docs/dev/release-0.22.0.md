# Ember 0.22.0：进行中存储与转场停止协议

基线为 0.21.6 / ac46d7cd6e9f93a4bf59468a4b0de716ae0b9c66，验证编辑器 Unity 6000.5.4f1。此版本是框架与正式模板的通用修复；没有修改、升级或部署 Windward，没有改玩家公式、手感、死亡/复生节奏或正常 Loading 计时。

## API 与顺序

- `EmberLifetime.RegisterStopOwner(Func<Task>, backgroundIndependent)`：在控制线程启动停止；只有完成不依赖 Unity 的任务才能声明 backgroundIndependent。`WhenOwnersStopped` 在停止启动后才是所有已登记任务的汇总凭证；停止前返回 CompletedTask 不代表会话已停。
- `GameLauncher.ShutdownFrameworkAsync`：撤销 Ready → 停止转场/广播 Shutdown → 启动所有者停止与同步中止 → await 全部任务 → 销毁 Module/Manager。重复正常停止共用同一 Task；错误不报告成功。普通 Quit 使用该路径，不自动保存。
- `ProgressStorageService.BeginStopOwnership`：创建线程同步封闭请求；后台独立推进 IO 结束、请求终态确定、目录释放三个里程碑。超时保留任务和锁；提交后仍为 Committed，未知提交不冒充取消。
- `StopTransitionsAsync` 等待请求全部清理；提交开始后的停止不承诺回滚。`GameSceneSession.WhenReleased` 与 Scope/Session.StopAsync 等待迟到准备和替换清理。Loading/输入在同步停止边界释放本会话所有权。

完整消费示例与边界见 [公开停止契约](../../Packages/com.ember/Documentation~/maintenance/inflight-ownership.md)。正式模板在 Assets 接线后通过 SaveTemplate/Bump/父级同步封存；具体版本与哈希以本版 release JSON 为准。

| 正式模板 | 本版内容版本 | 父模板 | 框架兼容 |
|---|---|---|---|
| base | 0.8.4 | 无 | 0.22.0 |
| platformer-2d | 0.11.3 | base 0.8.4 | 0.22.0 |
| source3d-2p5d | 0.5.1 | base 0.8.4 | 0.22.0 |
| visual-novel | 0.21.3 | base 0.8.4 | 0.22.0 |

platformer 保留 GameInputModule 与 PlayerDataModule 的 Enabled=true；其余继承变化通过正式父级同步完成。四个模板均更新，不能只升级 base 而沿用旧派生快照。

## 原始证据

证据目录：[inflight-ownership](evidence/inflight-ownership)。保留首次失败，不能将用户最初的源码发现改写为运行时已复现。

- 首次受控回归：2 项均失败，见 [baseline-tests.json](evidence/inflight-ownership/baseline-tests.json)。分别暴露无法脱离 owner context 释放目录、OnExit 停止后仍进入目标。
- 修复回归：61/61 EditMode（存储队列、转场、Lifetime）；26/26 场景参与者 PlayMode；28/28 Loading PlayMode。完整逐项结果分别在 storage-transition-retest、scene-scope-retest、loading-retest JSON。
- [正常域重载原始时序](evidence/inflight-ownership/normal-domain.txt) 与 [原配置直接重载时序](evidence/inflight-ownership/disabled-domain.txt)：旧域启动的异步 continuation 未完成；新域退出 Play。这证明 beforeAssemblyReload 不能充当 Unity 异步清理窗口。
- [提前收束实验](evidence/inflight-ownership/early-compilation-drain.txt)：编译开始后 Unity context 的 500ms 异步停止成功完成，先退出 Play，后域重载。
- [正常配置提交前真实保存](evidence/inflight-ownership/normal-storage-precommit.txt)：10:03:59Z 请求停止，10:04:00Z 屏障未放行时目录仍独占，随后 IO 结束、Cancelled、释放并重取目录。索引 SHA-256 前后均为 `07DD5EADE7C1CA66B13C068F96CE30940977D3485ECDF92DC5DFE5933744C085`。
- [当前配置提交后真实保存](evidence/inflight-ownership/disabled-storage-postcommit.txt)：结果发布前停止，最终 Committed，目录释放/重新取得成功。索引 SHA-256 始终为 `15AE5AB7E24C883DB75BEC7BF29A9411EA0BA49B9A44ECA448D04A97CA033A8C`。
- [真实 Unity 加载屏障](evidence/inflight-ownership/native-loading-result.txt)：停在激活前时触发编译，最终 Cancelled、CommitStarted=false、CanInteract=false、仍为 GameMainState；[时序](evidence/inflight-ownership/native-loading-reload.txt) 记录先退出 Play 后重载。

隔离存档使用随机临时目录，未读取或改写产品存档。测试中的有界等待超时仍持锁、IO 异常保留明确结果；场景加载/准备/卸载分段屏障、既有场景保留、提交边界由相关用例断言。后续增量结果另存证据，不用早期通过替代新改动的验证。

## 最终增量与发布检查

- 补充存储/Lifetime 7/7、输入 2/2；正式 platformer 编辑副本 EditMode 6/6、PlayMode 4/4。各批次可能包含重复用例，不累加为独立用例总数。
- 最终重进发现同域复用场景管理器仍保留退休标记，原始证据在 reentry-first-failure.json。修复为初始化新世代、旧续体保持失效，相关 EditMode 4/4，连续两次真实 Play 到 Main、存储 Ready、无自动保存，Console 无错误/警告；见 reentry-editmode-retest.json 与 reentry-smoke-final.json。另保留过快调用 stop/play 导致的编辑器生命周期错误；复测在确认 EditMode 完成后重进。
- 最终 MCP 主动编译完成：Scene 程序集 MVID `4140697e-5150-4731-ae90-a66e42bc8c58`，新世代门控 API 已加载。原配置 DisableDomainReload 保持不变。
- 四模板正式内容校验及父快照一致；见 final-template-validation.json、template-inheritance-static.json。source3d 与 visual-novel 检查继承源、快照及封存完整性，没有分别运行产品 Play 全套测试。
- 技能 bundle 原始字节全部匹配 manifest；工作区及所记 sourceCommit 的文本与 bundle 相同，4 个文件仅 LF/CRLF 不同，因此构建器 `-Check` 原始比较失败。保留 bundle/sourceCommit，详见 bundle-verification.json；没有把该命令标为通过。
- 发布候选暂存区 `check-shared-binaries.py --revision : --checkout` 通过（4 个字体二进制及 autocrlf=true/false 干净检出）；`check-shared-font-config.py --revision :` 通过；`git diff --cached --check` 通过。既有 4 个字体 SDF 资产和 slnx 工作区改动不纳入发布。

## 验证边界

正常编译使用 compilationStarted 的临时重载屏障；10 秒不完成或停止任务失败，报错并保留屏障及所有权。不会禁用域重载、保留热重载业务会话或强制卸载所有场景。恢复后重新进入 Play 正常启动 Main，不自动续玩。

直接 `RequestScriptReload`、强制退出 Play 或进程终止没有可等待的旧域 Unity 清理窗口：同步兜底取消、门控旧结果、对纯后台做有界等待；Unity 原生场景交给退出 Play，未确认的托管清理标记 Faulted。不能把此路径解释成任意第三方/产品异步 finally 已完成。参与者在返回前持有的非 Unity 资源必须接入可收束的停止协议。

本轮没有全量验收、发行构建、真实慢磁盘/断电故障、Windward 产品回调门控或玩家手感复测。原生场景真实加载重载已测；参与者准备/卸载的精确时点用受控端口/参与者屏障验证，不能扩大成所有 Unity 内部线程时点都已覆盖。

Windward 继续保留 0.21.6 / platformer-2d 0.11.2 已签收基线。后续通过正式升级入口安装本版，再三方合并模板差异；捕获操作 Lifetime、传取消令牌、门控 await 后发布/UI/finally，最后单独执行产品增量验收。发布框架不等于完成消费适配。
