---
name: ember-solution-design
description: >-
  Use when the user wants to discuss a feature idea, effect, or code change BEFORE any code is written,
  mentions "/solution-design", "评估方案", "设计方案", "帮我设计", "先评估",
  "don't just code it", "先别改代码", "有什么方案", "怎么实现", or describes a desired behavior
  and wants options rather than immediate changes.
  This skill acts as a gatekeeper — it MUST NOT write any code until the user explicitly confirms an approach.
---

# ember-solution-design — 方案评估与设计

## 概述

当用户提出一个功能需求或改动想法时，**先分析、出方案、等确认，再写代码**。
此 skill 的核心价值是阻止 AI 跳过讨论直接改代码——确保用户对自己的项目有完全的控制权。

---

## 核心原则

**🚫 在用户明确确认方案之前，绝对禁止：**
- 创建新文件
- 修改现有文件
- 写入任何代码

**✅ 只允许：**
- 搜索和阅读项目代码
- 分析现有架构和模式
- 输出评估报告和建议方案
- 使用 `AskUserQuestion` 让用户选择

---

## 执行步骤

### Step 0: 明确需求（必须先执行）

**如果用户调用 skill 时没有描述需求，必须先问：**

> "请描述你想要实现的效果或解决的问题。越具体越好——比如涉及哪个场景、哪个 GameObject、期望的交互方式等。"

用户描述后，提炼出核心需求并复述确认：

```markdown
**我理解你的需求是：**
1. ...
2. ...

**确认无误？**
```

用户确认后继续。

### Step 1: 调研现有代码

搜索项目中与需求相关的所有代码和资源：

**必须搜索的内容：**

| 类别 | 搜索方式 | 目的 |
|------|---------|------|
| 相关脚本 | `grep` 关键词、`Glob` 文件名 | 找到已有的类似实现 |
| 相关场景/预制体 | `find` + `Hierarchy` 路径 | 确定涉及的 GameObject |
| 依赖的框架 API | 读框架层代码 | 确认可用的框架能力（如 DOTween、UniRx） |
| 现有模式 | 读类似功能的代码 | 参考项目已有风格 |

**输出格式：**
```markdown
### 🔍 调研结果

**已找到的相关代码：**

| 文件 | 相关性 | 说明 |
|------|--------|------|
| Assets/Game/UI/MainPanel.cs | 直接相关 | 需要添加动画的面板脚本 |
| Assets/Tem/Examples/OdinInspectorDemo.cs | 参考 | 使用了类似的属性面板模式 |

**可用的框架能力：**
- DOTween：补间动画（`transform.DOMove`、`DOFade` 等）
- UniRx：事件订阅
- ...
```

### Step 2: 设计备选方案

基于调研结果，给出 **1-3 个方案**，每个包含：

```markdown
### 方案 A：<方案名称>

**思路**：<一句话描述核心思路>

**改动范围**：

| 文件 | 操作 | 改动量 |
|------|------|--------|
| Assets/Game/UI/MainPanel.cs | 修改 | +15 行 |
| Assets/Game/UI/MainPanel.prefab | 修改 | 新增 CanvasGroup 组件 |

**伪代码**：
\`\`\`csharp
// 核心逻辑示意（不是最终代码）
void ShowPanel() {
    transform.DOMove(targetPosition, 0.3f).SetEase(Ease.OutBack);
}
\`\`\`

**优点**：...
**缺点**：...
```

**方案数量规则：**
- 简单需求（单文件、<50 行）：1 个方案即可
- 中等需求（2-3 文件）：2 个方案
- 复杂需求（多文件、架构级）：2-3 个方案

### Step 3: 用户选择

使用 `AskUserQuestion` 让用户选择方案：

```
AskUserQuestion(
  questions: [{
    question: "请选择一个实现方案：",
    header: "方案选择",
    options: [
      { label: "方案 A（推荐）", description: "..." },
      { label: "方案 B", description: "..." },
      { label: "暂不改动", description: "先不写代码，让我再想想" }
    ]
  }]
)
```

**如果只有一个方案，仍然要确认：**

```
AskUserQuestion(
  questions: [{
    question: "确认按此方案执行？",
    header: "确认实施",
    options: [
      { label: "确认，开始写代码", description: "..." },
      { label: "暂不改动", description: "让我再考虑考虑" }
    ]
  }]
)
```

### Step 4: 执行（仅在用户确认后）

用户确认方案后：

1. 按确认的方案逐步实现代码
2. 每改完一个文件，说明改了什么
3. 全部改完后，提醒用户在 Unity 中测试

---

## 输出格式

```markdown
## 🔬 方案评估报告

**需求**：<复述需求>
**评估时间**：<时间戳>

---

### 🔍 调研结果

...

---

### 💡 方案 A：<名称>

| 属性 | 值 |
|------|-----|
| 改动文件数 | N |
| 预估代码量 | ~X 行 |
| 风险等级 | 🟢 低 / 🟡 中 / 🔴 高 |

...

---

### 💡 方案 B：<名称>

...

---

### 📊 对比

| 维度 | 方案 A | 方案 B |
|------|--------|--------|
| 复杂度 | 低 | 中 |
| 性能 | 好 | 更好 |
| 可维护性 | 高 | 高 |
| 推荐 | ✅ | - |
```

---

## 易错点

- **不要跳过 Step 0**：即使需求看起来清楚，也要用自己的话复述一遍确保理解正确
- **不要只有一个方案就直接执行**：即使是最优方案，也要等用户确认
- **调研要全面**：不要只看一个文件就下结论，要搜索关联代码
- **伪代码不是最终代码**：Step 2 只展示思路，不要写完整的可直接运行的代码
- **尊重"暂不改动"**：用户选择不改动时，不要催促
- **依赖顺序**：如果方案涉及引入新依赖（如新的 UPM 包），提醒用户先安装

---

## 与其他 Skill 的关系

```
ember-solution-design    →    用户确认    →    编写代码
（评估方案、阻止跳步）                        │
                                            ├─ 涉及包迁移 → ember-plugin-migrate
                                            ├─ 包变更 → ember-package-scan
                                            └─ 提交前 → ember-commit-review
```
