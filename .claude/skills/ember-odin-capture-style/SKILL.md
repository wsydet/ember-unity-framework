---
name: ember-odin-capture-style
description: >-
  Use when the user is satisfied with a script's Odin Inspector panel and wants to
  capture its style/patterns into the docs, mentions "/odin-capture", "记录面板风格",
  "保存编辑器样式", "捕获Odin风格", or asks to save the current panel layout as reference.
  Reads the target script, extracts Odin patterns, and updates odin-usage-notes.md.
---

# ember-odin-capture-style — 捕获 Odin 面板风格

## 概述

读取用户指定的脚本，提取其中的 Odin Inspector 面板风格和布局模式，
将**新的、未记录的模式**写入 `docs/dev/odin-usage-notes.md`，
供 `ember-odin-inspector` 后续使用。

**只读分析，用户确认后再写入文档。**

---

## 前置条件

1. 读取 `docs/dev/odin-usage-notes.md`，缓存所有已有规则
2. 读取 `Assets/Ember/Examples/OdinInspectorDemo.cs`，缓存参考脚本的现有模式

---

## 执行步骤

### Step 0: 确认目标

**如果用户调用 skill 时没有指定目标脚本，使用 AskUserQuestion 询问：**

> "请指定要捕获风格的目标脚本："

让用户提供脚本路径。拿到后再继续。

### Step 1: 解析目标脚本

读取目标脚本，提取所有 Odin 使用模式：

- 每个 Odin 特性的使用方式和组合
- 分组/布局特性的搭配（如 `[Title]` → `[Button]` 的间距方式）
- 字段声明方式（`public` vs `[SerializeField]` vs `[ShowInInspector]`）
- 按钮的排列方式（是否用 `[ButtonGroup]`）
- 数据展示方式（`[TableList]`、`[ProgressBar]`、`[EnumToggleButtons]` 等）

### Step 2: 与已有文档对比

将提取的模式与 `docs/dev/odin-usage-notes.md` 中的规则对比，
找出**文档中尚未记录的模式**：

| 判断 | 条件 | 操作 |
|------|------|------|
| 已有记录 | 模式已在文档中存在 | 跳过 |
| 新发现 | 模式不在文档中 | 记录为新增 |
| 冲突 | 模式与文档中的建议相反 | 标记为"需讨论" |

### Step 3: 生成更新建议

对每个新发现的模式，生成文档草稿：

```markdown
### N.X 模式名称

**适用场景**：...

**示例**（来自 `<脚本名>`）：
\`\`\`csharp
...
\`\`\`
```

### Step 4: 用户确认并写入

展示新增内容草稿，逐条询问用户是否加入文档。
用户确认后写入 `docs/dev/odin-usage-notes.md`。

---

## 输出格式

```markdown
## 📸 Odin 面板风格捕获报告

**来源脚本**：`<路径>`
**捕获时间**：<时间戳>

---

### 新发现模式（共 N 个）

#### 新增 1：XXX 模式

**当前用法**（来自 `<脚本名>`）：
\`\`\`csharp
...
\`\`\`

**建议写入**：`docs/dev/odin-usage-notes.md` §X 节

---

### 已有模式确认（共 M 个）

以下模式已在文档中记录，与当前脚本一致：

- §1.1 HorizontalGroup 注意事项
- ...

---

### ⚠️ 需要讨论（共 K 个）

脚本中的某些用法与文档建议不同，需要确认哪个是正确的。
```

---

## 易错点

- 只提取 Odin 特性相关代码，不提取业务逻辑
- 已经在文档中的模式不要重复添加
- 如果脚本用法与文档建议相悖，标记为"需讨论"而不是直接覆盖
- 新增内容必须给出完整的代码示例，不能只写描述
- 更新后提醒用户运行 `ember-odin-inspector` 验证文档规则的有效性

---

## 与其他 Skill 的关系

```
（调试面板到满意）
        │
        ▼
ember-odin-capture-style     →     更新 docs/dev/odin-usage-notes.md
        │
        ▼
ember-odin-inspector         →     用更新后的文档检查/优化其他脚本
        │
        ▼
ember-commit-review          →     提交
```
