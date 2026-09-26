---
name: ember-vn-custom-node
description: 为 Ember 视觉小说实现框架没有内置的剧情行为——玩家输入名字、把一段小游戏插到步骤之间、调用外部系统并等它返回。先调研现有能力并把方案归到三级阶梯（二级步骤预设 / 模块桥接节点 / 自定义节点脚本），通过本地方案确认页面让用户选定一级并确认完整计划，确认后按已实现的自定义节点扩展点落地并校验。不负责改写剧情文本、导入图片或发布框架与模板。
---

# 自定义节点方案与实施

把「用户想要的效果」变成**已实现扩展点上的可落地方案**，先让用户在页面上确认，再实施。

不要重新设计扩展点：契约已经实现并封存在模板里，见
[扩展点契约速查](references/extension-contract.md) 与模板内 `Documentation~/visual-novel/CustomNodes.md`。

## 入口检查

经 Unity MCP 确认项目与实例，调用
`Ember.Core.Editor.EmberProjectSetup.GetTemplateSkillExecutionBlockReason("ember-vn-custom-node")`。
返回非空原因时**停止业务写入**，报告原因并指向项目中心的模板技能预览 / 同步；API 缺失说明框架不支持此工作流，
不能凭目录存在跳过。**无 Unity MCP 时只能做只读方案与草稿**，不能声称已实施，页面也会禁用确认。

读取目标项目规则，并核对扩展点是否真的存在：模板低于 `0.12.0` 的项目没有
`NovelCustomStepSO` / `INovelStepService` / `NovelStepServiceBridgeSO`。此时只报告所需业务升级，
**不要**向旧项目写入它无法执行的字段，也不要自己实现一套替代扩展点。

## 第一步：调研，不要直接开写

1. 读 [扩展点契约速查](references/extension-contract.md)，再按需读模板内的
   `Documentation~/visual-novel/CustomNodes.md`、`Authoring.md`，并核对用户提到的节点所在 Story/Chapter 的真实资产。
2. **判断落在哪一级阶梯**，逐级比较并写下理由：

   | 级 | 形态 | 代码量 |
   |---|---|---|
   | 1 `preset` | 二级步骤预设（`NarrativeStepPresetSO` / `NarrativeStepGroups`） | 零代码 |
   | 2 `bridge` | 模块桥接节点 + 业务 Module | 只写业务 Module |
   | 3 `script` | 继承 `NovelCustomStepSO` 写专属脚本 | 写脚本（+ 可选 Module） |

   **默认走第 2 级**：能用预设解决就不要写脚本；小游戏 / 外部系统优先用桥接节点，
   把玩法逻辑留在业务 Module 里，节点侧零代码。只有第 2 级确实表达不了时才降到第 3 级。
3. 找出这一步需要的：剧情步骤插入位置、变量（ID / 类型 / 作用域）、业务 Module 与请求键、
   是否需要 UI 页面、以及**旧存档兼容结论**（自定义节点参数进指纹，说话人变量不进）。
4. 读 [实现与目录规范](references/implementation-rules.md)，确认要新增的文件都落在允许的位置。

## 必须打开本地方案确认页面

必须先读并执行 [确认服务操作说明](references/confirmation-workflow.md)。本技能随附
`scripts/confirmation.py` 与 `assets/confirmation.html`，用本机回环 HTTP 服务生成页面，
把草稿 / 确认 / 取消写入本批 `.utmp/vn-custom-node/<批次>/`。

**不得**用聊天列表、静态预览或 `request_user_input_async` 代替此页面；工具调用成功不等于页面已打开，
更不等于用户确认。也不能套用图片导入专用的 `confirmation.py`。

本技能每次执行都必须生成并**实际打开**可交互页面，让用户选定阶梯并确认完整计划；用户已给出的偏好预填并保留，
不重复询问。仅当用户明确要求跳过页面时按其指令处理；否则即使已授权执行，也先让用户在页面选择本批具体方案。

页面至少提供：**阶梯选择**（三级中选一级，被标记不可用的不可选）、
**完整计划预览**（要新增 / 修改的文件与归属、目录、脚本名与脚本 ID、变量、剧情步骤与插入锚点、UI 页面、风险、验证方式）、
**保存草稿**、**明确提交**、**取消**。批次目录固定使用 `.utmp/vn-custom-node/<批次>/`，
用可保留 session ID 的终端启动服务（Windows 后台启动用 `Start-Process -WindowStyle Hidden`，保留 PID 和日志），
再用客户端浏览器工具实际打开返回的 URL，观察标题、阶梯单选、预览和按钮正常后，把可点击链接发给用户。
打开失败就报告并修复，不能仅生成 HTML 文件便声称已展示。

用户可先保存草稿，代理读取后补全真实计划，再刷新同一页面供最终提交。未准备完整计划、存在未解决歧义、
入口检查未通过或 MCP 未验证时禁用最终提交。预览后改选项会使旧计划失效，必须重新预览再确认。

只有本批 `status=confirmed` 且计划、选项与当前项目指纹一致时才执行。代理**不得**替用户点击提交、
调用确认接口或伪造回执。草稿、取消、关闭页面、等待超时均不触发写入；取消后停止本批。
用户提交后运行 `verify --work <本批目录>`，回读回执、复核原有 MCP 与身份条件，再执行已选计划，
**不另加一轮聊天许可**。源数据、目标或方案变化时使确认失效，回到页面展示新计划。

等待期间保留服务，明确告知「方案确认页面已打开，等待提交，尚未实施」，不能把任务标为已完成；
页面不保证自动唤醒代理，提示提交后回到当前任务继续。完成或取消后关闭本批服务，保留选择、回执和结果记录。

## 按选定的阶梯实施

**`preset`（零代码）**：用 `Game.Narrative.Editor.NarrativeStepGroups` 原位包装或插入既有指令组，
保留原 command / line / action ID；不要为了「自定义」而新建脚本或资产。

**`bridge`（只写业务 Module）**：新增业务 Module 实现 `INovelStepService` 并
`EmberServiceLocator.Register<INovelStepService>(this)`；在剧情清单里登记
`NovelStepServiceBridgeSO` 资产，填请求键、结果变量与**大于 0 的超时**；插入「自定义节点」步骤。
玩法逻辑全部留在 Module 里，用 `IEmberUpdate` 自己推进。

**`script`（写专属脚本）**：新增 `NovelCustomStepSO` 子类，只做「启动业务 + 接收结果」，
**不要把玩法逻辑写进脚本**；运行状态放 `context.State`；实现 `Validate` 让剧情校验能提前报错。

任何一种阶梯都必须遵守：

- **只新增文件，不修改模板自带文件。** 模板管理 `Assets/Game`、`Assets/Resources`、`Assets/Ember/Editor`、
  `Assets/Settings`、`Assets/GameResource` 五个目录：完整重新部署会覆盖，补丁增量只保留本地独有内容与单方修改。
  需要改 `NovelCommandKind`、校验器、`NovelSession` 之类的模板文件时，**停下来报告**，不要就地改。
- **脚本资产必须登记到剧情的「自定义节点脚本清单」**（流程编辑器 → 章节总览）。
  步骤里只保存 `ScriptId`；未登记会在**剧情校验阶段就报错**，不会等到运行时才炸。
- **既不 `Complete` 也不 `Fail` 会让剧情永久卡住**：运行器不会自己超时。桥接节点因此强制要求超时。
- **关键推进不要放 `OnTick`**：会话被暂停（菜单 / 历史 / 存档页 / `AcquirePause`）后逐帧 Tick 提前返回，
  `OnTick` 就不再被调用。持续推进交给业务 Module 的 `IEmberUpdate`。
- **运行状态放 `context.State`**，不要放脚本字段——脚本资产是共享的。
- **`OnCancel` 必须收干净**：退订事件（`EmberEventGroup` 可一键退订）、释放暂停、通知业务模块中止。
  读档 / 退出 / 故障都会走到它。
- 业务模块的迟到回调先查 `context.IsAlive`，再写变量或完成。
- **正式 UI 必须经 UI 中心制作**（`EUICreationService.TryBuildPlan` 预检 → `Create`；
  绑定调整后用 `EUIBindingCodeGenUtility.TryRegenerateCode` 重新生成），不能手搓 Prefab 或手拼 binding。
- **两套指纹规则不要混**：自定义节点的 `ScriptId` 与脚本资产内容**进**剧情指纹（改参数会让旧档不兼容）；
  说话人变量绑定**不进**指纹（纯表现层，旧档不受影响）。
- 走 MCP 检查错误，并运行一次真实剧情校验；**静态校验通过不等于画面通过**。

## 验证与交付

- **Unity 编译验证只走 Unity MCP**：改完 C# / 程序集 / 场景 / 资源后，用 MCP 触发刷新并读取编译结果；
  MCP 可用时只做一次有界确认，只有 MCP 明确报告正在编译时才等待该次编译，禁止无意义轮询。
- MCP 不可用、调用超时或首次查询失败时**立即停止编译验证**：不要用等待 Unity 自动刷新、反复读 `Editor.log`、
  轮询 Unity 进程、启动 BatchMode、改生成的 `.csproj` 或跑 `dotnet build` 来替代，也不得声称「编译通过」。
  此时最终回复必须原样写明：

  > 当前 Unity MCP 未连接或不可用，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。

- 模板带 `Game.Narrative.Tests` 时，至少跑 `NovelCustomStepRegistryTests` 与 `NovelSessionTests`
  的自定义节点分部——它们覆盖调度、取消、桥接超时与指纹划界。
- 交付报告写清：选中的阶梯与理由、文件清单（含 `new` / `template-owned` 归属）、脚本 ID 与资产路径、
  是否已登记进剧情清单、步骤插入锚点、变量 ID / 类型 / 作用域、请求键与超时、`Abort` 行为、
  UI 是否经 UI 中心、**指纹影响与旧档兼容结论**，以及实际完成与**未完成**的验证。
- 明确要求用户在做完手动验收（实际输入 / 小游戏 / 结果回写各走一遍）之后再决定是否封存业务与发布；
  **本技能不执行 SaveTemplate / Bump / 任何发布动作**。

## 参考文件

- [扩展点契约速查](references/extension-contract.md)：三级阶梯判定、钩子与上下文、指纹规则、运行期契约、错误码。
- [实现与目录规范](references/implementation-rules.md)：成果归属、五个模板目录、Module 与 UI 中心规范、Unity 验证规则。
- [确认服务操作说明](references/confirmation-workflow.md)：启动参数、方案草案格式、回执语义、执行闸门、维护自测。
