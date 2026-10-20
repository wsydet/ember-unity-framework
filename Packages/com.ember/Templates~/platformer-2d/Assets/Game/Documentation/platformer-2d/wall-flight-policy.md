# 墙跳与单次飞行策略

所有者：platformer-2d / Player。两项能力默认关闭；不修改内置方案、普通跳配置、地空惯性或共享重力。所有计时使用游戏秒，由现有 Controller 固定步推进，Unity 继续求解碰撞和位置。

## 墙跳配置

玩家开发工具 → 玩家配置 → 运动参数 → **墙跳独立分支与运动**。先 Copy 完整独立方案，经 RequireWritable、Undo、配置、Validate、Compare、Save 保存。公开入口为 `PlayerJumpFeel.ConfigureWallJump(PlayerWallJumpSpec)`；捕获值位于 `PlayerFeelRuntime.WallJump`。关闭用 `default(PlayerWallJumpSpec)`；空白方案也显式关闭。配置是 Jump 子资产中的值，不增加可变资产别名。

`PlayerWallJumpSpec` 的构造参数：

| 参数 | 契约 |
| --- | --- |
| vertical | 独立竖直曲线值。复用 `PlayerOrdinaryJumpSpec` 的保持、松键、上升/下降及顶点参数格式，但只激活 WallActive，绝不启动 OrdinaryActive 或读取普通跳配置 |
| outwardSpeed | 向墙外的水平初速，u/s |
| horizontalLimit / overspeedDeceleration | 超过该绝对速度时，独立减速到阈值；之后交回现有空中运动。不会修改地空惯性资产 |
| firstTowardVertical | 有资格且向当前墙输入时选择直上并消耗资格；中立/离墙输入向外。关闭时全部向外 |
| conversionWindow | 直上保持期间，起跳后此秒数内的反向输入可转向外；不重置竖直保持时间 |
| restoreOnConversion | 转向外是否恢复直上资格，显式选择，默认 false |

直上资格在新会话及落地时可用，消费后离墙、换墙、动作接管不会自动补回。直上保持期间水平为零；保持结束后正常空中控制。向外超速控制独立于普通空中加减速。启用此策略后，旧 WallJumpX/Y、WallProtection 和 WallReturn 不参与这次墙跳；墙滑速度与能力授权仍使用原配置。关闭后保留原固定向外、保护及折返行为。

碰顶立即结束保持；墙跳离开墙面仍继续独立飞行，下降重新进入墙滑时结束独立飞行并使用墙滑规则。观察接口为 `Wall.VerticalBranch / VerticalAvailable`、`VerticalMotion.WallActive / WallElapsed`。

## 单次下降策略

`PlayerFlightSpec` 是可序列化、只有只读访问器的值。`new PlayerFlightSpec(fallAcceleration, fallLimit, heldSlowFall, heldSpeed, heldAcceleration)` 使用 u/s²、u/s；default 关闭。验证拒绝 NaN、无穷、非正加速度/终速，以及比常规下降更大的低速加速度。

该策略仅在纵向速度非正时替换下降加速度和终速，上升速度及重力继续使用原通道规则。可选 HeldSlowFall 只在下降低速区且当前跳键按住时使用；输入清空/暂停恢复后，本次飞行不再获得旧持键优惠。它不会改变动作时钟、音效、动画、相机或环境风。

动作代码在已持有 Vertical 通道的真实激活中调用：

```csharp
// 数值、来源 ID 和 owner 由消费项目的冻结配置/运行会话提供。
Action.LaunchVertical(launchSpeed, ascentGravityScale, owner, sourceId, descent);
// 提交后的 Action.Flight 是本次飞行凭证。
// 原时刻照常 Release(Vertical) 和 End()；无需等到落地。
```

`LaunchVertical` 只提交一次速度。它在下一次正式物理提交时创建飞行；同一步提交前释放 Vertical 会取消尚未提交的命令。已提交策略在持有通道时也能管理下降，释放通道及动作正常结束后继续有效。再次 SetVertical/LaunchVertical 表示新的竖直运动，替换上一飞行。失效动作凭证无法提交；不要在 ExitAction 中无条件 Dispose 已交给飞行的凭证。

外部发射代码可传入 `PlayerExternalMotionRequest(..., mode: InitialVelocity, ..., flight: descent)`。获胜的实际纵向发射产生 `PlayerExternalMotionLease.Flight`；被动作屏蔽的请求不会产生飞行。原外部 lease 在发射一步后到期，飞行凭证独立存续。其他持续外力模式不接受 flight 参数，以免重复重启飞行；普通 AddVelocity 风场继续既有叠加规则。

消费项目必须为不同业务分支传入不同来源 ID，例如空中自动回弹、地面自动回弹、独立输入旗跳各自选择策略。模板不认识这些业务类别，也不依据笼统的 Action/External 枚举替消费者选择参数。策略可作为项目动作设置 SO 的字段；沿项目动作现有配置闭包/捕获流程复制为值，调用 `Validate()`。不要从可变源 SO 在每个物理步重读。

`PlayerFlightLease.SourceId / Generation / IsActive` 和 `VerticalMotion.Flight` 提供观察。凭证以实例身份匹配，Generation 在各玩家会话内递增；不同会话同号不代表同一飞行。Dispose 只取消该策略，不改速度，不会清理后来的 owner 或代次。

## 生命周期与优先级

| 事件 | 处理 |
| --- | --- |
| 动作释放部分通道、正常结束 | 已提交飞行继续；同一 owner 的通道缩减不触发重新接管清理 |
| 真实落地接触 | 清除策略；仅进入地面探针范围不提前清理下降限速 |
| 新普通跳、墙跳、二段跳、踩头 | 正式起跳通知替换；不恢复旧策略 |
| 新动作或内置运动接管 | 清除旧策略，新动作可显式发射新策略 |
| 新非附加外力 | 登记取消旧飞行；实际持续替换/强制上升取得纵向控制时再次清理，防止先登记的外力晚于动作取得控制而沿用动作策略 |
| AddVelocity | 环境贡献按原规则叠加，不替换飞行来源；终速约束的是自身纵向速度，不对环境附加速度伪造补偿 |
| 碰顶 | 自然转下降，单次下降策略继续；墙跳恒速保持结束 |
| 墙滑/挂边等内部重力接管 | 旧独立策略结束，由该动作控制 |
| 受击（含 pending）、死亡 | 当前更新前清理 |
| owner 禁用/销毁 | 下次玩家 Tick/FixedTick 剔除；暂停期间无物理推进，恢复首次更新先剔除 |
| 玩家禁用/销毁、停止、会话替换、场景退出 | 旧运行候选释放时清理，不能影响新候选 |
| 暂停恢复 | 不推进计时；保留下降策略，终止旧保持与低速持键优惠 |
| 瞬时快照 | 有独立飞行策略时拒绝；可选墙跳启用时空中也拒绝，以免丢失一次资格。继续用合法检查点基线，未改变 v3 格式 |

## 消费升级与验收

源码修改、项目中心普通保存、版本 Bump、框架发布和消费部署是不同步骤。普通保存仅更新 contentHash，不能将相同版本下尚未封存的内容当作已发布升级。维护方完成授权的 Bump 和框架发布后，消费方先升级 Ember 包，再按项目中心对当前版本支持的模板升级流程预览差异、保护本地修改并吸收 Assets；有冲突按正式恢复流程处理。禁止改 PackageCache 或全量覆盖产品工程。

升级后先核对版本、contentHash、活动模板和 API，再 Copy 独立候选。仅启用墙跳策略及目标动作分支的下降策略，保留普通跳、惯性、特殊跳配置；Validate/Compare/Save 后重建运行会话捕获。模板回归不代替消费项目的正式入口、完整路线、真实键盘手感、动画/听感和阶段签收。

旧帧制参考必须核对固定步及执行顺序，不把帧数绑定到渲染帧率。最短/最长边界、输入采样与新物理步会影响实测；所有演示数值均需消费项目独立校准。
