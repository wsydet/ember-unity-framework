# 柔光玻璃 UI 与 GM：阶段归档

- 任务：20261117-soft-glass-ui-gm；归档日期：2026-11-17，Asia/Shanghai。
- **状态：阶段资料已归档，任务未完成，仍需返工；未清理临时材料。**
- 用户在独立复验A2后要求“执行收束归档吧”。本次保存当前阶段，不将该请求解释为接受未修复缺陷、延期设备验证或放弃模板交付。
- 当前有效需求R4/F01—F04，实现A1-RW1，独立验收A2；R2视觉确认继续有效。历史材料原字节保留，旧状态文字以本页和A2结论为准。

## 当前交付和验收

框架提供管线无关的模态背景、玻璃Graphic与独立URP17采样后端；Assets包含24个EUI资源、22个Binding、5个页面注册、公共弹窗、GM/FPS悬浮入口和消费扩展示例。旧UI和GM保留可选路径。当前Assets属于base待回流草稿，尚未正式保存或分发。

| 项目 | 当前结论 |
| --- | --- |
| SG-IA-01：取消mask后的失效租约 | A2复验通过，当前交付基线下关闭 |
| SG-IA-02：输入与焦点 | 原始字符/Submit/Move穿透修复通过；双Popup隐藏/重显交互和返回链仍失败，保留问题未关闭，剩余子项P2 |
| AC03 | 当前Windows Editor范围通过 |
| AC04 | 失败，需修复上述隐藏/返回链子项 |
| AC09 | 真实触摸、设备旋转、安全区与键盘覆盖待设备验证 |
| AC11 | base正式中心保存/分发未完成 |
| AC13 | R2实际预览已获用户明确确认；原五图无漂移 |

其余AC的范围及原始结论见[独立验收报告A2及A1历史](records/acceptance.md)；不能把19/19相关EditMode和40/40运行回归通过写成全部行为或整个任务通过。A2另写的补充探针发现隐藏组合缺口，随后通过隔离对照确认，未删除失败记录。

## 正式使用说明

长期能力说明沿用[Popup背景效果](../../../Packages/com.ember/Documentation~/ui/popup-backdrop.md)、[柔光玻璃控件](../../../Packages/com.ember/Documentation~/ui/soft-glass.md)和[base接入说明](../../../Assets/Game/Documentation/base/soft-glass-ui.md)，本归档不复制第二套API手册。本次未改产品文档或代码，原验收产品基线保持。

## 记录与原始证据

- [需求R4](records/requirements.md)、[方案D1](records/design.md)、[实施与历史](records/implementation.md)、[R2返工](records/revision-r2.md)、[A1-RW1实施](records/rework-a1.md)。
- [独立验收A2/A1](records/acceptance.md)、[视觉确认](records/preview-confirmation.md)、[交接记录](records/handoff.md)。
- [A2原始证据](records/evidence/reacceptance-a1/)、[A1原始证据](records/evidence/acceptance/)、[实施返工证据](records/evidence/rework-a1/)、[R2实际预览](records/evidence/previews/r2/)。
- [剩余事项及恢复步骤](PENDING.md)、[路径/长度/SHA256清单](archive-manifest.json)、[逐文件归档核验](archive-verification.json)、[阶段收束状态](closure.json)。

完整原始归档 `task-evidence.tar.xz` 由维护者在本地留存，可按上述指纹清单复核。

完整证据包在source-task/下保留本任务所有原始文件、备份、探针、参考图和历史证据，不要求原临时路径存在才能读取。使用标准tar解包到任意目录即可按原相对路径查阅。records直接展开当前报告和常用证据，其历史机器路径、临时路径和提示词是来源记录，不是本归档的读取依赖。实际源码仍以项目当前文件及交付manifest核对。

## 基线和操作边界

Git为codex/local-snapshot-20261111-162819 / 16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2，工作区未发布；Unity6000.5.4f1、embedded Ember0.25.5、URP17.5.0。正式编辑topdown-2d0.1.2。交付manifest覆盖890文件，SHA256为816A14A2C661BBF226BCC3515C67765A41AEB9569CC1B808B3248507733DDA33。归档时实际重算结果见closure.json。

A2本轮MCP编译/域重载完成，UI MVID aad4d568-81c2-43f1-87df-31511fd34c11；19/19相关EditMode、40/40重跑矩阵及独立补充探针分别保留。关闭消费者60帧捕获10900→10900，0边界/RT/相机段/专用材质。最终Console空、Play=false、场景未脏。这些是A2证据，本次归档为纯文档和文件保存，没有新增Unity测试或产品修改。

临时任务材料继续保留用于实施返工；本轮没有清理、模板切换/保存/Bump/同步/部署、提交或发布。全部必需项完成或有用户明确延期决策后，仍需回入口核对最新独立验收，才能标完成并执行已归档临时材料的精确清理。
