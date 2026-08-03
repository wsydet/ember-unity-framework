# Input — 输入管理

## 概述

Unity Input System 的框架封装。持有 InputActionAsset，支持运行时切换 Action Map，
提供 GetAxis/GetFloat/IsPressed 便捷读取方法。

## 文件清单

| 角色 | 路径 |
|------|------|
| 主逻辑入口 | `Runtime/EmberInputManager.cs` |

## 依赖

| 依赖 | 类型 | 说明 |
|------|------|------|
| `Ember.Core` | 框架模块 | EmberSingleton、IEmberManager、EmberEventBus、EmberDebug、GameLauncher |
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
| `CurrentMap` (属性) | 当前激活的 Action Map 名称 |

## 主流程

**初始化：** `Init(asset, defaultMap)` → 获取 GameLauncher.Instance.InputHost → 获取/添加 PlayerInput → 设置 actions → 切换 defaultMap → `EmberEventBus.OnNext(InputReady)`

**切换 Map：** `SwitchMap(mapName)` → 禁用当前 Map → 启用目标 Map

**销毁：** `IEmberManager.Destroy()` → Dispatch(InputShutdown) → Disable actions → Destroy PlayerInput → 重置

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| 依赖 GameLauncher | 需要 GameBoot 下存在 InputHost 子节点 |
| 初始化 | IEmberManager.Init 仅做最小准备。完整初始化需调用 Init(asset) |
| PlayerInput | 自动添加 PlayerInput 组件，notificationBehavior 设为 InvokeUnityEvents |
