# 运行契约

运行程序集：`Game.LaneDefense.Runtime`。依赖 base 的 `Game.SceneFlow.Runtime`、`Game.Input.Runtime` 与 Ember 基础设施；公共包不引用本模块。业务不持有 EUI 对象。

## 配置与坐标

`LaneDefenseLevel.Validate()` 返回全部已发现配置错误；`Freeze()` 校验并复制单位、敌人、波次及地形，返回 `LaneLevelSnapshot`。运行中编辑原 SO 不改变会话。稳定 ID 使用 ordinal 字符串；重复 ID、无效数值、越界 lane、缺失敌人和未排序刷怪时间均拒绝启动。

`BoardModel` 独立保存地形与占用；`BoardProjection` 统一逻辑/世界/屏幕换算。格宽高可不同；棋盘外和右/上开边界拒绝拾取。相机看向 XY 平面。表现排序取逻辑脚底；同一角色使用 SortingGroup，弹丸有独立排序。

## 命令、实体与时间

`PlacementCommand(long session, long commandId, string unitId, Cell cell)`。session 必须为当前 Generation，commandId 在本会话严格单调递增且大于零。输入和消费适配器可共享 `NextCommandId()` 分配器；外部已提交较大编号时分配器会继续向前。失败命令也消耗其编号，重试用新编号；重复/过期编号不重复提交。`Place(in PlacementCommand)` 返回带原因和新实体标识的 `PlacementResult`。`CardRejection(id)` 供 UI 显示与格子无关的可用性，最终格子与产品策略检查仍在提交时执行。

预览和提交共用费用、冷却、地形、占用、上限及可选策略检查。最终提交在主线程同步完成，无 GameObject 实例化或外部事件回调；必要 Prefab 在 SceneParticipant 准备时预检。表现对象不决定模型提交，资源错误会使准备失败。消费项目增加必须在提交前成功的资源步骤时，须沿准备/候选会话边界实施，不在 UI 中半扣费。

`EntityKey(Session, Id)` 不因表现对象复用而复用。`RemoveUnit` 的公开玩家入口只接受 Demolished；死亡由战斗提交，Restart/SceneExit 由所有者释放。默认拆除返还配置比例，死亡、重开、退出无收益。

`Advance(float delta)` 是唯一权威时间入口，内部以最多 .02 秒的步长推进，单次 delta 合法范围为 0..1 秒。Module 每帧最多提交 .25 秒，避免长时失焦后的追帧突发。冷却、刷怪、攻击与弹丸共享战斗时间；暂停不推进该时间，不设置 Time.timeScale。UI 与 Loading 沿 EUI/base 生命周期运行。

## 波次和终局

Ready → Running → Intermission → Running → Won/Lost；Disposed 终止一切。暂停是独立标记。开始波次在 Ready/Intermission 有效，重复开始拒绝。空波可立即结束；无波配置在显式开始时胜利。

波次结束同时要求生成任务全部处理和存活敌人数为零。敌人只能被登记为 Killed、Escaped 或 Disposed 之一；逃逸伤害提交一次，使用饱和计算避免同帧大额伤害整数溢出。同一步先处理战斗事件再结算，基地生命小于等于零优先失败。终局生成一次不可变 BattleResult，清除未结束弹丸，封闭新命令；场景退出不生成胜利结果。模拟期通知在该次 Advance 迭代退出后发送，结果观察者可以安全请求清理。

## 准备、重开与释放

`LaneDefenseSceneParticipant.PrepareAsync` 验证冻结配置、输入准备状态、正交相机及必要 Prefab，返回未开始的候选。`IGameScenePreparedParticipant.Start` 绑定表现并启动会话，交接给 Gameplay Module。沿 base GameSceneScope/GameSceneSession 提交、失败释放及 stop/drain 协议，不另建场景管理器。

Module 是唯一帧更新入口；场景租约唯一持有会话与视图。输入使用 base GameInputModule 和 GameTransitionInputGate，Action 为 Gameplay/Point、Place、Cancel。重开销毁旧代次并从冻结关卡创建新会话。Dispose 可重复调用；退出清除输入订阅、实体表现、回调和权威实体。迟到的 ProjectileContext 和旧 EntityKey 无法向新会话提交。

示例 View 使用 URP Base 棋盘相机，准备时保持禁用；Start 接管 base 的 UI Overlay 相机栈，退出/重开时归还原 owner 和索引。不会让独立 Base 相机覆盖 UI。必要 Sprite 也在准备阶段校验；默认示例没有对象池，所有表现实例由本会话视图销毁。
