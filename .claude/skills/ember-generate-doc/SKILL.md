---
name: ember-generate-doc
description: >-
  Use when the user wants to generate API documentation for a module, mentions
  "/generate-doc", "/ember-generate-doc", "生成API文档", "生成文档", "写模块文档",
  "/gen-doc", or asks to document a module's public API.
  Do not use for updating existing non-API docs or for writing user-facing guides.
---

# ember-generate-doc — 模块 API 文档生成

## 概述

扫描指定模块的 C# 源码，按 `docs/dev/api-doc-template.md` 模板自动生成 `API.md`，
输出到模块目录下。

**与用户交互确认路径后再分析，展示预览，用户确认后才写入文件。**

---

## 前置条件

1. 读取 `docs/dev/api-doc-template.md`，缓存模板结构和各节要求

---

## 执行步骤

### Step 0: 扫描可用模块

先扫描 `Assets/Ember/` 下的所有子目录，找到可作为文档生成目标的模块。

```bash
ls -d Assets/Ember/*/
```

对每个子目录：

1. 检查是否包含 `.cs` 文件（递归）
2. 检查是否已有 `API.md`
3. 记录状态

以列表形式展示给用户选择：

```markdown
## 📂 可用模块

| # | 模块 | 路径 | .cs 文件 | 文档状态 |
|---|------|------|----------|----------|
| 1 | Resource | `Assets/Ember/Resource/` | 2 个 | ❌ 无文档 |
| 2 | Core | `Assets/Ember/Core/` | 5 个 | ❌ 无文档 |
| 3 | UI | `Assets/Ember/UI/` | 0 个 | ⏭️ 跳过（无源码） |
```

无 `.cs` 文件的目录自动跳过（如空目录、纯资源目录）。

### Step 1: 用户选择

使用 `AskUserQuestion` 让用户选择目标模块，选项来自 Step 0 的扫描结果。

同时让用户确认文档输出路径。默认推荐位置：

```
<模块路径>/API.md
```

例如：`Assets/Ember/Resource/API.md`

如果用户想放别处，接受自定义路径。

**如果用户提供的路径不在扫描列表中**（如手动输入了 `Assets/Game/Logic/`），同样接受并继续。

### Step 2: 确定模块边界

从用户确认的路径推断模块信息：

| 推断项 | 规则 | 示例 |
|--------|------|------|
| 模块名称 | 路径最深层目录名 | `Assets/Ember/Resource/` → "资源管理（Resource）" |
| 命名空间 | 搜索目录下所有 `.cs` 文件中的 `namespace` 声明，取最公共的前缀 | `Ember.Resource` |
| 模块根目录 | 路径本身 | `Assets/Ember/Resource/` |

### Step 3: 收集源码

递归扫描目标目录下的所有 `.cs` 文件（包含 `Runtime/`、`Editor/` 子目录）：

```bash
find <模块路径> -name "*.cs" -not -path "*/Test*" -not -path "*/Demo*" | sort
```

排除测试文件和 Demo 文件。

### Step 4: 源码分析

对每个 `.cs` 文件，提取以下信息：

#### 4a. 公开类型

搜索所有 `public` / `internal` 的 `class`、`interface`、`struct`、`enum` 声明。
对每个类型记录：

- 类型名称和完整命名空间
- 基类和实现的接口
- `[Attribute]` 标记
- 是否继承 `MonoBehaviour` / `ScriptableObject`
- 获取方式（Singleton.Instance / new / 外部传入 / Editor 菜单）

#### 4b. 公开方法

对每个公开类型，提取 `public` / `internal` 方法的签名。
关注点：

- 方法名、参数类型和名称、返回值
- 是同步还是回调/协程（`Action<T>` 参数或 `IEnumerator` 返回值）
- 每个方法的职责（从名称和注释推断）

#### 4c. 依赖关系

搜索以下模式识别模块对外依赖：

- `using` 语句：识别引用了哪些命名空间
- `[RequireComponent]`、`[AddComponentMenu]` 等特性
- 构造函数/方法中接受的参数（DI 依赖）
- 对其他模块 Manager 的引用（如 `EventBus.Instance`、`SceneManager.Instance`）

排除：
- `using UnityEngine` / `using System` 等引擎和标准库
- `using UnityEditor`（编辑器专用依赖，单独标注）

#### 4d. 调用链

追踪模块内的主要执行路径：

1. 找到所有 `public` 方法，标记为"入口"
2. 在方法体内搜索对其他私有方法的调用
3. 串联成调用链：`入口方法() → 内部方法A() → 内部方法B() → 回调/事件`
4. 对于异步流程（回调、协程），用 `→ [回调]` 标记跳转点

提取 2-4 条主流程即可，不追求覆盖所有分支。

#### 4e. 约束与陷阱

从代码中自动检测以下信号：

| 信号 | 提示 |
|------|------|
| `Debug.Assert` / `throw` | 前置条件 |
| `if (xxx == null) return` | 空引用风险 |
| `[Obsolete]` | 即将废弃的 API |
| `// TODO` / `// FIXME` / `// HACK` | 已知问题 |
| `DontDestroyOnLoad` | 全局生命周期 |
| `Destroy(obj)` / `Dispose()` | 手动资源释放 |
| `StartCoroutine` / `StopCoroutine` | 协程管理 |
| `+=` 事件订阅 / `-=` 很少出现 | 可能的事件泄漏 |
| `lock` / `ConcurrentQueue` 等 | 线程安全相关 |

### Step 5: 填充模板

按 `docs/dev/api-doc-template.md` 的顺序逐节填充：

```
1. 快速上手        → 从 Step 4b 选最核心的 1 个方法，写 3-5 行示例代码
2. 模块概述        → 从命名空间和类型名推断职责，1-2 句话
3. 依赖关系        → 从 Step 4c 的结果生成表格
4. 文件清单        → 从 Step 3 的文件列表按角色分类
5. 公开 API        → 从 Step 4a + 4b 的结果，选核心类型和方法
6. 主流程          → 从 Step 4d 的结果，选 2-3 条主流，箭头格式
7. 修改影响范围    → 从方法职责反向推导"要改 X → 改 Y"
8. 约束与已知陷阱  → 从 Step 4e 的结果，只列代码中实际存在的
```

**填充原则**：
- 模板中每个 `[填写...]` 占位符都必须被替换为实际内容
- 代码中确实没有对应内容时（如没有视图层），写"无"而不是留空
- 不确定的推断用 `（推测）` 标注，让用户人工确认
- `快速上手` 的示例代码必须是可以直接复制使用的最小可运行片段

### Step 6: 展示预览

在对话中展示完整的 API.md 内容，让用户审查。

同时报告：
- 覆盖了哪些文件
- 哪些推断是低置信度的（需用户确认）
- 模板中哪些节因为没有对应代码而填了"无"

### Step 7: 写入（用户确认后）

用户确认（或修改后确认），写入到 Step 1 确认的输出路径。

如果已存在 `API.md`，展示 diff 并确认是否覆盖。

---

## 输出示例

```markdown
## 📂 可用模块

| # | 模块 | 路径 | .cs 文件 | 文档状态 |
|---|------|------|----------|----------|
| 1 | Resource | `Assets/Ember/Resource/` | 2 个 | ❌ 无文档 |

---

请选择要生成文档的模块，以及确认输出路径（默认：`Assets/Ember/Resource/API.md`）
```

用户确认路径后：

```markdown
## 资源管理（Resource）— API.md 预览

（完整文档预览）

---
**覆盖文件**：IResourceProvider.cs、EmberResourceManager.cs（共 2 个）
**低置信度项**：无
**空节**：第 6 节"主流程"标记为"无"（当前只有接口定义，无调用链）

---

确认写入 `Assets/Ember/Resource/API.md` 吗？
```

---

## 易错点

- 不要分析 `.meta` 文件
- `using` 语句不等于依赖关系——只取框架模块和外部包的引用，忽略 System/UnityEngine
- 调用链只追踪模块内部，不要跨越到外部模块（标注 `→ [触发外部事件]` 即可）
- 方法签名不要列出完整参数类型（如 `Dictionary<string, List<int>>`），用简化的参数名替代
- 如果一个类型同时有 `public` 和 `internal` 方法，只把 `public` 放 API 节，`internal` 方法放主流程里体现
- 如果模块只有接口和抽象类（还没有实现），主流程和快速上手可能填不满——如实标注"模块尚未实现"即可
- 输出路径默认在模块目录下（和代码放一起），只有当用户明确要求时才放到 `docs/` 下

---

## 验证

- 确认在分析代码前，已经和用户确认了目标路径和输出路径
- 确认生成的文档包含模板的全部 8 个节
- 确认所有占位符都已被替换（没有残留的 `[填写...]`）
- 确认快速上手示例代码语法正确（至少类型名和方法名与源码一致）
- 确认文件清单中的路径真实存在
