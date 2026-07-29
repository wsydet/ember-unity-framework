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
| 2 | **Resource** | `Ember.Resource.Runtime` | ⬜ 待开始 | `ResManager` + `IResourceProxy` + YooAsset |
| 3 | **UI** | `Ember.UI.Runtime` | ⬜ 待开始 | `GameUIManager` + `Burner.UIExtension` |
| 4 | **Scene** | `Ember.Scene.Runtime` | ⬜ 待开始 | `GameSceneManager` |
| 5 | **Audio** | `Ember.Audio.Runtime` | ⬜ 待开始 | `AudioMgr` |
| 6 | **Input** | `Ember.Input.Runtime` | ⬜ 待开始 | Unity Input System 封装 |
| 7 | **Editor** | `Ember.Editor` | ⬜ 待开始 | 框架级编辑器工具 |

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
EmberEventBus.Dispatch("Module.Resource.Ready");

// Scene 模块监听 —— "Resource Ready 了我才开始加载"
EmberEventBus.Subscribe("Module.Resource.Ready", () => StartLoadScene());

// Audio 模块也监听 —— "Resource Ready 了我预热音频"
EmberEventBus.Subscribe("Module.Resource.Ready", () => PreloadBGM());
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
    /// 将 EmberEventBus 的 string-key 广播事件转为 UniRx IObservable，
    /// 让业务层可以享受操作符便利。
    /// </summary>
    public static IObservable<T> OnEvent<T>(string eventKey)
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

- [ ] 是否需要 Timer/TimerManager？（burner 有 `TimerManage`）
- [ ] 是否需要 Update 循环管理器？（burner 有 `GameUpdateManager` 反射扫描 `IGameUpdate`）
- [ ] 是否需要 Manager 自动发现机制？（burner 有 `GameMgrCollector` + `[InitOrder]`）

---

## 2. Resource 模块 `Ember.Resource.Runtime`

> 状态：⬜ 待开始
> burner 参考：`Assets/Game/GameCore/Runtime/Common/Res/`

### 规划

- IResourceProvider — 资源提供者接口（参考 burner `IResourceProxy`）
- EmberResourceManager — 资源管理器门面（参考 burner `ResManager`）
- 默认实现基于 Unity Addressables 或 Resources
- 支持可插拔的资源后端（Addressables / AssetBundle / YooAsset）

---

## 3. UI 模块 `Ember.UI.Runtime`

> 状态：⬜ 待开始
> burner 参考：`Assets/Game/GameLogic/GameManagers/UIFramework/` + `com.burner.uiextension`

### 规划

- IUIView — 界面生命周期接口（OnOpen / OnClose / OnPause / OnResume）
- EmberUIManager — 界面栈管理（Push / Pop / 层级管理）
- 支持 Canvas 层级系统：Background → Normal → Popup → TopMost → Loading

---

## 4. Scene 模块 `Ember.Scene.Runtime`

> 状态：⬜ 待开始
> burner 参考：`Assets/Game/GameLogic/GameManagers/GameScene/`

### 规划

- EmberSceneManager — 场景加载/卸载
- 过渡效果支持（Loading 界面、淡入淡出）
- 场景原型（Archetype）映射

---

## 5. Audio 模块 `Ember.Audio.Runtime`

> 状态：⬜ 待开始
> burner 参考：`Assets/Game/GameLogic/GameManagers/Audio/`

### 规划

- EmberAudioManager — 音频管理（BGM / SFX 分离）
- AudioGroup 音量分组控制
- 基于 Unity Audio Mixer

---

## 6. Input 模块 `Ember.Input.Runtime`

> 状态：⬜ 待开始
> burner 参考：Unity Input System 封装

### 规划

- 基于 Unity Input System 的抽象层
- 支持运行时切换输入 Action Map
- 输入事件桥接到 EmberEventBus

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
