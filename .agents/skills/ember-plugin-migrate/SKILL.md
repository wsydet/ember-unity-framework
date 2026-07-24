---
name: ember-plugin-migrate
description: >-
  Use when the user wants to migrate third-party plugins to UPM (Unity Package Manager),
  mentions "/plugin-migrate", "插件迁移", "包管理", "package manager", "migrate plugin",
  "插件转包管理", "扫描插件", or asks about converting imported plugins to package management.
  Do not use for project code under Assets/Ember/ or Assets/Game/.
---

# ember-plugin-migrate — 插件迁移到 Package Manager

## 概述

扫描 `Assets/` 下的所有第三方插件（不限于 `Plugins/` 目录），分析每个插件的结构、
版本和可用的迁移方案，生成完整的迁移报告。用户确认后，对选定的插件执行迁移操作。

**迁移策略优先级**：OpenUPM > Git URL > 本地嵌入式 Package > 保持现状

---

## 前置条件

1. 确认 `Packages/manifest.json` 可读写
2. 网络可用（用于查询 OpenUPM / GitHub）

---

## 执行步骤

### Step 1: 发现插件

扫描 `Assets/` 的一级子目录，识别潜在的第三方插件。

```bash
ls -d Assets/*/
```

**排除以下项目代码目录**（扫描时跳过，不进行分析）：

| 排除目录 | 原因 |
|----------|------|
| `Assets/Ember/` | 框架自身代码 |
| `Assets/Game/` | 业务逻辑代码 |
| `Assets/Scenes/` | Unity 场景目录 |
| `Assets/Settings/` | 项目设置 |
| `Assets/Resources/` | Unity 资源目录 |
| `Assets/StreamingAssets/` | Unity 流式资源 |
| `Assets/Editor/` | 项目自有的编辑器脚本 |
| `Assets/Editor Default Resources/` | Unity 默认资源 |
| `Assets/TutorialInfo/` | 模板教程 |
| `Assets/GeneratedLocalRepo/` | 本地生成文件 |
| `Assets/Reports/` | 报告输出 |
| `Assets/ArtWhiteBoxAsset/` | 美术资源 |
| `Assets/ContentPreview/` | 内容预览 |
| `Assets/Burner/` | 特定项目目录 |

**插件识别特征**（满足任一即视为候选插件）：

- 包含 `.dll` 文件
- 有独立的 `.asmdef` 且目录名不匹配项目模块
- 包含 `README*`、`LICENSE*`、`CHANGELOG*` 等第三方特征文件
- 目录名/结构与已知的 Asset Store 插件匹配

对每个候选插件目录，记录：
- 插件名称（目录名）
- 当前路径（`Assets/<目录名>/` 或 `Assets/<父目录>/<子目录>/`）
- 文件总数和总大小
- 顶层结构（有哪些子目录和关键文件）

### Step 0: 预过滤 —— 不可迁移名单

以下插件在扫描前直接跳过，标记为"不可迁移"：

| 插件 | 原因 |
|------|------|
| **Odin Inspector** | 安装脚本复杂，深度依赖 Plugins/ 特殊编译顺序，与 Unity 序列化系统深度集成，迁移会破坏编辑器功能 |
| 任何匹配 `Sirenix*`、`Odin*` 的目录 | 同 Odin，整个 Sirenix 套件不可迁移 |

> 用户无需针对这些插件做任何确认——skill 自动跳过并在报告中注明。

### Step 2: 分析插件结构

对每个插件，深入分析其内部结构：

#### 2a. 识别插件类型

| 类型 | 识别特征 | 迁移难度 |
|------|----------|----------|
| 纯 DLL | 只有 `.dll` + `.xml` + `.mdb/.pdb`，无或极少 `.cs` | 中等 |
| 纯源码 | 大量 `.cs` 文件，可能有 `.asmdef` | **简单** |
| 混合型 | `.dll` + `.cs` 源码模块共存（如 DOTween） | 中等 |
| 资源型 | 主要是 `.prefab`、`.asset`、贴图、材质等 | 复杂（不建议迁移） |

#### 2b. 提取版本信息

按优先级尝试：
1. 读取 `ReadMe.txt` / `README.md` / `CHANGELOG.md`，搜索版本号（如 `Ver X.Y.Z`、`Version X.Y.Z`）
2. 读取 `.dll` 的 `ProductVersion`（`Get-Item xxx.dll | % VersionInfo`）
3. 读取 `package.json`（如果有）
4. 以上都没有 → 标记为"未知"

#### 2c. 检查程序集引用

```bash
# 查找 .asmdef 文件
find <plugin_dir> -name "*.asmdef" -exec cat {} \;
```

记录：
- 插件是否有 `.asmdef`
- `.asmdef` 中是否引用了其他程序集
- 是否有 `overrideReferences` 或 `precompiledReferences`

#### 2d. 检查项目内引用

搜索项目代码中对插件的引用：
```bash
grep -r "<namespace_or_keyword>" Assets/ --include="*.cs" -l | grep -v "Assets/Plugins/"
```

记录哪些文件引用了该插件，迁移后需要验证这些引用不中断。

---

### Step 3: 查询可用迁移方案

对每个插件，按优先级查询：

#### 3a. 查询 OpenUPM

```bash
curl -s "https://package.openupm.com/-/v1/search?text=<plugin_name>&size=5"
```

匹配规则：
- 如果搜索结果中有包名包含插件名关键词 → 记录为 **候选 OpenUPM 方案**
- 优先匹配官方包（作者/组织名匹配插件作者）
- 记录：包名、最新版本、发布日期

常见插件的映射表（优先参考）：

| 插件 | 推荐方案 | 备注 |
|------|---------|------|
| UniRx | OpenUPM `com.neuecc.unirx` | 官方发布 |
| DOTween | 本地嵌入 | 无官方 UPM 包 |
| DoTween Pro | 本地嵌入 | 付费插件 |
| TextMesh Pro | 已内置 Unity | 无需迁移 |
| Odin Inspector | 🚫 不可迁移 | 深度依赖 Plugins/ 编译顺序，自动跳过 |
| Sirenix 系列（任何） | 🚫 不可迁移 | 同 Odin |
| Zenject / Extenject | OpenUPM `com.svermeulen.extenject` | 社区维护 |
| Addressables | 已内置 Unity | 无需迁移 |

#### 3b. 查询 Git URL

对于有已知 GitHub 仓库的插件：
1. 搜索 `https://github.com/<author>/<repo>`
2. 检查仓库是否包含 `package.json`（UPM 兼容标志）
3. 如有 `package.json` → 可通过 Git URL 安装：`"com.xxx.xxx": "https://github.com/xxx.git#vX.Y.Z"`

#### 3c. 本地嵌入式 Package 可行性

检查以下条件，全部满足即可本地嵌入：

- [ ] 插件结构清晰，有明确的运行时/编辑器边界
- [ ] 文件数量合理（< 200 个文件，否则包太大）
- [ ] 不依赖 `Assets/Plugins/` 的特殊路径行为（如 `Plugins/` 的优先编译顺序）
- [ ] 没有硬编码的路径引用

#### 3d. 不推荐迁移的情况

以下情况标记为"建议保持现状"：
- 纯资源型插件（大量 `.prefab`、`.asset`、贴图）
- 依赖 `Plugins/` 特殊编译顺序的 DLL
- 有复杂安装脚本的插件（如 Odin——已在前置过滤中自动跳过）
- 文件数量极其庞大（> 500 文件）
- 插件已被深度定制/修改，迁移后难以合并

---

### Step 4: 生成迁移报告

对每个插件输出一张卡片：

```markdown
### 📦 `<插件名>`

| 属性 | 值 |
|------|-----|
| 路径 | `Assets/<实际路径>/` |
| 类型 | `纯源码` / `纯 DLL` / `混合型` / `资源型` |
| 版本 | `<版本号>` (来源: `<来源>`) |
| .asmdef | `有 (N 个)` / `无` |
| 项目引用 | `N 个文件引用` / `无引用` |
| 推荐方案 | `OpenUPM` / `Git URL` / `本地嵌入` / `保持现状` / `🚫 不可迁移` |

**迁移方案详情：**

- **OpenUPM**: `com.xxx.xxx` @ `x.y.z` ✅ 可用 / ❌ 不可用
- **Git URL**: `https://github.com/xxx/xxx.git#vX.Y.Z` ✅ / ❌
- **本地嵌入**: ✅ 可行 / ⚠️ 需处理 / ❌ 不建议
  - 需处理项：...
```

### Step 5: 用户确认

使用 `AskUserQuestion` 让用户选择要迁移的插件（多选），以及确认迁移方案。

选项示例：
- "UniRx → OpenUPM (com.neuecc.unirx @ 7.1.0)"
- "DOTween → 本地嵌入式 Package"
- "XXX → 暂不迁移"

### Step 6: 执行迁移

对用户确认的每个插件，按对应方案执行：

#### 6a. OpenUPM 迁移

1. 检查 `manifest.json` 是否已有 OpenUPM scoped registry
   - 如果没有 → 添加 `"scopedRegistries"` 配置
   - 如果已有但 scope 不包含目标包 → 追加 scope
2. 在 `dependencies` 中添加包依赖
3. 删除 `Assets/Plugins/<插件目录>/`
4. 清理相关的 `.meta` 文件和空目录

#### 6b. Git URL 迁移

1. 在 `manifest.json` 的 `dependencies` 中添加：
   ```json
   "com.xxx.xxx": "https://github.com/xxx/xxx.git#vX.Y.Z"
   ```
2. 删除 `Assets/Plugins/<插件目录>/`
3. 清理相关的 `.meta` 文件和空目录

#### 6c. 本地嵌入式 Package 迁移

1. 创建 `Packages/<com.xxx.xxx>/` 目录
2. 编写 `package.json`：
   ```json
   {
     "name": "com.xxx.xxx",
     "version": "<检测到的版本>",
     "displayName": "<显示名称>",
     "description": "<描述>",
     "unity": "6000.0",
     "author": { "name": "<作者>", "url": "<URL>" }
   }
   ```
3. 将插件文件从 `Assets/Plugins/<插件>/` 复制到 `Packages/<com.xxx.xxx>/`
4. 删除 `Assets/Plugins/<插件目录>/`
5. 对于 `.asset` 设置文件（如 `DOTweenSettings.asset`）：
   - 检查是否引用了 DLL GUID（用 `grep -l "guid:"` 检查）
   - 如果引用 GUID → 删除该文件并告知用户需重新生成
   - 如果没有引用 GUID → 保留，移到合适的 Resources 位置

#### 6d. 迁移后清理

每个插件迁移完成后：
- 删除 `Assets/Plugins/<插件>/` 目录和对应的 `.meta`
- 如果 `Assets/Plugins/` 变为空目录 → 删除目录和 `Assets/Plugins.meta`

---

## 输出格式

```markdown
## 🔍 插件迁移扫描报告

**扫描时间**：<时间戳>
**扫描范围**：`Assets/` 全目录（已排除项目代码目录）
**发现候选插件**：N 个

---

### 📦 `PluginA` — 🟢 推荐: OpenUPM
...（单插件卡片）

### 📦 `PluginB` — 🟡 推荐: 本地嵌入
...（单插件卡片）

### 📦 `PluginC` — 🔴 推荐: 保持现状
...（单插件卡片）

---

### 🚫 不可迁移（N 个）

| 插件 | 路径 | 原因 |
|------|------|------|
| Odin Inspector | Assets/Plugins/Sirenix/ | 深度依赖 Plugins/ 编译顺序 |

---

### 📊 汇总

| 插件 | 路径 | 推荐方案 | 可迁移 |
|------|------|---------|--------|
| PluginA | Assets/Plugins/ | OpenUPM | ✅ |
| PluginB | Assets/ThirdParty/ | 本地嵌入 | ✅ |
| PluginC | Assets/Plugins/ | 保持现状 | ❌ |
| Odin Inspector | Assets/Plugins/Sirenix/ | 🚫 不可迁移 | ❌ |
```

---

## 注意事项

### 迁移安全性

- 🛑 **永远不要在迁移前删除原始文件** —— 先复制到 Packages 验证成功后再删
- 🛑 **如果插件被项目代码大量引用**，迁移后必须验证编译通过
- 🛑 **.asset 文件中的 GUID 引用会在迁移后失效**，需要告知用户重新生成
- 🛑 **付费/商业插件不要发布到公共 registry**，只走本地嵌入

### 插件间依赖

- 如果 PluginA 依赖 PluginB，迁移时要一起处理
- 迁移后 UPM 的依赖解析会自动处理包间依赖（如果都走 UPM）
- 本地嵌入的包之间需要手动配置 `.asmdef` 引用

### DLL 特殊处理

- `Assets/Plugins/` 下的 DLL 享有特殊编译顺序（在所有 C# 源码之前编译）
- 迁移到 `Packages/` 后，编译顺序由 `.asmdef` 和 UPM 决定
- 如果 DLL 依赖这个特殊顺序，可能需要调整

### 不自动操作

- 不自动执行 `git add` / `git commit`
- 不自动修改 `.asmdef` 文件（除非是迁移必需）
- 不自动修改任何源码文件中的 `using` 语句
- 每个操作都向用户报告并等待确认

---

## 验证

迁移完成后：
1. 确认 `Packages/<com.xxx.xxx>/` 目录结构和内容完整
2. 确认 `Assets/Plugins/<插件>/` 已删除
3. 确认 `manifest.json` 语法正确（合法 JSON）
4. 确认 `.meta` 文件清理干净（`find Assets -name "*.meta" | xargs grep -l "<plugin>"` 为空）
5. 列出用户在 Unity 中需要执行的后续操作（如重新 Setup）
