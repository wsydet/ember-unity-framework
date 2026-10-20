# 弹射水平控制与墙跳资格策略

归属 platformer-2d 的 Entity/Player 模板业务。Assets 实现已完成 Unity 编译、27 项专项、679 项 Player PlayMode（含专项）和 76 项 Player Editor 回归，均通过。本扩展随 platformer-2d 0.9.0 交付；消费项目验收独立进行。

## 公开入口与默认兼容

```csharp
var request = new Game.Entity.LaunchRequest2D(
    "vertical-launch", new Vector2(0, 20),
    Game.Entity.LaunchMode2D.ConstantRise, 13f / 60,
    horizontal: Game.Entity.LaunchHorizontal2D.PreserveControl,
    wallQualification: Game.Entity.LaunchWallQualification2D.RefreshFirstVertical);
var result = target.TryLaunch(this, request);
```

`Horizontal` 默认为 `HoldRequestVelocity`，`WallQualification` 默认为 `Preserve`，均为枚举零值。旧构造调用与缺少新字段的 LaunchZone2D 资产保持默认规则：ConstantRise 锁输入并保持请求 XY，不恢复墙跳资格。侧向弹射可显式选择 HoldRequestVelocity。

PreserveControl 只允许与 ConstantRise 一起使用，构造器拒绝其他组合及未知枚举。它忽略请求 X，逐物理步保持自身 Y；玩家 X 继续使用当前冻结方案的地面/空中速度、起步、刹车与转向规则。仅开放水平方向输入，跳跃、冲刺、攻击与竖直方向输入继续抑制，期间按键不在结束后补发。它不是 InitialVelocity，也不会新增玩家物理驱动。

LaunchZone2D Inspector 的 `_horizontal` 与 `_wallQualification` 对应两个选项。请求是不可变值类型，接受时复制到本次运动凭证；修改机关 Inspector 仅影响以后的请求。新字段不属于 PlayerFeel/PlayerScheme，因此无需增加方案 Validate/Copy/Compare/Capture 字段或改变方案指纹。请求构造器承担策略校验，外部运动请求再次校验模式组合。

## 速度合成与清理

PreserveControl 接受时只替换外部运动接管，保留 AddVelocity 环境凭证、上一物理步贡献以及支撑/离台继承状态。自身水平速度仍通过 SelfVelocity 扣除上一环境与支撑贡献，再执行原水平控制；Compose 最后只加一次当前环境与支撑。这里的恒定 Y 指自身速度，最终世界速度还包含环境/承载 Y。

默认 HoldRequestVelocity 保持原 T03 清空全部外部来源的行为。RejectWhileBusy 仍在已有暂态来源（包括风或支撑）时返回 Busy；需要保留风并替换弹射时使用默认 ReplaceExternalMotion。

时长按实际玩家固定步消耗，最后一个保持步可能量化到请求时长之后；不承诺与旧项目触发相位完全一致。到期、撞顶、owner 禁用/销毁或凭证释放后解除保持；End 为 StopVertical 时下一次 Compose 将自身 Y 置零，再合成环境与支撑。该待执行停速也属于瞬态，不能被即时快照捕获。

整体暂停不消耗玩家保持时长；死亡、玩家退出清理外部运动，受击在提交的同一步清理，再建立受击输入缓冲。旧凭证与旧 owner 不会删除新的登记。已经成功授予的墙跳资格不会因随后 owner 失效被撤销；未来落地和早期反向转换仍遵循原规则。

## 资格交接与通知顺序

RefreshFirstVertical 是一次接受效果，不是持续强制资格。接受顺序：

1. 检查目标、owner、玩家会话和互斥动作，执行项目动作合法退出。
2. 再检查会话及 owner 有效性，替换运动来源并登记本次凭证。
3. 刷新 `Context.Wall.VerticalAvailable`。
4. 同步发出一次 `PlayerLaunchTarget2D.Accepted(request)`，随后返回 Accepted。

因此回调和调用返回后同一步可见资格刷新，速度仍等待唯一固定步提交。Inactive、Busy、空/失效 owner 不刷新、不通知。默认策略不改变已消耗资格。上/侧弹射、InitialVelocity 均可选择刷新；载具没有墙跳资格，忽略该声明。

顺序调用的 Replace 请求以后接受者为准，事件按接受顺序各发一次。Accepted 观察者异常沿用既有记录错误并继续通知的规则，不回滚已接受结果。

## 平移载具

TranslationSupportLaunchTarget2D 的 PreserveControl 保留当前 X，仅设置发射 Y。`AllowsHorizontalControl` 表示宿主可以继续原有水平驱动；宿主将包含其风驱规则的水平结果提交给 `TrySetHorizontalVelocity(x)`，适配器保留当前 Y。没有重复发射请求，也不需要写玩家 Motor。双轴保持期间此方法拒绝写入。

宿主仍应在 IsLaunching 时暂停普通路径的完整 XY 发令；保持期间只走上述水平入口，并在玩家支撑结算前完成。乘员顶棚净空限制仍优先，不在净空限制之后恢复上升速度。载具的运动规则及风速公式属于消费项目，本契约不提供产品公式。载具与玩家证据必须分开。

## 验证和消费升级

新增 `PlayerStateTests.LaunchPolicy_*` 用例覆盖镜像松键/反向、Y15/Y20、无风/单风/相反风/多来源、支撑离台、停止/继续、回调前资格与合成键盘触发墙跳、拒绝、替换、暂停、owner、生命周期、顶棚和载具接口。左右墙及上/侧发射均连续完成三轮不落地资格交接；另覆盖机关接受后的配置冻结。渲染测试使用真实帧累加器驱动局部物理场景，记录目标 30/120/240、实际平均 FPS 与 .02/1⁄60 局部物理步长；不改全局 fixedDeltaTime，也不把目标值当作实测值。

2026-10-20 已完成上述专项及 Player 完整回归。正式交付前仍需在消费项目验证认可方案、完整挥旗自动回弹进入自有风场、原路线与实际设备；模板合成动作夹具不代表产品动作验收。

维护端先完成 Unity 编译与测试，再通过项目中心保存、核对 contentHash，按授权执行 Bump；正式框架发布另行授权。消费端在收到已封存版本/hash 后备份全部本地修改及方案，使用正式模板升级差异预览逐项合并；禁止全量覆盖、改 PackageCache 或手工替换模板快照。无选择的新 API 调用和现有资产保持兼容。S2 听感、手柄与 S3 接入门槛独立保留。
