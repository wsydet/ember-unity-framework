# 普通跳独立竖直策略

所有者：platformer-2d / Player。新增可选能力，默认关闭。原 T01 项目动作接管契约保持有效。

## 配置与正式工作流

在玩家开发工具的完整方案中先 Copy 独立候选，再编辑“运动参数 → 普通跳独立竖直策略”。通过 Validate、Compare、Save 保存。运行实例在准备阶段捕获值副本；编辑源方案 B 不会修改正在运行的冻结 A。空白方案显式关闭此能力，旧方案缺失字段按 disabled 处理，内置方案不主动启用。

公开配置入口为 `PlayerJumpFeel.ConfigureOrdinaryJump(PlayerOrdinaryJumpSpec)`；它配置下一次会话，不是运行中运动命令。编辑器调用方仍负责 Undo、SetDirty 和正式方案 Save。`default(PlayerOrdinaryJumpSpec)` 表示关闭。

```csharp
candidate.Feel.Jump.ConfigureOrdinaryJump(new PlayerOrdinaryJumpSpec(
    launchSpeed: 14, minimumHold: .02f, maximumHold: .2f,
    riseGravity: 120, fallGravity: 120, fallLimit: 20,
    release: PlayerOrdinaryJumpRelease.EndHold,
    apexEnabled: true, apexRequiresHeld: true,
    apexSpeed: 5, apexMultiplier: .5f));
```

这里的数值只是机制演示，不是任何消费项目的已验收手感。

| 字段 | 语义 |
| --- | --- |
| Enabled | 仅本次合法普通起跳启用；不凭当前状态 ID 推测来源 |
| LaunchSpeed | 起跳及保持期的恒定上升速度，u/s |
| MinimumHold / MaximumHold | 最短与按住时最长保持游戏秒数，包含起跳步；0 ≤ minimum ≤ maximum |
| RiseGravity / FallGravity | 上升／下降加速度，u/s²；独立于原共享 GravityScale |
| FallLimit | 最大下降速度，u/s；包含本步重力增量 |
| Release | EndHold 只结束保持；LimitSpeed 把正速度限制到 ReleaseValue；MultiplySpeed 对正速度乘 ReleaseValue，最多一次 |
| ReleaseValue | 限速为 0..LaunchSpeed；倍率为 0..1；EndHold 不使用 |
| ApexEnabled / ApexSpeed / ApexMultiplier | 保持期外，速度绝对值不大于阈值时，对当前上升或下降重力乘倍率 |
| ApexRequiresHeld | 需要起跳时的同一次持续按住；松开后重新按下不恢复本跳顶点优惠 |

保持以实际 FixedTick 的 dt 计时；Update 不推进计时。保持在一步中间结束时，仅剩余步长施加重力，由 Unity 求解位置与碰撞。松键削速在达到最短保持期后的首个物理决策执行，采样误差最多一固定步。已经松开的缓冲输入仍可合法起跳，但只获得最短保持期，随后执行所选松键规则。

这不是逐帧复刻旧控制器。离散时间、接触求解与输入采样会影响高度和时间；旧源码公式推算不能替代 Unity 实测。原 `EstimatedHeight / EstimatedApexTime` 仍描述共享基础抛物线，不用于估算含保持期的新策略。

## 运动所有权与生命周期

`PlayerVerticalMotion.Origin` 提供 Natural、OrdinaryJump、External、WallJump、AirJump、HeadJump、Action 观察值；`OrdinaryActive` 与 `OrdinaryElapsed` 只读。只有 `PlayerJumpState` 在消费合法普通起跳资格后才能激活策略。恢复上升速度、外部旗跳、回弹、墙跳与自然下落不会隐式激活。每个运行候选持有独立实例，没有全局 owner 或可被旧回调续用的新凭证。

模板的 Controller → VerticalMotion → EntityMotor2D 仍是唯一速度／重力提交链路；消费项目不需要改私有字段、直接写 Rigidbody2D 或另起控制器。此策略不写水平速度，水平起步、刹车、反向、惯性与原跳跃响应窗口保持原有实现。普通跳节奏变化会改变在空中的时间，因此不能把普通跳总水平位移也要求为不变；特殊跳完整轨迹则必须保持。

| 事件 | 处理 |
| --- | --- |
| 普通跳到顶转 fall | 策略继续，使用自己的完整下降参数，不在 Jump.Exit 清除 |
| 落地 | 实际接触探测到的地面且竖直速度非正时，清除策略及计时，恢复共享自由飞行参数；仅进入探地范围不提前清理，以保持完整下降限速。同一步缓冲再跳可建立新策略 |
| 碰顶 | 实际 y 速度非正时永久结束本跳保持和削速；下降仍用普通策略，不重新顶住天花板 |
| 挥旗／项目动作接管 | 正式 MotionControl.Take 时取消，即使动作随后释放竖直通道也不恢复旧策略 |
| 墙跳／二段跳／踩头反弹／墙滑及内置动作 | 各自正式起跳通知或运动接管取消；自由飞行使用原共享配置 |
| External InitialVelocity / ReplaceVelocity / ForcedRise / InteractionLock | 合法登记后取消；固定步再次检查待提交来源，避免同一步起跳覆盖取消 |
| AddVelocity 环境风 | 不替换飞行来源，仍按既有外部贡献合成规则叠加 |
| 受击（含 pending）／死亡 | 当前更新前取消；生命流程继续拥有原运动规则 |
| 暂停／输入清空 | 暂停不推进计时；结束按住与松键削速控制，恢复后继续已冻结的普通上升／下降参数，不复活旧按键 |
| 停止／禁用／销毁／关卡退出／会话替换 | 释放旧候选时清除，旧动作 lease 不得改变新激活或新会话 |
| 快照 | 活跃独立普通跳拒绝瞬时 Capture；使用合法检查点基线保存。策略、计时、输入版本不序列化，不改变 v3 快照格式 |

共享 Jump 字段继续决定特殊跳和自然下落。启用普通策略后，校准普通跳应只改 `_ordinaryJump`；不要同时改共享重力或反向补偿特殊动作。

## 消费与验收

交付版本与本次实际测试结果见仓库发布准备记录。发布后先通过 Ember/UPM Manager 升级包，再按项目中心正式模板流程吸收 Assets 变化。0.6 → 0.7 跨 minor，不适用同 minor 的 patch 三方增量；先保护产品改动并按正式部署／恢复或人工迁移流程处理，不能直接覆盖产品工程。

本功能关闭时不要求数值迁移；开启时从完整独立候选开始，只改普通策略。先固定共享配置、移动和特殊动作，比较短／中／长按完整轨迹、按住与松开顶点差异，再复验特殊跳、墙跳、发射、碰顶、生命周期、输入缓冲和多步长。模板回归通过不代表消费项目键盘白盒、原路线或关卡验收通过。
