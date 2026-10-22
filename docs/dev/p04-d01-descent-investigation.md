# P04-D01 下降减速调查与历史证据

最新维护交付：本轮已完成48专项、下降12实际帧率场景、16生命周期交叉及Game.Player.PlayModeTests整组839/839（0失败/跳过）。项目中心正式保存1476文件并Bump为platformer-2d0.11.1，hash=22dd884949be5e2697a2606d5da00c46，实际hash/父快照/谱系均核对。通过Ember0.21.2交付，远端回执见最终发布报告。详见[p04-d01-delivery.md](p04-d01-delivery.md)。以下按原样保留此前连接中断、初始候选与失败历史，不将历史未完成状态作为当前结论。Windward未修改/部署，Mountain只读；P04/S3未由维护侧代签。

第五次继续（2026-10-22）：遵照用户要求未刷新、未修改C#。set_active_instance和read_console成功，控制台含S03-runtime-combat-fault运行期测试信息，不能将它认定为当前编译失败。首次editor/state返回ping not answered，依仓库规则停止Unity调用，本轮未启动run_tests。一次读取测试产物仍为02:27:34的48项40通过/8失败旧报告，没有本轮或整组回归新结果。未保存、Bump、提交或发布，未修改/部署Windward，Mountain保持只读。下降专用实际帧率与生命周期新增交叉测试仍待完成。

## 最新状态：2026-10-22 第四次继续

按用户要求未调用 refresh_unity。初始 state=idle，read_console 无错误，直接启动专项。脚本变动使用 MCP apply_text_edits 的导入流程。

- 候选首次与再次专项的原始结果均为48项：40通过、8失败；完整XML分别保存于 evidence/p04-d01/candidate-first-results.xml 与 candidate-results.xml。失败均在双玩家Collider、dt=.04的上升段，测试错误比较目标20，而允许速度已被扫掠收窄至约15.5；关键下降回钳步提交正确。该上升段多Collider扫掠收窄行为未在本轮修改，后续须独立评估乘员全部Collider排除契约，不将目标值强制当作允许值。
- 已通过 MCP 临时移除候选旧表面速度分支并取得修复前运行：48/48提交不一致、48/48位移断言通过。原始 evidence/p04-d01/before-fix-results.xml SHA256=49DE8C90E1B8C15C8FED10A2D602943EB6C4F2113FE3A6008784555FA18B6B2D。例：dt=.01第23步允许/支撑=-4、command=-6.40000057、postBody=-4、玩家/平台dy均=-.03999996、gap=.014998436。由此确认该用例提交缺陷被碰撞解算消除，不能认定滑落。
- 候选分支已恢复。测试改为比较允许承载速度加风，并在下降段额外校验准确目标波形；已通过 MCP apply_text_edits 提交测试修改。后续整组回归尚未完成，不能认定修改后测试已全部通过。
- 对上述完整原始XML作离线轨迹核对（trace-audit.json）：旧逻辑48/48在回钳步不匹配；候选48/48回钳步匹配，且48/48全部记录步匹配允许速度加风。这是原轨迹复核，不能替代修正断言后的正式复跑。
- 整个 Game.Player.PlayModeTests 已启动，job=2a1349fb5f0241d4b05d38e46eb37583；等待期间Unity插件会话 disconnected while awaiting command_result。按规则停止Unity调用。一次读取TestResults.xml仍是02:27:34的48项旧结果，没有本次整组结果，未将旧XML计为回归通过。
- 正式模板 carrier-motion-contract.md 已补充下降减速、提交/物理后采样契约和测试入口。未保存/Bump/发布。Windward未修改或部署，Mountain只读。

剩余：修正断言后的专项复跑、整组回归、下降专用实际帧率及生命周期交叉验证、正式保存/Bump与发布。本轮未完成最后源码状态的编译验证；初始无错误仅对应进入本轮时的已编译状态。

2026-10-22。归属：platformer-2d 模板升级；EntityMotor2D、玩家合成器和测试均由模板 Assets 持有，不需要修改框架公共运行时。未来交付仍需要新的 com.ember tag。

## 证据与结论

消费基线 Ember 0.21.1 / platformer-2d 0.11.0。维护仓库 HEAD 为 d11b6ea123603d10038ae7cb2522bfa1bdd566c2，存在大量已有工作区改动；本轮未覆盖或提交它们。Windward 和 Mountain 未写入。

只读来源：Windward docs/implementation/S3-P04-descent-deceleration-handoff.md 及 evidence/s3-carrier-probe-20261021-181933-descent-isolation.tsv。

首次失败保留在消费项目原始证据中，本轮没有替换原报告。原始行摘录：

|step|allowedY|supportY|commandY|selfY|feet|top|
|---|---:|---:|---:|---:|---:|---:|
|22|-6.40000|-6.40000|-6.40000|0|6.41200|6.39700|
|23|-4.00000|-4.00000|-6.40000|0|6.33200|6.31700|
|24|-4.00000|-4.00000|-4.00000|0|6.25200|6.23700|

提交一致性：原始证据失败。实际运动一致性：22→23 脚底和顶面均下降 .08，间距保持 .015，支持该采样区间同步；没有刚体物理后速度，不能扩展为所有物理阶段或所有产品组合通过。

## 静态根因与候选修复

PlayerExternalMotion.RemoveContribution 从上一物理步 actual 中扣除 _lastContribution，但接触裁剪读取 obstacle.attachedRigidbody.linearVelocity；宿主已将它改成本拍 -4。

朝上接触法线下，旧贡献 -6.4、新表面 -4，inward=-2.4。旧贡献被裁剪为 -4；旧 actual=-6.4，扣除后留下 -2.4；Compose 再加本拍 -4，提交变成 -6.4。物理碰撞可能随后将该负向残差消除，因此提交缺陷不能推导为滑落。

候选修改保存上一合成时的支撑来源、Revision 和表面速度，仅对该来源的接触使用旧表面速度裁剪旧贡献。其他接触保留原处理；Clear 清空快照。没有调整重力、手感、风、动作数值或产品下降波形。尚待首次失败复跑和修复后测试确认，不能称为验证完成的根因修复。

## 采样契约

1. 宿主 -1000 设置目标；正式支撑 -500 执行 PrepareStep。TargetVelocity 是自身扫掠前目标，Velocity 是当前刚体允许速度。玩家 Observe 中的乘员净空约束还可能进一步收窄上升速度。
2. 玩家 Support.Observe 后的 Support.Velocity 是本拍选定支撑贡献。稳定承载、无自身Y和竖直环境/动作接管时，期望最终提交Y等于它；有风等竖直贡献时不承诺相等。
3. Motor.CommandVelocity 在 BeginStep 后是可变工作指令，Compose 后、ExecuteStep 后才可称本拍最终提交值。ExecuteStep 写 Body.linearVelocity 和 gravityScale；MotionTrace.Record 也仍在物理前。
4. Motor.Velocity/Body.linearVelocity 是读取当刻刚体速度；提交后立即读是提交值，不能称物理后结果。随后 Unity 重力和碰撞解算允许改变它，CommandVelocity 不随解算更新。
5. 隔离测试用正式 PrepareStep → 玩家 Tick/FixedTick → 提交快照 → 隔离场景 Simulate → 物理后刚体/位置快照。该测试驱动不应用于产品运行时。真实运行应在下一次宿主改写前记录前一步 Rigidbody 位置与速度，并用步号配对前一步提交；LateUpdate 不能区分同一渲染帧内多次固定步。插值 Transform 不作物理位移基准。

## 验证状态

新增 PlayerStateTests.Carrier_DescentRecoverySeparatesSubmissionAndPhysics，48 组合：-6.4→-4、-4.4→-4；dt=.01/.02/.04；单/双玩家碰撞体；普通/单向顶面；无输入/水平输入+向下风+水平承载。逐步记录允许、支撑、Command、提交后刚体、物理后刚体、双方位移、脚底间距和同一支撑；提交与位移分别断言。同步隔离测试 FPS 明确标为 N/A，不冒充真实帧率验证。

Unity 实例已选定 ember-unity-framework（未选择 Windward）。初始 state=idle，刷新返回 compile_requested=true；首次 read_console 返回 session not ready / ping not answered。该工具编排还发出了 run_tests，返回 no_unity_session，无 job id，无测试结果。此后未重试 Unity、读取 Editor.log 或使用替代编译。新增测试及候选代码没有完成编译验证。

剩余：保留候选前的首次失败运行；48组合复测；真实固定步/实际帧率采样；撞顶、落地、主动跳离、失效、替换、owner与旧凭证清理专项；P04第11步、P02、P03、P06以及移动惯性、普通长短跳、墙滑/墙跳、地面旗跳、空中挥旗准备→回弹→自有风场回归。已有测试不算本轮通过，Windward专有轨迹仍需消费复验。

## 版本与消费路径

### 2026-10-22 恢复验证再次中断

第三次继续：用户报告手动编译完成并恢复连接。维护实例初始 state 成功，read_console 成功读取 CS0117：当前测试库 Assert 不包含 Multiple。已将新专项末尾改为兼容数组联合断言，错误消息分别显示 motion/submission 两项；未更改运行时修复方案。临时还原旧裁剪分支后请求编译，首次后续 state 又返回 ping not answered。因此停止自动验证，未启动测试，再次从备份恢复候选并确认 SHA256 与下述值一致。此次编译修正尚无成功编译确认，不能把用户此前手动编译或初始 idle 当作新源码编译通过。

用户授权继续验证，通过后保存、Bump并发布。MCP instances 确认仅维护项目 ember-unity-framework，初始 editor/state 成功且 idle。候选 PlayerExternalMotion.cs 备份于 docs/dev/evidence/p04-d01/candidate-PlayerExternalMotion.cs.txt；临时移除使用旧表面速度的条件分支，准备运行修复前专项。refresh_unity 返回 compile_requested=true；随后首次 editor/state 返回 `Unity session not ready for 'get_editor_state' (ping not answered)`。

遵守首次查询失败停止规则，本轮未调用 run_tests，未取得修复前运行失败、48组合或回归结果。已逐字恢复候选源码并核对 SHA256=EDD03024618B6721A895536FB17EE025D635E731F6D4CE6F9D74EA000C4FA244。未保存、Bump、提交、创建tag或发布；版本及模板hash维持旧基线。Windward未修改或部署，Mountain只读。

当前包声明 0.21.1；已封存 platformer-2d 0.11.0，contentHash/versionedContentHash=48deb2fc08b00e1c7ee279890b289dad。这是旧基线，不包含本轮候选。未保存模板、未 Bump、未发布新版本/tag/hash。

Unity恢复后先完成验证，再通过项目中心正式保存和补丁Bump，完成框架配套发布。Windward后续通过 Ember/UPM Manager升级正式版本；模板通过旧基线/新模板/本地定制三方补丁预览处理冲突，保护 WindwardPlayer、机关与旗跳定制，不能手改manifest/lock、PackageCache或全量覆盖。维护测试通过也不代表Windward产品组合、P04或S3签收。
