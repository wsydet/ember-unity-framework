# Update — 统一帧更新循环

## 概述

EmberUpdateManager 一处驱动所有模块的 Update/LateUpdate/FixedUpdate。
不需要继承 MonoBehaviour：实现 IEmberUpdate 等接口 + 继承单例基类，
UpdateManager 通过反射自动发现并按阶段分组调用。

## 文件清单

| 角色 | 路径 |
|------|------|
| 更新管理器 | `EmberUpdateManager.cs` |
| 更新接口 | `IEmberUpdate.cs` |

## 公开 API

### EmberUpdateManager — 统一 Update 循环

继承 EmberMonoSingleton，实现 IEmberManager。反射扫描所有实现更新接口的单例。

| 方法 | 说明 |
|------|------|
| `DoUpdate()` | 驱动所有 IEmberUpdate 的 Update()，按 CurrentPhase 过滤，异常不中断 |
| `DoLateUpdate()` | 驱动所有 IEmberLateUpdate 的 LateUpdate() |
| `DoFixedUpdate()` | 驱动所有 IEmberFixedUpdate 的 FixedUpdate() |
| `CurrentPhase` (属性) | 当前激活的模块阶段（默认 int.MaxValue 全量 Tick） |

> 注意：GameLauncher 在 Update/LateUpdate/FixedUpdate 中调用 EmberUpdateManager 的 DoXxx 方法。
> MonoBehabiour 自身的 Update 不自动驱动，需要通过 GameLauncher 桥接。

### IEmberUpdate / IEmberLateUpdate / IEmberFixedUpdate

| 方法 | 说明 |
|------|------|
| `Update()` | 每帧调用 |
| `LateUpdate()` | LateUpdate 阶段调用 |
| `FixedUpdate()` | 物理帧调用 |

```csharp
[EmberInitOrder(EmberInitOrderAttribute.Game)]
public class MyUpdater : EmberSingleton<MyUpdater>, IEmberUpdate
{
    public void Update() { /* 每帧逻辑 */ }
}
```

## 主流程

**采集：** `IEmberManager.Init()` → 反射遍历程序集 → 过滤系统程序集(Ember/Game开头保留) → 筛选接口实现 → 反射获取 Instance → 读取 EmberInitOrder 作为 phase → 分组存入字典

**驱动：** `DoUpdate()` → 同帧防重 → 遍历 _updaters → 按 CurrentPhase 过滤 → 逐个 Update() → try/catch 异常不中断

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| 驱动方式 | 必须由 GameLauncher 桥接调用 DoUpdate/DoLateUpdate/DoFixedUpdate。EmberUpdateManager 自身的 Update 不直接驱动 |
| 阶段过滤 | CurrentPhase 可控制只 Tick 特定阶段，默认全量 |
| 反射扫描 | 只扫描 Ember/Game 开头的程序集 |
| 异常安全 | 单个 updater 异常不影响其他 updater |
