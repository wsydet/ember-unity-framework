# 项目动作接管契约

适用 platformer-2d 0.6.0。动作实现位于活动 Assets 的 `Game.Player.Runtime`，消费程序集引用此程序集、`Ember.Core.Runtime` 及自身使用的 Entity/Input 程序集。它是模板扩展契约，不是框架 FSM 内核新增的产品状态。默认移动/战斗图不会自动新增演示动作。

## 入口与真实状态

项目互斥动作继承 `PlayerActionState`，在构造时传入已声明且在图中绑定的退出转换；由 `EnterAction(PlayerActionLease)` 取得本次激活凭证，在 `ExitAction(PlayerActionEndReason)` 清理本动作资源。不要自行调用 Enter、保存状态单例或在另一个 MonoBehaviour.FixedUpdate 写刚体。

项目根 Definition 创建 `new PlayerLocomotionState(projectActions: true)`，保留原根的全部转换，再声明 FixedUpdate 阶段、`project-action` 分组中的项目入口。基础根使用原 Locomotion Definition；启用战斗时项目 Definition 须派生现有公开 `PlayerCombatLocomotionStateDefinition`，有冲刺攻击路由时派生 `PlayerDashCombatLocomotionStateDefinition`；启用生命时还实现 `IPlayerLifeRootDefinition`。保留这些装配校验，委托原根提供声明，不能丢失 life/combat/dash-combat 声明及绑定。独立示例兼容以上条件，以同一包装方式验证基础和战斗方案。未声明该组时不要开启 projectActions。

图增加根下的互斥叶子及返回 air 等实际目标的绑定；配置独占 AnimationKey、Clip、完整 Animator 路径和 AnimationProfile 槽。真实活动路径、动画和观察器都读取同一个 FSM，不接受仅在外部组件自称“正在挥旗”的影子状态。入口许可先查询 `Context.Actions.CanEnter`；动作自身 `CanEnterAction` 仍可返回合法拒绝。产品资格、资源消费和 FlagWave 输入按项目规则声明，模板不决定这些规则。

可编译、可运行的完整最小例子：[状态和 owner](../../Module/Player/Tests/Consumer/ActionContractProbe.cs)、[项目根声明](../../Module/Player/Tests/Consumer/ActionProbeRoot.cs)、[Definition](../../Module/Player/Tests/Consumer/ActionProbeDefinition.cs)、[配置](../../Module/Player/Tests/Consumer/ActionProbeSettings.cs)，程序集 `Game.Player.ActionContractConsumer`（仅测试编译，无友元授权、反射或刚体写入）。[PlayerActionContractTests.InstallActionGraph](../../Module/Player/Tests/PlayerActionContractTests.cs) 展示节点和绑定装配，[PlayerActionContractAnimationTests](../../Module/Player/Tests/PlayerActionContractAnimationTests.cs) 展示独立方案复制、保存重载和真实动画接线。项目可迁移这个模式到自己的正式程序集，不能把测试程序集作为发行依赖。

## 凭证、命令和部分释放

每个运行候选有独立 `Context.Actions`；每次真实激活生成独立 `PlayerActionLease`。身份同时受会话对象和 FSM 激活代数约束。Generation 仅是该 FSM 内的代数，不可当作跨会话/跨宿主 ID；调用方持有凭证对象本身。旧凭证的合法 Set/Release 返回 false，End 返回 StaleActivation，Dispose 不会清理新动作。凭证不提供重新取得已释放通道的能力；要重新接管必须重新进入动作。

| 通道 | 持有时承担的职责 | 释放后的宿主职责 |
| --- | --- | --- |
| Horizontal | SetHorizontal 提供持续水平速度 | 根水平加速/制动和输入 |
| Facing | SetFacing 提供朝向 | 根按输入更新朝向 |
| AutomaticTransitions | 阻止根普通移动/落地/战斗自动转换 | 恢复这些声明规则的评估 |
| Wall / Ledge | 阻止墙面/攀边后处理 | 恢复对应能力后处理 |
| Vertical | 提供竖直速度与非负 gravityScale | 恢复模板跳跃/顶点/下落重力计算 |
| Correction | 阻止模板运动补正 | 恢复补正；真实碰撞始终存在 |

默认取得 All。`SetHorizontal(0); SetVertical(0, 0); SetFacing(...)` 是空中悬停，既不冻结 Rigidbody，也不暂停世界。命令在宿主固定步提交；调用时不会立即改变物理对象。SetVertical 默认保持指定速度；传 `holdVelocity: false` 时仅下一固定步写入一次速度，此后由实际物理重力积分，gravityScale 保持到释放 Vertical/退出。退出或释放 Vertical 后，下一个固定步恢复模板重力，不要求项目计算补偿重力。

```csharp
// EnterAction 中悬停；项目决定具体时窗及数值。
action.SetHorizontal(0);
action.SetVertical(0, 0);
// 状态自身 FixedUpdate 的反冲/旗跳时刻：一次脉冲，仍由原 Motor 提交。
action.SetVertical(8, 3, holdVelocity: false);
// 后段恢复水平/朝向，再按需要恢复竖直及墙/攀边/纠正。
action.Release(PlayerActionChannels.Horizontal | PlayerActionChannels.Facing);
// 保留 AutomaticTransitions 可以继续动作动画；声明出口才结束真实状态。
```

后段可 `Release(Horizontal | Facing)` 恢复移动和朝向，仍保留真实动作状态、竖直及自动转换职责。释放 AutomaticTransitions 后，合法父级转换可能当步退出动作；代码应检查凭证 IsActive。`End()` 请求构造时传入的退出转换，返回真实 FSM 结果。GuardRejected/EntryRejected 时动作仍在；Dispose 同样不绕过 Guard，不能把 Dispose 当作强制停止。宿主 Stop/禁用/恢复替换不依赖动作退出 Guard，都会清理本代资源。

## 顺序与优先级

唯一固定步链：失效 owner 清理 → 探测/支撑事实 → Motor.BeginStep → 根 life → 项目 project-action → 普通 decision/战斗 → 叶子固定步 → Actions.Apply → 未占用 Wall/Ledge/Vertical/Correction → ExternalMotion.Compose → Motor.ExecuteStep → 动画同步及来源计时。父先子后和 FSM 提交短路保持不变；死亡/受击优先于项目动作。进入动作时清除旧墙跳保护窗口和支撑运动缓存，避免交接残留。

项目动作与已有 dash、ledge、rolling、landing、drop-through、head-jump、combat、生命反应及稳定地面交互锁互斥。它不抢占这些动作；应在项目规则中明确等待/拒绝。项目动作按轴屏蔽外部运动：持有 Horizontal 时不合成来源 X，持有 Vertical 时不合成来源 Y 或 ForcedRise 重力；对应轴释放后恢复原合成。仅保留 AutomaticTransitions/朝向/墙/攀边/纠正不会额外屏蔽已释放轴的风速。来源计时和既有非项目内部动作的优先规则不变；尚未退出动作时即时快照仍拒绝。

`ReplaceVelocity` 继续只替换速度，不承诺重力接管；ForcedRise 继续要求正 Y；InteractionLock 继续只适用于稳定地面交互。空中动作使用本契约。

## 取消事务

| 触发 | 行为 |
| --- | --- |
| 正常 End / 动作计时到期 | 请求声明出口，Changed 后释放；合法拒绝保留动作 |
| 弹射 ReplaceExisting | 先检查既有合法拒绝，再以 Launch 原因请求出口；失败返回 Busy；成功重新核对宿主/会话/owner，登记弹射，最后发 Accepted |
| 弹射 RejectWhileBusy | 活动动作返回 Busy，不退出、不发 Accepted |
| 死亡 / 受击 | 根 life 转换优先，ExitAction 收到 Death / Reaction |
| owner 失效 | 可选 BindOwner；下次宿主推进请求 OwnerDisabled 退出；若拒绝则停止宿主，防止悬挂接管 |
| 宿主禁用 / 销毁 | 立即停止候选并使凭证失效；Unity 销毁先 OnDisable 时原因是 Disabled，不承诺一定收到 Destroyed |
| 转场 / 正式 Stop / 恢复替换 | 旧候选关闭，SessionEnded；新候选有独立 session，旧清理不影响新候选 |
| EnterAction 异常 | PreparationFailed，归还通道，再交 FSM 故障处理；已提交产品资源不自动回滚 |
| 准备候选失败/取消 | 释放候选及冻结副本，不改变现有运行会话 |

ExitAction 在清除本代凭证及通道后调用，必须幂等、只清理本动作持有的租约。不要通过“当前组件上的最新 lease”去清理另一次动作。Accepted 仅用于接受后的通知，不再承担解除接管的前置责任。退出回调若禁用/替换玩家，旧弹射请求返回 Inactive，不能登记到新会话。全局暂停仍走 PlayerModule.Pause，暂停不推进动作状态计时或重力；没有新增第二套 Update/FixedUpdate。

## 冻结、复制与恢复

带项目参数的 Definition 实现 `IPlayerConfigurationCapture`。准备流程先创建私有 SO 副本，在副本上调用 `CapturePlayerConfiguration(PlayerConfigurationCapture capture)`；其中每个可变 SO 依赖必须赋值为 `capture.Capture(source)`。依赖若又包含可变 SO，也实现同一接口递归捕获。回调内校验值，非法配置抛异常，不能回退默认合法值。回调只在准备期间有效，不得保留 capture 对象供运行使用。

CreateState 将私有副本的有效标量复制进每宿主状态实例；运行中不读取源 Definition/SO。Unity 可序列化值字段/数组随 Instantiate 复制；未声明递归捕获的 UnityEngine.Object 引用仍共享，不自动保证深拷贝。美术等明确不可变引用可共享。禁止配置反向捕获整份 PlayerConfig、PlayerScheme 或图。

PlayerSchemeCapture 统一拥有副本；恢复候选沿用会话冻结 A，源值改变只影响新准备的会话。候选失败及引用计数归零后销毁副本。玩家配置工具的“独立复制”仍负责复制完整方案及其资产依赖；本轮以自定义 Definition→自定义 Settings 真实保存/重载验证该路径，不另建项目复制器。

活动项目动作无论剩余通道多少都拒绝 PlatformerSnapshot 即时捕获。已提交成果仍采用已有合法检查点 + CheckpointProgressCodec<T>/CheckpointSaveSession<T> 组合保存；动作计时、凭证及 Animator 进度不入档，恢复从合法基线重建。详细调用见 [检查点接入](CheckpointIntegration.md)。

## 验证边界

本契约的独立消费程序集、隔离 PhysicsScene2D、真实 Input System 键盘、正式 PlayerModule/Scope/恢复/独立存储及资产复制测试在 `PlayerActionContract*Tests`。框架维护记录提供本轮任务 ID 和实际结果。它们不能替代 Windward 正式场景、FlagWave、16 格风规则、资格刷新、贴纸及关卡验收；模板交付不代表 Windward S2 通过。
