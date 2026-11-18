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

`EmberAssetHandle<T>` 提供 Asset/IsDone/Succeeded/Error/IsCancelled/IsDisposed、Completed、Cancel 和 Dispose。所有请求与 Provider 通知在 Unity 主线程调用。Completed 成功传 T，失败/取消传 null；若已完成，订阅时立即通知，Dispose 后新增订阅忽略。单个回调异常被隔离并继续其他订阅。

```csharp
var handle = EmberResourceManager.Instance.LoadAssetHandle<UnityEngine.Sprite>("UI/Icons/coin");
handle.Completed += sprite => { if (sprite != null) { /* 显示 sprite */ } };
// 先卸下 Graphic 等对象上的拥有引用，再 handle.Dispose()。
```

Cancel 先进入 IsDone/IsCancelled 终态，再调用后端取消 hook 和一次 Completed(null)，不等于物理中断或释放。仍须 Dispose。Dispose 幂等，pending 先取消，成功/失败/取消均释放该请求 token 一次；成功 Dispose 不重复完成通知，清 Asset 且 Succeeded=false。释放捕获原 Provider 的 token，不通过 Manager 当前后端按路径卸载。

`EmberAssetHandleSlot<T>` 持有当前成功资源和 pending 请求：LoadAsync(path, onLoaded, reapplyIfCurrent=true) 按 Ordinal 精确地址去重，不 Trim/变更大小写。同成功地址同步回调；同 pending 地址替换回调，旧回调不再通知。不同地址失效旧代次并取消/Dispose pending。失败保留旧成功资源、LastError 给原因，失败地址不缓存，可重新请求。同步完成也不会漏接。reapplyIfCurrent=false 保留旧语义：回调 null。

CurrentAsset/CurrentAssetPath/LoadingAssetPath/TargetAssetPath/IsLoading 为查询入口。CancelLoading 只退休 pending；Clear 清空并可复用；Dispose 永久封闭。切换成功先提交新 current，再回调切换显示，finally 释放旧 handle；回调允许重入/抛错。Clear/Dispose 前必须先卸下显示引用。Slot 不是全局引用计数缓存。UI 每次请求结果、显示策略及池化见 [运行时扩展](../ui/runtime-extensions.md)。

### 外部 Provider 的每请求凭证

保留 IResourceProvider 原签名，用公开工厂和终结入口实现：

```csharp
// token 是本次后端操作的独立引用；示意接口由后端实现。
var handle = EmberAssetHandle<Sprite>.Create(path,
    cancel: token.Cancel,
    release: token.Release);
token.OnCompleted(sprite =>
{
    if (!handle.TryComplete(sprite, sprite != null, token.Error))
        token.ReleaseLateResult(sprite);
});
return handle;
```

Create 返回 pending，TryComplete 只接受一次 pending→terminal，重复/迟到返回 false。拒绝不接受资源所有权，Provider 仍负责自己的后端 token；无法物理取消时，已 release token 后迟到取得的引用也必须由 Provider 清理。cancel/release 独立且幂等，每个同路径请求都要拥有自己的凭证，不能双重释放同一个未增引用 token。不要按共享 asset 实例 Destroy。此合同不改变 EmberFileHandle 或场景加载，也未交付 Addressables/YooAsset 后端。

`EmberFileHandle` 保持原有状态和 GetBytes/GetText/GetFilePath，事件参数为文件句柄，勿与资产事件的 T 参数混淆。

## 默认后端的实际边界

- 资源使用相对于任意 `Resources` 目录的路径，不带扩展名。
- `LoadAssetAsync` 和资产 Handle 内部仍调用同步 `Resources.Load<T>`；方法名不代表后台异步。
- 文件加载以 `TextAsset` 获取字节；没有真实磁盘文件路径时 `GetFilePath()` 可以为空。
- `UnloadAsset(path)` 当前为空操作；`UnloadUnusedAssets()` 调用 Unity 释放未使用资源。
- Resources 的资产 Handle Dispose 仅放下强引用，不 Destroy/UnloadAsset 共享 Sprite/Texture，不提供逐请求物理卸载。
- 默认场景加载使用 Unity `SceneManager.LoadSceneAsync` 并返回其操作。
- Addressables/YooAsset Provider、下载/热更新与真正异步化尚待实现。

Manager 在 Init 阶段建立默认后端。自定义后端要遵守一次初始化规则，不应在默认后端已经初始化后无条件重复替换。
完整生命周期见 [Manager 文档](../../Core/Runtime/Manager/README.md)；场景激活见 [Scene](../scene/README.md)。
