# Windward S3 P01/P04 维护记录（未封存、未发布）

2026-10-21。本轮只修改框架开发仓库的项目 Assets；Windward、Mountain 和消费项目 PackageCache 均未写入。归属判定为**模板升级**：Entity/Player 是 platformer-2d 部署到 Assets 的可定制玩法组件，不提升为所有游戏必需的 Package Manager。模板交付仍需未来框架发布 tag；本轮未发布。

最新进展：原821项回归任务已取得最终结果，**821/821通过、0失败、0跳过**，未重复启动，完整结果见 [regression-821.json](evidence/carrier-contract/regression-821.json)。随后准备P04当前版本正式基线复现，刷新后的首次Console查询失去Editor响应；基线尚未运行。现已恢复与这821项受测草稿一致的所有运行源，未验证采样脚本撤回Assets外。模板保存/Bump仍未执行。以下历史中断记录保留原貌，以上述最终回归结果取代旧“全套结果未知”的状态。

## 实际基线与证据边界

- 仓库 HEAD：`cafc3ea0e459c7ef98eb0975f7a0504703055950`；工作区预先存在大量修改和未跟踪文件，未重置、暂存或提交。
- `com.ember/package.json`：**0.21.0**；Unity **6000.5.4f1**。
- 编辑身份与已封存 platformer-2d：**0.10.0**，`contentHash = versionedContentHash = 58d152503e6f95494434f095a8df6ef4`。
- 父模板：base **0.8.3**，`parentContentHash = 1270ce6dfcae3c01d050c09ecfd60337`。以上 hash 是本轮前的已存储内容，不代表本轮草稿。
- Windward 交接报告的 0.20.4 / 0.9.0 结果只作为历史证据；后续已消费 0.20.5 / 0.10.0 的情况未在消费端复测。

只读来源：Windward `S3-T02-carrier-collision-gap.md`、`S3-T03-carrier-expiry-order-gap.md`、`S3-generated-support-blueprints.md`、`Migration/Mountain/s3-moving-platform-plan.json` 及两组原始碰撞/乘员报告与 TSV。原规格的 60 个生成支撑、独立盒接缝和机器不对称参数均不由本轮改写。

当前版本的隔离物理 P01 诊断见 [baseline-current.txt](evidence/carrier-contract/baseline-current.txt)：41 × 0.02 秒，目标 `(4,-4)`，直接 SetVelocity 位移 `(3.28,-3.28)`；现有整向量扫掠最终约 `(1.22732,-1.22732)`，切向一起停止。与旧报告数值略有差异，不复用旧报告当作新测试。该诊断在真实 Play 中使用独立 PhysicsScene2D 同步推进，**不是渲染 FPS 或唯一 PlayerModule 正式流程验收**。

首次探针误在 Edit Mode 创建 LocalPhysicsMode 场景，创建前抛异常；记录保留在 [first-attempt.txt](evidence/carrier-contract/first-attempt.txt)，不计物理证据。

P04 当前版本在修改前只完成源码定位，**没有完成当前版本、正式 PlayerModule 的首次失败复现**。原版独立 FixedUpdate 与 GameLauncher 的顺序仍不受约束，但不将此静态事实称为新实测。原 0.20.4 报告第 11 步的平台 Y0 / 玩家 Support、Command Y20 和实际 FPS106.34/109.79 保留原含义，不标成稳定120。

## 修复草稿及兼容范围

项目维护源：

- `Assets/Game/Module/Entity/EntityTranslationSupport2D.cs`：增加默认关闭的载具运动约束、`PrepareStep`、目标速度诊断、乘员登记释放及瞬移/停用清理；自动阶段为 DefaultExecutionOrder(-500)。
- `Assets/Game/Module/Entity/TranslationSupportSweep2D.cs`：逐个有效附着实体 Collider Cast，过滤 Trigger、Layer 与忽略碰撞；按阻挡法线收窄位移，保留允许的切向。每次投影从真实原点重扫，复杂夹角12轮未收敛保守停止，不提交未验证路径。
- `Assets/Game/Module/Entity/TranslationSupportLaunchTarget2D.cs`：计时不再有独立 FixedUpdate，由支撑阶段内部调用 `AdvanceStep`；新增公开 `CancelLaunch`。PreserveControl、ContinueVelocity、StopVertical 的策略值和参数不变。
- `Assets/Game/Module/Player/PlayerSupportMotion.cs`：连续有效支撑按上一步相对速度判断玩家是否主动上跳，避免平台到期骤停时把上一步承载速度误当玩家跳离；离台、清理释放乘员登记。
- `Assets/Game/Module/Player/Tests/PlayerCarrierContractTests.cs`：25 个隔离物理专项用例。

`EntityMotor2D.SetSweptDisplacement`、`ExecuteStep` 完全未改。未改玩家方案、普通跳/墙跳/旗跳数值、风公式、PlayerModule 驱动入口、P03资格及P06尾段代码。未修改不等于回归已通过；交叉验证状态见下文。

自身扫掠默认关闭，旧支撑直接速度路径保持。载具计时移至支撑固定步阶段、停用与 Teleport 取消弹射属于有意修正的生命周期变化。调用方若曾依赖默认组件乱序或瞬移保留弹射，需调整。

单向规则由查询侧显式筛选：用 enabled/useOneWay、colliderMask、rotationalOffset、surfaceArc 及起始分离判断；向上可穿、从有效表面下降才阻挡。未更改原 PlatformEffector2D 或全局碰撞矩阵。旋转/缩放 Effector、复杂组合 Effector 尚待专项，不宣称与 Unity 所有 Effector 细节完全等价。

## 草稿接口与阶段约定（尚非已发布契约）

初始化保留 Kinematic、冻结旋转、零重力与正式实体形状；通过 `support.ConfigureMotion(true, obstacleLayers, .025f)` 显式开启。碰撞层必须包含正式地形；额外 Layer、Trigger、IgnoreCollision 仍由真实项目配置决定。

每个物理步的唯一流程：

1. 产品宿主 FixedUpdate **-1000**：计算产品自身水平与多来源风一次；弹射中只调用 `TrySetHorizontalVelocity(x)`，不回写 Y、不重发弹射。无弹射时 `SetVelocity(productVelocity)`；触发/替换请求也必须在支撑阶段前提交。
2. 支撑 FixedUpdate **-500**：内部推进当前 owner、剩余秒数与结束策略，再捕获 `TargetVelocity`，对全部实体形状约束，写本步自身允许速度。宿主不再调用 PrepareStep。
3. GameLauncher 默认 **0** → 唯一 PlayerModule → PlayerController.FixedTick：Sensors → Support.Observe。连续乘员保持有效支撑；净空收窄 Y 后再做自身扫掠，避免方向变化遗漏侧障碍；然后采集支撑最终速度。
4. 玩家外部运动在原 Compose 中只合成一次支撑与环境贡献，Motor ExecuteStep 提交，随后由 Unity 正常物理积分。

必须核对消费项目 Script Execution Order 没有覆盖上述相对顺序。产品在 -500 之后写载具或替换请求不属于该接线契约；不得在玩家观察后恢复目标速度。多玩家先后净空约束的全局一致性本轮未验收，本交接范围是唯一玩家。

```csharp
// 初始化阶段
support.ConfigureMotion(true, obstacleLayers, .025f);

// 产品宿主类标记 [DefaultExecutionOrder(-1000)]。
private void FixedUpdate()
{
    float horizontal = ComputeProductHorizontalOnce(); // 产品原公式，含风，只计算一次
    if (launch.IsLaunching)
        launch.TrySetHorizontalVelocity(horizontal); // 当前已约束的 Y 不被恢复
    else
        support.SetVelocity(new Vector2(horizontal, ComputeProductVertical()));
    // 不额外驱动支撑、玩家或 Physics2D.Simulate。
}
```

`PrepareStep(dt)` 公开给隔离物理测试/显式模拟宿主，不得与自动 FixedUpdate 在同一步重复调用。当前没有步 ID 去重，正式消费只使用自动阶段。`TargetVelocity` 指弹射到期处理后的自身扫掠输入；若要记录产品最初目标，宿主需另存自己的发令数据。`Velocity` 是当前最终速度，玩家不能将 TargetVelocity 用作承载。

13/60 秒在 dt=.02 的离散计时中第11次推进到期：StopVertical 本步在玩家观察前清Y；ContinueVelocity 到期仅释放弹射所有权并保留当前允许速度。碰撞/净空将Y收窄后，水平API不会复活原上升速度。替换先结束旧请求再设置新请求；旧owner不会清新owner。

## 已完成测试与中断

[tests-first.json](evidence/carrier-contract/tests-first.json) 保存完整 MCP 结果、每项名字、输出和耗时；[retest-traces.txt](evidence/carrier-contract/retest-traces.txt) 保存逐步原始文本。Job `7a8e3d2b72674b42a226d9d5fb62039f`，PlayMode **25/25通过，0失败，0跳过**，测试耗时约0.586秒。

这些是测试程序集中的同步隔离物理，不额外驱动生产会话。记录明确标注 `FPS=N/A-synchronous`；不得据此宣称真实120FPS或正式Module自动顺序已验收。

| 范围 | 本轮状态 |
| --- | --- |
| 空载地面、左右墙、顶棚、斜向贴地/沿墙升降 | 专项通过 |
| 附着子Circle实体、薄墙、角落及高速(200,100) | 专项通过（有限场景） |
| Trigger、碰撞对、Layer矩阵、显式掩码过滤 | 专项通过 |
| 原Box(1,.3)/offset(0,.35)、179.5度、Grouping=false，上穿/落稳/横移接缝 | 专项通过；属于来源验收，不标成此前新缺陷 |
| (15,10)、(-15,10)、(7.5,8)、(-10.5,5)运动向量 | 空载薄角障碍专项通过；未覆盖全部真实弹射组合 |
| Y20、13/60，左右X2，ContinueVelocity/StopVertical第11步玩家提交 | 显式PrepareStep的乘员专项通过 |
| 乘员净空及载具墙约束、水平更新不恢复Y | 单一组合专项通过 |
| Teleport revision、停用/启用清弹射 | 专项通过 |
| 正式 Main→白盒、真实键盘跳上载具、唯一PlayerModule自动阶段和实际FPS | **未验证** |
| 有乘员全部地面/左右墙/顶棚/沿墙矩阵及单向平台 | **未完整验证** |
| 载具与玩家同时多来源/反向风、None/Horizontal/Full | **未完成本轮交叉回归** |
| 到期与撞顶同一步、同一步替换、旧/新owner生命周期矩阵 | **未完整验证** |
| 暂停/恢复、销毁、复位、退出/转场及输入门禁/快照 | **未完成本轮交叉回归** |
| P02/P03/P06、长短跳、惯性、墙滑/墙跳 | 原821项Entity/Player回归全部通过；不代替尚未完成的正式载具流程与Windward专属回归 |
| Windward地面旗跳、空中挥旗准备→回弹→自有风、原路线、听感、手柄 | **未验证且不得本轮代签** |

随后启动 Entity/Player 全套 PlayMode Job `de09f55c68c84c9a8d413d3c52053519`；第一次有界状态等待返回 `success=false,error=TimeoutError`。遵循仓库 [Unity验证规则](../../Packages/com.ember/Documentation~/maintenance/unity-validation.md)，停止自动验证，不重试、读Editor.log或用其他编译器。该批未知，既不记通过，也不把MCP超时当成具体用例行为失败。完整中断记录见 [validation-interruption.json](evidence/carrier-contract/validation-interruption.json)。超时之后没有修改受测运行代码。

请在 Unity 中手动触发编译；如果仍有报错，发回首条编译错误及完整堆栈。本轮整体 Unity 编译及回归验证未完成；此前25项专项成功的证据仍独立有效。

## 封存与后续消费

### 续轮 MCP 恢复检查

第三次续轮遵照用户要求先读取原Job，不重复启动。持续取得389、417、491、538、628、654、698、703、741、744、768、770、803项进度，最终取得821/821通过（0失败/0跳过，2022.923秒）。编译重载前的准备工作仅在Assets外写入正式流程及交叉用例草稿，不改变原回归受测代码。保存范围静态核对仅含本任务3个运行源修改和新增Sweep/专项测试及meta，见 [pre-save-differences.json](evidence/carrier-contract/pre-save-differences.json)。

821项结束后，临时将EntityTranslationSupport2D、TranslationSupportLaunchTarget2D、PlayerSupportMotion恢复为封存0.10.0原源，SHA逐一相同（[核对记录](evidence/carrier-contract/p04-baseline-source-verification.json)），导入正式采样草稿并请求MCP刷新。首次Console查询返回 `Unity session not ready for 'read_console' (ping not answered)`，按规则停止验证；没有启动基线Play。因此仍不能宣称P04当前版本正式复现或正式流程通过。已立即恢复修复稿及原专项测试，并撤回新采样脚本，逐文件SHA与原受测草稿相同（[恢复记录](evidence/carrier-contract/post-baseline-attempt-restoration.json)）。详细中断见 [p04-baseline-attempt-interruption.json](evidence/carrier-contract/p04-baseline-attempt-interruption.json)。

待编译的正式流程与新增交叉用例保存在 [staging说明](evidence/carrier-contract/staging/README.md)，不是已执行证据。带乘员上穿单向平台存在源码疑点，尚未实测，未应用计划中的过滤修复。当前MCP失去Editor响应，本次恢复后的Unity编译验证未完成；请在Unity中手动触发编译，若有报错提供首条错误和完整堆栈。保存/Bump与发布均未执行。

第二次重连检查：框架实例唯一在线，Editor状态查询成功，确认上轮821项Job仍在PlayMode运行；一次即时job查询返回已完成382/821、报告失败列表为空、stuckSuspected=false。这只是中间进度，不计382项通过。准备正式流程采样期间，下次即时job查询返回 `Unity plugin session 0521ac58-6499-491d-b0a6-11ec9ddffb6c disconnected while awaiting command_result`。按规则再次停止自动验证；没有新运行代码、新测试、保存或Bump。详见 [mcp-recovery-check-2.json](evidence/carrier-contract/mcp-recovery-check-2.json)。本次未完成Unity编译验证，请在Unity中手动触发编译；仍有报错时提供首条编译错误及完整堆栈。

2026-10-21，用户明确授权重新检查MCP、补齐验证后保存并Bump。实例列表与框架会话选择成功，但首次 `mcpforunity://editor/state` 返回 `success=false`、`Unity session not ready for 'get_editor_state' (ping not answered)`。工具目录可读不代表Editor就绪。按Unity验证规则停止本轮自动验证，未重试、未增加运行代码、未执行新测试。P04当前版本正式流程复现、完整回归、保存和Bump仍待完成；上轮全套job结果仍未知。证据见 [mcp-recovery-check.json](evidence/carrier-contract/mcp-recovery-check.json)。本轮未完成Unity编译验证；请在Unity中手动触发编译，仍有报错时提供首条编译错误及完整堆栈。

**未执行中心保存、Bump、提交、推送或发布。** 因验证中断且验收矩阵未完成，当前只保留项目Assets维护草稿；未直接写Templates~或ParentSnapshot~，未伪造新版本/hash，不能消费为已发布修复。中心保存尚未尝试，不能声称中心保存失败或中心本身不可用。

恢复维护验证后：先取得完整回归结果，补正式Module与渲染采样，核对工作区原有草稿；将应分发的文档在Assets维护源中整理，然后通过项目中心SaveTemplate保存、显式Bump封存，复核新contentHash/versionedContentHash、父base0.8.3及父hash。新可选能力倾向minor，最终版本应在封存时明确，不在此预填。发布按维护对话实际授权单独执行。

未来Windward消费步骤（本轮均未执行）：

1. 保护本地定制和部署基线，确认实际旧包/模板版本与hash；保留认可方案和原风公式。
2. 仅在真实版本已发布后，通过 **Ember/UPM Manager** 升级包；不改manifest/lock或PackageCache。仅包升级不会更新部署到Assets的Entity/Player。
3. 对比新封存模板，以本地基线和定制人工移植上述4个运行维护源及新增Sweep文件，保留既有.meta/GUID；测试与文档按需迁移。避免全量覆盖。是否可用补丁增量必须符合最终major.minor版本条件，不能提前假定。
4. 在Windward载具宿主初始化启用约束，固定在-1000发令；弹射保持期只发水平，不改结束策略。核对默认GameLauncher0和support-500的实际顺序。
5. 独立复跑来源白盒、原单向平台、所有原不对称弹射与风组合、到期步轨迹、生命周期及原路线。记录目标/允许/实际、Support/Motor提交、位置、输入、物理步、目标/实际FPS。维护通过不能关闭Windward P01/P04或S3。

本轮没有发布提交，不能提供“对应修复提交”或新可消费hash；当前基线提交只用于定位。
