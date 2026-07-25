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
| [EmberEventBus.cs](../../Assets/Ember/Core/Runtime/EmberEventBus.cs) | 全局事件总线，string-key，0～4 泛型参数，遍历安全 | burner `EventDispatcher` |
| [EmberServiceLocator.cs](../../Assets/Ember/Core/Runtime/EmberServiceLocator.cs) | 轻量级服务定位器，接口→实现映射，支持延迟工厂 | —（burner 无此模式） |
| [EmberSingleton.cs](../../Assets/Ember/Core/Runtime/EmberSingleton.cs) | 两种单例基类：`EmberSingleton<T>`（纯 C#）和 `EmberMonoSingleton<T>`（MonoBehaviour） | burner `SafeMonoSingleton`、`Singleton<T>` |
| [EmberObjectPool.cs](../../Assets/Ember/Core/Runtime/EmberObjectPool.cs) | 通用对象池，支持 IPoolable 回调、统计、容量限制 | burner `BattleCore/ObjectPool` |

### 与 burner 的设计差异

| 维度 | burner | ember | 理由 |
|------|--------|-------|------|
| 事件 Key | int | string | 可读性更好，避免 Key 冲突管理 |
| 遍历安全 | 索引指针调整 | 延迟操作队列 (pending ops) | 更清晰的语义，支持嵌套 dispatch |
| 服务定位 | 无（Singleton.Instance + 反射） | EmberServiceLocator | 解耦接口与实现，方便测试和替换 |
| 对象池 | 最小实现（仅 Stack） | 带容量/统计/IPoolable | 更完整的生产级实现 |

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
