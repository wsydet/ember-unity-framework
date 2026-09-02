# Ember vs Burner UI 框架对比分析

> 更新：2026-08-11 | 对比源：ember-unity-framework vs burner GameUIManager

---

## 一、架构对比

| 维度 | Burner | Ember | 状态 |
|------|--------|-------|------|
| 项目层单例 | `GameUIManager : Singleton<T>` + `IManager, IGameUpdate, IGameLateUpdate` | `EUIPageRouter : EmberMonoSingleton<T>` + `IEmberManager` | ✅ 已实现 |
| 共享包层核心 | `BurnerUIManager` (com.burner.uiextension) | `EUIManager : EmberMonoSingleton<T>` + `IEmberManager` | ✅ 已实现 |
| 页面包装类 | `GamePage` (纯 C#，包装 Prefab) | `EUIPage` (纯 C#，包装 Prefab) | ✅ 已实现 |
| 业务逻辑基类 | `GameUILogic` | `EUILogic` | ✅ 已实现 |
| 页面元数据 | `PageDefineDictionary` (自动生成) | `EUIPageDef` (手动声明) | ✅ 已实现 |
| Update 驱动 | `IGameUpdate` / `IGameLateUpdate` 接口 | `EUIManager.Update()` / `LateUpdate()` | ✅ 已实现 |

**架构差异：**
- Burner 是双层架构：`GameUIManager`（项目层）→ `BurnerUIManager`（包层），项目层做路由/队列/相机管理，包层做生命周期引擎
- Ember 同样是双层：`EUIPageRouter`（路由层）→ `EUIManager`（引擎层），职责划分一致

---

## 二、页面类型对比

| 页面类型 | Burner (PageFlags) | Ember (PageType) | 状态 |
|----------|---------------------|------------------|------|
| 全屏主页面 | `MainPage = 1` | `MainPage` | ✅ |
| 模态弹窗 | `Popup = 2` | `Popup` | ✅ |
| 全屏弹窗 | `Popup` 与附加逻辑组合 | `FullScreenPopup` | ✅ 互斥类型 |
| 顶级弹窗 | `TopMost = 4` | `TopMost` | ✅ |
| 子页面 | `SubPage = 8` | `SubPage` | ✅ |
| 独立页面 | `FreePage = 16`（硬编码固定排序） | `Overlay` | ✅ 概念对齐 |
| Flags 组合 | 支持 `Popup \| TopMost` | 不支持组合（独立枚举值） | ⚠️ 设计差异 |

**差异说明：**
- Burner 用 `[Flags]` 枚举，支持 `Popup | TopMost` 组合，灵活但容易出错
- Ember 用独立枚举值，`PageType.TopMost`、`PageType.Popup` 和 `PageType.FullScreenPopup` 互斥；EUIBinding 与运行时共用 PageType，避免配置层 Flags 产生非法组合
- Burner 的 `FreePage` 有硬编码的 per-page 排序字典；Ember 的 `Overlay` 没有 per-page 排序，所有 Overlay 共享一个列表

---

## 三、渲染排序机制

| 特性 | Burner | Ember | 状态 |
|------|--------|-------|------|
| MainPage 基础排序 | `MainPageBaseOrder = 1000` | `MainPageBaseOrder = 1000` | ✅ 相同 |
| 页面间递增步长 | `PageGrowStep = 500` | `PageGrowStep = 500` | ✅ 相同 |
| TopMost 基础排序 | `TopMostBaseOrder = 25000` | `TopMostBaseOrder = 25000` | ✅ 相同 |
| Popup 排序 | 当前 MainPage + PopupCount × 500 | 当前 MainPage + PopupCount × 500 | ✅ 相同 |
| MainPage Z (planeDistance) | `MainPageZ = 250` | 无 | ❌ 未实现 |
| Popup 初始 Z | `200`，递减 `-10` | 无 | ❌ 未实现 |
| 渲染裁剪方式 | `planeDistance = 100000`（推到远裁面） | `CanvasGroup.alpha = 0` + `blocksRaycasts = false` | ⚠️ 方式不同 |
| FreePage 独立排序 | 硬编码字典（如 `UIGuideMainPage → 20000`） | Overlay 无 per-page 排序 | ❌ 未实现 |
| SubPage 排序 | `父页面 + 50`（`SubPageOrderGrowStep = 50`） | 未实现独立排序 | ❌ 未实现 |
| `ICanvasSortingOrderHandler` | ✅ 通知子组件 | ✅ 已从 burner 迁移 | ✅ |
| `RelativeCanvasOrder` | ✅ | ✅ 已从 burner 迁移 | ✅ |

**关键差异：**
1. **planeDistance 裁剪**：Burner 把被遮挡页面推到 `planeDistance = 100000`（远裁面之外），GameObject 保持激活但不渲染。这是为了避免 SetActive 的开销。Ember 使用 CanvasGroup alpha + blocksRaycasts 来隐藏，更简单但缺少这个优化。
2. **FreePage/Overlay 排序**：Burner 为每个 FreePage 硬编码了固定 sortingOrder；Ember 的 Overlay 没有 per-page 排序能力。
3. **SubPage 排序**：Burner 的 SubPage 从父页面 `sortingOrder + 50` 递增，有独立的 `curSubPageOrder` 追踪。Ember 的 SubPage 目前没有自己的 sortingOrder 管理。

---

## 四、页面栈管理

| 特性 | Burner | Ember | 状态 |
|------|--------|-------|------|
| MainPage 栈 | `PageContext.MainPageList.Stack` | `EUIPageContext._mainPageStack` | ✅ |
| Push/Pop MainPage | `RegisterPage` / `CloseMainPage` | `PushMainPage` / `PopMainPage` | ✅ |
| Popup 按 MainPage 分组 | 每个 StackEntry 有 Popups 列表 | 每个 StackEntry 有 Popups 列表 | ✅ |
| TopMost 独立列表 | `curMainCtx.TopMostPopups` | `_topMostList` | ✅ |
| **MainPageGroups（命名分组）** | ✅ 支持多组并行的 MainPage 栈 | ❌ 单栈 | ❌ 未实现 |
| SubPage 关闭时刷新排序 | `RefreshSubPageOrder()` 递归计算 | ❌ | ❌ 未实现 |
| `SetPageVisible` 实现 | `canvas.enabled` + `canvasRaycaster.enabled` | `CanvasGroup.alpha` + `blocksRaycasts` + `interactable` | ⚠️ 方式不同 |

**MainPageGroups 说明：** Burner 支持按 `group` 名称管理多组并行的 MainPage 栈（如 HUD 层和主内容层各有一组独立的页面栈）。Ember 目前是单一 MainPage 栈。这是一个重要的架构级差异。

---

## 五、BG Mask（背景遮罩）

| 特性 | Burner | Ember | 状态 |
|------|--------|-------|------|
| 对象池 | `GameObjectPool`（Template 实例化） | `EUIBgMaskPool`（动态创建） | ✅ |
| Popup 自动创建 | `GameUIBase` 在 `OnOpen` 中调用 `CreateBgMask` | `EUIPageRouter.ShowBgMaskForPopup` | ✅ |
| 点击关闭弹窗 | `bgMaskBtn.onClick` → `ClosePage` | `mask onClick` → `ClosePage` | ✅ |
| 模板 + 缩放 | 模板预制体 × `1.1` 缩放 | 动态创建 `new GameObject` | ⚠️ 方式不同 |
| SortingOrder | mask 的 sortingOrder = popup - 1 | mask 的 sortingOrder = popup - 1 | ✅ |

---

## 六、页面队列机制

| 特性 | Burner | Ember | 状态 |
|------|--------|-------|------|
| 显示队列 | `Queue<PageParams> mShowPageQueue` | `Queue<ShowRequest> _showQueue` | ✅ |
| 自动消费时机 | UIMainPage 打开 + 无 Popup + 无加载中 | `ProcessShowQueue()` 每帧消费 | ⚠️ 行为不同 |
| Enqueue 接口 | `EnqueuePage()` / `EnqueuePopupInternal()` | 无独立的 Enqueue API | ❌ 未实现 |
| 引导/跳过阻止消费 | `GuideNewModule.IsGuiding` / `GuideJumpModule.IsJumping` | 无 | ❌ 未实现（业务逻辑） |
| 队列条件判断 | 当前 MainPage 非 UIMainPage 或有 Popup | 无条件（直接加入队列） | ⚠️ 行为不同 |

**关键差异：** Burner 的队列有条件触发：只有当前 MainPage 不是 `UIMainPage` 或有 Popup 时才排队；Ember 的 ShowRequest 无条件进入队列。Burner 的队列在 Popup 全部关闭且 MainPage 恢复为 `UIMainPage` 时才消费；Ember 的队列每帧都在消费。

---

## 七、页面生命周期

| 生命周期阶段 | Burner GameUILogic | Ember EUILogic | 状态 |
|-------------|---------------------|---------------------|------|
| 控件绑定 | `Initialize` → `InitControlMap` → `OnBind` | `CreateLogic` → `ControlMap` 填充 → `OnBind()` | ✅ |
| 初始化 | `DoInit()` → `OnInit()` | `BroadcastInit()` → `OnInit()` | ✅ |
| 打开参数 | `DoOpen(param)` → `OnOpen(param)` | `BroadcastOpen(args)` → `OnOpen(args)` | ✅ |
| 显示 | `DoShow()` → `OnShow()` | `BroadcastShow()` → `OnShow()` | ✅ |
| 隐藏 | `DoHide()` → `OnHide()` | `BroadcastHide()` → `OnHide()` | ✅ |
| 暂停 | `OnPause()` | `BroadcastPause()` → `OnPause()` | ✅ |
| 恢复 | `OnResume()` | `BroadcastResume()` → `OnResume()` | ✅ |
| 关闭 | `DoClose()` → `OnClose()` | `BroadcastClose()` → `OnClose()` | ✅ |
| 重置 | `OnReset()` | `BroadcastReset()` → `OnReset()` | ✅ |
| 释放 | `DoDispose()` → `OnDispose()` | `BroadcastDispose()` → `OnDispose()` | ✅ |
| 重新打开 | `DoReopen(param)` | `OnReopen(args)` → `BroadcastOpen` → `PlayShow` | ✅ |
| Update | `DoUpdate()` / `DoLateUpdate()` | `OnUpdate()` / `OnLateUpdate()` | ✅ |
| **预加载** | `DoPreload()` / `NeedPreload` / `HasComponentPreload` | 无 | ❌ 未实现 |
| **延迟可见** | `PostponeSetVisible` / `DoPostponeSetVisible` | 无 | ❌ 未实现 |
| **渲染可见性变更** | `DoChangeRenderVisible(bool)` | 无 | ❌ 未实现 |
| **OnBecomeVisible** | `DoBecomeVisible()` | 无 | ❌ 未实现 |
| **OnReshow** | `GameUIBase.OnReshow(param)` | `OnReopen` 部分覆盖 | ⚠️ 概念不同 |
| **子 Logic 注册** | 自动扫描子 `GameUIBinding` | `RegisterChildLogic(EUILogic)` | ✅ |

---

## 八、分帧加载（Time-Slice）

| 特性 | Burner | Ember | 状态 |
|------|--------|-------|------|
| 时间预算 | `MaximalFrameTimeBudget = 500ms` | `TimeBudgetMs = 10` | ⚠️ 数值不同 |
| 分帧加载阶段 | `LoadStages`: OnResLoad → OnInit → OnLoad → Loaded | 无阶段划分 | ❌ 未实现 |
| 累积时间追踪 | `accumulatedTime` + `RegisterTimeSliceUsed` | `_loadTimer` + 简单 break | ⚠️ 简化版 |
| `TryConsumeTimeSlice` token 模式 | `LoadTimeToken : IDisposable`，自动计时 | 无 | ❌ 未实现 |
| 挂起阶段恢复 | `GamePage.OnUpdate()` 检查 `curStage < pendingStage` 继续执行 | `ProcessPendingOperations` 简单 break | ❌ 未实现 |
| 高优先级加载 | `RequestHighPriorityLoad` / `FinishHighPriorityLoad`（设 `ThreadPriority.High`） | 无 | ❌ 未实现 |

**关键差异：** Burner 的分帧加载是一个完整的系统：Prefab 加载完成后，`DoOnInit()` → `DoLoad()` → `OnLoaded` 三个阶段可以分散到多帧执行，每帧消耗的时间由 `TryConsumeTimeSlice` 控制（默认 500ms）。Ember 的 `ProcessPendingOperations` 只有 10ms 预算，且没有阶段化的加载流程——Ember 的页面 Prefab 是在 Router 层同步 `Instantiate` 的，没有分帧初始化。

---

## 九、页面关闭与复用

| 特性 | Burner | Ember | 状态 |
|------|--------|-------|------|
| 延迟销毁 | `isClosing` + `DestroyValue`（秒）+ `closeTime` | `_isClosing` + `_destroyDelay` + `_closeTime` | ✅ |
| 默认销毁延迟 | `DefaultDestoryDelay = 1` 秒 | `_destroyDelay = 30f` 秒 | ⚠️ 数值不同 |
| 延迟销毁中复用 | 检查 `isClosing` → 重置标志 → 复用 | `FindReusablePage(prefabPath)` → `CancelClosing()` | ✅ |
| 强制立即销毁 | `DoDispose()` | `ForceDispose()` | ✅ |
| 关闭事件延迟派发 | `pendingCloseEvents` + `openingCnt` 计数器 | 无 | ❌ 未实现 |

---

## 十、CanvasScaler 自适应

| 特性 | Burner | Ember | 状态 |
|------|--------|-------|------|
| 动态调整 `matchWidthOrHeight` | `AdjustCanvasScaler(GameObject)` — 根据屏幕宽高比切换 | 无 | ❌ 未实现 |
| 折叠屏/旋转检测 | `displayResolution` 变更检测 + `OnScreenResolutionChanged` 事件 | 无 | ❌ 未实现 |
| 自适应触发时机 | `BurnerUIManager.Update()` 中检测分辨率变化后自动调用 | 无 | ❌ 未实现 |

**说明：** Burner 的 `AdjustCanvasScaler` 在每次 Update 中检测屏幕分辨率变化，对每个页面的 CanvasScaler（仅 `MatchWidthOrHeight` 模式）动态调整 `matchWidthOrHeight`，支持折叠屏和横竖屏切换。Ember 没有这个能力。

---

## 十一、项目层扩展（GameUIBase 对标）

Burner 的 `GameUIBase` 继承 `GameUILogic`，提供了项目层扩展功能。Ember 中没有直接对标物，这些功能分散在不同层：

| GameUIBase 功能 | Burner 实现 | Ember 实现 | 状态 |
|-----------------|-------------|------------|------|
| BG Mask 创建/移除 | `CreateBgMask()` / `RemoveBgMask()` | `EUIPageRouter.ShowBgMaskForPopup` | ✅ 在 Router 层 |
| 子页面管理 | `ShowSubPage()` / `CloseSubPage()` / `CloseAllSubPages()` | `EUIPage.RegisterSubPage` + `EUIPageRouter.ShowSubPage` | ✅ |
| 事件自动清理 | `AddEvent()` 追踪 + `RemoveAllEvents()` | 无自动追踪 | ❌ 未实现 |
| SafeArea 适配 | `RegisterSafeArea()` / `UnregisterSafeArea()` | `BurnerSafeArea` 组件已迁移到 uiextension | ⚠️ 组件存在但无自动注册 |
| GameState 监听 | `RegisterGameState()` / `UnregisterGameState()` | 无 | ❌ 未实现 |
| ESC 键处理 | `OnEscapeKey()` 覆盖 | `EUIPage.OnEscapeKey()` + `EUIManager.HandleEscapeKey` | ✅ 在框架层 |
| 页面音乐 | `PlayPageMusic()` | 无 | ❌ 未实现 |
| OnReshow | `OnReshow(param)` — 已存在页面被重新打开 | `OnReopen` 部分覆盖 | ⚠️ 概念不同 |

---

## 十二、渲染与视觉效果

| 特性 | Burner | Ember | 状态 |
|------|--------|-------|------|
| Animator 状态管理 | `SetActive` 中管理 Animator 的 `In`/`Ani` 状态 + `StopPlayback` | 无 | ❌ 未实现 |
| ParticleSystem 管理 | `SetActive` 中自动 Play/Stop + Clear 粒子 | 无 | ❌ 未实现 |
| UIParticle 管理 | `SetActive` 中自动 Play/Stop + Clear | 无 | ❌ 未实现 |
| 首次打开动画区分 | `ShowHistory` 追踪 → 首次播 `In`，后续播 `Ani` | 无 | ❌ 未实现 |
| 背景模糊 | `EnableBlurPages()` / `EnableNodeBlurEffect()` | 无 | ❌ 未实现 |
| ScreenShot | `GetScreenShot()` → `RenderTexture` | 无 | ❌ 未实现 |
| 自定义动画 | `OnShow()` / `OnHide()` 协程 override | `OnShow()` / `OnHide()` 协程 override | ✅ |
| 预设渐入渐出 | 无（依赖 Animator） | `SetPresetFade()` UniTask alpha 动画 | ✅ Ember 独有 |

---

## 十三、相机与场景管理

| 特性 | Burner | Ember | 状态 |
|------|--------|-------|------|
| 主相机可见性管理 | `HandleCameraVisibilityOnPageOpen/Close` + `pagesThatKeep3DSceneVisible` 白名单 | 无 | ❌ 未实现 |
| 3D 场景相机 | `GameDefaultCinemachineCamera`（Cinemachine） | 无 | ❌ 未实现 |
| Scene Volume 权重控制 | `SetSceneVolumeWeight()` + `ApplyVolumeWeights()` | 无 | ❌ 未实现 |
| MapFog 可见性 | `SetMapFogVolumeVisible()` | 无 | ❌ 未实现 |
| HUD 层 | `uiHUDLayer` 独立 RectTransform | 无 | ❌ 未实现 |
| 3D 父节点 | `ThreeDimensionalParent` | 无 | ❌ 未实现 |

---

## 十四、监控与调试

| 特性 | Burner | Ember | 状态 |
|------|--------|-------|------|
| 页面加载计时 | `PageLoadTiming`（AssetLoadTime, InitTime, OpenTime, TotalLoadTime, FirstOpened） | 无 | ❌ 未实现 |
| 栈信息日志 | `GetStacksLog()` | 无 | ❌ 未实现 |
| 队列调试信息 | `GetCurrentPageQueueDebugStr()` | 无 | ❌ 未实现 |
| Profiler 标记 | `Profiler.BeginSample/EndSample` 包裹各阶段 | 无 | ❌ 未实现 |
| 全局点击事件 | `GlobalEvents.AddClickEventListener` → 记录所有按钮点击 | 无 | ❌ 未实现 |

---

## 十五、异步操作

| 特性 | Burner | Ember | 状态 |
|------|--------|-------|------|
| 页面打开异步 | `PageAsyncOperations` → `STTask<GameUILogic> OnShow` | `Action<EUIPage> onComplete` 回调 | ⚠️ Task vs 回调 |
| 页面关闭异步 | `PageAsyncOperations` → `STTask<object> OnClose` | `Action onComplete` 回调 | ⚠️ Task vs 回调 |
| 加载完成等待 | `ILoaderHandle` + `STTask` | 无 | ❌ 未实现 |

---

## 十六、总结：已实现 vs 缺失

### ✅ 已完善实现（核心架构对齐）

| 模块 | 覆盖率 |
|------|--------|
| 双层架构（Router + Manager） | 100% |
| 页面类型（Main/Popup/TopMost/Sub/Overlay） | 100% |
| 渲染排序常量（1000/500/25000） | 100% |
| MainPage 栈 + Popup 分组 + TopMost 列表 | 100% |
| BG Mask 对象池 | 100% |
| 页面生命周期（Init→Show→Pause→Resume→Hide→Close→Dispose→Reopen） | 95% |
| 延迟销毁 + 复用 | 100% |
| 预设渐入渐出动画 | 100%（Ember 独有） |
| ICanvasSortingOrderHandler | 100% |
| EUIPage 子类 override 动画钩子 | 100% |
| Escape Key 逐层处理 | 100% |
| 安全操作队列 | 100% |
| EUIObserver (UniRx) 事件 | 100% |
| EmberEventBus 集成 | 100% |
| EUI 组件体系（Button/Toggle/Image/Text 等 14 种控件） | 100% |
| ControlMap 自动绑定 + 代码生成 | 100% |

### ❌ 缺失的功能（按优先级排列）

#### P0 — 影响页面正确渲染

1. **SubPage 独立排序** — SubPage 需要从父页面 `sortingOrder + 50` 递增
2. **FreePage/Overlay per-page 排序** — 不同 Overlay 页面需要不同的固定 sortingOrder
3. **planeDistance 渲染裁剪** — 被遮挡页面推远裁面而非仅改 alpha

#### P1 — 影响性能和生产可用性

4. **分帧加载（Time-Slice）** — 页面初始化分阶段跨帧执行，防止卡顿（当前 Ember `Instantiate` 是同步的）
5. **CanvasScaler 自适应** — 折叠屏/旋转屏适配
6. **MainPageGroups** — 多组并行 MainPage 栈（HUD 层 vs 内容层）

#### P2 — 影响功能完整性

7. **预加载机制** — `NeedPreload` / `HasComponentPreload` / `DoPreload`
8. **延迟可见（PostponeSetVisible）** — OnShow 后再 SetActive
9. **ShowHistory 首次打开动画区分** — In vs Ani 状态
10. **页面操作挂起队列（PageTargetState）** — 加载中收到的 Show/Hide/Close 排队执行
11. **关闭事件延迟派发** — `pendingCloseEvents` + `openingCnt`
12. **页面加载计时（PageLoadTiming）** — 性能监控

#### P3 — 可选增强

13. **Animator / ParticleSystem 自动管理** — SetActive 时自动控制
14. **背景模糊效果** — `EnableBlurPages` / `EnableNodeBlurEffect`
15. **回调 → STTask 异步升级** — `PageAsyncOperations` 模式
16. **HUD 独立层** — `uiHUDLayer`
17. **事件自动清理追踪** — `AddEvent` + `RemoveAllEvents`
18. **相机可见性管理** — `pagesThatKeep3DSceneVisible` 白名单
19. **SafeArea 自动注册** — 页面显示/隐藏时自动注册/注销
20. **GameState 监听** — 页面级 GameState 注册
21. **全局点击事件监听** — GlobalEvents
22. **Profiler 标记** — 各阶段 `Profiler.BeginSample`
