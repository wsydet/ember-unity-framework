# 外部运动与可选塌陷平台

2026-10-17：以下API位于活动Assets，已通过Unity完整回归及收尾专项实测；准确的最终验证、普通保存和版本状态见开发仓库公共能力实施记录。平移支撑、弹射、检查点与组合保存见 [公共能力接入](CheckpointIntegration.md)。普通保存不代表发布。

## 外部运动入口

新增可选 `preserveHorizontalControl`（仅 ForcedRise）及其通用弹射接线见[弹射策略](launch-policy.md)。本页历史验证数字不覆盖该扩展，其本轮回归结果见弹射策略说明。

每个运行会话的 `player.Context.ExternalMotion` 独立登记来源。调用在 Unity 主线程执行，owner 必须是启用的 `Behaviour`；返回 `PlayerExternalMotionLease`，调用方在退出范围、禁用、销毁或交互关闭时 Dispose。会话还会在下一次玩家更新前剔除失效 owner，死亡和会话释放使全部旧凭证失效。

```csharp
// owner 是当前组件；不要从另一个 FixedUpdate 写玩家 Rigidbody2D。
PlayerExternalMotionLease source = player.Context.ExternalMotion.Register(this,
    new PlayerExternalMotionRequest("environment-zone",
        PlayerExternalMotionMode.AddVelocity, new Vector2(2, 0)));
// 退出区域或组件 OnDisable/OnDestroy：
source.Dispose();
```

| 模式 | 当前行为 |
| --- | --- |
| AddVelocity | 各来源向量相加；移除单个凭证不移除其他来源 |
| ReplaceVelocity | 替换基础 XY 速度后再加环境速度；priority 大者优先，同优先级后登记者优先 |
| ForcedRise | 指定有限 XY 速度（Y为正）与正时长，期间重力为0；时间到后按模板重力继续，上一物理步上升被阻止则结束 |
| InteractionLock | 从合法稳定地面姿态进入，冻结该玩家物理与 FSM 推进；玩家能力/效果和来源计时继续；嵌套凭证全部释放才解锁 |

`duration` 单位秒，0表示显式释放（ForcedRise 不允许0）。`lockInput` 屏蔽该玩家 Adapter 的移动/跳跃/冲刺/攻击，并在切换时清理缓存；不修改输入 Manager 的全局 Action Map。产品新增的输入动作应遵守 `IsInteractionLocked` 和自己的会话门控。

内部互斥运动/生命流程优先，外部速度在其占用期间暂不参与，但来源时长继续消耗。platformer-2d 0.6.0 提供[项目动作接管契约](ProjectActions.md)，由真实 FSM 动作声明出口，弹射先合法退出再 Accepted；未接入该契约的项目动作不会自动获得打断能力。ReplaceVelocity 不接管重力，空中零速度/零重力应走动作契约。环境叠加采用简单向量和；产品需要的强弱风合成表应先在产品侧算好，再提交一个合成来源。

唯一固定步写入链为：清理失效来源 → 探测/支撑/落地事实 → Motor.BeginStep（扣除上一环境贡献）→ FSM → Actions.Apply → 未占用的 Wall/Ledge/Vertical/Correction → ExternalMotion.Compose → Motor.ExecuteStep → 来源按秒计时。Motor 本身未增加 MonoBehaviour 更新入口，`Motor.Velocity` 继续表示物理实际速度；计算自身运动的入口使用 `ExternalMotion.SelfVelocity`。

```csharp
// 已由项目判断交互资格；必须是稳定地面且无暂态动作。
var interaction = player.Context.ExternalMotion.Register(this,
    new PlayerExternalMotionRequest("local-interaction",
        PlayerExternalMotionMode.InteractionLock, Vector2.zero));
// 世界继续更新；关闭只释放自己的凭证，不回滚世界。
interaction.Dispose();
```

局部锁使用 Motor.Freeze，锁定期间玩家刚体不参与物理模拟；项目须保证交互位置安全。此机制不适用于移动支撑物上编辑、需要保持刚体接触事件的交互或空中编辑。全球暂停仍使用已有 PlayerModule.Pause，恢复全球暂停不会释放局部锁。来源持续时间按实际玩家固定步计算，全球暂停不推进。

存在外部来源或尚未结算的环境贡献时，`PlatformerSnapshot.Capture` 拒绝即时快照；并未放宽快照合法性。组合检查点保存通过CheckpointProgressCodec<T>/CheckpointSaveSession<T>实现，详见配套接入文档。

`Describe()` 返回来源 owner、ID、模式、优先级、剩余时间、自身/环境/合成速度和覆盖原因；已接入原玩家 FSM 观察指标。`PlayerConstantVelocitySource` 是无产品依赖的恒速来源示例，在 Inspector 指定 Player 与速度。

## 可选塌陷平台

`Game.Entity.CollapsingPlatform2D` 不自动装配到场景。配置稳定 ID、实体碰撞体、站立触发探头、覆盖恢复体积的净空触发探头、占位对象 LayerMask 和时长；两个探头始终启用且独立于实体碰撞体。避免让探头范围覆盖固定地面。稳定 ID 的跨场景唯一性仍由场景配置方负责检查。

状态为 Ready → Counting → Hidden → WaitingForClearance → Ready。`collapseOnLeave` 决定提前离开是否塌陷，`canRecover` 决定自然恢复；`SetPaused(bool)` 仅暂停此机关。`RequestReset()` 也先等待净空，不直接在角色内部打开碰撞。组件禁用时关闭实体碰撞，再启用时等待净空。`PhaseChanged` 供视觉/音效使用，不依赖动画回调驱动碰撞。

## 必须补做的验证

1. Unity 编译后运行 `Game.Player.PlayModeTests`，包括新增 ExternalMotion/CollapsingPlatform 用例及全部原有移动、墙、冲刺、战斗、快照和恢复回归。原来源/塌陷测试与新增真实物理用例已运行，详细结果见实施记录；状态推进测试，不能代替探头物理实测。
2. 正式白盒加入两个恒定来源：重叠、逐个释放、禁用/销毁 owner，核对无累积；在墙、斜面和天花板测试碰撞后的自身速度恢复，尤其测试反向速度抵消与竖直附加速度。
3. 强制上升、普通跳跃、墙跳、冲刺交接，验证无残留和撞顶结束；测试不同 fixedDeltaTime。通用弹射和载具适配见配套接入文档；产品挥旗状态的打断接线仍须在项目验收。
4. 两个局部锁叠加，持有期间让独立机关持续运动、效果和限时来源到期；嵌套全球暂停/恢复，释放旧锁不能释放新锁；死亡、禁用、换场景后引用清空。
5. 塌陷平台实际站立/离开、多人及多碰撞体占位、恢复时占位、全局暂停、组件禁用/启用及复位；确认视觉事件和碰撞一致。
6. 在项目中心核对活动模板与全部受管差异后执行普通保存；保存后另核对内容 hash。Bump、发布、消费升级尚未执行。

2026-10-17复核：编辑器状态/Console可读，等待就绪超时不代表连接不可用。原539项回归已通过；速度抵消问题已通过实测复现并修正，来源/支撑/弹射/塌陷联合用例也已通过。最终完整回归及保存状态由实施记录承接。自身速度分解现在按真实接触法线和接触体速度处理，不以零合速度判定碰撞。
