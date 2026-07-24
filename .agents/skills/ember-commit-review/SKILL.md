---
name: ember-commit-review
description: >-
  Use when the user wants to review local changes before committing, mentions
  "/commit-review", "看看改动", "有什么要提交的", "review changes", "检查提交",
  or asks "which files should I commit" / "what's changed".
  Do not use for reviewing PRs from other branches or for code quality review.
---

# ember-commit-review — 提交前变更审查

## 概述

审查本地未提交的改动，分类为"需要提交 / 需要忽略 / 需要丢弃"三类，
对需要提交的文件按 `docs/dev/contributing.md` 规范推荐提交信息。

**只读分析，不自动执行任何 git 操作。**

---

## 前置条件

1. 读取 `docs/dev/contributing.md`，缓存提交格式规则
2. 确认当前分支：
   - 若在 `main` 分支：提醒当前处于初始搭建期，可直接提交，后续应切到 feature 分支
   - 若在 `feature/*` 分支：按正常流程处理

---

## 执行步骤

### Step 1: 收集变更

运行以下命令，收集完整的变更信息：

```bash
git status --short       # 列出所有变更文件
git diff --stat          # 已修改文件的统计
git diff --cached --stat # 已暂存文件的统计
```

对于已暂存和未暂存的改动，分别输出文件列表。

### Step 2: 读取变更内容

对每个改动的文件，读取关键的 diff 内容（跳过二进制文件和大文件）：

- `.cs` 文件：读取完整 diff，关注新增/删除的类、方法、属性
- `.asmdef` 文件：读取完整 diff
- `.meta` 文件：只确认是否与其源文件成对出现
- `manifest.json` / `packages-lock.json`：读取 diff，关注包的新增/版本变更
- `CLAUDE.md` / `docs/*.md`：读取 diff，关注新增章节
- 其他文本文件：读取 diff 前 50 行

### Step 3: 分类

将每个文件分入以下三类之一：

#### ✅ 需要提交

满足任一条件：
- 手写的源码文件（`.cs`、`.asmdef`）
- 与其源码文件成对出现的 `.meta` 文件
- 项目配置文件（`manifest.json`、`.claude/settings.json`）
- 文档文件（`CLAUDE.md`、`docs/*.md`）
- 场景/预制体/资源文件（`.unity`、`.prefab`、`.asset`）

#### ⛔ 需要忽略（已由 .gitignore 管理、无需操作）

满足任一条件：
- 路径在 `Library/`、`Temp/`、`Logs/`、`UserSettings/` 下
- 路径匹配 `.gitignore` 中的规则
- 个人 IDE 配置文件（`.vscode/`、`.idea/` 等，取决于 .gitignore）

#### ⚠️ 需要丢弃/还原

满足任一条件：
- 仅用于调试的临时修改（如 `Debug.Log` 注入）
- 明显是误改的自动生成文件
- 与本次改动无关的意外修改

#### ❓ 需要确认

以下情况标记为"需要确认"而非直接分类：
- 只有 `.meta` 变化而源文件没变（可能是 Unity 编辑器自动调整，通常应提交）
- `packages-lock.json` 有变更但 `manifest.json` 没变（可能是版本解析变化）
- `ProjectSettings/` 下的变更（可能是 Unity 编辑器自动修改）
- 新增文件没有对应的 `.meta`、或反过来

### Step 4: 生成提交建议

对"需要提交"的文件，按改动内容分组，每组推荐一条提交信息：

分组原则：
- 同一功能模块的改动归为一组
- 不同类型的改动（feat / fix / chore）分开提交
- 每组 3-10 个文件为宜，超过则考虑拆分

提交信息格式（严格按 `docs/dev/contributing.md`）：
```
<type>(<scope>): <subject>
```

**语言规则**：
- `type` 和 `scope` 保持英文小写（符合 Conventional Commits 规范，便于 changelog 工具解析）
- `subject` 使用中文，简洁描述改动内容，结尾不加句号
- 示例：`docs: 将文档按受众重组为 user/ 和 dev/ 目录`、`feat(core): 新增 EventBus 发布订阅系统`

scope 从文件路径推断：
- `Assets/Ember/Core/` → scope: `core`
- `Assets/Ember/UI/` → scope: `ui`
- `Assets/Ember/Resource/` → scope: `resource`
- `Assets/Ember/Scene/` → scope: `scene`
- `Assets/Ember/Audio/` → scope: `audio`
- `docs/`、`CLAUDE.md` → 无 scope
- `.claude/`、`Packages/` → scope: `chore`

每组提交必须包含：

1. **文件统计**：标注该组共多少个文件，其中新增(N)、修改(M)、删除(D) 各多少
2. **批量暂存命令**：一个可直接复制执行的 `git add` 命令，包含该组所有文件路径。文件路径按字母排序，用空格分隔。如文件过多（超过 20 个），考虑拆分为多个 `git add` 命令或使用目录级 `git add`（如 `git add Assets/Plugins/`）。注意：`git add` 对新增、修改、删除（跟踪文件被删除）的文件均适用。

### Step 5: 处理建议

对"需要忽略"和"需要丢弃"的文件，给出明确建议：

| 分类 | 处理方式 |
|------|----------|
| ⛔ 需要忽略 | 已在 `.gitignore` 中，无需操作 |
| ⚠️ 需要丢弃 | `git checkout -- <file>` 或 `git restore <file>` |
| ❓ 需要确认 | 列出文件并解释为什么需要确认，让用户决定 |

---

## 输出格式

```markdown
## 📋 变更审查报告

**分支**：`<当前分支>` [`<阶段>`]

---

### ✅ 建议提交（共 N 组）

#### 提交 1

```text
<推荐提交信息>
```
（X 个文件：+新增 A，~修改 B，-删除 C）

| 文件 | 状态 | 改动摘要 |
|------|------|----------|
| path/to/file.cs | 新增 | 新增了 XXX 方法 |
| path/to/other.cs | 修改 | 调整了 YYY 逻辑 |
| path/to/old.cs | 删除 | 已迁移至新位置 |

```bash
git add path/to/file.cs path/to/other.cs path/to/old.cs
```

#### 提交 2

```text
...
```

---

### ⚠️ 建议丢弃（共 M 个文件）

| 文件 | 原因 | 命令 |
|------|------|------|
| path/to/debug.cs | 临时调试代码 | `git restore path/to/debug.cs` |

---

### ❓ 需要确认（共 K 个文件）

| 文件 | 说明 |
|------|------|
| ProjectSettings/xxx.asset | Unity 编辑器自动修改，通常应提交 |

---

### ⛔ 已忽略（共 J 个文件）

已在 .gitignore 中，无需处理：`Library/...`, `Temp/...`
```

---

## 易错点

- `.meta` 文件必须和源文件一起提交，不能只提交一个
- `packages-lock.json` 的变更往往伴随 `manifest.json` 的变更，分开检查
- subject 部分用中文描述，不要混用英文；type 和 scope 保持英文小写
- 不要建议用 `git add .`，每次提交都精确指定文件
- `ProjectSettings/` 的变更通常是 Unity 自动产生的，但应该提交（除非是 UserSettings）
- 如果 diff 中出现了 `.csproj` 或 `.sln` 变更，检查是否因为新增/删除了 `.asmdef`

---

## 验证

- 确认推荐的分组之间没有重叠文件
- 确认推荐的提交信息格式符合 `docs/dev/contributing.md`
- 确认没有遗漏任何 git status 中列出的文件
