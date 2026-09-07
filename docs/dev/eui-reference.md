# EUI API 与绑定参考

> 更新：2026-09-07，按当前工作区源码核对。创建与维护流程见 [UI 开发参考](../user/UI开发参考.md)。

## 1. 架构与入口

`EUIManager` 负责业务页面队列、页面关系和 Loading；`EUIViewEngine` 执行加载、排序、过渡、遮罩与帧更新。
二者都是 Init 阶段的框架 Manager。`EUIPage` 是纯 C# 包装，`EUILogic` 是生成逻辑的基类，`EUIBinding` 是 Prefab 组件。
`ControlMap` 的值是 Unity `Component`，不会自动变成 `EUIButton` 等纯 C# 包装。

| 业务 API | 用法 |
|---|---|
| ShowMainPage / ShowPopup / ShowTopMost | 传入对应 PageType 的 EUIPageDef、可选 args 与 Action<EUIPage> 回调 |
| ShowOverlay / ShowFreePage | 打开固定 sortingOrder 的页面 |
| ShowSubPage | 必须显式传 parentPage；父关子关、子排序步长 50 |
| SetBackground / SetBackgroundAsync / ClearBackground | 管理单槽背景；异步版本可等待加载 |
| PreloadPage | 预先准备资源和绑定；OnInit/OnOpen 延后到真正打开 |
| ClosePage / ClosePageByDef | 按实例或页面定义关闭 |
| CloseTopPopup / CloseAllPopups | 关闭弹窗 |
| HidePageViewOnly / ShowPageViewOnly | 临时显隐，不等于关闭/重建 |
| GetReturnValue | 读取一次性页面返回值 |

MainPage 是栈式导航，ShowMainPage 不会替业务状态清理旧状态页面。
跨场景 TransitionTo 时在旧状态退出中 ClosePageByDef；Push/Pop 覆盖状态只暂停/恢复下层。
Popup 遮罩点击默认关闭页面，不会替 SettingsState 执行 Fsm.Pop；状态拥有的弹窗需让关闭入口统一走状态机。

`EUIPageDef` 构造参数为 `prefabPath, layer, pageType = MainPage, overlaySortingOrder = null, freePageSortingOrder = null, isFullScreen = false`。
常规页面由生成器填写，业务使用 GamePages 常量。PageType 包括 Background、MainPage、Popup、FullScreenPopup、TopMost、SubPage、Overlay、FreePage。
Item 是 Binding Role，不是 PageType。UILayer：Background=0、Normal=1000、Popup=2000、TopMost=25000。

## 2. 页面生命周期与动画

```text
CreateLogic：创建 Logic → 填 ControlMap → OnBeginLoad → OnBind
可选预加载：OnPreload(args, false)，不执行 OnInit/OnOpen
首次打开：OnResetDefault → OnInit → OnOpen → OnReset → OnShow → Enter 过渡完成
暂停/恢复：OnPause / OnResume
已显示页再次 Show：OnReopen，刷新参数而非重建
关闭：OnHide → Exit 过渡完成 → OnClose → OnResetDefault → OnReset → OnDispose
```

Page 的 OnDispose 清理订阅和引用。Item 的复用流程见下一节，不能照搬 Page 关闭即释放 Logic 的行为。
`TrackDisposable` 接受 IDisposable，并在 BroadcastDispose 中清理；手工 AddListener 应对称 RemoveListener。
Framework 页面写 `XxxUser` 钩子，OnBind 由生成的 `.Binding.cs` 提供。

普通 UI 过渡四选一：无 / 预设 CanvasGroup 渐变 / Animator / 自定义 UniTask。
业务自定义覆写 `EUILogic.OnCustomEnter()` 与 `OnCustomExit()`，不使用已移除的 EUIPage 协程 OnShow/OnHide。
Loading 方块仍采用“方块进入 → Custom Enter；Custom Exit → 方块退出”的专用链路。
完整遮挡退出以 LoadingFadeOutComplete 为准。

`NeedUpdate` 为 true 时驱动 OnUpdate/OnLateUpdate；Framework 模式从 Binding 勾选 UIUpdate 后重新生成，
业务增量写 OnUpdateUser。关闭选项时含用户代码的钩子由生成器保护，不手工覆盖。

## 3. EUIItem

`EUIBindingRole.Item` 是与 Page 平级的预制体角色，不是新的 `PageType`。Item 不能写入 `GamePages`，也不能通过 `EUIManager` 直接打开；它由列表、宿主页或业务模块实例化并放到指定父节点下。

标准 Item 根节点只有 `RectTransform + CanvasGroup + EUIBinding`，不会创建 Canvas、CanvasScaler、GraphicRaycaster、页面 Animator 或 SafeArea。它继承宿主 Canvas 的层级和输入环境。

```csharp
GameObject instance = Object.Instantiate(itemPrefab, parent, false);
if (EUIItemFactory.TryCreate(instance, out EUIItem item, out string error))
{
    item.BeginUse(args); // 先重置并进入一次借用
    // 在这里写入本次内容
    item.Show();
    // 临时隐藏：item.Hide();
    // 回池重置：item.ResetForPool();
    // 最终释放：item.Dispose();
}
```

Item 与 Page 复用同一个 `EUILogic` 和 `ControlMap`。在 Item 逻辑中，`Item` 属性指向所属 `EUIItem`，`Page` 为 null。生命周期规则：

- 创建：`OnBeginLoad → OnBind`，各一次。
- 首次借用并显示：`BeginUse: OnResetDefault → OnInit → OnOpen → OnReset`，写入业务内容后再由 `Show: OnShow` 显示。
- 同一次借用临时隐藏/恢复：`OnHide → OnShow`，不触发 OnClose。
- 回池：`OnHide → OnClose → OnResetDefault → OnReset`。
- 再次借出：不重复 OnInit，重放 `OnOpen → OnShow`。
- 最终销毁：`OnDispose`。

`NeedUpdate` 对 Item 同样有效；运行时驱动由 `EUIItem` 自动附加，业务预制体无需挂脚本。默认 `NeedUpdate == false` 时驱动组件保持禁用；使用基类 protected setter 会自动同步。若业务用自定义字段覆写并在运行时修改该字段，应调用 `Item.RefreshUpdateState()`。

## 4. 绑定控件类型

`WidgetTypes` 的序列化值以 EUIBinding.cs 为准：

| 类型 | 值 | 说明 |
|---|---:|---|
| Component | 0 | 通用 Component 引用 |
| Text / Toggle / Button / ProgressBar / Image | 1 / 2 / 3 / 4 / 5 | Text/TMP、Toggle、Button、Slider、Image |
| UIContainer / UILogic / InputField | 6 / 7 / 8 | 容器标记、子 Binding、InputField/TMP |
| ToggleGroup / ScrollRect / RawImage / Canvas | 9 / 10 / 11 / 12 | Unity 组件 |
| TabLoader | 13 | 保留的类型标记；不代表通用 Tab 系统已完整交付 |
| CanvasGroup | 14 | CanvasGroup 引用 |
| Extension | 65535 | 由 EUIExtension 特性注册的扩展组件 |

自动收集识别 `m_` 或 `m` + 大写开头的节点；遇到子 EUIBinding 时作为子 Logic 边界。
`EUIBindingExclude` 排除装饰节点。增强 Button/Toggle 的 Label 槽通过 IEUIExposedChildProvider
声明自有子组件，收集器跳过这些子节点，避免重复绑定。

| 增强组件 | 当前公开成员 |
|---|---|
| EUIButtonEx | EnableState、RefreshEnableState、AdditionalGraphics、Label |
| EUIToggleEx | Refresh、OnNode、OffNode、DisableNode、Label |
| EUIImageEx | SpriteIndex、SpriteArray、KeepNativeSize、PlaybackSpeed、Animated、RefreshSpriteState |
| EUICircleImage | 圆形/环形 Image；与 EUIImageEx 分别注册并精确识别 |

组件三点菜单可把 Button/Toggle/Image 替换为对应增强类型，操作使用 Undo 并延迟到 GUI 事件结束后执行。
附加效果 EUIGradient、EUIRoundedImageModifier、EUIPolygonRaycast、EUIGraphicAnimation 不作为替换型绑定注册。

`UIExtension/Runtime/Components` 还提供可手动使用的 C# 包装：EUIButton（Enable、UnityButton）、
EUIText（Text、Color、FontSize、TMP）、EUIImage（Sprite、FillAmount、UnityImage）、EUIInputField（Text、事件、IsLegacy）等。
这些类型独立于 ControlMap，不能把 Component 字典值强转成它们。当前 EUIText 不提供本地化系统。

## 5. 内部执行层与诊断

EUIViewEngine 的业务可替换点是 ResourceProvider 和 TransitionHandler；默认资源 Provider 连接 EmberResourceManager，
默认过渡 Handler 实现 CanvasGroup 渐变。普通业务页面开关仍走 EUIManager。

| 能力 | 入口 |
|---|---|
| 分帧预算 | FrameTimeBudgetMs，当前默认 100ms；这是处理预算，不是推荐帧时长 |
| 自适应 | AutoAdjustCanvasScaler、静态 AdjustCanvasScaler(GameObject) |
| 操作排队 | EnqueuePageOperation；加载/关闭中的挂起操作由页面状态控制 |
| 栈状态 | PageContext、ActivePages |
| 资源与表现分离 | OpenPage、ClosePage、PausePage、ResumePage、ReopenPage |
| 加载耗时 | EUIPage.LoadTiming；Unity Profiler 中的 EUIPage / EUIViewEngine / EUIManager 标记 |

CanvasScaler 自适应仅处理 ScaleWithScreenSize + MatchWidthOrHeight，按宽高比和参考分辨率选择匹配宽/高。
被遮挡页的渲染裁剪和恢复由 PageContext 处理；不把 sortingOrder、UILayer 和 planeDistance 当成同一含义。
`ClosePageInternal` 等 internal 成员属于实现细节，不是业务 API。

## 6. 事件与 Loading

### 4.1 EUIEvents（EmberEventBus 键值）

| 常量 | 值 | 说明 |
|------|------|------|
| `UIViewEngineReady` | 5000 | UI 视图引擎初始化完成 |
| `UIViewEngineShutdown` | 5001 | UI 视图引擎即将销毁 |
| `UIManagerReady` | 5002 | UI 业务入口初始化完成 |
| `LoadingFadeInStart` | 5010 | Loading 开始进入，玩家输入应关闭 |
| `LoadingFadeInComplete` | 5011 | 方块扫入和进度显示准备完成，可开始场景加载 |
| `LoadingFadeOutStart` | 5012 | 进度条开始渐隐；方块尚未扫出，不能开放玩家输入 |
| `LoadingFadeOutComplete` | 5013 | 进度条与方块全部退出，可开放玩家输入 |

通过 `EmberEventBus` 播报：
```csharp
EmberEventBus.OnNext(EUIEvents.UIManagerReady);
```

`LoadingFadeOutComplete` 由 `EUIManager.CloseLoadingPage` 的完整页面退出回调播报，
而不是由 `EUILoadingPage.OnCustomExit` 播报。后者只负责进度条渐隐，之后仍会执行方块扫出。
需要等待遮挡彻底消失的系统必须订阅 Complete，不能订阅 Start。

UI 引擎 Shutdown 不等待未结束的 Show/Hide。它会 ForceDispose 活跃页和延迟关闭页，
使正在 await 的页面过渡通过销毁取消正常退出，避免异步回调继续访问已销毁的 Unity 对象。

### 4.2 EUIObserver（UniRx 响应式）

基于 UniRx `Subject<T>`，业务模块通过静态属性订阅。

| 静态属性 | 类型 | 说明 |
|------|------|------|
| `OnPageOpened` | `IObservable<PageLifecycleEvent>` | 页面打开完成 |
| `OnPageClosed` | `IObservable<PageLifecycleEvent>` | 页面关闭完成 |
| `OnPagePaused` | `IObservable<PageLifecycleEvent>` | 页面被遮挡 |
| `OnPageResumed` | `IObservable<PageLifecycleEvent>` | 页面恢复 |
| `OnPageReopened` | `IObservable<PageLifecycleEvent>` | 页面重新打开 |
| `OnAllClosed` | `IObservable<Unit>` | 所有页面关闭 |

```csharp
// 订阅示例
TrackDisposable(EUIObserver.OnPageOpened.Subscribe(evt =>
{
    EmberDebug.Log(LogTags.Game, $"页面打开: {evt.Page}");
}));
```

## 7. 命名

### 5.1 绑定变量名规则

| 组件类型 | 前缀 | 示例 |
|----------|------|------|
| Button（原生） | `m_Btn_` | `m_Btn_Close`, `m_Btn_StartGame` |
| EUIButtonEx | `m_EUIBtn_` | `m_EUIBtn_Confirm`, `m_EUIBtn_Skip` |
| Toggle（原生） | `m_Tgl_` | `m_Tgl_AutoLogin`, `m_Tgl_Sound` |
| EUIToggleEx | `m_EUITgl_` | `m_EUITgl_Remember` |
| Text | `m_Txt_` | `m_Txt_Title`, `m_Txt_Gold` |
| Image（原生） | `m_Img_` | `m_Img_Icon`, `m_Img_Bg` |
| EUIImageEx | `m_EUIImg_` | `m_EUIImg_Avatar` |
| InputField | `m_Inp_` | `m_Inp_Name`, `m_Inp_Password` |
| Slider/ProgressBar | `m_Pgb_` | `m_Pgb_HP`, `m_Pgb_Volume` |
| ToggleGroup | `m_Tgp_` | `m_Tgp_TabGroup` |
| ScrollRect | `m_Scr_` | `m_Scr_ShopList` |
| UIContainer | `m_Ctn_` | `m_Ctn_ItemList` |
| Canvas | `m_Cvs_` | `m_Cvs_Main` |
| RawImage | `m_Raw_` | `m_Raw_Minimap` |
| TabLoader | `m_Tab_` | `m_Tab_Settings` |

### 5.2 通用规则

1. **绑定子节点必须以 `m_` 或 `m`+大写字母开头**（如 `m_Btn_Start` 或 `mBtnStart`），自动收集只识别这些节点
2. **EUI 增强组件用 `EU` 标记**（如 `m_EUIBtn_` 区别于 `m_Btn_`），一眼看出用了增强版
3. **不需要绑定的节点**不挂 `m_` 前缀（如 `Background`、`Layout`），或挂 `EUIBindingExclude` 排除
4. **页面级绑定**根节点用页面英文名（如 `MainMenuPanel` → 脚本名 `MainMenu`，页面名 `MainMenuPanel`）

## 8. 阅读与验证

- [源码阅读路线](uiextension-learning-path.md)
- [绑定与增强组件回归](uiextension-test-plan.md)
- [UI 开发中心与模板验收](framework-test-checklist.md)
- [SceneUI API](../../Packages/com.ember/SceneUI/README.md)

自动生成文件、用户钩子、Page/Item 路由和模块模板的写入保护均以生成器实现为准；修改后按 CLAUDE.md 执行验证。
