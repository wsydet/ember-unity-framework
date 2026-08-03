# Manager — 管理器基础设施

## 概述

定义管理器接口（IEmberManager / IEmberModule），通过 EmberInitOrderAttribute 指定初始化优先级，
由 EmberManagerCollector 反射扫描并自动初始化所有实现 IEmberManager 的单例。

## 文件清单

| 角色 | 路径 |
|------|------|
| 管理器接口 | `IEmberManager.cs` |
| 业务模块接口 | `IEmberModule.cs` |
| 初始化优先级特性 | `EmberInitOrderAttribute.cs` |
| 管理器自动收集器 | `EmberManagerCollector.cs` |

## 公开 API

### IEmberManager — 管理器接口

实现此接口 + 继承单例基类的类，会被 ManagerCollector 自动发现并初始化。

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

与 IEmberManager 平行，代表"只在某些游戏状态下才需要"的业务模块（战斗系统、网络连接等）。
IEmberManager 是框架管道（启动即初始化），IEmberModule 由状态机按 Phase 驱动。

| 成员 | 说明 |
|------|------|
| `int Phase { get; }` | 初始化阶段。Phase 0 保留给框架，业务从 Phase 1 开始 |
| `OnInit()` | 模块初始化 |
| `OnDestroy()` | 模块销毁 |
| `ResetModuleData()` | 热重启：清空运行时数据，保留对象引用 |

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

## 主流程

**初始化：** `InitializeAll()` → 重复检查 → `ScanAndCollect()` → 反射遍历程序集 → 过滤系统程序集(Ember/Game开头保留) → 筛选 IEmberManager → 反射获取 Instance → 读取 EmberInitOrder → 排序 → 依次调用 Init()

**销毁：** `DestroyAll()` → 逆序遍历 → 逐个 Destroy() → 异常不中断 → Clear

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| 反射扫描 | 只扫描以 Ember/Game 开头的程序集，跳过 System/Unity/Sirenix/UniTask 等 |
| 初始化顺序 | InitOrder 值越小越先初始化，销毁时逆序 |
| IEmberModule | 尚未实现 EmberModuleCollector，当前接口仅作为约定预留 |
