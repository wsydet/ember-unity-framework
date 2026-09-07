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

### Manager 与 Module 的组合模型

项目统一使用下面的架构定义：

```text
具体游戏 = 框架基础 + 必备 Managers + 按需选装的 Modules
```

| 概念 | Manager | Module |
|------|---------|--------|
| 定位 | 框架运行所必需的管理器和全局基础设施 | 可选、可组合的业务功能积木 |
| 接口 | `IEmberManager` | `IEmberModule` |
| 启动 | `InitState` 中由 `EmberManagerCollector` 反射发现，按 `EmberInitOrder` 调用 `Init` | `InitState` 只发现并构造 `Enabled = true` 的模块；进入其 `Phase` 后才调用 `OnInit` |
| 生命周期 | Init 阶段启动，跨游戏状态持续存活，框架退出时逆序销毁 | 仅在所属 Phase 激活，退出 Phase 时销毁业务状态，实例可供再次进入时复用 |
| 模板关系 | 所有模板共同具备，不作为玩法选项裁剪 | 模板按玩法自由添加、移除或通过 `Enabled = false` 关闭 |

- Manager 定义的是框架基座。被定义为 Manager 的能力必须在 `base` 和业务模板中都可用，不能为了
  某个模板临时省略；例如 `EmberInputManager`。
- “必要”是架构约束，不代表 Collector 内置了固定 Manager 清单；Collector 只会初始化当前已加载
  程序集中实际存在的实现，因此 Package 和模板维护者要负责保证必要 Managers 没有缺失。
- Module 定义的是业务组合单元。不同模板可以在同一套框架和 Managers 上装配不同 Modules，形成
  不同类型的游戏；例如 `PlayerControlModule` 只属于需要该操作方式的玩法模板。
- Module 可以消费 Manager 提供的通用能力；Manager 不依赖具体业务 Module。不要为了提前拿到实例
  而把可选业务功能实现成 `IEmberManager`。
- “发现并构造”不等于“已激活”：Module 只有 `OnInit` 成功后才进入活动状态并接收 Update。
- `Enabled` 是启动扫描时的类型级装配开关，不是运行时热插拔接口；当前内置自动接线覆盖 Global
  与 Gameplay，Main 或自定义 Phase 需要由对应状态显式驱动。

### 模板开发落盘规范

- `Packages/com.ember/Templates~/*/Assets` 与派生模板的 `ParentSnapshot~` 是模板系统管理的快照，
  不是日常业务开发目录。业务内容应先在项目 `Assets` 中修改，再通过模板开发面板保存。
- 正常流程固定为“加载模板 → 修改项目业务层 → 保存模板 → 显式 Bump 版本”。普通保存只更新实时
  `contentHash`，Bump 才把内容封存进 `versionedContentHash`。
- 禁止手工复制或直接修改模板快照，也禁止为了绕过校验而手改 `template.json` 的 hash。否则会破坏
  根模板、派生模板 `ParentSnapshot~`、父级指针和当前编辑记录之间的谱系一致性。
- 如果面板报告磁盘内容与 metadata 不一致，应先确认真实差异。经明确授权采纳磁盘改动时，必须一次性
  对齐根模板版本与 hash、派生模板父基线以及编辑记录；不得只修改单个子模板的 `contentHash`。

### 核心设计原则

- **依赖方向**：业务层 → 框架层 → 引擎，禁止反向依赖
- **单包交付**：框架统一由 `Packages/com.ember` 交付，并用 `.asmdef` 划分内部子系统；文档中的
  `IEmberModule` / Module 专指业务层可选积木，不等同于 Package 或框架程序集
- **程序集隔离**：通过 `.asmdef` 严格划分框架层和业务层的编译边界
- **接口驱动**：框架提供接口，业务层实现；框架不依赖业务层的具体类型

## 目录结构规划

```
Assets/
├── Game/                           # 业务层（示例/模板）
│   ├── Config/                     #   游戏配置
│   ├── Logic/                      #   游戏逻辑
│   ├── Module/                     #   可选业务 Module 积木
│   └── UI/                         #   游戏 UI
├── GameResource/                   # 业务资源
├── Editor/                         # 项目级编辑器配置
└── ThirdParty/                     # 非 UPM 第三方内容

Packages/
└── com.ember/                       # 统一框架 Package
    ├── Core/                        #   生命周期、状态机、Manager、Update
    ├── Resource/ Scene/ Audio/      #   框架必要 Managers 与通用能力
    ├── Camera/ Input/ UI/           #   框架必要 Managers 与通用能力
    ├── SceneUI/                     #   可由业务 Module 持有的通用引擎
    └── Templates~/                  #   base 与玩法模板
```

## 编码规范

### Unity 编译验证规范

- **Unity MCP 是唯一的自动编译验证入口。** 修改 C#、程序集定义、场景或资源后，优先通过 Unity MCP 触发刷新并读取编译结果。
- 如果 Unity MCP 可用，只做一次有界的编译状态确认；仅当 MCP 明确报告正在编译时，才等待该次编译完成，禁止无意义地重复轮询。
- 如果 Unity MCP 无法连接、调用超时或首次查询失败，立即停止编译验证，不再花费时间尝试证明项目能够编译。
- MCP 不可用时，禁止通过等待 Unity 自动刷新、反复读取 `Editor.log`、轮询 Unity 进程、启动 BatchMode、临时修改生成的 `.csproj`，或运行 `dotnet build` 来替代 Unity 编译验证。
- MCP 不可用时可以继续执行即时、非阻塞的静态检查，但不得声称“编译通过”。
- **强制显式提醒：本次任务涉及需要 Unity 编译验证的改动，或正在处理 Unity 编译报错时，只要 MCP 未连接、不可用、调用超时或首次查询失败，最终回复就必须明确写出“请在 Unity 中手动触发编译”。** 即使前文或上一轮已经提醒过，本轮最终回复仍不得省略。
- 手动编译提醒必须同时说明“本次未完成 Unity 编译验证”，并要求用户在仍有报错时发送首条编译错误及其完整堆栈。仅写“静态检查通过”“未验证”“请刷新”或“请 Reimport”不满足要求；Refresh/Reimport 等操作建议只能作为补充，不能代替显式的手动编译提醒。

MCP 不可用时，最终回复使用以下明确表述：

> 当前 Unity MCP 未连接或不可用，本次未完成 Unity 编译验证。请在 Unity 中手动触发编译；如果仍有报错，请将首条编译错误及其完整堆栈发回当前对话。

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
