# 可选弹射尾段契约

所属模板：platformer-2d。该契约属于 Entity/Player 可选玩法代码，数值由产品来源显式配置，不修改共享 PlayerFeel。

## 公共入口

```csharp
new LaunchRequest2D(
    string sourceId, Vector2 velocity,
    LaunchMode2D mode = LaunchMode2D.InitialVelocity,
    float duration = 0,
    LaunchEnd2D end = LaunchEnd2D.ContinueVelocity,
    LaunchInterrupt2D interrupt = LaunchInterrupt2D.ReplaceExternalMotion,
    LaunchHorizontal2D horizontal = LaunchHorizontal2D.HoldRequestVelocity,
    LaunchWallQualification2D wallQualification = LaunchWallQualification2D.Preserve,
    LaunchTailSpec2D tail = default);

new LaunchTailSpec2D(
    float riseAcceleration, float fallAcceleration, float fallLimit,
    bool slowBand = false, float slowSpeed = 0, float slowAcceleration = 0,
    bool latchSlowUntilDescending = false);
```

`LaunchTailSpec2D` 在 `Game.Entity` 中定义，`default` 关闭。加速度单位为世界单位/秒²，速度单位为世界单位/秒。终速传入正幅值。

尾段仅支持 `ConstantRise + ContinueVelocity`。非法组合在构造请求时拒绝，不清理原请求。
`PlayerExternalMotionRequest` 增加末尾可选值参数 `launchTail`，仅支持不启用 `stopVerticalOnEnd` 的 `ForcedRise`。
原有 `flight` 参数仍只接受 `InitialVelocity`，现有 `PlayerFlightSpec` 及旗跳下降契约不变。

`LaunchZone2D` 具有序列化 `_tail` 字段。Inspector/Prefab 中默认关闭；实例显式启用才生效。
`TranslationSupportLaunchTarget2D` 不支持玩家尾段，收到启用的策略返回新增的 `LaunchResult2D.UnsupportedPolicy`，保留原载具请求和速度。
不将玩家规则偷偷应用于载具，也不将此能力交付视为载具问题已修复。

## 接受、冻结和固定步

请求构造时执行 `Validate`；玩家接受前执行世界重力校验。配置是仅含值字段的结构体，
从来源组件到 `LaunchRequest2D`、`PlayerExternalMotionRequest`、会话凭证均按值复制。
来源实例随后编辑、重新反序列化或复制不会改变已接受的配置。
它不属于 `PlayerFeel` 或 `PlayerScheme`，因此不向这些配置的 Copy/Capture/Freeze 图添加共享引用；
其冻结边界就是请求接受。来源组件通过 Unity 序列化复制与模板正式 SaveTemplate 保存。

保持时长沿用既有固定步秒数计时：每个完整保持步结束扣减 dt；例如 13/60 秒在 dt=0.02 时保持11步，在 dt=1/60 时保持13步。
该量化规则未改变。

到期在最后保持步结束处建立尾段凭证，下一玩家固定步直接使用扣除上一步环境与支撑贡献后的真实自身 Y。
没有重新发射速度，没有项目 Update 轮询，也没有插入一帧共享重力。
碰顶则在下一固定步开始处结束保持并交接同一配置，使用真实物理解算结果。
启用尾段时同时检查真实顶面接触及相对表面速度，避免反向风扣除后自身Y仍为正而漏掉碰顶；
无顶棚时仅世界合速度变负不会提前取消保持。此检查不改动旧请求的判断路径。
尾段每步按当前自身速度选加速度，再限制本步可增加的下降速度；Unity 负责物理积分、接触和位置。

低速区为闭区间 `[-slowSpeed, +slowSpeed]`。
`latchSlowUntilDescending=true` 时，尾段初始化为强制慢速许可，
首次观察到自身 Y 严格小于 `-slowSpeed` 后永久解除本次闩锁；恰等于下边界时仍保留。
闩锁解除后使用当前 `JumpHeld`，以后即使速度重新进入低速区也不重新闩锁。
未启用闩锁时从第一步就跟随 `JumpHeld`。低速许可仅选择加速度，不生成跳跃输入。
落地或新动作取消本次尾段后回到既有动作规则。

尾段不占行动输入锁、不持有水平通道、不再刷新墙跳资格。
P02 水平策略仅作用于保持阶段；P03 刷新仅发生在接受成功时、`Accepted` 回调之前。
启用尾段的上弹和侧弹都保留已登记的附加风、支撑及离台继承，由既有合成器统一合成一次。
尾段加速度与限速作用于自身 Y，世界合速度可能因风或支撑而超过自身终速。

## 生命周期与优先级

| 边界 | 处理 |
| --- | --- |
| 正常到期 | 只有实际取得保持控制的请求可以交接尾段；保持输入锁立即释放 |
| 碰顶 | 结束保持，尾段采用真实自身速度，不恢复初始上升速度 |
| 落地 | 正式支撑且真实接触落地后清除尾段和慢速许可 |
| 接受新弹射/外部替换 | 旧尾段失效，新请求独立；同一步最后接受的替换拥有后续控制 |
| 拒绝候选 | 不清理旧请求、不刷新资格、不发接受通知 |
| owner 禁用/销毁 | 下次玩家更新前剔除；保持期直接取消，不交接尾段；重新启用不复活 |
| 旧凭证释放 | 只清理该凭证对应的尾段，不能清理后来接受的新尾段 |
| 弹射适配器禁用 | 释放其当前保持或已交接的尾段凭证 |
| 整体暂停 | 不推进保持计时、速度、尾段或慢速闩锁；恢复不重发速度，输入仍经正式输入恢复规则 |
| 新普通跳/墙跳/行动接管 | 既有 `NotifyJump` / 运动通道接管优先，取消尾段 |
| 受击/死亡 | 生命流程优先，取消保持和尾段；待处理受击也不能在到期时重新交接 |
| 会话停止/转场/恢复替换 | 会话释放清除全部临时所有权；旧会话凭证不能影响新会话 |
| 瞬时快照 | 保持期由外部运动检查拒绝，尾段由 `HasFlightPolicy` 检查拒绝；使用合法检查点基线组合保存 |

可通过 `PlayerVerticalMotion.LaunchTail` 只读观察 `SourceId`、`Generation`、`IsActive`、`SlowPermissionLatched`。
`PlayerExternalMotionLease.LaunchTail` 提供本请求交接后的凭证；`Dispose()` 只取消该次策略。
`PlayerLaunchTarget2D.IsLaunching` 仍仅表示保持/一次速度请求，不扩展为尾段锁定状态。

## 产品配置示例

以下是显式实例配置示例，数值不是模板默认值：

```csharp
var tail = new LaunchTailSpec2D(
    riseAcceleration: 120, fallAcceleration: 120, fallLimit: 20,
    slowBand: true, slowSpeed: 5, slowAcceleration: 60,
    latchSlowUntilDescending: true);
var request = new LaunchRequest2D(
    "product-up-source", new Vector2(0, 20),
    LaunchMode2D.ConstantRise, 13f / 60,
    LaunchEnd2D.ContinueVelocity,
    horizontal: LaunchHorizontal2D.PreserveControl,
    wallQualification: LaunchWallQualification2D.RefreshFirstVertical,
    tail: tail);
var result = target.TryLaunch(sourceOwner, request);
```

侧弹可使用世界速度 `(sign * 20, 10)` 和 `HoldRequestVelocity`，继续显式传入 `tail`。
不要在保持结束后补发 `InitialVelocity`，不要修改共享重力或普通跳参数以代替此策略。

## 默认兼容和消费迁移

所有新增参数放在末尾且默认关闭，旧调用源码继续编译；原枚举值顺序保留，新结果追加到末尾。
未启用尾段时仍使用原 `ContinueVelocity` / `StopVertical`、默认 `Preserve` 资格和原风/支撑路径。
现有 PlayerFeel、普通跳、墙跳、旗跳 flight 配置不需迁移。

消费项目先保护定制并通过 `Ember/UPM Manager` 更新框架，再按模板版本关系迁移 Entity/Player 源码。
模板新增可选能力按 minor 封存；跨 minor 不适用同 minor 的自动 patch 增量。
应在独立候选接线中只对目标来源启用策略，逐项复验完整尾段、旧路线和其他动作。
正式框架测试只证明契约，不替任何消费项目签收阶段出口。
