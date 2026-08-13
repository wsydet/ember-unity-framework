# EUI 框架 UI 参考文档

> 版本：0.1 | 更新：2026-08-11

---

## 一、WidgetTypes 支持的控件类型

| 类型 | 枚举值 | 对应组件 | 说明 |
|------|--------|----------|------|
| `Component` | 0 | 任意 | 兜底类型，无特殊逻辑 |
| `Text` | 1 | `Text` / `TextMeshProUGUI` | 文本显示 |
| `Toggle` | 2 | `Toggle` / `EUIToggleEx` | 开关 |
| `Button` | 3 | `Button` / `EUIButtonEx` | 按钮 |
| `ProgressBar` | 4 | `Slider` | 进度条 |
| `Image` | 5 | `Image` / `EUIImageEx` | 图片 |
| `UIContainer` | 6 | `UIContainer` | UI 列表容器 |
| `UILogic` | 7 | `EUIBinding` | 子页面绑定 |
| `InputField` | 8 | `InputField` / `TMP_InputField` | 输入框 |
| `ToggleGroup` | 9 | `ToggleGroup` | 开关组 |
| `ScrollRect` | 10 | `ScrollRect` | 滚动区域 |
| `RawImage` | 11 | `RawImage` | 原始图片 |
| `Canvas` | 12 | `Canvas` | 画布 |
| `TabLoader` | 13 | `TabLoader` | 标签页加载器 |
| `Extension` | 65535 | 自定义 | 通过 `[EUIExtension]` 注册的扩展类型 |

---

## 二、原生组件 vs EUI 增强组件

### 2.1 Button

#### Unity 原生 `Button`
| API | 说明 |
|-----|------|
| `button.onClick.AddListener(() => {})` | 注册点击事件 |
| `button.interactable` | 是否可交互 |
| `button.targetGraphic` | 目标图形（ColorTint 过渡用） |
| `button.transition` | 过渡方式（ColorTint / SpriteSwap / Animation） |
| `button.colors` | ColorTint 颜色配置 |
| `button.navigation` | 导航配置 |

#### 框架 `EUIButton`（C# 包装类，ControlMap 中获取）
| API | 说明 |
|-----|------|
| `Enable` | 启用/禁用按钮（get/set），支持 CanClickWhenDisable |
| `CanClickWhenDisable` | 禁用状态下仍可触发点击（仅 EUIButtonEx 支持） |
| `UnityButton` | 获取底层 Unity Button 引用 |

#### EUI 增强 `EUIButtonEx`（继承自 Button，挂载到 GameObject 上）
| API | 说明 |
|-----|------|
| `EnableState` | 自定义启用状态（get/set），自动切换 enableNode/disableNode |
| `RefreshEnableState()` | 手动刷新启用/禁用节点的可见性 |
| `AdditionalGraphics` | 附加的 Graphic 数组（get/set），状态切换时同步 CrossFadeColor |

**EUIButtonEx 额外功能：**
- **状态节点** — 配置 `启用节点`/`禁用节点`，根据 EnableState 自动显示/隐藏
- **附加图形** — ColorTint 过渡时同步变色，用于按钮+图标+文字颜色联动

#### 建议命名

| 组件 | 命名 |
|------|------|
| 原生 Button | `m_Btn_Xxx` 如 `m_Btn_Close`, `m_Btn_StartGame` |
| EUIButtonEx | `m_EUIBtn_Xxx` 如 `m_EUIBtn_Confirm`, `m_EUIBtn_Skip` |

---

### 2.2 Toggle

#### Unity 原生 `Toggle`
| API | 说明 |
|-----|------|
| `toggle.onValueChanged.AddListener(v => {})` | 值变更事件 |
| `toggle.isOn` | 是否选中 |
| `toggle.group` | 所属 ToggleGroup |
| `toggle.interactable` | 是否可交互 |

#### 框架 `EUIToggle`
| API | 说明 |
|-----|------|
| `Enable` | 启用/禁用 |
| `UnityToggle` | 底层 Unity Toggle 引用 |

#### EUI 增强 `EUIToggleEx`（继承自 Toggle）
| API | 说明 |
|-----|------|
| — | 无额外公开 API，通过状态节点自动管理 |

**EUIToggleEx 额外功能：**
- `On 节点` — isOn = true 时显示
- `Off 节点` — isOn = false 时显示
- `Disable 节点` — 不可交互时显示

#### 建议命名

| 组件 | 命名 |
|------|------|
| 原生 Toggle | `m_Tgl_Xxx` 如 `m_Tgl_AutoLogin` |
| EUIToggleEx | `m_EUITgl_Xxx` 如 `m_EUITgl_Remember` |

---

### 2.3 Text

#### Unity 原生 `Text` / `TextMeshProUGUI`
| API | 说明 |
|-----|------|
| `text.text` | 文本内容 |
| `text.color` | 颜色 |
| `text.fontSize` | 字号 |
| `text.font` | 字体 |

#### 框架 `EUIText`
| API | 说明 |
|-----|------|
| `Text` | 文本内容（get/set） |
| `SetActiveText(string)` | 设置文本内容 |
| `SetLocalizationKey(string)` | 设置多语言 key（依赖本地化系统） |
| `UnityText` | 底层 Unity Text 引用 |

#### 建议命名

```
m_Txt_Xxx    如 m_Txt_Title, m_Txt_PlayerName, m_Txt_Gold
```

---

### 2.4 Image

#### Unity 原生 `Image`
| API | 说明 |
|-----|------|
| `image.sprite` | 精灵 |
| `image.color` | 颜色 |
| `image.raycastTarget` | 是否参与射线检测 |
| `image.fillAmount` | 填充量（Filled 模式） |

#### 框架 `EUIImage`
| API | 说明 |
|-----|------|
| `Sprite` | 精灵（get/set） |
| `SetNativeSize()` | 设为原始尺寸 |
| `RaycastTarget` | 射线检测开关 |

#### EUI 增强 `EUIImageEx`（继承自 Image）
| API | 说明 |
|-----|------|
| `Sprites[]` | 精灵数组（序列帧） |
| `CurrentIndex` | 当前帧索引 |
| `PlayAnimation(fps, loop, delay)` | 播放帧动画 |
| `StopAnimation()` | 停止帧动画 |
| `AlphaHitTestThreshold` | 不规则点击区域阈值 |

#### 建议命名

| 组件 | 命名 |
|------|------|
| 原生 Image | `m_Img_Xxx` 如 `m_Img_Icon, m_Img_Background` |
| EUIImageEx | `m_EUIImg_Xxx` 如 `m_EUIImg_Avatar` |

---

### 2.5 InputField

#### Unity 原生 `InputField` / `TMP_InputField`
| API | 说明 |
|-----|------|
| `input.text` | 输入文本 |
| `input.onValueChanged.AddListener(v => {})` | 文本变更事件 |
| `input.onEndEdit.AddListener(v => {})` | 编辑结束事件 |
| `input.contentType` | 内容类型（数字/密码等） |

#### 框架 `EUIInputField`
| API | 说明 |
|-----|------|
| `Text` | 输入文本（get/set） |
| `Placeholder` | 占位文本 |
| `UnityInputField` | 底层 InputField 引用 |

#### 建议命名

```
m_Inp_Xxx    如 m_Inp_PlayerName, m_Inp_Password, m_Inp_Search
```

---

### 2.6 ProgressBar (Slider)

| API | 说明 |
|-----|------|
| `slider.value` | 当前值 |
| `slider.minValue` / `slider.maxValue` | 范围 |
| `slider.onValueChanged.AddListener(v => {})` | 值变更事件 |
| `EUIProgressBar.Value` | 框架包装的 value（get/set） |
| `EUIProgressBar.SetValue(float, bool animate)` | 带动画/不带动画设置 |

#### 建议命名

```
m_Pgb_Xxx    如 m_Pgb_Loading, m_Pgb_HP, m_Pgb_Volume
```

---

### 2.7 ToggleGroup

| API | 说明 |
|-----|------|
| `group.allowSwitchOff` | 是否允许全部关闭 |
| `group.AnyTogglesOn()` | 是否有选中的 Toggle |
| `group.GetFirstActiveToggle()` | 获取第一个选中的 Toggle |

#### 建议命名

```
m_Tgp_Xxx    如 m_Tgp_TabGroup
```

---

### 2.8 ScrollRect

| API | 说明 |
|-----|------|
| `scroll.content` | 内容 RectTransform |
| `scroll.viewport` | 视口 RectTransform |
| `scroll.horizontal` / `scroll.vertical` | 方向 |
| `scroll.normalizedPosition` | 归一化位置（0-1） |
| `scroll.velocity` | 惯性速度 |
| `scroll.onValueChanged.AddListener(v => {})` | 滚动位置变更事件 |

#### 建议命名

```
m_Scr_Xxx    如 m_Scr_ShopList, m_Scr_ChatHistory
```

---

### 2.9 UIContainer

| API | 说明 |
|-----|------|
| `container.AddCell(GameObject)` | 添加列表项 |
| `container.RemoveCell(GameObject)` | 移除列表项 |
| `container.Clear()` | 清空 |
| `container.ScrollTo(int index)` | 滚动到指定位置 |
| `container.CellCount` | 当前列表项数量 |

#### 建议命名

```
m_Ctn_Xxx    如 m_Ctn_ShopItemList, m_Ctn_BattleCards
```

---

### 2.10 标记排除 `EUIBindingExclude`

挂载到不需要加入 UIBinding 的节点上（及其子树），自动收集和扫描会跳过。

```
适用场景：背景图、纯装饰元素、不参与绑定的布局容器
```

### 2.11 EUIComponent（框架 UI 控件基类）

所有 Ember UI 控件的基类，封装了 GameObject、RectTransform 操作、可见性和事件管道。

| API | 说明 |
|------|------|
| `GameObject` | 包装的 GameObject |
| `RectTransform` | RectTransform 引用 |
| `Transform` | Transform 引用 |
| `UserState` | 用户自定义状态对象 |
| `IsDisposed` | 是否已释放 |
| `Visible` | 可见性（get/set） |
| `VisibleInHierarchy` | 层级可见性（只读） |
| `X` / `Y` / `Width` / `Height` | 位置/尺寸（get/set） |
| `AnchoredPosition` | 锚点位置 |
| `WorldPosition` | 世界坐标位置 |
| `OnClick` | 点击回调 `Action<EUIComponent>` |
| `OnLongPress` | 长按回调 `Action<EUIComponent, bool>` |
| `SetLongPressTime(delay, repeat)` | 设置长按时间 |
| `CancelLongPress()` | 取消长按 |
| `Enable` | 启用/禁用（虚方法） |
| `SetGray(bool)` | 设为灰色（虚方法） |
| `OnInit()` / `OnShow()` / `OnHide()` / `OnDispose()` / `OnUpdate()` | 生命周期虚方法 |

---

### 2.12 输入事件组件

#### EUIEventTriggerListener（指针事件监听）

| API | 说明 |
|------|------|
| `onClick` | 点击 `VoidDelegate(GameObject)` |
| `onDown` / `onUp` | 按下/抬起 |
| `onEnter` / `onExit` | 进入/离开 |
| `onSelect` | 选中 |
| `onLongPressTime` | 长按状态 `BoolDelegate(GameObject, bool)` |
| `onDropEnter` / `onDropExit` / `onDrop` | 拖放事件 |
| `Parameter` | 自定义参数 |
| `PointerEventData` | 最后一次事件的 PointerEventData |
| `IsClicking` | 是否正在点击 |
| `SetLongPressTime(delay, repeat)` | 设置长按参数 |
| `static Get(GameObject)` / `static Get(Transform)` | 获取/创建实例 |

#### DragEventTriggerListener（拖拽事件监听）

| API | 说明 |
|------|------|
| `OnDragCallback` | 拖拽中回调 |
| `OnDragStartCallback` / `OnDragEndCallback` | 拖拽开始/结束 |
| `OnDragToDropStart` / `OnDragToDropEnd` | DragToDrop 回调 |
| `IsDraggingToDrop` | 是否正在 DragToDrop |
| `IsDraggingMove` | 是否发生过移动 |
| `IsDragToDrop` | DragToDrop 模式开关 |
| `static Get(GameObject)` / `static Get(Transform)` | 获取/创建实例 |
| `static GetDragToDrop(...)` | 创建 DragToDrop 实例 |

---

### 2.13 视觉效果组件

| 组件 | 继承自 | 说明 |
|------|--------|------|
| `EUICircleImage` | `Image` | 圆形/环形图，`Segments`（三角数）、`FillPercent`（0-1） |
| `EUIRoundedImageModifier` | `BaseMeshEffect` | 圆角，`Radius`（像素）、`TriangleNum`（4-16） |
| `EUIGradient` | `BaseMeshEffect` | 渐变，支持双色/四色、左到右/上到下 |
| `EUIPolygonRaycast` | `Graphic` | 多边形精确射线检测，需 `PolygonCollider2D` |
| `EUIGraphicAnimation` | `MonoBehaviour` | Shader 属性动画，`AnimatedProperties[]` |

#### EUIImageEx 补充（2.4 节）

| 额外 API | 说明 |
|------|------|
| `SpriteIndex` (get/set) | 当前帧索引 |
| `SpriteArray` (get/set) | 精灵数组（序列帧） |
| `KeepNativeSize` | 自动 SetNativeSize |
| `PlaybackSpeed` | 帧动画速度倍数 |
| `Animated` (get/set) | 启用/禁用帧动画 |
| `RefreshSpriteState()` | 手动刷新精灵状态 |

#### ContentSizeFitterEx

| API | 说明 |
|------|------|
| `HorizontalFit` / `VerticalFit` | 自适应模式 |
| `MaxWidth` / `MaxHeight` | 最大约束（≤0 = 无限制） |

---

### 2.14 层级/排序组件

| 组件 | 说明 |
|------|------|
| `EUIMeshOrder` | 子节点 Renderer 排序，`OrderOffset` + `UpdateSortingOrder()` |
| `RelativeCanvasOrder` | 相对父 Canvas 排序，`OrderOffset` + `UpdateSortingOrder()` |
| `ICanvasSortingOrderHandler` | 排序接口，`void UpdateSortingOrder()` |

---

### 2.15 工具组件

| 组件 | 说明 |
|------|------|
| `AnimationEventReceiver` | 接收 Animation Clip Event，`AnimationEventCallback(string)` |
| `TransformCopier` | 每帧同步 Transform，`Copied`（目标 Transform） |
| `EUIBasicUIExtensions` | 静态扩展：`GetChildImage(root, name)` / `GetChildText(root, name)` / `SetTextColor(graphic, r, g, b, a)` |
| `EUIToggleGroupEx` | 增强 ToggleGroup，`ToggleGroupValueChange` 事件（含新旧索引）、`GetCurrentOnToggle()`、`SetToggleOn(index)` |

---

## 三、页面框架 API

### 3.1 EUIPage（页面包装类）

每个实例管理一个 UI 页面的完整生命周期。

#### 公共属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Name` | `string` | 页面名称（GameObject 名） |
| `GameObject` | `GameObject` | 包装的 GameObject |
| `Transform` | `Transform` | 页面 Transform |
| `CanvasGroup` | `CanvasGroup` | 透明度和交互控制 |
| `Canvas` | `Canvas` | Canvas 组件 |
| `RectTransform` | `RectTransform` | RectTransform 组件 |
| `EUIPageDef` | `EUIPageDef` | 页面定义元数据 |
| `ParentPage` | `EUIPage` | 父页面（SubPage 时非空） |
| `SubPages` | `IReadOnlyList<EUIPage>` | 子页面列表 |
| `Logic` | `EUILogic` | 逻辑层实例 |
| `State` | `PageState` | 当前状态（Unloaded/Showing/Opened/Hiding/Closed） |
| `IsOpened` | `bool` | 是否已打开 |
| `IsInitialized` | `bool` | 是否已初始化 |
| `LoadTimingData` | `LoadTiming` | 页面加载耗时数据（仅首次打开时有效） |

#### LoadTiming 结构体

| 字段 | 类型 | 说明 |
|------|------|------|
| `InitMs` | `float` | Init 阶段耗时（ms） |
| `ShowMs` | `float` | PlayShow 阶段耗时（ms） |
| `TotalMs` | `float` | 总耗时（ms）= InitMs + ShowMs |
| `IsFirstOpen` | `bool` | 是否首次打开（Reopen 不更新） |

打开完成时框架自动输出 `EmberDebug.LogInit` 日志：
`页面加载完成: MainMenu init=12.3ms show=85.7ms total=98.0ms`

#### SubPage 排序规则

打开 SubPage 时，框架自动计算 `Canvas.sortingOrder`，无需手动设置：

1. 沿父页面链向上找到**顶层非 SubPage 祖先**
2. 遍历祖先的所有已有 SubPage，取最大 `sortingOrder`
3. 新 SubPage = `max(祖先排序, 已有最大) + 50`

关闭 SubPage 后排序不会自动重算——新的 SubPage 会在已有最大值之上继续递增。

#### 挂起操作机制

页面在 `Showing` 或 `Hiding` 过渡状态时收到的 `Close`/`Reopen` 请求会被**自动挂起**，
过渡动画完成后由 `EUIManager` 自动调度执行。无需业务层关注。

#### EUIPage 子类可 override 的钩子

| 方法 | 触发时机 | 说明 |
|------|----------|------|
| `OnInitialize(object args)` | Init 阶段，最早 | 框架层初始化数据 |
| `OnShow()` → `IEnumerator` | PlayShow 阶段 | **打开动画协程**，返回 null = 无动画 |
| `OnHide()` → `IEnumerator` | PlayHide 阶段 | **关闭动画协程**，返回 null = 无动画 |
| `OnPaused()` | 被遮挡时 | 框架层暂停处理 |
| `OnResumed()` | 恢复可见时 | 框架层恢复处理 |
| `OnReopened(object args)` | 关闭后重新打开 | 框架层重开处理 |
| `OnCleanup()` | Cleanup 第一顺位 | 框架层清理 |
| `OnEscapeKey()` → `bool` | 返回键 | return true 阻止冒泡 |

---

### 3.2 EUILogic（业务逻辑基类）

生成的 `.cs` 文件继承此类，写具体业务逻辑。**非 MonoBehaviour，是纯 C# 类。**

#### 内部字段（框架填充）

| 字段 | 类型 | 说明 |
|------|------|------|
| `ControlMap` | `Dictionary<string, Component>` | 控件引用字典，key = 变量名 |
| `Page` | `EUIPage` | 所属页面引用 |

#### 生命周期钩子（业务 override）

| 方法 | 触发时机 | 用途 |
|------|----------|------|
| `OnBind()` | 仅一次，最早 | 从 ControlMap 取控件引用 |
| `OnInit()` | 仅一次，OnBind 后 | **注册事件、设默认值** |
| `OnOpen(object param)` | 每次打开 | 接收打开参数 |
| `OnShow()` | PlayShow 阶段（动画前） | **刷新数据** |
| `OnHide()` | PlayHide 阶段（动画前） | 停止刷新 |
| `OnPause()` | 被遮挡 | 暂停计时器/音频 |
| `OnResume()` | 恢复可见 | 恢复计时器/音频 |
| `OnClose()` | Cleanup 阶段 | **持久化数据**（UI 还在） |
| `OnReset()` | 打开后 + 关闭前 | 重置 UI 到默认状态 |
| `OnDispose()` | Cleanup 末尾，最后 | **注销事件、释放引用** |
| `OnUpdate()` | 每帧（NeedUpdate=true） | 逐帧逻辑 |
| `OnLateUpdate()` | 每帧（NeedUpdate=true） | 逐帧逻辑 |

#### 事件自动清理

| 方法 | 说明 |
|------|------|
| `TrackDisposable(IDisposable)` | 注册可销毁对象，`OnDispose` 时自动清理 |

适用于 `EmberEventBus.On` 返回值、UniRx `Subscribe` 等。在 `OnInit` 中注册后无需在 `OnDispose` 手动清理：

```csharp
public override void OnInit()
{
    TrackDisposable(EmberEventBus.On(MyEvents.Foo, OnFoo));
    TrackDisposable(Observable.EveryUpdate().Subscribe(_ => Tick()));
}
// OnDispose 中无需手动注销——框架自动清理所有 TrackDisposable
```

#### 子 Logic 管理

| 方法 | 说明 |
|------|------|
| `RegisterChildLogic(EUILogic)` | 注册嵌套 UIBinding 的子 Logic |

#### 示例代码

```csharp
public partial class UIMainMenu : EUILogic
{
    private Button _btnStart;
    private TextMeshProUGUI _txtTitle;

    public override void OnBind()
    {
        _btnStart = ControlMap["Btn_Start"] as Button;
        _txtTitle = ControlMap["Txt_Title"] as TextMeshProUGUI;
    }

    public override void OnInit()
    {
        _btnStart.onClick.AddListener(OnClickStart);
    }

    public override void OnOpen(object param)
    {
        _txtTitle.text = "欢迎回来";
    }

    public override void OnDispose()
    {
        _btnStart.onClick.RemoveListener(OnClickStart);
    }

    private void OnClickStart()
    {
        EUIPageRouter.ShowMainPage(PageDefs.battle);
    }
}
```

---

### 3.3 预设渐入渐出

在 EUIBinding Inspector 中配置，EUIPage 自动应用，<b>不需要自定义动画</b>：

| 字段 | 说明 |
|------|------|
| `启用预设渐入渐出` | 勾选后自动使用 CanvasGroup alpha 做动画 |
| `渐入时间 (秒)` | 打开时 α: 0→1 的时长 |
| `渐出时间 (秒)` | 关闭时 α: 1→0 的时长 |

如果需要自定义动画，创建 `EUIPage` 子类并 override `OnShow()` / `OnHide()` 返回协程。

---

### 3.4 EUIEnums

#### PageState（页面生命周期状态）

| 值 | 说明 |
|------|------|
| `Unloaded` | 未加载 |
| `Loading` | 正在加载 Prefab |
| `Loaded` | 已加载但未显示 |
| `Showing` | 正在播放打开动画 |
| `Opened` | 已打开，可见可交互 |
| `Paused` | 被上方页面遮挡（暂停） |
| `Hiding` | 正在播放关闭动画 |
| `Closed` | 已关闭（等待销毁/回池） |

#### PageType（页面行为模式）

| 值 | 说明 |
|------|------|
| `MainPage` | 全屏主页面，替换当前 MainPage |
| `Popup` | 弹窗，叠加在当前 MainPage 上方 |
| `TopMost` | 顶级弹窗（Loading、全局提示） |
| `SubPage` | 子页面，嵌入父页面指定区域 |
| `Overlay` | 覆盖层（引导遮罩、点击特效层） |

#### UILayer（渲染层级）

| 值 | 数值 | 说明 |
|------|------|------|
| `Background` | 0 | 背景层 |
| `Normal` | 100 | 普通层 |
| `Popup` | 200 | 弹窗层 |
| `TopMost` | 300 | 顶层 |

---

### 3.5 EUIManager（页面生命周期引擎）

继承 `EmberMonoSingleton<EUIManager>`。

**导入方式：** `EUIManager.Instance`

| API | 说明 |
|------|------|
| `OpenPage(page, pageDef, args, onComplete)` | 打开页面（Init → PlayShow → Opened） |
| `ClosePage(page, onComplete)` | 关闭页面（PlayHide → Cleanup → Destroy） |
| `PausePage(page)` | 暂停页面 |
| `ResumePage(page)` | 恢复页面 |
| `ReopenPage(page, args, onComplete)` | 重新打开已关闭页面（OnReopen → PlayShow） |
| `StartPageCoroutine(routine)` | 启动页面协程（EUIPage 不是 MonoBehaviour） |
| `EnsureLayerCanvas(layer)` | 确保指定层级 Canvas 存在 |
| `ShowBgMask(sortingOrder, onClick)` | 显示背景遮罩（Popup 弹窗时自动调用） |
| `HideBgMask(mask)` | 隐藏背景遮罩 |
| `HandleEscapeKey()` | 处理返回键（按层级从高到低） |
| `EnqueuePageOperation(op)` | 将页面操作加入安全队列（避免迭代中修改集合） |
| `AdjustCanvasScaler(go)` | **[静态]** 动态调整指定 GameObject 的 CanvasScaler.matchWidthOrHeight（屏幕适配） |
| `ActivePages` | 活跃页面列表（只读） |
| `PageContext` | 页面上下文（MainPage + Popup 栈） |
| `UIRoot` | UI 根节点 Transform |
| `UICamera` | UI 摄像机 |
| `FrameTimeBudgetMs` | 每帧操作处理的最大时间预算（ms），默认 100。增大减少延迟帧数，减小保证帧率 |
| `AutoAdjustCanvasScaler` | 是否自动屏幕适配（默认 true）。设为 false 跳过自适应逻辑 |
| `ResourceProvider` | 资源加载提供者（可替换为自定义实现） |
| `TransitionHandler` | 过渡动画处理器（可替换为自定义实现） |

---

### 3.6 EUIPageContext（页面栈管理）

管理 MainPage 栈 + Popup 层关系，自动计算 SortingOrder。

**导入方式：** `EUIManager.Instance.PageContext`

| API | 说明 |
|------|------|
| `PushMainPage(page)` | 推入 MainPage（暂停旧页面） |
| `PopMainPage(page)` | 关闭 MainPage（恢复上一页面） |
| `AddPopup(popup)` | 添加 Popup（隐藏下方 MainPage） |
| `RemovePopup(popup)` | 移除 Popup（恢复上一 Popup/MainPage） |
| `AddTopMost(page)` | 添加 TopMost 页面 |
| `RemoveTopMost(page)` | 移除 TopMost 页面 |
| `GetTopPopup()` | 获取最顶层 Popup |
| `GetTopTopMost()` | 获取最顶层 TopMost |
| `HasPopup()` | 是否有 Popup 正在显示 |
| `ForEachVisiblePage(action)` | 从高到低遍历所有可见页面 |
| `CloseAll()` | 关闭所有页面 |
| `CurrentMainPage` | 当前活跃的 MainPage |
| `MainPageCount` | MainPage 栈数量 |

**渲染裁剪：** 被遮挡页面自动设置 `canvas.planeDistance = 100000`（推到相机远裁面），
而不是仅设 CanvasGroup alpha=0。恢复时 planeDistance 还原为 `EUIPageDef.Layer`。

---

### 3.7 EUIPageRouter（页面路由）

应用层路由：决定"打开什么、何时打开"，管理层级遮挡、BG Mask、返回键。

**导入方式：** `EUIPageRouter.Instance`

| API | 说明 |
|------|------|
| `ShowMainPage(pageDef, args, onComplete)` | 显示主页面 |
| `ShowPopup(pageDef, args, onComplete)` | 显示弹窗（自动创建 BG Mask） |
| `ShowTopMost(pageDef, args, onComplete)` | 显示顶级弹窗（Loading 等） |
| `ShowSubPage(pageDef, parentPage, args, onComplete)` | 显示子页面 |
| `PreloadPage(pageDef, args, onComplete)` | 预加载页面（Init 但不 PlayShow），后续打开零延迟 |
| `ClosePage(page, returnValue)` | 关闭指定页面 |
| `CloseTopPopup()` | 关闭最顶层 Popup |
| `CloseAllPopups()` | 关闭所有 Popup |
| `GetReturnValue(page)` | 获取页面返回值（一次性） |

**静态扩展点：**
| 扩展点 | 说明 |
|------|------|
| `EUIPageRouter.OnPageCreated` | 页面创建回调，uiextension 包在此注入 Logic 层 |

**SubPage 排序：** `ShowSubPage` 自动计算 Canvas.sortingOrder（父页面最大 + 50）。详见 3.1 节 "SubPage 排序规则"。

---

### 3.8 IEUIView（页面契约接口）

所有页面的基础契约，`EUIManager` 只认这个接口。

| 方法 | 说明 |
|------|------|
| `Init(object args)` | 数据阶段：填文字、设图片、注册事件 |
| `PlayShow()` | 表现阶段：播放打开动画 |
| `PlayHide()` | 表现阶段：播放关闭动画 |
| `Cleanup()` | 数据阶段：注销事件、释放引用 |
| `OnPause()` | 被其他页面遮挡 |
| `OnResume()` | 重新回到顶层 |
| `OnReopen(object args)` | 关闭后重新打开（跳过 Init） |
| `TryEscapeKeyClose()` → `bool` | 返回键处理 |
| `IsInitialized` | 是否已完成 Init |
| `IsOpened` | 是否处于 Opened 状态 |
| `State` | 当前 PageState |

---

### 3.9 EUIPageDef（页面定义元数据）

```csharp
public static readonly EUIPageDef MainMenu = new EUIPageDef("ui/main_menu", UILayer.Normal, PageType.MainPage);
public static readonly EUIPageDef Settings = new EUIPageDef("ui/settings", UILayer.Popup, PageType.Popup);
```

| 构造参数 | 说明 |
|------|------|
| `prefabPath` | Prefab 资源路径 |
| `layer` | 渲染层级（UILayer 枚举或 int） |
| `pageType` | 页面行为模式（默认 MainPage） |
| `overlaySortingOrder` | **[可选]** Overlay 页面固定排序值（仅 PageType.Overlay 时有效） |

**Overlay 排序示例：**
```csharp
// 引导遮罩在引导 UI 之下
new EUIPageDef("ui/guide_mask",  UILayer.Normal, PageType.Overlay, overlaySortingOrder: 19999);
new EUIPageDef("ui/guide_main",  UILayer.Normal, PageType.Overlay, overlaySortingOrder: 20000);
```

---

### 3.10 EUIBgMaskPool（背景遮罩对象池）

Popup 弹窗时自动创建半透明背景遮罩（防止点击穿透），关闭时回池复用。

| API | 说明 |
|------|------|
| `Get(sortingOrder, onClick)` | 获取遮罩（优先池，空则创建） |
| `Return(mask)` | 归还遮罩到池 |
| `Clear()` | 销毁所有池内遮罩 |

---

### 3.11 可替换组件

| 接口 | 默认实现 | 说明 |
|------|------|------|
| `IEUIResourceProvider` | `DefaultUIResourceProvider` | Prefab 加载（默认用 `EmberResourceManager`） |
| `IEUITransitionHandler` | `DefaultUITransitionHandler` | 页面切换动画（默认无动画） |

设置方式：`EUIManager.Instance.ResourceProvider = new MyProvider();`

---

### 3.12 屏幕适配（CanvasScaler 自适应）

`EUIManager.AutoAdjustCanvasScaler` 默认开启。框架每帧检测分辨率变化，
变化时自动对所有活跃页面调用 `AdjustCanvasScaler(GameObject)`。

**生效条件：** 仅对 `ScaleWithScreenSize` + `MatchWidthOrHeight` 模式的 CanvasScaler 生效。

**匹配策略：**
| 屏幕方向 | 条件 | matchWidthOrHeight |
|----------|------|--------------------|
| 竖屏 | 宽高比 > 9:16 | 1（匹配高度） |
| 竖屏 | 宽高比 ≤ 9:16 | 0（匹配宽度） |
| 横屏 | 宽高比 > 16:9 | 1（匹配高度） |
| 横屏 | 宽高比 ≤ 16:9 | 0（匹配宽度） |

横竖屏交叉时（参考分辨率和当前屏幕方向不同），自动交换参考分辨率的 x/y。

**手动调用：**
```csharp
EUIManager.AdjustCanvasScaler(pageGameObject);
```

**关闭自适应：**
```csharp
EUIManager.Instance.AutoAdjustCanvasScaler = false;
```

**调整帧预算：**
```csharp
// 增大可减少页面打开延迟帧数，减小可保证帧率（默认 100ms）
EUIManager.Instance.FrameTimeBudgetMs = 200;
```

---

### 3.13 Profiler 集成

框架在关键路径自动插入 `Profiler.BeginSample/EndSample`，Unity Profiler 中可见以下标记：

| Profiler 标记 | 所在方法 |
|---------------|----------|
| `EUIManager.Update` | 主循环 |
| `EUIManager.ProcessPendingOperations` | 操作队列处理 |
| `EUIManager.OpenPage` | 页面打开（setup 阶段） |
| `EUIManager.ClosePageInternal` | 页面关闭（setup 阶段） |
| `EUIPage.Init` | 页面 Init 数据阶段 |
| `EUIPage.PlayShow` | 页面 Show 表现阶段 |
| `EUIPage.PlayHide` | 页面 Hide 表现阶段 |
| `EUIPage.Cleanup` | 页面清理阶段 |
| `EUIPageRouter.ProcessShowRequest` | 路由分发 |
| `EUIPageRouter.RouteAndOpenPage` | 路由注册 + 打开调度 |

---

## 四、事件系统

### 4.1 EUIEvents（EmberEventBus 键值）

| 常量 | 值 | 说明 |
|------|------|------|
| `UIManagerReady` | 5000 | UI 框架初始化完成 |
| `UIManagerShutdown` | 5001 | UI 框架即将销毁 |
| `UIPageRouterReady` | 5002 | 页面路由就绪 |

通过 `EmberEventBus` 播报：
```csharp
EmberEventBus.Emit(EUIEvents.UIManagerReady);
```

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
EUIObserver.OnPageOpened.Subscribe(evt =>
{
    Debug.Log($"页面打开: {evt.Page}");
}).AddTo(this);
```

### 4.3 输入事件

| 类 | 说明 |
|------|------|
| `EventTriggerListener` | 增强版 EventTrigger，支持拖拽、长按等 |
| `DragEventTriggerListener` | 拖拽事件专用监听器 |

---

## 五、命名规范速查

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

---

## 六、完整生命周期流程图

```
构造
  new EUIPage(go)
    → CreateLogic(ControlMap callback)
      → Logic.OnBind()           ← 取控件引用
      → Logic.RegisterChildLogic (嵌套 UIBinding)

打开
  Init(args)
    → OnInitialize(args)         ← 框架层
    → Logic.OnInit()             ← 注册事件
    → Logic.OnOpen(args)         ← 接收参数
    → Logic.OnReset()            ← 重置 UI
  PlayShow()
    → Logic.OnShow()             ← 刷新数据
    → OnShow()                   ← 打开动画 (null=无动画)
      → CompleteShow()           ← α=1, State→Opened

运行时
  OnPause() → OnPaused() → Logic.OnPause()     ← 被遮挡
  OnResume() → OnResumed() → Logic.OnResume()   ← 恢复

关闭
  PlayHide()
    → Logic.OnHide()             ← 停止刷新
    → OnHide()                   ← 关闭动画 (null=无动画)
      → CompleteHide()           ← α=0, State→Closed
  Cleanup()
    → OnCleanup()                ← 框架层清理
    → Logic.OnClose()            ← 持久化
    → Logic.OnReset()            ← 重置
    → Logic.OnDispose()          ← 注销事件！
    → Dispose → Destroy(go)
```

---

## 七、快速参考卡片

### 新手三步接入

1. **预制体命名**：子控件加 `m_` 前缀（如 `m_Btn_Start`），背景/装饰不加前缀
2. **Inspector 配置**：在 EUIBinding 面板填类名、页面名 → 点"生成代码"
3. **写业务逻辑**：在生成的 `.cs` 中 override `OnBind()` + `OnInit()` + `OnDispose()`

### 框架包装类对照

| 控件 | ControlMap 中获取的类型 | 建议新建脚本类型 |
|------|------------------------|-----------------|
| Button | `Button` 或 `EUIButton` | `EUIButtonEx`（需状态节点） |
| Toggle | `Toggle` 或 `EUIToggle` | `EUIToggleEx`（需状态节点） |
| Text | `TextMeshProUGUI` 或 `EUIText` | — |
| Image | `Image` 或 `EUIImage` | `EUIImageEx`（需序列帧/不规则点击） |
| InputField | `TMP_InputField` 或 `EUIInputField` | — |
| Slider | `Slider` 或 `EUIProgressBar` | — |
