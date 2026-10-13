# 0.18.0 发布推进记录

## 授权与顺序

2026-10-13 用户明确要求：base升版 → 三个派生模板升级 → 升级后的平台R12.7最终回归 → 发布新框架，并确认开始执行。此前“不Bump、不提交、不发布”仅保留为R12.6历史约束，现已被本次授权更新。模板切换仍由用户操作；消费项目升级不在本次范围。

采用 ember-release-framework 技能。目标框架0.18.0：包含公共FSM、转场及模板能力新增，按minor规划。当前main/HEAD为838320b，origin为既有wsydet/ember-unity-framework仓库；只读远端查询确认main及v0.17.5 peeled commit均为该提交，没有v0.18.0。尚未创建提交、tag或推送；后续发布前须再次确认远端。

## 已完成

- 确认Unity实例为本仓库、当前编辑base 0.8.2，正式比较0错误/0警告/0差异，scriptCompilationFailed=false。复用紧邻本次的SceneFlow PlayMode 34/34测试证据，未冒充新增全量验证。
- 通过正式BumpTemplateVersion将base封存为0.8.3。内容hash和封存hash均为`1270ce6dfcae3c01d050c09ecfd60337`，没有改业务内容。此增量是三份文档与QuickSceneLoad测试隔离修订，故使用patch。
- 先运行版本脚本Check，再正式将com.ember从0.17.5改为候选0.18.0。未手改项目manifest或lock；脚本输出的批量推送建议不采用。
- Unity最初仍报告0.17.5，按发布流程请求一次PackageManager.Resolve。后续版本查询确认0.18.0，并通过正式DeclareFrameworkVersion将base兼容声明更新为0.18.0。
- 发布前Git状态、包和四模板metadata备份在本机`.utmp/release-0.18.0/`。R12期间的配置草稿、窗口、21节点布局及场景保护副本继续保留。

## 已恢复的MCP停点

Resolve后的editor/state查询返回`Unity session not ready for 'get_editor_state' (ping not answered)`。同批次随后execute_code成功确认0.18.0并完成base兼容声明；这个成功不能替代失败的编辑器状态/编译验证。此后停止Unity调用，未执行派生同步。需用户确认Unity/MCP恢复并手动触发编译；恢复后先核对当前状态与base封存/兼容声明，不能重复Bump。

## 下一步

| 模板 | 起始版本 | 起始父基线 | 后续工作 |
| --- | --- | --- | --- |
| base | 已封存0.8.3 | 无 | 复核真实内容、编辑记录与兼容声明，不重复升版 |
| source3d-2p5d | 0.4.0 | base0.7.0 | 正式三方预览，核对启动/输入/存储/场景冲突，用户切换后在Assets维护并验证 |
| visual-novel | 0.19.1 | base0.7.0 | 保留小说专属存档与流程语义，不把公共存储接入计划当作已授权实现；处理三方冲突并验证 |
| platformer-2d | 0.2.1（存在已保存未封存内容） | base0.8.2 | 保留玩家方案、规则和布局，吸收base文档/测试修订，最终封存后执行R12.7 |

三个派生模板版本增量按实际结构变化和迁移影响决定，不预先把大范围父基线升级写成patch。只读预览可先做；应用必须用正式事务，冲突逐项核对，不自动接受父级覆盖。切换由用户操作，先保护当前Assets及内存状态。

完成模板与R12.7后：核对全部拟发布源码和资源、剔除无关测试产物与本机环境改动；更新CHANGELOG、正式发布说明、依赖声明及当前指针；按要求验证共享二进制/字体/技能bundle；选择性暂存和审查，再提交、创建annotated v0.18.0并原子推送main与该单一tag。不能把本候选计划当作正式发布声明。历史失败、未做的消费部署/发行Build/跨平台验收继续如实保留。

## 三个派生模板的只读预览（2026-10-13，未应用）

用户确认已恢复且Unity编译无报错；本轮MCP确认正确实例、EditMode/idle、framework0.18.0、编辑base0.8.3、scriptCompilationFailed=false，Console错误/警告0。没有重复Bump base，没有切换模板。该结果是状态复核，不冒充新跑编译或回归测试。

按资源+meta原子单元统计（不是文件数）：

| 模板 | 父级新增/修改 | 专属新增/修改/删除保留 | 冲突与结论 |
| --- | --- | --- | --- |
| source3d-2p5d | 55新增、10修改 | 42新增、5修改、2删除 | GameplayScene一处并发修改；正式语义dry-run成功，变为1项AutoMergeScene，可应用 |
| visual-novel | 55新增、9修改 | 504新增、25修改、5删除 | GameGameplayState和EUILoadingPage两处代码冲突，尚不可直接应用 |
| platformer-2d | 3修改 | 374新增、19修改；另1项已与父相同 | 无冲突，可应用；3项均为公共文档，QuickSceneLoad测试已匹配 |

2.5D场景：base只增加空GameSceneScope及根引用；子模板保有相机、PlayerControlSceneBinding、SceneUI与示例场景物体。官方UnityYamlMerge dry-run成功，合并hash为d6886854aeea8a196c651a52cf0c8c8f；这不是实际加载/运行验证，应用时必须重新预览以核对输入hash。计划升级到0.5.0，因为吸收了新的输入/存储/场景准备结构。

小说两处冲突不能简单AcceptParent：Gameplay必须保留NarrativeModule、NovelPreparedGameRequest、阅读页及暂停/恢复/会话清理；通用SceneScope示例不能覆盖小说参数语义。Loading需要吸收父级真实准备与非缩放进度，同时保留小说恢复遮罩的射线阻挡、取消输入、进度展示及对象销毁保护。现有小说通过RunWithLoadingAsync持有加载任务，通用PlayerData仍禁用；本轮升级不实施小说存储迁移计划。精确合并与相关回归留到用户加载小说维护源后处理；如需改变所有权或运行契约，带回方案确认。版本计划0.20.0，依实际合并复核。

平台三份文档可自动吸收，既有玩家模块启用、场景、输入、配置和UI修改保持；此前尚未封存的E01/E02等新增规则能力也需纳入最终版本，计划0.3.0，最终升级及草稿恢复后执行R12.7。

原始文件级计划与2.5D语义报告在本机`.utmp/release-0.18.0/*-preview.json`。本轮仅预览及文档记录，未ApplyParentSync、未改三个派生快照、未提交或发布。下一步先处理2.5D，模板加载由用户操作；父同步事务更新快照后还须确保实际加载的是新基线，不能在旧Assets直接SaveTemplate覆盖同步成果。

## 2.5D同步已落盘，等待重新加载（2026-10-13）

用户已加载source3d-2p5d 0.4.0。同步前正式比较0错误/0警告/0差异，仅FrameworkScene打开且不dirty，无dirty持久化SO。完整模板及父快照607文件已备份并校验在`.utmp/release-0.18.0/source3d-before-sync/`。

重新运行正式语义预览后通过ApplyParentSync(..., versionBumpField:1)完成事务：source3d-2p5d **0.5.0**，frameworkVersion0.18.0，parentVersion0.8.3，parentContentHash1270ce6dfcae3c01d050c09ecfd60337；contentHash与versionedContentHash均为**89e49f1c8c620812a15b181a0a049180**。未重复Bump base，未应用小说或平台同步。

静态复核：计划中保留的78个已有子模板文件逐字节不变；371个父快照文件与base逐字节一致。GameplayScene只增加GameSceneScope的GameObject/MonoBehaviour/Transform三个序列化块，原有块仅SceneRoots增加根引用，其他场景块未变。此结果不替代新场景实际运行验收。

事务只更新模板快照，当前项目Assets和编辑记录仍是0.4.0，禁止直接SaveTemplate。需要用户在项目中心重新加载同一2.5D模板0.5.0后，才能验证新基线。同步后的补充MCP查询片段误用了不存在的IsEditingTemplateStale，CodeDom编译失败，未取得该次查询结果；这是临时查询片段错误，不是项目脚本编译结果，此后未继续Unity调用。新版模板尚未完成Unity编译/运行验证；用户重新加载后请手动触发编译，若报错提供首条错误及完整堆栈，后续MCP核对正确API和实际状态再续验。

## 2.5D 0.5.0加载与验证完成（2026-10-13）

用户重新加载0.5.0后，MCP确认编译及域重载完成、scriptCompilationFailed=false，Console错误/警告0；项目对快照比较0错误/0警告/0差异，父级状态Synced。未重复Bump或保存覆盖。

- Game.Input.Tests + Game.PlayerData.Storage.Tests EditMode **71/71**，作业8b22e0ba355147949567db66fe94ab5e，3.4060576秒。
- Game.PlayerData.Tests EditMode **16/16**，作业f66db2e0e1b542ff8ea32c1e7e32a0a5，1.8144482秒。
- Game.SceneFlow.Tests PlayMode **34/34**，作业8b0b6cdda6fd45a699bd26f714eb64bb，0.6685571秒。

另从FrameworkScene正常Play启动至Main，调用与开始按钮相同的TransitionTo<GameplayState>路径：实际到达GameGameplayState/GameplayScene，SceneSession Prepared/Running均true，PlayerControl的moduleActive/inputReady/acceptingInput均true，LookAt与GamePlayCinemachineCamera引用存在。正常返回Main后moduleActive和acceptingInput为false，LookAt引用清空；再次进入Gameplay后控制恢复，场景仅1个GameSceneScope。随后退出Play，Console错误/警告0。

这是正常状态路由与接线实跑，未声称实际点击UI、人工拖拽/缩放手感或消费项目验收。原有2.5D专属内容保持；当前任务所需父级升级回归已通过。下一步由用户切换visual-novel，处理此前两处冲突并验证；平台及R12.7随后继续。框架尚未提交或发布。

## 小说切换与冲突准备，等待 MCP 恢复（2026-10-13）

用户已切换 visual-novel；磁盘编辑记录为 0.19.1。首次 MCP 状态显示正在编译，后续查询返回 `Unity session not ready for 'get_editor_state' (ping not answered)`，按验证规则停止 Unity 调用。尚未完成内存草稿预检，没有修改 Assets、SaveTemplate、ApplyParentSync 或 Bump；不能将磁盘身份核对视为运行实例及草稿核对完成。

两处冲突已完成静态处理准备：GameGameplayState 保留小说实现，保留 NarrativeModule、小说请求参数、阅读页提交、暂停恢复和会话清理，不采用 base 示例的 GameSceneSession 所有权。EUILoadingPage 候选合入 base 的真实准备判断、进度上限、复用重置、非缩放计时和零时长处理；用户区与当前小说源文完全相同，并保留三处对象销毁保护。RunWithLoadingAsync 已提供 LoadingWorkCompleted，小说仍由原有加载任务控制完成，未实施小说公共存储迁移。

候选及差异在本机 `.utmp/release-0.18.0/novel-conflict-candidate/`，同时记录四个输入源的 SHA256。此为未应用候选，不是升级完成证据。恢复后先核对源指纹及实际内存草稿并备份，再应用合并、验证小说新建/读档遮罩/取消/失败清理及进度完成边界，普通保存后重新计算正式同步计划并执行版本事务。同步后仍由用户重新加载新版，才验证完整父级升级结果。

本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如有错误，提供首条编译错误及完整堆栈。base 0.8.3 和 2.5D 0.5.0 不重复 Bump，框架尚未提交或发布。

## 小说合并实施与回归修订中（2026-10-13）

用户恢复 MCP 后确认正确实例、visual-novel 0.19.1、scriptCompilationFailed=false，FrameworkScene 不 dirty，无 dirty 持久化 SO，项目比较 0 错误/0 警告/0 差异。2334 个维护源/模板/父快照文件已备份并校验，见本机 `.utmp/release-0.18.0/novel-before-merge/manifest.json`；候选输入指纹未变。

已在 Assets 合并 EUILoadingPage，GameGameplayState 与 NovelSaveUI 原样保留。小说百分比用户钩子另作必要一致性修订：与进度条共用准备成功前的 99% 上限，避免用户区重写出“条 99%、文字 100%”。新增面板断言覆盖两种显示模式、射线阻挡/原值恢复、工作未完成不能就绪及完成后的 100%。编译/域重载完成，Console 无错误；NovelUIFollowupTests、NovelCheckpointTests、NovelSessionTests 定向 EditMode **210/210**（作业 e61a2c537a4249aa8e3cf772062a8b82，8.2790017 秒）。

实际菜单流程首次未进入加载，作业 70ba9208205c4db19db8e620b40c11e7 与仅加强菜单就绪后的 80ce87a2c2814186ba820c7b914bbed2 均 0/2。补充绑定身份断言后，99c9b41d1b2143a2b9bf991431b43e96 明确暴露：测试在 UI Root 下按名称命中了 FrameworkScene/UIPrefabs 的预置 EUIMainPanel，而非 EUIViewEngine 的运行页面。正式 ControlMap 按钮的独立 Play 启动可进入阅读并到达名字输入。测试 Page helper 已改查 ActivePages，场景/预置对象未改。

之后作业 899ca250f01a4c94b1e912f7ebd81fbd 已通过首次启动及慢加载检查，但 0/2：继续游戏在上一 Loading 尚未清理时遭并发拒绝，慢加载退出在上一转场请求尚未完成时请求返回菜单。测试显式返回菜单现改用 IsTransitioning 及 RequestTransitionTo 的 Completion，并断言 Completed；没有改变小说会话或框架并发语义。修订后的两项流程回归仍需取得最终结果，不得视为升级完成。

期间临时诊断片段使用 GetInstanceID 被 Unity 6.5 的弃用诊断拒绝，属于代理查询片段错误，非项目编译错误；另一次 get_test_job 遇插件会话断开。两次均停止 Unity 调用，用户明确恢复后才续验，未绕道验证。运行曾出现 DingTalk-JinBuTi 缺少繁体“語/體”字形的警告，暂未改字体资源，需在发布边界记录。

### 本轮最终停点：单项重复进入测试结果待取回

用户恢复后取回 899ca250f01a4c94b1e912f7ebd81fbd 的失败详情，再执行修订后的作业 ef346897dcd542fead75365656d909c4：**1/2 通过**。通过项 MainMenuContinueLoadsDirectlyAndLoadOpensChooser 覆盖真实按钮新建、慢阅读页加载期间遮罩不退出、继续游戏直接读最新槽、快速读档交换会话、损坏档保留原会话及原文件、取消后遮罩/输入恢复、手动槽再次读档。失败项 RealMenuReaderBranchEndingAndRepeatedExit 已越过慢加载退出，在循环再次启动处因旧等待条件没有检查 Fsm.IsTransitioning 而收到 Loading 并发拒绝；该处已改用 MainMenuReady，未修改运行所有权或并发保护。

最后一次修订后 Unity 编译/域重载完成，Console 错误 0，仅重跑未通过项，作业 **6d68a9b098804581b4b7ce06ec101462**。get_test_job 返回 `Unity plugin session 5398a84e-34a8-45a3-a942-30ccc187ce9d disconnected while awaiting command_result`；按规则停止，**未取得最终结果，不重跑或声称通过**。需要用户确认 Test Runner 完全结束并恢复 MCP 后先取回该作业；若有真实失败，再按具体断言处理。

截止此停点，备份清单内只有 Assets 的 EUILoadingPage.cs、NovelUIFollowupTests.cs、NovelGameplayTests.cs 三个文件变化；Gameplay 状态、NovelSaveUI、场景、配置及小说模板快照未改。未 SaveTemplate、未 ApplyParentSync、未 Bump 小说、未提交/发布。源文件静态 diff 检查通过。父级升级后的完整加载/编译/回归仍未执行；后续正式同步完成后，仍需用户重新加载新版小说。

## 小说 0.20.0 同步已落盘，等待重新加载（2026-10-13）

用户确认 Test Runner 已结束、MCP 恢复且 Unity 编译无报错后，首先取回原作业 **6d68a9b098804581b4b7ce06ec101462**：status=succeeded，completed=1/total=1，失败列表为空，测试 RealMenuReaderBranchEndingAndRepeatedExit **1/1 通过**。恢复后的作业缓存未附 result.summary，因此不补写耗时。没有重复启动测试。结合此前 210/210 定向测试与 ef346897dcd542fead75365656d909c4 中已通过的读档流程，本次冲突合并相关回归已通过；此前失败及修订原因仍保留。

保存前 MCP 核对正确实例、EditMode、scriptCompilationFailed=false、FrameworkScene 不 dirty、dirty 持久化 SO 为空。正式项目比较 0 错误/0 警告，恰好 3 处预期差异（Loading 与两份测试）；验证后的源文件及 metadata 另备份到本机 `.utmp/release-0.18.0/novel-validated-before-save/`。

通过 SaveTemplate 普通保存 1184 文件，小说仍为 0.19.1，实时 contentHash 更新为 b76c6c01c05c4422ad0385a838e0cc0a。保留完整原描述并更新父基线说明及本次变更说明。重新计算正式计划：父级 55 新增/9 修改，小说专属 504 新增/25 修改/5 删除保留，冲突仍仅 Gameplay 与 Loading。两项均显式 KeepChild（Loading 为已合并、验证后的版本），通过 ApplyParentSync(..., versionBumpField:1) 完成同步与封存：

- visual-novel **0.20.0**，frameworkVersion **0.18.0**，parentVersion **0.8.3**。
- parentContentHash：`1270ce6dfcae3c01d050c09ecfd60337`。
- contentHash 与 versionedContentHash 一致：`19a1f7dcfde65092a6a5cb9fa4ee5120`。
- 官方重新计算状态 **Synced**、无错误；base metadata 与同步前逐字节一致，没有重复 Bump base。

文件核对：973 个保留子模板文件与已保存项目源逐字节一致，119 个采纳父级文件与 base 一致，5 个删除路径保持不存在；371 个父快照文件及完整路径集合与 base 一致。记录位于本机 `.utmp/release-0.18.0/novel-sync-file-verification.json`。小说会话、读档遮罩和取消逻辑保留；未执行小说公共存储迁移。

**当前下一步由用户在项目中心重新加载视觉小说 0.20.0。** 正式同步只更新模板快照，项目 Assets/编辑记录仍为已保存的 0.19.1，IsEditingCopyStale=true，不能从旧 Assets 再保存覆盖同步成果。重新加载后验证完整新父基线的编译、项目比较、相关公共回归及小说入口。此前通过结果证明冲突合并版本，不能替代升级后验证。尚未切换平台、执行平台同步/R12.7、提交或发布框架。

## 小说 0.20.0 重新加载及升级后验证完成（2026-10-13）

用户重新加载后，MCP 确认本仓库小说 0.20.0、hash 19a1f7dcfde65092a6a5cb9fa4ee5120、IsEditingCopyStale=false、父级 Synced。加载触发的编译/域重载完成，scriptCompilationFailed=false。正式项目比较 0 错误/0 警告/0 差异，FrameworkScene 不 dirty、无 dirty 持久化 SO。

新父基线上的实际回归：

- Game.Input.Tests、Game.PlayerData.Storage.Tests、Game.PlayerData.Tests，EditMode **87/87**，作业 da3050a5b22e4dd79103c2b491e4690c，5.2453596 秒。
- Game.SceneFlow.Tests，PlayMode **34/34**，作业 52500210ec46467d833fcaea4696ed0d，0.7612544 秒。
- NovelUIFollowupTests 与 MainMenuContinueLoadsDirectlyAndLoadOpensChooser，EditMode 驱动实际 Play 流程 **4/4**，作业 8437b932ee7a4add8d999fb03852324d，23.2162032 秒。覆盖升级后的新建慢加载、继续/快速/手动读档、损坏文件保留原会话、取消清理及面板进度/遮罩状态。

GameInputModule、PlayerDataModule 仍为 Enabled=false，小说专属输入和存档所有权未切换。此前完整分支结局与重复退出 1/1 证据仍对应未改的小说实现，本轮不重复该大流程，也不声称做了全量小说测试、人工全剧情验收或消费部署。

测试结束后再次确认已退出 Play、FrameworkScene 干净、持久化 SO 无脏对象、项目比较仍为 0/0/0。重加载时 Console 仍有此前 DingTalk-JinBuTi 缺少“語/體”的两条字形警告，作为既有字体显示限制保留；没有改字体、再保存或重复 Bump。

**当前下一步：由用户切换 platformer-2d，再核对受保护的配置草稿与 21 节点布局，执行其父级同步及已授权的 R12.7。** 小说升级验收已完成；框架尚未提交或发布。

## 平台 0.3.0 已写入磁盘，等待恢复与重新加载（2026-10-13）

用户切换后，MCP 核对 platformer-2d 0.2.1、正确实例、EditMode、编译失败标志 false，FrameworkScene 干净、无 dirty 持久化 SO、玩家/蓝图窗口均关闭；正式项目比较 0 错误/0 警告/0 差异。只读读取 EditorPrefs 的 v4.exterior 布局，21 个节点保持受保护的新布局（basic-drive 为 1704,-364）；备份当前 SessionState 草稿和布局，旧 R12.6 内存草稿备份原样保留，未将旧草稿自动回写。维护源、模板及父快照共 2508 个文件已复制并校验 SHA256，位于本机 `.utmp/release-0.18.0/platformer-before-sync/`。

正式父级计划无冲突：仅 GameInput.md、PlayerDataStorage.md、SceneFlow README 三项采纳父级修改；374 项专属新增、19 项专属修改保持，QuickSceneLoad 测试已与父级一致。通过 SaveTemplate 普通保存 1083 文件并更新过时的模板描述，contentHash 不变。随后调用 ApplyParentSync(plan,null,1)，MCP 返回 success=false 且无错误详情，按规则停止 Unity 调用，没有重试升版。

只读磁盘核对显示事务已写入 platformer-2d **0.3.0**、frameworkVersion **0.18.0**、parentVersion **0.8.3**，parentContentHash 为 `1270ce6dfcae3c01d050c09ecfd60337`，contentHash 与 versionedContentHash 均为 `b9367677a5a5d17b77c1d74ff75a2342`。371 个父快照文件及完整路径集合与 base 一致，base metadata 逐字节不变。计划内 750 个保留子文件与同步前备份一致，6 个采纳父文件与 base 一致，2 个原已一致文件保持；详见 file-verification.json。未手改模板或 hash，未重复 Bump base。

该磁盘核对不能代替正式同步状态和升级后 Unity 验证。下一步请用户在 Unity 中手动触发编译，确认 MCP 恢复；随后在项目中心重新加载平台模板 **0.3.0**，不能从旧 Assets 普通保存覆盖同步成果。恢复后先核对正式状态、编译及项目比较，再进入 R12.7。本次未完成 Unity 编译验证（指同步后的新版本）；如有错误，请提供首条错误及完整堆栈。框架尚未提交或发布。

## 平台 0.3.0 重加载确认，编辑器回归结果待取回（2026-10-13）

用户继续后，磁盘编辑记录已为 platformer-2d 0.3.0，loadedAt 为 2026-10-13 18:32:26，hash b9367677a5a5d17b77c1d74ff75a2342。MCP 确认正确实例、EditMode 空闲、域重载完成、scriptCompilationFailed=false；正式父级计划 Synced、IsEditingCopyStale=false，项目比较 0 错误/0 警告/0 差异。Console 错误/警告 0，FrameworkScene 干净，无 dirty 持久化 SO。此前同步调用虽未返回成功，现已通过正式接口确认同步与重新加载完成，不能再次 Bump。

启动 EditMode 作业 **a5fce9cc83a649968900ee771b72c824**，请求程序集 Game.Input.Tests、Game.PlayerData.Storage.Tests、Game.PlayerData.Tests、Game.Player.Editor.Tests。首次查询返回 running，实际报告 total=131、completed=39、failures_so_far 为空，当前族 PlayerGroundPhaseEditorTests；不能按请求程序集或历史数量推算完成覆盖。下一次有界查询返回 TimeoutError，按仓库规则停止 Unity 调用，没有重跑或打断原作业。

恢复后先取回该作业最终结果并核对实际测试范围，再安排尚未覆盖的公共回归、玩家运行与 R12.7 实际窗口/流程验收。此次已完成重加载的编译状态确认，但本轮整体验证尚未完成；本次未完成 Unity 编译验证（测试后最终状态未确认）。请在 Unity 中手动触发编译；如仍有报错，提供首条错误及完整堆栈。需先确认 Test Runner 结束并恢复 MCP，避免干扰仍在执行的测试。未新增源码/资源改动，未保存模板、升版、提交或发布。

## R12.7 回归与用户追加白盒网格（2026-10-13）

用户确认 Test Runner 结束、MCP 恢复及编译无错后，先取回原作业 a5fce9cc83a649968900ee771b72c824：EditMode **131/131 通过，0失败/跳过，119.9770231秒**，没有重复启动。

玩家与 SceneFlow 联合运行回归作业 09266f4200954e88b3fae3221080217c 为 **343/444通过、101失败**。第一条失败位于 Test Runner 的 EditModePcHelper.SetEnumeratorPC：UnityTearDown 直接返回 UniTask.ToCoroutine 包装器，没有编译器生成的 $PC/<>1__state 字段。清理中止后产生同名 GameplayScene 残留和后续异常日志污染。仅在 Assets 的 PlayerFoundationTests.Cleanup 增加标准 yield return 外层迭代器，原 CleanupAsync 顺序不改，未修改 Test Framework/PackageCache 或玩家运行逻辑。MCP 编译/域重载完成、Console错误0；受影响族定向作业 ed9dadda7c0344749318868e528f44cc **99/99通过，8.2201254秒**。联合运行中另两项 Animation_InvalidMappingOrGameplayCurveIsRejected(event/alpha) 受到前一族 CleanupAsync 未观察异常日志污染，尚待定向复验；不声称444/444通过。

期间用户追加白盒背景网格要求：保留科技感/白盒感，扩大覆盖，优化图片拼接方式，每10世界单位加强主线并带距离数字。实施为平台专属资源 Assets/GameResource/Player/Whitebox/PlayerLabGrid.shader 与材质，白盒 MeasurementReferences 根对象保留，32个 Sprite 线条替换为1个 Quad MeshRenderer（4顶点/2三角、单材质、无碰撞/投影/探针）。世界坐标固定，1单位细线、10单位主线、原点轴及X/Y坐标均由Shader生成，不使用纹理或文本对象。覆盖中心(0,0)的2048×2048世界单位，并非数学无限网格；细线和小字按像素密度淡出。没有每帧更新脚本。原Sprite可能合批，因此没有声称32次绘制降为1次，也未做GPU耗时/FPS基准。

通过MCP建立材质和场景对象并保存项目白盒场景，ShaderUtil.ShaderHasError=false、Console错误0。场景静态结构差异仅原网格根Transform子引用及被替换的32条线（96个YAML块），新增Quad4个块，其余场景对象逐块一致。旧场景备份位于 .utmp/r127-grid/PlayerFeelLab-before.unity。未恢复旧草稿、未改21节点布局。未普通保存模板或Bump，模板仍为已封存0.3.0，当前Assets包含新网格及测试入口修订，待验证后普通保存。

实际视觉验收尚未完成：manage_camera screenshot 无法按名称找到 HideAndDontSave 的临时 R127GridPreviewCamera，返回 Camera not found。属于本次临时预览查询失败，不是已证实的MCP连接中断或项目编译错误；仍按首次查询失败规则停止Unity调用，未换工具绕过继续。恢复后先清理临时预览相机（通过Resources.FindObjectsOfTypeAll精确匹配，只销毁该HideAndDontSave对象），确认白盒场景状态，完成真实运行/宽窄视图/远处/缩放网格画面检查及R12.7剩余联测。当前白盒以Additive方式打开，FrameworkScene原本干净，勿直接以双场景状态进入正式流程；先保护/关闭本次打开的白盒，再从正常入口启动。

本次未完成 Unity 编译验证（网格实际绘制及最后状态尚未完成验收）。请在 Unity 中手动触发编译；如仍有错误，提供首条错误及完整堆栈。没有重复Bump、提交或发布。完整R12.7与新增网格尚未验收完成。

## 网格用户确认与正式流程验收停点（2026-10-13）

用户反馈“我看了，没问题”，确认新增网格视觉效果。恢复后MCP确认正确项目、EditMode、编译失败标志false；精确清理1个HideAndDontSave的R127GridPreviewCamera，并关闭本轮打开且干净的白盒附加场景。受污染的动画校验整组作业4359e6264cc349e7be0890aa1f5430b6 **10/10通过，0失败/跳过，0.2674604秒**，包含event/alpha；与99/99清理族复验共同解决前轮101项失败，未再跑444项联合全量。

通过PlayerDevelopmentEnvironment.Play(true)启动正式流程隔离验收，主菜单就绪且无转场，存储根为Library/PlayerFormalAcceptance/ee6827547d8f457abcf95c18feedc9bf。异步验收链依次请求正常新游戏、保存0号槽、暂停内读档/恢复并比较位置、退出Main及从0号槽重入；只使用独立验收库。结果通过SessionState键R127.Flow记录；最后取回仍为running，本机.utmp/r127-grid/formal-flow.txt也是running，不能宣称通过。下一轮先取回该键，不重新启动验收链；Play可能仍在运行。验收前白盒设置保存在SessionState布尔键R127.PreviousWhitebox，后续需要恢复。

随后代理临时诊断片段误用不存在的EmberStateMachine.CurrentState，CodeDom返回片段Compilation failed。该错误来自代理诊断代码，不是项目源码；按首次失败停止规则停止Unity调用。没有修改运行FSM接口，没有重新启动流程或重复Bump。恢复后只使用已核实的Is<T>/IsTransitioning等接口取回结果、确认或收尾Play，再完成白盒流程和最终保护检查。

新增网格使用说明已补入平台PlayerWorkflow维护源，包括坐标/单位、有限范围、四位数字与性能边界。尚未普通保存模板：0.3.0的content/versioned hash仍为此前封存值，Assets有测试入口、网格资源/场景和文档修改；不得将它描述为已封存交付。R12.7未完成，框架未提交/发布。本次未完成 Unity 编译验证（最终状态未确认）。请在 Unity 中手动触发编译；如有项目错误，提供首条错误及完整堆栈。

## R12.7 完成与普通保存（2026-10-13）

用户确认退出Play、MCP恢复及编译无错后，首先取回R127.Flow已有结果PASS，没有重新启动正式流程：GameplayScene正常idle运行，保存Committed，暂停内读档/恢复Completed且位置均为(0,0.89)，退出Completed且session=false，从槽0重入Completed。随后补跑白盒相同流程，R127.LabFlow也为PASS，实际PlayerFeelLab运行与新网格画面已采集；重入后LabGameplayCamera的Follow/LookAt均指向当前玩家。详见平台交付记录与.utmp/r127-grid两份流程结果和whitebox-runtime.png。

结束Play并恢复UseWhitebox=false。最终正式图21节点，坐标与保护基线误差小于0.001；PlayerConfig磁盘逐字节不变，旧草稿备份保留。框架/玩家/相机程序集依赖边界核对通过，三个模板Skill执行身份无阻断。玩家开发工具实际打开白盒页再切玩家配置页，最后Console无错误/警告；本轮未改两页布局，此前细分视觉验收按原边界沿用。用户确认保留FrameworkScene主相机远裁剪5000、正交尺寸8、Y=1.875并随普通保存带入。

通过正式SaveTemplate普通保存 **1088文件**，platformer-2d版本保持 **0.3.0**、frameworkVersion **0.18.0**、parentVersion **0.8.3**。contentHash更新为 **7da4c5f29ba27f9dedd54b602c9ba5b0**，versionedContentHash仍为 **b9367677a5a5d17b77c1d74ff75a2342**。保存后正式项目比较 **0错误/0警告/0差异**，父级计划Synced，compileFailed=false、play=false，Console错误/警告0。未重复Bump任何模板，未提交或推送。

**R12.7范围内的最终核对及普通保存已完成。** 新增内容仍未重新封存，不能直接进入框架发布；遵守用户“不重复Bump”的当前约束，停在此发布边界。消费部署、发行Build/产物运行、裁剪、跨平台和GPU/FPS基准未执行；以前各轮未验证事项、小说既有繁体字体警告等继续保留。下一步应先确定新增平台内容的正式版本封存安排，再完成0.18.0发布声明、选择性提交与远端tag流程；不能手改hash冒充封存。

## 发布候选准备与封存确认点（2026-10-13）

用户继续后完成封存前静态准备：新增docs/dev/release-0.18.0.md候选说明，清楚区分已验证结果、模板版本、未封存内容和消费迁移边界。只读远端查询main仍为838320b1e212432f353cf1d599e2a5150272f94d，无v0.18.0及peeled ref。暂存区为空，未创建提交或tag。

共享字体配置检查通过workspace与Git HEAD；共享二进制检查在工作区内临时目录运行，四份字体SHA256及core.autocrlf=false/true干净检出通过。这些是当前/基线静态检查，不替代最终候选index检查或Unity运行。默认沙箱临时目录曾导致git index.lock权限失败，改为本仓库.utmp下独立临时目录后通过，没有提升权限或更改源文件。

公共Skill官方检查首次受Windows PowerShell执行策略与5.1默认编码影响，使用pwsh后准确停在整个.agents/skills目录存在模板安装副本变化的检查。独立核对bundle32个文件SHA256、公共维护源原始字节及sourceCommit f58d4e1f61648fa0193b4066abb31c6b46ea969f的文本内容全部一致；未伪造sourceCommit、未删除模板安装副本或生成新bundle。最终候选仍需在正确隔离环境完成官方检查，不能把本轮替代核对记为官方脚本通过。

正式封存建议为仅platformer-2d 0.3.0→0.3.1（patch），内容准确采用已普通保存的7da4c5f29ba27f9dedd54b602c9ba5b0；base0.8.3、source3d-2p5d0.5.0、visual-novel0.20.0和框架候选0.18.0保持。需要用户确认这次新增内容的版本操作，以免把“继续”误读为解除此前“不重复Bump”的明确限制。确认前不调用Bump/SetTemplateVersion，不生成伪封存发布声明，不提交或发布。

后续选择性提交必须区分当前模板切换造成的Assets及.agents副本变化、本机MCP manifest/lock、字体动态缓存、EditorSettings、解决方案及测试产物；不得git add全部。历史发布说明已有文档维护差异，也须按发布历史不可变要求审查归属。此轮尚未完成候选暂存清单、正式release/manifest声明及最终index检查，不提前宣称发布准备全部完成。

## 最终平台封存与发布复核（2026-10-13）

用户明确授权仅将 platformer-2d 封存为 0.3.1 并发布框架 0.18.0。正式 Bump 调用返回无详情失败，但事务已落盘；没有重试 Bump。用户恢复 MCP 并手动编译后，正式核对四模板版本与 sealed hash 一致，三个派生父级均为 Synced，平台项目比较 0/0/0、编辑副本未过期、Console 错误/警告 0。其他模板未再次升版。

## 发布候选最终检查（2026-10-13）

四模板及新增平台0.3.1已封存。候选index共享二进制SHA、core.autocrlf两种检出及字体配置通过；公共Skill官方脚本在隔离副本验证11项/32文件通过。隔离检出曾受CRLF影响，逐文件证明bundle原始SHA符合manifest、Git分发字节准确、sourceCommit与候选源仅换行差异后对齐检出格式；未改sourceCommit或分发内容。文档strict检查378份、0失效。release声明模板Skill列表按两个实际目录核对，包含小说与平台技能。

空白整理后最终只读状态片段再次无详情失败，停止Unity调用；用户恢复并手动编译后，MCP状态确认编译完成，Console无错误，两个测试夹具Edges的UAC1001警告保留。未重复运行测试、Bump或模板切换。

用户确认清理提交外缓存、本机配置差异和无用产物，但保留当前平台Assets、技能安装副本、草稿与验收备份。历史发布文件差异不进入本版；本机MCP/lock、EditorSettings、解决方案与动态字体缓存恢复原提交内容。候选提交共2201文件，当前模板副本不作为UPM分发源；完整业务实现由正式Templates快照交付。
