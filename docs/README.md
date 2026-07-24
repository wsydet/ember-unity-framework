# ember-unity-framework 文档索引

> 新接手的开发者从这里开始。

---

## 项目文档

| 文档 | 内容 |
|------|------|
| [CLAUDE.md](../CLAUDE.md) | 项目概述、架构理念、目录结构、编码规范 |

---

## 用户文档 (`docs/user/`)

给框架**使用者**看的。

| 文档 | 内容 |
|------|------|
| [package-inventory.md](user/package-inventory.md) | 已集成的包清单，避免重复导入 |

---

## 开发文档 (`docs/dev/`)

给框架**开发者**看的。

| 文档 | 内容 |
|------|------|
| [contributing.md](dev/contributing.md) | Git 分支规范、提交信息格式 |
| [odin-usage-notes.md](dev/odin-usage-notes.md) | Odin Inspector 在 Unity 6 下的已知问题和推荐写法 |
| [skills-reference.md](dev/skills-reference.md) | 所有 Skill 的速查表 |
| [skill-writing-guide.md](dev/skill-writing-guide.md) | 编写新 Skill 的规范 |
| [burner-architecture.md](dev/burner-architecture.md) | burner 项目架构参考 |

---

## Skill 清单 (`.claude/skills/`)

| Skill | 做什么 |
|-------|--------|
| `ember-commit-review` | 提交前审查变更，推荐提交信息 |
| `ember-package-scan` | 扫描包 → 更新包清单文档 |
| `ember-plugin-migrate` | 插件迁移到 UPM |
| `ember-odin-inspector` | 检查/优化脚本的 Odin 编辑器面板 |
| `ember-odin-capture-style` | 从脚本捕获 Odin 面板风格 → 写入文档 |
| `template-skill` | 创建新 Skill 的模板 |

---

## 源代码 (`Assets/Ember/`)

| 文件 | 说明 |
|------|------|
| `Examples/OdinInspectorDemo.cs` | Odin 面板参考脚本，挂 GameObject 上看效果 |
| `Editor/OdinIntegrationTest.cs` | Odin 集成测试，菜单 `Ember > Test > Run` |
