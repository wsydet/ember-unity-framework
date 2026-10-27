# 动态 Item 列表

`Ember.UIExtension.EUIDynamicListHost` 保存真实 ScrollRect、Item Prefab、缓存容量和数据绑定语义。父页面将宿主绑定为 `Extension / Ember.UIExtension.EUIDynamicListHost`，不把动态模板登记为固定 UILogic。Content 初始必须为空，单一宿主只接受一个列表所有者。

```csharp
// 页面 OnOpen 建立，在 OnClose/OnDispose 释放。
rows = host.Create<RowData>(data => data.Id, lease =>
{
    var view = (MyRowItem)lease.Item.Logic;
    view.Bind(lease); // BeginUse 已完成，在此写数据；容器随后 Show。
});
rows.Replace(data); // 0项、扩充、缩减、重排。
rows.Clear();
rows.Dispose();
```

`EUIItemList<T>` 提供 Count/CachedCount/InstanceCount/CreatedCount/DestroyedCount/LogicCreatedCount/RebindCount。索引器返回当前 `EUIItemLease<T>`，含 Data/Key/Index/Generation/Cancellation/IsCurrent。所有操作在 Unity 主线程执行；异步工作结束后先回主线程，再通过 TryApply 写结果。每个借用都是独立对象，旧借用永不恢复有效。

```csharp
UnityAction handler = () => lease.TryApply(current => OnClick(current.Data, current.Index));
button.onClick.AddListener(handler);
lease.Track(() => button.onClick.RemoveListener(handler));
// 取消只是合作机制；即便请求未遵守取消，迟到结果也必须检查借用。
lease.TryApply(current => image.sprite = loadedSprite);
// 独立资源请求若返回 IDisposable，可登记 lease.Track(handle.Dispose)。
```

Track 保存本次使用的清理操作；结束时先失效，再取消令牌、逆序清理。某项清理抛错时继续其他项并记录错误。绑定回调抛错时清空本批全部借用，留下可再次使用的空列表。更新不能重入 Replace/Clear；Dispose 可从取消回调进入，释放幂等。

| 对象 | 唯一所有者及结束时机 |
| --- | --- |
| 列表数据与业务规则 | 页面/业务模块；容器仅保存本次借用引用 |
| Prefab、图片及其加载句柄 | 外部资源提供者；本实现借用序列化 Prefab 引用，外部须持有资源至列表 Dispose 后 |
| 实例 GameObject | 列表，回收后进入有界缓存；最终 Dispose Item 再 Destroy |
| EUIItem 与固定子逻辑 | 每实例仅 EUIItemFactory 创建一次，列表持有 |
| 数据回调、订阅、取消源 | 当前 EUIItemLease；结束后清理并失效 |
| 宿主组件 | 页面实例；OnDestroy 兜底 Dispose 当前列表 |

调用顺序：取得实例和逻辑 → BeginUse → 写数据显示及事件接线 → Show。临时 Hide/Show 保留借用，不代表取消；事件适配器应检查可见性与 interactable。回收：借用 Dispose → ResetForPool → 缓存或 Item.Dispose → Destroy。退出时同时释放活动与缓存实例。借用已结束时 Track 立即执行清理。

key 使用业务提供的非空字符串，按 Ordinal 区分大小写，整批预检重复/空 key 后才改变原列表；失败保留旧数据显示。index 仅是显示顺序，每次重排创建新借用，事件读取当前 Data/Index，不捕获历史 index。

刷新是 O(n) 整表重新绑定，清理旧周期后按缓存栈借用，不是按 key 增量差分。缓存容量只限制闲置实例，不截断活动数据；默认20，可设0。缓存保留 GameObject/逻辑，不保留事件或数据。本轮验证20行，不构成性能上限承诺；没有多模板、TabLoader、虚拟列表或页面预加载改造。

首版要求纵向 ScrollRect、Viewport 的 RectMask2D/Mask、Content 的 VerticalLayoutGroup 与单个纵向 PreferredSize fitter。Item 用 ILayoutElement 提供高度，父布局写尺寸；EUITextRowLayout 按 TMP 的实际宽度计算换行高度。诊断覆盖缺引用、错误层级、缺裁剪、横向滚动、双 fitter、Content 横向 fitter 循环风险及 Item 根 fitter 与父布局争用高度。S2 横向/网格宿主仍是静态结构，不属于本容器支持的布局。

契约 schemaVersion=1、toolVersion=1.1；动态资产声明 `requiredCapabilities: ["dynamic-list-v1"]`。lists 导出 kind=List/templateKind=PrefabRef、宿主身份、容器来源、Item GUID/路径/逻辑、Content/Viewport、数据接线、所有权、刷新和缓存策略。list-contract 差异阻断自动应用；消费者不理解 requiredCapabilities 时须拒绝动态资产。未知业务仍留在 unresolved。正式创建继续使用 EUICreationService 和绑定/生成服务。

base 示例说明在 `Assets/Game/Documentation/base/dynamic-lists.md`。相关测试为 EUIDynamicListEditTests 与 EUIContractEditTests，实际执行结果见工程阶段交接。
