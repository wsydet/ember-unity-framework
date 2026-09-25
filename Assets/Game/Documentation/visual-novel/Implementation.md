# 视觉小说模板实施清单

## 当前交付与验证（2026-09-22）

**当前结论：E0–E3 已获用户确认通过，E3 文档已收束；E4 测试已获用户确认通过、文档已收束；E5 镜头、背景擦除、预设与试播增量已获用户确认全部通过、文档已收束。** E2 的历史反馈为“测试全部通过，没有报错”。 本次依据用户反馈登记通过，未收到新 XML 或测试总数，也未由代理重跑。本次基础框架收尾版本为 0.6.0 / preview（父模板 base 0.6.4），E1–E5 与制作工具增量纳入正式 SaveTemplate / Bump；实际版本与 hash 以模板 metadata 为准。详见 [E2 测试通过与文档收束](#e2-测试通过与文档收束)。

**编辑工具最新结论：主 UI 布局与外观编辑、单节点编辑态演出试播已实现；2026-09-22 两处测试问题修复后，用户确认“通过”。** 未提供修复后 XML、复跑范围与总数，不推定为全项目 482/482；详情见 [编辑工具测试通过与文档收束](#编辑工具测试通过与文档收束)。E3 随后获用户“全部成功”反馈，文档已收束，详见 [E3 收束记录](#e3-用户验收通过与文档收束2026-09-22)。E4 随后获用户独立确认“测试也通过了”，见文末 E4 收束记录；E5 随后获用户独立反馈“全部通过，收束文档”，已登记通过并收束；依据见文末 E5 收束记录。

下文保留各阶段的实际验证与失败修复历史；早期“未测试”“待复验”描述的是当时状态，不覆盖上述最新结论。

历史封存 **visual-novel 0.5.0 / preview**，父基线 `base 0.6.4`；该版实施 E0 演出执行基础，沿用现有阅读、配音、稳定点存档和隐藏白框保护。通过项目中心 SaveTemplate 保存后显式次版本 Bump；未发布框架 tag、未部署消费项目、未提交 Git。

| 版本 | 本批交付 | 验证范围 |
|---|---|---|
| 0.4.1 | 对话底部黑色托底/透明渐变；历史全屏暗化、姓名/正文两列、最新记录标记与滚动条；打开历史隐藏底层对话和工具 | MCP 编译无错误；正式 Prefab Binding 校验；短/长/空历史预览；长历史滚动位置与滚动条比例检查 |
| 0.4.2 | Advance 命中区覆盖底部渐变；姓名与正文不挡点击；保留原 Space/Enter 绑定并验证 | `DialogueRegionAndSpaceAdvanceWithoutClickThrough` 1/1 通过：真实 UI Raycast 命中、点击推进、模拟 Space 推进、历史打开时 Space 不穿透 |
| 0.4.3 | 正文右侧留出推进箭头空间；阅读菜单压缩上下留白、移除末项分割线 | 1920×1080、1440×1080 静态渲染检查；正文与箭头边界断言；MCP Console 无错误；没有新增运行回归 |
| 0.4.4 | 合并当前说明、更新使用规范和验收边界，保留历史证据 | 文档本地链接和模板副本一致性检查；不重跑与文档无关的运行测试 |
| 0.4.6 | 新增演出能力实施计划及文档入口；E0–E6 均未实施 | 文档链接与模板副本一致性检查；无运行代码变更，不新增运行测试结论 |

0.4.2 通过报告为本地 `.utmp/visual-novel-m3/tests-20260921-074202711.xml`。测试暂时使用独立 InputSettings 副本排除编辑器失焦干扰，完成后恢复原设置；没有改变项目的键盘焦点规则。Game 视图失焦时空格不会送入游戏。先前失败运行分别暴露测试坐标未使用 UI 相机、后台键盘输入被编辑器拦截，修正测试后取得该份通过报告，不把早期失败报告改写为通过。

本轮布局预览保存在本地 `.utmp/novel-reference-qa/`；这是忽略目录中的工作证据，不是随模板分发的资源。较早文档插图继续保留来源语义，不代表 0.4.3 的最新几何布局。

### 0.4.5 隐藏白框修复

正常 Hide 原先已在 alpha 归零后清空 Sprite，但空槽/重复 Hide 会重新写入 `1 - progress` 的非零 alpha；uGUI Image 在没有 Sprite 时因此绘制白色矩形。PortraitItem 与 BackgroundItem 同步增加空图保护：有图才淡出，完成后依次禁用 Image、归零颜色、清空 Sprite；再次 Show 时恢复 Image。进度限制到 0–1，缺图 Show 也保持不可见。

MCP 编译无错误。正式两个 Item Prefab 的 `VisualHideKeepsSpriteUntilTransparentAndNeverShowsEmptyQuad` 参数用例 **2/2 通过**，覆盖首次空槽 Hide、正常中间帧保留 Sprite、完成清理、重复 Hide、再次 Show 和缺图 Show。MCP job：`26c302e1a9fe4063a187abe6f0f59c17`。这是状态专项，未声称完成本次整部剧情的逐帧画面复测或全量回归。

### 0.5.0 E0 演出执行基础（2026-09-21）

实现范围：`NovelActions` / `NovelSession.Actions` 定义会话动作句柄与实例寻址；新 Opacity / WaitActions 指令贯穿 SO、校验、内容编辑、观察窗口、正式 EUI 用户逻辑与 Schema 2 存档。原枚举值不变，无 E0 字段的故事保留旧指纹。资源加载、音频、暂停与 Loading 使用原 API；没有修改框架 API、Binding、正式 Prefab、LastLight、配表源或生成 bytes。

同属性接管立即取消旧句柄；延迟、暂停、倍率、并行对白、等待组、快进收敛、稳定点目标值、恢复后等待、退出清理均有测试。Hide 从当前透明度继续；显式 Replace/Hide 按实例寻址；显式 Show 冲突报错，旧空 ID 槽位命令保持兼容。舞台透明度只乘背景/人物，不影响阅读 UI。遮罩、效果创建和位移未实现；E1–E6 不在本批。

实际验证：

| 批次 | 结果与证据 | 范围 |
|---|---|---|
| 首轮 E0 | 4/5；`tests-20260921-082112829.xml`，MCP `0b53a710a2ae4dcdaae7c33b0b772ade` | 测试适配器错误覆盖舞台/人物透明度，修为与正式页一致的叠乘，保留失败报告 |
| 小说程序集回归 | 116/117，0 跳过；`tests-20260921-082558119.xml`，MCP `dcedae7555c6419687b16fb06cdf067f` | 含 LastLight、正式 Gameplay、阅读/Voice、存档、编辑器与 5 项 E0；唯一失败为手造 Schema 2 背景缺实例 ID，修复夹具为合法 background 身份 |
| 最终会话/存档/阅读专项 | **56/56，0 失败、0 跳过**；`tests-20260921-082922083.xml`，MCP `a596e2b671c745b2bdd2ad871d463b7f` | 含 7 项 E0、上述失败复验、最终 Hide 衔接、实际 Item 与完整 Reader 隔离渲染；不把多轮相加成单次全量通过 |
| Unity 编译 | 最终 C# 刷新经 MCP；Console error 0 | 无 dotnet/BatchMode 替代验证 |
| 正式页渲染 | `.utmp/visual-novel-e0/frames/`：start、middle、paused、end、restored、stage-multiply | 1920×1080 舞台、960×540 输出，开始/中间/结束/恢复已查看；暂停数值不变，恢复目标 alpha=.2/.4，舞台 .5 后=.1/.2；阅读 UI alpha 不变，Prefab 序列化不变 |

XML 均由 Unity Test Runner 原生回调保存在 `.utmp/visual-novel-m3/`，本机证据不随模板交付。全程序集跨 PlayMode 域重载后 MCP 进度停留在 12/117；确认原生 XML 已结束、Editor 已退出 Play 后才清理失效任务。没有把失焦提示或停留进度视为成功。一次读取 TestRunner 状态的临时代码因该版本不存在所查询 API 而失败，不涉及项目代码编译；改用原生结果和编辑器状态核对。

独立示例 `Config/Narrative/PresentationE0/Story` 通过 Unity 创建，含双实例、延迟并行对白、等待组、接管、按身份换表情及顺序舞台透明度；默认新游戏仍为 LastLight。正式 UI 复用现有 EUI 绑定，不新增骨架。项目中心完整预览为 39 项 E0 差异、0 错误、0 警告，未夹带场景、正式 Prefab 或配表改动；保存封存使用项目中心 API，完整差异预览与保存记录见本机 `.utmp/visual-novel-e0/`。

剩余验收：本批没有新一轮人工点击菜单/操作存档/通读完整 E0 示例；正式页渲染与自动会话驱动不等于交互式人工验收。没有重跑最终单次全项目测试、移动端/异形屏、Player 构建或消费项目部署。后续入口是 E1“立绘移动与角色强调”，需另获下一批指示。

### 尚待完成

- 按[演出能力实施计划](PresentationRoadmap.md)从 E1 继续；E0 已实现并完成上述自动验证，交互式人工验收边界保留。E6 按项目需求评估。
- 人工全功能确认、移动端/异形屏、大字号与极长文本边界；16:9/4:3 静态预览不能替代这些验收。
- 新单次全量报告、Player 构建和消费项目部署验证。本轮专项 1/1 不与以前的 3/3、14/14 或用户确认合并成全量通过数。
- 按发布流程发布框架 tag 后，在消费项目获取并验证模板；本次没有部署到 Call Me Heartless。

当前使用入口以 [README](README.md)、[UI 规范](ReadingUILayoutProposal.md) 和 [编写说明](Authoring.md) 为准。后续维护在本节更新当前状态，不再在首页叠加多段“最新状态”。

## 历史批次记录

以下内容保留当时版本、失败报告、人工确认及工具可用性。旧批次中的“未保存/Bump”“M5 未开始”“MCP 不可用/请手动编译”均只适用于对应批次，不是当前待办；不得将历史失败报告覆盖或拼接成新的通过报告。


> 当前目录归属及迁移规则见 [目录规范](DirectoryLayout.md)。历史记录中的旧路径仅用于追溯。

> **最新收束（2026-09-21，用户确认复测全部通过）**：双结局测试补齐 Wait 计时驱动后，用户确认“全部通过了”，据此关闭本轮两项失败，完成本批修正与测试记录收束。最新已归档 XML 仍为 382 项、380 通过、2 失败；复测通过来自用户确认，未提供新 XML，不声称取得单次 382/382 或 MCP 自动通过报告。编辑器/观察四组已有 XML 14/14 证据。M5 工作副本尚未保存/Bump，人工画面与消费端验收保留，不能把本批测试收束等同于 M5 全部交付。

> 下列较早状态保留批次语义；其中“尚未复测”“无新测试报告”等不覆盖上方最新确认。当前待办以 M5 分项及末尾收束记录为准。

> **最新确认（2026-09-21，Unity 6.5 API 警告修正后）**：用户反馈“已连接无报错”，记录为本批改动后的人工编译无报错确认，不记为 MCP 自动编译通过，也不推定全部警告清零或运行回归通过。当前任务工具清单仍无 Unity MCP 入口，尚无新测试报告。M5 编辑器/运行验收、保存/Bump 及消费端交付继续待办。

> **最新接手状态（2026-09-21，M5 继续实施）**：实际工作副本已包含 M5 多批实现，并非尚未开始。当前封存仍为 visual-novel 0.4.0 / preview、父 base 0.6.4；编辑记录与两项封存 hash 均为 `9387f8affe4f0ad9370e6e09a50b653c`。本次修正观察定位的剧情身份范围，新增同章节/节点 ID 跨剧情回归，并把 Gameplay 布局菜单纳入模块开关测试。当前工具未提供 Unity MCP，本次未编译、未运行测试、未保存/Bump、未部署；先前 3/3 不覆盖此增量。最新事实以本条、M5 分项及末尾记录为准，早期收口段落保留历史语义。

> **最新工作状态（2026-09-21，完整示例更新）**：LastLight 已按黄昏天台改写为 79 句、两路线两结局的可仿写示例，补齐旁白、内心、居中/双人/空镜、选择、变量分流及两句合成 Voice。MCP 编译与全流程静态校验无错误，三项 Gameplay 回归 3/3 通过（含两结局和演出覆盖断言）。详见 [示例体验与仿写](SampleWalkthrough.md)。人工全功能清单、手机/长句边界、M5 封存与消费项目验收仍待完成；封存版本仍为 0.4.0 / preview。

> **当前封存：visual-novel 0.4.0 / preview（2026-09-20，M3＋M4）**。父基线 base 0.6.4，未发布。M3 存档/菜单/Loading、Gameplay 布局与节点预览、M4 阅读辅助与配音及新游戏揭幕修复已收入本版；历史验证与剩余验收边界继续保留；M5 新增工作副本尚未封存。
> 此条为最新交付状态；以下各日期记录中的“未保存/Bump”和旧版本号仅代表当时状态。

> **M4 收束（2026-09-20）**：阅读辅助与配音功能验收通过，文档收束完成。新游戏揭幕增量已通过 MCP 编译及专项 1/1，实际画面确认仍保留；未保存/Bump，M3 其他待办独立跟踪，M5 未开始。

> 日期：2026-09-18 M3 收口核对。用户确认常规功能实测无问题；上一轮 Unity 编译及 UI 50/50 回归通过。完整故障专项证据、最终全量结果与保存封存仍待完成。封存仍为 visual-novel 0.3.1 / preview，父 base 0.6.4，未发布。当前状态以下方收口清单为准；后文各批记录保留历史语义。M4 功能验收于 2026-09-20 通过，保存封存待完成；M5 未开始。
> 设计依据：[Design.md](Design.md)；模板定位：[README.md](README.md)。

## 执行方式

依序推进，每阶段先完成最小闭环再扩展。完成项必须附实际文件、验证方式和结果；没有 Unity 实测的项目不能勾选运行通过。模板保存通过项目中心完成，每个可交付阶段显式封存版本。
用户已授权并完成 M1、M1E、M2，随后授权 M3，并逐项接受三项关键方案；本次确认实测无问题并要求收束 M3。随后用户明确授权本轮实施 M4；M3 剩余验收与封存保留，不作为 M4 授权阻塞。2026-09-20 用户明确授权 M5，当前正在实施，无需重复授权。以下历史勾选表示对应实现已落盘，Unity 验证范围以相应证据记录为准。

## 当前收口与接手清单（2026-09-18）

这是当前状态入口；后文“本轮”“尚未运行”“等待下一阶段”等措辞只对各自历史批次有效，不覆盖本节。

2026-09-20 收束结论：M4 原定功能清单已验收通过，阶段实现与文档收束完成。自动证据为 13 项 M4 全通过、最新新游戏/读档专项 1/1；全量 XML 保留为 431/432，观察窗口唯一失败项由用户后续确认通过，不合成为新全量 432/432。新游戏揭幕增量的人工视觉确认未收到，单独保留。保存封存尚未完成，不将工作副本收束表述为已交付新模板版本。

**最新验收：** 揭幕后残留读档中提示已修复，用户先确认“没有编译报错”，再针对继续游戏、快速读档、手动读档确认“测试没有问题”。该穿帮问题按人工验收关闭；最新代码编译记录为人工确认，不标为 MCP 验证。完整自动化报告与其他待验、封存项保留如下。

| 项目 | 当前结论 |
|---|---|
| 身份与交付 | visual-novel 0.4.0 / preview 已封存 M3＋M4，父 base 0.6.4，未发布；通过项目中心保存/Bump，不从旧快照覆盖 Assets |
| M3 存档实现 | 稳定 ID 快照、显式全局/局部变量、语义兼容校验、候选准备/提交、可靠写盘及成功指针、6＋1＋1 槽位、独立已读/偏好；已有专项证据，整体验收未完成 |
| M4 工作副本 | 功能验收通过：13 项 M4 用例有 XML 通过证据；观察窗口专项复测及手动验证由用户确认通过。未保存/Bump，见末尾 2026-09-20 验收记录 |
| 正式 UI | 五按钮主菜单与条件显隐、八行槽位、继续直接恢复、读取先选槽；统一无进度条 Loading 持有至恢复就绪，准备取消/失败保留原游戏 |
| Gameplay 编辑器 | 实际阅读页绑定定位、拖动/数值布局、Undo、保存保护与备份；预设/自定义分辨率，节点选择联动、逐句静态画面与全部选项文案；最新代码已随前轮 Unity 编译，用户反馈实测无问题，但未逐项提交编辑器边界场景记录 |
| 边界 | SO 唯一权威；预览不执行变量/分支/音频/存档，不猜测前置分支画面；PlayerDataModule 保持关闭。本轮新增 M4 阅读辅助、Voice 和偏好；迁移编辑器与 M5 不在范围 |

### 验证证据与适用范围

下表保留不同批次证据；此前启动的 419 项 EditMode 回归结果查询超时，不能记作最终通过。本表记录 M3 历史证据；M4 本轮实现及验证在末尾单列，不能沿用这些通过数量。

| 验证批次 | 实际证据 | 能证明的范围 |
|---|---|---|
| M2 基线 | 小说 50/50、全项目 380/380，见 M2 封存记录 | 仅 M2，不证明 M3 |
| M3 首轮与修正 | 用户 71/73；`tests-20260918-023256858.xml` 为 73/73 | 当时小说实现；首轮两个失败及修复保留在后文 |
| 用户已提供全项目 XML | `user-tests-20260918-143255.xml` 为 412/413，0 跳过 | 历史唯一失败为观察窗口 Play Mode 超时。修正后用户反馈测试通过，未提供新的完整 XML，不杜撰通过数量 |
| 关闭动画生命周期修复 | `transition-lifetime-tests.json`，最终 UI 50/50 | 前轮 Unity 编译完成；含新增 6 项打开/关闭阶段间存活、销毁、释放回归。首次 2 项正常日志预期失败已修正并整组复测 |
| 用户最终常规实测 | 对话确认“我测试没什么问题了” | 记录常规交互验收通过，不推定未明确执行的故障注入或性能基准也已通过 |
| 揭幕时机修复后人工验收 | 用户先确认“没有编译报错”，随后确认“测试没有问题，收口文档” | 最新修复编译及继续游戏、快读、手动读档人工复测通过，关闭揭幕后残留读档中提示；未提供新增自动化 XML |
| 本次全量回归 | job `71ce7d76d4f04a05a5cf1c16d9a90a3d`，419 项 | 最后成功查询完成 238 项、无失败；后续 TimeoutError，最终结果未知，不自动重开第二轮 |
| M3 较早全项目 | `tests-20260918-023911990.xml` 为 402/403；`tests-20260918-024542053.xml` 为 403/403 | 后一报告已取得，但早于 UI/异步存储、五按钮、Loading 和布局预览增量，不能当作最终全量通过 |
| 五按钮主菜单 | `tests-20260918-040110939.xml` 4/4；`tests-20260918-040301204.xml` 1/1 | 显隐/收拢与新游戏、继续、手动槽入口流程 |
| Loading 与恢复 | `tests-20260918-042906427.xml` 27/28；等待时机修正后 `tests-20260918-043113844.xml` 1/1 | 两轮合计覆盖该 28 项，不是单次 28/28；含慢页面遮挡、单一 Loading、损坏保留、取消和再次读取 |
| 布局工具初版 | `.utmp/visual-novel-ui-layout/validation.json` 及本节后方工具记录 | 拖动/Undo、保存往返、域重载草稿与外部冲突保护；早于最终节点预览源码 |
| 节点内容 | 同目录 `preview-validation.json` | 第二句、配表姓名、隐藏左立绘、SO/Prefab 不变；其中 colors=1 是当时渲染失败，不能作为画面通过证据 |
| 相机修复探针 | 同目录 `render-validation.json`、`game-0/2/4.png`、`gamecam.png` | 临时应用 CameraType.Game 后三种比例正常；最终写入源码后的编译查询失败，因此不等于最终成品通过 |

XML 未写目录时均位于 `.utmp/visual-novel-m3/`；本机证据不随模板交付。关闭动画生命周期修复有 MCP 编译及 UI 50/50 证据；之后的揭幕时机修复由用户确认编译和人工实测通过，不能将较早的 50/50 当作覆盖这次增量。此前 MCP 超时记录保留历史语义；M4 本轮有源码与资源改动，验证边界见末尾记录。

### 剩余验收与交付顺序

- [x] 关闭动画生命周期修复经 Unity MCP 编译及 UI 50/50；最新揭幕时机修复由用户确认无编译错误。这是 M3 历史证据，不覆盖 M4 代码。
- [x] 用户确认继续游戏、快速读档、手动读档揭幕复测无问题，关闭残留读档中提示的问题。
- [x] 用户确认常规交互实测无问题；此前的 TestPage UIUpdate 警告已核对为保护用户代码的测试预期，非正式页面失败。
- [ ] 编辑模式实际验收：打开布局窗口，图/Project 选择节点、锁定跟随、修改内容刷新、上一句/下一句、选择文案、缺资源提示；切换四个预设和自定义比例。节点内容不写回布局，布局预览不改 SO。
- [ ] 编辑器完整验收：拖动/数值调整、Undo、保存后重载与 Gameplay 效果一致；外部修改拒绝覆盖；源码重载草稿保留；关闭保存/放弃、Play Mode 与模块禁用门控。初版证据不能替代最终代码复验。
- [ ] 取得当前最终工作副本全量回归完整 XML：本轮 419 项已启动，结果查询超时；先检查 Unity Test Runner 是否结束，不要在仍运行时重开。用户常规实测已接受，自动化证据仍需归档。测试使用隔离目录，避免写入开发者真实存档。
- [ ] 补齐提交后页面/音频不可恢复故障返回菜单，以及观察窗 Restoring→取消/失败恢复旧源→成功只跟踪新源的完整验证；大体积存档性能尚无基准，遮挡不等于消除卡顿。
- [ ] 同步最终实际验证结果，使用项目中心完整预览五个受管目录及每项差异，排除其他未提交改动后保存并显式 Bump；不直接编辑 Templates~/ParentSnapshot~/hash，不提交 Git、不发布 tag。

上述为继续保留的 M3 待办。本轮另获明确 M4 实施授权，实际 M4 工作和验证边界见末尾最新记录；交付后等待用户“继续下一阶段”，不自动进入 M5。

## M2 收口与 M3 交接

以下为 M3 开工前历史交接；当前状态以顶部“当前收口与接手清单”为准。

- 当前加载模板必须为 `visual-novel 0.3.1 / preview`，父 `base 0.6.4`，未发布。M3 开工时项目中心比较为 0 差异、0 错误；本次新增交接文档尚未回存，不能用 Templates~ 覆盖 Assets。
- 已有 `NarrativeModule`（Gameplay，Enabled=true）、`GameTableModule`（Global，Enabled=true）、`NovelSession`、正式 Gameplay/NewGame、EUI 阅读/选择/背景/立绘、InputAction、Table/Resource/BGM/SFX 及只读观察。继续扩展这些实现，不重新创建同名能力。
- `NarrativeRunner` 尚无存档快照导入/恢复；`NovelSession` 尚无可持久化演出快照或候选会话交换；尚无 NovelSaveModule。PlayerDataModule 保持 Enabled=false，不能与小说存档重复持有同一剧情数据。DataSaver.Save 同步写盘且不返回成功结果，需要明确结果与可靠写入机制，是否提取框架 API 根据实际复用判断。
- 已确认决定见 Design §15：允许兼容改稿/排列调整，拒绝剧情语义变更旧档；候选会话提交前保留旧游戏、提交后不可恢复故障回菜单；6 手动槽＋1 快速槽＋1 自动槽及明确的自动保存/继续行为。后续具体技术方案不得扩大这些承诺。
- M3 范围按下方阶段清单：稳定点保存、全局/局部变量、演出状态、独立已读/偏好、有上限的历史数据、槽位索引、可靠写盘、恢复与正式存读档 UI；历史阅读页面、自动/快进和 Voice 留 M4，不扩展到 M4～M5。
- 保持 SO 唯一权威、稳定 ID/GUID、分层资源目录及独立 Editor 布局。恢复停顿点不得重跑先前赋值、SFX 或分支副作用；章节局部变量直接恢复，不能用默认值初始化覆盖。
- M2 验证：原生报告 `.utmp/visual-novel-m1e/tests-20260917-130617253.xml` 为 50/50；`tests-20260917-131027932.xml` 为全项目 380/380。Loading 销毁异常已修复；真实阅读/选择画面、暂停观察、音频句柄和返回菜单释放已检查。这些仅是历史基线，M3 必须重新验证。
- M3 开工备份：`.utmp/visual-novel-m3/before/Assets/{Game,GameResource}`；`status-before.txt` 保留大量既有未提交改动清单。本轮仅增加备份和三份文档，没有 M3 C#/资源改动、Unity 编译/测试、保存/Bump 或发布。
- Unity MCP 必须定位 `ember-unity-framework@b23e9b768e85ed30` 并核对项目根目录；不得操作 Unity Farm。编译严格遵守 CLAUDE，断连或首次查询失败后停止自动验证并交人工，不使用其他编译方式。
- 正式存读档页面/Item 继续使用 ember-eui-build 技能和开发中心；读取真实 Binding 后接用户逻辑。新增 Module 必须显式 Phase/Enabled，编辑器入口服从开关。日常改 Assets 或确有必要的框架源码，保存前完整预览五个受管目录，保留其他改动，不手改模板/父快照/hash。
- M3 完成后更新文档、记录实际文件与本轮成功/失败/遗留项，通过现有项目中心保存封存；不提交 Git、不发布 tag、不安装 Fungus，等待“继续下一阶段”。若需再问问题，直接在对话正文逐项提问，不仅依赖用户看不到的异步问答卡片。

## M1/M1E 收口与 M2 交接

以下为 M2 开工前的历史交接，当前状态以顶部“当前收口与接手清单”为准。

| 项目 | 当前事实与接手要求 |
|---|---|
| 工作基线 | 当前模板 visual-novel，派生 base 0.6.4；已封存 0.2.0 / preview，未发布。最新模块开关及文档仅在 Assets 工作副本；不得从 Templates~ 覆盖 |
| M1 已完成 | 独立 SO 与纯数据运行器、四表实际导出/加载、稳定 ID、分支/汇合/结局、输入令牌与暂停/循环保护、只读诊断；后来扩展 Story/章节出口及全局/局部变量 |
| M1E 已完成 | GraphView 两级总览与章节图、缩放/平移/吸附/排布/空格创建、固定尺寸缩略图、内容/设置页签、Undo、SO 唯一连接、Play Mode 只读观察、模块禁用时隐藏入口 |
| 模块现状 | NarrativeModule 已存在，Gameplay、Enabled=true，仅装配声明及空生命周期；M2 应扩展此类而非重复创建。GameTableModule 仍 Enabled=false，M2 核对实际依赖后显式启用并验证就绪流程 |
| 实际入口 | Editor/NarrativeObservationSample 只是测试宿主；正式 Gameplay/NewGame、EUI、Resource 演出与输入尚未接入，不能当作已有可玩模板 |
| 样例 | Assets/GameResource/Resources/VisualNovel/M1Sample/Story.asset；序章→校园/归途三章，原校园 GUID/稳定 ID 保留。按章与 Dialogue/Choices/Branches/Exits/Endings 分目录，布局独立放 Editor/Layouts |
| 最新代码验证 | 上一轮 MCP 编译错误查询 0；Unity 原生报告 tests-20260917-121818510.xml 为 44/44 通过、0 失败、0 跳过，包含实际进入/退出 Play Mode 的观察测试；完整路径与任务号见末尾模块启用记录 |
| 本次文档验证 | 仅核对源码、上轮报告和文档链接；没有重新运行 Unity。历史 17/23/27/41 项结果保留为各批证据，不混作本轮结果 |
| 未完成验收 | 修改 Enabled 源码并连续编译验证关→开、独立 Player 构建、完整消费项目部署、正式音画/UI/存档均未验证；不以 44 项测试替代这些验收 |
| 后续决策 | M2 演出槽位/过渡和资源所有权方案需区分常规实现细节与剧情/资产语义；关键选择推荐后确认。M3 修订兼容与会话交换失败策略、M4 Voice 留各自阶段，不提前实现 |

本次收口只修改 CLAUDE.md、docs/dev/ember-api-reference.md 和本目录三份文档。仓库规则及 API 示例明确要求新增 Module 显式填写 Enabled；修正“跨章连线仍延后”和“M2 重新创建 NarrativeModule”的旧表述。无代码/资源变更、无新增 Unity 验证、无模板保存/Bump、无 Git 提交或发布。历史记录保留以供追溯；当前状态以本表和各阶段清单为准。

## M0 — 文档与模板起点

- [x] 创建 `visual-novel`，派生自 `base 0.6.4`。
- [x] 核对当前编辑副本与新模板一致。
- [x] 建立定位、设计和实施文档，并在当前模板入口增加链接。

后续阶段都从本模板项目 Assets 开发。基础与 2.5D 模板保持各自归属；具体平台、分辨率和美术风格变化记录到设计，不隐式扩大首版范围。

## M1 — SO 流程数据、配表与运行器（已完成）

- [x] 确认 Design.md §9 的流程入口、节点目录/引用、条件分流和结局形态，再实现相关契约。
- [x] 定义对话段 SO 与独立选择 SO；对话段单后继，选择每个选项保存自己的目标；不在段内重复维护分支。另实现已确认的条件分流 SO、结局 SO。
- [x] 定义 chapterId、nodeId、commandId、optionId、lineId 等稳定标识及角色、变量模型；运行状态存入独立会话。
- [x] 复用现有 Table 接线，完成四表与稳定资源键查询闭环：Row/CSV/查询适配及用户生命周期钩子、Definition、生成 Binding/Catalog/ETBL 与实际加载测试。
- [x] 实现流程节点调度、线性段内 Say/SetVariable/Wait 执行与显式状态；选择等待合法输入，条件分流和结局按确认后的契约实现。
- [x] 加入入口/目标/类型/配表键校验、零选项报错、覆盖节点跳转与即时指令的总步数上限和会话取消。
- [x] 提供不依赖 UnityEditor 的只读诊断契约：会话代次、节点/指令位置、运行状态、等待/暂停原因、变量与错误定位。
- [x] 用纯数据与真实 SO 样例验证两个分支与两个结局，先不依赖完整美术页面。
- [x] 本轮 Unity 编译与针对性 EditMode 测试通过（17/17）。
- [x] 完整模板差异预览为 104 项，全部属于小说实现及三份文档；本批通过项目中心流程保存并封存为 0.1.2 / preview。

完成标准：同样输入得到同样变量与目标；分支可汇合，长直线可顺序拆段；坏 ID/变量类型/配表键有定位信息；重复推进不重复执行副作用；空段循环也不能卡死主线程；播放不改写 SO。使用有针对性的 EditMode 测试验证状态、分支与取消契约。

## M1E — 流程可视化编辑与运行观察窗口（已完成）

- [x] 确认编辑器技术与布局载体，建立独立 Editor 程序集。
- [x] 在图中创建/选取对话段 SO 与选择 SO，编辑段内指令、选项文字与条件。
- [x] 图与 SO 内容面板联动，段内指令列表显示角色/台词、背景、音频等可读摘要。
- [x] 对话节点提供顺序连接，选择节点为每个选项提供输出连接，支持分支和汇合。
- [x] 连线直接修改 SO 目标；布局不复制路线，Inspector 修改后图可刷新一致。
- [x] 支持保存、撤销/重做、复制生成新 ID、重排保留 ID、删除入链检查及错误定位。
- [x] 接入 Play Mode 只读观察：高亮当前 SO/指令，显示等待与暂停原因、变量和错误；提供跟随开关与定位当前 SO，区分浏览选中项与执行位置。
- [x] 窗口中途打开能读取当前快照，关闭重开清理订阅；换会话丢弃旧通知，退出时标明无会话/结束/故障，运行中禁止改写内容、连线或进度。
- [x] 根据用户卡顿反馈，将画布改为内置 GraphView；去除重绘中的磁盘读取、临时序列化包装与配表重载。
- [x] 加入滚轮缩放、中键平移、缩略图、空格搜索创建、端口拖拽、多选移动、对齐/网格吸附及可撤销的自动排布。
- [x] 本轮新增 200 节点画布导航、真实滚轮/空格事件、模板缓存、环路/汇合排布与 Undo 回归；复验实际 Play Mode 只读观察。
- [x] 按用户确认扩展为剧情总览 → 章节流程两级界面，独立章节出口与全局条件连线仍由 SO 唯一保存。
- [x] 支持 bool/int/string 会话全局变量与章节局部变量，按确认规则跨章保留或初始化；共用运行器、步数限制和输入令牌。
- [x] 窗口与节点 Inspector 拆分“对话内容 / SO 设置”，默认可读台词全文，运行诊断可折叠。
- [x] 缩略图改为固定尺寸、统一边界映射；规范化章节/类型目录与文件前缀，保留 GUID / 稳定 ID 迁移 M1 样例。
- [x] 模块启用约束覆盖菜单、SO 创建、打开/恢复窗口、Inspector 与写入入口；最新代码批次 44/44 测试通过，验证限制见收口表。

完成标准：在编辑器中完成“对话 → 选择 → 两条对话分支 → 汇合”的内容制作，保存并重开后内容与连线一致；运行器读取同一份 SO 得到相同路线；图布局改变不改变播放逻辑。此阶段是首版核心能力，不延至交付阶段才补做，也不扩展为通用自由节点编程。

运行观察先用 M1 数据样例验证节点/指令位置、选择等待及窗口重开；实际资源/演出、读档和配音的观察分别在 M2、M3、M4 补齐验证，不将尚未接入的等待原因标为通过。跟随关闭后可自由浏览且仍能定位当前 SO，窗口开关不改变执行结果；窗口代码不进入 Player 构建。

## M2 — 可玩的阅读与演出（已完成并封存）

- [x] 扩展已有 NarrativeModule（保持显式 Enabled），接入 Gameplay 生命周期和 NewGame 请求；核对并启用 GameTableModule，等待配表与页面/资源 Ready。
- [x] 通过 EUI 开发中心制作对话、选择、背景与立绘相关页面/Item。
- [x] 实现姓名、正文、打字机、点击补全/继续、角色表情与简单转场。
- [x] 接入配表就绪、SO 节点及资源加载/释放、BGM/SFX 和小说输入动作。
- [x] 建立基础暂停原因与模态输入门控，设置弹窗打开后不会继续推进；不依赖状态暂停自动停止 Module Update。
- [x] 打通主菜单、新游戏、分支、结局、返回菜单。
- [x] 将真实资源、文字、过渡和模态暂停状态接入观察快照，验证窗口与玩家画面对应，运行期间不改写 SO。

完成标准：一段占位样例可完整播放；UI 尚未准备好时不会提前推进；连续快速点击不跨句；重复进出 Gameplay 不残留立绘、输入订阅或资源。此阶段构成可玩原型，不宣称完整模板完成。

## M3 — 存档与恢复（实现已落盘，验收待完成）

下列为阶段完成条件，实现已落盘并有部分专项验证；勾选保持未完成，直到最终工作副本整体验收通过。当前证据和剩余项见顶部收口清单，历史文件清单见后文。

- [ ] 实现稳定点快照、独立全局已读/偏好、槽位索引与版本校验。
- [ ] 槽位保存/恢复 storyId、会话全局变量与当前章局部变量；恢复时不重新初始化局部变量，账号已读/偏好与会话全局变量分离。
- [ ] 对话使用 nodeId + commandId 定位，选择使用选择 nodeId 与 optionId 信息恢复；不重播前置对话或变量副作用。
- [ ] 明确 SO 拆分/合并、节点删除和选择语义变更的兼容处理；补齐会话交换阶段失败的恢复策略。
- [ ] 加入手动多槽位、自动槽、快速槽与明确的保存成功/失败反馈。
- [ ] 实现临时写入、备份/替换和损坏处理；评估是否需要通用框架 API。
- [ ] 实现读取预检、资源准备、会话交换、演出恢复和错误返回。
- [ ] 实现新游戏、继续、存档/读档页面以及防重复读写。
- [ ] 验证读档 Restoring、成功交换、失败处理和重新开始时的观察状态，旧会话通知不再高亮旧 SO。

完成标准：对话停顿点、选项停顿点、分支选择后分别保存并重启恢复，变量/背景/立绘/BGM/当前句一致；旧异步回调不污染新会话；坏档、未知版本、缺失指令或资源不覆盖有效档；写盘失败不显示成功。截图功能可延后，不影响快照正确性。

## M4 — 阅读辅助与配音（功能验收通过，保存封存待完成）

2026-09-20：全量 XML 中 13 项 M4 用例全部通过；用户随后确认观察窗口专项通过、手动验证无问题。据此勾选本阶段功能验收，证据范围见末尾记录；保存封存尚未执行。

- [x] 自动播放、仅已读快进、对话历史和隐藏对话 UI。
- [x] 扩展 M2 暂停机制到历史、存档、确认等嵌套弹窗，关闭一层不会提前恢复。
- [x] 独立 Voice 播放/停止/完成通知和音量；文字速度与自动间隔设置。
- [x] 明确快进时每类演出指令行为，并按 Design.md 实施。
- [x] 验证自动/快进模式、配音等待和嵌套暂停在观察窗口中如实显示，不用窗口自身计时推断完成。

完成标准：自动播放等待文字和配音；快进停在未读句与选项；嵌套弹窗关闭一层不会提前恢复；读档不重复历史、不回退全局已读；退出或跳句后无旧配音残留。若更改通用音频 API，还要验证现有 BGM/SFX 调用。

## M5 — 编辑体验完善与模板交付（本批测试已收束，交付未完成）

- [x] 收束本批测试失败：用户确认 Wait 测试宿主修正后全部通过；历史 XML 与后续人工反馈分别记录，不合成为新全量报告。

- [ ] 完善 M1E 的段内表单、配表资源选择与全流程静态校验；回归复制、排序和连接操作。
- [ ] 回归运行观察窗口的中途打开、关闭重开、跟随开关、错误定位、退出 Gameplay 与停止 Play Mode；确认无重复订阅、旧会话残留或资产写入。
- [ ] 指定章节/流程入口起播与错误定位；保留基础流程图范围，不扩展成通用自由节点编程。
- [x] 整理约 10 分钟、两分支两结局的完整示例，补编写与扩展说明。LastLight 已有 79 句、单路线 59 句、两句合成 Voice；Authoring / SampleWalkthrough 已落盘。时长是字数估算，听感与人工完整游玩仍属下一项验收。
- [ ] 检查屏幕缩放、长句、空配音、资源缺失和反复进入退出。
- [ ] 模板保存、封存、父基线与内容校验；新消费项目完整部署和运行验收。

当前分项：

| 分项 | 已落盘与实际证据 | 剩余验收 |
|---|---|---|
| 段内表单与配表 | 多行台词、分类资源选择、中文演出摘要、稳定 ID 复制/排序；用户确认无编译报错；本批 XML 的 GraphInteraction 4/4、StoryEditor 5/5 通过 | 实际表单、复制/排序/连线的人工交互确认；未明确执行的边界仍保留 |
| 观察与定位 | storyId 限定定位；XML 的跨剧情用例、Availability 3/3、ObservationPlay 2/2 通过 | 自动用例已通过；未明确确认的真实 Gameplay 恢复失败/取消换源等 M3 专项仍独立保留 |
| 指定入口 | NarrativeEntryLauncher → 正式新游戏 Loading；NarrativeStoryTests 覆盖默认变量、不重跑前置赋值及非法入口 | 实际 Story/Chapter/Node 三种入口试播与错误定位 |
| 完整示例 | 79 句、两个选项、两个结局、两句 Voice；历史 Gameplay 3/3；本轮双结局测试修正后用户确认全部通过 | 约 10 分钟是估算；听感、自动等待 Voice 与人工完整功能检查待办 |
| UI 与边界 | 既有真实 EUI 与分辨率工具；最新归档 XML 380/382，两个失败修正后由用户确认通过 | 新完整 XML 未归档；窄屏/长句大字号/安全区、偏好跨启动、揭幕人工确认 |
| 交付 | 本次完成五目录文件级差异盘点，父快照与封存前备份逐文件一致 | 项目中心正式预览、SaveTemplate、显式 Bump、CompareEditingTemplate；消费端部署运行 |

用户此前确认模板完成后自行部署到 Call Me Heartless。此次尚未向消费项目写入；不另建目录、不使用 Unity Farm、不在模板快照内部创建 Unity 工程。消费端验收不能因模板封存而勾选。

完成标准：新项目选择 visual-novel 后能直接运行示例；文档和业务资源随模板交付；基础模板不含小说专属内容；仅升级框架包不会被误认为已迁移消费项目 Assets。发布另行执行，不自动推送 tag。

## 工作量和变更控制

旧方案的 **15～25 人日**仅是未包含流程可视化编辑器时的历史估算，已不适用于当前范围。此次新增 M1E，并调整 M1 的 SO 流程与配表契约；待 Design.md §9 的关键问题确认后，重新估算各阶段及总量。估算仍以熟悉 Unity/Ember 的开发者、占位资源、桌面首版为前提，不含正式剧本、美术、配音生产和多平台发布，也不是交付承诺。

每阶段记录：完成日期、模板版本、修改文件/目录、Unity 编译结果、实际测试与未通过项。失败项保留，不以历史通过替代当前验收。涉及代码和资源的编译验证遵循项目规则，仅使用 Unity MCP；不可用时明确交给人工编译。

## 历史实施与验证记录

以下按发生顺序保留；“本轮”“尚未实现”“停在 M1”等描述仅指该条记录当时的状态，不覆盖上方收口结论。

2026-09-17 初始基线：visual-novel 从 `0.1.0 / preview` 建立设计文档后，已通过现有模板流程封存为 `0.1.1 / preview`，父基线 `base 0.6.4`，尚未发布。快照与 base 的差异仅为文档、入口及对应 .meta，小说功能未开始。

2026-09-17 本轮文档调整：编辑前复核 `Assets/Editor/EmberEditingTemplate.json` 仍为 visual-novel `0.1.1`。依据用户确认意见更新六个职责、按分支切段、选择 SO 及流程图连接规则；同步存档定位、M1/M1E 与后续验收，保留条件分流/结局、引用与布局、兼容及恢复失败等待定项。本轮仅编辑项目 Assets 内三份 Markdown，M1、M1E、M2～M5 均未开始。

本轮调整当前为工作副本文档，尚未保存回模板或 Bump；`0.1.1` 快照仍代表调整前的文档。后续封存必须先预览全部受管目录差异，避免夹带工作区其他改动，再使用项目中心现有流程；不得手改快照、ParentSnapshot 或 hash，不切换模板、不提交 Git、不发布 tag。

2026-09-17 Fungus 参考补充：再次复核当前编辑模板仍为 visual-novel `0.1.1`，仅更新三份 Markdown。将图与内容面板联动、可读指令摘要和运行观察纳入首版，明确只读诊断、当前 SO/指令跟踪、等待原因与会话清理边界，并将验证分配到 M1/M1E～M5。功能开发仍未开始，本次文档仍未保存回模板或 Bump。

### 2026-09-17 M1 首轮实施（历史记录）

基线：当前编辑记录为 visual-novel `0.1.1 / preview`，父模板 `base 0.6.4`；没有切换模板。采用最新 Assets 文档，保留原有场景、页面、调试配置、文档/模板快照以及 DevCenterSmoke 等未提交内容。此次属于视觉小说模板升级的项目工作副本开发，没有修改框架或模板快照。

实际源码文件（路径相对于项目）：

| 路径 | 内容 |
|---|---|
| `Assets/Game/Module/Narrative/Game.Narrative.Runtime.asmdef` | 小说运行程序集，引用框架与表程序集 |
| `Assets/Game/Module/Narrative/NarrativeData.cs` | 类型化变量、条件、指令、纯数据章节/节点/路线 |
| `Assets/Game/Module/Narrative/Assets/NarrativeNodeSO.cs` | 节点基类与序列化路线 |
| `Assets/Game/Module/Narrative/Assets/NarrativeChapterSO.cs` | 章节入口/清单/变量，SO 到会话定义适配 |
| `Assets/Game/Module/Narrative/Assets/NarrativeDialogueSO.cs` | 连续指令与单后继 |
| `Assets/Game/Module/Narrative/Assets/NarrativeChoiceSO.cs` | 选择提示、选项文字/条件/目标 |
| `Assets/Game/Module/Narrative/Assets/NarrativeBranchSO.cs` | 顺序条件分流与必填兜底 |
| `Assets/Game/Module/Narrative/Assets/NarrativeEndingSO.cs` | 显式 endingId 结束 |
| `Assets/Game/Module/Narrative/NarrativeValidator.cs` | ID、目标、变量、条件和配表键校验 |
| `Assets/Game/Module/Narrative/NarrativeRunner.cs` | 推进、选择、分流、Wait、预算、暂停/取消与回调令牌 |
| `Assets/Game/Module/Narrative/NarrativeDiagnostics.cs` | 只读快照、等待/暂停原因与定位错误 |
| `Assets/Game/Module/Narrative/NarrativeTableCatalog.cs` | 四类强类型表的角色/资源键查询 |
| `Assets/Game/Table/Game.Table.Runtime.asmdef` | Row/生成代码程序集 |
| `Assets/Game/Table/Rows/NovelCharacterRow.cs`、`NovelPortraitRow.cs`、`NovelBackgroundRow.cs`、`NovelAudioRow.cs` | 四张表 Schema |
| `Assets/GameResource/TableSources/novel_characters.etable.csv`、`novel_portraits.etable.csv`、`novel_backgrounds.etable.csv`、`novel_audio.etable.csv` | 两角色及三个空资源表的源数据 |
| `Assets/Game/Module/Table/GameTableModule.User.cs` | 现有 Table Module 的小说查询适配生命周期接线；模块仍关闭 |
| `Assets/Game/Module/Narrative/Editor/Game.Narrative.Editor.asmdef`、`NarrativeM1Assets.cs` | Editor 隔离与缺失样例/Definition 创建、导表预览入口 |
| `Assets/Game/Module/Narrative/Tests/Game.Narrative.Tests.asmdef`、`NarrativeRunnerTests.cs` | Editor 测试程序集与 17 个测试用例（未运行） |
| 本目录三份 Markdown | 确认决策、实际实现及待验收记录 |

新文件 `.meta` 由 Unity 导入生成，未手改模板 hash。没有修改生成器所有的 Catalog/Binding；当前 Catalog 仍为空。没有新增正式 EUI Prefab、NarrativeModule 或存档模块。

验证记录：

- MCP 实例列表中同时有两个项目；已显式选定 `ember-unity-framework@b23e9b768e85ed30`，项目路径核对为 `C:/Users/wuyu/My/ember-unity-framework`，未操作 Unity Farm。
- 源码落盘后调用一次 MCP `refresh_unity`（force/all/request），工具返回已请求刷新。随后首次编译状态查询 `execute_code` 返回 `success=false`，没有错误详情。依 CLAUDE.md 立即停止自动编译验证；没有轮询日志/进程、BatchMode 或 dotnet build。
- **本次未完成 Unity 编译验证，EditMode 测试未运行，Play Mode 未验证。** 刷新请求成功不能代替编译成功。失败后补充的源码与文档也未进行 Unity 编译验证。
- 测试源码覆盖：两分支两结局及汇合、首个条件命中、零合法选项、空段循环/即时指令预算、坏 ID/目标/变量/资源定位、同帧与过期输入、暂停冻结与嵌套恢复、取消重开、只读快照、观察者重入/抛错、演出失败、AND/OR、SO 隔离、实际四表/样例整合。只记录测试已编写，不能记录为通过。
- 即时静态检查：4 个新增程序集 JSON 可解析；运行目录无 UnityEditor/AssetDatabase 依赖（Editor 和 Tests 单独隔离）；新增 Runtime 不使用 Debug.Log；新源码/CSV/程序集均有 Unity 生成的 .meta；三份文档相对文件链接无缺失，已修改的既有 C# 文件通过 `git diff --check`。这些不是编译或测试通过。
- 保留核对：对基线中 1634 个相关既有文件逐个比较 SHA256，仅三份小说文档与 `GameTableModule.User.cs` 改变；模板快照、编辑记录、既有场景/页面/资源/调试配置等未改变。报告位于本机 `.utmp/visual-novel-m1/preservation-check.json`，不属于模板交付。

人工完成当前 M1 的顺序：

1. **请在 Unity 中手动触发编译**。如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。
2. 编译成功后，运行 `Ember/视觉小说/M1 创建样例与预览配表`。该入口创建四个 Definition、章节与八个独立节点 SO，保留既有样例目录；若创建中途失败，先检查残留，不会自动覆盖。
3. 在现有 `Ember/配置表中心` 核对全部生成差异，导出全部，生成 ETBL、Binding/Catalog，再编译。资源表当前只有表头，M2 才添加美术/音频占位内容。
4. Test Runner 运行 EditMode 程序集 `Game.Narrative.Tests`；其中整合测试依赖步骤 2～3 的实际资产，缺失时会失败而非跳过。回传本轮测试结果。
5. M1 验收后通过项目中心预览全部受管目录差异，处理既有 DevCenterSmoke 等无关内容，之后才保存/Bump。当前未做项目中心完整差异预览，因此没有保存或封存。

遗留：M1 导表与资产整合尚未闭环；Unity 编译/测试未验收；模板仍为旧 `0.1.1` 封存快照。M1E 的图编辑与复制 ID、M2 的真实资源/演出与 Gameplay、M3 的存档兼容/失败回退、M4 的自动快进/配音均未开始。本轮停在 M1，不能将当前状态记为阶段完成，也不能跳到 M1E。

### 2026-09-17 M1 补齐与验收

用户反馈“没有编译报错”，据此继续原 M1 授权范围。MCP 重新准确选中 `ember-unity-framework@b23e9b768e85ed30`，核对实际路径，Editor 处于 Edit Mode。没有操作 Unity Farm，也没有开始 M1E。

本轮实际新增产物（均由现有 Unity/Ember API 创建，不手写生成代码）：

- `Assets/Game/Table/Definitions/novel_audio.asset`、`novel_backgrounds.asset`、`novel_characters.asset`、`novel_portraits.asset` 及 .meta。
- `Assets/GameResource/Resources/VisualNovel/M1Sample/Chapter.asset` 与 intro、choice、ask、leave、merge、branch、good、quiet 八个节点 .asset，以及目录/文件 .meta。
- `Assets/GameResource/Resources/Config/Tables/novel_*.bytes` 四份 ETBL 数据与 .meta。
- `Assets/Game/Table/Generated/Game_Table_Novel*RowTableBinding.TableBinding.g.cs` 四份 Binding 与 .meta；现有 `GameTables.Catalog.g.cs` 和 `EmberTableArtifacts.manifest.json` 由生成器更新。

实际验证与结果：

1. `NarrativeM1Assets.CreateMissing()` 成功创建样例；`EmberTablePipeline.PreviewAll()` 成功，诊断 0，完整核对 10 个导表写入动作后调用 `BakeAndGenerateAll()` 成功，诊断 0。
2. 通过 MCP 刷新并请求编译；工具明确报告 compiling，随后有界查询确认编译/重载结束，控制台 `error CS` 查询为 0。
3. 运行 EditMode 程序集 `Game.Narrative.Tests`，任务 `b4777f396c3440c59fc266eb55c1ef9c`：**17 通过，0 失败，0 跳过**。真实四表/九个 SO 的整合用例通过，两条路线分别到达 good/quiet，资产内容比较未变化。
4. 调用项目中心同一 `CompareEditingTemplate` 完整比较五个受管目录：104 项差异，全部为本阶段内容，没有新增夹带 DevCenterSmoke、其他场景/页面或调试配置改动。保存与补丁 Bump 使用 `EmberProjectSetup.SaveTemplate` / `BumpTemplateVersion(..., 2)`，交付版本为 `0.1.2 / preview`，父基线保持 `base 0.6.4`，尚未发布。

本机证据保存于 `.utmp/visual-novel-m1/test-results-20260917.json` 与 `template-preview-20260917.json`，不随模板发布。首轮失败记录保留，上述为本轮新执行结果，未沿用历史测试结论。

阶段结果：M1 完成；没有已运行但未通过的测试。边界仍为 EditMode 数据与运行核心验收，没有执行 Play Mode、音画资源加载、正式 EUI、独立 Player 或新消费项目部署验证。GameTableModule 保持默认关闭，M2 接入 Gameplay 时启用并验证。后续图编辑/复制 ID、存档兼容和恢复失败策略等待定项继续保留。停在 M1，等待用户说“继续下一阶段”。

### 2026-09-17 M1E 实施与验收

用户说“继续下一阶段”，本轮仅推进 M1E。开工核对 visual-novel `0.1.2 / preview`，父基线 `base 0.6.4`；先读取最新 Assets 文档、API 手册、模板规范及既有图/列表编辑能力。用户明确接受 IMGUI 与每章独立 Editor 布局 SO。工作区其他未提交改动保留，没有操作 Unity Farm、安装插件或修改框架源码。

实际文件（路径相对于项目）：

| 文件 | 实际职责 |
|---|---|
| `Assets/Game/Module/Narrative/NarrativeObservation.cs` | 只读诊断源注册、替换、释放与运行初始化重置 |
| `Assets/Game/Module/Narrative/Editor/NarrativeGraphWindow.cs` | IMGUI 画布、端口连线、内容联动、缩放平移、校验定位、Play Mode 观察与订阅清理 |
| `Assets/Game/Module/Narrative/Editor/NarrativeGraphModel.cs` | 章节内节点创建/登记/复制/移除、连接、布局移动和仅本章保存；模板与编辑模式门控 |
| `Assets/Game/Module/Narrative/Editor/NarrativeGraphLayout.cs` | 只保存章节 GUID、nodeId 与坐标的布局 SO |
| `Assets/Game/Module/Narrative/Editor/NarrativeContentGUI.cs` | 四类节点表单、可读指令摘要、复制/重排、类型化变量字段、自定义只读运行 Inspector |
| `Assets/Game/Module/Narrative/Editor/NarrativeObservationSample.cs` | 与观察窗独立的 Editor 样例宿主和菜单，驱动真实 M1 运行器 |
| `Assets/Game/Module/Narrative/Editor/Layouts/f1f1a8ad66d139541a1ae44a687071aa.asset` | M1Sample 八节点初始布局；章节和剧情节点内容不变 |
| `Assets/Game/Module/Narrative/Tests/NarrativeGraphTests.cs` | 5 项图/注册契约测试，以及实际进入/退出 Play Mode 的观察集成测试 |
| `Assets/Game/Module/Narrative/Tests/NarrativeTestReport.cs` | Unity Test Runner 回调导出本轮 NUnit XML 到本机 `.utmp`，域重载后重新注册 |
| `Assets/Game/Module/Narrative/Tests/Game.Narrative.Tests.asmdef` | 追加 Editor 程序集引用，测试仍仅 Editor 编译 |
| 本目录 README / Design / Implementation | 已确认决策、实际操作入口、完成项、验证与边界 |

以上新文件及布局目录的 `.meta` 均由 Unity 导入生成。沿用 M1 的独立 Runtime/Editor 程序集，无正式 EUI、Gameplay Module 或存档实现。

实际验证：

1. 通过 Unity MCP 定位 `ember-unity-framework@b23e9b768e85ed30`，实际路径核对无误。修改后触发 Unity 刷新/编译，最终控制台 `error CS` 为 0；没有使用 dotnet、日志轮询或历史结果代替本轮验证。
2. 最终运行 `Game.Narrative.Tests`，MCP 任务 `c92a14c0445d422091286789f1a42342`。Unity 原生 NUnit 报告 `tests-20260917-101023678.xml`：**23 通过、0 失败、0 跳过**。其中 22 项为 EditMode 数据/编辑操作，1 项是 EditMode UnityTest 实际进入 Play Mode 验证观察后退出，不宣称独立 Player 测试。
3. 图操作测试实际创建“对话 → 选择 → 两条对话 → 汇合 → 结局”，保存并重新导入，再由同一 SO 定义跑两条路线；布局改变不影响定义。复制 ID、重排 ID 保持、跨章拒绝、入链移除及 Undo/Redo、保留独立资产、Inspector 改目标后图读取一致均通过。
4. 观察测试实际打开/关闭/重开 EditorWindow，核对当前 SO、选择等待、两层暂停、关闭跟随后浏览不跳走、错误定位、替换后旧源订阅清零、旧句柄不清掉新源、结局与退出后无会话；播放前后章及八节点序列化内容一致。运行模式下图模型写入被拒绝。
5. 实际窗口截图检查图与内容面板、八节点连线和角色/台词摘要，修复缩小时输入端口遮挡标题的问题。基本校验与编辑操作已验收；大章性能、复杂输入设备及更多作者使用体验留待 M5，不将截图当作功能测试替代。

本轮失败与处理：首轮编译发现 EmberDebug 标签参数遗漏，已修正并重新编译。第一次全量测试为 22 通过 / 1 失败，观察测试跨 EnterPlayMode 域重载保留编译器闭包导致空引用；额外等待一帧未解决，改为重载之后创建检查协程后通过。测试临时目录全部清理，初次生成的 Layouts 目录现包含正式样例布局，不再有未清理测试文件提示。MCP 测试任务在域重载后未收到结束通知，故以同一轮 Unity Test Runner 回调直接导出的 XML 核对实际结果，并清理失效的 MCP 任务记录，不将旧进度或失焦提示解释为成功。

模板交付：完整预览为 **23 项差异，0 错误、0 警告**，仅本阶段源码、测试、样例布局及三份文档，无场景、页面、配表或其他工作区内容夹带。使用项目中心现有 `SaveTemplate` / 补丁 `BumpTemplateVersion(..., 2)` 封存为 **0.1.3 / preview**，父基线仍为 base 0.6.4，尚未发布。证据位于本机 `.utmp/visual-novel-m1e/`，不进入模板。

阶段结果：M1E 完成，最终没有已运行但未通过的用例。尚未验证实际资源加载/过渡/Voice/读档的观察状态，它们分别由 M2/M3/M4 接入；尚无正式阅读页面、独立 Player 构建或消费项目部署验收。M3 剧情修订兼容及恢复提交后失败处理仍待确认。当前停在 M1E，等待用户说“继续下一阶段”再开始 M2。

### 2026-09-17 M1E 画布性能与交互优化

用户反馈面板严重卡顿，要求参考 Shader Graph 加入缩放、吸附排布及空格添加节点。本轮只优化 M1E，不推进 M2。开工确认当前为 visual-novel 0.1.3 / preview，父基线 base 0.6.4；已核对当前安装的 Shader Graph 源码和 Unity GraphView API，复用引擎内置编辑器能力，无新插件。

实际文件（均在 `Assets/Game/Module/Narrative/` 下，含 Unity 生成的新增 .meta）：

| 文件 | 本轮变化 |
|---|---|
| `Editor/NarrativeGraphWindow.cs` | UI Toolkit 工具栏、可调宽度内容侧栏、合并刷新和缓存表单；保持原有观察生命周期 |
| `Editor/NarrativeFlowGraphView.cs` | 新增保留式图、拖拽端口、多选、导航、快捷键、缩略图与只读执行高亮 |
| `Editor/NarrativeAutoLayout.cs` | 新增强连通分量分层排布与网格坐标计算，仅产生布局 |
| `Editor/NarrativeNodeSearch.cs` | 新增内置 SearchWindow 的四类剧情节点搜索菜单 |
| `Editor/NarrativeGraph.uss` | 新增节点摘要与执行位置样式 |
| `Editor/NarrativeGraphModel.cs` | 缓存模板只读检查、写前复核、类型化目标读取、整组布局 Undo |
| `Editor/NarrativeContentGUI.cs` | 复用窗口和 Inspector 的 SerializedObject，减少重绘分配 |
| `Tests/NarrativeGraphInteractionTests.cs` | 新增 4 项针对缓存、大图导航和排布的回归测试 |
| `Assets/Game/Documentation/visual-novel/` 三份文档 | 更新操作说明、实际设计、验证结果与边界 |

剧情 SO 类型、章节隔离、运行定义与状态分离、独立布局格式保持原契约；本轮没有修改样例剧情或作者现有布局坐标。自动排布由作者点击后执行，不在开窗时覆写布局。多选复制生成全新 ID，组内目标重映射至副本，组外目标保留。此项为常规编辑器操作规则，不新增运行语义或存档格式。

本轮实际验证：

1. Unity MCP 始终定位 `ember-unity-framework@b23e9b768e85ed30`。最后一次脚本刷新/编译后 `error CS` 为 0。初次新代码的 `WorldToLocal` 扩展调用遗漏接收者已修复；未使用静态检查代替编译。
2. 首次全程序集运行 27 项，24 通过 / 3 失败（`tests-20260917-103551433.xml`）。两项新测试把内存节点写入持久章节导致 Unity 清空引用，已改成测试专用临时子资产；正式节点仍为独立资产。第三项 Play Mode 在域重载时遇到 FMOD 输出设备初始化错误，不属于编译错误，也未绕过或忽略日志。
3. 修复后定向运行新增 4 项，任务 `90ecdbef32384d14bdee1bcc14ebfcd8`，**4/4 通过**：200 节点和 199 连线真实窗口、200 次变换不重建/不读模板、不创建布局；真实滚轮变更缩放、空格只触发一次搜索；循环与孤立节点可排布，汇合位于两前驱之后，整组 Undo 恢复坐标且剧情序列化内容未变化。
4. 单独复验实际进入/退出 Play Mode 的观察测试，任务 `2e4954e5d8074b3ea028572b17359971`，原生报告 `tests-20260917-104132270.xml` 为 **1/1 通过**；本次未再出现 FMOD 错误。本轮已运行的 27 个不同用例最终均有通过结果，包含上述定向重跑，不写成单次全量 27/27。域重载导致 MCP 结束通知丢失时，以该轮 Unity Test Runner 原生 XML 为准。
5. 实际打开新窗口，核对八节点与节点/侧栏几何尺寸。热点微基准使用同一个八节点章节，200 轮、1600 次目标读取，从 628.7574 ms 降至 50.1597 ms，缓存后模板文件读取 0。该基准包含旧绘制路径的读检查与端口查询，不是整窗帧率或输入延迟；Mono 分配计数不可据此判断，未宣称零 GC。证据在本机 `.utmp/visual-novel-m1e/graph-performance-before.json` 与 `graph-performance-after.json`。

模板交付：项目中心完整差异预览为 16 项，0 错误、0 警告，限于上述代码、测试和三份文档。样例布局的内存/磁盘/封存坐标一致，单独保存其 dirty 标记后文件字节未变化。通过现有 SaveTemplate / 补丁 Bump 流程封存为 0.1.4 / preview，父基线保持 base 0.6.4，未发布；没有夹带场景、页面、配表、调试配置或其他工作区改动。

未通过项：修复/复验后无遗留失败。仍待作者实际使用体验反馈、超大章节和不同缩放/输入设备压力测试；没有新增 Player 构建或 M2～M5 验收。GraphView 为当前 Unity 版本内置 Experimental API，后续升级 Unity 时需回归交互。停在 M1E，等待用户说“继续下一阶段”。

### 2026-09-17 M1/M1E 多章节扩展与缩略图修复

用户反馈缩小时 MiniMap 异常，要求默认显示全部章节、双击进入章内图、跨章全局条件、内容/设置页签和分层资产。开工确认 visual-novel 0.1.4 / preview，父基线 base 0.6.4。明确归属为模板升级；读取项目规则、API 速查、工作副本文档与实际源码后，仅修改项目 Assets。

已逐项确认：独立章节出口、条件顺序命中与必填兜底；会话全局＋章节局部变量，bool/int/string 和显式作用域；章节/类型分层、节点加章节前缀，并保留 GUID / 稳定 ID 迁移已有样例。上述选择已写入 Design §4 与 §12。

本轮实际文件（相对项目）：

| 文件 | 变化 |
|---|---|
| Narrative/NarrativeStory.cs | 纯剧情/出口定义，剧情与全局条件校验 |
| Narrative/Assets/NarrativeStorySO.cs | 章节登记、剧情入口、全局变量、出口路线；构造独立运行副本 |
| Narrative/Assets/NarrativeChapterExitSO.cs | 独立章节出口 |
| Narrative/NarrativeData.cs、NarrativeRunner.cs、NarrativeValidator.cs、NarrativeDiagnostics.cs | 显式变量作用域、同会话跨章、独立全局快照、循环保护、验证 |
| Narrative/Assets/NarrativeChapterSO.cs | 显示名/文件前缀，包含全局定义的章节读取 |
| Narrative/Editor/NarrativeStoryGraphView.cs、NarrativeGraphWindow.Story.cs | 章节总览、双击导航、出口条件编辑与全局变量侧栏 |
| Narrative/Editor/NarrativeStoryModel.cs | 总览写入、章节管理、命名/目录规范、迁移预检与 Unity 资产移动 |
| Narrative/Editor/NarrativeMiniMap.cs | 两级共用固定 200×144 概览，节点/视口联合边界等比投影、点击定位 |
| Narrative/Editor/NarrativeGraphWindow.cs、NarrativeFlowGraphView.cs、NarrativeContentGUI.cs、NarrativeGraphModel.cs、NarrativeNodeSearch.cs | 两级切换、内容/设置页签、出口创建、运行观察与分层创建路径 |
| Narrative/Editor/NarrativeStorySample.cs、NarrativeM1Assets.cs、NarrativeObservationSample.cs | 多章节样例、更新创建路径与实际 Story 运行宿主 |
| Narrative/Tests/NarrativeStoryTests.cs、NarrativeStoryEditorTests.cs | 跨章条件/局部重置/兜底/步数限制、迁移/同 ID 外部引用拒绝与总览交互 |
| Narrative/Tests/NarrativeGraphInteractionTests.cs、NarrativeGraphTests.cs、NarrativeRunnerTests.cs | 缩略图多倍率边界、两级 Play Mode 观察、迁移后原章节回归 |
| Assets/GameResource/Resources/VisualNovel/M1Sample/ | Story.asset、三个章节、规范子目录；新增序章/归途示例，原校园剧情保持 |
| Assets/Game/Module/Narrative/Editor/Layouts/ | 新剧情总览、序章与归途布局；原校园布局 GUID 与坐标保持 |
| 本目录 README、Design、Implementation | 新规则、使用方法、边界和本轮证据 |

表中的 Narrative/ 前缀是 Assets/Game/Module/Narrative/，新文件均带 Unity 生成的 .meta。没有改框架源码、正式 EUI、其他场景/调试配置，没有操作 Unity Farm、安装插件或提交 Git。

迁移预览逐项列出原 Chapter.asset 和八个节点的源/目标路径，使用 AssetDatabase.MoveAsset 移动 9 个 SO；返回 allGuidsPreserved=true，chapterId 仍为 vn_m1_sample，八个 nodeId 保持 intro/choice/ask/leave/merge/branch/good/quiet。新增章节名与前缀不会反向重命名已有资产。原章节的两个结局与路线不变；Story 新增序章选择 visitSchool 全局 bool，满足时进入校园章节，兜底进入归途章节。

本轮验证与已修复失败：

1. Unity MCP 精确定位 ember-unity-framework@b23e9b768e85ed30。各批脚本完成后刷新/编译，最后一次 error CS 查询为 0。
2. 第一轮针对性测试 13/13 通过，任务 0c4b219111974e8f93b7c5d3ecf84e5f：全局 bool/int/string、顺序优先、兜底、局部重置、新游戏重置、跨章循环预算、移除/Undo、迁移保留引用及真实双击事件。
3. 第一次全量 40 项为 39 通过 / 1 失败，原生报告 tests-20260917-115025179.xml。实际缺陷是 CreateGUI 的旧延迟定位回调把 Play Mode 总览自动切入章节；已限制为仅在二级视图执行。随后任务 b82d3d144a724db19381c414c82d4e27 的两项 Play Mode 观察测试 2/2 通过（tests-20260917-115404855.xml）。
4. 新增同 ID 但未登记的外部章节引用测试，确保运行适配器按资产引用验证归属。最后全量任务 11784fe2871946df83026fd1baacdf1e 的 Unity 原生报告 tests-20260917-115826900.xml 为 **41/41 通过、0 失败、0 跳过**，含两项实际进入/退出 Play Mode 的观察测试。MCP 的测试结束消息在域重载后丢失，按本轮原生报告核对，不使用历史结果代替；最终无未通过项。
5. 200 节点测试实际挂载编辑窗口，验证 10%、25%、87%、100%、300% 下全部节点投影留在概览边界，反向定位坐标可还原，概览尺寸固定；保留导航不重建节点/不读模板/不创建布局的原检查。真实 Story 三条路线分别到 good、quiet、return_home，比较 Story/章节/节点序列化内容不变。
6. 本机窗口图像通过 Unity GUIView.GrabPixels 抓取当前窗口渲染内容；仅作为布局核对，不替代交互或运行测试。证据放在 .utmp/visual-novel-story/，测试原生报告仍在 .utmp/visual-novel-m1e/。

模板交付：完整项目中心预览 113 项差异，0 错误、0 警告，全部为本轮小说源码、测试、三份文档、迁移与新增样例/布局，没有其他工作区改动夹带。只保存当前 Story 及其登记章节/布局，再用项目中心 SaveTemplate / minor Bump 封存为 0.2.0 / preview，父基线 base 0.6.4 不变，尚未发布。迁移旧文件在比较中表现为删除旧路径＋新增新路径，GUID 验证另行通过。

边界：多章节运行与全局变量仅在内存和观察快照中实现，M3 槽位写盘/恢复未实现；正式 Gameplay、EUI、资源演出和配音仍留 M2～M4。更多作者使用体验、超大章节压力、Player 构建和消费项目部署待后续验证。本轮继续停在 M1E，不因扩展跨章能力而自动进入下一阶段。

### 2026-09-17 · M1E 模块启用约束

- [x] 按用户要求，编辑器入口依赖 NarrativeModule 的 EmberModule.Enabled，与引导编辑器一致。
- [x] 禁用时隐藏八个小说菜单和七个 SO 创建菜单；阻止双击和 Open/OpenFor，关闭旧窗口，Inspector 回退普通资产显示。
- [x] 图写入、创建剧情/样例、资产迁移补充模块判断；保留原有模板身份和 Play Mode 限制。
- [x] 本轮 Unity 编译和 44 项测试；三份工作副本文档同步。
- [ ] M2 正式 Gameplay 会话/演出接线仍未开始。当前模块只有装配声明和空生命周期方法。

实际文件（路径前缀为 Assets/Game/Module/Narrative/）：

| 文件 | 变更 |
|---|---|
| NarrativeModule.cs、Game.Narrative.Runtime.asmdef | Gameplay 模块声明，本模板 Enabled=true；增加 Ember.Core.Runtime 引用 |
| Editor/NarrativeEditorAvailability.cs、Editor/Game.Narrative.Editor.asmdef | 缓存类型元数据，域重载后动态同步菜单；无单例访问、无逐帧扫描 |
| Assets/NarrativeStorySO.cs、NarrativeChapterSO.cs、NarrativeDialogueSO.cs、NarrativeChoiceSO.cs、NarrativeBranchSO.cs、NarrativeChapterExitSO.cs、NarrativeEndingSO.cs | 将静态 CreateAssetMenu 交由模块感知的动态菜单注册，资产结构不变 |
| Editor/NarrativeGraphWindow.cs、NarrativeContentGUI.cs | 窗口恢复、打开 API、资产双击和 Inspector 入口判断 |
| Editor/NarrativeGraphModel.cs、NarrativeStoryModel.cs、NarrativeM1Assets.cs、NarrativeStorySample.cs | 编辑和创建/迁移入口防绕过 |
| Editor/NarrativeObservationSample.cs | 菜单统一注册，禁用时拒绝启动并清理观察样例 |
| Tests/NarrativeAvailabilityTests.cs | 菜单全量隐藏/恢复、直接调用防绕过、资产内容保持、恢复窗口关闭三项测试 |
| 本目录 README.md、Design.md、Implementation.md | 开关使用说明、设计边界、实际文件和验证记录 |

验证证据：Unity MCP 精确定位 ember-unity-framework@b23e9b768e85ed30，刷新后编译错误查询为 0。测试任务 8d96452d33724cd7b112ecb0818dfa20 的结果查询在 Play Mode 域重载时断连；随后以本轮 Unity Test Runner 原生报告 `.utmp/visual-novel-m1e/tests-20260917-121818510.xml` 核对为 **44/44 通过、0 失败、0 跳过**，含既有实际进入/退出 Play Mode 的观察测试。新增测试在域内切换读取的模块元数据副本，不改写模块源码或模板配置；覆盖启用→禁用→启用及恢复窗口路径。测试完成后实际编译模块声明仍为 Enabled=true，真实菜单存在。

未通过项：无。遗留验证：本轮未通过修改源码并连续编译两次验证 Enabled=false→true，也未执行 Player 构建；Unity 内部菜单 API 的跨版本适配仍需后续升级时验证。项目中心只读比较无错误/警告，源码差异均位于本轮小说模块及文档；本轮只更新 Assets 工作副本，未保存回模板或 Bump，已封存版本仍为 0.2.0 / preview。未改框架源码、剧情 SO 内容、GUID、模板快照或其他工作区改动，未操作 Unity Farm。

### 2026-09-17 M2 开工与 MCP 阻塞（未完成）

本次明确授权只做 M2。只读核对 AGENTS/CLAUDE、索引、API、模板文档和三份小说工作副本文档；git 暂存区为空，大量既有未暂存/未跟踪内容保持。编辑记录及元数据均为 visual-novel 0.2.0 / preview，父 base 0.6.4。用户确认的槽位、跨章演出保留及样例资产组织已记录 Design §14。

本轮文件（均未封存）：

| 文件 | 当前内容及完成边界 |
|---|---|
| Assets/Game/Module/Narrative/NarrativeData.cs、NarrativeValidator.cs、Editor/NarrativeContentGUI.cs | 显式槽位、显示/替换/隐藏、过渡时长及静态校验；尚未修改样例 SO |
| NarrativeModule.cs、NovelSession.cs、NovelPresentation.cs、Game.Narrative.Runtime.asmdef | 扩展既有 Gameplay Module，加入请求、会话资源所有权、页面接口、Ready、打字机、等待、暂停、结束释放与输入读取代码；状态/UI/InputAction 资产尚未接通 |
| Assets/Game/Module/Table/GameTableModule.cs | Global 显式 Enabled=true；实际运行就绪尚未验证 |
| Assets/Game/UI/Editor/NovelM2UIBuilder.cs、Game.UI.Editor.asmdef | 开发中心预检、创建、布局、绑定、重生成及迁移工具，补实际程序集依赖 |
| Narrative/Editor/NarrativeEditorAvailability.cs、Game.Narrative.Editor.asmdef | 模块感知的 M2 制作菜单（最新增量未编译验收） |
| Narrative/Tests/NovelSessionTests.cs、NarrativeAvailabilityTests.cs | 5 项会话测试代码与新增入口开关覆盖；均未运行 |
| Assets/Game/UI/Runtime/VisualNovel/EUINovelChoiceItem.cs、.Binding.cs | 开发中心实际生成的空 Item 骨架；没有控件绑定或业务逻辑 |
| Assets/GameResource/Resources/UI/Module/VisualNovel/ | 开发中心创建的部分资源及目录；只有 Choice Item 空 Prefab，尚未迁到最终 UI/VisualNovel |
| Packages/com.ember/Audio/Runtime/EmberAudioPlayback.cs、EmberAudioManager.cs | 新增可释放/暂停 SFX 句柄、BGM 暂停与就绪查询；保留旧 API；运行回归待补 |
| 本目录 README、Design、Implementation | 已确认决定、真实文件、失败及恢复步骤 |

以上表格 Narrative/ 前缀为 Assets/Game/Module/Narrative/。本机备份位于 `.utmp/visual-novel-m2/before/`，开工 git 状态位于 `status-before.txt`；不随模板交付。

本轮实际验证及失败：

1. MCP 精确选中 ember-unity-framework@b23e9b768e85ed30，projectRoot 核对为 C:/Users/wuyu/My/ember-unity-framework；未操作 Unity Farm。
2. 预检四个 UI 请求成功。第一次执行工具片段误用了 EUIBindingRole 命名空间，片段编译失败后改为 Ember.UIExtension；不涉及项目脚本成功结论。
3. 首批脚本刷新只刷新 scripts，新 Audio 文件尚未导入导致类型缺失；随后 all 刷新导入。UI Editor 缺 UIExtension/TMP 引用的 6 条错误已修正，随后 MCP 错误查询为 0。
4. 第一次 UI 制作创建了 Choice Item 空骨架，然后因开发中心默认实际路径为 UI/Module/VisualNovel、脚本预期 UI/VisualNovel 失败。已修正制作路径并加入保留 GUID 的后置移动流程，但完整流程没有执行成功。
5. 随后 execute_code 返回无详情失败，read_console 明确返回 ping not answered。按 CLAUDE 停止自动验证。用户确认编辑器恢复后，重新核对项目并请求刷新，首次 read_console 再次因 2 秒无响应失败，制作调用也未返回有效结果。再次停止验证，没有读取 Editor.log、等待自动刷新、使用 dotnet 或 BatchMode 替代。
6. 最后一次成功的错误查询发生在新增会话测试及最终修正之前，**不能用于声明当前最终代码编译通过**。本轮没有运行 Unity 测试、Play Mode、完整链路或可视化验收；44/44 仅是 M1E 历史基线。

恢复后按顺序继续（仍属已授权 M2）：

1. 当前 Unity MCP 未连接或不可用，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。
2. MCP 可用后准确定位本项目；使用 `Ember/视觉小说/M2 制作正式阅读 UI` 或同一 Build API 完成剩余 Prefab/Binding。生成工具只补无绑定骨架，有正式最终目录时拒绝覆盖。成功报告目标 `.utmp/visual-novel-m2/ui-generated.json`；必须读实际 Binding 后再写页面/Item 逻辑。
3. 补齐正式 NewGame/GameGameplayState、EUI 阅读与选择输入、模态生命周期，生成小说 InputAction 资产；当前 Begin 未被正式入口调用，不能试作完整可玩原型。
4. 在已确认路径制作占位背景/表情/BGM/SFX，更新源表并通过 Table 中心实际导出，保留既有节点/台词 ID 追加演出指令。适配旧纯运行器测试，使其显式完成演出等待；真实演出必须由新整合测试验证，不能仅 CompletePresentation 冒充播放成功。
5. 编译并运行会话测试、原有回归、真实主菜单→分支→结局→菜单及窗口观察；补音频句柄、迟到资源结果、页面加载中退出、错误路径、暂停和重复进入退出验收。检查播放前后 SO 内容与 GUID 不变。
6. 阶段真正完成后，完整预览项目中心差异、核对无其他改动夹带，再 Save/Bump。当前没有保存、Bump、Git 提交、tag 或发布，也未开始 M3～M5。

阶段结论：M2 未完成；阻塞为 Unity MCP 无可靠响应以及正式 Binding 尚未生成，保留中间态继续本阶段，不等待或宣称进入 M3。

### 2026-09-17 M2 正式链路与退出边界（续作，尚未收口）

此记录覆盖上方中间态中“UI 未生成/资源未创建/正式入口未接线”的当前状态，保留此前失败历史。

已实际完成：

- `UI/Editor/NovelM2UIBuilder.cs` 经开发中心完成四个正式 Prefab、控件绑定和生成，保留 GUID 移动到 `Resources/UI/VisualNovel/Prefabs`，报告 `.utmp/visual-novel-m2/ui-generated.json`。已读取真实 Binding 后编写 Reader/Choice/Background/Portrait 用户逻辑，未手写 Binding 字段。
- `State/GameGameplayState.cs`、`UI/GamePages.User.cs`、`UI/Runtime/MainScene/EUIMainPage.cs` 接 NewGame、会话准备、页面加载、模态暂停及退出。已有 NarrativeModule 扩展，GameTableModule 显式启用。
- `Narrative/Editor/NarrativeM2Assets.cs` 通过 Unity 创建两张背景、三张立绘、BGM/SFX WAV 和 NovelInput.inputactions，保留原样例 GUID/稳定 ID 追加演出指令。Table PreviewAll 无诊断，仅三份 ETBL 内容变化；BakeAndGenerateAll 已执行成功。
- `Narrative/NovelSession.cs`、`NovelPresentation.cs` 接独立资源所有权、按类型/路径复用句柄、打字机、串行转场、错误和暂停；`NarrativeRunner.cs` 增加外部演出错误入口；`NarrativeDiagnostics.cs` 增加 Table/Page 等待。Session 直接注册现有只读观察源。
- `Packages/com.ember/Audio/Runtime/EmberAudioPlayback.cs`、`EmberAudioManager.cs` 提供可暂停/释放 SFX 和 BGM 暂停；`Packages/com.ember/UI/Runtime/EUIManager.cs` 的 ShowMainPage 增加可选 isCurrent，入队处理和资源返回时检查，旧会话页面不入栈并释放 Provider 资源。现有调用省略参数时行为不变。
- 用户反馈 Loading 退出动画访问已销毁 CanvasGroup。修复当前 Assets 中 `UI/Runtime/Framework/EUILoadingPage.cs` 的异步循环，每次继续写入及进入末尾都检查 Unity 对象存活。本文件是模板制作项目的框架页面源工作副本；本次最小修复涉及 Lifecycle 块，不修改 Binding、Templates~ 或父快照，不回写 base。后续通用 base 维护需单独同步，不隐式升级其他模板。
- `NarrativeModule.cs` 增加 SceneLoading 暂停，场景遮罩结束之前小说不消费输入；阅读资源仍在加载时可以返回菜单。此规则避免前一场景遮罩尚未退出时触发第二个 Loading 转场。
- API 速查和框架 Unreleased 记录新增接口；本模板 Design/README 更新当前入口及已确认语义。

本轮验证与失败（不能合并为全部通过）：

1. 用户确认此前正式 UI/Gameplay 批次手动编译无错。
2. Unity MCP 新会话测试任务 `39c30c5284334f558fdd64256880958e` 返回 **5/5 通过**：Ready、快速点击/嵌套暂停、加载中退出/迟到结果、重复释放与 SO 不变、准备失败。
3. 新 `Tests/NovelGameplayTests.cs` 初次编译发现 GameLauncher 无 TryGetInstance，已改用实际 Instance API。后续 MCP 错误查询 0。
4. 首次正式链路测试失败于错误断言：文本补全合法更新 PositionVersion，测试应检查 CommandId 未跨句。修正后原生报告 `.utmp/visual-novel-m1e/tests-20260917-125737907.xml` 为 **1/1 通过**，覆盖真实主菜单按钮、配表/音画资源、两轮分支结局与返回、设置暂停、会话释放及 SO 序列化内容不变。
5. 用户随后报告退出 Play Mode 的 Loading CanvasGroup MissingReferenceException；已加入存活检查。新增延迟 UI Provider 测试暴露场景 Loading 尚未退出就请求返回导致第二次转场等待，报告 `tests-20260917-130035461.xml` 为 **0/1 通过**。已补 SceneLoading 门控，测试将场景转场与阅读资源加载明确分开。
6. 最新修复后 MCP 编译错误查询 **0**。完整小说回归任务 `c06948c0a9a54244be3683962b9c5c8e` 的结果查询遇 Play Mode 域重载断连；即时检查没有该轮新原生报告，**完整回归结果未知**。按 CLAUDE 停止重试并请用户确认恢复。没有使用 Editor.log、进程轮询、dotnet 或 BatchMode 替代。
7. `git diff --check` 无空白错误（仅换行格式提示）。M1E 的 44/44 仍仅为历史基线。

待本阶段继续：取得本轮完整结果；验收 Loading 销毁与迟到页面释放；检查实际 UI 截图、补音频所有权与故障路径验证；核对观察窗口真实演出等待与暂停；完整预览项目中心差异后再 Save/Bump。尚未封存、提交或发布，未进入 M3～M5。

### 2026-09-17 M2 最终验收与封存

用户确认全部测试通过、无报错后，核对 Unity 原生报告：

- 小说回归 `.utmp/visual-novel-m1e/tests-20260917-130617253.xml`：**50/50 通过，0 失败、0 跳过**。
- 全项目回归 `.utmp/visual-novel-m1e/tests-20260917-131027932.xml`：**380/380 通过，0 失败、0 跳过**；没有 Loading CanvasGroup MissingReferenceException 或“过渡异常”记录。该轮为用户触发并确认，结果由实际 XML 核对。
- NovelGameplayTests 包含延迟阅读 Prefab、加载中退出、迟到请求释放且无页面入栈；实际菜单按钮→两轮分支→结局→菜单；快速补全、设置暂停、重复会话释放、观察源清理及所有样例 SO 内容保持。最新源码与这两份报告之间没有 C# 改动。
- MCP 实际运行额外核验：正文、背景、两个人物及选择画面正常，截图 `.utmp/visual-novel-m2/reader-live.png`、`choices-live.png`。自动测试截图未捕获到有效阅读画面，不作为视觉验收依据。
- 实际 PlayOwnedSFX 暂停后 IsFinished=false，Dispose 后为 true；返回 Main 后 Session/观察源均清空，AudioHost 下正在播放的 AudioSource 数为 0。
- 正式会话中打开现有 NarrativeGraphWindow，ObservedSnapshot 为 AwaitingChoice，PauseReasons 同步显示 M2Inspection；解除暂停并返回菜单后退出 Play Mode。为无人值守检查临时开启运行时 runInBackground，检查后恢复 false；未改变项目设置。
- 预览识别出一处 Editor 布局的 0→-0 序列化噪音，已用本轮开工备份恢复；其余既有布局、GUID 和稳定 ID 保留。

M2 实施与验收已完成；封存只收本模板 M1E 尚未保存的模块开关增量、M2 源码/四个正式 EUI/输入/音画样例及文档。框架通用 Audio 和 EUI 请求接口保留于 Packages 源码，未夹带到 base 或 2.5D 模板。封存目标为 **visual-novel 0.3.1 / preview**，父基线仍为 **base 0.6.4**，尚未发布。

遗留范围：美术/音频为占位资产；仅完成桌面横屏编辑器运行验收，未做 Player 构建、移动端适配或主观音质验收。存档兼容、Voice、自动/快进及正式交付属于 M3～M5，未实现也未默认确认 §9 的存档选择。Loading 页同类修复在 base/其他模板中的推广需独立维护。M2 无已知阻塞项；等待用户“继续下一阶段”。

封存前项目中心最终预览：111 项差异，预览错误/警告均 0；完整项目扫描 0 错误、3 警告，均为基础类型 MainState/GameplayState/SettingsState 未关联场景的提示。当前实际 Game 派生状态已通过真实场景切换测试，未为消除提示擅改既有场景映射。资源/.meta/GUID、配表生成物与 10 个 UI Prefab 生成链路检查通过。完整报告保存在 `.utmp/visual-novel-m2/template-preview-final.json` 与 `project-validation.json`。

实际封存：项目中心 SaveTemplate 返回 522 个文件，BumpTemplateVersion(visual-novel, 1) 成功，元数据为 0.3.1 / preview，父 base 0.6.4，内容 hash 与封存 hash 相同。补写本结果后仅再次预览/保存 README 与本清单；SetTemplateVersion 保持 0.3.0 被版本保护拒绝，按既有流程补丁 Bump 为 0.3.1，未修改模板文件或 hash。未提交 Git、未发布 tag、未安装 Fungus、未操作 Unity Farm。

## M3 工作副本记录（验收未完成）

> 本节为 M3 初次实现批次，后续测试/UI/Loading/编辑器记录继续更新；这里的未运行、缺少 meta、未修改框架等结论只对应当时快照。当前剩余项以顶部收口清单为准。

本轮依据用户明确授权只实施 M3；当前加载记录仍是 visual-novel 0.3.1 / preview，父 base 0.6.4。未切换模板、未编辑 Templates~ / ParentSnapshot~ / hash、未改框架源码、未提交 Git 或发布，未操作 Unity Farm。M4/M5 未实施。

### 实际文件与实现

| 文件（相对项目根目录） | 本轮变化 |
|---|---|
| `Assets/Game/Module/Narrative/NovelCheckpoint.cs` | schema 1 快照、演出/历史 DTO 和语义指纹 |
| `Assets/Game/Module/Narrative/NarrativeRunner.Checkpoint.cs` | 稳定点捕获、ID/变量/选项校验和不执行前置指令的恢复 |
| `Assets/Game/Module/Narrative/NarrativeRunner.cs`、`NarrativeDiagnostics.cs` | partial 扩展、章节/选择自动存档触发代次、Restoring 状态 |
| `Assets/Game/Module/Narrative/NovelSession.Checkpoint.cs`、`NovelSession.cs` | 演出快照、候选资源预备、历史上限/已读事件、显式提交与完整释放 |
| `Assets/Game/Module/Narrative/NarrativeModule.cs`、`NovelPresentation.cs` | 保留现有模块；候选请求、会话交换、观察源与提交后故障返回 |
| `Assets/Game/Module/NovelSave/` | 新 Global/Enabled=true Module、槽位索引、可靠写盘、独立账号已读/偏好、自动/快速保存与防重复 |
| `Assets/Game/UI/Editor/NovelM3UIBuilder.cs` | 经开发中心预检/创建/实际绑定/生成两份新 Prefab，追加现有入口；已存在对象不重新布局 |
| `Assets/Game/UI/Runtime/VisualNovel/{EUINovelSavePage,EUINovelSaveSlotItem}.cs` 及实际 `.Binding.cs` | 正式槽位页面/Item 与二次覆盖确认、取消、成功/失败反馈 |
| `Assets/Game/UI/Runtime/VisualNovel/NovelSaveUI.cs`、`EUINovelReaderPage.cs` | 页面意图、继续只读最后成功指针、阅读入口、会话替换后重接按钮 |
| `Assets/Game/UI/Runtime/MainScene/EUIMainPage.cs` 及实际 Binding、`Assets/Game/State/GameGameplayState.cs`、`GamePages.User.cs` | 主菜单/Gameplay 恢复路径与资源注册 |
| `Assets/GameResource/Resources/UI/VisualNovel/Prefabs/` | 新存档页/槽位 Item，更新阅读页正式资源 |
| `Assets/GameResource/Resources/UI/Common/Prefabs/EUIMainPanel.prefab` | 追加继续、读档与反馈控件，保留原按钮和框架绑定 |
| `Assets/Game/Module/Narrative/Tests/NovelCheckpointTests.cs`、`NovelSessionTests.cs` | 新增恢复/兼容/可靠写盘/失败/取消测试，尚未运行 |
| `Assets/Game/Module/Narrative/Tests/NarrativeTestReport.cs` | 后续小说测试原生 XML 保存到本轮 `.utmp/visual-novel-m3` |
| 小说 Editor / Tests 与 UI Editor `.asmdef`、`NarrativeEditorAvailability.cs` | 明确程序集依赖及存档模块感知的制作入口 |

测试用例包括稳定点、不重复赋值/SFX、旧输入代次拒绝、兼容文字修订/登记与选项重排、拒绝语义变化/未知版本/缺失 ID/非法变量/选项集合、全局与局部同名隔离/跨章恢复、自动保存触发、索引提交失败保留旧成功、损坏继续槽不回退、损坏索引不覆盖、独立账号数据、候选失败/取消/缺少背景或 BGM 不触碰原页面。**本轮这些测试未执行，不提供通过数。**

常规实现选择见 Design §16。可靠写盘先留小说业务层：现有 DataSaver 没有结果/事务契约，而本次索引提交与不可变快照的组合属于小说存档业务，没有为此增加通用框架 API。历史上限 200，单文件 4 MiB，旧 payload 和 index.bak 不自动回退或清理；缩略图延后。

### 实际验证、失败与证据

1. 只读核对 AGENTS/CLAUDE、指定文档、git status/暂存区、当前编辑记录、模板元数据与实际源码；确认 visual-novel 0.3.1/base 0.6.4。原有大批未提交改动保留，当前工作副本作为唯一开发起点。
2. 本轮备份为 `.utmp/visual-novel-m3/current-turn-before/{Game,GameResource}` 与 `status.txt`；之前 `before/Assets` 备份也保留，没有覆盖。
3. MCP 精确选择 `ember-unity-framework@b23e9b768e85ed30`，读取 project/info 确认根目录与目标一致，初始 Editor 空闲。
4. 第一批 scripts 刷新没有导入新增 partial 文件，控制台出现成员缺失错误；随后通过 MCP 完整 Refresh 修正导入，该中间快照控制台错误 0，`isCompiling=false`、`scriptCompilationFailed=false`。**这不是最终实现编译通过。**
5. 新 UI 制作器加入后的编译检查返回 `Unity session not ready for read_console (ping not answered)`。同一次已排队编排中的 execute_code 随后返回制作成功，真实两份 Prefab 与四份页面/Item Binding 已生成。本轮从收到结果后按 CLAUDE 停止自动编译检查，不再刷新/轮询编译，不读取 Editor.log、不使用 dotnet/BatchMode 等替代验证。
6. 生成后实际读取 Binding，再完成用户逻辑。发现注册路径被生成器写回 `UI/Module/VisualNovel`，已修正用户注册与制作脚本；保留默认创建目录的空 Animator/Atlas/Prefabs 元数据，没有把它们当成可用 UI。生成报告位于 `.utmp/visual-novel-m3/bindings/`；真实控件字段以 `.Binding.cs` 为准（报告 DTO 未序列化 Entries）。
7. 后续一次文件补丁遭遇 Windows sandbox IPC 超时；核对文件后重新执行成功，没有用未落盘操作冒充完成。
8. `git diff --check` 静态检查无差异格式错误。以备份逐文件 SHA-256 比较并保存 `.utmp/visual-novel-m3/current-turn-diff.json`；五个受管目录与当前封存快照的完整静态比较为 `static-template-preview.json`，脚本 `audit.ps1` 可重跑。它是静态文件清单，**不能替代项目中心预览、Unity 编译或运行验收**。

### 仍需完成的 M3 验收与封存

- [ ] 请在 Unity 中手动触发编译；本轮最终实现未完成 Unity 编译验证。如仍有报错，回传首条错误及完整堆栈。
- [ ] 编译通过后运行 `Game.Narrative.Tests`（包括新 M3 测试）并进行全项目回归，记录本轮原生 XML；不沿用 M2 的 50/50、380/380。
- [ ] FrameworkScene Play Mode：完整台词、选择前、选择后分别保存，退出重进后读取；核对当前句、选项、全局/局部变量、背景/立绘/BGM。手动槽 1～6、快存/快读、自动槽和继续均验收；非稳定点不可写、失败不显示成功。
- [ ] 原 A 会话加载 B，准备中取消/缺资源/坏档恢复 A；提交后页面/音频故障释放并回菜单。观察窗显示候选 Restoring，失败返回 A，成功只跟踪新会话；验证旧输入和迟到资源无污染。
- [ ] 主菜单/阅读页/存档页实际视觉、模态暂停、重复打开关闭、连续读取与资源释放验收。部分新脚本 `.meta` 尚待 Unity 导入生成，不手工制造 GUID。
- [ ] 最终文档与实际结果同步后，经项目中心完整预览五个受管目录，排除夹带，再保存并显式 Bump。当前仍为 0.3.1，不封存未验证 M3，不发布 tag。

当前阶段为 **M3 已落盘、验收未完成**，不是“阶段完成”。先完成以上验收和封存，再等待用户“继续下一阶段”；不得据此进入 M4/M5。

静态收口结果：本轮相对备份 51 个新增/修改文件、0 删除；五目录静态预览 51 项。两条实际用户 PageDef 均指向存在的 Prefab；6 个新脚本/生成文件尚缺 .meta，等待 Unity 导入。真实控件清单补存于 bindings/actual-generated-controls.json，验证状态为 verification-status.json。标准 git diff --check 返回 0；测试源码包含 19 个新 Checkpoint 测试实例及 4 个新增 Session 测试实例，均未执行。

## 2026-09-18 用户首轮验收失败修正

用户提供 `TestResults_20260918_102612.xml`：小说 73 项、71 通过、2 失败。报告已保留为 `.utmp/visual-novel-m3/user-tests-20260918-102612.xml`。M3 新增存档测试均通过；失败为：

- `NarrativeAvailabilityTests.RestoredWindowClosesWithoutBuildingGraphWhenDisabled`：假定两个测试帧内已执行 EditorApplication.delayCall，窗口仍存活。生产窗口改为一次性 EditorApplication.update 关闭回调，调用前自注销，OnDisable 也清理；不引入每帧模块检测。测试改为有界等待真实销毁，并在失败时清理窗口。
- `NovelGameplayTests.RealMenuReaderBranchEndingAndRepeatedExit`：主菜单等待超时。未改运行入口的情况下，单独原样重跑 1/1 通过，原生报告 `tests-20260918-022918451.xml`。现场配置 runInBackground=false；因此将 Gameplay 整合测试在 Play Mode 内暂时设为 true，finally 恢复，并增加失败后的 ExitPlayMode 清理。失焦导致运行循环与测试帧计时不同步是已处理的时序风险，并非已证明主菜单业务存在确定性故障。

修正后 MCP 确认 `isCompiling=false`、`scriptCompilationFailed=false`、error CS=0。完整小说测试 **73/73 通过、0 失败**，原生报告 `.utmp/visual-novel-m3/tests-20260918-023256858.xml`。MCP 的测试 job 状态在跨 Play Mode 域重载后没有收到最终回调，结果以原生 XML 为准，不把 stale running 状态当作测试仍失败。

随后发现原有真实 Gameplay 测试在 M3 会触发自动槽/已读写盘，因此仅在测试内将 Store/Account 注入独立 `.utmp/visual-novel-m3/gameplay-tests/<guid>` 目录；不修改业务存储路径和项目 runInBackground 设置。此前原样运行可能已更新实际自动槽/已读，未自动回滚或删除它们。隔离补丁之后 MCP 再次确认编译成功。

全项目 EditMode 回归 job `9b23dc62fb494989b135bf2e4d33fed3` 已启动，进度快照到 283/403 时记录一项失败：`Ember.UI.Tests.EUIDevelopmentCenterEditTests.CreateCustomSettings_WithGlobalSimpleNameCollision_ShouldUseGeneratedNamespaceType`，原因是“Unity 仍在编译或更新资产，请等待完成后再创建自定义 Settings 实例”。这不是小说测试失败；本轮未改框架源码或该测试，不能把它静默计为通过。随后 get_test_job 返回 TimeoutError，按 CLAUDE 停止后续自动验证，尚未取得全项目最终 XML，也没有完成所计划的单项复测。

当前最新确定结论：窗口/Gameplay 时序修正后的小说 73/73 通过；追加测试存储隔离后编译已确认，但最终整轮回归未完成确认。若 Test Runner 仍在运行，待结束后导出新的 XML；继续人工验收存读档页面与恢复行为。M3 尚未保存/Bump，仍是 visual-novel 0.3.1 / preview。未推进 M4/M5，未提交或发布。

## 2026-09-18 存档页可读性与点击停顿修正

本轮范围为模板 UI/存档优化，加上必要的 EUI 框架类型查找优化；没有修改 Templates~、ParentSnapshot~、hash，没有保存/Bump、Git 提交或发布。

### 实际原因与修改文件

- 正式 SavePage 的 VerticalLayoutGroup.childControlWidth=false，使 Item 保持默认 100 像素宽。`Assets/Game/UI/Editor/NovelM3UIBuilder.cs` 增加显式制作入口 `Ember/Visual Novel/M3/优化存档页布局`，检查两个模块开关、模板身份和非 Play 状态。已通过 MCP 调用该入口，以 PrefabUtility 修改 `Assets/GameResource/Resources/UI/VisualNovel/Prefabs/EUINovelSavePage.prefab` / `EUINovelSaveSlotItem.prefab`，由开发中心重新生成两份 Binding 并实际读取，保留现有字段和用户区注册路径。
- 槽位改为全宽八行、10 单位行距、两行摘要，保存/读取并排；空槽与自动槽按钮状态分明。`EUINovelSavePage.cs` 在页面实例生命周期内复用 Item，在 Dispose 释放；`EUINovelSaveSlotItem.cs` 格式化两行文字，摘要内换行折叠，超长内容省略。
- `Packages/com.ember/UIExtension/Runtime/EUIBindingBridge.cs`：Extension 完整类型名不再对每个程序集 GetTypes；增加成功的 Component 类型缓存。实例仍从当前 GameObject 取得，失败不缓存，短名与基类过滤保留。现场修改前单 Item TryCreate 为 237.40 / 217.92 / 230.15 ms，八项绑定足以造成近两秒停顿；优化后的时间尚未复测。
- `Assets/Game/Module/NovelSave/NovelSaveStore.cs` / `NovelSaveModule.cs`：在主线程捕获/序列化独立快照，后台执行磁盘读写，主线程 Update 处理结果与候选会话。槽位 payload 和 index 均实际提交后才报告成功。槽位索引用不可变候选整体发布，元数据副本不再用 JSON 往返克隆。已读/偏好按序后台写入，合并待写的最新账户状态。Save/SavePreferences 的 bool 表示请求受理，最终结果从 Changed/Message 读取。
- 后台写入期间仍捕获章节/选择后的首个稳定点，自动槽快照排队提交；失败不逐帧重试。文件读取阶段即可取消，完成后的迟到结果不创建会话、不提交。正常退出时待写账户快照交给原顺序队列；强制结束进程时未完成的后台写入不承诺持久化，玩家以保存成功提示为准。
- 回归补在 `NovelCheckpointTests.cs`（后台提交前不可见、并行写拒绝、异步索引失败保留原最新、账户顺序/快照隔离）、`NovelSessionTests.cs`（读取阶段取消后迟到结果不可提交）、`Packages/com.ember/Tests/EditMode/EUIDevelopmentCenterEditTests.cs`（完整/短名、类型过滤、缓存不串组件实例）。本轮新增共 4 个用例，尚未执行。

### 本轮证据与验证边界

- 编辑前核对 visual-novel 0.3.1 / preview 与现有 git status。备份 `.utmp/visual-novel-m3/ui-polish-before/`；保留其他未提交内容。
- MCP 精确定位 ember-unity-framework@b23e9b768e85ed30，并核对 projectRoot；没有操作 Unity Farm。小文件同步账户写盘采样 4 / 9 / 6 / 5 ms，也从交互线程移走。
- 本轮 read_console 返回 ping not answered；同一个已排队工具调用返回中间状态 failed=false，但没有以此替代最终验证。此后停止编译检查，不再 refresh/轮询编译/运行测试，也没有使用其他编译方式。后续只执行资产制作与离屏视觉检查。
- 实际正式 Prefab 实例离屏预览（示例数据，不读写玩家进度）1920×1080：八个槽位均为约 1552.13×91.93，间距 10，无重叠；检查图片 `.utmp/visual-novel-m3/save-layout-preview.png`。这不是 Play Mode 行为或帧耗时验收。
- 找到上一轮补出的原生报告 `tests-20260918-024542053.xml`：403/403，2026-09-18 02:45:14Z～02:45:42Z；此前失败报告仍保留。该报告早于本次优化，不能计为本轮通过。
- 静态 git diff --check 无格式错误。当前 Unity MCP 编译检查曾不可用，本次未完成最终 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请发送首条编译错误及其完整堆栈。

下一次验证：重新编译后运行小说测试与上述 EUI 绑定测试；Play Mode 连续开关存档页，检查八槽/覆盖确认/快存快读、保存期间界面响应和取消读档，再复测绑定耗时。完成后再恢复 M3 完整验收与项目中心差异预览/保存/Bump。仍不进入 M4/M5。

## 2026-09-18 主菜单新增按钮对齐原样式

按用户要求，仅调整 visual-novel 工作副本的主菜单。`EUIMainPanel.prefab` 中继续、读取存档及反馈文字从根节点移入原 `Animator/EUISafeArea/Center`；四按钮按开始游戏/继续/读取存档/设置居中纵排，中心 Y 为 150/50/-50/-150，统一 300×80、行距 20。新增按钮复制原开始按钮的蓝色 RGBA(0.25,0.45,0.8,1)、字体/28 字号/白色文字与 ColorTint 状态配置，反馈文字放在下方。

实际检查确认新增按钮前后均无独立 Animator。原页面只有 `Animator` 节点上的 `EUICommon_Ani.controller`，Enter/Exit 只驱动该节点 CanvasGroup.alpha；旧新增按钮位于该层级外导致不同步。现在新增控件随原有整页淡入淡出，不增加动画或修改共享动画资产。

本轮使用 ember-eui-build 流程，备份于 `.utmp/visual-novel-m3/menu-align-before/`；经 MCP 确认目标实例与 visual-novel 身份，以 PrefabUtility 修改正式资产，调用开发中心 TryRegenerateCode，再读取真实 `EUIMainPage.Binding.cs` 验证五项引用。用户脚本逻辑未改。`NovelM3UIBuilder.cs` 增加显式菜单“Ember/Visual Novel/M3/统一主菜单按钮样式”；首次创建使用相同布局，已存在按钮按 Binding 名判断，避免移动后再次 Build 创建重复对象，不在域重载时自动重排。

实际 Prefab 离屏预览 `.utmp/visual-novel-m3/main-menu-aligned.png` 已检查：四按钮居中、颜色一致、文字无重叠；安全区按完整画面进行预览，尚未进行 Play Mode 动画验收。静态 git diff --check 返回 0。本轮刷新后 editor/state 报 ping not answered，因此依 CLAUDE 停止自动编译验证；后续仅完成资产制作和离屏预览，没有通过其他方式替代编译。当前 Unity MCP 编译检查不可用，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请反馈首条编译错误及完整堆栈。没有改其他模板快照或框架源码，未保存/Bump、未提交或发布，仍为 M3。
2026-09-18 文案微调：按用户要求，将当前 visual-novel 工作副本 EUIMainPanel 的 m_Btn_Start 文案由“开始游戏”改为“新游戏”。通过 PrefabUtility 修改并重新加载核对；布局、Binding 与点击逻辑不变。MCP 确认 isCompiling=false、scriptCompilationFailed=false。

## 2026-09-18 Manager/Module 分类与存档启动时机确认

用户确认分类依据为职能与可选性：Manager 是框架不可按玩法裁剪的必要基座，Module 是可选装/移除/禁用的业务积木；不以是否在 Init 启动或是否常驻区分。核对 InitState.OnEnter，顺序为 DiscoverModules → InitializeAll Managers → InitPhase(Global) → CoreReady → 主场景流程。NovelSaveModule 当前明确 Global / Enabled=true，OnInit 构造 Store 读取槽位索引并读取账号数据，在主菜单前完成；不需要改变模块归属或启动代码。

本轮仅补正文档：CLAUDE、Core/Runtime/Manager/README、Documentation~/core/README、API 速查与启动时序，消除“Init 仅发现 Module”的歧义。主菜单依赖索引就绪，完整恢复仍在玩家选定进度后执行；未来若异步初始化须显式区分未就绪、无存档与读取失败。五按钮及显隐需求不由本轮文档修改自动实现，尚待后续 UI 落地。文档审计前后均 198 篇、0 问题，strict 通过，git diff --check=0；备份及报告在 .utmp/visual-novel-m3/module-doc-*。本轮没有修改 C#、Prefab、模板快照或发布内容，不需要 Unity 编译验证。

## 2026-09-18 五按钮主菜单落地与验证

已按用户确认规则修改当前 visual-novel 工作副本：顺序为新游戏、继续游戏、读取存档、设置、退出游戏。无槽位索引记录时隐藏继续/读取；仅快速或自动存档时只显示继续；至少一个手动槽记录才显示读取。索引损坏反馈错误，不把记录存在视为完整存档有效性校验。其余按钮由 Prefab 的 VerticalLayoutGroup + ContentSizeFitter 自动收拢并整体居中，保留原有蓝色、尺寸和整页 Animator 层级，未增加独立按钮动画。

实际文件：
- `Assets/GameResource/Resources/UI/Common/Prefabs/EUIMainPanel.prefab`：新增 MenuButtons 布局容器和 NovelQuit，新增退出 Binding；六个绑定均实际生成并读取，原两个 Framework 标记保留。
- `Assets/Game/UI/Editor/NovelM3UIBuilder.cs`：显式制作入口更新为五按钮和自适应布局，退出项按 Binding 幂等检测；修正 BindingEntry 为 struct 的判断。
- `Assets/Game/UI/Runtime/MainScene/EUIMainPage.Binding.cs`：开发中心重新生成，实际 NovelQuit 类型为 UnityEngine.UI.Button。
- 同目录 `EUIMainPage.cs` 用户区：根据索引刷新显隐/交互状态，退出调用 GameLauncher.Quit；框架生命周期块未改。
- `Assets/Game/UI/Runtime/VisualNovel/NovelSaveUI.cs`：Continue 直接 BeginLoad 最近成功槽位，不先打开选择页；提交前失败时才打开选择页，保留错误提示并允许玩家选择其他槽，不静默回退。
- `Assets/Game/Module/NovelSave/NovelSaveModule.cs`：BeginLoad 增加可选失败回调，在文件读取/候选准备失败时清理原状态后通知；取消不触发失败回调，成功提交时清除回调。
- `Assets/Game/Module/Narrative/Tests/NovelGameplayTests.cs`：新增四种显隐/布局断言及真实新游戏→快存→菜单继续→手动存档→菜单读取的流程测试，所有写盘使用独立测试目录。

本轮验证：
- 编译通过：MCP 返回 isCompiling=false、scriptCompilationFailed=false，error CS 查询 0；追加流程测试后再次确认编译状态正常。
- 显隐/布局 4/4 通过，原生报告 `.utmp/visual-novel-m3/tests-20260918-040110939.xml`。
- 真实 Gameplay 流程 1/1 通过，原生报告 `tests-20260918-040301204.xml`：继续恢复原指令且不弹槽位页，只有快速档时隐藏读取，有手动档后读取打开选择页且不启动会话。MCP job 在跨 Play Mode 重载后仍显示 running，实际 Editor 已退出 Play 且 tests.is_running=false，结论取原生报告。
- 三种正式 Prefab 离屏预览 `main-five-buttons-0/1/2.png` 已人工视觉检查，3/4/5 按钮均居中、不留空行；退出未实际触发关闭 Editor/进程，只核对框架 Quit 接线。
- 制作中先遇到 BindingEntry 误判为 null，已修正；一次临时制作代码将退出类型命名空间重复拼接，已通过修正真实 Binding 条目并重新生成解决，没有手改生成文件。首次预览被 EUIPage 构造时 alpha=0 隐藏，修正预览宿主可见状态后重新检查，未修改运行时隐藏契约。
- git diff --check 通过。备份在 `.utmp/visual-novel-m3/five-buttons-before/`。未修改模板快照、hash 或其他模板；未保存/Bump、未提交或发布。本轮菜单需求已落地，M3 整体验收与封存仍待完成，不进入 M4/M5。

## 2026-09-18 M3 读档复用无进度条 Loading

用户明确授权使用现有 Loading 防止读档穿帮。仍为 visual-novel 0.3.1 / preview、父 base 0.6.4 的 Assets 工作副本，未保存/Bump。备份和本轮开始状态：`.utmp/visual-novel-m3/loading-cover-before/`、`loading-cover-status-before.txt`。保留其他未提交改动。

实际改动：
- `Packages/com.ember/UI/Runtime/EUIManager.cs`：补充通用 `RunWithLoadingAsync`，等待真正 Show 完成及一帧后执行异步操作，成功留出两帧再关闭；异常也关闭。持有期间的场景切换沿用 SceneCoordinator 正常加载/PrepareEnter/Proceed/卸载，复用同一个 Loading，业务等待目标内容完成；普通场景 Loading 原路径保留。这是必要的框架通用能力增量，业务恢复逻辑仍留在 Assets。
- `Assets/Game/UI/Runtime/Framework/EUILoadingPage.cs`：仅本轮用户区改动，在 OnOpen 识别恢复参数，启用 SkipFakeProgress、隐藏真实进度绑定，临时启用已有 CanvasGroup 的射线阻挡并在关闭时还原；准备中 Novel/Menu 输入触发取消。实际 Prefab/Binding 已通过 Unity 读取核对，未增加 Prefab、独立动画或修改生成绑定；本轮 EmberManaged 与开工备份逐字比较相同。
- `Assets/Game/UI/Runtime/VisualNovel/NovelSaveUI.cs`：统一遮挡内的 BeginLoad/候选提交/跨场景等待；从请求开始暂停原会话，到遮挡退出才解除；防重入与遮挡进入期取消；提交后失败等待返回菜单页面就绪。失败选择页在遮挡退出后打开。
- 同目录 `EUINovelReaderPage.cs`：快速读档成功不先弹槽位页，失败才打开；`EUINovelSavePage.cs`：遮挡期间禁用保存操作并把关闭取消交给统一入口。
- `Assets/Game/UI/Runtime/MainScene/EUIMainPage.cs` 用户区：遮挡进入/退出期间同样视为 UI 忙碌，防止主菜单重复操作。
- `Assets/Game/Module/NovelSave/NovelSaveModule.cs`：可选取消完成回调；提交回调抛错也通知失败，保证等待者能收束，不再遗留遮挡。取消后后台读取可结束但不能创建或提交会话。
- `Assets/Game/Module/Narrative/Tests/NovelGameplayTests.cs`：扩展真实入口测试，含慢阅读页面、单一遮挡、无进度条、同场景快读、损坏保留、进入期取消、手动槽成功。`NovelCheckpointTests.cs` 新增取消只通知一次且迟到读取不能提交。
- README、Design、本文及 `docs/dev/ember-api-reference.md` 同步行为与接口。

验证过程：首次工具临时代码误用 BindingEntry.WidgetType 字段，工具 CodeDom 报错；按实际 Type/Name 修正后成功取得绑定与射线状态（该临时脚本错误不属于项目编译）。MCP 已确认新增代码编译通过，isCompiling=false、scriptCompilationFailed=false。首轮真实流程 1/1 通过，报告 `tests-20260918-042442899.xml`，随后增加取消与手动槽断言并再次编译通过。MCP 测试作业在 Play Mode 域重载后残留 running，已用完整原生报告及 Editor 非 Play/tests.is_running=false 核对后清理旧作业，继续专项回归。

专项回归第一次为 27/28（`tests-20260918-042906427.xml`）。失败发生在新增取消测试：页面退出帧已不可见，但 UniTask 取消收束/主菜单交互刷新尚未执行，断言过早读到了“正在准备读档…”。测试改为等待完整取消反馈与主菜单按钮恢复，并在从 Gameplay 返回菜单后等待其 Loading 退出再操作；保留原断言，未掩盖业务错误。普通新游戏/分支/反复退出及存档单元测试均已通过，修正后继续验证完整入口测试。

最终结果：修正测试等待时机后，完整读档入口流程重跑 1/1 通过，原生报告 `.utmp/visual-novel-m3/tests-20260918-043113844.xml`；与前次通过的 27 项组成此次 28 项专项覆盖（不是声称单次 28/28）。包含慢阅读页持续遮挡、跨场景只有一个 Loading、隐藏进度条、快速读档同场景交换、损坏文件与原会话保留、遮挡进入期取消后再次读取，以及手动槽完整恢复。模块取消一次性通知测试和原有新游戏/剧情分支/反复退出也通过。最后代码编译经 MCP 确认为 isCompiling=false / scriptCompilationFailed=false，Console error 查询 0；git diff --check=0。当前 Editor 已退出 Play，后一次作业同样按完整原生报告认定结果。

遗留边界：未进行大体积存档的性能基准，遮挡不保证消除主线程解码/Unity 资源构建耗时；本轮未重新跑全项目测试，也未注入验证“提交后音频或页面不可恢复失败”的完整运行路径，该路径仍遵循既有 M3 语义。没有更改 Prefab 布局、美术或新增动画；未改 Templates~/ParentSnapshot~/hash，未封存/Bump、提交或发布。M3 整体验收与封存仍待后续明确安排，不进入 M4/M5。

## 2026-09-18 用户追加：Gameplay 主UI布局编辑器

用户确认仅编辑 Gameplay 阅读主界面，不包含启动主菜单。新增 `Assets/Game/UI/Editor/NovelGameplayLayoutWindow.cs`（及 Unity 生成 meta），入口 `Ember/Visual Novel/Gameplay 主UI布局`。依现有 Game.UI.Editor 程序集使用 UI 开发中心公开 Binding 快照/校验接口，不新建 Module，不修改生成 Binding，不增加运行时布局覆盖配置。

窗口从正式 EUINovelReaderPage 的真实绑定定位 15 个布局元素（对白框通过 Speaker 的真实父级定位），支持实际 UI 预览、选中元素拖动、数值位置/尺寸增量/锚点/轴心/缩放、选项间距、Ctrl+Z、四种参考画布与示例选项。立绘槽布局影响该槽内所有角色，角色素材仍由剧情/配表选择。显示样例在独立预览克隆中，保存只操作 LoadPrefabContents 的正式资源工作副本，通过 SaveAsPrefabAsset 保存并在释放时 UnloadPrefabContents。

保存前检查 visual-novel 编辑身份、NarrativeModule.Enabled、非 Play Mode、同一 Prefab 未在 Prefab Mode 打开；使用资源依赖 hash 拒绝覆盖外部变更。每次保存备份 Prefab/meta 和真实 Binding 快照到 `.utmp/visual-novel-ui-layout/时间戳/`。未保存布局以序列化草稿跨源码域重载保留；模拟 OnDisable/OnEnable 不等于关闭后重开或重启 Unity 的持久化保证，外部资源已变化时不自动套用。标题栏关闭使用 EditorWindow 标准未保存提示。

已验证：Unity MCP 精确锁定 ember-unity-framework，源码编译成功；实际绑定 15 项；真实鼠标 MouseDown/Drag/Up 事件把 20×10 预览像素映射为 52.03×-26.02 画布单位，与预期一致，Undo 恢复；保存位置改动、重载读回、再恢复原位置均成功，GUID 与原对白保持不变；模拟窗口生命周期重建后草稿保留；外部 hash 冲突拒绝保存，源文件未变化。检查结果在 `.utmp/visual-novel-ui-layout/validation.json`，初始资源备份 `before-smoke.prefab`。本轮不改变默认布局；Unity 保存往返产生 TMP 的 m_fontColor32 / m_TextStyleHashCode 规范化，已完整比较差异，仅这些派生序列化字段变化，未改变文本、颜色语义、位置、绑定或 GUID。

预览验证发现重建后空白：Canvas 先启用再移入 Preview Scene 可能保留旧场景剔除信息。已改为先把宿主放进预览场景，再在宿主下实例化 UI，使 Canvas 从创建起属于目标场景；不使用“渲染所有场景”的绕过方案。排查时一次临时 MCP 代码使用旧版 CanvasRenderer.GetMesh 参数导致工具脚本编译失败，改用当前无参数接口后正常，不是项目 C# 编译错误。最终预览检查见下方补充。

预览排查补充：先前关于 GameViewObjects 剔除位的推测未通过重复重建验证，已撤回该实现。最终定位项目 URP 的 Preview 相机路径不绘制本预览的 uGUI；实际测试将独立预览相机设为 CameraType.Game 后，连续重建 16:9、4:3、自定义 1280×1024 均获得正确节点画面（采样颜色数 221/235/248，截图 game-0/2/4.png）。相机仍禁用自动渲染，仍限定在 PreviewRenderUtility 的独立场景，不修改任何场景剔除位；保留绘制重入保护及明确 viewport。最终代码使用同一修复并用 finally 配对结束预览。源代码核对参考 Unity 官方 PreviewRenderUtility.cs；排查中的临时材质/投影/手动网格方案没有进入正式代码。

### 追加：不同分辨率与节点预览

实际文件：
- `Assets/Game/UI/Editor/NovelGameplayLayoutWindow.cs`：四种分辨率加自定义宽高，按真实 CanvasScaler 换算逻辑画布；节点预览入口与独立预览相机渲染修复。
- `Assets/Game/UI/Editor/NovelGameplayLayoutWindow.NodePreview.cs`（Unity 生成 meta）：节点 SO 选择、跟随图编辑器和 Project、多句切换、节点内容变化刷新、只读配表加载、静态演出命令和选项文案预览。
- `Assets/Game/Module/Narrative/Editor/NarrativeGraphWindow.cs`：选中节点单向通知 UI 编辑器；侧栏“预览节点”按钮。UI.Editor 不依赖 Narrative.Editor，保持原有单向程序集关系。
- `Assets/Game/UI/Editor/Game.UI.Editor.asmdef`：增加 Ember.Table.Runtime、Game.Table.Runtime 引用以复用正式导表目录及解析器。
- 本目录 README/Design/Implementation 同步使用说明、静态预览边界与实际验证状态。

验证记录：
- 新增节点预览与程序集引用后，MCP 曾确认编译完成、Console error=0；对象反射已实际使用新增接口。
- 用现有序章开场节点的内存克隆追加第二句、隐藏左立绘及不存在变量的 SetVariable 指令：第二句文案、配表姓名“小艾”、左立绘清空均正确，没有执行变量命令。源 SO JSON、正式 Prefab 字节均不变，窗口无布局脏标记、Editor 非 Play。报告 `.utmp/visual-novel-ui-layout/preview-validation.json` 保留这一轮结果；其中 colors=1 是修复相机前的失败图像检查，不能当作最终渲染通过报告。
- 随后独立相机修复的连续分辨率检查通过，图像 `game-0.png`、`game-2.png`、`game-4.png` 与 `gamecam.png`；已查看 gamecam.png，确认序章真实对白和左右两张立绘，没有混入第三张样例立绘。
- 样例资产均为单句节点，首次寻找多句样例的工具检查失败，改为只在内存克隆上构造多句检查；未改写样例 SO。排查期间存在工具临时代码的旧 API、错误泛型以及 TMP_SubMeshUI 空材质恢复错误；没有采用这些诊断代码为正式实现。
- 最后把已验证相机配置及 finally 配对写入源码后，MCP `editor/state` 返回 session not ready / ping not answered。按 CLAUDE 停止自动编译验证，不再尝试替代方式；紧接该批次的 Console 返回 0 不能证明最终代码完成编译。最终版本必须人工触发 Unity 编译，再验收真实图节点联动、锁定跟随、内容实时刷新、选项文案、不同分辨率及布局拖动保存。未运行本轮全项目测试。

本轮保持默认阅读页布局（前述保存往返 TMP 派生字段规范化除外），未保存回模板或 Bump，未编辑 Templates~/ParentSnapshot~/hash，未提交、发布或进入 M4/M5。节点预览只计算所选节点内命令，不自动重演前置分支、变量、音频或存档；继承画面可显式指定预览图片。最终编译与交互人工验收仍是遗留项，不宣称整个 M3 已完成验收。

## 2026-09-18 本次文档收口

仅修改本目录 README/Design/Implementation 与仓库 `docs/README.md`，保留所有其他未提交改动。统一当前状态、历史报告适用批次、编辑器预览边界与接手顺序；纠正“功能尚未实现”“全项目最终报告未取得”和草稿跨关闭重开持久化的过度表述。未修改 C#、Prefab、模板快照或 hash，未触发 Unity 编译/测试、保存/Bump、Git 提交或发布。

静态核对 Unity 6000.5.4f1、manifest/lock 与本地解析的 URP 17.5.0；核对加载记录与元数据、实际布局/节点预览代码、原生 XML 和预览 JSON。维护备份、完整 git 状态及审计报告位于 `.utmp/ember-doc-maintenance/visual-novel-closeout/`。本次文档检查不改变此前最终 Unity 编译与交互待验收的结论。

文档审计前后均为 198 篇、0 问题，`--mode framework --strict` 通过；`git diff --check` 返回 0。链接审计不验证 Unity 功能，也不将历史失败报告改写为通过。

## 2026-09-18 用户 413 项报告：观察窗口超时

用户报告 `TestResults_20260918_143255.xml` 已备份为 `.utmp/visual-novel-m3/user-tests-20260918-143255.xml`：413 项、412 通过、1 失败、0 跳过。唯一失败 `NarrativeObservationPlayTests.PlayModeWindowReopenFollowPauseReplacementAndExitDoNotWriteAssets` 超过 180000 ms，实际用时约 198 秒，没有堆栈或步骤输出；同组章节总览观察用例通过（约 14.77 秒）。报告没有存档断言失败，也不足以确定超时发生在进入 Play、窗口检查还是退出阶段。

源码核对发现这两个较早的观察用例未像 NovelGameplayTests 一样处理失焦背景运行，也没有 UnityTearDown 在断言失败后退出 Play Mode。本次仅修改 `Assets/Game/Module/Narrative/Tests/NarrativeGraphTests.cs` 中这组测试：进入 Play 后临时启用 Application.runInBackground、finally 恢复；新增失败后 ExitPlayMode 清理；记录进入/检查/退出阶段日志。保留全部原观察/SO 不变断言，未增加 Timeout、未忽略失败或修改运行逻辑。这是针对已发现测试环境风险的防护，不宣称失焦已被证明是本次唯一根因。

修改前备份 `.utmp/visual-novel-m3/observation-timeout-before/NarrativeGraphTests.cs`。MCP 已核对实例 ember-unity-framework 与项目根目录，刷新请求成功，但后续 AssetDatabase.Refresh 导入调用返回 success=false、message/data 均空；按 CLAUDE 停止自动验证。**本次修改未完成 Unity 编译验证，也未复测。请在 Unity 中手动触发编译；如果仍有报错，请回传首条编译错误及完整堆栈。** 编译后先运行 `Game.Narrative.Tests.NarrativeObservationPlayTests` 两项，若再超时，提供新 XML 中阶段日志，再定位而非继续延长超时。M3 仍未完成验收/封存，未进入 M4/M5。

## 2026-09-18 Loading 关闭过渡访问已销毁页面

用户反馈测试通过，但 Console 出现页面 `(destroyed)` 的关闭过渡 MissingReferenceException；未收到新的完整 XML，不据此宣称全项目最终验收完成。MCP 精确定位 `ember-unity-framework@b23e9b768e85ed30`，取得堆栈：EUILoadingPage.OnCustomExit 返回后，EUIPage.RunHideAnimationSequence 继续调用 ResolvePresetHandler，在已销毁 GameObject 上 GetComponentInChildren。业务退出动画已有空检查，缺失的是框架两个异步阶段之间的页面存活检查。

本轮修改 `Packages/com.ember/UI/Runtime/EUIPage.cs`：方块打开动画后、Custom Exit 后均检查 CanCompleteTransition，页面销毁或已释放时不继续下一阶段，保留存活页面异常日志和原动画时序。修改 `Packages/com.ember/Tests/EditMode/UIModuleEditTests.cs` 并为 `Packages/com.ember/Tests/Ember.UI.Tests.asmdef` 添加 UniTask 引用，新增 6 项：打开/关闭各覆盖存活、销毁、释放，验证下一阶段及完成回调。未改 Prefab、存档格式或快照。

验证：本轮 Unity MCP 导入和域重载完成，无 C# 编译错误。初次导入中途曾出现尚未导入 UniTask 引用的错误，完整导入完成后消失。第一次 UI 回归 50 项中 2 项失败，原因是新测试 NoUnexpectedReceived 将正常打开/关闭 Log 也视作意外日志；补充明确日志预期后整组复测 **50/50、0 失败、0 跳过**。最终 job `0fed675010f64fe59d6f5a54683b259e`；两轮结果存于 `.utmp/visual-novel-m3/transition-lifetime-tests.json`，修改前源码备份在 `transition-lifetime-before/`。静态差异检查通过。

遗留：需要重走实际读档、返回菜单、Loading 期间退出 Play Mode，确认 Console 不再出现该错误；本轮没有重跑全项目，也没有宣称 M3 整体验收完成。未保存/Bump、未提交发布，未进入 M4/M5。

## 2026-09-18 用户实测通过后的 M3 收口核对

用户明确反馈“我测试没什么问题了，可以收束m3了吗”。本次将常规功能人工验收记为通过，并保留其范围：未把笼统确认扩展为未记录的故障注入、编辑器边界和大档性能全部通过。前一轮关闭过渡修复及 50/50 UI 回归仍是有效证据；TestPage.cs 的 OnUpdateUser Warning 已从完整堆栈确认来自 FrameworkSync_ShouldProtectCustomOnUpdateUserInNonInteractiveGeneration，是测试主动触发的代码保护分支。

本轮核对加载记录仍为 visual-novel 0.3.1 / preview，未切换模板。通过精确实例启动全量 EditMode 回归 419 项，job 71ce7d76d4f04a05a5cf1c16d9a90a3d；最后成功返回完成 238 项、无失败，下一次 get_test_job 返回 TimeoutError。依 CLAUDE 停止自动验证，没有通过 Editor.log、进程轮询或其他编译方式代替。任务可能仍在 Unity 内运行，先检查 Test Runner 并导出完整 XML，再决定是否重跑。

本轮仅更新 README、Design、Implementation 三份文档，备份及完整 git 状态在 `.utmp/visual-novel-m3/closeout/`。文档审计前后均为 198 篇、0 问题，严格检查通过。没有取得本轮项目中心完整保存预览，故未保存/Bump；不能夹带 Assets/Resources 或其他已有改动。正式封存仍待完整回归结果、提交后故障与观察恢复专项证据、完整差异预览。功能实测通过不等于 M3 全部交付步骤已完成；当前停在 M3，不进入 M4～M5。

## 2026-09-18 揭幕后残留读档中状态

用户在常规验收后补充发现：EUILoading 关闭后阅读页仍显示读档中提示，因此此前人工通过不覆盖此新增问题，M3 暂不封存。源码证据：NovelSaveModule 候选提交回调可能在阅读页未就绪时写入“正在打开阅读页面…”，之后未更新；EUINovelReaderPage.Render 在无会话状态时回退显示该 Message。原 NovelSaveUI 仅等待会话 IsReady，未明确等待实际阅读页 IsOpened。

本次修改 `Assets/Game/UI/Runtime/VisualNovel/NovelSaveUI.cs`：准备提交后等待同一候选会话的真实阅读页打开完成，再发布“读档成功”并完成 RunWithLoadingAsync 内的异步任务；Loading 所有者继续等待两次渲染机会后才启动退出动画。复用已有异步完成通知，不另加全局事件或让通用 Loading 依赖存档模块。失败/取消沿用已有收尾路径，提交后失败仍等待菜单就绪。没有改 Prefab、Binding、存档格式或框架 Loading 时序。

扩展 `Assets/Game/Module/Narrative/Tests/NovelGameplayTests.cs` 的真实菜单恢复用例：慢阅读页加载期间保持遮挡，遮挡开始退出时检查阅读页已打开与“读档成功”，退出后检查实际 Status 文本，继续/快读/手动槽均检查成功消息。为真实 TMP 文本断言在 `Game.Narrative.Tests.asmdef` 添加 Unity.TextMeshPro 引用。第一次编译报告测试缺少 TMP 引用，已补上；再次导入成功，但编译状态查询返回 session not ready / ping not answered，按 CLAUDE 停止验证，本轮未确认最终编译、未运行回归。请在 Unity 中手动触发编译；如仍报错，提供首条错误与完整堆栈。编译后先运行 MainMenuContinueLoadsDirectlyAndLoadOpensChooser，再实测揭幕全过程。

修改前两个 C# 文件备份在 `.utmp/visual-novel-m3/ready-before-uncover/`，git diff --check 通过。未保存/Bump、未提交发布，未进入 M4～M5。

## 2026-09-18 揭幕问题人工验收与文档收口

用户在修复后明确确认“没有编译报错”，并在收到继续游戏、快速读档、手动读档三条揭幕检查要求后回复“测试没有问题，收口文档”。据此将最新修复记为人工编译与上述流程验收通过，关闭揭幕后残留读档中提示的问题。此前 MCP 断连和测试引用修正属于历史过程，不再作为此问题的当前阻塞；没有新增自动化 XML，不声称最新增量已有 50/50 或 419/419 自动化结果。

本次仅更新本目录 README、Design、Implementation 与仓库 docs/README.md，核对 NovelSaveUI 实际等待条件及成功提示，统一证据范围与接手状态。未修改代码、Prefab、Binding、模板快照或 hash；没有触发 Unity 编译/测试、保存/Bump、Git 提交或发布。当前加载与封存仍为 visual-novel 0.3.1 / preview，父 base 0.6.4，未进入 M4～M5。

文档维护备份、完整 git 状态及审计报告位于 `.utmp/visual-novel-m3/loading-accepted-docs/`。前后框架模式文档审计均为 198 篇、0 问题，strict 通过。其余编辑器边界、故障专项证据、最终完整回归报告和项目中心差异预览/保存/Bump 仍保留，不因本次针对揭幕问题的人工确认被自动勾选。等待后续指令。


## 2026-09-18 M4 实现、实际 UI 生成与验证交接

用户明确授权 M4，仅完成本阶段，不继续 M5。开始时只读核对编辑记录为 visual-novel 0.3.1、元数据 preview/父 base 0.6.4；Unity 实例精确定位 `ember-unity-framework@b23e9b768e85ed30`，项目根目录正确、非 Play、没有测试运行。保留全部既有未提交改动；备份当前 Assets/Game、GameResource、编辑记录、Audio 与相关公共文档，完整 git 状态/差异在 `.utmp/visual-novel-m4/`。没有切换模板或操作 Unity Farm。

### 实际文件

路径相对于本仓库：

| 文件/目录 | 本轮内容 |
|---|---|
| Assets/Game/Module/Narrative/NovelSession.Reading.cs | 自动/已读快进、配音加载/等待、独立暂停令牌、隐藏与只读历史副本 |
| 同目录 NovelSession.cs、NovelSession.Checkpoint.cs、NovelPresentation.cs | 复用会话与资源所有权、Voice 适配、恢复保持手动且不播放旧配音；INovelAudio 为现有适配的测试替换接口 |
| 同目录 NarrativeDiagnostics.cs、NarrativeModule.cs | 实际模式/Voice/资源/自动计时与独立暂停原因；小说输入意图 |
| Assets/Game/Module/NovelSave/NovelSaveModule.cs、Game.NovelSave.Runtime.asmdef | 接入现有 IsRead/偏好、实时应用音量；增加必要音频程序集引用 |
| Assets/Game/Module/Narrative/Editor/NarrativeContentGUI.cs、NarrativeEditorAvailability.cs | Say 配音键编辑、模块感知的 M4 制作菜单 |
| Assets/Game/UI/Editor/NovelM4UIBuilder.cs | 开发中心创建/绑定/生成、保留原布局、制作前备份；不触碰主菜单 |
| 同目录 NovelGameplayLayoutWindow.cs | Gameplay 布局列表增加自动/快进/历史/隐藏四个现有真实 Binding |
| Assets/Game/UI/Runtime/VisualNovel/EUINovelReaderPage.cs、EUINovelHistoryPage.cs、EUINovelSavePage.cs | 按真实 Binding 接线、历史→存档→覆盖确认的嵌套暂停 |
| Assets/Game/UI/Runtime/SettingScene/EUISettingPage.cs | 仅用户钩子接入五项设置与暂停，未编辑 EmberManaged 块 |
| Assets/Game/UI/GamePages.User.cs、实际生成的三个 Binding.cs | 历史页注册及开发中心生成字段；未手写生成区 |
| Assets/GameResource/Resources/UI/VisualNovel/Prefabs/EUINovelReaderPage.prefab、EUINovelHistoryPage.prefab；UI/Common/Prefabs/EUISettingPanel.prefab | 正式阅读按钮、历史 ScrollRect、设置 Slider；已有位置/颜色/字体/尺寸保留 |
| Assets/GameResource/Resources/VisualNovel/NovelInput.inputactions | A / 左 Ctrl / H / F5 / F9 / 右键；既有 ID/键位保持 |
| Packages/com.ember/Audio/Runtime/EmberAudioManager.cs、EmberAudioPlayback.cs | 独立 Voice 句柄、自然完成通知/音量；无 Mixer BGM 音量修正 |
| Assets/Game/Module/Narrative/Tests/NovelReadingTests.cs、NovelReadingGameplayTests.cs | 13 个新增 M4 用例（含两种阅读模式参数）；复用现有 fixture 和隔离数据 |
| 同目录 NovelSessionTests.cs、NovelGameplayTests.cs、Game.Narrative.Tests.asmdef | fixture 改 partial 与音频测试引用；原断言未删除 |
| 本目录 README/Design/Implementation、docs/README.md、docs/dev/ember-api-reference.md | 当前状态、输入/演出语义、API 与验证边界同步 |

### 实际验证与失败

- **当轮 MCP 自动编译验证未完成；后续用户已确认无编译错误，见末尾人工记录。** 首轮刷新得到 `NovelSaveModule.cs: CS0234 Ember.Audio` 缺少程序集引用，已补 Game.NovelSave.Runtime → Ember.Audio.Runtime。第二轮刷新成功，但首次 editor/state 查询返回 `session not ready / ping not answered`。按 CLAUDE 立即停止自动编译验证；紧接控制台 0 错误不视作最终编译通过。其后的资源制作和预览调用成功，也不作为最终源码编译证明。未执行本轮测试，不引用 M2/M3 的历史通过数量来覆盖 M4。
- **正式 UI 实际生成成功。** Unity MCP 调用 NovelM4UIBuilder.Build，开发中心创建历史页，保存并重新加载三个 Prefab，校验 Binding、调用 TryRegenerateCode。随后读取生成字段再实现逻辑。`bindings/actual-entries.txt` 保存 32 项真实条目，无缺失对象。JsonUtility 的初版快照未输出 Entries，因此补了上述实际条目文本，不能只凭初版 JSON 声称完整绑定报告。
- **静态布局保护通过。** 与本轮备份比较阅读页 186 个、设置页 67 个原有位置/尺寸/锚点/旋转/缩放/颜色/字体字段，差异 0；既有输入 action/binding 的 GUID 和字段不变。初版审计正则误吞 YAML 块而报 missing，修正脚本后得到实际逐块结果，见 `static-audit.json`。
- **资源预览已检查，非运行验收。** `previews/` 保存阅读、历史、设置静态图。阅读按钮与历史面板可读；预览发现新 Slider 的 handle 宽度会被锚点更新归零，已给五个 handle 固定宽度 20，并同步制作器。新增设置面板限定在左侧 30% 区域，原控件不动。设置原有 SafeArea/动画容器在离屏预览中未按运行时展开，原设置面板出现在画面边缘，因此不据此宣称设置页最终运行布局通过，未移动原控件。需进入真实设置页验收并决定是否只调整新增面板位置。
- `git diff --check` 通过，文档 strict 审计 198 篇、0 问题；25 个本轮新增/改动 C# 文件通过词法括号配对检查。原有文件未删除、编辑记录未变、设置页 EmberManaged 块逐字一致。以上均不等于 C# 编译、Play Mode 或声音实测。新增回归尚未运行，旧 419 项报告最终结果仍未知。

### 人工接手与未完成项

**最新 M4 工作副本已获用户确认“没有编译报错”，人工编译项通过；尚未收到本轮运行专项或自动化测试结果。**

1. 运行 `NovelSessionTests`（含 M4 开头的新增 11 个用例）和 `NovelGameplayTests`（新增 2 个 M4 Play Mode 用例），再回归 NovelCheckpointTests 与原读档揭幕用例；导出实际 XML。音频测试覆盖 Voice 自然完成/暂停/主动停止、独立音量及 BGM/两类 SFX，尚无通过证据。
2. 实测 A 自动、左 Ctrl 已读快进及未读/选项停点，右键隐藏后 Space 首次只恢复；历史→存档→覆盖确认逐层关闭，观察暂停令牌和此前模式。测试文字速度/自动间隔与三类音量持久化、坏账号文件失败提示。检查观察窗口 Voice/Resource/Timer 与玩家状态一致。
3. 样例剧情 SO、表数据和实际配音内容未改写。Voice 自动化用例使用隔离表/临时 AudioClip；要人工听测角色配音，可在现有 novel_audio 表加入 Voice 类别资源、按原表中心导出后填入 Say 配音键，或使用已有测试。不能把现有占位 SFX 作为真实配音验收。
4. 实际设置页需检查新增面板与原设置/关闭控件的位置和交互；五个 Slider 的最终修正也需运行验证。Gameplay 布局工具的新增四按钮目标尚待人工拖动/保存验收，仍不涉及启动菜单。
5. M3 编辑器边界、提交后故障/观察恢复专项、419 全量报告及封存事项全部保留。M4 暂不标整体验收完成。未取得本轮项目中心五目录完整保存预览，未保存/Bump；未来封存必须描述 M3＋M4，不能标作纯 M3。未修改 Templates~/ParentSnapshot~/hash、未 Git 提交、未发布 tag、未安装 Fungus，PlayerDataModule 仍 Enabled=false。

本轮停在 M4 实现和验收交接，不进入 M5；后续先处理人工验证反馈，推进新阶段需用户“继续下一阶段”。


## 2026-09-18 M4 人工编译确认

用户在 M4 实现交接后明确反馈“没有编译报错”，据此将最新 M4 工作副本记为人工编译通过；不标为 MCP 自动验证通过。未收到新增测试 XML 或运行专项反馈，13 项新增用例、自动/快进、嵌套暂停、历史/隐藏、Voice 与 BGM/SFX 回归仍待验证。M3 的 419 项最终报告、专项证据、编辑器边界验收及保存封存待办保持原状。

本次仅同步本目录 README、Design、Implementation 与仓库 docs/README.md 的当前状态，保留历史失败记录；未修改源码或 UI 资产，未触发 Unity 编译/测试，未保存/Bump、提交、发布或进入 M5。


## 2026-09-18 用户 432 项报告：MCP 连接错误与域重载失败

用户提供 `TestResults_20260918_174748.xml`，本机备份 `.utmp/visual-novel-m4/user-tests-20260918-174748/results.xml`：432 项、428 通过、4 失败、0 跳过。M4 新增 11 项 NovelSessionTests 全部通过，2 项 NovelGameplayTests 未通过。此为用户提供的自动化报告，与此前人工编译确认分开记录。

- `M4RealHistorySaveNestingAndHiddenDialogueKeepPresentation`：`Errors occurred during domain reload.`，无失败堆栈；报告不足以认定具体原因。
- `M4VoiceHandleCompletionPauseVolumeAndBgmSfxRegression`、`MainMenuContinueLoadsDirectlyAndLoadOpensChooser`、`RealMenuReaderBranchEndingAndRepeatedExit`：均被未预期的 MCP 插件 WebSocket 连接错误判失败，堆栈指向 `WebSocketTransportClient.EstablishConnectionAsync`。没有业务断言失败证据，但不能据此将用例算作通过。

本轮首次读取 MCP instances 即因当前客户端地址 `http://127.0.0.1:8080/mcp` 连接失败，按 CLAUDE 停止自动验证；未读取 Editor.log、启动替代编译或重试轮询。未修改业务源码、测试断言、插件或 UI 资源，仅归档报告和同步文档。当前无需根据连接错误猜测修改业务逻辑。

接手：在 Unity 的 MCP 插件面板核对服务、URL 和连接状态，消除连接错误后，重跑 `Game.Narrative.Tests.NovelGameplayTests` 并导出 XML；如域重载错误仍发生，提供对应 Console 首条错误及完整堆栈。不添加忽略错误或预期连接失败日志来使测试变绿。M4 运行验收、既有 M3 专项/编辑器边界和保存封存继续待办；未保存/Bump，未提交、发布或进入 M5。


## 2026-09-18 用户 95 项报告与音频测试域重载修正

`TestResults_20260918_175806.xml`（备份 `.utmp/visual-novel-m4/user-tests-20260918-175806/results.xml`）为 95 项、94 通过、1 失败。唯一失败 `M4VoiceHandleCompletionPauseVolumeAndBgmSfxRegression` 是 NullReferenceException，堆栈落在 NovelReadingGameplayTests.cs:58 的捕获局部变量初始化。上一报告的连接日志失败不再出现；历史/存档嵌套及两个既有 Gameplay 流程在本报告通过。此为 95 项子集，不替代全项目完整回归。

修正 `Assets/Game/Module/Narrative/Tests/NovelReadingGameplayTests.cs`：将音频局部变量与断言移入 `VerifyVoiceHandlePlayback` 子协程，在 EnterPlayMode 域重载完成后新建，避免捕获状态跨域重载丢失。保留全部 Voice/BGM/SFX 断言与释放逻辑；未修改音频运行时代码或正式 UI。原文件已在上述证据目录备份。

本轮 MCP 精确选择 ember-unity-framework，刷新后观察到 compiling，再确认域重载结束、idle、Console error=0，Unity 编译通过。随后启动 NovelGameplayTests 整组回归，job `b7f730f07e8f4ee99315580be7565eff`；首次结果查询报插件 session disconnected while awaiting command_result，按 CLAUDE 停止自动验证。修正后的测试最终结果未知，不能算作通过；请从 Unity Test Runner 导出该组 XML，确认是否仍运行后再决定重跑。

仅修改上述测试与状态文档，未修改模板快照、保存/Bump、提交、发布或进入 M5。M4 剩余验收与 M3 遗留项仍保留。


## 2026-09-18 音频测试修正后人工确认

用户在上述协程修正后反馈“这次不报错了”，记录为修正后人工确认不再报错，该空引用问题按此反馈关闭。未附新的 XML，也未明确测试范围，不将该反馈换算为 95/95、432/432 或整组自动化通过；此前 MCP 编译通过与结果查询断连记录保留。M4 其他运行验收、M3 遗留证据与保存封存待办不变。

本轮仅同步四份状态文档，未修改代码或资产，未触发编译/测试，未保存/Bump、提交、发布或进入 M5。


## 2026-09-20 用户全量 431/432：观察窗口超时仍待定位

用户 `TestResults_20260920_104442.xml` 为 432 项、431 通过、1 失败、0 跳过；备份 `.utmp/visual-novel-m4/user-tests-20260920-104442/results.xml`。13 项 M4 用例全部通过，包含之前修正的音频测试。唯一失败 `NarrativeObservationPlayTests.PlayModeWindowReopenFollowPauseReplacementAndExitDoNotWriteAssets` 超过 180000 ms，无堆栈，仅有 entering Play Mode 与 checking window lifecycle 日志；旧观察窗口超时问题尚未关闭。本次完整报告替代“最新全量结果未知”的当前状态，但不改写历史 419 项任务结果未知的事实。

只修改 `Assets/Game/Module/Narrative/Tests/NarrativeGraphTests.cs`：在 CheckPlayingWindow 内增加章节加载、资产快照、运行器启动、窗口创建/显示/绑定、帧等待恢复、重开、会话替换、结束/故障与清理的 TestContext 日志。保留断言、帧等待和原超时，不将诊断增强声称为根因修复。原文件同目录备份。

首次 MCP instances 查询握手失败（当前客户端地址 http://127.0.0.1:8080/mcp），按 CLAUDE 停止自动验证。本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请提供首条编译错误及完整堆栈。然后仅运行该失败用例并导出 XML，利用最后一条 Observation detail 定位停顿，不需先重跑全量。M4 人工界面/偏好等验收与 M3 其他待办、保存封存仍保留，未保存/Bump、提交、发布或进入 M5。

## 2026-09-20 M4 功能验收通过

用户在单独复测观察窗口用例及手动验证后确认“通过了，手动验证也没问题”。据当前对话验收范围，将观察窗口超时问题按用户复测反馈关闭，并将 M4 阅读辅助与配音功能验收标为通过。手动确认覆盖此前列出的自动/快进、嵌套暂停、历史/隐藏、Voice、偏好、恢复及运行观察检查。

证据分开记录：`TestResults_20260920_104442.xml` 为全量 431/432，13 项 M4 用例全部通过；唯一观察窗口失败项之后由用户确认复测通过，未提供新 XML，不声称取得 432/432 的新全量报告。本轮没有运行编译或测试，仅同步本目录三份文档与 docs/README.md，静态差异检查通过。

M3 其他专项证据、编辑器边界与保存封存待办保持独立；未保存/Bump、修改模板快照、提交 Git 或发布。当前仍为 visual-novel 0.3.1 / preview、父 base 0.6.4。包含本轮内容的后续快照须标明 M3＋M4 并先完整预览五个受管目录。停在 M4，等待用户“继续下一阶段”，M5 未开始。


## 2026-09-20 新游戏揭幕增量修复

用户反馈新游戏进入 Gameplay 仍有一帧穿帮。核对新游戏按钮原来直接切换状态，场景 Loading 不等待异步阅读页及首屏剧情资源；已有读档使用 RunWithLoadingAsync 等待目标内容。

修改 NovelSaveUI.cs 新游戏入口复用 RunWithLoadingAsync；EUIMainPage.cs 仅用户区改接 StartNewGame；GameGameplayState.cs 标记遮挡下首屏准备；NarrativeModule.cs 在此阶段允许会话处理首屏资源/演出，遮挡期间统一屏蔽玩家输入，准备结束后保持暂停至揭幕结束。等待当前会话首个对白/选项/结局、无资源/演出/转场等待、对应阅读页 IsOpened，再交回现有 Loading 渲染帧与退出流程。失败回菜单后才揭幕。未新增全局事件、未修改布局/Binding/EmberManaged 或通用音频 API。

扩展 NovelGameplayTests.cs 的 MainMenuContinueLoadsDirectlyAndLoadOpensChooser：延迟新游戏阅读页加载 30 帧，检查遮挡持续保持，揭幕前页面完成打开、会话到首屏阅读点且背景 Item 已有 sprite；继续回归继续/快读/手动读档及取消/坏档。首轮失败为新增测试误在 Background 容器读取 Image（用户 TestResults_20260920_142002.xml 同批），已改查实际子 Item；第二轮首屏检查通过，后续快存因测试未等待首个自动存档完成而不满足 Latest=6，已明确等待自动槽提交后再测快存。业务存档规则未修改。

本轮精确定位 ember-unity-framework，经 MCP 刷新、域重载与 Console error=0 确认编译。测试结果见后续补记。源码备份 `.utmp/visual-novel-m4/new-game-cover-before/`，报告 `.utmp/visual-novel-m4/new-game-cover/`。此前 M4 人工验收记录保留，本次增量揭幕效果仍需实际界面确认；未保存/Bump、提交、发布或进入 M5。

最终专项结果：原生 `tests-20260920-062508669.xml`（备份 `new-game-cover/final-passed.xml`）**1/1 通过、0 失败、0 跳过**，22.54 秒，覆盖上述新游戏、继续/快读/手动读档及失败/取消路径。MCP job bd9376448f8b4c8ca6c8fbaaf6002e59 状态在域重载后仍显示 running，但 Editor 明确 tests.is_running=false；依据现有 Test Runner 回调生成的原生 XML 确認通过，不用过时 job 状态代替结果。`git diff --check` 与 strict 文档审计（198 篇、0 问题）通过。尚未获得本轮人工视觉反馈，不将旧 M4 手动验收自动延伸至本增量。


## 2026-09-20 M4 阶段收束

按用户“能否收束 M4”的请求，仅整理 README、Design、Implementation 和 docs/README.md 的当前结论与交接边界。M4 自动/仅已读快进、历史/隐藏、嵌套暂停、Voice 与偏好、快进演出策略及运行观察保持已验收通过；不继续 M5。

证据：全量报告 431/432，其中 M4 13 项通过；观察窗口专项后续通过为用户反馈。新游戏揭幕增量编译及专项 1/1 通过，人工画面反馈未收到。音频测试闭包修正、新增背景测试定位错误、快存测试时序修正的历史失败均保留。关于快存的修正只调整测试前置条件：先等自动槽完成，再测试快存 Latest=6；没有修改业务写入顺序，也不声称所有并发时序问题已被排除。

遗留：新游戏揭幕增量实际画面确认；M3 原有专项/编辑器边界待办；保存封存前完整预览五个受管目录，排除无关内容，经项目中心保存/Bump，快照必须包含 M3＋M4 的真实范围。上述事项未擅自勾选完成。模板仍为 0.3.1 / preview、父 base 0.6.4，未发布。本轮未修改代码或资产，未触发 Unity 编译/测试，未提交 Git。备份与状态记录位于 `.utmp/visual-novel-m4/closeout-20260920/`。


## 2026-09-20 M3＋M4 保存封存（0.4.0 / preview）

用户明确要求“帮我封存”。通过项目中心同一 CompareEditingTemplate 完整预览 Game、Resources、Ember/Editor、Settings、GameResource 五个受管目录，85 项差异全部属于 M3＋M4 范围。无未保存场景；保存遵循现有归一化与排除规则，没有新增夹带 DevCenterSmoke、其他模板或调试配置。更新当前文档后再次完整预览，使用 EmberProjectSetup.SaveTemplate 与 BumpTemplateVersion 的 Minor 操作，版本从 0.3.1 到 0.4.0，频道 preview、父基线 base 0.6.4 保持。

范围包含存档/五按钮菜单/读档 Loading、Gameplay 布局和节点预览、阅读辅助/配音/偏好/观察，以及新游戏首屏 Loading 就绪等待修复；不是纯 M3 快照。源码和资源保持工作副本内容，未重新生成 UI 或重设用户布局。

证据边界不因封存扩大：13 项 M4 已有全量 XML 通过证据；全量报告 431/432 的观察窗口唯一失败项后由用户确认通过，没有新的 432/432 XML；最新新游戏及读档专项 1/1 通过。新游戏增量人工画面确认、M3 独立专项与编辑器边界记录仍按原清单保留，不以保存代替验收。

模板依赖当前框架工作副本中既有音频句柄、Loading 与 UI 生命周期修复；模板封存只管理五个业务目录，不代表这些框架改动已发布，消费端交付需同批框架支持。未改父快照或手工填写 hash，未提交 Git、发布 tag 或进入 M5。备份、完整预览与封存后校验位于本机 `.utmp/visual-novel-m3-m4-seal/`。

## 2026-09-20 M5 首批实施（工作副本，未验收封存）

用户明确授权 M5。先只读确认编辑模板 visual-novel 0.4.0 / preview、父 base 0.6.4，编辑记录、contentHash 与 versionedContentHash 均为 `9387f8affe4f0ad9370e6e09a50b653c`；保存前的旧 Git 改动全部保留。未切换模板、提交或发布。初始 git status 记录 `.utmp/visual-novel-m5/status-before.txt`。

本批实际落盘：

- `NarrativeContentGUI`：多行台词、角色和资源分类下拉；Say 仅选择 Voice，未知键保持可修复；只在用户编辑时写回文本。
- `NarrativeTableCatalog`：公开已有只读行列表供 Editor 查询，没有另建表系统或修改生成 bytes。
- `NarrativeAssetValidation`：在现有全剧情/章节定义校验后检查实际 Sprite/AudioClip；图窗口校验接入此层。资源提供器的 Player 行为仍须运行验证。
- `NarrativeGraphWindow`：跨章节错误定位、节点/指令展开，无会话提示；默认打开新完整示例。复制、排序、连接与订阅生命周期沿用既有实现，其本批回归未执行。
- `NovelNewGameRequest`、`NarrativeRunner`、`NovelSession`、`NovelSaveUI` 与 `NarrativeEntryLauncher`：显式章节/节点新游戏，使用原 Loading 流程；非法入口报告 BadEntry。仅从独立制作菜单、主菜单无活动会话时启动，不在观察窗口增加运行跳转。定义入口和存档指纹保持不变；新会话默认变量，不补跑先前副作用，账号已读/自动槽仍按正常规则使用。
- `Assets/GameResource/Resources/VisualNovel/LastLight/`：10 个新 SO，79 条对白、88 条指令，两分支/两结局；主菜单新游戏默认读取它。原 M1Sample 逐文件与封存快照一致。新资产通过 Unity YAML 静态编写，导入及运行尚未验收；资源和 GUID 引用已做磁盘静态检查。
- `Authoring.md`：编写、扩展、试播语义、默认示例、资源和交付验收。未修改正式 Prefab、Binding 或用户布局；未修改通用音频 API，PlayerDataModule 仍 Enabled=false。

新增待运行测试共 8 项：指定入口跳过旧赋值且可捕获/恢复 1 项、非法章节/节点 2 项、完整示例两结局 2 项、跨章错误定位无资产写入 1 项、真实导表与资源校验 1 项、空 Voice 与类别错误 1 项。既有菜单开关测试加入新入口，Gameplay 两路线测试资产快照改为 LastLight。没有新 XML，不声称这些测试通过。

Unity 验证经过：精确锁定 `ember-unity-framework@b23e9b768e85ed30`，初始 Editor 空闲。首次 scripts 刷新后出现新校验类尚未导入的 CS0103；随后 all 刷新触发资产导入。下一次 read_console 返回 `ping not answered`。同一工具批次里紧接着的 execute_code 返回 scriptCompilationFailed=false，但该调用不应被用作失败后的验证补偿；此后停止全部自动 Unity 验证。后续入口、示例和测试变更均未完成编译验证，不把中间状态称作最终编译通过。没有使用 Editor.log、BatchMode、dotnet 或轮询进程替代。

静态证据：`.utmp/visual-novel-m5/static-results.json` 检查 10 个 SO、稳定指令/台词 ID 唯一、节点登记及引用、两条分支到两个结局、7 个复用资源文件存在、原 M1Sample 与封存 metadata 未改。两条路线为 3,275 / 3,248 字，约 11 分钟是阅读速度估算；未实测时长，也未验证 Sprite 导入类型或画面。完整的类型/资源运行检查仍待 Unity 测试。

剩余事项：最终编译和上述测试；复制/排序/连接、观察生命周期、缩放/长句、缺资源/空配音/反复进出及新游戏实际揭幕验收；有配音示例；五目录项目中心正式差异预览、保存/Bump、父基线/内容/编辑记录一致性检查。M3 原遗留与历史 431/432、观察专项人工通过、揭幕专项 1/1 的边界不变。

消费端安排已澄清：用户将在模板完成后自行部署到 `C:\Users\wuyu\My\Call Me Heartless` 并导入自己的小说内容。当前不操作该项目；不再要求另指定新项目目录，不在 Templates~ 内嵌工程，也不使用 Unity Farm。消费端部署与运行结果待用户后续执行/确认，不能勾选已完成。

当前 Unity MCP 在本轮验证中不可用，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。M5 尚未完成，模板封存仍为 0.4.0 / preview。

末次静态审计：strict 文档审计 199 篇、0 问题；git diff --check 无差异错误。五个受管目录相对 0.4.0 的原始磁盘差异仅见本批小说源码/测试/文档与 LastLight 新资产，无删除、无正式 Prefab/Binding 变化；清单位于 `.utmp/visual-novel-m5/disk-delta.json`，这不是项目中心的归一化预览，不能替代正式保存前检查。新入口脚本与 Authoring 文档仍需 Unity 导入生成相应 .meta，再执行后续验证与封存。


## 2026-09-20 SafeArea 修复准备（尚未应用 Prefab）

用户要求修复异形屏交互区域。只读核对发现阅读页 Dialogue/Choices/菜单工具、历史 HistoryPanel、存档 Panel、设置 NovelPreferences 位于页面根节点，未受既有 EUISafeArea 约束；背景与立绘应继续全屏。

新增 `NovelSafeAreaRepair`，通过 PrefabUtility 加载/保存/卸载，把上述容器迁入 Animator/EUISafeArea，保留 RectTransform 参数和 Binding 对象引用；阅读页 Animator 调整至背景/立绘之后，避免遮挡。使用开发中心 Binding 校验与生成接口。M2/M3/M4 制作工具接入归一化，迁移后的路径查找兼容原位置，避免重建同名控件。工具受 visual-novel 编辑身份、Narrative 开关和 Play Mode 限制。

实际执行状态：精确锁定 ember-unity-framework，初始空闲；刷新请求成功，Console 未返回错误，但执行新工具时出现 TypeLoadException，未能加载 NovelSafeAreaRepair。因此不能认定编译通过，Prefab 迁移未执行、Binding 未重新生成。遵照 CLAUDE.md 停止自动验证，没有替代编译方式。原 Prefab 与本轮备份逐字节一致。证据：`.utmp/visual-novel-m5/safe-area-before/`、`safe-area-status.json`。

待 Unity 手动编译后执行 `Ember/Visual Novel/修复 UI 安全区`，再检查四页 Binding、重复执行零迁移、全屏背景绘制顺序，以及 Device Simulator/真机的横竖屏安全边距。Gameplay 布局工具通过 Binding 对象定位而非固定路径；其迁移后预览及保存仍待回归。本条不是适配验收或模板封存。


## 2026-09-20 SafeArea 复核与临时工具清理

用户确认已执行修复，并明确要求核对及删除临时工具。本轮 Unity MCP 精确连接 ember-unity-framework，逐个 LoadPrefabContents 检查阅读、历史、存档、设置四个正式 Prefab：EUISafeArea 存在且启用，所有非背景/立绘/隐藏模板的 Binding 对象均位于安全区内，开发中心 ValidateBinding 均无错误；阅读页 Animator 为根节点最后一个兄弟，交互层位于全屏背景与立绘之上。实际 Reader.Binding 已包含 Animator/EUISafeArea/Dialogue/Speaker 等新路径。确认上条“尚未应用 Prefab”已由用户执行完成；本轮未重排或重写这些 Prefab。

已移除七个一次性脚本及对应 meta：NovelM2UIBuilder、NovelM3UIBuilder、NovelM4UIBuilder、NovelSafeAreaRepair、NarrativeM1Assets、NarrativeStorySample、NarrativeObservationSample。同步删除动态菜单注册与对已删除创建器的测试调用，并在本次 Unity 会话清理残留动态菜单。旧历史记录保留作证据，不再代表当前可用入口；README 的操作步骤已改为正式 Gameplay 会话。保留原样例 SO/音画资源、正式运行观察、入口试播、剧情资产创建与 Gameplay 布局编辑器。备份位于 `.utmp/visual-novel-m5/safe-area-cleanup/removed/`，不进入 Assets。

验证：清理后 MCP 刷新编译成功，Console 错误 0，Editor 非编译/导入状态且 scriptCompilationFailed=false，已加载程序集不再包含临时修复/装配类型。NarrativeAvailabilityTests 三项全部通过（菜单开关、禁用时拒绝打开和写资产、恢复窗口禁用关闭），job `192aeba6da3c479f9efe10e124eb452b`。SafeArea 资源检查记录 `safe-area-cleanup/verification.txt`。没有新全量测试报告；实际异形手机、横竖屏视觉适配仍待验收。未保存/Bump 模板、修改父快照或提交 Git。


## 2026-09-20 阅读 UI 新布局提案（等待用户确认）

根据四张参考截图形成 [提案 A](ReadingUILayoutProposal.md) 与概念预览。左上字号/历史/隐藏，右上自动/倍率，右下菜单容纳存读档等既有功能。第二个按钮暂按历史理解；倍率建议只作用于文字速度及自动间隔，保留等待配音与仅已读快进约束。用户要求预览同意后实施，因此本轮仅写文档，未修改 Prefab、Binding、运行代码或模板封存状态。图中人物场景不是正式替换资产。


## 2026-09-20 已确认视觉规范入档与 EUI 阅读界面首批实施

用户确认提案 A，并要求将预览图归入文档作为实例/规范，项目所有 UI（包括字号调整弹窗）必须经 EUI 系统制作。图片已复制到 `Images/reading-ui-approved-a.png`，正文嵌入 [阅读布局规范](ReadingUILayoutProposal.md)，README/Design 状态同步为已确认。图中“待确认”是出图时历史文字；图中的人物/背景只作示意，不替换正式美术。

实际通过开发中心 EUICreationService 预检并创建 EUINovelFontPage、EUINovelReadingMenuPage，两者均为 Business EUI Popup，Prefab 保留 GUID 移到既有 VisualNovel/Prefabs，使用 EUIBindingEditorUtility 对应的真实 Binding、TryRegenerateCode 生成，再读取 Binding 写用户逻辑。阅读页新增 ReadingControls、Speed、RestoreUI、ReadingShading 真实绑定；顶部分组、底部对白、右下菜单及 EUIGradient 装饰渐变已落盘。文字/交互留在 SafeArea；渐变与背景全屏。旧按钮绑定保留兼容引用，但存读档三个入口在阅读页隐藏，改由 EUI 阅读菜单进入。没有恢复临时工具菜单，一次性资源制作文本与备份只在 `.utmp/visual-novel-m5/`。

业务首批：字号小中大与倍率偏好写入既有账号存储队列，保留基础文字速度/音量；倍率作用于逐字速度和自动停留时间，不使用 Time.timeScale、不改变音频播放速度；自动仍等待 Voice。隐藏时关闭交互/对白/选项及装饰渐变，由 EUI 页内透明恢复按钮接收点击；恢复同帧 Advance/Choose 不穿透。字号/菜单持有独立暂停租约，菜单跳转通过 EUIManager 关闭并等页面退出后执行，历史/存档/系统设置继续复用既有 EUI。

验证证据严格分层：第一次资源脚本执行因 EUIBindingRole 命名空间错误未执行，修正后创建成功；渐变脚本末尾多余括号同样在执行前被拒绝，修正后执行成功。项目首次编译报告新菜单缺少 Ember.Core using，补齐后 Console 错误 0。NovelSessionTests 27/27 通过（含新增 1X/2X/3X 自动间隔与文字速度 3 项、隐藏恢复同帧输入 1 项），job `55c07afcf8bb4ef3b694a634576dce2a`。

随后补充渐变与真实页面回归，刷新后 Console 错误 0，启动 `M4RealHistorySaveNestingAndHiddenDialogueKeepPresentation`（扩展字号、倍率、菜单转存档及截图），job `ff4f54f9a1024ca3a2d3949aabae287f`；查询结果时 Unity plugin disconnected，已停止全部自动验证，没有重连、Editor.log、BatchMode 或 dotnet 补验。该真实页面专项结果未知，未取得截图，不声称实际画面或弹窗生命周期通过。Console 中间零错误也不替代最终验收。

仍需：本批最终 Unity 编译/真实 EUI 专项结果、长句大字号与窄屏布局、实际图标/字体可读性、倍率有配音等待、菜单系统设置与返回路径、偏好跨启动持久化。Gameplay 布局窗口对新速度按钮的专属编辑项尚未扩展（可直接用开发中心编辑真实 Prefab）；长句的额外分页交互尚未实现，沿用 TMP 换行。模板未 Save/Bump，未部署消费项目，未提交 Git。

本次未完成最终 Unity 编译与真实页面验收。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。


## 2026-09-20 阅读页注册缺失修复

用户报告四处 CS0117。阅读 Prefab、Binding 与逻辑仍在，但 GamePages.User.cs 缺少 EUINovelReaderPage；已补回 Normal/MainPage 定义，使用实际 UI/VisualNovel 路径。生成器注册原来按默认目录推算为 UI/Module/VisualNovel，迁移后再生成会写回错误路径；本轮改为已保存 Prefab 优先使用 AssetDatabase 实际路径，未保存骨架保留默认推算。这是框架编辑器与模板注册修复，尚未发布。

静态核对 11 个实际页面注册无重复、对应 Prefab 均存在。MCP 清理上轮残留测试任务返回成功，但刷新仍返回 tests_running；按 CLAUDE.md 停止自动验证，未用其他渠道替代。本次未完成 Unity 编译验证，也未运行迁移后重新生成回归。请在 Test Runner 停止残留测试后手动触发编译；仍有错误时发送首条错误及完整堆栈。未保存/Bump，上轮实际 UI 验收仍未完成。


## 2026-09-20 EUIBinding 中文用途补齐与 Test Runner 尝试

通过 PrefabUtility 与 SerializedObject 补齐 9 个小说 Prefab 的 EUIBinding.uiDescription：阅读、字号、阅读菜单、历史、存档、存档槽、选项、背景、立绘。保留已有 6 个公共页面中文说明；静态检查当前 UI 目录 15 个根 EUIBinding 全部有说明。仅修改用途说明，不改 Binding 字段、布局、生成代码或页面注册。证据与原文件备份位于 `.utmp/visual-novel-m5/eui-descriptions/`。规范新增 Page/Item 均须填写中文“UI 用途”的要求。

Test Runner 已实际启动 31 项筛选（NovelSessionTests、NarrativeAvailabilityTests、M4 真实页面专项），job `f259764d0d3948e88c622593631747d0`。两次有界结果查询均为运行中 0/31；后续 Unity 状态显示已空闲、tests.is_running=false，控制台无错误、仅 PerformanceTesting 预构建提示，服务端结果仍未完成。清理残留 job 后分离启动纯 EditMode 用例，调用在等待结果时发生 plugin disconnected，因此按 CLAUDE.md 停止自动验证。未收到新测试结果，不能复用上轮 27/27 作为本轮通过。

本次未完成 Unity 编译验证与 Test Runner 结果确认。请在 Unity 中手动触发编译；如果仍有报错，请发送首条错误及完整堆栈。中文用途已经保存；未保存/Bump 模板。

## 2026-09-20 示例素材拆分与 19:44 测试报告修复

用户提供 `TestResults_20260920_194445.xml`：本次执行 107 项，105 通过、2 失败；程序集发现数量 444 不代表执行了 444 项。原报告保留在 `.utmp/visual-novel-m5/user-tests-194445/results.xml`。

从已批准概念图重新生成并补全独立黄昏天台背景、透明林晚立绘，保存在 `Resources/VisualNovel/LastLight/Presentation`。这不是原始分层提取。Unity TextureImporter 已将两图导入 Sprite Single，关闭 mipmap，FullRect，PPU 100，最大尺寸 2048；MCP 实际加载 Sprite 成功。阅读布局规范文档包含图片及引用方式；Gameplay 布局窗口增加“使用规范示例：黄昏天台 / 林晚”，亦可作为节点预览起始上下文。仅编辑器预览使用，不自动修改剧情 SO、资源表、运行时示例、Prefab 或生成 bytes。

两处原始失败的前置条件已修正：历史/存档嵌套测试必须等待新游戏揭幕完成；迟到阅读页测试必须在加载未完成时保持遮罩，通过异步返回菜单取消，再等待会话释放后交付迟到回调。后者原来先等待揭幕再完成页面，违背当前首屏遮挡约定。重复新游戏改为等待实际菜单打开、存档空闲及按钮可交互；固定低文字速度以明确验证首次点击补全文字，并增加等待失败时的会话/暂停诊断。

复测发现真实缺陷：新游戏失败/取消后主菜单先于 Loading 结束打开，按钮被忙碌状态禁用，但 `StartCovered` 的 finally 清除 `_loading` 后没有通知刷新。现补发既有 NovelSaveModule 消息通知、保留当前提示，和既有读档 finally 行为一致；没有新增全局事件或改变存档写入顺序。

中间验证：首次新增测试辅助属性直接引用 UI 程序集导致 CS0234，改为通过已加载程序集反射后 MCP 编译错误为 0。首次重跑 2 项为 1 通过、1 失败，历史/字号/倍率/菜单/隐藏专项通过并产生实际截图；后续取消断言与菜单前置条件的失败均有独立 XML，不能视为通过。服务端测试 job 在域重载后仍显示 running，而 MCP Editor 状态已 idle，Console 明确报告测试 XML 保存路径；结果以该实际 XML 为准，未使用 Editor.log 或其他编译替代方式。

最终专项：MCP 刷新后 Console 编译错误 0；3 项合并回归全部通过，原始 XML 为 `.utmp/visual-novel-m3/tests-20260920-120421794.xml`，归档为 `.utmp/visual-novel-m5/user-tests-194445/final-passed.xml`。覆盖原报告两项失败及 MainMenuContinueLoadsDirectlyAndLoadOpensChooser。未重跑全部 107 项，不宣称全量通过。已清理 MCP 服务端残留 job 标记。

素材实测：Unity 独立 PreviewRenderUtility 输出验证背景与透明立绘合成。发现静态预览误读 Game View 的 SafeArea 坐标，现只在预览克隆中禁用 EUISafeArea 并使用无刘海全父级区域；再次 MCP 编译错误 0，重新渲染图已核对并收入 Images/reading-ui-sample-preview.png。此最后增量仅涉及编辑器预览，通过实际渲染验证，未重复业务三项测试；正式 Prefab、运行时安全区和用户布局未改。长句、各异形屏与人工视觉验收仍独立待办。

本轮未保存/Bump、未改 ParentSnapshot/hash、未部署消费项目、未提交 Git。当前仍为 visual-novel 0.4.0 编辑工作副本，新增示例和修复尚未封存。

## 2026-09-21 参考风格落地与三项失败回归

阅读页、字号、历史、阅读菜单已使用正式 EUI Prefab；顶部采用透明图标，底部渐变/金色分隔，菜单补齐六项图标。采用 Noto Serif SC 动态 TMP 字体，OFL 许可证随字体保存。生成代码通过 TryRegenerateCode 更新并读取核对，未手写 Binding。单次制作脚本与备份留在 .utmp，不新增 Assets 临时工具。

LastLight 的 CH01_intro、CH01_common 演出引用新增 lastlight_rooftop、lastlight_alice，采用中心单立绘，另一角色保留对白、画外发言。原角色 ID、台词和分支未改，素材名称“林晚”不等于剧情角色重命名。此前“仅预览、未改资源表”记录属于历史批次，当前示例已接入实际游戏。

用户 21:27 XML 实际包含单项诊断重跑（1 失败）；此前三项失败批次仍保留。诊断明确为 MissingResourceKey: lastlight_rooftop，导致新游戏失败、Session 为空。CSV 已增加记录，但烘焙时 Unity 缓存源资产未同步。本次先 ForceUpdate 导入两个 CSV，再经 EmberTablePipeline.BakeCurrent 烘焙并导入 bytes；两次均成功、无诊断。用全新 EmberTableEngine 加载实际 Resources 产物，背景和立绘键均解析成功。没有直接修改 bytes，没有放宽运行时校验或再延长测试等待。

MCP 刷新/编译后 Console 错误 0。三项专项全部通过：M4RealHistorySaveNestingAndHiddenDialogueKeepPresentation、MainMenuContinueLoadsDirectlyAndLoadOpensChooser、RealMenuReaderBranchEndingAndRepeatedExit。原 XML：.utmp/visual-novel-m3/tests-20260921-021207805.xml；归档 .utmp/visual-novel-m5/skin/final-passed.xml（3 执行 / 3 通过 / 0 失败）。测试服务端状态在域重载后滞留 running，依据 MCP Console 明确保存的 XML 读取结果；Editor 空闲后清理残留 job。没有使用 Editor.log 或替代编译途径。

实际截图收入 Images/reading-ui-runtime-{reader,menu,font}.png；静态布局截图 reading-ui-skin-layout.png。已目视检查背景、透明立绘、字号选中态及菜单六个图标。运行截图保留调试 FPS 浮层，右上倍率部分被其遮挡；不作为最终无调试画面或手机适配证据。全量、观察窗口/编辑器边界、超宽/窄屏长句大字号、真机安全区及人工视觉确认仍独立待办。未保存/Bump、未改父快照或 hash、未部署消费项目、未提交 Git。

## 2026-09-21 布局编辑器菜单定位与流程图可读性

修复 Gameplay 布局窗口仍选择阅读页旧 Saves/QuickSave/QuickLoad 隐藏控件的问题：现在从 EUINovelReadingMenuPage 的真实 Binding 取六个菜单按钮，新增系统设置、仅已读快进和返回主菜单编辑项。选中菜单项自动展开菜单预览，选中阅读页元素收起；定位正式 Prefab 按当前元素所属页面导航。两个正式 Prefab 同时载入，保存分别写回并备份，外部依赖 hash 和 Prefab Mode 冲突保护覆盖两页。预览克隆将菜单合入阅读画布以确保渲染，正式 EUI Popup 层级不变。

LastLight 当前章节已按现有自动布局排为开场→玩家选择→两条对话路线→汇合→条件分支→两个结局；坐标保存在编辑器 GraphLayout，未改 SO 连接、稳定 ID 或台词。节点标题显示开始/对话段/玩家选择/条件分支/结局类型；无布局资产时默认按真实连接分层，不再按资产列表折行。既有自定义布局仍保留，仅本次指定示例重新排布；打开的流程图已重新框选全部。

验证：MCP 编译检查无错误；六个菜单目标均断言属于实际菜单 Prefab；独立预览渲染并目视确认菜单显示，截图 .utmp/visual-novel-m5/editor-menu-layout/quick-save-preview.png。图布局备份同目录 graph-before.asset（原布局存在时）；本次未重跑业务 Gameplay 三项测试，前批 3/3 不是本次编辑器改动的测试报告。未封存、未部署、未提交。

## 2026-09-21 完整天台短篇与可仿写示例

按用户要求改写 LastLight 的四个 Dialogue（25+20+20+14=79 句），保持全部节点、选项、路线、结局 ID 与连接。单路线 59 句，约 2908/2937 字（含标点），按 300 字/分钟估算约 10 分钟，不声称实测。角色独立新增 lastlight_wan/lastlight_zhou/lastlight_inner；旧 M1 角色不动。旧行 ID 保留，textRevision 递增，四段及选择 contentRevision 递增；旧内容存档可能不兼容，不删档、不绕过检查。

所有段落显式准备背景/BGM/立绘。intro 展示居中、旁白、内心、双人、空镜 Wait、回场及选项；两条路线赋值 Chapter.heard；common 展示居中→双人→空镜，再进入既有双结局分流。内心是 Say 的角色/文本约定，没有新增 DSL、事件、Module 或运行态写入 SO。新增男立绘使用内置 image_gen，透明 RGBA；正式阅读 Prefab 左右槽位按两张新立绘调整，保留用户按钮及对白布局，备份 reader-before.prefab；EUI 生成 Binding 后读取核对。

两句 Voice 采用本机 Huihui 中文 TTS：5.466 秒与 8.077 秒。首次沙箱调用失败（0x80045040 / voice security），获得执行审批后通过 System.Speech 输出有效 WAV，替换失败的空产物；未引用损坏音频。源表先导入再 BakeCurrent 三张表，均成功无诊断，不直接编辑 bytes。剧情与资源全流程校验零错误，证据 sample-complete/validation.json。

MCP 编译错误 0；原报告 .utmp/visual-novel-m3/tests-20260921-024155038.xml，归档 .utmp/visual-novel-m5/sample-complete/final-passed.xml：3/3 通过。完整游玩测试新增两个实际结局及内心、双人、空镜、选择覆盖断言，保留退出清理及 SO 不变检查；其余两项验证主菜单继续/读档与阅读辅助嵌套。测试 job 域重载后滞留，依据 Console 提供的 XML 读取，Editor 空闲后清理。实际双人截图已入文档；静态独白/空镜截图不冒充运行截图。

SampleWalkthrough.md 提供按节点/句子位置的仿写与功能验收矩阵；Authoring、README、Design 更新当前入口。有配音自动等待、听感、跨启动偏好、长句/异形屏仍需逐项实测，不用这三项快速推进回归替代。跨章节保留 M1Sample 参考；正式示例不故意放坏资源。未 Save/Bump、未部署、未提交或发布。

## 2026-09-21 段内演出步骤可读性

根据用户截图优化 NarrativeContentGUI：指令类型、立绘槽位、显示/替换/隐藏、变量作用域使用中文下拉；始终显示语义摘要，例如“隐藏左侧立绘 · 0.25 秒”“显示居中立绘 · 林晚（lastlight_alice） · 0.25 秒”“本章节 · heard = 是（true）”。隐藏动作不再展示无意义的空资源输入框，并说明退场不需要图片、原位置为空时保持为空；切回显示/替换仍保留原资源键，不在绘制时清除数据。新增全部展开/收起，保持复制、排序、稳定 ID 和运行期只读约束。旁白/台词摘要继续显示角色与正文。

仅编辑器呈现变化，不删除示例的清场步骤，不修改 SO、配表或运行语义。静态核对枚举顺序与 NovelPortraitRow 的引用类型。当前会话 Unity MCP 工具不可用（refresh_unity 未提供，工具清单无 Unity 工具），停止自动验证；未编译、未取得界面截图、未运行 Test Runner，不能沿用上一批 3/3 作为本次验证。请在 Unity 中手动触发编译；若仍报错，发送首条错误及完整堆栈。未 Save/Bump。

## 2026-09-21 工具栏分组与统一模板菜单

流程窗口工具栏按剧情/章节分组，加大按钮内间距、支持窄窗口换行。保存/校验保持直接入口，新建剧情/章节收进“新建”，同步出口/章节总览排布收进“剧情工具”，节点排布/全部/聚焦/100% 收进“视图”；缩放与吸附仍可直接操作，侧栏按钮也允许换行。未修改剧情或用户图坐标。

统一 Ember/视觉小说 下三个工具：流程编辑与运行观察、Gameplay 主UI布局、从所选入口开始新游戏。移除布局窗口静态 MenuItem，改由现有 NarrativeEditorAvailability 动态注册并清理旧 Ember/Visual Novel 路径。可见条件为 NarrativeModule.Enabled 且编辑记录 templateId=visual-novel；工程变化时使模板缓存失效并延后合并刷新，非当前模板时移除菜单而非仅置灰，创建资产入口同样受限。入口执行再次检查条件，布局写入仍核对模板及 Prefab Mode；不丢弃布局窗口现存草稿。流程窗口直接打开入口也受模板检查。

静态核对 Game.Narrative.Editor 已引用 Game.UI.Editor，无新增循环依赖；旧英文工具菜单字符串仅留在清理逻辑。Unity MCP 工具清单当前无 Unity 工具，未尝试其他自动编译渠道；本次未完成 Unity 编译验证、实际工具栏截图或切换模板菜单显隐回归。请在 Unity 中手动触发编译；若仍报错，发送首条编译错误和完整堆栈。未保存/Bump、未提交。

## 2026-09-21 M5 再接手、跨剧情观察定位与交付盘点

先只读检查规则、设计、实施记录、git status、编辑记录和模板 metadata，确认 visual-novel 0.4.0 / preview、父 base 0.6.4。实际工作副本已经包含多批 M5，不重新制作示例、UI 或编辑器。保留所有既有未提交改动；未切模板、未改正式 Prefab/Binding、未提交或发布。

源码发现：LocateCurrent 原来优先比较当前 chapterId，否则在全部 Assets 按 chapterId 查找；浏览另一个剧情的相同章节/节点 ID 时可能定位错误，且关闭跟随时可能误高亮。现按诊断 storyId 解析所属剧情后限定章节范围，缺失/歧义保留 ID 和提示，不回退到其他剧情；没有 storyId 的独立章节宿主仍要求全项目唯一章节。运行错误按钮先解析当前诊断的所属章节，再用既有错误定位展开指令。图与段内高亮仅接收当前浏览剧情匹配的快照，观察面板仍展示真实运行快照。没有增加订阅、控制运行器或写入 SO。

新增 `NarrativeStoryEditorTests.ObservationUsesStoryIdentityWhenBrowsingAnotherStoryWithMatchingIds`：构造两个不同剧情但相同章节/节点 ID 的隔离资产，断言浏览外部剧情不高亮、定位回正确剧情和节点、全部 SO 内容不变；扩展 `NarrativeAvailabilityTests` 将 Gameplay 主UI布局加入菜单开关检查。测试已编写但未执行，不能记为通过。

本次即时静态证据位于 `.utmp/visual-novel-m5/handoff-20260921/`：

- `managed-differences.json` / `verification.json`：五个受管目录及其根目录 meta 的逐文件 SHA-256 比较共 180 项（121 新增、47 修改、12 删除）；其中五项是受管根目录自身 meta。文件分组均落在已记录的小说文档/示例/资源/表/UI/编辑器/测试改动，以及旧一次性制作器的删除。已有 DevCenterSmoke 资源本次相对快照无差异，未删除既有内容。这个原始文件盘点未执行项目中心的过滤/规范化，不冒充 CompareEditingTemplate 或正式保存预览。
- 父快照 262 个文件与 `.utmp/visual-novel-m3-m4-seal/template-before/ParentSnapshot~` 逐文件一致；元数据、版本及编辑记录未改。未以脚本重算或写入模板 contentHash。
- `static-content.json`：源 CSV 键、资源文件存在性及 Sprite 导入配置检查无问题；四段分别 25/20/20/14 条 Say，总计 79，Voice 引用两句，指令/台词 ID 无重复。这不是 Unity 资源加载、生成 bytes 或全流程运行校验。
- 读取既有 `sample-complete/final-passed.xml`，确认 2026-09-21 02:41:54Z 结束，实际执行 3/3、0 失败；仅作为该历史示例批次证据，不覆盖之后的表单、菜单和本次定位修正。

完整示例与编写说明的整理项已按实际资产核对勾选；其余 M5 验收和交付项保持待办。修正 Design 中仍写“11 分钟/Voice 留空”的旧当前态，README/仓库索引同步本次状态。约 10 分钟仍是估算；新游戏揭幕、窄屏长句大字号、真机安全区和有配音自动等待的人工确认没有补造。

当前工具清单没有 Unity MCP，按 CLAUDE 停止自动验证，没有读取 Editor.log、启动 BatchMode、运行 dotnet 或其他替代编译。恢复验证时先检查现有测试任务状态，再运行新增定位用例、NarrativeStoryEditorTests、NarrativeAvailabilityTests 与 NarrativeObservationPlayTests；完成表单/连接、三种入口、分辨率/长句/Voice/反复进出验收后，再取得当前工作副本全量 XML。项目中心完整预览五目录、SaveTemplate、显式 Bump 与保存后零差异核对仍未执行，不能用本次文件盘点替代。消费端部署运行同样待完成。

本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。

## 2026-09-21 Unity 6.5 编辑器 API 警告修正

用户提供 9 条 CS0618，项目版本为 6000.5.4f1。NarrativeMiniMap 和 NarrativeGraphWindow 改从 GraphView.contentViewContainer.resolvedStyle.translate / scale.value 读取当前平移与缩放，继续调用 UpdateViewTransform 写入，保留 GraphView 原有通知及视图持久化流程。NarrativeGraphInteractionTests 的滚轮断言改用同一新读取接口。OnOpenAsset 参数改为 UnityEngine.EntityId，直接交给 EntityIdToObject；NarrativeAvailabilityTests 的反射参数同步改为 EntityId.None，避免测试仍传装箱 int 导致类型不匹配。未屏蔽警告、未改正式 UI 或剧情资产。

接口核对依据：Unity 官方 UnityCsReference 的 VisualElement.cs（ITransform getter 到 resolvedStyle 的映射）、GraphView.cs（viewTransform 对应 contentViewContainer）及 AnimationWindow.cs（EntityId 版本 OnOpenAsset 回调）。这只是 API 源码核对，不代替本机 Unity 编译。

静态搜索 Narrative 目录没有剩余 viewTransform.position/scale 读取；git diff --check 通过。用户表示 MCP 已连接，但本任务工具清单没有 Unity 工具，资源及资源模板清单亦无 Unity 入口，因此未触发刷新/编译或运行测试，没有通过 Editor.log、BatchMode、dotnet 等方式补验。后续应复验小地图点击定位、章节/总览平移缩放与重开恢复、真实滚轮测试以及双击 SO 打开流程窗口。M5 未保存/Bump、未部署消费项目、未提交或发布。

本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。

## 2026-09-21 用户确认警告修正后无报错

用户在 9 条 CS0618 对应代码修正后反馈“已连接无报错”。据此登记本批工作副本人工编译无报错；未收到新的 Console 警告计数、Test Runner XML 或实际交互验收，不扩写成“全部警告已清零”“测试通过”或“M5 完成”。本次仅更新验收记录，没有再次修改代码。

本轮重新检查当前任务工具清单，仍未暴露 Unity MCP 工具，因此没有发起自动编译、测试或项目中心保存操作。下一步优先运行 NarrativeGraphInteractionTests、NarrativeStoryEditorTests、NarrativeAvailabilityTests 和 NarrativeObservationPlayTests，覆盖缩放/小地图、跨剧情定位、入口开关与观察生命周期；随后完成 Gameplay 和 UI 边界验收。封存仍为 visual-novel 0.4.0 / preview，父 base 0.6.4；未保存/Bump、未部署、未提交或发布。

## 2026-09-21 14:26 用户测试报告与双结局测试宿主修正

原始 TestResults_20260921_142600.xml 已归档至 `.utmp/visual-novel-m5/user-tests-20260921-142600/results.xml`，修改前测试源码同目录保留。报告实际执行 382 项：380 通过、2 失败、0 跳过；不把程序集发现数当执行数，也不将本批称为全部通过。

本报告确认最新编辑器相关四组通过：NarrativeGraphInteractionTests 4/4、NarrativeStoryEditorTests 5/5（含跨剧情同 ID 定位）、NarrativeAvailabilityTests 3/3、NarrativeObservationPlayTests 2/2。此为用户提供的自动化 XML 证据；不等于人工屏幕/长句/安全区验收，也不证明消费项目已部署。

两项失败同属 `NarrativeStoryTests.DeliverySampleHasTwoReachableEndingsWithoutAssetWrites` 的两个参数用例，断言期待 last_light_heard / last_light_tomorrow，实际 EndingId 为 null，堆栈指向原第 165 行。源码核对发现测试循环只处理 Choose、Presentation 与 Advance，没有处理 Timer；LastLight intro 已含 last_light_demo_intro_silence（0.6 秒 Wait）。运行器在该指令必须由 Tick 消耗时间，Advance 不处理计时等待；相邻的 M1 三路线测试已有对应 Tick 分支。

修正仅位于上述测试：Timer 时调用 Tick，每次驱动断言被接受并附章节/节点/指令/状态/等待信息，循环结束先断言 Ended，再断言指定结局。保留 500 步上限、双结局与所有 SO 不变断言，没有删除 Wait、改变真实播放计时或放宽结局要求。该纯运行器宿主依旧模拟演出完成，不将其解释为真实配音/画面验收。

当前任务仍无 Unity MCP 工具，本次未完成 Unity 编译验证、未复测，不声称已取得 382/382。下一步只需先重跑该方法两个参数用例确认修正；其余 380 项保留为修正前报告证据，不合成为新单次全量报告。请在 Unity 中手动触发编译；若仍报错，发送首条编译错误及完整堆栈。未保存/Bump、未部署、未提交或发布。

## 2026-09-21 用户确认全部通过与文档收束

用户在双结局测试宿主补齐 Timer/Tick 后确认“全部通过了，收束文档”。据此关闭 DeliverySampleHasTwoReachableEndingsWithoutAssetWrites 两个参数用例的失败，将本批修正及测试记录收束。确认来源是用户反馈；未收到新的 XML，不推定复测执行数量，不声称取得单次 382/382 或本次 MCP 自动编译/测试通过报告。原 14:26 XML 的 382 执行、380 通过、2 失败及完整堆栈保持原样，不能覆盖为通过报告。

此前警告修正后的无编译报错已有用户确认；GraphInteraction 4/4、StoryEditor 5/5、Availability 3/3、ObservationPlay 2/2 有原始 XML 证据。最后修正只补测试宿主的 Wait 时间驱动与诊断，没有改运行器、示例剧情、资源、布局、存档顺序或配音行为；本轮仅更新文档，不增加源码改动或重新启动测试。

已同步 Implementation 顶部/M5 分项、README、Design、Authoring 和仓库 docs/README。历史阶段记录保留当时状态，最新结论以上方收束状态为准。明确保留以下交付边界：

- 人工交互和画面专项未逐项收到确认：指定入口实际试播、窄屏/长句大字号/安全区、配音听感及自动等待、跨启动偏好，以及新游戏揭幕实际画面；M3 其他专项继续独立跟踪。
- 新全量 XML 未归档；不能将旧 XML 的通过项与复测口头反馈拼接成一份新报告。
- 五目录文件盘点不替代项目中心正式差异预览；SaveTemplate、显式 Bump、保存后 CompareEditingTemplate 与内容/父基线核对仍未执行。
- 新消费项目部署运行未完成；未写入 Call Me Heartless，也未使用 Unity Farm。

当前封存仍为 visual-novel 0.4.0 / preview、父 base 0.6.4，未发布；M5 工作副本尚未封存。此次“收束文档”表示本批测试问题与证据记录已收束，不等于模板已交付。未改 Templates~、ParentSnapshot~、hash 或编辑记录；未提交 Git、未发布 tag。

## 2026-09-21 目录归属整理

本次为视觉小说模板目录调整，规范见 [DirectoryLayout.md](DirectoryLayout.md)。业务模块 Narrative、NovelSave 保持原位；UI 脚本和 Prefab 按两模块拆分，图片进入对应 UI 的 Atlas，跨界面 auto/speed 图标和字体进入 Common，剧情/输入进入 Config/Narrative，音频进入 Audio/Narrative。旧 Resources/VisualNovel、UI/VisualNovel 及遗留空 UI/Module/VisualNovel 已移除。

通过 AssetDatabase.MoveAsset 保留 GUID，同步 GamePages 注册、Prefab classPath、剧情编辑器/样例创建器/布局预览、资源 CSV 及测试路径。旧存档 StoryPath 在请求入口兼容转换；不改存档 schema、稳定 ID 或剧情内容。配表由正式 BakeAndGenerateAll 成功导出，无诊断。

验证：22 项迁移 GUID、12 条配表资源加载、9 个 Prefab 缺失脚本检查，共 43 项检查无错误。MCP 刷新后控制台曾返回 0 编译错误；110 项 Narrative 回归停在 12 项完成、0 已报告失败，后台/播放模式停滞后 MCP 控制台查询超时，不能宣称完整测试通过或本轮最终编译验收完成。已退出测试播放、清理孤立测试任务并恢复临时 runInBackground 设置，回到 FrameworkScene。请在 Unity 中手动触发编译；如仍有报错，提供首条错误及完整堆栈。之后补跑 Game.Narrative.Tests，含旧存档路径兼容用例。

模板保存前已使用项目中心同一 CompareEditingTemplate 完整预览受管业务目录，0 错误、0 警告。通过 SaveTemplate 保存当前业务工作副本；包括此前尚未保存的 M5 内容，不 Bump、不发布、不部署消费项目。布局窗口未保存草稿不自动写入 Prefab。
### 本轮追加：M1Sample 与共用图标

按用户追加要求，Resources/Config/Narrative/M1Sample 已移走；真实跨章节 SO 以原 GUID 保留到 Game/Module/Narrative/Tests/Fixtures/M1Sample，测试改用 AssetDatabase 加载。默认运行剧情仍为 LastLight，旧 M1 剧情存档不再支持；纯存档单元测试采用独立测试路径。旧样例生成工具同步指向测试夹具，不会再生成运行 M1 目录。LastLight 复用的 BGM/SFX 与现有配表引用的占位图仍保留。

主对话框及阅读菜单的 8 张图标全部统一到 UI/Common/Atlas/Novel，移除 Narrative/Atlas/Icons 空目录。追加修改只完成静态路径/GUID核对，未重启已中断的自动编译/测试流程；仍需手动编译和回归。


## E1 续作记录（2026-09-21，未完成验收）

已落盘：人物身份与命名目标分离、位移/缩放/旋转/镜像/层级/手势/强调运行接线、Schema 3 恢复、正式阅读页既有 Binding 适配、编辑器参数与同属性冲突提示、《最后一盏灯》开场演出、5 个 E1 NUnit 用例。没有修改框架 API、创建新 UI 骨架或手改 Binding。

静态修正：允许画面外坐标投影后的存档偏移；强调目标退场后恢复其他人物亮度；旧槽位空 Hide 不误删已移走人物；内部 legacy 身份避免重用；拒绝 Schema 2 中非法的非默认变换。

实际验证：本轮 `git diff --check` 返回 0（仅仓库行尾转换提示）。静态核对新增枚举与编辑标签顺序、序列化字段、示例指令与动作 ID。**未执行 Unity 编译或测试，新增 5 个用例未通过验证，不计入 E0 的历史测试通过数。**

阻塞：本轮可调用工具和资源没有 Unity MCP；上轮刷新调用句柄无法恢复。遵守 CLAUDE.md，不通过 Editor.log、进程轮询、BatchMode 或 dotnet 替代编译验证。请在 Unity 中手动触发编译；仍有报错时提供首条错误及完整堆栈。

剩余验收：
- Unity MCP 恢复后编译并运行新增用例及原 Narrative 回归；根据实际失败修复。
- 从“新游戏”进入《最后一盏灯》，验收已写入的 E1 开场演出和后续原剧情。
- 补齐并运行正式阅读页 16:9 / 4:3 画面验证、往返位移、双人交换、强调/旁白、退场重入、快进、存档恢复及清理验证；当前新增用例未覆盖所有验收项。
- 项目中心 Compare 审阅范围后 SaveTemplate，再显式 Bump，确认内容与封版 hash 一致。当前未执行 E1 SaveTemplate/Bump，仍保留 0.5.0 封版，禁止把本批标记完成。

未自动提交、发布、打 tag 或部署消费项目。E1 闭环完成后才进入 E2。


### E1 验收入口简化

按用户要求，E1 的 44 条演出指令已直接加入现有 CH01_intro.asset，位于原剧情对白之前；保留原指令、资源 GUID、后续节点与分支。独立 E1 生成器及菜单已移除，无需点击生成按钮。开场使用已存在的 Alice / Lin 示例立绘（包含可见换表情），结束后恢复《最后一盏灯》原有人物和叙事。开场内容修订由 3 调整为 4；新游戏进入，旧进度可能因内容指纹变化无法恢复。

静态核对通过：新增命令/台词 ID 无重复、动作等待引用可解析、立绘和角色键在配表中存在、原指令和 _next 引用保留。首句前仍显示居中人物；正式 Gameplay 双路线测试帧上限随新增计时动作由 2400 调至 6000。尚未执行 Unity 导入、编译、自动测试或真人试玩，不把静态核对视为 E1 验收通过。SaveTemplate/Bump 仍待 Unity 验证完成后执行。


### E0 验收并入《最后一盏灯》（2026-09-21）

按用户要求，CH01_intro.asset 在 E1 前增加 27 条 E0 验收指令，内容修订由 4 升至 5。原有 E1 指令、原剧情、资源 GUID 与节点连接保持不变。新游戏依次执行 E0、E1、原剧情；人工验收不再要求选中独立示例。

覆盖：同角色双实例、人物透明度延迟并行与对白、等待组、同属性接管及取消后等待、按实例替换、背景透明度、舞台透明度。对白提供暂停、倍率、稳定点存档恢复、已读快进与动作中退出重入的检查提示。段尾等待并清理实例，背景和舞台透明度均恢复为 1。历史 PresentationE0 资产保留供既有自动测试使用。

实际完成：静态检查新增 ID 唯一、等待引用可解析、立绘键存在、透明度参数范围、三类目标覆盖，且剔除新增块后原资产文本仅内容修订不同；git diff --check 通过。正式 Gameplay 双路线测试增加 E0 计时演出后，帧预算从 6000 调整为 9000。

未完成：Unity MCP 本轮仍不可用，未执行 Unity 导入/编译、自动测试或人工演出验收；本记录不替代 E0 历史通过记录，也不声称新增编排已经通过。请在 Unity 中手动触发编译，若仍报错，提供首条错误及完整堆栈。项目中心 SaveTemplate 和显式 Bump 仍待验证后执行。未提交、发布或部署。


### 用户报告回归修复：TestResults_20260921_180904.xml

用户提供的报告为 461 项：451 通过、10 失败。报告保存在 `.utmp/visual-novel-e1/reported-failures/`，这次结果属于修复前运行，不代表下面的修改已通过复验。

已按证据处理：
- `RapidClickAndNestedPauseCannotCrossLineOrAdvanceTransition`：E1 曾把空逻辑槽 Hide 即时跳过，破坏旧剧情指定的过渡时长。恢复计时；若该物理根仍承载已移走的实例，不调用 Hide 或删除实例。新增 `E1EmptyLogicalSlotHideKeepsDurationAndDoesNotHideMovedActor` 覆盖此边界。
- `E1MoveAndScaleAreIndependentPauseAndCaptureFinalPose`：测试 Tick 已让对白全文显示，额外 Advance 推到了结尾，导致存档被正确拒绝。改为仅在 Revealing 时补全，并先断言 AwaitingAdvance。
- `DialogueRegionAndSpaceAdvanceWithoutClickThrough`：报告停在 Presentation/Actions，原按 1200 帧计的等待不对应演出秒数。通用等待改为实时 30 秒上限，仍保留实际完成断言。
- 5 项失败共享 Input System 静态初始化异常：`RestoreStateWithoutDevices` 恢复的 settings 为 null。输入测试此前替换并销毁临时 InputSettings；现改为在原对象上暂改两个输入行为字段，并在 finally 恢复，不再销毁设置对象。此修改针对已观察到的生命周期风险，是否消除全部连带异常仍待复验。
- 2 项 Observation 测试都在进入 Play 后第一次 yield null 超时，尚未执行窗口检查。加入进入前后取消编辑器暂停的保护；报告不足以确认超时根因，不能标记已修复，重跑仍需观察首帧日志。

本轮 git diff --check 通过。Unity MCP 不可用，未完成本轮 Unity 编译及测试复验；请在 Unity 中手动触发编译，若仍有编译错误提供首条错误及完整堆栈。Input System 静态初始化已失败的编辑器应先重启，以清除本轮残留状态，再重新运行测试。未 SaveTemplate/Bump，未提交、发布或部署。


### 重启后复验：TestResults_20260921_194006.xml

用户报告 462 项、460 通过、2 失败。E0 7 项全部通过，E1 6 项全部通过；上轮空槽 Hide、稳定点存档、输入行为用例及 Input System 异常涉及的用例本轮均未失败。剩余为 StoryOverview 观察测试超时与完整 Gameplay 双路线未在帧预算内到达结局。报告归档 `.utmp/visual-novel-e1/reported-failures/TestResults_20260921_194006.xml`。

本轮修改仅涉及测试驱动：
- StoryOverview 再次停在进入 Play 后的首个 `yield return null`。核对 ShowStory、OnSnapshot 和 LocateCurrent 的实际实现，其被验收的模型状态同步更新；改为在真实 Play Mode 中同步调用检查方法，移除两个不必要的玩家循环等待。保留跨章节、全局变量、总览跟随、只读拒绝、定位及资产不变断言；不声称覆盖屏幕绘制。
- 完整剧情演出按秒执行，9000 帧在本次运行中仍先耗尽。改为每条路线 60 秒实时上限，使用正式阅读倍率 3X 运行全部指令和 UI 交互，仍检查两种结局、双人、空镜、选项、资源清理及资产不变。另设同指令 15 秒无推进检查，失败时打印命令、等待、暂停与动作进度；不会把等待挂死当作通过。

本轮静态检查与 git diff --check 通过。Unity MCP 仍未提供可调用工具，未完成本轮 Unity 编译或重跑；460/462 是修改前用户复验结果，不能标记本轮全通过。请在 Unity 中手动触发编译，若仍报错提供首条编译错误及完整堆栈。先复跑上述两个用例，通过后再做完整回归；本轮无需因测试改动再次要求重启。仍未保存封版、提交、发布或部署。


### 正式示例叙事整合与占位图退役

用户确认上一轮测试通过（本轮未提供新的 XML，记录为用户确认，不虚构报告编号或计数）。随后按用户意见移除 LastLight 开头全部 E0/E1 测试段和技术提示对白，把演出重新编排进 intro/radio/letter/common 原情节；保留 79 句正文、原 lineId、Voice、变量分支和结局；调整 7 句旁白并递增对应文本修订。使用稳定实例 wan/zhou，正式资源键仅 lastlight_alice、lastlight_zhou、lastlight_rooftop。SampleWalkthrough 改为情节与动作的逐项仿写索引。

源表中的五个旧资源键重定向到三张正式图片，保持测试键兼容。NarrativeM2Assets 移除简陋图生成代码，避免旧图被再次创建。没有手改 bytes 或生成代码：新增一次性 Editor 导入迁移，调用配置表中心 EmberTablePipeline.BakeCurrent 烘焙两个表，确认无旧路径后备份并逐个删除五张旧 PNG/meta。Unity MCP 不可用，迁移尚未由本会话执行或确认；Unity 导入脚本后自动安排迁移，无额外操作按钮。如果导出失败保留尚未删除的旧图并报告诊断，不制造缺图。旧资源文件目前可能仍在 Assets，不能在运行迁移前宣称删除完成。

本轮已做静态剧情核对：79 句正文、183 个命令 ID 唯一、45 个动作 ID 唯一、等待引用先启动、实例目标存在、命名目标无冲突、同时最多三个实例。保留原图连接和角色/语音键；两个分支都重建明确状态。未运行 Unity 编译、自动回归、素材迁移或实际渲染复验；请在 Unity 中手动触发编译，如有错误提供首条错误及完整堆栈。SaveTemplate/Bump 仍待此次复验后通过项目中心完成。未提交、发布或部署。

### E2：工作区实现与待验证边界

> 初次实施时的历史记录。后续测试已获用户确认全部通过，当前状态见 [文档收束](#e2-测试通过与文档收束)；以下保留当时实际执行范围。

2026-09-21，用户授权实施 E2。本轮只修改 Assets 业务层与文档，保留既有未提交改动；未修改框架 API、模板快照、ParentSnapshot 或 hash，未提交、发布、部署。前轮“测试通过”的用户确认不覆盖本轮新增功能。

实际落盘：

- 枚举末尾追加 Shake/Cover/Flash/CrossFade（17–20）；数据、参数/资源校验、图编辑入口、摘要及同通道接管提示完整接线，旧枚举顺序和旧剧情指纹保持。
- E2 动作沿用句柄、并行/等待、延迟、暂停、倍率和快进。Shake 的临时偏移独立于 Move/Gesture；Cover 与 Flash 共用遮罩通道；Flash 结束恢复原遮罩。背景/人物双图渐变保留旧租约到不可见，无其他实例引用时释放；缺图保留旧画面并报定位错误；中途接管先收束旧目标再开始下一次双图过渡。旧式 Replace 保持原语义并取消该人物的 CrossFade。
- Schema 4 保存遮罩，兼容 Schema 1–3 默认值；稳定点投影最终颜色/新图，不存临时震动、不重播闪光；Stage Shake 不再被误投影成 StageOpacity。原配音、已读和恢复事务不变。
- 读屏层只克隆 EUI 中心已生成的背景/立绘 Item 为运行实例，沿用现有 Binding；未手写生成文件。新增系统设置偏好持久化为 Normal/Reduced/Off，缩放 1/0.25/0，不改变动作计时及最终状态。系统设置正式 Prefab/Binding 的升级通过 `NovelScreenSettingsMigration` 调用 EUI 中心 API，尚待 Unity 执行确认。
- imagegen 内置工具基于正式原图生成林晚微笑与屋顶入夜两张同构图变体，已检查输出并放入 Assets；没有复活旧简陋图片。配表源增加两键，Sprite 导入和 BakeCurrent 由 `NarrativeE2AssetImport` 安排，未手改 bytes。素材与提示词见 [E2Artwork](E2Artwork.md)。
- LastLight 保留全部 79 句 Say 记录（含 lineId、textRevision、正文、配音）以及原节点连接/变量/结局，新增自然演出和等待，现共 204 指令、58 唯一动作 ID，其中 13 个 E2 动作。无新测试对白、独立测试章节或测试按钮。

实际执行的静态检查：`.utmp/visual-novel-e2/validate.py` 全部通过，报告 `static-validation.json`。检查 YAML、旧对白一致、ID 唯一、先启动后等待、实例/命名位置、资源键/PNG 路径、旧枚举值、改动 C# 括号与空白、Narrative 生成 Binding 不变和设置页 EmberManaged 块不变；两张 PNG 解码成功。这些检查不等于 C# 类型检查、Unity 编译、资源导入或运行测试。

新增待运行测试：`NovelScreenTests.cs` 9 个状态/资源/兼容测试方法，`NovelScreenViewTests.cs` 1 个正式页测试方法 × 2 种分辨率，共 **11 个用例**。覆盖移动叠加、暂停/倍率/减弱关闭、延迟接管/等待、Cover 保持与快进、Flash 稳态恢复、双图租约释放、缺图、旧式替换中止淡化、参数/指纹、旧 schema 迁移；正式页检查两图中间帧、镜像/亮度/透明度继承、震动不影响正文、遮罩层级、退出清理。运行时会输出 `.utmp/visual-novel-e2/frames/{1920x1080,1440x1080}/{start,middle,end}.png`，本轮尚未生成这些验证图。

**本轮没有执行 Unity 编译或自动测试。** 工具清单没有 Unity MCP，按 CLAUDE.md 未用日志/进程/BatchMode/dotnet 代替验证。请在 Unity 中手动触发编译；如仍有报错，发送首条编译错误及完整堆栈。待完成：

1. 首次编译后确认 EUI 中心设置迁移、Sprite 导入与配表 BakeCurrent 成功；如生成 Binding 引发二次编译，待它结束再测试。两个新资源键必须能在流程编辑器下拉中选择。
2. 运行新增 11 用例、E0/E1 会话及存档阅读回归；再跑 LastLight 两条完整路线。不能用前轮通过记录替代。
3. 检查正式页开始/中间/结束帧、16:9 与 4:3、菜单/历史/设置暂停、黑幕后换景与揭幕、系统设置正常/减弱/关闭、快进/存读档/退出重开；移动端/Player 仍保留原待办。
4. 通过后在项目中心 **SaveTemplate → 显式 Bump**。本轮这两步未执行，封存仍 `visual-novel 0.5.0 / preview`，父模板 `base 0.6.4`。

下一批入口：E3 视觉效果实例、环境循环音、音频渐变与停止；本轮未实施。

### E2 测试通过与文档收束

2026-09-21，用户反馈：“测试全部通过，没有报错”，并要求收束文档。登记为本轮 E2 实施后的用户测试确认；此前要求重跑的测试不再列为当前阻塞。本次没有新的 XML、逐项结果或总数，未由代理重跑 Unity，不能把该反馈改写成 MCP 实测报告或自拟通过计数。新增测试定义共 11 个用例，此数字不是本次反馈的测试总数。

| 项目 | 当前结论 |
|---|---|
| E2 数据、运行、编辑入口 | 已实现 Shake、Cover、Flash、CrossFade；沿用 E0/E1 动作句柄、等待、暂停、倍率、快进与清理契约 |
| 正式示例 | 演出自然融入 LastLight 正文，使用正式男女立绘、背景及微笑/入夜变体；未新增测试段落或按钮 |
| 测试结果 | 用户确认全部通过、无报错；前次静态检查及历史报告另行保留，不冒充本轮自动运行证据 |
| 文档 | README、计划、制作说明、效果配置手册、正文索引、素材记录与本清单已统一状态 |
| 模板封存 | 只读核对 metadata：visual-novel 0.5.0 / preview，父模板 base 0.6.4；E1/E2 尚未保存封版 |

剩余事项分开跟踪：

1. 交付：通过项目中心 SaveTemplate 后显式 Bump，届时更新实际版本与验证记录；禁止手改模板快照、ParentSnapshot 或 hash。
2. 人工观感：未单独收到正式页开始/中间/结束帧、16:9/4:3、合成边缘、暂停/快进/读档/重开的逐项人工确认；沿 [示例正文](SampleWalkthrough.md)复核，无需添加独立测试段。测试通过不自动等同全部美术与交互观感签收。
3. 平台：移动端、异形屏、大字号/极长文本边界、Player 构建和消费项目验收继续保留，不因本次反馈自动关闭。

本次仅修改文档，未修改业务代码、资源或模板 metadata，未执行 SaveTemplate/Bump、Git 提交、框架发布或消费项目部署。

下一批入口为 **E3：视觉特效与声音编排**，范围见 [实施计划](PresentationRoadmap.md#e3视觉特效与声音编排)：效果实例播放/停止与清理、环境循环音、音频渐变和停止、持续状态恢复。本次未扩展到 E3。

### 选项半透明底框修复（2026-09-22）

用户截图显示 LastLight 两个选项只剩文字。静态定位到正式阅读页嵌套 ChoiceTemplate 的 Select/Image：源 Item 背景存在，但阅读页将颜色覆盖为白色、alpha = 0。运行时克隆该模板，因此两条选项均无可见底框。

新增 Editor 一次性修复 `NovelChoiceBackgroundMigration`，Unity 编译加载后（若正在播放则退出播放后）自动检查当前 visual-novel 工作区，仅将完全透明的选项背景恢复为深色、55% 不透明度。保留既有文字、布局、间距、按钮状态及整条点击范围；非透明的后续人工样式不覆盖。通过现有 EUI Binding 定位并调用开发中心校验 API，再由 PrefabUtility 保存正式阅读页，不改 Binding 生成代码、不创建额外 UI 或操作按钮。保存前备份阅读页和 meta 到 `.utmp/visual-novel-choice-background/before`。

本轮仅完成原因静态核对、修复脚本和空白检查。当前没有可调用的 Unity MCP，尚未执行迁移、Unity 编译或最终画面验收；不能以 E2 前轮全通过替代本次验证。请在 Unity 中手动触发编译，如仍有报错，提供首条编译错误及完整堆栈。随后直接在 LastLight 原分支处检查两个独立半透明底框、悬停/点击、菜单暂停恢复及 16:9 / 4:3 显示。未 SaveTemplate/Bump、提交、发布或部署。


### Gameplay 主 UI 布局与外观编辑（2026-09-22）

本批仅修改 Assets 中的编辑器、测试和说明。布局窗口增加子元素选择、图片/文字/按钮状态/现有渐变属性、选项模板和菜单外观入口、推进点击范围叠加与当前元素还原。外观草稿按层级索引、组件身份和白名单属性保存；嵌套覆盖、撤销/重做、预览重建与原有外部依赖冲突检测接线。保留正式 UI Binding 与运行时推进逻辑，不创建 UI 骨架，不直接修改模板快照。

新增 NovelLayoutAppearanceTests 两个参数用例，检查箭头与嵌套选项的换图/颜色/模式草稿重载、预览一致、点击范围不变及不修改正式资产；当前未运行。静态检查已完成，不能替代 Unity 编译与实际窗口验收。当前 Unity MCP 不可用，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。

待 Unity 验证：运行上述用例，人工检查底板/箭头换图、字体和渐变、撤销/重做、保存后关闭重开及两个分辨率。主题预设、批量样式复制、按钮状态试播与长文本专项预览未实现。未 SaveTemplate/Bump、Git 提交、框架发布或消费项目部署。


### 单节点编辑态演出试播（2026-09-22，实施时记录）

本批在 Assets 编辑器层新增 NovelNodePlaybackWindow、NovelPlaybackStory、NovelPlaybackView、NovelPlaybackAudio；节点编辑器新增“播放节点”。没有新增正式 UI、手改 Binding、修改运行时演出算法或全局 Manager。预览通过既有 EUIBindingBridge 绑定正式阅读页副本，调用已有 E0–E2 舞台接口；文字与输入由编辑态适配，不调用正式 OnOpen、账号与菜单逻辑。

临时剧情复制所选对话节点及变量定义，替换副本 Next 为独立结尾，并追加本节点动作等待组，防止结束时取消尚未完成的并行动作；原 SO 不变。起始背景、最多三个人物及章节/全局变量均为试播局部设置。默认空舞台，明确报告缺失的实例/资源/前置动作。停止恢复起始画面，结尾保留画面和循环 BGM 至停止；关窗、重编译、进入 Play Mode 释放会话、音频图和预览场景。节点修改提示重播，项目资源变化暂停试播，编辑器长时间阻塞也自动暂停。

音频采用公开 AudioClipPlayable / AudioPlayableOutput 和手动驱动 PlayableGraph，每轨独立资源与增益；不变更全局静音/时间、资源导入设置或用户场景。静音不缩短 Voice 计时。当前尚未在 Unity 实际听音，不能宣称编辑器音频后端已实测可用。

新增 NovelNodePlaybackTests：7 个用例（5 个测试方法，两个分别参数化为两例），覆盖并行末尾收束/暂停、不走原 Next、变量与源编辑隔离、缺失人物与显式起始人物、静音配音计时及音轨释放、16:9/4:3 正式双图中间状态与重复清理。此为待运行测试定义，不是通过计数。静态检查范围：源文件结构、关键接线、程序集引用、文档链接、meta 唯一性与空白检查；不替代类型检查、渲染、听音或运行测试。

当前没有可调用 Unity MCP，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。编译后先运行 NovelSessionTests 中 EditorPlayback 开头的 7 项，再回归既有 E0/E1/E2 用例；人工检查 LastLight 自包含节点及依赖前置人物的节点、自动/手动推进、声音叠加、暂停静音、停止重播、窗口关闭/重编译后无残音、两个分辨率。

未保存或 Bump 模板，未提交 Git、发布框架或部署 Call Me Heartless。跨节点连续播放、分支选择、时间轴拖动/倒放、未保存布局草稿预览与玩家账号显示偏好不属于本批。


### 试播与布局测试反馈修正：TestResults_20260922_130836.xml

用户提供报告：482 项，478 通过、4 失败、0 跳过。原始报告已归档到 `.utmp/visual-novel-node-playback/reported-failures/`。此为修复前结果，不是修复后通过记录。

- 两个 AppearanceDraftSurvivesReloadWithoutChangingAssetOrClickArea 用例停在撤销后的 Sprite 引用断言（原第 46 行），报告 Expected/Actual 均显示 null。原 Assert.AreSame 检查托管引用身份；改用 UnityEngine.Object 的相等语义检查实际 Sprite 或空对象，并补充重做后的图片断言。颜色、草稿重载、预览一致、点击范围与正式资产不变断言保留。
- 两个 EditorPlaybackFormalViewRunsE2AndRepeatedDisposalLeavesPrefabUnchanged 分辨率用例停在 AwaitingAdvance。测试循环每帧调用 SetReadMode(Auto)，而该方法每次都会重置 _autoElapsed；改为与实际试播窗口一致，仅会话活动且模式不同才切换。保留原帧上限、自动到达结尾、双图中间状态、最后对白、重复释放和 Prefab 不变断言，未用强制推进跳过问题。

本轮只修正上述两个测试文件和验证记录，未修改正式运行逻辑。静态检查完成；当前工具仍无 Unity MCP，本次未完成 Unity 编译验证及测试复跑。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。优先复跑上述四个失败用例，再运行本批布局/试播回归；此前因首个断言提前退出的后续检查仍需复验。

未保存封版、提交、发布或部署。


### 编辑工具测试通过与文档收束

2026-09-22，用户在两处测试修正后确认“通过”，本轮测试反馈关闭。证据来源是用户反馈；未附修复后 XML，也未明确复跑范围与总数，不能将修复前报告改记为 482/482，不能表述为代理自动复验通过。原始 482 项、478 通过、4 失败的 XML 保留作为修复前历史。

本轮交付：

- 主 UI 布局窗口支持子元素布局与图片、文字、按钮状态、已有渐变外观编辑，统一草稿、撤销/重做、还原与保存；推进点击范围可叠加查看。
- 对话节点“播放节点”支持编辑态整段单节点试播，复用正式会话与 E0–E2 舞台，提供自动/手动推进、暂停、倍率、静音、重播、停止、起始人物/背景/变量与错误定位。
- 测试修正保留原有行为断言：Unity Sprite 对象相等语义与自动模式切换计时已修正；运行逻辑未因本轮失败报告而改动。
- README、Authoring、PresentationRoadmap 与本清单统一为实现完成、测试修复后用户确认通过；历史验证记录保留。

未关闭的边界：实际编辑态听音、窗口交互与 16:9/4:3 人工观感、保存后关闭重开等人工检查未逐项确认；平台、Player 与消费项目验收继续保留。试播使用已保存正式 Prefab，不消费布局草稿；不含跨节点/分支连续播放、时间轴拖动或倒放。E5 仅完成这一独立制作工具子项，其他 E5 能力及 E3/E4/E6 状态不变。

只读核对封存 metadata 仍为 visual-novel 0.5.0 / preview、父模板 base 0.6.4；E1/E2 与本轮编辑工具尚未 SaveTemplate / Bump。本轮仅收束文档，未改代码或资源，未保存封版、提交 Git、发布框架 tag 或部署 Call Me Heartless。文档静态检查完成；没有新增 Unity 自动验证结果。

下一批为 E3：按 PresentationRoadmap 实施可寻址视觉效果实例、环境循环音、BGM 停止与渐变、持续状态恢复，并同步接入现有编辑态试播。优先评估 ParticleSystem，复用资源/音频/动作管线；持续效果不能加入节点末尾的有限动作等待而造成试播无法结束。示例继续融入 LastLight 原正文，不添加测试段落或按钮。

### E3 视觉特效与声音编排（2026-09-22）

本轮业务改动位于 Assets，保留既有修改；没有改动 Packages 框架 API、正式 UI Prefab/Binding、Templates~、ParentSnapshot~ 或 hash，未 SaveTemplate/Bump、Git 提交、发布或部署。封存仍为 visual-novel 0.5.0 / preview，父模板 base 0.6.4。

实际实现：

- 追加 EffectPlay、EffectStop、BGMStop、AmbientPlay、AmbientStop、AmbientVolume（21–26），原 BGM 扩展音量/渐变/延迟/并行。编辑字段、摘要、参数/资源类型校验、运行观察与兼容指纹接线；旧枚举值不变。
- 会话持有视觉实例、独立循环音和 INovelAssetLease；不建立 Manager。粒子使用现有中心生成背景 Item 的副本与 BaseMeshEffect，将受控 Local ParticleSystem 映射到 UI 层，支持实例、人物绑定/舞台绑定、位置、层级、缩放、舞台震动和透明度。一次性自然回收，持续状态显式停止，人物消失总是清理。
- BGM/环境音复用编辑态已有的公开 PlayableGraph 音频输出方式，在正式 GameLauncher.AudioHost 下持有私有输出；Voice/SFX 继续使用 EmberAudioManager 句柄。没有使用 Manager 中尚未实现的 fadeDuration。音量动作复用 NovelActionHandle/TickActions/WaitActions，双轨淡化、接管、嵌套暂停、倍率、快进与释放统一由会话驱动。剧情音量与玩家偏好叠乘。
- Schema 5 同步更新 Runner 和 NovelSaveStore；保存持续效果配置、循环音目标音量与存在性，停止中的轨不保存，CrossFade 只留目标曲目；旧 schema 的 BgmKey 迁移。恢复准备先校验/加载，不播放声音，提交时才创建实例；不恢复一次性粒子、SFX、Voice、粒子相位或采样位置。
- 节点试播继续使用正式 NovelSession/舞台接口，增加有限音量动作的末尾等待，持续生命周期不进入等待列表。结束保留气氛，停止/重播/关闭/重编译沿原生命周期释放。没有新增测试按钮或剧情段落。
- LastLight 79 条原 Say 记录逐块保持，218 条指令、70 个有限动作标识（含原 BGM 与新增音量动作）。正文新增光点/风声/录音按键闪点/音量变化/入夜交叉渐变/离场淡出。两个原创合成 WAV 已落盘。

资源导入边界：`NarrativeE3Assets` 在 Unity 编译加载后，使用 AssetDatabase / PrefabUtility 创建缺失的 `lastlight_motes`、`lastlight_spark`、`rain` Prefab，并调用既有配表中心 EmberTablePipeline.BakeCurrent 导出音频源表新增行。已有同名 Prefab 不覆盖。不手改生成 bytes。**本会话没有执行或确认这一步，不能声称三个 Prefab 和新键已在 Unity 导入成功。** 首批只支持单根 Local XY 有色矩形粒子，不支持纹理图集/材质/3D 网格/子发射器/拖尾/碰撞/脚本，自带音源禁止。

实际静态检查：`.utmp/visual-novel-e3/validate.py` 检查 LastLight YAML、79 条原对白记录一致、ID 唯一、等待先启动、源音频键与 WAV 可读、枚举追加顺序、C# 分隔符与 meta；报告为 `static-validation.json`。检查脚本一次因 Windows 默认 GBK 解码失败，显式 UTF-8 后复跑成功；这不是 Unity 测试失败或通过记录。

新增 `NovelMediaTests.cs`：12 个测试方法、17 个参数展开用例，**均尚未运行**。覆盖环境正常速度与倍率、暂停、接管与释放、双轨中间音量、稳定点最终值和仅持续状态恢复、换场保留两例、加载中退出/迟到结果/缺资源、快进不重放一次性、编辑态末尾不阻塞、环境音量/停止延迟、正式页 16:9/4:3 粒子几何与重复销毁、人物退场清理、4 类损坏持续快照、Schema 4 音乐迁移及 Schema 5 槽位写读。测试定义数量不是通过计数，未生成 E3 测试 XML 或渲染帧。

当前 Unity MCP 未连接或不可用，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。未使用 Editor.log、进程轮询、BatchMode 或 dotnet build 替代验证。

待验收：

1. Unity 编译并完成上述三个粒子 Prefab 的幂等导入和配表中心导出；运行“校验剧情”，确认资源存在与类型正确。
2. 运行 NovelSessionTests 的 E3 开头用例，回归现有 Narrative、存档/阅读和节点试播测试；保留实际 XML。之前“用户确认通过”的 E0–E2/编辑工具证据不覆盖本批，也不推定全项目 482/482。
3. LastLight 两路线和编辑态播放节点：检查光点/短促闪点的开始、中间、结束画面，人物绑定随移动，16:9/4:3、真实听音、暂停恢复无叠轨、倍率音高不变、存读档不重放、连续重播/停止/关窗/重编译无粒子/音源残留。当前没有本批画面/听音实测证据。
4. 本轮到此保留工作区；封版、Git、框架发布和消费项目部署均未执行。E3 验收后可继续 E4；E5 仍仅提前完成单节点试播子项，其他 E5/E6 与平台待办不变。


### E3 用户验收通过与文档收束（2026-09-22）

用户明确反馈“全部成功，收口文档，然后开始 E4”。据此将 E3 数据、运行、编辑、示例与测试交付标记为用户验收通过；上节未运行和导入待确认是首次交付时的历史边界，由本次用户反馈收束。没有收到修复后 XML、复跑范围或总数，不声称全项目 482/482，也不把反馈扩大为逐平台、Player 或每个分辨率的独立验收证据。

README、路线图、创作指南、效果配置与示例索引已同步。封存仍为 visual-novel 0.5.0 / preview，父模板 base 0.6.4；工作区增量尚未封版。本轮继续 E4，不执行 SaveTemplate/Bump、Git 提交、框架发布或 Call Me Heartless 部署。


### E4 文字与对白表现（2026-09-22）

在 E3 用户验收通过后实施。业务改动全部位于 Assets；复用既有会话、阅读暂停/倍率、配音租约、稳定点及 EUI 中心生成的 Dialogue/Body/Speaker。没有另建 Manager、正式 UI 骨架、Binding 或表导出产物。

- **数据与编辑**：Say 的 TextMode=Dialogue/Title/FullScreen；TextBeats 按 Unicode 标量位置配置 At/Pause/Speed/Instant。DialogueVisibility=27 追加至枚举末尾。图编辑器和 SO 内容 Inspector 暴露字段、范围说明与字数；新增指令重置新字段，复制保留节奏。校验拒绝乱序、重复、越界和非有限数。
- **时序与阅读**：文字时钟按节奏边界消费剩余时间；暂停冻结，1X/2X/3X 作用于文字与停顿。推进先补全当前页并越过剩余停顿，再翻页；同帧不能两次翻页。只有已读快进可以跳过整句全部页，遇未读恢复手动。中间页自动停留期间配音继续，末页仍等待整句配音完成，不重复播放 Voice。
- **页面和历史**：纯文本正文与节奏数据分离；现有 TMP 字符索引映射至 Unicode 标量，页边界随字号/区域重排。标题居中、旁白扩展至安全区，普通对白恢复原始锚点/对齐/背景与姓名分隔线。保留现有底部推进入口。历史只在整句完成时追加一次完整正文；正文中的尖括号在历史格式层按字面转义，不作为控制标记执行。
- **剧情显隐**：只隐藏对白层，演出继续。新 Say、选项、结束/故障/取消恢复剧情可见状态。玩家手动隐藏继续持有独立暂停，恢复时消耗一次输入。显隐等待期间不产生稳定存档点。
- **恢复与清理**：继续 Schema 5；仅整句末页/选择点可保存。TextMode 和节奏写入有条件的 E4 语义指纹；没有 E4 数据的旧剧情保持旧指纹。读档直接重排并显示当前句末页，不恢复句内计时、不重播配音、不重复历史。重开/退出沿原会话释放链路，页面还原布局，无新增对象/租约。LastLight 本批语义变化会使旧内容存档被现有兼容校验拒绝并保留文件，不伪装兼容。
- **编辑态**：“播放节点”使用同一 INovelTextView 和文字时钟；分页、显隐、模式、自动/手动、暂停/倍率都复用正式逻辑。停止/关闭/重编译沿现有 Dispose 清理。静态布局预览标明不模拟新文字模式，避免将剧情运行布局写回布局草稿。
- **示例**：220 条指令，原 79 句所有旧字段和先后顺序不变，另加 1 张“最后一盏灯”章节卡和 1 条入夜隐藏。4 句全屏旁白、8 个结构化节奏点；分支、变量、Next 与原 Voice 均保留。未另加测试段落或按钮。

验证证据：`.utmp/visual-novel-e4/validate.py` 已执行，`static-validation.json` 记录 YAML、原指令字段/顺序、79 条原 Say、连接/Voice、节奏范围、动作等待引用、枚举追加、105 个 C# 文件分隔符和 meta 检查通过。静态检查不是编译或测试执行。工作区已有修改保留，未编辑 Templates~、ParentSnapshot~、hash、生成 Binding 或 bytes。

新增 `NovelTextTests.cs` 15 个方法、22 个展开用例，**尚未运行**。覆盖分帧时序、Unicode、非法节奏、分页和同帧输入、历史稳定点、暂停倍率、自动与配音、已读快进、剧情/玩家隐藏、退出清理、不同页容量恢复、语义与编辑副本、历史字面文本，以及正式页复用的 16:9/4:3 长文分页/字号改变重测/模式还原/推进入口与 Prefab 不变性。数量是测试定义数，不是通过数；本轮未生成 Unity XML 或渲染帧。

Unity MCP 未连接或不可用，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。

待 Unity 验收：

1. 编译后运行 NovelSessionTests 中 E4 开头用例，回归 Narrative、存档/阅读、E3 媒体及节点试播；保留实际 XML。
2. 沿 LastLight intro 开场卡 → 全屏雨后台词 → 普通对白 → 手机消息即时片段，测试手动补全、自动、暂停、倍率和已读快进。
3. 在 common 入夜隐藏期间打开/关闭菜单，确认演出继续/暂停各自正确；下一句自动恢复。末句全屏旁白完成后存读档，确认末页完整显示且没有重播配音/重复历史。
4. 16:9/4:3 与小/中/大字号检查正文分页、原推进入口、标题/旁白背景、姓名分隔线恢复；“播放节点”在停顿、翻页、隐藏和结束状态停止/重播/关窗，确认舞台与音频按原链路清理。

未封版、未 Git 提交、未发布或部署。E3 确认不覆盖本批；平台、Player 与消费项目待办继续保留。



### E4 测试通过与文档收束（2026-09-22）

用户明确反馈“测试也通过了，收束文档然后开始 E5”。据此登记 E4 测试获用户确认通过，收束 README、路线图、创作指南、配置手册、示例索引与布局说明。上节“尚未运行”是初次交付时的历史记录，不覆盖本次后续确认。未收到测试 XML、复跑范围和总数，不推定新增 22/22 或全项目 482/482；没有将反馈扩展成每种分辨率、逐项人工观感、Player 或消费项目验收。

封存仍为 visual-novel 0.5.0 / preview、父模板 base 0.6.4；E1–E4 工作区增量未封版。本轮开始 E5，在既有单节点试播基础上补镜头、转场与制作预设，不提交 Git、发布框架或部署消费项目。



### E5 镜头、擦除与演出预设（2026-09-22）

> 以下保留初次交付时的验证边界；后续用户已确认“全部通过”，最新结论见文末收束记录。

本批数据、运行、编辑器、LastLight 示例和测试定义已落盘至 Assets；尚未完成 Unity 编译、测试执行及观感验收。E4 用户“测试也通过了”的反馈已单独收束，不扩展到 E5。

- 数据追加 Camera=28、Wipe=29，保留旧枚举；新字段只在对应指令参与 E5 指纹。运行复用原会话动作 ID、时钟、等待、接管及资源租约。镜头为独立舞台属性，与人物动作、舞台震动和媒体叠加。
- 正式页缓存作者布局，统一变换背景、人物与粒子，对白和菜单保持稳定；擦除沿用双图通道，加载/延迟保留旧图，完成/取消恢复 Image 类型并释放旧资源。
- Schema 6 保存镜头稳定值；旧 Schema 1–5 缺省中性镜头，现有指纹与槽位校验保留。未完成动作捕获最终值，不修改现场，不在恢复时重播擦除或镜头动画。存档槽支持同步升至 6。
- 预设窗口将进场/受击/回忆/复位参数展开成普通指令，生成唯一 ID 和等待组，支持编辑模式门禁与 Undo。现有单节点试播直接使用正式 Camera/Wipe 执行逻辑，末尾收束有限动作；停止、关窗、重编译仍沿用原资源清理路径。
- LastLight 增加 10 条演出，共 230 指令/80 Say；四段首复位、回忆推近/暖色遮罩、信件平移、夜色擦除。旧 80 条 Say 及其字段、配音指令、分支与结局不变；夜色既有指令仅改变转场种类并追加方向。未新增素材或配表。
- 滤镜评估：当前 Overlay UI 不直接适用场景 Cinemachine 后处理，缺少目标平台性能证据；模糊/灰阶/真正色调暂缓。暖色遮罩不宣称为滤镜；不引入新的管理器或渲染依赖。

验证记录：

1. `NovelCameraTests` 新增 11 个方法、19 个展开用例定义（未执行），覆盖镜头时序、暂停/倍率、人物组合、同通道接管、稳定点/旧 Schema/损坏恢复、擦除加载/失败/回收、交叉淡化接管/快进、预设参数/等待、节点末尾收束、正式页 16:9/4:3 变换与复位、指纹与边界测试定义。现有媒体槽位测试扩展至 Schema 6 镜头+声音往返。
2. 静态脚本 `.utmp/visual-novel-e5/validate.py` 核对旧枚举、指令/动作 ID、等待引用、LastLight 原指令顺序、80 条 Say 与配音不变、剧情连接不变、资源键、代码括号、元文件和本地文档链接；输出 `static-validation.json`。静态核对通过：230 指令、80 条 Say 保留，110 个 C# 文件括号/枚举引用/元文件检查、95 条本地文档链接检查。计数仅为静态覆盖，未执行 C#；这不是 Unity 编译或测试通过报告。
3. 当前可用工具目录无 Unity MCP，未用 BatchMode、dotnet、生成工程或 Editor.log 替代。没有新 XML/运行计数/性能数据。手动验证入口：Unity 编译 → `NovelCameraTests` 与既有 Narrative/NovelSave/节点试播回归 → 两条 LastLight 路线 → 暂停/快进/存读档/重开 → 关闭/重编译资源清理和多画幅观感。
4. 本批未新增正式 UI 骨架、Binding 或配表生成物，未触碰 Templates~、ParentSnapshot~ 或 hash；既有工作区修改保留。未 SaveTemplate/Bump、Git 提交、发布 tag 或部署消费项目。封存版本仍 visual-novel 0.5.0 / preview，父模板 base 0.6.4。

当前 Unity MCP 未连接或不可用，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。


### E5 用户验收通过与文档收束（2026-09-22）

用户明确反馈“全部通过，收束文档”。据此将 E5 本批镜头、背景擦除、演出预设、稳定状态恢复、编辑态试播及 LastLight 示例登记为用户确认通过，并同步收束 README、路线图、配置手册、创作指南、示例索引与布局说明。该确认独立于 E4；上节 MCP 不可用、尚未执行的描述保留为初次交付历史，不覆盖本次后续反馈。

证据为用户反馈，未提供测试 XML、复跑范围和总数。11 个方法/19 个展开用例仍为源码定义数量，不推定 19/19 或全项目 482/482；没有代理重跑的新报告，也不扩大为逐项听音、逐画幅渲染、移动端、Player 或消费项目验收。

范围保持不变：模糊、灰阶和真正色调滤镜继续暂缓；共享预设资产库、多节点时间轴及 E6 不在本批。LastLight 保持 230 指令/80 Say。后续平台和消费项目待办继续保留。

本轮仅修改 Assets 中的文档并核对本地链接/状态一致性，无运行代码或资源改动，不重跑 Unity 编译和测试。封存仍为 visual-novel 0.5.0 / preview、父模板 base 0.6.4；E1–E5 工作区增量尚未封版。未修改模板快照或 hash，未提交 Git、发布 tag 或部署，也未启动下一批实施。

### 0.6.0 基础框架收尾与封存（2026-09-22）

用户授权将未完成能力归档为未来拓展，并执行版本封存。E0–E5 已交付实现和用户通过反馈保留；未实现滤镜、更多转场、E6 动态立绘/眨眼口型/视频及共享预设/多节点时间轴按需重启，详见 PresentationRoadmap 的未来拓展清单。目标平台验收和消费端验证仍需在实际交付时完成，不计为已通过。

封存目标为 visual-novel **0.6.0 / preview**（本批为新增能力次版本），父模板保持 base **0.6.4**，框架兼容声明保持 **0.13.0**。不因基础功能收尾直接宣称全平台 stable。正式执行仅调用项目中心使用的 SaveTemplate 与 BumpTemplateVersion，完整核对五个受管目录的差异、资源元数据和编辑记录；不得手改 Templates~、ParentSnapshot~ 或 hash。实际封存版本与校验 hash 以 template.json 和项目中心返回结果为准，机器回执另存于忽略目录 `.utmp/visual-novel-closeout/`，避免将 hash 写入被计算 hash 的文档。

用户启动 MCP 后，已通过其配置中的本地 HTTP MCP 服务连接 ember-unity-framework 实例，确认项目目录一致、编辑器空闲且当前 Console 无 error 条目。本轮没有代码改动，也没有重新运行全量测试；此前“全部通过”仍是用户反馈，不推定 19/19 或 482/482。Git 提交、框架 tag 发布和消费项目部署不随本次模板封存执行。

封版前正式检查：项目中心 Scan 返回 **0 错误、3 警告、155 项差异**，业务资源/.meta/GUID、配表源与生成物一致性、15 个 UI Prefab 生成链路检查通过。155 项差异均核对为本批视觉小说/制作工具增量，包含 Narrative.E2 与 Narrative.UI 日志标签。三个警告为 MainState/GameplayState/SettingsState 基础类型场景映射提示，早期封版已有同类记录；本批不改变实际 Game 派生状态接线，保留提示，不宣称零警告。

封存操作：在正确编辑副本与父基线下，调用项目中心正式 API 保存业务层，再执行次版本 Bump（0.5.0 → 0.6.0），保留 preview 与 base 0.6.4。保存前确认无未保存场景、无编译/导入/Play 状态且差异路径未超出审阅清单；保存后核对模板实际内容 hash、封存 hash、当前编辑记录和父基线。ParentSnapshot 不在本次修改范围。完整验证回执由 MCP 返回并另存，不在此预填新自动测试数量。

本次没有 C# 实现变更或新测试运行；用户验收反馈与本轮 MCP 项目校验分别记录。未来消费端交付必须包含本工作区新增音频等 API 的框架版本，不能把本地模板封存当作已发布框架包或已部署消费项目。未来拓展清单不阻塞基础框架收尾，但目标平台与 Player 验收仍按实际交付范围补齐。


## 0.8.0 二级步骤、章节卡过渡与兼容修复（2026-09-24）

- 章节卡支持文字渐显/打字机/立即显示、进入和退出时长、速度倍率和缓动；点击先补全未完成文字，再整体淡出后推进。暂停冻结，重复点击不重复推进，退出期间不能创建稳定存档。
- 新增 HideAllCharacters，覆盖自由位置的人物并清理人物动作/绑定效果。彩色二级步骤保留基础命令，可折叠、展开、复制、解包、删除；自定义预设插入为独立快照，重建命令/台词/动作 ID 和内部等待引用，拒绝不完整的外部等待。
- LastLight 四段共 15 个分组，80 条 Say 的文本/说话人/ID/资源和图连接不变。开场三槽隐藏合并为单个即时隐藏所有立绘。新增隐藏所有立绘、恢复舞台、回忆氛围三份可复用预设。备份与差异报告在本地 `.utmp/vn-step-groups`。
- 导入、演出优化、导出三个模板技能支持二级步骤及文字过渡；最低业务模板版本 0.8.0，不能向未升级的旧业务写入新类型。
- 用户提供的 TestResults_20260924_170905.xml 共 645 项，其中 35 项失败。根因是旧命令的新增文字参数在反序列化后为零，触发 BadText 并造成后续加载失败；使用序列化版本与默认值迁移修复，保留新配置非法值的校验。剩余页面用例改为先完成章节卡退出，再验证阅读控制。
- Unity MCP 恢复后编译错误为零；156 项运行/剧情/存档用例、8 项正式 Prefab 布局/透明度用例通过。13 项实际页面/观察用例首轮 12 项通过，修复旧层级假设后最后 1 项单独复测通过，相关用例合计 177 项。跨 Play Mode 回调会丢失 MCP job 进度，以项目 NarrativeTestReport 落盘 XML 为最终结果。证据：`.utmp/visual-novel-m3/tests-20260924-091422231.xml`、`tests-20260924-091807200.xml`、`tests-20260924-092005330.xml`、`tests-20260924-092051840.xml`。
- 未重新运行全部 645 项框架测试，未发布框架 tag，也未部署/验收消费项目。

## 0.9.1 UI 皮肤与配表多语言（2026-09-25，工作副本未封存）

- 新增配表多语言：`novel_languages` / `novel_content_text`（内容）/ `novel_ui_text`（界面）；源语言列固定 `zh_Hans`，暂定简体、繁体、日语、英语，可继续扩展。回退链为目标语言列 → 源语言列 → 资产原文，因此缺翻译显示源语言而不是空白，Key 查不到也不报错。
- 字段新增：Say、选项/分流、章节出口路线加 `_textKey`，选择提示加 `_promptTextKey`，章节名与剧情名加 `_displayNameKey`。角色名走内容表的 `character.〈角色键〉`，未命中回退 `novel_characters.displayName`（该表结构未改，消费项目零迁移）。
- 正文替换放在 runner 的运行期文本副本上（变量绑定之后、`NovelValidator` 之后），校验始终面对原文，所以译文长短不同不会触发 `TextBeats` 越界；Key 不进指纹，切语言与补译文都不会让既有存档失效。选项、提示、章节名没有长度校验，在读定义时替换。
- UI 文本由 `TMPEx`（`Ember.UIExtension`）承接：TMP 的 Inspector 右上角三点菜单「替换为 TMPEx（多语言文本）」，沿用既有 `EUIComponentReplaceMenu` 延迟替换机制，字体/材质/对齐等原设置全部保留；编辑期不改写文本（带 `ExecuteAlways`，保住所见即所得），未装配配表时行为与普通 TMP 完全一致。
- 新增 UI 皮肤：`novel_skins` / `novel_skin_sprites` / `novel_story_skin`。覆盖行按「页面 + EUI 绑定控件 + 相对节点」寻址，运行期在页面绑定完成后按名换图，不修改任何 Prefab、Binding 或布局；按小说赋值，主界面与阅读页共用同一套解析。本期只换图片，位置与大小仍由各小说在布局窗口调整。
- 编辑器：节点与章节 Inspector 顶部、流程窗口工具栏各提供全局语言切换；对白、选项、提示、章节名四处新增多语言 Key 输入与逐语言预览（命中显示译文，未命中显示「缺条目 → 回退：原文」）。菜单 `Ember/视觉小说/导出皮肤可覆盖图片清单` 扫描全部页面 Prefab（含主界面），列出可覆盖图片并输出候选 CSV 行到剪贴板；布局窗口的外观区另提供「把此元素加入皮肤清单（复制 CSV 行）」，单个元素一键取行。两者共用同一套寻址实现（`NovelSkinImageCatalog.TryDescribe`），不会分叉。
- 编辑器内的三个预览入口（流程窗口、节点试播、布局窗口节点预览）在建立配表后同样装配多语言与皮肤，试播画面与运行期一致；已实测该装配方式下语言解析与按小说解析皮肤均生效。
- 导入/导出技能同步：新增 `references/localization.md` 作为 Key 与配表的唯一口径；导入时写 Key 并在 `novel_content_text` 追加行（只填 `zh_Hans`，不覆盖已交付译文）；导出新增「多语言Key」列。`catalog.json` 的 `minimumTemplateVersion` 待随本次 Bump 一并更新。
- 实测：新增 8 项多语言/皮肤用例 8/8 通过；指纹、定义读取、变量逻辑、图模型等高风险既有类 82/82 通过；`NovelSessionTests` 137 项中 134 项通过。失败 3 项均为既有 schema 断言（`NovelCheckpoint.cs` 的 `SchemaVersion = 7` 与两处硬编码 6 的断言），这三个文件与模板快照逐字节一致且未被本次修改。
- 指纹回归：`E0StoryFixture` 与 `M1StoryFixture` 的 `Fingerprint` 与改动前基线逐字节一致（`7D8B67D3…3FC32`、`FFBCEEA6…26AFB`），装配多语言并切换三种语言后仍一致。
- 皮肤实测：演示皮肤 `lastlight_alt` 在阅读页 `History/SkinIcon` 与主界面 `NovelBackdrop` 各命中 1 并真的换图；无覆盖行的 `lastlight_default` 命中 0（保持 Prefab 外观）。已知约束：覆盖值必须是项目 `Resources` 下的图片，Unity 内置 `UISprite` 无法用路径表示，只能当被替换方。
- 未验证：编辑器面板的实际渲染（Key 行与语言下拉的排版、交互）与运行期皮肤在真实页面上的画面效果，需要人工或 Play Mode 确认；`NarrativeLibraryTests.RenamingAndMovingStoryPreservesEntryAndStableLookup` 需要编辑器前台焦点，本轮未跑通；`NovelGameplayTests` 重场景用例在本环境下不稳定（改动前同样如此）。
- 未运行框架全量回归，未发布框架 tag，未部署或验收消费项目，模板尚未保存与 Bump。

## 0.10.0 说话人临时称呼（2026-09-26）

- 新增 Say 字段 `_speakerNameKey`（「说话人称呼 Key（留空用角色名）」）：只为这一句覆盖**姓名框显示**，
  用于「主角先遇到一个人、后面才知道名字」——揭晓前显示 `？？？`，揭晓后留空即显示真名。
  构造参数追加在参数列表末尾，`WithResolvedText` 的 `MemberwiseClone` 自然继承，`JsonUtility` 往返保留。
- 显示名解析收敛为一条链：`_speakerNameKey` 命中内容表 → `character.〈角色键〉` → `novel_characters.displayName`，
  共用入口 `NovelLocalization.SpeakerName(characterId, speakerNameKey, displayName)`。运行期当前句
  （`NovelSession.Render`）、历史解析（`NovelSession.Reading.ResolveSpeaker`）、运行期摘要
  （`NarrativeContentGUI.Summary`）与布局窗口节点预览（`NovelGameplayLayoutWindow.ApplyNodePreview`
  写 Speaker 文本处）四处都走它，不再各写一份回退规则。覆盖 Key 留空或查不到时按原回退链继续，不显示空白、不报错；
  编辑器里该字段复用现成的 `LocalizationKeyField`，逐语言预览未命中时显示「缺条目 → 回退：〈角色名〉」。
- **不改进存档指纹**：称呼 Key 是表现层字段，与 `_text` 同类，不写进 `NovelCompatibility.Fingerprint`，
  因此给既有剧情补称呼 Key 不会让玩家已读存档被判「剧情语义已变化」。新增用例锁定「只改称呼 Key
  时指纹逐字节一致」。
- 历史与存档：`NovelHistoryEntry` 增加 `SpeakerNameKey`，`RecordStableLine` 写入、`TryCapture` 复制、
  `History` 解析优先使用它，所以回看揭晓前的旧句仍是 `？？？`，不会因为后面说了真名而串味。
  旧档没有这个字段，反序列化后为空、解析退回角色名回退链，行为与改动前完全一致；恢复校验仍只做
  结构与规模检查，`SchemaVersion` 保持 **7**，没有版本迁移分支。
- 配表：内容表新增 Key 家族 `speaker.〈语义〉`，模板已提供 `speaker.unknown`（`？？？` / `？？？` / `？？？` / `???`，
  `novel_content_text` 与烘焙产物已同步）。多语言 Key 表、回退链与「揭晓前显示占位名」写法写入
  `references/localization.md` 第 2、4、4.1 节，导入技能的五列映射补「临时称呼 → 真实角色键 + 称呼 Key」，
  导出技能明确 `说话人` 列写真实角色名、称呼 Key 附注而不回写 `？？？`。
- 编辑器还顺手保证新建指令会清空该字段（`NarrativeGraphModel.AddItem`），不会从被复制的上一条指令继承。
- 实测：新增 5 项 `NovelSpeakerNameTests` 用例 **5/5 通过**（覆盖 Key 命中显示译文、Key 未命中回退角色名、
  只改称呼 Key 时指纹不变、历史带 Key 且旧档缺字段仍能恢复、Emphasis Auto 仍按真实角色键匹配）。
  过滤掉需要编辑器前台焦点的历史用例后，`Game.Narrative.Tests` 相关 11 个类 **228/228 通过**
  （含 `NovelSessionTests` 全部分片即 NovelLocalization / NovelText / NovelReading / NovelCheckpoint /
  NovelActor / NovelCamera / NovelScreen / NovelMedia / NovelStepPresentation / NovelVariableLogic、
  `NovelCheckpointTests`、`NarrativeRunnerTests`、`NarrativeStory*`、`NarrativeGraph*` 与
  `NarrativeAvailability/TemplateIdentity`）。既有指纹基线（E0 `7D8B67D3…`、M1 `FFBCEEA6…`）未变。
- 未验证：`NarrativeLibraryTests.RenamingAndMovingStoryPreservesEntryAndStableLookup` 需要编辑器前台焦点，
  本环境未跑通；`NovelGameplayTests` 重场景用例在本环境下无法启动（改动前同样如此）；
  未运行框架全量回归，未跑 Play Mode，未部署或验收消费项目。
- 已知边界：称呼 Key 是**跨角色**的语义 Key（`speaker.unknown` 谁都能用），不是「角色键的别名」；
  同一个临时称呼在同一部作品里应复用同一个 Key，语义不同才另开一个。译文与原文长度差异不影响它，
  因为它只替换姓名框，不参与正文分页与 `TextBeats` 校验。