# 战斗首版验收矩阵（实施证据）

**最新补验（2026-10-15）：** 上次编译阻塞已解除。攻击运行中故障注入 1/1、相关回滚回归 2/2 通过；正式 GameplayScene 右向和确认转身后的左向六帧刀光均完成真实渲染捕获与逐帧检查。详见[本轮证据](platformer-combat-evidence/2026-10-15-final/notes.md)。下段为此前阻塞历史，已被本次结果补齐；T03 延期及 P9 不整体放行保持不变。

2026-10-15 非 T03 补验续跑：新增 `CombatRuntimeFaultReleasesActiveAttackAndModuleSession` 攻击运行中故障注入测试，但编译刷新后的首次 Console 查询返回 `no_unity_session`，已停止 Unity 验证。新增测试未确认编译/运行，S03 对该细分范围仍未验证；六帧实时画面检查尚未开始。既有通过证据和 T03 延期决定不变。

**最新用户决定（2026-10-15）：T03 剩余项延期到后续制作真实项目时验证。** 合法派生、缺失/无效身份、消费身份及实际跨模板旧窗口/草稿恢复仍未验证；不继续当前项目或独立副本的切换测试，不据此放行 T03/P9。既有通过证据保持原范围。副本仅完成准备，不作为运行验证证据；后续按真实项目实际适用身份逐项核验。

## 当前验收结论（2026-10-15 文档收口）

首版实现及正式体验已有证据；P9 因 T03 剩余身份场景未验证，仍不整体放行。下文带日期的过程记录保留原验证范围；“本轮”指该次历史测试，不代表本次文档维护重跑。

最新内置方案同步的独立证据为 [保存前基线](platformer-combat-evidence/2026-10-15-feel-fixes/builtin-sync-before.json) 和 [保存后状态及 8 项测试](platformer-combat-evidence/2026-10-15-feel-fixes/builtin-sync-after.json)。job `a51c8436b8ce46a285a6a13939324adf`：8/8 通过，0.8737698 秒；加载内置方案验证三刀左右范围、空冲退出 0.25、短受击归位。保存重载、配置校验及普通保存只读保护通过；默认、调校副本、窗口草稿与同步前一致。此结果不替代历史完整回归或正式入口实播。

| T03 子场景 | 状态 | 证据与剩余边界 |
| --- | --- | --- |
| 当前所属模板入口 | 已验证 | 玩家菜单存在可用，Skill 守卫允许 |
| 无关 base 模板入口 | 已验证 | 原生菜单不存在，攻击 Skill 拒绝；未观测完整跨模板草稿迁移过程 |
| Player 关闭→域重载→重新启用 | 已验证 | 菜单禁用、守卫拒绝、候选完整保留、重新可用；原窗口恢复一致，见 [关闭](platformer-combat-evidence/2026-10-15-feel-fixes/t03-disabled-state.json)、[启用](platformer-combat-evidence/2026-10-15-feel-fixes/t03-enabled.json)、[恢复](platformer-combat-evidence/2026-10-15-feel-fixes/t03-restored.json) |
| 合法派生模板 | 未验证 | 需要真实合法派生身份下的入口和操作实测 |
| 缺失或无效身份 | 未验证 | 需要安全测试环境中的拒绝与草稿保留证据 |
| 消费项目部署身份 | 未验证 | 需要实际消费项目环境，不由开发身份结果代替 |
| 实际跨模板旧窗口与草稿恢复 | 未验证 | 需要切换前草稿基线、切换过程及恢复后逐项对比 |

用户要求剩余项延期，保持当前模板；不重复关闭 Player。需实际切模板时由用户操作。本次仅文档维护，无新增 Unity 编译或行为测试。

## 历史验证过程

2026-10-15 T03最新进展：模块关闭时原生菜单存在但禁用、操作守卫拒绝、旧窗口保留；重连后已确认Player实际Enabled=true、菜单可用、Options=1/Enabled=true、Console0errors。候选_attack完整一致，Combo JSON、来源、素材列表及其他业务窗口字段均一致，仅当前分类曾由方案与保存变为动作与机制，已按用户要求恢复。移除仅本次测试候选并恢复after.json原窗口，恢复序列化JSON完全一致，默认配置未变。该模块关闭→域重载→重启用→草稿核对链已有证据；并非所有T03身份场景通过。合法派生、缺失/无效、消费身份及实际跨模板旧草稿恢复仍未验证。详见feel-fixes/t03-enabled.json、t03-restored.json与notes.md。

2026-10-15 base重连实测：实时身份base 0.8.3，IsTemplateActive(platformer-2d)=false，原生MenuItemExists(Ember/Player/玩家开发工具)=false；攻击Skill执行守卫返回“此技能没有模板所有权记录。”，无关模板菜单隐藏及Skill限制通过。Options=1且Enabled=true，Console error查询0项。PlayerEditorAccess/PlayerSchemeWindow/PlayerDevelopmentEnvironment类型未加载，窗口枚举没有玩家窗口，确认当前没有残留可操作的玩家窗口入口。切换过程未持续观测、未取得切换前草稿内容基线，所以不声称旧窗口自动关闭机制或草稿跨切换恢复通过；这些及其他身份/模块关闭场景仍未验证。不自动切回。

2026-10-15 后续：用户已自行保存并切换base，授权验证T03无关模板及旧窗口保护，不自动切回。磁盘身份base 0.8.3；Unity状态查询ping未响应，本轮实时菜单/旧窗口/Skill拒绝检查未取得结果，仍未验证。Assets的Player目录与受管攻击Skill目录当前不存在仅为静态证据，不等同Unity菜单或草稿保护通过。

2026-10-15 用户明确要求：T03先保留未验证，不切换当前项目。该项延期，不视为通过；现阶段以已完成功能、使用说明和剩余验收清单交付，保留默认方案及用户草稿。

2026-10-15 P9验证结果汇总，T03实际菜单/模板切换验收未完成，阶段未整体放行。专项job526961def4e74a93a51bfda62cf2e97f为12/12 Passed；全量PlayMode jobeee9afff723641de8f4594ed4c228df5为600/600 Passed（793.6514071秒），明细见[本轮结果](platformer-combat-evidence/2026-10-15-p9/playmode-600.json)。Editor job88172f1550494ccbb21117549fc0b3b0为114/114 Passed（161.3746389秒），[逐项明细](platformer-combat-evidence/2026-10-15-p9/editmode-114.json)；存储job58e4dc48eebe4ea8b9510ae5aa04f611为47/47 Passed（2.353363秒），两组共161项通过。预期来自执行文档；未取得本轮对应结果前不标记通过。正式入口与白盒实播证据见实施记录及platformer-combat-evidence/2026-10-15-p8-before。

| ID | 操作 | 预期 | 本轮证据/状态 |
| --- | --- | --- | --- |
| C01 | 攻击总开关关闭，按攻击/冲刺攻击 | 无攻击、无资源消费；原移动仍正常 | 对应断言：AttackDisabledKeepsIndependentGraphPlayableAsMovement；对应PlayMode断言通过 |
| C02 | 攻击关闭但生命和陷阱开启 | 仍可掉血、受击、死亡恢复 | 对应断言：DeathModuleClosesOnlyGameplayAndScopeRecoveryKeepsWorldAndSavedHealth（攻击关闭生命夹具）；对应PlayMode断言通过 |
| C03 | 复制方案后改段时长/形状/连接 | 原方案不变；共享 Clip 不被修改 | 对应断言：CopiesOwnAllMutableConfigurationAndRetainStableIdsAndArt / FrozenNestedGeometryAndPayloadRemainIndependent；对应逻辑/Editor/PlayMode断言通过 |
| C04 | 运行冻结 A 后编辑源方案为 B，再恢复 | 当前及恢复候选仍用 A；下一新会话才用 B | 对应断言：RecapturePreservesFrozenLifeAndRejectsMissingSourceGroups / SchemeRecoveryUsesFrozenAssemblyAndPreservesSavedResources；对应逻辑/Editor/PlayMode断言通过 |
| C05 | 添加节点后保存重载/Undo/Redo | Combo/图/Controller/映射一致，原 ID/布局不变 | 对应断言：FourthAttackPreservesLifeDashAndDelayedToggleAcrossUndoRedoReload / SavedLayoutCopiesWithoutOverwritingOrPlacingNewNodes；对应逻辑/Editor/PlayMode断言通过 |
| C06 | 悬空目标/空必需 AbilityId/损坏 Clip Key/非法 Polygon | 定位到字段的显式错误，无半套提交；不混淆合法未装配与损坏引用 | 对应断言：InvalidPolygonAndMissingAbilityHaveLocatedErrors / JointCombatCommitRejectsMissingClipThenSupportsUndoRedoAndReload；对应逻辑/Editor/PlayMode断言通过 |
| C07 | 特殊 Ability 未装配；收刀 Clip 缺省且有 Idle 回退 | 方案可运行，分别普通收刀/Idle 回退，不被泛化依赖校验拒绝 | 对应断言：OptionalRecoveryClipResolvesIdleButAttackClipIsRequired / OptionalAttackAbilityNotInstalledRejectsActionWithoutBreakingMovement；对应逻辑/Editor/PlayMode断言通过 |
| C08 | 三段共用 Ability / 三段独立 Ability；关闭某段 | 两种绑定均合法；所有起手/连招/停顿/冲刺入口尊重目标开关与能力 | 对应断言：SharedAndIndependentAttackAbilitiesSpendOnlyEnabledSegments（4/4通过，ability-4.json）/ OptionalUninstalledAbilityIsValidButDanglingComboIsRejected；DisabledTargetIsRejectedByEveryCombatRouteWithoutSpending（entry/normal/delayed/dash，4/4通过，routes-4.json）；对应逻辑/Editor/PlayMode断言通过 |
| I01 | 攻击与冲刺同时缓存，冲刺先开放 | 执行冲刺，旧攻击请求清除 | 对应断言：CommittingOneActionClearsPeersAndUnavailableChannelClearsPending；对应PlayMode断言通过 |
| I02 | 攻击已可执行，冲刺尚未开放 | 执行攻击，不被未来冲刺阻塞 | 对应断言：ActualFsmContinuesAfterClosedGateOrEntryRejectionAndCommitsOnlyWinner；对应PlayMode断言通过 |
| I03 | 战斗取消上下文中冲刺与跳跃都可执行 | 冲刺优先；最终拒绝则按 continuation 处理，不乱扣次数 | 对应断言：ActualFsmContinuesAfterClosedGateOrEntryRejectionAndCommitsOnlyWinner；对应PlayMode断言通过 |
| I04 | 按住攻击经过数段时间 | 不因长按自动刷新/无限连击 | 对应断言：CombatDashBuffersButMovementDashDoesNotAndHoldNeverRefreshes；对应PlayMode断言通过 |
| I05 | 缓存刚到期/窗口边界前后 | 使用统一边界，无随机多一帧续接 | 对应断言：IntentAndDeadlineDoNotRefreshAcrossNaturalContinuation / ZeroBufferHasOneDecisionAndClearDoesNotResetGameClock；对应逻辑/Editor/PlayMode断言通过 |
| I06 | 普通请求等待至普通窗口关闭 | 丢弃，不转特殊、不延长机会 | 对应断言：NormalRequestCannotCrossIntoDelayedWindowAndExpiredDelayedRequestCannotRevive；对应PlayMode断言通过 |
| I07 | 本体→收刀、冲刺→收尾 | 有效请求保留但寿命不刷新 | 对应断言：DashTailPreservesUnexpiredLatePressWithoutRefreshingIt；对应PlayMode断言通过 |
| A01 | 本体后摇攻击节点开放时按下一刀 | 不等收刀即可进入下一段 | 对应断言：ThreeHitEarlyLinksLockFacingAndLastRecoveryRequiresFreshPress；对应PlayMode断言通过 |
| A02 | 每刀前反向；本刀中反向 | 新刀读最新方向；本刀不翻转检测/朝向 | 对应断言：ExplicitSelfLinkReentersAttackWithFreshClockAndDirection；对应PlayMode断言通过 |
| A03 | 站桩/移动攻击分别持方向 | 站桩开放移动取消后断连；移动攻击允许移动保持链 | 对应断言：StationaryMoveCancelBreaksChainAndGroundAttackCannotContinueInAir；对应PlayMode断言通过 |
| A04 | 最后一刀本体多按；收刀重新按 | 本体缓存不补第一刀；收刀新按可起手 | 对应断言：ThreeHitEarlyLinksLockFacingAndLastRecoveryRequiresFreshPress；对应PlayMode断言通过 |
| A05 | 地面专用走出平台/空中专用落地 | 按默认取消断连；地空通用可继续 | 对应断言：StationaryMoveCancelBreaksChainAndGroundAttackCannotContinueInAir / AirAttackLandingCancelsOnlyAirExclusiveSegment（Air/Both，补测6项组通过）；对应PlayMode断言通过 |
| A06 | 攻击中墙滑/挂边条件成立 | 不绕过动作限制抢状态；退出后既有行为正常 | 对应断言：ExplicitMotionControlBlocksParentJumpHorizontalAndFacingUntilReleased / AttackOwnsWallAndLedgeUntilRecoveryFinishes（墙滑/挂边两例，补测9项组通过）；对应PlayMode断言通过 |
| A07 | 收刀中输入合法跳跃/冲刺/攻击 | 可直接接管，不重新等待本体取消节点；断连或冲刺普通续接按专门规则 | 对应断言：AttackJumpWaitsForGateThenConsumesOnceAndReleasesControl / DashFromSpecialRecoveryCarriesOnlyNormalNext；对应PlayMode断言通过 |
| R01 | 无收刀 Clip | Idle 表现，逻辑接续完整 | 对应断言：OptionalRecoveryClipResolvesIdleButAttackClipIsRequired；对应逻辑/Editor/PlayMode断言通过 |
| R02 | 动画提前播完、移动切换表现 | 时间不重置，窗口不以 Clip 结束为准 | 对应断言：DedicatedRecoveryMovementAndClipCompletionOnlyChangeVisuals；对应PlayMode断言通过 |
| R03 | 特殊 Ability 未装配/禁用/未开启 | 普通收刀及其普通时间，不进特殊 | 对应断言：ClosedDelayedAbilityChoosesOrdinaryAndOpeningDoesNotExtendThisRecovery；对应PlayMode断言通过 |
| R04 | 特殊 Ability 开启且正常 | 从收刀进入就特殊表现；普通窗口后才提示特殊派生 | 对应断言：DelayedRecoveryStartsImmediatelyAndPromptTracksDynamicEligibility；对应PlayMode断言通过 |
| R05 | 普通/特殊分界前后各按一次 | 前者普通、后者特殊；不压缩普通窗口 | 对应断言：EarlyRecoveryPressUsesNormalThirdAndNeverDelayedTarget；对应PlayMode断言通过 |
| R06 | 次数不足/窗口内恢复次数 | 不误显示提示；仍有效请求才可执行 | 对应断言：ExhaustedDelayedAbilityStillSelectsSpecialAndBufferedRequestCanUseRefill；对应PlayMode断言通过 |
| R07 | 普通收刀途中解锁 | 本次不切特殊，不刷新计时 | 对应断言：ClosedDelayedAbilityChoosesOrdinaryAndOpeningDoesNotExtendThisRecovery；对应PlayMode断言通过 |
| R08 | 特殊途中关闭再开启 | 提示随资格变化；原时间不延长 | 对应断言：DelayedRecoveryStartsImmediatelyAndPromptTracksDynamicEligibility；对应PlayMode断言通过 |
| R09 | 特殊收刀被任何合法动作打断 | 本次 CanEAttack 句柄释放，无残留 | 对应断言：DashFromSpecialRecoveryCarriesOnlyNormalNext / DelayedInterruptionReleasesOnlyItsPromptAndStopIsIdempotent（跳跃/受击/死亡/Stop，补测6项组通过）；对应PlayMode断言通过 |
| D01 | 指定冲刺攻击模式 | 清旧连招，进入指定攻击，可接其自身后续 | 对应断言：DashNewPressUsesConfiguredModeAndWaitsForGate；对应PlayMode断言通过 |
| D02 | 第一/二刀后冲刺再攻击 | 只接原普通下一段，停顿资格结束 | 对应断言：DashFromSpecialRecoveryCarriesOnlyNormalNext；对应PlayMode断言通过 |
| D03 | 攻击取消冲刺后不再按攻击 | 不自动触发先前缓存的攻击 | 对应断言：CommittingOneActionClearsPeersAndUnavailableChannelClearsPending；对应PlayMode断言通过 |
| D04 | 冲刺开放点前短按，开放时仍有效/已过期 | 分别执行/不执行，不无限储存 | 对应断言：DashTailPreservesUnexpiredLatePressWithoutRefreshingIt；对应PlayMode断言通过 |
| D05 | 冲刺收尾持续移动/转向 | 保留续接与原剩余时间 | 对应断言：NaturalDashTailAllowsFreshAttackWhileMovementPreservesNext；对应PlayMode断言通过 |
| D06 | 收尾普通落地 | 剩余时间转移，不重置 | 对应断言：NormalLandingKeepsTailRemainingAndJumpBreaksOldNext；对应PlayMode断言通过 |
| D07 | 收尾跳跃/再冲/重落地/翻滚 | 断连；再次冲刺不继承旧 token | 对应断言：RedashFromTailConsumesNewRequestAndDropsOldNext / HardLandingOrRollingInterruptsTail；对应PlayMode断言通过 |
| D08 | 原目标地空不符、有/无环境起手 | 尝试环境起手/保持运动；无非法消费 | 对应断言：DashBodyAndTailUseSameEnvironmentFallback；对应PlayMode断言通过 |
| D09 | 原目标地空合法但 Ability 不可用 | 不借环境回退自动换招 | 对应断言：UnavailableNextDoesNotFallbackAndAirTailReversesSmoothly；对应PlayMode断言通过 |
| D10 | 空冲自然结束，松开/同向/反向 | 正常重力；惯性按配置减速/收敛/制动反转 | 对应断言：AirDashTailRestoresGravityAndInertiaOutlivesOpportunity；对应PlayMode断言通过 |
| D11 | 撞墙及收尾时间结束 | 不推墙；截止只清机会，不突兀清速度 | 对应断言：WallInterruptedDashDoesNotCreateNaturalTail / AirDashTailRestoresGravityAndInertiaOutlivesOpportunity；对应PlayMode断言通过 |
| D12 | 冲刺本体开放节点时原目标地空不符；收尾中新按攻击 | 本体也执行环境回退；收尾新输入合法，不要求只能用冲刺本体缓存 | 对应断言：DashBodyAndTailUseSameEnvironmentFallback / NaturalDashTailAllowsFreshAttackWhileMovementPreservesNext；对应PlayMode断言通过 |
| H01 | 同实体多个 Hurtbox、多形状同时覆盖 | 每窗口只一次默认结算 | 对应断言：NativeHitShapesSweepAcrossCompleteShortWindowAndDeduplicateEntity；对应PlayMode断言通过 |
| H02 | 同窗口换 Polygon 关键帧 | 范围改变，账本不重置 | 对应断言：ShapeKeyframeChangesKeepLedgerWhileNewWindowAndAttackResetIt；对应PlayMode断言通过 |
| H03 | 不同窗口命中同实体 | 各有一次机会，仍尊重目标无敌 | 对应断言：ShapeKeyframeChangesKeepLedgerWhileNewWindowAndAttackResetIt；对应PlayMode断言通过 |
| H04 | 先碰无敌目标后无敌结束，两种策略 | 记尝试不再打；记成功可在有效窗口内重试 | 对应断言：WindowRetryPolicyUsesAcceptedResultAndIgnoresBodyContactExclusion；对应PlayMode断言通过 |
| H05 | 自身/友方/错误层 | 过滤，不污染合法命中记录 | 对应断言：HitFiltersSelfFriendlyAndLayerWithoutRecordingExcludedTarget（自身/友方/层及账本）；对应PlayMode断言通过 |
| H06 | 逻辑步跨完整短窗口/高速平移 | 无整窗遗漏；记录扫掠支持范围与已知限制 | 对应断言：NativeHitShapesSweepAcrossCompleteShortWindowAndDeduplicateEntity；15.3自动物理短窗口/同批换血两组条件已通过（.02/目标30fps、.01/目标120fps）；对应PlayMode断言通过 |
| H07 | 编辑器左右姿态与运行关键帧比较 | 形状、偏移、镜像一致；预览无伤害副作用 | 对应断言：NativeColliderEditRoundTripsWithoutTouchingMainScene / native-hit-preview.png；NativeHitGeometryMirrorsOffsetAndShapeInBothDirections（Box/Circle/Polygon两方向6例通过，补测9项组）；对应逻辑/Editor/PlayMode断言通过 |
| H08 | 开身体穿透或受击闪烁，同时攻击 | 攻击查询不受身体层误改；刀光默认不跟身体 alpha | 对应断言：WindowRetryPolicyUsesAcceptedResultAndIgnoresBodyContactExclusion / AttackVisualUsesFrozenLogicalClockAndIsIndependentOfBodyFlash；对应PlayMode断言通过 |
| F01 | 双方同批命中，其中一方先致死 | 已成立候选仍能换血；同一目标只触发一次死亡 | 对应断言：TwoRealPlayersTradeLethalHitsAndDoNotShareHealthOrReactions；对应PlayMode断言通过 |
| F02 | 同批两次命中，第一次触发无敌 | 第二次看到新无敌并按规则拒绝 | 对应断言：NewProtectionStopsNextHitButNotCurrentReactionAndDoesNotRefreshOnReject；对应PlayMode断言通过 |
| F03 | 霸体但不免伤 | 扣血，原攻击可继续，不被硬直/击退/击飞接管 | 对应断言：ReactionProtectionDoesNotGrantDamageImmunityAndPauseFreezesStun；对应PlayMode断言通过 |
| F04 | 高/同/低优先级再次受击 | 替换/max 硬直/不覆盖；不累计冲量 | 对应断言：ReactionPriorityReplacesVelocityWithoutAddingImpulseAndProtectionKeepsFirstReaction；对应PlayMode断言通过 |
| F05 | 硬直期间尝试动作，结束前短按 | 期间不取消；结束仅执行仍有效请求 | 对应断言：AttackAndReactionCaptureRejectsTemporaryStateAndLateReactionInputCanResume；对应PlayMode断言通过 |
| F06 | 击飞落地但硬直未结束 | 保留硬直，无额外翻滚/普通落地停顿 | 对应断言：HitPreemptsAttackAndLandingKeepsRemainingStun；对应PlayMode断言通过 |
| F07 | 不装 Health、只反应；关动画、保留反应 | 分别可受位移/硬直；无动画仍保持逻辑 | 对应断言：ReactionWithoutHealthIsAcceptedAndReplacementInvalidatesOldTarget；对应PlayMode断言通过 |
| F08 | 一击同时造成伤害/反应并触发带反应保护的无敌 | 本击已接受的反应不被新保护回溯取消；下一击按新保护判断 | 对应断言：NewProtectionStopsNextHitButNotCurrentReactionAndDoesNotRefreshOnReject；对应PlayMode断言通过 |
| L01 | 血条关闭、生命开启 | 正常扣血/死亡，UI 无依赖异常 | 对应断言：HealthHudUsesGeneratedBindingAndFrozenIndependentSwitches / 正式HUD关闭入口实测；对应PlayMode断言通过 |
| L02 | 有致死规则但无处理方 | 配置拒绝；自定义处理方可替换默认 | 对应断言：MissingCustomLifeProvidersRejectWithoutChangingCurrentSession / CustomDeathHandlerRunsOnceOutsideFsmAndUsesSameRecoveryTransaction；对应PlayMode断言通过 |
| L03 | 死亡后连续命中/连续确认 | 死亡事件一次，恢复请求不重复提交 | 对应断言：DeathRecoverEntryRejectsRepeatedRequestAndCommitsThroughFsm；对应PlayMode断言通过 |
| L04 | 普通读档 vs 死亡恢复 | 保存 HP vs 按默认回满；位置策略正确 | 对应断言：LifeSnapshotPreservesSavedHealthWhileDeathCandidateAppliesFrozenRecoveryPolicy / RecoveryPolicyBuildsCandidateWithoutMutatingDeadPlayer；对应PlayMode断言通过 |
| L05 | 恢复候选准备失败 | 不先传送或半恢复；原会话保持安全可诊断 | 对应断言：SnapshotCandidatePreparationAndDiscardLeaveOldHostUntouched / PublicRecoveryWaitsForPreparationAndPresentationAndCancelsCleanly（修正重复请求断言后本轮通过）；对应PlayMode断言通过 |
| L06 | v2 旧档/能力集合改变 | 默认明确拒绝；自定义迁移入口可接入 | 对应断言：ExplicitV2MigrationPreservesResourcesAndStillValidatesCandidateHealth；对应PlayMode断言通过 |
| L07 | 在本期快照无法还原的临时动作/机会中调用底层保存 | 捕获入口拒绝并给原因，不能绕过 UI；不按动画名称一律禁存 | 对应断言：AttackAndReactionCaptureRejectsTemporaryStateAndLateReactionInputCanResume；对应PlayMode断言通过 |
| L08 | 死亡恢复后检查所有临时对象 | 无旧检测/缓存/连招/提示/旧队列命中；世界未隐式重置 | 对应断言：DeathModuleClosesOnlyGameplayAndScopeRecoveryKeepsWorldAndSavedHealth / LifeInterruptClearsDashBodyAndTail；对应PlayMode断言通过 |
| S01 | 两个角色使用同一配置 | 输入、时间、方向、血量、资源、账本与效果互不串扰 | 对应断言：TwoRealPlayersTradeLethalHitsAndDoNotShareHealthOrReactions；对应PlayMode断言通过 |
| S02 | 攻击中暂停再恢复 | 计时/动画冻结，输入清空，不补发 | 对应断言：AttackAnimationSpeedPauseAndRecoveryMovementVariantKeepLogicalClock；对应PlayMode断言通过 |
| S03 | Stop/Dispose/准备失败/图故障 | 清理幂等，无临时对象、订阅、运动和效果泄漏 | 对应断言：FrozenAssemblyReleasedOnCandidateCancelAndStop / CandidateStateFailureRestoresOldHostAndEffectBaseline / CancelDisableAndOldHandlesCannotLeakEffectsIntoNewSession / DelayedInterruptionReleasesOnlyItsPromptAndStopIsIdempotent（含重复Stop、准备与图启动故障、效果归属清理；运行中实例状态故障另由 CombatRuntimeFaultReleasesActiveAttackAndModuleSession 补验通过，见最新证据）；对应PlayMode断言通过 |
| S04 | 沙袋重置，旧命中仍在队列 | 新生命周期不接受旧命中；位置/速度/HP/反应重置 | 对应断言：DummyMovesThroughMotorAndResetInvalidatesQueuedHitsEffectsAndReaction；对应PlayMode断言通过 |
| T01 | 正式与白盒同配置 | 同一驱动与结果，主流程准备链可执行 | P9前一续跑已实测：正式Main新游戏→三连击（沙袋30→23）→保存返回→继续通过；GameplayScene/31节点/HP10，默认未改。白盒同驱动实播见实施记录。 |
| T02 | Skill 从素材选择到增加测试攻击 | 复用状态实现，配置/图/动画/命中完整，实播通过 | 窗口第四攻击/缺Clip拒绝/草稿保护2项通过，正式素材六帧PlayMode通过；完整攻击实播为正式三连击，第四段为隔离配置；FourthAttackFromPublicConfigurationEntryPlaysAndHits通过正式ApplyConfiguration增加第四段、动画装配、真实输入出招/命中/收刀/结束，补测9项组通过。 |
| T03 | 所属/合法派生/无关模板与模块关闭 | 入口隔离、操作校验和旧窗口失效正确，草稿保留 | 部分通过：当前所属、base 无关入口、模块关闭/重启用与测试草稿保留已实测。合法派生、缺失/无效身份、消费身份和实际跨模板旧草稿恢复未验证，见顶部子场景表；整项不放行。 |

## 结果口径与尚未执行边界

- 本轮编译确认：最新测试已加载、对应测试运行完成，最终Console 0 errors；不是用静态检查代替Unity编译。
- 本轮完整回归：PlayMode 600/600；Editor 114/114及Storage 47/47。原阻塞12项包含于600项，不重复加总。
- 后续增量补测23项全部通过：[环境/提示清理6项](platformer-combat-evidence/2026-10-15-p9/coverage-6.json)、[墙面/第四攻击/镜像9项](platformer-combat-evidence/2026-10-15-p9/coverage-9.json)、[能力绑定4项](platformer-combat-evidence/2026-10-15-p9/ability-4.json)、[段关闭路由4项](platformer-combat-evidence/2026-10-15-p9/routes-4.json)。它们在600项之后新增，未称一次运行623项。
- 正式Main→新游戏→三连击→保存返回→继续、白盒右刀光/陷阱死亡恢复的实播，是前一P9/P8续跑取得的独立证据，见实施记录和p8-before证据目录；本轮没有把它们冒充新实播。
- T03 剩余未验收：合法派生、缺失/无效身份、消费部署身份，以及实际跨模板旧窗口失效过程与草稿恢复。base 入口和模块关闭/重启用已有后续实测，不再列为未执行。按用户要求保留剩余项；实际模板切换由用户操作。
- S03另已补齐攻击运行中实例状态回调故障注入及会话清理，见最新补验证据；没有破坏正式图资产，不扩展为任意资产损坏均已验证。
- 最终实时状态：Options=1/Enabled=true，非Play、非编译，MainStage，FrameworkScene dirty=false；默认PlayerDefaultScheme且configDirty=false；攻击Skill blockReason=null。文档静态链接/空白检查通过。
