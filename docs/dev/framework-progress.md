# Ember Framework 开发进度

> 最后更新：2026-08-04
> 参考项目：[burner](../../c:/Users/wuyu/Project/burner/client/game/) — 成熟的 SLG 游戏框架

---

## 架构总览

```
Assets/Ember/                     # 框架层（零业务逻辑）
├── Core/                         #   核心：EventBus、ServiceLocator、Singleton
├── Resource/                     #   资源管理：加载/卸载抽象
├── UI/                           #   UI 管理：界面栈 + 生命周期
├── Scene/                        #   场景管理：加载/卸载/过渡
├── Audio/                        #   音频管理
├── Input/                        #   输入抽象层
└── Editor/                       #   框架级编辑器工具
```

### 依赖方向

```
Core ← Resource ← Scene
  ← UI
  ← Audio
  ← Input

Core 是叶子层，零依赖（除 Unity 引擎），所有上层模块只能依赖 Core。
```

---

## 开发策略：单机先行 → 网络补充 → 可视化收尾

```
┌──────────────────────────────────────────────────┐
│  Phase 1: 单机框架（当前）                          │
│  先按单机游戏的需求开发，所有模块默认本地运行。         │
│  网络相关接口预留但暂不实现。                         │
│                                                   │
│  Phase 2: 网络适配                                │
│  单机跑通后，补上网络层：RPC、状态同步、联网验证等。    │
│  利用 Phase 1 的接口预留位快速接入。                  │
│                                                   │
│  Phase 3: 可视化编辑器                             │
│  框架定型后，搭建蓝图/节点编辑器，面向策划和设计师。    │
└──────────────────────────────────────────────────┘
```

**提醒规则：** 开发过程中如果某个模块在联网场景下需要不同的行为
（例如 Scene 模块的加载方式、StateMachine 的同步校验），
文档中用 `🌐 [NET] 待适配` 标记，代码中用 `// TODO NET` 留空，
方便 Phase 2 全局搜索。

---

## 实现顺序

按依赖关系排列，先底层后上层：

| 序号 | 模块 | 程序集 | 状态 | 参考 burner |
|------|------|--------|------|-------------|
| 1 | **Core** | `Ember.Core.Runtime` | ✅ 已完成 | `GameCore.Runtime` + `Burner.Basic` |
| 2 | **Resource** | `Ember.Resource.Runtime` | ✅ 已完成 | `ResManager` + `IResourceProxy` + YooAsset |
| 3 | **UI** | `Ember.UI.Runtime` | ✅ 已完成 | `GameUIManager` + `Burner.UIExtension` |
| 4 | **Scene** | `Ember.Scene.Runtime` | ✅ 已完成 | `GameSceneManager` |
| 5 | **Audio** | `Ember.Audio.Runtime` | ✅ 已完成 | `AudioMgr` |
| 6 | **Input** | `Ember.Input.Runtime` | ✅ 已完成 | Unity Input System 封装 |
| 7 | **Editor** | `Ember.Editor` | ✅ 已完成 | 框架级编辑器工具 |
| 8 | **Manager 自动发现** | `Ember.Core.Runtime` | ✅ 已完成 | `GameMgrCollector` + `IManager` |
| 9 | **Update 循环管理器** | `Ember.Core.Runtime` | ✅ 已完成 | `GameUpdateManager` |
| 10 | **Timer 定时器** | `Ember.Core.Runtime` | ⬜ 待开始 | `TimerManage`（基于 UniTask，放入 com.ember.extensions） |
| 11 | **GameState 状态机** | `Ember.Core.Runtime` | ✅ 已完成 | `GameStateManager` |
| 12 | **日志系统** | `Ember.Core.Runtime` | ✅ 已完成 | `Debuger` |
| 13 | **Basic 包迁移** | `com.ember.basic` | ✅ 已完成 | 36 文件迁移 + 6 用户工具整合 + 21 个编辑器工具 |
| 14 | **Basic 编辑器工具优化** | `com.ember.basic` | 🧪 测试中 | 统一基类 + Odin 面板 + 右键快捷菜单 + 中英文切换 |

---

## 1. Core 模块 `Ember.Core.Runtime`

> 状态：✅ 已完成
> burner 参考：`Assets/Game/GameCore/Runtime/` + `com.burner.basic`

### 文件清单

| 文件 | 职责 | 参考 |
|------|------|------|
| [Ember.Core.Runtime.asmdef](../../Assets/Ember/Core/Runtime/Ember.Core.Runtime.asmdef) | 程序集定义，零依赖 | — |
| [EmberEventBus.cs](../../Assets/Ember/Core/Runtime/EmberEventBus.cs) | 全局事件总线，int-key + 常量表，0～4 泛型参数，遍历安全 | burner `EventDispatcher` |
| [EmberBroadcastEvent.cs](../../Assets/Ember/Core/Runtime/EmberBroadcastEvent.cs) | 广播事件 int 常量表，模块区间分配避免 Key 冲突 | burner `ModuleType` + `XxxEventDefine` |
| [EmberServiceLocator.cs](../../Assets/Ember/Core/Runtime/EmberServiceLocator.cs) | 轻量级服务定位器，接口→实现映射，支持延迟工厂 | —（burner 无此模式） |
| [EmberSingleton.cs](../../Assets/Ember/Core/Runtime/EmberSingleton.cs) | 两种单例基类：`EmberSingleton<T>`（纯 C#）和 `EmberMonoSingleton<T>`（MonoBehaviour） | burner `SafeMonoSingleton`、`Singleton<T>` |
| [EmberObjectPool.cs](../../Assets/Ember/Core/Runtime/EmberObjectPool.cs) | 通用对象池，支持 IPoolable 回调、统计、容量限制 | burner `BattleCore/ObjectPool` |

### 与 burner 的设计差异

| 维度 | burner | ember | 理由 |
|------|--------|-------|------|
| 事件 Key | int + 区间分配（ModuleType + EventDefine） | int + 区间分配（EmberBroadcastEvent 常量表） | 同方案，ember 合并为一个常量文件（模块数少） |
| 遍历安全 | 索引指针调整 | 延迟操作队列 (pending ops) | 更清晰的语义，支持嵌套 dispatch |
| 服务定位 | 无（Singleton.Instance + 反射） | EmberServiceLocator | 解耦接口与实现，方便测试和替换 |
| 对象池 | 最小实现（仅 Stack） | 带容量/统计/IPoolable | 更完整的生产级实现 |

### 事件通信分层策略

事件通信的选择标准不是"框架 vs 业务"，而是**事件的本质**——它是"广播型"还是"数据流型"：

| 维度 | 广播型事件（Broadcast） | 数据流型事件（Data Stream） |
|------|------------------------|---------------------------|
| **例子** | 模块初始化完成、模块销毁、场景加载完毕 | 玩家血量变化、获得物品、技能释放 |
| **发布者心态** | "这件事发生了，谁关心谁听" | "这个数据变了，有几个确定的消费者" |
| **消费方** | 不确定有几方，随项目增长动态变化 | 确定——UI、音效、数据记录各取所需 |
| **频率** | 低频（初始化/销毁各一次） | 高频（每次操作都可能触发） |
| **需要操作符？** | 不需要 | 需要（`Throttle` 防抖、`Where` 过滤、`Batch` 聚合） |
| **适合什么** | EmberEventBus | UniRx Subject / MessageBroker |

```
┌─────────────────────────────────────────────┐
│  UniRx Subject / MessageBroker              │
│  玩家血量、技能命中、物品变更、战斗结算...       │
│  "具体数据流，有确定的生产者和消费者"            │
│  优势：类型安全 + 操作符 + AddTo 生命周期      │
├─────────────────────────────────────────────┤
│  EmberEventBus                              │
│  Module.Ready / Module.Destroy / Scene.Loaded │
│  "框架级公告，谁关心谁听"                       │
│  优势：零引用耦合 + 零外部依赖 + 不确定消费方   │
└─────────────────────────────────────────────┘
```

#### 场景推演

**模块生命周期 → EmberEventBus**（广播型）：

```csharp
// Resource 模块初始化完毕 —— 不知道谁关心，广播一下
EmberEventBus.OnNext(EmberBroadcastEvent.ResourceReady);

// Scene 模块监听 —— "Resource Ready 了我才开始加载"
EmberEventBus.Subscribe(EmberBroadcastEvent.ResourceReady, () => StartLoadScene());

// Audio 模块也监听 —— "Resource Ready 了我预热音频"
EmberEventBus.Subscribe(EmberBroadcastEvent.ResourceReady, () => PreloadBGM());
```

**具体效果 → UniRx Subject**（数据流型）：

```csharp
// 玩家模块 —— 暴露一个 Subject，定义为"我的数据流"
public class PlayerRuntime
{
    public Subject<HpChangeInfo> OnHpChanged = new Subject<HpChangeInfo>();
}

// UI 模块 —— 节流防抖
playerRuntime.OnHpChanged
    .Where(h => h.Delta < 0)
    .Throttle(TimeSpan.FromMilliseconds(100))
    .Subscribe(h => ShowDamageNumber(h.Delta))
    .AddTo(this);

// 音效模块 —— 同一数据流，不同处理方式
playerRuntime.OnHpChanged
    .Subscribe(h => PlayHitSound(h.NewHp))
    .AddTo(this);
```

"血量变化"这个事件，UI 需要 `Throttle` 防抖，音效不需要——UniRx 的操作符让每个消费方按自己的需求独立处理，这在 EmberEventBus 的 `void Subscribe(string, Action)` 模型里需要消费方自己实现。

#### 选择规则

| 场景 | 用什么 | 理由 |
|------|--------|------|
| 模块生命周期（Ready / Destroy） | EmberEventBus | 广播型，不确定消费方是谁 |
| 全局系统通知（网络断开、切后台） | EmberEventBus | 零依赖，任何模块都可以发 |
| 具体游戏数据变化（血量、物品、技能） | UniRx Subject | 数据流型，各消费方需要独立处理 |
| 需要操作符的 UI 响应 | UniRx Subject | `Throttle` / `Where` / `Delay` 开箱即用 |
| 框架事件需被业务层消费 | EmberEventBus → UniRx 桥接 | 适配器转成 `IObservable<T>` |

#### 桥接示例

```csharp
public static class EmberEventBusExtensions
{
    /// <summary>
    /// 将 EmberEventBus 的 int-key 广播事件转为 UniRx IObservable，
    /// 让业务层可以享受操作符便利。
    /// </summary>
    public static IObservable<T> OnEvent<T>(int eventKey)
    {
        return Observable.FromEvent<Action<T>, T>(
            h => EmberEventBus.Subscribe(eventKey, h),
            h => EmberEventBus.Unsubscribe(eventKey, h));
    }
}
```

#### 演进路径

- **当前**：EmberEventBus 服务于广播型事件，UniRx Subject 服务于数据流型事件
- **底线**：框架 Core 零外部依赖，EmberEventBus 不会被 UniRx 替代——它覆盖的场景是 UniRx 也无法替代的（真正的 "谁关心谁听" 无耦合广播）

### 待设计讨论

- [x] ~~是否需要 Timer/TimerManager？~~ → ✅ 已完成（§10，基于 UniTask）
- [x] ~~是否需要 Update 循环管理器？~~ → ✅ 已完成（§9）
- [x] ~~是否需要 Manager 自动发现机制？~~ → ✅ 已完成（§8）

---

## 2. Resource 模块 `Ember.Resource.Runtime`

> 状态：✅ 已完成
> burner 参考：`Assets/Game/GameCore/Runtime/Common/Res/`

### 设计思路

Resource 模块是框架资源加载的统一入口，核心思想是**接口隔离**——
框架只定义 `IResourceProvider` 接口，具体后端由使用者实现并注册。
上层模块通过 `EmberResourceManager` 消费资源，不感知底层是 Resources、Addressables 还是 YooAsset。

```
UI / Scene / Audio / ...
        │
        ▼
EmberResourceManager (Singleton Facade)
        │
        ▼
IResourceProvider (接口)
        │
        ├── ResourcesProvider (开发/小项目)
        ├── AddressablesProvider (正式项目)
        └── YooAssetProvider (热更新/大项目)
```

### 文件清单

| 文件 | 职责 | 参考 |
|------|------|------|
| [Ember.Resource.Runtime.asmdef](../../Assets/Ember/Resource/Runtime/Ember.Resource.Runtime.asmdef) | 程序集定义，依赖 Ember.Core.Runtime | — |
| [IResourceProvider.cs](../../Assets/Ember/Resource/Runtime/IResourceProvider.cs) | 资源提供者接口：Init / LoadAssetAsync / LoadSceneAsync / Unload / Progress | burner `IResourceProxy` |
| [EmberResourceManager.cs](../../Assets/Ember/Resource/Runtime/EmberResourceManager.cs) | 资源管理器门面，EmberMonoSingleton，委托 Provider 执行加载，管理生命周期事件 | burner `ResManager` |

### API 速览

```csharp
// 启动时
EmberResourceManager.Instance.Initialize(new AddressablesProvider(), success => { ... });

// 运行时
EmberResourceManager.Instance.LoadAssetAsync<Sprite>("ui/icons/coin", sprite => { ... });
EmberResourceManager.Instance.LoadSceneAsync("Battle");
EmberResourceManager.Instance.UnloadUnusedAssets();
```

### 生命周期

```
Initialize(provider)
    │
    ├─→ Provider.Initialize()
    └─→ Dispatch(ResourceReady)

销毁时：
    └─→ Dispatch(ResourceShutdown) → UnloadUnusedAssets
```

### 待扩展

- [ ] 默认 `ResourcesProvider` 实现（零配置开发入门）
- [ ] 引用计数与自动卸载策略
- [ ] 资源加载句柄（Handle）支持取消和追踪

---

## 3. UI 模块 `Ember.UI.Runtime`

> 状态：✅ 已完成
> burner 参考：`Assets/Game/GameLogic/GameManagers/UIFramework/` + `com.burner.uiextension`

### 设计思路

UI 模块管理所有界面的**层级关系**和**显示/隐藏切换**。
核心是四个 Canvas 层（每层一个界面栈）+ IUIView 生命周期的四个阶段。

```
层级：Background(0) → Normal(100) → Popup(200) → TopMost(300)

每层一个栈：
  Push → LoadAssetAsync → Instantiate → PauseTop → OnOpen → 压入栈
  Pop  → OnClose → Destroy → ResumeTop → 弹出栈
```

### 文件清单

| 文件 | 职责 | 参考 |
|------|------|------|
| [Ember.UI.Runtime.asmdef](../../Assets/Ember/UI/Runtime/Ember.UI.Runtime.asmdef) | 程序集定义，依赖 Ember.Core.Runtime | — |
| [IUIView.cs](../../Assets/Ember/UI/Runtime/IUIView.cs) | 界面生命周期接口：OnOpen / OnClose / OnPause / OnResume | burner `GameUIBase` |
| [PageDef.cs](../../Assets/Ember/UI/Runtime/PageDef.cs) | 页面元数据定义：预制体路径 + 层级，支持静态注册表 | burner `PageDef` |
| [EmberUIManager.cs](../../Assets/Ember/UI/Runtime/EmberUIManager.cs) | UI 管理器：层级 Canvas 按需创建、界面栈推送/Pop、生命周期分发 | burner `GameUIManager` |

### API 速览

```csharp
// 静态注册表（手写或工具生成）
public static class GamePages
{
    public static readonly PageDef MainMenu = new("ui/main_menu", UILayer.Normal);
    public static readonly PageDef Settings = new("ui/settings",  UILayer.Popup);
    public static readonly PageDef Loading  = new("ui/loading",   UILayer.TopMost);
}

// 打开页面
EmberUIManager.Instance.Push(GamePages.Settings, args: null);

// 返回键
EmberUIManager.Instance.Pop(UILayer.Popup);

// 检查有无弹窗
if (EmberUIManager.Instance.HasView((int)UILayer.Popup)) { ... }
```

### 生命周期

```
Push(GamePages.Settings, args)
    │
    ├─→ main_menu.OnPause()          ← 被遮挡
    ├─→ EmberResourceManager.LoadAssetAsync(预制体)
    ├─→ Instantiate
    └─→ settings.OnOpen(args)

按返回键 → Pop(UILayer.Popup)
    ├─→ settings.OnClose()
    ├─→ Destroy(settings)
    └─→ main_menu.OnResume()         ← 重新可见
```

### 待扩展

- [ ] Canvas 层自动挂载 Canvas + CanvasScaler + GraphicRaycaster 组件（→ S9 已通过 EnsureLayerRoot 实现）
- [ ] Pop 动画支持（淡入淡出、滑动）
- [ ] 按返回键自动 Pop 最顶层（内置返回键监听）

### 3.1 结构性重写：两层架构（2026-08-04 启动）⬅ 当前

对 UI 模块进行架构级重构，**采用与 burner 相同的两层架构**：

```
┌─────────────────────────────────────────────┐
│  EmberUIPageRouter (应用层)                   │
│  ─────────────────────────────               │
│  · PageType 路由分发                          │
│  · Show Queue 顺序打开                        │
│  · 父子页面追踪                               │
│  · BG Mask 模态遮罩管理                       │
│  · Prepare/Finalize 两阶段事件                │
│  · 返回键自动处理                              │
│  · Return Value 页面回传值                    │
├─────────────────────────────────────────────┤
│  EmberUIManager (框架层)                      │
│  ─────────────────────                       │
│  · Page 生命周期（加载/实例化/销毁）             │
│  · PageContext（MainPage ↔ Popups 关系）      │
│  · Canvas 层管理 + CanvasScaler 适配           │
│  · Update / LateUpdate 统一分发               │
│  · 安全遍历（pendingAdd / pendingDelete）      │
│  · Hide / Restore 机制                       │
│  · 过渡动画管道 (IUITransitionHandler)        │
│  · Frame Time Budget                         │
├─────────────────────────────────────────────┤
│  IUIResourceProvider (资源注入层)              │
│  ─────────────────────────                   │
│  · 解耦预制体加载，默认走 EmberResourceManager  │
│  · 支持注入 Mock 实现（测试用）                 │
└─────────────────────────────────────────────┘
```

#### 与 burner 的对应关系

| burner | ember | 定位 |
|--------|-------|------|
| `BurnerUIManager` (com.burner.uiextension) | `EmberUIManager` (Ember.UI.Runtime) | 框架层：页面生命周期 + 资源加载 |
| `GameUIManager` (Assets/Game/...) | `EmberUIPageRouter` (Ember.UI.Runtime) | 应用层：路由分发 + 业务集成 |
| `GameUILogic` | `IUIView`（扩展） | 页面基类 / 接口 |
| `GameUIBase` | 业务层自行实现 | 游戏特有的 UI 基类（不在框架中） |
| `PageDef` (游戏常量) | `PageDef` + 业务层 `GamePages` | 页面元数据 |

#### 职责边界

**EmberUIManager（框架层）—— 管"怎么打开/关闭"：**

| 职责 | 说明 |
|------|------|
| Page 生命周期 | `GamePage` 的创建 → 加载 → 实例化 → 显示 → 隐藏 → 销毁 |
| PageContext | 维护 MainPage 栈 + Popup 列表 + TopMost 列表的关系 |
| Canvas 管理 | 按需创建层级 Canvas，自动挂载组件，CanvasScaler 适配 |
| 安全遍历 | `pendingAdd` / `pendingDelete` 延迟队列，避免 Update 中修改集合 |
| Hide / Restore | 临时隐藏页面（不销毁），之后恢复 |
| Update 分发 | 每帧驱动所有可见页面的 Update / LateUpdate |
| Frame Time Budget | 限制单帧加载耗时，防止卡顿 |
| 过渡动画管道 | `IUITransitionHandler` 插件，框架层调用，业务层实现 |
| 资源加载 | 通过 `IUIResourceProvider` 加载预制体，默认走 ResourceManager |

**EmberUIPageRouter（应用层）—— 管"打开什么/何时打开"：**

| 职责 | 说明 |
|------|------|
| PageType 路由 | 根据 `PageType` 分发到框架层的不同方法（MainPage → ShowMainPage, Popup → ShowPopup） |
| Show Queue | 页面排队机制，多个系统同时请求时按序打开 |
| 父子追踪 | 记录 SubPage 的父页面，父关闭时自动关子 |
| BG Mask | 弹窗背景遮罩对象池，自动创建/回收 |
| 两阶段事件 | OnPreparePageOpen/Close（意图）、OnFinalizePageOpen/Close（完成） |
| 返回键 | 内置 Escape / Android Back，从 TopMost → Popup 逐层询问 |
| Return Value | 页面关闭时回传数据给打开者 |

**不做的事（留给实际 Game 项目）：**

burner `GameUIManager` 中有部分逻辑属于特定游戏的业务，框架不内置：
- 相机可见性切换（哪些页面隐藏 3D 相机）
- Volume / PostProcess 控制
- 页面与音频绑定
- 引导系统集成
- 具体页面常量（`PageDef.UIMainPage` 等）

这些通过 `OnPreparePageOpen` / `OnFinalizePageClose` 等事件钩子让业务层自行处理。

#### 新文件清单

```
Assets/Ember/UI/Runtime/
├── Ember.UI.Runtime.asmdef              ← 保持
├── IUIView.cs                           ← 扩展：+OnReopen +TryEscapeKeyClose
├── PageDef.cs                           ← 扩展：+PageType 字段
├── EmberUIEnums.cs                      ← 新增：PageType 枚举
├── EmberUIManager.cs                    ← 重写：框架层核心
├── EmberUIPageRouter.cs                 ← 新增：应用层路由（类似 GameUIManager）
├── EmberPageContext.cs                  ← 新增：MainPage + Popups 关系管理
├── EmberBgMaskPool.cs                   ← 新增：模态背景遮罩对象池
├── IUITransitionHandler.cs              ← 新增：过渡动画接口
├── IUIResourceProvider.cs               ← 新增：资源加载解耦接口
└── EmberUIEvents.cs                     ← 新增：UI 事件常量表
```

#### PageType 设计

```csharp
public enum PageType
{
    MainPage,   // 全屏页面，替换当前 MainPage，压入主栈（preserveContext 可选）
    Popup,      // 弹窗，叠加在当前 MainPage 之上，不替换，自动创建 BG Mask
    TopMost,    // 置顶弹窗，高于所有 Popup（如全局提示、Loading）
    SubPage,    // 子页面，嵌入父页面的指定区域（Tab 切换内容等），父关子关
    Overlay,    // 自由排序的覆盖层（如 Guide Mask、点击特效层），不受 MainPage/Popup 栈管理
}
```

与 `UILayer` 的关系：
- `UILayer` 决定**渲染排序**（Canvas.sortingOrder），保持现有四层预设值
- `PageType` 决定**行为模式**（如何入栈、如何与其它页面互动）
- 两者正交：例如 `PageType.Popup` 可以渲染在 `UILayer.Popup (200)` 或自定义层级

#### IUIView 扩展

```csharp
public interface IUIView
{
    void OnOpen(object args);        // 保留
    void OnClose();                  // 保留（关闭动画在此之后播放）
    void OnPause();                  // 保留
    void OnResume();                 // 保留
    
    // 新增
    void OnReopen(object args);      // 已加载页面被重新打开（热启动）
    bool TryEscapeKeyClose();        // 返回键处理，true=已处理阻止冒泡
}
```

#### 3.1.1 burner uiextension 包结构分析

burner 的 `com.burner.uiextension@1.0.2` 是一个**完整的 UI 框架包**，远超单纯的 Manager。以下是其完整结构及我们对应要做的：

```
com.burner.uiextension/
├── Runtime/
│   ├── Manager/                        # ← 框架层核心（本次重写重点）
│   │   ├── BurnerUIManager.cs          #    UI 管理器入口
│   │   ├── PageContext.cs              #    MainPage + Popups 关系管理
│   │   ├── GlobalEvents.cs             #    全局事件转发（点击/动画）
│   │   ├── ILogicResolver.cs           #    UI 逻辑类发现接口
│   │   └── CacheManager.cs             #    资源缓存
│   ├── Pages/                          # ← 页面生命周期（本次重写重点）
│   │   ├── GamePage.cs                 #    页面核心：加载/实例化/显示/隐藏/销毁
│   │   ├── GameUILogic.cs              #    UI 逻辑基类（类似我们的 IUIView）
│   │   ├── IUIBehaviour.cs             #    行为接口
│   │   ├── GameUIBinding.cs            #    代码生成的绑定数据
│   │   └── GameUIBindingTemplate.cs    #    绑定代码模板
│   ├── Components/                     # ← UI 组件封装层（Phase 2）
│   │   ├── GameUIComponent.cs          #    组件基类：事件/动画/挂件
│   │   ├── GameButton.cs               #    Button 封装
│   │   ├── GameText.cs                 #    Text/TMP 封装
│   │   ├── GameImage.cs                #    Image 封装
│   │   ├── GameScrollRect.cs           #    ScrollRect 封装
│   │   ├── GameTabLoader.cs            #    Tab 切换加载器
│   │   ├── GameProgressBar.cs          #    进度条
│   │   ├── GameRawImage.cs             #    RawImage 封装
│   │   ├── GameToggle.cs / GameToggleGroup.cs
│   │   ├── GameInputField.cs
│   │   ├── GameCanvas.cs
│   │   ├── GameUIContainer.cs          #    容器（列表、网格）
│   │   ├── GameUIAttachment.cs         #    动态挂件
│   │   └── GamePagePreloader.cs        #    页面预加载器
│   ├── UIExt/                          # ← UI 扩展组件（Phase 3）
│   │   ├── Tweener/                    #    Tween 动画系统（11 个文件）
│   │   ├── ButtonEx/ImageEx/...        #    扩展控件
│   │   ├── Gradient/AdvancedText/...   #    视觉效果
│   │   └── EventTriggerListener.cs     #    事件触发器
│   ├── Behaviour/                      #    附加行为组件
│   ├── SafeArea/                       #    安全区域适配
│   ├── NodeScreenShot/                 #    节点截图/模糊
│   └── Utils/                          #    工具类（对象池/列表池/扩展方法）
└── Editor/                             # ← 编辑器工具（Phase 4）
    ├── Pages/GameUIBindingEditor.cs    #    绑定编辑器（核心）
    ├── UIExt/                          #    各组件 Inspector
    ├── Previews/                       #    UI 预览
    ├── Validation/                     #    Prefab 校验
    └── Bake/                           #    UI 烘焙
```

##### 与 ember 的映射关系

| burner 包文件 | ember 对应 | 优先级 |
|--------------|-----------|--------|
| `BurnerUIManager.cs` | `EmberUIManager.cs`（重写） | 🔴 P0 |
| `PageContext.cs` | `EmberPageContext.cs`（新增） | 🔴 P0 |
| `GamePage.cs` | `EmberPage.cs`（新增） | 🔴 P0 |
| `IUIBehaviour.cs` | `IUIView.cs`（扩展） | 🔴 P0 |
| `GameUILogic.cs` | 合并到 `IUIView` — 我们不搞 Logic/Component 分离 | 🟡 P1 |
| `GameUIComponent.cs` | `EmberUIComponent.cs`（新增） | 🟡 P1 |
| `GameButton/Text/Image...` | `EmberButton/Text/Image...` | 🟡 P1 |
| `GlobalEvents.cs` | `EmberUIEvents.cs` | 🔴 P0 |
| `GameUIBinding.cs` | `EmberUIBinding.cs`（恢复+适配） | 🟢 P2 |
| `Tweener/` | `EmberTweener/` | 🟢 P2 |
| `SafeArea/` | `EmberSafeArea.cs` | 🟢 P2 |
| `Editor/` | `Ember.UI.Editor/` | 🟢 P2 |

##### 分阶段计划

```
Phase A: 框架层核心（本次重写，对应 burner Manager + Pages 目录）
  ├── EmberPage.cs              页面生命周期 → 对应 GamePage
  ├── EmberPageContext.cs       页面关系管理 → 对应 PageContext
  ├── EmberUIManager.cs         框架层管理器 → 对应 BurnerUIManager
  ├── EmberUIPageRouter.cs      应用层路由 → 对应 GameUIManager（精简版）
  ├── EmberUIEvents.cs          事件常量 + GlobalEvents
  ├── IUIView.cs（扩展）         对应 IUIBehaviour + GameUILogic（精简）
  ├── PageDef.cs（扩展）         加 PageType
  ├── EmberBgMaskPool.cs        模态遮罩对象池
  ├── IUITransitionHandler.cs   过渡动画接口
  └── IUIResourceProvider.cs    资源加载解耦

Phase B: 组件封装层（类似 burner Components/ 目录）
  ├── EmberUIComponent.cs       组件基类
  ├── EmberButton.cs            Button 封装
  ├── EmberText.cs              Text/TMP 封装
  ├── EmberImage.cs             Image 封装
  └── EmberScrollRect.cs        ScrollRect 封装

Phase C: 扩展 + 编辑器（类似 burner UIExt/ + Editor/ 目录）
  ├── EmberTweener/             Tween 系统
  ├── EmberSafeArea.cs          安全区域
  ├── EmberUIBinding.cs         代码生成绑定
  ├── EmberUIBindingGenerator   绑定代码生成器
  └── Editor/                   各组件 Inspector
```

##### 关键设计决策（与 burner 的差异）

| 决策 | burner | ember | 理由 |
|------|--------|-------|------|
| Logic/Component 分离 | `GameUILogic` 和 `GameUIComponent` 是两个独立类，Logic 持有 Component 引用 | 合并为扩展后的 `IUIView`，View 即 Logic | ember 不引入代码生成绑定系统（Phase C 才做），分离增加复杂度无收益 |
| 资源加载 | `CacheManager` + `IResourceHandle`（burner 自己的资源系统） | `IUIResourceProvider` 注入，默认走 `EmberResourceManager` | ember 已有 Resource 模块，不重复造轮子 |
| 类名解析 | `ILogicResolver` 从 Assembly 扫描类名→类型 | 暂不做，Phase C 跟绑定代码生成一起做 | 无代码生成时不需要 |
| 安全区域 | `BurnerSafeArea` 组件 | Phase C | 不是核心功能 |
| 节点截图/模糊 | `NodePostProcessManager` | 不做 | 属于渲染效果，超出框架范围 |

##### GamePage 核心机制（重点参照）

burner 的 `GamePage` 是整个框架的心脏，以下是需要吸收的关键机制：

| 机制 | 说明 | ember 对应 |
|------|------|-----------|
| **PageTargetState** | 页面加载/预加载期间的操作挂起队列。Show/Hide/Close 请求不会丢失，加载完成后自动执行 | `_pendingOp` 字段 |
| **LoadStages** | 分阶段加载：OnResLoad → OnInit → OnLoad → OnBecomeVisible → Loaded。配合 Frame Time Budget 分帧执行 | `_loadStage` 枚举 |
| **RenderVisible** | 独立于 `Visible` 的渲染剔除开关。隐藏 = Canvas.planeDistance = 100000（不渲染但保持状态） | `_renderVisible` 字段 |
| **ShouldHideLowerPage** | Popup 的模态能力：打开时遮挡下层页面（不销毁），关闭时恢复 | `_hideLower` 字段 |
| **SetActive** | 激活/休眠时统一处理 Canvas + Raycaster + Animator + ParticleSystem | `SetActiveInternal` 方法 |
| **安全遍历** | `isUpdating` 标志 + `pendingDelete` 列表，防止 Update 中修改集合 | 已有 pattern（burner 的 pendingAdd/Delete） |
| **延时销毁** | `DestroyValue` + `closeTime`，关闭后延迟 N 秒再真正销毁（用于关闭动画 + 快速重开） | `_destroyDelay` 字段 |
| **SubPage 管理** | 父页面持有子页面字典，父关子关，排序 order 自动递增 | `_subPages` 字典 |
| **PageLoadTiming** | 性能分析：记录 AssetLoad / Init / Open 各阶段耗时 | 轻量版 `_loadTiming` |

##### PageContext 核心机制（重点参照）

| 机制 | 说明 | ember 对应 |
|------|------|-----------|
| **MainPageList + Groups** | 支持多组独立的 MainPage 栈（如主界面 + 联盟界面各自独立） | 简化版：先做单组 |
| **PageContextEntry** | 每个页面条目携带：Page + Context（SortingOrder, PlaneDistance, Parameter）+ Popups 列表 | `ContextEntry` |
| **Prepare/Finalize 两阶段** | Prepare = 注册到栈、计算 SortingOrder；Finalize = 资源加载完后真正设置 | 同 |
| **HideLowerPage/ShowLowerPage** | 处理 ShouldHideLowerPage 的级联逻辑：一个 Popup 遮挡下方时，更下方的页面不再关心 | 同 |
| **SortingOrder 自动计算** | MainPageOrder=1000, PageGrowStep=500, InitialTopMostOrder=25000 | 同 |
| **PlaneDistance** | 控制 Canvas 深度，配合 SortingOrder 保证渲染顺序 | 同 |

---

#### Phase A 实施顺序（本次重写）

| 步骤 | 内容 | 依赖 | 对应 burner |
|------|------|------|------------|
| A1 | `EmberUIEnums.cs`（PageType）+ `PageDef` 扩展 + `IUIView` 扩展 | 无 | `PageFlags` + `IUIBehaviour` |
| A2 | `IUIResourceProvider` + 默认实现 | 无 | `CacheManager`（精简） |
| A3 | `EmberPageContext` — MainPage + Popups 关系 | A1 | `PageContext` |
| A4 | `EmberPage` — 页面生命周期核心 | A1-3 | `GamePage` |
| A5 | `EmberUIManager` — 框架层核心 | A1-4 | `BurnerUIManager` |
| A6 | `EmberUIEvents` — 事件常量 + GlobalEvents | A1 | `GlobalEvents` |
| A7 | `EmberBgMaskPool` — 模态遮罩池 | A4-5 | GameUIManager 中的 BgMask |
| A8 | `IUITransitionHandler` + 默认实现 | A5 | GameUILogic 中的动画 |
| A9 | `EmberUIPageRouter` — 应用层路由 | A5-8 | `GameUIManager`（精简） |
| A10 | Edit Mode 测试 | A4-9 | — |

---

## 4. Scene 模块 `Ember.Scene.Runtime`

> 状态：✅ 已完成
> burner 参考：`Assets/Game/GameLogic/GameManagers/GameScene/`

### 设计思路

Scene 模块封装 Unity SceneManager，提供异步加载/卸载、激活前回调、过渡切换。
核心是基于协程的进度轮询 + `allowSceneActivation` 机制，
在场景加载到 90% 时触发 `OnBeforeActivate`，允许模块在激活前做初始化。

### 文件清单

| 文件 | 职责 | 参考 |
|------|------|------|
| [Ember.Scene.Runtime.asmdef](../../Assets/Ember/Scene/Runtime/Ember.Scene.Runtime.asmdef) | 程序集定义，依赖 Core + Resource | — |
| [EmberSceneManager.cs](../../Assets/Ember/Scene/Runtime/EmberSceneManager.cs) | 场景管理器：异步加载/卸载/过渡，OnBeforeActivate 回调 | burner `GameSceneManager` |

### API 速览

```csharp
// 叠加加载
EmberSceneManager.Instance.LoadSceneAsync("Battle", () => Debug.Log("就绪"));

// 切换（加载新 + 卸载旧）
EmberSceneManager.Instance.TransitionTo("Battle", "MainMenu");

// 激活前回调（初始化时机）
EmberSceneManager.Instance.OnBeforeActivate += (scene, activate) =>
{
    // 初始化操作...
    activate(); // 完成后激活场景
};
```

### 生命周期

```
LoadSceneAsync("Battle")
    │
    ├─→ Progress: 0.0 → 0.9
    ├─→ OnBeforeActivate(scene, activate)
    ├─→ activate()  ← 由业务层调用
    ├─→ Progress: 1.0
    └─→ Dispatch(SceneLoaded)
```

## 5. Audio 模块 `Ember.Audio.Runtime`

> 状态：✅ 已完成
> burner 参考：`Assets/Game/GameLogic/GameManagers/Audio/`

### 文件清单

| 文件 | 职责 | 参考 |
|------|------|------|
| [Ember.Audio.Runtime.asmdef](../../Assets/Ember/Audio/Runtime/Ember.Audio.Runtime.asmdef) | 程序集定义，依赖 Ember.Core.Runtime | — |
| [EmberAudioManager.cs](../../Assets/Ember/Audio/Runtime/EmberAudioManager.cs) | 音频管理器：BGM/SFX 分离、Mixer 音量控制 | burner `AudioMgr` |

### API 速览

```csharp
EmberAudioManager.Instance.Init(mixer);
EmberAudioManager.Instance.PlayBGM(bgmClip, loop: true);
EmberAudioManager.Instance.PlaySFX(sfxClip);
EmberAudioManager.Instance.SetBGMVolume(0.8f);
```

---

## 6. Input 模块 `Ember.Input.Runtime`

> 状态：✅ 已完成
> burner 参考：Unity Input System 封装

### 文件清单

| 文件 | 职责 | 参考 |
|------|------|------|
| [Ember.Input.Runtime.asmdef](../../Assets/Ember/Input/Runtime/Ember.Input.Runtime.asmdef) | 程序集定义，依赖 Ember.Core.Runtime | — |
| [EmberInputManager.cs](../../Assets/Ember/Input/Runtime/EmberInputManager.cs) | 输入管理器：Action Map 切换、GetAxis/IsPressed | Unity Input System |

### API 速览

```csharp
EmberInputManager.Instance.Init(inputActions, defaultMap: "Gameplay");
EmberInputManager.Instance.SwitchMap("UI");
var move = EmberInputManager.Instance.GetAxis("Move");
if (EmberInputManager.Instance.IsPressed("Jump")) { ... }
```

---

## 7. Editor 模块 `Ember.Editor`

> 状态：✅ 已完成

### 文件清单

| 文件 | 职责 |
|------|------|
| [Ember.Editor.asmdef](../../Assets/Ember/Editor/Ember.Editor.asmdef) | 编辑器程序集，依赖 Core.Runtime + Core.Editor |
| [OdinIntegrationTest.cs](../../Assets/Ember/Editor/OdinIntegrationTest.cs) | Odin Inspector 集成检测工具 |

---

## 8. Manager 自动发现 `Ember.Core.Runtime`

> 状态：✅ 已完成
> burner 参考：`GameMgrCollector` + `IManager` + `[InitOrder]`

### 文件清单

| 文件 | 职责 | 参考 |
|------|------|------|
| [IEmberManager.cs](../../Assets/Ember/Core/Runtime/IEmberManager.cs) | 框架管道接口：Init() / Destroy()，启动时由 Collector 初始化 | burner `IManager` |
| [IEmberModule.cs](../../Assets/Ember/Core/Runtime/IEmberModule.cs) | 业务模块接口：OnInit() / OnDestroy() / ResetModuleData()，由状态机按 Phase 驱动 | — |
| [EmberInitOrderAttribute.cs](../../Assets/Ember/Core/Runtime/EmberInitOrderAttribute.cs) | 初始化顺序特性，预定义 Core=100 → Game=700 | burner `[InitOrder]` |
| [EmberManagerCollector.cs](../../Assets/Ember/Core/Runtime/EmberManagerCollector.cs) | 反射扫描 → 按 Order 排序 → 依次 Init / 逆序 Destroy | burner `GameMgrCollector` |

---

## 9. Update 循环管理器 `Ember.Core.Runtime`

> 状态：✅ 已完成
> burner 参考：`GameUpdateManager` + `IGameUpdate`

### 文件清单

| 文件 | 职责 | 参考 |
|------|------|------|
| [IEmberUpdate.cs](../../Assets/Ember/Core/Runtime/IEmberUpdate.cs) | IEmberUpdate / IEmberLateUpdate / IEmberFixedUpdate 接口 | burner `IGameUpdate` |
| [EmberUpdateManager.cs](../../Assets/Ember/Core/Runtime/EmberUpdateManager.cs) | 反射扫描 + 每帧统一驱动所有 IEmberUpdate | burner `GameUpdateManager` |

---

## 10. Timer 定时器 `com.ember.extensions`

> 状态：⬜ 待开始（暂定放入 com.ember.extensions，避免 Core 依赖 UniTask）
> burner 参考：`TimerManage`

### 规划

- int-ID API（Delay / Interval / Schedule / Cancel），内部委托 UniTask
- 保持 Core 零外部依赖，Timer 作为可选扩展

---

## 11. GameState 状态机 `Ember.Core.Runtime`

> 状态：✅ 已完成
> burner 参考：`GameStateManager` + `GameStateBase`

### 核心三状态体系

框架强制提供三个核心状态，覆盖单机/网游通用流程：

```
                 ┌──────────────────────────┐
                 │        Settings          │
                 │   (Push 覆盖，可删除)      │
                 └────┬─────────────────┬───┘
                    Push               Push
                      ↑                 ↑
Init ──→ [Login] ──→ Main ←──→ Gameplay ──→ Main ──→ ...
 框架     网游可选     大厅      核心玩法      回到大厅
```

| 状态 | 文件 | 职责 | IsRequired |
|------|------|------|------------|
| **Init** | [InitState.cs](../../Assets/Ember/Core/Runtime/State/InitState.cs) | 初始化所有 Manager，广播 CoreReady，自动 TransitionTo Main | ✅ |
| **Main** | [MainState.cs](../../Assets/Ember/Core/Runtime/State/MainState.cs) | 大厅/主界面枢纽。Init 完成后的着陆点，退出 Gameplay 后的归宿 | — |
| **Gameplay** | [GameplayState.cs](../../Assets/Ember/Core/Runtime/State/GameplayState.cs) | 核心玩法循环。OnEnter/OnExit/OnUpdate/OnPause/OnResume 完整生命周期 | — |

### 子类化模式

三个状态都采用"密封外层 + 虚内层"模式，子类不需要关心日志和事件广播：

```csharp
// MainState 示例
public class MyMainState : MainState
{
    protected override void OnMainEnter(object args)
    {
        // 显示主界面、播放 BGM
    }
    protected override void OnMainExit()
    {
        // 隐藏主界面
    }
}

// GameplayState 示例
public class BattleState : GameplayState
{
    protected override void OnGameplayEnter(object args) { /* 加载战斗场景 */ }
    protected override void OnGameplayUpdate() { /* 战斗主循环 */ }
    protected override void OnGameplayPause() { /* 暂停战斗 */ }
    protected override void OnGameplayResume() { /* 继续战斗 */ }
    protected override void OnGameplayExit() { /* 卸载战斗场景 */ }
}
```

### 文件清单

| 文件 | 职责 | 参考 |
|------|------|------|
| [EmberStateMachine.cs](../../Assets/Ember/Core/Runtime/State/EmberStateMachine.cs) | 状态机引擎 + EmberGameState 抽象基类 | burner `GameStateManager` |
| [InitState.cs](../../Assets/Ember/Core/Runtime/State/InitState.cs) | 框架内置必需状态（IsRequired = true），自动过渡到 MainState | — |
| [MainState.cs](../../Assets/Ember/Core/Runtime/State/MainState.cs) | 大厅/主界面状态 | — |
| [GameplayState.cs](../../Assets/Ember/Core/Runtime/State/GameplayState.cs) | 核心玩法状态 | — |
| [SettingsState.cs](../../Assets/Ember/Core/Runtime/State/SettingsState.cs) | 通用覆盖式设置状态，通过 <see cref="SettingsContext"/> 区分 Main/Gameplay 上下文 | — |

### 设计要点

**标准流程（单机）**：

```csharp
// GameLauncher.ConfigureStateMachine
fsm.Register(new InitState());
fsm.Register(new MainState());
fsm.Register(new GameplayState());

// 自动流转：Init → Main
// 用户触发：Main → Gameplay（点击"开始游戏"）
// 用户触发：Gameplay → Main（退出战斗）
```

**扩展流程（网游）**：

```csharp
// GameLauncher.ConfigureStateMachine（子类 override）
base.ConfigureStateMachine(fsm);
fsm.Register(new LoginState());      // Init → Login → Main
fsm.Register(new ReconnectingState());
```

**图形化编辑器预留**：
- `RegisteredStates` 返回所有已注册状态（反射枚举）
- `Name` / `Description` 给编辑器展示
- `IsRequired = true` 的状态不可注销
- `Unregister<T>()` 拒绝删除必需状态和当前活跃状态

**TransitionTo vs Push/Pop**：

```csharp
// TransitionTo：替换式切换（Init → Main, Main → Gameplay）
fsm.TransitionTo<GameplayState>();

// Push/Pop：覆盖式弹窗（暂停 Gameplay 打开设置）
fsm.Push<SettingsState>();
fsm.Pop();
```

---

## 12. 日志系统 `Ember.Core.Runtime`

> 状态：✅ 已完成
> burner 参考：`Debuger`
> 详细文档：[docs/dev/ember-debug.md](../../docs/dev/ember-debug.md)

### 文件清单

| 文件 | 职责 |
|------|------|
| [EmberDebug.cs](../../Assets/Ember/Core/Runtime/EmberDebug.cs) | 日志核心：消息分色（Info/Init/Event/Cleanup/Warning/Error）+ 两级标签级联过滤 |
| [EmberLogPresets.cs](../../Assets/Ember/Core/Runtime/EmberLogPresets.cs) | 集中定义：LogTags（标签常量）、LogTagColors（预定义颜色）、LogColors（消息颜色） |
| [EmberDebugConfigSO.cs](../../Assets/Ember/Core/Runtime/EmberDebugConfigSO.cs) | SO 配置容器：全局开关、按类过滤、颜色管理 |
| [EmberDebugConfigEditor.cs](../../Assets/Ember/Core/Editor/EmberDebugConfigEditor.cs) | SO 自定义 Inspector：锁住预定义颜色、层级缩进、批量操作 |
| [EmberDebugConfigCreator.cs](../../Assets/Ember/Core/Editor/EmberDebugConfigCreator.cs) | 自动创建 SO（Unity 启动时检测，无则生成） |
| [GameLauncher.cs](../../Assets/Ember/Core/Runtime/GameLauncher.cs) | 游戏启动器：集中入口，驱动 Manager 初始化 → 状态机 → Update 循环 | — |

### API 速览

```csharp
private const string TAG = LogTags.CoreEventBus;

EmberDebug.Log(TAG, "普通消息");           // 白色
EmberDebug.LogInit(TAG, "初始化完成");      // 绿色
EmberDebug.LogEvent(TAG, "事件播报");       // 紫色
EmberDebug.LogCleanup(TAG, "清理资源");     // 灰色
EmberDebug.LogWarning(TAG, "异常");         // 白色+黄底
EmberDebug.LogError(TAG, "错误");           // 白色+红底，不受开关控制

// 过滤
EmberDebug.Disable(LogTags.Audio);          // 父标签关闭 → 所有子标签静默
EmberDebug.Disable(LogTags.CoreEventBus);   // 只关子标签
EmberDebug.GlobalOpen = false;              // 全关（Error 除外）
```

---

## Module 系统设计（待实现）

### 问题

`IEmberManager` 只覆盖"应用启动即初始化"的框架管道（7 个 Manager）。
业务模块（战斗、背包、网络）需要由状态机按需驱动 —— 进入 BattleState 才初始化，
退出时销毁。

### 两层初始化模型

```
┌────────────────────────────────────────────┐
│  IEmberManager (框架管道)                    │
│  ────────────────────────                   │
│  启动 → InitializeAll() → 全局存活 → 退出销毁  │
│  EmberUpdateManager / Resource / Audio ...  │
│                                             │
│  IEmberModule (业务模块)                     │
│  ────────────────────────                   │
│  Phase 1 → Login 后初始化                    │
│  Phase 2 → 进 Battle/MainMenu 初始化          │
│  状态退出 → OnDestroy / ResetModuleData      │
└────────────────────────────────────────────┘
```

### 接口定义

| 接口 | 文件 | 定位 |
|------|------|------|
| `IEmberManager` | [IEmberManager.cs](../../Assets/Ember/Core/Runtime/IEmberManager.cs) | 框架管道，启动时初始化 |
| `IEmberModule` | [IEmberModule.cs](../../Assets/Ember/Core/Runtime/IEmberModule.cs) | 业务模块，状态机驱动 |

两者**不继承**——EmberManagerCollector 只扫 `IEmberManager`，
EmberModuleCollector（未来实现）只扫 `IEmberModule`，互不干扰。

### 初始化流程

```
GameLauncher.Awake()
│
├─ EmberManagerCollector.InitializeAll()   ← 扫 IEmberManager（7 个管道）
│
└─ Fsm.Start<InitState>()
      │
      └─ Fsm.TransitionTo<LoginState>()
           │
           ├─ LoginState.OnEnter()
           │     EmberModuleCollector.InitPhase(1)   ← 扫 IEmberModule.Phase == 1
           │
           └─ LoginState.OnExit()
                 EmberModuleCollector.DestroyPhase(1)
```

### 待实现

- [ ] `EmberModuleCollector`：按 Phase 分组，对接状态机生命周期
- [ ] `Phase` 预定义常量（如 `ModulePhase.Login = 1, Gameplay = 2`）
- [ ] 热重启：`ResetModuleData()` 在一次游戏会话中复用模块对象

---

## Package 迁移：从 burner 包汲取到 ember 包 ⬅ 当前

> 启动：2026-08-04
> 策略：从 `com.ember.basic` 开始，逐个脚本取消注释 → 适配 ember 规范 → 优化

### 背景

项目 `Packages/` 下已复制了 burner 的三个包，全部代码均已注释（`////`），等待适配：

| 包 | 文件数 | 来源 | 状态 |
|---|--------|------|------|
| `com.ember.basic` | 36 cs | `com.burner.basic` | ⬜ 全部注释，待适配 |
| `com.ember.extensions` | 17 cs | `com.burner.extensions` | ⬜ 全部注释，待适配 |
| `com.ember.uiextension` | 80+ cs | `com.burner.uiextension` | ⬜ 全部注释，待适配 |

### 迁移策略

采用**逐文件审计**方式，而不是批量取消注释。每个脚本走以下流程：

```
1. 阅读 burner 原始代码 → 理解用途
2. 判断：是否需要？ → 否 → 删除文件
3. 判断：是否需要修改？ → 记录优化点
4. 取消注释 → 改命名空间 → 适配 ember 规范
5. 更新 asmdef 引用（如需要）
6. 标记完成
```

### 迁移规范

| 规则 | burner 原始 | ember 目标 |
|------|------------|-----------|
| 命名空间 | `Burner.Basic` / `Burner.Extensions` | `Ember.Basic` / `Ember.Extensions` |
| 日志 | `Burner.Logger` / `Debuger.Log` | `EmberDebug.Log`（框架代码）或 `UnityEngine.Debug`（基础工具类保留） |
| 类名前缀 | 无统一前缀 | 工具类保持原名，框架类加 `Ember` 前缀 |
| 版权头 | burner copyright | 替换为 ember copyright |
| 依赖 | `Burner.Basic.Tasks` 等 | 改为 ember 对应包 |
| 单例模式 | `Singleton<T>`（纯 C#） | 已有 `EmberSingleton<T>`，评估是否重复 |
| 对象池 | `ListPool<T>` 等 | 评估是否需要；ember Core 已有 `EmberObjectPool` |

### 1. com.ember.basic（36 个文件）⬅ 从这里开始

#### 1.1 目录结构与优先级

```
com.ember.basic/Runtime/
├── Async/                          # STTask 异步系统（6 文件）
│   ├── STTask.cs                   #   值类型 Task，零 GC
│   ├── STTaskCompletionSource.cs   #   Task 完成源
│   ├── STTaskFactory.cs            #   Task 工厂
│   ├── STAsyncTaskMethodBuilder.cs #   async/await 构建器
│   ├── IAwaiter.cs                 #   Awaiter 接口
│   ├── MoveNextRunner.cs           #   状态机驱动
│   └── AsyncMethodBuilderAttribute.cs
├── Base/                           # 基础数据结构（10 文件）
│   ├── Attributes.cs               #   HasGC / NoGC / ForTest / ForDebug 标记
│   ├── ListPool.cs                 #   List 对象池
│   ├── DictionaryPool.cs           #   Dictionary 对象池
│   ├── HashSetPool.cs              #   HashSet 对象池
│   ├── MemoryPool.cs               #   内存池
│   ├── PoolRefCount.cs             #   池引用计数（Editor 调试用）
│   ├── IPool.cs                    #   池接口
│   ├── QuickQueue.cs               #   快速队列
│   ├── StringView.cs               #   字符串视图（零分配子串）
│   ├── ValueTypeList.cs            #   值类型 List（unsafe）
│   └── CacheSortedList.cs          #   缓存排序列表
├── Extension/                      # 扩展方法（3 文件）
│   ├── CollectionExtension.cs      #   集合扩展
│   ├── StringExtension.cs          #   字符串扩展
│   └── Il2Cpp*.cs                  #   IL2CPP 编译属性
├── LitJson/                        # JSON 库（8 文件）
│   ├── JsonMapper.cs / JsonData.cs / JsonReader.cs / JsonWriter.cs / Lexer.cs
│   └── ...
├── Resource/                       # 资源接口（1 文件）
│   └── IUpdater.cs                 #   更新器接口
├── Unsafe/                         # 不安全代码（2 文件）
│   ├── NativeDataTypes.cs          #   原生数据类型
│   └── UnsafeString.cs             #   不安全字符串操作
└── Utils/                          # 工具（1 文件）
    └── Const.cs                    #   常量定义
```

#### 1.2 分批迁移计划

##### 第一批：Base 基础数据结构（核心依赖，其他模块依赖它们）

| # | 文件 | 决策 | 说明 |
|---|------|------|------|
| B1 | `Attributes.cs` | ✅ 已完成 | 标记用 Attribute：HasGC / NoGC / ForTest / ForDebug / Legacy |
| B2 | `IPool.cs` | ✅ 已完成 | 可池化对象接口（去掉了 burner 的 [Obsolete]） |
| B3 | `MemoryPool.cs` | ✅ 已完成 | 泛型对象池，API 重命名：Alloc→Get, Free→Return |
| B4 | `PoolRefCount.cs` | ✅ 已完成 | Editor 下池泄漏追踪（替换 burner 的 ForEach 为标准 foreach） |
| B5 | `ListPool.cs` | ✅ 已完成 | List 对象池，移除 PopLeast 别名，API：Pop→Get, Push→Return |
| B6 | `DictionaryPool.cs` | ✅ 已完成 | Dictionary 对象池 |
| B7 | `HashSetPool.cs` | ✅ 已完成 | HashSet 对象池 |
| B8 | `QuickQueue.cs` | ✅ 已完成 | 零 GC 双端队列，重构 InsertSorted，统一 ember 错误消息 |
| B9 | `ValueTypeList.cs` | ✅ 已完成 | 值类型 List，重命名内部 Comparer 类避免冲突 |
| B10 | `StringView.cs` | ✅ 已完成 | 零分配字符串视图，替换 StringExtension.ToAlphaLower → char.ToLowerInvariant |
| B11 | `CacheSortedList.cs` | ✅ 已完成 | 红黑树有序列表，重构 FixRemove 为左右分支独立方法，统一错误消息 |

##### 第二批：Extension 扩展方法（依赖 Base）

| # | 文件 | 决策 | 说明 |
|---|------|------|------|
| B12 | `CollectionExtension.cs` | ✅ 迁移 | 集合扩展方法 |
| B13 | `StringExtension.cs` | ✅ 迁移 | 字符串扩展方法 |
| B14 | `Il2CppEagerStaticClassConstructionAttribute.cs` | ✅ 迁移 | IL2CPP 编译提示 |
| B15 | `Il2CppSetOptionAttribute.cs` | ✅ 迁移 | IL2CPP 编译选项 |

##### 第三批：Async 异步系统（依赖 Base）

| # | 文件 | 决策 | 说明 |
|---|------|------|------|
| B16 | `IAwaiter.cs` | ✅ 迁移 | Awaiter 接口 |
| B17 | `STTask.cs` | ✅ 迁移 | 值类型 Task，零 GC 异步核心 |
| B18 | `STTaskCompletionSource.cs` | ✅ 迁移 | Task 完成源 |
| B19 | `STTaskFactory.cs` | ✅ 迁移 | Task 工厂方法 |
| B20 | `STAsyncTaskMethodBuilder.cs` | ✅ 迁移 | async/await 编译器支持 |
| B21 | `MoveNextRunner.cs` | ✅ 迁移 | 状态机驱动 |
| B22 | `AsyncMethodBuilderAttribute.cs` | ✅ 迁移 | 编译属性 polyfill |

##### 第四批：LitJson（零依赖）

| # | 文件 | 决策 | 说明 |
|---|------|------|------|
| B23-30 | `LitJson/` 全部 8 文件 | ✅ 迁移 | 轻量 JSON 库，无外部依赖；注意：Unity 6 已有内置 JsonUtility，LitJson 用于需要 `JsonData` 动态解析的场景 |

##### 第五批：其他

| # | 文件 | 决策 | 说明 |
|---|------|------|------|
| B31 | `IUpdater.cs` | ⚠️ 评估 | 资源更新器接口，确认是否已被 `IResourceProvider` 替代 |
| B32 | `NativeDataTypes.cs` | ⚠️ 评估 | unsafe，仅用于极端性能场景 |
| B33 | `UnsafeString.cs` | ⚠️ 评估 | unsafe，确认使用场景 |
| B34 | `Const.cs` | ✅ 迁移 | 常量定义 |

#### 1.3 迁移注意事项

- **命名空间统一**：`Burner.Basic` → `Ember.Basic`，子目录如 `Async` 可保持子命名空间 `Ember.Basic.Async`
- **asmdef 引用**：`com.ember.basic` 应零依赖（除 Unity 引擎），不依赖 `Ember.Core.Runtime`
- **日志处理**：basic 包是底层工具库，应避免依赖 `EmberDebug`。保留使用 `UnityEngine.Debug` 或提供可注入的 `LogAction`
- **与现有 ember 代码的冲突检查**：`Ember.Core.Runtime` 中已有 `EmberObjectPool`、`EmberSingleton`，确认 basic 包的工具类不与之重叠
- **burner 特定引用清理**：搜索 `Burner.`、`Debuger.` 引用，替换为 ember 对应物或移除

#### 1.4 完成状态：✅ 已完成（2026-08-04）

全部 36 个文件已迁移适配。变更汇总：

| 目录 | 文件数 | 主要变更 |
|------|--------|---------|
| `Base/` | 11 | 命名空间, API 重命名(Pop→Get, Push→Return), 移除 burner wiki 链接, 错误消息 ember 化, StringView 去除 StringExtension 依赖, CacheSortedList FixRemove 重构 |
| `Extension/` | 4 | 命名空间, Implode→JoinToString, 移除 ParallelForEach 中 burner 异常消息, Il2Cpp polyfill 保持原命名空间 |
| `Async/` | 7 | 命名空间, 字段名 `_camelCase`, 异常消息 ember 化 |
| `LitJson/` | 8 | 命名空间, 表达式体属性语法, 移除 NETSTANDARD1_5 条件编译 |
| `Resource/` | 1 | 命名空间, XML 文档, IUpdater+IDelayDisposable |
| `Unsafe/` | 2 | 命名空间, 字段 PascalCase, 方法 PascalCase, UnsafeString 放入 Ember.Basic |
| `Utils/` | 1 | 重命名 Const→SharedConst, 字段语义化命名 |

### 2. com.ember.extensions（17 个文件）

> 状态：⬜ 待开始（basic 包完成后启动）

#### 2.1 目录结构

```
com.ember.extensions/Runtime/
├── Async/
│   ├── STTaskFactory.cs                        # STTask 的 Unity 扩展工厂
│   └── SingleThreadSynchronizationContext.cs   # 单线程同步上下文
├── Base/
│   ├── CacheLRUList.cs                         # LRU 缓存列表
│   ├── Singleton.cs                            # 纯 C# 单例（与 EmberSingleton 重复？）
│   └── ThreadPool.cs                           # 线程池
├── Extension/
│   └── UnityExtension.cs                       # Unity 类型扩展方法
├── Resource/
│   ├── CacheManager.cs                         # 资源缓存管理器
│   ├── ILoaderHandle.cs                        # 加载器句柄接口
│   ├── IResourceLoader.cs                      # 资源加载器接口
│   ├── IResourceProxy.cs                       # 资源代理接口
│   └── ResourceLoader.cs                       # 资源加载器实现
└── Utils/
    ├── CachedIntPtrStrings.cs                  # IntPtr 字符串缓存
    ├── FieldsInitializer.cs                    # 字段初始化器
    ├── GameObjectUtils.cs                      # GameObject 工具方法
    ├── JsonUtils.cs                            # JSON 工具
    ├── StreamHelper.cs                         # 流处理工具
    └── Utility.cs                              # 通用工具
```

### 3. com.ember.uiextension（80+ 个文件）

> 状态：⬜ 待开始（basic + extensions 包完成后启动）
> 注意：此包的迁移与 §3.1 的 UIManager 重写密切相关，届时需要协调

---

## 程序集依赖图

```
Ember.Core.Runtime          (零依赖，叶子)
    ↑
    ├── Ember.Resource.Runtime
    │       ↑
    │       └── Ember.Scene.Runtime
    ├── Ember.UI.Runtime
    ├── Ember.Audio.Runtime
    └── Ember.Input.Runtime
```

---

## ▶ 接下来要做的事

| 优先级 | 事项 | 文件数 | 说明 |
|--------|------|--------|------|
| 🔴 P0 | **com.ember.extensions 包迁移** | 17 | Resource/Cache/ThreadPool/UnityExtension/GameObjectUtils 等。依赖 basic 包（已完成），迁移后 Timer 等新功能有处放 |
| 🔴 P0 | **UIManager 结构性重写 — Phase A** | ~10 | 框架层核心：EmberPage + EmberPageContext + EmberUIManager + EmberUIPageRouter 两层架构。参照 burner uiextension 包 |
| 🟡 P1 | **com.ember.uiextension 包迁移** | 80+ | UI 组件（GameButton/Text/Image 等）+ UIExt（Tweener 等）+ Editor。量大，分批做 |
| 🟡 P1 | **Module 系统** | 3 | `EmberModuleCollector` + `ModulePhase`，接口已定义 |
| 🟡 P1 | **Timer 定时器** | 1 | 放入 extensions，int-ID API（Delay/Interval/Schedule/Cancel），内部委托 UniTask |
| 🟡 P1 | **架构债务清理** | — | SceneManager 走 Resource、ServiceLocator 梳理、GameStateChanged 重复 dispatch |
| 🟢 P2 | **UI 绑定代码生成恢复** | — | EmberUIBinding + EmberUIBindingGenerator |
| 🟢 P2 | **Audio 多 Category + AudioAgent 池** | ~2 | |
| 🟢 P2 | **预制体对象池** | 1 | GameObject 预制体池化 |
| 🟢 P2 | **本地化** | — | |

<h3>当前进度概览</h3>

```
已完成：
  ✅ Core / Resource / UI / Scene / Audio / Input / Editor 模块（7 个）
  ✅ Manager 自动发现 / Update 循环 / GameState 状态机 / 日志系统
  ✅ S1-S9 场景集成验证
  ✅ com.ember.basic 包迁移（36 Runtime + 7 编辑器基础设施 + 21 编辑器工具 = 64 文件）
  ✅ API 速查手册（docs/dev/ember-api-reference.md）
  ✅ 编辑器工具测试清单（docs/dev/editor-tools-test-checklist.md, ~80 测试点）

进行中：
  🧪 编辑器工具手动测试

待开始：
  ⬜ com.ember.extensions 包迁移（17 文件）
  ⬜ UIManager 结构性重写 Phase A
  ⬜ com.ember.uiextension 包迁移（80+ 文件）
  ⬜ Module 系统 / Timer / 架构债务
```

---

## 编码规范速查

| 规则 | 示例 |
|------|------|
| 框架类前缀 | `EmberEventBus`、`EmberServiceLocator` |
| 接口 I 开头 | `IEmberService`、`IUIView` |
| 命名空间 | `Ember.Core`、`Ember.UI`、`Ember.Resource` |
| 私有字段 `_camelCase` | `_eventDict`、`_isInitialized` |
| 优先 `internal` | 只暴露必要的 `public` API |
| 禁止 | `GameObject.Find`、`FindObjectOfType` |

---

## 变更日志

| 日期 | 变更 |
|------|------|
| 2026-07-25 | 创建框架目录结构，13 个目录 + 13 个 .asmdef |
| 2026-07-25 | 完成 burner 项目框架层全面分析 |
| 2026-07-29 | Core 模块完成（EventBus / ServiceLocator / Singleton / ObjectPool / BroadcastEvent） |
| 2026-07-29 | UI 模块完成（IUIView / PageDef / EmberUIManager） |
| 2026-07-29 | Resource 模块完成（IResourceProvider / EmberResourceManager / ResourcesProvider） |
| 2026-07-30 | Scene / Audio / Input / Editor 模块完成 |
| 2026-07-30 | Manager 自动发现系统完成（IEmberManager + EmberManagerCollector） |
| 2026-07-30 | Update 循环管理器完成（IEmberUpdate + EmberUpdateManager） |
| 2026-07-30 | burner 基础包迁移（basic / extensions / uiextension，全部注释待适配） |
| 2026-07-31 | GameState 状态机完成（EmberStateMachine + InitState） |
| 2026-07-31 | Timer 决定放入 com.ember.extensions，保持 Core 零外部依赖 |
| 2026-07-31 | EmberDebug 日志系统完成（两级标签 + 消息分色 + SO 面板 + 全框架统一） |
| 2026-07-31 | 🔴 债务清理：5 个 Manager 统一实现 IEmberManager + [EmberInitOrder]，EmberUpdateManager 去 MonoBehaviour 化为纯 C# 类，新建 GameLauncher 集中驱动入口 |
| 2026-07-31 | 架构固化：定义 IEmberModule 接口，明确两层初始化模型（框架管道 vs 业务模块），Manager vs Module 平行不继承 |
| 2026-07-31 | EmberEventBus API 对齐 UniRx：Subscribe 返回 IDisposable，Dispatch 改名 OnNext |
| 2026-07-31 | InitState 接管 Manager 初始化（对齐 burner InitProcedure 模式） |
| 2026-07-31 | Camera 独立模块：CinemachineBrain + BlenderSettings + 相机堆栈 + 强制霸占模式 |
| 2026-07-31 | 新建 EmberBaseSO（继承溯源面板），Core 按功能分子文件夹 |
| 2026-08-01 | 🧪 场景验证 S1-S2 通过：GameBoot 场景搭建完成，7 个 Manager 初始化链路全部正常 |
| 2026-08-01 | 🐛 修复退出 Play 时 Odin [ShowInInspector] 访问已销毁对象的报错 |
| 2026-08-01 | 🔧 **架构改造：多场景叠加替代 DontDestroyOnLoad**（避开 Unity 6 DDOL 销毁竞态 bug） |
| 2026-08-01 | 🏗️ EmberSingleton 拆分：`EmberMonoSingleton`（无 DDOL）+ `EmberMonoSingletonDontDestroy`（含 DDOL） |
| 2026-08-01 | 📐 GameLauncher 退化为普通 `EmberMonoSingleton`（无 DDOL），由 FrameworkScene 保活 |
| 2026-08-01 | 🔨 FrameworkSceneBootstrapper：编译 + 文件变更时自动同步 Build Settings 场景列表 |
| 2026-08-01 | 🔘 Toolbar 自定义按钮（Unity 6 MainToolbarElement API）+ MenuItem 兜底，一键跳转 FrameworkScene |
| 2026-08-01 | 🧪 **退出 Play Mode 无报错**：S1-S2 重新验证通过，7 个 Manager 初始化链路全部正常 |
| 2026-08-03 | 🧪 **S3 验证通过**：Update/LateUpdate/FixedUpdate 循环正常，EmberUpdateDiagnostics 帧计数递增 |
| 2026-08-03 | 🧪 **S4 验证通过**：EmberEventBus 事件订阅/播报链路正常，GameStateChanged 事件确认可达 |
| 2026-08-03 | 🐛 **S5 修复日志系统**：`GlobalOpen` 读 SO 实时值，`IsEnabled` 优先查 SO，`Disable/Enable` 双向同步 SO |
| 2026-08-03 | 🏗️ **核心三状态体系**：Init → Main → Gameplay，密封外层 + 虚内层模式，子类只 override 业务钩子 |
| 2026-08-03 | 🏗️ **TransitionDescriptor 流转描述符**：声明流转目标 + Label + Condition + Guard，可视化编辑器 + 运行时校验双用途 |
| 2026-08-03 | 🏗️ **SettingsState 通用覆盖状态**：通过 `SettingsContext` 枚举传入上下文（Main / Gameplay），`IsRequired = false` 可被替换 |
| 2026-08-03 | 🏗️ **Init 预加载 MainScene**：`fsm.LoadSceneAsync` 委托 + `TransitionTo(skipSceneLoad)`，Init 先加载场景再过渡 |
| 2026-08-03 | 🏗️ **BootSplash + LoadingView 双遮罩**：BootSplash（Frame 0 黑幕，首次 LoadDone 后销毁）+ LoadingView（后续切换进度条，首跳跳过） |
| 2026-08-03 | 🏗️ **Init 启动动画预留**：`EmberInitAnimationStarter` 基类 + `InitSceneReady`/`InitAnimationDone` 事件，用户继承 override 即可 |
| 2026-08-03 | 🏗️ **场景映射 SO + 快速打开场景**：`EmberSceneMapping` 自动扫描状态 + 匹配同名场景，Toolbar 窗口一键打开 Framework + 目标场景 |
| 2026-08-03 | 🔧 **Play Mode 场景清理 + 退出恢复**：`FrameworkSceneBootstrapper` 点 Play 自动关闭多余场景，退出后恢复 |
| 2026-08-03 | 🔧 **事件 Key 间隔改为 1000**：SceneLoadDone 从 404 改为 4004，避免 HTTP 404 混淆 |
| 2026-08-03 | 🔧 **LogShutdown 淡紫色日志**：对应 LogInit 绿色，框架退出专用 |
| 2026-08-03 | 🔧 **Odin 编码规范补充**：$GROUP 成员引用语法不能拼接字符串，写入 odin-usage-notes.md §2.8 |
| 2026-08-04 | 📐 **UIManager 结构性重写启动**：对 `EmberUIManager` 进行架构重构，详见 §3.1 |
| 2026-08-04 | 📦 **Package 迁移计划制定**：分析 burner uiextension 包结构（80+ 文件），制定 ember 三层 Package（basic / extensions / uiextension）逐文件迁移策略，从 `com.ember.basic` 开始 |
| 2026-08-04 | ✅ **com.ember.basic 迁移完成**：36 文件全部适配（namespace / 命名 / API 优化 / `[HasGC]` `[NoGC]` 标注）。从用户旧项目整合 6 个工具（MathExtension / FloatCurve2D / NaturalStringComparer / FileEncodingUtility / DisplayFirstElementInHeader / DataSaver）。建立 API 速查手册 `docs/dev/ember-api-reference.md`，覆盖 73 文件、110+ 类型、570+ 成员 |
| 2026-08-04 | 🛠️ **编辑器工具全面优化**：从用户旧项目迁移 26 个编辑器工具，删除 5 个（重复/Odin 换壳/项目特定/URP 特定），保留 21 个并全部手动优化——统一继承 `EmberEditorWindow : OdinEditorWindow` 基类、提取 `EditorToolUtility` / `SpriteImportUtility` / `QuickMaintenanceTools` 等共享模块、所有工具加右键快捷菜单统一到 `GameObject/Ember/` 和 `Assets/Ember/` 路径、中英文双语支持。测试清单 `docs/dev/editor-tools-test-checklist.md` 已生成（~80 个测试点） |


---

# ▶ 场景集成 & 框架自检 — 全部完成 ✅

> 当前进度：S1-S9 全部通过，Init → Main 场景加载链路正常，LoadingPage + BootSplash 防护就绪。

### 阶段目标

| 序号 | 任务 | 状态 |
|------|------|------|
| **S1** | 搭建 GameBoot 场景 | ✅ |
| **S2** | 验证 Manager 初始化链路 | ✅ 8 个 Manager（含 SceneCoordinator） |
| **S3** | 验证 Update 循环 | ✅ |
| **S4** | 验证事件总线 | ✅ |
| **S5** | 验证/修复日志系统 | ✅ GlobalOpen + 实时生效 + LogShutdown |
| **S6** | 验证相机模块 | ✅ UICamera=OK, MainCamera=OK, Brain=OK |
| **S7** | 整理遗留问题 | ✅ |
| **S8** | LoadingPage + BootSplash | ✅ 双遮罩：BootSplash（启屏黑幕）+ LoadingView（场景切换进度） |
| **S9** | EmberUIManager 完善 | ✅ EnsureLayerRoot 自动挂载 Canvas 三件套 |

<!-- ═══════════════════════════════════════════════════════════════ -->
<!-- >>> CURRENT PHASE — 编辑器工具测试中 + extensions 包迁移待启动 <<< -->
<!-- ═══════════════════════════════════════════════════════════════ -->

### 架构快照（2026-08-03）

```
FrameworkScene.unity（启动场景，index 0，永不卸载）
  └── GameBoot
      ├── GameLauncher（EmberMonoSingleton）
      ├── GameBootCoordinator（可选）
      ├── UIRoot
      │     ├── BootSplash（EmberBootSplash，Frame 0 黑幕）
      │     └── LoadingPage（EmberLoadingView，进度条）
      ├── MainCamera（CinemachineBrain + DefaultCinemachineCamera）
      ├── UICamera（Overlay）
      └── EventSystem

启动流程：
  1. FrameworkScene 加载 → BootSplash Frame 0 黑幕
  2. GameLauncher.Start → Fsm.Start<InitState>
  3. InitState → InitializeAll → LoadSceneAsync("MainScene") → InitSceneReady
  4. EmberInitAnimationStarter（MainScene 上）→ InitAnimationDone
  5. TransitionTo<MainState>(skipSceneLoad) → BootSplash 销毁
  6. Main→Gameplay：LoadingView 进度条 → 场景切换

场景加载：
  Init ──(LoadSceneAsync预加载)──→ Main ──(TransitionTo正常加载)──→ Gameplay
  FrameworkScene 始终常驻，其余 Additive 叠加/卸载

关键文件：
  Assets/Ember/Core/Runtime/GameLauncher.cs              — 入口 + 状态机
  Assets/Ember/Core/Runtime/State/InitState.cs           — 预加载 MainScene + InitSceneReady/AnimationDone
  Assets/Ember/Core/Runtime/State/MainState.cs            — 大厅状态
  Assets/Ember/Core/Runtime/State/GameplayState.cs        — 玩法状态
  Assets/Ember/Core/Runtime/State/SettingsState.cs        — 覆盖式设置
  Assets/Ember/Core/Runtime/State/EmberStateMachine.cs    — 状态机 + TransitionTo(skipSceneLoad) + LoadSceneAsync
  Assets/Ember/Core/Runtime/State/TransitionDescriptor.cs — 流转描述符
  Assets/Ember/Scene/Runtime/SceneCoordinator.cs          — 场景加载桥接
  Assets/Ember/Scene/Runtime/EmberSceneManager.cs         — 场景异步加载
  Assets/Game/UI/EmberBootSplash.cs                       — 启屏黑幕
  Assets/Game/UI/EmberLoadingView.cs                      — 加载进度
  Assets/Game/UI/EmberInitAnimationStarter.cs             — 启动动画基类
  Assets/Ember/Editor/EmberSceneMapping.cs                — 状态↔场景映射 SO
  Assets/Ember/Editor/EmberSceneQuickOpener.cs            — 快速打开场景窗口
  Assets/Ember/Editor/FrameworkSceneBootstrapper.cs       — 自动同步 Build + Play 清理/恢复
```

---

---

## 技术债务 & 待重构

> 最后更新：2026-08-04（S7 整理）

### 🔴 待修改（影响架构）

| # | 事项 | 当前 | 目标 |
|---|------|------|------|
| 1 | **EmberSceneManager 走 Resource** | 直接调 `SceneManager.LoadSceneAsync` | 通过 `EmberResourceManager.LoadSceneAsync` |
| 2 | **ServiceLocator 定位梳理** | Resource 注册又移除，UI/Scene 强依赖 Instance | 框架内部用 Instance，外部后端用 ServiceLocator |
| 3 | **GameStateChanged 重复 dispatch** | `Start` 和 `TransitionTo` 各 dispatch 一次 | 合并为一次，或明确语义区分 |

### 🟡 待补完（功能完整度）

| # | 事项 | 说明 |
|---|------|------|
| 4 | **Module 系统** | `IEmberModule` + `EmberModuleCollector`，按 Phase 分组，对接状态机生命周期。接口已定义（[IEmberModule.cs](../../Assets/Ember/Core/Runtime/Manager/IEmberModule.cs)），Collector 待实现 |
| 5 | **UI 绑定代码生成** | `EmberUIBinding` + `EmberUIBindingGenerator` 被注释，需恢复适配 |
| 6 | **ResourcesProvider 异步化** | `LoadAssetAsync` 实际同步，应加真正异步 |
| 7 | **Timer 定时器** | 放入 `com.ember.extensions`，int-ID API（Delay/Interval/Schedule/Cancel），内部委托 UniTask |

### 🟢 待扩展（增强项）

| # | 事项 |
|---|------|
| 8 | Audio 多 Category + AudioAgent 池 |
| 9 | GameObject 预制体对象池 |
| 10 | 本地化 |
| 11 | Canvas 层自动挂载 CanvasScaler + Raycaster（→ S9） |
| 12 | UI Pop 动画 |
| 13 | **basic 包工具类迁移** | 旧项目工具类 + 编辑器脚本 → `com.ember.basic` Runtime/Editor |

### 📋 后续想法（待评估）

| # | 事项 |
|---|------|
| — | **DataSaver 异步版** | `DataSaver` 同步版已放入 basic（✅），异步版（`SaveAsync` / `TryLoadAsync`）需 UniTask，放入 `com.ember.extensions` |
| — | **状态机流转图可视化** | 读取 `GetTransitions()` / `GetPushTargets()` 构建节点图，条件边以不同颜色显示，在 EditorWindow 中可拖拽查看 |
| — | **必要状态视觉区分** | `IsRequired = true` 的节点以不同样式渲染（锁图标、加粗边框） |
| — | **场景选择器集成** | `EmberSceneField` 已创建（✅），可视化编辑器中使用拖拽式场景选择器替代手写字符串 |
| — | **LoadingPage 预制体化** | 当前 LoadingPage 为 FrameworkScene 中常驻 GameObject，未来改为预制体 + `EmberUIManager.Push/Pop` 动态加载（需要 EmberResourceManager 有 Provider 支持） |
| — | **Init 启动动画** | `EmberInitAnimationStarter` 基类已创建（✅），子类 override `PlayStartupAnimation` 即可。`InitSceneReady`/`InitAnimationDone` 事件已就绪 |
| — | **新建状态时自动关联场景** | `EmberSceneMappingCreator` 已自动创建 SO + 匹配同名场景（✅）。未来可视化编辑器创建新状态时，需<b>先创建场景 → 后创建状态</b>，这样 SO 的 `SyncNewStates()` 能自动关联 |
| — | **Settings UI 集成** | `SettingsState` 状态已创建（✅），待实现 UI 层：根据 `SettingsContext` 展示不同选项面板 |
| — | Wwise 适配 |
| — | 图片/纹理管理 |

### ✅ 已解决（S1-S6 验证期间修复）

| # | 事项 | 修复日期 |
|---|------|----------|
| 1 | 现有 Manager 实现 IEmberManager（5 个 Manager + EmberUpdateManager） | 2026-07-31 |
| 2 | EmberUpdateManager 去 MonoBehaviour → 纯 C# 类 | 2026-07-31 |
| 3 | GameLauncher 集中入口（驱动 Update/Manager/StateMachine） | 2026-07-31 |
| 4 | EmberDebug GlobalOpen 不抑制 LogInit | 2026-08-03 |
| 5 | EmberDebug 运行时 SO 修改不实时生效 | 2026-08-03 |
| 6 | EmberDebug Disable/Enable 只改缓存不同步 SO | 2026-08-03 |
| 7 | C# 9 `init` 访问器需要 IsExternalInit polyfill | 2026-08-03 |
