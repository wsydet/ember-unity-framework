# Ember 仓库代理规则

## 项目测试与 Unity MCP（强制）

需要测试或进行 Unity 验证前，完整阅读 [项目测试与 Unity 编译验证规范](Packages/com.ember/Documentation~/maintenance/unity-validation.md)。该公共规则适用于所有模板：默认只运行新增或相关部分用例，全量重测或预计耗时较长须获用户明确授权；MCP 断连后每隔 30 秒重试，最多 10 次，全部失败后停止并请用户确认 MCP 状态。

## UnityFarm 改动分流（强制）

在处理从 UnityFarm 发现、验证或提出的任何改动前，必须先完整阅读
[`Packages/com.ember/Documentation~/maintenance/unityfarm-change-routing.md`](Packages/com.ember/Documentation~/maintenance/unityfarm-change-routing.md)。

必须先把改动判定为“仅 UnityFarm 项目”“框架升级”“模板升级”或“框架 + 模板升级”，再开始落盘。
不得直接修改 UnityFarm 的 PackageCache，也不得把 UnityFarm 的产品业务误收进通用框架或模板。

其余架构、编码、模板保存和 Unity 验证约束见 [`CLAUDE.md`](CLAUDE.md)。
