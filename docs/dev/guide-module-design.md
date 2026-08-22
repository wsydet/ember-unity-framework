# 新手引导模块（GuideModule）设计文档

> 参考 burner `GuideNew` 模块，落地为 ember-unity-framework 的**业务模块**。
> 本文档先定义「本地化」后的设计，再据此实现。
> 最后更新：2026-08-19

---

## 0. 本地化要点（相比 burner 的取舍）

| 维度 | burner GuideNew | 本框架 GuideModule |
|------|-----------------|-------------------|
| 归属 | `GameLogic.GameModule.GuideNew` | `Game.Module.Guide`（`Assets/Game/Module/Guide/`，业务层） |
| 模块基类 | `ModuleBase<T> + IGameUpdate` | `EmberSingleton<GuideModule> + IEmberModule + IEmberUpdate` |
| 服务器同步 | `GuideServerData` + `SystemModule` 上报 | **本地持久化**（`DataSaver` → `guide_progress.json`），预留同步接口 |
| 配置来源 | CSV（guide.csv）+ 资源路径异步加载 | `GuideConfig`（SO 注册表）直引用 `GuideDefine`（SO），无路径字符串 |
| 事件来源 | 自定义 `EventDispatcher` + 几十个 SLG 专用事件 | `EUIObserver`（页面生命周期）+ `EmberEventBus`（引导内部事件），仅保留通用事件 |
| 执行器 | 40+ 个（建筑/帕鲁/timeline/镜头…） | 9 个通用执行器（mask / hand / UI / delay / tips / log） |
| 条件 | 36 个（建筑/英雄/任务/招募…） | 6 个通用条件（true / false / 事件触发 / UI 展示 / 引导完成 / 数值比较） |
| 小手 UI | `UIGuideMainPanel.prefab`（美术资源） | **运行时代码构建**（`GuideOverlay`，占位美术，可后续换 prefab） |
| 遮罩 UI | `UIGuideMaskPanel.prefab` | 并入 `GuideOverlay`（同一 Canvas） |

**结论**：保留 burner 的核心引擎——**步骤状态机（NotStart → Doing → Finished）**、
**双轨引导（顺序 + 非顺序）**、**条件 / 事件 / 执行器三层解耦**、**AND/OR 条件组合**——
去掉所有 SLG 业务耦合与服务器依赖，用框架原语重新表达。

---

## 1. 系统概述

### 1.1 设计目标
- **配置化驱动**：用 ScriptableObject 定义引导流程，不写死代码。
- **事件驱动**：引导步骤由游戏事件（页面打开、按钮点击、延时结束等）推进。
- **条件判断**：支持跳过 / 完成条件，AND/OR 组合。
- **进度持久化**：本地持久化引导进度（JSON），预留服务器同步接口。
- **可扩展**：新增条件 / 事件 / 执行器只需加枚举 + 注册一行，无需改状态机。

### 1.2 核心特性
- **双轨引导**：顺序引导（按 `sequenceOrder` 依次执行）+ 非顺序引导（条件满足即触发）。
- **步骤状态机**：`NotStart → Doing → Finished`。
- **事件注册**：步骤进入/退出时动态注册/反注册监听。
- **条件组合**：`AND` / `OR` 逻辑组合。
- **执行器框架**：统一 `Execute(blackboard)` 接口。

---

## 2. 目录与命名空间

```
Assets/Game/Module/Guide/
├── GuideModule.cs              # 主模块（单例 + 生命周期 + 进度持久化）
├── GuideProgress.cs            # 引导进度数据（可序列化，DataSaver 落盘）
├── GuideConfig.cs              # 引导注册表 SO（guide 列表 + 顺序 + 参数）
├── GuideDefine.cs              # 单条引导定义 SO（步骤列表 + 全局跳过条件）
├── GuideStepDefine.cs          # 单步定义（事件 / 条件 / 执行器）
├── GuideGroup.cs               # 运行时状态机 + 黑板 + 枚举 + 接口
├── GuideEvents.cs              # GuideEventType 枚举 + GuideEvent + 事件 Key 常量
├── GuideEventHandle.cs         # 事件处理器基类 + 具体处理器
├── GuideCondition.cs           # 条件基类 + Group + 具体条件 + 枚举 + 参数类
├── GuideExecutor.cs            # 执行器基类 + 枚举 + 参数类
├── GuideExecutorExtension.cs   # 具体执行器实现（静态方法）
├── GuideOverlay.cs             # 运行时构建的遮罩 / 小手覆盖层
├── GuideUtils.cs               # 工具（查找 UI 控件 / 屏幕矩形 / 日志）
└── Editor/
    └── GuideDefineEditor.cs    # 自定义 Inspector（加步骤 / 折叠显示）
```

- **命名空间**：`Game.Module.Guide`（独立子命名空间，避免 `GuideEvent` 等泛化名冲突）。
- **程序集**：运行时代码在 `Assembly-CSharp`（`Assets/Game/Module/Guide/` 下无 `.asmdef`），
  自动引用 `Ember.Core` / `Ember.Basic` / `Ember.UI` / `UniRx`。
  编辑器代码在 `Assets/Game/Module/Guide/Editor/`（`Assembly-CSharp-Editor`，可引用运行时）。
- **日志 TAG**：每个类 `private const string TAG = LogTags.Game + "." + nameof(类名);`（遵守业务日志约定）。

---

## 3. 核心数据结构

### 3.1 引导进度（本地持久化）

```csharp
[Serializable]
public class GuideProgress
{
    public int finishedSequentialId;      // 顺序引导：当前完成到的引导 id
    public List<int> finishedOtherId;     // 非顺序引导：已完成 id 列表
}
```

- 通过 `DataSaver.Save("guide_progress.json", progress)` / `TryLoad` 落盘到 `persistentDataPath`。
- 预留 `Action<GuideProgress> OnProgressChanged` 回调，未来接服务器同步只需在此处替换实现。

### 3.2 状态枚举

```csharp
public enum GuideStepState       { NotStart, Doing, Finished }
public enum GuideStartCheckResult { Fail, Success, Skip, SkipAll }
public enum GuideEndCheckResult   { Fail, Success, Cancel, CancelAll, FinishAll }
```

### 3.3 引导组黑板（Blackboard）

```csharp
public class GuideGroupBlackboard
{
    public GuideGroup GuideGroup;      // 所属引导组
    public int MaskCount;              // 遮罩引用计数（多层遮罩）
    public bool ShowedHand;            // 是否已显示小手
    public string[] StringParams;      // 字符串参数（来自 GuideConfig）
    public int[] IntParams;            // 整型参数（同时充当 id/int 参数）

    public string GetString(int index);
    public int GetInt(int index);
    public bool GetBool(int index);    // GetInt(index) > 0
}
```

> 本地化：burner 有 `StringParams` / `IdParams` / `IntParams` 三个数组，本框架合并为
> `StringParams` + `IntParams` 两个（id 与 int 本质都是整数，按索引区分即可）。

---

## 4. 配置系统

### 4.1 `GuideConfig`（引导注册表）

```csharp
[CreateAssetMenu(menuName = "Ember/Guide/GuideConfig")]
public class GuideConfig : ScriptableObject
{
    public List<GuideEntry> entries;
}

[Serializable]
public class GuideEntry
{
    public int id;                // 引导唯一 id（与进度持久化对应）
    public int sequenceOrder;     // >0 顺序引导（按值排序）；0 非顺序引导
    public GuideDefine define;    // 直接引用引导定义资产
    public string[] stringParams; // 字符串参数
    public int[] intParams;       // 整型参数
}
```

- 相当于 burner 的 `guide.csv`，但用 SO 直引用替代 CSV + 资源路径字符串。
- `sequenceOrder > 0` → 顺序引导（按值升序依次执行）；`== 0` → 非顺序引导（并行装载，条件满足即触发）。

### 4.2 `GuideDefine`（单条引导定义）

```csharp
[CreateAssetMenu(menuName = "Ember/Guide/GuideDefine")]
public class GuideDefine : ScriptableObject
{
    [SerializeReference] public GuideConditionBase baseSkipAll;   // 全局跳过条件
    public List<GuideStepDefine> guideSteps = new();              // 步骤列表
}
```

### 4.3 `GuideStepDefine`（单步定义）

```csharp
[Serializable]
public class GuideStepDefine
{
    public string name;          // 步骤名（调试用）
    public bool needUpdate;      // 是否每帧轮询条件（默认由事件驱动）

    // 开始阶段（NotStart → Doing）
    [SerializeReference] public List<GuideEvent> startEvents = new();
    [SerializeReference] public GuideConditionBase startConditionsToSkipAll;
    [SerializeReference] public GuideConditionBase startConditionsToSkip;
    [SerializeReference] public GuideConditionBase startConditionsToSuccess;
    [SerializeReference] public List<GuideExecutor> startExecutors = new();

    // 结束阶段（Doing → Finished）
    [SerializeReference] public List<GuideEvent> endEvents = new();
    [SerializeReference] public GuideConditionBase endConditionsToFinishAll;
    [SerializeReference] public GuideConditionBase endConditionsToCancelAll;
    [SerializeReference] public GuideConditionBase endConditionsToCancel;
    [SerializeReference] public GuideConditionBase endConditionsToSuccess;
    [SerializeReference] public List<GuideExecutor> endExecutors = new();
}
```

> 说明：`[SerializeReference]` 让 Unity 默认 Inspector 自带「类型下拉」，
> 可直接添加任意 `GuideConditionBase` / `GuideEvent` / `GuideExecutor` 子类。

---

## 5. 事件系统

### 5.1 事件类型枚举

```csharp
public enum GuideEventType
{
    None = 0,
    OnPageShown    = 1,   // UI 页面打开（eventParam: GuideEventParamPage）
    OnPageHidden   = 2,   // UI 页面关闭
    OnClickUIButton = 3,  // UI 按钮被点击（eventParam: GuideEventParamClickUI）
    OnDelayFinish  = 4,   // 延时结束
    OnGuideMaskClick = 5, // 引导遮罩被点击
    OnCustom       = 6,   // 自定义事件（eventParam: GuideEventParamCustom）
}
```

### 5.2 事件 Key（EmberEventBus，业务基址 10000 起）

```csharp
public static class GuideEventKey
{
    public const int DelayFinish    = EmberBroadcastEvent.Game + 1;  // 参数 int token
    public const int MaskClick      = EmberBroadcastEvent.Game + 2;  // 无参
    public const int ClickUIButton  = EmberBroadcastEvent.Game + 3;  // 参数 (string pagePath, string ctrlName)
    public const int Custom         = EmberBroadcastEvent.Game + 4;  // 参数 int key
}
```

- 页面打开 / 关闭事件来自 `EUIObserver.OnPageOpened` / `OnPageClosed`（UniRx），不占用 EventBus key。
- 业务代码触发引导事件走 `GuideModule.Instance.NotifyXXX(...)`，内部转发到 EmberEventBus。

### 5.3 事件处理器基类

```csharp
public abstract class GuideEventHandle
{
    public GuideEventType GuideEventType;
    public GuideGroup GuideGroup;
    public List<GuideEvent> GuideEvents = new();
    public bool IsUnRegistered;

    protected void Trigger();          // → GuideGroup.TryNext(GuideEventType)，带重入保护
    public abstract void Register();   // 订阅底层事件源
    public abstract void UnRegister(); // 反订阅
}
```

- 具体处理器：`GuideEventHandleOnPageShown` / `OnPageHidden`（订阅 `EUIObserver`，按 `eventParam` 过滤页面路径）、
  `OnDelayFinish` / `OnGuideMaskClick` / `OnClickUIButton` / `OnCustom`（订阅 `EmberEventBus`）。
- **重入保护**：事件在 executor 执行中触发时，记录当前步骤状态并延迟一帧重派发，避免流程错乱。

---

## 6. 条件系统

### 6.1 条件基类

```csharp
[Serializable]
public abstract class GuideConditionBase
{
    public abstract bool IsMet(GuideGroupBlackboard blackboard, GuideEventType triggerEvent, StringBuilder reason);
}
```

### 6.2 条件组合（AND / OR）

```csharp
public enum LogicOperator { And, Or }

[Serializable]
public class GuideConditionGroup : GuideConditionBase
{
    public LogicOperator logicOperator = LogicOperator.And;
    [SerializeReference] public List<GuideConditionBase> conditions = new();
}
```

### 6.3 具体条件（最小可用集）

| 枚举 | 含义 | 参数类 |
|------|------|--------|
| `True` | 恒真 | — |
| `False` | 恒假 | — |
| `IsTriggerByEvent` | 是否由指定事件触发 | `GuideCondParamIsTriggerByEvent { GuideEventType eventType }` |
| `IsUIShowing` | 页面是否展示在顶层 | `GuideCondParamIsUIShowing { string pagePath; bool isShowing }` |
| `IsGuideFinished` | 某引导是否已完成 | `GuideCondParamGuideFinished { int guideId }` |
| `CompareInt` | 黑板 int 参数比较 | `GuideCondParamCompareInt { int intParamIndex; GuideOperator op; int value }` |

> `GuideOperator { Equal, Greater, Less, GreaterAndEqual, LessAndEqual }`。
> 条件检查失败时会向 `reason` 追加判定过程，供 GM 面板 / 日志诊断「引导为什么没走」。

---

## 7. 执行器系统

### 7.1 执行器基类

```csharp
[Serializable]
public partial class GuideExecutor
{
    public GuideExecuteType executeType;
    [SerializeReference] public object executeParam;
    public void Execute(GuideGroupBlackboard blackboard);  // 按 executeType 分发到静态方法
}
```

### 7.2 执行器枚举（最小可用集）

| 枚举 | 含义 | 参数类 |
|------|------|--------|
| `OpenMask` | 打开全屏遮罩 | `GuideExeParamMask { float duration = -1f }` |
| `CloseMask` | 关闭遮罩 | — |
| `OpenHand` | 打开小手指引到 UI 控件 | `GuideExeParamOpenHand { pagePath, ctrlName, tips, maskColor, handDelay, clickMaskToCancel }` |
| `CloseHand` | 关闭小手 | — |
| `OpenUI` | 打开 UI 页面 | `GuideExeParamOpenUI { prefabPath, layer, pageType }` |
| `CloseUI` | 关闭 UI 页面 | `GuideExeParamCloseUI { prefabPath }` |
| `Delay` | 延时 | `GuideExeParamDelay { float seconds }` |
| `ShowTips` | 弹提示 | `GuideExeParamShowTips { string message }` |
| `Log` | 输出调试日志 | `GuideExeParamLog { string message }` |

- **遮罩引用计数**：`OpenMask` / `CloseMask` 通过黑板 `MaskCount` 计数，多层打开/关闭安全。
- **延时**：`Delay` 用 `EmberTimerManager.Delay`，结束后播 `GuideEventKey.DelayFinish`（带 token）。
- **打开页面**：`OpenUI` 用 `pageType` 分发到 `EUIManager.ShowMainPage / ShowPopup / ShowTopMost`。

---

## 8. 执行流程

### 8.1 单步状态机（与 burner 一致）

```
TryNext(eventType) 被调用
    │
    ├─ 状态 = NotStart
    │   ├─ 检查 skipAll / skip / success 条件
    │   │    ├─ Skip     → 步骤 +1，状态回 NotStart
    │   │    ├─ SkipAll  → 步骤跳到末尾，状态 Finished
    │   │    ├─ Fail     → 注册 startEvents，等待事件
    │   │    └─ Success  → 注册 endEvents，执行 startExecutors，状态 → Doing
    │
    ├─ 状态 = Doing
    │   ├─ 检查 finishAll / cancelAll / cancel / success 条件
    │   │    ├─ Cancel    → 清步骤，状态回 NotStart
    │   │    ├─ CancelAll → 步骤归零，状态 NotStart
    │   │    ├─ FinishAll → 步骤跳到末尾，状态 Finished
    │   │    ├─ Fail      → 继续等待（保留 endEvents）
    │   │    └─ Success   → 执行 endExecutors，步骤 +1 或 Finished
    │
    └─ 状态 = Finished
        └─ 通知 GuideModule：引导完成 / 取消
```

### 8.2 每帧驱动

- `GuideModule` 实现 `IEmberUpdate`，每帧 `Tick()` 调用当前引导组 `OnTick()`：
  - 对 `needUpdate = true` 的步骤，每帧 `TryNext()` 重新轮询条件。

### 8.3 引导装载（TrySetupAll）

```
TrySetupAll()
    ├─ 引导未开启 / 进度未加载 / 已有引导执行中 → 返回
    ├─ 顺序引导：finishedSequentialId 的下一个（sequenceOrder 升序）→ SetupSingle(id)
    └─ 非顺序引导：所有未完成的 → SetupSingle(id)（并行等待，条件满足即执行）
```

- **全局互斥**：同一时刻最多一个引导组进入 `Doing`，避免遮罩 / 小手冲突。

---

## 9. 遮罩与小手 UI（GuideOverlay）

运行时用代码构建一个 ScreenSpaceOverlay Canvas（`sortingOrder = 29000`，高于 TopMost、低于 FreePage），
parent 到 `EUIViewEngine.Instance.UIRoot`。结构：

```
GuideOverlayCanvas (Canvas, sortingOrder=29000)
├── ClickMask   (Image，全屏透明，拦截点击 → 触发 OnGuideMaskClick / 取消)
├── Top/Bottom/Left/Right (Image，4 块遮罩矩形，包围目标矩形形成镂空)
├── Hole        (Image，镂空高亮描边)
├── Finger      (Image，圆形手指指示，脉冲缩放)
└── Bubble      (Image 半透明底 + Text 提示文字)
```

- `OpenMask`：目标矩形为 `Rect.zero` → 全屏变暗、无镂空、无手指。
- `OpenHand`：目标矩形 = 控件屏幕矩形 → 4 块遮罩挖孔 + 手指脉冲 + 气泡文字。
- 手指 / 气泡为占位美术（代码生成的圆形纹理 + 默认 Text），后续可替换为正式 prefab。

---

## 10. 使用流程（业务接入）

1. **创建引导定义**：`Assets → Create → Ember/Guide/GuideDefine`，加步骤、配条件 / 事件 / 执行器。
2. **创建引导注册表**：`Assets → Create → Ember/Guide/GuideConfig`，填 `id` / `sequenceOrder` / `define` / 参数。
3. **启用模块**：把 `GuideModule.Enabled` 改为 `true`（默认 false）。
4. **初始化**：在 `GameMainState` 等入口调用
   ```csharp
   GuideModule.Instance.Initialize(config);   // 传入 GuideConfig
   GuideModule.Instance.Start();              // 装载并开始
   ```
5. **业务触发事件**：
   ```csharp
   GuideModule.Instance.NotifyButtonClick("MainMenu", "m_Btn_Start");
   GuideModule.Instance.NotifyDelayFinish(token);
   ```

---

## 11. 实现步骤（对照本文档）

1. 枚举 + 黑板 + 进度（`GuideProgress.cs`、`GuideGroup.cs` 前半）。
2. 配置（`GuideConfig.cs`、`GuideDefine.cs`、`GuideStepDefine.cs`）。
3. 事件（`GuideEvents.cs`、`GuideEventHandle.cs`）。
4. 条件（`GuideCondition.cs`）。
5. 执行器（`GuideExecutor.cs`、`GuideExecutorExtension.cs`）。
6. 状态机 + 主模块（`GuideGroup.cs` 后半、`GuideModule.cs`）。
7. 覆盖层 UI（`GuideOverlay.cs`）+ 工具（`GuideUtils.cs`）。
8. 编辑器（`GuideDefineEditor.cs`）。

---

## 12. 后续扩展（不在本次范围）

- **服务器同步**：`GuideProgress.OnProgressChanged` 接网络上报。
- **正式美术**：`GuideOverlay` 换成 prefab（手指动画、气泡九宫格）。
- **更多条件 / 事件 / 执行器**：按枚举 + 注册表扩展。
- **自动化测试**：为状态机补 EditMode 测试。
- **可视化编辑器**：远期在蓝图 / 节点编辑器里搭引导流程。
