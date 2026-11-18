# 实施对话启动提示词

接续任务 `20261117-soft-glass-ui-gm`，本对话负责开发实现与自检，不承担独立验收。

项目根 `C:/Users/wuyu/My/ember-unity-framework`；同一任务目录 `.utmp/ember-workflow/20261117-soft-glass-ui-gm/`。

先读项目 AGENTS.md / CLAUDE.md、UPM 实际 Ember 包内公共工作流与交接模板、本任务 handoff.md、requirements.md、有效 design.md、baseline 和必要 references。完整阅读架构/目录/编码规范与 API 速查；需要任何 Unity 验证前完整阅读 unity-validation.md；应用 ember-eui-build 制作正式 UI，使用 Unity MCP 时应用 unity-mcp-orchestrator。

前置状态：2026-11-17，handoff H4 明确方案 D1 已完成，design.md 为当前有效方案，可以开始实施 S0/S1。没有 implementation.md 或任何本轮运行验证。先核对是否有更新修订；如果方案文件缺失/失效，报告具体缺口，不擅自补方案后跨阶段实现。需求 R3 已确认 A01 悬浮小圆点、A02 通用控件 + 公共弹窗 + GM 并保留旧 UI，以及 Popup 默认新模糊遮罩；无需重复询问这些偏好。

按 design.md 的 S0—S5 顺序执行，先完成真实工作区备份与渲染接线最小实证，再批量制作24个Prefab目标（2骨架、5具体Page、17Item）。方案关键决定必须承接：

- 独立 SoftGlassLibrary + 运行时 EUIAppearanceConfig，CommonSkin、GmPresentation、PopupMaskEffect 分开。本轮不新增正式 Ember 扩展ID/hash，不替其他任务重签收当前 topdown 扩展配置（记录版本0.1.1，与正式编辑0.1.2不一致）。
- UI core 的效果/采样契约管线无关；独立URP17后端使用相机段与RenderGraph。实际场景Main Base+UICamera Overlay、PC Renderer只有SSAO，尚无Blur接线。URP17.5已不支持靠旧Compatibility开关关闭RenderGraph，不用旧Shader或仅OpaqueTexture宣称完成。
- S1用动态世界色块、HUD文字/帧号、Popup独特文字和上层标记证明采样含下层UI、排除本层前景、跨Camera同帧可读及最终resolve顺序；检查空闲layer池、相机/输入排序、MSAA/UV/颜色空间。S1不通过先定向修后端，不批量做假效果资产或把默认改回Solid。
- 效果优先级与旧配置迁移按design第4节：Inherit=0、全局默认Blur、显式Solid、UseMask双门禁、旧maskColor作为明确tint保留。贯通CreationRequest/绑定快照/Apply/Bridge/契约，回池清理lease、材质与回调；FullScreenPopup首次采样必须在隐藏下层前准备。
- GM生成来源已核实：当前CSharpLogicImplementationData普通重新生成保留现有.cs受管逻辑，首次才用Framework.tpl，Binding每次覆盖；没有另一份GM业务代码模板。仍不改旧GM受管区，新GM通过Business用户partial/业务helper实现，保留旧GM/GUID/注册。GameInit仅改GM选择调用，保留topdown试玩、输入等既有内容。
- 当前topdown不切换；新增UI先是活动Assets内base待回流集成草稿，不保存为topdown独有。按design第3节保护草稿，真正base加载/保存/父同步属于后续具体操作门槛。没有授权先完成所有不受影响的代码/资产/自检/预览，不能借模板门槛停掉已授权范围，也不能宣称base已保存/分发。
- 实施时先读取ember-eui-build，正式创建/绑定/注册走EUI开发中心，读取真实生成字段再写逻辑；DefaultLibrary和已有资源不整体重建。局部Glass Graphic遵循新圆角/Stencil/RectMask2D契约，复用现有按钮/输入/资源加载行为，保留本地改动。

建议性能预算是设计建议，未获用户阈值确认；真实硬件/API和真机环境未知，按design第8节短定向测量记录可得CPU/GPU/RT、限制与关闭开销，不虚构移动达标。超出已授权的长时/全量测试仍须处理具体授权。

核对实际 Git/未提交改动、正式活动模板、包版本/来源和扩展配置，与基线有差异时更新记录并判断影响。保留其他任务和用户改动，待改文件先备份当前内容，不能用 HEAD 当未提交现状。基线有 UI 运行时扩展、圆角、按钮等相关改动。

按有效方案与 R01—R08、AC01—AC13 完成已授权实现：

- 新增可选柔光玻璃风格，保留旧 UI/GM 的可用资产和选择路径，提供真实可复用控件、公共弹窗及消费端扩展样例。
- Popup 默认使用新模糊遮罩，无须逐页启用；纯色遮罩仍可显式选择，按方案处理全局/逐页覆盖和旧配置迁移，UseMask=false 不创建遮罩。模糊下层 UI/场景，保持前景清晰，兼容输入阻挡、点击关闭、叠层、回池和清理；默认遮罩与控件皮肤选择独立。
- 应用统一新圆角/交互状态；GM 以图 02 控制中心重组，保留有效调试功能，按已确认 A01 制作可拖动、吸边、点击打开的同风格悬浮小圆点入口。
- 维护 Shader/背景采样链路、资源释放、关闭开销与降级；交付有环境和限制的质量/性能记录，不能凭主观流畅声称达标。
- 正式 EUI Prefab、Binding、注册及生成代码经 EUI 开发中心维护；定位受管 GM 维护源，防止刷新丢失。通用机制放框架，业务 UI 在 Assets 维护源，不直接写 Templates~ 或 ParentSnapshot~。
- 更新对应正式模块说明与接入/回退指南。需要扩大范围或改变需求时记录差异，不私自降低 AC。

本任务整体已授权原范围内实现和相关定向测试，不需要重复实施审批。模板切换/覆盖草稿、模板保存/Bump、Git 提交/发布、消费项目升级/部署、全量或长时测试不在现有授权中；遇到必要项先完成不受影响的可审阅成果，说明真实缺口并按规则处理，不能绕过项目中心。未完成保存不能宣称模板已分发。

代码完成后必须主动通过 Unity MCP 请求本轮重编译，确认编译/域重载完成并读 Console，再执行新增或直接受影响的定向测试和运行 UI 行为检查。MCP 断连时按完整验证规范：检查待编译改动、尝试触发；无法送达立即提示用户手动编译，避免重复结果不明请求；每 30 秒重试最多 10 次，仍失败报告状态。不用 BatchMode/dotnet/日志轮询替代 Unity 自动编译验证。

在任务目录写 implementation.md，逐项列改动、方案偏差、编译/域重载/Console、测试范围与结果、参考图对照截图、行为/性能证据、未完成项及限制。区分实现、自检和人工签收，未执行的不记通过。更新 handoff.md 和 prompts/04-acceptance.md，使另一聊天可独立复核；交付后结束实施阶段，不自行宣布独立验收通过或清理目录。

R08/AC13 是用户明确的收束条件：保存并在聊天展示当前实际实现预览，至少覆盖通用控件、Popup 模糊遮罩、GM 控制中心和收起的悬浮入口；记录对应文件基线、展示日期及用户回复。方案图不能代替实现预览。用户未明确确认时保持待确认，不得收束、标记整个任务完成或清理；用户要求调整则交回实施并更新预览。独立验收仍须由另一聊天完成，不以用户确认替代。
