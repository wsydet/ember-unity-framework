---
name: ember-commit-review
description: >-
  审查本地待提交内容，区分已暂存、未暂存和未跟踪文件，按项目规范给出分组与提交建议。用于检查提交、看看改动、/commit-review；不用于其他分支 PR 的代码审查。
---

# Ember 提交前审查

## 建立当前项目基线

在用户指定的仓库根目录执行；多仓库逐个记录真实根目录、分支和状态。读取存在的 AGENTS.md、CLAUDE.md、CONTRIBUTING.md 或 docs/dev/contributing.md，遵循当前项目规则，不假定消费项目包含框架开发文档。

收集 `git status --short --untracked-files=all`、`git diff --stat`、`git diff --cached --stat`。记录 index 与工作区的区别，同一文件可以同时有已暂存和未暂存改动。未跟踪文件也须读取，不能仅以 git diff 为空宣称工作区干净。

当界面与命令行不一致或检查即将发布的结果时，用 `git -c core.untrackedCache=false -c core.fsmonitor=false status --short --untracked-files=all` 复核。检查目录联接/符号链接的真实目标；`.claude/skills` 可能指向 `.agents/skills`，同一实体不能误当两套需维护的源文件。忽略规则不影响已跟踪文件；发现双路径跟踪时报告，不能删掉链接目标或擅自清理索引。

## 审查与分组

- 完整阅读源码和 asmdef diff；对新增文件读取内容。二进制资源查看用途、元数据及匹配的验证证据，不能凭文件名判断无用。
- 源文件和 .meta 配对，检查 GUID 改变与引用影响。manifest 和 lock 分开核对；只有 lock 或 meta 变化不等于误改。
- 将变更分为应提交、已忽略产物、待确认归属。调试文件或自动生成文件先确认来源；不自动 reset、restore 或删除其他任务的修改。
- 按独立功能/修复/文档分组。列出每组文件及新增、修改、删除统计，配对给出适合当前 shell 的显式 `git add -- <paths>` 与 `git commit -m ...` 建议；不使用 `git add .`。

无项目提交格式时，默认 `<type>(<scope>): 中文简述`，type/scope 用英文小写、scope 可省略，主题不带句号。按目录或实际功能选择 scope，不要求消费项目采用框架仓库的分支名。

用户只要求审查时不提交。已明确要求代提交时，在授权范围内执行，无需再次批准相同动作；提交不自动授权 push 或发布。提交后检查实际 commit 和完整工作区状态，明确剩余变更。代码/资源验证按项目规则执行，静态检查不代表 Unity 编译通过。
