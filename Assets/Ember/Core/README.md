# Core — 框架基础设施层

Core 是 ember-unity-framework 的最底层模块，提供事件总线、服务定位器、单例模式、对象池、
状态机、统一 Update 循环、调试日志等基础设施能力。所有上层模块均依赖 Core。

## 📂 子模块

| 子目录 | 说明 | 文档 |
|--------|------|------|
| `Event/` | 事件总线 + 广播事件常量表 | [Event/README.md](Event/README.md) |
| `Manager/` | 管理器接口 + 自动收集器 + 初始化优先级 | [Manager/README.md](Manager/README.md) |
| `Service/` | 服务定位器 + 单例基类 + 对象池 + SO 基类 | [Service/README.md](Service/README.md) |
| `State/` | 游戏状态机 + 核心状态 + 流转描述符 | [State/README.md](State/README.md) |
| `Update/` | 统一 Update/LateUpdate/FixedUpdate 驱动 | [Update/README.md](Update/README.md) |
| `Debug/` | 增强日志 + 标签过滤 + 彩色输出 + SO 配置 | [Debug/README.md](Debug/README.md) |
| `Editor/` | Build Settings 同步 + Debug 配置自动创建 + 场景快速打开 | [Editor/README.md](Editor/README.md) |

## 📄 根级文件

| 角色 | 路径 |
|------|------|
| 游戏启动器（框架入口） | `Runtime/GameLauncher.cs` |
| 场景文件引用（Odin 面板） | `Runtime/EmberSceneField.cs` |
| 程序集可见性声明 | `Runtime/EmberCoreAssemblyInfo.cs` |
| C# 9 init polyfill | `Runtime/Compatibility/IsExternalInit.cs` |

## 🔌 快速上手

```csharp
// 框架入口：在初始场景的 GameBoot GameObject 上挂载 GameLauncher
// GameLauncher 自动创建状态机，注册 Init/Main/Gameplay 状态，驱动 Update 循环

// 获取各子系统：
var fsm = GameLauncher.Instance.Fsm;
var debug = EmberDebug.Log(LogTags.EmberCore, "框架启动");
var eventSub = EmberEventBus.Subscribe(EmberBroadcastEvent.CoreReady, () => { });
```

### GameLauncher — 框架入口

挂载在 FrameworkScene 的 GameBoot GameObject 上，是框架的统一启动点。

| Inspector 字段 | 说明 |
|----------------|------|
| UI Root | UI 宿主节点（RectTransform） |
| Audio Host | 音频宿主节点 |
| Input Host | 输入宿主节点 |
| UI Camera | UI 相机 |
| Main Camera | 主相机 |

**启动流程**：`Awake` → 创建 StateMachine + 注册状态 → `Start` → InitState.OnEnter（ManagerCollector.InitializeAll）→ TransitionTo<MainState>

**每帧驱动**：`Update` → EmberUpdateManager.DoUpdate + Fsm.Current.OnUpdate

### EmberSceneField — 场景文件引用

在 Inspector 中拖拽 .unity 文件选择场景，避免手写字符串拼写错误。支持 `string` 隐式转换。

```csharp
[SerializeField] private EmberSceneField _mainScene;
// 可直接当 string 用
EmberSceneManager.Instance.LoadSceneAsync(_mainScene);
```

## 依赖关系

| 依赖 | 类型 | 说明 |
|------|------|------|
| `UnityEngine` | 引擎 | MonoBehaviour、GameObject、DontDestroyOnLoad、Debug 等 |
| `System` / `System.Collections.Generic` | 标准库 | Action、Delegate、Dictionary、Stack 等 |
| `System.Reflection` | 标准库 | 运行时类型扫描 |
| `Sirenix.OdinInspector` | 第三方 | Odin 面板属性（Editor 和 Runtime 均使用） |
| `UniTask` | 第三方 | 异步驱动（Scene 加载等） |

> Core 不依赖其他 Ember 框架模块。它是整个框架的最底层。
