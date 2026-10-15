# 平台玩家战斗机制实施记录

## 2026-10-15 补验改动已正式保存到模板

按用户明确授权，通过 EmberProjectSetup.SaveTemplate 保存当前 platformer-2d，返回 1317 文件；保存事务完成资源/.meta/GUID 及 Skill 源校验。新增 PlayerCombatRuntimeFaultTests.cs 与 .meta、PlayerController 诊断修复及 PlayerCombat.md 正式说明均逐文件 SHA256 对比，与模板快照一致。此前“尚未保存”的描述为历史状态，已由此次保存补齐。

版本仍为 0.4.0，contentHash 更新为 373437cccbe9f4ba6e93359da2c8b2fd；versionedContentHash 仍为 9199e242cdf2226cab67711716ddc007，父级 base 0.8.3 及父 hash 不变。保存前后实时配置、玩家窗口列表与场景状态完全一致：默认 PlayerDefaultScheme、窗口为空、FrameworkScene dirty=false，非 Play，Options=1/Enabled=true。保留用户草稿，没有切换或加载模板。测试结果沿用紧邻上轮实际补验，本次仅保存与一致性核验，不声称重跑测试。T03 剩余项继续延期真实项目；未 Bump、提交或发布。

## 最新续跑完成：攻击运行中故障与刀光六帧（2026-10-15）

Unity MCP 重连、脚本编译及运行验证完成。Play Mode Options=1、Enabled=true。首次故障测试发现 PlayerController.Check 用诊断对象 ToString 丢失原始异常；增量修正为保留故障节点、阶段和 InnerException，没有修改公共框架、方案或玩法参数。首次失败 job 为 2933486ac23e4761a0ecd2bea6e29a34，修复后故障测试 1/1、相关回滚回归 2/2 通过，详见[本轮证据](platformer-combat-evidence/2026-10-15-final/notes.md)。

S03 新增证据：正式内置方案第一刀与刀光已运行时，向实例 FixedUpdate 回调注入异常，验证模块 Faulted、会话/输入关闭、物理冻结、动画和刀光释放、重复 Stop 幂等。未破坏正式图资产。正式 Main→新游戏进入 GameplayScene，以临时 0.1 时间倍率捕获右向与确认转身后的左向 Slash_0～5，共 12 张真实渲染画面；逐张检查朝向、镜像、展开与消退，通过。首次左向尝试实际仍朝右，保留为未满足采集条件的记录，不计左向通过。

结束状态：非 Play、非编译，FrameworkScene dirty=false，Options=1/Enabled=true、timeScale=1，临时键盘和回调已释放；玩家窗口与开始时同为空，默认配置仍为原 PlayerDefaultScheme。未改变默认方案及用户草稿。本轮新增故障测试、诊断修复和正式说明尚未另行保存进模板快照，之前保存的模板不受改写。T03 剩余项继续延期真实项目，P9 不整体放行；无 Bump、提交、发布。

## 最新续跑：S03 攻击中故障及六帧实时检查（2026-10-15，编译查询阻塞）

用户要求继续未完成部分，本轮范围为非 T03 的两项验证边界；T03 仍延期真实项目。Unity 最初连接成功，Application.dataPath 为原仓库 Assets，Options=1/Enabled=true，非 Play/非编译，玩家窗口枚举为空，默认仍 PlayerDefaultScheme。未修改默认配置、草稿或正式图/方案。

新增 `Assets/Game/Module/Player/Tests/PlayerCombatRuntimeFaultTests.cs`，测试 `Game.Player.Tests.PlayerFoundationTests.CombatRuntimeFaultReleasesActiveAttackAndModuleSession`：拟使用正式内置方案与实际 PlayerModule，在第一刀及刀光显示后仅给实例状态安装抛错回调，沿生产 FSM 与模块异常处理检查 Faulted、输入关闭、物理冻结、刀光/动画释放及重复 Stop 幂等。此为测试实现，不是通过证据；没有修改公共框架或正式运行代码。

请求 scripts 编译刷新成功，等待后首次 Console 查询返回 `Unity session not available` / `no_unity_session`。立即停止 Unity 调用，无重试、日志轮询、BatchMode 或 dotnet。新增测试编译和运行均未确认；六帧实时画面尚未开始。后续先重连并确认编译，再运行该测试；通过后继续六帧实播取证。静态检查不能代替这两项行为验收。

本轮新增测试尚未保存进模板，之前已保存的首版模板保持不动；不 Bump、不提交、不发布。本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；若仍有报错，请提供首条错误及完整堆栈。

## 2026-10-15 T03 临时副本已清理

按用户要求，仅删除本次创建的 `.utmp/t03-framework-copy`。删除前确认绝对路径位于本仓库 `.utmp`，无重解析链接、无 UnityLockfile，进程枚举未发现 Unity；使用原生 PowerShell 按 LiteralPath 清理，退出成功且副本路径不存在。未调用 Unity、未关闭原项目、未修改默认方案或用户草稿。

副本内测试计划和关键文件静态校验结果已先归档至 [测试计划](platformer-combat-evidence/2026-10-15-t03-copy-cleanup/test-plan.md) 与 [副本校验记录](platformer-combat-evidence/2026-10-15-t03-copy-cleanup/copy-verification.json)；原 `.utmp/t03-framework-copy-verification.json` 及既有验证记录保留。归档计划描述历史准备流程，不是当前操作要求。T03 剩余项仍延期至真实项目验证，没有新增通过结论；下方“副本保留原位”已被本节清理结果取代。无 Bump、提交或发布。

## 当前决定：T03 剩余项延期至真实项目（2026-10-15）

用户明确决定停止本轮 T03 剩余验证，待后续制作真实项目时再验证。合法派生、缺失/无效身份、消费项目部署身份、实际跨模板旧窗口及草稿恢复继续标记未验证；既有已通过结果保留，P9 不宣称全项完成。届时按真实项目适用身份逐项取证，不将开发仓库历史结果直接视为消费项目通过。

不再要求用户连接测试副本或切换当前模板，也不继续建立测试候选。`.utmp/t03-framework-copy` 保留原位，未自动删除；副本只完成磁盘准备和关键文件校验，Unity 编译/实时基线未确认。下方副本连接与切换步骤仅作为将来的参考，不是当前待用户执行事项。当前项目、默认方案和用户草稿保持不动；无 Bump、提交或发布。

## 2026-10-15 T03 独立测试副本准备

按用户授权，将当前磁盘 Assets、Packages、ProjectSettings 独立复制到 `.utmp/t03-framework-copy`，不复制 Library、原窗口或 SessionState；源目录无 reparse 链接，副本不共享可写目录。源文件约 582 MB，Unity 6000.5.4f1。已发出打开副本的交互式 Unity 启动请求（非 BatchMode），但 MCP 实例列表仍仅包含原项目，副本启动/导入/编译和实时基线均未确认；未继续自动 Unity 验证或切换模板。副本磁盘 Play Mode Options=1/Enabled=true，不等同实时确认。

发现合法派生前置限制：`EmberProjectSetup.EnsureParentCanBeBranched` 明确要求父模板 contentHash 等于 versionedContentHash。刚保存的 platformer-2d 不满足该条件；遵守不 Bump 要求，未创建派生、未改 hash、未绕过保护。该项继续未验证。缺失/无效身份已有纯逻辑测试不等于原生菜单/窗口交互通过；实际故障场景仍待准备，不能伪造正式身份放行。

下一步先由用户打开副本并连接其 MCP；代理核对 Application.dataPath 后建立独立测试候选和完整缓存基线，再通知用户在副本项目中心加载 base。base 取证完成后才通知切回 platformer-2d 比对恢复。操作顺序见副本根目录 `T03-TEST-PLAN.md`。当前未取得新的 T03 通过证据；原项目默认方案和草稿只读核对，未应用、清理或切换；不 Bump、不提交、不发布。

副本静态校验：Assets 29646、Packages 7207、ProjectSettings 26 个文件，与复制前计数一致；manifest/lock、框架包、模板元数据、编辑身份、默认配置、内置攻击方案及 EditorSettings 共 8 个关键文件 SHA256 一致。全文件 PowerShell 哈希校验耗时过长，主动中止该只读检查，未声称全文件哈希一致；实际结果见 `.utmp/t03-framework-copy-verification.json`。本次未完成副本 Unity 编译验证，需在副本 Unity 手动触发编译；若报错保留首条错误及完整堆栈。

## 2026-10-15 当前模板已保存（未 Bump）

用户明确授权后，经 Unity MCP 调用正式 `EmberProjectSetup.SaveTemplate` 保存 platformer-2d，返回 1315 文件。版本仍为 0.4.0，实时 contentHash 为 `77e1e1a9efad09016555eaea905837cc`；versionedContentHash、父模板 hash 均未改变。原显示名称及描述保持不变。正式入口完成暂存资源/.meta/GUID 与 Skill 源校验后事务提交；没有手改快照或 hash。

保存前后玩家窗口序列化内容、默认 PlayerConfig、场景 dirty 状态和已有资源 dirty 列表一致，Options=1。内置攻击方案原有 dirty 标记保留，其 18 个对象序列化内容与此前接受调校的保存后基线相同；没有应用或清理草稿。保存后的内置攻击资产、默认配置、正式使用说明及内置保存工具源码与项目磁盘源 SHA256 一致。证据见 [模板保存结果](platformer-combat-evidence/2026-10-15-feel-fixes/template-save.json)。

本次只保存模板快照，不新增玩法测试结论。T03 剩余项继续未验证，P9 不整体放行。没有 Bump、提交、推送、发布或消费升级。下方“尚未保存模板”描述属于此次授权之前的历史状态，以本节为准。

## 当前状态与续跑入口（2026-10-15 文档收口）

P0～P8 的实现及阶段验证已完成；P9 主体回归、正式入口与文档已有证据，但 **T03 部分身份场景仍未验证，P9 不标整体完成**。内置“通用攻击模板1”已采用用户确认的体验调校。下方各阶段的断连、未开始和待恢复描述保留为历史过程，不是当前阻塞。

| 证据范围 | 已取得结果 | 当前解释 |
| --- | --- | --- |
| P9 完整回归 | PlayMode 600/600、Editor 114/114、Storage 47/47，随后增量 23 项通过 | 调校同步前的历史回归，不冒充最新配置的全量重跑 |
| 最新内置同步 | 编译检查、保存重载、只读保护及专项 PlayMode 8/8 | 三刀左右范围、空冲退出 0.25、短受击归位；见下节和备份证据 |
| 正式入口与白盒 | 历史 Main→新游戏→保存返回→继续及战斗实播；后续用户确认调校体验 | 本轮文档维护没有新增实播；六帧实时截图未逐帧捕齐 |
| T03 | 所属入口、base 菜单隐藏/Skill 拒绝、Player 关闭/重启用与测试候选保留通过 | 合法派生、缺失/无效身份、消费身份、实际跨模板旧草稿恢复仍未验证 |

后续顺序：保留 T03 剩余项，待用户准备相应安全测试环境后续验；需实际切换模板时由用户操作。当前不重复关闭 Player，不改默认方案或草稿。普通资产保存已完成，但最新内置同步尚未由本任务保存到模板快照；模板保存、Bump、Git 提交、发布、消费升级均需各自明确授权，不能把实现完成写成已发布。详见[验收矩阵](platformer-combat-acceptance-matrix.md)。

本轮仅整理这份记录、验收矩阵和正式使用说明，未调用 Unity 或修改代码/配置，不产生新的编译与行为验证结果。维护前文件及 Git 状态备份在 `.utmp/ember-doc-maintenance/combat-closeout-20261015/`。

文档校验：framework 模式审计 398 份文档，维护前后均为 4 处既有发布记录断链（release-0.15.0、0.17.2、0.17.3、0.17.4），全仓 strict 因这些历史问题未通过；本次三份文档无坏链接、无新增断链。保留无关历史发布文件不扩张修复范围；报告见备份目录 before.json/after.json。本次文件 diff 空白检查通过。

## 2026-10-15 最新：体验调校同步内置方案

用户确认体验后，仅将调校副本的三段 Box 偏移 (3.2,0.2)/大小 (5.6,4)、刀光缩放 3.2/偏移 (2.4,0.2)、空冲退出倍率 0.25、21 个基础动画归零片段引用同步到内置“通用攻击模板1”。未改伤害、时间、图布局、默认方案或用户草稿。增补受模板开发身份及非 Play 限制的显式内置保存入口；不撤销归属登记，作用域只放行目标方案路径，finally 恢复保护。

本轮 Unity 编译后 Console 0 error；目标资产保存并强制重导后数值正确，完整方案 Validate 通过，普通 Save 仍拒绝内置写入。8 项专项 PlayMode 改为直接加载内置方案，本轮 job `a51c8436b8ce46a285a6a13939324adf` 为 8/8 通过（0.8737698 秒），覆盖三段左右远近命中边界、空冲速度倍率和短受击归位。同步前后调校副本、原默认方案、玩家窗口序列化内容完全相同；最终非 Play，Options=1/Enabled=true。证据见 `platformer-combat-evidence/2026-10-15-feel-fixes/builtin-sync-before.json` 与 `builtin-sync-after.json`。

本次未重跑完整矩阵或正式入口；此前白盒体验由用户确认，本轮为内置同步及专项回归。T03 剩余项继续未验证，不切换模板、不关闭模块。未保存模板、Bump、提交或发布。正式使用文档已更新当前数值与 Animator/刀光机制说明。

历史快照（T03 后续实测之前，不代表当前状态）：P9最新编译、恢复/时序/过滤/FSM仲裁阻塞已补齐。完整PlayMode600/600、Editor114/114及Storage47/47通过，随后新增覆盖23项全部通过，详见末节和验收矩阵。正式文档已增量收口。最终Options=1/Enabled=true、非Play、MainStage、FrameworkScene和默认PlayerDefaultScheme均无dirty；未改默认或用户草稿。T03实际模板/模块切换菜单验收仍未执行，不能将P9宣称整体完成；需按仓库规则由用户在安全工作区操作实际切换后继续。上轮旧测试Undo.ClearAll风险仍保留历史说明，本轮未使用全局Undo清理。
执行依据：[已确认实施方案](platformer-combat-implementation-plan.md)。本记录只承接实际进度，不改变玩法规则或授权范围。

## 历史进度快照（P1～P7，已被后续证据推进）

2026-10-15 续跑更新：**P2～P6阶段退出验证通过；通用攻击模板1已启用三连击并接入冲刺路由/收尾，29节点，正式资产保存重载校验通过。P7核心受击、生命快照、模块恢复、沙袋/陷阱已有实现和分项测试证据，但生命联合提交测试查询发生MCP断连，P7未通过，P8～P9未开始。正式方案尚未安装生命节点、最终命中窗口和美术；正式入口与白盒全体验仍未完成。** 下段 10 月 14 日状态保留为历史节点，以本行及末节最新证据为准。

2026-10-14，**P0、P1 阶段基础完成；P2 控制权/输入/FSM 集成验证被 MCP 连接阻塞；P3～P9 未开始**。本轮 P1 Unity 编译、动画回退、联合提交/故障回滚/Undo/Redo/重载及冻结捕获测试通过，纯移动 PlayMode 回归 3/3 通过，P2 请求/时间逻辑 5/5 通过；最新增量编译未确认。通用攻击模板1的独立空洞手感基础资产已创建，尚未启用攻击或登记为完成的内置攻击示例；真实攻击节点与三连击在 P3 接入，正式体验在 P8 完成。尚未实现可运行的攻击、受击或恢复新机制。未执行模板保存、Bump、提交、推送、发布、消费升级或模板切换。

用户本轮补充：正式方案命名为 **通用攻击模板1**，与空洞骑士风格、蔚蓝风格同级；从空洞骑士风格独立复制手感，再开启攻击模块编辑。此要求落实到 P1 的方案创建及 P8 的正式三连击示例，不修改原两套方案，不因复制手感共享可变攻击配置。增加拓扑前独立图和 Controller。没有据此自动更换项目默认方案。

主要归属为 **platformer-2d 模板升级**。Player、可选 Entity 战斗能力、Gameplay 会话协调、资源、Editor 与模板 Skill 应在 Assets 中实施。P1 续跑确认公共编辑器缺少布局复制入口，因此增加一项框架编辑器改动，整体归为 **框架 + 模板升级**；运行时 FSM 尚无需修改。具体依据和最小范围见末节。

## P0 工作区与环境基线

| 项目 | 本轮事实 |
| --- | --- |
| 仓库 | `C:/Users/wuyu/My/ember-unity-framework` |
| Git | `main`，HEAD `e3aad088de167dd9ef4835869d32a2eeaec55023` |
| 原有改动 | 909 个已跟踪删除、48 个已跟踪修改、851 个未跟踪文件；无暂存差异。未跟踪目录展开计数，不能与默认短状态的目录条目数混用 |
| 基线证据 | [完整状态](platformer-combat-evidence/2026-10-14-p0/git-status.txt)、[未暂存补丁](platformer-combat-evidence/2026-10-14-p0/unstaged.patch)、[暂存补丁](platformer-combat-evidence/2026-10-14-p0/staged.patch)、[1808 条原有改动路径及 SHA256](platformer-combat-evidence/2026-10-14-p0/changed-file-fingerprints.json)。删除路径指纹为 null；未跟踪文件只记录指纹，原文件留在原位。补丁只作保护记录，不授权回放覆盖 |
| Unity 连接 | `mcpforunity://instances` 成功，单实例 `ember-unity-framework@b23e9b768e85ed30`，Unity `6000.5.4f1` |
| 实际项目 | `mcpforunity://project/info` 成功，projectRoot 与 assetsPath 分别对应本仓库及其 Assets |
| 编辑器状态 | `editor/state` 成功：非 Play、非 Pause、未编译、未刷新；活动场景 `Assets/Game/Scenes/FrameworkScene.unity` |
| 包版本 | 磁盘 `Packages/com.ember/package.json` 为 `0.18.0`；不是本轮发布结果 |
| 模板记录 | 磁盘 `Assets/Editor/EmberEditingTemplate.json` 为 `platformer-2d`、`0.3.1`、loadedAt `2026-10-13 21:18:16`；实时 `IsTemplateActive` 查询未取得结果，不能把磁盘记录等同于完整运行校验 |
| 物理磁盘配置 | Fixed Timestep `0.02`；重力 `(0,-9.81)`；`m_AutoSimulation: 1`、`m_AutoSyncTransforms: 0`。实时模拟模式及姿态可见时点未验证 |
| 草稿 | 场景 isDirty、SO 内存草稿、当前工具选择与下次白盒方案查询失败；不声称无草稿。没有执行保存、关闭窗口、重载场景、刷新或进入 Play |

已阅读根 AGENTS.md、CLAUDE.md、执行方案及仓库架构、业务目录、编码、API 速查、Unity 验证、UnityFarm 改动分流和文档维护约束。范围内递归查找没有发现附加 AGENTS.md/CLAUDE.md。环境没有 rg，后续检索使用 PowerShell。

### MCP 阻塞证据

基础资源查询成功后，首次 `execute_code(action="execute")` 尝试只读返回 Application.dataPath、正式模板身份、实时物理设置、已加载场景脏标记和脏 ScriptableObject 清单。返回 `success:false, message:null, data:null`，没有错误堆栈，不能确定是连接、执行器还是查询代码问题；也不能把它写成已确认的编译错误。

按照用户第 6 条和 Unity 验证规范，立即停止后续自动编译及运行验证，没有重试，没有调用 dotnet、BatchMode、Editor.log 轮询或其他替代验证。后续工作仅为文件静态核对和记录。本轮没有 C# 或资源修改。

## 配置所有权与资源引用

磁盘默认入口为 `Assets/GameResource/Resources/Config/Player/PlayerConfig.asset`，其 `_scheme` 指向 `PlayerDefaultScheme.asset`（GUID `a2d7100fc47f54b4282b5be389d735b8`）。场景为 `GameplayScene`，出生位置 `(0,1.05)`。当前编辑方案、下次白盒方案和内存冻结方案仍待实时核对，不与项目默认方案混同。

| 默认方案引用 | 路径或 GUID | 所有权及后续处理 |
| --- | --- | --- |
| Graph | `PlayerGraph.asset`，`a545c004d0aa61f4a85aae967fa1d423` | 默认、空洞骑士风格、蔚蓝风格方案共用；结构修改先复制并保留节点 ID/绑定/布局 |
| Feel | `Feel/BaseProfile.asset`，`bf9c47ae726c0fe4ba923c7efec2654b` | 目前 Movement/Jump/Input；Attack 参数归此，Powers 不重复放入 |
| Powers | `PlayerPowers.asset`，`cc1899bbe74cf1a40b3fb0b0fd1a8a94` | 能力装配、Enabled、次数的唯一配置来源 |
| Animation | `PlayerAnimation.asset`，`770fbc778cec4574fac8639b1b9f40f7` | 动画映射归此；需增加显式视觉变体 |
| Controller | `Assets/GameResource/Player/Animation/PlayerVisual.controller`，`bfa4e938e9f2ca542bc2ed088d634435` | 三套方案共用；新增动画目标先独立 Controller，允许共享美术 Clip |
| ActionEffects | `PlayerDefaultActionEffects.asset`，`183bf8f8b7041684a96fb76bda6dcf71` | 唯一效果接线；默认 EnableEffects 为 false，不可静默开启 |
| Input | `gameplay` 通道，Direction / Primary / Dash | 输入资产 Gameplay map 尚无 Attack；UI map 有 Confirm，默认 Enter 键 |
| Combo / Life / Reactions / Recovery | 当前 PlayerScheme 无对应新配置 | 待 P1 定稿；连接只归 Combo，载荷归命中窗口，生命接收策略归生命配置 |

运行数据必须由每个候选/会话独立持有：输入、动作时间、方向、接续记录、命中账本、HP、受击、效果句柄和检测器。源 SO/Definition 只持配置。`PlayerSchemeCapture` 捕获配置 A，`PrepareReplacementAsync` 对旧 capture 调用 Retain；应扩展这条既有冻结链，而不是恢复时重新读取源方案 B。

## 真实源码契约与改造清单

以下为本轮静态阅读结论，均未替代运行测试。

| 阶段 | 真实入口与事实 | 必要增量改造 | 验收关联 |
| --- | --- | --- | --- |
| P1 | `PlayerSchemeCapture.CaptureDefinition` 仅特殊克隆地空冲刺 Definition，其余共享；Feel.Configure 只接三分组 | 新配置深捕获，包含攻击 ID、形状嵌套、载荷、拓扑与生命；关闭攻击保持移动方案合法 | C01～C04、C06～C08、S01 |
| P1 | `Editor/PlayerSchemeWorkflow.Mutable` 排除 Graph/Definition，Restore 要求同图引用 | 增加独立结构事务，图/Controller/映射/Combo 全部预检再提交；保留原布局及自定义节点 | C03、C05、C06 |
| P1/P2 | FSM 已提供稳定 target slot、RequestSite、CanEnter 重定向、提交及 continuation | 复用这些接口；攻击 SegmentId 可由节点 ID 查冻结配置，或由专用 Definition 捕获，不将建议类名视为既有 API | I01～I03、C05、S01 |
| P2 | `PlayerInputAdapter` 仅跳跃有持续缓冲；Dash 在 EndPhysicsStep 清理；无 Attack | 场景化请求有效期/版本/按下意图/机会归属，保留普通移动即时冲刺语义 | I01～I07、S02 |
| P2 | Locomotion 先尝试落地，再写水平和朝向；Air 先跳跃、后冲刺，能墙滑/挂边 | 父子共用动作控制权，战斗上下文按当前可执行资格排序；普通移动顺序不被顺手重排 | A02、A03、A06、F05 |
| P3/P4 | AnimationSession.Sync 在叶 ID 与 generation 不变时直接返回 | 显式视觉请求/变体与动作速度同步；独立收刀计时和提示句柄 | A01～A07、R01～R09 |
| P5 | Dash EndReason 已有 Completed/Wall/GroundLost/Landed/Interrupted；End 一律钳水平并清竖速 | 按结束原因交接普通下一段与空冲惯性，机会截止与运动收敛分离 | D01～D12 |
| P6 | Controller 当前只有单玩家驱动，无跨实体命中收集批次；BindEffects 收集所属全部 Collider/Sprite | Gameplay 所有的普通会话协调对象，两阶段收集/结算；细化身体/受击/攻击/特效目标集合 | H01～H08、F01、F02、S04 |
| P7 | PlatformerSnapshot.DATA_VERSION=2，无 HP；Capture 已拒绝冲刺、翻滚、挂边等临时动作 | 版本及装配迁移接口、生命记录、底层保存拒绝、新反应/死亡恢复事务；不覆盖旧字段 | F03～F08、L01～L08 |
| P8 | PlayerEditorAccess 使用 IsTemplateActive 与 Player 模块 Enabled；模板 Skill 清单 schema v2，现有三个平台 Skill | 正式三连击/沙袋/陷阱/刀光、隔离测试配置、工具与新攻击 Skill；源在 Assets，发现副本不手改 | T01～T03、S04 |
| P9 | 本轮没有行为结果 | 全矩阵、正式入口与白盒、两种固定步长及不同帧率、正式模板说明 | 全矩阵 |

### 攻击请求如何进入真实 FSM

`EmberFsm.Attempts.cs` 的 AttemptInternal 先验证 activation 的宿主、节点和 generation，再核对声明句柄、请求位置和 target slot；只读求值基础条件及附加条件。`EmberFsm.cs` 的 Change 完成目标路径 CanEnter 和所有重定向预检，才执行 `site.Commit`，随后旧路径 Exit、新路径 Enter。拒绝或 NoChange 不调用提交；提交后异常按 Fault 清理，不自动退款。

后续攻击可沿 `Activation.AttemptGroup`/`Attempt` 进入，提交回调负责一次消费和明确交接，目标状态 Enter 只接收已确认状态，避免两个位置重复扣资源。仍需原型证明可选 Ability 缺装时不被全局校验误拒绝、动态目标只经合法绑定，以及跨自然阶段清理顺序。

FSM 的 Initialize 为每节点调用工厂并传 node.Id，拒绝共享运行对象；Dispatch 沿父到子遍历，成功转换后结束本次路径派发。**FSM Elapsed 仅在 Update 累加，不能直接当成固定步攻击时钟。** 新动作时钟及跨窗处理待 P0/P1 物理证据完成后定稿。

### 速度写入与物理查询位置

源码调用链：`PlayerModule.Drive(true)` → `Controller.FixedTick(Time.fixedDeltaTime)` → Sensors.Sample → Landing/JumpWindow/HeadJump/Wall/Ledge 观察 → Motor.BeginStep → Correction.BeginStep → VerticalMotion.Observe → FSM.FixedUpdate → Wall.Apply/Ledge.Apply/VerticalMotion.Apply/Correction.Apply → Motor.ExecuteStep → 动画同步/落地记录 → finally 输入过期处理。

Motor.BeginStep 从刚体速度建立命令；SetHorizontal/VerticalVelocity 写命令，ExecuteStep 才写 Rigidbody2D.linearVelocity 与 gravityScale。它不等于完成物理积分；Correction 还可能直接更新 Body.position，因此不能简单假设一次 FixedTick 内所有查询观察同一姿态。

拟验证接入点：Gameplay 会话以同一固定步推进参与者动作/运动，明确统一的物理姿态采样边界，再收集所有实体不可变候选，最后稳定排序结算。命中不能各实体边查边结算。**当前未实现协调器，也未证明自动物理模拟下该边界正确**；必须先测 Body.position、查询形状姿态、补正后同步及跨完整短窗口，才能确定最终位置。没有切换全局手动物理。

### 恢复与清理链

`PlayerModule.Recover` 走 `flow.RequestInPlace<GameplayState>(PlayerEntryRequest.FromSnapshot(...))`；PrepareReplacementAsync 保留原冻结方案，准备独立 PlayerRuntimeCandidate；ReplaceSession/Controller.CommitSnapshot 在整体暂停下交接，失败恢复原运动/动画/效果绑定。Capture 是底层保存入口，应在此补生命和临时动作限制。死亡确认必须保留可用输入通道；当前 UI Confirm 的存在不代表死亡流程已接通。

## 本轮验证与继续入口

| 层次 | 实际结果 |
| --- | --- |
| 静态核对 | 已读源码、磁盘配置、引用与测试源码；记录内相对链接缺失数 0；复核 1808 条原有改动路径，内容/删除状态变化数 0。未执行完整资产 Inspect 或编译校验 |
| FSM 逻辑测试 | 未运行；已定位 EmberFsmTests.SharedDefinitionCreatesIndependentObjectsPerNodeAndHost、ParentTransitionStopsOldAndNewBranchDispatch；EmberFsmRuleTests.RedirectPrechecksFinishBeforeCommitAndGuardDoesNotRepeat、EntryRejectionRespectsSiteContinuation；EmberFsmConditionTests.TargetPrecheckDoesNotReevaluateExpressionAndCommitsOnce |
| 原移动/恢复回归入口 | `Assets/Game/Module/Player/Tests/` 下 PlayerStateTests、PlayerFoundationTests（partial）；PlayerDashActionTests、PlayerVerticalMotionTests、PlayerWallActionTests、PlayerRuntimeCandidateTests、PlayerFormalRecoveryTests 等文件；本轮均未运行 |
| PlayMode 物理原型 | 未执行，阻塞 P0；需覆盖共享绑定、父转换终止、姿态可见点和同批收集边界 |
| 正式入口 | 未执行；应从正常 Play → Main → 新游戏/继续验证准备链 |
| 白盒 | 未执行；复用 `Ember/Player/玩家开发工具` 与 PlayerDevelopmentEnvironment，不直接以孤立场景替代正式入口 |
| 验收编号 | 上表仅为改造追踪，C/I/A/R/D/H/F/L/S/T 均没有本轮通过证据 |

上表为首次执行记录；续跑结果见下节。P0 退出标准满足后自动推进 P1，无需再次确认既定玩法或逐阶段授权。P6 真实死亡/无敌/运动的全链验证仍按方案留到 P7，不倒置依赖。

## 2026-10-14 MCP 重连续跑

### 实时查询与草稿保护

用户明确要求重新检查后，资源与 execute_code 查询恢复成功。Application.dataPath 为本仓库 Assets；IsTemplateActive("platformer-2d") 返回 true；选定方案为 PlayerDefaultScheme；UseWhitebox 为 false。FrameworkScene 是唯一加载场景且 isDirty=false。Physics2D.simulationMode 为 FixedUpdate，fixedDeltaTime 返回 0.0199999921，autoSyncTransforms=false。

脏对象查询发现 19 个玩家/FSM 内存配置克隆，未将其当作无草稿或擅自销毁。已在 PlayMode 测试前保存类型、名称、hideFlags、资产路径和 EditorJsonUtility 序列化内容至 [内存记录](platformer-combat-evidence/2026-10-14-p0/memory-before-resume.json)。这是诊断序列化记录，不承诺可单凭 JSON 自动恢复跨域的全部临时对象引用。开发环境 Status 返回“播放已停止；开发配置和存储服务已释放”。没有覆盖源方案、图或 Controller，没有调用 SaveAssets 保存全部脏对象。

一次备份查询因使用 GetInstanceID 被当前执行编译器拒绝（Unity 6.5 已标过时）。这是已成功连接后的查询代码编译错误，有明确错误返回，并非连接失败；移除该调用后备份成功。没有因此修改项目 API。

### 已完成的本轮测试

| MCP job ID | 模式与范围 | 实际结果 |
| --- | --- | --- |
| `8fe68cfb926447c3a29aa41a8935f7ab` | EditMode：共享 Definition 的节点/宿主独立；父转换终止 Update/Fixed 派发；重定向全部预检后一次提交；拒绝 continuation；附加条件一次求值 | 8/8 Passed，失败 0，跳过 0，1.224028 秒 |
| `dacc91c51e3246dea493eaa1fc22d8f0` | PlayMode：IdleMove_PreserveParents_AndApplyFacingAndSpeed、GroundJump_ShortCircuitsOldAndNewChildren_AndLaunchesOnce、DashInput_ShortPressSurvivesUpdatesAndConsumesOnceIndependentlyOfJump | 3/3 Passed，失败 0，跳过 0，0.3496701 秒 |
| `be8e54abd446466e91974a09ad96e806` | PlayMode：FullPowersAndTimedEffectsRoundTripThroughPublicRecovery、SnapshotCandidatePreparationAndDiscardLeaveOldHostUntouched | 2/2 Passed，失败 0，跳过 0，0.9185206 秒 |

最后一组请求还包含物理探针类，但返回清单仅有上述两个既有测试，因此**不将请求名当作执行证据**。13 项通过只证明 P0 原有契约，不表示新战斗验收矩阵通过，也不代表正式 Main 入口或白盒已经实播。

### 物理探针与新的阻塞

新增 [PlayerCombatPhysicsBaselineTests.cs](../../Assets/Game/Module/Player/Tests/PlayerCombatPhysicsBaselineTests.cs)，归模板 Player 测试程序集。两个 UnityTest 拟在 0.02 秒/目标 30 FPS 和 0.01 秒/目标 120 FPS 下，通过真正 FixedUpdate 回调驱动两个 EntityMotor2D，验证写速度不立即积分、下一固定步观察上一步积分、统一 SyncTransforms 后 Collider 查询看到收集姿态。测试保持 FixedUpdate 自动模拟，不调用 Physics2D.Simulate；finally 恢复步长/目标帧率并释放隔离场景。目标帧率不是实测渲染帧率，后续仍须记录实际时序；该探针也不替代 P6 的同批伤害结算测试。

第一次 refresh_unity 使用 if_dirty，返回 refresh_triggered=false、compile_requested=true；随后 Console 无错误，但新文件没有 meta 且类型反射返回 NOT_LOADED。纠正早先“新探针编译完成”的进度表述：该次只确认已有程序集无报错，**未确认新探针编译**。

于是对新增文件执行 force/all 刷新，返回 refresh_triggered=true、compile_requested=true。随后 editor/state 返回：`Unity session not ready for 'get_editor_state' (ping not answered); please retry`。同一批调用中随后 Console 返回 0 条错误，但它不能推翻连接失败或证明新脚本编译完成。没有继续重试或运行测试，也没有用其他编译途径替代。

阶段状态：P0 实时查询及上述逻辑/PlayMode 基线已通过；新探针实现已落盘，编译与运行未确认；同批收集接入的最终物理边界尚未放行。P1～P9 未开始。

继续入口：重新确认 MCP 可用后，先有界确认新脚本已导入编译，再以两个测试的完整方法名运行探针并核对结果清单。补齐实际时序证据、确认统一查询/收集边界后完成 P0，再创建独立的“通用攻击模板1”并推进 P1。原用户改动 1808 条路径在本轮末再次复核，无内容或删除状态变化。

本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；如果仍有报错，请提供首条编译错误及完整堆栈。手动编译成功也不能替代 P0 缺失的运行和物理时序证据。

## P0 完成与 P1 开始

用户再次继续后，editor/state 返回 ready，Console 错误数为 0。以完整方法名运行物理探针，MCP job `0bf0bcdf2d614df7a33a906680a26aac`：2/2 Passed，失败/跳过均 0，总耗时 0.4549479 秒。新测试类型实际编译并执行，因此上节的新探针编译/执行阻塞已解除。

已验证两实体自动模拟下 Motor.ExecuteStep 只写速度、下一 FixedUpdate 看到上一步位移、SyncTransforms 后 Collider 查询与刚体姿态一致。P6 接入契约为：会话在同一固定步推进所有参与者动作/运动（含受控补正），统一同步姿态，使用各实体本步确认姿态及前一采样姿态进行区间采样/扫掠，全部候选收集完后才稳定结算；不把写入速度后的预测位置冒充已经积分的位置，不在每个宿主的查询内立即结算。跨短窗及真实伤害账本测试仍属于 P6，反应/死亡集成属于 P7。

P0 退出标准已满足；测试使用的目标渲染帧率不等于实际帧率证据，P9 的多渲染/物理比率验收继续保留。正式 Main 入口与白盒战斗体验尚未执行。P1 按“通用攻击模板1”的独立配置与结构所有权开始，不重建原图。

## P1 数据配置增量与测试连接阻塞

### 已落盘的部分实现

- 新增 `PlayerAttackProfile.cs`、`PlayerHitWindow.cs`、`PlayerCombatConfiguration.cs` 和 Entity 的 `EntityHitPayload.cs`：攻击总开关、段启用、Ability ID、取消节点、收刀时间、Combo 目标、冲刺接续配置、形状关键帧/载荷，以及生命、反应与恢复纯值配置。时间和伤害初值仅为实现/测试起点，尚非正式示例定值。
- 增量修改 `PlayerFeelProfile`、`PlayerScheme`、`PlayerConfig` 与 `Editor/PlayerSchemeWorkflow`：攻击配置复制/冻结，完整方案战斗字段校验；拒绝已有方案的空配置组，重复捕获冻结装配时保留其生命与 Combo。普通空白副本关闭攻击。
- `PlayerInputFeel` 增加攻击、战斗冲刺和战斗跳跃请求有效期，并采集至只读手感；尚未接入运行输入，接入与消费归 P2。
- 新增 `Editor/Tests/PlayerCombatConfigurationTests.cs` 五项测试：嵌套几何/载荷冻结隔离、损坏 Polygon/Ability 定位、未装配可选 Ability 与悬空 Combo、生命重复冻结/空组拒绝、旧移动方案与输入冻结。只创建并销毁测试自己的临时对象，无全局 Undo 清空。

关联验收：C01～C04、C06～C08、S01 的配置部分；仅表示实现与测试追踪，**没有新增验收通过项**。尚未完成结构独立化事务、带归属的战斗节点生成、图/Combo 一致性、必需/可选动画规则及保存重载验证；“通用攻击模板1”资产尚未创建。不得将当前数据骨架称作可运行战斗方案。

### 实际验证证据

本轮 editor/state 成功返回 ready，非 Play、非编译。force/all refresh 返回 refresh_triggered=true、compile_requested=true；随后 Console 查询成功返回 0 条错误。这不足以证明新增测试类型已编译并完成行为验证。

以五个完整方法名发起 EditMode 测试时，返回 success=false：`Unity plugin session c6cd3eac-9389-45cd-b0b0-8395886370bb disconnected while awaiting command_result`。未返回 job ID 或测试清单；五项结果均为未验证。依用户与仓库规则立即停止自动编译/测试，不重试、不开替代编译流程。没有进入 PlayMode、正式入口或白盒验证。

静态检查：`git diff --check` 返回 0（附既有文件换行提示），只覆盖 Git 跟踪差异，不能覆盖所有未跟踪新文件，更不能代替 C# 编译。PlayerFeelRuntime 构造调用静态搜索只有 Feel.Capture 一处，签名已同步。新增脚本 meta 已由 Unity 生成，但导入不等于编译/测试通过。

重新核对 P0 的 1808 条原改动路径：仅上述五个任务相关既有文件（PlayerSchemeWorkflow、PlayerConfig、PlayerFeelProfile、PlayerInputFeel、PlayerScheme）发生本任务增量修改，其余 1803 条内容/删除状态未变。已有图、Controller、场景与方案资产未修改。P1 开始前的相关源文件副本保存在 `platformer-combat-evidence/2026-10-14-p1-before/`，不用于覆盖回退用户内容。

下一步：用户要求续跑后重新确认 MCP，确认新增类型实际编译，再运行以上五个完整方法名并核对实际结果；完成 P1 剩余结构与保存重载契约后才进入 P2。P6/P7/P9 的后续集成测试继续留在对应阶段。

本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；如果仍有报错，请提供首条编译错误及完整堆栈。

## P1 重连续跑：配置测试通过与结构复制

本轮重新查询 editor/state 成功，项目路径匹配，Console 错误数 0。execute_code 确认 `Game.Player.PlayerAttackProfile` 与 `Game.Player.Editor.Tests.PlayerCombatConfigurationTests` 类型实际加载。MCP job `42d17e57a89b4e7fa19d9b61212046a7` 完整执行上节五项配置测试，5/5 Passed，失败/跳过 0，耗时 0.1703054 秒。上一轮新增配置编译及这五项测试的阻塞解除；这不代表 P1 全部退出标准通过。

新增模板工具 `PlayerSchemeStructureWorkflow.CopyIndependent`，在新路径创建方案、独立图/Definition 与 Controller，保留节点 ID/父子关系/目标绑定，普通参数复制继续使用原工作流。先校验路径、完整方案与 Controller 保存状态；不读取磁盘旧副本覆盖 Controller 内存草稿；异常仅删除此次创建路径。当前复制已有 Definition 的节点间引用会重映射，未声称任意外部自定义配置闭包已经独立。新增两项结构测试待执行。

公共 API 缺口依据：`EmberFsmBlueprintWindow` 的 LayoutKey/CurrentLayoutKey 为私有，布局仅存于按图 GUID 区分的 EditorPrefs；AssetDatabase 图复制无法继承。新增框架 Editor partial 文件 `EmberFsmBlueprintWindow.LayoutCopy.cs`，只公开 `CopySavedLayout(source,destination)`，按稳定 ID 复制已保存布局与视口，拒绝覆盖目标布局，不修改源图或提交源窗口中的预览。模板工具调用该 API，框架不引用 Game.Player。布局复制尚待行为验证。

本轮结构脚本 force/all refresh 已被接受，随后 editor/state 返回 success=false：`Unity session not ready for 'get_editor_state' (ping not answered); please retry`。立即停止自动编译和测试，未按 hint 重试，没有替代编译。新增结构代码编译、两项结构测试、布局行为均为未验证；此前五项配置测试的实际通过证据保留。尚未创建“通用攻击模板1”，P1 仍未完成，P2～P9 未开始。

下一次续跑：确认 MCP 与结构类型实际加载，先检查 Console，再执行 `Game.Player.Editor.Tests.PlayerSchemeStructureWorkflowTests.IndependentCopyPreservesIdsBindingsAndOwnsStructure` 和 `ExistingDestinationRejectsWithoutCreatingPartialAssets`。随后补布局复制、完整保存重载、事务后段失败回滚、Definition 外部配置闭包隔离证据，继续实现带归属节点/图与 Combo 一致性；不能将两项基础结构测试当作全部 P1 验收。

本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；如果仍有报错，请提供首条编译错误及完整堆栈。

## P1 结构与布局验证续跑

重新连接后 editor/state ready，Console 错误数 0；执行查询证实结构工具、CopySavedLayout 方法及结构测试类型已实际加载。job `9adceda3d6bb456c9d1105308ceb14a8` 两项结构测试 2/2 Passed（7.0579986 秒），但输出出现 `PlayerSchemeSaveProtection.IsOpenForEdit` 参数数量预期 2、实际 3 的诊断。未将此诊断忽略：先备份 PlayerSchemeLibrary.cs 到 P1-before，再将回调改为两个参数。

通过 execute_code 在本次新建的临时图与对应 GUID 的 EditorPrefs 键上验证布局：坐标 (137,-42)、固定/折叠标记、缩放 .75 正确复制；重复复制被拒绝、源值未变、新节点未排位。finally 清理本次临时资源与键。首次执行被工具通用 DeleteAsset 模式检查拦截（未执行），仅在确认清理目标为本次 GUID 临时目录后，使用工具公开的 safety_checks=false 执行同一隔离探针；不是 MCP 连接失败。

上述布局验证已加入持久测试；结构测试补强制 ImportAsset 后的完整 Validate。修正后的 job `a0afbb041b49499d916369ad794ccdbf`：3/3 Passed，失败/跳过 0，11.9357457 秒，全部 output 为空，之前回调诊断不再出现。覆盖结构隔离、路径冲突无部分创建、保存/重新导入和已保存布局复制；仍不等同于 C05 完整战斗节点生成验收。

继续发现参数 Restore 仅允许相同 Graph 引用，与独立副本工作流不兼容。已改为节点 ID/Definition 类型/父子关系/绑定及动画目标兼容性预检；恢复配置时保留目标图、Controller 和 Key 引用，不恢复结构。新增 `ParameterRestoreKeepsIndependentStructureAndSupportsUndoRedo`，只清理测试拥有对象的 Undo，不清空用户全局 Undo。此增量的编译与测试待确认。

随后该增量编译完成，Console 错误数 0；job `938bb6e02b12467ba50da1558d761da6` 1/1 Passed，6.1798612 秒，output 为空。独立图参数恢复与 Undo/Redo 的阻塞解除。

继续新增 `PlayerCombatGraphMap` 与 `PlayerCombatGraphBuilder`：前者仅保存攻击段对应的攻击/收刀节点身份，不重复存连招关系；绑定从 Combo 推导，已开启攻击但缺节点、角色声明不匹配、未登记节点或绑定不一致时明确拒绝。后者只创建内存候选，不提交源图；保留已有非战斗节点/绑定和已拥有战斗节点 ID，拒绝命名冲突及隐式删除，使用真实 Graph.Inspect 预检。方案编辑校验及冻结捕获接入同一契约，节点归属数据纳入深复制。测试 `CandidateOwnsOnlyCombatNodesAndDetectsComboDivergence` 使用 CreateState 必然抛错的隔离 Definition，以验证无运行工厂副作用。上述新增契约的编译/测试仍待确认；尚未把候选与动画/Controller 联合提交，也没有实现或安装正式攻击运行状态，不代表战斗方案已可运行。

候选生成测试首次编译发现 CS0122（测试程序集无法访问 internal 生成器），已通过 Editor/AssemblyInfo.cs 对测试程序集开放 InternalsVisibleTo 修复，没有将生成器升级为公共 API。重编译后 Console 错误数 0；job `a4102b14357d496ba9724c3631c60d36`：候选/Combo 分歧测试及旧移动配置回归 2/2 Passed，0.0721062 秒，output 为空。

随后补 Definition 可变 ScriptableObject 依赖的递归独立复制：同一次引用共享只复制一次，节点间引用及源图引用重映射，外部图/完整 Scheme/Config 的反向依赖显式拒绝，Clip 等美术引用保留。新增 `DefinitionDependenciesAreCopiedOnceAndRemainIndependent` 验证同一外部效果的双引用保持别名且与原效果独立；该增量刷新已接受，编译与测试待确认。

上述递归复制编译完成，Console 错误数 0；job `1b00113da5d74d96931e27ecc1a14dac` 1/1 Passed，13.6571958 秒，output 为空。

新增可选收刀动画解析：只有 IPlayerCombatNodeDefinition.Role=Recovery 的节点允许 Clip 缺省，且必须存在有效 idle 槽 Clip；攻击本体及其它节点缺 Clip 仍显式拒绝。AnimationSession 构造覆盖表复用 ResolveClip，不将空值交给 AnimatorOverrideController。新增 `OptionalRecoveryClipResolvesIdleButAttackClipIsRequired` 测试；编译与结果待确认。动作速度/表现变体的播放实测仍在 P3，未提前标记通过。

工作区复核发现 ProjectSettings/EditorSettings.asset 的 m_EnterPlayModeOptions 从 P0 的 1 变为 0（当前与 HEAD 一致）。没有回放 HEAD 或覆盖该文件。只读检查 MCP TestRunnerService 确认工具具有临时修改/恢复逻辑，但不足以证明这次变化来源；已异步询问用户是否主动调整，暂保留当前设置。其余变化为任务相关 Player 源码增量；原场景、图、Controller、两套内置方案未修改。

可选收刀 Clip 回退增量的 force/all 刷新被接受后，editor/state 返回 success=false、`Unity session not ready for 'get_editor_state' (ping not answered); please retry`。已停止自动编译/测试，无重试或替代编译。该最新改动与 `OptionalRecoveryClipResolvesIdleButAttackClipIsRequired` 未验证，不能据此放行 P1。先前各项实际 Passed 证据保留。

用户随后明确回复“不是，恢复 P0 设置”。已仅将磁盘 m_EnterPlayModeOptions 从 0 增量恢复为 1；MCP 失联后没有再次调用 Unity，内存实际设置及是否同步磁盘留待下次连接确认。

下一次续跑：确认 MCP、最新动画类型编译与 Console，运行可选收刀 Clip 测试，并继续完成图/Combo/Controller/动画映射联合提交与保存重载/Undo/Redo/失败回滚。候选生成已验证，但尚非正式结构编辑事务；“通用攻击模板1”仍未创建。C01～C08、S01 的已测配置子项不能等同于全部行为验收；P3/P6/P7/P9 的运行集成保持待后续验证。

本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；如果仍有报错，请提供首条编译错误及完整堆栈。

## P1 动画回退通过与联合提交事务

本轮重新连接成功。实时 EditorSettings.enterPlayModeOptionsEnabled=true、enterPlayModeOptions=1，已与 P0 同步；ResolveClip 方法实际加载，Console 错误数 0。job `01190d96fa524ac38967947ae4cb7f91`：`OptionalRecoveryClipResolvesIdleButAttackClipIsRequired` 1/1 Passed，0.766796 秒，output 为空。此前动画回退编译/配置测试阻塞解除；运行视觉表现仍待 P3。

新增 `Editor/PlayerCombatStructureTransaction.cs`：要求已保存的独占方案/图/Controller，先创建本次专有临时候选目录，组合图、Combo、攻击参数、Controller 与动画映射进行完整校验，再进入目标 Undo 事务。既有图/Controller 身份保留，只为新增节点建立状态和独立 Key；图生成器不隐式删除。目标改动不自动保存，调用方可按普通保存流程保存。临时候选及其 Undo 记录在 finally 清理，不清空用户全局 Undo。候选与实际提交都通过同一完整方案校验，异常回退本次目标 Undo 组。

新增 `JointCombatCommitRejectsMissingClipThenSupportsUndoRedoAndReload`：先验证缺攻击 Clip 不改图及总开关，再验证联合提交、Undo/Redo、保存及重新导入。测试用 Definition 拆为独立文件并显式序列化角色，以便复制和重导入保持配置。该事务与测试目前仅已落盘并发出 force/all 刷新，尚未确认编译和运行，不提前标记 C05/C06 通过，也未创建正式方案。

联合事务编译后 Console 错误数 0，但 job `28c8602fb781425092a355d6739b81a1` 失败，提示 `Sequence contains no matching element`，工具未返回完整 result。随后仅通过 MCP execute_code 调用相同测试并执行 SetUp/TearDown，取得堆栈定位至 ExtendAnimation 第 64 行的状态路径匹配。映射原先用请求 node.Id 拼接，已改为 Unity AddState 返回的实际 state.name，避免 Animator 名称处理后路径不匹配。修复后的编译/完整事务复验待确认；失败项没有跳过。

状态名称修复编译后 Console 0 错误，job `1f14fb0776734ebc82377d59887862a9` 仍失败，改为完整方案引用缺失；通过 MCP 重现堆栈定位至联合事务 Validate。临时克隆 Scheme 的 JSON 局部覆盖探针表明 Graph/Feel/Powers/Animation/ActionEffects 引用均保留，因此尚不能把问题归因于 JsonUtility。已增补候选/提交阶段和具体缺失字段的错误定位，正在查明事务中的引用生命周期；未创建正式方案，未放行 P1。

增强诊断后通过 MCP 重现确定缺失的是候选准备阶段的 Graph（目标提交尚未开始）。持久图引用探针不能证明临时候选图引用同样安全。为消除对 Unity 对象进行 JSON 局部覆盖的引用副作用，联合事务改用 PlayerScheme/PlayerFeelProfile 的 internal 纯值配置赋值入口，仍深复制 Combo/Map/Attack；不重写 Unity 对象其它字段。该修复已请求编译，候选引用问题是否解决仍待实际复验，不预先宣称根因已完全确认。

纯值赋值调整编译后问题仍可重现。进一步 MCP 隔离探针（仅本次新建临时方案，finally 删除）证实：对持久 Scheme 使用 SerializedObject 赋值 DontSave 临时 Graph 后，serializedReference=false；同一对象直接字段赋值后 directReference=true，graphAlive=true。因此根因已定位到候选图引用赋值方式。新增 internal SetPreparedGraph，仅用于隔离候选预检，替换此处 SerializedObject 赋值；不把临时图保存为正式资产。修复复验待确认。

该修复后 job `d29abbebaf19418fa1cdab769f5278e2` 已通过候选/实际提交与 Undo 前置断言，但在 Redo 后攻击 Enabled 应为 true 的断言失败（MCP 重现堆栈定位测试第 95 行）。未改弱断言。已将临时候选的 Undo 清理/资源删除移到目标 Undo 事务启动前，并在目标操作结束时 FlushUndoRecordObjects，避免候选清理干扰目标事务；是否解决仍待复验。尚无联合事务通过结果。

job `d87a7a4a598942e4a08c4b419f7e1138` 仍在布尔断言失败。原测试对提交后启用状态缺少即时断言，已补提交后 Attack.Enabled 与归属数量断言，避免误将更早的值丢失归为 Redo。目标纯值赋值移到动画资产扩展操作之后；同时修复关闭攻击且空归属清单时直接返回的校验漏洞：有战斗节点仍必须登记归属。上述增量待编译/复验，不宣称 Redo 已修复。

job `bf8d560009134901bc6dd0f84a9df90a` 证实提交后即时状态正确，失败确在 Redo。进一步调整 Undo 登记顺序：新增子资产的 RegisterCreatedObjectUndo 完成后，紧邻纯值/图赋值登记完整快照；动画映射在最终应用序列化值前单独登记。避免在创建对象触发的 Undo 刷新之前过早登记所有配置。该调整待复验；目前 P1 的联合提交退出标准仍未满足。

job `2aa7b155f3ee48a8a2dfcf7c19819ee5` 首次通过完整联合测试：1/1 Passed，15.8706823 秒，包含提交、Undo/Redo、保存及重新导入。输出含四条 `'.' is not allowed in State name`，没有忽略：创建 Animator 状态前将点替换为下划线，并仍记录实际 state.name；FSM 节点 ID 不改。

进一步补提交阶段失败回滚：测试 Definition 可显式在复制时改变角色，使原件预检通过、目标提交校验失败；断言原图 JSON、攻击关闭及 Controller 状态数恢复，然后继续正常提交/Undo/Redo/保存重载。此为测试专用故障，不在生产工具加入绕过校验的钩子。新故障覆盖及名称诊断修复待复验。

job `0c08073037a64c3a8460b35d05892251` 证实故障回滚恢复了内容，但 Controller 残留 dirty 标记使下一次事务被草稿保护拒绝。修复为记录原本干净对象的 JSON，回滚后仅在内容完全相同时恢复干净标记，不清除原有草稿。MCP 反射确认当前 EditorUtility.ClearDirty API。重编译后 Console 错误数 0，job `7d24d0b2560545118870fec56898d9bf`：1/1 Passed，21.9264714 秒，output 为空，包含提交阶段失败回滚、再次提交、Undo/Redo、保存重载；状态命名诊断也消除。

继续补联合提交中新增 Definition 的可变依赖递归复制，复用结构复制的引用闭包逻辑；测试加入攻击/收刀共享同一效果源，验证目标独立且内部共享别名不丢失。该增量已刷新，待编译/复验。C05/C06 的上述事务子项已有证据，完整 P1 及正式方案尚未宣布完成。

## P1 收口与 P2 入口

- 新增依赖闭包 job `161427e62d1d4740a3aeb9b62fb680ce` 1/1 Passed，21.5394223 秒，无输出诊断。
- 联合测试再补真实 PlayerSchemeCapture 和二次捕获：修改源攻击/Combo/归属后冻结数据不变。编译 Console 0 错误，job `cabc53701c9d44a89beb87907635e404` 1/1 Passed，17.172594 秒，无输出诊断。
- PlayMode job `4357e700ff604075a1cf0aea05618d96` 3/3 Passed，6.4511382 秒：空洞移动速度 8、跳高 3.0400；蔚蓝 7.5、2.8453；空白蔚蓝 7.5、2.8125，均经历 fall、着地，冻结主机互不污染。测试后实时 Play Mode 设置仍为 enabled=true/options=1。
- 已通过正式 CopyIndependent 创建 `Assets/GameResource/Resources/Config/Player/BuiltinSchemes/通用攻击模板1.asset`、独立 `GeneralAttack1Graph.asset` 与 `Assets/GameResource/Player/GeneralAttack1.controller`；来自空洞骑士风格，21 节点，移动速度 8。目前是待接攻击的基础资产，攻击关闭，未修改默认方案或原两内置；在 P3 以真实运行定义联合启用，不安装测试夹具或放宽缺节点校验。
- 静态指纹复核原有 80 个资源/场景/Controller/EditorSettings：0 变化。git diff --check 无空白错误，仅既有行尾提示。
- P1 覆盖 C01～C08、S01 的配置、静态校验、复制所有权、冻结、布局及事务基础；合法未装配 Ability 可通过配置，实际动作拒绝与收刀播放仍待 P2/P3 集成，不提前标记运行验收。正式入口、白盒体验和战斗全矩阵均未执行。
- 下一步 P2：输入版本/意图/机会截止、固定步动作时钟与父级控制权；用隔离测试节点证明拒绝零消费及优先级，再推进 P3 真实三连击。

## P2 输入与时间基础（实施中）

应用 ember-platformer-add-state 技能前，正式门禁发现技能归属与编辑身份不一致。只读核对确认仍为 platformer-2d 0.3.1，正式 PreviewCurrentTemplateSkills 显示三个平台技能身份更新及 tune-feel/references/parameters.md 一项源同步，NeedsBackupConfirmation=false。通过正式 SyncCurrentTemplateSkills(preview,true) 完成技能专用事务，门禁返回空；没有切换/加载/保存模板，没有手改身份 hash 或发现副本。此处同步不代表 P8 新攻击技能已完成。

新增 PlayerCombatRequests：请求版本、按下时意图、输入与机会双截止、自然阶段延续不刷新、提交清旧请求；新增 PlayerActionClock：固定步攻击倍率时间与跨窗区间。PlayerInputAdapter 可选接攻击输入，仅战斗上下文用专用冲刺/跳跃缓存，普通移动保持既有逻辑；PlayerRuntimeCandidate 从冻结配置绑定攻击。已新增五项纯逻辑测试（尚待执行）。P2 控制权、真实 FSM 优先级/拒绝消费和虚拟输入集成尚未完成；不能放行 P3。

本次 P2 增量已请求 Unity 刷新，编译响应仍待返回，未将其计为通过。原输入/候选文件增量前备份位于 platformer-combat-evidence/2026-10-14-p2-before。

随后刷新成功、editor/state ready、Console 0 错误；job `266b033afd60442f8c5cf468e2a4798b` 五项请求/时间逻辑测试 5/5 Passed，0.7919973 秒，output 均为空。覆盖 I04～I07/T01 的基础边界，不等同于真实攻击 FSM 行为通过。

继续新增 PlayerMotionControl 的显式职责占用，并接入父级水平/朝向、ground/air 自动转换、墙/挂边/竖直/补正后处理；强制重落地/翻滚仍由根级规则处理。新增虚拟键盘缓冲/消费/通道关闭测试与真实玩家父级接管测试，当前正等待此增量刷新结果。真实 FSM 多候选优先级测试尚未完成。

该批刷新后 editor/state ready、Console 0 错误。静态复核测试夹具实际默认开启 .12 秒跳跃缓冲，修正控制权测试为“父级拒绝后请求保留且未消费”，没有修改玩法参数。随后加入隔离 PlayerCombatArbitrationProbe 与五组实际 EmberFsm<PlayerContext> 请求竞争用例：冲刺优先、冲刺门未开放、目标预检拒绝继续、多个拒绝后攻击/移动胜出、仅胜出者提交一次。夹具使用真实输入适配器，仍不是正式攻击入口实测。

该最新测试增量 force/all 刷新成功返回后，editor/state 首次查询返回 success=false：`Unity session not ready for 'get_editor_state' (ping not answered); please retry`。立即停止后续 Unity 调用，没有重试、dotnet、BatchMode 或日志轮询。最新代码编译未确认；PlayerCombatInputTests 的 7 项用例及 PlayerStateTests.ExplicitMotionControlBlocksParentJumpHorizontalAndFacingUntilReleased 均未执行，P2 未通过，P3 不放行。修改输入与公共运动路径后的纯移动 PlayMode 回归也仍待执行（此前 3/3 是 P1 收口时证据）。

续跑入口：先重新确认 MCP、实时 options=1、最新编译与 Console；执行上述 8 项 PlayMode 用例及三组 CompleteSchemeWorkflow 回归，修复实际失败后再核对 P2 完整退出标准。之后 P3 才能用真实攻击/收刀状态接入通用攻击模板1、启用攻击并完成三连击；不将当前关闭攻击的基础资产误认为最终攻击方案。

## 2026-10-15 P2 通过与 P3 实施

重新连接 editor/state 成功（首次状态年龄提示 stale_status，非连接失败），Console 0 错误；execute_code 证实 optionsEnabled=true/options=1、最新 PlayerCombatArbitrationProbe 类型已加载、add-state 技能门禁为空。PlayMode job `d68f489c571642d4afdaf308db3a0b24`：11/11 Passed、0 失败/跳过、5.4267816 秒。覆盖 5 组实际 FSM 优先级/预检拒绝继续/仅胜出提交、2 项虚拟键盘缓冲与清理、1 项真实父级控制权、3 组完整方案纯移动回归。P2 的 I01～I07/T01 基础与控制权退出标准通过；实际攻击取消和正式入口仍待 P3 集成。

P3 新增宿主独占 PlayerCombatMotion、无可变配置的攻击/普通收刀 Definition 和状态、独立战斗根声明及转换组。按节点身份查询冻结参数；最终提交才消费输入/Ability，末刀收刀清本体旧缓存；本体锁向、站桩/移动运动、环境变化和普通收刀时钟接入控制权。图生成器接受显式战斗根定义并保留原根节点 ID/非战斗绑定，不修改旧移动方案图。新增重起手/空跳绑定与入口一致性校验，动画速度乘本体攻速。已请求编译；仍未创建正式招式配置、启用攻击或运行 P3 测试，不提前标记完成。

上述运行代码与 PlayerCombatExampleWorkflow.InstallThreeHit 编译完成，Console 0 错误。PlayMode job `99880ead28c648fa98c918d6ff0876a1` 2/2 Passed、9.7178745 秒，output 为空：三段提前接续、本刀锁向/下一刀取最新方向、末刀旧缓存清理/收刀新按起手、缺收刀 Clip 仍走逻辑收刀，以及跳跃等待取消节点后提交一次并释放控制权。测试用临时独立方案与 Idle 占位 Clip，无真实伤害或最终美术。

已对通用攻击模板1调用同一 InstallThreeHit 并普通保存：Attack.Enabled=true，段 first/second/third，27 节点、独立图与 Controller；基础 Ability 为 basic-attack（默认开启、不限次数）。三段目前显式使用 idle 占位 Clip，窗口集合为空，不能称伤害示例完成。原默认方案未切换。生产输入资产只追加 Attack/J/手柄西侧按钮，保留原 Action/Binding ID；落盘前实时确认输入资产不脏、没有打开的 Input 编辑窗口。修改前基础方案/图/Controller/inputactions 已备份至 platformer-combat-evidence/2026-10-15-p3-before。

继续补站桩移动取消/环境丢失、合法未装配 Ability 拒绝、关闭攻击运行独立移动图测试；并在跳跃/冲刺成功及断连退出时清理旧战斗请求。该增量已请求编译，结果待确认。动画收刀移动变体复用既有 move 目标并从头播放，仅切表现不重置逻辑计时；真实 Animator/正式入口与保存重载验证仍待补，P3 尚未完成。

上述增量随后编译确认 Console 0 错误。PlayMode job `86154e708c534d178222e0f595f5ead5` 5/5 Passed，25.5771875 秒：前述两项，加站桩移动取消/环境丢失、合法未装配 Ability 拒绝、关闭攻击独立图运行。job `0a54675a5d874f91b9f5b0aa4f0d5639` 1/1 Passed，5.2399032 秒：真实 Animator 攻速、暂停恢复、普通移动收刀变体、源参数修改不影响冻结计时。输出均为空。

EditMode job `248482747bda42d9bc1ffe004e619d8c` 6/6 Passed，30.9232466 秒：结构所有权/依赖复制、冲突路径拒绝、联合提交故障回滚/Undo/Redo/保存重载、参数恢复、布局复制。实时查询 optionsEnabled=true/options=1；正式三连击方案、图、Controller 均非 dirty，27节点且攻击开启。

核对第6.4节后新增可选 MovingRecoveryClip（仅允许收刀节点、沿用片段曲线与事件校验），收刀片段播完后表现回 Idle，专用移动变体复用本节点视觉目标；不修改逻辑节点/计时。新增真实 Animator 测试，最新增量已请求刷新，编译及测试待确认。覆盖 A01～A05/A07/R01～R02 的部分机制，正式入口/白盒及最终美术、伤害仍未验证，P3尚不放行。

动画增量的 jobs `7714f73b7b4e4b50a949f4f2298c7ebe`、`123abaca98924836aea0e9f28dd85834`、`16e6bd6295e84acc9469ba62364b4efd` 均复现缺省收刀覆盖键回退错误，未弱化断言。更换动态覆盖赋值方式不足以修复；最终改用 Unity 对象空值判断替代 `??`（序列化空引用可能保留托管占位对象），并保留完整覆盖列表。重新编译 Console 0错误，job `7b067e556e044744b3014eaa1422edc2` 2/2 Passed，10.0780082秒，无输出。覆盖专用移动收刀、播完回Idle、缺省回退、暂停与攻速、逻辑计时连续。

普通接续显式使用 ReenterTargetBranch，使合法自循环目标重新进入而非无操作。job `8a39ca6b060b4449aa79c23dad16ec2b` 2/2 Passed，10.1114596秒，无输出：自循环重置本刀时钟/方向/消费，三连击回归。动画生成新槽显式清空专用移动Clip，避免数组复制尾项误继承。

正式方案/独立图/Controller 所有子资产均确认非 dirty 后进行 ForceUpdate 重导入，PlayerSchemeWorkflow.Validate 通过，Attack.Enabled=true、27节点、Gameplay/Attack可解析，实时options=1。P3退出标准通过；覆盖 A01～A05/A07/R01～R02 的本阶段行为，C05/C06保存重载和结构回归；伤害、最终素材、正式入口及白盒验收按P6～P9安排保留未验证。P4现在开始，使用隔离配置，不增加正式示例招式数量。

## P4 特殊收刀与停顿派生（实施中）

新增两种声明复用既有攻击/收刀运行状态。仅启用停顿连接的段生成独立特殊收刀节点，图归属与绑定显式校验；联合事务复制新声明并扩展独立动画目标。普通三连击资产无需迁移或新增节点。运行时在本体自然结束依据段启用与 Ability 装配/Enabled/IsOpen 选择收刀，次数不足仍可选特殊收刀；选择后不随开关变化切换身份。特殊收刀分别使用普通/总截止，普通请求不转换成停顿请求。配置校验禁止特殊收刀压缩普通接续窗口。

方案新增可选 DelayedAttackPrompt，捕获为不可变 EntityEffectSpec，二次捕获保留；依赖效果容器和 Owned 生命周期，关闭容器显式报错。提示仅依据当前特殊窗口和目标资格，不要求输入；持有独立句柄，不驱散其他来源。新增4项隔离PlayMode用例，模拟第二刀普通第三刀/停顿特殊刀及后续连招、开关变化、次数不足/补充与提示归属。最新编译 Console 0错误，job `0147696f545f4e4db8ee56d009e54f4e` 执行中，尚不宣称P4通过。正式方案尚无停顿美术或额外招式，符合隔离验证范围。

P4 job `0147696f545f4e4db8ee56d009e54f4e` 4/4 Passed，31.6822653秒，output为空。新增边界/冻结 job `4468fed70f7942769e999c2e0d9f74c4` 2/2 Passed，7.4688155秒：普通请求不跨停顿窗口、过期特殊请求不因能力恢复复活、总截止退出并清提示、新按环境起手；提示容器依赖、生命周期、二次捕获及不压缩普通窗口校验。EditMode job `5c5ca5367bc04b2d96ef1dffb9c285a9` 配置/图/联合事务回归13/13 Passed，22.8727126秒，无输出。P4退出条件通过，覆盖R03～R09及I06/I07相关分窗行为；正式白盒与最终表现仍留P8/P9。P5开始，计划复用真实冲刺结束原因，独立出招路由/收尾并分离接续截止与惯性运动，不改公共FSM。

## P5 冲刺续接与空冲惯性（实施中）

新增 PlayerCombatDashMotion、显式出招路由声明、独立收尾声明和战斗冲刺根声明。攻击提交到冲刺时携带普通下一段，冲刺内新请求建立独立机会；路由预检只解析已声明目标，最终提交仍经目标 CanEnter/Commit。两种模式共用环境解析，资源不足不触发环境回退。自然结束通过现有 EndReason.Completed 进入独立收尾，墙等非自然结束重定向普通运动；空中收尾恢复重力、按比例保留水平速度，机会截止后剩余惯性继续平滑交还。图事务显式增加路由/收尾及改接冲刺出边，冻结时复制有路由列表的 Definition。

首次编译发现路由 Definition 缺少抽象 GetTransitions 实现（CS0534），已补显式空转换声明；重新编译 Console 0错误。新增隔离测试与纯移动回归 job `d75b9aabcfa6414f8d0eb7944e186310` 执行中。普通落地保留余时、环境回退、墙中断、跳跃/再冲/强制动作断连及最终正式方案接线仍待验证，P5未完成，P6不放行。磁盘 Play Mode optionsEnabled=1/options=1。

job `d75b9aabcfa6414f8d0eb7944e186310` 7/7 Passed，37.1624238秒：两种出招模式、冲刺开放前短缓存/旧缓存清理、收尾移动转向保留下一段、空冲正常重力与跨截止惯性；纯移动3组保持 move=8/7.5/7.5、jumpHeight=3.0400/2.8453/2.8125。job `b422fa7233064739b329e1cdf498ea43` 的4用例中收尾跳跃触发FSM Faulted，其余无失败报告，未把整批记为通过。根因是收尾 Exit 清掉目标 Enter 待消费的跳跃请求；改为仅最终预检通过的跳跃/再冲 Commit 标记保留输入，仍清旧连招/惯性。job `494d57baa85e45a5bcdbba3b5a937c55` 2/2 Passed，15.1166317秒，验证收尾跳跃及再次冲刺消费/断连。

继续补环境不符后明确丢弃旧普通目标、碰墙时不先偷跑攻击；墙跳/踩头跳/下穿/挂边接管停止剩余惯性。路由候选预检改为只验证已配置声明，不修改传入 Definition。job `60cdf439244f4491bfefabd3a15da60f` 4/4 Passed，29.8532907秒：启用普通落地停顿且真实碰撞支撑时保留收尾时间、重落地/翻滚断连、目标关闭不降级、反向制动。输出均为空。追加末尾输入自然保留与停顿资格不随冲刺携带测试、正式 EnableDashFollowup 联合接线入口，已编译Console 0错误；job `2b5091decd6a40e9b7e79b40f9868f15` 待结果。P5尚未放行。

job `2b5091decd6a40e9b7e79b40f9868f15` 2/2 Passed，17.5847085秒，无输出；停顿资格不携带、有效末尾输入自然保留通过。正式方案/图/Controller全子资产确认无dirty后备份到2026-10-15-p5-before，通过 EnableDashFollowup 联合事务接入并普通保存；重导入验证通过，29节点、攻击开启、options=1。未切默认方案、未保存模板。

最终回归 job `d553e6a3d8b84113a8cced0278ab2622` 8/8 Passed，42.6778614秒：地空回退（冲刺内和收尾）、两种出招、墙中断及三组纯移动基线；补齐此前失败批次中未单独确认通过的用例。P5退出标准通过，覆盖D01～D12的本阶段机制；受击/死亡打断留P7集成，正式入口与白盒实测留P9。P6开始，模板层可选Entity生命/接收协议和统一结算，不引入全局CombatManager或框架反向依赖。

## P6 命中检测与结算（实施中）

新增模板 Entity 可选 EntityHealth、不可变 EntityHit/Result、接收器与 EntityHitBatch。批次用显式来源/攻击/窗口/目标键稳定排序；同批来源死亡不撤销已成立候选，目标注销/替换使旧候选失效；每击保护先读取，实际扣血后建立的新保护只影响后续命中。最初 Entity.Tests 使用内部构造器导致编译错误，已改测试为公开 Definition.Capture；EditMode job b719492dbc474704815f077e7b46b03b 返回0用例，不算通过。正确 PlayMode job `2e358ce6c3ba4ba0ae0843bfe5582b87` 4/4 Passed、0.1608068秒：伤害模式与非致死、无生命反应、新保护顺序/拒绝不刷新、换血/已死拒绝/目标替换。

新增宿主独占检测池（原生Box/Circle/Polygon）、固定步实际姿态扫掠、按攻击实例与窗口独立账本，接入攻击本体时间推进。已通过实际反射确认 Unity6 显式姿态 Overlap/Cast API。job `0cbed7a41ff2473bb6a1578e3813fadf` PlayMode 5/5 Passed、0.3720451秒：三种形状跨完整短窗口、同实体多Collider去重、查询期间不结算、身体接触排除不影响查询、两种拒绝重试策略。Console 0错误，实时 options=1。覆盖H01/H03及相关查询/结算契约；尚未安装生产场景驱动/沙袋，不代表正式伤害体验完成。

已新增独立 PreviewSceneStage 原生 Collider 编辑和显式应用 API，预览对象不使用运行身体；应用前检查源形状未被其他草稿改动，支持Undo而不自动保存。新增三形状原生编辑往返/主场景不污染测试。最新增量已请求编译，编译及测试尚待确认。换关键帧保持账本、共享驱动集成、效果目标隔离和沙袋基础接收仍待补齐，P6未通过；真实反应运动/死亡集成按计划留P7。

P6 编译确认 Console 0错误。EditMode job `b3893d6e65464c2781588ae80df93d47` 3/3 Passed、0.8791523秒：Box/Circle/Polygon经原生碰撞体编辑再捕获、偏移旋转往返、缩放误用拒绝、源数据与主场景dirty不变。新增PlayerHitScene由PlayerModule固定步统一收集后结算、原位绑定清旧候选保留世界生命；EntityHitActor为显式稳定ID的非致死基础沙袋并接收反应请求，完整运动按P7实现。默认效果绑定只收非Trigger身体碰撞体与明确角色Sprite，另提供EntityEffectTargets显式登记，不把刀光/检测范围纳入身体穿透/闪烁。

PlayMode job `b9fac24785884c7ca105a905be7bf31a` 4/4 Passed、6.1021136秒：关键帧换形状不重置账本、新窗口/新攻击独立；真实攻击FSM到场景批次伤害/反应请求及重新绑定保留沙袋血量；子实体、独立刀光Sprite、Trigger检测范围不受身体效果误改；PlayerModule整体暂停时钟回归。两项Foundation测试输出仅既有EventBus订阅日志，无错误。覆盖H02及H01～H08的部分结算、资源隔离基础；新增加的原生Stage应用/Undo/Redo/草稿冲突/退出恢复测试尚待编译与执行，P6仍未放行。

原生Stage用例早期 jobs `df91953e72a746c8963a121ce318c173`、`b8bed515f4e64cc680499dd3799d07f2`、`db29d7db5c4c44a3b0180346e24447d2`、`5f97de7bcc8b459ca1bfbe97bdd30eae` 未通过：关闭Stage后Test Runner临时场景dirty改变；`f21041715eec40028725dd7326383275` 的附加场景诊断因Runner已有未命名场景而拒绝创建，不计通过。逐点断言确认打开、应用、Undo、Redo都未污染，最终定位未落盘ScriptableObject夹具；用实际资产承载草稿后 job `8a17ed83dc1349598a30b46d74025fac` 1/1 Passed、1.9514878秒。入口明确要求已创建资产，仍不保存其未保存草稿；最后 job `d7a688538229455a99d810ebca8426c3` 1/1 Passed、2.078747秒，验证未创建资产拒绝及完整原生Stage生命周期、Undo/Redo、冲突与选择/场景恢复。实时确认实际FrameworkScene dirty=false、options=1，没有清除场景dirty或重载用户场景。

P6本阶段退出标准通过：原生异形编辑、实际物理扫掠、窗口账本、统一批次、基础沙袋伤害/反应请求、效果目标隔离均有本轮证据。覆盖H01～H08相关基础契约及E目标隔离（实际反应运动/死亡换血/无敌全链留P7；完整开发工具时间轴、正式素材和入口白盒留P8/P9）。P7开始；目前沙袋显式非致死且只登记反应请求，玩家生命/反应/死亡恢复尚未接入，不能作为正式体验验收。

## P7 完整受击、死亡与恢复（实施中）

新增EntityReactionMotion按优先级登记请求、同优先级取较长剩余硬直并替换速度、不累计冲量；PlayerLifeSession持有可选生命/接收器/保护，受击与死亡经生命根转换组和独立FSM节点接管。新增三种根声明适配纯移动、普通战斗和冲刺战斗；攻击关闭不自动关闭生命装配。联合事务抽取候选提交公共过程，新增InstallLife，在候选图/动画/配置通过后统一提交，并保留现有图ID/绑定。生命快照、死亡恢复策略/确认、自定义处理、完整沙袋运动/陷阱仍未实现；P7不放行。

前一增量编译Console 0错误。随后新增三项真实FSM隔离测试，首次编译发现测试Place辅助函数传错第三参数（要求float），已修正并重新请求编译；新测试结果待确认。受击期间角色保持垂直控制权以避免跳跃修正覆盖击飞，普通重力及真实碰撞继续；不以此实现描述代替实测。

P7首批编译确认后 PlayMode job `7af58b468e084ee0a551b5d88f2f06e3` 3/3 Passed、22.9651482秒、无输出：受击打断攻击/清连招且落地保留硬直；同优先级替换速度不叠加、低优先级仍扣血；新无敌不否决首击反应而拒绝后续击；关闭默认反应仍扣血、真实死亡节点一次性接管。后续增加受击结束有效缓存保留/过期后归还普通输入，不能把该增量算入前一批证据。

生命快照扩展为v3，明确生命装配/HP；旧版本默认拒绝，新增显式迁移接口覆盖数据与能力集合，迁移结果仍进入候选校验。普通读档保留HP，死亡Recover单独在内存快照上生成位置/回血候选；增加自定义死亡和恢复位置接口，死亡通知在FSM返回后派发，Gameplay输入拒绝与现有UI恢复按钮分离。未改UI生成区或Binding。攻击本体/有效收刀/冲刺接续或惯性/受击/死亡捕获明确拒绝。

该增量编译Console 0错误。job `585ca491333d43ba8830967893102a2c` PlayMode 6/6 Passed、15.749596秒：v3 HP往返、v2拒绝、普通候选HP与死亡满血候选区分、冻结配置和失败候选保留死亡原会话；攻击/受击捕获拒绝及末尾输入恢复；现有完整快照资源计时/防御复制/损坏拒绝/候选隔离回归。四项Foundation输出仅既有EventBus订阅日志，无错误。完整PlayerModule死亡确认交接、输入通道、同批真实换血、暂停和自定义处理仍待验证。

继续为沙袋接入EntityMotor2D与反应运动/重置生命周期，为陷阱接入独立接触间隔及同一结算批次；没有单独Update或直接改玩家HP。该最新增量已请求编译，尚未确认或测试。正式方案仍为P5的29节点，尚未安装生命节点、最终伤害窗口或素材，P7未完成。

### P7 本轮新增证据与断连交接

后续编译确认后，取得以下本轮 PlayMode 结果：

| Job | 实际结果 | 覆盖内容与边界 |
| --- | --- | --- |
| `ee9c95e49f944cdb8b6ddfe98c257b28` | 3/3 Passed，18.1301799秒，无输出 | 沙袋通过Motor真实击退/击飞；重置位置、速度、HP、反应、效果，旧排队命中失效（S04）；陷阱独立间隔与玩家无敌；真实攻击到批次/沙袋伤害及玩家替换保留世界HP回归 |
| `743803804b3a43f5a1784fa1c40745df` | 2/2 Passed，10.3468217秒；仅既有EventBus订阅日志 | 实际PlayerModule与GameSceneScope：攻击关闭、生命开启，死亡只禁Gameplay且世界TimeScale=1；默认通知/自定义处理一次，失败恢复保留原死亡会话；候选恢复满血、普通读档保存HP、嵌套暂停及恢复输入（C02、L01～L05、L08部分）。测试直接使用Scope替换事务，尚不等同于正式UI点击或完整Recover入口验证 |
| `c6c5bd98e72b43bb94be4bd5304b5a49` | 2/2 Passed，15.2523568秒，无输出 | 两个真实玩家同批致死后均进入死亡节点、生命/反应不共享（S01部分）；霸体仅挡反应仍扣血，暂停冻结硬直并正常恢复 |

新增 `LifeJointCommitRejectsInvalidAnimationAndSupportsUndoRedoReload` 测试，验证非法动画拒绝、安装生命节点、Undo/Redo与保存重载；UI关闭时补充取消死亡事件订阅。刷新后实时 `editor/state` 返回 idle、非Play、非编译、无待刷新，Console错误查询返回0条。此时实时查询 `enterPlayModeOptionsEnabled=true`、`enterPlayModeOptions=1`、实际FrameworkScene `dirty=false`。

随后启动 EditMode 结构测试组 job `c5de7c72724545a1bf02dcff9f41d791`，包含新生命事务测试。等待结果时返回：`Unity plugin session 61953663-d12c-4b93-a54a-07887bdd1fbb disconnected while awaiting command_result`。**结果未知，不计通过或测试失败；不能确认断连后编辑器/测试清理状态。** 立即停止本轮自动Unity验证，未按工具hint重试，未使用dotnet、BatchMode或日志轮询。后续仅即时磁盘静态检查：EditorSettings两项值仍为1；`git diff --check`退出码0，仅行尾转换提示（不覆盖未跟踪C#的行为验证）。

P7仍阻塞，未放行P8。上述变更主要位于Assets内Player/Entity、快照编解码、生命节点联合事务、Gameplay页用户逻辑及测试；无本轮新增公共运行时API。P6文字中的“E目标隔离”不是验收编号，实际对应H08。

下次恢复顺序：

1. 重新检查Unity MCP、实时Play Mode选项与场景草稿；检查是否残留运行中的测试/临时资产，先确认状态，不强行清理用户内容。核实编译、查询上述job（若已丢失则重新执行结构组），补生命事务/Undo/Redo/重载结果。
2. 补P7尚未完成的集成：受击/死亡打断冲刺与收尾；恢复后旧攻击/待结算命中不得进入新玩家；默认确认输入与重复Recover请求；自定义恢复位置/回血策略及显式迁移接口的正反用例。区分Scope事务已有证据与实际Recover/UI入口未验证。
3. P7退出通过后才开始P8：正式三连击素材/命中窗口/简单刀光/生命接线及沙袋陷阱，工具时间轴/原生姿态预览/第四攻击事务与Skill。特殊连接关闭时保留既有拓扑的参数路径、生命根与新增攻击事务兼容性仍需核对，不得静默删节点或布局。
4. P9补全矩阵（包括H05完整过滤、H07编辑器左右姿态对照、H08完整表现），正式Main入口与白盒同驱动实测。正式方案仍为独立空洞手感复制的29节点版本，未登记为已完成的内置攻击示例、未切默认方案。

本次未完成 Unity 编译验证，请在 Unity 中手动触发编译。此前成功批次证据保留，但断连后的最终编译和结构测试结果不能确认；若仍有报错，请提供首条错误及完整堆栈。未执行模板保存、Bump、Git提交、推送、发布或消费项目升级。

### 2026-10-15 P7再次续跑

重新连接成功，单实例 `ember-unity-framework@b23e9b768e85ed30`，新连接session `112e3a7e-cea4-4805-8fd3-114ae1d44da8`。实时查询 `optionsEnabled=true/options=1`、FrameworkScene `dirty=false`；没有切换模板或重载场景。

成功取回上一轮 job `c5de7c72724545a1bf02dcff9f41d791`：EditMode **7/7 Passed，43.0957792秒**，包括生命联合提交非法动画拒绝、Undo/Redo、保存重载及原结构/布局复制回归（C05/C06）。这属于上轮实际执行、本轮取回的结果，不是本轮重跑。生命用例输出一条刻意非法Legacy Clip的拒绝诊断；当前Console错误查询0条。请求刷新编译后再次Console错误0条。

本轮在 `PlayerReactionTests.cs` 新增8个隔离用例，编译后Console错误0条；PlayMode job `d0710c8c9d2b4068a7ab3e590959a5e6` **8/8 Passed，113.9211426秒，全部无输出**：

- 冲刺本体/收尾分别被受击或死亡接管，清连招/惯性/旧攻击请求，后续攻击和冲刺输入不能越过受击/死亡（P5留P7的强制接管集成，共4例）。
- Full/Fixed/Ratio/Saved四种回血策略分别得到10/4/5/7 HP，Spawn位置正确，构造候选时死亡原玩家仍HP=0且保持死亡节点（L04/L05部分，共4例）。

随后增量修改 `PlayerLifeModuleTests.cs` 两个已有模块用例：自定义位置提供方返回(12,8)；在玩家替换前排队旧目标致死命中，替换后Resolve并检查旧目标注销、新HP不被影响。该增量尚未验证：先后出现CS7036缺少EntityHitReceiver的health/effects参数，按真实构造器签名修正为 `new EntityHitReceiver(null, null, null)` 并请求刷新。随后首次Console查询返回 `Unity session not ready for 'read_console' (ping not answered); please retry`。立即停止自动Unity验证，没有重试、替代编译或日志轮询；最终编译结果未知，两项修改后的模块用例不得沿用旧通过结果。

P7仍未完成，P8/P9未放行。修改范围仅上述两个测试文件及实施记录，正式29节点方案及资源本轮未改。下一步重新连接后确认设置/草稿、编译，并运行：

- `Game.Player.Tests.PlayerFoundationTests.DeathModuleClosesOnlyGameplayAndScopeRecoveryKeepsWorldAndSavedHealth`
- `Game.Player.Tests.PlayerFoundationTests.CustomDeathHandlerRunsOnceOutsideFsmAndUsesSameRecoveryTransaction`

之后继续补确认输入/重复Recover入口、显式迁移正反用例及自定义处理缺失诊断；P8正式体验和工具/Skill、P9正式入口白盒仍待实施。上节生命联合提交“结果未知”已由本节取回结果解除，不能因此把整个P7标记通过。

本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；若仍有报错，请提供首条错误及完整堆栈。

### 2026-10-15 P7退出与P8启动

Unity连接session `1d33a97f-2963-4362-9a5a-7dce0c40895a` 成功，实时optionsEnabled=true/options=1、FrameworkScene dirty=false，非Play/非编译；Console错误0条。上轮修正后的模块测试本轮实际重跑：

| Job | 结果 | 本轮证据 |
| --- | --- | --- |
| `ba55d00fedfd478e816a021d8c7b0192` | PlayMode 2/2 Passed，13.5767637秒 | 自定义位置(12,8)、旧玩家目标注销/排队致死命中失效，新玩家满血；默认/自定义模块恢复与失败原会话保留回归 |
| `0a23bf042f21479cbb1e6dba6e7c5f5d` | PlayMode 2/2 Passed，14.8495733秒 | 为隔离夹具增加独立UI通道，死亡禁Gameplay时虚拟键盘Enter确实触发Confirm一次；前一批模块断言同时通过 |
| `1799a484b6c44c19960d62e6b3fe8a63` | PlayMode 2/2 Passed，25.9463826秒，无输出 | 缺自定义死亡/恢复位置处理方显式诊断且不动原会话；真实v2字节布局默认拒绝，显式迁移接入v3且保留能力数据，合法HP候选可准备，迁移非法HP仍被候选拒绝，旧字节未改 |
| `2c49435fd6154fc5a0e589e723a325e6` | PlayMode 1/1 Passed，7.4598723秒 | 实际EmberStateMachine声明/请求、EmberSceneTransitionRunner、PlayerModule.Recover及PlayerRecoveryTransition/Scope替换链：等待准备时重复Recover拒绝，只提交一次，无场景重载/卸载；保持嵌套暂停及旧命中清理 |

最后一项隔离测试仅替换展示状态和依赖查找，使用正式FSM Start/边声明/请求仲裁，没有改FSM内部状态；不等同于正常Main入口或正式EUI点击。模块测试输出只有EventBus/模块生命周期日志，无错误。编译曾发现测试程序集缺少Ember.Scene.Runtime引用，已在测试asmdef增加该依赖并引入命名空间；重新编译Console错误0条后才运行上述最后一项。没有改公共框架API。

P7退出项现具备分项证据，覆盖C02、L01～L08相关生命/恢复契约及S01/S02/S04相关集成，结合前述受击优先级/无敌/同批换血/冲刺中断证据放行P8。完整矩阵不因此全标通过：正式UI点击、正式Main入口、最终美术与白盒实播按P8/P9仍待验证。P8开始完善工具与正式体验，不切换模板或默认方案。

## P8 工具、正式体验与Skill（实施中，编译验证阻塞）

首批改动仅模板Assets源码：

- `PlayerCombatExampleWorkflow.ApplyConfiguration`：公开完整候选联合提交入口，按源根声明保留生命和冲刺装配，复用攻击/收刀状态实现，不自动保存；为第四攻击/Skill调用提供实际入口。
- `PlayerCombatGraphBuilder`：关闭停顿派生保留已拥有的特殊收刀节点/稳定ID；允许已知纯移动生命根接入攻击，不允许覆盖任意自定义根。
- `PlayerCombatGraphMap`：启用停顿连接仍要求特殊收刀节点；关闭连接时允许保留节点，但继续完整检查节点归属和绑定，不能借关闭绕过FSM结构校验。

已请求Unity刷新编译，紧接的首次Console查询返回 `Unity session not ready for 'read_console' (ping not answered); please retry`。按规则立即停止本轮自动验证，没有重试、dotnet、BatchMode或日志轮询。**这三个文件的最新增量编译/行为均未验证；P8未通过，P9不放行。** 本轮P7已通过的作业结果保留。

后续最小入口：重新连接并确认options=1及场景/资产草稿；补P8最新编译，然后添加并运行生命+冲刺方案新增第四攻击、停顿派生关闭/重开保留拓扑、Undo/Redo/重载测试。通过后继续开发工具参数/时间轴/原生命中预览与诊断、正式三连击素材/窗口/刀光/生命/沙袋陷阱、Skill及清单，再执行P9正式入口/白盒矩阵。正式方案仍为已保存的29节点版本，本轮未改正式资源、默认引用或场景；没有生成美术或完成Skill。

本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；若仍有报错，请提供首条错误及完整堆栈。

### 2026-10-15 P8结构解除阻塞与表现工具续跑

本轮连接 session `76a6e861-1a2d-4fa9-b66a-e00d5b238e35` 成功，FrameworkScene dirty=false、optionsEnabled=true/options=1；初始及新增代码编译后Console错误0条。正式方案/图/Controller及其子资源dirty检查为空，已备份至 `platformer-combat-evidence/2026-10-15-p8-before`，未切换模板/默认方案。

| Job | 实际结果 | 证据范围 |
| --- | --- | --- |
| `0f1fade7849a4c2893641cd9e1379712` | EditMode 1/1 Passed，29.6566111秒 | 三连击+冲刺+生命新增第四攻击，复用状态类；旧ID/根装配保留；关闭/重开停顿派生；Undo/Redo/保存重载有效 |
| `89ec4e93296b4f808ef99ccaaff914e3` | PlayMode 2/2 Passed，26.6086384秒 | 停顿派生关闭仍保留拓扑，实际FSM进入普通收刀；重新开启进入特殊收刀 |
| `74e166c075674ee785a8a61d530471aa` | EditMode 1/1 Passed，30.4233974秒 | 上述结构回归，并增加纯时长参数提交图/Controller JSON不变、Undo/Redo验证 |
| `6584edee1b464a018113498c0f6b9d77` | PlayMode 1/1 Passed，9.6486853秒 | 冻结后的独立刀光帧/左朝向/暂停/结束隐藏/销毁，无Collider；身体闪烁alpha不影响刀光 |

以上作业均无测试输出，覆盖C05/C06结构事务、A07停顿连接相关以及表现时钟分项；不是整个P8或完整矩阵通过。第四攻击只存在隔离夹具，正式示例仍三段。

新增参数路径通过完整候选校验后仅Undo提交方案/Feel，不重建图或Controller。方案窗口新增独立攻击候选、连招连接候选、缺失动画选择、时间轴、原生命中形状编辑入口；草稿以SessionState保留来源/基线/Clip引用，源变化拒绝覆盖。工具窗口草稿往返、冲突与实际UI仍待验证。

新增PlayerAttackVisual配置及会话，使用冻结Combat逻辑进度选帧，挂在实体根独立于身体Sprite/闪烁，不承担命中。使用imagegen生成透明六帧刀光，来源及提示词在SlashSheet-source.md；实际2172×724，原等分第4/5帧切线穿过可见像素，已通过TextureImporter序列化属性调整边界为1465并保持原全局枢轴。重新读取六Sprite的Rect/Pivot确认成功，options仍1。新PNG没有覆盖已有资源。尚未接正式三连击、生命和沙袋陷阱场景，不代表正式体验完成。

最新小修过滤攻击窗口已移除/已安装的Clip候选，成功提交清空待新增Clip列表，防止后续纯参数编辑无谓重建图；已请求编译，结果待下次成功Console查询确认。后续继续工具草稿验证、正式资源接线、Skill，P8完整退出前不推进P9。

正式资源接线进展：新增Attack_1/2/3、Recovery、Hit、Death六个局部Sprite姿态Clip，沿用现有方块角色，没有修改Motor/碰撞体。通过InstallLife接入生命，图29→31节点，实时核对原29个ID全部保留；HP10、0.6秒受击无敌、身体闪烁、确认恢复，显式启用效果容器，HUD关闭。通过ApplyConfiguration提交三个独立main命中窗口（0.1～0.25秒），前两段伤害2、末段3，末段击退/击飞5/5，六帧独立刀光24fps；映射Clip并保存方案/图/Controller，自带Validate成功。实际伤害与画面还未实播，不据此标记正式体验完成，沙袋/陷阱资源与场景待接线，默认引用未改。

新增CombatWindowDraftSurvivesCloseAndRejectsChangedSource隔离EditMode测试：测试保存/恢复原SessionState，不操作现有窗口；覆盖未应用源值不变、窗口关闭重开恢复草稿、应用Undo后源变化拒绝并保留草稿。已请求编译，测试尚未运行。

后续已验证：窗口草稿测试 job `0680b70b903d42ef860f52b5a54e02a6` EditMode 1/1 Passed，8.0689772秒，无输出；正式资源隔离运行 job `f92a75040f8349ce86f7880c168eae13` PlayMode 1/1 Passed，0.7094007秒，无输出，真实31节点方案+Attack_1+六帧刀光+TrainingDummy.prefab，沙袋30→28 HP，显示项目SlashSheet帧且无Collider，ResetActors恢复30 HP和零速度。这不是Main/白盒入口或整套三连击体验验收。

TrainingDummy/ContactTrap.prefab已创建，动态合法Motor与统一Payload；备份两场景后逐个以Additive打开、增量加入CombatExample根并保存关闭，GameplayScene与PlayerFeelLab均各1沙袋/1陷阱，位置(-4,0.8)/(-8,0.25)，不重建原场景。FrameworkScene保持未脏。

诊断增量：现有蓝图观察入口加入运动控制者/通道、段/时间/收刀、开放门、冲刺续接/惯性、请求意图/有效期/失效原因、命中窗口账本与最近结算、HP/硬直；不主动刷日志。该增量编译后Console错误0条，行为回归待运行。

按skill-creator编写唯一维护源 ember-platformer-add-attack/SKILL.md，schema v2清单及三个既有Skill路由同步更新。quick_validate使用Python UTF-8模式通过（默认GBK读取曾失败，未误报通过）；正式PreviewCurrentTemplateSkills返回1新增/3更新、Errors空、NeedsBackupConfirmation=false。Skill端到端行为验证及完整工具/正式体验仍待后续。

Skill已通过正式SyncCurrentTemplateSkills(preview,true)安装/更新，执行前置检查返回null。同步触发导入后新增SKILL.md.meta导致预览尚有1项待同步，需要再按正式接口收尾；没有手改安装副本、Templates~或hash。

工具预览新增方案重载入口，按实际攻击Clip采样模板PlayerBlock表现子树、逻辑秒滑动及左右镜像编辑；左向原生Collider应用时转换回统一右向配置。正在补镜像往返测试，尚未标通过。预览当前角色来源明确为模板PlayerBlock；自定义角色选择入口仍需完善，不能宣称任意角色姿态均支持。

可选HUD：按ember-eui-build调用EUICreationService预检/Create，创建Business Item `EUIPlayerHealthItem`，通过EUIBindingEditorUtility登记HealthFill(RectTransform)/HealthValue(TMP)，保存后TryRegenerateCode成功，读取真实Binding后编写独立用户partial。备份Gameplay页面后仅追加HealthRoot Extension绑定，保留6个IsFramework条目，不手写Binding或改EmberManaged。正式方案HUD仍默认关闭；生命/显示独立冻结开关通过PlayerContext.ShowHealth暴露给UI。页面按会话提交/释放、显隐订阅/解绑生命Changed事件；新增隔离测试覆盖生命关闭、仅生命、生命+HUD、冻结源开关、释放后旧订阅不更新和重新绑定。代码已请求编译，最新测试待运行。

形状编辑回归 job `f153c92d7a7a43a8bd1cfccd98c1b094` EditMode 4/4 Passed，2.5748822秒，无输出，含Box/Circle/Polygon左右镜像往返、原生Stage应用Undo/Redo/冲突拒绝、主场景未脏。角色姿态采样的实际画面仍待验证。

HUD PlayMode job `d3c152fa988f4f098646beaea8ece0f1`：3/3 Passed，32.6138687秒，无输出。生命关闭/仅生命/生命+HUD独立开关、源编辑不改冻结显示、真实生成Item数值变化、会话释放后解绑及重新绑定均通过（L08分项）。原生预览阶段镜像测试前最新编译Console错误0条。

正式六个姿态Clip补局部Y位移抵消纵向压缩，实际Sprite/VisualRoot尺度逐片段采样21次：脚底基线-0.875，最大误差5.96e-8；这只证明脚底几何，不替代观感。Skill正式二次同步后HasChanges=false，执行前置检查无阻塞；Play Mode设置仍1。

准备P8白盒实播前，已记录原开发设置：Scheme GUID a2d7100fc47f54b4282b5be389d735b8，位置(0,1.05)，速度(0,0)，Whitebox=false，六个EditorPrefs键均存在。测试后须恢复这些设置；项目默认方案不修改。当前FrameworkScene未脏、非Play。

P8正常Main→白盒实播：实际Main的m_Btn_Start.onClick进入PlayerFeelLab，玩家冻结通用攻击模板1，HP10。两轮未触发输入轨迹保留：首先发现Editor无焦点，独立InputSettings副本处理焦点后仍无响应；进一步查明正式Input Host仅配对原Keyboard，新虚拟设备不在动作controls中。使用InputUser.PerformPairingWithDevice临时追加测试键盘，结束Unpair/Remove并恢复原InputSettings后，实际全局PlayerModule驱动完成first→second→third→recovery.third→idle，沙袋30→23；未直接改FSM/HP或手动Tick。

证据：whitebox-combo-paired-trace.txt记录真实时间/状态/位置；whitebox-slash-peak.png为实际游戏Camera在Slash_2帧暂停截图，左向刀光透明/枢轴/身体独立可见。初次whitebox-attack.png捕获延迟未显示刀光，不能作为刀光证据。实播发现新增沙袋(-4,0.8)与原白盒低平台重叠导致挤压，待改新增对象位置，不改原平台。

测试输入副本/虚拟设备已清理，原背景策略ResetAndDisableNonBackgroundDevices与PointersAndKeyboardsRespectGameViewFocus已恢复。已恢复原开发方案/位置/速度/Whitebox设置并请求退出Play。正式默认配置和P0选项1没有更改。P8仍实施中；需位置修正后回归、右向/陷阱死亡恢复实播、工具与Skill限制路径、内置登记，再推进P9。

新增沙袋已在两场景移至(-2,0.8)，避开白盒原低平台；位置修正后的物理/体验尚需重跑。正式三连击左向峰值实播截图已读取检查，刀光透明、无裁断、位于角色左侧；不是右向或完整帧稳定性验收。

为满足与空洞/蔚蓝同级的内置方案，新增PlayerBuiltinSchemes.RegisterAdditional：仅嵌入式框架模板编辑、非Play、合法独立完整方案、既有唯一登记且无草稿时增量追加，不重建旧方案或设默认。初编译发现直接使用TemplatePreview需额外UPM程序集引用，已改为既有IsEmbeddedPackage/GetEditingTemplate检查，避免扩大依赖；最新修正待编译确认。登记资产已备份，尚未实际登记。既有两移动方案测试改为只遍历对应两项，并新增三连击内置完整性断言；测试清理由全局Undo.ClearAll改为仅清本夹具资产Undo，保护用户历史。

### 本轮最新阻塞与续跑入口（P8未退出）

修正内置登记编译错误并请求刷新后，首次Console查询返回 `Unity session not ready for 'read_console' (ping not answered); please retry`。立即停止自动Unity验证，没有再次查询/重试，也没有dotnet、BatchMode、Editor.log或进程轮询。**PlayerBuiltinSchemes.RegisterAdditional和对应测试最新修正编译结果未知，内置登记未执行；不得直接运行预期已有3内置方案的PlayerSchemeWorkflowTests。** 需先编译确认，再在实时检查无草稿后调用登记，之后运行相关测试。

停Play和恢复开发偏好已经在失联前完成；随后成功查询并修改了两场景新增沙袋位置，证明当时已回EditMode。失联前最近options=1；失联后仅静态读取EditorSettings.asset确认Enabled=1/Options=1。没有重试Unity来证明最终状态。静态git diff --check无空白错误，Skill quick_validate成功，均不代替最新Unity编译。

下一轮依赖顺序：

1. 重连、核对项目/草稿/Play设置及编译。新增登记入口采用Core.Editor中真实GetEditingTemplate/IsEmbeddedPackage，未新增框架公共API或UPM依赖。
2. 校验并增量登记通用攻击模板1，保存重载；运行PlayerSchemeWorkflowTests（现在要求3方案，前两套保持21节点，第三套独立31节点），以及最新观察/结构/草稿回归。登记前已有备份PlayerSchemeLibrary.asset。
3. P8余项：修正位置后沙袋物理回归；右向与完整刀光帧实播；实际陷阱掉血/无敌/死亡确认恢复；原生角色姿态预览实视、工具窗口操作、Skill关闭/错误/受限模板路径。现有测试仅覆盖相应分项，不提前宣称完整P8通过。
4. P8退出后执行P9完整矩阵、正式Main新游戏/继续与白盒同驱动、文档收口。正式验收环境目前使用项目默认方案，攻击方案仍未设默认；如需测试正式GameplayScene中的攻击方案，应增加明确的本次验收装配入口或使用已授权临时配置注入，不能悄悄改项目默认或用白盒替代正式入口。

本轮范围：Assets内模板代码/测试、正式攻击/生命/刀光资源、沙袋陷阱Prefab与两个场景、EUI Item及Gameplay用户partial、Skill维护源与正式同步安装、实施记录/证据。未改公共运行框架，未执行模板保存/Bump/Git提交/推送/发布/消费升级，未切模板或默认方案。P8实现与验证尚有上述未完成项；P9未放行。

本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；如果仍有报错，请提供首条错误及完整堆栈。

### 2026-10-15 P8重连：登记持久化差异与测试查询超时

- 实时实例 ember-unity-framework@b23e9b768e85ed30；Application.dataPath正确，EditMode、非编译，FrameworkScene.isDirty=false。EditorSettings选项Enabled=true、Options=1。Console读取0条error；最新RegisterAdditional方法已加载且执行成功，补齐此前修正代码的加载证据，不代表完整测试验收。
- 调用前验证正式方案通过，方案/Graph/Controller均不脏，已有两套内置。RegisterAdditional调用返回成功，随后内存Builtins返回空洞骑士风格、蔚蓝风格、通用攻击模板1。未改默认方案。
- 启动EditMode回归：PlayerSchemeWorkflowTests、PlayerSchemeStructureWorkflowTests、PlayerSchemeObservationTests；job_id=37b5a6fa76594cfc8de7b16e7bd20f97。首次get_test_job(wait_timeout=30)返回success=false/error=TimeoutError。立即停止本轮全部Unity自动验证，无重试、无替代编译或日志轮询。测试运行/清理状态和结果未知，不能标记通过，也不能假定任务已停止。
- 失联后的单次即时静态检查：EditorSettings.asset仍Enabled=1/Options=1；git diff --check退出0（仅既有行尾警告）。登记文件Assets/GameResource/Player/Editor/PlayerSchemeLibrary.asset磁盘builtins仍仅两项，与调用返回的内存三项不一致。登记持久化尚未通过，不能以调用成功宣称完成。静态源码显示PlayerSchemeOwnership.Load将登记自身GUID纳入保护，PlayerSchemeSaveProtection.IsOpenForEdit及OnWillSaveAssets限制只读资产；这是需核对的保存阻断候选原因，尚无实时根因证据。未手改YAML、未解除全局保护、未在未知测试运行状态下编辑代码。
- 本轮落盘范围为实施记录；登记API曾请求保存归属资产，最终内存状态待重连核对。沿用此前备份，不覆盖用户草稿或重新生成资源。P8涉及同级方案/所有权与工具回归的验收未通过；沙袋位置修正后的实播、右向刀光、陷阱死亡恢复、工具与Skill剩余验收未执行。P9不放行。
- 下一轮先获取上述job状态及Unity草稿/Options=1，再核对内存与磁盘登记差异。修复必须通过最小、受限的模板编辑登记保存事务，保持所有权校验，不能广泛关闭保护。保存/重载和回归通过后，再沿上一节顺序完成P8余项与P9。

本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；如果仍有报错，请提供首条错误及完整堆栈。此前Console零错误和已加载API证据保留，但超时后的最终验证未完成。

### 2026-10-15 P8登记持久化修正续跑

已获取上轮job 37b5a6fa76594cfc8de7b16e7bd20f97最终结果：EditMode 25/25 Passed，124.0907846秒；上轮超时是结果查询失败，不是测试失败。本轮实时Options=1、Enabled=true；登记内存三项、dirty=true、NotEditable、IsOpenForEdit=false。先备份内存草稿至p8-before/library-pending-registration.json。

修正范围仅Assets模板Editor的PlayerSchemeLibrary.cs和PlayerBuiltinSchemes.cs：SaveRegistration校验模板编辑/非Play/唯一登记/非重入/完整方案，仅保存期间对该登记路径许可写入，finally恢复NotEditable和许可，保存后仍dirty则抛错；新增登记复用此入口。未放宽其他内置资源所有权或修改框架API。

Unity刷新返回成功，Console 0 errors，新方法已加载。核对内存JSON与备份完全一致后调用保存：dirty=false、IsOpenForEdit=false、NotEditable、磁盘含第三套GUID。仅在无dirty时强制重新导入登记并Reload，返回三套名称、dirty=false、editable=false、Options=1。持久化/重载基本验证通过。启动本轮同三组回归job e34a5f69efb54469af347dff65d7c68d，结果待查询。P8余项与P9仍未放行。

本轮最新阻塞：get_test_job(e34a5f69efb54469af347dff65d7c68d, wait_timeout=0)返回success=false，Unity plugin session 2c99d3d4-5005-4ce5-ac7a-5a6b88c8a629 disconnected while awaiting command_result。立即停止Unity自动验证，无重试、替代编译或日志轮询。新测试最终结果及清理状态未知。沙袋位置、右向刀光、陷阱死亡恢复、工具/Skill余项均未在本轮实测；P9未放行。

下一轮先查询此job结果和实时Options=1/场景草稿/登记dirty状态，不要重复追加登记。若通过，再推进P8剩余验收；本轮保存重载已取得成功证据，但仍需回归结果与最终保护状态确认。未切模板/默认方案，未模板保存/Bump/提交/发布。本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；若仍报错，请提供首条错误及完整堆栈。超时前的Console零错误、新方法加载和保存重载结果已如实保留。

### 2026-10-15 P8实播续跑与攻击内置复制缺陷

取回job e34a5f69efb54469af347dff65d7c68d：25/25 Passed，135.7591554秒。实时Options=1/Enabled=true，FrameworkScene无脏改动；登记dirty=false、NotEditable、IsOpenForEdit=false，磁盘有第三套，未重复登记。三方案只读，攻击独立图只读；旧两方案共享项目图保持既有可写关系。

正常Main新游戏→PlayerFeelLab，正式方案、真实Global驱动：沙袋落地(-2,0.76)、速度0，底边约0.01；右向真实配对虚拟键盘J+D，攻击Slash_2峰值自动暂停，截图whitebox-right-slash.png已查看，刀光透明、在右侧，沙袋不再穿平台。输入副本与设备已清理。陷阱新会话spawn(-8,.95)自然触发HP10→8→6→4→2→0，时间0/.084/.795/1.569/2.416/3.132；trace见trap-live-trace.txt。等待下一正常帧后life.dead，正式UI显示死亡确认；点击m_Btn_Respawn后HP10、fall、spawn(-8,.95)、新FSM、同沙袋对象和场景句柄，见trap-recovery-live.txt。原开发偏好已按continuation-prefs.json恢复并退出Play。此为白盒实际入口证据，不替代正式GameplayScene或P9全链。

工具实视准备发现真实失败：CopyIndependent(第三套正式攻击方案)在动画校验combat.attack.first抛错。Controller内嵌动画Key被CopyAsset复制，但方案Animation映射仍引用源Controller子资产。源资产未改，失败事务回滚新资产（外部创建的空CombatPreviewAcceptance目录尚需清理）。修正仅Assets Player Editor：复制后按GUID所属路径与local file ID重映射Animation内Controller子资产引用，类型/缺失检查失败即回滚；既有结构复制保存重载测试增加builtinIndex=2用例。最新刷新已请求，编译与此修正测试待确认，P8不退出。

最新编译Console 0 errors。复制回归job e9883bc4ae6844db89eb077e0c669307最终2/2 Passed，16.3430172秒；分别验证空洞和正式攻击内置方案的独立复制、节点/绑定保留、源不变、保存重载及Controller独立性。使用修正API创建隔离副本Assets/CombatPreviewAcceptance_69ff9366acce41f89ee2e9396e2ece26，打开原生命中预览，native-hit-preview.png已实视，角色姿态和命中框同时显示；未应用配置，不改变正式方案。

清理调用包含GoToMainStage、删除明确本次创建副本、清SessionState和读取Skill状态，但execute_code工具因AssetDatabase.DeleteAsset内置blocked-pattern检查拒绝整个调用，以上清理及Skill查询均未执行。该拦截是工具自身安全检查，不是自动审批审核拒绝。随后run_tests请求Skill源校验/最低版本/外来归属/临时项目切换四项时返回Unity plugin session 088f0e76-5dea-41a6-ac79-100c445d6409 disconnected while awaiting command_result，无job_id。按规则立即停止Unity自动验证，无重试或替代编译。

下轮必须先确认测试运行状态、当前PreviewStage和用户草稿：预览阶段可能仍为PlayerHitShapePreview，本次隔离副本目录69ff9366acce41f89ee2e9396e2ece26仍保留，SessionState键CombatAcceptance.PreviewFolder未清。先安全退出本次预览（恢复其记录的选择），确认目录内容均为本次生成再清理；不关闭其他用户预览。此前复制失败创建的空目录也应只在辨明本次路径后清理。禁止假定清理已完成或重复启动未知Skill测试。本轮最后成功实时Options=1，原开发方案/位置/速度/Whitebox已恢复，非Play；断连后状态未查询。

P8余项：Skill真实错误/受限路径、模块关闭入口（不得切换当前模板或改用户模块状态）、工具窗口完整交互及六帧全程表现检查；正式GameplayScene新游戏/继续仍留P9条件满足后执行。已完成实播只覆盖白盒，不提前等同全链验收。P9未放行。源码改动仅Assets Player Editor复制逻辑及对应测试，无模板保存/Bump/提交/发布。

本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；如果仍有报错，请提供首条错误及完整堆栈。断连前编译查询和复制测试证据保留，但最终Unity验证未完成。

### 2026-10-15 P8预览清理、Skill与六帧验收续跑

实时确认Options=1/Enabled=true，非Play且无测试运行。上轮Skill请求无job_id，不能取回；本轮重新启动job 3d70d00f8d094c8db48c6ec857e4eebb，5/5 Passed（1.6924728秒）：源格式、最低版本两例、外来归属、临时项目部署/切换保护。未切换当前Unity模板。当前攻击Skill执行blockReason=null。

已核对预览为本次PlayerHitShapePreview，SessionState路径精确匹配，三资产均无dirty且目录只含本次Scheme/Graph/Controller；使用明确允许的execute_code safety_checks=false执行本次清理，恢复MainStage及原选择，删除69ff9366acce41f89ee2e9396e2ece26副本并清SessionState。此前失败复制的fab788e8ca45434e901270e3b5575833目录确认为空后也删除。未触碰用户草稿。

正常Main→白盒完整帧实播：首次点击早于主菜单就绪，返回无匹配对象；随后只读查询确认按钮就绪后正常点击，Console0错误。实际输入捕获Slash_0/1/2/4/5截图已检查，位置/透明正常；Slash_3因实际帧采样跳过未捕获，补拍frame-Slash_3.png为无刀光画面，明确不作通过证据。原输入设置、设备及开发偏好已恢复，退出Play。隔离PreviewScene渲染正式六张Sprite并finally关闭，slash-six-sprite-preview.png已查看，全部六片无裁断、渐进消退，切片枢轴保持统一坐标；这是静态视觉证据，不冒充实播逐帧通过。

为补齐时钟驱动完整帧逻辑证据，增强现有FormalCombatAssetsDealDamageShowSlashAndResetDummy测试：按既有.02固定步进收集实际可见Sprite，断言正式六帧全部出现，攻击结束刀光关闭；原伤害/无Collider/沙袋重置断言保留。仅改Assets测试，刷新已返回，最新编译与测试待确认。P8未退出，P9未开始。

P8本轮结果补充：首次六帧测试过滤误用文件名PlayerThreeHitTests（实际类为partial PlayerStateTests），job09c593e608b449c6bb2ba75d65c6d71e共0项，不计通过。更正后job d9163afdfa104a96b90f78c8d8b272d2 PlayMode 1/1 Passed，.5929649秒，正式六帧显示和结束隐藏、伤害/沙袋重置均通过。六帧视觉检查为隔离Sprite渲染，实际输入采样缺Slash_3如上保留。

工具补充job de10e81b16c34c64a135db03766a22ac EditMode2/2 Passed，22.0433372秒：真实窗口候选Read/Apply路径新增第四段，缺Clip拒绝且源不变，补Clip成功，保留生命/冲刺；窗口关闭重开/冲突草稿保护通过。测试保护并恢复原SessionState，仅删隔离资源。覆盖C03/C05/C06及P8第四攻击退出条件。模块关闭/非所属模板入口为本轮静态核对：PlayerEditorAccess.Visible调用正式IsTemplateActive，Enabled读模块Attribute，Require及窗口DrawUnavailable统一限制；未实际切当前模板或关闭模块，不能称运行切换实测。Skill源说明要求执行前核对此边界；独立项目受限/错误路径已有本轮5项证据。

P8实现及规定退出条件已具备证据：工具配置/第四段、预览隔离、正式素材接入、Skill边界；保留上述实播采样及模块开关验证方法限制。进入P9全链验证，不代表整体交付完成。

P9开始：当前项目默认仍为原纯移动方案，正式验收不能用白盒冒充。因此新增Editor-only PlayFormalWithScheme：本次显式选定方案经现有冻结请求/Init前配置副本注入，使用正式场景和原配置出生点，沿用隔离存档；不写默认/开发偏好，不覆盖Gameplay场景路径。Play原入口清除一次性方案字段，Freeze消费后清除；非Play/完整方案/资产GUID校验。新增入口刚刷新，编译和正式Main新游戏/继续验证待执行；不新增框架API。

P9最新阻塞：refresh返回成功后首次read_console返回Unity session not ready for read_console (ping not answered)。立即停止Unity自动验证，未重试、未Play/测试、未替代编译或日志轮询。PlayFormalWithScheme最新编译结果未知。下一轮先重连、确认Options=1及草稿/阶段/无残留测试，补齐编译后正式Main→新游戏/保存返回/继续验收攻击方案，随后完整矩阵和正式文档。原开发偏好已在六帧实播后恢复；临时预览/副本清理有成功证据，无需重复删除。

本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；如果仍有报错，请提供首条错误及完整堆栈。

### 2026-10-15 P9正式入口与全量回归

重连Console0errors，PlayFormalWithScheme已加载，Options=1/Enabled=true，PlayerConfig默认PlayerDefaultScheme且dirty=false，FrameworkScene无草稿。调用本次正式验收，隔离库Library/PlayerFormalAcceptance/52ce3d07eb7e4ca19a0e2edab4c1466e，Whitebox=false。正常Main→新游戏进入Assets/Game/Scenes/GameplayScene.unity，HP10/31节点，沙袋落地(-2,.76)。真实配对虚拟键盘执行三连击，formal-combo-trace.txt记录first .155、second .473、third .866、recovery1.494、idle1.805；沙袋HP23。点击m_Btn_SaveExit→Main继续按钮可用→点击继续→正式GameplayScene HP10/31节点/idle，位置(-1.03,.89)，默认仍PlayerDefaultScheme且dirty=false。保存前位置(-1.08,.89)，继续后约.05的落地物理稳定差异保留，不声称逐位相等。证据formal-before-save.json/formal-continue.json；原输入已恢复，退出Play。

全量EditMode job16c1b3b227d44c0ea5cb47a1eb7493bd完成161项，status=failed，报告3项失败（结果summary返回null，不能自行推算其余158为最终Passed）：旧Movement用例遍历新增攻击内置导致Enabled断言失败；Recapture生命夹具未装效果却默认启用无敌；NativeStage用例硬编码主场景必须非dirty，未尊重进入测试前状态。修正前两项夹具为Take(2)和显式_enableEffects=true；原生预览改为每一步与初始dirty比较，仍校验无污染。最新修正待编译/重跑。

本轮审查发现并向用户说明：旧PlayerActionEffectMigrationTests和PlayerSchemeRulesTests包含Undo.ClearAll，本轮全量可能已执行，无法保证或恢复原全局Undo历史。已改为只清自身夹具目录/临时对象Undo，不覆盖资产/场景用户内容。今后全量前先审计此类清理。原场景若dirty不擅自保存或重载。

正式使用文档新增Assets/Game/Documentation/platformer-2d/PlayerCombat.md，增量更新README/PlayerWorkflow链接和旧“完整战斗未实现”表述；文档仍待最终事实/链接检查。新增docs/dev/platformer-combat-acceptance-matrix.md逐ID待核对表，未将设计预期标记通过。P9未完成。

P9回归补充：修正后的Editor四组job35367c1509f341138566ee6a30e52382，21/21 Passed（34.1823304秒），此前3项Editor失败已在对应组重跑通过。全量PlayMode job016d916cb27647928f7965b41274ce99执行598/598后status=failed，报告唯一失败PublicRecoveryWaitsForPreparationAndPresentationAndCancelsCleanly；最终summary/result仍为null，不推算其余597 Passed。源码PlayerModule.Recover在flow.IsTransitioning时明确拒绝重复请求，P7 DeathRecoverEntryRejectsRepeatedRequestAndCommitsThroughFsm已使用同一拒绝契约；旧测试仍期待Busy。仅修正旧断言为InvalidOperationException，保留准备取消、旧会话保持、展示屏障全部断言。

补充测试代码：自动FixedUpdate物理夹具加入.004～.006秒完整短窗口及双实体同批致死断言，沿用.02/30fps和.01/120fps两组条件；增加攻击可用但冲刺/跳跃未开放的FSM仲裁用例；增加自身/友方/层过滤及被过滤对象不提前写账本测试。上述新增测试刚请求刷新，编译及结果待确认。正式文档另同步PlayerArchitecture的v3/生命/临时动作禁存及PlayerRules的基础21/攻击31节点边界，全部相对链接静态检查0错误。矩阵已逐ID登记对应断言，缺覆盖仍明示待核对，P9不放行。

P9最新阻塞：refresh_unity返回success=false，Refresh triggered but timed out after 60s waiting for editor readiness（timeout=true）。立即停止全部Unity自动验证；未重试Console/状态/测试、未用dotnet/BatchMode或日志轮询替代。新恢复断言、仲裁、过滤、双步长同批命中测试尚未编译确认，也未执行。最后实时Options=1，超时后是否保持及场景/草稿状态尚未查询。无新临时预览/人工副本需要删除；全量测试结束状态有结果，但最终编辑器清理状态待下轮实时确认。

下一轮：重连后先核对Options=1、用户草稿/默认方案和编译状态，运行PlayerFoundationTests.PublicRecoveryWaitsForPreparationAndPresentationAndCancelsCleanly、PlayerCombatInputTests、PlayerCombatPhysicsBaselineTests、PlayerStateTests.HitFiltersSelfFriendlyAndLayerWithoutRecordingExcludedTarget。通过后获取可用的最终回归汇总并完成矩阵逐项覆盖，尤其C08/A05/A06/R09/H07/S03/T02/T03仍有细分覆盖待核对。不得将598项执行数写成598通过。正式文档见Assets/Game/Documentation/platformer-2d/PlayerCombat.md及关联说明；无模板保存/Bump/Git提交/推送/发布。

本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；若仍有报错，请提供首条错误及完整堆栈。超时前PlayFormalWithScheme已编译并实播成功，不代表最新测试代码也已编译。

### 2026-10-15 P9专项阻塞补齐

新session1c88fb6c-c8d2-43f3-8528-a3dbe0b1362c连接成功；Console0errors，新增HitFilters测试方法已加载。实时Options=1/Enabled=true、默认PlayerDefaultScheme且configDirty=false，FrameworkScene dirty=false、MainStage、无测试运行。未修改默认或用户草稿。

首个job97d47d645ab445609377197d7ba05cbd同时传group_names/test_names，筛选交集为0项，不计通过。更正为单一group_names后job526961def4e74a93a51bfda62cf2e97f PlayMode12/12 Passed，1.750093秒：6种FSM仲裁、2项输入清理、2组自动物理（.02秒/目标30fps；.01秒/目标120fps）、恢复准备/取消/展示屏障、命中过滤。自动物理使用.004～.006秒短窗口及双方收集后统一致死结算；targetFrameRate为请求设置，未测量或声称实际屏幕刷新率精确达到目标。覆盖I01～I04、H05/H06、F01及L05对应断言。最新编译及上轮阻塞专项已通过。

已启动完整PlayMode相关程序集回归jobeee9afff723641de8f4594ed4c228df5，结果待查询；仍需逐项矩阵、Editor完整汇总及文档收口。T03等未经实际切模板的边界保持未验证，不为取得证据切换/覆盖当前工作区。当前基线与测试源码哈希见platformer-combat-evidence/2026-10-15-p9。

完整PlayMode回归jobeee9afff723641de8f4594ed4c228df5最终600/600 Passed，0失败/0跳过，793.6514071秒；已保存逐用例名称、状态和耗时至platformer-combat-evidence/2026-10-15-p9/playmode-600.json。本轮新增仲裁/过滤使数量由598变600。正式PlayerWorkflow补第四个攻击Skill入链，PlayerCombat补隔离配置源码表与筛选/帧率边界。矩阵74项编号完整、所引用测试名称均存在；对应本轮通过断言已更新，细分缺口尚未放行。

Editor回归job88172f1550494ccbb21117549fc0b3b0正在运行114项；传参误写Game.PlayerDataStorage.Tests，实际存储程序集为Game.PlayerData.Storage.Tests，需后续单独补47项，不将本次114项当作原161全量。

Editor job88172f1550494ccbb21117549fc0b3b0最终114/114 Passed，161.3746389秒，明细editmode-114.json；正确存储程序集单独job58e4dc48eebe4ea8b9510ae5aa04f611最终47/47 Passed，2.353363秒。两组共161项，均有本轮结果，不依赖上轮推算。

覆盖复核后新增6个隔离测试：AirAttackLandingCancelsOnlyAirExclusiveSegment(Air/Both)；DelayedInterruptionReleasesOnlyItsPromptAndStopIsIdempotent(jump/hit/death/stop)。补A05反向环境及R09/S03部分打断清理缺口；不改正式图/方案/资源。refresh(wait_for_ready=false)已请求成功，最新编译及6项测试待确认；先前600/161通过不自动覆盖这些新用例。
补测jobbf9323d4e554490d915e6b4200eab1e2报告两项环境夹具Controller未保存，未放宽保护；仅保存其测试Controller后重跑job274faf9bac9349f195efa29e1427a1ab最终6/6 Passed（90.9266065秒），证据coverage-6.json。随后新增墙滑/挂边竞争2项、第四攻击真实配置入口出招命中1项、左右原生命中镜像6项，均为隔离测试；最新刷新已请求，编译及9项结果待确认。
补测jobb1b00cf0c12e4f599c34394f9ae5c739最终9/9 Passed（35.3196994秒），证据coverage-9.json，覆盖A06/H07/T02的直接行为缺口。另增加SharedAndIndependentAttackAbilitiesSpendOnlyEnabledSegments共4例（共用/独立绑定×第三段开关），Console0errors，已启动jobe819f558689d4963addbee3c8cbcab2b，结果待查。没有修改正式资源或默认方案。
共用/独立Ability组合jobe819f558689d4963addbee3c8cbcab2b最终4/4 Passed（37.4547401秒），证据ability-4.json。继续补DisabledTargetIsRejectedByEveryCombatRouteWithoutSpending(entry/normal/delayed/dash)4例验证段开关各路由，刚请求刷新，编译/结果待确认。

### 2026-10-15 P9本轮收口状态

DisabledTargetIsRejectedByEveryCombatRouteWithoutSpending首次编译发现RemainingUses为int?，改为var后Console0errors；jobd0d0404835a34d0e857d4ef00052ac93最终4/4 Passed（60.2860562秒），证据routes-4.json。共用/独立Ability、段关闭4路由、A05/A06、R09、H07、T02新增23项均已有通过证据。修改仅Assets测试及正式文档、docs/dev证据；未改正式方案/资源或框架API。

最终Unity实时读取：Options=1/Enabled=true，isPlaying=false、isCompiling=false，MainStage；FrameworkScene dirty=false；默认PlayerDefaultScheme且configDirty=false；攻击Skill执行blockReason=null。最终Console0errors。本轮Unity连接持续可用，没有以其他方式替代编译。此前完整PlayMode600/600及Editor+Storage161项结果保留；新增23项独立执行，不把旧600报告改写为623。测试期间夹具错误及0项筛选均已记录，未计为通过。

正式文档：PlayerCombat配置/默认数值/资源/策略/隔离测试位置和诊断，README/PlayerWorkflow/PlayerArchitecture/PlayerRules的索引、快照v3和图边界已同步；相对链接与git diff --check通过。验收矩阵74项均有对应证据/范围，明确实播、静态、逻辑、PlayMode来源。

未完成且不得放行：T03实际菜单/模板身份/模块关闭及旧窗口身份变化的交互验收。仓库business-directory-layout.md明确“需要实际切换模板时由用户操作”；已有静态守卫和隔离项目Skill测试不替代实际Unity菜单实测。未为此切换模板、修改模块Enabled或伪造身份。S03图故障证据为候选图创建/进入失败回滚，未单列正在攻击中破坏图的故障注入。P9主体实现/回归及正式文档已收口，但阶段整体完成标记保留未通过，下一步是用户在保留草稿的安全测试工作区完成实际切换后核对T03。无需再次全量重跑已通过测试，除非新改动或新失败提出必要性。

未执行模板保存、Bump、Git提交、推送、发布或消费项目升级。

### 2026-10-15 P9继续：T03当前身份原生菜单核对

后续用户要求三连击范围再翻倍：仅通用攻击模板1-体验调校的三段判定尺寸/偏移及刀光比例/偏移乘2，前沿由3u到6u，默认与草稿不变。更新测试靶点5.4/6.6u；Console0errors，专项jobd3a38ac1899447e4aa0cd1622095bf0c最终8/8 Passed（1.0232941秒）。新6u版本未追加实播截图，T03剩余仍未验证、不切模板。

T03最新恢复收口：本轮确认Player实际Enabled=true、菜单恢复可用、Options=1/Enabled=true、Console0errors。测试候选完整Attack匹配；其余窗口业务字段与t03-draft.json比较仅当前分类有变化，Combo/来源/Clip列表/基线均一致。默认配置与after.json相同。已按用户要求恢复原窗口、仅销毁本次测试候选并更新窗口缓存，t03-restored.json窗口序列化字符串与after.json完全一致。未重复关闭、未切模板、未改默认方案。模块关闭/域重载/重新启用/草稿保留链已验证，其他T03身份及跨模板旧草稿恢复仍未验证；矩阵已同步，不标整个T03通过。完整证据位于feel-fixes/t03-enabled.json及t03-restored.json。

T03最新续验：重连读取已加载PlayerModule.Enabled=false，源码true，说明上次源码恢复尚未反映到加载程序集。Options=1/Enabled=true；原生菜单exists=true/enabled=false、Available=false，Require拒绝“Player 模块已关闭。”，旧窗口仍在。已取得关闭入口/守卫和候选_attack跨重载保留证据，t03-disabled-state.json的_attack与关闭前完整一致，.47标记保留。非候选Feel引用为空符合RestoreCombatDraft只还原Attack逻辑，不声称整个Feel JSON一致。随后ForceUpdate导入true源码，20秒后Console查询ping失败，停止自动Unity验证。重新启用及原窗口恢复未完成，测试候选保留；下次不要重复关闭，先确认启用、核对Combo/素材等其余窗口字段，再按after.json恢复原选中和空候选。未改默认/未切模板。本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；仍报错请提供首条错误及完整堆栈。

用户授权代执行模块开关测试后：已备份现存测试草稿t03-before-disable.json，Options=1。按实际EmberModuleAttribute.Enabled正式装配开关仅改PlayerModule.cs的一处true→false，请求编译成功；20秒后Console首次查询ping not answered，停止Unity验证。已将同一源码开关恢复true，未操作草稿/默认/模板。编辑器加载状态尚未确认，T03关闭/重开均未通过；下一轮先确认恢复编译、Enabled及候选.47完整性，再决定重复该有界测试。说明修正：此前称“模块管理入口”不够准确，本仓库没有找到该面板，真实入口是类型特性。详见feel-fixes/notes.md。本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；仍报错请提供首条错误及完整堆栈。

体验调校续验：本轮Unity编译确认成功，Options=1，8项FeelFix专项job5e3da8de474248878991b789fe48df90全部通过（1.1265873秒）。正式隔离入口实播三连击扩展距离命中沙袋HP30→23、刀光scale1.6及截图；空冲正常Completed退出16→4；短受击life.hit→idle时Sprite y恢复0且后续无残留。首个空冲场景撞墙中断不计通过，已保留轨迹；实时刀光帧未全部捕齐。证据及边界见platformer-combat-evidence/2026-10-15-feel-fixes/notes.md、combo-live.json、dash-live.json、hit-live.json、slash-left.png。仅调校副本生效，未改原内置或默认。

实播后停止Play，Console0errors；默认配置与白盒选择不变，场景未脏，无遗留CombatFix虚拟设备。原窗口无候选，状态已复核导出after.json；随后按原计划准备T03：在原窗口选择独立调校副本，建立第一刀Duration=.47的未应用测试候选（资产仍.45），完整备份t03-draft.json。测试结束需恢复原窗口选择/空候选。未关闭Player、未切模板；下一步由用户正常关闭Player后查询菜单禁用/旧窗口保护并对照候选，之后重启用验证恢复。整体T03尚未放行。

最新体验修正实施：本轮连接先成功，实时platformer-2d 0.4.0、Options=1/Enabled=true、Skill block=null。窗口和SessionState攻击候选均空；已将窗口/缓存/默认配置/白盒身份导出到platformer-combat-evidence/2026-10-15-feel-fixes/baseline.json，未覆盖用户窗口。通过Copy创建通用攻击模板1-体验调校.asset，不修改默认与原内置方案。三刀前沿扩至3u、刀光比例1.6/偏移1.2、空冲倍率.25；详见该目录notes.md。

受击实测定位为动画位置残留：Hit .1秒时y=.2142857，切idle后仍保持该值；状态本身在单次受击后能回idle。给调校副本的基础动画创建独立副本并增加位置归零曲线，原共享片段不变；实际Animator验证Hit→idle/move/fall均回y=0。已新增8项专项测试，但刷新成功后Console查询ping not answered，立即停止后续验证，尚未编译确认/运行测试，不把Animator定点证据等同全部行为通过。停止了本次隔离Play；Player保持启用，T03未推进。下一轮先重连确认编译，运行PlayerStateTests.FeelFix_前缀8项并修复失败，再补实际输入/刀光观感和默认/草稿复核，三项通过后继续T03。本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；如仍报错请提供首条错误及完整堆栈。

后续优先级调整：用户切回platformer-2d且尚未关闭Player，要求先修复三连击距离短、空冲退出减速不明显、受击动画疑似卡住，再继续T03。磁盘身份platformer-2d 0.4.0，loadedAt 2026-10-15 14:33:55。首次Unity editor/state查询ping not answered，本轮立即停止Unity查询/自动编译测试；未关闭模块，未读取/覆盖内存草稿，未修改默认方案或业务资源。实时Options及草稿基线尚未确认。

只读分析：通用攻击模板1三段Box offset.x=.9、size.x=1.4，前沿距玩家中心1.6u；后续需配套扩大判定与刀光并实测有效距离。Feel.Attack.Dash的_airVelocityRatio=1、_airBraking=30、_airTurning=50；PlayerCombatDashMotion.EndDash正常空冲退出直接乘该倍率，因此确实保留原速度，已有字段可将候选改为.25，无须先改公共API。Hit.anim长.3s且不循环，含Sprite局部位移及缩放曲线（.1s缩放.7/1.4285715，.3s回1）；需实播区分life.hit未退出、持续接触再受击、动画退出后残留变形，尚未确定根因。PlayerSchemeWindow.Draft.cs使用SessionState缓存窗口及攻击草稿，仅在关闭/域重载缓存，不能用磁盘资产当作内存草稿备份。重连后先只读导出窗口/SessionState与默认/白盒基线，再做独立候选调参和受击诊断，不重新读取覆盖用户候选。

本轮仅维护记录；三项未修复、未验证，不沿用此前测试宣称通过。本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；若仍有报错，请提供首条错误及完整堆栈。

最新base重连验证（2026-10-15）：editor/state本次成功，随后逐次检查只读查询结果；实时编辑身份base 0.8.3，loadedAt=2026-10-15 14:16:49，platformerActive=false。Unity原生MenuItemExists返回玩家菜单不存在；GetTemplateSkillExecutionBlockReason(ember-platformer-add-attack)返回“此技能没有模板所有权记录。”。Options=1、optionsEnabled=true，非Play/非编译中，FrameworkScene dirty=false，Console error查询0项。T03无关模板的菜单隐藏与Skill执行限制取得实时通过证据。

窗口及类型核对：PlayerEditorAccess、PlayerSchemeWindow、PlayerDevelopmentEnvironment均未加载；全部EditorWindow枚举中没有玩家窗口，当前无残留可操作入口。本轮没有经历窗口跨身份变化过程，也没有切换前草稿内容基线，不能将窗口缺席等同于草稿已完整保留/可恢复，旧窗口失效过程及草稿恢复保持未验证。未尝试重建缺失类型、打开旧窗口、读取或覆盖草稿；未切回、未修改默认方案或base资源。只更新实施记录及矩阵，无项目代码改动、无新编译请求或测试运行；Console0仅表述当前错误查询结果。其余T03场景未整体放行。

后续base实测尝试：用户已自行保存模板并切到base，授权仅验证、不自动切回。磁盘EmberEditingTemplate.json为base 0.8.3，loadedAt 2026-10-15 14:16:49；Assets/Game/Module/Player和.agents/skills/ember-platformer-add-attack均不存在。实例列表返回ember-unity-framework@b23e9b768e85ed30，但首次editor/state返回ping not answered。编排脚本错误地仍发出后续只读execute_code/read_console，两者也失败且无有效数据；没有重试、编译刷新、替代验证或Unity写入。此处记录执行顺序偏差，后续须逐次检查查询成功再发下一调用。实时Options、菜单消失、旧窗口与草稿状态、Skill执行拒绝均未验证，不能使用切换前结果替代。仅更新docs/dev记录，未切回、未改base业务或用户草稿。本次未完成 Unity 编译验证，请在 Unity 中手动触发编译；若仍有报错，请提供首条错误及完整堆栈。

后续用户明确决定T03保留未验证、不切换当前项目。本次按此决定整理PlayerCombat.md的Unity体验步骤、真实按键、白盒与正式指定方案入口区别及剩余验收边界；不再要求用户准备切换环境。仅增量修改说明和验收状态，没有操作Unity窗口、默认方案、白盒偏好或用户草稿，没有新增编译/行为测试结果。既有测试结果保持其原日期和范围，P9不改写为全项通过。

Unity session f7594411-ab15-46de-b805-df0d6d7226fb连接成功。实时编辑身份仍为platformer-2d 0.3.1（loadedAt 2026-10-13 21:18:16），Options=1、Enabled=true，非Play且非编译中；FrameworkScene dirty=false，已有PlayerSchemeWindow保持打开，未操作其草稿。PlayerEditorAccess.Visible/Enabled均true。MCP menu-items列表未包含动态入口，因此没有据此判为漏注册；反射核对当前Unity实际提供的MenuItemExists/GetEnabled签名后只读调用，Ember/Player/玩家开发工具两者均true。当前所属模板的原生菜单存在/可用已取得实时证据；其他身份和模块关闭交互仍未验证。

只读查询最初使用GetInstanceID触发Unity 6过时API编译诊断，移除该非必要字段后查询成功；未修改项目C#，不是项目编译失败或连接失败。本次没有重新触发项目编译或重复完整回归，沿用上一节明确记录的验证范围。

继续条件：由用户在保护草稿的独立测试工作区准备合法派生、无关/无效身份及消费部署环境，并完成需要的实际模板切换，再连接该Unity实例逐项验证菜单显示、操作许可和旧窗口失效；模块关闭/重新启用及域重载也须在该安全环境检查。不得在当前工作区伪造身份、覆盖模板或丢弃草稿。依据business-directory-layout.md第82行“需要实际切换模板时由用户操作”，当前没有新的安全测试环境，因此T03及P9整体仍未放行。
