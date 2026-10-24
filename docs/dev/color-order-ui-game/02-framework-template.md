# 阶段一：建立通用 UI 游戏模板

本阶段在 `ember-unity-framework` 开发仓库实施，计划创建模板 `ui-game`，正式父模板为 `base`。产物是可供新项目部署、继续修改的完整 Assets 初始内容；色彩排序产品在[阶段二](03-consumer-project.md)实现。

本文编制于 2026-10-23，是实施方案，不代表模板、拟新增类型或示例已经存在。当前任务只产出文档，未授权或执行创建模板、切换、保存、Bump、提交、发布。后续执行者以用户当次授权范围推进；已明确授权的步骤直接执行，不因本文重新设置审批。未包含在范围内的 Bump、发布和消费部署分别报告待完成。

## 1. 目标、归属与完成边界

用户已选择产品 V2 路线：三个短关原型 → 九关 → 巡游。阶段一只提供这类 UI 游戏可以复用的生命周期和页面接线，不把三个色彩短关提前写进模板。产品规则以[产品规则与范围](01-product-rules.md)为准。

| 归属 | 本阶段工作 | 完成证据 |
| --- | --- | --- |
| `com.ember` 公共包 | 优先复用 EUI、模块、状态/场景切换、输入、音频等已有能力；真实公共缺口才另列框架改动 | 每项公共改动都有具体复现、适用项目和相关验证 |
| `ui-game` 模板 | 从 base 派生；加入 UI 玩法会话、页面、输入/暂停接线、中性示例及扩展说明 | 正式模板身份、Unity 运行证据、保存及封存记录 |
| 消费项目 `ColorOrder` | 色彩序列、九宫格、连击/评分、机关、九关、巡游、产品存档、美术文案和平台配置 | 留给阶段二，不进入 base 或通用模板 |

本阶段默认判定为“模板升级”。若必须修改通用 API，则调整为“框架 + 模板升级”，先确定公共契约，再调整模板接线。可选业务一律采用 Module；某游戏必需的能力不因此成为框架必备 Manager。判定依据见[改动分流规则](../../../Packages/com.ember/Documentation~/maintenance/unityfarm-change-routing.md)。

模板应让新项目从 FrameworkScene 正式启动后，能完成“主页 → 中性示例一局 → 结果 → 重开/回主页”，并展示设置/暂停及重新进入后的资源释放。结果属于一局会话阶段，不为每个产品阶段新增框架顶层状态。不建设通用关卡规则引擎、商业化、排行、经济或产品关卡编辑器。

## 2. 开始前的事实核对与草稿保护

### 2.1 本次只读核对结果

| 对象 | 编制时事实 | 开工时检查 |
| --- | --- | --- |
| Unity 工程 | `6000.5.4f1` | 读取 `ProjectSettings/ProjectVersion.txt` |
| 包源码声明 | `com.ember 0.21.4` | 实际 Unity 解析包来源必须为 embedded；版本声明与发布状态分开记录 |
| 正式编辑身份 | `platformer-2d 0.11.1`，加载于 2026-10-22 11:13:33 | 读取项目中心及 `Assets/Editor/EmberEditingTemplate.json` |
| 已存模板 | base、platformer-2d、source3d-2p5d、visual-novel | `GetTemplates()` 或项目中心模板树中重新确认 |
| 计划模板 | `ui-game` 尚不存在 | 检查重名，存在时先确认是否已有本任务成果，不覆盖重建 |
| 计划父模板 | `base 0.8.3`，stable，兼容声明 `0.21.0` | 中心确认父内容已封存，谱系与资源校验有效 |
| 工作区 | 已有大量修改、删除和未跟踪文件 | 保留用户既有内容，不将整份 dirty 状态当作本任务改动 |

base 元数据的 `contentHash` 与 `versionedContentHash` 在编制时都为 `1270ce6dfcae3c01d050c09ecfd60337`。这只是读取字段得到的结果，不代替本轮磁盘内容、GUID、父快照或 Unity 验证。

embedded 开发端以编辑记录为正式身份。现有 `EmberDeployedTemplates.json` 中旧 base 部署记录不能覆盖它；也不能根据当前场景、残留小说脚本或已打开窗口推断身份。真实身份规则见 [EmberProjectSetup.Identity.cs](../../../Packages/com.ember/Core/Editor/EmberProjectSetup.Identity.cs)。

### 2.2 切换前必须可恢复

1. 记录 Git 状态、当前编辑身份、场景、内存草稿和本次操作范围。保存需要保留的场景/资源；无法保存的窗口草稿记录其具体恢复方式。避免随意关闭有未保存布局或数据的窗口。
2. 使用可核对的备份保护五个受管目录、编辑记录以及相关非受管改动；保留文件与 `.meta` 配对。记录备份位置、时间和恢复步骤，并检查关键文件确实存在。不得用 `HEAD` 替代当前工作区备份。
3. 区分“当前项目相对 Git 的变化”和“相对当前已存模板的变化”；模板切换本身会造成大量 Git 差异。通过项目中心内容差异检查当前草稿，不能将所有差异直接保存进 platformer。
4. 当前 platformer 草稿是否正式保存/Bump 由其任务范围决定。保护草稿不自动授权将未完成内容封存，也不要求为了本任务提交全部工作区。
5. 完成保护后，按本次授权使用项目中心创建并加载新模板。未授权切换时，交付保护方案和可执行步骤，保留工作区等待用户执行；明确授权后不再重复询问。

加载或完整部署替换的目录为 `Assets/Game`、`Assets/Resources`、`Assets/Ember/Editor`、`Assets/Settings`、`Assets/GameResource`。`Assets/Art`、`Assets/ThirdParty`、`Assets/Editor` 和 `ProjectSettings` 不在模板快照内，但其中的相关改动仍须保留。

禁止 `git reset/clean` 清空工作区来准备模板，禁止直接改 `Templates~`、`ParentSnapshot~`、模板 hash 或身份文件绕过正式操作。

## 3. 推荐架构与现有能力

### 3.1 候选组成：下列新增名称均为设计

| 拟新增对象 | 候选位置 | 职责与边界 |
| --- | --- | --- |
| `UiGameModule` | `Assets/Game/Module/UiGame/` | Gameplay Phase 的 UI 玩法装配；拥有本次会话，进入/退出时初始化和释放 |
| `UiGameSession` 及小型状态类型 | 同模块 | 管理本局代次、运行/暂停/完成、输入资格和结束去重；不内置颜色、关卡星级、连击或产品计时规则 |
| 中性点击目标示例 | 同模块及对应 Config | 单一固定目标与完成动作，用来验证“进入一局、暂停、结束、重开”；可清楚替换/移除 |
| `EUIUiGameHomePage`、`EUIUiGamePlayPage`、`EUIUiGameResultPage` | `Assets/Game/UI/Runtime/Module/UiGame/` | 由 EUI 中心生成；分别展示主页、局内输入与结果，不在页面里维护产品规则真值 |
| 必需的 Item | 同 UI 模块 | 只有需要重复展示时才经中心创建；不为了未来九宫格提前建产品格子 |
| 模板说明与术语 | `Assets/Game/Documentation/ui-game/` | 正式使用、扩展、生命周期、暂停、存储接入和验收入口 |

以上不是框架现有 API，实施前需按实际程序集和命名约定核对。先实现足够支撑中性示例的最小契约，再由消费项目组合自己的 Module。不要先造一套所有 UI 游戏都必须实现的庞大接口。

`UiGameModule` 显式声明 `[EmberModule(ModulePhase.Gameplay, Enabled = true)]`。若新增其他模块，也必须明确 Enabled 和 Phase。Global 可常驻，Gameplay 自动随状态激活；Main Phase 当前没有自动接线，不能仅标属性便假定它已初始化。模板默认关闭的 PlayerData、GameTable、GameInput 等模块，只在实际需要时启用并验证。

### 3.2 生命周期和纯 UI 场景

复用 base 的 Init/Main/Gameplay/Settings 主流程及 SceneFlow，不以直接打开 GameplayScene 代替正式入口。源码参考仅用于阅读，不在快照中改代码：

- [base GameGameplayState](../../../Packages/com.ember/Templates~/base/Assets/Game/State/GameGameplayState.cs)
- [base GameSceneScope](../../../Packages/com.ember/Templates~/base/Assets/Game/Module/SceneFlow/GameSceneScope.cs)
- [框架 GameplayState](../../../Packages/com.ember/Core/Runtime/State/GameplayState.cs)
- [状态切换请求](../../../Packages/com.ember/Core/Runtime/State/EmberStateMachine.Transitions.cs)

base `GameGameplayState.PrepareEnterAsync` 使用 `GameSceneScope.Require(scene).PrepareAsync(...)`；纯 UI GameplayScene 也须保留唯一启用的 GameSceneScope，可没有业务参与者。不要因没有角色/地形而删除 Scope。

进入 Gameplay 时建立新会话代次，页面只提交意图，由模块判断是否允许。结束、重开、返回主页都关闭旧输入资格并释放旧订阅/异步工作；旧页面和动画的迟到回调不得结算新一局。结束操作要幂等，双击按钮不能重复结算、保存或导航。

业务暂停必须明确实现：框架 `GameplayState.OnPause/OnResume` 只调用默认空的 `OnGameplayPause/OnGameplayResume` 钩子，不会自动冻结 Module Update、业务计时或按钮。模板在状态钩子与模块会话之间接线，暂停时拒绝业务输入、停止需要暂停的业务推进，恢复后只恢复当前会话资格。不要仅靠打开设置窗口、`timeScale=0` 或启用 GameInput 推断已经暂停。内部转场锁、用户暂停、已完成状态分别保留来源，释放一个原因不能解除其他限制。

### 3.3 正式 EUI 制作

执行 UI 工作前使用仓库 [ember-eui-build](../../../.agents/skills/ember-eui-build/SKILL.md) 及其[编辑器 API 参考](../../../.agents/skills/ember-eui-build/references/editor-api.md)。所有正式 Page、Popup、Item、Prefab 骨架、Binding 和页面注册都经 EUI 开发中心创建/生成；先绑定，再依据实际生成成员写用户逻辑。

| 已有 API/行为 | 本阶段使用方式 |
| --- | --- |
| `EUICreationService.TryBuildPlan`、`Create` | 中心创建流程的服务实现；新 UI 选 Business，ClassPath 使用 `Module/UiGame` |
| `EUIBindingCodeGenUtility.TryRegenerateCode(EUIBinding, out string)` | 修改绑定后走中心重新生成，核对错误结果 |
| `EUIManager.ShowMainPage(...)` | 展示页面；不会自动销毁旧页面，退出时必须清理自己拥有的页面 |
| `EUIManager.ClosePageByDef(...)` | 结合实际 page definition 关闭旧页，不依赖显示另一页自动清理 |
| `EUIItemFactory.TryCreate(GameObject, out EUIItem, out string)` | 只做 Item 创建接线，宿主仍负责 GameObject 的实例化与销毁 |
| 状态 `RequestPush/RequestPop` | 若 Popup 与压栈状态绑定，关闭按钮走 Pop，并关闭 `ClickMaskToClose`，避免页面与状态分离 |

证据：[EUICreationService](../../../Packages/com.ember/UIExtension/Editor/Pages/EUICreationService.cs)、[生成入口](../../../Packages/com.ember/UIExtension/Editor/EUIBindingCodeGenUtility.cs)、[EUIManager](../../../Packages/com.ember/UI/Runtime/EUIManager.cs)、[Item 工厂](../../../Packages/com.ember/UIExtension/Runtime/EUIItemFactory.cs)。表中为现有方法名，参数以实施时源码及 Skill 为准，不根据示例猜重载。

新生成目录必须保留 `Runtime`。用户逻辑写用户文件/钩子，不手写 `.Binding.cs`、`EmberManaged` 区或注册生成文件。继承来的 Framework 模式页面按既有用户钩子扩展，不强行改成 Business 或在消费端越过保护重新生成。页面资源归 `GameResource/Resources/UI/Module/UiGame/`，通用字体和控件继续复用现有来源。

### 3.4 数据、输入和音频接入边界

模板提供存储接入说明；本次中性示例没有必须持久化的成绩，不为演示存档把产品数据结构固化成模板协议。

- base 的 PlayerDataModule 是可选 Global 模块，默认关闭。需要持久化的消费项目显式启用，并等待 `WhenReady` 返回的 PrepareResult 成功；初始化不会自动读档，`StopAsync` 不会自动保存。
- `SaveAsync<T>(int slot, T snapshot, SaveSummary summary, IProgressCodec<T> codec, CancellationToken cancellation = default)` 返回 `SaveOperation<SaveResult>`；`ReadAsync<T>(int slot, IProgressCodec<T> codec, CancellationToken cancellation = default)` 返回 `SaveOperation<ReadResult<T>>`。等待各自 `.Completion` 并检查结果 `Succeeded/Status`，不能把发起请求当保存成功。
- 产品自己定义 `IProgressCodec<T>` 的 `DataTypeId`、`Encode`、`Decode` 与格式版本，配置独立存储目录/数据族。base 的 version/level/coins 是示例，不用它承载 ColorOrder 正式进度。正常退出按已有 BeginDrain/DrainAsync 契约处理；强杀恢复能力另行验收。
- PlayerPrefs 只保存偏好；DataSaver 的同步 JSON 不替代正式进度事务。主成绩、关卡进度与中断恢复结构留给阶段二。
- GameTableModule 与 GameInputModule 也默认关闭；数据量或输入设备需要时再装配。配表经源表生成，不手改生成区。UI 按钮门控独立于 GameInput 是否启用。
- 音频可复用 `EmberAudioManager.PlayBGM/PlaySFX/SetBGMVolume/SetSFXVolume`；只加入中性示例必要的反馈，不收录产品专用音效。

证据：[PlayerDataModule](../../../Packages/com.ember/Templates~/base/Assets/Game/Module/PlayerData/PlayerDataModule.cs)、[配置](../../../Packages/com.ember/Templates~/base/Assets/Game/Module/PlayerData/PlayerDataConfig.cs)、[存储契约](../../../Packages/com.ember/Templates~/base/Assets/Game/Module/PlayerData/Storage/ProgressContracts.cs)。正式说明随 ui-game 的 Assets 分发，不要求消费项目访问这些开发仓库快照链接。

## 4. 可直接执行的任务序列

| 任务 | 操作 | 交付/停止条件 |
| --- | --- | --- |
| A0 状态与保护 | 读取公共规范、正式身份、Git 状态；完成第 2 节备份及恢复记录 | 旧草稿可恢复；没有为本任务清除其他内容 |
| A1 父级就绪 | 项目中心核对 base 的 schema、版本、封存、内容与谱系状态 | 父级必须 Root/Synced 且内容已封存；若不满足，明确原问题，不手修 metadata |
| A2 创建加载 | 按下文中心表单创建 ui-game，父 base | 新模板与 ParentSnapshot 由中心建立，正式编辑身份变为 ui-game |
| A3 最小接线 | 复用启动/状态/Scope，新增 UiGame Module 和中性会话 | 可进入并退出一局，不含 ColorOrder 业务 |
| A4 正式 UI | 中心创建并绑定主页、局内、结果；连接用户逻辑、暂停与返回 | 页面生成结果、注册、资源路径一致；旧会话不能影响新会话 |
| A5 文档与验证 | 在 Assets 写正式使用说明/术语；执行相关编译、测试、Play 与项目校验 | 第 6 节证据齐全；未覆盖项明确 |
| A6 保存与封存 | 中心预览差异、普通保存；范围包含 Bump 时显式选择版本级别 | 内容 hash、封存 hash、父快照与编辑记录一致；分别记录完成状态 |
| A7 发布准备/交接 | 依据授权准备或执行框架发布，填写版本交接 | 消费阶段只使用真正包含 ui-game 的 tag/commit；未发布就标待发布 |

任务之间出现编译错误或无效引用，先修复当前批次，不把问题推进到保存/发版后再处理。需要扩展为公共框架改动时，补充范围与验证；不要因可选业务需要而顺手重构 base。

### 4.1 项目中心准确创建步骤

1. 打开 `Ember/项目中心 → 模板开发`。消费安装来源不提供模板开发能力；需要在 embedded 框架仓库操作。
2. 点击 `+ 新建 / 派生 / 另存为`，类型选择“派生模板”，父模板明确选 `base`，填写 `ui-game`、显示名“UI 游戏”及通用职责描述。
3. 在已保护草稿且切换属于授权范围后，点击“创建并加载到项目编辑”。中心先创建父内容的完整物化副本与 ParentSnapshot，再加载它。
4. 核对正式编辑记录确实为 ui-game，再开始修改项目 Assets。选中左侧模板不等于已经加载。

不需要先加载 base 再创建一次；普通派生允许从任意合格父模板创建。不要用“将当前编辑副本另存为新模板”把当前 platformer 变成 UI 模板：另存为固定使用当前编辑模板作为父级，不能把表单选择的另一个父级强加给现有副本。

新派生模板起始为 `0.1.0 / preview`，复制父内容时已经具有初始 contentHash/versionedContentHash。新增 UI 游戏结构后先普通保存，再按真实改动显式 Bump；结构新增通常为 minor，可能得到 `0.2.0`，最终版本以中心结果为准。不要手工把它写回 0.1.0 来伪装未发生版本变化。

### 4.2 项目中心实现核对表

以下是现有 `Ember.Core.Editor.EmberProjectSetup` API，用于核对工具实现与结果，不授权绕过项目中心直接改快照。

| API | 真实职责/限制 |
| --- | --- |
| `GetTemplates()`、`GetCompatibleTemplates()` | 扫描模板；兼容列表要求框架 major.minor 相同，隐藏 deprecated，保留 preview |
| `GetEditingTemplate()`、`IsEmbeddedPackage()` | 开发身份及 embedded 来源检查 |
| `CreateTemplate(string id, string displayName, string description, string parentId)` | 创建独立/派生模板；父非空时物化完整内容与父快照，**自身不加载** |
| `LoadTemplate(string id)` | 替换五个受管目录并建立编辑身份，仅 embedded |
| `SaveEditingCopyAsNewTemplate(string id, string displayName, string description)` | 当前未过期编辑模板成为父，保存新派生并切换编辑记录 |
| `SaveTemplate(string id, string displayName, string description)` | 核对当前编辑身份、过期与磁盘元数据后保存，普通保存不升版 |
| `BumpTemplateVersion(string id, int field)` | 0=major、1=minor、2=patch；封存已经保存的内容，不包含尚在项目里的未保存改动 |
| `GetTemplateSyncStatus`、`ComputeParentSyncPreviewPlan`、`ApplyParentSync` | 父级更新的状态、预览与正式应用，不以文件复制替代 |
| `Initialize(string id)` | 消费端首次部署/合格补缺、注册场景、刷新映射 |
| `DeployReplacingActiveTemplate(string id)` | 消费端显式完整替换受管目录，不把业务内容写回包 |

实现：[EmberProjectSetup.cs](../../../Packages/com.ember/Core/Editor/EmberProjectSetup.cs)、[模板开发面板](../../../Packages/com.ember/Core/Editor/EmberTemplateDevelopmentPanel.cs)、[初始化面板](../../../Packages/com.ember/Core/Editor/EmberProjectSetupPanel.cs)。机制详见[模板开发、父子同步与消费升级](../template-upgrade-system.md)；该文历史版本和验收数字不能当成本轮证据。

## 5. 保存、Bump 与发布的交付顺序

1. 在项目中心“内容差异”核对新模板的范围，确认没有平台玩家、小说、ColorOrder 产品规则和机器专用路径混入。正式模板说明位于 `Assets/Game/Documentation/ui-game/`。
2. 完成实际编译/运行验证后执行普通保存。中心会提示保存场景、保存资源并重新计算差异。核对保存目标、编辑记录和 contentHash；普通保存不改变模板版本、versionedContentHash 或 frameworkVersion。
3. 在授权包含 Bump 时，到“版本与设置”显式封存适当版本。核对磁盘计算 hash=contentHash=versionedContentHash，父快照 hash=parentContentHash；只看字段相等不够。
4. 派生模板的框架兼容声明来自父级同步，不能单独手改。若本批框架 minor 改变，按正式根模板兼容声明与父级同步流程处理；没有内容变化时不伪造模板 Bump。
5. 模板随 `com.ember` 发布，新增模板必须进入新的框架发布 tag；当前旧 `v0.21.4` 不包含尚不存在的 ui-game。准备发布时使用 [ember-release-framework](../../../.agents/skills/ember-release-framework/SKILL.md) 核对包版本、变更日志、依赖声明、模板/父级状态与 Git 范围。不要替发布步骤预编造一个 tag。
6. 发布和提交按用户范围执行。只做发布准备时，交付具体变更、校验和待执行命令/操作，明确“未发布”；有发布授权时完成并核对远端 tag，不能只写本地版本号就宣称可消费。
7. 完成后按约定恢复原工作环境时，先保护/保存当前 ui-game 草稿，再通过中心恢复目标模板与备份；不擅自丢弃任一模板改动。恢复是否属于本次范围应在开始时记录。

项目中心不可用、保存失败或身份不明时，保留项目 Assets，报告具体未完成步骤。禁止直接编辑 Templates~/ParentSnapshot~、手改版本或 hash、运行复制脚本代替保存/Bump。

## 6. 增量验证与验收

实施前完整阅读[Unity 验证规范](../../../Packages/com.ember/Documentation~/maintenance/unity-validation.md)。本次规划文档未执行以下验证；执行者填写实际结果，不继承历史模板测试结论。

| 范围 | 最小验证 | 通过标准 |
| --- | --- | --- |
| 编译与导入 | 每批 C#/程序集改动后 Unity MCP 显式请求编译；资源修改刷新后核对 | 本轮编译与域重载完成证据、非 compiling/updating、随后 Console 无本轮错误 |
| 会话单元行为 | 仅新增/直接相关用例：开始/结束/重开、双击结束、旧回调、暂停/恢复、退出清理 | 结束仅一次、旧代次无写入、暂停不接受业务输入、重新进入无残留 |
| 页面链 | FrameworkScene 启动→主页→中性一局→结果→重开/主页，重复进入 | 不靠直接打开玩法场景；页面、订阅及 Item 生命周期完整 |
| Settings/暂停 | 暂停前后输入、更新、结束动画和返回行为 | 暂停业务停止，恢复只解除对应原因；Popup 关闭与状态一致 |
| 纯 UI 场景 | SceneFlow 准备与 Scope 校验 | GameplayScene 有唯一启用 Scope，准备结果成功，退出释放 |
| UI 资源与适配 | 按产品将使用的竖屏基准、窄屏/长屏、安全区和鼠标/单指模拟检查 | 文本可见、控件可达、遮罩不误触；示例仅证明模板链路，不证明产品九宫格适配 |
| 项目中心校验 | 内容差异、资源/.meta/GUID、场景、生成管理区与 UI 注册 | 无阻断错误，业务差异和警告逐项说明 |
| 模板封存 | 已授权保存/Bump 后核对 metadata、磁盘 hash、ParentSnapshot、编辑记录 | 内容/版本关系自洽，可作为后续消费基线 |
| 新消费工程 | 从实际交付包安装并直接部署最终模板，完成同一最小链路 | 包来源、部署记录、基线与运行结果匹配；不能用开发工程通过代替 |

UI 展示和交互必须保留真实 Play 截图/操作结果；静态链接检查和单元断言不能证明布局可用。相关用例之外的全量或预计较长测试，按公共规范获得明确范围授权，不拆批绕过。

MCP 断连遵循公共规范：先检查待编译改动及请求状态，无法送达时立即提示手动触发编译，再每隔 30 秒最多重试 10 次。结果不明不重复发编译/测试请求，不用 Editor.log 轮询、BatchMode 或 dotnet build 替代。最终未完成时如实记录，不填“编译通过”。

## 7. 交给阶段二的必填记录

详细验收模板见[04 验收与交接](04-acceptance-handoff.md)。至少提供以下信息；没有的项填写“未完成 + 原因”，不能留一个看似可安装的示例值。

| 字段 | 应填写内容 |
| --- | --- |
| 框架交付 | 实际 frameworkVersion、Git tag、commit、可安装 Git URL、是否已发布 |
| 环境/依赖 | 验收 Unity 版本、依赖声明位置、必要插件与权限、MCP 接入状态 |
| 模板身份 | templateId、version、channel、frameworkVersion |
| 内容/父级 | contentHash、versionedContentHash、parentId、parentVersion、parentContentHash、完整性核对结果 |
| 使用入口 | 随模板分发的 `Assets/Game/Documentation/ui-game/README.md` 及扩展、暂停、存储说明 |
| 实际生成物 | Module、页面/Item、注册定义、资源路径与中性示例替换位置 |
| 完成动作 | 项目修改、普通保存、Bump、Git 提交、发布各自完成情况 |
| 验证证据 | 编译/域重载、相关测试、Play、适配、项目校验、新消费部署的日期与范围 |
| 未完成项 | 消费验证、构建/平台、手工检查、已知限制及负责人/下一步 |
| 草稿恢复 | 原 platformer/工作区保护位置、是否恢复、仍保留的用户改动 |

阶段二首次部署直接选择最终 ui-game，不先部署 base。已有消费项目框架升级必须通过 `Ember/UPM Manager`；升级包不自动更新已部署 Assets。部署记录与 `ProjectSettings/EmberTemplateBaseline~/` 原稿一起保留，具体安装和产品实施按[03 新项目实施](03-consumer-project.md)执行。

## 8. 本阶段必读入口

- [仓库 CLAUDE.md](../../../CLAUDE.md)
- [通用术语](../../../Packages/com.ember/Documentation~/maintenance/glossary.md)及开工时正式活动模板的专属术语
- [仓库架构与模板约束](../../../Packages/com.ember/Documentation~/maintenance/repository-architecture.md)
- [业务目录与隔离](../../../Packages/com.ember/Documentation~/maintenance/business-directory-layout.md)
- 编写 C# 前：[编码规范](../../../Packages/com.ember/Documentation~/maintenance/coding-conventions.md)、[API 速查](../ember-api-reference.md)
- [编译与测试规范](../../../Packages/com.ember/Documentation~/maintenance/unity-validation.md)
- [包安装说明](../../../Packages/com.ember/README.md)、[消费依赖声明](../../../Packages/com.ember/Dependencies~/README.md)

本文件中的源码相对链接供框架仓库实施核对。正式模板说明必须复制其必要契约并引用实际安装包内公共文档，不能让消费项目依赖开发机绝对路径或未随包交付的 docs/dev。
