# 默认公共 UI 库

皮肤：EmberDefaultLight 1.1.0；参考分辨率 1280×720，CanvasScaler 按宽高匹配。浅底、蓝色主操作、细边框、圆角九宫格与轻阴影；文本使用 TMPEx 动态文本。

源资产位于 `Assets/GameResource/Resources/UI/Common/Prefabs/`：EUIStandardPage、EUIStandardPopupPage、EUIActionButtonItem、EUIChoiceToggleItem、EUIProgressItem、EUIContentHostItem。配置在 `Assets/Ember/Editor/UILibrary/DefaultLibrary.asset`，入口为 Ember/UI/UI 开发中心 → 公共库。

`UI/Module/UIFoundation/Prefabs/` 下 EUIOptionRowItem 提供图标、标题、选择状态与 SetContent。EUIOptionRowVariantItem 同时演示资源继承和 Binding 继承；EUILibraryPreviewPage / EUILibraryPopupPage 分别为两个页面骨架的真实 Variant。base 的主菜单、设置、玩法、背景、加载、GM 及示例页面均由公共库组件组成；原页面 GUID、Framework 标记和导航保持；Loading 的条／数字绑定已收拢为专用进度条逻辑绑定。示例不接入默认导航。

公共库目前登记 44 项。S8 补齐 EUITextItem、EUIImageItem、EUIRawImageItem、EUICircleProgressItem、EUIFreeScrollHostItem、EUITransitionSurfaceItem、EUIContinuousSliderItem。它们是 StructureOnly Item，不创建额外的用户逻辑或 Canvas；由宿主绑定具体控件。组件源内部仍使用 Unity UI/TMP 作为实现基础。

GM 入口继续显示 FPS 并支持拖拽，面板包含时间缩放、当前状态、双向滚动和增强控件预览。连续滑杆直接绑定 Slider，不使用带步长量化的 EUINumericControl；关闭时间控制会恢复 1×。加载页嵌套 `UI/Module/SceneFlow/Prefabs/EUILoadingProgressItem.prefab`，它是公共 `EUIProgressItem` 的真实 Variant。专用 Item 负责 Slider 填充、百分比文本、独立条／数字开关和复用重置；Loading 只传入经过准备状态约束的进度与显示开关，继续负责假进度时序、CanvasGroup 渐变与转场。页面不再绑定或格式化独立百分比文字。全屏背景使用 EUIImageItem 的空 Sprite 实色覆盖，不能套用圆角图片导致四角漏出底层。

维护时修改加载后的 Prefab，使用公开 Binding API 生成，再通过项目中心普通保存。对嵌套组件的文字、颜色等覆盖应调用 RecordPrefabInstancePropertyModifications 并重新加载核对；不能仅凭编辑中的对象判断保存成功。旧 PSD/BUI 回执是历史记录，继续换肤前从当前契约重新预检，不重放旧摘要。

运行示例：`EUIManager.Instance.ShowMainPage(GamePages.EUILibraryPreviewPage)`；Continue 打开弹窗，× 或遮罩关闭；Toggle 展示 0/100% 进度，列表可滚动。不要把预览页类型改为排序在 Popup 之上的 FreePage。

字体 DefaultLightSans 为现有 Liberation Sans 的动态 TMP 资产，源 TTF 与 OFL 授权位于 Common/Fonts/DefaultLight。通过字体中心创建的项目目录登记固定皮肤 4（DefaultLightSans）、正文槽位 1、标题槽位 2；保留原默认字体选择，中文/符号回退至框架 DingTalk。完整 CJK 外观需项目选定正式字体。

背景在 Common/Textures/DefaultLight/Background.png，空态在 Common/Atlas/DefaultLight/EmptyState.png。二者由 imagegen 创建并已复制到项目；圆角使用新制 RoundedSurfaceHiDPI.png（256×256，PPU400，border=64），普通控件倍率4/3、细进度条倍率8/3。Checkmark.png 为 imagegen 生成的独立透明图标，等比显示；不使用字体字符或 Unity 内置 UI 图片替代。旧 RoundedSurface.png 仅保留历史资产。来源与提示词见仓库 docs/dev/ui-production-upgrade/evidence/S2/materials.md。

DefaultUILibraryBuilder 是本轮初始制作助手，拒绝覆盖已存在资产，不是最终资产的无损重建器；后续在正式 Prefab 与配置上维护，并重新生成目录。不要再次 Build 或重建 GUID。S3 从 EUIContentHostItem 的 Viewport/Content 和 EUIOptionRowItem 接入 EUIItemFactory；先明确实例生命周期和释放所有权，不重复实现现有控件。


加载进度条外观在 `EUILoadingProgressItem` Variant 内调整 Track、Track/Fill、Value；页面仅覆盖整体位置、显隐初值和渐变组。百分比向下取整，必要准备成功前由 Loading 限制在 99%，准备成功后才能达到 100%；快速模式同时隐藏轨道、填充和数字。该 Item 由父页 UILogic 绑定统一拥有，不再另外用 EUIItemFactory 创建第二份逻辑。

## 专用组件的职责

GM 的连续倍率与倍率文字由 EUIGMTimeScaleItem（EUIContinuousSliderItem Variant）维护，页面只应用全局时间；状态文字与增强开关分别由 EUIGMStateItem、EUIGMEnhancedToggleItem 维护。圆环 50% 和绿色图片默认效果保存在 EUIGMCircleItem、EUIGMImageItem，页面不再在初始化时覆盖美术参数。

设置页通过 EUISettingsContextItem 展示 SettingsContext；库预览页通过 EUILibraryProgressItem 同步条和百分比；动态列表通过 EUIDynamicSummaryItem 展示数量/选中项，通过 EUIDynamicRowProgressItem 展示行进度。专用 Variant 位于 UI/Module/UIFoundation/Prefabs，逻辑位于 Game/UI/Runtime/Module/UIFoundation。父页面只传递业务数据，嵌套 UILogic 由父级唯一拥有。

确认/提示弹窗本身已是公共弹窗的专用 Variant，其请求、结果与一次性关闭回调由弹窗负责。主菜单/玩法导航、扩展库页的页面分区切换仍属于页面协调；静态装饰无需额外逻辑。Ownership 示例继续展示明确的两种 Item 所有权。BUI 候选继承正式源，继续制作前重新导出当前契约。
