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