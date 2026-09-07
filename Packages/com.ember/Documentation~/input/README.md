# Input — 输入管理

## 概述

Unity Input System 的框架封装。持有 InputActionAsset，支持运行时切换 Action Map，
提供 GetAxis/GetFloat/IsPressed 便捷读取方法。

## 文件清单

| 角色 | 路径 |
|------|------|
| 主逻辑入口 | [EmberInputManager.cs](../../Input/Runtime/EmberInputManager.cs) |
| 重绑定扩展契约 | [InputRebindingContracts.cs](../../Input/Runtime/InputRebindingContracts.cs) |

## 依赖

| 依赖 | 类型 | 说明 |
|------|------|------|
| `Ember.Core` | 框架模块 | EmberSingleton、IEmberManager、EmberEventBus、GameLauncher（日志 EmberDebug 来自 Basic） |
| `UnityEngine.InputSystem` | 引擎 | InputActionAsset、PlayerInput、InputAction |

## 公开 API

### EmberInputManager — 输入管理器

继承 EmberSingleton，实现 IEmberManager。[EmberInitOrder(Input)]。

| 方法 | 说明 |
|------|------|
| `Init(InputActionAsset actionAsset, string defaultMap)` | 初始化，传入 InputActionAsset。自动添加 PlayerInput 组件 |
| `SwitchMap(string mapName)` | 切换到指定 Action Map。先禁用当前 Map，再启用目标 |
| `GetAxis(string actionName) → Vector2` | 读取 Vector2 输入值（Move、Look 等） |
| `GetFloat(string actionName) → float` | 读取 float 输入值 |
| `IsPressed(string actionName) → bool` | 按钮是否本帧触发 |
| `GetAction(string actionName) → InputAction` | 获取 InputAction 引用，用于手动订阅 performed/canceled |
| `GetAction(string mapName, string actionName) → InputAction` | 从指定 Map 获取 Action，避免同名歧义 |
| `SetRebindingService(IEmberInputRebindingService)` | 注册未来的玩家输入重绑定实现；传 null 可移除 |
| `CurrentMap` (属性) | 当前激活的 Action Map 名称 |
| `ActionAsset` / `IsInitialized` (属性) | 向扩展服务暴露当前输入资源和初始化状态 |
| `RebindingService` (属性) | 当前注册的重绑定服务；默认 null |

### 玩家输入重绑定扩展点

当前包只定义重绑定契约，不包含交互式监听、Binding Override 修改或持久化实现。

| 类型 | 说明 |
|------|------|
| `EmberInputBindingTarget` | 用 Map、Action、稳定 BindingId 和可选 BindingIndex 定位待修改项，也能表示 Composite Part |
| `EmberInputRebindRequest` | 重绑定请求，预留取消按键和候选输入排除规则 |
| `EmberInputRebindResult` | 完成、取消或失败结果 |
| `IEmberInputRebindingService` | 开始/取消、显示键位、恢复默认、导入/导出 Overrides 的服务契约 |

未来实现服务后，通过 `EmberInputManager.Instance.SetRebindingService(service)` 注册。
Binding Overrides 的存储位置由业务层决定，服务只负责 JSON 导入/导出，避免输入模块绑定 PlayerPrefs、云存档等具体后端。

## 输入提供端与消费端

`EmberInputManager` 是输入提供端，不把 Move、Look 等动作自动转换为具体玩法行为。
框架包无法预知角色、相机或棋盘应该如何响应输入，因此消费端必须由业务模块实现。

`source3d-2p5d` 模板中的 `PlayerControlModule` 是一个业务消费端示例：它通过
`GetAction(mapName, actionName)` 读取 Player Map，将 Move、PointerPosition、CameraDrag
和 Zoom 转换为 LookAt 移动、世界锚点拖动和正交相机视野大小调整。
该模块不属于 Input Package，避免框架输入层反向依赖业务场景和相机配置。

## 模板边界

`EmberInputManager` 不是可选的模板玩法模块，而是实现 `IEmberManager` 的框架必要组件。
`base` 与其他模板都通过 `Packages/com.ember/Input` 具备它，并由 Manager 反射管道发现和初始化。
公共 Package 保持单一来源，因此切换或保存模板时不应把它的实现复制到模板 `Assets`；这不代表
`base` 可以缺少该 Manager。

各模板只负责自己的 InputActionAsset、默认绑定、配置引用和可选业务消费模块。`source3d-2p5d`
中的 `PlayerControlModule` 属于可选业务 Module，只应随该模板保存，`base` 不包含。业务
InputActionAsset 必须位于模板系统覆盖的目录内，例如 2.5D 模板使用
`Assets/Game/InputSystem_Actions.inputactions`。从 `source3d-2p5d` 切回 `base` 后，应确认
`EmberInputManager` 仍由框架正常初始化，并清理 2.5D PlayerControl 业务内容与引用。

Project-wide Default Input Actions 保存在 `ProjectSettings/EditorBuildSettings.asset`，不随业务模板
Assets 快照切换。加载一个不包含原输入资产的模板后，需要在 Project Settings 中清空失效引用或重新
指定该模板自己的 InputActionAsset。FrameworkScene 的 `InputSystemUIInputModule` 默认引用 Input
System 包内置 `DefaultInputActions`，与业务 Manager 使用的 InputActionAsset 相互独立。

## 主流程

**框架启动：** Manager 反射发现 `EmberInputManager` → `IEmberManager.Init()` 校验
`GameLauncher.InputHost`。该阶段只完成框架级准备，不猜测业务 InputActionAsset。

**业务绑定：** `Init(asset, defaultMap)` → 获取/添加 PlayerInput → 设置 actions →
切换 defaultMap → `EmberEventBus.OnNext(InputReady)`。

**切换 Map：** `SwitchMap(mapName)` → 禁用当前 Map → 启用目标 Map

**销毁：** `IEmberManager.Destroy()` → `EmberEventBus.OnNext(InputShutdown)` → Disable actions → Destroy PlayerInput → 重置

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| 依赖 GameLauncher | 需要 GameBoot 下存在 InputHost 子节点 |
| 初始化 | IEmberManager.Init 仅做最小准备。完整初始化需调用 Init(asset) |
| PlayerInput | 自动添加 PlayerInput 组件，notificationBehavior 设为 InvokeUnityEvents |
| 重绑定 | 仅预留接口，不提供默认 `IEmberInputRebindingService` 实现 |
| 消费行为 | Manager 只提供输入，不自动移动角色或相机；由业务模块消费 |
