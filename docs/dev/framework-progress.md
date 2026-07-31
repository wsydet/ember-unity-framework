# Ember Framework 开发进度

> 最后更新：2026-07-25
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
EmberEventBus.Dispatch(EmberBroadcastEvent.ResourceReady);

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

- [ ] Canvas 层自动挂载 Canvas + CanvasScaler + GraphicRaycaster 组件
- [ ] Pop 动画支持（淡入淡出、滑动）
- [ ] 按返回键自动 Pop 最顶层（内置返回键监听）

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
| [IEmberManager.cs](../../Assets/Ember/Core/Runtime/IEmberManager.cs) | 管理器接口：Init() / Destroy() | burner `IManager` |
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

### 文件清单

| 文件 | 职责 | 参考 |
|------|------|------|
| [EmberStateMachine.cs](../../Assets/Ember/Core/Runtime/EmberStateMachine.cs) | 状态机引擎：Register / Start / TransitionTo / Push / Pop | burner `GameStateManager` |
| [InitState.cs](../../Assets/Ember/Core/Runtime/InitState.cs) | 框架内置必需状态（IsRequired = true），不可注销 | — |

### 设计要点

**适配单机 & 网游**：状态不是枚举而是类，用户自由添加——

```csharp
fsm.Register(new InitState());       // 框架内置，IsRequired = true
fsm.Register(new LoginState());      // 自定
fsm.Register(new MainMenuState());   // 自定
fsm.Register(new ConnectingState()); // 网游专用
fsm.Register(new ReconnectingState()); // 网游专用
fsm.Register(new BattleState());     // 自定
```

**图形化编辑器预留**：
- `RegisteredStates` 返回所有已注册状态（反射枚举）
- `Name` / `Description` 给编辑器展示
- `IsRequired = true` 的状态不可注销
- `Unregister<T>()` 拒绝删除必需状态和当前活跃状态

**TransitionTo vs Push/Pop**：

```csharp
fsm.TransitionTo<BattleState>();     // Exit MainMenu → Enter Battle（替换式）
fsm.Push<SettingsState>();           // Pause Battle → Overlay Settings（覆盖式）
fsm.Pop();                           // Exit Settings → Resume Battle
```

---

## 12. 日志系统 `Ember.Core.Runtime`

> 状态：✅ 已完成
> burner 参考：`Debuger`

### 文件清单

| 文件 | 职责 |
|------|------|
| [EmberDebug.cs](../../Assets/Ember/Core/Runtime/EmberDebug.cs) | 标签化日志 + 运行时开关 + Editor 双击跳转修正 |

### API 速览

```csharp
private static readonly string Tag = EmberDebug.Tag(nameof(MyClass));

EmberDebug.Log(Tag, "正常消息");
EmberDebug.LogWarning(Tag, "警告");
EmberDebug.LogError(Tag, "错误");  // 不受开关控制，始终输出

EmberDebug.SetOpen(false);  // 关闭所有非 Error 日志，线上包零 GC
```

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

---

## 技术债务 & 待重构

> 临时方案和已知问题，每次改完划掉。

### 🔴 待修改（影响架构）

| # | 事项 | 当前 | 目标 |
|---|------|------|------|
| 1 | **现有 Manager 实现 IEmberManager** | 6 个 EmberXxxManager 各自 Init | 统一 `IEmberManager` + `[EmberInitOrder]`，交给 `EmberManagerCollector` |
| 2 | **EmberUpdateManager 去 MonoBehaviour** | 自己继承 `EmberMonoSingleton` | 纯 C# 类，由 `GameLauncher` 驱动（burner 模式） |
| 3 | **EmberSceneManager 走 Resource** | 直接调 `SceneManager.LoadSceneAsync` | 通过 `EmberResourceManager.LoadSceneAsync` |
| 4 | **ServiceLocator 定位梳理** | Resource 注册又移除，UI/Scene 强依赖 Instance | 框架内部用 Instance，外部后端用 ServiceLocator |

### 🟡 待补完（功能完整度）

| # | 事项 | 说明 |
|---|------|------|
| 5 | **Module 系统** | `IEmberModule` + `EmberModuleCollector`，按阶段初始化，支持 `ResetModuleData()` |
| 6 | **GameLauncher 入口** | 集中驱动 Update、Manager 初始化、状态切换 |
| 7 | **UI 绑定代码生成** | `EmberUIBinding` + `EmberUIBindingGenerator` 被注释，需恢复适配 |
| 8 | **ResourcesProvider 异步化** | `LoadAssetAsync` 实际同步，应加真正异步 |

### 🟢 待扩展（增强项）

| # | 事项 |
|---|------|
| 9 | Audio 多 Category + AudioAgent 池 |
| 10 | GameObject 预制体对象池 |
| 11 | 本地化 |
| 12 | Canvas 层自动挂载 CanvasScaler + Raycaster |
| 13 | UI Pop 动画 |
