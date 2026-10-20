# P06 弹射尾段维护交付记录

日期：2026-10-20。归属判定：**模板升级**，由新的 com.ember 版本分发。
运行契约属于 platformer-2d 的 Entity/Player；不引入框架 Manager，不修改消费项目。

实现、测试与正式模板封存已完成；发布提交与远端核验由本轮发布流程完成。Windward 的 P06 及 S3 尚未签收，P01/P04 独立保留。

## 起点与原始失败

维护起点：框架 0.20.4，提交 `30af301`；platformer-2d 0.9.0，
封存 hash `f640fe07081a1958665c2ed9194196ea`。
维护项目正式编辑记录对应这一模板，五个受管目录与封存快照逐文件一致。
既有工作区的模板切换、字体、manifest/lock 等改动不属于本次发布。

完整阅读 Windward 的 P06 交接、四组尾段证据、构造器探针及蘑菇详细验证记录。
其首次失败原样保存在 [consumer-original](evidence/p06-launch-tail/consumer-original/s3-mushroom-tail-gap.json)；
四份 TSV 的 SHA256 与交接报告逐份一致。实际尾段减速度约29.43，
触发/保持/表现通过不等于 `legacyRhythmMatches=true`。
这些是 Windward 提供的原始运行结果，不是本轮在消费项目重新运行。

只读核对 Mountain 三份来源，SHA256 与交接一致：

| 来源 | SHA256 |
| --- | --- |
| `Assets/Script/PlayerControl.cs` | `30f30133cfe0e4fba01a4634ec5211b045b66a4512e022b871bc479e47b8fbc1` |
| `Assets/Prefab/Player.prefab` | `1682983fe92544d85fb16ecd74179604921f98ad262c0355cc1bbb2118938172` |
| `ProjectSettings/TimeManager.asset` | `8fa4333ac4e99d3c522b512e4f9ec78d73401504ac0efaece4aeaafa0e45dfff` |

源码确认保持计数从0开始、上限12；自由飞行每步减2，闭区间±5且许可有效时每步减1，
下降终速−20。固定步0.016666668秒，推导加速度约120/60。
保持时强制慢速许可；结束后严格低于−5或落地才退出标记，再跟随正常 JumpHeld。
以上是源码和参数推导，**没有重新运行 Mountain**。

## 契约与范围

实现采用可选 `LaunchTailSpec2D`，默认关闭；仅支持 ConstantRise + ContinueVelocity。
来源配置按值复制冻结。保持到期或碰顶在玩家唯一固定步内交接真实自身Y，
不重新发射，不经过共享重力空档。尾段覆盖上升、低速区、下降和自身终速，
慢速闩锁首次严格低于下边界后不再自动建立。

P02只控制保持段水平行为；P03只在接受成功时刷新资格。尾段不延长输入锁。
启用策略的上/侧弹保留既有风和支撑，按自身速度计算加速度，再统一合成贡献。
原 `PlayerFlightSpec`、默认旧弹射、PlayerFeel、共享重力和方案资产不调整。
载具收到新策略明确拒绝，不静默接受；不交付或关闭P01/P04。
活跃尾段拒绝瞬时快照，使用合法检查点基线；owner、代次、生命流程和会话清理独立。

## 验证与发布状态

初始 MCP 服务不可达；用户恢复后服务曾返回零实例，用户再次连接后确认维护项目
`ember-unity-framework@b23e9b768e85ed30` / Unity 6000.5.4f1。
未使用 BatchMode、dotnet 或日志轮询替代编译。

修改前玩家状态测试 **530/530** 通过，原始 JSON/XML 均保留。
只使用旧API的15组逐步轨迹夹具在修改前 **15/15** 通过；首次实现专项 **44/44** 通过，
包含15组兼容轨迹、28项新策略状态测试和1项正式会话恢复测试。
旧轨迹覆盖地面/空中惯性、长短普通跳、墙滑/首次直上/蹬墙/反向转换/再次墙跳、
独立窗口地面旗跳、地面自动回弹、空中准备→回弹→风、旧上弹继续/停速和侧弹。
每组200个dt=0.02固定步，升级前后共3000对样本，位置XY、速度XY、重力数值差全部为0，
状态、资格及来源逐项一致。测试夹具的旗动作和风参数属于维护侧契约样例，
不是 Windward 的认可方案、自有风场或原房间产品复验；wall-repeat 夹具包含显式重定位。

- [修改前530项结果](evidence/p06-launch-tail/baseline-player-01.xml)
- [修改前轨迹](evidence/p06-launch-tail/baseline-protected-01.xml)
- [首次实现专项44项及原始轨迹](evidence/p06-launch-tail/implementation-first-01.xml)
- [逐步对照结果](evidence/p06-launch-tail/protected-comparison-01.json)
- [测试摘要、保持/尾段相邻步及实际FPS](evidence/p06-launch-tail/first-evidence-summary.json)
- [源码保护哈希](evidence/p06-launch-tail/protected-source-hashes.json)：6个既有接入文件修改，813个既有文件逐字节不变；新增5个C#及其.meta。PlayerFeel、方案配置、Controller等不变。

首次 scripts 刷新未导入新增资产，出现3条 CS0246；全资产刷新后导入成功、编译错误归零，
公共类型在Unity中成功加载。[首次编译诊断](evidence/p06-launch-tail/compile-01.txt)保留。
测试域重载曾断开MCP，用户恢复后读取正式测试回调生成的完整XML，并清除已结束的孤立任务；
没有以日志、BatchMode或dotnet替代Unity验证。

首次专项实际帧率（平均值，非稳定帧率/性能基准）：

| 目标FPS | dt=0.02实际平均FPS | dt≈1/60实际平均FPS |
| --- | --- | --- |
| 30 | 30.0000668 | 29.999939 |
| 120 | 119.312958 | 109.269226 |
| 240 | 217.122559 | 222.54007 |

输入为合成键盘，每个真实渲染帧重新提交输入，由帧累加器驱动独立PhysicsScene2D。
各组保留原始固定步位置/自身速度/保持标记以及帧数、实际秒数。帧率受编辑器焦点影响。
专项包含左右上/侧弹、按住/松开、普通/墙跳进入、闭区间边界、首次低于−5后许可变化、
真实落地、风和支撑、碰顶、拒绝/替换、owner、暂停、生命与恢复、快照拒绝。

无风、dt=0.02的上弹原始样本在t=0.22保持Y20，t=0.24直接为Y17.6；
侧弹从Y10直接进入Y7.6。每步减少2.4对应120加速度，低速区减少1.2对应60；
下降自身终速限制为20，真实落地后清除尾段。这里是维护项目物理结果，
只与旧源码推导目标对照，不标记为Mountain重跑实测。

首次玩家状态全回归 **572/573** 通过、0跳过，持续1649.2018343秒。
唯一失败是 `ActionContract_OwnerDisableOrDestroyEndsExactlyOnce(True)` 的TearDown捕获到
Unity Package Manager `Error fetching package list / EMFILE: too many open files`；
原始堆栈为 `UnityEditor.EditorApplication:Internal_CallUpdateFunctions()`。
没有把该环境日志改成预期日志来让测试通过。保留[完整首次全回归](evidence/p06-launch-tail/regression-player-01.xml)，
该失败用例已在最终相关复测中通过，首次失败不覆盖。此轮15组旧轨迹再次逐样本一致，见[第二轮对照](evidence/p06-launch-tail/protected-comparison-02.json)。

静态复核提出“顶棚+反向风”组合补测，恢复连接后实测 **4/6通过、2/6失败**：
向下风−3的上弹和侧弹在世界Y已被顶棚阻挡为0时，扣除负风后的自身Y仍为+3，
原先仅判断自身Y≤0的实现没有结束保持。正风及无顶棚强反向风用例通过。
[首次失败与完整轨迹](evidence/p06-launch-tail/collision-first-01.xml)独立保留。

修复只在启用尾段的分支中增加真实顶面接触/相对表面速度检查；
保留扣除环境后的真实自身速度，不清零或重发初速，且无顶棚时不因世界合速度为负误停。
默认旧请求仍沿用原判断路径。

修复后[最终相关复测](evidence/p06-launch-tail/final-related-01.xml) **79/79通过、0跳过**，
持续95.0317192秒。包括49项尾段/兼容/碰顶测试、1项正式会话测试、27项P02/P03策略测试，
以及2项owner禁用/销毁测试。此前两个反向风碰顶失败用例和受EMFILE日志干扰的owner用例均通过。
没有把首次572/573全回归改写为全绿；最终79项是修复后的相关复测，不代表重新运行完整全集。

[最终旧行为逐步对照](evidence/p06-launch-tail/protected-comparison-final.json)仍为15组、3000对样本，
位置XY、速度XY、重力数值差全部为0，状态、资格和来源一致。
[最终源码保护哈希](evidence/p06-launch-tail/protected-source-hashes-final.json)记录6个既有接入文件修改、
813个既有文件逐字节不变，新增6个C#及其.meta。
[已取得结果汇总](evidence/p06-launch-tail/verified-runs-before-editor-summary.json)保留各轮计数、失败堆栈、
原始相邻步及实际FPS，不包含尚未取得的Editor结果。

随后启动 `Game.Player.Editor.Tests`，任务ID `2e03214d67ab438ab4afd0b727c4e259`。
首次结果查询返回 `Unity plugin session 4a28993c-e111-4de6-9c67-6e678b6539ac disconnected while awaiting command_result`；
按仓库规则停止自动查询。检查时尚无预定的 `editor-01.xml`，不能判定该轮通过或失败。
已请求用户确认测试结束、手动编译并恢复MCP。该查询失败原文保存在
[Editor连接诊断](evidence/p06-launch-tail/editor-query-disconnect-01.json)。
用户确认手动编译并恢复MCP后，取得该任务成功结果：[Editor 76/76通过](evidence/p06-launch-tail/editor-01.xml)，
持续337.1212796秒、0跳过。恢复后C#编译错误查询为0条。
此前查询断开诊断保持不变；[完整各轮摘要](evidence/p06-launch-tail/final-evidence-summary.json)补齐Editor结果。

正式版本：框架0.20.5 / 模板0.10.0。模板新增可选能力升minor，框架包patch用于分发。
项目中心SaveTemplate保存1456个文件，再通过BumpTemplateVersion显式升minor。
封存hash为 `58d152503e6f95494434f095a8df6ef4`，父base 0.8.3及兼容声明0.20.0不变。
[正式校验](evidence/p06-launch-tail/release-template-validation.json)确认全部模板的实际hash与父快照，
当前编辑记录和磁盘内容一致，0错误/0警告/0差异；未手改模板快照或hash。
框架包版本通过正式upm-bump-version脚本预览后更新。
包版本变化后PackageInfo缓存仍旧，请求一次Resolve；首次版本查询失败。
用户再次确认手动恢复后，查询仍返回success=false且无详情，按规则停止自动Unity查询。
最后失败只发生在元数据更新后的缓存确认阶段；源码编译、最终79项、Editor76项和模板实际校验均已完成。
封存后C#、资源及父谱系未改变；没有已知编译错误或未封存模板，继续授权的Git发布，
但不声称Resolve后的编辑器缓存/编译状态已确认。
发布详情与消费步骤见[0.20.5发布说明](release-0.20.5.md)。

## 消费与验收边界

Windward 后续先保护基线、定制、GUID、认可方案和输入，再通过 UPM Manager 消费正式版本；
模板跨minor按正式迁移流程吸收必要代码。在独立候选中只给普通方向蘑菇显式启用策略，
复验完整轨迹、原路线、现有移动/普通跳/墙跳/两类地面旗动作及空中挥旗完整轨迹。

本轮不修改或升级 Windward，不修改 Mountain。框架测试与契约夹具不能替代产品接线、
手柄实机、主观手感、声音/镜头、发行构建或S3总出口验收。
