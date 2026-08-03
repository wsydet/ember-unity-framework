# Resource — 资源管理

## 概述

Resource 模块是框架的资源加载统一入口。通过 `IResourceProvider` 接口隔离具体资源后端，
`EmberResourceManager` 作为门面委托所有操作。内置 `ResourcesProvider` 用于原型开发。

## 文件清单

| 角色 | 路径 |
|------|------|
| 核心接口 | `Runtime/IResourceProvider.cs` |
| 主逻辑入口 | `Runtime/EmberResourceManager.cs` |
| 内置实现 | `Runtime/ResourcesProvider.cs` |

## 依赖

| 依赖 | 类型 | 说明 |
|------|------|------|
| `Ember.Core` | 框架模块 | EmberSingleton、IEmberManager、EmberEventBus、EmberBroadcastEvent、EmberDebug |

## 公开 API

### EmberResourceManager — 资源管理器

继承 EmberSingleton，实现 IEmberManager。[EmberInitOrder(Resource)]。

| 方法 | 说明 |
|------|------|
| `Initialize(IResourceProvider, Action<bool> onComplete)` | 初始化，传入后端实现。完成后派发 ResourceReady |
| `LoadAssetAsync<T>(string path, Action<T> onComplete)` | 异步加载资源，未初始化时回调 null |
| `LoadSceneAsync(string sceneName, Action onComplete)` | 异步加载场景 |
| `UnloadAsset(string path)` | 释放指定资源引用 |
| `UnloadUnusedAssets()` | 释放所有未使用资源 |

### IResourceProvider — 资源后端接口

| 方法 | 说明 |
|------|------|
| `Initialize(Action<bool> onComplete)` | 初始化后端 |
| `LoadAssetAsync<T>(string path, Action<T> onComplete)` | 异步加载资源 |
| `LoadSceneAsync(string sceneName, Action onComplete)` | 异步加载场景 |
| `UnloadAsset(string path)` | 释放资源引用 |
| `UnloadUnusedAssets()` | 释放未使用资源 |
| `Progress` (属性) | 当前加载进度 0.0～1.0 |

### ResourcesProvider — 内置 Resources 后端

基于 Unity Resources 目录。`Initialize` 立即回调 true。`LoadAssetAsync` 底层走 `Resources.Load<T>`（同步包装为异步回调）。`UnloadAsset` 为空操作。`Progress` 始终返回 1.0。

## 主流程

**初始化：** `Initialize(provider, onComplete)` → 重复检查 → provider 判空 → `_provider.Initialize(callback)` → 成功标记 _initialized → `EmberEventBus.OnNext(ResourceReady)` → onComplete

**加载：** `LoadAssetAsync<T>(path, onComplete)` → `IsProviderReady(onComplete)` → 未就绪 LogWarning + 回调 null → 委托 `_provider.LoadAssetAsync`

**销毁：** `IEmberManager.Destroy()` → `EmberEventBus.OnNext(ResourceShutdown)` → `_provider.UnloadUnusedAssets()` → 清理

## 约束与陷阱

| 类别 | 说明 |
|------|------|
| 初始化顺序 | 必须在 Initialize 完成后才能调用加载方法，否则返回 null + LogWarning |
| ResourcesProvider | LoadAssetAsync 实际是同步的（Resources.Load 无异步版本）。UnloadAsset 是空操作（Resources API 限制） |
| 扩展后端 | 实现 IResourceProvider 即可替换后端（Addressables、YooAsset 等） |
