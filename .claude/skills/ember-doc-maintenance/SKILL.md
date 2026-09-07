---
name: ember-doc-maintenance
description: >-
  维护 Ember 项目的全部文档，按当前源码更新过时说明，合并重复文档，删除已被替代的临时材料并修复索引。
  用于“文档维护”“清理旧文档”“更新所有文档”或调用 ember-doc-maintenance。
  单个模块 API 生成用 ember-generate-doc；仅同步依赖清单用 ember-package-scan。
---

# Ember 文档维护

以当前工作区为事实基线。用户要求维护即授权范围内的文档编辑与有依据的删除；不自动改业务代码、发布或提交。仅要求审计时保持只读。

## 建立基线

- 读取根目录 `CLAUDE.md`、适用的 `AGENTS.md`、`docs/README.md`，记录当前 git status，包括已暂存、未暂存、未跟踪文档。先保存将要大改/删除的当前版本到 `.utmp/ember-doc-maintenance/before/`，不能用 HEAD 覆盖用户未提交内容。
- 读取 `ProjectSettings/ProjectVersion.txt`、`Packages/manifest.json`、`packages-lock.json`、embedded `package.json`、各模块 asmdef 和相关模板元数据。工作区实现、包版本、已发布 tag、模板版本、编辑记录与部署记录分别表述。
- 在仓库根运行 `python .agents/skills/ember-doc-maintenance/scripts/audit_docs.py --root . --report .utmp/ember-doc-maintenance/before.json`。使用可用 Python 3 解释器；脚本优先用 rg，缺失时用 git。

## 逐份核对

1. 按清单完整阅读自有文档，给每份标记“保留 / 更新 / 合并后删除 / 待证据”。长文分段阅读，不能只看标题或搜索日期。
2. 对照源码验证路径、公开签名、默认值、生命周期、菜单、配置位置与示例。特别检查 Manager/可选 Module 装配、无创建查询、UI Page/Logic/Binding/Item 边界，以及异步命名与真实加载行为。XML 注释也可能过时，以实现为准。
3. 优先更新现有权威文档。保留真正未实施的设计并明确状态；测试旧勾选只作为历史记录，不当作本轮通过。对外部项目研究标明来源日期和未复核范围。
4. 只有实现已经完成、内容已被权威文档覆盖且无独有待办时，才删除临时计划/备份。先迁移必要设计、测试和教训，再搜索所有引用更新入口。文件老、无入链或名字含 TEMP 都不是单独删除依据。
5. 更新 `docs/README.md` 与受影响 Skill 的路径。Skill 在 `.agents/skills/`；兼容目录若为 junction，不重复写两份。保护目录拒绝写入时走平台审批，不绕过权限。

## 范围边界

- 忽略缓存、构建物和工具产物；第三方说明、版权许可及历史 CHANGELOG 保留来源语义，不当过时项目指南删除。
- `Templates~/*/Assets` 与 `ParentSnapshot` 是受控快照，不直接编辑、复制或手工重算哈希。遵循项目中心的模板保存流程；需要同步时明确记录。
- 同一工作区可能有其他任务修改源码。保留与本轮文档维护无关的改动，不做清理式 reset/restore。

## 验证与交付

- 执行脚本 `--strict`，修复本地链接目标不存在的问题。脚本不检查标题锚点、API 语义、完整 Markdown 语法或外链可用性，这些须另行核对。
- 搜索被删除文件名、旧路径、旧类型和错误版本叙述；检查索引覆盖，阅读相对于本轮基线的 diff。
- 如修改维护脚本，用隔离小项目验证有效链接、坏链接、编码/空格路径、围栏示例和第三方排除，不只检查语法。
- 仅文档/脚本变更不运行 Unity 编译；涉及代码/资源时严格按 CLAUDE.md 通过 Unity MCP 验证，工具不可用则如实列出未验证项。
- 最终报告更新/删除/保留范围、删除理由、验证结果与复用调用方式。完整审计可交付逐文件清单；不要把绝对本机路径、时间戳或本轮状态固化进 Skill。
