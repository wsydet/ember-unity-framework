# 可组合扩展与消费迁移

状态：**本地实现，未发布**。本文件和 [Windward 适配器样例](../samples/WindwardRoomGraphAdapter.cs.txt) 位于 `com.ember` 内，随包含此功能的正式框架包交付。当前工作区 `package.json` 的 `0.24.3` 不代表本功能已包含在 `v0.24.3`。2026-10-29 已按授权通过项目中心普通保存六个模板的独立声明与接线；没有 Bump、提交、发布或升级任何消费项目。发布人须在本页末尾填写真实框架 tag/commit 与模板版本/hash，不得提前填写推测版本。

## 职责和装配

扩展定义可组合能力与编辑器访问条件，不是运行时宿主。框架必要基础设施仍由既有 Managers 提供；层级 FSM、规则瓦片和视差是可复用运行能力；蓝图、关卡制作、试玩和房间组合属于 Editor 扩展；Player、WindwardFlow、存档和加载协调属于业务 Module。没有新增常驻 Manager/Module，没有新共同父模板，也没有拆分 UPM 包。

运行能力的声明参与依赖检查，不会卸载已编译类型、删除资产、改变现有 FSM 执行或热销毁业务 Module。组合配置是开发期配置，不能用于玩家存档、运行时热插拔或替代 `EmberModuleAttribute.Enabled`。

| 稳定 ID | 类别 | 直接依赖 | 首版兼容 |
| --- | --- | --- | --- |
| `ember.core` | 必备基础设施 | 无 | Unity 6（6000.x），不能关闭 |
| `ember.fsm.runtime` | 运行能力声明 | `ember.core` | 现有 `Ember.Core.Runtime`，不改变 FSM 类型/协议 |
| `ember.fsm.blueprint` | Editor | `ember.fsm.runtime` | `Ember.Core.Editor`，通用静态观察器，无模板专属类型 |
| `ember.level.runtime` | 运行能力声明 | `ember.core` | 现有 `Ember.LevelAuthoring.Runtime`，Tile/Parallax 格式不变 |
| `ember.level.authoring` | Editor | `ember.level.runtime` | `Ember.LevelAuthoring.Editor`，场景制作依旧需要项目适配器 |
| `ember.level.trial` | Editor | `ember.level.authoring` | 复用 `EmberLevelTrialSession`；Domain Reload、合法 FrameworkScene 与项目试玩适配器仍是前置条件 |
| `ember.level.room-graph` | Editor | `ember.level.authoring` | 接受任意房间/端口投影；试玩为可选接线，需另启 `ember.level.trial` |

`EmberExtensionDefinition` 声明稳定 ID、类别、依赖 ID、兼容说明及 `checkCompatibility`。检查返回空字符串/null 表示允许，非空给出阻断原因；异常按不可用处理。自定义扩展在 Editor 程序集的 `InitializeOnLoad` 接线中 `EmberExtensionProject.Register`，重复 ID 被拒绝；域重载后重新登记。可在兼容检查中核对自己的模块元数据、正式模板身份或项目格式版本。框架不推测产品依赖或版本。

`EmberExtensionCatalog.Resolve(definitions, requested)` 显式展开传递依赖，报告缺失、循环和兼容失败，阻止受影响分支；不影响不相关分支。不识别的 ID 保留为诊断，绝不静默丢弃。首次版本不提供扩展二进制的按需卸载。

## 默认组合与项目配置

入口为 `Ember/项目中心 → 扩展组合`。启用会包含依赖；“预览关闭影响”只修改面板草稿并列出将关闭的有效能力，保存后才生效，可用“重新读取”放弃。关闭被其他根依赖的能力，会一起从显式组合移除这些根。基础设施不可关闭。非法配置保留原文件并阻断入口；修复对应文件后重新读取。

模板在活动 `Assets/Game/Config/EmberExtensionDefaults.json` 提供默认组合：

```json
{
  "schemaVersion": 2,
  "ownerTemplateId": "my-template",
  "ownerTemplateVersion": "该模板实际版本",
  "enabled": ["ember.fsm.blueprint", "ember.level.authoring", "ember.level.trial"],
  "bindings": []
}
```

以上是结构示意，空 `bindings` 不授予可选能力。用 `CreateConfiguration(ids)` 生成真实绑定；默认文件写入当前活动 Assets 后通过项目中心普通保存，不能手填 hash。默认声明绑定精确活动模板 ID 和版本，新玩法各自提供声明，无需共同父模板。**没有声明时仅有基础设施，不按模板名称自动启用。** schema v1 旧配置保留为待审阅草稿，可选扩展关闭，项目中心审阅并保存后才绑定当前身份。platformer-2d 与 topdown-2d 的预期显式组合是蓝图、制作、试玩；base、ui-game、visual-novel、source3d-2p5d 预期为空组合。各模板迁移进度以末尾登记为准，预期不代表已保存。房间组合默认关闭，项目按需启用。

首版扩展使用正整数 `contractVersion`（当前为 1）和实际内容 SHA256；这是工作区契约身份，**不是已发布包版本/tag**。内置指纹覆盖所属源码、程序集声明及关键操作文档；文本指纹统一 UTF-8 无 BOM/LF，避免 Git 检出时仅换行转换造成误失效，二进制仍按原字节。自定义定义可提供 `sourceRoots` 和明确契约版本。配置保存包含传递依赖的绑定；任一传递依赖的源码/契约变化也会使旧绑定失效，项目中心显式重新签收后才恢复入口。

消费项目首次保存后写入自己的 `ProjectSettings/EmberExtensions.json`，记录 schema、精确模板 ID/版本、显式根和扩展版本/hash 绑定。后续升级不覆盖此文件；旧绑定失效时需审阅迁移。没有正式模板的独立项目可显式启用通用工具，但不能获得别的模板的业务参数和适配器。

独立项目的自有参数、观察源和适配器可明确使用 `ownerTemplateId: ""` 的项目范围，并提供 enabled 模块条件；省略/null 仍是捕获正式模板的兼容路径，不会自动放开。模板专属内容必须声明真实模板 ID。

框架开发仓库使用 `ProjectSettings/EmberExtensions.<正式模板ID>.json`，避免切换模板时串用组合；没有保存配置时读取当前活动模板默认值。配置与默认声明均绑定精确模板 ID/版本；派生模板显式签收自己的组合，不借用父模板的有效身份。模板保存只保存活动 Assets 的默认声明，消费项目配置不是模板内容。

六个模板均已在各自正式加载后的活动 Assets 完成声明并通过项目中心普通保存，版本和封存 hash 均未改变。platformer-2d 与 topdown-2d 显式启用蓝图、制作、试玩，其他四个模板为空可选组合；房间图全部默认关闭。每次加载确认由用户处理，保存后项目中心错误/差异为零，没有直接编辑快照或父快照。真实版本/live hash/sealed hash 见末尾登记；普通保存的草稿尚不能作为已发布版本分发。

## API 对应与入口门禁

| 原调用/实现 | 新调用/处理 |
| --- | --- |
| 参数/观察源 Register 隐式授予蓝图能力 | 项目配置启用 `ember.fsm.blueprint`；提供者不再改变通用工具开关 |
| `EmberFsmBlueprintAccess.Register(owner, enabled)` | 保留二进制源码层兼容的可释放登记入口；不再授予通用工具开关，新代码使用正式配置 |
| `EmberFsmBlueprintParameters.Register` / `EmberFsmObservationSources.Register` | 签名不变；继续显式填写 ownerTemplateId 与 enabled，查询时同时校验扩展、模板、业务模块 |
| `EmberFsmBlueprintWindow.Open`、布局/观察/参数入口 | 原 API 保留；Open 在扩展关闭时抛错，旧窗口停止观察和编辑，保留原图、参数选择与本机布局 |
| `EmberLevelAdapters.Register(adapter)` | 旧签名保留，捕获当前正式模板；迁移为 `Register(adapter, ownerTemplateId, enabled)`，参数约定 `""` 为显式独立项目，null 不授予访问 |
| `EmberLevelAdapters.Get/All/GetObject/ObjectAdapters` | 只暴露当前允许的适配器；禁用适配器不再从 Get 返回。会话内部单独保留清理通道，关闭不阻断 Stop/恢复 |
| `EmberLevelWorkbench.Open`、AssetService、ObjectEditing、TerrainEditing 写入 | 增加正式扩展和空闲编辑状态门禁；纯尺寸计算/规则检查与运行类型不受开发开关裁剪 |
| `EmberLevelTrialSession.Start/Retry` | 保留签名，增加制作/试玩/适配器校验；Stop 始终允许，已拥有会话发现条件失效后走原停止和现场恢复流程 |
| `WindwardRoomBoard` 的校验/Arrange | `EmberRoomGraphRules.Validate/Arrange`；排版返回 ID → 图位置草稿，不写原资产 |
| 产品 GraphView 连线/删除/拖动 | `EmberRoomGraphWindow` + `EmberRoomGraphService.Connect/Disconnect/ApplyLayout`，统一门禁、冲突检查、Undo 和失败回滚 |
| 产品静态截图 | 投影到 `EmberRoomGraphNode.Preview`；通用画布用 ScaleToFit 保持原比例；捕获共享背景的场景策略仍由 WindwardRoomThumbnail 负责 |
| 产品试玩按钮 | 可选 `IEmberRoomGraphTrialAdapter` 提供描述符与初态，再由 `EmberLevelTrialSession` 正式启动 |

通用菜单为 `Ember/FSM/蓝图观察器`、`Ember/Level/2D 关卡工作台`、`Ember/Level/房间地图组合`，依据正式扩展状态真正增加/移除；无适配器时可以打开通用窗口并显示接线说明。模板专属菜单、Inspector 按钮和快捷入口必须额外检查 owner/module，不能以“包内实现”豁免。不要绕过公开 Open 直接创建工具窗口作为业务入口。

项目接线写操作也必须使用同一门禁；已缓存的适配器引用不会被 CLR 自动撤销。消费项目自己的公开 Create/BeginTrial/截图/旧菜单应在入口重新检查扩展与业务条件。不能只迁移菜单而保留旧 API 绕过。

窗口保留打开时的精确活动身份。扩展关闭、模板变化及域重载后重新核对；不匹配时保留资源与制作字段，停止有效写入和观察。房间图排版草稿序列化在窗口中，Apply 必须核对原图与 Targets 序列化指纹；外部修改后拒绝旧草稿。明确点击“刷新基线（保留草稿）”才能重新基于当前原数据审核。关闭窗口前仍应应用/导出需要跨布局重置保留的草稿，Unity 编辑器窗口布局本身由 Unity 管理。

## Windward 迁移范围

### Skill 与专属文档全链路

模板 `TemplateSkills/catalog.json` 中的 `requiredExtensions` 声明扩展 ID，`requiredModuleScope` 可声明模块范围。模板模块在 Editor 初始化时用 `EmberExtensionDocuments.RegisterModuleScope(id, owner, predicate)` 登记只读模块条件，不创建 Module。UPM 安装器接收 Core 提供的启用身份快照，不反向引用 Core/玩法程序集；发现副本的安装记录保存实际扩展契约版本/hash、所属模板版本/hash及模块范围。

`EmberExtensionProject.Save` 将配置、Skill 发现副本及安装记录放入原有项目中心事务，失败整体回滚；源目录不删，已修改发现副本先备份到 `.utmp/ember-ai-skills`。项目中心可预览 Skill 影响。配置外部变化及域重载后自动收回失效扩展的受管发现副本，不自动覆盖仍有效的本地副本；新模板模块须完成加载/域重载后在项目中心同步技能。旧 AI 会话内容不能即时撤回，执行前仍须调用 `EmberProjectSetup.GetTemplateSkillExecutionBlockReason`，重新核对当前扩展、模块、模板与安装指纹。自动收回遇到不一致/损坏记录会明确报告错误，不接管未管理目录。

模板专属扩展文档通过 `Assets/Game/Documentation/ExtensionDocuments.json` 声明条目 ID、标题、相对文档路径、精确模板 ID/版本、`requiredExtensions`、可选 `moduleScope` 及文档实际 SHA256 `contentHash`。文档编辑后用 `EmberExtensionDocuments.ComputeContentHash(entry)` 生成待审阅指纹；只在活动 Assets 更新声明，再普通保存。`Available()` / 项目中心索引只显示当前可用条目，`Read(id)` / `Open(id)` 每次重新校验；旧索引不能绕过关闭条件。原 Markdown 保留，关闭不删草稿。公共术语 Wiki 和本迁移说明可解释未启用能力，阅读说明不授予工具/Skill 使用资格。

平台玩家参数/观察源和关卡适配器仍服从所属模板与 Player 模块；关卡正式试玩只要求制作/试玩扩展，不要求额外开启蓝图。跨版本旧窗口保留原字段并停止操作；未版本化的旧平台玩家会话草稿提供只读迁移回退，不删除原缓存。

本轮只读核对了 `WindwardRoomGraphWindow`、`WindwardRoomBoard`、房间 Graph/Definition/Trial 及 LevelAuthoring 试玩接线。未写 Windward，也未写它的 PackageCache。样例不是已安装或已在 Windward 编译的证据。

| 处理 | 内容 |
| --- | --- |
| 保留 | `WindwardRoomGraph`、`WindwardRoomNode`、`WindwardRoomConnection`、`WindwardRoomDefinition` 的原序列化类型、字段及 .meta；所有既有房间、独立内容场景、图和截图 |
| 保留 | `WindwardRoomTrial/Plan` 的冻结、副本和哈希；WindwardLevelAuthoringAdapter 的初态、开发存档隔离和现场恢复接线；RoomThumbnail 的共享背景捕获策略 |
| 保留 | 挑战、所需能力、评级、验收记录、检查点、产品模块、章节参与者、世界原点、跨房间加载/卸载、相机、保存/恢复策略 |
| 适配 | 新建项目 Editor 适配器，Read 将原字段投影为 Snapshot；Targets 列出图及房间序列化资产；WriteLayout 只写 graphPosition；Add/Remove 只改 connections |
| 替代 | Board 的通用卡片、固定1:1画布、端口连接兼容计算、对接排版、图片预览、占用/方向/世界锚点通用校验 |
| 迁移后删除 | 已由通用 Board 替代且无引用的旧 GraphView、连线连接器、重复排版/通用校验实现与对应样式。必须先迁走产品侧栏、截图操作和业务试跑，再检查引用；本轮未删除任何 Windward 文件 |
| 不迁入框架 | 十二槽强制校验、产品能力/评级/挑战、房间计划和加载策略。十二槽只提供可选图预设，任意项目可声明不同端口数与 ID |

完整接线样例见 [WindwardRoomGraphAdapter.cs.txt](../samples/WindwardRoomGraphAdapter.cs.txt)。在获得消费升级授权后，将它作为项目 Editor 源文件接入现有 `Game.WindwardFlow.Editor` 程序集并引用 `Ember.Core.Editor`、`Ember.LevelAuthoring.Editor`；保留运行时类型原程序集。示例中的模块判断只读 `EmberModuleAttribute.Enabled`，不构造或激活 Module。实际升级时要再次核对 Windward 当时源码。

Windward `graphPosition` 是卡片左上角的像素坐标，GraphSize 是占屏比例 × 270 的制作尺寸。端口 Anchor 为左上(0,0)/右下(1,1)的卡片坐标，Normal 使用画布方向（上为负 Y）。WorldAnchor 来自原 `port.Anchor(ConnectionBoundary)`，并不由图位置推导。`authoredWorldOrigin` 保持原值，任何排版都不能移动房间根对象、场景内容或出生位置。连接合法不代表角色实际通行已验收。

## 旧资产、程序集与升级策略

没有移动或重命名既有 Runtime/Editor 程序集；新增基础组合机制在 `Ember.Core.Editor`，房间工具在 `Ember.LevelAuthoring.Editor`。FSM 和 LevelAuthoring 原运行类型、字段与资源 GUID 保持。没有为 Windward 定义替代序列化房间类型，没有 `FormerlySerializedAs` 或类型搬迁需要；Snapshot 只是 Editor 内存投影，禁止批量替换旧房间资产。适配器写回原图，Unity Undo 记录 Targets，产品校验失败撤回该组变更；不会自动 SaveAssets。

测试夹具的独立 ScriptableObject 只存在于测试程序集，不能当作新生产资产格式。扩展配置使用 schema v2 JSON，schema v1 仅作为待迁移草稿读取；关闭只影响开发工具访问，升级前后原资产仍可被原运行时代码读取。

已有消费项目**不必重新部署模板**：正式升级到包含本实现的框架包，保存项目扩展配置，再增加/迁移项目 Editor 接线即可。Windward 可保持当前 platformer 基线和自有业务，不需要重建场景、变更 GUID 或新共同父模板。若希望以后新部署也获得相同默认组合和门禁，应对相应模板另行正式加载、Assets 修改、项目中心保存/Bump，并随新的框架 tag 发布；模板升级与项目增量接线是不同交付路径。

## 验证与回退

验证清单：

- 解析共享/传递依赖、缺失 ID、循环、兼容拒绝与异常、关闭影响、非法配置及未知 ID 保留。
- 实际菜单增加/移除、公开 Open/写 API 拒绝、业务模块/模板条件、旧窗口保留资源和房间排版草稿。
- 房间端口数量不固定，方向、关闭、占用、世界锚点相容性检查；回环和不连通图的稳定排版。
- 原资产 GUID 与 authoredWorldOrigin 不变；排版及连线 Undo/Redo；产品校验失败回滚；源数据变化拒绝旧草稿。
- Unity MCP 显式编译、对应域重载/程序集已加载、Console 检查；相关增量 EditMode 用例。
- 人工实际模板切换（含所属/派生/无关/无效身份和消费部署）、域重载后草稿、端口拖线/删除和原比例截图体验、实际房间通行、试玩停止/重试/现场恢复及项目构建。

2026-10-29，机制首批验证，本地 Unity 6000.5.4f1 / 正式活动 topdown-2d 的实际结果（随后各模板接线验证继续见下文）：

| 验证 | 实际结果与限制 |
| --- | --- |
| 最后主回归 | 26/26 EditMode 通过：依赖、门禁、菜单、房间图、旧资产/Undo、产品失败回滚、旧窗口/草稿、关闭后试玩清理、三项相关 FSM 布局用例；约 32 秒 |
| 最后范围补验 | 13/13 通过：扩展求解、蓝图提供者、条件菜单和显式项目观察源范围；约 3.5 秒 |
| 早期集成 | 新房间图、topdown 接线及两个错误提示 GUI 修正用例 14/14 通过。后续 29/29 相关检查通过；各批是增量范围，不是全量测试 |
| 编译/域重载 | 经框架实例 Unity MCP 显式编译，确认完成事件及已加载新程序集。最后 Console Error=0；仍有一条 MCP 在域重载时的 WebSocket 初始化警告，连接已恢复 |
| 实际关闭后域重载 | 在当前 topdown 下用临时已有图夹具实测：关闭状态保持，FSM/房间图真实菜单均隐藏；原图 GUID 引用、适配器/模板身份、版本指纹与两条排版草稿逐字段一致。夹具已删除，原配置已恢复 |
| 工作区保护 | 对照开始时的 10,960 个活动 Assets/模板文件指纹，10,322 个模板快照文件零变化；活动 Assets 中既有内容仅有四个已声明编辑器接线文件及 topdown README 的本轮差异。新增默认声明独立列出，无既有场景/资源/GUID 改写 |

后续 platformer 阶段：用户确认正常模板加载弹窗，Unity MCP 确认正式身份和编译/域重载。首批 8/8 通过；扩大相关回归 20 项中 19 项通过，一项旧蓝图测试在非 OnGUI 上下文调用真实平台绘制器，改为检查实际可用性谓词后，相关 5/5 复验通过。实际全链路关闭验证另发现单项 Skill 依赖被写为 JSON 字符串的问题，已修正数组与严格类型校验，最后 4/4 通过。不是全量测试。

实际关闭并域重载后：仅基础设施启用，FSM/Level/Player 扩展真实菜单全部缺席，专属文档索引为零，玩家 Skill 目录退出发现且旧 ID 执行被拒绝；房间图临时窗口的排版草稿、资源、所有者及版本指纹逐字段一致。原组合已恢复，临时窗口已关闭，无生产资产创建。最终 Console Error=0；编译过程仅有两条既有 FSM 测试字段序列化分析警告。

后续模板接线已经实际完成六个模板的正常加载、编译/域重载、Console 检查和普通保存。base 相关8/8通过；ui-game、source3d-2p5d、visual-novel 各2/2通过，实际确认可选扩展菜单、公开API、相关Skill及专属文档均未开放；小说原有9个模板Skill保留并通过执行前检查。

最终 topdown 补迁覆盖 schema v2、专属文档、窗口版本/hash和直接试玩门禁。12项中11项通过，另一项仅在清理未显示的临时窗口时抛错；修正清理后两个窗口用例2/2复验通过，隔离及试玩断言没有待修复失败。直接 BeginTrial 在制作启用而试玩关闭时被拒绝，Stop仍可用，正式试玩不依赖蓝图。最终编译/域重载完成，Console Error/Warning=0。最初备份的1053个topdown文件无缺失，仅五个预期现有文件变化；3490个父快照文件逐文件hash一致且无新增父快照文件。六份声明的精确模板身份及当前扩展版本/hash全部匹配。

蓝图接线指南尾部及首批保护记录保留的是早期阶段范围，当前累计验证以本页为准。仍未执行 Windward 编译/运行/升级、真实手柄或房间通行、全量测试及 Player 构建；消费部署身份和合法派生只覆盖相关身份矩阵，没有覆盖全部真实消费工程。房间图实际拖线手感与产品截图/试玩仍须消费项目人工体验。

回退步骤：停止试玩并等待现场恢复；备份项目扩展配置、Skill安装记录/本地副本、原资产和.meta、项目适配器及必要窗口草稿；通过版本控制撤回新增Editor接线与默认声明，恢复原产品窗口入口；通过UPM Manager回退到已确认的旧框架版本。新版配置可保留备份，旧框架不读取它；原房间与场景不需反向转换。Skill发现副本的本地修改备份在项目`.utmp/ember-ai-skills`，按对应模板身份通过正式预览/同步恢复，勿直接覆盖不相关技能。若已应用排版/连线，可先Undo或恢复图资产版本，不还原/删除内容场景。框架作者若需回退已经普通保存但未Bump的模板，先正式加载对应模板，只在活动Assets恢复本轮差异，再通过项目中心普通保存；不直接回写Templates~/ParentSnapshot~，也不覆盖其他模板草稿。

## 正式发布登记（尚未执行）

| 项目 | 真实交付值 |
| --- | --- |
| 包/tag/commit | 未发布，待授权正式发布后补充 |
| topdown 默认组合和全链路门禁 | 已最终普通保存0.1.0，实时 hash `b3bf3acb890f00980fd6633f8765a93a`；未Bump，封存 hash `61cbe49aa797b1604f293b9756674566`；首次保存hash `549c696fccdd7614fd68ff6e9f787933`为历史阶段 |
| platformer-2d 全链路接线 | 已普通保存 0.13.0，实时 hash `1cf67da87731c1b88d685a8d4e8a7e08`；未 Bump，封存 hash `ff217ca17005f39519049c46a83c138b` |
| base 空组合 | 已普通保存 0.10.0，实时 hash `e9d789311cad169a19b6a76331a2877a`；未 Bump，封存 hash `61cbe49aa797b1604f293b9756674566`；只新增空默认声明及其目录/.meta，8/8相关测试通过，编译/域重载和真实菜单/API/Skill/文档关闭验证通过 |
| ui-game 空组合 | 已普通保存 0.2.0，实时 hash `43030af8571b29f5c1d2d0631faa1733`；未 Bump，封存 hash `cc6430af3e8cdb46ee0e034ac1a18fb0`；仅新增声明及目录/.meta，2/2相关测试和实际无扩展入口验证通过，编译/域重载完成，Console Error=0 |
| source3d-2p5d 空组合 | 已普通保存 0.6.0，实时 hash `9be0d1035d4396e096a3eaa820765eed`；未 Bump，封存 hash `f98b85bf591042619e55fafd5cd88af4`；仅新增声明及目录/.meta，2/2相关测试和实际无扩展入口验证通过，编译/域重载完成，Console Error=0 |
| visual-novel 空组合 | 已普通保存 0.22.1，实时 hash `437a3552583df64cf2ab3d1c26083864`；未 Bump，封存 hash `61155f5eb3958eacdbd0a9467ebf60c3`；只新增空默认声明及目录/.meta，2/2相关测试和实际关闭检查通过，原有9个小说Skill仍有效，编译/域重载完成、Console Error=0 |
| 声明一致性 | 六份默认声明均为schema v2，精确模板ID/版本与当前扩展契约版本/hash匹配；ParentSnapshot~ 未修改 |
| Windward/其他消费项目 | 未升级、未安装接线、未写 PackageCache |
