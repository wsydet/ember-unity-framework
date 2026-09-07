# Resource — 资源管理

资源入口是 `EmberResourceManager`，由 `IResourceProvider` 隔离后端。源码位于
[Resource/Runtime](../../Resource/Runtime)，默认实现为 `ResourcesProvider`。

## API

| 方法 | 行为 |
|---|---|
| `Initialize(provider, onComplete = null)` | 初始化后端，成功后注册服务并广播 ResourceReady |
| `LoadAssetAsync<T>(path, onComplete)` | 回调式资源加载，未就绪返回 null |
| `LoadAssetHandle<T>(path)` | 返回 `EmberAssetHandle<T>`，可查询状态、取消、释放 |
| `LoadFileAsync(path)` | 返回 `EmberFileHandle`，读取 bytes/text/filePath |
| `LoadFileSync(path)` | 同步读取小文件字节 |
| `LoadSceneAsync(sceneName, mode = Additive)` | 返回 `AsyncOperation`，Scene 模块负责激活时序和完成通知 |
| `UnloadAsset(path)` / `UnloadUnusedAssets()` | 委托后端释放资源 |

`IResourceProvider` 提供同名加载/释放方法和 `Progress`，后端初始化使用 `Initialize(Action<bool>)`。
该接口没有旧版的 `LoadSceneAsync(string, Action)` 签名。

## Handle 与加载槽

`EmberAssetHandle<T>` 提供 `Asset`、`IsDone`、`Succeeded`、`Error`、`Completed`、`Cancel` 和 `Dispose`。
`EmberFileHandle` 提供同类状态，以及 `GetBytes()`、`GetText()`、`GetFilePath()`。
后端可能同步完成，应先检查 `IsDone`，否则再订阅 `Completed`。

```csharp
var handle = EmberResourceManager.Instance.LoadAssetHandle<UnityEngine.Sprite>("UI/Icons/coin");
if (handle.IsDone)
{
    if (handle.Succeeded) { /* 使用 handle.Asset */ }
}
else
{
    handle.Completed += h => { if (h.Succeeded) { /* 使用 h.Asset */ } };
}
// 在拥有该请求的对象结束使用时 handle.Dispose()。
```

`EmberAssetHandleSlot<T>` 持有当前资源和加载中的请求：`LoadAsync(path, onLoaded, reapplyIfCurrent = true)`
可复用相同请求，替换请求时取消旧加载；`CancelLoading()` 只取消待完成请求，`Dispose()` 清理整个槽。
适合头像等频繁换资源的 UI，避免旧回调覆盖新结果。它不是全局引用计数缓存。

## 默认后端的实际边界

- 资源使用相对于任意 `Resources` 目录的路径，不带扩展名。
- `LoadAssetAsync` 和资产 Handle 内部仍调用同步 `Resources.Load<T>`；方法名不代表后台异步。
- 文件加载以 `TextAsset` 获取字节；没有真实磁盘文件路径时 `GetFilePath()` 可以为空。
- `UnloadAsset(path)` 当前为空操作；`UnloadUnusedAssets()` 调用 Unity 释放未使用资源。
- 默认场景加载使用 Unity `SceneManager.LoadSceneAsync` 并返回其操作。
- Addressables/YooAsset Provider、下载/热更新与真正异步化尚待实现。

Manager 在 Init 阶段建立默认后端。自定义后端要遵守一次初始化规则，不应在默认后端已经初始化后无条件重复替换。
完整生命周期见 [Manager 文档](../../Core/Runtime/Manager/README.md)；场景激活见 [Scene](../scene/README.md)。
