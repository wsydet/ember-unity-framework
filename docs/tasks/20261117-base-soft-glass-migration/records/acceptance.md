# 独立验收记录：A-FINAL1—A-FINAL7

**最新状态以文末A-FINAL7为准：R4 base/UI游戏定向独立技术验收完成，原夹具false与补核均保留；用户已在主对话认可背景效果及其余五子。已正式恢复topdown与I-R15真实新起始现场，Unity交还主对话核收束；未擅自归档／清理／发布。**

以下第1—6节是A-FINAL1原始回交历史，等待点已被后续第7—10节逐步解除；不得作当前活动状态。

2026-11-18，Asia/Shanghai。独立对话 `01a1e96f-2bcc-74d2-af51-f37cfd79f4ab`；R1、D1/D2/D5、I-R12，完整模式。**本轮尚未整体通过。** 已完成六子静态/最终预览核对及 topdown 风险定向独立运行；ui-game 加载后的编译/Play 状态不明，正在等待用户检查弹窗。主对话明确要求本轮先回交可完成部分，剩余动态复验和本轮现场恢复接续，不降低 AC、不清理。

历史 [A-BASE1](acceptance-base.md) 的 G0/G1/base 封存前置通过仍有效；[A-CHILD-S1](acceptance-children-static.md) 的旧发现保留，下表记录最终修复的独立检查程度。没有重做整套 base，也没有把实施的测试结果改名为本轮独立运行。

## 1. 独立核对的真实保存基线

| 模板 | 版本 | 实时保存 hash（本轮磁盘重算相同） | 文件 / UI Prefab | 父快照 |
| --- | --- | --- | --- | --- |
| base | 0.11.0 | `9077bd5b5271898e8055d1c7cf72b0a7`（R4，旧封存1c591…） | 889 / 95 | 不适用 |
| ui-game | 0.2.3 | `44b95096df206b5a160a1853333cc498`（R4） | 943 / 99 | 889，等于旧封存base1c591… |
| source3d-2p5d | 0.6.3 | `ba33dd3fe620c9ec3aed3818315bd02f`（R3，见第11节） | 954 / 97 | 同上 |
| lane-defense-2d | 0.1.3 | `7630a27da3efa0620ab038eab47da4b0` | 983 / 99 | 同上 |
| platformer-2d | 0.13.3 | `27eeb3e3f26785b4c0ef92b469598adc` | 2143 / 96 | 同上 |
| visual-novel | 0.22.4 | `13f45e806dd12d33be1779b11864c40e`（R2，见第10节） | 1816 / 105 | 同上 |
| topdown-2d | 0.1.3 | `8789d264af2650178116bbdbe8ea4c5f` | 1251 / 96 | 同上 |

六子 parentId/version/hash 均指向已封存 base 0.11.0。base R4实时9077…与旧封存1c591…不同；六子父快照仍旧1c591…，未同步新背景；六子最终 UI **仅普通 Save，尚未最终封存**，各自封存 hash 仍为首次同步时的中间内容，不能把版本号当最终 UI 封存证明。本轮没有 Save、Bump、ApplyParentSync 或发布。详见本轮 [static.json](evidence/acceptance-final/static.json) 中直接读取的 metadata、独立树 hash 与父快照差异。

## 2. 独立静态与图像检查

- 687 行矩阵的 `actualAsset` 均存在，`actualSha256` 全部匹配，包含新增专用步骤 Variant。它们指向各自存储模板，未使用活动 Assets 冒充其他模板资源。
- 六子原 GUID 分别核 427/438/453/1054/906/595 个，均保持。六子 592 个 UI Prefab 的外部 GUID 在其所属模板、框架/包依赖及项目原 TMP 资源中均可解析；不是运行期 MissingComponent 扫描的替代品。
- 对有变化的 25 份直接 Binding，与各自制作前资源比较原对象引用、类型、IsFramework、PathMode、类路径、页面类型、排序和遮罩/过渡开关等均无漂移。Variant 继承配置未被直接 YAML 比较冒充为 Unity 完整展开。topdown BUI 继承正式 Main 的布局、补继承 Continue/Progress Binding 的事实保留，不宣称该变体整份外观字节未变。
- ui-game 共享 `EUIDynamicOptionRowItem` / `EUIDynamicRowProgressItem` 的 Prefab **和 meta** 与 pre-authoring 载荷精确相同。专用 `EUIUiGameStepItem` 继承原逻辑，只在正式 Steps 使用；旧 DynamicList/BUI 传递依赖静态保留。
- 已查看 review manifest 中全部 100 张图：ui-game 21、source 6、lane 15、platformer 15、novel 25、topdown 18；PNG 头的实际尺寸及 SHA 均匹配。先以本轮可追踪的九图拼图逐张检查，并展开 source/lane/novel 1280 原图核辅助字、操作和滚动区域。拼图仅供审阅，原 PNG 未修改。
- 三张 platformer 历史文件名为 1280、实际为 1080×1920 的图已明确排除；采用 `runtime-1280-recheck` 的真实 1280 图。可选 Health、小说 magenta/cyan 为测试夹具，不当默认外观。
- source 新设置图中气泡不再压住标题或关闭，辅助字已改 Ink 且原图可辨。小说长设置/存档可滚动的部分裁切不据单帧误判为不可达，仍需动态焦点复验。小说皮肤四张原始 A/B 图能看见底图颜色变化和移除后的恢复，不仅检查了颜色属性。

证据：[contracts.json](evidence/acceptance-final/contracts.json)、[static.json](evidence/acceptance-final/static.json)、本轮 `*-review-*.jpg`，原始来源与尺寸/SHA 仍在任务根 [vp2-review-manifest.json](vp2-review-manifest.json)。审图是独立技术检查，**不代替人类 VP2 视觉签收**。

## 3. 本轮新执行的独立 Unity 运行

当前更新后的 topdown 先完成，详 [topdown/result.json](evidence/acceptance-final/topdown/result.json)。2026-11-18 02:36:49.662—02:37:27.450，单探针可量运行约 **37.787 秒，25/25**：

1. Framework 正常进入真实 Main，隔离 PlayerDataModule 存储目录；缺档 Continue 留在主菜单并显示失败原因。
2. 两轮真实 Button Submit 路由：Main Settings/Close、Start、Gameplay Settings/Close、Back；逐次反射核运行监听数 1、FSM 请求增量 1。
3. Settings 在 world timeScale=0 时达到 Opened/可交互，关闭后恢复同一 Controller 与 SessionVersion、解除自有暂停。没有通过强改时间缩放推进设置。
4. 隔离存档下真实 HUD Save，快照为半血；原生 Load 恢复半血；原生 Retry 恢复检查点满血，与存档语义不同。为稳定数值采用 PlayerModule 自有 Pause 句柄，SessionCommitted 后重新取得，finally 解订阅/释放。
5. 两轮返回菜单均无玩家会话及 FreePage HUD；Continue 后新 HUD 持续存在且唯一，随后正常 ExitAsync 返回；唯一 GM。

截图 [continued.png](evidence/acceptance-final/topdown/continued.png) 是本轮真实运行，尺寸为原 GameView QHD；三尺寸外观范围采用前述实施原图，不冒充本轮三尺寸重跑。没有故意延迟异步资源加载来强制命中迟到 HUD 回调竞态；代次守卫静态检查及正常反复退出/重入已做，强制竞态未测。

显式 MCP 编译请求等待超时后，查询到该轮 compile `1794940491730→1794940495584`、domain `1794940501232`；随后 Console 0 warning/error，真实已加载 Player MVID `e190c566-51b8-4697-99a9-fa82959e43e6`，过渡类型 MVID `9a08d215-beb7-41df-8bed-0a0be9fa6baa`。见 [topdown-compile.json](evidence/acceptance-final/topdown-compile.json)。类型为 internal/位于不同程序集导致的两次临时 CodeDom 查询错误未执行产品修改；最终按跨程序集反射取得类型，未拿探针错误当项目编译错误。

清退后 Boundary/前景 Camera/RT/Backdrop 材质均为 0，capture **6190→6190**；[resource-release.json](evidence/acceptance-final/topdown/resource-release.json)。随后退出 Play，FrameworkScene 非脏，活动/共享字体无落盘变化，[font-restoration.json](evidence/acceptance-final/topdown/font-restoration.json) 为空。

运行成本只对有起止记录的片段给精确数字；启动、资源清退、编译/域重载、模板切换、人工等待另记，没有把未知间隔算零。ui-game Play 请求是否生效未知，不能给它填“运行耗时 0”。本轮没有 Test Runner、全量、设备、触摸或 Player 构建。

## 4. 旧发现及剩余风险

| ID | 本轮独立结论 | 待接续 |
| --- | --- | --- |
| A-CS-UI-01 / P2 | 原共享资源污染在最终磁盘上已修复，字节恢复和专用 Variant 隔离成立；旧失败原件保留 | ui-game 新运行 0→1→3、暂停同局、结果重开以及实际两对照页尚未执行 |
| A-CS-SRC-01 / P2 | 修复源码只借用原来可见的实际 SceneUI，Close/Dispose 仅恢复自己隐藏且仍 ViewHidden 的页面；三尺寸新图无气泡压标题/关闭 | 预隐藏保持隐藏、实际所有权/原实例恢复待独立动态 |
| A-CS-SRC-02 / P3 | source 辅助字 Ink 局部修复和三尺寸真实原图可读性复核通过 | 不代用户 VP2 |
| RV-CS-SRC-03 | 实施已保留“另一泡仍可见、单泡物理复用后 surface Lease 空”的真实旧失败；最终 OnShow 调用既有 RefreshSurfaces，静态归属合理 | 必须独立抽查单泡延迟回池/同实例复用及每 surface Lease 非空、Ready、等于宿主；尚未做，不能只凭宿主 Ready 关闭 |
| lane 相机/输入 | 取得/Release 恢复 main 三参数、board Overlay 与顺序的源码路径和原图已核；既有实施使用直接 Battle.Place 的证据边界明确 | 独立真实 InputSystem→GameInput→指针放置/拆除、两处 Restart/菜单再入、相机无增长待做 |
| platformer | 原 GUID/直接 Binding、真实三尺寸图及原始默认 Life/可选 Health 证据已核 | 最小 Save/Continue/设置暂停/返回独立运行待做 |
| novel | 默认隐藏 UIPrefabs 的问题定位/四版本 scene lineage、正式 ScreenSpaceCamera 交付、NovelScrollSelection/NovelGlassSkinSurface 及 OnShow RefreshSurfaces 源码和原图已核；原字体/皮肤方案不是新造服务 | 源节点实际隐藏、长列表焦点、偏好/语言、真实名字/阅读菜单、一次恢复、动态 Choice Lease 和皮肤移除回收的最小独立运行待做 |

本轮没有新增已确认产品缺陷；“尚缺独立运行证据”不作为虚构失败，也不记通过。实施原始 JSON/截图、历史失败与补跑仍为**实施自检来源**。例如 topdown I-R12 的 59/59 与 18/18、source I-R7 的 5/5 和 23/23、novel I-R11 的 46/46 等不累加到本轮 25/25。

## 5. 逐 AC 当前状态

| AC | 状态 | 理由 |
| --- | --- | --- |
| AC01 | 独立静态通过 | 687 行真实条目及资产 SHA 全匹配，处理结论可追溯 |
| AC02 | 沿用已独立通过 | A-BASE1 与用户 VP1，最终封存 base hash 未变 |
| AC03 | 部分，待动态/VP2 | 六子实际保存/资源/原图齐，剩余独立风险动态未完 |
| AC04 | 部分，待动态 | topdown 本轮 25 条通过；其余模板仅独立静态、原始实施证据，不能整体行为签收 |
| AC05 | 部分，待动态 | base G1 旧 A2 复现沿用 A-BASE1；topdown 资源清退通过；source 池化/其他子层级清退独立复验未完 |
| AC06 | 独立图像核对通过，动态部分待 | 100 张真实尺寸/SHA/页级图已核；source/novel 长列表所有权/焦点尚待，设备未测 |
| AC07 | 独立静态通过；动态保留边界 | GUID、592 Prefab 引用闭包、25 直接 Binding 契约通过；其余运行期展开沿实施资源扫描，本轮未全重扫 |
| AC08 | 部分，待本轮恢复 | 原 1420 载荷有效、I5 起始唯一原缓存差异及新现场已保护；本轮已 Load ui-game，尚未正式回 topdown |
| AC09 | topdown 通过；ui-game 待确认 | 取得 topdown 本轮编译/域重载/类型/Console；ui-game 的 recovered 不作完成证明 |
| AC10 | VP1 已认可；VP2 待用户 | 主对话已问人类，当前尚未取得反馈 |
| AC11 | 未完成 | 必要独立风险复验、用户反馈与本轮恢复尚欠，不能整体签收 |
| AC12 | 本轮通过 | 未 Save/Bump/父同步/提交/push/tag/发布/消费升级/清理；六子未最终封存如实列明 |

## 6. 精确等待点与恢复契约

中心 `LoadTemplate("ui-game")` **一次成功返回 943**，写 [load-ui-game.json](evidence/acceptance-final/load-ui-game.json)。之后 `refresh_unity compile=request` 一次返回 recovered；后续 state/Console 为 ping not answered，类型查询空失败，Play 请求 TimeoutError。**Load 已明确成功，后续编译/Play 的实际结果不明。** 已异步请用户检查可能的模板 Skill、场景重载/确认弹窗，主对话已补收并转问，尚未收到人类回答。期间没有再次调用 Unity，不重放 Load/compile/Play。

现在磁盘正式身份是 ui-game；编辑器可能仍在导入、弹窗或已执行 Play，未取得新状态不得推定。收到真实反馈后先查原操作结果/实际身份/Play/编译事件，再决定续接；确认无 modal 且真实断连时按公共 30 秒、最多 10 次流程，不能把本轮暂停回交伪写为已重试十次。

起始更新后 topdown 完整保护路径：[backups/acceptance-final-managed-start/manifest.json](backups/acceptance-final-managed-start/manifest.json)，1310 文件均有字节载荷和逐 SHA 校验；含五活动目录、Editor 身份/场景、共享/活动字体、原 index、存在的 layout 文件及根 meta。`acceptance-final-start` 是本轮误纳非受管美术后停止的广域部分副本，**不是完整基线，不用于恢复**。所有备份保留。

恢复只能正式中心 Load 更新后 topdown 0.1.3/8789d...，再按本轮精确差异恢复唯一原 DefaultLight 缓存及必要原现场；不得旧 G0 整树覆盖，不复制旧身份、metadata、Skill 状态。原始视图见 [editor-start.json](evidence/acceptance-final/editor-start.json)：10 项列表/index 5 QHD、位置 `(0,803.33,933,542.67)`、Selection 空、FrameworkScene clean。当前未知状态下不强发 Stop/Load，不覆盖可能仍被编辑器持有的字体。

最终磁盘漂移/保护与当前活动镜像记录在 [partial-protection.json](evidence/acceptance-final/partial-protection.json)：16,582 个 Packages/com.ember、ProjectSettings、UserSettings 文件零漂移；当前 ui-game 活动镜像与 943 文件存储零差异，28 个阶段字体零漂移，原 index `07b2454a...` 保持。1310 份本轮起始备份逐 SHA 有效，其中覆盖 I5 三方表 1253 行，与 I-R12 记载的起始 activeSha256 全部一致；其余 167 行不冒称已由本轮独立备份覆盖。原 1420 载荷均已独立校验有效。操作已知/未知状态另见 [operations.json](evidence/acceptance-final/operations.json)。

本轮正常结束交主对话收取；剩余独立运行、VP2 与现场恢复均继续保留，**不是任务完成或验收通过**。

## 7. A-FINAL2 续接与新回交

2026-11-18，03:44 起，原独立对话续接。直接输入由主对话转达：用户已正常处理 ui-game 弹窗；用户对六子 VP2 选择“需要调整”，随后明确“视觉小说需要保持原来的 UI，只改 GM”。本轮未开始小说操作，按主对话要求跳过当前小说成品加载/动态；以前的小说新视觉检查仅为历史，不能证明新范围完成。原实施须等待独立交回和恢复现场后再制作，验收未自代返工。

### 7.1 ui-game 原等待已解除，15/15 独立通过

首次续接 state 仍未响应；按 30 秒间隔第 1 次恢复即成功，同一实例 `ember-unity-framework@b23e9b768e85ed30`。恢复状态记录了 compile `1794944565530→1794944583533`、domain `1794944597390`；实际非编译/更新、非 Play/无待切换、FrameworkScene 非脏。正式身份仍 ui-game 0.2.3/a864177...，`Game.UI.EUIUiGameStepItem` 已加载，程序集 MVID `10eef44f-194e-4e82-b7ae-8d4a441d5358`，Console 0 warning/error。原成功 Load 与原编译请求均未重发；明确 Play 未处于执行/待执行状态后，才正常发起新 Play。见 [recovery-r2.json](evidence/acceptance-final/recovery-r2.json)、[ui-game-compile-r2.json](evidence/acceptance-final/ui-game-compile-r2.json)。

新独立探针在 **03:45:40.424—03:45:50.926，约 10.502 秒，15/15**，详 [ui-game/result.json](evidence/acceptance-final/ui-game/result.json)：

- 实际 Steps 的三行均为专用 Variant，0/1/2/3 步选中及 CurrentProgress 与已完成步数一致；包括暂停/恢复引起的重复 Changed 通知，重复检查次数如实保留，不称 15 个互不重叠场景。
- 第一步完成后 Settings 暂停同一 session，完成步数保持 1 且 CanAdvance 禁止；关闭后仍同一 session。
- 三行内容高 212 / viewport 220，无第三行裁切；真实三步进入 Result，Restart 得到新 session 且从 0 开始。
- 实际打开 `EUIDynamicListPage` 与 `EUIBuiCandidateDynamicListPage`：模板都不是新 Step Variant，模板及活动内容没有 BackdropGraphic，旧风格保留。

已查看本轮 [step1.png](evidence/acceptance-final/ui-game/step1.png)，1920×1080。三尺寸图像仍沿 A-FINAL1 逐图核对，不声称本轮三尺寸重复运行。结束 Console 0 warning/error；关闭全部消费者后 Boundary/Camera/RT/Backdrop 材质 0、capture **2803→2803**，确认退出 Play、字体 0 漂移。[resource-release.json](evidence/acceptance-final/ui-game/resource-release.json)、[font-restoration.json](evidence/acceptance-final/ui-game/font-restoration.json)。

**A-CS-UI-01 本轮独立动态闭环，关闭。** 原共享 Prefab/meta 的字节恢复、传递依赖隔离、真实玩法步骤与两实际对照页均已核；不将其等同于用户 VP2 接受外观。

### 7.2 当前 source 新等待点

确认 ui-game 已停止且场景非脏后，只正式 Load source3d-2p5d 一次，成功 **954 文件，03:47:15.525**。随后保护 28 个 source 阶段字体文件，发显式 compile=request 一次，工具返回 recovered；之后 state 仍 ping not answered，间隔 30 秒复查一次相同。为排除新的模板/场景确认弹窗，已再次异步请用户检查，**尚未收到本次 source 的人类反馈**。

从发出问题后没有 Unity 调用，没有重发 Load/compile，没有发 source Play；source 探针没有执行，不能宣称其编译、行为或清退通过。新等待与已处理的 ui-game 弹窗不同，不拿旧反馈替代。未声称十次断连重试耗尽，也不对未知编译追加手动重触发。详 [load-source-r2.json](evidence/acceptance-final/load-source-r2.json)、[source-operations-r2.json](evidence/acceptance-final/source-operations-r2.json)。

`source.cs.txt`、`lane.cs.txt`、`platformer.cs.txt` 已在任务 evidence 目录准备，均**尚未执行**。lane 计划采用短寿命 InputSystem Mouse 事件进入既有 GameInput/BoardView 路径，禁止用直接 Battle.Place 代替；合成设备、原 mouse current/timeScale 须 finally 恢复。source 需先补唯一 A-FINAL 临时 1280 GameView 项并核列表，才可使用准备脚本中的 index10。当前没有新增 GameView 项，原十项列表不能盲用 index10。任何准备脚本仍须按真实前置审查后运行，不能把文件存在当执行证据。

### 7.3 当前保护、剩余 AC 与恢复责任

[protection-r2.json](evidence/acceptance-final/protection-r2.json) 独立磁盘核对：

- 正式活动 source3d-2p5d 0.6.3/a25a385...，954 活动文件与存储镜像零差异。
- 16,582 框架/模板/ProjectSettings/UserSettings 文件零漂移；28 个 source 阶段字体零漂移。
- 原 index `07b2454ab28af72dce996b96f1ad27eba074c651c05f53f8f22a6d3a151655f0` 保持；起始 topdown 1310 文件备份再次逐 SHA 有效。
- 没有 source 临时运行会话、探针事件或合成输入设备需要清退；ui-game 已明确安全清退并停止。source 编辑器实际状态尚未取得，不强行 Stop/Load，不覆盖可能仍被持有的资产。

累计新独立业务探针可量片段为 topdown 37.787 + ui-game 10.502 ≈ **48.289 秒下界**，不含启动/清退/编译/导入/人工等待，也不把旧 ui-game 请求未知期间写成零耗时。未扩大为全量、设备或 Player。

当前 AC04/07 的 ui-game 风险运行完成、AC09 的 ui-game 编译门禁补齐；A-CS-UI-01 关闭。AC03/04/05 的 source/lane/platformer 风险独立运行仍待；AC08 本轮正式恢复仍待；AC10 是“用户要求调整，小说已明确只改 GM”，不是无反馈或视觉通过。新小说范围的矩阵/实现/重新验收待主对话安排，原 AC01/03/06/07 中对应小说的已核内容只能作为旧基线历史。AC11 整体未完成，不降低标准。

用户反馈新的 source 弹窗后，先查原 Load/compile 的实际身份/事件/Console/状态，再正常继续剩余风险动态。完成后仍须**正式中心 Load 更新后的 topdown**，按 `backups/acceptance-final-managed-start/manifest.json` 恢复唯一原 DefaultLight 缓存及原视图，不能旧整树覆盖或复制旧身份。当前视图选择在 ui-game 验证改为 index3；原列表十项未增删，最终应恢复 index5/QHD 和 editor-start.json 的原窗口现场。这个恢复尚未完成。

本轮按主对话“真实 modal 等待时完成可做部分后回交精确恢复点”的要求正常回交；不在等待中无限占用。没有改产品/测试/存储模板、Save/Bump/父同步、提交/发布/清理；整体未签收。

## 8. A-FINAL3：source 风险复验闭环，lane 新确认等待

2026-11-18，11:21 起续接。用户经主对话直接确认本次 source 弹窗已正常处理、Unity 可操作。本轮先查原操作，未重发 954 Load 或原编译。实际正式身份 source3d-2p5d 0.6.3/a25a385...，非 Play/编译/更新、FrameworkScene 非脏；取得原请求之后 compile `1794971220004→1794971229984`、domain `1794971238425`，实际 source UI MVID `3262b0f1-c93b-4317-bba4-22deeca191bb`，设置所有权字段存在、Console 0 warning/error。见 [source-compile-r3.json](evidence/acceptance-final/source-compile-r3.json)。之后才首次正常进入 source Play。

### 8.1 source 本轮独立运行

主探针 **11:22:15.660—11:22:30.327，14.667 秒**，原始 [source/result.json](evidence/acceptance-final/source/result.json) 保留 **13 条通过、1 条工具阈值失败**：

- 两轮仅隐藏/延迟回池目标 Cube 气泡，另一 Dynamic Cube 气泡持续存在，宿主 Lease 保持同一实例；重新显示物理复用同一 Item `-201214`，keeper 为 `-201182`。每个实际表面 Lease 非空、Ready、等于宿主。没有只用宿主 Ready 代替表面检查。
- 三个实际尺寸 1920×1080、1280×720、1080×1920 中，Settings 只隐藏原可见的 SceneUI，保留目标注册；关闭恢复相同气泡实例、原 alpha，各活动表面重新得到 Ready 宿主 Lease。原已 ViewHidden 的 SceneUI 不被误显示。返回菜单后 SceneUI 和注册均清退。
- 三张本轮设置 PNG 已逐张查看，标题/Close 无气泡遮挡、辅助字可辨。PNG 实际尺寸和运行记录一致，独立核对见 [dimensions.json](evidence/acceptance-final/source/dimensions.json)。此前试图给任务脚本增加尺寸断言的 shell 转义错误未执行修改；实际尺寸由 PNG 头和运行字段交叉核对，不伪称那条脚本写入成功。

原失败 `moved-target-followed` 的门槛为 RectTransform **世界坐标平方位移 >1**；实际记录 `(0.96,1000.82,91.53)→(1.32,1001.02,91.53)` 已发生位移，但不满足错误单位阈值。保留原 false，不覆盖结果。另用屏幕坐标做最小独立补核 **11:23:07.926—11:23:16.562，8.636 秒**：[movement-screen-recheck.json](evidence/acceptance-final/source/movement-screen-recheck.json) 通过，目标移动后气泡从 `(540,1116.75)` 到 `(607.88,1155.94)`，位移 **78.38005 px**；目标移回后气泡回 `(540,1116.75)`，误差 **0 px**。这确认原失败为验证阈值问题，无需改产品。

最后 Console 0 warning/error；全部消费者关闭后 Boundary/RT/前景 Camera/Backdrop 材质为 0，capture **4401→4401**。确认退出 Play；source 阶段字体原字节无变化。[resource-release.json](evidence/acceptance-final/source/resource-release.json)、[font-restoration.json](evidence/acceptance-final/source/font-restoration.json)。临时移动和隐藏均在 finally/正常退出中恢复，没有留下探针事件或运行会话。

**A-CS-SRC-01、A-CS-SRC-02、RV-CS-SRC-03 完成独立闭环并关闭。** 用户 VP2 仍按 R2 调整流程单独处理，不把技术通过当视觉认可。topdown/ui-game 已通过范围未重跑。

### 8.2 lane 当前精确等待点

source 安全退出、场景非脏/字体核对后，中心正式 Load lane-defense-2d **一次成功返回 983**，正式身份为 0.1.3/7630a27...，加载于 11:24 左右，详 [load-lane-r3.json](evidence/acceptance-final/load-lane-r3.json)。保护 28 阶段字体后，显式 compile=request 一次返回 recovered；随后 state 为 ping not answered，间隔 30 秒复查一次相同。

已直接发出**本次 lane** 的模板 Skill/场景/确认弹窗检查问题，并给出处理/无弹窗/取消选项。尚未收到本次反馈；先前 source 或 ui-game 的回复不能替代。问题发出后无 Unity 调用，**没有 lane Play 请求、没有 lane 探针执行，没有重复 Load/编译**。当前不能确认本轮 lane 编译/域重载/Console，不把 recovered 当完成，也未声称十次断连重试耗尽。见 [lane-operations-r3.json](evidence/acceptance-final/lane-operations-r3.json)。

因未知编辑器阻塞无法安全发后续请求，本轮按原 modal 回交规则收尾。尚不能越过该等待点去加载 platformer 或 topdown；不强行 Stop 或改底层句柄/权限。lane 的真实 InputSystem→GameInput→指针放置/拆除、相机和两处 Restart/菜单再入，platformer 最小存读档/设置均仍待独立执行；准备的 txt 不算执行证据。小说仍按 R2 跳过旧成品，原实施仅可继续离线准备。

### 8.3 保护与待恢复现场

[protection-r3.json](evidence/acceptance-final/protection-r3.json) 核对 16,582 文件零漂移、lane 983 活动镜像零差异、28 阶段字体零漂移、原 index `07b2454a...` 保持，起始更新后 topdown 1310 载荷逐 SHA 有效。

为补 source 三尺寸，本轮用 Unity 原生 API **只新增一个**任务自有 `A-FINAL 1280 (1280x720)` GameView 项，index10，新增前完整十项列表与 editor-start.json 一致。记录 [gameview-added-r3.json](evidence/acceptance-final/gameview-added-r3.json)。当前选择 index9，任务项未移除。最终恢复须核当前列表仅此一项为本任务自有，原生移除该项并恢复原十项/index5QHD/原窗口现场；未知状态下不改 preferences 文件假装已恢复。

最终仍须正式中心 Load 更新后 topdown0.1.3/8789d...，再按 A-FINAL1 起始保护精确恢复唯一原 DefaultLight 缓存 `14c9718...` 及必要视图，不复制旧身份或旧整树。**当前仍活动 lane，尚未恢复 topdown，Unity 未明确交还实施。**

本轮 source 可量业务探针新增 23.303 秒；连同 topdown/ui-game 可量下界约 **71.592 秒**，编译、切换、启动、资源清退、人工/模态等待与未知间隔不算零、不冒充完整总耗时。未全量、设备或 Player。当前独立旧发现已全部关闭；lane/platformer 风险动态、AC08 本轮恢复、R2 小说返工/新视觉评估及 AC11 整体仍未完成。无新增确定产品缺陷，无产品/测试/模板快照修改，无 Save/Bump/同步/提交/push/tag/发布/消费升级/清理。

## 9. A-FINAL4：剩余非小说风险验证完成，现场已恢复并交还

2026-11-18，11:35 起，用户经主对话确认 lane 本次弹窗已处理，要求先完成现有验收再恢复小说原 UI/仅改 GM。旧 topdown/ui-game/source/静态集合未重跑，小说旧成品未加载。本节取代前文各阶段等待状态。

### 9.1 lane 最终独立结果及原始失败

先核原983 Load后编译事件 `1794972552545→1794972564580`、domain `1794972570898`，正式 lane0.1.3/7630a27...，实际 Board MVID `a1d1a09f-e02b-46d3-80a4-c52a79a73be7`、UI MVID `4f78fb52-a168-4cfc-8dfe-fa5b66cf9a32`、非Play/编译/更新、场景干净。最初 Console 4 条仅为 MCP WebSocket 断连警告，不能写成空 Console。[lane-compile-r4.json](evidence/acceptance-final/lane-compile-r4.json)。

最终采用既有已登记 Mouse 的 InputSystem 状态事件，**不改设备过滤/绑定，不直接调用 Battle.Place/RemoveUnit**。有效运行 **11:45:58.975—11:46:25.032，26.058秒，14/14**：[lane-native-reset/result.json](evidence/acceptance-final/lane-native-reset/result.json)。

- 栈中唯一 `LaneDefenseCamera(Overlay)→UICamera→Foreground Segment0/1`；棋盘目标点无 UI 命中。
- 实际 GameInput Point 与目标像素一致，BoardView.ReadInput 消费鼠标新按下；放置后 UnitCount1、资源650→600、反馈Placed；拆除后 UnitCount0、反馈Unit dismantled，不是0→0空断言。
- 暂停时间停止、同战局恢复；Pause Restart 和自然终局 Result Restart 均产生新代次且唯一 board Overlay，不增长。
- 两轮菜单退出及再次进入，恢复 main 的 cullingMask/clearFlags/backgroundColor，清掉旧棋盘相机/HUD，重入只一 Overlay。
- finally 恢复原鼠标位置/按钮/current 和 timeScale；原位置与恢复位置相同，未创建新设备。

该结果之前的失败均保留，**不累加为通过**：首个合成 Mouse 被运行资产原设备过滤排除，Point/放置失败；其0→0拆除检查不采信，Pause再提交也早于真实按钮门禁恢复。第二个夹具临时扩运行设备列表后，Input System 绑定解析出现 NullReferenceException，清理异常导致没有 result.json。已显式移除唯一任务设备、恢复原 Mouse 和 timeScale，随后 Stop；连续 Play 又遇输入服务未Ready/旧PlayerInput引用，原错误归档到 [lane-fixture-console-r4.json](evidence/acceptance-final/lane-fixture-console-r4.json)。

项目原来启用 DisableDomainReload（EditorSettings 两字段均1，本轮未改）。归档错误并停止后，通过 MCP 一次显式编译/域重载恢复：compile `1794973434281→1794973439072`、domain `1794973446543`；30秒第1次有界恢复成功。之后正常启动才运行上述14/14。Console清空仅用于区分新旧日志，错误已完整保存；最终成功运行无warning/error，关闭资源 Boundary/RT/前景Camera/Backdrop材质0，capture **3639→3639**；确认停止后字体无漂移。

**ENV-AF4-01** 保留为夹具/环境待归因记录，不虚构为已确认迁移缺陷，也不宣称修复了普通连续 Play。异常仅在失败夹具链后观察到；没有从干净域、无设备过滤编辑的条件下另证普通 Play/Stop 的回归。详 [lane-fixture-analysis-r4.json](evidence/acceptance-final/lane-fixture-analysis-r4.json)。若后续调查，须另取干净起点复现，不用本轮夹具报错直接给产品定责。

### 9.2 platformer 最小独立结果

正式 Load 一次的原回执2143于11:48:02成功，首次空返回后用户直接确认处理弹窗；未重发Load。显式编译后仍短暂断连，用户确认没有新弹窗、Unity正常；按连接恢复核到 compile `1794973809844→1794973814125`、domain `1794973825371`，Player MVID `c3590bdd-cab6-4748-9364-3bb05b8967fa`、UI MVID `3d2f71fa-fe53-4857-970f-1628113e9268`，场景干净、Console0warning/error，才进入Play。[platformer-compile-r4.json](evidence/acceptance-final/platformer-compile-r4.json)。

**11:54:05.285—11:54:22.897，17.612秒，7/7**：[platformer/result.json](evidence/acceptance-final/platformer/result.json)。隔离存储目录下实际 Start、Settings/Close保持同Controller/暂停世界、Save0、SaveExit清退、Main Continue来源槽位0、Back清退均通过。默认 ShowHealth=false/Health=null 保持；没有为验收启用可选Life/Health方案，相关旧实施夹具仅作为已审原证据。已查看本轮Continued1920图；三尺寸外观沿A-FINAL1原图核对，不称本轮重跑三尺寸。

结束Console0warning/error；所有消费者关闭后相关资源全0，capture **4096→4096**；确认退出Play，28阶段字体无漂移。没有写用户原槽位，测试请求使用任务隔离目录；没有完整玩家/物理/路线重测。

### 9.3 正式 topdown 与原现场恢复

仅通过中心正式 Load 更新后 topdown0.1.3/8789d... 一次，**1251文件**于11:56:42成功；恢复前活动与正式存储逐SHA零差异。显式编译后本次topdown弹窗由用户直接确认处理，未重放；核 compile `1794974391990→1794974406953`、domain `1794974415710`，Player MVID `e190c566...` / UI MVID `9a08d215...`、非Play/编译/更新、Scene clean、Console0warning/error。[restoration-r4/compile.json](evidence/acceptance-final/restoration-r4/compile.json)。本轮没有再进入 topdown Play。

恢复原 DefaultLight 缓存仅将活动资产从正式97d40b...还原到起始 **14c9718d55d832593c689643cd752a277e0915fe418bb71f8f85c883653c0e4d**，保留前字节证据，ForceSynchronousImport重导入以丢弃内存差异；其他字体0差异。没有 SaveAssets 或 SaveTemplate，存储字体仍97d40b...。正式 PreviewCurrentTemplateSkills/SyncCurrentTemplateSkills 前后Differences均空，不虚构安装模板专属Skill。[defaultlight-restored.json](evidence/acceptance-final/restoration-r4/defaultlight-restored.json)、[skill-sync.json](evidence/acceptance-final/restoration-r4/skill-sync.json)。

原布局通过 Unity 原生 LoadWindowLayout 成功加载精确字节工作副本；原生操作写回布局后的939a...另保留，磁盘default-6000.dwlt再精确恢复起始 **6e4452224022c3df9a1bb4aa357caff8028e06c4d84e882d155394010787d3c5**。说明：1310载荷目录本身未包括这个实际default-6000布局文件，早先“含存在layout”的概括不准确；布局起始SHA一直在start-products.json中，当前文件及既有原载荷均与该SHA相同，本轮先核相等再另存start-exact-layout.dwlt使用，没有猜用不同旧布局。

唯一任务自有index10 `A-FINAL 1280`经原生API移除并保存；原十项/顺序、index5 QHD、GameView位置`(0,803.33,933,542.67)`恢复，Selection空。实际 FrameworkScene 干净，非Play/待Play/编译/更新，设备仅Keyboard/Mouse/Touchscreen，无合成设备、前景分段或Backdrop Output残留。[gameview-restored.json](evidence/acceptance-final/restoration-r4/gameview-restored.json)、[editor-restored.json](evidence/acceptance-final/restoration-r4/editor-restored.json)。

### 9.4 最终保护与逐AC结论

[final-protection.json](evidence/acceptance-final/restoration-r4/final-protection.json) 独立脚本exit0：

- 起始1310字节载荷完整，当前1309份逐字节相同；唯一差异是正式Load合法生成的Editor身份loadedAt，ID/版本/保存hash正确，未复制旧身份。
- 16,582框架/模板/ProjectSettings/UserSettings文件零漂移，原Git index `07b2454a...`保持；与I5三方表1420行逐项比较，除合法新Editor身份外零差异。
- 活动与存储唯一差异为已明确的原DefaultLight缓存，其他活动镜像零差异；原布局字节、字体和视图均核回。其他模板成品/父快照/metadata未被验收改写。
- 已有可成对计时运行片段合计 **159.168秒下界**，含保留的失败运行；第二夹具未生成结果及调查/Play空闲、编译、导入、清退、模态等待等缺口不填零，不声称完整总时长或全量通过。

| AC | 当前结论 |
| --- | --- |
| AC01 | 原R1矩阵已独立核；R2小说矩阵待实施更新，旧小说条目仅历史 |
| AC02 | base沿用A-BASE1与VP1，封存hash未变 |
| AC03/04 | 非小说所派最小风险独立集合完成；R2小说尚未实作/验收，不能整体通过 |
| AC05 | base旧A2沿用；source旧发现闭环、各已跑模板清退通过；非全排列回归 |
| AC06 | 原100图实际尺寸/SHA已核；source新增三尺寸、lane/platformer有限新图；设备/Player未测，R2小说新图待 |
| AC07 | 先前GUID/Binding/引用核对沿用，实际风险路径可达；R2小说原UI恢复后仍须对应核验 |
| AC08 | **本轮原现场正式恢复通过**，新身份合法、唯一原缓存差异及全保护证据齐 |
| AC09 | 已跑范围MCP编译/域重载/已加载程序集/Console齐，夹具环境异常完整保留；R2新改动另验 |
| AC10 | 用户要求小说恢复原UI只改GM；技术检查不代用户新视觉评估，未整体签收 |
| AC11 | **本轮非小说风险验收与现场交接完成；完整任务仍待R2实施/复验/视觉结果** |
| AC12 | 无新增产品代码/配置、无Save/Bump/父同步/提交/push/tag/发布/消费升级/清理；六子最终UI仍普通Save未追加封存 |

**书面交还 Unity：原独立对话已完成本轮运行、临时输入与所有消费者清退，正式回到更新后 topdown 并精确恢复原现场。当前没有待执行/结果不明的Load、compile或Play，没有运行中的探针。Unity现可由主对话按已定R2顺序派原实施接管；实施仍须先做自己的实际保护和状态核对。** 本记录不代主对话派发，也不允许并发开始；独立之后不再调用Unity。所有失败/备份/原图保留，任务未归档或清理。


## 10. A-FINAL5：R2小说原UI／仅GM独立复验完成

2026-11-18，Asia/Shanghai。原独立对话沿用完整模式和主对话派发A-FINAL5，直接读取R2/D9、真实原C、正式保存成品及实施原始回执。**本节为最新技术结论，取代前文小说旧玻璃成品与待实作状态；不代用户VP2。** 其他五子与base沿A-FINAL1—4，不重复全量或完整矩阵。未实施产品修复或改写模板成品、测试、快照；模板切换与原现场恢复按授权执行。

### 10.1 原UI真实基线与限定差异

独立脚本 [static.json](evidence/acceptance-novel-r2/static.json) 32/32通过：原C **1633文件／eadb06cc16d55c09718ac839ae8dd554**；正式小说 **1816文件／13f45e806dd12d33be1779b11864c40e**，版本0.22.4、封存仍b577b887cef72e3db2264e5f33dd5c3c；父base0.11.0、889文件／1c5918eb6d798246aa4d080ac7b9eeee，父快照逐字节等于当前base。不是用已父同步的S冒充原UI。

- 14个指定玩家Prefab中12个与C精确相同；Main/Setting删去仅新增的四行后逐字一致，meta全部精确C。四个字段maskEffect=0／overrideMaskTint=false／backdropSettingsOverride=null／backdropSchema=0与当前初始化默认值一致；特别核schema0令HasExplicitMaskTint=true，保留原maskColor语义，不能只凭“数值0”认定。实际Main/Setting运行属性均已独立验证。
- 906个原GUID全保持；10份重新生成Binding的声明／ControlMap忽略生成注释和空白后与C等价；原Choice用户源精确C。Narrative360、Narrative配置76、Localization6、生成FontSkin ID2及FontSkins配置精确C。两个新辅助类及meta均删除，按旧meta GUID另扫正式资源无悬留引用。
- 小说105行迁移矩阵的实际路径／SHA全部匹配。BUI Main变体本体精确C并仍引用同GUID Main；此项为静态继承核对，本轮未独立实例化该变体。
- Appearance为Legacy玩家公共弹窗／SoftGlass GM，正式ShowGM接线保留。场景相对C仅源库GO与4个PrefabInstance文档不同，原相机／Launcher其余对象保持，源库inactive；不整场景回退。其他五子、框架和父快照最终零漂移。

另见 [additional-static.json](evidence/acceptance-novel-r2/additional-static.json)、[matrix-and-variant.json](evidence/acceptance-novel-r2/matrix-and-variant.json)。实施44张新PNG逐尺寸／SHA通过，1历史夹具图排除，43张非历史图以5张可追溯拼图逐项查看，含三实际尺寸原首页、设置、阅读、菜单、槽位和GM。旧I-R11小说玻璃玩家图不再作当前视觉依据；字体/皮肤夹具图不是默认外观。

### 10.2 本轮新独立行为与编译证据

正式Load小说一次1816成功，loadedAt12:53:27。用户正常处理本次弹窗后取得原回执，没有重放；显式MCP编译一次，连接经30秒间隔第2次恢复。核compile **1794977859854→1794977878105**／domain **1794977886905**，实际NovelSession MVID01ae51b0…／UI32bcb5ed…／Binding19493bf5…，非编译／更新、Scene clean，编译Console0warning/error。recovered回执没有直接当完成证明。[compile-confirmed.json](evidence/acceptance-novel-r2/compile-confirmed.json)。

**2026-11-18T13:00:24.7138841+08:00—2026-11-18T13:00:54.8190796+08:00，30.105秒，30/30**，见 [runtime/result.json](evidence/acceptance-novel-r2/runtime/result.json)。仅一次Play，无失败断言／补跑：

1. 原Main→Settings，实际七Slider契约、TextSpeed写隔离账户；英语／简中实际切换、一套字体皮肤与原映射保持；原Main/Setting新增默认字段的实际遮罩语义。
2. GM入口采用原Floating的PointerDown／PointerUp，中心Close用Button Pointer事件处理，打开／关闭／复开唯一实例。事件由EventSystem ExecuteEvents投递，留实际屏幕位置与raycast结果，**不是硬件鼠标或完整InputSystem设备测试**。
3. 在原Main仍打开时经API关闭实际GM FreePage，等待ActivePages移除；Boundary0、capture停止；原Main相机／层仍可见，实际Pointer点击Settings成功。然后经正式ShowGM恢复唯一实例。整页释放不冒称用户按钮功能。
4. 公共Confirm经原API路由到Legacy类型，Cancel后等待实际移除且回调恰好一次；不是声称小说业务必经该全局弹窗。
5. 真实新游戏、名字Ada、原Title推进到对白、ReadingMenu自有暂停；QuickSave实际槽6／当前Schema与Command；原八槽页面及自动槽只读；QuickLoad真实替换并Dispose旧Session，等Loading揭幕结束、Reader Menu实际可交互、暂停清空后再操作，恢复Command和名字一致。
6. 原Font页Small／Large修改预览与偏好，原Image皮肤A／B／base可见且恢复Sprite／Color；ReturnMenu释放阅读Session、真实隔离存档令Continue出现；源库仍inactive。没有重新跑全剧情、全语言字形、所有槽覆盖／取消矩阵或Choice路径，相关实施自检来源保持区分。

本轮11张原始1920×1080 PNG已核实际尺寸／SHA并逐张看图，见 [independent-images.json](evidence/acceptance-novel-r2/independent-images.json)。原字体面板实测屏幕点(81,672)：A像素(63,33,63)、B(29,64,63)、base(47,45,35)，证明原Image皮肤视觉生效；不是只核属性。新原图未编辑，审阅拼图另存。三尺寸范围采用上述43张新实施原图，**不冒称本轮重新跑三尺寸**。

结束Console **0错误／6警告**，均为原DingTalk缺“體、語”字形，旧原资源／映射完整且I-R13已记录此限制，不当新增迁移缺陷，也不宣称四语字形全覆盖。[runtime-console.json](evidence/acceptance-novel-r2/runtime-console.json)。关闭消费者后Boundary／前景Camera／RT／Backdrop材质全0、capture **2336→2336**。确认退出Play并同步重导入6个字体/配置资产丢弃运行内存副作用；42份字体阶段保护0磁盘漂移。39份用户原存档前后逐SHA相同，两个语言偏好和活动字体皮肤finally还原；所有业务写入任务隔离store。

计时 [timing.json](evidence/acceptance-novel-r2/timing.json)：本轮业务30.105秒；从Play请求返回到退出并完成字体导入确认的保守包络上界 **128.215秒**，包含启动、空闲、清退和Stop后导入，非精确有效Play时间。原A-FINAL1—4可量片段159.168秒下界保留，加本轮为189.273秒独立可量片段下界；I-R13自检70.635秒另记，不混为本轮独立结果。切换、编译、模态等待及未知间隔不算零，不宣称任务完整总耗时。

### 10.3 本轮新保护与正式现场恢复

先于Load建立自己的 [1351文件起始载荷](backups/acceptance-novel-r2-start/manifest.json)，逐SHA有效，含实际default-6000布局字节、43份.agents发现文件、原index和所有受管现场；只借用I-R13路径清单，未借旧备份字节／hash冒充本轮起始。新起始Editor/布局证据在evidence/acceptance-novel-r2。

正式回载topdown一次1251成功，loadedAt **2026-11-18 13:03:43**，身份0.1.3／8789d264af2650178116bbdbe8ea4c5f；原请求未重放。本次显式compile **1794978721355→1794978737358**／domain **1794978744526**，实际程序集及Console齐。随后不再Play，不SaveAssets/Template。

原DefaultLight本地缓存精确恢复14c9718d55d832593c689643cd752a277e0915fe418bb71f8f85c883653c0e4d，并强制同步导入；正式存储仍97d40b…，不把缓存写进成品。正式Skill Preview／Sync前后Differences空，发现文件43份精确。Unity原生加载本轮1351备份中精确原布局工作副本成功，磁盘布局恢复原SHA **6e4452224022c3df9a1bb4aa357caff8028e06c4d84e882d155394010787d3c5**；原10项／顺序、index5 QHD、位置(0,803.33,933,542.67)及空Selection均核回。本轮未新增分辨率项，不移除任何用户项。

恢复工具过程另留两项非产品失败：首次CodeDom查询因6000.5的GetInstanceID弃用而未执行，已改用名称记录空Selection；随后给已停靠GameView写position使它变成浮窗，已保留异常现场，重新通过同一精确原布局恢复停靠，删除position setter并核原位置。原布局延迟回调曾仍在队列，确认唯一且未执行后移除并调用原回调一次；第二次布局加载是修正已知浮窗副作用，非重放结果不明操作。最终本任务待执行布局回调为0，原三个输入设备、无脏字体／前景Camera／RT。见restoration/view-query-first-attempt.json、editor-restored-before-dock-correction.json、layout-queued-callback-confirmed.json、layout-dock-correction.json；首次漂移审计因尚无editor-restored.json而中止，最终补齐证据后exit0。小说30/30用例未重跑，无产品修复／新增编译请求。

[final-protection.json](evidence/acceptance-novel-r2/restoration/final-protection.json) 独立脚本exit0：1351备份有效，当前1350份字节相同，仅正式Load合法新Editor loadedAt不同；16,578份框架／模板／ProjectSettings／UserSettings零漂移，原index 07b2454ab28af72dce996b96f1ad27eba074c651c05f53f8f22a6d3a151655f0 保持。当前活动与存储唯一差异仍原DefaultLight缓存。用户原39文件、原C／原成品／父快照／其他模板未被验收改写。

最终MCP state observed **1794979159966** ready／idle、非Play／暂停／切换／编译／重载／更新／测试，FrameworkScene clean；没有结果不明或待执行操作，没有运行探针、合成设备或Backdrop输出。见 [书面Unity交还](evidence/acceptance-novel-r2/restoration/unity-handoff.json)。此后独立不再调用Unity，交主对话收取；后续接手仍先核实际状态。

### 10.4 当前逐AC与边界

| AC | 最新结论 |
| --- | --- |
| AC01 | 原其余矩阵沿用；R2小说105行全部实际SHA匹配，当前静态通过 |
| AC02 | 沿用A-BASE1／VP1，base封存保持 |
| AC03/04 | R2原UI／仅GM及其他五子所派最小风险独立范围均完成；仍不代VP2或全玩法 |
| AC05 | 本轮GM最后消费者与全部资源清退独立通过；既有base/source等风险结果沿用，非全排列 |
| AC06 | 新43图三尺寸原证据审阅＋本轮11图1920原图／像素通过；未新增设备／Player测试 |
| AC07 | 14Prefab、906GUID、10生成绑定／Choice／配置及运行路径通过；BUI仅静态核继承，不冒称全Prefab原生重扫 |
| AC08 | **本轮新1351保护与正式返回／精确现场恢复通过，Unity已交还** |
| AC09 | 本轮小说与返回topdown的显式MCP编译／域重载／实际程序集／Console齐；原字体缺字限制保留 |
| AC10 | **用户新视觉评估仍待**，旧小说玻璃图被R2取代，其他五子也不据技术通过自动签收 |
| AC11 | **所派独立技术验收完成；整体任务仍待必要人类视觉结果及主对话收束，不标完成／不清理** |
| AC12 | 无产品／测试改动，无Save/Bump/Apply／提交/push/tag／框架发布／消费升级／清理；当前六子仍普通Save未最终追加封存 |

没有新增确定产品缺陷、没有需要实施返工的技术失败；恢复工具错误与修正另如实保留。所有原图、备份、旧夹具失败和证据保留；不因本轮通过删除材料或扩大到发布／消费端验收。


## 11. A-FINAL6：R3原场景气泡定向独立复验与topdown交还

2026-11-18，Asia/Shanghai。主对话派原独立对话承接R3/I-R14-save-r2，当前结论以本节为准。**R3所派技术范围通过，topdown及新起始现场已恢复，Unity已书面交还；R2小说30/30与其他已收尾范围未重跑。用户新视觉与整体收束仍待主对话，未整体签收／清理。**

**基线：**同一实例ember-unity-framework@b23e9b768e85ed30、新会话f0bab1f1…，直接承接已保存source0.6.3／**ba33dd3fe620c9ec3aed3818315bd02f**，未重复Load source或无变化地编译。独立正式算法重算一致，活动／正式／保护projection均954文件逐SHA相等。原C0.6.2／80a86599d447326f049b4be3954db7cb，三个恢复文件精确C；438原GUID、两Prefab EUIBinding序列化块及原Bubble.Binding.cs保持，另951文件与实际修改前source-active一致；新父889逐字节等于base0.11.0／1c591…，原封存74704c…保持。97行source矩阵和5张新版1920原图尺寸／SHA均核，图已独立查看。[static.json](evidence/acceptance-source-r3/static.json)。

I-R14显式compile1794982132306→1794982138805／domain1794982154832沿用，当前重启后事件字段为空的事实保留；本轮实际读取程序集MVID **e303657a-69d6-439a-8381-0c49dc396840**、OnShow IL7／SHA10a1c2f6…与原编译记录相同，且活动源与已编译保存源精确相等、Editor idle／Scene clean／Console0，才进入Play。没有把重启事件空值或单次idle独立当编译证明。[native-start.json](evidence/acceptance-source-r3/native-start.json)。

**本轮一次Play，12/12，10.843628秒**（2026-11-18T14:48:59.157902+08:00—2026-11-18T14:49:10.0015302+08:00）：真实Main Start→Gameplay/SceneUI，两个原Sprite气泡注册并显示；气泡无SoftGlassSurface／BackdropGraphic、场景宿主无独立BackdropGroup。单泡隐藏至延迟回池、另一泡连续可见，再显复用原实体，global Boundary数量保持且无新增气泡专属消费者；不沿用旧“每泡Lease必须非空”的已失效断言。移动原Cube并核屏幕位移>1像素，归位误差<1像素；设置确实隐藏自有SceneUI且Close屏内可用，完全关闭后原alpha／气泡实体集合／无玻璃状态恢复；Back清退SceneUI及物体注册，当前QHD尺寸保持，临时位置在退出前还原。[runtime/result.json](evidence/acceptance-source-r3/runtime/result.json)。

五张本轮**2560×1440**原PNG已逐尺寸／SHA核并审图，含原气泡、跟随、设置、关回、返回首页；不是实施1920图冒充本轮输出。[independent-images.json](evidence/acceptance-source-r3/independent-images.json)。不做多尺寸扫、全量、设备、Player、小说或其他模板验证；本轮只一次隐藏／复用，不重复此前预隐藏等完整所有权矩阵。未新增产品或行为失败。定位脚本初次误用了不存在的备份名、把完整保护当五根投影、假定SceneUIPanel有独立.Binding.cs；均在Play前更正为实际备份／投影及两真实EUIBinding块，未执行产品修改，也不记成产品失败。

Console运行及最终均0warning/error。关闭全部消费者后Boundary／前景Camera／RT／Backdrop材质全0，capture **2156→2156**停止；确认Stop后原41份字体阶段文件0磁盘漂移并同步重导入5份字体资产，语言偏好／fontSkin1保持，source954镜像仍精确。未写用户存档。[resource-release.json](evidence/acceptance-source-r3/resource-release.json)、[fonts-restored.json](evidence/acceptance-source-r3/fonts-restored.json)。本轮业务10.843628秒、含启动/空闲/清退及Stop后导入确认的Play包络上界87.143秒；接续独立可量片段下界200.116823秒，I-R14自检12.357827秒单列，编译/模态/重启和未知缺口不算0，不报全任务完整时长。[timing.json](evidence/acceptance-source-r3/timing.json)。

**恢复：**按派发逐SHA核实并复用I-R14重启后新topdown-start **1351**字节保护，没有另复制这1351项；仅独立保护本次source字体/当前668c布局和当前产品hash。完成后中心正式Load topdown **一次1251成功**，loadedAt **2026-11-18 14:50:39**，0.1.3／8789d264af2650178116bbdbe8ea4c5f。一次显式compile请求；未知回执未重放，用户确认MCP后观测编译开始、域重载间隔一次30秒重连恢复，核compile **1794984753104→1794984774180**／domain **1794984785711**、UI9a08d215…／Player e190c566…及Console0。[compile-confirmed.json](evidence/acceptance-source-r3/restoration/compile-confirmed.json)。此次没有目录事务失败，没有移动／删除旧失败原件、修改权限或事务引擎。

原DefaultLight本地缓存恢复**14c9718…**并强制导入，正式字体仍**97d40b…**；之后无Save/Play。公开Skill Preview/Sync前后无差异，43发现文件精确。通过原生LoadWindowLayout(path,false,false,true,true)一次同步应用新保护中的原完整**6e445222…**布局，不写position setter或排队deferred callback；同调用内坐标尚为局部y24，保留原记录，正常布局完成后只读核回原停靠位置**(0,803.33,933,542.67)**、docked=true、原十项/index5 QHD／空Selection，未新增或删除分辨率项。磁盘布局随后恢复精确原字节；source接手的重启668c…字节独立保全，非混改YAML或默默丢失。原启动缓存与新版metadata未整体倒灌。

[final-protection.json](evidence/acceptance-source-r3/restoration/final-protection.json) 独立脚本exit0：1351原保护完整；当前仅两项合法差异——Editor新loadedAt、source模板普通Save的新contentHash（a25…→ba33…，其余metadata字段逐项相同）。16,578产品相对本轮接手只差按要求恢复的原layout，其他五子／框架／source成品／父889不漂移；43Skill、原index **07b2454ab28af72dce996b96f1ad27eba074c651c05f53f8f22a6d3a151655f0** 保持。活动topdown与正式存储唯一差异仍原本地DefaultLight缓存。原C及旧失败备份未清理。

最终state observed **1794985027375** ready／idle，非Play／切换／编译／重载／更新／测试，FrameworkScene clean，原Keyboard/Mouse/Touchscreen、无dirtyFont／前景Camera／输出RT／事务保留目录，Console0。**无本任务结果不明或待执行Load/compile/Play/布局／探针，Unity已交还主对话，此后独立不再调用Unity。** [unity-handoff.json](evidence/acceptance-source-r3/restoration/unity-handoff.json)。

AC01/03/04/05/06/07/08/09的R3定向技术证据齐；AC02及R2/其他范围沿既有，AC12边界保持。**AC10用户视觉仍待、AC11整体未完成**；技术审图不能代人类签收。无Save/Bump/Apply/提交/push/tag/发布/消费升级/清理，所有备份、失败和原图保留。


## 12. A-FINAL7：R4正式背景独立技术验收与最终现场交还

2026-11-18，Asia/Shanghai。主对话派原独立对话完成最后一次R4定向验收，并明确转达用户“**背景效果符合要求**”；其他五子视觉已认可。**本轮独立技术范围通过，Unity已恢复topdown并交还主对话。人类视觉与技术证据分开记录；主对话可核对收束，不由独立擅自归档／清理／发布。** R2小说30/30、R3气泡12/12及其余已通过范围未重跑。

### 12.1 两模板真实成品和限定差异

| 模板 | 版本／文件 | 当前实时 | 原封存 |
| --- | --- | --- | --- |
| base | 0.11.0／889 | 9077bd5b5271898e8055d1c7cf72b0a7 | 1c5918eb6d798246aa4d080ac7b9eeee |
| ui-game | 0.2.3／943 | 44b95096df206b5a160a1853333cc498 | 0c4917852d8cf6d759e629d2b21371f3 |

中心同算法独立重算一致；接手已保存UI游戏活动与正式943文件逐SHA相同。与I-R15实际修改前存储比较，两个模板各仅GameMainState.cs、EUIBackgroundPage.prefab、FrameworkScene.unity三文件，base另EUIGamePlayPage.prefab。Main源只删OnMainExit的ClearBackground，原await等其余源保持；场景只相机clearFlags=SolidColor及(.94,.96,.98,1)；Background新增纯装饰RawImage/EnvelopeParent引用既有Background.png GUID7bcbdb…、不抢raycast。base第四文件只增加旧无Sprite BgImage的m_Enabled=0覆盖，保节点／绑定／其他控件。各自其他资源包括公共玻璃参数、Renderer、Shader、Steps Variant与共享对照全部字节保持。Background原绑定块仅多4个隐式默认值的显式序列化字段（Inherit0/false/null/schema0），删这4行后精确旧块；schema0仍保旧maskTint语义，不误称整块字节相同。

六份父快照各889，逐SHA仍等于保存前sealed base1c591…，没有同步9077…；其他五子/框架无本轮产品漂移。base当前**未封存**，后续ParentUnversioned门禁保留，不能宣称六子继承了新背景。两模板194矩阵行实际SHA匹配。30张最终三尺寸原图已核PNG实际尺寸／SHA并逐张审阅；base采用最终runtime-r3，早期NaN和旧不透明底板失败图仅历史，不混入通过。[static.json](evidence/acceptance-background-r4/static.json)。

### 12.2 独立UI游戏：真实路径及两项夹具补核

原ui-game0.2.3/44b…、同实例b23…，实际程序集89516c13…、OnMainExit IL16，与I-R15显式compile1794988729891→1794988734054/domain1794988740138一致，非Play/编译/更新、Scene clean、Console0，未重复Load ui-game或无变化编译。

一次Play在原QHD上实际Pointer raycast及PointerDown/Up/Click：首页→三步玩法，step1设置暂停同一Session，关闭后原Session/进度继续；三步真实结果、Restart新Session0、返回首页通过。专用三Step Variant保持。全路背景单槽位同实体，低排序0／EnvelopeParent全覆盖，正式材质乘白、不抢输入，SolidColor兜底，无默认天空/地平线，玻璃Lease Ready。

原探针**27项中20 true／7 false**如实保留，[原result.json](evidence/acceptance-background-r4/ui-game/runtime/result.json)，10.944038秒。六个相同背景复合断言误要求背景与内容同相机；真实背景是UICamera，内容合法地在EUI Foreground Segment相机，记录在camera诊断与修正脚本。即时CaptureScreenshotAsTexture像素全黑，而同帧正常PNG显示底图，原黑值不能证明背景不绘制。**未修改产品**，只定向补核上述两点，不重复整套三步流程：8/8，8.539779秒，[supplemental/result.json](evidence/acceptance-background-r4/ui-game/supplemental/result.json)。修正为合法相机关系／低层顺序，PNG落盘后读取实际像素；临时隐藏FormalBackground对照结束finally恢复。四处外缘点由背景图的不同RGB变为一致兜底RGB，至少两点差>.015，证实正式底图实际参与合成。原27与新8不虚拼成35/35，原false与未重复的业务证据全部保留，7项夹具发现已闭环。

### 12.3 独立base：16/16

正式Load base一次889，loadedAt16:18:22。原Load经MCP触发本次实际compile **1794989906885→1794989916238／domain1794989922872**，实际aee95b07…/IL16及Console0齐；没有额外源代码修改，未追加无变化第二次编译，也未用旧I-R15事件冒充本次。证据[load-compile-confirmed.json](evidence/acceptance-background-r4/base/load-compile-confirmed.json)。

原QHD一次Play **16/16，10.632613秒**：[base/result.json](evidence/acceptance-background-r4/base/runtime/result.json)。真实首页→玩法→设置→原玩法页→首页，原装饰BgImage.enabled=false；同一正式背景贯穿，低层/全覆盖/原比例/不抢输入/安全相机与玻璃Ready通过。PNG on/off诊断也确认底图实际可见，临时enabled变化在finally恢复，无产品Save。base只核同一玩法Page实例，未虚构不存在的UI游戏Session语义。

两模板各一次Play，未重跑多尺寸/设备/Player/全量。独立16张QHD原PNG全部尺寸/SHA核验，其中2张off-diagnostic仅为隐藏夹具、不能作默认视觉；14张正常图已审。三尺寸采用上述实施30张有效图，不冒称本轮重跑。独立主要观察稳定页及过渡期间背景同一性，未新增逐帧开屏观察器／强制迟到资源竞态。用户已认可视觉，此项不由技术审图代签。[independent-images.json](evidence/acceptance-background-r4/independent-images.json)。

### 12.4 清退、保护与topdown恢复

两模板先关闭内容保留同一背景：UI游戏capture **7635→7635**、base **2857→2857**，Boundary／前景Camera／RT全0；再ClearBackground后槽位与活动FormalBackground全0，才Stop。运行/最终Console均0warning/error；各41字体阶段文件0磁盘漂移，同步重导入5字体资产丢弃内存副作用，语言偏好与skin1保持，诊断RawImage.enabled均已还原。未进行存档操作，未写产品。[UI清退](evidence/acceptance-background-r4/ui-game/runtime/resource-release.json)、[base清退](evidence/acceptance-background-r4/base/runtime/resource-release.json)。

T01新增可量业务片段含原夹具false共 **30.116430秒**；UI游戏与base的保守Play包络上界分别183.063/105.902秒，含启动／空闲／清退／Stop后导入，非精确有效Play时长。独立累计已知片段下界230.233253秒；I-R15自检1627.104987秒下界另列，不与独立断言混计，不把编译/modal/此前未知缺口填0。[timing.json](evidence/acceptance-background-r4/timing.json)。

复用并逐SHA核实**I-R15真实topdown-start1351**，未重复复制完整载荷；独立仅另保护各阶段字体、产品hash及I-R15视图映射。正式回载topdown一次1251，loadedAt **2026-11-18 16:25:36**、0.1.3/8789d264…；一次显式MCP compile请求，未知时未重放，实际compile **1794990456076→1794990470995／domain1794990477650**、UI9a08d215…／Player e190c566…、Console0齐。[compile-confirmed.json](evidence/acceptance-background-r4/restoration/compile-confirmed.json)。

原本地DefaultLight14c9718…精确恢复并强制导入，正式字体仍97d40b…，之后无Save/Play；公开Skill Preview/Sync前后无差异、43文件精确。用I-R15原完整6e445222…布局同步原生加载一次，keepMainWindow=true，无position setter／deferred队列；立即调用内y24局部坐标原样保留，布局稳定后只读核回原停靠**(0,803.33,933,542.67)**、原十项/index5 QHD／空Selection，磁盘字节精确还原。未新增/删除尺寸，未倒灌旧身份、base/UI元数据，旧事务失败765原件及所有旧备份均未触碰。

最终独立审计exit0：[final-protection.json](evidence/acceptance-background-r4/restoration/final-protection.json)。1351保护有效；当前仅三合法差异：Editor新loadedAt、base与ui-game普通Save新contentHash，其他metadata字段相同；**16,578当前产品/设置0漂移**，其他五子/父快照保持，43Skill/原index07b2454…/布局6e445222…精确。活动与topdown存储唯一差异仍原DefaultLight缓存。最终state observed **1794990775495** ready/idle、非Play/待切换/编译/重载/更新/测试，FrameworkScene clean，无dirtyFont/前景Camera/RT/事务保留目录，无本任务pending操作/探针，原三输入设备。**Unity已书面交还主对话，此后独立不再调用Unity。** [unity-handoff.json](evidence/acceptance-background-r4/restoration/unity-handoff.json)。

### 12.5 当前AC结论

AC01—09所派R4技术范围通过，既有R2/R3/其他范围沿用，未扩测设备/Player。AC10依主对话转达用户“背景效果符合要求”及其余五子认可，视觉已通过，独立不代签。AC11技术交付与现场交还齐，可由主对话最终核收束，尚未由本独立对话归档。AC12无产品Save/Bump/Apply/父同步/提交/push/tag/发布/消费升级/清理；base9077…和ui-game44b…仅普通保存，原封存及六子旧父1c591…保持，发布仍须另行授权。没有新增确定产品缺陷或需实施返工项；所有原false、补核、失败和原图保留。
