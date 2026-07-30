# 模块名称：资源管理（Resource）

---

## 1. 快速上手

```csharp
// 1. 启动时：传入资源后端并初始化
EmberResourceManager.Instance.Initialize(new ResourcesProvider(), success =>
{
    if (success) Debug.Log("资源系统就绪");
});

// 2. 运行时：异步加载资源
EmberResourceManager.Instance.LoadAssetAsync<Sprite>("ui/icons/coin", sprite =>
{
    image.sprite = sprite;
});

// 3. 场景切换后：释放未使用资源
EmberResourceManager.Instance.UnloadUnusedAssets();
```

---

## 2. 模块概述

Resource 模块是框架的资源加载统一入口。通过 `IResourceProvider` 接口隔离具体资源后端
（Resources / Addressables / YooAsset），上层业务代码只与 `EmberResourceManager` 交互，
不感知底层实现。已内置 `ResourcesProvider`（基于 Unity Resources 目录）用于原型开发和编辑器工具。

---

## 3. 依赖关系

| 依赖 | 类型 | 说明 |
|------|------|------|
| `Ember.Core` | 框架模块 | EmberMonoSingleton（单例基类）、EmberEventBus（派发 ResourceReady/ResourceShutdown）、EmberBroadcastEvent（事件 Key 常量） |
| `UnityEngine` | 引擎 | Object、Resources、SceneManager、MonoBehaviour 等 |
| `System` / `System.Collections` | 标准库 | Action、IEnumerator 等 |

---

## 4. 文件清单

| 角色 | 路径 |
|------|------|
| 核心接口 | `Assets/Ember/Resource/Runtime/IResourceProvider.cs` |
| 主逻辑入口 | `Assets/Ember/Resource/Runtime/EmberResourceManager.cs` |
| 内置实现（Resources 后端） | `Assets/Ember/Resource/Runtime/ResourcesProvider.cs` |
| 视图/表现层 | 无 |
| 编辑器扩展 | 无 |

---

## 5. 公开 API

### 5.1 入口类型

| 类型 | 职责 | 获取方式 |
|------|------|----------|
| `EmberResourceManager` | 资源加载统一入口，门面类 | `EmberResourceManager.Instance`（MonoSingleton） |
| `IResourceProvider` | 资源后端接口，隔离具体实现 | 由业务层实现，通过 `Initialize()` 注入 |
| `ResourcesProvider` | Unity Resources 目录的内置后端 | `new ResourcesProvider()` |

### 5.2 核心方法

#### EmberResourceManager — 资源管理器

| 方法签名 | 说明 |
|----------|------|
| `Initialize(IResourceProvider provider, Action<bool> onComplete)` | 初始化资源系统，传入后端实现。完成后派发 `ResourceReady` 事件 |
| `LoadAssetAsync<T>(string path, Action<T> onComplete)` | 异步加载资源，未初始化时回调 null |
| `LoadSceneAsync(string sceneName, Action onComplete)` | 异步加载场景 |
| `UnloadAsset(string path)` | 释放指定路径的资源引用 |
| `UnloadUnusedAssets()` | 释放所有未使用资源（场景切换后调用） |
| `IsInitialized` (属性) | 资源系统是否已完成初始化 |
| `Progress` (属性) | 当前加载进度（0.0 ~ 1.0），未设置 Provider 时返回 0 |

#### IResourceProvider — 资源后端接口

| 方法签名 | 说明 |
|----------|------|
| `Initialize(Action<bool> onComplete)` | 初始化后端（版本检查、资源下载等耗时操作） |
| `LoadAssetAsync<T>(string path, Action<T> onComplete)` | 异步加载资源，失败时回调 null |
| `LoadSceneAsync(string sceneName, Action onComplete)` | 异步加载场景 |
| `UnloadAsset(string path)` | 释放指定路径的资源引用 |
| `UnloadUnusedAssets()` | 释放所有未使用资源 |
| `Progress` (属性) | 当前加载/下载进度（0.0 ~ 1.0） |

#### ResourcesProvider — 内置 Resources 后端

| 方法签名 | 说明 |
|----------|------|
| `Initialize(Action<bool> onComplete)` | 无需初始化，立即回调 true |
| `LoadAssetAsync<T>(string path, Action<T> onComplete)` | 底层走 `Resources.Load<T>`（同步），包装为异步回调 |
| `LoadSceneAsync(string sceneName, Action onComplete)` | 走 `SceneManager.LoadSceneAsync` |
| `UnloadAsset(string path)` | **空操作**（Resources 不支持单个资源卸载） |
| `UnloadUnusedAssets()` | 调用 `Resources.UnloadUnusedAssets()` |
| `Progress` (属性) | 始终返回 1.0 |

---

## 6. 主流程

**流程一：初始化**
`[外部] Initialize(provider, onComplete)` → `[重复初始化检查]` → `[provider 判空]` → `_provider.Initialize(callback)` → `[success: 标记 _initialized]` → `EmberEventBus.Dispatch(ResourceReady)` → `onComplete?.Invoke(success)`

**流程二：资源加载**
`[外部] LoadAssetAsync<T>(path, onComplete)` → `IsProviderReady(onComplete)` → `[未就绪: LogWarning + 回调 null]` → `_provider.LoadAssetAsync<T>(path, onComplete)` → `[回调]`

**流程三：销毁清理**
`OnSingletonDestroy()` → `EmberEventBus.Dispatch(ResourceShutdown)` → `_provider.UnloadUnusedAssets()` → `_provider = null` → `_initialized = false`

---

## 7. 修改影响范围

- **替换资源后端（如从 Resources 切换到 Addressables）** → 新增实现 `IResourceProvider` 的类，启动时替换 `Initialize()` 的传入参数
- **新增资源加载变体（同步加载、批量加载）** → 在 `IResourceProvider` 添加方法签名，同步更新 `EmberResourceManager` 和所有 Provider 实现
- **调整初始化流程（如增加预加载步骤）** → 改 `EmberResourceManager.Initialize()`
- **新增资源事件（如单个资源加载完成事件）** → 在 `EmberBroadcastEvent` 添加事件 Key，在 `EmberResourceManager` 中派发
- **调整 ResourcesProvider 行为** → 改 `ResourcesProvider` 对应方法（注意 `UnloadAsset` 目前是空操作）

---

## 8. 约束与已知陷阱

| 类别 | 说明 |
|------|------|
| 初始化顺序 | 必须在 `EmberResourceManager.Instance.Initialize()` 完成后才能调用加载方法，否则返回 null + LogWarning |
| 生命周期 | `EmberResourceManager` 继承 `EmberMonoSingleton`，挂载 `DontDestroyOnLoad`，场景切换不销毁。销毁时自动派发 `ResourceShutdown` 并清理 Provider |
| 数据边界 | `ResourcesProvider.LoadAssetAsync` 底层走同步 `Resources.Load`，资源路径相对于 `Assets/Resources/` 目录，不包含扩展名。路径为空时回调 null |
| 线程安全 | 所有操作仅限主线程使用。`ResourcesProvider.LoadSceneAsync` 内部使用 `SceneManager.LoadSceneAsync` 的 `completed` 事件（主线程回调） |
| 已知问题 | `ResourcesProvider` 中 `_coroutineRunner` 字段已声明但**从未使用**（死代码）。`ResourcesProvider.UnloadAsset()` 为空操作（Resources API 限制），实际释放需依赖 `UnloadUnusedAssets()`。`ResourcesProvider.LoadAssetAsync` 方法名包含 "Async" 但实际是**同步加载**（`Resources.Load` 无异步版本），仅接口签名保持异步风格 |
