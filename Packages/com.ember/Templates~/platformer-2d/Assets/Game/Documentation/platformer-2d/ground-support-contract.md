# 地面支撑事实与跳跃机会

归属 `platformer-2d`。本契约修复旧 `grounded && worldVelocity.y <= 0` 在微斜支撑表面上的误判。维护源为本模板的 Entity / Player 模块；不是 `com.ember` 的通用 Runtime API。工作区修复的保存、封存、发布状态见维护交付记录，不能只凭旧模板版本号判断是否包含本契约。

## 唯一物理步判定

`PlayerController.FixedTick` 按以下顺序消费事实：

1. `EntitySensors2D.Sample()` 保留原有 `IsGrounded/Ground` 的几何探测语义，同时采集 `GroundContact`：有效、非触发、符合地面层和法线阈值、未被碰撞豁免或单向 Effector 禁用的真实接触。瞬移后离开探测范围的旧接触缓存被排除。
2. `PlayerSupportMotion.ResolveContact` 对已承载的平移平台使用上一物理步的表面速度。不能拿宿主刚提交、尚未模拟的新速度与角色上一拍的解算速度相减。
3. `PlayerJumpWindow.Observe(contact, dt, groundInRange, suppressSupport)` 统一确认支撑及取得边沿；Controller 在该边沿补充地面资源，然后依次驱动承载、落地、竖直运动与 FSM。
4. 地面/空中分支、移动、跳跃、落地、资源补充和承载消费同一 `HasGroundSupport`。`Sensors.IsGrounded` 仅表示附近几何，不能单独用作落地或资源补充条件。

尚未真正接触的下降角色可以保留已经产生的 coyote，但不会因为 Cast 命中而新增地面机会。原先依赖探测距离提前授予机会的自定义逻辑需要检查。coyote 与输入缓冲仍使用原配置和物理时间边界；没有扩大时长，也未改 Feel、重力、跳跃高度或移动惯性。

## 法线、相对速度与容差

`EntityGroundContact2D` 是只读值，提供 `Collider`、`Point`、朝向角色的单位 `Normal`、接触点的 `ActorVelocity`、`SurfaceVelocity`、`HasContact` 与 `SeparationSpeed`。

```text
SeparationSpeed = dot(ActorVelocity - SurfaceVelocity, Normal)
```

世界 Y 向上不等于离开地面：沿斜面运动和上升载具都可能有较大的世界 Y 速度，实际相对法向速度仍接近零。正式判定只在真实接触有效时允许 `SeparationSpeed <= 0.001 u/s`。这是固定的法向解算噪声上限，不是世界 Y 的放宽，也不是接触距离；在 0.02 秒物理步中对应 0.00002 单位的相对位移。没有接触、不可用法线或非有限速度不能靠容差取得支撑。明显朝外分离的接触返回 `Separating`。

主动普通跳、墙跳、二段跳、踩头弹起通过 `Consume()` 消费机会；向上外部替换/初速度/强制上升以及项目动作竖直指令也走消费。即使显式向上指令小于数值容差，仍必须消费。消费时存在支撑/真实接触则锁定该次接触，必须先观察到接触离开，才能由后续新接触重新授予机会。持续留在旧接触内的微小指令不会自行重置机会；业务不能每帧强行补充。

挂边和下穿的暂时抑制与一次性起跳消费分开。Controller 在动作有效期间抑制取得支撑，动作交回控制后再接收真实落地，避免每帧反复补资源或在翻越后永久锁住支撑。外部运动和项目动作仍遵守既有 owner、凭证、优先级及清理生命周期。

## 只读诊断

从 `player.Context.JumpWindow` 读取：

| 字段 | 含义 |
| --- | --- |
| `HasGroundSupport` | 当前统一支撑结果 |
| `CanJump / CoyoteRemaining` | 当前地面/离台机会和剩余秒数 |
| `Contact` | 本步使用的接触对象、法线、点速度和分离速度 |
| `AwaitingSeparation` | 已消费旧接触，等待实际离开 |
| `SupportReason` | `NoContact`、`Separating`、`ConsumedUntilSeparation`、`Contact`、`ActionSuppressed` 或兼容入口的 `LegacyProbe` |

玩家 FSM 观察面板同步显示支撑原因、接触对象、法线、相对分离速度及离开锁。诊断读取不采样、不推进计时、不补充资源。

旧 `Observe(bool grounded, float verticalSpeed, float dt)` 签名保留给已有调用方，仍使用保守的世界 Y 判定；bool 本身无法证明真实接触，因此不扩大该入口的 epsilon。正式 Controller 已全部改用接触重载。只拷贝 JumpWindow 而遗漏 Sensors/Controller/状态接线，不会获得完整修复。自定义唯一驱动应传入明确接触事实，普通消费项目应继续使用正式 Controller，避免增加第二个采样/补充入口。

## 落地、恢复和承载

落地冲击速度取最后一个提交物理步相对接触表面的法向速度，不取整段下落最大值。移动平台自身提交/解算时序、离台惯性、瞬移/禁用清理保持既有契约。

同步恢复快照时尚无新的物理接触，不提前声明支撑。恢复点位于原探地范围内且未上升时，记录一次待建立的地面基线；随后首次真实接触不补满存档中已消费的资源。真正离开探地范围后再落地正常补充。冻结方案、暂停、会话交接与旧凭证失效规则不变。

## 消费合并

先确认实际解析包与旧部署原稿的模板 ID/版本/hash，再比较新模板与项目本地内容。修复涉及 Entity 接触采样、Player 窗口、Controller 物理顺序、地空状态、落地、承载、外部运动和项目动作，必须作为一组审查。Prefab GUID、场景、方案和数值无需为本修复重建。

后续正式发布需经项目中心显式封存模板，并由新的框架发布版本承载。消费端先通过 Ember/UPM Manager 接入包，再按项目中心的模板差异与三方合并流程保留本地定制。不能直接改 PackageCache、覆盖整个 Player 目录或把同版本号当作内容一致。消费后只做接触、微斜台面跳跃、消费锁、离台、落地/资源及项目实际使用的承载/弹射专项；最后仍需用户在原台面人工复验。
