# 公共机关、检查点与关卡接入

维护源为活动 Assets 的 Player、Entity 与 Game.State；不是框架 Manager。保存/版本/发布和测试作业的准确状态见开发仓库公共能力实施记录。本文所有命名均对应实际源码。

## 平移支撑（T02）

Kinematic Rigidbody2D 平台添加 EntityTranslationSupport2D，玩家从 Sensors.Ground 自动识别同一刚体。使用 `support.SetVelocity(Vector2)` 在玩家固定步前发令，`support.Teleport(Vector2)` 显式复位并使旧承载失效。普通静态地面无需组件。

Context.Support.Velocity 与 ExternalMotion.EnvironmentVelocity 分开统计，再进入唯一 Motor 提交。离开继承可配 None/Horizontal/Full；惯性保留到再次落地或会话清理，不重复叠加。瞬移、禁用、销毁、旋转变化不生成离开惯性。

上升平台通过 ConstrainForRider 按玩家主碰撞体头顶净空限速。宿主不得在玩家固定步之后再次覆盖平台速度；恢复运动由宿主下次发令决定。旋转承载、多驱动器争抢、复杂多形状乘员不属于本轮保证范围。风施加于平台还是玩家由项目分别提交，不含任何产品风速公式。

## 弹射（T03）

可选竖直保持/水平控制与接受时墙跳资格刷新见[弹射策略](launch-policy.md)；该扩展已通过 Player 回归，随 0.9.0 交付，以下无选项调用仍保持原默认行为。

公共协议为 Game.Entity.ILaunchTarget2D.TryLaunch(Behaviour, LaunchRequest2D)。玩家添加 PlayerLaunchTarget2D，平移载具添加 TranslationSupportLaunchTarget2D。LaunchZone2D 使用触发器按目标去重，冷却按游戏秒数，支持多碰撞体目标。

```csharp
var result = target.TryLaunch(this, new Game.Entity.LaunchRequest2D(
    "launcher", new Vector2(3, 8), Game.Entity.LaunchMode2D.ConstantRise,
    .4f, Game.Entity.LaunchEnd2D.ContinueVelocity,
    Game.Entity.LaunchInterrupt2D.ReplaceExternalMotion));
```

起点保持当前位置，不隐式传送。InitialVelocity 只提交一个固定步，之后由当前玩家方案控制；需要持续横向速度时使用 ConstantRise 的XY速度。ConstantRise 要求正Y速度和正秒数，到期/撞顶/owner失效结束，End选择继续速度或停止竖直速度。载具宿主在 IsLaunching 时暂停普通路径发令。

ReplaceExternalMotion 清理旧外部来源；RejectWhileBusy 在已有暂态运动时拒绝。内部冲刺、攻击、挂边、翻滚、下穿、受击等互斥动作返回 Busy，不无条件打断任意FSM。项目外部动作订阅 PlayerLaunchTarget2D.Accepted 清理自己的会话和表现；Windward 挥旗状态最终接线由项目验证。LaunchZone2D.Launched 供表现使用。没有蒲公英特殊倍率、旗或风场业务。

## 世界检查点（T04）

场景添加 WorldResetScope2D，按顺序登记 WorldResetParticipant2D。每个参与者提供唯一 StableId、CaptureState()、RestoreState(string, WorldResetReason2D)，配置 Retain/Initial/Checkpoint 策略与 Death/Load/NewRun/Leave 原因掩码。初始状态在 Awake/Initialize 冻结，未登记对象不自动复位。TranslationSupportReset2D 是平台位置/速度复位示例。

玩家可选添加 PlayerCheckpointRecovery，引用同场景世界Scope。PlayerSceneParticipant 启动时初始化入口基线；PlayerCheckpointZone2D 尝试采集稳定地面检查点。TryCapture失败时 LastError说明原因，不放宽快照合法性。PlayerCheckpointRecord 保存稳定ID、玩家基线及复制后的世界记录。

```csharp
bool restored = await recovery.RecoverAsync(playerModule, flow,
    Game.Entity.WorldResetReason2D.Death, cancellation);
```

顺序：整体暂停/关输入 → 清运动来源与支撑 → 按策略恢复世界 → 可选项目回调 → SceneScope原位玩家交接 → 既有SessionCommitted相机复位 → 区域立即重算 → 释放本次暂停。失败返回false并保留暂停/LastError，修复后可重试；部分参与者已执行时不声称自动回滚世界。失败后禁用恢复组件会停止玩家会话，避免释放暂停后在部分复位世界中继续运行；重新进入须通过正式会话入口。

组件实现 IPlayerDeathHandler，HandlerId为 `checkpoint-world`。生命配置显式选择Custom并填写该ID后，死亡走此事务；默认死亡策略保持原行为。PlayerRecoveryHazard2D 是直接请求检查点恢复的可选危险区示例，统计/表现由项目实现。IPlayerRecoveryPositionProvider仍只解析位置，不修改世界。

## 冻结关卡入口（T05）

PlayerLevelDefinition资产配置LevelId、ScenePath、PlayerConfig。配置Scene须与目标场景名称一致；创建请求时冻结完整方案，后改源资产不影响该请求。

```csharp
using var entry = levelDefinition.CreateEntry();
var gameplay = (Game.State.GameGameplayState)flow.GetState<Ember.Core.GameplayState>();
var result = await gameplay.EnterLevel(flow, entry, cancellation).Completion;
```

也可调用 PlayerEntryRequest.ForLevel(levelId, scenePath, config, checkpointSnapshot)。请求保持到转场完成再Dispose。GameGameplayState只在框架同步读取ScenePath的发起区间开放重入；繁忙时拒绝第二入口。同场景续玩使用PlayerModule.RestoreCheckpoint，不借跨关入口创建覆盖层。唯一玩家、Scope、配置/快照/场景一致性检查保留。

跨场景准备不覆盖旧玩家；候选在旧Gameplay模块退出后启动，失败由Scope事务释放。目标场景仍须纳入项目加载配置，并有一个启用的GameSceneScope、PlayerController和PlayerSceneParticipant。

## 单记录组合保存（T05）

CheckpointProgress<T>包含RunId、LevelId、PlayerCheckpointRecord和项目T。CheckpointProgressCodec<T>将玩家v3基线、世界记录与项目codec字节编码为同一条版本1外层记录，并保留项目DataVersion。不是两个文件分别保存。

```csharp
// projectCodec 由项目实现 IProgressCodec<T>；data 已就绪且配置相同DataTypeId。
var codec = new CheckpointProgressCodec<T>("mygame.checkpoint.v1", projectCodec);
var writes = new CheckpointSaveSession<T>(data, codec, runId);
var operation = writes.Save(runId, slot, levelId, legalCheckpoint,
    latestProjectData, new Game.PlayerData.SaveSummary("检查点续玩"), cancellation);
var saved = await operation.Completion;
```

legalCheckpoint来自合法采集或已验证基线。Save调用时同步编码最新项目数据，底层复制字节后入队；后改对象不改变已接受记录。空中、弹射或不能即时Capture时仍可组合旧基线与最新已提交成果。项目不应持久化半完成事务或临时动作。

SaveAndExitAsync先暂停并等待组合保存成功，再调用原ExitToMenuAsync(flow,null)排空队列后离场；失败保留现场并释放本次暂停。BeginRunAsync(newRunId)关闭本实例入口、排空旧队列后切换轮次；Save的expectedRun不符会拒绝。所有该轮次写入必须经同一实例，不能绕过它直接写同槽。清哪些业务字段由项目决定。

读取使用data.ReadAsync(slot,codec)。同场景可调用writes.RestoreAsync(savedProgress,recovery,playerModule,flow,restoreProject,cancellation)：项目回调在暂停的世界事务内执行，失败保留诊断。跨场景按LevelId选择配置，项目SceneScope参与者负责其业务数据准备/提交；位置解析器不承担业务恢复。

采用组合格式应使用项目自己的DataTypeId和UI入口。模板默认Save/Load按钮仍面向PlatformerSnapshot，不自动变成组合保存，也不自动导入旧格式、决定槽位数量或清历史记录。

## 区域与相机（T05/T06）

EntityRegion2D只描述RegionId、Collider2D几何和Priority。PlayerRegionTracker2D按玩家物理根位置选区，高优先级优先，同级按ID排序；无匹配为null。Changed提供进入/离开结果，Reevaluate()用于传送后立即重算。安全区、挑战区和RunTimer由项目消费这份归属；区域不自动成为检查点。

相机复用PlayerCameraParticipant、EmberCameraRegistration与Cinemachine 3 Follow。相机参与者在Scope中排于玩家之前，注册器关闭Activate On Awake；恢复重置跟随历史，卸载清理绑定。未新增相机Manager或自动房间边界。产品可订阅统一区域归属，通过现有相机注册/切换接线，同时遵守锁和覆盖层优先权。

## 消费验证

自动测试使用Unity真实隔离物理场景、正式玩家图、虚拟输入及实际存档服务。另有[两关正式链路验收](LevelAcceptance.md)，已通过真实启动器、场景加载、UI、组合保存退出/续玩以及相机加载/恢复/卸载的42条运行断言，包含缺少Scope/玩家参与者、保存失败的保护。消费项目仍需验证自身资源键、组合UI入口、房间相机与区域配置及发行构建；公共白盒不等同于Windward关卡验收。
