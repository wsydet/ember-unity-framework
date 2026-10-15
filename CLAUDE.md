# CLAUDE.md — ember-unity-framework

## 必读规则

本仓库区分框架公共能力、base 业务骨架与玩法模板。开始任务先完整阅读 [仓库架构与模板开发约束](Packages/com.ember/Documentation~/maintenance/repository-architecture.md)，不得反向依赖或把可选业务提升为 Manager。

- 新增功能、文档、Skill 或 Tool 时阅读 [业务目录与隔离规范](Packages/com.ember/Documentation~/maintenance/business-directory-layout.md)。专属菜单为 `Ember/<业务分类>/<工具名>`，按正式活动模板及模块条件控制所有入口。
- 编写 C# 前完整阅读 [编码规范](Packages/com.ember/Documentation~/maintenance/coding-conventions.md) 和 [API 速查](docs/dev/ember-api-reference.md)，复用既有接口。Editor 代码独立程序集，保持序列化与运行行为。
- 涉及 C#、程序集、场景或资源时完整阅读 [Unity 验证规则](Packages/com.ember/Documentation~/maintenance/unity-validation.md)。Unity MCP 是唯一自动编译验证入口；首次连接/查询失败或超时即停止，不用日志轮询、BatchMode 或 dotnet 替代。验证未完成时必须明确提醒用户在 Unity 中手动触发编译并提供首条错误及完整堆栈。
- 模板内容只在项目 `Assets` 编辑；模板保存与 Bump 必须通过项目中心的正式保存、Bump 操作完成（见[模板开发落盘规范](Packages/com.ember/Documentation~/maintenance/repository-architecture.md#模板开发落盘规范)）。禁止直接修改已储存的 `Templates~`、`ParentSnapshot~`，或用脚本复制、手改版本号/hash 代替中心操作；中心不可用时保留项目改动并报告未完成，不得绕过。普通保存、Bump、提交、发布和消费升级分别执行与授权。保留当前草稿、GUID、布局及其他工作区改动。
- 文档维护遵循 [维护约定](Packages/com.ember/Documentation~/maintenance/documentation-maintenance.md) 和适用 Skill：先备份现状、核对源码及唯一内容承接，再删除已替代文档；严格链接检查不能替代语义或 Unity 验证。
- UnityFarm 来源改动开始前，按 [AGENTS.md](AGENTS.md) 完整阅读 [改动分流](Packages/com.ember/Documentation~/maintenance/unityfarm-change-routing.md)，先确定项目/框架/模板归属。

用户当前对话的范围与授权优先；规则链接是职责拆分，不是放宽原有约束。当前任务的未完成事项和验收边界由相应实施记录承接，不能以历史测试或发布记录代替本轮验证。
