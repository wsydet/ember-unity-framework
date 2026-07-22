# Burner 项目架构分析

> 来源：`c:\Users\wuyu\Project\burner\client\game`
> 日期：2026-07-20
> 用途：为 ember-unity-framework 框架开发提供架构参考

---

## 一、项目概览

| 属性 | 值 |
|---|---|
| 引擎 | Unity 2022.3 LTS |
| 渲染管线 | URP 14.0.13（Burner 定制版） |
| 语言 | C# |
| 程序集数量 | 44 个 `.asmdef`（Assets/ 下） |
| 自定义 Package | 13 个 `com.burner.*` |
| 热更新方案 | HybridCLR |
| 资源管理 | YooAsset |
| 第三方 | Spine、Odin Inspector、Wwise、NiceVibrations |

---

## 二、程序集依赖层次（6 层）

```
Layer 0 - 零依赖基础层（纯 C#，不依赖引擎）
  Burner.Basic.Runtime*     ← 整个生态的基石，noEngineRefs: true
  BattleCore                ← 零依赖的战斗核心
  GiantSDK

Layer 1 - 最小依赖层（仅依赖 UGUI）
  spine-csharp, Compression, PrefabManifest, SceneLodInfo

Layer 2 - 轻量依赖层
  spine-unity, Burner.Extensions*, BattleLogic（noEngineRefs）
  Burner.UIExtension*       ← Burner UI 框架（依赖 Basic + Extensions + TMP）

Layer 3 - 中等依赖层
  Game.MapArea.Runtime, GameLogic.InputAutomation

Layer 4 - 核心集成层
  GameCore.Runtime          ← 19 个引用，集成 Unity/Burner 包（8 个程序集引用它）
  SDKAdapter                ← SDK 适配层

Layer 5 - 顶层胶水层
  GameLogic                 ← 41 个引用，整合一切（10 个 Editor 程序集引用它）

Layer 6 - 编辑器层（叶子节点，只被消费，不消费他人）
  GameLogic.Editor, CityBuilding.Editor, SkillEditor, 等 20+ 个
```

> \* 标记的来自 `Packages/`（`com.burner.*`），不在 Assets/ 下

**关键发现**：
- 没有叫 "Core" 或 "Framework" 的单一程序集。基础能力拆分为：`Burner.Basic.Runtime` → `Burner.Extensions` → `Burner.UIExtension` 三层 Package
- 所有 Editor 程序集都是叶子节点（Layer 6），只引用别人，不被别人引用
- `GameLogic` 是最终胶水层，`autoReferenced: false` 明确控制引用

---

## 三、框架核心系统

### 3.1 单例与生命周期

```
Singleton<T>                    ← Burner.Basic DLL（外部预编译库）
  └── SafeMonoSingleton<T>      ← 源码中的 MonoBehaviour 单例（双重检查锁，自动创建 GameObject）

IManager                        ← 接口：Init(), Destroy()
  └── GameMgrCollector          ← 反射扫描所有 IManager，按 [InitOrder] 排序后自动 Init()

IGameUpdate / IGameLateUpdate / IGameFixedUpdate   ← 帧循环接口
  └── GameUpdateManager         ← 反射收集所有实现，统一派发 Update/LateUpdate/FixedUpdate

IModule                         ← 接口：Init(), ResetModuleData(), Destroy()
  └── ModuleBase<T>             ← 模块基类（自带 Singleton 实例 + 网络消息注册辅助）
      └── [ModuleAttribute(ModuleOrder)]  ← 标记模块加载阶段
          └── ModuleManager     ← 反射扫描所有 IModule，按阶段 Init()
```

**设计精髓**：反射自动发现。新增系统只需：
1. 实现 `IManager` 接口
2. 加上 `[InitOrder]` Attribute
3. 框架自动发现并调用 `Init()`

不需要修改任何入口代码。

---

### 3.2 事件系统（两套并存）

| 特性 | EventDispatcher | UniEvent |
|---|---|---|
| 键类型 | `int` 常量 | `typeof(T).GetHashCode()` |
| 消息载体 | 直接传参（最多 4 个泛型参数） | `IEventMessage` 接口 |
| 派发方式 | 立即同步 | 支持 `SendMessage`（立即）和 `PostMessage`（延迟到下一帧） |
| 位置 | `GameLogic/Common/Event/` | `GameCore/Runtime/Common/Res/UniEvent/` |
| 用途 | UI 事件、模块间通信 | 资源更新通知（PatchEvents） |
| 方法 | `Register/Unregister/Publish`（静态） | `AddListener/RemoveListener/SendMessage/PostMessage` |

**补充**：
- `GameUIEventDefine.cs`：定义 UI 相关的 int 事件 ID（OnOpenPage、OnPageShown 等）
- `PatchEventDefine.cs`：定义资源更新相关的事件消息类（DownloadProgress、PatchDone 等）

---

### 3.3 对象池系统（三套并存）

#### A. 基础对象池（`Pool/` 目录）
```
IPoolable 接口（OnInstance, OnRecycle）
  └── Pool<T> where T : IPoolable, new()     ← Get/Recycle（静态）
  └── ObjectPool<T> where T : new()           ← Get/Release（实例）
  └── ListPool<T> / HashSetPool<T>            ← 集合池（静态）
  └── GameObjectPool / GameObjectPoolMgr       ← GameObject 专用池
```

#### B. 进阶对象池（`ObjectPool/` 目录）
```
IPooledObject 接口（OnPoolGet, OnPoolRelease, OnPoolDispose）
  └── InheritedObjectPool<T> : IPooledObject, new()   ← 支持 EnsureSize、AdjustPoolSize
  └── Generic_ObjectPool<T> : new()                    ← 静态工厂 + SafeCapacity 保护
  └── Generic_MemoryPool<T> : class                    ← 链表实现
  └── Generic_ListPool<T>                              ← 集合池（静态）
  └── GenericObjectPools                                ← 全局工厂类
```

#### C. 战斗专用对象池
```
BattleCore/ObjectPool.cs               ← 战斗核心专用
ObjectPoolCanPrepare<T>                 ← 支持预准备
```

---

### 3.4 资源管理（基于 YooAsset）

```
ResManager : Singleton<ResManager>
  ├── YooAsset 启动与初始化
  ├── 资源加载：LoadAssetAsync<T>, LoadSceneAsync, LoadBytesSync/Async
  ├── 资源存在检查：IsAssetExist
  ├── 下载管理：HaveAnyResourceToDownload, DownloadAssetsByTag
  └── 卸载：UnloadUnusedAssets

GameResourceProxy : IResourceProxy       ← Burner.Basic 资源接口的实现
  └── GameResourceHandle : IResourceHandle  ← YooAsset Handle 包装
  └── AssetHandleSlot<T>                   ← 线程安全的异步加载槽（支持取消和重入）
  └── ResFileHandle                        ← 原始文件/bytes 加载包装
```

---

### 3.5 UI 框架（分层架构）

```
BurnerUIManager (DLL)                  ← Burner.UIExtension 提供底层 UI 栈
  └── GameUIManager : Singleton<GameUIManager>, IManager, IGameUpdate
      ├── 页面管理：ShowPage, ClosePage, HidePage, CloseAllPopups, CloseAllPages
      ├── 页面队列：EnqueuePage, ShowQueue, ClearQueue
      ├── 页面查询：GetPage<T>, IsPageShow, IsUIOpeningAtTop, IsAnyWindowOpen
      ├── 生命周期 Hook：OnPreparePageOpen, OnFinalizePageOpen, OnPreparePageClose...
      ├── 背景遮罩：CreateBgMask, RemoveBgMask
      └── 摄像机管理：SetSceneVolumeWeight

GameUIBase : GameUILogic                ← 项目级 UI 基类（继承 DLL 中的 GameUILogic）
  ├── 生命周期：OnOpen, OnBind, OnShow, OnHide, OnClose, OnDispose
  ├── 自动事件清理：AddEvent / RemoveAllEvents（自动配 EventDispatcher）
  ├── 子页面：ShowSubPage, CloseSubPage
  └── 点击遮罩：CreateClickMask, ClearClickMask

PageDef                                 ← 所有 UI Prefab 名称 + PageFlags 注册表
GameUIConst                             ← 标准分辨率、层级顺序、UI 根节点路径
GameUIEventDefine                       ← UI 事件 ID 定义
```

---

### 3.6 场景管理

```
GameSceneManager : Singleton<GameSceneManager>, IManager
  ├── ChangeScene(SceneArgs)            ← 异步加载场景
  ├── IsCurSceneLoaded, GetCurSceneName, IsCurScene(SceneName)
  ├── IsInnerCity, IsWorld, IsBattleScene
  ├── LoadAssetInCurScene<T>            ← 场景级资源管理（场景退出时自动卸载）
  └── UnloadAssetInCurScene

GameSceneBase (抽象)                     ← 场景基类
  ├── LoadInternal（通过 ResManager.LoadSceneAsync）
  ├── OnEnter, OnExit, OnLoadNextBegin
  └── 具体实现：WorldScene, BuildingScene, RolePlayScene, SlgBattleScene, FormationScene

SceneName 枚举                           ← Login=1, Building=2, RolePlay=3, World=4, SlgBattle=5, Formation=6
SceneRootBase : MonoBehaviour            ← 场景根节点（CameraRoot, LightRoot, RenderRoot）

GameStateManager : Singleton<GameStateManager>   ← 场景级状态机
  ├── ChangeState(StateName), IsInState, IsInBattleState
  └── States: Init, Login, Main(Building), RolePlay, CityBattle, World
```

---

### 3.7 其他系统速览

| 系统 | 核心类 | 关键方法 |
|---|---|---|
| **网络** | `NetworkManager`（TCP + AES-GCM 加密） | 后台收包/解析线程，Gate 加密握手 |
| **网络消息** | `PacketManager`（Protobuf 消息分发） | 按消息类型 ID 路由到 Handler |
| **日志** | `GameLogManager` | 捕获 Unity Log → 并发队列 → 文件环形缓冲 |
| **定时器** | `TimerManage`（静态） | AddTimer(delay, interval, playCnt, useLogicTime)，支持泛型参数 |
| **游戏时间** | `GameTime`（静态） | DeltaTime, CurTime, CurLogicTime, SetTimeScale |
| **音频** | `AudioMgr : Singleton, IManager, IGameUpdate` | PlayMusic/Sound/Voice, SetRTPC, Volume 控制 |
| **性能** | `PerformanceTierManager` | SetPerformanceLevel, CheckResolution |
| **启动器** | `GameLauncher : BaseBoot` | 初始化 Log → ResManager → HybridCLR 热更 DLL → GameLogicEntryWrap |

---

## 四、目录组织

```
Assets/
├── Game/                              # 核心游戏代码
│   ├── BattleCore/                    #   战斗核心（零依赖）
│   ├── BattleLogic/                   #   战斗逻辑（noEngineRefs）
│   ├── GameCore/Runtime/              #   游戏核心运行时
│   │   └── Common/
│   │       ├── Res/                   #     资源管理 + UniEvent
│   │       ├── GameLog/               #     日志系统
│   │       ├── Network/Server/        #     服务器选择
│   │       └── Time/                  #     游戏时间
│   ├── GameLogic/                     #   游戏逻辑（顶层胶水层）
│   │   ├── Common/                    #     公共：单例、事件、对象池、定时器
│   │   ├── Configs/                   #     配置表
│   │   ├── GameEntry/                 #     入口：GameMgrCollector
│   │   ├── GameManagers/              #     各类 Manager
│   │   │   ├── UIFramework/           #       UI 框架
│   │   │   ├── GameScene/             #       场景管理
│   │   │   ├── GameState/             #       游戏状态机
│   │   │   ├── Net/                   #       网络
│   │   │   ├── Audio/                 #       音频
│   │   │   └── Performance/           #       性能
│   │   ├── GameModule/                #     游戏模块（ModuleBase + ModuleManager）
│   │   └── Protos/                    #     Protobuf 定义
│   └── SDKAdapter/                    #   SDK 适配层
├── CommonTools/                       # 共享工具
│   ├── 3rdParty/                      #   第三方（Spine, Odin, NiceVibrations...）
│   └── InHouseTools/                  #   内部工具（动画扩展、引导编辑、地图编辑...）
├── Editor/                            # 编辑器专用脚本
├── GameResource/                      # 资源管理（DLL、Editor、Runtime 资源）
├── InstanceRenderer/                  # GPU 实例化渲染
├── Plugins/                           # 原生插件（Android, iOS, Wwise, GiantSDK）
└── StreamingAssets/                   # 流式资产
```

---

## 五、对 ember-unity-framework 的启示

### 5.1 可以直接借鉴的设计

| 设计 | 说明 |
|---|---|
| **反射自动发现** | GameMgrCollector + GameUpdateManager + ModuleManager 的反射机制，新增系统零侵入 |
| **[InitOrder] 排序** | 管理器按优先级自动排序初始化，解决依赖顺序问题 |
| **双事件系统** | int-keyed（高性能全局事件）+ type-keyed（类型安全的领域事件）各有适用场景 |
| **Editor/Runtime 分离** | 通过 .asmdef 明确隔离编辑器代码和运行时代码 |
| **场景封装** | 每个场景继承 GameSceneBase，封装加载/卸载/资源生命周期 |
| **模块阶段加载** | ModuleOrder flags 枚举（GameStart / BeforeLoginSuccess / AfterLoginSuccess），按游戏阶段激活模块 |

### 5.2 需要简化的部分

| Burner 现状 | ember 简化方向 |
|---|---|
| 对象池有三套 | 保留一套最完善的（InheritedObjectPool + Generic_ListPool） |
| Singleton 在 DLL 中 | 放在框架源码内，减少外部依赖 |
| UI 框架依赖 Burner.UIExtension DLL | 独立实现 UI 栈，不依赖闭源 DLL |
| YooAsset 作为资源方案 | 改用 Unity 原生 Addressables 或保持可选 |
| HybridCLR 热更新 | 作为可选插件，非框架核心 |
| 三层 Package（Basic → Extensions → UIExtension） | 合并为扁平化 Package 结构 |

### 5.3 建议的 ember 实现顺序

1. **核心基础**：Singleton、SafeMonoSingleton、IManager、InitOrder
2. **事件系统**：EventDispatcher（int-keyed）+ UniEvent（type-keyed）
3. **更新循环**：GameUpdateManager + IGameUpdate/IGameLateUpdate/IGameFixedUpdate
4. **对象池**：InheritedObjectPool + GameObjectPool
5. **管理器收集**：GameMgrCollector（反射自动发现）
6. **资源管理**：ResManager（基于 Addressables 或 YooAsset）
7. **UI 框架**：UIManager + UIBase + 页面栈
8. **场景管理**：SceneManager + SceneBase + 场景状态机
9. **模块系统**：ModuleManager + ModuleBase + ModuleOrder
