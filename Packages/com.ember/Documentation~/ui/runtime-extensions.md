# UI 运行时扩展

本页为 `Ember.UIExtension` 的滚动定位/吸附、TMP 跑马灯、地址图片加载及多图/圆角组合使用说明。它们是框架公共组件，不依赖具体模板。不需要 Burner、定制 TMP 或额外 Shader。资源合同以 [Resource](../resource/README.md) 为准。

进度展示另见 [缓冲进度条](buffered-progress-bar.md)：`EUIBufferedProgressBar` 以显式 Extension 组合 Slider/Filled Image，支持独立慢尾、白条 Shrink/Fade 及真实数值文字，保留旧 ProgressBar 自动映射。

## 正式页面绑定

在 EUI 开发中心维护的页面节点上添加相应 companion，然后在绑定列表选择 `Extension` 的具体类名，再通过中心重新生成 Binding：

| 节点原有组件 | 添加组件 | 生成字段类型 |
| --- | --- | --- |
| ScrollRect | EUIScrollController | Ember.UIExtension.EUIScrollController |
| TextMeshProUGUI / TMPEx | TMPMarquee | Ember.UIExtension.TMPMarquee |
| Image / EUIImageEx | EUIImageLoader | Ember.UIExtension.EUIImageLoader |
| RawImage | EUIRawImageLoader | Ember.UIExtension.EUIRawImageLoader |

四者使用 `EUIExtensionAttribute.AutoDetect=false`，仍出现在组件下拉框，自动收集保留原有 ScrollRect/Text/Image/RawImage 或旧扩展。既有绑定也可用 `GetComponent<T>()` 取得 companion。不要手改生成的 `.Binding.cs`。标记的 AutoDetect 是绑定选择策略，与项目中心的能力启用开关无关。

## 滚动定位和吸附

```csharp
scroll.ScrollToBegin();
scroll.ScrollToEnd(duration: 0.2f, completed: result => Handle(result.Status));
scroll.ScrollTo(row.RectTransform, EUIScrollAlignment.Center, new Vector2(0, 12), 0.2f);
scroll.ScrollByPixels(new Vector2(0, 80));
bool visible = scroll.IsVisible(row.RectTransform, fullyVisible: true);
scroll.SetSnapContainer(content, EUIScrollAxis.Vertical);
scroll.SnapCompleted += result => OnSnap(result.Target);
```

`Scroll` 返回原生 ScrollRect。组件不改 movementType/inertia/decelerationRate，不提供虚拟化。content/viewport 沿用 ScrollRect 常规同向、单位相对缩放的坐标布局；支持任意 pivot/anchor、嵌套 target 和 Canvas 缩放，不承诺相对旋转或斜切布局。

| API / 参数 | 坐标合同 |
| --- | --- |
| ScrollToNormalized(Vector2) | Unity 原生：x=0 左、1 右；y=1 顶、0 底。Begin=(0,1)，End=(1,0)，仅写开启的轴 |
| ScrollByPixels(Vector2) | UI 本地单位，正 x 向内容右端浏览，正 y 向底端浏览；不是设备物理像素 |
| ScrollTo / ScrollToItem 的 offset | viewport 本地坐标，+x 向右、+y 向上；目标对齐点落在 viewport 对齐点加 offset |
| Begin / Center / End 对齐 | 左上 / 中心 / 右下；以目标世界四角投影的实际边界计算 |
| 边界 | content 自身 rect 决定可滚区，边界 clamp 优先，不人为补 padding；小/空内容不除零 |
| IsVisible | 默认有面积相交；fullyVisible 要求全部投影 bounds 包含。零面积、非 content 后代返回 false；不计算遮挡或 alpha |

请求/`RefreshLayout()` 更新布局；动画随最新有效布局重算目标，使用时长明确的 SmoothStep。duration<=0 立即完成，默认 `UseUnscaledTime=true`，暂停 timeScale 不阻止定位。每次请求返回递增 operationId，立即请求可在返回前回调。结果含 OperationId/Target/NormalizedPosition/Status：Completed、Stopped、Replaced、UserInterrupted、Disabled、TargetLost、Invalid。`Stop()`、隐藏和替换都会退休旧请求，每请求最多一次终态；回调抛错被隔离，允许回调重入。

拖拽和 wheel 立即接管；Scrollbar/外部位置变化也会中断。组件记住自身写入后的实际位置，原生延后的 onValueChanged 不应打断自己。ScrollRect 原生事件继续处理，不吞输入。禁用组件/ScrollRect 不排队请求，目标销毁或移出 content 返回 TargetLost。

吸附显式选择 Horizontal/Vertical 单轴；`SetSnapTargets` 复制候选列表，`SetSnapContainer` 使用活跃直接子节点，过滤无效/非 content 后代。最近项按当前中心到 viewport 中心的轴距离选，等距保留候选顺序。`FocusedTarget`/`FocusChanged` 按对象身份变化，清空也通知 null。`SnapCompleted` 仅在本次吸附成功达到可达位置时通知。

松手后保留原生惯性；默认无输入/位移 0.12 秒、轴速度<=20 UI 单位/秒、弹性越界已基本恢复时，以 0.15 秒向最近中心吸附。首尾受 clamp 限制仍算到达。`SnapToNearest()` 可主动停止惯性并吸附；程序定位完成或 Stop 不自动追加吸附。参数可通过 SnapDuration/SettleDelay/VelocityThreshold 设置。

## TMP / TMPEx 跑马灯

节点配置：固定窗口宽度、单行、关闭 AutoSize、NoWrap、Overflow，并在祖先添加 Mask 或 RectMask2D。裁剪窗口应与文本可视窗口一致；可显式设置 `Viewport` 为祖先裁剪矩形。默认可视宽为文本 rect 减左右 margin，显式 Viewport 再与该宽度求交。

```csharp
marquee.SetMarqueeEnabled(true, speed: 30);
marquee.StartDelay = 0.5f;
marquee.EndDelay = 0.5f;
marquee.Speed = 0; // 暂停在当前位置
marquee.Speed = 45;
marquee.ResetPosition();
marquee.Refresh();
marquee.SetMarqueeEnabled(false); // 恢复作者原始对齐几何
```

短文本保留作者对齐。溢出时起点对齐窗口左边，开头停顿→向左到尾端→尾部停顿→向右回起点。速度为文本本地 UI 单位/秒；重复 SetMarqueeEnabled 也更新速度。默认非缩放时间，隐藏不累计时间，重新启用从最新文本开头播放。

只使用 TMP 公开 OnPreRenderText/meshInfo/UpdateVertexData；缓存原始顶点，各 materialIndex 每帧从基线位移，避免累计偏移。支持单行 rich text、中文、fallback 和 sprite 子网格。文本/宽度/margin/字体皮肤及 TMP 几何变化后重建基线。不移动 RectTransform、不写布局配置、不复制或改写共享字体材质/Shader/Atlas，原生 Mask 的 stencil 材质仍由 TMP/uGUI 管理。

配置不满足时保持静态，`Diagnostic` 与 Inspector 提示原因。多行、3D TMP、RTL/竖排，以及多个独立效果同时写顶点位置不在支持范围。使用 ContentSizeFitter 将窗口扩成全文宽度时本来没有溢出，不会强制滚动。

## Image / RawImage 地址加载

```csharp
loader.Placeholder = loadingSprite; // 借入
loader.FailureImage = failedSprite; // RawImage 为 FailureTexture
loader.LoadingDisplay = EUIImageLoadingDisplay.KeepPrevious;
loader.KeepPreviousOnFailure = true;
loader.KeepNativeSize = false;
long id = loader.Load("UI/Icons/coin", result => Handle(result.OperationId, result.Status, result.Error));
loader.CancelLoading();
loader.Clear();
loader.SetSprite(manualSprite); // RawImage 为 SetTexture
```

每次 Load 有独立递增 operationId，恰好一次终态：Success/Failed/Cancelled/Replaced/Cleared/ManualOverride/Disabled/Destroyed；结果包含原 Path 和 Error，不转移资源所有权。回调可同步发生，允许重入，异常不会阻止清理。`IsLoading`、`CurrentPath`、`LoadingPath` 为查询入口。

- 默认 pending 显示借入 Placeholder（未指定则 null），失败显示 FailureImage/FailureTexture，未指定回退 Placeholder；失败同地址可重试。原生空 Image 可能显示白块，需要透明占位时由作者提供透明图或样式。
- KeepPrevious 保留已成功资源直到替换/清空，KeepPreviousOnFailure 控制失败是否保留前图。CancelLoading 取消 pending 并恢复仍拥有的前图；不会自动重试。
- null/空地址等价清空，不调用后端，不 Trim/转小写。后端决定地址格式；Resources 路径不带扩展名。
- 同地址 pending/成功在 Slot 去重，但旧 UI 请求仍得到 Replaced。A→B→A、反序完成按请求代次和句柄身份处理。失败不缓存为成功地址。
- 成功先核对代次和手动接管，再赋图和可选 SetNativeSize，最后通知；换图后才释放旧 token。占位/失败/手动图都是借入，永不 Destroy/Unload。
- `SetSprite`/`SetTexture` 明确接管，即使写入相同值；调用方必须持有自己有效的资源所有权，不能把刚释放的 loader 资源当作独立借用。
- 直接改 Image.sprite/overrideSprite 或 RawImage.texture 的实际变化，经 Graphic dirty、完成前检查和 LateUpdate 识别，旧请求按 ManualOverride 退休，保留手动图。原生属性非 virtual；相同值写入或同帧改后恢复原值无法推断意图，确定接管使用显式 Set API。颜色/材质变化不算赋图。
- Image 加载拥有期间清 overrideSprite 再写 sprite。与 EUIImageEx 组合保留 SpriteArray/SpriteIndex；索引切换视为接管。Animated=true 时拒绝异步请求，先关闭帧动画。

## 隐藏、池化和销毁

首次 inactive 绑定允许 Load，不依赖 Awake/Start。GameObject 隐藏或 loader.enabled=false 会退休 pending 并卸下拥有的显示后释放；重新显示不自动重播旧路径，页面 OnShow 或新 Bind 要显式 Load。仅 CanvasGroup.alpha=0、Graphic.enabled=false 或渲染裁剪属于表现隐藏，保留资源，业务要结束使用时调用 Clear。

动态列表在 Bind 后才 Show，借用可能从未激活便回池，因此绑定时登记：

```csharp
var loader = lease.Item.GameObject.GetComponent<EUIImageLoader>();
lease.Track(loader.Clear);
loader.Load(lease.Data.IconPath);
// Hide 保留 lease；OnShow 如需图片，显式按仍有效的数据再次 Load。
```

`OnDisable` 不替代 lease.Track；隐藏不使 lease 失效。`Clear` 可复用，销毁组件时最终结束内部资源槽。never-active GameObject 或仅移除 loader 可能没有 OnDestroy，内部缓存原生 destroyCancellationToken，并仅在尚未 Awake 且持有 pending/current 时添加 UniTask PlayerLoop 存活观察；结束所有权或 Awake 后撤销观察。不新增 Manager，不扫描场景。

参见 [动态 Item 列表](dynamic-lists.md)；不要在 UI 关闭时全局 UnloadUnusedAssets。

## EUIImageEx 多图与独立圆角

Inspector 的精灵列表拖入多张 Sprite，关闭帧动画，SpriteIndex=0/1 切对应图片。有效项通过 overrideSprite 显示；空数组、空项或非法索引清覆盖并回退 base sprite，不改作者索引。换数组、首次显示、禁用再启用、Inspector 多选及 Undo 会刷新。fps 或播放速度无效时暂停，防止无穷推进。

添加既有 `EUIRoundedImageModifier` 即可与换图组合。Radius 是 UI 本地半径，夹到绘制宽高的一半；TriangleNum 每角 4–16。尊重当前 sprite/overrideSprite 的 UV、padding 和 preserveAspect，零尺寸不生成非法顶点，禁用后原网格直通。

圆角仅支持 Image.Type.Simple；Sliced/Tiled/Filled 保留原网格并由 Inspector 提示。圆角不改变 alphaHitTest 的点击区域，也不是裁剪其他子节点的圆角 Mask。
