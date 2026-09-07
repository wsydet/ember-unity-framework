# SceneUI 业务层 API 参考

---


## 示例的模块前置条件

以下业务片段中的 `sceneUI` 指已装配的模块。调用前先查询并检查 Channel 状态；退出阶段清理时重新查询，避免意外构造已禁用模块。

```csharp
if (!EmberModuleCollector.TryGetInstance(out EmberModuleCollector collector)
    || !collector.TryGetModule(out SceneUIModule sceneUI))
    return;
// Channel 就绪后使用下文 sceneUI；TryShow... 返回 false 时不保存无效句柄。
```


## 1. 快速上手

最快的接入方式不需要编写注册代码：

1. 在业务场景物体上添加 `SceneUIObject`。
2. 在 Inspector 中选择气泡类型。
3. 选择 `Static` 或 `Dynamic`。
4. 配置世界坐标偏移和初始可见状态。

组件会等待 World Channel Ready，并自动注册到 `SceneUIModule`。

需要直接使用业务 API 时：

```csharp
if (sceneUI.TryShowExampleBubble(
        transform,
        out SceneUIModuleHandle handle))
{
    // 在对象销毁或不再需要显示时注销 handle。
}
```

注销：

```csharp
sceneUI.UnregisterSceneUI(handle);
```

## 2. 模块概述

业务层负责把项目语义转换为通用 SceneUI 请求，包括：

- 定义业务 Channel 编号
- 定义业务气泡类型编号
- 指定 EUI 宿主页
- 提供显式 SceneCamera
- 加载业务 `SceneUIViewCatalog`
- 为每种气泡提供 Binder
- 选择动态或静态锚点策略
- 提供场景物体附加组件
- 决定业务默认边界、排序、回收和遮挡策略

当前最小闭环：

| 配置 | 当前值 |
|---|---|
| Channel | `World = 1` |
| View | `Example = 1001` |
| 宿主页 | `GamePages.EUISceneUIPage` |
| 页面类型 | `FreePage` |
| 页面 sortingOrder | `20000` |
| SceneCamera | `EmberCameraManager.Instance.MainCamera` |
| Catalog | `Resources/UI/Module/SceneUI/SceneUIViewCatalog` |
| Example 预制体 | `EUIBaseBubbleItem` |
| 预热数量 | `4` |
| 最大池容量 | `16` |
| 相机更新源 | Cinemachine |
| Context 排序 | `PriorityThenDepth` |

## 3. 依赖关系

| 依赖 | 用途 |
|---|---|
| `Ember.SceneUI` | Engine 请求、Anchor、Policy 和 Handle |
| `Ember.SceneUI.Integration` | Module 基类、宿主页、Catalog 和 EUI View |
| `Ember.Camera` | 获取由启动流程显式注入的主 SceneCamera |
| `Ember.Core` | `[EmberModule]` 与 Gameplay Phase |
| `Ember.UI` / `Game.UI` | `GamePages.EUISceneUIPage` 和 EUI Item 逻辑 |
| Odin Inspector | `SceneUIObject` 的业务配置与运行时状态面板 |
| UnityEngine | Transform、MonoBehaviour、Resources 和序列化 |

运行资源：

| 资源 | 用途 |
|---|---|
| `Assets/GameResource/Resources/UI/Module/SceneUI/SceneUIViewCatalog.asset` | 业务 View 映射 |
| `Assets/GameResource/Resources/UI/Common/Prefabs/EUISceneUIPanel.prefab` | World Channel 宿主页 |
| `Assets/GameResource/Resources/UI/Module/SceneUI/Prefabs/EUIBaseBubbleItem.prefab` | Example 气泡 |
| `Assets/Game/UI/GamePages.cs` | 宿主页页面定义 |
| `Assets/Game/UI/Runtime/Module/SceneUI/EUIBaseBubbleItem.cs` | Example EUI Item 逻辑 |

## 4. 文件清单

| 文件 | 职责 |
|---|---|
| `SceneUIModule.cs` | World Channel、业务 View 映射、Binder 和注册 API |
| `SceneUIObject.cs` | 场景物体附加组件及自动注册生命周期 |
| `SceneUIAutoMover.cs` | Play Mode 动态物体移动验证辅助脚本 |

`SceneUIAutoMover` 仅用于验证动态跟随效果，不是生产业务接入所必需的组件。

## 5. 公开 API

### 5.1 入口类型

| 类型 | 说明 |
|---|---|
| `SceneUIModule` | 项目唯一 SceneUI 业务模块 |
| `SceneUIObject` | 挂载到场景物体的业务入口 |
| `SceneUIChannelId` | 业务 Channel 枚举 |
| `SceneUIViewId` | 业务气泡类型枚举 |
| `SceneUIObjectUpdateMode` | 静态或动态物体语义 |
| `SceneUIAutoMover` | 动态跟随验证辅助组件 |

业务标识：

```csharp
SceneUIChannelId.World = 1
SceneUIViewId.Example = 1001
```

业务键：

```csharp
SceneUIModule.WorldChannel
SceneUIModule.ExampleView
```

### 5.2 核心方法

#### SceneUIModule

| 方法 | 说明 |
|---|---|
| `TryShowExampleBubble(...)` | 使用默认配置为 Transform 注册 Example 气泡 |
| `TryShowBubble(...)` | 按气泡类型、更新方式、偏移和初始显隐注册 |
| `NotifyWorldCameraUpdated()` | 通知 World Channel 的相机最终姿态已确定 |

`TryShowBubble` 当前业务默认值：

- 屏幕外模式：`Clamp`
- 屏幕 Padding：`24`
- 边界模式：`FullRect`
- 缩放：`Fixed`
- 回收：`RecycleAfterDelay`
- 延迟：`0.25` 秒
- 遮挡：关闭
- 动态物体：`PollPosition`
- 静态物体：`Manual`

模块还继承以下通用 API：

- `GetChannelState`
- `TryRegisterSceneUI`
- `UnregisterSceneUI`
- `IsSceneUIValid`
- `MarkSceneUIPositionDirty`
- `RefreshSceneUIPosition`
- `MarkSceneUIContentDirty`
- `SetSceneUIVisible`
- `SetChannelVisible`
- `TryGetDiagnostics`

#### SceneUIObject

| 成员 | 说明 |
|---|---|
| `BubbleType` | 当前业务气泡类型 |
| `UpdateMode` | 当前静态或动态配置 |
| `WorldOffset` | Transform 上方的世界坐标偏移 |
| `IsRegistered` | 当前模块级句柄是否有效 |
| `NotifyPositionChanged()` | 静态物体移动后通知位置变化 |
| `RefreshPosition()` | 应用当前偏移并请求重新投影 |
| `SetVisible(bool)` | 修改业务显隐 |
| `RefreshRegistration()` | 配置变化后注销并重新注册 |

Inspector 配置字段没有公开 Setter。常规业务应通过预制体或场景序列化配置；如果需要完全动态地决定类型，应直接调用 `SceneUIModule.TryShowBubble`，或增加明确的业务 API。

## 6. 主流程

### 6.1 模块初始化

```text
Ember Module Collector 发现 [EmberModule(ModulePhase.Gameplay)]
  → 创建 SceneUIModule
  → 注册 World Channel
  → 打开 EUISceneUIPage
  → 加载 SceneUIViewCatalog
  → 绑定 BubbleRoot
  → World Channel Ready
  → 通知全部启用中的 SceneUIObject 尝试注册
```

模块不使用单独的 `Enabled` 开关。是否加载由 `[EmberModule(ModulePhase.Gameplay)]` 声明。

### 6.2 场景物体生命周期

```text
SceneUIObject.OnEnable
  → 加入活动对象集合
  → 若模块和 Channel 已 Ready，则立即注册
  → 否则等待 ChannelReady

SceneUIObject.OnDisable
  → 注销 SceneUIModuleHandle
  → 从活动对象集合移除
```

因此场景对象可以早于宿主页出现，也可以晚于 Channel Ready 激活。

### 6.3 静态与动态物体

`Static`：

- 使用 `SceneUIUpdatePolicy.Manual`
- 不在每次 Channel 刷新时轮询 Transform
- 相机变化时仍会重新投影
- 业务主动移动对象后调用 `NotifyPositionChanged()` 或 `RefreshPosition()`

`Dynamic`：

- 使用 `SceneUIUpdatePolicy.PollPosition`
- 每次 World Channel 刷新时检查 Transform 位置
- 适用于移动单位、跟随对象和动态交互目标

### 6.4 相机更新

当前 World Channel 使用：

```csharp
new CinemachineSceneUICameraUpdateSource(
    EmberCameraManager.Instance.MainCamera);
```

更新源只接受 `brain.OutputCamera` 与显式 SceneCamera 一致的完成事件。

非 Cinemachine 流程，或者相机最终姿态在其他阶段才确定时，由业务在最终更新后调用：

```csharp
sceneUI.NotifyWorldCameraUpdated();
```

## 7. 修改影响范围

| 修改点 | 可能影响 |
|---|---|
| `SceneUIChannelId` | Channel 存档、业务调用和诊断标识 |
| `SceneUIViewId` | Catalog Key、Inspector 序列化值和 Binder 映射 |
| `TryResolveView` | 气泡类型能否解析到正确 View 和 Binder |
| `ResolveSceneCamera` | World Channel 投影来源与相机更新过滤 |
| `ResolveViewCatalog` | Channel 准备状态和全部气泡资源 |
| `TryShowBubble` 默认策略 | 所有 `SceneUIObject` 的边界、回收与刷新行为 |
| `SceneUIObject` 生命周期 | 场景加载、禁用、销毁和模块热重启 |
| Example Binder | `EUIBaseBubbleItem` 类型约束和内容刷新 |
| Catalog 预热与池容量 | 首次显示峰值和运行时实例数量 |
| 宿主页或 BubbleRoot | 所有气泡的父节点和排序体系 |

修改 `SceneUIViewId` 的已有数值前，应确认没有场景、预制体或 Catalog 资产仍序列化旧值。

## 8. 约束与已知陷阱

| 约束 | 说明 |
|---|---|
| SceneCamera 来源必须显式 | 当前来源是 `EmberCameraManager`，禁止加入 `Camera.main` 兜底 |
| Catalog Key 必须与业务枚举一致 | `Example = 1001` 必须映射 Catalog Key `1001` |
| 每种气泡都必须有 Binder 映射 | 仅添加枚举和 Catalog 不足以完成业务接入 |
| 气泡预制体必须是 EUI Item | 不重新引入 `SceneUIViewBehaviour` |
| 气泡不能拥有独立排序 Canvas | 所有实例留在宿主页 `BubbleRoot` 下 |
| `TryShowBubble` 只在 Channel Ready 后成功 | 场景组件会自动等待；直接 API 调用方需处理 Loading |
| 静态物体移动需要显式通知 | 否则只有相机变化时才可能更新其投影 |
| 同一 Context 每帧最多刷新一次 | 应在相机最终姿态确定后统一通知 |
| Binder 单例只能保存无状态逻辑 | 每个气泡的独立内容不能写进共享 Binder 字段 |
| `SceneUIObject` 禁止重复挂载 | 类型带有 `[DisallowMultipleComponent]` |
| 模块销毁会使旧句柄失效 | 不能跨模块代次缓存并复用句柄 |
