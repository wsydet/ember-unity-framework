# Ember 仓库代理规则

## AI 分阶段工作流

新项目、较大功能和跨对话接续先读[公共工作流规范](Packages/com.ember/Documentation~/maintenance/ai-workflow.md)。调用 `ember-solution-design` 并说明需求后，先按工程量推荐完整多对话或简化单对话模式，由用户选择；已有指定或接续任务沿用。完整模式由主对话统一调度阶段、收取结果和安排返工；简化模式在当前对话实施并自检。验收地点独立于模式，消费端待验收时保留任务与证据；满足所选验收条件后由主对话归档清理。未调用技能的普通小修复和连续调试可简化，已有授权不重复审批。

## 项目测试与 Unity MCP（强制）

理解简化用语时先读[通用术语字典](Packages/com.ember/Documentation~/maintenance/glossary.md)，再按真实活动模板读取 `Assets/Game/Documentation/<template-id>/glossary.md`（若存在）。代码修改完成后必须主动通过 Unity MCP 请求重编译并确认本轮编译/域重载完成；详情见下方公共规则。

需要测试或进行 Unity 验证前，完整阅读 [项目测试与 Unity 编译验证规范](Packages/com.ember/Documentation~/maintenance/unity-validation.md)。该公共规则适用于所有模板：默认只运行新增或相关部分用例，全量重测或预计耗时较长须获用户明确授权；MCP 断连时先检查待编译改动并尝试触发编译，无法送达则立即提示用户手动触发编译，不盲目重复结果不明的请求；随后每隔 30 秒重试，最多 10 次，全部失败后停止并请用户确认 MCP 状态。

## UnityFarm 改动分流（强制）

在处理从 UnityFarm 发现、验证或提出的任何改动前，必须先完整阅读
[`Packages/com.ember/Documentation~/maintenance/unityfarm-change-routing.md`](Packages/com.ember/Documentation~/maintenance/unityfarm-change-routing.md)。

必须先把改动判定为“仅 UnityFarm 项目”“框架升级”“模板升级”或“框架 + 模板升级”，再开始落盘。
不得直接修改 UnityFarm 的 PackageCache，也不得把 UnityFarm 的产品业务误收进通用框架或模板。

其余架构、编码、模板保存和 Unity 验证约束见 [`CLAUDE.md`](CLAUDE.md)。
