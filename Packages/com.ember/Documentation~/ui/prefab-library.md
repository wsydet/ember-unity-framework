# 公共预制体库

入口：`Ember/UI/UI 开发中心 → 公共库`。`EUILibrary` 是维护源，默认位于 `Assets/Ember/Editor/UILibrary/DefaultLibrary.asset`；`catalog.json` 是可重新生成的只读目录。默认资源随 base 分发，不依赖 EditorPrefs 或个人缓存。

## Odin 公共库浏览器

左侧按 Category 分组，用搜索框按文字匹配稳定 ID、Family、Prefab 名或别名，右侧展示所选资源预览和只读详情。匹配/覆盖范围和公开槽位分组折叠，槽位每页8项，避免37个条目全部展开。刷新按稳定 ID 保留选择；窗口关闭释放详情 PropertyTree。浏览不直接修改配置或 Prefab 源。

顶部保留刷新、配置/槽位验证和语义目录生成；详情提供打开 Prefab、定位资源和查询引用方。目录状态来自真实依赖摘要，配置或资源变化后刷新检查。需要修改配置时打开配置资产 Inspector；正式创建、绑定和代码生成仍走下面的公开服务。Odin 仅优化浏览与诊断，不替代维护源或引入额外配置副本。

## 创建与复用

创建页选择源骨架、内容槽位、角色和生成模式，先检查计划，再创建。公开 API 为 `EUICreationService.TryBuildPlan/Create`，请求新增 `SourceSkeleton`、`ContentSlotPath`、`StructureOnly`、`InheritBinding`、`ReferenceResolution` 和 `MatchWidthOrHeight`。没有源骨架的旧请求仍有效；有源骨架时保存真实 Prefab Variant，保留源布局/Canvas 配置。

base 骨架为 EUIStandardPage / EUIStandardPopupPage，内容路径均为 `Animator/EUISafeArea/Center/Content`。骨架不生成类或注册页面；创建具体 Page 时取消 StructureOnly，生成独立逻辑类，按需在用户代码接线。弹窗 Close 入口的可运行例子见 EUILibraryPopupPage，调用 `EUIManager.Instance.ClosePage(Page)`；遮罩配置在 Binding 上。

资源继承与 Binding 继承独立。结构骨架不得选作 Binding 基类；有真实逻辑类的 Item 可显式 InheritBinding。源字段从快照解析到 Variant，生成器不重复声明继承字段；缺失字段、类型不符、错误角色、链循环会阻止生成。修改绑定使用 `EUIBindingEditorUtility`，然后 `EUIBindingCodeGenUtility.TryRegenerateCode`，不要手改 Binding.cs。

`EUILibraryService.Instantiate(library,id,parentRect)` 在可编辑实例中嵌套精确 Item；Page 应走创建服务。`ResolveInstanceSlot(library,id,instance,slotId,property)` 验证源、实例边界和允许属性后返回实例组件。调用方使用 SerializedObject/Unity API 修改该组件，并记录 Prefab override；该 API 不会修改源。

## 身份与目录

每条配置包含稳定 ID、Prefab 引用、family、类别、别名、AutoMatch、参考尺寸、必要角色、允许覆盖及真实组件 slots。槽位记录属性、状态语义和组件类型；导出身份为 GUID:localId。默认 AutoMatch 全部关闭。

`Validate` 检查重复身份、失效/越界槽位、类型与属性、必要角色、Prefab 依赖循环。`ResolveExact` 精确查找并验证；`Match` 只接受未过期目录、显式允许的 family/roles，多个候选保持数组，不猜测。`GenerateCatalog/IsCatalogCurrent` 使用配置和资产依赖摘要。目录过期不禁止经过验证的精确引用。

`FindConsumers(entry,root)` 查找 Prefab 依赖方；开发中心可查询引用方并打开资产。`EUIContractService.Export` 的 `libraryEntries` 导出实际依赖的 family、皮肤、允许覆盖及槽位；差异归 `library-contract`，默认阻断自动应用。

EnableState 与 Selectable.interactable 保持不同语义。内容宿主仅提供 ScrollRect、裁剪和布局，动态创建/回收、取消、池和虚拟列表不属于本接口。默认皮肤仅为版本化资源与配置，不是完整主题切换器。

## base 默认组件（S2 扩充）

默认皮肤1.1.0的目录包含36项可复用资产和1项展示页。按钮提供主/次/危险/图标/关闭/返回/数量样式；选择提供Toggle、Switch、Tab、TabGroup、SegmentedControl和固定选项Dropdown；输入提供TMP Input/Search、Slider与Stepper；展示提供设置行、标题、徽标、图标数量卡片、空态、Toast与锚定Tooltip；布局提供纵向/横向/网格ScrollRect宿主；页面提供标准/Popup骨架及Alert/Confirm。

扩充预览用 `EUIManager.Instance.ShowMainPage(GamePages.EUIExpandedLibraryPage)`，不接默认导航。默认弹窗使用 `EUIDialogRequest`，在页面完成关闭时回调 Accepted / Cancelled / Dismissed。上述 Game.UI 类型属于base默认资源配套代码，不是框架包反向依赖。

通用行为：`EUINumericControl.Configure/SetValue/Changed`、`EUIInputActions.SetReadOnly/SetInteractable/SetError`、`EUIBadgeView.ShowDot/ShowCount/ShowLabel`、`EUIIconCardView.SetContent`、`EUITimedNotice.Show/Hide`、`EUIAnchoredTip.ShowAt/Hide`、`EUIStaticChoiceMenu.SetOption/Select/Open/Close/Changed`。默认下拉预置3项；增减容量在Prefab阶段编辑，运行期不创建/回收选项。短提示面板尺寸由Prefab决定，调用方需限制长正文。宿主与标签演示仅操作预置子物体，不包含S3动态容器和池。

S3 增加独立的纵向动态列表 Variant 与使用周期缓存，见 [动态列表](dynamic-lists.md)。原 S2 三个宿主仍保留结构职责，不自动获得运行时数据接线。
