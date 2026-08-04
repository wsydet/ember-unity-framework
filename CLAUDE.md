# CLAUDE.md — ember-unity-framework

## 项目概述

**ember-unity-framework** 是一个通用的 Unity 游戏开发框架，目标是：

1. **隔离式的框架层与业务层** —— 框架提供基础能力（事件系统、资源管理、UI 管理、场景管理等），业务层在此之上构建具体游戏逻辑，两者通过清晰的 API 边界解耦
2. **开箱即用** —— 其他人拿到框架后可以快速开始一个新项目，只需关注业务逻辑
3. **蓝图式可视化编辑** —— 长期目标是提供类似蓝图的节点编辑器，让策划/设计师也能通过可视化界面搭建游戏逻辑（基于 Unity Visual Scripting 或自定义编辑器扩展）

## 技术栈

| 类别 | 选型 |
|------|------|
| 引擎 | Unity 6000.x（Unity 6） |
| 渲染管线 | URP（Universal Render Pipeline） |
| 语言 | C# |
| 输入系统 | Unity Input System 1.19 |
| 可视化脚本 | Unity Visual Scripting（用于蓝图基础） |
| UI 系统 | uGUI / UI Toolkit |
| 测试 | Unity Test Framework |

## 架构理念

```
┌─────────────────────────────────┐
│          业务层 (Game)           │  ← 具体游戏逻辑，可替换
│   ┌──────────┐ ┌──────────┐     │
│   │ 战斗系统  │ │  背包系统  │ ... │
│   └──────────┘ └──────────┘     │
├─────────────────────────────────┤
│         框架层 (Framework)       │  ← 通用能力，稳定不变
│   ┌─────┐ ┌─────┐ ┌─────┐      │
│   │Event│ │ResM│ │UIMgr│ ...    │
│   └─────┘ └─────┘ └─────┘      │
├─────────────────────────────────┤
│           Unity Engine           │
└─────────────────────────────────┘
```

### 核心设计原则

- **依赖方向**：业务层 → 框架层 → 引擎，禁止反向依赖
- **Package 化**：框架的每个模块以独立的 Unity Package（`com.ember.xxx`）形式组织，按需引用
- **程序集隔离**：通过 `.asmdef` 严格划分框架层和业务层的编译边界
- **接口驱动**：框架提供接口，业务层实现；框架不依赖业务层的具体类型

## 目录结构规划

```
Assets/
├── Ember/                          # 框架层（运行时）
│   ├── Core/                       #   核心：事件总线、单例模式、对象池
│   ├── Resource/                   #   资源管理：AssetBundle、Addressables
│   ├── UI/                         #   UI 管理：界面栈、生命周期
│   ├── Scene/                      #   场景管理：加载/卸载、过渡
│   ├── Audio/                      #   音频管理
│   ├── Input/                      #   输入抽象层
│   └── Editor/                     #   框架编辑器工具
├── Game/                           # 业务层（示例/模板）
│   ├── Config/                     #   游戏配置
│   ├── Logic/                      #   游戏逻辑
│   └── UI/                         #   游戏 UI
├── Plugins/                        # 第三方插件
└── Resources/                      # 运行时资源

Packages/
├── com.ember.core/                 # 框架核心 Package
├── com.ember.ui/                   # UI 框架 Package
├── com.ember.resource/             # 资源框架 Package
├── ...（更多框架 Package）
└── com.ember.blueprint/            # 蓝图编辑器 Package（远期目标）
```

## 编码规范

### 🔍 写代码前必查：API 速查手册

**在写任何新工具类、扩展方法、数据结构之前，必须先查阅 [docs/dev/ember-api-reference.md](docs/dev/ember-api-reference.md)**，确认没有现成的轮子。

手册覆盖：集合池、基础数据结构、扩展方法、异步 (STTask)、JSON、事件系统、服务定位、日志、资源管理、UI、场景、音频、输入、状态机、Update 循环、Manager 发现、Attribute 标记 等 20+ 个类别。

### 命名约定

- **类名**：PascalCase，框架类加 `Ember` 前缀（如 `EmberEventBus`）
- **接口**：以 `I` 开头（如 `IEmberService`）
- **方法**：PascalCase
- **私有字段**：`_camelCase` 前缀下划线
- **常量**：UPPER_SNAKE_CASE
- **命名空间**：`Ember.<模块名>` 用于框架，`Game.<模块名>` 用于业务

### 代码风格

- 优先使用 `internal` 访问修饰符，只暴露必要的 `public` API
- 避免 `GameObject.Find` 和 `FindObjectOfType`，使用依赖注入或注册机制
- 所有 `MonoBehaviour` 生命周期方法使用 `private`，避免外部调用
- 使用 `[SerializeField]` 暴露 Inspector 字段而非 `public` 字段
- **禁止直接使用 `Debug.Log`**：全部日志通过 `EmberDebug` 输出（`Log`/`LogInit`/`LogEvent`/`LogCleanup`/`LogShutdown`/`LogWarning`/`LogError`），利用标签过滤和彩色输出。
  - `LogInit`（绿色）：系统初始化、组件注册
  - `LogEvent`（紫色）：事件播报、状态切换
  - `LogCleanup`（灰色）：资源释放、模块卸载
  - `LogShutdown`（淡紫色）：框架退出、最终清理（与 Init 呼应）
  - `Log`（白色）：常规信息
  - `LogWarning` / `LogError`：警告和错误

### XML 文档注释规范

**泛型尖括号用 `《》` 替代 `<>`**。XML 不允许 `<` `>` 裸写在文本中，必须转义为 `&lt;` `&gt;`，但这在源码中可读性极差。统一用 `《》`：

```csharp
/// ListPool《int》 的池子里装的都是 List《int》
/// var list = ListPool《RaycastHit》.Get();
/// for (int i = 0; i 《 list.Count; i++)
```

而不是：

```csharp
/// ListPool&lt;int&gt; 的池子里装的都是 List&lt;int&gt;
/// var list = ListPool&lt;RaycastHit&gt;.Get();
/// for (int i = 0; i &lt; list.Count; i++)
```

`《` `》` 在 XML 中不是特殊字符，无需转义，人读起来也直观。

### `[HasGC]` / `[NoGC]` 标注规范

**`[HasGC]` 和 `[NoGC]` 标记每个静态方法是否产生 GC 分配**，让调用方在热路径上一眼就知道能不能放心用。

判定规则：方法体里有 `new`（分配新对象）、`string` 拼接/格式化/插值、接口类型枚举器
（struct 装箱）、闭包/lambda → `[HasGC]`；纯计算、纯遍历、只调 `[NoGC]` 方法 → `[NoGC]`。

**要标的方法**：
- 所有 `public static` 方法 —— 调用方不知道内部实现，必须在签名处标明
- 性能敏感类型的 `public` 实例方法 —— 池的 Get/Return、StringView 的比较等
- 如果调用方会在热路径上调、会纠结"这方法有没有 GC"——就标

**不标的方法**：
- `private` / `internal` 方法 —— 外部不可见，实现细节
- 天生就有 GC 的类型 —— `JsonMapper`、`JsonData`、`StringBuilder` 相关，全标没有意义
- Editor-only 代码 —— 不在运行时热路径上
- 简单属性/字段 getter —— 不可能有 GC

```csharp
// ✅ 标注
[HasGC]
public static List<T> Get(int capacity = 16) { ... }  // 池空时会 new

[NoGC]
public static void Return(List<T> list) { ... }        // 只调 Clear + Add

// ❌ 不标
private static void ValidateInput() { ... }            // private，外面看不到
```

### 代码组织

使用 `#region` 将类内成员按职责分为最多五个块，按以下顺序排列：

| 顺序 | Region | 内容 | 说明 |
|------|--------|------|------|
| 1 | `编辑器面板参数` | `[SerializeField]` / `public` 字段 | Inspector 中可见的序列化字段 |
| 2 | `内部参数` | `private` 字段（非序列化）、属性、常量、事件 | 不暴露在 Inspector 中的数据成员 |
| 3 | `生命周期` | `Awake` / `Start` / `OnDestroy` 等 | 仅 MonoBehaviour 有此块，普通类跳过 |
| 4 | `内部方法` | `private` 方法、嵌套类型 | 内部实现细节和辅助方法 |
| 5 | `外部方法` | `public` / `internal` 方法 | 对外暴露的 API |

```csharp
public class ExampleManager : MonoBehaviour
{
    #region 编辑器面板参数

    [SerializeField] private float _duration = 1f;

    #endregion

    // --------------------------------------------------------

    #region 内部参数

    private bool _isRunning;
    private const int MAX_COUNT = 10;

    #endregion

    // --------------------------------------------------------

    #region 生命周期

    private void Awake() { ... }
    private void OnDestroy() { ... }

    #endregion

    // --------------------------------------------------------

    #region 内部方法

    private void Tick() { ... }
    private void Cleanup() { ... }

    #endregion

    // --------------------------------------------------------

    #region 外部方法

    public void StartProcess() { ... }
    public void StopProcess() { ... }

    #endregion
}
```

规则：
- 块之间用 `// ----` 或 `// ====` 分隔线隔开
- 如果某个块没有内容（如普通 class 没有生命周期），直接跳过，不留空 region
- static class 通常只有 `内部参数`、`内部方法`、`外部方法` 三个块
- `编辑器面板参数`：仅放置 Inspector 可见的序列化字段（`[SerializeField]`、`[SerializeReference]`、`public` 字段）
- `内部参数`：放置 `private` 非序列化字段、`const`、`static`、`readonly`、属性、事件

### 程序集划分

- 框架层每个模块有独立的 `.asmdef`，模块间通过引用链接
- 业务层 `.asmdef` 只能引用框架层，不能反向
- 编辑器代码放在独立的 `Editor` 程序集中