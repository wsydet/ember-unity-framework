# Core — 框架基础设施层

Core 是 ember-unity-framework 的生命周期与运行管线子系统，提供事件总线、服务定位器、单例模式、对象池、
状态机、统一 Update 循环等基础设施能力。日志和通用数据结构由 Basic 提供。

## Manager 与 Module

Ember 用两条平行管道组装游戏：

```text
具体游戏 = 框架基础 + 必备 Managers + 按需选装的 Modules
```

| | Manager | Module |
|---|---|---|
| 定位 | 框架运行必需的全局管理器 | 可选、可拼装的业务功能积木 |
| 生命周期 | Init 阶段统一启动，框架退出时销毁 | 按 `EmberModuleAttribute` 的 `Enabled` 与 `Phase` 激活/退出 |
| 模板策略 | `base` 和业务模板共同具备 | 每个模板按目标玩法自由组合 |

`InitState` 会先发现并构造所有启用 Module，但暂不启动；随后初始化全部 Manager；最后激活
Global Phase。其他 Module 只有在所属 Phase 调用 `OnInit` 后才进入活动状态。Module 可以消费
Manager，Manager 不依赖具体业务 Module。完整规则见 [Manager 文档](../../Core/Runtime/Manager/README.md)。
当前内置状态已经驱动 Global 与 Gameplay；Main 或自定义 Phase 需要在对应状态显式接线。

## 📂 子模块

| 子目录 | 说明 | 文档 |
|--------|------|------|
| `Event/` | 事件总线 + 广播事件常量表 | [Event/README.md](../../Core/Runtime/Event/README.md) |
| `Manager/` | 管理器接口 + 自动收集器 + 初始化优先级 | [Manager/README.md](../../Core/Runtime/Manager/README.md) |
| `Service/` | 服务定位器 + 单例基类 + 对象池 | [Service/README.md](../../Core/Runtime/Service/README.md) |
| `State/` | 游戏状态机 + 核心状态 + 流转描述符 | [State/README.md](../../Core/Runtime/State/README.md) |
| `Update/` | 统一 Update/LateUpdate/FixedUpdate 驱动 | [Update/README.md](../../Core/Runtime/Update/README.md) |
| `Basic/Runtime/Debug/` | 日志 + 标签过滤 + SO 配置 | [日志文档](../../../../docs/dev/ember-debug.md) |
| `Core/Editor/` | 场景映射、项目初始化与模板开发 | [Editor/README.md](../../Core/Editor/README.md) |

## 📄 根级文件

| 角色 | 路径 |
|------|------|
| 游戏启动器（框架入口） | [GameLauncher.cs](../../Core/Runtime/GameLauncher.cs) |
| 场景文件引用（Odin 面板） | [EmberSceneField.cs](../../Core/Runtime/EmberSceneField.cs) |
| 程序集可见性声明 | [EmberCoreAssemblyInfo.cs](../../Core/Runtime/EmberCoreAssemblyInfo.cs) |
| C# 9 init polyfill | [IsExternalInit.cs](../../Core/Runtime/Compatibility/IsExternalInit.cs) |

## 🔌 快速上手

```csharp
// 框架入口：在初始场景的 GameBoot GameObject 上挂载 GameLauncher
// GameLauncher 自动创建状态机，注册 Init/Main/Gameplay 状态，驱动 Update 循环

// 获取各子系统：
var fsm = GameLauncher.Instance.Fsm;
EmberDebug.Log(LogTags.EmberCore, "框架启动");
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

**启动流程**：`Awake` → 创建 StateMachine + 注册状态 → `Start` →
`InitState.OnEnter`（ModuleCollector.DiscoverModules → ManagerCollector.InitializeAll → Global Module OnInit）→
TransitionTo<MainState>

业务模块使用“发现/构造”和“阶段激活”两段生命周期。框架在加载业务场景前构造并登记所有启用模块；
场景组件只能通过 `EmberModuleCollector.TryGetModule` 获取已发现实例并注入引用，不能用 `.Instance`
隐式创建模块。场景激活完成、状态进入对应 Phase 后，Collector 才调用模块的 `OnInit`。

模块必须声明 `[EmberModule(phase)]`。Collector 在访问 `Instance` 前读取该特性；
`Enabled = false` 或缺少特性的模块会直接跳过，不会产生实例。UpdateManager 也只复用 Collector
已经创建的启用模块，不再独立反射创建业务模块；模块只有在 `OnInit` 成功后才接收帧更新。

`EmberSingleton.TryGetInstance` 只返回已存在实例，不会触发懒创建，适合验证框架启动顺序。

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

> `Ember.Core.Runtime.asmdef` 引用 `Ember.Basic.Runtime`、Odin 属性程序集和 UniTask。Basic 是其基础依赖，不应写成 Core 零外部依赖。
