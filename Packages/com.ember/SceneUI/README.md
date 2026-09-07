# SceneUI 框架层 API 参考

---

## 1. 快速上手

业务通常继承 `EmberSceneUIModuleBase<TModule>` 注册 Channel，由框架负责 Engine、宿主页与资源生命周期：

```csharp
protected override void RegisterSceneUIChannels()
{
    HostedSceneUIContextOptions options = HostedSceneUIContextOptions.Default;
    options.SortingMode = SceneUISortingMode.PriorityThenDepth;

    RegisterCinemachineChannel(
        WorldChannel,
        hostPage,
        sceneCamera,
        viewCatalog,
        options);
}
```

`sceneCamera`、`hostPage` 和 `viewCatalog` 必须由业务显式提供。框架不会使用 `Camera.main` 或场景搜索推断相机。

注册一个场景 UI：

```csharp
TryRegisterSceneUI(
    WorldChannel,
    new SceneUIModuleRequest
    {
        ViewKey = viewKey,
        Anchor = new TransformSceneUIAnchor(target),
        UpdatePolicy = SceneUIUpdatePolicy.PollPosition,
        VisibilityPolicy = SceneUIVisibilityPolicy.DefaultHide,
        ScalePolicy = SceneUIScalePolicy.Fixed,
        ViewLifetimePolicy = SceneUIViewLifetimePolicy.RecycleAfterDelay,
        RecycleDelaySeconds = 0.25f,
        OcclusionPolicy = SceneUIOcclusionPolicy.Disabled,
        BusinessVisible = true,
    },
    out SceneUIModuleHandle handle);
```

不再需要显示时，应使用模块级句柄注销：

```csharp
UnregisterSceneUI(handle);
```

## 2. 模块概述

SceneUI 框架负责把世界空间锚点投影到现有 EUI 页面中的 `BubbleRoot`，并统一处理：

- Channel 和 Context 隔离
- 相机完成更新后的批量投影
- 屏幕边界裁剪、隐藏与钳制
- 动态和静态锚点更新
- EUI Item 的创建、绑定、复用和回收
- View 预热与池容量
- 深度和业务优先级排序
- 可选的物理遮挡检测
- 版本化句柄校验
- 运行时诊断

框架由两层组成：

- `Ember.SceneUI`：与具体 UI、资源和相机框架无关的投影核心。
- `Ember.SceneUI.Integration`：连接 Ember Module、EUI、Resources 和 Cinemachine。

`EmberSceneUIEngine` 是普通可释放对象，不是全局单例。典型项目由 `EmberSceneUIModuleBase<TModule>` 创建并持有 Engine。

## 3. 依赖关系

| 依赖 | 用途 |
|---|---|
| UnityEngine / Unity UI | Camera、Transform、RectTransform、Canvas 和坐标转换 |
| Unity Cinemachine | 在目标 SceneCamera 完成本帧更新后驱动 Channel |
| Ember.Basic | GC 标记、日志与基础约定 |
| Ember.Core | `IEmberModule`、模块生命周期和 `EmberSingleton<T>` |
| Ember.UI | EUI 页面、页面定义和页面管理 |
| Ember.UIExtension | EUI Item 绑定与逻辑访问 |
| Ember.Resource | 异步加载资源型气泡预制体 |

框架层不依赖 `Assets/Game` 中的业务枚举、页面编号或气泡类型。

## 4. 文件清单

| 文件 | 职责 |
|---|---|
| `Runtime/Core/EmberSceneUIEngine.cs` | Context、Entry、投影、显隐、排序、回收和诊断主引擎 |
| `Runtime/Core/SceneUIAnchors.cs` | Transform 与固定世界坐标锚点 |
| `Runtime/Core/SceneUIContracts.cs` | Anchor、View、Binder、ViewHost、相机更新源等契约 |
| `Runtime/Core/SceneUIHandles.cs` | Channel、Context、Entry 和 View 的类型安全标识 |
| `Runtime/Core/SceneUIModels.cs` | Context、Request、空间状态和诊断数据 |
| `Runtime/Core/SceneUIPolicies.cs` | 更新、边界、缩放、回收、排序与遮挡策略 |
| `Runtime/Core/SceneUIProjection.cs` | 世界投影、可见区域、Canvas 映射和缩放计算 |
| `Runtime/Integration/EmberSceneUIModuleBase.cs` | 面向业务的 SceneUI 模块基类 |
| `Runtime/Integration/SceneUIModuleModels.cs` | Channel 描述、模块请求与模块级句柄 |
| `Runtime/Integration/HostedSceneUIContext.cs` | 把 Engine Context 绑定到现有 EUI 宿主页 |
| `Runtime/Integration/SceneUIPageHost.cs` | 声明并验证宿主页 `BubbleRoot` |
| `Runtime/Integration/CinemachineSceneUICameraUpdateSource.cs` | 监听指定 SceneCamera 的 Cinemachine 更新完成事件 |
| `Runtime/Integration/EUIItemSceneUIView.cs` | 把 EUI Item 适配为 `ISceneUIView` |
| `Runtime/Integration/PrefabSceneUIViewHost.cs` | EUI Item 预制体实例化、池化和预热 |
| `Runtime/Integration/ResourceSceneUIViewHost.cs` | 异步加载资源后建立可同步获取的 ViewHost |
| `Runtime/Integration/SceneUIViewCatalog.cs` | 直接预制体 Catalog |
| `Runtime/Integration/SceneUIViewResourceCatalog.cs` | 资源地址型 Catalog |
| `Runtime/Integration/SceneUIViewResourceLoader.cs` | Ember Resource 加载适配器 |
| `Runtime/Integration/PhysicsSceneUIOcclusionTester.cs` | 基于目标相机 PhysicsScene 的遮挡检测 |
| `Editor/SceneUIDiagnosticsWindow.cs` | SceneUI 运行时诊断窗口 |

## 5. 公开 API

### 5.1 入口类型

| 类型 | 说明 |
|---|---|
| `EmberSceneUIEngine` | SceneUI 投影和 View 生命周期核心 |
| `EmberSceneUIModuleBase<TModule>` | 推荐的业务接入入口 |
| `SceneUIContextDescriptor` | Engine Context 的完整依赖描述 |
| `SceneUIRequest` | Engine 级 SceneUI 注册请求 |
| `SceneUIModuleRequest` | 模块级 SceneUI 注册请求 |
| `SceneUIChannelDescriptor` | 宿主页、相机、Catalog 和更新源描述 |
| `HostedSceneUIContextOptions` | 可见区域、排序、预热和遮挡配置 |
| `HostedSceneUIContextFactory` | 在现有 `SceneUIPageHost` 上创建 Context |
| `SceneUIPageHost` | EUI 宿主页中的 `BubbleRoot` 声明组件 |
| `SceneUIViewCatalog` | ViewKey 到 EUI Item 预制体的映射 |
| `SceneUIViewResourceCatalog` | ViewKey 到资源地址的映射 |
| `PrefabSceneUIViewHost` | 已加载预制体的同步 ViewHost |
| `ResourceSceneUIViewHost` | 资源异步准备型 ViewHost |
| `EUIItemSceneUIView` | EUI Item 的 SceneUI View 包装 |
| `TransformSceneUIAnchor` | 跟随 Transform 的锚点 |
| `WorldPositionSceneUIAnchor` | 由业务写入固定世界坐标的锚点 |
| `ManualSceneUICameraUpdateSource` | 由业务显式触发的相机更新源 |
| `CinemachineSceneUICameraUpdateSource` | Cinemachine 更新源 |
| `PhysicsSceneUIOcclusionTester` | 物理射线遮挡检测器 |
| `ISceneUIBinder` | View 内容绑定、刷新和解绑契约 |
| `ISceneUIView` | Engine 操作 View 的最小契约 |
| `ISceneUIViewHost` | View 获取和回收契约 |
| `ISceneUIAnchor` | 世界坐标来源契约 |
| `ISceneUICameraUpdateSource` | Channel 刷新时机契约 |
| `ISceneUIVisibleRegionProvider` | 屏幕可见区域契约 |
| `ISceneUIOcclusionTester` | 遮挡检测契约 |
| `SceneUIHandle` | Engine Entry 的版本化句柄 |
| `SceneUIContextHandle` | Engine Context 的版本化句柄 |
| `SceneUIModuleHandle` | 带模块代次与 Channel 的业务句柄 |
| `SceneUIChannelKey` | Channel 标识，值 `0` 无效 |
| `SceneUIViewKey` | View 类型标识，值 `0` 无效 |
| `SceneUIDiagnostics` | Engine 累计运行时诊断 |
| `SceneUIEntryDiagnostics` | 单个 Entry 的运行状态 |

主要策略枚举：

- `SceneUIUpdatePolicy`：`Manual`、`PollPosition`
- `SceneUIOutOfBoundsMode`：隐藏、钳制或保留越界位置
- `SceneUIViewLifetimePolicy`：保持、立即回收或延迟回收
- `SceneUIBoundsMode`：点、局部矩形或完整矩形
- `SceneUISortingMode`：无排序、业务优先级、优先级加深度
- `SceneUIScaleMode`：固定缩放或相对缩放
- `SceneUIOcclusionState`：遮挡状态
- `SceneUIInvisibleReason`：不可见原因

### 5.2 核心方法

#### EmberSceneUIEngine

| 方法 | 说明 |
|---|---|
| `RegisterContext(...)` | 注册一套相机、Canvas、Root、ViewHost 和更新源 |
| `Register(...)` | 注册单个 SceneUI Entry |
| `TryRegisterBatch(...)` | 批量注册 Entry |
| `Unregister(...)` | 注销单个 Entry |
| `UnregisterBatch(...)` | 批量注销 Entry |
| `UnregisterContext(...)` | 注销 Context 及其全部 Entry |
| `FlushContext(...)` | 获取相机快照并批量刷新 Context |
| `MarkSpatialDirty(...)` | 标记锚点或空间状态变化 |
| `MarkContentDirty(...)` | 标记 Binder 内容需要刷新 |
| `MarkOcclusionDirty(...)` | 标记遮挡状态需要重测 |
| `MarkContextSpatialDirty(...)` | 强制 Context 空间刷新 |
| `SetBusinessVisible(...)` | 修改单个 Entry 的业务显隐 |
| `SetContextVisible(...)` | 修改整个 Context 的显隐 |
| `SetAnchor(...)` | 替换锚点 |
| `SetOffsets(...)` | 修改世界与 UI 偏移 |
| `SetViewMetrics(...)` | 修改气泡尺寸与 Pivot |
| `SetSortingPriority(...)` | 修改业务排序优先级 |
| `SetViewLifetimePolicy(...)` | 修改 View 回收策略 |
| `SetOcclusionPolicy(...)` | 修改遮挡策略 |
| `IsEntryValid(...)` | 校验 Engine 句柄 |
| `IsContextValid(...)` | 校验 Context 句柄 |
| `TryGetEntryDiagnostics(...)` | 读取单个 Entry 状态 |
| `Dispose()` | 释放 Context、订阅和 ViewHost |

#### EmberSceneUIModuleBase&lt;TModule&gt;

| 方法或事件 | 说明 |
|---|---|
| `ChannelReady` | 宿主页和资源准备完成，Channel 可注册 |
| `ChannelUnavailable` | Channel 创建失败并提供原因 |
| `GetChannelState(...)` | 读取 Channel 状态 |
| `TryRegisterSceneUI(...)` | 向 Ready Channel 注册业务请求 |
| `UnregisterSceneUI(...)` | 注销业务句柄 |
| `IsSceneUIValid(...)` | 校验业务句柄 |
| `MarkSceneUIPositionDirty(...)` | 通知位置已变化 |
| `RefreshSceneUIPosition(...)` | 应用偏移并请求刷新所在 Channel |
| `MarkSceneUIContentDirty(...)` | 请求 Binder 刷新内容 |
| `SetSceneUIVisible(...)` | 修改单个气泡显隐 |
| `SetChannelVisible(...)` | 修改整个 Channel 显隐 |
| `NotifyCameraUpdated(...)` | 通知目标相机已完成最终姿态更新 |
| `UnregisterChannel(...)` | 释放 Channel 并关闭其宿主页 |
| `RegisterChannel(...)` | 子类注册自定义更新源 Channel |
| `RegisterCinemachineChannel(...)` | 子类注册 Cinemachine Channel |

## 6. 主流程

### 6.1 Channel 初始化

```text
Ember Module 进入目标 Phase
  → EmberSceneUIModuleBase 创建 EmberSceneUIEngine
  → 业务 RegisterSceneUIChannels()
  → EUIManager 打开 Overlay 或 FreePage 宿主页
  → SceneUIPageHost 解析 Canvas 与 BubbleRoot
  → ResourceSceneUIViewHost 异步准备 Catalog
  → Engine 注册 Context
  → 触发 ChannelReady
```

### 6.2 每帧空间刷新

```text
指定相机完成最终更新
  → ISceneUICameraUpdateSource 发出通知
  → Engine 获取相机与 Canvas 快照
  → 检测相机、可见区域和锚点变化
  → 世界坐标投影到屏幕坐标
  → 屏幕坐标映射为 BubbleRoot anchoredPosition
  → 应用边界、缩放、排序与遮挡策略
  → 显示、隐藏或回收 EUI Item
```

`PollPosition` Entry 会在 Channel 刷新时读取锚点。`Manual` Entry 不轮询物体位置，但相机快照变化时仍会重新投影。

### 6.3 View 生命周期

```text
Entry 首次可渲染
  → ViewHost.TryAcquire
  → EUI Item BeginUse
  → Binder.Bind
  → 应用空间状态
  → Binder.RefreshContent
  → 显示

Entry 不可见或注销
  → 按 LifetimePolicy 保持、立即回收或延迟回收
  → Binder.Unbind
  → ViewHost.Release
```

资源加载只发生在 Channel 准备阶段。`TryAcquire` 不负责运行时异步加载。

## 7. 修改影响范围

| 修改点 | 可能影响 |
|---|---|
| `SceneUIContextDescriptor` | Context 校验、相机快照、宿主页和全部 Entry |
| 投影或 Canvas 映射 | 所有 Channel 的位置、边界和多分辨率行为 |
| `SceneUIVisibilityPolicy` | 屏幕外隐藏、钳制和气泡矩形判定 |
| `SceneUIScalePolicy` | 所有距离缩放行为 |
| View 生命周期策略 | 活跃实例数、池命中率和 GC 峰值 |
| 排序算法 | 同屏重叠关系和 sibling 顺序 |
| 遮挡预算 | 每帧物理查询量和遮挡响应延迟 |
| `ISceneUIView` / `ISceneUIViewHost` | EUI 适配、对象池和 Binder 生命周期 |
| Module 生命周期 | 宿主页关闭、句柄失效和热重启清理 |
| Catalog 格式 | 资源准备、预热和池容量 |
| 相机更新源 | 投影刷新时机及相机移动跟随效果 |

修改核心算法后，至少应覆盖 EditMode Core 测试、Integration 测试以及现有 PlayMode 测试。

## 8. 约束与已知陷阱

| 约束 | 说明 |
|---|---|
| 必须显式提供 SceneCamera | 禁止以 `Camera.main` 或场景搜索作为隐式兜底 |
| 必须提供相机更新源 | Engine 不自行决定相机何时完成更新 |
| 宿主页只能是 `Overlay` 或 `FreePage` | Item 属于 `EUIBindingRole`，`PageType` 中没有 Item |
| 所有气泡必须位于 `BubbleRoot` | 框架不会创建独立 SceneUI Canvas |
| 气泡必须是 EUI Item | 不使用 `SceneUIViewBehaviour` |
| 禁止嵌套 `overrideSorting` Canvas | 避免气泡脱离宿主页排序体系 |
| 句柄必须按版本校验 | 注销、模块重启或 Channel 重建后旧句柄安全失效 |
| 一个 Context 每帧最多 Flush 一次 | 同帧多次通知会合并；位置变化必须在最终刷新前提交 |
| `FullRect` 需要有效 ViewMetrics | 可由 Request 提供，也可由 ViewHost 从预制体取得 |
| 遮挡测试需要预算和 Tester | 仅开启 Policy 不会自动产生无限制物理查询 |
| Engine 必须释放 | 推荐交由 `EmberSceneUIModuleBase` 管理 |
| Binder 应区分有状态与无状态 | 无状态 Binder 可以共享；持有单个对象数据的 Binder 必须按注册实例创建 |
| 资源型 ViewHost 必须先 Prepare | 资源未准备完成时 Channel 不应进入 Ready |
