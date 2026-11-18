# UI 通用运行时能力补齐：实施方案

- 任务：`20261116-ui-runtime-extensions`；修订 D1；2026-11-16，Asia/Shanghai。
- 有效需求：`requirements.md` R1，R01–R06、AC01–AC10，无需求增删。
- 性质：本文件是方案阶段的工程决定，范围由入口用户授权；参数和接口细节不是用户逐项指定。尚未实施、编译、运行或独立验收。
- 阶段结论：具备实施交接条件，无关键产品歧义或所需新授权。实施必须先重新核对实际工作区，必要偏差记入 implementation.md。

## 1. 基线、事实和参考边界

当前分支 `codex/local-snapshot-20261111-162819`，HEAD `16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2`。入口 source-baseline.json 的 22 个文件 SHA256 全部相同；Git 状态仍为入口列出的既有修改，无本任务产品改动。保留按钮三份代码、EUIButtonEditTests 及其他全部工作。方案基线另见 `evidence/design-baseline.json`，它是静态检查，不是 Unity 验证。

`packages-lock.json` 中 com.ember 为 `file:com.ember` / embedded，实际维护源 `Packages/com.ember`，package.json 工作区版本 0.25.5。Unity 6000.5.4f1，uGUI 2.5.0（本地解析缓存 `com.unity.ugui@f2d87ebaaa32`），TMP 代码位于该 uGUI 包。正式编辑模板为 topdown-2d 0.1.2，hash `d83863b16bda8c2655f21d6aa3d55693`。本阶段不运行探针；实施阶段通过 Unity MCP 再确认实际包解析、模板身份和新程序集。

归属为框架公共 UI 能力，全部产品改动在 `Packages/com.ember`；不改变模板或业务，不新增 Manager/Module。来源是用户要求参考 Burner，并非 UnityFarm 回流。无模板保存、Bump、快照编辑或消费升级。

| 源码事实（只读核对） | 对方案的影响 |
| --- | --- |
| Burner GameScrollRect 覆盖 normalized 定位、像素/Item 坐标、SmoothDamp、松手吸附和焦点通知；依赖其 GameUIComponent/GameUIContainer | 采用能力意图；不移植容器、0.3 秒强锁位置或速度翻页规则，改为明确的最近项和完成契约 |
| Burner TMPMarquee 使用私有 m_subTextObjects 反射及 TextMeshPro/Distance Field(Marquee) Shader | 不使用该实现或 Shader。Ember 同名文件全是注释，没有生效类型；保留现有 .meta，写成 Ember 的公开 API 实现 |
| Burner GameImage/GameRawImage 持有资源句柄、支持 NativeSize、回调和销毁释放，但与其后端和包装体系耦合 | 使用 Ember Handle/Slot/Manager，自有代次、显示和借用所有权，不引入 Burner 依赖或 EmptySprite 资源 |
| EUIBindingBridge 内置 Image/RawImage/ScrollRect/Text 返回 Unity/TMP 组件；Extension 按 ClassName 精确找 Component | 新能力必须是可独立挂载、可通过 Extension 绑定的 MonoBehaviour，不只扩展旧 EUIImage/EUIText 包装 |
| EUIDynamicListHost/EUIItemList 只支持小型纵向整表重绑；先 BeginUse/绑定数据再 Show；Hide 将实例 SetActive(false)，保留 lease；回池前 Dispose lease | 初次 inactive 绑定可加载；已有请求在 OnDisable 退休；回池使用 lease.Track 兜底，即使实例从未激活也清理。不改现有 lease 语义 |
| EUIImageEx 已有 SpriteArray/SpriteIndex、OnEnable 刷新、ImageEditor 派生 Inspector，使用 overrideSprite | 复用现有组件和序列化字段；补空数组/非法索引清除过期覆盖及 Inspector 即时刷新，不复制另一个 ImageEx |
| EUIRoundedImageModifier 是独立 BaseMeshEffect，仅 Simple；当前未判断 IsActive，零尺寸除法、preserveAspect 处理需补 | 修正这些直接阻止组合使用的边界；不扩成 Sliced/Tiled/Filled 支持 |

TMP 依据已同时核对本地解析源码及 Unity 官方公开说明：`TextMeshProUGUI.OnPreRenderText`、`TMP_TextInfo.meshInfo`、`UpdateVertexData(Vertices)`。本地 UpdateVertexData 对 materialCount 中全部主/子网格上传，因此可覆盖 fallback/submesh，不必读取私有数组。官方参考：[OnPreRenderText](https://docs.unity3d.com/ja/Packages/com.unity.textmeshpro%403.0/api/TMPro.TMP_Text.OnPreRenderText.html)、[UpdateVertexData](https://docs.unity3d.com/ja/Packages/com.unity.textmeshpro%403.0/api/TMPro.TMP_Text.UpdateVertexData.html)；版本可用性以本地 uGUI 2.5.0 源码为准。

## 2. 对外入口、程序集和文件

沿用 UIExtension 的 EUI 前缀和 `Ember.UIExtension` 命名空间。新增组件均 DisallowMultipleComponent、RequireComponent 对应原生组件，AddComponentMenu 使用现有 `UI/EUI/...` 类目，无新顶层 Editor 菜单。Runtime 无 UnityEditor 依赖，日志走 EmberDebug，静态公开方法按 HasGC/NoGC 真实标注。

| 归属 | 新增或最小修改 |
| --- | --- |
| UIExtension/Runtime/Behaviour | 新增 EUIScrollController.cs、EUIImageLoader.cs、EUIRawImageLoader.cs；必要的内部共用图片状态机 EUIAssetImageSlot.cs，只复用流程，不公开第二套资源后端 |
| UIExtension/Runtime/UIExt | 原 TMPMarquee.cs/.meta 保留路径和 GUID，提供 `TMPMarquee` 类型；EUIImageEx、EUIExtensionAttribute 最小扩展；新增 EUIScrollContracts.cs / EUIImageLoadResult.cs 等实际需要的小合同，可就近放 Behaviour，避免空目录 |
| UIExtension/Runtime/Behaviour | 原 EUIRoundedImageModifier 最小修正 |
| UIExtension/Editor | Pages/EUIBindingEditorUtility 的伴随组件发现控制；UIExt/EUIImageExEditor 刷新和组合提示；Behaviour 下必要的滚动/跑马灯配置诊断 Inspector 与圆角 Simple 提示，全部 Editor 程序集 |
| Resource/Runtime | EmberAssetHandle、EmberAssetHandleSlot、IResourceProvider 合同说明、ResourcesProvider 的工厂接入；Manager 只在异常/空句柄适配确有需要时局部处理，不改变初始化或场景/文件加载 |
| 测试 | 新 Resource/Tests/EditMode，复用既有 Tests/EditMode 增加定向 UI 测试；新增 UIExtension/Tests/PlayMode 的短行为夹具，不改运行场景/正式业务 Prefab |
| 文档 | 新 Documentation~/ui/runtime-extensions.md 为三项能力及图片组合的权威使用页，ui/README.md 入链；resource/README.md 和 API 速查更新真实合同；dynamic-lists.md 增加 lease 使用链接，避免复制全文 |

`Ember.UIExtension.Runtime.asmdef` 增加直接引用 `Ember.Resource.Runtime`；现有 UI.Runtime → Resource.Runtime 已存在，不新增反向引用。Editor 仍为 Ember.UIExtension.Editor，只在自身直接消费 Resource 类型时增加显式引用。UI EditMode 测试 `Ember.UI.Tests` 增加 Resource.Runtime 引用；新 Resource EditMode 程序集引用 Resource/Core/Basic，Editor-only、UNITY_INCLUDE_TESTS。新短 PlayMode 程序集引用 UIExtension/UI/Resource/TMP/uGUI/TestRunner，按现有 SceneUI.PlayModeTests 模式隔离。不改 UPM 依赖、第三方版本或游戏 asmdef。

正式 Binding 路径：挂 companion → UI 中心绑定列表选择具体 Extension → 中心生成强类型字段 → 页面调用其公开 API。保留旧 Image/Text/ScrollRect 的自动绑定类型；也可从已有字段 `image.GetComponent<EUIImageLoader>()` / `text.GetComponent<TMPMarquee>()` / `scroll.GetComponent<EUIScrollController>()` 取得。

为让组件在下拉框可发现又不争抢自动绑定，给 EUIExtensionAttribute 增加 `AutoDetect` 属性（默认 true，兼容现有扩展），三个领域的四个 companion 使用 `AutoDetect = false`；GetComponentTypeNames 保留登记，TryGetFirstMatchingExtension 跳过禁止自动选择者。ComponentType 填组件自身类型，表示伴随绑定而非替换 Image/ScrollRect。准确文档化这是 Binding 扩展标记，不是项目中心能力开关。不变更 WidgetTypes 枚举序号、已存绑定 ClassName、旧 EUIImageEx 自动发现或 Bridge 查找规则。测试必须证明旧类型选择不变、新下拉可见、精确 Extension 能取得组件，不能靠 Component 模糊返回第一个组件。

## 3. 滚动定位与吸附合同

选择 companion EUIScrollController，与现有 ScrollRect 同一 GameObject；不替换其类型，不改变 content/viewport 布局所有权、movementType、inertia、decelerationRate。定位支持横、纵及双轴；吸附显式选择单一 Horizontal/Vertical 轴，双轴 ScrollRect 不猜主轴。初版不提供虚拟化、网格索引或速度翻页。

建议公开签名（名称和可观察语义为实施约束；小型 DTO 的布局可按代码习惯落地）：

```csharp
ScrollRect Scroll { get; }
bool IsScrolling { get; }
RectTransform FocusedTarget { get; }
long ScrollToBegin(float duration = 0f, Action<EUIScrollResult> completed = null);
long ScrollToEnd(float duration = 0f, Action<EUIScrollResult> completed = null);
long ScrollToNormalized(Vector2 position, float duration = 0f, Action<EUIScrollResult> completed = null);
long ScrollByPixels(Vector2 offset, float duration = 0f, Action<EUIScrollResult> completed = null);
long ScrollTo(RectTransform target, EUIScrollAlignment alignment = Center,
    Vector2 offset = default, float duration = 0f, Action<EUIScrollResult> completed = null);
long ScrollToItem(EUIItem item, /* 同上 alignment/offset/duration/completed */);
bool IsVisible(RectTransform target, bool fullyVisible = false);
void RefreshLayout();
void Stop();
void SetSnapTargets(IReadOnlyList<RectTransform> targets, EUIScrollAxis axis);
void SetSnapContainer(RectTransform container, EUIScrollAxis axis);
void ClearSnapTargets();
bool SnapToNearest();
event Action<RectTransform> FocusChanged;
event Action<EUIScrollResult> SnapCompleted;
```

`ScrollToItem` 只读 item.RectTransform；列表页面同样可传 `list[index].Item.RectTransform`，不更改列表实现。对齐 enum 为 Begin/Center/End，分别左上、中心、右下；两个已开启轴采用对应边。Inspector 给出 SnapContainer/Axis、SnapDuration=0.15s、SettleDelay=0.12s、VelocityThreshold=20 viewport 单位/秒和 unscaledTime=true；显式 API 也可设置属性。参数负值/非有限值规范化或拒绝，不能引入 NaN。下列坐标合同必须写 XML 和使用页：

1. normalized 沿 Unity 原生：x=0 左，x=1 右；y=1 顶，y=0 底。Begin=(0,1)，End=(1,0)，只写 ScrollRect 开启的轴。
2. `ScrollByPixels` 的正 x 表示向内容右端浏览，正 y 表示向内容底端浏览；是 UI 本地单位，非设备像素。它与目标 offset 的语义不同，名字和示例必须清楚。
3. 目标 offset 使用 viewport 本地坐标，+x 向右、+y 向上；期望目标对齐点出现在 viewport 对齐点加 offset。边界导致无法满足时 clamp 优先，不人为扩大 content/padding。
4. 请求/显式 RefreshLayout 时更新 Canvas/layout 后取世界四角，投影到 viewport 本地空间得到 content 自身矩形及 target 的 AABB；不把所有子对象 bounds 并入 content 代替 content.rect。target 可以是 content 的任意后代，pivot/anchor 及中间层平移/缩放不影响转换。用实际目标 bounds 边/中心，不假定 anchoredPosition 是左上角。
5. 对每轴 hidden=max(contentBounds.size-view.size,0)。target 请求以 viewport 对齐点+offset减目标点得到需要的位移 delta，hidden>epsilon 时 normalized 新值=当前原生 normalized-delta/hidden，然后 clamp；ScrollByPixels 则 x+=dx/hidden、y-=dy/hidden。读取和输出都先做有限性保护。小内容按原生 ScrollRect 的 pivot 扩展 bounds 规则归位，不除零、不构造额外可滚区；查询位置对无滚动轴给稳定 Begin 值。
6. content 与 viewport 使用 ScrollRect 常规同向坐标、单位缩放约定（任意 pivot/anchor；viewport 外的 Canvas 缩放可用）；不承诺旋转/斜切 content 相对 viewport 的非标准 ScrollRect 布局。目标后代可投影取包围盒。IsVisible 默认任意有面积相交，fullyVisible 要求全部 bounds 在 view 内；零面积/无效/非后代返回 false，不算遮挡、alpha 或其他 UI 覆盖。

动画为时长明确的 SmoothStep 插值，默认非缩放时间；duration<=0 立即定位。启动及每次写入前 StopMovement，写入仅作用开启轴。目标型动画每帧在布局有效后重算最终目标，不逐帧 ForceRebuildLayout；内容/viewport 尺寸变化后按新界限夹取，最终帧写精确结果。非活跃/ScrollRect disabled 请求立刻返回 Invalid 或 Disabled 结果，不静默排队；布局尚未可用时允许等待一次有效布局帧，有界失败，不锁 0.3 秒。

每个请求返回递增 operationId；EUIScrollResult 含 id、target、最终 normalized、Completed/Stopped/Replaced/UserInterrupted/Disabled/TargetLost/Invalid。每请求终态回调至多一次，立即请求可在方法返回前回调。先退休旧状态/提交新代次，再通知旧回调，回调后检查新代次，避免回调重入覆盖新请求。Stop/替换/disable/销毁不发成功事件。订阅抛错不得阻止退休清理。目标销毁或移出 content 则 TargetLost。

手动接管：同节点处理 BeginDrag/EndDrag/Scroll 事件，BeginDrag 和 wheel 立即打断动画；原生 ScrollRect 继续处理输入，不吞事件。Scrollbar 或外部 normalized 改动经 onValueChanged 及实际位置变化监测识别，排除本组件 guarded writes 后中断。动画中的原生 onValueChanged 通常发生在后续 LateUpdate，不能仅靠写入瞬间 bool 排除：记录预期位置/操作代次，并在 ScrollRect 之后同步判断，验证不会把自己的下一帧通知当输入。ScrollRect.enabled 变化、无 EventSystem 的程序化改动也要有终态。

吸附候选为显式目标列表副本，或 container 的活跃直接 RectTransform 子项（每次开始吸附/结构改变时重取，过滤 template/无效/非 content 后代；不包含 inactive 缓存）。容器及列表改变使当前吸附失效。按当前目标中心到 viewport 中心在所选轴上的绝对距离选最近项；等距离按候选顺序。FocusChanged 仅对象身份改变时发一次，包括首次有效候选和清空至 null；不能以数组下标作为身份。

EndDrag 后保留原生惯性：无拖拽、最后手动输入/位置变化至少 0.12s、轴速度<=20 且弹性越界已基本回弹（<=0.5 UI 单位）时启动吸附；wheel/scrollbar 停止同样走该条件。结束后选最近有效项，StopMovement，以 Center 和默认 0.15s 定位；首尾夹到可达边界算成功，不强造两端居中间距。SnapCompleted 只在当前吸附达到最终可达位置时一次，FocusChanged 不表示动画完成。人工 `SnapToNearest` 可立即停惯性并发起。程序化 ScrollTo 完成后不自动再吸附，Stop 或取消吸附不自动重启；下一轮手动输入或显式 SnapToNearest 才触发。

## 4. TMP/TMPEx 跑马灯合同

复用原 TMPMarquee.cs 的 GUID，新类 `Ember.UIExtension.TMPMarquee : MonoBehaviour` 只作用 TextMeshProUGUI（自然包含 TMPEx）。选择水平往返模式：开头停顿 → 向左移动到文本尾端可见 → 尾部停顿 → 向右回到开头，循环。无首尾拼接副本、无专用 Shader，无 RectTransform 移动/改宽度/重挂父子/驱动 ContentSizeFitter。

公开 `SetMarqueeEnabled(bool enabled, float speed=30f)`、`ResetPosition()`、`Refresh()`；属性 IsRunning、Speed、StartDelay（0.5s）、EndDelay（0.5s）、UseUnscaledTime（true）、Viewport（可选显式祖先裁剪节点）。speed 是文本本地 UI 单位/秒，<=0 暂停并保持当前位置，ResetPosition 恢复开头。时间参数限制有限、非负；设置速度重复调用也应生效，不复刻 Burner 只在开关变化时改速度的问题。

使用前置明确：固定窗口宽度、单行、关闭 AutoSize、NoWrap、overflowMode=Overflow、祖先 RectMask2D 或 Mask，且其裁剪窗口与文本使用的可视窗口一致。默认窗口为文本自身 rect 的左右边减 margin；Viewport 指定时使用其矩形在文本本地坐标的交集。裁剪由原生 Mask/RectMask2D 负责。组件不擅自给节点加 mask、不改 TMP wrapping/overflow/AutoSize 源配置：不满足时保持静态并给一次清晰诊断（Inspector 实时提示），使用页和测试夹具展示正确配置。选择这些前置是为了不改变页面布局真源或悄悄覆盖作者设置；普通默认 TMP 要先按说明配置。

显式多行/换行、3D TextMeshPro、RTL/竖排跑马灯、与其他独立顶点位置动画同时写同一 TMP、自动字号收缩到不溢出时强制跑马灯，不在本次支持范围。不能把自动换行后第二行当做横向溢出。rich text、TMP sprite glyph、中文主字库+fallback 子网格在单行条件内支持；正文和材质不改。

算法使用公开 OnPreRenderText 接收 TMP 原始 geometry，复制当前 materialCount 下每份 meshInfo.vertices 的有效区间作为基线；从实际可见字符的主/回退/精灵顶点合并 x 范围，忽略未用顶点和空格占位，结合实际 line/preferred advance 保证行尾可读。空文本/零宽窗口/无可见字形直接静态。可视文本宽度<=窗口宽+0.5 时偏移为零且保留原对齐；溢出时用一次基础位移让文本起点对齐窗口左边，再在 [0, textWidth-windowWidth] 内往返。首次/显式 Refresh/文本宽度或字体变化重置起点和停顿。

每帧只对基线顶点赋 `base + 本帧offset`，绝不对上帧结果累加。更新所有 materialIndex 的有效顶点，保留 UV/颜色/材质引用，调用 `UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices)` 上传；OnPreRenderText 内只改已生成的数据，不递归 ForceMeshUpdate。首次和 dirty 时按公开 ForceMeshUpdate 有界重建；避免每帧全文解析、复制数组、GetComponents 扫描或 material 实例化。容量变化才重分配缓存。

变更检测由 OnPreRenderText 新基线、TMP text changed 通知、RectTransform 尺寸变化及 viewport/margin/font/material/text/signature dirty 补充；字体皮肤由 TMPEx 正常换 font/fontSharedMaterial 后触发 TMP 重建。不能仅监听字符串，因为 fallback atlas/materialCount 可以在文字不变时变化。适配布局更新顺序，以最新 geometry 为准，并避免 TEXT_CHANGED_EVENT/自身 UpdateVertexData 形成反复 dirty。材质数量减少后释放旧缓存引用。其他颜色渐变可以保留，但不承诺多个写顶点位置效果叠加。

关闭/OnDisable：立即取消时钟、解除事件、恢复零附加偏移的当前 TMP 原始网格（溢出时也恢复作者原对齐），不恢复过期文本或过期字体。重新启用：若用户开关仍开启，从最新文本开头重新停顿播放；不累计隐藏时间。OnDestroy 幂等解绑并清缓存，无材质销毁，因为未创建材质。Mask/RectMask2D 及其生成的 stencil/fallback 材质继续由 TMP/uGUI 管理，不写 shared/font/materialForRendering 的 Shader 参数，不修改源字体/atlas/共享资产。

## 5. 资源基础合同的最小修复

以下为源码事实及静态推导，尚无本轮运行结论：

| 问题 | 当前代码依据 | 本任务必要处理 |
| --- | --- | --- |
| RES01 取消非终态 | EmberAssetHandle.Cancel 先 _isCancelled=true；Complete 对 _isCancelled 直接 return | 用单一终态入口，先置 IsDone/Cancelled/Error，再执行后端取消与一次 Completed(null)，不因取消标记拒绝自己的终态 |
| RES02 成功资源无释放 hook | Handle.Dispose 只清 asset/callback；_cancelAction 仅 pending Cancel 调用 | 分离 cancel 与 release，成功/失败/取消后的 Dispose 均可释放本请求 Provider token，最多一次；UI 不按路径盲目 UnloadAsset |
| RES03 外部 Provider 无法正常创建/完成 Handle | 构造、SetCancellation、Complete 都 internal，而 IResourceProvider 是公开接口 | 增加最小 public factory 和 public TryComplete；保留现有 internal 入口兼容，不要求消费方反射或进入 Resource 程序集 |
| RES04 Slot 失败被当当前路径缓存 | OnHandleCompleted 无 Succeeded 判断，总把 handle/path 置 current | 只把成功资源晋升 current；失败句柄释放，不缓存失败，同路径可重试；原成功资源保留到替换/明确 Clear |
| RES05 Slot 异常/取消释放不全 | CancelLoading 只 Cancel 不 Dispose；回调后释放 oldHandle 没 finally | 先失效并解绑，Cancel+Dispose pending；回调期间可重入，finally 释放真正退休的旧句柄，抛错不泄漏 |
| RES06 Slot 身份/路径 | 无回调来源参数，只看 _loadingHandle；NormalizePath 使用文化相关 ToLower 且标 NoGC | 回调携带句柄身份+代次，晚到不能吃新状态；按 Ordinal 精确路径去重，不改后端地址大小写，去除错误标注 |

公开工厂建议 `EmberAssetHandle<T>.Create(string path, Action cancel = null, Action release = null)`，返回 pending；公开 `bool TryComplete(T asset, bool succeeded, string error = null)`，只允许 pending → terminal，拒绝重复/迟到返回 false；查询补 IsCancelled/IsDisposed。继续兼容 Completed(Action<T>)，成功资源、失败/取消 null；完成后订阅立即通知，Dispose 后新增订阅忽略。事件通知前快照并清空订阅，单个回调异常要隔离、记录并继续其他订阅，释放逻辑在 finally 中完成。所有 API/Provider 回调只在 Unity 主线程调用，本任务不增加线程安全调度系统。

Cancel 只逻辑终止 pending 请求，已完成时无操作；Cancel 不等同物理中断或资源卸载，仍应 Dispose。取消状态先提交，后端 cancel 同步重入完成也被拒绝，取消/释放委托异常仍保证终态和清理。Dispose 幂等：pending 先完成取消，清资源引用且 Succeeded=false，释放 token 一次；已成功 Dispose 不补发第二次 Completed，现有持有 Asset 的人必须另有资源所有者。release callback 在清所有权后调用，不靠 Manager 当前 Provider（可能已切换/销毁）。

IResourceProvider 无破坏性签名变更：Provider 在 LoadAssetHandle 内创建操作，Create 捕获此操作的取消和释放委托，最终 TryComplete。release 是每次请求的引用凭证，不能按 asset 实例 Destroy，也不能因两个相同 path 请求而两次释放同一个未增引用 token。后台不能物理取消时，Provider 必须在逻辑取消/Dispose 后仍观察其后端完成，并使后来取得的引用归该已释放 token 清理；TryComplete(false) 不接受资源所有权。必要示例展示 token 已 release 时迟到结果立即由 Provider 释放，不能把拒绝 Complete 等同自动销毁对象。

实际随包仅 ResourcesProvider，当前 LoadAssetHandle 是同步 Resources.Load；没有真正物理取消，也没有逐请求引用计数。其 release 只放下 Handle 强引用，不调用 Resources.UnloadAsset/Destroy，不冒险卸掉其他使用者共享 Sprite/Texture；全局 UnloadUnusedAssets 仍按原生命周期。自定义 Addressables/YooAsset 后端未交付，本次只提供可实现的公开合同及可计数测试 Provider，不声称已验证那些后端。不更改 EmberFileHandle、场景或纯回调加载语义，不引入下载/HTTP 后端。

Slot 保留 LoadAsync/CancelLoading/Dispose 签名；新增可复用 `Clear()`、只读 `CurrentAsset`、`LastError`。Clear 失效 pending 并清 current，Dispose 永久封闭。LoadAsync 同一成功地址立即回调当前资源；同一 pending 地址只保留最新回调（旧回调不再通知，UI 层负责终结自己的旧请求）；不同地址先取消旧请求。失败保留旧成功 current，同时回调 null、LastError 供 UI 形成结果；空地址 UI 层 Clear，不作为 Provider 加载请求。同步完成须先写 loading 身份，再订阅 Completed，避免“返回前完成”漏接。旧句柄只能在新显示引用已切换后释放；业务回调异常也执行 finally。保留 reapplyIfCurrent=false 旧语义并补 XML，UI 层固定用 true。

回归仅覆盖 Asset Handle/Slot 新合同、Manager 未初始化失败及同步/延迟 Provider、SceneUI 实际 HandleAdapter 取消/Dispose 这条直接调用链；不借机修整个 Resource 模块。

## 6. Image/RawImage 的显示和生命周期

EUIImageLoader 绑定 Image（可为 EUIImageEx），EUIRawImageLoader 绑定 RawImage，内部各持 EmberAssetHandleSlot<Sprite/Texture>。公开形态：

```csharp
long Load(string path, Action<EUIImageLoadResult> completed = null);
void CancelLoading();
void Clear();
void SetSprite(Sprite borrowed);       // Image 版本
void SetTexture(Texture borrowed);     // RawImage 版本
bool IsLoading { get; }
string CurrentPath { get; }
string LoadingPath { get; }
// Placeholder / FailureImage(或Texture) / LoadingDisplay / KeepNativeSize 属性
```

结果含 operationId、原 path、Success/Failed/Cancelled/Replaced/Cleared/ManualOverride/Disabled/Destroyed、Error；不转移资源所有权，也不把结果中的图片当拥有型句柄。回调可读取目标组件现有显示，但不得在组件释放 token 后继续使用资源而不另取引用。结果回调每次 Load 恰好一次（包括清空和替换），清状态/提交代次后通知，重入按新代次守卫。订阅抛错记录且不妨碍清理。

| 场景 | 合同 |
| --- | --- |
| 默认加载显示 | 默认显示借入 Placeholder（未指定则 null），不改 color/alpha、enabled、raycastTarget、material、uvRect；uGUI 空 Image 可能显示白色块，这是原生语义，不伪造全局透明 Sprite。需要透明占位时由作者提供透明 Sprite 或显式控制样式 |
| 可选 KeepPrevious | pending 保留当前图和当前 token 到成功/清空；失败默认显示 FailureImage，未指定回退 Placeholder；可选失败保留前图。配置选择在 Inspector/XML/文档明确 |
| 成功 | 先核对操作代次/目标/手动接管，设置图，再按 KeepNativeSize 调原生 SetNativeSize，最后通知；Slot 在新显示完成后释放旧 token |
| 失败 | 形成具体 Failed/Error，按失败策略显示，Placeholder 策略不再显示旧资源时 Clear 其 token。不能把失败 path 当已成功 current，同地址再次 Load 必须可重试 |
| null/空地址 | 等价 Clear，退休旧请求、清显示/当前地址并释放拥有资源，当前 Load 返回 Cleared；不访问 Provider，不强行 Trim 或变更路径大小写 |
| 同路径 pending | 一个后端请求，旧 UI 请求先 Replaced，新 UI 请求承接 Slot 最新回调；同路径成功可同步 Success，无重新 Provider 负载。A→B→A 且旧 A 仍为 current 时取消 B，再立即使用仍拥有的 A |
| A/B 反序 | UI operationId + Slot generation/handle 双重校验；A 完成不能覆盖 B，Provider token 最终释放；不能只比较路径（A→B→A 有同路径不同代次） |
| CancelLoading | 当前 pending Cancelled，保留最后成功已显示图（若当前仅占位，恢复此前仍拥有的图或继续占位，明确不暴露已释放图）；不自动重试 |
| SetSprite/SetTexture | 显式手动接管，先退休所有加载和已拥有 token，但先移除显示引用；新图为借入，只赋值，不 Destroy/Unload。即使与当前对象相同，若它来自 loader，调用方必须另有独立所有权；不能“同一对象赋回”让已释放的拥有资源变成有效借用 |
| 直接 image.sprite/overrideSprite 或 raw.texture 赋值 | 注册 Graphic dirty 回调并比较上次本组件写入的原值/有效值，内部写 guarded；完成前再检查一次，LateUpdate 兜底。观察到外部实际变化立即退休旧请求/资源为 ManualOverride，保留外部图。Image.sprite 不是 virtual，不声称拦截相同值写入或两次变化后恢复原值；需要确定地表达接管用 SetSprite/SetTexture。颜色/材质 dirty 不应被当赋图 |
| GameObject 隐藏/组件禁用 | OnDisable 退休 pending 并释放 current，若仍显示自己拥有的图则先换借入 Placeholder/空，再 Dispose，不留下悬挂引用。重新启用不自动重放旧请求，需要 OnShow/新 Bind 明确 Load |
| 初次未激活 Bind | lazy 取得目标/Slot，允许 Load 和同步完成，不依赖 Awake/Start；后续 Show 不重复加载。若请求在未激活实例上结束借用，lease.Track(loader.Clear) 使其退休，不能依赖从未发生的 OnDisable |
| 回池/销毁 | EUIItemLease.Track(loader.Clear) 结束借用；OnDisable/OnDestroy 双兜底，Dispose 幂等。仅 CanvasGroup.alpha=0/CanvasRenderer cull/Graphic.enabled=false 不代表数据借用结束，按“仅表现隐藏”保留资源；业务需要释放时调用 Clear。不要静默改现有 EUIItemLease.Hide 的存活含义 |

Image 路径的显示赋值需协调 overrideSprite：加载拥有显示期间清旧 override 并写 sprite，记录原始与 effective 值；外部 override/SpriteIndex 接管时不再抢回显示。与 EUIImageEx 组合时不清作者 SpriteArray/不改序列化索引。启用帧动画会持续写图，加载请求应以明确错误拒绝，提示先 Animated=false；无动画的手动 SpriteIndex 切换视为接管并取消 loader。OnEnable 刷新索引是现有 ImageEx 行为，不允许 loader 迟到结果再覆盖它。使用文档把异步单图和序列帧显示的所有者说明清楚，不删除已有数据来避免冲突。

必须记录每个拥有资源是否仍挂在 Graphic 上，卸下显示引用先于释放。占位/失败/手动图都是借入，不进入 Slot，不调用 Destroy/Unload/Dispose 资源本体。不要用 Resources.Load 包装绕过 ResourceManager，也不要在关闭页面时全局 UnloadUnusedAssets。

**从未激活即销毁的兜底也必须实现。** 不能假定 inactive 对象必然收到 OnDestroy。第一次 Load 的 lazy 初始化在对象尚存时缓存并登记原生 destroyCancellationToken，回收逻辑独立且幂等；同时只对尚未 Awake 且拥有 pending/current 的实例，用已引用 UniTask 的 PlayerLoopHelper/IPlayerLoopItem 登记一个内部轻量存活观察项。它每帧只检查目标组件是否仍存活：销毁时释放纯资源状态而不访问已销毁 Graphic；Awake 已发生或 Clear/Dispose 结束所有权时移除，之后走普通生命周期。不新增全局 Manager，不扫描场景，不新造协程宿主。框架已有 AsyncDestroyTrigger.AwakeMonitor 展示了同一 never-active 问题及 PlayerLoop 解法，可参考但不修改 UniTask。必须实测 never-active GameObject 销毁、仅 loader 组件被移除、同步已成功/仍 pending 两种状态；回调仍遵守代次并且 token 释放一次。原生 token 的缓存要求见 [Unity 官方 API](https://docs.unity3d.com/ja/current/ScriptReference/MonoBehaviour-destroyCancellationToken.html)。EditMode 夹具显式 Clear/Dispose，真正 PlayerLoop 兜底用短 PlayMode 验证，不能用测试手动调私有 Tick 冒充生命周期。

## 7. 多图索引和圆角的局部补齐

EUIImageEx 保留 GUID、全部序列化字段和公开 SpriteArray/SpriteIndex/Animated API。索引不强制改写作者输入：有效项设 overrideSprite；空数组、非法索引或空项统一清 overrideSprite，回退 base sprite（base sprite 也空则原生空 Image 语义），不能残留上次覆盖。OnEnable 在 Animated=false 时保留索引并刷新；SpriteArray 更新立即刷新，即使索引未变。Inspector ApplyModifiedProperties 后对修改的目标刷新（保留多选/Undo/Prefab override），含编辑期与首次预览，避免只有重新启用才更新。

动画本身不重构；校验 fps/playbackSpeed 非有限、<=0 时暂停，避免负 tpf 导致 while 无穷循环，这是 Inspector 可输入而影响本次切图的必要防御；不更改默认帧率/循环模式。OnEnable 重置延迟状态，防止前一次 disabled 留下等待。补 Inspector 提示“静态索引切换请关闭帧动画”“圆角由独立组件提供，仅 Simple”。

圆角维持同一个 EUIRoundedImageModifier：ModifyMesh 先判 IsActive、有效 Image 及 Simple；零尺寸/退化绘制区域安全返回或清空，不除零；有限半径夹在 [0,min(width,height)/2]，triangle 数夹 [4,16]（包括序列化脏值）。绘制边界遵循 Image.preserveAspect 和当前 active/override sprite 的 padding/UV，避免换图后拉伸及无效 mesh。Radius/TriangleNum 改动 SetVerticesDirty，disable 不再改网格，enable 恢复生效。Simple 以外原网格直通，Inspector 明确警告；不承诺圆角改变 alphaHitTest 点击区域，也不扩成圆角 Mask。

## 8. 定向验证和验收映射

本阶段只完成静态源码核对，不执行以下验证、不标 AC 通过。实施开始先完整复读 unity-validation.md 并声明本批具体过滤。预估新增约 35–50 个参数化 EditMode 行为情景、4–6 个短 PlayMode 情景；测试类与参数数量以必要覆盖为准，不为凑数。EditMode 目标几十秒，PlayMode 目标 30–60 秒，首次导入/域重载耗时另记；若实际估计为长时/数分钟以上批次，先说明并请求授权，不能拆批规避。仅直接回归，不全量测试或全工程试玩。

每批 C#/asmdef 完成：Unity MCP refresh_unity compile=request → 确认本轮编译及域重载完成（新 API/程序集 MVID 等实证）→ 非 compiling/updating → Console → 指定测试。只记录刷新成功不合格。断连先检查待编译并尝试请求，送达不明先查状态；无法送达立即提示用户手动触发编译，再每 30 秒最多 10 次恢复；不使用日志轮询/BatchMode/dotnet 替代。

| AC | 定向用例/方法及观察值 | 执行与证据 |
| --- | --- | --- |
| AC01 | EUIScrollControllerEditTests：横/纵 Begin/End、Center/Begin/End+正负 offset；pivot 0/0.5/1、stretch anchor、嵌套 target；空/小/零 content，布局高度变化；IsVisible 相交/全部/零面积 | 真实 ScrollRect/Canvas/LayoutGroup，调用公开 API；以世界四角投影后的实际相对边/中心和有限值断言，不能复制 normalized 算法做预期；保存参数和失败现场 |
| AC02 | 短 PlayMode：真实 EventSystem ExecuteEvents 拖拽/松手与 wheel；惯性停止后最近项和等距/边界；动画 A→B、Stop、目标销毁、回调重入、timeScale=0；Scrollbar 输入；旧延迟 onValueChanged 不误取消自己 | 记录 operationId、结果/Focus/Snap 事件序列，检查每请求一次终态、仅成功发 SnapCompleted；最终位置可达，无迟到覆盖；截图一组横纵首尾 |
| AC03 | TMPMarqueeEditTests/PlayMode：短长文本、速度=0/恢复/两次设置速度、首尾停顿、disable/enable/销毁；原 RectTransform/TMP 配置不变 | 测实际 mesh 在两次已知时间采样的距离、往返边界、复位后原几何；短文本完全稳定；记录 mesh snapshot 和截图，材质引用/数量前后对照 |
| AC04 | 原生 TMP 和 TMPEx；改文本/宽度/margin、切字体皮肤、中文+缺字回退符号+sprite glyph，至少两个 materialIndex；分别 RectMask2D 和 Mask；缺 mask/AutoSize/多行错误配置 | 所有子 mesh 位移一致，重建后无累计偏移；源 fontSharedMaterial/Shader/Atlas 引用及布局不被组件写；真实渲染截图确认裁剪与 fallback，不能仅数顶点。其他写位置效果、3D/RTL 属明确不适用限制，不能把声明支持的 fallback 标不适用 |
| AC05 | EUIAsyncImageEditTests：Sprite/Texture、同步成功/失败、未初始化 Manager、空地址、placeholder/keepPrevious/failure、NativeSize | 延迟 Provider 仅控制后端完成时序，调用真实 Manager/Handle/Slot/Loader；另用真实 ResourcesProvider 导入小型临时 Sprite/Texture 资源，验证同步返回前完成和真实图；夹具清理临时资源与单例注册状态 |
| AC06 | A/B 反序、A→B→A、同地址 pending/成功/失败重试、Cancel、直接 sprite/override/texture 赋图、Explicit Set、回调重入/抛错；首次 inactive Bind→Show、Hide、回池前未 Show、Destroy；never-active GO/仅 loader 移除、同步成功和 pending | 检查最终 Graphic、operationId/每回调次数、Provider 每请求 cancel/release 计数和无未释放 token，外部图存活；真实 EUIItemList lease.Track+Hide/Show 场景，OnShow 明确重发后成功，无旧路径自动重播；PlayerLoop 兜底观察在无资源/已 Awake 后不残留 |
| AC07 | EmberAssetHandleEditTests/SlotEditTests：取消先终态且一次通知、完成后订阅、成功 Dispose、重复 Dispose、后端取消同步完成、release 抛错、迟到完成、当前失败不缓存；两个同路径独立 token；大小写/文化测试；SceneUI 真实 HandleAdapter 取消链 | public factory 从独立测试程序集创建，不反射内部状态；fake Provider 验计数，ResourcesProvider 验真实共享资产仍存活；补 SceneUICore 的 ResourceHost_* 三例直接回归，不跑整个 SceneUI |
| AC08 | EUIImageExAndRoundedEditTests：拖两 Sprite、Animated=false、0/1、首次/重启、数组替换、空项/空数组/非法索引、Undo/多选；圆角半径/三角数边界、disabled、零尺寸、preserveAspect、Sprite UV 更新 | 使用 SerializedObject 和实际 Inspector 路径验证字段可见/更新；实际 mesh 有限且圆角范围变化；Simple 组合视觉截图，Sliced/Tiled/Filled 原 mesh 直通并提示；不只查组件存在 |
| AC09 | EUIRuntimeExtensionsBindingEditTests：下拉登记、旧自动检测保留、四 companion 精确 Extension、center generator 生成类型与 Bridge map 一致；实际页面逻辑使用新 API | 通过 EUI 中心既有生成/绑定服务处理临时测试夹具；若需持久正式示例必须按 ember-eui-build/UI 中心创建，禁止手写 Binding。增量回归 EUIButtonEditTests（前轮16例，以实际数为准）、EUIDynamicListEditTests、EmberFontSkinEditTests、EmberLocalizationEditTests 及受影响绑定检测方法；不跑 Ember.UI.Tests 全程序集 |
| AC10 | 每批 MCP 编译完成/Console、上述精确 filter 及结果、证据时间、使用页步骤可复现、独立对话复核 | implementation.md 逐项自检和 evidence；acceptance.md 由下一对话逐项判定；静态/编译/自动/视觉/人工区分，缺视觉截图或 fallback 实测不能写通过 |

测试夹具可即时构建原生 Canvas/ScrollRect/TMP，并在测试结束恢复场景/Play 状态、timeScale、字体皮肤、ResourceManager/ServiceLocator、临时导入资源；不把夹具当正式示例，不保存或覆盖用户当前场景。可视验证通过 Unity MCP 创建/观察临时层级，保存截图与层级参数至 evidence；不擅自改用户已打开的页面资源。自动输入/截图记自动行为或视觉检查，不记用户人工签收。实际点击/设备测试未执行则明列。

## 9. 实施顺序、迁移和回退

1. 复核 Git/UPM/模板/源指纹，备份将修改的既有文件到本任务 evidence/backups（含 .meta，尤其所有未提交文件），记录新文件清单，不能以 HEAD 代替用户工作区。先取得本轮基线 Console/实例信息，避免将原有错误归本任务。
2. 修 Asset Handle/Slot 及 Provider 最小合同，写行为测试；MCP 编译/相关测试，确认 cancel/release 后才让 UI 依赖它。若发现更广泛后端设计阻碍，保留已完成部分、记录具体差异，只对新增实质范围回交，不暗扩文件/场景资源系统。
3. 新滚动 companion 与定向几何/输入用例；新 TMPMarquee 与必要 Inspector 诊断；按完整小批编译验证。运行只覆当前批相关测试，最终回归可以一次执行尚未运行的跨功能用例，已通过者无变更不反复重跑。
4. 新两类图片 loader，接 Handle/Slot，补竞态/所有权/EUI inactive+pool 用例；再补 ImageEx/圆角局部边界与组合验证。
5. 伴随 Binding 登记、中心生成/Bridge 验证及旧自动选择回归，完成一组短 PlayMode/视觉证据。文档与实现同步，检查没有 Burner namespace、私有 TMP 反射、missing Shader、模板写入或丢失用户按钮改动。
6. 写 implementation.md：实际文件/API、偏差、AC 自检及限制、各批编译/测试证据和未覆盖项；更新 handoff/验收提示词，顺序创建独立验收对话，明确检查已启动。本对话不代写 acceptance.md。

新增能力默认不挂现有 Prefab，故旧页面不需迁移；旧 ImageEx 字段、GUID、Binding enum 保持。采用新 companion 的页面通过 UI 中心重新生成 Binding，用户逻辑按公开 API 接线。Slot 地址去重改成 Ordinal 是修复不正确的后端地址折叠，文档说明大小写不同地址不再合并；本仓库目前无 Slot 其他调用方。外部 Provider 可继续实现原接口，新工厂提供所缺的实际创建/终结入口；必须按 release 合同接入各自 token，不能声称升级自动补好第三方后端。

回退时以本轮备份和变更清单逐文件/逐 hunk 撤销，仅删除本任务创建且无其他编辑的新文件，不 git reset/clean，不覆盖前轮按钮和其他用户修改；有冲突先保留并报告。已挂新组件的页面先由 UI 中心移除/重绑再删除脚本，保留已有 .meta，不操作 Templates~/ParentSnapshot~。资源合同与 UI 依赖需成组回退，不能只删 loader 或只撤 release hook 导致残留依赖。回退代码同样须 MCP 编译及直接受影响测试。

正式文档由实施阶段写入包内；最终需求/设计取舍、交付、逐 AC 验收与必要证据由入口收束到 docs/tasks/20261116-ui-runtime-extensions，验收前不归档清理。提交/推送/发布/模板保存/Bump/部署/全量长时测试仍无授权，当前已计划实施不依赖这些操作。
