# Ember Skill 编写规范与指南

最后更新：2026-07-20

本文是 `.claude/skills/` 的长期强制规范与写作指南。

> 参考来源：Burner 客户端 Skill 编写规范、[Anthropic Skill Best Practices](https://platform.claude.com/docs/en/agents-and-tools/agent-skills/best-practices)、[Agent Skills 规范](https://agentskills.io/specification)

---

## 1. 适用范围

本规范适用于 Ember 项目内所有 Skill：

- 真实目录：`.claude/skills/<skill-name>/`
- 入口文件：`.claude/skills/<skill-name>/SKILL.md`

---

## 2. 基本原则

1. Skill 是工程资产，不是一次性 prompt。
2. 只写 Agent 不知道的项目知识、硬约束和易错点。
3. SKILL.md 做指南针，细节放 `references/`，脚本放 `scripts/`。
4. 脆弱、重复、可确定的操作用脚本，不让 Agent 每次重写。
5. 会写文件、改资源的 Skill 必须有 dry-run、确认门禁或可回滚路径。

---

## 3. 目录结构

```text
skill-name/
├── SKILL.md          # 必须，唯一入口
├── references/       # 可选：详细规则、映射表、eval
├── scripts/          # 可选：确定性 CLI 脚本
└── assets/           # 可选：静态模板、示例图
```

约束：

- 目录名与 `SKILL.md` frontmatter 的 `name` 一致。
- 文件引用使用从 Skill 根目录出发的相对路径，如 `references/evaluation.md`。
- 引用深度保持一层。
- 不在 Skill 根目录放运行产物、缓存、临时文件。

---

## 4. Frontmatter

```yaml
---
name: skill-name
description: >-
  Use when the user [触发条件]. Do not use for [排除场景].
---
```

规则：

- `name`：小写字母 + 数字 + 连字符，长度 1-64
- `description`：1-1024 字符，推荐 < 500
- `description` 只写**触发条件**，不写步骤、命令或输出格式
- `description` 必须包含用户会说的**关键词**
- `description` 必须写明**排除场景**（尤其是容易误触发的通用词）

### Description 示例

```yaml
# ✅ 好的
description: >-
  Use when the user asks to create a new Unity C# script following Ember naming conventions,
  mentions "/ember-create-script", or needs a MonoBehaviour/ScriptableObject template.
  Do not use for editing existing scripts or creating non-Unity code.

# ❌ 坏的
description: >-
  This skill creates scripts. First read the template, then generate the file,
  then verify the result. Output the file path at the end.
```

---

## 5. SKILL.md 正文结构

```markdown
# Skill 名称

## 概述
## 前置条件
## 易错点
## 执行步骤
## 验证
## 失败处理
## 输出要求
```

写作要求：

- 开头说明核心边界，不重复 description
- `易错点` 放真实踩坑，不放泛泛提醒
- 线性流程用编号列表，复杂分支才用表格或流程图
- 正文尽量短：高频 Skill < 200 词，普通 Skill < 500 词

---

## 6. references / scripts / assets

### references/
- 放详细规则、字段表、eval、历史失败案例
- 保持一层路径
- 文件名表达用途，如 `evaluation.md`、`type-mapping.md`

### scripts/
- 放确定性小型 CLI
- 必须有清晰错误消息和 `--help`
- 不依赖本机绝对路径

### assets/
- 放静态模板、示例图、固定资源
- 不放运行生成物

---

## 7. 运行产物路径

运行产物统一放到项目根目录的临时目录：

```text
.tmp/<skill-name>/
```

长期交付物放到对应的文档目录：

```text
docs/<domain>/<report-name>.md
```

禁止：
- 把运行产物写入 `.claude/skills/<skill-name>/`
- 把长期缓存写入 Unity `Temp/` 或系统临时目录
- 把本机绝对路径写进 SKILL.md

---

## 8. Unity Skill 额外要求

涉及 Unity C#、Prefab、材质、场景、资源、配置表的 Skill：

- 读取并遵守 `CLAUDE.md` 中的编码规范
- 修改 C# 后以 Unity 编译 0 error 作为完成标准
- 不直接写 `.meta` 文件
- Play Mode 下不做触发编译或资源导入的不安全操作
- 无法验证时，最终输出必须列出未验证文件和后续验证动作

---

## 9. Eval 与回归

新增或大改 Skill 时，至少做四类验证：

| 验证类型 | 检查内容 |
|----------|----------|
| 发现验证 | 哪些 prompt 应触发，哪些不应触发 |
| 逻辑验证 | 按真实任务走一遍，找出模糊点 |
| 边界验证 | 依赖缺失、脚本失败、用户不确认 |
| 回归验证 | 历史失败场景是否已被易错点或脚本覆盖 |

---

## 10. 提交前检查清单

修改 Skill 后逐项确认：

- [ ] `name` 与目录名一致
- [ ] `description` 只写触发条件，含关键词和排除场景
- [ ] SKILL.md 和 references 中文优先，不含个人路径或一次性状态
- [ ] 易错点至少覆盖真实高风险错误
- [ ] 引用路径不超过一层
- [ ] 会写文件或外部系统的流程有 dry-run 或确认门禁
- [ ] Unity 相关 Skill 写明编译验证要求

---

## 11. 参考来源

- Anthropic Skill Best Practices：https://platform.claude.com/docs/en/agents-and-tools/agent-skills/best-practices
- Anthropic Skill 方法论：https://claude.com/blog/lessons-from-building-claude-code-how-we-use-skills
- Agent Skills 规范：https://agentskills.io/specification
- 社区 Skill 最佳实践：https://github.com/mgechev/skills-best-practices
