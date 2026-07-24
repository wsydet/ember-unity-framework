# Skill 速查表

> 忘了哪个 skill 做什么的？Ctrl+F 搜中文关键词即可。

---

## 包管理

| 调用方式 | 做什么 | 一句话 |
|----------|--------|--------|
| `/ember-package-scan` | 扫描包 → 更新文档 | 检查项目装了哪些包，跟文档对比，找出差异 |
| `/ember-plugin-migrate` | 插件迁移 | 把 `Assets/Plugins/` 下的旧插件迁移到 UPM 包 |

---

## Odin 面板

| 调用方式 | 做什么 | 一句话 |
|----------|--------|--------|
| `/ember-odin-inspector` | 检查/优化面板 | 指定一个脚本，检查它的 Odin 面板有没有兼容问题，给出修正建议 |
| `/ember-odin-capture-style` | 捕获面板风格 | 面板调到满意了，调用它把当前的风格/写法存入文档，以后检查脚本时参考 |

---

## 提交

| 调用方式 | 做什么 | 一句话 |
|----------|--------|--------|
| `/ember-commit-review` | 提交前审查 | 看看改了什么，分组推荐提交信息，区分需要提交/忽略/丢弃的文件 |

---

## 工作流示例

```
加了一个新包 → /ember-package-scan → 更新文档 → /ember-commit-review → 提交
插件迁移     → /ember-plugin-migrate → /ember-package-scan → /ember-commit-review → 提交
调试面板     → /ember-odin-capture-style → /ember-odin-inspector → /ember-commit-review → 提交
```
