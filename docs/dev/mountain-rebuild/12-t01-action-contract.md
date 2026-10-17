# T01 项目动作接管补齐（2026-10-18）

后续授权：用户在模板封存交付后明确要求发布新版本，现由 [Ember 0.20.1 发布说明](../release-0.20.1.md)承接版本、提交与远端分发。下文“未提交/未发布”描述封存时状态；Windward 消费仍未执行。

来源：只读 Windward `docs/implementation/T01-action-contract-gap.md` 及同目录两份 S2 证据；核对本目录 01、02，尤其 02 的 5.2、7、12。Windward 基线 Unity 6000.5.4f1、Ember 0.20.0（Git 包指纹 `1a258e762a5636f83c19cd9674e0a60ede7236c1`）、platformer-2d 0.5.0，模板 hash `b0d2b8954453f5eefeb8f094854c24eb`。

归属为**模板升级**：接管职责属于 Game.Player，框架 FSM 已有真实激活、合法转换、代数防陈旧、异常清理及观察能力，无需修改通用 FSM 内核。所有模板代码先在活动 Assets 维护，经项目中心保存和 Bump；不直接编辑已保存模板或父快照。未操作 Windward 编辑器/存档/文件，未写 Mountain，未提交、推送或发布。

## 实现和复用

- `PlayerActionState` + 每候选 `PlayerActionSession` + 每激活 `PlayerActionLease`：真实 FSM 进入才取得职责。公开七组通道、零速度/零重力、部分释放、合法出口和 owner 清理；Motor 仍只有原宿主写入链。
- `PlayerLocomotionState(projectActions: true)`：显式启用根 `project-action` 声明组。生命先执行；保留原移动/战斗声明、类型校验及合法拒绝。未启用项目动作的原方案保持默认行为。
- `PlayerLaunchTarget2D`：先检查拒绝条件，再以 Launch 原因请求动作出口；成功退出并重新核对同一宿主/会话后才登记弹射和发 Accepted。退出 Guard 拒绝仍 Busy，旧回调不清理新动作。
- `IPlayerConfigurationCapture`：在私有副本上递归捕获项目可变 SO，失败整体释放；CreateState 使用冻结值。复用 PlayerSchemeCapture 引用生命周期、正式恢复候选和现有完整独立复制，不新建项目复制系统。
- 动作参与即时快照拒绝。复用检查点基线 + CheckpointProgressCodec/CheckpointSaveSession 保存已提交成果，不保存动作实例/计时或 Animator 进度。
- 独立 `Game.Player.ActionContractConsumer` 只有公开依赖，无友元授权、反射或额外物理更新；测试演示根声明、Definition、配置和状态。生产动作应放消费项目自有程序集，测试夹具不作为发行依赖。

权威 API、调用顺序、优先级、取消语义、配置捕获限制和最小例子见已封存模板 [ProjectActions.md](../../../Packages/com.ember/Templates~/platformer-2d/Assets/Game/Documentation/platformer-2d/ProjectActions.md)。旧 ReplaceVelocity 继续只替换速度；ForcedRise 的正 Y 限制及稳定地面 InteractionLock 语义保留。本次补的是动作契约，不将交接单解释为既有 ReplaceVelocity 实现违约。

## 本轮验证

维护实例固定为 `ember-unity-framework@b23e9b768e85ed30`，Unity 6000.5.4f1。隔离物理测试使用真实 Input System 虚拟键盘、PhysicsScene2D；正式装配测试沿 PlayerModule、SceneScope、恢复流程和独立临时存储。完整方案测试实际复制自定义 SO 依赖、保存重载并核对 Animator 与真实 FSM。

首轮 22 项动作契约测试全部通过，0 失败/跳过，job `08c74b25052e489bb8cd1b7efdaa200d`，10.542407 秒。[完整结果](evidence/t01-initial-contract-tests.json)。后续完整回归、竖直脉冲/逐轴释放补充、正式入口重放及保存封存结果在收尾记录中追加，不把此数字当作最终版本验证。

本轮完整 `Game.Player.PlayModeTests`：587/587，0 失败/跳过，938.7948968 秒，job `024eca8263bc4c12b8e6462b06b6be55`，[原始结果](evidence/t01-full-player-tests.json)。随后补充竖直一次脉冲、逐轴外部合成、基础/战斗完整复制及根类型接线；最终受影响用例集合 **77/77**，其中动作契约 29 项，0 失败/跳过，44.9237039 秒，job `d1d6108a3ade46988fa83e4264899d74`，[逐项结果和输出](evidence/t01-final-playmode-tests.json)。两次集合有重叠，不相加宣称 664 个独立用例；587 全集未在最后增量后重复，最后增量由 77 项专项覆盖。

隔离真实物理证据：原 ReplaceVelocity y 从 `1.144684` 降至 `1.026964`；新动作在 fixedDeltaTime 为 0.01、0.02、0.03333333 时，0.2 秒观察窗口内 y 均保持 `1.144684`，gravityScale=0，真实状态 project-probe。原 ForcedRise 零 Y 仍合法拒绝。正负竖直脉冲由真实重力逐步积分；AddVelocity、ReplaceVelocity、ForcedRise 均验证已释放轴恢复、仍持有轴不被覆盖。

保留修复前尝试：测试文件缺 InputSystem using 的编译错误已修正；误启动而未运行的任务 `71186c1ef7544d51ae39241615e88bbb` 被取消，不计通过。编辑器 TestRunner 真正停止后才清理 MCP 的残留 running 标志，没有改工具源码。新增战斗夹具经历 [Animator 内嵌键复制](evidence/t01-supplement-first-attempt.json)、[冲刺根类型校验](evidence/t01-combat-root-first-attempt.json)、[弹射首步仍有地面探测缓存](evidence/t01-combat-handoff-first-attempt.json) 三次失败：分别改用正式完整复制、保留 PlayerDashCombatLocomotionStateDefinition 继承约束、等待真实飞行落地后测试地面攻击。没有降低断言或放宽生产校验；最后 77 项全部通过。

## 恢复验证与正式保存

后续 EditMode `Game.Player.Editor.Tests / PlayerScheme` job `370a3349d71a4867b27cba623712b0d1` 的状态查询曾返回 `TimeoutError`，按 unity-validation.md 暂停。用户明确授权“一次恢复查询，正常后继续”；恢复查询正常，确认任务 **37/37 通过**，0 失败/跳过，212.7591552 秒。[完整结果](evidence/t01-editmode-tests.json)。本轮恢复后没有修改 C# 或程序集定义。

正式 FrameworkScene → Main → Gameplay → Main 重放已通过，使用 `Library/PlayerFormalAcceptance/30cd1daa8010437ab40a426cc20ec877` 独立存储。[逐项证据](evidence/t01-formal-run.txt)、[临时方案创建脚本](evidence/t01-formal-create.cs.txt)、[重放脚本](evidence/t01-formal-replay.cs.txt)。真实 Input System 起跳后，旧 ReplaceVelocity 在 0.2 秒内 y 从 1.819243 降至 1.701523；新动作 y 保持 1.701523、gravityScale=0。真实 FSM/Animator、暂停、弹射拒绝/接受及通知顺序、旧 owner、即时快照拒绝、正式退出清理均通过。该重放通过公开状态转换进入 Gameplay，不是鼠标点击主菜单测试。

重放初次失败记录保留：[首轮](evidence/t01-formal-first-attempt.txt)、[后台设备诊断](evidence/t01-formal-unfocused-attempt.txt)、[仅 EnableDevice 尝试](evidence/t01-formal-enable-only-attempt.txt)。最终夹具在运行期私有 InputSettings 副本中允许后台输入，将虚拟键盘通过公开 InputUser API 配对到正式 PlayerInput；finally 恢复原设置并移除虚拟键盘，不写输入资产。长测试会话后多次无 Domain Reload 重放出现 [UI RectTransform 观察者残留日志](evidence/t01-formal-warm-editor-errors.json)，执行一次编辑器脚本域重载后完成上述重放；该热重启 UI 问题没有在本次动作契约中修复，不宣称连续热重启 UI 已验收。临时 `Assets/T01ActionAcceptance` 已通过 AssetDatabase 清理，默认方案、FrameworkScene 均未保存改动。

2026-10-18 03:01 经项目中心 `SaveTemplate` 正式保存 **1418 文件**，保留原模板完整描述；随后 `BumpTemplateVersion("platformer-2d", 1)` 封存为 **0.6.0**。contentHash / versionedContentHash 均为 **`3c18689d3f9099062436d8eb107b2c7e`**。Ember 包版本及兼容声明仍 0.20.0，base 父版本 0.8.3 / hash `1270ce6dfcae3c01d050c09ecfd60337`。未手改记录或模板快照；内核无改动，未执行包发布版本 Bump。普通保存、模板 Bump 和正式校验的[原始回执](evidence/t01-official-save-bump.json)已保存。

03:02 项目中心校验：**0 错误、0 警告、0 差异**，资源/.meta/GUID、配置表、UI 生成链路及模板父级状态通过。模板 Skill 经 PreviewCurrentTemplateSkills → SyncCurrentTemplateSkills 正式同步，无本地冲突/备份确认；同步后 HasChanges=false，add-state 执行阻塞原因=null。当前编辑记录为 0.6.0 / 同 hash。未执行 Git 提交、推送、发布或 Windward 消费。

修改的 30 个 Player 源码/asmdef/meta 文件指纹见 [SHA-256 清单](evidence/t01-source-sha256.json)。原活动 Player 与文档备份在 `.utmp/t01-action-contract/`，原有 Git 切换模板修改保留。保存后重新逐项核对源码指纹，确认本轮测试后的源码未变。

## Windward 正式消费步骤与冲突

1. 本轮不执行消费。由原实施对话保护当前 S1/S2 修改、资产 GUID、布局、输入、场景、方案、存档和 `ProjectSettings/EmberTemplateBaseline~`；保留旧 0.5.0 精确基线，不能用当前混合业务目录冒充原稿。
2. 本地保存/Bump 不会让现有远端 Ember 0.20.0 包变成新内容。正式分发须另行授权新的框架版本/tag；通过 Ember/UPM Manager 安装包含本次模板 hash 的包，再单独执行项目中心模板消费。禁止改 PackageCache 或手改 manifest/lock、部署记录。
3. 0.5.0 → 0.6.0 是跨 minor，不能按同 minor patch 增量冒充升级。按模板冲突恢复流程预览，保护本地修改后正式部署并逐批恢复；若选择人工迁移，应保留真实版本/基线状态，不手写“已升级”记录。五个受管目录完整部署会覆盖产品修改，不能直接全量覆盖现有 Windward。
4. 重点合并 PlayerController、PlayerContext、PlayerRuntimeCandidate、PlayerSchemeCapture、PlayerLocomotionState、PlayerLaunchTarget2D、PlayerExternalMotion、PlatformerSnapshot 及新增契约；保留 Windward 自有模块、图、动画、输入和关卡。不要把通用例子的测试动作加入项目默认图，也不要覆盖 Windward 的自有方案以凑版本。
5. FlagWave 继承 PlayerActionState，配置实现捕获接口，项目根声明入口/出口并补齐动画槽。初期持有所需职责，按项目时窗提交悬停/反冲/旗跳，后段释放职责，结束通过声明出口。弹射不能只在 Accepted 中清理接管，改用本契约的接受前退出事务。
6. 保留 Q01—Q08；模板不新增 16 格风表、资格刷新、旗面、贴纸或关卡业务，不改变已确认时窗决策。Windward 模板 Attack 与 J 的冲突仍按 02 的 5.2 由项目解除，不自动启用战斗方案。
7. 消费后重跑 Windward 正式白盒及 A03/A18/A19、弹射组合、死亡/换关、配置冻结与组合保存。S1 键盘子集和 S2 25 项纯数据通过不等于完整挥旗/风场通过；本模板交付也不代表 Windward S2 通过。

## 未包含的验证

Windward 消费、产品 FlagWave/风场/关卡/音效、手柄与发行构建不在本轮运行证据内。旧 Mountain 仅作为先前设计来源，本轮不运行、不修改。既有发行构建和冷启动历史数字不用于替代本轮动作验证。
