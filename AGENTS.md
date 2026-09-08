# Ember 仓库代理规则

## UnityFarm 改动分流（强制）

在处理从 UnityFarm 发现、验证或提出的任何改动前，必须先完整阅读
[`Packages/com.ember/Documentation~/maintenance/unityfarm-change-routing.md`](Packages/com.ember/Documentation~/maintenance/unityfarm-change-routing.md)。

必须先把改动判定为“仅 UnityFarm 项目”“框架升级”“模板升级”或“框架 + 模板升级”，再开始落盘。
不得直接修改 UnityFarm 的 PackageCache，也不得把 UnityFarm 的产品业务误收进通用框架或模板。

其余架构、编码、模板保存和 Unity 验证约束见 [`CLAUDE.md`](CLAUDE.md)。
