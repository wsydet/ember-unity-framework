# 柔光玻璃 UI 与 GM

归属为 base 共用 UI。2026-11-17 已通过项目中心将公共玻璃库与正式主菜单、设置、玩法操作及加载外观普通保存到 base；版本仍为 0.10.2，当前内容尚未 Bump 封存，六个子模板的父同步与适配尚未执行。原 topdown 草稿保留完整备份，最终恢复须在模板迁移完成后按三方内容核对。本次未发布框架，新 UI 需与配套工作区源码一起使用，不能据旧兼容声明认定已发布包可直接消费。

## 选择、回退与 Popup

`Resources/Config/UIFoundation/EUIAppearance.asset` 的 `CommonSkin` 与 `GmPresentation` 分别选择 Legacy / SoftGlass；配置缺失时两者均为 Legacy。公共弹窗通过 `EUICommonDialogs.ShowAlert/ShowConfirm(EUIDialogRequest)` 选择相应 Page，旧页面、旧 GM GUID 和注册仍保留。GM 首次入口为 `EUIAppearanceBootstrap.ShowGM()`；需要在当前会话切换时调用 `SwitchGM(EUIGmPresentation.Legacy)` 或 SoftGlass，关闭旧实例后再打开，保留时间倍率。

上述选择只路由公共弹窗与 GM，不会自动撤销已换肤正式页面的外观。base 设置页提供来源上下文、桌面帧率与响应设置和返回操作；小说模板的完整偏好由其自身逻辑维护。

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

控制中心采用四列紧凑布局：标题占左两格，右侧为专用拖动和关闭圆钮；顶层 FSM 方卡、时间开关/恢复和单根倍率条固定在顶部，下方为帧率模式快切和独立滚动的消费项目操作。只有拖动圆钮移动主面板，按住呈黄色。关闭时间调节只禁用滑杆并保留倍率，独立恢复按钮始终将 Unity/Ember 时间同步至 1×。原增强、UI 库及背景演示不再自动注册到正式 GM；独立 Showcase 与 Legacy 仍保留。

入口与面板显示独立的 `FPSDisplay` 实测帧率。组件跳过启用后的第一个不完整帧区间，窗口未填满时用实际采样数作分子；目标帧率不是 FPS 读数。入口保留 8×Canvas.scaleFactor 的屏幕拖动阈值、左右吸边、安全区、0.18秒非缩放缓出及旧 Right/Y 位置键。短按开主面板，约550ms长按切换收藏 HUD；首次越过拖动阈值收起 HUD，松开不重开，下次长按使用新位置。

消费模块实现 `IGMActionProvider`，`Actions` 返回稳定唯一 Key、Group、Order、Title、Icon、Execute 或 ReadToggle/WriteToggle，可选 IsAvailable 与 Dangerous。用 `GMActionRegistry.Register(provider)` 返回的 IDisposable 控制有效期，在模块退出时 Dispose。主面板与 HUD 都只保存 key 并使用同一执行入口；危险操作经公共确认后重新解析并复查可用性。确认期间 GM 暂排在该模态弹窗下方，关闭后恢复原排序与 HUD 状态。

扩展操作可右键或约550ms长按收藏/取消，手势不执行原动作，移动、滚动、取消和失焦清理待决操作。最多5项，第6项明确拒绝；默认空，记录只含 schema 和稳定 key，注销项保留为潜在收藏。PlayerPrefs 按应用与系统用户隔离；有账号的消费项目在登录时调用 `GMFavorites.SetUserNamespace(userId)`。不要把业务对象或可执行委托存入收藏。

HUD 圆钮默认直径为入口的83%，角落优先改方向/弧度，再用76/69/62%档位。命中圆至少48DU且不小于入口64%，以完整命中圆校验安全区、入口、相邻项和显式避让矩形。业务用 `GMHudLayout.RegisterAvoidRect(RectTransform)` 持有可释放登记；不识别世界物体或未登记 UI。缩小立即，放大需4DU余量并稳定约180ms，全部候选无解才提示改用主面板并保留收藏。入口大小保持不变。

桌面模式由 `UIFrameRatePolicy` 统一管理：默认高响应自动、VSync 0；自动目标为60→66、90→95、120→124、144→147、165→168、180→182、240→242、360→361，表间取相邻更高档余量并向上取整。自定义正整数精确生效，不加余量。节能/平衡分别 VSync 2/1，禁用但保留目标类型和值。GM 只快切三模式，完整设置编辑自动/自定义；摘要读取实际 Unity 配置。仅刷新率变化时重算仍由本策略控制的自动目标，消费端主动接管后不逐帧覆盖。移动/XR不套用这套桌面映射。

本轮维护源为 `GMControlCenterAuthoring`，制作窗口提供 D10 有界预检/制作，保留目标 Prefab GUID，再经公开 EUI Binding 生成入口更新代码；普通生成不重排布局。旧 RevisionTwo 属于历史库版本，不用它覆盖 D10。玻璃安装由 `EUIAppearanceBootstrap.InitializeUI()` 在 GameInit 的 UI 启动阶段调用，独立于 ShowGM；安装幂等且 GM 关闭不释放其它页面的租约。Loading 根 Canvas 提供自己的 Group，快速模式无可见玻璃进度时不占租约。`GMBackdropSettings` 提供 GM 独立模糊质量配置；`SoftGlassDialog` 是设置和公共玻璃弹窗的通透表面样式，显式遮罩染色只用于这几个目标，Legacy 和其它页面保留原声明。

2026-11-19 的改动只在当前 Assets 与必要框架组件实施；活动编辑 base 0.11.1。模板保存、Bump、快照同步、提交和发布均未执行。下方2026-11-17记录是历史验证，不替代本轮任务 `20261119-gm-control-center` 的实施证据与后续独立验收。

## 验证边界

当前接线面向 Unity6/URP17、完整 viewport、单显示器。其他管线、XR、部分 viewport 或层池耗尽有 Solid 回退。2026-11-17 的维护工程已完成 1920×1080、1280×720、1080×1920 真实页面导航与预览自检，用户认可 base 页面视觉；双 Popup 隐藏链与资源清退有相关短验证。独立验收尚未完成，六子模板、移动真机、Player 构建及消费工程未据此签收。PC/Mobile Renderer 资源存在不表示对应设备或构建通过；用户视觉反馈、实施自检和独立行为验收分别记录，不能互相代替。

帧率设置由现有 `PlayerPrefsModule` 保存模式、自动选择及合法自定义目标，GM 快切与设置页共用该记录（`Ember.UI.FrameRate.v1.<Application.identifier>`）。无有效记录时使用高响应自动/自定义备用值180；非法记录整条回退，直到用户合法修改才写回。每次新运行（含禁域重载 Play）重新读取并应用，同次运行的初始化保持幂等。UI 启动接线通过会话 Cancellation 绑定的 PlayerLoop 每500ms检查刷新率，界面关闭及时间暂停时继续，退出后取消；仍保留消费端接管保护。HUD 放大候选按每个命中圆4DU额外半径构造间距，安全区、入口或避让几何变化会重置180ms连续稳定计时。
