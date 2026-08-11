# Ember UI 框架 —— 缺失功能分析与实现计划

> 版本：0.2 | 更新：2026-08-11 | 基于 [ember-vs-burner-ui-comparison.md](ember-vs-burner-ui-comparison.md)
>
> **第一轮（🔴 必须做）已完成：** SubPage 排序、分帧加载、操作挂起队列、CanvasScaler 自适应
>
> **第二轮（🟡 应该做）已完成：** Overlay 排序、PlaneDistance 裁剪、预加载、加载计时、Profiler、事件追踪

---

## 评估框架

每个缺失功能按四个维度评估：

| 标记 | 含义 | 决策 |
|------|------|------|
| 🔴 必须做 | 框架正确性/基础可用性依赖此功能 | 列入本轮实现 |
| 🟡 应该做 | 显著改善框架质量，不做的代价随时间累积 | 列入下一轮 |
| 🟢 可以做 | 锦上添花，特定场景有价值 | 延后，有人提再补 |
| ⚪ 不需要 | 属于业务层/项目特定逻辑，不应进入框架 | 不做 |

---

## 一、🔴 必须做（4 项）

### 1. SubPage 独立排序 ✅ 已完成

**现状：** ~~[EUIPageContext.cs:73-156](Assets/Ember/UI/Runtime/EUIPageContext.cs) 完全没有 SubPage 相关的 sortingOrder 逻辑。~~`EUIPageRouter.RouteAndOpenPage` 对 SubPage 只调了 `parent.RegisterSubPage(page)`，不设 Canvas.sortingOrder。

**问题：** SubPage 的 Canvas.sortingOrder 是默认值 0，会被所有其他页面遮挡。SubPage 必须在父页面之上渲染。

**Burner 做法：**
- `SubPageOrderGrowStep = 50`
- `curSubPageOrder` 追踪下一个可用的排序值
- 每个 SubPage 从父页面的 `curSubPageOrder` 取值，然后 `curSubPageOrder += 50`
- 关闭 SubPage 时递归 `RefreshSubPageOrder()` 重新计算

**实现方案：**
1. 在 `EUIPageContext` 增加 `internal const int SubPageOrderGrowStep = 50;`
2. 在 `EUIPageRouter.RouteAndOpenPage` 的 `SubPage` 分支中，计算并设置 Canvas.sortingOrder：
   - 从 parentPage 找到顶层非 SubPage 页面
   - 遍历已有 subPages 找到最大 sortingOrder
   - 新 SubPage = max(已有最大, parentPage.sortingOrder) + SubPageOrderGrowStep
3. 在 `EUIPage.UnregisterSubPage` 中触发排序重算

**改动范围：** `EUIPageRouter.cs`（~15 行）、`EUIPageContext.cs`（新增方法 ~30 行）

---

### 2. 分帧加载（Time-Slice Loading） ✅ 已完成

**现状：** ~~[EUIManager.cs:53-54](Assets/Ember/UI/Runtime/EUIManager.cs) 有 `TimeBudgetMs = 10`，但只用于操作队列的帧预算。~~页面 Prefab 的 `Instantiate` + `Init` 在 `EUIPageRouter.ProcessShowRequest` 中是同步执行的，大 Prefab 会导致卡顿。

**问题：** "开箱即用"的框架不能让用户打开一个复杂页面就卡一帧。这是框架的基础质量保证。

**Burner 做法：**
- `MaximalFrameTimeBudget = 500ms`
- `LoadStages` 枚举：None → OnResLoad → OnInit → OnLoad → OnBecomeVisible → Loaded
- `TryConsumeTimeSlice(out LoadTimeToken)` — 检查累积时间是否超预算
- `GamePage.OnUpdate()` 中恢复被暂停的阶段
- `accumulatedTime` 在每帧 `Update()` 末尾清零

**实现方案（简化版，适配 Ember 架构）：**
1. 将 `EUIManager.OpenPage` 中的 Init 和 PlayShow 拆分为可跨帧执行：
   - 当前：`EnqueueOperation(Init)` → `EnqueueOperation(PlayShow)` 在同一帧内连续执行
   - 改为：`Init` 执行后，如果超过帧预算，`PlayShow` 挂起到下一帧
2. `ProcessPendingOperations` 已经有时限检查，只需将 `TimeBudgetMs` 从 10 调大到合理的值（如 500ms）
3. 不需要照搬 Burner 的 `LoadStages` — Ember 的 Prefab 已经在 Router 层 `Instantiate` 了，分帧的主要目标是 Init 阶段的逻辑执行（`OnInitialize` + `Logic.OnInit` + `Logic.OnOpen`）

**改动范围：** `EUIManager.cs`（修改 `TimeBudgetMs` 默认值 + `OpenPage` 拆分 ~20 行）

---

### 3. 加载中操作挂起队列（PageTargetState） ✅ 已完成

**现状：** ~~`EUIManager.OpenPage` 假设页面是空闲的，不处理页面正在 Init/Show 期间收到的第二个请求。~~`EUIPage` 没有 `IsLoading` 状态追踪。

**问题：** 用户快速双击按钮，或者 A 页面正在加载时 B 页面请求打开，当前的实现会导致状态混乱。

**Burner 做法：**
- `PageTargetState` 结构体：Type（None/Show/Hide/Close/Restore/RequestShow）+ 操作参数
- `NeedQueueOperation()` — 检查 `IsLoading || IsPreloading`
- `ExecutePendingOperationIfAny()` — 加载完成后重放挂起操作
- 冲突仲裁：`SwitchState` — Close 可以被 Show 覆盖，其余不冲突

**实现方案：**
1. 在 `EUIPage` 增加 `IsLoading` 属性（`State == PageState.Loading || State == PageState.Showing`）
2. 增加 `PagePendingOp` 结构体（Type: Show/Hide/Close + 参数）
3. 在 `EUIManager.OpenPage` 入口检查 `IsLoading`，如果正在加载则写入 `_pendingOp`
4. 在 `EUIPage.CompleteShow` / `EUIPage.CompleteHide` 末尾检查并执行 `_pendingOp`

**改动范围：** `EUIPage.cs`（新增结构体 + 逻辑 ~40 行）、`EUIManager.cs`（入口检查 ~10 行）

---

### 4. CanvasScaler 自适应 ✅ 已完成

**现状：** ~~`EUIManager.EnsureLayerCanvas` 添加了 `CanvasScaler` 组件但没有配置。没有任何自适应逻辑。~~

**问题：** 框架声称"开箱即用"，但没有屏幕适配能力。用户在不同宽高比设备上打开 UI 会变形。

**Burner 做法：**
- `AutoAdjustCanvasScaler`（默认 true）
- 每帧 `Update()` 检测 `Screen.width/height` 变化
- `AdjustCanvasScaler(GameObject)` 对每个页面的 `CanvasScaler`（`MatchWidthOrHeight` 模式）调整 `matchWidthOrHeight`
- `OnScreenResolutionChanged` 事件 + `OnAdjustCanvasScaler` 回调

**实现方案（简化版）：**
1. 提供静态方法 `EUIManager.AdjustCanvasScaler(CanvasScaler)` — 根据当前屏幕宽高比和参考分辨率计算 `matchWidthOrHeight`
2. 在 `EUIManager.Update()` 中检测分辨率变化，变化时遍历所有活跃页面的 CanvasScaler 调用 Adjust
3. 通过 `EUIObserver` 或 `EmberEventBus` 播报分辨率变化
4. 不强制要求 `MatchWidthOrHeight` — 如果页面用的是 `Expand` 模式则跳过

**改动范围：** `EUIManager.cs`（~30 行）、新增 `ResolutionChangeEvent` 到 `EUIEvents`

---

## 二、🟡 应该做（6 项）

### 5. Overlay per-page 排序 ✅ 已完成

**现状：** ~~`EUIPageContext` 的 `_overlayList` 是一个简单列表，没有 per-page 排序管理。~~所有 Overlay 共享同一个列表，排序完全依赖 `EUIPageDef.Layer` 初始值。

**问题：** Overlay 类型的页面之间有确定的上下关系（如引导遮罩必须在引导主界面之下），框架应该提供这个能力。

**实现方案：**
- `EUIPageDef` 增加可选的 `int? overlaySortingOrder` 字段
- 若设置了，`AddOverlay` 时直接使用；若未设置，使用 `EUIPageDef.Layer` 作为默认值
- 不需要 burner 那样的硬编码字典 — 排序逻辑在 EUIPageDef 声明处即可

**改动范围：** `EUIPageDef.cs`（+1 字段）、`EUIPageContext.cs`（~5 行）

---

### 6. PlaneDistance 渲染裁剪 ✅ 已完成

**现状：** ~~Ember 用 `CanvasGroup.alpha = 0` + `blocksRaycasts = false` 隐藏被遮挡页面。~~GameObject 保持 `activeSelf = true`，Canvas 仍在渲染管线中（虽然不可见）。

**问题：** CanvasGroup alpha=0 时 Canvas 仍然会触发 Rebuild（Canvas.SendWillRenderCanvases）。大量隐藏页面积累会造成不必要的 CPU 开销。PlaneDistance 推到远裁面之外是更彻底的渲染剔除方式。

**Burner 做法：**
- `planeDistance = 100000` 时 Camera 不渲染该 Canvas
- 恢复时设回正常值（MainPageZ=250, PopupZ=200, TopMostZ=100, 递减-10）

**实现方案：**
1. 在 `EUIPageContext.SetPageVisible` 中，隐藏时设 `canvas.planeDistance = 100000`，显示时恢复为预设值
2. 预设值：通过 `EUIPageDef.Layer` 计算（UILayer → 对应 planeDistance 范围）
3. 保留 CanvasGroup alpha 方案作为 fallback（无 Canvas 组件时）

**改动范围：** `EUIPageContext.cs`（修改 `SetPageVisible` ~10 行）

---

### 7. 预加载机制 ✅ 已完成

**现状：** ~~没有 Preload 概念。页面要么已加载要么未加载。~~

**问题：** 无预加载时，打开一个复杂页面 = 用户等待 Prefab 加载 + Init 执行。预加载可以让页面"静默准备好"，打开时只剩 Show 动画。

**实现方案：**
1. `EUIPageRouter` 增加 `PreloadPage(EUIPageDef pageDef)` — 异步加载 Prefab + Init，但不 PlayShow
2. 预加载完成的页面 State = Loaded（不是 Opened），后续 `ShowMainPage/ShowPopup` 时复用
3. 预加载不强制 — 框架提供能力，业务决定是否用

**改动范围：** `EUIPageRouter.cs`（新增方法 ~20 行）、`EUIManager.cs`（支持跳过 PlayShow 的 Open ~10 行）

---

### 8. 页面加载计时 ✅ 已完成

**现状：** ~~没有性能计时。~~

**问题：** 框架没有给开发者提供任何页面加载耗时数据，排查慢页面全靠手动插 `Stopwatch`。

**实现方案：**
1. `EUIPage` 增加 `PageLoadTiming` 属性（结构体: `AssetLoadMs`, `InitMs`, `ShowMs`, `TotalMs`）
2. 在关键节点打时间戳：Prefab 加载完成 → Init 完成 → PlayShow 完成
3. `EmberDebug.Log` 输出（`LogInit` 级别），受 `EMBED_DEBUG` 宏控制

**改动范围：** `EUIPage.cs`（+ 结构体 + 打点 ~25 行）

---

### 9. Profiler 标记 ✅ 已完成

**现状：** ~~没有 `Profiler.BeginSample/EndSample`。~~

**问题：** 开发者在 Unity Profiler 中看不到 UI 框架的各个阶段耗时。

**实现方案：**
1. 在关键路径添加 Profiler 标记（加载、Init、PlayShow、PlayHide、ProcessOperations）
2. 用 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 控制

**改动范围：** `EUIManager.cs`、`EUIPage.cs`、`EUIPageRouter.cs`（各 ~5 行）

---

### 10. 事件自动清理追踪 ✅ 已完成

**现状：** ~~用户在 `EUILogic.OnInit()` 中注册事件（如 `EmberEventBus.On(...)`），需要在 `OnDispose()` 中手动注销。忘记注销 = 泄漏。~~

**问题：** 这是最容易出 bug 的地方。框架应该提供自动追踪能力。

**实现方案：**
1. `EUILogic` 增加 `TrackDisposable(IDisposable)` — 内部用 `List<IDisposable>` 存储
2. `BroadcastDispose()` 中自动遍历清理
3. `EmberEventBus` 的 `On` 方法返回 `IDisposable`，和 UniRx 的 `Subscribe` 返回 `IDisposable` 天然兼容

**改动范围：** `EUILogic.cs`（~15 行）

---

## 三、🟢 可以做（7 项）

### 11. PostponeSetVisible
延迟 SetActive 到 OnShow 执行后。特定场景需要（先发 OnShow 事件让业务准备数据，再让 UI 可见）。但不常用，且可以通过 override OnShow 实现类似效果。

### 12. HideLowerPage Per-Popup Flag
某个 Popup 可以设置"打开时隐藏下方所有页面"的 flag。特定弹窗（如全屏设置页）需要，但大多数 Popup 不需要。

### 13. MainPageGroups
多组并行 MainPage 栈。burner 用这个来隔离 HUD 层和内容层的页面栈。框架级引入会增加概念复杂度，多数项目不需要。如果有项目需要，可以通过并行的 PageContext 实例实现。

### 14. CloseAllPagesAfter API
便利 API：关闭指定页面及其之后打开的所有页面。可以通过多次 `ClosePage` 组合实现。

### 15. Animator / ParticleSystem 自动管理
SetActive 时自动控制 Animator 和 ParticleSystem。与具体 Prefab 结构耦合，框架层做太多假设容易出问题。

### 16. HidePage / RestorePage
隐藏页面（保持加载但不渲染）vs 关闭页面（销毁）。当前有 Pause/Resume 和 Close/Reopen，已有两条路径。Hide/Restore 是第三条路，增加了状态机复杂度。

### 17. 全局点击事件监听
记录所有按钮点击用于埋点。这是埋点系统的职责，不应进入 UI 框架。

---

## 四、⚪ 不需要（6 项）

| 功能 | 不做的原因 |
|------|-----------|
| 背景模糊（Blur Pages） | 项目特效，依赖 `NodePostProcessManager` + RenderTexture 截图 |
| 相机可见性管理 | 硬编码的 `pagesThatKeep3DSceneVisible` 白名单，完全属于项目逻辑 |
| HUD 独立层 | 项目 UI 布局设计，用 `UILayer` + `PageType` 组合即可模拟 |
| GameState 监听 | burner 项目的状态机概念（SLG/RPG 切换），不是通用框架概念 |
| 页面音乐 | 项目音频系统集成，框架不应管理音频逻辑 |
| ScreenShot | 业务功能，不是框架职责 |

---

## 五、实现计划

### 第一轮（已完成 ✅）—— 🔴 必须做

| # | 功能 | 改动文件 | 实际行数 |
|---|------|---------|---------|
| 1 | SubPage 独立排序 | `EUIPageRouter.cs`, `EUIPage.cs` | ~30 |
| 2 | 分帧加载 | `EUIManager.cs` | ~10 |
| 3 | 加载中操作挂起队列 | `EUIPage.cs`, `EUIManager.cs` | ~55 |
| 4 | CanvasScaler 自适应 | `EUIManager.cs` | ~45 |

### 第二轮（已完成 ✅）—— 🟡 应该做

| # | 功能 | 改动文件 |
|---|------|---------|
| 5 | Overlay per-page 排序 | `EUIPageDef.cs`, `EUIPageContext.cs` |
| 6 | PlaneDistance 渲染裁剪 | `EUIPageContext.cs` |
| 7 | 预加载机制 | `EUIPageRouter.cs`, `EUIManager.cs` |
| 8 | 页面加载计时 | `EUIPage.cs` |
| 9 | Profiler 标记 | `EUIManager.cs`, `EUIPage.cs`, `EUIPageRouter.cs` |
| 10 | 事件自动清理追踪 | `EUILogic.cs` |

### 第三轮（远期）—— 🟢 可以做

| # | 功能 |
|---|------|
| 11-17 | PostponeSetVisible、HideLowerPage、MainPageGroups 等 |

### 不做 —— ⚪

| # | 功能 |
|---|------|
| 18-23 | 背景模糊、相机可见性、HUD 层、GameState 监听、页面音乐、ScreenShot |

---

## 六、实施约束

1. **不破坏现有 API** — `ShowMainPage` / `ShowPopup` / `ShowSubPage` 等方法签名保持不变
2. **向后兼容** — 现有业务代码（`Assets/Game/`）无需改动
3. **最小改动原则** — 优先在现有类中扩展，不引入新的核心类型
4. **XML 文档** — 所有新增 public API 必须有 `///` 注释
5. **编码规范** — 遵循 CLAUDE.md 的命名、region 分块、`EmberDebug` 日志、`[HasGC]/[NoGC]` 标注

---

> **下一步：** 确认此计划后，从第一轮第 1 项（SubPage 排序）开始逐项实现。
