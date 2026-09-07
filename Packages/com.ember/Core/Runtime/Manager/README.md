# Manager 与 Module — 框架基座和业务积木

## 概述

这里定义两条彼此平行的生命周期管道：框架必要的 Manager，以及可选组合的业务 Module。
Manager 通过 `EmberInitOrderAttribute` 指定初始化优先级；Module 通过
`EmberModuleAttribute` 声明生命周期阶段和启用状态。

本文中的 Manager / Module 专指 `IEmberManager` / `IEmberModule` 生命周期角色；Package 内的
“子系统”或程序集分组不等同于这里的可选业务 Module。

## 统一定义

```text
具体游戏 = 框架基础 + 必备 Managers + 按需选装的 Modules
```

| 对比项 | Manager | Module |
|---|---|---|
| 架构定位 | 框架必要管理器、全局基础设施 | 可选业务模块，可像积木一样组合 |
| 接口 | `IEmberManager` | `IEmberModule` |
| 是否可由模板省略 | 否；被定义为 Manager 的能力应由所有模板共同具备 | 是；类型可以不存在，也可以通过 `Enabled = false` 关闭 |
| 启动时机 | `InitState` 中反射发现并按顺序调用 `Init()` | Init 阶段仅发现并构造；进入声明的 `Phase` 后才调用 `OnInit()` |
| 存活范围 | 从 Init 到框架退出，跨业务状态持续可用 | 只在所属 Phase 活跃，退出 Phase 时调用 `OnDestroy()` |
| 依赖方向 | 提供稳定的框架能力，不依赖具体业务 Module | 可以组合并消费一个或多个 Manager |

Manager 和 Module 不是父子关系，也不能仅凭类名判断。一个功能是否属于 Manager，取决于它是否是
所有游戏都必须在 Init 阶段启动的框架基础设施；一个功能是否属于 Module，取决于它能否按模板和
玩法阶段选装。不同游戏共享同一套框架与 Managers，通过选择不同 Modules 形成不同玩法组合。

例如，`EmberInputManager` 是所有模板都需要的输入基础设施；`PlayerControlModule` 只是把输入转换
成某种 2.5D 相机操作的业务积木，`base` 可以不包含它。

这里的“必要”是架构归类约束：Collector 会初始化当前已加载程序集里存在的 Manager 实现，但不会
拿一份固定清单检查缺失项。因此模板和 Package 维护者必须保证框架定义的 Managers 没有被裁掉。

### 新能力如何归类

1. 所有游戏从 Init 到退出都必须依赖，并且需要提供稳定的全局服务：实现 `IEmberManager`。
2. 只有部分模板或部分游戏状态需要，可以独立添加、移除或关闭：实现 `IEmberModule`。
3. 只是某项能力的内部运行对象：保持普通类，由对应 Manager 或 Module 持有，不因为名称里有
   Manager/Engine 就加入生命周期管道。

这条边界比“是否需要单例访问”更重要。可选功能即使需要方便访问，也仍应是 Module，并通过
`EmberModuleCollector.TryGetModule` 获取已装配实例。

## 文件清单

| 角色 | 路径 |
|------|------|
| 管理器接口 | `IEmberManager.cs` |
| 业务模块接口 | `IEmberModule.cs` |
| 初始化优先级特性 | `EmberInitOrderAttribute.cs` |
| 业务模块元数据 | `EmberModuleAttribute.cs` |
| 管理器自动收集器 | `EmberManagerCollector.cs` |
| 业务模块收集器 | `EmberModuleCollector.cs` |

## 公开 API

### IEmberManager — 管理器接口

实现此接口并提供公开静态 `Instance` 的具体类，会在 `InitState` 中被 ManagerCollector 自动发现，
按 `EmberInitOrder` 调用 `Init()`。Manager 是框架必要组件，不提供类似 Module 的模板级 Enabled
开关；如果某项能力不是所有游戏都需要，就应设计成 Module 或由 Module 持有的普通对象。

| 方法 | 说明 |
|------|------|
| `Init()` | 初始化，由 Collector 按 InitOrder 顺序调用 |
| `Destroy()` | 销毁，由 Collector 按 InitOrder 逆序调用 |

```csharp
[EmberInitOrder(EmberInitOrderAttribute.Core)]
public class MyManager : EmberSingleton<MyManager>, IEmberManager
{
    void IEmberManager.Init() { /* 初始化 */ }
    void IEmberManager.Destroy() { /* 清理 */ }
}
```

### IEmberModule — 业务模块接口

与 IEmberManager 平行，代表“只在部分游戏或部分状态中需要”的业务积木（玩家控制、战斗系统、
场景 UI 等）。模板可以不提供某个 Module，也可以使用 `Enabled = false` 保留代码但关闭装配。

启用的 Module 会在 `InitState` 被发现并构造，以便后续场景安全绑定；这一步不会调用 `OnInit`，
也不表示模块已经启动。只有状态机进入其 `Phase`、Collector 调用 `InitPhase` 且 `OnInit` 成功后，
模块才处于活动状态并能接收 Update。

| 成员 | 说明 |
|------|------|
| `OnInit()` | 模块初始化 |
| `OnDestroy()` | 模块销毁 |
| `ResetModuleData()` | 热重启：清空运行时数据，保留对象引用 |

```csharp
[EmberModule(ModulePhase.Gameplay)]
public sealed class BattleModule : EmberSingleton<BattleModule>, IEmberModule
{
    void IEmberModule.OnInit() { /* 初始化 */ }
    void IEmberModule.OnDestroy() { /* 清理 */ }
    void IEmberModule.ResetModuleData() { /* 热重启 */ }
}
```

`Enabled = false` 写在特性上。Collector 会先读取特性，再决定是否访问 `Instance`，因此禁用模块
不会被构造。缺少 EmberModuleAttribute 的具体模块也会被跳过并报告警告。

### EmberInitOrderAttribute — 初始化优先级

| 常量 | 值 | 说明 |
|------|-----|------|
| `Core` | 100 | 基础设施（EventBus、ServiceLocator） |
| `Resource` | 200 | 资源系统 |
| `Audio` | 300 | 音频 |
| `Input` | 400 | 输入 |
| `UI` | 500 | UI |
| `Scene` | 600 | 场景 |
| `Game` | 700 | 业务层 |
| `Default` | 1000 | 未标注时的 fallback |

### EmberManagerCollector — 管理器自动收集

继承 EmberSingleton，反射扫描所有程序集中实现 IEmberManager 的类，按 InitOrder 排序后初始化。

| 方法 | 说明 |
|------|------|
| `InitializeAll()` | 扫描并初始化所有管理器。可安全重复调用 |
| `DestroyAll()` | 逆序销毁所有管理器，异常不中断 |
| `ManagerCount` (属性) | 已发现的管理器数量 |

### EmberModuleCollector — 可选模块装配与阶段生命周期

| 方法/属性 | 说明 |
|---|---|
| `DiscoverModules()` | 发现、构造并登记所有 Enabled Module；不调用 `OnInit` |
| `IsModuleEnabled<T>()` | 读取类型特性中的装配开关；不创建实例 |
| `TryGetModule<T>(out module)` | 获取已经登记的模块；不扫描、不创建实例 |
| `InitPhase(phase)` | 激活指定 Phase；首次调用 `OnInit`，重入先 Reset 再 Init |
| `DestroyPhase(phase)` | 退出指定 Phase，调用活动模块的 `OnDestroy`，保留实例供重入 |
| `DestroyAll()` | 框架退出时销毁所有活动 Module 并清空登记表 |
| `IsDiscovered` / `ModuleCount` | 查询发现状态与已装配模块数量 |

`Enabled` 是启动扫描时的类型级装配开关，不是运行时热插拔 API。当前框架内置状态已接入
Global 与 Gameplay；`ModulePhase.Main` 或自定义 Phase 需要由对应状态显式调用 `InitPhase` 和
`DestroyPhase`。

## 主流程

**InitState：** `DiscoverModules()`（仅构造并登记启用 Module）→ `InitializeAll()`（发现并启动全部
Manager）→ `InitPhase(Global)`（激活全局业务 Module）。

**Manager 初始化：** `InitializeAll()` → 重复检查 → `ScanAndCollect()` → 反射遍历程序集 →
过滤系统程序集 → 筛选 `IEmberManager` → 反射获取 `Instance` → 读取 `EmberInitOrder` → 排序 →
依次调用 `Init()`。

**Module 阶段切换：** 进入阶段 → `InitPhase(phase)` → `OnInit()`；退出阶段 →
`DestroyPhase(phase)` → `OnDestroy()`。再次进入同一阶段时，先 `ResetModuleData()` 再 `OnInit()`。

**框架退出：** `EmberModuleCollector.DestroyAll()` 先销毁高层业务 Module →
`EmberManagerCollector.DestroyAll()` 再按初始化逆序销毁底层 Manager。

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| 反射扫描 | 扫描已加载的非系统程序集，跳过 System/Unity/Sirenix/UniTask 等已知基础程序集 |
| 初始化顺序 | InitOrder 值越小越先初始化，销毁时逆序 |
| IEmberModule | 必须声明 EmberModuleAttribute；阶段和启用状态不放在实例属性上 |
| Phase 接线 | 当前内置 Global 与 Gameplay；不要假设声明任意 Phase 后状态机会自动驱动 |
| 职责误用 | 可选业务功能不得为了全局访问方便而实现 IEmberManager；应保留为 Module 并通过 Collector 查询 |
