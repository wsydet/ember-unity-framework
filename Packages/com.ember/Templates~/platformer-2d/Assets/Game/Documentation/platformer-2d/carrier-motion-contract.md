# 载具自身碰撞与本步承载速度

本契约归属 platformer-2d 及合法派生模板。载具保持 Kinematic、零重力和冻结旋转；产品拥有自身风驱、重力/路径公式与触发规则。玩家仍仅由 PlayerModule 驱动，产品不增加 Player.FixedTick、不覆写玩家刚体。

## 初始化与默认兼容

`EntityTranslationSupport2D.ConfigureMotion(true, obstacleLayers, .025f)` 显式启用自身碰撞约束。默认关闭，既有直接速度支撑不会自动启用碰撞；`EntityMotor2D.SetSweptDisplacement` 的整向量动作语义不变。

约束逐个扫掠刚体上启用、有效、非Trigger的附着Collider，按阻挡法向收窄速度并保留允许的切向；修正方向后从真实起点重扫。复杂夹角12轮未收敛时保守停住。它提交本步安全直线速度，不移动Transform、不单独推进物理，不是旋转平台或任意动态刚体接触求解器。

显式运动掩码、全局Layer矩阵、碰撞对豁免共同生效。单向表面根据启用的PlatformEffector2D、colliderMask、rotationalOffset、surfaceArc和起始分离过滤；从下向上可穿，从有效上表面下降才阻挡。原独立Box接缝保持独立，不合并为整块实心地面。

## 固定步顺序

| 阶段 | 所有者与操作 |
| --- | --- |
| -1000 | 产品宿主计算水平和环境风一次，提交常规速度或弹射请求；弹射保持期仅更新允许的水平控制 |
| -500 | 支撑自动FixedUpdate：校验弹射owner、推进剩余秒数、处理结束策略，再结算载具自身扫掠 |
| GameLauncher默认0 | 唯一PlayerModule驱动玩家：Sensors→Support.Observe→乘员净空约束→采集支撑最终速度→原Compose→Motor.ExecuteStep |
| Unity物理 | 正常积分本步载具和玩家指令 |

项目Script Execution Order不得覆盖上述相对关系。所有载具发令/替换必须发生在支撑阶段之前；不得在玩家观察之后再恢复目标Y。碰撞回调中的新请求在下一物理步经过支撑结算后生效。

乘员由支撑观察登记，在自身扫掠中排除；离台、会话结束、瞬移或停用时释放登记，不改变Unity碰撞对。乘员净空收窄Y后会再次检查载具自身路径。净空查询使用同样的单向规则，不把单向平台底面当成顶棚。外部速度扣除及尾段碰顶判断忽略`ContactPoint2D.enabled=false`的接触，防止上穿时错误移除上一承载贡献而重复合成。

该自动顺序针对模板唯一玩家。多个玩家在同一步各自收窄同一平台净空的全局一致性不在本次验收范围内。

## 宿主接线示例

```csharp
// 初始化：保留产品实体形状、风公式和所有权。
support.ConfigureMotion(true, obstacleLayers, .025f);

// 宿主类标记 [DefaultExecutionOrder(-1000)]。
private void FixedUpdate()
{
    float x = ComputeProductHorizontalOnce();
    if (launch.IsLaunching)
        launch.TrySetHorizontalVelocity(x);
    else
        support.SetVelocity(new Vector2(x, ComputeProductVertical()));
}
```

`PreserveControl`只在ConstantRise期间允许`TrySetHorizontalVelocity`。请求X不会替换产品X；水平API保留当前已约束Y，不能复活弹射最初的上升速度。`HoldRequestVelocity`仍保留原请求的水平策略。

`TargetVelocity`是弹射到期处理之后、自身扫掠之前的输入诊断；产品原始发令应由宿主自行记录。`Velocity`是当前允许速度，玩家承载读取它。不要把TargetVelocity当成承载输出。

`PrepareStep(dt)`由支撑FixedUpdate自动调用。它公开供隔离物理测试或完全显式的模拟宿主使用，没有步ID去重；正式产品不得在自动FixedUpdate之外再调用，避免重复计时。

## 到期、替换与清理

- dt=.02、保持13/60秒时，第11次支撑阶段推进到期。StopVertical先清本步Y，再供玩家观察；ContinueVelocity只结束弹射所有权，保留当前已允许速度。
- 撞顶或净空已收窄Y后，宿主水平更新不会恢复原Y。当前Y已归零时，下一支撑阶段结束上升保持。
- 同一步替换先结束旧请求，再安装新请求。旧owner禁用不会清新请求；当前owner失效在支撑阶段按当前结束策略处理。
- `CancelLaunch()`公开取消当前请求。`Teleport(position)`使revision变化、取消弹射、清速度与乘员登记；停用支撑也清理弹射和速度。`TranslationSupportReset2D`通过Teleport恢复基线，不保留旧弹射计时。
- 暂停期间停止物理步，或载具`Body.simulated=false`时不推进支撑计时。恢复后重新应用项目持有的碰撞豁免；不要通过停用/再启用支撑模拟可恢复暂停，因为停用本身是清理语义。
- None／Horizontal／Full离台使用最后一次有效允许速度。禁用、销毁、瞬移、旋转或失效revision不产生旧支撑惯性。

## 验证与消费边界

维护侧证据位于开发仓库`docs/dev/evidence/carrier-contract/`，基线复现、首次交叉失败与修复复测分别保留。同步隔离物理轨迹的FPS标记为N/A；正式流程报告记录目标和实际FPS，目标120不能替代稳定120实测。

原来源单向平台Box(1,.3)、offset(0,.35)、surfaceArc179.5、Grouping=false的上穿、下落及接缝属于来源验收输入。维护侧新增交叉实际发现并修复的行为与历史来源规格分开记录。

部署过的项目不会因包升级自动更新Assets。未来发布后先通过Ember/UPM Manager升级包，再保护定制和原GUID，比较部署基线与新模板，人工合并EntityTranslationSupport2D、TranslationSupportLaunchTarget2D、TranslationSupportSweep2D、PlayerSupportMotion、PlayerExternalMotion及必要接线。不要全量覆盖产品目录或替换已认可玩家方案。

模板中心保存/Bump仅代表维护侧内容封存，不代表包已发布、消费项目已升级或产品路线已验收。Windward的P01/P04、S3、听感和手柄验收继续由消费侧独立完成。
