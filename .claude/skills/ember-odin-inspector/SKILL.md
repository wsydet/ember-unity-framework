---
name: ember-odin-inspector
description: >-
  Use when the user explicitly invokes "/odin-inspector" or asks to
  "检查编辑器面板", "优化面板", "Odin面板", "inspector panel" with a specific script.
  Do NOT auto-trigger on vague mentions of Odin — the user must provide a target.
---

# ember-odin-inspector — Odin Inspector 面板检查与优化

## 概述

检查指定脚本的 Odin Inspector 面板定义，对照 `docs/dev/odin-usage-notes.md`
和 `Assets/Ember/Examples/OdinInspectorDemo.cs` 中的规范，
发现兼容性问题并给出修正建议。

**只读分析，用户确认后再写入。**

---

## 前置条件

1. 读取 `docs/dev/odin-usage-notes.md`，缓存所有已知问题、规则和示例
2. 读取 `Assets/Ember/Examples/OdinInspectorDemo.cs`，缓存正确的 Odin 用法示范

---

## 执行步骤

### Step 0: 确认目标（必须先执行）

**如果用户调用 skill 时没有指定目标脚本或文件夹，必须使用 AskUserQuestion 询问：**

> "请指定要检查的目标："

选项：
- "指定一个脚本文件"
- "指定一个文件夹（扫描其中所有 .cs 文件）"

用户选择后，让用户提供具体路径。拿到路径后再继续后续步骤。

**如果用户已经明确指定了脚本路径或文件夹，跳过此步骤。**

### Step 1: 收集目标文件

- 如果是单个脚本 → 读取该文件
- 如果是文件夹 → 用 Glob 扫描 `*.cs`，逐个读取

### Step 2: 解析目标脚本

对每个脚本，提取所有 Odin 相关代码：

- 所有 `using Sirenix.OdinInspector;` 等 Odin 命名空间引用
- 所有 Odin 特性标注的字段、属性、方法
- 注意区分 `public` 字段 vs `[ShowInInspector] private` 字段
- 注意 `[HorizontalGroup]`、`[ButtonGroup]`、`[FoldoutGroup]` 等布局特性
- 如果脚本完全没有 Odin 特性，报告"未使用 Odin，跳过"

### Step 3: 逐项检查

对照 `docs/dev/odin-usage-notes.md` 中的规则，逐项检查：

| 检查项 | 规则来源 | 严重度 |
|--------|----------|--------|
| HorizontalGroup 内是否有 [ShowInInspector] | §1.1 | 🔴 高风险 |
| HorizontalGroup 内是否有计算属性 | §1.1 | 🔴 高风险 |
| 连续独立 [Button] 是否未归组 | §1.2 | 🟡 中风险 |
| 是否用了 [ShowInInspector] 而可以用 [SerializeField] | §2.1 | 🟢 建议 |
| [AssetsOnly] / [SceneObjectsOnly] 空值 | §1.3 | 🟢 提示 |

### Step 4: 生成检查报告

对每个问题给出：

- **位置**：文件路径 + 行号 + 涉及的成员名
- **问题**：描述具体是什么问题
- **风险**：🔴 高风险 / 🟡 中风险 / 🟢 建议
- **修正**：给出具体的修改前后代码对比

### Step 5: 用户确认并应用

展示报告，逐项询问用户是否需要修改。
用户确认后执行修改（Edit 工具）。

---

## 输出格式

```markdown
## 🔍 Odin Inspector 面板检查报告

**目标**：`<路径>`
**检查时间**：<时间戳>

---

### 发现 N 个问题

#### 问题 1：HorizontalGroup 内使用 [ShowInInspector] 🔴

**位置**：`<路径>:<行号>`，字段 `xxx`
**问题**：...

**修正**：
\`\`\`csharp
// 改前
...
// 改后
...
\`\`\`

---
```

---

## 易错点

- **必须先确认目标再执行**，不要在没有目标的情况下自动扫描整个项目
- 不要改动脚本的业务逻辑，只修改 Odin 特性部分
- 不要把 `public` 字段改成 `[ShowInInspector] private`（反向错误）
- `[SerializeField] private` 是正确的替代方案，兼容性优于 `[ShowInInspector]`
- 如果脚本完全没有 Odin 特性，报告"未使用 Odin，跳过"
- 修改后提醒用户在 Unity 中验证编译和面板渲染

---

## 与其他 Skill 的关系

```
ember-odin-inspector    →    ember-commit-review
（检查/优化面板）              （提交）
```
