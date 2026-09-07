# SceneUI 使用参考

本文说明如何在业务场景中使用 SceneUI，以及如何新增气泡类型。

SceneUI 的目标是把世界空间目标显示为现有 EUI 页面中的气泡。框架不会为每个对象创建独立 Canvas，也不使用 `SceneUIViewBehaviour`。


## 示例的模块前置条件

以下业务片段中的 `sceneUI` 指已装配的模块。调用前先查询并检查 Channel 状态；退出阶段清理时重新查询，避免意外构造已禁用模块。

```csharp
if (!EmberModuleCollector.TryGetInstance(out EmberModuleCollector collector)
    || !collector.TryGetModule(out SceneUIModule sceneUI))
    return;
// Channel 就绪后使用下文 sceneUI；TryShow... 返回 false 时不保存无效句柄。
```


## 1. 架构边界

SceneUI 分为两部分。

### 1.1 框架层

目录：

```text
Packages/com.ember/SceneUI
```

框架层负责：

- 世界坐标投影
- Context 和 Entry 生命周期
- 显隐、边界、缩放和排序
- EUI Item 的创建、池化和回收
- 相机更新源
- 宿主页适配
- 资源准备
- 诊断数据

框架层不了解 `World`、`Example` 或任何项目气泡语义。

框架 API 详见：

```text
Packages/com.ember/SceneUI/README.md
```

### 1.2 业务层

目录：

```text
Assets/Game/Module/SceneUI
```

业务层负责：

- Channel 和 View 编号
- 显式 SceneCamera 来源
- 项目宿主页
- Catalog 资源路径
- 气泡 Binder
- 场景物体附加组件
- 静态或动态更新策略
- 项目默认显示策略

业务 API 详见：

```text
Assets/Game/Module/SceneUI/README.md
```

设计背景详见：

```text
docs/dev/scene-ui-module-design.md
```

## 2. 当前项目配置

当前项目已经提供最小 World Channel：

| 项目 | 配置 |
|---|---|
| Channel | `SceneUIChannelId.World = 1` |
| Example View | `SceneUIViewId.Example = 1001` |
| 宿主页 | `GamePages.EUISceneUIPage` |
| 页面类型 | `FreePage` |
| sortingOrder | `20000` |
| BubbleRoot | `EUISceneUIPanel/SceneUIPageHost.BubbleRoot` |
| SceneCamera | `EmberCameraManager.Instance.MainCamera` |
| Catalog 地址 | `UI/Module/SceneUI/SceneUIViewCatalog` |
| Example 预制体 | `EUIBaseBubbleItem` |
| 预热数量 | `4` |
| 最大池容量 | `16` |
| 相机驱动 | Cinemachine CameraUpdatedEvent |
| Context 排序 | `PriorityThenDepth` |

`SceneUIViewCatalog` 当前映射：

```text
ViewKey 1001
  → EUIBaseBubbleItem
  → PrewarmCount 4
  → MaxPooledCount 16
```

所有运行时气泡实例都必须是 `BubbleRoot` 的子节点。

## 3. 为场景物体显示气泡

场景物体由业务自行创建。SceneUI 不负责创建 Gameplay 对象。

选中需要显示气泡的物体，添加：

```text
Game.Module.SceneUIObject
```

在 Inspector 中配置：

| 字段 | 说明 |
|---|---|
| 气泡类型 | 选择 `SceneUIViewId` |
| 位置更新方式 | `Static` 或 `Dynamic` |
| 世界坐标偏移 | 相对于 `Transform.position` 的偏移 |
| 初始可见 | 注册完成后是否显示 |

默认配置：

```text
气泡类型：Example
位置更新方式：Static
世界坐标偏移：(0, 2, 0)
初始可见：true
```

进入 Play Mode 后：

1. `SceneUIObject` 在 `OnEnable` 中登记自身。
2. 如果 World Channel 已 Ready，立即注册。
3. 如果 Channel 仍在 Loading，等待 `ChannelReady`。
4. 气泡首次进入可渲染状态时，从池中取得 EUI Item。
5. 对象禁用时自动注销并释放气泡。

不需要在 Gameplay 场景中预先摆放气泡对象。

## 4. 选择 Static 或 Dynamic

### 4.1 Static

适用于运行时位置不变的对象，例如：

- 固定建筑
- 资源点
- 场景传送入口
- 固定交互标记

Static 不会在每次 Channel 刷新时轮询 Transform。

如果业务移动了静态对象，应调用：

```csharp
_sceneUIObject.NotifyPositionChanged();
```

也可以应用当前偏移并请求刷新：

```csharp
_sceneUIObject.RefreshPosition();
```

相机移动时，Static 气泡仍会重新投影；Static 只是不主动轮询物体 Transform。

### 4.2 Dynamic

适用于：

- 移动单位
- 玩家或 NPC
- 动态生成目标
- 跟随动画或物理移动的对象

Dynamic 会在 World Channel 刷新时检查 Transform：

```text
SceneUIObjectUpdateMode.Dynamic
  → SceneUIUpdatePolicy.PollPosition
```

`SceneUIAutoMover` 可用于 Play Mode 验证 Dynamic 跟随，但不是正式业务依赖。

## 5. 运行时控制

### 5.1 修改显隐

```csharp
_sceneUIObject.SetVisible(false);
_sceneUIObject.SetVisible(true);
```

这只修改业务显隐，不销毁场景物体。

### 5.2 配置改变后重新注册

如果运行时通过自定义业务逻辑或 Inspector 修改了气泡类型、更新方式等注册参数：

```csharp
_sceneUIObject.RefreshRegistration();
```

当前 `SceneUIObject` 的配置属性是只读公开属性。需要完全由代码动态配置时，建议直接调用模块 API，而不是通过反射修改序列化字段。

### 5.3 直接注册

```csharp
private SceneUIModuleHandle _sceneUIHandle;

private void ShowBubble()
{
    sceneUI.TryShowBubble(
        transform,
        SceneUIViewId.Example,
        SceneUIObjectUpdateMode.Dynamic,
        new Vector3(0f, 2f, 0f),
        true,
        out _sceneUIHandle);
}

private void HideBubble()
{
    sceneUI.UnregisterSceneUI(_sceneUIHandle);
    _sceneUIHandle = default;
}
```

直接调用只会在 World Channel Ready 后成功：

```csharp
SceneUIChannelState state =
    sceneUI.GetChannelState(SceneUIModule.WorldChannel);
```

对于需要跨 Channel Loading 自动恢复的场景对象，优先使用 `SceneUIObject`。

## 6. 新增业务气泡类型

以下步骤缺一不可。

### 6.1 定义稳定编号

在 `SceneUIModule.cs` 中增加业务枚举：

```csharp
public enum SceneUIViewId
{
    Example = 1001,
    CharacterName = 1002,
}
```

编号会进入 Catalog 和场景序列化数据。已发布编号不应随意修改或复用。

### 6.2 创建 EUI Item

通过项目 EUI 工作流创建气泡预制体，并确保：

- 角色是 Item，不是 Page
- 根节点包含有效 `RectTransform`
- 不添加独立 SceneUI Canvas
- 不添加 `SceneUIViewBehaviour`
- 不包含脱离宿主页排序体系的 `overrideSorting` Canvas
- EUI 逻辑可以通过 `EUIItemSceneUIView.GetLogic<T>()` 取得

### 6.3 加入 Catalog

打开：

```text
Assets/GameResource/Resources/UI/Module/SceneUI/SceneUIViewCatalog.asset
```

新增 Entry：

| 字段 | 建议 |
|---|---|
| Key | 与 `SceneUIViewId` 数值完全一致 |
| Prefab | 新建的 EUI Item 预制体 |
| Prewarm Count | 按首次同屏数量估算 |
| Max Pooled Count | 按常态峰值与内存成本估算 |

预热数量不能代替池容量。池容量控制隐藏或注销后最多保留多少实例。

### 6.4 实现 Binder

Binder 负责校验 View 类型、填充内容和清理复用状态。

当前 Example Binder 的核心形式：

```csharp
private sealed class ExampleBubbleBinder : ISceneUIBinder
{
    public void Bind(ISceneUIView view, SceneUIHandle handle)
    {
        ValidateView(view);
    }

    public void RefreshContent(ISceneUIView view)
    {
        ValidateView(view);
    }

    public void Unbind(ISceneUIView view)
    {
        // 清理事件订阅和不能带入下次复用的数据。
    }

    private static void ValidateView(ISceneUIView view)
    {
        if (!(view is EUIItemSceneUIView itemView)
            || itemView.GetLogic<EUIBaseBubbleItem>() == null)
        {
            throw new InvalidOperationException(
                "SceneUI View must match its EUI Item logic.");
        }
    }
}
```

Binder 生命周期：

```text
首次获得 View → Bind
内容标脏       → RefreshContent
View 被回收    → Unbind
```

注意：

- 无状态 Binder 可以作为静态单例共享。
- 如果 Binder 保存角色名称、血量或任务状态等单个对象数据，应为每次注册创建独立实例。
- `Unbind` 必须解除事件订阅，并清理可能污染下一次池复用的状态。

### 6.5 建立业务映射

在 `TryResolveView` 中补充 ViewKey 和 Binder：

```csharp
case SceneUIViewId.CharacterName:
    viewKey = new SceneUIViewKey((int)SceneUIViewId.CharacterName);
    binder = characterNameBinder;
    return true;
```

只添加枚举或 Catalog Entry 不会自动建立 Binder 语义。

### 6.6 暴露业务注册入口

简单类型可以继续复用：

```csharp
TryShowBubble(
    target,
    SceneUIViewId.CharacterName,
    updateMode,
    worldOffset,
    initiallyVisible,
    out handle);
```

如果新气泡需要文本、数据模型或事件回调，应新增语义明确的方法，例如：

```csharp
TryShowCharacterNameBubble(...)
```

由该方法创建对应 Binder，再构造 `SceneUIModuleRequest`。不要把具体业务数据塞进共享 Binder 单例。

## 7. 相机更新

World Channel 的 SceneCamera 来源是：

```csharp
EmberCameraManager.Instance.MainCamera
```

该相机由 `FrameworkScene.GameLauncher` 显式注入。SceneUI 不使用：

```csharp
Camera.main
FindObjectOfType<Camera>()
FindObjectsByType<Camera>()
```

当前 Channel 使用 `CinemachineSceneUICameraUpdateSource`，只响应输出到该 SceneCamera 的 Cinemachine Brain 完成事件。

如果使用非 Cinemachine 相机，或者最终相机姿态在其他系统中完成，应在最终更新后调用：

```csharp
sceneUI.NotifyWorldCameraUpdated();
```

不要在多个业务脚本中无序重复通知。同一 Context 每帧最多执行一次 Flush，推荐由相机所有者统一发出最终通知。

## 8. 边界、排序和回收

当前业务默认边界策略：

```text
OutOfBoundsMode = Clamp
ScreenPadding = 24
BoundsMode = FullRect
```

气泡会按完整矩形限制在屏幕可见区域内，而不是只限制 Pivot 点。

当前排序策略：

```text
PriorityThenDepth
```

先比较业务 `SortingPriority`，再根据深度调整同层气泡的 sibling 顺序。

当前回收策略：

```text
RecycleAfterDelay
Delay = 0.25 秒
```

短暂离屏不会立即释放实例；持续不可见后才返回对象池。

## 9. 生命周期与句柄

`SceneUIModuleHandle` 包含：

- 模块代次
- Channel Key
- Engine Handle

以下情况会使旧句柄失效：

- Entry 已注销
- `SceneUIObject` 被禁用
- Channel 被注销
- SceneUIModule 销毁或重新初始化
- Engine Entry 槽位被回收并增加 Version

调用业务 API 前可以校验：

```csharp
sceneUI.IsSceneUIValid(handle);
```

不要跨场景或跨模块生命周期永久缓存句柄。

## 10. 诊断与排查

打开运行时诊断窗口：

```text
Ember/Diagnostics/Scene UI
```

也可以从业务模块读取：

```csharp
if (sceneUI.TryGetDiagnostics(out SceneUIDiagnostics diagnostics))
{
    // 查看 Context、Entry、View、Pool、投影、裁剪和回收统计。
}
```

### 10.1 气泡没有出现

依次检查：

1. `SceneUIModule` 是否进入 Gameplay Phase。
2. World Channel 是否为 `Ready`。
3. `EUISceneUIPage` 是否成功打开。
4. `SceneUIPageHost.BubbleRoot` 是否有效。
5. `EmberCameraManager.Instance.MainCamera` 是否已注入。
6. Catalog 是否能从固定 Resources 地址加载。
7. Catalog Key 是否与 `SceneUIViewId` 一致。
8. 预制体是否为 EUI Item。
9. Binder 是否能取得正确的 EUI Item Logic。
10. 目标 Transform 是否有效且位于相机可投影范围。

### 10.2 气泡生成在错误层级

正确层级必须是：

```text
EUISceneUIPanel
  └─ BubbleRoot
      └─ EUI Item 气泡实例
```

不得生成独立 SceneUI Canvas，也不得把气泡挂到 Gameplay Transform 下。

### 10.3 静态物体移动后气泡不动

Static 不轮询 Transform。移动完成后调用：

```csharp
sceneUIObject.NotifyPositionChanged();
```

如果物体会持续移动，应改为 Dynamic。

### 10.4 相机移动后气泡不动

检查：

- Cinemachine Brain 的 `OutputCamera` 是否就是注册的 SceneCamera。
- 相机最终更新是否真的经过 Cinemachine。
- 非 Cinemachine 流程是否调用了 `NotifyWorldCameraUpdated()`。
- 是否在相机最终姿态确定之前过早发出了通知。

### 10.5 Pool Miss 持续增加

检查：

- `MaxPooledCount` 是否明显低于常态同屏峰值。
- 气泡是否频繁跨越可见边界。
- 回收延迟是否过短。
- 是否存在没有注销的业务对象。
- 预热数量是否覆盖常见首帧需求。

## 11. Play Mode 验收清单

### 基础显示

- 带 `SceneUIObject` 的场景物体能够显示 Example 气泡。
- 气泡实例位于宿主页 `BubbleRoot` 下。
- 气泡不包含独立 SceneUI Canvas。
- 页面排序保持 `sortingOrder = 20000`。

### 相机

- 平移、旋转和缩放相机时，气泡正确跟随目标。
- Cinemachine 切换或 Blend 时没有明显错帧。
- 相机更新不依赖 `Camera.main`。

### 物体更新

- Static 物体保持低轮询成本。
- Static 物体手动刷新后位置正确。
- Dynamic 物体连续移动时气泡跟随。
- 禁用场景物体后气泡注销。

### 显隐与边界

- `SetVisible(false)` 隐藏，`SetVisible(true)` 恢复。
- 目标移出屏幕时气泡按 24 像素 Padding 钳制。
- 完整气泡矩形不会越出可见区域。
- 目标重新进入屏幕后状态恢复正常。

### 生命周期与池

- 短暂不可见时遵循 0.25 秒延迟回收。
- 再次显示时能够命中对象池。
- 退出 Play Mode 后没有残留宿主页、订阅或运行时实例。
- 模块重启后旧句柄不能误操作新 Entry。

### 诊断

- Active Context 数量符合已注册 Channel。
- Active Entry 数量符合当前业务对象。
- Active View 与 Pooled View 变化合理。
- 没有持续增长的 ViewUnavailable、InvalidAnchor 或 PrewarmFailure。
