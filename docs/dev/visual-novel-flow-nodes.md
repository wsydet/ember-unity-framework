# visual-novel 成对跳转与复合流程交付记录

> 发布衔接（2026-09-30）：本记录的 0.18.0 实现与 0.18.1 文档封存成果随 [框架 0.17.2](release-0.17.2.md) 交付；下文“未发布/不发布”描述各实施阶段当时的范围。消费项目尚未升级。后补选择关联标记由 0.18.2 封存并随框架 0.17.3 交付，见文末追加记录。

日期：2026-09-30。工作目录：正式 Ember 开发仓库；未修改 Call Me Heartless、UnityFarm 或任何消费端 PackageCache。

## 归属与设计

按 `unityfarm-change-routing.md` 判定为 **模板升级**。NovelNode/Runner/Session/Save 与剧情编辑器都属于 visual-novel 的 Assets 业务层，本轮没有新增通用 Manager 或修改框架运行时契约。消费者的月份、步数、金钱与法力规则不进入框架。仓库已有 visual-novel 0.17.4 的有效加载记录，保留该编辑副本和任务开始前已有修改，在 Assets 实施，再调用正式 SaveTemplate 与 minor Bump API。

- 跳转：新增 Jump / Receiver 枚举值，使用章节节点稳定 ID；关联不占用输出连线；同章节同流程作用域内有效。
- 复合流程：新增 FlowStart / FlowCall / FlowReturn；定义与调用分离，当前栈帧记录调用点、流程定义、结果返回映射和独立变量。结果是可读名称，修改名称须更新所有调用点。
- 生命周期：调用初始化 Flow 变量，返回释放；Chapter 与 Global 沿用原数据源；普通跳转不初始化任何变量。禁止递归，深度上限 16。
- 位置与存档：执行器管理所有跳转及返回；存档格式 9 增加调用栈、返回地址及等待剩余时间，按稳定命令 ID 恢复，不重放随机或结算。完整对白、选择、计时等待可保存；自定义步骤/小游戏继续按既有拒绝保存策略处理。
- 兼容：旧枚举数值和无新节点剧情的语义指纹保持不变；格式 8 的随机/BGM 迁移边界仍固定为 8。内容修订导致指纹失配时保留存档并报出位置，不猜测返回路径。
- 编辑器：创建/搜索/Inspector/端口/分区布局、折叠和独立视图、关联定位、来源列表、只读关联线、复制重映射、删除与 Undo/Redo、连续试播和执行路径诊断。
- 示例：FlowExample，18 个节点，两处调用复用清扫流程，完成统一推进步数，取消返回选择而不推进，月末换月，两个月结束；不使用私有资源，不改变默认启动剧情。

使用说明与消费迁移流程见 [NovelFlow.md](../../Assets/Game/Documentation/NovelFlow.md)。

## 验证记录

由连接到正式开发仓库的 Unity MCP 编译与 Unity Test Framework 执行。只报告已完成的测试结果，不把中止或失去回调的批次算作通过。

完整相关回归 **299/299 通过、0 失败、0 跳过**，job `58332abc57954dc38d5b32ff7f708761`，NUnit 执行时长 126.87 秒。范围为 NovelFlowTests、NovelFlowEditorTests、NarrativeRunnerTests、NarrativeStoryTests、NovelCheckpointTests、NovelVariableLogicTests、NovelCustomStepRegistryTests、NovelSessionTests、NarrativeGraphTests、NarrativeGraphInteractionTests、NarrativeStoryEditorTests。NovelSessionTests 包括其多个 partial 文件中的演出、文本、BGM、保存恢复、自定义步骤和试播回归。

| 用户验收项 | 验证落点 |
| --- | --- |
| 多来源、重命名及重载 | ReceiverRenameSaveReloadDeleteUndoKeepsStableAssociation / SharedReceiverAndDuplicateInputKeepPlayerDrivenLoop |
| 多调用点各自返回 | SharedFlowReturnsToEachCallAndAllocatesFreshLocals |
| 嵌套、命名结果、独立局部变量 | NestedNamedReturnAndSavePreserveCallerLocalAndRandomState |
| 内部存档、返回及随机状态 | 上述嵌套测试、ChoiceCheckpointRestoresLocalConditionAndSettlesOnlyOnce、TimerCheckpointResumesRemainingTimeAndReturnsOnce |
| 重复输入、迟到回调 | SharedReceiverAndDuplicateInputKeepPlayerDrivenLoop / LatePresentationCallbackCannotReturnTwice |
| 缺失、递归、即时死循环 | MissingReceiverImmediateCycleAndRecursiveCallReportLocation / JumpCannotEscapeCallStackAndReturnAddressCannotBeTampered |
| 旧资产与存档 | SchemaEightRandomStoryRemainsRestorable，以及现有 LastLight、存档、会话回归 |
| 图编辑器引用与撤销 | WholeFlowCopyRemapsScopeJumpCallAndResultThenUndoRestoresMembership / FlowCollapseAndMovementAreUndoableAndIndependentOfRuntimeDefinition / IsolatedViewAndFoldPreservePortsAndLocateRevealsHiddenNode |
| 示例与试播 | ExampleCancelsWithoutStepThenCompletesTwoMonths / ContinuousPreviewClonesReferencesAndInternalPreviewReturnsWithoutChangingAssets |
| 已返回流程的历史称呼 | FlowSpeakerHistoryRetainsReleasedCallValueAcrossSave |

已通过的阶段回归：296/296（job `e513e03d626a480399355fd1e027bd7b`），其中包括运行器、剧情资产、存档、变量、自定义步骤、会话和图编辑器；随后连续试播及会话回归 189/189（job `b57838d6ee4c4ba9a41cbec6db79d919`）。两批有重叠，不相加为独立测试总数。

最终图编辑器调整（跳转目标显示名、切换章节清除独立视图）后复验 **15/15 通过**，job `66009b21e3a94a598a9bb060855ecc33`。对应本地 NUnit XML：`.utmp/visual-novel-m3/tests-20260930-031901187.xml`（299 项）与 `tests-20260930-032255667.xml`（15 项）。Unity MCP 编译未报告错误；`git diff --check` 通过。

新增测试覆盖稳定接收点重命名/重载/删除撤销、共享流程正确返回、嵌套命名返回与独立局部变量、对白/选择/计时等待存档、随机状态、防重复输入/迟到回调、目标缺失/递归/立即循环/跨域逃逸、旧格式 8、复制重映射、折叠/展开/定位、独立视图、连续试播资产隔离和示例两个月完整执行。

验证边界：没有对消费项目执行验收，没有构建发行包或进行设备端测试。确定的无等待闭环静态校验；依赖条件或调用返回的循环由运行时 StepLimit 保护。普通自定义步骤和外部小游戏没有通用可恢复状态协议；并行动作仍沿用既有归一到目标状态的存档策略。

## 功能实现封存记录（0.18.0）

2026-09-30 11:23（Asia/Shanghai），通过 `EmberProjectSetup.SaveTemplate` 保存 1153 个文件，再调用 `BumpTemplateVersion("visual-novel", 1)` 封存为 **0.18.0 / preview**。没有手工编辑快照、ParentSnapshot 或 hash。

- 内容实算 hash、`contentHash` 与 `versionedContentHash` 一致：`1c1985fb15057e194c5e398a8a331c78`。
- 编辑记录版本 0.18.0、hash 一致，`IsEditingCopyStale` 为 false。
- `ValidateTemplateGraph()` 返回 0 个问题；父级仍为 base 0.7.0，frameworkVersion 兼容声明仍为 0.17.0。
- 框架 package.json 仍为任务开始时的 0.17.1；没有执行框架发版。
- 样例实物为 18 节点、1 流程定义、2 个调用点；默认剧情配置未改。
- 运行时、编辑器、测试和示例目录共 448 个文件与正式保存快照逐字节一致，0 处差异。
- 保留任务前已有的 import-story skill 与 EmberDebugConfig 编辑内容，正式保存同时保留这些内容。场景、动态字体和 slnx 的测试副作用已还原；一次性实现脚本已清理。

## 后续发布与消费升级

本任务不提交、不推送、不打 tag、不发布。模板封存与 `com.ember` 发布分离：发布时按仓库 `ember-release-framework` 流程协调框架版本、发布说明与新 tag；消费者随后通过 UPM Manager 升级包，并按模板 minor 升级/冲突恢复流程部署 Assets。保留并合并消费端已有中文标题和连线折叠，不能假定本地改动已经回流。现有剧情先回归，再逐段抽取流程；结构变更导致旧存档失配时须明确安排重开或单独实现版本迁移。

## 文档收束（0.18.1）

2026-09-30 按 ember-doc-maintenance 的 framework 模式核对源码及本地链接。Unity 为 6000.5.4f1，manifest/lock 与实际解析均为本仓库 Embedded com.ember 0.17.1；功能实现封存记录仍按上节 0.18.0 留存，不能与文档补丁或远端发布混为一谈。

本轮更新 6 份文档，不删除文档或历史验收证据：

- `NovelFlow.md` 作为当前流程能力的使用说明，补节点端口对照、独立视图退出、两种示例试播入口及迁移边界。
- 模板 README 统一当前版本、存档稳定点和连续试播说明，并区分历史批次与本次验证。
- Authoring 补 Flow 作用域、内部节点不能直接新游戏起播的边界，并更新正式保存与消费迁移步骤。
- Design 保留 §1–§18 历史设计，在页首指向已实现的当前契约；子流程调用栈不再列为尚未实施。
- 开发文档索引增加本次交付入口；本记录保留设计归属、测试映射、历史 hash 和后续发布待办。

编辑前副本及完整 Git 状态保存于 `.utmp/ember-doc-maintenance/before/` 和 `status-before.txt`。307 份自有文档的维护前与维护后严格审计均无本地 Markdown 文件目标问题；审计不验证外链或所有标题锚点，本轮新增锚点已按目标标题人工核对。未改 C#、场景或运行资产，不触发 Unity 编译或重复运行测试；299/299 与 15/15 均是上节功能实现的既有证据。

文档通过正式 `SaveTemplate` 保存 1153 文件，再执行 patch Bump 为 **0.18.1 / preview**；保存前工作副本与快照仅有 4 份 Markdown 差异。实算 hash、contentHash 和 versionedContentHash 均为 `4f853face913953d7939443048d355a1`，编辑记录未过期，模板谱系校验 0 问题。上节 hash 为 0.18.0 历史封存值，未回写冒充本版。框架包仍为 0.17.1，未提交、推送、发布或部署消费项目。

## 成对节点选择标记补充（工作副本，未封存）

操作及显示边界统一见 [选择关联标记](../../Assets/Game/Documentation/NovelFlow.md#选择关联标记)。归属仍为模板编辑器改动，不新增运行时契约，不修改存档格式。

新增 `SelectionHighlightsPairedNodesWithoutChangingSelectionOrAssets`，覆盖双向、多来源、多选取消、重建、重新关联及 Undo、空章节和资产不变。本次 Unity MCP 刷新请求成功，但首次读取 Console 返回 `ping not answered`；按 CLAUDE.md 停止编译验证，未执行测试、未正式 SaveTemplate/Bump。当前正式封存仍为 0.18.1，不能把原 299/299 或 15/15 的结果作为本次验证。后续须手动编译并运行图编辑器相关测试，通过后再走正式模板保存流程。

收尾顺序：手动触发 Unity 编译；运行 NovelFlowEditorTests、NarrativeGraphTests、NarrativeGraphInteractionTests 并检查实际颜色与屏幕外定位；通过后使用正式 SaveTemplate → 显式 Bump 保存模板，记录版本/hash/谱系校验结果；发布和消费升级再按已有流程单独执行。

本次文档收束仅更新流程说明、模板首页、开发索引与本记录，不改代码、不重试 Unity 编译、不手改模板快照。保留全部历史验证与封存证据。

## 发布补丁封存（0.18.2 / 框架 0.17.3）

2026-09-30 重试发布时取回此前失败报告：16 项中 1 项失败，发生于新增用例 Undo 后关联检查。同帧测试准备与用户编辑未隔离 Undo 分组；修正测试分组并增加撤销后稳定关联 ID 断言，未修改运行时行为。

修正后本轮 NovelFlowEditorTests、NarrativeGraphTests、NarrativeGraphInteractionTests **16/16 通过、0 失败、0 跳过**，job `2b3d2233134b4ab38a2f6553002ff604`；NUnit 报告 `.utmp/visual-novel-m3/tests-20260930-061050879.xml`，96.42 秒。刷新后 Console 无错误，测试状态查询再次 TimeoutError，因此不追加编译验证；结果依据本轮 Unity Test Framework 落盘报告，不沿用旧版通过数字。请在 Unity 中手动触发编译；若仍报错，反馈首条错误及完整堆栈。实际颜色、屏幕外定位、消费项目及 Player/设备仍未验收。

通过正式 SaveTemplate 保存 1153 文件，再 patch Bump 为 **0.18.2 / preview**。实算 hash、contentHash 与 versionedContentHash 一致为 `b7acc31df3161bcb52a807f3fd929fdd`；父级 base 0.7.0 及父快照不变，兼容声明 0.17.0，谱系 0 问题、编辑记录未过期。无直接修改模板快照或 hash。随 [框架 0.17.3](release-0.17.3.md) 交付；消费项目尚未升级。
