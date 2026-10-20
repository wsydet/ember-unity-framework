# W01/V01 模板能力交付记录

更新：同日用户追加授权后，已通过项目中心 Bump 为 platformer-2d 0.8.0，随 Ember 0.20.3 发布；内容 hash 保持不变。下文版本表和未发布说明记录追加授权前的实现交付状态，当前发布状态以 [0.20.3 发布说明](release-0.20.3.md) 为准。Windward 未升级，S2 未签收。

日期：2026-10-20。归属为 **platformer-2d 模板升级**，没有新增框架 Manager 或 Mountain/Windward 业务。代码、测试、操作文档在 Assets 维护，经项目中心 `EmberProjectSetup.SaveTemplate` 普通保存。未 Bump、提交、推送、发布或消费部署；Windward S2 不签收，S3 不启动。

## 版本与保存状态

| 项 | 实际值 |
| --- | --- |
| 开发端 Unity | 6000.5.4f1 |
| Ember 包版本 | 0.20.2 |
| platformer-2d 版本 | 0.7.0，版本未封存更新 |
| 本次普通保存 contentHash | `003c4429f265bbcd8d377bf375c2c15c` |
| 原 versionedContentHash | `8198525b8d263ba6fb9c489571171e70`，保持原发布基线 |
| 项目中心保存 | 成功，1436 文件；最终 FrameworkScene、sceneDirty=false、非 Play、非编译中 |
| Windward 当前消费 | Unity 6000.5.4f1，包锁定 v0.20.2 / commit `ba59f7f6b0f806d69db8c3577b0addec15cbe76d`，活动模板 0.7.0 / 原 hash |

普通保存与 Bump 的区别由仓库架构规范明确规定。本次没有收到单独的 Bump 指令，且用户禁止自动发布，因此保留未封存状态；不能把当前 contentHash 当作已发布的 0.7.0 升级。后续授权版本封存/发布后，消费方按正式模板流程处理本地差异，不能直接覆盖产品工程或编辑 PackageCache。

## 实现与公开入口

正式说明：[墙跳与单次飞行策略](../../Assets/Game/Documentation/platformer-2d/wall-flight-policy.md)。

- `PlayerJumpFeel.ConfigureWallJump(PlayerWallJumpSpec)`，存储 `_wallJump`，冻结到 `PlayerFeelRuntime.WallJump`。默认关闭；玩家工具和蓝图参数入口可编辑；完整方案 Copy/Validate/Compare/Save 复用现有闭包。空白方案显式关闭。
- 墙跳支持向墙首次直上、资格消耗、落地恢复、早期反向转出，以及可选的转出后资格恢复。保持/释放/竖直曲线与普通跳独立，水平超速减速也独立。复用普通跳的值格式，不复用其激活来源。
- `PlayerActionLease.LaunchVertical(velocity, gravityScale, owner, sourceId, PlayerFlightSpec)` 与 `PlayerExternalMotionRequest(..., flight: spec)`（只允许 InitialVelocity）。由唯一固定步提交，动作释放/结束后策略仍存在。
- `PlayerFlightLease` 按实例和会话内代次隔离，提供 SourceId/Generation/IsActive；旧动作、外力或飞行凭证无法清除新凭证。owner 禁用销毁、真实落地、新起跳/外力/动作、受击死亡和会话结束均有清理。
- `PlayerFlightSpec` 只改变所选飞行下降，保留原上升；可选低速持键下降规则。动作仍持有 Vertical 时也会应用下降策略；暂停不推进物理，恢复不续接旧持键优惠。
- 新独立飞行期间拒绝瞬时快照；可选墙跳启用时空中拒绝丢弃资格的快照。继续使用检查点基线，v3 格式未改变。

没有改变内置方案资产、地空惯性、普通跳策略资产、风、挥旗时间线、动画/音频/镜头接线。保存前对 GameResource 与旧快照逐文件比较无差异。源码差异集中在 Player 模块、编辑器入口、测试及对应模板文档。

## 旧版语义核对

只读核对 Mountain 的 PlayerControl.cs、Player.prefab 和 TimeManager.asset。旧固定步为 `0.016666668`，运动顺序为 StateCheck → GracityControl → MoveControl → JumpControl，不依赖新渲染帧率。旧 `rJumpFrames <= minJumpFrames` 包含零帧边界；1/12 的整数不能直接当作新保持时间的无误差换算。

旧直上反向转出会设置 `isCanWallJumpY=true`，新策略用 `restoreOnConversion` 显式表达。旧墙跳超速步减速 .5、普通竖直步加速度 2、低速步加速度 1、阈值 5、终速 -20，在旧 60Hz 下对应约 30/120/60 的秒制速率。旧低速条件还要求 `!isJumping && isInAir && (!isWinding || isTempFree) && isSlowDown`；`isSlowDown` 并非所有动作阶段都等于当前输入。新下降策略只开放下降段持键选项，不为复刻旧分支改变已认可的上升。

测试中的墙跳参数是机制覆盖用例，不作为 Windward 已验收候选值；最终 min/max 保持、转换窗口和下降低速选项由消费对话选择并实测。

## Unity 实际验证

全部由框架开发端 Unity MCP 执行，合成输入均为 **SYNTHETIC**。使用正式图、独立配置和局部真实 PhysicsScene2D；测试夹具可摆放初始位置和碰撞体，消费代码不需要写 Motor/Rigidbody。没有 MCP 截图。

| 批次 | 结果 | 证据与范围 |
| --- | --- | --- |
| 首轮新增契约 | 14 项执行，3 项失败 | [保留状态](evidence/wall-flight-20261020/first-run.json)。测试在探地命中时提前结束，尚未满足真实接触清理；修改测试落地采样终点，未放宽清理逻辑。该任务工具未提供完整 result，不能补造失败曲线 |
| 受影响原通路及新增契约 | 87/87 | [原始结果](evidence/wall-flight-20261020/regression-87.json)：原墙跳/墙滑、普通长短跳完整轨迹、移动惯性、外部发射轨迹、动作租约、墙跳新分支和来源隔离 |
| 生命周期增量 | 11/11 | [结果](evidence/wall-flight-20261020/boundary-11.json)：受击/死亡、owner 销毁、动作旧凭证、暂停持键、普通/墙跳替换等 |
| 正式编辑工作流 | 2/2 | [结果](evidence/wall-flight-20261020/workflow-2.json)：普通与墙跳 Copy/Validate/Compare/Save、独立资产引用、无效值拒绝保存、值捕获冻结、内置方案默认关闭 |
| 更新来源起点与高精度记录 | 14/14 | [结果](evidence/wall-flight-20261020/contract-14.json)：空中来源从 y=3、地面来源从 y=.9，帧率真实记录 |
| 最终运动边界 | 17/17 | [结果](evidence/wall-flight-20261020/final-boundary.json)：包括向下发射首步终速、未启用策略的原发射回归；此批覆盖最终运行代码 |
| 左右新墙跳完整曲线 | 4/4 | [结果](evidence/wall-flight-20261020/wall-curves-4.json)：左右 × 直上/外蹬，每项包含短/长按、真实落地及碰顶 |
| 物理步末时间标签纠正 | 7/7 | [最终曲线](evidence/wall-flight-20261020/final-curves-7.json)：上述 4 项和 3 项动作来源；只修正日志步末时间标签，不改变参数或运行逻辑 |

批次有重叠，不将数字相加成独立用例总数。最终控制台查询 0 条错误，真实类型已编译可用。之前仅 scripts 刷新未导入新文件导致的缺类型编译错误已由正式 all 刷新解决，不使用其他编译器代替 Unity 编译。

曲线采用真实物理步后位置/速度，最终来源曲线中相对上升高度均约 **3.190927**，到顶约 **0.46s**。独立地面旗跳代理保留共享下降；空中/地面自动回弹代理分别启用策略并验证下降加速度、终速及动作退出后继续有效。完成真实落地清理的观测时间分别约 .96/.86/.76s（旗跳/空中自动/地面自动）；地面探针和真实接触相位不同，不能将此终点直接冒充 Windward 原报告的落地相位，也不能比较不同起点的下降时间。测试代理验证契约隔离，不是 Windward 挥旗业务已接线。

帧率矩阵固定步 .02s，输入请求保持 .12s。最新墙跳/外部飞行实测平均 FPS：目标30为30.022/30.019，目标60为61.834/60.057，目标240为187.978/238.914；墙跳实际松键时间约 .133826/.133406/.139406s。前一轮高帧率平均约244.417/236.192。只记录这些平均实测，不声称稳定120/240或逐帧曲线完全相等。原来短按标签 t=0 表示首个步末采样的历史文件保留；最终7项使用正确的步末时间，FPS矩阵从一开始即使用累计物理时间。

## 未验证与消费边界

- 未在 Windward 部署或修改候选/动作代码，未进行正式 Main → 关卡原路线再验收；旧复现、失败诊断和候选文件保持原处。
- Windward 普通跳4/60、14/60及全部已认可特殊跳基线，需要在正式消费后按一致采样相位 A/B；本轮模板参数和代理轨迹不能替代该项目签收。
- 未做人工键盘观感、动画听感、镜头冲量复核；手柄暂缓，没有 S3、构建或三关签收。
- 新策略的独立测试验证了停止/owner/受击/死亡等清理；未单独运行带此策略的真实产品转场或跨模板切换。场景退出使用现有候选 Release 清理链路，仍需消费集成覆盖。
- 配置入口源码及工作流已验证，未做面板截图/人工视觉验收。没有修改或校准 Mountain。

后续顺序：授权 Bump/发布 → 消费方升级包并正式处理模板差异 → 独立完整候选配置与分支接线 → 基线/来源/生命周期/原路线回归 → 键盘观感与听感确认。W01/V01 通用能力已实现并普通保存；产品 S2 缺口关闭由 Windward 对话正式消费后判定。
