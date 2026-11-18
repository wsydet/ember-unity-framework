# 20261117-soft-glass-ui-gm：柔光玻璃 UI 与 GM 控制中心

## H14 用户请求阶段归档（当前有效，验收结论仍采用A2）

2026-11-17，用户在A2复验后要求“执行收束归档吧”。本轮据此将当前有效记录与必要证据保存到docs/tasks/20261117-soft-glass-ui-gm/，以该目录README.md、PENDING.md及archive-verification.json记录归档范围和校验结果。归档保存当前阶段，不把该请求解释为放弃失败项或设备/模板待办。

状态继续为“未完成，SG-IA-02隐藏/重显子项需实施返工”；SG-IA-01已通过，AC03通过、AC04失败、AC09/AC11待完成。正式目录保留完整原始快照及可直接阅读的当前记录；临时任务目录继续保留供后续返工，不执行删除。当前产品基线仍按A1-RW1 manifest核对；没有产品修改、模板操作、提交发布或新测试。

## H13 A1-RW1独立复验 A2（当前有效）

2026-11-17 17:48 Asia/Shanghai，由原独立聊天完成A2，详见[acceptance.md](acceptance.md)顶部。**SG-IA-01通过；SG-IA-02原穿透修复通过，但双Popup隐藏/重显子项失败，保留该ID未关闭。AC04仍失败，任务未完成。**

- 本轮19/19相关EditMode（job43f5d72aecf0448a8979f958e009329a，1.3323154秒）；审阅后独立重跑实施40项运行矩阵，40/40。另写15项独立补充探针暴露隐藏缺口，再以独立起始selection和不隐藏对照隔离确认，证据在evidence/reacceptance-a1/。
- SG-IA-01：旧lease disposed、新局部lease ready/未disposed且Graphic无旧引用；借用无Dispose权、迟到撤销、回池/释放测试和资源归零通过。
- SG-IA-02剩余P2子场景：双Popup隐藏上层30帧后，下层仍Paused/不可交互，selection=null；交叉隐藏/重显两层后依次关闭，原BackgroundMode selection丢失。hide-chain-repro.json、cross-hide-return-repro.json和03-top-hidden-dead-focus.png给出稳定复现。另3项补充false是焦点已丢失的连锁结果，孤立外部停用/重启和隐藏下层销毁均通过，不扩报为额外缺陷。
- 本轮MCP显式编译开始1794908498496、完成1794908501285、域重载1794908512356；实际UI MVID aad4d568-81c2-43f1-87df-31511fd34c11。最终Console空、Play=false、场景未脏。关闭消费者60帧捕获10900→10900，0边界/RT/段/专用材质。
- 890份产品文件与返工manifest零漂移，变化仍只有相对R2的8旧+4新；manifest SHA256 816A14A2C661BBF226BCC3515C67765A41AEB9569CC1B808B3248507733DDA33。原R2五图零漂移，AC13继续有效，Git状态前后无变化。验收没有修改产品，未保存模板/提交发布/归档清理。
- 当前AC03在Editor范围通过，AC04失败；AC09设备体验与AC11base正式保存/分发继续待办，其他A1当前范围结论沿用。H12的实施自检不等同独立整体验收通过。

下一阶段：实施修复SG-IA-02隐藏子项，按acceptance.md复验矩阵完成相关短验证再交本独立聊天。不得简单恢复全部业务交互标志；保护原本禁用根、GM更高层焦点、NoMask逻辑门禁和背景渲染。原范围修复已授权，无需重复申请；模板操作/设备/全量长测授权未扩展。保留A1/A2原始失败证据和全部任务材料，不进入入口归档清理。

## H12 A1 定向返工 A1-RW1（实施交付历史，由H13复验更新）

2026-11-17，本实施聊天按用户明确授权沿用当前 checkout 修复 SG-IA-02/P1 和 SG-IA-01/P2。阶段仍为实施自检，待原独立聊天01a1e8b5-afdf-7ed3-b93a-e3b89435a271复验；不代签 A1 通过，不归档清理。需求 R4、R2 用户视觉确认和未完成边界保持。

- 焦点：模态 mask 建立 EventSystem 作用域，TMP 输入框实际失焦，低层 Selectable 和页面返回键被阻挡，画面继续渲染。默认可用上层控件取得焦点；关闭/隐藏/重显/销毁及双弹窗乱序退出修复返回链。保留 UseMask=false/逻辑拒绝门禁；不抢用户转移到更高 GM 的焦点，不重新启用业务本来禁用的根。
- 租约：会话按引用撤销自身 Group 借用后释放，局部玻璃按自身配置恢复；迟到释放不撤销新借用，借用者不 Dispose 外部 lease；异常失效引用也能恢复。
- 最终编译/域重载完成，UI MVID aad4d568-81c2-43f1-87df-31511fd34c11；18/18 EditMode，job3faf0fa5330949a5bc8b336cbb92b418，1.2262788秒；40/40运行检查。最终关闭消费者60帧捕获4551→4551，0输出RT/边界/相机段/专用材质。Console无warning/error，Play=false，FrameworkScene未脏，GameView index5及圆点偏好原状态恢复。
- 证据和当前指纹：evidence/rework-a1/，以compile-delivery.json、tests-delivery.json、runtime.json、teardown.json、manifest.json为准。当前manifest SHA256为816A14A2C661BBF226BCC3515C67765A41AEB9569CC1B808B3248507733DDA33。原886文件初始零漂移，返工仅八份既有产品文件加四份新增代码/meta，现890份；其他产品无变化，R2五图未漂移。03/04-final实际图是功能修复现场，外观源未改，不重新要求已确认美术签收。
- 交接：[rework-a1.md](rework-a1.md)、[prompts/05-reacceptance-a1.md](prompts/05-reacceptance-a1.md)。保留原acceptance.md A1失败记录，由原独立聊天定向复验并更新。未自动发送消息或创建其他聊天。
- AC09真实触摸、旋转、安全区与键盘覆盖仍待设备；AC11 Assets base待回流，正式模板保存/分发未完成。未授权模板操作、提交发布、全量/长时测试的边界不变。未创建worktree/子代理，未覆盖其他任务改动。

## H11 独立验收 A1（返工前历史）

2026-11-17 17:02 Asia/Shanghai，独立聊天01a1e8b5-afdf-7ed3-b93a-e3b89435a271完成本轮核查与相关短验证，结论见[acceptance.md](acceptance.md)：**未通过整体验收，需实施定向返工，任务未完成**。未接管产品修改或入口归档。

- SG-IA-02 / P1：普通模态Popup打开后，下层已聚焦TMP输入框仍可接收GameView键盘字符，下层选中按钮也可被Submit触发；AC03/AC04失败。证据evidence/acceptance/issue02-repro.json及截图。
- SG-IA-01 / P2：Blur Popup设UseMask=false并RefreshBackdrop后，同页玻璃group保留disposed外部lease，变实色；重新开启mask才恢复。证据issue01-repro.json、before/after图；AC03/AC04失败。
- SG-PENDING-01 / AC11：base仍为Assets待回流草稿，正式中心保存/分发未完成且未授权。D1 S6要求此边界保持待办，不能整体标完成。
- SG-PENDING-02 / AC09：真实触摸/设备旋转、安全区与键盘覆盖体验仍待设备验证；没有延期/移出范围签收。
- AC13仍通过：R2用户确认有效，manifest SHA256 242518ABCD1CC0124A05BA3F3BE6C4C5CFBA8476ACC65B840DEC5EA727761F9A；独立开始/结束886份产品文件与5图均零漂移，Git状态零变化。
- 本轮MCP主动编译及域重载完成；13/13相关EditMode通过（job e6d6697f73d949888bc8c8ef4e6764b5），独立运行涵盖策略/门禁/输入/叠层/GM/静动态所有权。全部消费者关闭60帧捕获不增长，0RT/段/专用材质。最终Console无warning/error、Play=false、FrameworkScene未脏。见evidence/acceptance/。

下一阶段是实施定向修复SG-IA-02/01并复验，不是归档清理。原范围修复已有授权，不重复询问；正式模板操作、真机/构建/长测授权未扩展。修复后重新MCP编译、相关测试、更新实现指纹，按acceptance.md复验范围交回本独立聊天；视觉若变化则更新受影响预览与确认。未创建其他聊天、worktree或子代理，未向其他聊天发送消息，未保存模板/提交发布/归档删除。

## H10 独立验收已派发

2026-11-17，用户明确回复“可以，你来创建”，已据此创建独立验收聊天，threadId为01a1e8b5-afdf-7ed3-b93a-e3b89435a271，host为local，沿用本项目当前checkout，无新worktree，模型/推理配置使用用户默认值。初始消息为更新后的prompts/04-acceptance.md全文，承接R4/R2和已确认的AC13，不重新要求用户确认效果。

新聊天负责独立复核AC01—AC13、写acceptance.md及验收交接；本实施聊天停止产品修改，避免并发写同一基线。当前是“待独立验收结果”，不是验收通过或任务完成；模板保存/提交发布/全量长时测试授权没有扩展。H9中“尚未授权创建聊天”已被本节新授权取代。验收完成后再依实际结论进入归档清理。

## H9 收束请求与已确认状态（当前有效）

2026-11-17，用户在本实施聊天回复“可以进行收束了”，R2预览确认已登记于preview-confirmation.md。manifest SHA256为242518ABCD1CC0124A05BA3F3BE6C4C5CFBA8476ACC65B840DEC5EA727761F9A；886份产品文件预检无变化（evidence/r2/closure-preflight.json）。AC13用户视觉签收条件已满足。

当前实际缺口是独立验收：acceptance.md不存在。本聊天只实施与自检，不伪造独立验收。04提示词已更新为用户已确认R2，可直接交给另一聊天执行。用户仅授权收束，尚未明确要求创建/派发新聊天；未擅自创建或发送消息。收束准备索引closure-preparation.md记录具体资料和门槛。现阶段保留临时目录，未标整个任务完成、未清理，未保存base模板或提交发布。H8及以下“R2待用户确认”为历史，按本节和确认记录为准。

## H8 / R2 当前交接（优先于H7及以下历史）

用户对R1要求四点返工，需求更新R4/F01—F04，原图保存在references/r2-feedback。已完成无滑块Slider、原FPSDisplay入口、Gloss/Frost玻璃及紧凑横竖GM布局；具体改动、失败覆盖修正与实际数据见revision-r2.md。R2 evidence/r2：13/13 EditMode、23项原回归、7项专项、portrait样本完整与四组短测均有新结果。5张新图在previews/r2，R2 manifest对应新文件指纹；R1没有确认，R2待用户明确确认。仍无独立acceptance.md、无base保存/分发、无提交/发布/归档清理。下一聊天按04和R4独立核对，不能复用R1视觉签收或把实施自检当独立验收。

## H7 实施交付摘要（优先于下文入口历史状态）

- 实际交付、自检范围、偏差及AC映射见implementation.md；中间过程另存implementation-progress-history.md。
- 24中心Prefab/22 Binding/5注册、独立SoftGlass库、core/URP17后端、GM/入口/消费扩展示例均已落盘。旧GM GUID/路径/注册保留。
- 13/13 EditMode（tests-final.json）、23项运行检查（runtime-validation-final.json）、四组短测（performance.json）、GPU裁切/透明探针及真实退出动作均有原始证据；不作为另一聊天独立验收结论。
- 独立采样边界必须root Canvas；嵌套共享。失败嵌套路线已移除，示例已制作背景由Attachment借出/归还。720窄屏改340DU最小列宽单列滚动；过早于启动主页面打开的诊断图不用于预览。
- 最终编译/域重载、runtime/editor MVID、Console及未脏场景记录在各final JSON。GameView恢复原index5，两个任务自建尺寸移除；测试圆点位置键已清除恢复初始默认。字体缓存恢复S0指纹。
- R1共11张实际图在evidence/previews/r1；manifest记录当前文件与图像SHA256。preview-confirmation.md状态为待用户明确确认。当前实施聊天最终回复展示控件/Popup/GM/收起入口，不得代签AC13。
- 框架文档在Packages/com.ember/Documentation~/ui；base说明Assets/Game/Documentation/base/soft-glass-ui.md。Assets仍base待回流草稿，未切换/保存/Bump/同步/分发，无模板正式操作授权。
- 下一阶段使用04，由另一聊天独立复核，重点包含自定义Logic拒绝mask、输入焦点/真实触摸、安全区和平台/性能限制。无acceptance.md，整个任务未完成，未归档清理/提交发布。

## 身份、阶段与有效资料

- 修订/日期/时区：H8 / 2026-11-17 / Asia/Shanghai。
- 实施聊天已按用户授权完成原范围代码、24项中心资源和短定向自检，实际预览R2待用户明确确认。沿用当前checkout，无worktree/子代理/提交发布。当前权威状态见 implementation.md 与本文件H7增量；下文入口/方案阶段记录作为历史保留。
- S0：993 个当前文件已真实复制备份（11,343,546 字节），见 `evidence/baseline-implementation/files/` 与 manifest.json；Assets/模板 41,338 个文件另留 SHA256。与方案 962 个 UI 基线指纹差异为 0；场景 FrameworkScene 未脏。硬件 RTX 5060 Ti / Core Ultra 7 265KF / D3D12 / Linear，编辑器 Game View 2560×1440。旧 GM GUID `bba4cad8012e3f64690d24af5becf55d`。
- 入口对话：Codex `01a1e7c6-feb2-7f32-b229-ed1145d6c58b`，用户附八图并调用 ember-solution-design 的本次聊天。
- 所有者：本任务独占 `.utmp/ember-workflow/20261117-soft-glass-ui-gm/`。当前文件、prompts、references、baseline 全部由本任务建立；不得清理相邻任务。
- 项目：ember-unity-framework；当前阶段：实施交付与待预览确认；下一阶段可由另一聊天按04独立验收。没有acceptance.md，整个任务未完成。
- 方案聊天：`01a1e7dc-fd58-71f0-9534-c5ee10d1febd`，host `local`，2026-11-17 按更新后的 02-design 全文创建；本聊天已产出 D1 并更新实施交接，不自行启动后续聊天。
- 正式归档预定：`docs/tasks/20261117-soft-glass-ui-gm/`。长期能力说明进入 `Packages/com.ember/Documentation~/ui/`；base 使用说明进入其正式 Assets 文档维护源，由方案落实。
- 有效需求：[requirements.md](requirements.md)，R4，R01—R08、AC01—AC13。
- 有效方案：[design.md](design.md)，D1，2026-11-17。实施记录：[implementation.md](implementation.md)为当前交付；历史过程另存implementation-progress-history.md。R1实际图见evidence/previews/r1/manifest.json，预览展示与回复见preview-confirmation.md，用户尚未确认。
- 参考图：[references/01.jpg](references/01.jpg) 至 [08.jpg](references/08.jpg)，均复制自用户本轮附件；映射见需求文件。
- 当前已加入框架采样机制/URP后端、遮罩策略和部分正式资产；未完成整个功能、自检、独立验收或用户成品预览确认。下文入口历史事实按 implementation.md 与本轮证据补充。

## 环境与实际基线

| 项目 | 实际值及证据 |
| --- | --- |
| 项目根 | 本机 `C:/Users/wuyu/My/ember-unity-framework`；文件内使用项目相对路径，以便接手 |
| Git | 分支 `codex/local-snapshot-20261111-162819`；HEAD `16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2` |
| 工作区 | 多项既有未暂存/未跟踪改动，本任务不拥有；完整清单见 [开始状态](baseline/git-status-before.txt)、[差异统计](baseline/git-diff-stat-before.txt)、[暂存统计](baseline/git-staged-stat-before.txt)。不能用 HEAD 代替此工作区。 |
| UI 现状指纹 | [ui-files-sha256.json](baseline/ui-files-sha256.json)：962 个框架 UI/UIExtension 与项目 UI 代码/资源文件的 SHA256；这是指纹而非可恢复备份。实施前备份真正待改的当前文件。 |
| Unity | `ProjectSettings/ProjectVersion.txt`：6000.5.4f1，revision d550df8bd089 |
| Ember | manifest 为 `file:com.ember`，lock 为 embedded，实际目录 `Packages/com.ember`；package.json 标称 0.25.5；存在未提交差异，不能描述为本任务已发布版本。 |
| URP | manifest/lock 为 17.5.0；当前 Graphics/Quality PC 指向 PC_RPAsset，PC_Renderer 仅 SSAO；FrameworkScene 为 Main Base + UICamera Overlay，UI layer 5。MobileRenderer 无 Feature。17.5 本地源码已废弃 Compatibility 关闭开关。均为只读核对，无运行探针；详见 design 第2节。 |
| 正式编辑模板 | `Assets/Editor/EmberEditingTemplate.json`：topdown-2d 0.1.2，hash `d83863b16bda8c2655f21d6aa3d55693`，loadedAt `2026-11-11 15:53:02`。只读核对 template.json 相同，父 base 0.10.2，父 hash `0b0431013fcd599ea1d8f1c27aafd169`。 |
| 扩展记录 | `ProjectSettings/EmberExtensions.topdown-2d.json`：schema 2，ownerVersion 0.1.1，显式 roots 为 fsm.blueprint / level.authoring / level.trial。它与正式编辑版本 0.1.2 不同；未调用正式求解器验证可用性，不可宣称有效启用，也不在入口修复。新 UI 扩展目前不存在。 |
| 模板字典 | 已读公共字典、活动 topdown 字典；Assets 下无 base/glossary.md。 |
| 模板操作 | 入口未加载、保存、Bump、同步或部署模板；相关操作授权与状态分别处理。 |

## 需求、决策与变更

有效 R/AC 全表位于 requirements.md，避免维护第二份需求正文。用户确认范围与图片含义、源码事实、暂定解释严格分开。

- 已确认：新旧 UI 并存、消费端可选扩展、柔光玻璃、统一新圆角、图 02 风格 GM、同风格入口；Popup 默认使用新模糊遮罩，纯色仍可显式选择，UseMask 语义保持。
- A01/A02 已由用户异步答复确认：GM 使用可拖动吸边悬浮小圆点，点击打开；首批为通用控件 + 公共弹窗 + GM，旧 UI 保留。不再等待这两项偏好答复。
- 架构初判：框架公共机制 + base 共用 UI/GM，方案阶段确认实际归属和交付路径。当前 topdown 编辑身份不代表 UI 应变为玩法专属。
- 来源未涉及 UnityFarm 现场；若后续从 UnityFarm 引入新增改动，先完整读 unityfarm-change-routing.md 并分流，不能借本任务直接改其 PackageCache。
- H2 变更：按用户后续原话“popup默认选择这种新的遮罩”，将新遮罩从可选开启改为默认使用；同步 R03、D01、AC01/AC03 及三个提示词。控件皮肤仍可选，避免把新遮罩默认值误解为全部控件强制换肤。无已批准延期；技术方案尚未形成。
- H3 变更：用户要求新建方案设计聊天按 02-design 接续；新增 R08/AC13，收束前必须展示对应当前实现的预览图并取得用户明确确认。当前确认状态：尚未提供实现预览，尚未确认，禁止收束归档清理。

## 已核对的源码事实与定位

| 文件/入口 | 事实及后续用途 |
| --- | --- |
| `Packages/com.ember/UI/Runtime/EUIBgMaskPool.cs` | 当前池化的 Image/Button/Canvas 遮罩；支持颜色、相机和排序，无已接入的背景模糊配置。 |
| `Packages/com.ember/UI/Runtime/EUIManager.cs` | PopupMaskColor 与 ShowBgMaskForPopup；使用 UseMask、MaskColorOverride、ClickMaskToClose/Logic.NotifyClickMask。新效果须兼容现有关闭/输入语义。 |
| `Packages/com.ember/UI/Runtime/EUIPage.cs`、`EUIViewEngine.cs` | 页面遮罩数据与池的调用、清理路径；需核对 BindingBridge/生成配置与资源所有权。 |
| `Packages/com.ember/UIExtension/Runtime/Behaviour/EUIRoundedImageModifier.cs` | 已有 Simple Image 网格圆角修改器，当前本地有改动；不适用于 Sliced/Tiled。不预定替换它。 |
| `Packages/com.ember/UIExtension/Runtime/Resources/UI-NodeBlurEffectShader.shader` | 仓库有旧三 pass 模糊 Shader；搜索所见 NodeBlur 调用在注释旧代码内。入口没有确认当前有效运行链路，不可据此声称可直接复用。 |
| `Assets/Game/UI/Runtime/Framework/GMPage.cs` 与 Binding | 现有面板切换、退出、时间倍率/框架时间同步、状态显示与增强 Toggle；代码有 EmberManaged 受管区。方案须定位真实生成维护源，不能只改生成结果导致刷新丢失。 |
| `Assets/GameResource/Resources/UI/Common/Prefabs/GMPage.prefab` | 旧 GM 资产必须保留可选路径；新旧注册、入口和逻辑复用需方案说明。 |
| `Assets/Game/UI/Runtime/Module/UIFoundation/` | 有现成提示、确认、动态列表、库预览、GM Item 等公共 UI，须盘点复用并明确首批对应清单。 |
| `Assets/GameResource/Resources/UI/Common/Prefabs/` | 有 Button/Slider/Switch/Input/Search/StandardPopup 等既有公共 Prefab；并存策略需维护旧 GUID/路径和绑定。 |

入口没有运行时探针、编译、测试或修改产品文件。以上是只读源码事实，不是设计结论或行为验证。

## 实际成果与未完成项

| 成果 | 状态 |
| --- | --- |
| requirements.md、handoff.md | 入口文档完成，A01/A02 已确认，默认遮罩变更已同步 |
| design.md | D1 方案已完成；框架/base/玩法归属、遮罩默认与迁移、分段采样、24项Prefab目标、GM行为、风险/质量与AC映射均已明确 |
| 三份 prompts | 02 已完成；03 已更新并具备开始 S0/S1 的方案前置；04 仍待实施，不可跳过实施独立验收 |
| references、baseline | 参考附件与现状指纹已保留；临时目录可能被 Git 忽略，未承诺跨机器同步 |
| 产品代码/Shader/Prefab/模板 | 全部待后续阶段，本轮没有写入 |
| 关键缺口 | 无需用户重复回答的需求缺口；URP分段接线需实施S1实证；正式base加载/保存与父同步仍需具体模板操作授权。建议性能阈值未经用户确认，真机环境未指定。 |

## 方案 D1 的确定结论与源码校正

- D01：新风格采用独立 EUI 公共库、Prefab 与运行时外观配置，本轮不新增正式 Ember 扩展 ID/hash，不改既有扩展组合。CommonSkin、GM选择与PopupMaskEffect相互独立；旧UI可继续使用新默认遮罩。
- D02：UI core 管线无关，URP17后端独立程序集；默认相机分段+RenderGraph实时采样，目标Popup整组之前捕获场景与下层UI，同组玻璃共享输出。普通Popup默认Blur，显式Solid或UseMask=false分别处理；不以仅场景纹理或透明Image代替。
- D03：圆弧圆角矩形统一曲线，短边720DU体系，半径12/20/32/48DU及明确Circle/Capsule；GM采用状态/时间卡+竖滑杆+圆快捷操作，圆点拖拽吸边。布局/材质参数是方案设计值，未作为用户视觉签收。
- D04：新Business GM/Items保留旧GM资产与调用；真实生成配置在Assets/Ember/Editor/SOs。进一步核对发现当前生成器普通重生保留现有.cs受管逻辑，仅首建骨架、覆盖Binding并同步可选标记。旧文件头“整块替换”不能作为当前生成行为证明；没有另一份活动GM业务代码模板。新GM不改旧受管区，也不把GM业务加进通用Framework.tpl。
- D05：现有Main Base+UICamera Overlay，UICamera layer5；PC_RPAsset/PC_Renderer，只有SSAO；MobileRenderer无Feature。URP17.5本地RenderGraphSettings兼容开关已废弃且false，不设计关闭RenderGraph。以上只读事实，不是运行验证。
- D06：新增UI目标共24个Prefab（2骨架/5具体Page/17Item），明细见design第6节，正式EUI中心制作，保留现有字体、本地按钮/圆角/资源加载/运行时扩展改动。
- D07：当前topdown不切换，允许包机制及活动Assets内base待回流集成草稿；不保存为topdown独有内容。正式分发须先保护草稿，按另行授权经中心加载base、合入、保存；Bump/父同步/恢复/发布另记。未完成则报告base未保存/未分发，不绕过快照。
- D08：实际实现预览保存至evidence/previews/<revision>/，在聊天展示控件、Popup、GM横竖与收起入口，manifest记录文件指纹；preview-confirmation.md记录明确用户确认。当前未出实现图，不请求或代签AC13。

原环境表的URP“尚待核对”是入口历史状态，由本节与design第2.1节补充；分支/HEAD/正式模板版本及原有未提交改动核对未变。性能建议预算与真机/构建限制详见design第8/11节，不作为已批准延期或通过结果。

## 验证与验收证据

- AC01—AC13：全部待实现/待验证；没有通过结论。
- 入口只核对文件可读性、身份记录和交接引用；纯交接文档，无 Unity 编译请求，编译/域重载状态不适用本阶段。
- 交接检查：5 份 Markdown、8 张参考图可读取，Markdown 本地链接无缺失；对照 baseline 的 962 个既有 UI 文件指纹全部保持一致。任务目录被 Git 忽略，跨机器交接需明确携带。
- 未运行测试、未作运行视觉验证、未作独立验收或人工签收。
- 方案只执行源码/配置/参考图片阅读、技术文档查证与临时文档检查，没有Unity MCP操作、编译请求、运行时探针或测试。文档静态校验见[evidence/design-readonly-check.json](evidence/design-readonly-check.json)；最终检查结果以该文件为准。
- D1交接静态检查：24项Prefab目标数量及R01—R08/AC01—AC13覆盖一致；当前3份交付文档本地链接无缺失；入口962个UI文件指纹全未变，Git状态相对入口无新增/消失条目。这不代表任何Unity行为或视觉验收通过。
- 返工问题：暂无，因为尚未进入实现或验收。

## 授权与下一步

- 整体授权：按用户原始需求在后续对应聊天完成方案、实现与定向验证；不重复请求已授权实现范围。分阶段职责不因整体授权而合并。
- 入口授权：只读核对并建立本目录资料；本轮已执行到交接结束。
- 未授权操作：模板切换/覆盖草稿、模板保存/Bump、Git 提交/发布、消费部署/升级、全量/长时测试；先把必要事项做成可审阅成果再处理真正授权缺口。
- 用户已授权创建一个方案设计聊天，使用更新后的 prompts/02-design.md；只启动方案阶段，不同时派发实施或独立验收，不派发子代理。
- [02 方案](prompts/02-design.md) 已完成 → [03 实施](prompts/03-implementation.md) 当前可接续，先S0/S1再批量UI → 实现与定向验证、implementation.md、更新04 → [04 独立验收](prompts/04-acceptance.md) 仍待前置 → acceptance.md，失败定向返工，必需项与用户预览确认均完成后回入口归档清理。
- 接手先核对 Git/UI 现状、正式活动模板和扩展身份差异以及 R3 后是否有新需求变更；A01/A02 无需重复询问，不要套用另一个任务的授权或历史通过记录。

## 下一对话启动文本

当前使用 prompts/04-acceptance.md，由区别于实施的另一聊天独立复核；本次不自行创建或派发。implementation.md、最终契约、13例/23项行为和短测已具备；用户未确认预览，base未保存，不能整体收束。

## 回入口收束文本

```text
接续任务 20261117-soft-glass-ui-gm 的入口收束，任务目录为 .utmp/ember-workflow/20261117-soft-glass-ui-gm/。
读取最新 handoff、requirements、design、implementation、acceptance 并核对验收后的相关工作区变化。
先核对 AC13：已向用户展示对应当前实现的预览图，且有用户对该版效果的明确确认。若未确认，先展示预览并等待用户，不得归档收束、标记完成或清理。
必需项全部满足且用户已确认预览时，把有效需求、最终决策、交付、逐项验收和必要证据归档到 docs/tasks/20261117-soft-glass-ui-gm/ 及对应模块正式文档。
核对归档资料和链接独立可用后标记完成，验证精确任务目录、所有权及无 symlink/junction 后，仅清理本任务已承接临时资料。
仍有失败、受阻或待验证必需项则保留未完成状态与证据，明确返工，不删除临时目录。
```


