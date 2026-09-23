# 项目 Skill 速查

源文件在 `.agents/skills/`。Codex 中可显式写 `$技能名`；已有 `/技能名` 关键词也保留在各 Skill 的触发描述中。用名称加目标和要求即可。

| Skill | 使用场景 |
|---|---|
| [ember-template-conflict-recovery](../../.agents/skills/ember-template-conflict-recovery/SKILL.md) | 合并消费项目模板升级冲突、恢复本地业务改动，并交回项目中心重新预览；框架最低 0.14.5 |
| [ember-doc-maintenance](../../.agents/skills/ember-doc-maintenance/SKILL.md) | 阅读并维护全部项目文档，核对源码、合并旧计划、修复链接与索引 |
| [ember-generate-doc](../../.agents/skills/ember-generate-doc/SKILL.md) | 为指定模块生成或更新 API 文档 |
| [ember-package-scan](../../.agents/skills/ember-package-scan/SKILL.md) | 对照 manifest/lock/embedded 包同步依赖清单 |
| [ember-plugin-migrate](../../.agents/skills/ember-plugin-migrate/SKILL.md) | 评估或实施第三方插件迁移到 UPM |
| [ember-commit-review](../../.agents/skills/ember-commit-review/SKILL.md) | 审查本地改动并分组；明确要求代提交时按项目授权执行 |
| [ember-odin-inspector](../../.agents/skills/ember-odin-inspector/SKILL.md) | 指定脚本或目录，检查和优化 Odin 面板 |
| [ember-odin-capture-style](../../.agents/skills/ember-odin-capture-style/SKILL.md) | 从满意的面板提取写法到 Odin 规范 |
| [ember-region-organizer](../../.agents/skills/ember-region-organizer/SKILL.md) | 按项目约定整理 C# region |
| [ember-solution-design](../../.agents/skills/ember-solution-design/SKILL.md) | 先讨论方案；保留已有实施授权，不重复确认 |
| [ember-eui-build](../../.agents/skills/ember-eui-build/SKILL.md) | 使用 UI 开发中心制作 EUI Prefab、生成真实绑定并完成用户逻辑；可通过 UPM Manager 安装到消费项目 |

消费项目的技能安装、更新、兼容要求与备份恢复见 [AI Skill 独立更新](../../Packages/com.ember/Documentation~/maintenance/ai-skills.md)。发布目录由框架 `.agents/skills/catalog.json` 维护；v0.13.1 发布其中 9 个技能，v0.14.7 新增模板冲突恢复技能，共 10 项；除 ember-plugin-migrate 外，上表所有技能均可独立安装。空白 template-skill 不发布。

文档维护示例：

```text
$ember-doc-maintenance 阅读并维护项目所有文档，按当前源码更新过时内容，合并或删除已被替代的文档。
```

只想查看建议时写明“只审计，不修改”。新增 Skill 参考 [编写指南](skill-writing-guide.md)；不要把空白 Skill 或 Agent 模板当成可调用工具。
