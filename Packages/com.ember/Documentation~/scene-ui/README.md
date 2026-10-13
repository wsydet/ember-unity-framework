# SceneUI 通用接入

SceneUI 把世界空间锚点投影到业务 EUI 页面的 BubbleRoot。框架提供 Engine、ModuleBase、Host、Catalog、池化和相机更新源，不提供当前游戏的 World Channel、页面编号或气泡语义。完整接口见 [SceneUI API](../../SceneUI/README.md)。

`SceneUIModule`、`SceneUIObject`、Example 气泡与 20000 排序等具体接线属于 source3d-2p5d 示例；base、platformer-2d 或仅安装包的项目不能假定这些对象存在。示例说明随对应模板放在 `Assets/Game/Documentation/source3d-2p5d/scene-ui-business.md`。框架仓库可只读查看[模板携带的说明](../../Templates~/source3d-2p5d/Assets/Game/Documentation/source3d-2p5d/scene-ui-business.md)，修改时先加载正确模板，在 Assets 维护再正式保存，不能编辑快照。

## 模块与资源准备

1. 在项目业务目录创建具体 `EmberSceneUIModuleBase<TModule>` 子类，显式声明所需 Phase 和 Enabled。资源不齐时保持关闭，Engine 由模块 OnInit 创建、退出时释放；不增加全局 SceneUI Manager。
2. 通过 EUI 开发中心创建 Overlay 或 FreePage 宿主页和气泡 Item。宿主页放置 `SceneUIPageHost`，指定有效 BubbleRoot；所有气泡均在该节点下，不新增独立 Canvas 或 SceneUIViewBehaviour。
3. 由业务显式提供 SceneCamera、宿主页 PageDef、Catalog 与更新策略，在 `RegisterSceneUIChannels` 中登记。不同相机、区域或生命周期才拆 Channel；同 Channel 的外观类型由稳定 ViewKey 区分。
4. Catalog 的 Key、Prefab、预热数和最大池容量分别配置。气泡必须是有效 EUI Item，不能包含脱离宿主页排序体系的 overrideSorting Canvas；预热数量不等于池容量。
5. 为每种业务气泡建立 Binder 映射，填充内容并清理事件。仅增加枚举或 Catalog 不会自动产生 Binder。持有单个目标数据的 Binder 应按注册创建；无状态逻辑才可共享。

调用方通过 `EmberModuleCollector.TryGetInstance` 和 `TryGetModule` 查询已装配模块，不用 `.Instance` 试探可选业务。Module 被发现不等于 Channel 已 Ready；检查 `GetChannelState` 或监听 ChannelReady，`TryRegisterSceneUI` 返回 false 时不保留无效句柄。框架不排队保存 Loading 期间的注册请求，业务自己管理重试和取消。

## 刷新、显示与所有权

| 关注点 | 通用契约 |
| --- | --- |
| 静态目标 | Manual 不轮询位置；相机变化仍重新投影。业务移动目标后调用 MarkSceneUIPositionDirty 或 RefreshSceneUIPosition |
| 动态目标 | TransformSceneUIAnchor + PollPosition 在 Channel 刷新时读取位置 |
| Cinemachine | 更新源只接受 OutputCamera 与显式 SceneCamera 一致的 Brain 完成事件 |
| 其它相机 | 相机所有者在最终姿态确定后调用 NotifyCameraUpdated(channelKey)，不要多脚本无序重复通知 |
| 每帧刷新 | 同一 Context 每帧最多 Flush 一次，目标移动必须发生在最终刷新前 |
| 显隐 | SetSceneUIVisible / SetChannelVisible 修改业务显隐，不销毁 Gameplay 目标 |
| 边界与排序 | Clamp/Hide、Padding、FullRect、PriorityThenDepth 等由业务选择；FullRect 需有效 ViewMetrics，框架不决定宿主页排序 |
| 生命周期 | 模块级 Handle 包含模块代次、Channel 与 Engine版本；目标禁用、Entry注销、Channel销毁或模块重建后不得复用旧句柄 |
| 回收 | 不可见按策略保持或回池；Unbind 解除事件并清除内容，模块退出释放 Context、View、池与相机订阅 |

Item 首次可渲染时取得并 BeginUse，接着 Binder.Bind/RefreshContent 再显示；回池时 Unbind 后释放。隐藏不等于结束借用，真正回池才执行 Item 的关闭与重置。资源型 ViewHost 在 Channel 准备阶段加载，TryAcquire 不承担异步资源下载。

## 新增气泡与排查

新增类型必须同时完成稳定业务编号、EUI Item、Catalog Entry、Binder 与业务注册入口；已发布编号不随意重排或复用。有独立文本/事件参数时增加语义明确的业务方法，不能将目标数据写进共享单例 Binder。

不显示时依次检查模块启用与活动 Phase、Channel Ready、页面成功打开、BubbleRoot、显式相机、Catalog加载与Key、Item Binding/Logic、Binder、目标位置。层级不对检查 BubbleRoot；静态对象移动未刷新检查通知；相机错帧检查 OutputCamera和通知时机；Pool Miss 持续增长检查容量、回收延迟、频繁跨界与注销遗漏。运行诊断入口为 `Ember/Diagnostics/Scene UI`。

接入验收应覆盖真实页面层级、相机平移/旋转/缩放与混合、静态手动刷新、动态跟随、显隐/完整矩形边界、预热与池复用、目标禁用以及 Gameplay 退出重入后的旧句柄失效。预期数值由业务配置决定，不把 2.5D 示例的 Padding=24、回收延迟0.25秒或排序20000当成所有模板的硬约束。本文是操作与验收要求，不表示本轮执行过这些运行测试。
