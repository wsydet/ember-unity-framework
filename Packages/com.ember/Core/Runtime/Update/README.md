# Update — 统一帧更新循环

## 概述

EmberUpdateManager 一处驱动 Update/LateUpdate/FixedUpdate。业务 Module 的更新接收者只从
`EmberModuleCollector` 已登记的模块中取得，并且只有模块所属 Phase 已激活、`OnInit` 成功后才驱动；
其他非业务更新单例才通过反射发现并按优先级阈值调用。

## 文件清单

| 角色 | 路径 |
|------|------|
| 更新管理器 | `EmberUpdateManager.cs` |
| 更新接口 | `IEmberUpdate.cs` |

## 公开 API

### EmberUpdateManager — 统一 Update 循环

继承 EmberSingleton，实现 IEmberManager，是在 Init 阶段启动的框架必要 Manager。它复用已经发现的
业务 Module，绝不为了 Update 而自行创建被禁用或未装配的 Module。

| 方法 | 说明 |
|------|------|
| `DoUpdate()` | 驱动活动 Module 与符合优先级阈值的非业务 IEmberUpdate，异常不中断 |
| `DoLateUpdate()` | 驱动活动 Module 与符合优先级阈值的非业务 IEmberLateUpdate |
| `DoFixedUpdate()` | 驱动活动 Module 与符合优先级阈值的非业务 IEmberFixedUpdate |
| `CurrentPhase` (属性) | 非业务更新接收者的优先级阈值（默认 int.MaxValue） |

> 注意：GameLauncher 在 Update/LateUpdate/FixedUpdate 中调用 EmberUpdateManager 的 DoXxx 方法。
> MonoBehaviour 自身的 Update 不由本 Manager 代替，统一更新接口需要通过 GameLauncher 桥接。

### IEmberUpdate / IEmberLateUpdate / IEmberFixedUpdate

| 方法 | 说明 |
|------|------|
| `Update()` | 每帧调用 |
| `LateUpdate()` | LateUpdate 阶段调用 |
| `FixedUpdate()` | 物理帧调用 |

业务更新通常由可选 Module 实现：

```csharp
[EmberModule(ModulePhase.Gameplay)]
public class BattleModule : EmberSingleton<BattleModule>, IEmberModule, IEmberUpdate
{
    void IEmberModule.OnInit() { }
    void IEmberModule.OnDestroy() { }
    void IEmberModule.ResetModuleData() { }
    void IEmberUpdate.Update() { /* 仅在 Gameplay Phase 活动时调用 */ }
}
```

## 主流程

**采集：** `IEmberManager.Init()` → 读取 `ModuleCollector.DiscoveredModules` 并保存更新接口条目 →
反射扫描其余非 Module 更新单例 → 按 `EmberInitOrder` 优先级分组。

**驱动：** `DoUpdate()` → 非业务接收者按 `CurrentPhase` 阈值过滤 → Module 接收者按
`ModuleEntry.IsActive` 过滤 → 逐个 `Update()` → try/catch 异常不中断。

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| 驱动方式 | 必须由 GameLauncher 桥接调用 DoUpdate/DoLateUpdate/DoFixedUpdate。EmberUpdateManager 自身的 Update 不直接驱动 |
| Module 门控 | 业务 Module 必须已被 Collector 装配且 OnInit 成功；Enabled=false 的 Module 不会被创建或驱动 |
| 优先级过滤 | CurrentPhase 只影响非业务接收者，不是 Module Phase 的活动状态 |
| 反射扫描 | 只用于发现非业务更新单例；业务 Module 不会被二次反射创建 |
| 异常安全 | 单个 updater 异常不影响其他 updater |
