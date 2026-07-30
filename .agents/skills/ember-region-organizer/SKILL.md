---
name: ember-region-organizer
description: >-
  Use when the user invokes "/region-organizer", "/region", "代码分块", "region整理",
  "整理region", "组织代码块", or asks to reorganize C# scripts with #region blocks per
  the project convention. Accepts a file path or folder path, then reorganizes all
  members into the standard four regions: 参数, 外部方法, 生命周期, 内部方法.
  Do NOT auto-trigger — the user must explicitly invoke this skill.
---

# ember-region-organizer — C# 代码 Region 分块整理

## 概述

读取目标 C# 脚本，将其成员按项目编码规范（[CLAUDE.md](../../../CLAUDE.md) §代码组织）
重新组织为标准的 `#region` 块结构。对已有 region 的脚本也会先拆除再重建，确保一致。

**先出方案，用户确认后再写入。**

---

## 前置条件

1. 读取 `CLAUDE.md` §代码组织章节，缓存分块规则
2. 确认目标文件存在

---

## 执行步骤

### Step 0: 确认目标（必须先执行）

**如果调用时未指定目标，使用 AskUserQuestion 询问：**

> "要整理哪些脚本的 region 分块？"

选项：
- "指定一个脚本文件"
- "指定一个文件夹（整理其中所有 .cs 文件）"

**如果用户已明确指定路径，跳过此步骤。**

### Step 1: 收集目标文件

- 单个脚本 → 直接读取
- 文件夹 → 用 Glob 扫描 `*.cs`，列出所有匹配文件
- 排除 `.meta` 文件
- 如果扫描结果超过 20 个文件，先列出清单让用户确认范围

### Step 2: 解析脚本结构

对每个脚本，识别以下要素：

#### 2.1 类边界

- 找到 `class` / `struct` 声明及其闭合大括号
- 如果一个文件有多个顶层类型（partial class 等），分别处理
- 嵌套类型（class inside class）保持原样不动，只在日志中提及

#### 2.2 成员分类

将类内所有成员（字段、属性、方法、事件、委托、嵌套类型）分入四个类别：

| 类别 | Region 名 | 识别规则 |
|------|-----------|----------|
| **参数** | `参数` | 字段（含 `const`/`readonly`/`static`）、属性、事件声明 |
| **外部方法** | `外部方法` | `public` / `internal` 方法（非生命周期） |
| **生命周期** | `生命周期` | MonoBehaviour 生命周期方法（见下方清单），仅 MonoBehaviour 子类有此块 |
| **内部方法** | `内部方法` | `private` / `protected` 方法（非生命周期）、嵌套类型 |

MonoBehaviour 生命周期方法清单（仅以下方法归入"生命周期"）：

```
Awake, Start, OnEnable, OnDisable,
Update, FixedUpdate, LateUpdate,
OnDestroy, Reset, OnValidate,
OnApplicationFocus, OnApplicationPause, OnApplicationQuit,
OnRectTransformDimensionsChange, OnRectTransformRemoved,
OnBeforeTransformParentChanged, OnTransformParentChanged,
OnTransformChildrenChanged, OnCanvasGroupChanged,
OnCanvasHierarchyChanged, OnBecameVisible, OnBecameInvisible,
OnWillRenderObject, OnPreCull, OnPreRender, OnPostRender, OnRenderObject,
OnRenderImage, OnDrawGizmos, OnDrawGizmosSelected,
OnGUI, OnAnimatorIK, OnAnimatorMove, OnStateEnter, OnStateExit,
OnParticleCollision, OnJointBreak, OnTriggerEnter, OnTriggerExit,
OnTriggerStay, OnCollisionEnter, OnCollisionExit, OnCollisionStay,
OnControllerColliderHit, OnMouseDown, OnMouseUp, OnMouseEnter,
OnMouseExit, OnMouseOver, OnMouseDrag, OnMouseUpAsButton
```

#### 2.3 附件保持

- XML 文档注释（`///`）必须跟随其修饰的成员，不能断连
- `[Attribute]` 标注必须跟随其修饰的成员
- 预处理指令（`#if` / `#endif`）块保持完整不拆散
- 成员之间原有的空行/分隔注释保留（只需保证 region 内整洁）

### Step 3: 生成分块方案

对每个文件生成"改前 → 改后"的对比方案：

- 列出检测到的成员及其分类结果
- 标注：哪些成员会归入哪个 region
- 如果脚本已有 region，标注"将拆除旧 region 并重建"

**对于以下特殊情况**，不直接改动，而是标记为"需人工处理"：

- 类内包含 `#if UNITY_EDITOR` 等条件编译块且跨越多成员
- 文件包含多个顶层类型
- 部分类（`partial class`）

### Step 4: 展示方案并确认

将方案输出给用户，格式如下：

```markdown
## 🔧 Region 分块方案

**目标**：`<路径>`

### 成员分类

| 成员 | 类型 | → Region |
|------|------|----------|
| `_events0` | 字段 | 参数 |
| `Subscribe(...)` | public 方法 | 外部方法 |
| `Dispatch(...)` | public 方法 | 外部方法 |
| `Awake()` | 生命周期 | 生命周期 |
| `InDispatch(...)` | private 方法 | 内部方法 |

### 分块结构预览

\`\`\`csharp
#region 参数
...
#endregion

#region 外部方法
...
#endregion

#region 生命周期
...
#endregion

#region 内部方法
...
#endregion
\`\`\`

**共 N 个成员，4 个 region。**
```

用户确认后执行写入。

### Step 5: 写入

- 使用 Edit 工具逐步替换文件内容
- 如果替换范围太大（文件超过 200 行），使用 Write 工具整体重写
- 写入后验证：确认大括号配对、region 数量正确

---

## 分块模板

生成的代码结构如下：

```csharp
namespace Xxx
{
    public class Example : MonoBehaviour
    {
        #region 参数

        private int _value;

        #endregion

        // ============================================================

        #region 外部方法

        public void DoWork() { }

        #endregion

        // ============================================================

        #region 生命周期

        private void Awake() { }
        private void OnDestroy() { }

        #endregion

        // ============================================================

        #region 内部方法

        private void Helper() { }

        #endregion
    }
}
```

规则：
- 块之间用一行 `// ============================================================` 分隔
- 如果某个块无内容（如 static class 无生命周期），直接跳过，不留空 region
- 内部方法的嵌套类型（enum / struct / class）放在"内部方法"region 末尾
- Region 名称使用中文：`参数`、`外部方法`、`生命周期`、`内部方法`

---

## 输出格式

执行完毕后给出摘要：

```markdown
## ✅ Region 分块完成

| 文件 | 成员数 | 参数 | 外部方法 | 生命周期 | 内部方法 | 备注 |
|------|--------|------|----------|----------|----------|------|
| `Core/EmberEventBus.cs` | 28 | 6 | 17 | — | 5 | static，无生命周期 |
| `UI/UIPanel.cs` | 12 | 3 | 4 | 2 | 3 | |

**跳过**（N 个文件）：无成员 / 非 C# 文件 / 其他原因

**需人工处理**（M 个文件）：条件编译跨成员 / 多顶层类型
```

---

## 易错点

- **XML 注释不能和成员分离**——`/// <summary>` 必须紧贴在方法签名前，不能因 region 重组断开
- **属性 (`[SerializeField]`) 必须跟随字段**，不能落到其他 region
- **泛型参数列表中的逗号**不能误判为多个参数
- **嵌套类型**（类中类/类中枚举）放在"内部方法"region 末尾
- **static class** 跳过"生命周期"region，不要留空块
- **interface** 只有"参数"和"外部方法"（接口无实现），酌情处理，不强制四块结构
- 注释风格已有分块分隔线（`// ====`）的旧脚本，先拆除旧分隔线再加 region
- 如果文件中已有 `#region`，先拆除旧 region，不要嵌套

---

## 验证

- [ ] 所有 XML 注释跟随原成员
- [ ] 所有 `[Attribute]` 跟随原成员
- [ ] 大括号配对正确
- [ ] 没有空 region 残留
- [ ] 嵌套类型未被拆散
- [ ] 预处理指令块保持完整
