# 柔光玻璃 UI 与 GM 控制中心：实施方案

- 任务：20261117-soft-glass-ui-gm；方案 D1；2026-11-17，Asia/Shanghai。
- 输入：[需求 R3](requirements.md)，R01—R08、AC01—AC13；[入口与环境](handoff.md)。本方案不修改需求。
- 性质：基于当前磁盘源码的设计决定；新增类型、字段、路径均为拟实现契约，不能视为现有 API、资产或验证结果。
- 阶段：方案完成，可交实施。渲染接线须先完成 S1 的定向可行性验证；base 正式保存/分发须另获模板操作授权。没有运行时探针、编译、测试、产品代码或资源改动。
- 用户最终实现预览确认：**未提供、未确认**。本方案及其中线框不能代替 AC13，任务未完成。

## 1. 决定与理由

1. 框架提供遮罩策略、背景采样契约、圆角玻璃 Graphic 和可选 URP 后端；base 提供 EUI 皮肤库、公共弹窗、GM 与启动接线。玩法模板只继承、选择和扩展，不复制渲染实现，不将通用 UI 固化为 topdown 内容。
2. 新风格是一套独立 `EUILibrary`、Prefab 与外观配置。**本轮不登记新的正式 Ember 可组合扩展**：选材质/Prefab 属于运行时表现配置，既有扩展组合是开发期能力与工具门禁，两者不可混用。没有预造扩展 ID/hash、没有修改现有默认扩展组合的必要性。若以后新增独立制作工具能力，另按正式契约登记、兼容检查和真实指纹签收。
3. **Popup 的效果默认值为 Blur；旧 UI 的控件皮肤不随之改变。** 页面效果 `Inherit=0`，全局默认 Blur；显式 Solid 恢复原纯色，关闭模糊仍保留阻挡。`UseMask=false` 或逻辑拒绝建遮罩时直接不创建遮罩、不申请该遮罩的采样资源。
4. 采用 **URP RenderGraph + 按 UI 绘制边界分相机段 + 共享降采样模糊**。在目标页面的遮罩/玻璃绘制前采样完整下层合成结果。背景包含场景透明物、HUD、旧 UI 和更低 Popup；不包含该层自己的遮罩、面板、文字及更高层。禁止用 Opaque Texture 或在所有 UI 之后抓屏冒充此能力。
5. 默认实时更新；按需快照用于明确的低质量档和超出实时边界预算的深层降级。快照有所属边界、版本与失效条件，不能读回屏幕后把上一帧自身再次卷入。
6. 新 GM 独立生成 Business Page/Item，复用已有行为和调试资源；旧 `GMPage`、其 GUID、注册及类保留。启动入口在业务层按配置二选一，新入口是可拖动吸边圆点。新 GM 业务不放进通用框架或通用代码模板。

## 2. 当前源码事实与设计基线

Git 分支 `codex/local-snapshot-20261111-162819`，HEAD `16cff3d85d3b40a5ddab11e30801dfa5e36ed0c2`。Ember 从 `Packages/manifest.json` 的 `file:com.ember` 和 lock 的 embedded 解析到物理 `Packages/com.ember`，标称 0.25.5，工作区有未发布差异。Unity 6000.5.4f1，URP 17.5.0（本机缓存 `Library/PackageCache/com.unity.render-pipelines.universal@e38be786c41e`），uGUI 2.5.0。

正式编辑身份是 topdown-2d 0.1.2 / `d83863b16bda8c2655f21d6aa3d55693`，父 base 0.10.2 / `0b0431013fcd599ea1d8f1c27aafd169`。扩展项目配置仍绑定 topdown 0.1.1；仅能记录身份不一致，未运行正式求解器，不能称现有可选扩展有效，也不顺手重签收其他任务的配置。

| 当前维护源/源码 | 已核对事实与影响 |
| --- | --- |
| `UI/Runtime/EUIBgMaskPool.cs` | 池容量 10；Image/Button/Canvas/GraphicRaycaster；ScreenSpaceCamera；排序为页面 order−1；回池只关对象和清监听。没有模糊采样、材质租约或后端。新增效果必须在每次借用彻底重置，不能把 RT 长留在池实例。 |
| `UI/Runtime/EUIManager.cs:935`、`EUIViewEngine.cs:486` | UseMask 和 `ShouldCreateClickableMask` 双门禁；点击转 `NotifyClickMask`，无 Logic 按 ClickMaskToClose。`_activeMasks` 按页面记录对象。保持这些调用和清理含义，增加会话所有权。 |
| `UI/Runtime/EUIPageContext.cs` | Main 起始 1000、每级 500、TopMost 25000，GM FreePage 显式 30000；Popup order 随路由层叠。FullScreenPopup 会提前隐藏下层；首次采样必须在隐藏提交之前准备，不能抓到已清空的背景。 |
| `UIExtension/Runtime/EUIBinding.cs:649` 与 `EUIBindingBridge.cs:70` | useMask/maskColor/clickMaskToClose 由 Bridge 注入 Page；MaskColor 是非 nullable Color，因此旧 Binding 总会覆盖全局颜色，即使保留默认黑 0.5。效果类型不能从颜色/alpha 推断。 |
| `Assets/Ember/Editor/SOs/EmberCSharpImplementation.asset` | codePath=`Assets/Game/UI/Runtime`，业务 namespace=`Game.UI`，pageDefFile=`Assets/Game/UI/GamePages.User.cs`。新 Business 资产 classPath 使用 `Module/UIFoundation`，预检结果才是实际落点。 |
| `UIExtension/Editor/Pages/EUICreationService.cs`、`EUIBindingEditorUtility.cs`、`EUIBindingCodeGenUtility.cs` | 有 TryBuildPlan/Create、绑定快照和公开 TryRegenerateCode；骨架、Item、Page、绑定和注册必须走这些入口。新增遮罩字段须贯通 CreationRequest、快照/Apply、Inspector、Bridge、契约导出，不能只有 Inspector 能改。 |
| `UIExtension/Runtime/Behaviour/EUIRoundedImageModifier.cs` | Simple Image 网格裁圆角；不支持 Sliced/Tiled；本地已补 null/有限值/零尺寸/preserveAspect，保留这些变更。新解析玻璃 Graphic 不强行套此 Modifier；旧资源继续原实现。 |
| `UIExtension/Runtime/Resources/UI-NodeBlurEffectShader.shader` | CG 三 pass，降采样+横纵三 tap；没有 URP RenderGraph、uGUI stencil/clip 契约。NodeBlur 引用在注释的 Burner/GamePage 中，无活动后端，不作为可用链路；旧 Shader 路径保留。 |
| `Assets/Game/UI/Runtime/Framework/GMPage.cs` | 切面板、退出调用 GameLauncher.Quit、时间同步 Unity Time 与 EmberTimeManager、状态文本、增强 Toggle；还保留 Scroll/Image/RawImage/Circle 测试展示。退出/时间/测试不能只剩视觉占位。 |
| GM 的真正生成/维护关系 | 当前 `CSharpLogicImplementationData.cs:529–584` **仅首次生成 .cs 骨架**，普通重新生成保留现有受管内容，每次覆盖 Binding；`EUIBindingCodeGenUtility` 单独同步 UIUpdate 等可选标记。`CSharpCodeTemplate.Framework.tpl` 是通用骨架，不含 GM 业务。GM 的业务维护源是活动 Assets 里的 GMPage.cs，再由项目中心保存进入 base；没有发现另一份活动 GM 代码模板。旧文件头“刷新整块替换”的说明比现行行为更保守，仍不可据此直接改受管块。新 GM 用独立 Business 用户 partial，避免依赖这个历史歧义。 |
| `Assets/Game/State/GameInitState.cs` | OnEnter 中现有 ShowTopMost(GamePages.GMPage) 是业务启动接线；同时有 topdown PlayerTrial/GameInput 接线。未来只替换 GM 选择这一句，保留其余业务，不重写整文件。 |
| 公共库 | `DefaultLibrary.asset` 的 EmberDefaultLight 1.1.0 已有按钮、Switch、Slider、Input/Search、StandardPopup、Alert/Confirm 和示例。它是 Editor 维护资产，不是现成运行时主题引擎；不直接让运行程序集引用 EUILibrary Editor 类型。 |
| 现有本地改动 | UI 运行时扩展、按钮 Enable/CanClickWhenDisable、资源句柄/异步图片、滚动/缓冲进度、Binding AutoDetect 等存在未提交变更。新控件复用这些当前行为，不回退 HEAD，不新增同名兼容类。 |

### 2.1 相机、Renderer 与 RenderGraph

- `FrameworkScene.unity`：Main Camera 为 URP Base，stack 包含 UICamera；Base culling bits=23，UICamera Overlay bits=32（UI layer 5）；UIRoot Canvas renderMode=1。页面和遮罩同为 ScreenSpaceCamera。没有观察 Play 中动态更改，实施时必须再读实际实例。
- GraphicsSettings 及唯一 Quality 档 PC 指向 `PC_RPAsset.asset`（GUID `4b83569d67af61e458304325a23e5dfd`）；PC 默认 Renderer 是 `PC_Renderer.asset`，仅 SSAO，renderScale=1、HDR 开、MSAA=1、OpaqueTexture 开。Mobile_RPAsset 有独立 Renderer，无 Feature，renderScale=0.8、OpaqueTexture 关，当前未成为已测的移动质量档。
- URP17.5 本地 `Runtime/Settings/RenderGraphSettings.cs` 的 `enableRenderCompatibilityMode` 已标为自 6000.4 不再使用且返回 false；GlobalSettings 只存 RenderGraphSettings 版本。**不设计关闭 RenderGraph 的接线，不以旧 Compatibility 样例为主线。**
- 本地 `UniversalRendererRenderGraph.cs` 的透明段之后才执行 AfterRenderingTransparents 范围的自定义 Pass；`UniversalRenderPipeline.cs` 会拒绝不兼容 Renderer 类型的 stack。这支持按相机段插入采样的设计判断，不是已证明 Canvas 分流正确。S1 须以颜色/文字/帧编号测试图核对实际 Pass 顺序。

## 3. 归属、交付与当前草稿保护

| 所有者 | 拟新增/调整位置 | 责任 |
| --- | --- | --- |
| 框架 UI | `Packages/com.ember/UI/Runtime/Backdrop/`；既有 EUIManager/Page/ViewEngine/BgMaskPool/PageContext | 管线无关的效果枚举、设置、解析器、采样租约、页面/遮罩会话、排序边界及释放；不引用 Game.UI 或 URP。沿用必要 Manager，不新增 GlassManager。 |
| 框架 UIExtension | `UIExtension/Runtime/Behaviour/EUIGlassImage.cs`、`EUIBackdropGroup.cs`；`Runtime/Resources/SoftGlass/`；相关 Editor/Binding 文件 | MaskableGraphic、圆角/边光、通用渲染组声明及配置编辑；沿用 UI 组件/输入语义，Editor 不进入 Runtime。 |
| 可选 URP 后端 | `Packages/com.ember/UIRendering/Runtime/URP/` 与 `Editor/`，独立 asmdef | 引用 UI + URP/Core Rendering，实现采样接口、RendererFeature、相机段宿主和诊断；UI/Resource/Core 不反向引用它。URP 包条件通过 asmdef versionDefines/defineConstraints 检查，当前目标是 URP17，其他管线只保证核心纯色回退，不能宣称已兼容。 |
| base 共用 UI | `Assets/Game/UI/Runtime/Module/UIFoundation/`；`Assets/Game/UI/Editor/UIFoundation/SoftGlassUILibraryBuilder.cs` | 新 Page/Item 的业务 partial、对话 facade、GM presenter/操作登记、EUI 中心制作回执。Business 模式，输出 Module/UIFoundation/Prefabs，旧 Common 路径不动。 |
| base 资源/接线 | `Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/`、`Material/SoftGlass/`、`Resources/Config/UIFoundation/`；`Assets/Ember/Editor/UILibrary/SoftGlassLibrary.asset` | 可复用皮肤库和 runtime profile。拟配置文件 `EUIAppearance.asset` 与 `EUIBackdropSettings.asset`；Shader 用包引用，运行 RT 不做磁盘资源。 |
| base Renderer 接线 | `Assets/GameResource/Settings/UIFoundation/SoftGlassUI_Renderer.asset`（非 Resources）；项目 RPAsset rendererDataList 与场景/启动接线 | 专用 UniversalRendererData，纯 UI 用途、不复制 SSAO。通过 Unity API 创建/添加 Feature，登记实际 Renderer index；不硬编码 index 或层编号。RPAsset/TagManager 是项目设置，模板是否接管由中心预览确认，不能假定随 Assets 自动部署。 |
| 玩法模板 | 当前 topdown 只作集成宿主；未来各派生维护自身启动增量/父同步 | 不改 Player/HUD/关卡/输入业务，不为风格新增玩法共同父模板。 |
| 正式说明 | 包 `Documentation~/ui/soft-glass.md`、`popup-backdrop.md`；base `Assets/Game/Documentation/base/soft-glass-ui.md` | 分别描述公共机制与业务库/GM 接入。最终任务记录按 handoff 归档。当前阶段只写临时方案。 |

安全实施顺序：

1. 备份**当前待改文件及 .meta**、EUI 配置/注册/场景/RPAsset/TagManager、正式模板身份和已开场景状态；保存逐文件 SHA256 清单。原 962 文件清单是指纹，不是可恢复备份。另记录全 Assets 与模板快照指纹，区分本任务和其他任务。
2. 不切换模板，先实现包机制及 S1；新 UI 在当前活动 Assets 以新增 Business 资源制作为 **base 待回流集成草稿**。允许在继承场景验证通用能力，不得将其保存为 topdown 专属交付。制作器只处理本任务新资源、拒绝覆盖同名目标，写任务回执；不调用全库“重建/美化”。
3. 当前 GameInit 最小选择接线和全局效果设置都记录为待回流 base 的补丁，保留 topdown 用户内容。未获模板操作授权时可继续新资产、集成、自检和预览，不借此停掉原范围实现。
4. **真正分发 base 前必须有正常的项目中心加载/保存授权。** 提供当前草稿备份、精确差异和恢复计划供审阅后，按授权保护 topdown（含未保存场景），正式加载 base，在活动 Assets 以中心制作入口重放/合入本任务变化，保持新资产已分配 GUID；普通保存 base。不能把顶层 topdown Assets 全量倒进 base。
5. 父级正式保存后，另按授权做 base Bump/派生父同步及 topdown 恢复，复核原 topdown 草稿和本任务差异。中心不可用或未授权时保留草稿，记录“base 未保存/未分发”；不直接写 Templates~/ParentSnapshot~，不手造 hash。普通保存、Bump、提交、tag 发布、消费升级分别记录。
6. 包升级只传播框架机制，不会自动替换消费端已复制 UI。老消费项目可按指南仅接新库和 Renderer；模板升级/部署与这条增量接线分开，不强制重部署。

## 4. 配置、优先级与兼容契约

以下是拟新增 API 名称；实施若因现有类型冲突调整命名，保留语义并在 implementation 中记录映射。

### 4.1 Popup 配置与解析

| 配置/API | 拟定契约 |
| --- | --- |
| `EUIMaskEffect` | `Inherit=0, Solid=1, Blur=2`。Inherit 仅页面级有效；全局配置拒绝 Inherit 并回默认 Blur。原 API 无删改签名。 |
| `EUIManager.PopupMaskEffect` | 新全局默认 Blur；`PopupMaskColor` 继续存在，颜色不表示效果。全局设置影响之后打开的页面，现有页面显式 `RefreshBackdrop` 才换；不在每帧重建所有遮罩。 |
| EUIBinding 新字段 | `maskEffect=Inherit`、`overrideMaskTint=false`、`backdropSettingsOverride=null`、序列化 schema；useMask/maskColor/clickMaskToClose 原字段保留。创建请求、快照的新增字段均 nullable，老调用未提供时不清空现值。 |
| EUIPage 新属性 | 页面效果 Override=Inherit、设置 Override 可空；现有 MaskColorOverride、UseMask 和逻辑钩子继续有效。Bridge 在 CreateLogic 前注入。需要按次改变的调用方，在本次 OnOpen 设置 Page 属性，解析发生在 OnOpen 后、展示提交前。 |
| `IEUIBackdropProvider.Acquire(request)` | request 包含 owner/sessionGeneration、相机/显示目标、排序边界、区域/强度及更新策略；返回可释放 lease，带状态/失败原因/纹理版本。provider 不可用时返回明确 unavailable，遮罩仍阻挡。 |
| `EUIBackdropSettings` | 管线无关 SO：质量/更新率、blurRadiusDU、降采样、tint/饱和度、回退色、实时边界预算。URP 接线独立配置 Renderer 与 layer pool，UI Core 不持有 URP 类型。 |

解析顺序固定：

1. `UseMask=false` 或 `ShouldCreateClickableMask=false` → 无遮罩。不是创建透明 Image，也不申请遮罩 lease。页面自身显式玻璃组可独立存在，不能据此错误声称所有玻璃开销为零。
2. 效果：本次 OnOpen 对 Page 的显式非 Inherit 值 → Binding 注入的非 Inherit 值 → 全局 PopupMaskEffect → 内建 Blur。
3. 质量/半径：本次/页面设置 Override → 全局 EUIBackdropSettings → 内建 Balanced。局部 GlassStyle 独立解析，不能通过“选 Legacy 控件”强制改掉 Blur 默认。
4. 颜色：显式 Page.MaskColorOverride（旧代码仍支持）→ Binding 明确覆盖 → 全局 PopupMaskColor。全屏模糊输出作为基底，tint 在其上合成；alpha=0 只表示无染色，**不关闭模糊或输入**。
5. Blur 请求但后端不可用/资源分配失败/不支持目标 → 可读 Solid 回退、保留点击策略，并在诊断显示“请求 Blur，实际 Solid：原因”；不能把回退运行截图记为 AC03 通过。

旧配置迁移：

- 新增枚举的零值是 Inherit，旧 Prefab 缺字段即继承 Blur，无需逐页勾选；不依据旧 useMask/颜色推导 Solid。旧页面默认变更是 R03 的已确认要求，要在发行说明明确。
- 老 Binding 没有颜色覆盖开关：schema=0 时保留其旧 maskColor 作为明确 tint（包括原默认黑 0.5），避免丢掉用户调色；不以运行时 OnValidate 批量重写资产。新建 schema=1 默认继承全局 tint。显式迁移工具预览旧 tint，可逐资产选择保留/采用新建议 tint，保持 GUID。
- 无 Binding 的手工 EUIPage 使用全局 Blur。已有 `MaskColorOverride` setter 仍只改染色，想回旧纯色必须显式 Solid。
- 全局“关闭模糊”对应 Solid，仍有遮罩；完全无遮罩继续用 UseMask=false。ClickMaskToClose=false 仍阻挡，只不关闭；逻辑 OnClickMask 的原覆盖优先。
- 池借用每次设置效果/颜色/回调/相机/layer/order/材质；回池先撤租约和材质纹理引用，再关对象。旧的纯色 Get/ShowBgMask 签名保留为兼容入口；Popup 统一走新 resolved-request 重载，不能让旧 overload 把新默认悄悄改回 Solid。

### 4.2 皮肤、对话与 GM 装配

- 新 `SoftGlassLibrary.asset` 只登记精确新 Prefab、真实 Slots/skin version；AutoMatch 默认关闭。保持 DefaultLibrary 原 ID、源资源和目录有效。不把 Library 的 Editor 配置当运行时可加载类型。
- `EUIAppearanceConfig`（base）：`CommonSkin=Legacy/SoftGlass`、`GmPresentation=Legacy/SoftGlass`、玻璃样式 SO。缺配置默认 Legacy，保留既有消费项目；维护工程的本任务样例配置显式设置 SoftGlass GM。Popup 默认 Blur 不由此配置决定。
- `EUICommonDialogs.ShowAlert/ShowConfirm(EUIDialogRequest)` 按当前 CommonSkin 选择生成的 GamePages 新/旧定义；沿用 EUIDialogResult 及关闭完成回调一次语义。旧 `ShowPopup(GamePages.EUIConfirmPage, request)` 仍指旧前景资产，遮罩遵从新默认。
- `EUIAppearanceBootstrap.ShowGM()` 在原 GameInit GM 调用位置运行，由配置只打开一套 GM；保留 GamePages.GMPage 直接调用与旧入口。bootstrap 是业务 helper，不新增常驻 Manager，也不因样式新增自动发现 Module。
- 运行时切换配置影响下一次打开；已有控件不全局扫描替换。GM 切换需要先关闭并释放当前 GM，再打开另一套，时间倍率不被样式切换偷偷重置。可按业务显式恢复 1x；不承诺无损实时替换任意旧 Page。
- 消费端样例：从新 Item 派生 Variant，只通过公开 slot 修改标题、图标、tint、radius token；用新示例 Page 展示静态 UILogic 子 Item 与动态 EUIItemFactory 子 Item 两种所有权，以及 ShowConfirm 调用。动态宿主绑定 RectTransform，不同时注册 UILogic。

## 5. 采样、模糊与玻璃表面

### 5.1 路线比较

| 路线 | 判断 |
| --- | --- |
| 单透明 Image、渐变+描边 | 可作降级材质，不能形成背景柔化，不能独立满足 R02/R03。 |
| OpaqueTexture / Scene Color + 局部 Shader | 不含下层透明 UI；当前 Mobile 甚至未开 OpaqueTexture。淘汰为主线。 |
| UI 全画完后抓屏/每控件 GrabPass | 会卷入前景/自身，或每控件重复捕获；旧 GrabPass 也不是 URP17 主线。淘汰。 |
| 全屏截图快照 | 能含 UI，但要在前景展示前捕获且处理等待/分辨率/叠层；可作受控降级，不作为默认实时方案。禁止每帧隐藏前景抓图导致闪烁。 |
| 相机段 + RenderGraph GPU 采样 + 共享 blur | 明确控制前后景，符合现有 Camera Canvas，代价是更多相机/层和接线。选定；S1 先验证排序、MSAA/颜色空间、跨相机纹理可用性再扩资产。 |

### 5.2 帧顺序与 UI 分段

概念顺序（设计图，不是运行证据）：

```text
世界 Base（原后处理）
  → UI 段 C0：背景、HUD、低排序旧/新 UI
  → 捕获/模糊 B1
  → UI 段 C1：Popup1 的 mask + Popup1 前景/本组玻璃
  → 捕获/模糊 B2
  → UI 段 C2：Popup2 的 mask + Popup2 前景/本组玻璃
  → 捕获/模糊 B3
  → UI 段 C3：GM / 悬浮入口（按其真实 order）
  → 原有最终封屏/Loading/系统覆盖（保持最高优先级）
```

- **边界按最终 Canvas 合成排序建图，不按 PageType 猜层。** 使用 sortingLayer 的实际顺序、sortingOrder、确定的同序 tie-break（既有层级/sibling 规则）。组内保持原 order，mask 仍在自身 Page−1，系统 quit curtain/Loading 不可被新 GM 压住。不同 Camera/display 的请求不混用。
- Popup 的 mask 和正文归同一前景段，采样在整个段之前。每个 Popup 单独边界，所以上层背景包含下层 Popup，当前 Popup 自身保持清晰。一个 Page 内多个卡片/按钮默认同组，共享“该 Page 以下”的 B；不许每个玻璃控件创建相机或 RT。
- `EUIBackdropGroup` 声明页面/业务子树的 backdrop 边界；首批同组的卡片相互不折射/模糊，嵌套底色仍按正常透明合成。若一个局部前景需要捕获本 Page 的更低内容，作者明确拆出独立 Canvas/Group；不宣称自动支持任意同 Canvas 像素顺序反馈。
- 原 UICamera 复用为最低 UI 段，新增相机是它的 Overlay 派生（transform/projection/pixelRect/planeDistance/CanvasScaler 映射一致），按排序插入 Base stack。所有 UI 段关闭后处理、AudioListener、非 UI culling；世界仅渲染原世界层。
- 为确保不重绘/串入，安装时预检并分配**空闲 Unity layer 池**，最低段可沿用 UI=5，其余段映射到空闲层。层索引取真实 TagManager，不硬写 28/29/30/31，不覆盖现有层。runtime 只对受管 EUI 实例及新增节点应用段 layer 和 worldCamera，记录原值并在归还/关闭效果时恢复；不改磁盘 Prefab layer。任意业务自定义层/碰撞语义的子树须显式适配或报告不能接管，不静默重置。
- 每段只渲染自己的 layer，Base/其他非 UI Camera 排除所有池层；所有 UI Camera 的 renderer 类型兼容 Base（当前均 UniversalRenderer）。嵌套 Canvas/overrideSorting、GraphicRaycaster.eventCamera、阻挡对象、动态 Item 的新子节点都要随组同步；段Camera的depth保持相同，绘制先后由URP stack列表决定，使EventSystem可以继续按Canvas sortingLayer/order比较命中。若具体uGUI版本仍有优先级差异，使用统一raycaster排序适配并定向验证，不能以提高camera depth覆盖EUI逻辑排序。
- 浮点入口仅自身圆形命中区域可拦截；全屏 FreePage 根 CanvasGroup 不用全屏透明 Graphic 截输入。展开 GM 将它作为更高的独立模态内容组并有关闭按钮/ESC；折叠不申请全屏遮罩。

### 5.3 RenderGraph / Renderer 实际接线

1. 新后端提供 `EUIBackdropRendererFeature`，通过 Unity API 在专用 `SoftGlassUI_Renderer` 加入。将它加入当前 PC_RPAsset 与待验证 Mobile_RPAsset 的 rendererDataList，并用查得 index 指定 UI 段 Renderer，保留原 PC SSAO/世界 Renderer 不变。消费端由“检查/装配背景效果”预检展示目标 RPAsset、相机与空闲层，再应用其自有设置；缺接线明确诊断。
2. Feature 仅对当前已登记、有消费者的 UI 段生效；排除 SceneView/Preview/Reflection/未登记 Camera。在该段 **AfterRenderingTransparents** 取得 `UniversalResourceData.activeColorTexture`（含此前世界及当前段 UI），供下一段使用；不能在目标 Popup 全画完后再采自己。
3. 使用 RecordRenderGraph、显式 read/write 依赖和 Blitter/RenderGraph blit API；不同源/目标，不原地边读边写。可复用的结果是宿主持有的 RTHandle，ImportTexture 导入每个需要的图，跨相机/帧保持到 lease 释放；本帧 scratch 用图内临时纹理，不能缓存 TextureHandle 跨帧。跨相机消费者要显式声明导入读取；uGUI 使用的输出 pass 不被错误裁剪（对真正外部消费者的输出明确声明副作用/必要的 culling 设置并记录理由）。

   下一UI段在BeforeRenderingTransparents登记其将采样的所有外部输出，并在GPU执行顺序上发布纹理绑定；不在Record阶段反复改一个共享全局纹理，造成所有camera最后读到同一个值。各组材质缓存按backdrop context区分，冻结深层保留自身输出。需要全局发布时使用SetGlobalTextureAfterPass及对应依赖；本地17.5 DrawObjectsPass已调用UseAllGlobalTextures(true)，仍须在S1证明实际Canvas材质与全局/导入依赖都正确，不能用这个源码调用代替实际画面。
4. 在抓取段必须有可采样中间色目标；检查 isActiveTargetBackBuffer。当前 default Intermediate=Auto 不作为保证。安装的专用 UI Renderer 按实际17.5 API配置中间纹理策略，且首段采样点必须早于最终 resolve。若仍是 backbuffer 则报告无有效源并回退，不能读黑图。需 MSAA resolve 时使用对应安全采样，输出 MSAA=1、无深度、无 mip，记录 renderScale/UV scale-bias/y-flip。
5. 屏幕 UV 从实际 pixelRect 和归一化屏幕坐标计算，不用控件主纹理 UV。边缘 Clamp，半径换算考虑 canvas.scaleFactor、renderScale、downsample 和 RTHandle scale，横竖屏一致。颜色在线性空间模糊，UI tint 依项目色彩空间合成，输出不重复 gamma/tonemap。
6. 首批模糊使用降采样后 separable Gaussian（对称归一化权重，9-tap 可合并双线性采样）；Balanced 为 1/4 长宽、2 组 H/V；Local 与 Popup 用同一个源及设置时共享结果。不同强度按有限档位缓存，不因每个控件 float radius 产生一份 RT。Dual Kawase 对大半径更省但增加金字塔/小字光晕调参，本轮先不用它替代可测的 H/V；将来换核需复验同等视觉半径。
7. S1 是硬门槛：确认该截点包含 ScreenSpaceCamera 下层 UI、当前组排除、同帧 B1→C1 与 B2→C2 可见、最后 resolve 顺序、HDR/Linear一致。未通过先修后端，不批量制造“效果看似完成”的 Prefab，也不能把默认改回 Solid 作为解决。

### 5.4 多 Popup、更新与生命周期

- 相同 camera/display/边界代次/质量/强度的请求引用同一输出；Popup 与自身玻璃面可复用遮罩前的 B，表面不会采已经模糊的全屏 mask 再加一次 blur。更上 Popup 则有新的源与 B，真实捕获下层最终显示。
- 默认 Live：按不受 timeScale 影响的帧更新；Balanced 可限制 30Hz，质量 High 跟随显示帧，Low 15Hz。输入/文字始终逐帧清晰。打开、移层、下层显隐、场景/相机变化、分辨率/renderScale/方向改变均强制失效和更新；GM timeScale=0 时仍工作。
- 一次展示事务先取得 mask 输入阻挡，准备边界，等第一份有效输出后显示前景；正常允许最多两个呈现帧的准备。加载/取消/销毁带 sessionGeneration，迟到纹理不能回写已复用池对象。超时明确 Solid 过渡/诊断，不显示旧尺寸纹理或裸黑帧。
- FullScreenPopup 的首帧在路由隐藏下层**之前**建立采样屏障，准备完成才提交隐藏。其下层不再实际渲染后，保持这份背景快照，直到关闭/显式新数据刷新；不为了“实时”偷偷恢复隐藏页面的输入或 OnUpdate。这是现有 FullScreen 语义对应的受控快照，不可据此忽略普通 Popup 的实时要求。
- 默认实时边界预算建议 4（不含世界）；相机/空闲层池的实际容量安装时确定并显示。超预算时保留最高实时边界，深层结果冻结并显示降级计数；新边界先借临时段产出有效快照再降级，不能拿不含下层 UI 的纹理顶替。没有可用段或有效快照则该层明确 Solid。不会限制 Popup 功能数量；该降级场景单列验证，不能伪作所有深度全实时。
- 回池/隐藏/关闭完成释放所属 lease，清材质纹理与回调；同源其他消费者仍在则保留。最后消费者离开即不再 Enqueue 采样/blur，释放输出 RT、材质实例、空闲段 camera 和订阅；池对象不持有 GPU 资源。短期缓存如果保留须有显式容量/寿命，默认不保留。
- manager shutdown、域重载、场景卸载、应用退出以及初始化异常均幂等释放；还原世界 culling/原 UICamera/stack/实例层值，只撤销自己记录的变更，外部配置已变时报告冲突不全量回写。任意 lease/延迟回调不得影响新代次。
- 原 pool 的 Clear 与 active page teardown 顺序要一起检查，补充 active lease 清理通道；不能只清池中的对象而漏正在显示的 mask。

### 5.5 Glass Graphic 与裁剪

- 新 `EUIGlassImage : MaskableGraphic`，绘制圆角表面与薄边光。材质采样 B，叠加可控 tint、轻去饱和、上缘柔光和很弱内阴影；首版不做大幅折射，避免文字边缘和卡片边界泳动。无噪声纹理也能交付，不为效果强加大量位图。
- 表面使用背景合成色覆盖其内部，再以圆角 coverage 与 CanvasGroup alpha 混合；不得把“已合成背景”按低 alpha 再混一遍造成双影。遮罩全屏输出先覆盖背景，tint 再混合。前景文本/图标始终独立 Graphic。
- Shader 必须有 uGUI stencil 参数、ColorMask、UNITY_UI_CLIP_RECT/ALPHACLIP、RectMask2D softness 与 CanvasGroup 支持；手写 Universal HLSL 并保留正确 UI tags/混合规则。材质改值按 style/context 缓存，顶点携带尺寸/半径；不用 Renderer 的 MaterialPropertyBlock 假装它能直接驱动 CanvasRenderer。
- 新 Graphic 的圆角也用于 raycast coverage；玻璃 Image 的 alpha 不等于 Sprite alpha hit test。非交互装饰 raycastTarget=false；圆点与圆形按钮按实际圆形命中。
- 新圆角表面首版支持普通矩形、圆/胶囊和轴对齐缩放；不把 Sliced/Tiled/任意 Sprite 轮廓或世界空间弯曲面纳入自动玻璃支持。旧 EUIImageEx、Sliced/Tiled 和旧 RoundedModifier 按原组件工作；需要嵌套裁剪用同形 Mask 或 RectMask2D，记录不同 stencil 深度验证。多个圆角组件不能叠加裁两次。

## 6. 首批准确交付清单

所有名称为本轮新建目标；预检冲突时先检查已有资产，不删除重建。以下统一 Business、classPath=`Module/UIFoundation`，预计 Prefab 根为 `Assets/GameResource/Resources/UI/Module/UIFoundation/Prefabs/`；实际 TryBuildPlan 路径与生成回执逐项登记。Skin 库与旧库同屏可选。

| 新资产 | 角色与必要槽位/行为 | 复用关系 |
| --- | --- | --- |
| EUISoftGlassPanelItem | Item；Surface/Content/Header；可嵌套容器、统一材质参数 | 通用容器，不自建 Canvas/EventSystem |
| EUISoftGlassPrimaryButtonItem | Item；Button/Label/Icon，主操作 | 复用 EUIButtonEx，保留 Enable 与 interactable 区别 |
| EUISoftGlassSecondaryButtonItem | Item；同上，次操作 | Primary 的 Variant/同 family，明确允许覆盖 |
| EUISoftGlassIconButtonItem | Item；Button/Icon、语义标签 | 圆形快捷操作基础 |
| EUISoftGlassCloseButtonItem | Item；关闭图标/点击 | IconButton Variant，使用者接线 |
| EUISoftGlassSwitchItem | Item；EUIToggleEx/Label/Track/Thumb | 状态/禁用/Changed，复用现有 Toggle 行为 |
| EUISoftGlassSliderItem | Item；Slider/Fill/Handle/Value | 水平通用滑杆；不新增一套输入事件体系 |
| EUISoftGlassVerticalSliderItem | Item；同上，BottomToTop | 真正竖向布局，不旋转文字；GM 时间条复用 |
| EUISoftGlassInputItem | Item；TMP_InputField/Placeholder/Error | 复用 EUIInputActions，焦点/只读/错误 |
| EUISoftGlassSearchItem | Item；Input/SearchIcon/Clear | Input Variant，输入/清空可运行 |
| EUISoftGlassStandardPage | Page 结构骨架；SafeArea/Content/Header | StructureOnly，无类或注册 |
| EUISoftGlassStandardPopupPage | Popup 结构骨架；Title/Body/Actions/Close | StructureOnly；默认 Inherit 遮罩 |
| EUISoftGlassAlertPage | Popup Page；Title/Body/Accept/Close | EUIDialogRequest，关闭完成回调一次 |
| EUISoftGlassConfirmPage | Popup Page；另有 Cancel | Accepted/Cancelled/Dismissed 完整语义 |
| EUISoftGlassShowcasePage | Page；完整控件状态墙、深/浅/彩色动态背景、旧新对照、弹窗演示 | 正式可运行示例，不接默认玩家导航 |
| EUISoftGlassExtensionExampleItem | Item；派生样式/自有图标、可换文案 | 演示允许 slot、Variant 与参数扩展 |
| EUISoftGlassExtensionExamplePage | Page；静态+动态 Item 宿主/确认调用 | EUIItemFactory 所有权及清理示例 |
| EUISoftGlassGMPage | FreePage；EntryHost/CenterHost/状态与调试区 | 常驻页只负责入口与 GM 展开协调；不全屏拦截 |
| EUISoftGlassGMEntryItem | Item；圆形入口/拖拽命中/吸边 | 使用安全区归一化位置 |
| EUISoftGlassGMCenterItem | Item；Header/Close/卡片网格/ScrollContent | GM 可见内容，自己的 backdrop group |
| EUISoftGlassGMStateItem | Item；状态名/框架时间倍率/Unity倍率 | 对应旧 EUIGMStateItem 行为 |
| EUISoftGlassGMTimeItem | Item；启用 Toggle/倍率数值/VerticalSlider/1x Reset | 对应旧时间条与关闭恢复 1x |
| EUISoftGlassGMActionItem | Item；Icon/Label/Button/可选 Toggle | 退出/重置/业务扩展操作 |
| EUISoftGlassGMTestItem | Item；增强开关、ScrollRect、Image/RawImage/Circle 示例 | 保留旧 GM 测试展示，在可折叠“组件测试”区域 |

共 24 个 Prefab 目标（含 2 结构骨架、5 具体 Page、17 Item）；新类/Binding 仅按真正需要生成，不为纯装饰硬建逻辑。最终以中心回执重新核对数量和实际路径，任何增删在 implementation 解释，不能只交一张效果图。

必要非 Prefab 交付：GlassStyle SO（中性/浅实/强调；可由一个 style 的状态 token 表示）、外观与背景设置、Renderer 资产/安装预检、SoftGlassLibrary 及生成 catalog、制作回执和新旧选择指南。标准 Page/Popup 的字体从当前项目真实 TMP 字体复用，不重新生成或覆盖已有本地修改的字体图集。

### 6.1 GM 行为与扩展接口

| 旧 GM 能力 | 新界面位置与保持规则 |
| --- | --- |
| Btn_GM/Panel_GM 显隐 | 圆点打开中心、明确 Close/ESC/返回关闭；重复点击幂等；只开一个中心实例。旧 Btn_GM 仅在 Legacy GM 被选择时出现。 |
| Tgl_Test + Pgb_TimeScale | 时间卡内“启用时间控制”+竖滑杆，保留原 Slider min/max/step（实施从真实资产/嵌套源导出，不能猜成 0—2）；关闭控制立即同步两套 timeScale=1，关闭 GM 面板本身不改变既有倍率语义。 |
| Unity Time + EmberTimeManager | 同一个业务 `ApplyTimeScale` helper 赋两者，检查 manager 缺席；显示真实数值，不用动画数值假装同步。 |
| 顶层状态 | 状态卡显示 GameLauncher.Fsm.Current.Name，null 为破折号；状态变更事件可用则订阅，否则可见时低频刷新，不能扩大框架 FSM 职责。 |
| 增强 Toggle | “组件测试”与快捷 Tile 可复用一份状态，Changed 反馈/日志保持。 |
| Scroll/Image/RawImage/EUIImage/Circle | 测试区保留真实控件/当前引用，不假造原页面未实现的网络、蓝牙、亮度或音量能力。 |
| Exit | 底部圆形危险操作，走 GameLauncher.Instance.Quit 的原完整退出流程；如展示确认也必须最终走该流程，取消不退出。 |

base 新 `IGMActionProvider` 提供只读 action 描述（稳定业务 action key、分组、排序、标题/图标、Button/Toggle、可用性/状态读取、执行回调）；`GMActionRegistry.Register(provider)` 返回 IDisposable，业务 module 在自身生命周期注册/释放。它是 Game 层服务，不进 UIManager。新页面通过动态宿主使用 EUIItemFactory，关闭时归还/解绑；provider 移除不能留下捕获业务对象的按钮闭包。至少一个真实样例 action（切换展示背景/输出当前状态）和一个 Toggle，保留 Legacy 的用户钩子接入路径。

## 7. 视觉、尺寸与交互规范

### 7.1 单位、曲线与嵌套

- 使用 **DU 设计单位**，参考短边 720 DU，横屏参考 1280×720、竖屏参考 720×1280；CanvasScaler 继续现有方向适配，但新页面明确对应参考分辨率，不使用截图物理像素。DU 经 Canvas scaleFactor 转为显示像素再计算 blur/边线。
- 第一版所有圆角采用**圆弧圆角矩形的同一曲线**（直线与圆弧切线连续）；不宣称复刻小米私有超椭圆，也不把 CSS 百分比当 Unity 半径。`effectiveRadius=min(token, min(width,height)/2)`；圆与胶囊显式 shape 模式，圆要求等宽高。
- 解析 rounded-rect signed distance 作为覆盖/描边依据，基于屏幕导数做 AA；边线宽按最终屏幕最少约 1px clamp，不因降采样背景而变锯齿。

| Token | DU | 用途 |
| --- | ---: | --- |
| RSmall | 12 | 紧凑值标签、内嵌状态块 |
| RControl | 20 | 常规按钮、输入、搜索、开关行 |
| RCard | 32 | 信息/状态卡、GM 中卡 |
| RPanel | 48 | 大弹窗/GM 大容器，窄屏可降至 32 |
| Capsule/Circle | min边/2 | 搜索胶囊、竖滑杆端部、圆形快捷入口 |

- 间距以 8DU 为基数：卡间 16、面板 padding24/32、安全区内外边距至少24。嵌套优先 `innerRadius=max(RSmall, outerRadius−padding)`，允许就近 token，避免外48内48挤在一起；子内容不越过父圆角。
- 普通按钮高72DU、最小交互命中64DU；图标40/48，正文26、辅助22、标题32、倍率大值44。长文本换行/滚动而不无限缩字；点击区域经设备验证需至少约44逻辑点，DU 值不能直接宣称等于 iOS pt。

### 7.2 材质与状态

参考01/05取“背景透色+柔化+细亮边”，02取控制中心秩序，04取嵌套，06验暖色，03/07/08验背景与前景分离。

| 状态/表面 | 建议起始参数（设计值，非用户已签收） |
| --- | --- |
| 中性玻璃 | blurRadius16DU，背景饱和度0.85，浅 tint 白0.14 / 深 tint #101827 0.28，描边白0.25、上缘高光0.35，极弱阴影；按背景模式选择前景色 |
| 浅实强调卡 | 白 tint0.85—0.92，深色正文 #152033，蓝色图标 #3488FF；仍保留少量背景透色 |
| 选中/启用 | 蓝 tint0.28—0.42、明确勾选/位置/状态文案；不只靠颜色表达 |
| 按下 | tint 加深或增亮约0.08、缩放0.98（无布局挤压），80ms进入/120ms恢复，unscaled；禁用不播放可执行反馈 |
| Hover/键盘焦点 | 边光提高、独立焦点环；触摸不保留 hover |
| 禁用 | 降饱和与前景透明，但正文仍可辨；复用原 Enable/interactable 规则，不借样式绕过输入屏蔽 |
| 输入焦点/错误/只读 | 焦点描边、错误文案与图标、只读光标/背景；保留 TMP selection/caret 清晰 |

明暗模式由 profile 明确指定，示例可切换；首版不做逐控件 GPU 亮度读回自动换字色。亮/深/彩色背景下正文目标对比度至少4.5:1、大字3:1是设计检查建议，不能当未测的已达标声明。必要时增加表面 tint 至0.8以上或使用稳定的文字底，仍保留周围玻璃透色，不能一味降低文字 alpha。

### 7.3 GM 布局线框（方案说明，非实际预览）

```text
竖屏 safe area / 可滚动主内容             横屏 safe area
GM 控制中心                  [×]        标题 / 状态                          [×]
[当前状态      ][框架/Unity时间]        [状态卡 ][时间卡+竖滑杆][组件测试/扩展区]
[ 时间控制卡   ][竖时间][只读信息]       [圆快捷][圆快捷][圆快捷][退出]
[ 数值/开关/1x ][ 滑杆 ][版本/质量]      高度不足时内部滚动，标题/关闭固定
[    可扩展操作分组条             ]
[圆重置] [圆测试] [圆扩展] [圆退出]
[组件测试（可折叠；滚动/图片等）   ]
```

竖屏内容最大宽640DU，横屏最大1120DU；按可用宽>=960DU切三列，640—959两列，<640一列并把竖时间条转独立时间卡（值与手柄方向清楚）。网格列宽按 available width 计算，卡片不随屏幕被压成窄条。关闭在顶部安全区内，不放刘海/手势条。采用 EUISafeArea/Screen.safeArea 实际转换，屏幕方向、安全区或窗口尺寸变化均重算；不能只监听宽高。

入口：视觉直径56DU、命中64DU，内部小圆环；首次右侧约中上位置且距安全边12DU以上。pointerDown记录 pointerId/位置，位移超过8DU（换算屏幕阈值）进入拖拽，drag 后即使 pointerUp 也禁止 click；只接管起始于圆点的单指，第二指不抢占。松开按安全区最近左右边吸附，约180ms unscaled，允许 reduced-motion 直接归位。保存左右边+归一化Y，方向/安全区变化 clamp；不可半藏到屏外。展开可淡出入口或将其作为关闭辅助，永远保留清晰 [×]/ESC，不依赖猜手势。收起仅圆内拦截，接近重要 HUD 时可由消费端传禁入矩形作位置修正。

## 8. 质量、资源与测量预算

参数有限范围并校验 NaN/负值：radius 0—40DU，tint/强度0—1，downsample 1/2、1/4、1/8，H/V iterations1—3，更新策略 Live/Throttled/Snapshot/Off，maxLiveBoundaries>=1；半径0允许有透色/表面但不做 blur kernel。Off/Solid 不执行采样工作；本页仍有其他玻璃消费者时按实际消费者计数，不能只看遮罩配置。

| 档 | 分辨率/迭代/更新 | 用途与降级 |
| --- | --- | --- |
| High | 1/2长宽，2组H/V，随显示帧 | PC 高质量对照；先测再推荐给设备 |
| Balanced（拟默认） | 1/4，2组H/V，最多30Hz | Popup 与通用玻璃共享；移动适配起点，不是移动达标承诺 |
| Low | 1/8，1组H/V，最多15Hz | 大模糊细节减少；清晰前景不降低更新率 |
| Snapshot | 打开/边界显式失效时捕获 | FullScreen隐藏背景/深层预算回退；不周期隐藏前景抓屏 |
| Off/不支持 | Solid与稳定tint | 无采样/blur RT，UI 可操作；显示实际回退原因 |

建议测量环境：当前 Unity6000.5.4f1/URP17.5、PC Renderer接线、Linear/HDR按实际Graphics设置、1920×1080和1080×1920、renderScale1、MSAA1，VSync/目标帧率固定记录；再覆盖Mobile rendererScale0.8和一种MSAA目标。测量前记录OS/CPU/GPU/驱动/API、分辨率、Editor或Player、Development/Profiler状态、当前模板、文件指纹。硬件/图形API目前未知，不假造。

对照场景固定动态彩色背景+HUD文字，A=Solid/无局部玻璃，B=单Popup+12同组控件，C=两Popup+GM，D=全部关闭；记录 Main/RenderThread CPU、GPU ms、capture/blur标记次数、drawcall/相机数、RT数量/实际字节和持续GC。先预热30帧再采180帧/组，记录中位/P95，连续打开关闭20次后资源回稳；这些是拟定短定向测量，实际若环境预计耗时较长，先说明范围/耗时并获取相应授权，不拆批规避。没有GPU计时就注明不可获得，不能用FPS倒推精确GPU开销。

**建议预算（未获用户确认，不作验收硬阈值）**：单边界 Balanced 增量 CPU中位<0.5ms、GPU中位<1.5ms；稳态托管GC=0B/帧；关闭后 capture/blur=0次；1920×1080 Balanced 自有 RT目标<=8MiB（不含URP原有颜色/深度）。RGBA16F的1/4输出约0.99MiB/边界，4边界输出约3.96MiB，再加可顺序共用scratch约1—2MiB；实际格式/额外resolve/纹理对齐和URP中间目标要实测，不能以这个算式宣称显存达标。多个相机的额外CPU成本必须纳入，超预算优先降更新率/分辨率/深层快照，不能漏掉下层UI。

真机型号、温控/功耗、低端预算及正式产品帧率未指定。实施给可复核PC数据与现有限制；真机/构建未做记待验证。没有用户决策不得把建议预算改写成“用户接受”，也不能用这一未知阻止既有PC功能实现。

## 9. 分步实施与定向验证

### S0 基线与生成前置

复核当前工作区、相关指纹和正式模板/扩展；读编码规范/API速查、完整 unity-validation、ember-eui-build 和 unity-mcp-orchestrator。备份真实待改内容，导出旧 GM/EUI 契约与旧资源GUID、嵌套Time Slider范围，形成 `evidence/baseline-implementation/`。不修复其他扩展配置。新建/目标冲突做EUI预检，记录sourceSkeleton与输出。

### S1 后端接线与第一份真实画面（先于批量UI）

实现最小UI core接口/URP backend、两个独立UI段+单mask+简单玻璃面；通过MCP本轮编译/域重载/Console后运行有界场景。测试图包含动态世界色块、下层HUD帧号、当前Popup独特文字和一个上层标记。Frame Debugger/RenderGraph Viewer与截图证明源纹理含下层、排除本层/更高层、下一段读到本帧输出。验证第二Popup、相机数组/renderer index、MSAA/UV/Linear；失败定向修复，不跳到“用透明Image交付”。

### S2 遮罩策略、迁移与释放

贯通Binding/CreationRequest/快照/Bridge/契约，新增枚举零值兼容和schema规则；池会话接入、FullScreen首次屏障、取消/迟到/回池/销毁和分辨率处理。相关EditMode测试解析矩阵（无字段/继承/显式Solid/Blur/UseMask/逻辑拒绝/tint），快照往返保留，lease幂等与代次拒绝。Play验证点击关闭/不关闭、连续开关20次、双Popup反序关闭、退出与重新初始化。不要全量测试UI程序集。

拟新增定向过滤：EUIBackdropPolicyEditTests、EUIBackdropLeaseEditTests、EUIGlassGeometryEditTests，以及EUIDevelopmentCenterEditTests中新增的遮罩字段创建/快照往返用例；UI渲染与输入在小型Play验证场景做，不用只镜像setter的测试代替。碰到既有失败先记录其是否本轮引入，不擅自扩大到整库修复。

### S3 通用库与公共弹窗

经EUI中心制作清单的通用控件/骨架/Alert/Confirm/Showcase/扩展样例，登记精确库slot并生成catalog。新增Graphic验证SDF圆角/Mask/RectMask2D/CanvasGroup/各种状态；复用当前按钮/Toggle/输入行为。读真实Binding后写业务partial。生成两次并比较用户区/注册/引用不丢失；旧库/GUID/入口原样可用，展示新旧并存、CommonSkin切换与独立遮罩策略。

### S4 GM 与悬浮入口

新GM Business Page/Items、业务presenter/action registry、bootstrap最小接线；逐项对照旧功能，不改旧受管区。验证Time控制开关/1x恢复、两套时间同步、状态空值/更新、增强开关/图片/滚动/退出流程及至少两种消费扩展操作。鼠标自动行为自检与真实触摸人工体验分别记录。收起时在圆点外点击游戏，拖动阈值/吸边/旋转/安全区/ESC/关闭/暂停下动画逐项验证。

### S5 视觉、短性能、自检与预览

深/浅/彩色动态背景；横竖至少1920×1080、1080×1920并加窄屏720×1280；双Popup、Mask裁剪和降级；按第8节记录可得CPU/GPU/RT。每批代码完主动MCP编译，确认本轮域重载/新程序集证据及Console后才测试。MCP断连按完整规范：检查待编译、尝试触发，无法送达立即提示手动编译；结果不明不重复；30秒间隔最多10次重连，耗尽后停依赖工作，明确未验证。

写 implementation.md、逐项结果/证据/偏差和当前文件指纹；更新handoff/04提示词。模板操作没有授权则保留第3节待办，不编造base保存。不能把未执行真机/人工签收/独立验收写通过。

### S6 正式模板与独立验收

获单独模板授权后按第3节完成base归属与中心保存，恢复topdown并复核；若未执行，AC11的正式分发边界保持待办，独立验收不得因此宣布整个任务完成。另一聊天读取需求和当前实现独立验收AC01—AC13；失败回实施。所有必需项满足且用户确认当前实现预览之后，回入口归档清理。本方案聊天不创建后续聊天。

### 9.1 R/AC 到步骤与证据映射

| AC | R | 实施与定向验收 |
| --- | --- | --- |
| AC01 | R01/R07 | S0/S3/S4：旧GUID/路径/注册/用户钩子对照，旧新库同屏、Legacy/SoftGlass选择、旧GM直接调用；Popup默认Blur独立检查。 |
| AC02 | R02 | S1/S3/S5：源RT与最终画面对照，深浅彩动态背景，边光/圆角/文字清晰；Live/Throttled/Snapshot按声明验证。 |
| AC03 | R03 | S1/S2：未设字段旧Popup、无BindingPopup、新Popup默认Blur；逐页Solid覆盖全局Blur、逐页Blur覆盖全局Solid、UseMask=false；世界+下层HUD模糊，前景排除。 |
| AC04 | R03 | S2/S5：点击策略/逻辑拒绝、两Popup叠层与关闭、FullScreen隐藏屏障、取消/迟到/20次回池、分辨率/相机变化、关闭0采样与资源回稳。 |
| AC05 | R04 | S3/S5：token/曲线/夹限，横竖与小尺寸，无锯齿拉伸，圆胶囊、Mask/RectMask2D嵌套/软裁剪；旧Simple/Sliced边界明确。 |
| AC06 | R01/R02/R04/R07 | S3：24目标逐项回执/正式Prefab/Binding/参数/适用状态，生成两次稳定；扩展Variant、静态/动态所有权样例实际运行。 |
| AC07 | R05 | S4/S5：图02对照控制中心网格、信息/时间卡/圆快捷区，窄/横/竖安全区不遮关键操作，长内容可滚动。 |
| AC08 | R05 | S0/S4：旧GM功能表逐项操作，Time两套同步/禁用复1x、状态、增强测试/展示、退出、action provider样例/释放及旧GM选择。 |
| AC09 | R06 | S4/S5：鼠标与触摸点击/拖拽分离、吸边、关闭/ESC、旋转后位置可见、圆外输入不阻挡、timeScale0动画。触摸真实手感需人工。 |
| AC10 | R02/R03/R07 | S1/S5：安装诊断、Off/后端缺席/超预算回退、RT释放/无持续采样，明确环境CPU/GPU/RT记录；真机不虚填。 |
| AC11 | R01—R07 | S0/S3/S6：包/Assets依赖边界、EUI中心回执、受管来源与生成稳定、快照零手改、topdown保护；正式base中心保存完成与否分别记录。 |
| AC12 | R01—R07 | S1—S5：本轮MCP主动编译/域重载/Console、过滤到新增/相关用例及行为证据；另一聊天独立验收，人工/构建边界分别写。 |
| AC13 | R08 | S5与回入口：第10节实际实现预览、显示基线与用户明确确认；变化重出受影响图，未确认不收束。 |

R01在S3/4交新旧并存；R02在S1/3交真实材质；R03在S1/2交默认/覆盖/输入；R04在S3/5交统一曲线；R05/R06在S4交GM与入口；R07在S3/6交例子和说明/分发状态；R08在S5及收束前交预览与确认。无需求被延期或静默缩小。

## 10. 实现预览与确认记录

保存到任务 `evidence/previews/<revision>/`，至少：

| 文件 | 必须是当前运行实现的内容 |
| --- | --- |
| controls-dark.png / controls-light.png / controls-color.png | 通用控件、适用交互状态、真实背景透色与边缘、字号/嵌套 |
| popup-stack.png | 场景与下层HUD/Popup模糊，上层正文按钮清晰；附对应sourceRT或FrameDebugger截图 |
| gm-portrait.png / gm-landscape.png | 完整GM控制中心及真实状态/倍率，安全区和长内容 |
| gm-entry-collapsed.png | 收起的悬浮入口；必要时短录屏演示拖拽吸边/圆外输入 |
| preview-manifest.json | 日期/环境/分辨率、GitHEAD、相关未提交文件SHA256、制作回执和截图场景/配置；只记真实值 |
| preview-confirmation.md | 展示的revision/图片路径/聊天来源、用户逐字或准确引用的明确确认、需调整点/重出范围 |

实施聊天把实际图片嵌入聊天供用户看（不是只给目录），标明“当前实现预览”和待验证设备。允许先收视觉反馈再独立验收，也可独立验收后回入口确认，但收束前二者都不可缺。用户未回复、只确认方案、旧版截图确认、自动测试通过都不等于当前视觉确认；状态保持“待用户预览确认”。用户要求修改则“需调整”，实现视觉变化重新出受影响图并确认，不能沿用旧签收。

本阶段只有文字线框/渲染顺序示意，没有实际实现图，**不向用户请求最终视觉签收**。

## 11. 风险与必须如实保留的边界

| 风险/未执行项 | 处理与完成条件 |
| --- | --- |
| URP跨Camera段采样/uGUI实际顺序未跑 | S1优先实证；来源只是当前源码和官方API，不宣称后端已验证。若API限制改变设计，回写方案偏差并保持R03，不能交场景-only blur。 |
| layer池/相机成本、自定义UI层冲突 | 安装预检、可配置空闲层、明确诊断和回退；不占用别的业务层、不递归改产品碰撞逻辑。 |
| FullScreen提前隐藏/延迟取消/quit curtain | 展示屏障与代次、保留系统封屏排序；定向回归，不改加载/退出事务含义。 |
| 当前topdown草稿与base分发 | 先完成集成草稿；正式base加载/保存及父同步是单独操作门槛，未经执行保持未分发，不要求用户重复授权原范围代码实现。 |
| 扩展身份0.1.1/0.1.2不一致 | 只记录与隔离，正式工具若拒绝则报告该具体前置；不手改绑定版本/hash，不替其他任务签收。 |
| 旧GM头注释与生成行为不一致 | 保留旧生成源；新GM用Business，不在通用Framework.tpl注入特例。生成二次检查用户逻辑不丢。 |
| 图形平台/手指体验/安全区/IME | 真机触摸、刘海/手势条、输入键盘覆盖与设备旋转必须人工/设备验证；自动鼠标模拟不替代。 |
| 性能/构建/XR/多显示器 | PC数据可交；真机温控功耗、Player构建、XR、多显示器和其他管线未覆盖。发现未满足必需项不得记通过，涉及扩大验证范围按授权处理。 |
| 用户视觉签收 | R08/AC13硬条件；没有当前实现确认不标任务完成、不归档清理。 |

没有需要用户重复回答的A01/A02或Popup默认偏好。当前没有阻止开始包机制与新增Assets实施的需求缺口；模板正式操作授权与真机环境是具体后续门槛，不把它们伪装为方案已执行结果。

## 12. 技术依据

本地源码优先，尤其是当前URP17.5包与EUI生成器行为。下列Unity官方文档用于确认公开机制，文档示例不代替本工程17.5的S1验证：

- [Camera stack与layer/culling配置](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/camera-stacking.html)：Base/Overlay按顺序合成，共享目标；本方案相机分段是据此与本地实现作出的工程设计。
- [RenderGraph编写pass与纹理读写](https://docs.unity.com/en-us/engine/6000.0/manual/render-pipelines/universal-render-pipeline/customizing-urp/render-graph/write-render-pass)：显式依赖，使用当前color资源，分别分配目标。
- [导入跨帧/跨Camera纹理](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/render-graph-import-a-texture.html)：ImportTexture与外部资源所有权；导入纹理由宿主释放。
- [Compatibility全屏blit说明](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/renderer-features/how-to-fullscreen-blit.html)：新效果优先RenderGraph；旧样例不作为本轮关闭RG的依据。

下一阶段使用 [实施提示词](prompts/03-implementation.md)，先完成S0/S1，随后按清单逐项交付。独立验收仍由另一个聊天完成。
