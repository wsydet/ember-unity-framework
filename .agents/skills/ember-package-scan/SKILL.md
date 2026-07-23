---
name: ember-package-scan
description: >-
  Use when the user wants to scan project packages and sync to documentation,
  mentions "/package-scan", "扫描包", "更新包清单", "同步依赖文档", "scan packages",
  "update package inventory", or after running ember-plugin-migrate to import a new package.
  Do not use for installing packages or for scanning Assets/ scripts.
---

# ember-package-scan — 包清单同步

## 概述

扫描项目实际安装的包，与 `docs/user/package-inventory.md` 对比，
检测差异并生成文档更新建议。

**只读分析，不自动修改文档。** 用户确认后再写入。

---

## 前置条件

1. 读取 `docs/user/package-inventory.md`，缓存当前文档中记录的所有包名和版本
2. 读取 `Packages/manifest.json`，缓存 scopedRegistries + dependencies
3. 扫描 `Packages/com.*/`、`Packages/dev.*/` 目录，找到所有 embedded package

---

## 执行步骤

### Step 1: 收集实际状态

从以下来源收集完整的包列表：

| 来源 | 读取方式 | 字段 |
|------|----------|------|
| `Packages/manifest.json` | 读取 `dependencies` | 包名、版本 |
| `Packages/manifest.json` | 读取 `scopedRegistries` | registry 名称、URL、scopes |
| `Packages/` 目录 | `ls -d Packages/com.*/ Packages/dev.*/` | 包名（embedded package） |
| embedded package 的 `package.json` | 读每个目录下的 `package.json` | displayName、description |

合并去重，生成 **实际包清单**：

```json
[
  { "name": "com.demigiant.dotween", "version": "embedded", "source": "embedded" },
  { "name": "com.neuecc.unirx", "version": "7.1.0", "source": "openupm" },
  { "name": "com.unity.cinemachine", "version": "3.1.7", "source": "unity" }
]
```

### Step 2: 解析文档状态

从 `docs/user/package-inventory.md` 的表格中提取所有记录的包名和版本。

### Step 3: 差异对比

对比实际包清单和文档记录，产生三种差异：

| 差异类型 | 条件 | 优先级 |
|----------|------|--------|
| ➕ 新增 | 实际存在但文档未记录 | 🔴 必须处理 |
| ➖ 过时 | 文档记录了但实际不存在 | 🔴 必须处理 |
| 🔄 版本变更 | 都存在但版本号不同 | 🟡 需要确认 |

### Step 4: 生成更新建议

对于每种差异，给出具体的文档修改建议：

**新增包**：
```
## 1.2 或 2.1 → 新增一行
| com.xxx.yyy | 1.2.3 | [TODO: 填写用途] | [来源] |
```

**过时包**：
```
## 1.2 → 删除这一行
| com.xxx.yyy | ... | （已从项目中移除） |
```

**版本变更**：
```
## 2.1 → 版本号从 "1.2.3" 改为 "2.0.0"
```

### Step 5: 检查 scoped registries

对比 `manifest.json` 中的 `scopedRegistries` 和文档中记录的 registry 配置：

- 如有新增 registry → 提醒在文档中补充
- 如有 registry 已从 manifest 删除但文档仍有记录 → 提醒清理

### Step 6: 输出并等待确认

展示差异报告，用户确认后更新文档（在对话中执行，不委托子代理）。

---

## 输出格式

```markdown
## 📦 包清单扫描报告

扫描时间：<时间戳>
对比基准：`docs/user/package-inventory.md`

---

### ➕ 新增包（文档未记录，共 N 个）

| 包名 | 版本 | 来源 | 建议添加到 |
|------|------|------|-----------|
| com.xxx.yyy | 1.2.3 | Unity Registry | 二、2.1 核心框架 |

---

### 🔄 版本变更（共 M 个）

| 包名 | 文档版本 | 实际版本 | 建议 |
|------|----------|----------|------|
| com.unity.xxx | 1.0.0 | 2.0.0 | 更新版本号 |

---

### ➖ 过时条目（文档记录但不存在，共 K 个）

| 包名 | 文档版本 | 建议 |
|------|----------|------|
| com.xxx.removed | 1.0.0 | 从文档中删除 |

---

### ✅ 无变化（共 X 个）

……

---

### 🔧 Registry 变更

| 变更类型 | 详情 |
|----------|------|
| 新增 | ... |
```

---

## 易错点

- Unity 内置模块（`com.unity.modules.*`）在文档三、节中列出，但不需要逐个对比——它们随引擎版本绑定，不会变动
- embedded package 的版本号可能不存在（`package.json` 中没有 `version` 字段），显示为 `embedded` 即可
- 不要自动推断"用途"列——标记 `[TODO]` 等人工填写
- 如果用户刚用 `ember-plugin-migrate` 迁移了包，remind 他们同步跑一下这个 scan

---

## 与其他 Skill 的关系

```
ember-plugin-migrate    →    ember-package-scan    →    ember-commit-review
（迁移插件到 Packages）        （更新包清单文档）          （提交）
```
