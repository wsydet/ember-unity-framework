# Ember Skill 编写与维护

最后核对：2026-09-07。项目 Skill 的真实目录是 `.agents/skills/<name>/SKILL.md`；本工作区 `.claude/skills` 是兼容 junction，修改真实目录即可，不维护两份副本。Skill 清单见 [速查表](skills-reference.md)。

## 结构与触发

目录名与 frontmatter 的 `name` 一致，使用小写字母、数字和连字符。`description` 写实际任务、用户表达和易混淆边界；步骤放正文。不要把只含 TODO 的模板放进会被发现的 Skill 目录。

````markdown
---
name: ember-example
description: 说明该技能解决的任务、何时使用，以及相邻但不适用的场景。
---

# 任务名称

说明目标、项目特有约束、证据来源、操作方式、验证方法和失败处理。
````

`scripts/` 放重复且确定的操作；`references/` 放按需读取的大段规则；`assets/` 放真正需要的模板。只有任务需要时才创建目录，不增加空壳文件。正文引用这些资源时使用相对于 Skill 目录的路径，项目路径注明相对仓库根。

## 授权与改动

遵从用户实际意图：评估请求只分析，已明确授权的生成/维护请求完成范围内写入。不要强制每一步重新确认。对可能丢失内容的处理先保存当前工作区版本并制作可审查的改动；权限保护交给平台审批，不绕过受保护目录。

不得用 HEAD 还原用户未提交内容，不自动提交、推送或发送消息。涉及 `.meta`、模板快照、场景、Prefab 时以 [CLAUDE.md](../../CLAUDE.md) 的具体约束为准。

## 路径和验证

- 运行产物放 `.utmp/<skill-name>/`（已被仓库忽略），正式交付物放对应源码或文档目录；不把缓存写进 Skill。
- 脚本提供 `--help`、明确失败信息，避免固化用户目录或机器上解释器路径。按当前 shell 生成命令，Windows 不套用 Bash 续行/删除命令。
- 创建或大改后校验 YAML、目录名、引用、未完成占位符；可用 skill-creator 自带 `quick_validate.py`。脚本还要验证真实输入输出及关键边界。
- Unity 代码/资源变更的编译检查仅走 Unity MCP；工具不可用时说明未验证。纯文档或维护脚本不宣称 Unity 测试通过。
- 用应触发/不应触发的真实请求检查 description，保留有效工作流，删除本次会话状态和重复约束。

可复用实例：[文档维护 Skill](../../.agents/skills/ember-doc-maintenance/SKILL.md)。
