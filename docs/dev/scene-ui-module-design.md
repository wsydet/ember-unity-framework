# Ember SceneUI 模块设计

> 状态：破坏性重构后的正式架构  
> 更新日期：2026-09-07  
> 定位：框架提供引擎与模块基类，项目提供具体业务模块和 UI 资源  
> 框架验证：Unity 全量测试 215/215 通过

## 1. 最终结论

SceneUI 不再是框架启动阶段常驻的全局 Manager，也不再自行创建 Canvas。

最终分层如下：

```text
框架启动常驻（IEmberManager）
    EUIViewEngine
        └── 页面视图、Canvas、过渡、池和帧驱动
    EUIManager
        └── 页面路由、显示队列、页面关系和跨场景协调

玩法阶段可选（IEmberModule）
    项目 SceneUIModule
        : EmberSceneUIModuleBase<SceneUIModule>
        ├── 持有一个 EmberSceneUIEngine
        ├── 管理一个或多个 Channel
        ├── 打开/关闭业务 EUI 宿主页
        ├── 以 EUI Item 作为气泡 View
        └── 向玩法提供气泡 API

普通实例（非 IEmberManager）
    EmberSceneUIEngine
        └── Context、投影、可见性、内部排序、池化和 View 生命周期
```

`EmberSceneUIEngine` 没有 `Instance`，只在具体 `SceneUIModule` 进入其 Phase 后创建，在模块退出时释放。
这正是框架组合模型的一个实例：所有游戏共享 UI 等必要 Managers，需要 SceneUI 的模板再选装
`SceneUIModule`；不需要 SceneUI 的游戏无需承担对应业务实例和生命周期。

## 2. 为什么 SceneUI 是 Engine 而不是 Manager

`IEmberManager` 表示随框架启动而初始化并常驻的基础设施。`EUIViewEngine` 与 `EUIManager` 都需要在 Init 阶段建立真实的全局 UI 环境，因此它们保留 `IEmberManager` 身份是合理的。

SceneUI 不同：

- 没有 Context 时没有可运行状态。
- 刷新由每个 Context 的 CameraUpdateSource 驱动。
- Context、页面和业务目标都属于具体玩法生命周期。
- 不使用 SceneUI 的项目不应在框架启动时产生实例或副作用。

因此原 `EmberSceneUIManager` 收束为普通的 `EmberSceneUIEngine`。类名中的 Engine 表示其计算与状态职责；是否实现 `IEmberManager` 则取决于生命周期，两者不能仅凭命名判断。

## 3. 模块是否启用的真实规则

`EmberModuleCollector` 扫描所有已加载的非系统程序集。一个类型只有同时满足以下条件才会被登记：

1. 实现 `IEmberModule`。
2. 不是抽象类或接口。
3. 声明 `EmberModuleAttribute`。
4. `EmberModuleAttribute.Enabled` 为 `true`。
5. 能通过公开静态 `Instance` 取得模块单例。

登记后，状态机进入特性声明的 `Phase` 是模块被激活的附加条件：Collector 此时才调用 `OnInit`；
它不是模块被发现和登记的条件。

因此框架基类必须保持抽象。项目没有具体 `SceneUIModule` 时，SceneUI 不会被收集；项目提供具体类后，它才成为功能接入声明。

Collector 会先读取类型特性，再决定是否访问 `Instance`。因此禁用模块不会被构造、登记或调用
`OnInit`。Engine 仍应在 `OnInit` 中创建，不能在构造函数或字段初始化器中创建。

通用模块生成工具只提供默认关闭的 `SceneUIModule` 接入骨架，确保业务资源尚未配置时没有运行副作用。
已经完成资源配置的项目可以显式启用。当前 `source3d-2p5d v0.2.3` 就是完整示例：它声明
`[EmberModule(ModulePhase.Gameplay)]`，并随模板交付宿主页、Catalog、气泡 Item 和场景接入对象。

## 4. 框架与业务边界

### 4.1 EmberSceneUIEngine

引擎保留：

- `SceneUIContextHandle`、`SceneUIHandle` 及版本失效机制。
- Anchor、世界坐标投影和 Canvas 坐标换算。
- 业务显隐、空间显隐和 Context 显隐。
- 屏幕边界、Clamp、缩放与气泡内部排序。
- 遮挡检测与硬预算。
- View 获取、预热、池化、延迟回收与诊断。
- Prefab/Resource ViewHost。
- Cinemachine 与手动相机更新源契约。

引擎不再：

- 创建 Canvas 或页面 GameObject。
- 决定宿主页 SortingOrder。
- 配置 CanvasScaler、GraphicRaycaster 或 UI Layer。
- 加载、关闭或销毁业务 EUI 页面。
- 了解 GamePages、Gameplay State 或具体气泡类型。

### 4.2 EmberSceneUIModuleBase

模块基类位于 EUI Integration 程序集，负责：

- 在 `OnInit` 创建 Engine，在 `OnDestroy` Dispose。
- 加载和关闭业务声明的 EUI 宿主页。
- 将页面中的 `SceneUIPageHost/BubbleRoot` 绑定为 Engine Context。
- 维护 Channel 状态和模块生命周期代次。
- 提供注册、注销、空间标脏、内容标脏、显隐和相机刷新 API。
- 阻止旧生命周期或跨 Channel 的 Handle 命中当前 Engine。

### 4.3 项目 SceneUIModule

具体业务模块负责：

- 声明 `SceneUIChannelId` 与 `SceneUIViewId`。
- 指定宿主页 `EUIPageDef`、SceneCamera、Catalog 和策略。
- 将业务数据转换成 `SceneUIModuleRequest`。
- 通过业务语义 API 封装 Binder 和 Handle，例如“显示单位名称”“隐藏交互提示”。
- 可提供挂在世界物体上的业务组件，由组件声明 View 类型、静态/动态更新方式并等待 Channel Ready；该组件是 Anchor 的业务入口，不是气泡 View。
- 在资源配置完成后显式启用模块。

普通玩法对象不应持有 Engine 或 `SceneUIContextHandle`。

### 4.4 EUI Item 与 SceneUI View

气泡统一使用 EUI 的 `Item` 角色，不再维护 `SceneUIViewBehaviour` 这套业务脚本入口：

- Item 根节点保留 `RectTransform + CanvasGroup + EUIBinding`，不创建独立 Canvas、Scaler、Raycaster、页面 Animator 或 SafeArea。
- `EUIItemFactory` 根据 Binding 创建生成的 `EUILogic`，并填充同一套 `ControlMap`。
- `EUIItemSceneUIView` 是运行时纯 C# 适配器，把 Item 的根矩形、显隐和回池生命周期接到 `ISceneUIView`。
- 业务 Binder 可通过 `EUIItemSceneUIView.GetLogic<TLogic>()` 取得强类型 Item 逻辑并写入内容。
- `EUILogic.Page` 在 Item 中为 null，`EUILogic.Item` 指向所属 Item；Item 不能写入 `GamePages`，也不能通过 `EUIManager` 直接打开。

Item 临时离开屏幕时只执行 `OnHide`，仍保留本次借用状态；真正归还 SceneUI 对象池时再执行 `OnClose + OnResetDefault + OnReset`。`OnInit` 每个池实例只执行一次，下一次借出重放 `OnOpen`。SceneUI 在 Binder 之前调用 `BeginUse`，因此初始化重置不会覆盖 Binder 刚写入的内容；最终顺序是“Item 初始化/重置 → Binder → OnShow”。

## 5. 宿主页与层级所有权

业务使用 EUI 创建 `Overlay` 或 `FreePage` 页面，并在预制体中放置 `SceneUIPageHost`：

```text
SceneUIHostPage (EUI Page / Canvas)
└── Animator / SafeArea / 业务结构
    ├── BackgroundOrOtherContent
    ├── BubbleRoot                  <- SceneUIPageHost 指向这里
    │   ├── Bubble Instance
    │   └── Bubble Instance
    └── ForegroundOrOtherContent
```

层级分成两个维度：

- 宿主页相对其他 EUI 页面的层级由 PageDef 的固定 SortingOrder 决定。
- 气泡相对同一页面其他节点的层级由 `BubbleRoot` 的 Prefab 兄弟顺序决定。

框架不会修改这两个业务决策。`SceneUIPageHost` 会校验 BubbleRoot 位于宿主页下，且其子树没有 `overrideSorting Canvas`；`PrefabSceneUIViewHost` 只接受 Item 角色的 EUIBinding，并拒绝带 `overrideSorting Canvas` 的气泡预制体。

UI 开发中心为 `Overlay` / `FreePage` 提供固定排序值；EUIBinding 面板仅在 `FreePage` 下显示“FreePage 渲染层级”：

- Overlay 新建默认值为 20000。
- FreePage 新建默认值为 30000。
- 两者都可填写任意业务需要的 int 值。
- 代码生成分别写入 `overlaySortingOrder` / `freePageSortingOrder`。

不存在强制的 SceneUI 默认页面类型。需要任意中间层时推荐 Overlay；沿用项目独立页规范时可以使用 FreePage，但必须显式配置排序值。

## 6. Channel 模型

Channel 是运行环境通道，不是气泡类型，也不是 EventBus：

- 一个宿主页和 BubbleRoot。
- 一套 SceneCamera/UICamera 投影环境。
- 一套可见区域、遮挡策略与刷新来源。
- 一个整体显隐和销毁边界。

血条、名称、任务标记等外观差异由 `SceneUIViewKey` 区分。只有不同 UI 层级、不同相机、不同可见区域或不同生命周期才拆分 Channel。

业务使用稳定整数枚举构建 `SceneUIChannelKey` 和 `SceneUIViewKey`，避免字符串热路径。

Channel 状态：

- `Unregistered`：未声明或已注销。
- `Loading`：宿主页异步加载中。
- `Ready`：Context 可注册气泡。
- `Unavailable`：页面、Host、Catalog 或 Context 校验失败。

第一阶段不保存 Loading 期间的业务注册请求。`TryRegisterSceneUI` 只在 Ready 时成功；业务可监听 `ChannelReady` 后重试，避免基础层隐式延长业务对象生命周期。

## 7. 模块级 Handle

`SceneUIModuleHandle` 包含：

- Module 生命周期代次。
- ChannelKey。
- Engine 的 `SceneUIHandle`。

所有模块 API 先验证三层作用域，再转发给 Engine。这样可以保证：

- Gameplay 热重启后的旧 Handle 失效。
- 不同 Channel 之间不能误用 Handle。
- Engine Slot 即使复用相同 Index，也不会被旧版本命中。

## 8. 相机与位置更新

- 场景物体组件的静态模式使用 `Manual`：不做常规位置轮询，但相机快照变化时仍会随整个 Context 重新投影；业务确实移动静态物体后调用 `RefreshPosition()` / `NotifyPositionChanged()`，或在播放模式通过 Odin 面板点击“手动刷新气泡位置”。
- 场景物体组件的动态模式使用 `PollPosition`：每次 Channel 刷新时比较目标位置，位置变化后才重新投影。
- `TransformSceneUIAnchor + PollPosition`：Engine 每次 Channel 刷新时读取位置，业务无需发送移动事件。
- `WorldPositionSceneUIAnchor + Manual`：业务更新坐标后调用 `MarkSceneUIPositionDirty`。
- Cinemachine：`CinemachineSceneUICameraUpdateSource` 只响应目标物理输出 Camera 的完成事件，并通知所属 Channel 统一刷新；静态与动态对象都会响应真实的相机快照变化。
- 手动 Camera：相机 owner 在本帧最终姿态确定后调用 `NotifyCameraUpdated(channelKey)`。

不使用全局 EventBus 广播“相机移动”或“场景物体移动”，否则会造成所有 Channel 无差别刷新和生命周期归属不清。

模板中的 `Dynamic SceneUI Cube + SceneUIAutoMover` 是动态目标验收样例：脚本让 Cube 沿本地轴往复移动，
`SceneUIObjectUpdateMode.Dynamic` 负责驱动气泡跟随。它属于业务模板测试内容，不进入 SceneUI Package。

## 9. 生命周期顺序

初始化：

```text
Collector 发现 Enabled 的具体 SceneUIModule
→ 进入 Module.Phase
→ ModuleBase 创建 EmberSceneUIEngine
→ 具体模块声明 Channel
→ EUIManager 加载宿主页
→ 查找 SceneUIPageHost/BubbleRoot
→ 注册 View Catalog
→ Engine 注册 Context
→ ChannelReady
```

销毁：

```text
停止接受注册
→ 业务销毁钩子
→ 注销所有 Channel Context/Entry/View
→ 关闭宿主页
→ Dispose Engine
→ 递增模块代次
```

Context Dispose 不销毁宿主页；页面所有权始终在 EUI/ModuleBase。Engine 只负责它拥有的 ViewHost 和 Context 数据。

## 10. 模板交付边界

### 10.1 通用骨架生成

模块生成工具提供：

- `Assets/Game/Module/SceneUI/SceneUIModule.cs`
- `SceneUIChannelId`、`SceneUIViewId` 示例枚举。
- 默认关闭的具体模块骨架。

生成工具不会替业务自动创建：

- 业务宿主页 Prefab。
- `SceneUIPageHost/BubbleRoot` 的业务层级。
- 具体气泡 Prefab 与 Binder。
- View Catalog 资产和最终枚举值。
- 项目如何取得 SceneCamera/Catalog 的资源引用。

这些内容必须由具体游戏或完整示例模板按实际玩法创建。旧的自动 Canvas Context 和 Marker 实现不再保留。

气泡 Prefab 应从 UI 开发中心选择 `Item` 创建；无需也不允许为它新增 PageType。Item 不生成 `EUIPageDef`，由 SceneUIModule/ViewHost 实例化到宿主页的 BubbleRoot 下。

### 10.2 `source3d-2p5d` 完整示例

`source3d-2p5d v0.2.3` 已完成配置并显式启用 SceneUI，随业务模板交付：

- `SceneUIModule`、`SceneUIObject` 以及示例 Channel/View 枚举与 Binder。
- `EUISceneUIPanel/SceneUIPageHost.BubbleRoot` 宿主页。
- `SceneUIViewCatalog` 与 `EUIBaseBubbleItem` 气泡 Item。
- `GameplayScene` 中的静态、动态目标，以及 `Dynamic SceneUI Cube + SceneUIAutoMover` 验收样例。

这些资源只属于该业务模板，不进入 SceneUI Package，也不进入 `base`。框架层仍只交付通用引擎、
模块基类、Host、Catalog 类型和 EUI Item 接入能力。

## 11. 验收标准

- UIRoot 下不再出现 SceneUI 自动创建的 Canvas。
- 气泡实例全部位于业务页面的 BubbleRoot 下。
- 气泡 Prefab 为 EUI Item，根节点没有独立 Canvas，也没有 SceneUI 专用 Behaviour。
- 修改页面固定 SortingOrder 能改变整组气泡与其他 EUI 页面的前后关系。
- 修改 BubbleRoot 兄弟顺序能改变气泡在宿主页内部的位置。
- 模块未实现或 `EmberModuleAttribute.Enabled == false` 时 Engine 不运行，且不会创建模块实例。
- Gameplay 退出后 Context、Entry、View 和池被释放。
- 重进 Gameplay 后旧句柄与旧相机回调不能命中新 Engine。
- 不同 Channel 的宿主、相机、显隐和刷新互不串扰。
- 稳态刷新保持无 GC；池未命中只允许一次性实例化分配。
