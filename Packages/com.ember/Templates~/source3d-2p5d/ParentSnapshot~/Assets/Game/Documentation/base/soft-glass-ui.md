# 柔光玻璃 UI 与 GM

归属为 base 共用 UI。2026-11-17 已通过项目中心将公共玻璃库与正式主菜单、设置、玩法操作及加载外观普通保存到 base；版本仍为 0.10.2，当前内容尚未 Bump 封存，六个子模板的父同步与适配尚未执行。原 topdown 草稿保留完整备份，最终恢复须在模板迁移完成后按三方内容核对。本次未发布框架，新 UI 需与配套工作区源码一起使用，不能据旧兼容声明认定已发布包可直接消费。

## 选择、回退与 Popup

`Resources/Config/UIFoundation/EUIAppearance.asset` 的 `CommonSkin` 与 `GmPresentation` 分别选择 Legacy / SoftGlass；配置缺失时两者均为 Legacy。公共弹窗通过 `EUICommonDialogs.ShowAlert/ShowConfirm(EUIDialogRequest)` 选择相应 Page，旧页面、旧 GM GUID 和注册仍保留。GM 首次入口为 `EUIAppearanceBootstrap.ShowGM()`；需要在当前会话切换时调用 `SwitchGM(EUIGmPresentation.Legacy)` 或 SoftGlass，关闭旧实例后再打开，保留时间倍率。

上述选择只路由公共弹窗与 GM，不会自动撤销已换肤正式页面的外观。base 设置页当前只显示来源上下文和返回操作，没有虚构音量、画质等持久化服务；小说模板的完整偏好由其自身逻辑维护。

Popup 效果独立于上述选择：`EUIManager.PopupMaskEffect` 默认 Blur，显式设 Solid 恢复纯色。页面 `MaskEffectOverride` 可覆盖，`UseMask=false` 完全关闭模态遮罩；局部玻璃仍可采样。质量、旧 tint 迁移和不支持环境的 Solid 回退见包内 `Documentation~/ui/popup-backdrop.md`。全局变更作用于后续打开，现有页面用 `RefreshBackdrop(Page)` 明确刷新。

`EUIBackdropInstallation.asset` 指定 PC/Mobile 管线的专用 Renderer index1、原 UI renderer index-1 和四个空闲层 EUIBackdrop0—3。GameInit 通过管线无关安装事件请求安装，不把 URP 类型引入 UI core 或业务运行程序集。移植前核对目标工程层冲突及 renderer index，不能照抄覆盖业务层。

## 资源与制作

独立 Editor 库为 `Assets/Ember/Editor/UILibrary/SoftGlassLibrary.asset`，24项、AutoMatch关闭，不替换 DefaultLibrary。菜单 `Ember/UI/柔光玻璃制作` 提供本任务范围的制作/再生成入口；正式 Prefab、Binding 和 GamePages 由 EUI 开发中心维护。

运行时 Prefab 地址前缀为 `UI/Module/UIFoundation/Prefabs/EUISoftGlass`。包括 StandardPage/StandardPopupPage 两个结构骨架，Panel、PrimaryButton、SecondaryButton、IconButton、CloseButton、Switch、Slider、VerticalSlider、Input、Search、ExtensionExample、GMEntry、GMState、GMTime、GMAction、GMTest、GMCenter 共17个 Item，以及 Alert、Confirm、Showcase、ExtensionExample、GM 共5个 Page。骨架不注册，Item 不生成 PageDef。生成 `.Binding.cs` 不手改，业务逻辑写同名用户 `.cs`。

Showcase 是可操作的控件页，包含禁用按钮、开关、输入清除、横竖 Slider 和深/浅/彩色动态背景；ExtensionExample 同时演示静态嵌套 Item 与资源句柄加载、EUIItemFactory 创建的动态 Item。原 Primary/Secondary、Icon/Close、Input/Search 及消费扩展分别保留 Variant 来源。样式资产 SoftGlassNeutral/Light/Accent 的 tint 与 Small12/Control20/Card32/Panel48 圆角 token 可显式 ApplyTo，库 slot 可修改文字、颜色、圆角和控件状态。

两个示例页按最小列宽340DU重排，720窄屏单列滚动；宽屏最多三列。固定尺寸内部控件不应被强行压到更窄列；圆形图标示例明确80×80。GM卡片另用响应式网格，窄屏滚动访问下方动作。

正式 base 页面保持原 Prefab GUID、Binding、PathMode 和用户逻辑，复用上述 R2 样式；主菜单纵向操作、受限宽度设置卡和浅背景深色前景已覆盖横竖屏。维护源为 `Assets/Game/UI/Editor/UIFoundation/BaseSoftGlassPageAuthoring.cs`，显式调用其 `Apply()` 才更新这批原位页面；普通 Binding 再生成不重做页面布局。加载页保留实际准备完成前不到 100%、快速模式隐藏进度和原遮幕准备条件。

示例背景 Canvas 已在 Prefab 中制作；`SoftGlassBackdropAttachment` 在页面存活期把它借出为独立根 Canvas，排序在页面前2级，最终释放时归还原父级、序位和布局。后端独立边界必须是 root Canvas，普通嵌套 Item 共用页面组；此 helper 不是运行时 UI 搭建器。动态 Item 增删后调用祖先 `EUIBackdropGroup.RefreshSurfaces()`，不同时给同一实例绑定 UILogic 和 Factory 所有权。

## GM 与业务扩展

控制中心保留真实 FSM 状态、Unity/Ember 时间同步、0.1—3倍连续调节、1倍复位、增强 Toggle 与原 Image/RawImage/EUIImage/Circle 测试、退出。收起不重置时间；关闭按钮与 ESC 收起中心，入口恢复为同风格圆点。入口单指/鼠标拖动超过8DU后不触发打开，安全区内吸左右边，保存边和归一化高度；动效使用非缩放时间。

当前入口圆钮显示原FPSDisplay的实时帧率，中心不再有环/圆点装饰。滑杆移除独立Handle，仍可在Track上点击、拖动和键盘调节；竖条用平顶填充表示数值。控制中心限制最大尺寸，横屏三张300DU卡片，竖屏两张300DU卡加240DU横向测试卡，快捷操作六/四列；四个测试样本完整显示。显式制作维护源SoftGlassUIRevisionTwo.Apply负责更新本库既有资源，普通Binding再生成不重排布局。

消费模块实现 `IGMActionProvider`，`Actions` 返回稳定唯一 Key、Group、Order、Title、Icon、Execute 或 ReadToggle/WriteToggle，可选 IsAvailable 与 Dangerous。用 `GMActionRegistry.Register(provider)` 返回的 IDisposable 控制有效期，在模块退出时 Dispose。按钮只保存 key，每次执行重新解析；注销后不保留失效业务对象。样例 `SoftGlassDemoActions` 注册背景切换和 verbose 开关；核心动作只在中心打开期间注册。不要在业务项目复制修改框架 GM 受管区。

## 验证边界

当前接线面向 Unity6/URP17、完整 viewport、单显示器。其他管线、XR、部分 viewport 或层池耗尽有 Solid 回退。2026-11-17 的维护工程已完成 1920×1080、1280×720、1080×1920 真实页面导航与预览自检，用户认可 base 页面视觉；双 Popup 隐藏链与资源清退有相关短验证。独立验收尚未完成，六子模板、移动真机、Player 构建及消费工程未据此签收。PC/Mobile Renderer 资源存在不表示对应设备或构建通过；用户视觉反馈、实施自检和独立行为验收分别记录，不能互相代替。
