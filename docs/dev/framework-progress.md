# Ember Framework 开发进度

> 最后更新：2026-08-03
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
<!-- >>> CURRENT PHASE — 从这里继续 <<< -->
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

> 最后更新：2026-08-03（S7 整理）

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
