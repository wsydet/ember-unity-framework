# PlayerControl 玩家操作模块

> 状态：实现、文档与模板落盘已完成  
> 本轮交付：`source3d-2p5d v0.2.3`，父基线为 `base v0.5.3`（2026-09-07）

`PlayerControlModule` 是 `source3d-2p5d` 模板按需装配的可选业务 Module。它读取
`EmberInputManager` 的 Player Action Map，把输入转换为 Gameplay 场景操作。

## 职责边界

| 类型 | 职责 |
|---|---|
| `EmberInputManager` | 初始化 InputActionAsset、切换 Action Map、提供 Action 和重绑定扩展入口 |
| `EmberCameraManager` | 持有输出相机、注册和切换 CinemachineCamera |
| `EmberCameraRegistration` | 随场景相机的 Awake/Destroy 完成注册和注销 |
| `PlayerControlModule` | 消费输入，移动 LookAt，执行世界坐标拖动和滚轮缩放 |
| `PlayerControlSceneBinding` | 向纯 C# 模块注入场景引用、配置资产和射线层 |
| `PlayerControlBounds` | 使用 BoxCollider 列表定义 LookAt 可移动区域的并集与区域解锁状态 |
| `PlayerControlSettings` | 保存可在播放状态实时调整的移动和缩放参数 |

玩家控制模块不注册相机，也不直接读取 `Mouse.current`、`Keyboard.current` 或
`Camera.main`。射线使用 `EmberCameraManager.MainCamera`，输入统一来自
`EmberInputManager`。

## 生命周期与输入门控

模块实现 `IEmberModule` 和 `IEmberUpdate`，并声明
`[EmberModule(ModulePhase.Gameplay)]`。实例会在 InitState 提前发现并构造，但只在 Gameplay Phase
调用 `OnInit`、接收更新和处理输入；离开阶段后对象仍保留，供下一次进入时热重启复用。

模块采用框架统一的两阶段生命周期：

```text
InitState → DiscoverModules（构造并登记模块，不调用 OnInit）
GameplayScene Awake → PlayerControlSceneBinding 获取已发现模块并 Bind 场景引用
GameplayState OnEnter → InitPhase(Gameplay) → PlayerControlModule.OnInit
```

`PlayerControlSceneBinding` 使用无创建查询，不会调用 `PlayerControlModule.Instance`。
如果模块没有在场景 Awake 前被框架发现，会报告启动流程错误，而不是由场景组件偷偷创建模块。
如果模块通过 `EmberModuleAttribute.Enabled = false` 明确关闭，Collector 不会创建或登记模块实例；
场景绑定组件会停止绑定流程且不误报启动错误。

进入 Gameplay 后模块不会立即接收输入：

```text
PlayerControlModule.OnInit
→ 绑定 Player Action Map
→ 等待 LoadingFadeOutComplete
→ 允许玩家操作
```

`LoadingFadeOutStart` 只表示进度条开始渐隐。之后仍有方块扫出动画，不能作为解除门控的时机。
`LoadingFadeOutComplete` 由 `EUIManager` 在整个页面退出过渡完成后广播，此时进度条和方块均已消失。
新的 Loading 渐入开始或模块销毁时，会立即停止输入并取消当前拖动捕获。

## Input Actions

Player Action Map 中与该模块相关的动作如下：

| Action | 类型 | 默认绑定 | 用途 |
|---|---|---|---|
| `Move` | Vector2 | WASD、方向键、手柄左摇杆 | 水平面移动 LookAt |
| `CameraDrag` | Button | 鼠标左键 | 开始和维持场景拖动 |
| `PointerPosition` | Vector2 PassThrough | Pointer Position | 提供绝对屏幕坐标 |
| `Zoom` | Axis | Mouse Scroll Y | 调整正交相机视野大小 |

`PointerPosition` 使用绝对坐标而不是 Pointer Delta，才能把鼠标射线与世界锚点建立一对一关系。
该模板的输入资产位于 `Assets/Game/InputSystem_Actions.inputactions`，属于模板保存覆盖范围。
这些动作仍是普通 Input System Binding，未来接入
`IEmberInputRebindingService` 后可以参与玩家按键重绑定。

## 世界坐标拖动

鼠标按下时，模块从主相机发射射线：

1. 射线命中场景碰撞体时，以命中点高度建立水平拖动平面。
2. 没有命中时，以 LookAt 当前高度建立兜底平面。
3. 保存按下位置在拖动平面上的世界锚点。
4. 持续按住时，将当前鼠标重新投影到同一平面，并用“锚点 - 当前投影点”移动 LookAt。

因此，在未触碰移动边界时，开始拖动时抓住的方块或地面点会持续位于鼠标下方。
拖动已经开始后，即使指针经过 UI，也会保持捕获直到松开，避免输入跳变；但从 UI 上按下不会启动场景拖动。

`LookAt` 是纯逻辑锚点，不需要碰撞体。场景保留其 Sphere 子物体作为调试可视标记，
但移除了 SphereCollider，避免射线误抓取随相机一起移动的锚点。

拖动期间暂停 WASD 和滚轮处理，避免多种位移来源破坏世界锚点关系。

## 移动边界

`GameplayScene/Level/CameraMovementBounds` 包含：

- 启用的 Trigger BoxCollider 区域列表；当前开发场景登记了两块区域用于多区域测试。
- `PlayerControlBounds`，持有允许区域列表，并把 LookAt 限制在所有已启用区域的 XZ 并集内。
- 每块区域对应的可见立方体围栏；围栏只表现边界，BoxCollider 才是实际约束来源。

WASD 和鼠标拖动最终都调用 `TryConstrainMovement(current, target)`。该接口从当前位置开始，
每一步只使用包含当前中间点的启用区域计算候选；候选进入接触或重叠区域后可以继续约束剩余移动，
但不会把目标点投影到另一块不相连的区域。到达围栏后边界优先，因此继续向围栏外拖动时，世界锚点
不能再保持在鼠标下方，这是预期行为；模块会同步丢弃边界拒绝的拖动残差，所以反向拖动会立即离开
边缘，不需要先走完一段“空行程”。

允许区域使用 `BoxCollider.enabled` 表示解锁状态。运行时可以通过
`EmberModuleCollector.TryGetInstance` 和 `TryGetModule` 获取已发现的模块，再通过 `MovementBounds`
访问当前场景边界；不要直接访问 `PlayerControlModule.Instance`，否则会绕过禁用状态和统一发现流程。

```csharp
if (EmberModuleCollector.TryGetInstance(out EmberModuleCollector collector)
    && collector.TryGetModule(out PlayerControlModule module)
    && module.MovementBounds != null)
{
    module.MovementBounds.SetRegionUnlocked(region, true);
}
```

边界公开 `AddRegion`、`RemoveRegion` 和 `SetRegionUnlocked`。当前位置或本次移动的中间候选进入相连
区域的接触边界或重叠范围后，同一次大步长也可以继续进入相邻区域；如果区域之间留有大于世界接缝
容差的真实空隙，LookAt 会停在原区域边缘，快速拖动和低帧率下的大步长也不能直接穿过未开放空间。

模块每帧重新读取区域的 `center`、`size`、Transform 和启用状态，因此播放状态修改边界后会在下一帧生效；
如果新范围排除了当前 LookAt，则使用独立的 `TryClampPosition` 管理性纠偏，将 LookAt 恢复到最近的已启用
区域并结束当前鼠标捕获。该恢复行为不会用于普通玩家移动。没有任何启用区域时，模块保持当前位置并拒绝继续移动。

## 滚轮缩放与运行时配置

缩放修改 `CinemachineCamera.Lens.OrthographicSize`，不移动相机位置。
滚轮向上减小正交尺寸并放大画面，向下增大正交尺寸并缩小画面。该实现按 Input System 默认的 `UniformAcrossAllPlatforms`
滚轮范围工作；不要把项目的 `scrollDeltaBehavior` 改成平台原始范围。

配置资产位于：

`Assets/GameResource/Resources/Scenes/GamePlay/PlayerControlSettings.asset`

| 参数 | 默认值 | 说明 |
|---|---:|---|
| WASD 移动速度 | 8 | 世界单位/秒 |
| 每格正交尺寸变化 | 1 | 每次滚轮刻度改变的 Orthographic Size |
| 最小正交尺寸 | 5 | 最大放大限制 |
| 最大正交尺寸 | 20 | 最大缩小限制 |

模块持有 SO 引用并在每帧直接读取属性，不在 Bind 阶段复制数值。因此播放模式修改资产后，
WASD 速度和滚轮缩放立即使用新值；修改缩放极值时，当前正交尺寸会在下一帧自动限制到新范围。

## 场景绑定检查

`GamePlayCamera` 上的 `PlayerControlSceneBinding` 必须配置：

- InputSystem_Actions
- PlayerControlSettings
- LookAt
- GamePlayCinemachineCamera 上的 CinemachineCamera
- CameraMovementBounds 上的 PlayerControlBounds
- 鼠标射线检测层

任一必要引用缺失时，模块会通过 `EmberDebug` 报错并拒绝启用玩家输入。
日志会分别指出缺失的 LookAt、CinemachineCamera、PlayerControlBounds、PlayerControlSettings 或 InputActionAsset。

## 手动验收

1. 进入 Gameplay，在 Loading 进度条消失但方块仍扫出时按 WASD，LookAt 不应移动。
2. 方块完全退出后，WASD 可以移动；四个方向均不能越过围栏内沿。
3. 从 UI 上按住左键并移出 UI，不应启动场景拖动。
4. 从原点 Cube 上按住左键拖动，未碰边界时 Cube 应持续位于鼠标下方。
5. 拖动经过 UI 后继续移动，直到松开前不应跳变。
6. 滚轮向上放大、向下缩小，Orthographic Size 不能超过 SO 的最小值和最大值。
7. 播放状态修改 SO 的移动速度、正交尺寸步长和极值，下一次输入应立即使用新值。
8. 退出 Gameplay 后，Player Action Map 切回 UI，玩家控制不再更新。
9. 运行时调整允许区域的 Center、Size 或 Transform，LookAt 应在下一帧使用新范围。
10. 使用场景中第二个相连或重叠区域切换 Collider Enabled，只有已启用区域允许进入。
11. 将两个区域分开形成真实空隙，慢拖、快速拖动和 WASD 大步长都应停在原区域边缘，不能跳到另一侧。
12. 鼠标拖到区域边缘后立即反向拖动，LookAt 应立即响应，不能出现累计残差造成的空行程。

## 模板同步边界

`PlayerControlModule`、场景围栏、Input Actions 和配置资产属于 `source3d-2p5d` 业务模板。
它是可选业务 Module，`base` 不应包含它或它的场景绑定与配置。

`Packages/com.ember/Input` 中的 `EmberInputManager` 与重绑定契约属于框架公共包；其中
`EmberInputManager` 是框架必备 Manager，`base` 与其他模板都必须通过 Manager 反射管道发现并
初始化它。公共包保持单一来源，所以不复制到模板的 `Assets`，但这不代表 `base` 不需要该 Manager。
业务内容应通过模板开发页面保存进 `source3d-2p5d`，不要手工复制模板树。
输入资产放在 `Assets/Game/InputSystem_Actions.inputactions`，确保随该业务模板一同保存；切换基础模板后，
公共输入 Manager 会继续存在并初始化，同时应核对基础模板自身的输入配置，并确认没有残留
PlayerControl 业务引用。

Project-wide Default Input Actions 位于项目设置而非模板快照。当前项目启用 2.5D 模板时可指向上述
业务资产；切换到不包含该资产的 `base` 后，应在 Project Settings 中清空失效引用或改绑基础模板自己的
输入资产。FrameworkScene 的 `InputSystemUIInputModule` 独立使用 Input System 包内的
`DefaultInputActions`，不应改绑为 PlayerControl 的业务资产。
